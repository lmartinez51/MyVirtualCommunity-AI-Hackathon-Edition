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
            // Pillar X centers: -8.0m (L1 Left), -4.0m (L1/L2), 0.0m (L2/L3 Center), +4.0m (L3/L4), +8.0m (L4 Right)
            float[] pillarXs = new float[] { -8.0f, -4.0f, 0.0f, 4.0f, 8.0f };
            for (int i = 0; i < pillarXs.Length; i++)
            {
                float px = pillarXs[i];
                CreateTrumpetPillar($"Pillar_Arch_{i + 1}", px, 45.0f, canopy.transform, whiteConcreteMat);
            }

            // 5. Construct 4 Vaulted Parabolic Arches spanning between the pillars
            // Span 1: [-8, -4], Span 2: [-4, 0], Span 3: [0, 4], Span 4: [4, 8]
            for (int i = 0; i < 4; i++)
            {
                float xLeft = pillarXs[i];
                float xRight = pillarXs[i + 1];
                CreateVaultedArch($"VaultedArch_Lane_{i + 1}", xLeft, xRight, 41.4f, 50.4f, canopy.transform, whiteConcreteMat);
            }

            // 6. Canopy Roof Slab & Front Fascia
            // Continuous roof slab across all 4 arches (Width = 18.4m, Depth = 9.4m, Top = 7.8m)
            GameObject roofSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofSlab.name = "Canopy_RoofSlab";
            roofSlab.transform.SetParent(canopy.transform);
            roofSlab.transform.position = new Vector3(0f, 7.55f, 45.9f);
            roofSlab.transform.localScale = new Vector3(18.4f, 0.5f, 9.6f);
            SetMaterial(roofSlab, canopyRoofMat);

            // Front Fascia Beam (Perimetral white cornice)
            GameObject frontFascia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontFascia.name = "Canopy_FrontFascia";
            frontFascia.transform.SetParent(canopy.transform);
            frontFascia.transform.position = new Vector3(0f, 7.35f, 41.35f);
            frontFascia.transform.localScale = new Vector3(18.6f, 1.4f, 0.35f);
            SetMaterial(frontFascia, whiteConcreteMat);

            // Upper Decorative Dentated Cornice Edge (Lip)
            GameObject upperLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperLip.name = "Canopy_UpperCornice";
            upperLip.transform.SetParent(canopy.transform);
            upperLip.transform.position = new Vector3(0f, 8.12f, 41.35f);
            upperLip.transform.localScale = new Vector3(18.8f, 0.16f, 0.45f);
            SetMaterial(upperLip, whiteConcreteMat);

            // Monumental Signage Quad ("PC-51 PUENTE INTERNACIONAL PASO DEL NORTE")
            GameObject signQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            signQuad.name = "Canopy_MonumentalSign";
            signQuad.transform.SetParent(canopy.transform);
            signQuad.transform.position = new Vector3(0f, 7.35f, 41.16f);
            signQuad.transform.localRotation = Quaternion.identity; // Normal points South (-Z)
            signQuad.transform.localScale = new Vector3(18.2f, 1.22f, 1.0f);
            SetMaterial(signQuad, canopySignMat);
            UnityEngine.Object.DestroyImmediate(signQuad.GetComponent<Collider>());

            // Downlight Fixtures (Row of recessed spotlights along under-fascia)
            GameObject downlightsRoot = new GameObject("Canopy_Downlights");
            downlightsRoot.transform.SetParent(canopy.transform);
            float[] downlightXs = new float[] { -7.2f, -5.5f, -3.8f, -2.1f, -0.4f, 0.4f, 2.1f, 3.8f, 5.5f, 7.2f };
            for (int i = 0; i < downlightXs.Length; i++)
            {
                CreateDownlight($"Downlight_{i + 1}", new Vector3(downlightXs[i], 6.55f, 41.25f), downlightsRoot.transform);
            }

            // 7. 3 Concrete Divider Islands and 3 Toll Collection Booths
            // Between lanes: X = -4.0m, X = 0.0m, X = 4.0m
            float[] islandXs = new float[] { -4.0f, 0.0f, 4.0f };
            for (int i = 0; i < islandXs.Length; i++)
            {
                float ix = islandXs[i];
                CreateTollIslandAndBooth($"TollStation_{i + 1}", ix, canopy.transform, whiteConcreteMat, yellowCurbsMat, bollardOrangeMat, glassMat, bridgeSteelMat);
            }

            // 8. Carril 2 Green Overhead Telepeaje Sign (Suspended under Arch 2 at X = -2.0m)
            CreateTelepeajeSign("Overhead_Telepeaje_Sign", new Vector3(-2.0f, 5.3f, 41.5f), canopy.transform, greenSignMat, bridgeSteelMat);

            // 9. 4-Lane Roadway Markings under & approaching bridge
            RebuildCorridorLaneMarkings(borderRoot.transform, whiteStripeMat);

            // 10. RECONFIGURE PEDESTRIAN PROMENADE & RELOCATE KIOSK (Elena Ramos)
            // Relocate Elena's Kiosk to the East Pedestrian Plaza safely out of vehicular traffic!
            RebuildPedestrianPlazaAndElenaKiosk(borderRoot.transform, whiteConcreteMat, curbMat, bridgeSteelMat);

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
            spot.intensity = 8.0f;
            spot.range = 7.0f;
            spot.spotAngle = 65f;
            spot.innerSpotAngle = 35f;
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

            // 2. Toll Collection Booth Body (0.85m wide, 2.4m high, 2.2m long)
            GameObject boothBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boothBody.name = "Booth_Body";
            boothBody.transform.SetParent(stationGo.transform);
            boothBody.transform.localPosition = new Vector3(0f, 1.45f, 0.2f);
            boothBody.transform.localScale = new Vector3(0.85f, 2.4f, 2.2f);
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

            // 3 Dash lines dividing 4 lanes at X = -4.0m, X = 0.0m, X = 4.0m from Z = 22m to Z = 41m
            float[] dividerXs = new float[] { -4.0f, 0.0f, 4.0f };
            foreach (float dx in dividerXs)
            {
                for (float z = 22f; z <= 41f; z += 3.5f)
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

        private static void RebuildPedestrianPlazaAndElenaKiosk(Transform parent, Material concreteMat, Material curbMat, Material steelMat)
        {
            // 1. Expand East Sidewalk with an extension slab ONLY beyond Sidewalk_East (X = 12.0m to 15.5m)
            // Sidewalk_East already covers X = 7.0m to 12.0m cleanly. Zero overlap = Zero Z-fighting!
            GameObject oldPlaza = GameObject.Find("Plazoleta_Peatonal_PasoDelNorte");
            if (oldPlaza != null) UnityEngine.Object.DestroyImmediate(oldPlaza);

            GameObject plazaGo = new GameObject("Plazoleta_Peatonal_PasoDelNorte");
            plazaGo.transform.SetParent(parent);

            // Extension Slab: Width = 3.5m (X from 12.0m to 15.5m, Center = 13.75m), Length = 22m (Z from 28m to 50m)
            // Abuts Sidewalk_East at X = 12.0m with zero overlap, creating an 8.5m wide continuous promenade!
            GameObject plazaSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plazaSlab.name = "Plaza_Pavement_Extension";
            plazaSlab.transform.SetParent(plazaGo.transform);
            plazaSlab.transform.position = new Vector3(13.75f, 0.1f, 39.0f);
            plazaSlab.transform.localScale = new Vector3(3.5f, 0.2f, 22.0f);
            SetMaterial(plazaSlab, concreteMat);

            // DO NOT create a duplicate curb at X = 6.9m; Curb_East already covers the entire street edge!

            // Protective Pedestrian Guardrails along street curb (X = 7.05m)
            for (float z = 29f; z <= 49f; z += 4.0f)
            {
                CreateGuardrailSection($"Plaza_Guardrail_Z{z:F0}", new Vector3(7.05f, 0.6f, z), plazaGo.transform, steelMat);
            }

            // Decorative Plaza Planters along eastern perimeter (X = 14.8m)
            Material planterMat = CreateOrGetMaterial("Assets/Settings/PlanterConcrete.mat", new Color(0.85f, 0.85f, 0.87f), 0.2f);
            Material foliageMat = CreateOrGetMaterial("Assets/Settings/TreeFoliage.mat", new Color(0.18f, 0.45f, 0.18f), 0.1f);
            for (float z = 31f; z <= 47f; z += 5.0f)
            {
                CreatePlazaPlanter($"Plaza_Planter_Z{z:F0}", new Vector3(14.8f, 0.1f, z), plazaGo.transform, planterMat, foliageMat);
            }

            // 2. RELOCATE KIOSK & ELENA RAMOS FACING SOUTH TOWARDS ARRIVING PEDESTRIANS!
            // Kiosk elevation: Y = 0.20m (flush on top of sidewalk slab Y = 0.20m).
            // Position: (12.2f, 0.20f, 38.0f) on the open pedestrian promenade.
            // Rotation: Quaternion.Euler(0f, 0f, 0f) -> Kiosk front desk & sign face SOUTH (-Z).
            // Elena is stationed behind desk looking SOUTH (-Z) directly at arriving travelers!
            GameObject kioskGo = GameObject.Find("Border_Information_Kiosk");
            if (kioskGo != null)
            {
                kioskGo.transform.position = new Vector3(12.2f, 0.20f, 38.0f);
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

            // Realign queue path centered at X = 9.1m (between sidewalk rail at X = 7.8m and plaza rail at X = 10.4m)
            // Elevation Y = 0.20m ensures witness shoes rest flush on sidewalk, eliminating foot Z-fighting
            // Mateo: (9.1f, 0.20f, 37.5f)
            var mateo = GameObject.Find("Witness_MateoNavarro");
            if (mateo != null)
            {
                mateo.transform.position = new Vector3(9.1f, 0.20f, 37.5f);
                var hl = mateo.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Sofia: (9.1f, 0.20f, 41.5f)
            var sofia = GameObject.Find("Witness_SofiaAlarcon");
            if (sofia != null)
            {
                sofia.transform.position = new Vector3(9.1f, 0.20f, 41.5f);
                var hl = sofia.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Hector: (9.1f, 0.20f, 45.5f)
            var hector = GameObject.Find("Witness_DonHector");
            if (hector != null)
            {
                hector.transform.position = new Vector3(9.1f, 0.20f, 45.5f);
                var hl = hector.transform.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, 0.015f, 0f);
                    hl.localScale = new Vector3(1.3f, 0.02f, 1.3f);
                }
            }

            // Update Pedestrian Queue Canopy Cover (White architectural canopy)
            var canopyCover = queueArea.transform.Find("Pedestrian_Queue_Canopy");
            if (canopyCover != null)
            {
                canopyCover.position = new Vector3(9.1f, 3.2f, 42.0f);
                canopyCover.localScale = new Vector3(2.8f, 0.12f, 14.0f);
                SetMaterial(canopyCover.gameObject, roofMat);
            }

            // Update railings to align with X = 8.0m and X = 10.2m
            for (int z = 36; z <= 48; z += 3)
            {
                var railL = queueArea.transform.Find($"Queue_Rail_L_{z}");
                if (railL != null) railL.position = new Vector3(8.0f, 0.5f, z);

                var railR = queueArea.transform.Find($"Queue_Rail_R_{z}");
                if (railR != null) railR.position = new Vector3(10.2f, 0.5f, z);
            }

            Debug.Log("[BridgeEntranceBuilder] Realigned Pedestrian Queue & Witnesses (Mateo, Sofia, Hector) with clean elevation.");
        }

        private static void CreateWestAdminFacadeAndRamp(Transform parent, Material concreteMat, Material steelMat)
        {
            GameObject oldAdmin = GameObject.Find("AdminBuilding_West_Inspection");
            if (oldAdmin != null) UnityEngine.Object.DestroyImmediate(oldAdmin);

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

            // Planter Concrete Box (1.6m x 0.6m x 1.6m)
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Planter_Box";
            box.transform.SetParent(planterGo.transform);
            box.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            box.transform.localScale = new Vector3(1.6f, 0.6f, 1.6f);
            SetMaterial(box, planterMat);

            // Bush / Plant Foliage
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush_Foliage";
            bush.transform.SetParent(planterGo.transform);
            bush.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            bush.transform.localScale = new Vector3(1.8f, 1.2f, 1.8f);
            SetMaterial(bush, foliageMat);
            UnityEngine.Object.DestroyImmediate(bush.GetComponent<Collider>());
        }

        private static Material CreateOrGetMaterial(string path, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
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

