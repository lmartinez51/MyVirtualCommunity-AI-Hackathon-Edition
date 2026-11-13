using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class CorridorForensicAudit
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Run Corridor Forensic Audit")]
        public static void RunAudit()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CorridorForensicAudit] Failed to open scene {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("CORRIDOR FORENSIC GEOMETRY AUDIT — BORDERCORRIDORSCENE");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            sb.AppendLine($"Total renderers found in scene: {renderers.Length}");
            sb.AppendLine();

            // Filter relevant corridor objects (X: 6 to 22, Z: 30 to 75, or with specific bridge/corridor keywords)
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("1. FORENSIC INVENTORY TABLE OF RELEVANT OBJECTS (X: [6, 22], Z: [30, 75])");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("{0,-45} | {1,-10} | {2,-28} | {3,-28} | {4,-8} | {5,-20}", 
                "GameObject Name", "Active", "World Min (X, Y, Z)", "World Max (X, Y, Z)", "Surf Y", "Collider Type"));
            sb.AppendLine(new string('-', 155));

            Array.Sort(renderers, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name, StringComparison.OrdinalIgnoreCase));

            foreach (var r in renderers)
            {
                var go = r.gameObject;
                var b = r.bounds;

                bool inZone = (b.center.x >= 5.0f && b.center.x <= 25.0f && b.center.z >= 25.0f && b.center.z <= 75.0f);
                bool nameMatch = go.name.IndexOf("Walkway", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Sidewalk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Roadway", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Bridge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Ramp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Rail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Curb", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Plaza", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("Deck", StringComparison.OrdinalIgnoreCase) >= 0;

                if (inZone || nameMatch)
                {
                    var col = go.GetComponent<Collider>();
                    string colType = col == null ? "None" : col.GetType().Name + (col.isTrigger ? " (Trig)" : "");
                    string minStr = $"({b.min.x:F2}, {b.min.y:F2}, {b.min.z:F2})";
                    string maxStr = $"({b.max.x:F2}, {b.max.y:F2}, {b.max.z:F2})";

                    sb.AppendLine(string.Format("{0,-45} | {1,-10} | {2,-28} | {3,-28} | {4,8:F3} | {5,-20}",
                        go.name, go.activeInHierarchy ? "Active" : "Inactive", minStr, maxStr, b.max.y, colType));
                }
            }

            sb.AppendLine();
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("2. HIERARCHICAL ANALYSIS & DETAILED COMPONENT BREAKDOWN");
            sb.AppendLine("--------------------------------------------------------------------------------");

            string[] keyRoots = new string[] {
                "Bridge_Span_Deck",
                "Roadway_Bridge_Approach_East",
                "Sidewalk_East_North_Section",
                "Border_Crossing_Plaza",
                "East_Walkway_Phase1_Root",
                "Pedestrian_Queue_Area"
            };

            foreach (var rootName in keyRoots)
            {
                var rootGo = GameObject.Find(rootName);
                if (rootGo == null)
                {
                    sb.AppendLine($"[NOT FOUND] Root: {rootName}");
                    continue;
                }

                sb.AppendLine($"=== ROOT: {rootName} (Active: {rootGo.activeInHierarchy}) ===");
                DumpGameObjectRecursive(rootGo, sb, 0);
                sb.AppendLine();
            }

            string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "corridor_forensic_audit_report.txt"));
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[CorridorForensicAudit] Audit complete. Report written to {outPath}");

            string brainReportPath = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8/corridor_forensic_audit_report.txt";
            try
            {
                File.WriteAllText(brainReportPath, sb.ToString());
            }
            catch { }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void DumpGameObjectRecursive(GameObject go, StringBuilder sb, int indent)
        {
            string pad = new string(' ', indent * 2);
            var t = go.transform;
            var r = go.GetComponent<Renderer>();
            var col = go.GetComponent<Collider>();

            string rInfo = "NoRenderer";
            if (r != null)
            {
                var b = r.bounds;
                string matName = r.sharedMaterial != null ? r.sharedMaterial.name : "null";
                rInfo = $"Bounds: Min({b.min.x:F2}, {b.min.y:F2}, {b.min.z:F2}) Max({b.max.x:F2}, {b.max.y:F2}, {b.max.z:F2}) Size({b.size.x:F2}, {b.size.y:F2}, {b.size.z:F2}) SurfY={b.max.y:F3} Mat={matName}";
            }

            string colInfo = col != null ? $"Col={col.GetType().Name}" : "NoCol";

            sb.AppendLine($"{pad}* {go.name} [Pos: ({t.position.x:F2}, {t.position.y:F2}, {t.position.z:F2}), Scale: ({t.lossyScale.x:F2}, {t.lossyScale.y:F2}, {t.lossyScale.z:F2})] | {rInfo} | {colInfo}");

            for (int i = 0; i < t.childCount; i++)
            {
                DumpGameObjectRecursive(t.GetChild(i).gameObject, sb, indent + 1);
            }
        }
    }
}

