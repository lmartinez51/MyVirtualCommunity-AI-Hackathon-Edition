using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    public enum AuthoritativeMovementState
    {
        Unknown,
        Fresh,
        Stale
    }

    /// <summary>
    /// Presentation-Only Controller for Deterministic NPC Waypoint Movement.
    ///
    /// Architectural Invariants:
    /// 1. World Core remains authoritative:
    ///    - Canonical positions, waypoint routes, headings, and arrival states originate from the backend.
    /// 2. Unity NEVER chooses routes, destinations, or departure/arrival times.
    /// 3. Unity smoothly interpolates visual transforms towards backend-provided positions.
    /// 4. When disconnected, retains last known positions without fabricating movement.
    /// </summary>
    public class NpcWaypointMovementController : MonoBehaviour
    {
        [Header("Backend Transport & Polling")]
        [Tooltip("Reference to the HTTP API client.")]
        public ApiClient apiClient;

        [Tooltip("Seconds between movement state polling queries.")]
        [Range(1f, 10f)]
        public float pollIntervalSeconds = 1.0f;

        [Header("Presentation Interpolation")]
        [Tooltip("Interpolation speed in meters per second (must exceed max World Core NPC speed).")]
        public float positionLerpSpeed = 6.0f;

        [Tooltip("Rotation interpolation speed in degrees per second.")]
        public float rotationLerpSpeed = 7.0f;

        [Header("Authoritative State Tracking")]
        [Tooltip("Current communication state with authoritative World Core movement service.")]
        public AuthoritativeMovementState movementAuthorityState = AuthoritativeMovementState.Unknown;

        [Tooltip("Authoritative civil time reported by the server.")]
        public string lastAuthoritativeCivilTime = "";

        [Tooltip("Seconds since last successful movement snapshot receipt.")]
        public float timeSinceLastSnapshot = 0f;

        private const float StaleThresholdSeconds = 30f;

        private class NpcPresentationTarget
        {
            public InteractionPoint point;
            public Transform visualTransform;
            public NpcAnimationController animationController;
            public Vector3 targetPosition;
            public Vector3 targetHeading;
            public Vector3 initialVisualWorldPos;
            public Quaternion initialVisualWorldRot;
            public Vector3 initialVisualLocalPos;
            public Quaternion initialVisualLocalRot;
            public bool isVisualDetachedSibling = false;
            public string primaryWaypointId = "";
            public string movementState = "stationary";
            public string currentWaypointId = "";
            public string destinationWaypointId = "";
            public string activeRouteId = "";
            public float progression = 0f;
            public string behaviorProfileType = "";
            public string autonomousBehaviorState = "";
            public string controlMode = "";
            public string patrolDirection = "none";
            public int patrolCycleIndex = 0;
            public bool isSuspendedByPlayerInteraction = false;
            public string passingEncounterPartnerNpcId = "";
            public string passingEncounterType = "";
            public string businessDomainId = "";
            public bool withinAuthorizedDomain = true;
            public float authoritativeSpeedMps = 0f;
            public float secondsSinceTargetUpdate = 0f;
            public Vector3 activeSegmentEndPosition = Vector3.zero;
            public bool hasActiveSegmentEnd = false;
            public float visualSpeedMps = 0f;
            public Vector3 lastVisualWorldPos;
            public bool hasLastVisualPos = false;
            public bool hasValidTarget = false;
        }

        private static readonly RaycastHit[] GroundHitBuffer = new RaycastHit[16];

        /// <summary>
        /// Resolves the actual solid walkway/sidewalk surface Y beneath candidatePos using a downward raycast,
        /// mirroring the PlayerController physical grounding pattern while ignoring NPC capsules and roofs (+2.5m above).
        /// </summary>
        private static float ResolveGroundedSurfaceY(Vector3 candidatePos)
        {
            Vector3 rayOrigin = new Vector3(candidatePos.x, candidatePos.y + 1.15f, candidatePos.z);
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, GroundHitBuffer, 2.5f, ~0, QueryTriggerInteraction.Ignore);
            float bestY = float.MinValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = GroundHitBuffer[i];
                if (hit.collider == null || hit.collider.isTrigger) continue;

                Transform hitT = hit.collider.transform;
                if (hitT.GetComponentInParent<BobbleheadCharacter>() != null ||
                    hitT.GetComponentInParent<InteractionPoint>() != null ||
                    hitT.GetComponentInParent<FirstPersonPlayer>() != null ||
                    hit.collider is CharacterController)
                {
                    continue;
                }

                float hitY = hit.point.y;
                if (hitY <= candidatePos.y + 0.90f && hitY >= candidatePos.y - 1.25f && hitY > bestY)
                {
                    bestY = hitY;
                    found = true;
                }
            }

            return found ? bestY : candidatePos.y;
        }

        private static Vector3 ClampBusinessDomainPresentationBoundsXZ(string npcId, Vector3 candidate)
        {
            if (string.Equals(npcId, "carlos-farmacia-attendant", StringComparison.OrdinalIgnoreCase))
            {
                candidate.x = Mathf.Clamp(candidate.x, 7.80f, 11.40f);
                candidate.z = Mathf.Clamp(candidate.z, -3.20f, 4.40f);
            }
            else if (string.Equals(npcId, "valeria-optica-attendant", StringComparison.OrdinalIgnoreCase))
            {
                candidate.x = Mathf.Clamp(candidate.x, -11.40f, -7.40f);
                candidate.z = Mathf.Clamp(candidate.z, -4.50f, 8.20f);
            }
            return candidate;
        }

        private Vector3 ComputeContinuousGoal(NpcPresentationTarget target)
        {
            Vector3 goal = target.targetPosition;
            if (movementAuthorityState == AuthoritativeMovementState.Fresh &&
                target.movementState == "moving" &&
                !target.isSuspendedByPlayerInteraction &&
                target.authoritativeSpeedMps > 0.05f &&
                target.targetHeading.sqrMagnitude > 0.001f)
            {
                float maxLeadSeconds = Mathf.Min(1.05f, Mathf.Max(0.25f, pollIntervalSeconds * 1.05f));
                float leadSec = Mathf.Clamp(target.secondsSinceTargetUpdate, 0f, maxLeadSeconds);
                float leadDist = target.authoritativeSpeedMps * leadSec;

                if (target.hasActiveSegmentEnd)
                {
                    // Strictly bounded to active authoritative segment endpoint (zero overshoot past turns or domain bounds)
                    goal = Vector3.MoveTowards(target.targetPosition, target.activeSegmentEndPosition, leadDist);
                }
                else
                {
                    Vector3 flatHeading = new Vector3(target.targetHeading.x, 0f, target.targetHeading.z).normalized;
                    goal += flatHeading * leadDist;
                    if (string.Equals(target.behaviorProfileType, "business_local_autonomy", StringComparison.OrdinalIgnoreCase))
                    {
                        goal = ClampBusinessDomainPresentationBoundsXZ(target.point != null ? target.point.characterId : "", goal);
                    }
                }
            }

            goal.y = ResolveGroundedSurfaceY(goal);
            return goal;
        }

        private static bool ShouldHoldForwardInsteadOfBackstepping(NpcPresentationTarget target, Vector3 currentPos, Vector3 continuousGoal)
        {
            if (target.movementState != "moving" ||
                target.isSuspendedByPlayerInteraction ||
                target.authoritativeSpeedMps <= 0.05f ||
                target.targetHeading.sqrMagnitude <= 0.001f)
            {
                return false;
            }
            Vector2 flatHead = new Vector2(target.targetHeading.x, target.targetHeading.z).normalized;
            Vector2 targetToCurrent = new Vector2(currentPos.x - target.targetPosition.x, currentPos.z - target.targetPosition.z);
            Vector2 toGoal = new Vector2(continuousGoal.x - currentPos.x, continuousGoal.z - currentPos.z);
            float dist = toGoal.magnitude;
            return Vector2.Dot(targetToCurrent, flatHead) > 0.0001f &&
                   dist > 0.0001f &&
                   dist < 0.75f &&
                   Vector2.Dot(toGoal, flatHead) < 0f;
        }

        private float ResolveStepSpeed(NpcPresentationTarget target, Vector3 currentPos, Vector3 continuousGoal)
        {
            float distXZ = Vector2.Distance(
                new Vector2(currentPos.x, currentPos.z),
                new Vector2(continuousGoal.x, continuousGoal.z));

            if (target.movementState == "moving" && !target.isSuspendedByPlayerInteraction)
            {
                float baseSpeed = target.authoritativeSpeedMps > 0.05f ? target.authoritativeSpeedMps : 2.5f;
                return distXZ > 0.20f
                    ? Mathf.Max(baseSpeed, Mathf.Min(positionLerpSpeed, baseSpeed + distXZ * 1.6f))
                    : baseSpeed;
            }

            return positionLerpSpeed;
        }

        private readonly Dictionary<string, NpcPresentationTarget> managedNpcs = new Dictionary<string, NpcPresentationTarget>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NpcAnimationController> animationControllersByNpcId = new Dictionary<string, NpcAnimationController>(StringComparer.OrdinalIgnoreCase);
        private Coroutine pollCoroutine = null;
        private string activePlayerInteractionNpcId = "";

        public void SetActivePlayerInteractionNpc(string npcId)
        {
            activePlayerInteractionNpcId = npcId ?? "";
        }

        private void Awake()
        {
            DiscoverSceneReferences();
        }

        private void Start()
        {
            if (apiClient == null)
            {
                apiClient = FindAnyObjectByType<ApiClient>();
            }

            if (Application.isPlaying && pollCoroutine == null)
            {
                pollCoroutine = StartCoroutine(PollMovementRosterRoutine());
            }
        }

        /// <summary>
        /// Discovers all NPCs in the scene by inspecting InteractionPoint components
        /// and binding reusable NpcAnimationController presentation components.
        /// </summary>
        public void DiscoverSceneReferences()
        {
            managedNpcs.Clear();
            animationControllersByNpcId.Clear();

            InteractionPoint[] points = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            foreach (var pt in points)
            {
                if (pt == null || string.IsNullOrEmpty(pt.characterId) || pt.category == InteractionCategory.Informational)
                {
                    continue;
                }

                if (!managedNpcs.ContainsKey(pt.characterId))
                {
                    BobbleheadCharacter bobble = ResolveNpcBobbleheadCharacter(pt, out bool isDetachedSibling);
                    Transform visualT = bobble != null ? bobble.transform : pt.transform;

                    NpcAnimationController animCtrl = ResolveOrAttachAnimationController(pt, bobble, visualT);
                    if (animCtrl != null)
                    {
                        animationControllersByNpcId[pt.characterId] = animCtrl;
                    }

                    managedNpcs[pt.characterId] = new NpcPresentationTarget
                    {
                        point = pt,
                        visualTransform = visualT,
                        animationController = animCtrl,
                        targetPosition = pt.transform.position,
                        targetHeading = pt.transform.forward,
                        initialVisualWorldPos = visualT.position,
                        initialVisualWorldRot = visualT.rotation,
                        initialVisualLocalPos = visualT.localPosition,
                        initialVisualLocalRot = visualT.localRotation,
                        isVisualDetachedSibling = isDetachedSibling,
                        primaryWaypointId = ResolvePrimaryWaypointId(pt.characterId),
                        movementState = pt.movementState ?? "stationary",
                        currentWaypointId = pt.currentWaypointId ?? "",
                        destinationWaypointId = pt.destinationWaypointId ?? "",
                        activeRouteId = pt.activeRouteId ?? "",
                        progression = pt.progression,
                        visualSpeedMps = 0f,
                        lastVisualWorldPos = visualT.position,
                        hasLastVisualPos = true,
                        hasValidTarget = true
                    };
                }
            }
        }

        private static BobbleheadCharacter ResolveNpcBobbleheadCharacter(InteractionPoint pt, out bool isDetachedSibling)
        {
            isDetachedSibling = false;
            if (pt == null) return null;

            BobbleheadCharacter childChar = pt.GetComponentInChildren<BobbleheadCharacter>();
            if (childChar != null)
            {
                return childChar;
            }

            if (pt.transform.parent != null)
            {
                BobbleheadCharacter siblingChar = pt.transform.parent.GetComponentInChildren<BobbleheadCharacter>();
                if (siblingChar != null)
                {
                    isDetachedSibling = true;
                    return siblingChar;
                }
            }

            return null;
        }

        private static NpcAnimationController ResolveOrAttachAnimationController(
            InteractionPoint pt,
            BobbleheadCharacter bobble,
            Transform visualTransform)
        {
            Transform hostTransform = visualTransform != null ? visualTransform : pt.transform;
            NpcAnimationController animCtrl =
                hostTransform.GetComponent<NpcAnimationController>() ??
                pt.GetComponent<NpcAnimationController>() ??
                pt.GetComponentInChildren<NpcAnimationController>();

            if (animCtrl == null)
            {
                animCtrl = hostTransform.gameObject.AddComponent<NpcAnimationController>();
            }

            animCtrl.Bind(pt, bobble, visualTransform);
            return animCtrl;
        }

        private static string ResolvePrimaryWaypointId(string characterId)
        {
            switch (characterId)
            {
                case "elena-border-concierge":
                    return "wp_elena_kiosk";
                case "carlos-farmacia-attendant":
                    return "wp_carlos_counter";
                case "valeria-optica-attendant":
                    return "wp_valeria_storefront";
                case "mateo-student":
                    return "wp_mateo_queue_entry";
                case "sofia-cross-border-retail":
                    return "wp_sofia_queue_mid";
                case "hector-artisan":
                    return "wp_hector_stand";
                case "patricia-citizen-rep":
                    return "wp_patricia_desk";
                default:
                    return "";
            }
        }

        private void Update()
        {
            StepVisualMovement(Time.deltaTime);
        }

        public void StepVisualMovement(float dt)
        {
            if (dt < 0f) dt = 0f;
            timeSinceLastSnapshot += dt;

            if (timeSinceLastSnapshot > StaleThresholdSeconds && movementAuthorityState == AuthoritativeMovementState.Fresh)
            {
                movementAuthorityState = AuthoritativeMovementState.Stale;
            }

            // Smoothly move each NPC transform towards its authoritative target
            foreach (var kvp in managedNpcs)
            {
                NpcPresentationTarget target = kvp.Value;
                if (target == null || target.point == null || !target.hasValidTarget) continue;

                Transform t = target.point.transform;
                target.secondsSinceTargetUpdate += dt;
                bool isFixedBorderKiosk = (t.gameObject.name == "Border_Information_Kiosk");
                bool isFixedCitizenKiosk = (t.gameObject.name == "CitizenServiceDesk");
                bool isAtPrimaryStation =
                    string.IsNullOrEmpty(target.currentWaypointId) ||
                    (!string.IsNullOrEmpty(target.primaryWaypointId) &&
                     string.Equals(target.currentWaypointId, target.primaryWaypointId, StringComparison.OrdinalIgnoreCase) &&
                     target.movementState != "moving" &&
                     target.progression <= 0.001f);

                Vector3 continuousGoal = (isFixedBorderKiosk || isFixedCitizenKiosk)
                    ? target.targetPosition
                    : ComputeContinuousGoal(target);

                bool isStaleFrozen = movementAuthorityState == AuthoritativeMovementState.Stale && !isFixedBorderKiosk && !isFixedCitizenKiosk;
                bool shouldRotateToHeading =
                    target.movementState == "moving" ||
                    string.Equals(target.autonomousBehaviorState, "turning_around", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(target.autonomousBehaviorState, "passing_encounter", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(target.behaviorProfileType, "business_local_autonomy", StringComparison.OrdinalIgnoreCase) &&
                     !target.isSuspendedByPlayerInteraction &&
                     !string.Equals(target.autonomousBehaviorState, "off_duty", StringComparison.OrdinalIgnoreCase));

                // Position interpolation (keep fixed physical kiosk booth anchored at its plaza foundation)
                if (isFixedBorderKiosk)
                {
                    t.position = new Vector3(19.20f, 0.20f, 39.50f);
                    t.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else if (isFixedCitizenKiosk)
                {
                    t.position = new Vector3(-10.80f, 0.0f, 18.55f);
                    t.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else if (!isStaleFrozen)
                {
                    if (!ShouldHoldForwardInsteadOfBackstepping(target, t.position, continuousGoal))
                    {
                        float effectiveSpeed = ResolveStepSpeed(target, t.position, continuousGoal);
                        Vector3 nextPos = Vector3.MoveTowards(t.position, continuousGoal, effectiveSpeed * dt);
                        nextPos.y = ResolveGroundedSurfaceY(new Vector3(nextPos.x, continuousGoal.y, nextPos.z));
                        t.position = nextPos;
                    }

                    // Heading rotation when moving, turning around, passing encounter, or business autonomy dwell
                    if (shouldRotateToHeading && target.targetHeading.sqrMagnitude > 0.001f)
                    {
                        Vector3 flatHeading = new Vector3(target.targetHeading.x, 0, target.targetHeading.z).normalized;
                        if (flatHeading.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(flatHeading, Vector3.up);
                            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotationLerpSpeed * dt);
                        }
                    }
                }

                // Move attached or detached visual character safely without disturbing fixed kiosks
                if (target.visualTransform != null)
                {
                    if (isStaleFrozen)
                    {
                        target.visualSpeedMps = 0f;
                    }
                    else if ((isFixedBorderKiosk || isFixedCitizenKiosk) && target.visualTransform != t)
                    {
                        if (isAtPrimaryStation)
                        {
                            target.visualTransform.localPosition = Vector3.MoveTowards(
                                target.visualTransform.localPosition,
                                target.initialVisualLocalPos,
                                positionLerpSpeed * dt);
                            target.visualTransform.localRotation = Quaternion.Slerp(
                                target.visualTransform.localRotation,
                                target.initialVisualLocalRot,
                                rotationLerpSpeed * dt);
                        }
                        else
                        {
                            float boothStepSpeed = ResolveStepSpeed(target, target.visualTransform.position, continuousGoal);
                            target.visualTransform.position = Vector3.MoveTowards(
                                target.visualTransform.position,
                                continuousGoal,
                                boothStepSpeed * dt);
                            if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                            {
                                Vector3 flatHeading = new Vector3(target.targetHeading.x, 0f, target.targetHeading.z).normalized;
                                if (flatHeading.sqrMagnitude > 0.001f)
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(flatHeading, Vector3.up);
                                    target.visualTransform.rotation = Quaternion.Slerp(
                                        target.visualTransform.rotation,
                                        targetRot,
                                        rotationLerpSpeed * dt);
                                }
                            }
                        }
                    }
                    else if (target.isVisualDetachedSibling)
                    {
                        if (isAtPrimaryStation)
                        {
                            target.visualTransform.position = Vector3.MoveTowards(
                                target.visualTransform.position,
                                target.initialVisualWorldPos,
                                positionLerpSpeed * dt);
                            target.visualTransform.rotation = Quaternion.Slerp(
                                target.visualTransform.rotation,
                                target.initialVisualWorldRot,
                                rotationLerpSpeed * dt);
                        }
                        else
                        {
                            if (!ShouldHoldForwardInsteadOfBackstepping(target, target.visualTransform.position, continuousGoal))
                            {
                                float visSpeed = ResolveStepSpeed(target, target.visualTransform.position, continuousGoal);
                                Vector3 nextVisPos = Vector3.MoveTowards(
                                    target.visualTransform.position,
                                    continuousGoal,
                                    visSpeed * dt);
                                nextVisPos.y = ResolveGroundedSurfaceY(new Vector3(nextVisPos.x, continuousGoal.y, nextVisPos.z));
                                target.visualTransform.position = nextVisPos;
                            }

                            if (shouldRotateToHeading && target.targetHeading.sqrMagnitude > 0.001f)
                            {
                                Vector3 flatHeading = new Vector3(target.targetHeading.x, 0f, target.targetHeading.z).normalized;
                                if (flatHeading.sqrMagnitude > 0.001f)
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(flatHeading, Vector3.up);
                                    target.visualTransform.rotation = Quaternion.Slerp(
                                        target.visualTransform.rotation,
                                        targetRot,
                                        rotationLerpSpeed * dt);
                                }
                            }
                        }
                    }

                    Vector3 currentVisualPos = target.visualTransform.position;
                    if (!isStaleFrozen && target.hasLastVisualPos && dt > 0.0001f)
                    {
                        float dx = currentVisualPos.x - target.lastVisualWorldPos.x;
                        float dz = currentVisualPos.z - target.lastVisualWorldPos.z;
                        target.visualSpeedMps = Mathf.Sqrt(dx * dx + dz * dz) / dt;
                    }
                    else
                    {
                        target.visualSpeedMps = 0f;
                    }

                    target.lastVisualWorldPos = currentVisualPos;
                    target.hasLastVisualPos = true;
                }

                // Keep InteractionPoint fields synchronized for HUD, nameplates, and animation
                target.point.movementState = target.movementState;
                target.point.currentWaypointId = target.currentWaypointId;
                target.point.destinationWaypointId = target.destinationWaypointId;
                target.point.activeRouteId = target.activeRouteId;
                target.point.progression = target.progression;
                target.point.behaviorProfileType = target.behaviorProfileType;
                target.point.autonomousBehaviorState = target.autonomousBehaviorState;
                target.point.movementControlMode = target.controlMode;
                target.point.patrolDirection = target.patrolDirection;
                target.point.patrolCycleIndex = target.patrolCycleIndex;
                target.point.isSuspendedByPlayerInteraction = target.isSuspendedByPlayerInteraction;
                target.point.passingEncounterPartnerNpcId = target.passingEncounterPartnerNpcId;
                target.point.passingEncounterType = target.passingEncounterType;
                target.point.businessDomainId = target.businessDomainId;
                target.point.withinAuthorizedDomain = target.withinAuthorizedDomain;

                if (target.animationController != null)
                {
                    target.animationController.SyncPresentationInputs(
                        target.movementState,
                        target.point.interactionAvailability,
                        target.point.interactionMode,
                        target.point.activityState,
                        target.currentWaypointId,
                        target.point.canInteract,
                        target.visualSpeedMps,
                        target.point.actorArchetype);
                    target.animationController.SyncAutonomousBehavior(
                        target.behaviorProfileType,
                        target.autonomousBehaviorState,
                        target.controlMode,
                        target.patrolDirection,
                        target.isSuspendedByPlayerInteraction,
                        target.passingEncounterPartnerNpcId,
                        target.passingEncounterType);
                }
            }
        }

        private IEnumerator PollMovementRosterRoutine()
        {
            // Initial wait for backend startup
            yield return new WaitForSeconds(1.5f);

            while (true)
            {
                if (apiClient != null)
                {
                    apiClient.GetNpcRosterMovement(
                        onSuccess: dto =>
                        {
                            if (dto != null && dto.success)
                            {
                                ApplyAuthoritativeRoster(dto, snapImmediately: false);
                            }
                        },
                        onError: err =>
                        {
                            if (timeSinceLastSnapshot > StaleThresholdSeconds)
                            {
                                movementAuthorityState = AuthoritativeMovementState.Stale;
                            }
                        }
                    );

                    // Reuse existing roster poll cadence with one aggregate interaction-context + social-awareness + social-interaction request (no per-NPC fan-out)
                    apiClient.GetNpcRosterInteractionContext(
                        onSuccess: ctxDto =>
                        {
                            if (ctxDto != null && ctxDto.success)
                            {
                                ApplyAuthoritativeInteractionContextRoster(ctxDto);
                            }
                        },
                        onError: err =>
                        {
                            if (timeSinceLastSnapshot > StaleThresholdSeconds)
                            {
                                ClearAllSocialAwareness();
                                ClearAllSocialInteractions();
                            }
                        },
                        playerId: "player-1",
                        activePlayerInteractionNpcId: activePlayerInteractionNpcId
                    );
                }

                yield return new WaitForSeconds(pollIntervalSeconds);
            }
        }

        /// <summary>
        /// Applies an authoritative roster movement snapshot across all managed NPCs.
        /// </summary>
        public void ApplyAuthoritativeRoster(NpcRosterMovementResponseDto dto, bool snapImmediately = false)
        {
            if (dto == null) return;

            movementAuthorityState = AuthoritativeMovementState.Fresh;
            timeSinceLastSnapshot = 0f;
            lastAuthoritativeCivilTime = dto.localCivilTime ?? "";

            if (dto.npcsList != null && dto.npcsList.Length > 0)
            {
                foreach (var npcSnap in dto.npcsList)
                {
                    ApplySingleNpcSnapshot(npcSnap, snapImmediately);
                }
            }
        }

        /// <summary>
        /// Applies authoritative movement data to an individual NPC.
        /// </summary>
        public void ApplySingleNpcSnapshot(NpcMovementSnapshotDto dto, bool snapImmediately = false)
        {
            if (dto == null || string.IsNullOrEmpty(dto.npcId)) return;

            movementAuthorityState = AuthoritativeMovementState.Fresh;
            timeSinceLastSnapshot = 0f;

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            if (managedNpcs.TryGetValue(dto.npcId, out NpcPresentationTarget target))
            {
                if (dto.position != null)
                {
                    target.targetPosition = new Vector3(dto.position.x, dto.position.y, dto.position.z);
                }
                if (dto.forwardHeading != null)
                {
                    target.targetHeading = new Vector3(dto.forwardHeading.x, 0, dto.forwardHeading.z);
                }

                target.movementState = dto.movementState ?? "stationary";
                target.currentWaypointId = dto.currentWaypointId ?? "";
                target.destinationWaypointId = dto.destinationWaypointId ?? "";
                target.activeRouteId = dto.activeRouteId ?? "";
                target.progression = dto.progression;
                target.behaviorProfileType = dto.behaviorProfileType ?? "";
                target.autonomousBehaviorState = dto.autonomousBehaviorState ?? "";
                target.controlMode = dto.controlMode ?? "";
                target.authoritativeSpeedMps =
                    target.movementState == "moving" && !dto.isSuspendedByPlayerInteraction && dto.currentSpeedMps > 0.05f
                        ? dto.currentSpeedMps
                        : 0f;
                target.secondsSinceTargetUpdate = 0f;
                if (dto.activeSegmentEndPosition != null &&
                    (Mathf.Abs(dto.activeSegmentEndPosition.x) > 0.0001f ||
                     Mathf.Abs(dto.activeSegmentEndPosition.y) > 0.0001f ||
                     Mathf.Abs(dto.activeSegmentEndPosition.z) > 0.0001f))
                {
                    target.activeSegmentEndPosition = new Vector3(
                        dto.activeSegmentEndPosition.x,
                        dto.activeSegmentEndPosition.y,
                        dto.activeSegmentEndPosition.z);
                    target.hasActiveSegmentEnd = true;
                }
                else
                {
                    target.activeSegmentEndPosition = target.targetPosition;
                    target.hasActiveSegmentEnd = false;
                }
                target.patrolDirection = string.IsNullOrEmpty(dto.patrolDirection) ? "none" : dto.patrolDirection;
                target.patrolCycleIndex = dto.patrolCycleIndex;
                target.isSuspendedByPlayerInteraction = dto.isSuspendedByPlayerInteraction;
                target.passingEncounterPartnerNpcId = dto.passingEncounterPartnerNpcId ?? "";
                target.passingEncounterType = dto.passingEncounterType ?? "";
                target.businessDomainId = dto.businessDomainId ?? "";
                target.withinAuthorizedDomain = dto.withinAuthorizedDomain;
                target.hasValidTarget = true;

                if (target.point != null)
                {
                    target.point.behaviorProfileType = target.behaviorProfileType;
                    target.point.autonomousBehaviorState = target.autonomousBehaviorState;
                    target.point.movementControlMode = target.controlMode;
                    target.point.patrolDirection = target.patrolDirection;
                    target.point.patrolCycleIndex = target.patrolCycleIndex;
                    target.point.isSuspendedByPlayerInteraction = target.isSuspendedByPlayerInteraction;
                    target.point.passingEncounterPartnerNpcId = target.passingEncounterPartnerNpcId;
                    target.point.passingEncounterType = target.passingEncounterType;
                    target.point.businessDomainId = target.businessDomainId;
                    target.point.withinAuthorizedDomain = target.withinAuthorizedDomain;
                    if (!string.IsNullOrEmpty(dto.activityState))
                    {
                        target.point.activityState = dto.activityState;
                    }
                    if (!string.IsNullOrEmpty(dto.actorArchetype))
                    {
                        target.point.actorArchetype = dto.actorArchetype;
                        target.point.isWitness = dto.isWitness;
                        target.point.witnessClassification = string.IsNullOrEmpty(dto.witnessClassification) ? "none" : dto.witnessClassification;
                        target.point.witnessId = dto.witnessId ?? "";
                        target.point.hasEmploymentSchedule = !string.Equals(dto.actorArchetype, "community_player_proxy", StringComparison.OrdinalIgnoreCase);
                        target.point.canEnterOffDuty = target.point.hasEmploymentSchedule;
                    }
                }

                if (snapImmediately && target.point != null)
                {
                    SnapSingleTarget(target);
                }
                else if (target.animationController != null && target.point != null)
                {
                    target.animationController.SyncPresentationInputs(
                        target.movementState,
                        target.point.interactionAvailability,
                        target.point.interactionMode,
                        target.point.activityState,
                        target.currentWaypointId,
                        target.point.canInteract,
                        target.visualSpeedMps,
                        target.point.actorArchetype);
                    target.animationController.SyncAutonomousBehavior(
                        target.behaviorProfileType,
                        target.autonomousBehaviorState,
                        target.controlMode,
                        target.patrolDirection,
                        target.isSuspendedByPlayerInteraction,
                        target.passingEncounterPartnerNpcId,
                        target.passingEncounterType);
                }
            }
        }

        /// <summary>
        /// Immediately snaps all managed NPC transforms to their current target positions.
        /// </summary>
        public void SnapToTargetPositions()
        {
            foreach (var kvp in managedNpcs)
            {
                NpcPresentationTarget target = kvp.Value;
                if (target == null || target.point == null || !target.hasValidTarget) continue;

                SnapSingleTarget(target);
            }
        }

        private static void SnapSingleTarget(NpcPresentationTarget target)
        {
            bool isAtPrimaryStation =
                string.IsNullOrEmpty(target.currentWaypointId) ||
                (!string.IsNullOrEmpty(target.primaryWaypointId) &&
                 string.Equals(target.currentWaypointId, target.primaryWaypointId, StringComparison.OrdinalIgnoreCase) &&
                 target.movementState != "moving" &&
                 target.progression <= 0.001f);

            bool isFixedBorderKiosk = target.point.gameObject.name == "Border_Information_Kiosk";
            bool isFixedCitizenKiosk = target.point.gameObject.name == "CitizenServiceDesk";

            if (isFixedBorderKiosk)
            {
                target.point.transform.position = new Vector3(19.20f, 0.20f, 39.50f);
                target.point.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else if (isFixedCitizenKiosk)
            {
                target.point.transform.position = new Vector3(-10.80f, 0.0f, 18.55f);
                target.point.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                Vector3 snappedPos = target.targetPosition;
                snappedPos.y = ResolveGroundedSurfaceY(snappedPos);
                target.point.transform.position = snappedPos;
                if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                {
                    Vector3 flatHeading = new Vector3(target.targetHeading.x, 0, target.targetHeading.z).normalized;
                    if (flatHeading.sqrMagnitude > 0.001f)
                    {
                        target.point.transform.rotation = Quaternion.LookRotation(flatHeading, Vector3.up);
                    }
                }
            }

            if (target.visualTransform != null)
            {
                if ((isFixedBorderKiosk || isFixedCitizenKiosk) && target.visualTransform != target.point.transform)
                {
                    if (isAtPrimaryStation)
                    {
                        target.visualTransform.localPosition = target.initialVisualLocalPos;
                        target.visualTransform.localRotation = target.initialVisualLocalRot;
                    }
                    else
                    {
                        target.visualTransform.position = target.targetPosition;
                        if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                        {
                            Vector3 flatHeading = new Vector3(target.targetHeading.x, 0f, target.targetHeading.z).normalized;
                            if (flatHeading.sqrMagnitude > 0.001f)
                            {
                                target.visualTransform.rotation = Quaternion.LookRotation(flatHeading, Vector3.up);
                            }
                        }
                    }
                }
                else if (target.isVisualDetachedSibling)
                {
                    if (isAtPrimaryStation)
                    {
                        target.visualTransform.position = target.initialVisualWorldPos;
                        target.visualTransform.rotation = target.initialVisualWorldRot;
                    }
                    else
                    {
                        Vector3 visSnap = target.targetPosition;
                        visSnap.y = ResolveGroundedSurfaceY(visSnap);
                        target.visualTransform.position = visSnap;
                        if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                        {
                            Vector3 flatHeading = new Vector3(target.targetHeading.x, 0f, target.targetHeading.z).normalized;
                            if (flatHeading.sqrMagnitude > 0.001f)
                            {
                                target.visualTransform.rotation = Quaternion.LookRotation(flatHeading, Vector3.up);
                            }
                        }
                    }
                }

                target.lastVisualWorldPos = target.visualTransform.position;
                target.hasLastVisualPos = true;
            }

            target.point.movementState = target.movementState;
            target.point.currentWaypointId = target.currentWaypointId;
            target.point.destinationWaypointId = target.destinationWaypointId;
            target.point.activeRouteId = target.activeRouteId;
            target.point.progression = target.progression;

            float nominalSpeed = target.movementState == "moving"
                ? (target.authoritativeSpeedMps > 0.05f ? target.authoritativeSpeedMps : 2.5f)
                : 0f;
            target.visualSpeedMps = nominalSpeed;

            if (target.animationController != null)
            {
                target.animationController.SyncPresentationInputs(
                    target.movementState,
                    target.point.interactionAvailability,
                    target.point.interactionMode,
                    target.point.activityState,
                    target.currentWaypointId,
                    target.point.canInteract,
                    nominalSpeed,
                    target.point.actorArchetype);
                target.animationController.SyncAutonomousBehavior(
                    target.behaviorProfileType,
                    target.autonomousBehaviorState,
                    target.controlMode,
                    target.patrolDirection,
                    target.isSuspendedByPlayerInteraction,
                    target.passingEncounterPartnerNpcId,
                    target.passingEncounterType);
            }
        }

        // Public inspection methods for verification suites, HUD, and animation coordination
        public Vector3 GetNpcPosition(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target.point != null)
            {
                bool isFixedBooth = target.point.gameObject.name == "Border_Information_Kiosk" ||
                                    target.point.gameObject.name == "CitizenServiceDesk";
                bool isAtPrimary =
                    string.IsNullOrEmpty(target.currentWaypointId) ||
                    (!string.IsNullOrEmpty(target.primaryWaypointId) &&
                     string.Equals(target.currentWaypointId, target.primaryWaypointId, StringComparison.OrdinalIgnoreCase) &&
                     target.movementState != "moving" &&
                     target.progression <= 0.001f);

                if (isFixedBooth && !isAtPrimary && target.visualTransform != null)
                {
                    return target.visualTransform.position;
                }
                return target.point.transform.position;
            }
            return Vector3.zero;
        }

        public string GetNpcMovementState(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target))
            {
                return target.movementState;
            }
            return "unknown";
        }

        public float GetNpcProgression(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target))
            {
                return target.progression;
            }
            return 0f;
        }

        public float GetVisualMoveSpeed(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target))
            {
                return target.visualSpeedMps;
            }
            return 0f;
        }

        public bool IsNpcVisuallyMoving(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target.animationController != null)
            {
                return target.animationController.IsWalkingActive;
            }
            return false;
        }

        public Transform GetNpcVisualTransform(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target))
            {
                return target.visualTransform;
            }
            return null;
        }

        public NpcAnimationController GetNpcAnimationController(string characterId)
        {
            if (animationControllersByNpcId.TryGetValue(characterId, out NpcAnimationController animCtrl))
            {
                return animCtrl;
            }
            return null;
        }

        public void TriggerNpcGreeting(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (animationControllersByNpcId.TryGetValue(characterId, out NpcAnimationController animCtrl) && animCtrl != null)
            {
                animCtrl.TriggerGreetingCue();
            }
        }

        /// <summary>
        /// Applies authoritative interaction-context and embedded social awareness from a single aggregate roster response.
        /// </summary>
        public void ApplyAuthoritativeInteractionContextRoster(NpcRosterInteractionContextResponseDto dto)
        {
            if (dto == null || !dto.success || dto.npcsList == null)
            {
                ClearAllSocialAwareness();
                ClearAllSocialInteractions();
                return;
            }

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            foreach (var ctx in dto.npcsList)
            {
                if (ctx == null || string.IsNullOrEmpty(ctx.npcId)) continue;
                if (managedNpcs.TryGetValue(ctx.npcId, out NpcPresentationTarget target) && target != null && target.point != null)
                {
                    target.point.interactionAvailability = ctx.interactionAvailability ?? "available_normal";
                    target.point.interactionMode = ctx.interactionMode ?? "normal_service";
                    target.point.isAvailableForNormalService = ctx.isAvailableForNormalService;
                    target.point.canInteract = ctx.canInitiateConversation;
                    if (!string.IsNullOrEmpty(ctx.activityState))
                    {
                        target.point.activityState = ctx.activityState;
                    }
                    if (!string.IsNullOrEmpty(ctx.actorArchetype))
                    {
                        target.point.actorArchetype = ctx.actorArchetype;
                        target.point.hasEmploymentSchedule = ctx.hasEmploymentSchedule;
                        target.point.canEnterOffDuty = ctx.canEnterOffDuty;
                        target.point.isWitness = ctx.isWitness;
                        target.point.witnessClassification = string.IsNullOrEmpty(ctx.witnessClassification) ? "none" : ctx.witnessClassification;
                        target.point.witnessId = ctx.witnessId ?? "";
                    }
                }

                if (ctx.socialAwareness != null)
                {
                    ApplySingleNpcSocialAwareness(ctx.socialAwareness);
                }

                if (ctx.socialInteraction != null)
                {
                    ApplySingleNpcSocialInteraction(ctx.socialInteraction);
                }
            }
        }

        /// <summary>
        /// Applies an aggregate authoritative social-awareness roster response across all canonical NPCs.
        /// </summary>
        public void ApplyAuthoritativeSocialAwarenessRoster(NpcRosterSocialAwarenessResponseDto dto)
        {
            if (dto == null || !dto.success || dto.npcsList == null)
            {
                ClearAllSocialAwareness();
                return;
            }

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            foreach (var awareness in dto.npcsList)
            {
                ApplySingleNpcSocialAwareness(awareness);
            }
        }

        /// <summary>
        /// Applies authoritative social-awareness state to an individual managed NPC without altering movement or root transforms.
        /// </summary>
        public void ApplySingleNpcSocialAwareness(NpcSocialAwarenessDto awareness)
        {
            if (awareness == null || string.IsNullOrEmpty(awareness.npcId)) return;

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            if (!managedNpcs.TryGetValue(awareness.npcId, out NpcPresentationTarget target) || target == null)
            {
                return;
            }

            Vector2 dirXZ = Vector2.zero;
            float distMeters = -1f;
            if (awareness.spatialRelation != null)
            {
                distMeters = awareness.spatialRelation.horizontalDistanceMeters;
                if (awareness.spatialRelation.directionToTarget != null)
                {
                    dirXZ = new Vector2(
                        awareness.spatialRelation.directionToTarget.x,
                        awareness.spatialRelation.directionToTarget.z);
                }
            }

            if (target.point != null)
            {
                if (!string.IsNullOrEmpty(awareness.actorArchetype))
                {
                    target.point.actorArchetype = awareness.actorArchetype;
                    target.point.hasEmploymentSchedule = !string.Equals(awareness.actorArchetype, "community_player_proxy", StringComparison.OrdinalIgnoreCase);
                    target.point.canEnterOffDuty = target.point.hasEmploymentSchedule;
                    target.point.isWitness = awareness.isWitness;
                    target.point.witnessClassification = string.IsNullOrEmpty(awareness.witnessClassification) ? "none" : awareness.witnessClassification;
                    target.point.witnessId = awareness.witnessId ?? "";
                }
                target.point.socialAwarenessState = string.IsNullOrEmpty(awareness.awarenessState) ? "unaware" : awareness.awarenessState;
                target.point.socialTargetEntityType = string.IsNullOrEmpty(awareness.targetEntityType) ? "none" : awareness.targetEntityType;
                target.point.socialTargetEntityId = awareness.targetEntityId ?? "";
                target.point.socialPairedPartnerNpcId = awareness.pairedPartnerNpcId ?? "";
                target.point.socialTargetHorizontalDistanceMeters = distMeters;
                target.point.socialTargetDirectionXZ = dirXZ;
                target.point.socialCanOrientTowardTarget = awareness.canOrientTowardTarget;
                target.point.socialRetainsContextualActivity = awareness.retainsContextualActivity;
                target.point.socialEligible = awareness.socialEligible;
                target.point.socialAwarenessAvailable = true;
            }

            Transform targetTransform = null;
            if (string.Equals(awareness.targetEntityType, "npc", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(awareness.targetEntityId) &&
                managedNpcs.TryGetValue(awareness.targetEntityId, out NpcPresentationTarget partnerTarget) &&
                partnerTarget != null)
            {
                targetTransform = partnerTarget.visualTransform != null
                    ? partnerTarget.visualTransform
                    : (partnerTarget.point != null ? partnerTarget.point.transform : null);
            }

            if (target.animationController != null)
            {
                target.animationController.SyncSocialAwareness(
                    awareness.awarenessState,
                    awareness.targetEntityType,
                    awareness.targetEntityId,
                    dirXZ,
                    distMeters,
                    awareness.canOrientTowardTarget,
                    awareness.retainsContextualActivity,
                    targetTransform);
            }
        }

        /// <summary>
        /// Clears all social-awareness presentation state safely when backend data is missing or unavailable.
        /// </summary>
        public void ClearAllSocialAwareness()
        {
            foreach (var kvp in managedNpcs)
            {
                NpcPresentationTarget target = kvp.Value;
                if (target == null) continue;

                if (target.point != null)
                {
                    target.point.socialAwarenessState = "unaware";
                    target.point.socialTargetEntityType = "none";
                    target.point.socialTargetEntityId = "";
                    target.point.socialPairedPartnerNpcId = "";
                    target.point.socialTargetHorizontalDistanceMeters = -1f;
                    target.point.socialTargetDirectionXZ = Vector2.zero;
                    target.point.socialCanOrientTowardTarget = false;
                    target.point.socialRetainsContextualActivity = false;
                    target.point.socialEligible = false;
                    target.point.socialAwarenessAvailable = false;
                }

                if (target.animationController != null)
                {
                    target.animationController.ClearSocialAwareness();
                }
            }
        }

        public NpcVisualAwarenessState GetNpcAwarenessState(string characterId)
        {
            if (animationControllersByNpcId.TryGetValue(characterId, out NpcAnimationController animCtrl) && animCtrl != null)
            {
                return animCtrl.CurrentAwarenessState;
            }
            return NpcVisualAwarenessState.Unaware;
        }

        /// <summary>
        /// Applies an aggregate authoritative social-interactions lifecycle roster response (Increment 5.21.0).
        /// </summary>
        public void ApplyAuthoritativeSocialInteractionsRoster(NpcRosterSocialInteractionsResponseDto dto)
        {
            if (dto == null || !dto.success || dto.npcsList == null)
            {
                ClearAllSocialInteractions();
                return;
            }

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            foreach (var interaction in dto.npcsList)
            {
                ApplySingleNpcSocialInteraction(interaction);
            }
        }

        /// <summary>
        /// Applies authoritative social interaction lifecycle state to an individual NPC without altering movement or transforms.
        /// </summary>
        public void ApplySingleNpcSocialInteraction(NpcSocialInteractionParticipationDto interaction)
        {
            if (interaction == null || string.IsNullOrEmpty(interaction.npcId)) return;

            if (managedNpcs.Count == 0)
            {
                DiscoverSceneReferences();
            }

            if (!managedNpcs.TryGetValue(interaction.npcId, out NpcPresentationTarget target) || target == null)
            {
                return;
            }

            if (target.point != null)
            {
                target.point.socialInteractionSessionId = interaction.sessionId ?? "";
                target.point.socialInteractionPhase = interaction.phase ?? "";
                target.point.socialInteractionType = interaction.interactionType ?? "";
                target.point.socialInteractionPartnerNpcId = interaction.partnerNpcId ?? "";
                target.point.socialInteractionConversationalRole = string.IsNullOrEmpty(interaction.conversationalRole) ? "none" : interaction.conversationalRole;
                target.point.socialInteractionTurnIndex = interaction.turnIndex;
                target.point.socialInteractionStartedAt = interaction.sessionStartedAt ?? "";
                target.point.socialInteractionExpectedEndAt = interaction.sessionExpectedEndAt ?? "";
                target.point.socialInteractionCooldownUntil = interaction.cooldownUntil ?? "";
                target.point.socialInteractionInCooldown = interaction.inCooldown;
                target.point.socialInteractionActive = interaction.inActiveInteraction;
                target.point.socialInteractionAvailable = true;
            }

            Transform partnerTransform = null;
            if (interaction.inActiveInteraction &&
                !string.IsNullOrEmpty(interaction.partnerNpcId) &&
                managedNpcs.TryGetValue(interaction.partnerNpcId, out NpcPresentationTarget partnerTarget) &&
                partnerTarget != null)
            {
                partnerTransform = partnerTarget.visualTransform != null
                    ? partnerTarget.visualTransform
                    : (partnerTarget.point != null ? partnerTarget.point.transform : null);
            }

            if (target.animationController != null)
            {
                target.animationController.SyncSocialInteraction(
                    interaction.sessionId,
                    interaction.phase,
                    interaction.conversationalRole,
                    interaction.turnIndex,
                    interaction.partnerNpcId,
                    interaction.inActiveInteraction,
                    partnerTransform);
            }
        }

        /// <summary>
        /// Clears all social interaction lifecycle presentation state safely without mutating world positions.
        /// </summary>
        public void ClearAllSocialInteractions()
        {
            foreach (var kvp in managedNpcs)
            {
                NpcPresentationTarget target = kvp.Value;
                if (target == null) continue;

                if (target.point != null)
                {
                    target.point.socialInteractionSessionId = "";
                    target.point.socialInteractionPhase = "";
                    target.point.socialInteractionType = "";
                    target.point.socialInteractionPartnerNpcId = "";
                    target.point.socialInteractionConversationalRole = "none";
                    target.point.socialInteractionTurnIndex = 0;
                    target.point.socialInteractionStartedAt = "";
                    target.point.socialInteractionExpectedEndAt = "";
                    target.point.socialInteractionCooldownUntil = "";
                    target.point.socialInteractionInCooldown = false;
                    target.point.socialInteractionActive = false;
                    target.point.socialInteractionAvailable = false;
                }

                if (target.animationController != null)
                {
                    target.animationController.ClearSocialInteraction();
                }
            }
        }

        public NpcVisualSocialInteractionPhase GetNpcSocialInteractionPhase(string characterId)
        {
            if (animationControllersByNpcId.TryGetValue(characterId, out NpcAnimationController animCtrl) && animCtrl != null)
            {
                return animCtrl.CurrentSocialInteractionPhase;
            }
            return NpcVisualSocialInteractionPhase.None;
        }

        public string GetNpcAutonomousBehaviorState(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target != null)
            {
                return target.autonomousBehaviorState;
            }
            return "";
        }

        public string GetNpcBehaviorProfileType(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target != null)
            {
                return target.behaviorProfileType;
            }
            return "";
        }

        public string GetNpcPatrolDirection(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target != null)
            {
                return target.patrolDirection;
            }
            return "none";
        }

        public bool IsNpcSuspendedByPlayerInteraction(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target != null)
            {
                return target.isSuspendedByPlayerInteraction;
            }
            return false;
        }
    }
}
