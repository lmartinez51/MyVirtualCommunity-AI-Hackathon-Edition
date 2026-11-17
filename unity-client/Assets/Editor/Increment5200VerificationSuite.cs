using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity Editor / Batchmode verification suite for Increment 5.20.0
    /// (Contextual Social Behavior & Environmental Awareness).
    /// </summary>
    public static class Increment5200VerificationSuite
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

        [MenuItem("MyVirtualCommunity/Run Increment 5.20.0 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5200Verification] Starting Increment 5.20.0 Social Awareness Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5200Verification] FAIL: Could not open {ScenePath}");
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
            bool createdTempMovCtrl = false;
            if (movCtrl == null)
            {
                var go = new GameObject("Temp_NpcWaypointMovementController_5200");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            movCtrl.DiscoverSceneReferences();

            // 1. Canonical controller and visual binding sanity
            foreach (string npcId in CanonicalNpcIds)
            {
                var ctrl = movCtrl.GetNpcAnimationController(npcId);
                if (ctrl != null) ctrl.InitializeIfNeeded(forceRecachePoses: true);
                AssertCheck($"NPC '{npcId}' bound for social awareness", ctrl != null);
            }

            // 2. DTO deserialization for /api/world/npcs/social-awareness & embedded /interaction-context payloads
            string socialJson = @"{
                ""success"": true,
                ""evaluatedAt"": ""2026-11-04T17:00:00.000Z"",
                ""timeZone"": ""America/Ciudad_Juarez"",
                ""localCivilTime"": ""2026-11-04T10:00:00.000-07:00"",
                ""playerId"": ""player-1"",
                ""playerPosition"": { ""x"": 10.5, ""y"": 1.0, ""z"": 3.0 },
                ""detectionBasis"": ""proximity_horizontal_vertical"",
                ""supportsLineOfSight"": false,
                ""environmentalAwarenessScope"": ""player_and_npc_proximity_only"",
                ""thresholds"": {
                    ""playerGeneralMeters"": 8.0,
                    ""playerCloseMeters"": 4.0,
                    ""npcAwarenessMeters"": 5.0,
                    ""npcSocialMeters"": 3.75,
                    ""maxVerticalSeparationMeters"": 3.0
                },
                ""npcsList"": [
                    {
                        ""npcId"": ""carlos-farmacia-attendant"",
                        ""canonicalName"": ""Carlos Méndez"",
                        ""awarenessState"": ""player_attentive"",
                        ""targetEntityType"": ""player"",
                        ""targetEntityId"": ""player-1"",
                        ""spatialRelation"": {
                            ""horizontalDistanceMeters"": 3.0,
                            ""verticalSeparationMeters"": 0.0,
                            ""directionToTarget"": { ""x"": 0.0, ""z"": 1.0 }
                        },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": """",
                        ""associatedPlaceId"": ""pharmacy:farmacias-benavides"",
                        ""currentWaypointId"": ""wp_carlos_counter"",
                        ""interactionAvailability"": ""available_normal"",
                        ""movementState"": ""stationary"",
                        ""nearbyPlayerDistanceMeters"": 3.0,
                        ""nearbyNpcCount"": 0,
                        ""detectionBasis"": ""proximity_horizontal_vertical"",
                        ""supportsLineOfSight"": false
                    },
                    {
                        ""npcId"": ""sofia-cross-border-retail"",
                        ""canonicalName"": ""Sofía Herrera"",
                        ""awarenessState"": ""social_engaged"",
                        ""targetEntityType"": ""npc"",
                        ""targetEntityId"": ""hector-artisan"",
                        ""spatialRelation"": {
                            ""horizontalDistanceMeters"": 3.5,
                            ""verticalSeparationMeters"": 0.0,
                            ""directionToTarget"": { ""x"": 0.0, ""z"": 1.0 }
                        },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": ""hector-artisan"",
                        ""associatedPlaceId"": ""corridor:pedestrian-approach"",
                        ""currentWaypointId"": ""wp_sofia_queue_mid"",
                        ""interactionAvailability"": ""available_normal"",
                        ""movementState"": ""stationary"",
                        ""nearbyPlayerDistanceMeters"": 0.0,
                        ""nearbyNpcCount"": 1,
                        ""detectionBasis"": ""proximity_horizontal_vertical"",
                        ""supportsLineOfSight"": false
                    },
                    {
                        ""npcId"": ""hector-artisan"",
                        ""canonicalName"": ""Don Héctor Morales"",
                        ""awarenessState"": ""social_engaged"",
                        ""targetEntityType"": ""npc"",
                        ""targetEntityId"": ""sofia-cross-border-retail"",
                        ""spatialRelation"": {
                            ""horizontalDistanceMeters"": 3.5,
                            ""verticalSeparationMeters"": 0.0,
                            ""directionToTarget"": { ""x"": 0.0, ""z"": -1.0 }
                        },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": ""sofia-cross-border-retail"",
                        ""associatedPlaceId"": ""corridor:pedestrian-approach"",
                        ""currentWaypointId"": ""wp_hector_stand"",
                        ""interactionAvailability"": ""available_normal"",
                        ""movementState"": ""stationary"",
                        ""nearbyPlayerDistanceMeters"": 0.0,
                        ""nearbyNpcCount"": 1,
                        ""detectionBasis"": ""proximity_horizontal_vertical"",
                        ""supportsLineOfSight"": false
                    }
                ],
                ""activeSocialPairs"": [
                    { ""npcIdA"": ""hector-artisan"", ""npcIdB"": ""sofia-cross-border-retail"", ""distanceMeters"": 2.4 }
                ],
                ""summary"": {
                    ""unawareCount"": 4,
                    ""playerAwareCount"": 0,
                    ""playerAttentiveCount"": 1,
                    ""npcAwareCount"": 0,
                    ""socialEngagedCount"": 2,
                    ""activePairCount"": 1,
                    ""totalCount"": 7
                },
                ""isDemonstrationAwareness"": true
            }";

            var socialDto = JsonUtility.FromJson<NpcRosterSocialAwarenessResponseDto>(socialJson);
            AssertCheck("Social awareness DTO parses cleanly with honest metadata",
                socialDto != null &&
                socialDto.success &&
                socialDto.detectionBasis == "proximity_horizontal_vertical" &&
                !socialDto.supportsLineOfSight &&
                socialDto.environmentalAwarenessScope == "player_and_npc_proximity_only" &&
                socialDto.npcsList != null && socialDto.npcsList.Length == 3 &&
                socialDto.activeSocialPairs != null && socialDto.activeSocialPairs.Length == 1 &&
                socialDto.summary != null && socialDto.summary.activePairCount == 1);

            string ctxJson = @"{
                ""success"": true,
                ""npcsList"": [
                    {
                        ""npcId"": ""carlos-farmacia-attendant"",
                        ""interactionAvailability"": ""available_normal"",
                        ""interactionMode"": ""normal_service"",
                        ""activityState"": ""available"",
                        ""isAvailableForNormalService"": true,
                        ""canInitiateConversation"": true,
                        ""socialAwareness"": {
                            ""npcId"": ""carlos-farmacia-attendant"",
                            ""awarenessState"": ""player_aware"",
                            ""targetEntityType"": ""player"",
                            ""targetEntityId"": ""player-1"",
                            ""spatialRelation"": {
                                ""horizontalDistanceMeters"": 6.0,
                                ""verticalSeparationMeters"": 0.0,
                                ""directionToTarget"": { ""x"": 0.8, ""z"": 0.6 }
                            },
                            ""socialEligible"": true,
                            ""canOrientTowardTarget"": true,
                            ""retainsContextualActivity"": false,
                            ""pairedPartnerNpcId"": """"
                        }
                    }
                ],
                ""socialAwarenessSummary"": { ""playerAwareCount"": 1, ""totalCount"": 7 }
            }";
            var ctxDto = JsonUtility.FromJson<NpcRosterInteractionContextResponseDto>(ctxJson);
            AssertCheck("Embedded socialAwareness inside /interaction-context DTO parses cleanly",
                ctxDto != null && ctxDto.success && ctxDto.npcsList != null && ctxDto.npcsList.Length == 1 &&
                ctxDto.npcsList[0].socialAwareness != null &&
                ctxDto.npcsList[0].socialAwareness.awarenessState == "player_aware");

            // 3. Apply player_aware via interaction-context aggregate path & verify smooth orientation without greeting hijack
            var carlosCtrl = movCtrl.GetNpcAnimationController("carlos-farmacia-attendant");
            Transform carlosVisual = movCtrl.GetNpcVisualTransform("carlos-farmacia-attendant");
            Vector3 carlosRootPosBefore = carlosVisual != null ? carlosVisual.position : Vector3.zero;
            carlosCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_carlos_counter", true, 0f);
            movCtrl.ApplyAuthoritativeInteractionContextRoster(ctxDto);

            for (int i = 0; i < 16; i++)
            {
                carlosCtrl.StepPresentation(0.05f);
            }

            AssertCheck("Carlos transitions to PlayerAware without altering GreetingCue",
                movCtrl.GetNpcAwarenessState("carlos-farmacia-attendant") == NpcVisualAwarenessState.PlayerAware &&
                carlosCtrl.CurrentBehaviorState != NpcVisualBehaviorState.GreetingCue);
            AssertCheck("Social awareness does NOT mutate NPC root transform position",
                carlosVisual == null || Vector3.Distance(carlosVisual.position, carlosRootPosBefore) < 0.0001f);

            // 4. Apply full roster awareness DTO (Carlos PlayerAttentive; Sofia & Hector mutual SocialEngaged)
            movCtrl.ApplyAuthoritativeSocialAwarenessRoster(socialDto);
            var sofiaCtrl = movCtrl.GetNpcAnimationController("sofia-cross-border-retail");
            var hectorCtrl = movCtrl.GetNpcAnimationController("hector-artisan");
            sofiaCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_sofia_queue_mid", true, 0f);
            hectorCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_hector_stand", true, 0f);

            for (int i = 0; i < 20; i++)
            {
                carlosCtrl.StepPresentation(0.05f);
                sofiaCtrl.StepPresentation(0.05f);
                hectorCtrl.StepPresentation(0.05f);
            }

            AssertCheck("Carlos enters PlayerAttentive and smoothly orients toward player direction",
                carlosCtrl.CurrentAwarenessState == NpcVisualAwarenessState.PlayerAttentive &&
                carlosCtrl.CurrentAwarenessTargetType == "player" &&
                (Mathf.Abs(carlosCtrl.CurrentHeadYawDegrees) > 1.0f || Mathf.Abs(carlosCtrl.CurrentTorsoSocialYawDegrees) > 0.5f));

            AssertCheck("Sofia and Hector enter mutual SocialEngaged state with perceptible bilateral orientation",
                sofiaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.SocialEngaged &&
                hectorCtrl.CurrentAwarenessState == NpcVisualAwarenessState.SocialEngaged &&
                Mathf.Abs(sofiaCtrl.CurrentBodySocialYawDegrees) > 10.0f &&
                Mathf.Abs(sofiaCtrl.CurrentTorsoSocialYawDegrees) <= 23.0f &&
                Mathf.Abs(hectorCtrl.CurrentBodySocialYawDegrees) > 100.0f &&
                Mathf.Abs(hectorCtrl.CurrentTorsoSocialYawDegrees) <= 23.0f &&
                (Mathf.Abs(hectorCtrl.CurrentBodySocialYawDegrees) + Mathf.Abs(hectorCtrl.CurrentTorsoSocialYawDegrees) + Mathf.Abs(hectorCtrl.CurrentHeadYawDegrees)) > 140.0f);

            InteractionPoint sofiaPoint = null;
            InteractionPoint hectorPoint = null;
            foreach (var p in UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.characterId == "sofia-cross-border-retail") sofiaPoint = p;
                if (p.characterId == "hector-artisan") hectorPoint = p;
            }
            AssertCheck("InteractionPoint exposes mutual pairedPartnerNpcId & non-LoS awareness metadata",
                sofiaPoint != null && hectorPoint != null &&
                sofiaPoint.socialAwarenessAvailable &&
                sofiaPoint.socialAwarenessState == "social_engaged" &&
                sofiaPoint.socialPairedPartnerNpcId == "hector-artisan" &&
                hectorPoint.socialPairedPartnerNpcId == "sofia-cross-border-retail");

            // 5. Return to Unaware transitions smoothly via ReturningToActivity before settling back to Unaware
            movCtrl.ApplySingleNpcSocialAwareness(new NpcSocialAwarenessDto
            {
                npcId = "carlos-farmacia-attendant",
                awarenessState = "unaware",
                targetEntityType = "none",
                targetEntityId = "",
                canOrientTowardTarget = false,
                retainsContextualActivity = false
            });
            carlosCtrl.StepPresentation(0.05f);
            AssertCheck("Leaving awareness passes through ReturningToActivity",
                carlosCtrl.CurrentAwarenessState == NpcVisualAwarenessState.ReturningToActivity);

            for (int i = 0; i < 30; i++)
            {
                carlosCtrl.StepPresentation(0.05f);
            }
            AssertCheck("After return settle window, Carlos returns cleanly to Unaware with zero social torso yaw",
                carlosCtrl.CurrentAwarenessState == NpcVisualAwarenessState.Unaware &&
                Mathf.Abs(carlosCtrl.CurrentTorsoSocialYawDegrees) < 0.25f);

            // 6. Moving / Busy / Contextual station activity / Off-duty eligibility preservation
            var valeriaCtrl = movCtrl.GetNpcAnimationController("valeria-optica-attendant");
            valeriaCtrl.SyncPresentationInputs("stationary", "available_contextual", "break_or_dwell", "calibrated_optics", "wp_optica_workshop", true, 0f);
            valeriaCtrl.SyncSocialAwareness("player_aware", "player", "player-1", new Vector2(1f, 0f), 6.0f, true, true, null);
            for (int i = 0; i < 10; i++) valeriaCtrl.StepPresentation(0.05f);
            AssertCheck("Valeria at optical workshop retains ContextualActivity while subtly noticing player",
                valeriaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.ContextualActivity &&
                valeriaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.PlayerAware &&
                Mathf.Abs(valeriaCtrl.CurrentTorsoSocialYawDegrees) < 0.1f);

            var patriciaCtrl = movCtrl.GetNpcAnimationController("patricia-citizen-rep");
            patriciaCtrl.SyncPresentationInputs("stationary", "off_duty", "off_duty_courtesy", "off_duty", "wp_patricia_desk", false, 0f);
            patriciaCtrl.SyncSocialAwareness("player_attentive", "player", "player-1", new Vector2(0f, 1f), 2.0f, false, true, null);
            patriciaCtrl.StepPresentation(0.05f);
            AssertCheck("Off-duty Patricia does not orient even if awareness query places player near",
                patriciaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty &&
                patriciaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.Unaware);

            // 7. Missing/malformed awareness payload clears safely without throwing
            movCtrl.ApplyAuthoritativeSocialAwarenessRoster(null);
            AssertCheck("Null awareness roster clears social awareness cleanly",
                !sofiaCtrl.HasAuthoritativeAwareness &&
                sofiaPoint != null && !sofiaPoint.socialAwarenessAvailable);

            // 8. Architectural verification: no per-NPC HTTP fan-out inside Update / PollMovementRosterRoutine / RefreshNpcRosterActivity
            string firstPersonPath = Path.Combine(Application.dataPath, "Scripts", "FirstPersonPlayer.cs");
            string npcMovPath = Path.Combine(Application.dataPath, "Scripts", "NpcWaypointMovementController.cs");
            string firstPersonSrc = File.Exists(firstPersonPath) ? File.ReadAllText(firstPersonPath) : "";
            string npcMovSrc = File.Exists(npcMovPath) ? File.ReadAllText(npcMovPath) : "";
            int refreshIdx = firstPersonSrc.IndexOf("public void RefreshNpcRosterActivity()", StringComparison.Ordinal);
            int refreshEndIdx = refreshIdx >= 0
                ? firstPersonSrc.IndexOf("public void RefreshIndividualNpcInteractionContext", refreshIdx, StringComparison.Ordinal)
                : -1;
            string refreshBody = (refreshIdx >= 0 && refreshEndIdx > refreshIdx)
                ? firstPersonSrc.Substring(refreshIdx, refreshEndIdx - refreshIdx)
                : "";
            AssertCheck("FirstPersonPlayer uses single aggregate GetNpcRosterInteractionContext (no per-NPC fan-out)",
                !string.IsNullOrEmpty(refreshBody) &&
                refreshBody.Contains("GetNpcRosterInteractionContext") &&
                !refreshBody.Contains("GetNpcInteractionContext("));
            AssertCheck("NpcWaypointMovementController polls aggregate GetNpcRosterInteractionContext alongside movement",
                npcMovSrc.Contains("GetNpcRosterInteractionContext"));

            // 9. Section 15 Elena movement consistency check: kiosk anchored while visualTransform moves to plaza
            Transform borderKiosk = GameObject.Find("Border_Information_Kiosk")?.transform;
            Transform elenaVisual = movCtrl.GetNpcVisualTransform("elena-border-concierge");
            AssertCheck("Elena visual transform is separate from Border_Information_Kiosk anchor",
                borderKiosk != null && elenaVisual != null && elenaVisual != borderKiosk &&
                elenaVisual.name.Contains("ElenaRamos_Attendant"));

            Vector3 kioskAnchorBefore = borderKiosk != null ? borderKiosk.position : Vector3.zero;
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "elena-border-concierge",
                movementState = "arrived",
                currentWaypointId = "wp_bridge_plaza",
                associatedPlaceId = "bridge:paso-del-norte",
                position = new Vector3Dto { x = 17.8f, y = 0.2f, z = 42.0f },
                forwardHeading = new HeadingDto { x = -1.0f, z = 0f }
            }, snapImmediately: true);

            Vector3 elenaVisualPos = elenaVisual != null ? elenaVisual.position : Vector3.zero;
            AssertCheck("Elena plaza target tracked (17.8, 42.0) while Border_Information_Kiosk stays anchored",
                borderKiosk != null && elenaVisual != null &&
                Vector3.Distance(borderKiosk.position, kioskAnchorBefore) < 0.001f &&
                Vector2.Distance(new Vector2(elenaVisualPos.x, elenaVisualPos.z), new Vector2(17.8f, 42.0f)) < 0.1f);

            // Return Elena smoothly to kiosk and verify return to wp_elena_kiosk without breaking kiosk anchor
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "elena-border-concierge",
                movementState = "stationary",
                currentWaypointId = "wp_elena_kiosk",
                associatedPlaceId = "bridge:paso-del-norte",
                position = new Vector3Dto { x = 19.2f, y = 0.2f, z = 39.5f },
                forwardHeading = new HeadingDto { x = 0f, z = -1.0f }
            }, snapImmediately: false);
            for (int i = 0; i < 40; i++) movCtrl.StepVisualMovement(0.05f);
            Vector3 elenaReturnPos = elenaVisual != null ? elenaVisual.position : Vector3.zero;
            // Elena stands on her raised counter platform at local (0, 0.38, 0.35) inside Border_Information_Kiosk (19.20, 0.20, 39.50)
            AssertCheck("Elena smoothly returns to wp_elena_kiosk station (19.20, 39.85) while kiosk stays anchored",
                borderKiosk != null && elenaVisual != null &&
                Vector3.Distance(borderKiosk.position, kioskAnchorBefore) < 0.001f &&
                Vector2.Distance(new Vector2(elenaReturnPos.x, elenaReturnPos.z), new Vector2(19.2f, 39.85f)) < 0.1f);

            // 10. Increment 5.20.0.1 Runtime Correction: real 08:30 Mateo <-> Sofia (3.5m) pair & Elena NpcAware (4.72m),
            // steady persistence, zero root transform mutation, player precedence override, and smooth return.
            var mateoCtrl = movCtrl.GetNpcAnimationController("mateo-student");
            var elenaCtrl = movCtrl.GetNpcAnimationController("elena-border-concierge");
            Transform mateoVisual = movCtrl.GetNpcVisualTransform("mateo-student");
            Transform sofiaVisual = movCtrl.GetNpcVisualTransform("sofia-cross-border-retail");
            Vector3 mateoRootPos0 = mateoVisual != null ? mateoVisual.position : Vector3.zero;
            Quaternion mateoRootRot0 = mateoVisual != null ? mateoVisual.rotation : Quaternion.identity;
            Vector3 sofiaRootPos0 = sofiaVisual != null ? sofiaVisual.position : Vector3.zero;
            Quaternion sofiaRootRot0 = sofiaVisual != null ? sofiaVisual.rotation : Quaternion.identity;

            mateoCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_mateo_queue_entry", true, 0f);
            sofiaCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_sofia_queue_mid", true, 0f);
            elenaCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_elena_kiosk", true, 0f);

            string morningQueueJson = @"{
                ""success"": true,
                ""evaluatedAt"": ""2026-11-11T15:30:00.000Z"",
                ""thresholds"": {
                    ""playerGeneralMeters"": 8.0,
                    ""playerCloseMeters"": 4.0,
                    ""npcAwarenessMeters"": 5.0,
                    ""npcSocialMeters"": 3.75,
                    ""maxVerticalSeparationMeters"": 3.0
                },
                ""npcsList"": [
                    {
                        ""npcId"": ""mateo-student"",
                        ""awarenessState"": ""social_engaged"",
                        ""targetEntityType"": ""npc"",
                        ""targetEntityId"": ""sofia-cross-border-retail"",
                        ""spatialRelation"": { ""horizontalDistanceMeters"": 3.5, ""verticalSeparationMeters"": 0.05, ""directionToTarget"": { ""x"": 0.0, ""z"": 1.0 } },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": ""sofia-cross-border-retail""
                    },
                    {
                        ""npcId"": ""sofia-cross-border-retail"",
                        ""awarenessState"": ""social_engaged"",
                        ""targetEntityType"": ""npc"",
                        ""targetEntityId"": ""mateo-student"",
                        ""spatialRelation"": { ""horizontalDistanceMeters"": 3.5, ""verticalSeparationMeters"": 0.05, ""directionToTarget"": { ""x"": 0.0, ""z"": -1.0 } },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": ""mateo-student""
                    },
                    {
                        ""npcId"": ""elena-border-concierge"",
                        ""awarenessState"": ""npc_aware"",
                        ""targetEntityType"": ""npc"",
                        ""targetEntityId"": ""mateo-student"",
                        ""spatialRelation"": { ""horizontalDistanceMeters"": 4.717, ""verticalSeparationMeters"": 0.13, ""directionToTarget"": { ""x"": -0.848, ""z"": 0.530 } },
                        ""socialEligible"": true,
                        ""canOrientTowardTarget"": true,
                        ""retainsContextualActivity"": false,
                        ""pairedPartnerNpcId"": """"
                    }
                ]
            }";
            var morningDto = JsonUtility.FromJson<NpcRosterSocialAwarenessResponseDto>(morningQueueJson);
            movCtrl.ApplyAuthoritativeSocialAwarenessRoster(morningDto);
            for (int i = 0; i < 30; i++)
            {
                mateoCtrl.StepPresentation(0.05f);
                sofiaCtrl.StepPresentation(0.05f);
                elenaCtrl.StepPresentation(0.05f);
            }
            float sofiaBodyAfter30 = sofiaCtrl.CurrentBodySocialYawDegrees;
            float mateoBodyAfter30 = mateoCtrl.CurrentBodySocialYawDegrees;
            for (int i = 0; i < 20; i++)
            {
                mateoCtrl.StepPresentation(0.05f);
                sofiaCtrl.StepPresentation(0.05f);
            }
            AssertCheck("08:30 Mateo <-> Sofia (3.5m) both orient visibly & persist steadily without mutating root transforms",
                mateoCtrl.CurrentAwarenessState == NpcVisualAwarenessState.SocialEngaged &&
                sofiaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.SocialEngaged &&
                Mathf.Abs(mateoBodyAfter30) > 10.0f &&
                Mathf.Abs(mateoCtrl.CurrentBodySocialYawDegrees) > 10.0f &&
                Mathf.Abs(sofiaBodyAfter30) > 100.0f &&
                Mathf.Abs(sofiaCtrl.CurrentBodySocialYawDegrees) > 100.0f &&
                (mateoVisual == null || (Vector3.Distance(mateoVisual.position, mateoRootPos0) < 0.0001f && Quaternion.Angle(mateoVisual.rotation, mateoRootRot0) < 0.01f)) &&
                (sofiaVisual == null || (Vector3.Distance(sofiaVisual.position, sofiaRootPos0) < 0.0001f && Quaternion.Angle(sofiaVisual.rotation, sofiaRootRot0) < 0.01f)));

            AssertCheck("08:30 Elena (4.72m from Mateo) shows clear NpcAware orientation",
                elenaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.NpcAware &&
                (Mathf.Abs(elenaCtrl.CurrentHeadYawDegrees) + Mathf.Abs(elenaCtrl.CurrentBodySocialYawDegrees)) > 5.0f);

            // Player close attention breaks/precedes active NPC pair cleanly
            movCtrl.ApplySingleNpcSocialAwareness(new NpcSocialAwarenessDto
            {
                npcId = "sofia-cross-border-retail",
                awarenessState = "player_attentive",
                targetEntityType = "player",
                targetEntityId = "player-1",
                spatialRelation = new NpcAwarenessSpatialRelationDto
                {
                    horizontalDistanceMeters = 2.5f,
                    verticalSeparationMeters = 0f,
                    directionToTarget = new NpcAwarenessDirectionDto { x = -1.0f, z = 0.0f }
                },
                canOrientTowardTarget = true,
                retainsContextualActivity = false,
                pairedPartnerNpcId = ""
            });
            movCtrl.ApplySingleNpcSocialAwareness(new NpcSocialAwarenessDto
            {
                npcId = "mateo-student",
                awarenessState = "unaware",
                targetEntityType = "none",
                targetEntityId = "",
                canOrientTowardTarget = false,
                retainsContextualActivity = false,
                pairedPartnerNpcId = ""
            });
            sofiaCtrl.StepPresentation(0.05f);
            mateoCtrl.StepPresentation(0.05f);
            bool mateoEnteredReturning = mateoCtrl.CurrentAwarenessState == NpcVisualAwarenessState.ReturningToActivity;
            for (int i = 0; i < 30; i++)
            {
                sofiaCtrl.StepPresentation(0.05f);
                mateoCtrl.StepPresentation(0.05f);
            }
            AssertCheck("Player close attention overrides Sofia pair and Mateo returns smoothly to activity",
                sofiaCtrl.CurrentAwarenessState == NpcVisualAwarenessState.PlayerAttentive &&
                sofiaCtrl.CurrentAwarenessTargetType == "player" &&
                mateoEnteredReturning &&
                mateoCtrl.CurrentAwarenessState == NpcVisualAwarenessState.Unaware &&
                Mathf.Abs(mateoCtrl.CurrentBodySocialYawDegrees) < 0.5f &&
                Mathf.Abs(mateoCtrl.CurrentTorsoSocialYawDegrees) < 0.25f);

            // 11. All 7 canonical NPCs track scheduled waypoint movement cleanly while fixed buildings/kiosks stay anchored
            Transform citizenDesk = GameObject.Find("CitizenServiceDesk")?.transform;
            Transform benavidesStore = GameObject.Find("Storefront_FarmaciasBenavides")?.transform;
            Transform opticaStore = GameObject.Find("Storefront_OpticaFashion")?.transform;
            Vector3 citizenDeskBefore = citizenDesk != null ? citizenDesk.position : Vector3.zero;
            Vector3 benavidesBefore = benavidesStore != null ? benavidesStore.position : Vector3.zero;
            Vector3 opticaBefore = opticaStore != null ? opticaStore.position : Vector3.zero;

            var movementCases = new (string npcId, string awayWp, Vector3 awayPos, string homeWp, Vector3 homeApiPos, Vector3 homeExpectedVisualPos)[]
            {
                ("mateo-student", "wp_bridge_toll_plaza", new Vector3(15.2f, 0.35f, 47.0f), "wp_mateo_queue_entry", new Vector3(15.2f, 0.33f, 42.0f), new Vector3(15.2f, 0.33f, 42.0f)),
                ("sofia-cross-border-retail", "wp_canopy_waiting", new Vector3(16.0f, 0.25f, 41.0f), "wp_sofia_queue_mid", new Vector3(15.2f, 0.38f, 45.5f), new Vector3(15.2f, 0.38f, 45.5f)),
                ("patricia-citizen-rep", "wp_west_inspection", new Vector3(-7.5f, 0.0f, 26.0f), "wp_patricia_desk", new Vector3(-10.8f, 0.0f, 18.55f), new Vector3(-11.25f, 0.0f, 18.55f)),
                ("valeria-optica-attendant", "wp_optica_workshop", new Vector3(-9.5f, 0.2f, 8.0f), "wp_valeria_storefront", new Vector3(-10.5f, 1.0f, 1.5f), new Vector3(-10.5f, 1.0f, 1.5f)),
                ("hector-artisan", "wp_hector_bench", new Vector3(15.2f, 0.46f, 50.5f), "wp_hector_stand", new Vector3(15.2f, 0.44f, 49.0f), new Vector3(15.2f, 0.44f, 49.0f)),
                ("carlos-farmacia-attendant", "wp_east_dining_benches", new Vector3(8.5f, 0.2f, 22.0f), "wp_carlos_counter", new Vector3(10.5f, 1.0f, 0.0f), new Vector3(10.5f, 1.0f, 0.0f))
            };

            bool allSixTracked = true;
            bool fixedStructuresStayedAnchoredDuringAway = true;
            foreach (var mc in movementCases)
            {
                Transform vis = movCtrl.GetNpcVisualTransform(mc.npcId);
                if (vis == null) { allSixTracked = false; break; }

                movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
                {
                    npcId = mc.npcId,
                    movementState = "moving",
                    currentWaypointId = mc.awayWp,
                    position = new Vector3Dto { x = mc.awayPos.x, y = mc.awayPos.y, z = mc.awayPos.z },
                    forwardHeading = new HeadingDto { x = 0f, z = 1f }
                }, snapImmediately: false);
                for (int i = 0; i < 160; i++) movCtrl.StepVisualMovement(0.05f);
                if (Vector2.Distance(new Vector2(vis.position.x, vis.position.z), new Vector2(mc.awayPos.x, mc.awayPos.z)) > 0.25f)
                {
                    Debug.LogError($"  [Increment5200Verification] Away mismatch for {mc.npcId}: actual=({vis.position.x:F2}, {vis.position.z:F2}) vs expected=({mc.awayPos.x:F2}, {mc.awayPos.z:F2})");
                    allSixTracked = false;
                }
                if ((citizenDesk != null && Vector3.Distance(citizenDesk.position, citizenDeskBefore) > 0.001f) ||
                    (benavidesStore != null && Vector3.Distance(benavidesStore.position, benavidesBefore) > 0.001f) ||
                    (opticaStore != null && Vector3.Distance(opticaStore.position, opticaBefore) > 0.001f))
                {
                    fixedStructuresStayedAnchoredDuringAway = false;
                }

                movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
                {
                    npcId = mc.npcId,
                    movementState = "stationary",
                    currentWaypointId = mc.homeWp,
                    position = new Vector3Dto { x = mc.homeApiPos.x, y = mc.homeApiPos.y, z = mc.homeApiPos.z },
                    forwardHeading = new HeadingDto { x = 0f, z = 1f }
                }, snapImmediately: true);
                if (Vector2.Distance(new Vector2(vis.position.x, vis.position.z), new Vector2(mc.homeExpectedVisualPos.x, mc.homeExpectedVisualPos.z)) > 0.1f)
                {
                    Debug.LogError($"  [Increment5200Verification] Return mismatch for {mc.npcId}: actual=({vis.position.x:F2}, {vis.position.z:F2}) vs expected=({mc.homeExpectedVisualPos.x:F2}, {mc.homeExpectedVisualPos.z:F2})");
                    allSixTracked = false;
                }
            }
            AssertCheck("All remaining 6 NPCs track away destination + home return while CitizenDesk & Storefronts remain anchored",
                allSixTracked &&
                fixedStructuresStayedAnchoredDuringAway &&
                (citizenDesk == null || Vector3.Distance(citizenDesk.position, citizenDeskBefore) < 0.001f) &&
                (benavidesStore == null || Vector3.Distance(benavidesStore.position, benavidesBefore) < 0.001f) &&
                (opticaStore == null || Vector3.Distance(opticaStore.position, opticaBefore) < 0.001f));

            if (createdTempMovCtrl && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5200Verification] Completed: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
