using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode regression suite for Implementation 07.2.1 Part V
    /// (World-Scoped Player Presentation Isolation).
    /// Verifies that the Remanencia relic-held badge ("El Ojo del Umbral") never leaks onto
    /// the Avenida Benito Juárez surface world on startup, during transitions, or after return,
    /// while remaining available inside Remanencia and preserving persistent save & Discovery Journal state.
    /// </summary>
    [TestFixture]
    public class RemanenciaWorldContextTests
    {
        private string tempRootDir;
        private GameObject testTransitionGo;
        private GameObject testVaultGo;
        private SecretBackroomsTransitionController transitionCtrl;
        private SecretRoomDimensionVaultController vaultCtrl;

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_WorldContext_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRootDir);

            testTransitionGo = new GameObject("Test_SecretBackroomsTransitionController");
            transitionCtrl = testTransitionGo.AddComponent<SecretBackroomsTransitionController>();

            testVaultGo = new GameObject("Test_SecretRoomDimensionVaultController");
            vaultCtrl = testVaultGo.AddComponent<SecretRoomDimensionVaultController>();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerProgressionService.ResetSharedForTesting();
            if (testTransitionGo != null) UnityEngine.Object.DestroyImmediate(testTransitionGo);
            if (testVaultGo != null) UnityEngine.Object.DestroyImmediate(testVaultGo);
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

        private PlayerProgressionService CreatePersistedSaveHoldingRelic()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var writer = new PlayerProgressionService(storage);
            writer.StartNewGame();
            writer.RecordBackroomsEntered(viaAzucenasManhole: false);
            writer.RecordOjoDelUmbralAcquired();
            Assert.IsTrue(writer.IsHoldingOjoDelUmbral, "Fixture save must record El Ojo del Umbral in inventory.");
            Assert.IsTrue(writer.SaveNow().Success, "Fixture save must persist to disk.");

            // Reload fresh session (simulates fresh launch into Avenida Benito Juárez surface scene)
            var reloaded = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, reloaded.EnsureInitialized().Status);
            Assert.IsTrue(reloaded.IsHoldingOjoDelUmbral, "Reloaded progression must retain El Ojo del Umbral.");
            return reloaded;
        }

        [Test]
        public void SurfaceStartupWithPersistedRelic_DoesNotDisplayRemanenciaRelicBadge()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();

            // Fresh startup on Avenida Benito Juárez (neither in Remanencia nor transitioning)
            Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, transitionCtrl.CurrentWorldContext);
            Assert.IsFalse(transitionCtrl.IsPlayerActivelyInRemanencia);

            bool shouldShow = FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(
                transitionCtrl, vaultController: null, progression, cursorLocked: true);

            Assert.IsFalse(shouldShow,
                "Remanencia relic-held HUD badge must NOT appear while player is in the surface world on Avenida Juárez.");
            Assert.IsTrue(progression.IsHoldingOjoDelUmbral,
                "Suppressing surface HUD presentation must NOT remove the relic from persistent progression.");
        }

        [Test]
        public void InsideRemanencia_RelicPresentationAppearsAfterCompletedArrival()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();
            vaultCtrl.SetProgressionServiceForTesting(progression);
            vaultCtrl.ApplyProgressionState(progression);

            // Simulate completed arrival inside Remanencia
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
            Assert.AreEqual(PlayerWorldContext.RemanenciaLevel0, transitionCtrl.CurrentWorldContext);
            Assert.IsTrue(transitionCtrl.IsPlayerActivelyInRemanencia);

            bool shouldShow = FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(
                transitionCtrl, vaultCtrl, progression, cursorLocked: true);
            Assert.IsTrue(shouldShow,
                "Relic-held HUD badge must appear normally once the player has completed arrival inside Remanencia.");
            Assert.IsTrue(vaultCtrl.HasAstrolabeInInventory,
                "Vault controller must reflect persisted relic inventory inside Remanencia.");
        }

        [Test]
        public void ReturnToSurface_HidesRemanenciaPresentationWithoutClearingSavedRelic()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();
            vaultCtrl.SetProgressionServiceForTesting(progression);
            vaultCtrl.ApplyProgressionState(progression);

            // Enter Remanencia -> badge visible
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
            Assert.IsTrue(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, vaultCtrl, progression, cursorLocked: true));

            // Begin exit transition -> immediately hidden before outdoor return finishes
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: true);
            Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, transitionCtrl.CurrentWorldContext);
            Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, vaultCtrl, progression, cursorLocked: true));

            // Completed return to Avenida Juárez
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: false, transitioning: false);
            progression.RecordReturnedFromBackroomsToCorridor();
            Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, vaultController: null, progression, cursorLocked: true));
            Assert.IsTrue(progression.IsHoldingOjoDelUmbral,
                "Returning to Avenida Juárez must preserve El Ojo del Umbral in persistent progression.");
        }

        [Test]
        public void ReEntryIntoRemanencia_DoesNotDuplicateAcquisitionOrMutateSave()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();
            int initialArtifactCount = progression.CurrentData.permanent.acquiredArtifactIds.Count;
            int initialSaveSeq = progression.CurrentData.saveSequence;
            int duplicateNotifications = 0;
            progression.OnNewDiscoveryRecorded += _ => duplicateNotifications++;

            // Re-enter Remanencia and hydrate vault state
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
            vaultCtrl.SetProgressionServiceForTesting(progression);
            vaultCtrl.ApplyProgressionState(progression);

            Assert.IsTrue(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, vaultCtrl, progression, cursorLocked: true));
            Assert.IsFalse(vaultCtrl.TryTakeAstrolabeFromSafe(), "Re-entry must not re-acquire an already-held relic.");
            Assert.AreEqual(initialArtifactCount, progression.CurrentData.permanent.acquiredArtifactIds.Count);
            Assert.AreEqual(initialSaveSeq, progression.CurrentData.saveSequence, "Hydrating on re-entry must not mutate save sequence.");
            Assert.AreEqual(0, duplicateNotifications, "Re-entry hydration must not emit duplicate discovery notifications.");
        }

        [Test]
        public void DiscoveryJournal_PreservesPermanentRelicRecordsInSurfaceWorld()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();

            // Player is on Avenida Juárez (surface world)
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: false, transitioning: false);
            Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, null, progression, cursorLocked: true));

            // Permanent Discovery Journal records remain intact and accessible
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(progression, isEs: true);
            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(progression, isEs: false);

            Assert.IsTrue(progression.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));
            Assert.GreaterOrEqual(es.ArtifactEntries.Count, 1, "Discovery Journal (ES) must still show acquired relic record.");
            Assert.GreaterOrEqual(en.ArtifactEntries.Count, 1, "Discovery Journal (EN) must still show acquired relic record.");
        }

        [Test]
        public void WorldTransitionLifecycle_NeverReportsRemanenciaWhileStillTransitioning()
        {
            PlayerProgressionService progression = CreatePersistedSaveHoldingRelic();

            // Mid-entry transition (fade/load/teleport still active) -> must NOT report Remanencia yet
            transitionCtrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: true);
            Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, transitionCtrl.CurrentWorldContext);
            Assert.IsFalse(transitionCtrl.IsPlayerActivelyInRemanencia);
            Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(transitionCtrl, vaultCtrl, progression, cursorLocked: true));

            // Null transition controller safety -> defaults strictly to non-Remanencia
            Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(null, vaultCtrl, progression, cursorLocked: true));
        }
    }
}
