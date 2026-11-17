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
    /// Automated Unity Editor / Batchmode verification suite for Increment 5.22.0
    /// (Autonomous NPC Locomotion, Bridge Patrols & Bounded Business Activity).
    /// </summary>
    public static class Increment5220VerificationSuite
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

        [MenuItem("MyVirtualCommunity/Run Increment 5.22.0 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5220Verification] Starting Increment 5.22.0 Autonomous Locomotion, Bridge Patrols & Bounded Business Activity Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5220Verification] FAIL: Could not open {ScenePath}");
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
                var tempGo = new GameObject("Temp_NpcWaypointMovementController_5220");
                movCtrl = tempGo.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            AssertCheck("NpcWaypointMovementController available in scene", movCtrl != null);

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
                var ctrl = movCtrl.GetNpcAnimationController(npcId);
                if (ctrl != null) ctrl.InitializeIfNeeded(forceRecachePoses: true);
                AssertCheck($"Canonical NPC {npcId} bound with InteractionPoint & NpcAnimationController",
                    pointsByNpc.ContainsKey(npcId) && ctrl != null);
            }

            // Record fixed world structure anchors before any movement updates
            Transform borderKiosk = GameObject.Find("Border_Information_Kiosk")?.transform;
            Transform citizenDesk = GameObject.Find("CitizenServiceDesk")?.transform;
            Transform benavidesStore = GameObject.Find("Storefront_FarmaciasBenavides")?.transform;
            Transform opticaStore = GameObject.Find("Storefront_OpticaFashion")?.transform;

            Vector3 borderKioskBefore = borderKiosk != null ? borderKiosk.position : Vector3.zero;
            Vector3 citizenDeskBefore = citizenDesk != null ? citizenDesk.position : Vector3.zero;
            Vector3 benavidesBefore = benavidesStore != null ? benavidesStore.position : Vector3.zero;
            Vector3 opticaBefore = opticaStore != null ? opticaStore.position : Vector3.zero;

            // 1. Verify DTO deserialization & roster application for Bridge Patrol + Business Autonomy + Excluded Fixed Anchors
            string rosterJson = @"{
                ""success"": true,
                ""evaluatedAt"": ""2026-11-04T17:12:30.000Z"",
                ""timeZone"": ""America/Ciudad_Juarez"",
                ""localCivilTime"": ""2026-11-04T10:12:30"",
                ""summary"": {
                    ""stationaryCount"": 3,
                    ""movingCount"": 4,
                    ""arrivedCount"": 0,
                    ""waitingCount"": 0,
                    ""totalCount"": 7
                },
                ""npcsList"": [
                    {
                        ""npcId"": ""mateo-student"",
                        ""canonicalName"": ""Mateo Navarro"",
                        ""actorArchetype"": ""community_player_proxy"",
                        ""isWitness"": true,
                        ""witnessClassification"": ""community_queue_witness"",
                        ""witnessId"": ""mateo-student"",
                        ""movementState"": ""moving"",
                        ""currentWaypointId"": ""wp_mateo_queue_entry"",
                        ""destinationWaypointId"": ""wp_bridge_north_overlook"",
                        ""activeRouteId"": ""patrol_mateo-student_northbound"",
                        ""position"": { ""x"": 15.42, ""y"": 0.36, ""z"": 44.2 },
                        ""forwardHeading"": { ""x"": 0.0, ""z"": 1.0 },
                        ""progression"": 0.42,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""bridge_patrol"",
                        ""autonomousBehaviorState"": ""patrolling"",
                        ""controlMode"": ""autonomous_bridge_patrol"",
                        ""patrolDirection"": ""northbound"",
                        ""patrolCycleIndex"": 3,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": ""domain:puente-paso-del-norte-pedestrian-concourse"",
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""sofia-cross-border-retail"",
                        ""canonicalName"": ""Sofía Alarcón"",
                        ""actorArchetype"": ""community_player_proxy"",
                        ""isWitness"": true,
                        ""witnessClassification"": ""community_queue_witness"",
                        ""witnessId"": ""sofia-cross-border-retail"",
                        ""movementState"": ""moving"",
                        ""currentWaypointId"": ""wp_sofia_queue_mid"",
                        ""destinationWaypointId"": ""wp_bridge_south_approach"",
                        ""activeRouteId"": ""patrol_sofia-cross-border-retail_southbound"",
                        ""position"": { ""x"": 14.98, ""y"": 0.31, ""z"": 41.0 },
                        ""forwardHeading"": { ""x"": 0.0, ""z"": -1.0 },
                        ""progression"": 0.55,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""bridge_patrol"",
                        ""autonomousBehaviorState"": ""patrolling"",
                        ""controlMode"": ""autonomous_bridge_patrol"",
                        ""patrolDirection"": ""southbound"",
                        ""patrolCycleIndex"": 2,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": ""domain:puente-paso-del-norte-pedestrian-concourse"",
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""hector-artisan"",
                        ""canonicalName"": ""Don Héctor Méndez"",
                        ""actorArchetype"": ""community_player_proxy"",
                        ""isWitness"": true,
                        ""witnessClassification"": ""community_queue_witness"",
                        ""witnessId"": ""hector-artisan"",
                        ""movementState"": ""stationary"",
                        ""currentWaypointId"": ""wp_hector_bench"",
                        ""destinationWaypointId"": null,
                        ""activeRouteId"": null,
                        ""position"": { ""x"": 15.2, ""y"": 0.464, ""z"": 50.5 },
                        ""forwardHeading"": { ""x"": 0.0, ""z"": -1.0 },
                        ""progression"": 0.0,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""bridge_patrol"",
                        ""autonomousBehaviorState"": ""observing"",
                        ""controlMode"": ""autonomous_bridge_patrol"",
                        ""patrolDirection"": ""southbound"",
                        ""patrolCycleIndex"": 1,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": ""domain:puente-paso-del-norte-pedestrian-concourse"",
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""carlos-farmacia-attendant"",
                        ""canonicalName"": ""Carlos Méndez"",
                        ""actorArchetype"": ""business_attendant"",
                        ""isWitness"": false,
                        ""witnessClassification"": ""none"",
                        ""witnessId"": null,
                        ""movementState"": ""moving"",
                        ""currentWaypointId"": ""wp_farmacia_display_north"",
                        ""destinationWaypointId"": ""wp_farmacia_display_north"",
                        ""activeRouteId"": ""local_carlos-farmacia-attendant_to_wp_farmacia_display_north"",
                        ""position"": { ""x"": 10.2, ""y"": 0.2, ""z"": 2.4 },
                        ""forwardHeading"": { ""x"": -0.1, ""z"": 0.99 },
                        ""progression"": 0.75,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""business_local_autonomy"",
                        ""autonomousBehaviorState"": ""local_roaming"",
                        ""controlMode"": ""autonomous_business_local"",
                        ""patrolDirection"": ""none"",
                        ""patrolCycleIndex"": 4,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": ""domain:farmacias-benavides"",
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""valeria-optica-attendant"",
                        ""canonicalName"": ""Valeria Domínguez"",
                        ""actorArchetype"": ""business_attendant"",
                        ""isWitness"": false,
                        ""witnessClassification"": ""none"",
                        ""witnessId"": null,
                        ""movementState"": ""moving"",
                        ""currentWaypointId"": ""wp_optica_display_south"",
                        ""destinationWaypointId"": ""wp_optica_display_south"",
                        ""activeRouteId"": ""local_valeria-optica-attendant_to_wp_optica_display_south"",
                        ""position"": { ""x"": -10.2, ""y"": 0.2, ""z"": -1.5 },
                        ""forwardHeading"": { ""x"": 0.1, ""z"": -0.99 },
                        ""progression"": 0.80,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""business_local_autonomy"",
                        ""autonomousBehaviorState"": ""local_roaming"",
                        ""controlMode"": ""autonomous_business_local"",
                        ""patrolDirection"": ""none"",
                        ""patrolCycleIndex"": 3,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": ""domain:optica-fashion"",
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""elena-border-concierge"",
                        ""canonicalName"": ""Elena Ramos"",
                        ""actorArchetype"": ""public_servant"",
                        ""isWitness"": false,
                        ""witnessClassification"": ""none"",
                        ""witnessId"": null,
                        ""movementState"": ""stationary"",
                        ""currentWaypointId"": ""wp_elena_kiosk"",
                        ""destinationWaypointId"": null,
                        ""activeRouteId"": null,
                        ""position"": { ""x"": 19.2, ""y"": 0.2, ""z"": 39.5 },
                        ""forwardHeading"": { ""x"": 0.0, ""z"": 1.0 },
                        ""progression"": 0.0,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""fixed_anchor_scheduled"",
                        ""autonomousBehaviorState"": ""fixed_station"",
                        ""controlMode"": ""fixed_station_anchor"",
                        ""patrolDirection"": ""none"",
                        ""patrolCycleIndex"": 0,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": null,
                        ""withinAuthorizedDomain"": true
                    },
                    {
                        ""npcId"": ""patricia-citizen-rep"",
                        ""canonicalName"": ""Lic. Patricia Morales"",
                        ""actorArchetype"": ""public_servant"",
                        ""isWitness"": false,
                        ""witnessClassification"": ""none"",
                        ""witnessId"": null,
                        ""movementState"": ""stationary"",
                        ""currentWaypointId"": ""wp_patricia_desk"",
                        ""destinationWaypointId"": null,
                        ""activeRouteId"": null,
                        ""position"": { ""x"": -10.8, ""y"": 0.0, ""z"": 18.55 },
                        ""forwardHeading"": { ""x"": 0.0, ""z"": 1.0 },
                        ""progression"": 0.0,
                        ""activityState"": ""available"",
                        ""behaviorProfileType"": ""fixed_anchor_scheduled"",
                        ""autonomousBehaviorState"": ""fixed_station"",
                        ""controlMode"": ""fixed_station_anchor"",
                        ""patrolDirection"": ""none"",
                        ""patrolCycleIndex"": 0,
                        ""isSuspendedByPlayerInteraction"": false,
                        ""businessDomainId"": null,
                        ""withinAuthorizedDomain"": true
                    }
                ]
            }";

            var rosterDto = JsonUtility.FromJson<NpcRosterMovementResponseDto>(rosterJson);
            AssertCheck("Increment 5.22.0 NpcRosterMovementResponseDto deserialized cleanly",
                rosterDto != null && rosterDto.success && rosterDto.npcsList != null && rosterDto.npcsList.Length == 7);

            movCtrl.ApplyAuthoritativeRoster(rosterDto, snapImmediately: false);
            for (int i = 0; i < 40; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                foreach (var npcId in CanonicalNpcIds)
                {
                    movCtrl.GetNpcAnimationController(npcId)?.StepPresentation(0.05f);
                }
            }

            var mateoPt = pointsByNpc["mateo-student"];
            var sofiaPt = pointsByNpc["sofia-cross-border-retail"];
            var hectorPt = pointsByNpc["hector-artisan"];
            var carlosPt = pointsByNpc["carlos-farmacia-attendant"];
            var valeriaPt = pointsByNpc["valeria-optica-attendant"];
            var elenaPt = pointsByNpc["elena-border-concierge"];
            var patriciaPt = pointsByNpc["patricia-citizen-rep"];

            var mateoAnim = movCtrl.GetNpcAnimationController("mateo-student");
            var sofiaAnim = movCtrl.GetNpcAnimationController("sofia-cross-border-retail");
            var hectorAnim = movCtrl.GetNpcAnimationController("hector-artisan");
            var carlosAnim = movCtrl.GetNpcAnimationController("carlos-farmacia-attendant");
            var valeriaAnim = movCtrl.GetNpcAnimationController("valeria-optica-attendant");

            AssertCheck("Mateo, Sofia, and Hector retain community_player_proxy, Witness status, and bridge_patrol profile",
                mateoPt.IsCommunityPlayerProxy() && mateoPt.isWitness && !mateoPt.canEnterOffDuty && mateoPt.behaviorProfileType == "bridge_patrol" &&
                sofiaPt.IsCommunityPlayerProxy() && sofiaPt.isWitness && !sofiaPt.canEnterOffDuty && sofiaPt.behaviorProfileType == "bridge_patrol" &&
                hectorPt.IsCommunityPlayerProxy() && hectorPt.isWitness && !hectorPt.canEnterOffDuty && hectorPt.behaviorProfileType == "bridge_patrol");

            AssertCheck("Mateo (northbound) and Sofia (southbound) exhibit active Walking animation while patrolling in opposite directions",
                mateoPt.patrolDirection == "northbound" &&
                sofiaPt.patrolDirection == "southbound" &&
                mateoAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking &&
                sofiaAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking);

            AssertCheck("Hector in stationary observing state remains in AttentiveService observation posture",
                hectorPt.autonomousBehaviorState == "observing" &&
                hectorAnim.CurrentBehaviorState == NpcVisualBehaviorState.AttentiveService &&
                !hectorAnim.IsWalkingActive);

            AssertCheck("Carlos and Valeria local_roaming stays strictly inside verified business domains and drives Walking animation",
                carlosPt.behaviorProfileType == "business_local_autonomy" &&
                carlosPt.withinAuthorizedDomain &&
                carlosAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking &&
                valeriaPt.behaviorProfileType == "business_local_autonomy" &&
                valeriaPt.withinAuthorizedDomain &&
                valeriaAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking);

            // 2. Verify Passing Encounter presentation between Mateo and Sofia
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "mateo-student",
                movementState = "stationary",
                currentWaypointId = "wp_sofia_queue_mid",
                position = new Vector3Dto { x = 15.42f, y = 0.36f, z = 44.5f },
                forwardHeading = new HeadingDto { x = -0.24f, z = 0.97f },
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "passing_encounter",
                controlMode = "autonomous_bridge_patrol",
                patrolDirection = "northbound",
                passingEncounterPartnerNpcId = "sofia-cross-border-retail",
                passingEncounterType = "passing_greeting",
                withinAuthorizedDomain = true
            }, snapImmediately: true);
            for (int i = 0; i < 15; i++) mateoAnim.StepPresentation(0.05f);
            AssertCheck("Passing encounter stops walking and transitions Mateo into stationary greeting exchange",
                mateoAnim.CurrentAutonomousBehaviorState == "passing_encounter" &&
                !mateoAnim.IsWalkingActive &&
                mateoPt.passingEncounterPartnerNpcId == "sofia-cross-border-retail");

            // 3. Verify Player Interaction Suspension & Resume on Mateo (Subsystem A3)
            Vector3 mateoPausedTarget = new Vector3(15.42f, 0.36f, 44.5f);
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "mateo-student",
                movementState = "stationary",
                currentWaypointId = "wp_sofia_queue_mid",
                destinationWaypointId = "wp_bridge_north_overlook",
                activeRouteId = "patrol_mateo-student_northbound",
                position = new Vector3Dto { x = mateoPausedTarget.x, y = mateoPausedTarget.y, z = mateoPausedTarget.z },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                progression = 0.45f,
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "player_interaction_paused",
                controlMode = "player_interaction_suspended",
                patrolDirection = "northbound",
                isSuspendedByPlayerInteraction = true,
                suspendedByPlayerId = "player-1",
                withinAuthorizedDomain = true
            }, snapImmediately: true);
            for (int i = 0; i < 20; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                mateoAnim.StepPresentation(0.05f);
            }
            AssertCheck("Player interaction suspension keeps Mateo stationary in AttentiveService and suppresses social conversation",
                movCtrl.IsNpcSuspendedByPlayerInteraction("mateo-student") &&
                !mateoAnim.IsWalkingActive &&
                mateoAnim.CurrentBehaviorState == NpcVisualBehaviorState.AttentiveService &&
                mateoAnim.SocialConversationBlendWeight < 0.01f &&
                Vector3.Distance(movCtrl.GetNpcPosition("mateo-student"), mateoPausedTarget) < 0.01f);

            // Resume patrol from exact preserved position
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "mateo-student",
                movementState = "moving",
                currentWaypointId = "wp_sofia_queue_mid",
                destinationWaypointId = "wp_bridge_north_overlook",
                activeRouteId = "patrol_mateo-student_northbound",
                position = new Vector3Dto { x = mateoPausedTarget.x, y = mateoPausedTarget.y, z = mateoPausedTarget.z + 0.5f },
                forwardHeading = new HeadingDto { x = 0f, z = 1f },
                progression = 0.48f,
                behaviorProfileType = "bridge_patrol",
                autonomousBehaviorState = "resuming",
                controlMode = "autonomous_bridge_patrol",
                patrolDirection = "northbound",
                isSuspendedByPlayerInteraction = false,
                withinAuthorizedDomain = true
            }, snapImmediately: false);
            for (int i = 0; i < 20; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                mateoAnim.StepPresentation(0.05f);
            }
            AssertCheck("Releasing player interaction resumes Mateo walking smoothly along preserved northbound patrol route",
                !movCtrl.IsNpcSuspendedByPlayerInteraction("mateo-student") &&
                mateoAnim.CurrentAutonomousBehaviorState == "resuming" &&
                mateoAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking);

            // 4. Verify Business Attendant Customer Interaction Suspension & Off-Duty Guard (Subsystem B)
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "carlos-farmacia-attendant",
                movementState = "stationary",
                currentWaypointId = "wp_carlos_counter",
                position = new Vector3Dto { x = 10.5f, y = 1.0f, z = 0.0f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "customer_interaction",
                controlMode = "player_interaction_suspended",
                isSuspendedByPlayerInteraction = true,
                businessDomainId = "domain:farmacias-benavides",
                withinAuthorizedDomain = true
            }, snapImmediately: true);
            for (int i = 0; i < 15; i++) carlosAnim.StepPresentation(0.05f);
            AssertCheck("Customer interaction suspends Carlos local roaming and enters AttentiveService posture",
                carlosAnim.IsSuspendedByPlayerInteraction &&
                !carlosAnim.IsWalkingActive &&
                carlosAnim.CurrentBehaviorState == NpcVisualBehaviorState.AttentiveService);

            // Off-duty Valeria enters RestingOrOffDuty and does not roam
            valeriaPt.interactionAvailability = "off_duty";
            valeriaPt.interactionMode = "off_duty_courtesy";
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                movementState = "stationary",
                currentWaypointId = "wp_valeria_storefront",
                position = new Vector3Dto { x = -10.5f, y = 1.0f, z = 1.5f },
                forwardHeading = new HeadingDto { x = 1f, z = 0f },
                activityState = "off_duty",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "off_duty",
                controlMode = "fixed_station_anchor",
                isSuspendedByPlayerInteraction = false,
                businessDomainId = "domain:optica-fashion",
                withinAuthorizedDomain = true
            }, snapImmediately: true);
            for (int i = 0; i < 15; i++) valeriaAnim.StepPresentation(0.05f);
            AssertCheck("Off-duty Valeria remains stationary at primary station in RestingOrOffDuty state",
                !valeriaAnim.IsWalkingActive &&
                valeriaAnim.CurrentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty);

            // 5. Verify Excluded Fixed Kiosk NPCs (Elena & Patricia) and Fixed World Structures remain 100% anchored
            AssertCheck("Elena and Patricia retain fixed_anchor_scheduled profile and fixed_station state",
                elenaPt.behaviorProfileType == "fixed_anchor_scheduled" &&
                elenaPt.autonomousBehaviorState == "fixed_station" &&
                patriciaPt.behaviorProfileType == "fixed_anchor_scheduled" &&
                patriciaPt.autonomousBehaviorState == "fixed_station");

            AssertCheck("Fixed scene structures (Border Kiosk, Citizen Desk, Benavides, Optica) remain 100% anchored",
                (borderKiosk == null || Vector3.Distance(borderKiosk.position, borderKioskBefore) < 0.001f) &&
                (citizenDesk == null || Vector3.Distance(citizenDesk.position, citizenDeskBefore) < 0.001f) &&
                (benavidesStore == null || Vector3.Distance(benavidesStore.position, benavidesBefore) < 0.001f) &&
                (opticaStore == null || Vector3.Distance(opticaStore.position, opticaBefore) < 0.001f));

            if (createdTempMovCtrl && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5220Verification] Completed: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
