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
            doorCtrl.activeAccessCode = "123456";
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

        private static void BuildMysteryContainmentVaultInterior(
            Transform roomRoot,
            Material aluminumMat,
            Material darkCorniceMat,
            Material whiteFrameMat)
        {
            // Specialized mystery, horror & esoteric materials
            Material rackSteelMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_RackSteel.mat", new Color(0.13f, 0.14f, 0.16f), 0.42f, 0.72f);
            Material safeGunmetalMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_SafeGunmetal.mat", new Color(0.16f, 0.18f, 0.21f), 0.56f, 0.82f);
            Material antiqueBrassMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_AntiqueBrass.mat", new Color(0.78f, 0.61f, 0.24f), 0.66f, 0.85f);
            Material obsidianMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_Obsidian.mat", new Color(0.06f, 0.05f, 0.08f), 0.84f, 0.25f);
            Material crimsonVelvetMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_CrimsonVelvet.mat", new Color(0.46f, 0.06f, 0.10f), 0.15f, 0.0f);
            Material darkMahoganyMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_Mahogany.mat", new Color(0.28f, 0.14f, 0.08f), 0.32f, 0.0f);
            Material agedLeatherMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_AgedLeather.mat", new Color(0.34f, 0.21f, 0.13f), 0.22f, 0.0f);
            Material porcelainMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_Porcelain.mat", new Color(0.92f, 0.89f, 0.84f), 0.58f, 0.0f);
            Material saltWhiteMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_SaltWhite.mat", new Color(0.94f, 0.95f, 0.96f), 0.08f, 0.0f);
            Material corkMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_Cork.mat", new Color(0.58f, 0.42f, 0.25f), 0.12f, 0.0f);
            Material stickyYellowMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_StickyYellow.mat", new Color(0.98f, 0.88f, 0.26f), 0.18f, 0.0f);
            Material redThreadMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_RedThread.mat", new Color(0.88f, 0.12f, 0.14f), new Color(0.55f, 0.06f, 0.08f));
            Material goodGuyYellowMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_ToyBoxYellow.mat", new Color(0.94f, 0.74f, 0.12f), 0.36f, 0.0f);
            Material toyBoxBlueMat = CreateOrGetColorMat("Assets/Settings/West_SecretVault_ToyBoxBlue.mat", new Color(0.14f, 0.36f, 0.72f), 0.36f, 0.0f);
            Material vitrineGlassMat = LoadOrCreateColorMat("Assets/Settings/West_StorefrontClearGlass.mat", new Color(0.24f, 0.34f, 0.40f, 0.32f), 0.90f);

            // Emissive supernatural materials
            Material safeLedAmberMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_SafeLedAmber.mat", new Color(1.0f, 0.65f, 0.12f), new Color(1.0f, 0.62f, 0.10f) * 2.4f);
            Material crtStaticMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_CrtStatic.mat", new Color(0.68f, 0.86f, 1.0f), new Color(0.52f, 0.78f, 1.0f) * 1.9f);
            Material astrolabeCoreMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_AstrolabeCore.mat", new Color(0.32f, 0.92f, 1.0f), new Color(0.38f, 0.88f, 1.0f) * 2.8f);
            Material portalDormantMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_PortalDormant.mat", new Color(0.08f, 0.05f, 0.14f), new Color(0.26f, 0.10f, 0.48f) * 0.85f);
            Material portalActiveVortexMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_PortalActiveVortex.mat", new Color(0.22f, 0.78f, 1.0f), new Color(0.36f, 0.82f, 1.0f) * 3.1f);
            Material portalRuneVioletMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_PortalRuneViolet.mat", new Color(0.68f, 0.28f, 1.0f), new Color(0.68f, 0.28f, 1.0f) * 2.3f);
            Material turquoiseGlowMat = CreateOrGetEmissiveMat("Assets/Settings/West_SecretVault_TurquoiseGlow.mat", new Color(0.16f, 0.92f, 0.78f), new Color(0.16f, 0.92f, 0.78f) * 2.0f);

            GameObject vaultDecorRoot = new GameObject("SecretRoom_ContainmentVault_Root");
            vaultDecorRoot.transform.SetParent(roomRoot, false);
            vaultDecorRoot.transform.localPosition = Vector3.zero;

            // Attach master interactive vault/portal controller
            SecretRoomDimensionVaultController vaultCtrl = vaultDecorRoot.AddComponent<SecretRoomDimensionVaultController>();
            vaultCtrl.dormantMirrorMaterial = portalDormantMat;
            vaultCtrl.activeVortexMaterial = portalActiveVortexMat;

            // =========================================================================
            // A. BACK PARALLEL WALL (X = -13.66m, Z = -13.40..-12.60m, width 0.80m, 0°-180°):
            //    "EL UMBRAL DE LASSER — ESPEJO-PORTAL DIMENSIONAL & CUENCA OCTAGONAL"
            // =========================================================================
            GameObject portalRoot = new GameObject("Portal_DimensionalMirror_Root");
            portalRoot.transform.SetParent(vaultDecorRoot.transform, false);
            portalRoot.transform.localPosition = new Vector3(-13.61f, FloorTopY, -13.00f);

            // Protective white salt line across the floor directly in front of the Dimensional Portal
            CreateBox("Vault_Floor_SaltLine_Portal", portalRoot.transform,
                new Vector3(0.16f, 0.006f, 0f), new Vector3(0.035f, 0.012f, 0.74f), saltWhiteMat, keepCollider: false);

            // Stepped obsidian & brass threshold plinth (Y = 0.20..0.30m)
            CreateBox("Portal_ThresholdPlinth_Base", portalRoot.transform,
                new Vector3(0.03f, 0.04f, 0f), new Vector3(0.12f, 0.08f, 0.76f), obsidianMat, keepCollider: false);
            CreateBox("Portal_ThresholdPlinth_BrassTrim", portalRoot.transform,
                new Vector3(0.04f, 0.09f, 0f), new Vector3(0.10f, 0.024f, 0.72f), antiqueBrassMat, keepCollider: false);

            // Left (South) & Right (North) carved obsidian portal pillars with antique brass rune bands
            CreateBox("Portal_Frame_LeftPillar", portalRoot.transform,
                new Vector3(0.03f, 1.16f, -0.33f), new Vector3(0.10f, 2.14f, 0.09f), obsidianMat, keepCollider: false);
            CreateBox("Portal_Frame_RightPillar", portalRoot.transform,
                new Vector3(0.03f, 1.16f, 0.33f), new Vector3(0.10f, 2.14f, 0.09f), obsidianMat, keepCollider: false);

            float[] pillarRingYs = new float[] { 0.45f, 0.92f, 1.56f, 2.02f };
            for (int i = 0; i < pillarRingYs.Length; i++)
            {
                CreateBox($"Portal_LeftPillar_BrassBand_{i}", portalRoot.transform,
                    new Vector3(0.04f, pillarRingYs[i], -0.33f), new Vector3(0.115f, 0.035f, 0.105f), antiqueBrassMat, keepCollider: false);
                CreateBox($"Portal_RightPillar_BrassBand_{i}", portalRoot.transform,
                    new Vector3(0.04f, pillarRingYs[i], 0.33f), new Vector3(0.115f, 0.035f, 0.105f), antiqueBrassMat, keepCollider: false);
            }

            // Ornate Esoteric Top Arch & Keystone Pediment (Y = 2.22..2.46m)
            CreateBox("Portal_Frame_TopArchBeam", portalRoot.transform,
                new Vector3(0.04f, 2.26f, 0f), new Vector3(0.12f, 0.10f, 0.78f), obsidianMat, keepCollider: false);
            CreateBox("Portal_Frame_TopBrassCornice", portalRoot.transform,
                new Vector3(0.05f, 2.34f, 0f), new Vector3(0.13f, 0.045f, 0.80f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_Frame_CrownKeystone", portalRoot.transform,
                new Vector3(0.06f, 2.32f, 0f), new Vector3(0.14f, 0.16f, 0.16f), obsidianMat, keepCollider: false);
            CreateBox("Portal_Frame_CrownRuneGem", portalRoot.transform,
                new Vector3(0.12f, 2.32f, 0f), new Vector3(0.03f, 0.08f, 0.08f), portalRuneVioletMat, keepCollider: false);

            // Inner brass bezel surrounding the mirror glass
            CreateBox("Portal_InnerBrassBezel_L", portalRoot.transform,
                new Vector3(0.025f, 1.16f, -0.275f), new Vector3(0.06f, 2.04f, 0.022f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_InnerBrassBezel_R", portalRoot.transform,
                new Vector3(0.025f, 1.16f, 0.275f), new Vector3(0.06f, 2.04f, 0.022f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_InnerBrassBezel_T", portalRoot.transform,
                new Vector3(0.025f, 2.18f, 0f), new Vector3(0.06f, 0.025f, 0.56f), antiqueBrassMat, keepCollider: false);

            // The Dark Lasser Mirror / Dimensional Rift Surface (0.53m wide x 2.04m tall)
            GameObject mirrorSurface = CreateBox("Portal_MirrorSurface", portalRoot.transform,
                new Vector3(0.01f, 1.15f, 0f), new Vector3(0.025f, 2.04f, 0.53f), portalDormantMat, keepCollider: false);
            BoxCollider mirrorTrig = mirrorSurface.AddComponent<BoxCollider>();
            mirrorTrig.isTrigger = true;
            vaultCtrl.portalMirrorRenderer = mirrorSurface.GetComponent<Renderer>();
            AttachArtifactDescriptor(
                mirrorSurface,
                "portal:umbral-de-lasser",
                "El Umbral de Lasser (Espejo-Portal Dimensional)",
                "The Lasser Threshold (Dimensional Mirror-Portal)",
                "Espejo esotérico de cuerpo entero empotrado en el muro del fondo. Posee una cuenca octagonal en su pilar derecho que aguarda 'El Ojo del Umbral' para liberar sus cadenas y abrir el paso hacia otro mundo.",
                "Full-length esoteric mirror recessed into the back wall. An octagonal socket on its right pillar awaits 'The Eye of the Threshold' to release its chains and open a passage to another world.",
                new Color(0.55f, 0.82f, 1.0f));

            // Active Multiverse Vortex Rings (visible when unlocked with the Astrolabe Keystone!)
            GameObject vortexGroup = new GameObject("Portal_ActiveVortexGroup");
            vortexGroup.transform.SetParent(portalRoot.transform, false);
            vortexGroup.transform.localPosition = new Vector3(0.032f, 1.18f, 0f);
            vortexGroup.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreateBox("Portal_VortexRing_Outer", vortexGroup.transform,
                Vector3.zero, new Vector3(0.46f, 0.46f, 0.012f), portalRuneVioletMat, keepCollider: false);
            GameObject midVortex = CreateBox("Portal_VortexRing_Mid", vortexGroup.transform,
                new Vector3(0f, 0f, 0.008f), new Vector3(0.34f, 0.34f, 0.012f), antiqueBrassMat, keepCollider: false);
            midVortex.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateBox("Portal_VortexRing_Inner", vortexGroup.transform,
                new Vector3(0f, 0f, 0.016f), new Vector3(0.20f, 0.20f, 0.014f), astrolabeCoreMat, keepCollider: false);
            vortexGroup.SetActive(false);
            vaultCtrl.portalActiveVortexGroup = vortexGroup;

            // Crossed Heavy Containment Chains sealing the mirror while dormant
            GameObject chainsGroup = new GameObject("Portal_ChainsGroup");
            chainsGroup.transform.SetParent(portalRoot.transform, false);
            chainsGroup.transform.localPosition = new Vector3(0.055f, 1.15f, 0f);
            GameObject chain1 = CreateBox("Portal_SealChain_Diag1", chainsGroup.transform,
                Vector3.zero, new Vector3(0.025f, 1.56f, 0.038f), rackSteelMat, keepCollider: false);
            chain1.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            GameObject chain2 = CreateBox("Portal_SealChain_Diag2", chainsGroup.transform,
                Vector3.zero, new Vector3(0.025f, 1.56f, 0.038f), rackSteelMat, keepCollider: false);
            chain2.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            CreateBox("Portal_SealPadlock_Body", chainsGroup.transform,
                new Vector3(0.018f, -0.02f, 0f), new Vector3(0.035f, 0.11f, 0.085f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_SealPadlock_Rune", chainsGroup.transform,
                new Vector3(0.036f, -0.02f, 0f), new Vector3(0.01f, 0.05f, 0.05f), redThreadMat, keepCollider: false);
            vaultCtrl.portalChainsGroup = chainsGroup;

            // Portal Rift Point Light
            GameObject riftLightGo = new GameObject("Portal_DimensionalRiftLight");
            riftLightGo.transform.SetParent(portalRoot.transform, false);
            riftLightGo.transform.localPosition = new Vector3(0.22f, 1.25f, 0f);
            Light riftLight = riftLightGo.AddComponent<Light>();
            riftLight.type = LightType.Point;
            riftLight.color = new Color(0.58f, 0.24f, 0.92f);
            riftLight.intensity = 0.30f;
            riftLight.range = 2.4f;
            riftLight.shadows = LightShadows.None;
            vaultCtrl.portalRiftLight = riftLight;

            // =========================================================================
            // A2. THE DOCTOR STRANGE-INSPIRED OCTAGONAL KEYSTONE SOCKET ("CUENCA OCTAGONAL DEL UMBRAL")
            //     Mounted on the right portal pillar at chest level (X = -13.53m, Y = 1.24m, Z = -12.67m)
            // =========================================================================
            GameObject socketRoot = new GameObject("Portal_KeystoneSocket_Root");
            socketRoot.transform.SetParent(portalRoot.transform, false);
            socketRoot.transform.localPosition = new Vector3(0.085f, 1.04f, 0.33f);

            // Octagonal housing altar on the right pillar (2 overlapping 45° rotated brass squares + dark octagonal recess)
            CreateBox("Socket_AltarConsole", socketRoot.transform,
                new Vector3(-0.02f, 0f, 0f), new Vector3(0.06f, 0.22f, 0.19f), obsidianMat, keepCollider: false);
            CreateBox("Socket_OctBezel_A", socketRoot.transform,
                new Vector3(0.012f, 0f, 0f), new Vector3(0.022f, 0.155f, 0.155f), antiqueBrassMat, keepCollider: false);
            GameObject octBezelB = CreateBox("Socket_OctBezel_B", socketRoot.transform,
                new Vector3(0.012f, 0f, 0f), new Vector3(0.022f, 0.155f, 0.155f), antiqueBrassMat, keepCollider: false);
            octBezelB.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

            // Empty dark octagonal cavity waiting for the Astrolabe Keystone
            CreateBox("Socket_EmptyRecess_A", socketRoot.transform,
                new Vector3(0.020f, 0f, 0f), new Vector3(0.016f, 0.118f, 0.118f), obsidianMat, keepCollider: false);
            GameObject recessB = CreateBox("Socket_EmptyRecess_B", socketRoot.transform,
                new Vector3(0.020f, 0f, 0f), new Vector3(0.016f, 0.118f, 0.118f), obsidianMat, keepCollider: false);
            recessB.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            CreateBox("Socket_CenterContactPin", socketRoot.transform,
                new Vector3(0.026f, 0f, 0f), new Vector3(0.012f, 0.028f, 0.028f), portalRuneVioletMat, keepCollider: false);

            // Trigger collider for clicking / aiming at the Octagonal Socket
            GameObject socketTriggerGo = new GameObject("Portal_KeystoneSocket_Trigger");
            socketTriggerGo.transform.SetParent(socketRoot.transform, false);
            socketTriggerGo.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            BoxCollider socketCol = socketTriggerGo.AddComponent<BoxCollider>();
            socketCol.size = new Vector3(0.12f, 0.24f, 0.22f);
            socketCol.isTrigger = true;
            AttachArtifactDescriptor(
                socketTriggerGo,
                "portal:cuenca-octagonal",
                "Cuenca Octagonal del Umbral (Receptáculo Astral)",
                "Octagonal Threshold Socket (Astral Receptacle)",
                "Un hueco octagonal tallado en latón y obsidiana con 8 contactos astrales. Su geometría coincide exactamente con 'El Ojo del Umbral' resguardado en la Caja Fuerte Nivel Ω.",
                "An octagonal recess carved in brass and obsidian with 8 astral contacts. Its geometry matches 'The Eye of the Threshold' locked inside the Level-Ω Vault Safe.",
                new Color(1.0f, 0.78f, 0.32f));

            // The 3D Astrolabe Keystone locked inside the Socket (hidden until inserted by player!)
            GameObject socketAstrolabe = CreateAstrolabeKeystoneModel(
                "Portal_InsertedAstrolabeKeystone",
                socketRoot.transform,
                new Vector3(0.032f, 0f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                antiqueBrassMat,
                obsidianMat,
                astrolabeCoreMat,
                portalRuneVioletMat,
                out Transform socketSpinRing);
            socketAstrolabe.SetActive(false);
            vaultCtrl.astrolabeInSocketObject = socketAstrolabe;
            vaultCtrl.astrolabeSocketRingTransform = socketSpinRing;
            vaultCtrl.portalSocketAnchor = new Vector3(-13.52f, 1.24f, -12.67f);
            vaultCtrl.portalCenterAnchor = new Vector3(-13.58f, 1.32f, -13.00f);

            // =========================================================================
            // B. FRONT-LEFT ALCOVE (X = -12.10..-12.66m, Z = -13.39..-13.13m):
            //    1. HEAVY LEVEL-OMEGA VAULT SAFE WITH 2ND 6-DIGIT KEYPAD ("654321")
            //    2. "EL OJO DEL UMBRAL" (OCTAGONAL ASTROLABE KEYSTONE INSIDE SAFE)
            //    3. 90s CRT TV + CURSED VHS ("7 DÍAS" / THE RING) + 03:07 CONJURING CLOCK
            // =========================================================================
            GameObject safeRoot = new GameObject("SecretRoom_HeavyVaultSafe");
            safeRoot.transform.SetParent(vaultDecorRoot.transform, false);
            safeRoot.transform.localPosition = new Vector3(-12.38f, FloorTopY, -13.26f);

            // Concrete/steel base plinth & armored hollow safe cabinet (0.48m W x 0.62m H x 0.24m D)
            CreateBox("VaultSafe_BasePlinth", safeRoot.transform,
                new Vector3(0f, 0.03f, 0f), new Vector3(0.50f, 0.06f, 0.25f), darkCorniceMat, keepCollider: false);
            CreateBox("VaultSafe_BackWall", safeRoot.transform,
                new Vector3(0f, 0.36f, -0.10f), new Vector3(0.48f, 0.60f, 0.04f), safeGunmetalMat, keepCollider: false);
            CreateBox("VaultSafe_LeftWall", safeRoot.transform,
                new Vector3(-0.22f, 0.36f, 0f), new Vector3(0.04f, 0.60f, 0.24f), safeGunmetalMat, keepCollider: false);
            CreateBox("VaultSafe_RightWall", safeRoot.transform,
                new Vector3(0.22f, 0.36f, 0f), new Vector3(0.04f, 0.60f, 0.24f), safeGunmetalMat, keepCollider: false);
            CreateBox("VaultSafe_BottomWall", safeRoot.transform,
                new Vector3(0f, 0.08f, 0f), new Vector3(0.48f, 0.04f, 0.24f), safeGunmetalMat, keepCollider: false);
            CreateBox("VaultSafe_TopWall", safeRoot.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.48f, 0.04f, 0.24f), safeGunmetalMat, keepCollider: false);

            // Crimson velvet interior lining & pedestal cradle inside the safe
            CreateBox("VaultSafe_VelvetFloor", safeRoot.transform,
                new Vector3(0f, 0.11f, -0.01f), new Vector3(0.40f, 0.02f, 0.18f), crimsonVelvetMat, keepCollider: false);
            CreateBox("VaultSafe_VelvetBack", safeRoot.transform,
                new Vector3(0f, 0.36f, -0.075f), new Vector3(0.40f, 0.50f, 0.015f), crimsonVelvetMat, keepCollider: false);
            CreateBox("VaultSafe_KeystonePedestal", safeRoot.transform,
                new Vector3(0f, 0.18f, -0.01f), new Vector3(0.18f, 0.12f, 0.14f), obsidianMat, keepCollider: false);
            CreateBox("VaultSafe_PedestalVelvetTop", safeRoot.transform,
                new Vector3(0f, 0.245f, -0.01f), new Vector3(0.16f, 0.018f, 0.12f), crimsonVelvetMat, keepCollider: false);

            // "EL OJO DEL UMBRAL" — 3D Octagonal Obsidian & Brass Astrolabe Keystone inside the safe!
            GameObject safeAstrolabeRoot = new GameObject("VaultSafe_AstrolabeKeystone");
            safeAstrolabeRoot.transform.SetParent(safeRoot.transform, false);
            safeAstrolabeRoot.transform.localPosition = new Vector3(0f, 0.36f, -0.01f);
            BoxCollider astroTrig = safeAstrolabeRoot.AddComponent<BoxCollider>();
            astroTrig.size = new Vector3(0.24f, 0.24f, 0.18f);
            astroTrig.isTrigger = true;
            AttachArtifactDescriptor(
                safeAstrolabeRoot,
                "artifact:ojo-del-umbral",
                "El Ojo del Umbral (Astrolabio Dimensional de Obsidiana)",
                "The Eye of the Threshold (Obsidian Dimensional Astrolabe)",
                "Reliquia octagonal de latón grabado y obsidiana con un núcleo de fisura astral. Sus 8 muescas encajan exactamente en la Cuenca Octagonal del Espejo-Portal del fondo.",
                "Octagonal relic of engraved brass and obsidian with an astral rift core. Its 8 notches fit directly into the Octagonal Socket on the back-wall Dimensional Mirror-Portal.",
                new Color(0.32f, 0.94f, 1.0f));

            CreateAstrolabeKeystoneModel(
                "Astrolabe_VisualModel",
                safeAstrolabeRoot.transform,
                Vector3.zero,
                Quaternion.Euler(-16f, 0f, 0f),
                antiqueBrassMat,
                obsidianMat,
                astrolabeCoreMat,
                portalRuneVioletMat,
                out Transform safeAstroSpin);

            vaultCtrl.astrolabeInSafeObject = safeAstrolabeRoot;
            vaultCtrl.astrolabeSafeVisualTransform = safeAstroSpin;
            vaultCtrl.safeAstrolabeAnchor = new Vector3(-12.38f, 0.56f, -13.24f);

            // Hinged Armored Safe Door Pivot at left edge (-0.20, 0.10, +0.11) — swings outward (+Z) by -92° around Y
            GameObject safeDoorPivot = new GameObject("VaultSafe_DoorPivot");
            safeDoorPivot.transform.SetParent(safeRoot.transform, false);
            safeDoorPivot.transform.localPosition = new Vector3(-0.20f, 0.10f, 0.11f);

            GameObject safeDoorLeaf = CreateBox("VaultSafe_DoorLeaf", safeDoorPivot.transform,
                new Vector3(0.20f, 0.26f, 0f), new Vector3(0.41f, 0.52f, 0.036f), safeGunmetalMat, keepCollider: false);
            // Trigger collider on safe door so clicking the safe opens the 6-digit keypad without blocking player movement
            BoxCollider safeClickCol = safeDoorLeaf.AddComponent<BoxCollider>();
            safeClickCol.isTrigger = true;

            // Chrome locking bolts along right inner edge of safe door
            CreateBox("VaultSafe_LockBolt_1", safeDoorPivot.transform,
                new Vector3(0.39f, 0.42f, -0.015f), new Vector3(0.05f, 0.028f, 0.024f), aluminumMat, keepCollider: false);
            CreateBox("VaultSafe_LockBolt_2", safeDoorPivot.transform,
                new Vector3(0.39f, 0.26f, -0.015f), new Vector3(0.05f, 0.028f, 0.024f), aluminumMat, keepCollider: false);
            CreateBox("VaultSafe_LockBolt_3", safeDoorPivot.transform,
                new Vector3(0.39f, 0.10f, -0.015f), new Vector3(0.05f, 0.028f, 0.024f), aluminumMat, keepCollider: false);

            // Brass tri-spoke vault wheel handle on left of safe door
            CreateBox("VaultSafe_WheelHub", safeDoorPivot.transform,
                new Vector3(0.13f, 0.26f, 0.028f), new Vector3(0.055f, 0.055f, 0.03f), antiqueBrassMat, keepCollider: false);
            CreateBox("VaultSafe_WheelSpoke_V", safeDoorPivot.transform,
                new Vector3(0.13f, 0.26f, 0.038f), new Vector3(0.018f, 0.15f, 0.014f), antiqueBrassMat, keepCollider: false);
            CreateBox("VaultSafe_WheelSpoke_H", safeDoorPivot.transform,
                new Vector3(0.13f, 0.26f, 0.038f), new Vector3(0.15f, 0.018f, 0.014f), antiqueBrassMat, keepCollider: false);

            // 3D Electronic 6-Digit Keypad on right side of Safe Door
            GameObject safeKeypadRoot = new GameObject("VaultSafe_KeypadLock");
            safeKeypadRoot.transform.SetParent(safeDoorPivot.transform, false);
            safeKeypadRoot.transform.localPosition = new Vector3(0.30f, 0.28f, 0.024f);
            CreateBox("VaultSafe_KeypadHousing", safeKeypadRoot.transform,
                Vector3.zero, new Vector3(0.095f, 0.155f, 0.022f), obsidianMat, keepCollider: false);
            CreateBox("VaultSafe_KeypadBezel", safeKeypadRoot.transform,
                new Vector3(0f, 0f, -0.005f), new Vector3(0.108f, 0.168f, 0.012f), antiqueBrassMat, keepCollider: false);
            GameObject safeLedObj = CreateBox("VaultSafe_KeypadLED", safeKeypadRoot.transform,
                new Vector3(0f, 0.055f, 0.012f), new Vector3(0.062f, 0.015f, 0.006f), safeLedAmberMat, keepCollider: false);

            float[] sColsX = new float[] { -0.024f, 0f, 0.024f };
            float[] sRowsY = new float[] { 0.026f, 0.004f, -0.018f, -0.040f };
            for (int r = 0; r < sRowsY.Length; r++)
            {
                for (int c = 0; c < sColsX.Length; c++)
                {
                    CreateBox($"VaultSafe_KeyBtn_{r}_{c}", safeKeypadRoot.transform,
                        new Vector3(sColsX[c], sRowsY[r], 0.013f),
                        new Vector3(0.015f, 0.014f, 0.006f),
                        aluminumMat, keepCollider: false);
                }
            }

            // Safeblocker collider (trigger so it never impedes player capsule)
            GameObject safeBlockerGo = new GameObject("VaultSafe_ClosedBlocker");
            safeBlockerGo.transform.SetParent(safeRoot.transform, false);
            safeBlockerGo.transform.localPosition = new Vector3(0f, 0.36f, 0.11f);
            BoxCollider safeBlockerCol = safeBlockerGo.AddComponent<BoxCollider>();
            safeBlockerCol.size = new Vector3(0.42f, 0.52f, 0.04f);
            safeBlockerCol.isTrigger = true;

            SecretKeypadDoorController safeDoorCtrl = safeRoot.AddComponent<SecretKeypadDoorController>();
            safeDoorCtrl.doorId = "safe:secret-room-dimensional-vault";
            safeDoorCtrl.doorTitleEs = "Caja Fuerte de Contención (Nivel Ω)";
            safeDoorCtrl.doorTitleEn = "Containment Vault Safe (Level Ω)";
            safeDoorCtrl.activeAccessCode = "654321";
            safeDoorCtrl.requiredCodeLength = 6;
            safeDoorCtrl.allowInteriorSafetyBypass = false;
            safeDoorCtrl.southLeafPivot = safeDoorPivot.transform;
            safeDoorCtrl.northLeafPivot = null;
            safeDoorCtrl.closedDoorBlockerCollider = safeBlockerCol;
            safeDoorCtrl.openAngleDegrees = -92f;
            safeDoorCtrl.doorSwingSpeed = 2.6f;
            safeDoorCtrl.keypadInteractionAnchor = new Vector3(-12.38f, 0.62f, -13.10f);
            safeDoorCtrl.interactionRadius = 1.65f;
            safeDoorCtrl.statusLedRenderer = safeLedObj.GetComponent<Renderer>();
            vaultCtrl.vaultSafeDoorController = safeDoorCtrl;

            // 2. Above the Safe (Y = 0.88..1.38m): 90s CRT TV + Cursed VHS ("7 DÍAS" / The Ring) + 03:07 Clock (The Conjuring)
            GameObject crtRoot = new GameObject("Artifact_CrtTv_Root");
            crtRoot.transform.SetParent(vaultDecorRoot.transform, false);
            crtRoot.transform.localPosition = new Vector3(-12.38f, FloorTopY + 0.67f, -13.26f);
            crtRoot.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);

            GameObject crtBody = CreateBox("Artifact_CrtTv_Cabinet", crtRoot.transform,
                new Vector3(0f, 0.21f, 0f), new Vector3(0.38f, 0.34f, 0.22f), rackSteelMat, keepCollider: false);
            BoxCollider crtTrig = crtBody.AddComponent<BoxCollider>();
            crtTrig.isTrigger = true;
            AttachArtifactDescriptor(
                crtBody,
                "artifact:cinta-vhs-el-aro",
                "Televisor CRT y Cinta VHS '7 DÍAS' (EXP-04 • El Aro)",
                "CRT Television & '7 DAYS' Cursed VHS (EXP-04 • The Ring)",
                "Un televisor CRT de los años 90 con videocasetera integrada. Reproduce en bucle un eclipse circular de estática magnética. Etiqueta de cuarentena: 'NO MIRAR FIJAMENTE'.",
                "A 1990s CRT TV with built-in VCR deck looping a circular eclipse of magnetic static. Quarantine tag: 'DO NOT STARE DIRECTLY'.",
                new Color(0.55f, 0.84f, 1.0f));

            CreateBox("Artifact_CrtTv_StaticScreen", crtRoot.transform,
                new Vector3(-0.02f, 0.23f, 0.112f), new Vector3(0.28f, 0.22f, 0.01f), crtStaticMat, keepCollider: false);
            // The iconic dark Ring eclipse circle in the center of the CRT static screen
            CreateBox("Artifact_CrtTv_RingOuter", crtRoot.transform,
                new Vector3(-0.02f, 0.23f, 0.116f), new Vector3(0.11f, 0.11f, 0.008f), saltWhiteMat, keepCollider: false);
            CreateBox("Artifact_CrtTv_RingInnerVoid", crtRoot.transform,
                new Vector3(-0.02f, 0.23f, 0.120f), new Vector3(0.085f, 0.085f, 0.008f), obsidianMat, keepCollider: false);

            // Cursed VHS Cassette Tape resting beside the VCR slot
            CreateBox("Artifact_CursedVhsTape", crtRoot.transform,
                new Vector3(0.08f, 0.018f, 0.135f), new Vector3(0.14f, 0.024f, 0.085f), obsidianMat, keepCollider: false);
            CreateBox("Artifact_CursedVhsLabel", crtRoot.transform,
                new Vector3(0.08f, 0.031f, 0.135f), new Vector3(0.09f, 0.004f, 0.045f), saltWhiteMat, keepCollider: false);

            // Vintage Brass Alarm Clock Stopped at 03:07 AM (The Conjuring) on top of the CRT TV
            GameObject clockRoot = new GameObject("Artifact_Clock307");
            clockRoot.transform.SetParent(crtRoot.transform, false);
            clockRoot.transform.localPosition = new Vector3(-0.06f, 0.43f, 0.02f);
            CreateBox("Clock307_Body", clockRoot.transform,
                Vector3.zero, new Vector3(0.10f, 0.10f, 0.045f), antiqueBrassMat, keepCollider: false);
            CreateBox("Clock307_DialFace", clockRoot.transform,
                new Vector3(0f, 0f, 0.024f), new Vector3(0.082f, 0.082f, 0.005f), porcelainMat, keepCollider: false);
            CreateBox("Clock307_HourHand_3", clockRoot.transform,
                new Vector3(0.014f, 0f, 0.028f), new Vector3(0.028f, 0.006f, 0.004f), obsidianMat, keepCollider: false);
            GameObject minHand = CreateBox("Clock307_MinHand_07", clockRoot.transform,
                new Vector3(0.008f, 0.014f, 0.028f), new Vector3(0.005f, 0.034f, 0.004f), obsidianMat, keepCollider: false);
            minHand.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            CreateBox("Clock307_Bell_L", clockRoot.transform,
                new Vector3(-0.045f, 0.06f, 0f), new Vector3(0.032f, 0.022f, 0.032f), antiqueBrassMat, keepCollider: false);
            CreateBox("Clock307_Bell_R", clockRoot.transform,
                new Vector3(0.045f, 0.06f, 0f), new Vector3(0.032f, 0.022f, 0.032f), antiqueBrassMat, keepCollider: false);

            // =========================================================================
            // C. MID-TO-REAR LEFT WALL (X = -12.68..-13.56m, Z = -13.39..-13.15m):
            //    4-TIER INDUSTRIAL CONTAINMENT RACK & POP-CULTURE HORROR ARTIFACTS
            // =========================================================================
            GameObject rackRoot = new GameObject("SecretRoom_ContainmentRack");
            rackRoot.transform.SetParent(vaultDecorRoot.transform, false);
            rackRoot.transform.localPosition = new Vector3(-13.12f, FloorTopY, -13.27f);

            // 4 vertical steel angle-iron posts (width 0.84m, height 2.18m, depth 0.22m)
            float[] postXs = new float[] { -0.41f, 0.41f };
            float[] postZs = new float[] { -0.10f, 0.10f };
            foreach (float px in postXs)
            {
                foreach (float pz in postZs)
                {
                    CreateBox($"Rack_Post_{px:F2}_{pz:F2}", rackRoot.transform,
                        new Vector3(px, 1.09f, pz), new Vector3(0.028f, 2.18f, 0.028f), rackSteelMat, keepCollider: false);
                }
            }

            // 4 horizontal steel shelves (Y above floor = 0.06m, 0.62m, 1.28m, 1.78m, plus top cap 2.16m)
            float[] shelfYs = new float[] { 0.06f, 0.62f, 1.28f, 1.78f, 2.16f };
            for (int s = 0; s < shelfYs.Length; s++)
            {
                CreateBox($"Rack_Shelf_{s}", rackRoot.transform,
                    new Vector3(0f, shelfYs[s], 0f), new Vector3(0.84f, 0.026f, 0.23f), rackSteelMat, keepCollider: false);
                if (s < 4)
                {
                    // Red/white containment inventory tag on front shelf lip
                    CreateBox($"Rack_InventoryTag_{s}", rackRoot.transform,
                        new Vector3(-0.22f + s * 0.14f, shelfYs[s], 0.118f), new Vector3(0.09f, 0.022f, 0.006f), redThreadMat, keepCollider: false);
                }
            }

            // --- TIER 1 (Bottom Shelf, local Y = 0.08m): Dybbuk Chained Wine Box & The Babadook Pop-Up Book ---
            GameObject dybbukBox = CreateBox("Artifact_DybbukBox", rackRoot.transform,
                new Vector3(0.18f, 0.22f, 0.01f), new Vector3(0.24f, 0.28f, 0.16f), darkMahoganyMat, keepCollider: false);
            BoxCollider dybbukCol = dybbukBox.AddComponent<BoxCollider>();
            dybbukCol.isTrigger = true;
            AttachArtifactDescriptor(
                dybbukBox,
                "artifact:caja-dybbuk",
                "La Caja Dybbuk Sellada con Cadenas (EXP-01 • El Origen del Mal)",
                "Chained Dybbuk Wine Cabinet (EXP-01 • The Possession)",
                "Antigua licorera de caoba tallada en hebreo, atada con cadenas de hierro y rodeada por un cerco de sal blanca para contener a la entidad en su interior.",
                "Antique mahogany wine cabinet carved with Hebrew inscriptions, bound in iron chains inside a white salt perimeter to contain the entity within.",
                new Color(0.92f, 0.64f, 0.28f));
            CreateBox("Dybbuk_SaltRing", rackRoot.transform,
                new Vector3(0.18f, 0.078f, 0.01f), new Vector3(0.30f, 0.008f, 0.20f), saltWhiteMat, keepCollider: false);
            CreateBox("Dybbuk_Chain_H", dybbukBox.transform,
                Vector3.zero, new Vector3(1.06f, 0.14f, 1.06f), rackSteelMat, keepCollider: false);
            CreateBox("Dybbuk_Chain_V", dybbukBox.transform,
                Vector3.zero, new Vector3(0.14f, 1.06f, 1.06f), rackSteelMat, keepCollider: false);
            CreateBox("Dybbuk_WaxSeal", dybbukBox.transform,
                new Vector3(0f, 0f, 0.54f), new Vector3(0.22f, 0.22f, 0.08f), redThreadMat, keepCollider: false);

            GameObject babadookBook = CreateBox("Artifact_BabadookBook", rackRoot.transform,
                new Vector3(-0.20f, 0.23f, 0.01f), new Vector3(0.20f, 0.28f, 0.06f), obsidianMat, keepCollider: false);
            babadookBook.transform.localRotation = Quaternion.Euler(-8f, 6f, 0f);
            BoxCollider babaCol = babadookBook.AddComponent<BoxCollider>();
            babaCol.isTrigger = true;
            AttachArtifactDescriptor(
                babadookBook,
                "artifact:libro-babadook",
                "Libro Emergente 'Mister Babadook' (EXP-02)",
                "'Mister Babadook' Pop-Up Storybook (EXP-02)",
                "Libro infantil de tapa negra carbonizada que reaparece aunque sea destruido. Asegurado con correa de cuero para impedir que sus páginas emergentes se abran solas.",
                "Charcoal-black hardcover pop-up book that reappears even when burned. Strapped shut with leather to prevent its pop-up pages from opening on their own.",
                new Color(0.82f, 0.84f, 0.90f));
            CreateBox("Babadook_SilhouetteCover", babadookBook.transform,
                new Vector3(0f, -0.02f, 0.54f), new Vector3(0.55f, 0.68f, 0.06f), saltWhiteMat, keepCollider: false);
            CreateBox("Babadook_RedSpineTrim", babadookBook.transform,
                new Vector3(-0.46f, 0f, 0f), new Vector3(0.12f, 1.02f, 1.04f), crimsonVelvetMat, keepCollider: false);

            // --- TIER 2 (Eye-Level Shelf, local Y = 0.64m): Sealed Glass Vitrine with Porcelain Doll (Annabelle) & Spiral Puppet (Billy / Saw) ---
            GameObject dollVitrine = new GameObject("Artifact_DollGlassCase");
            dollVitrine.transform.SetParent(rackRoot.transform, false);
            dollVitrine.transform.localPosition = new Vector3(0.14f, 0.64f, 0.01f);

            CreateBox("Vitrine_WoodBase", dollVitrine.transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.04f, 0.20f), darkMahoganyMat, keepCollider: false);
            CreateBox("Vitrine_WoodTop", dollVitrine.transform,
                new Vector3(0f, 0.58f, 0f), new Vector3(0.36f, 0.04f, 0.20f), darkMahoganyMat, keepCollider: false);
            CreateBox("Vitrine_BackPanel", dollVitrine.transform,
                new Vector3(0f, 0.30f, -0.09f), new Vector3(0.35f, 0.54f, 0.02f), darkMahoganyMat, keepCollider: false);
            GameObject vitrineGlass = CreateBox("Vitrine_GlassEnclosure", dollVitrine.transform,
                new Vector3(0f, 0.30f, 0.005f), new Vector3(0.34f, 0.52f, 0.18f), vitrineGlassMat, keepCollider: false);
            BoxCollider vitrineCol = vitrineGlass.AddComponent<BoxCollider>();
            vitrineCol.isTrigger = true;
            AttachArtifactDescriptor(
                vitrineGlass,
                "artifact:muneca-porcelana",
                "Vitrina de Contención: Muñeca de Porcelana (EXP-03 • Annabelle)",
                "Containment Vitrine: Porcelain Conduit Doll (EXP-03 • Annabelle)",
                "Muñeca de porcelana agrietada encerrada tras cristal bendecido. El letrero superior advierte: 'ADVERTENCIA: POSITIVAMENTE NO ABRIR'.",
                "Cracked porcelain doll locked behind blessed glass. The upper warning plaque reads: 'WARNING: POSITIVELY DO NOT OPEN'.",
                new Color(1.0f, 0.36f, 0.38f));

            // Warning plaque "ADVERTENCIA: NO ABRIR" at top of glass case
            CreateBox("Vitrine_WarningSignBg", dollVitrine.transform,
                new Vector3(0f, 0.51f, 0.098f), new Vector3(0.28f, 0.065f, 0.008f), saltWhiteMat, keepCollider: false);
            CreateBox("Vitrine_WarningSignStripe", dollVitrine.transform,
                new Vector3(0f, 0.528f, 0.102f), new Vector3(0.26f, 0.022f, 0.005f), redThreadMat, keepCollider: false);

            // 3D Seated Porcelain Doll inside the vitrine
            CreateBox("Doll_Chair", dollVitrine.transform,
                new Vector3(0f, 0.14f, -0.03f), new Vector3(0.16f, 0.20f, 0.10f), darkMahoganyMat, keepCollider: false);
            CreateBox("Doll_DressSkirt", dollVitrine.transform,
                new Vector3(0f, 0.15f, 0.01f), new Vector3(0.15f, 0.18f, 0.11f), porcelainMat, keepCollider: false);
            CreateBox("Doll_CrimsonWaistRibbon", dollVitrine.transform,
                new Vector3(0f, 0.23f, 0.02f), new Vector3(0.13f, 0.035f, 0.09f), crimsonVelvetMat, keepCollider: false);
            CreateBox("Doll_Torso", dollVitrine.transform,
                new Vector3(0f, 0.28f, 0.0f), new Vector3(0.11f, 0.10f, 0.07f), porcelainMat, keepCollider: false);
            CreateBox("Doll_Head", dollVitrine.transform,
                new Vector3(0f, 0.38f, 0.01f), new Vector3(0.095f, 0.105f, 0.085f), porcelainMat, keepCollider: false);
            CreateBox("Doll_Eye_L", dollVitrine.transform,
                new Vector3(-0.022f, 0.385f, 0.052f), new Vector3(0.016f, 0.016f, 0.006f), obsidianMat, keepCollider: false);
            CreateBox("Doll_Eye_R", dollVitrine.transform,
                new Vector3(0.022f, 0.385f, 0.052f), new Vector3(0.016f, 0.016f, 0.006f), obsidianMat, keepCollider: false);
            CreateBox("Doll_Braid_L", dollVitrine.transform,
                new Vector3(-0.052f, 0.33f, 0.01f), new Vector3(0.024f, 0.12f, 0.024f), agedLeatherMat, keepCollider: false);
            CreateBox("Doll_Braid_R", dollVitrine.transform,
                new Vector3(0.052f, 0.33f, 0.01f), new Vector3(0.024f, 0.12f, 0.024f), agedLeatherMat, keepCollider: false);

            // Spiral-Cheek Ventriloquist Puppet Bust (Billy / Saw homage) next to the vitrine
            GameObject puppetRoot = new GameObject("Artifact_SpiralPuppet");
            puppetRoot.transform.SetParent(rackRoot.transform, false);
            puppetRoot.transform.localPosition = new Vector3(-0.24f, 0.64f, 0.01f);
            GameObject puppetTorso = CreateBox("Puppet_TuxedoTorso", puppetRoot.transform,
                new Vector3(0f, 0.14f, 0f), new Vector3(0.17f, 0.22f, 0.11f), obsidianMat, keepCollider: false);
            BoxCollider puppetCol = puppetTorso.AddComponent<BoxCollider>();
            puppetCol.size = new Vector3(1.4f, 2.2f, 1.4f);
            puppetCol.isTrigger = true;
            AttachArtifactDescriptor(
                puppetTorso,
                "artifact:titere-espiral",
                "Títere de Mejillas en Espiral (EXP-05 • Billy / Saw)",
                "Spiral-Cheek Ventriloquist Puppet (EXP-05 • Billy / Saw)",
                "Mariposa mecánica de rostro blanco, smoking negro y corbatín carmesí. Una grabadora oculta en su pecho susurra: 'Quiero jugar un juego...'.",
                "Pale-faced mechanical puppet in a black tuxedo and crimson bow tie. A hidden micro-cassette in its chest whispers: 'I want to play a game...'.",
                new Color(0.95f, 0.22f, 0.24f));
            CreateBox("Puppet_WhiteShirt", puppetRoot.transform,
                new Vector3(0f, 0.16f, 0.056f), new Vector3(0.06f, 0.14f, 0.008f), saltWhiteMat, keepCollider: false);
            CreateBox("Puppet_RedBowTie", puppetRoot.transform,
                new Vector3(0f, 0.22f, 0.060f), new Vector3(0.065f, 0.028f, 0.012f), redThreadMat, keepCollider: false);
            CreateBox("Puppet_PaleHead", puppetRoot.transform,
                new Vector3(0f, 0.32f, 0.01f), new Vector3(0.11f, 0.13f, 0.10f), saltWhiteMat, keepCollider: false);
            CreateBox("Puppet_SpiralCheek_L", puppetRoot.transform,
                new Vector3(-0.032f, 0.305f, 0.060f), new Vector3(0.028f, 0.028f, 0.006f), redThreadMat, keepCollider: false);
            CreateBox("Puppet_SpiralCheek_R", puppetRoot.transform,
                new Vector3(0.032f, 0.305f, 0.060f), new Vector3(0.028f, 0.028f, 0.006f), redThreadMat, keepCollider: false);
            CreateBox("Puppet_GlowingEye_L", puppetRoot.transform,
                new Vector3(-0.024f, 0.34f, 0.060f), new Vector3(0.016f, 0.014f, 0.006f), redThreadMat, keepCollider: false);
            CreateBox("Puppet_GlowingEye_R", puppetRoot.transform,
                new Vector3(0.024f, 0.34f, 0.060f), new Vector3(0.016f, 0.014f, 0.006f), redThreadMat, keepCollider: false);

            // --- TIER 3 (Upper-Mid Shelf, local Y = 1.30m): Necronomicon / Book of the Dead, Ceramic Hand (Talk to Me) & Lament Configuration (Hellraiser) ---
            GameObject grimoireRoot = new GameObject("Artifact_NecronomiconGrimoire");
            grimoireRoot.transform.SetParent(rackRoot.transform, false);
            grimoireRoot.transform.localPosition = new Vector3(0.24f, 1.30f, 0.01f);
            CreateBox("Grimoire_IronLectern", grimoireRoot.transform,
                new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.10f, 0.14f), rackSteelMat, keepCollider: false);
            GameObject grimoireTome = CreateBox("Grimoire_BoundTome", grimoireRoot.transform,
                new Vector3(0f, 0.16f, 0.01f), new Vector3(0.21f, 0.24f, 0.055f), agedLeatherMat, keepCollider: false);
            grimoireTome.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            BoxCollider grimCol = grimoireTome.AddComponent<BoxCollider>();
            grimCol.isTrigger = true;
            AttachArtifactDescriptor(
                grimoireTome,
                "artifact:grimorio-necronomicon",
                "Grimorio Encuadernado / Libro de los Muertos (EXP-06 • Evil Dead / Hamunaptra)",
                "Bound Grimoire / Book of the Dead (EXP-06 • Evil Dead / Hamunaptra)",
                "Antiguo códice encuadernado con relieves orgánicos y herrajes egipcios de oro. Sus páginas cerradas con broche de bronce murmuran cánticos de invocación.",
                "Ancient codex bound with organic relief ridges and golden Egyptian clasps. Its bronze-locked pages murmur invocation chants.",
                new Color(0.95f, 0.68f, 0.24f));
            CreateBox("Grimoire_GoldClasp_Top", grimoireTome.transform,
                new Vector3(0f, 0.44f, 0.52f), new Vector3(0.86f, 0.10f, 0.18f), antiqueBrassMat, keepCollider: false);
            CreateBox("Grimoire_GoldClasp_Bot", grimoireTome.transform,
                new Vector3(0f, -0.44f, 0.52f), new Vector3(0.86f, 0.10f, 0.18f), antiqueBrassMat, keepCollider: false);
            CreateBox("Grimoire_SculptedSigil", grimoireTome.transform,
                new Vector3(0f, 0f, 0.54f), new Vector3(0.45f, 0.48f, 0.12f), safeLedAmberMat, keepCollider: false);

            // The Embalmed Ceramic Hand (Talk to Me / Háblame)
            GameObject handRoot = new GameObject("Artifact_CeramicHand");
            handRoot.transform.SetParent(rackRoot.transform, false);
            handRoot.transform.localPosition = new Vector3(-0.02f, 1.30f, 0.02f);
            CreateBox("Hand_SaltRingBase", handRoot.transform,
                new Vector3(0f, 0.01f, 0f), new Vector3(0.16f, 0.012f, 0.14f), saltWhiteMat, keepCollider: false);
            GameObject forearm = CreateBox("Hand_CeramicForearm", handRoot.transform,
                new Vector3(0f, 0.12f, 0f), new Vector3(0.065f, 0.18f, 0.065f), porcelainMat, keepCollider: false);
            BoxCollider handCol = forearm.AddComponent<BoxCollider>();
            handCol.size = new Vector3(2.2f, 2.0f, 2.2f);
            handCol.isTrigger = true;
            AttachArtifactDescriptor(
                forearm,
                "artifact:mano-ceramica",
                "La Mano Embalsamada de Cerámica (EXP-07 • Háblame / Talk to Me)",
                "Embalmed Ceramic Hand (EXP-07 • Talk to Me)",
                "Antebrazo de cerámica blanca cubierto de firmas con tinta negra. Regla de contención: nunca sostenerla por más de 90 segundos ni decir 'Te dejo entrar'.",
                "White ceramic forearm covered in black-ink signatures. Containment rule: never hold for more than 90 seconds and never say 'I let you in'.",
                new Color(0.92f, 0.88f, 0.78f));
            CreateBox("Hand_Palm", handRoot.transform,
                new Vector3(0f, 0.24f, 0f), new Vector3(0.085f, 0.075f, 0.038f), porcelainMat, keepCollider: false);
            float[] fingerXs = new float[] { -0.030f, -0.010f, 0.010f, 0.030f };
            for (int f = 0; f < fingerXs.Length; f++)
            {
                CreateBox($"Hand_Finger_{f}", handRoot.transform,
                    new Vector3(fingerXs[f], 0.31f, 0f), new Vector3(0.015f, 0.075f, 0.018f), porcelainMat, keepCollider: false);
            }
            CreateBox("Hand_Thumb", handRoot.transform,
                new Vector3(-0.052f, 0.25f, 0.01f), new Vector3(0.028f, 0.045f, 0.018f), porcelainMat, keepCollider: false);
            CreateBox("Hand_InkInscription", handRoot.transform,
                new Vector3(0f, 0.14f, 0.034f), new Vector3(0.042f, 0.10f, 0.004f), obsidianMat, keepCollider: false);

            // The Lament Configuration Puzzle Box (Hellraiser) on crimson velvet cushion
            GameObject puzzleRoot = new GameObject("Artifact_LamentPuzzleBox");
            puzzleRoot.transform.SetParent(rackRoot.transform, false);
            puzzleRoot.transform.localPosition = new Vector3(-0.27f, 1.30f, 0.02f);
            CreateBox("Lament_VelvetCushion", puzzleRoot.transform,
                new Vector3(0f, 0.025f, 0f), new Vector3(0.18f, 0.045f, 0.15f), crimsonVelvetMat, keepCollider: false);
            GameObject lamentCube = CreateBox("Lament_PuzzleCube", puzzleRoot.transform,
                new Vector3(0f, 0.12f, 0f), new Vector3(0.115f, 0.115f, 0.115f), obsidianMat, keepCollider: false);
            lamentCube.transform.localRotation = Quaternion.Euler(14f, 32f, -10f);
            BoxCollider lamentCol = lamentCube.AddComponent<BoxCollider>();
            lamentCol.isTrigger = true;
            AttachArtifactDescriptor(
                lamentCube,
                "artifact:configuracion-lamento",
                "Caja Rompecabezas de Filigrana (EXP-08 • La Configuración del Lamento)",
                "Filigree Puzzle Box (EXP-08 • The Lament Configuration)",
                "Cubo mecánico de laca negra y latón grabado con anillos concéntricos. Se encuentra bloqueado a mitad de su secuencia para evitar abrir una grieta cenobita.",
                "Mechanical cube of black lacquer and etched brass with concentric rings. Locked mid-sequence to prevent opening an extradimensional rift.",
                new Color(1.0f, 0.78f, 0.28f));
            CreateBox("Lament_GoldFiligree_Front", lamentCube.transform,
                new Vector3(0f, 0f, 0.52f), new Vector3(0.76f, 0.76f, 0.04f), antiqueBrassMat, keepCollider: false);
            CreateBox("Lament_GoldFiligree_Top", lamentCube.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.76f, 0.04f, 0.76f), antiqueBrassMat, keepCollider: false);
            CreateBox("Lament_GoldFiligree_Side", lamentCube.transform,
                new Vector3(0.52f, 0f, 0f), new Vector3(0.04f, 0.76f, 0.76f), antiqueBrassMat, keepCollider: false);

            // --- TIER 4 (Top Shelf, local Y = 1.80m): Ouija Spirit Board & Chucky Good-Guy Yellow Box ---
            GameObject ouijaBoard = CreateBox("Artifact_OuijaBoard", rackRoot.transform,
                new Vector3(0.16f, 1.96f, 0.01f), new Vector3(0.36f, 0.24f, 0.025f), agedLeatherMat, keepCollider: false);
            ouijaBoard.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
            BoxCollider ouijaCol = ouijaBoard.AddComponent<BoxCollider>();
            ouijaCol.isTrigger = true;
            AttachArtifactDescriptor(
                ouijaBoard,
                "artifact:tablero-ouija",
                "Tablero Espiritista y Planchette (EXP-09 • El Exorcista / Ouija)",
                "Spirit Board & Planchette (EXP-09 • The Exorcist / Ouija)",
                "Tablero clásico de madera grabada con el sol, la luna y el arco alfabético. El puntero permanece clavado sobre la palabra 'GOODBYE'.",
                "Classic engraved wooden spirit board with sun, crescent moon, and alphabet arc. The planchette is pinned directly over 'GOODBYE'.",
                new Color(0.86f, 0.72f, 0.46f));
            CreateBox("Ouija_LetterArc_1", ouijaBoard.transform,
                new Vector3(0f, 0.14f, 0.56f), new Vector3(0.74f, 0.08f, 0.08f), obsidianMat, keepCollider: false);
            CreateBox("Ouija_LetterArc_2", ouijaBoard.transform,
                new Vector3(0f, -0.02f, 0.56f), new Vector3(0.68f, 0.08f, 0.08f), obsidianMat, keepCollider: false);
            CreateBox("Ouija_Planchette", ouijaBoard.transform,
                new Vector3(0.12f, -0.22f, 0.65f), new Vector3(0.22f, 0.24f, 0.16f), porcelainMat, keepCollider: false);

            GameObject chuckyBox = CreateBox("Artifact_PossessedDollBox", rackRoot.transform,
                new Vector3(-0.24f, 1.98f, 0.01f), new Vector3(0.22f, 0.32f, 0.14f), goodGuyYellowMat, keepCollider: false);
            BoxCollider chuckyCol = chuckyBox.AddComponent<BoxCollider>();
            chuckyCol.isTrigger = true;
            AttachArtifactDescriptor(
                chuckyBox,
                "artifact:muneco-good-guy",
                "Caja de Juguete 'Good Guy' con Rasguños Internos (EXP-10 • Chucky)",
                "'Good Guy' Toy Box with Internal Claw Marks (EXP-10 • Child's Play)",
                "Caja amarilla original de fábrica asegurada con flejes de acero. A través de la ventana se distingue un overol azul y marcas de corte hechas desde adentro.",
                "Factory yellow toy box secured with steel straps. Through the window, blue overalls and slash marks made from the inside are visible.",
                new Color(1.0f, 0.82f, 0.18f));
            CreateBox("GoodGuy_WindowRecess", chuckyBox.transform,
                new Vector3(0f, -0.04f, 0.52f), new Vector3(0.68f, 0.64f, 0.06f), toyBoxBlueMat, keepCollider: false);
            CreateBox("GoodGuy_RedHairHead", chuckyBox.transform,
                new Vector3(0f, 0.10f, 0.55f), new Vector3(0.36f, 0.24f, 0.06f), redThreadMat, keepCollider: false);
            CreateBox("GoodGuy_SteelStrap", chuckyBox.transform,
                Vector3.zero, new Vector3(1.05f, 0.10f, 1.06f), rackSteelMat, keepCollider: false);

            // =========================================================================
            // D. RIGHT DIAGONAL WALL (CornerNE (-12.06, -11.22) -> CornerNW (-13.66, -12.60), length 2.11m):
            //    1. CONSPIRACY EVIDENCE CORKBOARD WITH SAFE CODE CLUE ("654321") & RED STRINGS
            //    2. THE BLACK ROTARY PHONE WITH SEVERED CORD (The Black Phone)
            //    3. FLOATING RELIC SHELF: PAQUIMÉ OBSIDIAN MASK & ANOMALOUS ASTRAL COMPASS
            // =========================================================================
            Vector2 diagDelta = CornerNW - CornerNE;
            float diagLen = diagDelta.magnitude; // 2.11m
            Vector2 diagDir = diagDelta / diagLen;

            GameObject diagDecorRoot = new GameObject("RightDiagonalWall_Decor_Root");
            diagDecorRoot.transform.SetParent(vaultDecorRoot.transform, false);
            diagDecorRoot.transform.localPosition = new Vector3(CornerNE.x, FloorTopY, CornerNE.y);
            // Local +Z runs from CornerNE (0m) to CornerNW (2.11m); Local -X points inward into the room!
            diagDecorRoot.transform.localRotation = Quaternion.LookRotation(new Vector3(diagDir.x, 0f, diagDir.y), Vector3.up);

            // 1. Conspiracy Evidence Corkboard at localZ = 0.72m, Y above floor = 1.28m
            GameObject corkboardRoot = new GameObject("Artifact_EvidenceCorkboard");
            corkboardRoot.transform.SetParent(diagDecorRoot.transform, false);
            corkboardRoot.transform.localPosition = new Vector3(-0.025f, 1.28f, 0.72f);

            CreateBox("Corkboard_WoodFrame", corkboardRoot.transform,
                Vector3.zero, new Vector3(0.024f, 0.54f, 0.68f), darkMahoganyMat, keepCollider: false);
            GameObject corkFace = CreateBox("Corkboard_CorkSurface", corkboardRoot.transform,
                new Vector3(-0.008f, 0f, 0f), new Vector3(0.016f, 0.48f, 0.62f), corkMat, keepCollider: false);
            BoxCollider corkCol = corkFace.AddComponent<BoxCollider>();
            corkCol.isTrigger = true;
            AttachArtifactDescriptor(
                corkFace,
                "artifact:muro-evidencias-pdn",
                "Muro de Evidencias Conspirativas PDN-00 (Pista de Bóveda Ω: 654321)",
                "Conspiracy Evidence Board PDN-00 (Vault Ω Clue: 654321)",
                "Mapa de anomalías en Av. Benito Juárez conectado con hilos rojos hacia el Espejo del Umbral. Una nota amarilla subraya: 'ACCESO BÓVEDA NIVEL Ω = INVERSO (6-5-4-3-2-1)'.",
                "Anomaly map of Av. Benito Juárez connected with red yarn toward the Threshold Mirror. A yellow sticky note reads: 'LEVEL-Ω VAULT ACCESS = REVERSE (6-5-4-3-2-1)'.",
                new Color(1.0f, 0.88f, 0.28f));

            // Pinned photos, portal diagram & yellow sticky note with Safe Code Clue ("654321")
            CreateBox("Evidence_Photo_Bridge", corkboardRoot.transform,
                new Vector3(-0.018f, 0.11f, -0.18f), new Vector3(0.006f, 0.14f, 0.16f), whiteFrameMat, keepCollider: false);
            CreateBox("Evidence_Diagram_Astrolabe", corkboardRoot.transform,
                new Vector3(-0.018f, 0.08f, 0.16f), new Vector3(0.006f, 0.16f, 0.16f), porcelainMat, keepCollider: false);
            CreateBox("Evidence_Diagram_Ring", corkboardRoot.transform,
                new Vector3(-0.022f, 0.08f, 0.16f), new Vector3(0.006f, 0.09f, 0.09f), astrolabeCoreMat, keepCollider: false);
            CreateBox("Evidence_StickyNote_SafeCode654321", corkboardRoot.transform,
                new Vector3(-0.019f, -0.12f, -0.06f), new Vector3(0.008f, 0.12f, 0.14f), stickyYellowMat, keepCollider: false);

            // Red conspiracy threads connecting the photos to the Astrolabe diagram
            GameObject thread1 = CreateBox("Evidence_RedThread_1", corkboardRoot.transform,
                new Vector3(-0.022f, 0.095f, -0.01f), new Vector3(0.006f, 0.012f, 0.34f), redThreadMat, keepCollider: false);
            thread1.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            GameObject thread2 = CreateBox("Evidence_RedThread_2", corkboardRoot.transform,
                new Vector3(-0.022f, -0.02f, 0.05f), new Vector3(0.006f, 0.012f, 0.28f), redThreadMat, keepCollider: false);
            thread2.transform.localRotation = Quaternion.Euler(-38f, 0f, 0f);

            // 2. The Black Rotary Phone with Severed Cord (The Black Phone) at localZ = 1.28m, Y = 1.26m
            GameObject phoneRoot = new GameObject("Artifact_BlackRotaryPhone");
            phoneRoot.transform.SetParent(diagDecorRoot.transform, false);
            phoneRoot.transform.localPosition = new Vector3(-0.04f, 1.26f, 1.28f);

            GameObject phoneBody = CreateBox("BlackPhone_WallBox", phoneRoot.transform,
                Vector3.zero, new Vector3(0.065f, 0.22f, 0.14f), obsidianMat, keepCollider: false);
            BoxCollider phoneCol = phoneBody.AddComponent<BoxCollider>();
            phoneCol.isTrigger = true;
            AttachArtifactDescriptor(
                phoneBody,
                "artifact:telefono-negro",
                "El Teléfono Negro con Cable Cortado (EXP-11 • The Black Phone)",
                "The Black Rotary Phone with Severed Cord (EXP-11 • The Black Phone)",
                "Teléfono mural de baquelita negra cuyo cable fue cortado hace décadas. A pesar de no tener línea, a veces timbra en la madrugada con voces del otro lado del umbral.",
                "Black Bakelite wall telephone whose cord was severed decades ago. Despite having no line, it occasionally rings past midnight with voices from beyond the threshold.",
                new Color(0.75f, 0.80f, 0.90f));
            CreateBox("BlackPhone_RotaryDial", phoneRoot.transform,
                new Vector3(-0.035f, -0.01f, 0f), new Vector3(0.012f, 0.085f, 0.085f), aluminumMat, keepCollider: false);
            CreateBox("BlackPhone_ReceiverHandset", phoneRoot.transform,
                new Vector3(-0.032f, 0.125f, 0f), new Vector3(0.045f, 0.042f, 0.18f), obsidianMat, keepCollider: false);
            CreateBox("BlackPhone_SeveredCord", phoneRoot.transform,
                new Vector3(-0.018f, -0.19f, -0.03f), new Vector3(0.016f, 0.18f, 0.016f), obsidianMat, keepCollider: false);

            // 3. Slim Floating Relic Shelf at localZ = 1.66m, Y = 0.92m: Paquimé Obsidian Mask & Anomalous Astral Compass
            GameObject relicShelfRoot = new GameObject("Artifact_DiagonalRelicShelf");
            relicShelfRoot.transform.SetParent(diagDecorRoot.transform, false);
            relicShelfRoot.transform.localPosition = new Vector3(-0.055f, 0.92f, 1.66f);

            CreateBox("RelicShelf_Plank", relicShelfRoot.transform,
                Vector3.zero, new Vector3(0.11f, 0.024f, 0.46f), darkMahoganyMat, keepCollider: false);

            // Pre-Hispanic Paquimé Ceremonial Obsidian & Turquoise Mask
            GameObject maskObj = CreateBox("Artifact_PaquimeObsidianMask", relicShelfRoot.transform,
                new Vector3(0f, 0.12f, -0.10f), new Vector3(0.055f, 0.17f, 0.13f), obsidianMat, keepCollider: false);
            BoxCollider maskCol = maskObj.AddComponent<BoxCollider>();
            maskCol.isTrigger = true;
            AttachArtifactDescriptor(
                maskObj,
                "artifact:mascara-paquime",
                "Máscara Ceremonial de Obsidiana y Turquesa (EXP-12 • Reliquia Fronteriza)",
                "Ceremonial Obsidian & Turquoise Mask (EXP-12 • Borderland Relic)",
                "Máscara prehispánica tallada en obsidiana negra con grecas escalonadas de Paquimé e incrustaciones de turquesa que brillan cuando el Umbral Dimensional despierta.",
                "Pre-Hispanic mask carved in black obsidian with Paquimé step-frets and turquoise inlays that glow whenever the Dimensional Threshold awakens.",
                new Color(0.20f, 0.94f, 0.80f));
            CreateBox("PaquimeMask_Eye_L", maskObj.transform,
                new Vector3(-0.54f, 0.16f, -0.24f), new Vector3(0.16f, 0.14f, 0.22f), turquoiseGlowMat, keepCollider: false);
            CreateBox("PaquimeMask_Eye_R", maskObj.transform,
                new Vector3(-0.54f, 0.16f, 0.24f), new Vector3(0.16f, 0.14f, 0.22f), turquoiseGlowMat, keepCollider: false);
            CreateBox("PaquimeMask_ForeheadCrown", maskObj.transform,
                new Vector3(-0.52f, 0.38f, 0f), new Vector3(0.12f, 0.12f, 0.72f), antiqueBrassMat, keepCollider: false);

            // Anomalous Brass Astral Compass pointing straight at the Dimensional Mirror
            GameObject compassObj = CreateBox("Artifact_AnomalousCompass", relicShelfRoot.transform,
                new Vector3(-0.01f, 0.032f, 0.11f), new Vector3(0.075f, 0.028f, 0.075f), antiqueBrassMat, keepCollider: false);
            BoxCollider compCol = compassObj.AddComponent<BoxCollider>();
            compCol.isTrigger = true;
            AttachArtifactDescriptor(
                compassObj,
                "artifact:brujula-astral",
                "Brújula Astral Anómala (EXP-13)",
                "Anomalous Astral Compass (EXP-13)",
                "Brújula de latón confiscada en el Puente Paso del Norte. Ignora el norte magnético terrestre y apunta invariablemente hacia el Espejo del Umbral.",
                "Brass pocket compass confiscated at the Paso del Norte Bridge. It ignores Earth's magnetic north and points unerringly at the Threshold Mirror.",
                new Color(0.38f, 0.92f, 1.0f));
            CreateBox("Compass_GlowingNeedle", compassObj.transform,
                new Vector3(0f, 0.56f, 0f), new Vector3(0.18f, 0.14f, 0.78f), astrolabeCoreMat, keepCollider: false);

            vaultCtrl.RefreshVisualState();
        }

        private static GameObject CreateAstrolabeKeystoneModel(
            string name,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Material brassMat,
            Material obsidianMat,
            Material coreGlowMat,
            Material runeGlowMat,
            out Transform spinningRingTransform)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;

            // Outer octagonal brass disc formed by two overlapping 45°-offset squares
            CreateBox("Astrolabe_OuterOct_A", root.transform,
                Vector3.zero, new Vector3(0.115f, 0.115f, 0.018f), brassMat, keepCollider: false);
            GameObject octB = CreateBox("Astrolabe_OuterOct_B", root.transform,
                Vector3.zero, new Vector3(0.115f, 0.115f, 0.018f), brassMat, keepCollider: false);
            octB.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Inner obsidian celestial disc
            CreateBox("Astrolabe_ObsidianInlay_A", root.transform,
                new Vector3(0f, 0f, 0.004f), new Vector3(0.086f, 0.086f, 0.020f), obsidianMat, keepCollider: false);
            GameObject inlayB = CreateBox("Astrolabe_ObsidianInlay_B", root.transform,
                new Vector3(0f, 0f, 0.004f), new Vector3(0.086f, 0.086f, 0.020f), obsidianMat, keepCollider: false);
            inlayB.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Rotating inner astral ring & crosshairs
            GameObject spinRing = new GameObject("Astrolabe_SpinningCelestialRing");
            spinRing.transform.SetParent(root.transform, false);
            spinRing.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            CreateBox("Astrolabe_CrossBar_H", spinRing.transform,
                Vector3.zero, new Vector3(0.105f, 0.014f, 0.012f), brassMat, keepCollider: false);
            CreateBox("Astrolabe_CrossBar_V", spinRing.transform,
                Vector3.zero, new Vector3(0.014f, 0.105f, 0.012f), brassMat, keepCollider: false);
            GameObject runeDiamond = CreateBox("Astrolabe_RuneRing", spinRing.transform,
                Vector3.zero, new Vector3(0.058f, 0.058f, 0.014f), runeGlowMat, keepCollider: false);
            runeDiamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Faceted glowing Cyan-Violet Bifrost Rift Core Crystal at center
            CreateBox("Astrolabe_RiftCoreCrystal", spinRing.transform,
                new Vector3(0f, 0f, 0.008f), new Vector3(0.034f, 0.034f, 0.022f), coreGlowMat, keepCollider: false);

            spinningRingTransform = spinRing.transform;
            return root;
        }

        private static void AttachArtifactDescriptor(
            GameObject target,
            string id,
            string titleEs,
            string titleEn,
            string loreEs,
            string loreEn,
            Color accentColor)
        {
            if (target == null) return;
            SecretContainmentArtifactDescriptor desc = target.AddComponent<SecretContainmentArtifactDescriptor>();
            desc.artifactId = id;
            desc.titleEs = titleEs;
            desc.titleEn = titleEn;
            desc.loreEs = loreEs;
            desc.loreEn = loreEn;
            desc.accentColor = accentColor;
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
