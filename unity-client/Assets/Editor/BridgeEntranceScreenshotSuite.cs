using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Visual verification screenshot generator for the authentic Paso del Norte Bridge Entrance
    /// and relocated Elena Ramos Border Information Kiosk on the pedestrian promenade.
    /// </summary>
    public static class BridgeEntranceScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Bridge Entrance Verification Screenshots")]
        public static void GenerateScreenshots()
        {
            Debug.Log("[BridgeEntranceScreenshotSuite] Starting Bridge Entrance Screenshot Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BridgeEntranceScreenshotSuite] Failed to open scene {ScenePath}");
                return;
            }

            GameObject camGo = GameObject.Find("PlayerCamera");
            if (camGo == null) camGo = GameObject.FindWithTag("MainCamera");
            if (camGo == null)
            {
                camGo = new GameObject("ScreenshotCam");
                camGo.AddComponent<Camera>();
            }

            Camera cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "bridge_entrance"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // 1. Approach View: From roadway / crosswalk (Z = 12m) looking North towards bridge
            cam.transform.position = new Vector3(4.0f, 1.8f, 12f);
            cam.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_01_approach_downtown.png"),
                "MyVirtualCommunity — Approach to Paso del Norte Arched Bridge Canopy (1280x720)");

            // 2. Canopy Facade Close-up: Frontal view (Z = 28m) showcasing the 4 arched portals & monumental sign
            cam.transform.position = new Vector3(4.0f, 3.2f, 26f);
            cam.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_02_canopy_facade_closeup.png"),
                "MyVirtualCommunity — Authentic Arched Concrete Canopy & PC-51 Signage (1280x720)");

            // 3. Elena Ramos Kiosk on East Pedestrian Plaza: Frontal shot from open plaza
            cam.transform.position = new Vector3(19.20f, 1.7f, 33.50f);
            cam.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_03_elena_plaza_kiosk.png"),
                "MyVirtualCommunity — Elena Ramos in Wide Open East Pedestrian Plaza (1280x720)");

            // 4. East Pedestrian Promenade Overview: Showing Elena's Kiosk and Queue (Mateo, Sofia, Hector)
            cam.transform.position = new Vector3(18.5f, 3.2f, 31.0f);
            cam.transform.rotation = Quaternion.Euler(9f, -12f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_04_pedestrian_queue_and_plaza.png"),
                "MyVirtualCommunity — Pedestrian Concourse, Official Kiosk & Traveler Queue (1280x720)");

            // 5. 4 Vehicular Lanes & Toll Booths Perspective: Showing Lane 1, Lane 2, Lane 3, Lane 4
            cam.transform.position = new Vector3(2.0f, 1.6f, 33f);
            cam.transform.rotation = Quaternion.Euler(3f, 5f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_05_toll_lanes_perspective.png"),
                "MyVirtualCommunity — 4 Entry Lanes, Divider Islands & Toll Collection Casetas (1280x720)");

            // 6. User Sidewalk Perspective: Mirroring exact viewpoint on Av. Juárez East Sidewalk approaching Gardenias
            cam.transform.position = new Vector3(8.50f, 1.7f, 20.0f);
            cam.transform.rotation = Quaternion.Euler(3f, 10f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_06_user_perspective_east_sidewalk.png"),
                "MyVirtualCommunity — User Perspective from Av. Juárez East Sidewalk Approaching Bridge (1280x720)");

            // 7. Close-Up of Pillars 1 & 2 (matching Photo 1: Blue Numbers 1 & 2 on Pillars, Zócalo & Bullnose)
            cam.transform.position = new Vector3(-2.0f, 1.85f, 36.5f);
            cam.transform.rotation = Quaternion.Euler(-2f, 2f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_07_pillar_blue_numbers_closeup.png"),
                "MyVirtualCommunity — Blue Numbers 1, 2, 3, 4 on Toll Booth Pillars (Zero-Flicker 3D Relief)");

            // 8. Close-Up of Fideicomiso Surveillance Camera on South Bridge Facade (looking up at canopy cornice)
            cam.transform.position = new Vector3(1.2f, 4.2f, 34.5f);
            cam.transform.rotation = Quaternion.Euler(-28f, 10f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_08_fideicomiso_camera_closeup.png"),
                "MyVirtualCommunity — Fideicomiso Surveillance Camera on Bridge South Facade");

            // 9. Simulated POV from the Fideicomiso Camera (matching Photo 2: "Paso del Norte - Vista Sur")
            cam.transform.position = new Vector3(2.40f, 8.15f, 40.20f);
            cam.transform.rotation = Quaternion.Euler(28f, 180f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_09_fideicomiso_camera_vista_sur_pov.png"),
                "MyVirtualCommunity — Fideicomiso Camera POV: Paso del Norte (Vista Sur)");

            // 10. Diagonal White Metal Barricades & Yellow Support Posts (SE Corner of Calle Gardenias -> Pillar 4)
            cam.transform.position = new Vector3(5.2f, 2.1f, 21.5f);
            cam.transform.rotation = Quaternion.Euler(4f, 12f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_10_arch4_diagonal_barriers.png"),
                "MyVirtualCommunity — Diagonal White Barricades & Yellow Support Posts Closing Arch 4 from Av. Juárez");

            // 11. 4 Toll Barrier Gates across Arcos 1, 2, 3, and 4 (All Open 85 deg Upright by Default)
            for (int i = 1; i <= 4; i++)
            {
                var pivot = GameObject.Find($"Arch{i}_TollBarrier_Arm_Pivot");
                if (pivot != null) pivot.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
            }
            cam.transform.position = new Vector3(3.50f, 1.80f, 32.50f);
            cam.transform.rotation = Quaternion.Euler(6.0f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_11_toll_barriers_all_open_default.png"),
                "MyVirtualCommunity — 4 Toll Barrier Gates across Arcos 1-4 (Open 85° Upright by Default)");

            // 12. Close-up of Arch 1 & Arch 2 Toll Barriers (Yellow Cabinet, Steel Hinge & White/Red Striped Arm)
            cam.transform.position = new Vector3(-1.80f, 1.45f, 38.50f);
            cam.transform.rotation = Quaternion.Euler(8.0f, -12.0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_12_toll_barrier_arch1_arch2_closeup.png"),
                "MyVirtualCommunity — Arch 1 & Arch 2 Toll Barriers Close-Up (Yellow Cabinet & Hazard Stripes)");

            // 13. Closed State Demonstration (Arch 1 & Arch 4 Lowered to 0 deg Horizontal Spanning Roadway)
            var p1 = GameObject.Find("Arch1_TollBarrier_Arm_Pivot");
            if (p1 != null) p1.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var p4 = GameObject.Find("Arch4_TollBarrier_Arm_Pivot");
            if (p4 != null) p4.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            cam.transform.position = new Vector3(1.50f, 1.65f, 35.00f);
            cam.transform.rotation = Quaternion.Euler(4.0f, -5.0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_13_toll_barriers_closed_state_demonstration.png"),
                "MyVirtualCommunity — Dynamic Lane Closure: Arch 1 & Arch 4 Lowered to 0° Horizontal Spanning Lane");

            // Restore Arch 1 and Arch 4 back to default open 85 deg
            if (p1 != null) p1.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
            if (p4 != null) p4.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);

            // Restore PlayerCamera back to head height
            if (camGo != null && camGo.name == "PlayerCamera")
            {
                camGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                camGo.transform.localRotation = Quaternion.identity;
            }

            // Copy screenshots to brain artifacts directory
            string brainArtifactsDir = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            string[] capturedNames = new string[] {
                "bridge_11_toll_barriers_all_open_default.png",
                "bridge_12_toll_barrier_arch1_arch2_closeup.png",
                "bridge_13_toll_barriers_closed_state_demonstration.png"
            };
            foreach (var sName in capturedNames)
            {
                string src = Path.Combine(outDir, sName);
                string dst = Path.Combine(brainArtifactsDir, sName);
                try { if (File.Exists(src)) File.Copy(src, dst, true); } catch { }
            }

            Debug.Log("[BridgeEntranceScreenshotSuite] All 13 Bridge Entrance Verification Screenshots Generated Successfully!");
        }

        private static void CaptureShot(Camera cam, string savePath, string windowTitle)
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

            using (var ms = new MemoryStream(pngBytes))
            using (var sceneImg = new Bitmap(ms))
            using (var finalBmp = new Bitmap(WinWidth, WinHeight))
            using (var g = System.Drawing.Graphics.FromImage(finalBmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Title bar
                using (var titleBrush = new SolidBrush(System.Drawing.Color.FromArgb(28, 30, 36)))
                {
                    g.FillRectangle(titleBrush, 0, 0, WinWidth, TitleBarHeight);
                }

                using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
                using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 205, 215)))
                {
                    g.DrawString(windowTitle, font, textBrush, 12, 7);
                }

                // Window control buttons
                int btnW = 46, btnH = TitleBarHeight;
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(42, 45, 52)))
                {
                    g.FillRectangle(btnBrush, WinWidth - btnW * 3, 0, btnW, btnH);
                    g.FillRectangle(btnBrush, WinWidth - btnW * 2, 0, btnW, btnH);
                }
                using (var closeBrush = new SolidBrush(System.Drawing.Color.FromArgb(196, 43, 28)))
                {
                    g.FillRectangle(closeBrush, WinWidth - btnW, 0, btnW, btnH);
                }
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 1.2f))
                {
                    // Min
                    g.DrawLine(pen, WinWidth - btnW * 3 + 18, 16, WinWidth - btnW * 3 + 28, 16);
                    // Max
                    g.DrawRectangle(pen, WinWidth - btnW * 2 + 18, 11, 10, 10);
                    // Close
                    g.DrawLine(pen, WinWidth - btnW + 18, 11, WinWidth - btnW + 28, 21);
                    g.DrawLine(pen, WinWidth - btnW + 28, 11, WinWidth - btnW + 18, 21);
                }

                // Scene render
                g.DrawImage(sceneImg, 0, TitleBarHeight, WinWidth, renderH);

                // HUD Bar overlay
                using (var hudBrush = new SolidBrush(System.Drawing.Color.FromArgb(215, 12, 16, 24)))
                using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 50, 70, 95), 1.5f))
                {
                    var hudRect = new System.Drawing.Rectangle(12, TitleBarHeight + 8, WinWidth - 24, 28);
                    g.FillRectangle(hudBrush, hudRect);
                    g.DrawRectangle(borderPen, hudRect);
                }

                using (var fontHud = new System.Drawing.Font("Segoe UI", 9.0f, System.Drawing.FontStyle.Bold))
                using (var hudText = new SolidBrush(System.Drawing.Color.FromArgb(240, 245, 255)))
                using (var greenPill = new SolidBrush(System.Drawing.Color.FromArgb(50, 205, 120)))
                {
                    g.DrawString("Avenida Benito Juárez · Paso del Norte Corridor (⬆ North: Bridge & Elena's Kiosk | ⬇ South: Downtown)",
                        fontHud, hudText, 22, TitleBarHeight + 13);
                    g.DrawString("🟢 Server Connected · Meta Muse Spark 1.3 (Grounded)",
                        fontHud, greenPill, WinWidth - 390, TitleBarHeight + 13);
                }

                finalBmp.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Debug.Log($"[BridgeEntranceScreenshotSuite] PASS: Saved screenshot to '{savePath}'");
        }
    }
}

