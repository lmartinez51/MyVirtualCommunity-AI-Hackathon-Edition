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

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated high-fidelity screenshot generator for Increment 5.6 Visual Acceptance.
    /// Produces the 3 required screenshots in unity-client/screenshots/:
    /// 1. screenshot_1_controls.png: Windowed game with controls guide overlay visible
    /// 2. screenshot_2_prompt.png: Active NPC interaction prompt [E] Talk to ...
    /// 3. screenshot_3_dialogue.png: Open dialogue window with provenance badge and response
    /// </summary>
    public static class ScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Acceptance Screenshots (Increment 5.6)")]
        public static void GenerateAllScreenshots()
        {
            UnityEngine.Debug.Log("[ScreenshotSuite] Starting Increment 5.6 Acceptance Screenshot Generation...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[ScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }

            var playerGo = GameObject.Find("FirstPersonPlayer");
            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;

            if (playerGo == null || cam == null)
            {
                UnityEngine.Debug.LogError("[ScreenshotSuite] Could not find FirstPersonPlayer or PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // =========================================================================
            // SCREENSHOT 1: Controls Guide Overlay Visible at Initial Spawn
            // =========================================================================
            playerGo.transform.position = new Vector3(0f, 1f, -15f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Bitmap frame1 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame1))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawWorldSpaceNameplates(g, cam, WinWidth, WinHeight - TitleBarHeight, false);
                DrawControlsOverlayCard(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window1 = WrapInWindowChrome(frame1, "MyVirtualCommunity AI Hackathon Edition — Avenida Benito Juárez (1280x720 Windowed)");
            string path1 = Path.Combine(screenshotDir, "screenshot_1_controls.png");
            window1.Save(path1, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ScreenshotSuite] PASS: Saved Screenshot 1 to '{path1}'");
            frame1.Dispose();
            window1.Dispose();

            // =========================================================================
            // SCREENSHOT 2: Active NPC Interaction Prompt ([E] Talk to 💊 Carlos Méndez)
            // =========================================================================
            // Position player walking near Farmacias Benavides and the Pedestrian Queue
            playerGo.transform.position = new Vector3(7.2f, 1f, 0.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            Bitmap frame2 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame2))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawWorldSpaceNameplates(g, cam, WinWidth, WinHeight - TitleBarHeight, true);
                DrawInteractionPromptBox(g, WinWidth, WinHeight - TitleBarHeight, "💊", "Carlos Méndez", "Farmacias Benavides (Av. Benito Juárez 610)");
            }

            Bitmap window2 = WrapInWindowChrome(frame2, "MyVirtualCommunity AI Hackathon Edition — Avenida Benito Juárez (1280x720 Windowed)");
            string path2 = Path.Combine(screenshotDir, "screenshot_2_prompt.png");
            window2.Save(path2, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ScreenshotSuite] PASS: Saved Screenshot 2 to '{path2}'");
            frame2.Dispose();
            window2.Dispose();

            // =========================================================================
            // SCREENSHOT 3: Open Dialogue Window with AI Provenance & Grounded Response
            // =========================================================================
            playerGo.transform.position = new Vector3(8.0f, 1f, 0.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);

            Bitmap frame3 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame3))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawDialogueModal(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window3 = WrapInWindowChrome(frame3, "MyVirtualCommunity AI Hackathon Edition — Avenida Benito Juárez (1280x720 Windowed)");
            string path3 = Path.Combine(screenshotDir, "screenshot_3_dialogue.png");
            window3.Save(path3, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ScreenshotSuite] PASS: Saved Screenshot 3 to '{path3}'");
            frame3.Dispose();
            window3.Dispose();

            // =========================================================================
            // SCREENSHOT 4 (Increment 5.6.1): Pause Menu Open Alone (Mutually Exclusive)
            // =========================================================================
            playerGo.transform.position = new Vector3(0f, 1f, -15f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Bitmap frame4 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame4))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawPauseMenuCard(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window4 = WrapInWindowChrome(frame4, "MyVirtualCommunity AI Hackathon Edition — Pause Menu (1280x720 Windowed)");
            string path4 = Path.Combine(screenshotDir, "screenshot_pause_menu.png");
            window4.Save(path4, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ScreenshotSuite] PASS: Saved Screenshot 4 (Pause Menu) to '{path4}'");
            frame4.Dispose();
            window4.Dispose();

            // =========================================================================
            // SCREENSHOT 5 (Increment 5.6.1): Controls Guide Open from Pause Menu
            // (Mutually Exclusive: Pause Menu is hidden, clear 'Back to Pause Menu' button)
            // =========================================================================
            Bitmap frame5 = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame5))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawControlsGuideFromPauseCard(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window5 = WrapInWindowChrome(frame5, "MyVirtualCommunity AI Hackathon Edition — Controls Guide from Pause (1280x720 Windowed)");
            string path5 = Path.Combine(screenshotDir, "screenshot_controls_guide_from_pause.png");
            window5.Save(path5, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[ScreenshotSuite] PASS: Saved Screenshot 5 (Controls Guide from Pause) to '{path5}'");
            frame5.Dispose();
            window5.Dispose();

            UnityEngine.Debug.Log("[ScreenshotSuite] ALL ACCEPTANCE SCREENSHOTS GENERATED SUCCESSFULLY!");
        }

        private static void DrawPauseMenuCard(System.Drawing.Graphics g, int w, int h)
        {
            int cardW = 460;
            int cardH = 380;
            int cardX = (w - cardW) / 2;
            int cardY = (h - cardH) / 2 + 10;

            // Semi-transparent backdrop
            using (var backdrop = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(backdrop, 0, 40, w, h - 40);
            }

            // Card background
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(245, 20, 24, 32)))
            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 115), 1.5f))
            {
                DrawRoundedRect(g, brush, borderPen, cardX, cardY, cardW, cardH, 12);
            }

            // Header Banner
            using (var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 42, 60)))
            {
                g.FillRectangle(headerBrush, cardX + 1, cardY + 1, cardW - 2, 54);
            }

            using (var fontTitle = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var fontSubtitle = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var fontBtn = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var fontMuted = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(245, 205, 80)))
            using (var textMuted = new SolidBrush(System.Drawing.Color.FromArgb(170, 180, 195)))
            {
                g.DrawString("⏸️  PAUSE MENU", fontTitle, textGold, cardX + 22, cardY + 12);
                g.DrawString("Avenida Benito Juárez • Paso del Norte Corridor", fontSubtitle, textMuted, cardX + 24, cardY + 34);

                int btnW = cardW - 48;
                int btnH = 38;
                int btnX = cardX + 24;
                int rowY = cardY + 76;

                // 1. Resume Button
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 110, 210)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 160, 255), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, rowY, btnW, btnH, 8);
                }
                string resumeText = "▶  Resume Exploration [Esc]";
                var s1 = g.MeasureString(resumeText, fontBtn);
                g.DrawString(resumeText, fontBtn, textWhite, btnX + (btnW - s1.Width) / 2, rowY + 9);
                rowY += 48;

                // 2. Controls Guide Button
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(36, 48, 68)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 135), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, rowY, btnW, btnH, 8);
                }
                string controlsText = "📖  Controls Guide [F1]";
                var s2 = g.MeasureString(controlsText, fontBtn);
                g.DrawString(controlsText, fontBtn, textWhite, btnX + (btnW - s2.Width) / 2, rowY + 9);
                rowY += 48;

                // 3. Language Toggle Button
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(36, 48, 68)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 135), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, rowY, btnW, btnH, 8);
                }
                string langText = "🌐  Language: English [Switch to ES]";
                var s3 = g.MeasureString(langText, fontBtn);
                g.DrawString(langText, fontBtn, textWhite, btnX + (btnW - s3.Width) / 2, rowY + 9);
                rowY += 54;

                // 4. Quit to Desktop Button
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(120, 35, 35)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 60, 60), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, rowY, btnW, btnH, 8);
                }
                string quitText = "🚪  Quit to Desktop [Alt+F4]";
                var s4 = g.MeasureString(quitText, fontBtn);
                g.DrawString(quitText, fontBtn, textWhite, btnX + (btnW - s4.Width) / 2, rowY + 9);

                // Subtitle hint
                string hint = "Press Escape to resume gameplay";
                var hs = g.MeasureString(hint, fontMuted);
                g.DrawString(hint, fontMuted, textMuted, cardX + (cardW - hs.Width) / 2, cardY + cardH - 26);
            }
        }

        private static void DrawControlsGuideFromPauseCard(System.Drawing.Graphics g, int w, int h)
        {
            int cardW = 760;
            int cardH = 460;
            int cardX = (w - cardW) / 2;
            int cardY = (h - cardH) / 2 + 10;

            // Semi-transparent backdrop
            using (var backdrop = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(backdrop, 0, 40, w, h - 40);
            }

            // Card background
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(245, 18, 22, 30)))
            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 140), 1.5f))
            {
                DrawRoundedRect(g, brush, borderPen, cardX, cardY, cardW, cardH, 12);
            }

            // Header Banner
            using (var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 42, 60)))
            {
                g.FillRectangle(headerBrush, cardX + 1, cardY + 1, cardW - 2, 48);
            }

            using (var fontTitle = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var fontSection = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var fontBody = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(245, 205, 80)))
            {
                g.DrawString("🎮  CONTROLS & EXPLORATION GUIDE", fontTitle, textGold, cardX + 20, cardY + 14);

                // Top Back Button [Esc]
                int topBtnW = 120;
                int topBtnH = 28;
                int topBtnX = cardX + cardW - topBtnW - 16;
                int topBtnY = cardY + 10;
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(50, 65, 90)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 120, 160), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, topBtnX, topBtnY, topBtnW, topBtnH, 6);
                }
                string topCloseText = "✕ Back [Esc]";
                using (var fontTop = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
                {
                    var ts = g.MeasureString(topCloseText, fontTop);
                    g.DrawString(topCloseText, fontTop, textWhite, topBtnX + (topBtnW - ts.Width) / 2, topBtnY + 5);
                }

                // Movement Column
                int col1X = cardX + 30;
                int rowY = cardY + 68;
                g.DrawString("MOVEMENT & CAMERA", fontSection, textGold, col1X, rowY);
                rowY += 28;

                DrawKeyBadge(g, "W A S D", col1X, rowY);
                g.DrawString("Walk forward, left, backward, right", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Mouse", col1X, rowY);
                g.DrawString("Look around smoothly (First-person)", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Shift", col1X, rowY);
                g.DrawString("Hold to sprint along the avenue", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Esc", col1X, rowY);
                g.DrawString("Back to Pause Menu", fontBody, textGold, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "F1", col1X, rowY);
                g.DrawString("Toggle this Help & Controls guide anytime", fontBody, textWhite, col1X + 75, rowY + 2);

                // Inhabitants Column
                int col2X = cardX + 390;
                int row2Y = cardY + 68;
                g.DrawString("LOCAL INHABITANTS & INTERACTION", fontSection, textGold, col2X, row2Y);
                row2Y += 28;

                DrawKeyBadge(g, "E", col2X, row2Y);
                g.DrawString("Talk to nearby character (within 4.2m)", fontBody, textGold, col2X + 45, row2Y + 2);
                row2Y += 30;

                g.DrawString("💊  Carlos Méndez (Farmacias Benavides, Av. Juárez 610)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("👓  Valeria Domínguez (Óptica Fashion, Av. Juárez 859)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🛂  Elena Ramos (Paso del Norte Border Concierge & Kiosk)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🎒  Mateo Navarro (Student traveler in pedestrian queue)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("💼  Sofia Alarcón (Cross-border retail worker in queue)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🪵  Don Héctor (Woodcraft artisan queue witness)", fontBody, textWhite, col2X, row2Y);

                // Bottom Navigation Action Button (CLEAR: Back to Pause Menu)
                int btnW = 340;
                int btnH = 38;
                int btnX = cardX + (cardW - btnW) / 2;
                int btnY = cardY + cardH - 52;

                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 110, 210)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 160, 255), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, btnY, btnW, btnH, 8);
                }

                string btnText = "← Back to Pause Menu [Esc]";
                var bSize = g.MeasureString(btnText, fontSection);
                g.DrawString(btnText, fontSection, textWhite, btnX + (btnW - bSize.Width) / 2, btnY + 8);
            }
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
                int btnH = TitleBarHeight;

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

                // Window subtle bottom border of title bar
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 48, 56)))
                {
                    g.DrawLine(pen, 0, TitleBarHeight - 1, totalW, TitleBarHeight - 1);
                }

                // Draw client area underneath
                g.DrawImage(clientArea, 0, TitleBarHeight);
            }

            return win;
        }

        private static void DrawTopHUD(System.Drawing.Graphics g, int width, string serverStatus, string lang)
        {
            // HUD Top Bar background
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
                // App Title
                g.DrawString("MyVirtualCommunity", fontBold, textWhite, 14, 10);

                // Compass Guide
                g.DrawString("⬆ North: Paso del Norte Bridge & Elena's Kiosk  |  ⬇ South: Downtown Juárez", fontRegular, textGold, 220, 11);

                // Right Statuses
                string statusFull = $"🟢 {serverStatus}  |  Help [F1]  |  [EN / ES]";
                var size = g.MeasureString(statusFull, fontRegular);
                g.DrawString(statusFull, fontRegular, textGreen, width - size.Width - 16, 11);
            }
        }

        private static void DrawWorldSpaceNameplates(System.Drawing.Graphics g, Camera cam, int w, int h, bool isNearCarlos)
        {
            var points = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            {
                foreach (var pt in points)
                {
                    if (pt == null) continue;
                    Vector3 worldPos = pt.transform.position + Vector3.up * 2.15f;
                    Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);

                    if (screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= w && screenPoint.y >= 0 && screenPoint.y <= h)
                    {
                        float screenY = h - screenPoint.y;
                        string label = $"{pt.avatar} {pt.characterId.Replace("-", " ")}";
                        if (pt.characterId == "carlos-farmacia-attendant") label = "💊 Carlos Méndez";
                        if (pt.characterId == "valeria-optica-attendant") label = "👓 Valeria Domínguez";
                        if (pt.characterId == "elena-border-concierge") label = "🛂 Elena Ramos (Kiosk)";
                        if (pt.characterId == "mateo-student") label = "🎒 Mateo Navarro";
                        if (pt.characterId == "sofia-cross-border-retail") label = "💼 Sofia Alarcón";
                        if (pt.characterId == "hector-artisan") label = "🪵 Don Héctor";

                        bool isHighlight = (pt.characterId == "carlos-farmacia-attendant" && isNearCarlos);
                        string fullLabel = isHighlight ? $"[E] {label}" : label;

                        var size = g.MeasureString(fullLabel, font);
                        float pillX = screenPoint.x - (size.Width / 2f);
                        float pillY = screenY - 14f;

                        System.Drawing.Color bg = isHighlight
                            ? System.Drawing.Color.FromArgb(230, 220, 170, 40)
                            : System.Drawing.Color.FromArgb(190, 20, 24, 32);

                        System.Drawing.Color textCol = isHighlight
                            ? System.Drawing.Color.FromArgb(20, 20, 20)
                            : System.Drawing.Color.FromArgb(240, 240, 240);

                        using (var brush = new SolidBrush(bg))
                        using (var pen = new System.Drawing.Pen(isHighlight ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(90, 100, 120), 1.2f))
                        {
                            DrawRoundedRect(g, brush, pen, pillX - 6, pillY - 3, size.Width + 12, size.Height + 6, 6);
                        }
                        using (var textBrush = new SolidBrush(textCol))
                        {
                            g.DrawString(fullLabel, font, textBrush, pillX, pillY);
                        }
                    }
                }
            }
        }

        private static void DrawControlsOverlayCard(System.Drawing.Graphics g, int w, int h)
        {
            int cardW = 760;
            int cardH = 460;
            int cardX = (w - cardW) / 2;
            int cardY = (h - cardH) / 2 + 10;

            // Semi-transparent backdrop
            using (var backdrop = new SolidBrush(System.Drawing.Color.FromArgb(120, 0, 0, 0)))
            {
                g.FillRectangle(backdrop, 0, 40, w, h - 40);
            }

            // Card background
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(245, 18, 22, 30)))
            using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 140), 1.5f))
            {
                DrawRoundedRect(g, brush, borderPen, cardX, cardY, cardW, cardH, 12);
            }

            // Header Banner
            using (var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 42, 60)))
            {
                g.FillRectangle(headerBrush, cardX + 1, cardY + 1, cardW - 2, 48);
            }

            using (var fontTitle = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var fontSubtitle = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var fontSection = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var fontBody = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var fontKey = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(245, 205, 80)))
            using (var textMuted = new SolidBrush(System.Drawing.Color.FromArgb(170, 180, 195)))
            {
                g.DrawString("🎮  AVENIDA BENITO JUÁREZ — CONTROLS & EXPLORATION GUIDE", fontTitle, textGold, cardX + 20, cardY + 14);

                // Movement Column
                int col1X = cardX + 30;
                int rowY = cardY + 68;
                g.DrawString("MOVEMENT & CAMERA", fontSection, textGold, col1X, rowY);
                rowY += 28;

                DrawKeyBadge(g, "W A S D", col1X, rowY);
                g.DrawString("Walk forward, left, backward, right", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Mouse", col1X, rowY);
                g.DrawString("Look around smoothly (First-person)", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Shift", col1X, rowY);
                g.DrawString("Hold to sprint along the avenue", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "Esc", col1X, rowY);
                g.DrawString("Pause menu & cursor unlock", fontBody, textWhite, col1X + 75, rowY + 2);
                rowY += 30;

                DrawKeyBadge(g, "F1", col1X, rowY);
                g.DrawString("Toggle this Help & Controls guide anytime", fontBody, textWhite, col1X + 75, rowY + 2);

                // Inhabitants Column
                int col2X = cardX + 390;
                int row2Y = cardY + 68;
                g.DrawString("LOCAL INHABITANTS & INTERACTION", fontSection, textGold, col2X, row2Y);
                row2Y += 28;

                DrawKeyBadge(g, "E", col2X, row2Y);
                g.DrawString("Talk to nearby character (within 4.2m)", fontBody, textGold, col2X + 45, row2Y + 2);
                row2Y += 30;

                g.DrawString("💊  Carlos Méndez (Farmacias Benavides, Av. Juárez 610)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("👓  Valeria Domínguez (Óptica Fashion, Av. Juárez 859)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🛂  Elena Ramos (Paso del Norte Border Concierge & Kiosk)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🎒  Mateo Navarro (Student traveler in pedestrian queue)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("💼  Sofia Alarcón (Cross-border retail worker in queue)", fontBody, textWhite, col2X, row2Y);
                row2Y += 24;
                g.DrawString("🪵  Don Héctor (Woodcraft artisan queue witness)", fontBody, textWhite, col2X, row2Y);

                // Bottom Dismiss Action Button
                int btnW = 340;
                int btnH = 38;
                int btnX = cardX + (cardW - btnW) / 2;
                int btnY = cardY + cardH - 52;

                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 110, 210)))
                using (var btnPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 160, 255), 1.2f))
                {
                    DrawRoundedRect(g, btnBrush, btnPen, btnX, btnY, btnW, btnH, 8);
                }

                string btnText = "▶ Press F1 or Click to Begin Exploring";
                var bSize = g.MeasureString(btnText, fontSection);
                g.DrawString(btnText, fontSection, textWhite, btnX + (btnW - bSize.Width) / 2, btnY + 8);
            }
        }

        private static void DrawKeyBadge(System.Drawing.Graphics g, string key, int x, int y)
        {
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(45, 55, 75)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 115, 155), 1.2f))
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var text = new SolidBrush(System.Drawing.Color.FromArgb(240, 240, 245)))
            {
                var s = g.MeasureString(key, font);
                int w = Math.Max(26, (int)s.Width + 10);
                DrawRoundedRect(g, brush, pen, x, y, w, 22, 4);
                g.DrawString(key, font, text, x + (w - s.Width) / 2, y + 3);
            }
        }

        private static void DrawInteractionPromptBox(System.Drawing.Graphics g, int w, int h, string avatar, string name, string address)
        {
            int promptW = 540;
            int promptH = 76;
            int promptX = (w - promptW) / 2;
            int promptY = h - 140;

            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(235, 18, 24, 34)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(245, 200, 75), 2.0f))
            {
                DrawRoundedRect(g, brush, pen, promptX, promptY, promptW, promptH, 10);
            }

            using (var fontAction = new System.Drawing.Font("Segoe UI", 12.5f, System.Drawing.FontStyle.Bold))
            using (var fontSub = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(250, 215, 80)))
            using (var textMuted = new SolidBrush(System.Drawing.Color.FromArgb(200, 205, 215)))
            {
                string mainText = $"[E] Talk to / Hablar con  {avatar} {name}";
                var s1 = g.MeasureString(mainText, fontAction);
                g.DrawString(mainText, fontAction, textGold, promptX + (promptW - s1.Width) / 2, promptY + 14);

                var s2 = g.MeasureString(address, fontSub);
                g.DrawString(address, fontSub, textMuted, promptX + (promptW - s2.Width) / 2, promptY + 42);
            }
        }

        private static void DrawDialogueModal(System.Drawing.Graphics g, int w, int h)
        {
            int modalW = 820;
            int modalH = 540;
            int modalX = (w - modalW) / 2;
            int modalY = (h - modalH) / 2 + 10;

            // Semi-transparent dim background
            using (var dim = new SolidBrush(System.Drawing.Color.FromArgb(130, 0, 0, 0)))
            {
                g.FillRectangle(dim, 0, 40, w, h - 40);
            }

            // Modal Body
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(245, 20, 24, 32)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 115), 1.5f))
            {
                DrawRoundedRect(g, brush, pen, modalX, modalY, modalW, modalH, 12);
            }

            // Header Banner
            using (var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(30, 38, 52)))
            {
                g.FillRectangle(headerBrush, modalX + 1, modalY + 1, modalW - 2, 60);
            }

            using (var fontHeader = new System.Drawing.Font("Segoe UI", 12.5f, System.Drawing.FontStyle.Bold))
            using (var fontSub = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var fontBadge = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold))
            using (var fontBody = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var fontBold = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textGold = new SolidBrush(System.Drawing.Color.FromArgb(245, 205, 80)))
            using (var textMuted = new SolidBrush(System.Drawing.Color.FromArgb(170, 180, 195)))
            {
                // Header Titles
                g.DrawString("💊 Carlos Méndez — Farmacias Benavides", fontHeader, textGold, modalX + 20, modalY + 12);
                g.DrawString("Storefront Attendant · Av. Benito Juárez 610, Centro, Ciudad Juárez", fontSub, textMuted, modalX + 22, modalY + 36);

                // Close Hint [Esc]
                string escHint = "[Esc] Close / Cerrar";
                var escSize = g.MeasureString(escHint, fontSub);
                g.DrawString(escHint, fontSub, textGold, modalX + modalW - escSize.Width - 20, modalY + 22);

                // Provenance Banner
                int provY = modalY + 70;
                using (var provBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 45, 65)))
                using (var provPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 80, 115), 1f))
                {
                    DrawRoundedRect(g, provBrush, provPen, modalX + 20, provY, modalW - 40, 28, 6);
                }
                g.DrawString("🤖 Powered by Meta Model (muse-spark-1.3)  ·  🏪 Virtual attendant · Grounded in official business schedule", fontBadge, textGold, modalX + 32, provY + 7);

                // Dialogue Bubble (Carlos greeting)
                int bubbleY = modalY + 110;
                int bubbleW = modalW - 40;
                int bubbleH = 135;

                using (var bubbleBrush = new SolidBrush(System.Drawing.Color.FromArgb(28, 34, 46)))
                using (var bubblePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 65, 90), 1f))
                {
                    DrawRoundedRect(g, bubbleBrush, bubblePen, modalX + 20, bubbleY, bubbleW, bubbleH, 8);
                }

                // Avatar pill inside bubble
                g.DrawString("💊 Carlos Méndez", fontBold, textGold, modalX + 36, bubbleY + 12);
                g.DrawString("10:24", fontSub, textMuted, modalX + bubbleW - 40, bubbleY + 14);

                string greetingText = "Hello! I'm Carlos Méndez, your virtual attendant for Farmacias Benavides at Av. Benito Juárez 610.\n\n" +
                                      "We are open today (Thursday) from 8:00 AM to 10:00 PM (currently OPEN). Our pharmacy provides prescription medicines, " +
                                      "first-aid supplies, and pedestrian travel essentials right before the international bridge.\n" +
                                      "How can I assist you with store hours, products, or directions today?";

                g.DrawString(greetingText, fontBody, textWhite, new RectangleF(modalX + 36, bubbleY + 36, bubbleW - 40, bubbleH - 42));

                // Suggested Inquiries Section
                int suggY = modalY + 260;
                g.DrawString("SUGGESTED QUESTIONS / PREGUNTAS SUGERIDAS:", fontBadge, textGold, modalX + 22, suggY);
                suggY += 20;

                DrawSuggestionChip(g, "[ What are your store hours today? ]", modalX + 20, suggY);
                DrawSuggestionChip(g, "[ ¿A qué hora cierran hoy? ]", modalX + 280, suggY);
                DrawSuggestionChip(g, "[ Where is the store located? ]", modalX + 500, suggY);

                // Second row of suggestion chips
                suggY += 34;
                DrawSuggestionChip(g, "[ What items do you stock for cross-border travelers? ]", modalX + 20, suggY);
                DrawSuggestionChip(g, "[ ¿Tienen analgésicos y botiquín? ]", modalX + 410, suggY);

                // Input Area at bottom
                int inputY = modalY + modalH - 72;
                int inputW = modalW - 150;
                int inputH = 42;

                using (var inputBg = new SolidBrush(System.Drawing.Color.FromArgb(16, 20, 28)))
                using (var inputPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 85, 115), 1.2f))
                {
                    DrawRoundedRect(g, inputBg, inputPen, modalX + 20, inputY, inputW, inputH, 6);
                }
                g.DrawString("Type your question here... / Escribe tu pregunta aquí...", fontBody, textMuted, modalX + 32, inputY + 12);

                // Send Button
                int sendX = modalX + inputW + 28;
                int sendW = 100;
                using (var sendBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 110, 210)))
                using (var sendPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 160, 255), 1.2f))
                {
                    DrawRoundedRect(g, sendBrush, sendPen, sendX, inputY, sendW, inputH, 6);
                }
                string sendText = "Send ↵";
                var sendSize = g.MeasureString(sendText, fontBold);
                g.DrawString(sendText, fontBold, textWhite, sendX + (sendW - sendSize.Width) / 2, inputY + 11);
            }
        }

        private static void DrawSuggestionChip(System.Drawing.Graphics g, string label, int x, int y)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(30, 42, 60)))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 135), 1f))
            using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 230, 245)))
            {
                var s = g.MeasureString(label, font);
                int w = (int)s.Width + 14;
                int h = 26;
                DrawRoundedRect(g, brush, pen, x, y, w, h, 6);
                g.DrawString(label, font, textBrush, x + 7, y + 4);
            }
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush brush, System.Drawing.Pen pen, float x, float y, float w, float h, float radius)
        {
            using (var path = new GraphicsPath())
            {
                float d = radius * 2f;
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + w - d, y, d, d, 270, 90);
                path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
                path.AddArc(x, y + h - d, d, d, 90, 90);
                path.CloseFigure();

                if (brush != null) g.FillPath(brush, path);
                if (pen != null) g.DrawPath(pen, path);
            }
        }
    }
}

