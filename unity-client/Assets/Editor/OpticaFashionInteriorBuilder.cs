using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Increment 5.23.0 — Dedicated, idempotent builder for Óptica Fashion's reconstructed
    /// hollow retail interior, segmented physically-accessible glass entrance (inward-swinging
    /// north/right leaf), and rear consultation/calibration room.
    ///
    /// Coordinate convention (standing on West Sidewalk at X=-10.5 facing West -X into store):
    ///   - Depth (East -> West): X = -12.02m (storefront glass) -> X = -19.20m (rear room wall)
    ///   - Width (South/Left -> North/Right): Z = -1.80m -> Z = +3.05m (4.85m, center Z = +0.625m)
    ///   - Height (Floor -> Ceiling): Y = 0.20m -> Y = 3.04m (2.84m clear interior height)
    ///
    /// Photographic vs. Functional Reconstruction Provenance:
    ///   - Photographic observations (Feb 2016 open-door, Jan 2019 frontal, horario zoom):
    ///     ceramic tile floor, charcoal mat, red fire extinguisher & electrical box on south entry wall,
    ///     long left/south light-grey laminate & glass display counter, right/north waiting area with
    ///     2 cobalt-blue tubular-frame chairs + low glass table + catalog + corner bin/plant,
    ///     deep north-wall dark eyewear shelving, pale gray-green rear partition wall with diamond mirror
    ///     and dark burgundy cabinet, rear-right doorway opening, white ceiling with parallel fluorescent
    ///     tubes, and inward-swinging north/right glass door leaf.
    ///   - Functional reconstruction (inferred, not directly photographed):
    ///     rear consultation/calibration room (X = -16.96..-19.20m) containing optical calibration
    ///     workbench, lensometer instrument, lens trial case, tool drawers, Snellen eye chart,
    ///     task lighting, and verified NPC standing anchor Optica_CalibrationStation (-17.85, 0.20, -0.50).
    /// </summary>
    public static class OpticaFashionInteriorBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        public const string InteriorRootName = "OpticaFashion_Interior_Root";
        public const string CalibrationAnchorName = "Optica_CalibrationStation";

        // Verified World-Space Store Bounds (Increment 5.23.0)
        public const float StorefrontX = -12.02f;
        public const float RearPartitionX = -16.88f; // spans -16.96 .. -16.80m
        public const float DeepRearWallX = -19.20f;
        public const float SouthWallZ = -1.80f;
        public const float NorthWallZ = 3.05f;
        public const float CenterZ = 0.625f;
        public const float FloorTopY = 0.20f;
        public const float CeilingBottomY = 3.04f;

        // Verified Standing Anchor for Future Increment 5.23.1 (Valeria Calibration Station)
        public static readonly Vector3 CalibrationStationWorldPos = new Vector3(-17.85f, FloorTopY, -0.50f);
        public static readonly Vector3 CalibrationStationEuler = new Vector3(0f, -90f, 0f); // Facing West (-X) toward workbench

        [MenuItem("MyVirtualCommunity/Increment 5.23.0/Rebuild Óptica Fashion Interior")]
        public static void RebuildOpticaFashionInterior()
        {
            Debug.Log("[OpticaFashionInteriorBuilder] Starting idempotent Óptica Fashion interior reconstruction...");
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[OpticaFashionInteriorBuilder] Failed to open scene '{ScenePath}'.");
                return;
            }

            GameObject blockArch = GameObject.Find("WestBlock_HistoricArchitecture");
            GameObject opticaRoot = GameObject.Find("Storefront_OpticaFashion");
            if (blockArch == null || opticaRoot == null)
            {
                Debug.LogError("[OpticaFashionInteriorBuilder] WestBlock_HistoricArchitecture or Storefront_OpticaFashion not found. Run WestSidewalkBlockBuilder first.");
                return;
            }

            // Ensure legacy monolithic Bay 4 objects (if any remain from pre-5.23.0 scene) are cleanly removed before rebuilding
            CleanupLegacyInlineOpticaObjects(blockArch.transform);

            // Rebuild interior & segmented accessible storefront inside our single clean root
            GameObject openSignQuad = BuildOpticaFashionInteriorAndStorefront(blockArch.transform, opticaRoot.transform);

            // Re-link openSignVisual on existing Optica_RollUpShutter_Dynamic if present
            Transform shutterTr = blockArch.transform.Find("Optica_RollUpShutter_Dynamic");
            if (shutterTr != null && openSignQuad != null)
            {
                var ctrl = shutterTr.GetComponent<StorefrontShutterController>();
                if (ctrl != null)
                {
                    ctrl.openSignVisual = openSignQuad;
                    EditorUtility.SetDirty(ctrl);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[OpticaFashionInteriorBuilder] Óptica Fashion interior & accessible entrance successfully saved.");
        }

        /// <summary>
        /// Idempotently builds the complete hollow Óptica Fashion interior, segmented accessible
        /// storefront entrance (with inward-swinging north/right door leaf), rear consultation/calibration
        /// room, and verified calibration standing anchor. Returns the glowing ABIERTO sign GameObject
        /// so the dynamic roll-up shutter can bind openSignVisual to it.
        /// </summary>
        public static GameObject BuildOpticaFashionInteriorAndStorefront(Transform blockArchTransform, Transform opticaRootTransform)
        {
            // Destroy existing interior root cleanly for strict idempotency (zero duplicate walls/lights/furniture/anchors)
            Transform existingRoot = blockArchTransform.Find(InteriorRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
            }
            CleanupLegacyInlineOpticaObjects(blockArchTransform);

            // Ensure transparent alpha texture imports cleanly
            EnsureAlphaTextureImport("Assets/Textures/Environment/T_West_Optica_Horario.png");

            // Load / create project-native materials
            Material floorTileMat = CreateOrGetTexMat("Assets/Settings/West_OpticaInteriorFloor.mat",
                "Assets/Textures/Environment/T_West_Optica_TileFloor.png", 0.34f, new Vector2(4.75f, 3.22f));
            Material interiorWallMat = CreateOrGetColorMat("Assets/Settings/West_OpticaInteriorWall.mat",
                new Color(0.91f, 0.90f, 0.87f), 0.10f, 0.0f);
            Material rearWallTintMat = CreateOrGetColorMat("Assets/Settings/West_OpticaRearWallTint.mat",
                new Color(0.85f, 0.89f, 0.85f), 0.10f, 0.0f); // Subtle pale gray-green tint observed in Feb 2016 photo
            Material ceilingWhiteMat = CreateOrGetColorMat("Assets/Settings/West_OpticaCeilingWhite.mat",
                new Color(0.96f, 0.97f, 0.97f), 0.08f, 0.0f);
            Material doormatMat = CreateOrGetColorMat("Assets/Settings/West_DoormatDark.mat",
                new Color(0.13f, 0.14f, 0.15f), 0.04f, 0.0f);
            Material bronzeFrameMat = CreateOrGetColorMat("Assets/Settings/West_OpticaBronzeFrame.mat",
                new Color(0.16f, 0.11f, 0.09f), 0.42f, 0.20f);
            Material blackMetalMat = CreateOrGetColorMat("Assets/Settings/West_OpticaBlackFrame.mat",
                new Color(0.11f, 0.12f, 0.13f), 0.38f, 0.25f);
            Material aluminumMat = CreateOrGetColorMat("Assets/Settings/West_Aluminum.mat",
                new Color(0.68f, 0.71f, 0.74f), 0.62f, 0.55f);
            Material counterLaminateMat = CreateOrGetColorMat("Assets/Settings/West_OpticaCounterLaminate.mat",
                new Color(0.64f, 0.69f, 0.73f), 0.26f, 0.04f); // Pale blue-gray laminate matching Feb 2016 photo
            Material whiteVelvetMat = CreateOrGetColorMat("Assets/Settings/West_OpticaDisplayVelvet.mat",
                new Color(0.93f, 0.93f, 0.92f), 0.12f, 0.0f);
            Material chairBlueMat = CreateOrGetColorMat("Assets/Settings/West_OpticaChairBlue.mat",
                new Color(0.16f, 0.40f, 0.72f), 0.24f, 0.0f);
            Material burgundyWoodMat = CreateOrGetColorMat("Assets/Settings/West_OpticaBurgundyWood.mat",
                new Color(0.28f, 0.10f, 0.10f), 0.34f, 0.05f);
            Material fireRedMat = CreateOrGetColorMat("Assets/Settings/West_OpticaFireRed.mat",
                new Color(0.76f, 0.10f, 0.10f), 0.48f, 0.18f);
            Material electricalGreyMat = CreateOrGetColorMat("Assets/Settings/West_OpticaElectricalGrey.mat",
                new Color(0.48f, 0.51f, 0.54f), 0.35f, 0.35f);
            Material plantGreenMat = CreateOrGetColorMat("Assets/Settings/West_OpticaPlantGreen.mat",
                new Color(0.18f, 0.42f, 0.20f), 0.14f, 0.0f);
            Material terracottaPotMat = CreateOrGetColorMat("Assets/Settings/West_OpticaTerracottaPot.mat",
                new Color(0.56f, 0.30f, 0.20f), 0.12f, 0.0f);
            Material mirrorSurfaceMat = CreateOrGetColorMat("Assets/Settings/West_OpticaMirrorGlass.mat",
                new Color(0.84f, 0.89f, 0.92f), 0.94f, 0.88f);
            Material calibBenchTopMat = CreateOrGetColorMat("Assets/Settings/West_OpticaCalibBenchTop.mat",
                new Color(0.86f, 0.88f, 0.90f), 0.38f, 0.05f);
            Material calibDeviceWhiteMat = CreateOrGetColorMat("Assets/Settings/West_OpticaCalibDevice.mat",
                new Color(0.89f, 0.91f, 0.93f), 0.46f, 0.10f);
            Material calibScreenMat = CreateOrGetEmissiveColorMat("Assets/Settings/West_OpticaCalibScreen.mat",
                new Color(0.12f, 0.58f, 0.68f), new Color(0.25f, 1.35f, 1.45f, 1.0f));
            Material ceilingTubeMat = CreateOrGetEmissiveColorMat("Assets/Settings/West_OpticaTubeLight.mat",
                new Color(0.98f, 0.99f, 1.0f), new Color(2.1f, 2.2f, 2.3f, 1.0f));
            Material showcaseWarmTubeMat = CreateOrGetEmissiveColorMat("Assets/Settings/West_OpticaShowcaseLight.mat",
                new Color(1.0f, 0.97f, 0.90f), new Color(1.45f, 1.38f, 1.22f, 1.0f));
            Material clearGlassMat = CreateOrGetTransparentGlassMat("Assets/Settings/West_StorefrontClearGlass.mat",
                new Color(0.26f, 0.36f, 0.42f, 0.26f), 0.92f);
            Material showcaseGlassMat = CreateOrGetTransparentGlassMat("Assets/Settings/West_OpticaShowcaseGlass.mat",
                new Color(0.76f, 0.86f, 0.90f, 0.22f), 0.95f);

            Material abiertoMat = CreateOrGetEmissiveTexMat("Assets/Settings/West_Optica_Abierto.mat",
                "Assets/Textures/Environment/T_West_Optica_Abierto.png", 1.85f);
            Material horarioMat = CreateOrGetTransparentTexMat("Assets/Settings/West_Optica_Horario.mat",
                "Assets/Textures/Environment/T_West_Optica_Horario.png");
            Material snellenMat = CreateOrGetTexMat("Assets/Settings/West_Optica_SnellenChart.mat",
                "Assets/Textures/Environment/T_West_Optica_SnellenChart.png", 0.12f, Vector2.one);
            Material catalogMat = CreateOrGetTexMat("Assets/Settings/West_Optica_Catalog.mat",
                "Assets/Textures/Environment/T_West_Optica_Catalog.png", 0.25f, Vector2.one);

            // Create clean root container
            GameObject root = new GameObject(InteriorRootName);
            root.transform.SetParent(blockArchTransform, false);
            root.transform.localPosition = Vector3.zero;

            // =========================================================================
            // 1. ARCHITECTURAL SHELL (Continuous Floor, Walls, Ceiling, Rear Partition)
            //    Total interior footprint: X = -19.20..-12.02m (7.18m deep),
            //    Z = -1.80..+3.05m (4.85m wide), Y = 0.20..3.04m (2.84m clear height)
            // =========================================================================
            GameObject shellGroup = CreateGroup("Shell", root.transform);

            // Continuous walkable ceramic tile floor spanning BOTH main store & rear consultation room
            // Seamless transition from West Sidewalk (Y=0.20m at X=-12.00m) all the way to X=-19.22m!
            CreateBox("Optica_InteriorFloor", shellGroup.transform,
                new Vector3(-15.60f, 0.10f, CenterZ), new Vector3(7.24f, 0.20f, 4.90f), floorTileMat, keepCollider: true);

            // Smooth white ceiling at Y = 3.04m clear height (ceiling slab Y = 3.04..3.12m)
            CreateBox("Optica_InteriorCeiling", shellGroup.transform,
                new Vector3(-15.66f, 3.08f, CenterZ), new Vector3(7.12f, 0.08f, 4.90f), ceilingWhiteMat, keepCollider: true);

            // Dedicated interior perimeter walls (all collidable so player/NPC stay cleanly inside the cavity!)
            // South wall (Photographic Left, Z = -1.78m)
            CreateBox("Optica_Int_SouthWall", shellGroup.transform,
                new Vector3(-15.61f, 1.62f, -1.78f), new Vector3(7.18f, 2.84f, 0.06f), interiorWallMat, keepCollider: true);
            // North wall (Photographic Right, Z = +3.03m)
            CreateBox("Optica_Int_NorthWall", shellGroup.transform,
                new Vector3(-15.61f, 1.62f, 3.03f), new Vector3(7.18f, 2.84f, 0.06f), interiorWallMat, keepCollider: true);
            // Deep West rear wall closing the consultation/calibration room (X = -19.17m)
            CreateBox("Optica_Int_DeepBackWall", shellGroup.transform,
                new Vector3(-19.17f, 1.62f, CenterZ), new Vector3(0.08f, 2.84f, 4.86f), interiorWallMat, keepCollider: true);

            // Subtle baseboards along interior walls for crisp retail finish (non-collidable thin trim)
            CreateBox("Optica_Baseboard_South", shellGroup.transform,
                new Vector3(-15.61f, 0.25f, -1.745f), new Vector3(7.16f, 0.10f, 0.015f), blackMetalMat);
            CreateBox("Optica_Baseboard_North", shellGroup.transform,
                new Vector3(-15.61f, 0.25f, 2.995f), new Vector3(7.16f, 0.10f, 0.015f), blackMetalMat);
            CreateBox("Optica_Baseboard_Back", shellGroup.transform,
                new Vector3(-19.125f, 0.25f, CenterZ), new Vector3(0.015f, 0.10f, 4.80f), blackMetalMat);

            // Exterior front spandrel sealing the 0.46m band between door frame top (Y=2.66) and upper floor (Y=3.12)
            // Sits behind the awning at X=-12.06 so exterior looks 100% solid while interior stays 2.84m tall!
            CreateBox("Optica_FrontHeaderSpandrel", shellGroup.transform,
                new Vector3(-12.06f, 2.90f, CenterZ), new Vector3(0.12f, 0.48f, 4.90f), interiorWallMat, keepCollider: true);

            // Rear Partition Wall separating Main Retail Showroom (X=-12.02..-16.80m) from Rear Consultation Room (X=-16.96..-19.20m)
            // Subtle pale gray-green tint matching Feb 2016 open-door photograph!
            // Leaves an OPEN, TRAVERSABLE doorway at Z = +1.35..+2.65m (1.30m structural / 1.22m clear width, Y = 0.20..2.38m)
            CreateBox("Optica_InteriorBackWall", shellGroup.transform,
                new Vector3(-16.88f, 1.62f, -0.225f), new Vector3(0.16f, 2.84f, 3.15f), rearWallTintMat, keepCollider: true); // Z = -1.80..+1.35
            CreateBox("Optica_RearPartition_North", shellGroup.transform,
                new Vector3(-16.88f, 1.62f, 2.85f), new Vector3(0.16f, 2.84f, 0.40f), rearWallTintMat, keepCollider: true); // Z = +2.65..+3.05
            CreateBox("Optica_RearPartition_Header", shellGroup.transform,
                new Vector3(-16.88f, 2.71f, 2.00f), new Vector3(0.16f, 0.66f, 1.30f), rearWallTintMat, keepCollider: true); // Y = 2.38..3.04 above opening
            // Dark bronze/espresso doorframe trim framing the rear consultation doorway (clear width 1.22m, clear height 2.14m)
            CreateBox("Optica_RearDoorFrame_South", shellGroup.transform,
                new Vector3(-16.88f, 1.29f, 1.37f), new Vector3(0.20f, 2.18f, 0.04f), bronzeFrameMat);
            CreateBox("Optica_RearDoorFrame_North", shellGroup.transform,
                new Vector3(-16.88f, 1.29f, 2.63f), new Vector3(0.20f, 2.18f, 0.04f), bronzeFrameMat);
            CreateBox("Optica_RearDoorFrame_Top", shellGroup.transform,
                new Vector3(-16.88f, 2.36f, 2.00f), new Vector3(0.20f, 0.04f, 1.30f), bronzeFrameMat);

            // =========================================================================
            // 2. SEGMENTED ACCESSIBLE GLASS STOREFRONT & INWARD-SWINGING NORTH DOOR
            //    Mullions at Z = -1.78, -0.66, +0.62, +1.90, +3.02:
            //      - South Sidelight (fixed, collidable): Z = -1.74 .. -0.70 (1.04m)
            //      - South/Left Door (closed, collidable): Z = -0.62 .. +0.58 (1.20m)
            //      - North/Right Door (OPEN INWARD +85 deg): hinged at Z = +1.86 -> 1.09m clear passage!
            //      - North Sidelight (fixed, collidable): Z = +1.94 .. +2.98 (1.04m)
            // =========================================================================
            GameObject facadeGroup = CreateGroup("StorefrontEntrance", root.transform);

            CreateBox("Optica_Frame_Top", facadeGroup.transform,
                new Vector3(StorefrontX, 2.66f, CenterZ), new Vector3(0.10f, 0.09f, 4.86f), bronzeFrameMat, keepCollider: true);
            // Bottom frame thresholds ONLY under fixed panels (keep open doorway threshold flush with sidewalk at Y=0.20!)
            CreateBox("Optica_Frame_Bottom_South", facadeGroup.transform,
                new Vector3(StorefrontX, 0.24f, -0.58f), new Vector3(0.10f, 0.08f, 2.44f), bronzeFrameMat, keepCollider: true);
            CreateBox("Optica_Frame_Bottom_North", facadeGroup.transform,
                new Vector3(StorefrontX, 0.24f, 2.46f), new Vector3(0.10f, 0.08f, 1.16f), bronzeFrameMat, keepCollider: true);
            // Flush metallic doorway saddle strip at sidewalk level (non-collidable, zero trip bump!)
            CreateBox("Optica_Doorway_ThresholdStrip", facadeGroup.transform,
                new Vector3(StorefrontX, 0.204f, 1.26f), new Vector3(0.12f, 0.008f, 1.26f), aluminumMat);

            float[] mullionZs = new float[] { -1.78f, -0.66f, 0.62f, 1.90f, 3.02f };
            for (int i = 0; i < mullionZs.Length; i++)
            {
                CreateBox($"Optica_Frame_Mullion_{i}", facadeGroup.transform,
                    new Vector3(StorefrontX, 1.45f, mullionZs[i]), new Vector3(0.10f, 2.46f, 0.08f), bronzeFrameMat, keepCollider: true);
            }
            CreateBox("Optica_Door_Transom", facadeGroup.transform,
                new Vector3(StorefrontX, 2.38f, CenterZ), new Vector3(0.08f, 0.07f, 4.80f), bronzeFrameMat, keepCollider: true);

            // Fixed collidable glass panels (South Sidelight, North Sidelight, and Top Transom above doors)
            CreateBox("Optica_Glass_SouthSidelight", facadeGroup.transform,
                new Vector3(StorefrontX, 1.45f, -1.22f), new Vector3(0.025f, 2.34f, 1.04f), clearGlassMat, keepCollider: true);
            CreateBox("Optica_Glass_NorthSidelight", facadeGroup.transform,
                new Vector3(StorefrontX, 1.45f, 2.46f), new Vector3(0.025f, 2.34f, 1.04f), clearGlassMat, keepCollider: true);
            CreateBox("Optica_Glass_TopTransom", facadeGroup.transform,
                new Vector3(StorefrontX, 2.515f, 0.62f), new Vector3(0.025f, 0.22f, 2.48f), clearGlassMat, keepCollider: true);

            // Closed Left/South Door Leaf (Z = -0.62..+0.58m, collidable) with horizontal silver push bar & glowing LED ABIERTO sign!
            GameObject southDoor = CreateGroup("Optica_DoorLeaf_South", facadeGroup.transform);
            CreateBox("Optica_DoorLeaf_South_Glass", southDoor.transform,
                new Vector3(StorefrontX, 1.31f, -0.02f), new Vector3(0.025f, 2.08f, 1.16f), clearGlassMat, keepCollider: true);
            CreateBox("Optica_DoorLeaf_South_BottomRail", southDoor.transform,
                new Vector3(StorefrontX, 0.30f, -0.02f), new Vector3(0.055f, 0.10f, 1.18f), bronzeFrameMat);
            CreateBox("Optica_DoorLeaf_South_TopRail", southDoor.transform,
                new Vector3(StorefrontX, 2.31f, -0.02f), new Vector3(0.055f, 0.08f, 1.18f), bronzeFrameMat);
            CreateBox("Optica_DoorPushBar_Left", southDoor.transform,
                new Vector3(StorefrontX + 0.06f, 1.15f, -0.02f), new Vector3(0.042f, 0.042f, 1.12f), aluminumMat);
            CreateBox("Optica_DoorPushBar_Left_In", southDoor.transform,
                new Vector3(StorefrontX - 0.06f, 1.15f, -0.02f), new Vector3(0.042f, 0.042f, 1.12f), aluminumMat);

            // Glowing LED "ABIERTO" sign mounted inside the closed Left/South Door Leaf
            GameObject abiertoQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            abiertoQuad.name = "Optica_AbiertoSign";
            abiertoQuad.transform.SetParent(southDoor.transform, false);
            abiertoQuad.transform.localPosition = new Vector3(StorefrontX + 0.035f, 1.92f, -0.02f);
            abiertoQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            abiertoQuad.transform.localScale = new Vector3(0.60f, 0.29f, 1.0f);
            SetMat(abiertoQuad, abiertoMat);
            UnityEngine.Object.DestroyImmediate(abiertoQuad.GetComponent<Collider>());

            // Inward-Swinging Right/North Door Leaf hinged at North jamb (X = -12.02, Y = 0.20, Z = +1.86)
            // Rotated +85 deg around +Y so it swings INWARD (-X) exactly as observed in the Feb 2016 photograph,
            // providing a generous ~1.09m clear walkable doorway opening (Z = +0.66..+1.75m)!
            GameObject northDoorPivot = new GameObject("Optica_DoorLeaf_North_Pivot");
            northDoorPivot.transform.SetParent(facadeGroup.transform, false);
            northDoorPivot.transform.localPosition = new Vector3(StorefrontX, FloorTopY, 1.86f);
            northDoorPivot.transform.localRotation = Quaternion.Euler(0f, 85f, 0f);

            // Hinge barrels on the north jamb (visible hardware)
            float[] hingeYs = new float[] { 0.40f, 1.18f, 1.96f };
            for (int h = 0; h < hingeYs.Length; h++)
            {
                var hinge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hinge.name = $"Optica_DoorHinge_North_{h}";
                hinge.transform.SetParent(facadeGroup.transform, false);
                hinge.transform.localPosition = new Vector3(StorefrontX - 0.03f, FloorTopY + hingeYs[h], 1.86f);
                hinge.transform.localScale = new Vector3(0.032f, 0.055f, 0.032f);
                SetMat(hinge, bronzeFrameMat);
                UnityEngine.Object.DestroyImmediate(hinge.GetComponent<Collider>());
            }

            // Local leaf geometry extends from local Z = 0 (hinge) to local Z = -1.20m (latch edge)
            CreateBox("Optica_DoorLeaf_North_StileHinge", northDoorPivot.transform,
                new Vector3(0f, 1.08f, -0.03f), new Vector3(0.055f, 2.12f, 0.06f), bronzeFrameMat);
            CreateBox("Optica_DoorLeaf_North_StileLatch", northDoorPivot.transform,
                new Vector3(0f, 1.08f, -1.17f), new Vector3(0.055f, 2.12f, 0.06f), bronzeFrameMat);
            CreateBox("Optica_DoorLeaf_North_TopRail", northDoorPivot.transform,
                new Vector3(0f, 2.10f, -0.60f), new Vector3(0.055f, 0.08f, 1.20f), bronzeFrameMat);
            CreateBox("Optica_DoorLeaf_North_BottomRail", northDoorPivot.transform,
                new Vector3(0f, 0.08f, -0.60f), new Vector3(0.055f, 0.12f, 1.20f), bronzeFrameMat);
            CreateBox("Optica_DoorLeaf_North_Glass", northDoorPivot.transform,
                new Vector3(0f, 1.10f, -0.60f), new Vector3(0.025f, 1.98f, 1.10f), clearGlassMat, keepCollider: true);
            CreateBox("Optica_DoorPushBar_Right", northDoorPivot.transform,
                new Vector3(0.055f, 0.95f, -0.60f), new Vector3(0.042f, 0.042f, 1.10f), aluminumMat);
            CreateBox("Optica_DoorPushBar_Right_In", northDoorPivot.transform,
                new Vector3(-0.055f, 0.95f, -0.60f), new Vector3(0.042f, 0.042f, 1.10f), aluminumMat);

            // Coral-red "Bienvenidos / HORARIO" + white "Empuje" vinyl decal mounted cleanly on the swinging right door glass
            GameObject horarioQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            horarioQuad.name = "Optica_HorarioSign";
            horarioQuad.transform.SetParent(northDoorPivot.transform, false);
            horarioQuad.transform.localPosition = new Vector3(0.024f, 1.44f, -0.68f);
            horarioQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            horarioQuad.transform.localScale = new Vector3(0.52f, 0.69f, 1.0f);
            SetMat(horarioQuad, horarioMat);
            UnityEngine.Object.DestroyImmediate(horarioQuad.GetComponent<Collider>());

            // =========================================================================
            // 3. ENTRANCE & SOUTH/LEFT FOREGROUND ZONE (Photographic Observations)
            // =========================================================================
            GameObject entranceZone = CreateGroup("Zone_Entrance", root.transform);

            // Long rectangular charcoal entrance mat immediately inside threshold (2.04m x 0.70m!)
            CreateBox("Optica_InteriorDoormat", entranceZone.transform,
                new Vector3(-12.44f, FloorTopY + 0.008f, 0.62f), new Vector3(0.70f, 0.016f, 2.04f), doormatMat);

            // Wall-mounted red fire extinguisher on interior South wall near entrance (Feb 2016 photo!)
            GameObject extRoot = CreateGroup("Optica_FireExtinguisher", entranceZone.transform);
            extRoot.transform.localPosition = new Vector3(-12.36f, 1.42f, -1.66f);
            var extCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            extCyl.name = "Extinguisher_Body";
            extCyl.transform.SetParent(extRoot.transform, false);
            extCyl.transform.localPosition = Vector3.zero;
            extCyl.transform.localScale = new Vector3(0.14f, 0.25f, 0.14f);
            SetMat(extCyl, fireRedMat);
            UnityEngine.Object.DestroyImmediate(extCyl.GetComponent<Collider>());
            CreateBox("Extinguisher_Bracket", extRoot.transform, new Vector3(0f, 0.02f, -0.075f), new Vector3(0.06f, 0.26f, 0.03f), blackMetalMat);
            CreateBox("Extinguisher_Valve", extRoot.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.07f, 0.08f, 0.07f), blackMetalMat);
            CreateBox("Extinguisher_Handle", extRoot.transform, new Vector3(0.05f, 0.30f, 0f), new Vector3(0.12f, 0.025f, 0.03f), blackMetalMat);
            CreateBox("Extinguisher_Hose", extRoot.transform, new Vector3(0.08f, 0.12f, 0.03f), new Vector3(0.025f, 0.30f, 0.025f), blackMetalMat);

            // Small gray metallic electrical breaker panel on interior South wall near entrance
            CreateBox("Optica_ElectricalPanel", entranceZone.transform,
                new Vector3(-12.74f, 1.32f, -1.72f), new Vector3(0.32f, 0.44f, 0.07f), electricalGreyMat);
            CreateBox("Optica_ElectricalPanel_Latch", entranceZone.transform,
                new Vector3(-12.62f, 1.32f, -1.68f), new Vector3(0.03f, 0.08f, 0.02f), blackMetalMat);

            // Compact glass/acrylic eyewear display shelving inside the South/Left storefront window
            GameObject frontRack = CreateGroup("Optica_FrontGlassDisplayRack", entranceZone.transform);
            frontRack.transform.localPosition = new Vector3(-12.56f, FloorTopY, -1.22f);
            CreateBox("FrontRack_Base", frontRack.transform, new Vector3(0f, 0.26f, 0f), new Vector3(0.42f, 0.52f, 0.72f), counterLaminateMat, keepCollider: true);
            float[] rackPostX = new float[] { -0.19f, 0.19f };
            float[] rackPostZ = new float[] { -0.34f, 0.34f };
            foreach (float px in rackPostX)
                foreach (float pz in rackPostZ)
                    CreateBox($"FrontRack_Post_{px:F2}_{pz:F2}", frontRack.transform, new Vector3(px, 1.15f, pz), new Vector3(0.025f, 1.26f, 0.025f), aluminumMat);
            for (int sh = 0; sh < 4; sh++)
            {
                float sy = 0.68f + sh * 0.34f;
                CreateBox($"FrontRack_GlassShelf_{sh}", frontRack.transform, new Vector3(0f, sy, 0f), new Vector3(0.38f, 0.016f, 0.68f), showcaseGlassMat);
                CreateEyewearRow(frontRack.transform, $"FrontRack_Glasses_{sh}", new Vector3(0f, sy + 0.032f, 0f), count: 3, spacing: 0.20f, alongZ: true, blackMetalMat, chairBlueMat);
            }

            // =========================================================================
            // 4. LONG LEFT/SOUTH GLASS DISPLAY COUNTER (Defining Retail Feature!)
            //    Extends along X = -13.20..-16.20m (3.00m long) at Z = -0.60m (width 0.78m),
            //    leaving a 0.76m staff aisle behind (Z=-1.75..-0.99m) and a generous ~1.9m
            //    walkable customer aisle on its North side (Z=-0.21..+1.85m)!
            // =========================================================================
            GameObject counterZone = CreateGroup("Zone_LeftDisplayCounter", root.transform);

            // Unified continuous counter body + recessed kickplate + subtle stepped front trim
            CreateBox("Optica_LongCounter_Kickplate", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.05f, -0.60f), new Vector3(2.96f, 0.10f, 0.70f), blackMetalMat);
            CreateBox("Optica_LongCounter_Base", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.36f, -0.60f), new Vector3(3.00f, 0.56f, 0.78f), counterLaminateMat, keepCollider: true);
            // Stepped front customer molding accent matching the geometric counter profile in Feb 2016 photo
            CreateBox("Optica_LongCounter_FrontStep", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.60f, -0.19f), new Vector3(3.04f, 0.08f, 0.05f), whiteVelvetMat);

            // Upper glass showcase vitrine (Y = 0.84..1.18m, 0.34m tall) with aluminum rails & internal lighting
            CreateBox("Optica_LongCounter_DisplayDeck", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.65f, -0.60f), new Vector3(2.96f, 0.025f, 0.74f), whiteVelvetMat);
            CreateBox("Optica_LongCounter_GlassTop", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.98f, -0.60f), new Vector3(3.00f, 0.02f, 0.78f), showcaseGlassMat, keepCollider: true);
            CreateBox("Optica_LongCounter_GlassFront", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.82f, -0.215f), new Vector3(2.98f, 0.32f, 0.015f), showcaseGlassMat);
            CreateBox("Optica_LongCounter_GlassEastEnd", counterZone.transform,
                new Vector3(-13.205f, FloorTopY + 0.82f, -0.60f), new Vector3(0.015f, 0.32f, 0.76f), showcaseGlassMat);
            CreateBox("Optica_LongCounter_GlassWestEnd", counterZone.transform,
                new Vector3(-16.195f, FloorTopY + 0.82f, -0.60f), new Vector3(0.015f, 0.32f, 0.76f), showcaseGlassMat);

            // Aluminum showcase frame rails & vertical corner mullions
            CreateBox("Optica_LongCounter_TopRailFront", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.98f, -0.215f), new Vector3(3.01f, 0.025f, 0.025f), aluminumMat);
            CreateBox("Optica_LongCounter_TopRailBack", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.98f, -0.985f), new Vector3(3.01f, 0.025f, 0.025f), aluminumMat);
            CreateBox("Optica_LongCounter_InnerLightBar", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.955f, -0.24f), new Vector3(2.88f, 0.018f, 0.022f), showcaseWarmTubeMat);
            float[] counterPostXs = new float[] { -13.21f, -14.20f, -15.20f, -16.19f };
            foreach (float cx in counterPostXs)
            {
                CreateBox($"Optica_CounterPost_F_{cx:F2}", counterZone.transform, new Vector3(cx, FloorTopY + 0.82f, -0.215f), new Vector3(0.025f, 0.33f, 0.025f), aluminumMat);
                CreateBox($"Optica_CounterPost_B_{cx:F2}", counterZone.transform, new Vector3(cx, FloorTopY + 0.82f, -0.985f), new Vector3(0.025f, 0.33f, 0.025f), aluminumMat);
            }

            // 6 organized eyewear trays with rows of eyeglasses visible inside the glass showcase
            for (int t = 0; t < 6; t++)
            {
                float tx = -13.48f - t * 0.49f;
                CreateBox($"Optica_CounterTray_{t}", counterZone.transform,
                    new Vector3(tx, FloorTopY + 0.675f, -0.58f), new Vector3(0.38f, 0.018f, 0.56f), electricalGreyMat);
                CreateEyewearRow(counterZone.transform, $"Optica_TrayGlassesA_{t}", new Vector3(tx - 0.08f, FloorTopY + 0.71f, -0.58f), count: 3, spacing: 0.17f, alongZ: true, blackMetalMat, bronzeFrameMat);
                CreateEyewearRow(counterZone.transform, $"Optica_TrayGlassesB_{t}", new Vector3(tx + 0.09f, FloorTopY + 0.71f, -0.58f), count: 3, spacing: 0.17f, alongZ: true, burgundyWoodMat, chairBlueMat);
            }

            // Countertop circular fitting mirror (preserved iconic retail detail!)
            var mirrorStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mirrorStand.name = "Optica_TableMirror_Stand";
            mirrorStand.transform.SetParent(counterZone.transform, false);
            mirrorStand.transform.localPosition = new Vector3(-13.60f, FloorTopY + 1.07f, -0.46f);
            mirrorStand.transform.localScale = new Vector3(0.055f, 0.09f, 0.055f);
            SetMat(mirrorStand, blackMetalMat);
            UnityEngine.Object.DestroyImmediate(mirrorStand.GetComponent<Collider>());
            var mirrorDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mirrorDisc.name = "Optica_TableMirror_Disc";
            mirrorDisc.transform.SetParent(counterZone.transform, false);
            mirrorDisc.transform.localPosition = new Vector3(-13.60f, FloorTopY + 1.24f, -0.46f);
            mirrorDisc.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
            mirrorDisc.transform.localScale = new Vector3(0.24f, 0.018f, 0.24f);
            SetMat(mirrorDisc, mirrorSurfaceMat);
            UnityEngine.Object.DestroyImmediate(mirrorDisc.GetComponent<Collider>());

            // Keep legacy compatibility name Optica_InteriorCounter aliased to the main counter group root
            CreateBox("Optica_InteriorCounter", counterZone.transform,
                new Vector3(-14.70f, FloorTopY + 0.36f, -0.60f), new Vector3(0.02f, 0.02f, 0.02f), counterLaminateMat);

            // South interior wall eyewear slatwall behind the counter (X = -13.40..-16.20m, Z = -1.73m)
            CreateBox("Optica_SouthWall_DisplayPanel", counterZone.transform,
                new Vector3(-14.80f, 1.62f, -1.73f), new Vector3(2.80f, 1.56f, 0.03f), whiteVelvetMat);
            for (int r = 0; r < 4; r++)
            {
                float ry = 1.04f + r * 0.34f;
                CreateBox($"Optica_SouthWall_Shelf_{r}", counterZone.transform,
                    new Vector3(-14.80f, ry, -1.66f), new Vector3(2.70f, 0.022f, 0.14f), aluminumMat);
                CreateEyewearRow(counterZone.transform, $"Optica_SouthWall_Glasses_{r}", new Vector3(-14.80f, ry + 0.035f, -1.65f), count: 9, spacing: 0.28f, alongZ: false, blackMetalMat, burgundyWoodMat);
            }

            // =========================================================================
            // 5. RIGHT/NORTH WAITING AREA NEAR ENTRANCE (Zoomed Photo 3!)
            //    Placed cleanly north of open door swing (Z = +2.25..+2.95m, X = -13.45..-15.05m):
            //    2 cobalt-blue chairs with dark tubular frames & armrests + glass side table + catalog
            // =========================================================================
            GameObject waitingZone = CreateGroup("Zone_RightWaitingArea", root.transform);

            CreateVisitorChair("Optica_WaitingChair_0", waitingZone.transform, new Vector3(-13.66f, FloorTopY, 2.58f), chairBlueMat, blackMetalMat);
            CreateVisitorChair("Optica_WaitingChair_1", waitingZone.transform, new Vector3(-14.86f, FloorTopY, 2.58f), chairBlueMat, blackMetalMat);

            // Low glass-topped side table between the two blue chairs + optical catalog/brochure on top
            GameObject sideTable = CreateGroup("Optica_WaitingSideTable", waitingZone.transform);
            sideTable.transform.localPosition = new Vector3(-14.26f, FloorTopY, 2.66f);
            CreateBox("SideTable_ColliderBox", sideTable.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.50f, 0.48f, 0.46f), showcaseGlassMat, keepCollider: true);
            UnityEngine.Object.DestroyImmediate(sideTable.transform.Find("SideTable_ColliderBox").GetComponent<MeshRenderer>());
            CreateBox("SideTable_GlassTop", sideTable.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.50f, 0.018f, 0.46f), showcaseGlassMat);
            CreateBox("SideTable_FrameTop", sideTable.transform, new Vector3(0f, 0.445f, 0f), new Vector3(0.48f, 0.022f, 0.44f), blackMetalMat);
            float[] tblX = new float[] { -0.22f, 0.22f };
            float[] tblZ = new float[] { -0.20f, 0.20f };
            foreach (float tx in tblX)
                foreach (float tz in tblZ)
                    CreateBox($"SideTable_Leg_{tx:F2}_{tz:F2}", sideTable.transform, new Vector3(tx, 0.225f, tz), new Vector3(0.024f, 0.45f, 0.024f), blackMetalMat);
            // Optical catalog/brochure resting slightly angled on the glass side table
            GameObject catalog = CreateBox("Optica_TableCatalog", sideTable.transform,
                new Vector3(0.02f, 0.476f, -0.02f), new Vector3(0.24f, 0.012f, 0.18f), catalogMat);
            catalog.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);

            // Compact dark corner container & modest potted plant inside front-right sidelight corner (non-blocking!)
            var cornerBin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cornerBin.name = "Optica_CornerBin";
            cornerBin.transform.SetParent(waitingZone.transform, false);
            cornerBin.transform.localPosition = new Vector3(-12.36f, FloorTopY + 0.24f, 2.76f);
            cornerBin.transform.localScale = new Vector3(0.28f, 0.24f, 0.28f);
            SetMat(cornerBin, blackMetalMat);
            UnityEngine.Object.DestroyImmediate(cornerBin.GetComponent<Collider>());

            GameObject plantRoot = CreateGroup("Optica_CornerPlant", waitingZone.transform);
            plantRoot.transform.localPosition = new Vector3(-12.74f, FloorTopY, 2.80f);
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "Plant_Pot";
            pot.transform.SetParent(plantRoot.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            pot.transform.localScale = new Vector3(0.20f, 0.11f, 0.20f);
            SetMat(pot, terracottaPotMat);
            UnityEngine.Object.DestroyImmediate(pot.GetComponent<Collider>());
            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Plant_Foliage";
            foliage.transform.SetParent(plantRoot.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            foliage.transform.localScale = new Vector3(0.28f, 0.24f, 0.28f);
            SetMat(foliage, plantGreenMat);
            UnityEngine.Object.DestroyImmediate(foliage.GetComponent<Collider>());

            // =========================================================================
            // 6. DEEP RIGHT/NORTH WALL EYEWEAR DISPLAY SHELVING (X = -15.35..-16.65m)
            // =========================================================================
            GameObject northShelfZone = CreateGroup("Zone_DeepRightEyewearDisplays", root.transform);
            GameObject northRack = CreateGroup("Optica_NorthWall_EyewearDisplay", northShelfZone.transform);
            northRack.transform.localPosition = new Vector3(-16.00f, FloorTopY, 2.91f);

            CreateBox("NorthRack_Backing", northRack.transform,
                new Vector3(0f, 1.24f, 0.08f), new Vector3(1.30f, 1.98f, 0.04f), blackMetalMat, keepCollider: true);
            CreateBox("NorthRack_SideE", northRack.transform, new Vector3(0.64f, 1.24f, -0.01f), new Vector3(0.03f, 1.98f, 0.20f), blackMetalMat);
            CreateBox("NorthRack_SideW", northRack.transform, new Vector3(-0.64f, 1.24f, -0.01f), new Vector3(0.03f, 1.98f, 0.20f), blackMetalMat);
            CreateBox("NorthRack_MidDiv", northRack.transform, new Vector3(0f, 1.24f, -0.01f), new Vector3(0.025f, 1.94f, 0.18f), blackMetalMat);
            CreateBox("NorthRack_HeaderLight", northRack.transform, new Vector3(0f, 2.21f, -0.04f), new Vector3(1.24f, 0.025f, 0.04f), showcaseWarmTubeMat);
            for (int r = 0; r < 6; r++)
            {
                float ry = 0.44f + r * 0.29f;
                CreateBox($"NorthRack_Shelf_{r}", northRack.transform,
                    new Vector3(0f, ry, -0.01f), new Vector3(1.24f, 0.022f, 0.18f), aluminumMat);
                CreateEyewearRow(northRack.transform, $"NorthRack_Glasses_{r}", new Vector3(0f, ry + 0.034f, -0.02f), count: 5, spacing: 0.24f, alongZ: false, burgundyWoodMat, bronzeFrameMat);
            }

            // =========================================================================
            // 7. REAR WALL COMPOSITION (Diamond Mirror + Dark Burgundy Cabinet)
            //    Mounted on pale gray-green rear partition wall at X = -16.80m, Z = +0.20m
            // =========================================================================
            GameObject rearFocalZone = CreateGroup("Zone_RearWallFocal", root.transform);

            // Signature Diamond-Shaped Decorative Mirror with Dark Frame (rotated 45 deg around X!)
            GameObject diamondRoot = CreateGroup("Optica_RearDiamondMirror", rearFocalZone.transform);
            diamondRoot.transform.localPosition = new Vector3(-16.775f, 1.78f, 0.18f);
            diamondRoot.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            CreateBox("DiamondMirror_Frame", diamondRoot.transform, Vector3.zero, new Vector3(0.045f, 0.74f, 0.74f), bronzeFrameMat);
            CreateBox("DiamondMirror_Glass", diamondRoot.transform, new Vector3(0.024f, 0f, 0f), new Vector3(0.012f, 0.62f, 0.62f), mirrorSurfaceMat);

            // Dark Wood / Burgundy Cabinet beneath the diamond mirror
            GameObject rearCab = CreateGroup("Optica_RearBurgundyCabinet", rearFocalZone.transform);
            rearCab.transform.localPosition = new Vector3(-16.56f, FloorTopY, 0.18f);
            CreateBox("RearCabinet_Body", rearCab.transform, new Vector3(0f, 0.40f, 0f), new Vector3(0.44f, 0.80f, 1.38f), burgundyWoodMat, keepCollider: true);
            CreateBox("RearCabinet_Top", rearCab.transform, new Vector3(0.01f, 0.815f, 0f), new Vector3(0.48f, 0.035f, 1.44f), bronzeFrameMat);
            CreateBox("RearCabinet_DoorSeam", rearCab.transform, new Vector3(0.225f, 0.40f, 0f), new Vector3(0.01f, 0.72f, 0.015f), blackMetalMat);
            CreateBox("RearCabinet_HandleS", rearCab.transform, new Vector3(0.235f, 0.48f, -0.08f), new Vector3(0.02f, 0.12f, 0.02f), aluminumMat);
            CreateBox("RearCabinet_HandleN", rearCab.transform, new Vector3(0.235f, 0.48f, 0.08f), new Vector3(0.02f, 0.12f, 0.02f), aluminumMat);

            // =========================================================================
            // 8. REAR CONSULTATION / CALIBRATION ROOM (Functional Reconstruction)
            //    X = -16.96..-19.20m (2.24m deep), Z = -1.80..+3.05m (4.85m wide)
            //    Accessible through the 1.22m-wide rear-right doorway (Z = +1.35..+2.65m)
            // =========================================================================
            GameObject rearRoomZone = CreateGroup("Zone_RearCalibrationRoom", root.transform);

            // Dedicated optical calibration workbench against the deep West wall (X = -19.12..-18.42m, Z = -1.45..+0.45m)
            GameObject workbench = CreateGroup("Optica_CalibrationWorkbench", rearRoomZone.transform);
            workbench.transform.localPosition = new Vector3(-18.77f, FloorTopY, -0.50f);

            // Benchtop + steel frame legs + under-bench tool storage drawers
            CreateBox("Workbench_Top", workbench.transform, new Vector3(0f, 0.88f, 0f), new Vector3(0.70f, 0.05f, 1.90f), calibBenchTopMat, keepCollider: true);
            CreateBox("Workbench_DrawerUnit", workbench.transform, new Vector3(-0.02f, 0.43f, -0.60f), new Vector3(0.62f, 0.84f, 0.62f), counterLaminateMat, keepCollider: true);
            for (int d = 0; d < 3; d++)
            {
                CreateBox($"Workbench_DrawerHandle_{d}", workbench.transform,
                    new Vector3(0.30f, 0.22f + d * 0.24f, -0.60f), new Vector3(0.02f, 0.025f, 0.18f), aluminumMat);
            }
            // Right bench support legs (keeping legroom open under the lensometer center!)
            CreateBox("Workbench_Leg_NE", workbench.transform, new Vector3(0.30f, 0.43f, 0.88f), new Vector3(0.05f, 0.86f, 0.05f), blackMetalMat);
            CreateBox("Workbench_Leg_NW", workbench.transform, new Vector3(-0.30f, 0.43f, 0.88f), new Vector3(0.05f, 0.86f, 0.05f), blackMetalMat);
            CreateBox("Workbench_BackPanel", workbench.transform, new Vector3(-0.32f, 0.43f, 0.25f), new Vector3(0.03f, 0.84f, 1.24f), electricalGreyMat);

            // Lens-measurement / optical calibration instrument (Lensometer / Auto-Refractor)
            GameObject lenso = CreateGroup("Optica_LensometerInstrument", workbench.transform);
            lenso.transform.localPosition = new Vector3(-0.02f, 0.905f, 0.00f);
            CreateBox("Lensometer_Base", lenso.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.30f, 0.08f, 0.26f), calibDeviceWhiteMat);
            CreateBox("Lensometer_Column", lenso.transform, new Vector3(-0.06f, 0.20f, 0f), new Vector3(0.14f, 0.28f, 0.16f), calibDeviceWhiteMat);
            CreateBox("Lensometer_ScreenBezel", lenso.transform, new Vector3(0.02f, 0.26f, 0f), new Vector3(0.04f, 0.14f, 0.15f), blackMetalMat);
            CreateBox("Lensometer_ScreenDisplay", lenso.transform, new Vector3(0.042f, 0.26f, 0f), new Vector3(0.01f, 0.11f, 0.12f), calibScreenMat);
            var lensClamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lensClamp.name = "Lensometer_LensRest";
            lensClamp.transform.SetParent(lenso.transform, false);
            lensClamp.transform.localPosition = new Vector3(0.08f, 0.13f, 0f);
            lensClamp.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
            SetMat(lensClamp, aluminumMat);
            UnityEngine.Object.DestroyImmediate(lensClamp.GetComponent<Collider>());

            // Organized lens trial case & eyewear components tray on north end of bench
            CreateBox("Optica_LensTrialCase", workbench.transform,
                new Vector3(0.02f, 0.935f, 0.56f), new Vector3(0.34f, 0.06f, 0.44f), burgundyWoodMat);
            CreateBox("Optica_LensTrialFoam", workbench.transform,
                new Vector3(0.02f, 0.968f, 0.56f), new Vector3(0.30f, 0.01f, 0.40f), blackMetalMat);
            for (int l = 0; l < 4; l++)
            {
                var lensDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lensDisk.name = $"TrialLens_{l}";
                lensDisk.transform.SetParent(workbench.transform, false);
                lensDisk.transform.localPosition = new Vector3(0.02f, 0.976f, 0.42f + l * 0.09f);
                lensDisk.transform.localScale = new Vector3(0.065f, 0.008f, 0.065f);
                SetMat(lensDisk, aluminumMat);
                UnityEngine.Object.DestroyImmediate(lensDisk.GetComponent<Collider>());
            }

            // Compact tool organizer & optical parts tray on south end of bench
            CreateBox("Optica_CalibPartsTray", workbench.transform,
                new Vector3(0.04f, 0.925f, -0.58f), new Vector3(0.28f, 0.04f, 0.36f), electricalGreyMat);
            CreateEyewearRow(workbench.transform, "Optica_CalibBenchFrames", new Vector3(0.04f, 0.96f, -0.58f), count: 2, spacing: 0.16f, alongZ: true, blackMetalMat, bronzeFrameMat);

            // Dedicated task lamp mounted on rear edge of workbench
            CreateBox("Optica_CalibTaskLamp_Post", workbench.transform, new Vector3(-0.26f, 1.18f, -0.34f), new Vector3(0.03f, 0.56f, 0.03f), blackMetalMat);
            CreateBox("Optica_CalibTaskLamp_Head", workbench.transform, new Vector3(-0.12f, 1.46f, -0.34f), new Vector3(0.30f, 0.035f, 0.14f), blackMetalMat);
            CreateBox("Optica_CalibTaskLamp_Glow", workbench.transform, new Vector3(-0.12f, 1.44f, -0.34f), new Vector3(0.26f, 0.012f, 0.10f), showcaseWarmTubeMat);

            // Snellen Visual Acuity Chart on deep rear West wall north of workbench (visible as you step through rear doorway!)
            GameObject snellenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            snellenQuad.name = "Optica_SnellenChart";
            snellenQuad.transform.SetParent(rearRoomZone.transform, false);
            snellenQuad.transform.localPosition = new Vector3(-19.12f, 1.72f, 1.55f);
            snellenQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            snellenQuad.transform.localScale = new Vector3(0.56f, 0.84f, 1.0f);
            SetMat(snellenQuad, snellenMat);
            UnityEngine.Object.DestroyImmediate(snellenQuad.GetComponent<Collider>());

            // Compact wall-mounted double-door lens storage cabinet on South wall of rear room (above floor, zero floor obstruction!)
            CreateBox("Optica_RearRoom_WallCabinet", rearRoomZone.transform,
                new Vector3(-18.08f, 1.88f, -1.60f), new Vector3(1.10f, 0.64f, 0.28f), whiteVelvetMat);
            CreateBox("Optica_RearRoom_WallCabinet_Seam", rearRoomZone.transform,
                new Vector3(-18.08f, 1.88f, -1.455f), new Vector3(0.015f, 0.60f, 0.012f), electricalGreyMat);
            CreateBox("Optica_RearRoom_WallCabinet_HandleW", rearRoomZone.transform,
                new Vector3(-18.14f, 1.76f, -1.448f), new Vector3(0.018f, 0.11f, 0.018f), aluminumMat);
            CreateBox("Optica_RearRoom_WallCabinet_HandleE", rearRoomZone.transform,
                new Vector3(-18.02f, 1.76f, -1.448f), new Vector3(0.018f, 0.11f, 0.018f), aluminumMat);

            // Verified NPC Standing Anchor for future Increment 5.23.1 (NOT yet wired to Valeria's autonomous movement!)
            GameObject calibAnchor = new GameObject(CalibrationAnchorName);
            calibAnchor.transform.SetParent(rearRoomZone.transform, false);
            calibAnchor.transform.position = CalibrationStationWorldPos;
            calibAnchor.transform.rotation = Quaternion.Euler(CalibrationStationEuler);

            // =========================================================================
            // 9. INTERIOR CEILING FLUORESCENT FIXTURES & LIGHTWEIGHT RETAIL LIGHTING
            //    Parallel linear cool-white fixtures matching Feb 2016 / Jan 2019 photos.
            //    Only 2 non-shadow-casting PointLights (LightRenderMode.Auto) for MX150 thermal safety!
            // =========================================================================
            GameObject lightZone = CreateGroup("Zone_InteriorLighting", root.transform);

            CreateFluorescentFixture("Optica_Fluorescent_MainFront", lightZone.transform,
                new Vector3(-13.65f, CeilingBottomY - 0.04f, CenterZ), lengthZ: 2.35f, aluminumMat, ceilingTubeMat);
            CreateFluorescentFixture("Optica_Fluorescent_MainRear", lightZone.transform,
                new Vector3(-15.65f, CeilingBottomY - 0.04f, CenterZ), lengthZ: 2.35f, aluminumMat, ceilingTubeMat);
            CreateFluorescentFixture("Optica_Fluorescent_RearRoom", lightZone.transform,
                new Vector3(-18.08f, CeilingBottomY - 0.04f, CenterZ), lengthZ: 2.15f, aluminumMat, ceilingTubeMat);

            CreateInteriorLight("Optica_MainStore_Light", lightZone.transform,
                new Vector3(-14.60f, 2.58f, CenterZ), new Color(0.95f, 0.98f, 1.0f), intensity: 1.75f, range: 6.8f);
            CreateInteriorLight("Optica_RearRoom_Light", lightZone.transform,
                new Vector3(-18.08f, 2.56f, 0.45f), new Color(0.96f, 0.98f, 1.0f), intensity: 1.30f, range: 4.8f);

            return abiertoQuad;
        }

        private static void CleanupLegacyInlineOpticaObjects(Transform parent)
        {
            string[] exactNames = new string[]
            {
                "Optica_InteriorFloor", "Optica_InteriorDoormat", "Optica_InteriorBackWall", "Optica_InteriorCeiling",
                "Optica_InteriorCounter", "Optica_CounterTop", "Optica_TableMirror_Stand", "Optica_TableMirror_Disc",
                "Optica_LeftDisplayTower", "Optica_LeftTower_ShelfAccent",
                "Optica_Frame_Top", "Optica_Frame_Bottom", "Optica_Door_Transom", "Optica_Door_MidRail",
                "Optica_DoorPushBar_Left", "Optica_DoorPushBar_Right", "Optica_DoorHandle_Left", "Optica_DoorHandle_Right",
                "Optica_GlassPane_Full", "Optica_AbiertoSign", "Optica_HorarioSign", "Optica_PosterLentes", "Optica_PinkNotice"
            };
            foreach (string n in exactNames)
            {
                Transform t = parent.Find(n);
                if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform c = parent.GetChild(i);
                if (c.name.StartsWith("Optica_DisplayShelf_") ||
                    c.name.StartsWith("Optica_FramesRow_") ||
                    c.name.StartsWith("Optica_WaitingChair") ||
                    c.name.StartsWith("Optica_Frame_Mullion_"))
                {
                    UnityEngine.Object.DestroyImmediate(c.gameObject);
                }
            }
        }

        private static void CreateVisitorChair(string name, Transform parent, Vector3 floorLocalPos, Material blueFabricMat, Material darkSteelMat)
        {
            GameObject chair = CreateGroup(name, parent);
            chair.transform.localPosition = floorLocalPos;

            // Invisible compact collision envelope (0.54 x 0.80 x 0.54m)
            GameObject col = CreateBox("Chair_Collider", chair.transform, new Vector3(0f, 0.40f, 0f), new Vector3(0.54f, 0.80f, 0.54f), darkSteelMat, keepCollider: true);
            UnityEngine.Object.DestroyImmediate(col.GetComponent<MeshRenderer>());

            // Padded cobalt-blue seat & contoured backrest (facing South -Z toward the main counter!)
            CreateBox("Chair_Seat", chair.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.46f, 0.075f, 0.46f), blueFabricMat);
            GameObject back = CreateBox("Chair_Back", chair.transform, new Vector3(0f, 0.72f, 0.21f), new Vector3(0.45f, 0.36f, 0.07f), blueFabricMat);
            back.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            // Dark tubular steel 4-leg frame + integrated side armrests (exact Photo 3 detail!)
            float[] lx = new float[] { -0.23f, 0.23f };
            float[] lz = new float[] { -0.21f, 0.21f };
            foreach (float x in lx)
            {
                foreach (float z in lz)
                {
                    CreateBox($"Chair_Leg_{x:F2}_{z:F2}", chair.transform, new Vector3(x, 0.22f, z), new Vector3(0.026f, 0.44f, 0.026f), darkSteelMat);
                }
                // Armrest top rail & upright supports
                CreateBox($"Chair_ArmTop_{x:F2}", chair.transform, new Vector3(x, 0.64f, 0.01f), new Vector3(0.032f, 0.028f, 0.44f), darkSteelMat);
                CreateBox($"Chair_ArmPostF_{x:F2}", chair.transform, new Vector3(x, 0.54f, -0.19f), new Vector3(0.024f, 0.20f, 0.024f), darkSteelMat);
                CreateBox($"Chair_ArmPostB_{x:F2}", chair.transform, new Vector3(x, 0.57f, 0.21f), new Vector3(0.024f, 0.28f, 0.024f), darkSteelMat);
            }
            CreateBox("Chair_CrossBar", chair.transform, new Vector3(0f, 0.40f, 0f), new Vector3(0.46f, 0.025f, 0.03f), darkSteelMat);
        }

        private static void CreateEyewearRow(Transform parent, string prefix, Vector3 centerPos, int count, float spacing, bool alongZ, Material frameMatA, Material frameMatB)
        {
            float startOffset = -((count - 1) * spacing) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float off = startOffset + i * spacing;
                Vector3 pos = alongZ ? centerPos + new Vector3(0f, 0f, off) : centerPos + new Vector3(off, 0f, 0f);
                Material mat = (i % 2 == 0) ? frameMatA : frameMatB;
                Vector3 lensScale = alongZ ? new Vector3(0.035f, 0.032f, 0.045f) : new Vector3(0.045f, 0.032f, 0.035f);
                Vector3 lensOff = alongZ ? new Vector3(0f, 0f, 0.034f) : new Vector3(0.034f, 0f, 0f);
                CreateBox($"{prefix}_{i}_L", parent, pos - lensOff, lensScale, mat);
                CreateBox($"{prefix}_{i}_R", parent, pos + lensOff, lensScale, mat);
                Vector3 bridgeScale = alongZ ? new Vector3(0.015f, 0.010f, 0.030f) : new Vector3(0.030f, 0.010f, 0.015f);
                CreateBox($"{prefix}_{i}_B", parent, pos + new Vector3(0f, 0.008f, 0f), bridgeScale, mat);
            }
        }

        private static void CreateFluorescentFixture(string name, Transform parent, Vector3 localPos, float lengthZ, Material housingMat, Material tubeMat)
        {
            GameObject fix = CreateGroup(name, parent);
            fix.transform.localPosition = localPos;
            CreateBox("Housing", fix.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.22f, 0.04f, lengthZ + 0.08f), housingMat);
            CreateBox("Tube_A", fix.transform, new Vector3(-0.05f, -0.005f, 0f), new Vector3(0.032f, 0.032f, lengthZ), tubeMat);
            CreateBox("Tube_B", fix.transform, new Vector3(0.05f, -0.005f, 0f), new Vector3(0.032f, 0.032f, lengthZ), tubeMat);
        }

        private static void CreateInteriorLight(string name, Transform parent, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
        }

        private static GameObject CreateGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            SetMat(go, mat);
            if (!keepCollider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        private static void EnsureAlphaTextureImport(string texPath)
        {
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
            {
                if (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp || ti.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.SaveAndReimport();
                }
            }
        }

        private static Material CreateOrGetColorMat(string path, Color color, float smoothness, float metallic)
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

        private static Material CreateOrGetEmissiveColorMat(string path, Color baseColor, Color emissionColor)
        {
            Material mat = CreateOrGetColorMat(path, baseColor, 0.40f, 0.0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTexMat(string matPath, string texPath, float smoothness, Vector2 tiling)
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
                mat.mainTextureScale = tiling;
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetEmissiveTexMat(string matPath, string texPath, float emissionIntensity)
        {
            Material mat = CreateOrGetTexMat(matPath, texPath, 0.35f, Vector2.one);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_EmissionMap", tex);
            }
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(emissionIntensity, emissionIntensity, emissionIntensity, 1.0f));
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTransparentTexMat(string matPath, string texPath)
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
            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_Blend", 0.0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0.0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTransparentGlassMat(string path, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_Blend", 0.0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0.0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.08f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
