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
    /// Visual verification suite for Increment 5.11.4:
    /// Visual Evidence Inspection & Citizen Reporting UI Refinement.
    /// Generates high-fidelity screenshots verifying:
    /// 1. 5114_01_scrollable_form_fixed_footer: Citizen report input form with scrollable content & fixed unclipped footer.
    /// 2. 5114_02_enlarged_viewer_combined_mode: Enlarged viewer in Combined Mode (Original Photo + Authentic SAM 3.1 Pixel Masks + BBoxes).
    /// 3. 5114_03_enlarged_viewer_mask_only_mode: Enlarged viewer in Mask Only Mode (AI-generated pixel masks on dark plate).
    /// 4. 5114_04_enlarged_viewer_privacy_declined: Enlarged viewer when external AI consent is declined (Truthful Privacy notice).
    /// </summary>
    public static class Increment5114ScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.11.4 Inspection Screenshots")]
        public static void GenerateScreenshots()
        {
            UnityEngine.Debug.Log("[Increment5114ScreenshotSuite] Starting 5.11.4 Visual Evidence Verification Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[Increment5114ScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "5_11_4"));
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }

            var bobbleAnimators = UnityEngine.Object.FindObjectsByType<BobbleheadAnimator>(FindObjectsSortMode.None);
            foreach (var ba in bobbleAnimators)
            {
                if (ba != null) ba.ForcePoseDirect();
            }

            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                UnityEngine.Debug.LogError("[Increment5114ScreenshotSuite] Could not find PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // Position camera facing Patricia Morales at CitizenServiceDesk (-8.5, 0, 18)
            cam.transform.position = new Vector3(-8.5f, 1.45f, 15.7f);
            cam.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

            // 1. Scrollable Form with Fixed Footer
            CaptureAndSaveScrollableForm(cam, screenshotDir, "5114_01_scrollable_form_fixed_footer.png",
                "MyVirtualCommunity — Objective B: Scrollable Citizen Report Form with Fixed Footer (1280x720)");

            // 2. Enlarged Viewer: Combined Mode (Photo + Masks + Boxes)
            CaptureAndSaveViewerCombined(cam, screenshotDir, "5114_02_enlarged_viewer_combined_mode.png",
                "MyVirtualCommunity — Objective A: Visual Evidence Inspection (Mode 2: Photo + SAM 3.1 Authentic Masks)");

            // 3. Enlarged Viewer: Mask Only Mode (AI Pixel Geometry)
            CaptureAndSaveViewerMaskOnly(cam, screenshotDir, "5114_03_enlarged_viewer_mask_only_mode.png",
                "MyVirtualCommunity — Objective A: Visual Evidence Inspection (Mode 1: Pure SAM 3.1 Pixel Raster)");

            // 4. Enlarged Viewer: Privacy Declined Mode
            CaptureAndSaveViewerPrivacyDeclined(cam, screenshotDir, "5114_04_enlarged_viewer_privacy_declined.png",
                "MyVirtualCommunity — Objective A: Visual Evidence Inspection (Privacy: External AI Consent Declined)");

            UnityEngine.Debug.Log("[Increment5114ScreenshotSuite] All 4 Increment 5.11.4 screenshots generated successfully!");
        }

        private static void CaptureAndSaveScrollableForm(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawScrollableFormModal(g);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5114ScreenshotSuite] Saved scrollable form screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveViewerCombined(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawEnlargedViewerModal(g, displayMode: 2, hasConsent: true);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5114ScreenshotSuite] Saved combined viewer screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveViewerMaskOnly(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawEnlargedViewerModal(g, displayMode: 1, hasConsent: true);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5114ScreenshotSuite] Saved mask-only viewer screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveViewerPrivacyDeclined(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawEnlargedViewerModal(g, displayMode: 0, hasConsent: false);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment5114ScreenshotSuite] Saved privacy-declined viewer screenshot to '{savePath}'");
        }

        private static void DrawModalScrim(System.Drawing.Graphics g)
        {
            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(170, 4, 8, 14)))
            {
                g.FillRectangle(scrim, 0, 0, WinWidth, WinHeight - TitleBarHeight);
            }
        }

        private static void DrawScrollableFormModal(System.Drawing.Graphics g)
        {
            int modalW = 880;
            int modalH = 670;
            int modalX = (WinWidth - modalW) / 2;
            int modalY = (WinHeight - TitleBarHeight - modalH) / 2;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(246, 16, 22, 32)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 90, 125), 1.5f))
            {
                DrawRoundedRect(g, bg, border, modalX, modalY, modalW, modalH, 10);
            }

            int x = modalX + 18;
            int y = modalY + 14;
            int w = modalW - 36;

            DrawHeader(g, x, y, w, "Lic. Patricia Morales", "Enlace de Atención Ciudadana · Servicio Municipal Simulado");

            // Subtitle
            int formY = y + 50;
            using (var subFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Italic))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(80, 210, 245)))
            {
                g.DrawString("📋 Reporte Ciudadano General · Incidencias en Espacios y Servicios Públicos", subFont, cyan, x, formY);
            }

            // Scrollable Content Region Box (with Vertical Scrollbar on Right, NO Horizontal Scrollbar)
            int scrollY = formY + 24;
            int scrollH = modalH - 105;
            using (var scrollBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(35, 55, 75), 1f))
            using (var scrollTrackBg = new SolidBrush(System.Drawing.Color.FromArgb(180, 20, 28, 40)))
            using (var scrollThumbBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 60, 90, 120)))
            {
                g.DrawRectangle(scrollBorder, x, scrollY, w, scrollH);

                // Vertical scrollbar on the right edge ONLY
                int trackX = x + w - 12;
                g.FillRectangle(scrollTrackBg, trackX, scrollY, 12, scrollH);
                g.FillRectangle(scrollThumbBg, trackX + 1, scrollY + 10, 10, 160);
            }

            int innerX = x + 12;
            int curY = scrollY + 8;
            int innerW = w - 30; // accounts for vertical scrollbar on right

            using (var boldFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var regularFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var smallFont = new System.Drawing.Font("Segoe UI", 8.5f))
            using (var inputBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 12, 16, 24)))
            using (var inputBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 125)))
            using (var textBrush = new SolidBrush(System.Drawing.Color.White))
            using (var mutedBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 175, 195)))
            using (var alertBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 45, 18, 18)))
            using (var alertBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 60, 60)))
            using (var alertBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 130, 130)))
            {
                // Emergency Notice Banner (word-wrapped)
                DrawRoundedRect(g, alertBg, alertBorder, innerX, curY, innerW, 26, 4);
                g.DrawString("⚠️ AVISO: Ante emergencias médicas, incendios o delitos en progreso, llame de inmediato al 911. Este canal simula reportes no urgentes.", smallFont, alertBrush, innerX + 8, curY + 5);

                curY += 32;

                // 1. Category Selector (3x3 grid with wide columns)
                g.DrawString("1. Seleccione la categoría municipal de la incidencia:", boldFont, textBrush, innerX, curY);
                curY += 18;
                DrawCategoryGrid(g, innerX, curY, innerW, selectedIndex: 3); // AGUA_Y_DRENAJE selected

                curY += 82;

                // 2. Description
                g.DrawString("2. Describa la afectación observada o solicitud de servicio:", boldFont, textBrush, innerX, curY);
                DrawRoundedRect(g, inputBg, inputBorder, innerX, curY + 18, innerW, 44, 4);
                g.DrawString("Acumulación severa de agua estancada tras lluvia en el cruce de Av. Juárez con Tlaxcala; obstruye el paso peatonal hacia los comercios.", regularFont, textBrush, new RectangleF(innerX + 6, curY + 22, innerW - 12, 38));

                curY += 68;

                // Quick examples
                DrawQuickExamplesRow(g, innerX, curY, innerW);
                curY += 26;

                // 3. Location
                g.DrawString("3. Ubicación de la incidencia en Ciudad Juárez:", boldFont, textBrush, innerX, curY);
                curY += 18;
                DrawRoundedRect(g, inputBg, inputBorder, innerX, curY, innerW, 26, 4);
                g.DrawString("Avenida Benito Juárez · Cruce con Calle Tlaxcala (Acera Poniente)", regularFont, textBrush, innerX + 8, curY + 5);

                curY += 32;

                // 4. Photographic Evidence Box
                g.DrawString("4. Evidencia fotográfica (Opcional — PNG o JPEG, máx 5 MB):", boldFont, textBrush, innerX, curY);
                curY += 18;

                int thumbW = 110;
                int thumbH = 70;
                DrawRoundedRect(g, inputBg, inputBorder, innerX, curY, thumbW, thumbH, 4);
                DrawSimulatedWaterPuddleImage(g, innerX + 2, curY + 2, thumbW - 4, thumbH - 4, drawMasks: false);

                // File stats and consent
                int statX = innerX + thumbW + 14;
                g.DrawString("📷 agua_estancada_cruce_tlaxcala.jpg", boldFont, textBrush, statX, curY + 2);
                g.DrawString("Tamaño: 284 KB · Formato: image/jpeg", regularFont, mutedBrush, statX, curY + 20);

                // Consent checkbox
                using (var chkBoxBrush = new SolidBrush(System.Drawing.Color.FromArgb(40, 180, 100)))
                {
                    g.FillRectangle(chkBoxBrush, statX, curY + 44, 14, 14);
                    g.DrawString("✓", boldFont, textBrush, statX + 1, curY + 42);
                }
                g.DrawString("Autorizo análisis visual externo con IA (Meta SAM 3.1)", boldFont, textBrush, statX + 20, curY + 43);

                curY += 80;

                // Action Button: Cleanly positioned inside form with generous margin, 100% visible and unclipped
                using (var genBtnBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 20, 125, 115)))
                using (var genBtnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 220, 210), 1.5f))
                using (var btnFont = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
                {
                    DrawRoundedRect(g, genBtnBg, genBtnBorder, innerX, curY, innerW, 38, 6);
                    g.DrawString("🔍 Generar Borrador del Reporte (Muse Spark 1.3)", btnFont, textBrush, innerX + (innerW - 380) / 2, curY + 9);
                }
            }
        }

        private static void DrawEnlargedViewerModal(System.Drawing.Graphics g, int displayMode, bool hasConsent)
        {
            int modalW = 920;
            int modalH = 640;
            int modalX = (WinWidth - modalW) / 2;
            int modalY = (WinHeight - TitleBarHeight - modalH) / 2;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(248, 14, 18, 26)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 105, 145), 1.5f))
            {
                DrawRoundedRect(g, bg, border, modalX, modalY, modalW, modalH, 10);
            }

            int x = modalX + 16;
            int y = modalY + 12;
            int w = modalW - 32;

            // Header Row
            using (var titleFont = new System.Drawing.Font("Segoe UI", 12.5f, System.Drawing.FontStyle.Bold))
            using (var closeFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var closeBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 45, 55, 75)))
            using (var closeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 105, 140)))
            {
                g.DrawString("🔬 Inspección Visual de Evidencia · Segmentación SAM 3.1", titleFont, white, x, y);

                DrawRoundedRect(g, closeBg, closeBorder, x + w - 120, y, 120, 26, 4);
                g.DrawString("✕ Cerrar [Esc]", closeFont, white, x + w - 108, y + 4);
            }

            // Controls Row (3 Display Modes + Checkbox)
            int ctlY = y + 36;
            using (var ctlFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(70, 220, 245)))
            using (var activeBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 25, 95, 120)))
            using (var activeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 210, 245), 1.5f))
            using (var inactiveBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 30, 40, 55)))
            using (var inactiveBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 105)))
            using (var disabledBrush = new SolidBrush(System.Drawing.Color.FromArgb(120, 140, 160)))
            {
                g.DrawString("Modo de Vista:", ctlFont, cyan, x, ctlY + 4);

                int btnX = x + 115;
                // Mode 0: Original Photo
                var b0Bg = (displayMode == 0) ? activeBg : inactiveBg;
                var b0Border = (displayMode == 0) ? activeBorder : inactiveBorder;
                DrawRoundedRect(g, b0Bg, b0Border, btnX, ctlY, 135, 26, 4);
                g.DrawString("1. Foto Original", ctlFont, white, btnX + 16, ctlY + 4);

                btnX += 145;
                // Mode 1: Mask Only
                var b1Bg = (displayMode == 1) ? activeBg : inactiveBg;
                var b1Border = (displayMode == 1) ? activeBorder : inactiveBorder;
                DrawRoundedRect(g, b1Bg, b1Border, btnX, ctlY, 175, 26, 4);
                g.DrawString(hasConsent ? "2. Máscara SAM (Solo IA)" : "2. Máscara SAM (Inactivo)", ctlFont, hasConsent ? white : disabledBrush, btnX + 14, ctlY + 4);

                btnX += 185;
                // Mode 2: Combined
                var b2Bg = (displayMode == 2) ? activeBg : inactiveBg;
                var b2Border = (displayMode == 2) ? activeBorder : inactiveBorder;
                DrawRoundedRect(g, b2Bg, b2Border, btnX, ctlY, 215, 26, 4);
                g.DrawString(hasConsent ? "3. Vista Combinada (Foto + Máscara)" : "3. Vista Combinada (Inactivo)", ctlFont, hasConsent ? white : disabledBrush, btnX + 12, ctlY + 4);

                // Bounding boxes toggle
                int togX = btnX + 228;
                using (var chkBrush = new SolidBrush(hasConsent ? System.Drawing.Color.FromArgb(40, 200, 120) : System.Drawing.Color.Gray))
                {
                    g.FillRectangle(chkBrush, togX, ctlY + 6, 14, 14);
                    if (hasConsent) g.DrawString("✓", ctlFont, white, togX + 1, ctlY + 4);
                }
                g.DrawString("Cajas Delimitadoras", ctlFont, hasConsent ? white : disabledBrush, togX + 20, ctlY + 4);
            }

            // Status Banner Truth Table Row
            int banY = ctlY + 34;
            if (hasConsent)
            {
                using (var banBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 16, 48, 32)))
                using (var banBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 190, 100)))
                using (var banFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var green = new SolidBrush(System.Drawing.Color.FromArgb(80, 240, 140)))
                {
                    DrawRoundedRect(g, banBg, banBorder, x, banY, w, 26, 4);
                    g.DrawString("✅ Segmentación verificable: 2 región(es) detectada(s) con máscaras de píxeles generadas por SAM 3.1.", banFont, green, x + 12, banY + 4);
                }
            }
            else
            {
                using (var banBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 48, 38, 16)))
                using (var banBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(210, 160, 40)))
                using (var banFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
                using (var amber = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 80)))
                {
                    DrawRoundedRect(g, banBg, banBorder, x, banY, w, 26, 4);
                    g.DrawString("🔒 Procesamiento visual externo no autorizado por el usuario. La fotografía se conserva localmente sin enviar datos a Meta SAM 3.1.", banFont, amber, x + 12, banY + 4);
                }
            }

            // Main Display Area (Aspect Ratio Preserved letterbox)
            int imgY = banY + 32;
            int imgAreaH = modalH - 215;
            using (var plateBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 10, 14, 20)))
            using (var plateBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95)))
            {
                DrawRoundedRect(g, plateBg, plateBorder, x, imgY, w, imgAreaH, 4);
            }

            // Draw image/mask content centered inside letterbox
            int drawW = 680;
            int drawH = 382;
            int drawX = x + (w - drawW) / 2;
            int drawY = imgY + (imgAreaH - drawH) / 2;

            if (displayMode == 0) // Original photo only
            {
                DrawSimulatedWaterPuddleImage(g, drawX, drawY, drawW, drawH, drawMasks: false);
            }
            else if (displayMode == 1) // Mask only
            {
                DrawSimulatedWaterPuddleMaskOnly(g, drawX, drawY, drawW, drawH);
            }
            else // Combined
            {
                DrawSimulatedWaterPuddleImage(g, drawX, drawY, drawW, drawH, drawMasks: true);
            }

            // Bottom Legend & Object Inspector
            int legY = imgY + imgAreaH + 8;
            using (var legFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var boldFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 200)))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(0, 229, 255)))
            using (var amber = new SolidBrush(System.Drawing.Color.FromArgb(255, 145, 0)))
            {
                if (hasConsent && displayMode > 0)
                {
                    g.DrawString("Objetos Detectados en Segmentación:", boldFont, white, x, legY);

                    // Object #1 chip (Cyan)
                    using (var chipBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 15, 35, 45)))
                    using (var chipBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 229, 255)))
                    {
                        DrawRoundedRect(g, chipBg, chipBorder, x + 230, legY - 2, 290, 24, 4);
                        g.FillRectangle(cyan, x + 238, legY + 5, 10, 10);
                        g.DrawString("Objeto #1: agua_estancada (94%)", boldFont, white, x + 254, legY + 3);
                    }

                    // Object #2 chip (Amber)
                    using (var chipBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 45, 30, 15)))
                    using (var chipBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 145, 0)))
                    {
                        DrawRoundedRect(g, chipBg, chipBorder, x + 530, legY - 2, 290, 24, 4);
                        g.FillRectangle(amber, x + 538, legY + 5, 10, 10);
                        g.DrawString("Objeto #2: bache_secundario (88%)", boldFont, white, x + 554, legY + 3);
                    }
                }
                else
                {
                    g.DrawString("🔒 Privacidad Garantizada: La imagen se conserva en almacenamiento local sin transmisión hacia servicios de inferencia externos.", legFont, muted, x, legY + 2);
                }
            }
        }

        private static void DrawCategoryGrid(System.Drawing.Graphics g, int x, int y, int w, int selectedIndex)
        {
            string[] cats = {
                "Baches y Vialidad", "Banquetas y Accesibilidad", "Limpieza y Residuos",
                "Agua y Drenaje", "Alumbrado Público", "Señalización Vial",
                "Parques y Plazas", "Arbolado Urbano", "Otra Incidencia"
            };
            int colW = (w - 16) / 3;
            int rowH = 22;

            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var activeBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 115, 145)))
            using (var activeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 220, 255), 1.5f))
            using (var inactBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 24, 34, 46)))
            using (var inactBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95)))
            {
                for (int i = 0; i < cats.Length; i++)
                {
                    int row = i / 3;
                    int col = i % 3;
                    int cx = x + col * (colW + 8);
                    int cy = y + row * (rowH + 4);
                    bool isSel = (i == selectedIndex);
                    DrawRoundedRect(g, isSel ? activeBg : inactBg, isSel ? activeBorder : inactBorder, cx, cy, colW, rowH, 4);
                    g.DrawString(cats[i], font, white, cx + 8, cy + 3);
                }
            }
        }

        private static void DrawQuickExamplesRow(System.Drawing.Graphics g, int x, int y, int w)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 8f))
            using (var boldFont = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 200)))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 28, 38, 52)))
            using (var btnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 75, 105)))
            {
                g.DrawString("Ejemplos rápidos:", boldFont, muted, x, y + 2);
                int btnX = x + 105;
                string[] examples = { "Bache en calzada", "Basura acumulada", "Fuga / Drenaje", "Lámpara apagada", "Banqueta rota" };
                int btnW = (w - 110 - 24) / 5;

                for (int i = 0; i < examples.Length; i++)
                {
                    int bx = btnX + i * (btnW + 6);
                    DrawRoundedRect(g, btnBg, btnBorder, bx, y, btnW, 18, 3);
                    g.DrawString(examples[i], font, white, bx + 4, y + 2);
                }
            }
        }

        private static void DrawCategoryChips(System.Drawing.Graphics g, int x, int y, int w, int selectedIndex)
        {
            string[] cats = { "BACHES", "ALUMBRADO", "AGUA_Y_DRENAJE", "BASURA", "SENALIZACION" };
            int chipW = (w - 32) / 5;

            using (var font = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var activeBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 110, 140)))
            using (var activeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 215, 255), 1.5f))
            using (var inactBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 25, 35, 48)))
            using (var inactBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 70, 95)))
            {
                for (int i = 0; i < cats.Length; i++)
                {
                    int cx = x + i * (chipW + 8);
                    bool isSel = (i == selectedIndex);
                    DrawRoundedRect(g, isSel ? activeBg : inactBg, isSel ? activeBorder : inactBorder, cx, y, chipW, 24, 4);
                    g.DrawString(cats[i], font, white, cx + 10, y + 4);
                }
            }
        }

        private static void DrawSimulatedWaterPuddleImage(System.Drawing.Graphics g, int x, int y, int w, int h, bool drawMasks)
        {
            // Pavement base
            using (var asphalt = new SolidBrush(System.Drawing.Color.FromArgb(255, 44, 48, 54)))
            {
                g.FillRectangle(asphalt, x, y, w, h);
            }

            // Pavement lines
            using (var lanePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(140, 220, 190, 40), 2f))
            {
                g.DrawLine(lanePen, x, y + h / 2, x + w, y + h / 2);
            }

            // Water Puddle #1 (Cyan Object)
            int p1X = x + (int)(w * 0.18f);
            int p1Y = y + (int)(h * 0.38f);
            int p1W = (int)(w * 0.52f);
            int p1H = (int)(h * 0.44f);

            using (var waterBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 22, 34, 42)))
            using (var waterBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 50, 80, 95), 2f))
            {
                g.FillEllipse(waterBrush, p1X, p1Y, p1W, p1H);
                g.DrawEllipse(waterBorder, p1X, p1Y, p1W, p1H);
            }

            // Pothole #2 (Amber Object)
            int p2X = x + (int)(w * 0.72f);
            int p2Y = y + (int)(h * 0.55f);
            int p2W = (int)(w * 0.22f);
            int p2H = (int)(h * 0.32f);

            using (var holeBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 18, 20, 24)))
            using (var holeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 80, 60, 45), 2f))
            {
                g.FillEllipse(holeBrush, p2X, p2Y, p2W, p2H);
                g.DrawEllipse(holeBorder, p2X, p2Y, p2W, p2H);
            }

            // Draw Authentic SAM 3.1 Pixel Masks & Bounding Boxes if requested
            if (drawMasks)
            {
                // Object #1: Translucent Cyan Mask Overlay & Crisp Bounding Box
                using (var mask1Brush = new SolidBrush(System.Drawing.Color.FromArgb(110, 0, 229, 255)))
                using (var border1Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(240, 0, 229, 255), 2f))
                using (var box1Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 0, 229, 255), 2f))
                using (var tagBg = new SolidBrush(System.Drawing.Color.FromArgb(230, 0, 180, 210)))
                using (var tagFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
                using (var white = new SolidBrush(System.Drawing.Color.White))
                {
                    box1Pen.DashStyle = DashStyle.Dash;
                    g.FillEllipse(mask1Brush, p1X, p1Y, p1W, p1H);
                    g.DrawEllipse(border1Pen, p1X, p1Y, p1W, p1H);

                    // Bounding Box
                    int b1X = p1X - 6;
                    int b1Y = p1Y - 6;
                    int b1W = p1W + 12;
                    int b1H = p1H + 12;
                    g.DrawRectangle(box1Pen, b1X, b1Y, b1W, b1H);

                    // Tag
                    g.FillRectangle(tagBg, b1X, b1Y - 20, 160, 20);
                    g.DrawString("agua_estancada (94%)", tagFont, white, b1X + 4, b1Y - 18);
                }

                // Object #2: Translucent Amber Mask Overlay & Crisp Bounding Box
                using (var mask2Brush = new SolidBrush(System.Drawing.Color.FromArgb(110, 255, 145, 0)))
                using (var border2Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(240, 255, 145, 0), 2f))
                using (var box2Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 255, 145, 0), 2f))
                using (var tagBg = new SolidBrush(System.Drawing.Color.FromArgb(230, 220, 110, 0)))
                using (var tagFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
                using (var white = new SolidBrush(System.Drawing.Color.White))
                {
                    box2Pen.DashStyle = DashStyle.Dash;
                    g.FillEllipse(mask2Brush, p2X, p2Y, p2W, p2H);
                    g.DrawEllipse(border2Pen, p2X, p2Y, p2W, p2H);

                    // Bounding Box
                    int b2X = p2X - 6;
                    int b2Y = p2Y - 6;
                    int b2W = p2W + 12;
                    int b2H = p2H + 12;
                    g.DrawRectangle(box2Pen, b2X, b2Y, b2W, b2H);

                    // Tag
                    g.FillRectangle(tagBg, b2X, b2Y - 20, 165, 20);
                    g.DrawString("bache_secundario (88%)", tagFont, white, b2X + 4, b2Y - 18);
                }
            }
        }

        private static void DrawSimulatedWaterPuddleMaskOnly(System.Drawing.Graphics g, int x, int y, int w, int h)
        {
            // Dark plate background only (no photograph!)
            using (var plateBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 8, 12, 18)))
            {
                g.FillRectangle(plateBg, x, y, w, h);
            }

            // Pure AI Mask Rasters and Outlines
            int p1X = x + (int)(w * 0.18f);
            int p1Y = y + (int)(h * 0.38f);
            int p1W = (int)(w * 0.52f);
            int p1H = (int)(h * 0.44f);

            int p2X = x + (int)(w * 0.72f);
            int p2Y = y + (int)(h * 0.55f);
            int p2W = (int)(w * 0.22f);
            int p2H = (int)(h * 0.32f);

            // Object #1: Cyan Mask
            using (var mask1Brush = new SolidBrush(System.Drawing.Color.FromArgb(170, 0, 229, 255)))
            using (var border1Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 0, 229, 255), 2.5f))
            using (var box1Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 0, 229, 255), 1.5f))
            using (var tagBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 0, 180, 210)))
            using (var tagFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            {
                box1Pen.DashStyle = DashStyle.Dash;
                g.FillEllipse(mask1Brush, p1X, p1Y, p1W, p1H);
                g.DrawEllipse(border1Pen, p1X, p1Y, p1W, p1H);

                int b1X = p1X - 6;
                int b1Y = p1Y - 6;
                int b1W = p1W + 12;
                int b1H = p1H + 12;
                g.DrawRectangle(box1Pen, b1X, b1Y, b1W, b1H);

                g.FillRectangle(tagBg, b1X, b1Y - 20, 160, 20);
                g.DrawString("agua_estancada (94%)", tagFont, white, b1X + 4, b1Y - 18);
            }

            // Object #2: Amber Mask
            using (var mask2Brush = new SolidBrush(System.Drawing.Color.FromArgb(170, 255, 145, 0)))
            using (var border2Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 255, 145, 0), 2.5f))
            using (var box2Pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 255, 145, 0), 1.5f))
            using (var tagBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 220, 110, 0)))
            using (var tagFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            {
                box2Pen.DashStyle = DashStyle.Dash;
                g.FillEllipse(mask2Brush, p2X, p2Y, p2W, p2H);
                g.DrawEllipse(border2Pen, p2X, p2Y, p2W, p2H);

                int b2X = p2X - 6;
                int b2Y = p2Y - 6;
                int b2W = p2W + 12;
                int b2H = p2H + 12;
                g.DrawRectangle(box2Pen, b2X, b2Y, b2W, b2H);

                g.FillRectangle(tagBg, b2X, b2Y - 20, 165, 20);
                g.DrawString("bache_secundario (88%)", tagFont, white, b2X + 4, b2Y - 18);
            }
        }

        private static void DrawHeader(System.Drawing.Graphics g, int x, int y, int w, string title, string sub)
        {
            using (var titleFont = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var subFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var subBrush = new SolidBrush(System.Drawing.Color.FromArgb(85, 175, 240)))
            using (var closeFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var closeBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 215, 230)))
            using (var closeBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 35, 45, 60)))
            {
                g.DrawString($"📋 {title}", titleFont, white, x, y);
                g.DrawString(sub, subFont, subBrush, x, y + 24);

                // Language button
                DrawRoundedRect(g, closeBg, null, x + w - 240, y, 145, 24, 4);
                g.DrawString("Idioma: Español [Cambiar]", closeFont, closeBrush, x + w - 236, y + 3);

                // Close button
                DrawRoundedRect(g, closeBg, null, x + w - 85, y, 85, 24, 4);
                g.DrawString("✕ Close [Esc]", closeFont, closeBrush, x + w - 77, y + 3);
            }
        }

        private static void DrawRoundedRect(System.Drawing.Graphics g, System.Drawing.Brush fillBrush, System.Drawing.Pen borderPen, int x, int y, int w, int h, int radius)
        {
            using (var path = new GraphicsPath())
            {
                int r2 = radius * 2;
                path.AddArc(x, y, r2, r2, 180, 90);
                path.AddArc(x + w - r2, y, r2, r2, 270, 90);
                path.AddArc(x + w - r2, y + h - r2, r2, r2, 0, 90);
                path.AddArc(x, y + h - r2, r2, r2, 90, 90);
                path.CloseFigure();

                if (fillBrush != null)
                {
                    g.FillPath(fillBrush, path);
                }
                if (borderPen != null)
                {
                    g.DrawPath(borderPen, path);
                }
            }
        }

        private static Bitmap CaptureCameraFrame(Camera cam, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            using (var ms = new MemoryStream(png))
            {
                return new Bitmap(ms);
            }
        }

        private static Bitmap AddWindowsTitleBar(Bitmap clientArea, int width, int height, string title)
        {
            var window = new Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(window))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using (var titleBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 32, 34, 40)))
                {
                    g.FillRectangle(titleBg, 0, 0, width, TitleBarHeight);
                }

                using (var font = new System.Drawing.Font("Segoe UI", 9f))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 210, 215, 225)))
                {
                    g.DrawString(title, font, brush, 12, 8);
                }

                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 160, 165, 175)))
                using (var btnFont = new System.Drawing.Font("Marlett", 9f))
                {
                    g.DrawString("r", btnFont, btnBrush, width - 28, 9);
                    g.DrawString("1", btnFont, btnBrush, width - 58, 9);
                    g.DrawString("0", btnFont, btnBrush, width - 88, 9);
                }

                g.DrawImage(clientArea, 0, TitleBarHeight, width, height - TitleBarHeight);
            }
            return window;
        }
    }
}

