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
        public const string NorthWingM1RootName = "Hotel_NorthWing_M1";
        public const string HotelNarrativeEvidenceRootName = "Hotel_Narrative_Evidence";
        public const string HotelEarlyEcologyRootName = "Hotel_Early_Ecology";
        public const string HotelFaunalTracesRootName = "Hotel_Faunal_Traces";
        public const string HotelFirstLivingMicrofaunaRootName = "Hotel_FirstLivingMicrofauna";
        public const string HotelRoom111BRootName = "Hotel_Room111B_Anomaly";
        public const string HotelRoom111BGuestInteriorRootName = "Hotel_Room111B_GuestInterior";
        public const string NorthwestServiceStaircaseRootName = "Hotel_NorthwestServiceStaircase";
        public const string NorthUpperGalleryRootName = "Hotel_NorthUpperGallery";
        public const string CourtyardLightwellOverlookRootName = "Hotel_CourtyardLightwellOverlook";
        public const string EastOverhangRootName = "Hotel_EastOverhang";
        public const string OverlookSafetyGlassMatPath = "Assets/Settings/Remanencia_Level0_OverlookSafetyGlass.mat";
        public const string RoomPlaque111BTexPath = "Assets/Textures/Environment/T_Remanencia_RoomPlaque_111B.png";
        public const string RoomPlaque111BMatPath = "Assets/Settings/Remanencia_Level0_RoomPlaque_111B.mat";

        // Implementation 07.12: Public North Corridor directory (107-110 ONLY — excludes secret 111-B),
        // Room 111-B guest-room interior materials, Threshold of Trust & isolated one-way entrance glass
        public const string NorthCorridorDirectoryEs = "HABITACIONES 107-110";
        public const string NorthCorridorDirectoryRooms = "107  108  109  110";
        public const string NorthCorridorDirectoryTexPath = "Assets/Textures/Environment/T_Remanencia_Sign_NorthCorridorDirectory.png";
        public const string NorthCorridorDirectoryMatPath = "Assets/Settings/Remanencia_Sign_NorthCorridorDirectory.mat";
        public const string Room111BFloorTileTexPath = "Assets/Textures/Environment/T_Remanencia_Room111B_FloorTile.png";
        public const string Room111BFloorTileMatPath = "Assets/Settings/Remanencia_Room111B_FloorTile.mat";
        public const string Room111BWoodMatPath = "Assets/Settings/Remanencia_Room111B_Wood.mat";
        public const string Room111BLinenMatPath = "Assets/Settings/Remanencia_Room111B_Linen.mat";
        public const string Room111BBlanketMatPath = "Assets/Settings/Remanencia_Room111B_Blanket.mat";
        public const string Room111BMirrorMatPath = "Assets/Settings/Remanencia_Room111B_MirrorGlass.mat";
        public const string ThresholdOvercastTexPath = "Assets/Textures/Environment/T_Remanencia_Threshold_OvercastHaze.png";
        public const string ThresholdOvercastMatPath = "Assets/Settings/Remanencia_Threshold_OvercastHaze.mat";
        public const string EntranceStreetFallbackTexPath = "Assets/Textures/Environment/T_Remanencia_Entrance_StreetViewFallback.png";
        public const string EntranceOneWayGlassMatPath = "Assets/Settings/Remanencia_Entrance_OneWayGlass.mat";
        public const float NorthWingMinX = -16.0f;
        public const float NorthWingMaxX = 16.0f;
        public const float NorthWingMinZ = 12.0f;
        public const float NorthWingMaxZ = 34.0f;
        public const string LightwellDiffuserMatPath = "Assets/Settings/Remanencia_Level0_LightwellDiffuser.mat";
        public const string EvidencePlanMatPath = "Assets/Settings/Remanencia_Evidence_ProjectPlan.mat";
        public const string EvidenceRegisterMatPath = "Assets/Settings/Remanencia_Evidence_RoomRegister.mat";
        public const string EvidenceMaintenanceMatPath = "Assets/Settings/Remanencia_Evidence_MaintenanceLog.mat";
        public const string EvidenceCobaltNoteMatPath = "Assets/Settings/Remanencia_Evidence_CobaltRumorNote.mat";
        public const string AnomalyMineralJointMatPath = "Assets/Settings/Remanencia_Anomaly_MineralJoint.mat";
        public const string AnomalySurfaceStainMatPath = "Assets/Settings/Remanencia_Anomaly_SurfaceStain.mat";
        public const string EcologyMyceliumMatPath = "Assets/Settings/Remanencia_Ecology_Mycelium.mat";
        public const string EcologyFungalCapMatPath = "Assets/Settings/Remanencia_Ecology_FungalCap.mat";
        public const string EcologyVegetalLeafMatPath = "Assets/Settings/Remanencia_Ecology_VegetalLeaf.mat";
        public const string EcologyVegetalRootMatPath = "Assets/Settings/Remanencia_Ecology_VegetalRoot.mat";
        public const string EcologyMyceliumMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_MyceliumWeb.asset";
        public const string EcologyFungalClusterMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_FungalCluster_Low.asset";
        public const string EcologyFungalBracketMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_FungalBracket.asset";
        public const string EcologyVegetalRootMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_VegetalRootRunner.asset";
        public const string EcologyVegetalLeafMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_VegetalLeafTuft.asset";
        public const string PipeCylinderMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_PipeCylinder.asset";
        public const string DecalQuadMeshPath = "Assets/Meshes/Remanencia/Ecology/M_Remanencia_DecalQuad.asset";
        public const string FaunalFilamentMatPath = "Assets/Settings/Remanencia_Faunal_Filament.mat";
        public const string FaunalCuticleMatPath = "Assets/Settings/Remanencia_Faunal_Cuticle.mat";
        public const string FaunalResidueMatPath = "Assets/Settings/Remanencia_Faunal_Residue.mat";
        public const string FaunalFilamentMeshPath = "Assets/Meshes/Remanencia/FaunalTraces/M_Remanencia_FaunalFilament.asset";
        public const string FaunalCuticleMeshPath = "Assets/Meshes/Remanencia/FaunalTraces/M_Remanencia_FaunalCuticleFragment.asset";
        public const string FaunalResidueMeshPath = "Assets/Meshes/Remanencia/FaunalTraces/M_Remanencia_FaunalMicroResidue.asset";
        public const string MicrofaunaBodyMatPath = "Assets/Settings/Remanencia_Microfauna_Body.mat";
        public const string MicrofaunaBodyMeshPath = "Assets/Meshes/Remanencia/Microfauna/M_Remanencia_MicrofaunaBody.asset";

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
            public Material LightwellDiffuser;
            public Material[] RoomPlaqueMats; // Index 0..9 -> Rooms 101..110
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
            string[] plaqueTexPaths = new string[10];
            for (int r = 0; r < 10; r++)
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
            for (int r = 0; r < 10; r++)
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

            Material[] plaqueMats = new Material[10];
            for (int r = 0; r < 10; r++)
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
                LightwellDiffuser = CreateEmissiveMat(LightwellDiffuserMatPath, new Color(0.58f, 0.63f, 0.68f), new Color(0.32f, 0.36f, 0.41f) * 1.15f),
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

            // Canonical North Hotel Wing & Courtyard Loop (Module M1)
            BuildOrUpdateNorthWingM1(shellRoot.transform, visualMats);

            // First Environmental Narrative Layer: 4 inspectable hotel history discoveries + subtle anomalies (Implementation 07.6)
            BuildOrUpdateHotelNarrativeLayer(root.transform, visualMats);

            // First Anomalous Ecology: localized static fungal colonies & anomalous vegetation (Implementation 07.7)
            BuildOrUpdateHotelEarlyEcology(root.transform);

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
        /// Targeted, idempotent Implementation 07.5 horizontal expansion migration utility.
        /// Opens the existing BackroomsLevel1Scene.unity without rebuilding the scene or modifying
        /// Hotel_GroundFloor_Architecture, Hotel_Elevator_Core, Hotel_Environmental_Identity,
        /// lighting budget (18 lights, 0 shadows), gameplay anchors, or save schemas.
        /// Opens Connection A (Main Lobby North Archway) and Connection B (NW Service Hall) cleanly
        /// along Z = +12.0 and constructs/updates Hotel_NorthWing_M1 (32m x 22m = 704 m²).
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia North Wing M1 (In-Place)")]
        public static void MigrateRemanenciaNorthWingM1AndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaNorthWingM1InOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia North Wing M1 (32m x 22m) horizontal expansion migration completed and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaNorthWingM1InOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia North Wing M1 migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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
            if (shellRoot == null)
            {
                throw new InvalidOperationException("Backrooms_Architecture not found under BackroomsLevel1_Root.");
            }

            Transform hotelRoot = shellRoot.Find(HotelArchitectureRootName);
            if (hotelRoot == null)
            {
                throw new InvalidOperationException($"Expected '{HotelArchitectureRootName}' under Backrooms_Architecture before running 07.5 North Wing M1 migration.");
            }

            RemanenciaVisualMaterials mats = EnsureRemanenciaVisualAssets();
            BuildOrUpdateNorthWingM1(shellRoot, mats);
        }

        private static void BuildOrUpdateNorthWingM1(Transform shellRoot, RemanenciaVisualMaterials mats)
        {
            const float wallT = 0.22f;
            const float outerT = 0.26f;

            // 1. Shorten retained Wall_Outer_North (and its baseboard) in place to X = -20.065..-12.000
            //    so NW Vault Safe (-16.4, 11.42) and 07.3 plumbing risers (-12.24..-12.04, 11.74) remain solidly backed
            //    while preserving the exact 15 retained Wall_* count under Backrooms_Architecture.
            UpdateRetainedWallSegmentInPlace(
                shellRoot, "Wall_Outer_North",
                new Vector2(-20.0f, 12.0f), new Vector2(-12.065f, 12.0f),
                outerT, mats.WallConcrete, mats.BaseboardConcrete);

            Transform m1Root = EnsureHotelSubgroup(shellRoot, NorthWingM1RootName);

            // =========================================================================
            // 1. CONNECTION A — MAIN LOBBY NORTH ARCHWAY + M1 SHELL SLABS & OUTER PERIMETER
            //    Clear opening: X = +3.80..+7.00 (3.20 m clear width x 2.42 m height)
            // =========================================================================
            Transform connLobby = EnsureHotelSubgroup(m1Root, "NorthWing_Connection_Lobby");

            // Seamless East North-Perimeter Closure behind Reception Alcove & Entrance Vestibule (X = +7.20..+20.065)
            EnsureHotelWallSegment(connLobby, "NorthWing_PerimeterSeal_East",
                new Vector2(7.265f, 12.0f), new Vector2(20.0f, 12.0f), outerT, mats.WallConcrete, mats.BaseboardConcrete);

            // Framed Main Lobby North Archway piers, overhead transom, metal reveal trim & flush threshold
            EnsureHotelBox(connLobby, "NorthWing_ConnA_ArchPierW",
                new Vector3(3.70f, BaseY + ClearHeight * 0.5f, 12.0f), Quaternion.identity,
                new Vector3(0.20f, ClearHeight, 0.34f), mats.ColumnConcrete, keepCollider: true);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_ArchPierE",
                new Vector3(7.10f, BaseY + ClearHeight * 0.5f, 12.0f), Quaternion.identity,
                new Vector3(0.20f, ClearHeight, 0.34f), mats.ColumnConcrete, keepCollider: true);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_ArchTransom",
                new Vector3(5.40f, CeilingY - 0.215f, 12.0f), Quaternion.identity,
                new Vector3(3.60f, 0.43f, 0.32f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_TrimW",
                new Vector3(3.82f, BaseY + 1.21f, 12.0f), Quaternion.identity,
                new Vector3(0.045f, 2.42f, 0.36f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_TrimE",
                new Vector3(6.98f, BaseY + 1.21f, 12.0f), Quaternion.identity,
                new Vector3(0.045f, 2.42f, 0.36f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_TrimTop",
                new Vector3(5.40f, BaseY + 2.41f, 12.0f), Quaternion.identity,
                new Vector3(3.24f, 0.055f, 0.36f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connLobby, "NorthWing_ConnA_FloorThreshold",
                new Vector3(5.40f, BaseY + 0.004f, 12.0f), Quaternion.identity,
                new Vector3(3.20f, 0.008f, 0.28f), mats.IndustrialMetal, keepCollider: false);

            // Continuous ground-floor slab (X=-16.3..+16.3, Z=+12.2..+34.3; 10cm overlap with Backrooms_Floor Zmax=+12.3, zero gap, zero fall hazard)
            EnsureHotelBox(connLobby, "NorthWing_FloorSlab",
                new Vector3(0.0f, BaseY - 0.151f, 23.25f), Quaternion.identity,
                new Vector3(32.60f, 0.30f, 22.10f), mats.FloorConcrete, keepCollider: true);

            // 4 Perimeter ceiling slabs framing the 10m x 8m central Courtyard Lightwell opening (X=-5..+5, Z=+18..+26)
            EnsureHotelBox(connLobby, "NorthWing_CeilingSlab_South",
                new Vector3(0.0f, CeilingY + 0.102f, 15.10f), Quaternion.identity,
                new Vector3(32.60f, 0.20f, 5.80f), mats.CeilingSoffit, keepCollider: true);
            // When Implementation 07.15 (Hotel_NorthwestServiceStaircase) is installed, preserve the
            // trimmed NorthWing_CeilingSlab_North opening above the NW reserve (X < -9.60) in-place.
            bool hasNorthwestStaircase = shellRoot.Find(NorthwestServiceStaircaseRootName) != null;
            EnsureHotelBox(connLobby, "NorthWing_CeilingSlab_North",
                new Vector3(hasNorthwestStaircase ? NorthwestServiceStaircaseBuilder.TrimmedCeilingNorthCenterX : 0.0f, CeilingY + 0.102f, 30.15f),
                Quaternion.identity,
                new Vector3(hasNorthwestStaircase ? NorthwestServiceStaircaseBuilder.TrimmedCeilingNorthWidthX : 32.60f, 0.20f, 8.30f),
                mats.CeilingSoffit, keepCollider: true);
            EnsureHotelBox(connLobby, "NorthWing_CeilingSlab_West",
                new Vector3(-10.65f, CeilingY + 0.102f, 22.0f), Quaternion.identity,
                new Vector3(11.30f, 0.20f, 8.00f), mats.CeilingSoffit, keepCollider: true);
            EnsureHotelBox(connLobby, "NorthWing_CeilingSlab_East",
                new Vector3(10.65f, CeilingY + 0.102f, 22.0f), Quaternion.identity,
                new Vector3(11.30f, 0.20f, 8.00f), mats.CeilingSoffit, keepCollider: true);

            // Outer M1 Perimeter Enclosure (X=-16..+16, Z=+12..+34 -> exact 32m x 22m = 704 m² envelope)
            // When Implementation 07.11 (Hotel_Room111B_Anomaly) is installed, preserve the concealed East opening at Z = +26.80..+28.40
            // while keeping Z = +12.0..+26.80 (including Z = +20.0) solidly sealed on the retained NorthWing_OuterWall_East GameObject.
            float eastOuterWallEndZ = shellRoot.Find(HotelRoom111BRootName) != null ? 26.735f : 34.0f;
            EnsureHotelWallSegment(connLobby, "NorthWing_OuterWall_North",
                new Vector2(-16.0f, 34.0f), new Vector2(16.0f, 34.0f), outerT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(connLobby, "NorthWing_OuterWall_East",
                new Vector2(16.0f, 12.0f), new Vector2(16.0f, eastOuterWallEndZ), outerT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(connLobby, "NorthWing_OuterWall_West",
                new Vector2(-16.0f, 12.0f), new Vector2(-16.0f, 34.0f), outerT, mats.WallConcrete, mats.BaseboardConcrete);

            // =========================================================================
            // 2. CONNECTION B — NORTHWEST SERVICE HALL + MID NORTH-PERIMETER SEAL
            //    Clear opening: X = -12.00..-9.58 (2.42 m masonry / 2.34 m trim clear width)
            // =========================================================================
            Transform connService = EnsureHotelSubgroup(m1Root, "NorthWing_Connection_Service");

            // Seamless Mid North-Perimeter Closure behind Rooms 102, 101 & Luggage Alcove (X = -9.58..+3.60)
            EnsureHotelWallSegment(connService, "NorthWing_PerimeterSeal_Mid",
                new Vector2(-9.515f, 12.0f), new Vector2(3.535f, 12.0f), outerT, mats.WallConcrete, mats.BaseboardConcrete);

            EnsureHotelBox(connService, "NorthWing_ConnB_Transom",
                new Vector3(-10.79f, BaseY + 2.28f + 0.285f, 12.0f), Quaternion.identity,
                new Vector3(2.46f, 0.57f, 0.30f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(connService, "NorthWing_ConnB_TrimW",
                new Vector3(-11.98f, BaseY + 1.14f, 12.0f), Quaternion.identity,
                new Vector3(0.04f, 2.28f, 0.34f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connService, "NorthWing_ConnB_TrimE",
                new Vector3(-9.60f, BaseY + 1.14f, 12.0f), Quaternion.identity,
                new Vector3(0.04f, 2.28f, 0.34f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connService, "NorthWing_ConnB_TrimTop",
                new Vector3(-10.79f, BaseY + 2.28f, 12.0f), Quaternion.identity,
                new Vector3(2.44f, 0.055f, 0.34f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(connService, "NorthWing_ConnB_FloorThreshold",
                new Vector3(-10.79f, BaseY + 0.004f, 12.0f), Quaternion.identity,
                new Vector3(2.40f, 0.008f, 0.28f), mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // 3. CENTRAL COURTYARD / UNFINISHED ATRIUM (10m x 8m: X=-5..+5, Z=+18..+26)
            //    Safe ground-floor courtyard framed by colonnade piers, waist parapets,
            //    4 wide cardinal portals (2.8m-3.2m), central plinth & sealed skylight shaft.
            // =========================================================================
            Transform courtyard = EnsureHotelSubgroup(m1Root, "NorthWing_Courtyard");

            // 4 corner structural colonnade piers (full floor-to-ceiling concrete)
            Vector2[] courtCorners =
            {
                new Vector2(-4.85f, 18.15f),
                new Vector2( 4.85f, 18.15f),
                new Vector2(-4.85f, 25.85f),
                new Vector2( 4.85f, 25.85f)
            };
            for (int c = 0; c < courtCorners.Length; c++)
            {
                EnsureHotelBox(courtyard, $"NorthWing_Courtyard_CornerPier_{c}",
                    new Vector3(courtCorners[c].x, BaseY + ClearHeight * 0.5f, courtCorners[c].y), Quaternion.identity,
                    new Vector3(0.54f, ClearHeight, 0.54f), mats.ColumnConcrete, keepCollider: true);
            }

            // Low unfinished concrete parapet curbs (1.02m tall) framing 4 wide cardinal portals
            // South & North portals: X = -1.60..+1.60 (3.20m clear)
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetSW", new Vector3(-3.12f, BaseY + 0.51f, 18.15f), Quaternion.identity, new Vector3(2.92f, 1.02f, 0.24f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetSE", new Vector3( 3.12f, BaseY + 0.51f, 18.15f), Quaternion.identity, new Vector3(2.92f, 1.02f, 0.24f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetNW", new Vector3(-3.12f, BaseY + 0.51f, 25.85f), Quaternion.identity, new Vector3(2.92f, 1.02f, 0.24f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetNE", new Vector3( 3.12f, BaseY + 0.51f, 25.85f), Quaternion.identity, new Vector3(2.92f, 1.02f, 0.24f), mats.WallConcrete, keepCollider: true);
            // West & East portals: Z = +20.60..+23.40 (2.80m clear)
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetWS", new Vector3(-4.85f, BaseY + 0.51f, 19.48f), Quaternion.identity, new Vector3(0.24f, 1.02f, 2.16f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetWN", new Vector3(-4.85f, BaseY + 0.51f, 24.52f), Quaternion.identity, new Vector3(0.24f, 1.02f, 2.16f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetES", new Vector3( 4.85f, BaseY + 0.51f, 19.48f), Quaternion.identity, new Vector3(0.24f, 1.02f, 2.16f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_ParapetEN", new Vector3( 4.85f, BaseY + 0.51f, 24.52f), Quaternion.identity, new Vector3(0.24f, 1.02f, 2.16f), mats.WallConcrete, keepCollider: true);

            // Courtyard interior architectural centerpiece: stepped concrete planter plinth + drain + 2 offset sightline columns
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_CenterPlinthBase",
                new Vector3(0.0f, BaseY + 0.21f, 22.0f), Quaternion.identity,
                new Vector3(2.20f, 0.42f, 1.40f), mats.ColumnConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_CenterPlinthTrim",
                new Vector3(0.0f, BaseY + 0.43f, 22.0f), Quaternion.identity,
                new Vector3(2.30f, 0.05f, 1.50f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_CenterDrainGrate",
                new Vector3(0.0f, BaseY + 0.46f, 22.0f), Quaternion.identity,
                new Vector3(0.36f, 0.015f, 0.36f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_SightlinePierSW",
                new Vector3(-2.70f, BaseY + ClearHeight * 0.5f, 20.30f), Quaternion.identity,
                new Vector3(0.42f, ClearHeight, 0.42f), mats.ColumnConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Courtyard_SightlinePierNE",
                new Vector3( 2.70f, BaseY + ClearHeight * 0.5f, 23.70f), Quaternion.identity,
                new Vector3(0.42f, ClearHeight, 0.42f), mats.ColumnConcrete, keepCollider: true);

            // Raised coffered concrete skylight lightwell shaft (Y = CeilingY..-44.80m, solidly sealed on top)
            const float wellHeight = 2.35f;
            float wellMidY = CeilingY + wellHeight * 0.5f;
            float wellTopY = CeilingY + wellHeight; // -44.80m
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_WallS", new Vector3( 0.0f, wellMidY, 17.92f), Quaternion.identity, new Vector3(10.32f, wellHeight, 0.20f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_WallN", new Vector3( 0.0f, wellMidY, 26.08f), Quaternion.identity, new Vector3(10.32f, wellHeight, 0.20f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_WallW", new Vector3(-5.08f, wellMidY, 22.00f), Quaternion.identity, new Vector3(0.20f, wellHeight, 8.32f), mats.WallConcrete, keepCollider: true);
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_WallE", new Vector3( 5.08f, wellMidY, 22.00f), Quaternion.identity, new Vector3(0.20f, wellHeight, 8.32f), mats.WallConcrete, keepCollider: true);

            // Structural skylight cross-beams & galvanized purlins
            float[] wellBeamXs = { -2.4f, 0.0f, 2.4f };
            for (int b = 0; b < wellBeamXs.Length; b++)
            {
                EnsureHotelBox(courtyard, $"NorthWing_Lightwell_ConcreteBeam_{b}",
                    new Vector3(wellBeamXs[b], wellTopY - 0.36f, 22.0f), Quaternion.identity,
                    new Vector3(0.30f, 0.48f, 8.10f), mats.ColumnConcrete, keepCollider: false);
            }
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_SteelPurlinS",
                new Vector3(0.0f, wellTopY - 0.14f, 20.6f), Quaternion.identity,
                new Vector3(10.10f, 0.14f, 0.14f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_SteelPurlinN",
                new Vector3(0.0f, wellTopY - 0.14f, 23.4f), Quaternion.identity,
                new Vector3(10.10f, 0.14f, 0.14f), mats.IndustrialMetal, keepCollider: false);

            // Luminous overcast translucent diffuser skylight cap (solid collider seals top cleanly)
            EnsureHotelBox(courtyard, "NorthWing_Lightwell_SkylightCap",
                new Vector3(0.0f, wellTopY + 0.08f, 22.0f), Quaternion.identity,
                new Vector3(10.60f, 0.16f, 8.60f), mats.LightwellDiffuser, keepCollider: true);

            // =========================================================================
            // 4. EAST ARCADE, SOUTH COURTYARD PROMENADE & NORTH CORRIDOR STRUCTURE
            //    South Promenade: Z = +15.0..+18.0 (3.0m wide) + recessed South alcoves (Z = +12..+15)
            //    East Arcade: X = +5.0..+8.2 (3.2m wide), North Corridor: Z = +26.0..+28.8 (2.8m wide)
            // =========================================================================
            Transform eastArcade = EnsureHotelSubgroup(m1Root, "NorthWing_EastArcade");

            // South Promenade sightline-break architectural alcoves (Z = +12.0..+15.0, open North into Z = 15..18)
            EnsureHotelWallSegment(eastArcade, "NorthWing_SouthPromenade_AlcovePierW",
                new Vector2(-4.2f, 12.0f), new Vector2(-4.2f, 15.0f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(eastArcade, "NorthWing_SouthPromenade_AlcovePierMid",
                new Vector2(-1.0f, 12.0f), new Vector2(-1.0f, 15.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(eastArcade, "NorthWing_SouthPromenade_LobbyNibPier",
                new Vector2(3.6f, 12.0f), new Vector2(3.6f, 14.6f), 0.28f, mats.ColumnConcrete, mats.BaseboardConcrete);

            // Overhead soffit beams along South Promenade, East Arcade & North Guest Corridor
            float[] southBeamXs = { -4.2f, -1.0f, 3.6f };
            for (int b = 0; b < southBeamXs.Length; b++)
            {
                EnsureHotelBox(eastArcade, $"NorthWing_SouthPromenade_Beam_{b}",
                    new Vector3(southBeamXs[b], CeilingY - 0.16f, 16.5f), Quaternion.identity,
                    new Vector3(0.28f, 0.32f, 3.20f), mats.ColumnConcrete, keepCollider: false);
            }
            float[] eastBeamZs = { 15.0f, 18.0f, 22.0f, 26.0f };
            for (int b = 0; b < eastBeamZs.Length; b++)
            {
                EnsureHotelBox(eastArcade, $"NorthWing_EastArcade_Beam_{b}",
                    new Vector3(6.60f, CeilingY - 0.16f, eastBeamZs[b]), Quaternion.identity,
                    new Vector3(3.20f, 0.32f, 0.28f), mats.ColumnConcrete, keepCollider: false);
            }
            float[] northCorrBeamXs = { 8.2f, 3.2f, -3.6f, -8.6f };
            for (int b = 0; b < northCorrBeamXs.Length; b++)
            {
                EnsureHotelBox(eastArcade, $"NorthWing_NorthCorridor_Beam_{b}",
                    new Vector3(northCorrBeamXs[b], CeilingY - 0.16f, 27.4f), Quaternion.identity,
                    new Vector3(0.28f, 0.32f, 2.80f), mats.ColumnConcrete, keepCollider: false);
            }
            // Deliberate architectural overlook return at NE corridor end (X = +13.6..+16.0, Z = +26.0..+28.8)
            EnsureHotelWallSegment(eastArcade, "NorthWing_NorthCorridor_EastOverlookNib",
                new Vector2(13.6f, 26.0f), new Vector2(13.6f, 27.0f), 0.24f, mats.ColumnConcrete, mats.BaseboardConcrete);

            // =========================================================================
            // 5. WEST ARCADE & SERVICE-TO-PUBLIC BULKHEAD SEPARATION (X = -8.6..-5.0)
            //    Dual 2.6m framed bulkheads at X = -8.6 (Z = 15.2..17.8 & Z = 26.1..28.7)
            // =========================================================================
            Transform westArcade = EnsureHotelSubgroup(m1Root, "NorthWing_WestArcade");

            EnsureHotelWallSegment(westArcade, "NorthWing_WestArcade_ServiceDivider_SouthStub",
                new Vector2(-8.6f, 12.0f), new Vector2(-8.6f, 15.2f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(westArcade, "NorthWing_WestArcade_ServiceDivider_Mid",
                new Vector2(-8.6f, 17.8f), new Vector2(-8.6f, 26.1f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            // Recessed architectural pilaster pair breaking flatness along the West Arcade wall
            EnsureHotelBox(westArcade, "NorthWing_WestArcade_PilasterS",
                new Vector3(-8.40f, BaseY + ClearHeight * 0.5f, 20.2f), Quaternion.identity,
                new Vector3(0.24f, ClearHeight, 0.34f), mats.ColumnConcrete, keepCollider: true);
            EnsureHotelBox(westArcade, "NorthWing_WestArcade_PilasterN",
                new Vector3(-8.40f, BaseY + ClearHeight * 0.5f, 23.8f), Quaternion.identity,
                new Vector3(0.24f, ClearHeight, 0.34f), mats.ColumnConcrete, keepCollider: true);

            // South Service Bulkhead Frame (X = -8.6, Z = +15.2..+17.8 -> 2.60m clear width)
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadS_Transom",
                new Vector3(-8.6f, CeilingY - 0.28f, 16.5f), Quaternion.identity,
                new Vector3(0.28f, 0.56f, 2.64f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadS_JambS",
                new Vector3(-8.6f, BaseY + 1.15f, 15.24f), Quaternion.identity,
                new Vector3(0.30f, 2.30f, 0.055f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadS_JambN",
                new Vector3(-8.6f, BaseY + 1.15f, 17.76f), Quaternion.identity,
                new Vector3(0.30f, 2.30f, 0.055f), mats.IndustrialMetal, keepCollider: false);

            // North Service Bulkhead Frame (X = -8.6, Z = +26.1..+28.7 -> 2.60m clear width)
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadN_Transom",
                new Vector3(-8.6f, CeilingY - 0.28f, 27.4f), Quaternion.identity,
                new Vector3(0.28f, 0.56f, 2.64f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadN_JambS",
                new Vector3(-8.6f, BaseY + 1.15f, 26.14f), Quaternion.identity,
                new Vector3(0.30f, 2.30f, 0.055f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(westArcade, "NorthWing_ServiceBulkheadN_JambN",
                new Vector3(-8.6f, BaseY + 1.15f, 28.66f), Quaternion.identity,
                new Vector3(0.30f, 2.30f, 0.055f), mats.IndustrialMetal, keepCollider: false);

            float[] westBeamZs = { 18.0f, 22.0f, 26.0f };
            for (int b = 0; b < westBeamZs.Length; b++)
            {
                EnsureHotelBox(westArcade, $"NorthWing_WestArcade_Beam_{b}",
                    new Vector3(-6.80f, CeilingY - 0.16f, westBeamZs[b]), Quaternion.identity,
                    new Vector3(3.60f, 0.32f, 0.28f), mats.ColumnConcrete, keepCollider: false);
            }

            // =========================================================================
            // 6. FOUR ADDITIONAL EMPTY GUEST-ROOM SHELLS (ROOMS 107-110, ZERO FURNITURE!)
            //    Row Z = +28.8..+34.0 (5.2m deep) along North Guest Corridor (Z = +26.0..+28.8)
            //    Modest per-room variations + interconnecting suite opening (Room 109 <-> 110)
            // =========================================================================
            Transform guestRooms = EnsureHotelSubgroup(m1Root, "NorthWing_GuestRooms_107_110");

            // --- GUEST ROOM 107 (NE Corner Suite: X = +9.6..+16.0, Z = +28.8..+34.0) ---
            Transform r107 = EnsureHotelSubgroup(guestRooms, "Hotel_GuestRoom_107");
            EnsureHotelDoorwayWallX(r107, "Hotel_Room107_CorridorWall", z: 28.8f, minX: 9.6f, maxX: 16.0f, doorMinX: 10.6f, doorMaxX: 12.0f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[6], plaqueX: 12.35f, corridorOnSouthSide: true);
            EnsureHotelWallSegment(r107, "Hotel_Room107_WestPartyWall", new Vector2(9.6f, 28.8f), new Vector2(9.6f, 34.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // Unfinished East bathroom shell (opening facing West at Z = 29.6..30.7)
            EnsureHotelWallSegment(r107, "Hotel_Room107_BathWallN", new Vector2(13.6f, 31.6f), new Vector2(16.0f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r107, "Hotel_Room107_BathWallW", new Vector2(13.6f, 30.7f), new Vector2(13.6f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r107, "Hotel_Room107_BathWallWS", new Vector2(13.6f, 28.8f), new Vector2(13.6f, 29.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 108 (North-East-Center Room: X = +3.2..+9.6, Z = +28.8..+34.0) ---
            Transform r108 = EnsureHotelSubgroup(guestRooms, "Hotel_GuestRoom_108");
            EnsureHotelDoorwayWallX(r108, "Hotel_Room108_CorridorWall", z: 28.8f, minX: 3.2f, maxX: 9.6f, doorMinX: 6.8f, doorMaxX: 8.2f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[7], plaqueX: 6.45f, corridorOnSouthSide: true);
            EnsureHotelWallSegment(r108, "Hotel_Room108_WestPartyWall", new Vector2(3.2f, 28.8f), new Vector2(3.2f, 34.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            // West bathroom bay + entry foyer sightline nib
            EnsureHotelWallSegment(r108, "Hotel_Room108_BathWallN", new Vector2(3.2f, 31.6f), new Vector2(5.4f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r108, "Hotel_Room108_BathWallE", new Vector2(5.4f, 28.8f), new Vector2(5.4f, 29.8f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r108, "Hotel_Room108_BathWallEN", new Vector2(5.4f, 30.9f), new Vector2(5.4f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r108, "Hotel_Room108_FoyerNib", new Vector2(6.15f, 28.8f), new Vector2(6.15f, 30.4f), 0.16f, mats.WallPlaster, mats.BaseboardConcrete);

            // --- GUEST ROOM 109 (North-West-Center Interconnecting Suite: X = -3.6..+3.2, Z = +28.8..+34.0) ---
            Transform r109 = EnsureHotelSubgroup(guestRooms, "Hotel_GuestRoom_109");
            EnsureHotelDoorwayWallX(r109, "Hotel_Room109_CorridorWall", z: 28.8f, minX: -3.6f, maxX: 3.2f, doorMinX: -2.6f, doorMaxX: -1.2f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[8], plaqueX: -0.85f, corridorOnSouthSide: true);
            // Interconnecting framed doorway to Room 110 (X = -3.6, Z = 30.0..31.3 -> 1.30m wide escape connector)
            EnsureHotelDoorwayWallZ(r109, "Hotel_Room109_InterconnectWallW", x: -3.6f, minZ: 28.8f, maxZ: 34.0f, doorMinZ: 30.0f, doorMaxZ: 31.3f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal);
            // NE wet-wall partition inside Room 109 (opening facing West at Z = 31.8..33.0)
            EnsureHotelWallSegment(r109, "Hotel_Room109_BathWallS", new Vector2(0.8f, 31.6f), new Vector2(3.2f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r109, "Hotel_Room109_BathWallWN", new Vector2(0.8f, 33.0f), new Vector2(0.8f, 34.0f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // --- GUEST ROOM 110 (North-West Raw Construction Suite: X = -9.6..-3.6, Z = +28.8..+34.0) ---
            Transform r110 = EnsureHotelSubgroup(guestRooms, "Hotel_GuestRoom_110");
            EnsureHotelDoorwayWallX(r110, "Hotel_Room110_CorridorWall", z: 28.8f, minX: -9.6f, maxX: -3.6f, doorMinX: -6.8f, doorMaxX: -5.4f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: mats.RoomPlaqueMats[9], plaqueX: -5.05f, corridorOnSouthSide: true);
            EnsureHotelWallSegment(r110, "Hotel_Room110_WestServiceWall", new Vector2(-9.6f, 28.8f), new Vector2(-9.6f, 34.0f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            // NW corner plumbing chase partition (opening facing South at X = -9.1..-8.0)
            EnsureHotelWallSegment(r110, "Hotel_Room110_ChaseWallE", new Vector2(-7.8f, 31.6f), new Vector2(-7.8f, 34.0f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);
            EnsureHotelWallSegment(r110, "Hotel_Room110_ChaseWallSW", new Vector2(-9.6f, 31.6f), new Vector2(-9.1f, 31.6f), 0.16f, mats.WallConcrete, mats.BaseboardConcrete);

            // =========================================================================
            // 7. UNFINISHED BANQUET / DINING HALL SHELL (X = +8.2..+16.0, Z = +12.0..+26.0)
            //    7.8m x 14.0m (109.2 m²) open public hall with wide concrete transfer beams,
            //    dual arcade portals (Z=15.4..18.2 & 22.4..25.0), North corridor door, SE nib,
            //    and Sealed Future East Expansion Anchor — ZERO furniture!
            // =========================================================================
            Transform banquet = EnsureHotelSubgroup(m1Root, "NorthWing_BanquetHall");

            // West dividing wall separating Banquet Hall from East Arcade (2 wide framed portals)
            EnsureHotelWallSegment(banquet, "NorthWing_Banquet_WestWallS", new Vector2(8.2f, 12.0f), new Vector2(8.2f, 15.4f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(banquet, "NorthWing_Banquet_WestWallMid", new Vector2(8.2f, 18.2f), new Vector2(8.2f, 22.4f), wallT, mats.WallPlaster, mats.BaseboardConcrete);
            EnsureHotelWallSegment(banquet, "NorthWing_Banquet_WestWallN", new Vector2(8.2f, 25.0f), new Vector2(8.2f, 26.0f), wallT, mats.WallPlaster, mats.BaseboardConcrete);

            // South Banquet Portal Frame (X = +8.2, Z = 15.4..18.2 -> 2.80m wide)
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalS_Transom", new Vector3(8.2f, CeilingY - 0.225f, 16.8f), Quaternion.identity, new Vector3(0.26f, 0.45f, 2.86f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalS_JambS", new Vector3(8.2f, BaseY + 1.20f, 15.44f), Quaternion.identity, new Vector3(0.28f, 2.40f, 0.055f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalS_JambN", new Vector3(8.2f, BaseY + 1.20f, 18.16f), Quaternion.identity, new Vector3(0.28f, 2.40f, 0.055f), mats.IndustrialMetal, keepCollider: false);

            // North Banquet Portal Frame (X = +8.2, Z = 22.4..25.0 -> 2.60m wide)
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalN_Transom", new Vector3(8.2f, CeilingY - 0.225f, 23.7f), Quaternion.identity, new Vector3(0.26f, 0.45f, 2.66f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalN_JambS", new Vector3(8.2f, BaseY + 1.20f, 22.44f), Quaternion.identity, new Vector3(0.28f, 2.40f, 0.055f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_Banquet_PortalN_JambN", new Vector3(8.2f, BaseY + 1.20f, 24.96f), Quaternion.identity, new Vector3(0.28f, 2.40f, 0.055f), mats.IndustrialMetal, keepCollider: false);

            // North dividing wall separating Banquet Hall from North Guest Corridor (with 2.4m connector door X = +10.4..+12.8)
            EnsureHotelDoorwayWallX(banquet, "NorthWing_Banquet_NorthWall", z: 26.0f, minX: 8.2f, maxX: 16.0f, doorMinX: 10.4f, doorMaxX: 12.8f,
                thickness: wallT, wallMat: mats.WallPlaster, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: null, plaqueX: 0f, corridorOnSouthSide: false);

            // Wider structural spans: 3 deep concrete ceiling transfer beams & engaged East wall pilasters
            float[] banquetBeamZs = { 15.6f, 19.0f, 22.4f };
            for (int b = 0; b < banquetBeamZs.Length; b++)
            {
                EnsureHotelBox(banquet, $"NorthWing_Banquet_TransferBeam_{b}",
                    new Vector3(12.10f, CeilingY - 0.22f, banquetBeamZs[b]), Quaternion.identity,
                    new Vector3(7.60f, 0.44f, 0.34f), mats.ColumnConcrete, keepCollider: false);
                EnsureHotelBox(banquet, $"NorthWing_Banquet_EastPilaster_{b}",
                    new Vector3(15.74f, BaseY + ClearHeight * 0.5f, banquetBeamZs[b]), Quaternion.identity,
                    new Vector3(0.28f, ClearHeight, 0.38f), mats.ColumnConcrete, keepCollider: true);
            }

            // SE Service-Pass Architectural Nib Wall (pure unfinished partition, zero furniture)
            EnsureHotelWallSegment(banquet, "NorthWing_Banquet_ServicePassNib",
                new Vector2(12.4f, 12.0f), new Vector2(12.4f, 14.4f), 0.18f, mats.WallConcrete, mats.BaseboardConcrete);

            // Sealed Future East Expansion Anchor Frame (pure recessed concrete relief on solid East perimeter, zero fake door)
            EnsureHotelBox(banquet, "NorthWing_FutureAnchor_EastFrameL", new Vector3(15.84f, BaseY + 1.25f, 17.5f), Quaternion.identity, new Vector3(0.08f, 2.50f, 0.18f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_FutureAnchor_EastFrameR", new Vector3(15.84f, BaseY + 1.25f, 20.5f), Quaternion.identity, new Vector3(0.08f, 2.50f, 0.18f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(banquet, "NorthWing_FutureAnchor_EastFrameTop", new Vector3(15.84f, BaseY + 2.46f, 19.0f), Quaternion.identity, new Vector3(0.08f, 0.18f, 3.18f), mats.ColumnConcrete, keepCollider: false);

            // =========================================================================
            // 8. HOUSEKEEPING / SERVICE CORE (X = -16.0..-8.6, Z = +12.0..+34.0)
            //    Maintenance Access Corridor (X = -12.0..-8.6) fed by Connection B,
            //    3 back-of-house room shells (Storage, Utility, Linen — Utility <-> Linen dual-door!),
            //    NW Future Stairwell Reserve Landing (X = -16.0..-9.6, Z = +28.8..+34.0),
            //    and Sealed Future North & West Expansion Anchors.
            // =========================================================================
            Transform housekeeping = EnsureHotelSubgroup(m1Root, "NorthWing_HousekeepingCore");

            // West corridor wall along X = -12.0 dividing Maintenance Corridor from the 3 back-of-house shells
            EnsureHotelDoorwayWallZ(housekeeping, "NorthWing_Housekeeping_StorageWallE", x: -12.0f, minZ: 12.0f, maxZ: 18.0f, doorMinZ: 14.2f, doorMaxZ: 15.6f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal);
            EnsureHotelDoorwayWallZ(housekeeping, "NorthWing_Housekeeping_UtilityWallE", x: -12.0f, minZ: 18.0f, maxZ: 23.4f, doorMinZ: 19.8f, doorMaxZ: 21.2f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal);
            EnsureHotelDoorwayWallZ(housekeeping, "NorthWing_Housekeeping_LinenWallE", x: -12.0f, minZ: 23.4f, maxZ: 28.8f, doorMinZ: 25.4f, doorMaxZ: 26.8f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal);

            // Transverse partitions separating Storage (12..18), Utility (18..23.4), Linen (23.4..28.8) & NW Landing (28.8..34)
            EnsureHotelWallSegment(housekeeping, "NorthWing_Housekeeping_DividerSouth",
                new Vector2(-16.0f, 18.0f), new Vector2(-12.0f, 18.0f), wallT, mats.WallConcrete, mats.BaseboardConcrete);
            // Internal dual-door bypass between Utility & Linen rooms (Z = 23.4, X = -14.8..-13.4)
            EnsureHotelDoorwayWallX(housekeeping, "NorthWing_Housekeeping_UtilityLinenPass", z: 23.4f, minX: -16.0f, maxX: -12.0f, doorMinX: -14.8f, doorMaxX: -13.4f,
                thickness: wallT, wallMat: mats.WallConcrete, baseMat: mats.BaseboardConcrete, trimMat: mats.IndustrialMetal,
                plaqueMat: null, plaqueX: 0f, corridorOnSouthSide: false);
            EnsureHotelWallSegment(housekeeping, "NorthWing_Housekeeping_LinenNorthWall",
                new Vector2(-16.0f, 28.8f), new Vector2(-12.0f, 28.8f), wallT, mats.WallConcrete, mats.BaseboardConcrete);

            // Framed 2.4m portal opening straight North from Maintenance Corridor into NW Future Stairwell Landing (X = -12.0..-9.6, Z = 28.8)
            EnsureHotelBox(housekeeping, "NorthWing_NWLanding_PortalTransom",
                new Vector3(-10.8f, CeilingY - 0.28f, 28.8f), Quaternion.identity,
                new Vector3(2.44f, 0.56f, 0.28f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_NWLanding_PortalTrimW",
                new Vector3(-11.96f, BaseY + 1.15f, 28.8f), Quaternion.identity,
                new Vector3(0.055f, 2.30f, 0.30f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_NWLanding_PortalTrimE",
                new Vector3(-9.64f, BaseY + 1.15f, 28.8f), Quaternion.identity,
                new Vector3(0.055f, 2.30f, 0.30f), mats.IndustrialMetal, keepCollider: false);

            // Sealed Future North Expansion Anchor (Z = +34.0 inside NW Landing, X = -14.2..-11.4)
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_NorthFrameW", new Vector3(-14.2f, BaseY + 1.25f, 33.84f), Quaternion.identity, new Vector3(0.18f, 2.50f, 0.08f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_NorthFrameE", new Vector3(-11.4f, BaseY + 1.25f, 33.84f), Quaternion.identity, new Vector3(0.18f, 2.50f, 0.08f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_NorthFrameTop", new Vector3(-12.8f, BaseY + 2.46f, 33.84f), Quaternion.identity, new Vector3(2.98f, 0.18f, 0.08f), mats.ColumnConcrete, keepCollider: false);

            // Sealed Future West Expansion Anchor (X = -16.0 inside Utility Room, Z = +19.4..+22.0)
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_WestFrameS", new Vector3(-15.84f, BaseY + 1.25f, 19.4f), Quaternion.identity, new Vector3(0.08f, 2.50f, 0.18f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_WestFrameN", new Vector3(-15.84f, BaseY + 1.25f, 22.0f), Quaternion.identity, new Vector3(0.08f, 2.50f, 0.18f), mats.ColumnConcrete, keepCollider: false);
            EnsureHotelBox(housekeeping, "NorthWing_FutureAnchor_WestFrameTop", new Vector3(-15.84f, BaseY + 2.46f, 20.7f), Quaternion.identity, new Vector3(0.08f, 0.18f, 2.78f), mats.ColumnConcrete, keepCollider: false);

            // =========================================================================
            // 9. SELECTIVE CONSTRUCTION DETAILS (NON-COLLIDING, ZERO NEW POINT LIGHTS!)
            //    Emissive-only industrial ceiling fixtures matching hotel style, exposed
            //    conduit trays, plumbing rough-ins, room finishes & expansion joints.
            // =========================================================================
            Transform details = EnsureHotelSubgroup(m1Root, "NorthWing_ConstructionDetails");

            // 10 emissive-only industrial ceiling fixtures (ZERO Light components -> preserves exact 18-light VRAM budget!)
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_SouthPromE",   new Vector3(  2.0f, CeilingY - 0.07f, 16.5f), Quaternion.identity,                lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_SouthPromW",   new Vector3( -5.4f, CeilingY - 0.07f, 16.5f), Quaternion.identity,                lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_EastArcadeS",  new Vector3(  6.6f, CeilingY - 0.07f, 19.8f), Quaternion.Euler(0f, 90f, 0f),      lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_EastArcadeN",  new Vector3(  6.6f, CeilingY - 0.07f, 24.2f), Quaternion.Euler(0f, 90f, 0f),      lit: false, warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_WestArcadeS",  new Vector3( -6.8f, CeilingY - 0.07f, 19.8f), Quaternion.Euler(0f, 90f, 0f),      lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_WestArcadeN",  new Vector3( -6.8f, CeilingY - 0.07f, 24.2f), Quaternion.Euler(0f, 90f, 0f),      lit: true,  warm: true,  mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_NorthCorrE",   new Vector3(  6.4f, CeilingY - 0.07f, 27.4f), Quaternion.identity,                lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_NorthCorrW",   new Vector3( -2.4f, CeilingY - 0.07f, 27.4f), Quaternion.identity,                lit: true,  warm: false, mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_BanquetCenter",new Vector3( 12.1f, CeilingY - 0.07f, 20.6f), Quaternion.Euler(0f, 90f, 0f),      lit: true,  warm: true,  mats);
            EnsureNorthWingEmissiveFixture(details, "NorthWing_FixtureStrip_ServiceCorr",  new Vector3(-10.3f, CeilingY - 0.07f, 21.0f), Quaternion.Euler(0f, 90f, 0f),      lit: true,  warm: true,  mats);

            // Overhead galvanized cable tray running along Maintenance Access Corridor & North Guest Corridor
            EnsureHotelBox(details, "NorthWing_Infra_ServiceCableTray",
                new Vector3(-9.15f, CeilingY - 0.14f, 20.4f), Quaternion.identity,
                new Vector3(0.24f, 0.04f, 16.4f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_Infra_NorthCorrConduit",
                new Vector3(2.8f, CeilingY - 0.12f, 28.56f), Quaternion.identity,
                new Vector3(22.0f, 0.04f, 0.04f), mats.IndustrialMetal, keepCollider: false);

            // Selective plumbing rough-ins & electrical panels (true cylindrical pipes, clamps, flanges & detailed breaker box)
            Mesh pipeCylMesh = EnsureEcologyMeshAsset(PipeCylinderMeshPath, BuildProceduralPipeCylinderMesh);

            // Detailed Industrial Breaker Panel Box on West face (-8.71) of Maintenance Corridor divider wall
            EnsureHotelBox(details, "NorthWing_Infra_ServicePanelBox",
                new Vector3(-8.745f, BaseY + 1.48f, 21.60f), Quaternion.identity,
                new Vector3(0.07f, 0.64f, 0.46f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_Infra_ServicePanelDoor",
                new Vector3(-8.784f, BaseY + 1.48f, 21.60f), Quaternion.identity,
                new Vector3(0.014f, 0.56f, 0.38f), mats.ElevatorDoorMetal, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_Infra_ServicePanelLatch",
                new Vector3(-8.796f, BaseY + 1.48f, 21.44f), Quaternion.identity,
                new Vector3(0.016f, 0.09f, 0.024f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_Infra_ServicePanelDirectoryCard",
                new Vector3(-8.793f, BaseY + 1.64f, 21.64f), Quaternion.identity,
                new Vector3(0.005f, 0.14f, 0.18f), mats.AgedCeramicPatch, keepCollider: false);
            // Cylindrical galvanized conduits entering top of panel from cable tray + sheared lower conduit stub
            EnsureOrganicMeshObject(details, "NorthWing_Infra_ServicePanelConduitTopA", pipeCylMesh, mats.ElevatorDoorMetal,
                new Vector3(-8.75f, BaseY + 2.30f, 21.48f), Quaternion.identity, new Vector3(0.036f, 1.02f, 0.036f));
            EnsureOrganicMeshObject(details, "NorthWing_Infra_ServicePanelConduitTopB", pipeCylMesh, mats.IndustrialMetal,
                new Vector3(-8.75f, BaseY + 2.30f, 21.70f), Quaternion.identity, new Vector3(0.030f, 1.02f, 0.030f));
            EnsureOrganicMeshObject(details, "NorthWing_Infra_ServicePanelConduitDrop", pipeCylMesh, mats.IndustrialMetal,
                new Vector3(-8.75f, BaseY + 0.92f, 21.62f), Quaternion.identity, new Vector3(0.032f, 0.48f, 0.032f));

            // Cylindrical Utility Room Pipe Risers A (oxidized copper) & B (galvanized steel) + flanges, wall clamps & valve
            EnsureDetailedPipeRiser(details, "NorthWing_Infra_UtilityPipeRiserA", pipeCylMesh,
                new Vector3(-15.72f, BaseY, 18.45f), ClearHeight, 0.082f, mats.PipeCopperRust, mats.IndustrialMetal, wallNormalDirX: -1f);
            EnsureDetailedPipeRiser(details, "NorthWing_Infra_UtilityPipeRiserB", pipeCylMesh,
                new Vector3(-15.72f, BaseY, 18.72f), ClearHeight, 0.064f, mats.ElevatorDoorMetal, mats.IndustrialMetal, wallNormalDirX: -1f);
            // Valve wheel & T-junction on Copper Riser A
            EnsureOrganicMeshObject(details, "NorthWing_Infra_UtilityValveStem", pipeCylMesh, mats.IndustrialMetal,
                new Vector3(-15.65f, BaseY + 1.12f, 18.45f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.028f, 0.12f, 0.028f));
            EnsureOrganicMeshObject(details, "NorthWing_Infra_UtilityValveWheel", pipeCylMesh, mats.PipeCopperRust,
                new Vector3(-15.58f, BaseY + 1.12f, 18.45f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.15f, 0.018f, 0.15f));

            EnsureDetailedPipeRiser(details, "NorthWing_Infra_BanquetServiceRiser", pipeCylMesh,
                new Vector3(15.72f, BaseY, 12.60f), ClearHeight, 0.070f, mats.PipeCopperRust, mats.IndustrialMetal, wallNormalDirX: 1f);
            EnsureHotelBox(details, "NorthWing_Infra_BanquetCeramicSplash",
                new Vector3(15.84f, BaseY + 0.92f, 13.50f), Quaternion.identity,
                new Vector3(0.025f, 1.10f, 2.20f), mats.AgedCeramicPatch, keepCollider: false);

            // Selective per-room construction variations (107-110, zero furniture)
            EnsureOrganicMeshObject(details, "NorthWing_Room107_BathPipeStub", pipeCylMesh, mats.PipeCopperRust,
                new Vector3(15.72f, BaseY + 0.68f, 30.80f), Quaternion.identity, new Vector3(0.055f, 1.36f, 0.055f));
            EnsureOrganicMeshObject(details, "NorthWing_Room107_BathDrain", pipeCylMesh, mats.IndustrialMetal,
                new Vector3(14.80f, BaseY + 0.004f, 30.20f), Quaternion.identity, new Vector3(0.18f, 0.008f, 0.18f));
            EnsureHotelBox(details, "NorthWing_Room108_BathCeramicStrip",
                new Vector3(3.34f, BaseY + 0.62f, 30.20f), Quaternion.identity,
                new Vector3(0.025f, 0.92f, 2.30f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_Room108_LeaningWoodDoorLeaf",
                new Vector3(6.34f, BaseY + 1.02f, 29.65f), Quaternion.Euler(0f, 0f, -7f),
                new Vector3(0.045f, 2.04f, 0.88f), mats.UnfinishedWoodDoor, keepCollider: false);
            EnsureExposedDoorHinges(details, "NorthWing_Room110_EmptyHinge",
                new Vector3(-6.76f, BaseY, 28.88f), mats.IndustrialMetal);
            EnsureDetailedPipeRiser(details, "NorthWing_Room110_ChaseRiser", pipeCylMesh,
                new Vector3(-9.36f, BaseY, 33.62f), 1.85f, 0.062f, mats.PipeCopperRust, mats.IndustrialMetal, wallNormalDirX: -1f);

            // Flush architectural floor expansion-joint strips
            EnsureHotelBox(details, "NorthWing_FloorJoint_SouthProm",
                new Vector3(0.0f, BaseY + 0.003f, 15.0f), Quaternion.identity,
                new Vector3(16.2f, 0.006f, 0.05f), mats.IndustrialMetal, keepCollider: false);
            EnsureHotelBox(details, "NorthWing_FloorJoint_NorthCorr",
                new Vector3(2.0f, BaseY + 0.003f, 26.0f), Quaternion.identity,
                new Vector3(16.0f, 0.006f, 0.05f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void EnsureHotelDoorwayWallZ(
            Transform parent, string prefix, float x, float minZ, float maxZ, float doorMinZ, float doorMaxZ,
            float thickness, Material wallMat, Material baseMat, Material trimMat)
        {
            if (doorMinZ - minZ > 0.05f)
            {
                EnsureHotelWallSegment(parent, $"{prefix}_South", new Vector2(x, minZ), new Vector2(x, doorMinZ), thickness, wallMat, baseMat);
            }
            if (maxZ - doorMaxZ > 0.05f)
            {
                EnsureHotelWallSegment(parent, $"{prefix}_North", new Vector2(x, doorMaxZ), new Vector2(x, maxZ), thickness, wallMat, baseMat);
            }

            float doorWidth = doorMaxZ - doorMinZ;
            float doorCenterZ = 0.5f * (doorMinZ + doorMaxZ);
            const float doorHeight = 2.20f;
            float transomH = ClearHeight - doorHeight;

            EnsureHotelBox(parent, $"{prefix}_Transom",
                new Vector3(x, BaseY + doorHeight + transomH * 0.5f, doorCenterZ), Quaternion.identity,
                new Vector3(thickness, transomH, doorWidth + 0.04f), wallMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_JambS",
                new Vector3(x, BaseY + doorHeight * 0.5f, doorMinZ + 0.02f), Quaternion.identity,
                new Vector3(thickness + 0.05f, doorHeight, 0.055f), trimMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_JambN",
                new Vector3(x, BaseY + doorHeight * 0.5f, doorMaxZ - 0.02f), Quaternion.identity,
                new Vector3(thickness + 0.05f, doorHeight, 0.055f), trimMat, keepCollider: false);
            EnsureHotelBox(parent, $"{prefix}_HeaderTrim",
                new Vector3(x, BaseY + doorHeight, doorCenterZ), Quaternion.identity,
                new Vector3(thickness + 0.05f, 0.055f, doorWidth + 0.08f), trimMat, keepCollider: false);
        }

        private static void EnsureNorthWingEmissiveFixture(
            Transform parent, string name, Vector3 pos, Quaternion rot, bool lit, bool warm, RemanenciaVisualMaterials mats)
        {
            Material tubeMat = !lit ? mats.FluorOff : (warm ? mats.UtilityWarmOn : mats.FluorOn);
            EnsureHotelBox(parent, name, pos, rot, new Vector3(1.36f, 0.046f, 0.068f), tubeMat, keepCollider: false);
            EnsureHotelBox(parent, name + "_Housing", pos + new Vector3(0f, 0.042f, 0f), rot, new Vector3(1.44f, 0.040f, 0.115f), mats.IndustrialMetal, keepCollider: false);
        }

        /// <summary>
        /// Targeted, idempotent Implementation 07.6 environmental narrative layer migration utility.
        /// Opens the existing BackroomsLevel1Scene.unity without rebuilding the scene or modifying any
        /// architectural root, elevator bay, existing artifact, light, or progression schema.
        /// Creates/updates strictly Hotel_Narrative_Evidence (4 inspectable bilingual documents +
        /// restrained non-colliding anomalous surface traces) directly under BackroomsLevel1_Root.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Hotel Narrative Layer (In-Place)")]
        public static void MigrateRemanenciaHotelNarrativeAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaHotelNarrativeInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Hotel narrative layer (4 discoveries + subtle anomalies) migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaHotelNarrativeInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia hotel narrative migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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
            if (shellRoot == null || shellRoot.Find(NorthWingM1RootName) == null)
            {
                throw new InvalidOperationException($"Expected '{NorthWingM1RootName}' under Backrooms_Architecture before running 07.6 narrative migration.");
            }

            RemanenciaVisualMaterials mats = EnsureRemanenciaVisualAssets();
            BuildOrUpdateHotelNarrativeLayer(root.transform, mats);
        }

        private static void BuildOrUpdateHotelNarrativeLayer(Transform backroomsRoot, RemanenciaVisualMaterials mats)
        {
            // Generate modest target-specific procedural textures & matte materials for the 4 documents + subtle anomalies
            string planTex = EnsureEvidenceProjectPlanTexture("Assets/Textures/Environment/T_Remanencia_Evidence_ProjectPlan.png");
            string regTex = EnsureEvidenceRoomRegisterTexture("Assets/Textures/Environment/T_Remanencia_Evidence_RoomRegister.png");
            string maintTex = EnsureEvidenceMaintenanceLogTexture("Assets/Textures/Environment/T_Remanencia_Evidence_MaintenanceLog.png");
            string rumorTex = EnsureEvidenceCobaltRumorNoteTexture("Assets/Textures/Environment/T_Remanencia_Evidence_CobaltRumorNote.png");
            string mineralDecalTex = EnsureFeatheredMineralCrackDecalTexture("Assets/Textures/Environment/T_Remanencia_Anomaly_MineralJoint.png");
            string stainDecalTex = EnsureFeatheredDampSeepDecalTexture("Assets/Textures/Environment/T_Remanencia_Anomaly_SurfaceStain.png");
            AssetDatabase.Refresh();

            ConfigureSignTextureImporter(planTex);
            ConfigureSignTextureImporter(regTex);
            ConfigureSignTextureImporter(maintTex);
            ConfigureSignTextureImporter(rumorTex);
            ConfigureAlphaDecalTextureImporter(mineralDecalTex);
            ConfigureAlphaDecalTextureImporter(stainDecalTex);

            Material planMat = CreateTexturedMat(EvidencePlanMatPath, planTex, Color.white, 0.14f, Vector2.one);
            Material regMat = CreateTexturedMat(EvidenceRegisterMatPath, regTex, Color.white, 0.12f, Vector2.one);
            Material maintMat = CreateTexturedMat(EvidenceMaintenanceMatPath, maintTex, Color.white, 0.12f, Vector2.one);
            Material rumorMat = CreateTexturedMat(EvidenceCobaltNoteMatPath, rumorTex, Color.white, 0.10f, Vector2.one);

            // Conformal feathered alpha-blended concrete crack/efflorescence & damp seepage decals (ZERO rectangular edges, ZERO glow)
            Material mineralJointMat = CreateAlphaBlendedDecalMat(AnomalyMineralJointMatPath, mineralDecalTex, Color.white, 0.08f, renderQueue: 3002);
            Material surfaceStainMat = CreateAlphaBlendedDecalMat(AnomalySurfaceStainMatPath, stainDecalTex, Color.white, 0.18f, renderQueue: 3001);
            Mesh decalQuad = EnsureEcologyMeshAsset(DecalQuadMeshPath, BuildProceduralDecalQuadMesh);

            Transform narrativeRoot = EnsureHotelSubgroup(backroomsRoot, HotelNarrativeEvidenceRootName);

            // =========================================================================
            // 1. EVIDENCE 01 — THE UNFINISHED PROJECT PLAN (Reception North Wall)
            //    Mounted flush on Reception Alcove back wall (facing South -Z, upright +Z)
            // =========================================================================
            GameObject evPlan = EnsureInspectableEvidenceSheet(
                narrativeRoot,
                "Hotel_Evidence_UnfinishedProjectPlan",
                new Vector3(8.85f, BaseY + 1.48f, 11.83f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.58f, 0.42f, 0.014f),
                planMat,
                ProgressionIds.ClueRemanenciaHotelPlan01,
                "Plano de Obra Inconclusa — Nivel de Acceso",
                "Unfinished Project Plan — Ground Level",
                "Pliego de obra pegado al muro sin firma ni fecha definitiva. Muestra el vestíbulo y el patio norte, pero traza un pasillo adicional y una habitación donde hoy existe un muro macizo de concreto.",
                "Unsigned, undated construction sheet taped to the wall. It charts the lobby and north courtyard, yet draws an extra corridor and room where a solid concrete structural wall stands today.",
                new Color(0.68f, 0.82f, 0.90f));
            EnsureHotelBox(evPlan.transform, "TapeStrip_TL", new Vector3( 8.60f, BaseY + 1.68f, 11.822f), Quaternion.Euler(0f, 0f, -12f), new Vector3(0.06f, 0.022f, 0.005f), mats.AgedCeramicPatch, keepCollider: false);
            EnsureHotelBox(evPlan.transform, "TapeStrip_TR", new Vector3( 9.10f, BaseY + 1.68f, 11.822f), Quaternion.Euler(0f, 0f,  10f), new Vector3(0.06f, 0.022f, 0.005f), mats.AgedCeramicPatch, keepCollider: false);

            // =========================================================================
            // 2. EVIDENCE 02 — THE ROOM REGISTRATION RECORD (North Corridor, Rooms 109-110)
            //    Mounted on Corridor wall between Room 110 plaque (-5.05) & Room 109 door (-2.6)
            // =========================================================================
            GameObject evReg = EnsureInspectableEvidenceSheet(
                narrativeRoot,
                "Hotel_Evidence_RoomRegistrationRecord",
                new Vector3(-3.85f, BaseY + 1.44f, 28.67f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.34f, 0.44f, 0.014f),
                regMat,
                ProgressionIds.ClueRemanenciaHotelRegister02,
                "Hoja de Registro de Habitaciones (Fragmento)",
                "Room Registration Sheet (Fragment)",
                "Registro arrancado de una bitácora sin encabezado. Anota las habitaciones 107 a 110 como asignadas, junto a un cuarto '111-B' y fechas incompletas de estancias previas a que la obra concluyera.",
                "Torn ledger page with no letterhead. Lists Rooms 107 through 110 as assigned alongside a '111-B' entry, bearing incomplete stay dates recorded before construction was ever finished.",
                new Color(0.88f, 0.82f, 0.68f));
            EnsureHotelBox(evReg.transform, "TapeStrip_Top", new Vector3(-3.85f, BaseY + 1.65f, 28.662f), Quaternion.Euler(0f, 0f, 4f), new Vector3(0.07f, 0.022f, 0.005f), mats.AgedCeramicPatch, keepCollider: false);

            // =========================================================================
            // 3. EVIDENCE 03 — THE MAINTENANCE RECORD (North Wing Maintenance Access Corridor)
            //    Mounted on West-facing divider wall just North of Service Panel Box (-8.74, 21.6)
            // =========================================================================
            GameObject evMaint = EnsureInspectableEvidenceSheet(
                narrativeRoot,
                "Hotel_Evidence_MaintenanceRecord",
                new Vector3(-8.73f, BaseY + 1.46f, 22.45f),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(0.34f, 0.44f, 0.014f),
                maintMat,
                ProgressionIds.ClueRemanenciaHotelMaintenance03,
                "Parte de Mantenimiento — Obra Norte",
                "Maintenance Log — North Wing Works",
                "Hoja de servicio fijada junto al tablero: 'Manchas grisáceas en juntas secas del concreto. Cortes eléctricos sin falla en línea. Dos resanes anotados el día 14 ya estaban hechos antes del daño; pasillo mide 1.4 m más que en plano.'",
                "Service sheet fixed near the panel: 'Greyish stains along dry concrete joints. Power drops with no line fault. Two patches logged on the 14th appeared before the damage; corridor measures 1.4 m longer than drawn.'",
                new Color(0.84f, 0.76f, 0.64f));
            EnsureHotelBox(evMaint.transform, "TapeStrip_Top", new Vector3(-8.738f, BaseY + 1.67f, 22.45f), Quaternion.Euler(0f, -90f, -3f), new Vector3(0.07f, 0.022f, 0.005f), mats.AgedCeramicPatch, keepCollider: false);

            // =========================================================================
            // 4. EVIDENCE 04 — THE COBALT-60 RUMOR NOTE (Housekeeping Utility Room, Back-of-House)
            //    Deep service location away from Entrance & Reception; unsigned folded pencil note
            // =========================================================================
            GameObject evRumor = EnsureInspectableEvidenceSheet(
                narrativeRoot,
                "Hotel_Evidence_Cobalt60RumorNote",
                new Vector3(-12.75f, BaseY + 1.38f, 23.27f),
                Quaternion.Euler(0f, 180f, 3.5f),
                new Vector3(0.26f, 0.21f, 0.010f),
                rumorMat,
                ProgressionIds.ClueRemanenciaHotelCobaltRumor04,
                "Nota Anónima — Rumor de Obra",
                "Anonymous Note — Construction Rumor",
                "Nota doblada a lápiz sin firma: 'Dicen que parte de la varilla llegó contaminada. Algunos mencionan Cobalto-60, pero nadie ha mostrado pruebas. Lo extraño es que las manchas siguen apareciendo incluso donde ya se retiró el material.'",
                "Unsigned folded pencil note: 'Word is some of the rebar arrived contaminated. A few mention Cobalt-60, though nobody has shown proof. The strange part is the stains keep returning even where the material was removed.'",
                new Color(0.86f, 0.74f, 0.56f));
            EnsureHotelBox(evRumor.transform, "PinTab_Top", new Vector3(-12.74f, BaseY + 1.475f, 23.264f), Quaternion.identity, new Vector3(0.032f, 0.018f, 0.005f), mats.IndustrialMetal, keepCollider: false);

            // =========================================================================
            // 5. SUBTLE ANOMALOUS ENVIRONMENTAL TRACES (FEATHERED CONFORMAL DECALS)
            //    Alpha-blended hairline fractures, chalky salt efflorescence & organic damp
            //    seepage stains flush on concrete — zero sharp rectangular boxes, zero glow.
            // =========================================================================
            Transform anomalies = EnsureHotelSubgroup(narrativeRoot, "Hotel_Subtle_Anomalies");

            // Maintenance Corridor West-facing wall (surface X = -8.710, outward normal = -X -> Euler(0, -90, 0))
            EnsureOrganicMeshObject(anomalies, "Anomaly_ServiceJointStainHalo", decalQuad, surfaceStainMat,
                new Vector3(-8.714f, BaseY + 0.72f, 22.08f), Quaternion.Euler(0f, -90f, 0f), new Vector3(1.10f, 1.55f, 1f));
            EnsureOrganicMeshObject(anomalies, "Anomaly_ServiceJointDeposit_A", decalQuad, mineralJointMat,
                new Vector3(-8.715f, BaseY + 0.78f, 22.06f), Quaternion.Euler(0f, -90f, 0f), new Vector3(0.62f, 1.52f, 1f));
            EnsureOrganicMeshObject(anomalies, "Anomaly_ServiceJointDeposit_B", decalQuad, mineralJointMat,
                new Vector3(-8.715f, BaseY + 0.62f, 22.88f), Quaternion.Euler(0f, -90f, 4f), new Vector3(0.56f, 1.24f, 1f));

            // Floor seepage continuing beneath solid concrete service divider wall (X = -8.6, floor Y = -50.000, normal +Y -> Euler(-90, yaw, 0))
            EnsureOrganicMeshObject(anomalies, "Anomaly_ServiceFloorUnderwallSeep_W", decalQuad, surfaceStainMat,
                new Vector3(-9.02f, BaseY + 0.003f, 22.24f), Quaternion.Euler(-90f, 18f, 0f), new Vector3(1.24f, 1.02f, 1f));
            EnsureOrganicMeshObject(anomalies, "Anomaly_ServiceFloorUnderwallSeep_E", decalQuad, surfaceStainMat,
                new Vector3(-8.18f, BaseY + 0.003f, 22.24f), Quaternion.Euler(-90f, -24f, 0f), new Vector3(0.96f, 0.82f, 1f));

            // Utility Room: damp stain below Rumor Note (South-facing Z = +23.290 -> Euler(0, 180, 0)) & mineral crack behind risers (East-facing X = -15.870 -> Euler(0, 90, 0))
            EnsureOrganicMeshObject(anomalies, "Anomaly_UtilityWallDiscoloration_A", decalQuad, surfaceStainMat,
                new Vector3(-12.75f, BaseY + 0.72f, 23.285f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.08f, 0.94f, 1f));
            EnsureOrganicMeshObject(anomalies, "Anomaly_UtilityWallMineralEdge", decalQuad, mineralJointMat,
                new Vector3(-15.865f, BaseY + 0.88f, 18.64f), Quaternion.Euler(0f, 90f, -4f), new Vector3(0.96f, 1.72f, 1f));

            // North Guest Corridor subtle mineral fracture patch below Room Register (South-facing Z = +28.690 -> Euler(0, 180, 0))
            EnsureOrganicMeshObject(anomalies, "Anomaly_NorthCorrConcreteMismatch", decalQuad, mineralJointMat,
                new Vector3(-3.85f, BaseY + 0.68f, 28.686f), Quaternion.Euler(0f, 180f, 5f), new Vector3(0.82f, 1.12f, 1f));

            // Central Courtyard plinth West flank (surface X = -1.100, normal -X -> Euler(0, -90, 0))
            EnsureOrganicMeshObject(anomalies, "Anomaly_CourtyardPlinthResidue", decalQuad, mineralJointMat,
                new Vector3(-1.104f, BaseY + 0.21f, 21.82f), Quaternion.Euler(0f, -90f, 78f), new Vector3(0.40f, 0.86f, 1f));
        }

        private static GameObject EnsureInspectableEvidenceSheet(
            Transform parent, string name, Vector3 worldPos, Quaternion worldRot, Vector3 scale, Material mat,
            string clueId, string titleEs, string titleEn, string loreEs, string loreEn, Color accentColor)
        {
            GameObject go = EnsureHotelBox(parent, name, worldPos, worldRot, scale, mat, keepCollider: true);
            BoxCollider col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.enabled = true;
            col.isTrigger = true;
            col.center = new Vector3(0f, 0f, 1.5f);
            col.size = new Vector3(1.25f, 1.25f, 8.0f);

            SecretContainmentArtifactDescriptor desc = go.GetComponent<SecretContainmentArtifactDescriptor>();
            if (desc == null) desc = go.AddComponent<SecretContainmentArtifactDescriptor>();
            desc.artifactId = clueId;
            desc.titleEs = titleEs;
            desc.titleEn = titleEn;
            desc.loreEs = loreEs;
            desc.loreEn = loreEn;
            desc.accentColor = accentColor;
            desc.inspectionScope = ArtifactInspectionScope.SecretRoomOrBackrooms;
            return go;
        }

        private static string EnsureEvidenceProjectPlanTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color paper = new Color(0.76f, 0.78f, 0.76f);
            Color blueprintBg = new Color(0.24f, 0.34f, 0.42f);
            Color gridLine = new Color(0.32f, 0.44f, 0.53f);
            Color planLine = new Color(0.84f, 0.89f, 0.92f);
            Color anomalyLine = new Color(0.76f, 0.38f, 0.32f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool margin = x < 10 || x >= w - 10 || y < 10 || y >= h - 10;
                    float grain = (PeriodicHash(x, y, w, h, 7601) - 0.5f) * 0.045f;
                    Color c = margin ? paper : blueprintBg;
                    if (!margin && ((x - 10) % 16 == 0 || (y - 10) % 16 == 0)) c = gridLine;
                    // Draw outer hotel footprint & courtyard box
                    bool outerRect = ((x == 26 || x == 216) && y >= 24 && y <= 162) || ((y == 24 || y == 162) && x >= 26 && x <= 216);
                    bool courtyardRect = ((x == 86 || x == 150) && y >= 78 && y <= 126) || ((y == 78 || y == 126) && x >= 86 && x <= 150);
                    bool wingDivide = (y == 68 && x >= 26 && x <= 216) || (x == 60 && y >= 24 && y <= 162) || (x == 176 && y >= 68 && y <= 162);
                    // Contradictory phantom wing & dashed corridor drawn beyond the actual wall
                    bool phantomWing = ((x == 216 || x == 238) && y >= 82 && y <= 136 && (y % 6 < 4)) ||
                                       ((y == 82 || y == 136) && x >= 216 && x <= 238 && (x % 6 < 4)) ||
                                       (y == 108 && x >= 176 && x <= 236 && (x % 5 < 3));
                    if (outerRect || courtyardRect || wingDivide) c = planLine;
                    if (phantomWing) c = anomalyLine;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + grain), Mathf.Clamp01(c.g + grain), Mathf.Clamp01(c.b + grain)));
                }
            }
            DrawBitmapTextCentered(tex, w, h, "PLANO NIVEL 0", 14, 2, planLine, false);
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static void EnsureDetailedPipeRiser(
            Transform parent, string name, Mesh cylMesh, Vector3 baseFloorPos, float height, float diameter,
            Material pipeMat, Material hardwareMat, float wallNormalDirX)
        {
            EnsureOrganicMeshObject(parent, name, cylMesh, pipeMat,
                new Vector3(baseFloorPos.x, baseFloorPos.y + height * 0.5f, baseFloorPos.z),
                Quaternion.identity, new Vector3(diameter, height, diameter));

            // Floor & ceiling penetration escutcheon flanges
            EnsureOrganicMeshObject(parent, name + "_FloorFlange", cylMesh, hardwareMat,
                new Vector3(baseFloorPos.x, baseFloorPos.y + 0.018f, baseFloorPos.z),
                Quaternion.identity, new Vector3(diameter * 1.95f, 0.036f, diameter * 1.95f));
            if (height >= ClearHeight - 0.1f)
            {
                EnsureOrganicMeshObject(parent, name + "_CeilFlange", cylMesh, hardwareMat,
                    new Vector3(baseFloorPos.x, baseFloorPos.y + height - 0.018f, baseFloorPos.z),
                    Quaternion.identity, new Vector3(diameter * 1.85f, 0.036f, diameter * 1.85f));
            }

            // Wall-mounting collar rings & standoffs firmly anchored into concrete wall
            float[] collarYs = height > 2.2f ? new[] { 0.68f, 1.52f, 2.34f } : new[] { 0.58f, 1.38f };
            for (int i = 0; i < collarYs.Length; i++)
            {
                Transform oldRing = parent.Find($"{name}_ClampRing_{i}");
                if (oldRing != null) UnityEngine.Object.DestroyImmediate(oldRing.gameObject);
                Transform oldStrut = parent.Find($"{name}_ClampStrut_{i}");
                if (oldStrut != null) UnityEngine.Object.DestroyImmediate(oldStrut.gameObject);

                float cy = baseFloorPos.y + collarYs[i];
                EnsureOrganicMeshObject(parent, $"{name}_CollarRing_{i}", cylMesh, hardwareMat,
                    new Vector3(baseFloorPos.x, cy, baseFloorPos.z),
                    Quaternion.identity, new Vector3(diameter * 1.38f, 0.032f, diameter * 1.38f));
                EnsureHotelBox(parent, $"{name}_CollarStrut_{i}",
                    new Vector3(baseFloorPos.x + wallNormalDirX * 0.085f, cy, baseFloorPos.z),
                    Quaternion.identity, new Vector3(0.17f, 0.026f, 0.030f), hardwareMat, keepCollider: false);
            }
        }

        private static Mesh BuildProceduralPipeCylinderMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            const int sides = 16;
            Vector3[] pts = { new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0.0f, 0f), new Vector3(0f, 0.5f, 0f) };
            float[] rads = { 0.5f, 0.5f, 0.5f };
            AddCurvedTube(verts, uvs, tris, pts, rads, sides);

            // Top & bottom end caps for sheared stubs / valve wheels
            int topCenter = verts.Count;
            verts.Add(new Vector3(0f, 0.5f, 0f)); uvs.Add(new Vector2(0.5f, 0.5f));
            int botCenter = verts.Count;
            verts.Add(new Vector3(0f, -0.5f, 0f)); uvs.Add(new Vector2(0.5f, 0.5f));
            int topRing = 2 * (sides + 1);
            int botRing = 0;
            for (int s = 0; s < sides; s++)
            {
                tris.Add(topCenter); tris.Add(topRing + s + 1); tris.Add(topRing + s);
                tris.Add(botCenter); tris.Add(botRing + s); tris.Add(botRing + s + 1);
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralDecalQuadMesh()
        {
            // Unit 1x1 quad in XY with front face & normal pointing strictly along +Z (clockwise when viewed from +Z)
            Mesh m = new Mesh();
            m.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f)
            };
            m.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            m.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            m.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            m.RecalculateBounds();
            return m;
        }

        private static string EnsureFeatheredMineralCrackDecalTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                float endFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(v, 1f - v) / 0.16f));
                float meander = (PeriodicFbm(0.35f, v, 2, 5, 4, 8801) - 0.5f) * 0.24f +
                                (PeriodicFbm(0.72f, v * 2f, 2, 9, 3, 8802) - 0.5f) * 0.08f;
                float centerU = 0.5f + meander;

                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float sideFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.14f));
                    float dist = Mathf.Abs(u - centerU);
                    float n1 = PeriodicFbm(u * 2f, v * 2f, 6, 6, 4, 8803);
                    float n2 = PeriodicFbm(u * 4f, v * 4f, 10, 10, 3, 8804);

                    // Dark hairline crack core + irregular chalky salt/mineral efflorescence halo
                    float crackMask = Mathf.Clamp01(1f - dist / (0.011f + n2 * 0.010f)) * endFade * sideFade;
                    float saltRadius = (0.045f + n1 * 0.18f) * endFade;
                    float saltMask = Mathf.Clamp01((saltRadius - dist) / Mathf.Max(0.02f, saltRadius * 0.65f));
                    saltMask = Mathf.Pow(saltMask, 1.35f) * Mathf.Clamp01(n2 * 1.45f - 0.18f) * endFade * sideFade;

                    // Secondary branching micro-fissure
                    float branchDist = Mathf.Abs(u - (centerU + (v - 0.45f) * 0.36f));
                    float branchMask = (v > 0.35f && v < 0.78f)
                        ? Mathf.Clamp01(1f - branchDist / 0.009f) * Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(v - 0.56f) / 0.22f) * 0.75f
                        : 0f;

                    Color saltCol = Color.Lerp(new Color(0.64f, 0.61f, 0.54f), new Color(0.86f, 0.84f, 0.78f), n2);
                    Color crackCol = new Color(0.11f, 0.11f, 0.10f);
                    float totalCrack = Mathf.Max(crackMask, branchMask);
                    Color rgb = Color.Lerp(saltCol, crackCol, Mathf.Clamp01(totalCrack * 1.15f));
                    float alpha = Mathf.Clamp01(Mathf.Max(saltMask * 0.88f, totalCrack * 0.92f));
                    tex.SetPixel(x, y, new Color(rgb.r, rgb.g, rgb.b, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureFeatheredDampSeepDecalTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float dx = (u - 0.5f) * 2.0f;
                    float dy = (v - 0.5f) * 2.0f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float warp = PeriodicFbm(u, v, 4, 4, 4, 8811) * 0.52f + PeriodicFbm(u * 2f, v * 2f, 9, 9, 3, 8812) * 0.24f;
                    float organicDist = r + (warp - 0.38f) * 0.68f;
                    // Strict zero-alpha border guard so NO rectangular edge can ever appear
                    float borderGuard = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.92f - r) / 0.22f));

                    float body = Mathf.Clamp01((0.76f - organicDist) / 0.48f) * borderGuard;
                    // Evaporative mineral tide-ring near outer perimeter of stain
                    float tideRing = Mathf.Exp(-Mathf.Pow((organicDist - 0.58f) / 0.085f, 2f)) * 0.38f * borderGuard;
                    float alpha = Mathf.Clamp01((body * 0.68f + tideRing) * (0.75f + 0.25f * warp));

                    Color innerDark = new Color(0.11f, 0.12f, 0.11f);
                    Color mineralRust = new Color(0.24f, 0.21f, 0.17f);
                    Color rgb = Color.Lerp(innerDark, mineralRust, Mathf.Clamp01(tideRing * 1.8f + (1f - body) * 0.45f));
                    tex.SetPixel(x, y, new Color(rgb.r, rgb.g, rgb.b, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        /// <summary>
        /// Refreshes North Wing M1 detailed infrastructure, 07.6 feathered anomaly decals, and 07.7
        /// high-detail 3D ecology in place without rebuilding the scene.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Refresh Remanencia Visual Polish & Ecology (In-Place)")]
        public static void RefreshRemanenciaVisualPolishAndEcologyAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaNorthWingM1InOpenScene(scene);
            MigrateRemanenciaHotelNarrativeInOpenScene(scene);
            MigrateRemanenciaEarlyEcologyInOpenScene(scene);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save refreshed scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Refreshed North Wing infrastructure, feathered anomaly decals & 3D organic ecology in {ScenePath}.");
        }

        /// <summary>
        /// Targeted, idempotent Implementation 07.7 early anomalous ecology migration utility.
        /// Opens the existing BackroomsLevel1Scene.unity without rebuilding the scene or modifying any
        /// architecture, elevator, narrative discovery, lighting budget, or progression schema.
        /// Creates/updates strictly Hotel_Early_Ecology (static non-colliding fungal colonies &
        /// anomalous vegetation with custom procedural meshes and matte materials) under BackroomsLevel1_Root.
        /// </summary>
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Early Ecology (In-Place)")]
        public static void MigrateRemanenciaEarlyEcologyAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaEarlyEcologyInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia early anomalous ecology (fungi & vegetation) migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaEarlyEcologyInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia early ecology migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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

            if (root.transform.Find(HotelNarrativeEvidenceRootName) == null)
            {
                throw new InvalidOperationException($"Expected '{HotelNarrativeEvidenceRootName}' under BackroomsLevel1_Root before running 07.7 ecology migration.");
            }

            BuildOrUpdateHotelEarlyEcology(root.transform);
        }

        private static void BuildOrUpdateHotelEarlyEcology(Transform backroomsRoot)
        {
            // 1. High-resolution 512x512 anatomically shaded procedural textures (ZERO emission, ZERO neon)
            string mycTex = EnsureDendriticMyceliumTexture("Assets/Textures/Environment/T_Remanencia_Ecology_Mycelium.png");
            string capTex = EnsureMycologicalShadedAtlasTexture("Assets/Textures/Environment/T_Remanencia_Ecology_FungalCap.png");
            string leafTex = EnsureVegetalBladeShadedTexture("Assets/Textures/Environment/T_Remanencia_Ecology_VegetalLeaf.png");
            string rootTex = EnsureVegetalRootBarkTexture("Assets/Textures/Environment/T_Remanencia_Ecology_VegetalRoot.png");
            AssetDatabase.Refresh();

            ConfigureAlphaDecalTextureImporter(mycTex);
            ConfigureHighDetailOrganicImporter(capTex);
            ConfigureHighDetailOrganicImporter(leafTex);
            ConfigureHighDetailOrganicImporter(rootTex);

            // Dendritic mycelium uses smooth alpha-blending (renderQueue 3005 above damp decals) + 3D raised cords;
            // fruiting bodies, brackets, roots & foliage use crisp opaque anatomically shaded matte materials (ZERO emission).
            Material mycMat = CreateAlphaBlendedDecalMat(EcologyMyceliumMatPath, mycTex, Color.white, 0.08f, renderQueue: 3005);
            Material capMat = CreateOpaqueTexturedMat(EcologyFungalCapMatPath, capTex, Color.white, 0.14f);
            Material leafMat = CreateOpaqueTexturedMat(EcologyVegetalLeafMatPath, leafTex, Color.white, 0.18f);
            Material rootMat = CreateOpaqueTexturedMat(EcologyVegetalRootMatPath, rootTex, Color.white, 0.10f);

            // 2. Sculpted reusable organic mesh assets (mycelium +Z normal; clusters/roots/leaves +Y up; brackets +Z outward)
            Mesh mycMesh = EnsureEcologyMeshAsset(EcologyMyceliumMeshPath, BuildProceduralMyceliumWebMesh);
            Mesh clusterMesh = EnsureEcologyMeshAsset(EcologyFungalClusterMeshPath, BuildProceduralFungalClusterMesh);
            Mesh bracketMesh = EnsureEcologyMeshAsset(EcologyFungalBracketMeshPath, BuildProceduralFungalBracketMesh);
            Mesh rootMesh = EnsureEcologyMeshAsset(EcologyVegetalRootMeshPath, BuildProceduralVegetalRootMesh);
            Mesh leafMesh = EnsureEcologyMeshAsset(EcologyVegetalLeafMeshPath, BuildProceduralVegetalLeafTuftMesh);

            Transform ecoRoot = EnsureHotelSubgroup(backroomsRoot, HotelEarlyEcologyRootName);

            // =========================================================================
            // ZONE A — CENTRAL COURTYARD: FIRST VEGETAL EMERGENCE (NorthWing_Courtyard)
            //   Sculpted gnarled root systems & 22-blade 3D V-channeled sage-agave rosettes
            //   emerging from concrete joints around the central plinth & colonnade piers.
            // =========================================================================
            Transform courtyardEco = EnsureHotelSubgroup(ecoRoot, "Ecology_Courtyard_Vegetation");

            // Plinth West seam beneath Anomaly_CourtyardPlinthResidue (-1.104, BaseY+0.21, 21.82)
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PlinthRootCluster_W", rootMesh, rootMat,
                new Vector3(-1.32f, BaseY + 0.004f, 21.82f), Quaternion.Euler(0f, 18f, 0f), new Vector3(1.12f, 1.05f, 1.12f));
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PlinthLeafTuft_W", leafMesh, leafMat,
                new Vector3(-1.26f, BaseY + 0.004f, 21.82f), Quaternion.Euler(0f, 34f, 0f), new Vector3(1.14f, 1.12f, 1.14f));

            // Plinth upper concrete/ceramic rim seam near central drain grate (strictly inside [-1.10, +1.10] plinth top!)
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PlinthTopRoots_NW", rootMesh, rootMat,
                new Vector3(-0.38f, BaseY + 0.456f, 22.18f), Quaternion.Euler(0f, -48f, 0f), new Vector3(0.54f, 0.56f, 0.54f));
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PlinthTopLeaves_NW", leafMesh, leafMat,
                new Vector3(-0.38f, BaseY + 0.456f, 22.18f), Quaternion.Euler(0f, 118f, 0f), new Vector3(0.76f, 0.78f, 0.76f));

            // SW Courtyard sightline pier floor joint (pier at (-2.70, 20.30), East face at -2.48)
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PierSW_SeamRoots", rootMesh, rootMat,
                new Vector3(-2.45f, BaseY + 0.004f, 20.34f), Quaternion.Euler(0f, 28f, 0f), new Vector3(0.92f, 0.90f, 0.92f));
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_PierSW_LeafCluster", leafMesh, leafMat,
                new Vector3(-2.44f, BaseY + 0.004f, 20.34f), Quaternion.Euler(0f, -64f, 0f), new Vector3(0.96f, 0.98f, 0.96f));

            // NE Courtyard corner pier concrete joint (4.85, 25.85)
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_CornerNE_SeamRoots", rootMesh, rootMat,
                new Vector3(4.54f, BaseY + 0.004f, 25.52f), Quaternion.Euler(0f, -138f, 0f), new Vector3(0.88f, 0.88f, 0.88f));
            EnsureOrganicMeshObject(courtyardEco, "Eco_Courtyard_CornerNE_LeafTuft", leafMesh, leafMat,
                new Vector3(4.54f, BaseY + 0.004f, 25.52f), Quaternion.Euler(0f, 74f, 0f), new Vector3(0.88f, 0.90f, 0.88f));

            // =========================================================================
            // ZONE B — MAINTENANCE CORRIDOR: FIRST MYCELIAL COLONIES
            //   Wall surface X = -8.710 (normal -X -> Euler(0, -90, roll)); floor Y = -50.000
            //   (normal +Y -> Euler(-90, yaw, 0)). Maintenance Record (Z=22.45, Y=BaseY+1.46) stays 100% clear!
            // =========================================================================
            Transform maintEco = EnsureHotelSubgroup(ecoRoot, "Ecology_Maintenance_Mycelium");

            // Dendritic wall mycelium radiating UP along weeping concrete crack (safely below Maintenance Record)
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_JointMycelium_WallA", mycMesh, mycMat,
                new Vector3(-8.716f, BaseY + 0.48f, 22.06f), Quaternion.Euler(0f, -90f, 12f), new Vector3(1.12f, 1.02f, 1.0f));

            // Floor dendritic mycelium fan spreading out from the damp under-wall seepage on the West (Maintenance) side
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_FloorSeepMycelium_W", mycMesh, mycMat,
                new Vector3(-9.02f, BaseY + 0.005f, 22.22f), Quaternion.Euler(-90f, -22f, 0f), new Vector3(1.26f, 1.14f, 1.0f));

            // Matching dendritic mycelium emerging on the East (West Arcade) side of solid wall X = -8.6
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_FloorSeepMycelium_E", mycMesh, mycMat,
                new Vector3(-8.20f, BaseY + 0.005f, 22.22f), Quaternion.Euler(-90f, 158f, 0f), new Vector3(1.04f, 0.96f, 1.0f));

            // Sculpted 15-stem gilled fruiting colony nestled at the damp wall-floor junction + shelf brackets rooted INSIDE crack B
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_JointBase_FungalCluster", clusterMesh, capMat,
                new Vector3(-8.86f, BaseY + 0.003f, 22.10f), Quaternion.Euler(0f, -74f, 0f), new Vector3(1.14f, 1.12f, 1.14f));
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_JointBase_Mycelium", bracketMesh, capMat,
                new Vector3(-8.711f, BaseY + 0.54f, 22.88f), Quaternion.Euler(0f, -90f, 3f), new Vector3(0.86f, 0.84f, 0.88f));

            // Dendritic mycelium reaching up toward the sheared conduit stub below the Service Breaker Panel (Z = 21.60)
            EnsureOrganicMeshObject(maintEco, "Eco_Maint_PanelLower_Mycelium", mycMesh, mycMat,
                new Vector3(-8.716f, BaseY + 0.76f, 21.62f), Quaternion.Euler(0f, -90f, -28f), new Vector3(0.88f, 0.88f, 1.0f));

            // =========================================================================
            // ZONE C — HOUSEKEEPING / UTILITY AREA: PROTECTED FUNGAL GROWTH
            //   East-facing wall X = -15.870 (normal +X -> Euler(0, 90, roll)) behind cylindrical
            //   copper & galvanized risers. Rumor Note (-12.75, BaseY+1.38, 23.27) stays 100% clear!
            // =========================================================================
            Transform utilityEco = EnsureHotelSubgroup(ecoRoot, "Ecology_Utility_FungalGrowth");

            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_RiserCorner_ColonyMain", clusterMesh, capMat,
                new Vector3(-15.52f, BaseY + 0.003f, 18.56f), Quaternion.Euler(0f, 48f, 0f), new Vector3(1.34f, 1.32f, 1.34f));
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_RiserCorner_ColonySat", clusterMesh, capMat,
                new Vector3(-15.60f, BaseY + 0.003f, 18.96f), Quaternion.Euler(0f, 118f, 0f), new Vector3(1.04f, 1.00f, 1.04f));
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_RiserFloor_Mycelium", mycMesh, mycMat,
                new Vector3(-15.46f, BaseY + 0.005f, 18.68f), Quaternion.Euler(-90f, 42f, 0f), new Vector3(1.42f, 1.34f, 1.0f));
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_RiserWall_Mycelium", mycMesh, mycMat,
                new Vector3(-15.862f, BaseY + 0.72f, 18.64f), Quaternion.Euler(0f, 90f, -8f), new Vector3(1.34f, 1.40f, 1.0f));

            // Concentric polypore shelf brackets emerging out (+X) from the concrete wall fissure & wrapping copper riser A!
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_WallBracket_Lower", bracketMesh, capMat,
                new Vector3(-15.868f, BaseY + 0.56f, 18.66f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.16f, 1.12f, 1.18f));
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_WallBracket_Upper", bracketMesh, capMat,
                new Vector3(-15.735f, BaseY + 0.94f, 18.45f), Quaternion.Euler(0f, 76f, -3f), new Vector3(0.92f, 0.90f, 0.96f));

            // Creeping floor mycelium strictly at floor level (1.38m below Cobalt-60 Rumor Note!)
            EnsureOrganicMeshObject(utilityEco, "Eco_Utility_NorthBase_Mycelium", mycMesh, mycMat,
                new Vector3(-12.76f, BaseY + 0.005f, 23.02f), Quaternion.Euler(-90f, 172f, 0f), new Vector3(1.08f, 0.94f, 1.0f));

            // =========================================================================
            // ZONE D (OPTIONAL SECONDARY TRACE) — ROOM 110 UNFINISHED PLUMBING CHASE
            //   Crisp dendritic mycelium & small gilled cluster around cylindrical copper riser
            // =========================================================================
            Transform guestTrace = EnsureHotelSubgroup(ecoRoot, "Ecology_Optional_GuestRoomTrace");

            EnsureOrganicMeshObject(guestTrace, "Eco_Room110_ChaseRiser_Mycelium", mycMesh, mycMat,
                new Vector3(-9.28f, BaseY + 0.005f, 33.52f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(1.04f, 0.98f, 1.0f));
            EnsureOrganicMeshObject(guestTrace, "Eco_Room110_ChaseRiser_FungalSprout", clusterMesh, capMat,
                new Vector3(-9.30f, BaseY + 0.003f, 33.46f), Quaternion.Euler(0f, 142f, 0f), new Vector3(0.92f, 0.88f, 0.92f));
        }

        // =========================================================================
        // IMPLEMENTATION 07.8 — REMANENCIA FIRST FAUNAL TRACES (STATIC, RESTRAINED)
        //   Independent sibling root Hotel_Faunal_Traces under BackroomsLevel1_Root.
        //   Reuses strictly 3 procedural meshes, 3 opaque non-emissive matte/parchment
        //   materials, and existing 07.7 textures (ZERO new textures, ZERO colliders,
        //   ZERO lights, ZERO scripts, ZERO living fauna or identifiable creatures).
        // =========================================================================
        [MenuItem("MyVirtualCommunity/Migrate Remanencia First Faunal Traces (In-Place)")]
        public static void MigrateRemanenciaFirstFaunalTracesAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaFirstFaunalTracesInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia first faunal traces migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaFirstFaunalTracesInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia first faunal traces migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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

            if (root.transform.Find(HotelEarlyEcologyRootName) == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{HotelEarlyEcologyRootName}' under BackroomsLevel1_Root before running 07.8 faunal traces migration.");
            }

            BuildOrUpdateHotelFaunalTraces(root.transform);
        }

        private static void BuildOrUpdateHotelFaunalTraces(Transform backroomsRoot)
        {
            // Reuse existing 07.7 512x512 procedural textures (ZERO unnecessary new texture assets!)
            const string existingFungalAtlasTex = "Assets/Textures/Environment/T_Remanencia_Ecology_FungalCap.png";
            const string existingRootBarkTex = "Assets/Textures/Environment/T_Remanencia_Ecology_VegetalRoot.png";

            Material filamentMat = CreateOpaqueTexturedMat(FaunalFilamentMatPath, existingFungalAtlasTex, new Color(0.74f, 0.72f, 0.66f), 0.22f);
            Material cuticleMat = CreateOpaqueTexturedMat(FaunalCuticleMatPath, existingFungalAtlasTex, new Color(0.88f, 0.80f, 0.67f), 0.34f);
            Material residueMat = CreateOpaqueTexturedMat(FaunalResidueMatPath, existingRootBarkTex, new Color(0.76f, 0.71f, 0.63f), 0.08f);

            Mesh filamentMesh = EnsureEcologyMeshAsset(FaunalFilamentMeshPath, BuildProceduralFaunalFilamentMesh);
            Mesh cuticleMesh = EnsureEcologyMeshAsset(FaunalCuticleMeshPath, BuildProceduralFaunalCuticleFragmentMesh);
            Mesh residueMesh = EnsureEcologyMeshAsset(FaunalResidueMeshPath, BuildProceduralFaunalMicroResidueMesh);

            Transform faunalRoot = EnsureHotelSubgroup(backroomsRoot, HotelFaunalTracesRootName);

            // =========================================================================
            // ZONE A — COURTYARD (Faunal_Courtyard_Traces) — 4 restrained traces
            //   Sheltered concrete plinth & pier seams beside existing 07.7 roots;
            //   strictly off circulation walkways and off prominent foliage leaves.
            // =========================================================================
            Transform courtyardTraces = EnsureHotelSubgroup(faunalRoot, "Faunal_Courtyard_Traces");

            EnsureOrganicMeshObject(courtyardTraces, "Faunal_Courtyard_PlinthSeam_Filaments", filamentMesh, filamentMat,
                new Vector3(-1.104f, BaseY + 0.09f, 21.62f), Quaternion.Euler(0f, -90f, 6f), new Vector3(0.95f, 0.90f, 0.95f));
            EnsureOrganicMeshObject(courtyardTraces, "Faunal_Courtyard_PierSW_Filaments", filamentMesh, filamentMat,
                new Vector3(-2.478f, BaseY + 0.11f, 20.48f), Quaternion.Euler(0f, 90f, -4f), new Vector3(0.85f, 0.85f, 0.85f));
            EnsureOrganicMeshObject(courtyardTraces, "Faunal_Courtyard_PlinthBase_Cuticle", cuticleMesh, cuticleMat,
                new Vector3(-1.17f, BaseY + 0.004f, 22.06f), Quaternion.Euler(0f, 32f, 0f), new Vector3(1.05f, 1.00f, 1.05f));
            EnsureOrganicMeshObject(courtyardTraces, "Faunal_Courtyard_PierNE_Cuticle", cuticleMesh, cuticleMat,
                new Vector3(4.56f, BaseY + 0.004f, 25.64f), Quaternion.Euler(0f, -125f, 0f), new Vector3(0.95f, 0.95f, 0.95f));

            // =========================================================================
            // ZONE B — MAINTENANCE CORRIDOR (Faunal_Maintenance_Traces) — 5 traces
            //   Near existing mycelium, wall-floor joints, and sheared conduit stub;
            //   well clear of Hotel_Evidence_MaintenanceRecord (Y = BaseY + 1.46, Z = 22.45).
            // =========================================================================
            Transform maintTraces = EnsureHotelSubgroup(faunalRoot, "Faunal_Maintenance_Traces");

            EnsureOrganicMeshObject(maintTraces, "Faunal_Maint_ConduitStub_Filaments", filamentMesh, filamentMat,
                new Vector3(-8.712f, BaseY + 0.72f, 21.62f), Quaternion.Euler(0f, -90f, -8f), new Vector3(0.88f, 0.92f, 1.15f));
            EnsureOrganicMeshObject(maintTraces, "Faunal_Maint_JointMycelium_Filaments", filamentMesh, filamentMat,
                new Vector3(-8.712f, BaseY + 0.24f, 22.04f), Quaternion.Euler(0f, -90f, 10f), new Vector3(0.92f, 0.88f, 0.90f));
            EnsureOrganicMeshObject(maintTraces, "Faunal_Maint_BracketCrack_Filaments", filamentMesh, filamentMat,
                new Vector3(-8.712f, BaseY + 0.44f, 22.88f), Quaternion.Euler(0f, -90f, -6f), new Vector3(0.76f, 0.78f, 0.85f));
            EnsureOrganicMeshObject(maintTraces, "Faunal_Maint_WallFloorJoint_ResidueA", residueMesh, residueMat,
                new Vector3(-8.775f, BaseY + 0.003f, 21.84f), Quaternion.Euler(0f, -90f, 0f), new Vector3(0.95f, 0.90f, 0.92f));
            EnsureOrganicMeshObject(maintTraces, "Faunal_Maint_WallFloorJoint_ResidueB", residueMesh, residueMat,
                new Vector3(-8.775f, BaseY + 0.003f, 22.86f), Quaternion.Euler(0f, -82f, 0f), new Vector3(0.86f, 0.85f, 0.85f));

            // =========================================================================
            // ZONE C — HOUSEKEEPING UTILITY ROOM (Faunal_Utility_Traces) — 7 traces
            //   Densest but still restrained concentration around SW plumbing risers &
            //   protected fungal corner (> 5.2 m away from Hotel_Evidence_Cobalt60RumorNote).
            // =========================================================================
            Transform utilityTraces = EnsureHotelSubgroup(faunalRoot, "Faunal_Utility_Traces");

            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_RiserBridge_FilamentsA", filamentMesh, filamentMat,
                new Vector3(-15.72f, BaseY + 0.52f, 18.585f), Quaternion.Euler(0f, 90f, 4f), new Vector3(0.98f, 0.96f, 0.95f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_ValveBracket_FilamentsB", filamentMesh, filamentMat,
                new Vector3(-15.74f, BaseY + 0.86f, 18.48f), Quaternion.Euler(0f, 90f, -6f), new Vector3(0.84f, 0.86f, 0.95f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_LowerPipe_FilamentsC", filamentMesh, filamentMat,
                new Vector3(-15.864f, BaseY + 0.16f, 18.78f), Quaternion.Euler(0f, 90f, 10f), new Vector3(0.86f, 0.82f, 1.05f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_RiserBase_ResidueA", residueMesh, residueMat,
                new Vector3(-15.76f, BaseY + 0.003f, 18.30f), Quaternion.Euler(0f, 44f, 0f), new Vector3(1.10f, 1.02f, 1.08f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_ColonyEdge_ResidueB", residueMesh, residueMat,
                new Vector3(-15.77f, BaseY + 0.003f, 18.80f), Quaternion.Euler(0f, 88f, 0f), new Vector3(0.96f, 0.92f, 0.94f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_CornerBehindPipe_CuticleA", cuticleMesh, cuticleMat,
                new Vector3(-15.62f, BaseY + 0.004f, 18.24f), Quaternion.Euler(0f, 18f, 0f), new Vector3(1.15f, 1.10f, 1.15f));
            EnsureOrganicMeshObject(utilityTraces, "Faunal_Utility_PipeFlange_CuticleB", cuticleMesh, cuticleMat,
                new Vector3(-15.74f, BaseY + 0.004f, 19.14f), Quaternion.Euler(0f, 114f, 0f), new Vector3(1.02f, 1.00f, 1.02f));
        }

        private static Mesh BuildProceduralFaunalFilamentMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 5 short, discontinuous, asymmetric catenary-sagging biological filaments (NOT spider-web rings or cobweb sheets!).
            // EVERY endpoint seats cleanly into the host surface (z = -0.005m) while arching delicately outward (+Z) across
            // the crevice or pipe gap so ZERO blunt floating tips ever appear in mid-air!
            (Vector3 a, Vector3 b, float sagY, float bowZ, float midRad)[] strands =
            {
                (new Vector3(-0.145f,  0.072f, -0.005f), new Vector3( 0.142f,  0.036f, -0.005f), 0.032f, 0.044f, 0.0014f),
                (new Vector3(-0.138f, -0.015f, -0.005f), new Vector3( 0.145f, -0.048f, -0.005f), 0.028f, 0.052f, 0.0013f),
                (new Vector3(-0.132f,  0.022f, -0.005f), new Vector3( 0.095f,  0.092f, -0.005f), 0.024f, 0.036f, 0.0012f),
                (new Vector3(-0.085f, -0.082f, -0.005f), new Vector3( 0.138f, -0.008f, -0.005f), 0.022f, 0.048f, 0.0012f),
                (new Vector3(-0.142f, -0.056f, -0.005f), new Vector3( 0.062f,  0.042f, -0.005f), 0.020f, 0.030f, 0.0011f)
            };

            var savedCurves = new List<Vector3[]>();
            for (int s = 0; s < strands.Length; s++)
            {
                var st = strands[s];
                var pts = new Vector3[9];
                var rads = new float[9];
                for (int k = 0; k < 9; k++)
                {
                    float t = k / 8f;
                    float arch = Mathf.Sin(t * Mathf.PI);
                    Vector3 p = Vector3.Lerp(st.a, st.b, t);
                    p.y -= arch * st.sagY;
                    p.z += arch * st.bowZ + Mathf.Sin(t * Mathf.PI * 2f + s) * 0.004f;
                    pts[k] = p;
                    // Subtle anchor flare embedded at wall contact, ultra-fine delicate silk profile along span
                    float endFlare = 1f + Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 4f) * 0.55f;
                    rads[k] = st.midRad * endFlare;
                }
                savedCurves.Add(pts);
                AddCurvedTubeWithVRange(verts, uvs, tris, pts, rads, sides: 5, vStart: 0.44f, vEnd: 0.56f);
            }

            // 2 delicate irregular cross-draped connecting threads anchored cleanly between existing primary strands (zero loose tips!)
            (int sA, int idxA, int sB, int idxB, float drop)[] bridges =
            {
                (0, 3, 1, 5, 0.016f),
                (2, 4, 4, 6, 0.014f)
            };
            foreach (var br in bridges)
            {
                Vector3 pA = savedCurves[br.sA][br.idxA];
                Vector3 pB = savedCurves[br.sB][br.idxB];
                var bPts = new Vector3[5];
                var bRads = new float[5];
                for (int k = 0; k < 5; k++)
                {
                    float t = k / 4f;
                    Vector3 p = Vector3.Lerp(pA, pB, t);
                    p.y -= Mathf.Sin(t * Mathf.PI) * br.drop;
                    bPts[k] = p;
                    bRads[k] = 0.0010f;
                }
                AddCurvedTubeWithVRange(verts, uvs, tris, bPts, bRads, sides: 4, vStart: 0.46f, vEnd: 0.56f);
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralFaunalCuticleFragmentMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 4 tiny, empty, dry, C-curled semi-chitinous shell/husk fragments caught in a protected corner.
            // Strictly non-specific open shards: ZERO legs, ZERO wings, ZERO antennae, ZERO heads, ZERO animal silhouettes!
            (Vector3 pos, float yaw, float roll, float length, float width, float curlH)[] shards =
            {
                (new Vector3(-0.025f, 0.002f,  0.012f),  24f,  14f, 0.046f, 0.020f, 0.014f),
                (new Vector3( 0.042f, 0.002f, -0.018f), 118f, -22f, 0.036f, 0.016f, 0.011f),
                (new Vector3(-0.056f, 0.002f, -0.036f), -68f,  30f, 0.028f, 0.013f, 0.009f),
                (new Vector3( 0.016f, 0.002f,  0.052f), 162f, -12f, 0.024f, 0.011f, 0.008f)
            };

            const int lenSegs = 7;
            const int arcSegs = 6;
            for (int f = 0; f < shards.Length; f++)
            {
                var sh = shards[f];
                Quaternion rot = Quaternion.Euler(0f, sh.yaw, sh.roll);
                int gridStart = verts.Count;

                for (int i = 0; i <= lenSegs; i++)
                {
                    float tl = (float)i / lenSegs; // 0..1 along broken husk length
                    // Irregular broken-edge width profile + subtle transverse dry cuticle annulation ripples
                    float brokenTaper = Mathf.Sin(Mathf.Lerp(0.18f, 0.90f, tl) * Mathf.PI);
                    float ripple = 1f + 0.08f * Mathf.Sin(tl * Mathf.PI * 5f + f);
                    float curW = sh.width * brokenTaper * ripple;
                    float curH = sh.curlH * brokenTaper * ripple;
                    float zAlong = (tl - 0.5f) * sh.length;
                    // Slight banana/C longitudinal curl so the empty husk lifts organically off the concrete
                    float longArchY = (1f - Mathf.Sin(tl * Mathf.PI)) * (sh.curlH * 0.45f);
                    float curveX = Mathf.Sin(tl * Mathf.PI) * (sh.length * 0.14f) * (f % 2 == 0 ? 1f : -1f);

                    for (int j = 0; j <= arcSegs; j++)
                    {
                        float ta = (float)j / arcSegs; // 0..1 across open C-channel shell
                        float theta = Mathf.Lerp(-Mathf.PI * 0.46f, Mathf.PI * 0.46f, ta);
                        Vector3 local = new Vector3(
                            curveX + Mathf.Sin(theta) * curW * 0.5f,
                            longArchY + (1f - Mathf.Cos(theta)) * curH + 0.002f,
                            zAlong);
                        verts.Add(sh.pos + rot * local);
                        // UV v: 0.58 bright translucent-looking parchment margin -> 0.90 dry chestnut inner hollow
                        float edgeDist = Mathf.Abs(ta - 0.5f) * 2f;
                        float vCoord = Mathf.Lerp(0.90f, 0.58f, edgeDist);
                        uvs.Add(new Vector2(tl, vCoord));
                    }
                }

                int stride = arcSegs + 1;
                for (int i = 0; i < lenSegs; i++)
                {
                    int r0 = gridStart + i * stride;
                    int r1 = gridStart + (i + 1) * stride;
                    for (int j = 0; j < arcSegs; j++)
                    {
                        tris.Add(r0 + j); tris.Add(r1 + j); tris.Add(r0 + j + 1);
                        tris.Add(r0 + j + 1); tris.Add(r1 + j); tris.Add(r1 + j + 1);
                    }
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralFaunalMicroResidueMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var rng = new System.Random(7803);

            // 1. Low-profile organic mineral-debris seam crust hugging concrete joint (tapers cleanly below Y=0 at rim)
            const int rings = 4;
            const int segs = 16;
            int centerIdx = verts.Count;
            verts.Add(new Vector3(0f, 0.015f, 0.010f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            int ringStart = verts.Count;
            for (int r = 1; r <= rings; r++)
            {
                float tr = (float)r / rings;
                for (int s = 0; s <= segs; s++)
                {
                    float a = (s / (float)segs) * Mathf.PI * 2f;
                    float irreg = 0.78f + 0.28f * Mathf.Sin(a * 3f + 0.6f) + 0.14f * Mathf.Cos(a * 5f - 0.9f);
                    float rx = 0.115f * tr * irreg;
                    float rz = 0.068f * tr * irreg;
                    float y = Mathf.Lerp(0.015f, -0.002f, Mathf.Pow(tr, 1.35f)) +
                              (r < rings ? (float)(rng.NextDouble() - 0.5) * 0.004f : 0f);
                    verts.Add(new Vector3(Mathf.Cos(a) * rx, y, 0.015f + Mathf.Sin(a) * rz));
                    uvs.Add(new Vector2((float)s / segs, tr));
                }
            }

            int stride = segs + 1;
            for (int s = 0; s < segs; s++)
            {
                tris.Add(centerIdx); tris.Add(ringStart + s + 1); tris.Add(ringStart + s);
            }
            for (int r = 0; r < rings - 1; r++)
            {
                int r0 = ringStart + r * stride;
                int r1 = ringStart + (r + 1) * stride;
                for (int s = 0; s < segs; s++)
                {
                    tris.Add(r0 + s); tris.Add(r0 + s + 1); tris.Add(r1 + s);
                    tris.Add(r1 + s); tris.Add(r0 + s + 1); tris.Add(r1 + s + 1);
                }
            }

            // 2. 24 compact dry organic granules & micro-debris particles embedded around the seam crust
            for (int g = 0; g < 24; g++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Sqrt((float)rng.NextDouble()) * 0.095f;
                float gx = Mathf.Cos(ang) * dist;
                float gz = 0.014f + Mathf.Sin(ang) * dist * 0.62f;
                float radX = 0.0045f + (float)rng.NextDouble() * 0.0065f;
                float radY = 0.0038f + (float)rng.NextDouble() * 0.0055f;
                float radZ = 0.0042f + (float)rng.NextDouble() * 0.0060f;
                float baseH = Mathf.Lerp(0.011f, 0.002f, Mathf.Clamp01(dist / 0.095f));
                Vector3 c = new Vector3(gx, baseH, gz);

                // Compact 6-sided organic faceted granule (top apex, mid-ring, base)
                int topV = verts.Count;
                verts.Add(c + new Vector3(0f, radY, 0f)); uvs.Add(new Vector2(0.5f, 0.85f));
                int midV = verts.Count;
                for (int k = 0; k < 6; k++)
                {
                    float ka = (k / 6f) * Mathf.PI * 2f + g * 0.4f;
                    float j = 0.82f + (float)rng.NextDouble() * 0.36f;
                    verts.Add(c + new Vector3(Mathf.Cos(ka) * radX * j, 0f, Mathf.Sin(ka) * radZ * j));
                    uvs.Add(new Vector2(k / 6f, 0.45f));
                }
                int botV = verts.Count;
                verts.Add(c + new Vector3(0f, -radY * 0.65f, 0f)); uvs.Add(new Vector2(0.5f, 0.10f));

                for (int k = 0; k < 6; k++)
                {
                    int kNext = (k + 1) % 6;
                    tris.Add(topV); tris.Add(midV + kNext); tris.Add(midV + k);
                    tris.Add(botV); tris.Add(midV + k); tris.Add(midV + kNext);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================
        // IMPLEMENTATION 07.9 — REMANENCIA FIRST LIVING MICROFAUNA
        //   Independent sibling root Hotel_FirstLivingMicrofauna under BackroomsLevel1_Root.
        //   Strictly 2 tiny (3.6 cm) unidentified segmented organisms (Primary: Zone C Utility,
        //   Secondary: Zone B Maintenance) using 1 shared mesh, 1 shared non-emissive opaque
        //   material (reusing existing T_Remanencia_Ecology_FungalCap.png), and lightweight
        //   deterministic ambient motion (RemanenciaAmbientMicrofaunaController).
        // =========================================================================
        [MenuItem("MyVirtualCommunity/Migrate Remanencia First Living Microfauna (In-Place)")]
        public static void MigrateRemanenciaFirstLivingMicrofaunaAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaFirstLivingMicrofaunaInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia first living microfauna migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaFirstLivingMicrofaunaInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia first living microfauna migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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

            if (root.transform.Find(HotelFaunalTracesRootName) == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{HotelFaunalTracesRootName}' under BackroomsLevel1_Root before running 07.9 microfauna migration.");
            }

            BuildOrUpdateHotelFirstLivingMicrofauna(root.transform);
        }

        // =========================================================================
        // IMPLEMENTATION 07.11 — ROOM 111-B: THE IMPOSSIBLE EAST WING (IN-PLACE MIGRATION)
        //   Constructs approved Option A (Hidden East Architectural Passage) strictly inside
        //   BackroomsLevel1Scene.unity under Backrooms_Architecture/Hotel_Room111B_Anomaly:
        //     - Shortens retained NorthWing_OuterWall_East (and _Baseboard) IN-PLACE to Z=12.0..26.80
        //       (preserving its existing GameObject/fileID and keeping Z=+20.0 100% solid).
        //     - Room111B_ConcealedOpening: Z=+26.80..+28.40 framed opening (1.60m x 2.20m) behind
        //       NorthWing_NorthCorridor_EastOverlookNib, screened by offset concrete shadow-baffle wall
        //       at X=+17.55 so the corridor East end initially reads as a solid architectural dead end.
        //     - Room111B_EastCorridor: phantom East corridor (X=+16.13..+18.90, Z=+20.20..+28.68,
        //       continuous floor & ceiling slabs, misaligned floor expansion joint, 1 emissive-only strip).
        //     - Room111B_RoomShell: empty architectural shell (X=+19.12..+24.52, Z=+20.00..+24.80,
        //       5.40m x 4.80m extending beyond original hotel East boundary X=+20.0, weathered plaster
        //       finish contrast, older framed doorway at X=+19.01 Z=+21.70..+23.10, zero furniture/props/lights).
        //     - Room111B_Signage: upright non-mirrored '111-B' room plaque facing West into East Corridor.
        //   Strictly preserves BorderCorridorScene.unity, Backrooms_ExitDoor_ToStreet, Hotel_Elevator_Core,
        //   07.6-07.9 content, exact 18 scene lights (0 shadows), and save/progression schemas.
        // =========================================================================
        [MenuItem("MyVirtualCommunity/Migrate Remanencia Room 111-B Architecture (In-Place)")]
        public static void MigrateRemanenciaRoom111BAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaRoom111BInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Room 111-B (Impossible East Wing) architecture migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaRoom111BInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia Room 111-B migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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
            if (shellRoot == null)
            {
                throw new InvalidOperationException("Backrooms_Architecture not found under BackroomsLevel1_Root.");
            }
            if (shellRoot.Find(NorthWingM1RootName) == null)
            {
                throw new InvalidOperationException($"Expected '{NorthWingM1RootName}' under Backrooms_Architecture before running 07.11 Room 111-B migration.");
            }
            if (root.transform.Find(HotelFirstLivingMicrofaunaRootName) == null)
            {
                throw new InvalidOperationException($"Expected '{HotelFirstLivingMicrofaunaRootName}' under BackroomsLevel1_Root before running 07.11 Room 111-B migration.");
            }

            BuildOrUpdateRoom111BAnomaly(shellRoot);
        }

        private static void BuildOrUpdateRoom111BAnomaly(Transform shellRoot)
        {
            const float wallT = 0.22f;
            const float outerT = 0.26f;

            // Reuse existing canonical Nivel 0 materials read-only (ZERO mutations to existing material/texture assets)
            Material wallConcrete = AssetDatabase.LoadAssetAtPath<Material>(WallConcreteMatPath);
            Material wallPlaster = AssetDatabase.LoadAssetAtPath<Material>(PlasterWallMatPath);
            Material columnConcrete = AssetDatabase.LoadAssetAtPath<Material>(ColumnConcreteMatPath);
            Material floorConcrete = AssetDatabase.LoadAssetAtPath<Material>(FloorConcreteMatPath);
            Material ceilingSoffit = AssetDatabase.LoadAssetAtPath<Material>(CeilingSoffitMatPath);
            Material baseboardConcrete = AssetDatabase.LoadAssetAtPath<Material>(BaseboardMatPath);
            Material industrialMetal = AssetDatabase.LoadAssetAtPath<Material>(IndustrialMetalMatPath);
            Material elevatorMetal = AssetDatabase.LoadAssetAtPath<Material>(ElevatorDoorMetalMatPath);
            Material fluorOn = AssetDatabase.LoadAssetAtPath<Material>(FluorOnMatPath);
            Material utilityWarmOn = AssetDatabase.LoadAssetAtPath<Material>(UtilityWarmOnMatPath);

            if (wallConcrete == null || wallPlaster == null || columnConcrete == null ||
                floorConcrete == null || ceilingSoffit == null || baseboardConcrete == null ||
                industrialMetal == null || elevatorMetal == null || fluorOn == null || utilityWarmOn == null)
            {
                throw new InvalidOperationException("One or more canonical Remanencia Nivel 0 architectural materials are missing.");
            }

            // Generate/load only the dedicated 111-B room plaque texture & material (matte, zero glow)
            string plaque111BTex = EnsureRoom111BPlaqueTexture(RoomPlaque111BTexPath);
            AssetDatabase.Refresh();
            ConfigurePlaqueTextureImporter(plaque111BTex);
            Material plaque111BMat = CreateTexturedMat(RoomPlaque111BMatPath, plaque111BTex, Color.white, 0.28f, Vector2.one);

            // 1. Shorten retained NorthWing_OuterWall_East (and _Baseboard) IN-PLACE to Z = +12.00..+26.80
            //    Preserves existing GameObject/Transform/Collider fileIDs and keeps Z = +20.00 100% solid.
            Transform connLobby = shellRoot.Find($"{NorthWingM1RootName}/NorthWing_Connection_Lobby");
            if (connLobby == null)
            {
                throw new InvalidOperationException("Missing NorthWing_Connection_Lobby under Hotel_NorthWing_M1.");
            }
            UpdateRetainedWallSegmentInPlace(
                connLobby, "NorthWing_OuterWall_East",
                new Vector2(16.0f, 12.0f), new Vector2(16.0f, 26.735f),
                outerT, wallConcrete, baseboardConcrete);

            Transform anomalyRoot = EnsureHotelSubgroup(shellRoot, HotelRoom111BRootName);
            anomalyRoot.localPosition = Vector3.zero;
            anomalyRoot.localRotation = Quaternion.identity;
            anomalyRoot.localScale = Vector3.one;

            // =========================================================================
            // SUBGROUP 1: Room111B_ConcealedOpening
            //   Opening at X = +16.00, Z in [+26.80, +28.40] (1.60m clear width x 2.20m clear height).
            //   Offset concrete shadow-baffle wall at X = +17.55 (West face +17.43, Z = +25.40..+28.80)
            //   leaves a 1.30m clear South dogleg throat (2.03x player capsule diameter) while completely
            //   occluding the East Corridor from normal North Guest Corridor approach sightlines.
            // =========================================================================
            Transform openingGroup = EnsureHotelSubgroup(anomalyRoot, "Room111B_ConcealedOpening");

            // North continuation of East perimeter wall sealing Room 107 East wall (Z = +28.40..+34.065)
            EnsureHotelWallSegment(openingGroup, "Room111B_OuterWallEast_NorthStub",
                new Vector2(16.0f, 28.465f), new Vector2(16.0f, 34.0f), outerT, wallConcrete, baseboardConcrete);

            // Overhead concrete transom above the 2.20m clear opening (Y = -47.80..-47.15)
            EnsureHotelBox(openingGroup, "Room111B_Opening_Transom",
                new Vector3(16.0f, BaseY + 2.525f, 27.60f), Quaternion.identity,
                new Vector3(outerT, 0.65f, 1.64f), wallConcrete, keepCollider: true);

            // Restrained unfinished structural concrete recess jambs (blends as a solid recessed dead end from corridor approach)
            EnsureHotelBox(openingGroup, "Room111B_Opening_RevealS",
                new Vector3(16.00f, BaseY + 1.10f, 26.815f), Quaternion.identity,
                new Vector3(0.27f, 2.20f, 0.03f), baseboardConcrete, keepCollider: false);
            EnsureHotelBox(openingGroup, "Room111B_Opening_RevealN",
                new Vector3(16.00f, BaseY + 1.10f, 28.385f), Quaternion.identity,
                new Vector3(0.27f, 2.20f, 0.03f), baseboardConcrete, keepCollider: false);
            EnsureHotelBox(openingGroup, "Room111B_Opening_RevealTop",
                new Vector3(16.00f, BaseY + 2.205f, 27.60f), Quaternion.identity,
                new Vector3(0.27f, 0.03f, 1.60f), baseboardConcrete, keepCollider: false);

            // Offset concrete shadow-baffle wall (X = +17.55, thickness 0.24m -> West face +17.43, East +17.67)
            // Reads as a solid recessed concrete dead-end wall from corridor approach X <= +13.5
            EnsureHotelWallSegment(openingGroup, "Room111B_ConcealedBaffle_Wall",
                new Vector2(17.55f, 25.40f), new Vector2(17.55f, 28.80f), 0.24f, wallConcrete, baseboardConcrete);

            // North enclosure wall continuing Room 107's corridor wall line (Z = +28.80) across the recess
            EnsureHotelWallSegment(openingGroup, "Room111B_Opening_NorthSeal",
                new Vector2(16.0f, 28.80f), new Vector2(17.55f, 28.80f), 0.24f, wallConcrete, baseboardConcrete);

            // Transverse shoulder sealing the step-out from baffle X = +17.55 to corridor East wall X = +19.01 at Z = +25.40
            EnsureHotelWallSegment(openingGroup, "Room111B_Baffle_EastShoulder",
                new Vector2(17.55f, 25.40f), new Vector2(19.01f, 25.40f), wallT, wallConcrete, baseboardConcrete);

            // Subtle dark concrete floor expansion seam visible only on close inspection inside the recess (zero bright cue from afar)
            EnsureHotelBox(openingGroup, "Room111B_Opening_FloorJointCue",
                new Vector3(16.42f, BaseY + 0.002f, 26.82f), Quaternion.identity,
                new Vector3(0.84f, 0.004f, 0.032f), baseboardConcrete, keepCollider: false);

            // =========================================================================
            // SUBGROUP 2: Room111B_EastCorridor
            //   Phantom East Corridor extending South behind the Banquet Hall's East wall:
            //   Clear interior X in [+16.13, +18.90] (2.77m wide), Z in [+20.20, +28.68] (8.48m total length),
            //   floor Y = -50.00, clear height 2.85m. Zero exterior void gaps.
            // =========================================================================
            Transform corridorGroup = EnsureHotelSubgroup(anomalyRoot, "Room111B_EastCorridor");

            // Continuous floor & ceiling slabs (5cm overlap with NorthWing slabs, 0.5mm Y delta -> zero Z-fighting, zero seam gap)
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_FloorSlab",
                new Vector3(17.725f, BaseY - 0.1515f, 24.45f), Quaternion.identity,
                new Vector3(2.95f, 0.30f, 9.10f), floorConcrete, keepCollider: true);
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_CeilingSlab",
                new Vector3(17.725f, CeilingY + 0.1025f, 24.45f), Quaternion.identity,
                new Vector3(2.95f, 0.20f, 9.10f), ceilingSoffit, keepCollider: true);

            // Solid South end wall (inner North face at Z = +20.20, outer South face at Z = +19.98)
            EnsureHotelWallSegment(corridorGroup, "Room111B_EastCorr_SouthWall",
                new Vector2(16.00f, 20.09f), new Vector2(19.01f, 20.09f), wallT, wallConcrete, baseboardConcrete);

            // Engaged concrete structural pilaster on East face of NorthWing_OuterWall_East mirroring Banquet pilaster at Z = +22.40
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_WestPilaster",
                new Vector3(16.21f, BaseY + ClearHeight * 0.5f, 22.40f), Quaternion.identity,
                new Vector3(0.18f, ClearHeight, 0.34f), columnConcrete, keepCollider: true);

            // Subtle architectural irregularity #1: misaligned dark floor expansion-joint strip running off-axis down East Corridor
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_FloorJointMisaligned",
                new Vector3(16.82f, BaseY + 0.002f, 23.40f), Quaternion.identity,
                new Vector3(0.036f, 0.004f, 6.20f), baseboardConcrete, keepCollider: false);

            // Overhead concrete soffit beams (non-colliding)
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_Beam_0",
                new Vector3(17.52f, CeilingY - 0.16f, 22.40f), Quaternion.identity,
                new Vector3(2.80f, 0.32f, 0.28f), columnConcrete, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_Beam_1",
                new Vector3(17.52f, CeilingY - 0.16f, 25.40f), Quaternion.identity,
                new Vector3(2.80f, 0.32f, 0.28f), columnConcrete, keepCollider: false);

            // Single restrained emissive-only ceiling strip (ZERO Light components -> preserves exact 18-light budget)
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_FixtureStrip",
                new Vector3(17.52f, CeilingY - 0.07f, 22.80f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.36f, 0.046f, 0.068f), fluorOn, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_EastCorr_FixtureStrip_Backplate",
                new Vector3(17.52f, CeilingY - 0.024f, 22.80f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.44f, 0.046f, 0.14f), industrialMetal, keepCollider: false);

            // East dividing wall at X = +19.01 (West face +18.90, East face +19.12) with framed Room 111-B doorway (Z = +21.70..+23.10)
            // Wall ends trimmed flush to doorway opening (no jamb Z-fighting); full 1.40m masonry clear width x 2.20m clear height.
            // Subtle architectural irregularity #2: slightly older, more complete brushed-metal doorway jambs & floor threshold plate
            EnsureHotelWallSegment(corridorGroup, "Room111B_DoorWall_South",
                new Vector2(19.01f, 19.87f), new Vector2(19.01f, 21.645f), wallT, wallConcrete, baseboardConcrete);
            EnsureHotelWallSegment(corridorGroup, "Room111B_DoorWall_North",
                new Vector2(19.01f, 23.155f), new Vector2(19.01f, 25.40f), wallT, wallConcrete, baseboardConcrete);
            EnsureHotelBox(corridorGroup, "Room111B_DoorWall_Transom",
                new Vector3(19.01f, BaseY + 2.20f + 0.325f, 22.40f), Quaternion.identity,
                new Vector3(wallT, 0.65f, 1.44f), wallConcrete, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_DoorWall_JambS",
                new Vector3(19.01f, BaseY + 1.10f, 21.725f), Quaternion.identity,
                new Vector3(wallT + 0.06f, 2.20f, 0.05f), elevatorMetal, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_DoorWall_JambN",
                new Vector3(19.01f, BaseY + 1.10f, 23.075f), Quaternion.identity,
                new Vector3(wallT + 0.06f, 2.20f, 0.05f), elevatorMetal, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_DoorWall_HeaderTrim",
                new Vector3(19.01f, BaseY + 2.20f, 22.40f), Quaternion.identity,
                new Vector3(wallT + 0.06f, 0.055f, 1.46f), elevatorMetal, keepCollider: false);
            EnsureHotelBox(corridorGroup, "Room111B_Doorway_ThresholdPlate",
                new Vector3(19.01f, BaseY + 0.004f, 22.40f), Quaternion.identity,
                new Vector3(0.28f, 0.008f, 1.38f), industrialMetal, keepCollider: false);

            // =========================================================================
            // SUBGROUP 3: Room111B_RoomShell
            //   Empty architectural volume extending beyond original hotel East boundary (X = +20.0):
            //   Clear interior X in [+19.12, +24.52] (5.40m), Z in [+20.00, +24.80] (4.80m), 25.92 m².
            //   Weathered plaster wall finish contrasts subtly with raw corridor concrete.
            //   Strictly empty: ZERO furniture, ZERO props, ZERO documents, ZERO organisms, ZERO Light components.
            // =========================================================================
            Transform shellGroup = EnsureHotelSubgroup(anomalyRoot, "Room111B_RoomShell");

            // Continuous floor & ceiling slabs (5cm overlap with East Corridor slabs at X = +19.15..+19.20)
            EnsureHotelBox(shellGroup, "Room111B_Shell_FloorSlab",
                new Vector3(22.00f, BaseY - 0.1510f, 22.40f), Quaternion.identity,
                new Vector3(5.70f, 0.30f, 5.40f), floorConcrete, keepCollider: true);
            EnsureHotelBox(shellGroup, "Room111B_Shell_CeilingSlab",
                new Vector3(22.00f, CeilingY + 0.1020f, 22.40f), Quaternion.identity,
                new Vector3(5.70f, 0.20f, 5.40f), ceilingSoffit, keepCollider: true);

            // Full perimeter enclosure of Room 111-B (inner faces X=+24.52, Z=+20.00, Z=+24.80; West closed by Room111B_DoorWall X=+19.12)
            EnsureHotelWallSegment(shellGroup, "Room111B_Shell_WallEast",
                new Vector2(24.65f, 19.87f), new Vector2(24.65f, 24.93f), outerT, wallPlaster, baseboardConcrete);
            EnsureHotelWallSegment(shellGroup, "Room111B_Shell_WallSouth",
                new Vector2(19.01f, 19.87f), new Vector2(24.65f, 19.87f), outerT, wallPlaster, baseboardConcrete);
            EnsureHotelWallSegment(shellGroup, "Room111B_Shell_WallNorth",
                new Vector2(19.01f, 24.93f), new Vector2(24.65f, 24.93f), outerT, wallPlaster, baseboardConcrete);

            // Engaged East wall structural pilasters & overhead ceiling beam (pure empty concrete/plaster architecture)
            EnsureHotelBox(shellGroup, "Room111B_Shell_EastPilasterS",
                new Vector3(24.44f, BaseY + ClearHeight * 0.5f, 20.90f), Quaternion.identity,
                new Vector3(0.16f, ClearHeight, 0.34f), columnConcrete, keepCollider: true);
            EnsureHotelBox(shellGroup, "Room111B_Shell_EastPilasterN",
                new Vector3(24.44f, BaseY + ClearHeight * 0.5f, 23.90f), Quaternion.identity,
                new Vector3(0.16f, ClearHeight, 0.34f), columnConcrete, keepCollider: true);
            EnsureHotelBox(shellGroup, "Room111B_Shell_CeilingBeam",
                new Vector3(21.82f, CeilingY - 0.14f, 22.40f), Quaternion.identity,
                new Vector3(5.40f, 0.28f, 0.26f), columnConcrete, keepCollider: false);

            // Single restrained emissive-only ceiling strip cleanly mounted to underside of ceiling beam (warm utility tint, ZERO Light components)
            EnsureHotelBox(shellGroup, "Room111B_Shell_FixtureStrip",
                new Vector3(21.82f, CeilingY - 0.335f, 22.40f), Quaternion.identity,
                new Vector3(1.36f, 0.046f, 0.068f), utilityWarmOn, keepCollider: false);
            EnsureHotelBox(shellGroup, "Room111B_Shell_FixtureStrip_Backplate",
                new Vector3(21.82f, CeilingY - 0.295f, 22.40f), Quaternion.identity,
                new Vector3(1.44f, 0.040f, 0.14f), industrialMetal, keepCollider: false);

            // =========================================================================
            // SUBGROUP 4: Room111B_Signage
            //   Upright, non-mirrored '111-B' room plaque mounted beside the Room 111-B doorway
            //   on the West-facing East Corridor wall (X = +18.888, Z = +23.45), facing West (-X).
            // =========================================================================
            Transform signageGroup = EnsureHotelSubgroup(anomalyRoot, "Room111B_Signage");

            // Orient Unity Cube's upright +Z face West (-X) toward the player approaching along the East Corridor
            EnsureHotelBox(signageGroup, "Room111B_Plaque_111B",
                new Vector3(18.888f, BaseY + 1.58f, 23.45f), Quaternion.Euler(0f, -90f, 0f),
                new Vector3(0.28f, 0.11f, 0.016f), plaque111BMat, keepCollider: false);
        }

        private static string EnsureRoom111BPlaqueTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 192, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            // Slightly aged brushed-steel plaque matching Rooms 101-110 signage palette with subtle patina
            Color plate = new Color(0.65f, 0.65f, 0.63f);
            Color rim = new Color(0.23f, 0.24f, 0.25f);
            Color ink = new Color(0.13f, 0.14f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    float grain = (PeriodicHash(x, y, w, h, 7111) - 0.5f) * 0.045f;
                    Color c = border
                        ? rim
                        : new Color(Mathf.Clamp01(plate.r + grain), Mathf.Clamp01(plate.g + grain), Mathf.Clamp01(plate.b + grain));
                    tex.SetPixel(x, y, c);
                }
            }

            // Centered crisp 5x7 stencil lettering "111-B" at scale 5 (total width 145px inside 192px)
            DrawBitmapTextCentered(tex, w, h, "111-B", (h - 7 * 5) / 2, 5, ink, fadeLastChar: false);

            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        // =========================================================================
        // IMPLEMENTATION 07.12 — ROOM 111-B: THE GUEST ROOM & THRESHOLD OF TRUST
        // =========================================================================
        /// <summary>
        /// Strictly read-only Editor helper that opens BorderCorridorScene.unity WITHOUT modifying
        /// or saving it, renders a single 512x384 Av. Benito Juárez street view from just East of
        /// BuildingA_SouthBay_StorageDoor (-11.78, 1.55, -11.99 facing +X), applies a subtle smoked
        /// hotel-glass tint, and writes the pre-baked fallback PNG strictly inside Assets/Textures/Environment/.
        /// </summary>
        public static void BakeEntranceStreetFallbackFromBorderCorridor()
        {
            const string borderScenePath = "Assets/Scenes/BorderCorridorScene.unity";
            var borderScene = EditorSceneManager.OpenScene(borderScenePath, OpenSceneMode.Single);
            if (!borderScene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {borderScenePath} read-only to bake street fallback.");
            }

            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", EntranceStreetFallbackTexPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            int w = SecretBackroomsTransitionController.ExteriorObservationWidth;
            int h = SecretBackroomsTransitionController.ExteriorObservationHeight;

            GameObject camGo = new GameObject("__TempBakeStreetFallbackCam__");
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            try
            {
                camGo.transform.position = SecretBackroomsTransitionController.ExteriorObservationCamPos;
                camGo.transform.rotation = Quaternion.Euler(SecretBackroomsTransitionController.ExteriorObservationCamEuler);

                Camera cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 58f;
                cam.nearClipPlane = 0.18f;
                cam.farClipPlane = 65f;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = null;

                for (int y = 0; y < h; y++)
                {
                    float vy = (float)y / (h - 1);
                    for (int x = 0; x < w; x++)
                    {
                        float vx = (float)x / (w - 1);
                        Color p = tex.GetPixel(x, y);
                        float edgeDist = Mathf.Min(Mathf.Min(vx, 1f - vx), Mathf.Min(vy, 1f - vy));
                        float vig = Mathf.Lerp(0.76f, 1.0f, Mathf.SmoothStep(0f, 0.14f, edgeDist));
                        Color glass = new Color(
                            Mathf.Clamp01(p.r * 0.86f * vig),
                            Mathf.Clamp01(p.g * 0.89f * vig),
                            Mathf.Clamp01(p.b * 0.91f * vig));
                        tex.SetPixel(x, y, glass);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                Debug.Log($"[BackroomsLevel1Builder] Baked read-only Av. Benito Juárez fallback view to {EntranceStreetFallbackTexPath} (BorderCorridorScene untouched).");
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        [MenuItem("MyVirtualCommunity/Migrate Remanencia Room 111-B Interior & Threshold (In-Place)")]
        public static void MigrateRemanenciaRoom111BInteriorAndThresholdAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open existing scene at {ScenePath}.");
            }

            MigrateRemanenciaRoom111BInteriorAndThresholdInOpenScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save migrated scene at {ScenePath}.");
            }
            Debug.Log($"[BackroomsLevel1Builder] Remanencia Room 111-B interior, directory, Threshold of Trust & one-way entrance view migrated and saved to {ScenePath}.");
        }

        public static void MigrateRemanenciaRoom111BInteriorAndThresholdInOpenScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Remanencia Room 111-B 07.12 migration requires open scene '{ScenePath}', but found '{scene.path}'.");
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
            if (shellRoot == null || shellRoot.Find(HotelRoom111BRootName) == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{HotelRoom111BRootName}' under Backrooms_Architecture before running 07.12 migration.");
            }

            BuildOrUpdateRoom111BGuestInterior(root.transform);
        }

        private static void BuildOrUpdateRoom111BGuestInterior(Transform backroomsRoot)
        {
            Material columnConcrete = AssetDatabase.LoadAssetAtPath<Material>(ColumnConcreteMatPath);
            Material industrialMetal = AssetDatabase.LoadAssetAtPath<Material>(IndustrialMetalMatPath);
            Material elevatorMetal = AssetDatabase.LoadAssetAtPath<Material>(ElevatorDoorMetalMatPath);
            Material agedCeramic = AssetDatabase.LoadAssetAtPath<Material>(AgedCeramicPatchMatPath);
            if (columnConcrete == null || industrialMetal == null || elevatorMetal == null || agedCeramic == null)
            {
                throw new InvalidOperationException("Missing canonical Remanencia materials required for 07.12.");
            }

            string dirTexPath = EnsureArchitecturalSignTexture(
                NorthCorridorDirectoryTexPath, 512, 112,
                NorthCorridorDirectoryEs, NorthCorridorDirectoryRooms,
                darkBronzeStyle: false, weatheredIncomplete: false, fadeLastChar: false);
            string floorTileTexPath = EnsureRoom111BFloorTileTexture(Room111BFloorTileTexPath);
            string hazeTexPath = EnsureThresholdOvercastHazeTexture(ThresholdOvercastTexPath);
            string streetFallbackFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", EntranceStreetFallbackTexPath));
            if (!File.Exists(streetFallbackFull))
            {
                EnsureFallbackSyntheticStreetTexture(EntranceStreetFallbackTexPath);
            }
            AssetDatabase.Refresh();

            ConfigureSignTextureImporter(dirTexPath);
            ConfigureArchitecturalTextureImporter(floorTileTexPath);
            ConfigureSignTextureImporter(hazeTexPath);
            ConfigureSignTextureImporter(EntranceStreetFallbackTexPath);

            Material dirMat = CreateTexturedMat(NorthCorridorDirectoryMatPath, dirTexPath, Color.white, 0.28f, Vector2.one);
            Material floorTileMat = CreateTexturedMat(Room111BFloorTileMatPath, floorTileTexPath, new Color(0.95f, 0.92f, 0.88f), 0.24f, new Vector2(4.2f, 3.8f));
            Material woodMat = CreateColorMat(Room111BWoodMatPath, new Color(0.30f, 0.22f, 0.16f), 0.22f, 0.0f);
            Material linenMat = CreateColorMat(Room111BLinenMatPath, new Color(0.76f, 0.74f, 0.69f), 0.08f, 0.0f);
            Material blanketMat = CreateColorMat(Room111BBlanketMatPath, new Color(0.45f, 0.34f, 0.29f), 0.06f, 0.0f);
            Material mirrorMat = CreateColorMat(Room111BMirrorMatPath, new Color(0.32f, 0.35f, 0.37f), 0.72f, 0.78f);
            Material thresholdHazeMat = CreateEmissiveTexturedMat(
                ThresholdOvercastMatPath, hazeTexPath,
                new Color(0.90f, 0.92f, 0.94f),
                new Color(0.74f, 0.78f, 0.82f));
            Material oneWayGlassMat = CreateEmissiveTexturedMat(
                EntranceOneWayGlassMatPath, EntranceStreetFallbackTexPath,
                new Color(0.88f, 0.90f, 0.92f),
                new Color(0.55f, 0.59f, 0.63f));

            Transform interiorRoot = EnsureHotelSubgroup(backroomsRoot, HotelRoom111BGuestInteriorRootName);
            interiorRoot.localPosition = Vector3.zero;
            interiorRoot.localRotation = Quaternion.identity;
            interiorRoot.localScale = Vector3.one;

            // 1. PUBLIC NORTH GUEST CORRIDOR ROOM DIRECTORY (107-110 ONLY — EXCLUDES SECRET 111-B)
            Transform dirGroup = EnsureHotelSubgroup(interiorRoot, "Room111B_NorthCorridorDirectory");
            EnsureHotelBox(dirGroup, "Hotel_NorthCorridor_PublicDirectory",
                new Vector3(9.40f, BaseY + 1.62f, 28.678f), Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.58f, 0.24f, 0.016f), dirMat, keepCollider: false);

            // 2. ROOM 111-B MODEST PREPARED HOTEL GUEST ROOM INTERIOR
            Transform furnGroup = EnsureHotelSubgroup(interiorRoot, "Room111B_FurnitureAndFinishes");

            // Subtle Discrepancy #1: Aged ceramic floor tile inlay rotated 1.4 deg off corridor doorway axis
            EnsureHotelBox(furnGroup, "Room111B_Interior_FloorFinish",
                new Vector3(21.86f, BaseY + 0.0025f, 22.40f), Quaternion.Euler(0f, 1.4f, 0f),
                new Vector3(4.92f, 0.005f, 4.46f), floorTileMat, keepCollider: false);

            // 2A. Modest Double Bed (South Wall, X in [20.56, 21.96], Z in [20.02, 21.80])
            Transform bedGroup = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_DoubleBed");
            EnsureHotelBox(bedGroup, "DoubleBed_Base",
                new Vector3(21.26f, BaseY + 0.20f, 20.91f), Quaternion.identity,
                new Vector3(1.40f, 0.40f, 1.78f), woodMat, keepCollider: true);
            EnsureHotelBox(bedGroup, "DoubleBed_Headboard",
                new Vector3(21.26f, BaseY + 0.54f, 20.05f), Quaternion.identity,
                new Vector3(1.46f, 1.08f, 0.06f), woodMat, keepCollider: false);
            EnsureHotelBox(bedGroup, "DoubleBed_Mattress",
                new Vector3(21.26f, BaseY + 0.47f, 20.92f), Quaternion.identity,
                new Vector3(1.34f, 0.16f, 1.70f), linenMat, keepCollider: false);
            EnsureHotelBox(bedGroup, "DoubleBed_BlanketFold",
                new Vector3(21.26f, BaseY + 0.505f, 21.38f), Quaternion.identity,
                new Vector3(1.37f, 0.145f, 0.81f), blanketMat, keepCollider: false);
            EnsureHotelBox(bedGroup, "DoubleBed_PillowW",
                new Vector3(20.92f, BaseY + 0.60f, 20.28f), Quaternion.Euler(8f, 0f, 0f),
                new Vector3(0.54f, 0.11f, 0.34f), linenMat, keepCollider: false);
            EnsureHotelBox(bedGroup, "DoubleBed_PillowE",
                new Vector3(21.60f, BaseY + 0.60f, 20.28f), Quaternion.Euler(8f, 0f, 0f),
                new Vector3(0.54f, 0.11f, 0.34f), linenMat, keepCollider: false);

            // 2B. Two Simple Bedside Tables flanking bed along South wall
            Transform bedsideW = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_BedsideW");
            EnsureHotelBox(bedsideW, "BedsideW_Body",
                new Vector3(20.25f, BaseY + 0.28f, 20.25f), Quaternion.identity,
                new Vector3(0.46f, 0.56f, 0.46f), woodMat, keepCollider: true);
            EnsureHotelBox(bedsideW, "BedsideW_Top",
                new Vector3(20.25f, BaseY + 0.575f, 20.25f), Quaternion.identity,
                new Vector3(0.50f, 0.03f, 0.50f), woodMat, keepCollider: false);
            EnsureHotelBox(bedsideW, "BedsideW_Pull",
                new Vector3(20.25f, BaseY + 0.42f, 20.49f), Quaternion.identity,
                new Vector3(0.10f, 0.02f, 0.02f), industrialMetal, keepCollider: false);

            Transform bedsideE = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_BedsideE");
            EnsureHotelBox(bedsideE, "BedsideE_Body",
                new Vector3(22.28f, BaseY + 0.28f, 20.25f), Quaternion.identity,
                new Vector3(0.46f, 0.56f, 0.46f), woodMat, keepCollider: true);
            EnsureHotelBox(bedsideE, "BedsideE_Top",
                new Vector3(22.28f, BaseY + 0.575f, 20.25f), Quaternion.identity,
                new Vector3(0.50f, 0.03f, 0.50f), woodMat, keepCollider: false);
            EnsureHotelBox(bedsideE, "BedsideE_Pull",
                new Vector3(22.28f, BaseY + 0.42f, 20.49f), Quaternion.identity,
                new Vector3(0.10f, 0.02f, 0.02f), industrialMetal, keepCollider: false);

            // 2C. Modest Built-In Wardrobe / Closet (NW West Wall, Subtle Discrepancy #2: impossible inset depth)
            Transform wardrobe = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_Wardrobe");
            EnsureHotelBox(wardrobe, "Wardrobe_Carcass",
                new Vector3(19.46f, BaseY + 1.09f, 24.10f), Quaternion.identity,
                new Vector3(0.64f, 2.18f, 1.32f), woodMat, keepCollider: true);
            EnsureHotelBox(wardrobe, "Wardrobe_DoorS",
                new Vector3(19.785f, BaseY + 1.08f, 23.78f), Quaternion.identity,
                new Vector3(0.024f, 2.04f, 0.60f), woodMat, keepCollider: false);
            EnsureHotelBox(wardrobe, "Wardrobe_DoorN",
                new Vector3(19.785f, BaseY + 1.08f, 24.42f), Quaternion.identity,
                new Vector3(0.024f, 2.04f, 0.60f), woodMat, keepCollider: false);
            EnsureHotelBox(wardrobe, "Wardrobe_CenterGap",
                new Vector3(19.796f, BaseY + 1.08f, 24.10f), Quaternion.identity,
                new Vector3(0.008f, 2.04f, 0.014f), industrialMetal, keepCollider: false);
            EnsureHotelBox(wardrobe, "Wardrobe_HandleS",
                new Vector3(19.805f, BaseY + 1.08f, 24.03f), Quaternion.identity,
                new Vector3(0.02f, 0.14f, 0.02f), industrialMetal, keepCollider: false);
            EnsureHotelBox(wardrobe, "Wardrobe_HandleN",
                new Vector3(19.805f, BaseY + 1.08f, 24.17f), Quaternion.identity,
                new Vector3(0.02f, 0.14f, 0.02f), industrialMetal, keepCollider: false);

            // 2D. Compact Writing Desk (North Wall, X in [20.92, 22.18], Z in [24.22, 24.76])
            Transform desk = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_Desk");
            EnsureHotelBox(desk, "Desk_Top",
                new Vector3(21.55f, BaseY + 0.74f, 24.49f), Quaternion.identity,
                new Vector3(1.26f, 0.05f, 0.54f), woodMat, keepCollider: true);
            EnsureHotelBox(desk, "Desk_LegW",
                new Vector3(20.96f, BaseY + 0.36f, 24.49f), Quaternion.identity,
                new Vector3(0.06f, 0.72f, 0.50f), woodMat, keepCollider: true);
            EnsureHotelBox(desk, "Desk_LegE",
                new Vector3(22.14f, BaseY + 0.36f, 24.49f), Quaternion.identity,
                new Vector3(0.06f, 0.72f, 0.50f), woodMat, keepCollider: true);
            EnsureHotelBox(desk, "Desk_DrawerBox",
                new Vector3(21.92f, BaseY + 0.58f, 24.49f), Quaternion.identity,
                new Vector3(0.38f, 0.24f, 0.48f), woodMat, keepCollider: false);
            EnsureHotelBox(desk, "Desk_DrawerPull",
                new Vector3(21.92f, BaseY + 0.58f, 24.24f), Quaternion.identity,
                new Vector3(0.09f, 0.02f, 0.02f), industrialMetal, keepCollider: false);

            // 2E. Simple Wooden Chair neatly tucked under desk (X in [21.33, 21.77], Z in [24.18, 24.58])
            Transform chair = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_Chair");
            EnsureHotelBox(chair, "Chair_Seat",
                new Vector3(21.55f, BaseY + 0.45f, 24.38f), Quaternion.identity,
                new Vector3(0.44f, 0.04f, 0.40f), woodMat, keepCollider: true);
            EnsureHotelBox(chair, "Chair_Backrest",
                new Vector3(21.55f, BaseY + 0.69f, 24.20f), Quaternion.identity,
                new Vector3(0.44f, 0.46f, 0.04f), woodMat, keepCollider: false);
            EnsureHotelBox(chair, "Chair_LegSW", new Vector3(21.36f, BaseY + 0.225f, 24.21f), Quaternion.identity, new Vector3(0.04f, 0.45f, 0.04f), woodMat, keepCollider: false);
            EnsureHotelBox(chair, "Chair_LegSE", new Vector3(21.74f, BaseY + 0.225f, 24.21f), Quaternion.identity, new Vector3(0.04f, 0.45f, 0.04f), woodMat, keepCollider: false);
            EnsureHotelBox(chair, "Chair_LegNW", new Vector3(21.36f, BaseY + 0.225f, 24.55f), Quaternion.identity, new Vector3(0.04f, 0.45f, 0.04f), woodMat, keepCollider: false);
            EnsureHotelBox(chair, "Chair_LegNE", new Vector3(21.74f, BaseY + 0.225f, 24.55f), Quaternion.identity, new Vector3(0.04f, 0.45f, 0.04f), woodMat, keepCollider: false);

            // 2F. Restrained Wall Mirror above desk (North Wall, zero reflection camera/RT cost)
            Transform mirror = EnsureHotelSubgroup(furnGroup, "Room111B_Furn_WallMirror");
            EnsureHotelBox(mirror, "WallMirror_Frame",
                new Vector3(21.55f, BaseY + 1.46f, 24.775f), Quaternion.identity,
                new Vector3(0.76f, 0.94f, 0.03f), woodMat, keepCollider: false);
            EnsureHotelBox(mirror, "WallMirror_Glass",
                new Vector3(21.55f, BaseY + 1.46f, 24.756f), Quaternion.identity,
                new Vector3(0.66f, 0.84f, 0.012f), mirrorMat, keepCollider: false);

            // 3. THE THRESHOLD OF TRUST — UNCONVENTIONAL EXIT INSIDE ROOM 111-B
            Transform thresholdGroup = EnsureHotelSubgroup(interiorRoot, "Room111B_ThresholdOfTrust");

            EnsureHotelBox(thresholdGroup, "Threshold_RecessJambS",
                new Vector3(24.24f, BaseY + 1.17f, 21.71f), Quaternion.identity,
                new Vector3(0.56f, 2.34f, 0.08f), columnConcrete, keepCollider: true);
            EnsureHotelBox(thresholdGroup, "Threshold_RecessJambN",
                new Vector3(24.24f, BaseY + 1.17f, 23.09f), Quaternion.identity,
                new Vector3(0.56f, 2.34f, 0.08f), columnConcrete, keepCollider: true);
            EnsureHotelBox(thresholdGroup, "Threshold_RecessHeader",
                new Vector3(24.24f, BaseY + 2.39f, 22.40f), Quaternion.identity,
                new Vector3(0.58f, 0.14f, 1.48f), columnConcrete, keepCollider: false);

            EnsureHotelBox(thresholdGroup, "Threshold_InnerRevealS",
                new Vector3(24.25f, BaseY + 1.16f, 21.755f), Quaternion.identity,
                new Vector3(0.50f, 2.30f, 0.015f), agedCeramic, keepCollider: false);
            EnsureHotelBox(thresholdGroup, "Threshold_InnerRevealN",
                new Vector3(24.25f, BaseY + 1.16f, 23.045f), Quaternion.identity,
                new Vector3(0.50f, 2.30f, 0.015f), agedCeramic, keepCollider: false);
            EnsureHotelBox(thresholdGroup, "Threshold_InnerRevealTop",
                new Vector3(24.25f, BaseY + 2.315f, 22.40f), Quaternion.identity,
                new Vector3(0.50f, 0.015f, 1.29f), agedCeramic, keepCollider: false);

            EnsureHotelBox(thresholdGroup, "Threshold_FloorSeam",
                new Vector3(23.08f, BaseY + 0.006f, 22.40f), Quaternion.identity,
                new Vector3(2.86f, 0.005f, 0.048f), industrialMetal, keepCollider: false);
            EnsureHotelBox(thresholdGroup, "Threshold_SillPlate",
                new Vector3(23.98f, BaseY + 0.0065f, 22.40f), Quaternion.identity,
                new Vector3(0.12f, 0.007f, 1.32f), elevatorMetal, keepCollider: false);

            EnsureHotelBox(thresholdGroup, "Threshold_OvercastDaylightPlane",
                new Vector3(24.495f, BaseY + 1.16f, 22.40f), Quaternion.Euler(0f, -90f, 0f),
                new Vector3(1.28f, 2.30f, 0.015f), thresholdHazeMat, keepCollider: false);

            Transform trigTf = thresholdGroup.Find("Room111B_Threshold_TriggerVolume");
            GameObject trigGo;
            if (trigTf != null)
            {
                trigGo = trigTf.gameObject;
            }
            else
            {
                trigGo = new GameObject("Room111B_Threshold_TriggerVolume");
                trigGo.transform.SetParent(thresholdGroup, false);
            }
            trigGo.transform.position = new Vector3(24.26f, BaseY + 1.15f, 22.40f);
            trigGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            trigGo.transform.localScale = Vector3.one;

            BoxCollider trigBox = trigGo.GetComponent<BoxCollider>();
            if (trigBox == null) trigBox = trigGo.AddComponent<BoxCollider>();
            trigBox.isTrigger = true;
            trigBox.center = Vector3.zero;
            trigBox.size = new Vector3(1.10f, 2.20f, 0.44f);

            RemanenciaThresholdOfTrustTrigger trustTrigger = trigGo.GetComponent<RemanenciaThresholdOfTrustTrigger>();
            if (trustTrigger == null) trustTrigger = trigGo.AddComponent<RemanenciaThresholdOfTrustTrigger>();
            trustTrigger.crossingPlaneX = 23.92f;
            trustTrigger.minZ = 21.80f;
            trustTrigger.maxZ = 23.00f;
            trustTrigger.floorY = BaseY;
            trustTrigger.maxVerticalOffset = 1.60f;

            // 4. INTERIOR-ONLY HOTEL ENTRANCE APPEARANCE & ONE-WAY OBSERVATION ILLUSION
            Transform entGroup = EnsureHotelSubgroup(interiorRoot, "Room111B_InteriorEntranceIllusion");
            entGroup.position = new Vector3(19.79f, BaseY, 8.80f);
            entGroup.rotation = Quaternion.Euler(0f, -90f, 0f);
            entGroup.localScale = Vector3.one;

            EnsureHotelBox(entGroup, "InteriorEntrance_FrameL",
                entGroup.TransformPoint(new Vector3(-0.74f, 1.08f, 0.02f)), entGroup.rotation,
                new Vector3(0.09f, 2.16f, 0.09f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_FrameR",
                entGroup.TransformPoint(new Vector3(0.74f, 1.08f, 0.02f)), entGroup.rotation,
                new Vector3(0.09f, 2.16f, 0.09f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_FrameTop",
                entGroup.TransformPoint(new Vector3(0.00f, 2.14f, 0.02f)), entGroup.rotation,
                new Vector3(1.56f, 0.09f, 0.09f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_CenterMullion",
                entGroup.TransformPoint(new Vector3(0.00f, 1.06f, 0.035f)), entGroup.rotation,
                new Vector3(0.06f, 2.08f, 0.06f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_Kickplate",
                entGroup.TransformPoint(new Vector3(0.00f, 0.19f, 0.035f)), entGroup.rotation,
                new Vector3(1.40f, 0.38f, 0.055f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_MidRail",
                entGroup.TransformPoint(new Vector3(0.00f, 1.02f, 0.042f)), entGroup.rotation,
                new Vector3(1.40f, 0.055f, 0.045f), industrialMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_HandleL",
                entGroup.TransformPoint(new Vector3(-0.11f, 1.06f, 0.075f)), entGroup.rotation,
                new Vector3(0.024f, 0.34f, 0.036f), elevatorMetal, keepCollider: false);
            EnsureHotelBox(entGroup, "InteriorEntrance_HandleR",
                entGroup.TransformPoint(new Vector3(0.11f, 1.06f, 0.075f)), entGroup.rotation,
                new Vector3(0.024f, 0.34f, 0.036f), elevatorMetal, keepCollider: false);

            // Remove legacy split glass panes if migrating from earlier draft
            Transform oldPaneL = entGroup.Find("InteriorEntrance_GlassPane_L");
            if (oldPaneL != null) UnityEngine.Object.DestroyImmediate(oldPaneL.gameObject);
            Transform oldPaneR = entGroup.Find("InteriorEntrance_GlassPane_R");
            if (oldPaneR != null) UnityEngine.Object.DestroyImmediate(oldPaneR.gameObject);

            // Single continuous true-aspect Av. Benito Juárez observation glass plane framed behind the hotel double-door mullions
            GameObject unifiedPane = EnsureHotelBox(entGroup, "InteriorEntrance_GlassPane_Unified",
                entGroup.TransformPoint(new Vector3(0.00f, 1.24f, 0.014f)), entGroup.rotation,
                new Vector3(1.36f, 1.72f, 0.012f), oneWayGlassMat, keepCollider: false);

            RemanenciaOneWayEntranceView oneWayView = entGroup.GetComponent<RemanenciaOneWayEntranceView>();
            if (oneWayView == null) oneWayView = entGroup.gameObject.AddComponent<RemanenciaOneWayEntranceView>();
            oneWayView.unifiedGlassPane = unifiedPane.GetComponent<Renderer>();
        }

        private static string EnsureRoom111BFloorTileTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color grout = new Color(0.28f, 0.26f, 0.24f);
            Color tileA = new Color(0.55f, 0.49f, 0.43f);
            Color tileB = new Color(0.51f, 0.46f, 0.41f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int gx = x % 64;
                    int gy = y % 64;
                    bool isGrout = gx < 3 || gy < 3;
                    int cellId = (x / 64) + (y / 64) * 4;
                    Color baseCol = ((x / 64 + y / 64) % 2 == 0) ? tileA : tileB;
                    float speckle = (PeriodicHash(x, y, w, h, 7121 + cellId * 17) - 0.5f) * 0.055f;
                    float wear = (PeriodicFbm((float)x / w, (float)y / h, 4, 4, 2, 7129) - 0.5f) * 0.06f;
                    Color c = isGrout
                        ? grout
                        : new Color(
                            Mathf.Clamp01(baseCol.r + speckle + wear),
                            Mathf.Clamp01(baseCol.g + speckle + wear),
                            Mathf.Clamp01(baseCol.b + speckle + wear));
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureThresholdOvercastHazeTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color groundCol = new Color(0.62f, 0.62f, 0.60f);
            Color horizonCol = new Color(0.88f, 0.89f, 0.87f);
            Color upperSkyCol = new Color(0.75f, 0.80f, 0.85f);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                Color band = v < 0.36f
                    ? Color.Lerp(groundCol, horizonCol, Mathf.SmoothStep(0.05f, 0.36f, v))
                    : Color.Lerp(horizonCol, upperSkyCol, Mathf.SmoothStep(0.36f, 0.96f, v));

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    float mist = (PeriodicFbm(u, v, 3, 3, 2, 7127) - 0.5f) * 0.05f;
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float softVignette = Mathf.Lerp(0.84f, 1.0f, Mathf.SmoothStep(0f, 0.12f, edge));
                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01((band.r + mist) * softVignette),
                        Mathf.Clamp01((band.g + mist) * softVignette),
                        Mathf.Clamp01((band.b + mist) * softVignette)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureFallbackSyntheticStreetTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 256, h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                Color c = v < 0.42f
                    ? Color.Lerp(new Color(0.26f, 0.27f, 0.28f), new Color(0.50f, 0.49f, 0.46f), v / 0.42f)
                    : Color.Lerp(new Color(0.58f, 0.62f, 0.66f), new Color(0.46f, 0.54f, 0.64f), (v - 0.42f) / 0.58f);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static void BuildOrUpdateHotelFirstLivingMicrofauna(Transform backroomsRoot)
        {
            // Reuse existing 07.7 512x512 procedural texture (ZERO new textures, ZERO emission)
            const string existingFungalAtlasTex = "Assets/Textures/Environment/T_Remanencia_Ecology_FungalCap.png";
            Material bodyMat = CreateOpaqueTexturedMat(MicrofaunaBodyMatPath, existingFungalAtlasTex, new Color(0.72f, 0.67f, 0.59f), 0.28f);
            Mesh bodyMesh = EnsureEcologyMeshAsset(MicrofaunaBodyMeshPath, BuildProceduralMicrofaunaBodyMesh);

            Transform microRoot = EnsureHotelSubgroup(backroomsRoot, HotelFirstLivingMicrofaunaRootName);

            // =========================================================================
            // PRIMARY ENCOUNTER — ZONE C: HOUSEKEEPING UTILITY ROOM (SW Pipe Risers)
            //   Concealed at (-15.765, BaseY+0.001, 18.445) behind Copper Pipe Riser A
            //   (-15.72, 18.45, Ø 0.082m) & its floor flange (Ø 0.160m). Emerges ~17.5 cm
            //   beside Faunal_Utility_RiserBase_ResidueA toward the mycelium edge, pauses
            //   briefly with a subtle heading shift, and retreats back behind the copper riser.
            //   > 5.2 m from Hotel_Evidence_Cobalt60RumorNote (-12.75, BaseY+1.38, 23.27).
            // =========================================================================
            Vector3[] utilWaypoints =
            {
                new Vector3(-15.765f, BaseY + 0.001f, 18.445f), // [0] Occluded behind Copper Riser A & floor flange
                new Vector3(-15.655f, BaseY + 0.001f, 18.385f), // [1] Emerges past SE rim of riser flange
                new Vector3(-15.595f, BaseY + 0.001f, 18.435f)  // [2] Brief pause near mycelium/cuticle edge (~17.5 cm excursion)
            };
            EnsureMicrofaunaEncounter(
                microRoot, "Microfauna_Utility_Encounter", bodyMesh, bodyMat,
                utilWaypoints, Vector3.up,
                activationRadius: 4.2f, deactivationRadius: 5.6f, startleRetreatRadius: 0.85f,
                crawlSpeed: 0.082f, retreatSpeedMult: 1.45f,
                idleMin: 1.15f, idleMax: 2.30f, pauseSec: 0.85f,
                cooldownMin: 11.5f, cooldownMax: 15.5f, phaseSeed: 7901);

            // =========================================================================
            // SECONDARY ENCOUNTER — ZONE B: MAINTENANCE CORRIDOR (Baseboard / Residue Crevice)
            //   Concealed at (-8.738, BaseY+0.001, 21.845) inside the concrete baseboard seam
            //   & dark embedded residue crust (Faunal_Maint_WallFloorJoint_ResidueA at -8.775, 21.84).
            //   Emerges ~14.8 cm along the damp floor seam toward the mycelium edge, pauses briefly,
            //   and retreats cleanly back beneath the baseboard/residue recess.
            //   > 1.45 m below Hotel_Evidence_MaintenanceRecord (-8.73, BaseY+1.46, 22.45).
            // =========================================================================
            Vector3[] maintWaypoints =
            {
                new Vector3(-8.712f, BaseY + 0.001f, 21.845f), // [0] Fully occluded inside concrete baseboard wall-floor seam
                new Vector3(-8.815f, BaseY + 0.001f, 21.890f), // [1] Slips out past dark residue edge
                new Vector3(-8.855f, BaseY + 0.001f, 21.955f)  // [2] Brief pause near mycelium margin (~18.8 cm path)
            };
            EnsureMicrofaunaEncounter(
                microRoot, "Microfauna_Maintenance_Encounter", bodyMesh, bodyMat,
                maintWaypoints, Vector3.up,
                activationRadius: 3.6f, deactivationRadius: 5.0f, startleRetreatRadius: 0.85f,
                crawlSpeed: 0.076f, retreatSpeedMult: 1.50f,
                idleMin: 1.35f, idleMax: 2.55f, pauseSec: 0.75f,
                cooldownMin: 12.5f, cooldownMax: 17.0f, phaseSeed: 7902);
        }

        private static GameObject EnsureMicrofaunaEncounter(
            Transform parent, string name, Mesh bodyMesh, Material bodyMat,
            Vector3[] waypoints, Vector3 surfaceNormal,
            float activationRadius, float deactivationRadius, float startleRetreatRadius,
            float crawlSpeed, float retreatSpeedMult,
            float idleMin, float idleMax, float pauseSec,
            float cooldownMin, float cooldownMax, int phaseSeed)
        {
            Vector3 initialForward = Vector3.ProjectOnPlane(waypoints[1] - waypoints[0], surfaceNormal);
            if (initialForward.sqrMagnitude < 1e-6f) initialForward = Vector3.forward;
            Quaternion initialRot = Quaternion.LookRotation(initialForward.normalized, surfaceNormal.normalized);

            GameObject go = EnsureOrganicMeshObject(parent, name, bodyMesh, bodyMat, waypoints[0], initialRot, Vector3.one);

            RemanenciaAmbientMicrofaunaController ctrl = go.GetComponent<RemanenciaAmbientMicrofaunaController>();
            if (ctrl == null) ctrl = go.AddComponent<RemanenciaAmbientMicrofaunaController>();

            ctrl.waypoints = waypoints;
            ctrl.surfaceNormal = surfaceNormal.normalized;
            ctrl.activationRadius = activationRadius;
            ctrl.deactivationRadius = deactivationRadius;
            ctrl.startleRetreatRadius = startleRetreatRadius;
            ctrl.crawlSpeed = crawlSpeed;
            ctrl.retreatSpeedMultiplier = retreatSpeedMult;
            ctrl.idleMinSeconds = idleMin;
            ctrl.idleMaxSeconds = idleMax;
            ctrl.pauseSeconds = pauseSec;
            ctrl.cooldownMinSeconds = cooldownMin;
            ctrl.cooldownMaxSeconds = cooldownMax;
            ctrl.phaseSeed = phaseSeed;
            ctrl.undulationFrequency = 11.5f;
            ctrl.lateralYawAmplitudeDeg = 4.5f;
            ctrl.axialPulseAmplitude = 0.055f;
            ctrl.pauseProbeYawDeg = 14.0f;
            ctrl.ResetToConcealedState();

            // Guarantee zero unwanted physics/gameplay components
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            return go;
        }

        private static Mesh BuildProceduralMicrofaunaBodyMesh()
        {
            // Compact, elongated, subtly 7-segmented unidentified micro-organism (~3.6 cm long, 1.1 cm wide, 0.85 cm tall).
            // Local origin (0,0,0) rests flush on crawling surface (+Y up, +Z forward).
            // Strictly ambiguous anatomy: NO eyes, NO face, NO distinct head capsule, NO wings, NO antennae,
            // NO mandibles, NO spikes, NO insect/spider/larval silhouette.
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            const float bodyLength = 0.036f; // 3.6 cm (within 2.5 - 4.0 cm requirement)
            const int lengthSteps = 28;
            const int radialSteps = 12;

            int gridStart = verts.Count;
            for (int i = 0; i <= lengthSteps; i++)
            {
                float tl = (float)i / lengthSteps; // 0 = posterior tip, 1 = blunt anterior pole
                float z = (tl - 0.5f) * bodyLength;

                // Smooth asymmetric fusiform envelope (slightly fuller mid-anterior, smoothly tapered posterior)
                float baseEnv = Mathf.Sin(Mathf.Pow(tl, 0.82f) * Mathf.PI);
                // 7 subtle organic segmentation constrictions along the dorsal carapace
                float segRipple = (tl > 0.08f && tl < 0.92f)
                    ? (0.91f + 0.09f * Mathf.Abs(Mathf.Sin(tl * Mathf.PI * 7f)))
                    : 1f;

                float halfW = 0.0055f * baseEnv * segRipple;
                float height = 0.0082f * baseEnv * (0.94f + 0.06f * segRipple);
                float ventralLift = 0.0008f + 0.0006f * (1f - baseEnv);

                for (int j = 0; j <= radialSteps; j++)
                {
                    float ta = (float)j / radialSteps;
                    float ang = ta * Mathf.PI * 2f; // 0 = right, PI/2 = dorsal crest, PI = left, 3PI/2 = ventral belly
                    float cx = Mathf.Cos(ang);
                    float sy = Mathf.Sin(ang);

                    // Gently flattened ventral belly hugging surface, smoothly arched dorsal back
                    float y = sy >= 0f
                        ? ventralLift + sy * height
                        : ventralLift + sy * (height * 0.14f);
                    float x = cx * halfW;

                    verts.Add(new Vector3(x, Mathf.Max(0.0003f, y), z));
                    // UV v mapped to T_Remanencia_Ecology_FungalCap.png: dorsal crest warm pale taupe (0.64..0.86),
                    // dark natural contact shadow under belly (0.30..0.44)
                    float dorsalFactor = 0.5f + 0.5f * sy;
                    float vCoord = Mathf.Lerp(0.32f, 0.86f, dorsalFactor);
                    uvs.Add(new Vector2(tl, vCoord));
                }
            }

            int stride = radialSteps + 1;
            for (int i = 0; i < lengthSteps; i++)
            {
                int r0 = gridStart + i * stride;
                int r1 = gridStart + (i + 1) * stride;
                for (int j = 0; j < radialSteps; j++)
                {
                    tris.Add(r0 + j); tris.Add(r1 + j); tris.Add(r0 + j + 1);
                    tris.Add(r0 + j + 1); tris.Add(r1 + j); tris.Add(r1 + j + 1);
                }
            }

            // 4 pairs of tiny, subtle recessed ventral locomotion nubs/pads (2.6 mm, tucked low beneath mid segments)
            float[] padZs = { -0.0085f, -0.0025f, 0.0035f, 0.0090f };
            for (int p = 0; p < padZs.Length; p++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pRoot = new Vector3(side * 0.0032f, 0.0022f, padZs[p]);
                    Vector3 pMid = new Vector3(side * 0.0056f, 0.0014f, padZs[p] + 0.0004f);
                    Vector3 pTip = new Vector3(side * 0.0066f, 0.0002f, padZs[p] + 0.0006f);
                    AddCurvedTubeWithVRange(verts, uvs, tris,
                        new[] { pRoot, pMid, pTip },
                        new[] { 0.00095f, 0.00075f, 0.00045f },
                        sides: 4, vStart: 0.34f, vEnd: 0.48f);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static GameObject EnsureOrganicMeshObject(
            Transform parent, string name, Mesh mesh, Material mat, Vector3 worldPos, Quaternion worldRot, Vector3 scale)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = scale;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;

            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            return go;
        }

        private static Mesh EnsureEcologyMeshAsset(string assetPath, Func<Mesh> generator)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            Mesh built = generator();
            built.name = Path.GetFileNameWithoutExtension(assetPath);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = built.vertices;
                existing.normals = built.normals;
                existing.uv = built.uv;
                existing.triangles = built.triangles;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(built);
                return existing;
            }

            AssetDatabase.CreateAsset(built, assetPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }

        private static Mesh BuildProceduralMyceliumWebMesh()
        {
            // Oriented in local XY with +Z pointing strictly outward from the host surface (clockwise from +Z):
            //   1) Conformal 1x1 decal quad sampling the 512x512 RGBA asymmetric dendritic hyphal fan texture
            //   2) 6 sinuous, meandering, ultra-fine 3D rhizomorph cord veins hugging the surface (zero radial spikes!)
            var verts = new List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0.001f),
                new Vector3( 0.5f, -0.5f, 0.001f),
                new Vector3(-0.5f,  0.5f, 0.001f),
                new Vector3( 0.5f,  0.5f, 0.001f)
            };
            var uvs = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            var tris = new List<int> { 0, 1, 2, 2, 1, 3 };

            var rng = new System.Random(7711);
            int mainVeins = 6;
            for (int v = 0; v < mainVeins; v++)
            {
                float ang = (v / (float)mainVeins) * Mathf.PI * 2f + (float)(rng.NextDouble() - 0.5) * 0.55f;
                float len = 0.16f + (float)rng.NextDouble() * 0.14f;
                var pts = new Vector3[9];
                var rads = new float[9];
                Vector2 cur = Vector2.zero;
                for (int k = 0; k < 9; k++)
                {
                    float t = k / 8f;
                    float reliefZ = Mathf.Lerp(0.005f, 0.001f, t);
                    pts[k] = new Vector3(cur.x, cur.y, reliefZ);
                    rads[k] = Mathf.Lerp(0.0042f, 0.0008f, t);
                    ang += Mathf.Sin(k * 1.4f + v) * 0.36f + (float)(rng.NextDouble() - 0.5) * 0.35f;
                    cur += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (len / 8f);
                }
                AddCurvedTubeCustomUV(verts, uvs, tris, pts, rads, 5, uvCenter: new Vector2(0.5f, 0.5f));
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static void AddCurvedTubeCustomUV(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3[] pts, float[] radii, int sides, Vector2 uvCenter)
        {
            int ringStart = verts.Count;
            for (int p = 0; p < pts.Length; p++)
            {
                Vector3 tangent = p == 0 ? (pts[1] - pts[0]) : (p == pts.Length - 1 ? (pts[p] - pts[p - 1]) : (pts[p + 1] - pts[p - 1]));
                tangent = tangent.sqrMagnitude > 1e-6f ? tangent.normalized : Vector3.up;
                Vector3 upRef = Mathf.Abs(Vector3.Dot(tangent, Vector3.forward)) > 0.92f ? Vector3.up : Vector3.forward;
                Vector3 right = Vector3.Cross(upRef, tangent).normalized;
                Vector3 binorm = Vector3.Cross(tangent, right).normalized;

                for (int s = 0; s <= sides; s++)
                {
                    float a = (s / (float)sides) * Mathf.PI * 2f;
                    Vector3 radial = right * Mathf.Cos(a) + binorm * Mathf.Sin(a);
                    verts.Add(pts[p] + radial * radii[p]);
                    uvs.Add(uvCenter); // Sample opaque ivory core of dendritic texture
                }
            }

            int stride = sides + 1;
            for (int p = 0; p < pts.Length - 1; p++)
            {
                int r0 = ringStart + p * stride;
                int r1 = ringStart + (p + 1) * stride;
                for (int s = 0; s < sides; s++)
                {
                    tris.Add(r0 + s); tris.Add(r1 + s); tris.Add(r0 + s + 1);
                    tris.Add(r0 + s + 1); tris.Add(r1 + s); tris.Add(r1 + s + 1);
                }
            }
        }

        private static Mesh BuildProceduralFungalClusterMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 15 natural multi-tiered fruiting bodies (0.044m emerging primordia up to 0.255m smooth dome/bell caps)
            (Vector2 off, float height, float capR, Vector2 lean)[] bodies =
            {
                (new Vector2( 0.00f,  0.00f), 0.250f, 0.084f, new Vector2( 0.032f,  0.022f)),
                (new Vector2( 0.11f, -0.05f), 0.202f, 0.070f, new Vector2( 0.048f, -0.028f)),
                (new Vector2(-0.10f,  0.06f), 0.182f, 0.065f, new Vector2(-0.042f,  0.034f)),
                (new Vector2(-0.03f, -0.12f), 0.152f, 0.056f, new Vector2(-0.018f, -0.044f)),
                (new Vector2( 0.07f,  0.11f), 0.140f, 0.052f, new Vector2( 0.030f,  0.042f)),
                (new Vector2(-0.15f, -0.05f), 0.120f, 0.046f, new Vector2(-0.045f, -0.018f)),
                (new Vector2( 0.17f,  0.04f), 0.108f, 0.042f, new Vector2( 0.042f,  0.016f)),
                (new Vector2(-0.08f,  0.16f), 0.094f, 0.038f, new Vector2(-0.024f,  0.038f)),
                (new Vector2( 0.05f, -0.18f), 0.086f, 0.035f, new Vector2( 0.018f, -0.034f)),
                (new Vector2( 0.15f, -0.13f), 0.072f, 0.031f, new Vector2( 0.026f, -0.022f)),
                (new Vector2(-0.18f,  0.09f), 0.066f, 0.029f, new Vector2(-0.030f,  0.020f)),
                (new Vector2( 0.00f,  0.19f), 0.058f, 0.025f, new Vector2( 0.008f,  0.024f)),
                (new Vector2(-0.11f, -0.16f), 0.052f, 0.023f, new Vector2(-0.016f, -0.018f)),
                (new Vector2( 0.21f, -0.02f), 0.046f, 0.021f, new Vector2( 0.020f, -0.006f)),
                (new Vector2(-0.21f, -0.01f), 0.042f, 0.019f, new Vector2(-0.018f,  0.006f))
            };

            for (int b = 0; b < bodies.Length; b++)
            {
                var item = bodies[b];
                Vector3 p0 = new Vector3(item.off.x, 0f, item.off.y);
                Vector3 p1 = p0 + new Vector3(item.lean.x * 0.25f, item.height * 0.32f, item.lean.y * 0.25f);
                Vector3 p2 = p0 + new Vector3(item.lean.x * 0.72f, item.height * 0.68f, item.lean.y * 0.72f);
                Vector3 pTop = p0 + new Vector3(item.lean.x, item.height, item.lean.y);

                // Sinuous 10-sided tapered stem mapped to UV v=[0.26..0.54]
                AddCurvedTubeWithVRange(verts, uvs, tris,
                    new[] { p0, p1, p2, pTop },
                    new[] { item.capR * 0.34f, item.capR * 0.21f, item.capR * 0.16f, item.capR * 0.19f },
                    sides: 10, vStart: 0.26f, vEnd: 0.54f);

                // Smooth 22-sided campanulate/convex dome cap (zero polygonal star spikes!)
                const int sides = 22;
                Vector3 capAxis = (pTop - p2).normalized;
                Quaternion capRot = Quaternion.FromToRotation(Vector3.up, capAxis);

                int centerTop = verts.Count;
                verts.Add(pTop + capRot * new Vector3(0f, item.capR * 0.48f, 0f));
                uvs.Add(new Vector2(0.5f, 0.98f));

                int upperDome = verts.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = (i / (float)sides) * Mathf.PI * 2f;
                    float asym = 0.96f + 0.05f * Mathf.Sin(a * 2f + b);
                    Vector3 local = new Vector3(Mathf.Cos(a) * item.capR * 0.44f * asym, item.capR * 0.40f, Mathf.Sin(a) * item.capR * 0.44f * asym);
                    verts.Add(pTop + capRot * local);
                    uvs.Add(new Vector2((float)i / sides, 0.86f));
                }

                int midDome = verts.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = (i / (float)sides) * Mathf.PI * 2f;
                    float asym = 0.94f + 0.06f * Mathf.Sin(a * 2f + b * 1.3f) + 0.03f * Mathf.Cos(a * 3f - b);
                    Vector3 local = new Vector3(Mathf.Cos(a) * item.capR * 0.80f * asym, item.capR * 0.22f, Mathf.Sin(a) * item.capR * 0.80f * asym);
                    verts.Add(pTop + capRot * local);
                    uvs.Add(new Vector2((float)i / sides, 0.73f));
                }

                int outerRim = verts.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = (i / (float)sides) * Mathf.PI * 2f;
                    float wave = 0.93f + 0.07f * Mathf.Sin(a * 2f + b * 1.3f) + 0.035f * Mathf.Sin(a * 5f + b);
                    float rOut = item.capR * wave;
                    float yOut = item.capR * (-0.02f + 0.035f * Mathf.Cos(a * 3f + b));
                    verts.Add(pTop + capRot * new Vector3(Mathf.Cos(a) * rOut, yOut, Mathf.Sin(a) * rOut));
                    uvs.Add(new Vector2((float)i / sides, 0.60f));
                }

                // Underside gill rim & recessed dark gill hub (v = 0.22 -> 0.04)
                int gillRim = verts.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = (i / (float)sides) * Mathf.PI * 2f;
                    float wave = 0.92f + 0.07f * Mathf.Sin(a * 2f + b * 1.3f);
                    float rOut = item.capR * wave * 0.94f;
                    float pleatY = ((i % 2 == 0) ? -0.02f : -0.045f) * item.capR;
                    verts.Add(pTop + capRot * new Vector3(Mathf.Cos(a) * rOut, pleatY, Mathf.Sin(a) * rOut));
                    uvs.Add(new Vector2((float)i / sides, 0.22f));
                }
                int gillCenter = verts.Count;
                verts.Add(pTop + capRot * new Vector3(0f, item.capR * 0.08f, 0f));
                uvs.Add(new Vector2(0.5f, 0.05f));

                for (int i = 0; i < sides; i++)
                {
                    tris.Add(centerTop); tris.Add(upperDome + i + 1); tris.Add(upperDome + i);

                    tris.Add(upperDome + i); tris.Add(upperDome + i + 1); tris.Add(midDome + i);
                    tris.Add(midDome + i);   tris.Add(upperDome + i + 1); tris.Add(midDome + i + 1);

                    tris.Add(midDome + i);   tris.Add(midDome + i + 1);   tris.Add(outerRim + i);
                    tris.Add(outerRim + i);  tris.Add(midDome + i + 1);   tris.Add(outerRim + i + 1);

                    tris.Add(outerRim + i);  tris.Add(outerRim + i + 1);  tris.Add(gillRim + i);
                    tris.Add(gillRim + i);   tris.Add(outerRim + i + 1);  tris.Add(gillRim + i + 1);

                    tris.Add(gillCenter);    tris.Add(gillRim + i);       tris.Add(gillRim + i + 1);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralFungalBracketMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 7 fleshy, smooth kidney/oyster polypore shelf brackets embedded -0.015m into wall seam & projecting along +Z
            (Vector3 off, float radiusX, float depthZ, float thickY, float tiltZ)[] shelves =
            {
                (new Vector3( 0.00f,  0.00f, -0.015f), 0.160f, 0.145f, 0.032f,  0f),
                (new Vector3(-0.07f,  0.055f, -0.015f), 0.125f, 0.112f, 0.025f,  5f),
                (new Vector3( 0.08f, -0.050f, -0.015f), 0.132f, 0.118f, 0.026f, -4f),
                (new Vector3(-0.03f, -0.105f, -0.015f), 0.105f, 0.094f, 0.021f,  3f),
                (new Vector3( 0.04f,  0.108f, -0.015f), 0.096f, 0.086f, 0.020f, -6f),
                (new Vector3(-0.11f, -0.025f, -0.015f), 0.082f, 0.074f, 0.018f,  7f),
                (new Vector3( 0.11f,  0.045f, -0.015f), 0.076f, 0.068f, 0.017f, -7f)
            };

            const int rings = 4;
            const int segs = 20;
            for (int s = 0; s < shelves.Length; s++)
            {
                var sh = shelves[s];
                Quaternion tilt = Quaternion.Euler(0f, 0f, sh.tiltZ);

                int topGridStart = verts.Count;
                for (int r = 0; r <= rings; r++)
                {
                    float tr = (float)r / rings;
                    float vCoord = Mathf.Lerp(0.97f, 0.60f, tr);
                    float archY = sh.thickY * Mathf.Cos(tr * Mathf.PI * 0.45f);
                    for (int i = 0; i <= segs; i++)
                    {
                        float ti = (float)i / segs;
                        float a = Mathf.Lerp(-Mathf.PI * 0.48f, Mathf.PI * 0.48f, ti);
                        // Smooth organic oyster/polypore kidney margin (zero jagged sawtooth!)
                        float smoothLobe = 1f + tr * (0.09f * Mathf.Sin(ti * Mathf.PI * 3f + s * 1.4f) + 0.04f * Mathf.Cos(ti * Mathf.PI * 5f - s));
                        Vector3 local = new Vector3(
                            Mathf.Sin(a) * sh.radiusX * tr * smoothLobe,
                            archY + tr * Mathf.Sin(ti * Mathf.PI * 2f + s) * 0.006f,
                            Mathf.Cos(a) * sh.depthZ * tr * smoothLobe);
                        verts.Add(sh.off + tilt * local);
                        uvs.Add(new Vector2(ti, vCoord));
                    }
                }

                int stride = segs + 1;
                for (int r = 0; r < rings; r++)
                {
                    int r0 = topGridStart + r * stride;
                    int r1 = topGridStart + (r + 1) * stride;
                    for (int i = 0; i < segs; i++)
                    {
                        tris.Add(r0 + i); tris.Add(r0 + i + 1); tris.Add(r1 + i);
                        tris.Add(r1 + i); tris.Add(r0 + i + 1); tris.Add(r1 + i + 1);
                    }
                }

                // Dark pored/gilled underside fan (v: 0.05..0.22)
                int botRoot = verts.Count;
                verts.Add(sh.off + tilt * new Vector3(0f, -sh.thickY * 1.15f, 0.004f));
                uvs.Add(new Vector2(0.5f, 0.05f));
                int botRim = verts.Count;
                int outerTopRow = topGridStart + rings * stride;
                for (int i = 0; i <= segs; i++)
                {
                    Vector3 topPt = verts[outerTopRow + i];
                    verts.Add(topPt - new Vector3(0f, sh.thickY * 0.35f, 0f));
                    uvs.Add(new Vector2((float)i / segs, 0.22f));
                }
                for (int i = 0; i < segs; i++)
                {
                    // Lip connect
                    tris.Add(outerTopRow + i); tris.Add(outerTopRow + i + 1); tris.Add(botRim + i);
                    tris.Add(botRim + i); tris.Add(outerTopRow + i + 1); tris.Add(botRim + i + 1);
                    // Underside fan
                    tris.Add(botRoot); tris.Add(botRim + i + 1); tris.Add(botRim + i);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralVegetalRootMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var rng = new System.Random(7744);

            // 8 gnarled woody-mineral primary roots hugging the slab and dipping cleanly INTO concrete joints (-0.004m at tip)
            for (int r = 0; r < 8; r++)
            {
                float ang = (r / 8f) * Mathf.PI * 2f + (float)(rng.NextDouble() - 0.5) * 0.32f;
                float len = 0.38f + (float)rng.NextDouble() * 0.30f;
                var pts = new Vector3[7];
                var rads = new float[7];
                Vector3 p = new Vector3(0f, 0.016f, 0f);
                for (int k = 0; k < 7; k++)
                {
                    float t = k / 6f;
                    float gnarlH = Mathf.Sin(t * Mathf.PI * 2f + r) * 0.007f * (1f - t) + Mathf.Lerp(0.016f, -0.004f, t);
                    pts[k] = new Vector3(p.x, gnarlH, p.z);
                    rads[k] = Mathf.Lerp(0.020f, 0.0028f, t);
                    if (k == 3)
                    {
                        float fAng = ang + (r % 2 == 0 ? 0.62f : -0.62f);
                        Vector3 fp = pts[k];
                        var fPts = new Vector3[4];
                        var fRads = new float[4];
                        for (int fk = 0; fk < 4; fk++)
                        {
                            float ft = fk / 3f;
                            fPts[fk] = new Vector3(fp.x, Mathf.Lerp(pts[k].y, -0.004f, ft), fp.z);
                            fRads[fk] = Mathf.Lerp(rads[k] * 0.78f, 0.0018f, ft);
                            fAng += (float)(rng.NextDouble() - 0.5) * 0.4f;
                            fp += new Vector3(Mathf.Cos(fAng), 0f, Mathf.Sin(fAng)) * (len * 0.15f);
                        }
                        AddCurvedTubeWithVRange(verts, uvs, tris, fPts, fRads, 6, 0.35f, 0.95f);
                    }
                    ang += (float)(rng.NextDouble() - 0.5) * 0.46f;
                    p += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (len / 6f);
                }
                AddCurvedTubeWithVRange(verts, uvs, tris, pts, rads, 7, 0.05f, 0.95f);
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static Mesh BuildProceduralVegetalLeafTuftMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var rng = new System.Random(7755);

            // 26 sculpted 3D V-folded lanceolate desert-sage / concrete-crevice rosette blades in 4 natural tiers (zero blunt sticks!)
            (int count, float minH, float maxH, float minReach, float maxReach, float bladeW)[] tiers =
            {
                (5,  0.38f, 0.52f, 0.04f, 0.10f, 0.020f), // Tall central finely-pointed spear blades
                (6,  0.30f, 0.44f, 0.10f, 0.18f, 0.027f), // Inner upright spear blades
                (7,  0.22f, 0.35f, 0.18f, 0.28f, 0.032f), // Mid arching rosette blades
                (8,  0.12f, 0.23f, 0.26f, 0.38f, 0.028f)  // Outer spreading weathered blades
            };

            int globalIdx = 0;
            foreach (var tier in tiers)
            {
                for (int s = 0; s < tier.count; s++)
                {
                    float ang = (s / (float)tier.count) * Mathf.PI * 2f + globalIdx * 0.37f + (float)(rng.NextDouble() - 0.5) * 0.24f;
                    float height = Mathf.Lerp(tier.minH, tier.maxH, (float)rng.NextDouble());
                    float reach = Mathf.Lerp(tier.minReach, tier.maxReach, (float)rng.NextDouble());

                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 side = new Vector3(-dir.z, 0f, dir.x);
                    Vector3 baseOff = dir * 0.015f + Vector3.up * 0.012f;

                    int baseVert = verts.Count;
                    const int segs = 6;
                    for (int k = 0; k <= segs; k++)
                    {
                        float t = (float)k / segs;
                        float wProfile = Mathf.Sin(Mathf.Pow(t, 0.78f) * Mathf.PI) * tier.bladeW + (1f - t) * 0.005f;
                        float rDist = t * reach + t * t * reach * 0.32f;
                        float yPos = t * height - t * t * t * height * 0.28f;
                        Vector3 center = baseOff + dir * rDist + Vector3.up * yPos;
                        Vector3 foldLift = Vector3.up * (wProfile * 0.38f);

                        verts.Add(center - side * wProfile + foldLift);
                        verts.Add(center);
                        verts.Add(center + side * wProfile + foldLift);

                        uvs.Add(new Vector2(0f, t));
                        uvs.Add(new Vector2(0.5f, t));
                        uvs.Add(new Vector2(1f, t));

                        if (k > 0)
                        {
                            int r0 = baseVert + (k - 1) * 3;
                            int r1 = baseVert + k * 3;
                            tris.Add(r0);     tris.Add(r1);     tris.Add(r0 + 1);
                            tris.Add(r0 + 1); tris.Add(r1);     tris.Add(r1 + 1);
                            tris.Add(r0 + 1); tris.Add(r1 + 1); tris.Add(r0 + 2);
                            tris.Add(r0 + 2); tris.Add(r1 + 1); tris.Add(r1 + 2);

                            tris.Add(r0);     tris.Add(r0 + 1); tris.Add(r1);
                            tris.Add(r0 + 1); tris.Add(r1 + 1); tris.Add(r1);
                            tris.Add(r0 + 1); tris.Add(r0 + 2); tris.Add(r1 + 1);
                            tris.Add(r0 + 2); tris.Add(r1 + 2); tris.Add(r1 + 1);
                        }
                    }
                    globalIdx++;
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }


        private static void AddCurvedTube(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3[] pts, float[] radii, int sides)
        {
            AddCurvedTubeWithVRange(verts, uvs, tris, pts, radii, sides, 0f, 1f);
        }

        private static void AddCurvedTubeWithVRange(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3[] pts, float[] radii, int sides, float vStart, float vEnd)
        {
            int ringStart = verts.Count;
            for (int p = 0; p < pts.Length; p++)
            {
                Vector3 tangent = p == 0 ? (pts[1] - pts[0]) : (p == pts.Length - 1 ? (pts[p] - pts[p - 1]) : (pts[p + 1] - pts[p - 1]));
                tangent = tangent.sqrMagnitude > 1e-6f ? tangent.normalized : Vector3.up;
                Vector3 upRef = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(upRef, tangent).normalized;
                Vector3 binorm = Vector3.Cross(tangent, right).normalized;

                float vCoord = Mathf.Lerp(vStart, vEnd, (float)p / Mathf.Max(1, pts.Length - 1));
                for (int s = 0; s <= sides; s++)
                {
                    float a = (s / (float)sides) * Mathf.PI * 2f;
                    Vector3 radial = right * Mathf.Cos(a) + binorm * Mathf.Sin(a);
                    verts.Add(pts[p] + radial * radii[p]);
                    uvs.Add(new Vector2((float)s / sides, vCoord));
                }
            }

            int stride = sides + 1;
            for (int p = 0; p < pts.Length - 1; p++)
            {
                int r0 = ringStart + p * stride;
                int r1 = ringStart + (p + 1) * stride;
                for (int s = 0; s < sides; s++)
                {
                    tris.Add(r0 + s); tris.Add(r1 + s); tris.Add(r0 + s + 1);
                    tris.Add(r0 + s + 1); tris.Add(r1 + s); tris.Add(r1 + s + 1);
                }
            }
        }

        private static string EnsureDendriticMyceliumTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float[,] density = new float[size, size];
            var rng = new System.Random(8831);

            // Recursive dendritic hyphal tree painter (organic curved forks & lacy fan tips, zero border)
            void DrawBranch(Vector2 p, float angle, float len, float startRadius, int depth)
            {
                int steps = Mathf.Max(8, Mathf.CeilToInt(len * 1.4f));
                Vector2 cur = p;
                float curAng = angle;
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    float rad = Mathf.Lerp(startRadius, Mathf.Max(0.85f, startRadius * 0.28f), t);
                    int ir = Mathf.CeilToInt(rad + 1.5f);
                    int cx = Mathf.RoundToInt(cur.x);
                    int cy = Mathf.RoundToInt(cur.y);
                    for (int dy = -ir; dy <= ir; dy++)
                    {
                        int py = cy + dy;
                        if (py < 8 || py >= size - 8) continue;
                        for (int dx = -ir; dx <= ir; dx++)
                        {
                            int px = cx + dx;
                            if (px < 8 || px >= size - 8) continue;
                            float d = Mathf.Sqrt((px - cur.x) * (px - cur.x) + (py - cur.y) * (py - cur.y));
                            float val = Mathf.Clamp01(1.15f - d / rad);
                            if (val > density[px, py]) density[px, py] = val;
                        }
                    }

                    curAng += (float)(rng.NextDouble() - 0.5) * 0.16f;
                    cur += new Vector2(Mathf.Cos(curAng), Mathf.Sin(curAng)) * (len / steps);

                    if (depth > 0 && (i == steps / 3 || i == (steps * 2) / 3 || i == steps - 1))
                    {
                        float spread = 0.34f + (float)rng.NextDouble() * 0.38f;
                        DrawBranch(cur, curAng + spread, len * 0.62f, rad * 0.76f, depth - 1);
                        if (rng.NextDouble() > 0.22)
                        {
                            DrawBranch(cur, curAng - spread * 0.92f, len * 0.58f, rad * 0.72f, depth - 1);
                        }
                    }
                }
            }

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            int primaryBranches = 15;
            for (int b = 0; b < primaryBranches; b++)
            {
                float a = (b / (float)primaryBranches) * Mathf.PI * 2f + (float)(rng.NextDouble() - 0.5) * 0.34f;
                // Asymmetric lobate reach so mycelium fans irregularly instead of forming a circular pancake!
                float lobeMod = 0.55f + 0.45f * Mathf.Sin(a * 3f + 0.8f) * Mathf.Cos(a * 2f - 0.5f);
                float len = size * (0.14f + lobeMod * 0.24f);
                DrawBranch(center, a, len, 4.2f, depth: 3);
            }

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float dx = (u - 0.5f) * 2f;
                    float dy = (v - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float lobeGuard = 0.68f + 0.24f * Mathf.Sin(ang * 3f + 0.8f) * Mathf.Cos(ang * 2f - 0.5f);
                    float borderGuard = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Min(0.93f, lobeGuard) - r) / 0.18f));
                    float fbm = PeriodicFbm(u * 2f, v * 2f, 8, 8, 3, 8832);
                    float coreWeb = Mathf.Clamp01((0.15f - r) / 0.14f) * (0.50f + 0.50f * fbm);

                    float d = Mathf.Max(density[x, y], coreWeb) * borderGuard;
                    Color darkEdge = new Color(0.36f, 0.34f, 0.29f);
                    Color ivoryCore = new Color(0.92f, 0.90f, 0.84f);
                    Color rgb = Color.Lerp(darkEdge, ivoryCore, Mathf.SmoothStep(0.12f, 0.82f, d));
                    float alpha = Mathf.Clamp01(d * 1.02f);
                    tex.SetPixel(x, y, new Color(rgb.r, rgb.g, rgb.b, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureMycologicalShadedAtlasTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float fbm = PeriodicFbm(u, v, 8, 8, 4, 8841);
                    float fine = (PeriodicHash(x, y, size, size, 8842) - 0.5f) * 0.045f;
                    Color c;

                    if (v < 0.25f)
                    {
                        // Zone 1: Radially pleated dark sepia-umber gill/pore underside
                        float t = v / 0.25f;
                        float gillPleat = 0.5f + 0.5f * Mathf.Cos(u * Mathf.PI * 2f * 44f);
                        Color deepShadow = new Color(0.11f, 0.09f, 0.08f);
                        Color gillEdge = new Color(0.48f, 0.43f, 0.36f);
                        c = Color.Lerp(deepShadow, gillEdge, gillPleat * (0.35f + 0.65f * t));
                    }
                    else if (v < 0.56f)
                    {
                        // Zone 2: Curved fibrous stalk (dark earthy attachment -> striated bone-ivory neck)
                        float t = (v - 0.25f) / 0.31f;
                        float fiber = 0.5f + 0.5f * Mathf.Sin((u * 28f + fbm * 1.5f) * Mathf.PI * 2f);
                        Color stalkBase = new Color(0.24f, 0.20f, 0.16f);
                        Color stalkTop = new Color(0.86f, 0.83f, 0.76f);
                        c = Color.Lerp(stalkBase, stalkTop, Mathf.SmoothStep(0f, 1f, t));
                        c *= (0.90f + 0.14f * fiber);
                    }
                    else
                    {
                        // Zone 3: Smooth organic cap & polypore shelf (creamy ivory rim -> warm ochre-umber central umbo, subtle radial silkiness)
                        float t = (v - 0.56f) / 0.44f; // 0 at outer rim, 1 at center crown
                        float radialSilk = 0.5f + 0.5f * Mathf.Sin((u * 38f + fbm * 1.2f) * Mathf.PI * 2f);
                        float softGrowth = 0.5f + 0.5f * Mathf.Sin((t * 2.5f + fbm * 0.9f) * Mathf.PI * 2f);
                        Color brightRim = new Color(0.92f, 0.89f, 0.82f);
                        Color midParchment = new Color(0.76f, 0.70f, 0.60f);
                        Color centerUmbo = new Color(0.45f, 0.37f, 0.28f);
                        Color baseGrad = t < 0.58f
                            ? Color.Lerp(brightRim, midParchment, Mathf.SmoothStep(0f, 1f, t / 0.58f))
                            : Color.Lerp(midParchment, centerUmbo, Mathf.SmoothStep(0f, 1f, (t - 0.58f) / 0.42f));
                        c = baseGrad * (0.94f + 0.08f * radialSilk) * (0.94f + 0.08f * softGrowth) * (0.93f + 0.12f * fbm);
                    }

                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(c.r + fine),
                        Mathf.Clamp01(c.g + fine),
                        Mathf.Clamp01(c.b + fine)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureVegetalBladeShadedTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float fbm = PeriodicFbm(u * 2f, v * 2f, 6, 6, 3, 8851);
                    float vein = 0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 24f);

                    // Dark olive-umber base (AO) -> dusty desert sage mid -> pale silvery mineral-sage tip
                    Color baseAO = new Color(0.18f, 0.21f, 0.17f);
                    Color midSage = new Color(0.48f, 0.56f, 0.46f);
                    Color tipSilver = new Color(0.76f, 0.81f, 0.73f);
                    Color grad = v < 0.45f
                        ? Color.Lerp(baseAO, midSage, v / 0.45f)
                        : Color.Lerp(midSage, tipSilver, (v - 0.45f) / 0.55f);

                    // Crisp pale margin & subtle central V-fold shadow for unmistakable 3D blade definition
                    float centerDist = Mathf.Abs(u - 0.5f) * 2f; // 0 at midrib, 1 at edge
                    float foldShade = Mathf.Lerp(0.76f, 1.12f, Mathf.Pow(centerDist, 0.7f));
                    Color c = grad * foldShade * (0.92f + 0.10f * vein) * (0.93f + 0.12f * fbm);
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureVegetalRootBarkTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float fbm = PeriodicFbm(u * 3f, v * 2f, 8, 6, 4, 8861);
                    float barkGroove = 0.5f + 0.5f * Mathf.Sin((u * 18f + fbm * 2.4f) * Mathf.PI * 2f);
                    // Circumferential top-lit highlight vs underside contact shadow so cylindrical roots pop in 3D
                    float circumLight = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI);
                    Color darkBark = new Color(0.20f, 0.17f, 0.14f);
                    Color dryMineralWood = new Color(0.60f, 0.55f, 0.47f);
                    Color c = Color.Lerp(darkBark, dryMineralWood, circumLight * (0.45f + 0.55f * barkGroove)) * (0.90f + 0.16f * fbm);
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }


        private static string EnsureEvidenceRoomRegisterTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 192, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color paper = new Color(0.80f, 0.76f, 0.68f);
            Color rule = new Color(0.56f, 0.54f, 0.49f);
            Color ink = new Color(0.18f, 0.19f, 0.21f);
            Color tearBg = new Color(0.32f, 0.30f, 0.28f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Irregular torn bottom-right corner
                    bool torn = (y < 28 && x > w - 34 + (int)(Mathf.Sin(y * 0.45f) * 6f));
                    float grain = (PeriodicHash(x, y, w, h, 7602) - 0.5f) * 0.05f;
                    Color c = torn ? tearBg : paper;
                    if (!torn)
                    {
                        if (x < 6 || x >= w - 6 || y < 6 || y >= h - 6) c = rule * 0.85f;
                        else if (y > 22 && y < h - 38 && (y % 26 == 0 || x == 74 || x == 134)) c = rule;
                        // Simulated anonymous ink scribble dashes in ledger columns
                        else if (y > 32 && y < h - 50 && (y % 26 >= 10 && y % 26 <= 14) && x > 82 && x < 176)
                        {
                            float stroke = PeriodicHash(x / 3, y / 2, w, h, 7612);
                            if (stroke > 0.58f && x != 134) c = ink;
                        }
                    }
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + grain), Mathf.Clamp01(c.g + grain), Mathf.Clamp01(c.b + grain)));
                }
            }
            DrawBitmapTextCentered(tex, w, h, "REGISTRO", h - 30, 2, ink, false);
            DrawBitmapTextCentered(tex, 74, h, "107", 184, 2, ink, false);
            DrawBitmapTextCentered(tex, 74, h, "108", 158, 2, ink, false);
            DrawBitmapTextCentered(tex, 74, h, "109", 132, 2, ink, false);
            DrawBitmapTextCentered(tex, 74, h, "110", 106, 2, ink, false);
            DrawBitmapTextCentered(tex, 74, h, "111-B", 80, 2, new Color(0.48f, 0.18f, 0.16f), false);
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }


        private static string EnsureEvidenceMaintenanceLogTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 192, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color paper = new Color(0.75f, 0.74f, 0.70f);
            Color rule = new Color(0.55f, 0.56f, 0.54f);
            Color ink = new Color(0.17f, 0.18f, 0.19f);
            Color smudge = new Color(0.46f, 0.47f, 0.45f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float grain = (PeriodicHash(x, y, w, h, 7603) - 0.5f) * 0.05f;
                    Color c = paper;
                    if (x < 6 || x >= w - 6 || y < 6 || y >= h - 6) c = rule * 0.85f;
                    else if (y > 20 && y < h - 36 && y % 20 == 0) c = rule;
                    else if (y > 24 && y < h - 44 && (y % 20 >= 6 && y % 20 <= 11) && x > 18 && x < w - 22)
                    {
                        float stroke = PeriodicHash(x / 2, y / 2, w, h, 7623);
                        if (stroke > 0.56f) c = ink;
                    }
                    // Subtle dry greyish residue smudge on lower-left margin
                    float dx = (x - 36f) / 34f;
                    float dy = (y - 38f) / 30f;
                    if (dx * dx + dy * dy < 1.0f) c = Color.Lerp(c, smudge, 0.45f * (1f - (dx * dx + dy * dy)));
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + grain), Mathf.Clamp01(c.g + grain), Mathf.Clamp01(c.b + grain)));
                }
            }
            DrawBitmapTextCentered(tex, w, h, "MANTENIMIENTO", h - 28, 2, ink, false);
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }


        private static string EnsureEvidenceCobaltRumorNoteTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int w = 160, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            Color scrapPaper = new Color(0.82f, 0.77f, 0.64f);
            Color foldedCorner = new Color(0.66f, 0.61f, 0.50f);
            Color pencil = new Color(0.28f, 0.27f, 0.25f);
            Color edgeShadow = new Color(0.42f, 0.39f, 0.34f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float grain = (PeriodicHash(x, y, w, h, 7604) - 0.5f) * 0.06f;
                    bool raggedBorder = x < 4 + (int)(Mathf.Sin(y * 0.35f) * 2f) ||
                                        y < 4 + (int)(Mathf.Cos(x * 0.31f) * 2f) ||
                                        x >= w - 4 || y >= h - 4;
                    bool cornerFold = (x > w - 28 && y > h - 28 && (x - (w - 28)) + (y - (h - 28)) > 26);
                    Color c = raggedBorder ? edgeShadow : (cornerFold ? foldedCorner : scrapPaper);
                    // Uneven handwritten pencil lines (no letterhead, no seals, no symbols)
                    if (!raggedBorder && !cornerFold && x > 14 && x < w - 18 && y > 18 && y < h - 18)
                    {
                        int band = y % 16;
                        if (band >= 5 && band <= 8)
                        {
                            float jitter = Mathf.Sin(x * 0.28f + y * 0.11f);
                            float stroke = PeriodicHash(x / 2, y / 2, w, h, 7634);
                            if (stroke > 0.52f && jitter > -0.35f) c = pencil;
                        }
                    }
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(c.r + grain), Mathf.Clamp01(c.g + grain), Mathf.Clamp01(c.b + grain)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
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
                new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" }, // 7
                new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" }, // 8
                new[] { "01110", "10001", "10001", "01111", "00001", "10001", "01110" }, // 9
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
                case '7': return new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" };
                case '8': return new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" };
                case '9': return new[] { "01110", "10001", "10001", "01111", "00001", "10001", "01110" };
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
            Transform northWingRoot = shell.Find(NorthWingM1RootName);
            Transform narrativeRoot = root.transform.Find(HotelNarrativeEvidenceRootName);
            Transform ecologyRoot = root.transform.Find(HotelEarlyEcologyRootName);
            Transform faunalRoot = root.transform.Find(HotelFaunalTracesRootName);
            Transform microfaunaRoot = root.transform.Find(HotelFirstLivingMicrofaunaRootName);
            Transform room111BRoot = shell.Find(HotelRoom111BRootName);
            Transform room111BInteriorRoot = root.transform.Find(HotelRoom111BGuestInteriorRootName);
            bool hasRoom111BInterior = room111BInteriorRoot != null &&
                                       room111BInteriorRoot.Find("Room111B_NorthCorridorDirectory") != null &&
                                       room111BInteriorRoot.Find("Room111B_FurnitureAndFinishes") != null &&
                                       room111BInteriorRoot.Find("Room111B_ThresholdOfTrust") != null &&
                                       room111BInteriorRoot.Find("Room111B_InteriorEntranceIllusion") != null;
            bool hasRoom111B = room111BRoot != null &&
                               room111BRoot.Find("Room111B_ConcealedOpening") != null &&
                               room111BRoot.Find("Room111B_EastCorridor") != null &&
                               room111BRoot.Find("Room111B_RoomShell") != null &&
                               room111BRoot.Find("Room111B_Signage") != null;
            bool hasFirstLivingMicrofauna = microfaunaRoot != null &&
                                            microfaunaRoot.Find("Microfauna_Utility_Encounter") != null &&
                                            microfaunaRoot.Find("Microfauna_Maintenance_Encounter") != null;
            bool hasFaunalTraces = faunalRoot != null &&
                                   faunalRoot.Find("Faunal_Courtyard_Traces") != null &&
                                   faunalRoot.Find("Faunal_Maintenance_Traces") != null &&
                                   faunalRoot.Find("Faunal_Utility_Traces") != null;
            bool hasEarlyEcology = ecologyRoot != null &&
                                   ecologyRoot.Find("Ecology_Courtyard_Vegetation") != null &&
                                   ecologyRoot.Find("Ecology_Maintenance_Mycelium") != null &&
                                   ecologyRoot.Find("Ecology_Utility_FungalGrowth") != null &&
                                   ecologyRoot.Find("Ecology_Optional_GuestRoomTrace") != null;
            bool hasNarrativeLayer = narrativeRoot != null &&
                                     narrativeRoot.Find("Hotel_Evidence_UnfinishedProjectPlan") != null &&
                                     narrativeRoot.Find("Hotel_Evidence_RoomRegistrationRecord") != null &&
                                     narrativeRoot.Find("Hotel_Evidence_MaintenanceRecord") != null &&
                                     narrativeRoot.Find("Hotel_Evidence_Cobalt60RumorNote") != null &&
                                     narrativeRoot.Find("Hotel_Subtle_Anomalies") != null;
            bool hasNorthWing = northWingRoot != null &&
                                northWingRoot.Find("NorthWing_Connection_Lobby") != null &&
                                northWingRoot.Find("NorthWing_Connection_Service") != null &&
                                northWingRoot.Find("NorthWing_Courtyard") != null &&
                                northWingRoot.Find("NorthWing_EastArcade") != null &&
                                northWingRoot.Find("NorthWing_WestArcade") != null &&
                                northWingRoot.Find("NorthWing_GuestRooms_107_110") != null &&
                                northWingRoot.Find("NorthWing_BanquetHall") != null &&
                                northWingRoot.Find("NorthWing_HousekeepingCore") != null &&
                                northWingRoot.Find("NorthWing_ConstructionDetails") != null;
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
                                     envRoot.Find("Hotel_Room_Variations") != null &&
                                     hasNorthWing &&
                                     hasNarrativeLayer &&
                                     hasEarlyEcology &&
                                     hasFaunalTraces &&
                                     hasFirstLivingMicrofauna &&
                                     hasRoom111B &&
                                     hasRoom111BInterior &&
                                     NorthwestServiceStaircaseBuilder.VerifyStructurePresent(shell) &&
                                     NorthUpperGalleryBuilder.VerifyStructurePresent(shell) &&
                                     CourtyardLightwellOverlookBuilder.VerifyStructurePresent(shell) &&
                                     EastOverhangBuilder.VerifyStructurePresent(shell);

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

        private static void ConfigureAlphaDecalTextureImporter(string assetPath)
        {
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        private static void ConfigureHighDetailOrganicImporter(string assetPath)
        {
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        private static Material CreateAlphaBlendedDecalMat(string assetPath, string texPath, Color tint, float smoothness, int renderQueue = 3000)
        {
            Material mat = CreateTexturedMat(assetPath, texPath, tint, smoothness, Vector2.one);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            if (mat.HasProperty("_BlendModePreserveSpecular")) mat.SetFloat("_BlendModePreserveSpecular", 0f);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
            if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = renderQueue;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOpaqueTexturedMat(string assetPath, string texPath, Color tint, float smoothness)
        {
            Material mat = CreateTexturedMat(assetPath, texPath, tint, smoothness, Vector2.one);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.DisableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = -1;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Editor verification utility: renders 4 actual URP PlayMode-equivalent camera views
        /// (with exact Remanencia Level 0 ambient light & exponential-squared fog) to PNG files
        /// without modifying or saving the scene.
        /// </summary>
        public static void RenderEcologyVerificationViews()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {ScenePath} for camera verification.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_ECOLOGY_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_EcologyRenders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempEcologyVerifyCam__");
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D readTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            try
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                RenderSettings.fogDensity = SecretBackroomsTransitionController.RemanenciaLevel0FogDensity;

                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    ("verify_zoneB_maint.png",     new Vector3(-10.65f, BaseY + 1.42f, 22.12f), new Vector3( -8.72f, BaseY + 0.78f, 22.05f)),
                    ("verify_zoneC_utility.png",   new Vector3(-13.85f, BaseY + 1.42f, 20.30f), new Vector3(-15.70f, BaseY + 0.72f, 18.65f)),
                    ("verify_zoneA_courtyard.png", new Vector3( -2.85f, BaseY + 1.18f, 22.85f), new Vector3( -1.15f, BaseY + 0.32f, 21.75f)),
                    ("verify_zoneD_room110.png",   new Vector3( -8.35f, BaseY + 1.10f, 32.35f), new Vector3( -9.32f, BaseY + 0.35f, 33.54f))
                };

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.transform.LookAt(shot.lookAt, Vector3.up);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    RenderTexture.active = null;

                    string fullFile = Path.Combine(outDir, shot.fileName);
                    File.WriteAllBytes(fullFile, readTex.EncodeToPNG());
                    Debug.Log($"[BackroomsLevel1Builder] Rendered camera verification view: {fullFile}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);

                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;
            }
        }

        /// <summary>
        /// Read-only Editor camera verification utility for Implementation 07.8 (First Faunal Traces):
        /// renders medium-range and close-inspection URP views of Zones A, B, and C without modifying the scene.
        /// </summary>
        public static void RenderFaunalTracesVerificationViews()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {ScenePath} for 07.8 camera verification.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_FAUNAL_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_FaunalRenders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempFaunalVerifyCam__");
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D readTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            try
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                RenderSettings.fogDensity = SecretBackroomsTransitionController.RemanenciaLevel0FogDensity;

                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                cam.fieldOfView = 55f;
                cam.nearClipPlane = 0.03f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    ("verify_078_zoneA_courtyard_close.png", new Vector3(-1.82f, BaseY + 0.42f, 21.65f), new Vector3(-1.14f, BaseY + 0.06f, 21.84f)),
                    ("verify_078_zoneB_maint_wide.png",      new Vector3(-10.55f, BaseY + 1.38f, 22.15f), new Vector3(-8.72f, BaseY + 0.72f, 22.10f)),
                    ("verify_078_zoneB_maint_close.png",     new Vector3(-9.42f, BaseY + 0.56f, 21.88f), new Vector3(-8.74f, BaseY + 0.30f, 21.86f)),
                    ("verify_078_zoneC_utility_wide.png",    new Vector3(-14.00f, BaseY + 1.32f, 20.15f), new Vector3(-15.70f, BaseY + 0.58f, 18.65f)),
                    ("verify_078_zoneC_utility_close.png",   new Vector3(-15.05f, BaseY + 0.58f, 18.92f), new Vector3(-15.72f, BaseY + 0.28f, 18.54f))
                };

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.transform.LookAt(shot.lookAt, Vector3.up);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    RenderTexture.active = null;

                    string fullFile = Path.Combine(outDir, shot.fileName);
                    File.WriteAllBytes(fullFile, readTex.EncodeToPNG());
                    Debug.Log($"[BackroomsLevel1Builder] Rendered 07.8 faunal verification view: {fullFile}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);

                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;
            }
        }

        /// <summary>
        /// Focused runtime movement verification procedure for Implementation 07.9 (First Living Microfauna).
        /// Opens BackroomsLevel1Scene.unity read-only, steps BOTH live RemanenciaAmbientMicrofaunaController
        /// instances frame-by-frame (dt = 1/60s) with an approaching observer camera through the complete
        /// Concealed -> IdleWait -> Emerging -> Paused -> Retreating -> Cooldown cycle, verifies zero
        /// teleportation and exact concealment return, and renders keyframe URP evidence images without
        /// modifying or saving the scene.
        /// </summary>
        public static void VerifyAndCaptureMicrofaunaRuntimeCycle()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {ScenePath} for 07.9 runtime verification.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_MICROFAUNA_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_MicrofaunaVerify");
            }
            Directory.CreateDirectory(outDir);

            Transform microRoot = GameObject.Find(HotelFirstLivingMicrofaunaRootName)?.transform;
            if (microRoot == null)
            {
                throw new InvalidOperationException($"Missing {HotelFirstLivingMicrofaunaRootName} in scene.");
            }

            var encounters = new (string name, Vector3 camPos, Vector3 camLookAt, Vector3 approachPos)[]
            {
                ("Microfauna_Utility_Encounter",
                    new Vector3(-15.32f, BaseY + 0.24f, 18.66f),
                    new Vector3(-15.64f, BaseY + 0.01f, 18.42f),
                    new Vector3(-14.20f, BaseY + 1.50f, 19.60f)),
                ("Microfauna_Maintenance_Encounter",
                    new Vector3(-9.18f, BaseY + 0.22f, 21.74f),
                    new Vector3(-8.82f, BaseY + 0.01f, 21.91f),
                    new Vector3(-10.10f, BaseY + 1.50f, 21.85f))
            };

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempMicrofaunaVerifyCam__");
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D readTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            try
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                RenderSettings.fogDensity = SecretBackroomsTransitionController.RemanenciaLevel0FogDensity;

                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                cam.fieldOfView = 50f;
                cam.nearClipPlane = 0.02f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                void CaptureShot(string fileName, Vector3 pos, Vector3 lookAt)
                {
                    cam.transform.position = pos;
                    cam.transform.LookAt(lookAt, Vector3.up);
                    cam.Render();
                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, fileName), readTex.EncodeToPNG());
                }

                const float dt = 1f / 60f;
                foreach (var enc in encounters)
                {
                    Transform tf = microRoot.Find(enc.name);
                    var ctrl = tf != null ? tf.GetComponent<RemanenciaAmbientMicrofaunaController>() : null;
                    if (ctrl == null) throw new InvalidOperationException($"Missing controller on {enc.name}.");

                    ctrl.ResetToConcealedState();
                    Vector3 startPos = tf.position;
                    CaptureShot($"verify_079_{enc.name}_01_concealed.png", enc.camPos, enc.camLookAt);

                    bool sawIdle = false, sawEmerging = false, sawPaused = false, sawRetreating = false, sawCooldown = false;
                    bool capturedPaused = false;
                    float maxFrameStep = 0f;
                    float maxExcursion = 0f;
                    Vector3 prevPos = tf.position;
                    float elapsed = 0f;

                    // Step 600 frames (10.0 s) with observer within activationRadius
                    for (int frame = 0; frame < 600; frame++)
                    {
                        ctrl.StepSimulation(dt, enc.approachPos, hasObserver: true);
                        elapsed += dt;

                        float stepDist = Vector3.Distance(prevPos, tf.position);
                        if (stepDist > maxFrameStep) maxFrameStep = stepDist;
                        prevPos = tf.position;

                        float exc = Vector3.Distance(startPos, tf.position);
                        if (exc > maxExcursion) maxExcursion = exc;

                        switch (ctrl.CurrentState)
                        {
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.IdleWait: sawIdle = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Emerging: sawEmerging = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Paused:
                                sawPaused = true;
                                if (!capturedPaused)
                                {
                                    CaptureShot($"verify_079_{enc.name}_02_emerged_paused.png", enc.camPos, enc.camLookAt);
                                    capturedPaused = true;
                                }
                                break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Retreating: sawRetreating = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Cooldown: sawCooldown = true; break;
                        }
                        if (sawCooldown) break;
                    }

                    CaptureShot($"verify_079_{enc.name}_03_retreated.png", enc.camPos, enc.camLookAt);
                    float endOffset = Vector3.Distance(startPos, tf.position);
                    Debug.Log($"[MicrofaunaRuntimeVerify] {enc.name}: elapsed={elapsed:F2}s states=(Idle:{sawIdle},Emerge:{sawEmerging},Pause:{sawPaused},Retreat:{sawRetreating},Cooldown:{sawCooldown}) pathLen={ctrl.TotalPathLength:F3}m maxExcursion={maxExcursion:F3}m maxFrameStep={maxFrameStep:F4}m endReturnErr={endOffset:F5}m");

                    if (!sawIdle || !sawEmerging || !sawPaused || !sawRetreating || !sawCooldown || endOffset > 0.002f || maxFrameStep > 0.005f)
                    {
                        throw new InvalidOperationException($"Runtime cycle verification failed for {enc.name}.");
                    }
                    ctrl.ResetToConcealedState();
                }
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);

                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;
            }
        }

        /// <summary>
        /// Read-only Editor camera verification utility for Implementation 07.11 (Room 111-B: The Impossible East Wing).
        /// Renders the 7 required architectural verification views (plus close-up of the 111-B plaque) with exact
        /// Remanencia Level 0 ambient light & fog without modifying or saving the scene.
        /// </summary>
        public static void RenderRoom111BVerificationViews()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {ScenePath} for 07.11 camera verification.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_ROOM111B_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_Room111BRenders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempRoom111BVerifyCam__");
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D readTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            try
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                RenderSettings.fogDensity = SecretBackroomsTransitionController.RemanenciaLevel0FogDensity;

                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                cam.fieldOfView = 62f;
                cam.nearClipPlane = 0.04f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    ("verify_0711_01_corridor_approach.png",        new Vector3( 8.50f, BaseY + 1.60f, 27.45f), new Vector3(16.00f, BaseY + 1.42f, 27.45f)),
                    ("verify_0711_02_apparent_dead_end.png",        new Vector3(12.40f, BaseY + 1.60f, 27.60f), new Vector3(17.20f, BaseY + 1.38f, 27.55f)),
                    ("verify_0711_03_concealed_opening_closeup.png",new Vector3(16.25f, BaseY + 1.54f, 27.68f), new Vector3(16.88f, BaseY + 1.28f, 24.40f)),
                    ("verify_0711_04_east_corridor_interior.png",   new Vector3(16.80f, BaseY + 1.58f, 25.60f), new Vector3(18.10f, BaseY + 1.32f, 22.10f)),
                    ("verify_0711_05_room111b_doorway.png",         new Vector3(17.10f, BaseY + 1.58f, 22.95f), new Vector3(19.01f, BaseY + 1.45f, 22.90f)),
                    ("verify_0711_06_room111b_interior_plaque.png", new Vector3(19.65f, BaseY + 1.58f, 22.40f), new Vector3(24.40f, BaseY + 1.35f, 22.40f)),
                    ("verify_0711_07_return_connection.png",        new Vector3(17.25f, BaseY + 1.58f, 23.60f), new Vector3(16.55f, BaseY + 1.40f, 27.60f)),
                    ("verify_0711_08_plaque_111b_closeup.png",      new Vector3(18.15f, BaseY + 1.58f, 23.45f), new Vector3(18.89f, BaseY + 1.58f, 23.45f))
                };

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.transform.LookAt(shot.lookAt, Vector3.up);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    RenderTexture.active = null;

                    string fullFile = Path.Combine(outDir, shot.fileName);
                    File.WriteAllBytes(fullFile, readTex.EncodeToPNG());
                    Debug.Log($"[BackroomsLevel1Builder] Rendered 07.11 Room 111-B verification view: {fullFile}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);

                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;
            }
        }

        /// <summary>
        /// Read-only Editor camera verification utility for Implementation 07.12.
        /// </summary>
        public static void RenderRoom111BInteriorAndThresholdViews()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {ScenePath} for 07.12 camera verification.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_ROOM111B_0712_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_Room111B_0712_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempRoom111B0712VerifyCam__");
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D readTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            try
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                RenderSettings.fogDensity = SecretBackroomsTransitionController.RemanenciaLevel0FogDensity;

                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
                cam.fieldOfView = 62f;
                cam.nearClipPlane = 0.04f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    ("verify_0712_01_public_directory_107_110.png", new Vector3( 9.40f, BaseY + 1.62f, 27.25f), new Vector3( 9.40f, BaseY + 1.62f, 28.68f)),
                    ("verify_0712_02_room111b_from_doorway.png",    new Vector3(19.35f, BaseY + 1.56f, 22.40f), new Vector3(23.80f, BaseY + 1.15f, 22.10f)),
                    ("verify_0712_03_room111b_bed_and_south.png",   new Vector3(23.20f, BaseY + 1.55f, 23.60f), new Vector3(21.10f, BaseY + 0.72f, 20.60f)),
                    ("verify_0712_04_room111b_desk_wardrobe.png",   new Vector3(23.10f, BaseY + 1.52f, 21.15f), new Vector3(20.60f, BaseY + 1.05f, 24.45f)),
                    ("verify_0712_05_threshold_of_trust.png",       new Vector3(21.40f, BaseY + 1.50f, 22.40f), new Vector3(24.48f, BaseY + 1.12f, 22.40f)),
                    ("verify_0712_06_interior_hotel_entrance.png",  new Vector3(17.50f, BaseY + 1.52f,  8.80f), new Vector3(19.82f, BaseY + 1.25f,  8.80f))
                };

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.transform.LookAt(shot.lookAt, Vector3.up);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    RenderTexture.active = null;

                    string fullFile = Path.Combine(outDir, shot.fileName);
                    File.WriteAllBytes(fullFile, readTex.EncodeToPNG());
                    Debug.Log($"[BackroomsLevel1Builder] Rendered 07.12 verification view: {fullFile}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);

                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;
            }
        }
    }
}
