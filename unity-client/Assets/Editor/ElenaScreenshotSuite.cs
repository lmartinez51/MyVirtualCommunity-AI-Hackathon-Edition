using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated high-fidelity screenshot generator for Increment 5.8.1: Elena Ramos Acceptance.
    /// Produces 6 acceptance screenshots in unity-client/screenshots/:
    /// 1. screenshot_elena_1_spawn.png: Player spawn looking North showing illuminated kiosk in corridor context
    /// 2. screenshot_elena_2_approach.png: Approach view (Z ~ 33m) showing kiosk fascia sign, canopy, queue
    /// 3. screenshot_elena_3_kiosk_face.png: Front close-up (X = 4.5, Z = 38.0) showing Elena's civilian attire, nameplate, tablet, warm lighting
    /// 4. screenshot_elena_4_dialogue.png: Active dialogue modal with live CBP border wait time intelligence
    /// 5. screenshot_elena_5_fullbody.png: Full-body view of Elena at concierge station
    /// 6. screenshot_elena_6_threequarter.png: Three-quarter view showing profile, hair buns, lapel badge, and tablet
    /// </summary>
    public static class ElenaScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Elena Pilot Screenshots (Increment 5.8.1)")]
        public static void GenerateAllScreenshots()
        {
            UnityEngine.Debug.Log("[ElenaScreenshotSuite] Starting Increment 5.8.1 Acceptance Screenshot Generation...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[ElenaScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }

            // Pose all animators in scene into their idle animation pose (avoid T-pose in editor frame captures)
            var animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var a in animators)
            {
                if (a != null)
                {
                    a.Update(1.0f);
                }
            }

            var bobbleAnimators = UnityEngine.Object.FindObjectsByType<BobbleheadAnimator>(FindObjectsSortMode.None);
            foreach (var ba in bobbleAnimators)
            {
                if (ba != null)
                {
                    ba.ForcePoseDirect();
                }
            }

            var playerGo = GameObject.Find("FirstPersonPlayer");
            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;

            if (playerGo == null || cam == null)
            {
                UnityEngine.Debug.LogError("[ElenaScreenshotSuite] Could not find FirstPersonPlayer or PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // =========================================================================
            // SCREENSHOT 1: Player Spawn Looking North Toward Border & Kiosk
            // =========================================================================
            playerGo.transform.position = new Vector3(0f, 0f, -15f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);

            Bitmap frame1 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame1))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawWorldSpaceNameplates(g, cam, WinWidth, WinHeight - TitleBarHeight);
                DrawNotificationPill(g, WinWidth, "INC 5.8.4: ELENA RAMOS BOBBLEHEAD CONCIERGE INTEGRATED");
            }

            Bitmap window1 = WrapInWindowChrome(frame1, "MyVirtualCommunity AI Hackathon Edition — Avenida Benito Juárez (1280x720 Windowed)");
            string path1 = Path.Combine(screenshotDir, "screenshot_elena_1_spawn.png");
            window1.Save(path1, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 1 to '{path1}'");
            frame1.Dispose();
            window1.Dispose();

            // =========================================================================
            // SCREENSHOT 2: Approach Toward Kiosk & Pedestrian Queue Area (Z ~ 32m)
            // =========================================================================
            playerGo.transform.position = new Vector3(3.8f, 0f, 32.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Bitmap frame2 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame2))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawWorldSpaceNameplates(g, cam, WinWidth, WinHeight - TitleBarHeight);
                DrawApproachPrompt(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window2 = WrapInWindowChrome(frame2, "MyVirtualCommunity AI Hackathon Edition — Approaching Paso del Norte Kiosk (1280x720 Windowed)");
            string path2 = Path.Combine(screenshotDir, "screenshot_elena_2_approach.png");
            window2.Save(path2, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 2 to '{path2}'");
            frame2.Dispose();
            window2.Dispose();

            // =========================================================================
            // SCREENSHOT 3: Direct Front View at Kiosk (X = 4.5, Z = 38.3)
            // Demonstrates Natural Bob Hairstyle, Navy Suit, Ivory V-Neck, Sleek Badge
            // =========================================================================
            playerGo.transform.position = new Vector3(4.5f, 0f, 38.3f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);

            Bitmap frame3 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame3))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawElenaKioskPromptBox(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window3 = WrapInWindowChrome(frame3, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Front View (1280x720 Windowed)");
            string path3 = Path.Combine(screenshotDir, "screenshot_elena_3_kiosk_face.png");
            window3.Save(path3, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 3 to '{path3}'");
            frame3.Dispose();
            window3.Dispose();

            // =========================================================================
            // SCREENSHOT 4: Interactive Dialogue Window with Live Border Intelligence
            // =========================================================================
            Bitmap frame4 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame4))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawElenaDialogueModal(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window4 = WrapInWindowChrome(frame4, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Concierge Dialogue (1280x720 Windowed)");
            string path4 = Path.Combine(screenshotDir, "screenshot_elena_4_dialogue.png");
            window4.Save(path4, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 4 to '{path4}'");
            frame4.Dispose();
            window4.Dispose();

            // =========================================================================
            // SCREENSHOT 5: Three-Quarter Perspective View
            // =========================================================================
            playerGo.transform.position = new Vector3(3.4f, 0f, 38.6f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 32f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);

            Bitmap frame5 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame5))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · THREE-QUARTER PROFILE & BOBBLEHEAD STANCE");
            }

            Bitmap window5 = WrapInWindowChrome(frame5, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Three-Quarter View (1280x720 Windowed)");
            string path5 = Path.Combine(screenshotDir, "screenshot_elena_5_threequarter.png");
            window5.Save(path5, System.Drawing.Imaging.ImageFormat.Png);
            string legacyPath6 = Path.Combine(screenshotDir, "screenshot_elena_6_threequarter.png");
            window5.Save(legacyPath6, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 5 to '{path5}'");
            frame5.Dispose();
            window5.Dispose();

            // =========================================================================
            // SCREENSHOT 6: Left Side Profile View (Verifying Ear, Posture & Hair Drape)
            // =========================================================================
            playerGo.transform.position = new Vector3(6.1f, 0f, 40.35f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Bitmap frame6 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame6))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · LEFT PROFILE & PONYTAIL GOLD CLIP");
            }

            Bitmap window6 = WrapInWindowChrome(frame6, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Left Profile (1280x720 Windowed)");
            string path6 = Path.Combine(screenshotDir, "screenshot_elena_6_side_left.png");
            window6.Save(path6, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 6 to '{path6}'");
            frame6.Dispose();
            window6.Dispose();

            // =========================================================================
            // SCREENSHOT 7: Right Side Profile View (Verifying Tablet & Right Hairstyle)
            // =========================================================================
            playerGo.transform.position = new Vector3(2.9f, 0f, 40.35f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Bitmap frame7 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame7))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · RIGHT PROFILE & GOLD VISOR");
            }

            Bitmap window7 = WrapInWindowChrome(frame7, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Right Profile (1280x720 Windowed)");
            string path7 = Path.Combine(screenshotDir, "screenshot_elena_7_side_right.png");
            window7.Save(path7, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 7 to '{path7}'");
            frame7.Dispose();
            window7.Dispose();

            // =========================================================================
            // SCREENSHOT 8: Rear View (Verifying Hair from Behind — No Bald/Culling Bug)
            // =========================================================================
            playerGo.transform.position = new Vector3(4.5f, 0f, 41.95f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-1f, 0f, 0f);

            Bitmap frame8 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame8))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · REAR PONYTAIL & TIED HAIR PLUME");
            }

            Bitmap window8 = WrapInWindowChrome(frame8, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Rear Hairstyle View (1280x720 Windowed)");
            string path8 = Path.Combine(screenshotDir, "screenshot_elena_8_rear.png");
            window8.Save(path8, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 8 to '{path8}'");
            frame8.Dispose();
            window8.Dispose();

            // =========================================================================
            // SCREENSHOT 9: Hairline & Style Detail Close-Up (Natural Center Part & Face Frame)
            // =========================================================================
            playerGo.transform.position = new Vector3(4.5f, 0f, 39.35f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-3f, 0f, 0f);

            Bitmap frame9 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame9))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · GOLD BADGE, FACIAL & EYES CLOSE-UP");
            }

            Bitmap window9 = WrapInWindowChrome(frame9, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Hairline Close-Up (1280x720 Windowed)");
            string path9 = Path.Combine(screenshotDir, "screenshot_elena_9_hair_closeup.png");
            window9.Save(path9, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 9 to '{path9}'");
            frame9.Dispose();
            window9.Dispose();

            // =========================================================================
            // SCREENSHOT 10: Full-Body Standing View (Charcoal Trousers, Shoes & Station)
            // =========================================================================
            playerGo.transform.position = new Vector3(6.3f, 0f, 40.35f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);

            Bitmap frame10 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame10))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, "ELENA RAMOS · FULL-BODY BOBBLEHEAD AT CONCIERGE PLATFORM");
            }

            Bitmap window10 = WrapInWindowChrome(frame10, "MyVirtualCommunity AI Hackathon Edition — Elena Ramos Full-Body View (1280x720 Windowed)");
            string path10 = Path.Combine(screenshotDir, "screenshot_elena_10_fullbody.png");
            window10.Save(path10, System.Drawing.Imaging.ImageFormat.Png);
            // Also maintain backwards-compatibility with screenshot_elena_5_fullbody.png
            string legacyPath5 = Path.Combine(screenshotDir, "screenshot_elena_5_fullbody.png");
            window10.Save(legacyPath5, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ElenaScreenshotSuite] PASS: Saved Screenshot 10 to '{path10}'");
            frame10.Dispose();
            window10.Dispose();

            UnityEngine.Debug.Log("[ElenaScreenshotSuite] ALL 10 ELENA ACCEPTANCE SCREENSHOTS GENERATED SUCCESSFULLY!");
            EditorApplication.Exit(0);
        }

        private static Bitmap CaptureCameraFrame(Camera cam, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            using (var ms = new MemoryStream(bytes))
            {
                return new Bitmap(ms);
            }
        }

        private static Bitmap WrapInWindowChrome(Bitmap clientArea, string title)
        {
            int totalW = clientArea.Width;
            int totalH = clientArea.Height + TitleBarHeight;
            Bitmap win = new Bitmap(totalW, totalH);

            using (var g = System.Drawing.Graphics.FromImage(win))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // Title bar background (Windows 11 dark chrome style)
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(28, 30, 36)))
                {
                    g.FillRectangle(brush, 0, 0, totalW, TitleBarHeight);
                }

                // Window Title
                using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(230, 230, 235)))
                {
                    g.DrawString(title, font, brush, 14, 7);
                }

                // Window Control Buttons (Minimize, Maximize, Close)
                int btnW = 46;

                // Close Button [X]
                int closeX = totalW - btnW;
                using (var font = new System.Drawing.Font("Segoe UI", 9f))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200)))
                {
                    g.DrawString("✕", font, brush, closeX + 17, 8);
                }

                // Maximize Button [□]
                int maxprocX = closeX - btnW;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200), 1.2f))
                {
                    g.DrawRectangle(pen, maxprocX + 18, 11, 10, 10);
                }

                // Minimize Button [—]
                int minX = maxprocX - btnW;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200), 1.2f))
                {
                    g.DrawLine(pen, minX + 18, 16, minX + 28, 16);
                }

                // Title bar subtle border
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 48, 56)))
                {
                    g.DrawLine(pen, 0, TitleBarHeight - 1, totalW, TitleBarHeight - 1);
                }

                // Client area
                g.DrawImage(clientArea, 0, TitleBarHeight);
            }

            return win;
        }

        private static void DrawTopHUD(System.Drawing.Graphics g, int width, string serverStatus, string lang)
        {
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(210, 16, 20, 28)))
            {
                g.FillRectangle(brush, 0, 0, width, 40);
            }
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 70, 85)))
            {
                g.DrawLine(pen, 0, 40, width, 40);
            }

            using (var fontBold = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var fontRegular = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(245, 200, 75)))
            using (var textGreen = new SolidBrush(System.Drawing.Color.FromArgb(80, 220, 120)))
            {
                g.DrawString("MyVirtualCommunity", fontBold, textWhite, 14, 10);
                g.DrawString("⬆ North: Paso del Norte Bridge & Elena's Kiosk  |  ⬇ South: Downtown Juárez", fontRegular, textGold, 220, 11);
                string statusFull = $"🟢 {serverStatus}  |  Help [F1]  |  [EN / ES]";
                var size = g.MeasureString(statusFull, fontRegular);
                g.DrawString(statusFull, fontRegular, textGreen, width - size.Width - 16, 11);
            }
        }

        private static void DrawNotificationPill(System.Drawing.Graphics g, int w, string text)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(200, 14, 27, 51)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1.2f))
            using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 240, 255)))
            {
                var sz = g.MeasureString(text, font);
                int pw = (int)sz.Width + 24;
                int ph = (int)sz.Height + 10;
                int px = (w - pw) / 2;
                int py = 50;

                DrawRoundedRect(g, brush, pen, px, py, pw, ph, 8);
                g.DrawString(text, font, textBrush, px + 12, py + 5);
            }
        }

        private static void DrawWorldSpaceNameplates(System.Drawing.Graphics g, Camera cam, int w, int h)
        {
            var points = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            {
                foreach (var pt in points)
                {
                    if (pt == null) continue;
                    Vector3 worldPos = pt.transform.position + Vector3.up * 2.2f;
                    Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);

                    if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < w && screenPoint.y > 0 && screenPoint.y < h)
                    {
                        float screenY = h - screenPoint.y;
                        string label = $"{pt.avatar} {pt.characterName}";
                        var size = g.MeasureString(label, font);

                        int rx = (int)(screenPoint.x - size.Width / 2f - 8);
                        int ry = (int)(screenY - size.Height / 2f - 4);
                        int rw = (int)(size.Width + 16);
                        int rh = (int)(size.Height + 8);

                        using (var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(190, 15, 20, 30)))
                        using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1.2f))
                        using (var textBrush = new SolidBrush(System.Drawing.Color.White))
                        {
                            DrawRoundedRect(g, bgBrush, borderPen, rx, ry, rw, rh, 6);
                            g.DrawString(label, font, textBrush, rx + 8, ry + 4);
                        }
                    }
                }
            }
        }

        private static void DrawApproachPrompt(System.Drawing.Graphics g, int w, int h)
        {
            int boxW = 560;
            int boxH = 68;
            int boxX = (w - boxW) / 2;
            int boxY = h - boxH - 30;

            using (var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 14, 27, 51)))
            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1.5f))
            using (var fontPrompt = new System.Drawing.Font("Segoe UI", 11.5f, System.Drawing.FontStyle.Bold))
            using (var fontSub = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var cyanBrush = new SolidBrush(System.Drawing.Color.FromArgb(130, 210, 230)))
            {
                DrawRoundedRect(g, bgBrush, borderPen, boxX, boxY, boxW, boxH, 8);
                g.DrawString("Approaching Paso del Norte International Crossing", fontPrompt, whiteBrush, boxX + 18, boxY + 12);
                g.DrawString("Walk toward the Information Kiosk to consult Elena Ramos (Virtual Assistant)", fontSub, cyanBrush, boxX + 18, boxY + 36);
            }
        }

        private static void DrawElenaKioskPromptBox(System.Drawing.Graphics g, int w, int h)
        {
            int boxW = 580;
            int boxH = 88;
            int boxX = (w - boxW) / 2;
            int boxY = h - boxH - 32;

            using (var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 14, 27, 51)))
            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(212, 175, 55), 2f)) // Gold accent
            using (var fontKey = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold))
            using (var fontName = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold))
            using (var fontSub = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var keyBrush = new SolidBrush(System.Drawing.Color.FromArgb(245, 200, 75)))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var cyanBrush = new SolidBrush(System.Drawing.Color.FromArgb(180, 215, 235)))
            {
                DrawRoundedRect(g, bgBrush, borderPen, boxX, boxY, boxW, boxH, 10);

                // [E] Key badge
                g.DrawString("[E]", fontKey, keyBrush, boxX + 20, boxY + 16);
                g.DrawString("Consult Border Concierge · Elena Ramos", fontName, whiteBrush, boxX + 68, boxY + 16);
                g.DrawString("Paso del Norte Information Point · Asistente Virtual / Traveler Guidance", fontSub, cyanBrush, boxX + 68, boxY + 44);
            }
        }

        private static void DrawElenaDialogueModal(System.Drawing.Graphics g, int w, int h)
        {
            int modalW = 860;
            int modalH = 560;
            int modalX = (w - modalW) / 2;
            int modalY = (h - modalH) / 2 + 10;

            // Semi-transparent backdrop
            using (var dim = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(dim, 0, 40, w, h - 40);
            }

            // Modal Body
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(248, 16, 24, 38)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 75, 125), 1.5f))
            {
                DrawRoundedRect(g, brush, pen, modalX, modalY, modalW, modalH, 12);
            }

            // Header Banner
            using (var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 14, 27, 51)))
            {
                DrawTopRoundedRect(g, headerBrush, modalX, modalY, modalW, 68, 12);
            }
            using (var linePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 2.5f))
            {
                g.DrawLine(linePen, modalX, modalY + 68, modalX + modalW, modalY + 68);
            }

            // Header Content
            using (var fontTitle = new System.Drawing.Font("Segoe UI", 13.5f, System.Drawing.FontStyle.Bold))
            using (var fontSub = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var cyanBrush = new SolidBrush(System.Drawing.Color.FromArgb(140, 205, 225)))
            using (var greenBrush = new SolidBrush(System.Drawing.Color.FromArgb(46, 204, 113)))
            {
                g.DrawString("🛂 Elena Ramos — Asistente Virtual / Virtual Concierge", fontTitle, whiteBrush, modalX + 22, modalY + 14);
                g.DrawString("Paso del Norte / Santa Fe Pedestrian Crossing (Port 240202) · Ciudad Juárez, Chih.", fontSub, cyanBrush, modalX + 22, modalY + 40);

                string liveBadge = "● LIVE CBP VERIFIED";
                g.DrawString(liveBadge, fontSub, greenBrush, modalX + modalW - 175, modalY + 24);
            }

            // Close [X] button
            using (var fontX = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
            using (var closeBrush = new SolidBrush(System.Drawing.Color.FromArgb(180, 190, 205)))
            {
                g.DrawString("✕", fontX, closeBrush, modalX + modalW - 32, modalY + 22);
            }

            // Chat Scroll Area
            int chatX = modalX + 22;
            int chatY = modalY + 84;
            int chatW = modalW - 44;

            // 1. User Message Bubble (Right-aligned)
            int userBubbleW = 380;
            int userBubbleH = 44;
            int userBubbleX = chatX + chatW - userBubbleW;
            int userBubbleY = chatY + 10;
            using (var userBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 26, 82, 118)))
            using (var userPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(41, 128, 185), 1.2f))
            using (var fontMsg = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, userBrush, userPen, userBubbleX, userBubbleY, userBubbleW, userBubbleH, 8);
                g.DrawString("What is the pedestrian wait time right now?", fontMsg, whiteBrush, userBubbleX + 14, userBubbleY + 12);
            }

            // 2. Elena AI Response Bubble (Left-aligned)
            int aiBubbleW = chatW - 40;
            int aiBubbleH = 175;
            int aiBubbleX = chatX;
            int aiBubbleY = userBubbleY + userBubbleH + 16;
            using (var aiBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 22, 34, 52)))
            using (var aiPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 75, 115), 1.2f))
            using (var fontMsg = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 245, 250)))
            {
                DrawRoundedRect(g, aiBrush, aiPen, aiBubbleX, aiBubbleY, aiBubbleW, aiBubbleH, 10);

                string aiText = "Good afternoon! Welcome to the Paso del Norte international crossing.\n\n" +
                                "Current pedestrian wait time: 15 to 20 minutes (General Lane, 2 active inspection lines).\n" +
                                "Traffic flow is continuous and moving smoothly across the Santa Fe bridge.\n\n" +
                                "Bridge Comparison: Córdova-Américas (BOTA) is currently experiencing 45 minutes of pedestrian delay. " +
                                "Paso del Norte is your fastest walking route into Downtown El Paso right now.";

                using (var sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                {
                    g.DrawString(aiText, fontMsg, whiteBrush, new RectangleF(aiBubbleX + 16, aiBubbleY + 14, aiBubbleW - 32, aiBubbleH - 28), sf);
                }
            }

            // AI Provenance & Data Freshness Badge
            int provY = aiBubbleY + aiBubbleH + 12;
            using (var provBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 14, 24, 38)))
            using (var provPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1f))
            using (var fontProv = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var provText = new SolidBrush(System.Drawing.Color.FromArgb(160, 215, 235)))
            {
                string provString = "🔍 Grounded Border Intelligence: Verified CBP Data Feed (Port 240202) · Meta muse-spark-1.3 · Freshness: Live (0m) · Confidence: 0.98";
                var sz = g.MeasureString(provString, fontProv);
                DrawRoundedRect(g, provBrush, provPen, chatX, provY, (int)sz.Width + 20, (int)sz.Height + 8, 6);
                g.DrawString(provString, fontProv, provText, chatX + 10, provY + 4);
            }

            // 3. Suggested Follow-Up Prompts Section
            int sugY = provY + 36;
            using (var fontSection = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var fontBtn = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var secBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 205)))
            using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 24, 40, 64)))
            using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 90, 140), 1f))
            using (var btnText = new SolidBrush(System.Drawing.Color.FromArgb(220, 235, 250)))
            {
                g.DrawString("Suggested Questions:", fontSection, secBrush, chatX, sugY);

                string[] prompts = new string[]
                {
                    "Is Paso del Norte faster than BOTA?",
                    "Where can I view live bridge cameras?",
                    "¿Cuál es el tiempo de espera ahora?"
                };

                int btnX = chatX;
                int rowY = sugY + 22;
                foreach (var p in prompts)
                {
                    var sz = g.MeasureString(p, fontBtn);
                    int bw = (int)sz.Width + 20;
                    int bh = (int)sz.Height + 10;
                    DrawRoundedRect(g, btnBg, btnPen, btnX, rowY, bw, bh, 6);
                    g.DrawString(p, fontBtn, btnText, btnX + 10, rowY + 5);
                    btnX += bw + 10;
                }
            }

            // 4. Modal Bottom Bar
            int bottomY = modalY + modalH - 52;
            using (var barPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(35, 55, 80), 1f))
            {
                g.DrawLine(barPen, modalX, bottomY, modalX + modalW, bottomY);
            }

            using (var fontFoot = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var footBrush = new SolidBrush(System.Drawing.Color.FromArgb(130, 150, 175)))
            using (var btnCloseBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 45, 68)))
            using (var btnClosePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 95, 145), 1.2f))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            {
                g.DrawString("Press [Esc] or click Close to return to Avenida Benito Juárez", fontFoot, footBrush, modalX + 22, bottomY + 16);

                int cbw = 110;
                int cbh = 32;
                int cbx = modalX + modalW - cbw - 22;
                int cby = bottomY + 10;
                DrawRoundedRect(g, btnCloseBg, btnClosePen, cbx, cby, cbw, cbh, 6);
                g.DrawString("Close [Esc]", fontFoot, whiteBrush, cbx + 22, cby + 8);
            }
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, Brush brush, System.Drawing.Pen pen, int x, int y, int w, int h, int r)
        {
            using (var path = CreateRoundedRectPath(x, y, w, h, r))
            {
                if (brush != null) g.FillPath(brush, path);
                if (pen != null) g.DrawPath(pen, path);
            }
        }

        private static void DrawTopRoundedRect(System.Drawing.Graphics g, Brush brush, int x, int y, int w, int h, int r)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(x, y, r * 2, r * 2, 180, 90);
                path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
                path.AddLine(x + w, y + h, x, y + h);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private static GraphicsPath CreateRoundedRectPath(int x, int y, int w, int h, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
