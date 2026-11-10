using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Constructs the authentic architectural facade, arched canopy, 4 vehicular entry lanes,
    /// 3 toll collection booths, pedestrian plaza, and relocated concierge kiosk for the
    /// Paso del Norte International Bridge (PC-51) in BorderCorridorScene.unity.
    /// Grounded in official photographic reference of the Mexican port of entry on Av. Juárez.
    /// </summary>
    public static class BridgeEntranceBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Rebuild Authentic Bridge Entrance & Relocate Kiosk")]
        public static void RebuildBridgeEntrance()
        {
            Debug.Log("[BridgeEntranceBuilder] Starting Authentic Bridge Entrance Construction...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BridgeEntranceBuilder] Failed to open scene {ScenePath}");
                return;
            }

            // 1. Setup Materials
            Material whiteConcreteMat = CreateOrGetMaterial("Assets/Settings/BridgeWhiteConcrete.mat", new Color(0.95f, 0.95f, 0.96f), 0.15f);
            Material canopyRoofMat = CreateOrGetMaterial("Assets/Settings/CanopyRoof.mat", new Color(0.92f, 0.92f, 0.93f), 0.1f);
            Material bridgeSteelMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.75f, 0.77f, 0.80f), 0.7f);
            Material glassMat = CreateOrGetMaterial("Assets/Settings/GlassMaterial.mat", new Color(0.2f, 0.35f, 0.45f, 0.85f), 0.9f);
            Material asphaltMat = CreateOrGetMaterial("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material concreteMat = CreateOrGetMaterial("Assets/Settings/ConcreteMaterial.mat", new Color(0.68f, 0.67f, 0.64f), 0.1f);
            Material curbMat = CreateOrGetMaterial("Assets/Settings/CurbMaterial.mat", new Color(0.82f, 0.80f, 0.78f), 0.1f);
            Material yellowCurbsMat = CreateOrGetMaterial("Assets/Settings/YellowSafetyCurb.mat", new Color(0.95f, 0.78f, 0.10f), 0.3f);
            Material bollardOrangeMat = CreateOrGetMaterial("Assets/Settings/BollardOrange.mat", new Color(0.95f, 0.42f, 0.05f), 0.4f);
            Material greenSignMat = CreateOrGetMaterial("Assets/Settings/GreenSign.mat", new Color(0.05f, 0.45f, 0.22f), 0.2f);
            Material whiteStripeMat = CreateOrGetMaterial("Assets/Settings/WhiteStripeMaterial.mat", new Color(0.95f, 0.95f, 0.95f), 0.2f);

            // Signboard Material (T_Bridge_Canopy_Sign.png)
            Material canopySignMat = CreateOrGetMaterial("Assets/Settings/BridgeCanopySignMat.mat", Color.white, 0.1f);
            Texture2D signTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/T_Bridge_Canopy_Sign.png");
            if (signTex != null)
            {
                canopySignMat.SetTexture("_BaseMap", signTex);
                canopySignMat.SetColor("_BaseColor", Color.white);
                canopySignMat.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.18f));
                canopySignMat.EnableKeyword("_EMISSION");
            }

            // 2. Locate or create Border_Crossing_Plaza root
            GameObject borderRoot = GameObject.Find("Border_Crossing_Plaza");
            if (borderRoot == null)
            {
                borderRoot = new GameObject("Border_Crossing_Plaza");
            }

            // Remove non-existent overhead gantry with green signs if present
            GameObject overheadGantry = GameObject.Find("Overhead_Highway_Gantry");
            if (overheadGantry != null)
            {
                UnityEngine.Object.DestroyImmediate(overheadGantry);
            }

            // 3. Rebuild Canopy Structure: Bridge_Canopy_PlazaDeCobro
            GameObject oldCanopy = GameObject.Find("Bridge_Canopy_PlazaDeCobro");
            if (oldCanopy != null)
            {
                UnityEngine.Object.DestroyImmediate(oldCanopy);
            }

            GameObject canopy = new GameObject("Bridge_Canopy_PlazaDeCobro");
            canopy.transform.SetParent(borderRoot.transform);
            canopy.transform.position = Vector3.zero;

            // 4. Construct 5 Trumpet / Parabolic Arch Pillars across the 4 entry lanes
            // Shifted by +4.0m to the East to align cleanly with the 2 Av. Juárez through lanes and widened West sidewalk.
            // Pillar X centers: -4.0m (L1 Left), 0.0m (L1/L2), +4.0m (L2/L3 Center), +8.0m (L3/L4), +12.0m (L4 Right)
            float[] pillarXs = new float[] { -4.0f, 0.0f, 4.0f, 8.0f, 12.0f };
            for (int i = 0; i < pillarXs.Length; i++)
            {
                float px = pillarXs[i];
                CreateTrumpetPillar($"Pillar_Arch_{i + 1}", px, 45.0f, canopy.transform, whiteConcreteMat);
            }

            // 5. Construct 4 Vaulted Parabolic Arches spanning between the pillars
            // Span 1: [-4, 0], Span 2: [0, 4], Span 3: [4, 8], Span 4: [8, 12]
            for (int i = 0; i < 4; i++)
            {
                float xLeft = pillarXs[i];
                float xRight = pillarXs[i + 1];
                CreateVaultedArch($"VaultedArch_Lane_{i + 1}", xLeft, xRight, 41.4f, 50.4f, canopy.transform, whiteConcreteMat);
            }

            // 6. Canopy Roof Slab & Front Fascia
            // Continuous roof slab across all 4 arches (Width = 18.4m, Depth = 9.0m flush with arches Z:41.4-50.4m, Top = 7.8m)
            GameObject roofSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofSlab.name = "Canopy_RoofSlab";
            roofSlab.transform.SetParent(canopy.transform);
            roofSlab.transform.position = new Vector3(4.0f, 7.55f, 45.9f);
            roofSlab.transform.localScale = new Vector3(18.4f, 0.5f, 9.0f);
            SetMaterial(roofSlab, canopyRoofMat);

            // Front Fascia Beam (Perimetral white cornice)
            GameObject frontFascia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontFascia.name = "Canopy_FrontFascia";
            frontFascia.transform.SetParent(canopy.transform);
            frontFascia.transform.position = new Vector3(4.0f, 7.35f, 41.35f);
            frontFascia.transform.localScale = new Vector3(18.6f, 1.4f, 0.35f);
            SetMaterial(frontFascia, whiteConcreteMat);

            // Upper Decorative Dentated Cornice Edge (Lip)
            GameObject upperLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperLip.name = "Canopy_UpperCornice";
            upperLip.transform.SetParent(canopy.transform);
            upperLip.transform.position = new Vector3(4.0f, 8.12f, 41.35f);
            upperLip.transform.localScale = new Vector3(18.8f, 0.16f, 0.45f);
            SetMaterial(upperLip, whiteConcreteMat);

            // Monumental Signage Quad ("PC-51 PUENTE INTERNACIONAL PASO DEL NORTE")
            GameObject signQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            signQuad.name = "Canopy_MonumentalSign";
            signQuad.transform.SetParent(canopy.transform);
            signQuad.transform.position = new Vector3(4.0f, 7.35f, 41.15f);
            signQuad.transform.localRotation = Quaternion.identity; // Normal points South (-Z)
            signQuad.transform.localScale = new Vector3(18.2f, 1.22f, 1.0f);
            SetMaterial(signQuad, canopySignMat);
            UnityEngine.Object.DestroyImmediate(signQuad.GetComponent<Collider>());

            // Downlight Fixtures (Row of recessed spotlights along under-fascia)
            GameObject downlightsRoot = new GameObject("Canopy_Downlights");
            downlightsRoot.transform.SetParent(canopy.transform);
            float[] downlightXs = new float[] { -3.2f, -1.5f, 0.2f, 1.9f, 3.6f, 4.4f, 6.1f, 7.8f, 9.5f, 11.2f };
            for (int i = 0; i < downlightXs.Length; i++)
            {
                CreateDownlight($"Downlight_{i + 1}", new Vector3(downlightXs[i], 6.55f, 41.25f), downlightsRoot.transform);
            }

            // 7. 3 Concrete Divider Islands and 3 Toll Collection Booths
            // Between lanes: X = 0.0m, X = 4.0m, X = 8.0m (All 4 arches open to traffic, zero barriers/cones)
            float[] islandXs = new float[] { 0.0f, 4.0f, 8.0f };
            for (int i = 0; i < islandXs.Length; i++)
            {
                float ix = islandXs[i];
                CreateTollIslandAndBooth($"TollStation_{i + 1}", ix, canopy.transform, whiteConcreteMat, yellowCurbsMat, bollardOrangeMat, glassMat, bridgeSteelMat);
            }

            // 7B. Blue Arch / Caseta Numbers (1, 2, 3, 4) on Pillars 1, 2, 3, and 4 (Zero-flicker 3D disjoint relief)
            Material pillarNumberBlueMat = CreateOrGetMaterial("Assets/Settings/PillarNumberBlue.mat", new Color(0.05f, 0.28f, 0.68f), 0.04f);
            Material boothZocaloMat = CreateOrGetMaterial("Assets/Settings/West_DarkZocalo.mat", new Color(0.32f, 0.33f, 0.35f), 0.10f);
            CreateArchPillarBlueNumbers(canopy.transform, whiteConcreteMat, boothZocaloMat, pillarNumberBlueMat);

            // 7C. 4 Toll Barrier Gates ("Plumas de Peaje") across Arcos 1, 2, 3, and 4
            Material barrierWhiteMat = CreateOrGetMaterial("Assets/Settings/BarrierArmWhite.mat", new Color(0.96f, 0.96f, 0.96f), 0.2f);
            Material barrierRedMat = CreateOrGetMaterial("Assets/Settings/BarrierArmRed.mat", new Color(0.85f, 0.12f, 0.14f), 0.2f);
            Material signalGreenMat = CreateOrGetMaterial("Assets/Settings/SignalGreen.mat", new Color(0.12f, 0.88f, 0.24f), 0.1f);
            Material signalRedMat = CreateOrGetMaterial("Assets/Settings/SignalRed.mat", new Color(0.92f, 0.12f, 0.14f), 0.1f);
            CreateArchTollBarrierGates(canopy.transform, yellowCurbsMat, boothZocaloMat, bridgeSteelMat, barrierWhiteMat, barrierRedMat, signalGreenMat, signalRedMat);

            // 8. Fideicomiso Surveillance Camera on South Bridge Facade ("Paso del Norte - Vista Sur")
            CreateFideicomisoFacadeCamera(canopy.transform, whiteConcreteMat, bridgeSteelMat, boothZocaloMat);

            // 9. 4-Lane Roadway Markings under & approaching bridge
            RebuildCorridorLaneMarkings(borderRoot.transform, whiteStripeMat);

            // 9B. Diagonal White Metal Barricades & Yellow Support Posts (SE Corner of Calle Gardenias -> Pillar 4)
            // Closes direct access from Av. Benito Juárez into Arch 4 so Arch 4 is accessed exclusively from Calle Gardenias
            Material whiteBarrierMat = CreateOrGetMaterial("Assets/Settings/WhiteMetalBarricade.mat", new Color(0.94f, 0.95f, 0.96f), 0.15f);
            CreateArch4DiagonalClosureBarriers(borderRoot.transform, whiteBarrierMat, yellowCurbsMat);

            // Ensure continuous dark asphalt for Lane 4 approach (X = 7.0m to 12.10m, Z = 32.4m to 65.0m)
            EnsureBridgeRoadwayAsphalt(borderRoot.transform, asphaltMat);

            // 10. RECONFIGURE PEDESTRIAN PROMENADE & RELOCATE KIOSK (Elena Ramos)
            // Relocate Elena's Kiosk to the East Pedestrian Plaza safely out of vehicular traffic!
            RebuildPedestrianPlazaAndElenaKiosk(borderRoot.transform, concreteMat, curbMat, bridgeSteelMat);

            // 11. Re-align Pedestrian Queue (Mateo, Sofia, Hector)
            RealignPedestrianQueue(borderRoot.transform, bridgeSteelMat, whiteConcreteMat);

            // 12. West Side Administration / Pedestrian Inspection Facade & Ramp
            CreateWestAdminFacadeAndRamp(borderRoot.transform, whiteConcreteMat, bridgeSteelMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BridgeEntranceBuilder] Authentic Bridge Entrance & Relocated Kiosk successfully saved to '{ScenePath}'!");
        }

        #region Helper Geometry Builders

        private static void CreateTrumpetPillar(string name, float x, float z, Transform parent, Material mat)
        {
            GameObject pillarRoot = new GameObject(name);
            pillarRoot.transform.SetParent(parent);
            pillarRoot.transform.position = new Vector3(x, 0f, z);

            // Lower Sturdy Base Shaft (1.0m wide, 3.6m high, 1.4m deep)
            GameObject baseShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseShaft.name = "BaseShaft";
            baseShaft.transform.SetParent(pillarRoot.transform);
            baseShaft.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            baseShaft.transform.localScale = new Vector3(0.95f, 3.6f, 1.4f);
            SetMaterial(baseShaft, mat);

            // Intermediate Flared Trumpet Transition (1)
            GameObject flare1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flare1.name = "Flare_Tier1";
            flare1.transform.SetParent(pillarRoot.transform);
            flare1.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            flare1.transform.localScale = new Vector3(1.35f, 1.2f, 1.45f);
            SetMaterial(flare1, mat);

            // Upper Flared Trumpet Transition (2)
            GameObject flare2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flare2.name = "Flare_Tier2";
            flare2.transform.SetParent(pillarRoot.transform);
            flare2.transform.localPosition = new Vector3(0f, 5.3f, 0f);
            flare2.transform.localScale = new Vector3(2.0f, 1.0f, 1.5f);
            SetMaterial(flare2, mat);

            // Crown Flare Cap (reaching the arch springline)
            GameObject flare3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flare3.name = "Flare_Crown";
            flare3.transform.SetParent(pillarRoot.transform);
            flare3.transform.localPosition = new Vector3(0f, 6.2f, 0f);
            flare3.transform.localScale = new Vector3(2.8f, 0.8f, 1.55f);
            SetMaterial(flare3, mat);
        }

        private static void CreateVaultedArch(string name, float xLeft, float xRight, float zFront, float zBack, Transform parent, Material mat)
        {
            GameObject archRoot = new GameObject(name);
            archRoot.transform.SetParent(parent);
            float midX = (xLeft + xRight) * 0.5f;
            float midZ = (zFront + zBack) * 0.5f;
            float depth = zBack - zFront;
            archRoot.transform.position = new Vector3(midX, 0f, midZ);

            // Parabolic arch apex beam
            GameObject archApex = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archApex.name = "Arch_ApexBeam";
            archApex.transform.SetParent(archRoot.transform);
            archApex.transform.localPosition = new Vector3(0f, 6.75f, 0f);
            archApex.transform.localScale = new Vector3(1.8f, 0.6f, depth);
            SetMaterial(archApex, mat);

            // Left Haunch (Angle upward towards apex)
            GameObject haunchL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            haunchL.name = "Arch_Haunch_L";
            haunchL.transform.SetParent(archRoot.transform);
            haunchL.transform.localPosition = new Vector3(-1.15f, 6.35f, 0f);
            haunchL.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            haunchL.transform.localScale = new Vector3(1.1f, 0.55f, depth);
            SetMaterial(haunchL, mat);

            // Right Haunch (Angle upward towards apex)
            GameObject haunchR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            haunchR.name = "Arch_Haunch_R";
            haunchR.transform.SetParent(archRoot.transform);
            haunchR.transform.localPosition = new Vector3(1.15f, 6.35f, 0f);
            haunchR.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            haunchR.transform.localScale = new Vector3(1.1f, 0.55f, depth);
            SetMaterial(haunchR, mat);

            // Upper Spandrel Wall Fill (above the arch curve to roof)
            GameObject spandrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spandrel.name = "Arch_SpandrelFill";
            spandrel.transform.SetParent(archRoot.transform);
            spandrel.transform.localPosition = new Vector3(0f, 7.15f, (zFront - midZ) + 0.15f);
            spandrel.transform.localScale = new Vector3(xRight - xLeft, 0.5f, 0.35f);
            SetMaterial(spandrel, mat);
        }

        private static void CreateDownlight(string name, Vector3 pos, Transform parent)
        {
            GameObject downlightGo = new GameObject(name);
            downlightGo.transform.SetParent(parent);
            downlightGo.transform.position = pos;

            // Recessed Can housing
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Can_Housing";
            can.transform.SetParent(downlightGo.transform);
            can.transform.localPosition = Vector3.zero;
            can.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
            Material canMat = CreateOrGetMaterial("Assets/Settings/DownlightHousing.mat", new Color(0.3f, 0.3f, 0.3f), 0.8f);
            SetMaterial(can, canMat);
            UnityEngine.Object.DestroyImmediate(can.GetComponent<Collider>());

            // Spot / Point Light
            GameObject lightChild = new GameObject("LightSource");
            lightChild.transform.SetParent(downlightGo.transform);
            lightChild.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            lightChild.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spot = lightChild.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.94f, 0.85f);
            spot.intensity = 3.5f;
            spot.range = 6.2f;
            spot.spotAngle = 60f;
            spot.innerSpotAngle = 30f;
        }

        private static void CreateTollIslandAndBooth(string name, float x, Transform parent, Material concreteMat, Material curbMat, Material bollardMat, Material glassMat, Material steelMat)
        {
            GameObject stationGo = new GameObject(name);
            stationGo.transform.SetParent(parent);
            stationGo.transform.position = new Vector3(x, 0f, 45.0f);

            // 1. Concrete Island Base (0.9m wide, 0.25m high, 7.5m long)
            GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cube);
            island.name = "Island_Platform";
            island.transform.SetParent(stationGo.transform);
            island.transform.localPosition = new Vector3(0f, 0.125f, 0f);
            island.transform.localScale = new Vector3(0.9f, 0.25f, 7.5f);
            SetMaterial(island, concreteMat);

            // High-visibility yellow safety curb edges
            GameObject curbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbL.name = "Curb_Yellow_L";
            curbL.transform.SetParent(stationGo.transform);
            curbL.transform.localPosition = new Vector3(-0.45f, 0.15f, 0f);
            curbL.transform.localScale = new Vector3(0.1f, 0.28f, 7.5f);
            SetMaterial(curbL, curbMat);
            UnityEngine.Object.DestroyImmediate(curbL.GetComponent<Collider>());

            GameObject curbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbR.name = "Curb_Yellow_R";
            curbR.transform.SetParent(stationGo.transform);
            curbR.transform.localPosition = new Vector3(0.45f, 0.15f, 0f);
            curbR.transform.localScale = new Vector3(0.1f, 0.28f, 7.5f);
            SetMaterial(curbR, curbMat);
            UnityEngine.Object.DestroyImmediate(curbR.GetComponent<Collider>());

            // Rounded Nose at South tip (Z = -3.75m local)
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nose.name = "Island_Nose_South";
            nose.transform.SetParent(stationGo.transform);
            nose.transform.localPosition = new Vector3(0f, 0.125f, -3.75f);
            nose.transform.localScale = new Vector3(0.9f, 0.125f, 0.9f);
            SetMaterial(nose, curbMat);

            // Safety Attenuator / Bollards on South Nose
            GameObject bollard1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bollard1.name = "Safety_Bollard_1";
            bollard1.transform.SetParent(stationGo.transform);
            bollard1.transform.localPosition = new Vector3(0f, 0.65f, -3.75f);
            bollard1.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            SetMaterial(bollard1, bollardMat);

            GameObject bollard2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bollard2.name = "Safety_Bollard_2";
            bollard2.transform.SetParent(stationGo.transform);
            bollard2.transform.localPosition = new Vector3(0f, 0.65f, -3.15f);
            bollard2.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            SetMaterial(bollard2, bollardMat);

            // 2. Toll Collection Booth Body split cleanly into lower dark grey zocalo (Y=0.25..1.35) and upper white body (Y=1.35..2.65) with zero overlap
            Material zocaloMat = CreateOrGetMaterial("Assets/Settings/West_DarkZocalo.mat", new Color(0.32f, 0.33f, 0.35f), 0.10f);
            GameObject boothZocalo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boothZocalo.name = "Booth_Base_Zocalo";
            boothZocalo.transform.SetParent(stationGo.transform);
            boothZocalo.transform.localPosition = new Vector3(0f, 0.80f, 0.2f);
            boothZocalo.transform.localScale = new Vector3(0.85f, 1.10f, 2.2f);
            SetMaterial(boothZocalo, zocaloMat);

            GameObject boothBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boothBody.name = "Booth_Body";
            boothBody.transform.SetParent(stationGo.transform);
            boothBody.transform.localPosition = new Vector3(0f, 2.00f, 0.2f);
            boothBody.transform.localScale = new Vector3(0.85f, 1.30f, 2.2f);
            SetMaterial(boothBody, concreteMat);

            // Panoramic Windows (Both sides)
            GameObject windowL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windowL.name = "Window_L";
            windowL.transform.SetParent(stationGo.transform);
            windowL.transform.localPosition = new Vector3(-0.43f, 1.6f, 0.2f);
            windowL.transform.localScale = new Vector3(0.04f, 1.0f, 1.8f);
            SetMaterial(windowL, glassMat);

            GameObject windowR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windowR.name = "Window_R";
            windowR.transform.SetParent(stationGo.transform);
            windowR.transform.localPosition = new Vector3(0.43f, 1.6f, 0.2f);
            windowR.transform.localScale = new Vector3(0.04f, 1.0f, 1.8f);
            SetMaterial(windowR, glassMat);

            // Cashier Ledge / Counter
            GameObject ledgeL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledgeL.name = "Ledge_L";
            ledgeL.transform.SetParent(stationGo.transform);
            ledgeL.transform.localPosition = new Vector3(-0.48f, 1.08f, 0.2f);
            ledgeL.transform.localScale = new Vector3(0.18f, 0.05f, 1.2f);
            SetMaterial(ledgeL, steelMat);

            GameObject ledgeR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledgeR.name = "Ledge_R";
            ledgeR.transform.SetParent(stationGo.transform);
            ledgeR.transform.localPosition = new Vector3(0.48f, 1.08f, 0.2f);
            ledgeR.transform.localScale = new Vector3(0.18f, 0.05f, 1.2f);
            SetMaterial(ledgeR, steelMat);

            // Booth Overhanging Cap Roof
            GameObject boothRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boothRoof.name = "Booth_Roof";
            boothRoof.transform.SetParent(stationGo.transform);
            boothRoof.transform.localPosition = new Vector3(0f, 2.7f, 0.2f);
            boothRoof.transform.localScale = new Vector3(1.15f, 0.12f, 2.5f);
            SetMaterial(boothRoof, concreteMat);
        }

        private static void CreateTelepeajeSign(string name, Vector3 pos, Transform parent, Material signMat, Material postMat)
        {
            GameObject signGo = new GameObject(name);
            signGo.transform.SetParent(parent);
            signGo.transform.position = pos;

            // Green Signboard
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Sign_Board";
            board.transform.SetParent(signGo.transform);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(2.4f, 0.8f, 0.1f);
            SetMaterial(board, signMat);

            // White Inner Border
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "Sign_Border";
            border.transform.SetParent(signGo.transform);
            border.transform.localPosition = new Vector3(0f, 0f, -0.055f);
            border.transform.localScale = new Vector3(2.3f, 0.72f, 0.02f);
            Material borderMat = CreateOrGetMaterial("Assets/Settings/WhiteStripeMaterial.mat", Color.white, 0.2f);
            SetMaterial(border, borderMat);
            UnityEngine.Object.DestroyImmediate(border.GetComponent<Collider>());

            // Inner green plate
            GameObject innerGreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerGreen.name = "Sign_InnerGreen";
            innerGreen.transform.SetParent(signGo.transform);
            innerGreen.transform.localPosition = new Vector3(0f, 0f, -0.065f);
            innerGreen.transform.localScale = new Vector3(2.2f, 0.62f, 0.02f);
            SetMaterial(innerGreen, signMat);
            UnityEngine.Object.DestroyImmediate(innerGreen.GetComponent<Collider>());

            // Suspension Brackets
            GameObject bracket1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket1.name = "Bracket_1";
            bracket1.transform.SetParent(signGo.transform);
            bracket1.transform.localPosition = new Vector3(-0.8f, 0.65f, 0f);
            bracket1.transform.localScale = new Vector3(0.06f, 0.6f, 0.06f);
            SetMaterial(bracket1, postMat);

            GameObject bracket2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket2.name = "Bracket_2";
            bracket2.transform.SetParent(signGo.transform);
            bracket2.transform.localPosition = new Vector3(0.8f, 0.65f, 0f);
            bracket2.transform.localScale = new Vector3(0.06f, 0.6f, 0.06f);
            SetMaterial(bracket2, postMat);
        }

        private static void RebuildCorridorLaneMarkings(Transform parent, Material stripeMat)
        {
            GameObject laneMarkingsGo = GameObject.Find("Lane_Markings_Corridor");
            if (laneMarkingsGo != null)
            {
                UnityEngine.Object.DestroyImmediate(laneMarkingsGo);
            }

            laneMarkingsGo = new GameObject("Lane_Markings_Corridor");
            laneMarkingsGo.transform.SetParent(parent);

            // Dash lines dividing open approach lanes at X = 0.0m and X = 4.0m from Z = 34.0m to Z = 41m
            // Note: X = 8.0m is closed by the diagonal white metal barricades & yellow support posts from Corner_SE to Pillar 4
            float[] dividerXs = new float[] { 0.0f, 4.0f };
            foreach (float dx in dividerXs)
            {
                for (float z = 34.0f; z <= 41f; z += 3.5f)
                {
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dash.name = $"LaneDash_X{dx:F0}_Z{z:F0}";
                    dash.transform.SetParent(laneMarkingsGo.transform);
                    dash.transform.position = new Vector3(dx, 0.015f, z);
                    dash.transform.localScale = new Vector3(0.14f, 0.01f, 1.8f);
                    SetMaterial(dash, stripeMat);
                    UnityEngine.Object.DestroyImmediate(dash.GetComponent<Collider>());
                }
            }
        }

        private static void CreateArchPillarBlueNumbers(Transform canopyParent, Material whiteConcreteMat, Material zocaloMat, Material blueMat)
        {
            GameObject numbersRoot = new GameObject("Arch_Pillar_Numbers_Root");
            numbersRoot.transform.SetParent(canopyParent);
            numbersRoot.transform.position = Vector3.zero;

            // Pillar 1 (X = -4.0m): Add matching upper white caseta front panel above Pillar1_DarkGrey_BaseWrap (Y = 1.575m..2.65m)
            // South face at Z = 44.10m to match TollStation_1..3 South faces
            GameObject p1BoothUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1BoothUpper.name = "Pillar1_Booth_UpperBody";
            p1BoothUpper.transform.SetParent(numbersRoot.transform);
            p1BoothUpper.transform.position = new Vector3(-4.00f, 2.1125f, 44.20f);
            p1BoothUpper.transform.localScale = new Vector3(0.95f, 1.075f, 0.20f);
            SetMaterial(p1BoothUpper, whiteConcreteMat);

            // Positions for the 4 blue numbers (1, 2, 3, 4) on the upper-right of each pillar's South-facing caseta panel:
            // Protruding 4.0cm (0.04m) South of the white wall face so Z-fighting is 100% impossible at any distance or camera movement!
            float[] pillarCenterXs = new float[] { -4.00f, 0.00f, 4.00f, 8.00f };
            for (int i = 0; i < 4; i++)
            {
                int digit = i + 1;
                float px = pillarCenterXs[i];
                // Upper-right of the South white caseta panel (offset +0.16m East of pillar center, Y = 2.16m, South wall face Z = 44.10m)
                Vector3 digitCenter = new Vector3(px + 0.16f, 2.16f, 44.06f);
                CreateDisjoint3DBlueDigit(digit, digitCenter, numbersRoot.transform, blueMat);
            }
        }

        /// <summary>
        /// Builds a bold, crisp 3D relief digit (1, 2, 3, or 4) using strictly disjoint, non-overlapping
        /// 3D rectangular bars that protrude 40mm in front of the background wall, with matte finish
        /// and ShadowCastingMode.Off to guarantee 0% flickering/shimmering during camera movement.
        /// </summary>
        private static void CreateDisjoint3DBlueDigit(int digit, Vector3 centerPos, Transform parent, Material blueMat)
        {
            GameObject digitRoot = new GameObject($"Pillar_BlueNumber_{digit}");
            digitRoot.transform.SetParent(parent);
            digitRoot.transform.position = centerPos;

            float d = 0.06f; // 6cm depth (protrudes 4cm in front of Z=44.10m wall)

            if (digit == 1)
            {
                // Main vertical stem: X in [0.00, +0.09], Y in [-0.22, +0.22]
                AddNonFlickerDigitBar("Stem_V", digitRoot.transform, new Vector3(0.045f, 0.00f, 0f), new Vector3(0.09f, 0.44f, d), blueMat);
                // Top-left beak/serif strictly to the left of Stem_V (X in [-0.075, -0.002]): 0 overlap!
                AddNonFlickerDigitBar("Serif_TopLeft", digitRoot.transform, new Vector3(-0.0385f, 0.14f, 0f), new Vector3(0.073f, 0.08f, d), blueMat);
            }
            else if (digit == 2)
            {
                // 5 strictly disjoint horizontal/vertical bars (zero overlapping faces)
                // 1. Top bar: X in [-0.14, +0.14], Y in [+0.14, +0.22]
                AddNonFlickerDigitBar("Bar_Top", digitRoot.transform, new Vector3(0.00f, 0.18f, 0f), new Vector3(0.28f, 0.08f, d), blueMat);
                // 2. Upper-right vertical: X in [+0.06, +0.14], Y in [+0.04, +0.138]
                AddNonFlickerDigitBar("Stem_UpperRight", digitRoot.transform, new Vector3(0.10f, 0.089f, 0f), new Vector3(0.08f, 0.098f, d), blueMat);
                // 3. Middle bar: X in [-0.14, +0.14], Y in [-0.04, +0.038]
                AddNonFlickerDigitBar("Bar_Mid", digitRoot.transform, new Vector3(0.00f, -0.001f, 0f), new Vector3(0.28f, 0.078f, d), blueMat);
                // 4. Lower-left vertical: X in [-0.14, -0.06], Y in [-0.138, -0.042]
                AddNonFlickerDigitBar("Stem_LowerLeft", digitRoot.transform, new Vector3(-0.10f, -0.09f, 0f), new Vector3(0.08f, 0.096f, d), blueMat);
                // 5. Bottom bar: X in [-0.14, +0.14], Y in [-0.22, -0.14]
                AddNonFlickerDigitBar("Bar_Bottom", digitRoot.transform, new Vector3(0.00f, -0.18f, 0f), new Vector3(0.28f, 0.08f, d), blueMat);
            }
            else if (digit == 3)
            {
                // 5 strictly disjoint bars (zero overlapping faces)
                // 1. Top bar: X in [-0.14, +0.14], Y in [+0.14, +0.22]
                AddNonFlickerDigitBar("Bar_Top", digitRoot.transform, new Vector3(0.00f, 0.18f, 0f), new Vector3(0.28f, 0.08f, d), blueMat);
                // 2. Upper-right vertical: X in [+0.06, +0.14], Y in [+0.04, +0.138]
                AddNonFlickerDigitBar("Stem_UpperRight", digitRoot.transform, new Vector3(0.10f, 0.089f, 0f), new Vector3(0.08f, 0.098f, d), blueMat);
                // 3. Middle bar: X in [-0.10, +0.14], Y in [-0.04, +0.038]
                AddNonFlickerDigitBar("Bar_Mid", digitRoot.transform, new Vector3(0.02f, -0.001f, 0f), new Vector3(0.24f, 0.078f, d), blueMat);
                // 4. Lower-right vertical: X in [+0.06, +0.14], Y in [-0.138, -0.042]
                AddNonFlickerDigitBar("Stem_LowerRight", digitRoot.transform, new Vector3(0.10f, -0.09f, 0f), new Vector3(0.08f, 0.096f, d), blueMat);
                // 5. Bottom bar: X in [-0.14, +0.14], Y in [-0.22, -0.14]
                AddNonFlickerDigitBar("Bar_Bottom", digitRoot.transform, new Vector3(0.00f, -0.18f, 0f), new Vector3(0.28f, 0.08f, d), blueMat);
            }
            else if (digit == 4)
            {
                // 4 strictly disjoint bars (zero overlapping faces)
                // 1. Full right vertical stem: X in [+0.03, +0.11], Y in [-0.22, +0.22]
                AddNonFlickerDigitBar("Stem_RightFull", digitRoot.transform, new Vector3(0.07f, 0.00f, 0f), new Vector3(0.08f, 0.44f, d), blueMat);
                // 2. Left horizontal crossbar (strictly left of Stem_RightFull, X in [-0.14, +0.028]):
                AddNonFlickerDigitBar("Crossbar_Left", digitRoot.transform, new Vector3(-0.056f, -0.03f, 0f), new Vector3(0.168f, 0.08f, d), blueMat);
                // 3. Right horizontal crossbar tip (strictly right of Stem_RightFull, X in [+0.112, +0.152]):
                AddNonFlickerDigitBar("Crossbar_RightTip", digitRoot.transform, new Vector3(0.132f, -0.03f, 0f), new Vector3(0.04f, 0.08f, d), blueMat);
                // 4. Upper-left vertical stem (strictly above Crossbar_Left, Y in [+0.012, +0.22]):
                AddNonFlickerDigitBar("Stem_UpperLeft", digitRoot.transform, new Vector3(-0.10f, 0.116f, 0f), new Vector3(0.08f, 0.208f, d), blueMat);
            }
        }

        private static void AddNonFlickerDigitBar(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent);
            bar.transform.localPosition = localPos;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = localScale;
            var rend = bar.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
            UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
        }

        /// <summary>
        /// Constructs the decorative Fideicomiso de Puentes Fronterizos surveillance camera
        /// ("Paso del Norte - Vista Sur") mounted on the upper South fascia of the bridge canopy
        /// at (X = 2.40m, Y = 7.95m, Z = 41.12m), aimed South at a 28-degree downward pitch toward Av. Benito Juárez.
        /// </summary>
        private static void CreateFideicomisoFacadeCamera(Transform canopyParent, Material whiteHousingMat, Material steelMat, Material darkLensMat)
        {
            GameObject camRoot = new GameObject("Bridge_Fideicomiso_Camera_VistaSur");
            camRoot.transform.SetParent(canopyParent);
            camRoot.transform.position = new Vector3(2.40f, 7.95f, 41.12f);

            // 1. Weatherproof NEMA Junction Box mounted on the South fascia
            GameObject junctionBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            junctionBox.name = "Camera_Junction_Box";
            junctionBox.transform.SetParent(camRoot.transform);
            junctionBox.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            junctionBox.transform.localScale = new Vector3(0.32f, 0.32f, 0.14f);
            SetMaterial(junctionBox, steelMat);
            UnityEngine.Object.DestroyImmediate(junctionBox.GetComponent<Collider>());

            // 2. Cantilever Mounting Arm extending South (-Z) and slightly upward (+Y)
            GameObject armHoriz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armHoriz.name = "Mounting_Arm_Horizontal";
            armHoriz.transform.SetParent(camRoot.transform);
            armHoriz.transform.localPosition = new Vector3(0f, 0.12f, -0.34f);
            armHoriz.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            armHoriz.transform.localScale = new Vector3(0.06f, 0.28f, 0.06f);
            SetMaterial(armHoriz, steelMat);
            UnityEngine.Object.DestroyImmediate(armHoriz.GetComponent<Collider>());

            GameObject armVert = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armVert.name = "Mounting_Knuckle_Vertical";
            armVert.transform.SetParent(camRoot.transform);
            armVert.transform.localPosition = new Vector3(0f, 0.22f, -0.56f);
            armVert.transform.localScale = new Vector3(0.07f, 0.14f, 0.07f);
            SetMaterial(armVert, steelMat);
            UnityEngine.Object.DestroyImmediate(armVert.GetComponent<Collider>());

            // 3. Articulated Camera Housing Head tilted 28 deg downward looking South (Yaw = 180 deg)
            GameObject headPivot = new GameObject("Camera_Head_Pivot");
            headPivot.transform.SetParent(camRoot.transform);
            headPivot.transform.localPosition = new Vector3(0f, 0.34f, -0.58f);
            headPivot.transform.localRotation = Quaternion.Euler(28f, 180f, 0f);

            // Main camera body
            GameObject camBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            camBody.name = "Camera_Body_Housing";
            camBody.transform.SetParent(headPivot.transform);
            camBody.transform.localPosition = Vector3.zero;
            camBody.transform.localScale = new Vector3(0.24f, 0.20f, 0.48f);
            SetMaterial(camBody, whiteHousingMat);
            UnityEngine.Object.DestroyImmediate(camBody.GetComponent<Collider>());

            // Top weather sunshield hood (separated vertically above body to avoid coplanar overlap)
            GameObject sunHood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sunHood.name = "Camera_Sunshield_Hood";
            sunHood.transform.SetParent(headPivot.transform);
            sunHood.transform.localPosition = new Vector3(0f, 0.125f, 0.04f);
            sunHood.transform.localScale = new Vector3(0.28f, 0.04f, 0.56f);
            SetMaterial(sunHood, whiteHousingMat);
            UnityEngine.Object.DestroyImmediate(sunHood.GetComponent<Collider>());

            // Front Optical Lens Barrel (strictly in front of Camera_Body_Housing at local Z > +0.24m)
            GameObject lensBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lensBarrel.name = "Camera_Lens_Barrel";
            lensBarrel.transform.SetParent(headPivot.transform);
            lensBarrel.transform.localPosition = new Vector3(0f, -0.01f, 0.27f);
            lensBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lensBarrel.transform.localScale = new Vector3(0.15f, 0.03f, 0.15f);
            SetMaterial(lensBarrel, darkLensMat);
            UnityEngine.Object.DestroyImmediate(lensBarrel.GetComponent<Collider>());

            // 4. Secret Easter Egg Raycast Target Collider (Trigger so it never blocks physical movement)
            SphereCollider clickTrigger = camRoot.AddComponent<SphereCollider>();
            clickTrigger.center = new Vector3(0f, 0.25f, -0.45f);
            clickTrigger.radius = 0.90f;
            clickTrigger.isTrigger = true;
        }

        /// <summary>
        /// Constructs the diagonal line of white tubular metal barricades and yellow support posts
        /// running from the South-East corner of Calle Gardenias (X=6.95m, Z=24.50m) to the South nose
        /// of Pillar 4 / TollStation_3 (X=8.00m, Z=40.80m), closing direct vehicular entry into Arch 4
        /// from Avenida Benito Juárez so Arch 4 is accessed exclusively from Calle Gardenias.
        /// </summary>
        private static void CreateArch4DiagonalClosureBarriers(Transform parent, Material whiteMetalMat, Material yellowPostMat)
        {
            while (GameObject.Find("Arch4_Diagonal_Closure_Barriers") != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject.Find("Arch4_Diagonal_Closure_Barriers"));
            }

            GameObject barriersRoot = new GameObject("Arch4_Diagonal_Closure_Barriers");
            barriersRoot.transform.SetParent(parent);
            barriersRoot.transform.position = Vector3.zero;

            Vector3 startPt = new Vector3(6.95f, 0.00f, 24.50f); // SE Corner of Calle Gardenias / Av. Juárez curb
            Vector3 endPt = new Vector3(8.00f, 0.00f, 40.80f);   // South nose of Pillar 4 (TollStation_3)
            Vector3 dir = (endPt - startPt).normalized;
            float totalLen = Vector3.Distance(startPt, endPt);
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 westNormal = rot * Vector3.left; // Points West (-X) toward Av. Juárez traffic

            int numBarricades = 8;
            float stepLen = totalLen / numBarricades; // ~2.04m per section
            float frameLen = stepLen - 0.14f;         // ~1.90m clear span between end posts

            for (int i = 0; i < numBarricades; i++)
            {
                float tMid = (i + 0.5f) / numBarricades;
                Vector3 secCenter = Vector3.Lerp(startPt, endPt, tMid);

                GameObject secGo = new GameObject($"White_Metal_Barricade_{i + 1}");
                secGo.transform.SetParent(barriersRoot.transform);
                secGo.transform.position = secCenter;
                secGo.transform.rotation = rot;

                // 1. Two vertical tubular end posts (diameter = 0.055m, height = 1.02m, local Z = ±frameLen*0.5)
                float halfSpan = frameLen * 0.5f;
                CreateBarricadeCylinder("Post_South", secGo.transform, new Vector3(0f, 0.51f, -halfSpan), new Vector3(0.055f, 0.51f, 0.055f), whiteMetalMat);
                CreateBarricadeCylinder("Post_North", secGo.transform, new Vector3(0f, 0.51f,  halfSpan), new Vector3(0.055f, 0.51f, 0.055f), whiteMetalMat);

                // 2. Top and Bottom Horizontal Rails (strictly between the two end posts to avoid overlap)
                float innerRailLen = frameLen - 0.06f;
                CreateBarricadeBox("Rail_Top", secGo.transform, new Vector3(0f, 0.96f, 0f), new Vector3(0.048f, 0.048f, innerRailLen), whiteMetalMat);
                CreateBarricadeBox("Rail_Bottom", secGo.transform, new Vector3(0f, 0.20f, 0f), new Vector3(0.048f, 0.048f, innerRailLen), whiteMetalMat);

                // 3. 7 Vertical Inner Pickets (strictly between Rail_Bottom top Y=0.224 and Rail_Top bottom Y=0.936)
                int numPickets = 7;
                for (int p = 1; p <= numPickets; p++)
                {
                    float zLocal = Mathf.Lerp(-innerRailLen * 0.44f, innerRailLen * 0.44f, (p - 1f) / (numPickets - 1f));
                    CreateBarricadeCylinder($"Picket_{p}", secGo.transform, new Vector3(0f, 0.58f, zLocal), new Vector3(0.028f, 0.35f, 0.028f), whiteMetalMat);
                }

                // 4. Two Yellow Support Posts on the West (traffic-facing) side of each barricade section
                // Positioned at -0.13m local X so they brace the white barricade cleanly without overlapping meshes
                CreateBarricadeCylinder("Yellow_Support_Post_A", secGo.transform,
                    new Vector3(-0.13f, 0.46f, -halfSpan * 0.58f), new Vector3(0.14f, 0.46f, 0.14f), yellowPostMat);
                CreateBarricadeCylinder("Yellow_Support_Post_B", secGo.transform,
                    new Vector3(-0.13f, 0.46f,  halfSpan * 0.58f), new Vector3(0.14f, 0.46f, 0.14f), yellowPostMat);
            }

            // Unified physical collision barrier along the entire diagonal closure line
            GameObject colGo = new GameObject("Arch4_Closure_Collider");
            colGo.transform.SetParent(barriersRoot.transform);
            colGo.transform.position = (startPt + endPt) * 0.5f + new Vector3(0f, 0.55f, 0f);
            colGo.transform.rotation = rot;
            BoxCollider boxCol = colGo.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.35f, 1.10f, totalLen);
        }

        private static void CreateBarricadeCylinder(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.transform.SetParent(parent);
            cyl.transform.localPosition = localPos;
            cyl.transform.localRotation = Quaternion.identity;
            cyl.transform.localScale = localScale;
            SetMaterial(cyl, mat);
            UnityEngine.Object.DestroyImmediate(cyl.GetComponent<Collider>());
        }

        private static void CreateBarricadeBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.localPosition = localPos;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localScale;
            SetMaterial(box, mat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        /// <summary>
        /// Constructs the 4 vehicular toll barrier gates ("plumas de cobro") across Arcos 1, 2, 3, and 4
        /// mounted on the toll islands, with articulated pivots, alternating red/white hazard striped arms,
        /// BoxColliders, and overhead red/green lane status lights.
        /// </summary>
        private static void CreateArchTollBarrierGates(
            Transform parent,
            Material yellowCurbsMat,
            Material darkZocaloMat,
            Material steelMat,
            Material barrierWhiteMat,
            Material barrierRedMat,
            Material signalGreenMat,
            Material signalRedMat)
        {
            while (GameObject.Find("Arch_Toll_Barriers_Root") != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject.Find("Arch_Toll_Barriers_Root"));
            }

            GameObject barriersRoot = new GameObject("Arch_Toll_Barriers_Root");
            barriersRoot.transform.SetParent(parent);
            barriersRoot.transform.position = Vector3.zero;

            barriersRoot.AddComponent<TollBarrierController>();

            // Setup the 4 lanes:
            // Arch 1: Lane 1 (X in [-4.0, 0.0]m, width ~3.1m, cabinet at X = -3.55m, Z = 42.10m)
            // Arch 2: Lane 2 (X in [0.0, 4.0]m, width ~3.1m, cabinet at X = +0.55m, Z = 42.10m)
            // Arch 3: Lane 3 (X in [4.0, 8.0]m, width ~3.1m, cabinet at X = +4.55m, Z = 42.10m)
            // Arch 4: Lane 4 (X in [8.0, 12.0]m, width ~3.1m, cabinet at X = +8.55m, Z = 42.10m)
            float[] cabinetXs = new float[] { -3.55f, 0.55f, 4.55f, 8.55f };
            float[] laneCenterXs = new float[] { -2.00f, 2.00f, 6.00f, 10.00f };

            for (int i = 0; i < 4; i++)
            {
                int archNum = i + 1;
                float cx = cabinetXs[i];
                float midX = laneCenterXs[i];
                float armLength = 3.05f;

                GameObject barrierGo = new GameObject($"Arch{archNum}_TollBarrier_Unit");
                barrierGo.transform.SetParent(barriersRoot.transform);
                barrierGo.transform.position = Vector3.zero;

                // 1. Electromechanical Base Cabinet (Yellow safety prism with dark base zocalo and top cap)
                GameObject cabinetBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabinetBase.name = "Cabinet_Base";
                cabinetBase.transform.SetParent(barrierGo.transform);
                cabinetBase.transform.position = new Vector3(cx, 0.55f, 42.10f);
                cabinetBase.transform.localScale = new Vector3(0.36f, 0.90f, 0.36f);
                SetMaterial(cabinetBase, yellowCurbsMat);

                GameObject cabinetCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabinetCap.name = "Cabinet_Cap";
                cabinetCap.transform.SetParent(barrierGo.transform);
                cabinetCap.transform.position = new Vector3(cx, 1.025f, 42.10f);
                cabinetCap.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
                SetMaterial(cabinetCap, darkZocaloMat);
                UnityEngine.Object.DestroyImmediate(cabinetCap.GetComponent<Collider>());

                // Hinge Bearing (Dark steel cylindrical hub)
                GameObject hinge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hinge.name = "Hinge_Bearing";
                hinge.transform.SetParent(barrierGo.transform);
                hinge.transform.position = new Vector3(cx + 0.12f, 0.95f, 42.10f);
                hinge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                hinge.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
                SetMaterial(hinge, steelMat);
                UnityEngine.Object.DestroyImmediate(hinge.GetComponent<Collider>());

                // 2. Articulated Pivot (Rotation around Z axis: 0 deg = closed horizontal along +X, 85 deg = open vertical)
                GameObject pivotGo = new GameObject($"Arch{archNum}_TollBarrier_Arm_Pivot");
                pivotGo.transform.SetParent(barrierGo.transform);
                pivotGo.transform.position = new Vector3(cx + 0.16f, 0.95f, 42.10f);
                pivotGo.transform.localRotation = Quaternion.Euler(0f, 0f, 85.0f); // Default open!

                // BoxCollider on the pivot that rotates with the arm
                BoxCollider armCollider = pivotGo.AddComponent<BoxCollider>();
                armCollider.center = new Vector3(armLength * 0.5f, 0f, 0f);
                armCollider.size = new Vector3(armLength, 0.14f, 0.06f);

                // 3. Striped Arm Segments (8 alternating non-overlapping white/red blocks, 0% Z-fighting)
                int numSegments = 8;
                float segLen = armLength / numSegments;
                for (int s = 0; s < numSegments; s++)
                {
                    bool isRed = (s % 2 == 1);
                    float segCenterX = (s + 0.5f) * segLen;

                    GameObject segGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segGo.name = $"Arm_Seg_{(isRed ? "Red" : "White")}_{s + 1}";
                    segGo.transform.SetParent(pivotGo.transform);
                    segGo.transform.localPosition = new Vector3(segCenterX, 0f, 0f);
                    segGo.transform.localRotation = Quaternion.identity;
                    segGo.transform.localScale = new Vector3(segLen, 0.11f, 0.045f);
                    SetMaterial(segGo, isRed ? barrierRedMat : barrierWhiteMat);
                    UnityEngine.Object.DestroyImmediate(segGo.GetComponent<Collider>());
                }

                // Black rubber tip bumper
                GameObject tipBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tipBumper.name = "Arm_Tip_Bumper";
                tipBumper.transform.SetParent(pivotGo.transform);
                tipBumper.transform.localPosition = new Vector3(armLength + 0.015f, 0f, 0f);
                tipBumper.transform.localRotation = Quaternion.identity;
                tipBumper.transform.localScale = new Vector3(0.03f, 0.12f, 0.05f);
                SetMaterial(tipBumper, darkZocaloMat);
                UnityEngine.Object.DestroyImmediate(tipBumper.GetComponent<Collider>());

                // 4. Overhead Lane Status Signal Light [ROJO | VERDE] under the arch
                GameObject signalBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                signalBox.name = $"Arch{archNum}_Signal_Housing";
                signalBox.transform.SetParent(barrierGo.transform);
                signalBox.transform.position = new Vector3(midX, 5.80f, 41.25f);
                signalBox.transform.localScale = new Vector3(0.56f, 0.28f, 0.10f);
                SetMaterial(signalBox, darkZocaloMat);
                UnityEngine.Object.DestroyImmediate(signalBox.GetComponent<Collider>());

                // Red light aspect (Left)
                GameObject redLight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                redLight.name = "Signal_Red_Aspect";
                redLight.transform.SetParent(signalBox.transform);
                redLight.transform.localPosition = new Vector3(-0.25f, 0f, -0.52f);
                redLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                redLight.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
                SetMaterial(redLight, signalRedMat);
                UnityEngine.Object.DestroyImmediate(redLight.GetComponent<Collider>());

                // Green light aspect (Right)
                GameObject greenLight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                greenLight.name = "Signal_Green_Aspect";
                greenLight.transform.SetParent(signalBox.transform);
                greenLight.transform.localPosition = new Vector3(0.25f, 0f, -0.52f);
                greenLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                greenLight.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
                SetMaterial(greenLight, signalGreenMat);
                UnityEngine.Object.DestroyImmediate(greenLight.GetComponent<Collider>());
            }
        }

        private static void EnsureBridgeRoadwayAsphalt(Transform parent, Material asphaltMat)
        {
            while (GameObject.Find("Roadway_Bridge_Approach_East") != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject.Find("Roadway_Bridge_Approach_East"));
            }

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Roadway_Bridge_Approach_East";
            road.transform.SetParent(parent);
            road.transform.position = new Vector3(9.55f, -0.05f, 48.70f);
            road.transform.localScale = new Vector3(5.10f, 0.10f, 32.60f);
            SetMaterial(road, asphaltMat);
        }

        private static void RebuildPedestrianPlazaAndElenaKiosk(Transform parent, Material concreteMat, Material curbMat, Material steelMat)
        {
            // 1. Expand East Sidewalk with an extension slab beyond Sidewalk_East_North_Section (X = 17.00m to 23.50m)
            GameObject oldPlaza = GameObject.Find("Plazoleta_Peatonal_PasoDelNorte");
            if (oldPlaza != null) UnityEngine.Object.DestroyImmediate(oldPlaza);

            GameObject plazaGo = new GameObject("Plazoleta_Peatonal_PasoDelNorte");
            plazaGo.transform.SetParent(parent);

            // Extension Slab: spans X from 17.00m to 23.50m (Width = 6.50m, Center = 20.25m), Length = 31.0m (Z from 34.0m to 65.0m)
            // Abuts Sidewalk_East_North_Section (X = 12.10m to 17.00m) cleanly at X = 17.00m with ZERO overlap!
            // Trimmed to start at Z = 34.0m so it NEVER invades Calle Gardenias!
            GameObject plazaSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plazaSlab.name = "Plaza_Pavement_Extension";
            plazaSlab.transform.SetParent(plazaGo.transform);
            plazaSlab.transform.position = new Vector3(20.25f, 0.10f, 49.50f);
            plazaSlab.transform.localScale = new Vector3(6.50f, 0.20f, 31.00f);
            SetMaterial(plazaSlab, concreteMat);

            // Clean up any stray or duplicate plaza guardrails
            for (float z = 35f; z <= 51f; z += 1.0f)
            {
                var strayRail = GameObject.Find($"Plaza_Guardrail_Z{z:F0}");
                if (strayRail != null) UnityEngine.Object.DestroyImmediate(strayRail);
            }

            // Decorative Plaza Planters along eastern perimeter (X = 22.20m) framing the wide plaza
            Material planterMat = CreateOrGetMaterial("Assets/Settings/PlanterConcrete.mat", new Color(0.85f, 0.85f, 0.87f), 0.2f);
            Material foliageMat = CreateOrGetMaterial("Assets/Settings/TreeFoliage.mat", new Color(0.18f, 0.45f, 0.18f), 0.1f);
            float[] planterZs = new float[] { 37f, 42f, 47f };
            foreach (float z in planterZs)
            {
                CreatePlazaPlanter($"Plaza_Planter_Z{z:F0}", new Vector3(22.20f, 0.1f, z), plazaGo.transform, planterMat, foliageMat);
            }

            // 2. RELOCATE KIOSK & ELENA RAMOS TO AUTHENTIC OPEN EAST PLAZA!
            // Positioned at X = 19.20m, Z = 39.50m:
            // - Over 7.0m east of Pillar 5 (X = 12.00m) and 4.0m east of pedestrian queue canopy (X = 15.20m)
            // - Completely separate from bridge facade and toll arches
            // - Rotation: Quaternion.Euler(0f, 0f, 0f) -> Kiosk front desk & sign face SOUTH (-Z) directly greeting arriving pedestrians!
            // - Generous 5.5m wide unobstructed front concourse (Z: 34m-39.5m)
            GameObject kioskGo = GameObject.Find("Border_Information_Kiosk");
            if (kioskGo != null)
            {
                kioskGo.transform.position = new Vector3(19.20f, 0.20f, 39.50f);
                kioskGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // FACES SOUTH!

                // Eliminate Z-fighting on highlight disc by placing it slightly above sidewalk surface (Y = 0.215m)
                var highlight = kioskGo.transform.Find("Highlight_Indicator");
                if (highlight != null)
                {
                    highlight.localPosition = new Vector3(0f, 0.015f, -1.0f);
                    highlight.localScale = new Vector3(1.8f, 0.02f, 1.8f);
                }

                var kioskInteract = kioskGo.GetComponent<InteractionPoint>();
                if (kioskInteract != null)
                {
                    kioskInteract.suggestedPromptsEn = new string[]
                    {
                        "What is the pedestrian wait time right now?",
                        "Is Paso del Norte faster than BOTA?",
                        "Where can I view live bridge cameras?",
                        "What do queue witnesses (Mateo or Sofia) think?"
                    };
                    kioskInteract.suggestedPromptsEs = new string[]
                    {
                        "¿Cuál es el tiempo de espera peatonal ahora?",
                        "¿Paso del Norte está más rápido que Córdova-Américas?",
                        "¿Dónde puedo ver las cámaras en vivo del puente?",
                        "¿Qué opinan Mateo o Sofía (testigos en la fila)?"
                    };
                }
                Debug.Log($"[BridgeEntranceBuilder] Relocated Border_Information_Kiosk facing SOUTH at {kioskGo.transform.position}");
            }
            else
            {
                Debug.LogWarning("[BridgeEntranceBuilder] Border_Information_Kiosk not found to reposition!");
            }
        }

        private static void RealignPedestrianQueue(Transform parent, Material steelMat, Material roofMat)
        {
            GameObject queueArea = GameObject.Find("Pedestrian_Queue_Area");
            if (queueArea == null) return;

            // Realign queue path in authentic East Pedestrian Crossing Concourse centered at X = 15.20m:
            // - Segregated by a 3.10m safety buffer from Pillar 5 (X = 12.00m) and Arch 4 curb (X = 12.10m)
            // - Shaded under authentic pedestrian canopy covering X = [13.90m, 16.50m]
            // - Elevation Y = 0.20m ensures shoes rest flush on sidewalk, eliminating foot Z-fighting
            // Mateo: (15.20f, 0.20f, 42.0f)
            var mateo = GameObject.Find("Witness_MateoNavarro");
            if (mateo != null)
            {
                mateo.transform.position = new Vector3(15.20f, 0.20f, 42.0f);
                mateo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                var hl = mateo.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Sofia: (15.20f, 0.20f, 45.5f)
            var sofia = GameObject.Find("Witness_SofiaAlarcon");
            if (sofia != null)
            {
                sofia.transform.position = new Vector3(15.20f, 0.20f, 45.5f);
                sofia.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                var hl = sofia.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Hector: (15.20f, 0.20f, 49.0f)
            var hector = GameObject.Find("Witness_DonHector");
            if (hector != null)
            {
                hector.transform.position = new Vector3(15.20f, 0.20f, 49.0f);
                hector.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                var hl = hector.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Update Pedestrian Queue Canopy Cover (White architectural concourse canopy)
            var canopyCover = queueArea.transform.Find("Pedestrian_Queue_Canopy");
            if (canopyCover != null)
            {
                canopyCover.position = new Vector3(15.20f, 3.2f, 45.5f);
                canopyCover.localScale = new Vector3(2.6f, 0.12f, 13.0f);
                SetMaterial(canopyCover.gameObject, roofMat);
            }

            // 4 Steel Support Posts for Pedestrian Queue Canopy
            Vector3[] postPositions = new Vector3[]
            {
                new Vector3(14.00f, 1.6f, 39.5f),
                new Vector3(16.40f, 1.6f, 39.5f),
                new Vector3(14.00f, 1.6f, 51.5f),
                new Vector3(16.40f, 1.6f, 51.5f)
            };
            for (int i = 0; i < postPositions.Length; i++)
            {
                string postName = $"Canopy_Support_Post_{i + 1}";
                Transform existingPost = queueArea.transform.Find(postName);
                GameObject postGo = existingPost != null ? existingPost.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postGo.name = postName;
                postGo.transform.SetParent(queueArea.transform);
                postGo.transform.position = postPositions[i];
                postGo.transform.localScale = new Vector3(0.10f, 1.6f, 0.10f);
                SetMaterial(postGo, steelMat);
                var col = postGo.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }

            // Update protective railings without stacking multiple rails at the same Z coordinate:
            // West rail at X = 13.80m (protective barrier separating queue from vehicular curb buffer)
            // East rail at X = 16.50m (queue partition separating queue from Elena's plaza)
            int railIdx = 0;
            for (int z = 36; z <= 51; z += 3)
            {
                var railL = queueArea.transform.Find($"Queue_Rail_L_{z}");
                if (railL != null)
                {
                    railL.position = new Vector3(13.80f, 0.5f, 39.5f + railIdx * 3.0f);
                }

                var railR = queueArea.transform.Find($"Queue_Rail_R_{z}");
                if (railR != null)
                {
                    // Leave south entrance open (Z = 39.5m..41.1m) for pedestrians to join queue:
                    railR.position = new Vector3(16.50f, 0.5f, 42.5f + railIdx * 3.0f);
                }
                railIdx++;
            }

            Debug.Log("[BridgeEntranceBuilder] Realigned Pedestrian Queue & Witnesses (Mateo, Sofia, Hector) safely at X=15.20m under dedicated concourse canopy.");
        }

        private static void CreateWestAdminFacadeAndRamp(Transform parent, Material concreteMat, Material steelMat)
        {
            GameObject oldAdmin = GameObject.Find("AdminBuilding_West_Inspection");
            if (oldAdmin != null)
            {
                // If already built with authentic architecture by CalleGardeniasBuilder, preserve it
                if (oldAdmin.transform.Find("Admin_UpperWalls") != null) return;
                UnityEngine.Object.DestroyImmediate(oldAdmin);
            }

            GameObject adminGo = new GameObject("AdminBuilding_West_Inspection");
            adminGo.transform.SetParent(parent);
            adminGo.transform.position = new Vector3(-12.5f, 0f, 45.0f);

            // Low 1-story administrative office block (White stucco, matching photo)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Building_Block";
            building.transform.SetParent(adminGo.transform);
            building.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            building.transform.localScale = new Vector3(6.5f, 5.0f, 14.0f);
            SetMaterial(building, concreteMat);

            // Blue Official Inspection / Customs Sign
            GameObject blueSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blueSign.name = "Sign_RevisionAduanal";
            blueSign.transform.SetParent(adminGo.transform);
            blueSign.transform.localPosition = new Vector3(3.3f, 3.2f, -4.5f);
            blueSign.transform.localScale = new Vector3(0.1f, 0.9f, 1.8f);
            Material blueSignMat = CreateOrGetMaterial("Assets/Settings/BenavidesBlue.mat", new Color(0.04f, 0.28f, 0.68f), 0.4f);
            SetMaterial(blueSign, blueSignMat);

            // Pedestrian Access Ramp toward Bridge
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Pedestrian_Ramp_Slab";
            ramp.transform.SetParent(adminGo.transform);
            ramp.transform.localPosition = new Vector3(3.2f, 0.4f, 0f);
            ramp.transform.localRotation = Quaternion.Euler(-3.5f, 0f, 0f);
            ramp.transform.localScale = new Vector3(2.2f, 0.3f, 12.0f);
            SetMaterial(ramp, concreteMat);

            // Ramp Handrails
            for (float z = -4f; z <= 4f; z += 3.5f)
            {
                CreateGuardrailSection($"Ramp_Rail_Z{z:F0}", new Vector3(-10.2f, 0.9f, 45.0f + z), adminGo.transform, steelMat);
            }
        }

        private static void CreateGuardrailSection(string name, Vector3 pos, Transform parent, Material mat)
        {
            GameObject railGo = new GameObject(name);
            railGo.transform.SetParent(parent);
            railGo.transform.position = pos;

            // Top rail
            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "TopBar";
            topBar.transform.SetParent(railGo.transform);
            topBar.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            topBar.transform.localScale = new Vector3(0.08f, 0.08f, 3.8f);
            SetMaterial(topBar, mat);

            // Mid rail
            GameObject midBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midBar.name = "MidBar";
            midBar.transform.SetParent(railGo.transform);
            midBar.transform.localPosition = new Vector3(0f, 0.0f, 0f);
            midBar.transform.localScale = new Vector3(0.06f, 0.06f, 3.8f);
            SetMaterial(midBar, mat);

            // Posts
            CreatePost("Post_1", new Vector3(0f, 0f, -1.8f), railGo.transform, mat);
            CreatePost("Post_2", new Vector3(0f, 0f, 0.0f), railGo.transform, mat);
            CreatePost("Post_3", new Vector3(0f, 0f, 1.8f), railGo.transform, mat);
        }

        private static void CreatePost(string name, Vector3 localPos, Transform parent, Material mat)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name;
            post.transform.SetParent(parent);
            post.transform.localPosition = localPos;
            post.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            SetMaterial(post, mat);
        }

        private static void CreatePlazaPlanter(string name, Vector3 pos, Transform parent, Material planterMat, Material foliageMat)
        {
            GameObject planterGo = new GameObject(name);
            planterGo.transform.SetParent(parent);
            planterGo.transform.position = pos;

            // Planter Concrete Box (1.30m x 0.45m x 1.30m)
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Planter_Box";
            box.transform.SetParent(planterGo.transform);
            box.transform.localPosition = new Vector3(0f, 0.225f, 0f);
            box.transform.localScale = new Vector3(1.30f, 0.45f, 1.30f);
            SetMaterial(box, planterMat);

            // Bush / Plant Foliage (neat, manicured urban shrub)
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush_Foliage";
            bush.transform.SetParent(planterGo.transform);
            bush.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            bush.transform.localScale = new Vector3(1.35f, 0.85f, 1.35f);
            SetMaterial(bush, foliageMat);
            UnityEngine.Object.DestroyImmediate(bush.GetComponent<Collider>());
        }

        private static Material CreateOrGetMaterial(string path, Color color, float smoothness)
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
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        #endregion
    }
}

