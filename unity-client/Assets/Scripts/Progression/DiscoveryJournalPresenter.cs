using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyVirtualCommunity.Client.Progression
{
    public enum JournalCategory
    {
        Evidence = 0,
        Knowledge = 1,
        Artifacts = 2,
        Milestones = 3
    }

    public sealed class JournalEntryView
    {
        public string Title { get; }
        public string StatusBadge { get; }
        public string Description { get; }
        public string SecondaryNote { get; }
        public Color AccentColor { get; }

        public JournalEntryView(string title, string statusBadge, string description, string secondaryNote, Color accentColor)
        {
            Title = title ?? string.Empty;
            StatusBadge = statusBadge ?? string.Empty;
            Description = description ?? string.Empty;
            SecondaryNote = secondaryNote ?? string.Empty;
            AccentColor = accentColor;
        }
    }

    public sealed class DiscoveryJournalSnapshot
    {
        public string CurrentCheckpointLabel { get; }
        public bool IsWriteProtected { get; }
        public string WriteProtectionWarning { get; }
        public IReadOnlyList<JournalEntryView> EvidenceEntries { get; }
        public IReadOnlyList<JournalEntryView> KnowledgeEntries { get; }
        public IReadOnlyList<JournalEntryView> ArtifactEntries { get; }
        public IReadOnlyList<JournalEntryView> MilestoneEntries { get; }
        public IReadOnlyList<JournalEntryView> ActiveTemporalEntries { get; }

        public int TotalDiscoveryCount =>
            EvidenceEntries.Count + KnowledgeEntries.Count + ArtifactEntries.Count + MilestoneEntries.Count;

        public bool IsEmpty => TotalDiscoveryCount == 0 && ActiveTemporalEntries.Count == 0;

        public DiscoveryJournalSnapshot(
            string currentCheckpointLabel,
            bool isWriteProtected,
            string writeProtectionWarning,
            List<JournalEntryView> evidenceEntries,
            List<JournalEntryView> knowledgeEntries,
            List<JournalEntryView> artifactEntries,
            List<JournalEntryView> milestoneEntries,
            List<JournalEntryView> activeTemporalEntries)
        {
            CurrentCheckpointLabel = currentCheckpointLabel ?? string.Empty;
            IsWriteProtected = isWriteProtected;
            WriteProtectionWarning = writeProtectionWarning ?? string.Empty;
            EvidenceEntries = evidenceEntries;
            KnowledgeEntries = knowledgeEntries;
            ArtifactEntries = artifactEntries;
            MilestoneEntries = milestoneEntries;
            ActiveTemporalEntries = activeTemporalEntries;
        }
    }

    /// <summary>
    /// Pure read-only presenter translating PlayerProgressionService / ProgressionSaveData
    /// into localized Discovery Journal entries. Never mutates progression state, never writes
    /// files, never reveals undiscovered entries, and never exposes raw technical IDs.
    /// </summary>
    public static class DiscoveryJournalPresenter
    {
        public static DiscoveryJournalSnapshot BuildSnapshot(
            PlayerProgressionService progression,
            bool isEs,
            Func<string, string> liveDoorCodeResolver = null,
            Func<string, (string title, string lore)?> liveEvidenceResolver = null,
            DateTime? utcNowOverride = null)
        {
            if (progression == null)
            {
                return new DiscoveryJournalSnapshot(
                    isEs ? "Inicio en Av. Benito Juárez" : "Arrival on Av. Benito Juárez",
                    false,
                    string.Empty,
                    new List<JournalEntryView>(),
                    new List<JournalEntryView>(),
                    new List<JournalEntryView>(),
                    new List<JournalEntryView>(),
                    new List<JournalEntryView>());
            }

            ProgressionSaveData data = progression.CurrentData;
            DateTime utcNow = utcNowOverride ?? DateTime.UtcNow;

            bool isWriteProtected = progression.IsWriteProtectedDueToLoadFailure;
            string warning = isWriteProtected
                ? (isEs
                    ? "Aviso: el archivo de guardado previo no pudo cargarse y se conserva intacto. Es posible que los descubrimientos de esta sesión no se guarden hasta iniciar una Nueva Partida en Pausa."
                    : "Notice: the previous save file could not be loaded and has been preserved untouched. Discoveries made during this session may not be saved until you start a New Game in the Pause Menu.")
                : string.Empty;

            var evidenceEntries = BuildEvidenceEntries(data.permanent.discoveredEvidenceIds, isEs, liveEvidenceResolver);
            var knowledgeEntries = BuildKnowledgeEntries(data.permanent.learnedCodeIds, data.permanent.solvedPuzzleIds, isEs, liveDoorCodeResolver);
            var artifactEntries = BuildArtifactEntries(progression, data, isEs);
            var milestoneEntries = BuildMilestoneEntries(data.permanent.unlockedMilestoneIds, isEs);
            var temporalEntries = BuildActiveTemporalEntries(data.temporal.entries, isEs, utcNow);

            return new DiscoveryJournalSnapshot(
                progression.GetLocalizedCheckpointLabel(isEs),
                isWriteProtected,
                warning,
                evidenceEntries,
                knowledgeEntries,
                artifactEntries,
                milestoneEntries,
                temporalEntries);
        }

        private static List<JournalEntryView> BuildEvidenceEntries(
            List<string> discoveredEvidenceIds,
            bool isEs,
            Func<string, (string title, string lore)?> liveEvidenceResolver)
        {
            var result = new List<JournalEntryView>();
            if (discoveredEvidenceIds == null) return result;

            string discoveredBadge = isEs ? "Descubierta" : "Discovered";

            for (int i = 0; i < discoveredEvidenceIds.Count; i++)
            {
                string id = discoveredEvidenceIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                DiscoveryJournalCatalog.LocalizedEntryMetadata meta = DiscoveryJournalCatalog.GetEvidenceOrFallback(id);
                string title = meta.GetTitle(isEs);
                string description = meta.GetSummary(isEs);

                if (liveEvidenceResolver != null)
                {
                    var live = liveEvidenceResolver(id);
                    if (live.HasValue)
                    {
                        if (!string.IsNullOrWhiteSpace(live.Value.title)) title = live.Value.title;
                        if (!string.IsNullOrWhiteSpace(live.Value.lore)) description = live.Value.lore;
                    }
                }

                result.Add(new JournalEntryView(
                    title,
                    discoveredBadge,
                    description,
                    string.Empty,
                    meta.AccentColor));
            }

            return result;
        }

        private static List<JournalEntryView> BuildKnowledgeEntries(
            List<string> learnedCodeIds,
            List<string> solvedPuzzleIds,
            bool isEs,
            Func<string, string> liveDoorCodeResolver)
        {
            var result = new List<JournalEntryView>();

            if (learnedCodeIds != null)
            {
                for (int i = 0; i < learnedCodeIds.Count; i++)
                {
                    string codeId = learnedCodeIds[i];
                    if (string.IsNullOrWhiteSpace(codeId)) continue;

                    if (DiscoveryJournalCatalog.TryGetLearnedCode(codeId, out DiscoveryJournalCatalog.LearnedCodeMetadata meta))
                    {
                        string resolvedCode = liveDoorCodeResolver != null
                            ? liveDoorCodeResolver(meta.AssociatedDoorId)
                            : null;

                        string secondary;
                        if (!string.IsNullOrWhiteSpace(resolvedCode))
                        {
                            secondary = isEs
                                ? $"Código registrado: {resolvedCode.Trim()}"
                                : $"Recorded code: {resolvedCode.Trim()}";
                        }
                        else
                        {
                            secondary = isEs
                                ? "Estado: Código aprendido (el valor activo se valida al acceder al mecanismo correspondiente)."
                                : "Status: Code learned (the active value is verified when approaching the corresponding mechanism).";
                        }

                        result.Add(new JournalEntryView(
                            meta.GetTitle(isEs),
                            isEs ? "Código aprendido" : "Code Learned",
                            meta.GetSummary(isEs),
                            secondary,
                            new Color(0.42f, 0.92f, 0.72f)));
                    }
                    else
                    {
                        result.Add(new JournalEntryView(
                            isEs ? "Conocimiento de acceso registrado" : "Recorded Access Knowledge",
                            isEs ? "Aprendido" : "Learned",
                            isEs
                                ? "Clave o dato de acceso aprendido y conservado de forma permanente."
                                : "Access code or detail learned and permanently retained.",
                            string.Empty,
                            new Color(0.42f, 0.92f, 0.72f)));
                    }
                }
            }

            if (solvedPuzzleIds != null)
            {
                for (int i = 0; i < solvedPuzzleIds.Count; i++)
                {
                    string puzzleId = solvedPuzzleIds[i];
                    if (string.IsNullOrWhiteSpace(puzzleId)) continue;

                    DiscoveryJournalCatalog.LocalizedEntryMetadata meta = DiscoveryJournalCatalog.GetPuzzleOrFallback(puzzleId);
                    result.Add(new JournalEntryView(
                        meta.GetTitle(isEs),
                        isEs ? "Resuelto" : "Solved",
                        meta.GetSummary(isEs),
                        string.Empty,
                        meta.AccentColor));
                }
            }

            return result;
        }

        private static List<JournalEntryView> BuildArtifactEntries(
            PlayerProgressionService progression,
            ProgressionSaveData data,
            bool isEs)
        {
            var result = new List<JournalEntryView>();
            List<string> acquired = data.permanent.acquiredArtifactIds;
            if (acquired == null) return result;

            for (int i = 0; i < acquired.Count; i++)
            {
                string artifactId = acquired[i];
                if (string.IsNullOrWhiteSpace(artifactId)) continue;

                DiscoveryJournalCatalog.LocalizedEntryMetadata meta = DiscoveryJournalCatalog.GetArtifactOrFallback(artifactId);

                if (artifactId == ProgressionIds.ArtifactOjoDelUmbral)
                {
                    string statusBadge;
                    string secondary;
                    string placement = data.evolving.ojoDelUmbralPlacement;
                    bool portalActive = progression.IsLasserPortalCurrentlyActive;
                    bool everActivated = progression.HasMilestone(ProgressionIds.MilestoneLasserPortalActivated) ||
                                         progression.IsPuzzleSolved(ProgressionIds.PuzzleLasserPortalActivation);

                    if (placement == ProgressionIds.PlacementSocketedLasserPortal && portalActive)
                    {
                        statusBadge = isEs ? "Colocado • Portal activo" : "Placed • Portal Active";
                        secondary = isEs
                            ? "Registro permanente: adquirido. Estado actual: colocado en El Umbral de Lasser (portal actualmente activo)."
                            : "Permanent record: acquired. Current state: placed in The Lasser Threshold (portal currently active).";
                    }
                    else if (placement == ProgressionIds.PlacementSocketedLasserPortal)
                    {
                        statusBadge = isEs ? "Colocado en El Umbral de Lasser" : "Placed in The Lasser Threshold";
                        secondary = isEs
                            ? "Registro permanente: adquirido. Estado actual: colocado en El Umbral de Lasser."
                            : "Permanent record: acquired. Current state: placed in The Lasser Threshold.";
                    }
                    else
                    {
                        statusBadge = isEs ? "En tu posesión" : "In Possession";
                        secondary = everActivated
                            ? (isEs
                                ? "Registro permanente: adquirido (El Umbral de Lasser fue activado previamente). Estado actual: en tu posesión; portal en reposo."
                                : "Permanent record: acquired (The Lasser Threshold was previously activated). Current state: in your possession; portal dormant.")
                            : (isEs
                                ? "Registro permanente: adquirido. Estado actual: en tu posesión."
                                : "Permanent record: acquired. Current state: in your possession.");
                    }

                    result.Add(new JournalEntryView(
                        meta.GetTitle(isEs),
                        statusBadge,
                        meta.GetSummary(isEs),
                        secondary,
                        meta.AccentColor));
                }
                else
                {
                    result.Add(new JournalEntryView(
                        meta.GetTitle(isEs),
                        isEs ? "Adquirido" : "Acquired",
                        meta.GetSummary(isEs),
                        string.Empty,
                        meta.AccentColor));
                }
            }

            return result;
        }

        private static List<JournalEntryView> BuildMilestoneEntries(List<string> unlockedMilestoneIds, bool isEs)
        {
            var result = new List<JournalEntryView>();
            if (unlockedMilestoneIds == null) return result;

            string badge = isEs ? "Registrado" : "Recorded";
            for (int i = 0; i < unlockedMilestoneIds.Count; i++)
            {
                string id = unlockedMilestoneIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                DiscoveryJournalCatalog.LocalizedEntryMetadata meta = DiscoveryJournalCatalog.GetMilestoneOrFallback(id);
                result.Add(new JournalEntryView(
                    meta.GetTitle(isEs),
                    badge,
                    meta.GetSummary(isEs),
                    string.Empty,
                    meta.AccentColor));
            }

            return result;
        }

        private static List<JournalEntryView> BuildActiveTemporalEntries(
            List<TemporalProgressionEntry> entries,
            bool isEs,
            DateTime utcNow)
        {
            var result = new List<JournalEntryView>();
            if (entries == null || entries.Count == 0) return result;

            for (int i = 0; i < entries.Count; i++)
            {
                TemporalProgressionEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;

                if (!string.IsNullOrWhiteSpace(entry.expiresAtUtc) &&
                    DateTime.TryParse(
                        entry.expiresAtUtc,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                        out DateTime expiryUtc) &&
                    utcNow >= expiryUtc)
                {
                    continue;
                }

                result.Add(new JournalEntryView(
                    isEs ? "Evento narrativo temporal activo" : "Active Temporary Narrative Note",
                    isEs ? "Temporal" : "Temporary",
                    isEs
                        ? "Nota temporal activa en tu sesión actual. Su expiración nunca borra descubrimientos permanentes."
                        : "Temporary note active in your current progression. Its expiration never erases permanent discoveries.",
                    string.Empty,
                    new Color(0.96f, 0.78f, 0.42f)));
            }

            return result;
        }

        /// <summary>
        /// Produces a concise, localized discovery feedback line for a genuinely new progression event.
        /// Never exposes raw technical IDs or reveals unlearned information.
        /// </summary>
        public static string FormatNewDiscoveryFeedback(ProgressionDiscoveryNotification notification, bool isEs)
        {
            if (notification == null) return string.Empty;

            switch (notification.Kind)
            {
                case DiscoveryNotificationKind.Evidence:
                {
                    string title = DiscoveryJournalCatalog.GetEvidenceOrFallback(notification.EntryId).GetTitle(isEs);
                    return isEs
                        ? $"Diario [J] • Nueva evidencia: {title}"
                        : $"Journal [J] • New evidence: {title}";
                }
                case DiscoveryNotificationKind.Knowledge:
                {
                    string title = DiscoveryJournalCatalog.TryGetLearnedCode(notification.EntryId, out var codeMeta)
                        ? codeMeta.GetTitle(isEs)
                        : DiscoveryJournalCatalog.GetPuzzleOrFallback(notification.EntryId).GetTitle(isEs);
                    return isEs
                        ? $"Diario [J] • Nuevo conocimiento: {title}"
                        : $"Journal [J] • New knowledge: {title}";
                }
                case DiscoveryNotificationKind.Artifact:
                {
                    string title = DiscoveryJournalCatalog.GetArtifactOrFallback(notification.EntryId).GetTitle(isEs);
                    return isEs
                        ? $"Diario [J] • Objeto registrado: {title}"
                        : $"Journal [J] • Artifact recorded: {title}";
                }
                case DiscoveryNotificationKind.Milestone:
                default:
                {
                    string title = DiscoveryJournalCatalog.GetMilestoneOrFallback(notification.EntryId).GetTitle(isEs);
                    return isEs
                        ? $"Diario [J] • Hito registrado: {title}"
                        : $"Journal [J] • Milestone recorded: {title}";
                }
            }
        }
    }
}
