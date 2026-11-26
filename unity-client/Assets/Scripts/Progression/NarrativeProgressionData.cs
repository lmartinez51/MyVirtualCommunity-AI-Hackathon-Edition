using System;
using System.Collections.Generic;

namespace MyVirtualCommunity.Client.Progression
{
    /// <summary>
    /// Canonical identifiers for implemented single-player narrative progression.
    /// Keeps all persisted keys explicit, player-local, and independent of World Core simulation state.
    /// </summary>
    public static class ProgressionIds
    {
        // Existing door & safe identifiers (matching BorderCorridorScene / BackroomsLevel1Scene)
        public const string DoorBuildingASouth = "door:building-a-south-secret-wedge";
        public const string SafeBackroomsOmegaVault = "safe:backrooms-level1-omega-vault";

        // Stable fictional gameplay access code assembled from the three underground clues (58 + 19 + 26)
        public const string DefaultBuildingASouthAccessCode = "581926";

        // Environmental clue identifiers (Underground Storm-Drain Network -> Building A South Secret Room)
        public const string ClueSewerSectorC0 = "clue:sewer-sector-c0-sequence-01";
        public const string ClueCisternElNudo = "clue:cistern-el-nudo-sequence-02";
        public const string ClueAbasoloSiphon = "clue:abasolo-siphon-sequence-03";

        // Remanencia Nivel 0 — Hotel de las Palomas environmental narrative discoveries (Implementation 07.6)
        public const string ClueRemanenciaHotelPlan01 = "clue:remanencia-hotel-plan-01";
        public const string ClueRemanenciaHotelRegister02 = "clue:remanencia-hotel-register-02";
        public const string ClueRemanenciaHotelMaintenance03 = "clue:remanencia-hotel-maintenance-03";
        public const string ClueRemanenciaHotelCobaltRumor04 = "clue:remanencia-hotel-cobalt-rumor-04";

        public static bool IsRemanenciaHotelNarrativeClue(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId)) return false;
            string trimmed = evidenceId.Trim();
            return trimmed == ClueRemanenciaHotelPlan01 ||
                   trimmed == ClueRemanenciaHotelRegister02 ||
                   trimmed == ClueRemanenciaHotelMaintenance03 ||
                   trimmed == ClueRemanenciaHotelCobaltRumor04;
        }

        // Existing artifact & portal identifiers (matching BackroomsLevel1Builder)
        public const string ArtifactEvidenceBoardPdn00 = "artifact:muro-evidencias-pdn";
        public const string ArtifactOjoDelUmbral = "artifact:ojo-del-umbral";
        public const string PortalUmbralDeLasser = "portal:umbral-de-lasser";
        public const string PortalCuencaOctagonal = "portal:cuenca-octagonal";

        // Original Backrooms Level 1 archival & anomalous artifact identifiers (IP-01 replacements)
        public const string ArtifactShiftClock0307 = "artifact:reloj-turno-0307";
        public const string ArtifactSealedNotebook = "artifact:cuaderno-paginas-selladas";
        public const string ArtifactServiceIntercom = "artifact:intercomunicador-servicio";
        public const string ArtifactSignalMonitor = "artifact:monitor-senal-sin-origen";
        public const string ArtifactSpecimenVitrine = "artifact:vitrina-figura-sin-rasgos";
        public const string ArtifactArticulatedWoodAssembly = "artifact:ensamble-madera-articulado";
        public const string ArtifactArchiveCase06 = "artifact:caja-archivo-06";
        public const string ArtifactSealedCustodyCase = "artifact:estuche-custodia-sellado";
        public const string ArtifactFracturedInsulator = "artifact:aislador-ceramico-fracturado";
        public const string ArtifactCoordinatePlottingPlate = "artifact:lamina-trazado-coordenadas";
        public const string ArtifactAnomalousGeometryAtlas = "artifact:atlas-geometria-anomala";
        public const string ArtifactFoldedTopologyCore = "artifact:nucleo-topologia-plegada";

        // Backward-compatible save-data migration map from retired prototype artifact IDs to canonical IDs.
        // Ensures pre-existing local progression saves keep all discovered entries without data loss.
        private static readonly Dictionary<string, string> LegacyEvidenceIdAliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["artifact:reloj-0307"] = ArtifactShiftClock0307,
                ["artifact:libro-babadook"] = ArtifactSealedNotebook,
                ["artifact:telefono-negro"] = ArtifactServiceIntercom,
                ["artifact:cinta-vhs-el-aro"] = ArtifactSignalMonitor,
                ["artifact:muneca-porcelana"] = ArtifactSpecimenVitrine,
                ["artifact:titere-espiral"] = ArtifactArticulatedWoodAssembly,
                ["artifact:muneco-good-guy"] = ArtifactArchiveCase06,
                ["artifact:caja-dybbuk"] = ArtifactSealedCustodyCase,
                ["artifact:mano-ceramica"] = ArtifactFracturedInsulator,
                ["artifact:tablero-ouija"] = ArtifactCoordinatePlottingPlate,
                ["artifact:grimorio-necronomicon"] = ArtifactAnomalousGeometryAtlas,
                ["artifact:configuracion-lamento"] = ArtifactFoldedTopologyCore
            };

        public static IReadOnlyDictionary<string, string> LegacyEvidenceIdMigrationMap => LegacyEvidenceIdAliases;

        public static string CanonicalizeEvidenceId(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId)) return string.Empty;
            string trimmed = evidenceId.Trim();
            return LegacyEvidenceIdAliases.TryGetValue(trimmed, out string canonical) ? canonical : trimmed;
        }

        // Learned code identifiers (logical knowledge, never rotating/authoritative backend state)
        // Legacy demo code ID retained for backward compatibility without granting the new sewer-clue code
        public const string CodeBuildingASouthSecretDoor = "code:building-a-south-secret-door";
        public const string CodeBuildingASouthFromSewerClues = "code:building-a-south-secret-door-from-sewer-clues";
        public const string CodeOmegaVaultSafe = "code:omega-vault-safe";

        // Permanently solved puzzle identifiers
        public const string PuzzleBuildingASouthDoor = "puzzle:door:building-a-south-secret-wedge";
        public const string PuzzleOmegaVaultSafe = "puzzle:safe:backrooms-level1-omega-vault";
        public const string PuzzleLasserPortalActivation = "puzzle:lasser-portal-activation";

        // Personal access & narrative milestones
        public const string MilestoneUndergroundNetworkEntered = "milestone:underground-network-entered";
        public const string MilestoneRioBravoOutflowOpened = "milestone:rio-bravo-outflow-opened";
        public const string MilestoneSecretRoomKeypadUnlocked = "milestone:secret-room-keypad-unlocked";
        public const string MilestoneBackroomsEntered = "milestone:backrooms-entered";
        public const string MilestoneBackroomsEnteredViaVeil = "milestone:backrooms-entered-via-veil";
        public const string MilestoneBackroomsEnteredViaAzucenas = "milestone:backrooms-entered-via-azucenas-manhole";
        public const string MilestoneEvidenceBoardPdn00Discovered = "milestone:evidence-board-pdn00-discovered";
        public const string MilestoneOmegaSafeUnlocked = "milestone:omega-safe-unlocked";
        public const string MilestoneOjoDelUmbralAcquired = "milestone:ojo-del-umbral-acquired";
        public const string MilestoneLasserPortalActivated = "milestone:lasser-portal-activated";
        public const string MilestoneReturnedFromBackrooms = "milestone:returned-to-border-corridor-from-backrooms";

        // Logical narrative checkpoints (strictly independent from world spawn transform)
        public const string CheckpointArrival = "checkpoint:arrival";
        public const string CheckpointUndergroundEntered = "checkpoint:underground-entered";
        public const string CheckpointSecretDoorUnlocked = "checkpoint:secret-door-unlocked";
        public const string CheckpointBackroomsEntered = "checkpoint:backrooms-entered";
        public const string CheckpointEvidenceBoardPdn00Inspected = "checkpoint:evidence-board-pdn00-inspected";
        public const string CheckpointOmegaSafeUnlocked = "checkpoint:omega-safe-unlocked";
        public const string CheckpointOjoDelUmbralAcquired = "checkpoint:ojo-del-umbral-acquired";
        public const string CheckpointLasserPortalActivated = "checkpoint:lasser-portal-activated";

        // Evolving placement values for El Ojo del Umbral
        public const string PlacementNone = "";
        public const string PlacementCarried = "carried";
        public const string PlacementSocketedLasserPortal = "socketed_lasser_portal";

        public static string PuzzleForDoor(string doorId)
        {
            if (string.IsNullOrWhiteSpace(doorId)) return string.Empty;
            if (doorId == DoorBuildingASouth) return PuzzleBuildingASouthDoor;
            if (doorId == SafeBackroomsOmegaVault) return PuzzleOmegaVaultSafe;
            return "puzzle:" + doorId.Trim();
        }

        /// <summary>
        /// Logical rank for implemented milestones so revisiting an earlier area never regresses
        /// the player's latest narrative checkpoint. Unknown future checkpoints return -1 and always update.
        /// </summary>
        public static int GetCheckpointRank(string checkpointId)
        {
            switch (checkpointId)
            {
                case CheckpointArrival: return 0;
                case CheckpointUndergroundEntered: return 10;
                case CheckpointSecretDoorUnlocked: return 20;
                case CheckpointBackroomsEntered: return 30;
                case CheckpointEvidenceBoardPdn00Inspected: return 40;
                case CheckpointOmegaSafeUnlocked: return 50;
                case CheckpointOjoDelUmbralAcquired: return 60;
                case CheckpointLasserPortalActivated: return 70;
                default: return -1;
            }
        }
    }

    /// <summary>
    /// Versioned local single-player narrative progression document serialized to JSON.
    /// Strictly player-specific: never stores player transforms, World Core clock, NPC schedules,
    /// business operating status/prices, citizen reports, or shared community state.
    /// </summary>
    [Serializable]
    public class ProgressionSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string saveId = string.Empty;
        public string createdAtUtc = string.Empty;
        public string updatedAtUtc = string.Empty;
        public int saveSequence = 0;

        // Logical narrative checkpoint (independent from world entry/spawn location)
        public string latestCheckpointId = ProgressionIds.CheckpointArrival;
        public string latestCheckpointAtUtc = string.Empty;

        // Category A: Permanent player discoveries, knowledge, solved puzzles, artifacts, milestones
        public PermanentProgressionState permanent = new PermanentProgressionState();

        // Category B: Evolving player-specific narrative consequences for implemented mechanics
        public EvolvingProgressionState evolving = new EvolvingProgressionState();

        // Category C: Minimal container for future temporal/repeatable narrative states
        public TemporalProgressionState temporal = new TemporalProgressionState();

        public static ProgressionSaveData CreateNew(DateTime utcNow)
        {
            string iso = FormatUtc(utcNow);
            return new ProgressionSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                saveId = Guid.NewGuid().ToString("D"),
                createdAtUtc = iso,
                updatedAtUtc = iso,
                saveSequence = 0,
                latestCheckpointId = ProgressionIds.CheckpointArrival,
                latestCheckpointAtUtc = iso,
                permanent = new PermanentProgressionState(),
                evolving = new EvolvingProgressionState(),
                temporal = new TemporalProgressionState()
            };
        }

        public static string FormatUtc(DateTime utcTime)
        {
            DateTime normalized = utcTime.Kind == DateTimeKind.Utc ? utcTime : utcTime.ToUniversalTime();
            return normalized.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// Ensures nested objects and lists are initialized and deduplicated without altering meaning.
        /// </summary>
        public void Normalize()
        {
            if (permanent == null) permanent = new PermanentProgressionState();
            if (evolving == null) evolving = new EvolvingProgressionState();
            if (temporal == null) temporal = new TemporalProgressionState();

            permanent.Normalize();
            evolving.Normalize();
            temporal.Normalize();

            if (string.IsNullOrWhiteSpace(latestCheckpointId))
            {
                latestCheckpointId = ProgressionIds.CheckpointArrival;
            }
            if (saveSequence < 0)
            {
                saveSequence = 0;
            }
        }

        public ProgressionSaveData DeepClone()
        {
            Normalize();
            var clone = new ProgressionSaveData
            {
                schemaVersion = schemaVersion,
                saveId = saveId,
                createdAtUtc = createdAtUtc,
                updatedAtUtc = updatedAtUtc,
                saveSequence = saveSequence,
                latestCheckpointId = latestCheckpointId,
                latestCheckpointAtUtc = latestCheckpointAtUtc,
                permanent = permanent.DeepClone(),
                evolving = evolving.DeepClone(),
                temporal = temporal.DeepClone()
            };
            return clone;
        }
    }

    /// <summary>
    /// Category A: Permanent progression. Survives restarts, scene unloads, and temporal state changes.
    /// </summary>
    [Serializable]
    public class PermanentProgressionState
    {
        public List<string> discoveredEvidenceIds = new List<string>();
        public List<string> learnedCodeIds = new List<string>();
        public List<string> solvedPuzzleIds = new List<string>();
        public List<string> acquiredArtifactIds = new List<string>();
        public List<string> unlockedMilestoneIds = new List<string>();

        public void Normalize()
        {
            discoveredEvidenceIds = CanonicalizeAndDeduplicateEvidenceIds(discoveredEvidenceIds);
            learnedCodeIds = DeduplicateStrings(learnedCodeIds);
            solvedPuzzleIds = DeduplicateStrings(solvedPuzzleIds);
            acquiredArtifactIds = DeduplicateStrings(acquiredArtifactIds);
            unlockedMilestoneIds = DeduplicateStrings(unlockedMilestoneIds);
        }

        private static List<string> CanonicalizeAndDeduplicateEvidenceIds(List<string> source)
        {
            var result = new List<string>();
            if (source == null) return result;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string canonical = ProgressionIds.CanonicalizeEvidenceId(source[i]);
                if (string.IsNullOrEmpty(canonical)) continue;
                if (seen.Add(canonical))
                {
                    result.Add(canonical);
                }
            }
            return result;
        }

        public PermanentProgressionState DeepClone()
        {
            return new PermanentProgressionState
            {
                discoveredEvidenceIds = new List<string>(discoveredEvidenceIds ?? new List<string>()),
                learnedCodeIds = new List<string>(learnedCodeIds ?? new List<string>()),
                solvedPuzzleIds = new List<string>(solvedPuzzleIds ?? new List<string>()),
                acquiredArtifactIds = new List<string>(acquiredArtifactIds ?? new List<string>()),
                unlockedMilestoneIds = new List<string>(unlockedMilestoneIds ?? new List<string>())
            };
        }

        internal static List<string> DeduplicateStrings(List<string> source)
        {
            var result = new List<string>();
            if (source == null) return result;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string item = source[i];
                if (string.IsNullOrWhiteSpace(item)) continue;
                string trimmed = item.Trim();
                if (seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Category B: Evolving progression. Tracks only implemented, player-specific narrative
    /// consequences that can change across play without duplicating World Core simulation state.
    /// </summary>
    [Serializable]
    public class EvolvingProgressionState
    {
        public string ojoDelUmbralPlacement = ProgressionIds.PlacementNone;
        public bool lasserPortalCurrentlyActive = false;
        public bool sewerOutflowGateOpened = false;
        public List<NarrativeStateEntry> flags = new List<NarrativeStateEntry>();

        public void Normalize()
        {
            if (ojoDelUmbralPlacement != ProgressionIds.PlacementNone &&
                ojoDelUmbralPlacement != ProgressionIds.PlacementCarried &&
                ojoDelUmbralPlacement != ProgressionIds.PlacementSocketedLasserPortal)
            {
                ojoDelUmbralPlacement = lasserPortalCurrentlyActive
                    ? ProgressionIds.PlacementSocketedLasserPortal
                    : ProgressionIds.PlacementNone;
            }

            if (flags == null)
            {
                flags = new List<NarrativeStateEntry>();
            }
            else
            {
                var normalized = new List<NarrativeStateEntry>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = flags.Count - 1; i >= 0; i--)
                {
                    NarrativeStateEntry entry = flags[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                    string key = entry.key.Trim();
                    if (seen.Add(key))
                    {
                        normalized.Add(new NarrativeStateEntry
                        {
                            key = key,
                            value = entry.value ?? string.Empty,
                            updatedAtUtc = entry.updatedAtUtc ?? string.Empty
                        });
                    }
                }
                normalized.Reverse();
                flags = normalized;
            }
        }

        public EvolvingProgressionState DeepClone()
        {
            var clonedFlags = new List<NarrativeStateEntry>();
            if (flags != null)
            {
                for (int i = 0; i < flags.Count; i++)
                {
                    var f = flags[i];
                    if (f == null) continue;
                    clonedFlags.Add(new NarrativeStateEntry
                    {
                        key = f.key,
                        value = f.value,
                        updatedAtUtc = f.updatedAtUtc
                    });
                }
            }
            return new EvolvingProgressionState
            {
                ojoDelUmbralPlacement = ojoDelUmbralPlacement ?? ProgressionIds.PlacementNone,
                lasserPortalCurrentlyActive = lasserPortalCurrentlyActive,
                sewerOutflowGateOpened = sewerOutflowGateOpened,
                flags = clonedFlags
            };
        }
    }

    [Serializable]
    public class NarrativeStateEntry
    {
        public string key = string.Empty;
        public string value = string.Empty;
        public string updatedAtUtc = string.Empty;
    }

    /// <summary>
    /// Category C: Temporal or repeatable progression.
    /// Minimal representation ensuring the schema does not assume every narrative state is permanent.
    /// Expiring or clearing temporal entries never erases permanent or evolving progression.
    /// </summary>
    [Serializable]
    public class TemporalProgressionState
    {
        public List<TemporalProgressionEntry> entries = new List<TemporalProgressionEntry>();

        public void Normalize()
        {
            if (entries == null)
            {
                entries = new List<TemporalProgressionEntry>();
                return;
            }
            var normalized = new List<TemporalProgressionEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                TemporalProgressionEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                string id = entry.id.Trim();
                if (seen.Add(id))
                {
                    normalized.Add(new TemporalProgressionEntry
                    {
                        id = id,
                        state = entry.state ?? string.Empty,
                        recordedAtUtc = entry.recordedAtUtc ?? string.Empty,
                        expiresAtUtc = entry.expiresAtUtc ?? string.Empty
                    });
                }
            }
            normalized.Reverse();
            entries = normalized;
        }

        public TemporalProgressionState DeepClone()
        {
            var cloned = new List<TemporalProgressionEntry>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e == null) continue;
                    cloned.Add(new TemporalProgressionEntry
                    {
                        id = e.id,
                        state = e.state,
                        recordedAtUtc = e.recordedAtUtc,
                        expiresAtUtc = e.expiresAtUtc
                    });
                }
            }
            return new TemporalProgressionState { entries = cloned };
        }
    }

    [Serializable]
    public class TemporalProgressionEntry
    {
        public string id = string.Empty;
        public string state = string.Empty;
        public string recordedAtUtc = string.Empty;
        public string expiresAtUtc = string.Empty;
    }
}
