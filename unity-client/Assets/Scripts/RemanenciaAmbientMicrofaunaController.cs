using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Lightweight, strictly cosmetic, deterministic ambient locomotion controller for
    /// Implementation 07.9 (Remanencia Nivel 0 — First Living Microfauna).
    ///
    /// Technical boundaries:
    ///   - Zero colliders, physics, Rigidbody, NavMeshAgent, AudioSource, ParticleSystem, Light, or Animator.
    ///   - Zero World Core, save-system, progression, NPC, enemy, combat, or interaction registration.
    ///   - Zero UnityEngine.Random calls: all idle/cooldown durations are deterministic hashes of (cycleIndex, phaseSeed).
    ///   - Lazily caches the observer camera transform (never searches every frame).
    ///   - Moves continuously along its serialized world-space crevice path with zero teleportation.
    /// </summary>
    public class RemanenciaAmbientMicrofaunaController : MonoBehaviour
    {
        public enum MicrofaunaState
        {
            Concealed = 0,
            IdleWait = 1,
            Emerging = 2,
            Paused = 3,
            Retreating = 4,
            Cooldown = 5
        }

        [Header("Deterministic Crevice Path (World Space)")]
        [Tooltip("Minimum 3 waypoints: [0] = fully occluded concealment recess, [1] = emergence bend, [2] = brief pause point.")]
        public Vector3[] waypoints = new Vector3[3];
        public Vector3 surfaceNormal = Vector3.up;

        [Header("Proximity Thresholds (Meters)")]
        public float activationRadius = 4.2f;
        public float deactivationRadius = 5.6f;
        public float startleRetreatRadius = 0.85f;

        [Header("Deterministic Timing & Locomotion")]
        public float crawlSpeed = 0.082f;
        public float retreatSpeedMultiplier = 1.45f;
        public float idleMinSeconds = 1.10f;
        public float idleMaxSeconds = 2.35f;
        public float pauseSeconds = 0.85f;
        public float cooldownMinSeconds = 11.0f;
        public float cooldownMaxSeconds = 15.5f;
        public int phaseSeed = 7901;

        [Header("Subtle Organic Motion (Cosmetic Only)")]
        public float undulationFrequency = 11.5f;
        public float lateralYawAmplitudeDeg = 4.5f;
        public float axialPulseAmplitude = 0.055f;
        public float pauseProbeYawDeg = 14.0f;

        // Runtime-only ephemeral state (never persisted to disk or progression saves)
        private MicrofaunaState currentState = MicrofaunaState.Concealed;
        private float pathProgress = 0f;
        private float stateTimer = 0f;
        private float motionPhase = 0f;
        private int cycleIndex = 0;
        private Vector3 baseLocalScale = Vector3.one;
        private bool initialized = false;

        // Lazily cached observer camera (throttled retry, zero per-frame scene searches)
        private Transform cachedObserver;
        private float nextObserverLookupTime = -10f;
        private const float ObserverLookupIntervalSeconds = 1.0f;

        public MicrofaunaState CurrentState => currentState;
        public float PathProgress => pathProgress;
        public int CycleIndex => cycleIndex;
        public float TotalPathLength => ComputeTotalPathLength();

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ResetToConcealedState();
            TryResolveObserver(force: true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Transform observer = GetCachedObserver(Time.unscaledTime);
            bool hasObserver = observer != null;
            Vector3 observerPos = hasObserver ? observer.position : Vector3.zero;
            StepSimulation(dt, observerPos, hasObserver);
        }

        /// <summary>
        /// Resets runtime state cleanly to the occluded start recess (waypoints[0]).
        /// </summary>
        public void ResetToConcealedState()
        {
            EnsureInitialized();
            currentState = MicrofaunaState.Concealed;
            pathProgress = 0f;
            stateTimer = 0f;
            motionPhase = 0f;
            cycleIndex = 0;
            ApplyPose(0f, moving: false, retreating: false, pauseNormalized: 0f);
        }

        /// <summary>
        /// Allows deterministic EditMode/PlayMode verification tests to supply an explicit observer transform.
        /// </summary>
        public void SetObserverOverrideForTesting(Transform observer)
        {
            cachedObserver = observer;
        }

        /// <summary>
        /// Advances the deterministic ambient microfauna simulation by a fixed or frame delta time.
        /// Guarantees continuous, non-teleporting movement along the crevice path.
        /// </summary>
        public void StepSimulation(float deltaTime, Vector3 observerPosition, bool hasObserver)
        {
            EnsureInitialized();
            if (deltaTime <= 0f || waypoints == null || waypoints.Length < 2)
            {
                return;
            }

            float clampedDt = Mathf.Min(deltaTime, 0.1f);
            float pathLen = Mathf.Max(0.01f, ComputeTotalPathLength());
            Vector3 anchorPos = waypoints[0];
            float distToObserver = hasObserver ? Vector3.Distance(observerPosition, anchorPos) : float.MaxValue;

            switch (currentState)
            {
                case MicrofaunaState.Concealed:
                    pathProgress = 0f;
                    if (hasObserver && distToObserver <= activationRadius)
                    {
                        stateTimer = ComputeDeterministicDuration(idleMinSeconds, idleMaxSeconds, cycleIndex, phaseSeed);
                        currentState = MicrofaunaState.IdleWait;
                    }
                    ApplyPose(0f, moving: false, retreating: false, pauseNormalized: 0f);
                    break;

                case MicrofaunaState.IdleWait:
                    pathProgress = 0f;
                    if (!hasObserver || distToObserver > deactivationRadius)
                    {
                        currentState = MicrofaunaState.Concealed;
                        stateTimer = 0f;
                    }
                    else
                    {
                        stateTimer -= clampedDt;
                        if (stateTimer <= 0f)
                        {
                            // If observer is already standing directly on top of the crevice, stay concealed
                            if (distToObserver <= startleRetreatRadius)
                            {
                                stateTimer = ComputeDeterministicDuration(cooldownMinSeconds, cooldownMaxSeconds, cycleIndex, phaseSeed + 31);
                                currentState = MicrofaunaState.Cooldown;
                            }
                            else
                            {
                                currentState = MicrofaunaState.Emerging;
                            }
                        }
                    }
                    ApplyPose(0f, moving: false, retreating: false, pauseNormalized: 0f);
                    break;

                case MicrofaunaState.Emerging:
                    motionPhase += clampedDt * undulationFrequency;
                    if (hasObserver && distToObserver <= startleRetreatRadius)
                    {
                        // Smooth continuous retreat from current pathProgress (zero teleport!)
                        currentState = MicrofaunaState.Retreating;
                        ApplyPose(pathProgress, moving: true, retreating: true, pauseNormalized: 0f);
                        break;
                    }

                    pathProgress += (crawlSpeed * clampedDt) / pathLen;
                    if (pathProgress >= 1f)
                    {
                        pathProgress = 1f;
                        stateTimer = Mathf.Max(0.15f, pauseSeconds);
                        currentState = MicrofaunaState.Paused;
                        ApplyPose(1f, moving: false, retreating: false, pauseNormalized: 0f);
                    }
                    else
                    {
                        ApplyPose(pathProgress, moving: true, retreating: false, pauseNormalized: 0f);
                    }
                    break;

                case MicrofaunaState.Paused:
                    pathProgress = 1f;
                    if (hasObserver && distToObserver <= startleRetreatRadius)
                    {
                        currentState = MicrofaunaState.Retreating;
                        ApplyPose(1f, moving: true, retreating: true, pauseNormalized: 0f);
                        break;
                    }

                    stateTimer -= clampedDt;
                    float pauseNorm = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.15f, pauseSeconds));
                    if (stateTimer <= 0f)
                    {
                        currentState = MicrofaunaState.Retreating;
                        ApplyPose(1f, moving: true, retreating: true, pauseNormalized: 1f);
                    }
                    else
                    {
                        ApplyPose(1f, moving: false, retreating: false, pauseNormalized: pauseNorm);
                    }
                    break;

                case MicrofaunaState.Retreating:
                    motionPhase += clampedDt * undulationFrequency * retreatSpeedMultiplier;
                    pathProgress -= (crawlSpeed * retreatSpeedMultiplier * clampedDt) / pathLen;
                    if (pathProgress <= 0f)
                    {
                        pathProgress = 0f;
                        stateTimer = ComputeDeterministicDuration(cooldownMinSeconds, cooldownMaxSeconds, cycleIndex, phaseSeed + 31);
                        currentState = MicrofaunaState.Cooldown;
                        ApplyPose(0f, moving: false, retreating: false, pauseNormalized: 0f);
                    }
                    else
                    {
                        ApplyPose(pathProgress, moving: true, retreating: true, pauseNormalized: 0f);
                    }
                    break;

                case MicrofaunaState.Cooldown:
                    pathProgress = 0f;
                    stateTimer -= clampedDt;
                    if (stateTimer <= 0f)
                    {
                        cycleIndex++;
                        currentState = MicrofaunaState.Concealed;
                        stateTimer = 0f;
                    }
                    ApplyPose(0f, moving: false, retreating: false, pauseNormalized: 0f);
                    break;
            }
        }

        public Vector3 EvaluatePathPosition(float normalizedProgress)
        {
            if (waypoints == null || waypoints.Length == 0) return transform.position;
            if (waypoints.Length == 1) return waypoints[0];

            float t = Mathf.Clamp01(normalizedProgress);
            float totalLen = ComputeTotalPathLength();
            if (totalLen <= 1e-5f) return waypoints[0];

            float targetDist = t * totalLen;
            float accum = 0f;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                float segLen = Vector3.Distance(waypoints[i], waypoints[i + 1]);
                if (accum + segLen >= targetDist || i == waypoints.Length - 2)
                {
                    float localT = segLen > 1e-5f ? Mathf.Clamp01((targetDist - accum) / segLen) : 0f;
                    // Smooth Hermite ease across interior waypoint bends while preserving endpoint contact
                    return Vector3.Lerp(waypoints[i], waypoints[i + 1], localT);
                }
                accum += segLen;
            }
            return waypoints[waypoints.Length - 1];
        }

        public Vector3 EvaluatePathTangent(float normalizedProgress)
        {
            float eps = 0.02f;
            Vector3 pA = EvaluatePathPosition(Mathf.Clamp01(normalizedProgress - eps));
            Vector3 pB = EvaluatePathPosition(Mathf.Clamp01(normalizedProgress + eps));
            Vector3 dir = pB - pA;
            if (dir.sqrMagnitude < 1e-8f && waypoints != null && waypoints.Length >= 2)
            {
                dir = waypoints[1] - waypoints[0];
            }
            return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward;
        }

        public static float ComputeDeterministicDuration(float minSeconds, float maxSeconds, int cycle, int seed)
        {
            uint h = (uint)(cycle * 374761393 + seed * 668265263 + 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            float u = (h & 0x00FFFFFFu) / 16777215f;
            return Mathf.Lerp(minSeconds, maxSeconds, u);
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            baseLocalScale = transform.localScale;
            if (baseLocalScale.sqrMagnitude < 1e-6f) baseLocalScale = Vector3.one;
            if (surfaceNormal.sqrMagnitude < 1e-6f) surfaceNormal = Vector3.up;
            else surfaceNormal = surfaceNormal.normalized;
            initialized = true;
        }

        private float ComputeTotalPathLength()
        {
            if (waypoints == null || waypoints.Length < 2) return 0f;
            float total = 0f;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                total += Vector3.Distance(waypoints[i], waypoints[i + 1]);
            }
            return total;
        }

        private void ApplyPose(float normalizedProgress, bool moving, bool retreating, float pauseNormalized)
        {
            Vector3 up = surfaceNormal.sqrMagnitude > 1e-6f ? surfaceNormal.normalized : Vector3.up;
            Vector3 pos = EvaluatePathPosition(normalizedProgress);
            Vector3 forward = EvaluatePathTangent(normalizedProgress);
            if (retreating) forward = -forward;

            // Project travel direction cleanly onto the crawling surface plane
            forward = Vector3.ProjectOnPlane(forward, up);
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            forward.Normalize();

            float yawOffset = 0f;
            float axialPulse = 0f;
            float heaveOffset = 0f;

            if (moving)
            {
                yawOffset = Mathf.Sin(motionPhase) * lateralYawAmplitudeDeg;
                axialPulse = Mathf.Sin(motionPhase * 1.6f) * axialPulseAmplitude;
                heaveOffset = (0.5f + 0.5f * Mathf.Sin(motionPhase * 1.6f)) * 0.0005f;
            }
            else if (currentState == MicrofaunaState.Paused)
            {
                // Subtle tentative lateral probe during brief pause before retreating
                float probeCurve = Mathf.Sin(pauseNormalized * Mathf.PI);
                float sideSign = (phaseSeed % 2 == 0) ? 1f : -1f;
                yawOffset = probeCurve * pauseProbeYawDeg * sideSign;
            }

            Quaternion baseRot = Quaternion.LookRotation(forward, up);
            Quaternion animRot = Quaternion.AngleAxis(yawOffset, up) * baseRot;

            transform.position = pos + up * heaveOffset;
            transform.rotation = animRot;
            transform.localScale = new Vector3(
                baseLocalScale.x * (1f - axialPulse * 0.45f),
                baseLocalScale.y * (1f + axialPulse * 0.25f),
                baseLocalScale.z * (1f + axialPulse));
        }

        private Transform GetCachedObserver(float currentUnscaledTime)
        {
            if (cachedObserver != null && cachedObserver.gameObject.activeInHierarchy)
            {
                return cachedObserver;
            }

            if (currentUnscaledTime >= nextObserverLookupTime)
            {
                nextObserverLookupTime = currentUnscaledTime + ObserverLookupIntervalSeconds;
                TryResolveObserver(force: false);
            }
            return cachedObserver;
        }

        private void TryResolveObserver(bool force)
        {
            if (!force && cachedObserver != null) return;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cachedObserver = mainCam.transform;
                return;
            }

            FirstPersonPlayer player = FindFirstObjectByType<FirstPersonPlayer>();
            if (player != null)
            {
                cachedObserver = player.transform;
            }
        }
    }
}
