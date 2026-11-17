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
    /// Automated Unity Editor / Batchmode verification suite for Increment 5.19.0
    /// (NPC Animation, Idle Behavior & Living Presence).
    /// </summary>
    public static class Increment5190VerificationSuite
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

        [MenuItem("MyVirtualCommunity/Run Increment 5.19.0 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5190Verification] Starting Increment 5.19.0 NPC Animation & Presence Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5190Verification] FAIL: Could not open {ScenePath}");
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

            // Ensure movement controller discovers all 7 canonical NPCs and binds NpcAnimationController
            var movCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTempMovCtrl = false;
            if (movCtrl == null)
            {
                var go = new GameObject("Temp_NpcWaypointMovementController");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTempMovCtrl = true;
            }
            movCtrl.DiscoverSceneReferences();

            // 1. Asset and Component Integrity for all 7 Canonical NPCs
            var controllers = new Dictionary<string, NpcAnimationController>();
            var distinctHeadYawsAtSample = new HashSet<string>();

            foreach (string npcId in CanonicalNpcIds)
            {
                NpcAnimationController animCtrl = movCtrl.GetNpcAnimationController(npcId);
                Transform visualT = movCtrl.GetNpcVisualTransform(npcId);
                controllers[npcId] = animCtrl;

                AssertCheck($"NPC '{npcId}' animation controller bound", animCtrl != null);
                AssertCheck($"NPC '{npcId}' visual transform resolved", visualT != null);

                if (animCtrl != null)
                {
                    animCtrl.InitializeIfNeeded(forceRecachePoses: true);
                    AssertCheck($"NPC '{npcId}' articulated Bobblehead rig cached", animCtrl.HasArticulatedRig);
                    AssertCheck($"NPC '{npcId}' Neck/HeadHinge bone available", animCtrl.HasHeadBone);

                    animCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "", true, 0f);
                    for (int s = 0; s < 12; s++)
                    {
                        animCtrl.StepPresentation(0.08f);
                    }

                    var bobble = visualT != null ? visualT.GetComponent<BobbleheadCharacter>() : null;
                    if (bobble != null && bobble.HeadHinge != null)
                    {
                        distinctHeadYawsAtSample.Add($"{bobble.HeadHinge.localEulerAngles.y:F2}");
                    }
                }
            }

            AssertCheck(
                $"Idle motion phases desynchronized across population ({distinctHeadYawsAtSample.Count} distinct sample angles)",
                distinctHeadYawsAtSample.Count >= 4);

            // 2. Missing Animator / Root Motion Protection / Non-Rigged Child Fallback
            {
                var tempRoot = new GameObject("Temp_NonRiggedNpc");
                tempRoot.transform.position = new Vector3(1.5f, 0.2f, 2.5f);
                Vector3 initialRootPos = tempRoot.transform.position;
                Quaternion initialRootRot = tempRoot.transform.rotation;

                var tempVisualChild = new GameObject("VisualChild");
                tempVisualChild.transform.SetParent(tempRoot.transform, false);

                var fakeAnimator = tempRoot.AddComponent<Animator>();
                fakeAnimator.applyRootMotion = true;

                var fallbackCtrl = tempRoot.AddComponent<NpcAnimationController>();
                fallbackCtrl.ConfigureRole("fallback-test-npc");
                fallbackCtrl.InitializeIfNeeded(forceRecachePoses: true);
                fallbackCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "", true, 0f);
                for (int i = 0; i < 10; i++)
                {
                    fallbackCtrl.StepPresentation(0.1f);
                }

                AssertCheck("Animator.applyRootMotion forced false on root", !fakeAnimator.applyRootMotion);
                AssertCheck("Root position untouched by animation controller", Vector3.Distance(tempRoot.transform.position, initialRootPos) < 0.0001f);
                AssertCheck("Root rotation untouched by animation controller", Quaternion.Angle(tempRoot.transform.rotation, initialRootRot) < 0.001f);
                AssertCheck("Non-rigged NPC reports HasArticulatedRig == false", !fallbackCtrl.HasArticulatedRig);
                AssertCheck("Non-rigged NPC without head bone keeps head tracking at 0", Mathf.Abs(fallbackCtrl.CurrentHeadYawDegrees) < 0.001f);

                UnityEngine.Object.DestroyImmediate(tempRoot);
            }

            // 3. Authoritative Locomotion, Detached Visual Following & Hysteresis
            {
                var carlosCtrl = controllers["carlos-farmacia-attendant"];
                Transform carlosVisual = movCtrl.GetNpcVisualTransform("carlos-farmacia-attendant");
                Vector3 carlosStartVisualPos = carlosVisual.position;

                var carlosMoveSnap = new NpcMovementSnapshotDto
                {
                    npcId = "carlos-farmacia-attendant",
                    movementState = "moving",
                    currentWaypointId = "wp_east_sidewalk_mid",
                    destinationWaypointId = "wp_east_dining_benches",
                    activeRouteId = "route_carlos_to_lunch",
                    position = new Vector3Dto { x = 7.6f, y = 0.2f, z = 12.0f },
                    forwardHeading = new HeadingDto { x = 0f, z = 1f },
                    progression = 0.5f,
                    activityState = "busy"
                };
                movCtrl.ApplySingleNpcSnapshot(carlosMoveSnap, snapImmediately: true);
                for (int i = 0; i < 10; i++)
                {
                    carlosCtrl.StepPresentation(0.08f);
                }

                AssertCheck("Carlos detached visual transform follows sidewalk movement", Vector3.Distance(carlosVisual.position, new Vector3(7.6f, 0.2f, 12.0f)) < 0.1f && Vector3.Distance(carlosVisual.position, carlosStartVisualPos) > 1.0f);
                AssertCheck("Carlos switches to Walking state during authoritative movement", carlosCtrl.IsWalkingActive && carlosCtrl.CurrentBehaviorState == NpcVisualBehaviorState.Walking);
                AssertCheck("Carlos locomotion blend rises smoothly toward 1.0", carlosCtrl.LocomotionBlendWeight > 0.7f);
                AssertCheck("Authoritative GetNpcPosition remains exact server target", Vector3.Distance(movCtrl.GetNpcPosition("carlos-farmacia-attendant"), new Vector3(7.6f, 0.2f, 12.0f)) < 0.01f);

                var carlosBobble = carlosVisual.GetComponent<BobbleheadCharacter>();
                float movingLegAngle = carlosBobble != null && carlosBobble.LeftLegRoot != null
                    ? Quaternion.Angle(Quaternion.identity, carlosBobble.LeftLegRoot.localRotation)
                    : 0f;
                AssertCheck("Walking gait drives articulated leg swing", movingLegAngle > 1.0f);

                // Brief single-tick drop below threshold inside exit grace window must NOT flicker to idle
                carlosCtrl.SyncPresentationInputs("stationary", "available_contextual", "break_or_dwell", "busy", "wp_east_dining_benches", true, 0.02f);
                carlosCtrl.StepPresentation(0.04f);
                AssertCheck("Movement hysteresis prevents immediate flicker on brief sub-threshold tick", carlosCtrl.IsWalkingActive);

                // Sustained stationary at benches settles into contextual break state
                for (int i = 0; i < 12; i++)
                {
                    carlosCtrl.StepPresentation(0.08f);
                }
                AssertCheck("Carlos transitions smoothly back from walking when sustained stationary", !carlosCtrl.IsWalkingActive);
                AssertCheck("Carlos at wp_east_dining_benches uses ContextualActivity posture", carlosCtrl.CurrentBehaviorState == NpcVisualBehaviorState.ContextualActivity);
            }

            // 4. Contextual States & Greeting Priority
            {
                var elenaCtrl = controllers["elena-border-concierge"];
                Transform elenaVisual = movCtrl.GetNpcVisualTransform("elena-border-concierge");
                var elenaPt = GameObject.Find("Border_Information_Kiosk")?.GetComponent<InteractionPoint>();
                Vector3 kioskPosBefore = elenaPt != null ? elenaPt.transform.position : Vector3.zero;
                Quaternion kioskRotBefore = elenaPt != null ? elenaPt.transform.rotation : Quaternion.identity;

                elenaCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_elena_kiosk", true, 0f);
                elenaCtrl.StepPresentation(0.1f);
                AssertCheck("Elena stationary available_normal maps to AttentiveService", elenaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.AttentiveService);

                elenaCtrl.TriggerGreetingCue();
                elenaCtrl.StepPresentation(0.05f);
                AssertCheck("Elena stationary authorized dialogue triggers GreetingCue", elenaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.GreetingCue);

                // Authoritative movement overrides transient greeting
                elenaCtrl.SyncPresentationInputs("moving", "available_in_transit", "in_transit", "available", "wp_elena_kiosk", true, 0.5f);
                elenaCtrl.StepPresentation(0.08f);
                AssertCheck("Walking priority overrides active GreetingCue without conflict", elenaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.Walking);

                // Return Elena stationary to kiosk and test off_duty & Head Tracking
                for (int i = 0; i < 8; i++)
                {
                    elenaCtrl.SyncPresentationInputs("stationary", "off_duty", "off_duty_courtesy", "off_duty", "wp_elena_kiosk", false, 0f);
                    elenaCtrl.StepPresentation(0.08f);
                }
                AssertCheck("Off-duty maps cleanly to RestingOrOffDuty state", elenaCtrl.CurrentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty);

                // 5. Constrained Head Orientation toward nearby player vs behind/out-of-range
                var tempPlayer = new GameObject("Temp_PlayerHeadTarget");
                elenaCtrl.SetPlayerLookTarget(tempPlayer.transform);

                // Restore available_normal so gaze activates
                elenaCtrl.SyncPresentationInputs("stationary", "available_normal", "normal_service", "available", "wp_elena_kiosk", true, 0f);

                // Place player 3m ahead-right in front of Elena's visual forward
                Vector3 frontTarget = elenaVisual.position + elenaVisual.forward * 3.0f + elenaVisual.right * 1.4f + Vector3.up * 1.55f;
                tempPlayer.transform.position = frontTarget;
                for (int i = 0; i < 20; i++)
                {
                    elenaCtrl.StepPresentation(0.08f);
                }

                AssertCheck(
                    $"Head orientation tracks front player smoothly within clamp ({elenaCtrl.CurrentHeadYawDegrees:F1} deg)",
                    elenaCtrl.CurrentHeadYawDegrees > 8f && elenaCtrl.CurrentHeadYawDegrees <= 42.1f);
                AssertCheck(
                    "Head tracking does not rotate authoritative InteractionPoint kiosk root",
                    elenaPt != null &&
                    Vector3.Distance(elenaPt.transform.position, kioskPosBefore) < 0.0001f &&
                    Quaternion.Angle(elenaPt.transform.rotation, kioskRotBefore) < 0.001f);

                // Move player behind Elena's back -> should smoothly return to neutral 0
                tempPlayer.transform.position = elenaVisual.position - elenaVisual.forward * 3.0f + Vector3.up * 1.55f;
                for (int i = 0; i < 25; i++)
                {
                    elenaCtrl.StepPresentation(0.08f);
                }
                AssertCheck(
                    $"Head returns smoothly to neutral when player moves behind NPC ({elenaCtrl.CurrentHeadYawDegrees:F1} deg)",
                    Mathf.Abs(elenaCtrl.CurrentHeadYawDegrees) < 2.0f);

                elenaCtrl.SetPlayerLookTarget(null);
                UnityEngine.Object.DestroyImmediate(tempPlayer);
            }

            if (createdTempMovCtrl && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5190Verification] SUMMARY: {passed}/{total} checks passed.");
            if (passed == total)
            {
                Debug.Log("[Increment5190Verification] PASS: All Increment 5.19.0 checks succeeded.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Increment5190Verification] FAIL: {total - passed} checks failed.");
                EditorApplication.Exit(1);
            }
        }
    }
}
