using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyVirtualCommunity.Client.Progression
{
    public enum DiscoveryNotificationKind
    {
        Evidence,
        Knowledge,
        Artifact,
        Milestone
    }

    public sealed class ProgressionDiscoveryNotification
    {
        public DiscoveryNotificationKind Kind { get; }
        public string EntryId { get; }

        public ProgressionDiscoveryNotification(DiscoveryNotificationKind kind, string entryId)
        {
            Kind = kind;
            EntryId = entryId ?? string.Empty;
        }
    }

    /// <summary>
    /// Authoritative local single-player narrative progression service.
    /// Operates 100% offline using LocalProgressionStorage (Application.persistentDataPath),
    /// separates Permanent, Evolving, and Temporal state categories, and enforces strict
    /// separation from World Core simulation authority and player world transforms.
    /// </summary>
    public sealed class PlayerProgressionService
    {
        private static PlayerProgressionService sharedInstance;
        private static bool quitHookRegistered = false;

        private readonly LocalProgressionStorage storage;
        private readonly Func<DateTime> utcNowProvider;

        private ProgressionSaveData currentData;
        private ProgressionLoadResult lastLoadResult;
        private ProgressionSaveResult lastSaveResult;
        private bool isInitialized = false;
        private bool isWriteProtectedDueToLoadFailure = false;
        private bool recoveredFromBackupOnInit = false;
        private bool hasUnsavedChanges = false;

        public event Action<ProgressionSaveData> OnProgressionChanged;
        public event Action<ProgressionDiscoveryNotification> OnNewDiscoveryRecorded;

        public LocalProgressionStorage Storage => storage;
        public ProgressionSaveData CurrentData
        {
            get
            {
                EnsureInitialized();
                return currentData;
            }
        }

        public ProgressionLoadResult LastLoadResult
        {
            get
            {
                EnsureInitialized();
                return lastLoadResult;
            }
        }

        public ProgressionSaveResult LastSaveResult => lastSaveResult;
        public bool IsWriteProtectedDueToLoadFailure => isWriteProtectedDueToLoadFailure;
        public bool HasLoadedExistingSave => lastLoadResult != null && lastLoadResult.IsSuccess;
        public bool HasUnsavedChanges => hasUnsavedChanges;

        public static bool HasSharedInstance => sharedInstance != null;

        public static PlayerProgressionService Shared
        {
            get
            {
                if (sharedInstance == null)
                {
                    sharedInstance = new PlayerProgressionService(new LocalProgressionStorage());
                    sharedInstance.EnsureInitialized();
                    RegisterQuitHook();
                }
                return sharedInstance;
            }
        }

        public PlayerProgressionService(LocalProgressionStorage storage, Func<DateTime> utcNowProvider = null)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Allows automated tests to substitute an isolated temp-directory progression service.
        /// </summary>
        public static void SetSharedForTesting(PlayerProgressionService service)
        {
            sharedInstance = service;
        }

        public static void ResetSharedForTesting()
        {
            sharedInstance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnSubsystemRegistration()
        {
            Application.quitting -= HandleApplicationQuitting;
            quitHookRegistered = false;
            sharedInstance = null;
        }

        private static void RegisterQuitHook()
        {
            if (quitHookRegistered) return;
            quitHookRegistered = true;
            Application.quitting -= HandleApplicationQuitting;
            Application.quitting += HandleApplicationQuitting;
        }

        private static void HandleApplicationQuitting()
        {
            if (sharedInstance != null && sharedInstance.hasUnsavedChanges && !sharedInstance.isWriteProtectedDueToLoadFailure)
            {
                sharedInstance.SaveNow();
            }
        }

        /// <summary>
        /// Loads existing local progression on first access. Never silently overwrites corrupted
        /// or unsupported save files.
        /// </summary>
        public ProgressionLoadResult EnsureInitialized()
        {
            if (isInitialized) return lastLoadResult;
            return ReloadFromDisk();
        }

        public ProgressionLoadResult ReloadFromDisk()
        {
            isInitialized = true;
            hasUnsavedChanges = false;
            recoveredFromBackupOnInit = false;
            isWriteProtectedDueToLoadFailure = false;

            ProgressionLoadResult result = storage.Load();
            lastLoadResult = result;

            switch (result.Status)
            {
                case ProgressionLoadStatus.Loaded:
                    currentData = result.Data;
                    currentData.Normalize();
                    break;

                case ProgressionLoadStatus.LoadedFromBackup:
                    currentData = result.Data;
                    currentData.Normalize();
                    recoveredFromBackupOnInit = true;
                    Debug.LogWarning($"[Progression] {result.ErrorMessage}");
                    break;

                case ProgressionLoadStatus.NotFound:
                    currentData = ProgressionSaveData.CreateNew(utcNowProvider());
                    break;

                case ProgressionLoadStatus.Corrupted:
                case ProgressionLoadStatus.UnsupportedVersion:
                case ProgressionLoadStatus.IoError:
                    currentData = ProgressionSaveData.CreateNew(utcNowProvider());
                    isWriteProtectedDueToLoadFailure = true;
                    Debug.LogWarning(
                        $"[Progression] Local save load failed ({result.Status}): {result.ErrorMessage}. " +
                        "Existing save file has been preserved untouched; automatic writes are paused until a New Game is explicitly started.");
                    break;
            }

            OnProgressionChanged?.Invoke(currentData);
            return result;
        }

        /// <summary>
        /// Starts a clean new playthrough. If a prior save (valid, corrupted, or unsupported) exists
        /// on disk, it is archived safely rather than silently destroyed.
        /// </summary>
        public ProgressionSaveResult StartNewGame(bool archiveExisting = true)
        {
            isInitialized = true;
            if (archiveExisting && (storage.HasPrimaryFile || storage.HasBackupFile))
            {
                storage.ArchiveExistingSavesForNewGame("newgame");
            }

            currentData = ProgressionSaveData.CreateNew(utcNowProvider());
            isWriteProtectedDueToLoadFailure = false;
            recoveredFromBackupOnInit = false;
            hasUnsavedChanges = true;

            ProgressionSaveResult saveResult = SaveNow(allowArchiveUnreadable: archiveExisting);
            if (saveResult.Success)
            {
                lastLoadResult = new ProgressionLoadResult(
                    ProgressionLoadStatus.Loaded,
                    currentData,
                    storage.PrimaryFilePath,
                    backupUsed: false,
                    detectedSchemaVersion: currentData.schemaVersion,
                    errorMessage: string.Empty);
            }
            OnProgressionChanged?.Invoke(currentData);
            return saveResult;
        }

        /// <summary>
        /// Persists the current player progression atomically to disk.
        /// Refuses to overwrite an unreadable or unsupported save unless explicitly permitted.
        /// </summary>
        public ProgressionSaveResult SaveNow(bool allowArchiveUnreadable = false)
        {
            EnsureInitialized();

            if (isWriteProtectedDueToLoadFailure && !allowArchiveUnreadable)
            {
                string msg = lastLoadResult != null
                    ? $"Write skipped to protect unreadable/unsupported save ({lastLoadResult.Status}: {lastLoadResult.ErrorMessage})."
                    : "Write skipped due to prior load failure.";
                lastSaveResult = new ProgressionSaveResult(false, storage.PrimaryFilePath, false, string.Empty, msg);
                return lastSaveResult;
            }

            DateTime now = utcNowProvider();
            currentData.Normalize();
            currentData.updatedAtUtc = ProgressionSaveData.FormatUtc(now);
            currentData.saveSequence++;

            bool shouldArchiveUnreadable = allowArchiveUnreadable || recoveredFromBackupOnInit;
            ProgressionSaveResult result = storage.Save(currentData, archiveUnreadableExisting: shouldArchiveUnreadable);
            lastSaveResult = result;

            if (result.Success)
            {
                hasUnsavedChanges = false;
                recoveredFromBackupOnInit = false;
                if (allowArchiveUnreadable)
                {
                    isWriteProtectedDueToLoadFailure = false;
                }
            }
            else
            {
                // Roll back sequence counter when write was refused/failed
                currentData.saveSequence = Math.Max(0, currentData.saveSequence - 1);
                Debug.LogWarning($"[Progression] Save failed: {result.ErrorMessage}");
            }

            return result;
        }

        // ---------------------------------------------------------------------
        // Read Queries (Permanent, Evolving, Temporal, Checkpoint)
        // ---------------------------------------------------------------------

        public string LatestCheckpointId => CurrentData.latestCheckpointId;

        public bool HasDiscoveredEvidence(string evidenceId)
        {
            return ContainsId(CurrentData.permanent.discoveredEvidenceIds, evidenceId);
        }

        public bool HasLearnedCode(string codeId)
        {
            return ContainsId(CurrentData.permanent.learnedCodeIds, codeId);
        }

        public bool HasLearnedCodeForDoor(string doorId)
        {
            if (doorId == ProgressionIds.DoorBuildingASouth)
            {
                // Explicitly require the sewer-clue sequence learned-code ID so legacy saves
                // holding the old demonstration code do not falsely claim discovery of 581926.
                return HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues);
            }
            if (doorId == ProgressionIds.SafeBackroomsOmegaVault)
            {
                return HasLearnedCode(ProgressionIds.CodeOmegaVaultSafe) ||
                       IsDoorPuzzleSolved(ProgressionIds.SafeBackroomsOmegaVault);
            }
            return IsDoorPuzzleSolved(doorId);
        }

        public bool HasDiscoveredAllThreeSewerClues()
        {
            return HasDiscoveredEvidence(ProgressionIds.ClueSewerSectorC0) &&
                   HasDiscoveredEvidence(ProgressionIds.ClueCisternElNudo) &&
                   HasDiscoveredEvidence(ProgressionIds.ClueAbasoloSiphon);
        }

        public bool IsPuzzleSolved(string puzzleId)
        {
            return ContainsId(CurrentData.permanent.solvedPuzzleIds, puzzleId);
        }

        public bool IsDoorPuzzleSolved(string doorId)
        {
            return IsPuzzleSolved(ProgressionIds.PuzzleForDoor(doorId));
        }

        public bool HasAcquiredArtifact(string artifactId)
        {
            return ContainsId(CurrentData.permanent.acquiredArtifactIds, artifactId);
        }

        public bool HasMilestone(string milestoneId)
        {
            return ContainsId(CurrentData.permanent.unlockedMilestoneIds, milestoneId);
        }

        public bool IsHoldingOjoDelUmbral =>
            HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral) &&
            CurrentData.evolving.ojoDelUmbralPlacement == ProgressionIds.PlacementCarried;

        public bool IsLasserPortalCurrentlyActive =>
            HasAcquiredArtifact(ProgressionIds.ArtifactOjoDelUmbral) &&
            CurrentData.evolving.lasserPortalCurrentlyActive &&
            CurrentData.evolving.ojoDelUmbralPlacement == ProgressionIds.PlacementSocketedLasserPortal;

        public bool IsSewerOutflowGateOpened => CurrentData.evolving.sewerOutflowGateOpened;

        public string GetEvolvingFlag(string key, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;
            string trimmed = key.Trim();
            List<NarrativeStateEntry> flags = CurrentData.evolving.flags;
            for (int i = 0; i < flags.Count; i++)
            {
                if (string.Equals(flags[i].key, trimmed, StringComparison.Ordinal))
                {
                    return flags[i].value;
                }
            }
            return defaultValue;
        }

        public TemporalProgressionEntry GetTemporalEntry(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            string trimmed = id.Trim();
            List<TemporalProgressionEntry> list = CurrentData.temporal.entries;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].id, trimmed, StringComparison.Ordinal))
                {
                    return list[i];
                }
            }
            return null;
        }

        // ---------------------------------------------------------------------
        // Gameplay Integration Operations (Explicit, Minimal, Side-Effect Clear)
        // ---------------------------------------------------------------------

        public bool RecordUndergroundEntered(bool autoSave = true)
        {
            EnsureInitialized();
            bool addedMilestone = AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneUndergroundNetworkEntered);
            bool changed = addedMilestone;
            changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointUndergroundEntered);
            ProgressionDiscoveryNotification notification = addedMilestone
                ? new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, ProgressionIds.MilestoneUndergroundNetworkEntered)
                : null;
            return FinalizeMutation(changed, autoSave, notification);
        }

        public bool RecordSewerOutflowGateState(bool isOpen, bool autoSave = true)
        {
            EnsureInitialized();
            bool addedMilestone = false;
            bool changed = false;
            if (isOpen)
            {
                addedMilestone = AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneRioBravoOutflowOpened);
                changed |= addedMilestone;
            }
            if (currentData.evolving.sewerOutflowGateOpened != isOpen)
            {
                currentData.evolving.sewerOutflowGateOpened = isOpen;
                changed = true;
            }
            ProgressionDiscoveryNotification notification = addedMilestone
                ? new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, ProgressionIds.MilestoneRioBravoOutflowOpened)
                : null;
            return FinalizeMutation(changed, autoSave, notification);
        }

        public bool RecordKeypadDoorUnlocked(string doorId, bool autoSave = true)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(doorId)) return false;

            bool changed = false;
            string puzzleId = ProgressionIds.PuzzleForDoor(doorId);
            bool addedPuzzle = AddUnique(currentData.permanent.solvedPuzzleIds, puzzleId);
            changed |= addedPuzzle;

            ProgressionDiscoveryNotification notification = null;
            if (doorId == ProgressionIds.DoorBuildingASouth)
            {
                bool addedCode = AddUnique(currentData.permanent.learnedCodeIds, ProgressionIds.CodeBuildingASouthFromSewerClues);
                changed |= addedCode;
                changed |= AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneSecretRoomKeypadUnlocked);
                changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointSecretDoorUnlocked);
                if (addedCode || addedPuzzle)
                {
                    notification = new ProgressionDiscoveryNotification(
                        DiscoveryNotificationKind.Knowledge,
                        addedCode ? ProgressionIds.CodeBuildingASouthFromSewerClues : puzzleId);
                }
            }
            else if (doorId == ProgressionIds.SafeBackroomsOmegaVault)
            {
                bool addedCode = AddUnique(currentData.permanent.learnedCodeIds, ProgressionIds.CodeOmegaVaultSafe);
                changed |= addedCode;
                changed |= AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneOmegaSafeUnlocked);
                changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointOmegaSafeUnlocked);
                if (addedPuzzle)
                {
                    notification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Knowledge, puzzleId);
                }
            }
            else if (addedPuzzle)
            {
                notification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Knowledge, puzzleId);
            }

            return FinalizeMutation(changed, autoSave, notification);
        }

        public bool RecordBackroomsEntered(bool viaAzucenasManhole, bool autoSave = true)
        {
            EnsureInitialized();
            bool addedMain = AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneBackroomsEntered);
            string routeMilestone = viaAzucenasManhole
                ? ProgressionIds.MilestoneBackroomsEnteredViaAzucenas
                : ProgressionIds.MilestoneBackroomsEnteredViaVeil;
            bool addedRoute = AddUnique(currentData.permanent.unlockedMilestoneIds, routeMilestone);
            bool changed = addedMain || addedRoute;
            changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointBackroomsEntered);

            ProgressionDiscoveryNotification notification = null;
            if (addedMain)
            {
                notification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, ProgressionIds.MilestoneBackroomsEntered);
            }
            else if (addedRoute)
            {
                notification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, routeMilestone);
            }

            return FinalizeMutation(changed, autoSave, notification);
        }

        /// <summary>
        /// Records returning from BackroomsLevel1Scene to BorderCorridorScene.
        /// Explicitly never resets permanent discoveries, evolving states, or narrative checkpoints.
        /// </summary>
        public bool RecordReturnedFromBackroomsToCorridor(bool autoSave = true)
        {
            EnsureInitialized();
            bool added = AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneReturnedFromBackrooms);
            ProgressionDiscoveryNotification notification = added
                ? new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, ProgressionIds.MilestoneReturnedFromBackrooms)
                : null;
            return FinalizeMutation(added, autoSave, notification);
        }

        /// <summary>
        /// Explicitly and idempotently records that the player learned the Building A South
        /// Secret Room access code from the three underground clues, independently of whether
        /// the keypad door has been unlocked yet.
        /// </summary>
        public bool RecordBuildingASouthCodeLearnedFromSewerClues(bool autoSave = true)
        {
            EnsureInitialized();
            bool addedCode = AddUnique(currentData.permanent.learnedCodeIds, ProgressionIds.CodeBuildingASouthFromSewerClues);
            if (!addedCode) return false;

            var notification = new ProgressionDiscoveryNotification(
                DiscoveryNotificationKind.Knowledge,
                ProgressionIds.CodeBuildingASouthFromSewerClues);
            return FinalizeMutation(true, autoSave, notification);
        }

        public bool RecordEvidenceInspected(string evidenceId, bool autoSave = true)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(evidenceId)) return false;

            string trimmed = evidenceId.Trim();
            bool addedEvidence = AddUnique(currentData.permanent.discoveredEvidenceIds, trimmed);
            bool changed = addedEvidence;
            bool addedOmegaCode = false;

            if (trimmed == ProgressionIds.ArtifactEvidenceBoardPdn00)
            {
                addedOmegaCode = AddUnique(currentData.permanent.learnedCodeIds, ProgressionIds.CodeOmegaVaultSafe);
                changed |= addedOmegaCode;
                changed |= AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneEvidenceBoardPdn00Discovered);
                changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointEvidenceBoardPdn00Inspected);
            }

            ProgressionDiscoveryNotification primaryNotification = null;
            if (addedOmegaCode)
            {
                primaryNotification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Knowledge, ProgressionIds.CodeOmegaVaultSafe);
            }
            else if (addedEvidence)
            {
                primaryNotification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Evidence, trimmed);
            }

            bool sewerSequenceCompletedNow =
                (trimmed == ProgressionIds.ClueSewerSectorC0 ||
                 trimmed == ProgressionIds.ClueCisternElNudo ||
                 trimmed == ProgressionIds.ClueAbasoloSiphon) &&
                HasDiscoveredAllThreeSewerClues() &&
                !HasLearnedCode(ProgressionIds.CodeBuildingASouthFromSewerClues);

            // If all three sewer clues are now discovered, defer the disk flush until the learned-code
            // entry is also recorded so both mutations persist atomically while emitting both deduplicated notifications.
            bool recordedPrimary = FinalizeMutation(changed, autoSave && !sewerSequenceCompletedNow, primaryNotification);
            bool recordedCode = false;
            if (sewerSequenceCompletedNow)
            {
                recordedCode = RecordBuildingASouthCodeLearnedFromSewerClues(autoSave);
            }

            return recordedPrimary || recordedCode;
        }

        public bool RecordOjoDelUmbralAcquired(bool autoSave = true)
        {
            EnsureInitialized();
            bool addedArtifact = AddUnique(currentData.permanent.acquiredArtifactIds, ProgressionIds.ArtifactOjoDelUmbral);
            bool changed = addedArtifact;
            changed |= AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneOjoDelUmbralAcquired);

            // Idempotent placement transition: only transition None -> Carried so calling this
            // again while already socketed in El Umbral de Lasser never regresses portal state.
            if (currentData.evolving.ojoDelUmbralPlacement == ProgressionIds.PlacementNone)
            {
                currentData.evolving.ojoDelUmbralPlacement = ProgressionIds.PlacementCarried;
                changed = true;
            }

            changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointOjoDelUmbralAcquired);
            ProgressionDiscoveryNotification notification = addedArtifact
                ? new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Artifact, ProgressionIds.ArtifactOjoDelUmbral)
                : null;
            return FinalizeMutation(changed, autoSave, notification);
        }

        public bool RecordLasserPortalState(bool isPortalActivated, bool autoSave = true)
        {
            EnsureInitialized();
            bool changed = false;
            ProgressionDiscoveryNotification notification = null;

            // Interacting with the portal socket always implies the player acquired El Ojo del Umbral
            changed |= AddUnique(currentData.permanent.acquiredArtifactIds, ProgressionIds.ArtifactOjoDelUmbral);
            changed |= AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneOjoDelUmbralAcquired);

            if (isPortalActivated)
            {
                bool addedPuzzle = AddUnique(currentData.permanent.solvedPuzzleIds, ProgressionIds.PuzzleLasserPortalActivation);
                bool addedMilestone = AddUnique(currentData.permanent.unlockedMilestoneIds, ProgressionIds.MilestoneLasserPortalActivated);
                changed |= addedPuzzle || addedMilestone;
                changed |= TryAdvanceCheckpoint(ProgressionIds.CheckpointLasserPortalActivated);

                if (addedMilestone || addedPuzzle)
                {
                    notification = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Milestone, ProgressionIds.MilestoneLasserPortalActivated);
                }

                if (currentData.evolving.ojoDelUmbralPlacement != ProgressionIds.PlacementSocketedLasserPortal)
                {
                    currentData.evolving.ojoDelUmbralPlacement = ProgressionIds.PlacementSocketedLasserPortal;
                    changed = true;
                }
                if (!currentData.evolving.lasserPortalCurrentlyActive)
                {
                    currentData.evolving.lasserPortalCurrentlyActive = true;
                    changed = true;
                }
            }
            else
            {
                // Removing the astrolabe updates evolving state without erasing permanent discovery/activation milestone
                if (currentData.evolving.ojoDelUmbralPlacement != ProgressionIds.PlacementCarried)
                {
                    currentData.evolving.ojoDelUmbralPlacement = ProgressionIds.PlacementCarried;
                    changed = true;
                }
                if (currentData.evolving.lasserPortalCurrentlyActive)
                {
                    currentData.evolving.lasserPortalCurrentlyActive = false;
                    changed = true;
                }
            }

            return FinalizeMutation(changed, autoSave, notification);
        }

        public bool SetEvolvingFlag(string key, string value, bool autoSave = true)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(key)) return false;

            string trimmedKey = key.Trim();
            string safeVal = value ?? string.Empty;
            string nowIso = ProgressionSaveData.FormatUtc(utcNowProvider());

            List<NarrativeStateEntry> flags = currentData.evolving.flags;
            for (int i = 0; i < flags.Count; i++)
            {
                if (string.Equals(flags[i].key, trimmedKey, StringComparison.Ordinal))
                {
                    if (string.Equals(flags[i].value, safeVal, StringComparison.Ordinal))
                    {
                        return false;
                    }
                    flags[i].value = safeVal;
                    flags[i].updatedAtUtc = nowIso;
                    return FinalizeMutation(true, autoSave);
                }
            }

            flags.Add(new NarrativeStateEntry
            {
                key = trimmedKey,
                value = safeVal,
                updatedAtUtc = nowIso
            });
            return FinalizeMutation(true, autoSave);
        }

        public bool SetTemporalEntry(string id, string state, DateTime? expiresAtUtc = null, bool autoSave = true)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(id)) return false;

            string trimmedId = id.Trim();
            string safeState = state ?? string.Empty;
            string nowIso = ProgressionSaveData.FormatUtc(utcNowProvider());
            string expiresIso = expiresAtUtc.HasValue ? ProgressionSaveData.FormatUtc(expiresAtUtc.Value) : string.Empty;

            List<TemporalProgressionEntry> list = currentData.temporal.entries;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].id, trimmedId, StringComparison.Ordinal))
                {
                    list[i].state = safeState;
                    list[i].recordedAtUtc = nowIso;
                    list[i].expiresAtUtc = expiresIso;
                    return FinalizeMutation(true, autoSave);
                }
            }

            list.Add(new TemporalProgressionEntry
            {
                id = trimmedId,
                state = safeState,
                recordedAtUtc = nowIso,
                expiresAtUtc = expiresIso
            });
            return FinalizeMutation(true, autoSave);
        }

        /// <summary>
        /// Removes expired temporal entries without touching permanent or evolving progression.
        /// Never resets progression simply because a calendar day changes.
        /// </summary>
        public int PruneExpiredTemporalEntries(bool autoSave = false)
        {
            EnsureInitialized();
            DateTime now = utcNowProvider();
            List<TemporalProgressionEntry> list = currentData.temporal.entries;
            int removed = 0;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                TemporalProgressionEntry entry = list[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.expiresAtUtc)) continue;

                if (DateTime.TryParse(
                    entry.expiresAtUtc,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out DateTime expiryUtc))
                {
                    if (now >= expiryUtc)
                    {
                        list.RemoveAt(i);
                        removed++;
                    }
                }
            }

            if (removed > 0)
            {
                FinalizeMutation(true, autoSave);
            }
            return removed;
        }

        /// <summary>
        /// Explicitly clears temporal entries without affecting permanent or evolving progression.
        /// </summary>
        public int ClearTemporalEntries(bool autoSave = true)
        {
            EnsureInitialized();
            int count = currentData.temporal.entries.Count;
            if (count == 0) return 0;

            currentData.temporal.entries.Clear();
            FinalizeMutation(true, autoSave);
            return count;
        }

        public string GetLocalizedCheckpointLabel(bool isEs)
        {
            switch (LatestCheckpointId)
            {
                case ProgressionIds.CheckpointLasserPortalActivated:
                    return isEs ? "El Umbral de Lasser activado" : "The Threshold of Lasser Activated";
                case ProgressionIds.CheckpointOjoDelUmbralAcquired:
                    return isEs ? "El Ojo del Umbral obtenido" : "The Eye of the Threshold Acquired";
                case ProgressionIds.CheckpointOmegaSafeUnlocked:
                    return isEs ? "Caja Fuerte Nivel Ω abierta" : "Level-Ω Vault Safe Unlocked";
                case ProgressionIds.CheckpointEvidenceBoardPdn00Inspected:
                    return isEs ? "Muro de Evidencias PDN-00 inspeccionado" : "Evidence Board PDN-00 Inspected";
                case ProgressionIds.CheckpointBackroomsEntered:
                    return isEs ? "Acceso a Nivel 1 (Backrooms)" : "Entered Backrooms Level 1";
                case ProgressionIds.CheckpointSecretDoorUnlocked:
                    return isEs ? "Puerta secreta desbloqueada" : "Secret Door Unlocked";
                case ProgressionIds.CheckpointUndergroundEntered:
                    return isEs ? "Red subterránea explorada" : "Underground Network Reached";
                default:
                    return isEs ? "Inicio en Av. Benito Juárez" : "Arrival on Av. Benito Juárez";
            }
        }

        private bool TryAdvanceCheckpoint(string candidateCheckpointId)
        {
            if (string.IsNullOrWhiteSpace(candidateCheckpointId)) return false;

            int currentRank = ProgressionIds.GetCheckpointRank(currentData.latestCheckpointId);
            int candidateRank = ProgressionIds.GetCheckpointRank(candidateCheckpointId);

            // Advance if higher rank or unrecognized future checkpoint ID; never regress known checkpoints
            if (candidateRank < 0 || candidateRank > currentRank)
            {
                currentData.latestCheckpointId = candidateCheckpointId;
                currentData.latestCheckpointAtUtc = ProgressionSaveData.FormatUtc(utcNowProvider());
                return true;
            }
            return false;
        }

        private bool FinalizeMutation(bool changed, bool autoSave, ProgressionDiscoveryNotification notification = null)
        {
            if (!changed) return false;
            hasUnsavedChanges = true;
            if (autoSave)
            {
                SaveNow();
            }
            OnProgressionChanged?.Invoke(currentData);
            if (notification != null)
            {
                OnNewDiscoveryRecorded?.Invoke(notification);
            }
            return true;
        }

        private static bool ContainsId(List<string> list, string id)
        {
            if (list == null || string.IsNullOrWhiteSpace(id)) return false;
            string trimmed = id.Trim();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], trimmed, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool AddUnique(List<string> list, string id)
        {
            if (list == null || string.IsNullOrWhiteSpace(id)) return false;
            string trimmed = id.Trim();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], trimmed, StringComparison.Ordinal)) return false;
            }
            list.Add(trimmed);
            return true;
        }
    }
}
