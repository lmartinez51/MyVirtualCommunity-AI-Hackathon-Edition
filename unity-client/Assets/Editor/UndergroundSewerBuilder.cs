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
    /// Phase 1 Idempotent Builder for the "Third World": Ciudad Juarez Underground Storm-Drain
    /// Collector Network ("El Alcantarillado") built physically underneath BorderCorridorScene.unity.
    ///
    /// Features:
    /// - 5 interactive round cast-iron municipal manholes ([E] slide lid & climb down/up),
    ///   including Manhole #1 at the exact Street View photo position (Av. Juarez x Azucenas,
    ///   X=+3.30m, Z=-32.20m) pre-wired with the Phase-2 Backrooms No-Clip flag.
    /// - Coherent vaulted storm-drain network beneath Av. Benito Juarez, Calle Azucenas,
    ///   Calle Ignacio Mariscal, Calle Gardenias, and the Paso del Norte Bridge approach.
    /// - Grand Octagonal Central Cistern Vault ("La Cisterna / El Nudo") beneath Av. Juarez x
    ///   Calle Gardenias (16.4m diameter, 6.0m vertical depth, sunken flooded basin, catwalk ring,
    ///   industrial staircase & dark recessed lair ready for the future "evil" character).
    /// - 6 "Efecto Georgie" curbside storm-drain intake slots with diagonal light shafts & a
    ///   subtle folded white paper boat in the shallow water edge.
    /// - Subtle lore bulkhead ("SECTOR C-0 / CLAUSURADO") directly underneath La Esquina de Chihuas.
    /// - Seamless ramped Northern Trunk Collector emerging through a cleanly split River_BankSouth_C
    ///   into a monumental weathered concrete storm outflow headwall + interactive rusted iron gate
    ///   onto the walkable South Bank of the Rio Bravo right beneath Puente Internacional Paso del Norte!
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

        // Central Cistern Vault ("La Cisterna / El Nudo" at Av. Juarez x Gardenias)
        private const float VaultCenterX = JuarezAxisX;
        private const float VaultCenterZ = GardeniasAxisZ;
        private const float VaultRadius = 8.20f;
        private const float VaultBasinFloorY = -6.55f;
        private const float VaultBasinWaterY = -6.28f;

        // Rio Bravo Outflow Headwall (proud on South Levee face beneath Puente Internacional Paso del Norte!)
        private const float RampStartZ = 174.00f;
        private const float OutflowMouthZ = 197.20f;
        private const float OutflowWalkwayY = -2.42f;
        private const float OutflowChannelY = -2.62f;

        [MenuItem("MyVirtualCommunity/Rebuild Underground Sewer Network (Phase 1)")]
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

        [MenuItem("MyVirtualCommunity/Rebuild, Verify & Capture Underground Sewer (Phase 1)")]
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

            // Build / load sewer materials
            Material concreteMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_DampConcrete.mat",
                "Assets/Textures/Sewer/T_Sewer_DampConcrete.png", new Color(0.56f, 0.58f, 0.55f), 0.22f, 0.0f, new Vector2(1f, 1f));
            Material brickArchMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_BrickArch.mat",
                "Assets/Textures/Sewer/T_Sewer_BrickArch.png", new Color(0.42f, 0.39f, 0.36f), 0.16f, 0.0f, new Vector2(1f, 1f));
            Material manholeCoverMat = CreateOrGetTexturedMat("Assets/Settings/Sewer/Sewer_ManholeCover.mat",
                "Assets/Textures/Sewer/T_Sewer_ManholeCover.png", new Color(0.32f, 0.33f, 0.34f), 0.32f, 0.65f, new Vector2(1f, 1f));
            Material rustedIronMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_RustedIron.mat",
                new Color(0.24f, 0.17f, 0.13f), 0.32f, 0.72f);
            Material darkIronMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_DarkIron.mat",
                new Color(0.14f, 0.15f, 0.16f), 0.38f, 0.78f);
            Material waterMat = CreateOrGetWaterMat("Assets/Settings/Sewer/Sewer_MurkyWater.mat",
                new Color(0.06f, 0.14f, 0.12f, 0.88f));
            Material warmBulbMat = CreateOrGetEmissiveMat("Assets/Settings/Sewer/Sewer_WarmBulb.mat",
                new Color(1.0f, 0.86f, 0.55f), new Color(2.6f, 1.85f, 0.85f));
            Material backroomsLeakMat = CreateOrGetEmissiveMat("Assets/Settings/Sewer/Sewer_BackroomsLeak.mat",
                new Color(1.0f, 0.94f, 0.60f), new Color(2.4f, 2.15f, 0.90f));
            Material lightShaftMat = CreateOrGetLightShaftMat("Assets/Settings/Sewer/Sewer_LightShaft.mat",
                new Color(0.86f, 0.92f, 0.98f, 0.14f));
            Material mossConcreteMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_MossConcrete.mat",
                new Color(0.30f, 0.36f, 0.27f), 0.14f, 0.0f);
            Material paperWhiteMat = CreateOrGetColorMat("Assets/Settings/Sewer/Sewer_PaperBoat.mat",
                new Color(0.94f, 0.92f, 0.86f), 0.08f, 0.0f);

            AssetDatabase.SaveAssets();

            GameObject root = new GameObject(RootName);
            root.transform.position = Vector3.zero;
            root.AddComponent<UndergroundSewerAtmosphereController>();

            // 1. Subsurface safety floor strictly under the sewer & vault so player can never fall into void
            BuildSewerSubsurfaceSafety(root.transform);

            // 2. Junction Chambers (SW, SE, NW) & Dead-End Barred Grates (East Azucenas, East Gardenias)
            BuildJunctionChamber("Chamber_Juarez_Azucenas", root.transform, JuarezAxisX, AzucenasAxisZ,
                openN: true, openS: false, openE: true, openW: true, concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildJunctionChamber("Chamber_Mariscal_Azucenas", root.transform, MariscalAxisX, AzucenasAxisZ,
                openN: true, openS: false, openE: true, openW: false, concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildJunctionChamber("Chamber_Mariscal_Gardenias", root.transform, MariscalAxisX, GardeniasAxisZ,
                openN: false, openS: true, openE: true, openW: false, concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3. Vaulted Storm-Drain Tunnel Network
            var tunnelsRoot = new GameObject("Sewer_Tunnels_Root").transform;
            tunnelsRoot.SetParent(root.transform);

            // 3A. Colector Av. Benito Juarez Sur (Azucenas -> Central Vault South Portal)
            BuildVaultedTunnelNS("Tunnel_Juarez_South", tunnelsRoot, JuarezAxisX, AzucenasAxisZ + 2.2f, VaultCenterZ - VaultRadius + 0.3f,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3B. Ramal Calle Azucenas Oeste (Mariscal -> Juarez)
            BuildVaultedTunnelEW("Tunnel_Azucenas_West", tunnelsRoot, AzucenasAxisZ, MariscalAxisX + 2.2f, JuarezAxisX - 2.2f,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3C. Ramal Calle Azucenas Este (Juarez -> Manuel Doblado barred end)
            BuildVaultedTunnelEW("Tunnel_Azucenas_East", tunnelsRoot, AzucenasAxisZ, JuarezAxisX + 2.2f, EastBranchEndX,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildBarredEndCap("EndGrate_Azucenas_East", tunnelsRoot, new Vector3(EastBranchEndX, WalkwayY, AzucenasAxisZ), 90f, concreteMat, rustedIronMat);

            // 3D. Colector Calle Ignacio Mariscal (Azucenas -> Gardenias)
            BuildVaultedTunnelNS("Tunnel_Mariscal_NS", tunnelsRoot, MariscalAxisX, AzucenasAxisZ + 2.2f, GardeniasAxisZ - 2.2f,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3E. Ramal Calle Gardenias Oeste (Mariscal -> Central Vault West Portal)
            BuildVaultedTunnelEW("Tunnel_Gardenias_West", tunnelsRoot, GardeniasAxisZ, MariscalAxisX + 2.2f, VaultCenterX - VaultRadius + 0.3f,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3F. Ramal Calle Gardenias Este (Central Vault East Portal -> East Branch)
            BuildVaultedTunnelEW("Tunnel_Gardenias_East", tunnelsRoot, GardeniasAxisZ, VaultCenterX + VaultRadius - 0.3f, EastBranchEndX,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);
            BuildBarredEndCap("EndGrate_Gardenias_East", tunnelsRoot, new Vector3(EastBranchEndX, WalkwayY, GardeniasAxisZ), 90f, concreteMat, rustedIronMat);

            // 3G. Colector Troncal Norte (Central Vault North Portal -> Ramp Start Z=174m)
            BuildVaultedTunnelNS("Tunnel_Juarez_NorthTrunk", tunnelsRoot, JuarezAxisX, VaultCenterZ + VaultRadius - 0.3f, RampStartZ,
                concreteMat, brickArchMat, waterMat, rustedIronMat, warmBulbMat);

            // 3H. Gentle Ramped North Collector (Z = 174m Y=-3.20m -> Outflow Headwall Z = 195m Y=-2.42m)
            BuildRampedOutflowTunnelNS("Tunnel_Juarez_NorthOutflowRamp", tunnelsRoot, JuarezAxisX, RampStartZ, OutflowMouthZ,
                WalkwayY, OutflowWalkwayY, concreteMat, brickArchMat, waterMat, rustedIronMat);

            // 4. Grand Octagonal Central Cistern Vault ("La Cisterna / El Nudo") — Future Lair of "Evil" Character!
            BuildGrandCentralCisternVault(root.transform, concreteMat, brickArchMat, waterMat, rustedIronMat, darkIronMat, warmBulbMat, mossConcreteMat);

            // 5. Subtle Lore Bulkhead directly underneath La Esquina de Chihuas (X = JuarezAxisX - TunnelHalfW, Z = -18.5m)
            BuildBackroomsLoreBulkhead(root.transform, new Vector3(JuarezAxisX - TunnelHalfW + 0.04f, WalkwayY, -18.50f),
                darkIronMat, rustedIronMat, backroomsLeakMat);

            // 6. 5 Interactive Round Cast-Iron Street Manholes + Vertical Brick Shafts & Ladders
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

            // Manhole #2: Calle Ignacio Mariscal x Calle Azucenas
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

            // Manhole #3: Calle Ignacio Mariscal x Calle Gardenias
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

            // Manhole #4: Calle Gardenias Este (near Manzana Este)
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

            // Manhole #5: Calle Azucenas Este (near Manuel Doblado)
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

            // 7. 6 "Efecto Georgie" Curbside Storm-Drain Intake Slots + Volumetric Light Shafts + Paper Boat!
            BuildGeorgieStormDrainSlots(root.transform, darkIronMat, concreteMat, lightShaftMat, paperWhiteMat);

            // 8. Rio Bravo Storm Outflow Headwall, Interactive Rusted Iron Gate & Walkable Riverside Bank
            BuildRioBravoOutflowAndRiverbank(root.transform, concreteMat, mossConcreteMat, rustedIronMat, darkIronMat, waterMat);

            Debug.Log("[UndergroundSewerBuilder] Built Phase 1 Underground Storm-Drain Network, Central Cistern Vault & Rio Bravo Outflow cleanly.");
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
            // Restore original River_BankSouth_C if we previously hid it before splitting
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
        // VAULTED TUNNELS (N-S, E-W & RAMPED OUTFLOW)
        // =========================================================================

        private static void BuildVaultedTunnelNS(string name, Transform parent, float centerX, float zMin, float zMax,
            Material concreteMat, Material brickMat, Material waterMat, Material ironMat, Material bulbMat)
        {
            float length = zMax - zMin;
            if (length <= 0.1f) return;
            float midZ = 0.5f * (zMin + zMax);

            var go = new GameObject(name);
            go.transform.SetParent(parent);

            float walkW = TunnelHalfW - ChannelHalfW; // 0.95m
            float leftX = centerX - ChannelHalfW - walkW * 0.5f;
            float rightX = centerX + ChannelHalfW + walkW * 0.5f;

            // Left & Right Concrete Walkways (Y = -3.20m)
            CreateBox("Walkway_West", go.transform, new Vector3(leftX, WalkwayY - 0.15f, midZ), new Vector3(walkW, 0.30f, length), concreteMat, true);
            CreateBox("Walkway_East", go.transform, new Vector3(rightX, WalkwayY - 0.15f, midZ), new Vector3(walkW, 0.30f, length), concreteMat, true);

            // Central Channel Floor (Y = -3.45m) & Flowing Water Strip (Y = -3.32m)
            CreateBox("Channel_Bed", go.transform, new Vector3(centerX, ChannelFloorY - 0.12f, midZ), new Vector3(ChannelHalfW * 2f + 0.10f, 0.24f, length), concreteMat, true);
            CreateBox("Channel_Water", go.transform, new Vector3(centerX, WaterSurfaceY, midZ), new Vector3(ChannelHalfW * 2f - 0.02f, 0.02f, length), waterMat, false);

            // Lower Vertical Walls (WalkwayY -> ArchSpringY)
            float wallH = ArchSpringY - WalkwayY + 0.30f; // ~1.75m
            float wallCenterY = WalkwayY - 0.15f + wallH * 0.5f;
            CreateBox("Wall_West", go.transform, new Vector3(centerX - TunnelHalfW - 0.14f, wallCenterY, midZ), new Vector3(0.28f, wallH, length), concreteMat, true);
            CreateBox("Wall_East", go.transform, new Vector3(centerX + TunnelHalfW + 0.14f, wallCenterY, midZ), new Vector3(0.28f, wallH, length), concreteMat, true);

            // True Curved Vault Arch Roof Mesh (7 segments, TwoSided shadows to block outdoor sun!)
            BuildBarrelVaultArchMesh("Vault_Arch", go.transform, new Vector3(centerX, ArchSpringY, zMin), new Vector3(centerX, ArchSpringY, zMax),
                TunnelHalfW + 0.04f, ArchCrownY - ArchSpringY, brickMat);

            // Rusted Overhead Utility Pipe running along East shoulder
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "UtilityPipe_East";
            pipe.transform.SetParent(go.transform);
            pipe.transform.position = new Vector3(centerX + TunnelHalfW - 0.14f, ArchSpringY - 0.08f, midZ);
            pipe.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pipe.transform.localScale = new Vector3(0.14f, length * 0.5f, 0.14f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

            // Concrete Structural Ribs & Dim Caged Bulkhead Lights
            for (float z = zMin + 4.0f; z <= zMax - 3.0f; z += 7.5f)
            {
                CreateBox($"Rib_W_{z:F0}", go.transform, new Vector3(centerX - TunnelHalfW + 0.06f, wallCenterY, z), new Vector3(0.14f, wallH, 0.24f), concreteMat, false);
                CreateBox($"Rib_E_{z:F0}", go.transform, new Vector3(centerX + TunnelHalfW - 0.06f, wallCenterY, z), new Vector3(0.14f, wallH, 0.24f), concreteMat, false);
            }
            for (float z = zMin + 8.0f; z <= zMax - 6.0f; z += 22.0f)
            {
                CreateMaintenanceBulkheadLight($"TunnelLight_{z:F0}", go.transform,
                    new Vector3(centerX - TunnelHalfW + 0.16f, ArchSpringY + 0.05f, z), bulbMat, ironMat, intensity: 3.4f, range: 13.5f);
            }
        }

        private static void BuildVaultedTunnelEW(string name, Transform parent, float centerZ, float xMin, float xMax,
            Material concreteMat, Material brickMat, Material waterMat, Material ironMat, Material bulbMat)
        {
            float length = xMax - xMin;
            if (length <= 0.1f) return;
            float midX = 0.5f * (xMin + xMax);

            var go = new GameObject(name);
            go.transform.SetParent(parent);

            float walkW = TunnelHalfW - ChannelHalfW;
            float southZ = centerZ - ChannelHalfW - walkW * 0.5f;
            float northZ = centerZ + ChannelHalfW + walkW * 0.5f;

            CreateBox("Walkway_South", go.transform, new Vector3(midX, WalkwayY - 0.15f, southZ), new Vector3(length, 0.30f, walkW), concreteMat, true);
            CreateBox("Walkway_North", go.transform, new Vector3(midX, WalkwayY - 0.15f, northZ), new Vector3(length, 0.30f, walkW), concreteMat, true);

            CreateBox("Channel_Bed", go.transform, new Vector3(midX, ChannelFloorY - 0.12f, centerZ), new Vector3(length, 0.24f, ChannelHalfW * 2f + 0.10f), concreteMat, true);
            CreateBox("Channel_Water", go.transform, new Vector3(midX, WaterSurfaceY, centerZ), new Vector3(length, 0.02f, ChannelHalfW * 2f - 0.02f), waterMat, false);

            float wallH = ArchSpringY - WalkwayY + 0.30f;
            float wallCenterY = WalkwayY - 0.15f + wallH * 0.5f;
            CreateBox("Wall_South", go.transform, new Vector3(midX, wallCenterY, centerZ - TunnelHalfW - 0.14f), new Vector3(length, wallH, 0.28f), concreteMat, true);
            CreateBox("Wall_North", go.transform, new Vector3(midX, wallCenterY, centerZ + TunnelHalfW + 0.14f), new Vector3(length, wallH, 0.28f), concreteMat, true);

            BuildBarrelVaultArchMesh("Vault_Arch", go.transform, new Vector3(xMin, ArchSpringY, centerZ), new Vector3(xMax, ArchSpringY, centerZ),
                TunnelHalfW + 0.04f, ArchCrownY - ArchSpringY, brickMat);

            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "UtilityPipe_North";
            pipe.transform.SetParent(go.transform);
            pipe.transform.position = new Vector3(midX, ArchSpringY - 0.08f, centerZ + TunnelHalfW - 0.14f);
            pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            pipe.transform.localScale = new Vector3(0.14f, length * 0.5f, 0.14f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());

            for (float x = xMin + 4.0f; x <= xMax - 3.0f; x += 7.5f)
            {
                CreateBox($"Rib_S_{x:F0}", go.transform, new Vector3(x, wallCenterY, centerZ - TunnelHalfW + 0.06f), new Vector3(0.24f, wallH, 0.14f), concreteMat, false);
                CreateBox($"Rib_N_{x:F0}", go.transform, new Vector3(x, wallCenterY, centerZ + TunnelHalfW - 0.06f), new Vector3(0.24f, wallH, 0.14f), concreteMat, false);
            }
            for (float x = xMin + 8.0f; x <= xMax - 6.0f; x += 20.0f)
            {
                CreateMaintenanceBulkheadLight($"TunnelLight_{x:F0}", go.transform,
                    new Vector3(x, ArchSpringY + 0.05f, centerZ - TunnelHalfW + 0.16f), bulbMat, ironMat, intensity: 3.4f, range: 13.5f);
            }
        }

        private static void BuildRampedOutflowTunnelNS(string name, Transform parent, float centerX, float zStart, float zEnd,
            float yStart, float yEnd, Material concreteMat, Material brickMat, Material waterMat, Material ironMat)
        {
            float dz = zEnd - zStart;
            float dy = yEnd - yStart;
            float slopeLen = Mathf.Sqrt(dz * dz + dy * dy);
            float pitchDeg = -Mathf.Atan2(dy, dz) * Mathf.Rad2Deg; // Negative X-rotation tilts +Z upward!

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(centerX, 0.5f * (yStart + yEnd), 0.5f * (zStart + zEnd));
            go.transform.rotation = Quaternion.Euler(pitchDeg, 0f, 0f);

            float walkW = TunnelHalfW - ChannelHalfW;
            CreateBoxLocal("Walkway_West", go.transform, new Vector3(-(ChannelHalfW + walkW * 0.5f), -0.15f, 0f), new Vector3(walkW, 0.30f, slopeLen + 0.25f), concreteMat, true);
            CreateBoxLocal("Walkway_East", go.transform, new Vector3(+(ChannelHalfW + walkW * 0.5f), -0.15f, 0f), new Vector3(walkW, 0.30f, slopeLen + 0.25f), concreteMat, true);
            CreateBoxLocal("Channel_Bed", go.transform, new Vector3(0f, -0.34f, 0f), new Vector3(ChannelHalfW * 2f + 0.10f, 0.24f, slopeLen + 0.25f), concreteMat, true);
            CreateBoxLocal("Channel_Water", go.transform, new Vector3(0f, -0.14f, 0f), new Vector3(ChannelHalfW * 2f - 0.02f, 0.02f, slopeLen + 0.25f), waterMat, false);

            float wallH = 1.75f;
            CreateBoxLocal("Wall_West", go.transform, new Vector3(-(TunnelHalfW + 0.14f), 0.72f, 0f), new Vector3(0.28f, wallH, slopeLen + 0.25f), concreteMat, true);
            CreateBoxLocal("Wall_East", go.transform, new Vector3(+(TunnelHalfW + 0.14f), 0.72f, 0f), new Vector3(0.28f, wallH, slopeLen + 0.25f), concreteMat, true);

            // Ramped arched ceiling in world coords (follows exact slope!)
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
                float a = Mathf.PI * ((float)i / arcSegs); // 0 (+right) -> PI (-right)
                Vector3 localOffset = right * (Mathf.Cos(a) * halfWidth) + up * (Mathf.Sin(a) * archHeight);
                verts.Add(startSpringCenter + localOffset);
                verts.Add(endSpringCenter + localOffset);
                uvs.Add(new Vector2((float)i / arcSegs * 2.5f, 0f));
                uvs.Add(new Vector2((float)i / arcSegs * 2.5f, len * 0.28f));
            }

            // Inward-facing ceiling triangles + TwoSided shadow casting so outdoor sun NEVER leaks through!
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
        // JUNCTION CHAMBERS & BARRED END CAPS
        // =========================================================================

        private static void BuildJunctionChamber(string name, Transform parent, float cx, float cz,
            bool openN, bool openS, bool openE, bool openW,
            Material concreteMat, Material brickMat, Material waterMat, Material ironMat, Material bulbMat)
        {
            const float half = 2.25f;
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            // Floor deck at WalkwayY with shallow recessed water basin center
            CreateBox("Chamber_Floor", go.transform, new Vector3(cx, WalkwayY - 0.15f, cz), new Vector3(half * 2f + 0.2f, 0.30f, half * 2f + 0.2f), concreteMat, true);
            CreateBox("Chamber_WaterPool", go.transform, new Vector3(cx, WaterSurfaceY + 0.04f, cz), new Vector3(ChannelHalfW * 2.1f, 0.02f, ChannelHalfW * 2.1f), waterMat, false);

            // Ceiling slab at ArchCrownY
            CreateBox("Chamber_Ceiling", go.transform, new Vector3(cx, ArchCrownY + 0.10f, cz), new Vector3(half * 2f + 0.4f, 0.24f, half * 2f + 0.4f), brickMat, true);

            float wallH = ArchCrownY - WalkwayY + 0.25f;
            float wallY = WalkwayY - 0.10f + wallH * 0.5f;

            // Closed walls on non-open cardinal sides; corner pilasters on all 4 corners
            if (!openN) CreateBox("Wall_N", go.transform, new Vector3(cx, wallY, cz + half), new Vector3(half * 2f + 0.3f, wallH, 0.28f), concreteMat, true);
            if (!openS) CreateBox("Wall_S", go.transform, new Vector3(cx, wallY, cz - half), new Vector3(half * 2f + 0.3f, wallH, 0.28f), concreteMat, true);
            if (!openE) CreateBox("Wall_E", go.transform, new Vector3(cx + half, wallY, cz), new Vector3(0.28f, wallH, half * 2f + 0.3f), concreteMat, true);
            if (!openW) CreateBox("Wall_W", go.transform, new Vector3(cx - half, wallY, cz), new Vector3(0.28f, wallH, half * 2f + 0.3f), concreteMat, true);

            // 4 solid corner masonry jambs framing tunnel portals cleanly
            float jambOffset = TunnelHalfW + 0.32f;
            CreateBox("Jamb_NW", go.transform, new Vector3(cx - jambOffset, wallY, cz + jambOffset), new Vector3(0.65f, wallH, 0.65f), brickMat, true);
            CreateBox("Jamb_NE", go.transform, new Vector3(cx + jambOffset, wallY, cz + jambOffset), new Vector3(0.65f, wallH, 0.65f), brickMat, true);
            CreateBox("Jamb_SW", go.transform, new Vector3(cx - jambOffset, wallY, cz - jambOffset), new Vector3(0.65f, wallH, 0.65f), brickMat, true);
            CreateBox("Jamb_SE", go.transform, new Vector3(cx + jambOffset, wallY, cz - jambOffset), new Vector3(0.65f, wallH, 0.65f), brickMat, true);

            // Warm dim maintenance lamp inside junction
            CreateMaintenanceBulkheadLight("Chamber_Lamp", go.transform, new Vector3(cx - 1.55f, ArchSpringY + 0.25f, cz + 1.55f),
                bulbMat, ironMat, intensity: 4.5f, range: 14f);
        }

        private static void BuildBarredEndCap(string name, Transform parent, Vector3 centerFloor, float yawDeg, Material concreteMat, Material ironMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = centerFloor;
            go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            // Dark backing wall 1.2m behind the iron bars for depth
            CreateBoxLocal("DarkBacking", go.transform, new Vector3(0f, 1.35f, 1.15f), new Vector3(3.8f, 3.0f, 0.3f), concreteMat, true);
            // Frame
            CreateBoxLocal("Frame_Top", go.transform, new Vector3(0f, 2.35f, 0f), new Vector3(3.5f, 0.14f, 0.14f), ironMat, true);
            // Vertical rusted bars
            for (float bx = -1.55f; bx <= 1.56f; bx += 0.28f)
            {
                CreateBoxLocal($"Bar_{bx:F2}", go.transform, new Vector3(bx, 1.15f, 0f), new Vector3(0.055f, 2.55f, 0.055f), ironMat, true);
            }
            // Solid collider preventing player clipping through bars
            var colGo = new GameObject("Grate_Blocker");
            colGo.transform.SetParent(go.transform, false);
            colGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var bc = colGo.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.5f, 2.6f, 0.25f);
        }

        // =========================================================================
        // GRAND CENTRAL CISTERN VAULT ("LA CISTERNA / EL NUDO" — FUTURE EVIL LAIR!)
        // =========================================================================

        private static void BuildGrandCentralCisternVault(Transform root, Material concreteMat, Material brickMat,
            Material waterMat, Material rustedIronMat, Material darkIronMat, Material bulbMat, Material mossMat)
        {
            var vault = new GameObject("Central_Cistern_Vault_LaGuarida");
            vault.transform.SetParent(root);
            vault.transform.position = new Vector3(VaultCenterX, 0f, VaultCenterZ);

            const int sides = 8;
            float r = VaultRadius; // 8.20m
            float ceilingY = -0.58f;
            float totalH = ceilingY - VaultBasinFloorY; // ~5.97m tall!

            // 1. Sunken Octagonal Cistern Floor & Murky Flooded Water Basin (Y = -6.55m / water -6.28m)
            CreateBoxLocal("Vault_BasinFloor", vault.transform, new Vector3(0f, VaultBasinFloorY - 0.20f, 0f), new Vector3(r * 2.15f, 0.40f, r * 2.15f), concreteMat, true);
            CreateBoxLocal("Vault_BasinWater", vault.transform, new Vector3(0f, VaultBasinWaterY, 0f), new Vector3(r * 2.02f, 0.04f, r * 2.02f), waterMat, false);

            // 2. Massive Vaulted Ceiling Cap (blocks 100% of street/sun light above Calle Gardenias)
            CreateBoxLocal("Vault_CeilingCap", vault.transform, new Vector3(0f, ceilingY + 0.14f, 0f), new Vector3(r * 2.20f, 0.28f, r * 2.20f), brickMat, true);

            // 3. 8 Octagonal Perimeter Walls (leaving clean arched openings at N, S, E, W above WalkwayY!)
            float faceLen = 2f * r * Mathf.Tan(Mathf.PI / sides); // ~6.79m per face
            for (int i = 0; i < sides; i++)
            {
                float angleDeg = i * 45f; // 0=+Z(N), 90=+X(E), 180=-Z(S), 270=-X(W)
                bool isPortalSide = (i % 2 == 0);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 faceDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 faceRight = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
                Vector3 faceCenter = faceDir * r;

                if (!isPortalSide)
                {
                    // Full solid damp concrete/brick diagonal wall
                    var w = CreateBoxLocal($"Vault_Wall_{i}", vault.transform,
                        new Vector3(faceCenter.x, VaultBasinFloorY + totalH * 0.5f, faceCenter.z),
                        new Vector3(faceLen + 0.35f, totalH + 0.2f, 0.45f), brickMat, true);
                    w.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                }
                else
                {
                    // Lower wall below portal walkway (Y = -6.55m -> -3.20m)
                    float lowerH = WalkwayY - VaultBasinFloorY;
                    var lowerW = CreateBoxLocal($"Vault_PortalLower_{i}", vault.transform,
                        new Vector3(faceCenter.x, VaultBasinFloorY + lowerH * 0.5f, faceCenter.z),
                        new Vector3(faceLen + 0.35f, lowerH, 0.45f), mossMat, true);
                    lowerW.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                    // Left & Right side cheek walls framing the 3.40m portal opening above WalkwayY
                    float upperH = ceilingY - WalkwayY;
                    float cheekW = (faceLen - (TunnelHalfW * 2f)) * 0.5f + 0.25f;
                    float cheekOffset = TunnelHalfW + cheekW * 0.5f;
                    var leftCheek = CreateBoxLocal($"Vault_PortalCheekL_{i}", vault.transform,
                        faceCenter - faceRight * cheekOffset + Vector3.up * (WalkwayY + upperH * 0.5f),
                        new Vector3(cheekW, upperH + 0.1f, 0.45f), brickMat, true);
                    leftCheek.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                    var rightCheek = CreateBoxLocal($"Vault_PortalCheekR_{i}", vault.transform,
                        faceCenter + faceRight * cheekOffset + Vector3.up * (WalkwayY + upperH * 0.5f),
                        new Vector3(cheekW, upperH + 0.1f, 0.45f), brickMat, true);
                    rightCheek.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                }

                // Massive Concrete Buttress Column rising out of the water at each octagon vertex
                float vRad = (angleDeg + 22.5f) * Mathf.Deg2Rad;
                Vector3 colPos = new Vector3(Mathf.Sin(vRad) * (r - 1.85f), VaultBasinFloorY + totalH * 0.5f, Mathf.Cos(vRad) * (r - 1.85f));
                var pillar = CreateBoxLocal($"Vault_Pier_{i}", vault.transform, colPos, new Vector3(0.85f, totalH + 0.1f, 0.85f), concreteMat, true);
                pillar.transform.localRotation = Quaternion.Euler(0f, angleDeg + 22.5f, 0f);
            }

            // 4. Upper Perimeter Concrete Catwalk Balcony Ring at Y = -3.20m (connects N, S, E, W portals seamlessly!)
            float catwalkInnerR = r - 2.25f; // 5.95m
            for (int i = 0; i < sides; i++)
            {
                float angleDeg = i * 45f;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 faceDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                float segMidR = 0.5f * (r + catwalkInnerR);
                float segLen = 2f * r * Mathf.Tan(Mathf.PI / sides) + 0.45f;

                var deck = CreateBoxLocal($"Vault_BalconyDeck_{i}", vault.transform,
                    new Vector3(faceDir.x * segMidR, WalkwayY - 0.14f, faceDir.z * segMidR),
                    new Vector3(segLen, 0.28f, 2.35f), concreteMat, true);
                deck.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                // Rusted Iron Guardrail on inner lip (skip side 5 = SW where staircase descends!)
                if (i != 5)
                {
                    float railR = catwalkInnerR + 0.08f;
                    float railLen = 2f * catwalkInnerR * Mathf.Tan(Mathf.PI / sides);
                    var topRail = CreateBoxLocal($"Vault_RailTop_{i}", vault.transform,
                        new Vector3(faceDir.x * railR, WalkwayY + 0.98f, faceDir.z * railR),
                        new Vector3(railLen, 0.06f, 0.06f), rustedIronMat, true);
                    topRail.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

                    var midRail = CreateBoxLocal($"Vault_RailMid_{i}", vault.transform,
                        new Vector3(faceDir.x * railR, WalkwayY + 0.52f, faceDir.z * railR),
                        new Vector3(railLen, 0.045f, 0.045f), rustedIronMat, false);
                    midRail.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                }
            }

            // 5. Industrial Metal Staircase with side stringer beams descending from SW Balcony (Y=-3.20m) to Central Island (Y=-6.08m)
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

            // 6. Central Raised Maintenance Plinth Island ("La Guarida" Staging Area — MeshCollider for exact flat top!)
            var island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "Vault_Central_Island_Plinth";
            island.transform.SetParent(vault.transform, false);
            island.transform.localPosition = new Vector3(0f, -6.32f, 0f);
            island.transform.localScale = new Vector3(4.8f, 0.24f, 4.8f);
            island.GetComponent<MeshRenderer>().sharedMaterial = concreteMat;
            // Replace default CapsuleCollider (which domes up +2.4m!) with exact flat MeshCollider
            var cylCap = island.GetComponent<CapsuleCollider>();
            if (cylCap != null) UnityEngine.Object.DestroyImmediate(cylCap);
            var islandMc = island.AddComponent<MeshCollider>();
            islandMc.sharedMesh = island.GetComponent<MeshFilter>().sharedMesh;

            // Huge Corroded Overflow Pipes sticking out of the NW/NE lower walls pouring into the cistern
            for (int p = 0; p < 3; p++)
            {
                float ang = (-45f + p * 45f) * Mathf.Deg2Rad;
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = $"Vault_OverflowPipe_{p}";
                pipe.transform.SetParent(vault.transform, false);
                pipe.transform.localPosition = new Vector3(Mathf.Sin(ang) * (r - 0.6f), -4.85f, Mathf.Cos(ang) * (r - 0.6f));
                pipe.transform.localRotation = Quaternion.Euler(90f, (-45f + p * 45f), 0f);
                pipe.transform.localScale = new Vector3(0.75f, 1.4f, 0.75f);
                pipe.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
                UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());
            }

            // 7. Hanging Industrial Flickering Lamps mounted cleanly on vertical conduit stems from ceiling (-0.58m)
            CreateHangingVaultLamp("VaultFlicker_Light_N", vault.transform, new Vector3(0f, -1.55f, 5.2f), ceilingY, bulbMat, rustedIronMat, 6.5f, 16f);
            CreateHangingVaultLamp("VaultFlicker_Light_S", vault.transform, new Vector3(0f, -1.55f, -5.2f), ceilingY, bulbMat, rustedIronMat, 5.5f, 16f);
            CreateHangingVaultLamp("VaultFlicker_Light_Basin", vault.transform, new Vector3(0f, -3.75f, 0f), ceilingY, bulbMat, rustedIronMat, 5.2f, 14f);

            // Placeholder Anchor Marker for future "Evil" Character Lair (Phase 3+)
            var lairAnchor = new GameObject("EvilCharacter_Lair_SpawnAnchor");
            lairAnchor.transform.SetParent(vault.transform, false);
            lairAnchor.transform.localPosition = new Vector3(0f, -6.08f, 0.4f);
        }

        // =========================================================================
        // BACKROOMS / LA ESQUINA DE CHIHUAS SUBTLE LORE BULKHEAD
        // =========================================================================

        private static void BuildBackroomsLoreBulkhead(Transform root, Vector3 pos, Material darkIronMat, Material rustedIronMat, Material leakGlowMat)
        {
            var go = new GameObject("LoreBulkhead_SectorC0_UnderChihuas");
            go.transform.SetParent(root);
            go.transform.position = pos;

            // Heavy riveted steel bulkhead recessed into the west wall directly beneath La Esquina de Chihuas
            CreateBoxLocal("Bulkhead_Frame", go.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.16f, 2.05f, 1.40f), rustedIronMat, true);
            CreateBoxLocal("Bulkhead_Door", go.transform, new Vector3(0.03f, 1.02f, 0f), new Vector3(0.08f, 1.88f, 1.20f), darkIronMat, true);
            // Sealed valve wheel
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Bulkhead_ValveWheel";
            wheel.transform.SetParent(go.transform, false);
            wheel.transform.localPosition = new Vector3(0.11f, 1.05f, 0.15f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.42f, 0.04f, 0.42f);
            wheel.GetComponent<MeshRenderer>().sharedMaterial = rustedIronMat;
            UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());

            // Subtle eerie yellow-fluorescent hairline light leaking beneath the sealed door seam!
            CreateBoxLocal("Backrooms_SeamLeak", go.transform, new Vector3(0.08f, 0.04f, 0f), new Vector3(0.04f, 0.035f, 1.12f), leakGlowMat, false);
            var glowGo = new GameObject("Backrooms_SeamPointLight");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0.32f, 0.25f, 0f);
            var pl = glowGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.92f, 0.52f);
            pl.intensity = 1.45f;
            pl.range = 3.8f;
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

            // Outer heavy cast-iron collar ring embedded ultra-flush in street asphalt/pavers (~0.84m OD)
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Manhole_RimCollar";
            rim.transform.SetParent(go.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            rim.transform.localScale = new Vector3(0.84f, 0.006f, 0.84f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
            UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());

            // Dark shadow pit disk beneath the lid (visible when lid slides aside!)
            var pitDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pitDisk.name = "Manhole_DarkPitOpening";
            pitDisk.transform.SetParent(go.transform, false);
            pitDisk.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            pitDisk.transform.localScale = new Vector3(0.68f, 0.006f, 0.68f);
            pitDisk.GetComponent<MeshRenderer>().sharedMaterial = darkIronMat;
            UnityEngine.Object.DestroyImmediate(pitDisk.GetComponent<Collider>());

            // Movable Round Cast-Iron Lid with stamped JMAS manhole texture (~0.74m diameter, flush!)
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

            // Underground Vertical Brick Chimney & Iron Ladder directly above undergroundPos!
            var ladderRoot = new GameObject("Underground_LadderShaft");
            ladderRoot.transform.SetParent(parent);
            ladderRoot.transform.position = new Vector3(undergroundPos.x, WalkwayY, undergroundPos.z);

            // Recessed vertical chimney cap rising up into the ceiling
            var shaftCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaftCap.name = "Shaft_ChimneyNeck";
            shaftCap.transform.SetParent(ladderRoot.transform, false);
            shaftCap.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            shaftCap.transform.localScale = new Vector3(0.95f, 0.35f, 0.95f);
            shaftCap.GetComponent<MeshRenderer>().sharedMaterial = brickMat;
            UnityEngine.Object.DestroyImmediate(shaftCap.GetComponent<Collider>());

            // Rusted iron ladder rails & 8 rungs mounted cleanly along the chimney wall
            CreateBoxLocal("Ladder_RailL", ladderRoot.transform, new Vector3(0.45f, 1.35f, -0.22f), new Vector3(0.045f, 2.70f, 0.045f), rustedIronMat, false);
            CreateBoxLocal("Ladder_RailR", ladderRoot.transform, new Vector3(0.45f, 1.35f, +0.22f), new Vector3(0.045f, 2.70f, 0.045f), rustedIronMat, false);
            for (int r = 0; r < 8; r++)
            {
                CreateBoxLocal($"Ladder_Rung_{r}", ladderRoot.transform, new Vector3(0.45f, 0.30f + r * 0.32f, 0f), new Vector3(0.04f, 0.04f, 0.44f), rustedIronMat, false);
            }

            // Wire interactive component
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

            // Surface curbside storm-drain intake slots (authentic horizontal slots right under the curb lip!)
            // + corresponding diagonal volumetric light beams inside the sewer tunnels directly below!
            var slots = new (string name, Vector3 curbPos, float curbYaw, Vector3 tunnelBeamPos, float beamYaw, bool addPaperBoat)[]
            {
                // 1. NW Corner Av. Juarez x Calle Azucenas (iconic "Georgie" corner + folded white paper boat!)
                ("GeorgieSlot_Juarez_Azucenas_NW",
                    new Vector3(-9.80f, 0.09f, -29.10f), 0f,
                    new Vector3(-9.80f, ArchSpringY + 0.35f, AzucenasAxisZ + TunnelHalfW - 0.15f), 180f, true),
                // 2. East Curb Av. Juarez mid-corridor (by Farmacia Juarez / Michoacana)
                ("GeorgieSlot_Juarez_MidCorridor_W",
                    new Vector3(-2.60f, 0.09f, -4.00f), 90f,
                    new Vector3(JuarezAxisX - TunnelHalfW + 0.15f, ArchSpringY + 0.35f, -4.00f), 90f, false),
                // 3. SW Corner Calle Ignacio Mariscal x Azucenas
                ("GeorgieSlot_Mariscal_Azucenas_SW",
                    new Vector3(-54.50f, 0.09f, -23.00f), -90f,
                    new Vector3(MariscalAxisX + TunnelHalfW - 0.15f, ArchSpringY + 0.35f, -23.00f), -90f, false),
                // 4. NW Corner Calle Ignacio Mariscal x Gardenias
                ("GeorgieSlot_Mariscal_Gardenias_NW",
                    new Vector3(-54.50f, 0.09f, 16.00f), -90f,
                    new Vector3(MariscalAxisX + TunnelHalfW - 0.15f, ArchSpringY + 0.35f, 16.00f), -90f, false),
                // 5. Calle Gardenias West (north of Dental Jarquin)
                ("GeorgieSlot_Gardenias_West_N",
                    new Vector3(-26.00f, 0.09f, 24.30f), 180f,
                    new Vector3(-26.00f, ArchSpringY + 0.35f, GardeniasAxisZ - TunnelHalfW + 0.15f), 0f, false),
                // 6. North Trunk Collector breather grate (midway to Rio Bravo)
                ("GeorgieSlot_NorthTrunk_Mid",
                    new Vector3(JuarezAxisX, 0.005f, 118.00f), 0f,
                    new Vector3(JuarezAxisX - TunnelHalfW + 0.15f, ArchSpringY + 0.35f, 118.00f), 90f, false),
            };

            foreach (var s in slots)
            {
                // Surface curbside horizontal storm-drain mouth ("boca de tormenta")
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

                // Underground diagonal intake louver + volumetric light beam + soft daylight spot
                var beamGo = new GameObject(s.name + "_UndergroundLightShaft");
                beamGo.transform.SetParent(slotsRoot);
                beamGo.transform.position = s.tunnelBeamPos;
                beamGo.transform.rotation = Quaternion.Euler(0f, s.beamYaw, 0f);

                // Angled intake recess frame in upper tunnel wall
                CreateBoxLocal("Chute_Frame", beamGo.transform, Vector3.zero, new Vector3(1.25f, 0.34f, 0.22f), darkIronMat, false);

                // Soft feathered diagonal dusty light beam quad streaming onto the damp floor (smooth alpha fade!)
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

                // Subtle cool-daylight spotlight grazing the damp floor
                var spotGo = new GameObject("DrainSlot_SpotLight");
                spotGo.transform.SetParent(beamGo.transform, false);
                spotGo.transform.localPosition = new Vector3(0f, 0.10f, 0.25f);
                spotGo.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                var sl = spotGo.AddComponent<Light>();
                sl.type = LightType.Spot;
                sl.color = new Color(0.84f, 0.92f, 1.00f);
                sl.intensity = 10.5f;
                sl.range = 8.5f;
                sl.spotAngle = 48f;
                sl.innerSpotAngle = 22f;
                sl.shadows = LightShadows.None;

                // Subtle folded white paper boat ("S.S. Georgie" nod) resting gently at the water edge!
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

            // Origami hull (2 angled folded sides + pointed central sail pyramid)
            var hullL = CreateBoxLocal("Hull_Left", boat.transform, new Vector3(0f, 0.03f, 0.045f), new Vector3(0.24f, 0.055f, 0.012f), paperMat, false);
            hullL.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            var hullR = CreateBoxLocal("Hull_Right", boat.transform, new Vector3(0f, 0.03f, -0.045f), new Vector3(0.24f, 0.055f, 0.012f), paperMat, false);
            hullR.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            var sail = CreateBoxLocal("Center_Sail", boat.transform, new Vector3(0f, 0.095f, 0f), new Vector3(0.11f, 0.12f, 0.016f), paperMat, false);
            sail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // =========================================================================
        // RIO BRAVO OUTFLOW HEADWALL, INTERACTIVE GATE & WALKABLE RIVERBANK
        // =========================================================================

        private static void BuildRioBravoOutflowAndRiverbank(Transform root, Material concreteMat, Material mossMat,
            Material rustedIronMat, Material darkIronMat, Material waterMat)
        {
            var outRoot = new GameObject("RioBravo_StormOutflow_Root");
            outRoot.transform.SetParent(root);

            // 1. Surgically split River_BankSouth_C around our 4.6m-wide outflow portal at X = +2.15m
            //    so there is ZERO invisible collider blocking exit onto the Rio Bravo bank!
            GameObject origBank = FindInactiveOrActive("River_BankSouth_C");
            if (origBank != null)
            {
                Material bankMat = origBank.GetComponent<MeshRenderer>()?.sharedMaterial ?? concreteMat;
                Vector3 cPos = origBank.transform.position;   // (-4.73, -1.20, 196.58)
                Quaternion cRot = origBank.transform.rotation;
                Vector3 cScale = origBank.transform.localScale; // (67.85, 0.15, 6.05)

                // Find local X along River_BankSouth_C corresponding to world X = +2.15m
                Vector3 worldRight = cRot * Vector3.right;
                float portalLocalX = Vector3.Dot(new Vector3(JuarezAxisX - cPos.x, 0f, OutflowMouthZ - cPos.z), worldRight);
                float halfSpan = cScale.x * 0.5f; // 33.925m
                float gapHalf = 2.35f;            // 4.70m clean opening for our outflow headwall

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

                // Hide the uncut original slab cleanly (reversible on CleanupPreviousObjects)
                origBank.SetActive(false);
            }

            // 2. Monumental Weathered Concrete Storm Outflow Headwall (X = +2.15m, Z = +195.0m)
            var headwall = new GameObject("Outflow_Headwall_Structure");
            headwall.transform.SetParent(outRoot.transform);
            headwall.transform.position = new Vector3(JuarezAxisX, OutflowWalkwayY, OutflowMouthZ);

            // Thick concrete portal cheek piers & brow lintel
            CreateBoxLocal("Headwall_PierWest", headwall.transform, new Vector3(-2.15f, 1.45f, 0.15f), new Vector3(0.95f, 3.25f, 1.45f), mossMat, true);
            CreateBoxLocal("Headwall_PierEast", headwall.transform, new Vector3(+2.15f, 1.45f, 0.15f), new Vector3(0.95f, 3.25f, 1.45f), mossMat, true);
            CreateBoxLocal("Headwall_BrowLintel", headwall.transform, new Vector3(0f, 2.85f, 0.15f), new Vector3(5.25f, 0.72f, 1.55f), concreteMat, true);

            // Angled concrete retaining wing walls biting cleanly into the split levee bank
            var wingW = CreateBoxLocal("Headwall_WingWallWest", headwall.transform, new Vector3(-3.35f, 1.15f, 1.45f), new Vector3(0.55f, 2.65f, 3.20f), mossMat, true);
            wingW.transform.localRotation = Quaternion.Euler(0f, -26f, 0f);
            var wingE = CreateBoxLocal("Headwall_WingWallEast", headwall.transform, new Vector3(+3.35f, 1.15f, 1.45f), new Vector3(0.55f, 2.65f, 3.20f), mossMat, true);
            wingE.transform.localRotation = Quaternion.Euler(0f, +26f, 0f);

            // 3. Interactive Heavy Rusted Iron Outflow Gate ([E] swings open +108° outward toward Rio Bravo!)
            var gateRoot = new GameObject("RioBravo_Outflow_InteractiveGate");
            gateRoot.transform.SetParent(headwall.transform, false);
            gateRoot.transform.localPosition = new Vector3(0f, 0f, 0.45f);

            var hinge = new GameObject("Gate_HingePivot");
            hinge.transform.SetParent(gateRoot.transform, false);
            hinge.transform.localPosition = new Vector3(-1.68f, 0f, 0f); // Hinged on West pier

            // Gate frame + vertical iron storm bars (width = 3.36m, height = 2.45m)
            CreateBoxLocal("Gate_TopBar", hinge.transform, new Vector3(1.68f, 2.38f, 0f), new Vector3(3.36f, 0.10f, 0.10f), rustedIronMat, true);
            CreateBoxLocal("Gate_MidBar", hinge.transform, new Vector3(1.68f, 1.22f, 0f), new Vector3(3.36f, 0.08f, 0.08f), rustedIronMat, false);
            CreateBoxLocal("Gate_BotBar", hinge.transform, new Vector3(1.68f, 0.18f, 0f), new Vector3(3.36f, 0.09f, 0.09f), rustedIronMat, false);
            for (float gx = 0.14f; gx <= 3.25f; gx += 0.28f)
            {
                CreateBoxLocal($"Gate_Rod_{gx:F2}", hinge.transform, new Vector3(gx, 1.28f, 0f), new Vector3(0.05f, 2.28f, 0.05f), darkIronMat, false);
            }
            // Single clean BoxCollider on the swinging gate leaf so Physics.SyncTransforms() opens passage cleanly!
            var gateCol = new GameObject("Gate_LeafCollider");
            gateCol.transform.SetParent(hinge.transform, false);
            gateCol.transform.localPosition = new Vector3(1.68f, 1.25f, 0f);
            var gbc = gateCol.AddComponent<BoxCollider>();
            gbc.size = new Vector3(3.34f, 2.45f, 0.16f);

            var gateCtrl = gateRoot.AddComponent<SewerOutflowGateController>();
            gateCtrl.gateHingePivot = hinge.transform;
            gateCtrl.closedYaw = 0f;
            gateCtrl.openYaw = -108f; // Swings cleanly outward & west toward wing wall!

            // 4. Walkable Concrete Outfall Apron & South Riverside Promenade along the Rio Bravo under Puente Paso del Norte!
            //    Steps cleanly from OutflowWalkwayY (-2.42m) down to the river bank ledge (Y = -2.62m) right beside the water (Y = -2.50m)!
            CreateBox("RioBravo_Outfall_Apron", outRoot.transform,
                new Vector3(JuarezAxisX, OutflowWalkwayY - 0.20f, OutflowMouthZ + 2.60f),
                new Vector3(5.60f, 0.40f, 5.20f), concreteMat, true);

            // Wide walkable weathered-concrete south bank ledge right along the Rio Bravo under the bridge (X=-10..+18m, Z=200..206m)
            var bankWalk = CreateBox("RioBravo_SouthBank_WalkableLedge", outRoot.transform,
                new Vector3(JuarezAxisX + 1.5f, -2.62f, OutflowMouthZ + 5.80f),
                new Vector3(34.0f, 0.34f, 5.80f), concreteMat, true);
            bankWalk.transform.rotation = Quaternion.Euler(0f, 14.035f, 0f);

            // Shallow drainage runoff stream flowing from the outflow channel straight into Rio Bravo water
            CreateBox("RioBravo_Outflow_RunoffStream", outRoot.transform,
                new Vector3(JuarezAxisX, OutflowWalkwayY + 0.01f, OutflowMouthZ + 4.40f),
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

            Check("Grand Central Cistern Vault & EvilCharacter lair anchor exist",
                GameObject.Find("Central_Cistern_Vault_LaGuarida") != null &&
                GameObject.Find("EvilCharacter_Lair_SpawnAnchor") != null);

            Check("Lore bulkhead under La Esquina de Chihuas & PaperBoat_Georgie exist",
                GameObject.Find("LoreBulkhead_SectorC0_UnderChihuas") != null &&
                GameObject.Find("PaperBoat_Georgie") != null);

            var gate = UnityEngine.Object.FindFirstObjectByType<SewerOutflowGateController>();
            Check("Rio Bravo interactive storm outflow gate & split levee wings exist",
                gate != null &&
                GameObject.Find("River_BankSouth_C_WestSplit") != null &&
                GameObject.Find("River_BankSouth_C_EastSplit") != null);

            // Walkable floor raycasts INSIDE tunnels, Vault, Ramp & Rio Bravo Bank!
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
            Check("Mariscal Collector walkway walkable at (-58.10, 0.0, Y=-3.20m)",
                HitUnderground(MariscalAxisX + 1.15f, 0.0f, -1.8f, -3.30f, -3.10f));
            Check("Central Cistern Vault sunken floor walkable at (2.15, 28.0, Y=-6.08m)",
                HitUnderground(VaultCenterX, VaultCenterZ, -4.5f, -6.25f, -5.95f));
            Check("North Trunk Collector walkway walkable at (3.30, 120.0, Y=-3.20m)",
                HitUnderground(JuarezAxisX + 1.15f, 120.0f, -1.8f, -3.30f, -3.10f));
            Check("Rio Bravo Outflow Apron walkable outside gate at (2.15, 197.5, Y=-2.42m)",
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

            // Temporarily enable underground lights for crisp Editor batchmode verification renders
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

            // Attach a preview flashlight on the verification cam for underground shots!
            var previewSpot = camGo.AddComponent<Light>();
            previewSpot.type = LightType.Spot;
            previewSpot.range = 32f;
            previewSpot.spotAngle = 62f;
            previewSpot.intensity = 16f;
            previewSpot.color = new Color(1.0f, 0.95f, 0.86f);
            previewSpot.shadows = LightShadows.None;

            void Shoot(string filename, Vector3 pos, Vector3 lookAt, bool withFlashlight, Color bgCol, float fov = 62f)
            {
                cam.fieldOfView = fov;
                cam.clearFlags = withFlashlight ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
                cam.backgroundColor = bgCol;
                previewSpot.enabled = withFlashlight;
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

            // 1. Surface Street View verification of Manhole #1 at Av. Benito Juarez x Calle Azucenas!
            Shoot("sewer_01_manhole_juarez_azucenas.png",
                new Vector3(1.80f, 2.15f, -38.50f),
                new Vector3(2.80f, 0.25f, -24.00f),
                withFlashlight: false, bgCol: Color.black, fov: 56f);

            // 2. Underground Collector Tunnel with Georgie Curb Light Shaft + Paper Boat!
            Shoot("sewer_02_tunnel_georgie_lightshaft.png",
                new Vector3(-16.5f, WalkthroughEyeY(), AzucenasAxisZ + 0.95f),
                new Vector3(-8.5f, WalkwayY + 0.85f, AzucenasAxisZ + 0.35f),
                withFlashlight: true, bgCol: new Color(0.03f, 0.05f, 0.055f), fov: 64f);

            // 3. Grand Octagonal Central Cistern Vault ("La Cisterna / El Nudo")
            Shoot("sewer_03_central_cistern_vault.png",
                new Vector3(VaultCenterX + 0.8f, WalkwayY + 1.65f, VaultCenterZ - 7.1f),
                new Vector3(VaultCenterX - 0.5f, -4.80f, VaultCenterZ + 2.0f),
                withFlashlight: true, bgCol: new Color(0.03f, 0.045f, 0.05f), fov: 68f);

            // 4. Rio Bravo Storm Outflow Headwall + Interactive Rusted Iron Gate + Blue Rio Bravo + Bridge above!
            Shoot("sewer_04_rio_bravo_outflow_gate.png",
                new Vector3(JuarezAxisX + 11.5f, -0.35f, OutflowMouthZ + 14.5f),
                new Vector3(JuarezAxisX, -1.25f, OutflowMouthZ + 0.5f),
                withFlashlight: false, bgCol: Color.black, fov: 56f);

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[UndergroundSewerBuilder] Captured 4 Phase-1 verification screenshots.");
        }

        private static float WalkthroughEyeY() => WalkwayY + 1.55f;

        // =========================================================================
        // SHARED PRIMITIVES & PROCEDURAL TEXTURE/MATERIAL GENERATORS
        // =========================================================================

        private static void CreateHangingVaultLamp(string name, Transform parent, Vector3 localPos, float ceilingY,
            Material bulbMat, Material ironMat, float intensity, float range)
        {
            CreateMaintenanceBulkheadLight(name, parent, localPos, bulbMat, ironMat, intensity, range);
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
            float intensity, float range)
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

            var lightGo = new GameObject(name + "_PointLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0.28f, -0.05f, 0f);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.78f, 0.44f);
            pl.intensity = intensity;
            pl.range = range;
            pl.shadows = LightShadows.None;
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
            go.transform.SetParent(parent);
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

            GenerateDampConcreteTex(Path.Combine(dir, "T_Sewer_DampConcrete.png"));
            GenerateBrickArchTex(Path.Combine(dir, "T_Sewer_BrickArch.png"));
            GenerateManholeCoverTex(Path.Combine(dir, "T_Sewer_ManholeCover.png"));
            GenerateLightShaftGradientTex(Path.Combine(dir, "T_Sewer_LightShaftGradient.png"));
            AssetDatabase.Refresh();
            var lti = AssetImporter.GetAtPath("Assets/Textures/Sewer/T_Sewer_LightShaftGradient.png") as TextureImporter;
            if (lti != null && (!lti.alphaIsTransparency || lti.wrapMode != TextureWrapMode.Clamp))
            {
                lti.alphaIsTransparency = true;
                lti.wrapMode = TextureWrapMode.Clamp;
                lti.SaveAndReimport();
            }
        }

        private static void GenerateLightShaftGradientTex(string path)
        {
            int w = 64, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                // Top (y=h-1) brightest, bottom (y=0) fades softly to 0!
                float vy = (float)y / (h - 1);
                float vertFade = Mathf.Pow(vy, 1.35f) * Mathf.SmoothStep(1f, 0.85f, vy);
                for (int x = 0; x < w; x++)
                {
                    float vx = (float)x / (w - 1);
                    float edgeFade = Mathf.Sin(vx * Mathf.PI); // Soft Gaussian-like horizontal edge feather
                    float a = Mathf.Clamp01(vertFade * Mathf.Pow(edgeFade, 1.4f) * 0.34f);
                    tex.SetPixel(x, y, new Color(0.90f, 0.95f, 1.00f, a));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateDampConcreteTex(string path)
        {
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            for (int y = 0; y < sz; y++)
            {
                float vy = (float)y / sz;
                float tide = Mathf.Clamp01(1f - vy * 2.8f); // Darker wet moss stain near bottom
                for (int x = 0; x < sz; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                    float streak = Mathf.PerlinNoise(x * 0.18f, y * 0.025f);
                    float joint = (x % 64 < 2 || y % 64 < 2) ? -0.10f : 0f;
                    float v = Mathf.Clamp01(0.46f + (n - 0.5f) * 0.16f + (streak - 0.5f) * 0.12f + joint - tide * 0.18f);
                    tex.SetPixel(x, y, new Color(v * (0.94f - tide * 0.14f), v * (0.97f - tide * 0.04f), v * (0.91f - tide * 0.16f)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateBrickArchTex(string path)
        {
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGB24, false);
            int rowH = 16, colW = 32;
            for (int y = 0; y < sz; y++)
            {
                int row = y / rowH;
                int offset = (row % 2) * (colW / 2);
                bool hMortar = (y % rowH) < 2;
                for (int x = 0; x < sz; x++)
                {
                    bool vMortar = ((x + offset) % colW) < 2;
                    float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                    if (hMortar || vMortar)
                    {
                        float mv = 0.22f + n * 0.08f;
                        tex.SetPixel(x, y, new Color(mv, mv * 0.98f, mv * 0.92f));
                    }
                    else
                    {
                        float bv = 0.34f + (n - 0.5f) * 0.14f;
                        tex.SetPixel(x, y, new Color(bv * 1.06f, bv * 0.94f, bv * 0.85f));
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
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
                    float v = 0.26f + (n - 0.5f) * 0.08f;

                    // Concentric cast-iron rings + radial grip waffle lugs
                    if (Mathf.Abs(r - 0.92f) < 0.045f || Mathf.Abs(r - 0.62f) < 0.035f || Mathf.Abs(r - 0.28f) < 0.035f)
                        v += 0.12f;
                    else if (r < 0.90f && r > 0.30f)
                    {
                        float spoke = Mathf.Abs(Mathf.Sin(ang * 12f));
                        float ring = Mathf.Abs(Mathf.Sin(r * 26f));
                        if (spoke > 0.72f && ring > 0.65f) v += 0.10f;
                    }
                    // Two pry-bar notch slots on opposite edges
                    if (Mathf.Abs(r - 0.82f) < 0.05f && (Mathf.Abs(d.y) < 5f)) v = 0.08f;

                    tex.SetPixel(x, y, new Color(v * 1.02f, v * 0.98f, v * 0.95f));
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

        private static Material CreateOrGetLightShaftMat(string path, Color color)
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
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
            Texture2D gradTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sewer/T_Sewer_LightShaftGradient.png");
            if (gradTex != null)
            {
                mat.SetTexture("_BaseMap", gradTex);
                mat.mainTexture = gradTex;
            }
            ConfigureURPTransparent(mat);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // Double-sided beam
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
