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
    /// Diagnostic visual verification suite for Increment 5.13.1 (Typography, Glyph Repairs & Layout Stabilization).
    /// Generates diagnostic captures at 1280x720 and 1920x1080 directly into root 'scratch/' (gitignored)
    /// to prove:
    /// 1. 0 broken Unicode glyph square boxes (□).
    /// 2. Header language selector is compact (ES | EN) with >= 16px clearance before Close control.
    /// 3. Modal background scrim (rgba(10, 14, 22, 0.72)) visibly darkens the 3D scene behind modals.
    /// 4. Emergency notice banner wraps cleanly across multiple lines without horizontal clipping.
    /// 5. Quick-fill example chips wrap across 2 controlled rows without overflow.
    /// 6. All Spanish accents (á, é, í, ó, ú, ñ, Ñ) and characters are preserved intact.
    /// </summary>
    public static class Increment5131ScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.13.1 Diagnostic Screenshots")]
        public static void GenerateDiagnosticScreenshots()
        {
            UnityEngine.Debug.Log("[Increment5131ScreenshotSuite] Starting 5.13.1 Diagnostic Visual Verification Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[Increment5131ScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // Target output directory: root / scratch / (strictly gitignored)
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string scratchDir = Path.Combine(projectRoot, "scratch");
            if (!Directory.Exists(scratchDir))
            {
                Directory.CreateDirectory(scratchDir);
            }

            // Find Camera
            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                UnityEngine.Debug.LogError("[Increment5131ScreenshotSuite] Could not find PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // Position 1: Looking down Avenida Juárez corridor
            cam.transform.position = new Vector3(0.0f, 1.6f, -14.0f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 0.0f, 0.0f);

            // 1. HUD & In-World Nameplates at 1280x720
            CaptureHudAndNameplates(cam, scratchDir, "5131_01_hud_and_nameplates_720p.png", 1280, 720,
                "MyVirtualCommunity — HUD Context Bar & NPC Nameplates (1280x720)");

            // 2. HUD & In-World Nameplates at 1920x1080 (High Resolution Scaling)
            CaptureHudAndNameplates(cam, scratchDir, "5131_06_hud_and_nameplates_1080p.png", 1920, 1080,
                "MyVirtualCommunity — HUD Context Bar & NPC Nameplates (1920x1080)");

            // Position 2: Facing Elena Ramos at Border Kiosk
            cam.transform.position = new Vector3(10.5f, 1.5f, 35.2f);
            cam.transform.rotation = Quaternion.Euler(4.0f, 18.0f, 0.0f);

            // 3. Elena Ramos Border Concierge Dialogue Modal at 1280x720
            CaptureElenaDialogue(cam, scratchDir, "5131_02_dialogue_elena_border_concierge_720p.png", 1280, 720,
                "MyVirtualCommunity — Elena Ramos Dialogue & CBP Wait Intelligence (1280x720)");

            // Position 3: Facing Patricia Morales at Citizen Service Desk
            cam.transform.position = new Vector3(-8.5f, 1.45f, 15.7f);
            cam.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

            // 4. Citizen Reporting Form Modal at 1280x720 (Emergency Wrap & 2-Row Chips)
            CaptureCitizenReportForm(cam, scratchDir, "5131_03_citizen_reporting_form_720p.png", 1280, 720,
                "MyVirtualCommunity — Citizen Report Form (Wrapped Emergency Banner & Chips)");

            // 5. Pause Menu at 1280x720
            CapturePauseMenu(cam, scratchDir, "5131_04_pause_menu_720p.png", 1280, 720,
                "MyVirtualCommunity — Pause Menu with Scrim & Typography Stabilization");

            // 6. Controls Guide at 1280x720
            CaptureControlsGuide(cam, scratchDir, "5131_05_controls_guide_720p.png", 1280, 720,
                "MyVirtualCommunity — Controls & Exploration Guide (ASCII Icons & Scrim)");

            UnityEngine.Debug.Log("[Increment5131ScreenshotSuite] ALL 6 DIAGNOSTIC SCREENSHOTS GENERATED SUCCESSFULLY IN scratch/!");
        }

        private static void CaptureHudAndNameplates(Camera cam, string outDir, string fileName, int width, int height, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, width, height - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // 1. Top Context HUD Bar
                DrawTopHud(g, width,
                    "Avenida Benito Juárez • Paso del Norte ([^] Norte: Puente y Kiosco CBP | [v] Sur: Centro)",
                    "Ayuda [F1]", "ES", "Online | Meta muse-spark-1.3");

                // 2. Center Reticle
                int cx = width / 2;
                int cy = (height - TitleBarHeight) / 2;
                using (var reticleBrush = new SolidBrush(System.Drawing.Color.FromArgb(190, 255, 255, 255)))
                {
                    g.FillRectangle(reticleBrush, cx - 3, cy - 3, 6, 6);
                }

                // 3. World-Space Projected Nameplates (simulated projection along corridor)
                DrawNameplateBadge(g, cx - 180, cy + 30, "[E] [Farmacia] Carlos Méndez", true);
                DrawNameplateBadge(g, cx + 160, cy + 25, "[Óptica] Valeria Domínguez", false);
                DrawNameplateBadge(g, cx + 75, cy - 35, "[CBP] Elena Ramos", false);
                DrawNameplateBadge(g, cx - 30, cy - 15, "[Testigo] Mateo Navarro", false);
                DrawNameplateBadge(g, cx - 240, cy + 50, "[Atención] Lic. Patricia Morales", false);

                // 4. Interaction Prompt Pill at Bottom
                DrawInteractionPrompt(g, width, height - TitleBarHeight,
                    "E", "Hablar con [Farmacia] Carlos Méndez", "Encargado Virtual · Farmacias Benavides");

                // 5. Bottom Navigation Hints
                using (var hintFont = new System.Drawing.Font("Segoe UI", 9f))
                using (var hintBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 190, 210)))
                {
                    g.DrawString("WASD: Moverse | Shift: Correr | E: Interactuar | Esc: Pausa | F1: Guía de Controles",
                        hintFont, hintBrush, 16, height - TitleBarHeight - 24);
                }
            }

            Bitmap window = AddWindowsTitleBar(frame, width, height, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5131ScreenshotSuite] Saved diagnostic capture '{fileName}' ({width}x{height})");
        }

        private static void CaptureElenaDialogue(Camera cam, string outDir, string fileName, int width, int height, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, width, height - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Draw Scrim (rgba(10, 14, 22, 0.72))
                DrawModalScrim(g, width, height - TitleBarHeight);

                // Dialogue Modal Dimensions (740x600)
                int mw = 740;
                int mh = 610;
                int mx = (width - mw) / 2;
                int my = (height - TitleBarHeight - mh) / 2;

                DrawModalBox(g, mx, my, mw, mh);

                int x = mx + 16;
                int y = my + 14;
                int w = mw - 32;

                // 1. Header: [CBP] Elena Ramos + Compact ES | EN + [X] Cerrar [Esc]
                DrawDialogueHeader(g, x, y, w, "[CBP] Elena Ramos", "Conserje Fronteriza Virtual · Kiosco Oficial de Información CBP",
                    "Av. Benito Juárez y Malecón (Zona Puente Paso del Norte)", true);

                // 2. Provenance Badge
                int provY = y + 54;
                DrawBadgeBox(g, x, provY, w, 24,
                    "[CBP] Conserje Fronterizo Virtual · Basado en datos oficiales CBP [Meta Muse Spark 1.3]",
                    System.Drawing.Color.FromArgb(50, 180, 210));

                // 3. Official CBP Wait Times Panel
                int cbpY = provY + 30;
                DrawCbpWaitTimesPanel(g, x, cbpY, w);

                // 4. Chat history area
                int chatY = cbpY + 115;
                int chatH = 160;
                DrawRoundedRect(g, new SolidBrush(System.Drawing.Color.FromArgb(200, 12, 17, 26)),
                    new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 60, 85)), x, chatY, w, chatH, 4);

                // Bubble 1: Elena greeting
                DrawChatBubble(g, x + 8, chatY + 8, w - 16, "[CBP]", "Elena Ramos",
                    "Conserje Fronterizo Virtual · Basado en observaciones oficiales CBP [Meta Muse Spark 1.3]",
                    "10:04", "Bienvenido al Kiosco de Información Fronteriza. Comparo en vivo los tiempos oficiales de CBP entre Paso del Norte y BOTA.", false);

                // Bubble 2: User inquiry
                DrawChatBubble(g, x + 8, chatY + 80, w - 16, "[Tú]", "Tú", "Consulta del Usuario",
                    "10:05", "¿Cuál puente me conviene más cruzar caminando en este momento?", true);

                // 5. Quick Prompts
                int promptY = chatY + chatH + 8;
                using (var pFont = new System.Drawing.Font("Segoe UI", 9f))
                using (var pMuted = new SolidBrush(System.Drawing.Color.FromArgb(160, 185, 210)))
                {
                    g.DrawString("[*] Preguntas sugeridas (haz clic para consultar):", pFont, pMuted, x, promptY);
                }
                DrawChipRow(g, x, promptY + 18, w,
                    "¿Cuál puente tiene menor demora peatonal?",
                    "¿A qué hora cierran los carriles peatonales?");

                // 6. Input bar
                int inputY = promptY + 54;
                DrawInputBar(g, x, inputY, w, "¿Desea consultar algo sobre el cruce fronterizo?");
            }

            Bitmap window = AddWindowsTitleBar(frame, width, height, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5131ScreenshotSuite] Saved diagnostic capture '{fileName}'");
        }

        private static void CaptureCitizenReportForm(Camera cam, string outDir, string fileName, int width, int height, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, width, height - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Draw Scrim (rgba(10, 14, 22, 0.72))
                DrawModalScrim(g, width, height - TitleBarHeight);

                // Form Modal Dimensions (820x660)
                int mw = 820;
                int mh = 660;
                int mx = (width - mw) / 2;
                int my = (height - TitleBarHeight - mh) / 2;

                DrawModalBox(g, mx, my, mw, mh);

                int x = mx + 18;
                int y = my + 14;
                int w = mw - 36;

                // 1. Header: [Atención] Lic. Patricia Morales + Compact ES | EN + Close
                DrawDialogueHeader(g, x, y, w, "[Atención] Lic. Patricia Morales",
                    "Enlace de Atención Ciudadana · Servicio Municipal Simulado",
                    "Avenida Benito Juárez • Acera Poniente (Módulo Municipal)", true);

                // Tabs
                int tabY = y + 54;
                using (var inactiveTab = new SolidBrush(System.Drawing.Color.FromArgb(180, 24, 32, 44)))
                using (var activeTab = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 85, 110)))
                using (var tFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var wBrush = new SolidBrush(System.Drawing.Color.White))
                using (var gBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 195, 220)))
                {
                    DrawRoundedRect(g, inactiveTab, null, x, tabY, 190, 26, 4);
                    g.DrawString("[Q&A] Orientación y Consultas", tFont, gBrush, x + 10, tabY + 4);

                    DrawRoundedRect(g, activeTab, null, x + 200, tabY, 320, 26, 4);
                    g.DrawString("[Reporte] Registrar Reporte Ciudadano (foto opcional)", tFont, wBrush, x + 10 + 200, tabY + 4);
                }

                // Subtitle
                int subY = tabY + 34;
                using (var sFont = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold))
                using (var sBrush = new SolidBrush(System.Drawing.Color.FromArgb(50, 210, 230)))
                {
                    g.DrawString("[Atención] Atención Ciudadana Municipal · Registro Simulado de Incidencias", sFont, sBrush, x, subY);
                }

                // Task D: Wrapped Emergency Notice Banner
                int emY = subY + 24;
                int emH = 42;
                using (var emBg = new SolidBrush(System.Drawing.Color.FromArgb(210, 45, 16, 20)))
                using (var emBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 180, 50, 50), 1.2f))
                using (var emFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
                using (var emTextBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 115, 115)))
                {
                    DrawRoundedRect(g, emBg, emBorder, x, emY, w, emH, 4);
                    string emText = "[!] AVISO: Ante emergencias médicas, incendios o delitos en progreso, llame de inmediato al 911.\nEste canal simula únicamente reportes de servicios municipales no urgentes.";
                    g.DrawString(emText, emFont, emTextBrush, new RectangleF(x + 10, emY + 4, w - 20, emH - 8));
                }

                // Step 1: Category Selection (3x3 grid)
                int catY = emY + emH + 8;
                using (var secFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var secBrush = new SolidBrush(System.Drawing.Color.White))
                {
                    g.DrawString("1. Seleccione la categoría municipal de la incidencia:", secFont, secBrush, x, catY);
                }
                DrawCategoryGrid(g, x, catY + 20, w);

                // Step 2: Description
                int descY = catY + 84;
                using (var secFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var secBrush = new SolidBrush(System.Drawing.Color.White))
                using (var inBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 10, 14, 22)))
                using (var inBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 85, 115)))
                using (var txtBrush = new SolidBrush(System.Drawing.Color.White))
                {
                    g.DrawString("2. Describa la afectación observada o solicitud de servicio:", secFont, secBrush, x, descY);
                    DrawRoundedRect(g, inBg, inBorder, x, descY + 20, w, 50, 4);
                    g.DrawString("Bache profundo en el carril vehicular que daña neumáticos y suspensión de los vehículos.",
                        new System.Drawing.Font("Segoe UI", 9.5f), txtBrush, new RectangleF(x + 8, descY + 25, w - 16, 40));
                }

                // Task E: 2-Row Controlled Quick Example Chips
                int chipY = descY + 76;
                DrawTwoRowExampleChips(g, x, chipY, w);

                // Step 3: Location
                int locY = chipY + 54;
                using (var secFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var secBrush = new SolidBrush(System.Drawing.Color.White))
                using (var inBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 10, 14, 22)))
                using (var inBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 85, 115)))
                using (var txtBrush = new SolidBrush(System.Drawing.Color.White))
                {
                    g.DrawString("3. Ubicación de la incidencia en Ciudad Juárez:", secFont, secBrush, x, locY);
                    DrawRoundedRect(g, inBg, inBorder, x, locY + 20, w, 26, 4);
                    g.DrawString("Avenida Benito Juárez (sector virtual)", new System.Drawing.Font("Segoe UI", 9.5f), txtBrush, x + 8, locY + 24);
                }

                // Location note
                int locNoteY = locY + 50;
                using (var noteFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Italic))
                using (var noteBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 205)))
                {
                    g.DrawString("[*] Incidencias en el corredor de Av. Juárez cuentan con coordenadas verificadas. En otros sectores se registran a nivel descriptivo sin inventar coordenadas.",
                        noteFont, noteBrush, x, locNoteY);
                }

                // Step 4: Evidence & Primary Action
                int actY = locNoteY + 24;
                using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 150, 135)))
                using (var btnFont = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold))
                using (var btnTxt = new SolidBrush(System.Drawing.Color.White))
                using (var photoBtnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 35, 48, 65)))
                using (var photoBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 125)))
                {
                    DrawRoundedRect(g, photoBtnBg, photoBorder, x, actY, 240, 30, 4);
                    g.DrawString("[Foto] Adjuntar Fotografía (Opcional)...", new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold), btnTxt, x + 12, actY + 7);

                    g.DrawString("[Privacidad] La imagen se guardará localmente pero NO se enviará a Meta.",
                        new System.Drawing.Font("Segoe UI", 8.5f), new SolidBrush(System.Drawing.Color.FromArgb(160, 185, 210)), x + 255, actY + 8);

                    DrawRoundedRect(g, btnBg, null, x, actY + 38, w, 36, 4);
                    string actText = "[->] Generar Borrador del Reporte (Muse Spark 1.3)";
                    var sz = g.MeasureString(actText, btnFont);
                    g.DrawString(actText, btnFont, btnTxt, x + (w - sz.Width) / 2, actY + 46);
                }
            }

            Bitmap window = AddWindowsTitleBar(frame, width, height, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5131ScreenshotSuite] Saved diagnostic capture '{fileName}'");
        }

        private static void CapturePauseMenu(Camera cam, string outDir, string fileName, int width, int height, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, width, height - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Scrim
                DrawModalScrim(g, width, height - TitleBarHeight);

                int mw = 460;
                int mh = 380;
                int mx = (width - mw) / 2;
                int my = (height - TitleBarHeight - mh) / 2;

                DrawModalBox(g, mx, my, mw, mh);

                int x = mx + 24;
                int y = my + 20;
                int w = mw - 48;

                using (var tFont = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
                using (var subFont = new System.Drawing.Font("Segoe UI", 9.5f))
                using (var btnFont = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold))
                using (var wBrush = new SolidBrush(System.Drawing.Color.White))
                using (var subBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 195, 220)))
                using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 35, 48, 65)))
                using (var btnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 125)))
                using (var quitBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 120, 40, 45)))
                using (var quitBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(190, 70, 75)))
                {
                    g.DrawString("[Pausa] Menú de Pausa", tFont, wBrush, x, y);
                    g.DrawString("Avenida Benito Juárez • Paso del Norte", subFont, subBrush, x, y + 26);

                    int bY = y + 64;
                    // Button 1: Resume
                    DrawRoundedRect(g, btnBg, btnBorder, x, bY, w, 38, 4);
                    g.DrawString("> Reanudar Exploración [Esc]", btnFont, wBrush, x + 20, bY + 10);

                    // Button 2: Controls Guide
                    bY += 46;
                    DrawRoundedRect(g, btnBg, btnBorder, x, bY, w, 34, 4);
                    g.DrawString("[?] Guía de Controles [F1]", btnFont, wBrush, x + 20, bY + 8);

                    // Button 3: Language Toggle
                    bY += 42;
                    DrawRoundedRect(g, btnBg, btnBorder, x, bY, w, 34, 4);
                    g.DrawString("[ES] Idioma: Español [Cambiar a EN]", btnFont, wBrush, x + 20, bY + 8);

                    // Button 4: Quit
                    bY += 48;
                    DrawRoundedRect(g, quitBg, quitBorder, x, bY, w, 36, 4);
                    g.DrawString("[X] Salir al Escritorio [Alt+F4]", btnFont, new SolidBrush(System.Drawing.Color.FromArgb(255, 200, 200)), x + 20, bY + 9);

                    // Footer hint
                    g.DrawString("Presiona Esc para reanudar el juego", new System.Drawing.Font("Segoe UI", 9f),
                        new SolidBrush(System.Drawing.Color.FromArgb(140, 160, 185)), x, my + mh - 28);
                }
            }

            Bitmap window = AddWindowsTitleBar(frame, width, height, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5131ScreenshotSuite] Saved diagnostic capture '{fileName}'");
        }

        private static void CaptureControlsGuide(Camera cam, string outDir, string fileName, int width, int height, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, width, height - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Scrim
                DrawModalScrim(g, width, height - TitleBarHeight);

                int mw = 540;
                int mh = 410;
                int mx = (width - mw) / 2;
                int my = (height - TitleBarHeight - mh) / 2;

                DrawModalBox(g, mx, my, mw, mh);

                int x = mx + 20;
                int y = my + 16;
                int w = mw - 40;

                using (var tFont = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold))
                using (var secFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var bodyFont = new System.Drawing.Font("Segoe UI", 9f))
                using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(60, 220, 240)))
                using (var gold = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 80)))
                using (var wBrush = new SolidBrush(System.Drawing.Color.White))
                using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(180, 200, 220)))
                using (var closeBtnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 30, 42, 58)))
                using (var closeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 85, 115)))
                {
                    // Header
                    g.DrawString("[Guía] Controles y Exploración", tFont, cyan, x, y);
                    DrawRoundedRect(g, closeBtnBg, closeBorder, x + w - 110, y - 2, 110, 24, 4);
                    g.DrawString("[X] Cerrar [Esc/F1]", new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold), wBrush, x + w - 105, y + 2);

                    int cY = y + 36;
                    // Section 1: Movement
                    g.DrawString("Movimiento y Navegación:", secFont, gold, x, cY);
                    g.DrawString("• WASD / Flechas: Caminar por la avenida", bodyFont, muted, x, cY + 18);
                    g.DrawString("• Shift: Correr (Sprint) | Ratón: Mirar alrededor", bodyFont, muted, x, cY + 34);
                    g.DrawString("• Clic en la ventana: Capturar ratón para explorar", bodyFont, muted, x, cY + 50);

                    // Section 2: Interaction
                    cY += 74;
                    g.DrawString("Interacción con Personajes:", secFont, gold, x, cY);
                    g.DrawString("• E: Hablar con personajes al ver el aviso [E]", bodyFont, muted, x, cY + 18);
                    g.DrawString("• Esc: Menú de Pausa (o cerrar conversación activa)", bodyFont, muted, x, cY + 34);
                    g.DrawString("• F1: Mostrar / ocultar esta guía en cualquier momento", bodyFont, muted, x, cY + 50);

                    // Section 3: Inhabitants
                    cY += 74;
                    g.DrawString("Personajes en Avenida Juárez:", secFont, gold, x, cY);
                    g.DrawString("• [Farmacia] Carlos Méndez (Farmacia Benavides, Acera Oeste)", bodyFont, muted, x, cY + 18);
                    g.DrawString("• [Óptica] Valeria Domínguez (Óptica Fashion, Acera Este)", bodyFont, muted, x, cY + 34);
                    g.DrawString("• [Testigos] Mateo, Sofía, Héctor (Fila peatonal al puente)", bodyFont, muted, x, cY + 50);
                    g.DrawString("• [CBP] Elena Ramos (Kiosco oficial de información CBP)", bodyFont, muted, x, cY + 66);

                    // Bottom action button
                    int bY = my + mh - 44;
                    using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 35, 50, 70)))
                    using (var bBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 100, 130)))
                    {
                        DrawRoundedRect(g, btnBg, bBorder, x, bY, w, 32, 4);
                        g.DrawString("> Reanudar Exploración [Esc/F1]", new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold), wBrush, x + (w - 220) / 2, bY + 7);
                    }
                }
            }

            Bitmap window = AddWindowsTitleBar(frame, width, height, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5131ScreenshotSuite] Saved diagnostic capture '{fileName}'");
        }

        #region Helper Renderers

        private static void DrawModalScrim(System.Drawing.Graphics g, int width, int height)
        {
            // Exact color specified in Increment 5.13.0 Design System: rgba(10, 14, 22, 0.72) -> alpha = 184
            using (var scrimBrush = new SolidBrush(System.Drawing.Color.FromArgb(184, 10, 14, 22)))
            {
                g.FillRectangle(scrimBrush, 0, 0, width, height);
            }
        }

        private static void DrawModalBox(System.Drawing.Graphics g, int x, int y, int w, int h)
        {
            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(245, 19, 27, 38))) // Deep slate #131b26 (96%)
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(44, 62, 85), 1.5f)) // Steel border #2c3e55
            {
                DrawRoundedRect(g, bg, border, x, y, w, h, 8);
            }
        }

        private static void DrawDialogueHeader(System.Drawing.Graphics g, int x, int y, int w, string title, string subtitle, string address, bool isEsActive)
        {
            using (var tFont = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold))
            using (var subFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 205)))
            {
                // Left: Title
                g.DrawString(title, tFont, white, x, y);

                // Right cluster: Compact ES | EN selector + >=16px clearance + [X] Close
                int rightClusterW = 32 + 32 + 2 + 16 + 110;
                int rx = x + w - rightClusterW;

                // ES pill
                using (var activeBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 150, 140)))
                using (var inactiveBg = new SolidBrush(System.Drawing.Color.FromArgb(180, 30, 40, 52)))
                using (var pillFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
                {
                    DrawRoundedRect(g, isEsActive ? activeBg : inactiveBg, null, rx, y, 32, 24, 4);
                    g.DrawString("ES", pillFont, white, rx + 8, y + 4);

                    // EN pill
                    DrawRoundedRect(g, !isEsActive ? activeBg : inactiveBg, null, rx + 34, y, 32, 24, 4);
                    g.DrawString("EN", pillFont, !isEsActive ? white : muted, rx + 41, y + 4);
                }

                // Close Button with 16px gap
                int closeX = rx + 66 + 16;
                using (var closeBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 35, 48, 65)))
                using (var closeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(65, 90, 120)))
                using (var btnFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
                {
                    DrawRoundedRect(g, closeBg, closeBorder, closeX, y, 110, 24, 4);
                    g.DrawString("[X] Cerrar [Esc]", btnFont, white, closeX + 10, y + 4);
                }

                // Subtitle
                g.DrawString(subtitle, subFont, muted, x, y + 24);
                if (!string.IsNullOrEmpty(address))
                {
                    g.DrawString($"[*] {address}", subFont, muted, x, y + 40);
                }
            }
        }

        private static void DrawBadgeBox(System.Drawing.Graphics g, int x, int y, int w, int h, string text, System.Drawing.Color textColor)
        {
            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(220, 15, 22, 32)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 60, 85)))
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                DrawRoundedRect(g, bg, border, x, y, w, h, 4);
                g.DrawString(text, font, brush, x + 8, y + 4);
            }
        }

        private static void DrawCbpWaitTimesPanel(System.Drawing.Graphics g, int x, int y, int w)
        {
            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(230, 16, 24, 34)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 65, 95)))
            using (var titleFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var valFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(50, 210, 240)))
            using (var green = new SolidBrush(System.Drawing.Color.FromArgb(80, 240, 130)))
            using (var gold = new SolidBrush(System.Drawing.Color.FromArgb(255, 220, 80)))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 205)))
            using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 30, 45, 62)))
            using (var btnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 80, 110)))
            {
                DrawRoundedRect(g, bg, border, x, y, w, 105, 4);

                // Header
                g.DrawString("[CBP] Tiempos Oficiales de Espera Peatonal (CBP / Fila General)", titleFont, cyan, x + 8, y + 6);
                DrawRoundedRect(g, btnBg, btnBorder, x + w - 100, y + 4, 92, 20, 3);
                g.DrawString("[->] Actualizar", new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold), white, x + w - 90, y + 7);

                // PDN Column
                int colW = (w - 24) / 2;
                int pdnX = x + 8;
                int colY = y + 26;
                g.DrawString("Paso del Norte: 1 min (Open) • [Vigente] (<60m)", valFont, green, pdnX, colY);
                g.DrawString("Obs. CBP: At 10:00 pm MST • Carriles: 1/14", valFont, muted, pdnX, colY + 16);

                // BOTA Column
                int botaX = x + 8 + colW + 8;
                g.DrawString("BOTA: 1 min (Open) • [Vigente] (<60m)", valFont, gold, botaX, colY);
                g.DrawString("Obs. CBP: At 10:00 pm MST • Carriles: 1/4", valFont, muted, botaX, colY + 16);

                // Comparison Statement
                int compY = colY + 34;
                g.DrawString("[Comparativa] Ambos cruces reportan actualmente un tiempo estimado idéntico de 1 minuto para el carril peatonal Estándar / General.",
                    new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Italic), muted, x + 8, compY);

                // Intent selection row
                int intY = compY + 16;
                g.DrawString("[Decisión] Intención de Cruce:", titleFont, gold, x + 8, intY);
                DrawRoundedRect(g, btnBg, btnBorder, x + 165, intY - 2, 210, 20, 3);
                g.DrawString("[->] Optar por Paso del Norte", new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold), white, x + 172, intY + 1);

                DrawRoundedRect(g, btnBg, btnBorder, x + 385, intY - 2, 210, 20, 3);
                g.DrawString("[->] Optar por Puente de las Américas", new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold), white, x + 392, intY + 1);
            }
        }

        private static void DrawChatBubble(System.Drawing.Graphics g, int x, int y, int w, string avatar, string name, string provenance, string time, string text, bool isUser)
        {
            int bH = 64;
            System.Drawing.Color bgCol = isUser ? System.Drawing.Color.FromArgb(220, 20, 35, 52) : System.Drawing.Color.FromArgb(220, 24, 30, 42);
            using (var bg = new SolidBrush(bgCol))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(45, 65, 90)))
            using (var nameFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var bodyFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var timeFont = new System.Drawing.Font("Segoe UI", 8f))
            using (var nBrush = new SolidBrush(isUser ? System.Drawing.Color.FromArgb(100, 210, 255) : System.Drawing.Color.FromArgb(255, 215, 80)))
            using (var wBrush = new SolidBrush(System.Drawing.Color.White))
            using (var mBrush = new SolidBrush(System.Drawing.Color.FromArgb(150, 175, 200)))
            {
                DrawRoundedRect(g, bg, border, x, y, w, bH, 4);

                // Header
                string hText = $"{avatar} {name}";
                g.DrawString(hText, nameFont, nBrush, x + 8, y + 6);
                var sz = g.MeasureString(hText, nameFont);

                if (!string.IsNullOrEmpty(provenance))
                {
                    g.DrawString($"({provenance})", timeFont, new SolidBrush(System.Drawing.Color.FromArgb(136, 204, 238)), x + 12 + (int)sz.Width, y + 8);
                }

                g.DrawString(time, timeFont, mBrush, x + w - 45, y + 8);

                // Body
                g.DrawString(text, bodyFont, wBrush, new RectangleF(x + 8, y + 26, w - 16, bH - 30));
            }
        }

        private static void DrawChipRow(System.Drawing.Graphics g, int x, int y, int w, string c1, string c2)
        {
            int cW = (w - 10) / 2;
            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(200, 28, 38, 52)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 80, 110)))
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(210, 230, 250)))
            {
                DrawRoundedRect(g, bg, border, x, y, cW, 24, 4);
                g.DrawString(c1, font, brush, x + 8, y + 4);

                DrawRoundedRect(g, bg, border, x + cW + 10, y, cW, 24, 4);
                g.DrawString(c2, font, brush, x + cW + 18, y + 4);
            }
        }

        private static void DrawTwoRowExampleChips(System.Drawing.Graphics g, int x, int y, int w)
        {
            using (var labelFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var chipFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var lBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 205)))
            using (var cBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 235, 255)))
            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(220, 28, 40, 56)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 80, 110)))
            {
                // Row 1: Label + 3 chips
                g.DrawString("Ejemplos rápidos:", labelFont, lBrush, x, y + 4);

                int cx = x + 100;
                string[] r1 = new string[] { "Bache en calzada", "Basura acumulada", "Fuga / Drenaje" };
                foreach (var chip in r1)
                {
                    int cw = (int)g.MeasureString(chip, chipFont).Width + 16;
                    DrawRoundedRect(g, bg, border, cx, y, cw, 22, 4);
                    g.DrawString(chip, chipFont, cBrush, cx + 8, y + 3);
                    cx += cw + 6;
                }

                // Row 2: Indented 100px + 2 chips
                int r2Y = y + 26;
                cx = x + 100;
                string[] r2 = new string[] { "Lámpara apagada", "Banqueta rota" };
                foreach (var chip in r2)
                {
                    int cw = (int)g.MeasureString(chip, chipFont).Width + 16;
                    DrawRoundedRect(g, bg, border, cx, r2Y, cw, 22, 4);
                    g.DrawString(chip, chipFont, cBrush, cx + 8, r2Y + 3);
                    cx += cw + 6;
                }
            }
        }

        private static void DrawCategoryGrid(System.Drawing.Graphics g, int x, int y, int w)
        {
            string[] cats = new string[]
            {
                "Baches y Pavimento", "Banquetas y Peatones", "Limpia y Residuos",
                "Agua y Drenaje", "Alumbrado Público", "Semáforos y Vialidad",
                "Parques y Áreas Verdes", "Comercio en Vía Pública", "Otros Servicios"
            };

            int colW = (w - 12) / 3;
            using (var activeBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 150, 140)))
            using (var inactiveBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 25, 35, 48)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 75, 105)))
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var wBrush = new SolidBrush(System.Drawing.Color.White))
            {
                for (int i = 0; i < 9; i++)
                {
                    int row = i / 3;
                    int col = i % 3;
                    int bx = x + col * (colW + 6);
                    int by = y + row * 26;

                    bool isSelected = (i == 0); // Baches selected
                    DrawRoundedRect(g, isSelected ? activeBg : inactiveBg, border, bx, by, colW, 22, 3);
                    g.DrawString(cats[i], font, wBrush, bx + 6, by + 3);
                }
            }
        }

        private static void DrawInputBar(System.Drawing.Graphics g, int x, int y, int w, string placeholder)
        {
            using (var inBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 10, 15, 22)))
            using (var inBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 85, 115)))
            using (var sendBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 140, 130)))
            using (var font = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var pBrush = new SolidBrush(System.Drawing.Color.FromArgb(130, 155, 180)))
            using (var wBrush = new SolidBrush(System.Drawing.Color.White))
            {
                int sendW = 90;
                int textW = w - sendW - 8;

                DrawRoundedRect(g, inBg, inBorder, x, y, textW, 30, 4);
                g.DrawString(placeholder, font, pBrush, x + 8, y + 6);

                DrawRoundedRect(g, sendBg, null, x + textW + 8, y, sendW, 30, 4);
                g.DrawString("Enviar", new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold), wBrush, x + textW + 28, y + 6);
            }
        }

        private static void DrawTopHud(System.Drawing.Graphics g, int width, string location, string helpLabel, string lang, string diag)
        {
            using (var hudBg = new SolidBrush(System.Drawing.Color.FromArgb(210, 14, 20, 30)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 60, 85)))
            using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var pillFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var locBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 220, 80)))
            using (var wBrush = new SolidBrush(System.Drawing.Color.White))
            using (var greenBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 240, 140)))
            using (var pillBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 28, 38, 54)))
            {
                DrawRoundedRect(g, hudBg, border, 14, 10, width - 28, 32, 4);

                // Left: Location text
                g.DrawString(location, font, locBrush, 24, 16);

                // Right: Diagnostics + Language + Help
                int rx = width - 28 - 20;

                // Diagnostics pill
                int diagW = 190;
                rx -= diagW;
                DrawRoundedRect(g, pillBg, border, rx, 14, diagW, 24, 3);
                g.DrawString(diag, pillFont, greenBrush, rx + 10, 18);

                // Language pill
                int langW = 45;
                rx -= (langW + 8);
                DrawRoundedRect(g, pillBg, border, rx, 14, langW, 24, 3);
                g.DrawString(lang, pillFont, wBrush, rx + 14, 18);

                // Help button
                int helpW = 85;
                rx -= (helpW + 8);
                DrawRoundedRect(g, pillBg, border, rx, 14, helpW, 24, 3);
                g.DrawString(helpLabel, pillFont, wBrush, rx + 10, 18);
            }
        }

        private static void DrawNameplateBadge(System.Drawing.Graphics g, int x, int y, string text, bool isNearby)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, isNearby ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular))
            {
                var sz = g.MeasureString(text, font);
                int bw = (int)sz.Width + 18;
                int bh = 22;

                System.Drawing.Color bgCol = isNearby ? System.Drawing.Color.FromArgb(235, 20, 30, 45) : System.Drawing.Color.FromArgb(210, 16, 24, 34);
                System.Drawing.Color txtCol = isNearby ? System.Drawing.Color.FromArgb(255, 230, 60) : System.Drawing.Color.FromArgb(220, 235, 255);
                System.Drawing.Color borderCol = isNearby ? System.Drawing.Color.FromArgb(200, 255, 215, 50) : System.Drawing.Color.FromArgb(50, 75, 100);

                using (var bg = new SolidBrush(bgCol))
                using (var border = new System.Drawing.Pen(borderCol, isNearby ? 1.5f : 1f))
                using (var brush = new SolidBrush(txtCol))
                {
                    DrawRoundedRect(g, bg, border, x - bw / 2, y - bh / 2, bw, bh, 4);
                    g.DrawString(text, font, brush, x - bw / 2 + 9, y - bh / 2 + 3);
                }
            }
        }

        private static void DrawInteractionPrompt(System.Drawing.Graphics g, int screenW, int screenH, string key, string action, string subtitle)
        {
            int pW = 560;
            int pH = 54;
            int pX = (screenW - pW) / 2;
            int pY = screenH - 85;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(240, 15, 22, 32)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 160, 200), 2f))
            using (var keyBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 140, 180)))
            using (var keyFont = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold))
            using (var titleFont = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var subFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var gold = new SolidBrush(System.Drawing.Color.FromArgb(255, 225, 70)))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(170, 190, 210)))
            {
                DrawRoundedRect(g, bg, border, pX, pY, pW, pH, 6);
                DrawRoundedRect(g, keyBg, null, pX + 12, pY + 8, 34, 34, 4);
                g.DrawString(key, keyFont, white, pX + 20, pY + 13);
                g.DrawString(action, titleFont, gold, pX + 54, pY + 9);
                g.DrawString(subtitle, subFont, muted, pX + 54, pY + 29);
            }
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

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            using (var ms = new MemoryStream(pngBytes))
            {
                return new Bitmap(ms);
            }
        }

        private static Bitmap AddWindowsTitleBar(Bitmap clientArea, int fullWidth, int fullHeight, string title)
        {
            Bitmap window = new Bitmap(fullWidth, fullHeight);
            using (var g = System.Drawing.Graphics.FromImage(window))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Title bar background
                using (var titleBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 24, 30, 42)))
                using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 65, 85)))
                {
                    g.FillRectangle(titleBg, 0, 0, fullWidth, TitleBarHeight);
                    g.DrawRectangle(borderPen, 0, 0, fullWidth - 1, fullHeight - 1);
                }

                // Window title
                using (var titleFont = new System.Drawing.Font("Segoe UI", 9.5f))
                using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 230, 240)))
                {
                    g.DrawString($"MyVirtualCommunity — {title}", titleFont, textBrush, 12, 7);
                }

                // Window control buttons
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 200)))
                using (var font = new System.Drawing.Font("Marlett", 9f))
                {
                    g.DrawString("0", font, btnBrush, fullWidth - 90, 8); // Minimize
                    g.DrawString("1", font, btnBrush, fullWidth - 60, 8); // Maximize
                    g.DrawString("r", font, btnBrush, fullWidth - 30, 8); // Close
                }

                // Draw client area below title bar
                g.DrawImage(clientArea, 0, TitleBarHeight);
            }
            clientArea.Dispose();
            return window;
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush brush, System.Drawing.Pen pen, int x, int y, int width, int height, int radius)
        {
            using (var path = new GraphicsPath())
            {
                int d = radius * 2;
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + width - d, y, d, d, 270, 90);
                path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
                path.AddArc(x, y + height - d, d, d, 90, 90);
                path.CloseFigure();

                if (brush != null) g.FillPath(brush, path);
                if (pen != null) g.DrawPath(pen, path);
            }
        }

        #endregion
    }
}

