using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    public static class CitizenKioskVerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Verify and Capture Citizen Kiosk Visuals")]
        public static void VerifyAndCapture()
        {
            Debug.Log("[CitizenKioskVerificationSuite] Starting Kiosk Verification & Capture...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CitizenKioskVerificationSuite] Failed to open scene: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // Run the updated Citizen Service Desk baker (method is internal/public in same assembly)
            // Save scene and update geometry
            var deskMethod = typeof(BobbleheadPrefabBaker).GetMethod("UpdateCitizenServiceDesk", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (deskMethod != null)
            {
                deskMethod.Invoke(null, null);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[CitizenKioskVerificationSuite] Updated CitizenServiceDesk geometry and saved scene.");
            }

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Create temporary camera for high-quality rendering
            var camGo = new GameObject("VerificationCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Shot 1: Frontal view looking West directly at Patricia's Kiosk and Upper Sign
            cam.transform.position = new Vector3(-6.2f, 1.65f, 12.8f);
            cam.transform.rotation = Quaternion.Euler(2f, -90f, 0f); // Looking West (-X)
            CaptureRender(cam, 1920, 1080, Path.Combine(outputDir, "citizen_kiosk_frontal.png"));

            // Shot 2: Perspective view looking Northwest from South sidewalk / street
            cam.transform.position = new Vector3(-6.8f, 1.7f, 6.5f);
            cam.transform.LookAt(new Vector3(-10.8f, 1.6f, 12.8f));
            CaptureRender(cam, 1920, 1080, Path.Combine(outputDir, "citizen_kiosk_perspective.png"));

            // Shot 3: Close-up on the Upper Fascia Sign and Patricia
            cam.transform.position = new Vector3(-7.8f, 2.05f, 12.8f);
            cam.transform.rotation = Quaternion.Euler(6f, -90f, 0f); // Looking West (-X) slightly upward
            CaptureRender(cam, 1920, 1080, Path.Combine(outputDir, "citizen_kiosk_sign_closeup.png"));

            UnityEngine.Object.DestroyImmediate(camGo);

            Debug.Log("[CitizenKioskVerificationSuite] Screenshots captured successfully!");
            EditorApplication.Exit(0);
        }

        private static void CaptureRender(Camera cam, int width, int height, string filePath)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"[CitizenKioskVerificationSuite] Saved screenshot: {filePath}");
        }
    }
}
