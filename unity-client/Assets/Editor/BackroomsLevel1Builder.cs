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
    /// Idempotent builder for Assets/Scenes/BackroomsLevel1Scene.unity:
    /// A full real-world scale 40m x 24m (960 m²) liminal Backrooms Level 0 maze (origin Y = -50.0m,
    /// clear height 2.85m), matching the actual footprint of the south block between Av. Benito Juárez
    /// and C. Ignacio Mariscal. Loaded additively when the player crosses the Threshold Veil inside
    /// the Building A South mysterious door.
    ///
    /// Includes:
    ///   - Seamless procedural Level 0 yellow wallpaper, damp mustard carpet & acoustic ceiling grid
    ///   - 24 fluorescent ceiling fixtures (steady, flickering & dead tubes) + ceiling void tiles
    ///   - Vast Kane Pixels-style central pillared hall, long disorienting corridors & isolated rooms
    ///   - Strictly 1 lone workstation (1 desk + 1 laptop + 1 office chair) in a surreal random alcove
    ///   - 8 "Reality-Bridge" props ready for future live API integrations (Laptop, FM Radio, Alexa,
    ///     Diario de Juárez iPad, Juárez Live Weather/Clock Terminal, 90s Wall Payphone, CCTV Frame,
    ///     Thermal Receipt Printer)
    ///   - All 13 pop-culture horror & esoteric containment artifacts dispersed across the 40x24m maze
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

        [MenuItem("MyVirtualCommunity/Build Backrooms Level 1 Scene")]
        public static void BuildBackroomsLevel1Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Procedural Level 0 & Reality-Bridge textures
            string wallpaperTexPath = EnsureBackroomsWallpaperTexture("Assets/Textures/Environment/T_Backrooms_YellowWallpaper.png");
            string carpetTexPath = EnsureBackroomsCarpetTexture("Assets/Textures/Environment/T_Backrooms_DampCarpet.png");
            string ceilingTexPath = EnsureBackroomsCeilingGridTexture("Assets/Textures/Environment/T_Backrooms_CeilingGrid.png");
            string diarioTexPath = EnsureDiarioJuarezScreenTexture("Assets/Textures/Environment/T_Backrooms_DiarioJuarez_Screen.png");
            string laptopTexPath = EnsureLaptopMysteryScreenTexture("Assets/Textures/Environment/T_Backrooms_Laptop_Screen.png");
            string weatherTexPath = EnsureBorderTerminalScreenTexture("Assets/Textures/Environment/T_Backrooms_BorderTerminal_Screen.png");
            AssetDatabase.Refresh();

            Material wallMat = CreateTexturedMat("Assets/Settings/Backrooms_YellowWallpaper.mat", wallpaperTexPath, new Color(0.95f, 0.92f, 0.78f), 0.12f, new Vector2(1.5f, 1.0f));
            Material carpetMat = CreateTexturedMat("Assets/Settings/Backrooms_DampCarpet.mat", carpetTexPath, new Color(0.86f, 0.80f, 0.62f), 0.06f, new Vector2(20f, 12f));
            Material ceilingMat = CreateTexturedMat("Assets/Settings/Backrooms_CeilingGrid.mat", ceilingTexPath, new Color(0.92f, 0.90f, 0.82f), 0.10f, new Vector2(20f, 12f));
            Material baseboardMat = CreateColorMat("Assets/Settings/Backrooms_Baseboard.mat", new Color(0.28f, 0.23f, 0.14f), 0.18f, 0.0f);
            Material fluorOnMat = CreateEmissiveMat("Assets/Settings/Backrooms_FluorescentOn.mat", new Color(1.0f, 0.98f, 0.84f), new Color(1.0f, 0.95f, 0.74f) * 2.3f);
            Material fluorOffMat = CreateColorMat("Assets/Settings/Backrooms_FluorescentOff.mat", new Color(0.45f, 0.44f, 0.38f), 0.35f, 0.1f);
            Material voidBlackMat = CreateColorMat("Assets/Settings/Backrooms_CeilingVoid.mat", new Color(0.03f, 0.03f, 0.03f), 0.05f, 0.0f);

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
            Material goodGuyYellowMat = CreateColorMat("Assets/Settings/West_SecretVault_ToyBoxYellow.mat", new Color(0.94f, 0.74f, 0.12f), 0.36f, 0.0f);
            Material toyBoxBlueMat = CreateColorMat("Assets/Settings/West_SecretVault_ToyBoxBlue.mat", new Color(0.14f, 0.36f, 0.72f), 0.36f, 0.0f);
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

            // Acoustic office ceiling slab (bottom at CeilingY = -47.15m) + 2 eerie missing ceiling tiles
            CreateBox("Backrooms_Ceiling", shellRoot.transform,
                new Vector3(0f, CeilingY + 0.10f, 0f), new Vector3(40.6f, 0.20f, 24.6f), ceilingMat, keepCollider: true);
            CreateMissingCeilingTile(shellRoot.transform, new Vector3(8.5f, CeilingY, 5.2f), voidBlackMat, ceilingMat);
            CreateMissingCeilingTile(shellRoot.transform, new Vector3(-6.2f, CeilingY, -1.8f), voidBlackMat, ceilingMat);

            // Outer 40m x 24m Perimeter Walls
            CreateWallSegment("Wall_Outer_South", shellRoot.transform, new Vector2(-20f, -12f), new Vector2(20f, -12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_North", shellRoot.transform, new Vector2(-20f,  12f), new Vector2(20f,  12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_West",  shellRoot.transform, new Vector2(-20f, -12f), new Vector2(-20f, 12f), 0.26f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_East",  shellRoot.transform, new Vector2( 20f, -12f), new Vector2( 20f, 12f), 0.26f, wallMat, baseboardMat);

            // =========================================================================
            // 2. 40m x 24m LIMINAL LEVEL 0 LABYRINTH ARCHITECTURE
            //    Every doorway/passage is >= 2.2m wide for effortless navigation!
            // =========================================================================

            // --- ZONE NE: ARRIVAL VESTIBULE & ENTRY BAFFLE (X=+13..+20, Z=+5..+12) ---
            // Long south divider separating Arrival Vestibule from East Hall (2.6m doorway at X=+13..+15.6)
            CreateWallSegment("Wall_NE_ArrivalSouth", shellRoot.transform, new Vector2(15.6f, 5.4f), new Vector2(20.0f, 5.4f), 0.22f, wallMat, baseboardMat);
            // Sightline baffle directly ahead of arrival so the player steps into an eerie corridor T-junction
            CreateWallSegment("Wall_NE_SightlineBaffle", shellRoot.transform, new Vector2(12.2f, 7.6f), new Vector2(12.2f, 12.0f), 0.22f, wallMat, baseboardMat);

            // --- ZONE N-EAST: QUIET CORRIDOR & DIARIO IPAD ALCOVE (X=+1..+12.2, Z=+5.4..+12) ---
            CreateWallSegment("Wall_NorthCorridor_SouthA", shellRoot.transform, new Vector2(6.8f, 5.4f), new Vector2(12.2f, 5.4f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NorthCorridor_AlcoveW", shellRoot.transform, new Vector2(7.6f, 8.6f), new Vector2(7.6f, 12.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NorthCorridor_Divider", shellRoot.transform, new Vector2(1.0f, 5.4f), new Vector2(1.0f, 9.2f), 0.22f, wallMat, baseboardMat);

            // --- ZONE N-CENTER: SEALED DOLL CONTAINMENT ROOM (X=-9..-1, Z=+5.6..+12) ---
            CreateWallSegment("Wall_NCenter_East", shellRoot.transform, new Vector2(-1.4f, 5.6f), new Vector2(-1.4f, 9.4f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NCenter_SouthL", shellRoot.transform, new Vector2(-9.2f, 5.6f), new Vector2(-6.2f, 5.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NCenter_SouthR", shellRoot.transform, new Vector2(-3.6f, 5.6f), new Vector2(-1.4f, 5.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NCenter_West", shellRoot.transform, new Vector2(-9.2f, 5.6f), new Vector2(-9.2f, 12.0f), 0.22f, wallMat, baseboardMat);

            // --- ZONE NW: LEVEL-OMEGA VAULT SAFE ROOM (X=-20..-12.5, Z=+4.8..+12) ---
            CreateWallSegment("Wall_NWVault_East", shellRoot.transform, new Vector2(-12.5f, 4.8f), new Vector2(-12.5f, 9.4f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NWVault_SouthW", shellRoot.transform, new Vector2(-20.0f, 4.8f), new Vector2(-17.8f, 4.8f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NWVault_SouthE", shellRoot.transform, new Vector2(-15.0f, 4.8f), new Vector2(-12.5f, 4.8f), 0.22f, wallMat, baseboardMat);

            // --- ZONE EAST WING: FM RADIO HALL & RANDOM WORKSTATION RECESS (X=+10..+20, Z=-4.6..+5.4) ---
            CreateWallSegment("Wall_EastWing_WestN", shellRoot.transform, new Vector2(10.2f, 1.0f), new Vector2(10.2f, 5.4f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_EastWing_WorkstationN", shellRoot.transform, new Vector2(5.4f, -0.8f), new Vector2(9.4f, -0.8f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_EastWing_WorkstationE", shellRoot.transform, new Vector2(9.4f, -4.6f), new Vector2(9.4f, -0.8f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_EastWing_MidBaffle", shellRoot.transform, new Vector2(14.8f, -4.6f), new Vector2(14.8f, 0.8f), 0.22f, wallMat, baseboardMat);

            // --- ZONE SE: PAYPHONE CORRIDOR & 03:07 ALCOVE (X=+9.4..+20, Z=-12..-4.6) ---
            CreateWallSegment("Wall_SE_NorthE", shellRoot.transform, new Vector2(14.8f, -4.6f), new Vector2(17.4f, -4.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SE_NorthW", shellRoot.transform, new Vector2(9.4f, -4.6f), new Vector2(12.0f, -4.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SE_West", shellRoot.transform, new Vector2(9.4f, -12.0f), new Vector2(9.4f, -7.4f), 0.22f, wallMat, baseboardMat);

            // --- ZONE S-CENTER: EVIDENCE CORKBOARD INVESTIGATION ALCOVE (X=-1.5..+9.4, Z=-12..-4.6) ---
            CreateWallSegment("Wall_SCenter_NorthE", shellRoot.transform, new Vector2(3.8f, -4.6f), new Vector2(6.8f, -4.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SCenter_NorthW", shellRoot.transform, new Vector2(-1.5f, -4.6f), new Vector2(1.2f, -4.6f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SCenter_West", shellRoot.transform, new Vector2(-1.5f, -9.2f), new Vector2(-1.5f, -4.6f), 0.22f, wallMat, baseboardMat);

            // --- ZONE S-WEST LABYRINTH & ALEXA CORNER (X=-12.5..-1.5, Z=-12..-4.0) ---
            CreateWallSegment("Wall_SWLab_NorthE", shellRoot.transform, new Vector2(-7.0f, -4.0f), new Vector2(-4.0f, -4.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWLab_NorthW", shellRoot.transform, new Vector2(-12.5f, -4.0f), new Vector2(-9.8f, -4.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWLab_Baffle", shellRoot.transform, new Vector2(-6.5f, -12.0f), new Vector2(-6.5f, -7.2f), 0.22f, wallMat, baseboardMat);

            // --- ZONE WEST CORRIDOR & CRT NICHE (X=-20..-12.5, Z=-4.0..+4.8) ---
            CreateWallSegment("Wall_WestCorr_East", shellRoot.transform, new Vector2(-12.5f, -1.0f), new Vector2(-12.5f, 2.2f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_WestCorr_CrtBaffle", shellRoot.transform, new Vector2(-16.5f, -1.8f), new Vector2(-16.5f, 1.8f), 0.22f, wallMat, baseboardMat);

            // --- ZONE SW: MONUMENTAL DIMENSIONAL PORTAL CHAMBER (X=-20..-12.5, Z=-12..-4.0) ---
            // Deep isolated sanctuary in the opposite corner (~40m diagonally from entrance!)
            CreateWallSegment("Wall_SWPortal_EastN", shellRoot.transform, new Vector2(-12.5f, -6.4f), new Vector2(-12.5f, -4.0f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWPortal_EastS", shellRoot.transform, new Vector2(-12.5f, -12.0f), new Vector2(-12.5f, -9.2f), 0.22f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWPortal_North", shellRoot.transform, new Vector2(-20.0f, -4.0f), new Vector2(-15.5f, -4.0f), 0.22f, wallMat, baseboardMat);

            // --- VAST CENTRAL PILLARED HALL (Kane Pixels Iconic Square Yellow Columns!) ---
            // 12 evenly spaced yellow-wallpapered square columns spanning X = -10..+6, Z = -2.2..+3.8
            float[] pillarXs = new float[] { -9.5f, -5.5f, -1.5f, 2.5f };
            float[] pillarZs = new float[] { -1.8f, 1.4f, 4.0f };
            int pIdx = 0;
            for (int ix = 0; ix < pillarXs.Length; ix++)
            {
                for (int iz = 0; iz < pillarZs.Length; iz++)
                {
                    // Skip one pillar near Workstation recess so sightlines open eerily
                    if (ix == 3 && iz == 0) continue;
                    string pName = (ix == 2 && iz == 1) ? "Pillar_CentralHall_Phone" : $"Pillar_CentralHall_{pIdx++}";
                    CreatePillar(pName, shellRoot.transform, pillarXs[ix], pillarZs[iz], 0.68f, wallMat, baseboardMat);
                }
            }
            // Extra iconic offset pillars in East & West halls
            CreatePillar("Pillar_EastHall_Mid", shellRoot.transform, 12.6f, -1.4f, 0.64f, wallMat, baseboardMat);
            CreatePillar("Pillar_SouthLab_Mid", shellRoot.transform, -9.6f, -8.2f, 0.64f, wallMat, baseboardMat);

            // =========================================================================
            // 3. LEVEL 0 FLUORESCENT CEILING PANELS (24 FIXTURES, 16 EFFICIENT POINT LIGHTS)
            //    Tuned for rich liminal glow across 960 m² while staying fast on 2 GB VRAM GPUs!
            // =========================================================================
            GameObject lightsRoot = new GameObject("Backrooms_FluorescentLights");
            lightsRoot.transform.SetParent(root.transform, false);

            (float x, float z, bool lit, bool spawnLight, bool flicker, float intensity)[] fixtures = new[]
            {
                // North Row (Z = +8.8m)
                ( 16.5f,  8.8f, true,  true,  false, 1.15f), // NE Arrival Vestibule
                ( 10.2f,  8.8f, true,  false, false, 0.00f), // NE Baffle Corridor (emissive panel)
                (  4.5f,  8.8f, true,  true,  false, 1.10f), // N-East Diario de Juárez iPad Alcove
                ( -5.2f,  8.8f, true,  true,  true,  0.95f), // N-Center Annabelle Containment Room (flicker!)
                (-11.0f,  8.8f, false, false, false, 0.00f), // Dead tube before NW Vault
                (-16.5f,  8.6f, true,  true,  false, 1.15f), // NW Level-Omega Vault Safe Room

                // Upper-Central Row (Z = +2.8m)
                ( 16.5f,  2.2f, true,  true,  false, 1.05f), // East Corridor
                ( 12.5f,  2.2f, true,  false, true,  0.00f), // FM Radio Hall panel
                (  5.5f,  2.8f, true,  true,  false, 1.10f), // Central Pillared Hall East
                ( -1.5f,  2.8f, true,  true,  true,  1.05f), // Central Pillared Hall Center (flicker!)
                ( -8.5f,  2.8f, true,  true,  false, 1.05f), // Central Pillared Hall West
                (-15.5f,  2.4f, false, false, false, 0.00f), // Dead tube in West Corridor

                // Lower-Central Row (Z = -2.4m)
                ( 17.5f, -2.4f, true,  false, false, 0.00f), // Far East Corridor
                (  7.2f, -2.6f, true,  true,  false, 1.10f), // Lone Laptop Workstation Recess
                (  0.5f, -2.2f, true,  true,  false, 1.05f), // Central-South Hall
                ( -6.8f, -2.4f, true,  true,  true,  0.95f), // Alexa Corner Hall (flicker!)
                (-14.5f, -1.6f, true,  true,  false, 0.90f), // West CRT TV Niche

                // South Row (Z = -8.4m)
                ( 15.5f, -8.4f, true,  true,  false, 1.05f), // SE Payphone & 03:07 Clock Corridor
                ( 11.2f, -8.4f, false, false, false, 0.00f), // Dead tube between SE & S-Center
                (  4.0f, -8.4f, true,  true,  false, 1.15f), // S-Center Evidence Corkboard & Thermal Printer
                ( -3.5f, -8.4f, true,  false, false, 0.00f), // South Labyrinth East
                ( -9.5f, -8.4f, true,  true,  true,  0.95f), // SW Labyrinth Dybbuk & Border Clock (flicker!)
                (-16.5f, -8.2f, true,  true,  true,  0.65f), // Far SW Monumental Portal Chamber (dimmer eerie glow!)
                (-16.5f, -5.2f, false, false, false, 0.00f), // Dead tube at Portal Threshold
            };

            for (int i = 0; i < fixtures.Length; i++)
            {
                var fx = fixtures[i];
                GameObject panel = CreateBox($"Backrooms_FluorPanel_{i}", lightsRoot.transform,
                    new Vector3(fx.x, CeilingY - 0.015f, fx.z), new Vector3(1.22f, 0.03f, 0.58f),
                    fx.lit ? fluorOnMat : fluorOffMat, keepCollider: false);
                CreateBox($"Backrooms_FluorFrame_{i}", panel.transform,
                    new Vector3(0f, 0.25f, 0f), new Vector3(1.05f, 0.8f, 1.06f), rackSteelMat, keepCollider: false);

                if (fx.lit && fx.spawnLight)
                {
                    GameObject lGo = new GameObject($"Backrooms_FluorLight_{i}");
                    lGo.transform.SetParent(panel.transform, false);
                    lGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
                    Light l = lGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1.0f, 0.95f, 0.72f);
                    l.intensity = fx.intensity;
                    l.range = 9.8f;
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
            // 7. THE 13 POP-CULTURE HORROR & ESOTERIC ARTIFACTS DISPERSED OVER 960 m²
            // =========================================================================
            GameObject artifactsRoot = new GameObject("Backrooms_DispersedArtifacts");
            artifactsRoot.transform.SetParent(root.transform, false);

            // 1. Evidence Corkboard PDN-00 (Safe Code Clue 654321 -> points to NW Vault!) in South-Central Alcove
            BuildEvidenceCorkboard(artifactsRoot.transform, new Vector3(4.00f, BaseY + 1.48f, -11.78f), Quaternion.Euler(0f, -90f, 0f),
                darkMahoganyMat, corkMat, porcelainMat, astrolabeCoreMat, stickyYellowMat, redThreadMat);

            // 2. 03:07 AM Brass Alarm Clock on a narrow wooden wall shelf in SE Corridor
            CreateBox("Clock307_Pedestal", propsRoot.transform, new Vector3(11.40f, BaseY + 0.45f, -11.45f), new Vector3(0.42f, 0.90f, 0.42f), darkMahoganyMat, keepCollider: true);
            BuildClock307(artifactsRoot.transform, new Vector3(11.40f, BaseY + 0.98f, -11.45f), Quaternion.Euler(0f, 180f, 0f),
                antiqueBrassMat, porcelainMat, obsidianMat);

            // 3. Babadook Pop-Up Storybook lying alone on the damp carpet in quiet NE Dead-End Alcove
            BuildBabadookBook(artifactsRoot.transform, new Vector3(9.80f, BaseY + 0.04f, 10.60f), Quaternion.Euler(0f, 128f, 0f),
                obsidianMat, saltWhiteMat, crimsonVelvetMat);

            // 4. The Black Rotary Phone mounted on Pillar_CentralHall_Phone (-1.5, +1.4) facing East (+X)
            BuildBlackRotaryPhone(artifactsRoot.transform, new Vector3(-1.12f, BaseY + 1.42f, 1.40f), Quaternion.Euler(0f, 180f, 0f),
                obsidianMat, aluminumMat);

            // 5. 90s CRT TV + Cursed VHS ("7 DÍAS") on rolling AV cart in West Niche (-18.9, -1.6)
            CreateBox("CrtTv_RollingCart", propsRoot.transform, new Vector3(-18.90f, BaseY + 0.34f, -1.60f), new Vector3(0.54f, 0.68f, 0.68f), rackSteelMat, keepCollider: true);
            BuildCrtTvAndVhs(artifactsRoot.transform, new Vector3(-18.88f, BaseY + 0.68f, -1.60f), Quaternion.Euler(0f, 90f, 0f),
                rackSteelMat, crtStaticMat, saltWhiteMat, obsidianMat);

            // 6. Anomalous Astral Compass on a lone pedestal in Central Pillared Hall (points SW toward Portal!)
            CreateBox("Compass_SideTable", propsRoot.transform, new Vector3(2.50f, BaseY + 0.42f, -1.40f), new Vector3(0.48f, 0.84f, 0.48f), darkMahoganyMat, keepCollider: true);
            BuildAnomalousCompass(artifactsRoot.transform, new Vector3(2.50f, BaseY + 0.86f, -1.40f), Quaternion.Euler(0f, -112f, 0f),
                antiqueBrassMat, astrolabeCoreMat);

            // 7. Annabelle Sealed Glass Vitrine centered inside North-Central Containment Room (-5.2, +10.2)
            CreateBox("DollVitrine_Pedestal", propsRoot.transform, new Vector3(-5.20f, BaseY + 0.46f, 10.40f), new Vector3(0.54f, 0.92f, 0.48f), darkMahoganyMat, keepCollider: true);
            BuildDollGlassCase(artifactsRoot.transform, new Vector3(-5.20f, BaseY + 0.92f, 10.40f), Quaternion.Euler(0f, 180f, 0f),
                darkMahoganyMat, vitrineGlassMat, saltWhiteMat, redThreadMat, porcelainMat, crimsonVelvetMat, obsidianMat, agedLeatherMat);

            // 8. Billy Spiral-Cheek Puppet perched on a lone wooden crate (NO extra office chair!) in West Corridor
            CreateBox("Billy_WoodenCrate", propsRoot.transform, new Vector3(-11.20f, BaseY + 0.28f, 3.00f), new Vector3(0.54f, 0.56f, 0.54f), cardboardMat, keepCollider: true);
            BuildSpiralPuppet(artifactsRoot.transform, new Vector3(-11.20f, BaseY + 0.56f, 3.00f), Quaternion.Euler(0f, 145f, 0f),
                obsidianMat, saltWhiteMat, redThreadMat);

            // 9. Good Guy Yellow Box (Chucky) resting on archive boxes in Far-North Corridor (+2.0, +10.9)
            BuildPossessedDollBox(artifactsRoot.transform, new Vector3(1.85f, BaseY + 0.92f, 10.90f), Quaternion.Euler(0f, 175f, 0f),
                goodGuyYellowMat, toyBoxBlueMat, redThreadMat, rackSteelMat);

            // 10. Chained Dybbuk Box inside a white salt ring on the damp carpet in South-West Labyrinth
            BuildDybbukBox(artifactsRoot.transform, new Vector3(-8.40f, BaseY + 0.02f, -10.40f), Quaternion.Euler(0f, -25f, 0f),
                darkMahoganyMat, saltWhiteMat, rackSteelMat, redThreadMat);

            // 11. Embalmed Ceramic Hand (Talk to Me) on a dark pedestal in South-West Corridor (-4.0, -10.5)
            CreateBox("CeramicHand_Pedestal", propsRoot.transform, new Vector3(-4.00f, BaseY + 0.44f, -10.80f), new Vector3(0.44f, 0.88f, 0.44f), obsidianMat, keepCollider: true);
            BuildCeramicHand(artifactsRoot.transform, new Vector3(-4.00f, BaseY + 0.88f, -10.80f), Quaternion.Euler(0f, 180f, 0f),
                saltWhiteMat, porcelainMat, obsidianMat);

            // 12. Ouija Spirit Board & Planchette on a low relic table in South-Central Alcove (+6.5, -10.9)
            CreateBox("Ouija_RelicTable", propsRoot.transform, new Vector3(6.50f, BaseY + 0.38f, -11.10f), new Vector3(0.96f, 0.76f, 0.52f), darkMahoganyMat, keepCollider: true);
            BuildOuijaBoard(artifactsRoot.transform, new Vector3(6.50f, BaseY + 0.78f, -11.10f), Quaternion.Euler(0f, 180f, 0f),
                agedLeatherMat, obsidianMat, porcelainMat);

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

            // Relic Table along West Wall of NW Vault Room holding Necronomicon Grimoire & Lament Configuration
            CreateBox("NWVault_RelicTable", propsRoot.transform, new Vector3(-19.20f, BaseY + 0.42f, 8.20f), new Vector3(0.68f, 0.84f, 2.20f), darkMahoganyMat, keepCollider: true);
            BuildNecronomiconGrimoire(artifactsRoot.transform, new Vector3(-19.15f, BaseY + 0.84f, 8.75f), Quaternion.Euler(0f, 90f, 0f),
                rackSteelMat, agedLeatherMat, antiqueBrassMat, safeLedAmberMat);
            BuildLamentPuzzleBox(artifactsRoot.transform, new Vector3(-19.15f, BaseY + 0.84f, 7.65f), Quaternion.Euler(0f, 90f, 0f),
                crimsonVelvetMat, obsidianMat, antiqueBrassMat);

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
            Debug.Log($"[BackroomsLevel1Builder] Built & saved 40m x 24m (960 m²) real-scale Backrooms Level 0 maze with all 13 artifacts, 8 Reality-Bridge props, NW Level-Omega Safe & far SW Monumental Portal at {ScenePath}.");
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
                "Tableta sobre atril mostrando la portada digital de El Diario de Juárez. Preparada para sincronizar titulares reales del día desde Ciudad Juárez dentro de los Backrooms.",
                "Tablet on a floor stand displaying the digital front page of El Diario de Juárez. Ready to pull live daily headlines from Ciudad Juárez straight into the Backrooms.",
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
        private static void CreateMissingCeilingTile(Transform parent, Vector3 pos, Material voidMat, Material tileMat)
        {
            CreateBox("Backrooms_MissingTileVoid", parent,
                new Vector3(pos.x, pos.y - 0.005f, pos.z), new Vector3(0.68f, 0.02f, 0.68f), voidMat, keepCollider: false);
            GameObject crookedTile = CreateBox("Backrooms_CrookedCeilingTile", parent,
                new Vector3(pos.x + 0.38f, pos.y - 0.08f, pos.z + 0.02f), new Vector3(0.62f, 0.025f, 0.62f), tileMat, keepCollider: false);
            crookedTile.transform.localRotation = Quaternion.Euler(14f, 8f, -18f);
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
                "El Umbral de Lasser — Única Puerta Dimensional (Nivel 1 → Nivel 2)",
                "The Lasser Threshold — Sole Dimensional Portal (Level 1 → Level 2)",
                "El único portal dimensional escondido en lo más profundo del laberinto. Su pilar derecho posee una Cuenca Octagonal que requiere 'El Ojo del Umbral' para liberar las cadenas y abrir paso al Nivel 2.",
                "The sole dimensional gateway hidden deep within the maze. Its right pillar holds an Octagonal Socket requiring 'The Eye of the Threshold' to release the chains and open the path to Level 2.",
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

        private static void BuildClock307(Transform parent, Vector3 pos, Quaternion rot, Material brassMat, Material faceMat, Material darkMat)
        {
            GameObject r = new GameObject("Artifact_Clock307");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            GameObject body = CreateBox("Clock307_Body", r.transform, Vector3.zero, new Vector3(0.14f, 0.14f, 0.06f), brassMat, keepCollider: false);
            BoxCollider col = body.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 1.6f, 2.0f);
            col.isTrigger = true;
            AttachDesc(body, "artifact:reloj-0307",
                "Reloj Despertador Detenido a las 03:07 AM (EXP-00 • El Conjuro)",
                "Alarm Clock Frozen at 03:07 AM (EXP-00 • The Conjuring)",
                "Todos los relojes dentro de los Backrooms se detienen exactamente a las 03:07 AM sin importar la hora de Ciudad Juárez.",
                "Every clock inside the Backrooms stops at exactly 03:07 AM regardless of the real time in Ciudad Juárez.",
                new Color(1.0f, 0.80f, 0.34f));
            CreateBox("Clock307_Dial", r.transform, new Vector3(0f, 0f, 0.034f), new Vector3(0.115f, 0.115f, 0.006f), faceMat, keepCollider: false);
            CreateBox("Clock307_Hour", r.transform, new Vector3(0.02f, 0f, 0.04f), new Vector3(0.04f, 0.008f, 0.005f), darkMat, keepCollider: false);
            GameObject minH = CreateBox("Clock307_Min", r.transform, new Vector3(0.012f, 0.02f, 0.04f), new Vector3(0.007f, 0.048f, 0.005f), darkMat, keepCollider: false);
            minH.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            CreateBox("Clock307_BellL", r.transform, new Vector3(-0.06f, 0.085f, 0f), new Vector3(0.045f, 0.03f, 0.045f), brassMat, keepCollider: false);
            CreateBox("Clock307_BellR", r.transform, new Vector3(0.06f, 0.085f, 0f), new Vector3(0.045f, 0.03f, 0.045f), brassMat, keepCollider: false);
        }

        private static void BuildBabadookBook(Transform parent, Vector3 pos, Quaternion rot, Material obsidianMat, Material whiteMat, Material crimsonMat)
        {
            GameObject book = CreateBox("Artifact_BabadookBook", parent, pos, new Vector3(0.24f, 0.06f, 0.32f), obsidianMat, keepCollider: false);
            book.transform.rotation = rot;
            BoxCollider col = book.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 3.0f, 1.3f);
            col.isTrigger = true;
            AttachDesc(book, "artifact:libro-babadook",
                "Libro Emergente 'Mister Babadook' (EXP-02)",
                "'Mister Babadook' Pop-Up Storybook (EXP-02)",
                "Libro infantil de tapa carbonizada abandonado sobre la alfombra húmeda. Atado con correa para impedir que sus páginas emergentes se abran.",
                "Charcoal-black pop-up book lying alone on the damp carpet. Strapped shut so its pop-up pages cannot unfold.",
                new Color(0.82f, 0.84f, 0.90f));
            CreateBox("Babadook_CoverArt", book.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.56f, 0.06f, 0.68f), whiteMat, keepCollider: false);
            CreateBox("Babadook_RedSpine", book.transform, new Vector3(-0.46f, 0f, 0f), new Vector3(0.12f, 1.04f, 1.02f), crimsonMat, keepCollider: false);
        }

        private static void BuildBlackRotaryPhone(Transform parent, Vector3 pos, Quaternion rot, Material obsidianMat, Material aluminumMat)
        {
            GameObject r = new GameObject("Artifact_BlackRotaryPhone");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            GameObject body = CreateBox("BlackPhone_WallBox", r.transform, Vector3.zero, new Vector3(0.08f, 0.26f, 0.18f), obsidianMat, keepCollider: false);
            BoxCollider col = body.AddComponent<BoxCollider>();
            col.size = new Vector3(2.0f, 1.4f, 1.4f);
            col.isTrigger = true;
            AttachDesc(body, "artifact:telefono-negro",
                "El Teléfono Negro con Cable Cortado (EXP-11 • The Black Phone)",
                "The Black Rotary Phone with Severed Cord (EXP-11 • The Black Phone)",
                "Teléfono mural de baquelita montado en una columna amarilla del gran salón. Su cable está cortado, pero a veces timbra en el silencio de los pasillos.",
                "Black Bakelite wall telephone mounted on a yellow column in the great pillared hall. Its cord is severed, yet it still rings in the silence.",
                new Color(0.75f, 0.80f, 0.90f));
            CreateBox("BlackPhone_Dial", r.transform, new Vector3(-0.045f, -0.01f, 0f), new Vector3(0.015f, 0.10f, 0.10f), aluminumMat, keepCollider: false);
            CreateBox("BlackPhone_Handset", r.transform, new Vector3(-0.04f, 0.15f, 0f), new Vector3(0.055f, 0.05f, 0.22f), obsidianMat, keepCollider: false);
            CreateBox("BlackPhone_Cord", r.transform, new Vector3(-0.02f, -0.22f, -0.04f), new Vector3(0.02f, 0.22f, 0.02f), obsidianMat, keepCollider: false);
        }

        private static void BuildCrtTvAndVhs(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material crtStaticMat, Material whiteMat, Material obsidianMat)
        {
            GameObject r = new GameObject("Artifact_CrtTv_Root");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            GameObject cab = CreateBox("Artifact_CrtTv_Cabinet", r.transform, new Vector3(0f, 0.26f, 0f), new Vector3(0.48f, 0.44f, 0.36f), steelMat, keepCollider: false);
            BoxCollider col = cab.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(cab, "artifact:cinta-vhs-el-aro",
                "Televisor CRT y Cinta VHS '7 DÍAS' (EXP-04 • El Aro / Ringu)",
                "CRT Television & '7 DAYS' Cursed VHS (EXP-04 • The Ring)",
                "Televisor de los 90 encendido sin estar enchufado en un nicho del ala Oeste. Reproduce en bucle un eclipse circular de estática. Etiqueta: '7 DÍAS'.",
                "Unplugged 1990s CRT TV glowing in a West wing niche, looping a circular eclipse of static. Tape label: '7 DAYS'.",
                new Color(0.55f, 0.84f, 1.0f));
            CreateBox("Artifact_CrtTv_StaticScreen", r.transform, new Vector3(-0.02f, 0.28f, 0.184f), new Vector3(0.36f, 0.28f, 0.012f), crtStaticMat, keepCollider: false);
            CreateBox("Artifact_CrtTv_RingOuter", r.transform, new Vector3(-0.02f, 0.28f, 0.190f), new Vector3(0.14f, 0.14f, 0.01f), whiteMat, keepCollider: false);
            CreateBox("Artifact_CrtTv_RingInnerVoid", r.transform, new Vector3(-0.02f, 0.28f, 0.196f), new Vector3(0.11f, 0.11f, 0.01f), obsidianMat, keepCollider: false);
            CreateBox("Artifact_CursedVhsTape", r.transform, new Vector3(0.10f, 0.02f, 0.22f), new Vector3(0.18f, 0.03f, 0.11f), obsidianMat, keepCollider: false);
            CreateBox("Artifact_CursedVhsLabel", r.transform, new Vector3(0.10f, 0.038f, 0.22f), new Vector3(0.11f, 0.006f, 0.055f), whiteMat, keepCollider: false);

            GameObject tvGlow = new GameObject("Artifact_CrtTv_BlueGlow");
            tvGlow.transform.SetParent(r.transform, false);
            tvGlow.transform.localPosition = new Vector3(0f, 0.30f, 0.45f);
            Light l = tvGlow.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.45f, 0.78f, 1.0f);
            l.intensity = 0.85f;
            l.range = 4.6f;
            l.shadows = LightShadows.None;
        }

        private static void BuildDollGlassCase(Transform parent, Vector3 pos, Quaternion rot,
            Material woodMat, Material glassMat, Material whiteMat, Material redMat, Material porcelainMat,
            Material crimsonMat, Material obsidianMat, Material leatherMat)
        {
            GameObject r = new GameObject("Artifact_DollGlassCase");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Vitrine_WoodBase", r.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.46f, 0.05f, 0.34f), woodMat, keepCollider: false);
            CreateBox("Vitrine_WoodTop", r.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.05f, 0.34f), woodMat, keepCollider: false);
            CreateBox("Vitrine_Back", r.transform, new Vector3(0f, 0.37f, -0.15f), new Vector3(0.44f, 0.68f, 0.03f), woodMat, keepCollider: false);
            GameObject glass = CreateBox("Vitrine_GlassEnclosure", r.transform, new Vector3(0f, 0.37f, 0.01f), new Vector3(0.43f, 0.66f, 0.31f), glassMat, keepCollider: false);
            BoxCollider col = glass.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(glass, "artifact:muneca-porcelana",
                "Vitrina de Contención: Muñeca de Porcelana (EXP-03 • Annabelle)",
                "Containment Vitrine: Porcelain Conduit Doll (EXP-03 • Annabelle)",
                "Muñeca de porcelana agrietada encerrada tras cristal bendecido. Letrero superior: 'ADVERTENCIA: POSITIVAMENTE NO ABRIR'.",
                "Cracked porcelain doll sealed behind blessed glass. Warning plaque: 'WARNING: POSITIVELY DO NOT OPEN'.",
                new Color(1.0f, 0.36f, 0.38f));
            CreateBox("Vitrine_SignBg", r.transform, new Vector3(0f, 0.63f, 0.168f), new Vector3(0.34f, 0.08f, 0.01f), whiteMat, keepCollider: false);
            CreateBox("Vitrine_SignStripe", r.transform, new Vector3(0f, 0.65f, 0.174f), new Vector3(0.32f, 0.028f, 0.008f), redMat, keepCollider: false);
            CreateBox("Doll_Chair", r.transform, new Vector3(0f, 0.16f, -0.04f), new Vector3(0.20f, 0.24f, 0.14f), woodMat, keepCollider: false);
            CreateBox("Doll_Skirt", r.transform, new Vector3(0f, 0.18f, 0.02f), new Vector3(0.20f, 0.22f, 0.14f), porcelainMat, keepCollider: false);
            CreateBox("Doll_Ribbon", r.transform, new Vector3(0f, 0.28f, 0.03f), new Vector3(0.17f, 0.045f, 0.12f), crimsonMat, keepCollider: false);
            CreateBox("Doll_Torso", r.transform, new Vector3(0f, 0.35f, 0.01f), new Vector3(0.14f, 0.13f, 0.09f), porcelainMat, keepCollider: false);
            CreateBox("Doll_Head", r.transform, new Vector3(0f, 0.48f, 0.02f), new Vector3(0.12f, 0.13f, 0.11f), porcelainMat, keepCollider: false);
            CreateBox("Doll_EyeL", r.transform, new Vector3(-0.028f, 0.49f, 0.078f), new Vector3(0.02f, 0.02f, 0.008f), obsidianMat, keepCollider: false);
            CreateBox("Doll_EyeR", r.transform, new Vector3(0.028f, 0.49f, 0.078f), new Vector3(0.02f, 0.02f, 0.008f), obsidianMat, keepCollider: false);
            CreateBox("Doll_BraidL", r.transform, new Vector3(-0.065f, 0.42f, 0.02f), new Vector3(0.03f, 0.15f, 0.03f), leatherMat, keepCollider: false);
            CreateBox("Doll_BraidR", r.transform, new Vector3(0.065f, 0.42f, 0.02f), new Vector3(0.03f, 0.15f, 0.03f), leatherMat, keepCollider: false);
        }

        private static void BuildSpiralPuppet(Transform parent, Vector3 pos, Quaternion rot, Material obsidianMat, Material whiteMat, Material redMat)
        {
            GameObject r = new GameObject("Artifact_SpiralPuppet");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            GameObject torso = CreateBox("Puppet_TuxedoTorso", r.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.28f, 0.14f), obsidianMat, keepCollider: false);
            BoxCollider col = torso.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 2.2f, 1.5f);
            col.isTrigger = true;
            AttachDesc(torso, "artifact:titere-espiral",
                "Títere de Mejillas en Espiral (EXP-05 • Billy / Saw)",
                "Spiral-Cheek Ventriloquist Puppet (EXP-05 • Billy / Saw)",
                "Posado sobre un cajón solitario con smoking negro y corbatín rojo. Su grabadora interna murmura: 'Quiero jugar un juego...'.",
                "Perched atop a lone wooden crate in black tuxedo and red bow tie. Its internal recorder whispers: 'I want to play a game...'.",
                new Color(0.95f, 0.22f, 0.24f));
            CreateBox("Puppet_Shirt", r.transform, new Vector3(0f, 0.20f, 0.072f), new Vector3(0.08f, 0.18f, 0.01f), whiteMat, keepCollider: false);
            CreateBox("Puppet_BowTie", r.transform, new Vector3(0f, 0.28f, 0.078f), new Vector3(0.085f, 0.036f, 0.015f), redMat, keepCollider: false);
            CreateBox("Puppet_Head", r.transform, new Vector3(0f, 0.41f, 0.01f), new Vector3(0.14f, 0.16f, 0.13f), whiteMat, keepCollider: false);
            CreateBox("Puppet_SpiralL", r.transform, new Vector3(-0.04f, 0.39f, 0.078f), new Vector3(0.036f, 0.036f, 0.008f), redMat, keepCollider: false);
            CreateBox("Puppet_SpiralR", r.transform, new Vector3(0.04f, 0.39f, 0.078f), new Vector3(0.036f, 0.036f, 0.008f), redMat, keepCollider: false);
            CreateBox("Puppet_EyeL", r.transform, new Vector3(-0.03f, 0.435f, 0.078f), new Vector3(0.02f, 0.018f, 0.008f), redMat, keepCollider: false);
            CreateBox("Puppet_EyeR", r.transform, new Vector3(0.03f, 0.435f, 0.078f), new Vector3(0.02f, 0.018f, 0.008f), redMat, keepCollider: false);
        }

        private static void BuildPossessedDollBox(Transform parent, Vector3 pos, Quaternion rot, Material yellowMat, Material blueMat, Material redMat, Material steelMat)
        {
            GameObject box = CreateBox("Artifact_PossessedDollBox", parent, pos, new Vector3(0.28f, 0.40f, 0.18f), yellowMat, keepCollider: false);
            box.transform.rotation = rot;
            BoxCollider col = box.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(box, "artifact:muneco-good-guy",
                "Caja de Juguete 'Good Guy' con Rasguños Internos (EXP-10 • Chucky)",
                "'Good Guy' Toy Box with Internal Claw Marks (EXP-10 • Child's Play)",
                "Caja amarilla asegurada con fleje de acero sobre cajas de archivo húmedas. Hay marcas de rasguños hechas desde el interior.",
                "Factory yellow toy box strapped in steel atop damp archive boxes. Deep claw marks tear outward from inside.",
                new Color(1.0f, 0.82f, 0.18f));
            CreateBox("GoodGuy_Window", box.transform, new Vector3(0f, -0.04f, 0.52f), new Vector3(0.68f, 0.64f, 0.06f), blueMat, keepCollider: false);
            CreateBox("GoodGuy_Head", box.transform, new Vector3(0f, 0.10f, 0.55f), new Vector3(0.36f, 0.24f, 0.06f), redMat, keepCollider: false);
            CreateBox("GoodGuy_Strap", box.transform, Vector3.zero, new Vector3(1.05f, 0.10f, 1.06f), steelMat, keepCollider: false);
        }

        private static void BuildDybbukBox(Transform parent, Vector3 pos, Quaternion rot, Material woodMat, Material saltMat, Material steelMat, Material redMat)
        {
            GameObject r = new GameObject("Artifact_DybbukBox_Group");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Dybbuk_SaltRing", r.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.46f, 0.016f, 0.36f), saltMat, keepCollider: false);
            GameObject box = CreateBox("Artifact_DybbukBox", r.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.32f, 0.40f, 0.24f), woodMat, keepCollider: false);
            BoxCollider col = box.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(box, "artifact:caja-dybbuk",
                "La Caja Dybbuk Sellada con Cadenas (EXP-01 • El Origen del Mal)",
                "Chained Dybbuk Wine Cabinet (EXP-01 • The Possession)",
                "Antigua licorera de caoba atada con cadenas de hierro y rodeada por un cerco de sal blanca sobre la alfombra húmeda.",
                "Antique mahogany wine cabinet bound in iron chains inside a white salt perimeter on the damp carpet.",
                new Color(0.92f, 0.64f, 0.28f));
            CreateBox("Dybbuk_ChainH", box.transform, Vector3.zero, new Vector3(1.06f, 0.14f, 1.06f), steelMat, keepCollider: false);
            CreateBox("Dybbuk_ChainV", box.transform, Vector3.zero, new Vector3(0.14f, 1.06f, 1.06f), steelMat, keepCollider: false);
            CreateBox("Dybbuk_WaxSeal", box.transform, new Vector3(0f, 0f, 0.54f), new Vector3(0.22f, 0.22f, 0.08f), redMat, keepCollider: false);
        }

        private static void BuildNecronomiconGrimoire(Transform parent, Vector3 pos, Quaternion rot,
            Material steelMat, Material leatherMat, Material brassMat, Material glowMat)
        {
            GameObject r = new GameObject("Artifact_NecronomiconGrimoire");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Grimoire_Lectern", r.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.28f, 0.14f, 0.20f), steelMat, keepCollider: false);
            GameObject tome = CreateBox("Grimoire_BoundTome", r.transform, new Vector3(0f, 0.20f, 0.02f), new Vector3(0.28f, 0.32f, 0.07f), leatherMat, keepCollider: false);
            tome.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            BoxCollider col = tome.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(tome, "artifact:grimorio-necronomicon",
                "Grimorio Encuadernado / Libro de los Muertos (EXP-06 • Evil Dead / Hamunaptra)",
                "Bound Grimoire / Book of the Dead (EXP-06 • Evil Dead / Hamunaptra)",
                "Códice antiguo con relieves orgánicos y herrajes egipcios de oro. Permanece sellado junto a la Caja Fuerte Nivel Ω en el Ala Noroeste.",
                "Ancient codex bound with organic ridges and golden Egyptian clasps, sealed beside the Level-Ω Vault Safe in the NW Wing.",
                new Color(0.95f, 0.68f, 0.24f));
            CreateBox("Grimoire_ClaspT", tome.transform, new Vector3(0f, 0.44f, 0.52f), new Vector3(0.86f, 0.10f, 0.18f), brassMat, keepCollider: false);
            CreateBox("Grimoire_ClaspB", tome.transform, new Vector3(0f, -0.44f, 0.52f), new Vector3(0.86f, 0.10f, 0.18f), brassMat, keepCollider: false);
            CreateBox("Grimoire_Sigil", tome.transform, new Vector3(0f, 0f, 0.54f), new Vector3(0.45f, 0.48f, 0.12f), glowMat, keepCollider: false);
        }

        private static void BuildCeramicHand(Transform parent, Vector3 pos, Quaternion rot, Material saltMat, Material porcelainMat, Material obsidianMat)
        {
            GameObject r = new GameObject("Artifact_CeramicHand");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Hand_SaltRing", r.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.22f, 0.015f, 0.20f), saltMat, keepCollider: false);
            GameObject arm = CreateBox("Hand_CeramicForearm", r.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.08f, 0.24f, 0.08f), porcelainMat, keepCollider: false);
            BoxCollider col = arm.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 2.0f, 2.2f);
            col.isTrigger = true;
            AttachDesc(arm, "artifact:mano-ceramica",
                "La Mano Embalsamada de Cerámica (EXP-07 • Háblame / Talk to Me)",
                "Embalmed Ceramic Hand (EXP-07 • Talk to Me)",
                "Antebrazo de cerámica blanca cubierto de firmas oscuras. Regla: jamás sostenerla por más de 90 segundos.",
                "White ceramic forearm covered in dark signatures. Rule: never hold it for longer than 90 seconds.",
                new Color(0.92f, 0.88f, 0.78f));
            CreateBox("Hand_Palm", r.transform, new Vector3(0f, 0.30f, 0f), new Vector3(0.11f, 0.095f, 0.05f), porcelainMat, keepCollider: false);
            float[] fXs = new float[] { -0.038f, -0.012f, 0.012f, 0.038f };
            for (int f = 0; f < fXs.Length; f++)
                CreateBox($"Hand_Finger_{f}", r.transform, new Vector3(fXs[f], 0.39f, 0f), new Vector3(0.018f, 0.095f, 0.022f), porcelainMat, keepCollider: false);
            CreateBox("Hand_Thumb", r.transform, new Vector3(-0.065f, 0.32f, 0.01f), new Vector3(0.036f, 0.055f, 0.022f), porcelainMat, keepCollider: false);
            CreateBox("Hand_Ink", r.transform, new Vector3(0f, 0.17f, 0.044f), new Vector3(0.055f, 0.12f, 0.006f), obsidianMat, keepCollider: false);
        }

        private static void BuildLamentPuzzleBox(Transform parent, Vector3 pos, Quaternion rot, Material velvetMat, Material obsidianMat, Material brassMat)
        {
            GameObject r = new GameObject("Artifact_LamentPuzzleBox");
            r.transform.SetParent(parent, false);
            r.transform.position = pos;
            r.transform.rotation = rot;
            CreateBox("Lament_VelvetCushion", r.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.24f, 0.06f, 0.20f), velvetMat, keepCollider: false);
            GameObject cube = CreateBox("Lament_PuzzleCube", r.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.15f, 0.15f, 0.15f), obsidianMat, keepCollider: false);
            cube.transform.localRotation = Quaternion.Euler(14f, 32f, -10f);
            BoxCollider col = cube.AddComponent<BoxCollider>();
            col.isTrigger = true;
            AttachDesc(cube, "artifact:configuracion-lamento",
                "Caja Rompecabezas de Filigrana (EXP-08 • La Configuración del Lamento)",
                "Filigree Puzzle Box (EXP-08 • The Lament Configuration)",
                "Cubo mecánico de laca negra y latón grabado con anillos concéntricos, bloqueado a mitad de su secuencia.",
                "Mechanical cube of black lacquer and etched brass locked mid-sequence to prevent opening a rift.",
                new Color(1.0f, 0.78f, 0.28f));
            CreateBox("Lament_GoldF", cube.transform, new Vector3(0f, 0f, 0.52f), new Vector3(0.76f, 0.76f, 0.04f), brassMat, keepCollider: false);
            CreateBox("Lament_GoldT", cube.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.76f, 0.04f, 0.76f), brassMat, keepCollider: false);
            CreateBox("Lament_GoldS", cube.transform, new Vector3(0.52f, 0f, 0f), new Vector3(0.04f, 0.76f, 0.76f), brassMat, keepCollider: false);
        }

        private static void BuildOuijaBoard(Transform parent, Vector3 pos, Quaternion rot, Material woodMat, Material darkMat, Material porcelainMat)
        {
            GameObject board = CreateBox("Artifact_OuijaBoard", parent, pos, new Vector3(0.48f, 0.03f, 0.34f), woodMat, keepCollider: false);
            board.transform.rotation = rot;
            BoxCollider col = board.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 4.0f, 1.2f);
            col.isTrigger = true;
            AttachDesc(board, "artifact:tablero-ouija",
                "Tablero Espiritista y Planchette (EXP-09 • El Exorcista / Ouija)",
                "Spirit Board & Planchette (EXP-09 • The Exorcist / Ouija)",
                "Tablero clásico de madera grabada con el puntero clavado sobre 'GOODBYE'.",
                "Classic engraved spirit board with its planchette pinned over 'GOODBYE'.",
                new Color(0.86f, 0.72f, 0.46f));
            CreateBox("Ouija_Arc1", board.transform, new Vector3(0f, 0.58f, 0.14f), new Vector3(0.74f, 0.08f, 0.08f), darkMat, keepCollider: false);
            CreateBox("Ouija_Arc2", board.transform, new Vector3(0f, 0.58f, -0.02f), new Vector3(0.68f, 0.08f, 0.08f), darkMat, keepCollider: false);
            CreateBox("Ouija_Planchette", board.transform, new Vector3(0.14f, 0.82f, -0.22f), new Vector3(0.22f, 0.45f, 0.24f), porcelainMat, keepCollider: false);
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

        private static string EnsureBackroomsWallpaperTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 256;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float dampBottom = y < 44 ? (1f - y / 44f) * 0.18f : 0f;
                for (int x = 0; x < sz; x++)
                {
                    float stripe = ((x % 32) < 4) ? -0.055f : 0f;
                    int gx = (x % 32) - 16;
                    int gy = (y % 32) - 16;
                    bool diamond = Mathf.Abs(gx) + Mathf.Abs(gy) == 7 || (Mathf.Abs(gx) == 0 && Mathf.Abs(gy) < 6);
                    float motif = diamond ? -0.045f : 0f;
                    float noise = (Mathf.PerlinNoise(x * 0.08f, y * 0.08f) - 0.5f) * 0.06f;
                    float r = Mathf.Clamp01(0.84f + stripe + motif + noise - dampBottom);
                    float g = Mathf.Clamp01(0.76f + stripe + motif + noise - dampBottom * 1.05f);
                    float b = Mathf.Clamp01(0.45f + stripe * 0.6f + motif * 0.6f + noise * 0.5f - dampBottom * 0.9f);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureBackroomsCarpetTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 256;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float grain = (Mathf.PerlinNoise(x * 0.35f, y * 0.35f) - 0.5f) * 0.10f;
                    float damp = Mathf.PerlinNoise(x * 0.035f + 7f, y * 0.035f + 3f) > 0.66f ? -0.12f : 0f;
                    float r = Mathf.Clamp01(0.62f + grain + damp);
                    float g = Mathf.Clamp01(0.54f + grain + damp);
                    float b = Mathf.Clamp01(0.32f + grain * 0.7f + damp * 0.8f);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return assetPath;
        }

        private static string EnsureBackroomsCeilingGridTexture(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            const int sz = 256;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    bool gridLine = (x % 64) < 3 || (y % 64) < 3;
                    if (gridLine)
                    {
                        tex.SetPixel(x, y, new Color(0.56f, 0.55f, 0.48f));
                    }
                    else
                    {
                        float speckle = (Mathf.PerlinNoise(x * 0.48f, y * 0.48f) - 0.5f) * 0.09f;
                        float r = Mathf.Clamp01(0.84f + speckle);
                        float g = Mathf.Clamp01(0.81f + speckle);
                        float b = Mathf.Clamp01(0.68f + speckle);
                        tex.SetPixel(x, y, new Color(r, g, b));
                    }
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
