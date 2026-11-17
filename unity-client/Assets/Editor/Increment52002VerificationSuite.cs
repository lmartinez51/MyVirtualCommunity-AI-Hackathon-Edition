using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity Editor / Batchmode verification suite for Increment 5.20.0.2
    /// (NPC Actor Archetypes, Community Player Proxies & Witness Classification).
    /// </summary>
    public static class Increment52002VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        private static readonly string[] ProxyIds =
        {
            "mateo-student",
            "sofia-cross-border-retail",
            "hector-artisan"
        };

        private static readonly string[] StaffIds =
        {
            "elena-border-concierge",
            "carlos-farmacia-attendant",
            "valeria-optica-attendant",
            "patricia-citizen-rep"
        };

        [MenuItem("MyVirtualCommunity/Run Increment 5.20.0.2 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment52002Verification] Starting Increment 5.20.0.2 Archetype & Witness Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment52002Verification] FAIL: Could not open {ScenePath}");
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
                var go = new GameObject("Temp_NpcWaypointMovementController_52002");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            movCtrl.DiscoverSceneReferences();

            InteractionPoint FindPoint(string npcId)
            {
                var pts = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
                foreach (var pt in pts)
                {
                    if (pt != null && string.Equals(pt.characterId, npcId, StringComparison.OrdinalIgnoreCase))
                    {
                        return pt;
                    }
                }
                return null;
            }

            // 1. Deserialize authoritative DTOs with actorArchetype and witness fields (late-night scenario)
            string rosterJson = @"{
                ""success"": true,
                ""evaluatedAt"": ""2026-11-08T09:30:00.000Z"",
                ""timeZone"": ""America/Ciudad_Juarez"",
                ""localCivilTime"": ""2026-11-08T02:30:00.000-07:00"",
                ""summary"": { ""availableNormalCount"": 3, ""inTransitCount"": 0, ""contextualCount"": 0, ""busyCount"": 0, ""offDutyCount"": 4, ""unavailableCount"": 0, ""totalCount"": 7 },
                ""npcsList"": [
                    { ""npcId"": ""elena-border-concierge"", ""canonicalName"": ""Elena Ramos"", ""actorArchetype"": ""public_servant"", ""hasEmploymentSchedule"": true, ""canEnterOffDuty"": true, ""isWitness"": false, ""witnessClassification"": ""none"", ""witnessId"": null, ""activityState"": ""off_duty"", ""interactionAvailability"": ""off_duty"", ""interactionMode"": ""off_duty_courtesy"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": false, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_elena_kiosk"" },
                    { ""npcId"": ""carlos-farmacia-attendant"", ""canonicalName"": ""Carlos Méndez"", ""actorArchetype"": ""business_attendant"", ""hasEmploymentSchedule"": true, ""canEnterOffDuty"": true, ""isWitness"": false, ""witnessClassification"": ""none"", ""witnessId"": null, ""activityState"": ""off_duty"", ""interactionAvailability"": ""off_duty"", ""interactionMode"": ""off_duty_courtesy"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": false, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_carlos_counter"" },
                    { ""npcId"": ""valeria-optica-attendant"", ""canonicalName"": ""Valeria Domínguez"", ""actorArchetype"": ""business_attendant"", ""hasEmploymentSchedule"": true, ""canEnterOffDuty"": true, ""isWitness"": false, ""witnessClassification"": ""none"", ""witnessId"": null, ""activityState"": ""off_duty"", ""interactionAvailability"": ""off_duty"", ""interactionMode"": ""off_duty_courtesy"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": false, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_valeria_storefront"" },
                    { ""npcId"": ""patricia-citizen-rep"", ""canonicalName"": ""Lic. Patricia Morales"", ""actorArchetype"": ""public_servant"", ""hasEmploymentSchedule"": true, ""canEnterOffDuty"": true, ""isWitness"": false, ""witnessClassification"": ""none"", ""witnessId"": null, ""activityState"": ""off_duty"", ""interactionAvailability"": ""off_duty"", ""interactionMode"": ""off_duty_courtesy"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": false, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_patricia_desk"" },
                    { ""npcId"": ""mateo-student"", ""canonicalName"": ""Mateo Navarro"", ""actorArchetype"": ""community_player_proxy"", ""hasEmploymentSchedule"": false, ""canEnterOffDuty"": false, ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""mateo-student"", ""activityState"": ""available"", ""interactionAvailability"": ""available_normal"", ""interactionMode"": ""normal_service"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": true, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_mateo_queue_entry"", ""socialAwareness"": { ""npcId"": ""mateo-student"", ""actorArchetype"": ""community_player_proxy"", ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""mateo-student"", ""awarenessState"": ""unaware"", ""targetEntityType"": ""none"", ""canOrientTowardTarget"": true, ""socialEligible"": true } },
                    { ""npcId"": ""sofia-cross-border-retail"", ""canonicalName"": ""Sofía Alarcón"", ""actorArchetype"": ""community_player_proxy"", ""hasEmploymentSchedule"": false, ""canEnterOffDuty"": false, ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""sofia-cross-border-retail"", ""activityState"": ""available"", ""interactionAvailability"": ""available_normal"", ""interactionMode"": ""normal_service"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": true, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_sofia_queue_mid"", ""socialAwareness"": { ""npcId"": ""sofia-cross-border-retail"", ""actorArchetype"": ""community_player_proxy"", ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""sofia-cross-border-retail"", ""awarenessState"": ""social_engaged"", ""targetEntityType"": ""npc"", ""targetEntityId"": ""hector-artisan"", ""pairedPartnerNpcId"": ""hector-artisan"", ""canOrientTowardTarget"": true, ""socialEligible"": true } },
                    { ""npcId"": ""hector-artisan"", ""canonicalName"": ""Don Héctor Méndez"", ""actorArchetype"": ""community_player_proxy"", ""hasEmploymentSchedule"": false, ""canEnterOffDuty"": false, ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""hector-artisan"", ""activityState"": ""available"", ""interactionAvailability"": ""available_normal"", ""interactionMode"": ""normal_service"", ""canInitiateConversation"": true, ""isAvailableForNormalService"": true, ""movementState"": ""stationary"", ""currentWaypointId"": ""wp_hector_stand"", ""socialAwareness"": { ""npcId"": ""hector-artisan"", ""actorArchetype"": ""community_player_proxy"", ""isWitness"": true, ""witnessClassification"": ""community_queue_witness"", ""witnessId"": ""hector-artisan"", ""awarenessState"": ""social_engaged"", ""targetEntityType"": ""npc"", ""targetEntityId"": ""sofia-cross-border-retail"", ""pairedPartnerNpcId"": ""sofia-cross-border-retail"", ""canOrientTowardTarget"": true, ""socialEligible"": true } }
                ]
            }";

            var rosterDto = JsonUtility.FromJson<NpcRosterInteractionContextResponseDto>(rosterJson);
            AssertCheck("Roster DTO deserialized 7 entries", rosterDto != null && rosterDto.npcsList != null && rosterDto.npcsList.Length == 7);

            movCtrl.ApplyAuthoritativeInteractionContextRoster(rosterDto);

            // 2. Verify all 3 Community Player Proxies: authoritative metadata, badges, and animation states
            foreach (string id in ProxyIds)
            {
                var pt = FindPoint(id);
                var ctrl = movCtrl.GetNpcAnimationController(id);
                AssertCheck($"Proxy '{id}' InteractionPoint & AnimationController found", pt != null && ctrl != null);
                if (pt == null || ctrl == null) continue;

                AssertCheck($"Proxy '{id}' has actorArchetype == 'community_player_proxy'", pt.actorArchetype == "community_player_proxy");
                AssertCheck($"Proxy '{id}' has isWitness == true & witnessId == '{id}'", pt.isWitness && pt.witnessId == id);
                AssertCheck($"Proxy '{id}' IsCommunityPlayerProxy() == true", pt.IsCommunityPlayerProxy());

                ctrl.InitializeIfNeeded(forceRecachePoses: true);
                ctrl.SyncPresentationInputs(pt.movementState, pt.interactionAvailability, pt.interactionMode, pt.activityState, pt.currentWaypointId, pt.canInteract, 0f, pt.actorArchetype);
                ctrl.StepPresentation(0.1f);
                AssertCheck($"Proxy '{id}' stays active (not RestingOrOffDuty) during late-night roster sync", ctrl.CurrentBehaviorState != NpcVisualBehaviorState.RestingOrOffDuty);

                string badgeEn = pt.GetInteractionAvailabilityBadge("en");
                string badgeEs = pt.GetInteractionAvailabilityBadge("es");
                AssertCheck($"Proxy '{id}' EN badge never displays Off Duty/Closed ({badgeEn})", !badgeEn.Contains("Off Duty") && !badgeEn.Contains("Closed"));
                AssertCheck($"Proxy '{id}' ES badge never displays Fuera de turno/Cerrado ({badgeEs})", !badgeEs.Contains("Fuera de turno") && !badgeEs.Contains("Cerrado"));

                // Guardrail test: even if stale off_duty inputs are fed with community_player_proxy archetype, Unity refuses off-duty UI & posture
                pt.interactionAvailability = "off_duty";
                pt.activityState = "off_duty";
                string guardedEn = pt.GetInteractionAvailabilityBadge("en");
                ctrl.SyncPresentationInputs("stationary", "off_duty", "off_duty_courtesy", "off_duty", pt.currentWaypointId, true, 0f, "community_player_proxy");
                ctrl.StepPresentation(0.1f);
                AssertCheck($"Proxy '{id}' refuses Off Duty badge even on stale input ({guardedEn})", !guardedEn.Contains("Off Duty") && !guardedEn.Contains("Closed"));
                AssertCheck($"Proxy '{id}' refuses RestingOrOffDuty posture even on stale input", ctrl.CurrentBehaviorState != NpcVisualBehaviorState.RestingOrOffDuty);
            }

            // 3. Verify all 4 Business Attendants & Public Servants preserve off_duty posture and labels
            foreach (string id in StaffIds)
            {
                var pt = FindPoint(id);
                var ctrl = movCtrl.GetNpcAnimationController(id);
                AssertCheck($"Staff '{id}' InteractionPoint & AnimationController found", pt != null && ctrl != null);
                if (pt == null || ctrl == null) continue;

                bool expectBusiness = id == "carlos-farmacia-attendant" || id == "valeria-optica-attendant";
                string expectedArch = expectBusiness ? "business_attendant" : "public_servant";
                AssertCheck($"Staff '{id}' has actorArchetype == '{expectedArch}'", pt.actorArchetype == expectedArch);
                AssertCheck($"Staff '{id}' has isWitness == false", !pt.isWitness);
                AssertCheck($"Staff '{id}' IsCommunityPlayerProxy() == false", !pt.IsCommunityPlayerProxy());

                string badgeEn = pt.GetInteractionAvailabilityBadge("en");
                AssertCheck($"Staff '{id}' shows Off Duty badge at 02:30 ({badgeEn})", badgeEn == "[Off Duty]");

                ctrl.InitializeIfNeeded(forceRecachePoses: true);
                ctrl.SyncPresentationInputs("stationary", "off_duty", "off_duty_courtesy", "off_duty", pt.currentWaypointId, true, 0f, pt.actorArchetype);
                ctrl.StepPresentation(0.1f);
                AssertCheck($"Staff '{id}' transitions cleanly to RestingOrOffDuty", ctrl.CurrentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty);
            }

            // 4. Verify Sofia & Hector preserve social_engaged presentation during late-night queue sync
            AssertCheck("Sofia social awareness state == SocialEngaged", movCtrl.GetNpcAwarenessState("sofia-cross-border-retail") == NpcVisualAwarenessState.SocialEngaged);
            AssertCheck("Hector social awareness state == SocialEngaged", movCtrl.GetNpcAwarenessState("hector-artisan") == NpcVisualAwarenessState.SocialEngaged);

            // 5. Verify Unity consumes explicit archetype rather than inferring from NPC display name
            var carlosPt = FindPoint("carlos-farmacia-attendant");
            if (carlosPt != null)
            {
                string origArch = carlosPt.actorArchetype;
                string origAvail = carlosPt.interactionAvailability;
                carlosPt.actorArchetype = "community_player_proxy";
                carlosPt.interactionAvailability = "off_duty";
                AssertCheck("Classification driven strictly by server actorArchetype field, not name/location", carlosPt.IsCommunityPlayerProxy());
                carlosPt.actorArchetype = origArch;
                carlosPt.interactionAvailability = origAvail;
            }

            if (createdTempMovCtrl && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment52002Verification] Completed: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
