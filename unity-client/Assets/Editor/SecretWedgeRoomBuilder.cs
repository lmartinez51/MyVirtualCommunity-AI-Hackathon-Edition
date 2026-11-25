using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Dedicated, idempotent builder for Building A's South Mystery-Door Bay & Secret Room:
    ///   1. Upper 2nd-Story Original Diagonal Structure (Y = 3.12..7.85m):
    ///      Preserves Building A's original 29.9° diagonal chamfer from (-12.00, -11.20) to
    ///      the building-junction vertex (-14.35, -12.55) with ArchWindow_1_SouthDiag, stepped
    ///      cornice, and the 2-story white lateral party wall receding West (-14.35..-24.00m).
    ///   2. Ground-Floor Street-Aligned "False Wall" (X = -12.00m, Z = -13.45..-11.20m, width 2.25m):
    ///      Aligned flush with Farmacia Juárez and México Mágico, framed by a warm taupe-gray
    ///      perimeter relief molding (Y = 0.20..3.12m), with a 0.67m left wall section (concrete
    ///      zócalo + electrical utility box) and a wide 1.32m double-leaf metal door on the right
    ///      equipped with the 3D 6-digit electronic keypad lock ("123456").
    ///   3. 4-Sided Interior "Cuarto Secreto" (matching the user's architectural sketch):
    ///      - Front (East) flat wall at X = -12.06m (Z = -13.40..-11.22m, width 2.18m)
    ///      - Left (South) perpendicular wall at Z = -13.40m (X = -12.06..-13.76m, length 1.70m)
    ///      - Rear-Left angled wall from (-13.76, -13.40) to the junction vertex (-14.35, -12.55) (1.04m)
    ///      - Right (North-East) diagonal wall from (-12.06, -11.22) to (-14.35, -12.55) (2.65m,
    ///        directly following Building A's 2-story diagonal chamfer!)
    /// </summary>
    public static class SecretWedgeRoomBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public const string RoomRootName = "BuildingA_SouthBay_SecretRoom_Root";
        public const string ChamferRootName = "BuildingA_SouthChamfer_Root";
        public const string LateralFlankRootName = "BuildingA_SouthLateralFlank_Root";
        public const string DoorRootName = "BuildingA_SouthBay_StorageDoor";
        public const string UpperWallName = "BuildingA_SouthBay_ContinuousWall";
        public const string RearBackingName = "BuildingA_SouthBay_RearBacking";

        // Upper 2nd-Story Original Diagonal Chamfer endpoints (Shared Building-Union Vertex -> Front corner at Farmacia Juárez)
        // Both principal buildings (1-story South Annex & 2-story Building A) meet at UpperChamferVertex (-14.60, -13.45)
        // and share the party wall line Z = -13.45m going West, so their structures are 100% joined ("pegadas") with zero gap.
        public static readonly Vector2 UpperChamferVertex = new Vector2(-14.60f, -13.45f);
        public static readonly Vector2 UpperChamferFrontNE = new Vector2(-12.00f, -11.20f);

        // Ground-Floor Street-Aligned False Wall bounds (X = -12.00m, Z = -13.45..-11.20m)
        public const float FrontFacadeX = -12.00f;
        public const float BaySouthZ = -13.48f;
        public const float BayNorthZ = -11.15f;
        public const float FloorTopY = 0.20f;
        public const float GrayFrameTopY = 3.10f;
        public const float CeilingBottomY = 2.96f;
        public const float DoorTopY = 2.24f;
        public const float InnerApexX = -13.66f;

        // 4-Sided Right-Trapezoid Interior Floor Corners (XZ):
        //   - CornerSE: Front-Left (-12.06, -13.40)
        //   - CornerNE: Front-Right (-12.06, -11.22)  [Wall 1 Front = 2.18m along X = -12.06m (0°-180°)]
        //   - CornerSW: Rear-Left (-13.66, -13.40)    [Wall 2 Left = 1.60m perpendicular along Z = -13.40m]
        //   - CornerNW: Rear-Right (-13.66, -12.60)   [Wall 3 Back/Fondo = 0.80m along X = -13.66m, PARALLEL (0°-180°) to Front entrance!
        //                                              Wall 4 Right Diagonal = 2.11m following Building A's diagonal chamfer]
        public static readonly Vector2 CornerSE = new Vector2(-12.06f, -13.40f);
        public static readonly Vector2 CornerNE = new Vector2(-12.06f, -11.22f);
        public static readonly Vector2 CornerSW = new Vector2(-13.66f, -13.40f);
        public static readonly Vector2 CornerNW = new Vector2(-13.66f, -12.60f);

        [MenuItem("MyVirtualCommunity/Rebuild Secret Wedge Room (Building A South)")]
        public static void RebuildSecretWedgeRoom()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            BuildIntoCurrentScene();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log("[SecretWedgeRoomBuilder] Built street-aligned false front, upper 2-story diagonal chamfer, 6-digit keypad door & 4-sided sketch secret room cleanly.");
        }

        public static void BuildIntoCurrentScene()
        {
            GameObject blockArch = GameObject.Find("WestBlock_HistoricArchitecture");
            if (blockArch == null)
            {
                throw new InvalidOperationException("[SecretWedgeRoomBuilder] Could not find 'WestBlock_HistoricArchitecture' in BorderCorridorScene.");
            }

            // Materials
            Material creamStuccoMat = LoadOrCreateColorMat("Assets/Settings/West_CreamStucco.mat", new Color(0.86f, 0.83f, 0.77f), 0.14f);
            Material weatheredCreamMat = LoadOrCreateColorMat("Assets/Settings/West_WeatheredCream.mat", new Color(0.84f, 0.81f, 0.76f), 0.06f);
            Material darkCorniceMat = LoadOrCreateColorMat("Assets/Settings/West_DarkCornice.mat", new Color(0.28f, 0.29f, 0.31f), 0.22f);
            Material stoneMasonryMat = LoadOrCreateColorMat("Assets/Settings/West_StoneMasonry.mat", new Color(0.62f, 0.60f, 0.57f), 0.16f);
            Material whiteFrameMat = LoadOrCreateColorMat("Assets/Settings/West_WhiteFrame.mat", new Color(0.92f, 0.92f, 0.90f), 0.24f);
            Material aluminumMat = LoadOrCreateColorMat("Assets/Settings/West_Aluminum.mat", new Color(0.75f, 0.77f, 0.80f), 0.65f, 0.75f);
            Material darkGlassMat = LoadOrCreateColorMat("Assets/Settings/West_DarkGlass.mat", new Color(0.10f, 0.13f, 0.16f), 0.82f);
            Material archWindowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/West_ArchWindow.mat") ?? darkGlassMat;

            // Warm taupe-gray perimeter relief molding & weathered door metal matching video captures
            Material taupeFrameMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_TaupeFrame.mat", new Color(0.55f, 0.51f, 0.47f), 0.14f, 0.0f);
            Material falseWallCreamMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_FalseWallCream.mat", new Color(0.89f, 0.83f, 0.75f), 0.10f, 0.0f);
            Material doorMetalMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_DoorMetal.mat", new Color(0.82f, 0.81f, 0.78f), 0.22f, 0.10f);

            Material bareFloorMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_ConcreteFloor.mat", new Color(0.36f, 0.37f, 0.36f), 0.12f, 0.0f);
            Material bareWallMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_RawPlaster.mat", new Color(0.58f, 0.56f, 0.52f), 0.10f, 0.0f);
            Material bareCeilingMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_Ceiling.mat", new Color(0.46f, 0.45f, 0.43f), 0.08f, 0.0f);
            Material baseboardMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_Baseboard.mat", new Color(0.24f, 0.25f, 0.26f), 0.16f, 0.0f);
            Material thresholdVeilMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretWedge_ThresholdVeil.mat", new Color(0.03f, 0.03f, 0.04f), new Color(0.16f, 0.13f, 0.06f) * 0.65f);
            Material keypadBodyMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_KeypadBody.mat", new Color(0.11f, 0.12f, 0.14f), 0.48f, 0.65f);
            Material keypadKeyMat = CreateOrGetColorMat("Assets/Settings/West_SecretWedge_KeypadKeys.mat", new Color(0.72f, 0.76f, 0.80f), 0.55f, 0.35f);
            Material keypadLedMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretWedge_KeypadLed.mat", new Color(0.95f, 0.12f, 0.12f), new Color(0.95f, 0.12f, 0.12f) * 2.4f);
            Material utilityBulbMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretWedge_UtilityBulb.mat", new Color(1.0f, 0.88f, 0.62f), new Color(1.0f, 0.84f, 0.54f) * 1.6f);

            // 1. Clean prior builder objects for 100% idempotency
            DestroyIfFound(RoomRootName);
            DestroyIfFound(ChamferRootName);
            DestroyIfFound(LateralFlankRootName);
            DestroyIfFound(DoorRootName);
            DestroyIfFound(UpperWallName);
            DestroyIfFound(RearBackingName);
            DestroyIfFound("ArchWindow_1_SouthDiag");
            DestroyIfFound("BuildingA_SouthBay_Zocalo_S");
            DestroyIfFound("BuildingA_SouthBay_Zocalo_N");

            // =========================================================================
            // 2. UPPER 2ND-STORY ORIGINAL DIAGONAL CHAMFER OF BUILDING A (Y = 3.10..7.85m)
            //    Runs along the exact right-hand diagonal of the user's sketch:
            //    from UpperChamferVertex (-14.35, -12.55) to UpperChamferFrontNE (-12.00, -11.20) [length 2.71m at 29.9°]
            // =========================================================================
            Vector2 upperDelta = UpperChamferFrontNE - UpperChamferVertex;
            float upperChamferLen = upperDelta.magnitude; // 2.7102m
            Vector2 upperDir = upperDelta / upperChamferLen;

            GameObject chamferRoot = new GameObject(ChamferRootName);
            chamferRoot.transform.SetParent(blockArch.transform, false);
            chamferRoot.transform.localPosition = new Vector3(UpperChamferVertex.x, 0f, UpperChamferVertex.y);
            // Local +Z runs from Vertex (0) to FrontNE (2.7102m); Local +X points outward SE; Local -X points inward NW
            chamferRoot.transform.localRotation = Quaternion.LookRotation(new Vector3(upperDir.x, 0f, upperDir.y), Vector3.up);

            float upperH = 7.80f - GrayFrameTopY; // 4.70m
            float upperMidY = GrayFrameTopY + upperH * 0.5f; // 5.45m
            CreateBox(UpperWallName, chamferRoot.transform,
                new Vector3(-0.22f, upperMidY, upperChamferLen * 0.5f),
                new Vector3(0.44f, upperH, upperChamferLen + 0.16f),
                creamStuccoMat, keepCollider: true);

            // Angled 3-Band Top Stepped Parapet Cornice on 2nd-story Chamfer (Y = 7.05..7.85m)
            CreateBox("Chamfer_Cornice_Band1", chamferRoot.transform,
                new Vector3(0.10f, 7.20f, upperChamferLen * 0.5f), new Vector3(0.28f, 0.26f, upperChamferLen + 0.24f), darkCorniceMat, keepCollider: false);
            CreateBox("Chamfer_Cornice_Band2", chamferRoot.transform,
                new Vector3(0.14f, 7.48f, upperChamferLen * 0.5f), new Vector3(0.36f, 0.24f, upperChamferLen + 0.28f), stoneMasonryMat, keepCollider: false);
            CreateBox("Chamfer_Cornice_Band3", chamferRoot.transform,
                new Vector3(0.18f, 7.74f, upperChamferLen * 0.5f), new Vector3(0.42f, 0.22f, upperChamferLen + 0.32f), darkCorniceMat, keepCollider: false);

            CreateBox("Chamfer_UpperReliefPanel", chamferRoot.transform,
                new Vector3(0.03f, 6.18f, upperChamferLen * 0.5f), new Vector3(0.06f, 0.92f, 1.98f), weatheredCreamMat, keepCollider: false);
            CreateBox("Chamfer_ArchSillCourse", chamferRoot.transform,
                new Vector3(0.07f, 3.85f, upperChamferLen * 0.5f), new Vector3(0.18f, 0.10f, upperChamferLen + 0.18f), darkCorniceMat, keepCollider: false);

            // Angled 1st Arched Lunette Window ("ArchWindow_1_SouthDiag") centered on upper 2nd-story chamfer
            GameObject arch1Root = new GameObject("ArchWindow_1_SouthDiag");
            arch1Root.transform.SetParent(chamferRoot.transform, false);
            arch1Root.transform.localPosition = new Vector3(0.03f, 4.64f, upperChamferLen * 0.5f);
            arch1Root.transform.localRotation = Quaternion.identity;
            GameObject arch1Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arch1Quad.name = "Arch_FaceQuad";
            arch1Quad.transform.SetParent(arch1Root.transform, false);
            arch1Quad.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            arch1Quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            arch1Quad.transform.localScale = new Vector3(1.42f, 1.36f, 1.0f);
            SetMaterial(arch1Quad, archWindowMat);
            UnityEngine.Object.DestroyImmediate(arch1Quad.GetComponent<Collider>());
            CreateBox("Arch_StoneSill", arch1Root.transform,
                new Vector3(0.04f, -0.66f, 0f), new Vector3(0.12f, 0.10f, 1.58f), darkCorniceMat, keepCollider: false);

            // =========================================================================
            // 3. 2-STORY WHITE LATERAL PARTY WALL OF BUILDING A (West from Vertex (-14.35, -12.55))
            //    Corresponds to the vertical top line in the user's sketch where both buildings meet!
            // =========================================================================
            GameObject flankRoot = new GameObject(LateralFlankRootName);
            flankRoot.transform.SetParent(blockArch.transform, false);
            flankRoot.transform.localPosition = Vector3.zero;

            float flankMinX = -24.00f;
            float flankMaxX = UpperChamferVertex.x; // -14.35f
            float flankLenX = flankMaxX - flankMinX; // 9.65m
            float flankMidX = 0.5f * (flankMinX + flankMaxX); // -19.175f
            float partyWallZ = UpperChamferVertex.y; // -12.55f

            CreateBox("BuildingA_SouthLateralFlankWall", flankRoot.transform,
                new Vector3(flankMidX, 5.45f, partyWallZ + 0.18f), new Vector3(flankLenX, 4.70f, 0.36f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_SouthLateralCornice_1", flankRoot.transform,
                new Vector3(flankMidX, 7.20f, partyWallZ - 0.04f), new Vector3(flankLenX + 0.10f, 0.26f, 0.24f), darkCorniceMat, keepCollider: false);
            CreateBox("BuildingA_SouthLateralCornice_2", flankRoot.transform,
                new Vector3(flankMidX, 7.48f, partyWallZ - 0.06f), new Vector3(flankLenX + 0.14f, 0.24f, 0.30f), stoneMasonryMat, keepCollider: false);
            CreateBox("BuildingA_SouthLateralCornice_3", flankRoot.transform,
                new Vector3(flankMidX, 7.74f, partyWallZ - 0.08f), new Vector3(flankLenX + 0.18f, 0.22f, 0.34f), darkCorniceMat, keepCollider: false);
            CreateBox("BuildingA_SouthLateralSillCourse", flankRoot.transform,
                new Vector3(flankMidX, 3.85f, partyWallZ - 0.02f), new Vector3(flankLenX + 0.08f, 0.10f, 0.16f), darkCorniceMat, keepCollider: false);

            float[] flankWinXs = new float[] { -15.65f, -17.85f, -20.05f, -22.25f };
            for (int w = 0; w < flankWinXs.Length; w++)
            {
                CreateBox($"BuildingA_SouthLateralWin_Frame_{w}", flankRoot.transform,
                    new Vector3(flankWinXs[w], 5.15f, partyWallZ - 0.01f), new Vector3(1.12f, 1.38f, 0.06f), weatheredCreamMat, keepCollider: false);
                CreateBox($"BuildingA_SouthLateralWin_Glass_{w}", flankRoot.transform,
                    new Vector3(flankWinXs[w], 5.15f, partyWallZ - 0.03f), new Vector3(0.96f, 1.22f, 0.04f), darkGlassMat, keepCollider: false);
                CreateBox($"BuildingA_SouthLateralWin_Sill_{w}", flankRoot.transform,
                    new Vector3(flankWinXs[w], 4.44f, partyWallZ - 0.05f), new Vector3(1.20f, 0.08f, 0.12f), darkCorniceMat, keepCollider: false);
            }

            // Solid rear masonry backing west of the meeting vertex (X = -24.00..-14.60m, Y = 0..3.10m, Z = -13.45..-11.20m)
            CreateBox(RearBackingName, blockArch.transform,
                new Vector3(-19.30f, GrayFrameTopY * 0.5f, -12.325f),
                new Vector3(9.40f, GrayFrameTopY, 2.25f),
                creamStuccoMat, keepCollider: true);

            // =========================================================================
            // 4. GROUND-FLOOR STREET-ALIGNED "FALSE WALL" AT X = -12.00m (Z = -13.45..-11.20m, width 2.25m)
            //    Closes off the triangular pocket at sidewalk level, matching Images 1 & 2 from the real video!
            // =========================================================================
            GameObject roomRoot = new GameObject(RoomRootName);
            roomRoot.transform.SetParent(blockArch.transform, false);
            roomRoot.transform.localPosition = Vector3.zero;

            float gfH = GrayFrameTopY - FloorTopY; // 2.90m (Y = 0.20..3.10m)
            float gfMidY = FloorTopY + gfH * 0.5f; // 1.65m

            // 4a. Warm Taupe-Gray Perimeter Relief Molding Frame around the entire 2.25m false front wall
            // Left vertical frame column (Z = -13.45..-13.33m, width 0.12m, right against México Mágico's pink side wing)
            CreateBox("Chamfer_GrayFrame_LeftCol", roomRoot.transform,
                new Vector3(-11.98f, gfMidY, -13.39f), new Vector3(0.10f, gfH, 0.12f), taupeFrameMat, keepCollider: true);
            // Right vertical frame column (Z = -11.32..-11.20m, width 0.12m, right next to PHARMACY pilaster)
            CreateBox("Chamfer_GrayFrame_RightCol", roomRoot.transform,
                new Vector3(-11.98f, gfMidY, -11.26f), new Vector3(0.10f, gfH, 0.12f), taupeFrameMat, keepCollider: true);
            // Top horizontal relief lintel beam (Y = 2.98..3.12m, Z = -13.45..-11.20m)
            CreateBox("Chamfer_GrayFrame_TopBeam", roomRoot.transform,
                new Vector3(-11.97f, GrayFrameTopY - 0.05f, -12.325f), new Vector3(0.12f, 0.14f, 2.25f), taupeFrameMat, keepCollider: true);

            // 4b. Left Wall Section (Z = -13.33..-12.66m, width 0.67m) + Concrete Gray Zócalo + Wall Electrical Utility Box (Image 1!)
            CreateBox("Chamfer_LeftWhiteWall", roomRoot.transform,
                new Vector3(-12.02f, gfMidY, -12.995f), new Vector3(0.08f, gfH, 0.67f), falseWallCreamMat, keepCollider: true);
            CreateBox("BuildingA_SouthBay_Zocalo_S", roomRoot.transform,
                new Vector3(-11.975f, FloorTopY + 0.16f, -12.995f), new Vector3(0.04f, 0.32f, 0.67f), stoneMasonryMat, keepCollider: false);

            // Small white/gray electrical utility register box on the left edge of the false wall (visible in Image 1 next to pink fin!)
            CreateBox("Chamfer_LeftUtilityBox_Housing", roomRoot.transform,
                new Vector3(-11.945f, 1.32f, -13.21f), new Vector3(0.07f, 0.36f, 0.22f), whiteFrameMat, keepCollider: false);
            CreateBox("Chamfer_LeftUtilityBox_Recess", roomRoot.transform,
                new Vector3(-11.925f, 1.32f, -13.21f), new Vector3(0.04f, 0.30f, 0.17f), baseboardMat, keepCollider: false);

            // 4c. Upper Wall Header Above the Double Door (Z = -12.66..-11.32m, width 1.34m, Y = 2.24..2.98m)
            float transomH = (GrayFrameTopY - 0.12f) - DoorTopY; // 0.74m
            float transomMidY = DoorTopY + transomH * 0.5f;     // 2.61m
            CreateBox("Chamfer_DoorUpperTransomWall", roomRoot.transform,
                new Vector3(-12.02f, transomMidY, -11.99f), new Vector3(0.08f, transomH, 1.34f), falseWallCreamMat, keepCollider: true);

            // =========================================================================
            // 5. 4-SIDED SEMI-TRIANGULAR SECRET ROOM INTERIOR (Matching User's Sketch!)
            //    - Wall 1 (Front East): X = -12.06m, Z = -13.40..-11.22m (2.18m wide)
            //    - Wall 2 (Left South): Z = -13.40m, X = -12.06..-13.76m (1.70m straight perpendicular)
            //    - Wall 3 (Rear-Left):  (-13.76, -13.40) -> (-14.35, -12.55) (1.04m angled back)
            //    - Wall 4 (Right NE):   (-12.06, -11.22) -> (-14.35, -12.55) (2.65m diagonal under 2nd-story chamfer)
            // =========================================================================
            float clearHeight = CeilingBottomY - FloorTopY; // 2.76m (Y = 0.20..2.96m)
            float wallMidY = FloorTopY + clearHeight * 0.5f; // 1.58m

            // Floor slab strictly inside X = -14.60..-12.00m, Z = -13.46..-11.18m (top at Y = 0.200m, zero overhang past X = -12.00m!)
            CreateBox("SecretRoom_Floor", roomRoot.transform,
                new Vector3(-13.30f, 0.10f, -12.32f), new Vector3(2.60f, 0.20f, 2.28f),
                bareFloorMat, keepCollider: true);

            // Ceiling slab & exterior roof cap over the triangular pocket between X = -12.00m false wall and 2nd-story diagonal chamfer!
            CreateBox("SecretRoom_Ceiling", roomRoot.transform,
                new Vector3(-13.30f, CeilingBottomY + 0.08f, -12.32f), new Vector3(2.60f, 0.16f, 2.28f),
                bareCeilingMat, keepCollider: true);

            // Wall 2: Left (South) Straight Perpendicular Wall (CornerSE (-12.06, -13.40) -> CornerSW (-13.66, -13.40), 1.60m long)
            CreateAngledWall("SecretRoom_SouthWall", roomRoot.transform,
                CornerSE, CornerSW, outwardSign: +1f, thickness: 0.10f,
                bottomY: FloorTopY, height: clearHeight, extendStart: 0.04f, extendEnd: 0.08f,
                bareWallMat, keepCollider: true);

            // Wall 3: Back (West / Fondo) Straight Wall PARALLEL (0°-180°) to the Front Entrance Wall (CornerSW (-13.66, -13.40) -> CornerNW (-13.66, -12.60), 0.80m wide)
            CreateAngledWall("SecretRoom_ApexWall", roomRoot.transform,
                CornerSW, CornerNW, outwardSign: +1f, thickness: 0.14f,
                bottomY: FloorTopY, height: clearHeight, extendStart: 0.08f, extendEnd: 0.10f,
                bareWallMat, keepCollider: true);

            // Wall 4: Right (North-East) Diagonal Wall directly under Building A's 2-story chamfer (CornerNE (-12.06, -11.22) -> CornerNW (-13.66, -12.60), 2.11m long)
            CreateAngledWall("SecretRoom_NorthWall", roomRoot.transform,
                CornerNE, CornerNW, outwardSign: -1f, thickness: 0.14f,
                bottomY: FloorTopY, height: clearHeight, extendStart: 0.04f, extendEnd: 0.12f,
                bareWallMat, keepCollider: true);

            // Interior concrete floor baseboards along all 3 interior walls
            CreateAngledWall("SecretRoom_Baseboard_South", roomRoot.transform,
                CornerSE, CornerSW, outwardSign: -1f, thickness: 0.016f,
                bottomY: FloorTopY, height: 0.09f, extendStart: 0.0f, extendEnd: 0.0f,
                baseboardMat, keepCollider: false);
            CreateAngledWall("SecretRoom_Baseboard_Apex", roomRoot.transform,
                CornerSW, CornerNW, outwardSign: -1f, thickness: 0.016f,
                bottomY: FloorTopY, height: 0.09f, extendStart: 0.0f, extendEnd: 0.0f,
                baseboardMat, keepCollider: false);
            CreateAngledWall("SecretRoom_Baseboard_North", roomRoot.transform,
                CornerNE, CornerNW, outwardSign: +1f, thickness: 0.016f,
                bottomY: FloorTopY, height: 0.09f, extendStart: 0.0f, extendEnd: 0.0f,
                baseboardMat, keepCollider: false);

            // Warm amber caged industrial vault pendant lamp
            GameObject lightHousing = CreateBox("SecretRoom_UtilityLightHousing", roomRoot.transform,
                new Vector3(-12.88f, CeilingBottomY - 0.04f, -12.50f), new Vector3(0.22f, 0.08f, 0.22f),
                darkCorniceMat, keepCollider: false);
            CreateBox("SecretRoom_UtilityLightDiffuser", lightHousing.transform,
                new Vector3(0f, -0.75f, 0f), new Vector3(0.55f, 0.85f, 0.55f),
                utilityBulbMat, keepCollider: false);
            CreateBox("SecretRoom_UtilityCageRing", lightHousing.transform,
                new Vector3(0f, -0.85f, 0f), new Vector3(0.78f, 0.18f, 0.78f),
                baseboardMat, keepCollider: false);

            GameObject lightGo = new GameObject("SecretRoom_UtilityPointLight");
            lightGo.transform.SetParent(roomRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(-12.88f, CeilingBottomY - 0.30f, -12.50f);
            Light ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(1.0f, 0.78f, 0.46f);
            ptLight.intensity = 0.78f;
            ptLight.range = 4.6f;
            ptLight.shadows = LightShadows.None;

            // =========================================================================
            // 5B. OPAQUE THRESHOLD VEIL ("VELO DEL UMBRAL") & BACKROOMS TRANSITION HOOK
            //     - Small real room stays completely empty & dusty (decoy if occupied by another player!).
            //     - When free, SecretRoom_ThresholdVeil blocks sight from the sidewalk; walking completely
            //       through the veil slams & relocks the exterior door and additively loads BackroomsLevel1Scene!
            // =========================================================================
            GameObject thresholdVeil = CreateBox("SecretRoom_ThresholdVeil", roomRoot.transform,
                new Vector3(-12.04f, FloorTopY + 1.03f, -11.99f), new Vector3(0.03f, 2.06f, 1.32f),
                thresholdVeilMat, keepCollider: false);
            Renderer veilRend = thresholdVeil.GetComponent<Renderer>();
            if (veilRend != null) veilRend.shadowCastingMode = ShadowCastingMode.Off;

            // =========================================================================
            // 6. HINGED DOUBLE METAL DOOR + 3D ELECTRONIC 6-DIGIT KEYPAD LOCK AT X = -11.98m
            //    Spans Z = -12.66..-11.32m (width 1.34m, center Z = -11.99m, two wide 0.63m leaves!)
            // =========================================================================
            GameObject doorRoot = new GameObject(DoorRootName);
            doorRoot.transform.SetParent(blockArch.transform, false);
            doorRoot.transform.localPosition = Vector3.zero;

            float doorH = DoorTopY - FloorTopY; // 2.04m
            float doorMidY = FloorTopY + doorH * 0.5f; // 1.22m
            float doorCenterZ = -11.99f;
            float doorSouthZ = -12.64f;
            float doorNorthZ = -11.34f;

            // Thin taupe-gray inner metal doorframe (cosmetic trim, keepCollider: false so full 1.34m opening is clear)
            CreateBox("StorageDoor_Frame_South", doorRoot.transform,
                new Vector3(-11.97f, doorMidY, doorSouthZ), new Vector3(0.08f, doorH, 0.04f), taupeFrameMat, keepCollider: false);
            CreateBox("StorageDoor_Frame_North", doorRoot.transform,
                new Vector3(-11.97f, doorMidY, doorNorthZ), new Vector3(0.08f, doorH, 0.04f), taupeFrameMat, keepCollider: false);
            CreateBox("StorageDoor_Frame_Top", doorRoot.transform,
                new Vector3(-11.97f, DoorTopY, doorCenterZ), new Vector3(0.08f, 0.05f, 1.34f), taupeFrameMat, keepCollider: false);

            // Single doorway blocker collider (enabled while locked/closed, disabled when open!)
            GameObject blockerGo = new GameObject("StorageDoor_ClosedBlocker");
            blockerGo.transform.SetParent(doorRoot.transform, false);
            blockerGo.transform.localPosition = new Vector3(-11.99f, doorMidY, doorCenterZ);
            BoxCollider blockerCol = blockerGo.AddComponent<BoxCollider>();
            blockerCol.size = new Vector3(0.10f, doorH, 1.30f);
            blockerCol.enabled = true;

            // Hinged South Leaf Pivot at (-11.97, 0.20, -12.62) — leaf extends +Z by 0.625m to center seam (-11.99)
            float leafW = 0.62f;
            float leafH = doorH - 0.02f; // 2.02m
            GameObject southPivot = new GameObject("StorageDoor_SouthPivot");
            southPivot.transform.SetParent(doorRoot.transform, false);
            southPivot.transform.localPosition = new Vector3(-11.97f, FloorTopY, -12.62f);

            CreateBox("StorageDoor_Leaf_South", southPivot.transform,
                new Vector3(-0.015f, leafH * 0.5f, leafW * 0.5f), new Vector3(0.036f, leafH, leafW), doorMetalMat, keepCollider: true);
            CreateBox("StorageDoor_South_Rail_Top", southPivot.transform,
                new Vector3(0.005f, 1.70f, leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_South_Rail_Mid", southPivot.transform,
                new Vector3(0.005f, 0.96f, leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_South_Rail_Bot", southPivot.transform,
                new Vector3(0.005f, 0.26f, leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_Hinge_SLow", southPivot.transform,
                new Vector3(0.012f, 0.38f, 0.01f), new Vector3(0.024f, 0.065f, 0.026f), darkCorniceMat, keepCollider: false);
            CreateBox("StorageDoor_Hinge_SHigh", southPivot.transform,
                new Vector3(0.012f, 1.62f, 0.01f), new Vector3(0.024f, 0.065f, 0.026f), darkCorniceMat, keepCollider: false);

            // Hinged North Leaf Pivot at (-11.97, 0.20, -11.36) — leaf extends -Z by 0.625m to center seam (-11.99)
            GameObject northPivot = new GameObject("StorageDoor_NorthPivot");
            northPivot.transform.SetParent(doorRoot.transform, false);
            northPivot.transform.localPosition = new Vector3(-11.97f, FloorTopY, -11.36f);

            CreateBox("StorageDoor_Leaf_North", northPivot.transform,
                new Vector3(-0.015f, leafH * 0.5f, -leafW * 0.5f), new Vector3(0.036f, leafH, leafW), doorMetalMat, keepCollider: true);
            CreateBox("StorageDoor_CenterSeam", northPivot.transform,
                new Vector3(0.004f, leafH * 0.5f, -leafW - 0.005f), new Vector3(0.016f, leafH, 0.018f), darkCorniceMat, keepCollider: false);
            CreateBox("StorageDoor_North_Rail_Top", northPivot.transform,
                new Vector3(0.005f, 1.70f, -leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_North_Rail_Mid", northPivot.transform,
                new Vector3(0.005f, 0.96f, -leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_North_Rail_Bot", northPivot.transform,
                new Vector3(0.005f, 0.26f, -leafW * 0.5f), new Vector3(0.012f, 0.024f, leafW - 0.02f), stoneMasonryMat, keepCollider: false);
            CreateBox("StorageDoor_Hinge_NLow", northPivot.transform,
                new Vector3(0.012f, 0.38f, -0.01f), new Vector3(0.024f, 0.065f, 0.026f), darkCorniceMat, keepCollider: false);
            CreateBox("StorageDoor_Hinge_NHigh", northPivot.transform,
                new Vector3(0.012f, 1.62f, -0.01f), new Vector3(0.024f, 0.065f, 0.026f), darkCorniceMat, keepCollider: false);

            // 7. 3D Electronic 6-Digit Keypad Lock mounted on the North Door Leaf beside the center seam
            GameObject keypadRoot = new GameObject("StorageDoor_KeypadLock");
            keypadRoot.transform.SetParent(northPivot.transform, false);
            keypadRoot.transform.localPosition = new Vector3(0.018f, 1.05f, -0.53f);

            GameObject keypadBody = CreateBox("Keypad_Housing", keypadRoot.transform,
                Vector3.zero, new Vector3(0.034f, 0.17f, 0.088f), keypadBodyMat, keepCollider: true);
            CreateBox("Keypad_Bezel", keypadRoot.transform,
                new Vector3(-0.012f, 0f, 0f), new Vector3(0.012f, 0.184f, 0.10f), aluminumMat, keepCollider: false);

            GameObject ledObj = CreateBox("Keypad_StatusLED", keypadRoot.transform,
                new Vector3(0.017f, 0.062f, 0f), new Vector3(0.008f, 0.016f, 0.056f), keypadLedMat, keepCollider: false);
            Renderer ledRenderer = ledObj.GetComponent<Renderer>();

            GameObject ledLightGo = new GameObject("Keypad_StatusLedLight");
            ledLightGo.transform.SetParent(keypadRoot.transform, false);
            ledLightGo.transform.localPosition = new Vector3(0.055f, 0.062f, 0f);
            Light ledLight = ledLightGo.AddComponent<Light>();
            ledLight.type = LightType.Point;
            ledLight.color = new Color(0.95f, 0.12f, 0.12f);
            ledLight.intensity = 0.03f;
            ledLight.range = 0.28f;
            ledLight.shadows = LightShadows.None;

            float[] btnColsZ = new float[] { -0.022f, 0.0f, 0.022f };
            float[] btnRowsY = new float[] { 0.032f, 0.008f, -0.016f, -0.040f };
            for (int r = 0; r < btnRowsY.Length; r++)
            {
                for (int c = 0; c < btnColsZ.Length; c++)
                {
                    CreateBox($"Keypad_Btn_{r}_{c}", keypadRoot.transform,
                        new Vector3(0.018f, btnRowsY[r], btnColsZ[c]),
                        new Vector3(0.007f, 0.015f, 0.015f),
                        keypadKeyMat, keepCollider: false);
                }
            }

            CreateBox("Keypad_LeverRose", keypadRoot.transform,
                new Vector3(0.010f, -0.115f, 0f), new Vector3(0.022f, 0.038f, 0.038f), aluminumMat, keepCollider: false);
            CreateBox("Keypad_LeverShaft", keypadRoot.transform,
                new Vector3(0.032f, -0.115f, 0f), new Vector3(0.030f, 0.016f, 0.016f), aluminumMat, keepCollider: false);
            CreateBox("Keypad_LeverGrip", keypadRoot.transform,
                new Vector3(0.045f, -0.115f, 0.042f), new Vector3(0.016f, 0.018f, 0.105f), aluminumMat, keepCollider: false);

            CreateBox("Keypad_InteriorExitHandle", northPivot.transform,
                new Vector3(-0.048f, 0.95f, -0.50f), new Vector3(0.026f, 0.14f, 0.022f), aluminumMat, keepCollider: false);

            // 8. Wire SecretKeypadDoorController
            SecretKeypadDoorController doorCtrl = doorRoot.AddComponent<SecretKeypadDoorController>();
            doorCtrl.doorId = "door:building-a-south-secret-wedge";
            doorCtrl.doorTitleEs = "Puerta misteriosa";
            doorCtrl.doorTitleEn = "Mysterious Door";
            doorCtrl.activeAccessCode = "581926";
            doorCtrl.requiredCodeLength = 6;
            doorCtrl.southLeafPivot = southPivot.transform;
            doorCtrl.northLeafPivot = northPivot.transform;
            doorCtrl.closedDoorBlockerCollider = blockerCol;
            doorCtrl.openAngleDegrees = 88f;
            doorCtrl.doorSwingSpeed = 2.8f;
            doorCtrl.keypadInteractionAnchor = new Vector3(-11.94f, 1.25f, -11.95f);
            doorCtrl.interactionRadius = 2.75f;
            doorCtrl.interiorThresholdX = -12.06f;
            doorCtrl.statusLedRenderer = ledRenderer;
            doorCtrl.statusLedLight = ledLight;

            if (keypadBody != null)
            {
                keypadBody.tag = "Untagged";
            }

            // 9. Attach SecretBackroomsTransitionController on roomRoot linking the outer street door & Threshold Veil
            SecretBackroomsTransitionController transitionCtrl = roomRoot.AddComponent<SecretBackroomsTransitionController>();
            transitionCtrl.outerStreetDoor = doorCtrl;
            transitionCtrl.thresholdVeilObject = thresholdVeil;
            transitionCtrl.veilTriggerX = -11.94f;
            transitionCtrl.roomMinZ = -13.50f;
            transitionCtrl.roomMaxZ = -11.15f;
            transitionCtrl.streetExitReturnPosition = new Vector3(-11.20f, 0.24f, -11.99f);
            transitionCtrl.streetExitReturnYaw = 90f;

            // 10. Keep FirstPersonPlayer serialized CharacterController shoulder clearance aligned with runtime Awake()
            GameObject playerGo = GameObject.Find("FirstPersonPlayer");
            if (playerGo != null)
            {
                CharacterController playerCc = playerGo.GetComponent<CharacterController>();
                if (playerCc != null)
                {
                    playerCc.radius = 0.28f;
                    playerCc.skinWidth = 0.04f;
                    EditorUtility.SetDirty(playerCc);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateAngledWall(
            string name,
            Transform parent,
            Vector2 startXZ,
            Vector2 endXZ,
            float outwardSign,
            float thickness,
            float bottomY,
            float height,
            float extendStart,
            float extendEnd,
            Material mat,
            bool keepCollider)
        {
            Vector2 delta = endXZ - startXZ;
            float baseLen = delta.magnitude;
            Vector2 dir = delta / Mathf.Max(0.0001f, baseLen);
            Vector2 perp = new Vector2(-dir.y, dir.x) * Mathf.Sign(outwardSign);

            Vector2 adjStart = startXZ - dir * extendStart;
            Vector2 adjEnd = endXZ + dir * extendEnd;
            float totalLen = Vector2.Distance(adjStart, adjEnd);
            Vector2 midXZ = 0.5f * (adjStart + adjEnd) + perp * (thickness * 0.5f);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(midXZ.x, bottomY + height * 0.5f, midXZ.y);
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y), Vector3.up);
            go.transform.localScale = new Vector3(thickness, height, totalLen);

            SetMaterial(go, mat);
            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            SetMaterial(go, mat);
            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        private static void DestroyIfFound(string name)
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all)
            {
                if (t != null && t.gameObject.scene.IsValid() && t.name == name)
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        private static Shader FindLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static Material LoadOrCreateColorMat(string assetPath, Color color, float smoothness, float metallic = 0f)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;
            return CreateOrGetColorMat(assetPath, color, smoothness, metallic);
        }

        private static Material CreateOrGetColorMat(string assetPath, Color color, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(FindLitShader());
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetEmissiveMat(string assetPath, Color baseColor, Color emissionColor)
        {
            Material mat = CreateOrGetColorMat(assetPath, baseColor, 0.35f, 0.0f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
