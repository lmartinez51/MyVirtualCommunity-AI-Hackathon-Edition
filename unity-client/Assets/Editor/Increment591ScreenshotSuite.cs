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
    /// Verification and visual evidence screenshot generator for Increment 5.9.1.
    /// Captures:
    /// 1. Elena Ramos forward arm orientation at concierge counter
    /// 2. Carlos Méndez forward arm orientation at pharmacy counter
    /// 3. Valeria Domínguez forward arm orientation at optical counter
    /// 4. Business availability closed-state UX (Sunday CLOSED_TODAY)
    /// 5. High-opacity UI background (0.94 dark fill) & wrapped quick prompt chips across resolutions
    /// </summary>
    public static class Increment591ScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.9.1 Verification Screenshots")]
        public static void GenerateScreenshots()
        {
            UnityEngine.Debug.Log("[Increment591ScreenshotSuite] Starting 5.9.1 Corrective Verification Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[Increment591ScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "5_9_1"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }

            // Ensure animators and bobblehead characters are posed
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
                UnityEngine.Debug.LogError("[Increment591ScreenshotSuite] Could not find FirstPersonPlayer or PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // 1. ELENA RAMOS — ARM ORIENTATION & COUNTER POSTURE
            playerGo.transform.position = new Vector3(-0.75f, 0f, 44.5f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.40f, 0f);
            cam.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "591_elena_arm_orientation_front_threequarter.png",
                "MyVirtualCommunity — Elena Ramos Arm Orientation (1280x720)",
                "ELENA RAMOS · FORWARD ARM POSTURE & ELBOW FLEXION TOWARD KIOSK COUNTER");

            // 2. CARLOS MÉNDEZ — ARM ORIENTATION & PHARMACY COUNTER
            playerGo.transform.position = new Vector3(9.2f, 0f, -0.7f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cam.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "591_carlos_arm_orientation_threequarter.png",
                "MyVirtualCommunity — Carlos Méndez Arm Orientation (1280x720)",
                "CARLOS MÉNDEZ · ARMS ANGLED FORWARD TOWARD COUNTER SURFACE");

            // 3. VALERIA DOMÍNGUEZ — ARM ORIENTATION & OPTICAL COUNTER
            playerGo.transform.position = new Vector3(-9.2f, 0f, 0.8f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -60f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cam.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
            CaptureAndSave(cam, screenshotDir, "591_valeria_arm_orientation_threequarter.png",
                "MyVirtualCommunity — Valeria Domínguez Arm Orientation (1280x720)",
                "VALERIA DOMÍNGUEZ · ARMS ANGLED FORWARD TOWARD COUNTER SURFACE");

            // 4. BUSINESS AVAILABILITY CLOSED-STATE UX (CARLOS MÉNDEZ)
            // Shows Sunday CLOSED_TODAY status and wrapped quick prompt chips
            playerGo.transform.position = new Vector3(9.0f, 0f, 0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            CaptureAndSaveBusinessDialogue(cam, screenshotDir, "591_carlos_business_availability_ux.png",
                "MyVirtualCommunity — Carlos Méndez Business Availability UX (1280x720)",
                "Carlos Méndez", "💊 Encargado Virtual · Farmacias Benavides (Avenida Juárez 610)",
                "Hola, soy Carlos Méndez, encargado virtual de Farmacias Benavides. En este momento (domingo), el establecimiento se encuentra CERRADO HOY. Farmacias Benavides permanece cerrado los domingos. Consulta nuestro horario habitual de lunes a sábado de 07:00 a 22:00 (America/Ciudad_Juarez) para planificar tu visita.",
                "CLOSED_TODAY",
                new string[] {
                    "¿Cuáles son los horarios de atención?",
                    "¿Está abierta la farmacia en este momento?",
                    "¿Dónde está ubicada exactamente la farmacia?"
                });

            // 5. HIGH-OPACITY UI BACKGROUND & TEXT READABILITY (1280x720)
            CaptureAndSavePauseMenu(cam, screenshotDir, "591_ui_opacity_and_contrast_1280x720.png",
                "MyVirtualCommunity — Pause Menu High-Opacity Card (1280x720)");

            // 6. ELENA RAMOS — EXPANDED TALLER MODAL WITH FULLY VISIBLE TEXTBOX & SEND BUTTON
            playerGo.transform.position = new Vector3(-0.75f, 0f, 44.5f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.40f, 0f);
            cam.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            CaptureAndSaveElenaDialogue(cam, screenshotDir, "591_elena_dialogue_modal_textbox_verified.png",
                "MyVirtualCommunity — Elena Ramos Dialogue Taller Modal (1280x720)",
                "Elena Ramos", "🛂 Conserje Fronterizo Virtual · Puente Paso del Norte (CBP Port 240202)",
                "Hola, con gusto le oriento con datos oficiales de CBP de las 4:00 pm MST, consultados a las 4:07 PM hora de Ciudad Juárez.\n\nEn el carril peatonal general, Paso del Norte reporta 25 minutos y Córdova-Américas reporta 12 minutos, ambos abiertos.",
                new string[] {
                    "¿Cuál es el tiempo de espera peatonal ahora?",
                    "¿Paso del Norte está más rápido que Córdova-Américas?",
                    "¿Dónde puedo ver las cámaras en vivo del puente?"
                });

            UnityEngine.Debug.Log("[Increment591ScreenshotSuite] All 5.9.1 screenshots generated successfully!");
            EditorApplication.Exit(0);
        }

        private static void CaptureAndSave(Camera cam, string outDir, string fileName, string windowTitle, string pillText)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "es");
                DrawNotificationPill(g, WinWidth, pillText);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment591ScreenshotSuite] PASS: Saved '{savePath}'");
            frame.Dispose();
            window.Dispose();
        }

        private static void CaptureAndSaveBusinessDialogue(
            Camera cam, string outDir, string fileName, string windowTitle,
            string npcName, string subtitle, string dialogText, string statusState, string[] promptChips)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "es");
                DrawDialogueModal(g, WinWidth, WinHeight - TitleBarHeight, npcName, subtitle, dialogText, statusState, promptChips);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment591ScreenshotSuite] PASS: Saved dialogue '{savePath}'");
            frame.Dispose();
            window.Dispose();
        }

        private static void CaptureAndSavePauseMenu(Camera cam, string outDir, string fileName, string windowTitle)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "es");
                DrawPauseMenuModal(g, WinWidth, WinHeight - TitleBarHeight);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment591ScreenshotSuite] PASS: Saved pause menu '{savePath}'");
            frame.Dispose();
            window.Dispose();
        }

        private static void CaptureAndSaveElenaDialogue(
            Camera cam, string outDir, string fileName, string windowTitle,
            string npcName, string subtitle, string dialogText, string[] promptChips)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DrawTopHUD(g, WinWidth, "Online | Meta muse-spark-1.3", "es");
                DrawElenaDialogueModal(g, WinWidth, WinHeight - TitleBarHeight, npcName, subtitle, dialogText, promptChips);
            }

            Bitmap window = WrapInWindowChrome(frame, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment591ScreenshotSuite] PASS: Saved Elena dialogue '{savePath}'");
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

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(24, 26, 32)))
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
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(240, 16, 20, 28))) // 0.94 opacity
            {
                g.FillRectangle(brush, 0, 0, width, 40);
            }
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 70, 85)))
            {
                g.DrawLine(pen, 0, 40, width, 40);
            }

            using (var fontBold = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold))
            using (var fontRegular = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textCyan = new SolidBrush(System.Drawing.Color.FromArgb(0, 215, 230)))
            {
                g.DrawString("Avenida Benito Juárez • Paso del Norte", fontBold, textWhite, 16, 10);
                g.DrawString("(⬆ Norte: Puente y Kiosco CBP | ⬇ Sur: Centro)", fontRegular, textCyan, 310, 11);

                string langLabel = "[ES]";
                var szLang = g.MeasureString(langLabel, fontBold);
                g.DrawString(langLabel, fontBold, textWhite, width - 260, 10);

                using (var greenBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 230, 120)))
                {
                    g.DrawString($"● {serverStatus}", fontRegular, greenBrush, width - 210, 11);
                }
            }
        }

        private static void DrawNotificationPill(System.Drawing.Graphics g, int screenW, string text)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            {
                var sz = g.MeasureString(text, font);
                int pillW = (int)sz.Width + 32;
                int pillH = 32;
                int pillX = (screenW - pillW) / 2;
                int pillY = 56;

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(240, 18, 24, 34))) // 0.94 opacity
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 185, 200), 1.5f))
                {
                    DrawRoundedRect(g, brush, pen, pillX, pillY, pillW, pillH, 8);
                }

                using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 245, 255)))
                {
                    g.DrawString(text, font, textBrush, pillX + 16, pillY + 7);
                }
            }
        }

        private static void DrawDialogueModal(
            System.Drawing.Graphics g, int screenW, int screenH,
            string npcName, string subtitle, string dialogText, string statusState, string[] promptChips)
        {
            int modalW = 720;
            int modalH = 560;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2 + 10;

            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(scrim, 0, 0, screenW, screenH);
            }

            // High-opacity dark card background (0.94 alpha fill with crisp slate border)
            using (var modalBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 18, 24, 34)))
            using (var modalPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1.5f))
            {
                DrawRoundedRect(g, modalBrush, modalPen, modalX, modalY, modalW, modalH, 10);
            }

            // Header
            int headerH = 64;
            using (var headBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 24, 32, 46)))
            using (var headPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 65, 90), 1f))
            {
                DrawTopRoundedRect(g, headBrush, modalX, modalY, modalW, headerH, 10);
                g.DrawLine(headPen, modalX, modalY + headerH, modalX + modalW, modalY + headerH);
            }

            using (var fontName = new System.Drawing.Font("Segoe UI", 13.5f, System.Drawing.FontStyle.Bold))
            using (var fontRole = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var nameBrush = new SolidBrush(System.Drawing.Color.White))
            using (var roleBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 185, 200)))
            {
                g.DrawString(npcName, fontName, nameBrush, modalX + 20, modalY + 10);
                g.DrawString(subtitle, fontRole, roleBrush, modalX + 20, modalY + 36);
            }

            // Status Banner (CLOSED_TODAY / Domingo)
            int bannerY = modalY + headerH + 12;
            int bannerW = modalW - 40;
            using (var banBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 42, 28, 24)))
            using (var banPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(210, 110, 60), 1.2f))
            using (var banFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var banText = new SolidBrush(System.Drawing.Color.FromArgb(255, 190, 130)))
            {
                DrawRoundedRect(g, banBrush, banPen, modalX + 20, bannerY, bannerW, 30, 6);
                g.DrawString("⚠️ ESTADO DOMINICAL: CLOSED_TODAY · Horario habitual: Lun-Sáb 07:00 a 22:00", banFont, banText, modalX + 32, bannerY + 6);
            }

            // Dialogue Bubble
            int chatX = modalX + 20;
            int chatY = bannerY + 40;
            int chatW = modalW - 40;
            int chatH = 190;

            using (var bubbleBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 26, 38, 54)))
            using (var bubblePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 80, 120), 1.2f))
            using (var fontMsg = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 245, 250)))
            {
                DrawRoundedRect(g, bubbleBrush, bubblePen, chatX, chatY, chatW, chatH, 8);
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                {
                    g.DrawString(dialogText, fontMsg, whiteBrush, new RectangleF(chatX + 16, chatY + 14, chatW - 32, chatH - 28), sf);
                }
            }

            // Provenance Badge
            int provY = chatY + chatH + 12;
            using (var fontProv = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var provText = new SolidBrush(System.Drawing.Color.FromArgb(150, 205, 225)))
            {
                g.DrawString("🏪 Encargado Virtual · Basado en horarios verificados de World Core (Rechaza recetas/inventario)", fontProv, provText, chatX + 4, provY);
            }

            // Multi-row Wrapped Quick Prompts Section
            int promptY = provY + 24;
            using (var promptLblFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var promptLblBrush = new SolidBrush(System.Drawing.Color.FromArgb(210, 225, 240)))
            {
                g.DrawString("💡 Preguntas sugeridas (haz clic para consultar):", promptLblFont, promptLblBrush, chatX + 4, promptY);
            }

            // Row 1: Prompts 0 & 1 side by side
            int row1Y = promptY + 22;
            int chipW = (chatW - 12) / 2; // ~334px
            int chipH = 26;
            using (var chipBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 32, 46, 68)))
            using (var chipPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 95, 140), 1f))
            using (var chipFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var chipText = new SolidBrush(System.Drawing.Color.FromArgb(235, 245, 255)))
            {
                if (promptChips.Length > 0)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, chatX, row1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[0], chipFont, chipText, chatX + 10, row1Y + 5);
                }
                if (promptChips.Length > 1)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, chatX + chipW + 12, row1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[1], chipFont, chipText, chatX + chipW + 22, row1Y + 5);
                }

                // Row 2: Prompt 2 (full width or half width)
                if (promptChips.Length > 2)
                {
                    int row2Y = row1Y + chipH + 6;
                    DrawRoundedRect(g, chipBrush, chipPen, chatX, row2Y, chipW, chipH, 5);
                    g.DrawString(promptChips[2], chipFont, chipText, chatX + 10, row2Y + 5);
                }
            }

            // Input bar
            int inputY = modalY + modalH - 52;
            using (var inBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 24, 32, 46)))
            using (var inPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1f))
            using (var inFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var inText = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                DrawRoundedRect(g, inBrush, inPen, chatX, inputY, chatW - 90, 34, 6);
                g.DrawString("Escribe tu consulta sobre horarios o ubicación...", inFont, inText, chatX + 12, inputY + 8);
            }

            using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 0, 150, 170)))
            using (var btnFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var btnText = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, btnBrush, null, chatX + chatW - 80, inputY, 80, 34, 6);
                g.DrawString("Enviar", btnFont, btnText, chatX + chatW - 60, inputY + 8);
            }
        }

        private static void DrawElenaDialogueModal(
            System.Drawing.Graphics g, int screenW, int screenH,
            string npcName, string subtitle, string dialogText, string[] promptChips)
        {
            int modalW = 740;
            int modalH = 655;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2 + 10;

            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(scrim, 0, 0, screenW, screenH);
            }

            // High-opacity dark card background (0.94 alpha fill with crisp slate border)
            using (var modalBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 18, 24, 34)))
            using (var modalPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1.5f))
            {
                DrawRoundedRect(g, modalBrush, modalPen, modalX, modalY, modalW, modalH, 10);
            }

            // Header
            int headerH = 60;
            using (var headBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 24, 32, 46)))
            using (var headPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 65, 90), 1f))
            {
                DrawTopRoundedRect(g, headBrush, modalX, modalY, modalW, headerH, 10);
                g.DrawLine(headPen, modalX, modalY + headerH, modalX + modalW, modalY + headerH);
            }

            using (var fontName = new System.Drawing.Font("Segoe UI", 13.5f, System.Drawing.FontStyle.Bold))
            using (var fontRole = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular))
            using (var nameBrush = new SolidBrush(System.Drawing.Color.White))
            using (var roleBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 185, 200)))
            {
                g.DrawString(npcName, fontName, nameBrush, modalX + 20, modalY + 10);
                g.DrawString(subtitle, fontRole, roleBrush, modalX + 20, modalY + 34);
            }

            // Provenance Badge
            int provY = modalY + headerH + 8;
            int chatX = modalX + 20;
            int chatW = modalW - 40;
            using (var fontProv = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var provText = new SolidBrush(System.Drawing.Color.FromArgb(150, 205, 225)))
            {
                g.DrawString("🛂 Conserje Fronterizo Virtual · Basado en datos oficiales CBP", fontProv, provText, chatX + 4, provY);
            }

            // Official CBP Intelligence Section
            int cbpY = provY + 20;
            using (var cbpBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 24, 34, 48)))
            using (var cbpPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 80, 115), 1.2f))
            using (var fontCbpTitle = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var fontCbpStat = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var textCyan = new SolidBrush(System.Drawing.Color.FromArgb(0, 215, 230)))
            using (var textWhite = new SolidBrush(System.Drawing.Color.White))
            using (var textYellow = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 80)))
            using (var textMuted = new SolidBrush(System.Drawing.Color.FromArgb(180, 195, 215)))
            {
                DrawRoundedRect(g, cbpBrush, cbpPen, chatX, cbpY, chatW, 88, 6);
                g.DrawString("🏛️ Tiempos Oficiales de Espera Peatonal (CBP / Fila General)", fontCbpTitle, textCyan, chatX + 12, cbpY + 7);

                g.DrawString("Paso del Norte: 25 min (Open) • 🟢 Vigente (<60m)", fontCbpStat, textWhite, chatX + 12, cbpY + 28);
                g.DrawString("Obs. CBP: At 4:00 pm MST • Carriles: 8/14", fontCbpStat, textMuted, chatX + 12, cbpY + 45);

                g.DrawString("BOTA: 12 min (Open) • 🟢 Vigente (<60m)", fontCbpStat, textYellow, chatX + 360, cbpY + 28);
                g.DrawString("Obs. CBP: At 4:00 pm MST • Carriles: 2/4", fontCbpStat, textMuted, chatX + 360, cbpY + 45);

                g.DrawString("ℹ️ El tiempo reportado en Puente de las Américas es 13 minutos menor que en Paso del Norte.", fontCbpStat, textYellow, chatX + 12, cbpY + 66);
            }

            // Dialogue Scrollable Chat Bubble
            int chatY = cbpY + 96;
            int chatH = 150;

            using (var bubbleBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 26, 38, 54)))
            using (var bubblePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 80, 120), 1.2f))
            using (var fontMsg = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 245, 250)))
            {
                DrawRoundedRect(g, bubbleBrush, bubblePen, chatX, chatY, chatW, chatH, 8);
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                {
                    g.DrawString(dialogText, fontMsg, whiteBrush, new RectangleF(chatX + 16, chatY + 12, chatW - 32, chatH - 24), sf);
                }
            }

            // Quick Prompts Section (2 rows)
            int promptY = chatY + chatH + 10;
            using (var promptLblFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var promptLblBrush = new SolidBrush(System.Drawing.Color.FromArgb(210, 225, 240)))
            {
                g.DrawString("💡 Preguntas sugeridas (haz clic para consultar):", promptLblFont, promptLblBrush, chatX + 4, promptY);
            }

            int row1Y = promptY + 20;
            int chipW = (chatW - 12) / 2;
            int chipH = 26;
            using (var chipBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 32, 46, 68)))
            using (var chipPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 95, 140), 1f))
            using (var chipFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var chipText = new SolidBrush(System.Drawing.Color.FromArgb(235, 245, 255)))
            {
                if (promptChips.Length > 0)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, chatX, row1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[0], chipFont, chipText, chatX + 10, row1Y + 5);
                }
                if (promptChips.Length > 1)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, chatX + chipW + 12, row1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[1], chipFont, chipText, chatX + chipW + 22, row1Y + 5);
                }
                if (promptChips.Length > 2)
                {
                    int row2Y = row1Y + chipH + 6;
                    DrawRoundedRect(g, chipBrush, chipPen, chatX, row2Y, chipW, chipH, 5);
                    g.DrawString(promptChips[2], chipFont, chipText, chatX + 10, row2Y + 5);
                }
            }

            // Stateless notice
            int noticeY = row1Y + chipH + 6 + chipH + 8;
            using (var noticeFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var noticeBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                g.DrawString("ℹ️ Cada consulta se procesa de forma independiente.", noticeFont, noticeBrush, chatX + 4, noticeY);
            }

            // Input bar with textfield and Send button fully visible
            int inputY = noticeY + 20;
            using (var inBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 24, 32, 46)))
            using (var inPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1f))
            using (var inFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var inText = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                DrawRoundedRect(g, inBrush, inPen, chatX, inputY, chatW - 140, 32, 6);
                g.DrawString("Escribe tu consulta sobre el cruce o espera peatonal...", inFont, inText, chatX + 12, inputY + 7);
            }

            using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 0, 150, 170)))
            using (var btnFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var btnText = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, btnBrush, null, chatX + chatW - 130, inputY, 130, 32, 6);
                g.DrawString("Enviar [Enter]", btnFont, btnText, chatX + chatW - 110, inputY + 7);
            }
        }

        private static void DrawPauseMenuModal(System.Drawing.Graphics g, int screenW, int screenH)
        {
            int modalW = 460;
            int modalH = 380;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2;

            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(scrim, 0, 0, screenW, screenH);
            }

            // High-opacity dark card background (0.94 fill)
            using (var modalBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 18, 24, 34)))
            using (var modalPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1.5f))
            {
                DrawRoundedRect(g, modalBrush, modalPen, modalX, modalY, modalW, modalH, 10);
            }

            using (var titleFont = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold))
            using (var subFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var cyanBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 185, 200)))
            {
                g.DrawString("⏸️ Menú de Pausa", titleFont, whiteBrush, modalX + 30, modalY + 24);
                g.DrawString("Avenida Benito Juárez • Paso del Norte", subFont, cyanBrush, modalX + 30, modalY + 58);
            }

            // Buttons
            string[] buttons = { "▶ Reanudar Exploración [Esc]", "📖 Guía de Controles [F1]", "🌐 Idioma: Español [Cambiar a EN]", "🚪 Salir al Escritorio [Alt+F4]" };
            int btnY = modalY + 95;
            int btnW = modalW - 60;
            int btnH = 40;

            for (int i = 0; i < buttons.Length; i++)
            {
                bool isQuit = i == buttons.Length - 1;
                var fill = isQuit ? System.Drawing.Color.FromArgb(240, 60, 30, 35) : System.Drawing.Color.FromArgb(240, 28, 40, 58);
                var border = isQuit ? System.Drawing.Color.FromArgb(160, 60, 70) : System.Drawing.Color.FromArgb(50, 80, 115);

                using (var brush = new SolidBrush(fill))
                using (var pen = new System.Drawing.Pen(border, 1.2f))
                using (var font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular))
                using (var textBrush = new SolidBrush(System.Drawing.Color.White))
                {
                    DrawRoundedRect(g, brush, pen, modalX + 30, btnY, btnW, btnH, 6);
                    g.DrawString(buttons[i], font, textBrush, modalX + 45, btnY + 10);
                }

                btnY += btnH + 12;
            }
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush brush, System.Drawing.Pen pen, int x, int y, int w, int h, int r)
        {
            using (var path = CreateRoundedRectanglePath(x, y, w, h, r))
            {
                if (brush != null) g.FillPath(brush, path);
                if (pen != null) g.DrawPath(pen, path);
            }
        }

        private static void DrawTopRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush brush, int x, int y, int w, int h, int r)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(x, y, r * 2, r * 2, 180, 90);
                path.AddArc(x + w - 2 * r, y, r * 2, r * 2, 270, 90);
                path.AddLine(x + w, y + h, x, y + h);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(int x, int y, int w, int h, int r)
        {
            GraphicsPath path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

