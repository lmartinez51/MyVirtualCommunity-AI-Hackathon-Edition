using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyVirtualCommunity.Client.Progression
{
    /// <summary>
    /// Focused, read-only Unity IMGUI presentation overlay for the Player Discovery Journal.
    /// Renders the four localized categories (Evidence, Knowledge, Artifacts, Places & Milestones)
    /// derived directly from PlayerProgressionService via DiscoveryJournalPresenter.
    /// </summary>
    public sealed class DiscoveryJournalOverlay
    {
        private JournalCategory activeCategory = JournalCategory.Evidence;
        private Vector2 scrollPosition = Vector2.zero;

        private Texture2D entryCardTex;
        private Texture2D warningCardTex;
        private GUIStyle entryCardStyle;
        private GUIStyle warningCardStyle;

        public JournalCategory ActiveCategory => activeCategory;

        public void SelectCategory(JournalCategory category)
        {
            if (activeCategory != category)
            {
                activeCategory = category;
                scrollPosition = Vector2.zero;
            }
        }

        public void ResetScroll()
        {
            scrollPosition = Vector2.zero;
        }

        public void Dispose()
        {
            if (entryCardTex != null)
            {
                UnityEngine.Object.Destroy(entryCardTex);
                entryCardTex = null;
            }
            if (warningCardTex != null)
            {
                UnityEngine.Object.Destroy(warningCardTex);
                warningCardTex = null;
            }
            entryCardStyle = null;
            warningCardStyle = null;
        }

        public void Draw(
            DiscoveryJournalSnapshot snapshot,
            bool isEs,
            bool openedFromPauseMenu,
            GUIStyle modalCardStyle,
            GUIStyle titleStyle,
            GUIStyle subtitleStyle,
            GUIStyle mutedStyle,
            Action onClose)
        {
            EnsureStyles();

            float width = Mathf.Clamp(Screen.width - 64f, 520f, 760f);
            float height = Mathf.Clamp(Screen.height - 76f, 400f, 580f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, modalCardStyle);

            GUILayout.BeginArea(new Rect(x + 20f, y + 16f, width - 40f, height - 32f));
            GUILayout.BeginVertical();

            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(
                isEs ? "Diario de Descubrimientos" : "Discovery Journal",
                titleStyle);
            GUILayout.Label(
                isEs
                    ? $"Hito narrativo actual: {snapshot.CurrentCheckpointLabel}"
                    : $"Current narrative milestone: {snapshot.CurrentCheckpointLabel}",
                subtitleStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            string closeText = openedFromPauseMenu
                ? (isEs ? "[X] Volver [Esc/J]" : "[X] Back [Esc/J]")
                : (isEs ? "[X] Cerrar [Esc/J]" : "[X] Close [Esc/J]");
            if (GUILayout.Button(closeText, GUILayout.Width(134f), GUILayout.Height(28f)))
            {
                onClose?.Invoke();
            }
            GUILayout.EndHorizontal();

            // Write-protection warning banner when save failed to load
            if (snapshot.IsWriteProtected && !string.IsNullOrEmpty(snapshot.WriteProtectionWarning))
            {
                GUILayout.Space(6f);
                GUILayout.BeginVertical(warningCardStyle);
                GUIStyle warnStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    normal = { textColor = new Color(1.0f, 0.80f, 0.36f) }
                };
                GUILayout.Label(snapshot.WriteProtectionWarning, warnStyle);
                GUILayout.EndVertical();
            }

            GUILayout.Space(8f);

            // Four category tabs
            GUILayout.BeginHorizontal();
            DrawCategoryTab(
                JournalCategory.Evidence,
                isEs ? $"Evidencias ({snapshot.EvidenceEntries.Count})" : $"Evidence ({snapshot.EvidenceEntries.Count})");
            DrawCategoryTab(
                JournalCategory.Knowledge,
                isEs ? $"Conocimientos ({snapshot.KnowledgeEntries.Count})" : $"Knowledge ({snapshot.KnowledgeEntries.Count})");
            DrawCategoryTab(
                JournalCategory.Artifacts,
                isEs ? $"Objetos ({snapshot.ArtifactEntries.Count})" : $"Artifacts ({snapshot.ArtifactEntries.Count})");
            DrawCategoryTab(
                JournalCategory.Milestones,
                isEs ? $"Lugares e Hitos ({snapshot.MilestoneEntries.Count})" : $"Places & Milestones ({snapshot.MilestoneEntries.Count})");
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Scrollable entries list
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.ExpandHeight(true));
            DrawActiveCategoryContent(snapshot, isEs, mutedStyle);
            GUILayout.EndScrollView();

            GUILayout.Space(6f);

            // Footer hint
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                isEs
                    ? $"Registro personal local • Total de entradas: {snapshot.TotalDiscoveryCount}"
                    : $"Local personal record • Total entries: {snapshot.TotalDiscoveryCount}",
                mutedStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                isEs ? "Esc / J para cerrar" : "Esc / J to close",
                mutedStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawCategoryTab(JournalCategory category, string label)
        {
            Color prevBg = GUI.backgroundColor;
            if (activeCategory == category)
            {
                GUI.backgroundColor = new Color(0.28f, 0.78f, 0.96f);
            }

            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                SelectCategory(category);
            }

            GUI.backgroundColor = prevBg;
        }

        private void DrawActiveCategoryContent(DiscoveryJournalSnapshot snapshot, bool isEs, GUIStyle mutedStyle)
        {
            IReadOnlyList<JournalEntryView> entries;
            string emptyMessage;

            switch (activeCategory)
            {
                case JournalCategory.Evidence:
                    entries = snapshot.EvidenceEntries;
                    emptyMessage = isEs
                        ? "Aún no has registrado evidencias. Observa e inspecciona pistas u objetos relevantes durante tu exploración."
                        : "No evidence recorded yet. Observe and inspect relevant clues or objects during your exploration.";
                    break;

                case JournalCategory.Knowledge:
                    entries = snapshot.KnowledgeEntries;
                    emptyMessage = isEs
                        ? "Aún no has aprendido códigos ni resuelto mecanismos. Los accesos y claves que descubras aparecerán aquí."
                        : "No codes learned or mechanisms solved yet. Access codes and solutions you discover will appear here.";
                    break;

                case JournalCategory.Artifacts:
                    entries = snapshot.ArtifactEntries;
                    emptyMessage = isEs
                        ? "Aún no has obtenido objetos narrativos."
                        : "No narrative artifacts acquired yet.";
                    break;

                case JournalCategory.Milestones:
                default:
                    entries = snapshot.MilestoneEntries;
                    emptyMessage = isEs
                        ? "Aún no has registrado hitos de exploración. Recorre Avenida Benito Juárez y sus alrededores para comenzar."
                        : "No exploration milestones recorded yet. Explore Avenida Benito Juárez and its surroundings to begin.";
                    break;
            }

            if (entries.Count == 0)
            {
                GUILayout.Space(18f);
                GUIStyle emptyStyle = new GUIStyle(mutedStyle)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUILayout.Label(emptyMessage, emptyStyle);
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DrawEntryCard(entries[i]);
                    GUILayout.Space(6f);
                }
            }

            // Show active non-expired temporal notes only when present under Milestones tab
            if (activeCategory == JournalCategory.Milestones && snapshot.ActiveTemporalEntries.Count > 0)
            {
                GUILayout.Space(8f);
                GUIStyle tempHeader = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.96f, 0.78f, 0.42f) }
                };
                GUILayout.Label(
                    isEs ? "Notas temporales activas:" : "Active temporary notes:",
                    tempHeader);
                for (int i = 0; i < snapshot.ActiveTemporalEntries.Count; i++)
                {
                    DrawEntryCard(snapshot.ActiveTemporalEntries[i]);
                    GUILayout.Space(4f);
                }
            }
        }

        private void DrawEntryCard(JournalEntryView entry)
        {
            GUILayout.BeginVertical(entryCardStyle);

            GUILayout.BeginHorizontal();
            GUIStyle entryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = entry.AccentColor }
            };
            GUILayout.Label(entry.Title, entryTitleStyle);

            if (!string.IsNullOrEmpty(entry.StatusBadge))
            {
                GUILayout.FlexibleSpace();
                GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.86f, 0.92f, 0.98f) }
                };
                GUILayout.Label($"[{entry.StatusBadge}]", badgeStyle, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(entry.Description))
            {
                GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    normal = { textColor = new Color(0.90f, 0.93f, 0.96f) }
                };
                GUILayout.Label(entry.Description, bodyStyle);
            }

            if (!string.IsNullOrEmpty(entry.SecondaryNote))
            {
                GUIStyle noteStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    normal = { textColor = new Color(0.60f, 0.92f, 0.88f) }
                };
                GUILayout.Label(entry.SecondaryNote, noteStyle);
            }

            GUILayout.EndVertical();
        }

        private void EnsureStyles()
        {
            if (entryCardStyle == null)
            {
                entryCardTex = CreateSolidTexture(new Color(0.10f, 0.13f, 0.18f, 0.92f));
                entryCardStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = entryCardTex },
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(0, 4, 0, 0)
                };
            }
            if (warningCardStyle == null)
            {
                warningCardTex = CreateSolidTexture(new Color(0.32f, 0.20f, 0.06f, 0.94f));
                warningCardStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = warningCardTex },
                    padding = new RectOffset(10, 10, 6, 6),
                    margin = new RectOffset(0, 0, 0, 0)
                };
            }
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
