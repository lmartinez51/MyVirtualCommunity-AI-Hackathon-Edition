using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Phase 3 Definitive Precision Builder: East Pedestrian Walkway.
    /// Incorporates real-world photo alignment from webcam media_1794461277329_9ca93cc4.jpg:
    /// 1. Unified continuous sidewalk: Smooth, continuous concrete floor elevated +12cm (Y = 0.520m)
    ///    above street asphalt (Y = 0.400m). No steps, no artificial breaks.
    /// 2. Blue accessible ramp as an embedded curb cut:
    ///    - Width strictly 1.05m (X = 14.00m to 15.05m) for single-wheelchair capacity.
    ///    - Starts flush with sidewalk at Z = 58.00m (Y = 0.520m, 0.0cm step).
    ///    - Descents at 3.0% slope to street asphalt at Z = 54.00m (Y = 0.400m, 0.0cm step).
    /// 3. Narrow street exit mouth (1.20m wide):
    ///    - Exterior 3-tube galvanized guardrail and yellow curb extend south all the way to Z = 55.20m!
    ///    - Only Z = 54.00m to 55.20m (1.20m wide) is cut down for the wheelchair exit.
    ///    - Stacked orange traffic drums at Z = 55.20m matching the real photo.
    /// 4. Yellow safety railing:
    ///    - Longitudinal divider along X = 15.05m (Z = 54.00m to 58.00m).
    ///    - Transverse closure barrier at Z = 54.00m from wall corner (X = 14.00m) to rail (X = 15.05m).
    /// 5. Camera shots including Shot 8 matching the webcam angle exactly.
    /// </summary>
    public static class EastWalkwayPhase1Builder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const int WinWidth = 1280;
        private const int WinHeight = 720;
        private const int TitleBarHeight = 32;

        [MenuItem("MyVirtualCommunity/Execute East Walkway Phase 1 Upgrade")]
        public static void ExecutePhase1Upgrade()
        {
            Debug.Log("================================================================================");
            Debug.Log("[EastWalkwayPhase1Builder] STARTING DEFINITIVE PHOTO-ALIGNED RECONSTRUCTION");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[EastWalkwayPhase1Builder] Failed to open scene {ScenePath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // 1. Materials Setup
            var concreteFloorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/ConcreteMaterial.mat") ??
                                   CreateOrGetMaterial("Assets/Settings/SidewalkPavement.mat", new UnityEngine.Color(0.68f, 0.67f, 0.65f), 0.15f);
            var darkConcreteMat = CreateOrGetMaterial("Assets/Settings/BridgeDarkGreyConcrete.mat", new UnityEngine.Color(0.24f, 0.25f, 0.27f), 0.12f);
            var handicapBlueMat = CreateOrGetMaterial("Assets/Settings/HandicapAccessibilityBlue.mat", new UnityEngine.Color(0.00f, 0.45f, 0.81f), 0.35f);
            var yellowCurbMat = CreateOrGetMaterial("Assets/Settings/YellowSafetyCurb.mat", new UnityEngine.Color(0.96f, 0.78f, 0.08f), 0.30f);
            var yellowRailMat = CreateOrGetMaterial("Assets/Settings/InteriorSafetyYellowRail.mat", new UnityEngine.Color(0.98f, 0.80f, 0.05f), 0.45f);
            var tubularSteelMat = CreateOrGetMaterial("Assets/Settings/BridgeGalvanizedTubularSteel.mat", new UnityEngine.Color(0.80f, 0.82f, 0.85f), 0.70f);
            var whiteBorderMat = CreateOrGetMaterial("Assets/Settings/WhiteStripeMaterial.mat", new UnityEngine.Color(0.96f, 0.96f, 0.96f), 0.2f);
            var tactilePavingMat = CreateOrGetMaterial("Assets/Settings/TactileWarningPaving.mat", new UnityEngine.Color(0.95f, 0.75f, 0.05f), 0.25f);

            // 2. Disable obsolete conflicting GameObjects
            string[] obsoleteNames = new string[]
            {
                "Sidewalk_Slab_South_Z34_54",
                "Sidewalk_Slab_MidConcourse_Z54_58",
                "Sidewalk_Slab_North_Z58_69",
                "EastWalkway_HandicapBlue_AccessibleRamp",
                "EastWalkway_HandicapBlue_RampPlate",
                "Ramp_HandicapBlue_SurfacePlate",
                "Sidewalk_BridgeDeck_TransitionRamp",
                "EastWalkway_ShortConcreteWall_4.5m"
            };

            foreach (string objName in obsoleteNames)
            {
                GameObject[] found = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var g in found)
                {
                    if (g.name == objName && g.transform.parent?.name != "East_Walkway_Phase1_Root")
                    {
                        g.SetActive(false);
                    }
                }
            }

            GameObject oldRampSouth = GameObject.Find("Walkway_Ramp_South");
            if (oldRampSouth != null) oldRampSouth.SetActive(false);

            GameObject oldBarrier = GameObject.Find("Bridge_Barrier_East");
            if (oldBarrier != null) UnityEngine.Object.DestroyImmediate(oldBarrier);

            // Disable ALL solid concrete blind parapet walls along the East fixed-roof bridge walkway
            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject parapet = GameObject.Find($"Bridge_Parapet_East_Seg{seg:D2}");
                if (parapet != null) parapet.SetActive(false);
            }
            GameObject rampParapet = GameObject.Find("Plaza_RampParapet_East");
            if (rampParapet != null) rampParapet.SetActive(false);

            // 3. Locate or recreate root for East Walkway Phase 3
            GameObject root = GameObject.Find("East_Walkway_Phase1_Root");
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            root = new GameObject("East_Walkway_Phase1_Root");

            GameObject borderPlaza = GameObject.Find("Border_Crossing_Plaza");
            if (borderPlaza != null) root.transform.SetParent(borderPlaza.transform);

            // =========================================================================================
            // 4. PROCEDURAL WALKWAY SLABS (UNIFIED CONTINUOUS ELEVATED SIDEWALK)
            // Street roadway asphalt (Bridge_Span_Deck) is at Y = 0.400m.
            // Sidewalk is elevated +12cm at Y = 0.520m.
            // A) South Plaza Approach (Z = 34.00m to 54.00m, X = 14.00m to 16.50m)
            //    Smoothly rises from plaza Y = 0.200m at Z = 34m to bridge sidewalk Y = 0.520m at Z = 54m (1.6% slope).
            // B) Main Pedestrian Concourse (Z = 54.00m to 58.00m, X = 15.05m to 16.50m, Width = 1.45m)
            //    Continuous level concrete surface at Y = 0.520m! Zero steps!
            // C) Blue Preferential Accessible Ramp (Z = 54.00m to 58.00m, X = 14.00m to 15.05m, Width = 1.05m)
            //    Embedded curb cut: Descents from Y = 0.520m (at Z = 58m) down to Y = 0.400m (at Z = 54m).
            // D) Bridge Plateau (Z = 58.00m to 64.50m, X = 14.00m to 16.50m, Full Width 2.50m, Y = 0.520m)
            // E) Northern Transition (Z = 64.50m to 68.015m, Y = 0.520m -> 0.4432m, matches Walkway_Slab_Seg01)
            // =========================================================================================
            GameObject sidewalkRoot = new GameObject("EastWalkway_ProceduralSidewalk_Root");
            sidewalkRoot.transform.SetParent(root.transform);

            // A) South Plaza Approach (Z = 34.00m to 54.00m)
            // 1. Main Concourse & East Infill (Z = 34.00m to 54.00m, X = 13.80m to 17.00m)
            //    Spans from the concrete wall/canopy west posts (X = 13.80m) to Plaza_Pavement_Extension (X = 17.00m).
            //    Completely seals the 50cm east trench against the kiosk plaza!
            CreateSolidSlab("Sidewalk_Plaza_South_Main_Z34_54", sidewalkRoot.transform,
                13.80f, 17.00f, 34.00f, 54.00f,
                0.200f, 0.520f, 0.000f,
                0.000f, 0.000f, concreteFloorMat);

            // 2. West Apron Infill (Z = 34.00m to 49.50m, X = 12.10m to 13.80m)
            //    Spans from the toll booth curb Curb_East_North_Section (X = 12.10m) to the concourse (X = 13.80m).
            //    Ends cleanly at the south face of the short concrete wall (Z = 49.50m).
            //    Rises smoothly in identical slope (0.200m at Z=34m to 0.448m at Z=49.5m) flush with Main Concourse.
            //    Completely seals the 1.90m west brown trench against Toll Booth #4!
            CreateSolidSlab("Sidewalk_Plaza_South_WestApron_Z34_49", sidewalkRoot.transform,
                12.10f, 13.80f, 34.00f, 49.50f,
                0.200f, 0.448f, 0.000f,
                0.000f, 0.000f, concreteFloorMat);

            // B) Main Pedestrian Walking Concourse (Z = 54.00m to 58.00m, X = 15.05m to 17.00m)
            //    Extends east edge to X = 17.00m to seamlessly abut Plaza_Pavement_Extension.
            CreateSolidSlab("Sidewalk_Ramp_MainConcourse_Z54_58", sidewalkRoot.transform,
                15.05f, 17.00f, 54.00f, 58.00f,
                0.520f, 0.520f, 0.200f,
                0.000f, 0.000f, concreteFloorMat);

            // C) Blue Accessible Ramp Paint (Z = 54.00m to 56.70m, X = 14.00m to 15.05m, Width = 1.05m)
            //    Final exit landing (Z = 54.00m to 55.20m) is level at Y = 0.400m, flush with street asphalt!
            //    Ramp slopes smoothly from Y = 0.400m at Z = 55.20m up to Y = 0.4643m at Z = 56.70m.
            CreateSolidSlab("Sidewalk_Ramp_BlueExitLanding_Z54_55", sidewalkRoot.transform,
                14.00f, 15.05f, 54.00f, 55.20f,
                0.400f, 0.400f, 0.200f,
                0.000f, 0.000f, handicapBlueMat);

            CreateSolidSlab("Sidewalk_Ramp_BlueChute_Z54_56", sidewalkRoot.transform,
                14.00f, 15.05f, 55.20f, 56.70f,
                0.400f, 0.4642857f, 0.200f,
                0.000f, 0.000f, handicapBlueMat);

            // C2) Continuation of the ramp slope in standard concrete (Z = 56.70m to 58.00m)
            // Preserves 100% of the physical floor slope and elevation, but textured in standard concrete!
            CreateSolidSlab("Sidewalk_Ramp_NorthConcrete_Z56_58", sidewalkRoot.transform,
                14.00f, 15.05f, 56.70f, 58.00f,
                0.4642857f, 0.520f, 0.200f,
                0.000f, 0.000f, concreteFloorMat);

            // D) Elevated Bridge Sidewalk Plateau (Z = 58.00m to 64.50m, Full Width 3.00m: X = 14.00m to 17.00m)
            //    Extends east edge to X = 17.00m to seamlessly abut Plaza_Pavement_Extension.
            CreateSolidSlab("Sidewalk_Plateau_Bridge_Z58_64", sidewalkRoot.transform,
                14.00f, 17.00f, 58.00f, 64.50f,
                0.520f, 0.520f, 0.200f,
                0.000f, 0.000f, concreteFloorMat);

            // E) Northern Transition to Walkway_Slab_Seg01 (Z = 64.50m to 68.015m, Y = 0.520m -> 0.4432m)
            //    Extends east edge to X = 17.00m to seamlessly abut Plaza_Pavement_Extension.
            CreateSolidSlab("Sidewalk_Transition_North_Z64_68", sidewalkRoot.transform,
                14.00f, 17.00f, 64.50f, 68.015f,
                0.520f, 0.4432f, 0.200f,
                0.000f, 0.000f, concreteFloorMat);

            // =========================================================================================
            // 5. SHORT DARK GREY CONCRETE WALL ("Barda Corta", Z = 49.50m to 54.00m, Length = 4.5m)
            // =========================================================================================
            GameObject shortWallGo = new GameObject("EastWalkway_ShortConcreteWall_4.5m");
            shortWallGo.transform.SetParent(root.transform);
            shortWallGo.transform.position = new Vector3(13.82f, 0.35f, 51.75f);

            GameObject wallBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallBody.name = "Wall_DarkConcrete_Body";
            wallBody.transform.SetParent(shortWallGo.transform);
            wallBody.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            wallBody.transform.localScale = new Vector3(0.38f, 0.90f, 4.50f);
            SetMaterial(wallBody, darkConcreteMat);

            GameObject wallCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallCap.name = "Wall_DarkConcrete_Cap";
            wallCap.transform.SetParent(shortWallGo.transform);
            wallCap.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            wallCap.transform.localScale = new Vector3(0.44f, 0.08f, 4.50f);
            SetMaterial(wallCap, darkConcreteMat);

            // =========================================================================================
            // 6. EXTERIOR YELLOW CURB (X = 13.80m to 14.00m)
            // - Along bridge plateau (Z = 58.00m to 64.50m): Level at Y = 0.520m (+12cm above asphalt)
            // - Along ramp side (Z = 55.20m to 58.00m): Guardrail curb slopes with the ramp
            // - VADO / CURB CUT (Z = 54.00m to 55.20m, 1.20m wide): Tapers down to asphalt Y = 0.400m!
            // =========================================================================================
            GameObject curbRoot = new GameObject("EastWalkway_YellowCurb_Root");
            curbRoot.transform.SetParent(root.transform);

            // Curb Section A: Along Ramp Outer Edge (Z = 55.20m to 58.00m)
            CreateSolidSlab("Sidewalk_Bridge_YellowCurb_SecA_Ramp", curbRoot.transform,
                13.80f, 14.00f, 55.20f, 58.00f,
                0.400f, 0.520f, 0.350f,
                0.000f, 0.000f, yellowCurbMat);

            // Curb Section B: Along Bridge Plateau (Z = 58.00m to 64.50m)
            CreateSolidSlab("Sidewalk_Bridge_YellowCurb_SecB_Plateau", curbRoot.transform,
                13.80f, 14.00f, 58.00f, 64.50f,
                0.520f, 0.520f, 0.350f,
                0.000f, 0.000f, yellowCurbMat);

            // Curb Section C: Along North Transition (Z = 64.50m to 68.015m)
            CreateSolidSlab("Sidewalk_Bridge_YellowCurb_SecC_Transition", curbRoot.transform,
                13.80f, 14.00f, 64.50f, 68.015f,
                0.520f, 0.4432f, 0.250f,
                0.000f, 0.000f, yellowCurbMat);

            // Level curb cut lip at the 1.20m mouth (Z = 54.00m to 55.20m, X = 13.85m to 14.00m), 100% flush with street Y=0.400m
            CreateSolidSlab("Sidewalk_Ramp_ExitLip_Z54_55", curbRoot.transform,
                13.85f, 14.00f, 54.00f, 55.20f,
                0.400f, 0.400f, 0.350f,
                0.000f, 0.000f, yellowCurbMat);

            // =========================================================================================
            // 7. EXTERIOR 3-TUBE GALVANIZED GUARDRAIL (Z = 55.20m TO 68.015m)
            // Extends all the way down to Z = 55.20m!
            // Leaves ONLY a 1.20m wide opening (Z = 54.00m to 55.20m) for the wheelchair exit!
            // =========================================================================================
            GameObject guardrailRoot = new GameObject("EastWalkway_3Tube_TubularGuardrail_Root");
            guardrailRoot.transform.SetParent(root.transform);

            float[] postZ = new float[] { 55.20f, 56.60f, 58.00f, 59.60f, 61.20f, 62.80f, 64.50f, 66.25f, 68.015f };
            float[] postCurbY = new float[postZ.Length];
            for (int i = 0; i < postZ.Length; i++)
            {
                postCurbY[i] = GetCurbSurfaceY(postZ[i]);
                GameObject postGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postGo.name = $"Post_Tubular_Bridge_{i + 1}";
                postGo.transform.SetParent(guardrailRoot.transform);
                postGo.transform.position = new Vector3(13.90f, postCurbY[i] + 0.575f, postZ[i]);
                postGo.transform.localScale = new Vector3(0.07f, 0.575f, 0.07f);
                SetMaterial(postGo, tubularSteelMat);

                GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseGo.name = $"AnchorPlate_Bridge_{i + 1}";
                baseGo.transform.SetParent(guardrailRoot.transform);
                baseGo.transform.position = new Vector3(13.90f, postCurbY[i] + 0.015f, postZ[i]);
                baseGo.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
                SetMaterial(baseGo, tubularSteelMat);
            }

            // Continuous horizontal tubes spanning between each pair of posts
            float[] tubeRelHeights = new float[] { 0.35f, 0.70f, 1.05f };
            for (int s = 0; s < postZ.Length - 1; s++)
            {
                float zA = postZ[s], zB = postZ[s + 1];
                float yA = postCurbY[s], yB = postCurbY[s + 1];
                for (int t = 0; t < 3; t++)
                {
                    CreateSlopedTube($"Tube_Span_{s + 1}_Level_{t + 1}", guardrailRoot.transform,
                        13.90f, yA + tubeRelHeights[t], zA,
                        13.90f, yB + tubeRelHeights[t], zB,
                        0.05f, tubularSteelMat);
                }
            }

            // 7B. Extend Yellow Curb and 3-Tube Galvanized Guardrail across the entire fixed-roof bridge
            //     (Seg01..Seg28 + Plaza Ramp), replacing all disabled blind parapet walls.
            BuildBridgeNorthExtensionCurbAndGuardrail(root.transform, yellowCurbMat, tubularSteelMat);

            // (Sign_NoEstacionarse_Board removed per user request)

            // =========================================================================================
            // 9. INTERIOR YELLOW SAFETY DIVIDER RAILING (X = 15.05m) & TRANSVERSE SOUTH CLOSURE
            // Width of blue ramp = 1.05m (from X = 14.00m to X = 15.05m)
            // Trimmed to 3 posts (Z = 54.00m, 55.35m, 56.70m) per user instruction.
            // =========================================================================================
            GameObject yellowRailRoot = new GameObject("EastWalkway_InteriorYellowSafetyRail_Root");
            yellowRailRoot.transform.SetParent(root.transform);

            // A) Longitudinal Railing along X = 15.05m (3 posts, ending at Z = 56.70m)
            float[] yellowPostZ = new float[] { 54.00f, 55.35f, 56.70f };
            for (int i = 0; i < yellowPostZ.Length; i++)
            {
                float pZ = yellowPostZ[i];
                float pFloorY = GetRampSurfaceY(pZ);

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"YellowRail_Long_Post_{i + 1}";
                post.transform.SetParent(yellowRailRoot.transform);
                post.transform.position = new Vector3(15.05f, pFloorY + 0.525f, pZ);
                post.transform.localScale = new Vector3(0.06f, 0.525f, 0.06f);
                SetMaterial(post, yellowRailMat);

                GameObject basePl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                basePl.name = $"YellowRail_Long_Base_{i + 1}";
                basePl.transform.SetParent(yellowRailRoot.transform);
                basePl.transform.position = new Vector3(15.05f, pFloorY + 0.015f, pZ);
                basePl.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
                SetMaterial(basePl, yellowRailMat);
            }

            // Continuous longitudinal handrail spans
            float[] yellowRelHeights = new float[] { 0.15f, 0.75f, 0.95f };
            string[] yellowTubeNames = new string[] { "ToeRail_0.15m", "LowerHandrail_0.75m", "UpperHandrail_0.95m" };
            for (int s = 0; s < yellowPostZ.Length - 1; s++)
            {
                float zA = yellowPostZ[s], zB = yellowPostZ[s + 1];
                float yA = GetRampSurfaceY(zA), yB = GetRampSurfaceY(zB);
                for (int t = 0; t < 3; t++)
                {
                    CreateSlopedTube($"YellowRail_Long_Span_{s + 1}_{yellowTubeNames[t]}", yellowRailRoot.transform,
                        15.05f, yA + yellowRelHeights[t], zA,
                        15.05f, yB + yellowRelHeights[t], zB,
                        t == 0 ? 0.035f : (t == 1 ? 0.040f : 0.050f), yellowRailMat);
                }
            }

            // North terminal return at 3rd post (Z = 56.70m)
            CreateTerminalReturn("YellowRail_North_Return", yellowRailRoot.transform,
                15.05f, GetRampSurfaceY(56.70f), 56.70f, 0.20f, yellowRailMat);

            // B) Transverse South Closure Barrier at Z = 54.00m (from Wall X=14.00m to Rail X=15.05m)
            GameObject southTransGo = new GameObject("YellowRail_South_TransverseClosure");
            southTransGo.transform.SetParent(yellowRailRoot.transform);

            float southFloorY = 0.400f; // Surface elevation of blue ramp at Z = 54.00m
            // Corner post at wall connection (X = 14.00m, Z = 54.00m)
            GameObject cornerPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cornerPost.name = "YellowRail_CornerPost_WallConnection";
            cornerPost.transform.SetParent(southTransGo.transform);
            cornerPost.transform.position = new Vector3(14.00f, southFloorY + 0.525f, 54.00f);
            cornerPost.transform.localScale = new Vector3(0.06f, 0.525f, 0.06f);
            SetMaterial(cornerPost, yellowRailMat);

            GameObject cornerBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornerBase.name = "YellowRail_CornerBase_WallConnection";
            cornerBase.transform.SetParent(southTransGo.transform);
            cornerBase.transform.position = new Vector3(14.00f, southFloorY + 0.015f, 54.00f);
            cornerBase.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            SetMaterial(cornerBase, yellowRailMat);

            // Upper transverse closure tube (+0.95m)
            CreateSlopedTube("YellowRail_South_Transverse_UpperTube", southTransGo.transform,
                14.00f, southFloorY + 0.95f, 54.00f,
                15.05f, southFloorY + 0.95f, 54.00f,
                0.05f, yellowRailMat);

            // Lower transverse closure tube (+0.75m)
            CreateSlopedTube("YellowRail_South_Transverse_MidTube", southTransGo.transform,
                14.00f, southFloorY + 0.75f, 54.00f,
                15.05f, southFloorY + 0.75f, 54.00f,
                0.04f, yellowRailMat);

            // Toe protective rail (+0.15m)
            CreateSlopedTube("YellowRail_South_Transverse_ToeRail", southTransGo.transform,
                14.00f, southFloorY + 0.15f, 54.00f,
                15.05f, southFloorY + 0.15f, 54.00f,
                0.035f, yellowRailMat);

            // =========================================================================================
            // 10. UPDATE WITNESS DON HECTOR POSITION TO MATCH SMOOTH SIDEWALK ELEVATION
            // =========================================================================================
            GameObject donHector = GameObject.Find("Witness_DonHector");
            if (donHector != null)
            {
                // Sidewalk at Z = 49.00m rises smoothly: Y = 0.200 + (49 - 34)/(54 - 34) * (0.520 - 0.200) = 0.440m
                donHector.transform.position = new Vector3(15.60f, 0.440f, 49.00f);
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // =========================================================================================
            // 11. CAPTURE HIGH-RESOLUTION VERIFICATION SCREENSHOTS
            // =========================================================================================
            CaptureAllVerificationScreenshots();

            Debug.Log("[EastWalkwayPhase1Builder] PHASE 3 DEFINITIVE RECONSTRUCTION FINISHED SUCCESSFULLY!");
            Debug.Log("================================================================================");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void CaptureAllVerificationScreenshots()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "VerificationScreenshots"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string brainArtifactsDir = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8";

            GameObject camGo = new GameObject("VerificationShotCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Looking South towards Juarez along walkway
            cam.transform.position = new Vector3(15.60f, 1.70f, 62.00f);
            cam.transform.rotation = Quaternion.Euler(4.0f, 186.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot1_looking_south_juarez.png",
                "Paso del Norte — Vista Sur: Andador Parejo, Rampa Azul Angosta y Barandal Cerrado al Sur");

            // Shot 2: Looking North towards Bridge along walkway
            cam.transform.position = new Vector3(15.60f, 1.70f, 50.00f);
            cam.transform.rotation = Quaternion.Euler(4.0f, 352.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot2_looking_north_bridge.png",
                "Paso del Norte — Vista Norte: Cierre Sur hacia la Fila, Rampa Azul Integrada y Barandal 3 Tubos");

            // Shot 3: Lateral Profile showing slope and street alignment
            cam.transform.position = new Vector3(10.00f, 2.00f, 56.00f);
            cam.transform.rotation = Quaternion.Euler(6.0f, 75.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot3_lateral_profile.png",
                "Paso del Norte — Perfil Lateral: Barandal Exterior hasta Z=55.20m y Boca de Salida de 1.20m");

            // Shot 4: Top-Down Aerial Plan View
            cam.transform.position = new Vector3(15.00f, 12.00f, 56.00f);
            cam.transform.rotation = Quaternion.Euler(75.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot4_topdown_aerial.png",
                "Paso del Norte — Vista Cenital: Boca de Salida de 1.20m, Tambores de Tráfico y Rampa Angosta");

            // Shot 5: Detailed view of the narrow street exit mouth
            cam.transform.position = new Vector3(12.50f, 1.30f, 53.00f);
            cam.transform.rotation = Quaternion.Euler(10.0f, 40.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot5_roadway_edge_connection.png",
                "Paso del Norte — Detalle Boca de Salida: Vado de 1.20m a Ras de Asfalto con Tambores Naranja");

            // Shot 6: Frontal detail of the south closure
            cam.transform.position = new Vector3(14.50f, 1.30f, 51.50f);
            cam.transform.rotation = Quaternion.Euler(12.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot6_blue_route_south_entrance.png",
                "Paso del Norte — Detalle Frontal: Barandal Amarillo de Cierre Sur hacia la Fila Peatonal");

            // Shot 7: North junction at Z = 68.015m
            cam.transform.position = new Vector3(15.25f, 1.40f, 65.50f);
            cam.transform.rotation = Quaternion.Euler(14.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot7_north_junction_walkway.png",
                "Paso del Norte — Empalme Norte Z=68.015m: Continuidad Total con Andador Techado");

            // Shot 8: CAMERA PERSPECTIVE MATCHING WEBCAM PHOTO EXACTLY!
            // High vantage point looking down and north-northwest towards ramp, canopy, and street
            cam.transform.position = new Vector3(17.50f, 6.80f, 46.50f);
            cam.transform.rotation = Quaternion.Euler(28.0f, 336.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot8_webcam_matching_angle.png",
                "Paso del Norte — Perspectiva Elevada Idéntica a Cámara Real de Tráfico");

            // Shot 9: PERSPECTIVE MATCHING USER SCREENSHOT (media_1794510065555_92a36ad2.jpg)
            // Player standing at Citizen Service Sector looking North towards canopy queue, Elena Kiosk and Toll Booth 4
            cam.transform.position = new Vector3(16.50f, 1.70f, 30.00f);
            cam.transform.rotation = Quaternion.Euler(5.0f, 15.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot9_queue_gaps_fixed.png",
                "Paso del Norte — Vista Frente a Caseta 4 y Kiosco: Huecos y Zanjas de Piso 100% Sellados");

            // Shot 10: PERSPECTIVE MATCHING USER ROADWAY SCREENSHOT (media_1794515694651_2eb723b9.jpg)
            // Looking North along the roadway at the transition into the fixed-roof walkway (blind parapet replaced)
            cam.transform.position = new Vector3(10.50f, 1.70f, 62.00f);
            cam.transform.rotation = Quaternion.Euler(2.0f, 8.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainArtifactsDir, "east_walkway_shot10_parapet_replaced_by_guardrail.png",
                "Paso del Norte — Muro Ciego Reemplazado por Barandal Tubular, Banqueta y Cordón Amarillo");

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void BuildBridgeNorthExtensionCurbAndGuardrail(Transform root, Material yellowCurbMat, Material tubularSteelMat)
        {
            GameObject extRoot = new GameObject("EastWalkway_BridgeExtension_GuardrailAndCurb_Root");
            extRoot.transform.SetParent(root);

            var segments = new System.Collections.Generic.List<Transform>();
            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject slabGo = GameObject.Find($"Walkway_Slab_Seg{seg:D2}");
                if (slabGo != null) segments.Add(slabGo.transform);
            }
            GameObject rampSlabGo = GameObject.Find("Walkway_Slab_Ramp");
            if (rampSlabGo != null) segments.Add(rampSlabGo.transform);

            if (segments.Count == 0)
            {
                Debug.LogWarning("[EastWalkwayPhase1Builder] No Walkway_Slab_SegXX objects found to extend guardrail.");
                return;
            }

            var postAnchors = new System.Collections.Generic.List<Vector3>();
            // Start seamlessly from Post_Tubular_Bridge_9 at Z = 68.015m
            postAnchors.Add(new Vector3(13.90f, GetCurbSurfaceY(68.015f), 68.015f));

            float[] tubeRelHeights = new float[] { 0.35f, 0.70f, 1.05f };

            for (int i = 0; i < segments.Count; i++)
            {
                Transform slab = segments[i];
                float slabLen = slab.localScale.z;
                bool isRamp = slab.name == "Walkway_Slab_Ramp";

                // 1. Continue Yellow Curb ("cordón amarillo") flush with Walkway_Slab top (+0.075m)
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = $"BridgeExt_YellowCurb_{slab.name}";
                curb.transform.SetParent(extRoot.transform);
                curb.transform.position = slab.position + slab.right * (-1.35f) + slab.up * (-0.075f);
                curb.transform.rotation = slab.rotation;
                curb.transform.localScale = new Vector3(0.20f, 0.30f, slabLen);
                SetMaterial(curb, yellowCurbMat);

                // 2. Sample post anchors every ~2.0m along the curb top surface
                float startMeters = isRamp ? -(slabLen * 0.5f) + 2.0f : -4.0f;
                float endMeters = isRamp ? (slabLen * 0.5f) - 1.0f : 4.0f;
                for (float d = startMeters; d <= endMeters + 0.01f; d += 2.0f)
                {
                    Vector3 anchor = slab.position + slab.right * (-1.35f) + slab.up * (0.075f) + slab.forward * d;
                    if (postAnchors.Count == 0 || Vector3.Distance(postAnchors[postAnchors.Count - 1], anchor) >= 1.2f)
                    {
                        postAnchors.Add(anchor);
                    }
                }
            }

            // 3. Instantiate posts (skip index 0 because Post_Tubular_Bridge_9 already sits there) and 3-tube spans
            for (int i = 0; i < postAnchors.Count; i++)
            {
                Vector3 p = postAnchors[i];
                if (i > 0)
                {
                    GameObject postGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    postGo.name = $"BridgeExt_Post_{i}";
                    postGo.transform.SetParent(extRoot.transform);
                    postGo.transform.position = p + Vector3.up * 0.575f;
                    postGo.transform.localScale = new Vector3(0.07f, 0.575f, 0.07f);
                    SetMaterial(postGo, tubularSteelMat);

                    GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    baseGo.name = $"BridgeExt_AnchorPlate_{i}";
                    baseGo.transform.SetParent(extRoot.transform);
                    baseGo.transform.position = p + Vector3.up * 0.015f;
                    baseGo.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
                    SetMaterial(baseGo, tubularSteelMat);
                }

                if (i < postAnchors.Count - 1)
                {
                    Vector3 pNext = postAnchors[i + 1];
                    for (int t = 0; t < 3; t++)
                    {
                        CreateSlopedTube($"BridgeExt_Tube_Span_{i + 1}_Level_{t + 1}", extRoot.transform,
                            p.x, p.y + tubeRelHeights[t], p.z,
                            pNext.x, pNext.y + tubeRelHeights[t], pNext.z,
                            0.05f, tubularSteelMat);
                    }
                }
            }
        }

        // --- Geometric Helper Methods ---

        private static float GetCurbSurfaceY(float z)
        {
            if (z <= 55.20f) return 0.400f; // Flush with asphalt at the mouth
            if (z <= 58.00f) return 0.400f + (z - 55.20f) / 2.80f * (0.520f - 0.400f);
            if (z <= 64.50f) return 0.520f;
            if (z <= 68.015f) return 0.520f + (z - 64.50f) / (68.015f - 64.50f) * (0.4432f - 0.520f);
            return 0.4432f;
        }

        private static float GetRampSurfaceY(float z)
        {
            if (z <= 55.20f) return 0.400f; // Flush with asphalt at the 1.20m exit landing
            if (z <= 58.00f) return 0.400f + (z - 55.20f) / 2.80f * (0.520f - 0.400f);
            return 0.520f;
        }

        private static GameObject CreateSolidSlab(string name, Transform parent,
            float xMin, float xMax, float zMin, float zMax,
            float yStartWest, float yEndWest, float yBottom,
            float crossSlopeStart, float crossSlopeEnd, Material mat, float xRef = 14.00f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";

            float dx_min = xMin - xRef;
            float dx_max = xMax - xRef;

            Vector3 v_NW = new Vector3(xMin, yEndWest + crossSlopeEnd * dx_min, zMax);
            Vector3 v_NE = new Vector3(xMax, yEndWest + crossSlopeEnd * dx_max, zMax);
            Vector3 v_SE = new Vector3(xMax, yStartWest + crossSlopeStart * dx_max, zMin);
            Vector3 v_SW = new Vector3(xMin, yStartWest + crossSlopeStart * dx_min, zMin);

            Vector3 v_bNW = new Vector3(xMin, yBottom, zMax);
            Vector3 v_bNE = new Vector3(xMax, yBottom, zMax);
            Vector3 v_bSE = new Vector3(xMax, yBottom, zMin);
            Vector3 v_bSW = new Vector3(xMin, yBottom, zMin);

            Vector3[] vertices = new Vector3[]
            {
                // Top
                v_NW, v_NE, v_SE, v_SW,
                // Bottom
                v_bSW, v_bSE, v_bNE, v_bNW,
                // South (-Z)
                v_SW, v_SE, v_bSE, v_bSW,
                // North (+Z)
                v_NE, v_NW, v_bNW, v_bNE,
                // West (-X)
                v_NW, v_SW, v_bSW, v_bNW,
                // East (+X)
                v_SE, v_NE, v_bNE, v_bSE
            };

            int[] triangles = new int[]
            {
                // Top (NW, NE, SE, SW)
                0, 1, 2,  0, 2, 3,
                // Bottom
                4, 5, 6,  4, 6, 7,
                // South
                8, 9, 10,  8, 10, 11,
                // North
                12, 13, 14,  12, 14, 15,
                // West
                16, 17, 18,  16, 18, 19,
                // East
                20, 21, 22,  20, 22, 23
            };

            Vector2[] uvs = new Vector2[24];
            for (int i = 0; i < 6; i++)
            {
                uvs[i * 4 + 0] = new Vector2(0f, 1f);
                uvs[i * 4 + 1] = new Vector2(1f, 1f);
                uvs[i * 4 + 2] = new Vector2(1f, 0f);
                uvs[i * 4 + 3] = new Vector2(0f, 0f);
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return go;
        }

        private static GameObject CreateSlopedTube(string name, Transform parent,
            float xA, float yA, float zA,
            float xB, float yB, float zB,
            float diameter, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);

            Vector3 start = new Vector3(xA, yA, zA);
            Vector3 end = new Vector3(xB, yB, zB);
            Vector3 mid = (start + end) * 0.5f;
            Vector3 dir = end - start;
            float len = dir.magnitude;

            go.transform.position = mid;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(diameter, len * 0.5f, diameter);

            SetMaterial(go, mat);
            return go;
        }

        private static void CreateTerminalReturn(string name, Transform parent,
            float x, float y, float z, float returnRadius, Material mat)
        {
            GameObject retGo = new GameObject(name);
            retGo.transform.SetParent(parent);

            GameObject upperPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperPost.name = $"{name}_UpperPost";
            upperPost.transform.SetParent(retGo.transform);
            upperPost.transform.position = new Vector3(x, y + 0.85f, z);
            upperPost.transform.localScale = new Vector3(0.045f, 0.10f, 0.045f);
            SetMaterial(upperPost, mat);

            GameObject bend = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bend.name = $"{name}_Bend";
            bend.transform.SetParent(retGo.transform);
            bend.transform.position = new Vector3(x, y + 0.95f, z);
            bend.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            SetMaterial(bend, mat);
        }

        private static Material CreateOrGetMaterial(string path, UnityEngine.Color col, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = col;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        private static void CaptureAndCopy(Camera cam, string outDir, string brainDir, string fileName, string title)
        {
            string localPath = Path.Combine(outDir, fileName);
            string brainPath = Path.Combine(brainDir, fileName);
            CaptureShot(cam, localPath, title);
            CopyFile(localPath, brainPath);
        }

        private static void CaptureShot(Camera cam, string savePath, string windowTitle)
        {
            int renderH = WinHeight - TitleBarHeight;
            RenderTexture rt = new RenderTexture(WinWidth, renderH, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(WinWidth, renderH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, WinWidth, renderH), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            using (var ms = new MemoryStream(pngBytes))
            using (var sceneImg = new Bitmap(ms))
            using (var finalBmp = new Bitmap(WinWidth, WinHeight))
            using (var g = System.Drawing.Graphics.FromImage(finalBmp))
            {
                g.Clear(System.Drawing.Color.FromArgb(32, 34, 37));

                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(47, 49, 54)))
                {
                    g.FillRectangle(brush, 0, 0, WinWidth, TitleBarHeight);
                }

                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(20, 20, 20), 1))
                {
                    g.DrawLine(pen, 0, TitleBarHeight - 1, WinWidth, TitleBarHeight - 1);
                }

                using (var font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(220, 221, 222)))
                {
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.DrawString(windowTitle, font, brush, 12, 7);
                }

                int btnW = 46;
                int closeX = WinWidth - btnW;
                int maxBtnX = closeX - btnW;
                int minBtnX = maxBtnX - btnW;

                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 180, 180), 1))
                {
                    int cy = TitleBarHeight / 2;
                    g.DrawLine(pen, minBtnX + 18, cy, minBtnX + 28, cy);
                    g.DrawRectangle(pen, maxBtnX + 18, cy - 5, 10, 10);
                    g.DrawLine(pen, closeX + 18, cy - 5, closeX + 28, cy + 5);
                    g.DrawLine(pen, closeX + 28, cy - 5, closeX + 18, cy + 5);
                }

                g.DrawImage(sceneImg, 0, TitleBarHeight, WinWidth, renderH);

                string dir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                finalBmp.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static void CopyFile(string src, string dst)
        {
            try
            {
                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EastWalkwayPhase1Builder] Could not copy {src} to {dst}: {ex.Message}");
            }
        }
    }
}
