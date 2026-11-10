using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    public static class TollBarriersVisualVerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public static void VerifyAndCapture()
        {
            Debug.Log("================================================================================");
            Debug.Log("[TollBarriersVisualVerificationSuite] STARTING TOLL BARRIERS VERIFICATION");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[TollBarriersVisualVerificationSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string artifactsDir = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            string screenshotsDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(screenshotsDir)) Directory.CreateDirectory(screenshotsDir);

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camGo = new GameObject("Verification_Cam");
                cam = camGo.AddComponent<Camera>();
            }

            TollBarrierController controller = UnityEngine.Object.FindAnyObjectByType<TollBarrierController>();
            if (controller == null)
            {
                Debug.LogError("[TollBarriersVisualVerificationSuite] TollBarrierController not found in scene!");
                EditorApplication.Exit(1);
                return;
            }

            // Verify all 4 barrier pivots and units exist
            for (int i = 1; i <= 4; i++)
            {
                var unit = GameObject.Find($"Arch{i}_TollBarrier_Unit");
                var pivot = GameObject.Find($"Arch{i}_TollBarrier_Arm_Pivot");
                if (unit == null || pivot == null)
                {
                    Debug.LogError($"[TollBarriersVisualVerificationSuite] Missing Arch {i} toll barrier components!");
                    EditorApplication.Exit(1);
                    return;
                }
                var col = pivot.GetComponent<BoxCollider>();
                if (col == null)
                {
                    Debug.LogError($"[TollBarriersVisualVerificationSuite] Missing BoxCollider on Arch {i} pivot!");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log($"[PASS] Arch {i} barrier unit, pivot, and BoxCollider validated.");
            }

            // -------------------------------------------------------------------------
            // CAPTURE 1: All 4 Barriers in Default Open (85 deg) Upright Position
            // View from roadway approach looking North toward the bridge canopy
            // -------------------------------------------------------------------------
            for (int i = 1; i <= 4; i++)
            {
                var pivot = GameObject.Find($"Arch{i}_TollBarrier_Arm_Pivot");
                if (pivot != null) pivot.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
            }

            cam.transform.position = new Vector3(3.50f, 1.70f, 32.50f);
            cam.transform.rotation = Quaternion.Euler(6.0f, 0f, 0f);

            string cap1Path = Path.Combine(screenshotsDir, "bridge_11_toll_barriers_all_open_default.png");
            CaptureCameraFrame(cam, cap1Path, 1920, 1080);
            CopyFile(cap1Path, Path.Combine(artifactsDir, "bridge_11_toll_barriers_all_open_default.png"));
            Debug.Log($"[PASS] Capture 1 saved: {cap1Path}");

            // -------------------------------------------------------------------------
            // CAPTURE 2: Close-up of Arch 1 & Arch 2 (Cabinet, stripes, blue number 1)
            // -------------------------------------------------------------------------
            cam.transform.position = new Vector3(-1.80f, 1.45f, 38.50f);
            cam.transform.rotation = Quaternion.Euler(8.0f, -12.0f, 0f);

            string cap2Path = Path.Combine(screenshotsDir, "bridge_12_toll_barrier_arch1_arch2_closeup.png");
            CaptureCameraFrame(cam, cap2Path, 1920, 1080);
            CopyFile(cap2Path, Path.Combine(artifactsDir, "bridge_12_toll_barrier_arch1_arch2_closeup.png"));
            Debug.Log($"[PASS] Capture 2 saved: {cap2Path}");

            // -------------------------------------------------------------------------
            // CAPTURE 3: Closed State Demonstration (Arch 1 & Arch 4 Lowered to 0 deg horizontal)
            // -------------------------------------------------------------------------
            var p1 = GameObject.Find("Arch1_TollBarrier_Arm_Pivot");
            if (p1 != null) p1.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // Closed!

            var p4 = GameObject.Find("Arch4_TollBarrier_Arm_Pivot");
            if (p4 != null) p4.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // Closed!

            // Toggle signal lights for Arch 1 to red
            var s1Green = GameObject.Find("Arch1_Signal_Housing")?.transform.Find("Signal_Green_Aspect");
            var s1Red = GameObject.Find("Arch1_Signal_Housing")?.transform.Find("Signal_Red_Aspect");
            if (s1Green != null) s1Green.gameObject.SetActive(false);
            if (s1Red != null) s1Red.gameObject.SetActive(true);

            cam.transform.position = new Vector3(1.50f, 1.65f, 35.00f);
            cam.transform.rotation = Quaternion.Euler(4.0f, -5.0f, 0f);

            string cap3Path = Path.Combine(screenshotsDir, "bridge_13_toll_barriers_closed_state_demonstration.png");
            CaptureCameraFrame(cam, cap3Path, 1920, 1080);
            CopyFile(cap3Path, Path.Combine(artifactsDir, "bridge_13_toll_barriers_closed_state_demonstration.png"));
            Debug.Log($"[PASS] Capture 3 saved: {cap3Path}");

            // Restore Arch 1 and Arch 4 back to default open before exiting
            if (p1 != null) p1.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
            if (p4 != null) p4.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
            if (s1Green != null) s1Green.gameObject.SetActive(true);

            // Restore PlayerCamera back to standing eye-level height (1.60m)
            if (cam.name == "PlayerCamera" || cam.transform.parent != null)
            {
                cam.transform.localPosition = new Vector3(0f, 1.60f, 0f);
                cam.transform.localRotation = Quaternion.identity;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("================================================================================");
            Debug.Log("[TollBarriersVisualVerificationSuite] VERIFICATION COMPLETE: ALL CHECKS PASSED!");
            Debug.Log("================================================================================");
            EditorApplication.Exit(0);
        }

        private static void CopyFile(string src, string dst)
        {
            try
            {
                if (File.Exists(src)) File.Copy(src, dst, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not copy {src} to {dst}: {ex.Message}");
            }
        }

        private static void CaptureCameraFrame(Camera cam, string outputPath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = prev;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}

