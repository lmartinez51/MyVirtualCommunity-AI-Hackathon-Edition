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
            cam.transform.position = new Vector3(0f, 1.8f, 12f);
            cam.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_01_approach_downtown.png"),
                "MyVirtualCommunity — Approach to Paso del Norte Arched Bridge Canopy (1280x720)");

            // 2. Canopy Facade Close-up: Frontal view (Z = 28m) showcasing the 4 arched portals & monumental sign
            cam.transform.position = new Vector3(0f, 3.2f, 26f);
            cam.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_02_canopy_facade_closeup.png"),
                "MyVirtualCommunity — Authentic Arched Concrete Canopy & PC-51 Signage (1280x720)");

            // 3. Elena Ramos Kiosk on East Pedestrian Plaza: Medium shot (Z = 31m, X = 11.5m)
            cam.transform.position = new Vector3(11.2f, 1.7f, 32.5f);
            cam.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_03_elena_plaza_kiosk.png"),
                "MyVirtualCommunity — Elena Ramos Relocated to East Pedestrian Plaza (1280x720)");

            // 4. East Pedestrian Promenade Overview: Showing Elena's Kiosk and Queue (Mateo, Sofia, Hector)
            cam.transform.position = new Vector3(14.8f, 2.4f, 29f);
            cam.transform.rotation = Quaternion.Euler(6f, -22f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_04_pedestrian_queue_and_plaza.png"),
                "MyVirtualCommunity — Pedestrian Promenade, Official Kiosk & Traveler Queue (1280x720)");

            // 5. 4 Vehicular Lanes & Toll Booths Perspective: Showing Lane 1, Telepeaje, Lane 3, Bus Lane 4
            cam.transform.position = new Vector3(-2.0f, 1.6f, 33f);
            cam.transform.rotation = Quaternion.Euler(3f, 5f, 0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_05_toll_lanes_perspective.png"),
                "MyVirtualCommunity — 4 Entry Lanes, Divider Islands & Toll Collection Casetas (1280x720)");

            Debug.Log("[BridgeEntranceScreenshotSuite] All 5 Bridge Entrance Verification Screenshots Generated Successfully!");
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

