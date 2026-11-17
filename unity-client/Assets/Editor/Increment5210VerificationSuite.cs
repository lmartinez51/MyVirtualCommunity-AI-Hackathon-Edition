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
    /// Automated Unity Editor / Batchmode verification suite for Increment 5.21.0
    /// (Autonomous NPC Social Interaction Lifecycle & Conversation Behavior).
    /// </summary>
    public static class Increment5210VerificationSuite
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

        [MenuItem("MyVirtualCommunity/Run Increment 5.21.0 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5210Verification] Starting Increment 5.21.0 Autonomous Social Interaction Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5210Verification] FAIL: Could not open {ScenePath}");
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

            // 1. Discover scene controllers (bootstrap temporary runtime controller if EditMode scene creates it lazily via FirstPersonPlayer)
            var movementCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTempMovCtrl = false;
            if (movementCtrl == null)
            {
                var tempGo = new GameObject("Temp_NpcWaypointMovementController_5210");
                movementCtrl = tempGo.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            AssertCheck("NpcWaypointMovementController available in scene", movementCtrl != null);

            movementCtrl.DiscoverSceneReferences();

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
                var ctrl = movementCtrl.GetNpcAnimationController(npcId);
                if (ctrl != null) ctrl.InitializeIfNeeded(forceRecachePoses: true);
                AssertCheck($"Canonical NPC {npcId} has InteractionPoint and NpcAnimationController",
                    pointsByNpc.ContainsKey(npcId) && ctrl != null);
            }

            // 2. Verify DTO deserialization for aggregate interaction-context + social-interactions payloads
            string jsonPayload = @"{
                ""success"": true,
                ""evaluatedAt"": ""2026-11-04T17:00:10.000Z"",
                ""timeZone"": ""America/Ciudad_Juarez"",
                ""localCivilTime"": ""2026-11-04T10:00:10"",
                ""sessions"": [
                    {
                        ""sessionId"": ""soc_hector-artisan_sofia-cross-border-retail_1793811600"",
                        ""participantNpcIds"": [""hector-artisan"", ""sofia-cross-border-retail""],
                        ""interactionType"": ""community_queue_exchange"",
                        ""phase"": ""engaged"",
                        ""isActive"": true,
                        ""startedAt"": ""2026-11-04T17:00:00.000Z"",
                        ""expectedEndAt"": ""2026-11-04T17:00:36.000Z"",
                        ""cooldownEndsAt"": ""2026-11-04T17:01:00.000Z"",
                        ""turnIndex"": 1,
                        ""activeSpeakerNpcId"": ""sofia-cross-border-retail"",
                        ""activeListenerNpcId"": ""hector-artisan"",
                        ""distanceMeters"": 3.5
                    }
                ],
                ""npcsList"": [
                    {
                        ""npcId"": ""sofia-cross-border-retail"",
                        ""canonicalName"": ""Sofía Alarcón"",
                        ""actorArchetype"": ""community_player_proxy"",
                        ""hasEmploymentSchedule"": false,
                        ""canEnterOffDuty"": false,
                        ""isWitness"": true,
                        ""witnessClassification"": ""community_queue_witness"",
                        ""witnessId"": ""sofia-cross-border-retail"",
                        ""inActiveInteraction"": true,
                        ""sessionId"": ""soc_hector-artisan_sofia-cross-border-retail_1793811600"",
                        ""phase"": ""engaged"",
                        ""interactionType"": ""community_queue_exchange"",
                        ""partnerNpcId"": ""hector-artisan"",
                        ""conversationalRole"": ""speaker"",
                        ""turnIndex"": 1,
                        ""sessionStartedAt"": ""2026-11-04T17:00:00.000Z"",
                        ""sessionExpectedEndAt"": ""2026-11-04T17:00:36.000Z"",
                        ""cooldownUntil"": ""2026-11-04T17:01:00.000Z"",
                        ""inCooldown"": false,
                        ""eligibleForSocialInteraction"": true,
                        ""supersededByPlayerPriority"": false
                    },
                    {
                        ""npcId"": ""hector-artisan"",
                        ""canonicalName"": ""Don Héctor Méndez"",
                        ""actorArchetype"": ""community_player_proxy"",
                        ""hasEmploymentSchedule"": false,
                        ""canEnterOffDuty"": false,
                        ""isWitness"": true,
                        ""witnessClassification"": ""community_queue_witness"",
                        ""witnessId"": ""hector-artisan"",
                        ""inActiveInteraction"": true,
                        ""sessionId"": ""soc_hector-artisan_sofia-cross-border-retail_1793811600"",
                        ""phase"": ""engaged"",
                        ""interactionType"": ""community_queue_exchange"",
                        ""partnerNpcId"": ""sofia-cross-border-retail"",
                        ""conversationalRole"": ""listener"",
                        ""turnIndex"": 1,
                        ""sessionStartedAt"": ""2026-11-04T17:00:00.000Z"",
                        ""sessionExpectedEndAt"": ""2026-11-04T17:00:36.000Z"",
                        ""cooldownUntil"": ""2026-11-04T17:01:00.000Z"",
                        ""inCooldown"": false,
                        ""eligibleForSocialInteraction"": true,
                        ""supersededByPlayerPriority"": false
                    }
                ],
                ""summary"": {
                    ""activeSessionCount"": 1,
                    ""initiatingCount"": 0,
                    ""engagedCount"": 1,
                    ""closingCount"": 0,
                    ""completedCount"": 0,
                    ""participatingNpcCount"": 2,
                    ""cooldownNpcCount"": 0,
                    ""totalCount"": 2
                }
            }";

            var rosterDto = JsonUtility.FromJson<NpcRosterSocialInteractionsResponseDto>(jsonPayload);
            AssertCheck("NpcRosterSocialInteractionsResponseDto deserializes session & participants",
                rosterDto != null && rosterDto.success && rosterDto.sessions != null && rosterDto.sessions.Length == 1);

            // 3. Apply roster without player proximity & confirm root transforms do NOT move
            Vector3 sofiaPosBefore = movementCtrl.GetNpcPosition("sofia-cross-border-retail");
            Vector3 hectorPosBefore = movementCtrl.GetNpcPosition("hector-artisan");
            Vector3 elenaPosBefore = movementCtrl.GetNpcPosition("elena-border-concierge");
            Vector3 patriciaPosBefore = movementCtrl.GetNpcPosition("patricia-citizen-rep");

            movementCtrl.ApplyAuthoritativeSocialInteractionsRoster(rosterDto);

            var sofiaPoint = pointsByNpc["sofia-cross-border-retail"];
            var hectorPoint = pointsByNpc["hector-artisan"];
            var sofiaAnim = movementCtrl.GetNpcAnimationController("sofia-cross-border-retail");
            var hectorAnim = movementCtrl.GetNpcAnimationController("hector-artisan");

            AssertCheck("Sofia InteractionPoint synced to engaged speaker without player nearby",
                sofiaPoint.socialInteractionActive &&
                sofiaPoint.socialInteractionPhase == "engaged" &&
                sofiaPoint.socialInteractionConversationalRole == "speaker" &&
                sofiaPoint.socialInteractionPartnerNpcId == "hector-artisan");
            AssertCheck("Hector InteractionPoint synced to engaged listener without player nearby",
                hectorPoint.socialInteractionActive &&
                hectorPoint.socialInteractionPhase == "engaged" &&
                hectorPoint.socialInteractionConversationalRole == "listener" &&
                hectorPoint.socialInteractionPartnerNpcId == "sofia-cross-border-retail");

            // 4. Step animation lifecycle through Initiating -> Engaged (speaker vs listener differentiation) -> Closing -> Completed -> Clear
            sofiaAnim.ClearSocialAwareness();
            hectorAnim.ClearSocialAwareness();
            sofiaAnim.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_sofia_queue_mid", true, 0f, "community_player_proxy");
            hectorAnim.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_hector_stand", true, 0f, "community_player_proxy");

            Transform sofiaVis = movementCtrl.GetNpcVisualTransform("sofia-cross-border-retail");
            Transform hectorVis = movementCtrl.GetNpcVisualTransform("hector-artisan");

            // Initiating phase
            sofiaAnim.SyncSocialInteraction("sess-1", "initiating", "speaker", 0, "hector-artisan", true, hectorVis);
            hectorAnim.SyncSocialInteraction("sess-1", "initiating", "listener", 0, "sofia-cross-border-retail", true, sofiaVis);
            for (int i = 0; i < 30; i++)
            {
                sofiaAnim.StepPresentation(1f / 30f);
                hectorAnim.StepPresentation(1f / 30f);
            }
            AssertCheck("Both NPCs enter Initiating phase and build smooth blend weight",
                sofiaAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Initiating &&
                hectorAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Initiating &&
                sofiaAnim.SocialConversationBlendWeight > 0.5f);

            // Engaged phase: Speaker (Sofia) vs Listener (Hector) must exhibit distinguishable upper-body / forearm motion
            sofiaAnim.SyncSocialInteraction("sess-1", "engaged", "speaker", 0, "hector-artisan", true, hectorVis);
            hectorAnim.SyncSocialInteraction("sess-1", "engaged", "listener", 0, "sofia-cross-border-retail", true, sofiaVis);
            var sofiaRig = sofiaAnim.GetComponentInChildren<BobbleheadCharacter>();
            var hectorRig = hectorAnim.GetComponentInChildren<BobbleheadCharacter>();
            float sofiaRightArmSpread = 0f;
            float hectorRightArmSpread = 0f;
            float minArmX = 999f, maxArmX = -999f;
            float minHectorArmX = 999f, maxHectorArmX = -999f;
            for (int i = 0; i < 60; i++)
            {
                sofiaAnim.StepPresentation(1f / 30f);
                hectorAnim.StepPresentation(1f / 30f);
                if (sofiaRig != null && sofiaRig.RightArmRoot != null)
                {
                    float rx = Mathf.DeltaAngle(0f, sofiaRig.RightArmRoot.localEulerAngles.x);
                    minArmX = Mathf.Min(minArmX, rx);
                    maxArmX = Mathf.Max(maxArmX, rx);
                }
                if (hectorRig != null && hectorRig.RightArmRoot != null)
                {
                    float hrx = Mathf.DeltaAngle(0f, hectorRig.RightArmRoot.localEulerAngles.x);
                    minHectorArmX = Mathf.Min(minHectorArmX, hrx);
                    maxHectorArmX = Mathf.Max(maxHectorArmX, hrx);
                }
            }
            sofiaRightArmSpread = maxArmX - minArmX;
            hectorRightArmSpread = maxHectorArmX - minHectorArmX;

            AssertCheck("Engaged speaker (Sofia) exhibits broader forearm/arm gesture modulation than calm listener (Hector)",
                sofiaAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Engaged &&
                hectorAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Engaged &&
                sofiaRightArmSpread > hectorRightArmSpread + 2.0f);

            // Closing phase
            sofiaAnim.SyncSocialInteraction("sess-1", "closing", "mutual", 1, "hector-artisan", true, hectorVis);
            hectorAnim.SyncSocialInteraction("sess-1", "closing", "mutual", 1, "sofia-cross-border-retail", true, sofiaVis);
            for (int i = 0; i < 20; i++)
            {
                sofiaAnim.StepPresentation(1f / 30f);
                hectorAnim.StepPresentation(1f / 30f);
            }
            AssertCheck("Both NPCs enter Closing phase and taper conversation blend weight smoothly",
                sofiaAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Closing &&
                sofiaAnim.SocialConversationBlendWeight < 0.6f);

            // Completed phase & clean recovery (zero stale blend weight)
            sofiaAnim.SyncSocialInteraction("sess-1", "completed", "none", 0, "hector-artisan", false, null);
            hectorAnim.SyncSocialInteraction("sess-1", "completed", "none", 0, "sofia-cross-border-retail", false, null);
            for (int i = 0; i < 30; i++)
            {
                sofiaAnim.StepPresentation(1f / 30f);
                hectorAnim.StepPresentation(1f / 30f);
            }
            AssertCheck("Completed phase clears conversational blend cleanly back to zero",
                sofiaAnim.CurrentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Completed &&
                sofiaAnim.SocialConversationBlendWeight < 0.01f &&
                hectorAnim.SocialConversationBlendWeight < 0.01f);

            // 5. Contextual task & off_duty guard preservation (attendant vs proxy)
            var carlosAnim = movementCtrl.GetNpcAnimationController("carlos-farmacia-attendant");
            carlosAnim.SyncPresentationInputs("stationary", "busy", "busy_priority", "busy", "wp_carlos_counter", false, 0f, "business_attendant");
            carlosAnim.SyncSocialInteraction("sess-2", "engaged", "speaker", 0, "elena-border-concierge", true, null);
            for (int i = 0; i < 15; i++) carlosAnim.StepPresentation(1f / 30f);
            AssertCheck("Busy business_attendant retains ContextualActivity and suppresses social gesture blend",
                carlosAnim.CurrentBehaviorState == NpcVisualBehaviorState.ContextualActivity &&
                carlosAnim.SocialConversationBlendWeight < 0.01f);

            // Walking suppresses social conversation
            sofiaAnim.SyncPresentationInputs("moving", "available_in_transit", "in_transit", "moving", "wp_sofia_queue_mid", true, 0.9f, "community_player_proxy");
            sofiaAnim.SyncSocialInteraction("sess-1", "engaged", "speaker", 0, "hector-artisan", true, hectorVis);
            for (int i = 0; i < 15; i++) sofiaAnim.StepPresentation(1f / 30f);
            AssertCheck("Walking priority overrides social gestures while active movement is running",
                sofiaAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking &&
                sofiaAnim.SocialConversationBlendWeight < 0.01f);

            // 6. Verify zero authoritative transform / fixed-anchor mutation from social interactions
            AssertCheck("Social interaction synchronization does not mutate Sofia/Hector/Elena/Patricia world positions",
                Vector3.Distance(movementCtrl.GetNpcPosition("sofia-cross-border-retail"), sofiaPosBefore) < 0.001f &&
                Vector3.Distance(movementCtrl.GetNpcPosition("hector-artisan"), hectorPosBefore) < 0.001f &&
                Vector3.Distance(movementCtrl.GetNpcPosition("elena-border-concierge"), elenaPosBefore) < 0.001f &&
                Vector3.Distance(movementCtrl.GetNpcPosition("patricia-citizen-rep"), patriciaPosBefore) < 0.001f);

            movementCtrl.ClearAllSocialInteractions();
            AssertCheck("ClearAllSocialInteractions resets InteractionPoint fields safely",
                !sofiaPoint.socialInteractionActive && !sofiaPoint.socialInteractionAvailable);

            if (createdTempMovCtrl && movementCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movementCtrl.gameObject);
            }

            Debug.Log($"[Increment5210Verification] Completed: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
