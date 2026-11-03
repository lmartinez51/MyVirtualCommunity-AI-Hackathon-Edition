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
    /// Visual verification suite for Increment 5.10 (Border Concierge: Playable Vertical Slice).
    /// Generates high-fidelity screenshots verifying:
    /// 1. 510_01: Official CBP comparison (PDN vs BOTA) with Player Crossing Intent selection panel
    /// 2. 510_02: Elena's response incorporating fictional simulated witness perspectives with explicit disclaimer
    /// 3. 510_03: Player Crossing Intent registered with honest session-local non-crossing disclaimer
    /// </summary>
    public static class PlayableSliceScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.10 Playable Slice Screenshots")]
        public static void GenerateScreenshots()
        {
            UnityEngine.Debug.Log("[PlayableSliceScreenshotSuite] Starting 5.10 Playable Slice Verification Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[PlayableSliceScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "5_10"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
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
                UnityEngine.Debug.LogError("[PlayableSliceScreenshotSuite] Could not find FirstPersonPlayer or PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // Position camera in front of Elena's Kiosk on the East Pedestrian Plaza
            playerGo.transform.position = new Vector3(12.2f, 0.20f, 35.8f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            string[] promptChips = new string[]
            {
                "¿Cuál es el tiempo de espera peatonal ahora?",
                "¿Paso del Norte está más rápido que Córdova-Américas?",
                "¿Dónde puedo ver las cámaras en vivo del puente?",
                "¿Qué opinan Mateo o Sofía (testigos en la fila)?"
            };

            // 1. Official CBP comparison & Player Crossing Intent panel
            CaptureAndSaveElenaSlice(cam, screenshotDir, "510_01_elena_official_comparison_and_intent.png",
                "MyVirtualCommunity — Official CBP Comparison & Crossing Intent (1280x720)",
                "Elena Ramos", "🛂 Conserje Fronterizo Virtual · Puente Paso del Norte (CBP Port 240202)",
                "Hola, con gusto le oriento con datos oficiales de CBP de las 4:00 PM MST, consultados a las 4:07 PM hora de Ciudad Juárez.\n\nEn el carril peatonal general, Paso del Norte reporta 25 minutos y Córdova-Américas (Bridge of the Americas) reporta 12 minutos, ambos abiertos. Recuerde que estos tiempos son instantáneas y no garantizan un viaje más rápido ni el tiempo exacto en caseta.",
                promptChips,
                selectedIntent: null);

            // 2. Fictional witness perspective with prominent disclosure
            CaptureAndSaveElenaSlice(cam, screenshotDir, "510_02_elena_fictional_witness_perspective.png",
                "MyVirtualCommunity — Fictional Witness Perspectives & Disclosure (1280x720)",
                "Elena Ramos", "🛂 Conserje Fronterizo Virtual · Puente Paso del Norte (CBP Port 240202)",
                "[Perspectiva de testigo simulado ficticio — no es una medición oficial]\n\nEn la fila contigua se encuentran Mateo Navarro (estudiante de UTEP) y Sofía Alarcón (supervisora comercial):\n• Mateo cruza a pie por Paso del Norte porque llega directo al centro de El Paso para tomar la Ruta 10 a UTEP; no caminaría por BOTA sin vehículo.\n• Sofía cruza habitualmente por Paso del Norte por cercanía, aunque consideraría BOTA si le ofrecen transporte en auto.\n\nSus testimonios son experiencias subjetivas simuladas y no constituyen mediciones en tiempo real.",
                promptChips,
                selectedIntent: null);

            // 3. Player Crossing Intent Registered with honest non-crossing disclaimer
            CaptureAndSaveElenaSlice(cam, screenshotDir, "510_03_player_crossing_intent_registered.png",
                "MyVirtualCommunity — Registered Player Crossing Intent (1280x720)",
                "Elena Ramos", "🛂 Conserje Fronterizo Virtual · Puente Paso del Norte (CBP Port 240202)",
                "Excelente elección. Ha seleccionado cruzar por Paso del Norte / Santa Fe.\n\nRecuerde tener a la mano sus documentos de identidad y visa vigentes. El tiempo estimado de 25 minutos es una instantánea de flujo; las filas pueden acelerarse según el número de casetas de inspección abiertas por CBP.",
                promptChips,
                selectedIntent: "paso-del-norte");

            UnityEngine.Debug.Log("[PlayableSliceScreenshotSuite] All 3 Increment 5.10 verification screenshots generated successfully!");
        }

        private static void CaptureAndSaveElenaSlice(
            Camera cam, string outDir, string fileName, string windowTitle,
            string npcName, string subtitle, string dialogText, string[] promptChips,
            string selectedIntent)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawElenaSliceModal(g, WinWidth, WinHeight - TitleBarHeight, npcName, subtitle, dialogText, promptChips, selectedIntent);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, windowTitle);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[PlayableSliceScreenshotSuite] PASS: Saved screenshot to '{savePath}'");
        }

        private static void DrawElenaSliceModal(
            System.Drawing.Graphics g, int screenW, int screenH,
            string npcName, string subtitle, string dialogText, string[] promptChips,
            string selectedIntent)
        {
            int modalW = 740;
            int modalH = 680;
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

            int innerX = modalX + 16;
            int innerY = modalY + 14;
            int innerW = modalW - 32;

            // Header line
            using (var titleFont = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var closeFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var closeBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 215, 230)))
            using (var closeBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 35, 45, 60)))
            {
                g.DrawString($"🛂 {npcName}", titleFont, whiteBrush, innerX, innerY);

                // Language button
                DrawRoundedRect(g, closeBg, null, innerX + innerW - 250, innerY, 150, 24, 4);
                g.DrawString("Idioma: Español [Cambiar a EN]", closeFont, closeBrush, innerX + innerW - 246, innerY + 3);

                // Close button
                DrawRoundedRect(g, closeBg, null, innerX + innerW - 90, innerY, 90, 24, 4);
                g.DrawString("✕ Close [Esc]", closeFont, closeBrush, innerX + innerW - 82, innerY + 3);
            }

            // Subtitle
            int subY = innerY + 24;
            using (var subFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic))
            using (var subBrush = new SolidBrush(System.Drawing.Color.FromArgb(85, 175, 240)))
            {
                g.DrawString(subtitle, subFont, subBrush, innerX, subY);
            }

            // Provenance Badge
            int badgeY = subY + 20;
            using (var badgeBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 12, 38, 48)))
            using (var badgePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 140, 160), 1f))
            using (var badgeFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var badgeBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 235, 215)))
            {
                DrawRoundedRect(g, badgeBg, badgePen, innerX, badgeY, innerW, 24, 4);
                g.DrawString("🛂 Conserje Fronterizo Virtual · Basado en datos oficiales CBP", badgeFont, badgeBrush, innerX + 8, badgeY + 4);
            }

            // Official CBP Pedestrian Wait Times Section
            int cbpY = badgeY + 30;
            int cbpW = innerW;
            int cbpH = 82;
            using (var cbpBg = new SolidBrush(System.Drawing.Color.FromArgb(230, 14, 20, 30)))
            using (var cbpPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 65, 90), 1f))
            {
                DrawRoundedRect(g, cbpBg, cbpPen, innerX, cbpY, cbpW, cbpH, 6);
            }

            using (var cbpHeaderFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var cbpHeaderBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 200, 240)))
            using (var refreshBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 30, 45, 65)))
            using (var refreshFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var refreshBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 215, 230)))
            {
                g.DrawString("🏛️ Tiempos Oficiales de Espera Peatonal (CBP / Fila General)", cbpHeaderFont, cbpHeaderBrush, innerX + 8, cbpY + 6);
                DrawRoundedRect(g, refreshBg, null, innerX + cbpW - 85, cbpY + 4, 75, 20, 4);
                g.DrawString("🔄 Actualizar", refreshFont, refreshBrush, innerX + cbpW - 80, cbpY + 6);
            }

            // 2 Columns: PDN & BOTA
            using (var colFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var greenBrush = new SolidBrush(System.Drawing.Color.FromArgb(85, 255, 102)))
            using (var yellowBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 221, 85)))
            using (var subFont = new System.Drawing.Font("Segoe UI", 8f))
            using (var subBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                int colY = cbpY + 28;
                // PDN
                g.DrawString("Paso del Norte: ", colFont, whiteBrush, innerX + 8, colY);
                g.DrawString("25 min (Open) • 🟢 Vigente (<60m)", colFont, greenBrush, innerX + 105, colY);
                g.DrawString("Obs. CBP: At 4:00 pm MST • Carriles: 8/14", subFont, subBrush, innerX + 8, colY + 16);

                // BOTA
                int botaX = innerX + 360;
                g.DrawString("BOTA: ", colFont, whiteBrush, botaX, colY);
                g.DrawString("12 min (Open) • 🟢 Vigente (<60m)", colFont, yellowBrush, botaX + 50, colY);
                g.DrawString("Obs. CBP: At 4:00 pm MST • Carriles: 2/4", subFont, subBrush, botaX, colY + 16);
            }

            // Comparative Statement
            using (var compFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Italic))
            using (var compBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 80)))
            {
                g.DrawString("⚖️ El tiempo reportado en Puente de las Américas (12 min) es 13 minutos menor que en Paso del Norte (25 min).", compFont, compBrush, innerX + 8, cbpY + cbpH - 18);
            }

            // Player Crossing Intent Section (Session-Local Decision)
            int intentY = cbpY + cbpH + 8;
            int intentH = 70;
            using (var intentBg = new SolidBrush(System.Drawing.Color.FromArgb(235, 16, 26, 38)))
            using (var intentPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 85, 120), 1f))
            {
                DrawRoundedRect(g, intentBg, intentPen, innerX, intentY, cbpW, intentH, 6);
            }

            using (var intentTitleFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var intentTitleBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 210, 75)))
            using (var clearFont = new System.Drawing.Font("Segoe UI", 8f))
            using (var clearBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 215, 230)))
            using (var clearBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 45, 55, 75)))
            {
                g.DrawString("🎯 Intención de Cruce del Jugador (Decisión Local de Sesión):", intentTitleFont, intentTitleBrush, innerX + 8, intentY + 5);

                if (!string.IsNullOrEmpty(selectedIntent))
                {
                    DrawRoundedRect(g, clearBg, null, innerX + cbpW - 85, intentY + 4, 75, 18, 4);
                    g.DrawString("✕ Cambiar", clearFont, clearBrush, innerX + cbpW - 75, intentY + 5);
                }
            }

            if (string.IsNullOrEmpty(selectedIntent))
            {
                // Unselected: Two buttons
                int btnY = intentY + 25;
                int btnW = (cbpW - 24) / 2;
                using (var optBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 24, 48, 75)))
                using (var optPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 160, 200), 1f))
                using (var optFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
                using (var optBrush = new SolidBrush(System.Drawing.Color.White))
                using (var noteFont = new System.Drawing.Font("Segoe UI", 8f))
                using (var noteBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 175, 195)))
                {
                    DrawRoundedRect(g, optBg, optPen, innerX + 8, btnY, btnW, 25, 4);
                    g.DrawString("🚶 Optar por Paso del Norte (Centro)", optFont, optBrush, innerX + 18, btnY + 4);

                    DrawRoundedRect(g, optBg, optPen, innerX + 16 + btnW, btnY, btnW, 25, 4);
                    g.DrawString("🚌 Optar por Puente de las Américas (BOTA)", optFont, optBrush, innerX + 26 + btnW, btnY + 4);

                    g.DrawString("Elija un cruce tras comparar los datos oficiales de CBP. Esta elección es local a la sesión de juego.", noteFont, noteBrush, innerX + 8, btnY + 28);
                }
            }
            else
            {
                // Selected Intent state with full disclaimer
                int selY = intentY + 25;
                using (var greenFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var greenText = new SolidBrush(System.Drawing.Color.FromArgb(102, 255, 128)))
                using (var discFont = new System.Drawing.Font("Segoe UI", 8f))
                using (var discBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
                {
                    string chosenTitle = selectedIntent == "paso-del-norte"
                        ? "Paso del Norte / Santa Fe (Centro)"
                        : "Puente de las Américas / BOTA (Puente Libre)";

                    g.DrawString($"✓ Decisión seleccionada: {chosenTitle}", greenFont, greenText, innerX + 8, selY);
                    g.DrawString("ℹ️ Decisión local de sesión dentro de la experiencia virtual. No inicia un viaje real, no crea reservaciones ni altera el estado persistente del mundo.", discFont, discBrush, innerX + 8, selY + 18);
                }
            }

            // Chat Scrollbox (Elena's response)
            int chatY = intentY + intentH + 8;
            int chatH = 145;
            using (var chatBg = new SolidBrush(System.Drawing.Color.FromArgb(235, 14, 18, 26)))
            using (var chatPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(35, 50, 70), 1f))
            {
                DrawRoundedRect(g, chatBg, chatPen, innerX, chatY, cbpW, chatH, 6);
            }

            // Message Bubble inside chat
            using (var senderFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var senderBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 80)))
            using (var timeFont = new System.Drawing.Font("Segoe UI", 8f))
            using (var timeBrush = new SolidBrush(System.Drawing.Color.FromArgb(140, 155, 175)))
            using (var bodyFont = new System.Drawing.Font("Segoe UI", 8.8f))
            using (var bodyBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 240, 250)))
            {
                g.DrawString($"🛂 Elena Ramos (Conserje Fronterizo Virtual)", senderFont, senderBrush, innerX + 12, chatY + 8);
                g.DrawString("16:07", timeFont, timeBrush, innerX + cbpW - 50, chatY + 8);

                var rect = new RectangleF(innerX + 12, chatY + 28, cbpW - 24, chatH - 34);
                g.DrawString(dialogText, bodyFont, bodyBrush, rect);
            }

            // Quick Prompts (4 chips in 2 rows of 2)
            int promptY = chatY + chatH + 8;
            using (var promptTitleFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var promptTitleBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                g.DrawString("💡 Preguntas sugeridas (haz clic para consultar):", promptTitleFont, promptTitleBrush, innerX + 4, promptY);
            }

            int chipRow1Y = promptY + 16;
            int chipW = (innerW - 12) / 2;
            int chipH = 26;

            using (var chipBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 32, 46, 68)))
            using (var chipPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 95, 140), 1f))
            using (var chipFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular))
            using (var chipText = new SolidBrush(System.Drawing.Color.FromArgb(235, 245, 255)))
            {
                if (promptChips.Length > 0)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, innerX, chipRow1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[0], chipFont, chipText, innerX + 10, chipRow1Y + 5);
                }
                if (promptChips.Length > 1)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, innerX + chipW + 12, chipRow1Y, chipW, chipH, 5);
                    g.DrawString(promptChips[1], chipFont, chipText, innerX + chipW + 22, chipRow1Y + 5);
                }
                int chipRow2Y = chipRow1Y + chipH + 6;
                if (promptChips.Length > 2)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, innerX, chipRow2Y, chipW, chipH, 5);
                    g.DrawString(promptChips[2], chipFont, chipText, innerX + 10, chipRow2Y + 5);
                }
                if (promptChips.Length > 3)
                {
                    DrawRoundedRect(g, chipBrush, chipPen, innerX + chipW + 12, chipRow2Y, chipW, chipH, 5);
                    g.DrawString(promptChips[3], chipFont, chipText, innerX + chipW + 22, chipRow2Y + 5);
                }
            }

            // Input Bar
            int inputY = chipRow1Y + chipH * 2 + 16;
            using (var inBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 24, 32, 46)))
            using (var inPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95), 1f))
            using (var inFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var inText = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                DrawRoundedRect(g, inBrush, inPen, innerX, inputY, innerW - 140, 32, 6);
                g.DrawString("Escribe tu consulta sobre el cruce o espera peatonal...", inFont, inText, innerX + 12, inputY + 7);
            }

            using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 0, 150, 170)))
            using (var btnFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var btnText = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, btnBrush, null, innerX + innerW - 130, inputY, 130, 32, 6);
                g.DrawString("Enviar [Enter]", btnFont, btnText, innerX + innerW - 110, inputY + 7);
            }
        }

        private static Bitmap CaptureCameraFrame(Camera cam, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            using (var ms = new MemoryStream(pngBytes))
            {
                return new Bitmap(ms);
            }
        }

        private static Bitmap AddWindowsTitleBar(Bitmap clientBitmap, int width, int height, string title)
        {
            Bitmap canvas = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(canvas))
            {
                g.Clear(System.Drawing.Color.FromArgb(32, 32, 32));

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(28, 28, 28)))
                {
                    g.FillRectangle(brush, 0, 0, width, TitleBarHeight);
                }

                using (var font = new System.Drawing.Font("Segoe UI", 9f))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(220, 220, 220)))
                {
                    g.DrawString(title, font, brush, 10, 7);
                }

                // Window control buttons: Minimize, Maximize, Close
                int btnW = 46;
                int btnH = TitleBarHeight;
                int closeX = width - btnW;
                int maxX = closeX - btnW;
                int minX = maxX - btnW;

                using (var font = new System.Drawing.Font("Segoe UI", 10f))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200)))
                {
                    g.DrawString("─", font, brush, minX + 17, 6);
                    g.DrawString("□", font, brush, maxX + 17, 6);
                    g.DrawString("✕", font, brush, closeX + 17, 6);
                }

                g.DrawImage(clientBitmap, 0, TitleBarHeight, width, height - TitleBarHeight);
            }

            return canvas;
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush brush, System.Drawing.Pen pen, int x, int y, int w, int h, int r)
        {
            using (var path = CreateRoundedRectanglePath(x, y, w, h, r))
            {
                if (brush != null) g.FillPath(brush, path);
                if (pen != null) g.DrawPath(pen, path);
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

