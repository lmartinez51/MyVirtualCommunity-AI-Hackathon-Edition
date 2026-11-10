using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public float pollIntervalSeconds = 2.0f;

        [Header("Presentation Interpolation")]
        [Tooltip("Interpolation speed in meters per second.")]
        public float positionLerpSpeed = 3.5f;

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
            public Vector3 targetPosition;
            public Vector3 targetHeading;
            public string movementState = "stationary";
            public string currentWaypointId = "";
            public string destinationWaypointId = "";
            public string activeRouteId = "";
            public float progression = 0f;
            public bool hasValidTarget = false;
        }

        private readonly Dictionary<string, NpcPresentationTarget> managedNpcs = new Dictionary<string, NpcPresentationTarget>(StringComparer.OrdinalIgnoreCase);
        private Coroutine pollCoroutine = null;

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
        /// Discovers all NPCs in the scene by inspecting InteractionPoint components.
        /// </summary>
        public void DiscoverSceneReferences()
        {
            managedNpcs.Clear();
            InteractionPoint[] points = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            foreach (var pt in points)
            {
                if (pt != null && !string.IsNullOrEmpty(pt.characterId))
                {
                    if (!managedNpcs.ContainsKey(pt.characterId))
                    {
                        managedNpcs[pt.characterId] = new NpcPresentationTarget
                        {
                            point = pt,
                            targetPosition = pt.transform.position,
                            targetHeading = pt.transform.forward,
                            movementState = pt.movementState ?? "stationary",
                            currentWaypointId = pt.currentWaypointId ?? "",
                            destinationWaypointId = pt.destinationWaypointId ?? "",
                            activeRouteId = pt.activeRouteId ?? "",
                            progression = pt.progression,
                            hasValidTarget = true
                        };
                    }
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
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
                bool isFixedBorderKiosk = (t.gameObject.name == "Border_Information_Kiosk");
                bool isFixedCitizenKiosk = (t.gameObject.name == "CitizenServiceDesk");

                // Position interpolation (keep fixed physical kiosk booth anchored at its plaza foundation)
                if (isFixedBorderKiosk)
                {
                    t.position = new Vector3(19.20f, 0.20f, 39.50f);
                    t.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else if (isFixedCitizenKiosk && target.movementState != "moving")
                {
                    t.position = new Vector3(-10.80f, 0.0f, 18.55f);
                    t.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    float step = positionLerpSpeed * dt;
                    t.position = Vector3.MoveTowards(t.position, target.targetPosition, step);

                    // Heading rotation when moving
                    if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                    {
                        Vector3 flatHeading = new Vector3(target.targetHeading.x, 0, target.targetHeading.z).normalized;
                        if (flatHeading.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(flatHeading, Vector3.up);
                            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotationLerpSpeed * dt);
                        }
                    }
                }

                // Keep InteractionPoint fields synchronized for HUD and nameplates
                target.point.movementState = target.movementState;
                target.point.currentWaypointId = target.currentWaypointId;
                target.point.destinationWaypointId = target.destinationWaypointId;
                target.point.activeRouteId = target.activeRouteId;
                target.point.progression = target.progression;
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
                target.hasValidTarget = true;

                if (snapImmediately && target.point != null)
                {
                    target.point.transform.position = target.targetPosition;
                    if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                    {
                        Vector3 flatHeading = new Vector3(target.targetHeading.x, 0, target.targetHeading.z).normalized;
                        if (flatHeading.sqrMagnitude > 0.001f)
                        {
                            target.point.transform.rotation = Quaternion.LookRotation(flatHeading, Vector3.up);
                        }
                    }
                    target.point.movementState = target.movementState;
                    target.point.currentWaypointId = target.currentWaypointId;
                    target.point.destinationWaypointId = target.destinationWaypointId;
                    target.point.activeRouteId = target.activeRouteId;
                    target.point.progression = target.progression;
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

                target.point.transform.position = target.targetPosition;
                if (target.movementState == "moving" && target.targetHeading.sqrMagnitude > 0.001f)
                {
                    Vector3 flatHeading = new Vector3(target.targetHeading.x, 0, target.targetHeading.z).normalized;
                    if (flatHeading.sqrMagnitude > 0.001f)
                    {
                        target.point.transform.rotation = Quaternion.LookRotation(flatHeading, Vector3.up);
                    }
                }
                target.point.movementState = target.movementState;
                target.point.currentWaypointId = target.currentWaypointId;
                target.point.destinationWaypointId = target.destinationWaypointId;
                target.point.activeRouteId = target.activeRouteId;
                target.point.progression = target.progression;
            }
        }

        // Public inspection methods for verification suites and HUD
        public Vector3 GetNpcPosition(string characterId)
        {
            if (managedNpcs.TryGetValue(characterId, out NpcPresentationTarget target) && target.point != null)
            {
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
    }
}
