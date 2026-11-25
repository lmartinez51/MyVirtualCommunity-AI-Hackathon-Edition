using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode test suite for Implementation 01: Offline Single-Player Progression Foundation.
    /// Runnable via Unity Test Runner (-runTests -testPlatform EditMode) and via batchmode
    /// -executeMethod MyVirtualCommunity.Editor.OfflineProgressionTests.RunBatchmodeVerification.
    /// </summary>
    [TestFixture]
    public class OfflineProgressionTests
    {
        private string tempRootDir;

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_ProgressionTests_" + Guid.NewGuid().ToString("N"));
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
                // Ignore transient Windows file lock during test cleanup
            }
        }

        [Test]
        public void NewSaveCreation_ProducesValidSchemaV1WithUtcTimestampsAndDefaultCheckpoint()
        {
            DateTime fixedNow = new DateTime(2026, 11, 24, 21, 15, 0, DateTimeKind.Utc);
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage, () => fixedNow);

            ProgressionSaveResult saveRes = service.StartNewGame();

            Assert.IsTrue(saveRes.Success, saveRes.ErrorMessage);
            Assert.IsTrue(File.Exists(storage.PrimaryFilePath), "Primary save file should exist.");
            Assert.IsFalse(File.Exists(storage.TempFilePath), "Temporary write file should be cleaned up.");
            Assert.AreEqual(ProgressionSaveData.CurrentSchemaVersion, service.CurrentData.schemaVersion);
            Assert.IsFalse(string.IsNullOrWhiteSpace(service.CurrentData.saveId));
            Assert.AreEqual("2026-11-24T21:15:00.000Z", service.CurrentData.createdAtUtc);
            Assert.AreEqual("2026-11-24T21:15:00.000Z", service.CurrentData.updatedAtUtc);
            Assert.AreEqual(1, service.CurrentData.saveSequence);
            Assert.AreEqual(ProgressionIds.CheckpointArrival, service.LatestCheckpointId);
            Assert.AreEqual(0, service.CurrentData.permanent.discoveredEvidenceIds.Count);
            Assert.AreEqual(0, service.CurrentData.permanent.acquiredArtifactIds.Count);
            Assert.IsFalse(service.IsHoldingOjoDelUmbral);
            Assert.IsFalse(service.IsLasserPortalCurrentlyActive);
        }

        [Test]
        public void SaveAndLoadRoundTrip_PreservesPermanentEvolvingTemporalAndCheckpoint()
        {
            DateTime clock = new DateTime(2026, 11, 24, 21, 30, 0, DateTimeKind.Utc);
            var storage = new LocalProgressionStorage(tempRootDir);
            var writer = new PlayerProgressionService(storage, () => clock);

            writer.StartNewGame();
            clock = clock.AddMinutes(2);
            writer.RecordUndergroundEntered();
            writer.RecordSewerOutflowGateState(true);
            writer.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            writer.RecordBackroomsEntered(viaAzucenasManhole: false);
            writer.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);
            writer.RecordEvidenceInspected("artifact:mascara-paquime");
            writer.RecordKeypadDoorUnlocked(ProgressionIds.SafeBackroomsOmegaVault);
            writer.RecordOjoDelUmbralAcquired();
            writer.RecordLasserPortalState(isPortalActivated: true);
            writer.SetEvolvingFlag("encounter:threshold_resonance", "witnessed");
            writer.SetTemporalEntry("temp:sample_window", "active", clock.AddHours(6));

            // Fresh service instance simulates full application restart offline
            var reader = new PlayerProgressionService(storage, () => clock.AddMinutes(10));
            ProgressionLoadResult loadResult = reader.EnsureInitialized();

            Assert.AreEqual(ProgressionLoadStatus.Loaded, loadResult.Status);
            Assert.IsFalse(loadResult.BackupUsed);
            Assert.AreEqual(writer.CurrentData.saveId, reader.CurrentData.saveId);
            Assert.AreEqual(ProgressionIds.CheckpointLasserPortalActivated, reader.LatestCheckpointId);

            // Permanent assertions
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneUndergroundNetworkEntered));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneRioBravoOutflowOpened));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneSecretRoomKeypadUnlocked));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneBackroomsEntered));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneBackroomsEnteredViaVeil));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneEvidenceBoardPdn00Discovered));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneOmegaSafeUnlocked));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneOjoDelUmbralAcquired));
            Assert.IsTrue(reader.HasMilestone(ProgressionIds.MilestoneLasserPortalActivated));
            Assert.IsTrue(reader.HasLearnedCode(ProgressionIds.CodeBuildingASouthSecretDoor));
            Assert.IsTrue(reader.HasLearnedCode(ProgressionIds.CodeOmegaVaultSafe));
            Assert.IsTrue(reader.IsDoorPuzzleSolved(ProgressionIds.DoorBuildingASouth));
            Assert.IsTrue(reader.IsDoorPuzzleSolved(ProgressionIds.SafeBackroomsOmegaVault));
            Assert.IsTrue(reader.IsPuzzleSolved(ProgressionIds.PuzzleLasserPortalActivation));
            Assert.IsTrue(reader.HasDiscoveredEvidence(ProgressionIds.ArtifactEvidenceBoardPdn00));
            Assert.IsTrue(reader.HasDiscoveredEvidence("artifact:mascara-paquime"));
            Assert.IsTrue(reader.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));

            // Evolving assertions
            Assert.IsTrue(reader.IsSewerOutflowGateOpened);
            Assert.IsTrue(reader.IsLasserPortalCurrentlyActive);
            Assert.IsFalse(reader.IsHoldingOjoDelUmbral, "Astrolabe is currently socketed in portal, not carried.");
            Assert.AreEqual("witnessed", reader.GetEvolvingFlag("encounter:threshold_resonance"));

            // Temporal assertions
            TemporalProgressionEntry temp = reader.GetTemporalEntry("temp:sample_window");
            Assert.IsNotNull(temp);
            Assert.AreEqual("active", temp.state);
        }

        [Test]
        public void ProgressionSemantics_TemporalExpiryAndRemovalNeverErasePermanentOrEvolvingState()
        {
            DateTime clock = new DateTime(2026, 11, 24, 22, 0, 0, DateTimeKind.Utc);
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage, () => clock);

            service.StartNewGame();
            service.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);
            service.RecordOjoDelUmbralAcquired();
            service.RecordLasserPortalState(isPortalActivated: true);
            service.SetTemporalEntry("temp:short_signal", "heard", clock.AddHours(1));
            service.SetTemporalEntry("temp:long_signal", "active", clock.AddDays(5));

            // Advance across a calendar day boundary (48 hours later):
            // Only the expired temporal entry should be pruned; permanent & evolving state must survive intact.
            clock = clock.AddHours(48);
            int pruned = service.PruneExpiredTemporalEntries(autoSave: true);

            Assert.AreEqual(1, pruned);
            Assert.IsNull(service.GetTemporalEntry("temp:short_signal"));
            Assert.IsNotNull(service.GetTemporalEntry("temp:long_signal"));

            // Removing El Ojo del Umbral from portal updates evolving state without erasing permanent activation milestone
            service.RecordLasserPortalState(isPortalActivated: false);
            Assert.IsFalse(service.IsLasserPortalCurrentlyActive);
            Assert.IsTrue(service.IsHoldingOjoDelUmbral);
            Assert.IsTrue(service.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));
            Assert.IsTrue(service.HasMilestone(ProgressionIds.MilestoneLasserPortalActivated));
            Assert.IsTrue(service.IsPuzzleSolved(ProgressionIds.PuzzleLasserPortalActivation));

            // Clearing all temporal entries still leaves permanent & evolving intact
            int cleared = service.ClearTemporalEntries(autoSave: true);
            Assert.AreEqual(1, cleared);
            Assert.IsTrue(service.HasDiscoveredEvidence(ProgressionIds.ArtifactEvidenceBoardPdn00));
            Assert.IsTrue(service.IsHoldingOjoDelUmbral);
        }

        [Test]
        public void WorldEntryAndCheckpointSeparation_ReturningNeverResetsAndCheckpointNeverRegresses()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);

            service.StartNewGame();
            service.RecordOjoDelUmbralAcquired();
            service.RecordLasserPortalState(isPortalActivated: true);
            Assert.AreEqual(ProgressionIds.CheckpointLasserPortalActivated, service.LatestCheckpointId);

            // Returning to BorderCorridorScene and later descending a sewer manhole must NOT reset or regress checkpoint
            service.RecordReturnedFromBackroomsToCorridor();
            service.RecordUndergroundEntered();

            Assert.AreEqual(ProgressionIds.CheckpointLasserPortalActivated, service.LatestCheckpointId);
            Assert.IsTrue(service.HasMilestone(ProgressionIds.MilestoneReturnedFromBackrooms));
            Assert.IsTrue(service.HasMilestone(ProgressionIds.MilestoneUndergroundNetworkEntered));
            Assert.IsTrue(service.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));
            Assert.IsTrue(service.IsLasserPortalCurrentlyActive);
        }

        [Test]
        public void StrictWorldCoreBoundary_SerializedJsonExcludesTransformsAndWorldCoreState()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame();
            service.RecordKeypadDoorUnlocked(ProgressionIds.DoorBuildingASouth);
            service.RecordOjoDelUmbralAcquired();

            string json = File.ReadAllText(storage.PrimaryFilePath, Encoding.UTF8);

            // Must not serialize player world coordinates or World Core authoritative simulation data
            StringAssert.DoesNotContain("playerPosition", json);
            StringAssert.DoesNotContain("worldClock", json);
            StringAssert.DoesNotContain("npcSchedule", json);
            StringAssert.DoesNotContain("citizenReport", json);
            StringAssert.DoesNotContain("businessPrice", json);
            StringAssert.DoesNotContain("exchangeRate", json);
            StringAssert.Contains("\"schemaVersion\": 1", json);
            StringAssert.Contains("\"latestCheckpointId\"", json);
        }

        [Test]
        public void MissingSaveFile_ReturnsNotFoundCleanlyWithoutCreatingFile()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            ProgressionLoadResult loadRes = storage.Load();

            Assert.AreEqual(ProgressionLoadStatus.NotFound, loadRes.Status);
            Assert.IsNull(loadRes.Data);
            Assert.IsFalse(loadRes.IsSuccess);
            Assert.IsFalse(File.Exists(storage.PrimaryFilePath));
        }

        [Test]
        public void CorruptedAndTruncatedJson_ReturnsCorruptedPreservesFileAndBlocksSilentOverwrite()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            Directory.CreateDirectory(storage.SaveDirectory);

            // Truncated JSON (ends mid-object)
            const string truncatedJson = "{\n  \"schemaVersion\": 1,\n  \"saveId\": \"abc-123\",\n  \"permanent\": { \"discoveredEvidenceIds\": [\"artifact:muro-evidencias-pdn\"]";
            File.WriteAllText(storage.PrimaryFilePath, truncatedJson, new UTF8Encoding(false));

            var service = new PlayerProgressionService(storage);
            ProgressionLoadResult loadRes = service.EnsureInitialized();

            Assert.AreEqual(ProgressionLoadStatus.Corrupted, loadRes.Status);
            Assert.IsTrue(service.IsWriteProtectedDueToLoadFailure);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loadRes.ErrorMessage));

            // Subsequent gameplay mutations or SaveNow() must NOT silently overwrite the corrupted file
            service.RecordUndergroundEntered(autoSave: true);
            Assert.IsNotNull(service.LastSaveResult);
            Assert.IsFalse(service.LastSaveResult.Success);
            Assert.AreEqual(truncatedJson, File.ReadAllText(storage.PrimaryFilePath, Encoding.UTF8));

            // Also verify malformed non-JSON garbage is caught cleanly
            File.WriteAllText(storage.PrimaryFilePath, "<<<NOT_JSON>>>", new UTF8Encoding(false));
            ProgressionLoadResult garbageRes = storage.Load();
            Assert.AreEqual(ProgressionLoadStatus.Corrupted, garbageRes.Status);
        }

        [Test]
        public void UnsupportedSchemaVersion_ReturnsUnsupportedPreservesFileAndBlocksSilentOverwrite()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            Directory.CreateDirectory(storage.SaveDirectory);

            const string futureJson = "{\n  \"schemaVersion\": 99,\n  \"saveId\": \"future-save-99\",\n  \"latestCheckpointId\": \"checkpoint:future\"\n}";
            File.WriteAllText(storage.PrimaryFilePath, futureJson, new UTF8Encoding(false));

            var service = new PlayerProgressionService(storage);
            ProgressionLoadResult loadRes = service.EnsureInitialized();

            Assert.AreEqual(ProgressionLoadStatus.UnsupportedVersion, loadRes.Status);
            Assert.AreEqual(99, loadRes.DetectedSchemaVersion);
            Assert.IsTrue(service.IsWriteProtectedDueToLoadFailure);

            ProgressionSaveResult refusedSave = service.SaveNow();
            Assert.IsFalse(refusedSave.Success);
            Assert.AreEqual(futureJson, File.ReadAllText(storage.PrimaryFilePath, Encoding.UTF8), "Future-version save must remain untouched.");

            // Schema version 0 is also rejected as unsupported
            File.WriteAllText(storage.PrimaryFilePath, "{\n  \"schemaVersion\": 0,\n  \"saveId\": \"zero\"\n}", new UTF8Encoding(false));
            ProgressionLoadResult zeroRes = storage.Load();
            Assert.AreEqual(ProgressionLoadStatus.UnsupportedVersion, zeroRes.Status);
        }

        [Test]
        public void BackupAndRecovery_RecoversValidBackupWhenPrimaryCorruptedWithoutPoisoningBackup()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);

            // Save #1 -> primary written
            service.StartNewGame();
            service.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);
            // Save #2 -> prior valid primary rotated into .bak
            service.RecordKeypadDoorUnlocked(ProgressionIds.SafeBackroomsOmegaVault);

            Assert.IsTrue(File.Exists(storage.PrimaryFilePath));
            Assert.IsTrue(File.Exists(storage.BackupFilePath));

            // Simulate power loss corrupting primary save
            File.WriteAllText(storage.PrimaryFilePath, "{ \"schemaVersion\": 1, \"saveId\": \"corrupt", new UTF8Encoding(false));

            // Restart -> should recover cleanly from .bak
            var recovered = new PlayerProgressionService(storage);
            ProgressionLoadResult loadRes = recovered.EnsureInitialized();

            Assert.AreEqual(ProgressionLoadStatus.LoadedFromBackup, loadRes.Status);
            Assert.IsTrue(loadRes.BackupUsed);
            Assert.IsFalse(recovered.IsWriteProtectedDueToLoadFailure);
            Assert.IsTrue(recovered.HasDiscoveredEvidence(ProgressionIds.ArtifactEvidenceBoardPdn00));

            // Next save after backup recovery must archive corrupt primary instead of overwriting .bak with corrupt bytes
            recovered.RecordOjoDelUmbralAcquired();
            Assert.IsTrue(recovered.LastSaveResult.Success, recovered.LastSaveResult.ErrorMessage);
            Assert.IsFalse(string.IsNullOrEmpty(recovered.LastSaveResult.ArchivedPath), "Corrupted primary should be archived safely.");
            Assert.IsTrue(File.Exists(recovered.LastSaveResult.ArchivedPath));

            // Verify .bak is STILL a valid parseable save (never poisoned by corrupt primary)
            string backupContent = File.ReadAllText(storage.BackupFilePath, Encoding.UTF8);
            StringAssert.Contains("\"schemaVersion\": 1", backupContent);
            StringAssert.DoesNotContain("corrupt", backupContent);

            // Verify fresh reload loads cleanly from repaired primary
            var reloaded = new PlayerProgressionService(storage);
            ProgressionLoadResult finalLoad = reloaded.EnsureInitialized();
            Assert.AreEqual(ProgressionLoadStatus.Loaded, finalLoad.Status);
            Assert.IsTrue(reloaded.IsHoldingOjoDelUmbral);
        }

        [Test]
        public void RepeatedSaveOperations_IncrementSequenceCleanTempAndMaintainValidBackup()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame(); // sequence 1

            for (int i = 0; i < 10; i++)
            {
                service.RecordEvidenceInspected($"artifact:test-item-{i:D2}");
                Assert.IsTrue(service.LastSaveResult.Success, $"Save iteration {i} failed: {service.LastSaveResult.ErrorMessage}");
                Assert.IsFalse(File.Exists(storage.TempFilePath), $"Temp file leaked at iteration {i}.");
            }

            Assert.AreEqual(11, service.CurrentData.saveSequence);
            Assert.IsTrue(File.Exists(storage.PrimaryFilePath));
            Assert.IsTrue(File.Exists(storage.BackupFilePath));

            var reloaded = new PlayerProgressionService(storage);
            ProgressionLoadResult loadRes = reloaded.EnsureInitialized();
            Assert.AreEqual(ProgressionLoadStatus.Loaded, loadRes.Status);
            Assert.AreEqual(11, reloaded.CurrentData.saveSequence);
            Assert.AreEqual(10, reloaded.CurrentData.permanent.discoveredEvidenceIds.Count);
        }

        [Test]
        public void SceneTransitionAndRestartPersistence_RealControllersSurviveUnloadAndRestart()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            // Build real runtime controllers mirroring BorderCorridorScene & BackroomsLevel1Scene
            GameObject root1 = new GameObject("Session1_TestRoot");
            try
            {
                // 1. Building A South exterior door
                var streetDoor = new GameObject("StreetDoor").AddComponent<SecretKeypadDoorController>();
                streetDoor.transform.SetParent(root1.transform);
                streetDoor.doorId = ProgressionIds.DoorBuildingASouth;
                streetDoor.activeAccessCode = "123456";
                streetDoor.allowInteriorSafetyBypass = true;
                streetDoor.SetProgressionServiceForTesting(session1);

                Assert.IsTrue(streetDoor.TrySubmitCode("123456"));
                session1.RecordBackroomsEntered(viaAzucenasManhole: false);
                session1.RecordEvidenceInspected(ProgressionIds.ArtifactEvidenceBoardPdn00);

                // 2. Backrooms Level-Omega Safe + Dimension Vault Controller + Portal visual objects
                var safeDoor = new GameObject("OmegaSafe").AddComponent<SecretKeypadDoorController>();
                safeDoor.transform.SetParent(root1.transform);
                safeDoor.doorId = ProgressionIds.SafeBackroomsOmegaVault;
                safeDoor.activeAccessCode = "654321";
                safeDoor.allowInteriorSafetyBypass = false;
                safeDoor.SetProgressionServiceForTesting(session1);

                var vault = new GameObject("DimensionVault").AddComponent<SecretRoomDimensionVaultController>();
                vault.transform.SetParent(root1.transform);
                vault.vaultSafeDoorController = safeDoor;
                vault.astrolabeInSafeObject = new GameObject("AstrolabeInSafe");
                vault.astrolabeInSafeObject.transform.SetParent(root1.transform);
                vault.astrolabeInSocketObject = new GameObject("AstrolabeInSocket");
                vault.astrolabeInSocketObject.transform.SetParent(root1.transform);
                vault.portalChainsGroup = new GameObject("PortalChains");
                vault.portalChainsGroup.transform.SetParent(root1.transform);
                vault.portalActiveVortexGroup = new GameObject("PortalVortex");
                vault.portalActiveVortexGroup.transform.SetParent(root1.transform);
                vault.SetProgressionServiceForTesting(session1);

                // 3. Sewer Outflow Gate
                var outflowGate = new GameObject("OutflowGate").AddComponent<SewerOutflowGateController>();
                outflowGate.transform.SetParent(root1.transform);
                outflowGate.gateHingePivot = new GameObject("GateHinge").transform;
                outflowGate.gateHingePivot.SetParent(root1.transform);
                outflowGate.SetProgressionServiceForTesting(session1);
                outflowGate.ToggleGate();

                // Execute Backrooms puzzle through the actual controllers
                Assert.IsTrue(safeDoor.TrySubmitCode("654321"));
                Assert.IsTrue(vault.TryTakeAstrolabeFromSafe());
                Assert.IsTrue(session1.IsHoldingOjoDelUmbral);
                Assert.IsTrue(vault.TryToggleAstrolabeInPortalSocket());
                Assert.IsTrue(vault.IsPortalActivated);
                Assert.IsTrue(session1.IsLasserPortalCurrentlyActive);

                // Simulate unloading BackroomsLevel1Scene & returning to BorderCorridorScene
                session1.RecordReturnedFromBackroomsToCorridor();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root1);
            }

            // Simulate full application restart + re-entering BackroomsLevel1Scene (brand-new controllers + fresh service)
            var session2 = new PlayerProgressionService(storage);
            ProgressionLoadResult restartLoad = session2.EnsureInitialized();
            Assert.AreEqual(ProgressionLoadStatus.Loaded, restartLoad.Status);

            GameObject root2 = new GameObject("Session2_ReloadedRoot");
            try
            {
                // Exterior street door preserves locked physical state (Section 2.1) while milestone remains known
                var streetDoor2 = new GameObject("StreetDoor2").AddComponent<SecretKeypadDoorController>();
                streetDoor2.transform.SetParent(root2.transform);
                streetDoor2.doorId = ProgressionIds.DoorBuildingASouth;
                streetDoor2.allowInteriorSafetyBypass = true;
                streetDoor2.ApplyProgressionState(session2);
                Assert.IsFalse(streetDoor2.IsOpen, "Exterior street door must preserve closed default behavior.");
                Assert.IsTrue(session2.IsDoorPuzzleSolved(ProgressionIds.DoorBuildingASouth));

                // Reloaded Backrooms safe + vault must restore unlocked safe, absent safe astrolabe, and active portal!
                var safeDoor2 = new GameObject("OmegaSafe2").AddComponent<SecretKeypadDoorController>();
                safeDoor2.transform.SetParent(root2.transform);
                safeDoor2.doorId = ProgressionIds.SafeBackroomsOmegaVault;
                safeDoor2.allowInteriorSafetyBypass = false;

                var vault2 = new GameObject("DimensionVault2").AddComponent<SecretRoomDimensionVaultController>();
                vault2.transform.SetParent(root2.transform);
                vault2.vaultSafeDoorController = safeDoor2;
                vault2.astrolabeInSafeObject = new GameObject("AstrolabeInSafe2");
                vault2.astrolabeInSafeObject.transform.SetParent(root2.transform);
                vault2.astrolabeInSocketObject = new GameObject("AstrolabeInSocket2");
                vault2.astrolabeInSocketObject.transform.SetParent(root2.transform);
                vault2.portalChainsGroup = new GameObject("PortalChains2");
                vault2.portalChainsGroup.transform.SetParent(root2.transform);
                vault2.portalActiveVortexGroup = new GameObject("PortalVortex2");
                vault2.portalActiveVortexGroup.transform.SetParent(root2.transform);

                vault2.ApplyProgressionState(session2);

                Assert.IsTrue(safeDoor2.IsUnlocked, "Level-Omega safe should stay unlocked after reload/restart.");
                Assert.IsTrue(safeDoor2.IsOpen, "Level-Omega safe should restore open after reload/restart.");
                Assert.IsTrue(vault2.IsSafeOpen);
                Assert.IsFalse(vault2.astrolabeInSafeObject.activeSelf, "Astrolabe must not duplicate inside safe.");
                Assert.IsTrue(vault2.IsPortalActivated, "Lasser portal must stay activated after reload/restart.");
                Assert.IsTrue(vault2.astrolabeInSocketObject.activeSelf, "Astrolabe must remain in portal socket.");
                Assert.IsFalse(vault2.portalChainsGroup.activeSelf, "Portal chains must remain retracted.");
                Assert.IsTrue(vault2.portalActiveVortexGroup.activeSelf, "Portal vortex must remain active.");

                // Reloaded Sewer Outflow Gate restores open state
                var outflowGate2 = new GameObject("OutflowGate2").AddComponent<SewerOutflowGateController>();
                outflowGate2.transform.SetParent(root2.transform);
                outflowGate2.gateHingePivot = new GameObject("GateHinge2").transform;
                outflowGate2.gateHingePivot.SetParent(root2.transform);
                outflowGate2.ApplyProgressionState(session2);
                Assert.IsTrue(outflowGate2.IsOpen);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root2);
            }
        }

        [Test]
        public void StartNewGameAfterCorruptedSave_ArchivesDamagedFileAndStartsFreshOffline()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            Directory.CreateDirectory(storage.SaveDirectory);
            File.WriteAllText(storage.PrimaryFilePath, "{ broken json", new UTF8Encoding(false));

            var service = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Corrupted, service.EnsureInitialized().Status);
            Assert.IsTrue(service.IsWriteProtectedDueToLoadFailure);

            ProgressionSaveResult resetRes = service.StartNewGame(archiveExisting: true);
            Assert.IsTrue(resetRes.Success, resetRes.ErrorMessage);
            Assert.IsFalse(service.IsWriteProtectedDueToLoadFailure);

            string[] archivedFiles = Directory.GetFiles(storage.SaveDirectory, "progression_save.newgame.*.json");
            Assert.AreEqual(1, archivedFiles.Length, "Corrupted save should be archived rather than deleted.");

            var reloaded = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, reloaded.EnsureInitialized().Status);
        }

        [Test]
        public void ExteriorSecretDoor_RestartPreservesLearnedCodeAcceptsCodeAndRetainsSolvedPuzzle()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            GameObject root1 = new GameObject("ExtDoor_Session1");
            try
            {
                var streetDoor = new GameObject("BuildingASouthDoor").AddComponent<SecretKeypadDoorController>();
                streetDoor.transform.SetParent(root1.transform);
                streetDoor.doorId = ProgressionIds.DoorBuildingASouth;
                streetDoor.activeAccessCode = "123456";
                streetDoor.allowInteriorSafetyBypass = true;
                streetDoor.SetProgressionServiceForTesting(session1);

                Assert.IsFalse(session1.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));
                Assert.IsTrue(streetDoor.TrySubmitCode("123456"));
                Assert.IsTrue(session1.HasLearnedCode(ProgressionIds.CodeBuildingASouthSecretDoor));
                Assert.IsTrue(session1.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));
                Assert.IsTrue(session1.IsDoorPuzzleSolved(ProgressionIds.DoorBuildingASouth));

                // Entering Backrooms and returning to BorderCorridorScene must never erase exterior door knowledge
                session1.RecordBackroomsEntered(viaAzucenasManhole: false);
                session1.RecordReturnedFromBackroomsToCorridor();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root1);
            }

            // Simulate application restart
            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);
            Assert.IsTrue(session2.HasLearnedCode(ProgressionIds.CodeBuildingASouthSecretDoor));
            Assert.IsTrue(session2.HasLearnedCodeForDoor(ProgressionIds.DoorBuildingASouth));
            Assert.IsTrue(session2.IsDoorPuzzleSolved(ProgressionIds.DoorBuildingASouth));

            GameObject root2 = new GameObject("ExtDoor_Session2");
            try
            {
                var streetDoor2 = new GameObject("BuildingASouthDoor2").AddComponent<SecretKeypadDoorController>();
                streetDoor2.transform.SetParent(root2.transform);
                streetDoor2.doorId = ProgressionIds.DoorBuildingASouth;
                streetDoor2.activeAccessCode = "123456";
                streetDoor2.allowInteriorSafetyBypass = true;
                streetDoor2.SetProgressionServiceForTesting(session2);
                streetDoor2.ApplyProgressionState(session2);

                // Physical door preserves locked exterior default while permanent knowledge remains intact
                Assert.IsFalse(streetDoor2.IsUnlocked);
                Assert.IsFalse(streetDoor2.IsOpen);
                // Previously learned code remains accepted cleanly after restart
                Assert.IsTrue(streetDoor2.TrySubmitCode("123456"));
                Assert.IsTrue(streetDoor2.IsUnlocked);
                Assert.IsTrue(streetDoor2.IsOpen);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root2);
            }
        }

        [Test]
        public void ArtifactAndLasserPortal_IdempotencyAcrossReloadsAndTogglePreservesPermanentMilestones()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            // 1. Unlock safe and acquire El Ojo del Umbral (carried, NOT yet socketed)
            GameObject r1 = new GameObject("Idem_Session1");
            try
            {
                var (safe1, vault1) = CreateVaultRig(r1.transform, session1);
                Assert.IsTrue(safe1.TrySubmitCode("654321"));
                Assert.IsTrue(vault1.TryTakeAstrolabeFromSafe());
                // Duplicate pickup attempt must fail
                Assert.IsFalse(vault1.TryTakeAstrolabeFromSafe());
                Assert.IsFalse(vault1.astrolabeInSafeObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(r1);
            }

            // 2. Reload Backrooms while carried -> must NOT duplicate inside safe, must stay carried
            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);
            GameObject r2 = new GameObject("Idem_Session2");
            try
            {
                var (safe2, vault2) = CreateVaultRig(r2.transform, session2);
                vault2.ApplyProgressionState(session2);
                // Re-applying progression state multiple times must remain idempotent
                vault2.ApplyProgressionState(session2);

                Assert.IsTrue(safe2.IsOpen);
                Assert.IsTrue(vault2.HasAstrolabeInInventory);
                Assert.IsFalse(vault2.IsPortalActivated);
                Assert.IsFalse(vault2.astrolabeInSafeObject.activeSelf, "Carried relic must not duplicate in safe.");
                Assert.IsFalse(vault2.TryTakeAstrolabeFromSafe(), "Cannot re-acquire already carried relic.");
                Assert.IsTrue(vault2.portalChainsGroup.activeSelf);

                // Activate portal
                Assert.IsTrue(vault2.TryToggleAstrolabeInPortalSocket());
                Assert.IsTrue(vault2.IsPortalActivated);

                // Calling RecordOjoDelUmbralAcquired again while socketed must NOT clobber socketed/active state
                session2.RecordOjoDelUmbralAcquired();
                Assert.IsTrue(session2.IsLasserPortalCurrentlyActive);
                Assert.AreEqual(ProgressionIds.PlacementSocketedLasserPortal, session2.CurrentData.evolving.ojoDelUmbralPlacement);

                // Toggle portal OFF (player removes astrolabe back to hand)
                Assert.IsTrue(vault2.TryToggleAstrolabeInPortalSocket());
                Assert.IsFalse(vault2.IsPortalActivated);
                Assert.IsTrue(vault2.HasAstrolabeInInventory);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(r2);
            }

            // 3. Restart after toggling portal OFF -> permanent activation milestone persists, evolving state restores carried & resealed
            var session3 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session3.EnsureInitialized().Status);
            Assert.IsTrue(session3.HasMilestone(ProgressionIds.MilestoneLasserPortalActivated));
            Assert.IsTrue(session3.IsPuzzleSolved(ProgressionIds.PuzzleLasserPortalActivation));
            Assert.AreEqual(ProgressionIds.CheckpointLasserPortalActivated, session3.LatestCheckpointId);
            Assert.IsTrue(session3.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));
            Assert.IsTrue(session3.IsHoldingOjoDelUmbral);
            Assert.IsFalse(session3.IsLasserPortalCurrentlyActive);

            GameObject r3 = new GameObject("Idem_Session3");
            try
            {
                var (safe3, vault3) = CreateVaultRig(r3.transform, session3);
                vault3.ApplyProgressionState(session3);

                Assert.IsTrue(safe3.IsOpen);
                Assert.IsTrue(vault3.HasAstrolabeInInventory);
                Assert.IsFalse(vault3.IsPortalActivated);
                Assert.IsFalse(vault3.astrolabeInSafeObject.activeSelf);
                Assert.IsFalse(vault3.astrolabeInSocketObject.activeSelf);
                Assert.IsTrue(vault3.portalChainsGroup.activeSelf);
                Assert.IsFalse(vault3.portalActiveVortexGroup.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(r3);
            }
        }

        [Test]
        public void NewGame_ArchivesPrimaryAndStaleBackupSoOldPlaythroughCannotResurrect()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var oldSession = new PlayerProgressionService(storage);
            oldSession.StartNewGame();
            oldSession.RecordOjoDelUmbralAcquired();
            oldSession.RecordLasserPortalState(isPortalActivated: true);

            Assert.IsTrue(File.Exists(storage.PrimaryFilePath));
            Assert.IsTrue(File.Exists(storage.BackupFilePath));

            // Starting New Game must archive BOTH primary and stale .bak from old playthrough
            ProgressionSaveResult newGameRes = oldSession.StartNewGame(archiveExisting: true);
            Assert.IsTrue(newGameRes.Success, newGameRes.ErrorMessage);
            Assert.IsFalse(File.Exists(storage.BackupFilePath), "Stale backup from previous playthrough should be archived out of active .bak slot.");
            Assert.GreaterOrEqual(Directory.GetFiles(storage.SaveDirectory, "progression_save.newgame.*.json").Length, 1);
            Assert.GreaterOrEqual(Directory.GetFiles(storage.SaveDirectory, "progression_save.newgame_bak.*.json").Length, 1);

            var freshSession = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, freshSession.EnsureInitialized().Status);
            Assert.AreEqual(ProgressionIds.CheckpointArrival, freshSession.LatestCheckpointId);
            Assert.IsFalse(freshSession.HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral));
        }

        private static (SecretKeypadDoorController safe, SecretRoomDimensionVaultController vault) CreateVaultRig(
            Transform parent,
            PlayerProgressionService progression)
        {
            var safe = new GameObject("OmegaSafe").AddComponent<SecretKeypadDoorController>();
            safe.transform.SetParent(parent);
            safe.doorId = ProgressionIds.SafeBackroomsOmegaVault;
            safe.activeAccessCode = "654321";
            safe.allowInteriorSafetyBypass = false;
            safe.SetProgressionServiceForTesting(progression);

            var vault = new GameObject("DimensionVault").AddComponent<SecretRoomDimensionVaultController>();
            vault.transform.SetParent(parent);
            vault.vaultSafeDoorController = safe;
            vault.astrolabeInSafeObject = new GameObject("AstrolabeInSafe");
            vault.astrolabeInSafeObject.transform.SetParent(parent);
            vault.astrolabeInSocketObject = new GameObject("AstrolabeInSocket");
            vault.astrolabeInSocketObject.transform.SetParent(parent);
            vault.portalChainsGroup = new GameObject("PortalChains");
            vault.portalChainsGroup.transform.SetParent(parent);
            vault.portalActiveVortexGroup = new GameObject("PortalVortex");
            vault.portalActiveVortexGroup.transform.SetParent(parent);
            vault.SetProgressionServiceForTesting(progression);
            return (safe, vault);
        }

        /// <summary>
        /// Direct batchmode entry point for quick CLI verification alongside Unity Test Runner.
        /// </summary>
        public static void RunBatchmodeVerification()
        {
            var suite = new OfflineProgressionTests();
            var tests = new (string name, Action body)[]
            {
                (nameof(NewSaveCreation_ProducesValidSchemaV1WithUtcTimestampsAndDefaultCheckpoint), suite.NewSaveCreation_ProducesValidSchemaV1WithUtcTimestampsAndDefaultCheckpoint),
                (nameof(SaveAndLoadRoundTrip_PreservesPermanentEvolvingTemporalAndCheckpoint), suite.SaveAndLoadRoundTrip_PreservesPermanentEvolvingTemporalAndCheckpoint),
                (nameof(ProgressionSemantics_TemporalExpiryAndRemovalNeverErasePermanentOrEvolvingState), suite.ProgressionSemantics_TemporalExpiryAndRemovalNeverErasePermanentOrEvolvingState),
                (nameof(WorldEntryAndCheckpointSeparation_ReturningNeverResetsAndCheckpointNeverRegresses), suite.WorldEntryAndCheckpointSeparation_ReturningNeverResetsAndCheckpointNeverRegresses),
                (nameof(StrictWorldCoreBoundary_SerializedJsonExcludesTransformsAndWorldCoreState), suite.StrictWorldCoreBoundary_SerializedJsonExcludesTransformsAndWorldCoreState),
                (nameof(MissingSaveFile_ReturnsNotFoundCleanlyWithoutCreatingFile), suite.MissingSaveFile_ReturnsNotFoundCleanlyWithoutCreatingFile),
                (nameof(CorruptedAndTruncatedJson_ReturnsCorruptedPreservesFileAndBlocksSilentOverwrite), suite.CorruptedAndTruncatedJson_ReturnsCorruptedPreservesFileAndBlocksSilentOverwrite),
                (nameof(UnsupportedSchemaVersion_ReturnsUnsupportedPreservesFileAndBlocksSilentOverwrite), suite.UnsupportedSchemaVersion_ReturnsUnsupportedPreservesFileAndBlocksSilentOverwrite),
                (nameof(BackupAndRecovery_RecoversValidBackupWhenPrimaryCorruptedWithoutPoisoningBackup), suite.BackupAndRecovery_RecoversValidBackupWhenPrimaryCorruptedWithoutPoisoningBackup),
                (nameof(RepeatedSaveOperations_IncrementSequenceCleanTempAndMaintainValidBackup), suite.RepeatedSaveOperations_IncrementSequenceCleanTempAndMaintainValidBackup),
                (nameof(SceneTransitionAndRestartPersistence_RealControllersSurviveUnloadAndRestart), suite.SceneTransitionAndRestartPersistence_RealControllersSurviveUnloadAndRestart),
                (nameof(StartNewGameAfterCorruptedSave_ArchivesDamagedFileAndStartsFreshOffline), suite.StartNewGameAfterCorruptedSave_ArchivesDamagedFileAndStartsFreshOffline),
                (nameof(ExteriorSecretDoor_RestartPreservesLearnedCodeAcceptsCodeAndRetainsSolvedPuzzle), suite.ExteriorSecretDoor_RestartPreservesLearnedCodeAcceptsCodeAndRetainsSolvedPuzzle),
                (nameof(ArtifactAndLasserPortal_IdempotencyAcrossReloadsAndTogglePreservesPermanentMilestones), suite.ArtifactAndLasserPortal_IdempotencyAcrossReloadsAndTogglePreservesPermanentMilestones),
                (nameof(NewGame_ArchivesPrimaryAndStaleBackupSoOldPlaythroughCannotResurrect), suite.NewGame_ArchivesPrimaryAndStaleBackupSoOldPlaythroughCannotResurrect),
            };

            int passed = 0;
            foreach (var (name, body) in tests)
            {
                suite.SetUp();
                try
                {
                    body();
                    passed++;
                    Debug.Log($"[OfflineProgressionTests] [PASS] {name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OfflineProgressionTests] [FAIL] {name}: {ex}");
                }
                finally
                {
                    suite.TearDown();
                }
            }

            Debug.Log($"[OfflineProgressionTests] SUMMARY: {passed}/{tests.Length} tests passed.");
            EditorApplication.Exit(passed == tests.Length ? 0 : 1);
        }
    }
}
