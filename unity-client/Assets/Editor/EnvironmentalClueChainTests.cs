using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode & Editor Scene verification suite for Implementation 03:
    /// Underground Environmental Clue Chain (Sector C-0 "58" -> La Cisterna "19" -> Abasolo Siphon "26"
    /// -> Learned Code "581926" -> Building A South Secret Room).
    /// </summary>
    [TestFixture]
    public class EnvironmentalClueChainTests
    {
        private string tempRootDir;

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_ClueChainTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRootDir);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerProgressionService.ResetSharedForTesting();
            try
            {
                if (!string.IsNullOrEmpty(tempRootDir) && Directory.Exists(tempRootDir))
                {
                    Directory.Delete(tempRootDir, recursive: true);
                }
            }
            catch
            {
                // Ignore transient Windows file lock during cleanup
            }
        }

        [Test]
        public void NewGame_HasZeroSewerCluesAndNoLearnedAccessCode()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            Assert.IsFalse(service.HasDiscoveredEvidence(ProgressionIds.ClueSewerSectorC0));
            Assert.IsFalse(service.HasDiscoveredEvidence(ProgressionIds.ClueCisternElNudo));
            Assert.IsFalse(service.HasDiscoveredEvidence(ProgressionIds.ClueAbasoloSiphon));
            Assert.IsFalse(service.HasDiscoveredAllThreeSewerClues());
            Assert.IsFalse(service.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.IsFalse(service.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));
        }

        [Test]
        public void DiscoveringCluesIncrementally_LearnsCodeOnlyOnThirdClueWithExactNotifications()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            var notifications = new List<ProgressionDiscoveryNotification>();
            service.OnNewDiscoveryRecorded += n => notifications.Add(n);

            Func<string, string> liveResolver = doorId =>
                doorId == ProgressionIds.DoorBuildingASouth ? ProgressionIds.DefaultBuildingASouthAccessCode : null;

            // 1. Clue 1 only -> 1 Evidence notification, complete code remains hidden in Knowledge
            Assert.IsTrue(service.RecordEvidenceInspected(ProgressionIds.ClueSewerSectorC0));
            Assert.IsTrue(service.HasDiscoveredEvidence(ProgressionIds.ClueSewerSectorC0));
            Assert.IsFalse(service.HasDiscoveredEvidence(ProgressionIds.ClueCisternElNudo));
            Assert.IsFalse(service.HasDiscoveredEvidence(ProgressionIds.ClueAbasoloSiphon));
            Assert.IsFalse(service.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(DiscoveryNotificationKind.Evidence, notifications[0].Kind);
            Assert.AreEqual(ProgressionIds.ClueSewerSectorC0, notifications[0].EntryId);

            DiscoveryJournalSnapshot after1 = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false, liveResolver);
            Assert.AreEqual(1, after1.EvidenceEntries.Count);
            StringAssert.Contains("58", after1.EvidenceEntries[0].Description);
            Assert.AreEqual(0, after1.KnowledgeEntries.Count);

            // Revisiting Clue 1 -> zero duplicate notifications
            Assert.IsFalse(service.RecordEvidenceInspected(ProgressionIds.ClueSewerSectorC0));
            Assert.AreEqual(1, notifications.Count);

            // 2. Clue 2 only -> 1 Evidence notification, complete code STILL hidden
            Assert.IsTrue(service.RecordEvidenceInspected(ProgressionIds.ClueCisternElNudo));
            Assert.IsTrue(service.HasDiscoveredEvidence(ProgressionIds.ClueCisternElNudo));
            Assert.IsFalse(service.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.AreEqual(2, notifications.Count);
            Assert.AreEqual(DiscoveryNotificationKind.Evidence, notifications[1].Kind);
            Assert.AreEqual(ProgressionIds.ClueCisternElNudo, notifications[1].EntryId);

            DiscoveryJournalSnapshot after2 = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true, liveResolver);
            Assert.AreEqual(2, after2.EvidenceEntries.Count);
            Assert.AreEqual(0, after2.KnowledgeEntries.Count);

            // 3. Clue 3 completes the sequence -> 1 Evidence notification + 1 Knowledge notification
            Assert.IsTrue(service.RecordEvidenceInspected(ProgressionIds.ClueAbasoloSiphon));
            Assert.IsTrue(service.HasDiscoveredAllThreeSewerClues());
            Assert.IsTrue(service.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.IsTrue(service.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));
            Assert.AreEqual(4, notifications.Count);
            Assert.AreEqual(DiscoveryNotificationKind.Evidence, notifications[2].Kind);
            Assert.AreEqual(ProgressionIds.ClueAbasoloSiphon, notifications[2].EntryId);
            Assert.AreEqual(DiscoveryNotificationKind.Knowledge, notifications[3].Kind);
            Assert.AreEqual(ProgressionIds.CodeBuildingASouthFromSewerClues, notifications[3].EntryId);

            // Calling explicit code-learning method or revisiting any clue again is idempotent (zero duplicate notifications)
            Assert.IsFalse(service.RecordBuildingASouthCodeLearnedFromSewerClues());
            Assert.IsFalse(service.RecordEvidenceInspected(ProgressionIds.ClueAbasoloSiphon));
            Assert.AreEqual(4, notifications.Count);

            // Journal now reveals 581926 via live authoritative resolver (ES & EN)
            DiscoveryJournalSnapshot after3En = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false, liveResolver);
            DiscoveryJournalSnapshot after3Es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true, liveResolver);
            Assert.AreEqual(3, after3En.EvidenceEntries.Count);
            Assert.AreEqual(1, after3En.KnowledgeEntries.Count);
            StringAssert.Contains("581926", after3En.KnowledgeEntries[0].SecondaryNote);
            StringAssert.Contains("581926", after3Es.KnowledgeEntries[0].SecondaryNote);

            // Toasts before/on completion must never leak raw IDs or unlearned full codes
            string toast1En = DiscoveryJournalPresenter.FormatNewDiscoveryFeedback(notifications[0], isEs: false);
            string toast4Es = DiscoveryJournalPresenter.FormatNewDiscoveryFeedback(notifications[3], isEs: true);
            StringAssert.DoesNotContain("581926", toast1En);
            StringAssert.DoesNotContain("clue:", toast1En);
            StringAssert.DoesNotContain("code:", toast4Es);
        }

        [Test]
        public void LearnedCode_SurvivesApplicationRestartAndSceneUnloadWithoutReplayingNotifications()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            // Discover in non-sequential order (III -> I -> II) to verify order independence of discovery
            session1.RecordEvidenceInspected(ProgressionIds.ClueAbasoloSiphon);
            session1.RecordEvidenceInspected(ProgressionIds.ClueSewerSectorC0);
            Assert.IsFalse(session1.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            session1.RecordEvidenceInspected(ProgressionIds.ClueCisternElNudo);
            Assert.IsTrue(session1.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));

            // Simulate Backrooms entry & unload return to corridor
            session1.RecordBackroomsEntered(viaAzucenasManhole: false);
            session1.RecordReturnedFromBackroomsToCorridor();

            // Simulate full application restart
            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);
            int replayedCount = 0;
            session2.OnNewDiscoveryRecorded += _ => replayedCount++;

            Assert.IsTrue(session2.HasDiscoveredAllThreeSewerClues());
            Assert.IsTrue(session2.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.IsTrue(session2.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));

            // Re-inspecting clues after restart generates zero notifications
            session2.RecordEvidenceInspected(ProgressionIds.ClueSewerSectorC0);
            session2.RecordEvidenceInspected(ProgressionIds.ClueCisternElNudo);
            session2.RecordEvidenceInspected(ProgressionIds.ClueAbasoloSiphon);
            session2.RecordBuildingASouthCodeLearnedFromSewerClues();
            Assert.AreEqual(0, replayedCount);

            // Journal still exposes learned code via live resolver after restart
            DiscoveryJournalSnapshot snap = DiscoveryJournalPresenter.BuildSnapshot(
                session2,
                isEs: false,
                doorId => doorId == ProgressionIds.DoorBuildingASouth ? "581926" : null);
            Assert.AreEqual(3, snap.EvidenceEntries.Count);
            Assert.AreEqual(1, snap.KnowledgeEntries.Count);
            StringAssert.Contains("581926", snap.KnowledgeEntries[0].SecondaryNote);
        }

        [Test]
        public void LegacySaveWithOldDemoCode_DoesNotFalselyGainSewerClueCodeOrReveal581926()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame();

            // Inject legacy pre-clue-chain learned code ID only
            service.CurrentData.permanent.learnedCodeIds.Add(ProgressionIds.CodeBuildingASouthSecretDoor);
            service.SaveNow();

            var reloaded = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, reloaded.EnsureInitialized().Status);

            Assert.IsFalse(reloaded.HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues));
            Assert.IsFalse(reloaded.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));

            DiscoveryJournalSnapshot snap = DiscoveryJournalPresenter.BuildSnapshot(
                reloaded,
                isEs: false,
                doorId => doorId == ProgressionIds.DoorBuildingASouth ? "581926" : null);
            Assert.AreEqual(1, snap.KnowledgeEntries.Count);
            StringAssert.DoesNotContain("581926", snap.KnowledgeEntries[0].SecondaryNote);
        }

        [Test]
        public void KeypadAcceptance_NewCode581926UnlocksBuildingASouthOld123456RejectedAndSafe654321Unchanged()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            GameObject root = new GameObject("ClueKeypadTestRoot");
            try
            {
                var streetDoor = new GameObject("BuildingASouthDoor").AddComponent<SecretKeypadDoorController>();
                streetDoor.transform.SetParent(root.transform);
                streetDoor.doorId = ProgressionIds.DoorBuildingASouth;
                streetDoor.SetProgressionServiceForTesting(service);

                // Default Building A South code must be 581926 and reject legacy 123456
                Assert.AreEqual("581926", streetDoor.activeAccessCode);
                Assert.IsFalse(streetDoor.TrySubmitCode("123456"), "Legacy demo code 123456 must be rejected.");
                Assert.IsFalse(streetDoor.IsUnlocked);
                Assert.IsTrue(streetDoor.TrySubmitCode("581926"), "New underground clue-chain code 581926 must unlock Building A South.");
                Assert.IsTrue(streetDoor.IsUnlocked);
                Assert.IsTrue(streetDoor.IsOpen);
                Assert.IsTrue(service.IsDoorPuzzleSolved(ProgressionIds.DoorBuildingASouth));

                // Level-Omega Safe remains 654321 and rejects 581926
                var omegaSafe = new GameObject("OmegaSafe").AddComponent<SecretKeypadDoorController>();
                omegaSafe.transform.SetParent(root.transform);
                omegaSafe.doorId = ProgressionIds.SafeBackroomsOmegaVault;
                omegaSafe.activeAccessCode = "654321";
                omegaSafe.allowInteriorSafetyBypass = false;
                omegaSafe.SetProgressionServiceForTesting(service);

                Assert.IsFalse(omegaSafe.TrySubmitCode("581926"));
                Assert.IsTrue(omegaSafe.TrySubmitCode("654321"));
                Assert.IsTrue(omegaSafe.IsUnlocked);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BorderCorridorScene_ContainsThreeUndergroundClueMarkersAnd581926Keypad()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BorderCorridorScene.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BorderCorridorScene.unity must open cleanly.");
            Physics.SyncTransforms();

            // 1. Verify Building A South exterior keypad door has authoritative code 581926
            GameObject doorGo = GameObject.Find("BuildingA_SouthBay_StorageDoor");
            Assert.IsNotNull(doorGo, "BuildingA_SouthBay_StorageDoor must exist in BorderCorridorScene.");
            var doorCtrl = doorGo.GetComponent<SecretKeypadDoorController>();
            Assert.IsNotNull(doorCtrl);
            Assert.AreEqual(ProgressionIds.DoorBuildingASouth, doorCtrl.doorId);
            Assert.AreEqual("581926", doorCtrl.activeAccessCode);

            // 2. Verify all 3 physical underground maintenance clue descriptors exist with exact IDs & scope
            var expectedClues = new[]
            {
                ("CluePlate_01_SectorC0", ProgressionIds.ClueSewerSectorC0, "58"),
                ("CluePlate_02_CisternElNudo", ProgressionIds.ClueCisternElNudo, "19"),
                ("CluePlate_03_AbasoloSiphon", ProgressionIds.ClueAbasoloSiphon, "26")
            };

            foreach (var (goName, clueId, fragment) in expectedClues)
            {
                GameObject go = GameObject.Find(goName);
                Assert.IsNotNull(go, $"Missing clue object {goName} in BorderCorridorScene.");
                var desc = go.GetComponent<SecretContainmentArtifactDescriptor>();
                Assert.IsNotNull(desc, $"Missing SecretContainmentArtifactDescriptor on {goName}.");
                Assert.AreEqual(clueId, desc.artifactId);
                Assert.AreEqual(ArtifactInspectionScope.UndergroundSewer, desc.inspectionScope);
                StringAssert.Contains(fragment, desc.loreEs);
                StringAssert.Contains(fragment, desc.loreEn);

                // Must use non-blocking trigger collider only (never block traversal)
                Collider[] cols = go.GetComponentsInChildren<Collider>(true);
                Assert.GreaterOrEqual(cols.Length, 1, $"{goName} must have an inspection trigger collider.");
                foreach (Collider c in cols)
                {
                    Assert.IsTrue(c.isTrigger, $"{goName} collider {c.name} must be a non-blocking trigger.");
                }

                // Must be physically below street elevation (underground sewer)
                Assert.Less(go.transform.position.y, -1.2f, $"{goName} must be inside the underground sewer network.");
            }
        }

        private PlayerProgressionService CreateFreshService()
        {
            return new PlayerProgressionService(new LocalProgressionStorage(tempRootDir));
        }

        /// <summary>
        /// Batchmode helper that applies the Implementation 03 scene updates (rebuilds UndergroundSewerBuilder
        /// with the 3 maintenance clue plates and updates BuildingA_SouthBay_StorageDoor to 581926) and saves BorderCorridorScene.
        /// </summary>
        public static void ApplySceneUpdatesAndVerify()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BorderCorridorScene.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Impl03SceneUpdate] Failed to open BorderCorridorScene.unity");
                EditorApplication.Exit(1);
                return;
            }

            UndergroundSewerBuilder.BuildInOpenScene();

            GameObject doorGo = GameObject.Find("BuildingA_SouthBay_StorageDoor");
            SecretKeypadDoorController doorCtrl = doorGo != null ? doorGo.GetComponent<SecretKeypadDoorController>() : null;
            if (doorCtrl == null)
            {
                Debug.LogError("[Impl03SceneUpdate] Missing BuildingA_SouthBay_StorageDoor SecretKeypadDoorController!");
                EditorApplication.Exit(1);
                return;
            }
            doorCtrl.activeAccessCode = ProgressionIds.DefaultBuildingASouthAccessCode;
            EditorUtility.SetDirty(doorCtrl);

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Impl03SceneUpdate] BorderCorridorScene saved={saved}, BuildingA_SouthBay_StorageDoor.activeAccessCode={doorCtrl.activeAccessCode}");

            bool sewerOk = UndergroundSewerBuilder.RunVerificationChecks();
            Debug.Log($"[Impl03SceneUpdate] UndergroundSewerBuilder.RunVerificationChecks() = {sewerOk}");
            EditorApplication.Exit(saved && sewerOk ? 0 : 1);
        }
    }
}
