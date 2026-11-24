using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Idempotent urban-geometry builder that completes the West Manzana ("Manzana Oeste")
    /// perimeter sidewalks, rounded SW/NW corners, delimited interior urban lot pad,
    /// full Calle Ignacio Mariscal corridor (from Calle Azucenas to Calle Gardenias, plus
    /// authentic south intersection stub), Calle Azucenas far-west extension, and the
    /// opposing curved Pedestrian Plaza edge ("Plaza Peatonal") on the West side of
    /// Calle Ignacio Mariscal.
    ///
    /// Grounded in OpenStreetMap survey alignment & real aerial imagery:
    /// - Existing Av. Benito Juarez N-S corridor (Z = -32.91m Azucenas centerline to
    ///   Z = +27.10m Gardenias mouth = 60.01m vs 60.72m real OSM, 98.8% exact 1:1!) is preserved.
    /// - Option A E-W depth: 40.00m building lot depth from Av. Juarez facade (X = -12.00m)
    ///   to Mariscal West building line (X = -52.00m), matching Backrooms Level 1 (40m x 24m).
    /// - Cleanly trims/splits WestTriangleAndAbasoloBuilder's placeholder Gardenias SouthExt
    ///   at the Calle Ignacio Mariscal intersection so the T-junction into Calle Gardenias West
    ///   is 100% open and continuous.
    /// </summary>
    public static class WestManzanaPerimeterBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string RootName = "West_Manzana_Mariscal_Root";
        private const string ArtifactDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";

        // Elevation hierarchy (zero Z-fighting, exact match with WestAzucenasCornerBuilder)
        private const float RoadBotY = -0.10f;
        private const float RoadTopY = -0.0015f;
        private const float MarkingsY = 0.012f;
        private const float SidewalkBotY = -0.05f;
        private const float SidewalkTopY = 0.20f;
        private const float CurbTopY = 0.204f;
        private const float PlazaPavingTopY = 0.206f;
        private const float UrbanLotTopY = 0.212f;
        private const float CurbWidth = 0.20f;

        // Shared East/South/North anchors from existing scene builders
        public const float AzucenasNorthCurbZ = -29.11f;
        public const float AzucenasSouthCurbZ = -36.71f;
        public const float AzucenasSidewalkCutX = -38.00f;   // Exact west end of WestAzucenasCornerBuilder sidewalks
        public const float AzucenasRoadCutX = -42.00f;       // Exact west end of Roadway_Calle_Azucenas_West
        public const float GardeniasSouthCurbZ = 24.30f;     // Exact south curb of Calle Gardenias West
        public const float GardeniasDentalCutX = -34.00f;    // Exact west end of Sidewalk_DentalJarquin_Organic
        public const float LotSouthLineZ = -26.80f;          // Exact south wall of NWCorner_AnnexBuilding
        public const float LotNorthLineZ = 21.72f;           // Exact north wall of Dental_MainBody / Sidewalk_DentalJarquin

        // Option A (1:1 40m E-W Manzana Oeste + Calle Ignacio Mariscal + Plaza Peatonal)
        public const float LotWestLineX = -52.00f;           // 40.00m E-W from Av. Juarez facade (-12.00m)
        public const float MariscalEastCurbX = -54.50f;      // 2.50m Mariscal East sidewalk
        public const float MariscalRoadWidth = 9.50f;        // Authentic 2-lane + parallel parking width
        public const float MariscalWestCurbX = -64.00f;      // Plaza Peatonal East curb (-54.50 - 9.50)
        public const float MariscalSouthStubZ = -54.00f;     // Realistic south stub below Calle Azucenas
        public const float FarWestExtentX = -92.00f;         // Plaza Peatonal & Azucenas far-west extent
        public const float GardeniasFarWestX = -150.00f;     // Matches WestTriangleAndAbasoloBuilder

        // Corner Fillet Radii
        private const float ManzanaSwRadius = 3.00f;
        private const float ManzanaNwRadius = 3.50f;
        private const float PlazaSeRadius = 8.00f;           // Iconic broad circular Plaza corner on satellite/OSM
        private const float PlazaNeRadius = 3.50f;
        private const float SouthOfAzCornerRadius = 3.00f;

        [MenuItem("MyVirtualCommunity/Rebuild West Manzana Perimeter & Calle Ignacio Mariscal")]
        public static void RebuildAndSave()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestManzanaPerimeterBuilder] Failed to open scene: {ScenePath}");
                return;
            }

            BuildInOpenScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WestManzanaPerimeterBuilder] Saved BorderCorridorScene cleanly.");
        }

        [MenuItem("MyVirtualCommunity/Rebuild, Verify & Capture West Manzana + Mariscal")]
        public static void RebuildVerifyAndCapture()
        {
            RebuildAndSave();
            bool ok = RunVerificationChecks();
            CaptureVerificationScreenshots();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        /// <summary>
        /// Safe, idempotent builder method for an already-open BorderCorridorScene (without saving).
        /// Can be invoked standalone or chained at the end of WestTriangleAndAbasoloBuilder.ExecuteRebuild.
        /// </summary>
        public static void BuildInOpenScene()
        {
            CleanupPreviousObjects();
            TrimBlockingGardeniasSouthExt();

            // Load / create consistent project materials
            Material asphaltMat = LoadOrFallbackMat("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.19f, 0.21f), 0.10f);
            Material concreteMat = LoadOrFallbackMat("Assets/Settings/ConcreteMaterial.mat", new Color(0.75f, 0.75f, 0.74f), 0.12f);
            Material curbMat = LoadOrFallbackMat("Assets/Settings/CurbMaterial.mat", new Color(0.64f, 0.64f, 0.63f), 0.10f);
            Material whiteStripeMat = LoadOrFallbackMat("Assets/Settings/CrosswalkWhiteStripe.mat", new Color(0.95f, 0.95f, 0.95f), 0.15f);
            Material yellowCurbMat = LoadOrFallbackMat("Assets/Settings/YellowSafetyCurb.mat", new Color(0.95f, 0.78f, 0.10f), 0.30f);
            Material urbanLotMat = LoadOrFallbackMat("Assets/Settings/West_UrbanLotSurface.mat", new Color(0.36f, 0.35f, 0.34f), 0.05f);
            Material tactileMat = LoadOrFallbackMat("Assets/Settings/West_Azucenas_Tactile.mat", new Color(0.56f, 0.57f, 0.58f), 0.14f);
            Material bollardBlackMat = LoadOrFallbackMat("Assets/Settings/West_Azucenas_BollardBlack.mat", new Color(0.08f, 0.09f, 0.10f), 0.35f);
            Material bollardSilverMat = LoadOrFallbackMat("Assets/Settings/West_Azucenas_BollardSilver.mat", new Color(0.88f, 0.90f, 0.92f), 0.65f);
            Material plazaPavingMat = CreateOrGetMat("Assets/Settings/West_PlazaPeatonal_Paving.mat", new Color(0.45f, 0.41f, 0.39f), 0.10f, 0.0f);
            Material lotBorderLineMat = CreateOrGetMat("Assets/Settings/West_LotDemarcationBorder.mat", new Color(0.52f, 0.51f, 0.49f), 0.08f, 0.0f);
            Material signPostMat = LoadOrFallbackMat("Assets/Settings/BridgeSteel.mat", new Color(0.32f, 0.35f, 0.38f), 0.50f);
            Material streetSignGreenMat = LoadOrFallbackMat("Assets/Settings/StreetSignGreen.mat", new Color(0.05f, 0.38f, 0.19f), 0.25f);

            Transform envParent = GameObject.Find("Environment")?.transform;
            GameObject root = new GameObject(RootName);
            if (envParent != null) root.transform.SetParent(envParent);
            root.transform.position = Vector3.zero;

            // Precompute all smooth Bezier corner arcs (exact tangents!)
            // 1. West Manzana SW corner: (-51.50, -29.11) [going W] -> (-54.50, -26.11) [going N]
            List<Vector2> manzanaSwArc = SampleQuarterArc(
                new Vector2(MariscalEastCurbX + ManzanaSwRadius, AzucenasNorthCurbZ), Vector2.left,
                new Vector2(MariscalEastCurbX, AzucenasNorthCurbZ + ManzanaSwRadius), Vector2.up,
                ManzanaSwRadius, 16);

            // 2. West Manzana NW corner: (-54.50, +20.80) [going N] -> (-51.00, +24.30) [going E]
            List<Vector2> manzanaNwArc = SampleQuarterArc(
                new Vector2(MariscalEastCurbX, GardeniasSouthCurbZ - ManzanaNwRadius), Vector2.up,
                new Vector2(MariscalEastCurbX + ManzanaNwRadius, GardeniasSouthCurbZ), Vector2.right,
                ManzanaNwRadius, 16);

            // 3. Plaza Peatonal SE sweeping corner: (-72.00, -29.11) [going E] -> (-64.00, -21.11) [going N]
            List<Vector2> plazaSeArc = SampleQuarterArc(
                new Vector2(MariscalWestCurbX - PlazaSeRadius, AzucenasNorthCurbZ), Vector2.right,
                new Vector2(MariscalWestCurbX, AzucenasNorthCurbZ + PlazaSeRadius), Vector2.up,
                PlazaSeRadius, 22);

            // 4. Plaza Peatonal NE corner: (-64.00, +20.80) [going N] -> (-67.50, +24.30) [going W]
            List<Vector2> plazaNeArc = SampleQuarterArc(
                new Vector2(MariscalWestCurbX, GardeniasSouthCurbZ - PlazaNeRadius), Vector2.up,
                new Vector2(MariscalWestCurbX - PlazaNeRadius, GardeniasSouthCurbZ), Vector2.left,
                PlazaNeRadius, 16);

            // 5. South-of-Azucenas SE corner (East side of Mariscal south stub):
            //    (-51.50, -36.71) [going W] -> (-54.50, -39.71) [going S]
            List<Vector2> southBlockSeArc = SampleQuarterArc(
                new Vector2(MariscalEastCurbX + SouthOfAzCornerRadius, AzucenasSouthCurbZ), Vector2.left,
                new Vector2(MariscalEastCurbX, AzucenasSouthCurbZ - SouthOfAzCornerRadius), Vector2.down,
                SouthOfAzCornerRadius, 14);

            // 6. South-of-Azucenas SW corner (West side of Mariscal south stub):
            //    (-64.00, -39.71) [going N] -> (-67.00, -36.71) [going W]
            List<Vector2> southBlockSwArc = SampleQuarterArc(
                new Vector2(MariscalWestCurbX, AzucenasSouthCurbZ - SouthOfAzCornerRadius), Vector2.up,
                new Vector2(MariscalWestCurbX - SouthOfAzCornerRadius, AzucenasSouthCurbZ), Vector2.left,
                SouthOfAzCornerRadius, 14);

            // STEP 1: Sub-surface continuous foundation safety collider (prevents any void drop at seams)
            BuildSubsurfaceSafetySlab(root.transform);

            // STEP 2: Roadways (Calle Ignacio Mariscal + Calle Azucenas Far-West + 6 corner aprons + markings)
            BuildRoadwaysAndMarkings(root.transform, manzanaSwArc, manzanaNwArc, plazaSeArc, plazaNeArc,
                southBlockSeArc, southBlockSwArc, asphaltMat, whiteStripeMat);

            // STEP 3: West Manzana Perimeter Sidewalk, Continuous Curbs, Bollards & Delimited Interior Urban Lot
            BuildWestManzanaPerimeterAndLot(root.transform, manzanaSwArc, manzanaNwArc,
                concreteMat, curbMat, yellowCurbMat, urbanLotMat, lotBorderLineMat, tactileMat,
                bollardBlackMat, bollardSilverMat);

            // STEP 4: Plaza Peatonal Delimited Edge on West Side of Calle Ignacio Mariscal + Gardenias Far-West Curb
            BuildPlazaPeatonalAndGardeniasWestContinuation(root.transform, plazaSeArc, plazaNeArc,
                concreteMat, curbMat, plazaPavingMat, bollardBlackMat, bollardSilverMat);

            // STEP 5: South-of-Azucenas Sidewalk Continuations framing the Azucenas x Mariscal 4-way intersection
            BuildSouthOfAzucenasExtensions(root.transform, southBlockSeArc, southBlockSwArc,
                concreteMat, curbMat, urbanLotMat);

            // STEP 6: Corner Street Nomenclature Signposts (Mariscal x Azucenas & Mariscal x Gardenias)
            BuildCornerSignposts(root.transform, signPostMat, streetSignGreenMat);

            Debug.Log("[WestManzanaPerimeterBuilder] Built West Manzana perimeter, urban lot, Calle Ignacio Mariscal & Plaza Peatonal edge cleanly.");
        }

        // =========================================================================
        // CLEANUP & SURGICAL TRIM OF BLOCKING GARDENIAS PLACEHOLDER
        // =========================================================================

        private static void CleanupPreviousObjects()
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                if (tr != null && tr.gameObject.scene.IsValid() && tr.name == RootName)
                {
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
                }
            }
        }

        /// <summary>
        /// Destroys the legacy continuous placeholder Sidewalk_Gardenias_West_SouthExt &
        /// Curb_Gardenias_West_SouthExt (which ran from X = -18.5 to -150m straight across
        /// Calle Ignacio Mariscal!) so that Mariscal opens cleanly into Calle Gardenias West.
        /// Our builder replaces the East segment (-34..-51m) with the West Manzana North sidewalk
        /// and restarts the West segment cleanly west of the Plaza NE curve (-67.5..-150m).
        /// </summary>
        private static void TrimBlockingGardeniasSouthExt()
        {
            string[] legacyNames = { "Sidewalk_Gardenias_West_SouthExt", "Curb_Gardenias_West_SouthExt" };
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                if (tr == null || !tr.gameObject.scene.IsValid()) continue;
                for (int i = 0; i < legacyNames.Length; i++)
                {
                    if (tr.name == legacyNames[i])
                    {
                        UnityEngine.Object.DestroyImmediate(tr.gameObject);
                        break;
                    }
                }
            }
        }

        // =========================================================================
        // STEP 1: SUB-SURFACE FOUNDATION COLLIDER
        // =========================================================================

        private static void BuildSubsurfaceSafetySlab(Transform root)
        {
            var go = new GameObject("WestManzana_Subsurface_SafetyCollider");
            go.transform.SetParent(root);
            // Covers X = [-94..-22], Z = [-55..+26] strictly below RoadTopY (-0.0015m)
            go.transform.position = new Vector3(-58.0f, -0.045f, -14.5f);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(72.0f, 0.05f, 81.0f);
        }

        // =========================================================================
        // STEP 2: ROADWAYS & MARKINGS (ZERO OVERLAP BUTT JOINTS)
        // =========================================================================

        private static void BuildRoadwaysAndMarkings(Transform root,
            List<Vector2> manzanaSwArc, List<Vector2> manzanaNwArc,
            List<Vector2> plazaSeArc, List<Vector2> plazaNeArc,
            List<Vector2> southBlockSeArc, List<Vector2> southBlockSwArc,
            Material asphaltMat, Material whiteStripeMat)
        {
            var roadRoot = new GameObject("Roadways_IgnacioMariscal_And_AzucenasExt");
            roadRoot.transform.SetParent(root);

            // 1. Calle Azucenas Far-West Extension (exact butt joint at X = -42.00m -> -92.00m)
            CreateFlatPrism("Roadway_Calle_Azucenas_FarWest", roadRoot.transform, new List<Vector2>
            {
                new Vector2(FarWestExtentX, AzucenasSouthCurbZ),
                new Vector2(AzucenasRoadCutX, AzucenasSouthCurbZ),
                new Vector2(AzucenasRoadCutX, AzucenasNorthCurbZ),
                new Vector2(FarWestExtentX, AzucenasNorthCurbZ)
            }, RoadBotY, RoadTopY, asphaltMat, addCollider: true);

            // 2. Calle Ignacio Mariscal Main Corridor (Z = -29.11m Azucenas -> Z = +24.30m Gardenias)
            CreateFlatPrism("Roadway_Calle_IgnacioMariscal_Main", roadRoot.transform, new List<Vector2>
            {
                new Vector2(MariscalWestCurbX, AzucenasNorthCurbZ),
                new Vector2(MariscalEastCurbX, AzucenasNorthCurbZ),
                new Vector2(MariscalEastCurbX, GardeniasSouthCurbZ),
                new Vector2(MariscalWestCurbX, GardeniasSouthCurbZ)
            }, RoadBotY, RoadTopY, asphaltMat, addCollider: true);

            // 3. Calle Ignacio Mariscal South Stub (Z = -54.00m -> -36.71m below Azucenas)
            CreateFlatPrism("Roadway_Calle_IgnacioMariscal_SouthStub", roadRoot.transform, new List<Vector2>
            {
                new Vector2(MariscalWestCurbX, MariscalSouthStubZ),
                new Vector2(MariscalEastCurbX, MariscalSouthStubZ),
                new Vector2(MariscalEastCurbX, AzucenasSouthCurbZ),
                new Vector2(MariscalWestCurbX, AzucenasSouthCurbZ)
            }, RoadBotY, RoadTopY, asphaltMat, addCollider: true);

            // 4. Curved Corner Asphalt Aprons filling the exact pocket between straight street lines & curved curbs
            BuildCornerApron("Apron_Manzana_SW", roadRoot.transform, new Vector2(MariscalEastCurbX, AzucenasNorthCurbZ), manzanaSwArc, asphaltMat);
            BuildCornerApron("Apron_Manzana_NW", roadRoot.transform, new Vector2(MariscalEastCurbX, GardeniasSouthCurbZ), manzanaNwArc, asphaltMat);
            BuildCornerApron("Apron_Plaza_SE", roadRoot.transform, new Vector2(MariscalWestCurbX, AzucenasNorthCurbZ), plazaSeArc, asphaltMat);
            BuildCornerApron("Apron_Plaza_NE", roadRoot.transform, new Vector2(MariscalWestCurbX, GardeniasSouthCurbZ), plazaNeArc, asphaltMat);
            BuildCornerApron("Apron_SouthBlock_SE", roadRoot.transform, new Vector2(MariscalEastCurbX, AzucenasSouthCurbZ), southBlockSeArc, asphaltMat);
            BuildCornerApron("Apron_SouthBlock_SW", roadRoot.transform, new Vector2(MariscalWestCurbX, AzucenasSouthCurbZ), southBlockSwArc, asphaltMat);

            // 5. Crisp Road Markings (Centerlines, Stop Bars & Continental Crosswalks)
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            float mariscalMidX = 0.5f * (MariscalEastCurbX + MariscalWestCurbX); // -59.25m
            float azucenasMidZ = 0.5f * (AzucenasNorthCurbZ + AzucenasSouthCurbZ); // -32.91m

            // Dashed centerline along Calle Ignacio Mariscal (between crosswalks: Z = -23.0 .. +18.0m)
            for (float z = -22.5f; z <= 17.5f; z += 5.0f)
            {
                float zEnd = Mathf.Min(z + 2.4f, 18.2f);
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(mariscalMidX - 0.07f, z),
                    new Vector2(mariscalMidX + 0.07f, z),
                    new Vector2(mariscalMidX + 0.07f, zEnd),
                    new Vector2(mariscalMidX - 0.07f, zEnd),
                    MarkingsY);
            }

            // Dashed centerline along Mariscal South Stub (Z = -52.0 .. -40.5m)
            for (float z = -52.0f; z <= -41.5f; z += 5.0f)
            {
                float zEnd = Mathf.Min(z + 2.4f, -40.5f);
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(mariscalMidX - 0.07f, z),
                    new Vector2(mariscalMidX + 0.07f, z),
                    new Vector2(mariscalMidX + 0.07f, zEnd),
                    new Vector2(mariscalMidX - 0.07f, zEnd),
                    MarkingsY);
            }

            // Dashed centerline along Calle Azucenas East-of-Mariscal continuation (-41.5 .. -48.5m)
            for (float x = -42.2f; x >= -48.5f; x -= 4.8f)
            {
                float xStart = Mathf.Max(x - 2.3f, -49.5f);
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(xStart, azucenasMidZ - 0.06f),
                    new Vector2(x, azucenasMidZ - 0.06f),
                    new Vector2(x, azucenasMidZ + 0.06f),
                    new Vector2(xStart, azucenasMidZ + 0.06f),
                    MarkingsY);
            }

            // Dashed centerline along Calle Azucenas West-of-Mariscal (-68.5 .. -90.0m)
            for (float x = -69.5f; x >= -89.0f; x -= 5.0f)
            {
                float xStart = Mathf.Max(x - 2.4f, -90.5f);
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(xStart, azucenasMidZ - 0.06f),
                    new Vector2(x, azucenasMidZ - 0.06f),
                    new Vector2(x, azucenasMidZ + 0.06f),
                    new Vector2(xStart, azucenasMidZ + 0.06f),
                    MarkingsY);
            }

            // Continental Zebra Crosswalk: North mouth of Ignacio Mariscal at Calle Gardenias (Z = +20.8..+23.0m)
            for (float x = MariscalWestCurbX + 0.85f; x <= MariscalEastCurbX - 0.85f; x += 0.95f)
            {
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(x - 0.22f, 20.90f),
                    new Vector2(x + 0.22f, 20.90f),
                    new Vector2(x + 0.22f, 23.00f),
                    new Vector2(x - 0.22f, 23.00f),
                    MarkingsY);
            }
            // Stop bar south of Gardenias crosswalk (Northbound half of Mariscal)
            AddQuadXZ(verts, tris, uvs,
                new Vector2(mariscalMidX + 0.20f, 19.65f),
                new Vector2(MariscalEastCurbX - 0.35f, 19.65f),
                new Vector2(MariscalEastCurbX - 0.35f, 20.05f),
                new Vector2(mariscalMidX + 0.20f, 20.05f),
                MarkingsY);

            // Continental Zebra Crosswalk: South mouth of Ignacio Mariscal at Calle Azucenas (Z = -27.4..-25.3m)
            for (float x = MariscalWestCurbX + 0.85f; x <= MariscalEastCurbX - 0.85f; x += 0.95f)
            {
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(x - 0.22f, -27.50f),
                    new Vector2(x + 0.22f, -27.50f),
                    new Vector2(x + 0.22f, -25.40f),
                    new Vector2(x - 0.22f, -25.40f),
                    MarkingsY);
            }

            // Continental Zebra Crosswalk: Calle Azucenas East side of Mariscal (X = -53.2..-51.1m)
            for (float z = AzucenasSouthCurbZ + 0.65f; z <= AzucenasNorthCurbZ - 0.65f; z += 0.90f)
            {
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(-53.20f, z - 0.20f),
                    new Vector2(-51.10f, z - 0.20f),
                    new Vector2(-51.10f, z + 0.20f),
                    new Vector2(-53.20f, z + 0.20f),
                    MarkingsY);
            }

            // Stop bar east of Azucenas crosswalk (Westbound Azucenas traffic)
            AddQuadXZ(verts, tris, uvs,
                new Vector2(-50.30f, azucenasMidZ + 0.15f),
                new Vector2(-49.95f, azucenasMidZ + 0.15f),
                new Vector2(-49.95f, AzucenasNorthCurbZ - 0.25f),
                new Vector2(-50.30f, AzucenasNorthCurbZ - 0.25f),
                MarkingsY);

            CreateMeshObject("Roadway_IgnacioMariscal_Markings", roadRoot.transform, verts, tris, uvs, whiteStripeMat);
        }

        private static void BuildCornerApron(string name, Transform parent, Vector2 cornerAnchor, List<Vector2> arc, Material asphaltMat)
        {
            var poly = new List<Vector2> { cornerAnchor };
            poly.AddRange(arc);
            CreateFlatPrism(name, parent, poly, RoadBotY, RoadTopY, asphaltMat, addCollider: true);
        }

        // =========================================================================
        // STEP 3: WEST MANZANA PERIMETER SIDEWALK, CURBS & DELIMITED URBAN LOT
        // =========================================================================

        private static void BuildWestManzanaPerimeterAndLot(Transform root,
            List<Vector2> manzanaSwArc, List<Vector2> manzanaNwArc,
            Material concreteMat, Material curbMat, Material yellowCurbMat,
            Material urbanLotMat, Material lotBorderLineMat, Material tactileMat,
            Material bollardBlackMat, Material bollardSilverMat)
        {
            var mzRoot = new GameObject("WestManzana_Perimeter_And_Lot");
            mzRoot.transform.SetParent(root);

            // 1. Continuous U-Shaped Outer Curb Chain walking CLOCKWISE around the West Manzana:
            //    South Azucenas (-38.00 -> -51.50) -> SW Arc -> West Mariscal (-26.11 -> +20.80) -> NW Arc -> North Gardenias (-51.00 -> -34.00)
            //    Sidewalk is consistently on the RIGHT of this directed chain!
            var outerCurbChain = new List<Vector2>();
            outerCurbChain.Add(new Vector2(AzucenasSidewalkCutX, AzucenasNorthCurbZ));
            outerCurbChain.AddRange(manzanaSwArc);
            outerCurbChain.AddRange(manzanaNwArc);
            outerCurbChain.Add(new Vector2(GardeniasDentalCutX, GardeniasSouthCurbZ));

            // Concrete curb along Azucenas + Mariscal + NW arc -> Gardenias
            BuildCurbChain("WestManzana_Curb_Continuous_SW_W_NW", mzRoot.transform, outerCurbChain,
                sidewalkIsRight: true, CurbWidth, CurbTopY, curbMat);

            // Subtle yellow safety curb accent on the NW corner & Gardenias parking stretch (matching Dental_YellowCurb_Organic!)
            var nwYellowChain = new List<Vector2>(manzanaNwArc);
            nwYellowChain.Add(new Vector2(GardeniasDentalCutX, GardeniasSouthCurbZ));
            BuildCurbChain("WestManzana_Curb_GardeniasYellowAccent", mzRoot.transform, nwYellowChain,
                sidewalkIsRight: true, CurbWidth + 0.004f, CurbTopY + 0.002f, yellowCurbMat);

            // 2. Continuous U-Shaped Perimeter Sidewalk Slab (between inner curb offset and the exact lot lines!)
            List<Vector2> innerCurbChain = OffsetChainRight(outerCurbChain, CurbWidth);
            var sidewalkPoly = new List<Vector2>();
            sidewalkPoly.AddRange(innerCurbChain);
            // Return CCW along the exact lot lines back to start:
            //   (-34.00, 21.72) -> (-52.00, 21.72) -> (-52.00, -26.80) -> (-38.00, -26.80)
            sidewalkPoly.Add(new Vector2(GardeniasDentalCutX, LotNorthLineZ));
            sidewalkPoly.Add(new Vector2(LotWestLineX, LotNorthLineZ));
            sidewalkPoly.Add(new Vector2(LotWestLineX, LotSouthLineZ));
            sidewalkPoly.Add(new Vector2(AzucenasSidewalkCutX, LotSouthLineZ));

            CreateFlatPrism("WestManzana_PerimeterSidewalk_U", mzRoot.transform, sidewalkPoly,
                SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // 3. Delimited Interior Urban Lot Pad (Clean concave polygon strictly west of ALL existing buildings!)
            //    Zero overlap with NWCorner_Annex (-26m), Building A/B & Secret Wedge/Optica (-24m), or Dental Jarquin (-32m).
            var urbanLotPoly = new List<Vector2>
            {
                new Vector2(LotWestLineX, LotSouthLineZ),      // (-52.00, -26.80) SW lot corner
                new Vector2(-26.00f, LotSouthLineZ),           // (-26.00, -26.80) Annex rear SW
                new Vector2(-26.00f, -13.45f),                 // (-26.00, -13.45) Annex / Bldg A party wall
                new Vector2(-24.00f, -13.45f),                 // (-24.00, -13.45) Bldg A rear SE
                new Vector2(-24.00f, 15.00f),                  // (-24.00, +15.00) Bldg B rear NE
                new Vector2(-32.00f, 15.00f),                  // (-32.00, +15.00) Dental Jarquin rear SE
                new Vector2(-32.00f, LotNorthLineZ),           // (-32.00, +21.72) Dental Jarquin rear NW
                new Vector2(LotWestLineX, LotNorthLineZ)       // (-52.00, +21.72) NW lot corner
            };
            CreateFlatPrism("WestManzana_UrbanLot_Pad", mzRoot.transform, urbanLotPoly,
                SidewalkBotY, UrbanLotTopY, urbanLotMat, addCollider: true);

            // Crisp architectural demarcation lip along the 3 exposed lot edges (South, West, North)
            BuildDemarcationStrip("WestManzana_LotBorder_South", mzRoot.transform,
                new Vector2(LotWestLineX, LotSouthLineZ), new Vector2(-26.00f, LotSouthLineZ), 0.16f, UrbanLotTopY + 0.004f, lotBorderLineMat);
            BuildDemarcationStrip("WestManzana_LotBorder_West", mzRoot.transform,
                new Vector2(LotWestLineX, LotSouthLineZ), new Vector2(LotWestLineX, LotNorthLineZ), 0.16f, UrbanLotTopY + 0.004f, lotBorderLineMat);
            BuildDemarcationStrip("WestManzana_LotBorder_North", mzRoot.transform,
                new Vector2(LotWestLineX, LotNorthLineZ), new Vector2(-32.00f, LotNorthLineZ), 0.16f, UrbanLotTopY + 0.004f, lotBorderLineMat);

            // 4. Tactile pedestrian warning pads & protective steel corner bollards at SW & NW corners
            CreateBox("TactilePad_Manzana_SW_Mariscal", mzRoot.transform,
                new Vector3(MariscalEastCurbX + 0.65f, SidewalkTopY + 0.004f, -26.45f),
                new Vector3(0.75f, 0.008f, 1.80f), tactileMat, addCollider: false);
            CreateBox("TactilePad_Manzana_SW_Azucenas", mzRoot.transform,
                new Vector3(-52.15f, SidewalkTopY + 0.004f, AzucenasNorthCurbZ + 0.65f),
                new Vector3(1.80f, 0.008f, 0.75f), tactileMat, addCollider: false);
            CreateBox("TactilePad_Manzana_NW_Mariscal", mzRoot.transform,
                new Vector3(MariscalEastCurbX + 0.65f, SidewalkTopY + 0.004f, 21.95f),
                new Vector3(0.75f, 0.008f, 1.80f), tactileMat, addCollider: false);

            // Corner bollards matching WestAzucenasCornerBuilder style
            CreateBollard("Bollard_Manzana_SW_1", mzRoot.transform, new Vector3(-53.55f, SidewalkTopY, -28.15f), bollardBlackMat, bollardSilverMat);
            CreateBollard("Bollard_Manzana_SW_2", mzRoot.transform, new Vector3(-54.05f, SidewalkTopY, -27.20f), bollardBlackMat, bollardSilverMat);
            CreateBollard("Bollard_Manzana_SW_3", mzRoot.transform, new Vector3(-52.60f, SidewalkTopY, -28.65f), bollardBlackMat, bollardSilverMat);

            CreateBollard("Bollard_Manzana_NW_1", mzRoot.transform, new Vector3(-53.40f, SidewalkTopY, 23.20f), bollardBlackMat, bollardSilverMat);
            CreateBollard("Bollard_Manzana_NW_2", mzRoot.transform, new Vector3(-54.00f, SidewalkTopY, 22.10f), bollardBlackMat, bollardSilverMat);
            CreateBollard("Bollard_Manzana_NW_3", mzRoot.transform, new Vector3(-52.15f, SidewalkTopY, 23.80f), bollardBlackMat, bollardSilverMat);
        }

        // =========================================================================
        // STEP 4: PLAZA PEATONAL WEST OF MARISCAL + GARDENIAS FAR-WEST CONTINUATION
        // =========================================================================

        private static void BuildPlazaPeatonalAndGardeniasWestContinuation(Transform root,
            List<Vector2> plazaSeArc, List<Vector2> plazaNeArc,
            Material concreteMat, Material curbMat, Material plazaPavingMat,
            Material bollardBlackMat, Material bollardSilverMat)
        {
            var plazaRoot = new GameObject("West_PlazaPeatonal_Edge");
            plazaRoot.transform.SetParent(root);

            // 1. Continuous Curb wrapping Plaza Peatonal from Far-West Azucenas -> Sweeping SE Curve ->
            //    West Curb of Mariscal -> NE Curve -> Gardenias West Continuation (-150m)!
            //    Walking East along Azucenas -> North along Mariscal -> West along Gardenias keeps Plaza on the LEFT!
            var plazaCurbChain = new List<Vector2>();
            plazaCurbChain.Add(new Vector2(FarWestExtentX, AzucenasNorthCurbZ));
            plazaCurbChain.AddRange(plazaSeArc);
            plazaCurbChain.AddRange(plazaNeArc);
            plazaCurbChain.Add(new Vector2(FarWestExtentX, GardeniasSouthCurbZ));

            BuildCurbChain("PlazaPeatonal_Curb_Continuous", plazaRoot.transform, plazaCurbChain,
                sidewalkIsRight: false, CurbWidth, CurbTopY, curbMat);

            // 2. Concrete Sidewalk + Base Deck of the Plaza Peatonal (X = -92..-64m, Z = -29.11..+24.30m)
            List<Vector2> innerPlazaCurb = OffsetChainLeft(plazaCurbChain, CurbWidth);
            var plazaDeckPoly = new List<Vector2>(innerPlazaCurb);
            CreateFlatPrism("PlazaPeatonal_ConcreteBase", plazaRoot.transform, plazaDeckPoly,
                SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // 3. Recessed Architectural Plaza Paving Surface (inset 2.40m behind sidewalk curb so a clean
            //    concrete perimeter promenade borders Calle Ignacio Mariscal, Calle Azucenas & Calle Gardenias!)
            List<Vector2> innerPromenadeEdge = OffsetChainLeft(plazaCurbChain, 2.40f);
            var plazaPavingPoly = new List<Vector2>();
            for (int i = 0; i < innerPromenadeEdge.Count; i++)
            {
                Vector2 p = innerPromenadeEdge[i];
                // Clamp west edge cleanly at FarWestExtentX + 1.5m so polygon stays strictly simple & inside base
                if (p.x < FarWestExtentX + 1.50f) p.x = FarWestExtentX + 1.50f;
                if (plazaPavingPoly.Count == 0 || Vector2.Distance(plazaPavingPoly[plazaPavingPoly.Count - 1], p) > 0.05f)
                {
                    plazaPavingPoly.Add(p);
                }
            }
            CreateFlatPrism("PlazaPeatonal_PavedExplanada", plazaRoot.transform, plazaPavingPoly,
                SidewalkTopY - 0.01f, PlazaPavingTopY, plazaPavingMat, addCollider: false);

            // Plaza SE curve bollards framing the sweeping pedestrian plaza arc (matches real streetscape!)
            for (int i = 2; i < plazaSeArc.Count - 2; i += 4)
            {
                Vector2 p = plazaSeArc[i];
                Vector2 tangent = (plazaSeArc[i + 1] - plazaSeArc[i - 1]).normalized;
                Vector2 inward = new Vector2(-tangent.y, tangent.x); // Left = into Plaza
                Vector2 bp = p + inward * 0.70f;
                CreateBollard($"Bollard_Plaza_SE_{i}", plazaRoot.transform, new Vector3(bp.x, SidewalkTopY, bp.y), bollardBlackMat, bollardSilverMat);
            }

            // 4. Clean Continuation of Calle Gardenias West South Sidewalk & Curb BEYOND Plaza (-92m to -150m)
            //    so WestTriangleAndAbasoloBuilder's far-west roadway (-150m) remains fully curbed!
            float farLen = FarWestExtentX - GardeniasFarWestX; // 58.0m (-150..-92)
            float farMidX = 0.5f * (GardeniasFarWestX + FarWestExtentX); // -121.0m
            CreateBox("Sidewalk_Gardenias_FarWest_SouthExt", plazaRoot.transform,
                new Vector3(farMidX, 0.10f, 20.15f), new Vector3(farLen, 0.20f, 8.30f), concreteMat, addCollider: true);
            CreateBox("Curb_Gardenias_FarWest_SouthExt", plazaRoot.transform,
                new Vector3(farMidX, 0.10f, GardeniasSouthCurbZ - 0.10f), new Vector3(farLen, 0.20f, CurbWidth), curbMat, addCollider: true);
        }

        // =========================================================================
        // STEP 5: SOUTH-OF-AZUCENAS EXTENSIONS (FRAMING THE 4-WAY INTERSECTION)
        // =========================================================================

        private static void BuildSouthOfAzucenasExtensions(Transform root,
            List<Vector2> southBlockSeArc, List<Vector2> southBlockSwArc,
            Material concreteMat, Material curbMat, Material urbanLotMat)
        {
            var southRoot = new GameObject("SouthOfAzucenas_MariscalCorners");
            southRoot.transform.SetParent(root);

            // 1. East-of-Mariscal, South-of-Azucenas continuation (from X = -38.00m west to Mariscal East curb & south to -54m)
            //    Walking West along Azucenas South curb -> SE Arc -> South along Mariscal East curb keeps sidewalk on the LEFT!
            var eastChain = new List<Vector2>();
            eastChain.Add(new Vector2(AzucenasSidewalkCutX, AzucenasSouthCurbZ));
            eastChain.AddRange(southBlockSeArc);
            eastChain.Add(new Vector2(MariscalEastCurbX, MariscalSouthStubZ));

            BuildCurbChain("AzucenasSouth_MariscalSE_Curb", southRoot.transform, eastChain,
                sidewalkIsRight: false, CurbWidth, CurbTopY, curbMat);

            List<Vector2> eastInner = OffsetChainLeft(eastChain, CurbWidth);
            var eastPoly = new List<Vector2>(eastInner);
            eastPoly.Add(new Vector2(AzucenasSidewalkCutX, MariscalSouthStubZ));
            CreateFlatPrism("AzucenasSouth_MariscalSE_Sidewalk", southRoot.transform, eastPoly,
                SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // Clean urban lot infill south of Azucenas (inset 2.4m behind sidewalk)
            CreateFlatPrism("AzucenasSouth_MariscalSE_UrbanLot", southRoot.transform, new List<Vector2>
            {
                new Vector2(LotWestLineX, MariscalSouthStubZ),
                new Vector2(AzucenasSidewalkCutX, MariscalSouthStubZ),
                new Vector2(AzucenasSidewalkCutX, AzucenasSouthCurbZ - 2.40f),
                new Vector2(LotWestLineX, AzucenasSouthCurbZ - 2.40f)
            }, SidewalkTopY - 0.01f, UrbanLotTopY, urbanLotMat, addCollider: false);

            // 2. West-of-Mariscal, South-of-Azucenas corner (X = -92..-64m, Z = -54..-36.71m)
            //    Walking North along Mariscal West curb -> SW Arc -> West along Azucenas South curb keeps sidewalk on the LEFT!
            var westChain = new List<Vector2>();
            westChain.Add(new Vector2(MariscalWestCurbX, MariscalSouthStubZ));
            westChain.AddRange(southBlockSwArc);
            westChain.Add(new Vector2(FarWestExtentX, AzucenasSouthCurbZ));

            BuildCurbChain("AzucenasSouth_MariscalSW_Curb", southRoot.transform, westChain,
                sidewalkIsRight: false, CurbWidth, CurbTopY, curbMat);

            List<Vector2> westInner = OffsetChainLeft(westChain, CurbWidth);
            var westPoly = new List<Vector2>(westInner);
            westPoly.Add(new Vector2(FarWestExtentX, MariscalSouthStubZ));
            CreateFlatPrism("AzucenasSouth_MariscalSW_Sidewalk", southRoot.transform, westPoly,
                SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            CreateFlatPrism("AzucenasSouth_MariscalSW_UrbanLot", southRoot.transform, new List<Vector2>
            {
                new Vector2(FarWestExtentX, MariscalSouthStubZ),
                new Vector2(MariscalWestCurbX - 2.40f, MariscalSouthStubZ),
                new Vector2(MariscalWestCurbX - 2.40f, AzucenasSouthCurbZ - 2.40f),
                new Vector2(FarWestExtentX, AzucenasSouthCurbZ - 2.40f)
            }, SidewalkTopY - 0.01f, UrbanLotTopY, urbanLotMat, addCollider: false);
        }

        // =========================================================================
        // STEP 6: STREET NOMENCLATURE SIGNPOSTS
        // =========================================================================

        private static void BuildCornerSignposts(Transform root, Material poleMat, Material greenMat)
        {
            CreateStreetSignPost("StreetSign_Mariscal_Azucenas", root,
                new Vector3(LotWestLineX - 0.55f, SidewalkTopY, LotSouthLineZ - 0.55f), poleMat, greenMat);
            CreateStreetSignPost("StreetSign_Mariscal_Gardenias", root,
                new Vector3(LotWestLineX - 0.55f, SidewalkTopY, LotNorthLineZ + 0.55f), poleMat, greenMat);
        }

        private static void CreateStreetSignPost(string name, Transform parent, Vector3 basePos, Material poleMat, Material greenMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = basePos;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(go.transform);
            pole.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.65f, 0.08f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

            var bladeNS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bladeNS.name = "Blade_Mariscal";
            bladeNS.transform.SetParent(go.transform);
            bladeNS.transform.localPosition = new Vector3(0f, 3.10f, 0f);
            bladeNS.transform.localScale = new Vector3(0.04f, 0.22f, 0.85f);
            bladeNS.GetComponent<MeshRenderer>().sharedMaterial = greenMat;
            UnityEngine.Object.DestroyImmediate(bladeNS.GetComponent<Collider>());

            var bladeEW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bladeEW.name = "Blade_CrossStreet";
            bladeEW.transform.SetParent(go.transform);
            bladeEW.transform.localPosition = new Vector3(0f, 2.84f, 0f);
            bladeEW.transform.localScale = new Vector3(0.85f, 0.22f, 0.04f);
            bladeEW.GetComponent<MeshRenderer>().sharedMaterial = greenMat;
            UnityEngine.Object.DestroyImmediate(bladeEW.GetComponent<Collider>());
        }

        // =========================================================================
        // VERIFICATION SUITE & MULTI-ANGLE SCREENSHOT CAPTURE
        // =========================================================================

        public static bool RunVerificationChecks()
        {
            int passed = 0, total = 0;
            void Check(string desc, bool cond)
            {
                total++;
                if (cond) { passed++; Debug.Log($"[WestManzanaVerify] PASS: {desc}"); }
                else Debug.LogError($"[WestManzanaVerify] FAIL: {desc}");
            }

            Physics.SyncTransforms();

            Check("West_Manzana_Mariscal_Root exists", GameObject.Find(RootName) != null);
            Check("Legacy blocking Sidewalk_Gardenias_West_SouthExt & Curb removed",
                GameObject.Find("Sidewalk_Gardenias_West_SouthExt") == null &&
                GameObject.Find("Curb_Gardenias_West_SouthExt") == null);
            Check("Calle Ignacio Mariscal roadway & Azucenas far-west extension exist",
                GameObject.Find("Roadway_Calle_IgnacioMariscal_Main") != null &&
                GameObject.Find("Roadway_Calle_IgnacioMariscal_SouthStub") != null &&
                GameObject.Find("Roadway_Calle_Azucenas_FarWest") != null);
            Check("West Manzana continuous U-sidewalk, curbs & delimited urban lot exist",
                GameObject.Find("WestManzana_PerimeterSidewalk_U") != null &&
                GameObject.Find("WestManzana_Curb_Continuous_SW_W_NW") != null &&
                GameObject.Find("WestManzana_UrbanLot_Pad") != null);
            Check("Plaza Peatonal west edge & curved curb exist",
                GameObject.Find("PlazaPeatonal_Curb_Continuous") != null &&
                GameObject.Find("PlazaPeatonal_ConcreteBase") != null &&
                GameObject.Find("PlazaPeatonal_PavedExplanada") != null);
            Check("Gardenias far-west continuation west of Plaza exists",
                GameObject.Find("Sidewalk_Gardenias_FarWest_SouthExt") != null);

            // Raycast ground elevation checks across every critical walkable / drivable point!
            bool HitAt(float x, float z, float expectedMinY, float expectedMaxY, out float hitY, out string hitName)
            {
                hitY = -999f; hitName = "NONE";
                if (Physics.Raycast(new Vector3(x, 3.0f, z), Vector3.down, out RaycastHit hit, 10.0f))
                {
                    hitY = hit.point.y;
                    hitName = hit.collider.name;
                    return hitY >= expectedMinY && hitY <= expectedMaxY;
                }
                return false;
            }

            float y; string nm;
            Check($"Mariscal roadway center (-59.25, 0) is asphalt Y~0.00m",
                HitAt(-59.25f, 0.0f, -0.02f, 0.03f, out y, out nm));
            Check($"Mariscal x Gardenias T-junction (-59.25, +24.30) is OPEN asphalt (no blocking curb!)",
                HitAt(-59.25f, 24.30f, -0.02f, 0.03f, out y, out nm));
            Check($"Mariscal x Azucenas 4-way junction (-59.25, -32.91) is asphalt Y~0.00m",
                HitAt(-59.25f, -32.91f, -0.02f, 0.03f, out y, out nm));
            Check($"West Manzana Mariscal sidewalk (-53.25, 0) is walkable Y~0.20m",
                HitAt(-53.25f, 0.0f, 0.18f, 0.22f, out y, out nm));
            Check($"West Manzana Azucenas sidewalk (-45.0, -28.0) is walkable Y~0.20m",
                HitAt(-45.0f, -28.0f, 0.18f, 0.22f, out y, out nm));
            Check($"West Manzana Gardenias sidewalk (-42.0, +23.0) is walkable Y~0.20m",
                HitAt(-42.0f, 23.0f, 0.18f, 0.22f, out y, out nm));
            Check($"West Manzana Urban Lot center (-40.0, 0.0) is solid Y~0.212m",
                HitAt(-40.0f, 0.0f, 0.19f, 0.23f, out y, out nm));
            Check($"Plaza Peatonal west of Mariscal (-72.0, 0.0) is solid Y~0.20m",
                HitAt(-72.0f, 0.0f, 0.18f, 0.23f, out y, out nm));

            // Confirm DynamicsManager simulationMode remained Auto/FixedUpdate (regression guard!)
            Check("Physics.simulationMode is FixedUpdate (not frozen Script)",
                Physics.simulationMode == SimulationMode.FixedUpdate);

            Debug.Log($"[WestManzanaVerify] SUMMARY: {passed}/{total} checks passed.");
            return passed == total;
        }

        public static void CaptureVerificationScreenshots()
        {
            Directory.CreateDirectory(ArtifactDir);
            var camGo = new GameObject("Temp_WestManzanaVerifyCam");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = 54f;
            cam.clearFlags = CameraClearFlags.Skybox;

            void Shoot(string filename, Vector3 pos, Vector3 lookAt, float fov = 54f)
            {
                cam.fieldOfView = fov;
                cam.transform.position = pos;
                cam.transform.LookAt(lookAt);
                var rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(ArtifactDir, filename), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }

            // 1. High aerial top-down overview of entire West Manzana + Mariscal + Plaza + Azucenas + Gardenias
            Shoot("west_manzana_aerial_overview.png",
                new Vector3(-42.0f, 92.0f, -6.0f),
                new Vector3(-42.0f, 0.0f, -4.0f), 58f);

            // 2. Street-level looking North along Calle Ignacio Mariscal from Azucenas toward Gardenias
            Shoot("west_manzana_mariscal_north.png",
                new Vector3(-59.25f, 3.2f, -42.0f),
                new Vector3(-56.5f, 1.5f, 18.0f), 58f);

            // 3. Elevated view of NW Corner (Calle Ignacio Mariscal x Calle Gardenias West junction)
            Shoot("west_manzana_nw_gardenias_junction.png",
                new Vector3(-72.0f, 14.0f, 38.0f),
                new Vector3(-50.0f, 0.5f, 18.0f), 54f);

            // 4. Elevated view of SW Corner (Calle Azucenas x Calle Ignacio Mariscal + sweeping Plaza SE curve)
            Shoot("west_manzana_sw_azucenas_plaza.png",
                new Vector3(-46.0f, 16.0f, -48.0f),
                new Vector3(-60.0f, 0.5f, -24.0f), 56f);

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[WestManzanaPerimeterBuilder] Captured 4 verification screenshots.");
        }

        // =========================================================================
        // GEOMETRY PRIMITIVES (ROBUST EAR-CLIPPING + CURB CHAINS + BOLLARDS)
        // =========================================================================

        private static List<Vector2> SampleQuarterArc(Vector2 start, Vector2 startTangent,
            Vector2 end, Vector2 endTangent, float radius, int segments)
        {
            float handle = 0.55228475f * radius;
            Vector2 p0 = start;
            Vector2 p1 = start + startTangent.normalized * handle;
            Vector2 p2 = end - endTangent.normalized * handle;
            Vector2 p3 = end;

            var pts = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float u = 1f - t;
                Vector2 p = (u * u * u) * p0 + (3f * u * u * t) * p1 + (3f * u * t * t) * p2 + (t * t * t) * p3;
                pts.Add(p);
            }
            return pts;
        }

        private static List<Vector2> OffsetChainRight(List<Vector2> chain, float offset)
        {
            return OffsetChain(chain, offset, rightSide: true);
        }

        private static List<Vector2> OffsetChainLeft(List<Vector2> chain, float offset)
        {
            return OffsetChain(chain, offset, rightSide: false);
        }

        private static List<Vector2> OffsetChain(List<Vector2> chain, float offset, bool rightSide)
        {
            var res = new List<Vector2>(chain.Count);
            for (int i = 0; i < chain.Count; i++)
            {
                Vector2 nPrev = Vector2.zero;
                Vector2 nNext = Vector2.zero;
                if (i > 0)
                {
                    Vector2 d = (chain[i] - chain[i - 1]).normalized;
                    nPrev = rightSide ? new Vector2(d.y, -d.x) : new Vector2(-d.y, d.x);
                }
                if (i < chain.Count - 1)
                {
                    Vector2 d = (chain[i + 1] - chain[i]).normalized;
                    nNext = rightSide ? new Vector2(d.y, -d.x) : new Vector2(-d.y, d.x);
                }

                Vector2 n;
                float scale = offset;
                if (i == 0) n = nNext;
                else if (i == chain.Count - 1) n = nPrev;
                else
                {
                    Vector2 sum = nPrev + nNext;
                    if (sum.sqrMagnitude < 1e-6f) n = nPrev;
                    else
                    {
                        n = sum.normalized;
                        float dot = Mathf.Max(0.4f, Vector2.Dot(n, nPrev));
                        scale = offset / dot;
                    }
                }
                res.Add(chain[i] + n * scale);
            }
            return res;
        }

        private static void BuildCurbChain(string name, Transform parent, List<Vector2> outerPts,
            bool sidewalkIsRight, float width, float topY, Material mat)
        {
            var clean = DeduplicateChain(outerPts);
            if (clean.Count < 2) return;
            List<Vector2> innerPts = sidewalkIsRight ? OffsetChainRight(clean, width) : OffsetChainLeft(clean, width);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            float botY = -0.05f;
            float dist = 0f;

            for (int i = 0; i < clean.Count - 1; i++)
            {
                Vector2 o0 = clean[i];
                Vector2 o1 = clean[i + 1];
                Vector2 in0 = innerPts[i];
                Vector2 in1 = innerPts[i + 1];
                float segLen = Vector2.Distance(o0, o1);
                if (segLen < 1e-4f) continue;

                // Ensure top face normal points +Y regardless of left/right offset
                Vector2 a0 = sidewalkIsRight ? o0 : in0;
                Vector2 a1 = sidewalkIsRight ? o1 : in1;
                Vector2 b0 = sidewalkIsRight ? in0 : o0;
                Vector2 b1 = sidewalkIsRight ? in1 : o1;

                // Top face (CW in XZ: a0 -> a1 -> b1 -> b0)
                AddQuad3D(verts, tris, uvs,
                    new Vector3(a0.x, topY, a0.y),
                    new Vector3(a1.x, topY, a1.y),
                    new Vector3(b1.x, topY, b1.y),
                    new Vector3(b0.x, topY, b0.y),
                    dist, dist + segLen, 0f, width);

                // Outer face (street-facing)
                Vector3 fo0Top = new Vector3(o0.x, topY, o0.y);
                Vector3 fo1Top = new Vector3(o1.x, topY, o1.y);
                Vector3 fo1Bot = new Vector3(o1.x, botY, o1.y);
                Vector3 fo0Bot = new Vector3(o0.x, botY, o0.y);
                if (sidewalkIsRight)
                    AddQuad3D(verts, tris, uvs, fo1Top, fo0Top, fo0Bot, fo1Bot, dist, dist + segLen, 0f, 0.25f);
                else
                    AddQuad3D(verts, tris, uvs, fo0Top, fo1Top, fo1Bot, fo0Bot, dist, dist + segLen, 0f, 0.25f);

                // Inner face
                Vector3 fi0Top = new Vector3(in0.x, topY, in0.y);
                Vector3 fi1Top = new Vector3(in1.x, topY, in1.y);
                Vector3 fi1Bot = new Vector3(in1.x, botY, in1.y);
                Vector3 fi0Bot = new Vector3(in0.x, botY, in0.y);
                if (sidewalkIsRight)
                    AddQuad3D(verts, tris, uvs, fi0Top, fi1Top, fi1Bot, fi0Bot, dist, dist + segLen, 0f, 0.25f);
                else
                    AddQuad3D(verts, tris, uvs, fi1Top, fi0Top, fi0Bot, fi1Bot, dist, dist + segLen, 0f, 0.25f);

                dist += segLen;
            }

            var go = CreateMeshObject(name, parent, verts, tris, uvs, mat);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void BuildDemarcationStrip(string name, Transform parent, Vector2 a, Vector2 b, float width, float y, Material mat)
        {
            Vector2 dir = (b - a);
            float len = dir.magnitude;
            if (len < 0.01f) return;
            Vector2 mid = 0.5f * (a + b);
            float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(mid.x, y - 0.004f, mid.y);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(width, 0.008f, len);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void CreateBollard(string name, Transform parent, Vector3 basePos, Material blackMat, Material silverMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = basePos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            body.transform.localScale = new Vector3(0.14f, 0.44f, 0.14f);
            body.GetComponent<MeshRenderer>().sharedMaterial = blackMat;

            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "ReflectiveBand";
            band.transform.SetParent(root.transform);
            band.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            band.transform.localScale = new Vector3(0.146f, 0.045f, 0.146f);
            band.GetComponent<MeshRenderer>().sharedMaterial = silverMat;
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
        }

        private static void CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool addCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!addCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>
        /// Extrudes any simple 2D polygon (convex OR concave!) in XZ into a manifold 3D prism
        /// with guaranteed upward +Y top normals, outward side walls, planar UVs, and optional MeshCollider.
        /// </summary>
        private static GameObject CreateFlatPrism(string name, Transform parent, List<Vector2> rawPoly,
            float botY, float topY, Material mat, bool addCollider)
        {
            List<Vector2> poly = DeduplicateClosedPolygon(rawPoly);
            if (poly.Count < 3) return null;

            // Normalize to strictly CCW in mathematical XZ (signedArea > 0)
            if (SignedArea2D(poly) < 0f) poly.Reverse();

            List<int> triIndices = EarClipTriangulateCCW(poly);
            if (triIndices.Count < 3)
            {
                Debug.LogWarning($"[WestManzanaPerimeterBuilder] Triangulation failed for '{name}' (verts={poly.Count}).");
                return null;
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            // Top face (emit reversed (a, c, b) = CW in Unity XZ -> +Y normal!)
            int topBase = verts.Count;
            for (int i = 0; i < poly.Count; i++)
            {
                verts.Add(new Vector3(poly[i].x, topY, poly[i].y));
                uvs.Add(new Vector2(poly[i].x * 0.12f, poly[i].y * 0.12f));
            }
            for (int i = 0; i < triIndices.Count; i += 3)
            {
                tris.Add(topBase + triIndices[i]);
                tris.Add(topBase + triIndices[i + 2]);
                tris.Add(topBase + triIndices[i + 1]);
            }

            // Side skirt walls (for CCW edge a -> b, outward is RIGHT -> (aTop, bTop, bBot) & (aTop, bBot, aBot))
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];
                float len = Vector2.Distance(a, b);
                if (len < 1e-4f) continue;

                AddQuad3D(verts, tris, uvs,
                    new Vector3(a.x, topY, a.y),
                    new Vector3(b.x, topY, b.y),
                    new Vector3(b.x, botY, b.y),
                    new Vector3(a.x, botY, a.y),
                    0f, len * 0.25f, 0f, (topY - botY));
            }

            var go = CreateMeshObject(name, parent, verts, tris, uvs, mat);
            if (addCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }

        private static List<Vector2> DeduplicateChain(List<Vector2> pts)
        {
            var res = new List<Vector2>();
            for (int i = 0; i < pts.Count; i++)
            {
                if (res.Count == 0 || Vector2.Distance(res[res.Count - 1], pts[i]) > 0.005f)
                    res.Add(pts[i]);
            }
            return res;
        }

        private static List<Vector2> DeduplicateClosedPolygon(List<Vector2> pts)
        {
            var res = DeduplicateChain(pts);
            while (res.Count > 2 && Vector2.Distance(res[0], res[res.Count - 1]) < 0.005f)
                res.RemoveAt(res.Count - 1);
            return res;
        }

        private static float SignedArea2D(List<Vector2> poly)
        {
            float area = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];
                area += (a.x * b.y - b.x * a.y);
            }
            return 0.5f * area;
        }

        private static float Cross2D(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        private static bool PointInTriangleCCW(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            const float eps = -1e-5f;
            return Cross2D(b - a, p - a) >= eps &&
                   Cross2D(c - b, p - b) >= eps &&
                   Cross2D(a - c, p - c) >= eps;
        }

        private static List<int> EarClipTriangulateCCW(List<Vector2> poly)
        {
            var tris = new List<int>();
            var remaining = new List<int>(poly.Count);
            for (int i = 0; i < poly.Count; i++) remaining.Add(i);

            int guard = poly.Count * poly.Count * 2;
            while (remaining.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int iPrev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int iCurr = remaining[i];
                    int iNext = remaining[(i + 1) % remaining.Count];

                    Vector2 a = poly[iPrev];
                    Vector2 b = poly[iCurr];
                    Vector2 c = poly[iNext];

                    // Must be strictly convex in CCW order
                    if (Cross2D(b - a, c - b) <= 1e-6f) continue;

                    bool containsOther = false;
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        int idx = remaining[j];
                        if (idx == iPrev || idx == iCurr || idx == iNext) continue;
                        if (PointInTriangleCCW(poly[idx], a, b, c))
                        {
                            containsOther = true;
                            break;
                        }
                    }

                    if (!containsOther)
                    {
                        tris.Add(iPrev);
                        tris.Add(iCurr);
                        tris.Add(iNext);
                        remaining.RemoveAt(i);
                        clipped = true;
                        break;
                    }
                }
                if (!clipped) break;
            }

            if (remaining.Count == 3)
            {
                tris.Add(remaining[0]);
                tris.Add(remaining[1]);
                tris.Add(remaining[2]);
            }
            return tris;
        }

        private static void AddQuadXZ(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector2 p00, Vector2 p10, Vector2 p11, Vector2 p01, float y)
        {
            // CW in Unity XZ (p00 -> p01 -> p11 -> p10) -> +Y normal
            int b = verts.Count;
            verts.Add(new Vector3(p00.x, y, p00.y));
            verts.Add(new Vector3(p01.x, y, p01.y));
            verts.Add(new Vector3(p11.x, y, p11.y));
            verts.Add(new Vector3(p10.x, y, p10.y));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        private static void AddQuad3D(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float u0, float u1, float w0, float w1)
        {
            int b = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            uvs.Add(new Vector2(u0, w0));
            uvs.Add(new Vector2(u1, w0));
            uvs.Add(new Vector2(u1, w1));
            uvs.Add(new Vector2(u0, w1));
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        private static GameObject CreateMeshObject(string name, Transform parent,
            List<Vector3> verts, List<int> tris, List<Vector2> uvs, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;

            var mesh = new Mesh { name = $"{name}_Mesh" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            return go;
        }

        // =========================================================================
        // MATERIAL HELPERS
        // =========================================================================

        private static Material LoadOrFallbackMat(string path, Color fallbackColor, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            return CreateOrGetMat(path, fallbackColor, smoothness, 0f);
        }

        private static Material CreateOrGetMat(string path, Color color, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
