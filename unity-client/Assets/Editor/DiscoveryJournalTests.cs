using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode test suite for Implementation 02: Player Discovery Journal & Narrative Feedback.
    /// Verifies single source of truth over PlayerProgressionService, discovery gating,
    /// live authoritative door-code resolution, bilingual ES/EN rendering, unknown-ID fallback,
    /// notification deduplication, write-protection disclosure, and scene-unload/restart survival.
    /// </summary>
    [TestFixture]
    public class DiscoveryJournalTests
    {
        private string tempRootDir;

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_JournalTests_" + Guid.NewGuid().ToString("N"));
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
        public void NewGame_ProducesEmptyJournalWithNoUndiscoveredEntriesRevealed()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            Assert.IsTrue(en.IsEmpty);
            Assert.AreEqual(0, en.TotalDiscoveryCount);
            Assert.AreEqual(0, en.EvidenceEntries.Count);
            Assert.AreEqual(0, en.KnowledgeEntries.Count);
            Assert.AreEqual(0, en.ArtifactEntries.Count);
            Assert.AreEqual(0, en.MilestoneEntries.Count);
            Assert.IsFalse(en.IsWriteProtected);
            Assert.AreEqual("Arrival on Av. Benito Juárez", en.CurrentCheckpointLabel);
            Assert.AreEqual("Inicio en Av. Benito Juárez", es.CurrentCheckpointLabel);
        }

        [Test]
        public void EvidenceCategory_ShowsOnlyDiscoveredEvidenceAndHidesUndiscovered()
        {
            var service = CreateFreshService();
            service.StartNewGame();
            service.RecordEvidenceInspected("artifact:mascara-paquime");

            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            Assert.AreEqual(1, en.EvidenceEntries.Count);
            StringAssert.Contains("Paquimé", en.EvidenceEntries[0].Title);
            Assert.AreEqual("Discovered", en.EvidenceEntries[0].StatusBadge);
            Assert.AreEqual("Descubierta", es.EvidenceEntries[0].StatusBadge);

            // Undiscovered items (such as PDN-00 or Astral Compass) must NOT appear
            Assert.IsFalse(ContainsTitleFragment(en.EvidenceEntries, "PDN-00"));
            Assert.IsFalse(ContainsTitleFragment(en.EvidenceEntries, "Compass"));
            Assert.AreEqual(0, en.KnowledgeEntries.Count);
            Assert.AreEqual(0, en.ArtifactEntries.Count);
        }

        [Test]
        public void KnowledgeCategory_RevealsCodeOnlyAfterLearnedAndUsesLiveAuthoritativeSource()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            // Even if a live door resolver knows the code, unlearned codes MUST remain hidden
            Func<string, string> liveResolver = doorId =>
                doorId == ProgressionIds.DoorBuildingASouth ? ProgressionIds.DefaultBuildingASouthAccessCode : null;

            DiscoveryJournalSnapshot beforeLearn = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false, liveResolver);
            Assert.AreEqual(0, beforeLearn.KnowledgeEntries.Count);

            // Unlock Building A South door -> code learned & puzzle solved
            service.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            // Inspect PDN-00 -> Level-Omega code learned, while Backrooms scene is unloaded (resolver returns null)
            service.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);

            DiscoveryJournalSnapshot enLoadedStreet = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false, liveResolver);
            DiscoveryJournalSnapshot esLoadedStreet = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true, liveResolver);

            // Expect 2 learned codes + 1 solved puzzle = 3 Knowledge entries
            Assert.AreEqual(3, enLoadedStreet.KnowledgeEntries.Count);

            // Building A South code resolved from live controller -> exact value shown
            JournalEntryView streetCodeEntry = FindByTitleFragment(enLoadedStreet.KnowledgeEntries, "Building A South");
            Assert.IsNotNull(streetCodeEntry);
            StringAssert.Contains(ProgressionIds.DefaultBuildingASouthAccessCode, streetCodeEntry.SecondaryNote);

            // Level-Omega safe code when Backrooms scene is unloaded -> learned status shown WITHOUT hardcoding unreliable digits
            JournalEntryView safeCodeEntryEn = FindByTitleFragment(enLoadedStreet.KnowledgeEntries, "Level Ω");
            JournalEntryView safeCodeEntryEs = FindByTitleFragment(esLoadedStreet.KnowledgeEntries, "Nivel Ω");
            Assert.IsNotNull(safeCodeEntryEn);
            Assert.IsNotNull(safeCodeEntryEs);
            Assert.AreEqual("Code Learned", safeCodeEntryEn.StatusBadge);
            Assert.AreEqual("Código aprendido", safeCodeEntryEs.StatusBadge);
            StringAssert.DoesNotContain("654321", safeCodeEntryEn.SecondaryNote);

            // Once Backrooms scene is loaded, live resolver supplies the authoritative safe code cleanly
            DiscoveryJournalSnapshot whenBackroomsLoaded = DiscoveryJournalPresenter.BuildSnapshot(
                service,
                isEs: false,
                doorId => doorId == ProgressionIds.SafeBackroomsOmegaVault ? "654321" : null);
            JournalEntryView safeCodeLoaded = FindByTitleFragment(whenBackroomsLoaded.KnowledgeEntries, "Level Ω");
            Assert.IsNotNull(safeCodeLoaded);
            StringAssert.Contains("654321", safeCodeLoaded.SecondaryNote);

            // Solved puzzle entry present with accurate badge
            JournalEntryView solvedPuzzle = FindByBadge(enLoadedStreet.KnowledgeEntries, "Solved");
            Assert.IsNotNull(solvedPuzzle);
        }

        [Test]
        public void ArtifactsCategory_TracksOjoDelUmbralEvolvingPlacementAndPermanentLasserHistory()
        {
            var service = CreateFreshService();
            service.StartNewGame();

            // 1. Acquire El Ojo del Umbral -> In Possession
            service.RecordOjoDelUmbralAcquired();
            DiscoveryJournalSnapshot carriedEn = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot carriedEs = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            Assert.AreEqual(1, carriedEn.ArtifactEntries.Count);
            Assert.AreEqual("The Eye of the Threshold", carriedEn.ArtifactEntries[0].Title);
            Assert.AreEqual("In Possession", carriedEn.ArtifactEntries[0].StatusBadge);
            Assert.AreEqual("En tu posesión", carriedEs.ArtifactEntries[0].StatusBadge);

            // 2. Socket into El Umbral de Lasser -> Placed & Portal Active
            service.RecordLasserPortalState(isPortalActivated: true);
            DiscoveryJournalSnapshot activeEn = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot activeEs = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            Assert.AreEqual(1, activeEn.ArtifactEntries.Count);
            StringAssert.Contains("Portal Active", activeEn.ArtifactEntries[0].StatusBadge);
            StringAssert.Contains("Portal activo", activeEs.ArtifactEntries[0].StatusBadge);
            StringAssert.Contains("Lasser Threshold", activeEn.ArtifactEntries[0].SecondaryNote);

            // 3. Remove from portal -> Evolving state returns to In Possession while preserving permanent activation record
            service.RecordLasserPortalState(isPortalActivated: false);
            DiscoveryJournalSnapshot removedEn = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            Assert.AreEqual("In Possession", removedEn.ArtifactEntries[0].StatusBadge);
            StringAssert.Contains("previously activated", removedEn.ArtifactEntries[0].SecondaryNote);
            Assert.IsTrue(ContainsTitleFragment(removedEn.MilestoneEntries, "Lasser Threshold Activated"));
            Assert.IsTrue(ContainsTitleFragment(removedEn.KnowledgeEntries, "Activation of The Lasser Threshold"));
        }

        [Test]
        public void UnknownCatalogEntries_DegradeGracefullyWithoutExposingRawTechnicalIds()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame();

            // Inject unknown future IDs across all permanent categories
            service.CurrentData.permanent.discoveredEvidenceIds.Add("artifact:secret_unregistered_99");
            service.CurrentData.permanent.learnedCodeIds.Add("code:unregistered_door_77");
            service.CurrentData.permanent.solvedPuzzleIds.Add("puzzle:unregistered_lock_88");
            service.CurrentData.permanent.acquiredArtifactIds.Add("artifact:unregistered_relic_66");
            service.CurrentData.permanent.unlockedMilestoneIds.Add("milestone:unregistered_place_55");
            service.SaveNow();

            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            AssertNoRawIdExposed(en, "secret_unregistered_99", "unregistered_door_77", "unregistered_lock_88", "unregistered_relic_66", "unregistered_place_55");
            AssertNoRawIdExposed(es, "secret_unregistered_99", "unregistered_door_77", "unregistered_lock_88", "unregistered_relic_66", "unregistered_place_55");

            Assert.AreEqual("Recorded Evidence", en.EvidenceEntries[0].Title);
            Assert.AreEqual("Evidencia registrada", es.EvidenceEntries[0].Title);
            Assert.AreEqual("Acquired Narrative Artifact", en.ArtifactEntries[0].Title);
            Assert.AreEqual("Objeto narrativo adquirido", es.ArtifactEntries[0].Title);
            Assert.AreEqual("Exploration Milestone Recorded", en.MilestoneEntries[0].Title);
            Assert.AreEqual("Hito de exploración registrado", es.MilestoneEntries[0].Title);
        }

        [Test]
        public void NarrativeFeedback_NotifiesOncePerNewDiscoveryAndNeverRepeatsForKnownEntries()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame();

            var notifications = new List<ProgressionDiscoveryNotification>();
            service.OnNewDiscoveryRecorded += n => notifications.Add(n);

            // First inspections/milestones -> 1 notification each
            service.RecordUndergroundEntered();
            service.RecordUndergroundEntered(); // duplicate -> ignored
            service.RecordEvidenceInspected("artifact:mascara-paquime");
            service.RecordEvidenceInspected("artifact:mascara-paquime"); // duplicate -> ignored
            service.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            service.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth); // duplicate -> ignored
            service.RecordOjoDelUmbralAcquired();
            service.RecordOjoDelUmbralAcquired(); // duplicate -> ignored
            service.RecordLasserPortalState(isPortalActivated: true);
            service.RecordLasserPortalState(isPortalActivated: false); // evolving toggle off -> no new discovery notification
            service.RecordLasserPortalState(isPortalActivated: true);  // already permanently activated -> no duplicate notification

            Assert.AreEqual(5, notifications.Count);
            Assert.AreEqual(DiscoveryNotificationKind.Milestone, notifications[0].Kind);
            Assert.AreEqual(DiscoveryNotificationKind.Evidence, notifications[1].Kind);
            Assert.AreEqual(DiscoveryNotificationKind.Knowledge, notifications[2].Kind);
            Assert.AreEqual(DiscoveryNotificationKind.Artifact, notifications[3].Kind);
            Assert.AreEqual(DiscoveryNotificationKind.Milestone, notifications[4].Kind);

            // Formatted bilingual feedback must never expose raw technical IDs
            foreach (var n in notifications)
            {
                string enText = DiscoveryJournalPresenter.FormatNewDiscoveryFeedback(n, isEs: false);
                string esText = DiscoveryJournalPresenter.FormatNewDiscoveryFeedback(n, isEs: true);
                Assert.IsFalse(string.IsNullOrWhiteSpace(enText));
                Assert.IsFalse(string.IsNullOrWhiteSpace(esText));
                StringAssert.DoesNotContain("artifact:", enText);
                StringAssert.DoesNotContain("milestone:", enText);
                StringAssert.DoesNotContain("code:", enText);
                StringAssert.DoesNotContain("puzzle:", enText);
            }

            // After simulated restart, re-recording already-known discoveries fires ZERO notifications
            var restarted = new PlayerProgressionService(storage);
            restarted.EnsureInitialized();
            int postRestartNotifications = 0;
            restarted.OnNewDiscoveryRecorded += _ => postRestartNotifications++;

            restarted.RecordUndergroundEntered();
            restarted.RecordEvidenceInspected("artifact:mascara-paquime");
            restarted.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            restarted.RecordOjoDelUmbralAcquired();
            restarted.RecordLasserPortalState(isPortalActivated: true);

            Assert.AreEqual(0, postRestartNotifications, "Already-persisted discoveries must not re-notify after restart.");
        }

        [Test]
        public void JournalContent_SurvivesSceneUnloadAndApplicationRestart()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            session1.RecordUndergroundEntered();
            session1.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            session1.RecordBackroomsEntered(viaAzucenasManhole: false);
            session1.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);
            session1.RecordEvidenceInspected("artifact:brujula-astral");
            session1.RecordKeypadDoorUnlocked(ProgressionIds.SafeBackroomsOmegaVault);
            session1.RecordOjoDelUmbralAcquired();
            session1.RecordLasserPortalState(isPortalActivated: true);
            // Simulate BackroomsLevel1Scene unload & return to BorderCorridorScene
            session1.RecordReturnedFromBackroomsToCorridor();

            // Simulate full application restart with zero Backrooms scene loaded
            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);

            DiscoveryJournalSnapshot snapshot = DiscoveryJournalPresenter.BuildSnapshot(session2, isEs: false);
            Assert.IsFalse(snapshot.IsEmpty);
            Assert.AreEqual(2, snapshot.EvidenceEntries.Count);
            Assert.AreEqual(5, snapshot.KnowledgeEntries.Count); // 2 learned codes + 3 solved puzzles
            Assert.AreEqual(1, snapshot.ArtifactEntries.Count);
            Assert.GreaterOrEqual(snapshot.MilestoneEntries.Count, 8);
            Assert.AreEqual("The Threshold of Lasser Activated", snapshot.CurrentCheckpointLabel);
            Assert.IsTrue(ContainsTitleFragment(snapshot.MilestoneEntries, "Returned to Avenida Benito Juárez"));
        }

        [Test]
        public void WriteProtectedSave_CommunicatesWarningHonestlyInBothLanguages()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            Directory.CreateDirectory(storage.SaveDirectory);
            File.WriteAllText(storage.PrimaryFilePath, "{ \"schemaVersion\": 99, \"saveId\": \"future\" }", new UTF8Encoding(false));

            var service = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.UnsupportedVersion, service.EnsureInitialized().Status);
            Assert.IsTrue(service.IsWriteProtectedDueToLoadFailure);

            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);

            Assert.IsTrue(en.IsWriteProtected);
            Assert.IsTrue(es.IsWriteProtected);
            StringAssert.Contains("may not be saved", en.WriteProtectionWarning);
            StringAssert.Contains("no se guarden", es.WriteProtectionWarning);
        }

        [Test]
        public void TemporalEntries_FilterExpiredAndNeverErasePermanentJournalEntries()
        {
            DateTime clock = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage, () => clock);
            service.StartNewGame();

            service.RecordEvidenceInspected("artifact:mascara-paquime");
            service.SetTemporalEntry("temp:expired_event", "active", clock.AddMinutes(-5));
            service.SetTemporalEntry("temp:active_event", "active", clock.AddHours(2));

            DiscoveryJournalSnapshot snap = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false, utcNowOverride: clock);
            Assert.AreEqual(1, snap.EvidenceEntries.Count);
            Assert.AreEqual(1, snap.ActiveTemporalEntries.Count);
            StringAssert.DoesNotContain("temp:", snap.ActiveTemporalEntries[0].Title);
        }

        private PlayerProgressionService CreateFreshService()
        {
            return new PlayerProgressionService(new LocalProgressionStorage(tempRootDir));
        }

        private static bool ContainsTitleFragment(IReadOnlyList<JournalEntryView> list, string fragment)
        {
            return FindByTitleFragment(list, fragment) != null;
        }

        private static JournalEntryView FindByTitleFragment(IReadOnlyList<JournalEntryView> list, string fragment)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Title.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return list[i];
                }
            }
            return null;
        }

        private static JournalEntryView FindByBadge(IReadOnlyList<JournalEntryView> list, string badge)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].StatusBadge, badge, StringComparison.OrdinalIgnoreCase))
                {
                    return list[i];
                }
            }
            return null;
        }

        private static void AssertNoRawIdExposed(DiscoveryJournalSnapshot snapshot, params string[] forbiddenFragments)
        {
            var all = new List<JournalEntryView>();
            all.AddRange(snapshot.EvidenceEntries);
            all.AddRange(snapshot.KnowledgeEntries);
            all.AddRange(snapshot.ArtifactEntries);
            all.AddRange(snapshot.MilestoneEntries);
            all.AddRange(snapshot.ActiveTemporalEntries);

            foreach (JournalEntryView entry in all)
            {
                foreach (string raw in forbiddenFragments)
                {
                    StringAssert.DoesNotContain(raw, entry.Title);
                    StringAssert.DoesNotContain(raw, entry.Description);
                    StringAssert.DoesNotContain(raw, entry.SecondaryNote);
                }
            }
        }
    }
}
