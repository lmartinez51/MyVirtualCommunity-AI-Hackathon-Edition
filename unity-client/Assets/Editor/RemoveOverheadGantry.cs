using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class RemoveOverheadGantry
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Remove Overhead Highway Gantry")]
        public static void Execute()
        {
            Debug.Log("[RemoveOverheadGantry] Opening scene to remove overhead highway gantry...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[RemoveOverheadGantry] Failed to open scene {ScenePath}");
                return;
            }

            int removedCount = 0;

            // Target main root object
            GameObject gantryRoot = GameObject.Find("Overhead_Highway_Gantry");
            if (gantryRoot != null)
            {
                Debug.Log($"[RemoveOverheadGantry] Found '{gantryRoot.name}', deleting...");
                UnityEngine.Object.DestroyImmediate(gantryRoot);
                removedCount++;
            }

            // Also check for any loose/orphan gantry parts in the scene
            string[] gantryPartNames = new string[]
            {
                "Gantry_Post_West",
                "Gantry_Post_East",
                "Gantry_Beam",
                "Sign_PasoDelNorte",
                "Sign_PedestrianElPaso"
            };

            foreach (string partName in gantryPartNames)
            {
                GameObject part = GameObject.Find(partName);
                while (part != null)
                {
                    Debug.Log($"[RemoveOverheadGantry] Found orphan part '{part.name}', deleting...");
                    UnityEngine.Object.DestroyImmediate(part);
                    removedCount++;
                    part = GameObject.Find(partName);
                }
            }

            Debug.Log($"[RemoveOverheadGantry] Total objects removed: {removedCount}");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[RemoveOverheadGantry] Scene successfully saved to '{ScenePath}'!");

            CaptureVerificationScreenshots();
        }

        public static void CaptureVerificationScreenshots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("BorderCorridorScene.unity"))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("GantryVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Approaching the bridge from mid-corridor (Z = 10m looking North)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(0f, 2.0f, 10.0f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "bridge_approach_gantry_removed.png"));

            // Shot 2: Full corridor view from South looking North (Z = -27.5m)
            cam.fieldOfView = 62f;
            cam.transform.position = new Vector3(0.0f, 3.2f, -27.5f);
            cam.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "corridor_overview_gantry_removed.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[RemoveOverheadGantry] Verification screenshots generated successfully!");
        }

        private static void RenderToFile(Camera cam, int width, int height, string filePath)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(filePath, png);
            Debug.Log($"[RemoveOverheadGantry] Saved screenshot: {filePath}");
        }
    }
}

