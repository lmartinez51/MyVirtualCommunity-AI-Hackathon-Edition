using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class CleanEastPlazaElements
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Clean East Plaza Elements")]
        public static void Execute()
        {
            Debug.Log("[CleanEastPlazaElements] Opening scene to remove requested elements...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CleanEastPlazaElements] Failed to open scene {ScenePath}");
                return;
            }

            int removedCount = 0;

            // Target objects to remove:
            // 1. Two initial safety guardrails on East sidewalk approaching bridge: Plaza_Guardrail_Z29 and Plaza_Guardrail_Z33
            // 2. The first planter on East plaza: Plaza_Planter_Z31
            // 3. The small unlettered green sign hanging at the main bridge canopy: Overhead_Telepeaje_Sign
            string[] targetObjectNames = new string[]
            {
                "Plaza_Guardrail_Z29",
                "Plaza_Guardrail_Z33",
                "Plaza_Planter_Z31",
                "Overhead_Telepeaje_Sign"
            };

            foreach (string objName in targetObjectNames)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null)
                {
                    Debug.Log($"[CleanEastPlazaElements] Removing target object: {obj.name}");
                    UnityEngine.Object.DestroyImmediate(obj);
                    removedCount++;
                }
                else
                {
                    Debug.LogWarning($"[CleanEastPlazaElements] Target object '{objName}' not found or already removed.");
                }
            }

            Debug.Log($"[CleanEastPlazaElements] Successfully removed {removedCount} objects from scene.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CleanEastPlazaElements] Scene saved cleanly to '{ScenePath}'!");

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

            GameObject camGo = new GameObject("PlazaCleanupVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Approaching the East Plaza & Elena Ramos Kiosk from South (Z = 22m looking North-East)
            // Shows the open sidewalk without the first 2 guardrails and the 3 remaining planters (first planter removed)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(8.5f, 1.9f, 22.0f);
            cam.transform.rotation = Quaternion.Euler(-1.5f, 18f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "east_plaza_guardrails_planter_removed.png"));

            // Shot 2: Bridge Canopy direct facade view from roadway (Z = 28m looking North)
            // Shows all 4 arches with NO small green sign hanging under Arch 2
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(0f, 2.0f, 28.0f);
            cam.transform.rotation = Quaternion.Euler(3.0f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "bridge_entrance_green_sign_removed.png"));

            // Shot 3: Panoramic view from south corridor (Z = -15m looking North)
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(0.0f, 2.8f, -15.0f);
            cam.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "corridor_approach_full_cleaned.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[CleanEastPlazaElements] Verification screenshots generated successfully!");
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
            Debug.Log($"[CleanEastPlazaElements] Saved screenshot: {filePath}");
        }
    }
}

