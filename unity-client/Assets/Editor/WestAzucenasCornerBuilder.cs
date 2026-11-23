using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Idempotent builder that corrects the West-North (Oeste-Norte) sidewalk corner
    /// of Av. Benito Juárez &amp; Calle Azucenas, opens the Calle Azucenas West corridor,
    /// adds pedestrian curb ramps, black corner bollards, NW corner building extension
    /// (facing Calle Azucenas with terracotta coping and beige awning), and the SW
    /// corner counterpart with diagonal tactile strip matching the reference photograph.
    /// </summary>
    public static class WestAzucenasCornerBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string RootName = "West_Azucenas_Corner_Root";

        private const float RoadBotY = -0.10f;
        private const float RoadTopY = 0.00f;
        private const float SidewalkBotY = -0.05f;
        private const float SidewalkTopY = 0.20f;
        private const float MarkingsY = 0.012f;

        // Shared Av. Juárez x Calle Azucenas coordinates (symmetric with East Azucenas)
        public const float AzNorthAtJuarez = -29.11f;
        public const float AzSouthAtJuarez = -36.71f;
        public const float JuarezWestCurbFaceX = -2.60f;
        public const float CurbWidth = 0.20f;
        public const float NwFilletRadius = 3.00f;
        public const float SwFilletRadius = 3.50f;
        public const float SwWestCutZ = AzNorthAtJuarez + NwFilletRadius;   // -26.11f
        public const float SwSouthCutZ = AzSouthAtJuarez - SwFilletRadius;  // -40.21f

        private const float WestExtentX = -42.00f;
        private const float SouthExtentZ = -60.00f;
        private const float RampWestX = -8.80f;
        private const float RampEastX = -6.60f;
        private const float RampWingWidth = 0.30f;

        [MenuItem("MyVirtualCommunity/Rebuild West Azucenas NW Corner")]
        public static void RebuildAndCapture()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestAzucenasCornerBuilder] Failed to open scene: {ScenePath}");
                return;
            }

            BuildInOpenScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WestAzucenasCornerBuilder] Saved BorderCorridorScene cleanly.");
        }

        /// <summary>
        /// Rebuilds the West Azucenas NW corner, street opening, building annex, and SW
        /// counterpart inside an already-open BorderCorridorScene (without saving).
        /// Safe to call standalone or at the end of WestTriangleAndAbasoloBuilder.ExecuteRebuild.
        /// </summary>
        public static void BuildInOpenScene()
        {
            CleanupPreviousObjects();
            TrimNorthWestSidewalkAndCurb();

            Material asphaltMat = LoadOrFallbackMat("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.19f, 0.21f), 0.10f);
            Material concreteMat = LoadOrFallbackMat("Assets/Settings/ConcreteMaterial.mat", new Color(0.75f, 0.75f, 0.74f), 0.12f);
            Material curbMat = LoadOrFallbackMat("Assets/Settings/CurbMaterial.mat", new Color(0.64f, 0.64f, 0.63f), 0.10f);
            Material whiteStripeMat = LoadOrFallbackMat("Assets/Settings/CrosswalkWhiteStripe.mat", new Color(0.95f, 0.95f, 0.95f), 0.15f);
            Material tactileMat = CreateOrGetMat("Assets/Settings/West_Azucenas_Tactile.mat", new Color(0.56f, 0.57f, 0.58f), 0.14f, 0.0f);
            Material flagstoneMat = CreateOrGetMat("Assets/Settings/West_Azucenas_Flagstone.mat", new Color(0.73f, 0.67f, 0.61f), 0.08f, 0.0f);
            Material bollardBlackMat = CreateOrGetMat("Assets/Settings/West_Azucenas_BollardBlack.mat", new Color(0.08f, 0.09f, 0.10f), 0.35f, 0.30f);
            Material bollardReflectiveMat = CreateOrGetMat("Assets/Settings/West_Azucenas_BollardSilver.mat", new Color(0.88f, 0.90f, 0.92f), 0.65f, 0.50f);
            Material lampPoleMat = CreateOrGetMat("Assets/Settings/West_Azucenas_LampDark.mat", new Color(0.11f, 0.12f, 0.14f), 0.42f, 0.45f);

            // Architectural materials matching WestSidewalkBlockBuilder
            Material creamStuccoMat = LoadOrFallbackMat("Assets/Settings/West_CreamStucco.mat", new Color(0.90f, 0.88f, 0.84f), 0.08f);
            Material weatheredCreamMat = LoadOrFallbackMat("Assets/Settings/West_WeatheredCream.mat", new Color(0.84f, 0.81f, 0.76f), 0.05f);
            Material terracottaMat = LoadOrFallbackMat("Assets/Settings/West_TerracottaPlaque.mat", new Color(0.48f, 0.21f, 0.13f), 0.15f);
            Material darkZocaloMat = LoadOrFallbackMat("Assets/Settings/West_DarkZocalo.mat", new Color(0.22f, 0.25f, 0.27f), 0.12f);
            Material beigeAwningMat = LoadOrFallbackMat("Assets/Settings/West_BeigeAwning.mat", new Color(0.76f, 0.65f, 0.50f), 0.08f);
            Material awningValanceMat = LoadOrFallbackMat("Assets/Settings/West_AwningValance.mat", new Color(0.68f, 0.57f, 0.43f), 0.08f);
            Material darkGlassMat = LoadOrFallbackMat("Assets/Settings/West_DarkGlass.mat", new Color(0.10f, 0.13f, 0.16f), 0.82f);
            Material aluminumMat = LoadOrFallbackMat("Assets/Settings/West_Aluminum.mat", new Color(0.62f, 0.65f, 0.68f), 0.60f);
            Material shutterMat = LoadOrFallbackMat("Assets/Settings/West_MetalShutter.mat", new Color(0.55f, 0.57f, 0.59f), 0.35f);
            Material darkCorniceMat = LoadOrFallbackMat("Assets/Settings/West_DarkCornice.mat", new Color(0.32f, 0.34f, 0.35f), 0.12f);
            Material whiteFrameMat = LoadOrFallbackMat("Assets/Settings/West_WhiteFrame.mat", new Color(0.93f, 0.94f, 0.95f), 0.30f);
            Material blackFrameMat = LoadOrFallbackMat("Assets/Settings/West_OpticaBlackFrame.mat", new Color(0.09f, 0.10f, 0.11f), 0.38f);
            Material interiorDarkMat = LoadOrFallbackMat("Assets/Settings/West_InteriorDark.mat", new Color(0.07f, 0.08f, 0.09f), 0.05f);
            Material counterGreyMat = LoadOrFallbackMat("Assets/Settings/West_OpticaCounterGrey.mat", new Color(0.45f, 0.48f, 0.51f), 0.28f);
            Material clearGlassMat = LoadOrFallbackMat("Assets/Settings/West_StorefrontClearGlass.mat", new Color(0.24f, 0.34f, 0.40f, 0.32f), 0.90f);
            Material cfeDomeGlassMat = LoadOrFallbackMat("Assets/Settings/West_CFE_DomeGlass.mat", new Color(0.84f, 0.90f, 0.92f, 0.34f), 0.92f);
            Material cfeMeterFaceMat = CreateOrGetTexMat("Assets/Settings/West_CFE_MeterFace.mat", "Assets/Textures/Environment/T_West_CFE_MeterFace.png");
            Material stoneZocaloMat = CreateOrGetMat("Assets/Settings/West_SW_StoneZocalo.mat", new Color(0.30f, 0.31f, 0.32f), 0.14f, 0.0f);
            Material whiteShutterMat = CreateOrGetTexMat("Assets/Settings/West_BonanzaWhiteShutter.mat", "Assets/Textures/Environment/T_West_MetalShutter.png", new Color(0.95f, 0.96f, 0.97f));
            Material pharmacyInteriorMat = CreateOrGetMat("Assets/Settings/West_FarmalivioInterior.mat", new Color(0.82f, 0.88f, 0.87f), 0.16f, 0.0f);
            Material farmalivioWallQuoteMat = CreateOrGetTexMat("Assets/Settings/West_Farmalivio_WallQuote.mat", "Assets/Textures/Environment/T_West_Farmalivio_WallQuote.png");
            Material opticaMgrLentesMat = CreateOrGetAlphaDecalMat("Assets/Settings/West_OpticaMGR_WindowLentes.mat", "Assets/Textures/Environment/T_West_OpticaMGR_WindowLentes.png");
            Material liveLedBaseMat = LoadOrFallbackMat("Assets/Settings/West_CambiosSantaFe_LiveLed.mat", new Color(0.10f, 0.10f, 0.10f), 0.18f);
            Material gasYellowMat = CreateOrGetMat("Assets/Settings/West_Azucenas_GasYellow.mat", new Color(0.92f, 0.68f, 0.08f), 0.30f, 0.10f);
            Material signOpticaMgrMat = CreateOrGetTexMat("Assets/Settings/West_Sign_OpticaMGR.mat", "Assets/Textures/Environment/T_West_Sign_OpticaMGR.png");
            Material signCambiosBonanzaMat = CreateOrGetTexMat("Assets/Settings/West_Sign_CambiosBonanza.mat", "Assets/Textures/Environment/T_West_Sign_CambiosBonanza.png");
            Material signGrupoBillemexMat = CreateOrGetTexMat("Assets/Settings/West_Sign_GrupoBillemex.mat", "Assets/Textures/Environment/T_West_Sign_GrupoBillemex.png");

            Transform envParent = GameObject.Find("Environment")?.transform;
            GameObject root = new GameObject(RootName);
            if (envParent != null) root.transform.SetParent(envParent);
            root.transform.position = Vector3.zero;

            // Bezier corner arcs centered around (-5.60, -26.11) R=3.0 and (-6.10, -40.21) R=3.5
            List<Vector2> nwArc = SampleBezierChain(
                new Vector2(JuarezWestCurbFaceX, SwWestCutZ), Vector2.down,
                new Vector2(JuarezWestCurbFaceX - NwFilletRadius, AzNorthAtJuarez), Vector2.left,
                0.55f * NwFilletRadius, 16);

            List<Vector2> swArc = SampleBezierChain(
                new Vector2(JuarezWestCurbFaceX, SwSouthCutZ), Vector2.up,
                new Vector2(JuarezWestCurbFaceX - SwFilletRadius, AzSouthAtJuarez), Vector2.left,
                0.55f * SwFilletRadius, 16);

            // 1. Calle Azucenas West Asphalt & Curved Corner Aprons
            BuildAzucenasWestRoadway(root.transform, nwArc, swArc, asphaltMat, whiteStripeMat);

            // 2. NW Corner Rounded Curb, Sidewalk Slab, Pedestrian Ramp & Bollards
            BuildNorthWestCornerSidewalk(root.transform, nwArc, curbMat, concreteMat, tactileMat, bollardBlackMat, bollardReflectiveMat);

            // 3. NW Corner 1-Story 6-Shop Commercial Block (Z = -26.80m to -13.55m, symmetric with East corner at -26.80m!)
            BuildNorthWestCornerBuilding(root.transform, creamStuccoMat, weatheredCreamMat, terracottaMat,
                darkCorniceMat, darkZocaloMat, stoneZocaloMat, beigeAwningMat, awningValanceMat, darkGlassMat,
                clearGlassMat, aluminumMat, shutterMat, whiteShutterMat, whiteFrameMat, blackFrameMat,
                interiorDarkMat, pharmacyInteriorMat, counterGreyMat, gasYellowMat, cfeDomeGlassMat,
                cfeMeterFaceMat, farmalivioWallQuoteMat, opticaMgrLentesMat, liveLedBaseMat,
                signOpticaMgrMat, signCambiosBonanzaMat, signGrupoBillemexMat);

            // 4. SW Counterpart Curb, Sidewalk, Ramp, Tactile Path, Bollards & Lamppost
            BuildSouthWestCornerCounterpart(root.transform, envParent, swArc, curbMat, concreteMat,
                flagstoneMat, tactileMat, bollardBlackMat, bollardReflectiveMat, lampPoleMat);

            Debug.Log("[WestAzucenasCornerBuilder] Built West Azucenas NW & SW corners cleanly.");
        }

        [MenuItem("MyVirtualCommunity/Capture West Azucenas Corner Screenshots")]
        public static void CaptureVerificationScreenshots()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            const string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            var camGo = new GameObject("Temp_WestAzucenasVerifyCam");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;

            try
            {
                // Shot 1: Reference photo camera angle (warm up URP batchmode buffers first so frame 1 colors settle)
                cam.fieldOfView = 52f;
                cam.transform.position = new Vector3(-7.6f, 1.75f, -43.5f);
                cam.transform.LookAt(new Vector3(-1.5f, 2.35f, -23.5f));
                WarmupCamera(cam);
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_azucenas_ref_angle.png"));

                // Shot 2: NW Corner Close-Up (curb curve, ramp, bollards, south building awning)
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(0.5f, 2.25f, -33.2f);
                cam.transform.LookAt(new Vector3(-8.5f, 1.0f, -26.2f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_azucenas_nw_closeup.png"));

                // Shot 3: Top-Down Intersection Overview (both NW & SW curbs, Azucenas West, & East junction)
                cam.fieldOfView = 54f;
                cam.transform.position = new Vector3(-4.0f, 34.0f, -32.0f);
                cam.transform.LookAt(new Vector3(-4.0f, 0.0f, -31.8f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_azucenas_topdown.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        [MenuItem("MyVirtualCommunity/Verify West Azucenas Corner")]
        public static void VerifyWestAzucenasCorner()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestAzucenasVerify] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int passed = 0;
            int total = 0;
            void Check(string label, bool ok)
            {
                total++;
                if (ok)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            GameObject swWest = GameObject.Find("Sidewalk_West");
            GameObject curbWest = GameObject.Find("Curb_West");
            Check("Sidewalk_West trimmed cleanly at Azucenas NW cut (-26.11m)",
                swWest != null && swWest.TryGetComponent<Renderer>(out Renderer swR) &&
                Mathf.Abs(swR.bounds.min.z - SwWestCutZ) < 0.05f);
            Check("Curb_West trimmed cleanly at Azucenas NW cut (-26.11m)",
                curbWest != null && curbWest.TryGetComponent<Renderer>(out Renderer cR) &&
                Mathf.Abs(cR.bounds.min.z - SwWestCutZ) < 0.05f);

            GameObject westCol = GameObject.Find("Column_West_Lattice");
            Check("AvJuarez_DigitalArch West column remains grounded north of NW corner cut",
                westCol != null && westCol.transform.position.z > SwWestCutZ + 2.0f);

            Check("West_Azucenas_Corner_Root exists", GameObject.Find(RootName) != null);
            Check("Calle Azucenas West roadway & curved NW apron exist",
                GameObject.Find("Roadway_Calle_Azucenas_West") != null &&
                GameObject.Find("Roadway_Azucenas_West_NW_Apron") != null &&
                GameObject.Find("Roadway_Azucenas_West_SW_Apron") != null);
            Check("NW & SW rounded curbs, sidewalks, and pedestrian ramps exist",
                GameObject.Find("Azucenas_West_NW_Curb_Arc") != null &&
                GameObject.Find("Sidewalk_Azucenas_West_NW_Corner") != null &&
                GameObject.Find("Azucenas_West_NW_Ramp") != null &&
                GameObject.Find("Azucenas_West_SW_Curb_Arc") != null &&
                GameObject.Find("Sidewalk_West_SouthOfAzucenas") != null &&
                GameObject.Find("Azucenas_West_SW_Ramp") != null);
            Check("NW Corner Annex Building, south beige awning & gas riser exist",
                GameObject.Find("NWCorner_AnnexBuilding") != null &&
                GameObject.Find("NWAnnex_South_BeigeAwning") != null &&
                GameObject.Find("NWAnnex_Corner_GasPipe") != null);
            Check("Corner bollards & SW tactile strip exist",
                GameObject.Find("Bollard_NW_Corner_Apex") != null &&
                GameObject.Find("Bollard_SW_Corner_Apex") != null &&
                GameObject.Find("Sidewalk_West_SW_TactileGuideStrip") != null);
            Check("Expanded 1-story 6-shop SW block built cleanly (Óptica MGR, Farmalivio, Cambios Bonanza, Grupo Billemex, La Esquina de Chihuas & México Mágico)",
                GameObject.Find("Sign_OpticaMGR") != null &&
                GameObject.Find("Sign_Farmalivio_3DLogo") != null &&
                GameObject.Find("Sign_CambiosBonanza") != null &&
                GameObject.Find("Sign_GrupoBillemex") != null &&
                GameObject.Find("Sign_EsqChihuas_Wall") != null &&
                GameObject.Find("Sign_EsqChihuas_RooftopMarquee") != null &&
                GameObject.Find("Sign_MexicoMagico_CircleSign") != null &&
                GameObject.Find("Sign_MexicoMagico_Banner") != null);

            var farmalivioCtrl = GameObject.Find("Sign_Farmalivio_3DLogo")?.GetComponent<IlluminatedSignGlowController>();
            Check("Farmalivio 3D extruded channel-letter sign has solid 3D returns, HDR emissive face & night glow light",
                farmalivioCtrl != null &&
                GameObject.Find("Farmalivio_3DReturns") != null &&
                GameObject.Find("Farmalivio_FrontFace") != null &&
                farmalivioCtrl.baseEmissiveMaterial != null &&
                farmalivioCtrl.baseEmissiveMaterial.IsKeywordEnabled("_EMISSION") &&
                farmalivioCtrl.nightGlowLights != null &&
                farmalivioCtrl.nightGlowLights.Length == 1 &&
                farmalivioCtrl.nightGlowLights[0] != null);

            Check("Authentic 4-shop facades built (Óptica MGR black door & window, Farmalivio aluminum double doors & 3-line wall, Bonanza closed white shutter, Billemex white door + long teller window + twin CFE meters)",
                GameObject.Find("NWAnnex_OpticaMGR_WindowLentes") != null &&
                GameObject.Find("NWAnnex_OpticaMGR_BlackDoorGlass") != null &&
                GameObject.Find("NWAnnex_Farmalivio_DoubleDoorGlass") != null &&
                GameObject.Find("NWAnnex_Farmalivio_WallQuoteQuad") != null &&
                GameObject.Find("NWAnnex_Bonanza_ClosedWhiteShutter") != null &&
                GameObject.Find("CFE_Meter_Billemex_1") != null &&
                GameObject.Find("CFE_Meter_Billemex_2") != null &&
                GameObject.Find("NWAnnex_Billemex_WhiteSecurityDoor") != null &&
                GameObject.Find("NWAnnex_Billemex_CashTray") != null);

            var billemexLed = GameObject.Find("GrupoBillemex_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            Check("Grupo Billemex double-sided simultaneous COMPRA/VENTA LED sign wired to live API with HDR emission & night glow",
                billemexLed != null &&
                billemexLed.layoutMode == ExchangeLedLayoutMode.DualSimultaneousGreenRed &&
                billemexLed.baseLedMaterial != null &&
                billemexLed.baseLedMaterial.IsKeywordEnabled("_EMISSION") &&
                billemexLed.glowLights != null &&
                billemexLed.glowLights.Length == 2);

            var chihuasMarquee = GameObject.Find("Sign_EsqChihuas_RooftopMarquee")?.GetComponent<IlluminatedSignGlowController>();
            var mexicoCircle = GameObject.Find("Sign_MexicoMagico_CircleSign")?.GetComponent<IlluminatedSignGlowController>();
            var mexicoBanner = GameObject.Find("Sign_MexicoMagico_Banner")?.GetComponent<IlluminatedSignGlowController>();
            Check("La Esquina de Chihuas rooftop marquee & backlit wall sign + México Mágico protruding rosa mexicano portal & cutout circular lightbox wired cleanly",
                chihuasMarquee != null &&
                chihuasMarquee.baseEmissiveMaterial != null &&
                chihuasMarquee.baseEmissiveMaterial.IsKeywordEnabled("_EMISSION") &&
                mexicoCircle != null &&
                mexicoCircle.baseEmissiveMaterial != null &&
                mexicoCircle.baseEmissiveMaterial.IsKeywordEnabled("_EMISSION") &&
                mexicoBanner != null &&
                GameObject.Find("MexicoMagico_RosaSideWall") != null &&
                GameObject.Find("Sign_MexicoMagico_ArrowBox") != null);

            Debug.Log($"[WestAzucenasVerify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        private static void CleanupPreviousObjects()
        {
            var killNames = new HashSet<string>
            {
                RootName,
                "Sidewalk_West_SouthOfAzucenas",
                "Curb_West_SouthOfAzucenas"
            };
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                if (tr != null && tr.gameObject.scene.IsValid() && killNames.Contains(tr.name))
                {
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
                }
            }
        }

        private static void TrimNorthWestSidewalkAndCurb()
        {
            const float northEdgeZ = 20.20f;
            float segLen = northEdgeZ - SwWestCutZ;          // 46.31f
            float centerZ = 0.5f * (northEdgeZ + SwWestCutZ); // -2.955f

            GameObject swWest = GameObject.Find("Sidewalk_West");
            if (swWest != null)
            {
                swWest.transform.position = new Vector3(-7.40f, 0.10f, centerZ);
                swWest.transform.localScale = new Vector3(9.20f, 0.20f, segLen);
            }

            GameObject curbWest = GameObject.Find("Curb_West");
            if (curbWest != null)
            {
                curbWest.transform.position = new Vector3(-2.70f, 0.10f, centerZ);
                curbWest.transform.localScale = new Vector3(0.20f, 0.20f, segLen);
            }
        }

        private static void BuildAzucenasWestRoadway(Transform root, List<Vector2> nwArc, List<Vector2> swArc,
            Material asphaltMat, Material whiteStripeMat)
        {
            float topY = RoadTopY - 0.0015f;

            // Main Calle Azucenas West corridor
            List<Vector2> roadPoly = new List<Vector2>
            {
                new Vector2(WestExtentX, AzSouthAtJuarez),
                new Vector2(JuarezWestCurbFaceX, AzSouthAtJuarez),
                new Vector2(JuarezWestCurbFaceX, AzNorthAtJuarez),
                new Vector2(WestExtentX, AzNorthAtJuarez)
            };
            CreateFlatPrism("Roadway_Calle_Azucenas_West", root, roadPoly, RoadBotY, topY, asphaltMat, addCollider: true);

            // NW & SW Asphalt Aprons filling between the straight curb tangents and curved curbs
            List<Vector2> nwApron = new List<Vector2> { new Vector2(JuarezWestCurbFaceX, AzNorthAtJuarez) };
            nwApron.AddRange(nwArc);
            CreateFlatPrism("Roadway_Azucenas_West_NW_Apron", root, nwApron, RoadBotY, topY, asphaltMat, addCollider: true);

            List<Vector2> swApron = new List<Vector2> { new Vector2(JuarezWestCurbFaceX, AzSouthAtJuarez) };
            swApron.AddRange(swArc);
            CreateFlatPrism("Roadway_Azucenas_West_SW_Apron", root, swApron, RoadBotY, topY, asphaltMat, addCollider: true);

            // Crosswalk & lane markings across Calle Azucenas West mouth
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            // Pedestrian crosswalk bars linking SW & NW ramps across Azucenas (X = -8.25..-6.35, Z = -36.2..-29.6)
            for (float z = AzSouthAtJuarez + 0.65f; z <= AzNorthAtJuarez - 0.65f; z += 0.90f)
            {
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(-8.25f, z - 0.20f),
                    new Vector2(-6.35f, z - 0.20f),
                    new Vector2(-6.35f, z + 0.20f),
                    new Vector2(-8.25f, z + 0.20f),
                    MarkingsY);
            }

            // Stop line for Eastbound Calle Azucenas traffic before crosswalk
            AddQuadXZ(verts, tris, uvs,
                new Vector2(-9.60f, AzSouthAtJuarez + 0.25f),
                new Vector2(-9.25f, AzSouthAtJuarez + 0.25f),
                new Vector2(-9.25f, 0.5f * (AzSouthAtJuarez + AzNorthAtJuarez)),
                new Vector2(-9.60f, 0.5f * (AzSouthAtJuarez + AzNorthAtJuarez)),
                MarkingsY);

            // Center dashed divider west of crosswalk
            float midZ = 0.5f * (AzNorthAtJuarez + AzSouthAtJuarez);
            for (float x = -11.5f; x > WestExtentX + 2.0f; x -= 5.0f)
            {
                float xStart = Mathf.Max(x - 2.4f, WestExtentX + 1.0f);
                AddQuadXZ(verts, tris, uvs,
                    new Vector2(xStart, midZ - 0.06f),
                    new Vector2(x, midZ - 0.06f),
                    new Vector2(x, midZ + 0.06f),
                    new Vector2(xStart, midZ + 0.06f),
                    MarkingsY);
            }

            CreateMeshObject("Roadway_Calle_Azucenas_West_Markings", root, verts, tris, uvs, whiteStripeMat);
        }

        private static void BuildNorthWestCornerSidewalk(Transform root, List<Vector2> nwArc,
            Material curbMat, Material concreteMat, Material tactileMat,
            Material bollardBlackMat, Material bollardReflectiveMat)
        {
            // Curb segment 1: R=3.0m NW curve from (-2.60, -26.11) to (-5.60, -29.11), then short straight to ramp East cheek (-5.90)
            var nwCurvePlusShort = new List<Vector2>(nwArc);
            nwCurvePlusShort.Add(new Vector2(RampEastX + RampWingWidth, AzNorthAtJuarez));
            BuildCurbChain("Azucenas_West_NW_Curb_Arc", root, nwCurvePlusShort, outwardIsLeft: false, CurbWidth, SidewalkTopY + 0.004f, curbMat);

            // Curb segment 2: West of NW pedestrian ramp (-8.70 down to -38.00)
            var nwWestCurb = new List<Vector2>
            {
                new Vector2(RampWestX - RampWingWidth, AzNorthAtJuarez),
                new Vector2(-38.00f, AzNorthAtJuarez)
            };
            BuildCurbChain("Azucenas_West_NW_Curb_Straight", root, nwWestCurb, outwardIsLeft: false, CurbWidth, SidewalkTopY + 0.004f, curbMat);

            // Pedestrian Curb Ramp (X = -8.40 .. -6.20, sloping north into AzNorthAtJuarez + CurbWidth)
            BuildPedestrianRamp("Azucenas_West_NW_Ramp", root, RampWestX, RampEastX,
                apronEdgeZ: AzNorthAtJuarez, slopeDirectionZ: +1f, concreteMat, curbMat, tactileMat);

            // Sidewalk slab polygon filling behind inner curb up to Sidewalk_West (Z=-26.11) and Annex South Wall (Z=-24.80)
            // Compute inner curb points (offset 0.20m inward along RIGHT normal of nwArc)
            List<Vector2> innerArc = OffsetChainRight(nwArc, CurbWidth);
            float innerAzZ = AzNorthAtJuarez + CurbWidth; // -28.91f
            float rampBackZ = innerAzZ + 1.35f;           // -27.56f

            var nwSwPoly = new List<Vector2>
            {
                new Vector2(-38.00f, innerAzZ),
                new Vector2(-38.00f, -24.80f),
                new Vector2(-12.00f, -24.80f),
                new Vector2(-12.00f, SwWestCutZ),
                new Vector2(JuarezWestCurbFaceX - CurbWidth, SwWestCutZ)
            };
            for (int i = 1; i < innerArc.Count; i++)
            {
                nwSwPoly.Add(innerArc[i]);
            }
            nwSwPoly.Add(new Vector2(RampEastX + RampWingWidth, innerAzZ));
            nwSwPoly.Add(new Vector2(RampEastX, rampBackZ));
            nwSwPoly.Add(new Vector2(RampWestX, rampBackZ));
            nwSwPoly.Add(new Vector2(RampWestX - RampWingWidth, innerAzZ));

            CreateFlatPrism("Sidewalk_Azucenas_West_NW_Corner", root, nwSwPoly, SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // Black Cylindrical Steel Corner Bollards matching reference photo
            CreateBollard("Bollard_NW_Corner_East", root, new Vector3(-3.45f, SidewalkTopY, -26.95f), bollardBlackMat, bollardReflectiveMat);
            CreateBollard("Bollard_NW_Corner_Apex", root, new Vector3(-5.30f, SidewalkTopY, -28.35f), bollardBlackMat, bollardReflectiveMat);
            CreateBollard("Bollard_NW_Ramp_West", root, new Vector3(-9.05f, SidewalkTopY, -28.55f), bollardBlackMat, bollardReflectiveMat);
        }

        private static void BuildNorthWestCornerBuilding(Transform root, Material creamStuccoMat, Material weatheredCreamMat,
            Material terracottaMat, Material darkCorniceMat, Material darkZocaloMat, Material stoneZocaloMat,
            Material beigeAwningMat, Material awningValanceMat, Material darkGlassMat, Material clearGlassMat,
            Material aluminumMat, Material shutterMat, Material whiteShutterMat, Material whiteFrameMat,
            Material blackFrameMat, Material interiorDarkMat, Material pharmacyInteriorMat, Material counterGreyMat,
            Material gasYellowMat, Material cfeDomeGlassMat, Material cfeMeterFaceMat, Material farmalivioWallQuoteMat,
            Material opticaMgrLentesMat, Material liveLedBaseMat, Material signOpticaMgrMat,
            Material signCambiosBonanzaMat, Material signGrupoBillemexMat)
        {
            var bldgRoot = new GameObject("NWCorner_AnnexBuilding");
            bldgRoot.transform.SetParent(root);
            bldgRoot.transform.position = Vector3.zero;

            // Main 1-story white stucco corner commercial block:
            //   - Straight South section: X = -26.00..-12.00m, Z = -26.80..-16.35m (Length = 10.45m, Center Z = -21.575m)
            //   - Angled North section ("La Esquina de Chihuas" & "México Mágico"): Z = -16.35..-13.45m, receding diagonally
            //     from (-12.00, -16.35) to (-14.60, -13.45) to meet Building A's 2-story diagonal chamfer at the exact shared
            //     junction vertex (-14.60, -13.45) and party wall Z = -13.45m (100% joined / "pegados" with zero gap!).
            //   - Roof parapet height Y = 3.85m (matching Building A's first-floor stringcourse molding in real photo!)
            CreateBox("NWAnnex_MainVolume", bldgRoot.transform,
                new Vector3(-20.30f, 1.92f, -20.125f), new Vector3(11.40f, 3.84f, 13.35f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_SouthStraightBacking", bldgRoot.transform,
                new Vector3(-13.85f, 1.19f, -21.575f), new Vector3(1.50f, 2.38f, 10.45f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_MainVolume_Upper", bldgRoot.transform,
                new Vector3(-19.00f, 2.94f, -21.575f), new Vector3(14.00f, 1.12f, 10.45f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_GroundFloorPad", bldgRoot.transform,
                new Vector3(-12.56f, 0.10f, -21.575f), new Vector3(1.16f, 0.20f, 10.45f), weatheredCreamMat, addCollider: true);
            CreateBox("NWAnnex_GroundCeilingSoffit", bldgRoot.transform,
                new Vector3(-12.56f, 2.38f, -21.575f), new Vector3(1.16f, 0.08f, 10.45f), whiteFrameMat, addCollider: false);

            // Upper parapet band over South 4 shops (Z = -26.80..-16.35m, Length = 10.45m, Center Z = -21.575m, Y = 3.50..3.84m)
            CreateBox("NWAnnex_UpperParapet_Flat", bldgRoot.transform,
                new Vector3(-19.00f, 3.67f, -21.575f), new Vector3(14.00f, 0.34f, 10.45f), creamStuccoMat, addCollider: false);

            // Dark grey-brown parapet coping wrapping South & East straight section (Y = 3.85m, aligned with Building A stringcourse!)
            CreateBox("NWAnnex_Cornice_East", bldgRoot.transform,
                new Vector3(-11.93f, 3.86f, -21.575f), new Vector3(0.22f, 0.14f, 10.50f), darkCorniceMat, addCollider: false);

            // Terracotta / warm-brown parapet cornice along South face facing Calle Azucenas (Z = -26.80m)
            CreateBox("NWAnnex_Cornice_South", bldgRoot.transform,
                new Vector3(-18.96f, 3.86f, -26.87f), new Vector3(14.16f, 0.18f, 0.22f), terracottaMat, addCollider: false);

            // Subtle horizontal architectural molding band separating signage fascia from awnings along East facade (Y = 2.80m)
            CreateBox("NWAnnex_East_MoldingBand", bldgRoot.transform,
                new Vector3(-11.95f, 2.80f, -21.575f), new Vector3(0.10f, 0.07f, 10.46f), darkCorniceMat, addCollider: false);

            // Lower Dark Zocalo along South wall (Z = -26.80m)
            CreateBox("NWAnnex_Zocalo_South", bldgRoot.transform,
                new Vector3(-19.00f, 0.34f, -26.84f), new Vector3(14.02f, 0.28f, 0.08f), darkZocaloMat, addCollider: false);

            // --- South Facade Facing Calle Azucenas (Z = -26.80m, symmetric with East corner!) ---
            CreateBox("NWAnnex_South_WindowGlass", bldgRoot.transform,
                new Vector3(-18.00f, 1.65f, -26.83f), new Vector3(5.20f, 1.05f, 0.08f), darkGlassMat, addCollider: false);
            float[] winMullionX = { -20.58f, -18.85f, -17.15f, -15.42f };
            for (int i = 0; i < winMullionX.Length; i++)
            {
                CreateBox($"NWAnnex_South_Mullion_{i}", bldgRoot.transform,
                    new Vector3(winMullionX[i], 1.65f, -26.85f), new Vector3(0.07f, 1.08f, 0.08f), aluminumMat, addCollider: false);
            }
            CreateBox("NWAnnex_South_WindowSill", bldgRoot.transform,
                new Vector3(-18.00f, 1.10f, -26.86f), new Vector3(5.34f, 0.08f, 0.12f), aluminumMat, addCollider: false);

            // Beige Sloped Wedge Awning projecting South (-Z) over Azucenas North sidewalk
            CreateSouthWedgeAwning("NWAnnex_South_BeigeAwning", bldgRoot.transform,
                centerX: -18.00f, wallZ: -26.82f, topY: 2.74f, widthX: 5.50f, projectionZ: 1.15f, dropY: 0.52f,
                beigeAwningMat, awningValanceMat);

            // Corner shutter door near SE corner of South wall
            CreateBox("NWAnnex_South_CornerShutter", bldgRoot.transform,
                new Vector3(-13.80f, 1.22f, -26.83f), new Vector3(1.55f, 2.02f, 0.08f), shutterMat, addCollider: false);

            // Yellow corner gas pipe / meter riser on SE corner (signature detail in photo)
            CreateBox("NWAnnex_Corner_GasPipe", bldgRoot.transform,
                new Vector3(-12.06f, 1.05f, -26.88f), new Vector3(0.07f, 1.65f, 0.07f), gasYellowMat, addCollider: false);
            CreateBox("NWAnnex_Corner_GasBox", bldgRoot.transform,
                new Vector3(-12.22f, 1.45f, -26.88f), new Vector3(0.28f, 0.34f, 0.14f), gasYellowMat, addCollider: false);

            // =========================================================================
            // EAST FACADE FACING AV. BENITO JUÁREZ (Straight section Z = -26.80..-16.35m, Angled section Z = -16.35..-13.70m)
            // Harmonized door height Y = 0.20..2.24m (2.04m tall) and cornice height Y = 3.85m matching Building A stringcourse!
            // =========================================================================

            // --- SOUTH CORNER PIER (Z = -26.80..-26.25m, 0.55m clean stucco margin + stone zócalo!) ---
            CreateBox("NWAnnex_SouthCorner_PierWall", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -26.525f), new Vector3(1.16f, 2.18f, 0.56f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_SouthCorner_StoneZocalo", bldgRoot.transform,
                new Vector3(-11.98f, 0.56f, -26.525f), new Vector3(0.14f, 0.76f, 0.58f), stoneZocaloMat, addCollider: false);

            // --- SHOP 1 (Z = -26.25..-23.95m, center -25.10m, 2.30m): "Óptica MGR." ---
            CreateBox("NWAnnex_OpticaMGR_InteriorBack", bldgRoot.transform,
                new Vector3(-13.02f, 1.29f, -25.10f), new Vector3(0.10f, 2.18f, 2.28f), weatheredCreamMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DisplayShelf", bldgRoot.transform,
                new Vector3(-12.60f, 0.92f, -25.60f), new Vector3(0.45f, 0.06f, 1.08f), whiteFrameMat, addCollider: false);

            // South half (Z = -26.22..-25.00m): Display window above dark-grey stone zócalo + "LENTES" decal
            CreateBox("NWAnnex_OpticaMGR_WindowZocalo", bldgRoot.transform,
                new Vector3(-12.02f, 0.56f, -25.61f), new Vector3(0.16f, 0.76f, 1.24f), stoneZocaloMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_WindowSill", bldgRoot.transform,
                new Vector3(-11.98f, 0.96f, -25.61f), new Vector3(0.18f, 0.06f, 1.26f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_WindowGlass", bldgRoot.transform,
                new Vector3(-12.00f, 1.60f, -25.61f), new Vector3(0.03f, 1.24f, 1.20f), clearGlassMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_WinFrameS", bldgRoot.transform,
                new Vector3(-11.98f, 1.60f, -26.22f), new Vector3(0.08f, 1.30f, 0.06f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_WinFrameTop", bldgRoot.transform,
                new Vector3(-11.98f, 2.24f, -25.61f), new Vector3(0.08f, 0.06f, 1.26f), blackFrameMat, addCollider: false);
            var lentesQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lentesQuad.name = "NWAnnex_OpticaMGR_WindowLentes";
            lentesQuad.transform.SetParent(bldgRoot.transform);
            lentesQuad.transform.position = new Vector3(-11.96f, 2.02f, -25.62f);
            lentesQuad.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            lentesQuad.transform.localScale = new Vector3(0.86f, 0.24f, 1.0f);
            AssignMat(lentesQuad, opticaMgrLentesMat);
            UnityEngine.Object.DestroyImmediate(lentesQuad.GetComponent<Collider>());

            // North half (Z = -25.00..-24.00m): Black-framed glass Push/Pull entrance door (harmonized top Y = 2.24m!)
            CreateBox("NWAnnex_OpticaMGR_BlackDoorGlass", bldgRoot.transform,
                new Vector3(-12.00f, 1.22f, -24.50f), new Vector3(0.03f, 2.02f, 0.96f), clearGlassMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorJambS", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -24.99f), new Vector3(0.10f, 2.08f, 0.07f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorJambN", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -24.01f), new Vector3(0.10f, 2.08f, 0.07f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorHeader", bldgRoot.transform,
                new Vector3(-11.97f, 2.24f, -24.50f), new Vector3(0.10f, 0.07f, 1.04f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorMidRail", bldgRoot.transform,
                new Vector3(-11.96f, 1.14f, -24.50f), new Vector3(0.07f, 0.05f, 0.94f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorKick", bldgRoot.transform,
                new Vector3(-11.97f, 0.29f, -24.50f), new Vector3(0.08f, 0.18f, 0.96f), blackFrameMat, addCollider: false);
            CreateBox("NWAnnex_OpticaMGR_DoorHandle", bldgRoot.transform,
                new Vector3(-11.91f, 1.16f, -24.16f), new Vector3(0.05f, 0.32f, 0.03f), aluminumMat, addCollider: false);

            CreateEastPlaqueSign("Sign_OpticaMGR", bldgRoot.transform,
                centerZ: -25.10f, centerY: 3.30f, widthZ: 2.08f, heightY: 0.48f, terracottaMat, signOpticaMgrMat);
            CreateEastWedgeAwning("NWAnnex_Shop1_Awning", bldgRoot.transform,
                centerZ: -25.10f, wallX: -11.98f, topY: 2.72f, widthZ: 2.24f, projectionX: 1.12f, dropY: 0.52f,
                beigeAwningMat, awningValanceMat);

            // Narrow dividing pier between Óptica MGR. & Farmalivio (Z = -23.95m)
            CreateBox("NWAnnex_Pier_1_2", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -23.95f), new Vector3(1.16f, 2.18f, 0.14f), creamStuccoMat, addCollider: false);

            // --- SHOP 2 (Z = -23.95..-21.15m, center -22.55m, 2.80m): "Farmalivio" ---
            CreateBox("NWAnnex_Farmalivio_InteriorBack", bldgRoot.transform,
                new Vector3(-13.02f, 1.29f, -23.05f), new Vector3(0.10f, 2.18f, 1.76f), pharmacyInteriorMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_InteriorShelf1", bldgRoot.transform,
                new Vector3(-12.82f, 1.02f, -23.05f), new Vector3(0.28f, 0.05f, 1.56f), whiteFrameMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_InteriorShelf2", bldgRoot.transform,
                new Vector3(-12.82f, 1.48f, -23.05f), new Vector3(0.28f, 0.05f, 1.56f), whiteFrameMat, addCollider: false);

            // South module (Z = -23.88..-22.22m, 1.66m): Double transparent glass Push/Pull doors (top Y = 2.24m!)
            CreateBox("NWAnnex_Farmalivio_DoubleDoorGlass", bldgRoot.transform,
                new Vector3(-12.00f, 1.22f, -23.05f), new Vector3(0.03f, 2.02f, 1.62f), clearGlassMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_FrameS", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -23.86f), new Vector3(0.10f, 2.08f, 0.07f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_FrameN", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -22.24f), new Vector3(0.10f, 2.08f, 0.07f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_FrameHeader", bldgRoot.transform,
                new Vector3(-11.97f, 2.24f, -23.05f), new Vector3(0.10f, 0.07f, 1.68f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_CenterStileS", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -23.09f), new Vector3(0.08f, 2.02f, 0.05f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_CenterStileN", bldgRoot.transform,
                new Vector3(-11.97f, 1.22f, -23.01f), new Vector3(0.08f, 2.02f, 0.05f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_Kickplate", bldgRoot.transform,
                new Vector3(-11.97f, 0.29f, -23.05f), new Vector3(0.08f, 0.18f, 1.62f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_HandleS", bldgRoot.transform,
                new Vector3(-11.91f, 1.16f, -23.16f), new Vector3(0.05f, 0.34f, 0.03f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Farmalivio_HandleN", bldgRoot.transform,
                new Vector3(-11.91f, 1.16f, -22.94f), new Vector3(0.05f, 0.34f, 0.03f), aluminumMat, addCollider: false);

            // North module (Z = -22.22..-21.15m, 1.07m): Solid white stucco wall painted with 3-line quote "El alivio / empieza / aquí"
            CreateBox("NWAnnex_Farmalivio_QuoteWallBlock", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -21.685f), new Vector3(1.16f, 2.18f, 1.08f), creamStuccoMat, addCollider: true);
            var quoteQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quoteQuad.name = "NWAnnex_Farmalivio_WallQuoteQuad";
            quoteQuad.transform.SetParent(bldgRoot.transform);
            quoteQuad.transform.position = new Vector3(-11.97f, 1.32f, -21.685f);
            quoteQuad.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            quoteQuad.transform.localScale = new Vector3(0.92f, 1.32f, 1.0f);
            AssignMat(quoteQuad, farmalivioWallQuoteMat);
            UnityEngine.Object.DestroyImmediate(quoteQuad.GetComponent<Collider>());

            CreateEastWedgeAwning("NWAnnex_Shop2_Awning", bldgRoot.transform,
                centerZ: -22.55f, wallX: -11.98f, topY: 2.72f, widthZ: 2.74f, projectionX: 1.12f, dropY: 0.52f,
                beigeAwningMat, awningValanceMat);
            CreateExtrudedChannelLetterLogo(
                name: "Sign_Farmalivio_3DLogo",
                parent: bldgRoot.transform,
                wallX: -11.99f,
                centerY: 3.32f,
                centerZ: -22.55f,
                widthZ: 2.36f,
                heightY: 0.58f,
                extrudeDepthX: 0.105f,
                texAssetPath: "Assets/Textures/Environment/T_West_Sign_Farmalivio_Face.png",
                faceMatPath: "Assets/Settings/West_Sign_Farmalivio_Face.mat",
                sideMatPath: "Assets/Settings/West_Sign_Farmalivio_Side.mat",
                meshAssetPath: "Assets/Settings/Farmalivio_3DReturns_Mesh.asset",
                sideReturnColor: new Color(0.03f, 0.33f, 0.28f),
                nightGlowColor: new Color(0.10f, 0.92f, 0.76f, 1.0f));

            // --- SHOP 3 (Z = -21.15..-19.70m, center -20.425m, 1.45m): "Cambios Bonanza" — Permanently Closed White Metal Shutter ---
            CreateBox("NWAnnex_Bonanza_ClosedWhiteShutter", bldgRoot.transform,
                new Vector3(-11.98f, 1.22f, -20.425f), new Vector3(0.08f, 2.04f, 1.34f), whiteShutterMat, addCollider: true);
            CreateBox("NWAnnex_Bonanza_ShutterTrackS", bldgRoot.transform,
                new Vector3(-11.95f, 1.22f, -21.10f), new Vector3(0.08f, 2.08f, 0.06f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Bonanza_ShutterTrackN", bldgRoot.transform,
                new Vector3(-11.95f, 1.22f, -19.75f), new Vector3(0.08f, 2.08f, 0.06f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Bonanza_ShutterBox", bldgRoot.transform,
                new Vector3(-11.95f, 2.32f, -20.425f), new Vector3(0.18f, 0.18f, 1.44f), whiteFrameMat, addCollider: false);
            CreateEastPlaqueSign("Sign_CambiosBonanza", bldgRoot.transform,
                centerZ: -20.425f, centerY: 3.30f, widthZ: 1.32f, heightY: 0.48f, terracottaMat, signCambiosBonanzaMat);
            CreateEastWedgeAwning("NWAnnex_Shop3_Awning", bldgRoot.transform,
                centerZ: -20.425f, wallX: -11.98f, topY: 2.72f, widthZ: 1.40f, projectionX: 1.10f, dropY: 0.50f,
                beigeAwningMat, awningValanceMat);

            // --- WIDE DIVIDING PILASTER BETWEEN CAMBIOS BONANZA & GRUPO BILLEMEX (Z = -19.70..-19.05m, 0.65m) ---
            CreateBox("NWAnnex_Pier_Bonanza_Billemex", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -19.375f), new Vector3(1.18f, 2.18f, 0.66f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_Pier_Bonanza_Billemex_Zocalo", bldgRoot.transform,
                new Vector3(-11.96f, 0.58f, -19.375f), new Vector3(0.14f, 0.80f, 0.68f), stoneZocaloMat, addCollider: false);
            CreateCfeMeter("CFE_Meter_Billemex_1", bldgRoot.transform, new Vector3(-11.94f, 1.36f, -19.50f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);
            CreateCfeMeter("CFE_Meter_Billemex_2", bldgRoot.transform, new Vector3(-11.94f, 1.36f, -19.24f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);

            // --- SHOP 4 (Z = -19.05..-16.65m, center -17.85m, 2.40m): "Grupo Billemex" ---
            CreateBox("NWAnnex_Billemex_DoorFrameWall", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -18.60f), new Vector3(1.14f, 2.18f, 0.86f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_Billemex_WhiteSecurityDoor", bldgRoot.transform,
                new Vector3(-11.97f, 1.20f, -18.60f), new Vector3(0.08f, 1.98f, 0.78f), whiteFrameMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_DoorPerimeter", bldgRoot.transform,
                new Vector3(-11.99f, 1.20f, -18.60f), new Vector3(0.06f, 2.04f, 0.84f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_DoorHandle", bldgRoot.transform,
                new Vector3(-11.92f, 1.12f, -18.29f), new Vector3(0.04f, 0.16f, 0.04f), darkZocaloMat, addCollider: false);

            // Right side (Z = -18.18..-16.68m, 1.50m): Long reinforced bulletproof glass teller window + cash tray + stone base!
            CreateBox("NWAnnex_Billemex_BoothInterior", bldgRoot.transform,
                new Vector3(-12.88f, 1.52f, -17.43f), new Vector3(0.10f, 1.68f, 1.48f), weatheredCreamMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_BaseStoneWall", bldgRoot.transform,
                new Vector3(-12.22f, 0.62f, -17.43f), new Vector3(0.56f, 0.88f, 1.52f), stoneZocaloMat, addCollider: true);
            CreateBox("NWAnnex_Billemex_CashTray", bldgRoot.transform,
                new Vector3(-11.94f, 1.08f, -17.43f), new Vector3(0.28f, 0.06f, 1.52f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_CashSlot", bldgRoot.transform,
                new Vector3(-11.96f, 1.16f, -17.43f), new Vector3(0.16f, 0.10f, 0.56f), interiorDarkMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_BulletproofGlass", bldgRoot.transform,
                new Vector3(-12.00f, 1.66f, -17.43f), new Vector3(0.04f, 1.12f, 1.44f), clearGlassMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_WinFrameS", bldgRoot.transform,
                new Vector3(-11.97f, 1.66f, -18.15f), new Vector3(0.09f, 1.18f, 0.06f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_WinFrameN", bldgRoot.transform,
                new Vector3(-11.97f, 1.66f, -16.71f), new Vector3(0.09f, 1.18f, 0.06f), aluminumMat, addCollider: false);
            CreateBox("NWAnnex_Billemex_WinFrameTop", bldgRoot.transform,
                new Vector3(-11.97f, 2.24f, -17.43f), new Vector3(0.09f, 0.07f, 1.50f), aluminumMat, addCollider: false);

            CreateEastPlaqueSign("Sign_GrupoBillemex", bldgRoot.transform,
                centerZ: -17.85f, centerY: 3.30f, widthZ: 2.18f, heightY: 0.48f, terracottaMat, signGrupoBillemexMat);
            CreateEastWedgeAwning("NWAnnex_Shop4_Awning", bldgRoot.transform,
                centerZ: -17.85f, wallX: -11.98f, topY: 2.74f, widthZ: 2.32f, projectionX: 1.12f, dropY: 0.54f,
                beigeAwningMat, awningValanceMat);

            CreateDualSimultaneousExchangeLedSign("GrupoBillemex_ExchangeLedSign", bldgRoot.transform,
                pos: new Vector3(-10.88f, 3.04f, -19.22f), frameMat: aluminumMat, bezelMat: blackFrameMat,
                armMat: whiteFrameMat, liveLedBaseMat: liveLedBaseMat);

            // Corner Vertex Pier between Grupo Billemex & La Esquina de Chihuas (Z = -16.65..-16.35m)
            CreateBox("NWAnnex_Pier_4_5", bldgRoot.transform,
                new Vector3(-12.56f, 1.29f, -16.50f), new Vector3(1.18f, 2.18f, 0.32f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_Pier_4_5_Zocalo", bldgRoot.transform,
                new Vector3(-11.96f, 0.58f, -16.50f), new Vector3(0.14f, 0.80f, 0.34f), stoneZocaloMat, addCollider: false);

            // =========================================================================
            // UPPER ORIGINAL DIAGONAL PARAPET vs. GROUND-FLOOR ALIGNED FRONTS (Shops 5 & 6)
            //   - Upper original parapet & cornice (Y = 2.96..3.86m) recede diagonally
            //     from (-12.00, -16.35) to (-14.60, -13.45), meeting Building A's 2-story diagonal chamfer
            //     and lateral party wall at the exact shared junction vertex (-14.60, -13.45) with zero gap!
            //   - Ground-floor storefronts of "La Esquina de Chihuas" (Z = -16.35..-15.05m) and
            //     "México Mágico · Café Express-O" (Z = -15.05..-13.45m) are ALIGNED AT THE FRONT (X = -12.00m)
            //     via false front walls, with México Mágico's signature protruding fuchsia/rosa mexicano
            //     left & right side wing walls (salientes laterales out to X = -11.48m)!
            // =========================================================================
            Vector2 bendStart = new Vector2(-12.00f, -16.35f);
            Vector2 bendEnd = new Vector2(-14.60f, -13.45f);
            Vector2 bendDelta = bendEnd - bendStart;
            float bendLen = bendDelta.magnitude; // 3.895m
            Vector2 bendDir = bendDelta / bendLen;

            // Solid triangular upper building prism filling behind the diagonal parapet up to the shared party wall Z = -13.45m
            CreateFlatPrism("NWAnnex_NorthDiagonalUpperPrism", bldgRoot.transform,
                new List<Vector2>
                {
                    new Vector2(-14.65f, -16.38f),
                    new Vector2(-12.00f, -16.35f),
                    new Vector2(-14.60f, -13.45f)
                },
                2.88f, 3.84f, creamStuccoMat, addCollider: true);

            GameObject angledWingRoot = new GameObject("NWAnnex_NorthAngledWing_Root");
            angledWingRoot.transform.SetParent(bldgRoot.transform, false);
            angledWingRoot.transform.localPosition = new Vector3(bendStart.x, 0f, bendStart.y);
            angledWingRoot.transform.localRotation = Quaternion.LookRotation(new Vector3(bendDir.x, 0f, bendDir.y), Vector3.up);

            // Recessed original diagonal upper parapet wall & dark-gray coping cornice (visible above the aligned false fronts!)
            CreateBox("NWAnnex_UpperParapet_Sloped", angledWingRoot.transform,
                new Vector3(-0.32f, 3.38f, bendLen * 0.5f), new Vector3(0.64f, 0.92f, bendLen + 0.08f), creamStuccoMat, addCollider: true);
            CreateBox("NWAnnex_Cornice_EastSloped", angledWingRoot.transform,
                new Vector3(0.06f, 3.86f, bendLen * 0.5f), new Vector3(0.22f, 0.14f, bendLen + 0.12f), darkCorniceMat, addCollider: false);

            // Flat roof cap & ground floor pad bridging the triangular pocket between the aligned X = -12.00m fronts and the diagonal structure
            CreateBox("NWAnnex_AngledGroundFloorPad", bldgRoot.transform,
                new Vector3(-13.30f, 0.10f, -14.90f), new Vector3(2.64f, 0.20f, 2.94f), weatheredCreamMat, addCollider: true);
            CreateBox("NWAnnex_FalseFront_RoofCap", bldgRoot.transform,
                new Vector3(-13.30f, 2.94f, -14.90f), new Vector3(2.60f, 0.10f, 2.92f), creamStuccoMat, addCollider: false);
            CreateBox("NWAnnex_Angled_MoldingBand", bldgRoot.transform,
                new Vector3(-11.95f, 2.94f, -15.70f), new Vector3(0.10f, 0.08f, 1.32f), darkCorniceMat, addCollider: false);

            // --- SHOP 5 (Aligned at X = -12.00m, Z = -16.35..-15.05m, width 1.30m, center -15.70m): "LA ESQUINA DE CHIHUAS · RESTO-BAR" ---
            Material chihuasWallMat = CreateOrGetSeparateEmissiveMat(
                "Assets/Settings/West_Sign_EsqChihuas_Wall.mat",
                "Assets/Textures/Environment/T_West_Sign_EsqChihuas_Wall.png",
                "Assets/Textures/Environment/T_West_Sign_EsqChihuas_Wall_Emissive.png",
                alphaClip: false);
            Material chihuasMarqueeMat = CreateOrGetTexMat(
                "Assets/Settings/West_Sign_EsqChihuas_Marquee.mat",
                "Assets/Textures/Environment/T_West_Sign_EsqChihuas_Marquee.png");
            Material chihuasYellowMat = CreateOrGetMat("Assets/Settings/West_EsqChihuas_MarqueeYellow.mat", new Color(0.96f, 0.72f, 0.12f), 0.35f, 0.10f);
            Material chihuasBulbMat = CreateOrGetSolidEmissiveMat("Assets/Settings/West_EsqChihuas_BulbWarm.mat", new Color(1.0f, 0.94f, 0.78f), new Color(1.0f, 0.78f, 0.32f));
            Material chihuasWarmWoodMat = CreateOrGetMat("Assets/Settings/West_EsqChihuas_InteriorWood.mat", new Color(0.42f, 0.25f, 0.14f), 0.25f, 0.0f);

            CreateBox("EsqChihuas_InteriorBackWall", bldgRoot.transform,
                new Vector3(-13.02f, 1.29f, -15.70f), new Vector3(0.10f, 2.18f, 1.26f), chihuasWarmWoodMat, addCollider: false);
            CreateBox("EsqChihuas_InteriorBarCounter", bldgRoot.transform,
                new Vector3(-12.68f, 0.66f, -15.62f), new Vector3(0.45f, 0.92f, 0.86f), darkZocaloMat, addCollider: false);
            CreateBox("EsqChihuas_InteriorCeilingGlow", bldgRoot.transform,
                new Vector3(-12.50f, 2.34f, -15.70f), new Vector3(0.84f, 0.05f, 0.98f), chihuasBulbMat, addCollider: false);

            // Aligned white false-front portal framing at X = -12.00m
            CreateBox("EsqChihuas_PortalFrame_S", bldgRoot.transform,
                new Vector3(-12.02f, 1.48f, -16.28f), new Vector3(0.14f, 2.56f, 0.14f), creamStuccoMat, addCollider: false);
            CreateBox("EsqChihuas_PortalFrame_N", bldgRoot.transform,
                new Vector3(-12.02f, 1.48f, -15.12f), new Vector3(0.14f, 2.56f, 0.14f), creamStuccoMat, addCollider: false);
            CreateBox("EsqChihuas_UpperFasciaWall", bldgRoot.transform,
                new Vector3(-12.01f, 2.59f, -15.70f), new Vector3(0.10f, 0.66f, 1.30f), creamStuccoMat, addCollider: false);
            CreateBox("EsqChihuas_DoorHeader", bldgRoot.transform,
                new Vector3(-11.98f, 2.24f, -15.70f), new Vector3(0.10f, 0.07f, 1.06f), aluminumMat, addCollider: false);

            CreateBox("EsqChihuas_DoubleGlassDoors", bldgRoot.transform,
                new Vector3(-12.00f, 1.22f, -15.70f), new Vector3(0.03f, 2.02f, 1.02f), clearGlassMat, addCollider: false);
            CreateBox("EsqChihuas_DoorGapSeam", bldgRoot.transform,
                new Vector3(-11.985f, 1.22f, -15.70f), new Vector3(0.02f, 2.00f, 0.015f), darkZocaloMat, addCollider: false);
            CreateBox("EsqChihuas_Handle_S", bldgRoot.transform,
                new Vector3(-11.93f, 1.14f, -15.77f), new Vector3(0.04f, 0.68f, 0.028f), aluminumMat, addCollider: false);
            CreateBox("EsqChihuas_Handle_N", bldgRoot.transform,
                new Vector3(-11.93f, 1.14f, -15.63f), new Vector3(0.04f, 0.68f, 0.028f), aluminumMat, addCollider: false);
            CreateBox("EsqChihuas_DoorSticker", bldgRoot.transform,
                new Vector3(-11.97f, 1.54f, -15.70f), new Vector3(0.01f, 0.22f, 0.18f), whiteFrameMat, addCollider: false);

            var chihuasWallRoot = new GameObject("Sign_EsqChihuas_Wall");
            chihuasWallRoot.transform.SetParent(bldgRoot.transform, false);
            chihuasWallRoot.transform.localPosition = new Vector3(-11.95f, 2.58f, -15.70f);
            var chihuasWallQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            chihuasWallQuad.name = "EsqChihuas_WallFace";
            chihuasWallQuad.transform.SetParent(chihuasWallRoot.transform, false);
            chihuasWallQuad.transform.localPosition = Vector3.zero;
            chihuasWallQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            chihuasWallQuad.transform.localScale = new Vector3(1.14f, 0.50f, 1.0f);
            AssignMat(chihuasWallQuad, chihuasWallMat);
            UnityEngine.Object.DestroyImmediate(chihuasWallQuad.GetComponent<Collider>());

            var chihuasWallLightGo = new GameObject("EsqChihuas_WallGlowLight");
            chihuasWallLightGo.transform.SetParent(chihuasWallRoot.transform, false);
            chihuasWallLightGo.transform.localPosition = new Vector3(0.40f, -0.05f, 0f);
            var chihuasWallLight = chihuasWallLightGo.AddComponent<Light>();
            chihuasWallLight.type = LightType.Point;
            chihuasWallLight.color = new Color(1.0f, 0.76f, 0.28f, 1f);
            chihuasWallLight.intensity = 0.0f;
            chihuasWallLight.range = 3.8f;
            chihuasWallLight.enabled = false;
            chihuasWallLight.shadows = LightShadows.None;
            chihuasWallLight.renderMode = LightRenderMode.Auto;

            var chihuasWallGlow = chihuasWallRoot.AddComponent<IlluminatedSignGlowController>();
            chihuasWallGlow.baseEmissiveMaterial = chihuasWallMat;
            chihuasWallGlow.emissiveRenderers = new Renderer[] { chihuasWallQuad.GetComponent<Renderer>() };
            chihuasWallGlow.nightGlowLights = new Light[] { chihuasWallLight };
            chihuasWallGlow.dayEmissionIntensity = 0.55f;
            chihuasWallGlow.nightEmissionIntensity = 3.6f;
            chihuasWallGlow.nightLightColor = new Color(1.0f, 0.76f, 0.28f, 1f);
            chihuasWallGlow.nightLightIntensity = 1.45f;
            chihuasWallGlow.nightLightRange = 3.8f;

            // Rooftop Double-Sided Vintage Marquee Sign ("LA ESQ DE CH") mounted on the upper structure
            CreateEsqChihuasRooftopMarquee("Sign_EsqChihuas_RooftopMarquee", bldgRoot.transform,
                pos: new Vector3(-11.05f, 4.72f, -15.65f),
                trussMat: blackFrameMat, cabinetMat: terracottaMat, faceMat: chihuasMarqueeMat,
                yellowBorderMat: chihuasYellowMat, bulbMat: chihuasBulbMat);

            // --- SHOP 6 (Aligned at X = -12.00m, Z = -15.05..-13.45m, width 1.60m, center -14.25m): "MÉXICO MÁGICO · CAFÉ EXPRESS-O" ---
            // Features:
            //   1. Storefront glass & black/pink banner aligned at the front (X = -11.96m) with Chihuas & the Mystery Door false wall!
            //   2. Two protruding fuchsia / rosa mexicano side wing walls (salientes laterales out to X = -11.48m on right/North
            //      and X = -11.58m on left/South) rising to Y = 3.02m to frame the entrance, exactly as shown in video Image 1 & 2!
            //   3. Recessed pink openwork lattice screen (celosía rosa) sitting back on the original diagonal roof edge (Y = 3.44m)!
            Material rosaMexicanoMat = CreateOrGetMat("Assets/Settings/West_MexicoMagico_RosaMexicano.mat", new Color(0.84f, 0.06f, 0.44f), 0.52f, 0.05f);
            Material mexicoBannerMat = CreateOrGetSeparateEmissiveMat(
                "Assets/Settings/West_Sign_MexicoMagico_Banner.mat",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Banner.png",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Banner.png",
                alphaClip: false);
            Material mexicoCircleMat = CreateOrGetSeparateEmissiveMat(
                "Assets/Settings/West_Sign_MexicoMagico_Circle.mat",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Circle.png",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Circle_Emissive.png",
                alphaClip: true);
            Material mexicoArrowMat = CreateOrGetSeparateEmissiveMat(
                "Assets/Settings/West_Sign_MexicoMagico_Arrow.mat",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Arrow.png",
                "Assets/Textures/Environment/T_West_Sign_MexicoMagico_Arrow.png",
                alphaClip: false);
            Material blueCoolerMat = CreateOrGetSolidEmissiveMat("Assets/Settings/West_MexicoMagico_BlueCooler.mat", new Color(0.18f, 0.36f, 0.95f), new Color(0.22f, 0.44f, 1.0f));
            Material neonRedCupMat = CreateOrGetSolidEmissiveMat("Assets/Settings/West_MexicoMagico_NeonRed.mat", new Color(1.0f, 0.16f, 0.16f), new Color(1.0f, 0.16f, 0.16f));

            float mmCenterZ = -14.25f;

            // Threshold pad & top black canopy bar between the two protruding fuchsia side wings
            CreateBox("MexicoMagico_ProtrudingFloor", bldgRoot.transform,
                new Vector3(-11.76f, 0.10f, mmCenterZ), new Vector3(0.54f, 0.20f, 1.62f), weatheredCreamMat, addCollider: true);
            CreateBox("MexicoMagico_ProtrudingRoof", bldgRoot.transform,
                new Vector3(-11.74f, 3.04f, mmCenterZ), new Vector3(0.54f, 0.09f, 1.64f), blackFrameMat, addCollider: false);

            // Saliente Izquierda (South protruding fuchsia/rosa mexicano wing wall at Z = -15.04m, X = -12.02..-11.56m, Y = 0.20..3.02m)
            CreateBox("MexicoMagico_SouthReturnWall", bldgRoot.transform,
                new Vector3(-11.79f, 1.61f, -15.04f), new Vector3(0.46f, 2.82f, 0.08f), rosaMexicanoMat, addCollider: true);
            CreateBox("MexicoMagico_FrontRosaTrim_S", bldgRoot.transform,
                new Vector3(-11.55f, 1.61f, -15.01f), new Vector3(0.06f, 2.82f, 0.09f), rosaMexicanoMat, addCollider: false);

            // Saliente Derecha (North protruding fuchsia/rosa mexicano wing wall at Z = -13.47m, X = -12.02..-11.48m, Y = 0.20..3.02m)
            // Meets the flat Mystery-Door false wall (X = -12.00m) at a clean 90° corner, with black front metal post!
            CreateBox("MexicoMagico_RosaSideWall", bldgRoot.transform,
                new Vector3(-11.75f, 1.61f, -13.47f), new Vector3(0.54f, 2.82f, 0.08f), rosaMexicanoMat, addCollider: true);
            CreateBox("MexicoMagico_FrontRosaTrim_N", bldgRoot.transform,
                new Vector3(-11.49f, 1.61f, -13.50f), new Vector3(0.06f, 2.82f, 0.09f), blackFrameMat, addCollider: false);
            CreateBox("MexicoMagico_InteriorNorthPartition", bldgRoot.transform,
                new Vector3(-12.56f, 1.48f, -13.49f), new Vector3(1.10f, 2.56f, 0.08f), creamStuccoMat, addCollider: true);

            // Recessed pink perforated lattice screen (celosía rosa) sitting back on the original diagonal parapet (visible in Image 1 & 2!)
            CreateBox("MexicoMagico_RosaRoofScreen", angledWingRoot.transform,
                new Vector3(0.04f, 4.18f, 2.82f), new Vector3(0.08f, 0.52f, 2.05f), rosaMexicanoMat, addCollider: false);

            // Coffee Bar Interior visible behind the aligned 4-bay glass storefront
            CreateBox("MexicoMagico_InteriorBackWall", bldgRoot.transform,
                new Vector3(-13.02f, 1.48f, mmCenterZ), new Vector3(0.10f, 2.56f, 1.56f), weatheredCreamMat, addCollider: false);
            CreateBox("MexicoMagico_CounterBar", bldgRoot.transform,
                new Vector3(-12.42f, 0.66f, -14.38f), new Vector3(0.52f, 0.92f, 1.02f), counterGreyMat, addCollider: false);
            CreateBox("MexicoMagico_CounterTop", bldgRoot.transform,
                new Vector3(-12.42f, 1.14f, -14.38f), new Vector3(0.56f, 0.05f, 1.06f), aluminumMat, addCollider: false);
            CreateBox("MexicoMagico_SyrupShelf1", bldgRoot.transform,
                new Vector3(-12.84f, 1.48f, -14.50f), new Vector3(0.22f, 0.04f, 0.70f), whiteFrameMat, addCollider: false);
            CreateBox("MexicoMagico_SyrupShelf2", bldgRoot.transform,
                new Vector3(-12.84f, 1.82f, -14.50f), new Vector3(0.22f, 0.04f, 0.70f), whiteFrameMat, addCollider: false);
            CreateBox("MexicoMagico_BlueCooler", bldgRoot.transform,
                new Vector3(-12.82f, 1.58f, -13.92f), new Vector3(0.24f, 0.72f, 0.36f), blueCoolerMat, addCollider: false);
            CreateBox("MexicoMagico_InteriorNeonCup", bldgRoot.transform,
                new Vector3(-12.02f, 1.40f, -14.76f), new Vector3(0.03f, 0.16f, 0.18f), neonRedCupMat, addCollider: false);

            // 4-Bay Black-Framed Glass Storefront aligned at the front (X = -11.96m, between the protruding fuchsia fins!)
            CreateBox("MexicoMagico_StorefrontGlass", bldgRoot.transform,
                new Vector3(-11.96f, 1.28f, mmCenterZ), new Vector3(0.03f, 2.12f, 1.48f), clearGlassMat, addCollider: false);
            float[] mmMullionsZ = new float[] { -14.96f, -14.60f, -14.25f, -13.90f, -13.54f };
            for (int i = 0; i < mmMullionsZ.Length; i++)
            {
                CreateBox($"MexicoMagico_Mullion_{i}", bldgRoot.transform,
                    new Vector3(-11.94f, 1.28f, mmMullionsZ[i]), new Vector3(0.08f, 2.16f, 0.055f), blackFrameMat, addCollider: false);
            }
            CreateBox("MexicoMagico_FrameTop", bldgRoot.transform,
                new Vector3(-11.94f, 2.36f, mmCenterZ), new Vector3(0.09f, 0.07f, 1.52f), blackFrameMat, addCollider: false);
            CreateBox("MexicoMagico_FrameBot", bldgRoot.transform,
                new Vector3(-11.94f, 0.23f, mmCenterZ), new Vector3(0.09f, 0.06f, 1.52f), blackFrameMat, addCollider: false);
            CreateBox("MexicoMagico_DoorHandle", bldgRoot.transform,
                new Vector3(-11.89f, 1.18f, -13.84f), new Vector3(0.04f, 0.32f, 0.025f), aluminumMat, addCollider: false);

            // Hot-Pink Neon Horizontal Banner ("CAFÉ PREMIUM • CAFÉ DE OLLA / PAN DE MUERTO") framed between the fuchsia side fins (Y = 2.68m)
            var mmBannerRoot = new GameObject("Sign_MexicoMagico_Banner");
            mmBannerRoot.transform.SetParent(bldgRoot.transform, false);
            mmBannerRoot.transform.localPosition = new Vector3(-11.82f, 2.68f, mmCenterZ);
            CreateBox("MexicoMagico_BannerBox", mmBannerRoot.transform,
                new Vector3(-0.05f, 0f, 0f), new Vector3(0.10f, 0.54f, 1.50f), blackFrameMat, addCollider: false);
            var mmBannerQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mmBannerQuad.name = "MexicoMagico_BannerFace";
            mmBannerQuad.transform.SetParent(mmBannerRoot.transform, false);
            mmBannerQuad.transform.localPosition = new Vector3(0.006f, 0f, 0f);
            mmBannerQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            mmBannerQuad.transform.localScale = new Vector3(1.46f, 0.50f, 1.0f);
            AssignMat(mmBannerQuad, mexicoBannerMat);
            UnityEngine.Object.DestroyImmediate(mmBannerQuad.GetComponent<Collider>());

            var mmBannerLightGo = new GameObject("MexicoMagico_BannerGlowLight");
            mmBannerLightGo.transform.SetParent(mmBannerRoot.transform, false);
            mmBannerLightGo.transform.localPosition = new Vector3(0.42f, -0.08f, 0f);
            var mmBannerLight = mmBannerLightGo.AddComponent<Light>();
            mmBannerLight.type = LightType.Point;
            mmBannerLight.color = new Color(1.0f, 0.14f, 0.52f, 1f);
            mmBannerLight.intensity = 0.0f;
            mmBannerLight.range = 4.2f;
            mmBannerLight.enabled = false;
            mmBannerLight.shadows = LightShadows.None;
            mmBannerLight.renderMode = LightRenderMode.Auto;

            var mmBannerGlow = mmBannerRoot.AddComponent<IlluminatedSignGlowController>();
            mmBannerGlow.baseEmissiveMaterial = mexicoBannerMat;
            mmBannerGlow.emissiveRenderers = new Renderer[] { mmBannerQuad.GetComponent<Renderer>() };
            mmBannerGlow.nightGlowLights = new Light[] { mmBannerLight };
            mmBannerGlow.dayEmissionIntensity = 0.85f;
            mmBannerGlow.nightEmissionIntensity = 4.2f;
            mmBannerGlow.nightLightColor = new Color(1.0f, 0.14f, 0.52f, 1f);
            mmBannerGlow.nightLightIntensity = 1.60f;
            mmBannerGlow.nightLightRange = 4.2f;

            // Small Protruding Entrance Arrow Sign Box mounted on the front post of the North fuchsia wing (Image 1!)
            var arrowRoot = new GameObject("Sign_MexicoMagico_ArrowBox");
            arrowRoot.transform.SetParent(bldgRoot.transform, false);
            arrowRoot.transform.localPosition = new Vector3(-11.42f, 2.15f, -13.52f);
            var arrowBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowBody.name = "Arrow_Housing";
            arrowBody.transform.SetParent(arrowRoot.transform, false);
            arrowBody.transform.localPosition = Vector3.zero;
            arrowBody.transform.localScale = new Vector3(0.32f, 0.44f, 0.09f);
            AssignMat(arrowBody, blackFrameMat);
            UnityEngine.Object.DestroyImmediate(arrowBody.GetComponent<Collider>());

            var arrowFaceS = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arrowFaceS.name = "Arrow_Face_South";
            arrowFaceS.transform.SetParent(arrowRoot.transform, false);
            arrowFaceS.transform.localPosition = new Vector3(0f, 0f, -0.048f);
            arrowFaceS.transform.localRotation = Quaternion.identity;
            arrowFaceS.transform.localScale = new Vector3(0.30f, 0.42f, 1.0f);
            AssignMat(arrowFaceS, mexicoArrowMat);
            UnityEngine.Object.DestroyImmediate(arrowFaceS.GetComponent<Collider>());

            var arrowFaceN = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arrowFaceN.name = "Arrow_Face_North";
            arrowFaceN.transform.SetParent(arrowRoot.transform, false);
            arrowFaceN.transform.localPosition = new Vector3(0f, 0f, 0.048f);
            arrowFaceN.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            arrowFaceN.transform.localScale = new Vector3(0.30f, 0.42f, 1.0f);
            AssignMat(arrowFaceN, mexicoArrowMat);
            UnityEngine.Object.DestroyImmediate(arrowFaceN.GetComponent<Collider>());

            var arrowGlow = arrowRoot.AddComponent<IlluminatedSignGlowController>();
            arrowGlow.baseEmissiveMaterial = mexicoArrowMat;
            arrowGlow.emissiveRenderers = new Renderer[] { arrowFaceS.GetComponent<Renderer>(), arrowFaceN.GetComponent<Renderer>() };
            arrowGlow.nightGlowLights = new Light[0];
            arrowGlow.dayEmissionIntensity = 0.70f;
            arrowGlow.nightEmissionIntensity = 3.6f;

            // Cantilevered Double-Sided Circular Cutout Lightbox ("MÉXICO MÁGICO · CAFÉ EXPRESS-O")
            CreateMexicoMagicoCircularLightbox("Sign_MexicoMagico_CircleSign", bldgRoot.transform,
                pos: new Vector3(-10.85f, 3.82f, -14.20f),
                armMat: blackFrameMat, drumMat: blackFrameMat, circleFaceMat: mexicoCircleMat);

            // Secondary West Azucenas Neighbor Block (shifted cleanly to align with Z = -26.80m!)
            CreateBox("NWAnnex_WestNeighbor_Block", bldgRoot.transform,
                new Vector3(-32.00f, 1.95f, -22.40f), new Vector3(12.00f, 3.90f, 8.80f), weatheredCreamMat, addCollider: true);
            CreateBox("NWAnnex_WestNeighbor_Cornice", bldgRoot.transform,
                new Vector3(-32.00f, 3.88f, -26.85f), new Vector3(12.05f, 0.18f, 0.14f), terracottaMat, addCollider: false);
        }

        private static void BuildSouthWestCornerCounterpart(Transform root, Transform envParent, List<Vector2> swArc,
            Material curbMat, Material concreteMat, Material flagstoneMat, Material tactileMat,
            Material bollardBlackMat, Material bollardReflectiveMat, Material lampPoleMat)
        {
            Transform swParent = envParent ?? root;

            // Straight South-of-Azucenas Curb (X = -2.70, Z = -60.00..-40.21) matching Curb_East_SouthOfAzucenas
            float straightLen = SwSouthCutZ - SouthExtentZ; // 19.79f
            float straightCenterZ = 0.5f * (SwSouthCutZ + SouthExtentZ);
            CreateBox("Curb_West_SouthOfAzucenas", swParent,
                new Vector3(-2.70f, 0.10f, straightCenterZ), new Vector3(CurbWidth, 0.20f, straightLen), curbMat, addCollider: true);

            // SW Corner Curved Curb (from (-2.60, -40.21) around R=3.5m arc to (-6.10, -36.71), plus 20cm straight to ramp East cheek -6.30)
            var swArcPlusShort = new List<Vector2>(swArc);
            swArcPlusShort.Add(new Vector2(RampEastX + RampWingWidth, AzSouthAtJuarez));
            BuildCurbChain("Azucenas_West_SW_Curb_Arc", root, swArcPlusShort, outwardIsLeft: true, CurbWidth, SidewalkTopY + 0.004f, curbMat);

            // SW Azucenas West Straight Curb from RampWestX - wing (-8.70) to -38.00
            var swWestCurb = new List<Vector2>
            {
                new Vector2(RampWestX - RampWingWidth, AzSouthAtJuarez),
                new Vector2(-38.00f, AzSouthAtJuarez)
            };
            BuildCurbChain("Azucenas_West_SW_Curb_Straight", root, swWestCurb, outwardIsLeft: true, CurbWidth, SidewalkTopY + 0.004f, curbMat);

            // SW Pedestrian Ramp (X = -8.40 .. -6.20, sloping South into AzSouthAtJuarez - CurbWidth)
            BuildPedestrianRamp("Azucenas_West_SW_Ramp", root, RampWestX, RampEastX,
                apronEdgeZ: AzSouthAtJuarez, slopeDirectionZ: -1f, concreteMat, curbMat, tactileMat);

            // Smooth Concrete Sidewalk Apron South of Azucenas (matching photo foreground strip + corner + Azucenas S)
            List<Vector2> innerSwArc = OffsetChainLeft(swArc, CurbWidth);
            float innerAzSouthZ = AzSouthAtJuarez - CurbWidth; // -36.91f
            float rampBackZ = innerAzSouthZ - 1.35f;           // -38.26f

            var swSidewalkPoly = new List<Vector2>
            {
                new Vector2(-38.00f, SouthExtentZ),
                new Vector2(JuarezWestCurbFaceX - CurbWidth, SouthExtentZ),
                new Vector2(JuarezWestCurbFaceX - CurbWidth, SwSouthCutZ)
            };
            for (int i = 1; i < innerSwArc.Count; i++)
            {
                swSidewalkPoly.Add(innerSwArc[i]);
            }
            swSidewalkPoly.Add(new Vector2(RampEastX + RampWingWidth, innerAzSouthZ));
            swSidewalkPoly.Add(new Vector2(RampEastX, rampBackZ));
            swSidewalkPoly.Add(new Vector2(RampWestX, rampBackZ));
            swSidewalkPoly.Add(new Vector2(RampWestX - RampWingWidth, innerAzSouthZ));
            swSidewalkPoly.Add(new Vector2(-38.00f, innerAzSouthZ));

            // Use Sidewalk_West_SouthOfAzucenas on ConcreteMaterial as base walkable promenade
            CreateFlatPrism("Sidewalk_West_SouthOfAzucenas", swParent, swSidewalkPoly, SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // Warm stone flagstone promenade zone West of X = -5.60m on SW block (clean L-line matching photo!)
            var flagstonePoly = new List<Vector2>
            {
                new Vector2(-38.00f, SouthExtentZ + 0.5f),
                new Vector2(-5.60f, SouthExtentZ + 0.5f),
                new Vector2(-5.60f, rampBackZ - 0.15f),
                new Vector2(-9.20f, rampBackZ - 0.15f),
                new Vector2(-9.20f, innerAzSouthZ - 0.10f),
                new Vector2(-38.00f, innerAzSouthZ - 0.10f)
            };
            CreateFlatPrism("Sidewalk_West_SW_FlagstoneApron", root, flagstonePoly, SidewalkTopY - 0.005f, SidewalkTopY + 0.002f, flagstoneMat, addCollider: false);

            // Diagonal Tactile Pedestrian Strip leading from SW foreground NNE flush up to SW crossing ramp
            Vector2 tStart = new Vector2(-8.85f, -46.00f);
            Vector2 tEnd = new Vector2(-7.70f, rampBackZ);
            Vector2 tDir = (tEnd - tStart).normalized;
            Vector2 tPerp = new Vector2(-tDir.y, tDir.x) * 0.28f;
            var tactileStripPoly = new List<Vector2>
            {
                tStart - tPerp,
                tStart + tPerp,
                tEnd + tPerp,
                tEnd - tPerp
            };
            CreateFlatPrism("Sidewalk_West_SW_TactileGuideStrip", root, tactileStripPoly, SidewalkTopY, SidewalkTopY + 0.006f, tactileMat, addCollider: false);

            // Foreground SW Corner Bollards & Dark Octagonal Pedestrian Lamppost (matching right foreground of photo)
            CreateBollard("Bollard_SW_Corner_East", root, new Vector3(-3.20f, SidewalkTopY, -39.95f), bollardBlackMat, bollardReflectiveMat);
            CreateBollard("Bollard_SW_Corner_Apex", root, new Vector3(-5.85f, SidewalkTopY, -37.35f), bollardBlackMat, bollardReflectiveMat);
            CreateForegroundLamppost("Lamppost_SW_Corner", root, new Vector3(-4.85f, SidewalkTopY, -39.55f), lampPoleMat, bollardReflectiveMat);
        }

        private static void BuildPedestrianRamp(string name, Transform parent, float xWest, float xEast,
            float apronEdgeZ, float slopeDirectionZ, Material concreteMat, Material curbMat, Material tactileMat)
        {
            var rampRoot = new GameObject(name);
            rampRoot.transform.SetParent(parent);
            rampRoot.transform.position = Vector3.zero;

            float innerEdgeZ = apronEdgeZ + slopeDirectionZ * CurbWidth;
            float backEdgeZ = innerEdgeZ + slopeDirectionZ * 1.35f;
            float zNear = Mathf.Min(apronEdgeZ, innerEdgeZ);
            float zFar = Mathf.Max(apronEdgeZ, innerEdgeZ);

            // Flush street-level curb apron at entrance
            CreateFlatPrism($"{name}_FlushLip", rampRoot.transform,
                new List<Vector2>
                {
                    new Vector2(xWest - RampWingWidth, zNear),
                    new Vector2(xEast + RampWingWidth, zNear),
                    new Vector2(xEast + RampWingWidth, zFar),
                    new Vector2(xWest - RampWingWidth, zFar)
                },
                SidewalkBotY, RoadTopY + 0.016f, curbMat, addCollider: true);

            // Sloped ramp deck mesh (Y = 0.016 at innerEdgeZ up to SidewalkTopY at backEdgeZ)
            var go = new GameObject($"{name}_SlopeDeck");
            go.transform.SetParent(rampRoot.transform);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = concreteMat;

            float yLow = RoadTopY + 0.016f;
            float yHigh = SidewalkTopY;

            // Main quad + side sloped cheek triangles
            var v = new Vector3[]
            {
                new Vector3(xWest, yLow, innerEdgeZ),                 // 0: front-left low
                new Vector3(xEast, yLow, innerEdgeZ),                 // 1: front-right low
                new Vector3(xEast, yHigh, backEdgeZ),                 // 2: back-right high
                new Vector3(xWest, yHigh, backEdgeZ),                 // 3: back-left high
                new Vector3(xWest - RampWingWidth, yLow, innerEdgeZ), // 4: left wing front low
                new Vector3(xWest - RampWingWidth, yHigh, innerEdgeZ),// 5: left wing front high
                new Vector3(xEast + RampWingWidth, yLow, innerEdgeZ), // 6: right wing front low
                new Vector3(xEast + RampWingWidth, yHigh, innerEdgeZ) // 7: right wing front high
            };
            int[] t = slopeDirectionZ > 0f
                ? new int[]
                {
                    0, 3, 2, 0, 2, 1,   // main slope facing +Y
                    0, 4, 3,            // west flare cheek (+Y)
                    4, 5, 3,            // west vertical back-step transition
                    1, 2, 6,            // east flare cheek (+Y)
                    6, 2, 7             // east vertical back-step transition
                }
                : new int[]
                {
                    0, 1, 2, 0, 2, 3,
                    0, 3, 4,
                    4, 3, 5,
                    1, 6, 2,
                    6, 7, 2
                };

            var m = new Mesh { name = $"{name}_SlopeMesh" };
            m.vertices = v;
            m.triangles = t;
            m.uv = new Vector2[]
            {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
                new Vector2(-0.15f,0), new Vector2(-0.15f,0), new Vector2(1.15f,0), new Vector2(1.15f,0)
            };
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = m;

            // Tactile warning pad on lower half of slope
            float zTac0 = innerEdgeZ + slopeDirectionZ * 0.08f;
            float zTac1 = innerEdgeZ + slopeDirectionZ * 0.42f;
            float yTac0 = Mathf.Lerp(yLow, yHigh, 0.08f / 1.35f) + 0.005f;
            float yTac1 = Mathf.Lerp(yLow, yHigh, 0.42f / 1.35f) + 0.005f;
            CreateSlopedStrip($"{name}_TactilePad", rampRoot.transform,
                xWest + 0.08f, xEast - 0.08f, zTac0, zTac1, yTac0, yTac1, slopeDirectionZ, tactileMat);
        }

        private static void CreateSlopedStrip(string name, Transform parent, float x0, float x1, float z0, float z1,
            float y0, float y1, float slopeDirZ, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var m = new Mesh { name = $"{name}_Mesh" };
            m.vertices = new[]
            {
                new Vector3(x0, y0, z0),
                new Vector3(x1, y0, z0),
                new Vector3(x1, y1, z1),
                new Vector3(x0, y1, z1)
            };
            m.triangles = slopeDirZ > 0f ? new[] { 0, 3, 2, 0, 2, 1 } : new[] { 0, 1, 2, 0, 2, 3 };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.RecalculateNormals();
            mf.sharedMesh = m;
        }

        private static void CreateBollard(string name, Transform parent, Vector3 basePos, Material blackMat, Material silverMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = basePos;

            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "BasePlate";
            basePlate.transform.SetParent(root.transform);
            basePlate.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            basePlate.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
            AssignMat(basePlate, blackMat);
            UnityEngine.Object.DestroyImmediate(basePlate.GetComponent<Collider>());

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(root.transform);
            post.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            post.transform.localScale = new Vector3(0.14f, 0.44f, 0.14f);
            AssignMat(post, blackMat);

            var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "ReflectiveCollar";
            collar.transform.SetParent(root.transform);
            collar.transform.localPosition = new Vector3(0f, 0.74f, 0f);
            collar.transform.localScale = new Vector3(0.146f, 0.045f, 0.146f);
            AssignMat(collar, silverMat);
            UnityEngine.Object.DestroyImmediate(collar.GetComponent<Collider>());
        }

        private static void CreateForegroundLamppost(string name, Transform parent, Vector3 basePos, Material darkMat, Material collarMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = basePos;

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Plinth";
            plinth.transform.SetParent(root.transform);
            plinth.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            plinth.transform.localScale = new Vector3(0.32f, 0.18f, 0.32f);
            AssignMat(plinth, darkMat);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(root.transform);
            shaft.transform.localPosition = new Vector3(0f, 2.30f, 0f);
            shaft.transform.localScale = new Vector3(0.11f, 2.15f, 0.11f);
            AssignMat(shaft, darkMat);

            var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "MidCollar";
            collar.transform.SetParent(root.transform);
            collar.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            collar.transform.localScale = new Vector3(0.15f, 0.055f, 0.15f);
            AssignMat(collar, collarMat);
            UnityEngine.Object.DestroyImmediate(collar.GetComponent<Collider>());

            var lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "LanternHead";
            lantern.transform.SetParent(root.transform);
            lantern.transform.localPosition = new Vector3(0f, 4.42f, 0f);
            lantern.transform.localScale = new Vector3(0.24f, 0.36f, 0.24f);
            AssignMat(lantern, collarMat);
            UnityEngine.Object.DestroyImmediate(lantern.GetComponent<Collider>());

            var lanternCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lanternCap.name = "LanternCap";
            lanternCap.transform.SetParent(root.transform);
            lanternCap.transform.localPosition = new Vector3(0f, 4.64f, 0f);
            lanternCap.transform.localScale = new Vector3(0.30f, 0.08f, 0.30f);
            AssignMat(lanternCap, darkMat);
            UnityEngine.Object.DestroyImmediate(lanternCap.GetComponent<Collider>());
        }

        private static void CreateSouthWedgeAwning(string name, Transform parent, float centerX, float wallZ, float topY,
            float widthX, float projectionZ, float dropY, Material fabricMat, Material valanceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);

            float len = Mathf.Sqrt(projectionZ * projectionZ + dropY * dropY);
            float slopeDeg = Mathf.Atan2(dropY, projectionZ) * Mathf.Rad2Deg;

            // Rotates downward toward -Z (facing Calle Azucenas)
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(root.transform);
            canopy.transform.position = new Vector3(centerX, topY - dropY * 0.5f, wallZ - projectionZ * 0.5f);
            canopy.transform.rotation = Quaternion.Euler(-slopeDeg, 0f, 0f);
            canopy.transform.localScale = new Vector3(widthX, 0.06f, len);
            AssignMat(canopy, fabricMat);
            UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());

            var valance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valance.name = "Valance";
            valance.transform.SetParent(root.transform);
            valance.transform.position = new Vector3(centerX, topY - dropY - 0.10f, wallZ - projectionZ);
            valance.transform.localScale = new Vector3(widthX, 0.22f, 0.05f);
            AssignMat(valance, valanceMat);
            UnityEngine.Object.DestroyImmediate(valance.GetComponent<Collider>());
        }

        private static void CreateEastWedgeAwning(string name, Transform parent, float centerZ, float wallX, float topY,
            float widthZ, float projectionX, float dropY, Material fabricMat, Material valanceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);

            float len = Mathf.Sqrt(projectionX * projectionX + dropY * dropY);
            float slopeDeg = Mathf.Atan2(dropY, projectionX) * Mathf.Rad2Deg;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(root.transform);
            canopy.transform.position = new Vector3(wallX + projectionX * 0.5f, topY - dropY * 0.5f, centerZ);
            canopy.transform.rotation = Quaternion.Euler(0f, 0f, -slopeDeg);
            canopy.transform.localScale = new Vector3(len, 0.06f, widthZ);
            AssignMat(canopy, fabricMat);
            UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());

            var valance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valance.name = "Valance";
            valance.transform.SetParent(root.transform);
            valance.transform.position = new Vector3(wallX + projectionX, topY - dropY - 0.10f, centerZ);
            valance.transform.localScale = new Vector3(0.05f, 0.22f, widthZ);
            AssignMat(valance, valanceMat);
            UnityEngine.Object.DestroyImmediate(valance.GetComponent<Collider>());
        }

        #region Geometry & Material Helpers

        private static List<Vector2> SampleBezierChain(Vector2 p0, Vector2 t0, Vector2 p3, Vector2 t3, float handleLen, int steps)
        {
            Vector2 p1 = p0 + t0.normalized * handleLen;
            Vector2 p2 = p3 - t3.normalized * handleLen;
            var pts = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                pts.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
            return pts;
        }

        private static List<Vector2> OffsetChainRight(List<Vector2> chain, float dist)
        {
            var outPts = new List<Vector2>(chain.Count);
            for (int i = 0; i < chain.Count; i++)
            {
                Vector2 dPrev = i > 0 ? (chain[i] - chain[i - 1]).normalized : (chain[1] - chain[0]).normalized;
                Vector2 dNext = i < chain.Count - 1 ? (chain[i + 1] - chain[i]).normalized : dPrev;
                Vector2 r0 = new Vector2(dPrev.y, -dPrev.x);
                Vector2 r1 = new Vector2(dNext.y, -dNext.x);
                Vector2 avg = (r0 + r1);
                avg = avg.sqrMagnitude < 1e-4f ? r1 : avg.normalized;
                float miter = Mathf.Clamp(1f / Mathf.Max(0.45f, Vector2.Dot(avg, r1)), 1f, 2.1f);
                outPts.Add(chain[i] + avg * (dist * miter));
            }
            return outPts;
        }

        private static List<Vector2> OffsetChainLeft(List<Vector2> chain, float dist)
        {
            var outPts = new List<Vector2>(chain.Count);
            for (int i = 0; i < chain.Count; i++)
            {
                Vector2 dPrev = i > 0 ? (chain[i] - chain[i - 1]).normalized : (chain[1] - chain[0]).normalized;
                Vector2 dNext = i < chain.Count - 1 ? (chain[i + 1] - chain[i]).normalized : dPrev;
                Vector2 l0 = new Vector2(-dPrev.y, dPrev.x);
                Vector2 l1 = new Vector2(-dNext.y, dNext.x);
                Vector2 avg = (l0 + l1);
                avg = avg.sqrMagnitude < 1e-4f ? l1 : avg.normalized;
                float miter = Mathf.Clamp(1f / Mathf.Max(0.45f, Vector2.Dot(avg, l1)), 1f, 2.1f);
                outPts.Add(chain[i] + avg * (dist * miter));
            }
            return outPts;
        }

        private static void BuildCurbChain(string name, Transform parent, List<Vector2> streetEdgePts,
            bool outwardIsLeft, float curbWidth, float yTop, Material curbMat)
        {
            List<Vector2> inner = outwardIsLeft
                ? OffsetChainLeft(streetEdgePts, curbWidth)
                : OffsetChainRight(streetEdgePts, curbWidth);
            BuildQuadStripPrism(name, parent, streetEdgePts, inner, SidewalkBotY, yTop, curbMat, addCollider: true);
        }

        private static void BuildQuadStripPrism(string name, Transform parent, List<Vector2> leftEdge, List<Vector2> rightEdge,
            float yBot, float yTop, Material mat, bool addCollider)
        {
            int n = Mathf.Min(leftEdge.Count, rightEdge.Count);
            if (n < 2) return;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i < n - 1; i++)
            {
                Vector2 l0 = leftEdge[i], l1 = leftEdge[i + 1], r0 = rightEdge[i], r1 = rightEdge[i + 1];
                Vector2 diag = l1 - l0;
                Vector2 across = r0 - l0;
                bool swap = (diag.x * across.y - diag.y * across.x) > 0f;
                Vector2 a0 = swap ? r0 : l0, a1 = swap ? r1 : l1;
                Vector2 b0 = swap ? l0 : r0, b1 = swap ? l1 : r1;

                // Top
                AddQuadY(verts, tris, uvs,
                    new Vector3(a0.x, yTop, a0.y), new Vector3(a1.x, yTop, a1.y),
                    new Vector3(b1.x, yTop, b1.y), new Vector3(b0.x, yTop, b0.y));
                // Left wall
                AddQuadY(verts, tris, uvs,
                    new Vector3(a0.x, yTop, a0.y), new Vector3(a0.x, yBot, a0.y),
                    new Vector3(a1.x, yBot, a1.y), new Vector3(a1.x, yTop, a1.y));
                // Right wall
                AddQuadY(verts, tris, uvs,
                    new Vector3(b1.x, yTop, b1.y), new Vector3(b1.x, yBot, b1.y),
                    new Vector3(b0.x, yBot, b0.y), new Vector3(b0.x, yTop, b0.y));
            }

            // End caps
            AddQuadY(verts, tris, uvs,
                new Vector3(leftEdge[0].x, yTop, leftEdge[0].y),
                new Vector3(rightEdge[0].x, yTop, rightEdge[0].y),
                new Vector3(rightEdge[0].x, yBot, rightEdge[0].y),
                new Vector3(leftEdge[0].x, yBot, leftEdge[0].y));
            AddQuadY(verts, tris, uvs,
                new Vector3(rightEdge[n - 1].x, yTop, rightEdge[n - 1].y),
                new Vector3(leftEdge[n - 1].x, yTop, leftEdge[n - 1].y),
                new Vector3(leftEdge[n - 1].x, yBot, leftEdge[n - 1].y),
                new Vector3(rightEdge[n - 1].x, yBot, rightEdge[n - 1].y));

            GameObject go = CreateMeshObject(name, parent, verts, tris, uvs, mat);
            if (addCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        private static void CreateFlatPrism(string name, Transform parent, List<Vector2> poly,
            float yBot, float yTop, Material mat, bool addCollider)
        {
            if (poly == null || poly.Count < 3) return;
            var pts = new List<Vector2>(poly.Count);
            foreach (Vector2 p in poly)
            {
                if (pts.Count == 0 || (p - pts[pts.Count - 1]).sqrMagnitude > 1e-6f) pts.Add(p);
            }
            if (pts.Count >= 3 && (pts[pts.Count - 1] - pts[0]).sqrMagnitude <= 1e-6f) pts.RemoveAt(pts.Count - 1);
            int n = pts.Count;
            if (n < 3) return;

            float signedArea2 = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = pts[i], p1 = pts[(i + 1) % n];
                signedArea2 += (p0.x * p1.y - p1.x * p0.y);
            }
            if (signedArea2 > 0f) pts.Reverse(); // CW in XZ -> +Y normal

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(pts[i].x, yTop, pts[i].y));
                uvs.Add(new Vector2(pts[i].x * 0.25f, pts[i].y * 0.25f));
            }
            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(pts[i].x, yBot, pts[i].y));
                uvs.Add(new Vector2(pts[i].x * 0.25f, pts[i].y * 0.25f));
            }

            List<int> cap = TriangulateCW(pts);
            for (int t = 0; t + 2 < cap.Count; t += 3)
            {
                int a = cap[t], b = cap[t + 1], c = cap[t + 2];
                tris.AddRange(new[] { a, b, c });
                tris.AddRange(new[] { n + a, n + c, n + b });
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                int b = verts.Count;
                verts.Add(new Vector3(pts[i].x, yTop, pts[i].y));
                verts.Add(new Vector3(pts[next].x, yTop, pts[next].y));
                verts.Add(new Vector3(pts[next].x, yBot, pts[next].y));
                verts.Add(new Vector3(pts[i].x, yBot, pts[i].y));
                uvs.AddRange(new[] { Vector2.up, Vector2.one, Vector2.right, Vector2.zero });
                tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }

            GameObject go = CreateMeshObject(name, parent, verts, tris, uvs, mat);
            if (addCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        private static List<int> TriangulateCW(List<Vector2> pts)
        {
            var result = new List<int>();
            int n = pts.Count;
            if (n < 3) return result;
            var rem = new List<int>(n);
            for (int i = 0; i < n; i++) rem.Add(i);

            int guard = 0;
            while (rem.Count > 3 && guard++ < n * n + 10)
            {
                bool cut = false;
                for (int i = 0; i < rem.Count; i++)
                {
                    int i0 = rem[(i + rem.Count - 1) % rem.Count];
                    int i1 = rem[i];
                    int i2 = rem[(i + 1) % rem.Count];
                    Vector2 a = pts[i0], b = pts[i1], c = pts[i2];
                    if (((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) >= -1e-6f) continue;

                    bool empty = true;
                    for (int j = 0; j < rem.Count; j++)
                    {
                        int k = rem[j];
                        if (k == i0 || k == i1 || k == i2) continue;
                        if (PointInTriXZ(pts[k], a, b, c)) { empty = false; break; }
                    }
                    if (!empty) continue;
                    result.AddRange(new[] { i0, i1, i2 });
                    rem.RemoveAt(i);
                    cut = true;
                    break;
                }
                if (!cut) break;
            }
            if (rem.Count == 3) result.AddRange(new[] { rem[0], rem[1], rem[2] });
            return result;
        }

        private static bool PointInTriXZ(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static void AddQuadY(List<Vector3> v, List<int> t, List<Vector2> u, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int b = v.Count;
            v.AddRange(new[] { p0, p1, p2, p3 });
            u.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
        }

        private static void AddQuadXZ(List<Vector3> v, List<int> t, List<Vector2> u, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float y)
        {
            // Automatically enforces +Y upward winding in XZ
            float cross = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
            int b = v.Count;
            v.Add(new Vector3(p0.x, y, p0.y));
            v.Add(new Vector3(p1.x, y, p1.y));
            v.Add(new Vector3(p2.x, y, p2.y));
            v.Add(new Vector3(p3.x, y, p3.y));
            u.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            if (cross < 0f)
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            else
                t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }

        private static GameObject CreateMeshObject(string name, Transform parent, List<Vector3> v, List<int> t, List<Vector2> u, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var m = new Mesh { name = $"{name}_Mesh" };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.SetUVs(0, u);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            mf.sharedMesh = m;
            return go;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool addCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            AssignMat(go, mat);
            if (!addCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            return go;
        }

        private static void AssignMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        private static Material LoadOrFallbackMat(string path, Color fallbackColor, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            return CreateOrGetMat(path, fallbackColor, smoothness, 0.0f);
        }

        private static Material CreateOrGetMat(string path, Color color, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTexMat(string matPath, string texPath, Color? tint = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }
            Color col = tint ?? Color.white;
            mat.SetColor("_BaseColor", col);
            mat.SetColor("_Color", col);
            mat.SetFloat("_Smoothness", 0.14f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetAlphaDecalMat(string matPath, string texPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_AlphaClip", 1.0f);
            mat.SetFloat("_Cutoff", 0.35f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateCfeMeter(string name, Transform parent, Vector3 worldPos, Material metalMat, Material glassDomeMat, Material faceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;

            var baseSocket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseSocket.name = "CFE_SocketBase";
            baseSocket.transform.SetParent(root.transform, false);
            baseSocket.transform.localPosition = Vector3.zero;
            baseSocket.transform.localScale = new Vector3(0.06f, 0.26f, 0.24f);
            AssignMat(baseSocket, metalMat);
            UnityEngine.Object.DestroyImmediate(baseSocket.GetComponent<Collider>());

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CFE_BezelRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0.035f, 0f, 0f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ring.transform.localScale = new Vector3(0.195f, 0.015f, 0.195f);
            AssignMat(ring, metalMat);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());

            var dial = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dial.name = "CFE_DialFace";
            dial.transform.SetParent(root.transform, false);
            dial.transform.localPosition = new Vector3(0.052f, 0f, 0f);
            dial.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            dial.transform.localScale = new Vector3(0.17f, 0.17f, 1.0f);
            AssignMat(dial, faceMat);
            UnityEngine.Object.DestroyImmediate(dial.GetComponent<Collider>());

            var dome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dome.name = "CFE_GlassDome";
            dome.transform.SetParent(root.transform, false);
            dome.transform.localPosition = new Vector3(0.082f, 0f, 0f);
            dome.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            dome.transform.localScale = new Vector3(0.18f, 0.042f, 0.18f);
            AssignMat(dome, glassDomeMat);
            UnityEngine.Object.DestroyImmediate(dome.GetComponent<Collider>());
        }

        private static void CreateDualSimultaneousExchangeLedSign(string name, Transform parent, Vector3 pos,
            Material frameMat, Material bezelMat, Material armMat, Material liveLedBaseMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            // Two crisp horizontal white/silver steel support arms projecting from the wall (local X = -1.12m -> -0.44m)
            void MakePart(string pName, Vector3 lPos, Vector3 lScale, Material mat)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = pName;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = lPos;
                go.transform.localScale = lScale;
                AssignMat(go, mat);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            MakePart("Billemex_WallFlange_Top", new Vector3(-1.11f, 0.27f, 0f), new Vector3(0.045f, 0.14f, 0.14f), frameMat);
            MakePart("Billemex_WallFlange_Bot", new Vector3(-1.11f, -0.27f, 0f), new Vector3(0.045f, 0.14f, 0.14f), frameMat);
            MakePart("Billemex_MountArm_Top", new Vector3(-0.78f, 0.27f, 0f), new Vector3(0.68f, 0.05f, 0.05f), armMat);
            MakePart("Billemex_MountArm_Bot", new Vector3(-0.78f, -0.27f, 0f), new Vector3(0.68f, 0.05f, 0.05f), armMat);

            // Silver/white aluminum outer cabinet frame + matte black inner bezel (0.88m wide x 0.76m tall x 0.14m thick)
            MakePart("Billemex_LED_OuterFrame", Vector3.zero, new Vector3(0.88f, 0.76f, 0.13f), frameMat);
            MakePart("Billemex_LED_InnerBezel", Vector3.zero, new Vector3(0.82f, 0.70f, 0.142f), bezelMat);

            // South-facing (-Z) & North-facing (+Z) simultaneous COMPRA + VENTA screen quads
            var southQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            southQuad.name = "Billemex_LED_Face_South";
            southQuad.transform.SetParent(root.transform, false);
            southQuad.transform.localPosition = new Vector3(0f, 0f, -0.075f);
            southQuad.transform.localRotation = Quaternion.identity;
            southQuad.transform.localScale = new Vector3(0.79f, 0.67f, 1.0f);
            AssignMat(southQuad, liveLedBaseMat);
            UnityEngine.Object.DestroyImmediate(southQuad.GetComponent<Collider>());

            var northQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northQuad.name = "Billemex_LED_Face_North";
            northQuad.transform.SetParent(root.transform, false);
            northQuad.transform.localPosition = new Vector3(0f, 0f, 0.075f);
            northQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            northQuad.transform.localScale = new Vector3(0.79f, 0.67f, 1.0f);
            AssignMat(northQuad, liveLedBaseMat);
            UnityEngine.Object.DestroyImmediate(northQuad.GetComponent<Collider>());

            // Dual real-time URP PointLights casting red LED glow at night (off in daylight!)
            Light MakeGlowLight(string lName, Vector3 lPos)
            {
                var lg = new GameObject(lName);
                lg.transform.SetParent(root.transform, false);
                lg.transform.localPosition = lPos;
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.18f, 0.14f, 1f);
                l.intensity = 0.0f;
                l.range = 4.6f;
                l.enabled = false;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
                return l;
            }

            Light southLight = MakeGlowLight("Billemex_LED_GlowLight_South", new Vector3(0f, -0.04f, -0.40f));
            Light northLight = MakeGlowLight("Billemex_LED_GlowLight_North", new Vector3(0f, -0.04f, 0.40f));

            var ctrl = root.AddComponent<ExchangeRateLedSignController>();
            ctrl.layoutMode = ExchangeLedLayoutMode.DualSimultaneousGreenRed;
            ctrl.baseLedMaterial = liveLedBaseMat;
            ctrl.slideIntervalSeconds = 4.0f;
            ctrl.pollIntervalSeconds = 300.0f;
            ctrl.displayFaceRenderers = new Renderer[]
            {
                southQuad.GetComponent<Renderer>(),
                northQuad.GetComponent<Renderer>()
            };
            ctrl.glowLights = new Light[] { southLight, northLight };
        }

        private static Material CreateOrGetSeparateEmissiveMat(string matPath, string albedoTexPath, string emissiveTexPath, bool alphaClip)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoTexPath);
            Texture2D emissiveTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissiveTexPath);
            if (albedoTex != null)
            {
                mat.SetTexture("_BaseMap", albedoTex);
                mat.SetTexture("_MainTex", albedoTex);
            }
            if (emissiveTex != null)
            {
                mat.SetTexture("_EmissionMap", emissiveTex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", new Color(0.45f, 0.45f, 0.45f, 1.0f));
            mat.SetFloat("_Smoothness", 0.25f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (alphaClip)
            {
                mat.SetFloat("_AlphaClip", 1.0f);
                mat.SetFloat("_Cutoff", 0.40f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0.0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetSolidEmissiveMat(string matPath, Color baseCol, Color emissiveCol)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", baseCol);
            mat.SetColor("_Color", baseCol);
            mat.SetColor("_EmissionColor", emissiveCol);
            mat.SetFloat("_Smoothness", 0.45f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateEsqChihuasRooftopMarquee(string name, Transform parent, Vector3 pos,
            Material trussMat, Material cabinetMat, Material faceMat, Material yellowBorderMat, Material bulbMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            void MakeLocalBox(string pName, Vector3 lPos, Vector3 lScale, Material mat, Vector3? euler = null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = pName;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = lPos;
                go.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
                go.transform.localScale = lScale;
                AssignMat(go, mat);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            // Black steel rooftop lattice truss anchoring back to building parapet/roof (local X = -1.25 -> -0.48)
            MakeLocalBox("EsqChihuas_Truss_TopBar", new Vector3(-0.88f, 0.56f, 0f), new Vector3(0.84f, 0.05f, 0.05f), trussMat);
            MakeLocalBox("EsqChihuas_Truss_MidBar", new Vector3(-0.88f, 0.02f, 0f), new Vector3(0.84f, 0.05f, 0.05f), trussMat);
            MakeLocalBox("EsqChihuas_Truss_BotBar", new Vector3(-0.88f, -0.56f, 0f), new Vector3(0.84f, 0.05f, 0.05f), trussMat);
            MakeLocalBox("EsqChihuas_Truss_VertPost", new Vector3(-1.26f, -0.45f, 0f), new Vector3(0.06f, 2.06f, 0.06f), trussMat);
            MakeLocalBox("EsqChihuas_Truss_Diag1", new Vector3(-0.88f, 0.29f, 0f), new Vector3(0.96f, 0.04f, 0.04f), trussMat, new Vector3(0f, 0f, -33f));
            MakeLocalBox("EsqChihuas_Truss_Diag2", new Vector3(-0.88f, -0.27f, 0f), new Vector3(0.96f, 0.04f, 0.04f), trussMat, new Vector3(0f, 0f, 33f));

            // Main vertical chocolate-brown marquee cabinet (0.98m wide x 1.46m tall x 0.16m thick)
            MakeLocalBox("EsqChihuas_MarqueeCabinet", Vector3.zero, new Vector3(0.98f, 1.46f, 0.16f), cabinetMat);

            // Double-sided "LA ESQ DE / CH" faces (South -Z & North +Z)
            var faceS = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceS.name = "EsqChihuas_MarqueeFace_South";
            faceS.transform.SetParent(root.transform, false);
            faceS.transform.localPosition = new Vector3(0f, 0f, -0.084f);
            faceS.transform.localRotation = Quaternion.identity;
            faceS.transform.localScale = new Vector3(0.94f, 1.42f, 1.0f);
            AssignMat(faceS, faceMat);
            UnityEngine.Object.DestroyImmediate(faceS.GetComponent<Collider>());

            var faceN = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceN.name = "EsqChihuas_MarqueeFace_North";
            faceN.transform.SetParent(root.transform, false);
            faceN.transform.localPosition = new Vector3(0f, 0f, 0.084f);
            faceN.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            faceN.transform.localScale = new Vector3(0.94f, 1.42f, 1.0f);
            AssignMat(faceN, faceMat);
            UnityEngine.Object.DestroyImmediate(faceN.GetComponent<Collider>());

            // Signature Golden-Yellow "J"/"L" Marquee Border along street-facing (+X) & bottom (-Y) edges!
            MakeLocalBox("EsqChihuas_MarqueeBorder_East", new Vector3(0.58f, -0.08f, 0f), new Vector3(0.20f, 1.66f, 0.14f), yellowBorderMat);
            MakeLocalBox("EsqChihuas_MarqueeBorder_Bot", new Vector3(0.10f, -0.82f, 0f), new Vector3(1.16f, 0.19f, 0.14f), yellowBorderMat);
            MakeLocalBox("EsqChihuas_MarqueeTrim_East", new Vector3(0.69f, -0.08f, 0f), new Vector3(0.03f, 1.70f, 0.17f), trussMat);
            MakeLocalBox("EsqChihuas_MarqueeTrim_Bot", new Vector3(0.10f, -0.92f, 0f), new Vector3(1.20f, 0.03f, 0.17f), trussMat);

            // 3D Warm Vintage Marquee Bulbs along the yellow "J" channel on BOTH South (-Z) and North (+Z) faces!
            var bulbRenderers = new List<Renderer>();
            void AddBulbPair(float bx, float by, int idx)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bulb.name = $"EsqChihuas_Bulb_{idx}_{(side < 0 ? "S" : "N")}";
                    bulb.transform.SetParent(root.transform, false);
                    bulb.transform.localPosition = new Vector3(bx, by, side * 0.082f);
                    bulb.transform.localScale = new Vector3(0.055f, 0.055f, 0.045f);
                    AssignMat(bulb, bulbMat);
                    UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
                    var br = bulb.GetComponent<Renderer>();
                    if (br != null) br.shadowCastingMode = ShadowCastingMode.Off;
                    bulbRenderers.Add(br);
                }
            }
            // 9 bulbs running vertically down the East golden edge + 5 bulbs along the bottom golden edge
            for (int i = 0; i < 9; i++)
            {
                AddBulbPair(0.58f, 0.64f - i * 0.18f, i);
            }
            for (int j = 0; j < 5; j++)
            {
                AddBulbPair(0.40f - j * 0.20f, -0.82f, 10 + j);
            }

            // Dual warm amber PointLights for marquee nighttime glow
            Light MakeMarqueeLight(string lName, Vector3 lPos)
            {
                var lg = new GameObject(lName);
                lg.transform.SetParent(root.transform, false);
                lg.transform.localPosition = lPos;
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.78f, 0.30f, 1f);
                l.intensity = 0.0f;
                l.range = 5.0f;
                l.enabled = false;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
                return l;
            }
            Light mLightS = MakeMarqueeLight("EsqChihuas_MarqueeLight_S", new Vector3(0.20f, -0.10f, -0.50f));
            Light mLightN = MakeMarqueeLight("EsqChihuas_MarqueeLight_N", new Vector3(0.20f, -0.10f, 0.50f));

            var glowCtrl = root.AddComponent<IlluminatedSignGlowController>();
            glowCtrl.baseEmissiveMaterial = bulbMat;
            glowCtrl.emissiveRenderers = bulbRenderers.ToArray();
            glowCtrl.nightGlowLights = new Light[] { mLightS, mLightN };
            glowCtrl.emissionTint = new Color(1.0f, 0.84f, 0.38f, 1f);
            glowCtrl.dayEmissionIntensity = 0.70f;
            glowCtrl.nightEmissionIntensity = 5.0f;
            glowCtrl.nightLightColor = new Color(1.0f, 0.78f, 0.30f, 1f);
            glowCtrl.nightLightIntensity = 1.75f;
            glowCtrl.nightLightRange = 5.0f;
        }

        private static void CreateMexicoMagicoCircularLightbox(string name, Transform parent, Vector3 pos,
            Material armMat, Material drumMat, Material circleFaceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            // Two horizontal black steel arms projecting from building facade (local X = -1.36m) to cylindrical drum (-0.42m)
            void MakeArm(string aName, Vector3 lPos, Vector3 lScale)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = aName;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = lPos;
                go.transform.localScale = lScale;
                AssignMat(go, armMat);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            MakeArm("MexicoMagico_CircleArm_Top", new Vector3(-0.88f, 0.30f, 0f), new Vector3(0.96f, 0.05f, 0.05f));
            MakeArm("MexicoMagico_CircleArm_Bot", new Vector3(-0.88f, -0.30f, 0f), new Vector3(0.96f, 0.05f, 0.05f));
            MakeArm("MexicoMagico_CircleBrace", new Vector3(-1.32f, 0f, 0f), new Vector3(0.06f, 0.70f, 0.14f));

            // Cylindrical matte-black drum housing (diameter 0.92m, depth 0.14m along Z axis)
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "MexicoMagico_CircleDrum";
            drum.transform.SetParent(root.transform, false);
            drum.transform.localPosition = Vector3.zero;
            drum.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            drum.transform.localScale = new Vector3(0.92f, 0.07f, 0.92f);
            AssignMat(drum, drumMat);
            UnityEngine.Object.DestroyImmediate(drum.GetComponent<Collider>());

            // Double-sided alpha-clipped circular cutout faces (South -Z & North +Z)
            var faceS = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceS.name = "MexicoMagico_CircleFace_South";
            faceS.transform.SetParent(root.transform, false);
            faceS.transform.localPosition = new Vector3(0f, 0f, -0.074f);
            faceS.transform.localRotation = Quaternion.identity;
            faceS.transform.localScale = new Vector3(0.90f, 0.90f, 1.0f);
            AssignMat(faceS, circleFaceMat);
            UnityEngine.Object.DestroyImmediate(faceS.GetComponent<Collider>());

            var faceN = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceN.name = "MexicoMagico_CircleFace_North";
            faceN.transform.SetParent(root.transform, false);
            faceN.transform.localPosition = new Vector3(0f, 0f, 0.074f);
            faceN.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            faceN.transform.localScale = new Vector3(0.90f, 0.90f, 1.0f);
            AssignMat(faceN, circleFaceMat);
            UnityEngine.Object.DestroyImmediate(faceN.GetComponent<Collider>());

            // Dual crimson-magenta night PointLights (OFF in daylight so disk looks crisp matte black + vivid red; backlit at night!)
            Light MakeCircleLight(string lName, Vector3 lPos)
            {
                var lg = new GameObject(lName);
                lg.transform.SetParent(root.transform, false);
                lg.transform.localPosition = lPos;
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.14f, 0.32f, 1f);
                l.intensity = 0.0f;
                l.range = 4.2f;
                l.enabled = false;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
                return l;
            }
            Light cLightS = MakeCircleLight("MexicoMagico_CircleLight_S", new Vector3(0f, 0f, -0.44f));
            Light cLightN = MakeCircleLight("MexicoMagico_CircleLight_N", new Vector3(0f, 0f, 0.44f));

            var glowCtrl = root.AddComponent<IlluminatedSignGlowController>();
            glowCtrl.baseEmissiveMaterial = circleFaceMat;
            glowCtrl.emissiveRenderers = new Renderer[] { faceS.GetComponent<Renderer>(), faceN.GetComponent<Renderer>() };
            glowCtrl.nightGlowLights = new Light[] { cLightS, cLightN };
            glowCtrl.emissionTint = Color.white;
            glowCtrl.dayEmissionIntensity = 0.35f;
            glowCtrl.nightEmissionIntensity = 4.2f;
            glowCtrl.nightLightColor = new Color(1.0f, 0.14f, 0.32f, 1f);
            glowCtrl.nightLightIntensity = 1.45f;
            glowCtrl.nightLightRange = 4.2f;
        }

        private static void CreateEastPlaqueSign(string name, Transform parent, float centerZ, float centerY,
            float widthZ, float heightY, Material plaqueMat, Material faceTexMat)
        {
            var signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent);
            signRoot.transform.position = new Vector3(-11.88f, centerY, centerZ);

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "PlaqueBox";
            box.transform.SetParent(signRoot.transform, false);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.14f, heightY, widthZ);
            AssignMat(box, plaqueMat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FaceQuad";
            quad.transform.SetParent(signRoot.transform, false);
            quad.transform.localPosition = new Vector3(0.076f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            quad.transform.localScale = new Vector3(widthZ - 0.04f, heightY - 0.04f, 1.0f);
            AssignMat(quad, faceTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateExtrudedChannelLetterLogo(
            string name, Transform parent, float wallX, float centerY, float centerZ,
            float widthZ, float heightY, float extrudeDepthX,
            string texAssetPath, string faceMatPath, string sideMatPath, string meshAssetPath,
            Color sideReturnColor, Color nightGlowColor)
        {
            Material faceMat = CreateOrGetAlphaEmissiveMat(faceMatPath, texAssetPath);
            Material sideMat = CreateOrGetMat(sideMatPath, sideReturnColor, 0.36f, 0.18f);

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(wallX, centerY, centerZ);

            // 1. Lightweight solid 3D extruded channel-letter side returns (140x44 grid -> ~80% fewer triangles, zero shadow-pass spam!)
            string fullPngPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, texAssetPath.Replace('/', Path.DirectorySeparatorChar));
            Mesh returnsMesh = BuildExtrudedSilhouetteMesh(fullPngPath, meshAssetPath, widthZ, heightY, extrudeDepthX, gridW: 140, gridH: 44);
            if (returnsMesh != null)
            {
                var returnsGo = new GameObject("Farmalivio_3DReturns");
                returnsGo.transform.SetParent(root.transform, false);
                var mf = returnsGo.AddComponent<MeshFilter>();
                var mr = returnsGo.AddComponent<MeshRenderer>();
                mf.sharedMesh = returnsMesh;
                mr.sharedMaterial = sideMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // 2. Razor-sharp high-res alpha-clipped HDR emissive front acrylic face
            var frontQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontQuad.name = "Farmalivio_FrontFace";
            frontQuad.transform.SetParent(root.transform, false);
            frontQuad.transform.localPosition = new Vector3(extrudeDepthX + 0.002f, 0f, 0f);
            frontQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Normal points +X (East), left-to-right = South (-Z) -> North (+Z)
            frontQuad.transform.localScale = new Vector3(widthZ, heightY, 1.0f);
            AssignMat(frontQuad, faceMat);
            UnityEngine.Object.DestroyImmediate(frontQuad.GetComponent<Collider>());

            // 3. Dedicated night glow PointLight washing the white stucco wall & beige awning below in mint-turquoise at night
            var lightGo = new GameObject("Farmalivio_NightGlowLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(extrudeDepthX + 0.45f, -0.04f, 0f);
            var ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = nightGlowColor;
            ptLight.intensity = 0.0f;
            ptLight.range = 4.2f;
            ptLight.enabled = false;
            ptLight.shadows = LightShadows.None;
            ptLight.renderMode = LightRenderMode.Auto;

            var glowCtrl = root.AddComponent<IlluminatedSignGlowController>();
            glowCtrl.baseEmissiveMaterial = faceMat;
            glowCtrl.emissiveRenderers = new Renderer[] { frontQuad.GetComponent<Renderer>() };
            glowCtrl.nightGlowLights = new Light[] { ptLight };
            glowCtrl.emissionTint = Color.white;
            glowCtrl.dayEmissionIntensity = 0.45f;
            glowCtrl.nightEmissionIntensity = 3.8f;
            glowCtrl.nightLightColor = nightGlowColor;
            glowCtrl.nightLightIntensity = 1.65f;
            glowCtrl.nightLightRange = 4.2f;
        }

        private static Material CreateOrGetAlphaEmissiveMat(string matPath, string texPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
                mat.SetTexture("_EmissionMap", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", new Color(0.45f, 0.45f, 0.45f, 1.0f));
            mat.SetFloat("_Smoothness", 0.58f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_AlphaClip", 1.0f);
            mat.SetFloat("_Cutoff", 0.42f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh BuildExtrudedSilhouetteMesh(string fullPngPath, string meshAssetPath,
            float widthZ, float heightY, float depthX, int gridW, int gridH)
        {
            if (!File.Exists(fullPngPath))
            {
                Debug.LogError($"[WestAzucenasCornerBuilder] Missing silhouette PNG: {fullPngPath}");
                return null;
            }

            byte[] pngBytes = File.ReadAllBytes(fullPngPath);
            var tmpTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tmpTex.LoadImage(pngBytes))
            {
                UnityEngine.Object.DestroyImmediate(tmpTex);
                return null;
            }

            int srcW = tmpTex.width;
            int srcH = tmpTex.height;
            Color32[] srcPixels = tmpTex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tmpTex);

            bool[,] solid = new bool[gridW, gridH];
            for (int y = 0; y < gridH; y++)
            {
                int sy = Mathf.Clamp(Mathf.FloorToInt(((y + 0.5f) / gridH) * srcH), 0, srcH - 1);
                for (int x = 0; x < gridW; x++)
                {
                    int sx = Mathf.Clamp(Mathf.FloorToInt(((x + 0.5f) / gridW) * srcW), 0, srcW - 1);
                    solid[x, y] = srcPixels[sy * srcW + sx].a > 110;
                }
            }

            var verts = new List<Vector3>(16384);
            var norms = new List<Vector3>(16384);
            var uvs = new List<Vector2>(16384);
            var tris = new List<int>(24576);

            void AddOrientedSideQuad(Vector3 p00, Vector3 p10, Vector3 p01, Vector3 p11, Vector3 expectedNormal)
            {
                int b = verts.Count;
                verts.Add(p00); verts.Add(p10); verts.Add(p11); verts.Add(p01);
                norms.Add(expectedNormal); norms.Add(expectedNormal); norms.Add(expectedNormal); norms.Add(expectedNormal);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);

                Vector3 cross = Vector3.Cross(p10 - p00, p11 - p00);
                if (Vector3.Dot(cross, expectedNormal) >= 0f)
                {
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
                else
                {
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                    tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                }
            }

            float cellZ = widthZ / gridW;
            float cellY = heightY / gridH;
            float halfW = widthZ * 0.5f;
            float halfH = heightY * 0.5f;

            for (int y = 0; y < gridH; y++)
            {
                float y0 = y * cellY - halfH;
                float y1 = (y + 1) * cellY - halfH;
                for (int x = 0; x < gridW; x++)
                {
                    if (!solid[x, y]) continue;
                    float z0 = x * cellZ - halfW;
                    float z1 = (x + 1) * cellZ - halfW;

                    // South edge (-Z normal)
                    if (x == 0 || !solid[x - 1, y])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z0), new Vector3(depthX, y0, z0),
                            new Vector3(0f, y1, z0), new Vector3(depthX, y1, z0),
                            Vector3.back);
                    }
                    // North edge (+Z normal)
                    if (x == gridW - 1 || !solid[x + 1, y])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z1), new Vector3(depthX, y0, z1),
                            new Vector3(0f, y1, z1), new Vector3(depthX, y1, z1),
                            Vector3.forward);
                    }
                    // Bottom edge (-Y normal)
                    if (y == 0 || !solid[x, y - 1])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z0), new Vector3(depthX, y0, z0),
                            new Vector3(0f, y0, z1), new Vector3(depthX, y0, z1),
                            Vector3.down);
                    }
                    // Top edge (+Y normal)
                    if (y == gridH - 1 || !solid[x, y + 1])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y1, z0), new Vector3(depthX, y1, z0),
                            new Vector3(0f, y1, z1), new Vector3(depthX, y1, z1),
                            Vector3.up);
                    }
                }
            }

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            Mesh mesh = existing ?? new Mesh();
            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(meshAssetPath);
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, meshAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }
            return mesh;
        }

        private static void WarmupCamera(Camera cam)
        {
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void RenderToFile(Camera cam, int w, int h, string filePath)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[WestAzucenasCornerBuilder] Saved screenshot: {filePath}");
        }

        #endregion
    }
}
