using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Idempotent builder and targeted artifact migrator for Assets/Scenes/BackroomsLevel1Scene.unity:
    /// A full real-world scale 40m x 24m (960 m²) liminal Backrooms Level 0 maze (origin Y = -50.0m,
    /// clear height 2.85m), matching the actual footprint of the south block between Av. Benito Juárez
    /// and C. Ignacio Mariscal. Loaded additively when the player crosses the Threshold Veil inside
    /// the Building A South mysterious door.
    ///
    /// Includes:
    ///   - Seamless procedural Level 0 yellow wallpaper, damp mustard carpet & acoustic ceiling grid
    ///   - 24 fluorescent ceiling fixtures (steady, flickering & dead tubes) + ceiling void tiles
    ///   - Vast central pillared hall, long disorienting corridors & isolated rooms
    ///   - Strictly 1 lone workstation (1 desk + 1 laptop + 1 office chair) in a surreal random alcove
    ///   - 8 "Reality-Bridge" props ready for future live API integrations (Laptop, FM Radio, Alexa,
    ///     Diario de Juárez iPad, Juárez Live Weather/Clock Terminal, 90s Wall Payphone, CCTV Frame,
    ///     Thermal Receipt Printer)
    ///   - 12 original archival & anomalous artifacts plus Evidence Board PDN-00, Paquimé mask &
    ///     Anomalous Astral Compass dispersed across the 40x24m maze
    ///   - Level-Omega Vault Safe ("654321") in the far NW Vault Room holding "El Ojo del Umbral"
    ///   - Single Monumental Dimensional Mirror-Portal ("El Umbral de Lasser") in the far SW Chamber
    ///     (~40m diagonally away from the NE entrance!)
    /// </summary>
    public static class BackroomsLevel1Builder
    {
        public const string ScenePath = "Assets/Scenes/BackroomsLevel1Scene.unity";
        public const float BaseY = -50.00f;
        public const float ClearHeight = 2.85f;
        public const float CeilingY = BaseY + ClearHeight; // -47.15f
        public const float HalfWidthX = 20.0f;  // Total 40.0m (East-West: Mariscal <-> Av. Juárez)
        public const float HalfDepthZ = 12.0f;  // Total 24.0m (North-South: Azucenas <-> Farmacia Juárez)

        // Canonical texture & material paths (preserving existing Backrooms_* filenames & .meta GUIDs)
        public const string WallConcreteTexPath = "Assets/Textures/Environment/T_Backrooms_YellowWallpaper.png";
        public const string FloorConcreteTexPath = "Assets/Textures/Environment/T_Backrooms_DampCarpet.png";
        public const string CeilingSoffitTexPath = "Assets/Textures/Environment/T_Backrooms_CeilingGrid.png";
        public const string ColumnConcreteTexPath = "Assets/Textures/Environment/T_Remanencia_Level0_ConcreteColumn.png";
        public const string PlasterWallTexPath = "Assets/Textures/Environment/T_Remanencia_Level0_PlasterWall.png";

        public const string WallConcreteMatPath = "Assets/Settings/Backrooms_YellowWallpaper.mat";
        public const string FloorConcreteMatPath = "Assets/Settings/Backrooms_DampCarpet.mat";
        public const string CeilingSoffitMatPath = "Assets/Settings/Backrooms_CeilingGrid.mat";
        public const string BaseboardMatPath = "Assets/Settings/Backrooms_Baseboard.mat";
        public const string FluorOnMatPath = "Assets/Settings/Backrooms_FluorescentOn.mat";
        public const string FluorOffMatPath = "Assets/Settings/Backrooms_FluorescentOff.mat";
        public const string CeilingVoidMatPath = "Assets/Settings/Backrooms_CeilingVoid.mat";
        public const string ColumnConcreteMatPath = "Assets/Settings/Remanencia_Level0_ConcreteColumn.mat";
        public const string PlasterWallMatPath = "Assets/Settings/Remanencia_Level0_PlasterTerracottaWall.mat";
        public const string IndustrialMetalMatPath = "Assets/Settings/Remanencia_Level0_IndustrialMetal.mat";
        public const string UtilityWarmOnMatPath = "Assets/Settings/Remanencia_Level0_UtilityWarmOn.mat";

        // Curated subset of preserved interior partition walls receiving weathered plaster / faded terracotta skim-coat
        private static readonly HashSet<string> PlasterPartitionWallNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Wall_NE_ArrivalSouth",
            "Wall_NWVault_East",
            "Wall_NWVault_SouthW",
            "Wall_NWVault_SouthE",
            "Wall_SWPortal_EastN",
            "Wall_SWPortal_EastS",
            "Wall_SWPortal_North"
        };

        // Obsolete legacy Backrooms labyrinth interior walls explicitly replaced by the Hotel de las Palomas
        // ground-floor architecture (entrance vestibule, open reception/lobby, twin elevator core,
        // double-loaded guest-room corridor, 6 empty guest-room shells, and service wing bulkhead).
        private static readonly HashSet<string> ObsoleteLegacyMazeWallNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Wall_NE_SightlineBaffle",
            "Wall_NorthCorridor_SouthA",
            "Wall_NorthCorridor_AlcoveW",
            "Wall_NorthCorridor_Divider",
            "Wall_NCenter_East",
            "Wall_NCenter_SouthL",
            "Wall_NCenter_SouthR",
            "Wall_NCenter_West",
            "Wall_EastWing_WestN",
            "Wall_EastWing_WorkstationN",
            "Wall_EastWing_WorkstationE",
            "Wall_SE_NorthE",
            "Wall_SE_NorthW",
            "Wall_SE_West",
            "Wall_SCenter_NorthE",
            "Wall_SCenter_NorthW",
            "Wall_SCenter_West",
            "Wall_SWLab_NorthE",
            "Wall_SWLab_Baffle"
        };

        public const string HotelArchitectureRootName = "Hotel_GroundFloor_Architecture";
        public const string HotelEnvironmentalIdentityRootName = "Hotel_Environmental_Identity";

        public const string PipeCopperRustMatPath = "Assets/Settings/Remanencia_Level0_PipeCopperRust.mat";
        public const string UnfinishedWoodDoorMatPath = "Assets/Settings/Remanencia_Level0_UnfinishedWoodDoor.mat";
        public const string AgedCeramicPatchMatPath = "Assets/Settings/Remanencia_Level0_AgedCeramicPatch.mat";
        public const string SignHotelNameMatPath = "Assets/Settings/Remanencia_Sign_HotelName.mat";
        public const string SignReceptionMatPath = "Assets/Settings/Remanencia_Sign_Reception.mat";
        public const string SignElevatorsMatPath = "Assets/Settings/Remanencia_Sign_Elevators.mat";
        public const string SignElevatorNoticeMatPath = "Assets/Settings/Remanencia_Sign_ElevatorNotice.mat";
        public const string SignGuestRoomsMatPath = "Assets/Settings/Remanencia_Sign_GuestRooms.mat";
        public const string SignServiceMatPath = "Assets/Settings/Remanencia_Sign_Service.mat";
        public const string SignMaintenanceMatPath = "Assets/Settings/Remanencia_Sign_Maintenance.mat";

        // Canonical approved bilingual signage wording (UTF-8)
        public const string SignHotelNameEs = "HOTEL DE LAS PALOMAS";
        public const string SignHotelNameSub = "NIVEL DE ACCESO";
        public const string SignReceptionEs = "RECEPCIÓN";
        public const string SignReceptionEn = "RECEPTION";
        public const string SignElevatorsEs = "ELEVADORES";
        public const string SignElevatorsEn = "ELEVATORS";
        public const string SignElevatorNoticeEs = "FUERA DE SERVICIO";
        public const string SignElevatorNoticeEn = "OUT OF SERVICE";
        public const string SignGuestRoomsEs = "HABITACIONES 101-106";
        public const string SignGuestRoomsEn = "GUEST ROOMS";
        public const string SignServiceEs = "SOLO PERSONAL";
        public const string SignServiceEn = "STAFF ONLY";
        public const string SignMaintenanceEs = "ÁREA DE MANTENIMIENTO";
        public const string SignMaintenanceEn = "MAINTENANCE AREA";

        private enum IndustrialFixtureStyle
        {
            LinearStrip,
            HoodedStrip,
            CagedUtility
        }

        private struct RemanenciaVisualMaterials
        {
            public Material WallConcrete;
            public Material WallPlaster;
            public Material ColumnConcrete;
            public Material FloorConcrete;
            public Material CeilingSoffit;
            public Material BaseboardConcrete;
            public Material FluorOn;
            public Material FluorOff;
            public Material UtilityWarmOn;
            public Material IndustrialMetal;
            public Material CeilingVoid;
            public Material ElevatorDoorMetal;
            public Material[] RoomPlaqueMats; // Index 0..5 -> Rooms 101..106
            public Material PipeCopperRust;
            public Material UnfinishedWoodDoor;
            public Material AgedCeramicPatch;
            public Material SignHotelName;
            public Material SignReception;
            public Material SignElevators;
            public Material SignElevatorNotice;
            public Material SignGuestRooms;
            public Material SignService;
            public Material SignMaintenance;
        }

        private static readonly (float x, float z, bool lit, bool spawnLight, bool flicker, float intensity, IndustrialFixtureStyle style)[] CanonicalFixtures = new[]
        {
            // North Row (Z = +8.8m)
            ( 16.5f,  8.8f, true,  true,  false, 1.22f, IndustrialFixtureStyle.LinearStrip),  // 0: NE Arrival Vestibule
            ( 10.2f,  8.8f, true,  false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 1: NE Baffle Corridor (emissive strip)
            (  4.5f,  8.8f, true,  true,  false, 1.12f, IndustrialFixtureStyle.CagedUtility), // 2: N-East Alcove (warm work light)
            ( -5.2f,  8.8f, true,  true,  true,  1.05f, IndustrialFixtureStyle.HoodedStrip),  // 3: N-Center Specimen Vitrine Room (flicker)
            (-11.0f,  8.8f, false, false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 4: Dead tube before NW Vault
            (-16.5f,  8.6f, true,  true,  false, 1.20f, IndustrialFixtureStyle.HoodedStrip),  // 5: NW Level-Omega Vault Safe Room

            // Upper-Central Row (Z = +2.8m)
            ( 16.5f,  2.2f, true,  true,  false, 1.12f, IndustrialFixtureStyle.LinearStrip),  // 6: East Corridor
            ( 12.5f,  2.2f, true,  false, true,  0.00f, IndustrialFixtureStyle.HoodedStrip),  // 7: FM Radio Hall strip
            (  5.5f,  2.8f, true,  true,  false, 1.18f, IndustrialFixtureStyle.LinearStrip),  // 8: Central Pillared Hall East
            ( -1.5f,  2.8f, true,  true,  true,  1.12f, IndustrialFixtureStyle.LinearStrip),  // 9: Central Pillared Hall Center (flicker)
            ( -8.5f,  2.8f, true,  true,  false, 1.15f, IndustrialFixtureStyle.LinearStrip),  // 10: Central Pillared Hall West
            (-15.5f,  2.4f, false, false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 11: Dead tube in West Corridor

            // Lower-Central Row (Z = -2.4m)
            ( 17.5f, -2.4f, true,  false, false, 0.00f, IndustrialFixtureStyle.HoodedStrip),  // 12: Far East Corridor
            (  7.2f, -2.6f, true,  true,  false, 1.15f, IndustrialFixtureStyle.CagedUtility), // 13: Lone Workstation Recess (warm work light)
            (  0.5f, -2.2f, true,  true,  false, 1.12f, IndustrialFixtureStyle.LinearStrip),  // 14: Central-South Hall
            ( -6.8f, -2.4f, true,  true,  true,  1.05f, IndustrialFixtureStyle.HoodedStrip),  // 15: West-Central Hall (flicker)
            (-14.5f, -1.6f, true,  true,  false, 0.98f, IndustrialFixtureStyle.CagedUtility), // 16: West Signal Monitor Niche (warm work light)

            // South Row (Z = -8.4m)
            ( 15.5f, -8.4f, true,  true,  false, 1.10f, IndustrialFixtureStyle.CagedUtility), // 17: SE Corridor (warm work light)
            ( 11.2f, -8.4f, false, false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 18: Dead tube between SE & S-Center
            (  4.0f, -8.4f, true,  true,  false, 1.22f, IndustrialFixtureStyle.HoodedStrip),  // 19: S-Center Evidence Corkboard & Thermal Printer
            ( -3.5f, -8.4f, true,  false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 20: South Labyrinth East
            ( -9.5f, -8.4f, true,  true,  true,  1.02f, IndustrialFixtureStyle.CagedUtility), // 21: SW Labyrinth Custody Case & Border Clock (flicker, warm)
            (-16.5f, -8.2f, true,  true,  true,  0.76f, IndustrialFixtureStyle.CagedUtility), // 22: Far SW Dimensional Passage Chamber (dim warm flicker)
            (-16.5f, -5.2f, false, false, false, 0.00f, IndustrialFixtureStyle.LinearStrip),  // 23: Dead tube at Portal Threshold
        };

        private static readonly Color CoolIndustrialLightColor = new Color(0.84f, 0.90f, 0.95f);
        private static readonly Color WarmUtilityLightColor = new Color(0.96f, 0.84f, 0.65f);

        public const string ElevatorDoorMetalMatPath = "Assets/Settings/Remanencia_Level0_ElevatorDoorMetal.mat";

        private static RemanenciaVisualMaterials EnsureRemanenciaVisualAssets()
        {
            string wallTexPath = EnsureBackroomsWallpaperTexture(WallConcreteTexPath);
            string floorTexPath = EnsureBackroomsCarpetTexture(FloorConcreteTexPath);
            string ceilingTexPath = EnsureBackroomsCeilingGridTexture(CeilingSoffitTexPath);
            string columnTexPath = EnsureRemanenciaConcreteColumnTexture(ColumnConcreteTexPath);
            string plasterTexPath = EnsureRemanenciaPlasterWallTexture(PlasterWallTexPath);
            string[] plaqueTexPaths = new string[6];
            for (int r = 0; r < 6; r++)
            {
                int roomNum = 101 + r;
                plaqueTexPaths[r] = EnsureRoomNumberPlaqueTexture(
                    $"Assets/Textures/Environment/T_Remanencia_RoomPlaque_{roomNum}.png",
                    roomNum.ToString());
            }

            // Implementation 07.3 / 07.4 restrained bilingual hotel identity & wayfinding signage textures
            string signHotelTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_HotelName.png", 512, 96,
                SignHotelNameEs, SignHotelNameSub, darkBronzeStyle: true, weatheredIncomplete: true, fadeLastChar: true);
            string signReceptionTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_Reception.png", 512, 112,
                SignReceptionEs, SignReceptionEn, darkBronzeStyle: true, weatheredIncomplete: false, fadeLastChar: false);
            string signElevatorsTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_Elevators.png", 384, 96,
                SignElevatorsEs, SignElevatorsEn, darkBronzeStyle: false, weatheredIncomplete: false, fadeLastChar: false);
            string signElevNoticeTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_ElevatorNotice.png", 384, 128,
                SignElevatorNoticeEs, SignElevatorNoticeEn, darkBronzeStyle: false, weatheredIncomplete: true, fadeLastChar: false);
            string signGuestRoomsTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_GuestRooms.png", 384, 96,
                SignGuestRoomsEs, SignGuestRoomsEn, darkBronzeStyle: false, weatheredIncomplete: false, fadeLastChar: false);
            string signServiceTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_Service.png", 384, 96,
                SignServiceEs, SignServiceEn, darkBronzeStyle: false, weatheredIncomplete: true, fadeLastChar: false);
            string signMaintTex = EnsureArchitecturalSignTexture(
                "Assets/Textures/Environment/T_Remanencia_Sign_Maintenance.png", 512, 112,
                SignMaintenanceEs, SignMaintenanceEn, darkBronzeStyle: false, weatheredIncomplete: true, fadeLastChar: false);
            AssetDatabase.Refresh();

            ConfigureArchitecturalTextureImporter(wallTexPath);
            ConfigureArchitecturalTextureImporter(floorTexPath);
            ConfigureArchitecturalTextureImporter(ceilingTexPath);
            ConfigureArchitecturalTextureImporter(columnTexPath);
            ConfigureArchitecturalTextureImporter(plasterTexPath);
            for (int r = 0; r < 6; r++)
            {
                ConfigurePlaqueTextureImporter(plaqueTexPaths[r]);
            }
            ConfigureSignTextureImporter(signHotelTex);
            ConfigureSignTextureImporter(signReceptionTex);
            ConfigureSignTextureImporter(signElevatorsTex);
            ConfigureSignTextureImporter(signElevNoticeTex);
            ConfigureSignTextureImporter(signGuestRoomsTex);
            ConfigureSignTextureImporter(signServiceTex);
            ConfigureSignTextureImporter(signMaintTex);

            Material[] plaqueMats = new Material[6];
            for (int r = 0; r < 6; r++)
            {
                int roomNum = 101 + r;
                plaqueMats[r] = CreateTexturedMat(
                    $"Assets/Settings/Remanencia_Level0_RoomPlaque_{roomNum}.mat",
                    plaqueTexPaths[r],
                    Color.white,
                    0.28f,
                    Vector2.one);
            }

            var mats = new RemanenciaVisualMaterials
            {
                WallConcrete = CreateTexturedMat(WallConcreteMatPath, wallTexPath, new Color(0.94f, 0.93f, 0.91f), 0.14f, new Vector2(1.25f, 1.0f)),
                WallPlaster = CreateTexturedMat(PlasterWallMatPath, plasterTexPath, new Color(0.95f, 0.93f, 0.90f), 0.12f, new Vector2(1.25f, 1.0f)),
                ColumnConcrete = CreateTexturedMat(ColumnConcreteMatPath, columnTexPath, new Color(0.92f, 0.92f, 0.91f), 0.16f, new Vector2(1.0f, 1.0f)),
                FloorConcrete = CreateTexturedMat(FloorConcreteMatPath, floorTexPath, new Color(0.93f, 0.93f, 0.91f), 0.22f, new Vector2(10f, 6f)),
                CeilingSoffit = CreateTexturedMat(CeilingSoffitMatPath, ceilingTexPath, new Color(0.90f, 0.91f, 0.93f), 0.10f, new Vector2(10f, 6f)),
                BaseboardConcrete = CreateColorMat(BaseboardMatPath, new Color(0.31f, 0.30f, 0.29f), 0.15f, 0.0f),
                FluorOn = CreateEmissiveMat(FluorOnMatPath, new Color(0.90f, 0.94f, 0.96f), new Color(0.84f, 0.92f, 0.98f) * 2.35f),
                FluorOff = CreateColorMat(FluorOffMatPath, new Color(0.36f, 0.38f, 0.39f), 0.42f, 0.15f),
                UtilityWarmOn = CreateEmissiveMat(UtilityWarmOnMatPath, new Color(0.98f, 0.88f, 0.72f), new Color(0.96f, 0.78f, 0.50f) * 2.15f),
                IndustrialMetal = CreateColorMat(IndustrialMetalMatPath, new Color(0.26f, 0.27f, 0.28f), 0.36f, 0.68f),
                CeilingVoid = CreateColorMat(CeilingVoidMatPath, new Color(0.05f, 0.055f, 0.06f), 0.08f, 0.0f),
                ElevatorDoorMetal = CreateColorMat(ElevatorDoorMetalMatPath, new Color(0.46f, 0.48f, 0.50f), 0.48f, 0.72f),
                RoomPlaqueMats = plaqueMats,
                PipeCopperRust = CreateColorMat(PipeCopperRustMatPath, new Color(0.38f, 0.25f, 0.19f), 0.34f, 0.58f),
                UnfinishedWoodDoor = CreateColorMat(UnfinishedWoodDoorMatPath, new Color(0.56f, 0.48f, 0.39f), 0.18f, 0.0f),
                AgedCeramicPatch = CreateColorMat(AgedCeramicPatchMatPath, new Color(0.64f, 0.60f, 0.55f), 0.32f, 0.0f),
                SignHotelName = CreateTexturedMat(SignHotelNameMatPath, signHotelTex, Color.white, 0.26f, Vector2.one),
                SignReception = CreateTexturedMat(SignReceptionMatPath, signReceptionTex, Color.white, 0.26f, Vector2.one),
                SignElevators = CreateTexturedMat(SignElevatorsMatPath, signElevatorsTex, Color.white, 0.30f, Vector2.one),
                SignElevatorNotice = CreateTexturedMat(SignElevatorNoticeMatPath, signElevNoticeTex, Color.white, 0.24f, Vector2.one),
                SignGuestRooms = CreateTexturedMat(SignGuestRoomsMatPath, signGuestRoomsTex, Color.white, 0.30f, Vector2.one),
                SignService = CreateTexturedMat(SignServiceMatPath, signServiceTex, Color.white, 0.24f, Vector2.one),
                SignMaintenance = CreateTexturedMat(SignMaintenanceMatPath, signMaintTex, Color.white, 0.24f, Vector2.one)
            };
            return mats;
        }

        private static void ConfigureSignTextureImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                bool changed = false;
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
                if (importer.maxTextureSize != 512) { importer.maxTextureSize = 512; changed = true; }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigurePlaqueTextureImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                bool changed = false;
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
                if (importer.maxTextureSize != 256) { importer.maxTextureSize = 256; changed = true; }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureArchitecturalTextureImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                bool changed = false;
                if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
                if (importer.maxTextureSize != 512) { importer.maxTextureSize = 512; changed = true; }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        [MenuItem("MyVirtualCommunity/Build Backrooms Level 1 Scene")]
        public static void BuildBackroomsLevel1Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Canonical Remanencia Nivel 0 architectural textures & materials + Reality-Bridge screen textures
            RemanenciaVisualMaterials visualMats = EnsureRemanenciaVisualAssets();
            string diarioTexPath = EnsureDiarioJuarezScreenTexture("Assets/Textures/Environment/T_Backrooms_DiarioJuarez_Screen.png");
            string laptopTexPath = EnsureLaptopMysteryScreenTexture("Assets/Textures/Environment/T_Backrooms_Laptop_Screen.png");
            string weatherTexPath = EnsureBorderTerminalScreenTexture("Assets/Textures/Environment/T_Backrooms_BorderTerminal_Screen.png");
            AssetDatabase.Refresh();

            Material wallMat = visualMats.WallConcrete;
            Material columnMat = visualMats.ColumnConcrete;
            Material carpetMat = visualMats.FloorConcrete;
            Material ceilingMat = visualMats.CeilingSoffit;
            Material baseboardMat = visualMats.BaseboardConcrete;
            Material fluorOnMat = visualMats.FluorOn;
            Material fluorOffMat = visualMats.FluorOff;
            Material voidBlackMat = visualMats.CeilingVoid;
            Material industrialMetalMat = visualMats.IndustrialMetal;

            // Reality-Bridge screen & device materials
            Material diarioScreenMat = CreateEmissiveTexturedMat("Assets/Settings/Backrooms_DiarioScreen.mat", diarioTexPath, Color.white, Color.white * 0.85f);
            Material laptopScreenMat = CreateEmissiveTexturedMat("Assets/Settings/Backrooms_LaptopScreen.mat", laptopTexPath, Color.white, Color.white * 0.90f);
            Material weatherScreenMat = CreateEmissiveTexturedMat("Assets/Settings/Backrooms_BorderTerminalScreen.mat", weatherTexPath, Color.white, new Color(0.65f, 0.95f, 1.0f) * 0.95f);
            Material alexaRingMat = CreateEmissiveMat("Assets/Settings/Backrooms_AlexaCyanRing.mat", new Color(0.10f, 0.85f, 1.0f), new Color(0.10f, 0.88f, 1.0f) * 3.0f);
            Material radioDialMat = CreateEmissiveMat("Assets/Settings/Backrooms_RadioAmberDial.mat", new Color(1.0f, 0.72f, 0.24f), new Color(1.0f, 0.68f, 0.18f) * 2.1f);
            Material walnutWoodMat = CreateColorMat("Assets/Settings/Backrooms_WalnutWood.mat", new Color(0.36f, 0.20f, 0.11f), 0.34f, 0.0f);

            // Shared artifact & furniture materials
            Material rackSteelMat = CreateColorMat("Assets/Settings/West_SecretVault_RackSteel.mat", new Color(0.14f, 0.15f, 0.17f), 0.42f, 0.72f);
            Material safeGunmetalMat = CreateColorMat("Assets/Settings/West_SecretVault_SafeGunmetal.mat", new Color(0.16f, 0.18f, 0.21f), 0.56f, 0.82f);
            Material antiqueBrassMat = CreateColorMat("Assets/Settings/West_SecretVault_AntiqueBrass.mat", new Color(0.78f, 0.61f, 0.24f), 0.66f, 0.85f);
            Material obsidianMat = CreateColorMat("Assets/Settings/West_SecretVault_Obsidian.mat", new Color(0.06f, 0.05f, 0.08f), 0.84f, 0.25f);
            Material crimsonVelvetMat = CreateColorMat("Assets/Settings/West_SecretVault_CrimsonVelvet.mat", new Color(0.46f, 0.06f, 0.10f), 0.15f, 0.0f);
            Material darkMahoganyMat = CreateColorMat("Assets/Settings/West_SecretVault_Mahogany.mat", new Color(0.30f, 0.16f, 0.09f), 0.30f, 0.0f);
            Material agedLeatherMat = CreateColorMat("Assets/Settings/West_SecretVault_AgedLeather.mat", new Color(0.35f, 0.22f, 0.13f), 0.22f, 0.0f);
            Material porcelainMat = CreateColorMat("Assets/Settings/West_SecretVault_Porcelain.mat", new Color(0.92f, 0.89f, 0.84f), 0.58f, 0.0f);
            Material saltWhiteMat = CreateColorMat("Assets/Settings/West_SecretVault_SaltWhite.mat", new Color(0.94f, 0.95f, 0.96f), 0.08f, 0.0f);
            Material corkMat = CreateColorMat("Assets/Settings/West_SecretVault_Cork.mat", new Color(0.58f, 0.42f, 0.25f), 0.12f, 0.0f);
            Material stickyYellowMat = CreateColorMat("Assets/Settings/West_SecretVault_StickyYellow.mat", new Color(0.98f, 0.88f, 0.26f), 0.18f, 0.0f);
            Material redThreadMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_RedThread.mat", new Color(0.88f, 0.12f, 0.14f), new Color(0.55f, 0.06f, 0.08f));
            Material cardboardMat = CreateColorMat("Assets/Settings/Backrooms_Cardboard.mat", new Color(0.62f, 0.48f, 0.30f), 0.10f, 0.0f);
            Material officeBeigeMat = CreateColorMat("Assets/Settings/Backrooms_OfficeBeige.mat", new Color(0.74f, 0.70f, 0.60f), 0.28f, 0.05f);
            Material chairFabricMat = CreateColorMat("Assets/Settings/Backrooms_OfficeChairFabric.mat", new Color(0.22f, 0.26f, 0.30f), 0.12f, 0.0f);
            Material waterBlueMat = CreateColorMat("Assets/Settings/Backrooms_WaterJug.mat", new Color(0.45f, 0.75f, 0.92f), 0.82f, 0.05f);
            Material doorMetalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/West_SecretWedge_DoorMetal.mat")
                                    ?? CreateColorMat("Assets/Settings/West_SecretWedge_DoorMetal.mat", new Color(0.80f, 0.79f, 0.76f), 0.22f, 0.10f);
            Material aluminumMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/West_Aluminum.mat")
                                   ?? CreateColorMat("Assets/Settings/West_Aluminum.mat", new Color(0.75f, 0.77f, 0.80f), 0.65f, 0.75f);
            Material vitrineGlassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/West_StorefrontClearGlass.mat")
                                       ?? CreateColorMat("Assets/Settings/Backrooms_GlassFallback.mat", new Color(0.28f, 0.38f, 0.44f, 0.35f), 0.90f, 0.0f);

            Material safeLedAmberMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_SafeLedAmber.mat", new Color(1.0f, 0.65f, 0.12f), new Color(1.0f, 0.62f, 0.10f) * 2.4f);
            Material crtStaticMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_CrtStatic.mat", new Color(0.68f, 0.86f, 1.0f), new Color(0.52f, 0.78f, 1.0f) * 1.9f);
            Material astrolabeCoreMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_AstrolabeCore.mat", new Color(0.32f, 0.92f, 1.0f), new Color(0.38f, 0.88f, 1.0f) * 2.8f);
            Material portalDormantMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_PortalDormant.mat", new Color(0.08f, 0.05f, 0.14f), new Color(0.26f, 0.10f, 0.48f) * 0.85f);
            Material portalActiveVortexMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_PortalActiveVortex.mat", new Color(0.22f, 0.78f, 1.0f), new Color(0.36f, 0.82f, 1.0f) * 3.1f);
            Material portalRuneVioletMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_PortalRuneViolet.mat", new Color(0.68f, 0.28f, 1.0f), new Color(0.68f, 0.28f, 1.0f) * 2.3f);
            Material turquoiseGlowMat = CreateEmissiveMat("Assets/Settings/West_SecretVault_TurquoiseGlow.mat", new Color(0.16f, 0.92f, 0.78f), new Color(0.16f, 0.92f, 0.78f) * 2.0f);

            GameObject root = new GameObject("BackroomsLevel1_Root");
            root.transform.position = Vector3.zero;

            // Master interactive vault/portal controller
            SecretRoomDimensionVaultController vaultCtrl = root.AddComponent<SecretRoomDimensionVaultController>();
            vaultCtrl.dormantMirrorMaterial = portalDormantMat;
            vaultCtrl.activeVortexMaterial = portalActiveVortexMat;

            // =========================================================================
            // 1. FLOOR, ACOUSTIC DROP CEILING & OUTER 40m x 24m PERIMETER (X=-20..+20, Z=-12..+12)
            // =========================================================================
            GameObject shellRoot = new GameObject("Backrooms_Architecture");
            shellRoot.transform.SetParent(root.transform, false);

            // Damp mustard carpet floor (top exactly at BaseY = -50.00m, full 40.6 x 24.6m slab)
            CreateBox("Backrooms_Floor", shellRoot.transform,
                new Vector3(0f, BaseY - 0.15f, 0f), new Vector3(40.6f, 0.30f, 24.6f), carpetMat, keepCollider: true);

            // Exposed cast-concrete soffit slab (bottom at CeilingY = -47.15m) + 2 unfinished ceiling service openings
            CreateBox("Backrooms_Ceiling", shellRoot.transform,
                new Vector3(0f, CeilingY + 0.10f, 0f), new Vector3(40.6f, 0.20f, 24.6f), ceilingMat, keepCollider: true);
            CreateMissingCeilingTile(shellRoot.transform, new Vector3(8.5f, CeilingY, 5.2f), voidBlackMat, industrialMetalMat);
            CreateMissingCeilingTile(shellRoot.transform, new Vector3(-6.2f, CeilingY, -1.8f), voidBlackMat, industrialMetalMat);

            // Outer 40m x 24m Perimeter Walls
            CreateWallSegment("Wall_Outer_South", shellRoot.transform, new Vector2(-20f, -12f), new Vector2(20f, -12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_North", shellRoot.transform, new Vector2(-20f,  12f), new Vector2(20f,  12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_West",  shellRoot.transform, new Vector2(-20f, -12f), new Vector2(-20f, 12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_East",  shellRoot.transform, new Vector2( 20f, -12f), new Vector2( 20f, 12f), 0.26f, wallMat, baseboardMat);

            // =========================================================================
            // 2. REMANENCIA NIVEL 0 — HOTEL DE LAS PALOMAS GROUND-FLOOR ARCHITECTURE
            //    Entrance Vestibule -> Open Reception/Lobby -> Twin Elevator Core ->
            //    Double-Loaded Guest-Room Corridor (Rooms 101-106) -> Back-of-House Service Wing
            // =========================================================================
            // Preserved structural anchor walls for Entry Vestibule, SE Service Pier, Service Bulkheads, NW Vault & SW Portal
            CreateWallSegment("Wall_NE_ArrivalSouth", shellRoot.transform, new Vector2(16.2f, 5.4f), new Vector2(20.0f, 5.4f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_NWVault_East", shellRoot.transform, new Vector2(-12.5f, 4.8f), new Vector2(-12.5f, 9.4f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_NWVault_SouthW", shellRoot.transform, new Vector2(-20.0f, 4.8f), new Vector2(-17.8f, 4.8f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_NWVault_SouthE", shellRoot.transform, new Vector2(-15.0f, 4.8f), new Vector2(-12.5f, 4.8f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_EastWing_MidBaffle", shellRoot.transform, new Vector2(14.8f, -4.4f), new Vector2(14.8f, -1.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWLab_NorthW", shellRoot.transform, new Vector2(-12.5f, -4.0f), new Vector2(-9.8f, -4.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_WestCorr_East", shellRoot.transform, new Vector2(-12.5f, -1.0f), new Vector2(-12.5f, 2.2f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_WestCorr_CrtBaffle", shellRoot.transform, new Vector2(-16.5f, -1.8f), new Vector2(-16.5f, 1.8f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWPortal_EastN", shellRoot.transform, new Vector2(-12.5f, -6.4f), new Vector2(-12.5f, -4.0f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_SWPortal_EastS", shellRoot.transform, new Vector2(-12.5f, -12.0f), new Vector2(-12.5f, -9.2f), 0.22f, visualMats.WallPlaster, baseboardMat);
            CreateWallSegment("Wall_SWPortal_North", shellRoot.transform, new Vector2(-20.0f, -4.0f), new Vector2(-15.5f, -4.0f), 0.22f, visualMats.WallPlaster, baseboardMat);

            // --- 13 REINFORCED-CONCRETE STRUCTURAL COLUMNS (Preserved Structural Grid) ---
            float[] pillarXs = new float[] { -9.5f, -5.5f, -1.5f, 2.5f };
            float[] pillarZs = new float[] { -1.8f, 1.4f, 4.0f };
            int pIdx = 0;
            for (int ix = 0; ix < pillarXs.Length; ix++)
            {
                for (int iz = 0; iz < pillarZs.Length; iz++)
                {
                    if (ix == 3 && iz == 0) continue;
                    string pName = (ix == 2 && iz == 1) ? "Pillar_CentralHall_Phone" : $"Pillar_CentralHall_{pIdx++}";
                    CreatePillar(pName, shellRoot.transform, pillarXs[ix], pillarZs[iz], 0.68f, columnMat, baseboardMat);
                }
            }
            CreatePillar("Pillar_EastHall_Mid", shellRoot.transform, 12.6f, -1.4f, 0.64f, columnMat, baseboardMat);
            CreatePillar("Pillar_SouthLab_Mid", shellRoot.transform, -9.6f, -8.2f, 0.64f, columnMat, baseboardMat);

            // Build canonical Hotel de las Palomas ground-floor architecture
            BuildOrUpdateHotelGroundFloorArchitecture(shellRoot.transform, visualMats);
            ApplyWallSurfaceVariation(shellRoot.transform, visualMats.WallConcrete, visualMats.WallPlaster);

            // =========================================================================
            // 3. REMANENCIA INDUSTRIAL CONSTRUCTION LIGHTING (24 FIXTURES, 16 POINT LIGHTS)
            //    Mix of exposed linear strips, hooded shop fixtures & caged utility lights.
            // =========================================================================
            GameObject lightsRoot = new GameObject("Backrooms_FluorescentLights");
            lightsRoot.transform.SetParent(root.transform, false);

            for (int i = 0; i < CanonicalFixtures.Length; i++)
            {
                var fx = CanonicalFixtures[i];
                GameObject panel = CreateBox($"Backrooms_FluorPanel_{i}", lightsRoot.transform,
                    new Vector3(fx.x, CeilingY - 0.072f, fx.z), new Vector3(1.36f, 0.046f, 0.068f),
                    fx.lit ? fluorOnMat : fluorOffMat, keepCollider: false);
                CreateBox($"Backrooms_FluorFrame_{i}", panel.transform,
                    new Vector3(0f, 1.12f, 0f), new Vector3(1.05f, 0.85f, 1.65f), industrialMetalMat, keepCollider: false);

                if (fx.lit && fx.spawnLight)
                {
                    GameObject lGo = new GameObject($"Backrooms_FluorLight_{i}");
                    lGo.transform.SetParent(panel.transform, false);
                    lGo.transform.localPosition = new Vector3(0f, -2.4f, 0f);
                    Light l = lGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = fx.style == IndustrialFixtureStyle.CagedUtility ? WarmUtilityLightColor : CoolIndustrialLightColor;
                    l.intensity = fx.intensity;
                    l.range = 10.2f;
                    l.shadows = LightShadows.None;

                    if (fx.flicker)
                    {
                        var fl = panel.AddComponent<BackroomsAmbientFlicker>();
                        fl.targetLight = l;
                        fl.panelRenderer = panel.GetComponent<Renderer>();
                        fl.baseIntensity = fx.intensity;
                        fl.flickerStrength = 0.26f;
                        fl.phaseOffset = i * 1.7f;
                    }
                }

                ApplyIndustrialFixtureStyling(panel.transform, i, fx, visualMats);
            }

            // =========================================================================
            // 4. ARRIVAL SPAWN POINT & DOUBLE METAL EXIT DOOR (NE CORNER — AV. JUÁREZ SIDE!)
            // =========================================================================
            GameObject spawnPoint = new GameObject("Backrooms_SpawnPoint");
            spawnPoint.transform.SetParent(root.transform, false);
            spawnPoint.transform.position = new Vector3(17.80f, BaseY + 0.06f, 8.80f);
            spawnPoint.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Facing West (-X) down the 40m maze!

            GameObject exitDoorRoot = new GameObject("Backrooms_ExitDoor_ToStreet");
            exitDoorRoot.transform.SetParent(root.transform, false);
            exitDoorRoot.transform.position = new Vector3(19.86f, BaseY, 8.80f);
            exitDoorRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Mounted flush on East outer wall facing -X
            CreateBox("ExitDoor_Frame_L", exitDoorRoot.transform, new Vector3(-0.68f, 1.04f, 0f), new Vector3(0.08f, 2.08f, 0.12f), rackSteelMat, keepCollider: false);
            CreateBox("ExitDoor_Frame_R", exitDoorRoot.transform, new Vector3(0.68f, 1.04f, 0f), new Vector3(0.08f, 2.08f, 0.12f), rackSteelMat, keepCollider: false);
            CreateBox("ExitDoor_Frame_T", exitDoorRoot.transform, new Vector3(0f, 2.08f, 0f), new Vector3(1.44f, 0.08f, 0.12f), rackSteelMat, keepCollider: false);
            CreateBox("ExitDoor_Leaf_L", exitDoorRoot.transform, new Vector3(-0.32f, 1.02f, 0.02f), new Vector3(0.62f, 2.02f, 0.05f), doorMetalMat, keepCollider: false);
            CreateBox("ExitDoor_Leaf_R", exitDoorRoot.transform, new Vector3(0.32f, 1.02f, 0.02f), new Vector3(0.62f, 2.02f, 0.05f), doorMetalMat, keepCollider: false);
            CreateBox("ExitDoor_CenterSeam", exitDoorRoot.transform, new Vector3(0f, 1.02f, 0.05f), new Vector3(0.02f, 2.02f, 0.02f), obsidianMat, keepCollider: false);
            CreateBox("ExitDoor_Handle", exitDoorRoot.transform, new Vector3(0.08f, 1.02f, 0.07f), new Vector3(0.03f, 0.16f, 0.05f), aluminumMat, keepCollider: false);
            BoxCollider exitTrig = exitDoorRoot.AddComponent<BoxCollider>();
            exitTrig.center = new Vector3(0f, 1.10f, 0.12f);
            exitTrig.size = new Vector3(1.40f, 2.15f, 0.35f);
            exitTrig.isTrigger = true;
            exitDoorRoot.AddComponent<BackroomsExitDoorTrigger>();

            // =========================================================================
            // 5. SPARSE LIMINAL BACKROOMS PROPS (Strictly NO extra desks or office chairs!)
            // =========================================================================
            GameObject propsRoot = new GameObject("Backrooms_OrdinaryLiminalProps");
            propsRoot.transform.SetParent(root.transform, false);

            // Wooden step-ladder abandoned in the North-Central Corridor
            CreateStepLadder(propsRoot.transform, new Vector3(9.4f, BaseY, 7.2f), 26f, darkMahoganyMat);
            // Empty office water cooler jug in the Central Pillared Hall
            CreateWaterCooler(propsRoot.transform, new Vector3(-11.6f, BaseY, 4.4f), officeBeigeMat, waterBlueMat);
            // Sparse damp cardboard archive box stacks in distant corners
            CreateCardboardStack(propsRoot.transform, new Vector3(18.6f, BaseY, -10.8f), 14f, cardboardMat);
            CreateCardboardStack(propsRoot.transform, new Vector3(2.0f, BaseY, 10.9f), -9f, cardboardMat);
            // Red fire extinguisher mounted in East Corridor
            CreateBox("BackroomsProp_FireExtinguisher", propsRoot.transform, new Vector3(14.55f, BaseY + 0.95f, -2.2f), new Vector3(0.14f, 0.42f, 0.14f), redThreadMat, keepCollider: false);

            // =========================================================================
            // 6. REALITY-BRIDGE OBJECTS (8 FUTURE LIVE-API HOOKS TO THE REAL WORLD!)
            //    Includes strictly the ONLY workstation (1 desk + 1 laptop + 1 chair)
            // =========================================================================
            GameObject realityRoot = new GameObject("Backrooms_RealityBridgeObjects");
            realityRoot.transform.SetParent(root.transform, false);

            // 6A. THE LONE WORKSTATION: 1 Desk + 1 Laptop + 1 Chair (in a surreal random alcove!)
            BuildLoneLaptopWorkstation(realityRoot.transform, new Vector3(7.90f, BaseY, -2.70f), 90f,
                officeBeigeMat, rackSteelMat, chairFabricMat, aluminumMat, laptopScreenMat, obsidianMat);

            // 6B. Vintage FM Radio on a small side table (Future paranormal podcast / real FM stream API)
            BuildFmRadioTable(realityRoot.transform, new Vector3(14.00f, BaseY, 2.20f), -90f,
                walnutWoodMat, antiqueBrassMat, aluminumMat, obsidianMat, radioDialMat);

            // 6C. Smart Speaker "Alexa" on a wooden corner cabinet (Future voice/music/podcast API)
            BuildAlexaCornerStand(realityRoot.transform, new Vector3(-6.45f, BaseY, -3.45f), 45f,
                walnutWoodMat, obsidianMat, alexaRingMat);

            // 6D. iPad Displaying "El Diario de Juárez" on floor stand (Future live daily news API)
            BuildDiarioJuarezIpadStand(realityRoot.transform, new Vector3(4.50f, BaseY, 8.80f), 180f,
                aluminumMat, obsidianMat, diarioScreenMat);

            // 6E. [Creative Bridge #1] Live Juárez Weather, Clock & Border Wait-Time Wall Terminal
            BuildBorderClockTerminal(realityRoot.transform, new Vector3(-11.10f, BaseY + 1.52f, -4.16f), 180f,
                rackSteelMat, weatherScreenMat, alexaRingMat);

            // 6F. [Creative Bridge #2] 90s Stainless-Steel Public Wall Payphone (Future AI/Voice mystery calls)
            BuildWallPayphoneTerminal(realityRoot.transform, new Vector3(19.72f, BaseY + 1.38f, -8.40f), -90f,
                aluminumMat, obsidianMat, redThreadMat, antiqueBrassMat);

            // 6G. [Creative Bridge #3] Wall-Mounted Street-Cam Digital Frame (Future Fideicomiso live feed snapshot)
            BuildStreetCamPhotoFrame(realityRoot.transform, new Vector3(-16.32f, BaseY + 1.50f, 0.20f), 90f,
                rackSteelMat, crtStaticMat, aluminumMat);

            // 6H. [Creative Bridge #4] Thermal Receipt Teletype Printer on Pedestal (Future live reports/codes)
            BuildThermalPrinterPedestal(realityRoot.transform, new Vector3(0.40f, BaseY, -9.80f), 32f,
                darkMahoganyMat, officeBeigeMat, saltWhiteMat, obsidianMat, alexaRingMat);

            // =========================================================================
            // 7. ORIGINAL ARCHIVAL & ANOMALOUS ARTIFACTS DISPERSED OVER 960 m²
            // =========================================================================
            GameObject artifactsRoot = new GameObject("Backrooms_DispersedArtifacts");
            artifactsRoot.transform.SetParent(root.transform, false);

            // 1. Evidence Corkboard PDN-00 (Safe Code Clue 654321 -> points to NW Vault!) in South-Central Alcove
            BuildEvidenceCorkboard(artifactsRoot.transform, new Vector3(4.00f, BaseY + 1.48f, -11.78f), Quaternion.Euler(0f, -90f, 0f),
                darkMahoganyMat, corkMat, porcelainMat, astrolabeCoreMat, stickyYellowMat, redThreadMat);

            // 2. Shift Clock 03:07 on a narrow wooden pedestal in SE Corridor
            CreateBox("Clock307_Pedestal", propsRoot.transform, new Vector3(11.40f, BaseY + 0.45f, -11.45f), new Vector3(0.42f, 0.90f, 0.42f), darkMahoganyMat, keepCollider: true);
            BuildShiftClock0307(artifactsRoot.transform, new Vector3(11.40f, BaseY + 0.98f, -11.45f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, antiqueBrassMat, porcelainMat, obsidianMat);

            // 3. Notebook of Sealed Pages resting on the damp carpet in quiet NE Dead-End Alcove
            BuildSealedNotebook(artifactsRoot.transform, new Vector3(9.80f, BaseY + 0.04f, 10.60f), Quaternion.Euler(0f, 128f, 0f),
                agedLeatherMat, porcelainMat, antiqueBrassMat, darkMahoganyMat);

            // 4. Unwired Service Intercom mounted on Pillar_CentralHall_Phone (-1.5, +1.4) facing East (+X)
            BuildServiceIntercom(artifactsRoot.transform, new Vector3(-1.12f, BaseY + 1.42f, 1.40f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, aluminumMat, obsidianMat, antiqueBrassMat, safeLedAmberMat);

            // 5. Unsourced Signal Monitor on rolling AV cart in West Niche (-18.9, -1.6)
            CreateBox("CrtTv_RollingCart", propsRoot.transform, new Vector3(-18.90f, BaseY + 0.34f, -1.60f), new Vector3(0.54f, 0.68f, 0.68f), rackSteelMat, keepCollider: true);
            BuildSignalMonitor(artifactsRoot.transform, new Vector3(-18.88f, BaseY + 0.68f, -1.60f), Quaternion.Euler(0f, 90f, 0f),
                safeGunmetalMat, crtStaticMat, aluminumMat, obsidianMat);

            // 6. Anomalous Astral Compass on a lone pedestal in Central Pillared Hall (points SW toward Portal!)
            CreateBox("Compass_SideTable", propsRoot.transform, new Vector3(2.50f, BaseY + 0.42f, -1.40f), new Vector3(0.48f, 0.84f, 0.48f), darkMahoganyMat, keepCollider: true);
            BuildAnomalousCompass(artifactsRoot.transform, new Vector3(2.50f, BaseY + 0.86f, -1.40f), Quaternion.Euler(0f, -112f, 0f),
                antiqueBrassMat, astrolabeCoreMat);

            // 7. Specimen Vitrine: Featureless Figure centered inside North-Central Containment Room (-5.2, +10.4)
            CreateBox("SpecimenVitrine_Pedestal", propsRoot.transform, new Vector3(-5.20f, BaseY + 0.46f, 10.40f), new Vector3(0.54f, 0.92f, 0.48f), darkMahoganyMat, keepCollider: true);
            BuildSpecimenVitrine(artifactsRoot.transform, new Vector3(-5.20f, BaseY + 0.92f, 10.40f), Quaternion.Euler(0f, 180f, 0f),
                darkMahoganyMat, vitrineGlassMat, porcelainMat, obsidianMat, antiqueBrassMat);

            // 8. Articulated Wooden Assembly resting on a lone archive crate in West Corridor
            CreateBox("Archive_WoodenCrate", propsRoot.transform, new Vector3(-11.20f, BaseY + 0.28f, 3.00f), new Vector3(0.54f, 0.56f, 0.54f), cardboardMat, keepCollider: true);
            BuildArticulatedWoodAssembly(artifactsRoot.transform, new Vector3(-11.20f, BaseY + 0.56f, 3.00f), Quaternion.Euler(0f, 145f, 0f),
                walnutWoodMat, darkMahoganyMat, antiqueBrassMat);

            // 9. Archive Case 06 resting on archive boxes in Far-North Corridor (+1.85, +10.9)
            BuildArchiveCase06(artifactsRoot.transform, new Vector3(1.85f, BaseY + 0.92f, 10.90f), Quaternion.Euler(0f, 175f, 0f),
                rackSteelMat, officeBeigeMat, aluminumMat, obsidianMat);

            // 10. Sealed Custody Case resting on the damp carpet in South-West Labyrinth
            BuildSealedCustodyCase(artifactsRoot.transform, new Vector3(-8.40f, BaseY + 0.02f, -10.40f), Quaternion.Euler(0f, -25f, 0f),
                darkMahoganyMat, safeGunmetalMat, rackSteelMat, aluminumMat);

            // 11. Fractured Ceramic Insulator on a dark pedestal in South-West Corridor (-4.0, -10.8)
            CreateBox("Insulator_Pedestal", propsRoot.transform, new Vector3(-4.00f, BaseY + 0.44f, -10.80f), new Vector3(0.44f, 0.88f, 0.44f), obsidianMat, keepCollider: true);
            BuildFracturedInsulator(artifactsRoot.transform, new Vector3(-4.00f, BaseY + 0.88f, -10.80f), Quaternion.Euler(0f, 180f, 0f),
                porcelainMat, obsidianMat, antiqueBrassMat, rackSteelMat);

            // 12. Coordinate Plotting Plate on a low survey table in South-Central Alcove (+6.5, -11.1)
            CreateBox("Survey_RelicTable", propsRoot.transform, new Vector3(6.50f, BaseY + 0.38f, -11.10f), new Vector3(0.96f, 0.76f, 0.52f), darkMahoganyMat, keepCollider: true);
            BuildCoordinatePlottingPlate(artifactsRoot.transform, new Vector3(6.50f, BaseY + 0.78f, -11.10f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, antiqueBrassMat, aluminumMat);

            // 13. Paquimé Obsidian & Turquoise Ceremonial Mask on a dark display plinth in West Corridor (-18.8, +2.6)
            CreateBox("PaquimeMask_Plinth", propsRoot.transform, new Vector3(-19.10f, BaseY + 0.54f, 2.60f), new Vector3(0.42f, 1.08f, 0.42f), obsidianMat, keepCollider: true);
            BuildPaquimeMask(artifactsRoot.transform, new Vector3(-18.98f, BaseY + 1.22f, 2.60f), Quaternion.Euler(0f, 180f, 0f),
                obsidianMat, turquoiseGlowMat, antiqueBrassMat);

            // --- ZONE NW: LEVEL-OMEGA VAULT SAFE ROOM (X=-20..-12.5, Z=+4.8..+12) ---
            // Heavy Level-Omega Vault Safe ("654321") holding "El Ojo del Umbral" against North wall!
            BuildVaultSafeWithAstrolabe(
                artifactsRoot.transform,
                vaultCtrl,
                new Vector3(-16.40f, BaseY, 11.42f),
                Quaternion.Euler(0f, 180f, 0f), // Faces South (-Z) into NW Vault Room!
                darkCorniceMat: rackSteelMat,
                safeGunmetalMat: safeGunmetalMat,
                crimsonVelvetMat: crimsonVelvetMat,
                obsidianMat: obsidianMat,
                antiqueBrassMat: antiqueBrassMat,
                aluminumMat: aluminumMat,
                safeLedAmberMat: safeLedAmberMat,
                astrolabeCoreMat: astrolabeCoreMat,
                portalRuneVioletMat: portalRuneVioletMat);

            // Relic Table along West Wall of NW Vault Room holding Atlas of Anomalous Geometry & Folded Topology Core
            CreateBox("NWVault_RelicTable", propsRoot.transform, new Vector3(-19.20f, BaseY + 0.42f, 8.20f), new Vector3(0.68f, 0.84f, 2.20f), darkMahoganyMat, keepCollider: true);
            BuildAnomalousGeometryAtlas(artifactsRoot.transform, new Vector3(-19.15f, BaseY + 0.84f, 8.75f), Quaternion.Euler(0f, 90f, 0f),
                rackSteelMat, agedLeatherMat, antiqueBrassMat, aluminumMat, safeLedAmberMat);
            BuildFoldedTopologyCore(artifactsRoot.transform, new Vector3(-19.15f, BaseY + 0.84f, 7.65f), Quaternion.Euler(0f, 90f, 0f),
                safeGunmetalMat, porcelainMat, aluminumMat, antiqueBrassMat);

            // =========================================================================
            // 8. ZONE SW: FAR DEEP PORTAL CHAMBER (X=-20..-12.5, Z=-12..-4.0) —
            //    THE SINGLE MONUMENTAL DIMENSIONAL MIRROR-PORTAL ("EL UMBRAL DE LASSER")
            //    Placed ~40 meters diagonally away from the NE entrance!
            // =========================================================================
            BuildMonumentalDimensionalPortal(
                artifactsRoot.transform,
                vaultCtrl,
                new Vector3(-19.70f, BaseY, -8.20f),
                Quaternion.identity, // Faces East (+X) down into the SW Portal Chamber!
                saltWhiteMat, obsidianMat, antiqueBrassMat, portalDormantMat, portalRuneVioletMat,
                astrolabeCoreMat, rackSteelMat, redThreadMat);

            vaultCtrl.RefreshVisualState();

            // Save BackroomsLevel1Scene cleanly and ensure it is registered in EditorBuildSettings
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BackroomsLevel1Builder] Built & saved 40m x 24m (960 m²) real-scale Backrooms Level 0 maze with 15 original artifacts, 8 Reality-Bridge props, NW Level-Omega Safe & far SW Monumental Portal at {ScenePath}.");
        }

        /// <summary>
        /// Targeted, idempotent IP-01 scene migration utility.
        /// Opens the EXISTING BackroomsLevel1Scene.unity without recreating the scene, replaces ONLY the
        /// 12 retired horror-franchise artifact subhierarchies under Backrooms_DispersedArtifacts, and
        /// renames the 4 franchise-named pedestals in-place while preserving every unrelated GameObject,
        /// Transform fileID, component fileID, light, architecture wall, safe, portal, and material asset.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate IP-01 Backrooms Artifacts (In-Place)")]
        public static void MigrateIp01ArtifactsAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateIp01ArtifactsInOpenScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Targeted IP-01 in-place migration completed and saved to {ScenePath}.");
        }

        private static readonly HashSet<string> MigratedArtifactRootNames = new HashSet<string>(StringComparer.Ordinal)
        {
            // Retired prototype artifact roots
            "Artifact_Clock307",
            "Artifact_BabadookBook",
            "Artifact_BlackRotaryPhone",
            "Artifact_CrtTv_Root",
            "Artifact_DollGlassCase",
            "Artifact_SpiralPuppet",
            "Artifact_PossessedDollBox",
            "Artifact_DybbukBox_Group",
            "Artifact_NecronomiconGrimoire",
            "Artifact_CeramicHand",
            "Artifact_LamentPuzzleBox",
            "Artifact_OuijaBoard",
            // Canonical original replacement artifact roots (removed & rebuilt for idempotency)
            "Artifact_ShiftClock0307",
            "Artifact_SealedNotebook",
            "Artifact_ServiceIntercom",
            "Artifact_SignalMonitor_Root",
            "Artifact_SpecimenVitrine",
            "Artifact_ArticulatedWoodAssembly",
            "Artifact_ArchiveCase06",
            "Artifact_SealedCustodyCase",
            "Artifact_FracturedInsulator",
            "Artifact_CoordinatePlottingPlate",
            "Artifact_AnomalousGeometryAtlas",
            "Artifact_FoldedTopologyCore"
        };

        private static readonly Dictionary<string, string> PedestalRenameMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DollVitrine_Pedestal"] = "SpecimenVitrine_Pedestal",
            ["Billy_WoodenCrate"] = "Archive_WoodenCrate",
            ["CeramicHand_Pedestal"] = "Insulator_Pedestal",
            ["Ouija_RelicTable"] = "Survey_RelicTable"
        };

        public static void MigrateIp01ArtifactsInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject root = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "BackroomsLevel1_Root")
                {
                    root = go;
                    break;
                }
            }
            if (root == null)
            {
                throw new InvalidOperationException("BackroomsLevel1_Root not found in open scene.");
            }

            Transform artifactsRoot = root.transform.Find("Backrooms_DispersedArtifacts");
            Transform propsRoot = root.transform.Find("Backrooms_OrdinaryLiminalProps");
            if (artifactsRoot == null || propsRoot == null)
            {
                throw new InvalidOperationException("Expected Backrooms_DispersedArtifacts and Backrooms_OrdinaryLiminalProps under BackroomsLevel1_Root.");
            }

            // Load existing shared materials read-only (never SetDirty or SaveAssets on unrelated .mat files)
            Material rackSteelMat = RequireExistingMat("Assets/Settings/West_SecretVault_RackSteel.mat");
            Material safeGunmetalMat = RequireExistingMat("Assets/Settings/West_SecretVault_SafeGunmetal.mat");
            Material antiqueBrassMat = RequireExistingMat("Assets/Settings/West_SecretVault_AntiqueBrass.mat");
            Material obsidianMat = RequireExistingMat("Assets/Settings/West_SecretVault_Obsidian.mat");
            Material darkMahoganyMat = RequireExistingMat("Assets/Settings/West_SecretVault_Mahogany.mat");
            Material walnutWoodMat = RequireExistingMat("Assets/Settings/Backrooms_WalnutWood.mat");
            Material agedLeatherMat = RequireExistingMat("Assets/Settings/West_SecretVault_AgedLeather.mat");
            Material porcelainMat = RequireExistingMat("Assets/Settings/West_SecretVault_Porcelain.mat");
            Material officeBeigeMat = RequireExistingMat("Assets/Settings/Backrooms_OfficeBeige.mat");
            Material aluminumMat = RequireExistingMat("Assets/Settings/West_Aluminum.mat");
            Material vitrineGlassMat = RequireExistingMat("Assets/Settings/West_StorefrontClearGlass.mat");
            Material safeLedAmberMat = RequireExistingMat("Assets/Settings/West_SecretVault_SafeLedAmber.mat");
            Material crtStaticMat = RequireExistingMat("Assets/Settings/West_SecretVault_CrtStatic.mat");

            // 1. Rename the 4 franchise-named pedestals in-place (preserving their existing fileIDs and transforms)
            for (int i = 0; i < propsRoot.childCount; i++)
            {
                Transform child = propsRoot.GetChild(i);
                if (PedestalRenameMap.TryGetValue(child.name, out string cleanName))
                {
                    child.gameObject.name = cleanName;
                }
            }

            // 2. Remove ONLY the 12 retired (or previously migrated) artifact children under Backrooms_DispersedArtifacts
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < artifactsRoot.childCount; i++)
            {
                Transform child = artifactsRoot.GetChild(i);
                if (MigratedArtifactRootNames.Contains(child.name))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            for (int i = 0; i < toDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(toDestroy[i]);
            }

            // 3. Build the 12 original archival & anomalous artifacts at their exact existing world positions & rotations
            BuildShiftClock0307(artifactsRoot, new Vector3(11.40f, BaseY + 0.98f, -11.45f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, antiqueBrassMat, porcelainMat, obsidianMat);

            BuildSealedNotebook(artifactsRoot, new Vector3(9.80f, BaseY + 0.04f, 10.60f), Quaternion.Euler(0f, 128f, 0f),
                agedLeatherMat, porcelainMat, antiqueBrassMat, darkMahoganyMat);

            BuildServiceIntercom(artifactsRoot, new Vector3(-1.12f, BaseY + 1.42f, 1.40f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, aluminumMat, obsidianMat, antiqueBrassMat, safeLedAmberMat);

            BuildSignalMonitor(artifactsRoot, new Vector3(-18.88f, BaseY + 0.68f, -1.60f), Quaternion.Euler(0f, 90f, 0f),
                safeGunmetalMat, crtStaticMat, aluminumMat, obsidianMat);

            BuildSpecimenVitrine(artifactsRoot, new Vector3(-5.20f, BaseY + 0.92f, 10.40f), Quaternion.Euler(0f, 180f, 0f),
                darkMahoganyMat, vitrineGlassMat, porcelainMat, obsidianMat, antiqueBrassMat);

            BuildArticulatedWoodAssembly(artifactsRoot, new Vector3(-11.20f, BaseY + 0.56f, 3.00f), Quaternion.Euler(0f, 145f, 0f),
                walnutWoodMat, darkMahoganyMat, antiqueBrassMat);

            BuildArchiveCase06(artifactsRoot, new Vector3(1.85f, BaseY + 0.92f, 10.90f), Quaternion.Euler(0f, 175f, 0f),
                rackSteelMat, officeBeigeMat, aluminumMat, obsidianMat);

            BuildSealedCustodyCase(artifactsRoot, new Vector3(-8.40f, BaseY + 0.02f, -10.40f), Quaternion.Euler(0f, -25f, 0f),
                darkMahoganyMat, safeGunmetalMat, rackSteelMat, aluminumMat);

            BuildFracturedInsulator(artifactsRoot, new Vector3(-4.00f, BaseY + 0.88f, -10.80f), Quaternion.Euler(0f, 180f, 0f),
                porcelainMat, obsidianMat, antiqueBrassMat, rackSteelMat);

            BuildCoordinatePlottingPlate(artifactsRoot, new Vector3(6.50f, BaseY + 0.78f, -11.10f), Quaternion.Euler(0f, 180f, 0f),
                safeGunmetalMat, antiqueBrassMat, aluminumMat);

            BuildAnomalousGeometryAtlas(artifactsRoot, new Vector3(-19.15f, BaseY + 0.84f, 8.75f), Quaternion.Euler(0f, 90f, 0f),
                rackSteelMat, agedLeatherMat, antiqueBrassMat, aluminumMat, safeLedAmberMat);

            BuildFoldedTopologyCore(artifactsRoot, new Vector3(-19.15f, BaseY + 0.84f, 7.65f), Quaternion.Euler(0f, 90f, 0f),
                safeGunmetalMat, porcelainMat, aluminumMat, antiqueBrassMat);
        }

        private static Material RequireExistingMat(string assetPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                throw new InvalidOperationException($"Required existing material asset missing at {assetPath}.");
            }
            return mat;
        }

        /// <summary>
        /// Targeted, idempotent Remanencia Nivel 0 (Hotel de las Palomas) visual migration utility.
        /// Opens the existing BackroomsLevel1Scene.unity without recreating the scene or root hierarchy,
        /// re-authors the exclusive architectural textures & materials in place (preserving filenames and
        /// .meta GUIDs), updates structural walls, columns, ceiling openings, and industrial fixtures
        /// in place, and saves only BackroomsLevel1Scene.unity and its target-specific visual assets.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Nivel 0 Visuals (In-Place)")]
        public static void MigrateRemanenciaLevel0VisualsAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaLevel0VisualsInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Nivel 0 in-place visual migration completed and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaLevel0VisualsInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia visual migration requires open scene '{ScenePath}', but found '{scene.path}'.");
            }

            GameObject root = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "BackroomsLevel1_Root")
                {
                    root = go;
                    break;
                }
            }
            if (root == null)
            {
                throw new InvalidOperationException("BackroomsLevel1_Root not found in open scene.");
            }

            Transform shellRoot = root.transform.Find("Backrooms_Architecture");
            Transform lightsRoot = root.transform.Find("Backrooms_FluorescentLights");
            if (shellRoot == null || lightsRoot == null)
            {
                throw new InvalidOperationException("Expected Backrooms_Architecture and Backrooms_FluorescentLights under BackroomsLevel1_Root.");
            }

            // 1. Re-author exclusive target textures & materials in place (GUIDs preserved) + ensure Remanencia-specific materials
            RemanenciaVisualMaterials mats = EnsureRemanenciaVisualAssets();

            // 2. Migrate architectural surfaces under Backrooms_Architecture in place (preserving all GameObjects, Transforms & Colliders)
            Transform floorTf = shellRoot.Find("Backrooms_Floor");
            if (floorTf != null) SetSharedMatPreservingRendererSettings(floorTf.gameObject, mats.FloorConcrete);

            Transform ceilingTf = shellRoot.Find("Backrooms_Ceiling");
            if (ceilingTf != null) SetSharedMatPreservingRendererSettings(ceilingTf.gameObject, mats.CeilingSoffit);

            ApplyWallSurfaceVariation(shellRoot, mats.WallConcrete, mats.WallPlaster);

            for (int i = 0; i < shellRoot.childCount; i++)
            {
                Transform child = shellRoot.GetChild(i);
                string n = child.name;

                if (n.EndsWith("_Baseboard", StringComparison.Ordinal))
                {
                    SetSharedMatPreservingRendererSettings(child.gameObject, mats.BaseboardConcrete);
                }
                else if (n.StartsWith("Pillar_", StringComparison.Ordinal))
                {
                    SetSharedMatPreservingRendererSettings(child.gameObject, mats.ColumnConcrete);
                }
                else if (n == "Backrooms_MissingTileVoid")
                {
                    // Repurpose existing ceiling void GameObject into recessed unfinished utility chase opening
                    Vector3 p = child.localPosition;
                    child.localPosition = new Vector3(p.x, CeilingY - 0.008f, p.z);
                    child.localScale = new Vector3(0.86f, 0.024f, 0.62f);
                    SetSharedMatPreservingRendererSettings(child.gameObject, mats.CeilingVoid);
                }
                else if (n == "Backrooms_CrookedCeilingTile")
                {
                    // Repurpose existing crooked ceiling tile GameObject into tilted galvanized industrial access hatch
                    Vector3 p = child.localPosition;
                    child.localPosition = new Vector3(p.x, CeilingY - 0.085f, p.z);
                    child.localRotation = Quaternion.Euler(11f, 4f, -15f);
                    child.localScale = new Vector3(0.78f, 0.022f, 0.56f);
                    SetSharedMatPreservingRendererSettings(child.gameObject, mats.IndustrialMetal);
                }
            }

            // 3. Migrate all 24 existing ceiling fixtures & 16 point lights in place (preserving GameObjects, lights & 5 flicker controllers)
            for (int i = 0; i < CanonicalFixtures.Length; i++)
            {
                Transform panelTf = lightsRoot.Find($"Backrooms_FluorPanel_{i}");
                if (panelTf == null)
                {
                    throw new InvalidOperationException($"Expected existing fixture Backrooms_FluorPanel_{i} under Backrooms_FluorescentLights.");
                }
                ApplyIndustrialFixtureStyling(panelTf, i, CanonicalFixtures[i], mats);
            }
        }

        private static void ApplyWallSurfaceVariation(Transform shellRoot, Material concreteWallMat, Material plasterWallMat)
        {
            for (int i = 0; i < shellRoot.childCount; i++)
            {
                Transform child = shellRoot.GetChild(i);
                string n = child.name;
                if (!n.StartsWith("Wall_", StringComparison.Ordinal) || n.EndsWith("_Baseboard", StringComparison.Ordinal))
                {
                    continue;
                }
                Material targetMat = PlasterPartitionWallNames.Contains(n) ? plasterWallMat : concreteWallMat;
                SetSharedMatPreservingRendererSettings(child.gameObject, targetMat);
            }
        }

        private static void ApplyIndustrialFixtureStyling(
            Transform panelTf,
            int fixtureIndex,
            (float x, float z, bool lit, bool spawnLight, bool flicker, float intensity, IndustrialFixtureStyle style) fx,
            RemanenciaVisualMaterials mats)
        {
            Material emitterMat;
            if (!fx.lit)
            {
                emitterMat = mats.FluorOff;
            }
            else if (fx.style == IndustrialFixtureStyle.CagedUtility)
            {
                emitterMat = mats.UtilityWarmOn;
            }
            else
            {
                emitterMat = mats.FluorOn;
            }

            Transform frameTf = panelTf.Find($"Backrooms_FluorFrame_{fixtureIndex}");
            Transform lightTf = panelTf.Find($"Backrooms_FluorLight_{fixtureIndex}");

            // Remove only non-matching Remanencia_* detail children if a fixture ever changed style; reuse matching children for idempotency
            if (fx.style == IndustrialFixtureStyle.LinearStrip)
            {
                panelTf.localPosition = new Vector3(fx.x, CeilingY - 0.072f, fx.z);
                panelTf.localRotation = Quaternion.identity;
                panelTf.localScale = new Vector3(1.36f, 0.046f, 0.068f);
                SetSharedMatPreservingRendererSettings(panelTf.gameObject, emitterMat);

                if (frameTf != null)
                {
                    frameTf.localPosition = new Vector3(0f, 1.12f, 0f);
                    frameTf.localRotation = Quaternion.identity;
                    frameTf.localScale = new Vector3(1.05f, 0.85f, 1.65f);
                    SetSharedMatPreservingRendererSettings(frameTf.gameObject, mats.IndustrialMetal);
                }

                DestroyStaleFixtureChildrenExcept(panelTf, "Remanencia_EndSocket_L", "Remanencia_EndSocket_R");
                EnsureFixtureDetailChild(panelTf, "Remanencia_EndSocket_L", new Vector3(-0.505f, 0.25f, 0f), new Vector3(0.028f, 1.55f, 1.40f), mats.IndustrialMetal);
                EnsureFixtureDetailChild(panelTf, "Remanencia_EndSocket_R", new Vector3( 0.505f, 0.25f, 0f), new Vector3(0.028f, 1.55f, 1.40f), mats.IndustrialMetal);

                if (lightTf != null)
                {
                    lightTf.localPosition = new Vector3(0f, -2.60f, 0f);
                }
            }
            else if (fx.style == IndustrialFixtureStyle.HoodedStrip)
            {
                panelTf.localPosition = new Vector3(fx.x, CeilingY - 0.088f, fx.z);
                panelTf.localRotation = Quaternion.identity;
                panelTf.localScale = new Vector3(1.32f, 0.048f, 0.14f);
                SetSharedMatPreservingRendererSettings(panelTf.gameObject, emitterMat);

                if (frameTf != null)
                {
                    frameTf.localPosition = new Vector3(0f, 1.10f, 0f);
                    frameTf.localRotation = Quaternion.identity;
                    frameTf.localScale = new Vector3(1.06f, 0.72f, 2.25f);
                    SetSharedMatPreservingRendererSettings(frameTf.gameObject, mats.IndustrialMetal);
                }

                DestroyStaleFixtureChildrenExcept(panelTf, "Remanencia_DropRod_L", "Remanencia_DropRod_R");
                EnsureFixtureDetailChild(panelTf, "Remanencia_DropRod_L", new Vector3(-0.42f, 1.55f, 0f), new Vector3(0.024f, 0.95f, 0.24f), mats.IndustrialMetal);
                EnsureFixtureDetailChild(panelTf, "Remanencia_DropRod_R", new Vector3( 0.42f, 1.55f, 0f), new Vector3(0.024f, 0.95f, 0.24f), mats.IndustrialMetal);

                if (lightTf != null)
                {
                    lightTf.localPosition = new Vector3(0f, -2.40f, 0f);
                }
            }
            else // IndustrialFixtureStyle.CagedUtility
            {
                panelTf.localPosition = new Vector3(fx.x, CeilingY - 0.145f, fx.z);
                panelTf.localRotation = Quaternion.identity;
                panelTf.localScale = new Vector3(0.17f, 0.17f, 0.17f);
                SetSharedMatPreservingRendererSettings(panelTf.gameObject, emitterMat);

                if (frameTf != null)
                {
                    frameTf.localPosition = new Vector3(0f, 0.68f, 0f);
                    frameTf.localRotation = Quaternion.identity;
                    frameTf.localScale = new Vector3(1.42f, 0.44f, 1.42f);
                    SetSharedMatPreservingRendererSettings(frameTf.gameObject, mats.IndustrialMetal);
                }

                DestroyStaleFixtureChildrenExcept(panelTf, "Remanencia_CageGuard_A", "Remanencia_CageGuard_B");
                EnsureFixtureDetailChild(panelTf, "Remanencia_CageGuard_A", new Vector3(0f, -0.04f, 0f), new Vector3(1.26f, 1.22f, 0.14f), mats.IndustrialMetal);
                EnsureFixtureDetailChild(panelTf, "Remanencia_CageGuard_B", new Vector3(0f, -0.04f, 0f), new Vector3(0.14f, 1.22f, 1.26f), mats.IndustrialMetal);

                if (lightTf != null)
                {
                    lightTf.localPosition = new Vector3(0f, -0.95f, 0f);
                }
            }

            // Retune existing point light without creating or removing Light components or enabling shadows
            if (lightTf != null)
            {
                Light l = lightTf.GetComponent<Light>();
                if (l != null)
                {
                    l.type = LightType.Point;
                    l.color = fx.style == IndustrialFixtureStyle.CagedUtility ? WarmUtilityLightColor : CoolIndustrialLightColor;
                    l.intensity = fx.intensity;
                    l.range = 10.2f;
                    l.shadows = LightShadows.None;
                }
            }

            // Preserve existing BackroomsAmbientFlicker assignment and sync baseIntensity to retuned light
            BackroomsAmbientFlicker flickerComp = panelTf.GetComponent<BackroomsAmbientFlicker>();
            if (flickerComp != null)
            {
                flickerComp.baseIntensity = fx.intensity;
            }
        }

        private static void DestroyStaleFixtureChildrenExcept(Transform parent, string keepA, string keepB)
        {
            var stale = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name.StartsWith("Remanencia_", StringComparison.Ordinal) &&
                    c.name != keepA && c.name != keepB)
                {
                    stale.Add(c.gameObject);
                }
            }
            for (int i = 0; i < stale.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(stale[i]);
            }
        }

        private static GameObject EnsureFixtureDetailChild(Transform parent, string childName, Vector3 localPos, Vector3 localScale, Material mat)
        {
            Transform existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = localScale;
                SetSharedMatPreservingRendererSettings(go, mat);
                Collider col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
                return go;
            }

            go = CreateBox(childName, parent, localPos, localScale, mat, keepCollider: false);
            return go;
        }

        private static void SetSharedMatPreservingRendererSettings(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// Targeted, idempotent Remanencia Nivel 0 — Hotel de las Palomas ground-floor architectural
        /// migration utility (Implementation 07.2.1).
        /// Opens the existing BackroomsLevel1Scene.unity without recreating the scene or root hierarchy,
        /// preserves all 07.2 concrete/plaster materials, lighting, 13 columns, floor, ceiling, spawn,
        /// exit, safe, portal, artifacts, and Reality-Bridge objects, removes ONLY the 19 obsolete
        /// legacy Backrooms labyrinth partitions (+ baseboards), and builds/updates the identifiable
        /// Hotel de las Palomas ground-floor architecture (Entrance Vestibule, Reception/Lobby,
        /// Twin Elevator Core, Double-Loaded Guest-Room Corridor, 6 Empty Guest-Room Shells 101-106,
        /// and Back-of-House Service Bulkhead) in place.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Hotel Architecture (In-Place)")]
        public static void MigrateRemanenciaHotelArchitectureAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaHotelArchitectureInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Hotel de las Palomas ground-floor architecture migration completed and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaHotelArchitectureInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia hotel architecture migration requires open scene '{ScenePath}', but found '{scene.path}'.");
            }

            // Ensure all 07.2 visual materials, textures, ceiling openings, columns, and industrial fixtures are current
            MigrateRemanenciaLevel0VisualsInOpenScene(scene);

            GameObject root = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "BackroomsLevel1_Root")
                {
                    root = go;
                    break;
                }
            }
            if (root == null)
            {
                throw new InvalidOperationException("BackroomsLevel1_Root not found in open scene.");
            }

            Transform shellRoot = root.transform.Find("Backrooms_Architecture");
            if (shellRoot == null)
            {
                throw new InvalidOperationException("Backrooms_Architecture not found under BackroomsLevel1_Root.");
            }

            RemanenciaVisualMaterials mats = EnsureRemanenciaVisualAssets();

            // 1. Remove ONLY the 19 explicitly identified obsolete legacy maze walls (+ their 19 baseboards)
            var obsoleteWalls = new List<GameObject>();
            for (int i = 0; i < shellRoot.childCount; i++)
            {
                Transform child = shellRoot.GetChild(i);
                string n = child.name;
                string baseName = n.EndsWith("_Baseboard", StringComparison.Ordinal)
                    ? n.Substring(0, n.Length - "_Baseboard".Length)
                    : n;
                if (ObsoleteLegacyMazeWallNames.Contains(baseName))
                {
                    obsoleteWalls.Add(child.gameObject);
                }
            }
            for (int i = 0; i < obsoleteWalls.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(obsoleteWalls[i]);
            }

            // 2. Align 2 retained architectural anchor walls in place without altering their GameObjects or fileIDs
            UpdateRetainedWallSegmentInPlace(shellRoot, "Wall_NE_ArrivalSouth", new Vector2(16.2f, 5.4f), new Vector2(20.0f, 5.4f), 0.22f, mats.WallPlaster, mats.BaseboardConcrete);
            UpdateRetainedWallSegmentInPlace(shellRoot, "Wall_EastWing_MidBaffle", new Vector2(14.8f, -4.4f), new Vector2(14.8f, -1.0f), 0.22f, mats.WallConcrete, mats.BaseboardConcrete);

            // 3. Build or idempotently update the Hotel de las Palomas ground-floor architecture
            BuildOrUpdateHotelGroundFloorArchitecture(shellRoot, mats);
            ApplyWallSurfaceVariation(shellRoot, mats.WallConcrete, mats.WallPlaster);
        }

        private static void UpdateRetainedWallSegmentInPlace(
            Transform shellRoot, string wallName, Vector2 a, Vector2 b, float thickness, Material wallMat, Material baseMat)
        {
            Vector2 delta = b - a;
            float len = delta.magnitude;
            Vector2 mid = 0.5f * (a + b);
            Quaternion rot = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.y), Vector3.up);

            Transform wallTf = shellRoot.Find(wallName);
            if (wallTf != null)
            {
                wallTf.position = new Vector3(mid.x, BaseY + ClearHeight * 0.5f, mid.y);
                wallTf.rotation = rot;
                wallTf.localScale = new Vector3(thickness, ClearHeight, len + thickness * 0.5f);
                SetSharedMatPreservingRendererSettings(wallTf.gameObject, wallMat);
            }

            Transform baseTf = shellRoot.Find(wallName + "_Baseboard");
            if (baseTf != null)
            {
                baseTf.position = new Vector3(mid.x, BaseY + 0.06f, mid.y);
                baseTf.rotation = rot;
                baseTf.localScale = new Vector3(thickness + 0.025f, 0.12f, len + thickness * 0.52f);
                SetSharedMatPreservingRendererSettings(baseTf.gameObject, baseMat);
            }
        }

        private static void BuildOrUpdateHotelGroundFloorArchitecture(Transform shellRoot, RemanenciaVisualMaterials mats)
        {
            Transform hotelRootTf = shellRoot.Find(HotelArchitectureRootName);
            if (hotelRootTf == null)
            {
                GameObject hotelRootGo = new GameObject(HotelArchitectureRootName);
                hotelRootGo.transform.SetParent(shellRoot, false);
                hotelRootTf = hotelRootGo.transform;
            }
            hotelRootTf.localPosition = Vector3.zero;
            hotelRootTf.localRotation = Quaternion.identity;
            hotelRootTf.localScale = Vector3.one;

            const float wallT = 0.22f;

            // =========================================================================
            // A. HOTEL ENTRANCE VESTIBULE (X = +16.2..+20.0, Z = +5.4..+12.0)
            //    4.0 m framed lobby portal opening straight West from spawn (17.8, 8.8)
            // =========================================================================
            Transform entryGroup = EnsureHotelSubgroup(hotelRootTf, "Hotel_Entrance_Vestibule");
            EnsureHotelWallSegment(entryGroup, "Hotel_Entrance_ArchPierS", new Vector2(16.2f, 5.4f), new Vector2(16.2f, 6.8f), 0.28f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(entryGroup, "Hotel_Entrance_ArchPierN", new Vector2(16.2f, 10.8f), new Vector2(16.2f, 12.0f), 0.28f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelBox(entryGroup, "Hotel_Entrance_ArchTransom",
                new Vector3(16.2f, CeilingY - 0.22f, 8.8f), Quaternion.identity, new Vector3(0.32f, 0.44f, 4.28f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(entryGroup, "Hotel_Entrance_PortalTrimS",
                new Vector3(16.2f, BaseY + 1.21f, 6.82f), Quaternion.identity, new Vector3(0.34f, 2.42f, 0.06f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(entryGroup, "Hotel_Entrance_PortalTrimN",
                new Vector3(16.2f, BaseY + 1.21f, 10.78f), Quaternion.identity, new Vector3(0.34f, 2.42f, 0.06f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(entryGroup, "Hotel_Entrance_PortalTrimTop",
                new Vector3(16.2f, BaseY + 2.41f, 8.80f), Quaternion.identity, new Vector3(0.34f, 0.06f, 4.02f),
                mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // B. MAIN HOTEL LOBBY & ARCHITECTURAL RECEPTION ALCOVE (NO FURNITURE!)
            //    Open public hall X = +3.4..+16.2, Z = +1.4..+12.0 + SE Lobby Foyer
            // =========================================================================
            Transform lobbyGroup = EnsureHotelSubgroup(hotelRootTf, "Hotel_Lobby_Reception");
            // Built-in architectural Reception Alcove along North wall (X = +7.4..+14.2, Z = +9.2..+12.0)
            EnsureHotelWallSegment(lobbyGroup, "Hotel_Reception_PilasterW", new Vector2(7.4f, 10.5f), new Vector2(7.4f, 12.0f), 0.34f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(lobbyGroup, "Hotel_Reception_PilasterE", new Vector2(14.2f, 10.5f), new Vector2(14.2f, 12.0f), 0.34f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_CanopySoffit",
                new Vector3(10.8f, CeilingY - 0.21f, 10.60f), Quaternion.identity, new Vector3(7.14f, 0.42f, 2.80f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_CanopyValenceTrim",
                new Vector3(10.8f, CeilingY - 0.42f, 9.22f), Quaternion.identity, new Vector3(7.16f, 0.05f, 0.06f),
                mats.IndustrialMetal, keepCollider: false);
            // Recessed architectural wall-backing masonry frame on the Reception North Wall (pure architecture, zero furniture)
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_BackWallRevealTop",
                new Vector3(10.8f, BaseY + 2.18f, 11.83f), Quaternion.identity, new Vector3(5.40f, 0.10f, 0.12f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_BackWallRevealBot",
                new Vector3(10.8f, BaseY + 1.04f, 11.83f), Quaternion.identity, new Vector3(5.40f, 0.10f, 0.12f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_BackWallRevealL",
                new Vector3(8.14f, BaseY + 1.61f, 11.83f), Quaternion.identity, new Vector3(0.10f, 1.24f, 0.12f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Reception_BackWallRevealR",
                new Vector3(13.46f, BaseY + 1.61f, 11.83f), Quaternion.identity, new Vector3(0.10f, 1.24f, 0.12f),
                mats.ColumnConcrete, keepCollider: false);

            // Public Lobby ceiling structural soffit beams distinguishing public volume from guest corridors
            EnsureHotelBox(lobbyGroup, "Hotel_Lobby_Beam_East",
                new Vector3(16.2f, CeilingY - 0.17f, 0.50f), Quaternion.identity, new Vector3(0.30f, 0.34f, 9.80f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Lobby_Beam_Mid",
                new Vector3(10.2f, CeilingY - 0.17f, 3.80f), Quaternion.identity, new Vector3(0.30f, 0.34f, 16.40f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Lobby_Beam_West",
                new Vector3(3.4f, CeilingY - 0.17f, 5.10f), Quaternion.identity, new Vector3(0.30f, 0.34f, 13.80f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(lobbyGroup, "Hotel_Lobby_Beam_South",
                new Vector3(9.8f, CeilingY - 0.17f, 1.40f), Quaternion.identity, new Vector3(12.80f, 0.34f, 0.30f),
                mats.ColumnConcrete, keepCollider: false);
            // Architectural portal piers framing the transition from Main Lobby into the Guest-Room Corridor
            EnsureHotelWallSegment(lobbyGroup, "Hotel_Lobby_CorridorPortalPierN", new Vector2(10.2f, 1.4f), new Vector2(10.2f, 2.3f), 0.30f, mats.ColumnConcrete, mats.BaseboardConcrete);

            // Lobby Front-Office Architectural Alcove (X = +5.4..+10.2, Z = -4.4..-1.8, preserves existing LaptopWorkstation at (7.9,-2.7))
            EnsureHotelWallSegment(lobbyGroup, "Hotel_FrontOffice_EastWall", new Vector2(10.2f, -4.4f), new Vector2(10.2f, -1.8f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(lobbyGroup, "Hotel_FrontOffice_WestWall", new Vector2(5.4f, -4.4f), new Vector2(5.4f, -1.8f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(lobbyGroup, "Hotel_FrontOffice_NorthPierE", new Vector2(8.2f, -1.8f), new Vector2(10.2f, -1.8f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelBox(lobbyGroup, "Hotel_FrontOffice_ArchTransom",
                new Vector3(6.8f, CeilingY - 0.25f, -1.8f), Quaternion.identity, new Vector3(2.85f, 0.50f, 0.26f),
                mats.ColumnConcrete, keepCollider: false);

            // =========================================================================
            // C. TWIN REINFORCED-CONCRETE ELEVATOR CORE & WAITING LOBBY
            //    Core: X = +1.0..+3.4, Z = +4.2..+9.4 (2 recessed closed metal bays facing East into Lobby)
            //    Safe solid shaft walls & closed metal doors — ZERO vertical fall hazard!
            // =========================================================================
            Transform elevGroup = EnsureHotelSubgroup(hotelRootTf, "Hotel_Elevator_Core");
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_BackWall", new Vector2(1.12f, 4.2f), new Vector2(1.12f, 9.4f), 0.24f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_SouthWall", new Vector2(1.0f, 4.32f), new Vector2(3.4f, 4.32f), 0.24f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_NorthWall", new Vector2(1.0f, 9.28f), new Vector2(3.4f, 9.28f), 0.24f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_FrontPierS", new Vector2(3.28f, 4.2f), new Vector2(3.28f, 5.0f), 0.26f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_FrontPierMid", new Vector2(3.28f, 6.5f), new Vector2(3.28f, 7.1f), 0.26f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(elevGroup, "Hotel_ElevatorCore_FrontPierN", new Vector2(3.28f, 8.6f), new Vector2(3.28f, 9.4f), 0.26f, mats.ColumnConcrete, mats.BaseboardConcrete);
            EnsureHotelBox(elevGroup, "Hotel_ElevatorCore_FrontHeader",
                new Vector3(3.28f, CeilingY - 0.325f, 6.80f), Quaternion.identity, new Vector3(0.28f, 0.65f, 5.24f),
                mats.ColumnConcrete, keepCollider: true);
            // Dropped concrete Elevator Waiting Lobby soffit framing the waiting bay (X = +3.4..+5.8)
            EnsureHotelBox(elevGroup, "Hotel_ElevatorLobby_Soffit",
                new Vector3(4.60f, CeilingY - 0.18f, 6.80f), Quaternion.identity, new Vector3(2.40f, 0.36f, 5.40f),
                mats.ColumnConcrete, keepCollider: false);

            BuildOrUpdateElevatorBay(elevGroup, "Hotel_ElevatorBay_1", 5.75f, mats);
            BuildOrUpdateElevatorBay(elevGroup, "Hotel_ElevatorBay_2", 7.85f, mats);

            // Shared central hall-call bezel plate mounted on concrete pier between Bay 1 and Bay 2
            EnsureHotelBox(elevGroup, "Hotel_Elevator_CallPanelPlate",
                new Vector3(3.43f, BaseY + 1.18f, 6.80f), Quaternion.identity, new Vector3(0.025f, 0.26f, 0.14f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, "Hotel_Elevator_CallButtonUp",
                new Vector3(3.448f, BaseY + 1.23f, 6.80f), Quaternion.identity, new Vector3(0.014f, 0.04f, 0.04f),
                mats.ElevatorDoorMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, "Hotel_Elevator_CallButtonDown",
                new Vector3(3.448f, BaseY + 1.13f, 6.80f), Quaternion.identity, new Vector3(0.014f, 0.04f, 0.04f),
                mats.ElevatorDoorMetal, keepCollider: false);

            // =========================================================================
            // D. MAIN DOUBLE-LOADED HOTEL GUEST-ROOM CORRIDOR (X = -9.5..+10.2, Z = -1.8..+1.4)
            //    Rhythmic overhead concrete soffit beams + framed Service Bulkhead Portal at X = -9.5
            // =========================================================================
            Transform corrGroup = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestCorridor");
            float[] corrBeamXs = new float[] { 2.5f, -1.5f, -5.5f, -9.5f };
            for (int b = 0; b < corrBeamXs.Length; b++)
            {
                EnsureHotelBox(corrGroup, $"Hotel_Corridor_CeilingBeam_{b}",
                    new Vector3(corrBeamXs[b], CeilingY - 0.15f, -0.20f), Quaternion.identity, new Vector3(0.28f, 0.30f, 3.20f),
                    mats.ColumnConcrete, keepCollider: false);
            }
            // Framed Back-of-House Service Wing Bulkhead Portal at X = -9.5 (Z = -1.8..+1.4, 2.52 m clear width, no lock)
            Transform serviceGroup = EnsureHotelSubgroup(hotelRootTf, "Hotel_Service_Wing");
            EnsureHotelBox(serviceGroup, "Hotel_ServicePortal_Transom",
                new Vector3(-9.5f, CeilingY - 0.32f, -0.20f), Quaternion.identity, new Vector3(0.32f, 0.64f, 2.60f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(serviceGroup, "Hotel_ServicePortal_JambS",
                new Vector3(-9.5f, BaseY + 1.11f, -1.44f), Quaternion.identity, new Vector3(0.36f, 2.22f, 0.06f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(serviceGroup, "Hotel_ServicePortal_JambN",
                new Vector3(-9.5f, BaseY + 1.11f, 1.04f), Quaternion.identity, new Vector3(0.36f, 2.22f, 0.06f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(serviceGroup, "Hotel_ServicePortal_HeaderTrim",
                new Vector3(-9.5f, BaseY + 2.21f, -0.20f), Quaternion.identity, new Vector3(0.36f, 0.06f, 2.54f),
                mats.IndustrialMetal, keepCollider: false);

            // SW Maintenance Hall Entry Portal (Z = -1.8, X = -9.5..-5.6, 2.4 m wide framed opening X = -8.6..-6.2)
            EnsureHotelWallSegment(serviceGroup, "Hotel_SWService_NorthPierW", new Vector2(-9.5f, -1.8f), new Vector2(-8.6f, -1.8f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(serviceGroup, "Hotel_SWService_NorthPierE", new Vector2(-6.2f, -1.8f), new Vector2(-5.6f, -1.8f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelBox(serviceGroup, "Hotel_SWService_PortalTransom",
                new Vector3(-7.4f, CeilingY - 0.30f, -1.8f), Quaternion.identity, new Vector3(2.44f, 0.60f, 0.24f),
                mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(serviceGroup, "Hotel_SWService_PortalTrimW",
                new Vector3(-8.58f, BaseY + 1.13f, -1.8f), Quaternion.identity, new Vector3(0.06f, 2.26f, 0.28f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(serviceGroup, "Hotel_SWService_PortalTrimE",
                new Vector3(-6.22f, BaseY + 1.13f, -1.8f), Quaternion.identity, new Vector3(0.06f, 2.26f, 0.28f),
                mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // E. SIX DISTINCT EMPTY HOTEL GUEST-ROOM SHELLS (ROOMS 101-106, ZERO FURNITURE!)
            //    Each room has framed traversable doorway, neutral room plaque, party walls,
            //    and distinct unfinished architectural en-suite bathroom / entry foyer partitions.
            // =========================================================================

            // --- GUEST ROOM 101 (North Mid: X = -3.6..+1.0, Z = +1.4..+12.0) ---
            Transform r101 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_101");
            EnsureHotelDoorwayWallX(r101, "Hotel_Room101_CorridorWall", z: 1.4f, minX: -3.6f, maxX: 1.0f, doorMinX: -1.14f, doorMaxX: 0.26f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[0], plaqueX: 0.58f, corridorOnSouthSide: true);
            EnsureHotelWallSegment(r101, "Hotel_Room101_EastWall", new Vector2(1.0f, 1.4f), new Vector2(1.0f, 12.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r101, "Hotel_Room101_WestPartyWall", new Vector2(-3.6f, 1.4f), new Vector2(-3.6f, 12.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // Built-in unfinished bathroom shell inside Room 101 (open doorway facing West at Z = +4.4..+5.4)
            EnsureHotelWallSegment(r101, "Hotel_Room101_BathWallS", new Vector2(-0.8f, 4.4f), new Vector2(1.0f, 4.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r101, "Hotel_Room101_BathWallN", new Vector2(-0.8f, 7.0f), new Vector2(1.0f, 7.0f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r101, "Hotel_Room101_BathWallW", new Vector2(-0.8f, 5.4f), new Vector2(-0.8f, 7.0f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 102 (North-West Suite: X = -9.5..-3.6, Z = +1.4..+12.0; houses SpecimenVitrine cleanly) ---
            Transform r102 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_102");
            EnsureHotelDoorwayWallX(r102, "Hotel_Room102_CorridorWall", z: 1.4f, minX: -9.5f, maxX: -3.6f, doorMinX: -7.80f, doorMaxX: -6.40f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[1], plaqueX: -6.05f, corridorOnSouthSide: true);
            EnsureHotelWallSegment(r102, "Hotel_Room102_WestServiceWall", new Vector2(-9.5f, 1.4f), new Vector2(-9.5f, 12.0f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            // Built-in unfinished bathroom shell inside Room 102 (open doorway facing East at Z = +4.6..+5.6)
            EnsureHotelWallSegment(r102, "Hotel_Room102_BathWallS", new Vector2(-9.5f, 4.6f), new Vector2(-7.4f, 4.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r102, "Hotel_Room102_BathWallN", new Vector2(-9.5f, 7.4f), new Vector2(-7.4f, 7.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r102, "Hotel_Room102_BathWallE", new Vector2(-7.4f, 5.6f), new Vector2(-7.4f, 7.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 103 (South-East Corner Suite: X = +12.6..+20.0, Z = -12.0..-4.4; opens onto SE Lobby Foyer) ---
            Transform r103 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_103");
            EnsureHotelDoorwayWallX(r103, "Hotel_Room103_CorridorWall", z: -4.4f, minX: 12.6f, maxX: 20.0f, doorMinX: 15.20f, doorMaxX: 16.60f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[2], plaqueX: 16.95f, corridorOnSouthSide: false);
            EnsureHotelWallSegment(r103, "Hotel_Room103_WestPartyWall", new Vector2(12.6f, -12.0f), new Vector2(12.6f, -4.4f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // Built-in unfinished bathroom/closet partition inside Room 103
            EnsureHotelWallSegment(r103, "Hotel_Room103_BathWallE", new Vector2(14.6f, -7.2f), new Vector2(14.6f, -4.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r103, "Hotel_Room103_BathWallS", new Vector2(13.6f, -7.2f), new Vector2(14.6f, -7.2f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 104 (South-East Standard Room: X = +7.8..+12.6, Z = -12.0..-4.4; houses ShiftClock0307 cleanly) ---
            Transform r104 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_104");
            EnsureHotelDoorwayWallX(r104, "Hotel_Room104_CorridorWall", z: -4.4f, minX: 7.8f, maxX: 12.6f, doorMinX: 10.45f, doorMaxX: 11.80f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[3], plaqueX: 12.12f, corridorOnSouthSide: false);
            EnsureHotelWallSegment(r104, "Hotel_Room104_WestPartyWall", new Vector2(7.8f, -12.0f), new Vector2(7.8f, -4.4f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // Built-in entry foyer nib partition inside Room 104
            EnsureHotelWallSegment(r104, "Hotel_Room104_FoyerNibWall", new Vector2(9.4f, -6.8f), new Vector2(9.4f, -4.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 105 (South-Center Suite: Entry Foyer X=+1.4..+5.4, Z=-4.4..-1.8 + Main Chamber X=+1.4..+7.8, Z=-12.0..-4.4) ---
            Transform r105 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_105");
            EnsureHotelDoorwayWallX(r105, "Hotel_Room105_CorridorWall", z: -1.8f, minX: 1.4f, maxX: 5.4f, doorMinX: 3.30f, doorMaxX: 4.70f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[4], plaqueX: 2.92f, corridorOnSouthSide: false);
            EnsureHotelWallSegment(r105, "Hotel_Room105_WestPartyWall", new Vector2(1.4f, -12.0f), new Vector2(1.4f, -1.8f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // Built-in suite foyer divider & North backwall closure under Front-Office Alcove (Z = -4.4, X = +5.4..+7.8)
            EnsureHotelWallSegment(r105, "Hotel_Room105_NorthAlcoveWall", new Vector2(5.4f, -4.4f), new Vector2(7.8f, -4.4f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r105, "Hotel_Room105_FoyerDivider", new Vector2(1.4f, -4.4f), new Vector2(3.1f, -4.4f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 106 (South-West Suite: X = -5.6..+1.4, Z = -12.0..-1.8; doorway at X=-4.25..-2.80 keeps (-3.5,-2.2) clear) ---
            Transform r106 = EnsureHotelSubgroup(hotelRootTf, "Hotel_GuestRoom_106");
            EnsureHotelDoorwayWallX(r106, "Hotel_Room106_CorridorWall", z: -1.8f, minX: -5.6f, maxX: 1.4f, doorMinX: -4.25f, doorMaxX: -2.80f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[5], plaqueX: -2.42f, corridorOnSouthSide: false);
            EnsureHotelWallSegment(r106, "Hotel_Room106_WestServiceWall", new Vector2(-5.6f, -12.0f), new Vector2(-5.6f, -1.8f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            // Built-in unfinished bathroom partition inside Room 106 (NE flank, open South at X = -1.4..-0.3)
            EnsureHotelWallSegment(r106, "Hotel_Room106_BathWallW", new Vector2(-1.4f, -5.2f), new Vector2(-1.4f, -1.8f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r106, "Hotel_Room106_BathWallS", new Vector2(-0.3f, -5.2f), new Vector2(1.4f, -5.2f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // Implementation 07.3: Construction-era environmental storytelling, bilingual wayfinding signage,
            // exposed MEP infrastructure, and subtle per-room architectural variations (zero furniture).
            BuildOrUpdateHotelEnvironmentalIdentity(hotelRootTf, mats);
        }

        /// <summary>
        /// Targeted, idempotent Implementation 07.3 environmental identity migration utility.
        /// Opens the existing BackroomsLevel1Scene.unity without altering the 40m x 24m floor plan,
        /// elevator doors/shafts, lighting budget, IP-05 devices, or any existing gameplay/prop anchors,
        /// and builds/updates ONLY the Hotel_Environmental_Identity subgroup under Hotel_GroundFloor_Architecture.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Environmental Identity (In-Place)")]
        public static void MigrateRemanenciaEnvironmentalIdentityAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaEnvironmentalIdentityInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Hotel environmental identity migration completed and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaEnvironmentalIdentityInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia environmental identity migration requires open scene '{ScenePath}', but found '{scene.path}'.");
            }

            GameObject root = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "BackroomsLevel1_Root")
                {
                    root = go;
                    break;
                }
            }
            if (root == null)
            {
                throw new InvalidOperationException("BackroomsLevel1_Root not found in open scene.");
            }

            Transform shellRoot = root.transform.Find("Backrooms_Architecture");
            Transform hotelRoot = shellRoot != null ? shellRoot.Find(HotelArchitectureRootName) : null;
            if (hotelRoot == null)
            {
                throw new InvalidOperationException($"Expected '{HotelArchitectureRootName}' under Backrooms_Architecture before running 07.3 environmental migration.");
            }

            RemanenciaVisualMaterials mats = EnsureRemanenciaVisualAssets();
            BuildOrUpdateHotelEnvironmentalIdentity(hotelRoot, mats);
            EnsureRoomPlaqueOrientationsUpright(hotelRoot);
        }

        /// <summary>
        /// Unity's built-in PrimitiveType.Cube renders upright UVs on its local +Z face (while its -Z face
        /// has inverted UVs). Ensures all 6 room-number plaques orient their +Z face outward toward the corridor.
        /// </summary>
        private static void EnsureRoomPlaqueOrientationsUpright(Transform hotelRoot)
        {
            for (int r = 0; r < 6; r++)
            {
                int roomNum = 101 + r;
                Transform plaque = hotelRoot.Find($"Hotel_GuestRoom_{roomNum}/Hotel_Room{roomNum}_CorridorWall_RoomPlaque");
                if (plaque == null) continue;
                bool corridorOnSouthSide = roomNum == 101 || roomNum == 102;
                plaque.rotation = corridorOnSouthSide ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            }
        }

        private static void BuildOrUpdateHotelEnvironmentalIdentity(Transform hotelRootTf, RemanenciaVisualMaterials mats)
        {
            Transform envRoot = EnsureHotelSubgroup(hotelRootTf, HotelEnvironmentalIdentityRootName);

            // =========================================================================
            // 1. RESTRAINED BILINGUAL HOTEL IDENTITY & WAYFINDING SIGNAGE
            //    Unity's built-in Cube primitive has upright UVs on its local +Z face:
            //      - Facing East  (+X): Quaternion.Euler(0f,  90f, 0f)
            //      - Facing South (-Z): Quaternion.Euler(0f, 180f, 0f)
            //      - Facing North (+Z): Quaternion.identity
            //    Pure static environmental panels (zero interactive controllers, zero Cobalt-60 signage).
            // =========================================================================
            Transform signGroup = EnsureHotelSubgroup(envRoot, "Hotel_Identity_Signage");

            // Entrance Marquee facing East (+X) toward arriving player at (17.8, 8.8)
            EnsureHotelBox(signGroup, "Hotel_Sign_EntranceMarquee",
                new Vector3(16.375f, BaseY + 2.63f, 8.80f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(2.60f, 0.34f, 0.025f), mats.SignHotelName, keepCollider: false);
            EnsureHotelBox(signGroup, "Hotel_Sign_EntranceBracketL",
                new Vector3(16.36f, BaseY + 2.63f, 7.46f), Quaternion.identity,
                new Vector3(0.05f, 0.38f, 0.05f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(signGroup, "Hotel_Sign_EntranceBracketR",
                new Vector3(16.36f, BaseY + 2.63f, 10.14f), Quaternion.identity,
                new Vector3(0.05f, 0.38f, 0.05f), mats.IndustrialMetal, keepCollider: false);

            // Reception Alcove North Backdrop Sign facing South (-Z) + unmounted ghost outline below
            EnsureHotelBox(signGroup, "Hotel_Sign_ReceptionBackdrop",
                new Vector3(10.80f, BaseY + 1.86f, 11.82f), Quaternion.Euler(0f, 180f, 0f),
                new Vector3(2.40f, 0.38f, 0.024f), mats.SignReception, keepCollider: false);
            EnsureHotelBox(signGroup, "Hotel_Sign_ReceptionGhostOutline",
                new Vector3(10.80f, BaseY + 1.36f, 11.84f), Quaternion.identity,
                new Vector3(1.10f, 0.24f, 0.008f), mats.ColumnConcrete, keepCollider: false);

            // Elevator Area Identification facing East (+X) — does NOT modify any elevator door or shaft GameObject
            EnsureHotelBox(signGroup, "Hotel_Sign_ElevatorsHeader",
                new Vector3(3.435f, BaseY + 2.68f, 6.80f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.56f, 0.22f, 0.020f), mats.SignElevators, keepCollider: false);
            EnsureHotelBox(signGroup, "Hotel_Sign_ElevatorNotice",
                new Vector3(3.435f, BaseY + 0.88f, 6.80f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0.36f, 0.16f, 0.016f), mats.SignElevatorNotice, keepCollider: false);

            // Guest-Room Corridor Wayfinding mounted on Lobby-to-Corridor transition beam facing East (+X)
            EnsureHotelBox(signGroup, "Hotel_Sign_GuestRoomsWayfinding",
                new Vector3(10.365f, BaseY + 2.58f, -0.20f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.52f, 0.24f, 0.020f), mats.SignGuestRooms, keepCollider: false);

            // Back-of-House Service Bulkhead Signage facing East (+X) down the Guest Corridor & North (+Z) into Corridor
            EnsureHotelBox(signGroup, "Hotel_Sign_ServiceWing",
                new Vector3(-9.33f, BaseY + 2.50f, -0.20f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.36f, 0.24f, 0.020f), mats.SignService, keepCollider: false);
            EnsureHotelBox(signGroup, "Hotel_Sign_SWMaintenance",
                new Vector3(-7.40f, BaseY + 2.50f, -1.67f), Quaternion.identity,
                new Vector3(1.54f, 0.24f, 0.020f), mats.SignMaintenance, keepCollider: false);

            // =========================================================================
            // 2. EXPOSED ELECTRICAL INFRASTRUCTURE (Conduits, Cable Trays, Junction/Outlet Boxes)
            // =========================================================================
            Transform elecGroup = EnsureHotelSubgroup(envRoot, "Hotel_Infra_Electrical");

            // Overhead galvanized cable tray along North ceiling edge of Main Guest Corridor
            EnsureHotelBox(elecGroup, "Hotel_Elec_CorridorCableTray",
                new Vector3(-3.20f, CeilingY - 0.10f, 1.16f), Quaternion.identity,
                new Vector3(12.20f, 0.04f, 0.18f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_CorridorTrayHangerA",
                new Vector3(1.40f, CeilingY - 0.05f, 1.16f), Quaternion.identity,
                new Vector3(0.04f, 0.10f, 0.22f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_CorridorTrayHangerB",
                new Vector3(-3.50f, CeilingY - 0.05f, 1.16f), Quaternion.identity,
                new Vector3(0.04f, 0.10f, 0.22f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_CorridorTrayHangerC",
                new Vector3(-8.40f, CeilingY - 0.05f, 1.16f), Quaternion.identity,
                new Vector3(0.04f, 0.10f, 0.22f), mats.IndustrialMetal, keepCollider: false);

            // Incomplete electrical outlet housings & conduit drops on Reception North wall (pure architecture)
            EnsureHotelBox(elecGroup, "Hotel_Elec_ReceptionOutletL",
                new Vector3(8.80f, BaseY + 0.42f, 11.85f), Quaternion.identity,
                new Vector3(0.11f, 0.14f, 0.035f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ReceptionOutletR",
                new Vector3(12.80f, BaseY + 0.42f, 11.85f), Quaternion.identity,
                new Vector3(0.11f, 0.14f, 0.035f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ReceptionConduitDrop",
                new Vector3(12.80f, BaseY + 1.48f, 11.85f), Quaternion.identity,
                new Vector3(0.028f, 2.04f, 0.028f), mats.IndustrialMetal, keepCollider: false);

            // Unfinished Elevator Lobby overhead junction box & conduit stub beside core north corner
            EnsureHotelBox(elecGroup, "Hotel_Elec_ElevatorJunctionBox",
                new Vector3(3.43f, BaseY + 2.48f, 9.05f), Quaternion.identity,
                new Vector3(0.06f, 0.18f, 0.18f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ElevatorConduitRun",
                new Vector3(3.43f, BaseY + 2.66f, 9.05f), Quaternion.identity,
                new Vector3(0.03f, 0.34f, 0.03f), mats.IndustrialMetal, keepCollider: false);

            // Service Wing heavy exposed electrical conduit run & recessed service breaker panel
            EnsureHotelBox(elecGroup, "Hotel_Elec_ServiceMainConduitA",
                new Vector3(-12.36f, CeilingY - 0.28f, 2.60f), Quaternion.identity,
                new Vector3(0.04f, 0.04f, 12.60f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ServiceMainConduitB",
                new Vector3(-12.36f, CeilingY - 0.38f, 2.60f), Quaternion.identity,
                new Vector3(0.04f, 0.04f, 12.60f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ServiceJunctionBox",
                new Vector3(-12.35f, CeilingY - 0.33f, 4.60f), Quaternion.identity,
                new Vector3(0.07f, 0.22f, 0.22f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ServicePanelBox",
                new Vector3(-9.64f, BaseY + 1.46f, 3.20f), Quaternion.identity,
                new Vector3(0.09f, 0.72f, 0.48f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elecGroup, "Hotel_Elec_ServicePanelConduit",
                new Vector3(-9.64f, BaseY + 2.34f, 3.20f), Quaternion.identity,
                new Vector3(0.04f, 1.04f, 0.04f), mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // 3. EXPOSED PLUMBING & BUILDING SERVICES (Risers, Branches, Floor Drains, Access Panels)
            // =========================================================================
            Transform plumbGroup = EnsureHotelSubgroup(envRoot, "Hotel_Infra_Plumbing");

            // N-S Service Corridor North corner vertical risers + wall penetration collar
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_NorthServiceRiserA",
                new Vector3(-12.24f, BaseY + ClearHeight * 0.5f, 11.74f), Quaternion.identity,
                new Vector3(0.09f, ClearHeight, 0.09f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_NorthServiceRiserB",
                new Vector3(-12.04f, BaseY + ClearHeight * 0.5f, 11.74f), Quaternion.identity,
                new Vector3(0.06f, ClearHeight, 0.06f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_NorthServiceCollar",
                new Vector3(-12.14f, BaseY + 2.40f, 11.74f), Quaternion.identity,
                new Vector3(0.34f, 0.06f, 0.14f), mats.IndustrialMetal, keepCollider: false);

            // SW Maintenance Hall SE corner plumbing risers & high horizontal branch
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_SWServiceRiserA",
                new Vector3(-5.78f, BaseY + ClearHeight * 0.5f, -11.74f), Quaternion.identity,
                new Vector3(0.09f, ClearHeight, 0.09f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_SWServiceRiserB",
                new Vector3(-5.78f, BaseY + ClearHeight * 0.5f, -11.52f), Quaternion.identity,
                new Vector3(0.06f, ClearHeight, 0.06f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_SWServiceOverheadBranch",
                new Vector3(-8.40f, CeilingY - 0.22f, -11.76f), Quaternion.identity,
                new Vector3(5.20f, 0.07f, 0.07f), mats.PipeCopperRust, keepCollider: false);

            // West Mechanical / Utility Room vertical riser + recessed maintenance access hatch
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_WestUtilityRiser",
                new Vector3(-19.76f, BaseY + ClearHeight * 0.5f, 4.46f), Quaternion.identity,
                new Vector3(0.09f, ClearHeight, 0.09f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_ServiceAccessPanel",
                new Vector3(-12.36f, BaseY + 1.12f, -2.50f), Quaternion.identity,
                new Vector3(0.03f, 0.44f, 0.44f), mats.IndustrialMetal, keepCollider: false);

            // Flush service floor drains (non-colliding floor grates)
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_FloorDrain_ServiceN",
                new Vector3(-11.00f, BaseY + 0.004f, 5.60f), Quaternion.identity,
                new Vector3(0.24f, 0.008f, 0.24f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(plumbGroup, "Hotel_Plumb_FloorDrain_ServiceSW",
                new Vector3(-8.20f, BaseY + 0.004f, -5.80f), Quaternion.identity,
                new Vector3(0.24f, 0.008f, 0.24f), mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // 4. STRUCTURAL & CONSTRUCTION-STAGE VARIATION DETAILS (Lobby Transitions & Safe Capped Rebar)
            // =========================================================================
            Transform structGroup = EnsureHotelSubgroup(envRoot, "Hotel_Infra_Structural");

            // Flush lobby architectural threshold & Reception zone floor border strips (visual zone distinction)
            EnsureHotelBox(structGroup, "Hotel_Struct_EntranceFloorThreshold",
                new Vector3(16.20f, BaseY + 0.004f, 8.80f), Quaternion.identity,
                new Vector3(0.24f, 0.008f, 3.96f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(structGroup, "Hotel_Struct_ReceptionFloorBorder",
                new Vector3(10.80f, BaseY + 0.004f, 9.24f), Quaternion.identity,
                new Vector3(6.80f, 0.008f, 0.16f), mats.AgedCeramicPatch, keepCollider: false);
            // Partial unfinished decorative wainscot panel inside Reception Alcove
            EnsureHotelBox(structGroup, "Hotel_Struct_ReceptionWainscotPanel",
                new Vector3(9.45f, BaseY + 0.56f, 11.84f), Quaternion.identity,
                new Vector3(2.50f, 0.88f, 0.025f), mats.AgedCeramicPatch, keepCollider: false);

            // Safely capped upper exposed reinforcement steel (rebar) stubs high at unfinished soffit/beam seams
            EnsureCappedRebarStub(structGroup, "Hotel_Struct_RebarStub_LobbyA", new Vector3(7.42f, CeilingY - 0.46f, 10.46f), mats);
            EnsureCappedRebarStub(structGroup, "Hotel_Struct_RebarStub_LobbyB", new Vector3(7.52f, CeilingY - 0.48f, 10.46f), mats);
            EnsureCappedRebarStub(structGroup, "Hotel_Struct_RebarStub_ServiceA", new Vector3(-9.66f, CeilingY - 0.24f, 8.80f), mats);
            EnsureCappedRebarStub(structGroup, "Hotel_Struct_RebarStub_ServiceB", new Vector3(-9.66f, CeilingY - 0.26f, 8.92f), mats);

            // =========================================================================
            // 5. INDIVIDUAL GUEST-ROOM VARIATIONS (ROOMS 101-106 — ZERO FURNITURE!)
            //    Unfinished static open door leaves / exposed hinges, partial tile bands,
            //    unfinished bathroom rough-in pipes, floor drains, and incomplete electrical boxes.
            // =========================================================================
            Transform roomsGroup = EnsureHotelSubgroup(envRoot, "Hotel_Room_Variations");

            // Room 101: Static unfinished door leaf swung open inside against East foyer wall + partial ceramic bath band + plumbing & drain
            EnsureHotelBox(roomsGroup, "Hotel_Room101_StaticDoorLeaf",
                new Vector3(0.46f, BaseY + 1.09f, 2.02f), Quaternion.Euler(0f, 104f, 0f),
                new Vector3(0.96f, 2.18f, 0.045f), mats.UnfinishedWoodDoor, keepCollider: false);
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room101_Hinge", new Vector3(0.24f, BaseY, 1.48f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room101_BathTileStrip",
                new Vector3(0.87f, BaseY + 0.56f, 5.70f), Quaternion.identity,
                new Vector3(0.025f, 0.88f, 2.30f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room101_BathPipeRiser",
                new Vector3(0.84f, BaseY + 0.65f, 6.72f), Quaternion.identity,
                new Vector3(0.055f, 1.30f, 0.055f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room101_BathFloorDrain",
                new Vector3(0.15f, BaseY + 0.004f, 5.70f), Quaternion.identity,
                new Vector3(0.18f, 0.008f, 0.18f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room101_OutletBox",
                new Vector3(-3.46f, BaseY + 0.42f, 9.20f), Quaternion.identity,
                new Vector3(0.035f, 0.14f, 0.11f), mats.IndustrialMetal, keepCollider: false);

            // Room 102: Missing door leaf (exposed empty hinges) + exposed bath conduit, plumbing stub & drain
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room102_EmptyHinge", new Vector3(-6.42f, BaseY, 1.48f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room102_BathConduit",
                new Vector3(-7.30f, BaseY + 1.42f, 6.50f), Quaternion.identity,
                new Vector3(0.028f, 2.80f, 0.028f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room102_BathJunctionBox",
                new Vector3(-7.30f, BaseY + 1.55f, 6.50f), Quaternion.identity,
                new Vector3(0.05f, 0.14f, 0.14f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room102_BathPipeStub",
                new Vector3(-9.34f, BaseY + 0.55f, 6.10f), Quaternion.identity,
                new Vector3(0.055f, 1.10f, 0.055f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room102_BathFloorDrain",
                new Vector3(-8.45f, BaseY + 0.004f, 6.00f), Quaternion.identity,
                new Vector3(0.18f, 0.008f, 0.18f), mats.IndustrialMetal, keepCollider: false);

            // Room 103: Static unfinished door leaf swung open inside + partial ceramic floor tile strip & bath drain
            EnsureHotelBox(roomsGroup, "Hotel_Room103_StaticDoorLeaf",
                new Vector3(16.82f, BaseY + 1.09f, -5.02f), Quaternion.Euler(0f, -104f, 0f),
                new Vector3(0.96f, 2.18f, 0.045f), mats.UnfinishedWoodDoor, keepCollider: false);
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room103_Hinge", new Vector3(16.58f, BaseY, -4.48f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room103_BathTileFloorPatch",
                new Vector3(13.60f, BaseY + 0.004f, -5.80f), Quaternion.identity,
                new Vector3(1.65f, 0.008f, 2.20f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room103_BathPipeStub",
                new Vector3(12.76f, BaseY + 0.60f, -5.50f), Quaternion.identity,
                new Vector3(0.055f, 1.20f, 0.055f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room103_BathFloorDrain",
                new Vector3(13.60f, BaseY + 0.009f, -5.80f), Quaternion.identity,
                new Vector3(0.18f, 0.008f, 0.18f), mats.IndustrialMetal, keepCollider: false);

            // Room 104: Missing door leaf (exposed jamb hinges) + foyer electrical rough-in conduit & dual outlet boxes
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room104_EmptyHinge", new Vector3(10.47f, BaseY, -4.48f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room104_FoyerConduit",
                new Vector3(9.50f, BaseY + 1.56f, -5.80f), Quaternion.identity,
                new Vector3(0.028f, 2.40f, 0.028f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room104_OutletBoxA",
                new Vector3(9.50f, BaseY + 0.42f, -5.80f), Quaternion.identity,
                new Vector3(0.035f, 0.14f, 0.11f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room104_OutletBoxB",
                new Vector3(7.94f, BaseY + 0.42f, -8.80f), Quaternion.identity,
                new Vector3(0.035f, 0.14f, 0.11f), mats.IndustrialMetal, keepCollider: false);

            // Room 105: Static unfinished door leaf swung open inside against East foyer wall + ceiling foyer soffit beam & junction box
            EnsureHotelBox(roomsGroup, "Hotel_Room105_StaticDoorLeaf",
                new Vector3(4.92f, BaseY + 1.09f, -2.42f), Quaternion.Euler(0f, -106f, 0f),
                new Vector3(0.96f, 2.18f, 0.045f), mats.UnfinishedWoodDoor, keepCollider: false);
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room105_Hinge", new Vector3(4.68f, BaseY, -1.88f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room105_FoyerSoffitHeader",
                new Vector3(4.25f, CeilingY - 0.14f, -4.40f), Quaternion.identity,
                new Vector3(2.26f, 0.28f, 0.22f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room105_FoyerJunctionBox",
                new Vector3(2.35f, BaseY + 1.60f, -4.50f), Quaternion.identity,
                new Vector3(0.14f, 0.14f, 0.045f), mats.IndustrialMetal, keepCollider: false);

            // Room 106: Missing door leaf (exposed hinges) + exposed bath risers, partial ceramic band, drain & threshold strip
            EnsureExposedDoorHinges(roomsGroup, "Hotel_Room106_EmptyHinge", new Vector3(-2.82f, BaseY, -1.88f), mats.IndustrialMetal);
            EnsureHotelBox(roomsGroup, "Hotel_Room106_DoorThresholdPlate",
                new Vector3(-3.525f, BaseY + 0.004f, -1.80f), Quaternion.identity,
                new Vector3(1.42f, 0.008f, 0.22f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room106_BathTileStrip",
                new Vector3(1.27f, BaseY + 0.56f, -3.50f), Quaternion.identity,
                new Vector3(0.025f, 0.88f, 3.00f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room106_BathPipeRiserA",
                new Vector3(1.24f, BaseY + 0.72f, -4.85f), Quaternion.identity,
                new Vector3(0.055f, 1.44f, 0.055f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room106_BathPipeRiserB",
                new Vector3(1.24f, BaseY + 0.62f, -4.65f), Quaternion.identity,
                new Vector3(0.045f, 1.24f, 0.045f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(roomsGroup, "Hotel_Room106_BathFloorDrain",
                new Vector3(0.00f, BaseY + 0.004f, -3.50f), Quaternion.identity,
                new Vector3(0.18f, 0.008f, 0.18f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void EnsureExposedDoorHinges(Transform parent, string prefix, Vector3 basePos, Material metalMat)
        {
            float[] ys = { 0.34f, 1.10f, 1.86f };
            for (int i = 0; i < ys.Length; i++)
            {
                EnsureHotelBox(parent, $"{prefix}_{i}",
                    new Vector3(basePos.x, basePos.y + ys[i], basePos.z), Quaternion.identity,
                    new Vector3(0.032f, 0.11f, 0.032f), metalMat, keepCollider: false);
            }
        }

        private static void EnsureCappedRebarStub(Transform parent, string name, Vector3 pos, RemanenciaVisualMaterials mats)
        {
            EnsureHotelBox(parent, name, pos, Quaternion.identity, new Vector3(0.022f, 0.16f, 0.022f), mats.PipeCopperRust, keepCollider: false);
            EnsureHotelBox(parent, name + "_Cap", pos + new Vector3(0f, -0.09f, 0f), Quaternion.identity, new Vector3(0.042f, 0.036f, 0.042f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void BuildOrUpdateElevatorBay(Transform elevGroup, string bayPrefix, float centerZ, RemanenciaVisualMaterials mats)
        {
            // Recessed closed bi-parting metal elevator door leaves (solid colliders meeting flush at centerZ -> zero gap, zero fall hazard)
            EnsureHotelBox(elevGroup, $"{bayPrefix}_DoorLeafS",
                new Vector3(3.14f, BaseY + 1.10f, centerZ - 0.37f), Quaternion.identity, new Vector3(0.08f, 2.20f, 0.76f),
                mats.ElevatorDoorMetal, keepCollider: true);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_DoorLeafN",
                new Vector3(3.14f, BaseY + 1.10f, centerZ + 0.37f), Quaternion.identity, new Vector3(0.08f, 2.20f, 0.76f),
                mats.ElevatorDoorMetal, keepCollider: true);
            // Crisp vertical meeting seam between closed bi-parting leaves
            EnsureHotelBox(elevGroup, $"{bayPrefix}_CenterSeam",
                new Vector3(3.19f, BaseY + 1.10f, centerZ), Quaternion.identity, new Vector3(0.02f, 2.20f, 0.025f),
                mats.CeilingVoid, keepCollider: false);
            // Oxidized steel elevator surround jambs, top lintel, sill threshold, and floor-indicator housing
            EnsureHotelBox(elevGroup, $"{bayPrefix}_FrameJambS",
                new Vector3(3.38f, BaseY + 1.11f, centerZ - 0.75f), Quaternion.identity, new Vector3(0.18f, 2.22f, 0.08f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_FrameJambN",
                new Vector3(3.38f, BaseY + 1.11f, centerZ + 0.75f), Quaternion.identity, new Vector3(0.18f, 2.22f, 0.08f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_FrameHeader",
                new Vector3(3.38f, BaseY + 2.24f, centerZ), Quaternion.identity, new Vector3(0.18f, 0.08f, 1.58f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_FloorSill",
                new Vector3(3.30f, BaseY + 0.012f, centerZ), Quaternion.identity, new Vector3(0.30f, 0.024f, 1.54f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_IndicatorHousing",
                new Vector3(3.43f, BaseY + 2.46f, centerZ), Quaternion.identity, new Vector3(0.04f, 0.16f, 0.56f),
                mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(elevGroup, $"{bayPrefix}_IndicatorStrip",
                new Vector3(3.455f, BaseY + 2.46f, centerZ), Quaternion.identity, new Vector3(0.012f, 0.06f, 0.44f),
                mats.CeilingVoid, keepCollider: false);
        }

        private static Transform EnsureHotelSubgroup(Transform parent, string groupName)
        {
            Transform existing = parent.Find(groupName);
            if (existing != null)
            {
                existing.localPosition = Vector3.zero;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                return existing;
            }
            GameObject go = new GameObject(groupName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void EnsureHotelWallSegment(
            Transform parent, string name, Vector2 a, Vector2 b, float thickness, Material wallMat, Material baseMat)
        {
            Vector2 delta = b - a;
            float len = delta.magnitude;
            if (len < 0.01f) return;
            Vector2 mid = 0.5f * (a + b);
            Quaternion rot = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.y), Vector3.up);

            EnsureHotelBox(parent, name,
                new Vector3(mid.x, BaseY + ClearHeight * 0.5f, mid.y), rot,
                new Vector3(thickness, ClearHeight, len + thickness * 0.5f), wallMat, keepCollider: true);
            EnsureHotelBox(parent, name + "_Baseboard",
                new Vector3(mid.x, BaseY + 0.06f, mid.y), rot,
                new Vector3(thickness + 0.025f, 0.12f, len + thickness * 0.52f), baseMat, keepCollider: false);
        }

        private static void EnsureHotelDoorwayWallX(
            Transform parent, string prefix, float z, float minX, float maxX, float doorMinX, float doorMaxX,
            float thickness, Material wallMat, Material baseMat, Material trimMat,
            Material plaqueMat, float plaqueX, bool corridorOnSouthSide)
        {
            if (doorMinX - minX > 0.05f)
            {
                EnsureHotelWallSegment(parent, $"{prefix}_Left", new Vector2(minX, z), new Vector2(doorMinX, z), thickness, wallMat, baseMat);
            }
            if (maxX - doorMaxX > 0.05f)
            {
                EnsureHotelWallSegment(parent, $"{prefix}_Right", new Vector2(doorMaxX, z), new Vector2(maxX, z), thickness, wallMat, baseMat);
            }

            float doorWidth = doorMaxX - doorMinX;
            float doorCenterX = 0.5f * (doorMinX + doorMaxX);
            const float doorHeight = 2.20f;
            float transomH = ClearHeight - doorHeight; // 0.65m

            EnsureHotelBox(parent, $"{prefix}_Transom",
                new Vector3(doorCenterX, BaseY + doorHeight + transomH * 0.5f, z), Quaternion.identity,
                new Vector3(doorWidth + 0.04f, transomH, thickness), wallMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_JambL",
                new Vector3(doorMinX + 0.02f, BaseY + doorHeight * 0.5f, z), Quaternion.identity,
                new Vector3(0.055f, doorHeight, thickness + 0.05f), trimMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_JambR",
                new Vector3(doorMaxX - 0.02f, BaseY + doorHeight * 0.5f, z), Quaternion.identity,
                new Vector3(0.055f, doorHeight, thickness + 0.05f), trimMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_HeaderTrim",
                new Vector3(doorCenterX, BaseY + doorHeight, z), Quaternion.identity,
                new Vector3(doorWidth + 0.08f, 0.055f, thickness + 0.05f), trimMat, keepCollider: false);

            if (plaqueMat != null)
            {
                float zOffset = corridorOnSouthSide ? -(thickness * 0.5f + 0.012f) : (thickness * 0.5f + 0.012f);
                // Orient Unity Cube's upright +Z face outward toward the corridor
                Quaternion plaqueRot = corridorOnSouthSide ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                EnsureHotelBox(parent, $"{prefix}_RoomPlaque",
                    new Vector3(plaqueX, BaseY + 1.58f, z + zOffset), plaqueRot,
                    new Vector3(0.22f, 0.11f, 0.016f), plaqueMat, keepCollider: false);
            }
        }

        private static GameObject EnsureHotelBox(
            Transform parent, string name, Vector3 worldPos, Quaternion worldRot, Vector3 scale, Material mat, bool keepCollider)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                go.transform.position = worldPos;
                go.transform.rotation = worldRot;
                go.transform.localScale = scale;
                SetSharedMatPreservingRendererSettings(go, mat);
                BoxCollider col = go.GetComponent<BoxCollider>();
                if (keepCollider)
                {
                    if (col == null) col = go.AddComponent<BoxCollider>();
                    col.enabled = true;
                    col.isTrigger = false;
                }
                else if (col != null)
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }
                return go;
            }

            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = scale;
            SetMat(go, mat);
            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static string EnsureRoomNumberPlaqueTexture(string assetPath, string roomNumber)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 128, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color plate = new Color(0.68f, 0.69f, 0.68f);
            Color rim = new Color(0.24f, 0.25f, 0.26f);
            Color ink = new Color(0.14f, 0.15f, 0.16f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    float grain = (PeriodicHash(x, y, w, h, 3109 + roomNumber.GetHashCode()) - 0.5f) * 0.04f;
                    Color c = border ? rim : new Color(plate.r + grain, plate.g + grain, plate.b + grain);
                    tex.SetPixel(x, y, c);
                }
            }

            // Crisp 5x7 pixel-font stencil numerals centered on the metal plaque
            string[][] glyphs = new string[][]
            {
                new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" }, // 0
                new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" }, // 1
                new[] { "01110", "10001", "00001", "00110", "01000", "10000", "11111" }, // 2
                new[] { "01110", "10001", "00001", "00110", "00001", "10001", "01110" }, // 3
                new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" }, // 4
                new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" }, // 5
                new[] { "01110", "10000", "11110", "10001", "10001", "10001", "01110" }, // 6
            };

            const int scale = 5;
            const int charW = 5 * scale;
            const int gap = 7;
            int totalW = roomNumber.Length * charW + (roomNumber.Length - 1) * gap;
            int startX = (w - totalW) / 2;
            int startY = (h - 7 * scale) / 2;

            for (int cIdx = 0; cIdx < roomNumber.Length; cIdx++)
            {
                int digit = roomNumber[cIdx] - '0';
                if (digit < 0 || digit >= glyphs.Length) continue;
                string[] g = glyphs[digit];
                int cx = startX + cIdx * (charW + gap);
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (g[row][col] != '1') continue;
                        int pyBase = startY + (6 - row) * scale;
                        int pxBase = cx + col * scale;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                tex.SetPixel(pxBase + dx, pyBase + dy, ink);
                            }
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureArchitecturalSignTexture(
            string assetPath, int w, int h, string line1, string line2, bool darkBronzeStyle, bool weatheredIncomplete, bool fadeLastChar = false)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color bg = darkBronzeStyle ? new Color(0.20f, 0.19f, 0.18f) : new Color(0.64f, 0.65f, 0.64f);
            Color rim = darkBronzeStyle ? new Color(0.42f, 0.37f, 0.31f) : new Color(0.22f, 0.23f, 0.24f);
            Color inkMain = darkBronzeStyle ? new Color(0.84f, 0.80f, 0.72f) : new Color(0.14f, 0.15f, 0.16f);
            Color inkSub = darkBronzeStyle ? new Color(0.66f, 0.62f, 0.56f) : new Color(0.26f, 0.28f, 0.30f);

            int seed = line1.GetHashCode() ^ (w * 31);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    float grain = (PeriodicHash(x, y, w, h, seed) - 0.5f) * 0.055f;
                    float wear = weatheredIncomplete ? PeriodicFbm((float)x / w, (float)y / h, 5, 3, 3, seed + 17) : 0.5f;
                    float dustDelta = weatheredIncomplete && wear > 0.65f ? (wear - 0.65f) * 0.16f : 0f;
                    Color baseC = border ? rim : bg;
                    // Mounting screw dots in corners
                    bool screw = ((x - 10) * (x - 10) + (y - 10) * (y - 10) < 9) ||
                                 ((x - (w - 11)) * (x - (w - 11)) + (y - 10) * (y - 10) < 9) ||
                                 ((x - 10) * (x - 10) + (y - (h - 11)) * (y - (h - 11)) < 9) ||
                                 ((x - (w - 11)) * (x - (w - 11)) + (y - (h - 11)) * (y - (h - 11)) < 9);
                    if (screw) baseC = rim * 0.75f;
                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(baseC.r + grain + dustDelta),
                        Mathf.Clamp01(baseC.g + grain + dustDelta),
                        Mathf.Clamp01(baseC.b + grain + dustDelta)));
                }
            }

            bool hasLine2 = !string.IsNullOrEmpty(line2);
            bool line1HasAccent = ContainsSpanishAccent(line1);
            int maxRows1 = line1HasAccent ? 10 : 7;
            int scale1 = Mathf.Max(2, Mathf.Min((w - 28) / Mathf.Max(1, line1.Length * 6), hasLine2 ? ((h / 2 - 10) / maxRows1) : ((h - 18) / maxRows1)));
            int line1Y = hasLine2 ? (h / 2 + 3) : ((h - maxRows1 * scale1) / 2);
            DrawBitmapTextCentered(tex, w, h, line1, line1Y, scale1, inkMain, fadeLastChar: fadeLastChar && line1.Length > 10);

            if (hasLine2)
            {
                // Subtle horizontal divider rule
                int ruleY = h / 2 - 3;
                for (int x = 18; x < w - 18; x++)
                {
                    tex.SetPixel(x, ruleY, rim);
                }
                int scale2 = Mathf.Max(2, Mathf.Min((w - 36) / Mathf.Max(1, line2.Length * 6), Mathf.Max(2, scale1 - 1)));
                int line2Y = Mathf.Max(8, (h / 2 - 6 - 7 * scale2) / 2 + 3);
                DrawBitmapTextCentered(tex, w, h, line2, line2Y, scale2, inkSub, fadeLastChar: false);
            }

            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static bool ContainsSpanishAccent(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = char.ToUpperInvariant(text[i]);
                if (ch == 'Á' || ch == 'É' || ch == 'Í' || ch == 'Ó' || ch == 'Ú' || ch == 'Ñ') return true;
            }
            return false;
        }

        private static void DrawBitmapTextCentered(Texture2D tex, int w, int h, string text, int startY, int scale, Color ink, bool fadeLastChar)
        {
            string upper = text.ToUpperInvariant();
            int charW = 5 * scale;
            int gap = Mathf.Max(2, scale);
            int totalW = upper.Length * charW + Mathf.Max(0, upper.Length - 1) * gap;
            int startX = (w - totalW) / 2;

            for (int i = 0; i < upper.Length; i++)
            {
                char ch = upper[i];
                bool hasAcute = ch == 'Á' || ch == 'É' || ch == 'Í' || ch == 'Ó' || ch == 'Ú';
                char baseCh = ch == 'Á' ? 'A' : ch == 'É' ? 'E' : ch == 'Í' ? 'I' : ch == 'Ó' ? 'O' : ch == 'Ú' ? 'U' : ch;
                string[] glyph = Get5x7Glyph(baseCh);
                if (glyph == null) continue;
                Color c = (fadeLastChar && i == upper.Length - 1) ? Color.Lerp(ink, new Color(0.35f, 0.34f, 0.32f), 0.62f) : ink;
                int cx = startX + i * (charW + gap);
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (glyph[row][col] != '1') continue;
                        int pyBase = startY + (6 - row) * scale;
                        int pxBase = cx + col * scale;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            int py = pyBase + dy;
                            if (py < 0 || py >= h) continue;
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = pxBase + dx;
                                if (px >= 0 && px < w) tex.SetPixel(px, py, c);
                            }
                        }
                    }
                }

                // Crisp diagonal acute accent (´) above accented Spanish vowels Á, É, Í, Ó, Ú
                if (hasAcute)
                {
                    int[] accentCols = { 2, 3 };
                    int[] accentRowsAbove = { 1, 2 }; // 1..2 grid rows above top of 7-row glyph
                    for (int a = 0; a < 2; a++)
                    {
                        int pyBase = startY + (6 + accentRowsAbove[a]) * scale;
                        int pxBase = cx + accentCols[a] * scale;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            int py = pyBase + dy;
                            if (py < 0 || py >= h) continue;
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = pxBase + dx;
                                if (px >= 0 && px < w) tex.SetPixel(px, py, c);
                            }
                        }
                    }
                }
            }
        }

        private static string[] Get5x7Glyph(char ch)
        {
            switch (ch)
            {
                case 'A': return new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'B': return new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" };
                case 'C': return new[] { "01110", "10001", "10000", "10000", "10000", "10001", "01110" };
                case 'D': return new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" };
                case 'E': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
                case 'F': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" };
                case 'G': return new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01110" };
                case 'H': return new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'I': return new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" };
                case 'L': return new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" };
                case 'M': return new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" };
                case 'N': return new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" };
                case 'O': return new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'P': return new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" };
                case 'R': return new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
                case 'S': return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
                case 'T': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" };
                case 'U': return new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'V': return new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" };
                case 'Y': return new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
                case '0': return new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" };
                case '1': return new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" };
                case '6': return new[] { "01110", "10000", "11110", "10001", "10001", "10001", "01110" };
                case '-': return new[] { "00000", "00000", "00000", "11111", "00000", "00000", "00000" };
                default: return null;
            }
        }

        /// <summary>
        /// Read-only verification gate for the currently loaded BackroomsLevel1Scene.unity
        /// (Remanencia Nivel 0 — Hotel de las Palomas architecture, environmental identity & visual invariants).
        /// </summary>
        public static bool RunVerificationChecks()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            if (!scene.IsValid())
            {
                Debug.LogError($"[BackroomsLevel1Builder] RunVerificationChecks failed: could not open {ScenePath}.");
                return false;
            }

            GameObject root = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "BackroomsLevel1_Root") { root = go; break; }
            }
            if (root == null)
            {
                Debug.LogError("[BackroomsLevel1Builder] RunVerificationChecks failed: missing BackroomsLevel1_Root.");
                return false;
            }

            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform lights = root.transform.Find("Backrooms_FluorescentLights");
            Transform artifacts = root.transform.Find("Backrooms_DispersedArtifacts");
            Transform spawn = root.transform.Find("Backrooms_SpawnPoint");
            Transform exitDoor = root.transform.Find("Backrooms_ExitDoor_ToStreet");

            if (shell == null || lights == null || artifacts == null || spawn == null || exitDoor == null)
            {
                Debug.LogError("[BackroomsLevel1Builder] RunVerificationChecks failed: required scene roots missing.");
                return false;
            }

            Transform hotelRoot = shell.Find(HotelArchitectureRootName);
            Transform envRoot = hotelRoot != null ? hotelRoot.Find(HotelEnvironmentalIdentityRootName) : null;
            bool hasHotelStructure = hotelRoot != null &&
                                     hotelRoot.Find("Hotel_Entrance_Vestibule") != null &&
                                     hotelRoot.Find("Hotel_Lobby_Reception") != null &&
                                     hotelRoot.Find("Hotel_Elevator_Core") != null &&
                                     hotelRoot.Find("Hotel_GuestCorridor") != null &&
                                     hotelRoot.Find("Hotel_Service_Wing") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_101") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_102") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_103") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_104") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_105") != null &&
                                     hotelRoot.Find("Hotel_GuestRoom_106") != null &&
                                     envRoot != null &&
                                     envRoot.Find("Hotel_Identity_Signage") != null &&
                                     envRoot.Find("Hotel_Infra_Electrical") != null &&
                                     envRoot.Find("Hotel_Infra_Plumbing") != null &&
                                     envRoot.Find("Hotel_Infra_Structural") != null &&
                                     envRoot.Find("Hotel_Room_Variations") != null;

            int retainedWallCount = 0, pillarCount = 0, missingVoidCount = 0, crookedHatchCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                string n = shell.GetChild(i).name;
                if (n.StartsWith("Wall_", StringComparison.Ordinal) && !n.EndsWith("_Baseboard", StringComparison.Ordinal)) retainedWallCount++;
                else if (n.StartsWith("Pillar_", StringComparison.Ordinal) && !n.EndsWith("_Baseboard", StringComparison.Ordinal)) pillarCount++;
                else if (n == "Backrooms_MissingTileVoid") missingVoidCount++;
                else if (n == "Backrooms_CrookedCeilingTile") crookedHatchCount++;
            }

            int fixtureCount = lights.childCount;
            Light[] fixtureLights = lights.GetComponentsInChildren<Light>(includeInactive: true);
            BackroomsAmbientFlicker[] flickers = lights.GetComponentsInChildren<BackroomsAmbientFlicker>(includeInactive: true);
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            int shadowCastingLights = 0;
            for (int i = 0; i < allLights.Length; i++)
            {
                if (allLights[i].shadows != LightShadows.None) shadowCastingLights++;
            }

            bool ok = hasHotelStructure &&
                      retainedWallCount == 15 &&
                      pillarCount == 13 &&
                      missingVoidCount == 2 &&
                      crookedHatchCount == 2 &&
                      fixtureCount == 24 &&
                      fixtureLights.Length == 16 &&
                      flickers.Length == 5 &&
                      shadowCastingLights == 0 &&
                      artifacts.Find("SecretRoom_HeavyVaultSafe") != null &&
                      artifacts.Find("Portal_DimensionalMirror_Root") != null;

            Debug.Log($"[BackroomsLevel1Builder] RunVerificationChecks: ok={ok} (hotelStructure={hasHotelStructure}, retainedWalls={retainedWallCount}, pillars={pillarCount}, fixtures={fixtureCount}, fixtureLights={fixtureLights.Length}, flickers={flickers.Length}, shadowLights={shadowCastingLights}).");
            return ok;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in existing)
            {
                if (string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    s.enabled = true;
                    EditorBuildSettings.scenes = existing.ToArray();
                    return;
                }
            }
            existing.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
        }

        // =====================================================================
        // REALITY-BRIDGE PROPS (FUTURE LIVE API HOOKS TO THE REAL WORLD)
        // =====================================================================
        private static void BuildLoneLaptopWorkstation(
            Transform parent, Vector3 pos, float yaw,
            Material deskMat, Material metalMat, Material chairMat, Material aluminumMat,
            Material laptopScreenMat, Material obsidianMat)
        {
            GameObject root = new GameObject("RealityBridge_LaptopWorkstation");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // The ONLY office desk in Backrooms Level 1
            CreateOfficeDesk(root.transform, Vector3.zero, 0f, deskMat, metalMat);
            // The ONLY office chair in Backrooms Level 1 (pulled slightly back as if someone just stood up)
            CreateOfficeChair(root.transform, new Vector3(-0.08f, 0f, -0.78f), 12f, chairMat, metalMat);

            // Sleek open Laptop on desk (future hook: YouTube mystery channel / live webpage / API viewer)
            GameObject lap = new GameObject("RealityBridge_Laptop");
            lap.transform.SetParent(root.transform, false);
            lap.transform.localPosition = new Vector3(-0.06f, 0.77f, 0.04f);
            lap.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);

            GameObject baseChassis = CreateBox("Laptop_Base", lap.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.36f, 0.016f, 0.25f), aluminumMat, keepCollider: false);
            CreateBox("Laptop_Keyboard", lap.transform, new Vector3(0f, 0.02f, -0.02f), new Vector3(0.31f, 0.005f, 0.13f), obsidianMat, keepCollider: false);
            CreateBox("Laptop_Trackpad", lap.transform, new Vector3(0f, 0.02f, 0.08f), new Vector3(0.11f, 0.004f, 0.06f), metalMat, keepCollider: false);

            GameObject lidPivot = new GameObject("Laptop_LidPivot");
            lidPivot.transform.SetParent(lap.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.018f, 0.12f);
            lidPivot.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
            CreateBox("Laptop_LidBack", lidPivot.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.24f, 0.012f), aluminumMat, keepCollider: false);
            CreateBox("Laptop_DisplayScreen", lidPivot.transform, new Vector3(0f, 0.12f, -0.008f), new Vector3(0.33f, 0.205f, 0.004f), laptopScreenMat, keepCollider: false);

            BoxCollider col = baseChassis.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 7.5f, 0f);
            col.size = new Vector3(1.5f, 18.0f, 1.8f);
            col.isTrigger = true;
            AttachDesc(baseChassis, "reality-bridge:laptop-mystery-terminal",
                "Estación Solitaria • Laptop Conectada (Enlace Web / YouTube)",
                "Lone Workstation • Connected Laptop (Web / YouTube Bridge)",
                "El único escritorio y silla en todo este nivel. Su pantalla permanece encendida buscando señal de red: preparada para futuras transmisiones reales de YouTube y archivos de misterio.",
                "The sole desk and chair in this entire level. Its glowing screen searches for an uplink: ready for future live YouTube mystery channels and real-world web feeds.",
                new Color(0.45f, 0.82f, 1.0f));
        }

        private static void BuildFmRadioTable(
            Transform parent, Vector3 pos, float yaw,
            Material woodMat, Material brassMat, Material aluminumMat, Material obsidianMat, Material dialMat)
        {
            GameObject root = new GameObject("RealityBridge_FmRadioTable");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Compact wooden side table
            CreateBox("RadioTable_Top", root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.68f, 0.04f, 0.46f), woodMat, keepCollider: true);
            CreateBox("RadioTable_Leg1", root.transform, new Vector3(-0.28f, 0.36f, -0.18f), new Vector3(0.045f, 0.72f, 0.045f), woodMat, keepCollider: false);
            CreateBox("RadioTable_Leg2", root.transform, new Vector3( 0.28f, 0.36f, -0.18f), new Vector3(0.045f, 0.72f, 0.045f), woodMat, keepCollider: false);
            CreateBox("RadioTable_Leg3", root.transform, new Vector3(-0.28f, 0.36f,  0.18f), new Vector3(0.045f, 0.72f, 0.045f), woodMat, keepCollider: false);
            CreateBox("RadioTable_Leg4", root.transform, new Vector3( 0.28f, 0.36f,  0.18f), new Vector3(0.045f, 0.72f, 0.045f), woodMat, keepCollider: false);

            // Vintage FM Radio receiver
            GameObject radio = CreateBox("FmRadio_Cabinet", root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.36f, 0.22f, 0.16f), woodMat, keepCollider: false);
            radio.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            CreateBox("FmRadio_SpeakerGrille", radio.transform, new Vector3(-0.18f, -0.02f, 0.52f), new Vector3(0.52f, 0.72f, 0.06f), obsidianMat, keepCollider: false);
            CreateBox("FmRadio_GlowingDial", radio.transform, new Vector3(0.24f, 0.18f, 0.52f), new Vector3(0.34f, 0.24f, 0.05f), dialMat, keepCollider: false);
            CreateBox("FmRadio_Knob1", radio.transform, new Vector3(0.16f, -0.22f, 0.56f), new Vector3(0.11f, 0.16f, 0.14f), brassMat, keepCollider: false);
            CreateBox("FmRadio_Knob2", radio.transform, new Vector3(0.34f, -0.22f, 0.56f), new Vector3(0.11f, 0.16f, 0.14f), brassMat, keepCollider: false);
            GameObject ant = CreateBox("FmRadio_Antenna", radio.transform, new Vector3(0.38f, 1.35f, -0.25f), new Vector3(0.025f, 2.20f, 0.04f), aluminumMat, keepCollider: false);
            ant.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            BoxCollider col = radio.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.8f, 2.2f);
            col.isTrigger = true;
            AttachDesc(radio, "reality-bridge:fm-radio-receiver",
                "Radio FM Analógico • Sintonizador 99.9 MHz (Podcast / Audio Real)",
                "Analog FM Radio • 99.9 MHz Tuner (Live Podcast / Audio Bridge)",
                "Antiguo receptor FM con dial ámbar encendido en medio del pasillo. Preparado para enlazar podcasts paranormales reales y estaciones de radio en vivo en futuras etapas.",
                "Vintage FM radio glowing with an amber dial in the hall. Ready to stream real paranormal podcasts and live radio broadcasts in upcoming stages.",
                new Color(1.0f, 0.76f, 0.30f));
        }

        private static void BuildAlexaCornerStand(
            Transform parent, Vector3 pos, float yaw,
            Material woodMat, Material obsidianMat, Material cyanRingMat)
        {
            GameObject root = new GameObject("RealityBridge_AlexaCornerStand");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Corner cabinet pedestal (compact 0.52 x 0.78 x 0.52m)
            CreateBox("CornerStand_Body", root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.50f, 0.76f, 0.50f), woodMat, keepCollider: true);
            CreateBox("CornerStand_Top", root.transform, new Vector3(0f, 0.78f, 0f), new Vector3(0.56f, 0.04f, 0.56f), woodMat, keepCollider: false);

            // Smart Speaker (Echo / Alexa cylinder with iconic glowing cyan-blue top light ring)
            GameObject spk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spk.name = "Alexa_SmartSpeaker";
            spk.transform.SetParent(root.transform, false);
            spk.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            spk.transform.localScale = new Vector3(0.14f, 0.10f, 0.14f);
            SetMat(spk, obsidianMat);
            UnityEngine.Object.DestroyImmediate(spk.GetComponent<Collider>());

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Alexa_CyanListeningRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 1.005f, 0f);
            ring.transform.localScale = new Vector3(0.142f, 0.008f, 0.142f);
            SetMat(ring, cyanRingMat);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());

            BoxCollider trig = spk.AddComponent<BoxCollider>();
            trig.size = new Vector3(2.4f, 2.8f, 2.4f);
            trig.isTrigger = true;
            AttachDesc(spk, "reality-bridge:alexa-smart-speaker",
                "Altavoz Inteligente 'Alexa' en Mueble Esquinero (Enlace de Voz / Música)",
                "Smart Speaker 'Alexa' on Corner Stand (Voice / Music Bridge)",
                "Anillo cian en modo escucha sobre un mueble esquinero solitario. Preparado para conectar música real, podcasts paranormales o comandos de voz con el mundo exterior.",
                "Cyan light ring pulsing in listening mode atop a lone corner cabinet. Ready to stream real music, paranormal podcasts, or live external audio feeds.",
                new Color(0.18f, 0.88f, 1.0f));
        }

        private static void BuildDiarioJuarezIpadStand(
            Transform parent, Vector3 pos, float yaw,
            Material aluminumMat, Material obsidianMat, Material diarioScreenMat)
        {
            GameObject root = new GameObject("RealityBridge_DiarioJuarezIpad");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Sleek floor kiosk reading stand (post attaches cleanly behind angled iPad)
            CreateBox("IpadStand_Base", root.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.38f, 0.05f, 0.34f), aluminumMat, keepCollider: true);
            CreateBox("IpadStand_Post", root.transform, new Vector3(0f, 0.52f, -0.04f), new Vector3(0.05f, 1.00f, 0.05f), aluminumMat, keepCollider: true);

            GameObject holder = new GameObject("Ipad_AngledHolder");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0f, 1.10f, 0.01f);
            holder.transform.localRotation = Quaternion.Euler(-34f, 0f, 0f);

            GameObject chassis = CreateBox("Ipad_Chassis", holder.transform, Vector3.zero, new Vector3(0.30f, 0.40f, 0.018f), obsidianMat, keepCollider: false);
            CreateBox("Ipad_DiarioScreen", holder.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.27f, 0.37f, 0.004f), diarioScreenMat, keepCollider: false);

            BoxCollider col = chassis.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 1.6f, 12.0f);
            col.isTrigger = true;
            AttachDesc(chassis, "reality-bridge:diario-de-juarez-ipad",
                "iPad • Portada Digital 'El Diario de Juárez' (Noticias Reales del Día)",
                "iPad • 'El Diario de Juárez' Digital Front Page (Live Daily News)",
                "Tableta sobre atril mostrando la portada digital de El Diario de Juárez. Preparada para sincronizar titulares reales del día desde Ciudad Juárez dentro de Remanencia.",
                "Tablet on a floor stand displaying the digital front page of El Diario de Juárez. Ready to pull live daily headlines from Ciudad Juárez straight into Remanencia.",
                new Color(0.96f, 0.96f, 0.96f));
        }

        private static void BuildBorderClockTerminal(
            Transform parent, Vector3 pos, float yaw,
            Material steelMat, Material screenMat, Material glowMat)
        {
            GameObject root = new GameObject("RealityBridge_BorderClockTerminal");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject housing = CreateBox("Terminal_Frame", root.transform, Vector3.zero, new Vector3(0.92f, 0.44f, 0.07f), steelMat, keepCollider: false);
            CreateBox("Terminal_Display", root.transform, new Vector3(0f, 0f, 0.038f), new Vector3(0.84f, 0.36f, 0.008f), screenMat, keepCollider: false);
            CreateBox("Terminal_StatusLed", root.transform, new Vector3(0.40f, 0.18f, 0.04f), new Vector3(0.03f, 0.03f, 0.01f), glowMat, keepCollider: false);

            BoxCollider col = housing.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.5f, 4.0f);
            col.isTrigger = true;
            AttachDesc(housing, "reality-bridge:juarez-live-telemetry",
                "Monitor de Telemetría Real • Clima, Hora y Puente Paso del Norte",
                "Real-World Telemetry Monitor • Juárez Weather, Clock & Bridge Wait",
                "Pantalla mural anómala que recibe señales del exterior: clima real de Ciudad Juárez, hora local y tiempos del Puente Paso del Norte (lista para enlazar a World Core).",
                "Anomalous wall monitor receiving signals from the outside: live Ciudad Juárez weather, local time, and Paso del Norte wait times (ready to bind to World Core).",
                new Color(0.32f, 0.95f, 0.85f));
        }

        private static void BuildWallPayphoneTerminal(
            Transform parent, Vector3 pos, float yaw,
            Material aluminumMat, Material obsidianMat, Material redMat, Material brassMat)
        {
            GameObject root = new GameObject("RealityBridge_PayphoneTerminal");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject booth = CreateBox("Payphone_StainlessBody", root.transform, Vector3.zero, new Vector3(0.30f, 0.56f, 0.16f), aluminumMat, keepCollider: false);
            CreateBox("Payphone_KeypadPlate", root.transform, new Vector3(0.02f, 0.04f, 0.085f), new Vector3(0.14f, 0.18f, 0.014f), obsidianMat, keepCollider: false);
            CreateBox("Payphone_CoinSlot", root.transform, new Vector3(0.09f, 0.18f, 0.085f), new Vector3(0.04f, 0.06f, 0.016f), brassMat, keepCollider: false);
            CreateBox("Payphone_ReturnGlow", root.transform, new Vector3(0.02f, -0.17f, 0.085f), new Vector3(0.10f, 0.07f, 0.014f), redMat, keepCollider: false);
            CreateBox("Payphone_Handset", root.transform, new Vector3(-0.11f, 0.06f, 0.09f), new Vector3(0.055f, 0.28f, 0.06f), obsidianMat, keepCollider: false);

            BoxCollider col = booth.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 1.3f, 2.2f);
            col.isTrigger = true;
            AttachDesc(booth, "reality-bridge:payphone-voice-line",
                "Teléfono Público de Monedas (Línea Directa / Mensajes del Exterior)",
                "Public Coin Payphone (External Voice / Mystery Line Bridge)",
                "Caseta metálica de los 90 instalada en un pasillo amarillo sin salida. Preparada para recibir llamadas misteriosas con voz IA o mensajes de audio reales.",
                "1990s steel payphone mounted in a yellow corridor. Ready for future AI voice mystery calls or live external audio messages.",
                new Color(1.0f, 0.42f, 0.42f));
        }

        private static void BuildStreetCamPhotoFrame(
            Transform parent, Vector3 pos, float yaw,
            Material steelMat, Material staticMat, Material aluminumMat)
        {
            GameObject root = new GameObject("RealityBridge_CctvPhotoFrame");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject frame = CreateBox("CctvFrame_Bezel", root.transform, Vector3.zero, new Vector3(0.68f, 0.46f, 0.045f), steelMat, keepCollider: false);
            CreateBox("CctvFrame_InnerTrim", root.transform, new Vector3(0f, 0f, 0.024f), new Vector3(0.62f, 0.40f, 0.008f), aluminumMat, keepCollider: false);
            CreateBox("CctvFrame_Feed", root.transform, new Vector3(0f, 0f, 0.030f), new Vector3(0.58f, 0.36f, 0.006f), staticMat, keepCollider: false);

            BoxCollider col = frame.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.4f, 5.0f);
            col.isTrigger = true;
            AttachDesc(frame, "reality-bridge:fideicomiso-cctv-frame",
                "Marco Digital CCTV • Ventana al Puente Paso del Norte",
                "Digital CCTV Frame • Window to Paso del Norte Bridge",
                "Marco metálico colgado en la pared que capta capturas granuladas de las cámaras del Fideicomiso en tiempo real (preparado para sincronizar fotos reales de la calle).",
                "Wall-mounted steel display catching grainy snapshots from the real Paso del Norte Fideicomiso cameras (ready for live street-cam snapshots).",
                new Color(0.58f, 0.84f, 1.0f));
        }

        private static void BuildThermalPrinterPedestal(
            Transform parent, Vector3 pos, float yaw,
            Material woodMat, Material beigeMat, Material paperMat, Material obsidianMat, Material ledMat)
        {
            GameObject root = new GameObject("RealityBridge_ThermalPrinter");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox("Printer_Pedestal", root.transform, new Vector3(0f, 0.44f, 0f), new Vector3(0.46f, 0.88f, 0.46f), woodMat, keepCollider: true);
            GameObject body = CreateBox("Printer_Body", root.transform, new Vector3(0f, 0.96f, 0f), new Vector3(0.24f, 0.16f, 0.28f), beigeMat, keepCollider: false);
            CreateBox("Printer_Slot", root.transform, new Vector3(0f, 1.045f, 0.04f), new Vector3(0.16f, 0.01f, 0.025f), obsidianMat, keepCollider: false);
            CreateBox("Printer_Led", root.transform, new Vector3(0.08f, 1.02f, 0.142f), new Vector3(0.02f, 0.02f, 0.01f), ledMat, keepCollider: false);
            // Long curling receipt tape spilling over the front edge!
            GameObject tape = CreateBox("Printer_PaperRibbon", root.transform, new Vector3(0f, 0.88f, 0.24f), new Vector3(0.12f, 0.48f, 0.008f), paperMat, keepCollider: false);
            tape.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);

            BoxCollider col = body.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 2.5f, 1.8f);
            col.isTrigger = true;
            AttachDesc(body, "reality-bridge:thermal-teletype-printer",
                "Impresora Térmica de Teletipo (Reportes y Códigos del Mundo Real)",
                "Thermal Teletype Printer (Live World Core Reports & Codes)",
                "Imprime tiras interminables de papel sobre la alfombra húmeda. Preparada para recibir códigos diarios, reportes ciudadanos o pistas en vivo desde el backend.",
                "Spools endless receipt paper onto the damp carpet. Ready to print daily secret codes, live citizen reports, or clues straight from World Core.",
                new Color(0.88f, 0.94f, 1.0f));
        }

        // =====================================================================
        // STRUCTURAL & PROP BUILDERS
        // =====================================================================
        private static void CreateMissingCeilingTile(Transform parent, Vector3 pos, Material voidMat, Material hatchMat)
        {
            CreateBox("Backrooms_MissingTileVoid", parent,
                new Vector3(pos.x, pos.y - 0.008f, pos.z), new Vector3(0.86f, 0.024f, 0.62f), voidMat, keepCollider: false);
            GameObject serviceHatch = CreateBox("Backrooms_CrookedCeilingTile", parent,
                new Vector3(pos.x + 0.40f, pos.y - 0.085f, pos.z + 0.02f), new Vector3(0.78f, 0.022f, 0.56f), hatchMat, keepCollider: false);
            serviceHatch.transform.localRotation = Quaternion.Euler(11f, 4f, -15f);
        }

        private static void CreateWallSegment(string name, Transform parent, Vector2 a, Vector2 b, float thickness, Material wallMat, Material baseMat)
        {
            Vector2 delta = b - a;
            float len = delta.magnitude;
            Vector2 mid = 0.5f * (a + b);
            Quaternion rot = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.y), Vector3.up);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(mid.x, BaseY + ClearHeight * 0.5f, mid.y);
            wall.transform.rotation = rot;
            wall.transform.localScale = new Vector3(thickness, ClearHeight, len + thickness * 0.5f);
            SetMat(wall, wallMat);

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name + "_Baseboard";
            trim.transform.SetParent(parent, false);
            trim.transform.position = new Vector3(mid.x, BaseY + 0.06f, mid.y);
            trim.transform.rotation = rot;
            trim.transform.localScale = new Vector3(thickness + 0.025f, 0.12f, len + thickness * 0.52f);
            SetMat(trim, baseMat);
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static void CreatePillar(string name, Transform parent, float x, float z, float size, Material wallMat, Material baseMat)
        {
            CreateBox(name, parent, new Vector3(x, BaseY + ClearHeight * 0.5f, z), new Vector3(size, ClearHeight, size), wallMat, keepCollider: true);
            CreateBox(name + "_Baseboard", parent, new Vector3(x, BaseY + 0.06f, z), new Vector3(size + 0.03f, 0.12f, size + 0.03f), baseMat, keepCollider: false);
        }

        private static void CreateOfficeDesk(Transform parent, Vector3 localPos, float yaw, Material topMat, Material legMat)
        {
            GameObject g = new GameObject("BackroomsProp_OfficeDesk");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Desk_Top", g.transform, new Vector3(0f, 0.74f, 0f), new Vector3(1.36f, 0.05f, 0.68f), topMat, keepCollider: true);
            CreateBox("Desk_Drawers", g.transform, new Vector3(0.45f, 0.36f, 0f), new Vector3(0.40f, 0.72f, 0.60f), topMat, keepCollider: true);
            CreateBox("Desk_LegL1", g.transform, new Vector3(-0.62f, 0.36f, -0.28f), new Vector3(0.05f, 0.72f, 0.05f), legMat, keepCollider: false);
            CreateBox("Desk_LegL2", g.transform, new Vector3(-0.62f, 0.36f, 0.28f), new Vector3(0.05f, 0.72f, 0.05f), legMat, keepCollider: false);
        }

        private static void CreateOfficeChair(Transform parent, Vector3 localPos, float yaw, Material fabricMat, Material metalMat)
        {
            GameObject g = new GameObject("BackroomsProp_OfficeChair");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Chair_Seat", g.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.48f, 0.06f, 0.46f), fabricMat, keepCollider: true);
            CreateBox("Chair_Back", g.transform, new Vector3(0f, 0.74f, -0.21f), new Vector3(0.44f, 0.50f, 0.06f), fabricMat, keepCollider: false);
            CreateBox("Chair_Stem", g.transform, new Vector3(0f, 0.23f, 0f), new Vector3(0.05f, 0.44f, 0.05f), metalMat, keepCollider: false);
            CreateBox("Chair_BaseX", g.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.52f, 0.04f, 0.05f), metalMat, keepCollider: false);
            CreateBox("Chair_BaseZ", g.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.04f, 0.52f), metalMat, keepCollider: false);
        }

        private static void CreateStepLadder(Transform parent, Vector3 pos, float yaw, Material woodMat)
        {
            GameObject g = new GameObject("BackroomsProp_StepLadder");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject legF = CreateBox("Ladder_Front", g.transform, new Vector3(0f, 0.65f, 0.20f), new Vector3(0.48f, 1.35f, 0.05f), woodMat, keepCollider: false);
            legF.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            GameObject legB = CreateBox("Ladder_Back", g.transform, new Vector3(0f, 0.65f, -0.20f), new Vector3(0.48f, 1.35f, 0.05f), woodMat, keepCollider: false);
            legB.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            CreateBox("Ladder_Top", g.transform, new Vector3(0f, 1.30f, 0f), new Vector3(0.50f, 0.04f, 0.22f), woodMat, keepCollider: false);
            BoxCollider col = g.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.size = new Vector3(0.52f, 1.32f, 0.56f);
        }

        private static void CreateWaterCooler(Transform parent, Vector3 pos, Material bodyMat, Material jugMat)
        {
            GameObject g = new GameObject("BackroomsProp_WaterCooler");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            CreateBox("Cooler_Base", g.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.36f, 0.92f, 0.36f), bodyMat, keepCollider: true);
            CreateBox("Cooler_Jug", g.transform, new Vector3(0f, 1.12f, 0f), new Vector3(0.28f, 0.40f, 0.28f), jugMat, keepCollider: false);
        }

        private static void CreateCardboardStack(Transform parent, Vector3 pos, float yaw, Material boxMat)
        {
            GameObject g = new GameObject("BackroomsProp_CardboardStack");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Box_Bot1", g.transform, new Vector3(-0.22f, 0.20f, 0f), new Vector3(0.42f, 0.40f, 0.38f), boxMat, keepCollider: true);
            CreateBox("Box_Bot2", g.transform, new Vector3(0.24f, 0.18f, 0.06f), new Vector3(0.40f, 0.36f, 0.36f), boxMat, keepCollider: true);
            GameObject top = CreateBox("Box_Top", g.transform, new Vector3(-0.08f, 0.56f, 0.02f), new Vector3(0.38f, 0.32f, 0.34f), boxMat, keepCollider: false);
            top.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);
        }

        // =====================================================================
        // THE 13 ARTIFACTS, LEVEL-OMEGA SAFE & MONUMENTAL DIMENSIONAL PORTAL
        // =====================================================================
        private static void BuildVaultSafeWithAstrolabe(
            Transform parent, SecretRoomDimensionVaultController vaultCtrl, Vector3 pos, Quaternion rot,
            Material darkCorniceMat, Material safeGunmetalMat, Material crimsonVelvetMat, Material obsidianMat,
            Material antiqueBrassMat, Material aluminumMat, Material safeLedAmberMat,
            Material astrolabeCoreMat, Material portalRuneVioletMat)
        {
            GameObject safeRoot = new GameObject("SecretRoom_HeavyVaultSafe");
            safeRoot.transform.SetParent(parent, false);
            safeRoot.transform.position = pos;
            safeRoot.transform.rotation = rot;

            CreateBox("VaultSafe_BasePlinth", safeRoot.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.76f, 0.08f, 0.56f), darkCorniceMat, keepCollider: true);
            CreateBox("VaultSafe_BackWall", safeRoot.transform, new Vector3(0f, 0.54f, -0.23f), new Vector3(0.72f, 0.92f, 0.06f), safeGunmetalMat, keepCollider: true);
            CreateBox("VaultSafe_LeftWall", safeRoot.transform, new Vector3(-0.33f, 0.54f, 0f), new Vector3(0.06f, 0.92f, 0.52f), safeGunmetalMat, keepCollider: true);
            CreateBox("VaultSafe_RightWall", safeRoot.transform, new Vector3(0.33f, 0.54f, 0f), new Vector3(0.06f, 0.92f, 0.52f), safeGunmetalMat, keepCollider: true);
            CreateBox("VaultSafe_BottomWall", safeRoot.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.72f, 0.06f, 0.52f), safeGunmetalMat, keepCollider: true);
            CreateBox("VaultSafe_TopWall", safeRoot.transform, new Vector3(0f, 0.97f, 0f), new Vector3(0.72f, 0.06f, 0.52f), safeGunmetalMat, keepCollider: true);

            CreateBox("VaultSafe_VelvetFloor", safeRoot.transform, new Vector3(0f, 0.15f, -0.02f), new Vector3(0.60f, 0.02f, 0.40f), crimsonVelvetMat, keepCollider: false);
            CreateBox("VaultSafe_VelvetBack", safeRoot.transform, new Vector3(0f, 0.54f, -0.19f), new Vector3(0.60f, 0.78f, 0.02f), crimsonVelvetMat, keepCollider: false);
            CreateBox("VaultSafe_KeystonePedestal", safeRoot.transform, new Vector3(0f, 0.28f, -0.02f), new Vector3(0.24f, 0.22f, 0.20f), obsidianMat, keepCollider: false);
            CreateBox("VaultSafe_PedestalVelvetTop", safeRoot.transform, new Vector3(0f, 0.40f, -0.02f), new Vector3(0.22f, 0.02f, 0.18f), crimsonVelvetMat, keepCollider: false);

            GameObject safeAstro = new GameObject("VaultSafe_AstrolabeKeystone");
            safeAstro.transform.SetParent(safeRoot.transform, false);
            safeAstro.transform.localPosition = new Vector3(0f, 0.55f, -0.02f);
            BoxCollider astroTrig = safeAstro.AddComponent<BoxCollider>();
            astroTrig.size = new Vector3(0.30f, 0.30f, 0.24f);
            astroTrig.isTrigger = true;
            AttachDesc(safeAstro, "artifact:ojo-del-umbral",
                "El Ojo del Umbral (Astrolabio Dimensional de Obsidiana)",
                "The Eye of the Threshold (Obsidian Dimensional Astrolabe)",
                "Reliquia octagonal de latón grabado y obsidiana con un núcleo de fisura astral. Encaja en la Cuenca Octagonal de la única Puerta Dimensional del laberinto (en el extremo Suroeste).",
                "Octagonal relic of engraved brass and obsidian with an astral rift core. Fits into the Octagonal Socket of the maze's single Dimensional Portal (in the far Southwest chamber).",
                new Color(0.32f, 0.94f, 1.0f));
            CreateAstrolabeModel("Astrolabe_VisualModel", safeAstro.transform, Vector3.zero, Quaternion.Euler(-14f, 0f, 0f), 1.35f,
                antiqueBrassMat, obsidianMat, astrolabeCoreMat, portalRuneVioletMat, out Transform safeSpin);

            vaultCtrl.astrolabeInSafeObject = safeAstro;
            vaultCtrl.astrolabeSafeVisualTransform = safeSpin;
            vaultCtrl.safeAstrolabeAnchor = safeAstro.transform.position;

            GameObject doorPivot = new GameObject("VaultSafe_DoorPivot");
            doorPivot.transform.SetParent(safeRoot.transform, false);
            doorPivot.transform.localPosition = new Vector3(-0.31f, 0.14f, 0.24f);
            GameObject doorLeaf = CreateBox("VaultSafe_DoorLeaf", doorPivot.transform, new Vector3(0.31f, 0.40f, 0f), new Vector3(0.62f, 0.80f, 0.05f), safeGunmetalMat, keepCollider: true);
            BoxCollider clickTrig = doorLeaf.AddComponent<BoxCollider>();
            clickTrig.isTrigger = true;
            for (int b = 0; b < 3; b++)
            {
                CreateBox($"VaultSafe_Bolt_{b}", doorPivot.transform, new Vector3(0.61f, 0.16f + b * 0.24f, -0.02f), new Vector3(0.06f, 0.036f, 0.032f), aluminumMat, keepCollider: false);
            }
            CreateBox("VaultSafe_WheelHub", doorPivot.transform, new Vector3(0.20f, 0.40f, 0.04f), new Vector3(0.07f, 0.07f, 0.04f), antiqueBrassMat, keepCollider: false);
            CreateBox("VaultSafe_SpokeV", doorPivot.transform, new Vector3(0.20f, 0.40f, 0.055f), new Vector3(0.024f, 0.22f, 0.018f), antiqueBrassMat, keepCollider: false);
            CreateBox("VaultSafe_SpokeH", doorPivot.transform, new Vector3(0.20f, 0.40f, 0.055f), new Vector3(0.22f, 0.024f, 0.018f), antiqueBrassMat, keepCollider: false);

            GameObject kp = new GameObject("VaultSafe_KeypadLock");
            kp.transform.SetParent(doorPivot.transform, false);
            kp.transform.localPosition = new Vector3(0.46f, 0.44f, 0.034f);
            CreateBox("VaultSafe_KeypadHousing", kp.transform, Vector3.zero, new Vector3(0.11f, 0.18f, 0.025f), obsidianMat, keepCollider: false);
            CreateBox("VaultSafe_KeypadBezel", kp.transform, new Vector3(0f, 0f, -0.006f), new Vector3(0.125f, 0.195f, 0.014f), antiqueBrassMat, keepCollider: false);
            GameObject ledObj = CreateBox("VaultSafe_KeypadLED", kp.transform, new Vector3(0f, 0.065f, 0.014f), new Vector3(0.072f, 0.018f, 0.008f), safeLedAmberMat, keepCollider: false);

            GameObject blockerGo = new GameObject("VaultSafe_ClosedBlocker");
            blockerGo.transform.SetParent(safeRoot.transform, false);
            blockerGo.transform.localPosition = new Vector3(0f, 0.54f, 0.24f);
            BoxCollider blockerCol = blockerGo.AddComponent<BoxCollider>();
            blockerCol.size = new Vector3(0.62f, 0.80f, 0.05f);
            blockerCol.isTrigger = true;

            SecretKeypadDoorController safeDoorCtrl = safeRoot.AddComponent<SecretKeypadDoorController>();
            safeDoorCtrl.doorId = "safe:backrooms-level1-omega-vault";
            safeDoorCtrl.doorTitleEs = "Caja Fuerte de Contención (Nivel Ω)";
            safeDoorCtrl.doorTitleEn = "Containment Vault Safe (Level Ω)";
            safeDoorCtrl.activeAccessCode = "654321";
            safeDoorCtrl.requiredCodeLength = 6;
            safeDoorCtrl.allowInteriorSafetyBypass = false;
            safeDoorCtrl.southLeafPivot = doorPivot.transform;
            safeDoorCtrl.northLeafPivot = null;
            safeDoorCtrl.closedDoorBlockerCollider = blockerCol;
            safeDoorCtrl.openAngleDegrees = -95f;
            safeDoorCtrl.doorSwingSpeed = 2.6f;
            safeDoorCtrl.keypadInteractionAnchor = safeRoot.transform.TransformPoint(new Vector3(0f, 0.65f, 0.34f));
            safeDoorCtrl.interactionRadius = 2.20f;
            safeDoorCtrl.statusLedRenderer = ledObj.GetComponent<Renderer>();
            vaultCtrl.vaultSafeDoorController = safeDoorCtrl;
        }

        private static void BuildMonumentalDimensionalPortal(
            Transform parent, SecretRoomDimensionVaultController vaultCtrl, Vector3 pos, Quaternion rot,
            Material saltWhiteMat, Material obsidianMat, Material antiqueBrassMat, Material portalDormantMat,
            Material portalRuneVioletMat, Material astrolabeCoreMat, Material rackSteelMat, Material redThreadMat)
        {
            GameObject portalRoot = new GameObject("Portal_DimensionalMirror_Root");
            portalRoot.transform.SetParent(parent, false);
            portalRoot.transform.position = pos;
            portalRoot.transform.rotation = rot;

            CreateBox("Vault_Floor_SaltLine_Portal", portalRoot.transform, new Vector3(0.42f, 0.008f, 0f), new Vector3(0.055f, 0.016f, 1.48f), saltWhiteMat, keepCollider: false);
            CreateBox("Portal_ThresholdPlinth_Base", portalRoot.transform, new Vector3(0.05f, 0.05f, 0f), new Vector3(0.22f, 0.10f, 1.38f), obsidianMat, keepCollider: false);
            CreateBox("Portal_ThresholdPlinth_BrassTrim", portalRoot.transform, new Vector3(0.06f, 0.11f, 0f), new Vector3(0.18f, 0.03f, 1.30f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_Frame_LeftPillar", portalRoot.transform, new Vector3(0.04f, 1.28f, -0.58f), new Vector3(0.16f, 2.38f, 0.15f), obsidianMat, keepCollider: true);
            CreateBox("Portal_Frame_RightPillar", portalRoot.transform, new Vector3(0.04f, 1.28f, 0.58f), new Vector3(0.16f, 2.38f, 0.15f), obsidianMat, keepCollider: true);

            float[] ringYs = new float[] { 0.45f, 0.96f, 1.68f, 2.18f };
            for (int i = 0; i < ringYs.Length; i++)
            {
                CreateBox($"Portal_LBand_{i}", portalRoot.transform, new Vector3(0.05f, ringYs[i], -0.58f), new Vector3(0.18f, 0.045f, 0.17f), antiqueBrassMat, keepCollider: false);
                CreateBox($"Portal_RBand_{i}", portalRoot.transform, new Vector3(0.05f, ringYs[i], 0.58f), new Vector3(0.18f, 0.045f, 0.17f), antiqueBrassMat, keepCollider: false);
            }

            CreateBox("Portal_Frame_TopArchBeam", portalRoot.transform, new Vector3(0.06f, 2.48f, 0f), new Vector3(0.18f, 0.14f, 1.42f), obsidianMat, keepCollider: false);
            CreateBox("Portal_Frame_TopBrassCornice", portalRoot.transform, new Vector3(0.07f, 2.57f, 0f), new Vector3(0.20f, 0.05f, 1.46f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_Frame_CrownKeystone", portalRoot.transform, new Vector3(0.09f, 2.52f, 0f), new Vector3(0.20f, 0.22f, 0.22f), obsidianMat, keepCollider: false);
            CreateBox("Portal_Frame_CrownRuneGem", portalRoot.transform, new Vector3(0.18f, 2.52f, 0f), new Vector3(0.04f, 0.11f, 0.11f), portalRuneVioletMat, keepCollider: false);

            GameObject mirrorSurface = CreateBox("Portal_MirrorSurface", portalRoot.transform, new Vector3(0.02f, 1.28f, 0f), new Vector3(0.04f, 2.28f, 1.00f), portalDormantMat, keepCollider: false);
            BoxCollider mTrig = mirrorSurface.AddComponent<BoxCollider>();
            mTrig.isTrigger = true;
            vaultCtrl.portalMirrorRenderer = mirrorSurface.GetComponent<Renderer>();
            AttachDesc(mirrorSurface, "portal:umbral-de-lasser",
                "Pasaje dimensional",
                "Dimensional Passage",
                "El mecanismo está activo. Su destino no puede determinarse. La estructura parece conectar este espacio con otra región de Remanencia.",
                "The mechanism is active. Its destination cannot be determined. The structure appears to connect this space to another region of Remanencia.",
                new Color(0.55f, 0.86f, 1.0f));

            GameObject vortexGroup = new GameObject("Portal_ActiveVortexGroup");
            vortexGroup.transform.SetParent(portalRoot.transform, false);
            vortexGroup.transform.localPosition = new Vector3(0.055f, 1.28f, 0f);
            vortexGroup.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreateBox("Portal_VortexRing_Outer", vortexGroup.transform, Vector3.zero, new Vector3(0.76f, 0.76f, 0.016f), portalRuneVioletMat, keepCollider: false);
            GameObject midV = CreateBox("Portal_VortexRing_Mid", vortexGroup.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.54f, 0.54f, 0.016f), antiqueBrassMat, keepCollider: false);
            midV.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateBox("Portal_VortexRing_Inner", vortexGroup.transform, new Vector3(0f, 0f, 0.022f), new Vector3(0.32f, 0.32f, 0.018f), astrolabeCoreMat, keepCollider: false);
            vortexGroup.SetActive(false);
            vaultCtrl.portalActiveVortexGroup = vortexGroup;

            GameObject chainsGroup = new GameObject("Portal_ChainsGroup");
            chainsGroup.transform.SetParent(portalRoot.transform, false);
            chainsGroup.transform.localPosition = new Vector3(0.085f, 1.28f, 0f);
            GameObject c1 = CreateBox("Portal_SealChain_Diag1", chainsGroup.transform, Vector3.zero, new Vector3(0.036f, 2.08f, 0.055f), rackSteelMat, keepCollider: false);
            c1.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            GameObject c2 = CreateBox("Portal_SealChain_Diag2", chainsGroup.transform, Vector3.zero, new Vector3(0.036f, 2.08f, 0.055f), rackSteelMat, keepCollider: false);
            c2.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            CreateBox("Portal_SealPadlock_Body", chainsGroup.transform, new Vector3(0.025f, 0f, 0f), new Vector3(0.05f, 0.16f, 0.12f), antiqueBrassMat, keepCollider: false);
            CreateBox("Portal_SealPadlock_Rune", chainsGroup.transform, new Vector3(0.052f, 0f, 0f), new Vector3(0.015f, 0.07f, 0.07f), redThreadMat, keepCollider: false);
            vaultCtrl.portalChainsGroup = chainsGroup;

            GameObject riftLightGo = new GameObject("Portal_DimensionalRiftLight");
            riftLightGo.transform.SetParent(portalRoot.transform, false);
            riftLightGo.transform.localPosition = new Vector3(0.45f, 1.35f, 0f);
            Light rl = riftLightGo.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.58f, 0.24f, 0.92f);
            rl.intensity = 0.55f;
            rl.range = 5.2f;
            rl.shadows = LightShadows.None;
            vaultCtrl.portalRiftLight = rl;

            GameObject socketRoot = new GameObject("Portal_KeystoneSocket_Root");
            socketRoot.transform.SetParent(portalRoot.transform, false);
            socketRoot.transform.localPosition = new Vector3(0.12f, 1.28f, 0.58f);
            CreateBox("Socket_AltarConsole", socketRoot.transform, new Vector3(-0.02f, 0f, 0f), new Vector3(0.08f, 0.30f, 0.26f), obsidianMat, keepCollider: false);
            CreateBox("Socket_OctBezel_A", socketRoot.transform, new Vector3(0.02f, 0f, 0f), new Vector3(0.028f, 0.21f, 0.21f), antiqueBrassMat, keepCollider: false);
            GameObject octB = CreateBox("Socket_OctBezel_B", socketRoot.transform, new Vector3(0.02f, 0f, 0f), new Vector3(0.028f, 0.21f, 0.21f), antiqueBrassMat, keepCollider: false);
            octB.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            CreateBox("Socket_EmptyRecess_A", socketRoot.transform, new Vector3(0.032f, 0f, 0f), new Vector3(0.02f, 0.16f, 0.16f), obsidianMat, keepCollider: false);
            GameObject recB = CreateBox("Socket_EmptyRecess_B", socketRoot.transform, new Vector3(0.032f, 0f, 0f), new Vector3(0.02f, 0.16f, 0.16f), obsidianMat, keepCollider: false);
            recB.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            CreateBox("Socket_CenterContactPin", socketRoot.transform, new Vector3(0.04f, 0f, 0f), new Vector3(0.016f, 0.038f, 0.038f), portalRuneVioletMat, keepCollider: false);

            GameObject trigGo = new GameObject("Portal_KeystoneSocket_Trigger");
            trigGo.transform.SetParent(socketRoot.transform, false);
            trigGo.transform.localPosition = new Vector3(0.04f, 0f, 0f);
            BoxCollider sCol = trigGo.AddComponent<BoxCollider>();
            sCol.size = new Vector3(0.18f, 0.34f, 0.32f);
            sCol.isTrigger = true;
            AttachDesc(trigGo, "portal:cuenca-octagonal",
                "Cuenca Octagonal del Umbral (Receptáculo Astral)",
                "Octagonal Threshold Socket (Astral Receptacle)",
                "Receptáculo octagonal tallado en latón y obsidiana. Inserta aquí 'El Ojo del Umbral' (resguardado en la Caja Fuerte Nivel Ω del Ala Noroeste) para activar el Portal Dimensional.",
                "Octagonal brass-and-obsidian receptacle. Insert 'The Eye of the Threshold' (from the NW Wing Level-Ω Vault Safe) here to activate the Dimensional Portal.",
                new Color(1.0f, 0.78f, 0.32f));

            GameObject sockAstro = CreateAstrolabeModel("Portal_InsertedAstrolabeKeystone", socketRoot.transform,
                new Vector3(0.048f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 1.35f,
                antiqueBrassMat, obsidianMat, astrolabeCoreMat, portalRuneVioletMat, out Transform sockSpin);
            sockAstro.SetActive(false);
            vaultCtrl.astrolabeInSocketObject = sockAstro;
            vaultCtrl.astrolabeSocketRingTransform = sockSpin;
            vaultCtrl.portalSocketAnchor = socketRoot.transform.position;
            vaultCtrl.portalCenterAnchor = portalRoot.transform.position + Vector3.up * 1.30f;
        }

        private static GameObject CreateAstrolabeModel(
            string name, Transform parent, Vector3 localPos, Quaternion localRot, float scale,
            Material brassMat, Material obsidianMat, Material coreGlowMat, Material runeGlowMat, out Transform spinTransform)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;
            root.transform.localScale = Vector3.one * scale;

            CreateBox("Astrolabe_OuterOct_A", root.transform, Vector3.zero, new Vector3(0.115f, 0.115f, 0.018f), brassMat, keepCollider: false);
            GameObject octB = CreateBox("Astrolabe_OuterOct_B", root.transform, Vector3.zero, new Vector3(0.115f, 0.115f, 0.018f), brassMat, keepCollider: false);
            octB.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateBox("Astrolabe_ObsidianInlay_A", root.transform, new Vector3(0f, 0f, 0.004f), new Vector3(0.086f, 0.086f, 0.020f), obsidianMat, keepCollider: false);
            GameObject inB = CreateBox("Astrolabe_ObsidianInlay_B", root.transform, new Vector3(0f, 0f, 0.004f), new Vector3(0.086f, 0.086f, 0.020f), obsidianMat, keepCollider: false);
            inB.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            GameObject spinRing = new GameObject("Astrolabe_SpinningCelestialRing");
            spinRing.transform.SetParent(root.transform, false);
            spinRing.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            CreateBox("Astrolabe_CrossBar_H", spinRing.transform, Vector3.zero, new Vector3(0.105f, 0.014f, 0.012f), brassMat, keepCollider: false);
            CreateBox("Astrolabe_CrossBar_V", spinRing.transform, Vector3.zero, new Vector3(0.014f, 0.105f, 0.012f), brassMat, keepCollider: false);
            GameObject rDia = CreateBox("Astrolabe_RuneRing", spinRing.transform, Vector3.zero, new Vector3(0.058f, 0.058f, 0.014f), runeGlowMat, keepCollider: false);
            rDia.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateBox("Astrolabe_RiftCoreCrystal", spinRing.transform, new Vector3(0f, 0f, 0.008f), new Vector3(0.034f, 0.034f, 0.022f), coreGlowMat, keepCollider: false);

            spinTransform = spinRing.transform;
            return root;
        }

        private static void BuildEvidenceCorkboard(Transform parent, Vector3 pos, Quaternion rot,
            Material frameMat, Material corkMat, Material paperMat, Material astrolabeCoreMat, Material stickyMat, Material redThreadMat)
        {
            GameObject r = new GameObject("Artifact_EvidenceCorkboard");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Corkboard_WoodFrame", r.transform, Vector3.zero, new Vector3(0.04f, 0.82f, 1.12f), frameMat, keepCollider: false);
            GameObject face = CreateBox("Corkboard_CorkSurface", r.transform, new Vector3(-0.014f, 0f, 0f), new Vector3(0.024f, 0.74f, 1.04f), corkMat, keepCollider: false);
            BoxCollider col = face.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(face, "artifact:muro-evidencias-pdn",
                "Muro de Evidencias Conspirativas PDN-00 (Pista Bóveda Ω: 654321)",
                "Conspiracy Evidence Board PDN-00 (Vault Ω Clue: 654321)",
                "Fotografías de Av. Benito Juárez conectadas con hilos rojos hacia un boceto del Espejo Dimensional. Nota amarilla: 'BÓVEDA NIVEL Ω EN ALA NOROESTE • CÓDIGO INVERSO: 6-5-4-3-2-1'.",
                "Photos of Av. Benito Juárez linked with red yarn to a sketch of the Dimensional Mirror. Yellow note: 'LEVEL-Ω VAULT IN NW WING • REVERSE CODE: 6-5-4-3-2-1'.",
                new Color(1.0f, 0.88f, 0.28f));
            CreateBox("Evidence_Photo_1", r.transform, new Vector3(-0.028f, 0.16f, -0.30f), new Vector3(0.01f, 0.22f, 0.26f), paperMat, keepCollider: false);
            CreateBox("Evidence_Diagram", r.transform, new Vector3(-0.028f, 0.12f, 0.26f), new Vector3(0.01f, 0.26f, 0.26f), paperMat, keepCollider: false);
            CreateBox("Evidence_DiagramCore", r.transform, new Vector3(-0.034f, 0.12f, 0.26f), new Vector3(0.01f, 0.14f, 0.14f), astrolabeCoreMat, keepCollider: false);
            CreateBox("Evidence_StickyNote_654321", r.transform, new Vector3(-0.030f, -0.18f, -0.06f), new Vector3(0.012f, 0.18f, 0.22f), stickyMat, keepCollider: false);
            GameObject t1 = CreateBox("Evidence_RedThread_1", r.transform, new Vector3(-0.035f, 0.14f, -0.02f), new Vector3(0.008f, 0.016f, 0.56f), redThreadMat, keepCollider: false);
            t1.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            GameObject t2 = CreateBox("Evidence_RedThread_2", r.transform, new Vector3(-0.035f, -0.03f, 0.10f), new Vector3(0.008f, 0.016f, 0.44f), redThreadMat, keepCollider: false);
            t2.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
        }

        // =====================================================================
        // 12 ORIGINAL ARCHIVAL & ANOMALOUS ARTIFACTS (IP-01 REPLACEMENTS)
        // =====================================================================
        private static void BuildShiftClock0307(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material brassMat, Material faceMat, Material darkMat)
        {
            GameObject r = new GameObject("Artifact_ShiftClock0307");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            // Rectangular industrial shift timekeeper housing with recessed punch-card slot
            GameObject housing = CreateBox("ShiftClock_Housing", r.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.20f, 0.24f, 0.11f), steelMat, keepCollider: false);
            BoxCollider col = housing.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 1.4f, 2.2f);
            col.isTrigger = true;
            AttachDesc(housing, ProgressionIds.ArtifactShiftClock0307,
                "Reloj de Turno 03:07",
                "Shift Clock 03:07",
                "Reloj checador industrial detenido a las 03:07. Sin importar la hora en Ciudad Juárez ni el turno del corredor fronterizo, sus manecillas jamás avanzan.",
                "Industrial shift timekeeper frozen at 03:07. Regardless of the local hour in Ciudad Juárez or the border corridor shift, its hands never move.",
                new Color(1.0f, 0.80f, 0.34f));

            CreateBox("ShiftClock_BaseFoot", r.transform, new Vector3(0f, -0.065f, 0f), new Vector3(0.23f, 0.024f, 0.14f), darkMat, keepCollider: false);
            CreateBox("ShiftClock_BrassBezel", r.transform, new Vector3(0f, 0.095f, 0.056f), new Vector3(0.145f, 0.125f, 0.012f), brassMat, keepCollider: false);
            CreateBox("ShiftClock_DialFace", r.transform, new Vector3(0f, 0.095f, 0.064f), new Vector3(0.120f, 0.102f, 0.006f), faceMat, keepCollider: false);
            CreateBox("ShiftClock_HourHand", r.transform, new Vector3(0.018f, 0.095f, 0.070f), new Vector3(0.036f, 0.008f, 0.005f), darkMat, keepCollider: false);
            GameObject minHand = CreateBox("ShiftClock_MinuteHand", r.transform, new Vector3(0.010f, 0.114f, 0.070f), new Vector3(0.007f, 0.044f, 0.005f), darkMat, keepCollider: false);
            minHand.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            CreateBox("ShiftClock_CardSlot", r.transform, new Vector3(0f, -0.015f, 0.056f), new Vector3(0.110f, 0.016f, 0.014f), darkMat, keepCollider: false);
        }

        private static void BuildSealedNotebook(Transform parent, Vector3 pos, Quaternion rot,
            Material leatherMat, Material pageMat, Material brassMat, Material strapMat)
        {
            GameObject notebook = CreateBox("Artifact_SealedNotebook", parent, pos, new Vector3(0.22f, 0.048f, 0.30f), leatherMat, keepCollider: false);
            notebook.transform.rotation = rot;
            BoxCollider col = notebook.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 3.6f, 1.4f);
            col.isTrigger = true;
            AttachDesc(notebook, ProgressionIds.ArtifactSealedNotebook,
                "Cuaderno de Páginas Selladas",
                "Notebook of Sealed Pages",
                "Cuaderno de campo manchado por la humedad con diagramas técnicos doblados y varias hojas pegadas entre sí. Sus márgenes registran lecturas de un plano que no coincide con la superficie.",
                "Water-damaged field notebook filled with folded technical diagrams and pages sealed together. Its margins log survey measurements that do not match the surface grid.",
                new Color(0.82f, 0.84f, 0.90f));

            CreateBox("Notebook_PageBlock", notebook.transform, new Vector3(0.05f, 0f, 0f), new Vector3(0.92f, 0.72f, 0.94f), pageMat, keepCollider: false);
            GameObject foldedSheet = CreateBox("Notebook_FoldedDiagramSheet", notebook.transform, new Vector3(0.22f, 0.12f, 0.10f), new Vector3(0.76f, 0.08f, 0.60f), pageMat, keepCollider: false);
            foldedSheet.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);
            CreateBox("Notebook_TieBand", notebook.transform, new Vector3(0.08f, 0f, 0f), new Vector3(0.14f, 1.08f, 1.04f), strapMat, keepCollider: false);
            CreateBox("Notebook_CornerClasp", notebook.transform, new Vector3(0.42f, 0.44f, -0.40f), new Vector3(0.18f, 0.22f, 0.16f), brassMat, keepCollider: false);
        }

        private static void BuildServiceIntercom(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material aluminumMat, Material obsidianMat, Material brassMat, Material ledMat)
        {
            GameObject r = new GameObject("Artifact_ServiceIntercom");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            GameObject housing = CreateBox("Intercom_SteelHousing", r.transform, Vector3.zero, new Vector3(0.09f, 0.28f, 0.20f), steelMat, keepCollider: false);
            BoxCollider col = housing.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 1.4f, 1.4f);
            col.isTrigger = true;
            AttachDesc(housing, ProgressionIds.ArtifactServiceIntercom,
                "Intercomunicador de Servicio sin Línea",
                "Unwired Service Intercom",
                "Caja de intercomunicación municipal fijada a la columna con el ducto eléctrico cortado. Aunque no existe tendido conectado, el altavoz emite breves chasquidos de portadora en los pasillos vacíos.",
                "Municipal-style wall intercom box bolted to the column with its conduit sheared off. Though no line is connected, the speaker emits brief carrier clicks across the empty halls.",
                new Color(0.75f, 0.80f, 0.90f));

            CreateBox("Intercom_FacePlate", r.transform, new Vector3(-0.048f, 0f, 0f), new Vector3(0.012f, 0.24f, 0.17f), aluminumMat, keepCollider: false);
            CreateBox("Intercom_GrilleSlat_1", r.transform, new Vector3(-0.056f, 0.075f, 0f), new Vector3(0.008f, 0.014f, 0.12f), obsidianMat, keepCollider: false);
            CreateBox("Intercom_GrilleSlat_2", r.transform, new Vector3(-0.056f, 0.048f, 0f), new Vector3(0.008f, 0.014f, 0.12f), obsidianMat, keepCollider: false);
            CreateBox("Intercom_GrilleSlat_3", r.transform, new Vector3(-0.056f, 0.021f, 0f), new Vector3(0.008f, 0.014f, 0.12f), obsidianMat, keepCollider: false);
            CreateBox("Intercom_CallButton", r.transform, new Vector3(-0.058f, -0.055f, 0.028f), new Vector3(0.016f, 0.032f, 0.032f), brassMat, keepCollider: false);
            CreateBox("Intercom_StatusLamp", r.transform, new Vector3(-0.058f, -0.055f, -0.042f), new Vector3(0.012f, 0.020f, 0.020f), ledMat, keepCollider: false);
            CreateBox("Intercom_CutConduitStub", r.transform, new Vector3(-0.010f, -0.20f, 0f), new Vector3(0.026f, 0.14f, 0.026f), aluminumMat, keepCollider: false);
        }

        private static void BuildSignalMonitor(Transform parent, Vector3 pos, Quaternion rot,
            Material gunmetalMat, Material crtStaticMat, Material aluminumMat, Material obsidianMat)
        {
            GameObject r = new GameObject("Artifact_SignalMonitor_Root");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            GameObject cab = CreateBox("SignalMonitor_Chassis", r.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.50f, 0.42f, 0.36f), gunmetalMat, keepCollider: false);
            BoxCollider col = cab.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(cab, ProgressionIds.ArtifactSignalMonitor,
                "Monitor de Señal sin Origen",
                "Unsourced Signal Monitor",
                "Monitor CRT industrial encendido sin cable de alimentación en un nicho del ala Oeste. Sus barras horizontales de interferencia barren la pantalla en un ciclo irregular sin fuente identificable.",
                "Industrial CRT test monitor glowing without a power cord in a West wing niche. Irregular horizontal scan bars drift across its screen with no traceable broadcast source.",
                new Color(0.55f, 0.84f, 1.0f));

            // Recessed bezel & glowing interference screen with irregular horizontal scan bars (no circular ring, no VHS)
            CreateBox("SignalMonitor_Bezel", r.transform, new Vector3(-0.04f, 0.27f, 0.182f), new Vector3(0.36f, 0.29f, 0.014f), obsidianMat, keepCollider: false);
            CreateBox("SignalMonitor_Screen", r.transform, new Vector3(-0.04f, 0.27f, 0.190f), new Vector3(0.32f, 0.25f, 0.008f), crtStaticMat, keepCollider: false);
            CreateBox("SignalMonitor_ScanBar_A", r.transform, new Vector3(-0.04f, 0.335f, 0.196f), new Vector3(0.31f, 0.018f, 0.004f), obsidianMat, keepCollider: false);
            CreateBox("SignalMonitor_ScanBar_B", r.transform, new Vector3(-0.08f, 0.255f, 0.196f), new Vector3(0.22f, 0.012f, 0.004f), aluminumMat, keepCollider: false);
            CreateBox("SignalMonitor_ScanBar_C", r.transform, new Vector3(0.01f, 0.195f, 0.196f), new Vector3(0.24f, 0.016f, 0.004f), obsidianMat, keepCollider: false);
            CreateBox("SignalMonitor_ControlStrip", r.transform, new Vector3(0.185f, 0.26f, 0.185f), new Vector3(0.055f, 0.28f, 0.012f), aluminumMat, keepCollider: false);
            CreateBox("SignalMonitor_DialTop", r.transform, new Vector3(0.185f, 0.33f, 0.198f), new Vector3(0.028f, 0.028f, 0.018f), obsidianMat, keepCollider: false);
            CreateBox("SignalMonitor_DialBottom", r.transform, new Vector3(0.185f, 0.26f, 0.198f), new Vector3(0.028f, 0.028f, 0.018f), obsidianMat, keepCollider: false);

            GameObject glow = new GameObject("SignalMonitor_Glow");
            glow.transform.SetParent(r.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.30f, 0.45f);
            Light l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.45f, 0.78f, 1.0f);
            l.intensity = 0.85f;
            l.range = 4.6f;
            l.shadows = LightShadows.None;
        }

        private static void BuildSpecimenVitrine(Transform parent, Vector3 pos, Quaternion rot,
            Material woodMat, Material glassMat, Material ceramicMat, Material obsidianMat, Material brassMat)
        {
            GameObject r = new GameObject("Artifact_SpecimenVitrine");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            CreateBox("Vitrine_WoodBase", r.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.46f, 0.05f, 0.34f), woodMat, keepCollider: false);
            CreateBox("Vitrine_WoodTop", r.transform, new Vector3(0f, 0.70f, 0f), new Vector3(0.46f, 0.045f, 0.34f), woodMat, keepCollider: false);
            CreateBox("Vitrine_BackPanel", r.transform, new Vector3(0f, 0.36f, -0.15f), new Vector3(0.44f, 0.64f, 0.028f), woodMat, keepCollider: false);
            GameObject glass = CreateBox("Vitrine_GlassEnclosure", r.transform, new Vector3(0f, 0.36f, 0.01f), new Vector3(0.43f, 0.64f, 0.31f), glassMat, keepCollider: false);
            BoxCollider col = glass.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(glass, ProgressionIds.ArtifactSpecimenVitrine,
                "Vitrina de Muestra: Figura sin Rasgos",
                "Specimen Vitrine: Featureless Figure",
                "Vitrina sellada de archivo que resguarda una forma cerámica lisa y sin rostro ni inscripción. Su placa de inventario carece de fecha de ingreso o procedencia.",
                "Sealed archival vitrine housing a smooth, featureless ceramic form with no markings. Its brass inventory tag carries neither an intake date nor a point of origin.",
                new Color(0.84f, 0.78f, 0.66f));

            // Small unlabelled brass catalog plaque on base front + abstract non-human tapered ceramic monolith
            CreateBox("Vitrine_CatalogPlaque", r.transform, new Vector3(0f, 0.028f, 0.175f), new Vector3(0.14f, 0.032f, 0.010f), brassMat, keepCollider: false);
            CreateBox("Specimen_Plinth", r.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.22f, 0.08f, 0.18f), obsidianMat, keepCollider: false);
            CreateBox("Specimen_LowerTier", r.transform, new Vector3(0f, 0.21f, 0f), new Vector3(0.15f, 0.16f, 0.12f), ceramicMat, keepCollider: false);
            GameObject midTier = CreateBox("Specimen_MidPrism", r.transform, new Vector3(0f, 0.36f, 0f), new Vector3(0.11f, 0.18f, 0.11f), ceramicMat, keepCollider: false);
            midTier.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            CreateBox("Specimen_BrassCollar", r.transform, new Vector3(0f, 0.455f, 0f), new Vector3(0.12f, 0.018f, 0.12f), brassMat, keepCollider: false);
            CreateBox("Specimen_UpperMonolith", r.transform, new Vector3(0f, 0.535f, 0f), new Vector3(0.085f, 0.14f, 0.085f), ceramicMat, keepCollider: false);
        }

        private static void BuildArticulatedWoodAssembly(Transform parent, Vector3 pos, Quaternion rot,
            Material walnutMat, Material mahoganyMat, Material brassMat)
        {
            GameObject r = new GameObject("Artifact_ArticulatedWoodAssembly");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            // Abstract jointed wooden workshop calibration linkage & hanging brass plumb weight
            GameObject baseBlock = CreateBox("WoodAssembly_BaseBlock", r.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.26f, 0.07f, 0.18f), mahoganyMat, keepCollider: false);
            BoxCollider col = baseBlock.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 3.2f, 0f);
            col.size = new Vector3(1.5f, 7.2f, 1.8f);
            col.isTrigger = true;
            AttachDesc(baseBlock, ProgressionIds.ArtifactArticulatedWoodAssembly,
                "Ensamble de Madera Articulado",
                "Articulated Wooden Assembly",
                "Estructura articulada de madera y pernos de latón abandonada sobre una caja de archivo. Sus brazos móviles conservan marcas de calibración a lápiz, aunque no corresponde a ningún instrumento convencional.",
                "Hand-built jointed wooden frame with brass pivot pins left atop an archive crate. Its hinged arms bear pencil calibration marks, yet it matches no standard drafting or survey tool.",
                new Color(0.86f, 0.68f, 0.42f));

            GameObject strutL = CreateBox("WoodAssembly_StrutLeft", r.transform, new Vector3(-0.07f, 0.21f, 0f), new Vector3(0.032f, 0.30f, 0.036f), walnutMat, keepCollider: false);
            strutL.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            GameObject strutR = CreateBox("WoodAssembly_StrutRight", r.transform, new Vector3(0.07f, 0.21f, 0f), new Vector3(0.032f, 0.30f, 0.036f), walnutMat, keepCollider: false);
            strutR.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            CreateBox("WoodAssembly_CrossBeam", r.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.24f, 0.030f, 0.042f), mahoganyMat, keepCollider: false);
            GameObject upperArm = CreateBox("WoodAssembly_CantileverArm", r.transform, new Vector3(0.05f, 0.43f, 0.01f), new Vector3(0.22f, 0.026f, 0.032f), walnutMat, keepCollider: false);
            upperArm.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            CreateBox("WoodAssembly_PivotPinL", r.transform, new Vector3(-0.045f, 0.35f, 0.024f), new Vector3(0.024f, 0.024f, 0.022f), brassMat, keepCollider: false);
            CreateBox("WoodAssembly_PivotPinR", r.transform, new Vector3(0.045f, 0.35f, 0.024f), new Vector3(0.024f, 0.024f, 0.022f), brassMat, keepCollider: false);
            CreateBox("WoodAssembly_PlumbWeight", r.transform, new Vector3(0.0f, 0.19f, 0.01f), new Vector3(0.038f, 0.075f, 0.038f), brassMat, keepCollider: false);
        }

        private static void BuildArchiveCase06(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material beigeMat, Material aluminumMat, Material obsidianMat)
        {
            GameObject box = CreateBox("Artifact_ArchiveCase06", parent, pos, new Vector3(0.30f, 0.34f, 0.22f), steelMat, keepCollider: false);
            box.transform.rotation = rot;
            BoxCollider col = box.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.3f, 1.4f);
            col.isTrigger = true;
            AttachDesc(box, ProgressionIds.ArtifactArchiveCase06,
                "Caja de Archivo 06",
                "Archive Case 06",
                "Contenedor metálico de archivo sin logotipo comercial, asegurado con cierre de acero sobre las cajas húmedas del pasillo Norte. Su etiqueta numerada '06' está en blanco en el reverso.",
                "Unbranded archival storage case secured with a steel latch atop damp corridor cartons in the North hall. Its numbered index tag '06' bears no sender or contents manifest.",
                new Color(0.76f, 0.80f, 0.84f));

            CreateBox("ArchiveCase_LidSeam", box.transform, new Vector3(0f, 0.34f, 0f), new Vector3(1.03f, 0.06f, 1.03f), obsidianMat, keepCollider: false);
            CreateBox("ArchiveCase_Latch", box.transform, new Vector3(0f, 0.24f, 0.52f), new Vector3(0.18f, 0.24f, 0.08f), aluminumMat, keepCollider: false);
            CreateBox("ArchiveCase_LabelPlate", box.transform, new Vector3(0f, -0.08f, 0.52f), new Vector3(0.46f, 0.26f, 0.04f), beigeMat, keepCollider: false);
            CreateBox("ArchiveCase_HandleTop", box.transform, new Vector3(0f, 0.54f, 0f), new Vector3(0.42f, 0.08f, 0.14f), aluminumMat, keepCollider: false);
        }

        private static void BuildSealedCustodyCase(Transform parent, Vector3 pos, Quaternion rot,
            Material woodMat, Material gunmetalMat, Material steelMat, Material aluminumMat)
        {
            GameObject r = new GameObject("Artifact_SealedCustodyCase");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            // Low steel transit skid pad + reinforced archival mahogany custody case with clamp bars & metal seal tag
            CreateBox("CustodyCase_SkidPad", r.transform, new Vector3(0f, 0.014f, 0f), new Vector3(0.40f, 0.028f, 0.30f), gunmetalMat, keepCollider: false);
            GameObject chest = CreateBox("CustodyCase_Body", r.transform, new Vector3(0f, 0.21f, 0f), new Vector3(0.34f, 0.36f, 0.24f), woodMat, keepCollider: false);
            BoxCollider col = chest.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(chest, ProgressionIds.ArtifactSealedCustodyCase,
                "Estuche de Custodia Sellado",
                "Sealed Custody Case",
                "Estuche de madera oscura reforzado con barras de sujeción de acero y precinto metálico de custodia municipal. Al acercarse se percibe un zumbido sordo amortiguado en su interior.",
                "Dark wood custody chest clamped shut with industrial steel retention bars and a metal transit seal. Up close, a faint muffled resonance vibrates against the casing.",
                new Color(0.92f, 0.64f, 0.28f));

            CreateBox("CustodyCase_ClampBarL", chest.transform, new Vector3(-0.34f, 0f, 0f), new Vector3(0.11f, 1.05f, 1.06f), steelMat, keepCollider: false);
            CreateBox("CustodyCase_ClampBarR", chest.transform, new Vector3(0.34f, 0f, 0f), new Vector3(0.11f, 1.05f, 1.06f), steelMat, keepCollider: false);
            CreateBox("CustodyCase_LockBridge", chest.transform, new Vector3(0f, 0.04f, 0.52f), new Vector3(0.84f, 0.14f, 0.08f), gunmetalMat, keepCollider: false);
            CreateBox("CustodyCase_TransitTag", chest.transform, new Vector3(0f, 0.04f, 0.58f), new Vector3(0.24f, 0.16f, 0.05f), aluminumMat, keepCollider: false);
        }

        private static void BuildAnomalousGeometryAtlas(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material leatherMat, Material brassMat, Material aluminumMat, Material accentMat)
        {
            GameObject r = new GameObject("Artifact_AnomalousGeometryAtlas");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            CreateBox("Atlas_DraftingStand", r.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.30f, 0.14f, 0.22f), steelMat, keepCollider: false);
            GameObject folio = CreateBox("Atlas_TechnicalFolio", r.transform, new Vector3(0f, 0.20f, 0.02f), new Vector3(0.30f, 0.34f, 0.055f), leatherMat, keepCollider: false);
            folio.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            BoxCollider col = folio.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(folio, ProgressionIds.ArtifactAnomalousGeometryAtlas,
                "Atlas de Geometría Anómala",
                "Atlas of Anomalous Geometry",
                "Folio técnico de gran formato colocado sobre un atril de acero junto a la bóveda Nivel Ω. Contiene levantamientos topográficos de pasillos cerrados en bucle y ángulos imposibles.",
                "Oversized technical archival folio resting on a steel drafting stand beside the Level-Ω vault. Its bound plates chart closed-loop corridors and non-Euclidean interior angles.",
                new Color(0.95f, 0.68f, 0.24f));

            CreateBox("Atlas_SpineRail", folio.transform, new Vector3(-0.46f, 0f, 0f), new Vector3(0.10f, 1.02f, 1.15f), aluminumMat, keepCollider: false);
            CreateBox("Atlas_CornerTL", folio.transform, new Vector3(-0.42f, 0.44f, 0.52f), new Vector3(0.14f, 0.12f, 0.14f), brassMat, keepCollider: false);
            CreateBox("Atlas_CornerTR", folio.transform, new Vector3(0.42f, 0.44f, 0.52f), new Vector3(0.14f, 0.12f, 0.14f), brassMat, keepCollider: false);
            CreateBox("Atlas_CornerBL", folio.transform, new Vector3(-0.42f, -0.44f, 0.52f), new Vector3(0.14f, 0.12f, 0.14f), brassMat, keepCollider: false);
            CreateBox("Atlas_CornerBR", folio.transform, new Vector3(0.42f, -0.44f, 0.52f), new Vector3(0.14f, 0.12f, 0.14f), brassMat, keepCollider: false);
            CreateBox("Atlas_DiagramPlate", folio.transform, new Vector3(0.04f, 0.04f, 0.54f), new Vector3(0.56f, 0.44f, 0.08f), steelMat, keepCollider: false);
            CreateBox("Atlas_GridAxisH", folio.transform, new Vector3(0.04f, 0.04f, 0.60f), new Vector3(0.46f, 0.04f, 0.06f), accentMat, keepCollider: false);
            CreateBox("Atlas_GridAxisV", folio.transform, new Vector3(0.04f, 0.04f, 0.60f), new Vector3(0.04f, 0.36f, 0.06f), accentMat, keepCollider: false);
        }

        private static void BuildFracturedInsulator(Transform parent, Vector3 pos, Quaternion rot,
            Material porcelainMat, Material obsidianMat, Material brassMat, Material steelMat)
        {
            GameObject r = new GameObject("Artifact_FracturedInsulator");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            // High-voltage ribbed ceramic electrical insulator with dark fracture seam and brass top cap
            CreateBox("Insulator_BaseCollar", r.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.18f, 0.04f, 0.18f), steelMat, keepCollider: false);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Insulator_CoreColumn";
            core.transform.SetParent(r.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.19f, 0f);
            core.transform.localScale = new Vector3(0.10f, 0.16f, 0.10f);
            SetMat(core, porcelainMat);
            UnityEngine.Object.DestroyImmediate(core.GetComponent<Collider>());

            BoxCollider col = core.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 2.2f, 2.2f);
            col.isTrigger = true;
            AttachDesc(core, ProgressionIds.ArtifactFracturedInsulator,
                "Aislador Cerámico Fracturado",
                "Fractured Ceramic Insulator",
                "Aislador de porcelana de alta tensión con tres campanas acanaladas y una fractura vítrea en el núcleo. Conserva restos de hollín eléctrico pese a hallarse lejos de cualquier subestación.",
                "Ribbed high-voltage porcelain insulator split by a glassy fracture seam across its core. It retains traces of arc scorch despite sitting far from any utility line or substation.",
                new Color(0.92f, 0.88f, 0.78f));

            float[] shedYs = new float[] { 0.09f, 0.185f, 0.28f };
            float[] shedRadii = new float[] { 0.20f, 0.18f, 0.155f };
            for (int i = 0; i < shedYs.Length; i++)
            {
                GameObject shed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shed.name = $"Insulator_Shed_{i}";
                shed.transform.SetParent(r.transform, false);
                shed.transform.localPosition = new Vector3(0f, shedYs[i], 0f);
                shed.transform.localScale = new Vector3(shedRadii[i], 0.022f, shedRadii[i]);
                SetMat(shed, porcelainMat);
                UnityEngine.Object.DestroyImmediate(shed.GetComponent<Collider>());
            }

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Insulator_BrassCap";
            cap.transform.SetParent(r.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.365f, 0f);
            cap.transform.localScale = new Vector3(0.075f, 0.032f, 0.075f);
            SetMat(cap, brassMat);
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());

            GameObject fracture = CreateBox("Insulator_FractureSeam", r.transform, new Vector3(0.015f, 0.19f, 0.088f), new Vector3(0.020f, 0.19f, 0.024f), obsidianMat, keepCollider: false);
            fracture.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        }

        private static void BuildFoldedTopologyCore(Transform parent, Vector3 pos, Quaternion rot,
            Material gunmetalMat, Material ceramicMat, Material aluminumMat, Material brassMat)
        {
            GameObject r = new GameObject("Artifact_FoldedTopologyCore");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;

            // Low gunmetal steel calibration cradle + three offset asymmetric ceramic/aluminum stepped plates
            CreateBox("TopologyCore_SteelCradle", r.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.24f, 0.05f, 0.18f), gunmetalMat, keepCollider: false);
            GameObject plateLow = CreateBox("TopologyCore_LowerPlate", r.transform, new Vector3(0f, 0.085f, 0f), new Vector3(0.18f, 0.042f, 0.14f), aluminumMat, keepCollider: false);
            plateLow.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            BoxCollider col = plateLow.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.8f, 0f);
            col.size = new Vector3(1.5f, 5.2f, 1.7f);
            col.isTrigger = true;
            AttachDesc(plateLow, ProgressionIds.ArtifactFoldedTopologyCore,
                "Núcleo de Topología Plegada",
                "Folded Topology Core",
                "Ensamble mecánico asimétrico de placas cerámicas y aluminio montado sobre una cuna de acero. Sus secciones escalonadas se desfasan entre sí como un modelo a escala de un espacio plegado.",
                "Asymmetric mechanical assembly of stepped ceramic and aluminum plates seated in a steel cradle. Its offset tiers shift out of phase like a scale model of folded architectural space.",
                new Color(0.74f, 0.86f, 0.96f));

            GameObject plateMid = CreateBox("TopologyCore_MidCeramicTier", r.transform, new Vector3(0.018f, 0.142f, -0.012f), new Vector3(0.15f, 0.052f, 0.12f), ceramicMat, keepCollider: false);
            plateMid.transform.localRotation = Quaternion.Euler(8f, -24f, 6f);
            GameObject plateTop = CreateBox("TopologyCore_UpperAluminumTier", r.transform, new Vector3(-0.014f, 0.205f, 0.010f), new Vector3(0.12f, 0.038f, 0.10f), aluminumMat, keepCollider: false);
            plateTop.transform.localRotation = Quaternion.Euler(-10f, 34f, -8f);
            CreateBox("TopologyCore_BrassAxle", r.transform, new Vector3(0f, 0.145f, 0f), new Vector3(0.024f, 0.22f, 0.024f), brassMat, keepCollider: false);
        }

        private static void BuildCoordinatePlottingPlate(Transform parent, Vector3 pos, Quaternion rot,
            Material slateMat, Material brassMat, Material aluminumMat)
        {
            GameObject plate = CreateBox("Artifact_CoordinatePlottingPlate", parent, pos, new Vector3(0.48f, 0.026f, 0.34f), slateMat, keepCollider: false);
            plate.transform.rotation = rot;
            BoxCollider col = plate.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 4.5f, 1.2f);
            col.isTrigger = true;
            AttachDesc(plate, ProgressionIds.ArtifactCoordinatePlottingPlate,
                "Lámina de Trazado de Coordenadas",
                "Coordinate Plotting Plate",
                "Placa cartográfica de pizarra y latón con retícula ortogonal y regla deslizable. Sus coordenadas trazan intersecciones bajo la Avenida Benito Juárez que no figuran en los planos catastrales.",
                "Slate-and-brass cartographic plotting plate fitted with an orthogonal grid and sliding vernier rule. Its etched intersections map chambers beneath Avenida Benito Juárez absent from civic surveys.",
                new Color(0.86f, 0.72f, 0.46f));

            // Orthogonal brass border frame, straight survey grid lines & sliding vernier arm (no arcs, no planchette)
            CreateBox("PlotPlate_RailNorth", plate.transform, new Vector3(0f, 0.56f, 0.45f), new Vector3(0.96f, 0.18f, 0.06f), brassMat, keepCollider: false);
            CreateBox("PlotPlate_RailSouth", plate.transform, new Vector3(0f, 0.56f, -0.45f), new Vector3(0.96f, 0.18f, 0.06f), brassMat, keepCollider: false);
            CreateBox("PlotPlate_GridX1", plate.transform, new Vector3(0f, 0.54f, 0.15f), new Vector3(0.86f, 0.08f, 0.025f), aluminumMat, keepCollider: false);
            CreateBox("PlotPlate_GridX2", plate.transform, new Vector3(0f, 0.54f, -0.15f), new Vector3(0.86f, 0.08f, 0.025f), aluminumMat, keepCollider: false);
            CreateBox("PlotPlate_GridZ1", plate.transform, new Vector3(-0.22f, 0.54f, 0f), new Vector3(0.025f, 0.08f, 0.80f), aluminumMat, keepCollider: false);
            CreateBox("PlotPlate_GridZ2", plate.transform, new Vector3(0.22f, 0.54f, 0f), new Vector3(0.025f, 0.08f, 0.80f), aluminumMat, keepCollider: false);
            CreateBox("PlotPlate_VernierRule", plate.transform, new Vector3(0.10f, 0.72f, 0f), new Vector3(0.08f, 0.24f, 0.96f), brassMat, keepCollider: false);
        }

        private static void BuildPaquimeMask(Transform parent, Vector3 pos, Quaternion rot, Material obsidianMat, Material turquoiseMat, Material brassMat)
        {
            GameObject mask = CreateBox("Artifact_PaquimeObsidianMask", parent, pos, new Vector3(0.07f, 0.22f, 0.17f), obsidianMat, keepCollider: false);
            mask.transform.rotation = rot;
            BoxCollider col = mask.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 1.4f, 1.4f);
            col.isTrigger = true;
            AttachDesc(mask, "artifact:mascara-paquime",
                "Máscara Ceremonial de Obsidiana y Turquesa (EXP-12 • Paquimé)",
                "Ceremonial Obsidian & Turquoise Mask (EXP-12 • Paquimé)",
                "Máscara prehispánica tallada en obsidiana con grecas de Paquimé y ojos de turquesa que brillan hacia el Portal Dimensional.",
                "Pre-Hispanic obsidian mask with Paquimé step-frets and turquoise eyes glowing toward the Dimensional Portal.",
                new Color(0.20f, 0.94f, 0.80f));
            CreateBox("Paquime_EyeL", mask.transform, new Vector3(-0.54f, 0.16f, -0.24f), new Vector3(0.16f, 0.14f, 0.22f), turquoiseMat, keepCollider: false);
            CreateBox("Paquime_EyeR", mask.transform, new Vector3(-0.54f, 0.16f, 0.24f), new Vector3(0.16f, 0.14f, 0.22f), turquoiseMat, keepCollider: false);
            CreateBox("Paquime_Crown", mask.transform, new Vector3(-0.52f, 0.38f, 0f), new Vector3(0.12f, 0.12f, 0.72f), brassMat, keepCollider: false);
        }

        private static void BuildAnomalousCompass(Transform parent, Vector3 pos, Quaternion rot, Material brassMat, Material glowMat)
        {
            GameObject comp = CreateBox("Artifact_AnomalousCompass", parent, pos, new Vector3(0.11f, 0.04f, 0.11f), brassMat, keepCollider: false);
            comp.transform.rotation = rot;
            BoxCollider col = comp.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 4.0f, 2.2f);
            col.isTrigger = true;
            AttachDesc(comp, "artifact:brujula-astral",
                "Brújula Astral Anómala (EXP-13 — Apunta hacia el Portal)",
                "Anomalous Astral Compass (EXP-13 — Points to the Portal)",
                "Ignora el norte magnético y su aguja cian apunta invariablemente hacia el Suroeste, directo a la Cámara del Umbral Dimensional en el rincón más alejado del laberinto.",
                "Ignores magnetic north; its glowing cyan needle points unerringly Southwest toward the Dimensional Threshold Chamber in the farthest corner of the maze.",
                new Color(0.38f, 0.92f, 1.0f));
            CreateBox("Compass_Needle", comp.transform, new Vector3(0f, 0.56f, 0f), new Vector3(0.18f, 0.18f, 0.80f), glowMat, keepCollider: false);
        }

        // =====================================================================
        // PROCEDURAL LEVEL 0 & REALITY-BRIDGE SCREEN TEXTURES
        // =====================================================================
        private static string EnsureDiarioJuarezScreenTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 352;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            Color paper = new Color(0.95f, 0.94f, 0.91f);
            Color navy = new Color(0.06f, 0.16f, 0.34f);
            Color crimson = new Color(0.76f, 0.10f, 0.14f);
            Color ink = new Color(0.15f, 0.17f, 0.20f);
            Color gray = new Color(0.68f, 0.70f, 0.72f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = paper;
                    if (y > 296) c = navy; // Top newspaper masthead bar ("EL DIARIO DE JUÁREZ")
                    else if (y >= 286 && y <= 296) c = crimson; // Breaking edition accent stripe
                    else if (y > 312 && y < 336 && x > 22 && x < 234 && ((x / 6) % 2 == 0)) c = Color.white;
                    // Bold headline block
                    else if (y > 246 && y < 274 && x > 14 && x < 242 && ((y % 12) > 3)) c = ink;
                    // Hero front-page photo frame (Bridge / Juárez silhouette)
                    else if (y > 144 && y < 238 && x > 14 && x < 168)
                    {
                        float grad = (y - 144f) / 94f;
                        c = Color.Lerp(new Color(0.24f, 0.32f, 0.42f), new Color(0.68f, 0.56f, 0.42f), grad);
                    }
                    // Right-column news lines
                    else if (y > 144 && y < 238 && x > 178 && x < 242 && ((y % 7) > 2)) c = ink;
                    // Lower 3-column article body
                    else if (y > 18 && y < 130 && x > 14 && x < 242)
                    {
                        bool gutter = (x > 84 && x < 92) || (x > 164 && x < 172);
                        if (!gutter && (y % 6) > 2) c = (y > 110) ? ink : gray;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            // Masthead white lettering blocks inside navy banner
            for (int y = 312; y < 336; y++)
                for (int x = 24; x < 232; x++)
                    if (((x - 24) % 14) < 10 && ((y - 312) < 6 || (y - 312) > 17 || ((x - 24) % 14) < 3))
                        tex.SetPixel(x, y, Color.white);

            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureLaptopMysteryScreenTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            Color bg = new Color(0.07f, 0.08f, 0.11f);
            Color topBar = new Color(0.13f, 0.15f, 0.20f);
            Color ytRed = new Color(0.88f, 0.12f, 0.14f);
            Color cyan = new Color(0.24f, 0.80f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = bg;
                    if (y > 140) c = topBar;
                    else if (y > 144 && y < 154 && x > 10 && x < 38) c = ytRed;
                    // Main video player viewport
                    else if (x > 12 && x < 178 && y > 36 && y < 132)
                    {
                        float n = (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.12f;
                        c = new Color(0.10f + n, 0.14f + n, 0.18f + n);
                        // Play triangle icon in center (oriented right ▶ on -Z facing screen)
                        int dx = 95 - x, dy = y - 84;
                        if (dx >= -10 && dx <= 14 && Mathf.Abs(dy) <= (14 - dx) * 0.65f) c = ytRed;
                    }
                    // Red progress bar under player
                    else if (x > 12 && x < 118 && y >= 31 && y <= 34) c = ytRed;
                    else if (x >= 118 && x < 178 && y >= 31 && y <= 34) c = new Color(0.32f, 0.34f, 0.38f);
                    // Right sidebar recommended mystery thumbnails
                    else if (x > 186 && x < 244 && ((y > 102 && y < 130) || (y > 68 && y < 96) || (y > 34 && y < 62)))
                    {
                        c = new Color(0.18f, 0.22f, 0.28f);
                    }
                    else if (x > 12 && x < 150 && y > 14 && y < 24 && (y % 5 > 1)) c = cyan;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureBorderTerminalScreenTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            Color bg = new Color(0.03f, 0.08f, 0.11f);
            Color cyan = new Color(0.16f, 0.90f, 0.82f);
            Color amber = new Color(1.0f, 0.78f, 0.22f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = bg;
                    if (y % 4 == 0) c *= 0.85f; // Scanlines
                    if (y > 102 && y < 120 && x > 10 && x < 246) c = new Color(0.06f, 0.24f, 0.30f);
                    else if (y > 54 && y < 92 && x > 14 && x < 118 && ((x % 16) < 11) && ((y % 14) < 10)) c = cyan;
                    else if (y > 54 && y < 92 && x > 134 && x < 242 && ((y % 10) > 4)) c = amber;
                    else if (y > 16 && y < 40 && x > 14 && x < 242 && ((y % 8) > 3)) c = new Color(0.45f, 0.88f, 0.94f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        // =====================================================================
        // SEAMLESS PERIODIC NOISE & REMANENCIA NIVEL 0 ARCHITECTURAL TEXTURES
        // =====================================================================
        private static float PeriodicHash(int ix, int iy, int px, int py, int seed)
        {
            int x = ((ix % px) + px) % px;
            int y = ((iy % py) + py) % py;
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215f;
        }

        private static float PeriodicValueNoise(float u, float v, int periodX, int periodY, int seed)
        {
            float fx = u * periodX;
            float fy = v * periodY;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float v00 = PeriodicHash(x0, y0, periodX, periodY, seed);
            float v10 = PeriodicHash(x0 + 1, y0, periodX, periodY, seed);
            float v01 = PeriodicHash(x0, y0 + 1, periodX, periodY, seed);
            float v11 = PeriodicHash(x0 + 1, y0 + 1, periodX, periodY, seed);

            float vx0 = Mathf.Lerp(v00, v10, tx);
            float vx1 = Mathf.Lerp(v01, v11, tx);
            return Mathf.Lerp(vx0, vx1, ty);
        }

        private static float PeriodicFbm(float u, float v, int basePeriodX, int basePeriodY, int octaves, int seed)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            int px = basePeriodX;
            int py = basePeriodY;
            for (int o = 0; o < octaves; o++)
            {
                sum += PeriodicValueNoise(u, v, px, py, seed + o * 97) * amp;
                norm += amp;
                amp *= 0.5f;
                px *= 2;
                py *= 2;
            }
            return sum / norm;
        }

        /// <summary>
        /// Re-authors T_Backrooms_YellowWallpaper.png (preserving filename & GUID) as unfinished
        /// poured-concrete hotel construction with formwork seams, tie-rod dimples, irregular
        /// weathered off-white plaster skim patches, and subtle lower-wall dust/moisture staining.
        /// </summary>
        private static string EnsureBackroomsWallpaperTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 512;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float v = (float)y / sz;
                float lowerDust = y < 104 ? Mathf.Pow(1f - y / 104f, 1.5f) * 0.13f : 0f;
                bool formSeamH = (y % 128) < 3;
                bool formLipH = (y % 128) == 3 || (y % 128) == 4;

                for (int x = 0; x < sz; x++)
                {
                    float u = (float)x / sz;
                    float macro = PeriodicFbm(u, v, 4, 4, 4, 101);
                    float fine = PeriodicFbm(u, v, 24, 24, 3, 211) - 0.5f;
                    float streak = PeriodicFbm(u, v, 16, 3, 3, 307);

                    // Base raw unfinished poured concrete (dusty warm mineral gray)
                    Color concrete = new Color(
                        0.55f + (macro - 0.5f) * 0.10f + fine * 0.08f,
                        0.54f + (macro - 0.5f) * 0.10f + fine * 0.08f,
                        0.52f + (macro - 0.5f) * 0.09f + fine * 0.07f);

                    // Formwork board horizontal seams & subtle vertical shuttering joints
                    bool formSeamV = ((x + ((y / 128) % 2) * 128) % 256) < 2;
                    if (formSeamH || formSeamV)
                    {
                        concrete *= 0.84f;
                    }
                    else if (formLipH)
                    {
                        concrete *= 1.05f;
                    }

                    // Recessed concrete tie-rod pin dimples
                    int tx = (x % 128) - 64;
                    int ty = (y % 128) - 64;
                    int distSq = tx * tx + ty * ty;
                    if (distSq < 18)
                    {
                        concrete *= 0.68f;
                    }
                    else if (distSq < 34)
                    {
                        concrete *= 0.90f;
                    }

                    // Irregular troweled unfinished off-white plaster skim-coat patches
                    float plasterMask = PeriodicFbm(u, v, 3, 3, 4, 419);
                    float plasterBlend = Mathf.SmoothStep(0.55f, 0.64f, plasterMask);
                    if (plasterBlend > 0.001f)
                    {
                        float trowel = (PeriodicFbm(u, v, 12, 6, 3, 521) - 0.5f) * 0.07f;
                        Color plaster = new Color(
                            0.72f + trowel + fine * 0.04f,
                            0.70f + trowel + fine * 0.04f,
                            0.67f + trowel + fine * 0.04f);
                        // Faint dusty terracotta primer undertone along worn patch margins
                        float margin = plasterBlend * (1f - plasterBlend) * 4f;
                        plaster = Color.Lerp(plaster, new Color(0.64f, 0.56f, 0.50f), margin * 0.35f);
                        concrete = Color.Lerp(concrete, plaster, plasterBlend);
                    }

                    // Localized mineral seepage / vertical water stain & bottom dust gradient
                    float stain = Mathf.Clamp01((streak - 0.62f) * 2.4f) * (1f - v * 0.45f) * 0.14f;
                    float r = Mathf.Clamp01(concrete.r - lowerDust - stain * 0.92f);
                    float g = Mathf.Clamp01(concrete.g - lowerDust * 1.02f - stain);
                    float b = Mathf.Clamp01(concrete.b - lowerDust * 1.05f - stain * 1.06f);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        /// <summary>
        /// Generates T_Remanencia_Level0_PlasterWall.png: weathered off-white and muted faded terracotta
        /// northern Mexican stucco plaster interrupted mid-finish, exposing patches of raw cement blockwork.
        /// </summary>
        private static string EnsureRemanenciaPlasterWallTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 512;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float v = (float)y / sz;
                float lowerDust = y < 100 ? Mathf.Pow(1f - y / 100f, 1.4f) * 0.14f : 0f;
                bool blockCourseH = (y % 48) < 3;

                for (int x = 0; x < sz; x++)
                {
                    float u = (float)x / sz;
                    float macro = PeriodicFbm(u, v, 4, 4, 4, 613);
                    float fine = PeriodicFbm(u, v, 28, 28, 3, 709) - 0.5f;
                    float wash = PeriodicFbm(u, v, 3, 5, 3, 811);

                    // Base weathered off-white plaster blended with muted faded terracotta stucco patches
                    Color offWhite = new Color(0.71f + fine * 0.06f, 0.69f + fine * 0.06f, 0.65f + fine * 0.05f);
                    Color fadedTerracotta = new Color(0.65f + fine * 0.05f, 0.55f + fine * 0.05f, 0.49f + fine * 0.05f);
                    float terraBlend = Mathf.SmoothStep(0.46f, 0.72f, wash) * 0.62f;
                    Color wallCol = Color.Lerp(offWhite, fadedTerracotta, terraBlend);

                    // Exposed unfinished concrete-block / raw cement patches where skim coat stopped
                    float rawPatch = Mathf.SmoothStep(0.40f, 0.31f, macro);
                    if (rawPatch > 0.001f)
                    {
                        bool blockJointV = ((x + ((y / 48) % 2) * 48) % 96) < 3;
                        float mortar = (blockCourseH || blockJointV) ? -0.07f : 0f;
                        Color rawBlock = new Color(
                            0.51f + mortar + fine * 0.09f,
                            0.50f + mortar + fine * 0.09f,
                            0.49f + mortar + fine * 0.08f);
                        wallCol = Color.Lerp(wallCol, rawBlock, rawPatch);
                    }

                    // Subtle damp streaks near upper/lower transitions
                    float drip = Mathf.Clamp01((PeriodicFbm(u, v, 14, 3, 3, 919) - 0.64f) * 2.2f) * 0.11f;
                    float r = Mathf.Clamp01(wallCol.r - lowerDust - drip * 0.90f);
                    float g = Mathf.Clamp01(wallCol.g - lowerDust * 1.03f - drip);
                    float b = Mathf.Clamp01(wallCol.b - lowerDust * 1.06f - drip * 1.05f);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        /// <summary>
        /// Generates T_Remanencia_Level0_ConcreteColumn.png: reinforced cast-concrete structural column
        /// with pour lift lines, corner formwork shading, darker cement patch repairs, and lower weathering.
        /// </summary>
        private static string EnsureRemanenciaConcreteColumnTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 512;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float v = (float)y / sz;
                float lowerWeather = y < 128 ? Mathf.Pow(1f - y / 128f, 1.35f) * 0.16f : 0f;
                bool liftSeam = (y % 170) < 4;

                for (int x = 0; x < sz; x++)
                {
                    float u = (float)x / sz;
                    float macro = PeriodicFbm(u, v, 4, 5, 4, 1021);
                    float fine = PeriodicFbm(u, v, 32, 32, 3, 1117) - 0.5f;

                    // Cooler structural reinforced concrete gray
                    Color col = new Color(
                        0.49f + (macro - 0.5f) * 0.11f + fine * 0.09f,
                        0.495f + (macro - 0.5f) * 0.11f + fine * 0.09f,
                        0.49f + (macro - 0.5f) * 0.10f + fine * 0.08f);

                    // Vertical formwork edge bevels on column faces
                    if (x < 14 || x > sz - 15)
                    {
                        float edgeFactor = (x < 14) ? (1f - x / 14f) : (1f - (sz - 1 - x) / 14f);
                        col *= (1f - edgeFactor * 0.15f);
                    }

                    // Horizontal concrete pour lift lines & tie holes
                    if (liftSeam) col *= 0.80f;
                    int tx = (x % 256) - 128;
                    int ty = (y % 170) - 85;
                    if (tx * tx + ty * ty < 22) col *= 0.64f;

                    // Darker cement repair patches
                    float repairMask = PeriodicFbm(u, v, 5, 5, 3, 1223);
                    float repairBlend = Mathf.SmoothStep(0.65f, 0.72f, repairMask);
                    col = Color.Lerp(col, new Color(0.38f + fine * 0.05f, 0.385f + fine * 0.05f, 0.38f + fine * 0.05f), repairBlend * 0.85f);

                    // Small unfinished white-gray plaster smears
                    float plasterMask = PeriodicFbm(u, v, 4, 4, 3, 1327);
                    float plasterBlend = Mathf.SmoothStep(0.68f, 0.75f, plasterMask) * 0.55f;
                    col = Color.Lerp(col, new Color(0.66f, 0.65f, 0.62f), plasterBlend);

                    float r = Mathf.Clamp01(col.r - lowerWeather);
                    float g = Mathf.Clamp01(col.g - lowerWeather * 1.02f);
                    float b = Mathf.Clamp01(col.b - lowerWeather * 1.04f);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        /// <summary>
        /// Re-authors T_Backrooms_DampCarpet.png (preserving filename & GUID) as an unfinished
        /// poured-concrete hotel floor with expansion joints, trowel variation, fine cracks, dust,
        /// localized damp/repaired patches, and a subtle incomplete ceramic hotel tile remnant.
        /// </summary>
        private static string EnsureBackroomsCarpetTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 512;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float v = (float)y / sz;
                bool expJointY = y < 3 || y >= sz - 2;
                for (int x = 0; x < sz; x++)
                {
                    float u = (float)x / sz;
                    bool expJointX = x < 3 || x >= sz - 2;

                    float macro = PeriodicFbm(u, v, 4, 4, 4, 1429);
                    float trowel = PeriodicFbm(u, v, 8, 8, 3, 1531) - 0.5f;
                    float aggregate = PeriodicFbm(u, v, 48, 48, 2, 1637) - 0.5f;

                    // Raw troweled poured concrete (balanced mineral gray for crisp walkability)
                    Color floorCol = new Color(
                        0.48f + (macro - 0.5f) * 0.12f + trowel * 0.06f + aggregate * 0.07f,
                        0.475f + (macro - 0.5f) * 0.12f + trowel * 0.06f + aggregate * 0.07f,
                        0.46f + (macro - 0.5f) * 0.11f + trowel * 0.055f + aggregate * 0.065f);

                    // Lighter dusty plaster/cement powder drifts
                    float dust = Mathf.SmoothStep(0.60f, 0.76f, PeriodicFbm(u, v, 5, 5, 3, 1741));
                    floorCol = Color.Lerp(floorCol, new Color(0.57f, 0.56f, 0.53f), dust * 0.55f);

                    // Localized darker damp or repaired concrete patches (restrained, never black)
                    float damp = Mathf.SmoothStep(0.64f, 0.76f, PeriodicFbm(u, v, 3, 3, 3, 1847));
                    floorCol *= (1f - damp * 0.22f);

                    // Fine shrinkage hairline crack ridge
                    float crackField = Mathf.Abs(PeriodicFbm(u, v, 6, 6, 4, 1951) - 0.5f);
                    if (crackField < 0.0085f && macro < 0.55f)
                    {
                        float crackStrength = 1f - (crackField / 0.0085f);
                        floorCol *= (1f - crackStrength * 0.34f);
                    }

                    // Subtle localized remnant cluster of unfinished muted aged hotel ceramic tiles (32x32 px each)
                    // Stair-stepped incomplete border showing where tile installation halted mid-construction
                    int tileCol = x / 32;
                    int tileRow = y / 32;
                    bool inTileRemnant =
                        (tileRow == 2 && tileCol >= 2 && tileCol <= 5) ||
                        (tileRow == 3 && tileCol >= 2 && tileCol <= 6) ||
                        (tileRow == 4 && tileCol >= 3 && tileCol <= 5) ||
                        (tileRow == 5 && tileCol >= 3 && tileCol <= 4);
                    if (inTileRemnant)
                    {
                        int lx = x % 32;
                        int ly = y % 32;
                        bool grout = lx < 2 || ly < 2;
                        if (grout)
                        {
                            floorCol = new Color(0.36f, 0.35f, 0.34f);
                        }
                        else
                        {
                            bool altTile = ((tileCol + tileRow) % 2) == 0;
                            float tileVar = (PeriodicHash(tileCol, tileRow, 16, 16, 2063) - 0.5f) * 0.05f;
                            Color ceramic = altTile
                                ? new Color(0.60f + tileVar + aggregate * 0.03f, 0.56f + tileVar + aggregate * 0.03f, 0.51f + tileVar + aggregate * 0.03f)
                                : new Color(0.56f + tileVar + aggregate * 0.03f, 0.53f + tileVar + aggregate * 0.03f, 0.49f + tileVar + aggregate * 0.03f);
                            // Dust & wear blending tiles naturally into surrounding slab
                            floorCol = Color.Lerp(ceramic, floorCol, 0.22f + dust * 0.30f);
                        }
                    }

                    // Concrete slab expansion-joint score lines
                    if (expJointX || expJointY)
                    {
                        floorCol *= 0.72f;
                    }

                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(floorCol.r),
                        Mathf.Clamp01(floorCol.g),
                        Mathf.Clamp01(floorCol.b)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        /// <summary>
        /// Re-authors T_Backrooms_CeilingGrid.png (preserving filename & GUID) as an exposed
        /// cast-concrete structural soffit with coffered beam transitions, plywood formwork seams,
        /// visible galvanized conduit runs, and subtle mineral water staining (zero office ceiling grid).
        /// </summary>
        private static string EnsureBackroomsCeilingGridTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 512;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float v = (float)y / sz;
                bool beamEdgeY = y < 22 || y >= sz - 22;
                bool beamLipY = y == 22 || y == 23 || y == sz - 23 || y == sz - 24;
                bool formJointY = (y % 128) < 2;

                for (int x = 0; x < sz; x++)
                {
                    float u = (float)x / sz;
                    bool beamEdgeX = x < 24 || x >= sz - 24;
                    bool beamLipX = x == 24 || x == 25 || x == sz - 25 || x == sz - 26;
                    bool formJointX = (x % 256) < 2;

                    float macro = PeriodicFbm(u, v, 4, 4, 4, 2179);
                    float fine = PeriodicFbm(u, v, 32, 32, 3, 2281) - 0.5f;
                    float woodGrain = (PeriodicFbm(u, v, 32, 6, 2, 2389) - 0.5f) * 0.035f;

                    // Cool cast-concrete soffit base
                    Color soffit = new Color(
                        0.49f + (macro - 0.5f) * 0.10f + fine * 0.07f + woodGrain,
                        0.50f + (macro - 0.5f) * 0.10f + fine * 0.07f + woodGrain,
                        0.51f + (macro - 0.5f) * 0.10f + fine * 0.07f + woodGrain);

                    // Coffered structural concrete beam transitions along bay borders
                    if (beamEdgeX || beamEdgeY)
                    {
                        soffit *= 0.80f;
                    }
                    else if (beamLipX || beamLipY)
                    {
                        soffit *= 0.68f;
                    }
                    else if (formJointX || formJointY)
                    {
                        soffit *= 0.88f;
                    }

                    // Subtle mineral moisture seepage patches
                    float seepage = Mathf.SmoothStep(0.65f, 0.78f, PeriodicFbm(u, v, 4, 4, 3, 2477));
                    soffit *= (1f - seepage * 0.18f);

                    // Exposed galvanized steel electrical conduit runs & junction box along soffit
                    bool conduitX = y >= 210 && y <= 216;
                    bool conduitY = x >= 146 && x <= 152;
                    bool junctionBox = x >= 139 && x <= 159 && y >= 203 && y <= 223;
                    bool conduitShadow = (y >= 217 && y <= 219) || (x >= 153 && x <= 155);
                    if (junctionBox)
                    {
                        bool rim = x <= 141 || x >= 157 || y <= 205 || y >= 221;
                        soffit = rim ? new Color(0.22f, 0.23f, 0.25f) : new Color(0.32f, 0.34f, 0.36f);
                    }
                    else if (conduitX || conduitY)
                    {
                        bool strap = (conduitX && (x % 64) < 3) || (conduitY && (y % 64) < 3);
                        soffit = strap ? new Color(0.20f, 0.21f, 0.23f) : new Color(0.34f, 0.36f, 0.38f);
                    }
                    else if (conduitShadow)
                    {
                        soffit *= 0.76f;
                    }

                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(soffit.r),
                        Mathf.Clamp01(soffit.g),
                        Mathf.Clamp01(soffit.b)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            SetMat(go, mat);
            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        private static void AttachDesc(GameObject target, string id, string titleEs, string titleEn, string loreEs, string loreEn, Color accent)
        {
            var d = target.AddComponent<SecretContainmentArtifactDescriptor>();
            d.artifactId = id;
            d.titleEs = titleEs;
            d.titleEn = titleEn;
            d.loreEs = loreEs;
            d.loreEn = loreEn;
            d.accentColor = accent;
        }

        private static Shader FindLitShader() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        private static Material CreateColorMat(string assetPath, Color color, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null) { mat = new Material(FindLitShader()); AssetDatabase.CreateAsset(mat, assetPath); }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTexturedMat(string assetPath, string texPath, Color tint, float smoothness, Vector2 tiling)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Material mat = CreateColorMat(assetPath, tint, smoothness, 0f);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", tiling); }
                if (mat.HasProperty("_MainTex")) { mat.SetTexture("_MainTex", tex); mat.SetTextureScale("_MainTex", tiling); }
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissiveTexturedMat(string assetPath, string texPath, Color baseTint, Color emissionTint)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Material mat = CreateTexturedMat(assetPath, texPath, baseTint, 0.35f, Vector2.one);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionTint);
            }
            if (tex != null && mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", tex);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissiveMat(string assetPath, Color baseColor, Color emissionColor)
        {
            Material mat = CreateColorMat(assetPath, baseColor, 0.35f, 0f);
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissionColor); }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
