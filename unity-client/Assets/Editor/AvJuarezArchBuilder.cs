using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MyVirtualCommunity.Client.World;
using UColor = UnityEngine.Color;
using GColor = System.Drawing.Color;
using GFont = System.Drawing.Font;
using UFont = UnityEngine.Font;
using UImage = UnityEngine.UI.Image;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Builds the Avenida Benito Juárez Gateway Arch ("CIUDAD JUÁREZ / BIENVENIDOS")
    /// just north of the Calle Azucenas corner (Z = -23.50m) faithfully matching the
    /// real-world lattice steel structure, tripartite billboard crown, side ROGER panels,
    /// LED garland bulbs, "15 km/h MÁXIMO" traffic sign, and interactive/live World-Space
    /// central LED display driven by JuarezArchDisplayController.
    /// </summary>
    public static class AvJuarezArchBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string ArchRootName = "AvJuarez_DigitalArch_Root";

        // Geometry & Placement Constants (verified against BorderCorridorScene.unity)
        // Road_AvenidaJuarez curb-to-curb asphalt: X = -2.60 .. +6.80 (center X = +2.10m, width = 9.40m)
        // Curb_East South Cut (Calle Azucenas corner): Z = -26.12m
        // First block corner facades & streetlights: Z = -21.50m / -20.50m
        public const float ArchCenterX = 2.10f;
        public const float ArchZ = -23.50f;
        public const float WestColumnX = -3.15f;
        public const float EastColumnX = 7.35f;
        public const float SidewalkBaseY = 0.20f;
        public const float ColumnTopY = 5.75f;
        public const float ColumnWidth = 0.62f;

        private const string TexDir = "Assets/Textures/Environment";
        private const string MatDir = "Assets/Settings";

        [MenuItem("MyVirtualCommunity/Build Av. Juárez Digital Arch")]
        public static void BuildArchInScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[AvJuarezArchBuilder] Cannot open scene: {ScenePath}");
                return;
            }

            // 1. Ensure Textures & Materials
            EnsureFolderHierarchy();
            GenerateSidePanelTexture($"{TexDir}/T_JuarezArch_SidePanel.png");
            GeneratePlaqueTexture($"{TexDir}/T_JuarezArch_CiudadJuarez.png", "CIUDAD JUÁREZ", 1024, 192, isTrapezoid: false);
            GeneratePlaqueTexture($"{TexDir}/T_JuarezArch_Bienvenidos.png", "BIENVENIDOS", 1024, 224, isTrapezoid: true);
            GenerateSpeedLimitTexture($"{TexDir}/T_JuarezArch_Speed15.png");
            GeneratePromoChihuahuaTexture($"{TexDir}/T_JuarezArch_Promo_Chihuahua.png");
            GeneratePromoCentroHistoricoTexture($"{TexDir}/T_JuarezArch_Promo_CentroHistorico.png");
            AssetDatabase.Refresh();

            Material steelBlackMat = CreateOrGetColorMat($"{MatDir}/JuarezArch_SteelBlack.mat", new UColor(0.11f, 0.12f, 0.135f), 0.35f, 0.55f);
            Material cabinetMat = CreateOrGetColorMat($"{MatDir}/JuarezArch_CabinetDark.mat", new UColor(0.06f, 0.065f, 0.075f), 0.20f, 0.35f);
            Material bulbMat = CreateOrGetColorMat($"{MatDir}/JuarezArch_BulbWarm.mat", new UColor(0.95f, 0.88f, 0.68f), 0.6f, 0.05f);

            Material sidePanelMat = CreateOrGetTextureMat($"{MatDir}/JuarezArch_SidePanel.mat", $"{TexDir}/T_JuarezArch_SidePanel.png", emissiveStrength: 0.28f);
            Material juarezSignMat = CreateOrGetTextureMat($"{MatDir}/JuarezArch_CiudadJuarez.mat", $"{TexDir}/T_JuarezArch_CiudadJuarez.png", emissiveStrength: 0.12f);
            Material bienvenidosMat = CreateOrGetTextureMat($"{MatDir}/JuarezArch_Bienvenidos.mat", $"{TexDir}/T_JuarezArch_Bienvenidos.png", emissiveStrength: 0.12f);
            Material speedSignMat = CreateOrGetTextureMat($"{MatDir}/JuarezArch_Speed15.mat", $"{TexDir}/T_JuarezArch_Speed15.png", emissiveStrength: 0.0f);

            // 2. Remove existing instance cleanly (idempotent)
            GameObject existing = GameObject.Find(ArchRootName);
            while (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                existing = GameObject.Find(ArchRootName);
            }

            GameObject root = new GameObject(ArchRootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // 3. Build West & East Lattice Tower Columns
            BuildLatticeColumn("Column_West_Lattice", root.transform, WestColumnX, steelBlackMat);
            BuildLatticeColumn("Column_East_Lattice", root.transform, EastColumnX, steelBlackMat);

            // 4. Build Lower Arched Bridge Truss & LED Garland Bulbs
            BuildArchedBridgeTruss(root.transform, steelBlackMat, bulbMat);

            // 5. Build Upper Curved Crown Truss & Tripartite Billboard Backing
            BuildUpperBillboardCrown(root.transform, steelBlackMat, cabinetMat);

            // 6. Build Curved Turquoise Side Panels (Left West & Right East)
            BuildCurvedSidePanel("SidePanel_West_Announce", root.transform, xLeft: -3.10f, xRight: -0.56f, isLeftWing: true, cabinetMat, sidePanelMat);
            BuildCurvedSidePanel("SidePanel_East_Announce", root.transform, xLeft: 4.76f, xRight: 7.30f, isLeftWing: false, cabinetMat, sidePanelMat);

            // 7. Build Mounted Plaques ("CIUDAD JUÁREZ", "BIENVENIDOS") & "15 km/h MÁXIMO" Traffic Sign
            BuildMountedSignQuads(root.transform, steelBlackMat, juarezSignMat, bienvenidosMat, speedSignMat);

            // 8. Build Central Dynamic 5.00m x 3.20m LED Display & Wire JuarezArchDisplayController
            BuildCentralDynamicDisplay(root.transform, cabinetMat);

            // 9. Re-index TimeOfDayEnvironmentController so the Arch LED garland bulbs glow at night automatically
            TimeOfDayEnvironmentController tod = UnityEngine.Object.FindAnyObjectByType<TimeOfDayEnvironmentController>();
            if (tod != null)
            {
                tod.DiscoverSceneReferences();
                EditorUtility.SetDirty(tod);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AvJuarezArchBuilder] Saved '{ArchRootName}' cleanly at X={ArchCenterX:F2}, Z={ArchZ:F2} in '{ScenePath}'.");
        }

        private static void EnsureFolderHierarchy()
        {
            EnsureUnityFolder("Assets/Textures");
            EnsureUnityFolder("Assets/Textures/Environment");
            EnsureUnityFolder("Assets/Settings");
            EnsureUnityFolder("Assets/StreamingAssets");
            EnsureUnityFolder("Assets/StreamingAssets/ArchMedia");
        }

        private static void EnsureUnityFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void BuildLatticeColumn(string name, Transform parent, float centerX, Material steelMat)
        {
            GameObject colRoot = new GameObject(name);
            colRoot.transform.SetParent(parent);
            colRoot.transform.position = new Vector3(centerX, SidewalkBaseY, ArchZ);

            float half = ColumnWidth * 0.5f - 0.035f; // 0.275m chord center offset
            float height = ColumnTopY - SidewalkBaseY; // 5.55m

            // Steel base pedestal + solid pedestrian collider
            GameObject basePed = CreateBox("Base_Pedestal", colRoot.transform,
                new Vector3(0f, 0.07f, 0f), new Vector3(ColumnWidth + 0.16f, 0.14f, ColumnWidth + 0.16f), steelMat, keepCollider: true);

            GameObject topCap = CreateBox("Column_Capital", colRoot.transform,
                new Vector3(0f, height - 0.06f, 0f), new Vector3(ColumnWidth + 0.12f, 0.12f, ColumnWidth + 0.12f), steelMat, keepCollider: false);

            // Physics blocker trigger-free invisible column core for clean capsule collision
            GameObject colCollider = new GameObject("Column_ColliderCore");
            colCollider.transform.SetParent(colRoot.transform);
            colCollider.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            BoxCollider bc = colCollider.AddComponent<BoxCollider>();
            bc.size = new Vector3(ColumnWidth, height, ColumnWidth);

            // 4 vertical corner chords
            Vector2[] corners = new Vector2[]
            {
                new Vector2(-half, -half),
                new Vector2( half, -half),
                new Vector2(-half,  half),
                new Vector2( half,  half),
            };
            for (int i = 0; i < corners.Length; i++)
            {
                CreateBox($"Chord_{i}", colRoot.transform,
                    new Vector3(corners[i].x, height * 0.5f, corners[i].y),
                    new Vector3(0.068f, height, 0.068f), steelMat, keepCollider: false);
            }

            // Horizontal rings & zig-zag lattice diagonals along 8 bays
            int bays = 8;
            float usableH = height - 0.24f;
            float bayH = usableH / bays;
            for (int b = 0; b <= bays; b++)
            {
                float y = 0.14f + b * bayH;
                CreateBox($"Ring_S_{b}", colRoot.transform, new Vector3(0f, y, -half), new Vector3(ColumnWidth - 0.06f, 0.048f, 0.048f), steelMat);
                CreateBox($"Ring_N_{b}", colRoot.transform, new Vector3(0f, y,  half), new Vector3(ColumnWidth - 0.06f, 0.048f, 0.048f), steelMat);
                CreateBox($"Ring_W_{b}", colRoot.transform, new Vector3(-half, y, 0f), new Vector3(0.048f, 0.048f, ColumnWidth - 0.06f), steelMat);
                CreateBox($"Ring_E_{b}", colRoot.transform, new Vector3( half, y, 0f), new Vector3(0.048f, 0.048f, ColumnWidth - 0.06f), steelMat);

                if (b < bays)
                {
                    float y0 = y;
                    float y1 = y + bayH;
                    bool flip = (b % 2 == 1);
                    float xA = flip ? half : -half;
                    float xB = flip ? -half : half;
                    CreateStrutBetween($"Diag_S_{b}", colRoot.transform, new Vector3(xA, y0, -half), new Vector3(xB, y1, -half), 0.036f, steelMat);
                    CreateStrutBetween($"Diag_N_{b}", colRoot.transform, new Vector3(xB, y0,  half), new Vector3(xA, y1,  half), 0.036f, steelMat);
                    CreateStrutBetween($"Diag_W_{b}", colRoot.transform, new Vector3(-half, y0, xA), new Vector3(-half, y1, xB), 0.036f, steelMat);
                    CreateStrutBetween($"Diag_E_{b}", colRoot.transform, new Vector3( half, y0, xB), new Vector3( half, y1, xA), 0.036f, steelMat);
                }
            }
        }

        private static float EvaluateLowerArchY(float worldX)
        {
            float halfSpan = (EastColumnX - WestColumnX) * 0.5f; // 5.25m
            float u = Mathf.Clamp((worldX - ArchCenterX) / halfSpan, -1f, 1f);
            // Smooth segmental arch rising from Y=4.35m at inner column faces to Y=5.15m at center crest
            return 4.35f + 0.80f * (1f - u * u);
        }

        private static float EvaluateCrownArchY(float worldX)
        {
            float halfTotal = 5.45f;
            float u = Mathf.Clamp((worldX - ArchCenterX) / halfTotal, -1f, 1f);
            // Graceful convex top crown curve from Y=8.22m over columns to Y=9.48m at apex
            return 8.22f + 1.26f * (1f - u * u);
        }

        private static void BuildArchedBridgeTruss(Transform parent, Material steelMat, Material bulbMat)
        {
            GameObject trussRoot = new GameObject("Bridge_LowerArch_Truss");
            trussRoot.transform.SetParent(parent);

            float spanStart = WestColumnX + ColumnWidth * 0.40f; // -2.90f
            float spanEnd = EastColumnX - ColumnWidth * 0.40f;   // +7.10f
            float spanLen = EastColumnX - WestColumnX + ColumnWidth * 0.90f; // 11.06m
            float zFront = ArchZ - 0.24f;
            float zBack = ArchZ + 0.24f;

            // Upper double horizontal chord supporting billboard assembly (Y = 5.46m .. 5.84m)
            CreateBox("TopChord_Front_Upper", trussRoot.transform, new Vector3(ArchCenterX, 5.76f, zFront), new Vector3(spanLen, 0.12f, 0.10f), steelMat);
            CreateBox("TopChord_Back_Upper", trussRoot.transform, new Vector3(ArchCenterX, 5.76f, zBack), new Vector3(spanLen, 0.12f, 0.10f), steelMat);
            CreateBox("TopChord_Front_Lower", trussRoot.transform, new Vector3(ArchCenterX, 5.48f, zFront), new Vector3(spanLen, 0.09f, 0.09f), steelMat);
            CreateBox("TopChord_Back_Lower", trussRoot.transform, new Vector3(ArchCenterX, 5.48f, zBack), new Vector3(spanLen, 0.09f, 0.09f), steelMat);

            // Continuous arched lower chord & lattice web (20 bays)
            int segments = 20;
            float prevX = spanStart;
            float prevY = EvaluateLowerArchY(prevX);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float curX = Mathf.Lerp(spanStart, spanEnd, t);
                float curY = EvaluateLowerArchY(curX);

                // Vertical post & front-to-back tie bar
                CreateStrutBetween($"WebVert_F_{i}", trussRoot.transform, new Vector3(curX, curY, zFront), new Vector3(curX, 5.48f, zFront), 0.042f, steelMat);
                CreateStrutBetween($"WebVert_B_{i}", trussRoot.transform, new Vector3(curX, curY, zBack), new Vector3(curX, 5.48f, zBack), 0.042f, steelMat);
                CreateBox($"CrossTie_Bot_{i}", trussRoot.transform, new Vector3(curX, curY, ArchZ), new Vector3(0.05f, 0.05f, 0.50f), steelMat);
                CreateBox($"CrossTie_Top_{i}", trussRoot.transform, new Vector3(curX, 5.64f, ArchZ), new Vector3(0.05f, 0.06f, 0.50f), steelMat);

                if (i > 0)
                {
                    // Lower curved arch segments (front & back)
                    CreateStrutBetween($"ArchChord_F_{i}", trussRoot.transform, new Vector3(prevX, prevY, zFront), new Vector3(curX, curY, zFront), 0.085f, steelMat);
                    CreateStrutBetween($"ArchChord_B_{i}", trussRoot.transform, new Vector3(prevX, prevY, zBack), new Vector3(curX, curY, zBack), 0.085f, steelMat);

                    // X-bracing diagonals in each arched bay
                    CreateStrutBetween($"WebDiagA_F_{i}", trussRoot.transform, new Vector3(prevX, prevY, zFront), new Vector3(curX, 5.48f, zFront), 0.036f, steelMat);
                    CreateStrutBetween($"WebDiagB_F_{i}", trussRoot.transform, new Vector3(prevX, 5.48f, zFront), new Vector3(curX, curY, zFront), 0.036f, steelMat);
                    CreateStrutBetween($"WebDiagA_B_{i}", trussRoot.transform, new Vector3(prevX, prevY, zBack), new Vector3(curX, 5.48f, zBack), 0.036f, steelMat);
                    CreateStrutBetween($"WebDiagB_B_{i}", trussRoot.transform, new Vector3(prevX, 5.48f, zBack), new Vector3(curX, curY, zBack), 0.036f, steelMat);
                }

                prevX = curX;
                prevY = curY;
            }

            // Column knee braces for visual strength
            CreateStrutBetween("KneeBrace_West_F", trussRoot.transform, new Vector3(WestColumnX + 0.25f, 3.65f, zFront), new Vector3(WestColumnX + 1.15f, EvaluateLowerArchY(WestColumnX + 1.15f), zFront), 0.065f, steelMat);
            CreateStrutBetween("KneeBrace_East_F", trussRoot.transform, new Vector3(EastColumnX - 0.25f, 3.65f, zFront), new Vector3(EastColumnX - 1.15f, EvaluateLowerArchY(EastColumnX - 1.15f), zFront), 0.065f, steelMat);

            // Garland of warm LED bulbs along South (-Z) upper horizontal truss bar (like photo 2)
            GameObject bulbsRoot = new GameObject("Arch_Garland_LED_Bulbs");
            bulbsRoot.transform.SetParent(trussRoot.transform);
            int bulbCount = 36;
            for (int i = 0; i < bulbCount; i++)
            {
                float bx = Mathf.Lerp(spanStart + 0.15f, spanEnd - 0.15f, (float)i / (bulbCount - 1));
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = $"Luminaire_Bulb_Arch_{i:D2}";
                bulb.transform.SetParent(bulbsRoot.transform);
                bulb.transform.position = new Vector3(bx, 5.39f, zFront - 0.06f);
                bulb.transform.localScale = new Vector3(0.068f, 0.068f, 0.068f);
                SetMat(bulb, bulbMat);
                UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
            }
        }

        private static void BuildUpperBillboardCrown(Transform parent, Material steelMat, Material cabinetMat)
        {
            GameObject crownRoot = new GameObject("Billboard_Crown_Structure");
            crownRoot.transform.SetParent(parent);

            float crownStartX = -3.32f;
            float crownEndX = 7.52f;
            float zFront = ArchZ - 0.24f;
            float zBack = ArchZ + 0.24f;

            // Curved outer top arch chord & secondary lower top chord over panels
            int segs = 24;
            float prevX = crownStartX;
            float prevY = EvaluateCrownArchY(prevX);

            for (int i = 0; i <= segs; i++)
            {
                float curX = Mathf.Lerp(crownStartX, crownEndX, (float)i / segs);
                float curY = EvaluateCrownArchY(curX);
                float innerY = curY - 0.22f;

                if (i > 0)
                {
                    CreateStrutBetween($"CrownTop_F_{i}", crownRoot.transform, new Vector3(prevX, prevY, zFront), new Vector3(curX, curY, zFront), 0.085f, steelMat);
                    CreateStrutBetween($"CrownTop_B_{i}", crownRoot.transform, new Vector3(prevX, prevY, zBack), new Vector3(curX, curY, zBack), 0.085f, steelMat);
                    CreateStrutBetween($"CrownBot_F_{i}", crownRoot.transform, new Vector3(prevX, prevY - 0.22f, zFront), new Vector3(curX, innerY, zFront), 0.055f, steelMat);
                    CreateStrutBetween($"CrownBot_B_{i}", crownRoot.transform, new Vector3(prevX, prevY - 0.22f, zBack), new Vector3(curX, innerY, zBack), 0.055f, steelMat);
                    CreateStrutBetween($"CrownDiag_F_{i}", crownRoot.transform, new Vector3(prevX, prevY - 0.22f, zFront), new Vector3(curX, curY, zFront), 0.035f, steelMat);
                }

                CreateStrutBetween($"CrownRib_F_{i}", crownRoot.transform, new Vector3(curX, innerY, zFront), new Vector3(curX, curY, zFront), 0.038f, steelMat);
                CreateBox($"CrownTie_{i}", crownRoot.transform, new Vector3(curX, curY - 0.10f, ArchZ), new Vector3(0.045f, 0.045f, 0.50f), steelMat);

                prevX = curX;
                prevY = curY;
            }

            // Outer vertical end-posts & center divider mullions flanking the 5.00m central LED screen
            float[] mullionsX = new float[] { -3.22f, -0.49f, 4.69f, 7.42f };
            foreach (float mx in mullionsX)
            {
                float topY = EvaluateCrownArchY(mx) - 0.12f;
                float botY = 5.80f;
                float midY = 0.5f * (topY + botY);
                CreateBox($"Crown_Mullion_{mx:F1}", crownRoot.transform,
                    new Vector3(mx, midY, ArchZ), new Vector3(0.14f, topY - botY, 0.48f), steelMat);
            }

            // Solid dark steel housing behind the central 5.00m x 3.20m LED screen
            CreateBox("CentralLED_RearCabinet", crownRoot.transform,
                new Vector3(ArchCenterX, 7.55f, ArchZ + 0.02f), new Vector3(5.12f, 3.30f, 0.42f), cabinetMat, keepCollider: true);
        }

        private static void BuildCurvedSidePanel(
            string name,
            Transform parent,
            float xLeft,
            float xRight,
            bool isLeftWing,
            Material cabinetMat,
            Material faceMat)
        {
            GameObject wingRoot = new GameObject(name);
            wingRoot.transform.SetParent(parent);

            const float yBottom = 5.92f;
            const int slices = 20;
            float zFace = ArchZ - 0.24f;
            float zBack = ArchZ + 0.12f;

            // Front curved textured quad mesh facing South (-Z)
            Mesh faceMesh = new Mesh { name = name + "_FaceMesh" };
            Mesh backMesh = new Mesh { name = name + "_BackMesh" };
            var fVerts = new List<Vector3>();
            var fUvs = new List<Vector2>();
            var fTris = new List<int>();
            var bVerts = new List<Vector3>();
            var bUvs = new List<Vector2>();
            var bTris = new List<int>();

            float heightOffsetBelowCrown = 0.16f;
            for (int i = 0; i <= slices; i++)
            {
                float u = (float)i / slices;
                float x = Mathf.Lerp(xLeft, xRight, u);
                float yTop = Mathf.Min(9.05f, EvaluateCrownArchY(x) - heightOffsetBelowCrown);

                fVerts.Add(new Vector3(x, yBottom, zFace));
                fVerts.Add(new Vector3(x, yTop, zFace));
                fUvs.Add(new Vector2(u, 0f));
                fUvs.Add(new Vector2(u, 1f));

                bVerts.Add(new Vector3(x, yBottom, zBack));
                bVerts.Add(new Vector3(x, yTop, zBack));
                bUvs.Add(new Vector2(1f - u, 0f));
                bUvs.Add(new Vector2(1f - u, 1f));

                if (i > 0)
                {
                    int b0 = (i - 1) * 2;
                    // Front faces -Z (South): clockwise when viewed from -Z in Unity left-handed space
                    fTris.Add(b0 + 0); fTris.Add(b0 + 1); fTris.Add(b0 + 3);
                    fTris.Add(b0 + 0); fTris.Add(b0 + 3); fTris.Add(b0 + 2);

                    // Back faces +Z (North)
                    bTris.Add(b0 + 0); bTris.Add(b0 + 3); bTris.Add(b0 + 1);
                    bTris.Add(b0 + 0); bTris.Add(b0 + 2); bTris.Add(b0 + 3);
                }
            }

            faceMesh.SetVertices(fVerts);
            faceMesh.SetUVs(0, fUvs);
            faceMesh.SetTriangles(fTris, 0);
            faceMesh.RecalculateNormals();
            faceMesh.RecalculateBounds();

            backMesh.SetVertices(bVerts);
            backMesh.SetUVs(0, bUvs);
            backMesh.SetTriangles(bTris, 0);
            backMesh.RecalculateNormals();
            backMesh.RecalculateBounds();

            string faceMeshPath = $"{MatDir}/{name}_FaceMesh.asset";
            string backMeshPath = $"{MatDir}/{name}_BackMesh.asset";
            AssetDatabase.DeleteAsset(faceMeshPath);
            AssetDatabase.DeleteAsset(backMeshPath);
            AssetDatabase.CreateAsset(faceMesh, faceMeshPath);
            AssetDatabase.CreateAsset(backMesh, backMeshPath);
            AssetDatabase.SaveAssets();
            faceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(faceMeshPath);
            backMesh = AssetDatabase.LoadAssetAtPath<Mesh>(backMeshPath);

            GameObject frontGo = new GameObject("Panel_FrontFace");
            frontGo.transform.SetParent(wingRoot.transform);
            frontGo.AddComponent<MeshFilter>().sharedMesh = faceMesh;
            var fMr = frontGo.AddComponent<MeshRenderer>();
            fMr.sharedMaterial = faceMat;
            fMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject backGo = new GameObject("Panel_RearCover");
            backGo.transform.SetParent(wingRoot.transform);
            backGo.AddComponent<MeshFilter>().sharedMesh = backMesh;
            backGo.AddComponent<MeshRenderer>().sharedMaterial = cabinetMat;

            // Dark backing frame behind the side billboard
            float midX = 0.5f * (xLeft + xRight);
            float avgHeight = isLeftWing
                ? ((EvaluateCrownArchY(xRight) - yBottom) + (EvaluateCrownArchY(xLeft) - yBottom)) * 0.45f
                : ((EvaluateCrownArchY(xLeft) - yBottom) + (EvaluateCrownArchY(xRight) - yBottom)) * 0.45f;
            CreateBox("Panel_BackingCore", wingRoot.transform,
                new Vector3(midX, yBottom + avgHeight * 0.5f, zFace + 0.16f),
                new Vector3(Mathf.Abs(xRight - xLeft) - 0.04f, avgHeight, 0.28f), cabinetMat);
        }

        private static void BuildMountedSignQuads(
            Transform parent,
            Material steelMat,
            Material juarezSignMat,
            Material bienvenidosMat,
            Material speedSignMat)
        {
            GameObject signsRoot = new GameObject("Arch_Plaques_And_Signs");
            signsRoot.transform.SetParent(parent);

            // 1. Top center plaque: "CIUDAD JUÁREZ" (sitting over the horizontal truss bar, Z = -23.81)
            CreateSignQuadWithBacking("Plaque_CiudadJuarez", signsRoot.transform,
                new Vector3(ArchCenterX, 5.66f, ArchZ - 0.32f),
                width: 1.92f, height: 0.34f, steelMat, juarezSignMat);

            // 2. Hanging center lower plaque: "BIENVENIDOS" (just under lower arch crest)
            CreateSignQuadWithBacking("Plaque_Bienvenidos", signsRoot.transform,
                new Vector3(ArchCenterX, 4.94f, ArchZ - 0.30f),
                width: 1.38f, height: 0.28f, steelMat, bienvenidosMat);

            // 3. West Column "15 km/h MÁXIMO" traffic sign facing approaching South traffic
            CreateSignQuadWithBacking("Sign_SpeedLimit_15Kmh", signsRoot.transform,
                new Vector3(WestColumnX + 0.58f, 3.18f, ArchZ - 0.34f),
                width: 0.62f, height: 0.78f, steelMat, speedSignMat);

            // Bracket connecting speed sign cleanly to West lattice column
            CreateBox("Sign_SpeedLimit_BracketTop", signsRoot.transform,
                new Vector3(WestColumnX + 0.35f, 3.40f, ArchZ - 0.28f), new Vector3(0.42f, 0.045f, 0.05f), steelMat);
            CreateBox("Sign_SpeedLimit_BracketBot", signsRoot.transform,
                new Vector3(WestColumnX + 0.35f, 2.95f, ArchZ - 0.28f), new Vector3(0.42f, 0.045f, 0.05f), steelMat);
        }

        private static void CreateSignQuadWithBacking(
            string name,
            Transform parent,
            Vector3 frontPos,
            float width,
            float height,
            Material backMat,
            Material faceMat)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent);
            holder.transform.position = frontPos;

            CreateBox("Backing", holder.transform, new Vector3(0f, 0f, 0.035f), new Vector3(width + 0.04f, height + 0.04f, 0.05f), backMat);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FaceQuad";
            quad.transform.SetParent(holder.transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity; // Quad faces -Z (South)
            quad.transform.localScale = new Vector3(width, height, 1f);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

            var mr = quad.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = faceMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void BuildCentralDynamicDisplay(Transform parent, Material cabinetMat)
        {
            // World-Space Canvas centered cleanly on South (-Z) face of the 5.00m x 3.20m LED screen
            GameObject canvasGo = new GameObject("JuarezArch_CentralDisplay");
            canvasGo.transform.SetParent(parent);
            canvasGo.transform.position = new Vector3(ArchCenterX, 7.55f, ArchZ - 0.22f);
            canvasGo.transform.rotation = Quaternion.identity; // Faces -Z (South toward arriving traffic)

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            const float logicalW = 640f;
            const float logicalH = 410f;
            canvasRect.sizeDelta = new Vector2(logicalW, logicalH);
            // 640 * 0.0078125 = 5.000m wide; 410 * 0.0078125 = 3.203m tall
            canvasGo.transform.localScale = new Vector3(0.0078125f, 0.0078125f, 1f);

            canvasGo.AddComponent<CanvasScaler>();

            UFont uiFont = Resources.GetBuiltinResource<UFont>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = UFont.CreateDynamicFontFromOSFont("Arial", 24);

            // Solid near-black LED backboard
            UImage bgImg = canvasGo.AddComponent<UImage>();
            bgImg.color = new UColor(0.03f, 0.05f, 0.11f, 1.0f);

            // 1. Top Live Telemetry Ribbon (Height 48px -> 0.375m; leaves 640x362 = ~16:9 widescreen viewport below!)
            GameObject topRibbon = CreateUiRect("TopTelemetryRibbon", canvasRect,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f), anchoredPos: Vector2.zero, sizeDelta: new Vector2(0f, 48f));
            topRibbon.AddComponent<UImage>().color = new UColor(0.04f, 0.10f, 0.22f, 0.96f);

            Text tickerTime = CreateUiText("Ticker_Time", topRibbon.transform, uiFont, 21, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleLeft, new UColor(0.38f, 0.92f, 1.0f),
                new Vector2(0.02f, 0f), new Vector2(0.30f, 1f), "HORA --:--");

            Text tickerWeather = CreateUiText("Ticker_Weather", topRibbon.transform, uiFont, 21, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(1.0f, 0.96f, 0.80f),
                new Vector2(0.29f, 0f), new Vector2(0.73f, 1f), "--°C • --");

            Text tickerBorder = CreateUiText("Ticker_Border", topRibbon.transform, uiFont, 20, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleRight, new UColor(0.52f, 1.0f, 0.62f),
                new Vector2(0.72f, 0f), new Vector2(0.98f, 1f), "PDN: -- min");

            // Thin cyan LED divider line
            GameObject divLine = CreateUiRect("TopDividerLine", canvasRect,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(0f, 3f));
            divLine.AddComponent<UImage>().color = new UColor(0.0f, 0.78f, 0.88f, 1.0f);

            // 2. Main Media Viewport (bottom 359px -> 640 x 359 ≈ exact 16:9!)
            GameObject mediaGo = CreateUiRect("MainMediaViewport", canvasRect,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(0f, -25.5f), sizeDelta: new Vector2(0f, -51f));
            RawImage mediaRawImage = mediaGo.AddComponent<RawImage>();
            mediaRawImage.color = UColor.white;

            // 3. Dedicated Full-Frame Live Clock + Weather + Paso del Norte Board
            GameObject infoPanel = CreateUiRect("LiveInfoDashboard", canvasRect,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(0f, -25.5f), sizeDelta: new Vector2(0f, -51f));
            infoPanel.AddComponent<UImage>().color = new UColor(0.04f, 0.14f, 0.42f, 1.0f);

            Text boardPhaseDate = CreateUiText("Board_PhaseDate", infoPanel.transform, uiFont, 22, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(0.58f, 0.94f, 1.0f),
                new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.96f), "CD. JUÁREZ  •  CENTRO HISTÓRICO");

            // Left Card: Official Clock
            GameObject clockCard = CreateUiPanel("ClockCard", infoPanel.transform,
                new Vector2(0.04f, 0.34f), new Vector2(0.48f, 0.80f), new UColor(0.06f, 0.22f, 0.56f, 0.95f));
            CreateUiText("ClockCard_Header", clockCard.transform, uiFont, 18, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(0.75f, 0.90f, 1.0f),
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f), "HORA OFICIAL");
            Text boardClock = CreateUiText("Board_ClockValue", clockCard.transform, uiFont, 62, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, UColor.white,
                new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.74f), "--:--");

            // Right Card: Live Backend Weather
            GameObject weatherCard = CreateUiPanel("WeatherCard", infoPanel.transform,
                new Vector2(0.52f, 0.34f), new Vector2(0.96f, 0.80f), new UColor(0.05f, 0.36f, 0.54f, 0.95f));
            CreateUiText("WeatherCard_Header", weatherCard.transform, uiFont, 18, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(0.78f, 0.98f, 1.0f),
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.95f), "CLIMA ACTUAL");
            Text boardTemp = CreateUiText("Board_TempValue", weatherCard.transform, uiFont, 40, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(1.0f, 0.94f, 0.60f),
                new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.75f), "--°C");
            Text boardWeatherDetail = CreateUiText("Board_WeatherDetail", weatherCard.transform, uiFont, 16, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, UColor.white,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.34f), "Consultando clima...");

            // Bottom Strip: Paso del Norte CBP wait status + Traffic Notice
            GameObject borderBanner = CreateUiPanel("BorderStatusCard", infoPanel.transform,
                new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.30f), new UColor(0.03f, 0.11f, 0.28f, 0.95f));
            Text boardBorderStatus = CreateUiText("Board_BorderStatus", borderBanner.transform, uiFont, 18, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(0.52f, 1.0f, 0.65f),
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), "PUENTE PASO DEL NORTE: Consultando CBP...");

            Text boardFooter = CreateUiText("Board_Footer", infoPanel.transform, uiFont, 16, UnityEngine.FontStyle.Bold,
                TextAnchor.MiddleCenter, new UColor(0.85f, 0.92f, 1.0f),
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f), "BIENVENIDOS AL CENTRO HISTÓRICO  •  15 km/h MÁX.");

            infoPanel.SetActive(false);

            // Attach & wire JuarezArchDisplayController
            Texture2D promo1 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_JuarezArch_Promo_Chihuahua.png");
            Texture2D promo2 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_JuarezArch_Promo_CentroHistorico.png");
            var promoList = new List<Texture2D>();
            if (promo1 != null) promoList.Add(promo1);
            if (promo2 != null) promoList.Add(promo2);

            JuarezArchDisplayController controller = canvasGo.AddComponent<JuarezArchDisplayController>();
            controller.AssignUiReferences(
                mediaRawImage,
                infoPanel,
                tickerTime,
                tickerWeather,
                tickerBorder,
                boardClock,
                boardPhaseDate,
                boardTemp,
                boardWeatherDetail,
                boardBorderStatus,
                boardFooter,
                promoList);
            EditorUtility.SetDirty(controller);
        }

        private static GameObject CreateUiRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static GameObject CreateUiPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, UColor color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<UImage>().color = color;
            return go;
        }

        private static Text CreateUiText(
            string name,
            Transform parent,
            UFont font,
            int fontSize,
            UnityEngine.FontStyle style,
            TextAnchor alignment,
            UColor color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string initialText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = alignment;
            t.color = color;
            t.text = initialText;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        #region Geometric Primitives & Material Helpers

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            SetMat(go, mat);
            if (!keepCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            return go;
        }

        private static GameObject CreateStrutBetween(string name, Transform parent, Vector3 pA, Vector3 pB, float thickness, Material mat)
        {
            Vector3 diff = pB - pA;
            float len = diff.magnitude;
            if (len < 0.001f) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = 0.5f * (pA + pB);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, diff.normalized);
            go.transform.localScale = new Vector3(thickness, len, thickness);
            SetMat(go, mat);
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        private static Material CreateOrGetColorMat(string path, UColor color, float smoothness, float metallic)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = s;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material CreateOrGetTextureMat(string matPath, string texPath, float emissiveStrength)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, matPath);
            }
            m.shader = s;
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", UColor.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", UColor.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.22f);
            if (emissiveStrength > 0.001f && tex != null)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new UColor(emissiveStrength, emissiveStrength, emissiveStrength, 1f));
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        #endregion

        #region Procedural High-Resolution Artwork Generation

        private static void GenerateSidePanelTexture(string assetPath)
        {
            const int w = 1024;
            const int h = 1024;
            using (var bmp = new Bitmap(w, h))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (var bgBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, w, h),
                    GColor.FromArgb(24, 198, 190),
                    GColor.FromArgb(8, 134, 162),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bgBrush, 0, 0, w, h);
                }

                // Soft decorative subtle circle glow
                using (var glowBrush = new SolidBrush(GColor.FromArgb(26, 255, 255, 255)))
                {
                    g.FillEllipse(glowBrush, 140, 320, 744, 520);
                }

                // ROGER top badge pill
                using (var badgeBrush = new SolidBrush(GColor.FromArgb(14, 165, 112)))
                using (var badgePen = new Pen(GColor.White, 8f))
                using (var pillPath = CreateRoundedRectPath(240, 118, 544, 96, 48))
                {
                    g.FillPath(badgeBrush, pillPath);
                    g.DrawPath(badgePen, pillPath);
                }

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var rogerFont = new GFont("Segoe UI", 50, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var rogerBrush = new SolidBrush(GColor.White))
                {
                    g.DrawString("ROGER", rogerFont, rogerBrush, new RectangleF(240, 120, 544, 96), sfCenter);
                }

                // "ANÚNCIATE AQUÍ"
                using (var titleFont = new GFont("Segoe UI", 92, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var whiteBrush = new SolidBrush(GColor.White))
                {
                    g.DrawString("ANÚNCIATE", titleFont, whiteBrush, new RectangleF(40, 238, w - 80, 108), sfCenter);
                    g.DrawString("AQUÍ", titleFont, whiteBrush, new RectangleF(40, 336, w - 80, 108), sfCenter);
                }

                // Cute friendly smiling billboard mascot icon in center
                using (var bodyPath = CreateRoundedRectPath(342, 466, 340, 224, 54))
                using (var whiteBrush = new SolidBrush(GColor.White))
                using (var outlinePen = new Pen(GColor.FromArgb(16, 70, 130), 10f))
                {
                    g.FillPath(whiteBrush, bodyPath);
                    g.DrawPath(outlinePen, bodyPath);
                }
                using (var visorPath = CreateRoundedRectPath(378, 500, 268, 152, 34))
                using (var visorBrush = new SolidBrush(GColor.FromArgb(18, 42, 82)))
                {
                    g.FillPath(visorBrush, visorPath);
                }
                using (var eyeBrush = new SolidBrush(GColor.FromArgb(40, 232, 240)))
                {
                    g.FillEllipse(eyeBrush, 428, 540, 44, 44);
                    g.FillEllipse(eyeBrush, 552, 540, 44, 44);
                }
                using (var smilePen = new Pen(GColor.FromArgb(40, 232, 240), 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(smilePen, 462, 566, 100, 54, 15, 150);
                }

                // Phone number white capsule
                using (var phonePill = CreateRoundedRectPath(128, 728, 768, 84, 42))
                using (var pillFill = new SolidBrush(GColor.FromArgb(235, 255, 255, 255)))
                {
                    g.FillPath(pillFill, phonePill);
                }
                using (var phoneFont = new GFont("Segoe UI", 54, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var darkTealBrush = new SolidBrush(GColor.FromArgb(8, 86, 114)))
                {
                    g.DrawString("656 335 1141", phoneFont, darkTealBrush, new RectangleF(128, 728, 768, 84), sfCenter);
                }

                // Footer: "ESPACIO DISPONIBLE"
                using (var footFont = new GFont("Segoe UI", 80, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var whiteBrush = new SolidBrush(GColor.White))
                {
                    g.DrawString("ESPACIO", footFont, whiteBrush, new RectangleF(40, 824, w - 80, 92), sfCenter);
                    g.DrawString("DISPONIBLE", footFont, whiteBrush, new RectangleF(40, 906, w - 80, 92), sfCenter);
                }

                string absPath = Path.GetFullPath(assetPath);
                bmp.Save(absPath, ImageFormat.Png);
            }
        }

        private static void GeneratePlaqueTexture(string assetPath, string text, int w, int h, bool isTrapezoid)
        {
            using (var bmp = new Bitmap(w, h))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(GColor.FromArgb(24, 26, 30));

                if (isTrapezoid)
                {
                    PointF[] poly = new PointF[]
                    {
                        new PointF(14f, 12f),
                        new PointF(w - 14f, 12f),
                        new PointF(w - 92f, h - 14f),
                        new PointF(92f, h - 14f),
                    };
                    using (var fill = new SolidBrush(GColor.FromArgb(30, 33, 38)))
                    using (var pen = new Pen(GColor.FromArgb(210, 214, 220), 10f))
                    {
                        g.FillPolygon(fill, poly);
                        g.DrawPolygon(pen, poly);
                    }
                }
                else
                {
                    using (var pen = new Pen(GColor.FromArgb(200, 204, 210), 10f))
                    {
                        g.DrawRectangle(pen, 10, 10, w - 20, h - 20);
                    }
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                float fontSize = isTrapezoid ? 92f : 96f;
                using (var font = new GFont("Segoe UI", fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(GColor.White))
                {
                    g.DrawString(text, font, brush, new RectangleF(24f, 8f, w - 48f, h - 16f), sf);
                }

                bmp.Save(Path.GetFullPath(assetPath), ImageFormat.Png);
            }
        }

        private static void GenerateSpeedLimitTexture(string assetPath)
        {
            const int w = 512;
            const int h = 640;
            using (var bmp = new Bitmap(w, h))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(GColor.FromArgb(245, 246, 248));

                using (var blackPen = new Pen(GColor.FromArgb(25, 27, 30), 10f))
                {
                    g.DrawRectangle(blackPen, 10, 10, w - 20, 440);
                    g.DrawRectangle(blackPen, 10, 464, w - 20, 166);
                }

                using (var redPen = new Pen(GColor.FromArgb(208, 28, 34), 38f))
                {
                    g.DrawEllipse(redPen, 76, 48, 360, 360);
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var numFont = new GFont("Segoe UI", 164f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var unitFont = new GFont("Segoe UI", 56f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var maxFont = new GFont("Segoe UI", 86f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var darkBrush = new SolidBrush(GColor.FromArgb(22, 24, 28)))
                {
                    g.DrawString("15", numFont, darkBrush, new RectangleF(80, 96, 352, 185), sf);
                    g.DrawString("km/h", unitFont, darkBrush, new RectangleF(80, 268, 352, 74), sf);
                    g.DrawString("MÁXIMO", maxFont, darkBrush, new RectangleF(16, 466, w - 32, 162), sf);
                }

                bmp.Save(Path.GetFullPath(assetPath), ImageFormat.Png);
            }
        }

        private static void GeneratePromoChihuahuaTexture(string assetPath)
        {
            const int w = 1280;
            const int h = 720;
            using (var bmp = new Bitmap(w, h))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (var bg = new LinearGradientBrush(
                    new Rectangle(0, 0, w, h),
                    GColor.FromArgb(14, 72, 215),
                    GColor.FromArgb(8, 36, 152),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bg, 0, 0, w, h);
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var mainFont = new GFont("Segoe UI", 132f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var subFont = new GFont("Segoe UI", 54f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var tagFont = new GFont("Segoe UI", 48f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var white = new SolidBrush(GColor.White))
                using (var yellow = new SolidBrush(GColor.FromArgb(255, 214, 48)))
                {
                    g.DrawString("Chihuahua", mainFont, white, new RectangleF(40, 86, w - 80, 180), sf);
                    g.DrawString("es para ti   ¡Conócelo!", subFont, yellow, new RectangleF(80, 270, w - 160, 82), sf);

                    // Social media icon circles matching reference screenshot 1
                    GColor[] ringColors = { GColor.FromArgb(24, 24, 28), GColor.FromArgb(228, 64, 95), GColor.FromArgb(24, 119, 242) };
                    string[] glyphs = { "♪", "◎", "f" };
                    int startX = w / 2 - 210;
                    for (int i = 0; i < 3; i++)
                    {
                        int cx = startX + i * 160;
                        using (var fill = new SolidBrush(ringColors[i]))
                        using (var rim = new Pen(GColor.White, 6f))
                        {
                            g.FillEllipse(fill, cx, 405, 116, 116);
                            g.DrawEllipse(rim, cx, 405, 116, 116);
                        }
                        g.DrawString(glyphs[i], new GFont("Segoe UI", 56f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel), white,
                            new RectangleF(cx, 405, 116, 116), sf);
                    }

                    g.DrawString("PASO DEL NORTE • CENTRO HISTÓRICO", tagFont, white, new RectangleF(60, 566, w - 120, 86), sf);
                }

                bmp.Save(Path.GetFullPath(assetPath), ImageFormat.Png);
            }
        }

        private static void GeneratePromoCentroHistoricoTexture(string assetPath)
        {
            const int w = 1280;
            const int h = 720;
            using (var bmp = new Bitmap(w, h))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (var bg = new LinearGradientBrush(
                    new Rectangle(0, 0, w, h),
                    GColor.FromArgb(14, 28, 72),
                    GColor.FromArgb(30, 56, 112),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bg, 0, 0, w, h);
                }

                // Night desert hill silhouette & glowing Cerro Bola star inspired by photo 2
                using (var hillBrush = new SolidBrush(GColor.FromArgb(18, 22, 34)))
                {
                    PointF[] hill = { new PointF(0, h), new PointF(0, 520), new PointF(640, 330), new PointF(w, 500), new PointF(w, h) };
                    g.FillPolygon(hillBrush, hill);
                }

                PointF[] star = new PointF[10];
                float sx = 640f, sy = 410f, rOut = 92f, rIn = 38f;
                for (int i = 0; i < 10; i++)
                {
                    double ang = -Math.PI / 2.0 + i * Math.PI / 5.0;
                    float rad = (i % 2 == 0) ? rOut : rIn;
                    star[i] = new PointF(sx + (float)Math.Cos(ang) * rad, sy + (float)Math.Sin(ang) * rad);
                }
                using (var starPen = new Pen(GColor.FromArgb(255, 224, 96), 12f))
                {
                    g.DrawPolygon(starPen, star);
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var headFont = new GFont("Segoe UI", 68f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var subFont = new GFont("Segoe UI", 38f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var gold = new SolidBrush(GColor.FromArgb(255, 224, 110)))
                using (var white = new SolidBrush(GColor.White))
                {
                    g.DrawString("FESTIVAL DEL CENTRO HISTÓRICO", headFont, gold, new RectangleF(40, 54, w - 80, 100), sf);
                    g.DrawString("AVENIDA BENITO JUÁREZ • CIUDAD JUÁREZ", subFont, white, new RectangleF(40, 162, w - 80, 62), sf);
                    g.DrawString("¡DISFRUTA CULTURA, GASTRONOMÍA Y MÚSICA!", subFont, white, new RectangleF(40, 588, w - 80, 64), sf);
                }

                bmp.Save(Path.GetFullPath(assetPath), ImageFormat.Png);
            }
        }

        private static GraphicsPath CreateRoundedRectPath(float x, float y, float width, float height, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        #endregion
    }
}
