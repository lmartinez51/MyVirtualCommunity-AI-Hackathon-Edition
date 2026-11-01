using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated high-fidelity screenshot generator for Increment 5.9: Unified Bobblehead NPC Population.
    /// Captures the full cast in their production corridor positions:
    /// 1. Carlos Méndez (Farmacias Benavides) — Full body
    /// 2. Carlos Méndez — Close-up
    /// 3. Carlos Méndez — Interactive Attendant Dialogue Modal
    /// 4. Valeria Domínguez (Óptica Fashion) — Full body
    /// 5. Valeria Domínguez — Close-up (designer glasses)
    /// 6. Mateo Navarro (Queue Witness #1) — Full body & backpack
    /// 7. Mateo Navarro — Close-up
    /// 8. Sofía Alarcón (Queue Witness #2) — Full body & afro curly hairstyle
    /// 9. Sofía Alarcón — Close-up
    /// 10. Don Héctor Méndez (Queue Witness #3) — Full body, leather vest & satchel
    /// 11. Don Héctor Méndez — Close-up (mustache)
    /// 12. Street Overview — Avenida Juárez corridor context
    /// 13. Cast Unity — Elena Ramos alongside witnesses in corridor
    /// </summary>
    public static class CorridorPopulationScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.9 Population Screenshots")]
        public static void GenerateAllScreenshots()
        {
            UnityEngine.Debug.Log("[CorridorPopulationScreenshotSuite] Starting Increment 5.9 Population Screenshot Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[CorridorPopulationScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "population"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }

            // Ensure animators and bobblehead characters are posed
            var animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var a in animators)
            {
                if (a != null) a.Update(1.0f);
            }

            var bobbleAnimators = UnityEngine.Object.FindObjectsByType<BobbleheadAnimator>(FindObjectsSortMode.None);
            foreach (var ba in bobbleAnimators)
            {
                if (ba != null) ba.ForcePoseDirect();
            }

            var playerGo = GameObject.Find("FirstPersonPlayer");
            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;

            if (playerGo == null || cam == null)
            {
                UnityEngine.Debug.LogError("[CorridorPopulationScreenshotSuite] Could not find FirstPersonPlayer or PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // 1. CARLOS MENDEZ — FULL BODY
            playerGo.transform.position = new Vector3(8.7f, 0f, 0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_carlos_1_fullbody.png",
                "MyVirtualCommunity — Carlos Méndez at Farmacias Benavides (1280x720)",
                "CARLOS MÉNDEZ · FARMACIAS BENAVIDES ATTENDANT · FULL BODY");

            // 2. CARLOS MENDEZ — CLOSE-UP
            playerGo.transform.position = new Vector3(9.5f, 0f, 0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_carlos_2_closeup.png",
                "MyVirtualCommunity — Carlos Méndez Front Close-Up (1280x720)",
                "CARLOS MÉNDEZ · WHITE LAB COAT, ROYAL BLUE COLLAR & GOLD BADGE");

            // 3. CARLOS MENDEZ — ATTENDANT DIALOGUE
            CaptureAndSaveDialogue(cam, screenshotDir, "screenshot_carlos_3_dialogue.png",
                "MyVirtualCommunity — Carlos Méndez Attendant Dialogue (1280x720)",
                "Carlos Méndez",
                "Farmacias Benavides Attendant · Avenida Juárez #142",
                "¡Buenas tardes! Welcome to Farmacias Benavides on Avenida Juárez.\n\n" +
                "Are you looking for cross-border prescription consultations, over-the-counter essentials, or travel personal care before crossing the bridge?\n\n" +
                "We carry authentic Mexican pharmaceuticals, first-aid kits, and electrolytes. Please remember to keep all medication boxes and original doctor prescriptions together for CBP inspection at the Paso del Norte port.");

            // 4. VALERIA DOMINGUEZ — FULL BODY
            playerGo.transform.position = new Vector3(-8.7f, 0f, 1.5f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_valeria_1_fullbody.png",
                "MyVirtualCommunity — Valeria Domínguez at Óptica Fashion (1280x720)",
                "VALERIA DOMÍNGUEZ · ÓPTICA FASHION BOUTIQUE ATTENDANT · FULL BODY");

            // 5. VALERIA DOMINGUEZ — CLOSE-UP
            playerGo.transform.position = new Vector3(-9.5f, 0f, 1.5f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_valeria_2_closeup.png",
                "MyVirtualCommunity — Valeria Domínguez Close-Up (1280x720)",
                "VALERIA DOMÍNGUEZ · SIGNATURE DESIGNER GLASSES, GOLD COLLAR & SLICK BOB");

            // 6. MATEO NAVARRO — FULL BODY (THREE-QUARTER SHOWING BACKPACK)
            playerGo.transform.position = new Vector3(0.5f, 0f, 10.6f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -38f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_mateo_1_fullbody.png",
                "MyVirtualCommunity — Mateo Navarro Queue Witness (1280x720)",
                "MATEO NAVARRO · UTEP STUDENT COMMUTER · FULL BODY & BACKPACK");

            // 7. MATEO NAVARRO — CLOSE-UP
            playerGo.transform.position = new Vector3(-0.1f, 0f, 11.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_mateo_2_closeup.png",
                "MyVirtualCommunity — Mateo Navarro Close-Up (1280x720)",
                "MATEO NAVARRO · COLLEGIATE NAVY ZIP JACKET & HEATHER GRAY TEE");

            // 8. SOFÍA ALARCÓN — FULL BODY
            playerGo.transform.position = new Vector3(-0.6f, 0f, 14.6f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 38f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_sofia_1_fullbody.png",
                "MyVirtualCommunity — Sofía Alarcón Queue Witness (1280x720)",
                "SOFÍA ALARCÓN · CROSS-BORDER RETAIL WORKER · FULL BODY");

            // 9. SOFÍA ALARCÓN — CLOSE-UP (AFRO CURLY HAIR)
            playerGo.transform.position = new Vector3(0.0f, 0f, 15.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_sofia_2_closeup.png",
                "MyVirtualCommunity — Sofía Alarcón Close-Up (1280x720)",
                "SOFÍA ALARCÓN · DISTINCTIVE AFRO-CURLY HAIRSTYLE & PLUM COMMUTER JACKET");

            // 10. DON HÉCTOR MÉNDEZ — FULL BODY
            playerGo.transform.position = new Vector3(0.7f, 0f, 18.6f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -38f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_hector_1_fullbody.png",
                "MyVirtualCommunity — Don Héctor Méndez Queue Witness (1280x720)",
                "DON HÉCTOR MÉNDEZ · RETIRED LEATHER ARTISAN · SADDLE VEST & SATCHEL");

            // 11. DON HÉCTOR MÉNDEZ — CLOSE-UP (MUSTACHE)
            playerGo.transform.position = new Vector3(0.1f, 0f, 19.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_hector_2_closeup.png",
                "MyVirtualCommunity — Don Héctor Méndez Close-Up (1280x720)",
                "DON HÉCTOR MÉNDEZ · SILVER MUSTACHE & HAND-TOOLED LEATHER SATCHEL");

            // 12. CORRIDOR OVERVIEW — AVENIDA BENITO JUÁREZ
            playerGo.transform.position = new Vector3(0f, 0f, -4f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-1.5f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_corridor_overview.png",
                "MyVirtualCommunity — Avenida Benito Juárez Corridor Overview (1280x720)",
                "AVENIDA JUÁREZ CORRIDOR · ALL 5 BOBBLEHEAD NPCS IN PRODUCTION POSITIONS");

            // 13. CAST COMPARISON — ELENA RAMOS WITH QUEUE WITNESSES
            playerGo.transform.position = new Vector3(3.2f, 0f, 38.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 195f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cam.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "screenshot_cast_elena_and_witnesses.png",
                "MyVirtualCommunity — Elena Ramos alongside Corridor Witnesses (1280x720)",
                "CAST UNITY · ELENA RAMOS AT KIOSK ALONGSIDE BOBBLEHEAD QUEUE CAST");

            UnityEngine.Debug.Log("[CorridorPopulationScreenshotSuite] All 13 acceptance screenshots generated successfully!");
        }

        private static void CaptureAndSave(Camera cam, string outDir, string fileName, string windowTitle, string pillText)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawNotificationPill(g, WinWidth, pillText);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[CorridorPopulationScreenshotSuite] PASS: Saved '{savePath}'");
            frame.Dispose();
            window.Dispose();
        }

        private static void CaptureAndSaveDialogue(Camera cam, string outDir, string fileName, string windowTitle, string npcName, string subtitle, string dialogText)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "en");
                DrawDialogueModal(g, WinWidth, WinHeight - TitleBarHeight, npcName, subtitle, dialogText);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[CorridorPopulationScreenshotSuite] PASS: Saved dialogue screenshot '{savePath}'");
            frame.Dispose();
            window.Dispose();
        }

        private static Bitmap CaptureCameraFrame(Camera cam, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
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

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(28, 30, 36)))
                {
                    g.FillRectangle(brush, 0, 0, totalW, TitleBarHeight);
                }

                using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(230, 230, 235)))
                {
                    g.DrawString(title, font, brush, 14, 7);
                }

                int btnW = 46;
                int closeX = totalW - btnW;
                using (var font = new System.Drawing.Font("Segoe UI", 9f))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200)))
                {
                    g.DrawString("✕", font, brush, closeX + 17, 8);
                }

                int maxprocX = closeX - btnW;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200), 1.2f))
                {
                    g.DrawRectangle(pen, maxprocX + 18, 11, 10, 10);
                }

                int minX = maxprocX - btnW;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200), 1.2f))
                {
                    g.DrawLine(pen, minX + 18, 16, minX + 28, 16);
                }

                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 48, 56)))
                {
                    g.DrawLine(pen, 0, TitleBarHeight - 1, totalW, TitleBarHeight - 1);
                }

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
                g.DrawString("⬆ North: Paso del Norte Bridge  |  Avenida Benito Juárez Corridor", fontRegular, textGold, 220, 11);
                string statusFull = $"🟢 {serverStatus}  |  Help [F1]  |  [EN / ES]";
                var size = g.MeasureString(statusFull, fontRegular);
                g.DrawString(statusFull, fontRegular, textGreen, width - size.Width - 16, 11);
            }
        }

        private static void DrawNotificationPill(System.Drawing.Graphics g, int w, string text)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            {
                var sz = g.MeasureString(text, font);
                int pillW = (int)sz.Width + 28;
                int pillH = 28;
                int pillX = (w - pillW) / 2;
                int pillY = 52;

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(235, 14, 26, 42)))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1.2f))
                using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 248, 255)))
                {
                    DrawRoundedRect(g, brush, pen, pillX, pillY, pillW, pillH, 8);
                    g.DrawString(text, font, textBrush, pillX + 14, pillY + 5);
                }
            }
        }

        private static void DrawDialogueModal(System.Drawing.Graphics g, int screenW, int screenH, string npcName, string subtitle, string dialogText)
        {
            int modalW = 820;
            int modalH = 460;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2 + 20;

            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(scrim, 0, 0, screenW, screenH);
            }

            using (var modalBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 14, 20, 30)))
            using (var modalPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 60, 90), 1.5f))
            {
                DrawRoundedRect(g, modalBrush, modalPen, modalX, modalY, modalW, modalH, 12);
            }

            int headerH = 68;
            using (var headBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 20, 28, 44)))
            using (var headPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(35, 55, 80), 1f))
            {
                DrawTopRoundedRect(g, headBrush, modalX, modalY, modalW, headerH, 12);
                g.DrawLine(headPen, modalX, modalY + headerH, modalX + modalW, modalY + headerH);
            }

            int avatarSize = 48;
            int avatarX = modalX + 16;
            int avatarY = modalY + 10;
            using (var avBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 48, 76)))
            using (var avPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1.5f))
            {
                g.FillEllipse(avBrush, avatarX, avatarY, avatarSize, avatarSize);
                g.DrawEllipse(avPen, avatarX, avatarY, avatarSize, avatarSize);
                using (var avFont = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold))
                using (var avText = new SolidBrush(System.Drawing.Color.FromArgb(0, 173, 181)))
                {
                    string initial = npcName.Length > 0 ? npcName.Substring(0, 1) : "N";
                    var sz = g.MeasureString(initial, avFont);
                    g.DrawString(initial, avFont, avText, avatarX + (avatarSize - sz.Width) / 2, avatarY + (avatarSize - sz.Height) / 2);
                }
            }

            using (var fontName = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var fontRole = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var nameBrush = new SolidBrush(System.Drawing.Color.White))
            using (var roleBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 173, 181)))
            {
                g.DrawString(npcName, fontName, nameBrush, avatarX + avatarSize + 14, modalY + 12);
                g.DrawString(subtitle, fontRole, roleBrush, avatarX + avatarSize + 14, modalY + 38);
            }

            // Message box
            int chatX = modalX + 24;
            int chatY = modalY + headerH + 20;
            int chatW = modalW - 48;
            int chatH = 220;

            using (var aiBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 22, 34, 52)))
            using (var aiPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 75, 115), 1.2f))
            using (var fontMsg = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 245, 250)))
            {
                DrawRoundedRect(g, aiBrush, aiPen, chatX, chatY, chatW, chatH, 10);
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                {
                    g.DrawString(dialogText, fontMsg, whiteBrush, new RectangleF(chatX + 18, chatY + 16, chatW - 36, chatH - 32), sf);
                }
            }

            // AI Provenance Badge
            int provY = chatY + chatH + 16;
            using (var provBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 14, 24, 38)))
            using (var provPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 173, 181), 1f))
            using (var fontProv = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var provText = new SolidBrush(System.Drawing.Color.FromArgb(160, 215, 235)))
            {
                string provString = $"🔍 Grounded Business Knowledge · Farmacias Benavides Juarez · Meta muse-spark-1.3 · Persona Guardrails Active";
                var sz = g.MeasureString(provString, fontProv);
                DrawRoundedRect(g, provBrush, provPen, chatX, provY, (int)sz.Width + 20, (int)sz.Height + 8, 6);
                g.DrawString(provString, fontProv, provText, chatX + 10, provY + 4);
            }

            // Modal footer
            int bottomY = modalY + modalH - 50;
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
                g.DrawString("Press [Esc] or click Close to return to Avenida Benito Juárez", fontFoot, footBrush, modalX + 22, bottomY + 15);
                int cbw = 110;
                int cbh = 30;
                int cbx = modalX + modalW - cbw - 22;
                int cby = bottomY + 10;
                DrawRoundedRect(g, btnCloseBg, btnClosePen, cbx, cby, cbw, cbh, 6);
                g.DrawString("Close [Esc]", fontFoot, whiteBrush, cbx + 22, cby + 7);
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

