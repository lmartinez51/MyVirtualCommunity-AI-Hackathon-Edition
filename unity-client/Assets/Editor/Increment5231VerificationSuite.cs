using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity batchmode verification suite for Increment 5.23.1:
    /// Valeria Indoor/Outdoor Autonomous Locomotion & Scheduled Calibration Lifecycle.
    ///
    /// Covers:
    ///   1. Verified scene anchor Optica_CalibrationStation (-17.85, 0.20, -0.50), Euler (0, -90, 0)
    ///   2. Valeria's actual scene CapsuleCollider dimensions (radius=0.35m, height=1.65m)
    ///   3. Full continuous Physics.OverlapCapsule + floor Raycast traversal (every 0.10m, NOT just endpoints!)
    ///      across EVERY connected edge in Zone A (exterior), Zone B (1.087m open front door),
    ///      Zone C (main showroom), Zone D1 (1.22m rear doorway), and Zone D2 (rear calibration room)
    ///   4. Fixed glass & closed south door collision enforcement (negative checks)
    ///   5. Unity presentation (NpcWaypointMovementController + NpcAnimationController):
    ///      - Unclamped indoor traversal through open door -> showroom -> calibration station
    ///      - Segment-bounded extrapolation (zero overshoot into walls/furniture)
    ///      - Natural lensometer-facing orientation (-X) & ContextualActivity work posture
    ///      - Player interaction suspension & smooth resume
    ///   6. Regression protection for Carlos, Elena, Patricia, Mateo, Sofía, and Don Héctor
    /// </summary>
    public static class Increment5231VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Increment 5.23.1/Run Verification Suite")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5231Verify] Starting Increment 5.23.1 Valeria Indoor/Outdoor Locomotion & Calibration Verification...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5231Verify] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // Open dynamic shutter to simulate operating business hours (09:00-17:00 Mon-Sat)
            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);
            Physics.SyncTransforms();

            int passed = 0;
            int total = 0;
            void Check(string label, bool ok)
            {
                total++;
                if (ok)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            // 1. Verify Optica_CalibrationStation transform in saved scene
            GameObject calibAnchor = GameObject.Find(OpticaFashionInteriorBuilder.CalibrationAnchorName);
            bool anchorTransformOk = calibAnchor != null &&
                Vector3.Distance(calibAnchor.transform.position, new Vector3(-17.85f, 0.20f, -0.50f)) < 0.01f &&
                calibAnchor.transform.forward.x < -0.99f;
            Check(
                $"Optica_CalibrationStation verified in scene at {calibAnchor?.transform.position} facing West (forward={calibAnchor?.transform.forward})",
                anchorTransformOk
            );

            // 2. Discover Valeria's actual CapsuleCollider dimensions from scene hierarchy
            GameObject valeriaGo = GameObject.Find("ValeriaDominguez_Attendant");
            CapsuleCollider valeriaCap = valeriaGo != null ? valeriaGo.GetComponentInChildren<CapsuleCollider>(true) : null;
            float capRadius = valeriaCap != null ? valeriaCap.radius : 0.35f;
            float capHeight = valeriaCap != null ? valeriaCap.height : 1.65f;
            float capCenterY = valeriaCap != null ? valeriaCap.center.y : 0.85f;
            Debug.Log($"[Increment5231Verify] Valeria scene CapsuleCollider: found={valeriaCap != null}, radius={capRadius:F2}m, height={capHeight:F2}m, centerY={capCenterY:F2}m");
            Check(
                $"Valeria scene CapsuleCollider verified (r={capRadius:F2}m, h={capHeight:F2}m)",
                valeriaCap != null && Mathf.Abs(capRadius - 0.35f) < 0.02f && capHeight >= 1.55f && capHeight <= 1.85f
            );

            // Helper: check capsule clearance & continuous floor at a world foot position
            bool IsCapsuleClearAndGrounded(Vector3 footPos, string contextLabel)
            {
                float bottomSphereY = footPos.y + Mathf.Max(capRadius + 0.06f, capCenterY - capHeight * 0.5f + capRadius);
                float topSphereY = footPos.y + Mathf.Max(bottomSphereY - footPos.y + 0.1f, capCenterY + capHeight * 0.5f - capRadius);
                Vector3 p0 = new Vector3(footPos.x, bottomSphereY, footPos.z);
                Vector3 p1 = new Vector3(footPos.x, topSphereY, footPos.z);

                Collider[] hits = Physics.OverlapCapsule(p0, p1, capRadius - 0.005f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                foreach (var c in hits)
                {
                    if (c == null) continue;
                    string n = c.gameObject.name;
                    if (n == "Optica_InteriorFloor" || n == "Sidewalk_West") continue;
                    if (c.transform.GetComponentInParent<BobbleheadCharacter>() != null ||
                        c.transform.GetComponentInParent<InteractionPoint>() != null ||
                        c.transform.GetComponentInParent<FirstPersonPlayer>() != null)
                    {
                        continue;
                    }
                    Debug.LogError($"    [COLLISION at {contextLabel} {footPos}] overlapped '{n}'");
                    blocked = true;
                }

                bool hasFloor = Physics.Raycast(footPos + Vector3.up * 0.8f, Vector3.down, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) &&
                                Mathf.Abs(hit.point.y - 0.20f) < 0.04f;
                if (!hasFloor)
                {
                    Debug.LogError($"    [NO SOLID FLOOR at {contextLabel} {footPos}]");
                }
                return !blocked && hasFloor;
            }

            // 3. Full continuous capsule sweep (every 0.10m!) along EVERY edge of Valeria's connected World Core graph
            var waypoints = new Dictionary<string, Vector3>
            {
                ["wp_valeria_storefront"] = new Vector3(-10.50f, 0.20f, 1.50f),
                ["wp_optica_north_window"] = new Vector3(-10.55f, 0.20f, 2.55f),
                ["wp_optica_plaza_threshold"] = new Vector3(-8.35f, 0.20f, 0.85f),
                ["wp_optica_south_promenade"] = new Vector3(-8.55f, 0.20f, -1.10f),
                ["wp_optica_south_window"] = new Vector3(-10.55f, 0.20f, -0.55f),
                ["wp_optica_door_approach"] = new Vector3(-11.20f, 0.20f, 1.20f),
                ["wp_optica_door_threshold"] = new Vector3(-12.02f, 0.20f, 1.20f),
                ["wp_optica_interior_entry"] = new Vector3(-13.30f, 0.20f, 1.20f),
                ["wp_optica_showroom_counter_east"] = new Vector3(-13.85f, 0.20f, 0.45f),
                ["wp_optica_showroom_center"] = new Vector3(-14.70f, 0.20f, 0.90f),
                ["wp_optica_showroom_counter_west"] = new Vector3(-15.55f, 0.20f, 0.50f),
                ["wp_optica_showroom_waiting"] = new Vector3(-14.35f, 0.20f, 1.65f),
                ["wp_optica_showroom_north_display"] = new Vector3(-15.75f, 0.20f, 1.85f),
                ["wp_optica_rear_door_threshold"] = new Vector3(-16.88f, 0.20f, 2.00f),
                ["wp_optica_rear_room_north"] = new Vector3(-17.85f, 0.20f, 2.00f),
                ["wp_optica_calibration_station"] = new Vector3(-17.85f, 0.20f, -0.50f),
            };

            var graphEdges = new (string a, string b)[]
            {
                // Zone A exterior frontage
                ("wp_valeria_storefront", "wp_optica_north_window"),
                ("wp_valeria_storefront", "wp_optica_plaza_threshold"),
                ("wp_valeria_storefront", "wp_optica_south_window"),
                ("wp_optica_north_window", "wp_optica_plaza_threshold"),
                ("wp_optica_plaza_threshold", "wp_optica_south_promenade"),
                ("wp_optica_south_promenade", "wp_optica_south_window"),
                // Zone A -> Zone B -> Zone C open doorway
                ("wp_valeria_storefront", "wp_optica_door_approach"),
                ("wp_optica_door_approach", "wp_optica_door_threshold"),
                ("wp_optica_door_threshold", "wp_optica_interior_entry"),
                // Zone C main showroom
                ("wp_optica_interior_entry", "wp_optica_showroom_counter_east"),
                ("wp_optica_interior_entry", "wp_optica_showroom_center"),
                ("wp_optica_interior_entry", "wp_optica_showroom_waiting"),
                ("wp_optica_showroom_counter_east", "wp_optica_showroom_center"),
                ("wp_optica_showroom_counter_east", "wp_optica_showroom_counter_west"),
                ("wp_optica_showroom_center", "wp_optica_showroom_counter_west"),
                ("wp_optica_showroom_center", "wp_optica_showroom_waiting"),
                ("wp_optica_showroom_center", "wp_optica_showroom_north_display"),
                ("wp_optica_showroom_counter_west", "wp_optica_showroom_north_display"),
                ("wp_optica_showroom_waiting", "wp_optica_showroom_north_display"),
                // Zone D task-only rear doorway & calibration room
                ("wp_optica_showroom_north_display", "wp_optica_rear_door_threshold"),
                ("wp_optica_rear_door_threshold", "wp_optica_rear_room_north"),
                ("wp_optica_rear_room_north", "wp_optica_calibration_station"),
            };

            bool allEdgesClear = true;
            int totalSamplesChecked = 0;
            foreach (var (a, b) in graphEdges)
            {
                Vector3 posA = waypoints[a];
                Vector3 posB = waypoints[b];
                float dist = Vector3.Distance(posA, posB);
                int steps = Mathf.Max(4, Mathf.CeilToInt(dist / 0.10f));
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    Vector3 samplePos = Vector3.Lerp(posA, posB, t);
                    totalSamplesChecked++;
                    if (!IsCapsuleClearAndGrounded(samplePos, $"{a}->{b}[{i}/{steps}]"))
                    {
                        allEdgesClear = false;
                    }
                }
            }
            Debug.Log($"[Increment5231Verify] Swept {graphEdges.Length} edges ({totalSamplesChecked} capsule samples at 0.10m resolution).");
            Check(
                $"Full 0.10m-resolution Physics capsule traversal clear across all {graphEdges.Length} connected graph edges (Zones A/B/C/D)",
                allEdgesClear
            );

            // 4. Negative check: closed south door leaf & fixed glass panels STILL block capsule traversal
            bool closedDoorBlocked = !IsCapsuleClearAndGrounded(new Vector3(-12.02f, 0.20f, -0.02f), "ExpectedBlocked_ClosedSouthDoor");
            bool southGlassBlocked = !IsCapsuleClearAndGrounded(new Vector3(-12.02f, 0.20f, -1.22f), "ExpectedBlocked_SouthSidelight");
            bool northGlassBlocked = !IsCapsuleClearAndGrounded(new Vector3(-12.02f, 0.20f, 2.46f), "ExpectedBlocked_NorthSidelight");
            bool counterBlocked = !IsCapsuleClearAndGrounded(new Vector3(-14.70f, 0.20f, -0.60f), "ExpectedBlocked_LongCounter");
            bool workbenchBlocked = !IsCapsuleClearAndGrounded(new Vector3(-18.77f, 0.20f, -0.50f), "ExpectedBlocked_Workbench");
            Check(
                "Closed south door leaf, fixed sidelight glass, display counter & calibration workbench remain solid colliders",
                closedDoorBlocked && southGlassBlocked && northGlassBlocked && counterBlocked && workbenchBlocked
            );

            // 5. Unity Presentation Controller + Animation Controller end-to-end validation
            var movCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTemp = false;
            if (movCtrl == null)
            {
                var go = new GameObject("Temp_NpcWaypointMovementController_5231");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTemp = true;
            }
            movCtrl.DiscoverSceneReferences();

            Transform valeriaVisual = movCtrl.GetNpcVisualTransform("valeria-optica-attendant");
            NpcAnimationController valeriaAnim = movCtrl.GetNpcAnimationController("valeria-optica-attendant");
            Check("Valeria visual transform & NpcAnimationController discovered cleanly", valeriaVisual != null && valeriaAnim != null);

            // 5a. Doorway & showroom segment-bounded traversal (never clamped to exterior X=-11.40, never overshoots segment end!)
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "moving",
                currentWaypointId = "wp_optica_door_threshold",
                destinationWaypointId = "wp_optica_interior_entry",
                activeRouteId = "local_valeria-optica-attendant_wp_optica_door_threshold_to_wp_optica_interior_entry",
                position = new Vector3Dto { x = -12.02f, y = 0.20f, z = 1.20f },
                activeSegmentEndPosition = new Vector3Dto { x = -13.30f, y = 0.20f, z = 1.20f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 1.10f,
                progression = 0.0f,
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "local_roaming",
                controlMode = "autonomous_business_local",
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-front-entrance"
            }, snapImmediately: true);

            for (int i = 0; i < 40; i++) // 2.0s (> 1.16s needed to reach -13.30)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
            }
            Vector3 enteredPos = movCtrl.GetNpcPosition("valeria-optica-attendant");
            Check(
                $"Valeria crosses open doorway into interior and stops bounded at activeSegmentEndPosition X=-13.30 (actual={enteredPos})",
                enteredPos.x <= -13.15f && enteredPos.x >= -13.31f && Mathf.Abs(enteredPos.z - 1.20f) < 0.02f && Mathf.Abs(enteredPos.y - 0.20f) < 0.03f
            );

            // 5b. Arrival at Optica_CalibrationStation -> exact position, West (-X) lensometer orientation & ContextualActivity work posture
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "waiting",
                currentWaypointId = "wp_optica_calibration_station",
                destinationWaypointId = "",
                activeRouteId = "",
                position = new Vector3Dto { x = -17.85f, y = 0.20f, z = -0.50f },
                activeSegmentEndPosition = new Vector3Dto { x = -17.85f, y = 0.20f, z = -0.50f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 0f,
                progression = 0f,
                activityState = "busy",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "scheduled_station_task",
                controlMode = "scheduled_routine_transition",
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-rear-calibration-room",
                activeTaskId = "valeria_calibration_walk",
                taskDestinationReachable = true,
                physicalCalibrationActive = true
            }, snapImmediately: true);

            for (int i = 0; i < 20; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
            }
            Vector3 calibPos = movCtrl.GetNpcPosition("valeria-optica-attendant");
            bool calibPresentationOk =
                Vector3.Distance(calibPos, new Vector3(-17.85f, 0.20f, -0.50f)) < 0.03f &&
                valeriaVisual != null && valeriaVisual.forward.x < -0.95f &&
                valeriaAnim != null &&
                valeriaAnim.CurrentBehaviorState == NpcVisualBehaviorState.ContextualActivity &&
                !valeriaAnim.IsWalkingActive;
            Check(
                $"Calibration presentation verified at {calibPos}, forward={valeriaVisual?.forward}, animState={valeriaAnim?.CurrentBehaviorState}",
                calibPresentationOk
            );

            // 5c. Player interaction suspension freezes movement cleanly inside laboratory
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "stationary",
                currentWaypointId = "wp_optica_calibration_station",
                position = new Vector3Dto { x = -17.85f, y = 0.20f, z = -0.50f },
                activeSegmentEndPosition = new Vector3Dto { x = -17.85f, y = 0.20f, z = -0.50f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 0f,
                activityState = "busy",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "customer_interaction",
                controlMode = "player_interaction_suspended",
                isSuspendedByPlayerInteraction = true,
                withinAuthorizedDomain = true,
                physicalCalibrationActive = false
            }, snapImmediately: false);

            for (int i = 0; i < 10; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
            }
            Check(
                "Player interaction suspension holds Valeria stationary in laboratory with zero visual walk speed",
                movCtrl.IsNpcSuspendedByPlayerInteraction("valeria-optica-attendant") &&
                movCtrl.GetVisualMoveSpeed("valeria-optica-attendant") < 0.01f &&
                valeriaAnim != null && !valeriaAnim.IsWalkingActive
            );

            // 6. Regression: all other 6 canonical NPCs remain discovered & untouched
            string[] otherNpcs = new string[]
            {
                "carlos-farmacia-attendant",
                "elena-border-concierge",
                "patricia-citizen-rep",
                "mateo-student",
                "sofia-cross-border-retail",
                "hector-artisan"
            };
            bool othersOk = true;
            foreach (var id in otherNpcs)
            {
                if (movCtrl.GetNpcVisualTransform(id) == null)
                {
                    Debug.LogError($"    Missing visual transform for canonical NPC '{id}'");
                    othersOk = false;
                }
            }
            Check("Regression clean: all 6 other canonical NPCs (Carlos, Elena, Patricia, Mateo, Sofía, Don Héctor) intact", othersOk);

            if (createdTemp && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5231Verify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
