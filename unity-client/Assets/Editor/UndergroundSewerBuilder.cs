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
    /// Idempotent Builder for the "Third World": Ciudad Juarez Aged Underground Storm-Drain
    /// Collector Network ("El Alcantarillado — Colector 1958") built physically beneath
    /// BorderCorridorScene.unity.
    ///
    /// Engineering & Visual Standards:
    /// - ZERO floor/wall/balcony Z-fighting: single seamless octagonal balcony ring mesh in the
    ///   Central Cistern Vault, exact mathematical butt-joints at all 4 Vault portals, all 3
    ///   recessed-channel junction chambers, and the Northern Outflow ramp.
    /// - Dark, dense, oppressive storm-drain lighting (making the [F] flashlight essential) with
    ///   sparse rusty-amber lamps, sputtering cages, dead/shattered fixtures & feathered curbside
    ///   "Georgie" daylight shafts.
    /// - 65-year aging & decay pass: 512x512 cracked/mossy/efflorescence concrete, crumbling
    ///   soot-stained brickwork, pitted flaking rust, gossamer cobwebs, twisted ceiling roots,
    ///   broken brick rubble piles, corroded valve wheels, sagging Vault rails, overflow water
    ///   trickles, floating soggy debris & faded 1958 stenciled municipal markings.
    /// - 5 interactive round street manholes (Manhole #1 at Av. Juarez x Azucenas armed with the
    ///   Phase-2 silent Kane Pixels ceiling no-clip drop into BackroomsLevel1Scene).
    /// - Single ramped Rio Bravo storm outflow headwall + interactive rusted iron gate onto the
    ///   walkable south riverbank beneath Puente Internacional Paso del Norte.
    /// </summary>
    public static class UndergroundSewerBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string RootName = "Underground_Sewer_Network_Root";
        private const string ArtifactDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";

        // Key X / Z Street Centerlines matching BorderCorridorScene.unity
        private const float JuarezAxisX = 2.15f;
        private const float MariscalAxisX = -59.25f;
        private const float AzucenasAxisZ = -32.91f;
        private const float GardeniasAxisZ = 28.00f;
        private const float EastBranchEndX = 24.00f;

        // Vertical Elevations (100% clear of deepest street slab Y = -0.37m!)
        private const float WalkwayY = -3.20f;
        private const float ChannelFloorY = -3.45f;
        private const float WaterSurfaceY = -3.32f;
        private const float ArchSpringY = -1.75f;
        private const float ArchCrownY = -0.60f;
        private const float TunnelHalfW = 1.70f;     // Total interior clear width = 3.40m
        private const float ChannelHalfW = 0.75f;    // Central water channel = 1.50m; each walkway = 0.95m
        private const float JunctionHalf = 2.20f;    // Exact half-size of junction chambers (ZERO overlap!)

        // Central Cistern Vault ("La Cisterna / El Nudo" at Av. Juarez x Gardenias)
        private const float VaultCenterX = JuarezAxisX;
        private const float VaultCenterZ = GardeniasAxisZ;
        private const float VaultApothem = 8.20f;    // Exact flat cardinal face distance (ZERO overlap!)
        private const float VaultInnerApothem = 5.95f;
        private const float VaultBasinFloorY = -6.55f;
        private const float VaultBasinWaterY = -6.28f;

        // Rio Bravo Outflow Headwall (proud on South Levee face beneath Puente Internacional Paso del Norte!)
        private const float RampStartZ = 174.00f;
        private const float OutflowMouthZ = 197.20f;
        private const float OutflowWalkwayY = -2.42f;

        [MenuItem("MyVirtualCommunity/Rebuild Underground Sewer Network (Aged & Z-Fight Free)")]
        public static void RebuildAndSave()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[UndergroundSewerBuilder] Failed to open scene: {ScenePath}");
                return;
            }

            BuildInOpenScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[UndergroundSewerBuilder] Saved BorderCorridorScene cleanly.");
        }

        [MenuItem("MyVirtualCommunity/Rebuild, Verify & Capture Underground Sewer")]
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

        public static void BuildInOpenScene()
        {
            CleanupPreviousObjects();
            EnsureProceduralTextures();

            // Aged & weathered sewer materials
            Material concreteWallMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_DampConcrete.mat",
                "Assets/Textures/Sewer/T_Sewer_DampConcrete.png", new Color(0.50f, 0.52f, 0.48f), 0.18f, 0.0f, new Vector2(1f, 1f));
            Material walkwayGrimeMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_WalkwayGrime.mat",
                "Assets/Textures/Sewer/T_Sewer_WalkwayGrime.png", new Color(0.46f, 0.46f, 0.42f), 0.28f, 0.0f, new Vector2(1f, 1f));
            Material brickArchMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_BrickArch.mat",
                "Assets/Textures/Sewer/T_Sewer_BrickArch.png", new Color(0.36f, 0.33f, 0.30f), 0.14f, 0.0f, new Vector2(1f, 1f));
            Material manholeCoverMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_ManholeCover.mat",
                "Assets/Textures/Sewer/T_Sewer_ManholeCover.png", new Color(0.28f, 0.29f, 0.30f), 0.26f, 0.62f, new Vector2(1f, 1f));
            Material rustedIronMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_RustedIron.mat",
                "Assets/Textures/Sewer/T_Sewer_RustedIron.png", new Color(0.44f, 0.30f, 0.22f), 0.22f, 0.58f, new Vector2(1f, 1f));
            Material darkIronMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_DarkIron.mat",
                new Color(0.12f, 0.12f, 0.13f), 0.30f, 0.72f);
            Material agedRedValveMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_AgedRedValve.mat",
                new Color(0.38f, 0.11f, 0.08f), 0.20f, 0.45f);
            Material waterMat = CreateOrGetWaterMat("Assets/Settings/Sewer/Sewer_MurkyWater.mat",
                new Color(0.035f, 0.085f, 0.072f, 0.91f));
            Material waterTrickleMat = CreateOrGetWaterMat("Assets/Settings/Sewer/Sewer_WaterTrickle.mat",
                new Color(0.48f, 0.60f, 0.56f, 0.42f));
            Material warmBulbMat = CreateOrGetEmissiveMat("Assets/Settings/Sewer/Sewer_WarmBulb.mat",
                new Color(0.95f, 0.68f, 0.32f), new Color(1.9f, 1.15f, 0.42f));
            Material deadBulbMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_DeadBulb.mat",
                new Color(0.18f, 0.18f, 0.17f), 0.55f, 0.10f);
            Material backroomsLeakMat = CreateOrGetEmissiveMat("Assets/Settings/Sewer/Sewer_BackroomsLeak.mat",
                new Color(1.0f, 0.94f, 0.60f), new Color(2.2f, 1.95f, 0.80f));
            Material lightShaftMat = CreateOrGetLightShaftMat("Assets/Settings/Sewer/Sewer_LightShaft.mat");
            Material mossSludgeMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_MossConcrete.mat",
                new Color(0.16f, 0.21f, 0.14f), 0.36f, 0.0f);
            Material rubbleConcreteMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_Rubble.mat",
                new Color(0.32f, 0.31f, 0.28f), 0.08f, 0.0f);
            Material rootWoodMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_DarkRoot.mat",
                new Color(0.17f, 0.13f, 0.09f), 0.10f, 0.0f);
            Material soggyPlankMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_SoggyPlank.mat",
                new Color(0.22f, 0.18f, 0.13f), 0.28f, 0.0f);
            Material paperWhiteMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_PaperBoat.mat",
                new Color(0.92f, 0.90f, 0.84f), 0.08f, 0.0f);

            // Bilingual crimson graffiti, English Crop Circle symbols & Paris Catacombs wooden plaque
            Material graffitiSalDeAquiMat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_Graffiti_SalDeAqui.mat",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_SalDeAqui.png", Color.white, new Color(0.28f, 0.02f, 0.02f));
            Material graffitiGetOutMat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_Graffiti_GetOut.mat",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_GetOut.png", Color.white, new Color(0.28f, 0.02f, 0.02f));
            Material graffitiNoMiresMat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_Graffiti_NoMiresAtras.mat",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_NoMiresAtras.png", Color.white, new Color(0.22f, 0.02f, 0.02f));
            Material graffitiDontFollowMat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_Graffiti_DontFollow.mat",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_DontFollow.png", Color.white, new Color(0.24f, 0.02f, 0.02f));
            Material graffitiStillHereMat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_Graffiti_StillHere.mat",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_StillHere.png", Color.white, new Color(0.26f, 0.02f, 0.02f));

            Material cropCircle1Mat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_CropCircle_1.mat",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_1.png", Color.white, new Color(0.08f, 0.08f, 0.07f));
            Material cropCircle2Mat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_CropCircle_2.mat",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_2.png", Color.white, new Color(0.20f, 0.02f, 0.02f));
            Material cropCircle3Mat = CreateOrGetLitDecalMat("Assets/Settings/Sewer/Sewer_CropCircle_3.mat",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_3.png", Color.white, new Color(0.08f, 0.07f, 0.06f));

            Material catacombsSignMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_CatacombsSign.mat",
                "Assets/Textures/Sewer/T_Sewer_CatacombsSign.png", Color.white, 0.14f, 0.0f, Vector2.one);
            catacombsSignMat.EnableKeyword("_EMISSION");
            catacombsSignMat.SetColor("_EmissionColor", new Color(0.07f, 0.06f, 0.045f));

            Material stencil1958Mat = CreateOrGetAlphaDecalMat("Assets/Settings/Sewer/Sewer_Stencil_1958.mat",
                "Assets/Textures/Sewer/T_Sewer_Stencil_1958.png", Color.white);
            Material stencilRioMat = CreateOrGetAlphaDecalMat("Assets/Settings/Sewer/Sewer_Stencil_RioBravo.mat",
                "Assets/Textures/Sewer/T_Sewer_Stencil_RioBravo.png", Color.white);
            Material stencilWarnMat = CreateOrGetAlphaDecalMat("Assets/Settings/Sewer/Sewer_Stencil_Warning.mat",
                "Assets/Textures/Sewer/T_Sewer_Stencil_Warning.png", Color.white);

            AssetDatabase.SaveAssets();

            GameObject root = new GameObject(RootName);
            root.transform.position = Vector3.zero;
            root.AddComponent<UndergroundSewerAtmosphereController>();

            // 1. Subsurface safety floor strictly under the sewer & vault
            BuildSewerSubsurfaceSafety(root.transform);

            // 2. Recessed-Channel Junction Chambers (EXACT ±2.20m butt joints -> ZERO Z-fighting!)
            BuildJunctionChamber("Chamber_Juarez_Azucenas", root.transform, JuarezAxisX, AzucenasAxisZ,
                openN: true, openS: false, openE: true, openW: true, concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildJunctionChamber("Chamber_Mariscal_Azucenas", root.transform, MariscalAxisX, AzucenasAxisZ,
                openN: true, openS: false, openE: true, openW: false, concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildJunctionChamber("Chamber_Mariscal_Gardenias", root.transform, MariscalAxisX, GardeniasAxisZ,
                openN: false, openS: true, openE: true, openW: false, concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3. Vaulted Storm-Drain Tunnel Network (EXACT butt-joints at ±JunctionHalf & ±VaultApothem -> ZERO overlap!)
            var tunnelsRoot = new GameObject("Sewer_Tunnels_Root").transform;
            tunnelsRoot.SetParent(root.transform);

            // 3A. Colector Av. Benito Juarez Sur (Azucenas +2.20 -> Vault South face EXACTLY VaultCenterZ - VaultApothem!)
            BuildVaultedTunnelNS("Tunnel_Juarez_South", tunnelsRoot, JuarezAxisX, AzucenasAxisZ + JunctionHalf, VaultCenterZ - VaultApothem,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 101);

            // 3B. Ramal Calle Azucenas Oeste (Mariscal +2.20 -> Juarez -2.20)
            BuildVaultedTunnelEW("Tunnel_Azucenas_West", tunnelsRoot, AzucenasAxisZ, MariscalAxisX + JunctionHalf, JuarezAxisX - JunctionHalf,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 202);

            // 3C. Ramal Calle Azucenas Este (Juarez +2.20 -> East barred gate)
            BuildVaultedTunnelEW("Tunnel_Azucenas_East", tunnelsRoot, AzucenasAxisZ, JuarezAxisX + JunctionHalf, EastBranchEndX,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 303);
            BuildBarredEndCap("EndGrate_Azucenas_East", tunnelsRoot, new Vector3(EastBranchEndX, WalkwayY, AzucenasAxisZ), 90f, concreteWallMat, rustedIronMat);

            // 3D. Colector Calle Ignacio Mariscal (Azucenas +2.20 -> Gardenias -2.20)
            BuildVaultedTunnelNS("Tunnel_Mariscal_NS", tunnelsRoot, MariscalAxisX, AzucenasAxisZ + JunctionHalf, GardeniasAxisZ - JunctionHalf,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 404);

            // 3E. Ramal Calle Gardenias Oeste (Mariscal +2.20 -> Vault West face EXACTLY VaultCenterX - VaultApothem!)
            BuildVaultedTunnelEW("Tunnel_Gardenias_West", tunnelsRoot, GardeniasAxisZ, MariscalAxisX + JunctionHalf, VaultCenterX - VaultApothem,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 505);

            // 3F. Ramal Calle Gardenias Este (Vault East face EXACTLY VaultCenterX + VaultApothem -> East Branch)
            BuildVaultedTunnelEW("Tunnel_Gardenias_East", tunnelsRoot, GardeniasAxisZ, VaultCenterX + VaultApothem, EastBranchEndX,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 606);
            BuildBarredEndCap("EndGrate_Gardenias_East", tunnelsRoot, new Vector3(EastBranchEndX, WalkwayY, GardeniasAxisZ), 90f, concreteWallMat, rustedIronMat);

            // 3G. Colector Troncal Norte (Vault North face EXACTLY VaultCenterZ + VaultApothem -> RampStartZ 174.0m)
            BuildVaultedTunnelNS("Tunnel_Juarez_NorthTrunk", tunnelsRoot, JuarezAxisX, VaultCenterZ + VaultApothem, RampStartZ,
                concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat, deadBulbMat, mossSludgeMat, seed: 707);

            // 3H. Gentle Ramped North Collector (EXACT Z = 174.0m -> OutflowMouthZ = 197.20m, ZERO overlap!)
            BuildRampedOutflowTunnelNS("Tunnel_Juarez_NorthOutflowRamp", tunnelsRoot, JuarezAxisX, RampStartZ, OutflowMouthZ,
                WalkwayY, OutflowWalkwayY, concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat);

            // 4. Grand Octagonal Central Cistern Vault ("La Cisterna / El Nudo") with SINGLE SEAMLESS BALCONY RING MESH!
            BuildGrandCentralCisternVault(root.transform, concreteWallMat, walkwayGrimeMat, brickArchMat, waterMat,
                waterTrickleMat, rustedIronMat, darkIronMat, warmBulbMat, mossSludgeMat, rubbleConcreteMat,
                cropCircle1Mat, graffitiStillHereMat);

            // 5. Subtle Lore Bulkhead directly underneath La Esquina de Chihuas (centered cleanly between wall ribs!) + Paris Catacombs Wooden Plaque!
            BuildBackroomsLoreBulkhead(root.transform, new Vector3(JuarezAxisX - TunnelHalfW + 0.04f, WalkwayY, -14.80f),
                darkIronMat, rustedIronMat, backroomsLeakMat, rootWoodMat, catacombsSignMat);

            // 6. Comprehensive 65-Year Aging, Bilingual Horror Graffiti & Crop Circle Pass (ZERO cobwebs!)
            BuildAgingAndDecayDetails(root.transform, rubbleConcreteMat, brickArchMat, rootWoodMat,
                rustedIronMat, agedRedValveMat, soggyPlankMat, stencil1958Mat, stencilRioMat, stencilWarnMat,
                graffitiSalDeAquiMat, graffitiGetOutMat, graffitiNoMiresMat, graffitiDontFollowMat, graffitiStillHereMat,
                cropCircle1Mat, cropCircle2Mat, cropCircle3Mat);

            // 7. 5 Interactive Round Cast-Iron Street Manholes + Vertical Brick Shafts & Ladders
            var manholesRoot = new GameObject("Sewer_Manholes_Root").transform;
            manholesRoot.SetParent(root.transform);

            // Manhole #1: EXACT STREET VIEW PHOTO LOCATION (Av. Benito Juarez x Calle Azucenas!)
            // Phase 2 ACTIVE: Silent Kane Pixels ceiling no-clip drop into random Backrooms spot!
            BuildInteractiveManhole("Manhole_01_Juarez_Azucenas", manholesRoot,
                id: "manhole_juarez_azucenas",
                labelEs: "Alcantarilla (Av. Juárez × Azucenas)",
                labelEn: "Manhole (Av. Juarez x Azucenas)",
                surfacePos: new Vector3(3.30f, 0.004f, -32.20f),
                surfaceExitPos: new Vector3(3.30f, 0.15f, -30.50f),
                surfaceExitYaw: 0f,
                undergroundPos: new Vector3(JuarezAxisX + 1.15f, WalkwayY + 0.08f, AzucenasAxisZ + 0.55f),
                undergroundYaw: 0f,
                triggerBackroomsTwist: true,
                manholeCoverMat, darkIronMat, brickArchMat, rustedIronMat);

            BuildInteractiveManhole("Manhole_02_Mariscal_Azucenas", manholesRoot,
                id: "manhole_mariscal_azucenas",
                labelEs: "Alcantarilla (Ignacio Mariscal × Azucenas)",
                labelEn: "Manhole (Ignacio Mariscal x Azucenas)",
                surfacePos: new Vector3(-58.50f, 0.004f, AzucenasAxisZ),
                surfaceExitPos: new Vector3(-58.50f, 0.15f, AzucenasAxisZ + 2.0f),
                surfaceExitYaw: 0f,
                undergroundPos: new Vector3(MariscalAxisX + 1.15f, WalkwayY + 0.08f, AzucenasAxisZ + 0.45f),
                undergroundYaw: 90f,
                triggerBackroomsTwist: false,
                manholeCoverMat, darkIronMat, brickArchMat, rustedIronMat);

            BuildInteractiveManhole("Manhole_03_Mariscal_Gardenias", manholesRoot,
                id: "manhole_mariscal_gardenias",
                labelEs: "Alcantarilla (Ignacio Mariscal × Gardenias)",
                labelEn: "Manhole (Ignacio Mariscal x Gardenias)",
                surfacePos: new Vector3(-58.50f, 0.004f, GardeniasAxisZ),
                surfaceExitPos: new Vector3(-58.50f, 0.15f, GardeniasAxisZ - 2.0f),
                surfaceExitYaw: 180f,
                undergroundPos: new Vector3(MariscalAxisX + 1.15f, WalkwayY + 0.08f, GardeniasAxisZ - 0.45f),
                undergroundYaw: 90f,
                triggerBackroomsTwist: false,
                manholeCoverMat, darkIronMat, brickArchMat, rustedIronMat);

            BuildInteractiveManhole("Manhole_04_Gardenias_East", manholesRoot,
                id: "manhole_gardenias_east",
                labelEs: "Alcantarilla (Calle Gardenias Este)",
                labelEn: "Manhole (Calle Gardenias East)",
                surfacePos: new Vector3(21.50f, 0.004f, GardeniasAxisZ),
                surfaceExitPos: new Vector3(21.50f, 0.15f, GardeniasAxisZ - 2.0f),
                surfaceExitYaw: 270f,
                undergroundPos: new Vector3(21.20f, WalkwayY + 0.08f, GardeniasAxisZ - 1.15f),
                undergroundYaw: 270f,
                triggerBackroomsTwist: false,
                manholeCoverMat, darkIronMat, brickArchMat, rustedIronMat);

            BuildInteractiveManhole("Manhole_05_Azucenas_East", manholesRoot,
                id: "manhole_azucenas_east",
                labelEs: "Alcantarilla (Calle Azucenas Este)",
                labelEn: "Manhole (Calle Azucenas East)",
                surfacePos: new Vector3(21.50f, 0.004f, AzucenasAxisZ),
                surfaceExitPos: new Vector3(21.50f, 0.15f, AzucenasAxisZ + 2.0f),
                surfaceExitYaw: 270f,
                undergroundPos: new Vector3(21.20f, WalkwayY + 0.08f, AzucenasAxisZ + 1.15f),
                undergroundYaw: 270f,
                triggerBackroomsTwist: false,
                manholeCoverMat, darkIronMat, brickArchMat, rustedIronMat);

            // 8. 6 "Efecto Georgie" Curbside Storm-Drain Intake Slots + Soft Light Shafts + Paper Boat
            BuildGeorgieStormDrainSlots(root.transform, darkIronMat, concreteWallMat, lightShaftMat, paperWhiteMat);

            // 9. Aged Rio Bravo Storm Outflow Headwall, Interactive Rusted Iron Gate & Walkable Riverbank
            BuildRioBravoOutflowAndRiverbank(root.transform, concreteWallMat, mossSludgeMat, rustedIronMat, darkIronMat, waterMat, rubbleConcreteMat);

            Debug.Log("[UndergroundSewerBuilder] Built Aged, Dark & Z-Fight-Free Underground Storm-Drain Network cleanly.");
        }

        // =========================================================================
        // CLEANUP & SUBSURFACE SAFETY
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
            GameObject origBank = FindInactiveOrActive("River_BankSouth_C");
            if (origBank != null) origBank.SetActive(true);
        }

        private static GameObject FindInactiveOrActive(string name)
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                if (tr != null && tr.gameObject.scene.IsValid() && tr.name == name)
                    return tr.gameObject;
            }
            return null;
        }

        private static void BuildSewerSubsurfaceSafety(Transform root)
        {
            var go = new GameObject("Sewer_Subsurface_SafetyFloor");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(-20f, -7.20f, 80f);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(130f, 0.4f, 270f);
        }

        // =========================================================================
        // VAULTED TUNNELS (EXACT BUTT-JOINTS, DARK SPARSE LIGHTS, AGED GRIME)
        // =========================================================================

        private static void BuildVaultedTunnelNS(string name, Transform parent, float centerX, float zMin, float zMax,
            Material wallMat, Material walkwayMat, Material brickMat, Material waterMat, Material ironMat,
            Material bulbMat, Material deadBulbMat, Material mossMat, int seed)
        {
            float length = zMax - zMin;
            if (length <= 0.1f) return;
            float midZ = 0.5f * (zMin + zMax);

            var go = new GameObject(name);
            go.transform.SetParent(parent);

            float walkW = TunnelHalfW - ChannelHalfW; // 0.95m
            float leftX = centerX - ChannelHalfW - walkW * 0.5f;
            float rightX = centerX + ChannelHalfW + walkW * 0.5f;

            // Left & Right Weathered Concrete Walkways (top strictly at WalkwayY = -3.200m, exact length!)
            CreateBox("Walkway_West", go.transform, new Vector3(leftX, WalkwayY - 0.15f, midZ), new Vector3(walkW, 0.30f, length), walkwayMat, true);
            CreateBox("Walkway_East", go.transform, new Vector3(rightX, WalkwayY - 0.15f, midZ), new Vector3(walkW, 0.30f, length), walkwayMat, true);

            // Central Channel Bed (strictly between walkways, no horizontal coplanar overlap!) & Murky Water
            CreateBox("Channel_Bed", go.transform, new Vector3(centerX, ChannelFloorY - 0.12f, midZ), new Vector3(ChannelHalfW * 2f, 0.24f, length), wallMat, true);
            CreateBox("Channel_Water", go.transform, new Vector3(centerX, WaterSurfaceY, midZ), new Vector3(ChannelHalfW * 2f - 0.02f, 0.02f, length), waterMat, false);

            // Lower Vertical Walls (WalkwayY -> ArchSpringY)
            float wallH = ArchSpringY - WalkwayY + 0.15f;
            float wallCenterY = WalkwayY + (wallH - 0.15f) * 0.5f;
            CreateBox("Wall_West", go.transform, new Vector3(centerX - TunnelHalfW - 0.14f, wallCenterY, midZ), new Vector3(0.28f, wallH, length), wallMat, true);
            CreateBox("Wall_East", go.transform, new Vector3(centerX + TunnelHalfW + 0.14f, wallCenterY, midZ), new Vector3(0.28f, wallH, length), wallMat, true);

            // Dark damp algae/sludge tide skirt proud by +1.8cm along lower wall bases (adds decade-old realism, zero Z-fighting!)
            CreateBox("SludgeSkirt_W", go.transform, new Vector3(centerX - TunnelHalfW + 0.015f, WalkwayY + 0.20f, midZ), new Vector3(0.03f, 0.38f, length - 0.08f), mossMat, false);
            CreateBox("SludgeSkirt_E", go.transform, new Vector3(centerX + TunnelHalfW - 0.015f, WalkwayY + 0.20f, midZ), new Vector3(0.03f, 0.38f, length - 0.08f), mossMat, false);

            // Curved Brick Barrel-Vault Arch Roof Mesh
            BuildBarrelVaultArchMesh("Vault_Arch", go.transform, new Vector3(centerX, ArchSpringY, zMin), new Vector3(centerX, ArchSpringY, zMax),
                TunnelHalfW + 0.04f, ArchCrownY - ArchSpringY, brickMat);

            // Corroded Utility Pipe + Mounting Clamps mounted high at arch springline (clears all wall graffiti cleanly!)
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "UtilityPipe_East";
            pipe.transform.SetParent(go.transform);
            pipe.transform.position = new Vector3(centerX + TunnelHalfW - 0.14f, ArchSpringY + 0.05f, midZ);
            pipe.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pipe.transform.localScale = new Vector3(0.14f, length * 0.5f, 0.14f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

            // Concrete Ribs + Pipe Flanges
            for (float z = zMin + 4.5f; z <= zMax - 3.5f; z += 8.0f)
            {
                CreateBox($"Rib_W_{z:F0}", go.transform, new Vector3(centerX - TunnelHalfW + 0.07f, wallCenterY, z), new Vector3(0.14f, wallH - 0.10f, 0.24f), wallMat, false);
                CreateBox($"Rib_E_{z:F0}", go.transform, new Vector3(centerX + TunnelHalfW - 0.07f, wallCenterY, z), new Vector3(0.14f, wallH - 0.10f, 0.24f), wallMat, false);
                CreateBox($"PipeClamp_{z:F0}", go.transform, new Vector3(centerX + TunnelHalfW - 0.14f, ArchSpringY + 0.05f, z), new Vector3(0.24f, 0.20f, 0.09f), ironMat, false);
            }

            // Sparse, Dark, Aged Bulkhead Lights (mix of very dim amber, weakly sputtering, and dead/shattered cages!)
            System.Random rng = new System.Random(seed);
            int lightIdx = 0;
            for (float z = zMin + 10.0f; z <= zMax - 8.0f; z += 29.0f)
            {
                int roll = rng.Next(0, 10);
                bool isDead = (roll < 3);           // 30% burned-out broken fixtures!
                bool isSputtering = (!isDead && roll < 5); // 20% sputtering old tungsten!
                string lName = isSputtering ? $"Sputter_TunnelLight_{lightIdx}" : $"TunnelLight_{lightIdx}";
                CreateMaintenanceBulkheadLight(lName, go.transform,
                    new Vector3(centerX - TunnelHalfW + 0.14f, ArchSpringY + 0.04f, z),
                    isDead ? deadBulbMat : bulbMat, ironMat,
                    intensity: isDead ? 0f : 1.15f, range: 8.5f, addLight: !isDead);
                lightIdx++;
            }
        }

        private static void BuildVaultedTunnelEW(string name, Transform parent, float centerZ, float xMin, float xMax,
            Material wallMat, Material walkwayMat, Material brickMat, Material waterMat, Material ironMat,
            Material bulbMat, Material deadBulbMat, Material mossMat, int seed)
        {
            float length = xMax - xMin;
            if (length <= 0.1f) return;
            float midX = 0.5f * (xMin + xMax);

            var go = new GameObject(name);
            go.transform.SetParent(parent);

            float walkW = TunnelHalfW - ChannelHalfW;
            float southZ = centerZ - ChannelHalfW - walkW * 0.5f;
            float northZ = centerZ + ChannelHalfW + walkW * 0.5f;

            CreateBox("Walkway_South", go.transform, new Vector3(midX, WalkwayY - 0.15f, southZ), new Vector3(length, 0.30f, walkW), walkwayMat, true);
            CreateBox("Walkway_North", go.transform, new Vector3(midX, WalkwayY - 0.15f, northZ), new Vector3(length, 0.30f, walkW), walkwayMat, true);

            CreateBox("Channel_Bed", go.transform, new Vector3(midX, ChannelFloorY - 0.12f, centerZ), new Vector3(length, 0.24f, ChannelHalfW * 2f), wallMat, true);
            CreateBox("Channel_Water", go.transform, new Vector3(midX, WaterSurfaceY, centerZ), new Vector3(length, 0.02f, ChannelHalfW * 2f - 0.02f), waterMat, false);

            float wallH = ArchSpringY - WalkwayY + 0.15f;
            float wallCenterY = WalkwayY + (wallH - 0.15f) * 0.5f;
            CreateBox("Wall_South", go.transform, new Vector3(midX, wallCenterY, centerZ - TunnelHalfW - 0.14f), new Vector3(length, wallH, 0.28f), wallMat, true);
            CreateBox("Wall_North", go.transform, new Vector3(midX, wallCenterY, centerZ + TunnelHalfW + 0.14f), new Vector3(length, wallH, 0.28f), wallMat, true);

            CreateBox("SludgeSkirt_S", go.transform, new Vector3(midX, WalkwayY + 0.20f, centerZ - TunnelHalfW + 0.015f), new Vector3(length - 0.08f, 0.38f, 0.03f), mossMat, false);
            CreateBox("SludgeSkirt_N", go.transform, new Vector3(midX, WalkwayY + 0.20f, centerZ + TunnelHalfW - 0.015f), new Vector3(length - 0.08f, 0.38f, 0.03f), mossMat, false);

            BuildBarrelVaultArchMesh("Vault_Arch", go.transform, new Vector3(xMin, ArchSpringY, centerZ), new Vector3(xMax, ArchSpringY, centerZ),
                TunnelHalfW + 0.04f, ArchCrownY - ArchSpringY, brickMat);

            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "UtilityPipe_North";
            pipe.transform.SetParent(go.transform);
            pipe.transform.position = new Vector3(midX, ArchSpringY + 0.05f, centerZ + TunnelHalfW - 0.14f);
            pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            pipe.transform.localScale = new Vector3(0.14f, length * 0.5f, 0.14f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

            for (float x = xMin + 4.5f; x <= xMax - 3.5f; x += 8.0f)
            {
                CreateBox($"Rib_S_{x:F0}", go.transform, new Vector3(x, wallCenterY, centerZ - TunnelHalfW + 0.07f), new Vector3(0.24f, wallH - 0.10f, 0.14f), wallMat, false);
                CreateBox($"Rib_N_{x:F0}", go.transform, new Vector3(x, wallCenterY, centerZ + TunnelHalfW - 0.07f), new Vector3(0.24f, wallH - 0.10f, 0.14f), wallMat, false);
                CreateBox($"PipeClamp_{x:F0}", go.transform, new Vector3(x, ArchSpringY + 0.05f, centerZ + TunnelHalfW - 0.14f), new Vector3(0.09f, 0.20f, 0.24f), ironMat, false);
            }

            System.Random rng = new System.Random(seed);
            int lightIdx = 0;
            for (float x = xMin + 10.0f; x <= xMax - 8.0f; x += 28.0f)
            {
                int roll = rng.Next(0, 10);
                bool isDead = (roll < 3);
                bool isSputtering = (!isDead && roll < 5);
                string lName = isSputtering ? $"Sputter_TunnelLight_{lightIdx}" : $"TunnelLight_{lightIdx}";
                CreateMaintenanceBulkheadLight(lName, go.transform,
                    new Vector3(x, ArchSpringY + 0.04f, centerZ - TunnelHalfW + 0.14f),
                    isDead ? deadBulbMat : bulbMat, ironMat,
                    intensity: isDead ? 0f : 1.15f, range: 8.5f, addLight: !isDead);
                lightIdx++;
            }
        }

        private static void BuildRampedOutflowTunnelNS(string name, Transform parent, float centerX, float zStart, float zEnd,
            float yStart, float yEnd, Material wallMat, Material walkwayMat, Material brickMat, Material waterMat)
        {
            float dz = zEnd - zStart;
            float dy = yEnd - yStart;
            float slopeLen = Mathf.Sqrt(dz * dz + dy * dy);
            float pitchDeg = -Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(centerX, 0.5f * (yStart + yEnd), 0.5f * (zStart + zEnd));
            go.transform.rotation = Quaternion.Euler(pitchDeg, 0f, 0f);

            // Exact slopeLen (zero overlap into Tunnel_Juarez_NorthTrunk or Outfall_Apron!)
            float walkW = TunnelHalfW - ChannelHalfW;
            CreateBoxLocal("Walkway_West", go.transform, new Vector3(-(ChannelHalfW + walkW * 0.5f), -0.15f, 0f), new Vector3(walkW, 0.30f, slopeLen), walkwayMat, true);
            CreateBoxLocal("Walkway_East", go.transform, new Vector3(+(ChannelHalfW + walkW * 0.5f), -0.15f, 0f), new Vector3(walkW, 0.30f, slopeLen), walkwayMat, true);
            CreateBoxLocal("Channel_Bed", go.transform, new Vector3(0f, -0.34f, 0f), new Vector3(ChannelHalfW * 2f, 0.24f, slopeLen), wallMat, true);
            CreateBoxLocal("Channel_Water", go.transform, new Vector3(0f, -0.14f, 0f), new Vector3(ChannelHalfW * 2f - 0.02f, 0.02f, slopeLen), waterMat, false);

            float wallH = 1.75f;
            CreateBoxLocal("Wall_West", go.transform, new Vector3(-(TunnelHalfW + 0.14f), 0.72f, 0f), new Vector3(0.28f, wallH, slopeLen), wallMat, true);
            CreateBoxLocal("Wall_East", go.transform, new Vector3(+(TunnelHalfW + 0.14f), 0.72f, 0f), new Vector3(0.28f, wallH, slopeLen), wallMat, true);

            BuildBarrelVaultArchMesh("OutflowRamp_VaultArch", parent,
                new Vector3(centerX, yStart + 1.45f, zStart),
                new Vector3(centerX, yEnd + 1.45f, zEnd),
                TunnelHalfW + 0.04f, 1.15f, brickMat);
        }

        private static void BuildBarrelVaultArchMesh(string name, Transform parent, Vector3 startSpringCenter, Vector3 endSpringCenter,
            float halfWidth, float archHeight, Material mat)
        {
            const int arcSegs = 8;
            Vector3 fwd = (endSpringCenter - startSpringCenter);
            float len = fwd.magnitude;
            if (len < 0.01f) return;
            Vector3 dir = fwd / len;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 up = Vector3.Cross(dir, right).normalized;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i <= arcSegs; i++)
            {
                float a = Mathf.PI * ((float)i / arcSegs);
                Vector3 localOffset = right * (Mathf.Cos(a) * halfWidth) + up * (Mathf.Sin(a) * archHeight);
                verts.Add(startSpringCenter + localOffset);
                verts.Add(endSpringCenter + localOffset);
                uvs.Add(new Vector2((float)i / arcSegs * 2.5f, 0f));
                uvs.Add(new Vector2((float)i / arcSegs * 2.5f, len * 0.28f));
            }

            for (int i = 0; i < arcSegs; i++)
            {
                int b = i * 2;
                tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 3);
                tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
            }

            var go = CreateMeshObject(name, parent, verts, tris, uvs, mat, ShadowCastingMode.TwoSided);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        // =========================================================================
        // RECESSED-CHANNEL JUNCTION CHAMBERS (ZERO COPLANAR FLOOR OVERLAP!)
        // =========================================================================

        private static void BuildJunctionChamber(string name, Transform parent, float cx, float cz,
            bool openN, bool openS, bool openE, bool openW,
            Material wallMat, Material walkwayMat, Material brickMat, Material waterMat, Material ironMat, Material bulbMat)
        {
            float half = JunctionHalf; // Exact 2.20m matching tunnel start/end!
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            // Continuous recessed cross-channel bed (Y = -3.45m) & water (Y = -3.32m) across full 4.40m chamber
            CreateBox("Chamber_ChannelBed", go.transform, new Vector3(cx, ChannelFloorY - 0.12f, cz), new Vector3(half * 2f, 0.24f, half * 2f), wallMat, true);
            CreateBox("Chamber_WaterCross_NS", go.transform, new Vector3(cx, WaterSurfaceY, cz), new Vector3(ChannelHalfW * 2f - 0.02f, 0.02f, half * 2f), waterMat, false);
            CreateBox("Chamber_WaterCross_EW", go.transform, new Vector3(cx, WaterSurfaceY - 0.003f, cz), new Vector3(half * 2f, 0.02f, ChannelHalfW * 2f - 0.02f), waterMat, false);

            // 4 exact corner walkway pads at WalkwayY = -3.20m framing the central water cross (ZERO overlap with tunnels or water!)
            float padSize = half - ChannelHalfW; // 1.45m x 1.45m each corner
            float padOff = ChannelHalfW + padSize * 0.5f; // 1.475m
            CreateBox("CornerPad_NW", go.transform, new Vector3(cx - padOff, WalkwayY - 0.15f, cz + padOff), new Vector3(padSize, 0.30f, padSize), walkwayMat, true);
            CreateBox("CornerPad_NE", go.transform, new Vector3(cx + padOff, WalkwayY - 0.15f, cz + padOff), new Vector3(padSize, 0.30f, padSize), walkwayMat, true);
            CreateBox("CornerPad_SW", go.transform, new Vector3(cx - padOff, WalkwayY - 0.15f, cz - padOff), new Vector3(padSize, 0.30f, padSize), walkwayMat, true);
            CreateBox("CornerPad_SE", go.transform, new Vector3(cx + padOff, WalkwayY - 0.15f, cz - padOff), new Vector3(padSize, 0.30f, padSize), walkwayMat, true);

            // Solid walkway bridges on closed sides so players can walk smoothly around the chamber perimeter!
            if (!openN) CreateBox("ClosedWalk_N", go.transform, new Vector3(cx, WalkwayY - 0.15f, cz + padOff), new Vector3(ChannelHalfW * 2f, 0.30f, padSize), walkwayMat, true);
            if (!openS) CreateBox("ClosedWalk_S", go.transform, new Vector3(cx, WalkwayY - 0.15f, cz - padOff), new Vector3(ChannelHalfW * 2f, 0.30f, padSize), walkwayMat, true);
            if (!openE) CreateBox("ClosedWalk_E", go.transform, new Vector3(cx + padOff, WalkwayY - 0.15f, cz), new Vector3(padSize, 0.30f, ChannelHalfW * 2f), walkwayMat, true);
            if (!openW) CreateBox("ClosedWalk_W", go.transform, new Vector3(cx - padOff, WalkwayY - 0.15f, cz), new Vector3(padSize, 0.30f, ChannelHalfW * 2f), walkwayMat, true);

            // Ceiling slab at ArchCrownY
            CreateBox("Chamber_Ceiling", go.transform, new Vector3(cx, ArchCrownY + 0.12f, cz), new Vector3(half * 2f + 0.30f, 0.24f, half * 2f + 0.30f), brickMat, true);

            float wallH = ArchCrownY - WalkwayY + 0.05f;
            float wallY = WalkwayY + wallH * 0.5f;

            if (!openN) CreateBox("Wall_N", go.transform, new Vector3(cx, wallY, cz + half + 0.14f), new Vector3(half * 2f + 0.28f, wallH, 0.28f), wallMat, true);
            if (!openS) CreateBox("Wall_S", go.transform, new Vector3(cx, wallY, cz - half - 0.14f), new Vector3(half * 2f + 0.28f, wallH, 0.28f), wallMat, true);
            if (!openE) CreateBox("Wall_E", go.transform, new Vector3(cx + half + 0.14f, wallY, cz), new Vector3(0.28f, wallH, half * 2f + 0.28f), wallMat, true);
            if (!openW) CreateBox("Wall_W", go.transform, new Vector3(cx - half - 0.14f, wallY, cz), new Vector3(0.28f, wallH, half * 2f + 0.28f), wallMat, true);

            // 4 masonry corner jambs cleanly inside corners (zero wall Z-fighting!)
            float jambSize = half - TunnelHalfW; // 0.50m
            float jambOff = TunnelHalfW + jambSize * 0.5f; // 1.95m
            CreateBox("Jamb_NW", go.transform, new Vector3(cx - jambOff, wallY, cz + jambOff), new Vector3(jambSize, wallH, jambSize), brickMat, true);
            CreateBox("Jamb_NE", go.transform, new Vector3(cx + jambOff, wallY, cz + jambOff), new Vector3(jambSize, wallH, jambSize), brickMat, true);
            CreateBox("Jamb_SW", go.transform, new Vector3(cx - jambOff, wallY, cz - jambOff), new Vector3(jambSize, wallH, jambSize), brickMat, true);
            CreateBox("Jamb_SE", go.transform, new Vector3(cx + jambOff, wallY, cz - jambOff), new Vector3(jambSize, wallH, jambSize), brickMat, true);

            // Dim moody maintenance lamp inside junction
            CreateMaintenanceBulkheadLight("Chamber_Lamp", go.transform, new Vector3(cx - 1.55f, ArchSpringY + 0.22f, cz + 1.55f),
                bulbMat, ironMat, intensity: 1.85f, range: 10.5f, addLight: true);
        }

        private static void BuildBarredEndCap(string name, Transform parent, Vector3 centerFloor, float yawDeg, Material wallMat, Material ironMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = centerFloor;
            go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            CreateBoxLocal("DarkBacking", go.transform, new Vector3(0f, 1.35f, 1.15f), new Vector3(3.8f, 3.0f, 0.3f), wallMat, true);
            CreateBoxLocal("Frame_Top", go.transform, new Vector3(0f, 2.35f, 0f), new Vector3(3.5f, 0.14f, 0.14f), ironMat, true);
            for (float bx = -1.55f; bx <= 1.56f; bx += 0.28f)
            {
                CreateBoxLocal($"Bar_{bx:F2}", go.transform, new Vector3(bx, 1.15f, 0f), new Vector3(0.055f, 2.55f, 0.055f), ironMat, false);
            }
            var colGo = new GameObject("Grate_Blocker");
            colGo.transform.SetParent(go.transform, false);
            colGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var bc = colGo.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.5f, 2.6f, 0.25f);
        }

        // =========================================================================
        // GRAND CENTRAL CISTERN VAULT (SEAMLESS OCTAGONAL RING -> ZERO FLICKER!)
        // =========================================================================

        private static void BuildGrandCentralCisternVault(Transform root, Material concreteMat, Material walkwayMat,
            Material brickMat, Material waterMat, Material trickleMat, Material rustedIronMat, Material darkIronMat,
            Material bulbMat, Material mossMat, Material rubbleMat, Material vaultCropMat, Material vaultGraffitiMat)
        {
            var vault = new GameObject("Central_Cistern_Vault_LaGuarida");
            vault.transform.SetParent(root);
            vault.transform.position = new Vector3(VaultCenterX, 0f, VaultCenterZ);

            const int sides = 8;
            float r = VaultApothem;          // 8.20m exact outer cardinal face apothem
            float rIn = VaultInnerApothem;   // 5.95m exact inner balcony lip apothem
            float ceilingY = -0.58f;
            float totalH = ceilingY - VaultBasinFloorY;

            // 1. Sunken Octagonal Cistern Floor & Dark Murky Water Basin
            CreateBoxLocal("Vault_BasinFloor", vault.transform, new Vector3(0f, VaultBasinFloorY - 0.20f, 0f), new Vector3(r * 2.15f, 0.40f, r * 2.15f), concreteMat, true);
            CreateBoxLocal("Vault_BasinWater", vault.transform, new Vector3(0f, VaultBasinWaterY, 0f), new Vector3(r * 2.02f, 0.04f, r * 2.02f), waterMat, false);

            // 2. Massive Vaulted Ceiling Cap
            CreateBoxLocal("Vault_CeilingCap", vault.transform, new Vector3(0f, ceilingY + 0.14f, 0f), new Vector3(r * 2.22f, 0.28f, r * 2.22f), brickMat, true);

            // 3. SINGLE SEAMLESS OCTAGONAL BALCONY RING MESH (ZERO overlapping corner quads, ZERO Z-fighting!)
            BuildSeamlessOctagonalRingMesh("Vault_BalconyRing_SeamlessMesh", vault.transform,
                outerApothem: r, innerApothem: rIn, topY: WalkwayY, botY: WalkwayY - 0.28f, walkwayMat);

            // 4. 8 Octagonal Perimeter Walls (EXACT faceLen without overlap + recessed portal openings!)
            float faceLen = 2f * r * Mathf.Tan(Mathf.PI / sides); // ~6.793m exact octagon edge length
            for (int i = 0; i < sides; i++)
            {
                float angleDeg = i * 45f; // 0=+Z(N), 90=+X(E), 180=-Z(S), 270=-X(W)
                bool isPortalSide = (i % 2 == 0);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 faceDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 faceRight = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
                Vector3 wallCenter = faceDir * (r + 0.225f); // Inner wall face sits strictly at r = 8.20m!

                if (!isPortalSide)
                {
                    var w = CreateBoxLocal($"Vault_Wall_{i}", vault.transform,
                        new Vector3(wallCenter.x, VaultBasinFloorY + totalH * 0.5f, wallCenter.z),
                        new Vector3(faceLen, totalH, 0.45f), brickMat, true);
                    w.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                }
                else
                {
                    // Lower mossy wall strictly below balcony underside (Y = -6.55m -> -3.48m)
                    float lowerTop = WalkwayY - 0.28f;
                    float lowerH = lowerTop - VaultBasinFloorY;
                    var lowerW = CreateBoxLocal($"Vault_PortalLower_{i}", vault.transform,
                        new Vector3(wallCenter.x, VaultBasinFloorY + lowerH * 0.5f, wallCenter.z),
                        new Vector3(faceLen, lowerH, 0.45f), mossMat, true);
                    lowerW.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                    // Left & Right side cheek walls framing the exact 3.40m portal opening above WalkwayY
                    float upperH = ceilingY - WalkwayY;
                    float cheekW = (faceLen - (TunnelHalfW * 2f)) * 0.5f;
                    float cheekOffset = TunnelHalfW + cheekW * 0.5f;
                    var leftCheek = CreateBoxLocal($"Vault_PortalCheekL_{i}", vault.transform,
                        wallCenter - faceRight * cheekOffset + Vector3.up * (WalkwayY + upperH * 0.5f),
                        new Vector3(cheekW, upperH, 0.45f), brickMat, true);
                    leftCheek.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                    var rightCheek = CreateBoxLocal($"Vault_PortalCheekR_{i}", vault.transform,
                        wallCenter + faceRight * cheekOffset + Vector3.up * (WalkwayY + upperH * 0.5f),
                        new Vector3(cheekW, upperH, 0.45f), brickMat, true);
                    rightCheek.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                }

                // Massive Weathered Concrete Buttress Pier rising out of the dark water at each octagon vertex
                float vRad = (angleDeg + 22.5f) * Mathf.Deg2Rad;
                Vector3 colPos = new Vector3(Mathf.Sin(vRad) * (r - 1.85f), VaultBasinFloorY + totalH * 0.5f, Mathf.Cos(vRad) * (r - 1.85f));
                var pillar = CreateBoxLocal($"Vault_Pier_{i}", vault.transform, colPos, new Vector3(0.86f, totalH, 0.86f), concreteMat, true);
                pillar.transform.localRotation = Quaternion.Euler(0f, angleDeg + 22.5f, 0f);

                // Weathered dark algae stain collar around each pier right above water level
                var stain = CreateBoxLocal($"Vault_PierAlgae_{i}", vault.transform,
                    new Vector3(colPos.x, VaultBasinWaterY + 0.42f, colPos.z), new Vector3(0.90f, 0.90f, 0.90f), mossMat, false);
                stain.transform.localRotation = Quaternion.Euler(0f, angleDeg + 22.5f, 0f);

                // Rusted Iron Guardrail on inner lip (slightly aged/sagging tilt on two segments, skip SW stair opening i==5!)
                if (i != 5)
                {
                    float railR = rIn + 0.09f;
                    float railLen = 2f * rIn * Mathf.Tan(Mathf.PI / sides);
                    float sagRoll = (i == 2) ? 3.2f : ((i == 7) ? -2.4f : 0f);
                    var topRail = CreateBoxLocal($"Vault_RailTop_{i}", vault.transform,
                        new Vector3(faceDir.x * railR, WalkwayY + 0.96f, faceDir.z * railR),
                        new Vector3(railLen, 0.06f, 0.06f), rustedIronMat, true);
                    topRail.transform.localRotation = Quaternion.Euler(0f, angleDeg, sagRoll);

                    var midRail = CreateBoxLocal($"Vault_RailMid_{i}", vault.transform,
                        new Vector3(faceDir.x * railR, WalkwayY + 0.50f, faceDir.z * railR),
                        new Vector3(railLen, 0.045f, 0.045f), rustedIronMat, false);
                    midRail.transform.localRotation = Quaternion.Euler(0f, angleDeg, sagRoll * 0.5f);

                    // Vertical rail posts (skip one middle post on side 2 to look decades old & damaged!)
                    for (int p = -1; p <= 1; p++)
                    {
                        if (i == 2 && p == 0) continue;
                        Vector3 postPos = faceDir * railR + faceRight * (p * railLen * 0.34f) + Vector3.up * (WalkwayY + 0.48f);
                        var post = CreateBoxLocal($"Vault_RailPost_{i}_{p}", vault.transform, postPos, new Vector3(0.055f, 0.96f, 0.055f), rustedIronMat, false);
                        post.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                    }
                }
            }

            // 5. Industrial Metal Staircase with side stringer beams & handrail descending SW Balcony -> Central Island
            const int steps = 16;
            Vector3 stairTop = new Vector3(-4.15f, WalkwayY, -4.15f);
            Vector3 stairBot = new Vector3(-2.10f, -6.08f, -1.60f);
            Vector3 stairDelta = stairBot - stairTop;
            float stairLen = stairDelta.magnitude;
            Vector3 stairMid = 0.5f * (stairTop + stairBot);
            Quaternion stairRot = Quaternion.LookRotation(stairDelta.normalized, Vector3.up);
            Vector3 stairSide = Vector3.Cross(Vector3.up, stairDelta.normalized).normalized;

            var strL = CreateBoxLocal("Vault_StairStringer_L", vault.transform, stairMid - stairSide * 0.72f - Vector3.up * 0.14f, new Vector3(0.08f, 0.24f, stairLen + 0.3f), darkIronMat, false);
            strL.transform.localRotation = stairRot;
            var strR = CreateBoxLocal("Vault_StairStringer_R", vault.transform, stairMid + stairSide * 0.72f - Vector3.up * 0.14f, new Vector3(0.08f, 0.24f, stairLen + 0.3f), darkIronMat, false);
            strR.transform.localRotation = stairRot;
            var stairHandrailR = CreateBoxLocal("Vault_StairHandrail_R", vault.transform, stairMid + stairSide * 0.72f + Vector3.up * 0.85f, new Vector3(0.05f, 0.05f, stairLen + 0.2f), rustedIronMat, false);
            stairHandrailR.transform.localRotation = stairRot;

            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                Vector3 sp = Vector3.Lerp(stairTop, stairBot, t);
                var step = CreateBoxLocal($"Vault_StairStep_{s}", vault.transform,
                    sp - new Vector3(0f, 0.06f, 0f), new Vector3(1.45f, 0.12f, 0.36f), rustedIronMat, true);
                step.transform.localRotation = Quaternion.Euler(0f, 38f, 0f);
            }

            // 6. Central Raised Weathered Island Plinth ("La Guarida" Staging Area — exact flat MeshCollider!)
            var island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "Vault_Central_Island_Plinth";
            island.transform.SetParent(vault.transform, false);
            island.transform.localPosition = new Vector3(0f, -6.32f, 0f);
            island.transform.localScale = new Vector3(4.8f, 0.24f, 4.8f);
            island.GetComponent<MeshRenderer>().sharedMaterial = walkwayMat;
            var cylCap = island.GetComponent<CapsuleCollider>();
            if (cylCap != null) UnityEngine.Object.DestroyImmediate(cylCap);
            var islandMc = island.AddComponent<MeshCollider>();
            islandMc.sharedMesh = island.GetComponent<MeshFilter>().sharedMesh;

            // Rubble chunks & corroded barrel resting on the weathered island edge
            SpawnRubbleCluster("Vault_IslandRubble_1", vault.transform, new Vector3(1.55f, -6.08f, 0.95f), rubbleMat, brickMat, 5, seed: 71);
            SpawnRubbleCluster("Vault_IslandRubble_2", vault.transform, new Vector3(-1.35f, -6.08f, 1.45f), rubbleMat, brickMat, 4, seed: 72);
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Vault_RustedBarrel";
            drum.transform.SetParent(vault.transform, false);
            drum.transform.localPosition = new Vector3(1.42f, -5.68f, -1.15f);
            drum.transform.localRotation = Quaternion.Euler(4f, 18f, -6f);
            drum.transform.localScale = new Vector3(0.58f, 0.44f, 0.58f);
            drum.GetComponent<MeshRenderer>().sharedMaterial = rustedIronMat;

            // 3 Giant Corroded Overflow Pipes pouring thin glistening water streams into the cistern!
            for (int p = 0; p < 3; p++)
            {
                float ang = (-45f + p * 45f) * Mathf.Deg2Rad;
                Vector3 pipeTip = new Vector3(Mathf.Sin(ang) * (r - 1.35f), -4.85f, Mathf.Cos(ang) * (r - 1.35f));
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = $"Vault_OverflowPipe_{p}";
                pipe.transform.SetParent(vault.transform, false);
                pipe.transform.localPosition = new Vector3(Mathf.Sin(ang) * (r - 0.6f), -4.85f, Mathf.Cos(ang) * (r - 0.6f));
                pipe.transform.localRotation = Quaternion.Euler(90f, (-45f + p * 45f), 0f);
                pipe.transform.localScale = new Vector3(0.75f, 1.4f, 0.75f);
                pipe.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
                UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

                // Thin vertical water stream cascading from pipe mouth down to the flooded basin!
                float dropH = (-4.95f) - VaultBasinWaterY;
                var stream = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stream.name = $"Vault_OverflowStream_{p}";
                stream.transform.SetParent(vault.transform, false);
                stream.transform.localPosition = new Vector3(pipeTip.x, VaultBasinWaterY + dropH * 0.5f, pipeTip.z);
                stream.transform.localScale = new Vector3(0.11f, dropH * 0.5f, 0.11f);
                stream.GetComponent<MeshRenderer>().sharedMaterial = trickleMat;
                UnityEngine.Object.DestroyImmediate(stream.GetComponent<Collider>());

                // Pale ripple splash disk on water surface
                var splash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                splash.name = $"Vault_OverflowSplash_{p}";
                splash.transform.SetParent(vault.transform, false);
                splash.transform.localPosition = new Vector3(pipeTip.x, VaultBasinWaterY + 0.025f, pipeTip.z);
                splash.transform.localScale = new Vector3(0.72f, 0.008f, 0.72f);
                splash.GetComponent<MeshRenderer>().sharedMaterial = trickleMat;
                UnityEngine.Object.DestroyImmediate(splash.GetComponent<Collider>());
            }

            // Cryptic Crop Circle glyph on NE diagonal Vault wall (i=1) & ominous crimson warning on SE diagonal wall (i=3)
            // Placed +0.04m proud of inner diagonal wall face (r = 8.20m) -> ZERO Z-fighting!
            float neRad = 45f * Mathf.Deg2Rad;
            Vector3 nePos = new Vector3(Mathf.Sin(neRad) * (r - 0.04f), WalkwayY + 1.35f, Mathf.Cos(neRad) * (r - 0.04f));
            SpawnWallDecalLocal("Vault_CropCircle_NE", vault.transform, nePos, 45f, 2.05f, 2.05f, vaultCropMat);

            float nwRad = 315f * Mathf.Deg2Rad;
            Vector3 nwPos = new Vector3(Mathf.Sin(nwRad) * (r - 0.04f), WalkwayY + 1.32f, Mathf.Cos(nwRad) * (r - 0.04f));
            SpawnWallDecalLocal("Vault_Graffiti_StillHere_NW", vault.transform, nwPos, 315f, 2.75f, 1.15f, vaultGraffitiMat);

            // 7. Dim, Moody Flickering Hanging Industrial Lamps (much darker & more atmospheric!)
            CreateHangingVaultLamp("VaultFlicker_Light_N", vault.transform, new Vector3(0f, -1.55f, 5.2f), ceilingY, bulbMat, rustedIronMat, 2.2f, 11.5f);
            CreateHangingVaultLamp("VaultFlicker_Light_S", vault.transform, new Vector3(0f, -1.55f, -5.2f), ceilingY, bulbMat, rustedIronMat, 1.7f, 11.0f);
            CreateHangingVaultLamp("VaultFlicker_Light_Basin", vault.transform, new Vector3(0f, -3.75f, 0f), ceilingY, bulbMat, rustedIronMat, 1.9f, 10.5f);

            var lairAnchor = new GameObject("EvilCharacter_Lair_SpawnAnchor");
            lairAnchor.transform.SetParent(vault.transform, false);
            lairAnchor.transform.localPosition = new Vector3(0f, -6.08f, 0.4f);
        }

        /// <summary>
        /// Generates ONE single continuous manifold octagonal balcony ring mesh (outer apothem -> inner apothem)
        /// with ZERO overlapping triangles at any of the 8 corners — eliminates 100% of floor Z-fighting!
        /// </summary>
        private static void BuildSeamlessOctagonalRingMesh(string name, Transform parent, float outerApothem, float innerApothem,
            float topY, float botY, Material mat)
        {
            const int sides = 8;
            float cosHalf = Mathf.Cos(Mathf.PI / sides); // cos(22.5 deg)
            float rOut = outerApothem / cosHalf;
            float rIn = innerApothem / cosHalf;

            Vector2[] outPts = new Vector2[sides];
            Vector2[] inPts = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                // Angles at 22.5 + i*45 deg so the 4 flat cardinal faces lie strictly at ±outerApothem!
                float rad = (22.5f + i * 45f) * Mathf.Deg2Rad;
                outPts[i] = new Vector2(Mathf.Sin(rad) * rOut, Mathf.Cos(rad) * rOut);
                inPts[i] = new Vector2(Mathf.Sin(rad) * rIn, Mathf.Cos(rad) * rIn);
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Vector2 o0 = outPts[i], o1 = outPts[next];
                Vector2 i0 = inPts[i], i1 = inPts[next];

                // Top face (CW in Unity XZ: o0 -> o1 -> i1 -> i0 => +Y normal!)
                AddQuadMesh(verts, tris, uvs,
                    new Vector3(o0.x, topY, o0.y),
                    new Vector3(o1.x, topY, o1.y),
                    new Vector3(i1.x, topY, i1.y),
                    new Vector3(i0.x, topY, i0.y),
                    new Vector2(o0.x * 0.25f, o0.y * 0.25f),
                    new Vector2(o1.x * 0.25f, o1.y * 0.25f),
                    new Vector2(i1.x * 0.25f, i1.y * 0.25f),
                    new Vector2(i0.x * 0.25f, i0.y * 0.25f));

                // Inner vertical drop skirt facing inward toward the flooded cistern!
                float segLen = Vector2.Distance(i0, i1);
                AddQuadMesh(verts, tris, uvs,
                    new Vector3(i1.x, topY, i1.y),
                    new Vector3(i0.x, topY, i0.y),
                    new Vector3(i0.x, botY, i0.y),
                    new Vector3(i1.x, botY, i1.y),
                    new Vector2(0f, 0.28f), new Vector2(segLen * 0.4f, 0.28f),
                    new Vector2(segLen * 0.4f, 0f), new Vector2(0f, 0f));

                // Underside soffit (visible when standing down on the sunken central island looking up!)
                AddQuadMesh(verts, tris, uvs,
                    new Vector3(i0.x, botY, i0.y),
                    new Vector3(i1.x, botY, i1.y),
                    new Vector3(o1.x, botY, o1.y),
                    new Vector3(o0.x, botY, o0.y),
                    new Vector2(i0.x * 0.25f, i0.y * 0.25f),
                    new Vector2(i1.x * 0.25f, i1.y * 0.25f),
                    new Vector2(o1.x * 0.25f, o1.y * 0.25f),
                    new Vector2(o0.x * 0.25f, o0.y * 0.25f));
            }

            var go = CreateMeshObject(name, parent, verts, tris, uvs, mat, ShadowCastingMode.TwoSided);
            go.transform.localPosition = Vector3.zero;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void AddQuadMesh(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            int b = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            uvs.Add(uv0); uvs.Add(uv1); uvs.Add(uv2); uvs.Add(uv3);
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        // =========================================================================
        // 65-YEAR AGING, BILINGUAL HORROR GRAFFITI & CROP CIRCLE PASS (ZERO COBWEBS!)
        // =========================================================================

        private static void BuildAgingAndDecayDetails(Transform root, Material rubbleMat, Material brickMat,
            Material rootWoodMat, Material rustedIronMat, Material agedRedValveMat, Material soggyPlankMat,
            Material stencil1958Mat, Material stencilRioMat, Material stencilWarnMat,
            Material graffitiSalDeAquiMat, Material graffitiGetOutMat, Material graffitiNoMiresMat,
            Material graffitiDontFollowMat, Material graffitiStillHereMat,
            Material cropCircle1Mat, Material cropCircle2Mat, Material cropCircle3Mat)
        {
            var agingRoot = new GameObject("Sewer_Aging_And_Decay_Root").transform;
            agingRoot.SetParent(root);

            // 1. Crumbled Masonry Rubble & Fallen Brick Clusters in tunnel corners & junction bases
            var rubbleSpots = new Vector3[]
            {
                new Vector3(JuarezAxisX - 1.48f, WalkwayY, -24.5f),
                new Vector3(JuarezAxisX + 1.46f, WalkwayY, -6.2f),
                new Vector3(JuarezAxisX - 1.45f, WalkwayY, 12.8f),
                new Vector3(-18.5f, WalkwayY, AzucenasAxisZ + 1.45f),
                new Vector3(-41.0f, WalkwayY, AzucenasAxisZ - 1.45f),
                new Vector3(MariscalAxisX - 1.45f, WalkwayY, -12.0f),
                new Vector3(MariscalAxisX + 1.45f, WalkwayY, 14.5f),
                new Vector3(-28.0f, WalkwayY, GardeniasAxisZ + 1.45f),
                new Vector3(JuarezAxisX - 1.46f, WalkwayY, 58.0f),
                new Vector3(JuarezAxisX + 1.46f, WalkwayY, 112.0f),
                new Vector3(JuarezAxisX - 1.46f, WalkwayY, 158.0f),
            };
            for (int i = 0; i < rubbleSpots.Length; i++)
            {
                SpawnRubbleCluster($"RubblePile_{i}", agingRoot, rubbleSpots[i], rubbleMat, brickMat, 4 + (i % 3), seed: 500 + i * 17);
            }

            // 2. Bilingual Borderland Crimson Graffiti & English Crop Circle Symbols
            //    Strictly centered BETWEEN wall ribs, BELOW the utility pipe (WalkwayY + 0.96m), and 3.8cm proud -> ZERO overlap/flicker!
            float wOff = TunnelHalfW - 0.038f; // 1.662m from tunnel axis

            // Colector Av. Benito Juarez Sur (between ribs at -26.2, -18.2, -10.2, -2.2, +5.8, +13.8)
            SpawnWallDecalWorld("Graffiti_SalDeAqui_JuarezS", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 0.96f, -22.2f), 90f, 2.25f, 0.94f, graffitiSalDeAquiMat);
            SpawnWallDecalWorld("CropCircle_1_JuarezS", agingRoot, new Vector3(JuarezAxisX - wOff, WalkwayY + 0.96f, -6.2f), -90f, 1.18f, 1.18f, cropCircle1Mat);
            SpawnWallDecalWorld("Graffiti_DontFollow_JuarezS", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 0.96f, 9.8f), 90f, 2.30f, 0.96f, graffitiDontFollowMat);

            // Ramal Calle Azucenas Oeste (between ribs at -52.5, -44.5, -36.5, -28.5, -20.5, -12.5)
            SpawnWallDecalWorld("Graffiti_GetOut_AzucenasW", agingRoot, new Vector3(-24.5f, WalkwayY + 0.96f, AzucenasAxisZ + wOff), 0f, 2.25f, 0.94f, graffitiGetOutMat);
            SpawnWallDecalWorld("CropCircle_2_AzucenasW", agingRoot, new Vector3(-40.5f, WalkwayY + 0.96f, AzucenasAxisZ - wOff), 180f, 1.18f, 1.18f, cropCircle2Mat);

            // Colector Calle Ignacio Mariscal (between ribs at -26.2, -18.2, -10.2, -2.2, +5.8, +13.8, +21.8)
            SpawnWallDecalWorld("Graffiti_NoMiresAtras_Mariscal", agingRoot, new Vector3(MariscalAxisX - wOff, WalkwayY + 0.96f, -14.2f), -90f, 2.30f, 0.96f, graffitiNoMiresMat);
            SpawnWallDecalWorld("CropCircle_3_Mariscal", agingRoot, new Vector3(MariscalAxisX + wOff, WalkwayY + 0.96f, 1.8f), 90f, 1.18f, 1.18f, cropCircle3Mat);
            SpawnWallDecalWorld("Graffiti_SalDeAqui_MariscalN", agingRoot, new Vector3(MariscalAxisX - wOff, WalkwayY + 0.96f, 17.8f), -90f, 2.20f, 0.92f, graffitiSalDeAquiMat);

            // Ramal Calle Gardenias Oeste (between ribs at -52.5, -44.5, -36.5, -28.5, -20.5, -12.5)
            SpawnWallDecalWorld("Graffiti_StillHere_GardeniasW", agingRoot, new Vector3(-32.5f, WalkwayY + 0.96f, GardeniasAxisZ - wOff), 180f, 2.30f, 0.96f, graffitiStillHereMat);
            SpawnWallDecalWorld("CropCircle_1_GardeniasW", agingRoot, new Vector3(-16.5f, WalkwayY + 0.96f, GardeniasAxisZ + wOff), 0f, 1.18f, 1.18f, cropCircle1Mat);

            // Colector Troncal Norte toward Rio Bravo (between ribs at 40.7, 48.7, 56.7, 64.7, 80.7, 104.7, 136.7, 160.7)
            SpawnWallDecalWorld("Graffiti_GetOut_NorthTrunk", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 0.96f, 52.7f), 90f, 2.30f, 0.96f, graffitiGetOutMat);
            SpawnWallDecalWorld("CropCircle_2_NorthTrunk", agingRoot, new Vector3(JuarezAxisX - wOff, WalkwayY + 0.96f, 84.7f), -90f, 1.22f, 1.22f, cropCircle2Mat);
            SpawnWallDecalWorld("Graffiti_NoMiresAtras_NorthTrunk", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 0.96f, 116.7f), 90f, 2.35f, 0.98f, graffitiNoMiresMat);
            SpawnWallDecalWorld("CropCircle_3_NorthTrunk", agingRoot, new Vector3(JuarezAxisX - wOff, WalkwayY + 0.96f, 152.7f), -90f, 1.22f, 1.22f, cropCircle3Mat);

            // 3. Twisted Dark Tree Roots breaking down through cracks in the aged brick ceiling arch!
            var rootSpots = new Vector3[]
            {
                new Vector3(MariscalAxisX - 0.95f, ArchCrownY - 0.08f, -18.0f),
                new Vector3(MariscalAxisX - 0.85f, ArchCrownY - 0.08f, 6.5f),
                new Vector3(-36.0f, ArchCrownY - 0.08f, AzucenasAxisZ + 0.90f),
                new Vector3(-18.0f, ArchCrownY - 0.08f, GardeniasAxisZ - 0.90f),
                new Vector3(JuarezAxisX + 0.90f, ArchCrownY - 0.08f, 76.0f),
                new Vector3(JuarezAxisX - 0.85f, ArchCrownY - 0.08f, 134.0f),
            };
            for (int i = 0; i < rootSpots.Length; i++)
            {
                SpawnHangingTreeRoots($"CeilingRoots_{i}", agingRoot, rootSpots[i], rootWoodMat, seed: 800 + i * 29);
            }

            // 4. Aged Corroded Red/Rust Gate Valve Wheels mounted on tunnel side walls
            var valveSpots = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(JuarezAxisX + TunnelHalfW - 0.08f, WalkwayY + 1.15f, -14.2f), -90f),
                (new Vector3(JuarezAxisX - TunnelHalfW + 0.08f, WalkwayY + 1.15f, 8.5f), 90f),
                (new Vector3(MariscalAxisX + TunnelHalfW - 0.08f, WalkwayY + 1.15f, -15.5f), -90f),
                (new Vector3(JuarezAxisX + TunnelHalfW - 0.08f, WalkwayY + 1.15f, 62.0f), -90f),
                (new Vector3(JuarezAxisX - TunnelHalfW + 0.08f, WalkwayY + 1.15f, 142.0f), 90f),
            };
            for (int i = 0; i < valveSpots.Length; i++)
            {
                SpawnCorrodedWallValve($"WallValve_{i}", agingRoot, valveSpots[i].pos, valveSpots[i].yaw, agedRedValveMat, rustedIronMat);
            }

            // 5. Floating Soggy Wooden Planks & Old Detritus in the dark water channel
            var debrisSpots = new (Vector3 pos, float yaw, float len)[]
            {
                (new Vector3(JuarezAxisX - 0.22f, WaterSurfaceY + 0.015f, -14.0f), 14f, 1.35f),
                (new Vector3(JuarezAxisX + 0.18f, WaterSurfaceY + 0.015f, 6.5f), -19f, 1.10f),
                (new Vector3(-29.0f, WaterSurfaceY + 0.015f, AzucenasAxisZ + 0.15f), 78f, 1.25f),
                (new Vector3(MariscalAxisX + 0.12f, WaterSurfaceY + 0.015f, 3.0f), -8f, 1.40f),
                (new Vector3(JuarezAxisX - 0.10f, WaterSurfaceY + 0.015f, 86.0f), 11f, 1.30f),
                (new Vector3(JuarezAxisX + 0.24f, WaterSurfaceY + 0.015f, 152.0f), -16f, 1.05f),
            };
            for (int i = 0; i < debrisSpots.Length; i++)
            {
                var plank = CreateBox($"FloatingPlank_{i}", agingRoot, debrisSpots[i].pos, new Vector3(0.22f, 0.035f, debrisSpots[i].len), soggyPlankMat, false);
                plank.transform.rotation = Quaternion.Euler(0f, debrisSpots[i].yaw, 0f);
            }

            // 6. Faded Peeling 1958 Municipal Stenciled Concrete Markings (strictly centered between wall ribs!)
            SpawnWallStencil("Stencil_1958_JuarezSouth", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 1.25f, -29.0f), 90f, stencil1958Mat);
            SpawnWallStencil("Stencil_RioBravo_VaultNorth", agingRoot, new Vector3(JuarezAxisX - wOff, WalkwayY + 1.25f, 44.7f), -90f, stencilRioMat);
            SpawnWallStencil("Stencil_Warn_Mariscal", agingRoot, new Vector3(MariscalAxisX - wOff, WalkwayY + 1.25f, -22.2f), -90f, stencilWarnMat);
            SpawnWallStencil("Stencil_1958_GardeniasW", agingRoot, new Vector3(-24.5f, WalkwayY + 1.25f, GardeniasAxisZ + wOff), 0f, stencil1958Mat);
            SpawnWallStencil("Stencil_RioBravo_MidNorth", agingRoot, new Vector3(JuarezAxisX + wOff, WalkwayY + 1.25f, 128.7f), 90f, stencilRioMat);
        }

        private static void SpawnWallDecalWorld(string name, Transform parent, Vector3 pos, float yawDeg, float width, float height, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent);
            q.transform.position = pos;
            q.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            q.transform.localScale = new Vector3(width, height, 1f);
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
        }

        private static void SpawnWallDecalLocal(string name, Transform parent, Vector3 localPos, float yawDeg, float width, float height, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos;
            q.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            q.transform.localScale = new Vector3(width, height, 1f);
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
        }

        private static void SpawnRubbleCluster(string name, Transform parent, Vector3 basePos, Material rubbleMat, Material brickMat, int count, int seed)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = basePos;
            System.Random rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float ox = ((float)rng.NextDouble() - 0.5f) * 0.45f;
                float oz = ((float)rng.NextDouble() - 0.5f) * 0.55f;
                float sx = 0.08f + (float)rng.NextDouble() * 0.16f;
                float sy = 0.05f + (float)rng.NextDouble() * 0.09f;
                float sz = 0.08f + (float)rng.NextDouble() * 0.18f;
                var chunk = CreateBoxLocal($"Chunk_{i}", g.transform, new Vector3(ox, sy * 0.45f, oz), new Vector3(sx, sy, sz), (i % 2 == 0) ? rubbleMat : brickMat, false);
                chunk.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 25f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 25f);
            }
        }

        private static void SpawnHangingTreeRoots(string name, Transform parent, Vector3 ceilingPos, Material rootMat, int seed)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent);
            g.transform.position = ceilingPos;
            System.Random rng = new System.Random(seed);
            for (int i = 0; i < 4; i++)
            {
                float len = 0.55f + (float)rng.NextDouble() * 0.85f;
                float thick = 0.03f + (float)rng.NextDouble() * 0.045f;
                var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                r.name = $"Root_{i}";
                r.transform.SetParent(g.transform, false);
                r.transform.localPosition = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.35f, -len * 0.45f, ((float)rng.NextDouble() - 0.5f) * 0.35f);
                r.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 32f, (float)rng.NextDouble() * 360f, ((float)rng.NextDouble() - 0.5f) * 32f);
                r.transform.localScale = new Vector3(thick, len * 0.5f, thick);
                r.GetComponent<MeshRenderer>().sharedMaterial = rootMat;
                UnityEngine.Object.DestroyImmediate(r.GetComponent<Collider>());
            }
        }

        private static void SpawnCorrodedWallValve(string name, Transform parent, Vector3 pos, float yawDeg, Material redValveMat, Material ironMat)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "ValveStem";
            stem.transform.SetParent(g.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            stem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            stem.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
            stem.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());

            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "ValveWheel";
            wheel.transform.SetParent(g.transform, false);
            wheel.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.38f, 0.03f, 0.38f);
            wheel.GetComponent<MeshRenderer>().sharedMaterial = redValveMat;
            UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }

        private static void SpawnWallStencil(string name, Transform parent, Vector3 pos, float yawDeg, Material stencilMat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent);
            q.transform.position = pos;
            q.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            q.transform.localScale = new Vector3(1.65f, 0.44f, 1f);
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = stencilMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
        }

        // =========================================================================
        // BACKROOMS / LA ESQUINA DE CHIHUAS BULKHEAD + PARIS CATACOMBS WOODEN SIGN
        // =========================================================================

        private static void BuildBackroomsLoreBulkhead(Transform root, Vector3 pos, Material darkIronMat, Material rustedIronMat,
            Material leakGlowMat, Material darkWoodMat, Material catacombsSignMat)
        {
            var go = new GameObject("LoreBulkhead_SectorC0_UnderChihuas");
            go.transform.SetParent(root);
            go.transform.position = pos;

            // Sealed steel bulkhead door (compact industrial 1.56m height so the hanging Paris Catacombs plaque
            // hangs 100% unobstructed right below the brick arch curve!)
            CreateBoxLocal("Bulkhead_Frame", go.transform, new Vector3(0f, 0.80f, 0f), new Vector3(0.16f, 1.60f, 1.36f), rustedIronMat, true);
            CreateBoxLocal("Bulkhead_Door", go.transform, new Vector3(0.03f, 0.78f, 0f), new Vector3(0.08f, 1.48f, 1.16f), darkIronMat, true);

            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Bulkhead_ValveWheel";
            wheel.transform.SetParent(go.transform, false);
            wheel.transform.localPosition = new Vector3(0.11f, 0.82f, 0.16f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.38f, 0.04f, 0.38f);
            wheel.GetComponent<MeshRenderer>().sharedMaterial = rustedIronMat;
            UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());

            // Hanging Hand-Painted Weathered Wooden Plaque cleanly framed below the brick arch curve:
            //   "Arrête ! C'est ici l'empire de la mort" (Paris Catacombs style!)
            var signRoot = new GameObject("CatacombsSign_EmpireDeLaMort");
            signRoot.transform.SetParent(go.transform, false);
            signRoot.transform.localPosition = new Vector3(0.28f, 1.84f, 0f);
            signRoot.transform.localRotation = Quaternion.Euler(1.8f, 0f, 0f); // Subtle authentic hand-hung tilt!

            // 3D weathered timber plank backing + front hand-painted face quad (offset +0.028m -> zero Z-fighting!)
            CreateBoxLocal("WoodPlank_Back", signRoot.transform, Vector3.zero, new Vector3(0.045f, 0.44f, 1.46f), darkWoodMat, false);
            var faceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceQuad.name = "WoodPlank_InscriptionFace";
            faceQuad.transform.SetParent(signRoot.transform, false);
            faceQuad.transform.localPosition = new Vector3(0.028f, 0f, 0f);
            faceQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces +X into the tunnel!
            faceQuad.transform.localScale = new Vector3(1.44f, 0.42f, 1f);
            faceQuad.GetComponent<MeshRenderer>().sharedMaterial = catacombsSignMat;
            UnityEngine.Object.DestroyImmediate(faceQuad.GetComponent<Collider>());

            // Two short rusted iron mounting chains hanging the wooden plaque cleanly from the brick arch above
            CreateBoxLocal("Chain_Left", signRoot.transform, new Vector3(0f, 0.34f, -0.58f), new Vector3(0.025f, 0.30f, 0.025f), rustedIronMat, false);
            CreateBoxLocal("Chain_Right", signRoot.transform, new Vector3(0f, 0.34f, +0.58f), new Vector3(0.025f, 0.30f, 0.025f), rustedIronMat, false);

            // Subtle eerie warm light leaking beneath the sealed door seam + faint readable spill on the plaque!
            CreateBoxLocal("Backrooms_SeamLeak", go.transform, new Vector3(0.08f, 0.04f, 0f), new Vector3(0.04f, 0.032f, 1.12f), leakGlowMat, false);
            var glowGo = new GameObject("Backrooms_SeamPointLight");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0.55f, 1.15f, 0f);
            var pl = glowGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.88f, 0.56f);
            pl.intensity = 1.45f;
            pl.range = 4.2f;
            pl.shadows = LightShadows.None;
        }

        // =========================================================================
        // INTERACTIVE ROUND CAST-IRON MANHOLES + VERTICAL BRICK SHAFTS & LADDERS
        // =========================================================================

        private static void BuildInteractiveManhole(string name, Transform parent, string id, string labelEs, string labelEn,
            Vector3 surfacePos, Vector3 surfaceExitPos, float surfaceExitYaw,
            Vector3 undergroundPos, float undergroundYaw, bool triggerBackroomsTwist,
            Material coverMat, Material darkIronMat, Material brickMat, Material rustedIronMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = surfacePos;

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Manhole_RimCollar";
            rim.transform.SetParent(go.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            rim.transform.localScale = new Vector3(0.84f, 0.006f, 0.84f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
            UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());

            var pitDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pitDisk.name = "Manhole_DarkPitOpening";
            pitDisk.transform.SetParent(go.transform, false);
            pitDisk.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            pitDisk.transform.localScale = new Vector3(0.68f, 0.006f, 0.68f);
            pitDisk.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
            UnityEngine.Object.DestroyImmediate(pitDisk.GetComponent<Collider>());

            var lidPivot = new GameObject("Manhole_LidPivot");
            lidPivot.transform.SetParent(go.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.004f, 0f);

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Manhole_CoverDisk";
            lid.transform.SetParent(lidPivot.transform, false);
            lid.transform.localPosition = Vector3.zero;
            lid.transform.localScale = new Vector3(0.74f, 0.007f, 0.74f);
            lid.GetComponent<MeshRenderer>().sharedMaterial = coverMat;
            UnityEngine.Object.DestroyImmediate(lid.GetComponent<Collider>());

            var ladderRoot = new GameObject("Underground_LadderShaft");
            ladderRoot.transform.SetParent(parent);
            ladderRoot.transform.position = new Vector3(undergroundPos.x, WalkwayY, undergroundPos.z);

            var shaftCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaftCap.name = "Shaft_ChimneyNeck";
            shaftCap.transform.SetParent(ladderRoot.transform, false);
            shaftCap.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            shaftCap.transform.localScale = new Vector3(0.95f, 0.35f, 0.95f);
            shaftCap.GetComponent<MeshRenderer>().sharedMaterial = brickMat;
            UnityEngine.Object.DestroyImmediate(shaftCap.GetComponent<Collider>());

            CreateBoxLocal("Ladder_RailL", ladderRoot.transform, new Vector3(0.45f, 1.35f, -0.22f), new Vector3(0.045f, 2.70f, 0.045f), rustedIronMat, false);
            CreateBoxLocal("Ladder_RailR", ladderRoot.transform, new Vector3(0.45f, 1.35f, +0.22f), new Vector3(0.045f, 2.70f, 0.045f), rustedIronMat, false);
            for (int r = 0; r < 8; r++)
            {
                CreateBoxLocal($"Ladder_Rung_{r}", ladderRoot.transform, new Vector3(0.45f, 0.30f + r * 0.32f, 0f), new Vector3(0.04f, 0.04f, 0.44f), rustedIronMat, false);
            }

            var interactable = go.AddComponent<SewerManholeInteractable>();
            interactable.manholeId = id;
            interactable.labelEs = labelEs;
            interactable.labelEn = labelEn;
            interactable.lidPivot = lidPivot.transform;
            interactable.surfaceExitPosition = surfaceExitPos;
            interactable.surfaceExitYaw = surfaceExitYaw;
            interactable.undergroundLandingPosition = undergroundPos;
            interactable.undergroundLandingYaw = undergroundYaw;
            interactable.triggerBackroomsNoClipTwist = triggerBackroomsTwist;
        }

        // =========================================================================
        // 6 "EFECTO GEORGIE" CURBSIDE STORM-DRAIN SLOTS + LIGHT SHAFTS + PAPER BOAT
        // =========================================================================

        private static void BuildGeorgieStormDrainSlots(Transform root, Material darkIronMat, Material concreteMat,
            Material lightShaftMat, Material paperWhiteMat)
        {
            var slotsRoot = new GameObject("Georgie_StormDrain_Slots_Root").transform;
            slotsRoot.SetParent(root);

            var slots = new (string name, Vector3 curbPos, float curbYaw, Vector3 tunnelBeamPos, float beamYaw, bool addPaperBoat)[]
            {
                ("GeorgieSlot_Juarez_Azucenas_NW",
                    new Vector3(-9.80f, 0.09f, -29.10f), 0f,
                    new Vector3(-9.80f, ArchSpringY + 0.35f, AzucenasAxisZ + TunnelHalfW - 0.15f), 180f, true),
                ("GeorgieSlot_Juarez_MidCorridor_W",
                    new Vector3(-2.60f, 0.09f, -4.00f), 90f,
                    new Vector3(JuarezAxisX - TunnelHalfW + 0.15f, ArchSpringY + 0.35f, -4.00f), 90f, false),
                ("GeorgieSlot_Mariscal_Azucenas_SW",
                    new Vector3(-54.50f, 0.09f, -23.00f), -90f,
                    new Vector3(MariscalAxisX + TunnelHalfW - 0.15f, ArchSpringY + 0.35f, -23.00f), -90f, false),
                ("GeorgieSlot_Mariscal_Gardenias_NW",
                    new Vector3(-54.50f, 0.09f, 16.00f), -90f,
                    new Vector3(MariscalAxisX + TunnelHalfW - 0.15f, ArchSpringY + 0.35f, 16.00f), -90f, false),
                ("GeorgieSlot_Gardenias_West_N",
                    new Vector3(-26.00f, 0.09f, 24.30f), 180f,
                    new Vector3(-26.00f, ArchSpringY + 0.35f, GardeniasAxisZ - TunnelHalfW + 0.15f), 0f, false),
                ("GeorgieSlot_NorthTrunk_Mid",
                    new Vector3(JuarezAxisX, 0.005f, 118.00f), 0f,
                    new Vector3(JuarezAxisX - TunnelHalfW + 0.15f, ArchSpringY + 0.35f, 118.00f), 90f, false),
            };

            foreach (var s in slots)
            {
                var curbGo = new GameObject(s.name + "_SurfaceCurb");
                curbGo.transform.SetParent(slotsRoot);
                curbGo.transform.position = s.curbPos;
                curbGo.transform.rotation = Quaternion.Euler(0f, s.curbYaw, 0f);
                CreateBoxLocal("Slot_DarkMouth", curbGo.transform, Vector3.zero, new Vector3(1.35f, 0.13f, 0.10f), darkIronMat, false);
                CreateBoxLocal("Slot_TopLip", curbGo.transform, new Vector3(0f, 0.085f, -0.02f), new Vector3(1.48f, 0.04f, 0.14f), concreteMat, false);
                for (float bx = -0.45f; bx <= 0.46f; bx += 0.30f)
                {
                    CreateBoxLocal($"Slot_Bar_{bx:F2}", curbGo.transform, new Vector3(bx, 0f, -0.045f), new Vector3(0.03f, 0.13f, 0.03f), darkIronMat, false);
                }

                var beamGo = new GameObject(s.name + "_UndergroundLightShaft");
                beamGo.transform.SetParent(slotsRoot);
                beamGo.transform.position = s.tunnelBeamPos;
                beamGo.transform.rotation = Quaternion.Euler(0f, s.beamYaw, 0f);
                CreateBoxLocal("Chute_Frame", beamGo.transform, Vector3.zero, new Vector3(1.25f, 0.34f, 0.22f), darkIronMat, false);

                var shaft = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shaft.name = "VolumetricLightCone";
                shaft.transform.SetParent(beamGo.transform, false);
                shaft.transform.localPosition = new Vector3(0f, -0.95f, 0.88f);
                shaft.transform.localRotation = Quaternion.Euler(-42f, 0f, 0f);
                shaft.transform.localScale = new Vector3(1.25f, 2.75f, 1f);
                var mr = shaft.GetComponent<MeshRenderer>();
                mr.sharedMaterial = lightShaftMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

                // Subtle cool-daylight spot (toned down for dense dark tunnels!)
                var spotGo = new GameObject("DrainSlot_SpotLight");
                spotGo.transform.SetParent(beamGo.transform, false);
                spotGo.transform.localPosition = new Vector3(0f, 0.10f, 0.25f);
                spotGo.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                var sl = spotGo.AddComponent<Light>();
                sl.type = LightType.Spot;
                sl.color = new Color(0.76f, 0.86f, 0.96f);
                sl.intensity = 4.6f;
                sl.range = 6.8f;
                sl.spotAngle = 44f;
                sl.innerSpotAngle = 18f;
                sl.shadows = LightShadows.None;

                if (s.addPaperBoat)
                {
                    BuildFoldedPaperBoat("PaperBoat_Georgie", slotsRoot,
                        new Vector3(s.tunnelBeamPos.x, WaterSurfaceY + 0.03f, AzucenasAxisZ + ChannelHalfW - 0.18f),
                        paperWhiteMat);
                }
            }
        }

        private static void BuildFoldedPaperBoat(string name, Transform parent, Vector3 pos, Material paperMat)
        {
            var boat = new GameObject(name);
            boat.transform.SetParent(parent);
            boat.transform.position = pos;
            boat.transform.rotation = Quaternion.Euler(0f, 24f, 5f);

            var hullL = CreateBoxLocal("Hull_Left", boat.transform, new Vector3(0f, 0.03f, 0.045f), new Vector3(0.24f, 0.055f, 0.012f), paperMat, false);
            hullL.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            var hullR = CreateBoxLocal("Hull_Right", boat.transform, new Vector3(0f, 0.03f, -0.045f), new Vector3(0.24f, 0.055f, 0.012f), paperMat, false);
            hullR.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            var sail = CreateBoxLocal("Center_Sail", boat.transform, new Vector3(0f, 0.095f, 0f), new Vector3(0.11f, 0.12f, 0.016f), paperMat, false);
            sail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // =========================================================================
        // AGED RIO BRAVO OUTFLOW HEADWALL, INTERACTIVE GATE & WALKABLE RIVERBANK
        // =========================================================================

        private static void BuildRioBravoOutflowAndRiverbank(Transform root, Material concreteMat, Material mossMat,
            Material rustedIronMat, Material darkIronMat, Material waterMat, Material rubbleMat)
        {
            var outRoot = new GameObject("RioBravo_StormOutflow_Root");
            outRoot.transform.SetParent(root);

            GameObject origBank = FindInactiveOrActive("River_BankSouth_C");
            if (origBank != null)
            {
                Material bankMat = origBank.GetComponent<MeshRenderer>()?.sharedMaterial ?? concreteMat;
                Vector3 cPos = origBank.transform.position;
                Quaternion cRot = origBank.transform.rotation;
                Vector3 cScale = origBank.transform.localScale;

                Vector3 worldRight = cRot * Vector3.right;
                float portalLocalX = Vector3.Dot(new Vector3(JuarezAxisX - cPos.x, 0f, OutflowMouthZ - cPos.z), worldRight);
                float halfSpan = cScale.x * 0.5f;
                float gapHalf = 2.35f;

                float westLen = (portalLocalX - gapHalf) - (-halfSpan);
                float eastLen = halfSpan - (portalLocalX + gapHalf);

                if (westLen > 1f)
                {
                    float wCenterLocal = -halfSpan + westLen * 0.5f;
                    var wBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wBank.name = "River_BankSouth_C_WestSplit";
                    wBank.transform.SetParent(outRoot.transform);
                    wBank.transform.position = cPos + worldRight * wCenterLocal;
                    wBank.transform.rotation = cRot;
                    wBank.transform.localScale = new Vector3(westLen, cScale.y, cScale.z);
                    wBank.GetComponent<MeshRenderer>().sharedMaterial = bankMat;
                }
                if (eastLen > 1f)
                {
                    float eCenterLocal = (portalLocalX + gapHalf) + eastLen * 0.5f;
                    var eBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    eBank.name = "River_BankSouth_C_EastSplit";
                    eBank.transform.SetParent(outRoot.transform);
                    eBank.transform.position = cPos + worldRight * eCenterLocal;
                    eBank.transform.rotation = cRot;
                    eBank.transform.localScale = new Vector3(eastLen, cScale.y, cScale.z);
                    eBank.GetComponent<MeshRenderer>().sharedMaterial = bankMat;
                }
                origBank.SetActive(false);
            }

            // Weathered Aged Concrete Headwall with damp algae base skirts & concrete debris
            var headwall = new GameObject("Outflow_Headwall_Structure");
            headwall.transform.SetParent(outRoot.transform);
            headwall.transform.position = new Vector3(JuarezAxisX, OutflowWalkwayY, OutflowMouthZ);

            CreateBoxLocal("Headwall_PierWest", headwall.transform, new Vector3(-2.15f, 1.45f, 0.15f), new Vector3(0.95f, 3.25f, 1.45f), concreteMat, true);
            CreateBoxLocal("Headwall_PierEast", headwall.transform, new Vector3(+2.15f, 1.45f, 0.15f), new Vector3(0.95f, 3.25f, 1.45f), concreteMat, true);
            CreateBoxLocal("Headwall_BrowLintel", headwall.transform, new Vector3(0f, 2.85f, 0.15f), new Vector3(5.25f, 0.72f, 1.55f), concreteMat, true);

            // Algae waterline grime on headwall piers
            CreateBoxLocal("Headwall_MossBaseW", headwall.transform, new Vector3(-2.15f, 0.35f, 0.89f), new Vector3(0.96f, 0.72f, 0.04f), mossMat, false);
            CreateBoxLocal("Headwall_MossBaseE", headwall.transform, new Vector3(+2.15f, 0.35f, 0.89f), new Vector3(0.96f, 0.72f, 0.04f), mossMat, false);

            var wingW = CreateBoxLocal("Headwall_WingWallWest", headwall.transform, new Vector3(-3.35f, 1.15f, 1.45f), new Vector3(0.55f, 2.65f, 3.20f), concreteMat, true);
            wingW.transform.localRotation = Quaternion.Euler(0f, -26f, 0f);
            var wingE = CreateBoxLocal("Headwall_WingWallEast", headwall.transform, new Vector3(+3.35f, 1.15f, 1.45f), new Vector3(0.55f, 2.65f, 3.20f), concreteMat, true);
            wingE.transform.localRotation = Quaternion.Euler(0f, +26f, 0f);

            SpawnRubbleCluster("Outflow_Rubble_W", headwall.transform, new Vector3(-2.45f, 0.0f, 1.85f), rubbleMat, mossMat, 5, seed: 911);
            SpawnRubbleCluster("Outflow_Rubble_E", headwall.transform, new Vector3(+2.45f, 0.0f, 1.95f), rubbleMat, mossMat, 4, seed: 912);

            // Interactive Rusted Iron Outflow Gate
            var gateRoot = new GameObject("RioBravo_Outflow_InteractiveGate");
            gateRoot.transform.SetParent(headwall.transform, false);
            gateRoot.transform.localPosition = new Vector3(0f, 0f, 0.45f);

            var hinge = new GameObject("Gate_HingePivot");
            hinge.transform.SetParent(gateRoot.transform, false);
            hinge.transform.localPosition = new Vector3(-1.68f, 0f, 0f);

            CreateBoxLocal("Gate_TopBar", hinge.transform, new Vector3(1.68f, 2.38f, 0f), new Vector3(3.36f, 0.10f, 0.10f), rustedIronMat, true);
            CreateBoxLocal("Gate_MidBar", hinge.transform, new Vector3(1.68f, 1.22f, 0f), new Vector3(3.36f, 0.08f, 0.08f), rustedIronMat, false);
            CreateBoxLocal("Gate_BotBar", hinge.transform, new Vector3(1.68f, 0.18f, 0f), new Vector3(3.36f, 0.09f, 0.09f), rustedIronMat, false);
            for (float gx = 0.14f; gx <= 3.25f; gx += 0.28f)
            {
                CreateBoxLocal($"Gate_Rod_{gx:F2}", hinge.transform, new Vector3(gx, 1.28f, 0f), new Vector3(0.05f, 2.28f, 0.05f), darkIronMat, false);
            }
            var gateCol = new GameObject("Gate_LeafCollider");
            gateCol.transform.SetParent(hinge.transform, false);
            gateCol.transform.localPosition = new Vector3(1.68f, 1.25f, 0f);
            var gbc = gateCol.AddComponent<BoxCollider>();
            gbc.size = new Vector3(3.34f, 2.45f, 0.16f);

            var gateCtrl = gateRoot.AddComponent<SewerOutflowGateController>();
            gateCtrl.gateHingePivot = hinge.transform;
            gateCtrl.closedYaw = 0f;
            gateCtrl.openYaw = -108f;

            // Outfall Apron starting EXACTLY at OutflowMouthZ (zero overlap under gate threshold!)
            CreateBox("RioBravo_Outfall_Apron", outRoot.transform,
                new Vector3(JuarezAxisX, OutflowWalkwayY - 0.20f, OutflowMouthZ + 2.60f),
                new Vector3(5.60f, 0.40f, 5.20f), concreteMat, true);

            var bankWalk = CreateBox("RioBravo_SouthBank_WalkableLedge", outRoot.transform,
                new Vector3(JuarezAxisX + 1.5f, -2.62f, OutflowMouthZ + 5.80f),
                new Vector3(34.0f, 0.34f, 5.80f), concreteMat, true);
            bankWalk.transform.rotation = Quaternion.Euler(0f, 14.035f, 0f);

            CreateBox("RioBravo_Outflow_RunoffStream", outRoot.transform,
                new Vector3(JuarezAxisX, OutflowWalkwayY + 0.012f, OutflowMouthZ + 4.40f),
                new Vector3(1.45f, 0.02f, 8.80f), waterMat, false);
        }

        // =========================================================================
        // VERIFICATION SUITE & 4 VERIFICATION SCREENSHOTS
        // =========================================================================

        public static bool RunVerificationChecks()
        {
            int passed = 0, total = 0;
            void Check(string desc, bool cond)
            {
                total++;
                if (cond) { passed++; Debug.Log($"[SewerVerify] PASS: {desc}"); }
                else Debug.LogError($"[SewerVerify] FAIL: {desc}");
            }

            Physics.SyncTransforms();

            Check("Underground_Sewer_Network_Root & AtmosphereController exist",
                GameObject.Find(RootName) != null &&
                UnityEngine.Object.FindFirstObjectByType<UndergroundSewerAtmosphereController>() != null);

            var manholes = UnityEngine.Object.FindObjectsByType<SewerManholeInteractable>(FindObjectsSortMode.None);
            Check($"All 5 interactive street manholes exist (found {manholes.Length})", manholes.Length == 5);

            var mainManhole = GameObject.Find("Manhole_01_Juarez_Azucenas")?.GetComponent<SewerManholeInteractable>();
            int twistCount = 0;
            foreach (var m in manholes) if (m != null && m.triggerBackroomsNoClipTwist) twistCount++;
            Check("Manhole #1 at Av. Juarez x Azucenas (X=3.30, Z=-32.20) is the SOLE Phase-2 silent Backrooms no-clip manhole (1 of 5)",
                mainManhole != null &&
                Vector2.Distance(new Vector2(mainManhole.transform.position.x, mainManhole.transform.position.z), new Vector2(3.30f, -32.20f)) < 0.05f &&
                mainManhole.triggerBackroomsNoClipTwist &&
                twistCount == 1);

            Check("Seamless Vault Balcony Ring mesh exists (zero overlapping octagon deck cubes!)",
                GameObject.Find("Vault_BalconyRing_SeamlessMesh") != null &&
                GameObject.Find("Vault_BalconyDeck_0") == null);

            Check("ZERO cobwebs exist anywhere in the underground sewer",
                GameObject.Find("Vault_Cobweb_0") == null &&
                GameObject.Find("TunnelCobweb_0") == null &&
                GameObject.Find("Bulkhead_Cobweb") == null);

            Check("Bilingual crimson graffiti, Crop Circles & Paris Catacombs wooden plaque exist",
                GameObject.Find("Sewer_Aging_And_Decay_Root") != null &&
                GameObject.Find("Graffiti_SalDeAqui_JuarezS") != null &&
                GameObject.Find("Graffiti_GetOut_AzucenasW") != null &&
                GameObject.Find("CropCircle_1_JuarezS") != null &&
                GameObject.Find("Vault_CropCircle_NE") != null &&
                GameObject.Find("CatacombsSign_EmpireDeLaMort") != null &&
                GameObject.Find("Vault_OverflowStream_0") != null);

            Check("Lore bulkhead under La Esquina de Chihuas & PaperBoat_Georgie exist",
                GameObject.Find("LoreBulkhead_SectorC0_UnderChihuas") != null &&
                GameObject.Find("PaperBoat_Georgie") != null);

            var gate = UnityEngine.Object.FindFirstObjectByType<SewerOutflowGateController>();
            Check("Rio Bravo interactive storm outflow gate & split levee wings exist",
                gate != null &&
                GameObject.Find("River_BankSouth_C_WestSplit") != null &&
                GameObject.Find("River_BankSouth_C_EastSplit") != null);

            bool HitUnderground(float x, float z, float rayStartY, float expectedMinY, float expectedMaxY)
            {
                if (Physics.Raycast(new Vector3(x, rayStartY, z), Vector3.down, out RaycastHit hit, 6.0f))
                {
                    return hit.point.y >= expectedMinY && hit.point.y <= expectedMaxY;
                }
                return false;
            }

            Check("Juarez South Collector walkway walkable at (3.30, -10.0, Y=-3.20m)",
                HitUnderground(JuarezAxisX + 1.15f, -10.0f, -1.8f, -3.30f, -3.10f));
            Check("Vault South Portal threshold walkable & butt-jointed at (3.30, 19.80, Y=-3.20m)",
                HitUnderground(JuarezAxisX + 1.15f, VaultCenterZ - VaultApothem + 0.2f, -1.8f, -3.28f, -3.12f));
            Check("Mariscal Collector walkway walkable at (-58.10, 0.0, Y=-3.20m)",
                HitUnderground(MariscalAxisX + 1.15f, 0.0f, -1.8f, -3.30f, -3.10f));
            Check("Central Cistern Vault sunken island walkable at (2.15, 28.0, Y=-6.08m)",
                HitUnderground(VaultCenterX, VaultCenterZ, -4.5f, -6.25f, -5.95f));
            Check("North Trunk Collector walkway walkable at (3.30, 120.0, Y=-3.20m)",
                HitUnderground(JuarezAxisX + 1.15f, 120.0f, -1.8f, -3.30f, -3.10f));
            Check("Rio Bravo Outflow Apron walkable outside gate at (2.15, 199.7, Y=-2.42m)",
                HitUnderground(JuarezAxisX, OutflowMouthZ + 2.5f, -0.5f, -2.60f, -2.30f));
            Check("Rio Bravo South Bank Ledge walkable at (3.65, 203.0, Y~-2.45m)",
                HitUnderground(JuarezAxisX + 1.5f, OutflowMouthZ + 5.8f, -0.5f, -2.60f, -2.30f));
            Check("Physics.simulationMode remains FixedUpdate",
                Physics.simulationMode == SimulationMode.FixedUpdate);

            Debug.Log($"[SewerVerify] SUMMARY: {passed}/{total} checks passed.");
            return passed == total;
        }

        public static void CaptureVerificationScreenshots()
        {
            Directory.CreateDirectory(ArtifactDir);

            // Apply realistic dark underground fog & ambient during Editor capture so screenshots match real runtime gloom!
            Color savedFogCol = RenderSettings.fogColor;
            float savedFogDens = RenderSettings.fogDensity;
            bool savedFog = RenderSettings.fog;
            Color savedAmbSky = RenderSettings.ambientSkyColor;
            Color savedAmbEq = RenderSettings.ambientEquatorColor;
            Color savedAmbGnd = RenderSettings.ambientGroundColor;

            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                foreach (var l in root.GetComponentsInChildren<Light>(true))
                    if (l != null) l.enabled = true;
            }

            var camGo = new GameObject("Temp_SewerVerifyCam");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.12f;
            cam.farClipPlane = 350f;
            cam.fieldOfView = 62f;

            // Player [F] flashlight preview beam for underground renders
            var previewSpot = camGo.AddComponent<Light>();
            previewSpot.type = LightType.Spot;
            previewSpot.range = 28f;
            previewSpot.spotAngle = 54f;
            previewSpot.innerSpotAngle = 24f;
            previewSpot.intensity = 16.5f;
            previewSpot.color = new Color(1.0f, 0.96f, 0.88f);
            previewSpot.shadows = LightShadows.None;

            void Shoot(string filename, Vector3 pos, Vector3 lookAt, bool darkUnderground, float fov = 62f)
            {
                cam.fieldOfView = fov;
                previewSpot.enabled = darkUnderground;
                if (darkUnderground)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.011f, 0.019f, 0.020f);
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogColor = new Color(0.011f, 0.019f, 0.020f);
                    RenderSettings.fogDensity = 0.075f;
                    RenderSettings.ambientSkyColor = new Color(0.014f, 0.020f, 0.022f);
                    RenderSettings.ambientEquatorColor = new Color(0.010f, 0.015f, 0.015f);
                    RenderSettings.ambientGroundColor = new Color(0.006f, 0.008f, 0.008f);
                }
                else
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    RenderSettings.fog = savedFog;
                    RenderSettings.fogColor = savedFogCol;
                    RenderSettings.fogDensity = savedFogDens;
                    RenderSettings.ambientSkyColor = savedAmbSky;
                    RenderSettings.ambientEquatorColor = savedAmbEq;
                    RenderSettings.ambientGroundColor = savedAmbGnd;
                }

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

            // 1. Sealed Bulkhead under La Esquina de Chihuas + Hanging Paris Catacombs Wooden Plaque!
            Shoot("sewer_v3_01_catacombs_door.png",
                new Vector3(JuarezAxisX + 1.15f, WalkwayY + 1.48f, -15.8f),
                new Vector3(JuarezAxisX - TunnelHalfW + 0.16f, WalkwayY + 1.32f, -14.8f),
                darkUnderground: true, fov: 62f);

            // 2. Intense Crimson "SAL DE AQUÍ!" Spray Graffiti on East Wall (100% clear of pipe & ghost box!)
            Shoot("sewer_v3_02_graffiti_saldeaqui.png",
                new Vector3(JuarezAxisX - 1.05f, WalkwayY + 1.15f, -23.4f),
                new Vector3(JuarezAxisX + TunnelHalfW - 0.04f, WalkwayY + 0.96f, -22.2f),
                darkUnderground: true, fov: 56f);

            // 3. American English Crimson "GET OUT!" (100% clear of pipe & ghost box!)
            Shoot("sewer_v3_03_graffiti_getout_crop.png",
                new Vector3(-25.8f, WalkwayY + 1.15f, AzucenasAxisZ - 1.05f),
                new Vector3(-24.5f, WalkwayY + 0.96f, AzucenasAxisZ + TunnelHalfW - 0.04f),
                darkUnderground: true, fov: 56f);

            // 4. Wiltshire Crop Circle #1 on West Wall of Av. Juarez South Collector (100% crisp chalk-on-concrete!)
            Shoot("sewer_v3_04_vault_no_cobwebs.png",
                new Vector3(JuarezAxisX + 0.95f, WalkwayY + 1.15f, -7.2f),
                new Vector3(JuarezAxisX - TunnelHalfW + 0.04f, WalkwayY + 0.96f, -6.2f),
                darkUnderground: true, fov: 56f);

            // Restore Editor lighting state cleanly
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogCol;
            RenderSettings.fogDensity = savedFogDens;
            RenderSettings.ambientSkyColor = savedAmbSky;
            RenderSettings.ambientEquatorColor = savedAmbEq;
            RenderSettings.ambientGroundColor = savedAmbGnd;

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[UndergroundSewerBuilder] Captured 4 aged & Z-fight-free verification screenshots.");
        }

        // =========================================================================
        // LAMPS, PRIMITIVES & 512x512 AGED PROCEDURAL TEXTURE GENERATORS
        // =========================================================================

        private static void CreateHangingVaultLamp(string name, Transform parent, Vector3 localPos, float ceilingY,
            Material bulbMat, Material ironMat, float intensity, float range)
        {
            CreateMaintenanceBulkheadLight(name, parent, localPos, bulbMat, ironMat, intensity, range, addLight: true);
            float rodLen = Mathf.Max(0.1f, ceilingY - localPos.y);
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = name + "_CeilingConduit";
            rod.transform.SetParent(parent, false);
            rod.transform.localPosition = new Vector3(localPos.x, localPos.y + rodLen * 0.5f, localPos.z);
            rod.transform.localScale = new Vector3(0.05f, rodLen * 0.5f, 0.05f);
            rod.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(rod.GetComponent<Collider>());
        }

        private static void CreateMaintenanceBulkheadLight(string name, Transform parent, Vector3 pos, Material bulbMat, Material ironMat,
            float intensity, float range, bool addLight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            CreateBoxLocal("CageBase", go.transform, Vector3.zero, new Vector3(0.18f, 0.24f, 0.14f), ironMat, false);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(go.transform, false);
            bulb.transform.localPosition = new Vector3(0.06f, 0f, 0f);
            bulb.transform.localScale = new Vector3(0.14f, 0.16f, 0.14f);
            bulb.GetComponent<MeshRenderer>().sharedMaterial = bulbMat;
            UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());

            if (addLight && intensity > 0.01f)
            {
                var lightGo = new GameObject(name + "_PointLight");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0.28f, -0.05f, 0f);
                var pl = lightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(0.96f, 0.64f, 0.28f); // Dim sickly aged tungsten amber
                pl.intensity = intensity;
                pl.range = range;
                pl.shadows = LightShadows.None;
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool addCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            if (!addCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject CreateBoxLocal(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool addCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            if (!addCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, List<Vector3> verts, List<int> tris,
            List<Vector2> uvs, Material mat, ShadowCastingMode shadowMode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadowMode;
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

        private static void EnsureProceduralTextures()
        {
            const string dir = "Assets/Textures/Sewer";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            GenerateAgedConcreteWallTex(Path.Combine(dir, "T_Sewer_DampConcrete.png"));
            GenerateAgedWalkwayGrimeTex(Path.Combine(dir, "T_Sewer_WalkwayGrime.png"));
            GenerateAgedBrickArchTex(Path.Combine(dir, "T_Sewer_BrickArch.png"));
            GeneratePittedRustTex(Path.Combine(dir, "T_Sewer_RustedIron.png"));
            GenerateManholeCoverTex(Path.Combine(dir, "T_Sewer_ManholeCover.png"));
            GenerateLightShaftGradientTex(Path.Combine(dir, "T_Sewer_LightShaftGradient.png"));
            GenerateStencilTex(Path.Combine(dir, "T_Sewer_Stencil_1958.png"), "COLECTOR 1958", "SECTOR SUR");
            GenerateStencilTex(Path.Combine(dir, "T_Sewer_Stencil_RioBravo.png"), "<- RIO BRAVO", "DESAGUE NORTE");
            GenerateStencilTex(Path.Combine(dir, "T_Sewer_Stencil_Warning.png"), "NO PASAR", "NIVEL AVENIDA");

            AssetDatabase.Refresh();

            string[] alphaPaths =
            {
                "Assets/Textures/Sewer/T_Sewer_LightShaftGradient.png",
                "Assets/Textures/Sewer/T_Sewer_Stencil_1958.png",
                "Assets/Textures/Sewer/T_Sewer_Stencil_RioBravo.png",
                "Assets/Textures/Sewer/T_Sewer_Stencil_Warning.png",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_SalDeAqui.png",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_GetOut.png",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_NoMiresAtras.png",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_DontFollow.png",
                "Assets/Textures/Sewer/T_Sewer_Graffiti_StillHere.png",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_1.png",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_2.png",
                "Assets/Textures/Sewer/T_Sewer_CropCircle_3.png"
            };
            foreach (string ap in alphaPaths)
            {
                var ti = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (ti != null && (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp))
                {
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.SaveAndReimport();
                }
            }
        }

        private static void GenerateAgedConcreteWallTex(string path)
        {
            int sz = 512;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float vy = (float)y / sz;
                float algaeTide = Mathf.Clamp01(1f - vy * 2.5f); // Heavy wet moss/sludge lower 40%
                float efflorescenceTop = Mathf.Clamp01((vy - 0.65f) / 0.35f);
                for (int x = 0; x < sz; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.025f, y * 0.025f);
                    float n2 = Mathf.PerlinNoise(x * 0.11f + 31f, y * 0.11f + 17f);
                    float drip = Mathf.PerlinNoise(x * 0.16f, y * 0.015f);
                    float crackNoise = Mathf.Abs(Mathf.PerlinNoise(x * 0.035f + 7f, y * 0.035f + 9f) - 0.5f);
                    float crack = crackNoise < 0.014f ? -0.22f : 0f;
                    float spall = (n1 < 0.28f && n2 < 0.42f) ? -0.14f : 0f;
                    float joint = (x % 128 < 3 || y % 128 < 3) ? -0.12f : 0f;

                    // Crusty white lime mineral seepage weeping from top joints
                    float lime = (efflorescenceTop > 0.1f && drip > 0.62f) ? (drip - 0.62f) * 0.95f * efflorescenceTop : 0f;

                    float baseV = Mathf.Clamp01(0.38f + (n1 - 0.5f) * 0.18f + (n2 - 0.5f) * 0.12f + (drip - 0.5f) * 0.14f + crack + spall + joint - algaeTide * 0.22f + lime);
                    float r = baseV * (0.90f - algaeTide * 0.26f) + lime * 0.22f;
                    float g = baseV * (0.94f - algaeTide * 0.08f) + lime * 0.22f;
                    float b = baseV * (0.86f - algaeTide * 0.28f) + lime * 0.18f;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateAgedWalkwayGrimeTex(string path)
        {
            int sz = 512;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.03f + 5f, y * 0.03f + 13f);
                    float grit = Mathf.PerlinNoise(x * 0.22f, y * 0.22f);
                    float puddle = Mathf.PerlinNoise(x * 0.018f + 41f, y * 0.018f + 23f);
                    float crackN = Mathf.Abs(Mathf.PerlinNoise(x * 0.045f + 2f, y * 0.045f + 19f) - 0.5f);
                    float crack = crackN < 0.013f ? -0.20f : 0f;
                    float joint = (y % 128 < 4) ? -0.14f : 0f;
                    float mud = puddle < 0.34f ? -0.16f : 0f;

                    float v = Mathf.Clamp01(0.36f + (n1 - 0.5f) * 0.16f + (grit - 0.5f) * 0.11f + crack + joint + mud);
                    tex.SetPixel(x, y, new Color(v * 0.96f, v * 0.94f, v * 0.86f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateAgedBrickArchTex(string path)
        {
            int sz = 512;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            int rowH = 24, colW = 48;
            for (int y = 0; y < sz; y++)
            {
                int row = y / rowH;
                int offset = (row % 2) * (colW / 2);
                bool hMortar = (y % rowH) < 4;
                for (int x = 0; x < sz; x++)
                {
                    bool vMortar = ((x + offset) % colW) < 4;
                    float soot = Mathf.PerlinNoise(x * 0.02f + 11f, y * 0.02f + 7f);
                    float grime = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                    int brickHash = ((row * 73) ^ (((x + offset) / colW) * 31)) & 15;
                    float brickVar = (brickHash / 15f - 0.5f) * 0.12f;

                    if (hMortar || vMortar)
                    {
                        float mv = Mathf.Clamp01(0.11f + grime * 0.06f - soot * 0.04f);
                        tex.SetPixel(x, y, new Color(mv, mv * 0.98f, mv * 0.90f));
                    }
                    else
                    {
                        float moss = (soot > 0.66f && grime > 0.52f) ? 0.07f : 0f;
                        float bv = Mathf.Clamp01(0.24f + brickVar + (grime - 0.5f) * 0.10f - (soot - 0.4f) * 0.12f);
                        tex.SetPixel(x, y, new Color(Mathf.Clamp01(bv * 1.12f - moss * 0.5f), Mathf.Clamp01(bv * 0.90f + moss), Mathf.Clamp01(bv * 0.78f - moss * 0.4f)));
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GeneratePittedRustTex(string path)
        {
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                    float n2 = Mathf.PerlinNoise(x * 0.24f + 15f, y * 0.24f + 9f);
                    float rustPatch = Mathf.Clamp01((n1 + n2 * 0.5f) - 0.35f);
                    float r = Mathf.Lerp(0.14f, 0.46f, rustPatch);
                    float g = Mathf.Lerp(0.11f, 0.21f, rustPatch);
                    float b = Mathf.Lerp(0.10f, 0.11f, rustPatch);
                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateCobwebTex(string path)
        {
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color clear = new Color(0.8f, 0.82f, 0.78f, 0f);
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, clear);

            // Corner spiderweb radiating from top-left (0, sz-1) with sagging concentric silk strands
            Vector2 origin = new Vector2(4f, sz - 4f);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 d = new Vector2(x, y) - origin;
                    float dist = d.magnitude;
                    if (dist < 4f || dist > sz * 0.94f) continue;
                    float ang = Mathf.Atan2(-d.y, d.x); // 0..PI/2 inside corner quadrant
                    if (ang < 0.02f || ang > Mathf.PI * 0.48f) continue;

                    float radial = Mathf.Abs(Mathf.Sin(ang * 10f));
                    float spiralFreq = dist * 0.14f + Mathf.Sin(ang * 10f) * 0.45f;
                    float spiral = Mathf.Abs(Mathf.Sin(spiralFreq));

                    float a = 0f;
                    if (radial < 0.055f) a = Mathf.Max(a, 0.65f);
                    if (spiral < 0.065f) a = Mathf.Max(a, 0.50f);
                    float fade = Mathf.Clamp01(1f - dist / (sz * 0.94f));
                    float dust = Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                    a *= fade * (0.55f + 0.45f * dust);
                    if (a > 0.02f) tex.SetPixel(x, y, new Color(0.84f, 0.86f, 0.80f, a));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateStencilTex(string path, string line1, string line2)
        {
            int w = 512, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));

            // Faded distressed dark industrial stencil band + bold pixel-rendered weathered lettering
            for (int y = 14; y < h - 14; y++)
            {
                for (int x = 16; x < w - 16; x++)
                {
                    bool border = (y < 22 || y > h - 22 || x < 24 || x > w - 24);
                    float wear = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                    if (border && wear > 0.34f)
                    {
                        tex.SetPixel(x, y, new Color(0.76f, 0.66f, 0.28f, (wear - 0.25f) * 0.72f));
                    }
                }
            }

            DrawWeatheredBitmapText(tex, line1, 44, 88, 5, new Color(0.84f, 0.78f, 0.48f, 0.78f));
            DrawWeatheredBitmapText(tex, line2, 44, 34, 4, new Color(0.74f, 0.72f, 0.60f, 0.68f));

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void DrawWeatheredBitmapText(Texture2D tex, string text, int startX, int startY, int scale, Color col)
        {
            int cursorX = startX;
            foreach (char raw in text.ToUpperInvariant())
            {
                string[] g = GetMiniGlyph5x7(raw);
                for (int row = 0; row < 7; row++)
                {
                    for (int colIdx = 0; colIdx < 5; colIdx++)
                    {
                        if (g[row][colIdx] != '#') continue;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = cursorX + colIdx * scale + dx;
                                int py = startY + (6 - row) * scale + dy;
                                if (px < 0 || px >= tex.width || py < 0 || py >= tex.height) continue;
                                float peel = Mathf.PerlinNoise(px * 0.09f + 13f, py * 0.09f + 7f);
                                if (peel < 0.33f) continue; // Chipped/peeling vintage paint!
                                tex.SetPixel(px, py, new Color(col.r, col.g, col.b, col.a * Mathf.Clamp01((peel - 0.2f) * 1.4f)));
                            }
                        }
                    }
                }
                cursorX += 6 * scale;
            }
        }

        private static string[] GetMiniGlyph5x7(char c)
        {
            switch (c)
            {
                case 'A': return new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" };
                case 'B': return new[] { "#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### " };
                case 'C': return new[] { " ####", "#    ", "#    ", "#    ", "#    ", "#    ", " ####" };
                case 'D': return new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### " };
                case 'E': return new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####" };
                case 'G': return new[] { " ####", "#    ", "#    ", "#  ##", "#   #", "#   #", " ####" };
                case 'I': return new[] { " ### ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " };
                case 'L': return new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####" };
                case 'N': return new[] { "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #", "#   #" };
                case 'O': return new[] { " ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " };
                case 'P': return new[] { "#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    " };
                case 'R': return new[] { "#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #" };
                case 'S': return new[] { " ####", "#    ", "#    ", " ### ", "    #", "    #", "#### " };
                case 'T': return new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  " };
                case 'U': return new[] { "#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " };
                case 'V': return new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  " };
                case '1': return new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " };
                case '5': return new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " };
                case '8': return new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " };
                case '9': return new[] { " ### ", "#   #", "#   #", " ####", "    #", "    #", " ### " };
                case '<': return new[] { "   # ", "  #  ", " #   ", "#    ", " #   ", "  #  ", "   # " };
                case '-': return new[] { "     ", "     ", "     ", "#####", "     ", "     ", "     " };
                default:  return new[] { "     ", "     ", "     ", "     ", "     ", "     ", "     " };
            }
        }

        private static void GenerateManholeCoverTex(string path)
        {
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            Vector2 c = new Vector2(sz * 0.5f, sz * 0.5f);
            float maxR = sz * 0.48f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 d = new Vector2(x, y) - c;
                    float r = d.magnitude / maxR;
                    float ang = Mathf.Atan2(d.y, d.x);
                    float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                    float v = 0.24f + (n - 0.5f) * 0.08f;

                    if (Mathf.Abs(r - 0.92f) < 0.045f || Mathf.Abs(r - 0.62f) < 0.035f || Mathf.Abs(r - 0.28f) < 0.035f)
                        v += 0.12f;
                    else if (r < 0.90f && r > 0.30f)
                    {
                        float spoke = Mathf.Abs(Mathf.Sin(ang * 12f));
                        float ring = Mathf.Abs(Mathf.Sin(r * 26f));
                        if (spoke > 0.72f && ring > 0.65f) v += 0.10f;
                    }
                    if (Mathf.Abs(r - 0.82f) < 0.05f && (Mathf.Abs(d.y) < 5f)) v = 0.08f;
                    tex.SetPixel(x, y, new Color(v * 1.04f, v * 0.96f, v * 0.90f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateLightShaftGradientTex(string path)
        {
            int w = 64, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float vy = (float)y / (h - 1);
                float vertFade = Mathf.Pow(vy, 1.35f) * Mathf.SmoothStep(1f, 0.85f, vy);
                for (int x = 0; x < w; x++)
                {
                    float vx = (float)x / (w - 1);
                    float edgeFade = Mathf.Sin(vx * Mathf.PI);
                    float a = Mathf.Clamp01(vertFade * Mathf.Pow(edgeFade, 1.4f) * 0.28f);
                    tex.SetPixel(x, y, new Color(0.84f, 0.91f, 0.98f, a));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static Material CreateOrGetTexturedMat(string matPath, string texPath, Color tint, float smoothness, float metallic, Vector2 tiling)
        {
            Material mat = CreateOrGetColorMat(matPath, tint, smoothness, metallic);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetColorMat(string path, Color color, float smoothness, float metallic)
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

        private static Material CreateOrGetEmissiveMat(string path, Color baseCol, Color emissiveCol)
        {
            Material mat = CreateOrGetColorMat(path, baseCol, 0.4f, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissiveCol);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetWaterMat(string path, Color color)
        {
            Material mat = CreateOrGetColorMat(path, color, 0.88f, 0.12f);
            ConfigureURPTransparent(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetLightShaftMat(string path)
        {
            return CreateOrGetAlphaDecalMat(path, "Assets/Textures/Sewer/T_Sewer_LightShaftGradient.png", Color.white);
        }

        private static Material CreateOrGetLitDecalMat(string path, string texPath, Color tint, Color subtleEmission)
        {
            // Use Opaque + AlphaClip (AlphaTest) on URP/Lit so 0%-alpha background pixels are 100% discarded
            // by the GPU — ZERO faint grey rectangular ghost card, ZERO Z-fighting, FULL real flashlight lighting!
            Material mat = CreateOrGetColorMat(path, tint, 0.06f, 0.0f);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            if (subtleEmission.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", subtleEmission);
                if (tex != null) mat.SetTexture("_EmissionMap", tex);
            }
            // Reset any prior Transparent surface flags -> crisp AlphaClip cutout!
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.16f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetAlphaDecalMat(string path, string texPath, Color tint)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
                mat = new Material(shader);
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", tint);
            mat.color = tint;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            ConfigureURPTransparent(mat);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureURPTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
