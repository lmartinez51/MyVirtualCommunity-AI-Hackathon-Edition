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
    /// Adds the "CHIHUAHUA LES DESEA / FELIZ VIAJE" sign face onto GantrySign_Board_FelizViaje
    /// using a high-res 8:1 texture on a dedicated Quad offset 0.04m in front of the board
    /// to completely eliminate any Z-fighting / flickering.
    /// </summary>
    public static class BridgeGantrySignBuilder
    {
        private const string SignFaceName = "GantrySign_FrontFace_FelizViaje";
        private const string TextureAssetPath = "Assets/Textures/Environment/T_Bridge_Gantry_FelizViaje.png";
        private const string MaterialAssetPath = "Assets/Settings/GantrySign_FelizViaje.mat";

        [MenuItem("MyVirtualCommunity/Update Gantry Sign Feliz Viaje")]
        public static void ExecuteGantrySign()
        {
            const string scenePath = "Assets/Scenes/BorderCorridorScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            GameObject board = GameObject.Find("GantrySign_Board_FelizViaje");
            if (board == null)
            {
                Debug.LogError("[BridgeGantrySignBuilder] GantrySign_Board_FelizViaje not found!");
                return;
            }

            // 1. Generate high-resolution 8:1 (2048x256) highway sign texture
            GenerateSignTexture();

            // 2. Create/update material
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialAssetPath);
            }
            mat.mainTexture = tex;
            mat.color = UnityEngine.Color.white;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", UnityEngine.Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // 3. Remove any previous face object to keep idempotency
            GameObject existing = GameObject.Find(SignFaceName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            // 4. Create Quad offset 0.04m in front of the board's South (-Z) face
            //    to guarantee zero coplanar Z-fighting / flickering at any camera distance.
            Transform bt = board.transform;
            Vector3 boardScale = bt.lossyScale;
            float frontOffset = boardScale.z * 0.5f + 0.04f; // 0.10 + 0.04 = 0.14m from board center

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = SignFaceName;
            face.transform.position = bt.position - bt.forward * frontOffset;
            face.transform.rotation = bt.rotation; // Unity Quad normal is -Z (faces South toward approaching traffic)
            face.transform.localScale = new Vector3(boardScale.x, boardScale.y, 1f);
            UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());

            var mr = face.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // 5. Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // 6. Capture close-up & overview verification screenshots
            CaptureScreenshots();

            Debug.Log("[BridgeGantrySignBuilder] GANTRY SIGN 'FELIZ VIAJE' UPDATED SUCCESSFULLY!");
        }

        private static void GenerateSignTexture()
        {
            const int width = 2048;
            const int height = 256;

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), TextureAssetPath.Replace('/', Path.DirectorySeparatorChar));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var bmp = new Bitmap(width, height))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // Highway sign green background (#0B785B)
                System.Drawing.Color signGreen = System.Drawing.Color.FromArgb(11, 120, 91);
                System.Drawing.Color offWhite = System.Drawing.Color.FromArgb(242, 244, 242);

                g.Clear(signGreen);

                // Thin white border inset
                using (var borderPen = new Pen(offWhite, 6f))
                {
                    g.DrawRectangle(borderPen, 10, 10, width - 20, height - 20);
                }

                // Top-left emblem: Chihuahua shield + label
                DrawChihuahuaEmblem(g, 42, 28, offWhite);

                // Top-right emblem: Fideicomiso bridge arch + label
                DrawFideicomisoEmblem(g, width - 310, 26, offWhite);

                // Center top line: "CHIHUAHUA LES DESEA"
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var subFont = new System.Drawing.Font("Arial", 32f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var mainFont = new System.Drawing.Font("Arial", 122f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(offWhite))
                {
                    g.DrawString("CHIHUAHUA LES DESEA", subFont, brush, new RectangleF(320f, 18f, width - 640f, 52f), sf);
                    g.DrawString("FELIZ VIAJE", mainFont, brush, new RectangleF(140f, 68f, width - 280f, 174f), sf);
                }

                bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(TextureAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 8;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static void DrawChihuahuaEmblem(System.Drawing.Graphics g, float x, float y, System.Drawing.Color col)
        {
            using (var pen = new Pen(col, 3.5f))
            using (var brush = new SolidBrush(col))
            using (var titleFont = new System.Drawing.Font("Arial", 17f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
            using (var smallFont = new System.Drawing.Font("Arial", 10f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
            {
                // Shield outline
                PointF[] shield = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x + 50, y),
                    new PointF(x + 50, y + 36),
                    new PointF(x + 25, y + 58),
                    new PointF(x, y + 36)
                };
                g.DrawPolygon(pen, shield);
                g.DrawLine(pen, x + 8, y + 18, x + 42, y + 18);
                g.DrawLine(pen, x + 25, y + 6, x + 25, y + 48);

                g.DrawString("CHIHUAHUA", titleFont, brush, x + 60, y + 6);
                g.DrawString("GOBIERNO DEL ESTADO", smallFont, brush, x + 60, y + 28);
            }
        }

        private static void DrawFideicomisoEmblem(System.Drawing.Graphics g, float x, float y, System.Drawing.Color col)
        {
            using (var pen = new Pen(col, 3.5f))
            using (var brush = new SolidBrush(col))
            using (var smallFont = new System.Drawing.Font("Arial", 11f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
            {
                // Stylized bridge arch + roadway
                g.DrawArc(pen, x + 6, y + 12, 54, 44, 180, 180);
                g.DrawLine(pen, x, y + 38, x + 66, y + 38);
                g.DrawLine(pen, x + 18, y + 54, x + 28, y + 20);
                g.DrawLine(pen, x + 48, y + 54, x + 38, y + 20);

                g.DrawString("FIDEICOMISO", smallFont, brush, x + 76, y + 6);
                g.DrawString("DE PUENTES", smallFont, brush, x + 76, y + 20);
                g.DrawString("FRONTERIZOS", smallFont, brush, x + 76, y + 34);
                g.DrawString("DE CHIHUAHUA", smallFont, brush, x + 76, y + 48);
            }
        }

        private static void CaptureScreenshots()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempGantrySignCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;

            // Shot 1: Close-up on GantrySign_Board_FelizViaje
            camGo.transform.position = new Vector3(4.00f, 6.80f, 47.00f);
            camGo.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            string p1 = Path.Combine(outDir, "gantry_sign_shot1_closeup.png");
            CaptureShot(cam, p1);
            CopyFile(p1, Path.Combine(brainDir, "gantry_sign_shot1_closeup.png"));

            // Shot 2: Webcam overview with gantry sign + 4 lanes + white bollards
            camGo.transform.position = new Vector3(4.00f, 9.50f, 48.00f);
            camGo.transform.rotation = Quaternion.Euler(14.0f, 1.5f, 0.0f);
            string p2 = Path.Combine(outDir, "gantry_sign_shot2_webcam_overview.png");
            CaptureShot(cam, p2);
            CopyFile(p2, Path.Combine(brainDir, "gantry_sign_shot2_webcam_overview.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void CaptureShot(Camera cam, string savePath)
        {
            int w = 1280;
            int h = 720;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(savePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void CopyFile(string src, string dst)
        {
            try
            {
                if (File.Exists(src)) File.Copy(src, dst, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BridgeGantrySignBuilder] Could not copy {src} to {dst}: {ex.Message}");
            }
        }
    }
}

