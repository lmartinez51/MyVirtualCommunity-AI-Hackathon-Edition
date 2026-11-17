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
    /// Increment 5.22.1 scene cleanup (removes unwanted Highlight_Indicator foot bases from all NPCs)
    /// and automated Unity batchmode verification suite.
    /// </summary>
    public static class Increment5221VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        private static readonly string[] CanonicalNpcIds =
        {
            "elena-border-concierge",
            "carlos-farmacia-attendant",
            "valeria-optica-attendant",
            "mateo-student",
            "sofia-cross-border-retail",
            "hector-artisan",
            "patricia-citizen-rep"
        };

        [MenuItem("MyVirtualCommunity/Increment 5.22.1/Remove NPC Highlight Indicators From Scene")]
        public static void RemoveNpcHighlightIndicators()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5221Cleanup] FAIL: Could not open {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            int removedCount = 0;
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go != null && string.Equals(go.name, "Highlight_Indicator", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[Increment5221Cleanup] Removing unwanted foot base '{go.name}' from parent '{go.transform.parent?.name ?? "<root>"}'");
                    UnityEngine.Object.DestroyImmediate(go);
                    removedCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Increment5221Cleanup] Removed {removedCount} Highlight_Indicator objects and saved {ScenePath}.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("MyVirtualCommunity/Increment 5.22.1/Run Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5221Verification] Starting Increment 5.22.1 Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5221Verification] FAIL: Could not open {ScenePath}");
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

            // 1. Verify zero Highlight_Indicator objects remain anywhere in scene or NPC hierarchies
            int remainingIndicators = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && string.Equals(go.name, "Highlight_Indicator", StringComparison.OrdinalIgnoreCase))
                {
                    remainingIndicators++;
                }
            }
            AssertCheck("Zero Highlight_Indicator foot bases remain in BorderCorridorScene", remainingIndicators == 0);

            // 2. Verify all 7 canonical NPCs retain InteractionPoint, BobbleheadCharacter, CapsuleCollider, and NpcAnimationController
            var movCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTempMovCtrl = false;
            if (movCtrl == null)
            {
                var tempGo = new GameObject("Temp_NpcWaypointMovementController_5221");
                movCtrl = tempGo.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            movCtrl.DiscoverSceneReferences();

            var pointsByNpc = new Dictionary<string, InteractionPoint>(StringComparer.OrdinalIgnoreCase);
            foreach (var pt in UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(pt.characterId))
                {
                    pointsByNpc[pt.characterId] = pt;
                }
            }

            foreach (var npcId in CanonicalNpcIds)
            {
                bool hasPoint = pointsByNpc.TryGetValue(npcId, out var pt) && pt != null;
                var animCtrl = movCtrl.GetNpcAnimationController(npcId);
                BobbleheadCharacter bobble = null;
                CapsuleCollider cap = null;
                if (hasPoint)
                {
                    bobble = pt.GetComponentInChildren<BobbleheadCharacter>(true) ??
                             (pt.transform.parent != null ? pt.transform.parent.GetComponentInChildren<BobbleheadCharacter>(true) : null);
                    cap = bobble != null ? bobble.GetComponent<CapsuleCollider>() : pt.GetComponentInChildren<CapsuleCollider>(true);
                }

                bool hasHighlightChild = false;
                if (hasPoint)
                {
                    foreach (var tr in pt.GetComponentsInChildren<Transform>(true))
                    {
                        if (string.Equals(tr.name, "Highlight_Indicator", StringComparison.OrdinalIgnoreCase))
                        {
                            hasHighlightChild = true;
                        }
                    }
                }

                AssertCheck(
                    $"NPC {npcId}: no foot base + intact InteractionPoint, Bobblehead, CapsuleCollider(r=0.35), NpcAnimationController",
                    hasPoint && !hasHighlightChild && bobble != null && cap != null && Mathf.Abs(cap.radius - 0.35f) < 0.02f && animCtrl != null
                );
            }

            // 3. Verify NpcWaypointMovementController polling & interpolation speed match tuned NPC walking speeds (<= 3.85 m/s)
            AssertCheck(
                "NpcWaypointMovementController pollIntervalSeconds <= 1.0f and positionLerpSpeed >= 5.5f",
                movCtrl.pollIntervalSeconds <= 1.0f && movCtrl.positionLerpSpeed >= 5.5f
            );

            // 4. Verify elevated bridge crest tracking (Z=214.07, Y=7.058) raises detached visual instead of clamping to 0.20m
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "mateo-student",
                canonicalName = "Mateo Navarro",
                actorArchetype = "community_player_proxy",
                isWitness = true,
                movementState = "moving",
                currentWaypointId = "wp_bridge_north_overlook",
                destinationWaypointId = "wp_bridge_north_overlook",
                activeRouteId = "patrol_mateo-student_east_walkway_northbound",
                position = new Vector3Dto { x = 16.16f, y = 7.058f, z = 214.07f },
                forwardHeading = new HeadingDto { x = 0.04f, z = 0.999f },
                progression = 0.5f,
                activityState = "available",
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "patrolling",
                controlMode = "autonomous_bridge_patrol",
                patrolDirection = "northbound",
                withinAuthorizedDomain = true
            }, snapImmediately: true);

            Vector3 mateoCrestPos = movCtrl.GetNpcPosition("mateo-student");
            AssertCheck(
                $"Bridge crest elevation tracking moves Mateo to Y={mateoCrestPos.y:F3}m (> 6.5m)",
                mateoCrestPos.y > 6.5f && mateoCrestPos.z > 200f
            );

            // 5. Verify Sofia brisk walking (3.85 m/s) drives Walking animation & locomotion blend cleanly
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "sofia-cross-border-retail",
                canonicalName = "Sofía Alarcón",
                actorArchetype = "community_player_proxy",
                isWitness = true,
                movementState = "moving",
                currentWaypointId = "wp_bridge_north_overlook",
                destinationWaypointId = "wp_bridge_north_overlook",
                activeRouteId = "patrol_sofia-cross-border-retail_west_walkway_southbound",
                position = new Vector3Dto { x = -7.25f, y = 3.454f, z = 134.25f },
                forwardHeading = new HeadingDto { x = 0f, z = -1f },
                progression = 0.6f,
                activityState = "available",
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "patrolling",
                controlMode = "autonomous_bridge_patrol",
                patrolDirection = "southbound",
                withinAuthorizedDomain = true
            }, snapImmediately: false);

            var sofiaAnim = movCtrl.GetNpcAnimationController("sofia-cross-border-retail");
            for (int i = 0; i < 30; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                sofiaAnim?.StepPresentation(0.05f);
            }
            AssertCheck(
                "Sofia at 3.85 m/s activates Walking animation and locomotion blend cleanly",
                sofiaAnim != null && sofiaAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking && sofiaAnim.IsWalkingActive && sofiaAnim.LocomotionBlendWeight > 0.5f
            );

            // 6. Verify static kiosk booth risers (Attendant_Platform) remain intact
            var borderKiosk = GameObject.Find("Border_Information_Kiosk");
            var citizenDesk = GameObject.Find("CitizenServiceDesk");
            AssertCheck(
                "Static kiosk booth risers remain intact for Elena and Patricia",
                borderKiosk != null && borderKiosk.transform.Find("Attendant_Platform") != null &&
                citizenDesk != null && citizenDesk.transform.Find("Attendant_Platform") != null
            );

            // 7. Verify physical walkway grounding snaps elevated snapshot Y onto actual collider surface
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "mateo-student",
                canonicalName = "Mateo Navarro",
                actorArchetype = "community_player_proxy",
                isWitness = true,
                movementState = "moving",
                currentWaypointId = "wp_bridge_north_overlook",
                destinationWaypointId = "wp_bridge_north_overlook",
                activeRouteId = "patrol_mateo-student_east_walkway_northbound",
                position = new Vector3Dto { x = 16.16f, y = 7.40f, z = 214.07f },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                currentSpeedMps = 3.7f,
                progression = 0.5f,
                activityState = "available",
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "patrolling",
                controlMode = "autonomous_bridge_patrol",
                patrolDirection = "northbound",
                withinAuthorizedDomain = true
            }, snapImmediately: true);

            Vector3 groundedMateoPos = movCtrl.GetNpcPosition("mateo-student");
            AssertCheck(
                $"Physical raycast grounding prevents hover on walkway crest (7.40m -> {groundedMateoPos.y:F3}m)",
                groundedMateoPos.y >= 6.95f && groundedMateoPos.y <= 7.05f
            );

            // 8. Verify continuous inter-poll progression at currentSpeedMps keeps NPC & legs advancing smoothly
            var mateoAnim = movCtrl.GetNpcAnimationController("mateo-student");
            float startZ = groundedMateoPos.z;
            for (int i = 0; i < 16; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                mateoAnim?.StepPresentation(0.05f);
            }
            float advancedZ = movCtrl.GetNpcPosition("mateo-student").z - startZ;
            AssertCheck(
                $"Continuous inter-poll extrapolation advances Mateo +{advancedZ:F2}m with active synced leg cycle",
                advancedZ >= 2.2f && advancedZ <= 3.3f &&
                mateoAnim != null && mateoAnim.IsWalkingActive && mateoAnim.LocomotionBlendWeight > 0.85f
            );

            if (createdTempMovCtrl && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5221Verification] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
