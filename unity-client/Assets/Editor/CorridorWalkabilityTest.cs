using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class CorridorWalkabilityTest
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Test Corridor Walkability")]
        public static void RunWalkabilityTest()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WalkabilityTest] Failed to load {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("CORRIDOR WALKABILITY & COLLISION TEST REPORT");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            bool allPassed = true;

            // Test Path 1: Main Pedestrian Concourse (X = 15.60m, Z: 35m to 75m, step = 0.5m)
            sb.AppendLine("--- TEST PATH 1: MAIN PEDESTRIAN CONCOURSE (X = 15.60m, South -> North) ---");
            allPassed &= TestCorridorPath(15.60f, 35.0f, 75.0f, 0.5f, sb);

            // Test Path 1 Reverse: Main Concourse (North -> South)
            sb.AppendLine("\n--- TEST PATH 1R: MAIN PEDESTRIAN CONCOURSE (X = 15.60m, North -> South) ---");
            allPassed &= TestCorridorPath(15.60f, 75.0f, 35.0f, -0.5f, sb);

            // Test Path 2: Blue Preferential Accessible Ramp (North Bridge Sidewalk -> South Exit to Street)
            // Ramp boards from bridge at Z = 62.0m, descends at 3.33% slope to Z = 54.0m, and exits smoothly to street asphalt
            sb.AppendLine("\n--- TEST PATH 2: ACCESSIBLE BLUE RAMP EGRESS (North -> South -> Street Exit) ---");
            allPassed &= TestAccessibleRoute(sb);

            // Test Path 3: Street Exit Level Roll-out (X = 14.44m down ramp to X = 13.50m street asphalt at Z = 54.5m, clearing wall cap)
            sb.AppendLine("\n--- TEST PATH 3: STREET EXIT ROLL-OUT LEVEL JUNCTION (Z = 54.5m, Ramp -> Roadway Asphalt) ---");
            allPassed &= TestLateralExit(14.44f, 13.50f, 54.5f, sb);

            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"OVERALL WALKABILITY VERDICT: {(allPassed ? "PASSED (100% CONTINUOUS)" : "FAILED (DISCONTINUITIES DETECTED)")}");
            sb.AppendLine("================================================================================");

            string report = sb.ToString();
            Debug.Log(report);

            string outPath = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8/walkability_test_report.txt";
            File.WriteAllText(outPath, report);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(allPassed ? 0 : 2);
            }
        }

        private static bool TestCorridorPath(float x, float zStart, float zEnd, float zStep, StringBuilder sb)
        {
            bool pathOk = true;
            float prevY = float.NaN;
            int count = Mathf.RoundToInt(Mathf.Abs(zEnd - zStart) / Mathf.Abs(zStep)) + 1;

            float maxStep = 0f;
            int stepViolations = 0;
            int noHitCount = 0;

            for (int i = 0; i < count; i++)
            {
                float z = zStart + i * zStep;
                // Cast from 1.50m (pedestrian waist height, below overhead 3.2m canopy roofs)
                Vector3 rayOrigin = new Vector3(x, 1.50f, z);
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 3.0f))
                {
                    float y = hit.point.y;
                    if (!float.IsNaN(prevY))
                    {
                        float deltaY = Mathf.Abs(y - prevY);
                        if (deltaY > maxStep) maxStep = deltaY;

                        // More than 5cm step in 50cm horizontal distance indicates an accidental obstruction
                        if (deltaY > 0.05f)
                        {
                            sb.AppendLine($"  [WARNING] Step at Z={z:F2}m: DeltaY={deltaY * 100f:F1}cm (Y={prevY:F3} -> {y:F3}) on {hit.collider.gameObject.name}");
                            stepViolations++;
                            pathOk = false;
                        }
                    }
                    prevY = y;
                }
                else
                {
                    sb.AppendLine($"  [NO HIT / HOLE] Raycast missed floor at X={x:F2}m, Z={z:F2}m!");
                    noHitCount++;
                    pathOk = false;
                }
            }

            sb.AppendLine($"  Result: Samples={count}, MaxStepIn0.5m={maxStep * 100f:F1}cm, Violations={stepViolations}, Holes={noHitCount} => {(pathOk ? "OK" : "FAILED")}");
            return pathOk;
        }

        private static bool TestAccessibleRoute(StringBuilder sb)
        {
            bool pathOk = true;
            float prevY = float.NaN;
            float maxStep = 0f;
            int stepViolations = 0;
            int noHitCount = 0;

            // Route from bridge plateau (Z = 62.0m) down blue ramp to street exit at Z = 54.0m
            for (float z = 62.0f; z >= 54.0f; z -= 0.5f)
            {
                Vector3 rayOrigin = new Vector3(14.44f, 1.50f, z);
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 3.0f))
                {
                    float y = hit.point.y;
                    if (!float.IsNaN(prevY))
                    {
                        float deltaY = Mathf.Abs(y - prevY);
                        if (deltaY > maxStep) maxStep = deltaY;
                        if (deltaY > 0.05f)
                        {
                            sb.AppendLine($"  [WARNING] Step at Z={z:F2}m: DeltaY={deltaY * 100f:F1}cm (Y={prevY:F3} -> {y:F3}) on {hit.collider.gameObject.name}");
                            stepViolations++;
                            pathOk = false;
                        }
                    }
                    prevY = y;
                }
                else
                {
                    sb.AppendLine($"  [NO HIT] Raycast missed at X=14.44m, Z={z:F2}m");
                    noHitCount++;
                    pathOk = false;
                }
            }

            sb.AppendLine($"  Result: Samples=17, MaxStepIn0.5m={maxStep * 100f:F1}cm, Violations={stepViolations}, Holes={noHitCount} => {(pathOk ? "OK" : "FAILED")}");
            return pathOk;
        }

        private static bool TestLateralExit(float xStart, float xEnd, float z, StringBuilder sb)
        {
            bool pathOk = true;
            float prevY = float.NaN;
            float maxStep = 0f;
            int stepViolations = 0;
            int noHitCount = 0;

            // Roll-out across X from blue ramp (X = 14.44m) to roadway asphalt (X = 13.50m) at Z = 54.0m
            float step = -0.1f;
            int count = (int)Mathf.Abs((xEnd - xStart) / step) + 1;

            for (int i = 0; i < count; i++)
            {
                float x = xStart + i * step;
                Vector3 rayOrigin = new Vector3(x, 1.50f, z);
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 3.0f))
                {
                    float y = hit.point.y;
                    if (!float.IsNaN(prevY))
                    {
                        float deltaY = Mathf.Abs(y - prevY);
                        if (deltaY > maxStep) maxStep = deltaY;
                        if (deltaY > 0.03f)
                        {
                            sb.AppendLine($"  [WARNING] Lateral Step at X={x:F2}m, Z={z:F2}m: DeltaY={deltaY * 100f:F1}cm (Y={prevY:F3} -> {y:F3}) on {hit.collider.gameObject.name}");
                            stepViolations++;
                            pathOk = false;
                        }
                    }
                    prevY = y;
                }
                else
                {
                    sb.AppendLine($"  [NO HIT] Lateral raycast missed at X={x:F2}m, Z={z:F2}m");
                    noHitCount++;
                    pathOk = false;
                }
            }

            sb.AppendLine($"  Result: Samples={count}, MaxStepIn0.1m={maxStep * 100f:F1}cm, Violations={stepViolations}, Holes={noHitCount} => {(pathOk ? "OK (Seamless Roll-Out)" : "FAILED")}");
            return pathOk;
        }
    }
}
