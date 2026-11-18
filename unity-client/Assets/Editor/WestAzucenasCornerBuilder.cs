using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            Material gasYellowMat = CreateOrGetMat("Assets/Settings/West_Azucenas_GasYellow.mat", new Color(0.92f, 0.68f, 0.08f), 0.30f, 0.10f);
            Material orangeFasciaMat = CreateOrGetMat("Assets/Settings/West_Azucenas_OrangeFascia.mat", new Color(0.78f, 0.33f, 0.11f), 0.22f, 0.0f);

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

            // 3. NW Corner Annex Building (wraps cleanly around Av. Juárez & Calle Azucenas corner)
            BuildNorthWestCornerBuilding(root.transform, creamStuccoMat, weatheredCreamMat, terracottaMat,
                darkZocaloMat, beigeAwningMat, awningValanceMat, darkGlassMat, aluminumMat, shutterMat,
                gasYellowMat, orangeFasciaMat);

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
            Material terracottaMat, Material darkZocaloMat, Material beigeAwningMat, Material awningValanceMat,
            Material darkGlassMat, Material aluminumMat, Material shutterMat, Material gasYellowMat, Material orangeFasciaMat)
        {
            var bldgRoot = new GameObject("NWCorner_AnnexBuilding");
            bldgRoot.transform.SetParent(root);
            bldgRoot.transform.position = Vector3.zero;

            // Main 1-story white stucco corner annex: X = -26..-12, Z = -24.80..-20.50, Y = 0..4.70 (abuts Optica_MainBuilding)
            CreateBox("NWAnnex_MainVolume", bldgRoot.transform,
                new Vector3(-19.00f, 2.35f, -22.65f), new Vector3(14.00f, 4.70f, 4.30f), creamStuccoMat, addCollider: true);

            // Terracotta / warm-brown parapet cornice wrapping around East & South faces (signature corner trim from photo)
            CreateBox("NWAnnex_Cornice_East", bldgRoot.transform,
                new Vector3(-11.93f, 4.62f, -22.65f), new Vector3(0.22f, 0.26f, 4.44f), terracottaMat, addCollider: false);
            CreateBox("NWAnnex_Cornice_South", bldgRoot.transform,
                new Vector3(-18.96f, 4.62f, -24.87f), new Vector3(14.16f, 0.26f, 0.22f), terracottaMat, addCollider: false);

            // Lower Dark Zocalo along East & South corner
            CreateBox("NWAnnex_Zocalo_East", bldgRoot.transform,
                new Vector3(-11.96f, 0.34f, -22.65f), new Vector3(0.08f, 0.28f, 4.32f), darkZocaloMat, addCollider: false);
            CreateBox("NWAnnex_Zocalo_South", bldgRoot.transform,
                new Vector3(-19.00f, 0.34f, -24.84f), new Vector3(14.02f, 0.28f, 0.08f), darkZocaloMat, addCollider: false);

            // --- South Facade Facing Calle Azucenas (Z = -24.80m) ---
            // Dark wide rectangular horizontal storefront window + aluminum frame
            CreateBox("NWAnnex_South_WindowGlass", bldgRoot.transform,
                new Vector3(-18.00f, 1.80f, -24.83f), new Vector3(5.20f, 1.15f, 0.08f), darkGlassMat, addCollider: false);
            float[] winMullionX = { -20.58f, -18.85f, -17.15f, -15.42f };
            for (int i = 0; i < winMullionX.Length; i++)
            {
                CreateBox($"NWAnnex_South_Mullion_{i}", bldgRoot.transform,
                    new Vector3(winMullionX[i], 1.80f, -24.85f), new Vector3(0.07f, 1.18f, 0.08f), aluminumMat, addCollider: false);
            }
            CreateBox("NWAnnex_South_WindowSill", bldgRoot.transform,
                new Vector3(-18.00f, 1.20f, -24.86f), new Vector3(5.34f, 0.08f, 0.12f), aluminumMat, addCollider: false);

            // Beige Sloped Wedge Awning projecting South (-Z) over Azucenas North sidewalk
            CreateSouthWedgeAwning("NWAnnex_South_BeigeAwning", bldgRoot.transform,
                centerX: -18.00f, wallZ: -24.82f, topY: 3.18f, widthX: 5.50f, projectionZ: 1.15f, dropY: 0.58f,
                beigeAwningMat, awningValanceMat);

            // Corner shutter door near SE corner of South wall
            CreateBox("NWAnnex_South_CornerShutter", bldgRoot.transform,
                new Vector3(-13.70f, 1.28f, -24.83f), new Vector3(1.55f, 2.10f, 0.08f), shutterMat, addCollider: false);

            // Yellow corner gas pipe / meter riser on SE corner (signature detail in photo)
            CreateBox("NWAnnex_Corner_GasPipe", bldgRoot.transform,
                new Vector3(-12.06f, 1.05f, -24.88f), new Vector3(0.07f, 1.65f, 0.07f), gasYellowMat, addCollider: false);
            CreateBox("NWAnnex_Corner_GasBox", bldgRoot.transform,
                new Vector3(-12.22f, 1.45f, -24.88f), new Vector3(0.28f, 0.34f, 0.14f), gasYellowMat, addCollider: false);

            // --- East Facade Facing Av. Juárez (X = -12.00m) ---
            CreateBox("NWAnnex_East_Shutter", bldgRoot.transform,
                new Vector3(-11.96f, 1.32f, -22.55f), new Vector3(0.08f, 2.20f, 3.30f), shutterMat, addCollider: false);
            CreateBox("NWAnnex_East_OrangeSignBand", bldgRoot.transform,
                new Vector3(-11.94f, 3.45f, -22.65f), new Vector3(0.10f, 0.56f, 3.60f), orangeFasciaMat, addCollider: false);
            CreateEastWedgeAwning("NWAnnex_East_BeigeAwning", bldgRoot.transform,
                centerZ: -22.65f, wallX: -11.98f, topY: 3.08f, widthZ: 3.60f, projectionX: 1.10f, dropY: 0.54f,
                beigeAwningMat, awningValanceMat);

            // Secondary West Azucenas Neighbor Block (keeps looking West along Calle Azucenas grounded)
            CreateBox("NWAnnex_WestNeighbor_Block", bldgRoot.transform,
                new Vector3(-32.00f, 1.95f, -20.40f), new Vector3(12.00f, 3.90f, 8.80f), weatheredCreamMat, addCollider: true);
            CreateBox("NWAnnex_WestNeighbor_Cornice", bldgRoot.transform,
                new Vector3(-32.00f, 3.88f, -24.85f), new Vector3(12.05f, 0.18f, 0.14f), terracottaMat, addCollider: false);
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
            go.transform.SetParent(parent);
            go.transform.position = pos;
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
