using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Constructs Calle Gardenias crossing Avenida Benito Juárez (East & West arms)
    /// strictly matching the municipal layout map:
    /// - Angled trajectory matching official layout (East arm slopes Southeast -8 deg, West arm slopes Northwest +16 deg)
    /// - East Arm: 2 lanes (South lane reserved for parallel parking, North lane for vehicular transit heading West)
    /// - West Arm: 2 lanes continuing toward West/Northwest
    /// - 4 Genuine Rounded Corner Radii with solid, smooth curved curb returns and flush sidewalk infill (zero square protrusions, zero invading slabs)
    /// - 4 Recessed Wheelchair Accessible Ramps with yellow tactile warning pavers (ADA/NOM) and side retaining curbs
    /// - Continuous sub-surface foundation collider slab (100% solid walking collision, ZERO void gaps)
    /// - Clean elevation layering: Roadbed (Y=0.00m), Markings (Y=0.008m), Sidewalks (Y=0.20m), zero Z-fighting
    /// - High-visibility continental zebra crosswalks, dashed lane dividers, parallel parking stalls, and directional arrows
    /// </summary>
    public static class CalleGardeniasBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        // Vertical Elevation Hierarchy (eliminates all Z-fighting)
        private const float RoadTopY = 0.00f;
        private const float SidewalkTopY = 0.20f;
        private const float MarkingsY = 0.008f; // 8mm above road surface

        // Corridor Geometry
        private const float AveJuarezEastCurbX = 6.90f;
        private const float AveJuarezWestCurbX = -2.70f; // Outer face of curb is at X = -2.60m, inner sidewalk at X = -2.80m

        // Street Opening Geometry
        // South boundary of Calle Gardenias mouth: Z = 22.00m (Curiosidades Azteca on East, Dental Juárez on West)
        // North boundary of Calle Gardenias mouth: Z = 32.20m (Bridge Promenade on East, West Admin on West)
        // Mouth opening = 10.20m
        private const float MouthSouthZ = 22.00f;
        private const float MouthNorthZ = 32.20f;
        private const float CornerRadius = 1.80f; // 1.8m radius of curvature

        // Wheelchair Crosswalk Corridors
        private const float EastCrosswalkX = 9.50f;
        private const float WestCrosswalkX = -9.50f;
        private const float CrosswalkWidth = 2.40f; // Spans X = [8.30m, 10.70m]

        [MenuItem("MyVirtualCommunity/Build Calle Gardenias")]
        public static void BuildAndVerify()
        {
            BridgeEntranceBuilder.RebuildBridgeEntrance();
            WestSidewalkBlockBuilder.RebuildWestSidewalkBlock();
            BuildCalleGardenias();
            VerifyWalkingColliders();
            CaptureVerificationScreenshots();
            WestSidewalkBlockBuilder.CaptureVerificationScreenshots();
            BridgeEntranceScreenshotSuite.GenerateScreenshots();
        }

        public static void BuildCalleGardenias()
        {
            Debug.Log("[CalleGardeniasBuilder] Constructing authentic Calle Gardenias...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CalleGardeniasBuilder] Failed to open scene {ScenePath}");
                return;
            }

            // 1. Locate Environment_Corridor root
            GameObject envRoot = GameObject.Find("Environment_Corridor");
            if (envRoot == null) envRoot = new GameObject("Environment_Corridor");

            // Clean up any prior Calle Gardenias root
            GameObject oldGardenias = GameObject.Find("Calle_Gardenias_Root");
            if (oldGardenias != null) UnityEngine.Object.DestroyImmediate(oldGardenias);

            GameObject gardeniasRoot = new GameObject("Calle_Gardenias_Root");
            gardeniasRoot.transform.SetParent(envRoot.transform);
            gardeniasRoot.transform.position = Vector3.zero;

            // 2. Setup Materials
            Material asphaltMat = CreateOrGetMaterial("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material concreteMat = CreateOrGetMaterial("Assets/Settings/ConcreteMaterial.mat", new Color(0.68f, 0.67f, 0.64f), 0.1f);
            Material curbMat = CreateOrGetMaterial("Assets/Settings/CurbMaterial.mat", new Color(0.82f, 0.80f, 0.78f), 0.1f);
            Material yellowWarningMat = CreateOrGetMaterial("Assets/Settings/YellowSafetyCurb.mat", new Color(0.95f, 0.78f, 0.10f), 0.35f);
            Material whiteStripeMat = CreateOrGetMaterial("Assets/Settings/WhiteStripeMaterial.mat", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            Material signPostMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.35f, 0.38f, 0.40f), 0.5f);
            Material blueSignMat = CreateOrGetMaterial("Assets/Settings/BenavidesBlue.mat", new Color(0.08f, 0.25f, 0.55f), 0.4f);

            // 3. Adjust Ave. Benito Juárez Sidewalks and Curbs to flush-fit the intersection mouth
            AdjustAveJuarezSidewalksAndCurbs(envRoot.transform, concreteMat, curbMat);
            AdjustAveJuarezRoadwayAndEastParking(envRoot.transform, whiteStripeMat);

            // 4. Construct Continuous Roadbed Across the Intersection (100% solid collision, NO VOID)
            ConstructIntersectionRoadway(gardeniasRoot.transform, asphaltMat, concreteMat, curbMat, yellowWarningMat, blueSignMat, whiteStripeMat);

            // 5. Construct 4 True Rounded Corners (Solid Curved Curbs + Flush Sidewalk Infill)
            ConstructAllCorners(gardeniasRoot.transform, concreteMat, curbMat, yellowWarningMat);

            // 6. Construct 4 Recessed Wheelchair Accessible Curb Ramps with Yellow Tactile Warning Pavers
            ConstructWheelchairRamps(gardeniasRoot.transform, concreteMat, curbMat, yellowWarningMat);

            // 7. Construct Authentic Road Surface Markings
            ConstructMarkingsEast(gardeniasRoot.transform, whiteStripeMat);
            ConstructMarkingsWest(gardeniasRoot.transform, whiteStripeMat);
            ConstructCrosswalks(gardeniasRoot.transform, whiteStripeMat);

            // 8. Corner Nomenclature Signpost ("C. Gardenias / Ave. B. Juarez") moved closer to the South-East corner
            Material greenStreetSignMat = CreateOrGetMaterial("Assets/Settings/StreetSignGreen.mat", new Color(0.05f, 0.40f, 0.20f), 0.25f);
            CreateStreetNomenclaturePost("StreetSign_Gardenias_Juarez", gardeniasRoot.transform,
                new Vector3(7.55f, SidewalkTopY, 23.60f), signPostMat, greenStreetSignMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CalleGardeniasBuilder] Calle Gardenias successfully built and saved to '{ScenePath}'!");
        }

        // --- SIDEWALK & CURB ADJUSTMENTS ON AVE. BENITO JUÁREZ ---

        private static void AdjustAveJuarezSidewalksAndCurbs(Transform envRoot, Material concreteMat, Material curbMat)
        {
            float zSouthCut = MouthSouthZ - CornerRadius; // 20.20m
            float zNorthCut = MouthNorthZ + CornerRadius; // 34.00m

            // --- EAST SIDEWALK ---
            // South Section: Z = -25.0m to 22.00m (Length = 47.00m, Center Z = -1.50m)
            // Stops cleanly at Z = 22.00m (Curiosidades Azteca north wall) to completely eliminate Z-fighting/flickering
            GameObject oldSwEast = GameObject.Find("Sidewalk_East");
            if (oldSwEast != null)
            {
                oldSwEast.transform.position = new Vector3(9.50f, 0.10f, -1.50f);
                oldSwEast.transform.localScale = new Vector3(5.00f, 0.20f, 47.00f);
            }

            // North Section (Bridge Plaza concourse): Z = 34.00m to 65.0m (Length = 31.00m, Center Z = 49.50m)
            // Starts cleanly at Z = 34.00m behind Ramp_Corner_NE with 0 overlap
            // Spans X = 12.10m to 17.00m (Width = 4.90m, Center X = 14.55m), abutting Plaza_Pavement_Extension at X = 17.00m with ZERO overlap!
            GameObject swEastNorth = GameObject.Find("Sidewalk_East_North_Section");
            if (swEastNorth != null) UnityEngine.Object.DestroyImmediate(swEastNorth);
            CreateBox("Sidewalk_East_North_Section", envRoot,
                new Vector3(14.55f, 0.10f, 49.50f), new Vector3(4.90f, 0.20f, 31.00f), concreteMat, keepCollider: true);

            // East Curbs: Z = -25.0m to 24.00m (Length = 49.00m, Center Z = -0.50m)
            // Extends to Z = 24.00m to meet Corner_SE arc at (6.90m, 24.00m) with 0 gap
            GameObject oldCurbEast = GameObject.Find("Curb_East");
            if (oldCurbEast != null)
            {
                oldCurbEast.transform.position = new Vector3(AveJuarezEastCurbX, 0.10f, -0.50f);
                oldCurbEast.transform.localScale = new Vector3(0.20f, 0.20f, 49.00f);
            }

            // East Curbs North Section: Z = 33.80m to 65.0m (Length = 31.20m, Center Z = 49.40m)
            // Shifted to X = 12.10m to meet Pillar_Arch_5 at X = 12.00m with zero gap
            GameObject curbEastNorth = GameObject.Find("Curb_East_North_Section");
            if (curbEastNorth != null) UnityEngine.Object.DestroyImmediate(curbEastNorth);
            CreateBox("Curb_East_North_Section", envRoot,
                new Vector3(12.10f, 0.10f, 49.40f), new Vector3(0.20f, 0.20f, 31.20f), curbMat);

            // --- WEST SIDEWALK (AUTHENTIC WIDENED PEATONAL PROMENADE) ---
            // South Section: Z = -25.0m to 20.20m (Length = 45.20m, Center Z = -2.40m)
            // Widened from 5.0m to 9.20m (spans X = -12.00m building wall to X = -2.80m inner curb)
            // Total sidewalk promenade width from building to street face = 9.40m!
            GameObject oldSwWest = GameObject.Find("Sidewalk_West");
            if (oldSwWest != null)
            {
                oldSwWest.transform.position = new Vector3(-7.40f, 0.10f, -2.40f);
                oldSwWest.transform.localScale = new Vector3(9.20f, 0.20f, 45.20f);
            }

            // North Section: Z = 44.00m to 65.0m (Length = 21.00m, Center Z = 54.50m)
            // Starts north of Pillar_Arch_1 so it never juts south into the NW intersection plaza
            GameObject swWestNorth = GameObject.Find("Sidewalk_West_North_Section");
            if (swWestNorth != null) UnityEngine.Object.DestroyImmediate(swWestNorth);
            CreateBox("Sidewalk_West_North_Section", envRoot,
                new Vector3(-9.50f, 0.10f, 54.50f), new Vector3(5.00f, 0.20f, 21.00f), concreteMat, keepCollider: true);

            // West Curbs: Z = -25.0m to 20.20m (Length = 45.20m, Center Z = -2.40m)
            // Extends to Z = 20.20m to meet Corner_SO organic arc at (-2.60m, 20.20m) with 0 gap
            GameObject oldCurbWest = GameObject.Find("Curb_West");
            if (oldCurbWest != null)
            {
                oldCurbWest.transform.position = new Vector3(AveJuarezWestCurbX, 0.10f, -2.40f);
                oldCurbWest.transform.localScale = new Vector3(0.20f, 0.20f, 45.20f);
            }

            // Re-position the 3 West streetlights to align cleanly along the widened curb at X = -3.00m (40cm setback)
            GameObject lampW1 = GameObject.Find("Streetlight_West_SouthCorner");
            if (lampW1 != null) lampW1.transform.position = new Vector3(-3.00f, SidewalkTopY, -20.50f);
            GameObject lampW2 = GameObject.Find("Streetlight_West_Center");
            if (lampW2 != null) lampW2.transform.position = new Vector3(-3.00f, SidewalkTopY, -2.50f);
            GameObject lampW3 = GameObject.Find("Streetlight_West_NorthCorner");
            if (lampW3 != null) lampW3.transform.position = new Vector3(-3.00f, SidewalkTopY, 19.50f);

            // North Section Curbs: Z = 44.00m to 65.0m (Length = 21.00m, Center Z = 54.50m)
            // Stays north of Pillar_Arch_1 so the NW diagonal plaza meets the bridge pillar cleanly
            GameObject curbWestNorth = GameObject.Find("Curb_West_North_Section");
            if (curbWestNorth != null) UnityEngine.Object.DestroyImmediate(curbWestNorth);
            CreateBox("Curb_West_North_Section", envRoot,
                new Vector3(AveJuarezWestCurbX, 0.10f, 54.50f), new Vector3(0.20f, 0.20f, 21.00f), curbMat);

            // --- CONVERT AND TRIM PLAZA PAVEMENT EXTENSION (ELENA RAMOS PROMENADE) ---
            // Continuous matching concrete sidewalk promenade (X = 17.00m to 23.50m, Z = 34.00m to 65.00m)
            // Abuts Sidewalk_East_North_Section (X = 12.10m to 17.00m) cleanly at X = 17.00m with ZERO overlap!
            GameObject plazaExt = GameObject.Find("Plaza_Pavement_Extension");
            if (plazaExt != null)
            {
                plazaExt.transform.position = new Vector3(20.25f, 0.10f, 49.50f);
                plazaExt.transform.localScale = new Vector3(6.50f, 0.20f, 31.00f);
                SetMaterial(plazaExt, concreteMat);
            }
        }

        // --- AVE. BENITO JUÁREZ ROADWAY MARKINGS & EAST SIDEWALK PARKING STALLS ---

        private static void AdjustAveJuarezRoadwayAndEastParking(Transform envRoot, Material whiteStripeMat)
        {
            // 1. Remove double yellow centerlines along the entirety of Avenida Benito Juárez
            GameObject yellow1 = GameObject.Find("Stripe_Yellow_Center_1");
            if (yellow1 != null) UnityEngine.Object.DestroyImmediate(yellow1);
            GameObject yellow2 = GameObject.Find("Stripe_Yellow_Center_2");
            if (yellow2 != null) UnityEngine.Object.DestroyImmediate(yellow2);

            Transform y1Tr = envRoot.Find("Stripe_Yellow_Center_1");
            if (y1Tr != null) UnityEngine.Object.DestroyImmediate(y1Tr.gameObject);
            Transform y2Tr = envRoot.Find("Stripe_Yellow_Center_2");
            if (y2Tr != null) UnityEngine.Object.DestroyImmediate(y2Tr.gameObject);

            // 2. Remove southern pedestrian crosswalk (Z = -15m zebra stripes)
            for (int i = -10; i <= 10; i++)
            {
                GameObject cross = GameObject.Find($"Crosswalk_Stripe_{i}");
                if (cross != null) UnityEngine.Object.DestroyImmediate(cross);
            }
            List<GameObject> toDestroyCross = new List<GameObject>();
            foreach (Transform child in envRoot)
            {
                if (child.name.StartsWith("Crosswalk_Stripe"))
                {
                    toDestroyCross.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroyCross)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // 3. Remove any prior East Parking Lanes root
            GameObject oldParking = GameObject.Find("Parking_Lanes_East");
            if (oldParking != null) UnityEngine.Object.DestroyImmediate(oldParking);

            // 4. Construct Authentic Parallel Parking Stalls along the East Sidewalk of Av. Benito Juárez
            // Block spans Z = -21.5m (Atención Dental) to Z = +22.0m (Curiosidades Azteca) -> Total 43.50m
            // 6 standard parallel stalls of 5.50m length x 2.40m width (Mexican NOM / Urban Standard)
            // Total parking bay length: 6 * 5.50m = 33.00m
            // Spans Z = -16.50m to Z = +16.50m
            // Corner clearances:
            //   - North corner (Calle Gardenias mouth @ Z = 22.0m): 22.0m - 16.5m = 5.50m buffer
            //   - South corner (South corridor boundary @ Z = -21.5m): -16.5m - (-21.5m) = 5.00m buffer
            // Both satisfy official traffic regulations requiring >= 5.0m setback from intersections/crossings.
            // Width: From East curb face (X = 6.80m) to X = 4.40m (2.40m wide stall).
            // Leaves 11.20m of open roadway (from X = -6.80m to X = +4.40m), perfectly fitting 2 wide circulation lanes.
            GameObject parkingRoot = new GameObject("Parking_Lanes_East");
            parkingRoot.transform.SetParent(envRoot);
            parkingRoot.transform.position = Vector3.zero;

            float curbFaceX = AveJuarezEastCurbX - 0.10f; // 6.80m (curb center is 6.90m, width 0.20m, inner street face is 6.80m)
            float stallWidth = 2.40f;
            float outerLineX = curbFaceX - stallWidth; // 4.40m
            float stallLength = 5.50f;
            int numStalls = 6;
            float totalLength = numStalls * stallLength; // 33.00f
            float startZ = -16.50f;
            float endZ = 16.50f;
            float dividerCenterX = (curbFaceX + outerLineX) * 0.5f; // 5.60f
            float stripeThickness = 0.12f;
            float stripeHeight = 0.012f;

            // Outer continuous longitudinal line separating parking from through-traffic lane
            GameObject longStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            longStripe.name = "Stall_Boundary_Longitudinal";
            longStripe.transform.SetParent(parkingRoot.transform);
            longStripe.transform.position = new Vector3(outerLineX, MarkingsY, (startZ + endZ) * 0.5f);
            longStripe.transform.localScale = new Vector3(stripeThickness, stripeHeight, totalLength);
            SetMaterial(longStripe, whiteStripeMat);
            UnityEngine.Object.DestroyImmediate(longStripe.GetComponent<Collider>());

            // Transverse dividing lines
            for (int i = 0; i <= numStalls; i++)
            {
                float z = startZ + i * stallLength;
                string divName = (i == 0) ? "Stall_EndCap_South" : (i == numStalls ? "Stall_EndCap_North" : $"Stall_Divider_{i}");
                GameObject div = GameObject.CreatePrimitive(PrimitiveType.Cube);
                div.name = divName;
                div.transform.SetParent(parkingRoot.transform);
                div.transform.position = new Vector3(dividerCenterX, MarkingsY, z);
                div.transform.localScale = new Vector3(stallWidth, stripeHeight, stripeThickness);
                SetMaterial(div, whiteStripeMat);
                UnityEngine.Object.DestroyImmediate(div.GetComponent<Collider>());
            }

            Debug.Log($"[CalleGardeniasBuilder] Configured {numStalls} East sidewalk parallel parking stalls (Z = [{startZ:F1}m, {endZ:F1}m]) with 5.5m North setback and 5.0m South setback.");

            // 5. Construct Authentic Dual-Head Parking Meters (Parquímetros Dobles) on the East Sidewalk
            ConstructEastSidewalkParkingMeters(envRoot);
        }

        private static void ConstructEastSidewalkParkingMeters(Transform envRoot)
        {
            // Remove prior parking meters container if present
            GameObject oldMeters = GameObject.Find("Parking_Meters_East");
            if (oldMeters != null) UnityEngine.Object.DestroyImmediate(oldMeters);

            // Shift central streetlight slightly to Z = -2.50m so it doesn't crowd the central meter at Z = 0.0m
            GameObject eastCenterLamp = GameObject.Find("Streetlight_East_BenavidesCenter");
            if (eastCenterLamp != null)
            {
                eastCenterLamp.transform.position = new Vector3(7.35f, SidewalkTopY, -2.50f);
            }

            GameObject metersRoot = new GameObject("Parking_Meters_East");
            metersRoot.transform.SetParent(envRoot);
            metersRoot.transform.position = Vector3.zero;

            // Setup materials for parking meters
            Material ironMat = CreateOrGetMaterial("Assets/Settings/Meter_DarkCastIron.mat",
                new Color(0.18f, 0.20f, 0.22f), 0.35f);
            Material steelMat = CreateOrGetMaterial("Assets/Settings/Meter_GalvanizedSteel.mat",
                new Color(0.68f, 0.70f, 0.72f), 0.60f);
            Material housingMat = CreateOrGetMaterial("Assets/Settings/Meter_HousingGrey.mat",
                new Color(0.42f, 0.45f, 0.48f), 0.45f);
            Material domeMat = CreateOrGetMaterial("Assets/Settings/Meter_DomeGlass.mat",
                new Color(0.85f, 0.92f, 0.95f, 0.85f), 0.85f);
            Material screenMat = CreateOrGetMaterial("Assets/Settings/Meter_ScreenLCD.mat",
                new Color(0.10f, 0.24f, 0.15f), 0.60f);
            Material badgeMat = CreateOrGetMaterial("Assets/Settings/Meter_BadgeStall.mat",
                new Color(0.15f, 0.55f, 0.25f), 0.40f);

            // 3 Dual-Head Parking Meters placed at the 3 shared stall dividing lines:
            // - Meter 1: Z = +11.00m (between Stall 1 and Stall 2)
            // - Meter 2: Z =   0.00m (between Stall 3 and Stall 4, in front of Benavides)
            // - Meter 3: Z = -11.00m (between Stall 5 and Stall 6, in front of Michoacana / Dental)
            // Placed on East Sidewalk at X = 7.25m (45cm setback from curb face at X = 6.80m)
            float meterX = 7.25f;
            float baseY = SidewalkTopY; // 0.20m

            CreateDualHeadParkingMeter("ParkingMeter_Dual_North", metersRoot.transform,
                new Vector3(meterX, baseY, 11.00f), 1, 2, ironMat, steelMat, housingMat, domeMat, screenMat, badgeMat);

            CreateDualHeadParkingMeter("ParkingMeter_Dual_Center", metersRoot.transform,
                new Vector3(meterX, baseY, 0.00f), 3, 4, ironMat, steelMat, housingMat, domeMat, screenMat, badgeMat);

            CreateDualHeadParkingMeter("ParkingMeter_Dual_South", metersRoot.transform,
                new Vector3(meterX, baseY, -11.00f), 5, 6, ironMat, steelMat, housingMat, domeMat, screenMat, badgeMat);

            Debug.Log("[CalleGardeniasBuilder] Successfully placed 3 Dual-Head Parking Meters on East Sidewalk (Z = +11m, 0m, -11m) serving all 6 stalls.");
        }

        private static void CreateDualHeadParkingMeter(string name, Transform parent, Vector3 basePos,
            int stallNorth, int stallSouth, Material ironMat, Material steelMat, Material housingMat, Material domeMat, Material screenMat, Material badgeMat)
        {
            GameObject meterGo = new GameObject(name);
            meterGo.transform.SetParent(parent);
            meterGo.transform.position = basePos;

            // 1. Base Flange (anchor to concrete sidewalk)
            GameObject flange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flange.name = "Flange_Base";
            flange.transform.SetParent(meterGo.transform);
            flange.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            flange.transform.localScale = new Vector3(0.18f, 0.025f, 0.18f);
            SetMaterial(flange, ironMat);
            UnityEngine.Object.DestroyImmediate(flange.GetComponent<Collider>());

            // 2. Galvanized Steel Post Shaft
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Pole_Shaft";
            shaft.transform.SetParent(meterGo.transform);
            shaft.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            shaft.transform.localScale = new Vector3(0.075f, 0.50f, 0.075f);
            SetMaterial(shaft, steelMat);
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

            // 3. Yoke / Dual Mounting Bracket
            GameObject yoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yoke.name = "Yoke_Bracket";
            yoke.transform.SetParent(meterGo.transform);
            yoke.transform.localPosition = new Vector3(0f, 1.08f, 0f);
            yoke.transform.localScale = new Vector3(0.08f, 0.05f, 0.30f);
            SetMaterial(yoke, ironMat);
            UnityEngine.Object.DestroyImmediate(yoke.GetComponent<Collider>());

            // 4. North Meter Head (+Z direction)
            CreateSingleMeterHead($"Head_Stall_{stallNorth}", meterGo.transform, new Vector3(0f, 1.22f, 0.11f),
                isNorth: true, housingMat, domeMat, screenMat, badgeMat, ironMat);

            // 5. South Meter Head (-Z direction)
            CreateSingleMeterHead($"Head_Stall_{stallSouth}", meterGo.transform, new Vector3(0f, 1.22f, -0.11f),
                isNorth: false, housingMat, domeMat, screenMat, badgeMat, ironMat);

            // 6. Unified Physical Collision Capsule
            CapsuleCollider col = meterGo.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.70f, 0f);
            col.radius = 0.14f;
            col.height = 1.40f;
        }

        private static void CreateSingleMeterHead(string name, Transform parent, Vector3 localPos,
            bool isNorth, Material housingMat, Material domeMat, Material screenMat, Material badgeMat, Material ironMat)
        {
            GameObject headGo = new GameObject(name);
            headGo.transform.SetParent(parent);
            headGo.transform.localPosition = localPos;

            // Main Housing Box
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Housing_Body";
            body.transform.SetParent(headGo.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.12f, 0.22f, 0.10f);
            SetMaterial(body, housingMat);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

            // Acrylic/Glass Inspection Dome Cap
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dome.name = "Inspection_Dome";
            dome.transform.SetParent(headGo.transform);
            dome.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            dome.transform.localScale = new Vector3(0.10f, 0.02f, 0.08f);
            SetMaterial(dome, domeMat);
            UnityEngine.Object.DestroyImmediate(dome.GetComponent<Collider>());

            // Display Screen Face (facing street at -X)
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Display_Screen";
            screen.transform.SetParent(headGo.transform);
            screen.transform.localPosition = new Vector3(-0.061f, 0.035f, 0f);
            screen.transform.localScale = new Vector3(0.005f, 0.07f, 0.075f);
            SetMaterial(screen, screenMat);
            UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());

            // Directional Arrow / Stall Indicator Badge (facing street at -X)
            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Stall_Indicator_Badge";
            badge.transform.SetParent(headGo.transform);
            badge.transform.localPosition = new Vector3(-0.061f, -0.045f, 0f);
            badge.transform.localScale = new Vector3(0.005f, 0.04f, 0.065f);
            SetMaterial(badge, badgeMat);
            UnityEngine.Object.DestroyImmediate(badge.GetComponent<Collider>());

            // Coin / Card Payment Slot (facing street at -X)
            GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.name = "Payment_Slot";
            slot.transform.SetParent(headGo.transform);
            slot.transform.localPosition = new Vector3(-0.061f, -0.010f, 0f);
            slot.transform.localScale = new Vector3(0.006f, 0.012f, 0.045f);
            SetMaterial(slot, ironMat);
            UnityEngine.Object.DestroyImmediate(slot.GetComponent<Collider>());
        }

        // --- CONTINUOUS ROADBED & CALLE GARDENIAS ROADWAYS ---

        private static void ConstructIntersectionRoadway(Transform parent, Material asphaltMat, Material concreteMat, Material curbMat, Material yellowWarningMat, Material blueSignMat, Material stripeMat)
        {
            // Continuous Sub-surface Foundation Collider spanning the entire intersection area (X = -35m to +35m, Z = 16m to 52m)
            // Sits at Y = -0.25m with invisible MeshRenderer to guarantee 100% solid walking collision while completely eliminating 504 m2 of coplanar Z-fighting overdraw!
            GameObject foundationSlab = CreateBox("Roadway_Foundation_Slab", parent,
                new Vector3(0f, -0.25f, 34.00f), new Vector3(70f, 0.40f, 36.00f), asphaltMat, keepCollider: true);
            var slabMr = foundationSlab.GetComponent<MeshRenderer>();
            if (slabMr != null) slabMr.enabled = false;

            // Seamless Central Intersection Throat Asphalt (between Ave. Juárez and Calle Gardenias mouths)
            // East throat: spans X = 6.90m to 16.00m, Z = 25.40m to 32.60m (sealed with South ramp lip at Z=25.40m and North ramp lip at Z=32.40m)
            CreateBox("Roadway_Throat_East", parent,
                new Vector3(11.45f, -0.05f, 29.00f), new Vector3(9.10f, 0.10f, 7.20f), asphaltMat, keepCollider: true);

            // Seamless Lane 4 Bridge Approach Asphalt: spans X = 7.00m to 12.10m, Z = 32.40m to 65.00m
            while (GameObject.Find("Roadway_Bridge_Approach_East") != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject.Find("Roadway_Bridge_Approach_East"));
            }
            CreateBox("Roadway_Bridge_Approach_East", parent,
                new Vector3(9.55f, -0.05f, 48.70f), new Vector3(5.10f, 0.10f, 32.60f), asphaltMat, keepCollider: true);

            // Seamless Roadway Asphalt Corner Pads under Corner_NE and Corner_SE curbs:
            // Guarantees zero missing asphalt, zero void, and consistent dark road surface all around the curbs!
            CreateBox("Roadway_Corner_NE_SurfaceAsphalt", parent,
                new Vector3(12.80f, -0.05f, 33.10f), new Vector3(1.40f, 0.10f, 1.40f), asphaltMat, keepCollider: true);
            CreateBox("Roadway_Corner_SE_SurfaceAsphalt", parent,
                new Vector3(7.60f, -0.05f, 24.70f), new Vector3(1.40f, 0.10f, 1.40f), asphaltMat, keepCollider: true);

            // West throat: spans X = -10.70m to -6.90m, Z = 22.00m to 44.00m
            CreateBox("Roadway_Throat_West", parent,
                new Vector3(-8.80f, -0.05f, 33.00f), new Vector3(3.80f, 0.10f, 22.00f), asphaltMat, keepCollider: true);

            // Trajectory of Calle Gardenias East Arm matching the layout map:
            // East arm: slopes downward towards Southeast (+8.0 deg in Unity rotates right toward -Z/South)
            float angleEast = 8.0f;
            Quaternion rotEast = Quaternion.Euler(0f, angleEast, 0f);

            float roadWidth = 7.65f; // Spans cleanly from South curb (Z=23.73m) to North curb (Z=31.46m at X=22.58m)
            float armLength = 24.0f;

            // EAST ARM: Starts cleanly at X = 10.70m (beyond the wheelchair ramp and crosswalk)
            Vector3 posRoadEast = new Vector3(22.58f, -0.05f, 27.23f);
            GameObject roadEast = CreateBox("Roadway_Gardenias_East", parent,
                new Vector3(22.58f, -0.05f, 27.60f), new Vector3(armLength, 0.10f, roadWidth), asphaltMat, keepCollider: true);
            roadEast.transform.rotation = rotEast;

            // --- SIDEWALKS ALONG CALLE GARDENIAS EAST ARM ---
            // South Sidewalk (along Curiosidades Azteca building wall)
            // Trapezoidal sidewalk adapting the angular discrepancy between straight wall (Z=22.0m) and angled curb
            CreateTrapezoidSidewalkPrism("Sidewalk_Gardenias_East_South_Building", parent,
                xWest: 10.70f, xEast: 24.00f, zSouth: 22.00f, zNorthWest: 25.30f, zNorthEast: 23.43f, concreteMat);

            // East extension beyond Curiosidades Azteca (X = 24.00m to 34.60m)
            Vector3 posSwEastExt = posRoadEast + rotEast * new Vector3(6.70f, 0.15f, -4.40f);
            GameObject swEastExt = CreateBox("Sidewalk_Gardenias_East_South_Extension", parent,
                posSwEastExt, new Vector3(10.60f, 0.20f, 2.0f), concreteMat, keepCollider: true);
            swEastExt.transform.rotation = rotEast;

            // South Curb along Calle Gardenias East: starts exactly at (10.70m, 25.40m) at the east edge of the ramp
            Vector3 posCurbEastSouth = new Vector3(22.58f, 0.10f, 23.73f);
            GameObject curbEastSouth = CreateBox("Curb_Gardenias_East_South", parent,
                posCurbEastSouth, new Vector3(armLength, 0.20f, 0.20f), curbMat);
            curbEastSouth.transform.rotation = rotEast;

            // North Curb along Calle Gardenias East: starts exactly at (15.90m, 32.40m) at the east edge of Ramp_Corner_NE
            // Zero step at the ramp and continuous +8 deg trajectory to X = 34.70m!
            Vector3 posCurbEastNorth = new Vector3(25.31f, 0.10f, 31.08f);
            GameObject curbEastNorth = CreateBox("Curb_Gardenias_East_North", parent,
                posCurbEastNorth, new Vector3(19.00f, 0.20f, 0.20f), curbMat);
            curbEastNorth.transform.rotation = rotEast;

            // North Sidewalk Transition (trapezoidal prism connecting north curb to full plaza width at Z = 34.00m, X in [15.90m, 23.50m])
            // Starts cleanly at (15.90m, 32.40m) flush with Ramp_Corner_NE (ZERO step) and spans to X = 23.50m (ZERO notch under planters)!
            CreateNorthTrapezoidSidewalkPrism("Sidewalk_Gardenias_East_North_Transition", parent,
                xWest: 15.90f, xEast: 23.50f, zNorth: 34.00f, zSouthWest: 32.40f, zSouthEast: 31.33f, concreteMat);

            // Sidewalk along Calle Gardenias East North beyond the plaza (X = 23.50m to 34.70m)
            Vector3 posSwEastNorth = new Vector3(29.24f, 0.10f, 31.53f);
            GameObject swEastNorth = CreateBox("Sidewalk_Gardenias_East_North", parent,
                posSwEastNorth, new Vector3(11.30f, 0.20f, 2.0f), concreteMat, keepCollider: true);
            swEastNorth.transform.rotation = rotEast;

            // Non-overlapping SE Corner Sidewalk Infills (Column A & Column B):
            // Column A South Infill: X in [7.00m, 8.30m], Z in [22.00m, 24.00m]
            CreateBox("Corner_SE_SouthInfill", parent,
                new Vector3(7.65f, 0.10f, 23.00f), new Vector3(1.30f, 0.20f, 2.00f), concreteMat, keepCollider: true);

            // Column B Ramp Back Infill: X in [8.30m, 10.70m], Z in [22.00m, 23.60m]
            CreateBox("Corner_SE_RampBackInfill", parent,
                new Vector3(9.50f, 0.10f, 22.80f), new Vector3(2.40f, 0.20f, 1.60f), concreteMat, keepCollider: true);

            // Non-overlapping NE Corner Sidewalk Infill (shifted with Bridge East Promenade to X >= 12.10m):
            // Column A North Infill: X in [12.10m, 13.50m], Z in [33.80m, 34.00m] (abuts Sidewalk_East_North_Section at Z=34.00m with ZERO overlap)
            CreateBox("Corner_NE_NorthInfill", parent,
                new Vector3(12.80f, 0.10f, 33.90f), new Vector3(1.40f, 0.20f, 0.20f), concreteMat, keepCollider: true);

            // --- CALLE GARDENIAS WEST ARM: AUTHENTIC WIDENED ROADWAY & CORNERS ---
            // 1. Widened Roadway West Asphalt Bed (14m-16m wide roadway)
            ConstructWidenedRoadwayWestAndBifurcation(parent, asphaltMat, concreteMat, curbMat, blueSignMat);

            // 2. Continuous Organic Curved Sidewalk & Yellow Safety Curb (South-West: Dental Jarquín)
            ConstructSouthWestOrganicCorner(parent, concreteMat, curbMat, yellowWarningMat);

            // 3. Authentic North-West Corner: Admin Building, Accessibility Ramp, Steps, Organic Yellow Curb & Plaza
            ConstructNorthWestCornerAndAdminBuilding(parent, concreteMat, curbMat, yellowWarningMat, asphaltMat, stripeMat);
        }

        // --- AUTHENTIC WIDENED CALLE GARDENIAS WEST ROADWAY & CENTRAL BIFURCATION ---

        private static void ConstructWidenedRoadwayWestAndBifurcation(Transform parent, Material asphaltMat, Material concreteMat, Material curbMat, Material blueMat)
        {
            GameObject roadRoot = new GameObject("Roadway_Gardenias_West_Root");
            roadRoot.transform.SetParent(parent);
            roadRoot.transform.position = Vector3.zero;

            // 1. Widened Asphalt Roadbed
            // Spans X = -6.90m to -35.00m (Length = 28.10m, Center X = -20.95m)
            // Z spans from south curb (Z ~ 24.00m) to north diagonal boundary (Z = 50.00m), width = 26.00m, Center Z = 37.00m
            CreateBox("Roadway_Gardenias_West_MainAsphalt", roadRoot.transform,
                new Vector3(-20.95f, -0.05f, 37.00f), new Vector3(28.10f, 0.10f, 26.00f), asphaltMat, keepCollider: true);

            // Note: The North Sidewalk, continuous organic yellow curb, and West sidewalk strip
            // are authentically constructed in ConstructNorthWestCornerAndAdminBuilding.
        }

        private static void CreateTelephoneKiosk(string name, Transform parent, Vector3 basePos, Material blueMat)
        {
            GameObject kiosk = new GameObject(name);
            kiosk.transform.SetParent(parent);
            kiosk.transform.position = basePos;

            Material steelMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.40f, 0.42f, 0.45f), 0.7f);
            Material glassMat = CreateOrGetMaterial("Assets/Settings/InteriorDarkMaterial.mat", new Color(0.10f, 0.15f, 0.22f), 0.8f);

            // Steel post
            CreateBox($"{name}_Post", kiosk.transform,
                new Vector3(0f, 0.70f, 0f), new Vector3(0.12f, 1.40f, 0.12f), steelMat);
            // Blue phone canopy / hood
            CreateBox($"{name}_Hood_Top", kiosk.transform,
                new Vector3(0f, 1.90f, 0f), new Vector3(0.72f, 0.08f, 0.65f), blueMat);
            CreateBox($"{name}_Hood_Back", kiosk.transform,
                new Vector3(0f, 1.50f, -0.28f), new Vector3(0.68f, 0.80f, 0.06f), blueMat);
            // Side privacy glass wings
            CreateBox($"{name}_Glass_L", kiosk.transform,
                new Vector3(-0.33f, 1.50f, 0f), new Vector3(0.04f, 0.75f, 0.55f), glassMat);
            CreateBox($"{name}_Glass_R", kiosk.transform,
                new Vector3(0.33f, 1.50f, 0f), new Vector3(0.04f, 0.75f, 0.55f), glassMat);
            // Dial box
            CreateBox($"{name}_DialBox", kiosk.transform,
                new Vector3(0f, 1.40f, -0.20f), new Vector3(0.30f, 0.40f, 0.12f), steelMat);
        }

        // --- AUTHENTIC ORGANIC SOUTH-WEST CORNER (DENTAL JARQUÍN) ---

        private static void ConstructSouthWestOrganicCorner(Transform parent, Material concreteMat, Material curbMat, Material yellowWarningMat)
        {
            GameObject cornerRoot = new GameObject("Corner_SO_Organic_Root");
            cornerRoot.transform.SetParent(parent);
            cornerRoot.transform.position = Vector3.zero;

            // 1. Organic Curved Yellow Safety Curb
            // Arc Section: sweeps from widened Juárez curb street face (-2.60m, 20.20m) to Gardenias frontage (-6.70m, 24.30m)
            // Center: (-6.70m, 20.20m), Radius R_outer = 4.10m, Angles: 0 deg -> 90 deg
            // Matches widened Curb_West outer face (X=-2.60m) and inner face (X=-2.80m) with 100% precision!
            float arcCenterX = -6.70f;
            float arcCenterZ = 20.20f;
            float arcRadiusOuter = 4.10f;
            int arcSegments = 16;
            float stepDeg = 90.0f / arcSegments;

            List<Vector3> curbOuterPoints = new List<Vector3>();

            // Generate Arc Points (0 deg to 90 deg)
            for (int i = 0; i <= arcSegments; i++)
            {
                float rad = (i * stepDeg) * Mathf.Deg2Rad;
                float x = arcCenterX + arcRadiusOuter * Mathf.Cos(rad);
                float z = arcCenterZ + arcRadiusOuter * Mathf.Sin(rad);
                curbOuterPoints.Add(new Vector3(x, 0.10f, z));
            }

            // Generate Frontage Points along Calle Gardenias westward to X = -34.0m
            // Straight, clean 3D curb frontage along Dental Jarquín north facade (Z = 24.30m)
            int westSegments = 16;
            float xStartWest = -6.70f;
            float xEndWest = -34.00f;
            float zWest = 24.30f;

            for (int i = 1; i <= westSegments; i++)
            {
                float t = (float)i / westSegments;
                float x = Mathf.Lerp(xStartWest, xEndWest, t);
                curbOuterPoints.Add(new Vector3(x, 0.10f, zWest));
            }

            // 1. Continuous Organic Curved Yellow Safety Curb with 3D Beveled Profile, Smooth ADA Ramp Dips, & ZERO Z-Fighting
            ConstructDentalJarquinCurvedYellowCurb("Dental_YellowCurb_Organic", cornerRoot.transform, curbOuterPoints, yellowWarningMat);

            // 2. Continuous 3D Manifold Sidewalk Mesh with smooth ramp slope & flared wings (Sidewalk_DentalJarquin_Organic)
            ConstructDentalJarquinSidewalkMesh("Sidewalk_DentalJarquin_Organic", cornerRoot.transform, curbOuterPoints, concreteMat);
        }

        private static void ConstructDentalJarquinCurvedYellowCurb(string name, Transform parent, List<Vector3> curbOuterPoints, Material yellowMat)
        {
            GameObject curbGo = new GameObject(name);
            curbGo.transform.SetParent(parent);
            curbGo.transform.position = Vector3.zero;

            MeshFilter mf = curbGo.AddComponent<MeshFilter>();
            MeshRenderer mr = curbGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = yellowMat;

            Mesh mesh = new Mesh { name = $"{name}_Mesh" };

            int count = curbOuterPoints.Count;
            float arcCenterX = -6.70f;
            float arcCenterZ = 20.20f;
            float curbThick = 0.20f;

            // Height profile along the curb (smooth dip for wheelchair ramp at indices 10..14)
            float[] curbHeights = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (i <= 9 || i >= 15)
                {
                    curbHeights[i] = 0.20f; // Full curb height (20cm above street)
                }
                else if (i == 10 || i == 14)
                {
                    curbHeights[i] = 0.11f; // Smooth transitional flare
                }
                else
                {
                    curbHeights[i] = 0.025f; // Lowered flush ADA street lip
                }
            }

            List<Vector3> outerPts = new List<Vector3>();
            List<Vector3> innerPts = new List<Vector3>();
            List<Vector2> inDirs = new List<Vector2>();

            for (int i = 0; i < count; i++)
            {
                Vector3 p = curbOuterPoints[i];
                outerPts.Add(new Vector3(p.x, 0f, p.z));

                Vector2 dir;
                if (i <= 16)
                {
                    dir = (new Vector2(arcCenterX, arcCenterZ) - new Vector2(p.x, p.z)).normalized;
                }
                else
                {
                    dir = new Vector2(0f, -1f);
                }
                inDirs.Add(dir);
                innerPts.Add(new Vector3(p.x + dir.x * curbThick, 0f, p.z + dir.y * curbThick));
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float yBot = -0.02f; // Extended 2cm below asphalt for a 100% gapless seal

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 o1 = outerPts[i];
                Vector3 o2 = outerPts[i + 1];
                Vector3 in1 = innerPts[i];
                Vector3 in2 = innerPts[i + 1];
                Vector2 d1 = inDirs[i];
                Vector2 d2 = inDirs[i + 1];

                float h1 = curbHeights[i];
                float h2 = curbHeights[i + 1];

                // Dynamic chamfer / bevel size: 2.5cm on full curb, scaling smoothly down at ramp mouth
                float b1 = Mathf.Min(0.025f, h1 * 0.20f);
                float b2 = Mathf.Min(0.025f, h2 * 0.20f);

                // 10 profile vertices per segment quad (5 at profile 1, 5 at profile 2)
                // Profile 1:
                Vector3 v1_botOut = new Vector3(o1.x, yBot, o1.z);
                Vector3 v1_topOut = new Vector3(o1.x, h1 - b1, o1.z);
                Vector3 v1_topBev = new Vector3(o1.x + d1.x * b1, h1, o1.z + d1.y * b1);
                Vector3 v1_topIn  = new Vector3(in1.x, h1, in1.z);
                Vector3 v1_botIn  = new Vector3(in1.x, yBot, in1.z);

                // Profile 2:
                Vector3 v2_botOut = new Vector3(o2.x, yBot, o2.z);
                Vector3 v2_topOut = new Vector3(o2.x, h2 - b2, o2.z);
                Vector3 v2_topBev = new Vector3(o2.x + d2.x * b2, h2, o2.z + d2.y * b2);
                Vector3 v2_topIn  = new Vector3(in2.x, h2, in2.z);
                Vector3 v2_botIn  = new Vector3(in2.x, yBot, in2.z);

                int baseIdx = verts.Count;
                verts.Add(v1_botOut); // 0
                verts.Add(v1_topOut); // 1
                verts.Add(v1_topBev); // 2
                verts.Add(v1_topIn);  // 3
                verts.Add(v1_botIn);  // 4

                verts.Add(v2_botOut); // 5
                verts.Add(v2_topOut); // 6
                verts.Add(v2_topBev); // 7
                verts.Add(v2_topIn);  // 8
                verts.Add(v2_botIn);  // 9

                for (int k = 0; k < 10; k++)
                {
                    Vector3 vk = verts[baseIdx + k];
                    uvs.Add(new Vector2(vk.x * 0.5f, vk.z * 0.5f));
                }

                // 1. Street Vertical Face (faces street, catches shadow/diffuse)
                tris.Add(baseIdx + 0); tris.Add(baseIdx + 1); tris.Add(baseIdx + 6);
                tris.Add(baseIdx + 0); tris.Add(baseIdx + 6); tris.Add(baseIdx + 5);

                // 2. 45° Safety Chamfer / Bevel (catches directional sun/sky highlight!)
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 7);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 7); tris.Add(baseIdx + 6);

                // 3. Horizontal Top Surface (width ~17.5cm)
                tris.Add(baseIdx + 2); tris.Add(baseIdx + 3); tris.Add(baseIdx + 8);
                tris.Add(baseIdx + 2); tris.Add(baseIdx + 8); tris.Add(baseIdx + 7);

                // 4. Bottom Face (sealed at Y = -0.02m)
                tris.Add(baseIdx + 0); tris.Add(baseIdx + 5); tris.Add(baseIdx + 9);
                tris.Add(baseIdx + 0); tris.Add(baseIdx + 9); tris.Add(baseIdx + 4);

                // NOTE: ZERO inner vertical face at 3->4! Sidewalk sits right against it, eliminating 100% of z-fighting!

                // Caps on first and last segments
                if (i == 0)
                {
                    // Sealed end-cap against Curb_West
                    tris.Add(baseIdx + 0); tris.Add(baseIdx + 4); tris.Add(baseIdx + 3);
                    tris.Add(baseIdx + 0); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
                    tris.Add(baseIdx + 0); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
                }
                if (i == count - 2)
                {
                    // Sealed end-cap at west Calle Gardenias end
                    tris.Add(baseIdx + 5); tris.Add(baseIdx + 6); tris.Add(baseIdx + 7);
                    tris.Add(baseIdx + 5); tris.Add(baseIdx + 7); tris.Add(baseIdx + 8);
                    tris.Add(baseIdx + 5); tris.Add(baseIdx + 8); tris.Add(baseIdx + 9);
                }
            }

            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            MeshCollider mc = curbGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        private static void ConstructDentalJarquinSidewalkMesh(string name, Transform parent, List<Vector3> curbOuterPoints, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh { name = $"{name}_Mesh" };

            int count = curbOuterPoints.Count;
            float arcCenterX = -6.70f;
            float arcCenterZ = 20.20f;
            float curbThick = 0.20f;
            float rampDepth = 1.30f; // Standard ADA/NOM ramp run depth from inner curb

            List<Vector2> frontPts = new List<Vector2>();
            List<Vector2> rampBackPts = new List<Vector2>();
            List<Vector2> buildingBackPts = new List<Vector2>();
            float[] frontHeights = new float[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 p = curbOuterPoints[i];
                Vector2 innerP;
                Vector2 rBackP;

                if (i <= 16)
                {
                    Vector2 dirToCenter = (new Vector2(arcCenterX, arcCenterZ) - new Vector2(p.x, p.z)).normalized;
                    innerP = new Vector2(p.x + dirToCenter.x * curbThick, p.z + dirToCenter.y * curbThick);
                    rBackP = new Vector2(p.x + dirToCenter.x * (curbThick + rampDepth), p.z + dirToCenter.y * (curbThick + rampDepth));
                }
                else
                {
                    innerP = new Vector2(p.x, p.z - curbThick);
                    rBackP = new Vector2(p.x, p.z - (curbThick + rampDepth));
                }

                frontPts.Add(innerP);
                rampBackPts.Add(rBackP);

                if (i <= 9 || i >= 15)
                {
                    frontHeights[i] = SidewalkTopY; // 0.20m
                }
                else if (i == 10 || i == 14)
                {
                    frontHeights[i] = 0.11f; // Transitional flare
                }
                else
                {
                    frontHeights[i] = 0.025f; // Lowered ramp mouth
                }

                float bx, bz;
                if (i <= 5)
                {
                    float t = (float)i / 5.0f;
                    bx = -12.00f;
                    bz = Mathf.Lerp(20.20f, 20.30f, t);
                }
                else if (i <= 16)
                {
                    float t = (float)(i - 5) / 11.0f;
                    bx = Mathf.Lerp(-12.00f, -13.50f, t);
                    bz = Mathf.Lerp(20.30f, 21.72f, t);
                }
                else
                {
                    float t = (float)(i - 16) / (count - 1 - 16);
                    bx = Mathf.Lerp(-13.50f, -34.00f, t);
                    bz = 21.72f;
                }
                buildingBackPts.Add(new Vector2(bx, bz));
            }

            int numQuads = count - 1;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float yTop = SidewalkTopY; // 0.20m
            float yBot = -0.02f;

            for (int i = 0; i < numQuads; i++)
            {
                Vector2 b1 = buildingBackPts[i];
                Vector2 b2 = buildingBackPts[i + 1];
                Vector2 rb1 = rampBackPts[i];
                Vector2 rb2 = rampBackPts[i + 1];
                Vector2 f1 = frontPts[i];
                Vector2 f2 = frontPts[i + 1];

                float h1 = frontHeights[i];
                float h2 = frontHeights[i + 1];

                // --- 1. INNER SIDEWALK PLATEAU (building wall to ramp back line: flat at 0.20m) ---
                int baseInner = vertices.Count;
                vertices.Add(new Vector3(b1.x, yTop, b1.y));   // 0
                vertices.Add(new Vector3(b2.x, yTop, b2.y));   // 1
                vertices.Add(new Vector3(rb2.x, yTop, rb2.y)); // 2
                vertices.Add(new Vector3(rb1.x, yTop, rb1.y)); // 3
                vertices.Add(new Vector3(b1.x, yBot, b1.y));   // 4
                vertices.Add(new Vector3(b2.x, yBot, b2.y));   // 5
                vertices.Add(new Vector3(rb2.x, yBot, rb2.y)); // 6
                vertices.Add(new Vector3(rb1.x, yBot, rb1.y)); // 7

                for (int v = 0; v < 8; v++)
                    uvs.Add(new Vector2(vertices[baseInner + v].x * 0.25f, vertices[baseInner + v].z * 0.25f));

                triangles.Add(baseInner + 0); triangles.Add(baseInner + 1); triangles.Add(baseInner + 2);
                triangles.Add(baseInner + 0); triangles.Add(baseInner + 2); triangles.Add(baseInner + 3);
                triangles.Add(baseInner + 4); triangles.Add(baseInner + 6); triangles.Add(baseInner + 5);
                triangles.Add(baseInner + 4); triangles.Add(baseInner + 7); triangles.Add(baseInner + 6);

                // Back skirt along building
                triangles.Add(baseInner + 1); triangles.Add(baseInner + 0); triangles.Add(baseInner + 4);
                triangles.Add(baseInner + 1); triangles.Add(baseInner + 4); triangles.Add(baseInner + 5);

                if (i == 0)
                {
                    triangles.Add(baseInner + 0); triangles.Add(baseInner + 3); triangles.Add(baseInner + 7);
                    triangles.Add(baseInner + 0); triangles.Add(baseInner + 7); triangles.Add(baseInner + 4);
                }
                if (i == numQuads - 1)
                {
                    triangles.Add(baseInner + 2); triangles.Add(baseInner + 1); triangles.Add(baseInner + 5);
                    triangles.Add(baseInner + 2); triangles.Add(baseInner + 5); triangles.Add(baseInner + 6);
                }

                // --- 2. OUTER SIDEWALK & RAMP/FLARES (ramp back line at 0.20m to front curb at h1/h2) ---
                int baseOuter = vertices.Count;
                vertices.Add(new Vector3(rb1.x, yTop, rb1.y)); // 0
                vertices.Add(new Vector3(rb2.x, yTop, rb2.y)); // 1
                vertices.Add(new Vector3(f2.x, h2, f2.y));     // 2
                vertices.Add(new Vector3(f1.x, h1, f1.y));     // 3
                vertices.Add(new Vector3(rb1.x, yBot, rb1.y)); // 4
                vertices.Add(new Vector3(rb2.x, yBot, rb2.y)); // 5
                vertices.Add(new Vector3(f2.x, yBot, f2.y));   // 6
                vertices.Add(new Vector3(f1.x, yBot, f1.y));   // 7

                for (int v = 0; v < 8; v++)
                    uvs.Add(new Vector2(vertices[baseOuter + v].x * 0.25f, vertices[baseOuter + v].z * 0.25f));

                triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 1); triangles.Add(baseOuter + 2);
                triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 3);
                triangles.Add(baseOuter + 4); triangles.Add(baseOuter + 6); triangles.Add(baseOuter + 5);
                triangles.Add(baseOuter + 4); triangles.Add(baseOuter + 7); triangles.Add(baseOuter + 6);

                // NOTE: ZERO duplicate front vertical skirt! The curb sits right in front, eliminating 100% of z-fighting!

                if (i == 0)
                {
                    triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 3); triangles.Add(baseOuter + 7);
                    triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 7); triangles.Add(baseOuter + 4);
                }
                if (i == numQuads - 1)
                {
                    triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 1); triangles.Add(baseOuter + 5);
                    triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 5); triangles.Add(baseOuter + 6);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            // Smooth normals on flat horizontal surfaces (y >= 0.199f) to avoid radial faceting
            Vector3[] norm = mesh.normals;
            Vector3[] vArray = mesh.vertices;
            for (int k = 0; k < vArray.Length; k++)
            {
                if (vArray[k].y >= 0.199f)
                {
                    norm[k] = Vector3.up;
                }
            }
            mesh.normals = norm;
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        private static void CreateWhiteTaxi(string name, Transform parent, Vector3 position, float rotationY, Material yellowMat)
        {
            GameObject taxiRoot = new GameObject(name);
            taxiRoot.transform.SetParent(parent);
            taxiRoot.transform.position = position;
            taxiRoot.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            Material whiteBodyMat = CreateOrGetMaterial("Assets/Settings/WhiteFrameMaterial.mat", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            Material darkGlassMat = CreateOrGetMaterial("Assets/Settings/InteriorDarkMaterial.mat", new Color(0.08f, 0.08f, 0.10f), 0.9f);
            Material tireMat = CreateOrGetMaterial("Assets/Settings/AsphaltMaterial.mat", new Color(0.12f, 0.12f, 0.12f), 0.1f);
            Material chromeMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.85f, 0.85f, 0.88f), 0.8f);
            Material tailRedMat = CreateOrGetMaterial("Assets/Settings/West_Sign_SantaFe.mat", new Color(0.85f, 0.15f, 0.12f), 0.3f);

            // 1. Lower chassis / body
            CreateBox($"{name}_Chassis", taxiRoot.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(4.20f, 0.50f, 1.80f), whiteBodyMat);

            // 2. Cabin / Greenhouse
            CreateBox($"{name}_Cabin", taxiRoot.transform,
                new Vector3(-0.25f, 0.95f, 0f), new Vector3(2.40f, 0.55f, 1.60f), whiteBodyMat);

            // 3. Windshield (Front, angled) & Rear Window
            GameObject windshield = CreateBox($"{name}_Windshield", taxiRoot.transform,
                new Vector3(1.00f, 0.92f, 0f), new Vector3(0.12f, 0.48f, 1.54f), darkGlassMat);
            windshield.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);

            GameObject rearWindow = CreateBox($"{name}_RearWindow", taxiRoot.transform,
                new Vector3(-1.48f, 0.92f, 0f), new Vector3(0.12f, 0.48f, 1.54f), darkGlassMat);
            rearWindow.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);

            // Side Windows
            CreateBox($"{name}_SideWin_L", taxiRoot.transform,
                new Vector3(-0.25f, 0.95f, 0.81f), new Vector3(2.30f, 0.45f, 0.04f), darkGlassMat);
            CreateBox($"{name}_SideWin_R", taxiRoot.transform,
                new Vector3(-0.25f, 0.95f, -0.81f), new Vector3(2.30f, 0.45f, 0.04f), darkGlassMat);

            // 4. Wheels (4 black cylinders/boxes)
            float wheelY = 0.30f;
            float[] wheelXs = new float[] { -1.35f, 1.35f };
            float[] wheelZs = new float[] { -0.85f, 0.85f };
            for (int wx = 0; wx < 2; wx++)
            {
                for (int wz = 0; wz < 2; wz++)
                {
                    CreateBox($"{name}_Wheel_{wx}_{wz}", taxiRoot.transform,
                        new Vector3(wheelXs[wx], wheelY, wheelZs[wz]), new Vector3(0.60f, 0.60f, 0.24f), tireMat);
                }
            }

            // 5. Bumpers
            CreateBox($"{name}_Bumper_Front", taxiRoot.transform,
                new Vector3(2.12f, 0.30f, 0f), new Vector3(0.12f, 0.20f, 1.84f), chromeMat);
            CreateBox($"{name}_Bumper_Rear", taxiRoot.transform,
                new Vector3(-2.12f, 0.30f, 0f), new Vector3(0.12f, 0.20f, 1.84f), chromeMat);

            // 6. Yellow TAXI Roof Light Box
            CreateBox($"{name}_TaxiRoofLight", taxiRoot.transform,
                new Vector3(-0.15f, 1.30f, 0f), new Vector3(0.48f, 0.16f, 0.22f), yellowMat);

            // 7. Headlights & Taillights
            CreateBox($"{name}_Headlight_L", taxiRoot.transform,
                new Vector3(2.13f, 0.48f, 0.65f), new Vector3(0.04f, 0.16f, 0.30f), yellowMat);
            CreateBox($"{name}_Headlight_R", taxiRoot.transform,
                new Vector3(2.13f, 0.48f, -0.65f), new Vector3(0.04f, 0.16f, 0.30f), yellowMat);
            CreateBox($"{name}_Taillight_L", taxiRoot.transform,
                new Vector3(-2.13f, 0.48f, 0.65f), new Vector3(0.04f, 0.16f, 0.30f), tailRedMat);
            CreateBox($"{name}_Taillight_R", taxiRoot.transform,
                new Vector3(-2.13f, 0.48f, -0.65f), new Vector3(0.04f, 0.16f, 0.30f), tailRedMat);
        }

        // =========================================================================
        // --- AUTHENTIC NORTH-WEST CORNER: ADMIN BUILDING, ACCESSIBILITY RAMP, STEPS & ORGANIC CURB ---
        // =========================================================================

        private static void ConstructNorthWestCornerAndAdminBuilding(Transform parent, Material concreteMat, Material curbMat, Material yellowWarningMat, Material asphaltMat, Material stripeMat)
        {
            GameObject nwRoot = new GameObject("Corner_NO_Organic_Root");
            nwRoot.transform.SetParent(parent);
            nwRoot.transform.position = Vector3.zero;

            // Load or Create Materials needed for the authentic facade and details
            Material whiteConcreteMat = CreateOrGetMaterial("Assets/Settings/BridgeWhiteConcrete.mat", new Color(0.95f, 0.95f, 0.96f), 0.15f);
            Material zocaloMat = CreateOrGetMaterial("Assets/Settings/West_DarkZocalo.mat", new Color(0.32f, 0.33f, 0.35f), 0.10f);
            Material darkTrimMat = CreateOrGetMaterial("Assets/Settings/InteriorDarkMaterial.mat", new Color(0.24f, 0.25f, 0.28f), 0.20f);
            Material redStripeMat = CreateOrGetMaterial("Assets/Settings/AdminRedStripe.mat", new Color(0.58f, 0.16f, 0.16f), 0.20f);
            Material canopyRoofMat = CreateOrGetMaterial("Assets/Settings/CanopyRoof.mat", new Color(0.90f, 0.91f, 0.92f), 0.10f);
            Material steelMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.75f, 0.77f, 0.80f), 0.70f);
            Material blueSignMat = CreateOrGetMaterial("Assets/Settings/BenavidesBlue.mat", new Color(0.06f, 0.38f, 0.78f), 0.35f);
            Material darkGlassMat = CreateOrGetMaterial("Assets/Settings/West_DarkGlass.mat", new Color(0.12f, 0.16f, 0.22f, 0.92f), 0.90f);
            Material whiteFrameMat = CreateOrGetMaterial("Assets/Settings/West_WhiteFrame.mat", new Color(0.96f, 0.96f, 0.96f), 0.20f);
            Material foliageMat = CreateOrGetMaterial("Assets/Settings/TreeFoliage.mat", new Color(0.18f, 0.45f, 0.15f), 0.10f);
            Material woodMat = CreateOrGetMaterial("Assets/Settings/West_WoodDoor.mat", new Color(0.38f, 0.26f, 0.18f), 0.15f);

            // =========================================================================
            // 1. V-SHAPED PEDESTRIAN PLAZA, BLUE DIAGONAL CURB & NORTHWEST DIAGONAL SIDEWALK
            // =========================================================================
            ConstructNorthWestOrganicCurbAndSidewalk(nwRoot.transform, concreteMat, curbMat, yellowWarningMat, blueSignMat, zocaloMat);

            // =========================================================================
            // 2. AUTHENTIC STEPPED 1-STORY CUSTOMS / ADMIN OFFICE BUILDING
            //    Positioned behind the switchback ramp (Z = 40.40m to 53.00m)
            //    - Main East Wing: X = -14.80m to -10.80m, Z = 41.20m to 53.00m
            //    - Stepped West Wing: X = -17.60m to -14.40m, Z = 40.20m to 50.00m
            // =========================================================================
            GameObject oldAdmin = GameObject.Find("AdminBuilding_West_Inspection");
            if (oldAdmin != null) UnityEngine.Object.DestroyImmediate(oldAdmin);

            GameObject adminGo = new GameObject("AdminBuilding_West_Inspection");
            adminGo.transform.SetParent(nwRoot.transform);
            adminGo.transform.position = new Vector3(-12.80f, 0f, 47.10f);

            // --- MAIN EAST WING (Center at local 0, 0, 0 -> World X=-12.80m, Z=47.10m; Size 4.00m x 11.80m) ---
            CreateBox("Admin_Zocalo", adminGo.transform,
                new Vector3(0f, 0.55f, 0f), new Vector3(4.00f, 1.10f, 11.80f), zocaloMat, keepCollider: true);
            CreateBox("Admin_Stripe_Grey", adminGo.transform,
                new Vector3(0f, 1.15f, 0f), new Vector3(4.06f, 0.10f, 11.86f), darkTrimMat);
            CreateBox("Admin_Stripe_Red", adminGo.transform,
                new Vector3(0f, 1.24f, 0f), new Vector3(4.06f, 0.06f, 11.86f), redStripeMat);
            CreateBox("Admin_UpperWalls", adminGo.transform,
                new Vector3(0f, 2.70f, 0f), new Vector3(4.00f, 2.90f, 11.80f), whiteConcreteMat, keepCollider: true);
            CreateBox("Admin_RoofSlab", adminGo.transform,
                new Vector3(0f, 4.20f, 0f), new Vector3(4.24f, 0.18f, 12.04f), canopyRoofMat, keepCollider: true);

            // Main Wing Parapet
            CreateBox("Admin_Parapet_South", adminGo.transform,
                new Vector3(0f, 4.42f, -5.90f), new Vector3(4.24f, 0.30f, 0.22f), whiteConcreteMat);
            CreateBox("Admin_Parapet_North", adminGo.transform,
                new Vector3(0f, 4.42f, 5.90f), new Vector3(4.24f, 0.30f, 0.22f), whiteConcreteMat);
            CreateBox("Admin_Parapet_East", adminGo.transform,
                new Vector3(2.00f, 4.42f, 0f), new Vector3(0.22f, 0.30f, 12.04f), whiteConcreteMat);

            // --- STEPPED WEST WING (Recessed/Stepped on the left, local X=-3.20m, Z=-2.00m -> World X=-16.00m, Z=45.10m; Size 3.00m x 9.60m) ---
            CreateBox("Admin_WestWing_Zocalo", adminGo.transform,
                new Vector3(-3.20f, 0.55f, -2.00f), new Vector3(3.00f, 1.10f, 9.60f), zocaloMat, keepCollider: true);
            CreateBox("Admin_WestWing_Stripe_Grey", adminGo.transform,
                new Vector3(-3.20f, 1.15f, -2.00f), new Vector3(3.06f, 0.10f, 9.66f), darkTrimMat);
            CreateBox("Admin_WestWing_Stripe_Red", adminGo.transform,
                new Vector3(-3.20f, 1.24f, -2.00f), new Vector3(3.06f, 0.06f, 9.66f), redStripeMat);
            CreateBox("Admin_WestWing_UpperWalls", adminGo.transform,
                new Vector3(-3.20f, 2.55f, -2.00f), new Vector3(3.00f, 2.60f, 9.60f), whiteConcreteMat, keepCollider: true);
            CreateBox("Admin_WestWing_RoofSlab", adminGo.transform,
                new Vector3(-3.20f, 3.90f, -2.00f), new Vector3(3.24f, 0.18f, 9.84f), canopyRoofMat, keepCollider: true);
            CreateBox("Admin_WestWing_Parapet_South", adminGo.transform,
                new Vector3(-3.20f, 4.10f, -6.80f), new Vector3(3.24f, 0.26f, 0.22f), whiteConcreteMat);
            CreateBox("Admin_WestWing_Parapet_West", adminGo.transform,
                new Vector3(-4.70f, 4.10f, -2.00f), new Vector3(0.22f, 0.26f, 9.84f), whiteConcreteMat);

            // --- BARRED WINDOWS (VENTANAS CON REJAS METÁLICAS) ---
            // 1. Main Wing South Window (facing ramp & street at Z = 41.20m)
            CreateBarredWindow("Admin_Window_Main_South", adminGo.transform,
                new Vector3(0.10f, 2.25f, -5.94f), Quaternion.identity, 1.65f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);

            // 2. Stepped West Wing South Window (facing ramp & diagonal street at Z = 40.30m)
            CreateBarredWindow("Admin_Window_WestWing_South", adminGo.transform,
                new Vector3(-3.00f, 2.20f, -6.84f), Quaternion.identity, 1.45f, 1.05f, 5, whiteFrameMat, darkGlassMat, steelMat);

            // 3. East Face Window 1 (facing bridge steps at X = -10.80m, Z = 43.20m)
            CreateBarredWindow("Admin_Window_East_1", adminGo.transform,
                new Vector3(2.04f, 2.25f, -3.90f), Quaternion.Euler(0f, -90f, 0f), 1.60f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);

            // 4. East Face Window 2 (further north along pedestrian walkway at X = -10.80m, Z = 46.80m)
            CreateBarredWindow("Admin_Window_East_2", adminGo.transform,
                new Vector3(2.04f, 2.25f, -0.30f), Quaternion.Euler(0f, -90f, 0f), 1.60f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);

            // --- UNIVERSAL ACCESSIBILITY PLAQUE (♿ BLUE SIGN ON LEFT CORNER OF WEST WING) ---
            GameObject signA11y = new GameObject("Admin_Sign_Accessibility");
            signA11y.transform.SetParent(adminGo.transform);
            signA11y.transform.localPosition = new Vector3(-4.20f, 2.15f, -6.85f);

            CreateBox("SignA11y_Backing", signA11y.transform,
                Vector3.zero, new Vector3(0.62f, 0.72f, 0.06f), blueSignMat);
            CreateBox("SignA11y_Border", signA11y.transform,
                new Vector3(0f, 0f, -0.01f), new Vector3(0.66f, 0.76f, 0.04f), whiteFrameMat);

            GameObject glyphHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glyphHead.name = "Glyph_Head";
            glyphHead.transform.SetParent(signA11y.transform);
            glyphHead.transform.localPosition = new Vector3(0.02f, 0.15f, -0.045f);
            glyphHead.transform.localScale = new Vector3(0.08f, 0.08f, 0.04f);
            SetMaterial(glyphHead, whiteFrameMat);
            UnityEngine.Object.DestroyImmediate(glyphHead.GetComponent<Collider>());

            CreateBox("Glyph_Back", signA11y.transform,
                new Vector3(-0.03f, 0.02f, -0.045f), new Vector3(0.05f, 0.18f, 0.025f), whiteFrameMat);
            CreateBox("Glyph_Seat", signA11y.transform,
                new Vector3(0.04f, -0.06f, -0.045f), new Vector3(0.15f, 0.05f, 0.025f), whiteFrameMat);
            CreateBox("Glyph_Wheel", signA11y.transform,
                new Vector3(-0.03f, -0.08f, -0.045f), new Vector3(0.16f, 0.16f, 0.025f), whiteFrameMat);

            // --- TELECOMMUNICATIONS ANTENNA TOWER ON ROOF (TORRE ARRIOSTRADA) ---
            GameObject towerGo = new GameObject("Admin_Roof_TelecomTower");
            towerGo.transform.SetParent(adminGo.transform);
            towerGo.transform.localPosition = new Vector3(-0.80f, 4.30f, -1.50f);

            Vector3[] legOffsets = new Vector3[]
            {
                new Vector3(0.00f, 2.40f, 0.40f),
                new Vector3(-0.35f, 2.40f, -0.20f),
                new Vector3(0.35f, 2.40f, -0.20f)
            };
            for (int l = 0; l < 3; l++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"TowerLeg_{l + 1}";
                leg.transform.SetParent(towerGo.transform);
                leg.transform.localPosition = legOffsets[l];
                leg.transform.localScale = new Vector3(0.045f, 2.40f, 0.045f);
                SetMaterial(leg, steelMat);
                UnityEngine.Object.DestroyImmediate(leg.GetComponent<Collider>());
            }
            for (float ry = 0.60f; ry <= 4.40f; ry += 0.80f)
            {
                GameObject rung1 = CreateBox($"Rung_A_{ry:F1}", towerGo.transform,
                    new Vector3(-0.17f, ry, 0.10f), new Vector3(0.42f, 0.03f, 0.03f), steelMat);
                rung1.transform.localRotation = Quaternion.Euler(0f, 60f, 0f);
                GameObject rung2 = CreateBox($"Rung_B_{ry:F1}", towerGo.transform,
                    new Vector3(0.17f, ry, 0.10f), new Vector3(0.42f, 0.03f, 0.03f), steelMat);
                rung2.transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
                GameObject rung3 = CreateBox($"Rung_C_{ry:F1}", towerGo.transform,
                    new Vector3(0.00f, ry, -0.20f), new Vector3(0.70f, 0.03f, 0.03f), steelMat);
            }
            GameObject mastPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mastPole.name = "Tower_TopMast";
            mastPole.transform.SetParent(towerGo.transform);
            mastPole.transform.localPosition = new Vector3(0f, 5.40f, 0f);
            mastPole.transform.localScale = new Vector3(0.04f, 0.80f, 0.04f);
            SetMaterial(mastPole, steelMat);
            UnityEngine.Object.DestroyImmediate(mastPole.GetComponent<Collider>());

            GameObject antennaSpike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antennaSpike.name = "Tower_AntennaSpike";
            antennaSpike.transform.SetParent(towerGo.transform);
            antennaSpike.transform.localPosition = new Vector3(0f, 6.50f, 0f);
            antennaSpike.transform.localScale = new Vector3(0.015f, 0.40f, 0.015f);
            SetMaterial(antennaSpike, steelMat);
            UnityEngine.Object.DestroyImmediate(antennaSpike.GetComponent<Collider>());

            Vector3[] guyAnchorLocal = new Vector3[]
            {
                new Vector3(-2.6f, 0f, -4.2f),
                new Vector3(2.4f, 0f, -4.2f),
                new Vector3(-2.6f, 0f, 6.2f),
                new Vector3(2.4f, 0f, 6.2f)
            };
            for (int g = 0; g < 4; g++)
            {
                Vector3 anchor = guyAnchorLocal[g];
                Vector3 wireTop = new Vector3(0f, 4.60f, 0f);
                Vector3 wireMid = (anchor + wireTop) * 0.5f;
                Vector3 diff = anchor - wireTop;
                GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wire.name = $"GuyWire_{g + 1}";
                wire.transform.SetParent(towerGo.transform);
                wire.transform.localPosition = wireMid;
                wire.transform.localRotation = Quaternion.FromToRotation(Vector3.up, diff.normalized);
                wire.transform.localScale = new Vector3(0.012f, diff.magnitude * 0.5f, 0.012f);
                SetMaterial(wire, steelMat);
                UnityEngine.Object.DestroyImmediate(wire.GetComponent<Collider>());
            }

            // =========================================================================
            // 3. 3 YELLOW-PAINTED PEDESTRIAN STEPS NEXT TO BRIDGE PILLAR (100% INSIDE PLAZA)
            //    Located between Admin Building East wall (X=-10.80m) and Pillar_Arch_1 (X=-8.0m)
            // =========================================================================
            GameObject stepsGo = new GameObject("Steps_Bridge_Pedestrian_Access");
            stepsGo.transform.SetParent(nwRoot.transform);
            stepsGo.transform.position = Vector3.zero;

            float stepCenterX = -5.65f;
            float stepWidth = 2.15f; // X from -6.72m to -4.58m, abutting Pillar_Arch_1 at X = -4.0m

            // Step 1 (Bottom step, Z = 40.30m to 40.95m, Top Y = 0.32m)
            CreateBox("Step_1_Concrete", stepsGo.transform,
                new Vector3(stepCenterX, 0.16f, 40.625f), new Vector3(stepWidth, 0.32f, 0.65f), yellowWarningMat, keepCollider: true);
            CreateBox("Step_1_TreadInlay", stepsGo.transform,
                new Vector3(stepCenterX, 0.322f, 40.68f), new Vector3(stepWidth - 0.16f, 0.01f, 0.48f), concreteMat);

            // Step 2 (Middle step, Z = 40.95m to 41.60m, Top Y = 0.44m)
            CreateBox("Step_2_Concrete", stepsGo.transform,
                new Vector3(stepCenterX, 0.22f, 41.275f), new Vector3(stepWidth, 0.44f, 0.65f), yellowWarningMat, keepCollider: true);
            CreateBox("Step_2_TreadInlay", stepsGo.transform,
                new Vector3(stepCenterX, 0.442f, 41.33f), new Vector3(stepWidth - 0.16f, 0.01f, 0.48f), concreteMat);

            // Step 3 (Top step & Bridge Pedestrian Walkway Deck, Z = 41.60m to 53.00m, Top Y = 0.56m)
            CreateBox("Step_3_YellowRiser", stepsGo.transform,
                new Vector3(stepCenterX, 0.28f, 41.925f), new Vector3(stepWidth, 0.56f, 0.65f), yellowWarningMat, keepCollider: true);
            CreateBox("Bridge_Pedestrian_UpperWalkway", stepsGo.transform,
                new Vector3(stepCenterX, 0.28f, 47.60f), new Vector3(stepWidth, 0.56f, 10.80f), concreteMat, keepCollider: true);

            // =========================================================================
            // 4. SWITCHBACK (DOUBLE-RUN) ACCESSIBILITY RAMP WITH GALVANIZED STEEL HANDRAILS
            //    100% Contained inside the V-shaped sidewalk plaza (never invading the street!)
            // =========================================================================
            GameObject rampRoot = new GameObject("Ramp_Universal_Access_Root");
            rampRoot.transform.SetParent(nwRoot.transform);
            rampRoot.transform.position = Vector3.zero;

            // Run 1 (Outer Diagonal Run parallel to the blue curb: ascends from behind the NW curb ramp to NW mid-landing)
            Vector3 run1Start = new Vector3(-8.75f, 0.20f, 36.50f);
            Vector3 run1End   = new Vector3(-12.70f, 0.38f, 38.12f);
            ConstructAngledRampRun("Ramp_Run1_OuterDiagonal", rampRoot.transform,
                run1Start, run1End, width: 1.25f, concreteMat, steelMat);

            // Mid-Turnaround Landing Platform (at NW corner of the ramp, Y = 0.38m)
            Vector3 midLandingPos = new Vector3(-13.05f, 0.19f, 38.85f);
            GameObject midLanding = CreateBox("Ramp_MidTurnaround_Landing", rampRoot.transform,
                midLandingPos, new Vector3(1.50f, 0.38f, 2.20f), concreteMat, keepCollider: true);
            midLanding.transform.rotation = Quaternion.Euler(0f, 28.8f, 0f);

            // Perimeter handrail around the West/Northwest back edge of the Mid-Landing
            ConstructFlatHandrailSegment("Handrail_MidLanding_Back", rampRoot.transform,
                new Vector3(-13.95f, 0.38f, 38.30f), new Vector3(-13.10f, 0.38f, 39.85f), steelMat);

            // Run 2 (Inner Run parallel to Admin Building front: ascends from mid-landing to top landing at steps)
            Vector3 run2Start = new Vector3(-12.20f, 0.38f, 39.35f);
            Vector3 run2End   = new Vector3(-6.75f, 0.56f, 41.25f);
            ConstructAngledRampRun("Ramp_Run2_InnerToBridge", rampRoot.transform,
                run2Start, run2End, width: 1.25f, concreteMat, steelMat);

            // =========================================================================
            // 5. DIAGONAL URBAN SHADE TREES (ARBOLADO EN BANQUETA DIAGONAL NOROESTE)
            // =========================================================================
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-18.50f, 0.20f, 41.30f),
                new Vector3(-23.20f, 0.20f, 43.88f),
                new Vector3(-27.90f, 0.20f, 46.47f),
                new Vector3(-32.60f, 0.20f, 49.05f)
            };
            for (int t = 0; t < treePositions.Length; t++)
            {
                CreateUrbanShadeTree($"UrbanTree_North_{t + 1}", nwRoot.transform,
                    treePositions[t], woodMat, foliageMat);
            }

            // =========================================================================
            // 6. REFLECTIVE ROADWAY STUDS (VIALETAS REFLEJANTES DESDE EL VÉRTICE HASTA EL PILAR)
            // =========================================================================
            GameObject vialetasRoot = new GameObject("Vialetas_Roadway_Curve");
            vialetasRoot.transform.SetParent(nwRoot.transform);
            vialetasRoot.transform.position = Vector3.zero;

            // In the real photo, a clean line of white reflective road studs runs on the asphalt
            // from just east of the NW wheelchair curb ramp (-5.65m, 35.10m) curving to Pillar_Arch_1 (-3.32m, 43.25m)
            Vector3[] studPoints = new Vector3[]
            {
                new Vector3(-5.65f,  0.015f, 35.10f),
                new Vector3(-5.38f,  0.015f, 35.95f),
                new Vector3(-5.12f,  0.015f, 36.85f),
                new Vector3(-4.85f,  0.015f, 37.75f),
                new Vector3(-4.58f,  0.015f, 38.65f),
                new Vector3(-4.32f,  0.015f, 39.55f),
                new Vector3(-4.06f,  0.015f, 40.45f),
                new Vector3(-3.80f,  0.015f, 41.40f),
                new Vector3(-3.55f,  0.015f, 42.32f),
                new Vector3(-3.32f,  0.015f, 43.25f)
            };
            for (int m = 0; m < studPoints.Length; m++)
            {
                GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stud.name = $"Vialeta_{m + 1}";
                stud.transform.SetParent(vialetasRoot.transform);
                stud.transform.position = studPoints[m];
                stud.transform.rotation = Quaternion.Euler(0f, 19f, 0f);
                stud.transform.localScale = new Vector3(0.12f, 0.024f, 0.12f);
                SetMaterial(stud, stripeMat);
                UnityEngine.Object.DestroyImmediate(stud.GetComponent<Collider>());
            }
        }

        private static void CreateBarredWindow(string name, Transform parent, Vector3 localPos, Quaternion localRot,
            float width, float height, int barCount, Material frameMat, Material glassMat, Material steelMat)
        {
            GameObject winGo = new GameObject(name);
            winGo.transform.SetParent(parent);
            winGo.transform.localPosition = localPos;
            winGo.transform.localRotation = localRot;

            CreateBox("Frame", winGo.transform,
                Vector3.zero, new Vector3(width, height, 0.10f), frameMat);
            CreateBox("Glass", winGo.transform,
                new Vector3(0f, 0f, 0.01f), new Vector3(width - 0.16f, height - 0.16f, 0.06f), glassMat);

            float spanX = width - 0.28f;
            for (int b = 0; b < barCount; b++)
            {
                float bx = -spanX * 0.5f + b * (spanX / (barCount - 1));
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = $"Bar_{b + 1}";
                bar.transform.SetParent(winGo.transform);
                bar.transform.localPosition = new Vector3(bx, 0f, -0.045f);
                bar.transform.localScale = new Vector3(0.03f, height * 0.48f, 0.03f);
                SetMaterial(bar, steelMat);
                UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            }

            // Two horizontal cross-bars on the security grille
            CreateBox("CrossBar_Top", winGo.transform,
                new Vector3(0f, height * 0.25f, -0.045f), new Vector3(width - 0.12f, 0.03f, 0.03f), steelMat);
            CreateBox("CrossBar_Bot", winGo.transform,
                new Vector3(0f, -height * 0.25f, -0.045f), new Vector3(width - 0.12f, 0.03f, 0.03f), steelMat);
        }

        private static void ConstructAngledRampRun(string name, Transform parent,
            Vector3 startPos, Vector3 endPos, float width, Material concreteMat, Material steelMat)
        {
            GameObject runGo = new GameObject(name);
            runGo.transform.SetParent(parent);

            Vector3 flatStart = new Vector3(startPos.x, 0f, startPos.z);
            Vector3 flatEnd   = new Vector3(endPos.x, 0f, endPos.z);
            Vector3 flatDelta = flatEnd - flatStart;
            float horizDist   = flatDelta.magnitude;
            float deltaY      = endPos.y - startPos.y;
            float runLen      = Mathf.Sqrt(horizDist * horizDist + deltaY * deltaY);

            float yawDeg   = Mathf.Atan2(flatDelta.x, flatDelta.z) * Mathf.Rad2Deg;
            float pitchDeg = -Mathf.Atan2(deltaY, horizDist) * Mathf.Rad2Deg;

            Vector3 midPos = (startPos + endPos) * 0.5f;
            runGo.transform.position = midPos;
            runGo.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            // Solid concrete ramp wedge/slab (thick enough so bottom sits inside the 0.20m plaza slab)
            GameObject slab = CreateBox($"{name}_Slab", runGo.transform,
                new Vector3(0f, -0.10f, 0f), new Vector3(width, 0.22f, runLen), concreteMat, keepCollider: true);
            slab.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

            // Raised concrete side curbs (5cm wide)
            float halfW = width * 0.5f;
            float[] sideXs = new float[] { -halfW + 0.03f, halfW - 0.03f };
            foreach (float sx in sideXs)
            {
                // Top rail (+0.92m), Mid rail (+0.48m), Low guide rail (+0.12m)
                GameObject topBar = CreateBox("Rail_Top", runGo.transform,
                    new Vector3(sx, 0.92f, 0f), new Vector3(0.045f, 0.045f, runLen), steelMat);
                topBar.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

                GameObject midBar = CreateBox("Rail_Mid", runGo.transform,
                    new Vector3(sx, 0.48f, 0f), new Vector3(0.038f, 0.038f, runLen), steelMat);
                midBar.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

                GameObject lowBar = CreateBox("Rail_Low", runGo.transform,
                    new Vector3(sx, 0.12f, 0f), new Vector3(0.035f, 0.035f, runLen), steelMat);
                lowBar.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

                int postCount = 5;
                for (int p = 0; p < postCount; p++)
                {
                    float t = (float)p / (postCount - 1);
                    float localZ = Mathf.Lerp(-horizDist * 0.5f, horizDist * 0.5f, t);
                    float localY = Mathf.Lerp(-deltaY * 0.5f, deltaY * 0.5f, t);

                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{p + 1}";
                    post.transform.SetParent(runGo.transform);
                    post.transform.localPosition = new Vector3(sx, localY + 0.46f, localZ);
                    post.transform.localRotation = Quaternion.identity;
                    post.transform.localScale = new Vector3(0.045f, 0.46f, 0.045f);
                    SetMaterial(post, steelMat);
                    UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                }
            }
        }

        private static void ConstructFlatHandrailSegment(string name, Transform parent, Vector3 p1, Vector3 p2, Material steelMat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            Vector3 mid = (p1 + p2) * 0.5f;
            Vector3 diff = p2 - p1;
            float len = diff.magnitude;
            go.transform.position = mid;
            go.transform.rotation = Quaternion.LookRotation(diff.normalized);

            CreateBox("Rail_Top", go.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.045f, 0.045f, len), steelMat);
            CreateBox("Rail_Mid", go.transform, new Vector3(0f, 0.48f, 0f), new Vector3(0.038f, 0.038f, len), steelMat);
            CreateBox("Rail_Low", go.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.035f, 0.035f, len), steelMat);

            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(-len * 0.5f, len * 0.5f, i * 0.5f);
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Post_{i + 1}";
                post.transform.SetParent(go.transform);
                post.transform.localPosition = new Vector3(0f, 0.46f, z);
                post.transform.localRotation = Quaternion.identity;
                post.transform.localScale = new Vector3(0.045f, 0.46f, 0.045f);
                SetMaterial(post, steelMat);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            }
        }

        private static void CreateUrbanShadeTree(string name, Transform parent, Vector3 basePos, Material trunkMat, Material foliageMat)
        {
            GameObject treeGo = new GameObject(name);
            treeGo.transform.SetParent(parent);
            treeGo.transform.position = basePos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeGo.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.10f, 0f);
            trunk.transform.localScale = new Vector3(0.24f, 1.10f, 0.24f);
            SetMaterial(trunk, trunkMat);

            GameObject canopy1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy1.name = "Canopy_Main";
            canopy1.transform.SetParent(treeGo.transform);
            canopy1.transform.localPosition = new Vector3(0f, 2.90f, 0f);
            canopy1.transform.localScale = new Vector3(2.90f, 2.30f, 2.90f);
            SetMaterial(canopy1, foliageMat);
            UnityEngine.Object.DestroyImmediate(canopy1.GetComponent<Collider>());

            GameObject canopy2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy2.name = "Canopy_Top";
            canopy2.transform.SetParent(treeGo.transform);
            canopy2.transform.localPosition = new Vector3(0.20f, 3.50f, -0.15f);
            canopy2.transform.localScale = new Vector3(2.20f, 1.80f, 2.20f);
            SetMaterial(canopy2, foliageMat);
            UnityEngine.Object.DestroyImmediate(canopy2.GetComponent<Collider>());
        }

        private static void ConstructNorthWestOrganicCurbAndSidewalk(Transform parent, Material concreteMat, Material curbMat, Material yellowWarningMat, Material blueCurbMat, Material zocaloMat)
        {
            GameObject cornerGo = new GameObject("Corner_NO_Organic");
            cornerGo.transform.SetParent(parent);
            cornerGo.transform.position = Vector3.zero;

            // =========================================================================
            // 1. YELLOW BULLNOSE ISLAND BASE AROUND BRIDGE PILLAR_ARCH_1 (X=-4.0m, Z=45.0m)
            //    Note: Does NOT modify the bridge pillar itself; adds the authentic yellow
            //    rounded sidewalk bullnose & bollard post at the base of the pillar!
            // =========================================================================
            GameObject bullnoseGo = new GameObject("Pillar1_Yellow_Bullnose_Base");
            bullnoseGo.transform.SetParent(cornerGo.transform);
            bullnoseGo.transform.position = new Vector3(-4.00f, 0f, 43.90f);

            // Rounded yellow bullnose endcap at the south base of Pillar_Arch_1
            GameObject bullnoseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bullnoseCyl.name = "Bullnose_Curved_Tip";
            bullnoseCyl.transform.SetParent(bullnoseGo.transform);
            bullnoseCyl.transform.localPosition = new Vector3(0f, 0.14f, -0.35f);
            bullnoseCyl.transform.localScale = new Vector3(1.55f, 0.14f, 1.10f);
            SetMaterial(bullnoseCyl, yellowWarningMat);

            CreateBox("Bullnose_Body", bullnoseGo.transform,
                new Vector3(0f, 0.14f, 0.55f), new Vector3(1.55f, 0.28f, 1.80f), yellowWarningMat, keepCollider: true);

            // Yellow protective bollard post on the bullnose tip (visible in real photo)
            CreateBox("Bullnose_Yellow_Bollard", bullnoseGo.transform,
                new Vector3(0.38f, 0.65f, -0.45f), new Vector3(0.18f, 0.95f, 0.18f), yellowWarningMat);

            // Dark grey lower column base trim around Pillar_Arch_1 (matching real photo)
            CreateBox("Pillar1_DarkGrey_BaseWrap", bullnoseGo.transform,
                new Vector3(0f, 0.90f, 1.10f), new Vector3(1.44f, 1.35f, 1.44f), zocaloMat);

            // =========================================================================
            // 2. V-SHAPED PLAZA PERIMETER, RECESSED NW CURB RAMP & NORTHWEST DIAGONAL CURB (28.8° ANGLE)
            //    - Northeast Edge: from Pillar_Arch_1 (-3.70m, 43.60m) to East Ramp Flare (-5.65m, 36.25m)
            //    - Recessed NW Wheelchair Curb Ramp (Ramp_Corner_NO): mouth from (-6.00m, 35.12m) to (-7.72m, 34.82m),
            //      centered at (-6.86m, 34.97m) directly facing Ramp_Corner_SO!
            //    - Northwest Diagonal Edge: from (-8.35m, 35.80m) to (-31.00m, 48.32m)
            // =========================================================================
            Vector3[] frontPerimeter = new Vector3[]
            {
                new Vector3(-3.70f,  0.10f, 43.60f), // 0: Meets Pillar_Arch_1 bullnose base at X = -4.0m
                new Vector3(-4.35f,  0.10f, 41.20f), // 1: In front of yellow steps
                new Vector3(-5.10f,  0.10f, 38.60f), // 2: Mid NE plaza edge
                new Vector3(-5.65f,  0.10f, 36.25f), // 3: East start of NW curb ramp flared wing (Y=0.20m)
                new Vector3(-6.00f,  0.10f, 35.12f), // 4: East lip of NW wheelchair curb ramp (Y=0.015m)
                new Vector3(-7.72f,  0.10f, 34.82f), // 5: West lip of NW wheelchair curb ramp (Y=0.015m)
                new Vector3(-8.35f,  0.10f, 35.80f), // 6: West end of NW curb ramp flared wing / Blue curb continues (Y=0.20m)
                new Vector3(-12.20f, 0.10f, 37.98f), // 7: End of accessibility ramp zone (Blue curb ends)
                new Vector3(-21.60f, 0.10f, 43.15f), // 8: Mid Northwest diagonal street curb
                new Vector3(-31.00f, 0.10f, 48.32f)  // 9: Far Northwest street boundary
            };

            for (int i = 0; i < frontPerimeter.Length - 1; i++)
            {
                Vector3 p1 = frontPerimeter[i];
                Vector3 p2 = frontPerimeter[i + 1];
                Vector3 mid = (p1 + p2) * 0.5f;
                Vector3 delta = p2 - p1;
                float segLen = delta.magnitude + 0.04f;
                float rotAngle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

                if (i == 4)
                {
                    // Lowered flush curb lip at the mouth of Ramp_Corner_NO (Y = 0.01m, height = 0.02m)
                    GameObject lip = CreateBox($"Curb_NW_RampLip_{i}", cornerGo.transform,
                        new Vector3(mid.x, 0.01f, mid.z), new Vector3(0.22f, 0.02f, segLen), blueCurbMat);
                    lip.transform.rotation = Quaternion.Euler(0f, rotAngle, 0f);
                }
                else if (i == 3 || i == 5)
                {
                    // Flared transition curb stones sloping between full curb height (0.20m) and flush ramp lip (0.02m)
                    float pitch = (i == 3) ? 8.5f : -8.5f;
                    Material flareMat = (i == 5) ? blueCurbMat : curbMat;
                    GameObject flare = CreateBox($"Curb_NW_RampFlare_{i}", cornerGo.transform,
                        new Vector3(mid.x, 0.06f, mid.z), new Vector3(0.22f, 0.12f, segLen), flareMat);
                    flare.transform.rotation = Quaternion.Euler(pitch, rotAngle, 0f);
                }
                else
                {
                    Material segMat = curbMat;
                    if (i == 6)
                    {
                        segMat = blueCurbMat;
                    }
                    else if (i == 0)
                    {
                        segMat = yellowWarningMat;
                    }

                    GameObject seg = CreateBox($"Curb_NW_Edge_{i}", cornerGo.transform,
                        mid, new Vector3(0.22f, 0.20f, segLen), segMat);
                    seg.transform.rotation = Quaternion.LookRotation(delta.normalized);
                }
            }

            // =========================================================================
            // 3. SOLID MANIFOLD 3D SIDEWALK PLAZA, RECESSED RAMP_CORNER_NO & DIAGONAL SIDEWALK MESH
            //    Spans 100% of the area behind frontPerimeter up to Z_back = 53.00m
            //    with an intermediate vertex line 1.35m behind the front edge to form Ramp_Corner_NO!
            // =========================================================================
            GameObject plazaGo = new GameObject("Sidewalk_NW_Plaza_Mesh");
            plazaGo.transform.SetParent(cornerGo.transform);
            plazaGo.transform.position = Vector3.zero;

            MeshFilter mf = plazaGo.AddComponent<MeshFilter>();
            MeshRenderer mr = plazaGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = concreteMat;

            Mesh mesh = new Mesh();
            mesh.name = "Sidewalk_NW_Plaza";

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float yTop = 0.20f;
            float yBot = 0.00f;
            float zBack = 53.00f;

            int ptCount = frontPerimeter.Length;
            float[] fTopYs = new float[ptCount];
            Vector3[] midPerimeter = new Vector3[ptCount];
            for (int i = 0; i < ptCount; i++)
            {
                // Vertices 4 and 5 form the lowered street lip of Ramp_Corner_NO (Y = 0.015m)
                fTopYs[i] = (i == 4 || i == 5) ? 0.015f : yTop;
                midPerimeter[i] = new Vector3(frontPerimeter[i].x, yTop, frontPerimeter[i].z + 1.38f);
            }

            int stripCount = ptCount - 1;
            for (int i = 0; i < stripCount; i++)
            {
                Vector3 f1 = frontPerimeter[i];
                Vector3 f2 = frontPerimeter[i + 1];
                Vector3 m1 = midPerimeter[i];
                Vector3 m2 = midPerimeter[i + 1];
                float fy1 = fTopYs[i];
                float fy2 = fTopYs[i + 1];

                // --- INNER STRIP (midPerimeter to zBack = 53.00m: always flat at yTop = 0.20m) ---
                Vector3 b1 = new Vector3(f1.x, yTop, zBack);
                Vector3 b2 = new Vector3(f2.x, yTop, zBack);
                Vector3 m1_bot = new Vector3(m1.x, yBot, m1.z);
                Vector3 m2_bot = new Vector3(m2.x, yBot, m2.z);
                Vector3 b1_bot = new Vector3(b1.x, yBot, zBack);
                Vector3 b2_bot = new Vector3(b2.x, yBot, zBack);

                int baseInner = vertices.Count;
                vertices.Add(b1);     // 0
                vertices.Add(b2);     // 1
                vertices.Add(m2);     // 2
                vertices.Add(m1);     // 3
                vertices.Add(b1_bot); // 4
                vertices.Add(b2_bot); // 5
                vertices.Add(m2_bot); // 6
                vertices.Add(m1_bot); // 7

                for (int k = 0; k < 8; k++)
                {
                    Vector3 vk = vertices[baseInner + k];
                    uvs.Add(new Vector2(vk.x * 0.25f, vk.z * 0.25f));
                }

                // Top (+Y) & Bottom (-Y)
                triangles.Add(baseInner + 0); triangles.Add(baseInner + 3); triangles.Add(baseInner + 2);
                triangles.Add(baseInner + 0); triangles.Add(baseInner + 2); triangles.Add(baseInner + 1);
                triangles.Add(baseInner + 4); triangles.Add(baseInner + 6); triangles.Add(baseInner + 7);
                triangles.Add(baseInner + 4); triangles.Add(baseInner + 5); triangles.Add(baseInner + 6);

                // Back face (at Z = 53.00m)
                triangles.Add(baseInner + 1); triangles.Add(baseInner + 5); triangles.Add(baseInner + 4);
                triangles.Add(baseInner + 1); triangles.Add(baseInner + 4); triangles.Add(baseInner + 0);

                if (i == 0)
                {
                    triangles.Add(baseInner + 0); triangles.Add(baseInner + 4); triangles.Add(baseInner + 7);
                    triangles.Add(baseInner + 0); triangles.Add(baseInner + 7); triangles.Add(baseInner + 3);
                }
                if (i == stripCount - 1)
                {
                    triangles.Add(baseInner + 2); triangles.Add(baseInner + 6); triangles.Add(baseInner + 5);
                    triangles.Add(baseInner + 2); triangles.Add(baseInner + 5); triangles.Add(baseInner + 1);
                }

                // --- OUTER STRIP (frontPerimeter at fy1/fy2 to midPerimeter at yTop=0.20m: forms Ramp_Corner_NO & flared wings!) ---
                Vector3 f1_top = new Vector3(f1.x, fy1, f1.z);
                Vector3 f2_top = new Vector3(f2.x, fy2, f2.z);
                Vector3 f1_bot = new Vector3(f1.x, yBot, f1.z);
                Vector3 f2_bot = new Vector3(f2.x, yBot, f2.z);

                int baseOuter = vertices.Count;
                vertices.Add(m1);     // 0
                vertices.Add(m2);     // 1
                vertices.Add(f2_top); // 2
                vertices.Add(f1_top); // 3
                vertices.Add(m1_bot); // 4
                vertices.Add(m2_bot); // 5
                vertices.Add(f2_bot); // 6
                vertices.Add(f1_bot); // 7

                for (int k = 0; k < 8; k++)
                {
                    Vector3 vk = vertices[baseOuter + k];
                    uvs.Add(new Vector2(vk.x * 0.25f, vk.z * 0.25f));
                }

                // Top (+Y) & Bottom (-Y)
                triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 3); triangles.Add(baseOuter + 2);
                triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 1);
                triangles.Add(baseOuter + 4); triangles.Add(baseOuter + 6); triangles.Add(baseOuter + 7);
                triangles.Add(baseOuter + 4); triangles.Add(baseOuter + 5); triangles.Add(baseOuter + 6);

                // Front face (facing street)
                triangles.Add(baseOuter + 3); triangles.Add(baseOuter + 7); triangles.Add(baseOuter + 6);
                triangles.Add(baseOuter + 3); triangles.Add(baseOuter + 6); triangles.Add(baseOuter + 2);

                if (i == 0)
                {
                    triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 4); triangles.Add(baseOuter + 7);
                    triangles.Add(baseOuter + 0); triangles.Add(baseOuter + 7); triangles.Add(baseOuter + 3);
                }
                if (i == stripCount - 1)
                {
                    triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 6); triangles.Add(baseOuter + 5);
                    triangles.Add(baseOuter + 2); triangles.Add(baseOuter + 5); triangles.Add(baseOuter + 1);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;

            MeshCollider mc = plazaGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            // =========================================================================
            // 4. RAMP_CORNER_NO VERTEX-CONFORMED ACCESSIBILITY BLUE & TACTILE OVERLAY
            //    Conforms 100% to strip i=4 (vertices 4 -> 5) so zero edges ever poke out!
            // =========================================================================
            GameObject rampNoGo = new GameObject("Ramp_Corner_NO");
            rampNoGo.transform.SetParent(cornerGo.transform);
            rampNoGo.transform.position = Vector3.zero;

            MeshFilter rnoMf = rampNoGo.AddComponent<MeshFilter>();
            MeshRenderer rnoMr = rampNoGo.AddComponent<MeshRenderer>();
            rnoMr.sharedMaterial = blueCurbMat;

            Vector3 rm1 = new Vector3(midPerimeter[4].x, yTop + 0.005f, midPerimeter[4].z);
            Vector3 rm2 = new Vector3(midPerimeter[5].x, yTop + 0.005f, midPerimeter[5].z);
            Vector3 rf1 = new Vector3(frontPerimeter[4].x, fTopYs[4] + 0.005f, frontPerimeter[4].z);
            Vector3 rf2 = new Vector3(frontPerimeter[5].x, fTopYs[5] + 0.005f, frontPerimeter[5].z);

            Mesh rnoMesh = new Mesh { name = "Ramp_Corner_NO_BlueMesh" };
            rnoMesh.vertices = new Vector3[] { rm1, rm2, rf2, rf1 };
            rnoMesh.uv = new Vector2[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
            rnoMesh.triangles = new int[] { 0, 3, 2, 0, 2, 1 };
            rnoMesh.RecalculateNormals();
            rnoMesh.RecalculateBounds();
            rnoMf.sharedMesh = rnoMesh;

            // Yellow tactile warning band along the bottom 30% of Ramp_Corner_NO
            GameObject rnoTactileGo = new GameObject("Ramp_Corner_NO_Tactile_Pavers");
            rnoTactileGo.transform.SetParent(rampNoGo.transform);
            rnoTactileGo.transform.position = Vector3.zero;

            MeshFilter rnotMf = rnoTactileGo.AddComponent<MeshFilter>();
            MeshRenderer rnotMr = rnoTactileGo.AddComponent<MeshRenderer>();
            rnotMr.sharedMaterial = yellowWarningMat;

            Vector3 rtTop1 = Vector3.Lerp(rm1, rf1, 0.68f) + new Vector3(0f, 0.003f, 0f);
            Vector3 rtTop2 = Vector3.Lerp(rm2, rf2, 0.68f) + new Vector3(0f, 0.003f, 0f);
            Vector3 rtBot1 = Vector3.Lerp(rm1, rf1, 0.98f) + new Vector3(0f, 0.003f, 0f);
            Vector3 rtBot2 = Vector3.Lerp(rm2, rf2, 0.98f) + new Vector3(0f, 0.003f, 0f);

            Mesh rnotMesh = new Mesh { name = "Ramp_Corner_NO_TactileMesh" };
            rnotMesh.vertices = new Vector3[] { rtTop1, rtTop2, rtBot2, rtBot1 };
            rnotMesh.uv = new Vector2[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
            rnotMesh.triangles = new int[] { 0, 3, 2, 0, 2, 1 };
            rnotMesh.RecalculateNormals();
            rnotMesh.RecalculateBounds();
            rnotMf.sharedMesh = rnotMesh;
        }

        // --- AUTHENTIC ROUNDED CORNERS (TRUE RADIUS CUTOUT) ---

        private static void ConstructAllCorners(Transform parent, Material concreteMat, Material curbMat, Material yellowWarningMat)
        {
            // Corner SE: Curiosidades Azteca
            // Sweeps from Ave. Juárez curb (X=6.90m, Z=24.00m) to Gardenias south curb lip (X=8.30m, Z=25.40m)
            // Center: (8.30m, 24.00m), Radius: 1.40m, Angles: 180 deg -> 90 deg
            ConstructSolidCurvedCorner("Corner_SE", parent,
                arcCenterX: 8.30f, arcCenterZ: 24.00f, radius: 1.40f,
                startAngleDeg: 180f, endAngleDeg: 90f,
                concreteMat, curbMat);

            // Corner NE: Bridge Promenade / Elena Ramos (Yellow Safety Warning Curb)
            // Sweeps from Bridge Promenade curb (X=12.10m, Z=33.80m) to Gardenias north curb lip (X=13.50m, Z=32.40m)
            // Center: (13.50m, 33.80m), Radius: 1.40m, Angles: 180 deg -> 270 deg
            ConstructSolidCurvedCorner("Corner_NE", parent,
                arcCenterX: 13.50f, arcCenterZ: 33.80f, radius: 1.40f,
                startAngleDeg: 180f, endAngleDeg: 270f,
                concreteMat, yellowWarningMat);

            // Note: Corner NO is authentically constructed as a continuous organic curve with R=4m-6.6m
            // and yellow safety curb in ConstructNorthWestCornerAndAdminBuilding.
        }

        /// <summary>
        /// Constructs a solid, non-glitching rounded sidewalk corner:
        /// - 8 smooth overlapping solid 3D curb blocks along the exact circular arc
        /// - Solid 3D manifold concrete sidewalk wedge mesh (top, bottom, and vertical side walls sealing against the ramp)
        /// - ZERO square protruding blocks into the street
        /// - ZERO backface culling issues, 100% solid walking collision
        /// </summary>
        private static void ConstructSolidCurvedCorner(string name, Transform parent,
            float arcCenterX, float arcCenterZ, float radius,
            float startAngleDeg, float endAngleDeg,
            Material concreteMat, Material curbMat)
        {
            GameObject cornerGo = new GameObject(name);
            cornerGo.transform.SetParent(parent);
            cornerGo.transform.position = Vector3.zero;

            int segments = 8;
            float step = (endAngleDeg - startAngleDeg) / segments;

            // 1. Solid Curved Curb Stones along the arc
            for (int i = 0; i < segments; i++)
            {
                float a1 = (startAngleDeg + i * step) * Mathf.Deg2Rad;
                float a2 = (startAngleDeg + (i + 1) * step) * Mathf.Deg2Rad;

                Vector3 p1 = new Vector3(arcCenterX + radius * Mathf.Cos(a1), 0.10f, arcCenterZ + radius * Mathf.Sin(a1));
                Vector3 p2 = new Vector3(arcCenterX + radius * Mathf.Cos(a2), 0.10f, arcCenterZ + radius * Mathf.Sin(a2));

                Vector3 mid = (p1 + p2) * 0.5f;
                Vector3 delta = p2 - p1;
                float segLen = delta.magnitude;
                float rotAngle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"{name}_CurbStone_{i}";
                seg.transform.SetParent(cornerGo.transform);
                seg.transform.position = mid;
                seg.transform.rotation = Quaternion.Euler(0f, rotAngle, 0f);
                seg.transform.localScale = new Vector3(0.20f, 0.20f, segLen + 0.02f);
                SetMaterial(seg, curbMat);
            }

            // 2. Solid Sidewalk Fillet Wedge 3D Manifold Mesh (filling the sector behind the curb with vertical side skirts)
            Mesh wedgeMesh = new Mesh { name = $"{name}_FilletWedge" };
            float innerRadius = radius - 0.10f;
            float yTop = SidewalkTopY; // 0.20m
            float yBot = 0.00f;

            int arcVertCount = segments + 1;
            int totalVerts = (arcVertCount + 1) * 2;
            Vector3[] verts = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];

            // Top center: 0
            verts[0] = new Vector3(arcCenterX, yTop, arcCenterZ);
            uvs[0] = new Vector2(arcCenterX * 0.2f, arcCenterZ * 0.2f);

            // Top arc: 1..arcVertCount
            for (int i = 0; i < arcVertCount; i++)
            {
                float rad = (startAngleDeg + i * step) * Mathf.Deg2Rad;
                verts[1 + i] = new Vector3(arcCenterX + innerRadius * Mathf.Cos(rad), yTop, arcCenterZ + innerRadius * Mathf.Sin(rad));
                uvs[1 + i] = new Vector2(verts[1 + i].x * 0.2f, verts[1 + i].z * 0.2f);
            }

            // Bottom center: arcVertCount + 1
            int botCenter = arcVertCount + 1;
            verts[botCenter] = new Vector3(arcCenterX, yBot, arcCenterZ);
            uvs[botCenter] = new Vector2(arcCenterX * 0.2f, arcCenterZ * 0.2f);

            // Bottom arc: botCenter + 1 .. totalVerts - 1
            for (int i = 0; i < arcVertCount; i++)
            {
                float rad = (startAngleDeg + i * step) * Mathf.Deg2Rad;
                verts[botCenter + 1 + i] = new Vector3(arcCenterX + innerRadius * Mathf.Cos(rad), yBot, arcCenterZ + innerRadius * Mathf.Sin(rad));
                uvs[botCenter + 1 + i] = new Vector2(verts[botCenter + 1 + i].x * 0.2f, verts[botCenter + 1 + i].z * 0.2f);
            }

            int numTris = segments * 3 * 2 + 12 + segments * 6;
            int[] tris = new int[numTris];
            int tIdx = 0;
            bool sweepPositive = (endAngleDeg > startAngleDeg);

            // Top face (+Y)
            for (int i = 0; i < segments; i++)
            {
                if (sweepPositive)
                {
                    tris[tIdx++] = 0;
                    tris[tIdx++] = 1 + (i + 1);
                    tris[tIdx++] = 1 + i;
                }
                else
                {
                    tris[tIdx++] = 0;
                    tris[tIdx++] = 1 + i;
                    tris[tIdx++] = 1 + (i + 1);
                }
            }

            // Bottom face (-Y)
            for (int i = 0; i < segments; i++)
            {
                if (sweepPositive)
                {
                    tris[tIdx++] = botCenter;
                    tris[tIdx++] = botCenter + 1 + i;
                    tris[tIdx++] = botCenter + 1 + (i + 1);
                }
                else
                {
                    tris[tIdx++] = botCenter;
                    tris[tIdx++] = botCenter + 1 + (i + 1);
                    tris[tIdx++] = botCenter + 1 + i;
                }
            }

            // Radial Side 1 (at start angle i = 0)
            int topStart = 1;
            int botStart = botCenter + 1;
            if (sweepPositive)
            {
                tris[tIdx++] = 0; tris[tIdx++] = topStart; tris[tIdx++] = botStart;
                tris[tIdx++] = 0; tris[tIdx++] = botStart; tris[tIdx++] = botCenter;
            }
            else
            {
                tris[tIdx++] = 0; tris[tIdx++] = botStart; tris[tIdx++] = topStart;
                tris[tIdx++] = 0; tris[tIdx++] = botCenter; tris[tIdx++] = botStart;
            }

            // Radial Side 2 (at end angle i = segments, forms vertical retention wall facing ramp)
            int topEnd = arcVertCount;
            int botEnd = botCenter + arcVertCount;
            if (sweepPositive)
            {
                tris[tIdx++] = 0; tris[tIdx++] = botEnd; tris[tIdx++] = topEnd;
                tris[tIdx++] = 0; tris[tIdx++] = botCenter; tris[tIdx++] = botEnd;
            }
            else
            {
                tris[tIdx++] = 0; tris[tIdx++] = topEnd; tris[tIdx++] = botEnd;
                tris[tIdx++] = 0; tris[tIdx++] = botEnd; tris[tIdx++] = botCenter;
            }

            // Outer curved face along arc
            for (int i = 0; i < segments; i++)
            {
                int t1 = 1 + i;
                int t2 = 1 + (i + 1);
                int b1 = botCenter + 1 + i;
                int b2 = botCenter + 1 + (i + 1);

                if (sweepPositive)
                {
                    tris[tIdx++] = t1; tris[tIdx++] = t2; tris[tIdx++] = b2;
                    tris[tIdx++] = t1; tris[tIdx++] = b2; tris[tIdx++] = b1;
                }
                else
                {
                    tris[tIdx++] = t1; tris[tIdx++] = b2; tris[tIdx++] = t2;
                    tris[tIdx++] = t1; tris[tIdx++] = b1; tris[tIdx++] = b2;
                }
            }

            wedgeMesh.vertices = verts;
            wedgeMesh.uv = uvs;
            wedgeMesh.triangles = tris;
            wedgeMesh.RecalculateNormals();
            wedgeMesh.RecalculateBounds();

            GameObject wedgeGo = new GameObject($"{name}_FilletTop");
            wedgeGo.transform.SetParent(cornerGo.transform);
            wedgeGo.transform.position = Vector3.zero;
            var mf = wedgeGo.AddComponent<MeshFilter>();
            mf.sharedMesh = wedgeMesh;
            var mr = wedgeGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = concreteMat;
            var col = wedgeGo.AddComponent<MeshCollider>();
            col.sharedMesh = wedgeMesh;
        }

        // --- RECESSED WHEELCHAIR RAMPS (VADOS PEATONALES) ---

        private static void ConstructWheelchairRamps(Transform parent, Material concreteMat, Material curbMat, Material yellowWarningMat)
        {
            // South Ramps:
            // Ramp_Corner_SE: slopes down from Z = 23.60m (Y=0.20m) to Z = 25.40m (Y=0.00m at street lip), flush with Crosswalk_Gardenias_East
            CreateRecessedRampUnit("Ramp_Corner_SE", parent,
                centerX: EastCrosswalkX, startZ: 23.60f, endZ: 25.40f, facingNorth: true, concreteMat, curbMat, yellowWarningMat);

            // Note: Ramp_Corner_SO (Dental Jarquín) is seamlessly and organically integrated into 
            // ConstructSouthWestOrganicCorner / ConstructDentalJarquinSidewalkMesh with curved ADA lowered curb lip,
            // radial tactile warning pavers, and 100% gapless flared transition wings.

            // North Ramps:
            // Ramp_Corner_NE: slopes down from Z = 34.00m (Y=0.20m, flush with Sidewalk_East_North_Section) to Z = 32.40m (Y=0.00m at street lip), perfectly abuts Corner_NE at X = 13.50m
            CreateRecessedRampUnit("Ramp_Corner_NE", parent,
                centerX: 14.70f, startZ: 34.00f, endZ: 32.40f, facingNorth: false, concreteMat, curbMat, yellowWarningMat);

            // Note: Ramp_Corner_NO is authentically constructed in ConstructNorthWestCornerAndAdminBuilding
            // as the complete universal accessibility ramp system with non-slip blue floor, yellow safety guide curbs,
            // double-level tubular steel handrails, and direct connection steps to the bridge concourse.
        }

        private static void CreateRecessedRampUnit(string name, Transform parent,
            float centerX, float startZ, float endZ, bool facingNorth,
            Material concreteMat, Material curbMat, Material yellowWarningMat)
        {
            GameObject rampRoot = new GameObject(name);
            rampRoot.transform.SetParent(parent);

            float slopeLen = Mathf.Abs(endZ - startZ);
            float midZ = (startZ + endZ) * 0.5f;

            // Tilt angle matching exact slope drop of 0.20m
            float tiltAngle = Mathf.Atan2(0.20f, slopeLen) * Mathf.Rad2Deg;
            if (!facingNorth) tiltAngle = -tiltAngle;

            float slabThick = 0.15f;
            float slabMidY = 0.10f - (slabThick * 0.5f) * Mathf.Cos(tiltAngle * Mathf.Deg2Rad);

            // 1. Sloped Ramp Slab (matches full CrosswalkWidth = 2.40m, spans between vertical sidewalk retention faces)
            GameObject rampSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampSlab.name = $"{name}_Slab";
            rampSlab.transform.SetParent(rampRoot.transform);
            rampSlab.transform.position = new Vector3(centerX, slabMidY, midZ);
            rampSlab.transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            rampSlab.transform.localScale = new Vector3(CrosswalkWidth, slabThick, slopeLen);
            SetMaterial(rampSlab, concreteMat);

            // 2. Yellow ADA/NOM Tactile Warning Pavers at street lip (0.50m deep x CrosswalkWidth)
            float zPaver = facingNorth ? (endZ - 0.25f) : (endZ + 0.25f);
            float paverMidY = (facingNorth ? 0.025f : 0.025f) + 0.002f;
            GameObject pavers = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pavers.name = $"{name}_Tactile_Pavers";
            pavers.transform.SetParent(rampRoot.transform);
            pavers.transform.position = new Vector3(centerX, paverMidY, zPaver);
            pavers.transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            pavers.transform.localScale = new Vector3(CrosswalkWidth, 0.012f, 0.50f);
            SetMaterial(pavers, yellowWarningMat);
            UnityEngine.Object.DestroyImmediate(pavers.GetComponent<Collider>());
        }

        // --- AUTHENTIC ROAD MARKINGS ---

        private static void ConstructMarkingsEast(Transform parent, Material stripeMat)
        {
            float angleEast = 8.0f;
            Quaternion rotEast = Quaternion.Euler(0f, angleEast, 0f);

            GameObject markingsRoot = new GameObject("Markings_Gardenias_East");
            markingsRoot.transform.SetParent(parent);
            markingsRoot.transform.position = new Vector3(22.58f, MarkingsY, 27.23f);
            markingsRoot.transform.rotation = rotEast;

            // 1. Dashed Lane Divider separating Parking Lane (South) from Transit Lane (North)
            // Transit lane = 4.0m wide (local Z: -0.60m to +3.40m)
            // Parking lane = 2.8m wide (local Z: -3.40m to -0.60m)
            for (float x = -9.0f; x <= 9.0f; x += 3.6f)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = $"LaneDivider_Dash_{x:F0}";
                dash.transform.SetParent(markingsRoot.transform);
                dash.transform.localPosition = new Vector3(x, 0f, -0.60f);
                dash.transform.localRotation = Quaternion.identity;
                dash.transform.localScale = new Vector3(1.8f, 0.012f, 0.15f);
                SetMaterial(dash, stripeMat);
                UnityEngine.Object.DestroyImmediate(dash.GetComponent<Collider>());
            }

            // 2. Parallel Parking Stall Lines along South Curb (3 stalls for parked cars)
            // Stall width = 2.4m, Stall length = 6.0m
            float[] stallXs = new float[] { -7.5f, -1.5f, 4.5f, 10.5f };
            for (int i = 0; i < stallXs.Length; i++)
            {
                GameObject stallLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stallLine.name = $"Parking_Stall_Line_{i}";
                stallLine.transform.SetParent(markingsRoot.transform);
                stallLine.transform.localPosition = new Vector3(stallXs[i], 0f, -2.00f);
                stallLine.transform.localRotation = Quaternion.identity;
                stallLine.transform.localScale = new Vector3(0.12f, 0.012f, 2.60f);
                SetMaterial(stallLine, stripeMat);
                UnityEngine.Object.DestroyImmediate(stallLine.GetComponent<Collider>());
            }

            // 3. Directional Arrow on North Transit Lane (pointing WEST toward Ave. Benito Juárez: local -X)
            GameObject arrowShaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowShaft.name = "Arrow_Transit_Shaft";
            arrowShaft.transform.SetParent(markingsRoot.transform);
            arrowShaft.transform.localPosition = new Vector3(2.5f, 0f, 1.40f);
            arrowShaft.transform.localRotation = Quaternion.identity;
            arrowShaft.transform.localScale = new Vector3(3.2f, 0.012f, 0.35f);
            SetMaterial(arrowShaft, stripeMat);
            UnityEngine.Object.DestroyImmediate(arrowShaft.GetComponent<Collider>());

            GameObject barbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barbL.name = "Arrow_Barb_L";
            barbL.transform.SetParent(markingsRoot.transform);
            barbL.transform.localPosition = new Vector3(1.2f, 0f, 1.85f);
            barbL.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
            barbL.transform.localScale = new Vector3(1.4f, 0.012f, 0.28f);
            SetMaterial(barbL, stripeMat);
            UnityEngine.Object.DestroyImmediate(barbL.GetComponent<Collider>());

            GameObject barbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barbR.name = "Arrow_Barb_R";
            barbR.transform.SetParent(markingsRoot.transform);
            barbR.transform.localPosition = new Vector3(1.2f, 0f, 0.95f);
            barbR.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            barbR.transform.localScale = new Vector3(1.4f, 0.012f, 0.28f);
            SetMaterial(barbR, stripeMat);
            UnityEngine.Object.DestroyImmediate(barbR.GetComponent<Collider>());
        }

        private static void ConstructMarkingsWest(Transform parent, Material stripeMat)
        {
            GameObject markingsRoot = new GameObject("Markings_Gardenias_West");
            markingsRoot.transform.SetParent(parent);
            markingsRoot.transform.position = Vector3.zero;

            // 1. Taxi Stand Lane Divider Line (starts at X = -18.4m westward so the first stall near the corner is removed)
            for (float x = -18.4f; x >= -33.5f; x -= 3.6f)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = $"TaxiLane_Divider_{Mathf.Abs(x):F0}";
                dash.transform.SetParent(markingsRoot.transform);
                dash.transform.position = new Vector3(x, MarkingsY, 27.40f);
                dash.transform.localScale = new Vector3(1.8f, 0.012f, 0.15f);
                SetMaterial(dash, stripeMat);
                UnityEngine.Object.DestroyImmediate(dash.GetComponent<Collider>());
            }

            // 2. Parallel Parking Stall Lines along South Yellow Curb (Sitio de Taxis, first stall at -11.5m removed)
            float[] stallXs = new float[] { -17.5f, -23.5f, -29.5f };
            for (int i = 0; i < stallXs.Length; i++)
            {
                GameObject stallLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stallLine.name = $"Taxi_Stall_Line_{i}";
                stallLine.transform.SetParent(markingsRoot.transform);
                stallLine.transform.position = new Vector3(stallXs[i], MarkingsY, 26.00f);
                stallLine.transform.localScale = new Vector3(0.12f, 0.012f, 2.60f);
                SetMaterial(stallLine, stripeMat);
                UnityEngine.Object.DestroyImmediate(stallLine.GetComponent<Collider>());
            }
        }

        private static void ConstructCrosswalks(Transform parent, Material stripeMat)
        {
            // Continental Zebra Crosswalks:
            // East: 6 traffic-aligned bars connecting Ramp_Corner_SE to Ramp_Corner_NE safely clear of tactile paver lips with 0 overlap
            CreateTrafficAlignedZebraStripeSet("Crosswalk_Gardenias_East", parent,
                startPos: new Vector3(9.50f, MarkingsY, 25.40f),
                endPos: new Vector3(14.70f, MarkingsY, 32.40f),
                stripeLengthAlongTraffic: 1.80f, stripeWidthAcrossTraffic: 0.45f,
                trafficYawDeg: 8.0f, stripeMat: stripeMat, barCount: 6, marginT: 0.12f);

            // West: 8 traffic-aligned bars connecting Ramp_Corner_SO to Ramp_Corner_NO safely clear of ramp lips with 0 overlap
            // Aligned parallel to West roadway traffic (0 deg) with clean asphalt gaps between bars!
            CreateTrafficAlignedZebraStripeSet("Crosswalk_Gardenias_West", parent,
                startPos: new Vector3(-5.15f, MarkingsY, 24.05f),
                endPos: new Vector3(-6.86f, MarkingsY, 34.97f),
                stripeLengthAlongTraffic: 1.80f, stripeWidthAcrossTraffic: 0.45f,
                trafficYawDeg: 0.0f, stripeMat: stripeMat, barCount: 8, marginT: 0.10f);
        }

        private static void CreateTrafficAlignedZebraStripeSet(string name, Transform parent,
            Vector3 startPos, Vector3 endPos, float stripeLengthAlongTraffic, float stripeWidthAcrossTraffic,
            float trafficYawDeg, Material stripeMat, int barCount, float marginT = 0.10f)
        {
            GameObject cwRoot = new GameObject(name);
            cwRoot.transform.SetParent(parent);

            Quaternion barRot = Quaternion.Euler(0f, trafficYawDeg, 0f);

            for (int i = 0; i < barCount; i++)
            {
                float t = Mathf.Lerp(marginT, 1.0f - marginT, (float)i / (barCount - 1));
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"{name}_Bar_{i}";
                bar.transform.SetParent(cwRoot.transform);
                bar.transform.position = pos;
                bar.transform.rotation = barRot;
                bar.transform.localScale = new Vector3(stripeLengthAlongTraffic, 0.012f, stripeWidthAcrossTraffic);
                SetMaterial(bar, stripeMat);
                UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
        }

        private static void CreateZebraStripeSet(string name, Transform parent,
            float centerX, float startZ, float endZ, float stripeWidthX, float stripeThicknessZ, Material stripeMat, int barCount = 9)
        {
            GameObject cwRoot = new GameObject(name);
            cwRoot.transform.SetParent(parent);

            float stepZ = (endZ - startZ) / (barCount - 1);

            for (int i = 0; i < barCount; i++)
            {
                float z = startZ + i * stepZ;
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"Zebra_Bar_{i}";
                bar.transform.SetParent(cwRoot.transform);
                bar.transform.position = new Vector3(centerX, MarkingsY, z);
                bar.transform.rotation = Quaternion.identity;
                bar.transform.localScale = new Vector3(stripeWidthX, 0.012f, stripeThicknessZ);
                SetMaterial(bar, stripeMat);
                UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
        }

        private static void CreateStreetNomenclaturePost(string name, Transform parent, Vector3 basePos, Material postMat, Material plaqueMat)
        {
            AssetDatabase.Refresh();

            Material gardeniasFaceMat = CreateOrGetSignTextureMaterial(
                "Assets/Settings/StreetSign_Gardenias_Face.mat",
                "Assets/Textures/Environment/T_StreetSign_Gardenias.png");
            Material juarezFaceMat = CreateOrGetSignTextureMaterial(
                "Assets/Settings/StreetSign_Juarez_Face.mat",
                "Assets/Textures/Environment/T_StreetSign_Juarez.png");

            GameObject signGo = new GameObject(name);
            signGo.transform.SetParent(parent);
            signGo.transform.position = basePos;

            // Metallic Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signGo.transform);
            pole.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
            SetMaterial(pole, postMat);
            UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

            // Street Plaque 1 ("C. Gardenias")
            Vector3 p1Pos = new Vector3(0.42f, 2.85f, 0f);
            Quaternion p1Rot = Quaternion.Euler(0f, 8.0f, 0f);
            GameObject p1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1.name = "Plaque_Gardenias";
            p1.transform.SetParent(signGo.transform);
            p1.transform.localPosition = p1Pos;
            p1.transform.localRotation = p1Rot;
            p1.transform.localScale = new Vector3(0.85f, 0.22f, 0.04f);
            SetMaterial(p1, plaqueMat);
            UnityEngine.Object.DestroyImmediate(p1.GetComponent<Collider>());

            // Double-sided text faces for "C. Gardenias"
            AttachDoubleSidedPlaqueFaces("Plaque_Gardenias", signGo.transform, p1Pos, p1Rot, 0.84f, 0.21f, 0.021f, gardeniasFaceMat);

            // Street Plaque 2 ("Ave. B. Juarez")
            Vector3 p2Pos = new Vector3(0f, 3.08f, 0.42f);
            Quaternion p2Rot = Quaternion.Euler(0f, 90.0f, 0f);
            GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p2.name = "Plaque_Juarez";
            p2.transform.SetParent(signGo.transform);
            p2.transform.localPosition = p2Pos;
            p2.transform.localRotation = p2Rot;
            p2.transform.localScale = new Vector3(0.85f, 0.22f, 0.04f);
            SetMaterial(p2, plaqueMat);
            UnityEngine.Object.DestroyImmediate(p2.GetComponent<Collider>());

            // Double-sided text faces for "Ave. B. Juarez"
            AttachDoubleSidedPlaqueFaces("Plaque_Juarez", signGo.transform, p2Pos, p2Rot, 0.84f, 0.21f, 0.021f, juarezFaceMat);
        }

        private static void AttachDoubleSidedPlaqueFaces(string prefix, Transform parent, Vector3 centerPos, Quaternion rot,
            float width, float height, float halfThickOffset, Material faceMat)
        {
            // Front Face (-Z local of plaque rotation)
            GameObject frontQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontQuad.name = $"{prefix}_Face_Front";
            frontQuad.transform.SetParent(parent);
            frontQuad.transform.localPosition = centerPos + rot * new Vector3(0f, 0f, -halfThickOffset);
            frontQuad.transform.localRotation = rot;
            frontQuad.transform.localScale = new Vector3(width, height, 1.0f);
            SetMaterial(frontQuad, faceMat);
            UnityEngine.Object.DestroyImmediate(frontQuad.GetComponent<Collider>());

            // Back Face (+Z local of plaque rotation)
            GameObject backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backQuad.name = $"{prefix}_Face_Back";
            backQuad.transform.SetParent(parent);
            backQuad.transform.localPosition = centerPos + rot * new Vector3(0f, 0f, halfThickOffset);
            backQuad.transform.localRotation = rot * Quaternion.Euler(0f, 180f, 0f);
            backQuad.transform.localScale = new Vector3(width, height, 1.0f);
            SetMaterial(backQuad, faceMat);
            UnityEngine.Object.DestroyImmediate(backQuad.GetComponent<Collider>());
        }

        private static Material CreateOrGetSignTextureMaterial(string matPath, string texPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", 0.25f);
            mat.SetColor("_EmissionColor", new Color(0.14f, 0.14f, 0.14f));
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // --- VERIFICATION & DIAGNOSTICS ---

        public static void VerifyWalkingColliders()
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics.Simulate(0.02f);

            int voidCount = 0;
            int hitCount = 0;

            // 1. Test pedestrian crosswalk corridor on East side (Z = 24.5m to 33.5m)
            for (float t = 0f; t <= 1.0f; t += 0.05f)
            {
                Vector3 center = Vector3.Lerp(new Vector3(9.50f, 2.0f, 25.40f), new Vector3(14.70f, 2.0f, 32.40f), t);
                for (float offset = -1.0f; offset <= 1.0f; offset += 0.5f)
                {
                    Vector3 origin = center + new Vector3(offset, 0f, 0f);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f)) hitCount++;
                    else { voidCount++; Debug.LogError($"[VOID_GAP] East Crosswalk void gap at {origin}"); }
                }
            }

            // 2. Test pedestrian crosswalk corridor on West side (Z = 23.5m to 35.5m)
            for (float t = 0f; t <= 1.0f; t += 0.05f)
            {
                Vector3 center = Vector3.Lerp(new Vector3(-5.15f, 2.0f, 24.05f), new Vector3(-6.86f, 2.0f, 34.97f), t);
                for (float offset = -1.0f; offset <= 1.0f; offset += 0.5f)
                {
                    Vector3 origin = center + new Vector3(offset, 0f, 0f);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f)) hitCount++;
                    else { voidCount++; Debug.LogError($"[VOID_GAP] West Crosswalk void gap at {origin}"); }
                }
            }

            // 3. Test roadway across the intersection (X = -25.0m to +25.0m, Z = 22.0m to 32.0m)
            for (float x = -25.0f; x <= 25.0f; x += 1.0f)
            {
                for (float z = 22.5f; z <= 31.5f; z += 0.5f)
                {
                    Vector3 origin = new Vector3(x, 2.0f, z);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f))
                    {
                        hitCount++;
                    }
                    else
                    {
                        voidCount++;
                        Debug.LogError($"[VOID_GAP] Roadway void gap at X={x:F2}, Z={z:F2}");
                    }
                }
            }

            // 4. Test pedestrian plaza on North-West corner (X = -16.5m to -4.0m, Z = 35.5m to 43.5m)
            for (float x = -16.5f; x <= -4.0f; x += 0.35f)
            {
                for (float z = 35.5f; z <= 43.5f; z += 0.35f)
                {
                    Vector3 origin = new Vector3(x, 3.0f, z);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f))
                    {
                        hitCount++;
                    }
                    else
                    {
                        voidCount++;
                        Debug.LogError($"[VOID_GAP] North-West Plaza void gap at X={x:F2}, Z={z:F2}");
                    }
                }
            }

            // 5. Test Widened West Sidewalk Promenade (X = -11.5m to -3.0m, Z = -20.0m to 20.0m)
            for (float x = -11.5f; x <= -3.0f; x += 1.0f)
            {
                for (float z = -20.0f; z <= 20.0f; z += 2.0f)
                {
                    Vector3 origin = new Vector3(x, 2.0f, z);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f))
                    {
                        hitCount++;
                    }
                    else
                    {
                        voidCount++;
                        Debug.LogError($"[VOID_GAP] Widened West Sidewalk void gap at X={x:F2}, Z={z:F2}");
                    }
                }
            }

            // 6. Test East Pedestrian Promenade (X = 12.5m to 19.0m, Z = 35.0m to 48.0m)
            for (float x = 12.5f; x <= 19.0f; x += 0.5f)
            {
                for (float z = 35.0f; z <= 48.0f; z += 1.0f)
                {
                    Vector3 origin = new Vector3(x, 2.0f, z);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10.0f))
                    {
                        hitCount++;
                    }
                    else
                    {
                        voidCount++;
                        Debug.LogError($"[VOID_GAP] East Promenade void gap at X={x:F2}, Z={z:F2}");
                    }
                }
            }

            Debug.Log($"[RAYCAST_SUMMARY] Total Hits: {hitCount}, Void Gaps: {voidCount}");
        }

        public static void CaptureVerificationScreenshots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("BorderCorridorScene.unity"))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // Consistent screenshots directory in project root scratch/screenshots
            string projectDir = Directory.GetCurrentDirectory();
            string outDir = Path.Combine(projectDir, "scratch", "screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("GardeniasVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Elevated Panoramic Overview of the Intersection
            cam.fieldOfView = 62f;
            cam.transform.position = new Vector3(2.0f, 16.0f, 6.0f);
            cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_intersection_aerial_overview.png"));

            // Shot 2: Close-Up of Calle Gardenias Este (Parking Lane, Transit Lane, Ramp, Curiosidades Azteca corner)
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(3.2f, 2.2f, 19.5f);
            cam.transform.rotation = Quaternion.Euler(-1.5f, 58f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_east_parking_and_ramp.png"));

            // Shot 3: Close-Up of Calle Gardenias Oeste (Dental Jarquín corner, rounded radius, yellow curb, and ramp)
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(-3.2f, 2.2f, 19.5f);
            cam.transform.rotation = Quaternion.Euler(-1.5f, -58f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_west_corner_and_ramp.png"));

            // Shot 4: Full Corridor South-to-North View
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(0.0f, 2.8f, -15.0f);
            cam.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "corridor_full_overview_with_gardenias.png"));

            // Shot 5: Exact View of User Screenshot (Curiosidades Azteca corner, ramp, and Calle Gardenias East sidewalk)
            cam.fieldOfView = 55f;
            cam.transform.position = new Vector3(5.5f, 1.8f, 17.5f);
            cam.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_east_curiosidades_corner_user_view.png"));

            // Shot 6: Direct Ground-Level Close-Up of Curiosidades Azteca Corner, Ramp Lip, and Gardenias South Curb
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(6.8f, 1.4f, 21.0f);
            cam.transform.rotation = Quaternion.Euler(4.0f, 52f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_east_curiosidades_corner_close_up.png"));

            // Shot 7: Direct Ground-Level Close-Up of Elena Ramos Corner (Corner_NE), North Ramp Lip, and Gardenias North Curb
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(6.8f, 1.4f, 36.5f);
            cam.transform.rotation = Quaternion.Euler(4.0f, 128f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_east_elena_corner_close_up.png"));

            // Shot 7B: Exact View of User Screenshot (East Entrance to Bridge, Elena Kiosk, Pedestrian Queue, Lane 4, Crosswalk [N 021°])
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(9.2f, 1.68f, 22.5f);
            cam.transform.rotation = Quaternion.Euler(0.0f, 21.0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "bridge_east_entrance_reconstruction_verification.png"));

            // Shot 8: Exact View of User Screenshot (Dental Jarquín corner, 45° chamfer, cantilevered canopy, yellow curb, ramp)
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(-6.8f, 1.65f, 27.0f);
            cam.transform.rotation = Quaternion.Euler(6.0f, -139f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_west_dental_jarquin_user_view.png"));

            // Shot 9: User's Street Perspective Looking West Down Calle Gardenias (W 269°)
            cam.fieldOfView = 55f;
            cam.transform.position = new Vector3(0.0f, 1.8f, 20.0f);
            cam.transform.rotation = Quaternion.Euler(0.0f, -91f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_west_street_entry_user_view.png"));

            // Shot 10: Perspective Down Calle Gardenias West Showing Taxis and Bifurcation Island
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-10.0f, 1.9f, 23.0f);
            cam.transform.rotation = Quaternion.Euler(2.0f, -80f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_west_taxis_and_bifurcation_view.png"));

            // Shot 11: Exact View of User Screenshot (North-West corner: Admin Building, Accessibility Ramp with handrails, Yellow Steps, V-plaza & diagonal street)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-5.20f, 1.65f, 23.80f);
            cam.transform.rotation = Quaternion.Euler(2.0f, -38.0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_nw_admin_and_ramp.png"));

            // Shot 12: Close-Up View from Under Bridge Canopy matching the real Google Maps photo (looking WNW across the V-plaza, steps, ramp & diagonal street)
            cam.fieldOfView = 68f;
            cam.transform.position = new Vector3(-6.20f, 1.85f, 36.20f);
            cam.transform.rotation = Quaternion.Euler(6.0f, -62.0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_nw_canopy_real_photo_match.png"));

            // Shot 13: Exact View of User Screenshot (Looking South down Av. Benito Juárez [S 184°], roadway without centerlines or south crosswalk, showing East parking stalls)
            cam.fieldOfView = 65f;
            cam.transform.position = new Vector3(0.0f, 1.65f, 5.0f);
            cam.transform.rotation = Quaternion.Euler(0f, 184f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "ave_juarez_south_user_view.png"));

            // Shot 14: Angled View of East Sidewalk Parking Stalls along Av. Benito Juárez
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(-1.5f, 2.0f, 18.0f);
            cam.transform.rotation = Quaternion.Euler(4f, 162f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "ave_juarez_east_parking_stalls_view.png"));

            // Shot 15: Close-Up View of Dual-Head Parking Meter (Center Meter @ Z = 0m, showing both heads, badges, screens and stall line)
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(6.30f, 1.45f, 1.30f);
            cam.transform.rotation = Quaternion.Euler(5.5f, 144f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "ave_juarez_parking_meter_closeup.png"));

            // Shot 16: Perspective Down East Sidewalk Showing All 3 Dual-Head Parking Meters
            cam.fieldOfView = 55f;
            cam.transform.position = new Vector3(7.85f, 1.65f, 15.50f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 183f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "ave_juarez_east_meters_perspective.png"));

            // Shot 17: Close-Up of Dental Jarquín Corner with Widened West Sidewalk (X = -2.60m to Z = 24.30m)
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(-3.80f, 1.65f, 26.00f);
            cam.transform.rotation = Quaternion.Euler(6.0f, -148f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_west_dental_jarquin_widened_corner.png"));

            // Shot 18: Perspective Down the Widened West Pedestrian Promenade (9.40m wide sidewalk looking South)
            cam.fieldOfView = 65f;
            cam.transform.position = new Vector3(-7.50f, 1.65f, 18.00f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 180f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "ave_juarez_west_sidewalk_promenade_view.png"));

            // Shot 19: Perspective Looking North from Av. Benito Juárez Toward Bridge Paso del Norte (4 Open Arches)
            cam.fieldOfView = 65f;
            cam.transform.position = new Vector3(0.90f, 1.65f, 10.00f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 0.0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "gardenias_north_bridge_approach_view.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log($"[CalleGardeniasBuilder] Verification screenshots generated successfully in '{outDir}'!");
        }

        private static GameObject CreateNorthTrapezoidSidewalkPrism(string name, Transform parent,
            float xWest, float xEast, float zNorth, float zSouthWest, float zSouthEast, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";

            float yTop = SidewalkTopY; // 0.20m
            float yBot = 0.00f;

            Vector3 pNW_T = new Vector3(xWest, yTop, zNorth);
            Vector3 pNE_T = new Vector3(xEast, yTop, zNorth);
            Vector3 pSE_T = new Vector3(xEast, yTop, zSouthEast);
            Vector3 pSW_T = new Vector3(xWest, yTop, zSouthWest);
            Vector3 pNW_B = new Vector3(xWest, yBot, zNorth);
            Vector3 pNE_B = new Vector3(xEast, yBot, zNorth);
            Vector3 pSE_B = new Vector3(xEast, yBot, zSouthEast);
            Vector3 pSW_B = new Vector3(xWest, yBot, zSouthWest);

            // Use 24 unshared vertices (4 per face, like PrimitiveType.Cube) so top normals are strictly (0,1,0) with zero shading seam
            Vector3[] v = new Vector3[]
            {
                // Top (+Y): 0..3
                pNW_T, pNE_T, pSE_T, pSW_T,
                // Bottom (-Y): 4..7
                pNW_B, pSW_B, pSE_B, pNE_B,
                // North (+Z): 8..11
                pNW_T, pNE_T, pNE_B, pNW_B,
                // South (-Z): 12..15
                pSW_T, pSE_T, pSE_B, pSW_B,
                // West (-X): 16..19
                pNW_T, pSW_T, pSW_B, pNW_B,
                // East (+X): 20..23
                pSE_T, pNE_T, pNE_B, pSE_B
            };

            int[] triangles = new int[]
            {
                0, 1, 2,    0, 2, 3,
                4, 5, 6,    4, 6, 7,
                8, 9, 10,   8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            Vector2[] uvs = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                uvs[i] = new Vector2(v[i].x * 0.5f, v[i].z * 0.5f);
            }

            mesh.vertices = v;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;

            return go;
        }

        private static GameObject CreateTriangularSidewalkPrism(string name, Transform parent,
            Vector2 p1, Vector2 p2, Vector2 p3, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh { name = $"{name}_Mesh" };
            float yTop = SidewalkTopY; // 0.20m
            float yBot = 0.00f;

            Vector3[] v = new Vector3[]
            {
                new Vector3(p1.x, yTop, p1.y), // 0: Top P1
                new Vector3(p2.x, yTop, p2.y), // 1: Top P2
                new Vector3(p3.x, yTop, p3.y), // 2: Top P3
                new Vector3(p1.x, yBot, p1.y), // 3: Bot P1
                new Vector3(p2.x, yBot, p2.y), // 4: Bot P2
                new Vector3(p3.x, yBot, p3.y)  // 5: Bot P3
            };

            int[] triangles = new int[]
            {
                // Top (+Y)
                0, 1, 2,
                // Bottom (-Y)
                3, 5, 4,
                // Side 0-1 (along chamfer)
                0, 4, 1,  0, 3, 4,
                // Side 1-2 (along north face)
                1, 5, 2,  1, 4, 5,
                // Side 2-0 (along east face)
                2, 3, 0,  2, 5, 3
            };

            Vector2[] uvs = new Vector2[6];
            for (int i = 0; i < 6; i++) uvs[i] = new Vector2(v[i].x * 0.5f, v[i].z * 0.5f);

            mesh.vertices = v;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;

            return go;
        }

        private static GameObject CreateTrapezoidSidewalkPrism(string name, Transform parent,
            float xWest, float xEast, float zSouth, float zNorthWest, float zNorthEast, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";

            float yTop = SidewalkTopY; // 0.20m
            float yBot = 0.00f;

            Vector3 pSW_T = new Vector3(xWest, yTop, zSouth);
            Vector3 pNW_T = new Vector3(xWest, yTop, zNorthWest);
            Vector3 pNE_T = new Vector3(xEast, yTop, zNorthEast);
            Vector3 pSE_T = new Vector3(xEast, yTop, zSouth);
            Vector3 pSW_B = new Vector3(xWest, yBot, zSouth);
            Vector3 pNW_B = new Vector3(xWest, yBot, zNorthWest);
            Vector3 pNE_B = new Vector3(xEast, yBot, zNorthEast);
            Vector3 pSE_B = new Vector3(xEast, yBot, zSouth);

            Vector3[] v = new Vector3[]
            {
                // Top (+Y): 0..3
                pSW_T, pNW_T, pNE_T, pSE_T,
                // Bottom (-Y): 4..7
                pSW_B, pSE_B, pNE_B, pNW_B,
                // North (along curb): 8..11
                pNW_T, pNW_B, pNE_B, pNE_T,
                // South (along building): 12..15
                pSE_T, pSE_B, pSW_B, pSW_T,
                // West: 16..19
                pSW_T, pSW_B, pNW_B, pNW_T,
                // East: 20..23
                pNE_T, pNE_B, pSE_B, pSE_T
            };

            int[] triangles = new int[]
            {
                0, 1, 2,    0, 2, 3,
                4, 5, 6,    4, 6, 7,
                8, 9, 10,   8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            Vector2[] uvs = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                uvs[i] = new Vector2(v[i].x * 0.5f, v[i].z * 0.5f);
            }

            mesh.vertices = v;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;

            return go;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            SetMaterial(go, mat);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
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
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void RenderToFile(Camera cam, int width, int height, string filePath)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(filePath, png);
            Debug.Log($"[CalleGardeniasBuilder] Saved screenshot: {filePath}");
        }
    }
}
