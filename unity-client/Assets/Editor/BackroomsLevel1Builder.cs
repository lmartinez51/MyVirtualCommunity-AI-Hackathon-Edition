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
    /// A 14m x 14m liminal Backrooms Level 0 maze (origin Y = -50.0m, clear height 2.85m)
    /// loaded additively when the player crosses the Threshold Veil inside the Building A South
    /// mysterious door. Features procedural Level 0 yellow wallpaper, damp mustard carpet,
    /// acoustic drop-ceiling grid with humming/flickering fluorescent light panels, ordinary
    /// liminal office clutter, all 13 pop-culture horror & esoteric containment artifacts,
    /// the Level-Omega Vault Safe ("654321") holding "El Ojo del Umbral", and the single
    /// monumental Dimensional Mirror-Portal ("El Umbral de Lasser") leading to future Level 2.
    /// </summary>
    public static class BackroomsLevel1Builder
    {
        public const string ScenePath = "Assets/Scenes/BackroomsLevel1Scene.unity";
        public const float BaseY = -50.00f;
        public const float ClearHeight = 2.85f;
        public const float CeilingY = BaseY + ClearHeight; // -47.15f

        [MenuItem("MyVirtualCommunity/Build Backrooms Level 1 Scene")]
        public static void BuildBackroomsLevel1Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Procedural Level 0 textures (seamless wallpaper, damp carpet, acoustic ceiling tile grid)
            string wallpaperTexPath = EnsureBackroomsWallpaperTexture("Assets/Textures/Environment/T_Backrooms_YellowWallpaper.png");
            string carpetTexPath = EnsureBackroomsCarpetTexture("Assets/Textures/Environment/T_Backrooms_DampCarpet.png");
            string ceilingTexPath = EnsureBackroomsCeilingGridTexture("Assets/Textures/Environment/T_Backrooms_CeilingGrid.png");
            AssetDatabase.Refresh();

            Material wallMat = CreateTexturedMat("Assets/Settings/Backrooms_YellowWallpaper.mat", wallpaperTexPath, new Color(0.95f, 0.92f, 0.78f), 0.12f, new Vector2(1.5f, 1.0f));
            Material carpetMat = CreateTexturedMat("Assets/Settings/Backrooms_DampCarpet.mat", carpetTexPath, new Color(0.86f, 0.80f, 0.62f), 0.06f, new Vector2(7f, 7f));
            Material ceilingMat = CreateTexturedMat("Assets/Settings/Backrooms_CeilingGrid.mat", ceilingTexPath, new Color(0.92f, 0.90f, 0.82f), 0.10f, new Vector2(7f, 7f));
            Material baseboardMat = CreateColorMat("Assets/Settings/Backrooms_Baseboard.mat", new Color(0.28f, 0.23f, 0.14f), 0.18f, 0.0f);
            Material fluorOnMat = CreateEmissiveMat("Assets/Settings/Backrooms_FluorescentOn.mat", new Color(1.0f, 0.98f, 0.84f), new Color(1.0f, 0.95f, 0.74f) * 2.3f);
            Material fluorOffMat = CreateColorMat("Assets/Settings/Backrooms_FluorescentOff.mat", new Color(0.45f, 0.44f, 0.38f), 0.35f, 0.1f);
            Material voidBlackMat = CreateColorMat("Assets/Settings/Backrooms_CeilingVoid.mat", new Color(0.03f, 0.03f, 0.03f), 0.05f, 0.0f);

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

            // Attach master interactive vault/portal controller inside Backrooms Level 1
            SecretRoomDimensionVaultController vaultCtrl = root.AddComponent<SecretRoomDimensionVaultController>();
            vaultCtrl.dormantMirrorMaterial = portalDormantMat;
            vaultCtrl.activeVortexMaterial = portalActiveVortexMat;

            // =========================================================================
            // 1. FLOOR, ACOUSTIC DROP CEILING & OUTER 14m x 14m PERIMETER (X=-7..+7, Z=-7..+7)
            // =========================================================================
            GameObject shellRoot = new GameObject("Backrooms_Architecture");
            shellRoot.transform.SetParent(root.transform, false);

            // Damp mustard carpet floor (top exactly at BaseY = -50.00m)
            CreateBox("Backrooms_Floor", shellRoot.transform,
                new Vector3(0f, BaseY - 0.15f, 0f), new Vector3(14.6f, 0.30f, 14.6f), carpetMat, keepCollider: true);

            // Acoustic office ceiling slab (bottom at CeilingY = -47.15m) + dark void plenum box above missing tile
            CreateBox("Backrooms_Ceiling", shellRoot.transform,
                new Vector3(0f, CeilingY + 0.10f, 0f), new Vector3(14.6f, 0.20f, 14.6f), ceilingMat, keepCollider: true);
            CreateBox("Backrooms_MissingTileVoid", shellRoot.transform,
                new Vector3(0.8f, CeilingY - 0.005f, 0.6f), new Vector3(0.68f, 0.02f, 0.68f), voidBlackMat, keepCollider: false);
            GameObject crookedTile = CreateBox("Backrooms_CrookedCeilingTile", shellRoot.transform,
                new Vector3(1.18f, CeilingY - 0.08f, 0.62f), new Vector3(0.62f, 0.025f, 0.62f), ceilingMat, keepCollider: false);
            crookedTile.transform.localRotation = Quaternion.Euler(14f, 8f, -18f);

            // Outer Perimeter Walls
            CreateWallSegment("Wall_Outer_South", shellRoot.transform, new Vector2(-7.0f, -7.0f), new Vector2(7.0f, -7.0f), 0.24f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_North", shellRoot.transform, new Vector2(-7.0f, 7.0f), new Vector2(7.0f, 7.0f), 0.24f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_West", shellRoot.transform, new Vector2(-7.0f, -7.0f), new Vector2(-7.0f, 7.0f), 0.24f, wallMat, baseboardMat);
            CreateWallSegment("Wall_Outer_East", shellRoot.transform, new Vector2(7.0f, -7.0f), new Vector2(7.0f, 7.0f), 0.24f, wallMat, baseboardMat);

            // =========================================================================
            // 2. LIMINAL MAZE PARTITIONS, CORRIDORS, OFFICES & PILLARED HALL
            //    Every doorway/corridor is >= 1.8m wide for effortless navigation!
            // =========================================================================
            // SW Abandoned Office & Surveillance Room (X = -7..-2.6, Z = -7..-2.6)
            CreateWallSegment("Wall_SWOffice_East", shellRoot.transform, new Vector2(-2.6f, -7.0f), new Vector2(-2.6f, -4.4f), 0.20f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SWOffice_North", shellRoot.transform, new Vector2(-7.0f, -2.6f), new Vector2(-4.4f, -2.6f), 0.20f, wallMat, baseboardMat);

            // Arrival Vestibule Center Sightline Baffle (X = -1.0..+1.0, Z = -2.6)
            CreateWallSegment("Wall_Arrival_Baffle", shellRoot.transform, new Vector2(-1.0f, -2.6f), new Vector2(1.0f, -2.6f), 0.20f, wallMat, baseboardMat);

            // NW Doll & Sealed Containment Room (X = -7..-2.2, Z = +2.6..+7.0)
            CreateWallSegment("Wall_NWRoom_South", shellRoot.transform, new Vector2(-7.0f, 2.6f), new Vector2(-4.2f, 2.6f), 0.20f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NWRoom_East", shellRoot.transform, new Vector2(-2.2f, 2.6f), new Vector2(-2.2f, 4.8f), 0.20f, wallMat, baseboardMat);

            // SE Level-Omega Vault Office (X = +2.6..+7.0, Z = -7.0..-1.4)
            CreateWallSegment("Wall_SEVault_West", shellRoot.transform, new Vector2(2.6f, -7.0f), new Vector2(2.6f, -3.4f), 0.20f, wallMat, baseboardMat);
            CreateWallSegment("Wall_SEVault_North", shellRoot.transform, new Vector2(4.4f, -1.4f), new Vector2(7.0f, -1.4f), 0.20f, wallMat, baseboardMat);

            // NE Dimensional Portal Chamber (X = +1.8..+7.0, Z = +1.8..+7.0)
            CreateWallSegment("Wall_NEPortal_South", shellRoot.transform, new Vector2(3.8f, 1.8f), new Vector2(7.0f, 1.8f), 0.20f, wallMat, baseboardMat);
            CreateWallSegment("Wall_NEPortal_West", shellRoot.transform, new Vector2(1.8f, 1.8f), new Vector2(1.8f, 4.8f), 0.20f, wallMat, baseboardMat);

            // Iconic Kane Pixels Pillared Hall Square Yellow Columns (West Hall + Center)
            CreatePillar("Pillar_WestHall_Phone", shellRoot.transform, -4.6f, -0.4f, 0.64f, wallMat, baseboardMat);
            CreatePillar("Pillar_WestHall_Mid", shellRoot.transform, -2.4f, 0.9f, 0.64f, wallMat, baseboardMat);
            CreatePillar("Pillar_Center_North", shellRoot.transform, -0.2f, 3.6f, 0.64f, wallMat, baseboardMat);
            CreatePillar("Pillar_EastCorridor", shellRoot.transform, 1.4f, -0.2f, 0.56f, wallMat, baseboardMat);

            // =========================================================================
            // 3. CLASSIC LEVEL 0 FLUORESCENT CEILING LIGHT PANELS (WITH SUBTLE FLICKER)
            // =========================================================================
            GameObject lightsRoot = new GameObject("Backrooms_FluorescentLights");
            lightsRoot.transform.SetParent(root.transform, false);

            (float x, float z, bool lit, bool flicker, float intensity)[] fixtures = new[]
            {
                ( 0.0f, -5.0f, true,  false, 1.15f), // Arrival Vestibule
                (-4.8f, -4.8f, true,  false, 1.05f), // SW Office
                (-5.2f,  0.2f, true,  true,  0.95f), // West Pillared Hall (flickering near CRT TV)
                (-2.0f, -0.6f, true,  false, 1.05f), // West-Central Hall
                ( 0.4f,  1.2f, true,  true,  1.00f), // Center Cross-Hall (flickering)
                (-4.8f,  4.8f, true,  false, 1.00f), // NW Doll Room
                (-0.2f,  5.6f, false, false, 0.00f), // Dead tube in North corridor!
                ( 4.8f, -4.4f, true,  false, 1.10f), // SE Vault Office
                ( 4.6f,  0.2f, true,  false, 0.95f), // East Approach Hall
                ( 4.4f,  4.4f, true,  true,  0.65f), // NE Portal Chamber (dimmer atmospheric glow)
            };

            for (int i = 0; i < fixtures.Length; i++)
            {
                var fx = fixtures[i];
                GameObject panel = CreateBox($"Backrooms_FluorPanel_{i}", lightsRoot.transform,
                    new Vector3(fx.x, CeilingY - 0.015f, fx.z), new Vector3(1.16f, 0.03f, 0.56f),
                    fx.lit ? fluorOnMat : fluorOffMat, keepCollider: false);
                CreateBox($"Backrooms_FluorFrame_{i}", panel.transform,
                    new Vector3(0f, 0.25f, 0f), new Vector3(1.05f, 0.8f, 1.06f), rackSteelMat, keepCollider: false);

                if (fx.lit)
                {
                    GameObject lGo = new GameObject($"Backrooms_FluorLight_{i}");
                    lGo.transform.SetParent(panel.transform, false);
                    lGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
                    Light l = lGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1.0f, 0.95f, 0.72f); // Classic warm greenish-yellow Level 0 fluorescent tint
                    l.intensity = fx.intensity;
                    l.range = 7.4f;
                    l.shadows = LightShadows.None;

                    if (fx.flicker)
                    {
                        var fl = panel.AddComponent<BackroomsAmbientFlicker>();
                        fl.targetLight = l;
                        fl.panelRenderer = panel.GetComponent<Renderer>();
                        fl.baseIntensity = fx.intensity;
                        fl.flickerStrength = 0.28f;
                        fl.phaseOffset = i * 1.7f;
                    }
                }
            }

            // =========================================================================
            // 4. ARRIVAL SPAWN POINT & DOUBLE METAL EXIT DOOR BACK TO AV. BENITO JUÁREZ
            // =========================================================================
            GameObject spawnPoint = new GameObject("Backrooms_SpawnPoint");
            spawnPoint.transform.SetParent(root.transform, false);
            spawnPoint.transform.position = new Vector3(0f, BaseY + 0.06f, -5.85f);
            spawnPoint.transform.rotation = Quaternion.identity; // Facing +Z into the yellow maze

            GameObject exitDoorRoot = new GameObject("Backrooms_ExitDoor_ToStreet");
            exitDoorRoot.transform.SetParent(root.transform, false);
            exitDoorRoot.transform.position = new Vector3(0f, BaseY, -6.86f);
            CreateBox("ExitDoor_Frame_L", exitDoorRoot.transform, new Vector3(-0.68f, 1.04f, 0f), new Vector3(0.08f, 2.08f, 0.12f), rackSteelMat, keepCollider: false);
            CreateBox("ExitDoor_Frame_R", exitDoorRoot.transform, new Vector3(0.68f, 1.04f, 0f), new Vector3(0.08f, 2.08f, 0.12f), rackSteelMat, keepCollider: false);
            CreateBox("ExitDoor_Frame_T", exitDoorRoot.transform, new Vector3(0f, 2.08f, 0f), new Vector3(1.44f, 0.08f, 0.12f), rackSteelMat, keepCollider: false);
            GameObject exitLeafL = CreateBox("ExitDoor_Leaf_L", exitDoorRoot.transform, new Vector3(-0.32f, 1.02f, 0.02f), new Vector3(0.62f, 2.02f, 0.05f), doorMetalMat, keepCollider: false);
            CreateBox("ExitDoor_Leaf_R", exitDoorRoot.transform, new Vector3(0.32f, 1.02f, 0.02f), new Vector3(0.62f, 2.02f, 0.05f), doorMetalMat, keepCollider: false);
            CreateBox("ExitDoor_CenterSeam", exitDoorRoot.transform, new Vector3(0f, 1.02f, 0.05f), new Vector3(0.02f, 2.02f, 0.02f), obsidianMat, keepCollider: false);
            CreateBox("ExitDoor_Handle", exitDoorRoot.transform, new Vector3(0.08f, 1.02f, 0.07f), new Vector3(0.03f, 0.16f, 0.05f), aluminumMat, keepCollider: false);
            BoxCollider exitTrig = exitDoorRoot.AddComponent<BoxCollider>();
            exitTrig.center = new Vector3(0f, 1.10f, 0.12f);
            exitTrig.size = new Vector3(1.40f, 2.15f, 0.35f);
            exitTrig.isTrigger = true;
            exitDoorRoot.AddComponent<BackroomsExitDoorTrigger>();

            // =========================================================================
            // 5. ORDINARY / "RANDOM" LIMINAL BACKROOMS PROPS (Chairs, Desks, Ladder, Boxes, Cooler)
            // =========================================================================
            GameObject propsRoot = new GameObject("Backrooms_OrdinaryLiminalProps");
            propsRoot.transform.SetParent(root.transform, false);

            // Wooden step-ladder abandoned in the Arrival Corridor
            CreateStepLadder(propsRoot.transform, new Vector3(1.55f, BaseY, -4.15f), 22f, darkMahoganyMat);
            // Solitary office chair staring at a blank wall in the Central Cross-Hall
            CreateOfficeChair(propsRoot.transform, new Vector3(0.95f, BaseY, 1.85f), 170f, chairFabricMat, rackSteelMat);
            // Overturned/askew office chair in West Hall
            CreateOfficeChair(propsRoot.transform, new Vector3(-3.35f, BaseY, 0.15f), -35f, chairFabricMat, rackSteelMat);
            // Empty office water cooler jug in SW Office corner
            CreateWaterCooler(propsRoot.transform, new Vector3(-6.45f, BaseY, -3.20f), officeBeigeMat, waterBlueMat);
            // Stacked damp cardboard archive boxes in corners
            CreateCardboardStack(propsRoot.transform, new Vector3(-1.85f, BaseY, -6.25f), 12f, cardboardMat);
            CreateCardboardStack(propsRoot.transform, new Vector3(-6.25f, BaseY, 3.35f), -8f, cardboardMat);
            CreateCardboardStack(propsRoot.transform, new Vector3(2.55f, BaseY, 6.25f), 18f, cardboardMat);
            // Red fire extinguisher mounted in East Corridor
            CreateBox("BackroomsProp_FireExtinguisher", propsRoot.transform, new Vector3(2.45f, BaseY + 0.95f, -3.80f), new Vector3(0.14f, 0.42f, 0.14f), redThreadMat, keepCollider: false);

            // =========================================================================
            // 6. THE 13 HORROR & ESOTERIC CONTAINMENT ARTIFACTS DISPERSED ACROSS THE MAZE
            // =========================================================================
            GameObject artifactsRoot = new GameObject("Backrooms_DispersedArtifacts");
            artifactsRoot.transform.SetParent(root.transform, false);

            // --- ZONE A: SW ABANDONED OFFICE (X=-7..-2.6, Z=-7..-2.6) ---
            // Desk + Chair + Filing Cabinet + Evidence Corkboard (Safe Code Clue 654321) + 03:07 Clock + Babadook Book
            CreateOfficeDesk(propsRoot.transform, new Vector3(-5.20f, BaseY, -6.25f), 0f, officeBeigeMat, rackSteelMat);
            CreateOfficeChair(propsRoot.transform, new Vector3(-5.20f, BaseY, -5.45f), 185f, chairFabricMat, rackSteelMat);
            CreateFilingCabinet(propsRoot.transform, new Vector3(-6.50f, BaseY, -5.20f), 90f, officeBeigeMat, aluminumMat);

            // Evidence Corkboard PDN-00 on West Wall of SW Office (with 654321 clue!)
            BuildEvidenceCorkboard(artifactsRoot.transform, new Vector3(-6.84f, BaseY + 1.48f, -4.40f), Quaternion.Euler(0f, 180f, 0f),
                darkMahoganyMat, corkMat, porcelainMat, astrolabeCoreMat, stickyYellowMat, redThreadMat);

            // 03:07 AM Brass Alarm Clock on SW Office Desk
            BuildClock307(artifactsRoot.transform, new Vector3(-4.82f, BaseY + 0.82f, -6.20f), Quaternion.Euler(0f, 15f, 0f),
                antiqueBrassMat, porcelainMat, obsidianMat);

            // Babadook Pop-Up Storybook on top of SW Office Filing Cabinet
            BuildBabadookBook(artifactsRoot.transform, new Vector3(-6.45f, BaseY + 1.24f, -5.20f), Quaternion.Euler(0f, 80f, 0f),
                obsidianMat, saltWhiteMat, crimsonVelvetMat);

            // --- ZONE B: WEST PILLARED HALL & CRT NICHE (X=-7..-1, Z=-2.6..+2.6) ---
            // The Black Rotary Phone mounted on Pillar_WestHall_Phone (-4.6, -0.4) facing East (+X)
            BuildBlackRotaryPhone(artifactsRoot.transform, new Vector3(-4.24f, BaseY + 1.42f, -0.40f), Quaternion.Euler(0f, 180f, 0f),
                obsidianMat, aluminumMat);

            // 90s CRT TV + Cursed VHS ("7 DÍAS" / The Ring) on a low rolling AV cart at West Niche (-6.45, 0.0)
            CreateBox("CrtTv_RollingCart", propsRoot.transform, new Vector3(-6.40f, BaseY + 0.34f, 0.0f), new Vector3(0.54f, 0.68f, 0.68f), rackSteelMat, keepCollider: true);
            BuildCrtTvAndVhs(artifactsRoot.transform, new Vector3(-6.38f, BaseY + 0.68f, 0.0f), Quaternion.Euler(0f, 90f, 0f),
                rackSteelMat, crtStaticMat, saltWhiteMat, obsidianMat);

            // --- ZONE C: CENTRAL CROSS-HALL ---
            // Small side table with Anomalous Astral Compass pointing straight NE toward the Portal Chamber!
            CreateBox("Compass_SideTable", propsRoot.transform, new Vector3(0.0f, BaseY + 0.38f, -1.95f), new Vector3(0.55f, 0.76f, 0.45f), darkMahoganyMat, keepCollider: true);
            BuildAnomalousCompass(artifactsRoot.transform, new Vector3(0.0f, BaseY + 0.79f, -1.95f), Quaternion.Euler(0f, 34f, 0f),
                antiqueBrassMat, astrolabeCoreMat);

            // --- ZONE D: NW DOLL & CONTAINMENT ROOM (X=-7..-2.2, Z=+2.6..+7.0) ---
            // Pedestal + Annabelle Sealed Glass Vitrine
            CreateBox("DollVitrine_Pedestal", propsRoot.transform, new Vector3(-5.60f, BaseY + 0.46f, 6.20f), new Vector3(0.52f, 0.92f, 0.46f), darkMahoganyMat, keepCollider: true);
            BuildDollGlassCase(artifactsRoot.transform, new Vector3(-5.60f, BaseY + 0.92f, 6.20f), Quaternion.Euler(0f, 180f, 0f),
                darkMahoganyMat, vitrineGlassMat, saltWhiteMat, redThreadMat, porcelainMat, crimsonVelvetMat, obsidianMat, agedLeatherMat);

            // Billy Spiral-Cheek Puppet seated on an office chair in the NW corner
            CreateOfficeChair(propsRoot.transform, new Vector3(-3.30f, BaseY, 6.15f), 205f, chairFabricMat, rackSteelMat);
            BuildSpiralPuppet(artifactsRoot.transform, new Vector3(-3.30f, BaseY + 0.50f, 6.15f), Quaternion.Euler(0f, 205f, 0f),
                obsidianMat, saltWhiteMat, redThreadMat);

            // Good Guy Yellow Box (Chucky) on a filing cabinet in NW Room
            CreateFilingCabinet(propsRoot.transform, new Vector3(-6.48f, BaseY, 4.95f), 90f, officeBeigeMat, aluminumMat);
            BuildPossessedDollBox(artifactsRoot.transform, new Vector3(-6.44f, BaseY + 1.38f, 4.95f), Quaternion.Euler(0f, 95f, 0f),
                goodGuyYellowMat, toyBoxBlueMat, redThreadMat, rackSteelMat);

            // Chained Dybbuk Box on the damp carpet inside a white salt ring in NW Room
            BuildDybbukBox(artifactsRoot.transform, new Vector3(-2.85f, BaseY + 0.02f, 3.35f), Quaternion.Euler(0f, -145f, 0f),
                darkMahoganyMat, saltWhiteMat, rackSteelMat, redThreadMat);

            // --- ZONE E: SE LEVEL-OMEGA VAULT OFFICE (X=+2.6..+7.0, Z=-7.0..-1.4) ---
            // 1. Heavy Level-Omega Vault Safe ("654321") holding "El Ojo del Umbral" against South wall!
            BuildVaultSafeWithAstrolabe(
                artifactsRoot.transform,
                vaultCtrl,
                new Vector3(5.65f, BaseY, -6.48f),
                Quaternion.identity, // Faces +Z into SE Vault Office
                darkCorniceMat: rackSteelMat,
                safeGunmetalMat: safeGunmetalMat,
                crimsonVelvetMat: crimsonVelvetMat,
                obsidianMat: obsidianMat,
                antiqueBrassMat: antiqueBrassMat,
                aluminumMat: aluminumMat,
                safeLedAmberMat: safeLedAmberMat,
                astrolabeCoreMat: astrolabeCoreMat,
                portalRuneVioletMat: portalRuneVioletMat);

            // 2. Long Containment Relic Table along East Wall of SE Office (X = +6.45, Z = -4.20)
            CreateBox("SEVault_RelicTable", propsRoot.transform, new Vector3(6.40f, BaseY + 0.42f, -4.15f), new Vector3(0.68f, 0.84f, 2.35f), darkMahoganyMat, keepCollider: true);
            // Necronomicon Grimoire on Lectern
            BuildNecronomiconGrimoire(artifactsRoot.transform, new Vector3(6.35f, BaseY + 0.84f, -4.95f), Quaternion.Euler(0f, -90f, 0f),
                rackSteelMat, agedLeatherMat, antiqueBrassMat, safeLedAmberMat);
            // Embalmed Ceramic Hand (Talk to Me) inside salt ring
            BuildCeramicHand(artifactsRoot.transform, new Vector3(6.35f, BaseY + 0.84f, -4.25f), Quaternion.Euler(0f, -90f, 0f),
                saltWhiteMat, porcelainMat, obsidianMat);
            // Lament Configuration Puzzle Cube (Hellraiser) on crimson velvet pillow
            BuildLamentPuzzleBox(artifactsRoot.transform, new Vector3(6.35f, BaseY + 0.84f, -3.55f), Quaternion.Euler(0f, -90f, 0f),
                crimsonVelvetMat, obsidianMat, antiqueBrassMat);
            // Ouija Spirit Board & Planchette on table against North partition
            CreateBox("SEVault_SideDesk", propsRoot.transform, new Vector3(5.45f, BaseY + 0.38f, -1.95f), new Vector3(1.15f, 0.76f, 0.56f), officeBeigeMat, keepCollider: true);
            BuildOuijaBoard(artifactsRoot.transform, new Vector3(5.15f, BaseY + 0.80f, -1.95f), Quaternion.Euler(0f, 180f, 0f),
                agedLeatherMat, obsidianMat, porcelainMat);
            // Paquimé Obsidian & Turquoise Ceremonial Mask beside Ouija Board
            BuildPaquimeMask(artifactsRoot.transform, new Vector3(5.78f, BaseY + 0.88f, -1.95f), Quaternion.Euler(0f, -90f, 0f),
                obsidianMat, turquoiseGlowMat, antiqueBrassMat);

            // =========================================================================
            // 7. ZONE G: NE DEEP CHAMBER (X=+1.8..+7.0, Z=+1.8..+7.0) —
            //    THE SINGLE MONUMENTAL DIMENSIONAL MIRROR-PORTAL ("EL UMBRAL DE LASSER")
            // =========================================================================
            BuildMonumentalDimensionalPortal(
                artifactsRoot.transform,
                vaultCtrl,
                new Vector3(4.50f, BaseY, 6.76f),
                Quaternion.Euler(0f, 90f, 0f), // Faces South (-Z) into the NE Chamber!
                saltWhiteMat, obsidianMat, antiqueBrassMat, portalDormantMat, portalRuneVioletMat,
                astrolabeCoreMat, rackSteelMat, redThreadMat);

            vaultCtrl.RefreshVisualState();

            // Save BackroomsLevel1Scene cleanly and ensure it is registered in EditorBuildSettings
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BackroomsLevel1Builder] Built & saved 14m x 14m Backrooms Level 0 maze with all 13 artifacts, Level-Omega Safe & Monumental Portal at {ScenePath}.");
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
        // STRUCTURAL & PROP BUILDERS
        // =====================================================================
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

            // Dark wooden/vinyl baseboard along both sides
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

        private static void CreateOfficeDesk(Transform parent, Vector3 pos, float yaw, Material topMat, Material legMat)
        {
            GameObject g = new GameObject("BackroomsProp_OfficeDesk");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Desk_Top", g.transform, new Vector3(0f, 0.74f, 0f), new Vector3(1.36f, 0.05f, 0.68f), topMat, keepCollider: true);
            CreateBox("Desk_Drawers", g.transform, new Vector3(0.45f, 0.36f, 0f), new Vector3(0.40f, 0.72f, 0.60f), topMat, keepCollider: true);
            CreateBox("Desk_LegL1", g.transform, new Vector3(-0.62f, 0.36f, -0.28f), new Vector3(0.05f, 0.72f, 0.05f), legMat, keepCollider: false);
            CreateBox("Desk_LegL2", g.transform, new Vector3(-0.62f, 0.36f, 0.28f), new Vector3(0.05f, 0.72f, 0.05f), legMat, keepCollider: false);
        }

        private static void CreateOfficeChair(Transform parent, Vector3 pos, float yaw, Material fabricMat, Material metalMat)
        {
            GameObject g = new GameObject("BackroomsProp_OfficeChair");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Chair_Seat", g.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.48f, 0.06f, 0.46f), fabricMat, keepCollider: true);
            CreateBox("Chair_Back", g.transform, new Vector3(0f, 0.74f, -0.21f), new Vector3(0.44f, 0.50f, 0.06f), fabricMat, keepCollider: false);
            CreateBox("Chair_Stem", g.transform, new Vector3(0f, 0.23f, 0f), new Vector3(0.05f, 0.44f, 0.05f), metalMat, keepCollider: false);
            CreateBox("Chair_BaseX", g.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.52f, 0.04f, 0.05f), metalMat, keepCollider: false);
            CreateBox("Chair_BaseZ", g.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.04f, 0.52f), metalMat, keepCollider: false);
        }

        private static void CreateFilingCabinet(Transform parent, Vector3 pos, float yaw, Material bodyMat, Material handleMat)
        {
            GameObject g = new GameObject("BackroomsProp_FilingCabinet");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBox("Cab_Body", g.transform, new Vector3(0f, 0.60f, 0f), new Vector3(0.48f, 1.20f, 0.56f), bodyMat, keepCollider: true);
            for (int d = 0; d < 4; d++)
            {
                CreateBox($"Cab_Handle_{d}", g.transform, new Vector3(0f, 0.18f + d * 0.28f, 0.29f), new Vector3(0.14f, 0.025f, 0.03f), handleMat, keepCollider: false);
            }
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

            // Larger, imposing freestanding Vault Safe (0.72m W x 0.96m H x 0.54m D)
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

            // "El Ojo del Umbral" Astrolabe Keystone inside Safe
            GameObject safeAstro = new GameObject("VaultSafe_AstrolabeKeystone");
            safeAstro.transform.SetParent(safeRoot.transform, false);
            safeAstro.transform.localPosition = new Vector3(0f, 0.55f, -0.02f);
            BoxCollider astroTrig = safeAstro.AddComponent<BoxCollider>();
            astroTrig.size = new Vector3(0.30f, 0.30f, 0.24f);
            astroTrig.isTrigger = true;
            AttachDesc(safeAstro, "artifact:ojo-del-umbral",
                "El Ojo del Umbral (Astrolabio Dimensional de Obsidiana)",
                "The Eye of the Threshold (Obsidian Dimensional Astrolabe)",
                "Reliquia octagonal de latón grabado y obsidiana con un núcleo de fisura astral. Encaja en la Cuenca Octagonal de la única Puerta Dimensional del laberinto.",
                "Octagonal relic of engraved brass and obsidian with an astral rift core. Fits into the Octagonal Socket of the maze's single Dimensional Portal.",
                new Color(0.32f, 0.94f, 1.0f));
            CreateAstrolabeModel("Astrolabe_VisualModel", safeAstro.transform, Vector3.zero, Quaternion.Euler(-14f, 0f, 0f), 1.35f,
                antiqueBrassMat, obsidianMat, astrolabeCoreMat, portalRuneVioletMat, out Transform safeSpin);

            vaultCtrl.astrolabeInSafeObject = safeAstro;
            vaultCtrl.astrolabeSafeVisualTransform = safeSpin;
            vaultCtrl.safeAstrolabeAnchor = safeAstro.transform.position;

            // Hinged Safe Door Pivot (swings outward +Z by -95°)
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

            // 3D Electronic 6-Digit Keypad on Safe Door
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

            // Protective white salt line across the Backrooms carpet in front of the Dimensional Portal
            CreateBox("Vault_Floor_SaltLine_Portal", portalRoot.transform, new Vector3(0.42f, 0.008f, 0f), new Vector3(0.055f, 0.016f, 1.48f), saltWhiteMat, keepCollider: false);

            // Monumental Obsidian & Brass Portal Frame (1.28m wide x 2.52m tall)
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
                "El único portal dimensional dentro de este nivel liminal. Su pilar derecho posee una Cuenca Octagonal que requiere 'El Ojo del Umbral' para liberar las cadenas y abrir paso al Nivel 2.",
                "The sole dimensional gateway in this liminal level. Its right pillar holds an Octagonal Socket requiring 'The Eye of the Threshold' to release the chains and open the path to Level 2.",
                new Color(0.55f, 0.86f, 1.0f));

            // Active Multiverse Vortex Rings
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

            // Heavy Crossed Containment Chains
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
            riftLightGo.transform.localPosition = new Vector3(0.38f, 1.35f, 0f);
            Light rl = riftLightGo.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.58f, 0.24f, 0.92f);
            rl.intensity = 0.45f;
            rl.range = 4.0f;
            rl.shadows = LightShadows.None;
            vaultCtrl.portalRiftLight = rl;

            // Octagonal Keystone Socket on Right Pillar
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
                "Receptáculo octagonal tallado en latón y obsidiana. Inserta aquí 'El Ojo del Umbral' (resguardado en la Caja Fuerte Nivel Ω) para activar el Portal Dimensional.",
                "Octagonal brass-and-obsidian receptacle. Insert 'The Eye of the Threshold' (from the Level-Ω Vault Safe) here to activate the Dimensional Portal.",
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
                "Fotografías de Av. Benito Juárez conectadas con hilos rojos hacia un boceto del Espejo Dimensional. Nota amarilla: 'BÓVEDA NIVEL Ω EN OFICINA SURESTE • CÓDIGO INVERSO: 6-5-4-3-2-1'.",
                "Photos of Av. Benito Juárez linked with red yarn to a sketch of the Dimensional Mirror. Yellow note: 'LEVEL-Ω VAULT IN SE OFFICE • REVERSE CODE: 6-5-4-3-2-1'.",
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
                "Libro infantil de tapa carbonizada que reaparece aunque sea destruido. Atado con correa para impedir que sus páginas emergentes se abran.",
                "Charcoal-black pop-up book that reappears even when burned. Strapped shut so its pop-up pages cannot unfold.",
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
                "Teléfono mural de baquelita montado en una columna amarilla. Su cable está cortado, pero a veces timbra en el silencio de los pasillos.",
                "Black Bakelite wall telephone mounted on a yellow hall pillar. Its cord is severed, yet it still rings in the silence of the corridors.",
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
                "Televisor de los 90 encendido sin estar enchufado en un nicho de los Backrooms. Reproduce en bucle un eclipse circular de estática. Etiqueta: '7 DÍAS'.",
                "Unplugged 1990s CRT TV glowing in a Backrooms niche, looping a circular eclipse of static. Tape label: '7 DAYS'.",
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
            l.range = 4.2f;
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
                "Sentado en una silla solitaria de oficina, lleva smoking negro y corbatín rojo. Su grabadora interna murmura: 'Quiero jugar un juego...'.",
                "Seated on a lone office chair in black tuxedo and red bow tie. Its internal recorder whispers: 'I want to play a game...'.",
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
                "Caja amarilla asegurada con fleje de acero sobre un archivador oxidado. Hay marcas de rasguños hechas desde el interior.",
                "Factory yellow toy box strapped in steel atop a rusted filing cabinet. Deep claw marks tear outward from inside.",
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
                "Códice antiguo con relieves orgánicos y herrajes egipcios de oro. Permanece sellado junto a la Caja Fuerte Nivel Ω.",
                "Ancient codex bound with organic ridges and golden Egyptian clasps, sealed beside the Level-Ω Vault Safe.",
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
                "Ignora el norte magnético y su aguja cian apunta invariablemente hacia el Noreste, directo a la Cámara del Umbral Dimensional.",
                "Ignores magnetic north; its glowing cyan needle points unerringly Northeast toward the Dimensional Threshold Chamber.",
                new Color(0.38f, 0.92f, 1.0f));
            CreateBox("Compass_Needle", comp.transform, new Vector3(0f, 0.56f, 0f), new Vector3(0.18f, 0.18f, 0.80f), glowMat, keepCollider: false);
        }

        // =====================================================================
        // PROCEDURAL LEVEL 0 TEXTURES & HELPERS
        // =====================================================================
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

        private static Material CreateEmissiveMat(string assetPath, Color baseColor, Color emissionColor)
        {
            Material mat = CreateColorMat(assetPath, baseColor, 0.35f, 0f);
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissionColor); }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
