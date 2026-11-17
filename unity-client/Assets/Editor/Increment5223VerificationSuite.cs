using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Increment 5.22.3 verification suite covering Unity segment-bounded extrapolation,
    /// stale-snapshot freeze, non-backstepping smoothness, and business dwell orientation.
    /// </summary>
    public static class Increment5223VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Increment 5.22.3/Run Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5223Verification] Starting Increment 5.22.3 Business Autonomy & Extrapolation Safety Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5223Verification] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int passed = 0;
            int total = 0;

            void AssertCheck(string name, bool condition)
            {
                total++;
                if (condition)
                {
                    passed++;
                    Debug.Log($"  [PASS] {name}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {name}");
                }
            }

            var movCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTemp = false;
            if (movCtrl == null)
            {
                var go = new GameObject("Temp_NpcWaypointMovementController_5223");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTemp = true;
            }
            movCtrl.DiscoverSceneReferences();

            Transform carlosVisual = movCtrl.GetNpcVisualTransform("carlos-farmacia-attendant");
            Transform valeriaVisual = movCtrl.GetNpcVisualTransform("valeria-optica-attendant");
            AssertCheck("Carlos and Valeria visual transforms discovered", carlosVisual != null && valeriaVisual != null);

            // 1. Segment-end bounded extrapolation: Carlos stops at activeSegmentEndPosition without crossing safe boundary
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "carlos-farmacia-attendant",
                canonicalName = "Carlos Méndez",
                actorArchetype = "business_attendant",
                movementState = "moving",
                currentWaypointId = "wp_carlos_counter",
                destinationWaypointId = "wp_farmacia_display_north",
                activeRouteId = "local_carlos-farmacia-attendant_wp_carlos_counter_to_wp_farmacia_display_north",
                position = new Vector3Dto { x = 10.2f, y = 0.2f, z = 2.2f },
                activeSegmentEndPosition = new Vector3Dto { x = 10.2f, y = 0.2f, z = 3.2f },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                currentSpeedMps = 1.25f,
                progression = 0.65f,
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "local_roaming",
                controlMode = "autonomous_business_local",
                withinAuthorizedDomain = true
            }, snapImmediately: true);

            for (int i = 0; i < 30; i++) // 1.5s without another poll -> enough to reach 3.2f and attempt overshoot if uncapped
            {
                movCtrl.StepVisualMovement(0.05f);
            }
            Vector3 carlosPos = movCtrl.GetNpcPosition("carlos-farmacia-attendant");
            AssertCheck(
                $"Carlos extrapolation bounded to activeSegmentEndPosition Z=3.20 (actual Z={carlosPos.z:F3})",
                carlosPos.z >= 3.10f && carlosPos.z <= 3.205f && carlosPos.x >= 8.40f && carlosPos.x <= 10.85f
            );

            // 2. Non-backstepping smoothness: mid-segment delayed poll behind visual forward position never steps backward
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "carlos-farmacia-attendant",
                movementState = "moving",
                currentWaypointId = "wp_carlos_counter",
                destinationWaypointId = "wp_farmacia_display_north",
                activeRouteId = "local_carlos-farmacia-attendant_wp_carlos_counter_to_wp_farmacia_display_north",
                position = new Vector3Dto { x = 10.2f, y = 0.2f, z = 2.2f },
                activeSegmentEndPosition = new Vector3Dto { x = 10.2f, y = 0.2f, z = 3.2f },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                currentSpeedMps = 1.25f,
                progression = 0.65f,
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "local_roaming",
                withinAuthorizedDomain = true
            }, snapImmediately: true);
            for (int i = 0; i < 12; i++) movCtrl.StepVisualMovement(0.05f); // moves forward ~0.75m to ~2.95m
            float forwardPosZ = movCtrl.GetNpcPosition("carlos-farmacia-attendant").z;

            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "carlos-farmacia-attendant",
                movementState = "moving",
                currentWaypointId = "wp_carlos_counter",
                destinationWaypointId = "wp_farmacia_display_north",
                activeRouteId = "local_carlos-farmacia-attendant_wp_carlos_counter_to_wp_farmacia_display_north",
                position = new Vector3Dto { x = 10.2f, y = 0.2f, z = 2.75f }, // behind current visual ~2.95m
                activeSegmentEndPosition = new Vector3Dto { x = 10.2f, y = 0.2f, z = 3.2f },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                currentSpeedMps = 1.25f,
                progression = 0.85f,
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "local_roaming",
                withinAuthorizedDomain = true
            }, snapImmediately: false);

            float minZObserved = forwardPosZ;
            for (int i = 0; i < 3; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                float curZ = movCtrl.GetNpcPosition("carlos-farmacia-attendant").z;
                if (curZ < minZObserved) minZObserved = curZ;
            }
            AssertCheck(
                $"No backward snap on delayed poll arrival (minZ={minZObserved:F3} >= forwardPosZ={forwardPosZ:F3})",
                minZObserved >= forwardPosZ - 0.001f
            );

            // 3. Stale snapshot freeze: exceeding StaleThresholdSeconds freezes movement instead of inventing progress
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                movementState = "moving",
                currentWaypointId = "wp_valeria_storefront",
                destinationWaypointId = "wp_optica_plaza_threshold",
                position = new Vector3Dto { x = -9.5f, y = 0.2f, z = 1.2f },
                activeSegmentEndPosition = new Vector3Dto { x = -8.35f, y = 0.2f, z = 0.85f },
                forwardHeading = new HeadingDto { x = 0.957f, z = -0.291f },
                currentSpeedMps = 1.10f,
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "local_roaming",
                withinAuthorizedDomain = true
            }, snapImmediately: true);

            Vector3 frozenStartPos = movCtrl.GetNpcPosition("valeria-optica-attendant");
            movCtrl.timeSinceLastSnapshot = 45f;
            for (int i = 0; i < 12; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
            }
            Vector3 frozenEndPos = movCtrl.GetNpcPosition("valeria-optica-attendant");
            AssertCheck(
                $"Stale snapshot freezes autonomous progress (drift={Vector3.Distance(frozenStartPos, frozenEndPos):F4}m)",
                movCtrl.movementAuthorityState == AuthoritativeMovementState.Stale &&
                Vector3.Distance(frozenStartPos, frozenEndPos) < 0.001f &&
                movCtrl.GetVisualMoveSpeed("valeria-optica-attendant") < 0.01f
            );

            // 4. Business dwell orientation: Valeria smoothly rotates toward Optica Fashion (-X) when dwelling at plaza threshold
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                movementState = "stationary",
                currentWaypointId = "wp_optica_plaza_threshold",
                position = new Vector3Dto { x = -8.35f, y = 0.2f, z = 0.85f },
                activeSegmentEndPosition = new Vector3Dto { x = -8.35f, y = 0.2f, z = 0.85f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 0f,
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "idle_observation",
                controlMode = "autonomous_business_local",
                withinAuthorizedDomain = true
            }, snapImmediately: false);

            for (int i = 0; i < 30; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
            }
            AssertCheck(
                $"Valeria reorients intentionally toward Optica storefront during dwell (forward.x={valeriaVisual.forward.x:F2})",
                valeriaVisual != null && valeriaVisual.forward.x < -0.90f
            );

            if (createdTemp && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5223Verification] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
