using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Reconstructs the West bridge complex triangular lot ("pico"), masonry perimeter wall,
    /// interior garden, front diagonal avenue, relocated Street_Abasolo (Z = 54..64m), and
    /// converging Y-junction to closely match real aerial imagery of Puente Paso del Norte.
    /// Safe & idempotent: preserves East/West walkways, crosswalks, gantry sign, and toll plaza.
    /// </summary>
    public static class WestTriangleAndAbasoloBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string ArtifactDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";

        // Elevation standards (0 Z-fighting)
        private const float RoadTopY = 0.00f;
        private const float RoadBotY = -0.10f;
        private const float MarkingsY = 0.012f;
        private const float SidewalkTopY = 0.20f;
        private const float SidewalkBotY = 0.00f;

        // NW Diagonal Curb direction (matches CalleGardeniasBuilder P6..P9: dz/dx = -0.55)
        // Line through (-12.20, 37.98) and (-31.00, 48.32) => 28.8108 deg north of -X
        private const float DiagSlopeDzDx = -0.55f;
        private const float DiagYawDeg = 28.8107937f;
        private static readonly Vector2 DirWNW = new Vector2(-18.80f, 10.34f).normalized;     // (-0.876217, +0.481918)
        private static readonly Vector2 NormalIn = new Vector2(DirWNW.y, -DirWNW.x);          // (+0.481918, +0.876217) into triangle
        private static readonly Vector2 NormalOut = -NormalIn;                                 // (-0.481918, -0.876217) into street

        // Relocated Street_Abasolo corridor (directly north of AdminBuilding / behind canopy)
        private const float AbasoloSouthZ = 54.00f;
        private const float AbasoloNorthZ = 64.00f;
        private const float AbasoloCenterZ = 59.00f;
        private const float AbasoloWidth = 10.00f;

        // Front Diagonal Road width (perpendicular to 28.81 deg curb)
        private const float DiagRoadWidth = 10.50f;

        [MenuItem("MyVirtualCommunity/Rebuild West Triangle And Abasolo")]
        public static void ExecuteRebuild()
        {
            _cachedOsm = null;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestTriangleAndAbasoloBuilder] Cannot open scene: {ScenePath}");
                return;
            }

            // Load materials cleanly from project Settings
            Material asphaltMat = LoadMat("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.18f, 0.20f));
            Material concreteMat = LoadMat("Assets/Settings/ConcreteMaterial.mat", new Color(0.68f, 0.67f, 0.64f));
            Material curbMat = LoadMat("Assets/Settings/CurbMaterial.mat", new Color(0.82f, 0.80f, 0.78f));
            Material whiteStripeMat = LoadMat("Assets/Settings/WhiteStripeMaterial.mat", new Color(0.95f, 0.95f, 0.95f));
            Material whiteWallMat = LoadMat("Assets/Settings/West_WeatheredCream.mat", new Color(0.92f, 0.91f, 0.89f));
            Material zocaloMat = LoadMat("Assets/Settings/West_DarkZocalo.mat", new Color(0.24f, 0.25f, 0.27f));
            Material redStripeMat = LoadMat("Assets/Settings/AdminRedStripe.mat", new Color(0.68f, 0.12f, 0.12f));
            Material foliageMat = LoadMat("Assets/Settings/TreeFoliage.mat", new Color(0.18f, 0.45f, 0.15f));
            Material bridgeWhiteMat = LoadMat("Assets/Settings/BridgeWhiteConcrete.mat", new Color(0.88f, 0.88f, 0.86f));
            Material bridgeSteelMat = LoadMat("Assets/Settings/BridgeSteel.mat", new Color(0.45f, 0.47f, 0.50f));
            Material darkTrimMat = LoadMat("Assets/Settings/West_DarkCornice.mat", new Color(0.20f, 0.22f, 0.24f));
            Material darkGlassMat = LoadMat("Assets/Settings/West_DarkGlass.mat", new Color(0.12f, 0.16f, 0.20f));
            Material whiteFrameMat = LoadMat("Assets/Settings/WhiteFrameMaterial.mat", new Color(0.95f, 0.95f, 0.95f));
            Material blueSignMat = LoadMat("Assets/Settings/BenavidesBlue.mat", new Color(0.08f, 0.25f, 0.55f));
            Material bollardOrangeMat = LoadMat("Assets/Settings/BollardOrange.mat", new Color(0.95f, 0.42f, 0.08f));
            Material woodMat = CreateOrGetMat("Assets/Settings/TreeTrunkBrown.mat", new Color(0.32f, 0.21f, 0.14f), 0.0f);
            Material turfMat = CloneMatWithTint(foliageMat, "Assets/Settings/West_GardenTurf.mat", new Color(0.20f, 0.34f, 0.14f));
            Material shrubDarkMat = CloneMatWithTint(foliageMat, "Assets/Settings/West_ShrubDarkGreen.mat", new Color(0.12f, 0.32f, 0.11f));
            Material urbanLotMat = CreateOrGetMat("Assets/Settings/West_UrbanLotSurface.mat", new Color(0.36f, 0.35f, 0.34f), 0.05f);
            Material greenBandMat = CreateOrGetMat("Assets/Settings/West_AdminGreenBand.mat", new Color(0.08f, 0.42f, 0.20f), 0.10f);
            Material metalGateMat = CreateOrGetMat("Assets/Settings/West_MetalGateGrey.mat", new Color(0.42f, 0.44f, 0.46f), 0.35f);
            Material yellowStripeMat = CreateOrGetMat("Assets/Settings/West_YellowStripe.mat", new Color(0.95f, 0.76f, 0.08f), 0.05f);
            AssetDatabase.SaveAssets();

            // Step 0: Reconstruct AdminBuilding_West_Inspection recessed behind the diagonal curb (matches Google Street View!)
            RebuildWestAdminBuilding(concreteMat, whiteWallMat, zocaloMat, darkTrimMat, redStripeMat, greenBandMat,
                whiteFrameMat, darkGlassMat, bridgeSteelMat, blueSignMat);

            // Inspect AdminBuilding_West_Inspection runtime bounds for exact wall snapping
            Bounds adminBounds = GetHierarchyBounds("AdminBuilding_West_Inspection");
            Debug.Log($"[WestTriangleAndAbasoloBuilder] AdminBuilding bounds: min={adminBounds.min}, max={adminBounds.max}");

            // Step 1: Extend NW triangle sidewalk to tip ("pico"), build perimeter wall + metal gate + garden
            BuildNorthWestTriangleAndWall(adminBounds, concreteMat, curbMat, whiteWallMat, zocaloMat,
                redStripeMat, greenBandMat, metalGateMat, foliageMat, shrubDarkMat, woodMat, turfMat, bridgeSteelMat, bollardOrangeMat);

            // Step 2: Relocate Street_Abasolo (widened to 3 lanes from s=-40 Eastward) + yellow dividers + arrows + talud
            RelocateStreetAbasolo(asphaltMat, curbMat, whiteStripeMat, yellowStripeMat, concreteMat, bridgeWhiteMat);

            // Step 3: Rebuild West diagonal avenue + Y-junction + urban sidewalk wedge
            BuildDiagonalStreetAndYJunction(asphaltMat, concreteMat, curbMat, whiteStripeMat, urbanLotMat);

            // Step 4: Full-scale East junction (Abasolo, curved Gardenias East, Francisco Villa corner, elongated delimited block)
            BuildEastAbasoloGardeniasJunction(asphaltMat, concreteMat, curbMat, whiteStripeMat, yellowStripeMat, urbanLotMat);

            // Step 4B: Update Gardenias East curve + Central Block + Calle Azucenas East + delimited irregular Manzana
            BuildEastAzucenasAndManzana(asphaltMat, concreteMat, curbMat, whiteStripeMat, yellowStripeMat, urbanLotMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[WestTriangleAndAbasoloBuilder] Saved BorderCorridorScene.unity cleanly.");

            // Step 5: Capture verification views
            CaptureVerificationScreenshots();
        }

        // =========================================================================
        // STEP 0: AUTHENTIC RECESSED WEST ADMIN BUILDING (MATCHING STREET VIEW)
        // =========================================================================

        private static void RebuildWestAdminBuilding(
            Material concreteMat, Material whiteWallMat, Material zocaloMat, Material darkTrimMat,
            Material redStripeMat, Material greenBandMat, Material whiteFrameMat, Material darkGlassMat,
            Material steelMat, Material blueSignMat)
        {
            GameObject nwRoot = GameObject.Find("Corner_NO_Organic_Root");
            if (nwRoot == null) return;

            GameObject oldAdmin = GameObject.Find("AdminBuilding_West_Inspection");
            if (oldAdmin != null) UnityEngine.Object.DestroyImmediate(oldAdmin);

            // Origin sits cleanly behind sidewalk & wheelchair ramp: Main Wing X = -14.60..-10.80, Z = 42.60..52.80
            GameObject adminGo = new GameObject("AdminBuilding_West_Inspection");
            adminGo.transform.SetParent(nwRoot.transform);
            adminGo.transform.position = new Vector3(-12.70f, 0f, 47.70f);

            // --- 1. MAIN EAST WING (Projects south cleanly to Z = 42.60m, ~3.3m north of diagonal curb!) ---
            // Continuous municipal stripe height across Main Wing -> West Wing -> Gate -> Diagonal Wall: zocalo top Y=1.16m
            CreateBox("Admin_Zocalo", adminGo.transform,
                new Vector3(-12.70f, 0.58f, 47.70f), new Vector3(3.80f, 1.16f, 10.20f), zocaloMat, keepCollider: true);
            CreateBox("Admin_Stripe_Green", adminGo.transform,
                new Vector3(-12.70f, 1.20f, 47.70f), new Vector3(3.86f, 0.08f, 10.26f), greenBandMat);
            CreateBox("Admin_Stripe_Red", adminGo.transform,
                new Vector3(-12.70f, 1.28f, 47.70f), new Vector3(3.86f, 0.08f, 10.26f), redStripeMat);
            CreateBox("Admin_UpperWalls", adminGo.transform,
                new Vector3(-12.70f, 2.70f, 47.70f), new Vector3(3.80f, 2.76f, 10.20f), whiteWallMat, keepCollider: true);
            CreateBox("Admin_RoofSlab", adminGo.transform,
                new Vector3(-12.70f, 4.16f, 47.70f), new Vector3(4.02f, 0.16f, 10.42f), darkTrimMat, keepCollider: true);
            CreateBox("Admin_Parapet_South", adminGo.transform,
                new Vector3(-12.70f, 4.35f, 42.60f), new Vector3(4.02f, 0.24f, 0.20f), whiteWallMat);
            CreateBox("Admin_Parapet_North", adminGo.transform,
                new Vector3(-12.70f, 4.35f, 52.80f), new Vector3(4.02f, 0.24f, 0.20f), whiteWallMat);
            CreateBox("Admin_Parapet_East", adminGo.transform,
                new Vector3(-10.80f, 4.35f, 47.70f), new Vector3(0.20f, 0.24f, 10.42f), whiteWallMat);

            // --- 2. RECESSED WEST WING (Stepped ~2.0m NORTHWARD to Z = 44.60..52.72m, X = -18.40..-14.60m!) ---
            // Leaves ~2.8-3.2m completely clear sidewalk in front of both West Wing & exterior stairs!
            CreateBox("Admin_WestWing_Zocalo", adminGo.transform,
                new Vector3(-16.50f, 0.58f, 48.66f), new Vector3(3.80f, 1.16f, 8.12f), zocaloMat, keepCollider: true);
            CreateBox("Admin_WestWing_Stripe_Green", adminGo.transform,
                new Vector3(-16.50f, 1.20f, 48.66f), new Vector3(3.86f, 0.08f, 8.18f), greenBandMat);
            CreateBox("Admin_WestWing_Stripe_Red", adminGo.transform,
                new Vector3(-16.50f, 1.28f, 48.66f), new Vector3(3.86f, 0.08f, 8.18f), redStripeMat);
            CreateBox("Admin_WestWing_UpperWalls", adminGo.transform,
                new Vector3(-16.50f, 2.38f, 48.66f), new Vector3(3.80f, 2.12f, 8.12f), whiteWallMat, keepCollider: true);
            CreateBox("Admin_WestWing_RoofSlab", adminGo.transform,
                new Vector3(-16.50f, 3.52f, 48.66f), new Vector3(4.00f, 0.16f, 8.32f), darkTrimMat, keepCollider: true);
            CreateBox("Admin_WestWing_Parapet_South", adminGo.transform,
                new Vector3(-16.50f, 3.70f, 44.60f), new Vector3(4.00f, 0.22f, 0.20f), whiteWallMat);
            CreateBox("Admin_WestWing_Parapet_West", adminGo.transform,
                new Vector3(-18.40f, 3.70f, 48.66f), new Vector3(0.20f, 0.22f, 8.32f), whiteWallMat);

            // --- 3. EXTERIOR STAIRCASE, LANDING & UNDER-STAIR CABINET (visible in Google Street View photo!) ---
            // Ascends left-to-right from sidewalk (X=-17.65, Y=0.20) to landing (X=-15.70, Y=1.18) in front of doorway
            int steps = 7;
            for (int s = 0; s < steps; s++)
            {
                float t = (s + 0.5f) / steps;
                float stepTopY = Mathf.Lerp(0.32f, 1.18f, (float)(s + 1) / steps);
                float sx = Mathf.Lerp(-17.62f, -15.80f, t);
                CreateBox($"WestWing_StairStep_{s + 1}", adminGo.transform,
                    new Vector3(sx, 0.10f + (stepTopY - 0.10f) * 0.5f, 44.15f),
                    new Vector3(0.27f, stepTopY - 0.10f, 0.88f), concreteMat, keepCollider: true);
            }
            // Sloped corrugated grey under-stair panel matching Street View photo (tucks cleanly inside steps with zero Z-fighting)
            CreateBox("WestWing_StairUnderMask", adminGo.transform,
                new Vector3(-16.58f, 0.44f, 43.735f), new Vector3(1.52f, 0.48f, 0.04f), darkTrimMat);

            // Upper doorway landing in front of West Wing door (X=-15.80..-14.60, Z=43.71..44.60, top Y=1.18m)
            CreateBox("WestWing_StairLanding", adminGo.transform,
                new Vector3(-15.20f, 0.67f, 44.15f), new Vector3(1.20f, 0.94f, 0.88f), concreteMat, keepCollider: true);
            CreateBox("WestWing_LandingTop", adminGo.transform,
                new Vector3(-15.20f, 1.18f, 44.15f), new Vector3(1.24f, 0.08f, 0.92f), concreteMat, keepCollider: true);
            // Dark grey under-stair utility door (flush in front of landing block, X=-15.42..-14.70, zero Z-fighting)
            CreateBox("WestWing_UnderStairDoor", adminGo.transform,
                new Vector3(-15.06f, 0.67f, 43.68f), new Vector3(0.72f, 0.90f, 0.06f), darkTrimMat);
            CreateBox("WestWing_UnderStairDoorFrame", adminGo.transform,
                new Vector3(-15.06f, 0.67f, 43.70f), new Vector3(0.80f, 0.96f, 0.04f), steelMat);
            // Upper white entrance door on West Wing South facade
            CreateBox("WestWing_UpperDoorFrame", adminGo.transform,
                new Vector3(-15.20f, 2.22f, 44.58f), new Vector3(0.95f, 1.98f, 0.08f), whiteFrameMat);
            CreateBox("WestWing_UpperDoorGlass", adminGo.transform,
                new Vector3(-15.20f, 2.55f, 44.54f), new Vector3(0.55f, 0.80f, 0.05f), darkGlassMat);
            // Steel handrail along stair & landing outer edge (Z = 43.74m) with intermediate balusters
            ConstructSimpleRail("WestWing_StairHandrail_Slope", adminGo.transform,
                new Vector3(-17.64f, 0.22f, 43.74f), new Vector3(-15.80f, 1.22f, 43.74f), steelMat, intermediatePosts: 3);
            ConstructSimpleRail("WestWing_StairHandrail_Landing", adminGo.transform,
                new Vector3(-15.80f, 1.22f, 43.74f), new Vector3(-14.62f, 1.22f, 43.74f), steelMat, intermediatePosts: 1);

            // --- 4. BARRED WINDOWS ---
            CreateBarredWin("Admin_Window_Main_South", adminGo.transform,
                new Vector3(-12.70f, 2.35f, 42.58f), Quaternion.identity, 1.65f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);
            CreateBarredWin("Admin_Window_WestWing_South", adminGo.transform,
                new Vector3(-17.20f, 2.25f, 44.58f), Quaternion.identity, 1.35f, 0.95f, 5, whiteFrameMat, darkGlassMat, steelMat);
            CreateBarredWin("Admin_Window_East_1", adminGo.transform,
                new Vector3(-10.78f, 2.35f, 44.80f), Quaternion.Euler(0f, -90f, 0f), 1.60f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);
            CreateBarredWin("Admin_Window_East_2", adminGo.transform,
                new Vector3(-10.78f, 2.35f, 48.20f), Quaternion.Euler(0f, -90f, 0f), 1.60f, 1.10f, 6, whiteFrameMat, darkGlassMat, steelMat);

            // --- 5. ACCESSIBILITY PLAQUE ON MAIN WING SW CORNER & CURB CONTROL PEDESTAL ---
            GameObject signA11y = new GameObject("Admin_Sign_Accessibility");
            signA11y.transform.SetParent(adminGo.transform);
            signA11y.transform.position = new Vector3(-14.05f, 2.25f, 42.56f);
            CreateBox("SignA11y_Backing", signA11y.transform, signA11y.transform.position, new Vector3(0.58f, 0.68f, 0.06f), blueSignMat);

            // Slender dark parking meter / card pedestal positioned cleanly along outer sidewalk curb between ramp & stairs (clear of ramp rails!)
            Vector2 pedPt = new Vector2(-15.05f, 37.98f + DiagSlopeDzDx * (-15.05f + 12.20f)) + NormalIn * 0.42f;
            CreateBox("Sidewalk_ControlPedestal", adminGo.transform,
                new Vector3(pedPt.x, 0.85f, pedPt.y), new Vector3(0.22f, 1.30f, 0.16f), darkTrimMat, keepCollider: true);
            CreateBox("Sidewalk_ControlPedestal_Screen", adminGo.transform,
                new Vector3(pedPt.x - NormalIn.x * 0.085f, 1.05f, pedPt.y - NormalIn.y * 0.085f), new Vector3(0.16f, 0.34f, 0.04f), blueSignMat);

            // --- 6. TELECOM LATTICE TOWER ON MAIN WING ROOF ---
            GameObject towerGo = new GameObject("Admin_Roof_TelecomTower");
            towerGo.transform.SetParent(adminGo.transform);
            towerGo.transform.position = new Vector3(-13.10f, 4.24f, 48.50f);
            for (int l = 0; l < 3; l++)
            {
                float ang = l * 120f * Mathf.Deg2Rad;
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"TowerLeg_{l + 1}";
                leg.transform.SetParent(towerGo.transform);
                leg.transform.localPosition = new Vector3(Mathf.Sin(ang) * 0.36f, 2.35f, Mathf.Cos(ang) * 0.36f);
                leg.transform.localScale = new Vector3(0.045f, 2.35f, 0.045f);
                SetMaterial(leg, steelMat);
                UnityEngine.Object.DestroyImmediate(leg.GetComponent<Collider>());
            }
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Tower_TopMast";
            mast.transform.SetParent(towerGo.transform);
            mast.transform.localPosition = new Vector3(0f, 5.20f, 0f);
            mast.transform.localScale = new Vector3(0.04f, 0.90f, 0.04f);
            SetMaterial(mast, steelMat);
            UnityEngine.Object.DestroyImmediate(mast.GetComponent<Collider>());
        }

        // =========================================================================
        // STEP 1: NW TRIANGULAR LOT ("PICO"), RED-WHITE-GREEN WALL, GATE & GARDEN
        // =========================================================================

        private static void BuildNorthWestTriangleAndWall(
            Bounds adminBounds,
            Material concreteMat, Material curbMat,
            Material whiteWallMat, Material zocaloMat, Material redStripeMat,
            Material greenBandMat, Material metalGateMat,
            Material foliageMat, Material shrubDarkMat, Material woodMat, Material turfMat,
            Material steelMat, Material orangeMat)
        {
            GameObject nwRoot = GameObject.Find("Corner_NO_Organic_Root");
            if (nwRoot == null)
            {
                Debug.LogError("[WestTriangleAndAbasoloBuilder] Corner_NO_Organic_Root not found.");
                return;
            }

            // Destroy old upgrade root cleanly if re-running
            Transform oldUpgrade = nwRoot.transform.Find("WestTriangle_Upgrade_Root");
            if (oldUpgrade != null) UnityEngine.Object.DestroyImmediate(oldUpgrade.gameObject);

            // Remove old sparse curb-side trees UrbanTree_North_1..4 that obstructed the outer sidewalk
            for (int t = 1; t <= 6; t++)
            {
                GameObject oldTree = GameObject.Find($"UrbanTree_North_{t}");
                if (oldTree != null) UnityEngine.Object.DestroyImmediate(oldTree);
            }

            // Trim Sidewalk_West_North_Section so it does NOT protrude north of Z = 54.00m (into Street_Abasolo)
            GameObject swWestNorth = GameObject.Find("Sidewalk_West_North_Section");
            if (swWestNorth != null)
            {
                swWestNorth.transform.position = new Vector3(-9.50f, 0.10f, 49.00f);
                swWestNorth.transform.localScale = new Vector3(5.00f, 0.20f, 10.00f); // Z = 44.00..54.00m
            }

            GameObject upgradeRoot = new GameObject("WestTriangle_Upgrade_Root");
            upgradeRoot.transform.SetParent(nwRoot.transform);
            upgradeRoot.transform.localPosition = Vector3.zero;

            // Curb line starts seamlessly from CalleGardeniasBuilder P9 (-31.00, 48.32)
            // Straight line Z(x) = 37.98 - 0.55*(x + 12.20)
            // Beveled tip around X = -41.05, Z = 53.85 turning cleanly onto Z = 54.00 at X = -40.65
            Vector2 p9 = new Vector2(-31.00f, 48.32f);
            Vector2 pTipDiag = new Vector2(-41.05f, 53.85f);
            Vector2 pTipNorth = new Vector2(-40.65f, AbasoloSouthZ);
            Vector2 pBackJoin = new Vector2(-31.00f, AbasoloSouthZ);

            // 1A. Tip extension polygon (X = -31.00 westward to tip, Y = 0.00..0.20m)
            List<Vector2> tipPoly = new List<Vector2>
            {
                new Vector2(-31.00f, 48.32f),
                pTipDiag,
                pTipNorth,
                pBackJoin
            };
            CreateFlatPrism("Sidewalk_NW_Tip_Extension", upgradeRoot.transform, tipPoly, SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // 1B. North sidewalk apron strip behind Sidewalk_NW_Plaza_Mesh & AdminBuilding (Z = 53.00..54.00m, X = -31.00..-12.00m)
            // Abuts Sidewalk_West_North_Section (X = -12.00..-7.00, Z = 44..54) cleanly at X = -12.00m!
            CreateBox("Sidewalk_NW_NorthStrip", upgradeRoot.transform,
                new Vector3(-21.50f, 0.10f, 53.50f), new Vector3(19.00f, 0.20f, 1.00f), concreteMat, keepCollider: true);

            // 1C. Continuous concrete curbs along the new outer edges (-31.00, 48.32) -> tip -> (-10.80, 54.00)
            CreateCurbSegment("Curb_NW_Tip_Diag", upgradeRoot.transform, p9, pTipDiag, SidewalkTopY, curbMat, thickness: 0.20f, inwardNormal: NormalIn);
            CreateCurbSegment("Curb_NW_Tip_Bevel", upgradeRoot.transform, pTipDiag, pTipNorth, SidewalkTopY, curbMat, thickness: 0.20f,
                inwardNormal: (pBackJoin - pTipDiag).normalized);
            CreateCurbSegment("Curb_NW_North_Abasolo", upgradeRoot.transform, pTipNorth, new Vector2(-8.50f, AbasoloSouthZ), SidewalkTopY, curbMat,
                thickness: 0.20f, inwardNormal: Vector2.down);

            // 1D. Corner safety bollard + utility pole at the acute "pico" outer sidewalk nose (matches photo)
            CreatePicoCornerMarkers(upgradeRoot.transform, new Vector3(-39.65f, SidewalkTopY, 53.40f), steelMat, orangeMat, concreteMat);

            // 1E. Masonry Perimeter Wall + Metal Gate (Inset 2.50m behind diagonal curb so sidewalk is wide & clear!)
            float diagWallInset = 2.50f;
            Vector2 refWallPt = new Vector2(-12.20f, 37.98f) + NormalIn * diagWallInset; // outer face of wall
            Vector2 diagCenterRef = refWallPt + NormalIn * 0.12f;

            // North wall centerline at Z = 52.72m (leaves 1.28m north sidewalk/curb to Z = 54.00m)
            float northWallZ = 52.72f;

            // Intersect diagonal centerline with Z = northWallZ -> X_apex approx -33.56m
            float wallApexX = diagCenterRef.x + (northWallZ - diagCenterRef.y) / DiagSlopeDzDx;
            float wallBevelX = wallApexX + 0.45f; // bevel cut approx -33.11m
            float wallBevelDiagZ = diagCenterRef.y + DiagSlopeDzDx * (wallBevelX - diagCenterRef.x);

            // Start walls tucked cleanly against Admin_WestWing SW corner (-18.40, 44.38) & Main Wing NW (-14.60, 52.72)
            float diagStartX = -18.25f;
            float diagStartZ = diagCenterRef.y + DiagSlopeDzDx * (diagStartX - diagCenterRef.x);
            float northStartX = -14.60f;

            // First diagonal bay (-18.25 -> -21.55) is the grey double-leaf metal service gate (porton) seen in Street View!
            float gateEndX = -21.55f;
            float gateEndZ = diagCenterRef.y + DiagSlopeDzDx * (gateEndX - diagCenterRef.x);

            GameObject wallRoot = new GameObject("PerimeterWall_NW_Root");
            wallRoot.transform.SetParent(upgradeRoot.transform);
            wallRoot.transform.localPosition = Vector3.zero;

            // Build North wall segment
            BuildMasonryWallRun("Wall_NW_North", wallRoot.transform,
                new Vector2(northStartX, northWallZ), new Vector2(wallBevelX, northWallZ),
                zocaloMat, whiteWallMat, redStripeMat, greenBandMat, pilasterSpacing: 4.5f, heightScale: 1.0f);

            // Build Grey Metal Double Gate Bay adjacent to West Wing (X=-18.25 -> -21.55)
            BuildMetalGateBay("Wall_NW_MetalGate", wallRoot.transform,
                new Vector2(diagStartX, diagStartZ), new Vector2(gateEndX, gateEndZ),
                zocaloMat, whiteWallMat, redStripeMat, greenBandMat, metalGateMat);

            // Build remaining Diagonal wall segment from gate post to beveled tip (X=-21.55 -> -33.11)
            BuildMasonryWallRun("Wall_NW_Diagonal", wallRoot.transform,
                new Vector2(gateEndX, gateEndZ), new Vector2(wallBevelX, wallBevelDiagZ),
                zocaloMat, whiteWallMat, redStripeMat, greenBandMat, pilasterSpacing: 4.2f, heightScale: 1.0f);

            // Bevel endcap at the apex closing North wall <-> Diagonal wall
            BuildMasonryWallRun("Wall_NW_ApexCap", wallRoot.transform,
                new Vector2(wallBevelX, wallBevelDiagZ), new Vector2(wallBevelX, northWallZ),
                zocaloMat, whiteWallMat, redStripeMat, greenBandMat, pilasterSpacing: 99f, heightScale: 1.0f);

            // 1F. Inner Triangular Garden Planter & Tall Lush Trees (visible rising above gate/wall in Street View!)
            GameObject gardenRoot = new GameObject("Garden_NW_Courtyard_Root");
            gardenRoot.transform.SetParent(upgradeRoot.transform);
            gardenRoot.transform.localPosition = Vector3.zero;

            float gardenStartX = diagStartX - 0.35f;
            float gardenTipX = wallBevelX + 0.65f;
            float gardenNorthZ = northWallZ - 0.24f;
            float gardenDiagStartZ = diagCenterRef.y + DiagSlopeDzDx * (gardenStartX - diagCenterRef.x) + 0.28f;
            float gardenDiagTipZ = diagCenterRef.y + DiagSlopeDzDx * (gardenTipX - diagCenterRef.x) + 0.28f;

            if (gardenDiagStartZ < gardenNorthZ - 0.4f && gardenTipX < gardenStartX - 1.0f)
            {
                List<Vector2> gardenPoly = new List<Vector2>
                {
                    new Vector2(gardenStartX, gardenDiagStartZ),
                    new Vector2(gardenTipX, Mathf.Min(gardenDiagTipZ, gardenNorthZ - 0.15f)),
                    new Vector2(gardenTipX, gardenNorthZ),
                    new Vector2(gardenStartX, gardenNorthZ)
                };
                CreateFlatPrism("Garden_Planter_Turf", gardenRoot.transform, gardenPoly, SidewalkTopY - 0.01f, SidewalkTopY + 0.06f, turfMat, addCollider: false);
            }

            // Plant 3 full-height canopy trees inside the courtyard behind the gate & diagonal wall (matches tall tree in photo!)
            Vector3[] innerTrees = new Vector3[]
            {
                new Vector3(-20.40f, SidewalkTopY + 0.05f, 49.00f),
                new Vector3(-23.40f, SidewalkTopY + 0.05f, 50.00f),
                new Vector3(-26.20f, SidewalkTopY + 0.05f, 50.85f)
            };
            float[] treeScales = new float[] { 1.38f, 1.12f, 0.88f };
            for (int i = 0; i < innerTrees.Length; i++)
            {
                CreateGardenTree($"GardenTree_Inner_{i + 1}", gardenRoot.transform, innerTrees[i], treeScales[i], woodMat,
                    (i % 2 == 0) ? foliageMat : shrubDarkMat);
            }

            // Dense ornamental bushes/shrubs strictly bounded inside both walls so ZERO sphere ever clips outside the triangle!
            Vector4[] shrubs = new Vector4[]
            {
                new Vector4(-19.5f, 47.2f, 1.15f, 0),
                new Vector4(-22.0f, 48.2f, 1.15f, 1),
                new Vector4(-24.6f, 49.4f, 1.05f, 0),
                new Vector4(-27.2f, 50.6f, 0.90f, 1),
                new Vector4(-28.8f, 51.3f, 0.75f, 0),
                new Vector4(-20.5f, 51.5f, 1.15f, 1),
                new Vector4(-23.2f, 51.6f, 1.15f, 0),
                new Vector4(-25.8f, 51.6f, 1.00f, 1)
            };
            for (int s = 0; s < shrubs.Length; s++)
            {
                float sx = shrubs[s].x;
                float sc = shrubs[s].z;
                float rMargin = 0.60f * sc + 0.35f;
                float wallZAtX = diagCenterRef.y + DiagSlopeDzDx * (sx - diagCenterRef.x);
                float minSafeZ = wallZAtX + rMargin;
                float maxSafeZ = northWallZ - rMargin;
                if (minSafeZ <= maxSafeZ)
                {
                    float sz = Mathf.Clamp(shrubs[s].y, minSafeZ, maxSafeZ);
                    CreateGardenShrub($"GardenShrub_{s + 1}", gardenRoot.transform,
                        new Vector3(sx, SidewalkTopY + 0.05f, sz),
                        sc, (shrubs[s].w < 0.5f) ? foliageMat : shrubDarkMat);
                }
            }
        }

        // =========================================================================
        // STEP 2: SUNKEN UNDERPASS (PASO A DESNIVEL) & 3-LANE EAST ABASOLO + TALUD
        // =========================================================================

        private struct AbasoloSample
        {
            public float s;
            public Vector3 center;
            public Vector3 tangentXZ;
            public Vector3 northNormalXZ; // points "left" (+Z in straight section)
        }

        private const float AbasoloTunnelDepthY = -5.10f; // >= 4.75m clear headroom below -0.35m soffit for school buses
        private const float AbasoloCurveStartS = 24.0f;

        // Abasolo 3-lane width envelope: south fixed at -5.00 (Z=54.00m); north widens +5.00 -> +7.80 over s=-46..-40
        private const float AbasoloSouthOffset = -5.00f;
        private const float AbasoloNorth2LaneOffset = 5.00f;
        private const float AbasoloNorth3LaneOffset = 7.80f; // 12.80m total width => three 4.267m lanes
        private const float AbasoloLaneSep1 = -0.7333f;      // South confined W->E lane [-5.00..-0.733] | Center normal W->E [-0.733..+3.533]
        private const float AbasoloLaneSep2 = 3.5333f;       // Center normal W->E [-0.733..+3.533] | North normal E->W [+3.533..+7.80]
        private const float AbasoloLane1Center = -2.8667f;
        private const float AbasoloLane2Center = 1.4000f;
        private const float AbasoloLane3Center = 5.6667f;

        // OSM-aligned Avenida Francisco Villa half-width (10.0m total roadway width matching OSM corridor)
        private const float FVHalfWidth = 5.00f;
        private const float BuswayHalfWidth = 3.00f;

        private static OsmNetworkData _cachedOsm;

        private static OsmNetworkData GetOsmNetwork()
        {
            if (_cachedOsm == null) _cachedOsm = BuildOsmNetworkData();
            return _cachedOsm;
        }

        private static float EvalAbasoloNorthOffset(float s)
        {
            if (s <= -46.0f) return AbasoloNorth2LaneOffset;
            if (s >= -40.0f) return AbasoloNorth3LaneOffset;
            float t = Mathf.InverseLerp(-46.0f, -40.0f, s);
            return Mathf.Lerp(AbasoloNorth2LaneOffset, AbasoloNorth3LaneOffset, t * t * (3f - 2f * t));
        }

        private static float EvalAbasoloRampY(float s)
        {
            if (s > -40.0f && s < -10.5f)
            {
                float t = Mathf.InverseLerp(-40.0f, -10.5f, s);
                return AbasoloTunnelDepthY * (t * t * (3f - 2f * t));
            }
            if (s >= -10.5f && s <= 19.0f) return AbasoloTunnelDepthY;
            if (s > 19.0f && s < 55.0f)
            {
                float t = Mathf.InverseLerp(19.0f, 55.0f, s);
                return AbasoloTunnelDepthY * (1f - t * t * (3f - 2f * t));
            }
            return 0f;
        }

        private static float FindAbasoloFVWestHitS(float lateralOffset)
        {
            OsmNetworkData net = GetOsmNetwork();
            List<Vector2> fvWest = net.fvWestEdge;
            AbasoloSample prevSp = EvaluateAbasolo(60.0f);
            Vector2 prevP = new Vector2(prevSp.center.x, prevSp.center.z) + new Vector2(prevSp.northNormalXZ.x, prevSp.northNormalXZ.z) * lateralOffset;
            for (float s = 60.5f; s <= 145.0f; s += 0.5f)
            {
                AbasoloSample sp = EvaluateAbasolo(s);
                Vector2 curP = new Vector2(sp.center.x, sp.center.z) + new Vector2(sp.northNormalXZ.x, sp.northNormalXZ.z) * lateralOffset;
                if (FindSegmentPolylineHit(prevP, curP, fvWest, out Vector2 hit, out _))
                {
                    float frac = Vector2.Distance(prevP, hit) / Mathf.Max(1e-5f, Vector2.Distance(prevP, curP));
                    return Mathf.Lerp(s - 0.5f, s, frac);
                }
                prevP = curP;
            }
            return 112.0f;
        }

        private static AbasoloSample EvaluateAbasolo(float s)
        {
            float x, z;
            Vector2 tanXZ;
            if (s <= AbasoloCurveStartS)
            {
                x = s;
                z = AbasoloCenterZ;
                tanXZ = Vector2.right;
            }
            else
            {
                List<SpinePt2D> spine = GetOsmNetwork().abaSpine;
                float sRel = s - AbasoloCurveStartS;
                SpinePt2D last = spine[spine.Count - 1];
                if (sRel >= last.sAlong)
                {
                    float ds = sRel - last.sAlong;
                    Vector2 p = last.pos + last.tan * ds;
                    x = p.x; z = p.y; tanXZ = last.tan;
                }
                else
                {
                    int lo = 0, hi = spine.Count - 1;
                    while (hi - lo > 1)
                    {
                        int mid = (lo + hi) >> 1;
                        if (spine[mid].sAlong <= sRel) lo = mid; else hi = mid;
                    }
                    SpinePt2D a = spine[lo];
                    SpinePt2D b = spine[hi];
                    float u = Mathf.InverseLerp(a.sAlong, b.sAlong, sRel);
                    Vector2 p = Vector2.Lerp(a.pos, b.pos, u);
                    x = p.x; z = p.y;
                    tanXZ = Vector2.Lerp(a.tan, b.tan, u).normalized;
                }
            }

            float y = EvalAbasoloRampY(s);
            Vector3 tan = new Vector3(tanXZ.x, 0f, tanXZ.y).normalized;
            Vector3 north = new Vector3(-tan.z, 0f, tan.x);
            return new AbasoloSample
            {
                s = s,
                center = new Vector3(x, y, z),
                tangentXZ = tan,
                northNormalXZ = north
            };
        }

        private static void RelocateStreetAbasolo(Material asphaltMat, Material curbMat, Material whiteStripeMat, Material yellowStripeMat,
            Material concreteMat, Material bridgeWhiteMat)
        {
            GameObject plazaParent = GameObject.Find("Border_Crossing_Plaza");
            Transform abasoloParent = plazaParent != null ? plazaParent.transform : null;

            // Remove previously created upgrade container cleanly if re-running
            GameObject oldUpgrade = GameObject.Find("Street_Abasolo_Relocated_Root");
            if (oldUpgrade != null) UnityEngine.Object.DestroyImmediate(oldUpgrade);

            GameObject abasoloRoot = new GameObject("Street_Abasolo_Relocated_Root");
            if (abasoloParent != null) abasoloRoot.transform.SetParent(abasoloParent);
            abasoloRoot.transform.position = Vector3.zero;

            // 1. Disable legacy flat Abasolo slab & stripes (replaced by continuous sunken + curved 3-lane mesh)
            foreach (string legacyName in new[] { "Street_Abasolo", "Street_Abasolo_Line_-4.7", "Street_Abasolo_Line_+0.0", "Street_Abasolo_Line_+4.7" })
            {
                GameObject legacy = GameObject.Find(legacyName);
                if (legacy != null) legacy.SetActive(false);
            }

            // 2. Clear physical underpass obstacles in Z = 54..66.8m:
            // Trim Road_AvenidaJuarez (previously ran to Z=65) so it tucks cleanly under Bridge_Span_Deck South edge (Z <= 51.4m)
            GameObject roadJuarez = GameObject.Find("Road_AvenidaJuarez");
            if (roadJuarez != null)
            {
                // Keep Z from -25.00m to +51.40m (len = 76.40, center = 13.20)
                roadJuarez.transform.position = new Vector3(0f, -0.05f, 13.20f);
                roadJuarez.transform.localScale = new Vector3(14f, 0.10f, 76.40f);
            }

            // Trim Plaza_Pavement_Extension (previously ran to Z=69) to end at Z=53.70m, opening the East tunnel mouth & ascent!
            GameObject plazaExt = GameObject.Find("Plaza_Pavement_Extension");
            if (plazaExt != null)
            {
                // Spans X = 17.00..23.50m, Z = 34.00..53.70m (len = 19.70, center = 43.85)
                plazaExt.transform.position = new Vector3(20.25f, 0.10f, 43.85f);
                plazaExt.transform.localScale = new Vector3(6.50f, 0.20f, 19.70f);
            }

            // Keep Terrain_Skirt_South strictly west of ServiceNorth / Francisco Villa (X = -220..+68m, Z = 67.25..91.25m)
            GameObject terrainSkirt = GameObject.Find("Terrain_Skirt_South");
            Material sandMat = concreteMat;
            if (terrainSkirt != null)
            {
                terrainSkirt.transform.position = new Vector3(-76.00f, -0.22f, 79.25f);
                terrainSkirt.transform.localScale = new Vector3(288.00f, 0.30f, 24.00f);
                Renderer sr = terrainSkirt.GetComponent<Renderer>();
                if (sr != null && sr.sharedMaterial != null) sandMat = sr.sharedMaterial;
            }

            // 3. Build parametric Street_Abasolo sunken underpass + 3-lane full-scale East curve clipped cleanly at Francisco Villa West edge
            float sHitNorth = FindAbasoloFVWestHitS(AbasoloNorth3LaneOffset);
            float sHitSouth = FindAbasoloFVWestHitS(AbasoloSouthOffset);

            List<float> stations = new List<float>();
            for (float s = -150f; s < -46f; s += 8f) stations.Add(s);
            for (float s = -46f; s < 86f; s += 1.25f) stations.Add(s);
            for (float s = 86f; s < sHitNorth - 0.5f; s += 2.0f) stations.Add(s);
            stations.Add(sHitNorth);

            List<AbasoloSample> samples = new List<AbasoloSample>(stations.Count);
            for (int i = 0; i < stations.Count; i++) samples.Add(EvaluateAbasolo(stations[i]));

            BuildAbasoloRoadwayMesh("Street_Abasolo_UnderpassRoadway", abasoloRoot.transform, samples, sHitNorth, sHitSouth, asphaltMat);

            // 3-lane markings: continuous yellow dividers (-0.733, +3.533) + directional white arrows + edge lines
            BuildAbasoloMarkings(abasoloRoot.transform, samples, sHitNorth, sHitSouth, whiteStripeMat, yellowStripeMat);

            // Curbs & Concrete Retaining Walls along sunken trench + East curve terminating cleanly at Francisco Villa corner setbacks
            BuildAbasoloRetainingWallsAndCurbs(abasoloRoot.transform, samples, sHitNorth, sHitSouth, concreteMat, curbMat);

            // Structural Bridge Underpass Ceiling (Soffit) & Abutment Pilasters beneath Bridge_Span_Deck / Walkways
            BuildUnderpassBridgeStructure(abasoloRoot.transform, concreteMat, bridgeWhiteMat);

            // Curved sand infill embankment East/North of East curved Abasolo & under/around Francisco Villa out to X = 210m
            BuildEastCurveNorthEmbankment(abasoloRoot.transform, samples, sandMat);

            // Signature white sloped retaining embankment (talud) on West side north of widened Street_Abasolo (Z = 67.35..90m)
            BuildBridgeWestTalud(abasoloRoot.transform, bridgeWhiteMat);

            Debug.Log($"[WestTriangleAndAbasoloBuilder] Built 3-lane Abasolo (sHitNorth={sHitNorth:F2}, sHitSouth={sHitSouth:F2}).");
        }

        private static void BuildAbasoloRoadwayMesh(string name, Transform parent, List<AbasoloSample> samples,
            float sHitNorth, float sHitSouth, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            List<Vector3> northTops = new List<Vector3>(samples.Count + 8);
            List<Vector3> southTops = new List<Vector3>(samples.Count + 8);
            List<float> sValues = new List<float>(samples.Count + 8);

            for (int i = 0; i < samples.Count; i++)
            {
                AbasoloSample sp = samples[i];
                float nOff = EvalAbasoloNorthOffset(sp.s);
                northTops.Add(sp.center + sp.northNormalXZ * nOff);
                southTops.Add(sp.center + sp.northNormalXZ * AbasoloSouthOffset);
                sValues.Add(sp.s);
            }

            // Diagonal termination wedge from sHitNorth to sHitSouth flush along Francisco Villa's West edge (0 overlap!)
            Vector2 pNorthFV = EvalAbasoloOffsetXZ(sHitNorth, AbasoloNorth3LaneOffset, out _);
            Vector2 pSouthFV = EvalAbasoloOffsetXZ(sHitSouth, AbasoloSouthOffset, out _);
            int diagSteps = 8;
            for (int i = 1; i < diagSteps; i++)
            {
                float u = (float)i / diagSteps;
                float sCur = Mathf.Lerp(sHitNorth, sHitSouth, u);
                Vector2 nPos = Vector2.Lerp(pNorthFV, pSouthFV, u);
                Vector2 sPos = EvalAbasoloOffsetXZ(sCur, AbasoloSouthOffset, out _);
                northTops.Add(new Vector3(nPos.x, 0f, nPos.y));
                southTops.Add(new Vector3(sPos.x, 0f, sPos.y));
                sValues.Add(sCur);
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            float thickness = 0.25f;

            void AddFlatQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
            {
                int b = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
                uvs.Add(uv0); uvs.Add(uv1); uvs.Add(uv2); uvs.Add(uv3);
                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 0, b + 2, b + 3 });
            }

            for (int i = 0; i < northTops.Count - 1; i++)
            {
                Vector3 aN = northTops[i];
                Vector3 aS = southTops[i];
                Vector3 bN = northTops[i + 1];
                Vector3 bS = southTops[i + 1];
                float sa = sValues[i] * 0.1f;
                float sb = sValues[i + 1] * 0.1f;

                Vector3 aNB = aN - Vector3.up * thickness;
                Vector3 aSB = aS - Vector3.up * thickness;
                Vector3 bNB = bN - Vector3.up * thickness;
                Vector3 bSB = bS - Vector3.up * thickness;

                // Top (+Y)
                AddFlatQuad(aN, bN, bS, aS, new Vector2(sa, 1f), new Vector2(sb, 1f), new Vector2(sb, 0f), new Vector2(sa, 0f));
                // Bottom (-Y)
                AddFlatQuad(aSB, bSB, bNB, aNB, new Vector2(sa, 0f), new Vector2(sb, 0f), new Vector2(sb, 1f), new Vector2(sa, 1f));
                // South side
                AddFlatQuad(aS, bS, bSB, aSB, new Vector2(sa, 0f), new Vector2(sb, 0f), new Vector2(sb, 1f), new Vector2(sa, 1f));
                // North/FV side
                AddFlatQuad(aNB, bNB, bN, aN, new Vector2(sa, 1f), new Vector2(sb, 1f), new Vector2(sb, 0f), new Vector2(sa, 0f));
            }

            // Final closing triangle right up to pSouthFV
            {
                int last = northTops.Count - 1;
                Vector3 aN = northTops[last];
                Vector3 aS = southTops[last];
                Vector3 tip = new Vector3(pSouthFV.x, 0f, pSouthFV.y);
                Vector3 aNB = aN - Vector3.up * thickness;
                Vector3 aSB = aS - Vector3.up * thickness;
                Vector3 tipB = tip - Vector3.up * thickness;

                int bTop = verts.Count;
                verts.Add(aN); verts.Add(tip); verts.Add(aS);
                uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1.1f, 0f)); uvs.Add(new Vector2(1f, 0f));
                tris.AddRange(new int[] { bTop + 0, bTop + 1, bTop + 2 });

                int bBot = verts.Count;
                verts.Add(aSB); verts.Add(tipB); verts.Add(aNB);
                uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1.1f, 0f)); uvs.Add(new Vector2(1f, 1f));
                tris.AddRange(new int[] { bBot + 0, bBot + 1, bBot + 2 });

                AddFlatQuad(aS, tip, tipB, aSB, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                AddFlatQuad(aNB, tipB, tip, aN, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }

            Mesh m = new Mesh { name = $"{name}_Mesh" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = m;
        }

        private static void AddPavementArrow(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 centerPos, Vector3 dirXZ, float liftY)
        {
            Vector3 d = new Vector3(dirXZ.x, 0f, dirXZ.z).normalized;
            Vector3 l = new Vector3(-d.z, 0f, d.x);
            Vector3 c = centerPos + Vector3.up * liftY;

            // Shaft (-1.25d .. +0.75d, half-width 0.16m)
            Vector3 s0 = c - d * 1.25f + l * 0.16f;
            Vector3 s1 = c + d * 0.75f + l * 0.16f;
            Vector3 s2 = c + d * 0.75f - l * 0.16f;
            Vector3 s3 = c - d * 1.25f - l * 0.16f;
            int bs = verts.Count;
            verts.Add(s0); verts.Add(s1); verts.Add(s2); verts.Add(s3);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.up); uvs.Add(Vector2.one); uvs.Add(Vector2.right);
            tris.AddRange(new int[] { bs + 0, bs + 1, bs + 2, bs + 0, bs + 2, bs + 3 });

            // Arrowhead (+0.75d base half-width 0.56m -> +1.95d tip)
            Vector3 hL = c + d * 0.75f + l * 0.56f;
            Vector3 hTip = c + d * 1.95f;
            Vector3 hR = c + d * 0.75f - l * 0.56f;
            int bh = verts.Count;
            verts.Add(hL); verts.Add(hTip); verts.Add(hR);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.up); uvs.Add(Vector2.right);
            tris.AddRange(new int[] { bh + 0, bh + 1, bh + 2 });
        }

        private static void BuildAbasoloMarkings(Transform parent, List<AbasoloSample> samples,
            float sHitNorth, float sHitSouth, Material whiteMat, Material yellowMat)
        {
            void AddStripQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs, Vector3 aL, Vector3 aR, Vector3 bR, Vector3 bL)
            {
                int b = verts.Count;
                verts.Add(aL); verts.Add(bL); verts.Add(bR); verts.Add(aR);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.up); uvs.Add(Vector2.one); uvs.Add(Vector2.right);
                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 0, b + 2, b + 3 });
            }

            void EmitRibbonLine(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
                float sStart, float sEnd, Func<float, float> offsetFunc, float halfWidth, float liftY)
            {
                if (sEnd <= sStart + 0.05f) return;
                List<float> st = new List<float>();
                for (float s = sStart; s < sEnd; s += 1.25f) st.Add(s);
                st.Add(sEnd);
                for (int i = 0; i < st.Count - 1; i++)
                {
                    AbasoloSample a = EvaluateAbasolo(st[i]);
                    AbasoloSample b = EvaluateAbasolo(st[i + 1]);
                    float offA = offsetFunc(a.s);
                    float offB = offsetFunc(b.s);
                    Vector3 up = Vector3.up * liftY;
                    AddStripQuad(verts, tris, uvs,
                        a.center + a.northNormalXZ * (offA + halfWidth) + up,
                        a.center + a.northNormalXZ * (offA - halfWidth) + up,
                        b.center + b.northNormalXZ * (offB - halfWidth) + up,
                        b.center + b.northNormalXZ * (offB + halfWidth) + up);
                }
            }

            // A) White markings: North & South outer edge lines, West 2-lane dashes (s < -46), and 3-lane directional white arrows!
            GameObject whiteGo = new GameObject("Abasolo_Continuous_Markings");
            whiteGo.transform.SetParent(parent);
            MeshFilter wMf = whiteGo.AddComponent<MeshFilter>();
            whiteGo.AddComponent<MeshRenderer>().sharedMaterial = whiteMat;

            List<Vector3> wVerts = new List<Vector3>();
            List<int> wTris = new List<int>();
            List<Vector2> wUvs = new List<Vector2>();

            float sEndNorthEdge = FindAbasoloFVWestHitS(AbasoloNorth3LaneOffset - 0.36f) - 0.10f;
            float sEndSouthEdge = FindAbasoloFVWestHitS(AbasoloSouthOffset + 0.36f) - 0.10f;

            // Continuous North outer white line (smoothly shifts +4.64 -> +7.44 over s=-46..-40, stops cleanly at FVWest)
            EmitRibbonLine(wVerts, wTris, wUvs, -150f, sEndNorthEdge, s => EvalAbasoloNorthOffset(s) - 0.36f, 0.08f, 0.015f);

            // South edge white line (omitted across West Y-junction mouth -63.5..-40.5)
            EmitRibbonLine(wVerts, wTris, wUvs, -150f, -63.5f, _ => AbasoloSouthOffset + 0.36f, 0.08f, 0.015f);
            EmitRibbonLine(wVerts, wTris, wUvs, -40.5f, sEndSouthEdge, _ => AbasoloSouthOffset + 0.36f, 0.08f, 0.015f);

            // Legacy 2-lane dashed centerline west of 3-lane start (s = -150..-46)
            for (float s = -150f; s < -46.5f; s += 6.0f)
            {
                float dEnd = Mathf.Min(s + 3.0f, -46.5f);
                EmitRibbonLine(wVerts, wTris, wUvs, s, dEnd, _ => 0.0f, 0.08f, 0.015f);
            }

            // Directional white pavement arrows along all 3 lanes from s=-40 underpass descent through curve to Francisco Villa corner
            float sLimitL1 = FindAbasoloFVWestHitS(AbasoloLane1Center) - 2.8f;
            float sLimitL2 = FindAbasoloFVWestHitS(AbasoloLane2Center) - 2.8f;
            float sLimitL3 = FindAbasoloFVWestHitS(AbasoloLane3Center) - 2.8f;

            float[] arrowStations = new float[] { -33f, -19f, -4f, 12f, 28f, 44f, 60f, 76f, 92f, 108f };
            foreach (float sArr in arrowStations)
            {
                AbasoloSample sp = EvaluateAbasolo(sArr);
                // Lane 1 (South, W->E confined to Gardenias / Linea Express)
                if (sArr <= sLimitL1)
                    AddPavementArrow(wVerts, wTris, wUvs, sp.center + sp.northNormalXZ * AbasoloLane1Center, sp.tangentXZ, 0.017f);
                // Lane 2 (Center, W->E normal traffic)
                if (sArr <= sLimitL2)
                    AddPavementArrow(wVerts, wTris, wUvs, sp.center + sp.northNormalXZ * AbasoloLane2Center, sp.tangentXZ, 0.017f);
                // Lane 3 (North, E->W normal traffic)
                if (sArr <= sLimitL3)
                    AddPavementArrow(wVerts, wTris, wUvs, sp.center + sp.northNormalXZ * AbasoloLane3Center, -sp.tangentXZ, 0.017f);
            }

            Mesh wMesh = new Mesh { name = "Abasolo_Continuous_Markings_Mesh" };
            wMesh.vertices = wVerts.ToArray();
            wMesh.triangles = wTris.ToArray();
            wMesh.uv = wUvs.ToArray();
            wMesh.RecalculateNormals();
            wMesh.RecalculateBounds();
            wMf.sharedMesh = wMesh;

            // B) Two continuous solid yellow divider lines separating all 3 lanes from s=-40.0f up to Francisco Villa West edge
            GameObject yellowGo = new GameObject("Abasolo_Yellow_LaneDividers");
            yellowGo.transform.SetParent(parent);
            MeshFilter yMf = yellowGo.AddComponent<MeshFilter>();
            yellowGo.AddComponent<MeshRenderer>().sharedMaterial = yellowMat;

            List<Vector3> yVerts = new List<Vector3>();
            List<int> yTris = new List<int>();
            List<Vector2> yUvs = new List<Vector2>();

            float sEndSep1 = FindAbasoloFVWestHitS(AbasoloLaneSep1) - 0.10f;
            float sEndSep2 = FindAbasoloFVWestHitS(AbasoloLaneSep2) - 0.10f;
            EmitRibbonLine(yVerts, yTris, yUvs, -40.0f, sEndSep1, _ => AbasoloLaneSep1, 0.09f, 0.016f);
            EmitRibbonLine(yVerts, yTris, yUvs, -40.0f, sEndSep2, _ => AbasoloLaneSep2, 0.09f, 0.016f);

            Mesh yMesh = new Mesh { name = "Abasolo_Yellow_LaneDividers_Mesh" };
            yMesh.vertices = yVerts.ToArray();
            yMesh.triangles = yTris.ToArray();
            yMesh.uv = yUvs.ToArray();
            yMesh.RecalculateNormals();
            yMesh.RecalculateBounds();
            yMf.sharedMesh = yMesh;
        }

        private static void BuildAbasoloRetainingWallsAndCurbs(Transform parent, List<AbasoloSample> samples,
            float sHitNorth, float sHitSouth, Material concreteMat, Material curbMat)
        {
            void BuildSideWallStrip(string objName, Func<AbasoloSample, float> innerOffFunc, Func<AbasoloSample, float> outerOffFunc,
                float sStart, float sEnd, Func<AbasoloSample, float> topYFunc, Material mat, bool collider)
            {
                List<AbasoloSample> seg = new List<AbasoloSample>();
                seg.Add(EvaluateAbasolo(sStart));
                for (int i = 0; i < samples.Count; i++)
                {
                    if (samples[i].s > sStart + 0.02f && samples[i].s < sEnd - 0.02f) seg.Add(samples[i]);
                }
                seg.Add(EvaluateAbasolo(sEnd));
                if (seg.Count < 2) return;

                GameObject go = new GameObject(objName);
                go.transform.SetParent(parent);
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                bool invertWinding = outerOffFunc(seg[0]) < innerOffFunc(seg[0]);
                for (int i = 0; i < seg.Count; i++)
                {
                    AbasoloSample sp = seg[i];
                    float yBot = sp.center.y - 0.15f;
                    float yTop = Mathf.Max(yBot + 0.20f, topYFunc(sp));
                    Vector3 pInnerXZ = new Vector3(sp.center.x, 0f, sp.center.z) + sp.northNormalXZ * innerOffFunc(sp);
                    Vector3 pOuterXZ = new Vector3(sp.center.x, 0f, sp.center.z) + sp.northNormalXZ * outerOffFunc(sp);

                    verts.Add(pInnerXZ + Vector3.up * yTop); // 0 inner top
                    verts.Add(pOuterXZ + Vector3.up * yTop); // 1 outer top
                    verts.Add(pOuterXZ + Vector3.up * yBot); // 2 outer bot
                    verts.Add(pInnerXZ + Vector3.up * yBot); // 3 inner bot
                    uvs.Add(new Vector2(sp.s * 0.25f, yTop));
                    uvs.Add(new Vector2(sp.s * 0.25f, yTop));
                    uvs.Add(new Vector2(sp.s * 0.25f, yBot));
                    uvs.Add(new Vector2(sp.s * 0.25f, yBot));
                }

                for (int i = 0; i < seg.Count - 1; i++)
                {
                    int a = i * 4;
                    int b = (i + 1) * 4;
                    if (!invertWinding)
                    {
                        tris.AddRange(new int[] { a + 0, b + 1, b + 0, a + 0, a + 1, b + 1 });
                        tris.AddRange(new int[] { a + 3, b + 0, b + 3, a + 3, a + 0, b + 0 });
                        tris.AddRange(new int[] { a + 1, b + 2, b + 1, a + 1, a + 2, b + 2 });
                    }
                    else
                    {
                        tris.AddRange(new int[] { a + 0, b + 0, b + 1, a + 0, b + 1, a + 1 });
                        tris.AddRange(new int[] { a + 3, b + 3, b + 0, a + 3, b + 0, a + 0 });
                        tris.AddRange(new int[] { a + 1, b + 1, b + 2, a + 1, b + 2, a + 2 });
                    }
                }

                Mesh m = new Mesh { name = $"{objName}_Mesh" };
                m.vertices = verts.ToArray();
                m.triangles = tris.ToArray();
                m.uv = uvs.ToArray();
                m.RecalculateNormals();
                m.RecalculateBounds();
                mf.sharedMesh = m;
                if (collider) go.AddComponent<MeshCollider>().sharedMesh = m;
            }

            // 1. Sunken Trench Retaining Walls (South: -5.00..-5.34; North: +nOff..+nOff+0.34, tapering to 0.20m surface curb over s=51..55.5)
            float RetainingTopY(AbasoloSample sp)
            {
                if (sp.s >= -8.55f && sp.s <= 17.05f) return 0.05f;
                if (sp.s >= 51.0f) return Mathf.Lerp(0.36f, 0.20f, Mathf.InverseLerp(51.0f, 55.5f, sp.s));
                return 0.36f;
            }

            BuildSideWallStrip("Abasolo_RetainingWall_South", _ => -5.00f, _ => -5.34f, -40.5f, 55.5f, RetainingTopY, concreteMat, true);
            BuildSideWallStrip("Abasolo_RetainingWall_North", sp => EvalAbasoloNorthOffset(sp.s), sp => EvalAbasoloNorthOffset(sp.s) + 0.34f,
                -40.5f, 55.5f, RetainingTopY, concreteMat, true);

            // 2. Surface Concrete Curbs West of Y-junction (-150..-63.11 South; -150..-40.0 North following 2->3 lane transition)
            BuildSideWallStrip("Abasolo_Curb_WestFar_South", _ => -5.00f, _ => -5.22f, -150f, -63.11f, _ => 0.20f, curbMat, true);
            BuildSideWallStrip("Abasolo_Curb_West_North", sp => EvalAbasoloNorthOffset(sp.s), sp => EvalAbasoloNorthOffset(sp.s) + 0.22f,
                -150f, -40.0f, _ => 0.20f, curbMat, true);

            // 3. Surface Concrete Curbs along East curved exit up to CentralBlock_NE/ServiceNorth merge & FV setbacks (clean opening at ServiceNorth [25])
            OsmNetworkData net = GetOsmNetwork();
            float sSrvMerge = FindAbasoloPolylineHitS(net.srvNorthEdge, -5.00f, out float sHitCbNe) ? (sHitCbNe - 2.40f) : 95.0f;
            float abaSouthCurbEndS = Mathf.Clamp(sSrvMerge, 56.0f, sHitSouth - 2.60f);
            float abaNorthCurbEndS = Mathf.Max(56.0f, sHitNorth - 2.60f);
            BuildSideWallStrip("Abasolo_Curb_EastCurve_South", _ => -5.00f, _ => -5.24f, 55.0f, abaSouthCurbEndS, _ => 0.20f, curbMat, true);

            if (FindAbasoloPolylineHitS(net.srvNorthWestEdge, AbasoloNorth3LaneOffset, out float sSrvGapW) &&
                FindAbasoloPolylineHitS(net.srvNorthEastEdge, AbasoloNorth3LaneOffset, out float sSrvGapE))
            {
                float sGapA = Mathf.Min(sSrvGapW, sSrvGapE) - 0.6f;
                float sGapB = Mathf.Max(sSrvGapW, sSrvGapE) + 0.6f;
                if (sGapA > 55.5f)
                    BuildSideWallStrip("Abasolo_Curb_EastCurve_North_A", _ => AbasoloNorth3LaneOffset, _ => AbasoloNorth3LaneOffset + 0.24f,
                        55.0f, Mathf.Min(sGapA, abaNorthCurbEndS), _ => 0.20f, curbMat, true);
                if (abaNorthCurbEndS > sGapB + 0.5f)
                    BuildSideWallStrip("Abasolo_Curb_EastCurve_North_B", _ => AbasoloNorth3LaneOffset, _ => AbasoloNorth3LaneOffset + 0.24f,
                        sGapB, abaNorthCurbEndS, _ => 0.20f, curbMat, true);
            }
            else
            {
                BuildSideWallStrip("Abasolo_Curb_EastCurve_North", _ => AbasoloNorth3LaneOffset, _ => AbasoloNorth3LaneOffset + 0.24f,
                    55.0f, abaNorthCurbEndS, _ => 0.20f, curbMat, true);
            }
        }

        private static void BuildUnderpassBridgeStructure(Transform parent, Material concreteMat, Material bridgeWhiteMat)
        {
            // Widened soffit (Z = 53.65..67.15 => centerZ = 60.40, scaleZ = 13.50) spanning cleanly beneath Bridge_Span_Deck (Z = 51..69)
            float wideCenterZ = 60.40f;
            float wideScaleZ = 13.50f;
            float northAbutmentZ = AbasoloCenterZ + AbasoloNorth3LaneOffset + 0.18f; // 66.98m

            CreateBox("Abasolo_Tunnel_BridgeSoffit", parent,
                new Vector3(4.25f, -0.175f, wideCenterZ), new Vector3(26.00f, 0.35f, wideScaleZ), bridgeWhiteMat, keepCollider: true);

            CreateBox("Abasolo_Tunnel_PortalBeam_West", parent,
                new Vector3(-8.80f, -0.05f, wideCenterZ), new Vector3(0.45f, 0.60f, wideScaleZ), concreteMat, keepCollider: true);
            CreateBox("Abasolo_Tunnel_PortalBeam_East", parent,
                new Vector3(17.30f, -0.05f, wideCenterZ), new Vector3(0.45f, 0.60f, wideScaleZ), concreteMat, keepCollider: true);

            foreach (float xPil in new[] { -8.75f, 17.25f })
            {
                CreateBox($"Abasolo_Tunnel_Abutment_S_{Mathf.Abs(xPil):F0}", parent,
                    new Vector3(xPil, -2.65f, AbasoloSouthZ - 0.18f), new Vector3(0.55f, 5.10f, 0.42f), concreteMat, keepCollider: true);
                CreateBox($"Abasolo_Tunnel_Abutment_N_{Mathf.Abs(xPil):F0}", parent,
                    new Vector3(xPil, -2.65f, northAbutmentZ), new Vector3(0.55f, 5.10f, 0.42f), concreteMat, keepCollider: true);
            }

            // Ensure rotated background desert tiles (Terrain_South_W2/W1/C/E1/E2) stay North of Z=122.0m (safely behind Francisco Villa & ServiceNorth)
            foreach (string tileName in new[] { "Terrain_South_W2", "Terrain_South_W1", "Terrain_South_C", "Terrain_South_E1", "Terrain_South_E2" })
            {
                GameObject tile = GameObject.Find(tileName);
                if (tile != null)
                {
                    Renderer r = tile.GetComponent<Renderer>();
                    if (r != null && r.bounds.min.z < 122.0f)
                    {
                        tile.transform.position += new Vector3(0f, 0f, 122.0f - r.bounds.min.z);
                    }
                }
            }
        }

        private static void BuildEastCurveNorthEmbankment(Transform parent, List<AbasoloSample> samples, Material sandMat)
        {
            // Fill continuous sand terrain without ever overlapping Abasolo sunken ramp (s<=55.5) or sitting above RoadTopY=0.00f
            float northBoundaryZ = 67.28f; // meets Terrain_Skirt_South south edge (67.25) West of X=68m
            float eastBoundaryX = 68.00f;  // stops west of ServiceNorth [25] & Francisco Villa
            float topY = -0.085f;

            // North polygon strictly outside Abasolo's North curb (X=24..68)
            List<Vector2> polyN = new List<Vector2>();
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].s < AbasoloCurveStartS) continue;
                Vector3 edge = samples[i].center + samples[i].northNormalXZ * (AbasoloNorth3LaneOffset + 0.24f);
                if (edge.x <= eastBoundaryX && edge.z <= northBoundaryZ - 0.02f)
                {
                    polyN.Add(new Vector2(edge.x, edge.z));
                }
            }
            if (polyN.Count >= 2)
            {
                Vector2 firstPt = polyN[0];
                Vector2 lastPt = polyN[polyN.Count - 1];
                polyN.Add(new Vector2(eastBoundaryX, lastPt.y));
                polyN.Add(new Vector2(eastBoundaryX, northBoundaryZ));
                polyN.Add(new Vector2(firstPt.x, northBoundaryZ));
                CreateFlatPrism("East_Abasolo_NorthEmbankment", parent, polyN, -0.35f, topY, sandMat, addCollider: false);
            }

            // West-of-X56 south base ground covering X=24..56, Z=-140..+42.0 (strictly south of Abasolo sunken ramp!)
            CreateBox("East_Corridor_BaseGround", parent,
                new Vector3(40.0f, -0.225f, -49.0f), new Vector3(32.0f, 0.28f, 182.0f), sandMat, keepCollider: false);

            // Far-East base ground covering X=55.8..240, Z=-140..+124 (where Abasolo Y=0, safely 8.5cm below all East asphalt!)
            CreateBox("East_Corridor_BaseGround_FarEast", parent,
                new Vector3(147.9f, -0.225f, -8.0f), new Vector3(184.2f, 0.28f, 264.0f), sandMat, keepCollider: false);

            // Clean triangular ramp infill wedge south of Abasolo curve X=51..56, Z=42..48
            List<Vector2> rampSouthWedge = new List<Vector2>
            {
                new Vector2(51.5f, 42.0f),
                new Vector2(56.0f, 42.0f),
                new Vector2(56.0f, 47.0f)
            };
            CreateFlatPrism("East_Corridor_RampSouthWedge", parent, rampSouthWedge, -0.35f, topY, sandMat, addCollider: false);
        }

        private static void BuildBridgeWestTalud(Transform parent, Material bridgeWhiteMat)
        {
            // Widened 3-lane Abasolo north wall ends at Z=67.14m -> start talud at Z=67.35m up to Z=90m
            GameObject go = new GameObject("Bridge_West_RetainingTalud");
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = bridgeWhiteMat;

            float xInner = -8.52f;
            float xOuterBase = -11.40f;
            float[] zs = new float[] { 67.35f, 70.50f, 76.50f, 83.00f, 90.00f };
            float[] innerTopYs = new float[] { 0.36f, 0.38f, 0.50f, 0.70f, 0.96f };

            for (int s = 1; s <= 4; s++)
            {
                GameObject seg = GameObject.Find($"WestWalkway_Slab_Seg0{s}");
                if (seg != null)
                {
                    Renderer r = seg.GetComponent<Renderer>();
                    if (r != null) Debug.Log($"[WestTriangleAndAbasoloBuilder] WestWalkway_Slab_Seg0{s} bounds minY={r.bounds.min.y:F2}, maxY={r.bounds.max.y:F2}, z=[{r.bounds.min.z:F2},{r.bounds.max.z:F2}]");
                }
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int i = 0; i < zs.Length - 1; i++)
            {
                float z0 = zs[i];
                float z1 = zs[i + 1];
                float y0 = innerTopYs[i];
                float y1 = innerTopYs[i + 1];
                float yToe = -0.05f;

                Vector3 iTop0 = new Vector3(xInner, y0, z0);
                Vector3 iTop1 = new Vector3(xInner, y1, z1);
                Vector3 oTop0 = new Vector3(xOuterBase, 0.08f, z0);
                Vector3 oTop1 = new Vector3(xOuterBase, 0.08f, z1);
                Vector3 oBot0 = new Vector3(xOuterBase, yToe, z0);
                Vector3 oBot1 = new Vector3(xOuterBase, yToe, z1);
                Vector3 iBot0 = new Vector3(xInner, yToe, z0);
                Vector3 iBot1 = new Vector3(xInner, yToe, z1);

                int b = verts.Count;
                verts.AddRange(new Vector3[] { iTop0, iTop1, oTop1, oTop0, oBot0, oBot1, iBot1, iBot0 });
                for (int k = 0; k < 8; k++) uvs.Add(new Vector2(verts[b + k].z * 0.2f, verts[b + k].y * 0.5f));

                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 0, b + 2, b + 3 });
                tris.AddRange(new int[] { b + 3, b + 2, b + 5, b + 3, b + 5, b + 4 });
                tris.AddRange(new int[] { b + 1, b + 0, b + 7, b + 1, b + 7, b + 6 });
                if (i == 0) tris.AddRange(new int[] { b + 0, b + 3, b + 4, b + 0, b + 4, b + 7 });
                if (i == zs.Length - 2) tris.AddRange(new int[] { b + 2, b + 1, b + 6, b + 2, b + 6, b + 5 });
            }

            Mesh m = new Mesh { name = "Bridge_West_RetainingTalud_Mesh" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = m;
        }

        // =========================================================================
        // STEP 3: FRONT DIAGONAL STREET, CALLE GARDENIAS WEST & Y-JUNCTION
        // =========================================================================

        private static void BuildDiagonalStreetAndYJunction(Material asphaltMat, Material concreteMat, Material curbMat, Material whiteStripeMat, Material urbanLotMat)
        {
            GameObject roadRoot = GameObject.Find("Roadway_Gardenias_West_Root");
            if (roadRoot == null)
            {
                Debug.LogError("[WestTriangleAndAbasoloBuilder] Roadway_Gardenias_West_Root not found.");
                return;
            }

            // Remove old blocky rectangle asphalt
            GameObject oldMainAsphalt = GameObject.Find("Roadway_Gardenias_West_MainAsphalt");
            if (oldMainAsphalt != null) UnityEngine.Object.DestroyImmediate(oldMainAsphalt);

            Transform oldUpgrade = roadRoot.transform.Find("DiagonalAndJunction_Upgrade_Root");
            if (oldUpgrade != null) UnityEngine.Object.DestroyImmediate(oldUpgrade.gameObject);

            GameObject diagRoot = new GameObject("DiagonalAndJunction_Upgrade_Root");
            diagRoot.transform.SetParent(roadRoot.transform);
            diagRoot.transform.localPosition = Vector3.zero;

            // SW Curb of Diagonal Street shifted 10.50m along NormalOut from NE curb line Z = 37.98 - 0.55*(X + 12.20)
            Vector2 swShift = NormalOut * DiagRoadWidth; // (-5.060, -9.200)
            Vector2 swRef = new Vector2(-12.20f, 37.98f) + swShift; // (-17.260, 28.780)
            float zFork = 34.50f;
            float xFork = swRef.x + (zFork - swRef.y) / DiagSlopeDzDx; // -27.660m
            float xJuncOuter = swRef.x + (AbasoloSouthZ - swRef.y) / DiagSlopeDzDx; // -63.115m

            // Beveled urban corner block between Calle Gardenias West (Z=34.50m), Diagonal Street (28.81 deg) & Street_Abasolo (Z=54.00m)
            Vector2 pForkCutEast = new Vector2(xFork - 3.60f, zFork);                 // (-31.26, 34.50)
            Vector2 pForkOnDiag = new Vector2(xFork, zFork) + DirWNW * 3.80f;         // along SW diagonal line
            Vector2 pJuncOnDiag = new Vector2(xJuncOuter, AbasoloSouthZ) - DirWNW * 2.80f;
            Vector2 pJuncOnAbasolo = new Vector2(xJuncOuter - 3.20f, AbasoloSouthZ);  // (-66.315, 54.00)

            // Single seamless flat manifold mesh spanning out to X=-150m & filling both bevel corners cleanly
            BuildMergedWestRoadwayMesh(diagRoot.transform, xFork, zFork, xJuncOuter,
                pForkCutEast, pForkOnDiag, pJuncOnDiag, pJuncOnAbasolo, asphaltMat);

            List<Vector2> wedgePoly = new List<Vector2>
            {
                new Vector2(-150.00f, zFork),
                pForkCutEast,
                pForkOnDiag,
                pJuncOnDiag,
                pJuncOnAbasolo,
                new Vector2(-150.00f, AbasoloSouthZ)
            };
            CreateFlatPrism("UrbanCorner_Gardenias_West_Block", diagRoot.transform, wedgePoly, SidewalkBotY, SidewalkTopY, concreteMat, addCollider: true);

            // Exact parallel inner courtyard infill with uniform 2.20m sidewalk border (0 sawtooth / self-intersection!)
            float borderW = 2.20f;
            Vector2 innerDiagRef = swRef + NormalOut * borderW; // shifted 2.20m perpendicularly INTO the block
            float innerZBot = zFork + borderW;
            float innerZTop = AbasoloSouthZ - borderW;
            float innerXBot = innerDiagRef.x + (innerZBot - innerDiagRef.y) / DiagSlopeDzDx;
            float innerXTop = innerDiagRef.x + (innerZTop - innerDiagRef.y) / DiagSlopeDzDx;

            Vector2 pInnerForkEast = new Vector2(innerXBot - 2.20f, innerZBot);
            Vector2 pInnerForkDiag = new Vector2(innerXBot, innerZBot) + DirWNW * 2.40f;
            Vector2 pInnerJuncDiag = new Vector2(innerXTop, innerZTop) - DirWNW * 1.80f;
            Vector2 pInnerJuncAbasolo = new Vector2(innerXTop - 2.00f, innerZTop);

            List<Vector2> innerLotPoly = new List<Vector2>
            {
                new Vector2(-148.00f, innerZBot),
                pInnerForkEast,
                pInnerForkDiag,
                pInnerJuncDiag,
                pInnerJuncAbasolo,
                new Vector2(-148.00f, innerZTop)
            };
            CreateFlatPrism("UrbanCorner_West_InnerLot", diagRoot.transform, innerLotPoly, SidewalkTopY - 0.01f, SidewalkTopY + 0.015f, urbanLotMat, addCollider: false);

            // Clean concrete curbs along the outer sidewalk bevels
            CreateCurbSegment("Curb_Gardenias_West_NorthFork", diagRoot.transform, new Vector2(-150.00f, zFork), pForkCutEast, SidewalkTopY, curbMat, 0.20f, Vector2.up);
            CreateCurbSegment("Curb_Fork_Nose_Bevel", diagRoot.transform, pForkCutEast, pForkOnDiag, SidewalkTopY, curbMat, 0.20f, NormalOut);
            CreateCurbSegment("Curb_Diagonal_West_SW", diagRoot.transform, pForkOnDiag, pJuncOnDiag, SidewalkTopY, curbMat, 0.20f, NormalOut);
            CreateCurbSegment("Curb_Junction_Corner_Bevel", diagRoot.transform, pJuncOnDiag, pJuncOnAbasolo, SidewalkTopY, curbMat, 0.20f, Vector2.down);

            // Update Street_Abasolo West south curb to meet pJuncOnAbasolo cleanly
            GameObject farWestCurb = GameObject.Find("Abasolo_Curb_South_FarW");
            if (farWestCurb != null)
            {
                float farWLen = pJuncOnAbasolo.x - (-150.00f);
                farWestCurb.transform.position = new Vector3(-150.00f + farWLen * 0.5f, 0.10f, AbasoloSouthZ - 0.10f);
                farWestCurb.transform.localScale = new Vector3(farWLen, 0.20f, 0.20f);
            }

            // South curb & sidewalk continuation along Calle Gardenias West (X = -18.5..-150.0, Z = 16.0..24.30m)
            CreateBox("Sidewalk_Gardenias_West_SouthExt", diagRoot.transform,
                new Vector3(-84.25f, 0.10f, 20.15f), new Vector3(131.50f, 0.20f, 8.30f), concreteMat, keepCollider: true);
            CreateBox("Curb_Gardenias_West_SouthExt", diagRoot.transform,
                new Vector3(-84.25f, 0.10f, 24.20f), new Vector3(131.50f, 0.20f, 0.20f), curbMat, keepCollider: true);

            // Dashed white centerline along Diagonal Avenue (centered at 5.25m outward from NE curb)
            Vector2 diagCenterRef = new Vector2(-12.20f, 37.98f) + NormalOut * (DiagRoadWidth * 0.5f);
            Quaternion dashRot = Quaternion.Euler(0f, DiagYawDeg, 0f);
            for (int i = 0; i < 9; i++)
            {
                float distAlongWNW = 7.5f + i * 4.4f;
                Vector2 cPt = diagCenterRef + DirWNW * distAlongWNW;
                if (cPt.y > AbasoloSouthZ - 1.0f) break;
                CreateRotatedBox($"DiagRoad_CenterDash_{i + 1}", diagRoot.transform,
                    new Vector3(cPt.x, MarkingsY - 0.004f, cPt.y),
                    new Vector3(2.20f, 0.012f, 0.15f),
                    dashRot, whiteStripeMat);
            }

            // Dashed white centerline along Calle Gardenias West extension (-36m to -146m at Z=29.40m)
            for (float x = -38.0f; x >= -146.0f; x -= 6.0f)
            {
                CreateBox($"GardeniasWest_Dash_{Mathf.Abs(x):F0}", diagRoot.transform,
                    new Vector3(x, MarkingsY - 0.004f, 29.40f),
                    new Vector3(2.80f, 0.012f, 0.15f), whiteStripeMat);
            }
        }

        private static void BuildMergedWestRoadwayMesh(Transform parent, float xFork, float zFork, float xJuncOuter,
            Vector2 pForkCutEast, Vector2 pForkOnDiag, Vector2 pJuncOnDiag, Vector2 pJuncOnAbasolo, Material asphaltMat)
        {
            GameObject go = new GameObject("Roadway_Gardenias_West_MergedAsphalt");
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = asphaltMat;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            void AddTriPrism(Vector2 p0, Vector2 p1, Vector2 p2)
            {
                float cross = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
                if (cross > 0f) { Vector2 tmp = p1; p1 = p2; p2 = tmp; }
                int b = verts.Count;
                Vector2[] pts = { p0, p1, p2 };
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(new Vector3(pts[i].x, RoadTopY, pts[i].y));
                    uvs.Add(new Vector2(pts[i].x * 0.1f, pts[i].y * 0.1f));
                }
                for (int i = 0; i < 3; i++)
                {
                    verts.Add(new Vector3(pts[i].x, RoadBotY, pts[i].y));
                    uvs.Add(new Vector2(pts[i].x * 0.1f, pts[i].y * 0.1f));
                }
                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 3, b + 5, b + 4 });
                for (int i = 0; i < 3; i++)
                {
                    int n = (i + 1) % 3;
                    tris.AddRange(new int[] { b + i, b + 3 + n, b + n, b + i, b + 3 + i, b + 3 + n });
                }
            }

            void AddQuadPrism(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
            {
                float cross = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
                if (cross > 0f) { Vector2 tmp = p1; p1 = p3; p3 = tmp; }

                int b = verts.Count;
                Vector2[] pts = { p0, p1, p2, p3 };
                for (int i = 0; i < 4; i++)
                {
                    verts.Add(new Vector3(pts[i].x, RoadTopY, pts[i].y));
                    uvs.Add(new Vector2(pts[i].x * 0.1f, pts[i].y * 0.1f));
                }
                for (int i = 0; i < 4; i++)
                {
                    verts.Add(new Vector3(pts[i].x, RoadBotY, pts[i].y));
                    uvs.Add(new Vector2(pts[i].x * 0.1f, pts[i].y * 0.1f));
                }
                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 0, b + 2, b + 3 });
                tris.AddRange(new int[] { b + 4, b + 6, b + 5, b + 4, b + 7, b + 6 });
                for (int i = 0; i < 4; i++)
                {
                    int n = (i + 1) % 4;
                    tris.AddRange(new int[] { b + i, b + 4 + n, b + n, b + i, b + 4 + i, b + 4 + n });
                }
            }

            // Quad A: Calle Gardenias West main asphalt (-150.00..-6.90, Z=24.30..34.50)
            AddQuadPrism(
                new Vector2(-150.00f, 24.30f),
                new Vector2(-150.00f, zFork),
                new Vector2(-6.90f, zFork),
                new Vector2(-6.90f, 24.30f));

            // Sub-quad B1: Mouth bridge between Z=34.50 and (-8.35, 35.80) / (-6.90, 35.80)
            AddQuadPrism(
                new Vector2(xFork, zFork),
                new Vector2(-8.35f, 35.80f),
                new Vector2(-6.90f, 35.80f),
                new Vector2(-6.90f, zFork));

            // Sub-quad B2: Diagonal corridor from (-27.66, 34.50)..(-8.35, 35.80) up to (-63.115, 54.00)..(-41.05, 53.85)
            AddQuadPrism(
                new Vector2(xFork, zFork),
                new Vector2(xJuncOuter, AbasoloSouthZ),
                new Vector2(-41.05f, 53.85f),
                new Vector2(-8.35f, 35.80f));

            // Sub-tri B3: Y-junction tip triangle (-63.115, 54.00) -> (-40.65, 54.00) -> (-41.05, 53.85)
            AddTriPrism(
                new Vector2(xJuncOuter, AbasoloSouthZ),
                new Vector2(-40.65f, AbasoloSouthZ),
                new Vector2(-41.05f, 53.85f));

            // Exact flush bevel corner asphalt fills (zero overlap, zero ground gap outside sidewalk bevels!)
            AddTriPrism(pForkCutEast, new Vector2(xFork, zFork), pForkOnDiag);
            AddTriPrism(pJuncOnDiag, new Vector2(xJuncOuter, AbasoloSouthZ), pJuncOnAbasolo);

            Mesh m = new Mesh { name = "Roadway_Gardenias_West_MergedMesh" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = m;
        }

        // =========================================================================
        // STEP 4: UNITE CALLE GARDENIAS EAST & ABASOLO + CENTRAL DELIMITED BLOCK
        // =========================================================================

        private struct SpinePt2D
        {
            public Vector2 pos;
            public Vector2 tan;
            public Vector2 leftNorm; // (-tan.y, tan.x) -> points North (+Z) when tan is East (+X)
            public float sAlong;
        }

        private static Vector2 EvalAbasoloOffsetXZ(float s, float offset, out Vector2 tanXZ)
        {
            AbasoloSample sp = EvaluateAbasolo(s);
            tanXZ = new Vector2(sp.tangentXZ.x, sp.tangentXZ.z).normalized;
            return new Vector2(sp.center.x + sp.northNormalXZ.x * offset, sp.center.z + sp.northNormalXZ.z * offset);
        }

        private static Vector2 EvalCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, out Vector2 tanOut)
        {
            float u = 1f - t;
            Vector2 pos = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            Vector2 d = 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
            tanOut = d.sqrMagnitude > 1e-6f ? d.normalized : (p3 - p0).normalized;
            return pos;
        }

        private static List<Vector2> SampleBezierChain(Vector2 p0, Vector2 t0, Vector2 p3, Vector2 t3, float handleLen, int steps)
        {
            Vector2 p1 = p0 + t0.normalized * handleLen;
            Vector2 p2 = p3 - t3.normalized * handleLen;
            List<Vector2> pts = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                pts.Add(EvalCubicBezier(p0, p1, p2, p3, (float)i / steps, out _));
            }
            return pts;
        }

        // =========================================================================
        // OSM-ALIGNED EAST CORRIDOR NETWORK & DELIMITED BLOCKS (CATMULL-ROM ENGINE)
        // =========================================================================

        private class OsmNetworkData
        {
            public List<SpinePt2D> abaSpine;       // s >= 24 (Abasolo curve + Herrera East [21]+[39])
            public List<SpinePt2D> fvSpine;        // Av. Francisco Villa [1]+[26]+[20]
            public List<SpinePt2D> busSpine;       // Linea Express Busway [12]+[35]+[7]
            public List<SpinePt2D> gardSpine;      // Calle Gardenias East + [32] + [6] Herrera East
            public List<SpinePt2D> azSpine;        // Calle Azucenas East [3]+[33]
            public List<SpinePt2D> dobladSpine;    // Calle Manuel Doblado [5] south stub
            public List<SpinePt2D> srvSpine;       // Interior Service / Linea Express [22]+[14]
            public List<SpinePt2D> srvNorthSpine;  // Service North [25]

            public List<Vector2> fvWestEdge;
            public List<Vector2> fvEastEdge;
            public List<Vector2> busWestEdge;
            public List<Vector2> busEastEdge;
            public List<Vector2> gardNorthEdge;
            public List<Vector2> gardSouthEdge;
            public List<Vector2> azNorthEdge;
            public List<Vector2> azSouthEdge;
            public List<Vector2> dobladWestEdge;
            public List<Vector2> dobladEastEdge;
            public List<Vector2> srvNorthEdge;
            public List<Vector2> srvSouthEdge;
            public List<Vector2> srvNorthWestEdge;
            public List<Vector2> srvNorthEastEdge;

            public List<Vector2> manzanaOuterPoly;
            public List<Vector2> manzanaLotPoly;
            public List<Vector2> cbNeOuterPoly;
            public List<Vector2> cbNeLotPoly;
            public List<Vector2> cbSwOuterPoly;
            public List<Vector2> cbSwLotPoly;
            public List<Vector2> medianNorthPoly;
            public List<Vector2> medianMidPoly;
            public List<Vector2> medianSouthPoly;
        }

        private static List<SpinePt2D> SampleCentripetalCR(Vector2[] ctrlPts, int stepsPerSeg,
            Vector2? startExt = null, Vector2? endExt = null)
        {
            int n = ctrlPts.Length;
            Vector2 pFirst = startExt ?? (2f * ctrlPts[0] - ctrlPts[1]);
            Vector2 pLast = endExt ?? (2f * ctrlPts[n - 1] - ctrlPts[n - 2]);
            Vector2[] ext = new Vector2[n + 2];
            ext[0] = pFirst;
            Array.Copy(ctrlPts, 0, ext, 1, n);
            ext[n + 1] = pLast;

            List<SpinePt2D> raw = new List<SpinePt2D>((n - 1) * stepsPerSeg + 2);
            float Tj(float ti, Vector2 a, Vector2 b) => ti + Mathf.Sqrt(Mathf.Max(1e-4f, Vector2.Distance(a, b)));
            Vector2 LerpT(Vector2 pa, Vector2 pb, float ta, float tb, float tv)
            {
                float d = tb - ta;
                return Mathf.Abs(d) < 1e-6f ? pa : Vector2.Lerp(pa, pb, (tv - ta) / d);
            }

            for (int i = 1; i < ext.Length - 2; i++)
            {
                Vector2 p0 = ext[i - 1], p1 = ext[i], p2 = ext[i + 1], p3 = ext[i + 2];
                float t0 = 0f, t1 = Tj(t0, p0, p1), t2 = Tj(t1, p1, p2), t3 = Tj(t2, p2, p3);
                float d01 = Mathf.Max(1e-6f, t1 - t0), d12 = Mathf.Max(1e-6f, t2 - t1), d23 = Mathf.Max(1e-6f, t3 - t2);
                float d02 = Mathf.Max(1e-6f, t2 - t0), d13 = Mathf.Max(1e-6f, t3 - t1);
                Vector2 dA1 = (p1 - p0) / d01, dA2 = (p2 - p1) / d12, dA3 = (p3 - p2) / d23;

                int k0 = (i == 1) ? 0 : 1;
                for (int k = k0; k <= stepsPerSeg; k++)
                {
                    float u = (float)k / stepsPerSeg;
                    float t = t1 + u * (t2 - t1);
                    Vector2 A1 = LerpT(p0, p1, t0, t1, t);
                    Vector2 A2 = LerpT(p1, p2, t1, t2, t);
                    Vector2 A3 = LerpT(p2, p3, t2, t3, t);
                    Vector2 B1 = LerpT(A1, A2, t0, t2, t);
                    Vector2 B2 = LerpT(A2, A3, t1, t3, t);
                    Vector2 C  = LerpT(B1, B2, t1, t2, t);

                    Vector2 dB1 = (A2 - A1 + (t2 - t) * dA1 + (t - t0) * dA2) / d02;
                    Vector2 dB2 = (A3 - A2 + (t3 - t) * dA2 + (t - t1) * dA3) / d13;
                    Vector2 dC  = (B2 - B1 + (t2 - t) * dB1 + (t - t1) * dB2) / d12;
                    Vector2 tan = dC.sqrMagnitude > 1e-8f ? dC.normalized : (p2 - p1).normalized;
                    raw.Add(new SpinePt2D { pos = C, tan = tan, leftNorm = new Vector2(-tan.y, tan.x), sAlong = 0f });
                }
            }

            float sAcc = 0f;
            for (int i = 0; i < raw.Count; i++)
            {
                if (i > 0) sAcc += Vector2.Distance(raw[i - 1].pos, raw[i].pos);
                SpinePt2D pt = raw[i];
                pt.sAlong = sAcc;
                raw[i] = pt;
            }
            return raw;
        }

        private static bool SegmentsIntersectXZ(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit)
        {
            Vector2 r = b - a, s = d - c;
            float den = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(den) < 1e-6f) { hit = Vector2.zero; return false; }
            Vector2 qmp = c - a;
            float t = (qmp.x * s.y - qmp.y * s.x) / den;
            float u = (qmp.x * r.y - qmp.y * r.x) / den;
            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f) { hit = a + r * t; return true; }
            hit = Vector2.zero;
            return false;
        }

        private static bool FindSegmentPolylineHit(Vector2 a, Vector2 b, List<Vector2> poly, out Vector2 hit, out int segIdx)
        {
            for (int i = 0; i < poly.Count - 1; i++)
            {
                if (SegmentsIntersectXZ(a, b, poly[i], poly[i + 1], out hit)) { segIdx = i; return true; }
            }
            hit = Vector2.zero; segIdx = -1;
            return false;
        }

        private static bool FindPolylinePolylineIntersection(List<Vector2> a, List<Vector2> b, int startA, int startB,
            out Vector2 hit, out int idxA, out int idxB)
        {
            for (int i = startA; i < a.Count - 1; i++)
            {
                for (int j = startB; j < b.Count - 1; j++)
                {
                    if (SegmentsIntersectXZ(a[i], a[i + 1], b[j], b[j + 1], out hit))
                    {
                        idxA = i; idxB = j; return true;
                    }
                }
            }
            hit = Vector2.zero; idxA = -1; idxB = -1;
            return false;
        }

        private static bool FindAbasoloPolylineHitS(List<Vector2> poly, float lateralOffset, out float sHit)
        {
            AbasoloSample prevSp = EvaluateAbasolo(50.0f);
            Vector2 prevP = new Vector2(prevSp.center.x, prevSp.center.z) + new Vector2(prevSp.northNormalXZ.x, prevSp.northNormalXZ.z) * lateralOffset;
            for (float s = 50.5f; s <= 145.0f; s += 0.5f)
            {
                AbasoloSample sp = EvaluateAbasolo(s);
                Vector2 curP = new Vector2(sp.center.x, sp.center.z) + new Vector2(sp.northNormalXZ.x, sp.northNormalXZ.z) * lateralOffset;
                if (FindSegmentPolylineHit(prevP, curP, poly, out Vector2 hit, out _))
                {
                    float frac = Vector2.Distance(prevP, hit) / Mathf.Max(1e-5f, Vector2.Distance(prevP, curP));
                    sHit = Mathf.Lerp(s - 0.5f, s, frac);
                    return true;
                }
                prevP = curP;
            }
            sHit = 0f;
            return false;
        }

        private static List<Vector2> OffsetSpinePts(List<SpinePt2D> spine, Func<float, float> offsetByX)
        {
            List<Vector2> res = new List<Vector2>(spine.Count);
            for (int i = 0; i < spine.Count; i++) res.Add(spine[i].pos + spine[i].leftNorm * offsetByX(spine[i].pos.x));
            return res;
        }

        private static float EvalCurveXAtZ(List<Vector2> pts, float targetZ)
        {
            float bestDiff = float.MaxValue;
            float bestX = pts[0].x;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                if ((a.y >= targetZ && b.y <= targetZ) || (a.y <= targetZ && b.y >= targetZ))
                {
                    float dz = b.y - a.y;
                    float t = Mathf.Abs(dz) < 1e-6f ? 0f : (targetZ - a.y) / dz;
                    return Mathf.Lerp(a.x, b.x, t);
                }
                float d = Mathf.Abs(a.y - targetZ);
                if (d < bestDiff) { bestDiff = d; bestX = a.x; }
            }
            return bestX;
        }

        private static bool ClipEdgeAtZ(List<Vector2> pts, float zVal, out Vector2 pt)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                if ((a.y >= zVal && b.y <= zVal) || (a.y <= zVal && b.y >= zVal))
                {
                    float dz = b.y - a.y;
                    float t = Mathf.Abs(dz) < 1e-6f ? 0f : (zVal - a.y) / dz;
                    pt = Vector2.Lerp(a, b, t);
                    return true;
                }
            }
            pt = Vector2.zero;
            return false;
        }

        private static List<Vector2> BuildClippedMedianPoly(List<Vector2> fvEast, List<Vector2> busWest,
            float zHighFv, float zHighBus, float zLowFv, float zLowBus)
        {
            List<Vector2> poly = new List<Vector2>();
            if (ClipEdgeAtZ(fvEast, zHighFv, out Vector2 fvTop)) poly.Add(new Vector2(fvTop.x + 0.25f, fvTop.y));
            for (int i = 0; i < fvEast.Count; i++) if (fvEast[i].y < zHighFv && fvEast[i].y > zLowFv) poly.Add(new Vector2(fvEast[i].x + 0.25f, fvEast[i].y));
            if (ClipEdgeAtZ(fvEast, zLowFv, out Vector2 fvBot)) poly.Add(new Vector2(fvBot.x + 0.25f, fvBot.y));

            if (ClipEdgeAtZ(busWest, zLowBus, out Vector2 busBot)) poly.Add(new Vector2(busBot.x - 0.25f, busBot.y));
            for (int i = busWest.Count - 1; i >= 0; i--) if (busWest[i].y < zHighBus && busWest[i].y > zLowBus) poly.Add(new Vector2(busWest[i].x - 0.25f, busWest[i].y));
            if (ClipEdgeAtZ(busWest, zHighBus, out Vector2 busTop)) poly.Add(new Vector2(busTop.x - 0.25f, busTop.y));
            return poly;
        }

        private static float MinDistToPoly(Vector2 p, List<Vector2> poly, int i0, int i1, out int segIdx, out Vector2 closestPt)
        {
            float best = float.MaxValue;
            segIdx = Mathf.Clamp(i0, 0, Mathf.Max(0, poly.Count - 2));
            closestPt = poly[segIdx];
            int lo = Mathf.Clamp(i0, 0, Mathf.Max(0, poly.Count - 2));
            int hi = Mathf.Clamp(i1 - 1, lo, Mathf.Max(0, poly.Count - 2));
            for (int i = lo; i <= hi; i++)
            {
                Vector2 a = poly[i], b = poly[i + 1], ab = b - a;
                float l2 = ab.sqrMagnitude;
                float t = l2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                Vector2 q = a + ab * t;
                float d = Vector2.Distance(p, q);
                if (d < best) { best = d; segIdx = i; closestPt = q; }
            }
            return best;
        }

        private static void AppendRoundedWedgeNose(List<Vector2> poly, Vector2 fromPt, Vector2 toPt, Vector2 hitPt, float outExt = 0.25f)
        {
            Vector2 mid = 0.5f * (fromPt + toPt);
            Vector2 dir = hitPt - mid;
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;
            poly.Add(mid + dir * outExt);
        }

        private static float EvalGardHalfWidth(float x)
        {
            if (x <= 42f) return 3.802f;
            if (x >= 76f) return 3.300f;
            return Mathf.Lerp(3.802f, 3.300f, (x - 42f) / 34f);
        }

        private static float EvalAbaEastHalfLeft(float x) => x <= 90f ? AbasoloNorth3LaneOffset : (x >= 104f ? 4.20f : Mathf.Lerp(AbasoloNorth3LaneOffset, 4.20f, (x - 90f) / 14f));
        private static float EvalAbaEastHalfRight(float x) => x <= 90f ? AbasoloSouthOffset : (x >= 104f ? -4.20f : Mathf.Lerp(AbasoloSouthOffset, -4.20f, (x - 90f) / 14f));

        private static OsmNetworkData BuildOsmNetworkData()
        {
            OsmNetworkData net = new OsmNetworkData();

            net.abaSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(24.00f, 59.00f), new Vector2(32.50f, 59.00f), new Vector2(39.68f, 59.00f),
                new Vector2(47.49f, 53.66f), new Vector2(56.00f, 46.27f), new Vector2(59.46f, 42.48f),
                new Vector2(68.67f, 30.26f), new Vector2(75.82f, 19.61f), new Vector2(82.75f, 4.99f),
                new Vector2(86.78f, -2.16f), new Vector2(93.42f, -10.40f),new Vector2(99.21f, -15.53f),
                new Vector2(105.35f,-18.16f),new Vector2(117.87f,-22.04f),new Vector2(155.00f,-30.26f)
            }, 10, startExt: new Vector2(16.00f, 59.00f));

            net.fvSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(79.50f, 118.00f), new Vector2(89.28f, 100.22f), new Vector2(91.77f, 91.92f),
                new Vector2(93.74f, 74.88f),  new Vector2(93.15f, 42.00f),  new Vector2(92.61f, 8.72f),
                new Vector2(93.42f, -10.40f), new Vector2(94.11f, -28.77f), new Vector2(94.22f, -34.69f),
                new Vector2(93.90f, -65.00f)
            }, 10);

            net.busSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(107.13f, 72.73f), new Vector2(106.67f, 65.41f), new Vector2(105.33f, -5.13f),
                new Vector2(105.35f,-18.16f), new Vector2(104.92f,-32.10f), new Vector2(105.20f,-42.51f),
                new Vector2(105.00f,-65.00f)
            }, 8);

            float ang8 = 8.0f * Mathf.Deg2Rad;
            Vector2 pG0 = new Vector2(23.50f, 27.4655f);
            Vector2 tG0 = new Vector2(Mathf.Cos(ang8), -Mathf.Sin(ang8)).normalized;
            Vector2 pGWest = pG0 - tG0 * (12.80f / Mathf.Cos(ang8));

            List<SpinePt2D> gardCr = SampleCentripetalCR(new Vector2[]
            {
                pG0, new Vector2(29.40f, 24.10f), new Vector2(35.88f, 15.86f), new Vector2(46.50f, 5.80f),
                new Vector2(58.50f, -5.00f), new Vector2(69.86f, -15.52f), new Vector2(77.43f, -22.56f),
                new Vector2(82.98f, -25.45f), new Vector2(89.34f, -27.63f), new Vector2(94.11f, -28.77f),
                new Vector2(99.12f, -29.76f), new Vector2(104.92f, -32.10f), new Vector2(116.03f, -33.86f),
                new Vector2(155.00f, -43.08f)
            }, 10, startExt: pGWest);
            net.gardSpine = new List<SpinePt2D>(gardCr.Count + 12);
            float segLen = Vector2.Distance(pGWest, pG0);
            for (int i = 0; i < 12; i++)
            {
                float frac = (float)i / 12f;
                net.gardSpine.Add(new SpinePt2D { pos = Vector2.Lerp(pGWest, pG0, frac), tan = tG0, leftNorm = new Vector2(-tG0.y, tG0.x), sAlong = segLen * frac });
            }
            for (int i = 0; i < gardCr.Count; i++)
            {
                SpinePt2D pt = gardCr[i];
                pt.sAlong += segLen;
                net.gardSpine.Add(pt);
            }

            net.azSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(6.90f, -32.92f),  new Vector2(28.00f, -33.36f), new Vector2(50.42f, -33.81f),
                new Vector2(72.00f, -34.35f), new Vector2(89.12f, -34.81f), new Vector2(94.22f, -34.69f),
                new Vector2(99.20f, -33.81f), new Vector2(104.92f, -32.10f)
            }, 8);

            net.dobladSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(50.42f, -33.81f), new Vector2(49.70f, -49.00f), new Vector2(49.00f, -65.00f)
            }, 6);

            net.srvSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(24.50f, 27.30f), new Vector2(29.80f, 30.00f), new Vector2(35.46f, 32.10f),
                new Vector2(45.15f, 26.05f), new Vector2(54.40f, 14.75f), new Vector2(73.11f, -1.49f),
                new Vector2(76.65f, -4.17f), new Vector2(87.36f, -10.27f), new Vector2(93.42f, -10.40f)
            }, 8);

            net.srvNorthSpine = SampleCentripetalCR(new Vector2[]
            {
                new Vector2(78.84f, 17.50f), new Vector2(79.55f, 24.42f), new Vector2(79.87f, 26.82f),
                new Vector2(78.10f, 62.00f), new Vector2(76.47f, 97.05f), new Vector2(75.81f, 101.25f)
            }, 8);

            // Edges: remember for S-bound spines (FV, Busway, Doblado), leftNorm points East (+X) and -leftNorm points West (-X)
            net.fvWestEdge = OffsetSpinePts(net.fvSpine, _ => -FVHalfWidth);
            net.fvEastEdge = OffsetSpinePts(net.fvSpine, _ => FVHalfWidth);
            net.busWestEdge = OffsetSpinePts(net.busSpine, _ => -BuswayHalfWidth);
            net.busEastEdge = OffsetSpinePts(net.busSpine, _ => BuswayHalfWidth);
            net.gardNorthEdge = OffsetSpinePts(net.gardSpine, EvalGardHalfWidth);
            net.gardSouthEdge = OffsetSpinePts(net.gardSpine, x => -EvalGardHalfWidth(x));
            net.azNorthEdge = OffsetSpinePts(net.azSpine, _ => 3.80f);
            net.azSouthEdge = OffsetSpinePts(net.azSpine, _ => -3.80f);
            net.dobladWestEdge = OffsetSpinePts(net.dobladSpine, _ => -3.80f);
            net.dobladEastEdge = OffsetSpinePts(net.dobladSpine, _ => 3.80f);
            net.srvNorthEdge = OffsetSpinePts(net.srvSpine, _ => 2.40f);
            net.srvSouthEdge = OffsetSpinePts(net.srvSpine, _ => -2.40f);
            net.srvNorthWestEdge = OffsetSpinePts(net.srvNorthSpine, _ => 2.40f);
            net.srvNorthEastEdge = OffsetSpinePts(net.srvNorthSpine, _ => -2.40f);

            // 1. Manzana Sur outer polygon (Azucenas NW fillet -> Sidewalk_East -> NorthBlock -> Gardenias South -> rounded SE nose -> Azucenas North)
            bool hasNose = FindPolylinePolylineIntersection(net.gardSouthEdge, net.azNorthEdge, 14, 0,
                out Vector2 nosePt, out int gCut, out int aCut);
            int gLim = hasNose ? gCut : net.gardSouthEdge.Count - 1;
            int aLim = hasNose ? aCut : net.azNorthEdge.Count - 1;

            float azNorthAt12 = net.azNorthEdge[0].y;
            for (int j = 0; j < net.azNorthEdge.Count - 1; j++)
            {
                if (net.azNorthEdge[j].x <= 12.00f && net.azNorthEdge[j + 1].x >= 12.00f)
                {
                    float u = Mathf.InverseLerp(net.azNorthEdge[j].x, net.azNorthEdge[j + 1].x, 12.00f);
                    azNorthAt12 = Mathf.Lerp(net.azNorthEdge[j].y, net.azNorthEdge[j + 1].y, u);
                    break;
                }
            }

            net.manzanaOuterPoly = new List<Vector2> { new Vector2(12.00f, -21.50f), new Vector2(24.00f, -21.50f), new Vector2(24.00f, 22.00f) };
            Vector2 gTipPt = net.gardSouthEdge[14];
            Vector2 aTipPt = net.azNorthEdge[aLim];
            int aTipIdx = Mathf.Max(0, aLim - 1);
            for (int i = 0; i <= gLim; i++)
            {
                Vector2 p = net.gardSouthEdge[i];
                if (p.x < 24.50f || p.y > 23.00f) continue;
                float d = MinDistToPoly(p, net.azNorthEdge, 0, aLim, out int mIdx, out Vector2 q);
                if (d < 1.55f && p.x > 60.0f) break;
                net.manzanaOuterPoly.Add(p);
                gTipPt = p;
                aTipIdx = mIdx;
                aTipPt = q;
            }
            AppendRoundedWedgeNose(net.manzanaOuterPoly, gTipPt, aTipPt, hasNose ? nosePt : gTipPt + Vector2.right, 0.25f);
            net.manzanaOuterPoly.Add(aTipPt);
            for (int j = aTipIdx; j >= 0; j--)
            {
                Vector2 p = net.azNorthEdge[j];
                if (p.x >= 12.00f && Vector2.Distance(p, aTipPt) > 0.08f) net.manzanaOuterPoly.Add(p);
            }
            net.manzanaOuterPoly.Add(new Vector2(12.00f, azNorthAt12));

            // Inner Manzana urban lot pad (inset 1.60m inside curbs, strictly East of X=24 or South of Z=-21.5, ZERO overlap with existing stores!)
            List<Vector2> gardSouthIn = OffsetSpinePts(net.gardSpine, x => -(EvalGardHalfWidth(x) + 1.60f));
            List<Vector2> azNorthIn = OffsetSpinePts(net.azSpine, _ => 3.80f + 1.60f);
            bool hasInTip = FindPolylinePolylineIntersection(gardSouthIn, azNorthIn, 14, 0, out Vector2 inTip, out int giCut, out int aiCut);
            int giLim = hasInTip ? giCut : gardSouthIn.Count - 1;
            int aiLim = hasInTip ? aiCut : azNorthIn.Count - 1;
            net.manzanaLotPoly = new List<Vector2> { new Vector2(12.00f, -21.50f), new Vector2(24.00f, -21.50f), new Vector2(24.00f, 20.50f) };
            Vector2 giTip = gardSouthIn[14];
            Vector2 aiMatchPt = azNorthIn[aiLim];
            int aiMatch = Mathf.Max(0, aiLim - 1);
            for (int i = 0; i <= giLim; i++)
            {
                Vector2 p = gardSouthIn[i];
                if (p.x < 24.50f || p.y > 21.00f) continue;
                float d = MinDistToPoly(p, azNorthIn, 0, aiLim, out int mIdx, out Vector2 q);
                if (d < 0.60f && p.x > 60.0f) break;
                net.manzanaLotPoly.Add(p);
                giTip = p;
                aiMatch = mIdx;
                aiMatchPt = q;
            }
            if (hasInTip) AppendRoundedWedgeNose(net.manzanaLotPoly, giTip, aiMatchPt, inTip, 0.20f);
            net.manzanaLotPoly.Add(aiMatchPt);
            for (int j = aiMatch; j >= 0; j--)
            {
                Vector2 p = azNorthIn[j];
                if (p.x >= 12.00f && Vector2.Distance(p, aiMatchPt) > 0.08f) net.manzanaLotPoly.Add(p);
            }
            net.manzanaLotPoly.Add(new Vector2(12.00f, -21.50f));

            // 2. CentralBlock_NE (between Abasolo South wall/curb and Service North, Plaza seam X=23.50, rounded SE nose)
            List<Vector2> abaSouth = OffsetSpinePts(net.abaSpine, x => x <= 55.0f ? -5.34f : EvalAbaEastHalfRight(x));
            bool hasNeTip = FindPolylinePolylineIntersection(abaSouth, net.srvNorthEdge, 10, 10, out Vector2 cbNeTip, out int aNeCut, out int snNeCut);
            int aNeLim = hasNeTip ? aNeCut : abaSouth.Count - 1;
            int snNeLim = hasNeTip ? snNeCut : net.srvNorthEdge.Count - 1;
            net.cbNeOuterPoly = new List<Vector2>();
            Vector2 abaNeTipPt = abaSouth[0];
            Vector2 snNeMatchPt = net.srvNorthEdge[snNeLim];
            int snNeMatch = Mathf.Max(0, snNeLim - 1);
            for (int i = 0; i <= aNeLim; i++)
            {
                Vector2 p = abaSouth[i];
                if (p.x < 23.50f) continue;
                float d = MinDistToPoly(p, net.srvNorthEdge, 8, snNeLim, out int mIdx, out Vector2 q);
                if (d < 1.55f && p.x > 70.0f) break;
                net.cbNeOuterPoly.Add(p);
                abaNeTipPt = p;
                snNeMatch = mIdx;
                snNeMatchPt = q;
            }
            if (hasNeTip) AppendRoundedWedgeNose(net.cbNeOuterPoly, abaNeTipPt, snNeMatchPt, cbNeTip, 0.25f);
            net.cbNeOuterPoly.Add(snNeMatchPt);
            for (int j = snNeMatch; j >= 0; j--)
            {
                if (net.srvNorthEdge[j].x >= 28.80f && Vector2.Distance(net.srvNorthEdge[j], snNeMatchPt) > 0.08f)
                    net.cbNeOuterPoly.Add(net.srvNorthEdge[j]);
            }
            net.cbNeOuterPoly.Add(new Vector2(26.20f, 33.80f));
            net.cbNeOuterPoly.Add(new Vector2(23.50f, 34.00f));
            net.cbNeOuterPoly.Add(new Vector2(23.50f, 53.66f));

            List<Vector2> abaSouthIn = OffsetSpinePts(net.abaSpine, x => (x <= 55.0f ? -5.34f : EvalAbaEastHalfRight(x)) - 1.60f);
            List<Vector2> srvNorthIn = OffsetSpinePts(net.srvSpine, _ => 2.40f + 1.60f);
            bool hasNeInTip = FindPolylinePolylineIntersection(abaSouthIn, srvNorthIn, 12, 12, out Vector2 cbNeInTip, out int aInCut, out int snInCut);
            int aInLim = hasNeInTip ? aInCut : abaSouthIn.Count - 1;
            int snInLim = hasNeInTip ? snInCut : srvNorthIn.Count - 1;
            net.cbNeLotPoly = new List<Vector2>();
            Vector2 abaInTipPt = abaSouthIn[0];
            Vector2 snInMatchPt = srvNorthIn[snInLim];
            int snInMatch = Mathf.Max(0, snInLim - 1);
            for (int i = 0; i <= aInLim; i++)
            {
                Vector2 p = abaSouthIn[i];
                if (p.x < 25.10f) continue;
                float d = MinDistToPoly(p, srvNorthIn, 10, snInLim, out int mIdx, out Vector2 q);
                if (d < 0.60f && p.x > 68.0f) break;
                net.cbNeLotPoly.Add(p);
                abaInTipPt = p;
                snInMatch = mIdx;
                snInMatchPt = q;
            }
            if (hasNeInTip) AppendRoundedWedgeNose(net.cbNeLotPoly, abaInTipPt, snInMatchPt, cbNeInTip, 0.20f);
            net.cbNeLotPoly.Add(snInMatchPt);
            for (int j = snInMatch; j >= 0; j--)
            {
                if (srvNorthIn[j].x >= 30.20f && Vector2.Distance(srvNorthIn[j], snInMatchPt) > 0.08f)
                    net.cbNeLotPoly.Add(srvNorthIn[j]);
            }
            net.cbNeLotPoly.Add(new Vector2(25.10f, 35.80f));
            net.cbNeLotPoly.Add(new Vector2(25.10f, 52.00f));

            // 3. CentralBlock_SW (between Service South, Gardenias North, and FV West - 0.10m, rounded West nose)
            bool hasSwStart = FindPolylinePolylineIntersection(net.srvSouthEdge, net.gardNorthEdge, 0, 12, out Vector2 swStart, out int ssIdx, out int gnIdx);
            net.cbSwOuterPoly = new List<Vector2>();
            int ssStart = Mathf.Max(0, ssIdx + 1);
            int gnStart = Mathf.Max(0, gnIdx + 1);
            Vector2 gnStartPt = net.gardNorthEdge[gnStart];
            for (int i = ssStart; i < net.srvSouthEdge.Count; i++)
            {
                float d = MinDistToPoly(net.srvSouthEdge[i], net.gardNorthEdge, gnStart, net.gardNorthEdge.Count - 1, out int gMatch, out Vector2 q);
                if (d >= 1.55f) { ssStart = i; gnStart = gMatch; gnStartPt = q; break; }
            }
            if (hasSwStart)
                AppendRoundedWedgeNose(net.cbSwOuterPoly, gnStartPt, net.srvSouthEdge[ssStart], swStart, 0.25f);
            for (int i = ssStart; i < net.srvSouthEdge.Count; i++)
            {
                Vector2 p = net.srvSouthEdge[i];
                float fvLimX = EvalCurveXAtZ(net.fvWestEdge, p.y) - 0.10f;
                if (p.x <= fvLimX) net.cbSwOuterPoly.Add(p);
                else { net.cbSwOuterPoly.Add(new Vector2(fvLimX, p.y)); break; }
            }
            float zTopSw = net.cbSwOuterPoly[net.cbSwOuterPoly.Count - 1].y;
            List<Vector2> gnFilt = new List<Vector2> { gnStartPt };
            for (int j = gnStart + 1; j < net.gardNorthEdge.Count; j++)
            {
                Vector2 p = net.gardNorthEdge[j];
                float fvLimX = EvalCurveXAtZ(net.fvWestEdge, p.y) - 0.10f;
                if (p.x <= fvLimX) gnFilt.Add(p);
                else { gnFilt.Add(new Vector2(fvLimX, p.y)); break; }
            }
            float zBotSw = gnFilt[gnFilt.Count - 1].y;
            for (int k = 0; k < net.fvWestEdge.Count; k++)
            {
                if (net.fvWestEdge[k].y >= zBotSw && net.fvWestEdge[k].y <= zTopSw)
                    net.cbSwOuterPoly.Add(new Vector2(net.fvWestEdge[k].x - 0.10f, net.fvWestEdge[k].y));
            }
            for (int j = gnFilt.Count - 1; j >= 0; j--) net.cbSwOuterPoly.Add(gnFilt[j]);

            List<Vector2> srvSouthIn = OffsetSpinePts(net.srvSpine, _ => -2.40f - 1.50f);
            List<Vector2> gardNorthIn = OffsetSpinePts(net.gardSpine, x => EvalGardHalfWidth(x) + 1.50f);
            bool hasSwInStart = FindPolylinePolylineIntersection(srvSouthIn, gardNorthIn, 0, 12, out Vector2 swInStart, out int ssiIdx, out int gniIdx);
            net.cbSwLotPoly = new List<Vector2>();
            int ssiStart = Mathf.Max(0, ssiIdx + 1);
            int gniStart = Mathf.Max(0, gniIdx + 1);
            Vector2 gniStartPt = gardNorthIn[gniStart];
            for (int i = ssiStart; i < srvSouthIn.Count; i++)
            {
                float d = MinDistToPoly(srvSouthIn[i], gardNorthIn, gniStart, gardNorthIn.Count - 1, out int gMatch, out Vector2 q);
                if (d >= 0.60f) { ssiStart = i; gniStart = gMatch; gniStartPt = q; break; }
            }
            if (hasSwInStart)
                AppendRoundedWedgeNose(net.cbSwLotPoly, gniStartPt, srvSouthIn[ssiStart], swInStart, 0.20f);
            for (int i = ssiStart; i < srvSouthIn.Count; i++)
            {
                Vector2 p = srvSouthIn[i];
                if (p.x <= EvalCurveXAtZ(net.fvWestEdge, p.y) - 1.70f) net.cbSwLotPoly.Add(p);
            }
            if (net.cbSwLotPoly.Count > 1)
            {
                float zTopIn = net.cbSwLotPoly[net.cbSwLotPoly.Count - 1].y;
                List<Vector2> gnInFilt = new List<Vector2> { gniStartPt };
                for (int j = gniStart + 1; j < gardNorthIn.Count; j++)
                {
                    Vector2 p = gardNorthIn[j];
                    if (p.x <= EvalCurveXAtZ(net.fvWestEdge, p.y) - 1.70f) gnInFilt.Add(p);
                }
                if (gnInFilt.Count > 0)
                {
                    float zBotIn = gnInFilt[gnInFilt.Count - 1].y;
                    for (int k = 0; k < net.fvWestEdge.Count; k++)
                    {
                        if (net.fvWestEdge[k].y >= zBotIn && net.fvWestEdge[k].y <= zTopIn)
                            net.cbSwLotPoly.Add(new Vector2(net.fvWestEdge[k].x - 1.70f, net.fvWestEdge[k].y));
                    }
                    for (int j = gnInFilt.Count - 1; j >= 0; j--) net.cbSwLotPoly.Add(gnInFilt[j]);
                }
            }

            // 4. Three Curbed Median Islands slanted cleanly between Av. Francisco Villa East & Busway West
            net.medianNorthPoly = BuildClippedMedianPoly(net.fvEastEdge, net.busWestEdge, 71.50f, 71.50f, -2.40f, -10.20f);
            net.medianMidPoly   = BuildClippedMedianPoly(net.fvEastEdge, net.busWestEdge, -20.50f, -22.20f, -24.10f, -25.20f);
            net.medianSouthPoly = BuildClippedMedianPoly(net.fvEastEdge, net.busWestEdge, -38.65f, -37.80f, -64.00f, -64.00f);
            return net;
        }

        private static float PolySignedAreaXZ(List<Vector2> pts)
        {
            float sum = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                sum += a.x * b.y - b.x * a.y;
            }
            return 0.5f * sum;
        }

        private static void BuildCurbedUrbanBlock(string prefix, Transform parent, List<Vector2> outerPoly, List<Vector2> lotPoly,
            Material concreteMat, Material curbMat, Material urbanLotMat, bool addLotPad = true, float curbWidth = 0.22f)
        {
            if (outerPoly == null || outerPoly.Count < 3) return;
            CreateFlatPrism($"{prefix}_SidewalkConcrete", parent, outerPoly, -0.05f, SidewalkTopY - 0.004f, concreteMat, addCollider: true);

            if (addLotPad && lotPoly != null && lotPoly.Count >= 3 && Mathf.Abs(PolySignedAreaXZ(lotPoly)) > 4.0f)
                CreateFlatPrism($"{prefix}_DelimitedLotPad", parent, lotPoly, SidewalkTopY - 0.01f, SidewalkTopY + 0.002f, urbanLotMat, addCollider: true);

            // Inward sign determined strictly from signed area so perimeter curb NEVER protrudes onto asphalt!
            float sArea = PolySignedAreaXZ(outerPoly);
            float leftSign = sArea >= 0f ? 1f : -1f;
            int n = outerPoly.Count;
            List<Vector2> cInner = new List<Vector2>(n + 1);
            for (int i = 0; i < n; i++)
            {
                Vector2 pPrev = outerPoly[(i - 1 + n) % n];
                Vector2 pCur  = outerPoly[i];
                Vector2 pNext = outerPoly[(i + 1) % n];
                Vector2 d0 = (pCur - pPrev);
                Vector2 d1 = (pNext - pCur);
                if (d0.sqrMagnitude < 1e-6f) d0 = d1;
                if (d1.sqrMagnitude < 1e-6f) d1 = d0;
                d0.Normalize(); d1.Normalize();
                Vector2 in0 = new Vector2(-d0.y, d0.x) * leftSign;
                Vector2 in1 = new Vector2(-d1.y, d1.x) * leftSign;
                Vector2 avg = (in0 + in1);
                if (avg.sqrMagnitude < 1e-4f) avg = in1;
                avg.Normalize();
                float dot = Mathf.Max(0.45f, Vector2.Dot(avg, in1));
                float off = Mathf.Min(curbWidth / dot, curbWidth * 2.1f);
                cInner.Add(pCur + avg * off);
            }
            List<Vector2> ringA = new List<Vector2>(outerPoly); ringA.Add(outerPoly[0]);
            List<Vector2> ringB = new List<Vector2>(cInner);    ringB.Add(cInner[0]);
            BuildQuadStripPrism($"{prefix}_PerimeterCurb", parent, ringA, ringB, -0.05f, SidewalkTopY + 0.005f, curbMat, addCollider: true);
        }

        private static void BuildCurbAndSidewalkChain(string prefix, Transform parent, List<Vector2> streetEdgePts, bool outwardIsLeft,
            float swWidth, Material curbMat, Material concreteMat, float curbWidth = 0.22f)
        {
            if (streetEdgePts == null || streetEdgePts.Count < 2) return;
            float sign = outwardIsLeft ? 1f : -1f;
            int n = streetEdgePts.Count;
            List<Vector2> cOut = new List<Vector2>(n), swOut = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = i > 0 ? (streetEdgePts[i] - streetEdgePts[i - 1]).normalized : (streetEdgePts[1] - streetEdgePts[0]).normalized;
                Vector2 dNext = i < n - 1 ? (streetEdgePts[i + 1] - streetEdgePts[i]).normalized : dPrev;
                Vector2 n0 = new Vector2(-dPrev.y, dPrev.x) * sign;
                Vector2 n1 = new Vector2(-dNext.y, dNext.x) * sign;
                Vector2 avg = (n0 + n1);
                avg = avg.sqrMagnitude < 1e-4f ? n1 : avg.normalized;
                float miter = Mathf.Clamp(1f / Mathf.Max(0.45f, Vector2.Dot(avg, n1)), 1f, 2.1f);
                cOut.Add(streetEdgePts[i] + avg * (curbWidth * miter));
                swOut.Add(streetEdgePts[i] + avg * ((curbWidth + swWidth) * miter));
            }
            BuildQuadStripPrism($"{prefix}_Curb", parent, streetEdgePts, cOut, -0.05f, SidewalkTopY + 0.005f, curbMat, addCollider: true);
            if (swWidth > 0.05f)
                BuildQuadStripPrism($"{prefix}_Sidewalk", parent, cOut, swOut, -0.05f, SidewalkTopY - 0.004f, concreteMat, addCollider: true);
        }

        private static void EmitSpineRibbonMark(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            List<SpinePt2D> spine, int iStart, int iEnd, float latOff, float halfW, float liftY)
        {
            for (int i = Mathf.Max(0, iStart); i < Mathf.Min(spine.Count - 1, iEnd); i++)
            {
                SpinePt2D a = spine[i], b = spine[i + 1];
                AddMarkQuadXZ(verts, tris, uvs,
                    a.pos + a.leftNorm * (latOff + halfW), a.pos + a.leftNorm * (latOff - halfW),
                    b.pos + b.leftNorm * (latOff - halfW), b.pos + b.leftNorm * (latOff + halfW), liftY);
            }
        }

        private static void EmitCrosswalkBars(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector2 center, Vector2 forwardDir, float spanHalfWidth, float barLen, float liftY)
        {
            Vector2 d = forwardDir.normalized;
            Vector2 l = new Vector2(-d.y, d.x);
            for (float u = -spanHalfWidth + 0.45f; u <= spanHalfWidth - 0.45f; u += 0.90f)
            {
                Vector2 c = center + l * u;
                AddMarkQuadXZ(verts, tris, uvs,
                    c - d * (barLen * 0.5f) - l * 0.22f,
                    c - d * (barLen * 0.5f) + l * 0.22f,
                    c + d * (barLen * 0.5f) + l * 0.22f,
                    c + d * (barLen * 0.5f) - l * 0.22f, liftY);
            }
        }

        private static void BuildEastAbasoloGardeniasJunction(Material asphaltMat, Material concreteMat, Material curbMat,
            Material whiteStripeMat, Material yellowStripeMat, Material urbanLotMat)
        {
            GameObject plazaParent = GameObject.Find("Border_Crossing_Plaza");
            Transform parentTransform = plazaParent != null ? plazaParent.transform : null;

            HashSet<string> oldEastRoots = new HashSet<string>
            {
                "East_Abasolo_Gardenias_Junction_Root",
                "East_CentralEnclosedBlock_Root",
                "East_Medians_And_OuterSidewalks_Root",
                "East_OuterSidewalks_Root",
                "East_Gardenias_RefugeIsland_Concrete",
                "East_Gardenias_RefugeIsland_YellowCurb"
            };
            Transform[] sceneTransformsForEast = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in sceneTransformsForEast)
            {
                if (tr != null && tr.gameObject.scene.IsValid() && oldEastRoots.Contains(tr.name))
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
            }

            GameObject root = new GameObject("East_Abasolo_Gardenias_Junction_Root");
            if (parentTransform != null) root.transform.SetParent(parentTransform);
            root.transform.position = Vector3.zero;

            // 1. Disable legacy West-only East Gardenias stubs (keep Crosswalk_Gardenias_East, Corner ramps & Curiosidades)
            foreach (string stubName in new[]
            {
                "Roadway_Gardenias_East",
                "Sidewalk_Gardenias_East_South_Extension",
                "Curb_Gardenias_East_South",
                "Curb_Gardenias_East_North",
                "Sidewalk_Gardenias_East_North"
            })
            {
                GameObject stub = GameObject.Find(stubName);
                if (stub != null) stub.SetActive(false);
            }

            GameObject marksEast = GameObject.Find("Markings_Gardenias_East");
            if (marksEast != null)
            {
                foreach (Transform child in marksEast.transform)
                {
                    if (child.name == "LaneDivider_Dash_5" || child.name == "LaneDivider_Dash_9" || child.name == "Parking_Stall_Line_3")
                        child.gameObject.SetActive(false);
                }
            }

            OsmNetworkData net = GetOsmNetwork();

            // 2. Build Primary East N-S Corridors: Av. Francisco Villa (+0.0015f) & Linea Express Busway (+0.0030f)
            BuildQuadStripPrism("Roadway_Calle_FranciscoVilla", root.transform,
                net.fvWestEdge, net.fvEastEdge, RoadBotY, RoadTopY + 0.0015f, asphaltMat, addCollider: true);
            BuildQuadStripPrism("Roadway_Busway_LineaExpress", root.transform,
                net.busWestEdge, net.busEastEdge, RoadBotY, RoadTopY + 0.0030f, asphaltMat, addCollider: true);

            // 3. Herrera Jordan East ([21]+[39]) crossing FV & Busway out to X=155m (-0.0015f under FV & Busway)
            List<Vector2> herLeft = new List<Vector2>(), herRight = new List<Vector2>();
            for (int i = 0; i < net.abaSpine.Count; i++)
            {
                if (net.abaSpine[i].pos.x >= 85.0f)
                {
                    herLeft.Add(net.abaSpine[i].pos + net.abaSpine[i].leftNorm * EvalAbaEastHalfLeft(net.abaSpine[i].pos.x));
                    herRight.Add(net.abaSpine[i].pos + net.abaSpine[i].leftNorm * EvalAbaEastHalfRight(net.abaSpine[i].pos.x));
                }
            }
            BuildQuadStripPrism("Roadway_HerreraJordan_East", root.transform,
                herLeft, herRight, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);

            // 4. Calle Gardenias East + Herrera East ([9]+[32]+[6]): West end 0.000f flush with Plaza, -0.0015f at FV & East
            int gSplit = 0;
            for (int i = 0; i < net.gardSpine.Count; i++) { gSplit = i; if (net.gardSpine[i].pos.x >= 84.0f) break; }
            BuildQuadStripPrism("Roadway_Gardenias_East_Curved", root.transform,
                net.gardNorthEdge.GetRange(0, gSplit + 1), net.gardSouthEdge.GetRange(0, gSplit + 1), RoadBotY, RoadTopY, asphaltMat, addCollider: true);
            BuildQuadStripPrism("Roadway_HerreraSouth_East", root.transform,
                net.gardNorthEdge.GetRange(gSplit, net.gardSpine.Count - gSplit), net.gardSouthEdge.GetRange(gSplit, net.gardSpine.Count - gSplit),
                RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);

            // 5. Interior Service / Linea Express Curved Lane ([22]+[14]) + Service North ([25])
            BuildQuadStripPrism("Roadway_Service_LineaExpress", root.transform,
                net.srvNorthEdge, net.srvSouthEdge, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);
            BuildQuadStripPrism("Roadway_Service_North", root.transform,
                net.srvNorthWestEdge, net.srvNorthEastEdge, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);

            // 6. Central Enclosed Sub-Blocks NE & SW (ZERO buildings - sidewalks, curbs & urban lots only!)
            GameObject cbRoot = new GameObject("East_CentralEnclosedBlock_Root");
            cbRoot.transform.SetParent(root.transform);
            BuildCurbedUrbanBlock("CentralBlock_NE", cbRoot.transform, net.cbNeOuterPoly, net.cbNeLotPoly, concreteMat, curbMat, urbanLotMat, addLotPad: true);
            BuildCurbedUrbanBlock("CentralBlock_SW", cbRoot.transform, net.cbSwOuterPoly, net.cbSwLotPoly, concreteMat, curbMat, urbanLotMat, addLotPad: true);

            // 7. Medians N/M/S + Outer Sidewalks/Curbs (Abasolo North + FV Northbound, ServiceNorth, Busway East, Herrera East)
            GameObject outerRoot = new GameObject("East_Medians_And_OuterSidewalks_Root");
            outerRoot.transform.SetParent(root.transform);
            BuildCurbedUrbanBlock("Median_FV_Busway_North", outerRoot.transform, net.medianNorthPoly, null, concreteMat, curbMat, urbanLotMat, addLotPad: false);
            BuildCurbedUrbanBlock("Median_FV_Busway_Mid",   outerRoot.transform, net.medianMidPoly,   null, concreteMat, curbMat, urbanLotMat, addLotPad: false);
            BuildCurbedUrbanBlock("Median_FV_Busway_South", outerRoot.transform, net.medianSouthPoly, null, concreteMat, curbMat, urbanLotMat, addLotPad: false);

            // Abasolo North Sidewalk West of ServiceNorth [25] & NE FV corner North of Abasolo (rounded onto FV)
            List<Vector2> abaNorthAll = new List<Vector2>();
            for (float s = 55.5f; s <= 140.0f; s += 1.0f)
            {
                AbasoloSample sp = EvaluateAbasolo(s);
                abaNorthAll.Add(new Vector2(sp.center.x, sp.center.z) + new Vector2(sp.northNormalXZ.x, sp.northNormalXZ.z) * AbasoloNorth3LaneOffset);
            }
            bool hasSnW = FindPolylinePolylineIntersection(abaNorthAll, net.srvNorthWestEdge, 0, 0, out Vector2 snWHit, out _, out int snWSeg);
            bool hasSnE = FindPolylinePolylineIntersection(abaNorthAll, net.srvNorthEastEdge, 0, 0, out Vector2 snEHit, out _, out int snESeg);

            float sGapA = 76.5f, sGapB = 85.5f;
            if (FindAbasoloPolylineHitS(net.srvNorthWestEdge, AbasoloNorth3LaneOffset, out float sHitW) &&
                FindAbasoloPolylineHitS(net.srvNorthEastEdge, AbasoloNorth3LaneOffset, out float sHitE))
            {
                sGapA = Mathf.Min(sHitW, sHitE) - 0.55f;
                sGapB = Mathf.Max(sHitW, sHitE) + 0.55f;
            }

            List<Vector2> abaN_West = new List<Vector2>(), abaN_EastToFV = new List<Vector2>();
            float sHitNorth = FindAbasoloFVWestHitS(AbasoloNorth3LaneOffset);
            for (float s = 55.5f; s <= sGapA; s += 1.25f)
                abaN_West.Add(EvalAbasoloOffsetXZ(s, AbasoloNorth3LaneOffset, out _));

            float sStopCorner = Mathf.Max(sGapB + 1.0f, sHitNorth - 2.60f);
            for (float s = sGapB; s <= sStopCorner; s += 1.25f)
                abaN_EastToFV.Add(EvalAbasoloOffsetXZ(s, AbasoloNorth3LaneOffset, out _));
            Vector2 abaEndPt = EvalAbasoloOffsetXZ(sStopCorner, AbasoloNorth3LaneOffset, out Vector2 abaEndTan);
            if (abaN_EastToFV.Count == 0 || Vector2.Distance(abaN_EastToFV[abaN_EastToFV.Count - 1], abaEndPt) > 0.25f)
                abaN_EastToFV.Add(abaEndPt);

            float hitZNorth = EvalAbasoloOffsetXZ(sHitNorth, AbasoloNorth3LaneOffset, out _).y;
            float cornerFvZ = hitZNorth + 3.00f;
            Vector2 fvStartPt = new Vector2(EvalCurveXAtZ(net.fvWestEdge, cornerFvZ), cornerFvZ);
            List<Vector2> cornerArc = SampleBezierChain(abaEndPt, abaEndTan.normalized, fvStartPt, Vector2.up, 1.40f, 8);
            for (int c = 1; c < cornerArc.Count; c++) abaN_EastToFV.Add(cornerArc[c]);

            for (int i = net.fvWestEdge.Count - 1; i >= 0; i--)
            {
                Vector2 fp = net.fvWestEdge[i];
                if (fp.y > cornerFvZ + 0.4f && fp.y <= 115.0f)
                    abaN_EastToFV.Add(fp);
            }
            BuildCurbAndSidewalkChain("EastAbasolo_NorthWest", outerRoot.transform, abaN_West, true, 1.80f, curbMat, concreteMat);
            BuildCurbAndSidewalkChain("EastAbasolo_NorthEastToFV", outerRoot.transform, abaN_EastToFV, true, 1.80f, curbMat, concreteMat);

            // Service North [25] Sidewalks/Curbs flush down to exact intersection with Abasolo North edge (zero sand gap!)
            List<Vector2> snW = new List<Vector2>(), snE = new List<Vector2>();
            if (hasSnW)
            {
                snW.Add(snWHit);
                for (int i = snWSeg + 1; i < net.srvNorthWestEdge.Count; i++) snW.Add(net.srvNorthWestEdge[i]);
            }
            else
            {
                for (int i = 0; i < net.srvNorthSpine.Count; i++) if (net.srvNorthWestEdge[i].y >= 31.5f) snW.Add(net.srvNorthWestEdge[i]);
            }
            if (hasSnE)
            {
                snE.Add(snEHit);
                for (int i = snESeg + 1; i < net.srvNorthEastEdge.Count; i++) snE.Add(net.srvNorthEastEdge[i]);
            }
            else
            {
                for (int i = 0; i < net.srvNorthSpine.Count; i++) if (net.srvNorthEastEdge[i].y >= 24.5f) snE.Add(net.srvNorthEastEdge[i]);
            }
            BuildCurbAndSidewalkChain("ServiceNorth_WestSide", outerRoot.transform, snW, true, 1.40f, curbMat, concreteMat);
            BuildCurbAndSidewalkChain("ServiceNorth_EastSide", outerRoot.transform, snE, false, 1.40f, curbMat, concreteMat);

            // Busway East Curbs & Sidewalks split cleanly across Herrera Jordan (-14.8..-23.7m) and Herrera South (-28.2..-37.3m)
            void AddBusEastBand(string tag, float zHigh, float zLow)
            {
                List<Vector2> pts = new List<Vector2>();
                if (ClipEdgeAtZ(net.busEastEdge, zHigh, out Vector2 topP)) pts.Add(topP);
                for (int i = 0; i < net.busEastEdge.Count; i++) if (net.busEastEdge[i].y < zHigh && net.busEastEdge[i].y > zLow) pts.Add(net.busEastEdge[i]);
                if (ClipEdgeAtZ(net.busEastEdge, zLow, out Vector2 botP)) pts.Add(botP);
                BuildCurbAndSidewalkChain(tag, outerRoot.transform, pts, true, 1.80f, curbMat, concreteMat);
            }
            AddBusEastBand("Busway_East_North", 73.0f, -14.2f);
            AddBusEastBand("Busway_East_Mid",  -24.2f, -27.6f);
            AddBusEastBand("Busway_East_South",-37.8f, -64.0f);

            // Herrera Jordan East & Herrera South East Curbs East of Busway (flush with busEastEdge + 0.15m)
            List<Vector2> h1N = new List<Vector2>(), h1S = new List<Vector2>();
            for (int i = 0; i < herLeft.Count; i++) if (herLeft[i].x >= EvalCurveXAtZ(net.busEastEdge, herLeft[i].y) + 0.15f) h1N.Add(herLeft[i]);
            for (int i = 0; i < herRight.Count; i++) if (herRight[i].x >= EvalCurveXAtZ(net.busEastEdge, herRight[i].y) + 0.15f) h1S.Add(herRight[i]);
            BuildCurbAndSidewalkChain("HerreraNorth_EastNorthSide", outerRoot.transform, h1N, true,  1.60f, curbMat, concreteMat);
            BuildCurbAndSidewalkChain("HerreraNorth_EastSouthSide", outerRoot.transform, h1S, false, 1.60f, curbMat, concreteMat);

            List<Vector2> h2N = new List<Vector2>(), h2S = new List<Vector2>();
            for (int i = 0; i < net.gardNorthEdge.Count; i++) if (net.gardNorthEdge[i].x >= EvalCurveXAtZ(net.busEastEdge, net.gardNorthEdge[i].y) + 0.15f) h2N.Add(net.gardNorthEdge[i]);
            for (int i = 0; i < net.gardSouthEdge.Count; i++) if (net.gardSouthEdge[i].x >= EvalCurveXAtZ(net.busEastEdge, net.gardSouthEdge[i].y) + 0.15f) h2S.Add(net.gardSouthEdge[i]);
            BuildCurbAndSidewalkChain("HerreraSouth_EastNorthSide", outerRoot.transform, h2N, true,  1.60f, curbMat, concreteMat);
            BuildCurbAndSidewalkChain("HerreraSouth_EastSouthSide", outerRoot.transform, h2S, false, 1.60f, curbMat, concreteMat);

            Debug.Log($"[WestTriangleAndAbasoloBuilder] Built OSM-aligned FV, Busway, Medians, Herrera East, Service [22+14], ServiceNorth [25] & CentralBlock NE/SW.");
        }

        private static void BuildQuadStripPrism(string name, Transform parent, List<Vector2> edgeA, List<Vector2> edgeB,
            float yBot, float yTop, Material mat, bool addCollider)
        {
            int n = Mathf.Min(edgeA.Count, edgeB.Count);
            if (n < 2) return;

            // Ensure consistent top (+Y) orientation regardless of whether edgeB is left or right of edgeA
            Vector2 dSeg = edgeA[1] - edgeA[0];
            Vector2 dCross = edgeB[0] - edgeA[0];
            bool swapSides = (dSeg.x * dCross.y - dSeg.y * dCross.x) > 0f;
            List<Vector2> left = swapSides ? edgeB : edgeA;
            List<Vector2> right = swapSides ? edgeA : edgeB;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Duplicate top/bottom cap rings separately from vertical side walls so top surface has pure +Y flat normals
            for (int i = 0; i < n; i++)
            {
                Vector2 lt = left[i];
                Vector2 rt = right[i];
                verts.Add(new Vector3(lt.x, yTop, lt.y)); uvs.Add(new Vector2(lt.x * 0.15f, lt.y * 0.15f)); // 0 TopLeft
                verts.Add(new Vector3(rt.x, yTop, rt.y)); uvs.Add(new Vector2(rt.x * 0.15f, rt.y * 0.15f)); // 1 TopRight
                verts.Add(new Vector3(rt.x, yBot, rt.y)); uvs.Add(new Vector2(rt.x * 0.15f, rt.y * 0.15f)); // 2 BotRight
                verts.Add(new Vector3(lt.x, yBot, lt.y)); uvs.Add(new Vector2(lt.x * 0.15f, lt.y * 0.15f)); // 3 BotLeft
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;
                tris.AddRange(new int[] { a + 0, b + 0, b + 1, a + 0, b + 1, a + 1 }); // Top +Y
                tris.AddRange(new int[] { a + 2, b + 2, b + 3, a + 2, b + 3, a + 3 }); // Bottom -Y
            }

            void AddSideQuad(Vector2 p0, Vector2 p1, bool flip)
            {
                int b = verts.Count;
                Vector2 a = flip ? p1 : p0;
                Vector2 c = flip ? p0 : p1;
                verts.Add(new Vector3(a.x, yTop, a.y)); uvs.Add(new Vector2(0f, 1f));
                verts.Add(new Vector3(c.x, yTop, c.y)); uvs.Add(new Vector2(1f, 1f));
                verts.Add(new Vector3(c.x, yBot, c.y)); uvs.Add(new Vector2(1f, 0f));
                verts.Add(new Vector3(a.x, yBot, a.y)); uvs.Add(new Vector2(0f, 0f));
                tris.AddRange(new int[] { b + 0, b + 1, b + 2, b + 0, b + 2, b + 3 });
            }

            for (int i = 0; i < n - 1; i++)
            {
                AddSideQuad(right[i], right[i + 1], flip: false);
                AddSideQuad(left[i], left[i + 1], flip: true);
            }
            AddSideQuad(left[0], right[0], flip: true);
            AddSideQuad(left[n - 1], right[n - 1], flip: false);

            Mesh m = new Mesh { name = $"{name}_Mesh" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;

            if (addCollider)
            {
                MeshCollider mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = m;
            }
        }

        // =========================================================================
        // SHARED GEOMETRY & PROP HELPERS
        // =========================================================================

        private static void BuildMasonryWallRun(string name, Transform parent, Vector2 startXZ, Vector2 endXZ,
            Material zocaloMat, Material whiteWallMat, Material redStripeMat, Material greenBandMat, float pilasterSpacing, float heightScale = 1.0f)
        {
            Vector2 delta = endXZ - startXZ;
            float len = delta.magnitude;
            if (len < 0.05f) return;

            float rotYaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            Vector2 mid = (startXZ + endXZ) * 0.5f;
            Quaternion rot = Quaternion.Euler(0f, rotYaw, 0f);

            GameObject wallGo = new GameObject(name);
            wallGo.transform.SetParent(parent);
            wallGo.transform.position = Vector3.zero;

            float zocH = 0.96f * heightScale; // Top at Y = 1.16m -> aligns flush with West Wing & Main Wing zocalo!
            float bodyH = 0.96f * heightScale; // Top at Y = 2.28m
            float capH = 0.20f;                // Top at Y = 2.48m

            // Layer 1: Dark grey masonry zocalo (matching Admin Building zocalo)
            CreateRotatedBox($"{name}_Zocalo", wallGo.transform,
                new Vector3(mid.x, SidewalkTopY + zocH * 0.5f, mid.y),
                new Vector3(0.28f, zocH, len + 0.08f), rot, zocaloMat, keepCollider: true);

            // Continuous green + red municipal stripe between zocalo & white upper body (exact Y=1.20 & 1.28!)
            CreateRotatedBox($"{name}_GreenBand", wallGo.transform,
                new Vector3(mid.x, SidewalkTopY + zocH + 0.04f, mid.y),
                new Vector3(0.30f, 0.08f, len + 0.09f), rot, greenBandMat);
            CreateRotatedBox($"{name}_RedBand", wallGo.transform,
                new Vector3(mid.x, SidewalkTopY + zocH + 0.12f, mid.y),
                new Vector3(0.30f, 0.08f, len + 0.09f), rot, redStripeMat);

            // Layer 2: White stucco body
            CreateRotatedBox($"{name}_WhiteBody", wallGo.transform,
                new Vector3(mid.x, SidewalkTopY + zocH + 0.16f + bodyH * 0.5f, mid.y),
                new Vector3(0.24f, bodyH, len + 0.04f), rot, whiteWallMat, keepCollider: true);

            // Layer 3: Bold red coping top stripe (matches aerial drone photo silhouette!)
            float topY = SidewalkTopY + zocH + 0.16f + bodyH;
            CreateRotatedBox($"{name}_RedCap", wallGo.transform,
                new Vector3(mid.x, topY + capH * 0.5f, mid.y),
                new Vector3(0.30f, capH, len + 0.10f), rot, redStripeMat, keepCollider: true);

            // Structural pilasters along long runs
            int pilasterCount = Mathf.FloorToInt(len / pilasterSpacing);
            for (int p = 0; p <= pilasterCount; p++)
            {
                float t = (pilasterCount > 0) ? (float)p / pilasterCount : 0.5f;
                Vector2 pos = Vector2.Lerp(startXZ, endXZ, t);
                CreateRotatedBox($"{name}_Pilaster_Zoc_{p}", wallGo.transform,
                    new Vector3(pos.x, SidewalkTopY + zocH * 0.5f, pos.y),
                    new Vector3(0.38f, zocH, 0.38f), rot, zocaloMat);
                CreateRotatedBox($"{name}_Pilaster_Green_{p}", wallGo.transform,
                    new Vector3(pos.x, SidewalkTopY + zocH + 0.04f, pos.y),
                    new Vector3(0.40f, 0.08f, 0.40f), rot, greenBandMat);
                CreateRotatedBox($"{name}_Pilaster_Red_{p}", wallGo.transform,
                    new Vector3(pos.x, SidewalkTopY + zocH + 0.12f, pos.y),
                    new Vector3(0.40f, 0.08f, 0.40f), rot, redStripeMat);
                CreateRotatedBox($"{name}_Pilaster_Body_{p}", wallGo.transform,
                    new Vector3(pos.x, SidewalkTopY + zocH + 0.16f + bodyH * 0.5f, pos.y),
                    new Vector3(0.34f, bodyH, 0.34f), rot, whiteWallMat);
                CreateRotatedBox($"{name}_Pilaster_Cap_{p}", wallGo.transform,
                    new Vector3(pos.x, topY + capH * 0.5f + 0.04f, pos.y),
                    new Vector3(0.40f, capH + 0.04f, 0.40f), rot, redStripeMat);
            }
        }

        private static void BuildMetalGateBay(string name, Transform parent, Vector2 startXZ, Vector2 endXZ,
            Material zocaloMat, Material whiteWallMat, Material redStripeMat, Material greenBandMat, Material metalGateMat)
        {
            Vector2 delta = endXZ - startXZ;
            float len = delta.magnitude;
            if (len < 0.2f) return;

            float rotYaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            Vector2 mid = (startXZ + endXZ) * 0.5f;
            Quaternion rot = Quaternion.Euler(0f, rotYaw, 0f);

            GameObject gateGo = new GameObject(name);
            gateGo.transform.SetParent(parent);

            // Recessed double metal gate leaves (grey steel, 2.28m tall, flush behind pilasters)
            CreateRotatedBox($"{name}_Leaves", gateGo.transform,
                new Vector3(mid.x, SidewalkTopY + 1.14f, mid.y),
                new Vector3(0.14f, 2.28f, Mathf.Max(0.5f, len - 0.50f)), rot, metalGateMat, keepCollider: true);

            // Vertical center seam & gate framing posts
            CreateRotatedBox($"{name}_CenterBar", gateGo.transform,
                new Vector3(mid.x, SidewalkTopY + 1.14f, mid.y),
                new Vector3(0.18f, 2.28f, 0.08f), rot, zocaloMat);

            Vector2[] postPts = { startXZ, endXZ };
            for (int p = 0; p < 2; p++)
            {
                CreateRotatedBox($"{name}_Post_Zoc_{p}", gateGo.transform,
                    new Vector3(postPts[p].x, SidewalkTopY + 0.48f, postPts[p].y),
                    new Vector3(0.40f, 0.96f, 0.40f), rot, zocaloMat);
                CreateRotatedBox($"{name}_Post_Green_{p}", gateGo.transform,
                    new Vector3(postPts[p].x, SidewalkTopY + 1.00f, postPts[p].y),
                    new Vector3(0.42f, 0.08f, 0.42f), rot, greenBandMat);
                CreateRotatedBox($"{name}_Post_Red_{p}", gateGo.transform,
                    new Vector3(postPts[p].x, SidewalkTopY + 1.08f, postPts[p].y),
                    new Vector3(0.42f, 0.08f, 0.42f), rot, redStripeMat);
                CreateRotatedBox($"{name}_Post_Body_{p}", gateGo.transform,
                    new Vector3(postPts[p].x, SidewalkTopY + 1.12f + 0.48f, postPts[p].y),
                    new Vector3(0.36f, 0.96f, 0.36f), rot, whiteWallMat);
                CreateRotatedBox($"{name}_Post_Cap_{p}", gateGo.transform,
                    new Vector3(postPts[p].x, SidewalkTopY + 2.22f, postPts[p].y),
                    new Vector3(0.42f, 0.22f, 0.42f), rot, redStripeMat);
            }
        }

        private static void ConstructSimpleRail(string name, Transform parent, Vector3 start, Vector3 end, Material steelMat, int intermediatePosts = 0)
        {
            Vector3 diff = end - start;
            float len = diff.magnitude;
            if (len < 0.05f) return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);

            for (int r = 0; r < 2; r++)
            {
                float hOff = (r == 0) ? 0.52f : 0.96f;
                Vector3 a = start + Vector3.up * hOff;
                Vector3 b = end + Vector3.up * hOff;
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = $"{name}_Bar_{r}";
                bar.transform.SetParent(go.transform);
                bar.transform.position = (a + b) * 0.5f;
                bar.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
                bar.transform.localScale = new Vector3(0.05f, (b - a).magnitude * 0.5f, 0.05f);
                SetMaterial(bar, steelMat);
                UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
            int totalPosts = 2 + Mathf.Max(0, intermediatePosts);
            for (int p = 0; p < totalPosts; p++)
            {
                float t = (float)p / (totalPosts - 1);
                Vector3 basePt = Vector3.Lerp(start, end, t);
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"{name}_Post_{p}";
                post.transform.SetParent(go.transform);
                post.transform.position = basePt + Vector3.up * 0.50f;
                post.transform.localScale = new Vector3(0.055f, 0.50f, 0.055f);
                SetMaterial(post, steelMat);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            }
        }

        private static void CreateBarredWin(string name, Transform parent, Vector3 pos, Quaternion rot,
            float width, float height, int barCount, Material frameMat, Material glassMat, Material barMat)
        {
            GameObject winGo = new GameObject(name);
            winGo.transform.SetParent(parent);
            winGo.transform.position = pos;
            winGo.transform.rotation = rot;

            CreateBox("Frame", winGo.transform, pos, new Vector3(width + 0.14f, height + 0.14f, 0.06f), frameMat);
            GameObject fObj = winGo.transform.Find("Frame")?.gameObject;
            if (fObj != null) fObj.transform.localRotation = Quaternion.identity;

            GameObject glass = CreateBox("Glass", winGo.transform, pos + winGo.transform.forward * -0.025f, new Vector3(width, height, 0.03f), glassMat);
            glass.transform.localRotation = Quaternion.identity;

            for (int b = 0; b < barCount; b++)
            {
                float xOff = Mathf.Lerp(-width * 0.40f, width * 0.40f, (float)b / Mathf.Max(1, barCount - 1));
                GameObject bar = CreateRotatedBox($"Bar_{b}", winGo.transform,
                    pos + winGo.transform.right * xOff + winGo.transform.forward * -0.045f,
                    new Vector3(0.03f, height, 0.03f), rot, barMat);
            }
        }

        private static void CreatePicoCornerMarkers(Transform parent, Vector3 basePos, Material steelMat, Material orangeMat, Material concreteMat)
        {
            GameObject group = new GameObject("Pico_Corner_Markers");
            group.transform.SetParent(parent);
            group.transform.position = basePos;

            // Orange corner bollard (matching photo corner post; Concrete base omitted so it never spawns at world origin)
            GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bollard.name = "Corner_Orange_Bollard";
            bollard.transform.SetParent(group.transform);
            bollard.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            bollard.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
            SetMaterial(bollard, orangeMat);

            // Tall galvanized corner utility pole next to bollard
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Corner_Utility_Pole";
            pole.transform.SetParent(group.transform);
            pole.transform.localPosition = new Vector3(0.95f, 3.25f, -0.35f);
            pole.transform.localScale = new Vector3(0.16f, 3.25f, 0.16f);
            SetMaterial(pole, steelMat);

            CreateRotatedBox("Corner_Pole_Crossarm", group.transform,
                basePos + new Vector3(0.95f, 6.10f, -0.35f),
                new Vector3(1.60f, 0.10f, 0.10f), Quaternion.Euler(0f, DiagYawDeg, 0f), steelMat);
        }

        private static void CreateGardenTree(string name, Transform parent, Vector3 basePos, float scale, Material woodMat, Material foliageMat)
        {
            GameObject treeGo = new GameObject(name);
            treeGo.transform.SetParent(parent);
            treeGo.transform.position = basePos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeGo.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.35f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.32f * scale, 1.35f * scale, 0.32f * scale);
            SetMaterial(trunk, woodMat);

            Vector3[] crownOffsets = new Vector3[]
            {
                new Vector3(0f, 3.05f, 0f),
                new Vector3(0.75f, 2.75f, 0.40f),
                new Vector3(-0.70f, 2.80f, -0.30f),
                new Vector3(0.20f, 3.55f, -0.25f)
            };
            float[] crownRadii = new float[] { 2.55f, 1.85f, 1.75f, 1.65f };
            for (int c = 0; c < crownOffsets.Length; c++)
            {
                GameObject sph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sph.name = $"Canopy_{c}";
                sph.transform.SetParent(treeGo.transform);
                sph.transform.localPosition = crownOffsets[c] * scale;
                float r = crownRadii[c] * scale;
                sph.transform.localScale = new Vector3(r, r * 0.78f, r);
                SetMaterial(sph, foliageMat);
                var col = sph.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
        }

        private static void CreateGardenShrub(string name, Transform parent, Vector3 basePos, float scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = basePos + new Vector3(0f, 0.42f * scale, 0f);
            go.transform.localScale = new Vector3(1.15f * scale, 0.75f * scale, 1.05f * scale);
            SetMaterial(go, mat);
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        private static void CreateCurbSegment(string name, Transform parent, Vector2 a, Vector2 b, float topY, Material curbMat,
            float thickness, Vector2 inwardNormal)
        {
            Vector2 delta = b - a;
            float len = delta.magnitude;
            if (len < 0.05f) return;

            Vector2 mid = (a + b) * 0.5f + inwardNormal.normalized * (thickness * 0.5f);
            float yaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            float height = topY + 0.02f; // extends 2cm below asphalt (Y=0) so zero gap
            CreateRotatedBox(name, parent,
                new Vector3(mid.x, topY - height * 0.5f, mid.y),
                new Vector3(thickness, height, len + 0.04f),
                Quaternion.Euler(0f, yaw, 0f), curbMat, keepCollider: true);
        }

        private static List<int> TriangulatePolygonCW(List<Vector2> pts)
        {
            int n = pts.Count;
            List<int> result = new List<int>((n - 2) * 3);
            if (n < 3) return result;
            if (n == 3) { result.AddRange(new[] { 0, 1, 2 }); return result; }

            List<int> avail = new List<int>(n);
            for (int i = 0; i < n; i++) avail.Add(i);

            float Cross2D(Vector2 a, Vector2 b, Vector2 c) =>
                (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

            bool PointInTriCW(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                return Cross2D(a, b, p) <= 1e-6f && Cross2D(b, c, p) <= 1e-6f && Cross2D(c, a, p) <= 1e-6f;
            }

            int guard = 0;
            int idx = 0;
            while (avail.Count > 3 && guard < n * n + 16)
            {
                guard++;
                int m = avail.Count;
                int iPrev = avail[(idx + m - 1) % m];
                int iCurr = avail[idx % m];
                int iNext = avail[(idx + 1) % m];
                Vector2 a = pts[iPrev], b = pts[iCurr], c = pts[iNext];

                // Convex CW corner check (negative cross2D)
                if (Cross2D(a, b, c) < -1e-6f)
                {
                    bool containsAny = false;
                    for (int k = 0; k < m; k++)
                    {
                        int vi = avail[k];
                        if (vi == iPrev || vi == iCurr || vi == iNext) continue;
                        if (PointInTriCW(pts[vi], a, b, c))
                        {
                            containsAny = true;
                            break;
                        }
                    }
                    if (!containsAny)
                    {
                        result.Add(iPrev);
                        result.Add(iCurr);
                        result.Add(iNext);
                        avail.RemoveAt(idx % m);
                        if (avail.Count > 0) idx %= avail.Count;
                        continue;
                    }
                }
                idx = (idx + 1) % avail.Count;
            }

            if (avail.Count == 3)
            {
                result.Add(avail[0]);
                result.Add(avail[1]);
                result.Add(avail[2]);
            }
            else if (result.Count == 0)
            {
                // Fallback fan only if input was degenerate/self-intersecting
                for (int i = 1; i < n - 1; i++) { result.Add(0); result.Add(i); result.Add(i + 1); }
            }
            return result;
        }

        private static void CreateFlatPrism(string name, Transform parent, List<Vector2> polyCWorCCW,
            float yBot, float yTop, Material mat, bool addCollider)
        {
            if (polyCWorCCW == null || polyCWorCCW.Count < 3) return;

            // Remove consecutive duplicate / zero-length vertices before winding/triangulation
            List<Vector2> pts = new List<Vector2>(polyCWorCCW.Count);
            for (int i = 0; i < polyCWorCCW.Count; i++)
            {
                Vector2 p = polyCWorCCW[i];
                if (pts.Count == 0 || (p - pts[pts.Count - 1]).sqrMagnitude > 1e-6f)
                    pts.Add(p);
            }
            if (pts.Count >= 3 && (pts[pts.Count - 1] - pts[0]).sqrMagnitude <= 1e-6f)
                pts.RemoveAt(pts.Count - 1);

            int n = pts.Count;
            if (n < 3) return;

            // Ensure CW winding in XZ (+X right, +Z forward) for outward +Y normal
            float signedArea2 = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = pts[i];
                Vector2 p1 = pts[(i + 1) % n];
                signedArea2 += (p0.x * p1.y - p1.x * p0.y);
            }
            if (signedArea2 > 0f) pts.Reverse(); // convert CCW to CW in XZ so CW faces +Y

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Top vertices: 0..n-1, Bottom vertices: n..2n-1
            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(pts[i].x, yTop, pts[i].y));
                uvs.Add(new Vector2(pts[i].x * 0.25f, pts[i].y * 0.25f));
            }
            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(pts[i].x, yBot, pts[i].y));
                uvs.Add(new Vector2(pts[i].x * 0.25f, pts[i].y * 0.25f));
            }

            // Ear-clipped top (+Y) & bottom (-Y) caps supporting arbitrary concave polygons
            List<int> capTris = TriangulatePolygonCW(pts);
            for (int t = 0; t + 2 < capTris.Count; t += 3)
            {
                int a = capTris[t], b = capTris[t + 1], c = capTris[t + 2];
                tris.AddRange(new int[] { a, b, c });
                tris.AddRange(new int[] { n + a, n + c, n + b });
            }

            // Vertical side faces with duplicated vertices for crisp hard normals
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                int b = verts.Count;
                verts.Add(new Vector3(pts[i].x, yTop, pts[i].y));
                verts.Add(new Vector3(pts[next].x, yTop, pts[next].y));
                verts.Add(new Vector3(pts[next].x, yBot, pts[next].y));
                verts.Add(new Vector3(pts[i].x, yBot, pts[i].y));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 0f));
                tris.AddRange(new int[] { b + 0, b + 2, b + 1, b + 0, b + 3, b + 2 });
            }

            Mesh m = new Mesh { name = $"{name}_Mesh" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;

            if (addCollider)
            {
                MeshCollider mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = m;
            }
        }

        private static Bounds GetHierarchyBounds(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null) return new Bounds();
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        private static void UpdateLineObj(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(true);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = scale;
                SetMaterial(go, mat);
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false)
        {
            return CreateRotatedBox(name, parent, pos, scale, Quaternion.identity, mat, keepCollider);
        }

        private static GameObject CreateRotatedBox(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            SetMaterial(go, mat);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static Material LoadMat(string path, Color fallback)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            return CreateOrGetMat(path, fallback, 0.2f);
        }

        private static Material CloneMatWithTint(Material template, string path, Color tint)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(template);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = template.shader;
                m.CopyPropertiesFromMaterial(template);
            }
            m.SetColor("_BaseColor", tint);
            m.SetColor("_Color", tint);
            m.SetFloat("_Smoothness", 0.0f);
            m.SetFloat("_SpecularHighlights", 0.0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material CreateOrGetMat(string path, Color color, float smoothness)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = s;
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SpecularHighlights", 0.0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        // =========================================================================
        // STEP 4B: CALLE AZUCENAS (EAST), OSM CORRIDOR MARKINGS & IRREGULAR MANZANA
        // =========================================================================

        private static void AddMarkQuadXZ(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float y)
        {
            // Ensure +Y upward face normal in Unity left-handed coords
            float dz1 = p1.y - p0.y;
            float dx1 = p1.x - p0.x;
            float dz2 = p2.y - p0.y;
            float dx2 = p2.x - p0.x;
            if (dz1 * dx2 - dx1 * dz2 < 0f)
            {
                Vector2 tmp = p1;
                p1 = p3;
                p3 = tmp;
            }
            int b = verts.Count;
            verts.Add(new Vector3(p0.x, y, p0.y));
            verts.Add(new Vector3(p1.x, y, p1.y));
            verts.Add(new Vector3(p2.x, y, p2.y));
            verts.Add(new Vector3(p3.x, y, p3.y));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        private static GameObject CreateMeshChild(string name, Transform parent, List<Vector3> verts, List<int> tris, List<Vector2> uvs, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            Mesh m = new Mesh { name = name + "_Mesh" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.SetUVs(0, uvs);
            m.RecalculateNormals();
            m.RecalculateBounds();
            mf.sharedMesh = m;
            return go;
        }

        private static void ExtendAvenidaJuarezAndBaseGround(Transform envRoot, Material asphaltMat, Material concreteMat,
            Material curbMat, Material whiteStripeMat, float azNorthZAtJuarez, float azSouthZAtJuarez)
        {
            Material sandMat = curbMat;
            GameObject terrainSkirt = GameObject.Find("Terrain_Skirt_South");
            if (terrainSkirt != null && terrainSkirt.TryGetComponent<Renderer>(out Renderer tr) && tr.sharedMaterial != null)
                sandMat = tr.sharedMaterial;

            // Flush sandy ground west of X = 24.05m, south of Z = -24.90m (topY = -0.085f strictly below all asphalt)
            CreateBox("Azucenas_Corridor_BaseGround_Ext", envRoot,
                new Vector3(-47.975f, -0.225f, -82.45f), new Vector3(144.05f, 0.28f, 115.10f), sandMat, keepCollider: false);

            // Extend Road_AvenidaJuarez south to Z = -60.00m (keeps north trim at +51.40m under Bridge)
            GameObject roadJuarez = GameObject.Find("Road_AvenidaJuarez");
            if (roadJuarez != null)
            {
                roadJuarez.transform.position = new Vector3(0f, -0.05f, -4.30f);
                roadJuarez.transform.localScale = new Vector3(14.0f, 0.10f, 111.40f);
            }

            // Extend Sidewalk_West & Curb_West continuously down to Z = -60.00m
            GameObject swWest = GameObject.Find("Sidewalk_West");
            if (swWest != null)
            {
                swWest.transform.position = new Vector3(-7.40f, 0.10f, -19.90f);
                swWest.transform.localScale = new Vector3(9.20f, 0.20f, 80.20f);
            }
            GameObject curbWest = GameObject.Find("Curb_West");
            if (curbWest != null)
            {
                curbWest.transform.position = new Vector3(-2.70f, 0.10f, -19.90f);
                curbWest.transform.localScale = new Vector3(0.20f, 0.20f, 80.20f);
            }

            // Adjust Sidewalk_East & Curb_East to terminate cleanly at azNorthZAtJuarez + 3.00m (start of Azucenas NW R=3m corner fillet)
            float swEastCutZ = azNorthZAtJuarez + 3.00f;
            float swEastLen = 22.00f - swEastCutZ;
            GameObject swEast = GameObject.Find("Sidewalk_East");
            if (swEast != null)
            {
                swEast.transform.position = new Vector3(9.50f, 0.10f, 0.5f * (22.00f + swEastCutZ));
                swEast.transform.localScale = new Vector3(5.00f, 0.20f, swEastLen);
            }
            float curbEastLen = 24.00f - swEastCutZ;
            GameObject curbEast = GameObject.Find("Curb_East");
            if (curbEast != null)
            {
                curbEast.transform.position = new Vector3(6.90f, 0.10f, 0.5f * (24.00f + swEastCutZ));
                curbEast.transform.localScale = new Vector3(0.20f, 0.20f, curbEastLen);
            }

            // Continue Av. Juarez East Sidewalk & Curb South of Calle Azucenas (Z = -60.00..swSouthCutZ)
            float swSouthCutZ = azSouthZAtJuarez - 3.50f;
            float southSegLen = swSouthCutZ - (-60.00f);
            float southCenterZ = 0.5f * (-60.00f + swSouthCutZ);
            CreateBox("Sidewalk_East_SouthOfAzucenas", envRoot,
                new Vector3(9.50f, 0.10f, southCenterZ), new Vector3(5.00f, 0.20f, southSegLen), concreteMat, keepCollider: true);
            CreateBox("Curb_East_SouthOfAzucenas", envRoot,
                new Vector3(6.90f, 0.10f, southCenterZ), new Vector3(0.20f, 0.20f, southSegLen), curbMat, keepCollider: false);

            // Lane markings on Av. Juarez south of Azucenas
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            AddMarkQuadXZ(verts, tris, uvs, new Vector2(-2.32f, -59.5f), new Vector2(-2.32f, swSouthCutZ + 1.5f), new Vector2(-2.24f, swSouthCutZ + 1.5f), new Vector2(-2.24f, -59.5f), MarkingsY);
            AddMarkQuadXZ(verts, tris, uvs, new Vector2(6.44f, -59.5f), new Vector2(6.44f, swSouthCutZ), new Vector2(6.52f, swSouthCutZ), new Vector2(6.52f, -59.5f), MarkingsY);
            for (float z = -58.5f; z < swSouthCutZ + 1.0f; z += 5.5f)
            {
                float zEnd = Mathf.Min(z + 2.6f, swSouthCutZ + 1.2f);
                if (zEnd - z < 0.8f) continue;
                AddMarkQuadXZ(verts, tris, uvs, new Vector2(2.04f, z), new Vector2(2.04f, zEnd), new Vector2(2.16f, zEnd), new Vector2(2.16f, z), MarkingsY);
            }
            CreateMeshChild("Road_AvenidaJuarez_SouthMarkings", envRoot, verts, tris, uvs, whiteStripeMat);
        }

        private static void BuildEastOsmMarkings(Transform root, OsmNetworkData net, Material whiteStripeMat, Material yellowStripeMat)
        {
            const float yMark = 0.015f;
            List<Vector3> wVerts = new List<Vector3>(); List<int> wTris = new List<int>(); List<Vector2> wUvs = new List<Vector2>();
            List<Vector3> yVerts = new List<Vector3>(); List<int> yTris = new List<int>(); List<Vector2> yUvs = new List<Vector2>();

            void EmitDashedCenter(List<SpinePt2D> sp, float dashLen, float gapLen, Func<Vector2, bool> skipFn, List<Vector3> v, List<int> t, List<Vector2> u, float halfW = 0.07f)
            {
                float period = dashLen + gapLen;
                for (int i = 0; i < sp.Count - 1; i++)
                {
                    Vector2 mid = 0.5f * (sp[i].pos + sp[i + 1].pos);
                    if (skipFn != null && skipFn(mid)) continue;
                    if ((sp[i].sAlong % period) < dashLen)
                    {
                        AddMarkQuadXZ(v, t, u,
                            sp[i].pos + sp[i].leftNorm * halfW, sp[i].pos - sp[i].leftNorm * halfW,
                            sp[i + 1].pos - sp[i + 1].leftNorm * halfW, sp[i + 1].pos + sp[i + 1].leftNorm * halfW, yMark);
                    }
                }
            }

            void EmitEdgeStripes(List<SpinePt2D> sp, Func<float, float> hwFn, Func<Vector2, bool> skipFn, List<Vector3> v, List<int> t, List<Vector2> u)
            {
                for (int i = 0; i < sp.Count - 1; i++)
                {
                    Vector2 mid = 0.5f * (sp[i].pos + sp[i + 1].pos);
                    if (skipFn != null && skipFn(mid)) continue;
                    float hw0 = hwFn(sp[i].pos.x) - 0.32f, hw1 = hwFn(sp[i + 1].pos.x) - 0.32f;
                    AddMarkQuadXZ(v, t, u, sp[i].pos + sp[i].leftNorm * (hw0 + 0.07f), sp[i].pos + sp[i].leftNorm * (hw0 - 0.07f),
                        sp[i + 1].pos + sp[i + 1].leftNorm * (hw1 - 0.07f), sp[i + 1].pos + sp[i + 1].leftNorm * (hw1 + 0.07f), yMark);
                    AddMarkQuadXZ(v, t, u, sp[i].pos - sp[i].leftNorm * (hw0 - 0.07f), sp[i].pos - sp[i].leftNorm * (hw0 + 0.07f),
                        sp[i + 1].pos - sp[i + 1].leftNorm * (hw1 + 0.07f), sp[i + 1].pos - sp[i + 1].leftNorm * (hw1 - 0.07f), yMark);
                }
            }

            // 1. Av. Francisco Villa: double/solid yellow centerline + white outer edges (open across Abasolo / Gardenias / Azucenas triple intersection)
            bool FvInCross(Vector2 p) => p.y > -39.0f && p.y < 10.6f;
            for (int i = 0; i < net.fvSpine.Count - 1; i++)
            {
                Vector2 mid = 0.5f * (net.fvSpine[i].pos + net.fvSpine[i + 1].pos);
                if (FvInCross(mid)) continue;
                EmitSpineRibbonMark(yVerts, yTris, yUvs, net.fvSpine, i, i + 1, -0.14f, 0.06f, yMark);
                EmitSpineRibbonMark(yVerts, yTris, yUvs, net.fvSpine, i, i + 1,  0.14f, 0.06f, yMark);
            }
            EmitEdgeStripes(net.fvSpine, _ => FVHalfWidth, FvInCross, wVerts, wTris, wUvs);

            // Crosswalk zebra bars across Av. Francisco Villa (strictly outside diagonal intersections)
            EmitCrosswalkBars(wVerts, wTris, wUvs, new Vector2(92.59f, 11.8f),  Vector2.up, 4.4f, 2.2f, yMark);
            EmitCrosswalkBars(wVerts, wTris, wUvs, new Vector2(93.85f, -21.4f), Vector2.up, 4.4f, 2.2f, yMark);
            EmitCrosswalkBars(wVerts, wTris, wUvs, new Vector2(94.21f, -40.2f), Vector2.up, 4.4f, 2.2f, yMark);

            // 2. Busway Linea Express: continuous yellow/white transit guide edges + dashed white center
            bool BusInCross(Vector2 p) => p.y > -37.6f && p.y < -10.8f;
            EmitEdgeStripes(net.busSpine, _ => BuswayHalfWidth, BusInCross, yVerts, yTris, yUvs);
            EmitDashedCenter(net.busSpine, 2.8f, 3.6f, BusInCross, wVerts, wTris, wUvs, 0.07f);

            // 3. Calle Gardenias East + Herrera South ([9]+[32]+[6]): West end past plaza (X > 25m), open across FV & Busway
            bool InFvOrBuswayX(Vector2 p) => (p.x > 88.4f && p.x < 98.4f) || (p.x > 101.6f && p.x < 108.8f);
            EmitDashedCenter(net.gardSpine, 2.6f, 3.8f, p => p.x < 25.0f || InFvOrBuswayX(p), wVerts, wTris, wUvs);
            EmitEdgeStripes(net.gardSpine, EvalGardHalfWidth, p => p.x < 25.0f || InFvOrBuswayX(p), wVerts, wTris, wUvs);

            // Westbound arrows on Gardenias East + Eastbound on Herrera
            float[] gardArrowXs = new float[] { 34f, 52f, 68f, 122f, 142f };
            foreach (float ax in gardArrowXs)
            {
                SpinePt2D? best = null; float bd = 99f;
                for (int i = 0; i < net.gardSpine.Count; i++)
                {
                    float d = Mathf.Abs(net.gardSpine[i].pos.x - ax);
                    if (d < bd) { bd = d; best = net.gardSpine[i]; }
                }
                if (best.HasValue)
                {
                    SpinePt2D s = best.Value;
                    AddPavementArrow(wVerts, wTris, wUvs, new Vector3(s.pos.x + s.leftNorm.x * 1.65f, 0f, s.pos.y + s.leftNorm.y * 1.65f),
                        new Vector3(-s.tan.x, 0f, -s.tan.y), yMark + 0.001f);
                    AddPavementArrow(wVerts, wTris, wUvs, new Vector3(s.pos.x - s.leftNorm.x * 1.65f, 0f, s.pos.y - s.leftNorm.y * 1.65f),
                        new Vector3(s.tan.x, 0f, s.tan.y), yMark + 0.001f);
                }
            }

            // 4. Calle Azucenas East & Manuel Doblado & Herrera North & Service lanes
            EmitDashedCenter(net.azSpine, 2.6f, 3.6f, p => p.x < 12.0f || p.x > 84.0f, wVerts, wTris, wUvs);
            EmitEdgeStripes(net.azSpine, _ => 3.80f, p => p.x < 12.0f || p.x > 84.0f, wVerts, wTris, wUvs);
            EmitCrosswalkBars(wVerts, wTris, wUvs, new Vector2(8.4f, -32.91f), Vector2.right, 3.3f, 2.0f, yMark);

            EmitDashedCenter(net.dobladSpine, 2.6f, 3.6f, p => p.y > -38.5f, wVerts, wTris, wUvs);
            EmitEdgeStripes(net.dobladSpine, _ => 3.80f, p => p.y > -38.5f, wVerts, wTris, wUvs);

            EmitDashedCenter(net.abaSpine, 2.6f, 3.8f, p => p.x < 98.4f || InFvOrBuswayX(p), wVerts, wTris, wUvs);
            EmitEdgeStripes(net.abaSpine, EvalAbaEastHalfLeft, p => p.x < 98.4f || InFvOrBuswayX(p), wVerts, wTris, wUvs);

            EmitEdgeStripes(net.srvSpine, _ => 2.40f, p => p.x < 31.8f || p.x > 82.8f, yVerts, yTris, yUvs);
            for (int i = 10; i < net.srvSpine.Count - 8; i += 14)
            {
                SpinePt2D s = net.srvSpine[i];
                if (s.pos.x >= 35.0f && s.pos.x <= 80.0f)
                    AddPavementArrow(wVerts, wTris, wUvs, new Vector3(s.pos.x, 0f, s.pos.y), new Vector3(s.tan.x, 0f, s.tan.y), yMark + 0.001f);
            }

            EmitDashedCenter(net.srvNorthSpine, 2.6f, 3.8f, p => p.y < 32.0f, wVerts, wTris, wUvs, 0.06f);

            CreateMeshChild("Markings_East_AllStreets_White", root, wVerts, wTris, wUvs, whiteStripeMat);
            CreateMeshChild("Markings_East_AllStreets_Yellow", root, yVerts, yTris, yUvs, yellowStripeMat);
        }

        private static void ExportUnityLayoutJson(OsmNetworkData net)
        {
            string outPath = Path.Combine(ArtifactDir, "scratch", "unity_actual_layout.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            var ci = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder(32768);

            void AppendPts(string name, List<Vector2> pts, bool isLast = false)
            {
                sb.Append($"  \"{name}\": [");
                for (int i = 0; i < pts.Count; i++)
                {
                    sb.Append($"[{pts[i].x.ToString("F2", ci)},{pts[i].y.ToString("F2", ci)}]");
                    if (i + 1 < pts.Count) sb.Append(',');
                }
                sb.Append(isLast ? "]\n" : "],\n");
            }

            List<Vector2> ExtractCenter(List<SpinePt2D> sp)
            {
                List<Vector2> res = new List<Vector2>(sp.Count);
                for (int i = 0; i < sp.Count; i++) res.Add(sp[i].pos);
                return res;
            }

            // Build actual built Street_Abasolo West/Curve edges up to FVWest cut
            float sHitN = FindAbasoloFVWestHitS(AbasoloNorth3LaneOffset);
            float sHitS = FindAbasoloFVWestHitS(AbasoloSouthOffset);
            List<Vector2> abaNEdge = new List<Vector2>(), abaSEdge = new List<Vector2>(), abaCtr = new List<Vector2>();
            for (float s = -46f; s <= Mathf.Max(sHitN, sHitS); s += 1.5f)
            {
                AbasoloSample sp = EvaluateAbasolo(s);
                abaCtr.Add(new Vector2(sp.center.x, sp.center.z));
                if (s <= sHitN) abaNEdge.Add(new Vector2(sp.center.x + sp.northNormalXZ.x * EvalAbasoloNorthOffset(s), sp.center.z + sp.northNormalXZ.z * EvalAbasoloNorthOffset(s)));
                if (s <= sHitS) abaSEdge.Add(new Vector2(sp.center.x + sp.northNormalXZ.x * AbasoloSouthOffset, sp.center.z + sp.northNormalXZ.z * AbasoloSouthOffset));
            }

            sb.Append("{\n");
            AppendPts("abaCenter", abaCtr);
            AppendPts("abaNorthEdge", abaNEdge);
            AppendPts("abaSouthEdge", abaSEdge);
            AppendPts("abaHerreraCenter", ExtractCenter(net.abaSpine));
            AppendPts("fvCenter", ExtractCenter(net.fvSpine));
            AppendPts("fvWestEdge", net.fvWestEdge);
            AppendPts("fvEastEdge", net.fvEastEdge);
            AppendPts("busCenter", ExtractCenter(net.busSpine));
            AppendPts("busWestEdge", net.busWestEdge);
            AppendPts("busEastEdge", net.busEastEdge);
            AppendPts("gardCenter", ExtractCenter(net.gardSpine));
            AppendPts("gardNorthEdge", net.gardNorthEdge);
            AppendPts("gardSouthEdge", net.gardSouthEdge);
            AppendPts("azCenter", ExtractCenter(net.azSpine));
            AppendPts("azNorthEdge", net.azNorthEdge);
            AppendPts("azSouthEdge", net.azSouthEdge);
            AppendPts("dobladCenter", ExtractCenter(net.dobladSpine));
            AppendPts("dobladWestEdge", net.dobladWestEdge);
            AppendPts("dobladEastEdge", net.dobladEastEdge);
            AppendPts("srvCenter", ExtractCenter(net.srvSpine));
            AppendPts("srvNorthEdge", net.srvNorthEdge);
            AppendPts("srvSouthEdge", net.srvSouthEdge);
            AppendPts("srvNorthCenter", ExtractCenter(net.srvNorthSpine));
            AppendPts("srvNorthWestEdge", net.srvNorthWestEdge);
            AppendPts("srvNorthEastEdge", net.srvNorthEastEdge);
            AppendPts("manzanaOuter", net.manzanaOuterPoly);
            AppendPts("manzanaLot", net.manzanaLotPoly);
            AppendPts("cbNeOuter", net.cbNeOuterPoly);
            AppendPts("cbSwOuter", net.cbSwOuterPoly);
            AppendPts("medianNorth", net.medianNorthPoly);
            AppendPts("medianMid", net.medianMidPoly);
            AppendPts("medianSouth", net.medianSouthPoly, isLast: true);
            sb.Append("}\n");

            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[WestTriangleAndAbasoloBuilder] Exported actual Unity corridor layout JSON: {outPath}");
        }

        private static void BuildEastAzucenasAndManzana(Material asphaltMat, Material concreteMat, Material curbMat,
            Material whiteStripeMat, Material yellowStripeMat, Material urbanLotMat)
        {
            HashSet<string> killNames = new HashSet<string>
            {
                "East_Azucenas_Junction_Root",
                "East_AzucenasManzana_Root",
                "Azucenas_Corridor_BaseGround_Ext",
                "Sidewalk_East_SouthOfAzucenas",
                "Curb_East_SouthOfAzucenas",
                "Road_AvenidaJuarez_SouthMarkings",
                "Markings_East_AllStreets_White",
                "Markings_East_AllStreets_Yellow"
            };
            int destroyedCount = 0;
            Transform[] allSceneTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in allSceneTransforms)
            {
                if (tr != null && tr.gameObject.scene.IsValid() && killNames.Contains(tr.name))
                {
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
                    destroyedCount++;
                }
            }
            Debug.Log($"[WestTriangleAndAbasoloBuilder] Cleaned {destroyedCount} Azucenas/Manzana/Marking objects before rebuild.");

            GameObject eastRoadRoot = GameObject.Find("East_Abasolo_Gardenias_Junction_Root");
            if (eastRoadRoot == null)
            {
                eastRoadRoot = new GameObject("East_Abasolo_Gardenias_Junction_Root");
                eastRoadRoot.transform.position = Vector3.zero;
            }
            GameObject envRoot = GameObject.Find("Environment") ?? eastRoadRoot;

            GameObject azRoot = new GameObject("East_Azucenas_Junction_Root");
            azRoot.transform.position = Vector3.zero;

            OsmNetworkData net = GetOsmNetwork();
            float azNorthAtJuarez = net.azNorthEdge[0].y; // -29.11f
            float azSouthAtJuarez = net.azSouthEdge[0].y; // -36.71f

            // 1. Update Av. Juarez, West/East sidewalks & south markings
            ExtendAvenidaJuarezAndBaseGround(envRoot.transform, asphaltMat, concreteMat, curbMat, whiteStripeMat, azNorthAtJuarez, azSouthAtJuarez);

            // 2. Build OSM-aligned Calle Azucenas East (-0.0015f under Juarez/FV/Busway) & Manuel Doblado stub (-0.0030f)
            BuildQuadStripPrism("Roadway_Calle_Azucenas_East", azRoot.transform,
                net.azNorthEdge, net.azSouthEdge, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);
            BuildQuadStripPrism("Roadway_Calle_ManuelDoblado_Stub", azRoot.transform,
                net.dobladWestEdge, net.dobladEastEdge, RoadBotY, RoadTopY - 0.0030f, asphaltMat, addCollider: true);

            // 3. Corner Fillet Asphalt Aprons & Sidewalk Connectors at Av. Juarez x Azucenas (NW R=3.0m, SW R=3.5m)
            List<Vector2> nwArc = SampleBezierChain(
                new Vector2(6.80f, azNorthAtJuarez + 3.00f), Vector2.down,
                new Vector2(9.80f, azNorthAtJuarez), Vector2.right, 1.65f, 14);
            List<Vector2> nwApron = new List<Vector2> { new Vector2(6.80f, azNorthAtJuarez) };
            nwApron.AddRange(nwArc);
            CreateFlatPrism("Roadway_Azucenas_Juarez_NW_Apron", azRoot.transform, nwApron, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);

            List<Vector2> swArc = SampleBezierChain(
                new Vector2(6.80f, azSouthAtJuarez - 3.50f), Vector2.up,
                new Vector2(10.30f, azSouthAtJuarez), Vector2.right, 1.92f, 14);
            List<Vector2> swApron = new List<Vector2> { new Vector2(6.80f, azSouthAtJuarez) };
            swApron.AddRange(swArc);
            CreateFlatPrism("Roadway_Azucenas_Juarez_SW_Apron", azRoot.transform, swApron, RoadBotY, RoadTopY - 0.0015f, asphaltMat, addCollider: true);

            // Clean concrete infill caps & NW corner curb bridging Sidewalk_East cleanly into Manzana / South sidewalks
            List<Vector2> nwCurbChain = new List<Vector2>(nwArc);
            nwCurbChain.Add(new Vector2(12.00f, azNorthAtJuarez));
            BuildCurbAndSidewalkChain("Azucenas_NW_Corner", azRoot.transform, nwCurbChain, true, 1.80f, curbMat, concreteMat);

            List<Vector2> swCapPoly = new List<Vector2>
            {
                new Vector2(9.02f, azSouthAtJuarez - 3.50f),
                new Vector2(12.00f, azSouthAtJuarez - 3.50f),
                new Vector2(12.00f, azSouthAtJuarez - 2.22f),
                new Vector2(10.30f, azSouthAtJuarez - 2.22f)
            };
            CreateFlatPrism("Azucenas_SW_Sidewalk_ConnectorCap", azRoot.transform, swCapPoly, -0.05f, SidewalkTopY - 0.004f, concreteMat, addCollider: true);

            // 4. Irregular Southern Manzana (ZERO new buildings! Preserves NorthBlock/Benavides/Michoacana/DentalBlock untouched)
            GameObject mzRoot = new GameObject("East_AzucenasManzana_Root");
            mzRoot.transform.position = Vector3.zero;
            BuildCurbedUrbanBlock("EastManzana_Irregular", mzRoot.transform, net.manzanaOuterPoly, net.manzanaLotPoly, concreteMat, curbMat, urbanLotMat, addLotPad: true);

            // 5. Azucenas South Sidewalks & Curbs split cleanly across Calle Manuel Doblado + FV West Southbound
            List<Vector2> azSWChain = new List<Vector2>(swArc);
            float dobWestCutZ = azSouthAtJuarez - 0.8f;
            for (int i = 0; i < net.azSouthEdge.Count; i++)
            {
                Vector2 p = net.azSouthEdge[i];
                if (p.x > 10.30f && p.x <= EvalCurveXAtZ(net.dobladWestEdge, p.y) - 0.10f)
                {
                    azSWChain.Add(p);
                    dobWestCutZ = p.y;
                }
            }
            for (int i = 0; i < net.dobladWestEdge.Count; i++)
            {
                if (net.dobladWestEdge[i].y < dobWestCutZ - 0.15f) azSWChain.Add(net.dobladWestEdge[i]);
            }
            BuildCurbAndSidewalkChain("AzucenasSouth_WestOfDoblado", azRoot.transform, azSWChain, false, 1.80f, curbMat, concreteMat);

            float azSouthAtDobE = -37.70f;
            for (int i = 0; i < net.azSouthEdge.Count; i++)
            {
                Vector2 p = net.azSouthEdge[i];
                if (p.x >= EvalCurveXAtZ(net.dobladEastEdge, p.y) + 0.10f) { azSouthAtDobE = p.y; break; }
            }
            List<Vector2> azSEChain = new List<Vector2>();
            for (int i = net.dobladEastEdge.Count - 1; i >= 0; i--)
            {
                if (net.dobladEastEdge[i].y < azSouthAtDobE - 0.15f) azSEChain.Add(net.dobladEastEdge[i]);
            }
            float fvWestCutZ = azSouthAtDobE;
            for (int i = 0; i < net.azSouthEdge.Count; i++)
            {
                Vector2 p = net.azSouthEdge[i];
                if (p.x >= EvalCurveXAtZ(net.dobladEastEdge, p.y) + 0.10f && p.x <= EvalCurveXAtZ(net.fvWestEdge, p.y) - 0.10f)
                {
                    azSEChain.Add(p);
                    fvWestCutZ = p.y;
                }
            }
            for (int i = 0; i < net.fvWestEdge.Count; i++)
            {
                if (net.fvWestEdge[i].y < fvWestCutZ - 0.15f) azSEChain.Add(net.fvWestEdge[i]);
            }
            BuildCurbAndSidewalkChain("AzucenasSouth_EastOfDoblado", azRoot.transform, azSEChain, false, 1.80f, curbMat, concreteMat);

            // 6. All Crisp Lane Markings & Export Actual Built Layout JSON for Automated OSM Overlay Check
            BuildEastOsmMarkings(eastRoadRoot.transform, net, whiteStripeMat, yellowStripeMat);
            ExportUnityLayoutJson(net);

            Debug.Log("[WestTriangleAndAbasoloBuilder] Built OSM-aligned Calle Azucenas East, Manuel Doblado stub, Southern Irregular Manzana & exported actual JSON.");
        }

        // =========================================================================
        // VERIFICATION SCREENSHOTS
        // =========================================================================

        public static void CaptureVerificationScreenshots()
        {
            Directory.CreateDirectory(ArtifactDir);

            GameObject camGo = new GameObject("TempWestTriangleVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 650f;
            cam.clearFlags = CameraClearFlags.Skybox;

            try
            {
                // Shot 1: Drone Overview matching user drone screenshot (ALT ~24m, pitch ~18 deg N)
                cam.transform.position = new Vector3(-2.0f, 24.1f, -3.0f);
                cam.transform.rotation = Quaternion.Euler(19.0f, 355.0f, 0f);
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "west_pico_shot1_drone_24m.png"));

                // Shot 2: High Oblique focused on West Triangle, Wall, Diagonal Street & Y-Junction
                cam.transform.position = new Vector3(-40.0f, 30.0f, 26.0f);
                cam.transform.LookAt(new Vector3(-32.0f, 0.2f, 52.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "west_pico_shot2_topdown_junction.png"));

                // Shot 3: Street-level approaching the West "Pico", red-white perimeter wall & Y-junction
                cam.transform.position = new Vector3(-47.0f, 3.2f, 41.0f);
                cam.transform.LookAt(new Vector3(-28.0f, 1.2f, 51.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "west_pico_shot3_streetlevel_pico.png"));

                // Shot 4: Oblique North looking back across relocated Street_Abasolo & bridge talud
                cam.transform.position = new Vector3(-30.0f, 13.5f, 75.0f);
                cam.transform.LookAt(new Vector3(-8.0f, 1.0f, 51.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "west_pico_shot4_abasolo_behind_bridge.png"));

                // Shot 5: Google Street View match from 993 Calle Gardenias looking toward West Wing, Stairs, Gate & Wall
                cam.transform.position = new Vector3(-13.5f, 2.35f, 33.2f);
                cam.transform.LookAt(new Vector3(-17.2f, 2.15f, 45.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "west_pico_shot5_streetview_match.png"));

                // Shot 6: High Drone Overview (157m ALT) matching user screenshot across full East corridor & Francisco Villa
                cam.transform.position = new Vector3(26.0f, 157.0f, -58.0f);
                cam.transform.LookAt(new Vector3(48.0f, 0.0f, 20.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "abasolo_shot6_east_drone_overview.png"));

                // Shot 7: Street-level West underpass tunnel view checking 3-lane width, yellow dividers, arrows & bus clearance
                cam.transform.position = new Vector3(-36.0f, 0.3f, 60.4f);
                cam.transform.LookAt(new Vector3(16.0f, -3.5f, 60.4f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "abasolo_shot7_underpass_tunnel_west.png"));

                // Shot 8: Near-vertical Top-Down Plan View of full East Enclosed Block, Gardenias, 3-lane Abasolo, Azucenas & Francisco Villa/Busway
                cam.transform.position = new Vector3(54.0f, 178.0f, 12.0f);
                cam.transform.LookAt(new Vector3(54.0f, 0.0f, 12.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "east_junc_shot8_topdown_block.png"));

                // Shot 9: SE Oblique view at Francisco Villa corner showing 3-lane Abasolo, Gardenias, Azucenas & Busway junction
                cam.transform.position = new Vector3(124.0f, 38.0f, -56.0f);
                cam.transform.LookAt(new Vector3(78.0f, 0.2f, -16.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "east_junc_shot9_street_curve.png"));

                // Shot 10: High Top-Down Plan View centered on the Irregular Southern Manzana, Azucenas East, Gardenias & Francisco Villa
                cam.fieldOfView = 52f;
                cam.transform.position = new Vector3(56.0f, 146.0f, -5.0f);
                cam.transform.LookAt(new Vector3(56.0f, 0.0f, -4.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "azucenas_shot10_topdown_manzana.png"));

                // Shot 11: Real Aerial Drone Photo Angle (SE looking WNW across Francisco Villa, Busway, Gardenias mouth, Azucenas & Manzana)
                cam.fieldOfView = 36f;
                cam.transform.position = new Vector3(150.0f, 62.0f, -80.0f);
                cam.transform.LookAt(new Vector3(48.0f, 0.5f, -14.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "azucenas_shot11_photo8_angle.png"));

                // Shot 12: Av. Benito Juarez x Calle Azucenas East view looking East along Azucenas toward Gardenias & Francisco Villa
                cam.fieldOfView = 52f;
                cam.transform.position = new Vector3(1.5f, 9.5f, -44.0f);
                cam.transform.LookAt(new Vector3(58.0f, 0.2f, -31.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(ArtifactDir, "azucenas_shot12_av_juarez_corner.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static void RenderToFile(Camera cam, int w, int h, string filePath)
        {
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[WestTriangleAndAbasoloBuilder] Saved screenshot: {filePath}");
        }
    }
}
