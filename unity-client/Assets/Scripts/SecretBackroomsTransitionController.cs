using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.Progression;

namespace MyVirtualCommunity.Client.World
{
    public enum PlayerWorldContext
    {
        SurfaceCorridor,
        RemanenciaLevel0
    }

    /// <summary>
    /// Orchestrates the non-Euclidean Backrooms Level 1 transition from the Building A South
    /// mysterious doorway:
    ///   - Shows the opaque Threshold Veil inside the doorway while the vault is available
    ///     (hiding the interior from anyone peeking from the sidewalk).
    ///   - When another player already occupies Backrooms Level 1 ("decoy_empty"), hides the
    ///     veil so newcomers only see the small, empty dusty room.
    ///   - When an authorized player walks completely past the Threshold Veil (X < triggerX),
    ///     slams & relocks the real exterior door behind them, claims occupancy on World Core,
    ///     additively loads "BackroomsLevel1Scene", switches lighting/fog to Level 0 fluorescent
    ///     ambience, and teleports the player seamlessly into the 14m x 14m liminal maze.
    ///   - Exiting via BackroomsExitDoorTrigger unloads "BackroomsLevel1Scene", restores the
    ///     corridor sky/sun/HUD, releases occupancy, and returns the player to Av. Benito Juárez.
    /// </summary>
    public class SecretBackroomsTransitionController : MonoBehaviour
    {
        public const string BackroomsSceneName = "BackroomsLevel1Scene";

        // Remanencia — Nivel 0 (Hotel de las Palomas) enclosed unfinished-concrete atmosphere palette
        public static readonly Color RemanenciaLevel0AmbientLight = new Color(0.29f, 0.31f, 0.33f);
        public static readonly Color RemanenciaLevel0FogColor = new Color(0.16f, 0.175f, 0.19f);
        public const float RemanenciaLevel0FogDensity = 0.028f;

        [Header("Scene References")]
        public SecretKeypadDoorController outerStreetDoor;
        public GameObject thresholdVeilObject;

        [Header("Threshold Trigger Bounds (BorderCorridorScene)")]
        public float veilTriggerX = -11.94f;
        public float roomMinZ = -13.50f;
        public float roomMaxZ = -11.15f;
        public Vector3 streetExitReturnPosition = new Vector3(-11.20f, 0.24f, -11.99f);
        public float streetExitReturnYaw = 90f; // Facing East toward Av. Benito Juárez

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool isPlayerInBackrooms = false;
        [SerializeField] private bool isTransitioning = false;
        [SerializeField] private bool occupiedByOtherPlayer = false;
        [SerializeField, Range(0f, 1f)] private float fadeAlpha = 0f;

        private ApiClient cachedApiClient;
        private CharacterController cachedPlayerCC;
        private Transform cachedPlayerTransform;
        private float nextStatusPollTime = 0f;

        // Saved outdoor lighting/fog/environment state restored on exit
        private readonly List<Light> suspendedOutdoorLights = new List<Light>();
        private Material savedSkybox;
        private Color savedAmbientLight;
        private UnityEngine.Rendering.AmbientMode savedAmbientMode;
        private bool savedFogEnabled;
        private Color savedFogColor;
        private float savedFogDensity;
        private FogMode savedFogMode;
        private bool hasSavedRenderState = false;
        private Camera savedPlayerCamera;
        private CameraClearFlags savedCameraClearFlags;
        private Color savedCameraBackgroundColor;
        private bool hasSavedCameraState = false;
        private Texture2D fadeBlackTex;

        // Implementation 07.12: Low-cost single-shot exterior observation snapshot (384x480 matching 1.36x1.72m glass aspect, ~0.70 MB VRAM, 0 per-frame cost)
        public static readonly Vector3 ExteriorObservationCamPos = new Vector3(-11.78f, 1.55f, -11.99f);
        public static readonly Vector3 ExteriorObservationCamEuler = new Vector3(0f, 90f, 0f);
        public const int ExteriorObservationWidth = 384;
        public const int ExteriorObservationHeight = 480;

        private RenderTexture exteriorObservationSnapshot;
        private string lastExitSource = string.Empty;
        private int completedExitCount = 0;

        public const string LegacyInteriorDoorExitSource = "exit-door-to-street";

        public bool IsPlayerInBackrooms => isPlayerInBackrooms;
        public bool IsTransitioning => isTransitioning;
        public bool IsOccupiedByOtherPlayer => occupiedByOtherPlayer;
        public float TransitionFadeAlpha => fadeAlpha;
        public string LastExitSource => lastExitSource;
        public int CompletedExitCount => completedExitCount;
        public RenderTexture ActiveExteriorObservationSnapshot => exteriorObservationSnapshot;

        /// <summary>
        /// Implementation 07.13 centralized exit-source authorization.
        /// Only explicitly authorized unconventional exits (currently "threshold-of-trust")
        /// may initiate a return transition from Remanencia to Av. Benito Juárez.
        /// </summary>
        public static bool IsAuthorizedInWorldExitSource(string exitSource)
        {
            return string.Equals(exitSource, RemanenciaThresholdOfTrustTrigger.ExitSourceId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Authoritative world-context state for world-scoped player HUD presentation.
        /// Reports RemanenciaLevel0 strictly after the player has completed arrival inside Remanencia
        /// (never during surface startup, never during entry/exit transition fades).
        /// </summary>
        public PlayerWorldContext CurrentWorldContext =>
            isPlayerInBackrooms && !isTransitioning
                ? PlayerWorldContext.RemanenciaLevel0
                : PlayerWorldContext.SurfaceCorridor;

        public bool IsPlayerActivelyInRemanencia => CurrentWorldContext == PlayerWorldContext.RemanenciaLevel0;

        public void SetWorldTransitionStateForTest(bool inBackrooms, bool transitioning)
        {
            isPlayerInBackrooms = inBackrooms;
            isTransitioning = transitioning;
            RefreshThresholdVeilVisibility();
        }

        private void Start()
        {
            if (outerStreetDoor == null)
            {
                GameObject doorGo = GameObject.Find("BuildingA_SouthBay_StorageDoor");
                if (doorGo != null) outerStreetDoor = doorGo.GetComponent<SecretKeypadDoorController>();
            }
            cachedApiClient = FindFirstObjectByType<ApiClient>();
            RefreshThresholdVeilVisibility();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (cachedPlayerTransform == null)
            {
                CharacterController cc = FindFirstObjectByType<CharacterController>();
                if (cc != null)
                {
                    cachedPlayerCC = cc;
                    cachedPlayerTransform = cc.transform;
                }
            }

            // Periodically poll World Core occupancy while outside near the street block
            if (!isPlayerInBackrooms && !isTransitioning && Time.unscaledTime >= nextStatusPollTime)
            {
                nextStatusPollTime = Time.unscaledTime + 3.0f;
                PollVaultOccupancyStatus();
            }

            RefreshThresholdVeilVisibility();

            // Trigger seamless entry into Backrooms Level 1 as soon as player steps into the open Threshold Veil
            if (!isPlayerInBackrooms && !isTransitioning && !occupiedByOtherPlayer && cachedPlayerTransform != null)
            {
                Vector3 p = cachedPlayerTransform.position;
                bool doorIsOpen = outerStreetDoor == null || (outerStreetDoor.IsOpen && outerStreetDoor.OpenProgress > 0.55f);
                if (doorIsOpen && p.x < veilTriggerX && p.z >= roomMinZ && p.z <= roomMaxZ && p.y > -5f)
                {
                    StartCoroutine(EnterBackroomsRoutine());
                }
            }
        }

        private void OnGUI()
        {
            if (fadeAlpha <= 0.01f && glitchFlashAlpha <= 0.01f) return;
            if (fadeBlackTex == null)
            {
                fadeBlackTex = new Texture2D(1, 1);
                fadeBlackTex.SetPixel(0, 0, Color.white);
                fadeBlackTex.Apply();
            }
            Color prev = GUI.color;
            if (glitchFlashAlpha > 0.01f)
            {
                GUI.color = new Color(0.88f, 0.85f, 0.56f, glitchFlashAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeBlackTex);
            }
            if (fadeAlpha > 0.01f)
            {
                GUI.color = new Color(0.03f, 0.03f, 0.02f, fadeAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeBlackTex);
            }
            GUI.color = prev;
        }

        private void OnDestroy()
        {
            ReleaseExteriorObservationSnapshot();
            if (isPlayerInBackrooms && cachedApiClient != null)
            {
                cachedApiClient.LeaveSecretVault("player-1");
            }
            if (fadeBlackTex != null)
            {
                Destroy(fadeBlackTex);
                fadeBlackTex = null;
            }
        }

        public void SetOccupiedByOtherForTest(bool occupied)
        {
            occupiedByOtherPlayer = occupied;
            RefreshThresholdVeilVisibility();
        }

        private void PollVaultOccupancyStatus()
        {
            if (cachedApiClient == null)
            {
                cachedApiClient = FindFirstObjectByType<ApiClient>();
            }
            if (cachedApiClient == null) return;

            cachedApiClient.GetSecretVaultStatus(
                "player-1",
                res =>
                {
                    if (res != null && res.success)
                    {
                        occupiedByOtherPlayer = res.occupiedByOther || res.modeForPlayer == "decoy_empty";
                        RefreshThresholdVeilVisibility();
                    }
                },
                _ =>
                {
                    // Offline fallback: solo player can always enter Backrooms
                    occupiedByOtherPlayer = false;
                    RefreshThresholdVeilVisibility();
                });
        }

        private void RefreshThresholdVeilVisibility()
        {
            if (thresholdVeilObject == null) return;
            bool shouldShowVeil = !isPlayerInBackrooms && !occupiedByOtherPlayer;
            if (thresholdVeilObject.activeSelf != shouldShowVeil)
            {
                thresholdVeilObject.SetActive(shouldShowVeil);
            }
        }

        // Curated wall-free open spots across the 40m x 24m Backrooms Level 1 maze (deliberately FAR from the NE exit door!)
        private static readonly Vector2[] RandomCeilingDropZonesXZ = new Vector2[]
        {
            new Vector2(-7.5f,  2.7f),  // Central Pillared Hall NW
            new Vector2(-3.5f, -0.2f),  // Central Pillared Hall Center
            new Vector2( 0.5f,  2.7f),  // Central Pillared Hall SE
            new Vector2( 3.8f,  8.2f),  // North Quiet Corridor West
            new Vector2( 9.8f,  7.2f),  // North Quiet Corridor Mid
            new Vector2(12.6f,  3.0f),  // East Wing Hall North
            new Vector2(17.2f, -1.5f),  // East Wing Corridor South
            new Vector2( 4.5f, -8.2f),  // South-Center Investigation Corridor
            new Vector2(-3.8f, -8.5f),  // South-West Labyrinth Passage
            new Vector2(-14.5f, 0.8f),  // West Corridor Mid
        };

        private AudioSource dropAudioSource;
        private AudioClip noClipGlitchClip;
        private AudioClip carpetThudClip;
        private float glitchFlashAlpha = 0f;

        /// <summary>
        /// Retained non-bypassing legacy wrapper. Delegates strictly through centralized
        /// exit-source authorization, which rejects "exit-door-to-street" under the active
        /// 07.13 "Check-in Without Conventional Check-out" rule.
        /// </summary>
        public bool RequestExitBackrooms()
        {
            return RequestExitRemanencia(LegacyInteriorDoorExitSource);
        }

        /// <summary>
        /// Centralized return entry point. Under Implementation 07.13, only explicitly
        /// authorized in-world exit sources ("threshold-of-trust") may initiate ExitBackroomsRoutine().
        /// Prevents duplicate/simultaneous transitions and preserves the verified Av. Benito Juárez
        /// return position, atmosphere restoration, scene unload, World Core occupancy release,
        /// and progression integrity.
        /// </summary>
        public bool RequestExitRemanencia(string exitSource = "default", Vector3? customReturnPosition = null, float? customReturnYaw = null)
        {
            if (!isPlayerInBackrooms || isTransitioning) return false;
            if (!IsAuthorizedInWorldExitSource(exitSource))
            {
                return false;
            }
            lastExitSource = exitSource;
            Vector3 returnPos = customReturnPosition ?? streetExitReturnPosition;
            float returnYaw = customReturnYaw ?? streetExitReturnYaw;
            StartCoroutine(ExitBackroomsRoutine(returnPos, returnYaw));
            return true;
        }

        /// <summary>
        /// Exposes the actual return transition coroutine for automated verification tests
        /// while enforcing the exact same exit-source authorization gate as runtime.
        /// </summary>
        public IEnumerator CreateExitRemanenciaRoutineForTest(string exitSource = "test", Vector3? customReturnPosition = null, float? customReturnYaw = null)
        {
            if (!isPlayerInBackrooms || isTransitioning) return EmptyRoutine();
            if (!IsAuthorizedInWorldExitSource(exitSource)) return EmptyRoutine();
            lastExitSource = exitSource;
            Vector3 returnPos = customReturnPosition ?? streetExitReturnPosition;
            float returnYaw = customReturnYaw ?? streetExitReturnYaw;
            return ExitBackroomsRoutine(returnPos, returnYaw);
        }

        private static IEnumerator EmptyRoutine()
        {
            yield break;
        }

        public void CaptureAndApplyBackroomsAtmosphereForTest()
        {
            CaptureExteriorStreetSnapshot();
            isPlayerInBackrooms = true;
            isTransitioning = false;
            ApplyBackroomsAtmosphere();
            BindExteriorSnapshotToInteriorEntranceView();
        }

        public void RestoreOutdoorAtmosphereForTest()
        {
            isPlayerInBackrooms = false;
            isTransitioning = false;
            RestoreOutdoorAtmosphere();
        }

        /// <summary>
        /// Phase-2 Silent Easter Egg: Triggered without any hint or UI warning when the player
        /// descends the Av. Benito Juarez x Calle Azucenas manhole. Performs a sudden legacy
        /// dimensional reality-tear glitch and physically drops the player from the sealed
        /// acoustic ceiling into a random disorienting spot inside the 40m x 24m maze.
        /// </summary>
        public void TriggerManholeNoClipDropToBackrooms(Transform playerTransform)
        {
            if (isPlayerInBackrooms || isTransitioning) return;
            if (playerTransform != null)
            {
                cachedPlayerTransform = playerTransform;
                cachedPlayerCC = playerTransform.GetComponent<CharacterController>();
            }
            StartCoroutine(EnterBackroomsCeilingDropRoutine());
        }

        private void EnsureDropAudioReady()
        {
            if (dropAudioSource == null)
            {
                dropAudioSource = gameObject.AddComponent<AudioSource>();
                dropAudioSource.playOnAwake = false;
                dropAudioSource.spatialBlend = 0f;
            }
            if (noClipGlitchClip == null) noClipGlitchClip = CreateNoClipGlitchClip();
            if (carpetThudClip == null) carpetThudClip = CreateCarpetThudClip();
        }

        private IEnumerator EnterBackroomsCeilingDropRoutine()
        {
            isTransitioning = true;
            EnsureDropAudioReady();

            // 1. Sudden unsettling reality tear / no-clip audio + brief glitch flash (zero text/UI hints!)
            if (noClipGlitchClip != null && dropAudioSource != null)
            {
                dropAudioSource.PlayOneShot(noClipGlitchClip, 0.65f);
            }
            glitchFlashAlpha = 0.45f;
            yield return new WaitForSecondsRealtime(0.07f);
            glitchFlashAlpha = 0f;
            yield return FadeTo(1f, 0.18f);

            // 2. Claim occupancy gracefully in background (never blocks or shows decoy text for the Easter Egg)
            if (cachedApiClient != null)
            {
                cachedApiClient.EnterSecretVault("player-1", _ => { }, _ => { });
            }

            // 3. Additively load BackroomsLevel1Scene
            Scene backroomsScene = SceneManager.GetSceneByName(BackroomsSceneName);
            if (!backroomsScene.isLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(BackroomsSceneName, LoadSceneMode.Additive);
                if (loadOp != null)
                {
                    while (!loadOp.isDone) yield return null;
                }
            }

            // 4. Capture one-shot Av. Benito Juárez street snapshot BEFORE suspending outdoor lighting, then activate Backrooms atmosphere
            CaptureExteriorStreetSnapshot();
            isPlayerInBackrooms = true;
            RefreshThresholdVeilVisibility();
            ApplyBackroomsAtmosphere();
            BindExteriorSnapshotToInteriorEntranceView();
            if (Application.isPlaying)
            {
                PlayerProgressionService.Shared.RecordBackroomsEntered(viaAzucenasManhole: true);
                RestoreLoadedBackroomsProgression(PlayerProgressionService.Shared);
            }
            Physics.SyncTransforms();

            // 5. Pick a random verified wall-free drop zone far from the NE exit door + random disorienting yaw
            Vector2 dropXZ = PickRandomSafeDropZoneXZ();
            float randomYaw = UnityEngine.Random.Range(0f, 360f);
            const float floorY = -50.00f;
            const float ceilingDropStartY = -47.92f; // Feet ~2.08m above damp carpet, dropping right out of ceiling!

            TeleportPlayer(new Vector3(dropXZ.x, ceilingDropStartY, dropXZ.y), Quaternion.Euler(0f, randomYaw, 0f));

            // 6. Snap visibility open as player physically falls out of the ceiling!
            Camera cam = cachedPlayerTransform != null ? cachedPlayerTransform.GetComponentInChildren<Camera>() : null;
            Transform camT = cam != null ? cam.transform : null;
            Vector3 origCamLocalPos = camT != null ? camT.localPosition : new Vector3(0f, 1.6f, 0f);
            float rollDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;

            // Fast reveal into mid-air free-fall
            fadeAlpha = 0f;
            float currentY = ceilingDropStartY;
            float fallVel = -4.2f;
            const float fallGravity = 22.0f;

            if (cachedPlayerCC != null) cachedPlayerCC.enabled = false;
            while (currentY > floorY + 0.04f)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.04f);
                fallVel -= fallGravity * dt;
                currentY = Mathf.Max(floorY + 0.04f, currentY + fallVel * dt);
                if (cachedPlayerTransform != null)
                {
                    cachedPlayerTransform.position = new Vector3(dropXZ.x, currentY, dropXZ.y);
                }
                if (camT != null)
                {
                    float progress = Mathf.InverseLerp(ceilingDropStartY, floorY + 0.04f, currentY);
                    camT.localRotation = Quaternion.Euler(Mathf.Lerp(26f, 44f, progress), 0f, rollDir * Mathf.Lerp(5f, 15f, progress));
                }
                yield return null;
            }

            // 7. Heavy damp-carpet impact thud + disoriented camera jolt & recovery
            if (carpetThudClip != null && dropAudioSource != null)
            {
                dropAudioSource.PlayOneShot(carpetThudClip, 0.85f);
            }

            if (cachedPlayerTransform != null)
            {
                cachedPlayerTransform.position = new Vector3(dropXZ.x, floorY + 0.04f, dropXZ.y);
            }
            Physics.SyncTransforms();
            if (cachedPlayerCC != null) cachedPlayerCC.enabled = true;

            // Brief physical impact crouch dip & tilted head recovery (0.52s) — when player looks up, ceiling is sealed!
            float recoverElapsed = 0f;
            const float recoverDuration = 0.52f;
            while (recoverElapsed < recoverDuration)
            {
                recoverElapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(recoverElapsed / recoverDuration);
                float smooth = Mathf.SmoothStep(0f, 1f, u);
                if (camT != null)
                {
                    float dip = Mathf.Sin(u * Mathf.PI) * -0.26f * (1f - u * 0.5f);
                    camT.localPosition = origCamLocalPos + new Vector3(0f, dip, 0f);
                    float pitch = Mathf.Lerp(38f, 6f, smooth);
                    float roll = Mathf.Lerp(rollDir * 16f, 0f, smooth);
                    camT.localRotation = Quaternion.Euler(pitch, 0f, roll);
                }
                yield return null;
            }

            if (camT != null)
            {
                camT.localPosition = origCamLocalPos;
                camT.localRotation = Quaternion.Euler(6f, 0f, 0f);
            }

            isTransitioning = false;
        }

        private Vector2 PickRandomSafeDropZoneXZ()
        {
            // Shuffle candidate list and verify capsule clearance against Backrooms walls/pillars/furniture
            int count = RandomCeilingDropZonesXZ.Length;
            int startIdx = UnityEngine.Random.Range(0, count);
            for (int offset = 0; offset < count; offset++)
            {
                Vector2 c = RandomCeilingDropZonesXZ[(startIdx + offset) % count];
                Vector3 pBot = new Vector3(c.x, -49.55f, c.y);
                Vector3 pTop = new Vector3(c.x, -48.35f, c.y);
                if (!Physics.CheckCapsule(pBot, pTop, 0.42f))
                {
                    return c;
                }
            }
            // Guaranteed open fallback in Central Pillared Hall
            return new Vector2(-3.5f, -0.2f);
        }

        private static AudioClip CreateNoClipGlitchClip()
        {
            int sr = 22050;
            float dur = 0.34f;
            int samples = Mathf.RoundToInt(sr * dur);
            float[] data = new float[samples];
            System.Random rng = new System.Random(1996);
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float u = t / dur;
                float freq = Mathf.Lerp(260f, 42f, u * u);
                phase += 2f * Mathf.PI * freq / sr;
                float burst = ((i / 380) % 2 == 0) ? 1f : 0.35f; // VHS stutter tear
                float staticNoise = ((float)rng.NextDouble() * 2f - 1f) * 0.48f * burst;
                float dropSub = Mathf.Sin(phase) * 0.60f;
                float env = Mathf.Sin(u * Mathf.PI);
                data[i] = (dropSub + staticNoise) * env * 0.72f;
            }
            AudioClip clip = AudioClip.Create("BackroomsNoClipGlitch", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCarpetThudClip()
        {
            int sr = 22050;
            float dur = 0.28f;
            int samples = Mathf.RoundToInt(sr * dur);
            float[] data = new float[samples];
            System.Random rng = new System.Random(2024);
            float phase = 0f;
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 16f);
                float freq = Mathf.Lerp(115f, 38f, Mathf.Clamp01(t / 0.14f));
                phase += 2f * Mathf.PI * freq / sr;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.86f + noise * 0.14f;
                data[i] = (Mathf.Sin(phase) * 0.78f + lp * 0.65f) * env * 0.90f;
            }
            AudioClip clip = AudioClip.Create("BackroomsCarpetImpactThud", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private IEnumerator EnterBackroomsRoutine()
        {
            isTransitioning = true;

            // 1. Slam & relock the real exterior door behind the player immediately
            if (outerStreetDoor != null)
            {
                outerStreetDoor.CloseAndRelock();
            }

            // 2. Claim authoritative occupancy in World Core (graceful offline fallback if unreachable)
            bool occupancyResolved = false;
            bool granted = true;
            if (cachedApiClient != null)
            {
                cachedApiClient.EnterSecretVault(
                    "player-1",
                    res =>
                    {
                        if (res != null && res.success)
                        {
                            granted = res.granted && res.modeForPlayer != "decoy_empty";
                            occupiedByOtherPlayer = !granted;
                        }
                        occupancyResolved = true;
                    },
                    _ =>
                    {
                        granted = true;
                        occupancyResolved = true;
                    });
                float waitLimit = Time.unscaledTime + 0.65f;
                while (!occupancyResolved && Time.unscaledTime < waitLimit)
                {
                    yield return null;
                }
            }

            if (!granted)
            {
                // Another player claimed the vault at the exact same moment -> stay in empty decoy room
                RefreshThresholdVeilVisibility();
                isTransitioning = false;
                yield break;
            }

            // 3. Quick claustrophobic blackout fade-in (0.28s)
            yield return FadeTo(1f, 0.28f);

            // 4. Additively load BackroomsLevel1Scene
            Scene backroomsScene = SceneManager.GetSceneByName(BackroomsSceneName);
            if (!backroomsScene.isLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(BackroomsSceneName, LoadSceneMode.Additive);
                if (loadOp != null)
                {
                    while (!loadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // 5. Mark Backrooms mode active BEFORE teleporting so void-fall & street reconciliation guards never race
            isPlayerInBackrooms = true;
            RefreshThresholdVeilVisibility();
            if (Application.isPlaying)
            {
                PlayerProgressionService.Shared.RecordBackroomsEntered(viaAzucenasManhole: false);
                RestoreLoadedBackroomsProgression(PlayerProgressionService.Shared);
            }

            // 6. Capture one-shot Av. Benito Juárez street snapshot BEFORE suspending outdoor lighting, then switch to Level 0 atmosphere
            CaptureExteriorStreetSnapshot();
            ApplyBackroomsAtmosphere();
            BindExteriorSnapshotToInteriorEntranceView();

            // 7. Teleport player smoothly to Backrooms Arrival SpawnPoint
            GameObject spawnMarker = GameObject.Find("Backrooms_SpawnPoint");
            Vector3 targetPos = spawnMarker != null ? spawnMarker.transform.position : new Vector3(17.80f, -49.94f, 8.80f);
            Quaternion targetRot = spawnMarker != null ? spawnMarker.transform.rotation : Quaternion.Euler(0f, -90f, 0f);
            TeleportPlayer(targetPos, targetRot);

            yield return new WaitForSecondsRealtime(0.10f);
            yield return FadeTo(0f, 0.35f);
            isTransitioning = false;
        }

        private IEnumerator ExitBackroomsRoutine(Vector3 returnPos, float returnYaw)
        {
            isTransitioning = true;

            // 1. Fade to black
            yield return FadeTo(1f, 0.26f);

            // 2. Release single-shot observation texture, restore corridor sky/sun/fog & teleport player cleanly back onto Av. Benito Juárez sidewalk
            ReleaseExteriorObservationSnapshot();
            RestoreOutdoorAtmosphere();
            TeleportPlayer(returnPos, Quaternion.Euler(0f, returnYaw, 0f));

            // Keep exterior door locked behind player
            if (outerStreetDoor != null)
            {
                outerStreetDoor.CloseAndRelock();
            }

            // 3. Unload BackroomsLevel1Scene to free memory/GPU immediately
            Scene backroomsScene = SceneManager.GetSceneByName(BackroomsSceneName);
            if (backroomsScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(backroomsScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // 4. Release authoritative occupancy on World Core
            if (cachedApiClient != null)
            {
                cachedApiClient.LeaveSecretVault("player-1");
            }

            isPlayerInBackrooms = false;
            occupiedByOtherPlayer = false;
            completedExitCount++;
            RefreshThresholdVeilVisibility();
            if (Application.isPlaying)
            {
                PlayerProgressionService.Shared.RecordReturnedFromBackroomsToCorridor();
            }

            yield return new WaitForSecondsRealtime(0.08f);
            yield return FadeTo(0f, 0.32f);
            isTransitioning = false;
        }

        /// <summary>
        /// Captures a single low-resolution snapshot of Av. Benito Juárez from just outside the
        /// Building A South doorway facing East (+X) strictly BEFORE ApplyBackroomsAtmosphere()
        /// disables outdoor sunlight or modifies RenderSettings. Zero per-frame rendering cost
        /// and zero modifications to BorderCorridorScene.unity or the exterior door.
        /// </summary>
        private void CaptureExteriorStreetSnapshot()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }

            ReleaseExteriorObservationSnapshot();

            List<Renderer> temporarilyHidden = null;
            if (cachedPlayerTransform != null)
            {
                Renderer[] playerRenderers = cachedPlayerTransform.GetComponentsInChildren<Renderer>(includeInactive: false);
                if (playerRenderers.Length > 0)
                {
                    temporarilyHidden = new List<Renderer>(playerRenderers.Length);
                    for (int i = 0; i < playerRenderers.Length; i++)
                    {
                        if (playerRenderers[i] != null && playerRenderers[i].enabled)
                        {
                            playerRenderers[i].enabled = false;
                            temporarilyHidden.Add(playerRenderers[i]);
                        }
                    }
                }
            }

            GameObject camGo = new GameObject("__TempExteriorOneWaySnapshotCam__");
            try
            {
                exteriorObservationSnapshot = new RenderTexture(
                    ExteriorObservationWidth,
                    ExteriorObservationHeight,
                    16,
                    RenderTextureFormat.ARGB32);
                exteriorObservationSnapshot.name = "Remanencia_OneWayExteriorSnapshot_RT";
                exteriorObservationSnapshot.wrapMode = TextureWrapMode.Clamp;
                exteriorObservationSnapshot.filterMode = FilterMode.Bilinear;
                exteriorObservationSnapshot.Create();

                camGo.transform.position = ExteriorObservationCamPos;
                camGo.transform.rotation = Quaternion.Euler(ExteriorObservationCamEuler);

                Camera snapCam = camGo.AddComponent<Camera>();
                snapCam.fieldOfView = 58f;
                snapCam.nearClipPlane = 0.18f;
                snapCam.farClipPlane = 65f;
                snapCam.allowHDR = false;
                snapCam.allowMSAA = false;
                snapCam.targetTexture = exteriorObservationSnapshot;
                snapCam.Render();
                snapCam.targetTexture = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SecretBackroomsTransitionController] Single-shot exterior observation capture skipped; using static fallback. ({ex.Message})");
                ReleaseExteriorObservationSnapshot();
            }
            finally
            {
                if (Application.isPlaying) Destroy(camGo);
                else DestroyImmediate(camGo);

                if (temporarilyHidden != null)
                {
                    for (int i = 0; i < temporarilyHidden.Count; i++)
                    {
                        if (temporarilyHidden[i] != null) temporarilyHidden[i].enabled = true;
                    }
                }
            }
        }

        private void BindExteriorSnapshotToInteriorEntranceView()
        {
            if (exteriorObservationSnapshot == null) return;
            RemanenciaOneWayEntranceView view = FindFirstObjectByType<RemanenciaOneWayEntranceView>();
            if (view != null)
            {
                view.ApplyCapturedExteriorTexture(exteriorObservationSnapshot);
            }
        }

        private void ReleaseExteriorObservationSnapshot()
        {
            RemanenciaOneWayEntranceView view = FindFirstObjectByType<RemanenciaOneWayEntranceView>();
            if (view != null)
            {
                view.ClearCapturedExteriorTexture();
            }
            if (exteriorObservationSnapshot != null)
            {
                exteriorObservationSnapshot.Release();
                if (Application.isPlaying) Destroy(exteriorObservationSnapshot);
                else DestroyImmediate(exteriorObservationSnapshot);
                exteriorObservationSnapshot = null;
            }
        }

        /// <summary>
        /// Hydrates loaded BackroomsLevel1Scene vault/safe/portal controllers from local progression
        /// so previously unlocked safes, acquired relics, and activated portals survive scene unload/reload.
        /// </summary>
        public static void RestoreLoadedBackroomsProgression(PlayerProgressionService progression)
        {
            if (progression == null) return;

            SecretRoomDimensionVaultController vault = FindFirstObjectByType<SecretRoomDimensionVaultController>();
            if (vault != null)
            {
                vault.ApplyProgressionState(progression);
            }

            SecretKeypadDoorController[] doors = FindObjectsByType<SecretKeypadDoorController>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null && !doors[i].allowInteriorSafetyBypass)
                {
                    doors[i].ApplyProgressionState(progression);
                }
            }
        }

        private void TeleportPlayer(Vector3 worldPos, Quaternion worldRot)
        {
            if (cachedPlayerTransform == null) return;
            if (cachedPlayerCC != null) cachedPlayerCC.enabled = false;
            cachedPlayerTransform.position = worldPos;
            cachedPlayerTransform.rotation = worldRot;
            Physics.SyncTransforms();
            if (cachedPlayerCC != null) cachedPlayerCC.enabled = true;
        }

        private Camera ResolveActivePlayerCamera()
        {
            Camera cam = cachedPlayerTransform != null ? cachedPlayerTransform.GetComponentInChildren<Camera>() : null;
            if (cam == null) cam = Camera.main;
            return cam;
        }

        private void ApplyBackroomsAtmosphere()
        {
            if (!hasSavedRenderState)
            {
                savedSkybox = RenderSettings.skybox;
                savedAmbientLight = RenderSettings.ambientLight;
                savedAmbientMode = RenderSettings.ambientMode;
                savedFogEnabled = RenderSettings.fog;
                savedFogColor = RenderSettings.fogColor;
                savedFogDensity = RenderSettings.fogDensity;
                savedFogMode = RenderSettings.fogMode;
                hasSavedRenderState = true;
            }

            suspendedOutdoorLights.Clear();
            Scene mainScene = gameObject.scene;
            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l != null && l.gameObject.scene == mainScene && l.type == LightType.Directional && l.enabled)
                {
                    l.enabled = false;
                    suspendedOutdoorLights.Add(l);
                }
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = RemanenciaLevel0AmbientLight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = RemanenciaLevel0FogColor;
            RenderSettings.fogDensity = RemanenciaLevel0FogDensity;

            // Match empty void pixels behind exterior windows (e.g. 07.21 East Overhang) to Remanencia fog color
            // so Unity does not clear background void to its default blue when RenderSettings.skybox is null.
            Camera playerCam = ResolveActivePlayerCamera();
            if (playerCam != null)
            {
                if (!hasSavedCameraState)
                {
                    savedPlayerCamera = playerCam;
                    savedCameraClearFlags = playerCam.clearFlags;
                    savedCameraBackgroundColor = playerCam.backgroundColor;
                    hasSavedCameraState = true;
                }
                playerCam.clearFlags = CameraClearFlags.SolidColor;
                playerCam.backgroundColor = RemanenciaLevel0FogColor;
            }
        }

        private void RestoreOutdoorAtmosphere()
        {
            foreach (Light l in suspendedOutdoorLights)
            {
                if (l != null) l.enabled = true;
            }
            suspendedOutdoorLights.Clear();

            if (hasSavedCameraState)
            {
                Camera camToRestore = savedPlayerCamera != null ? savedPlayerCamera : ResolveActivePlayerCamera();
                if (camToRestore != null)
                {
                    camToRestore.clearFlags = savedCameraClearFlags;
                    camToRestore.backgroundColor = savedCameraBackgroundColor;
                }
                savedPlayerCamera = null;
                hasSavedCameraState = false;
            }

            if (hasSavedRenderState)
            {
                RenderSettings.skybox = savedSkybox;
                RenderSettings.ambientMode = savedAmbientMode;
                RenderSettings.ambientLight = savedAmbientLight;
                RenderSettings.fog = savedFogEnabled;
                RenderSettings.fogColor = savedFogColor;
                RenderSettings.fogDensity = savedFogDensity;
                RenderSettings.fogMode = savedFogMode;
                hasSavedRenderState = false;
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float start = fadeAlpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeAlpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            fadeAlpha = targetAlpha;
        }
    }
}
