using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class SceneRestorationVerifier
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Verify Corridor Restoration and Capture Views")]
        public static void VerifyAndCaptureAll()
        {
            Debug.Log("================================================================================");
            Debug.Log("[SceneRestorationVerifier] STARTING SCENE RESTORATION VERIFICATION");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[SceneRestorationVerifier] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // 1. Verify FirstPersonPlayer and PlayerCamera
            GameObject player = GameObject.Find("FirstPersonPlayer");
            if (player == null)
            {
                Debug.LogError("[SceneRestorationVerifier] FirstPersonPlayer NOT FOUND!");
                EditorApplication.Exit(1);
                return;
            }

            GameObject camGo = GameObject.Find("PlayerCamera");
            if (camGo == null)
            {
                Debug.LogError("[SceneRestorationVerifier] PlayerCamera NOT FOUND!");
                EditorApplication.Exit(1);
                return;
            }

            // Force localPosition to (0, 1.60, 0) and rotation to identity
            camGo.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            camGo.transform.localRotation = Quaternion.identity;

            Camera cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;

            Debug.Log($"[SceneRestorationVerifier] Player at {player.transform.position}, Camera local pos: {camGo.transform.localPosition}");

            // 2. Verify all major corridor landmarks exist and are active
            string[] requiredObjects = new string[]
            {
                "Road_AvenidaJuarez",
                "Sidewalk_East",
                "Sidewalk_West",
                "Storefront_FarmaciasBenavides",
                "CarlosMendez_Attendant",
                "Storefront_OpticaFashion",
                "ValeriaDominguez_Attendant",
                "NorthBlock_MainMass", // Curiosidades Azteca / Minerva block
                "Curiosidades_Awning",
                "Michoacana_MainMass",
                "BuildingB_CucarachaMass", // Bar La Cucaracha / Taqueria El Trompo
                "BuildingC_HistoricUpperMass", // Dental Jarquín
                "Calle_Gardenias_Root",
                "Corner_SE",
                "Corner_SO_Organic_Root",
                "Corner_NE",
                "Corner_NO_Organic_Root",
                "StreetSign_Gardenias_Juarez",
                "CitizenServiceDesk",
                "PatriciaMorales_Attendant",
                "Border_Crossing_Plaza",
                "Bridge_Canopy_PlazaDeCobro",
                "Arch1_TollBarrier_Unit",
                "Arch2_TollBarrier_Unit",
                "Arch3_TollBarrier_Unit",
                "Arch4_TollBarrier_Unit",
                "Bridge_Fideicomiso_Camera_VistaSur",
                "Border_Information_Kiosk",
                "ElenaRamos_Attendant",
                "Arch4_Diagonal_Closure_Barriers"
            };

            foreach (var req in requiredObjects)
            {
                var go = GameObject.Find(req);
                if (go == null)
                {
                    Debug.LogError($"[SceneRestorationVerifier] MISSING REQUIRED OBJECT: '{req}'");
                    EditorApplication.Exit(1);
                    return;
                }
                if (!go.activeInHierarchy)
                {
                    Debug.LogError($"[SceneRestorationVerifier] INACTIVE REQUIRED OBJECT: '{req}'");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log($"[PASS] Found active object: '{req}' at {go.transform.position}");
            }

            // Save clean scene with anchored camera
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneRestorationVerifier] Scene saved cleanly to '{ScenePath}'.");

            // 3. Capture verification screenshots
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string artifactsDir = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            string screenshotsDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(screenshotsDir)) Directory.CreateDirectory(screenshotsDir);

            // View 1: Player Start View (X=0, Y=1.65, Z=-15) looking North down Avenida Benito Juárez
            cam.transform.position = new Vector3(0f, 1.65f, -15f);
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            CaptureAndCopy(cam, screenshotsDir, artifactsDir, "restored_01_player_start_avenida_juarez.png",
                "MyVirtualCommunity — Player Spawn: Full Avenida Juárez Corridor Restored");

            // View 2: East Sidewalk View (X=8.5, Y=1.65, Z=-5) looking at Farmacias Benavides, Curiosidades Azteca, La Michoacana
            cam.transform.position = new Vector3(8.5f, 1.65f, -8f);
            cam.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            CaptureAndCopy(cam, screenshotsDir, artifactsDir, "restored_02_east_sidewalk_storefronts.png",
                "MyVirtualCommunity — East Sidewalk: Farmacias Benavides, Curiosidades Azteca, La Michoacana");

            // View 3: West Sidewalk View (X=-6.0, Y=1.65, Z=-5) looking at Bar La Cucaracha, Taquería El Trompo, Óptica Fashion
            cam.transform.position = new Vector3(-6.0f, 1.65f, -8f);
            cam.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            CaptureAndCopy(cam, screenshotsDir, artifactsDir, "restored_03_west_sidewalk_storefronts.png",
                "MyVirtualCommunity — West Sidewalk: Bar La Cucaracha, Taqueria El Trompo, Optica Fashion");

            // View 4: Calle Gardenias Intersection (X=0, Y=1.65, Z=18) looking North-West across the crosswalks and rounded corners
            cam.transform.position = new Vector3(0f, 1.65f, 18f);
            cam.transform.rotation = Quaternion.Euler(5f, -20f, 0f);
            CaptureAndCopy(cam, screenshotsDir, artifactsDir, "restored_04_calle_gardenias_intersection.png",
                "MyVirtualCommunity — Calle Gardenias Intersection: 4 Corners, Crosswalks, Taxi Stalls");

            // View 5: Bridge Entrance approaching toll booths (X=0, Y=1.65, Z=30) showing 4 plumas, blue numbers, camera, diagonal barricades
            cam.transform.position = new Vector3(0f, 1.65f, 30f);
            cam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);
            CaptureAndCopy(cam, screenshotsDir, artifactsDir, "restored_05_bridge_entrance_plumas_and_camera.png",
                "MyVirtualCommunity — Bridge Entrance: 4 Plumas, Blue Numbers 1-4, Camera, Diagonal Barricades");

            // Restore camera to exact player local head anchor (1.60m) before final exit
            camGo.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            camGo.transform.localRotation = Quaternion.identity;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("================================================================================");
            Debug.Log("[SceneRestorationVerifier] ALL 5 RESTORATION VERIFICATION VIEWS CAPTURED!");
            Debug.Log("================================================================================");
            EditorApplication.Exit(0);
        }

        private static void CaptureAndCopy(Camera cam, string outDir, string artifactsDir, string fileName, string title)
        {
            int renderH = WinHeight - TitleBarHeight;
            var rt = RenderTexture.GetTemporary(WinWidth, renderH, 24, RenderTextureFormat.ARGB32);
            var prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevRT;

            var tex = new Texture2D(WinWidth, renderH, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, WinWidth, renderH), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string filePath = Path.Combine(outDir, fileName);
            using (var ms = new MemoryStream(pngBytes))
            using (var sceneImg = new Bitmap(ms))
            using (var finalBmp = new Bitmap(WinWidth, WinHeight, PixelFormat.Format32bppArgb))
            using (var g = System.Drawing.Graphics.FromImage(finalBmp))
            {
                g.Clear(System.Drawing.Color.FromArgb(28, 30, 36));
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(45, 48, 56)))
                {
                    g.FillRectangle(brush, 0, 0, WinWidth, TitleBarHeight);
                }
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 74, 85)))
                {
                    g.DrawLine(pen, 0, TitleBarHeight - 1, WinWidth, TitleBarHeight - 1);
                }
                using (var brushClose = new SolidBrush(System.Drawing.Color.FromArgb(232, 17, 35)))
                using (var brushState = new SolidBrush(System.Drawing.Color.FromArgb(120, 120, 120)))
                {
                    g.FillEllipse(brushClose, 12, 10, 12, 12);
                    g.FillEllipse(brushState, 32, 10, 12, 12);
                    g.FillEllipse(brushState, 52, 10, 12, 12);
                }
                using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(210, 215, 225)))
                {
                    var rect = new RectangleF(75, 0, WinWidth - 150, TitleBarHeight);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(title, font, brush, rect, sf);
                }
                g.DrawImage(sceneImg, 0, TitleBarHeight, WinWidth, renderH);
                finalBmp.Save(filePath, ImageFormat.Png);
            }

            Debug.Log($"[SceneRestorationVerifier] Captured: {filePath}");

            // Copy to artifacts dir
            string artifactPath = Path.Combine(artifactsDir, fileName);
            try
            {
                File.Copy(filePath, artifactPath, true);
                Debug.Log($"[SceneRestorationVerifier] Copied to artifact: {artifactPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneRestorationVerifier] Could not copy to artifact: {ex.Message}");
            }
        }
    }
}
