using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.15:
    /// Option B — Northwest Service Staircase inside Remanencia — Nivel 0 (Hotel de las Palomas).
    ///
    /// Constructs a physically traversable 20-riser, 2-flight unfinished poured-concrete
    /// switchback staircase inside the pre-reserved Northwest Landing envelope
    /// (X in [-16.00, -9.60], Z in [+28.80, +34.00], clear 6.16m x 4.96m), rising 3.60m from
    /// lower floor Y = -50.00m via intermediate landing Y = -48.20m to a sealed, unfinished
    /// upper landing shell at Y = -46.40m (ceiling soffit Y = -43.55m, 2.85m clear height).
    ///
    /// Preserves NorthWing_CeilingSlab_North (fileID preserved in-place, trimmed strictly east
    /// of X = -9.60m), adds zero Unity Light components (retains exact 18-light budget), adds
    /// zero runtime MonoBehaviours, and leaves Elevators, Rooms 107-110, Room 111-B, the
    /// Threshold of Trust, entry-only door, and BorderCorridorScene.unity 100% untouched.
    /// </summary>
    public static class NorthwestServiceStaircaseBuilder
    {
        public const string RootName = BackroomsLevel1Builder.NorthwestServiceStaircaseRootName;

        // Authorized Northwest reserve bounds (verified from BackroomsLevel1Scene.unity)
        public const float EnvelopeMinX = -16.00f;
        public const float EnvelopeMaxX = -9.60f;
        public const float EnvelopeMinZ = 28.80f;
        public const float EnvelopeMaxZ = 34.00f;

        public const float ClearMinX = -15.87f;   // Inner East face of NorthWing_OuterWall_West (X=-16.00, t=0.26)
        public const float ClearMaxX = -9.71f;    // Inner West face of Hotel_Room110_WestServiceWall (X=-9.60, t=0.22)
        public const float ClearMinZ = 28.91f;    // Inner North face of NorthWing_Housekeeping_LinenNorthWall (Z=28.80, t=0.22)
        public const float ClearMaxZ = 33.87f;    // Inner South face of NorthWing_OuterWall_North (Z=34.00, t=0.26)

        // Exact elevations & vertical geometry
        public const float LowerFloorY = BackroomsLevel1Builder.BaseY;           // -50.00f
        public const float TotalVerticalRise = 3.60f;
        public const float IntermediateLandingY = LowerFloorY + 1.80f;           // -48.20f
        public const float UpperLandingY = LowerFloorY + TotalVerticalRise;      // -46.40f
        public const float UpperClearHeight = BackroomsLevel1Builder.ClearHeight;// 2.85f
        public const float UpperCeilingSoffitY = UpperLandingY + UpperClearHeight; // -43.55f
        public const float UpperSlabThickness = 0.20f;
        public const float UpperRoofThickness = 0.20f;

        // Exact staircase flight metrics (20 risers, 2 flights x 10 risers / 9 treads)
        public const int FlightsCount = 2;
        public const int RisersPerFlight = 10;
        public const int TreadsPerFlight = 9;
        public const int TotalRisers = FlightsCount * RisersPerFlight;           // 20
        public const int TotalTreads = FlightsCount * TreadsPerFlight;           // 18
        public const float RiserHeight = 0.18f;
        public const float TreadDepth = 0.28f;
        public const float FlightHorizontalRun = TreadsPerFlight * TreadDepth;   // 2.52f
        public const float RampHorizontalSpan = RisersPerFlight * TreadDepth;    // 2.80f (tangent to all 10 step nosings)

        // Horizontal X partitions inside ClearMinX..ClearMaxX (6.16m total):
        //   Intermediate Landing: [-15.87, -14.22] = 1.65m deep (>= 1.45m)
        //   Stair Flights Run:    [-14.22, -11.70] = 2.52m run  (exact 9 x 0.28m)
        //   East Vestibule/Upper: [-11.70,  -9.71] = 1.99m deep (>= 1.45m)
        public const float MidLandingEastX = -14.22f;
        public const float FlightsEastX = -11.70f;
        public const float MidLandingDepth = MidLandingEastX - ClearMinX;        // 1.65f
        public const float UpperLandingDepth = ClearMaxX - FlightsEastX;         // 1.99f

        // Horizontal Z partitions inside ClearMinZ..ClearMaxZ (4.96m total):
        //   Flight 1 (Lower, climbing East -> West): Z in [32.02, 33.78] = 1.76m masonry (1.64m clear inside rails)
        //   Center concrete parapet divider:         Z in [31.82, 32.02] = 0.20m
        //   Flight 2 (Upper, climbing West -> East): Z in [30.06, 31.82] = 1.76m masonry (1.64m clear inside rails)
        //   South lower soffit clearance slot:       Z in [28.91, 30.06] = 1.15m
        public const float Flight1MinZ = 32.02f;
        public const float Flight1MaxZ = 33.78f;
        public const float Flight2MinZ = 30.06f;
        public const float Flight2MaxZ = 31.82f;
        public const float FlightMasonryWidth = 1.76f;
        public const float HandrailInset = 0.06f;
        public const float FlightClearWidth = FlightMasonryWidth - (2f * HandrailInset); // 1.64f (>= 1.45f)
        public const float CenterParapetZ = 31.92f;
        public const float CenterParapetThickness = 0.18f;
        public const float SouthParapetZ = 29.97f;
        public const float SouthParapetThickness = 0.16f;
        public const float ParapetHeight = 1.05f;
        public const float StairWaistThickness = 0.16f;

        // Trimmed NorthWing_CeilingSlab_North bounds (preserving fileID=582163591 east of X = -9.60)
        public const float TrimmedCeilingNorthCenterX = 3.35f;                   // X in [-9.60, +16.30]
        public const float TrimmedCeilingNorthWidthX = 25.90f;

        private struct StairMaterials
        {
            public Material WallConcrete;
            public Material FloorConcrete;
            public Material CeilingSoffit;
            public Material ColumnConcrete;
            public Material BaseboardConcrete;
            public Material IndustrialMetal;
            public Material UtilityWarmOn;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.15 Northwest Service Staircase")]
        public static void MigrateNorthwestServiceStaircaseAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateNorthwestServiceStaircaseInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.15 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[NorthwestServiceStaircaseBuilder] Implementation 07.15 Northwest Service Staircase migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.15.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateNorthwestServiceStaircaseInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"NorthwestServiceStaircaseBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
            }

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            if (root == null)
            {
                throw new InvalidOperationException("BackroomsLevel1_Root not found in BackroomsLevel1Scene.unity.");
            }

            Transform shell = root.transform.Find("Backrooms_Architecture");
            if (shell == null)
            {
                throw new InvalidOperationException("Backrooms_Architecture not found under BackroomsLevel1_Root.");
            }

            Transform m1Root = shell.Find(BackroomsLevel1Builder.NorthWingM1RootName);
            if (m1Root == null)
            {
                throw new InvalidOperationException($"Expected '{BackroomsLevel1Builder.NorthWingM1RootName}' before running 07.15 staircase migration.");
            }

            StairMaterials mats = LoadExistingHotelMaterials();

            // 1. Trim existing NorthWing_CeilingSlab_North in-place (preserving its GameObject/Transform/BoxCollider fileIDs)
            TrimNorthCeilingSlabInPlace(m1Root);

            // 2. Build/update dedicated Hotel_NorthwestServiceStaircase root cleanly and idempotently
            Transform stairRoot = EnsureSubgroup(shell, RootName);

            BuildCeilingOpeningFillAndTrim(EnsureSubgroup(stairRoot, "NWStair_CeilingOpening"), mats);
            BuildFlight1Lower(EnsureSubgroup(stairRoot, "NWStair_Flight1_Lower"), mats);
            BuildIntermediateLanding(EnsureSubgroup(stairRoot, "NWStair_IntermediateLanding"), mats);
            BuildFlight2Upper(EnsureSubgroup(stairRoot, "NWStair_Flight2_Upper"), mats);
            BuildGuardrailsAndHandrails(EnsureSubgroup(stairRoot, "NWStair_GuardrailsAndHandrails"), mats);
            BuildUpperLandingShell(EnsureSubgroup(stairRoot, "NWStair_UpperLandingShell"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform root = architectureShell.Find(RootName);
            if (root == null) return false;
            return root.Find("NWStair_CeilingOpening") != null &&
                   root.Find("NWStair_Flight1_Lower") != null &&
                   root.Find("NWStair_IntermediateLanding") != null &&
                   root.Find("NWStair_Flight2_Upper") != null &&
                   root.Find("NWStair_GuardrailsAndHandrails") != null &&
                   root.Find("NWStair_UpperLandingShell") != null;
        }

        private static StairMaterials LoadExistingHotelMaterials()
        {
            var mats = new StairMaterials
            {
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath),
                FloorConcrete = LoadRequiredMat(BackroomsLevel1Builder.FloorConcreteMatPath),
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                BaseboardConcrete = LoadRequiredMat(BackroomsLevel1Builder.BaseboardMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                UtilityWarmOn = LoadRequiredMat(BackroomsLevel1Builder.UtilityWarmOnMatPath)
            };
            return mats;
        }

        private static Material LoadRequiredMat(string assetPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                throw new InvalidOperationException($"Required existing Remanencia material missing at '{assetPath}'.");
            }
            return mat;
        }

        private static void TrimNorthCeilingSlabInPlace(Transform m1Root)
        {
            Transform ceilingNorth = m1Root.Find("NorthWing_Connection_Lobby/NorthWing_CeilingSlab_North");
            if (ceilingNorth == null)
            {
                throw new InvalidOperationException("NorthWing_CeilingSlab_North not found under Hotel_NorthWing_M1.");
            }

            ceilingNorth.position = new Vector3(
                TrimmedCeilingNorthCenterX,
                BackroomsLevel1Builder.CeilingY + 0.102f,
                30.15f);
            ceilingNorth.rotation = Quaternion.identity;
            ceilingNorth.localScale = new Vector3(TrimmedCeilingNorthWidthX, 0.20f, 8.30f);
        }

        private static void BuildCeilingOpeningFillAndTrim(Transform group, StairMaterials mats)
        {
            float ceilCenterY = BackroomsLevel1Builder.CeilingY + 0.102f;

            // Flush concrete ceiling patch covering strictly Z in [+26.00, +28.91] across X in [-16.30, -9.60]
            // (seals Maintenance Corridor North end and Linen Room overhead without re-covering the NW stairwell).
            EnsureBox(group, "NWStair_CeilingFill_SouthCorridor",
                new Vector3(-12.95f, ceilCenterY, 27.455f), Quaternion.identity,
                new Vector3(6.70f, 0.20f, 2.91f), mats.CeilingSoffit, keepCollider: true);

            // Structural raw-concrete slab reveal curbs framing the exact 6.16m x 4.96m ceiling cutout
            EnsureBox(group, "NWStair_CeilingEdge_South",
                new Vector3(-12.79f, BackroomsLevel1Builder.CeilingY + 0.08f, ClearMinZ), Quaternion.identity,
                new Vector3(6.16f, 0.24f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_CeilingEdge_East",
                new Vector3(ClearMaxX, BackroomsLevel1Builder.CeilingY + 0.08f, 31.39f), Quaternion.identity,
                new Vector3(0.06f, 0.24f, 4.96f), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildFlight1Lower(Transform group, StairMaterials mats)
        {
            float f1CenterZ = (Flight1MinZ + Flight1MaxZ) * 0.5f; // 32.90f

            // Remove obsolete under-mass block if present from an earlier prototype pass
            Transform legacyUnderMass = group.Find("NWStair_F1_UnderMass");
            if (legacyUnderMass != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyUnderMass.gameObject);
            }

            // Visible individual poured-concrete steps 1..9 rising monolithically from LowerFloorY
            // (closed concrete masonry stair mass: zero under-stair cavity, zero poke-through!)
            for (int i = 0; i < TreadsPerFlight; i++)
            {
                float stepTopY = LowerFloorY + (i + 1) * RiserHeight;
                float stepCenterX = FlightsEastX - (i + 0.5f) * TreadDepth;
                float stepH = stepTopY - LowerFloorY;

                EnsureBox(group, $"NWStair_F1_Step_{i + 1:00}",
                    new Vector3(stepCenterX, LowerFloorY + stepH * 0.5f, f1CenterZ), Quaternion.identity,
                    new Vector3(TreadDepth + 0.004f, stepH, FlightMasonryWidth), mats.FloorConcrete, keepCollider: false);

                // Subtle weathered nosing edge strip along front riser top
                EnsureBox(group, $"NWStair_F1_Nosing_{i + 1:00}",
                    new Vector3(FlightsEastX - i * TreadDepth - 0.014f, stepTopY - 0.012f, f1CenterZ), Quaternion.identity,
                    new Vector3(0.032f, 0.026f, FlightMasonryWidth + 0.004f), mats.ColumnConcrete, keepCollider: false);
            }

            // Riser 10 vertical concrete closure face rising from tread 9 (-48.38) to intermediate landing (-48.20)
            EnsureBox(group, "NWStair_F1_Riser_10",
                new Vector3(MidLandingEastX - 0.02f, IntermediateLandingY - RiserHeight * 0.5f, f1CenterZ), Quaternion.identity,
                new Vector3(0.04f, RiserHeight, FlightMasonryWidth), mats.ColumnConcrete, keepCollider: false);

            // Clean side structural stringer trim tucked below Flight 1
            PlaceSlopedRampBox(group, "NWStair_F1_SoffitSlab",
                startPoint: new Vector3(FlightsEastX, LowerFloorY, f1CenterZ),
                endPoint: new Vector3(MidLandingEastX, IntermediateLandingY - RiserHeight, f1CenterZ),
                widthZ: FlightMasonryWidth - 0.01f,
                normalThickness: StairWaistThickness,
                mat: mats.ColumnConcrete,
                keepCollider: false,
                visibleRenderer: true);

            // Continuous concealed stair-support collider tangent to all 10 step nosings (32.735 deg < 45 deg slopeLimit)
            PlaceSlopedRampBox(group, "NWStair_F1_RampCollider",
                startPoint: new Vector3(FlightsEastX + TreadDepth, LowerFloorY, f1CenterZ),
                endPoint: new Vector3(MidLandingEastX, IntermediateLandingY, f1CenterZ),
                widthZ: FlightMasonryWidth,
                normalThickness: StairWaistThickness,
                mat: mats.FloorConcrete,
                keepCollider: true,
                visibleRenderer: false);
        }

        private static void BuildIntermediateLanding(Transform group, StairMaterials mats)
        {
            float midCenterX = (ClearMinX + MidLandingEastX) * 0.5f;  // -15.045f
            float midCenterZ = (ClearMinZ + ClearMaxZ) * 0.5f;        // 31.390f
            float midWidthZ = ClearMaxZ - ClearMinZ;                  // 4.960f

            // Walkable concrete intermediate landing slab (Y_top = -48.200)
            EnsureBox(group, "NWStair_MidLanding_Slab",
                new Vector3(midCenterX, IntermediateLandingY - 0.10f, midCenterZ), Quaternion.identity,
                new Vector3(MidLandingDepth, 0.20f, midWidthZ), mats.FloorConcrete, keepCollider: true);

            // Concrete expansion-joint seam & edge trim
            EnsureBox(group, "NWStair_MidLanding_JointSeam",
                new Vector3(midCenterX, IntermediateLandingY + 0.002f, CenterParapetZ), Quaternion.identity,
                new Vector3(MidLandingDepth - 0.06f, 0.004f, 0.025f), mats.BaseboardConcrete, keepCollider: false);

            // Solid concrete masonry closure wall beneath the entire intermediate landing (X = -14.22, Y = -50.00..-48.20)
            // Completely eliminates the unsafe 1.60m low-headroom under-landing pocket.
            EnsureBox(group, "NWStair_MidLanding_UnderWall",
                new Vector3(MidLandingEastX - 0.10f, LowerFloorY + 0.90f, midCenterZ), Quaternion.identity,
                new Vector3(0.20f, 1.80f, midWidthZ), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_MidLanding_UnderWall_Baseboard",
                new Vector3(MidLandingEastX - 0.10f, LowerFloorY + 0.06f, midCenterZ), Quaternion.identity,
                new Vector3(0.22f, 0.12f, midWidthZ), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildFlight2Upper(Transform group, StairMaterials mats)
        {
            float f2CenterZ = (Flight2MinZ + Flight2MaxZ) * 0.5f; // 30.94f

            // Visible individual poured-concrete steps 1..9 (global risers 11..19, treads 10..18) + riser 20 at upper landing
            for (int i = 0; i < TreadsPerFlight; i++)
            {
                float stepTopY = IntermediateLandingY + (i + 1) * RiserHeight;
                float stepCenterX = MidLandingEastX + (i + 0.5f) * TreadDepth;
                float stepBottomY = stepTopY - 0.185f;
                float stepH = stepTopY - stepBottomY;

                EnsureBox(group, $"NWStair_F2_Step_{i + 1:00}",
                    new Vector3(stepCenterX, stepBottomY + stepH * 0.5f, f2CenterZ), Quaternion.identity,
                    new Vector3(TreadDepth + 0.004f, stepH, FlightMasonryWidth), mats.FloorConcrete, keepCollider: false);

                EnsureBox(group, $"NWStair_F2_Nosing_{i + 1:00}",
                    new Vector3(MidLandingEastX + i * TreadDepth + 0.014f, stepTopY - 0.012f, f2CenterZ), Quaternion.identity,
                    new Vector3(0.032f, 0.026f, FlightMasonryWidth + 0.004f), mats.ColumnConcrete, keepCollider: false);
            }

            // Riser 20 vertical concrete closure face rising from tread 18 (-46.58) to upper landing (-46.40)
            EnsureBox(group, "NWStair_F2_Riser_20",
                new Vector3(FlightsEastX + 0.02f, UpperLandingY - RiserHeight * 0.5f, f2CenterZ), Quaternion.identity,
                new Vector3(0.04f, RiserHeight, FlightMasonryWidth), mats.ColumnConcrete, keepCollider: false);

            // Smooth poured-concrete sloped soffit underside of Flight 2 (clean continuous slab flush with step undersides)
            PlaceSlopedRampBox(group, "NWStair_F2_SoffitSlab",
                startPoint: new Vector3(MidLandingEastX, IntermediateLandingY - 0.02f, f2CenterZ),
                endPoint: new Vector3(FlightsEastX, UpperLandingY - 0.18f, f2CenterZ),
                widthZ: FlightMasonryWidth + 0.01f,
                normalThickness: 0.19f,
                mat: mats.ColumnConcrete,
                keepCollider: false,
                visibleRenderer: true);

            // Continuous concealed stair-support collider tangent to all 10 upper step nosings (32.735 deg)
            PlaceSlopedRampBox(group, "NWStair_F2_RampCollider",
                startPoint: new Vector3(MidLandingEastX - TreadDepth, IntermediateLandingY, f2CenterZ),
                endPoint: new Vector3(FlightsEastX, UpperLandingY, f2CenterZ),
                widthZ: FlightMasonryWidth,
                normalThickness: StairWaistThickness,
                mat: mats.FloorConcrete,
                keepCollider: true,
                visibleRenderer: false);
        }

        private static void BuildGuardrailsAndHandrails(Transform group, StairMaterials mats)
        {
            // 1. Center Concrete Stairwell Spine / Parapet Wall between Flight 1 & Flight 2 (Z = 31.92)
            //    Terminated cleanly into vertical concrete newel posts so zero slanted overhang pokes onto landings.
            PlaceSlopedRampBox(group, "NWStair_CenterParapet_Flight1",
                startPoint: new Vector3(FlightsEastX - 0.06f, LowerFloorY + ParapetHeight + 0.14f, CenterParapetZ),
                endPoint: new Vector3(MidLandingEastX + 0.06f, IntermediateLandingY + ParapetHeight - 0.04f, CenterParapetZ),
                widthZ: CenterParapetThickness,
                normalThickness: 0.96f,
                mat: mats.ColumnConcrete,
                keepCollider: true,
                visibleRenderer: true);

            // Lower Flight 1 vertical concrete start newel at X = -11.74, Z = 31.92 (anchors bottom of Flight 1 cleanly on LowerFloorY)
            EnsureBox(group, "NWStair_CenterNewel_Flight1Start",
                new Vector3(FlightsEastX - 0.04f, LowerFloorY + 0.58f, CenterParapetZ), Quaternion.identity,
                new Vector3(0.22f, 1.16f, CenterParapetThickness + 0.02f), mats.ColumnConcrete, keepCollider: true);

            PlaceSlopedRampBox(group, "NWStair_CenterParapet_Flight2",
                startPoint: new Vector3(MidLandingEastX + 0.04f, IntermediateLandingY + ParapetHeight + 0.04f, CenterParapetZ),
                endPoint: new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight - 0.05f, CenterParapetZ),
                widthZ: CenterParapetThickness,
                normalThickness: 0.98f,
                mat: mats.ColumnConcrete,
                keepCollider: true,
                visibleRenderer: true);

            // Concrete center newel pillars at West turn (MidLanding) and East Upper Landing arrival
            EnsureBox(group, "NWStair_CenterNewel_West",
                new Vector3(MidLandingEastX, IntermediateLandingY + ParapetHeight * 0.5f, CenterParapetZ), Quaternion.identity,
                new Vector3(0.24f, ParapetHeight, CenterParapetThickness + 0.04f), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_CenterNewel_UpperEast",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight * 0.5f, CenterParapetZ), Quaternion.identity,
                new Vector3(0.20f, ParapetHeight, CenterParapetThickness + 0.04f), mats.ColumnConcrete, keepCollider: true);

            // 2. South Sloped Concrete Parapet Wall along Flight 2 (Z = 29.97) — prevents any fall off South edge
            PlaceSlopedRampBox(group, "NWStair_SouthParapet_Flight2",
                startPoint: new Vector3(MidLandingEastX + 0.04f, IntermediateLandingY + ParapetHeight + 0.04f, SouthParapetZ),
                endPoint: new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight - 0.05f, SouthParapetZ),
                widthZ: SouthParapetThickness,
                normalThickness: 0.98f,
                mat: mats.ColumnConcrete,
                keepCollider: true,
                visibleRenderer: true);
            EnsureBox(group, "NWStair_SouthNewel_UpperEast",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight * 0.5f, SouthParapetZ), Quaternion.identity,
                new Vector3(0.20f, ParapetHeight, SouthParapetThickness + 0.04f), mats.ColumnConcrete, keepCollider: true);

            // 3. Intermediate Landing East Edge Guard Parapet south of Flight 2 (X = -14.22, Z in [28.91, 30.06])
            EnsureBox(group, "NWStair_MidLanding_SouthGuardParapet",
                new Vector3(MidLandingEastX - 0.08f, IntermediateLandingY + ParapetHeight * 0.5f, 29.485f), Quaternion.identity,
                new Vector3(0.16f, ParapetHeight, 1.15f), mats.ColumnConcrete, keepCollider: true);

            // 4. Upper Landing West Edge Guard Parapets (X = -11.78, Y in [-46.40, -45.35])
            //    Leaves ONLY Flight 2 entrance (Z in [30.06, 31.82]) open.
            EnsureBox(group, "NWStair_UpperLanding_NorthGuardParapet",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight * 0.5f, 32.845f), Quaternion.identity,
                new Vector3(0.16f, ParapetHeight, 2.05f), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_UpperLanding_SouthGuardParapet",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight * 0.5f, 29.485f), Quaternion.identity,
                new Vector3(0.16f, ParapetHeight, 1.15f), mats.ColumnConcrete, keepCollider: true);

            // 5. Restrained Continuous Dark-Steel Handrails (non-colliding visuals, hotel IndustrialMetal)
            //    Flight 1 North wall rail (Z = 33.78) & Center parapet rail (Z = 32.04)
            PlaceHandrailSpan(group, "NWStair_Handrail_F1_NorthWall",
                new Vector3(FlightsEastX + 0.10f, LowerFloorY + 0.94f, Flight1MaxZ - 0.02f),
                new Vector3(MidLandingEastX - 0.06f, IntermediateLandingY + 0.94f, Flight1MaxZ - 0.02f),
                mats.IndustrialMetal);
            PlaceHandrailSpan(group, "NWStair_Handrail_F1_CenterSide",
                new Vector3(FlightsEastX + 0.10f, LowerFloorY + 1.07f, CenterParapetZ),
                new Vector3(MidLandingEastX, IntermediateLandingY + 1.07f, CenterParapetZ),
                mats.IndustrialMetal);

            //    Intermediate landing West wall rail & South guard cap rail
            PlaceHandrailSpan(group, "NWStair_Handrail_MidLanding_WestWall",
                new Vector3(ClearMinX + 0.07f, IntermediateLandingY + 0.94f, Flight2MinZ + 0.08f),
                new Vector3(ClearMinX + 0.07f, IntermediateLandingY + 0.94f, Flight1MaxZ - 0.08f),
                mats.IndustrialMetal);
            PlaceHandrailSpan(group, "NWStair_Handrail_MidLanding_SouthCap",
                new Vector3(MidLandingEastX - 0.08f, IntermediateLandingY + ParapetHeight + 0.025f, ClearMinZ + 0.04f),
                new Vector3(MidLandingEastX - 0.08f, IntermediateLandingY + ParapetHeight + 0.025f, Flight2MinZ),
                mats.IndustrialMetal);

            //    Flight 2 Center parapet cap rail & South parapet cap rail
            PlaceHandrailSpan(group, "NWStair_Handrail_F2_CenterCap",
                new Vector3(MidLandingEastX - 0.10f, IntermediateLandingY + ParapetHeight + 0.025f, CenterParapetZ),
                new Vector3(FlightsEastX, UpperLandingY + ParapetHeight + 0.025f, CenterParapetZ),
                mats.IndustrialMetal);
            PlaceHandrailSpan(group, "NWStair_Handrail_F2_SouthCap",
                new Vector3(MidLandingEastX - 0.10f, IntermediateLandingY + ParapetHeight + 0.025f, SouthParapetZ),
                new Vector3(FlightsEastX, UpperLandingY + ParapetHeight + 0.025f, SouthParapetZ),
                mats.IndustrialMetal);

            //    Upper landing North & South parapet cap rails
            PlaceHandrailSpan(group, "NWStair_Handrail_UpperLanding_NorthCap",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight + 0.025f, Flight2MaxZ),
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight + 0.025f, ClearMaxZ - 0.04f),
                mats.IndustrialMetal);
            PlaceHandrailSpan(group, "NWStair_Handrail_UpperLanding_SouthCap",
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight + 0.025f, ClearMinZ + 0.04f),
                new Vector3(FlightsEastX - 0.08f, UpperLandingY + ParapetHeight + 0.025f, Flight2MinZ),
                mats.IndustrialMetal);
        }

        private static void BuildUpperLandingShell(Transform group, StairMaterials mats)
        {
            float upperCenterX = (FlightsEastX + ClearMaxX) * 0.5f;   // -10.705f
            float bayCenterZ = (ClearMinZ + ClearMaxZ) * 0.5f;        // 31.390f
            float bayWidthZ = ClearMaxZ - ClearMinZ;                  // 4.960f

            // 1. Walkable concrete Upper Landing floor slab (X in [-11.70, -9.71], Z in [28.91, 33.87], Y_top = -46.40)
            EnsureBox(group, "NWStair_UpperLanding_FloorSlab",
                new Vector3(upperCenterX, UpperLandingY - UpperSlabThickness * 0.5f, bayCenterZ), Quaternion.identity,
                new Vector3(UpperLandingDepth, UpperSlabThickness, bayWidthZ), mats.FloorConcrete, keepCollider: true);

            // Concrete expansion-joint detail on upper landing floor
            EnsureBox(group, "NWStair_UpperLanding_FloorJoint",
                new Vector3(upperCenterX, UpperLandingY + 0.002f, CenterParapetZ), Quaternion.identity,
                new Vector3(UpperLandingDepth - 0.08f, 0.004f, 0.025f), mats.BaseboardConcrete, keepCollider: false);

            // 2. Sealed Upper Stairwell & Landing Perimeter Walls (extending seamlessly from Y = -47.15 to -43.55, height 3.60m)
            float wallBotY = BackroomsLevel1Builder.CeilingY;         // -47.15f
            float wallH = UpperCeilingSoffitY - wallBotY;             // 3.60f
            float wallMidY = wallBotY + wallH * 0.5f;                 // -45.35f

            // When Implementation 07.17 (Hotel_NorthUpperGallery) is installed, preserve the
            // authorized East opening (Z in [30.28, 32.32]) by retaining WallEast & BaseboardEast as the South pier.
            bool hasNorthUpperGallery = group.parent != null &&
                                        group.parent.parent != null &&
                                        group.parent.parent.Find(BackroomsLevel1Builder.NorthUpperGalleryRootName) != null;

            EnsureBox(group, "NWStair_UpperShell_WallWest",
                new Vector3(EnvelopeMinX, wallMidY, 31.40f), Quaternion.identity,
                new Vector3(0.26f, wallH, 5.46f), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_UpperShell_WallEast",
                new Vector3(EnvelopeMaxX, wallMidY, hasNorthUpperGallery ? NorthUpperGalleryBuilder.TrimmedEastWallSouthCenterZ : 31.40f), Quaternion.identity,
                new Vector3(0.22f, wallH, hasNorthUpperGallery ? NorthUpperGalleryBuilder.TrimmedEastWallSouthLengthZ : 5.46f), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_UpperShell_WallNorth",
                new Vector3(-12.80f, wallMidY, EnvelopeMaxZ), Quaternion.identity,
                new Vector3(6.66f, wallH, 0.26f), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NWStair_UpperShell_WallSouth",
                new Vector3(-12.80f, wallMidY, EnvelopeMinZ), Quaternion.identity,
                new Vector3(6.66f, wallH, 0.22f), mats.WallConcrete, keepCollider: true);

            // Upper landing concrete baseboards (non-colliding)
            EnsureBox(group, "NWStair_UpperShell_BaseboardEast",
                new Vector3(ClearMaxX - 0.01f, UpperLandingY + 0.06f, hasNorthUpperGallery ? NorthUpperGalleryBuilder.TrimmedEastBaseboardSouthCenterZ : bayCenterZ), Quaternion.identity,
                new Vector3(0.03f, 0.12f, hasNorthUpperGallery ? NorthUpperGalleryBuilder.TrimmedEastBaseboardSouthLengthZ : bayWidthZ), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_BaseboardNorth",
                new Vector3(upperCenterX, UpperLandingY + 0.06f, ClearMaxZ - 0.01f), Quaternion.identity,
                new Vector3(UpperLandingDepth, 0.12f, 0.03f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_BaseboardSouth",
                new Vector3(upperCenterX, UpperLandingY + 0.06f, ClearMinZ + 0.01f), Quaternion.identity,
                new Vector3(UpperLandingDepth, 0.12f, 0.03f), mats.BaseboardConcrete, keepCollider: false);

            // 3. Solid Sealed Upper Ceiling Slab (Y in [-43.55, -43.35], 2.85m clear height above UpperLandingY)
            EnsureBox(group, "NWStair_UpperShell_CeilingSlab",
                new Vector3(-12.80f, UpperCeilingSoffitY + UpperRoofThickness * 0.5f, 31.40f), Quaternion.identity,
                new Vector3(6.70f, UpperRoofThickness, 5.50f), mats.CeilingSoffit, keepCollider: true);

            // Overhead raw-concrete transfer beam separating stairwell shaft from upper landing bay
            EnsureBox(group, "NWStair_UpperShell_CeilingBeam",
                new Vector3(FlightsEastX, UpperCeilingSoffitY - 0.14f, bayCenterZ), Quaternion.identity,
                new Vector3(0.26f, 0.28f, bayWidthZ), mats.ColumnConcrete, keepCollider: false);

            // 4. Unfinished Sealed Continuation Architecture (non-colliding concrete frames backed by 100% solid walls)
            //    East wall sealed future-corridor recess frame (X = -9.74, Z in [30.20, 32.40])
            EnsureBox(group, "NWStair_UpperShell_SealedEastFrameS",
                new Vector3(ClearMaxX - 0.035f, UpperLandingY + 1.18f, 30.20f), Quaternion.identity,
                new Vector3(0.07f, 2.36f, 0.16f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_SealedEastFrameN",
                new Vector3(ClearMaxX - 0.035f, UpperLandingY + 1.18f, 32.40f), Quaternion.identity,
                new Vector3(0.07f, 2.36f, 0.16f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_SealedEastFrameTop",
                new Vector3(ClearMaxX - 0.035f, UpperLandingY + 2.32f, 31.30f), Quaternion.identity,
                new Vector3(0.07f, 0.16f, 2.36f), mats.ColumnConcrete, keepCollider: false);

            //    North wall sealed continuation relief frame aligned over NorthWing_FutureAnchor_NorthFrame
            EnsureBox(group, "NWStair_UpperShell_SealedNorthFrameW",
                new Vector3(-11.45f, UpperLandingY + 1.18f, ClearMaxZ - 0.035f), Quaternion.identity,
                new Vector3(0.16f, 2.36f, 0.07f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_SealedNorthFrameE",
                new Vector3(-9.95f, UpperLandingY + 1.18f, ClearMaxZ - 0.035f), Quaternion.identity,
                new Vector3(0.16f, 2.36f, 0.07f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_SealedNorthFrameTop",
                new Vector3(-10.70f, UpperLandingY + 2.32f, ClearMaxZ - 0.035f), Quaternion.identity,
                new Vector3(1.66f, 0.16f, 0.07f), mats.ColumnConcrete, keepCollider: false);

            //    Restrained unfinished vertical electrical conduit + capped formwork stub on South-East corner
            EnsureBox(group, "NWStair_UpperShell_ConduitVertical",
                new Vector3(ClearMaxX - 0.04f, UpperLandingY + 1.42f, ClearMinZ + 0.24f), Quaternion.identity,
                new Vector3(0.035f, 2.84f, 0.035f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "NWStair_UpperShell_ConduitBox",
                new Vector3(ClearMaxX - 0.05f, UpperLandingY + 1.55f, ClearMinZ + 0.24f), Quaternion.identity,
                new Vector3(0.07f, 0.18f, 0.14f), mats.IndustrialMetal, keepCollider: false);

            // 5. Restrained Emissive-Only Utility Strip Fixtures (ZERO Unity Light components; preserves exact 18-light budget)
            EnsureBox(group, "NWStair_UpperLanding_StripBackplate",
                new Vector3(upperCenterX, UpperCeilingSoffitY - 0.025f, bayCenterZ), Quaternion.identity,
                new Vector3(0.14f, 0.04f, 1.32f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "NWStair_UpperLanding_StripTube",
                new Vector3(upperCenterX, UpperCeilingSoffitY - 0.055f, bayCenterZ), Quaternion.identity,
                new Vector3(0.06f, 0.035f, 1.24f), mats.UtilityWarmOn, keepCollider: false);

            EnsureBox(group, "NWStair_ShaftWall_StripBackplate",
                new Vector3(ClearMinX + 0.03f, -45.60f, CenterParapetZ), Quaternion.identity,
                new Vector3(0.05f, 0.12f, 1.18f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "NWStair_ShaftWall_StripTube",
                new Vector3(ClearMinX + 0.06f, -45.60f, CenterParapetZ), Quaternion.identity,
                new Vector3(0.03f, 0.05f, 1.10f), mats.UtilityWarmOn, keepCollider: false);
        }

        private static void PlaceSlopedRampBox(
            Transform parent,
            string name,
            Vector3 startPoint,
            Vector3 endPoint,
            float widthZ,
            float normalThickness,
            Material mat,
            bool keepCollider,
            bool visibleRenderer)
        {
            Vector3 delta = endPoint - startPoint;
            float runX = delta.x;
            float riseY = delta.y;
            float length = Mathf.Sqrt(runX * runX + riseY * riseY);
            float angleDeg = Mathf.Atan2(riseY, runX) * Mathf.Rad2Deg;

            // If climbing in -X (runX < 0), flip local X orientation so local +Y always points UPWARD (+Y world)
            Quaternion rot;
            if (runX < 0f)
            {
                float pitch = Mathf.Atan2(riseY, -runX) * Mathf.Rad2Deg;
                rot = Quaternion.Euler(0f, 0f, -pitch);
            }
            else
            {
                rot = Quaternion.Euler(0f, 0f, angleDeg);
            }

            Vector3 upNormal = rot * Vector3.up;
            Vector3 mid = (startPoint + endPoint) * 0.5f - upNormal * (normalThickness * 0.5f);

            GameObject go = EnsureBox(parent, name, mid, rot, new Vector3(length, normalThickness, widthZ), mat, keepCollider);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = visibleRenderer;
            }
        }

        private static void PlaceHandrailSpan(Transform parent, string name, Vector3 a, Vector3 b, Material metalMat)
        {
            Vector3 delta = b - a;
            float len = delta.magnitude;
            if (len < 0.01f) return;

            Vector3 mid = (a + b) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(delta.normalized, Vector3.up);
            EnsureBox(parent, name, mid, rot, new Vector3(0.042f, 0.042f, len), metalMat, keepCollider: false);
        }

        private static Transform EnsureSubgroup(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static GameObject EnsureBox(
            Transform parent,
            string name,
            Vector3 worldPos,
            Quaternion worldRot,
            Vector3 localScale,
            Material mat,
            bool keepCollider)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = localScale;
            go.isStatic = true;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.enabled = true;
            }

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

        /// <summary>
        /// Read-only Editor verification camera utility for Implementation 07.15.
        /// Captures diagnostic views without modifying scene state.
        /// </summary>
        public static void RenderNorthwestStaircaseVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_NWSTAIR_0715_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_NWStair_0715_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempNWStair0715VerifyCam__");
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
                cam.fieldOfView = 64f;
                cam.nearClipPlane = 0.04f;
                cam.farClipPlane = 60f;
                cam.targetTexture = rt;

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    ("verify_0715_01_lower_portal_entry.png",     new Vector3(-10.75f, LowerFloorY + 1.58f, 27.90f), new Vector3(-13.20f, LowerFloorY + 1.95f, 32.50f)),
                    ("verify_0715_02_flight1_ascent.png",         new Vector3(-10.55f, LowerFloorY + 1.56f, 32.90f), new Vector3(-14.80f, IntermediateLandingY + 0.90f, 32.60f)),
                    ("verify_0715_03_midlanding_turn_flight2.png",new Vector3(-15.20f, IntermediateLandingY + 1.56f, 31.10f), new Vector3(-10.60f, UpperLandingY + 0.70f, 30.94f)),
                    ("verify_0715_04_upper_landing_shell.png",    new Vector3(-11.45f, UpperLandingY + 1.56f, 30.94f), new Vector3(-9.75f, UpperLandingY + 1.25f, 32.20f)),
                    ("verify_0715_05_upper_looking_down.png",     new Vector3(-10.30f, UpperLandingY + 1.56f, 32.60f), new Vector3(-14.60f, IntermediateLandingY + 0.50f, 31.40f))
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
                    Debug.Log($"[NorthwestServiceStaircaseBuilder] Rendered 07.15 verification view: {fullFile}");
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
