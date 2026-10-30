using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated builder for Windows x86_64 Standalone Release Candidate.
    /// Produces a clean executable in unity-client/Builds/Windows/MyVirtualCommunity.exe
    /// containing BorderCorridorScene (Index 0) and HandshakeScene (Index 1).
    /// </summary>
    public static class WindowsStandaloneBuilder
    {
        private const string BuildOutputFolder = "Builds/Windows";
        private const string ExecutableName = "MyVirtualCommunity.exe";

        [MenuItem("MyVirtualCommunity/Build Windows Standalone")]
        public static void BuildWindowsExecutable()
        {
            Debug.Log("================================================================================");
            Debug.Log("[WindowsStandaloneBuilder] STARTING WINDOWS STANDALONE 64-BIT BUILD");
            Debug.Log("================================================================================");

            string[] scenes = new string[]
            {
                "Assets/Scenes/BorderCorridorScene.unity",
                "Assets/Scenes/HandshakeScene.unity"
            };

            string destinationDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildOutputFolder));
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            string destinationPath = Path.Combine(destinationDir, ExecutableName);
            Debug.Log($"Target executable path: {destinationPath}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = destinationPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("================================================================================");
                Debug.Log($"[WindowsStandaloneBuilder] BUILD SUCCEEDED!");
                Debug.Log($"Total Build Duration: {summary.totalTime.TotalSeconds:F1} seconds");
                Debug.Log($"Total Build Size:     {summary.totalSize / (1024 * 1024)} MB");
                Debug.Log($"Output Location:      {destinationPath}");
                Debug.Log("================================================================================");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("================================================================================");
                Debug.LogError($"[WindowsStandaloneBuilder] BUILD FAILED: {summary.result}");
                Debug.LogError($"Total Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}");
                Debug.LogError("================================================================================");
                EditorApplication.Exit(1);
            }
        }
    }
}

