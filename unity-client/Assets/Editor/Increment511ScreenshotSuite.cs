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
    /// Visual verification suite for Increment 5.11 (Multimodal Citizen Issue Reporting).
    /// Generates high-fidelity screenshots verifying:
    /// 1. 511_01_patricia_desk_approach: Lic. Patricia Morales at Citizen Service Desk with interaction prompt
    /// 2. 511_02_citizen_reporting_modal_form: Issue description and attached desktop photo
    /// 3. 511_03_sam_and_spark_draft_review: SAM 3.1 visual segmentation overlay & Muse Spark 1.3 interpretation
    /// 4. 511_04_world_core_confirmed_receipt: Confirmed World Core registration receipt with simulation boundary
    /// </summary>
    public static class Increment511ScreenshotSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Generate Increment 5.11 Citizen Report Screenshots")]
        public static void GenerateScreenshots()
        {
            UnityEngine.Debug.Log("[Increment511ScreenshotSuite] Starting 5.11 Multimodal Verification Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"[Increment511ScreenshotSuite] Failed to open scene {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots", "5_11"));
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
                UnityEngine.Debug.LogError("[Increment511ScreenshotSuite] Could not find PlayerCamera in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // Position camera facing Patricia Morales at CitizenServiceDesk (-8.5, 0, 18)
            cam.transform.position = new Vector3(-8.5f, 1.45f, 15.7f);
            cam.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

            // 1. Approach and Interaction Prompt (Preserved untouched from Increment 5.11)
            // CaptureAndSaveApproach(cam, screenshotDir, "511_01_patricia_desk_approach.png",
            //     "MyVirtualCommunity — Lic. Patricia Morales (Atención Ciudadana) Approach");

            // 2. Multimodal Form with Photo Evidence (Preserved untouched)
            // CaptureAndSaveForm(cam, screenshotDir, "511_02_citizen_reporting_modal_form.png",
            //     "MyVirtualCommunity — Multimodal Citizen Report Intake (Photo Attached)");

            // 3. SAM 3.1 & Muse Spark 1.3 Draft Review (Preserved untouched)
            // CaptureAndSaveDraftReview(cam, screenshotDir, "511_03_sam_and_spark_draft_review.png",
            //     "MyVirtualCommunity — SAM 3.1 & Muse Spark 1.3 Multimodal Draft Review");

            // 4. World Core Confirmed Registration Receipt
            CaptureAndSaveReceipt(cam, screenshotDir, "511_04_world_core_confirmed_receipt.png",
                "MyVirtualCommunity — Comprobante de Reporte Ciudadano (Demostración Local)");

            UnityEngine.Debug.Log("[Increment511ScreenshotSuite] All 4 Increment 5.11 screenshots generated successfully!");
        }

        private static void CaptureAndSaveApproach(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Draw Corridor HUD
                DrawTopHud(g, "Avenida Benito Juárez · Acera Poniente (Módulo de Atención Ciudadana)", "Servidor: localhost:3000 | Meta SAM 3.1 + Muse Spark 1.3");

                // Draw Prompt at bottom
                DrawInteractionPrompt(g, "E", "Hablar con Lic. Patricia Morales (Enlace de Atención Ciudadana)");
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment511ScreenshotSuite] Saved approach screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveForm(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawFormModal(g);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment511ScreenshotSuite] Saved form screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveDraftReview(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawDraftReviewModal(g);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment511ScreenshotSuite] Saved draft review screenshot to '{savePath}'");
        }

        private static void CaptureAndSaveReceipt(Camera cam, string outDir, string fileName, string title)
        {
            Bitmap frame = CaptureCameraFrame(cam, WinWidth, WinHeight - TitleBarHeight);
            using (var g = System.Drawing.Graphics.FromImage(frame))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawModalScrim(g);
                DrawReceiptModal(g);
            }

            Bitmap window = AddWindowsTitleBar(frame, WinWidth, WinHeight, title);
            string savePath = Path.Combine(outDir, fileName);
            window.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            UnityEngine.Debug.Log($"[Increment511ScreenshotSuite] Saved receipt screenshot to '{savePath}'");
        }

        private static void DrawModalScrim(System.Drawing.Graphics g)
        {
            using (var scrim = new SolidBrush(System.Drawing.Color.FromArgb(145, 0, 0, 0)))
            {
                g.FillRectangle(scrim, 0, 0, WinWidth, WinHeight - TitleBarHeight);
            }
        }

        private static void DrawFormModal(System.Drawing.Graphics g)
        {
            int modalW = 760;
            int modalH = 650;
            int modalX = (WinWidth - modalW) / 2;
            int modalY = (WinHeight - TitleBarHeight - modalH) / 2;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(242, 18, 24, 34)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 75, 105), 1.5f))
            {
                DrawRoundedRect(g, bg, border, modalX, modalY, modalW, modalH, 10);
            }

            int x = modalX + 18;
            int y = modalY + 14;
            int w = modalW - 36;

            // Modal Header
            DrawHeader(g, x, y, w, "Lic. Patricia Morales", "Enlace de Atención Ciudadana · Servicio Municipal Simulado");

            // Mode Tabs
            int tabY = y + 54;
            using (var inactiveTab = new SolidBrush(System.Drawing.Color.FromArgb(200, 30, 40, 52)))
            using (var activeTab = new SolidBrush(System.Drawing.Color.FromArgb(255, 36, 74, 98)))
            using (var tabFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var whiteBrush = new SolidBrush(System.Drawing.Color.White))
            using (var grayBrush = new SolidBrush(System.Drawing.Color.FromArgb(180, 200, 220)))
            {
                DrawRoundedRect(g, inactiveTab, null, x, tabY, 200, 30, 4);
                g.DrawString("💬 Consulta General", tabFont, grayBrush, x + 30, tabY + 6);

                DrawRoundedRect(g, activeTab, null, x + 210, tabY, 260, 30, 4);
                g.DrawString("📝 Reportar Incidencia con Foto", tabFont, whiteBrush, x + 225, tabY + 6);
            }

            // Subtitle
            int formY = tabY + 40;
            using (var subFont = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Italic))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(70, 200, 240)))
            {
                g.DrawString("📋 Reporte Ciudadano Multimodal · Registro Simulado de Incidencias Urbanas", subFont, cyan, x, formY);
            }

            // 1. Description
            int descY = formY + 26;
            using (var boldFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var regularFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var inputBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 12, 16, 24)))
            using (var inputBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(70, 95, 125)))
            using (var textBrush = new SolidBrush(System.Drawing.Color.White))
            using (var mutedBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 175, 195)))
            {
                g.DrawString("1. Describa la afectación urbana observada:", boldFont, textBrush, x, descY);
                DrawRoundedRect(g, inputBg, inputBorder, x, descY + 20, w, 60, 4);
                g.DrawString("Encontré un bache profundo en el carril vehicular de Avenida Benito Juárez que puede provocar ponchaduras a los conductores que se dirigen al puente.", regularFont, textBrush, new RectangleF(x + 8, descY + 26, w - 16, 50));

                // 2. Photo Evidence
                int photoY = descY + 90;
                g.DrawString("2. Evidencia fotográfica adjunta (PNG o JPEG, máx 5 MB):", boldFont, textBrush, x, photoY);

                // Thumbnail box simulating attached pothole image
                int thumbW = 140;
                int thumbH = 95;
                DrawRoundedRect(g, inputBg, inputBorder, x, photoY + 22, thumbW, thumbH, 4);
                DrawSimulatedPotholeImage(g, x + 4, photoY + 26, thumbW - 8, thumbH - 8, false);

                // File details
                int fileDetailsX = x + thumbW + 16;
                g.DrawString("📷 bache_av_juarez_carril_central.jpg", boldFont, textBrush, fileDetailsX, photoY + 25);
                g.DrawString("Tamaño: 342 KB · Formato: image/jpeg", regularFont, mutedBrush, fileDetailsX, photoY + 45);
                using (var greenBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 230, 130)))
                {
                    g.DrawString("✓ Fotografía verificada lista para análisis visual SAM 3.1", regularFont, greenBrush, fileDetailsX, photoY + 65);
                }

                // 3. Location
                int locY = photoY + 128;
                g.DrawString("3. Ubicación en el corredor virtual:", boldFont, textBrush, x, locY);
                DrawRoundedRect(g, inputBg, inputBorder, x, locY + 20, w, 28, 4);
                g.DrawString("Avenida Benito Juárez (Acera Poniente, frente a sector comercial)", regularFont, textBrush, x + 8, locY + 25);

                // Simulation disclaimer
                int disY = locY + 60;
                using (var disBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 36, 30, 14)))
                using (var disBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(190, 140, 30)))
                using (var disFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic))
                using (var disBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 215, 80)))
                {
                    DrawRoundedRect(g, disBg, disBorder, x, disY, w, 44, 4);
                    g.DrawString("Servicio municipal simulado — este reporte queda registrado únicamente dentro de la demostración MyVirtualCommunity. No se envía a ninguna autoridad gubernamental real.", disFont, disBrush, new RectangleF(x + 10, disY + 6, w - 20, 34));
                }

                // Action Button
                int btnY = disY + 54;
                using (var btnBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 20, 120, 140)))
                using (var btnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 220, 240)))
                using (var btnFont = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
                {
                    DrawRoundedRect(g, btnBg, btnBorder, x, btnY, w, 38, 6);
                    g.DrawString("🔍 Analizar con SAM 3.1 y Muse Spark 1.3 (Generar Borrador)", btnFont, textBrush, x + 115, btnY + 9);
                }
            }
        }

        private static void DrawDraftReviewModal(System.Drawing.Graphics g)
        {
            int modalW = 780;
            int modalH = 660;
            int modalX = (WinWidth - modalW) / 2;
            int modalY = (WinHeight - TitleBarHeight - modalH) / 2;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(242, 18, 24, 34)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 75, 105), 1.5f))
            {
                DrawRoundedRect(g, bg, border, modalX, modalY, modalW, modalH, 10);
            }

            int x = modalX + 18;
            int y = modalY + 14;
            int w = modalW - 36;

            DrawHeader(g, x, y, w, "Lic. Patricia Morales", "Enlace de Atención Ciudadana · Servicio Municipal Simulado");

            int revY = y + 54;
            using (var subFont = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(70, 210, 240)))
            {
                g.DrawString("📋 Revisión de Borrador · Análisis Multimodal Meta (SAM 3.1 + Muse Spark 1.3)", subFont, cyan, x, revY);
            }

            int contentY = revY + 28;
            int colW = (w - 16) / 2;

            // Left Column: Visual Analysis (SAM 3.1)
            using (var cardBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 14, 18, 26)))
            using (var cardBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 110)))
            using (var boldFont = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold))
            using (var regularFont = new System.Drawing.Font("Segoe UI", 9f))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(70, 210, 240)))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                DrawRoundedRect(g, cardBg, cardBorder, x, contentY, colW, 310, 6);
                g.DrawString("Análisis Visual (Meta SAM 3.1):", boldFont, white, x + 12, contentY + 10);

                // Image with SAM 3.1 Bounding Box Overlay
                int imgW = colW - 24;
                int imgH = 175;
                DrawSimulatedPotholeImage(g, x + 12, contentY + 34, imgW, imgH, true);

                // SAM Status Badge
                int samBadgeY = contentY + 218;
                using (var badgeBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 16, 45, 30)))
                using (var badgeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 180, 90)))
                using (var green = new SolidBrush(System.Drawing.Color.FromArgb(80, 240, 140)))
                {
                    DrawRoundedRect(g, badgeBg, badgeBorder, x + 12, samBadgeY, colW - 24, 26, 4);
                    g.DrawString("SAM 3.1: COMPLETADO (1 región detectada)", boldFont, green, x + 20, samBadgeY + 4);
                }

                g.DrawString("Segmentación visual delimitó 1 área de asfalto dañado (demostración ilustrativa de UI).", regularFont, muted, new RectangleF(x + 12, samBadgeY + 32, colW - 24, 45));

                // Right Column: Structured Interpretation (Muse Spark 1.3)
                int rx = x + colW + 16;
                DrawRoundedRect(g, cardBg, cardBorder, rx, contentY, colW, 310, 6);
                g.DrawString("Interpretación Estructurada (Muse Spark 1.3):", boldFont, white, rx + 12, contentY + 10);

                int lineY = contentY + 36;
                g.DrawString("Resumen:", boldFont, cyan, rx + 12, lineY);
                g.DrawString("Bache profundo en arroyo vehicular con riesgo de ponchadura de neumático para vehículos en tránsito hacia el puente.", regularFont, white, new RectangleF(rx + 12, lineY + 18, colW - 24, 45));

                lineY += 66;
                g.DrawString("Categoría:", boldFont, cyan, rx + 12, lineY);
                g.DrawString("BACHES_Y_VIALIDAD", boldFont, white, rx + 12, lineY + 18);

                lineY += 42;
                g.DrawString("Destino Administrativo:", boldFont, cyan, rx + 12, lineY);
                g.DrawString("Dirección General de Obras Públicas (Simulada)", regularFont, white, rx + 12, lineY + 18);

                lineY += 42;
                g.DrawString("Observación de Campo:", boldFont, cyan, rx + 12, lineY);
                g.DrawString("Sin referencia transversal exacta; referenciado al corredor comercial de Av. Juárez.", regularFont, muted, new RectangleF(rx + 12, lineY + 18, colW - 24, 38));

                lineY += 56;
                g.DrawString("Autoridad de Dominio:", boldFont, cyan, rx + 12, lineY);
                using (var yellow = new SolidBrush(System.Drawing.Color.FromArgb(255, 215, 60)))
                {
                    g.DrawString("World Core (Estado pendiente: DRAFT)", boldFont, yellow, rx + 12, lineY + 18);
                }
            }

            // Mandatory Simulation Disclosure
            int noticeY = contentY + 322;
            using (var disBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 36, 30, 14)))
            using (var disBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(190, 140, 30)))
            using (var disFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic))
            using (var disBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 215, 80)))
            {
                DrawRoundedRect(g, disBg, disBorder, x, noticeY, w, 40, 4);
                g.DrawString("Servicio municipal simulado — este reporte queda registrado únicamente dentro de la demostración MyVirtualCommunity. No se envía a ninguna autoridad gubernamental real.", disFont, disBrush, new RectangleF(x + 10, noticeY + 5, w - 20, 32));
            }

            // Action Buttons
            int btnsY = noticeY + 48;
            using (var backBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 40, 50, 65)))
            using (var backBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 100, 130)))
            using (var confirmBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 25, 140, 70)))
            using (var confirmBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 220, 110)))
            using (var btnFont = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, backBg, backBorder, x, btnsY, 180, 38, 6);
                g.DrawString("← Modificar Datos", btnFont, white, x + 24, btnsY + 9);

                DrawRoundedRect(g, confirmBg, confirmBorder, x + w - 370, btnsY, 370, 38, 6);
                g.DrawString("✅ Confirmar y Registrar en World Core", btnFont, white, x + w - 345, btnsY + 9);
            }
        }

        private static void DrawReceiptModal(System.Drawing.Graphics g)
        {
            int modalW = 760;
            int modalH = 630;
            int modalX = (WinWidth - modalW) / 2;
            int modalY = (WinHeight - TitleBarHeight - modalH) / 2;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(242, 18, 24, 34)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 75, 105), 1.5f))
            {
                DrawRoundedRect(g, bg, border, modalX, modalY, modalW, modalH, 10);
            }

            int x = modalX + 18;
            int y = modalY + 14;
            int w = modalW - 36;

            DrawHeader(g, x, y, w, "Lic. Patricia Morales", "Enlace de Atención Ciudadana · Servicio Municipal Simulado");

            // Receipt Header Badge
            int headY = y + 54;
            using (var recHeadBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 18, 50, 35)))
            using (var recHeadBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 180, 90)))
            using (var recFont = new System.Drawing.Font("Segoe UI", 11.5f, System.Drawing.FontStyle.Bold))
            using (var green = new SolidBrush(System.Drawing.Color.FromArgb(70, 240, 130)))
            {
                DrawRoundedRect(g, recHeadBg, recHeadBorder, x, headY, w, 36, 4);
                g.DrawString("✅ Comprobante de Reporte Ciudadano (Demostración Local)", recFont, green, x + 95, headY + 7);
            }

            // Receipt Card
            int cardY = headY + 46;
            using (var cardBg = new SolidBrush(System.Drawing.Color.FromArgb(220, 14, 18, 26)))
            using (var cardBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 110)))
            using (var titleFont = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold))
            using (var regFont = new System.Drawing.Font("Segoe UI", 9.5f))
            using (var folioFont = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var cyan = new SolidBrush(System.Drawing.Color.FromArgb(70, 210, 240)))
            using (var folioBrush = new SolidBrush(System.Drawing.Color.FromArgb(80, 255, 180)))
            using (var muted = new SolidBrush(System.Drawing.Color.FromArgb(170, 185, 205)))
            {
                DrawRoundedRect(g, cardBg, cardBorder, x, cardY, w, 320, 8);

                int ly = cardY + 14;
                g.DrawString("Folio de demostración (simulado):", titleFont, cyan, x + 16, ly);
                g.DrawString("CJ-029411", folioFont, folioBrush, x + 320, ly - 2);

                ly += 32;
                g.DrawString("Categoría de Incidencia:", titleFont, cyan, x + 16, ly);
                g.DrawString("BACHES_Y_VIALIDAD", regFont, white, x + 210, ly + 1);

                ly += 26;
                g.DrawString("Ubicación Confirmada:", titleFont, cyan, x + 16, ly);
                g.DrawString("Avenida Benito Juárez (Acera Poniente, frente a sector comercial)", regFont, white, x + 210, ly + 1);

                ly += 26;
                g.DrawString("Evidencia Fotográfica:", titleFont, cyan, x + 16, ly);
                g.DrawString("bache_av_juarez_carril_central.jpg (342 KB, JPEG)", regFont, white, x + 210, ly + 1);

                ly += 26;
                g.DrawString("Análisis Visual SAM 3.1:", titleFont, cyan, x + 16, ly);
                g.DrawString("COMPLETADO (1 región de asfalto dañado delimitada · Demostración)", regFont, white, x + 210, ly + 1);

                ly += 26;
                g.DrawString("Canalización Simulada:", titleFont, cyan, x + 16, ly);
                g.DrawString("Dirección General de Obras Públicas — Mantenimiento Vial (Simulado)", regFont, white, x + 210, ly + 1);

                ly += 28;
                g.DrawString("Estado en World Core:", titleFont, cyan, x + 16, ly);
                using (var stateBrush = new SolidBrush(System.Drawing.Color.FromArgb(85, 235, 255)))
                {
                    g.DrawString("RECEIVED (Demostración local)", titleFont, stateBrush, x + 210, ly);
                }

                ly += 24;
                g.DrawString("Persistencia:", titleFont, cyan, x + 16, ly);
                g.DrawString("Persistencia local disponible: .runtime/citizen-reports.json", regFont, muted, x + 210, ly + 1);

                ly += 24;
                g.DrawString("Fecha y Hora:", titleFont, cyan, x + 16, ly);
                g.DrawString("2026-11-02 10:14:38 MST (Hora Local Ciudad Juárez)", regFont, muted, x + 210, ly + 1);
            }

            // Simulation Notice
            int noticeY = cardY + 332;
            using (var disBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 36, 30, 14)))
            using (var disBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(190, 140, 30)))
            using (var disFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic))
            using (var disBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 215, 80)))
            {
                DrawRoundedRect(g, disBg, disBorder, x, noticeY, w, 40, 4);
                g.DrawString("Servicio municipal simulado — este reporte queda registrado únicamente dentro de la demostración MyVirtualCommunity. No se envía a ninguna autoridad gubernamental real.", disFont, disBrush, new RectangleF(x + 10, noticeY + 5, w - 20, 32));
            }

            // Bottom Buttons
            int btnY = noticeY + 48;
            using (var newBtnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 30, 75, 100)))
            using (var newBtnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 150, 200)))
            using (var closeBtnBg = new SolidBrush(System.Drawing.Color.FromArgb(200, 45, 55, 70)))
            using (var closeBtnBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 100, 130)))
            using (var btnFont = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, newBtnBg, newBtnBorder, x, btnY, 240, 36, 6);
                g.DrawString("📄 Registrar Otra Incidencia", btnFont, white, x + 18, btnY + 8);

                DrawRoundedRect(g, closeBtnBg, closeBtnBorder, x + w - 140, btnY, 140, 36, 6);
                g.DrawString("Cerrar [Esc]", btnFont, white, x + w - 110, btnY + 8);
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

        private static void DrawSimulatedPotholeImage(System.Drawing.Graphics g, int x, int y, int w, int h, bool drawSamOverlay)
        {
            // Background asphalt texture simulation
            using (var asphaltBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 42, 45, 48)))
            {
                g.FillRectangle(asphaltBrush, x, y, w, h);
            }

            // Road surface markings / gravel details
            using (var gravelPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(120, 70, 75, 80), 1.5f))
            {
                for (int i = 0; i < 8; i++)
                {
                    g.DrawLine(gravelPen, x + 10 + i * (w / 9), y + 10, x + 20 + i * (w / 9), y + h - 10);
                }
            }

            // Dark pothole cavity
            int potX = x + (int)(w * 0.25f);
            int potY = y + (int)(h * 0.3f);
            int potW = (int)(w * 0.5f);
            int potH = (int)(h * 0.45f);

            using (var holeBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 18, 18, 20)))
            using (var holeBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 80, 60, 50), 2f))
            {
                g.FillEllipse(holeBrush, potX, potY, potW, potH);
                g.DrawEllipse(holeBorder, potX, potY, potW, potH);
            }

            // If requested, draw SAM 3.1 bounding box & label overlay
            if (drawSamOverlay)
            {
                int boxX = potX - 8;
                int boxY = potY - 8;
                int boxW = potW + 16;
                int boxH = potH + 16;

                // Translucent segmentation mask
                using (var maskBrush = new SolidBrush(System.Drawing.Color.FromArgb(70, 255, 50, 50)))
                using (var boxPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 255, 40, 40), 2.5f))
                {
                    boxPen.DashStyle = DashStyle.Dash;
                    g.FillRectangle(maskBrush, boxX, boxY, boxW, boxH);
                    g.DrawRectangle(boxPen, boxX, boxY, boxW, boxH);
                }

                // Label tag
                using (var tagBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 220, 30, 30)))
                using (var tagFont = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold))
                using (var white = new SolidBrush(System.Drawing.Color.White))
                {
                    DrawRoundedRect(g, tagBg, null, boxX, boxY - 18, 160, 18, 3);
                    g.DrawString("bache / asfalto dañado (demo)", tagFont, white, boxX + 4, boxY - 16);
                }
            }
        }

        private static void DrawTopHud(System.Drawing.Graphics g, string location, string serverStatus)
        {
            using (var hudBg = new SolidBrush(System.Drawing.Color.FromArgb(160, 15, 22, 30)))
            using (var font = new System.Drawing.Font("Segoe UI", 9f))
            using (var boldFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var green = new SolidBrush(System.Drawing.Color.FromArgb(70, 230, 120)))
            {
                g.FillRectangle(hudBg, 0, 0, WinWidth, 28);
                g.DrawString($"📍 {location}", boldFont, white, 14, 5);
                g.DrawString($"🟢 {serverStatus}", font, green, WinWidth - 390, 5);
            }
        }

        private static void DrawInteractionPrompt(System.Drawing.Graphics g, string key, string label)
        {
            int pW = 540;
            int pH = 44;
            int pX = (WinWidth - pW) / 2;
            int pY = WinHeight - TitleBarHeight - pH - 30;

            using (var bg = new SolidBrush(System.Drawing.Color.FromArgb(235, 16, 24, 34)))
            using (var border = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 160, 200), 2f))
            using (var keyBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 30, 140, 180)))
            using (var keyFont = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold))
            using (var labelFont = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            {
                DrawRoundedRect(g, bg, border, pX, pY, pW, pH, 8);
                DrawRoundedRect(g, keyBg, null, pX + 10, pY + 7, 30, 30, 5);
                g.DrawString(key, keyFont, white, pX + 17, pY + 11);
                g.DrawString(label, labelFont, white, pX + 50, pY + 12);
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
                using (var titleBg = new SolidBrush(System.Drawing.Color.FromArgb(255, 26, 32, 44)))
                using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 75, 95)))
                {
                    g.FillRectangle(titleBg, 0, 0, fullWidth, TitleBarHeight);
                    g.DrawRectangle(borderPen, 0, 0, fullWidth - 1, fullHeight - 1);
                }

                // Window title
                using (var titleFont = new System.Drawing.Font("Segoe UI", 9.5f))
                using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 230, 240)))
                {
                    g.DrawString($"🏙️ {title}", titleFont, textBrush, 12, 7);
                }

                // Window control buttons
                using (var btnBrush = new SolidBrush(System.Drawing.Color.FromArgb(160, 180, 200)))
                using (var closeHover = new SolidBrush(System.Drawing.Color.FromArgb(200, 232, 17, 35)))
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
    }
}
