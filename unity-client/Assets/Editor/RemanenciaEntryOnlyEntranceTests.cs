using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode & deterministic runtime-coroutine verification suite for
    /// Implementation 07.13 — Remanencia: Check-in Without Conventional Check-out.
    /// Verifies entry-only interior door behavior, preserved hotel glass observation view,
    /// centralized exit-source authorization ("threshold-of-trust" only), anti-softlock
    /// reachability from both entry routes, progression/save safety, and absolute
    /// byte-for-byte preservation of BorderCorridorScene.unity and the exterior door.
    /// </summary>
    [TestFixture]
    public class RemanenciaEntryOnlyEntranceTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;
        private const string ExpectedBorderCorridorSceneSha256 =
            "0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B";

        private static string ComputeFileSha256(string relativeAssetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));
            Assert.IsTrue(File.Exists(fullPath), $"Expected file missing: {fullPath}");
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        [Test]
        public void InteriorDoor_IsVisibleRetainsObservationGlassBlocksPassageAndGivesRestrainedEntryOnlyFeedback()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 1. Backrooms_ExitDoor_ToStreet remains present, active, and visible
            Transform exitDoor = root.transform.Find("Backrooms_ExitDoor_ToStreet");
            Assert.IsNotNull(exitDoor, "Backrooms_ExitDoor_ToStreet must remain present.");
            Assert.IsTrue(exitDoor.gameObject.activeInHierarchy, "Backrooms_ExitDoor_ToStreet must remain visible.");

            // 2. Interior hotel entrance framing & unified Av. Benito Juárez one-way observation view remain intact
            Transform entIllusion = root.transform.Find($"{BackroomsLevel1Builder.HotelRoom111BGuestInteriorRootName}/Room111B_InteriorEntranceIllusion");
            Assert.IsNotNull(entIllusion, "Hotel interior entrance appearance must remain intact.");
            Assert.IsTrue(entIllusion.gameObject.activeInHierarchy);
            RemanenciaOneWayEntranceView oneWayView = entIllusion.GetComponent<RemanenciaOneWayEntranceView>();
            Assert.IsNotNull(oneWayView);
            Assert.IsNotNull(oneWayView.unifiedGlassPane, "Unified observation glass pane must remain wired.");
            Assert.IsTrue(oneWayView.unifiedGlassPane.enabled);
            Material oneWayMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.EntranceOneWayGlassMatPath);
            Assert.AreEqual(oneWayMat, oneWayView.unifiedGlassPane.sharedMaterial);
            Assert.IsNotNull(oneWayMat.GetTexture("_BaseMap"), "Observation pane must retain its street texture.");
            Assert.IsNotNull(entIllusion.Find("InteriorEntrance_HandleL"));
            Assert.IsNotNull(entIllusion.Find("InteriorEntrance_HandleR"));
            Assert.IsNotNull(entIllusion.Find("InteriorEntrance_CenterMullion"));

            // 3 & 8. Entry-only prompts & minimal bilingual feedback (no return promise, no tutorial/spoiler)
            BackroomsExitDoorTrigger trig = exitDoor.GetComponent<BackroomsExitDoorTrigger>();
            Assert.IsNotNull(trig);
            Assert.IsTrue(trig.enabled);
            Assert.IsTrue(trig.isEntryOnly, "BackroomsExitDoorTrigger must default to isEntryOnly = true in 07.13.");

            trig.ResetEntryOnlyFeedbackForTest();
            string idleEs = trig.GetPrompt(isEs: true, overrideNowUnscaledTime: 10f);
            string idleEn = trig.GetPrompt(isEs: false, overrideNowUnscaledTime: 10f);
            Assert.AreEqual("[E] Intentar abrir la puerta", idleEs);
            Assert.AreEqual("[E] Try the door", idleEn);
            Assert.IsFalse(idleEs.Contains("Av. Benito Juárez"));
            Assert.IsFalse(idleEn.Contains("Av. Benito Juárez"));
            Assert.IsFalse(idleEs.Contains("111-B"));
            Assert.IsFalse(idleEn.Contains("111-B"));

            string fbEs = trig.TriggerEntryOnlyFeedback(isEs: true, overrideNowUnscaledTime: 10f);
            Assert.AreEqual("La puerta no cede.", fbEs);
            Assert.AreEqual("La puerta no cede.", trig.GetPrompt(isEs: true, overrideNowUnscaledTime: 11f));
            Assert.AreEqual("The door won't budge.", trig.GetPrompt(isEs: false, overrideNowUnscaledTime: 11f));
            Assert.AreEqual(1, trig.EntryOnlyAttemptCount);
            // Feedback expires cleanly back to restrained prompt after 2.6s
            Assert.AreEqual("[E] Intentar abrir la puerta", trig.GetPrompt(isEs: true, overrideNowUnscaledTime: 13f));
            trig.ResetEntryOnlyFeedbackForTest();

            // 6. Physical door boundary prevents walking through from inside Remanencia (solid Wall_Outer_East behind door)
            Assert.IsTrue(
                Physics.Linecast(new Vector3(18.40f, BaseY + 1.2f, 8.80f), new Vector3(21.20f, BaseY + 1.2f, 8.80f), ~0, QueryTriggerInteraction.Ignore),
                "Physical entrance must remain non-traversable from inside Remanencia.");
        }

        [Test]
        public void TransitionAuthorization_RejectsConventionalDoorAndAuthorizesOnlyThresholdOfTrust()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            // 4, 5, 7. Source-level authorization rules
            Assert.IsTrue(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(RemanenciaThresholdOfTrustTrigger.ExitSourceId));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource("default"));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(string.Empty));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(null));

            // 4 & 5. Audit FirstPersonPlayer.cs call sites: zero runtime callers invoke RequestExitBackrooms()
            string fppPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Scripts/FirstPersonPlayer.cs"));
            string fppText = File.ReadAllText(fppPath);
            Assert.IsFalse(fppText.Contains("RequestExitBackrooms("),
                "FirstPersonPlayer.cs must not invoke RequestExitBackrooms() on [E] or mouse click.");

            // 9-18. Runtime harness verification of rejected legacy exit vs authorized Threshold of Trust exit
            AmbientMode savedAmbMode = RenderSettings.ambientMode;
            Color savedAmbLight = RenderSettings.ambientLight;
            bool savedFog = RenderSettings.fog;
            Color savedFogCol = RenderSettings.fogColor;
            float savedFogDens = RenderSettings.fogDensity;
            Material savedSky = RenderSettings.skybox;

            GameObject harness = new GameObject("__Test0713TransitionHarness__");
            GameObject playerGo = new GameObject("__Test0713Player__");
            try
            {
                playerGo.AddComponent<CharacterController>();
                playerGo.transform.position = new Vector3(24.08f, BaseY, 22.40f);

                SecretBackroomsTransitionController ctrl = harness.AddComponent<SecretBackroomsTransitionController>();
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
                RenderSettings.fog = false;
                ctrl.CaptureAndApplyBackroomsAtmosphereForTest();
                Assert.IsTrue(ctrl.IsPlayerInBackrooms);
                Assert.AreEqual(PlayerWorldContext.RemanenciaLevel0, ctrl.CurrentWorldContext);
                Assert.IsTrue(RenderSettings.fog);

                // Legacy wrapper & unauthorized sources are strictly rejected
                Assert.IsFalse(ctrl.RequestExitBackrooms(), "Legacy RequestExitBackrooms() must never bypass entry-only rule.");
                Assert.IsFalse(ctrl.RequestExitRemanencia(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource));
                Assert.IsFalse(ctrl.RequestExitRemanencia("unknown-source"));
                Assert.IsTrue(ctrl.IsPlayerInBackrooms);
                Assert.IsFalse(ctrl.IsTransitioning);
                Assert.AreEqual(0, ctrl.CompletedExitCount);

                // Unauthorized test routine yields empty without transitioning
                IEnumerator rejectedRoutine = ctrl.CreateExitRemanenciaRoutineForTest(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource);
                Assert.IsFalse(rejectedRoutine.MoveNext(), "Unauthorized exit routine must refuse to start.");
                Assert.IsTrue(ctrl.IsPlayerInBackrooms);

                // Threshold of Trust trigger remains present & authorized
                Transform trigTf = GameObject.Find("Room111B_Threshold_TriggerVolume")?.transform;
                Assert.IsNotNull(trigTf);
                RemanenciaThresholdOfTrustTrigger trustTrig = trigTf.GetComponent<RemanenciaThresholdOfTrustTrigger>();
                Assert.IsNotNull(trustTrig);
                Assert.IsTrue(trustTrig.IsWithinCrossingZone(new Vector3(24.08f, BaseY, 22.40f)));

                // Execute real ExitBackroomsRoutine() via authorized "threshold-of-trust"
                IEnumerator authRoutine = ctrl.CreateExitRemanenciaRoutineForTest(RemanenciaThresholdOfTrustTrigger.ExitSourceId);
                Assert.IsTrue(authRoutine.MoveNext(), "Authorized Threshold of Trust exit must begin cleanly.");
                Assert.IsTrue(ctrl.IsTransitioning, "Controller must mark isTransitioning immediately.");
                Assert.IsFalse(ctrl.RequestExitRemanencia(RemanenciaThresholdOfTrustTrigger.ExitSourceId),
                    "Duplicate Threshold of Trust requests during an active transition must be rejected.");

                int guard = 0;
                while (authRoutine.MoveNext() && guard++ < 500) { }

                Assert.IsFalse(ctrl.IsTransitioning);
                Assert.IsFalse(ctrl.IsPlayerInBackrooms);
                Assert.AreEqual(1, ctrl.CompletedExitCount, "Exit routine must complete exactly once.");
                Assert.AreEqual(RemanenciaThresholdOfTrustTrigger.ExitSourceId, ctrl.LastExitSource);
                Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, ctrl.CurrentWorldContext, "PlayerWorldContext must restore to SurfaceCorridor.");
                Assert.IsFalse(RenderSettings.fog, "Outdoor atmosphere RenderSettings must be restored cleanly.");
                Assert.IsNull(ctrl.ActiveExteriorObservationSnapshot, "Observation RenderTexture must be released cleanly.");
                Assert.Less(Vector3.Distance(ctrl.streetExitReturnPosition, new Vector3(-11.20f, 0.24f, -11.99f)), 0.01f,
                    "Verified Av. Benito Juárez return position must remain unchanged.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harness);
                UnityEngine.Object.DestroyImmediate(playerGo);
                RenderSettings.ambientMode = savedAmbMode;
                RenderSettings.ambientLight = savedAmbLight;
                RenderSettings.fog = savedFog;
                RenderSettings.fogColor = savedFogCol;
                RenderSettings.fogDensity = savedFogDens;
                RenderSettings.skybox = savedSky;
            }
        }

        [Test]
        public void EntryRoutesSurfaceWorldImmutabilityAndProgressionRecovery_ArePreserved()
        {
            // 24-27. BorderCorridorScene.unity remains byte-for-byte identical to baseline HEAD
            string actualBorderSha = ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity");
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, actualBorderSha,
                "CRITICAL VIOLATION: BorderCorridorScene.unity must remain 100% byte-for-byte unchanged.");

            // 19 & 20. Open BorderCorridorScene strictly read-only to confirm both surface entry routes remain wired
            Scene borderScene = EditorSceneManager.OpenScene("Assets/Scenes/BorderCorridorScene.unity", OpenSceneMode.Single);
            Assert.IsTrue(borderScene.IsValid());
            GameObject exteriorDoor = GameObject.Find("BuildingA_SouthBay_StorageDoor");
            Assert.IsNotNull(exteriorDoor, "Actual Av. Benito Juárez exterior door must remain intact.");
            Assert.IsNotNull(exteriorDoor.GetComponent<SecretKeypadDoorController>(), "Exterior door keypad controller must remain intact.");
            SecretBackroomsTransitionController transition = UnityEngine.Object.FindFirstObjectByType<SecretBackroomsTransitionController>();
            Assert.IsNotNull(transition, "SecretBackroomsTransitionController must remain present in BorderCorridorScene.");
            Assert.IsNotNull(transition.outerStreetDoor, "Building A South Threshold Veil entry must remain wired.");
            SewerManholeInteractable[] manholes = UnityEngine.Object.FindObjectsByType<SewerManholeInteractable>(FindObjectsSortMode.None);
            SewerManholeInteractable azucenasManhole = null;
            for (int i = 0; i < manholes.Length; i++)
            {
                if (manholes[i] != null && manholes[i].manholeId == "manhole_juarez_azucenas")
                {
                    azucenasManhole = manholes[i];
                    break;
                }
            }
            Assert.IsNotNull(azucenasManhole, "Azucenas Manhole entry (manhole_juarez_azucenas) must remain intact.");
            Assert.IsTrue(azucenasManhole.triggerBackroomsNoClipTwist, "Azucenas Manhole must remain wired for Remanencia no-clip entry.");

            // 29-33. Save schema v1, progression IDs unchanged, restart recovery remains decoupled from world spawn
            var saveData = new ProgressionSaveData();
            Assert.AreEqual(1, saveData.schemaVersion);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("milestone:returned-to-border-corridor-from-backrooms", ProgressionIds.MilestoneReturnedFromBackrooms);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);

            // Reopen BackroomsLevel1Scene and verify builder checks still pass
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());
        }
    }
}
