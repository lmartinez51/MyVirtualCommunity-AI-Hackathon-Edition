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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.23:
    /// Upper Elevator Approach — Alternative B (East-Side Corridor, Compact Transition Vestibule
    /// &amp; Sealed Upper Elevator Waiting Area with Two Visually Equivalent Sealed Bay Placeholders).
    ///
    /// Opens strictly Bay 2 of NUGallery_WallSouth (X in [+6.000, +8.400], 2.400m x 2.240m) between
    /// preserved pilasters PilasterSouth_2 (Xmax=+5.140) and PilasterSouth_3 (Xmin=+9.660), preserving
    /// NUGallery_WallSouth and NUGallery_BaseboardSouth in-place as the Mid segment (X in [+0.060, +6.000]).
    ///
    /// Constructs:
    ///   - East-Side Approach Corridor: X in [+6.000, +8.400], Z in [+11.800, +29.580] (17.780m x 2.400m = 42.672 m²),
    ///     outer West wall at X = +5.780 (+0.480m clear East of NorthWing_Lightwell_SkylightCap Xmax = +5.300).
    ///   - Compact Transition Vestibule: X in [+3.410, +8.400], Z in [+9.400, +11.800] (4.990m x 2.400m = 11.976 m²),
    ///     resolving the 0.200m nominal overlap via full 2.400m North and South portals.
    ///   - Sealed Upper Elevator Waiting Area: X in [+3.410, +6.200], Z in [+4.200, +9.400] (2.790m x 5.200m = 14.508 m²),
    ///     total new walkable area = 69.156 m², continuous level floor Y = -46.400m, ceiling soffit Y = -43.550m (2.850m headroom).
    ///   - Two 100% visually equivalent, physically sealed, non-interactive, unassigned upper elevator placeholders
    ///     aligned strictly with lower-level Bay 1 (Z = +5.750) and Bay 2 (Z = +7.850).
    ///   - Articulated architectural plenum support girders (Y in [-46.940, -46.600], 8mm clear above lower ceilings).
    ///
    /// Leaves Hotel_Elevator_Core (all 35 children), Backrooms_Ceiling, Backrooms_Floor, all 10 Lightwell objects,
    /// 07.15 Staircase, 07.19 Overlook, 07.21 East Overhang, Room 111-B, Threshold of Trust, BorderCorridorScene.unity,
    /// and the exact 18-light budget 100% untouched. Adds ZERO Unity Light components and ZERO runtime scripts.
    /// </summary>
    public static class UpperElevatorApproachBuilder
    {
        public const string RootName = BackroomsLevel1Builder.UpperElevatorApproachRootName;

        // Authorized Bay 2 South opening in NUGallery_WallSouth (between PilasterSouth_2 & PilasterSouth_3)
        public const float PortalMinX = 6.000f;
        public const float PortalMaxX = 8.400f;
        public const float PortalCenterX = (PortalMinX + PortalMaxX) * 0.5f;                  // +7.200f
        public const float PortalClearWidthX = PortalMaxX - PortalMinX;                       //  2.400f
        public const float PortalClearHeightY = 2.240f;                                       // -46.400 -> -44.160
        public const float PortalHeaderBottomY = GalleryFloorY + PortalClearHeightY;          // -44.160f

        // Trimmed NUGallery_WallSouth & BaseboardSouth Mid segments preserved in-place (X in [+0.060, +6.000])
        public const float GalleryWallSouthMidMinX = CourtyardLightwellOverlookBuilder.OpeningMaxX; // +0.060f
        public const float GalleryWallSouthMidMaxX = PortalMinX;                              // +6.000f
        public const float TrimmedSouthWallMidLengthX = GalleryWallSouthMidMaxX - GalleryWallSouthMidMinX; // 5.940f
        public const float TrimmedSouthWallMidCenterX = (GalleryWallSouthMidMinX + GalleryWallSouthMidMaxX) * 0.5f; // +3.030f
        public const float TrimmedSouthBaseboardMidLengthX = TrimmedSouthWallMidLengthX;      // 5.940f
        public const float TrimmedSouthBaseboardMidCenterX = TrimmedSouthWallMidCenterX;      // +3.030f

        // East segment completing split NUGallery_WallSouth (X in [+8.400, +16.130])
        public const float SplitSouthWallEastMinX = PortalMaxX;                               // +8.400f
        public const float SplitSouthWallEastMaxX = CourtyardLightwellOverlookBuilder.GallerySlabMaxX; // +16.130f
        public const float SplitSouthWallEastLengthX = SplitSouthWallEastMaxX - SplitSouthWallEastMinX; // 7.730f
        public const float SplitSouthWallEastCenterX = (SplitSouthWallEastMinX + SplitSouthWallEastMaxX) * 0.5f; // +12.265f
        public const float SplitSouthBaseboardEastMaxX = NorthUpperGalleryBuilder.GalleryClearMaxX; // +15.870f
        public const float SplitSouthBaseboardEastLengthX = SplitSouthBaseboardEastMaxX - SplitSouthWallEastMinX; // 7.470f
        public const float SplitSouthBaseboardEastCenterX = (SplitSouthWallEastMinX + SplitSouthBaseboardEastMaxX) * 0.5f; // +12.135f

        // Common elevations & thicknesses
        public const float GalleryFloorY = NorthUpperGalleryBuilder.GalleryFloorY;            // -46.400f
        public const float SlabThickness = NorthUpperGalleryBuilder.SlabThickness;            //   0.200f
        public const float SlabBottomY = GalleryFloorY - SlabThickness;                       // -46.600f
        public const float CeilingSoffitY = NorthUpperGalleryBuilder.GalleryCeilingSoffitY;   // -43.550f
        public const float CeilingTopY = CeilingSoffitY + SlabThickness;                      // -43.350f
        public const float ClearHeadroomY = CeilingSoffitY - GalleryFloorY;                   //   2.850f
        public const float WallThickness = 0.220f;
        public const float SupportPlenumBottomY = -46.940f;                                   // +0.008m clear above -46.948m lower ceilings

        // Region 1: East-Side Approach Corridor (42.672 m²)
        public const float CorridorMinX = 6.000f;
        public const float CorridorMaxX = 8.400f;
        public const float CorridorCenterX = (CorridorMinX + CorridorMaxX) * 0.5f;            // +7.200f
        public const float CorridorClearWidthX = CorridorMaxX - CorridorMinX;                 //  2.400f
        public const float CorridorMinZ = 11.800f;
        public const float CorridorMaxZ = NorthUpperGalleryBuilder.GalleryClearMinZ - NorthUpperGalleryBuilder.GalleryWallThickness; // +29.580f
        public const float CorridorCenterZ = (CorridorMinZ + CorridorMaxZ) * 0.5f;            // +20.690f
        public const float CorridorLengthZ = CorridorMaxZ - CorridorMinZ;                     // 17.780f
        public const float CorridorOuterMinX = CorridorMinX - WallThickness;                  // +5.780f (+0.480m clear East of SkylightCap +5.300)
        public const float CorridorOuterMaxX = CorridorMaxX + WallThickness;                  // +8.620f

        // Region 2: Compact Transition Vestibule (11.976 m²)
        public const float VestibuleMinX = 3.410f;
        public const float VestibuleMaxX = 8.400f;
        public const float VestibuleCenterX = (VestibuleMinX + VestibuleMaxX) * 0.5f;         // +5.905f
        public const float VestibuleWidthX = VestibuleMaxX - VestibuleMinX;                   //  4.990f
        public const float VestibuleMinZ = 9.400f;
        public const float VestibuleMaxZ = CorridorMinZ;                                      // +11.800f
        public const float VestibuleCenterZ = (VestibuleMinZ + VestibuleMaxZ) * 0.5f;         // +10.600f
        public const float VestibuleDepthZ = VestibuleMaxZ - VestibuleMinZ;                   //  2.400f
        public const float VestibuleOuterMinX = VestibuleMinX - WallThickness;                // +3.190f
        public const float VestibuleOuterMaxX = CorridorOuterMaxX;                            // +8.620f

        // South Portal from Transition Vestibule to Upper Elevator Waiting Area (2.400m clear at Z = +9.400)
        public const float WaitingPortalMinX = 3.650f;
        public const float WaitingPortalMaxX = 6.050f;
        public const float WaitingPortalCenterX = (WaitingPortalMinX + WaitingPortalMaxX) * 0.5f; // +4.850f
        public const float WaitingPortalClearWidthX = WaitingPortalMaxX - WaitingPortalMinX;  //  2.400f
        public const float WaitingPortalMinZ = VestibuleMinZ - WallThickness;                 // +9.180f
        public const float WaitingPortalMaxZ = VestibuleMinZ;                                 // +9.400f
        public const float WaitingPortalCenterZ = (WaitingPortalMinZ + WaitingPortalMaxZ) * 0.5f; // +9.290f

        // Region 3: Sealed Upper Elevator Waiting Area (14.508 m²)
        public const float WaitingMinX = 3.410f;                                              // Aligned with Hotel_Elevator_Core East face (+3.410)
        public const float WaitingMaxX = 6.200f;
        public const float WaitingCenterX = (WaitingMinX + WaitingMaxX) * 0.5f;               // +4.805f
        public const float WaitingDepthX = WaitingMaxX - WaitingMinX;                         //  2.790f
        public const float WaitingMinZ = 4.200f;                                              // Aligned with Hotel_Elevator_Core South face (+4.200)
        public const float WaitingMaxZ = 9.400f;                                              // Aligned with Hotel_Elevator_Core North face (+9.400)
        public const float WaitingCenterZ = (WaitingMinZ + WaitingMaxZ) * 0.5f;               // +6.800f
        public const float WaitingWidthZ = WaitingMaxZ - WaitingMinZ;                         //  5.200f
        public const float WaitingOuterMinX = 3.150f;                                         // Aligned with Hotel_ElevatorCore_FrontPier* min X (+3.150)
        public const float WaitingOuterMaxX = WaitingMaxX + WallThickness;                    // +6.420f
        public const float WaitingOuterMinZ = WaitingMinZ - WallThickness;                    // +3.980f

        // Exact lower-level Bay 1 & Bay 2 Z centers (both upper placeholders strictly equivalent & sealed)
        public const float Bay1CenterZ = 5.750f;
        public const float Bay2CenterZ = 7.850f;
        public const float PlaceholderClearWidthZ = 1.370f;
        public const float PlaceholderClearHeightY = 2.200f;                                  // -46.400 -> -44.200 (mirrors lower 2.200m bays)
        public const float PlaceholderHeaderBottomY = GalleryFloorY + PlaceholderClearHeightY; // -44.200f

        private struct ApproachMaterials
        {
            public Material WallConcrete;
            public Material PlasterWall;
            public Material FloorConcrete;
            public Material CeilingSoffit;
            public Material ColumnConcrete;
            public Material BaseboardConcrete;
            public Material IndustrialMetal;
            public Material UtilityWarmOn;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.23 Upper Elevator Approach")]
        public static void MigrateUpperElevatorApproachAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateUpperElevatorApproachInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.23 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UpperElevatorApproachBuilder] Implementation 07.23 Upper Elevator Approach migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.23.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateUpperElevatorApproachInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UpperElevatorApproachBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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

            Transform galleryRoot = shell.Find(NorthUpperGalleryBuilder.RootName);
            if (galleryRoot == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{NorthUpperGalleryBuilder.RootName}' before running 07.23 Upper Elevator Approach migration.");
            }

            Transform overlookRoot = shell.Find(CourtyardLightwellOverlookBuilder.RootName);
            if (overlookRoot == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{CourtyardLightwellOverlookBuilder.RootName}' before running 07.23 Upper Elevator Approach migration.");
            }

            ApproachMaterials mats = LoadExistingMaterials();

            // 1. Open strictly Bay 2 (X in [+6.000, +8.400]) of NUGallery_WallSouth in-place,
            //    preserving NUGallery_WallSouth & BaseboardSouth fileIDs as the Mid segment [+0.060, +6.000].
            TrimGallerySouthWallForApproachInPlace(galleryRoot);

            // 2. Build/update dedicated Hotel_UpperElevatorApproach root cleanly and idempotently
            Transform approachRoot = EnsureSubgroup(shell, RootName);

            BuildGalleryPortal(EnsureSubgroup(approachRoot, "UpperElevatorApproach_GalleryPortal"), mats);
            BuildEastCorridor(EnsureSubgroup(approachRoot, "UpperElevatorApproach_EastCorridor"), mats);
            BuildTransitionVestibule(EnsureSubgroup(approachRoot, "UpperElevatorApproach_TransitionVestibule"), mats);
            BuildWaitingArea(EnsureSubgroup(approachRoot, "UpperElevatorApproach_WaitingArea"), mats);
            BuildSealedBayFrames(EnsureSubgroup(approachRoot, "UpperElevatorApproach_SealedBayFrames"), mats);
            BuildSupportStructure(EnsureSubgroup(approachRoot, "UpperElevatorApproach_SupportStructure"), mats);
            BuildLightingDetails(EnsureSubgroup(approachRoot, "UpperElevatorApproach_LightingDetails"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform root = architectureShell.Find(RootName);
            if (root == null) return false;
            return root.Find("UpperElevatorApproach_GalleryPortal") != null &&
                   root.Find("UpperElevatorApproach_EastCorridor") != null &&
                   root.Find("UpperElevatorApproach_TransitionVestibule") != null &&
                   root.Find("UpperElevatorApproach_WaitingArea") != null &&
                   root.Find("UpperElevatorApproach_SealedBayFrames") != null &&
                   root.Find("UpperElevatorApproach_SupportStructure") != null &&
                   root.Find("UpperElevatorApproach_LightingDetails") != null;
        }

        private static ApproachMaterials LoadExistingMaterials()
        {
            return new ApproachMaterials
            {
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath),
                PlasterWall = LoadRequiredMat(BackroomsLevel1Builder.PlasterWallMatPath),
                FloorConcrete = LoadRequiredMat(BackroomsLevel1Builder.FloorConcreteMatPath),
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                BaseboardConcrete = LoadRequiredMat(BackroomsLevel1Builder.BaseboardMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                UtilityWarmOn = LoadRequiredMat(BackroomsLevel1Builder.UtilityWarmOnMatPath)
            };
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

        private static void TrimGallerySouthWallForApproachInPlace(Transform galleryRoot)
        {
            Transform structure = galleryRoot.Find("NUGallery_Structure");
            if (structure == null)
            {
                throw new InvalidOperationException("NUGallery_Structure not found under Hotel_NorthUpperGallery.");
            }

            Transform wallSouth = structure.Find("NUGallery_WallSouth");
            if (wallSouth == null)
            {
                throw new InvalidOperationException("NUGallery_WallSouth not found under NUGallery_Structure.");
            }

            float wallH = CeilingSoffitY - SlabBottomY; // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f; // -45.075f

            // Retain NUGallery_WallSouth GameObject/fileID in-place as Mid segment X in [+0.060, +6.000]
            wallSouth.position = new Vector3(TrimmedSouthWallMidCenterX, wallMidY, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ);
            wallSouth.rotation = Quaternion.identity;
            wallSouth.localScale = new Vector3(TrimmedSouthWallMidLengthX, wallH, NorthUpperGalleryBuilder.GalleryWallThickness);

            Transform baseSouth = structure.Find("NUGallery_BaseboardSouth");
            if (baseSouth != null)
            {
                baseSouth.position = new Vector3(TrimmedSouthBaseboardMidCenterX, GalleryFloorY + 0.06f, NorthUpperGalleryBuilder.GalleryClearMinZ + 0.012f);
                baseSouth.rotation = Quaternion.identity;
                baseSouth.localScale = new Vector3(TrimmedSouthBaseboardMidLengthX, 0.12f, 0.025f);
            }
        }

        private static void BuildGalleryPortal(Transform group, ApproachMaterials mats)
        {
            float wallH = CeilingSoffitY - SlabBottomY; // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f; // -45.075f
            float wallCenterZ = NorthUpperGalleryBuilder.GalleryWallSouthCenterZ; // +29.690f
            float wallThickZ = NorthUpperGalleryBuilder.GalleryWallThickness;     //   0.220f

            // East segment completing split NUGallery_WallSouth (X in [+8.400, +16.130])
            EnsureBox(group, "UpperElev_WallSouth_EastSegment",
                new Vector3(SplitSouthWallEastCenterX, wallMidY, wallCenterZ), Quaternion.identity,
                new Vector3(SplitSouthWallEastLengthX, wallH, wallThickZ), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_BaseboardSouth_EastSegment",
                new Vector3(SplitSouthBaseboardEastCenterX, GalleryFloorY + 0.06f, NorthUpperGalleryBuilder.GalleryClearMinZ + 0.012f), Quaternion.identity,
                new Vector3(SplitSouthBaseboardEastLengthX, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);

            // Solid concrete transom above 2.240m portal (Y in [-44.160, -43.550]) & subfloor kick (Y in [-46.600, -46.400])
            float transomH = CeilingSoffitY - PortalHeaderBottomY; // 0.610f
            EnsureBox(group, "UpperElev_GalleryPortal_Transom",
                new Vector3(PortalCenterX, PortalHeaderBottomY + transomH * 0.5f, wallCenterZ), Quaternion.identity,
                new Vector3(PortalClearWidthX, transomH, wallThickZ), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_GalleryPortal_SubfloorKick",
                new Vector3(PortalCenterX, SlabBottomY + SlabThickness * 0.5f, wallCenterZ), Quaternion.identity,
                new Vector3(PortalClearWidthX, SlabThickness, wallThickZ), mats.WallConcrete, keepCollider: true);

            // Restrained architectural concrete portal reveals & flush metal threshold seam plate (non-colliding; proud 5mm to prevent Z-fighting)
            float revealH = PortalClearHeightY - 0.004f; // 2.236f
            float revealMidY = GalleryFloorY + 0.002f + revealH * 0.5f;
            EnsureBox(group, "UpperElev_GalleryPortal_RevealW",
                new Vector3(PortalMinX - 0.015f, revealMidY, wallCenterZ), Quaternion.identity,
                new Vector3(0.040f, revealH, wallThickZ + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_GalleryPortal_RevealE",
                new Vector3(PortalMaxX + 0.015f, revealMidY, wallCenterZ), Quaternion.identity,
                new Vector3(0.040f, revealH, wallThickZ + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_GalleryPortal_RevealTop",
                new Vector3(PortalCenterX, PortalHeaderBottomY + 0.020f, wallCenterZ), Quaternion.identity,
                new Vector3(PortalClearWidthX + 0.070f, 0.050f, wallThickZ + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_GalleryPortal_ThresholdPlate",
                new Vector3(PortalCenterX, GalleryFloorY + 0.002f, wallCenterZ), Quaternion.identity,
                new Vector3(PortalClearWidthX - 0.008f, 0.004f, wallThickZ + 0.040f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void BuildEastCorridor(Transform group, ApproachMaterials mats)
        {
            float slabWidthX = CorridorOuterMaxX - CorridorOuterMinX; // 2.840f
            float wallH = CeilingSoffitY - SlabBottomY;               // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f;              // -45.075f

            // Continuous unbroken level concrete floor slab & roof ceiling slab (Z in [+11.800, +29.580])
            EnsureBox(group, "UpperElev_EastCorr_FloorSlab",
                new Vector3(CorridorCenterX, SlabBottomY + SlabThickness * 0.5f, CorridorCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, CorridorLengthZ), mats.FloorConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_EastCorr_CeilingSlab",
                new Vector3(CorridorCenterX, CeilingSoffitY + SlabThickness * 0.5f, CorridorCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, CorridorLengthZ), mats.CeilingSoffit, keepCollider: true);

            // Solid enclosing concrete sidewalls (West outer face at X = +5.780 -> +0.480m clear East of SkylightCap +5.300)
            EnsureBox(group, "UpperElev_EastCorr_WallWest",
                new Vector3(CorridorMinX - WallThickness * 0.5f, wallMidY, CorridorCenterZ), Quaternion.identity,
                new Vector3(WallThickness, wallH, CorridorLengthZ), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_EastCorr_WallEast",
                new Vector3(CorridorMaxX + WallThickness * 0.5f, wallMidY, CorridorCenterZ), Quaternion.identity,
                new Vector3(WallThickness, wallH, CorridorLengthZ), mats.WallConcrete, keepCollider: true);

            // Baseboards (non-colliding)
            EnsureBox(group, "UpperElev_EastCorr_BaseboardW",
                new Vector3(CorridorMinX + 0.012f, GalleryFloorY + 0.060f, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.024f, 0.120f, CorridorLengthZ - 0.020f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_EastCorr_BaseboardE",
                new Vector3(CorridorMaxX - 0.012f, GalleryFloorY + 0.060f, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.024f, 0.120f, CorridorLengthZ - 0.020f), mats.BaseboardConcrete, keepCollider: false);

            // Rhythmic concrete structural pilasters & overhead cross-beams aligned over Nivel 0's East Arcade beams
            float[] bayZs = { 26.00f, 22.00f, 18.00f, 14.80f };
            for (int i = 0; i < bayZs.Length; i++)
            {
                float bz = bayZs[i];
                EnsureBox(group, $"UpperElev_EastCorr_PilasterW_{i}",
                    new Vector3(CorridorMinX + 0.040f, GalleryFloorY + ClearHeadroomY * 0.5f, bz), Quaternion.identity,
                    new Vector3(0.080f, ClearHeadroomY - 0.004f, 0.260f), mats.ColumnConcrete, keepCollider: false);
                EnsureBox(group, $"UpperElev_EastCorr_PilasterE_{i}",
                    new Vector3(CorridorMaxX - 0.040f, GalleryFloorY + ClearHeadroomY * 0.5f, bz), Quaternion.identity,
                    new Vector3(0.080f, ClearHeadroomY - 0.004f, 0.260f), mats.ColumnConcrete, keepCollider: false);
                EnsureBox(group, $"UpperElev_EastCorr_CeilingBeam_{i}",
                    new Vector3(CorridorCenterX, CeilingSoffitY - 0.122f, bz), Quaternion.identity,
                    new Vector3(CorridorClearWidthX - 0.004f, 0.240f, 0.260f), mats.ColumnConcrete, keepCollider: false);
            }

            // Subtle longitudinal concrete floor control seam & exposed overhead service conduit (non-colliding)
            EnsureBox(group, "UpperElev_EastCorr_FloorJointSeam",
                new Vector3(CorridorCenterX, GalleryFloorY + 0.002f, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.024f, 0.004f, CorridorLengthZ - 0.200f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_EastCorr_OverheadConduit",
                new Vector3(CorridorMaxX - 0.130f, CeilingSoffitY - 0.110f, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.038f, 0.038f, CorridorLengthZ - 0.300f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void BuildTransitionVestibule(Transform group, ApproachMaterials mats)
        {
            float slabWidthX = VestibuleOuterMaxX - VestibuleOuterMinX;     // 5.430f
            float slabCenterX = (VestibuleOuterMinX + VestibuleOuterMaxX) * 0.5f; // +5.905f
            float wallH = CeilingSoffitY - SlabBottomY;                     // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f;                    // -45.075f

            // Continuous level floor & ceiling slabs spanning X in [+3.190, +8.620], Z in [+9.400, +11.800]
            EnsureBox(group, "UpperElev_Vestibule_FloorSlab",
                new Vector3(slabCenterX, SlabBottomY + SlabThickness * 0.5f, VestibuleCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, VestibuleDepthZ), mats.FloorConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_Vestibule_CeilingSlab",
                new Vector3(slabCenterX, CeilingSoffitY + SlabThickness * 0.5f, VestibuleCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, VestibuleDepthZ), mats.CeilingSoffit, keepCollider: true);

            // North wall sealing strictly West of the 2.400m corridor entrance (X in [+3.190, +6.000], Z in [+11.800, +12.020])
            float northWallLenX = CorridorMinX - VestibuleOuterMinX; // 2.810f
            EnsureBox(group, "UpperElev_Vestibule_WallNorth",
                new Vector3(VestibuleOuterMinX + northWallLenX * 0.5f, wallMidY, VestibuleMaxZ + WallThickness * 0.5f), Quaternion.identity,
                new Vector3(northWallLenX, wallH, WallThickness), mats.WallConcrete, keepCollider: true);

            // East wall continuing flush from East corridor wall (X in [+8.400, +8.620], Z in [+9.180, +11.800])
            float eastWallLenZ = VestibuleMaxZ - WaitingPortalMinZ; // 2.620f
            EnsureBox(group, "UpperElev_Vestibule_WallEast",
                new Vector3(VestibuleMaxX + WallThickness * 0.5f, wallMidY, WaitingPortalMinZ + eastWallLenZ * 0.5f), Quaternion.identity,
                new Vector3(WallThickness, wallH, eastWallLenZ), mats.WallConcrete, keepCollider: true);

            // West wall aligned cleanly with Hotel_Elevator_Core front plane (X in [+3.190, +3.410], Z in [+9.400, +11.800])
            EnsureBox(group, "UpperElev_Vestibule_WallWest",
                new Vector3(VestibuleMinX - WallThickness * 0.5f, wallMidY, VestibuleCenterZ), Quaternion.identity,
                new Vector3(WallThickness, wallH, VestibuleDepthZ), mats.WallConcrete, keepCollider: true);

            // South wall sealing East of the 2.400m Waiting Area portal (X in [+6.050, +8.400], Z in [+9.180, +9.400])
            float southEastWallLenX = VestibuleMaxX - WaitingPortalMaxX; // 2.350f
            EnsureBox(group, "UpperElev_Vestibule_WallSouthEast",
                new Vector3(WaitingPortalMaxX + southEastWallLenX * 0.5f, wallMidY, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(southEastWallLenX, wallH, WallThickness), mats.WallConcrete, keepCollider: true);

            // Short West portal pier flanking the 2.400m South portal (X in [+3.410, +3.650], Z in [+9.180, +9.400])
            float southWestPierLenX = WaitingPortalMinX - VestibuleMinX; // 0.240f
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_PierW",
                new Vector3(VestibuleMinX + southWestPierLenX * 0.5f, wallMidY, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(southWestPierLenX, wallH, WallThickness), mats.ColumnConcrete, keepCollider: true);

            // Solid transom & subfloor kick framing the 2.400m x 2.240m South portal (X in [+3.650, +6.050], Z in [+9.180, +9.400])
            float transomH = CeilingSoffitY - PortalHeaderBottomY; // 0.610f
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_Transom",
                new Vector3(WaitingPortalCenterX, PortalHeaderBottomY + transomH * 0.5f, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(WaitingPortalClearWidthX, transomH, WallThickness), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_SubfloorKick",
                new Vector3(WaitingPortalCenterX, SlabBottomY + SlabThickness * 0.5f, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(WaitingPortalClearWidthX, SlabThickness, WallThickness), mats.WallConcrete, keepCollider: true);

            // Subtle Connection A alignment header beam & flush dark-steel floor reveal at North vestibule entrance (Z = +11.800)
            EnsureBox(group, "UpperElev_Vestibule_NorthHeaderBeam",
                new Vector3(CorridorCenterX, CeilingSoffitY - 0.132f, VestibuleMaxZ), Quaternion.identity,
                new Vector3(CorridorClearWidthX - 0.004f, 0.260f, 0.240f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_NorthSeamPlate",
                new Vector3(CorridorCenterX, GalleryFloorY + 0.002f, VestibuleMaxZ), Quaternion.identity,
                new Vector3(CorridorClearWidthX - 0.012f, 0.004f, 0.140f), mats.IndustrialMetal, keepCollider: false);

            // Architectural framing reveals & flush threshold plate on the South portal into Waiting Area (non-colliding)
            float revealH = PortalClearHeightY - 0.004f;
            float revealMidY = GalleryFloorY + 0.002f + revealH * 0.5f;
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_RevealW",
                new Vector3(WaitingPortalMinX - 0.015f, revealMidY, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(0.040f, revealH, WallThickness + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_RevealE",
                new Vector3(WaitingPortalMaxX + 0.015f, revealMidY, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(0.040f, revealH, WallThickness + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_RevealTop",
                new Vector3(WaitingPortalCenterX, PortalHeaderBottomY + 0.020f, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(WaitingPortalClearWidthX + 0.070f, 0.050f, WallThickness + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_SouthPortal_ThresholdPlate",
                new Vector3(WaitingPortalCenterX, GalleryFloorY + 0.002f, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(WaitingPortalClearWidthX - 0.008f, 0.004f, WallThickness + 0.040f), mats.IndustrialMetal, keepCollider: false);

            // Baseboards & subtle overhead transfer beam framing the Westward turn (non-colliding)
            EnsureBox(group, "UpperElev_Vestibule_BaseboardN",
                new Vector3((VestibuleMinX + CorridorMinX) * 0.5f, GalleryFloorY + 0.060f, VestibuleMaxZ - 0.012f), Quaternion.identity,
                new Vector3(CorridorMinX - VestibuleMinX - 0.020f, 0.120f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_BaseboardE",
                new Vector3(VestibuleMaxX - 0.012f, GalleryFloorY + 0.060f, VestibuleCenterZ), Quaternion.identity,
                new Vector3(0.024f, 0.120f, VestibuleDepthZ - 0.020f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_BaseboardW",
                new Vector3(VestibuleMinX + 0.012f, GalleryFloorY + 0.060f, VestibuleCenterZ), Quaternion.identity,
                new Vector3(0.024f, 0.120f, VestibuleDepthZ - 0.020f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_BaseboardSE",
                new Vector3((WaitingPortalMaxX + VestibuleMaxX) * 0.5f, GalleryFloorY + 0.060f, VestibuleMinZ + 0.012f), Quaternion.identity,
                new Vector3(VestibuleMaxX - WaitingPortalMaxX - 0.020f, 0.120f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Vestibule_CeilingBeamMid",
                new Vector3(CorridorMinX, CeilingSoffitY - 0.122f, VestibuleCenterZ), Quaternion.identity,
                new Vector3(0.240f, 0.240f, VestibuleDepthZ - 0.004f), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildWaitingArea(Transform group, ApproachMaterials mats)
        {
            float slabWidthX = WaitingOuterMaxX - WaitingOuterMinX; // 3.270f
            float slabCenterX = (WaitingOuterMinX + WaitingOuterMaxX) * 0.5f; // +4.785f
            float slabLenZ = WaitingMaxZ - WaitingOuterMinZ;        // 5.420f
            float slabCenterZ = (WaitingOuterMinZ + WaitingMaxZ) * 0.5f; // +6.690f
            float wallH = CeilingSoffitY - SlabBottomY;             // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f;            // -45.075f

            // Continuous unbroken level floor & preferred full-headroom ceiling slab (Y = -43.550m, 2.850m clear)
            EnsureBox(group, "UpperElev_Waiting_FloorSlab",
                new Vector3(slabCenterX, SlabBottomY + SlabThickness * 0.5f, slabCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, slabLenZ), mats.FloorConcrete, keepCollider: true);

            EnsureBox(group, "UpperElev_Waiting_CeilingSlab",
                new Vector3(slabCenterX, CeilingSoffitY + SlabThickness * 0.5f, slabCenterZ), Quaternion.identity,
                new Vector3(slabWidthX, SlabThickness, slabLenZ), mats.CeilingSoffit, keepCollider: true);

            // Older, quieter weathered plaster East & South enclosure walls framed by concrete corner columns
            float eastWallLenZ = WaitingPortalMinZ - WaitingOuterMinZ; // 5.200f
            EnsureBox(group, "UpperElev_Waiting_WallEast",
                new Vector3(WaitingMaxX + WallThickness * 0.5f, wallMidY, WaitingOuterMinZ + eastWallLenZ * 0.5f), Quaternion.identity,
                new Vector3(WallThickness, wallH, eastWallLenZ), mats.PlasterWall, keepCollider: true);

            EnsureBox(group, "UpperElev_Waiting_WallSouth",
                new Vector3(slabCenterX, wallMidY, WaitingMinZ - WallThickness * 0.5f), Quaternion.identity,
                new Vector3(slabWidthX, wallH, WallThickness), mats.PlasterWall, keepCollider: true);

            // West Elevator Core Upper Facade: continuous 100% solid concrete backing (X in [+3.150, +3.350])
            // aligned vertically above Nivel 0's Hotel_ElevatorCore_FrontPier* (zero shaft cutout, zero divider!)
            EnsureBox(group, "UpperElev_Waiting_WestCoreBackingWall",
                new Vector3(3.250f, wallMidY, WaitingCenterZ), Quaternion.identity,
                new Vector3(0.200f, wallH, WaitingWidthZ), mats.ColumnConcrete, keepCollider: true);

            // Three concrete facade piers & header proud to X = +3.410 (matching lower core piers S / Mid / N)
            // creating two symmetrical 0.060m-deep architectural bay recesses at Bay 1 (Z=+5.75) & Bay 2 (Z=+7.85)
            const float pierMinX = 3.350f;
            const float pierMaxX = WaitingMinX; // +3.410f
            const float pierCenterX = (pierMinX + pierMaxX) * 0.5f; // +3.380f
            const float pierDepthX = pierMaxX - pierMinX;           //  0.060f

            EnsureBox(group, "UpperElev_Waiting_CorePierS",
                new Vector3(pierCenterX, wallMidY, (WaitingMinZ + 5.065f) * 0.5f), Quaternion.identity,
                new Vector3(pierDepthX, wallH, 5.065f - WaitingMinZ), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "UpperElev_Waiting_CorePierMid",
                new Vector3(pierCenterX, wallMidY, WaitingCenterZ), Quaternion.identity,
                new Vector3(pierDepthX, wallH, 7.165f - 6.435f), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "UpperElev_Waiting_CorePierN",
                new Vector3(pierCenterX, wallMidY, (8.535f + WaitingMaxZ) * 0.5f), Quaternion.identity,
                new Vector3(pierDepthX, wallH, WaitingMaxZ - 8.535f), mats.ColumnConcrete, keepCollider: true);

            float coreHeaderH = CeilingSoffitY - PlaceholderHeaderBottomY; // 0.650f
            EnsureBox(group, "UpperElev_Waiting_CoreHeader",
                new Vector3(pierCenterX, PlaceholderHeaderBottomY + coreHeaderH * 0.5f, WaitingCenterZ), Quaternion.identity,
                new Vector3(pierDepthX, coreHeaderH, WaitingWidthZ), mats.ColumnConcrete, keepCollider: true);

            // Concrete corner columns framing the weathered plaster East & South walls + centered overhead ceiling beam
            EnsureBox(group, "UpperElev_Waiting_CornerColumnSE",
                new Vector3(WaitingMaxX - 0.050f, GalleryFloorY + ClearHeadroomY * 0.5f, WaitingMinZ + 0.050f), Quaternion.identity,
                new Vector3(0.100f, ClearHeadroomY - 0.004f, 0.100f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Waiting_CornerColumnNE",
                new Vector3(WaitingMaxX - 0.050f, GalleryFloorY + ClearHeadroomY * 0.5f, WaitingPortalMinZ - 0.050f), Quaternion.identity,
                new Vector3(0.100f, ClearHeadroomY - 0.004f, 0.100f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Waiting_CeilingBeamMid",
                new Vector3(WaitingCenterX, CeilingSoffitY - 0.122f, WaitingCenterZ), Quaternion.identity,
                new Vector3(WaitingDepthX - 0.004f, 0.240f, 0.260f), mats.ColumnConcrete, keepCollider: false);

            // Baseboards along plaster East & South walls + subtle floor seam aligned to core centerline Z = +6.800
            EnsureBox(group, "UpperElev_Waiting_BaseboardE",
                new Vector3(WaitingMaxX - 0.012f, GalleryFloorY + 0.060f, (WaitingMinZ + WaitingPortalMinZ) * 0.5f), Quaternion.identity,
                new Vector3(0.024f, 0.120f, (WaitingPortalMinZ - WaitingMinZ) - 0.120f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Waiting_BaseboardS",
                new Vector3(WaitingCenterX, GalleryFloorY + 0.060f, WaitingMinZ + 0.012f), Quaternion.identity,
                new Vector3(WaitingDepthX - 0.120f, 0.120f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "UpperElev_Waiting_FloorCenterSeam",
                new Vector3(WaitingCenterX, GalleryFloorY + 0.002f, WaitingCenterZ), Quaternion.identity,
                new Vector3(WaitingDepthX - 0.240f, 0.004f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildSealedBayFrames(Transform group, ApproachMaterials mats)
        {
            // When Implementation 07.24 (UpperElevatorApproach_BayArchitecture) is installed, preserve
            // the sealed backing panels in-place without recreating superseded uniform 07.23 trim strips.
            if (group.parent != null && group.parent.Find(UpperElevatorBayArchitectureBuilder.GroupName) != null)
            {
                return;
            }

            // Strictly identical helper invoked for Bay 1 (South, Z = +5.750) and Bay 2 (North, Z = +7.850).
            // Both bays are 100% visually equivalent, physically sealed, non-interactive, and unassigned.
            BuildIdenticalSealedPlaceholder(group, "UpperElev_SealedBay_1", Bay1CenterZ, mats);
            BuildIdenticalSealedPlaceholder(group, "UpperElev_SealedBay_2", Bay2CenterZ, mats);
        }

        private static void BuildIdenticalSealedPlaceholder(Transform group, string prefix, float centerZ, ApproachMaterials mats)
        {
            float halfW = PlaceholderClearWidthZ * 0.5f; // 0.685f
            float bayMidY = GalleryFloorY + PlaceholderClearHeightY * 0.5f; // -45.300f

            // Solid sealed masonry infill panel recessed inside the 0.060m concrete bay niche (X in [+3.350, +3.382])
            EnsureBox(group, $"{prefix}_SealedBackingPanel",
                new Vector3(3.366f, bayMidY, centerZ), Quaternion.identity,
                new Vector3(0.032f, PlaceholderClearHeightY, PlaceholderClearWidthZ), mats.WallConcrete, keepCollider: true);

            // Restrained oxidized-steel perimeter jambs & header framing the sealed bay (non-functional, zero buttons/indicators/signage)
            EnsureBox(group, $"{prefix}_FrameJambS",
                new Vector3(3.392f, bayMidY + 0.020f, centerZ - halfW - 0.030f), Quaternion.identity,
                new Vector3(0.084f, PlaceholderClearHeightY + 0.040f, 0.060f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, $"{prefix}_FrameJambN",
                new Vector3(3.392f, bayMidY + 0.020f, centerZ + halfW + 0.030f), Quaternion.identity,
                new Vector3(0.084f, PlaceholderClearHeightY + 0.040f, 0.060f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, $"{prefix}_FrameHeader",
                new Vector3(3.392f, PlaceholderHeaderBottomY + 0.040f, centerZ), Quaternion.identity,
                new Vector3(0.084f, 0.080f, PlaceholderClearWidthZ + 0.120f), mats.IndustrialMetal, keepCollider: true);

            // Flush steel sill threshold & subtle vertical construction seam (non-colliding; identical on both bays)
            EnsureBox(group, $"{prefix}_FloorSill",
                new Vector3(3.400f, GalleryFloorY + 0.007f, centerZ), Quaternion.identity,
                new Vector3(0.100f, 0.014f, PlaceholderClearWidthZ + 0.100f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, $"{prefix}_RecessSeamVert",
                new Vector3(3.386f, bayMidY, centerZ), Quaternion.identity,
                new Vector3(0.008f, PlaceholderClearHeightY - 0.040f, 0.022f), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildSupportStructure(Transform group, ApproachMaterials mats)
        {
            // Articulated architectural concrete transfer girders tucked cleanly inside the verified +0.348m..+0.350m plenum
            // (Y in [-46.940, -46.600], 0.340m deep -> +0.008m clear above lower ceiling slabs, ZERO lower-level edits).
            // Architectural load-path representation only (not civil-engineering structural certification).
            float girderH = SlabBottomY - SupportPlenumBottomY;           // 0.340f
            float girderMidY = SupportPlenumBottomY + girderH * 0.5f;     // -46.770f

            // Continuous longitudinal East Corridor bearing girders over East Arcade beams & Banquet West Wall (X = +8.20)
            EnsureBox(group, "UpperElev_Support_EastCorrGirderW",
                new Vector3(CorridorOuterMinX + 0.110f, girderMidY, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.220f, girderH, CorridorLengthZ), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "UpperElev_Support_EastCorrGirderE",
                new Vector3(CorridorOuterMaxX - 0.110f, girderMidY, CorridorCenterZ), Quaternion.identity,
                new Vector3(0.220f, girderH, CorridorLengthZ), mats.ColumnConcrete, keepCollider: true);

            // Primary East-West transfer girder directly over Nivel 0's Connection A spine (Z = +11.91) supporting Vestibule
            float vestSpanX = VestibuleOuterMaxX - VestibuleOuterMinX; // 5.430f
            float vestSpanCenterX = (VestibuleOuterMinX + VestibuleOuterMaxX) * 0.5f; // +5.905f
            EnsureBox(group, "UpperElev_Support_ConnASpineGirder",
                new Vector3(vestSpanCenterX, girderMidY, VestibuleMaxZ + 0.110f), Quaternion.identity,
                new Vector3(vestSpanX, girderH, 0.220f), mats.ColumnConcrete, keepCollider: true);

            // Secondary East-West transfer girder at Vestibule South portal line (Z = +9.29)
            EnsureBox(group, "UpperElev_Support_VestibuleSouthGirder",
                new Vector3(vestSpanCenterX, girderMidY, WaitingPortalCenterZ), Quaternion.identity,
                new Vector3(vestSpanX, girderH, 0.220f), mats.ColumnConcrete, keepCollider: true);

            // Waiting Area West bearing plinth directly above Hotel_Elevator_Core East piers & Hotel_Lobby_Beam_West
            EnsureBox(group, "UpperElev_Support_WaitingCorePlinthW",
                new Vector3(3.280f, girderMidY, WaitingCenterZ), Quaternion.identity,
                new Vector3(0.260f, girderH, WaitingWidthZ), mats.ColumnConcrete, keepCollider: true);

            // Waiting Area East & South perimeter transfer girders framing the 2.79m x 5.20m slab
            EnsureBox(group, "UpperElev_Support_WaitingGirderE",
                new Vector3(WaitingMaxX + 0.110f, girderMidY, (WaitingOuterMinZ + WaitingPortalMinZ) * 0.5f), Quaternion.identity,
                new Vector3(0.220f, girderH, WaitingPortalMinZ - WaitingOuterMinZ), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(group, "UpperElev_Support_WaitingGirderS",
                new Vector3((WaitingOuterMinX + WaitingOuterMaxX) * 0.5f, girderMidY, WaitingMinZ - 0.110f), Quaternion.identity,
                new Vector3(WaitingOuterMaxX - WaitingOuterMinX, girderH, 0.220f), mats.ColumnConcrete, keepCollider: true);
        }

        private static void BuildLightingDetails(Transform group, ApproachMaterials mats)
        {
            // Strictly emissive-only utility strips (ZERO Unity Light components; exact 18-light budget preserved).
            // Note: Emissive materials provide visual spatial landmarks only and do not emit real dynamic light.
            float[] corrStripZs = { 26.80f, 22.40f, 18.00f, 13.60f };
            for (int i = 0; i < corrStripZs.Length; i++)
            {
                BuildEmissiveStripNS(group, $"UpperElev_EastCorr_Strip_{i}", CorridorCenterX, corrStripZs[i], mats);
            }

            // Single restrained E-W strip centered in the Transition Vestibule
            BuildEmissiveStripEW(group, "UpperElev_Vestibule_Strip", VestibuleCenterX, VestibuleCenterZ, mats);

            // Single restrained N-S strip centered in the Upper Elevator Waiting Area
            BuildEmissiveStripNS(group, "UpperElev_Waiting_Strip", WaitingCenterX, WaitingCenterZ, mats);
        }

        private static void BuildEmissiveStripNS(Transform group, string prefix, float x, float z, ApproachMaterials mats)
        {
            EnsureBox(group, $"{prefix}_Backplate",
                new Vector3(x, CeilingSoffitY - 0.025f, z), Quaternion.identity,
                new Vector3(0.140f, 0.040f, 1.420f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, $"{prefix}_Tube",
                new Vector3(x, CeilingSoffitY - 0.055f, z), Quaternion.identity,
                new Vector3(0.065f, 0.035f, 1.320f), mats.UtilityWarmOn, keepCollider: false);
        }

        private static void BuildEmissiveStripEW(Transform group, string prefix, float x, float z, ApproachMaterials mats)
        {
            EnsureBox(group, $"{prefix}_Backplate",
                new Vector3(x, CeilingSoffitY - 0.025f, z), Quaternion.identity,
                new Vector3(1.420f, 0.040f, 0.140f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, $"{prefix}_Tube",
                new Vector3(x, CeilingSoffitY - 0.055f, z), Quaternion.identity,
                new Vector3(1.320f, 0.035f, 0.065f), mats.UtilityWarmOn, keepCollider: false);
        }

        private static Transform EnsureSubgroup(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.localPosition = Vector3.zero;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                return existing;
            }
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject EnsureBox(
            Transform parent,
            string name,
            Vector3 worldPos,
            Quaternion worldRot,
            Vector3 scale,
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
                go.transform.SetParent(parent, true);
            }

            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = scale;
            go.isStatic = true;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
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
        /// Read-only Editor diagnostic camera utility for Implementation 07.23.
        /// Captures all 12 required first-person views using exact Remanencia Level 0 ambient light,
        /// exponential-squared fog, fog-matched SolidColor clear flags, and PlayerCamera FOV (60°).
        /// </summary>
        public static void RenderUpperElevatorApproachVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_ELEV_APPROACH_0723_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_ElevApproach_0723_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempElevApproach0723VerifyCam__");
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
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.10f;
                cam.farClipPlane = 250f;
                cam.targetTexture = rt;

                const float eyeY = GalleryFloorY + 0.040f + 1.600f; // -44.760f

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    // 1. Approaching the South portal from the North Upper Gallery
                    ("verify_0723_01_approaching_south_portal.png",
                        new Vector3(3.60f, eyeY, 31.30f), new Vector3(7.20f, eyeY - 0.10f, 29.00f)),
                    // 2. Looking through the new Bay 2 South portal
                    ("verify_0723_02_looking_through_portal.png",
                        new Vector3(7.20f, eyeY, 31.40f), new Vector3(7.20f, eyeY - 0.05f, 22.00f)),
                    // 3. Entering the East-side approach corridor
                    ("verify_0723_03_entering_east_corridor.png",
                        new Vector3(7.20f, eyeY, 28.60f), new Vector3(7.20f, eyeY - 0.05f, 16.00f)),
                    // 4. Looking South along the 17.78m corridor
                    ("verify_0723_04_looking_south_corridor.png",
                        new Vector3(7.20f, eyeY, 23.00f), new Vector3(7.20f, eyeY - 0.05f, 11.00f)),
                    // 5. Approaching Connection A's upper alignment (Z = +11.80)
                    ("verify_0723_05_approaching_connA_alignment.png",
                        new Vector3(7.20f, eyeY, 15.20f), new Vector3(6.80f, eyeY - 0.15f, 10.60f)),
                    // 6. Entering the compact Transition Vestibule
                    ("verify_0723_06_entering_transition_vestibule.png",
                        new Vector3(7.50f, eyeY, 11.20f), new Vector3(4.60f, eyeY - 0.10f, 10.10f)),
                    // 7. Turning West & South through the 2.40m waiting-area portal
                    ("verify_0723_07_turning_through_south_portal.png",
                        new Vector3(5.60f, eyeY, 10.90f), new Vector3(4.30f, eyeY - 0.10f, 7.20f)),
                    // 8. Entering the Upper Elevator Waiting Area
                    ("verify_0723_08_entering_waiting_area.png",
                        new Vector3(4.85f, eyeY, 8.90f), new Vector3(3.80f, eyeY - 0.10f, 6.40f)),
                    // 9. Looking at both identical sealed upper elevator frames (Bay 1 & Bay 2)
                    ("verify_0723_09_both_identical_sealed_bays.png",
                        new Vector3(5.95f, eyeY, 6.80f), new Vector3(3.38f, eyeY - 0.15f, 6.80f)),
                    // 10. Looking back North toward the Transition Vestibule
                    ("verify_0723_10_looking_back_toward_vestibule.png",
                        new Vector3(4.85f, eyeY, 5.20f), new Vector3(4.95f, eyeY - 0.05f, 10.80f)),
                    // 11. Returning North into the East approach corridor
                    ("verify_0723_11_returning_into_east_corridor.png",
                        new Vector3(6.40f, eyeY, 10.40f), new Vector3(7.20f, eyeY - 0.05f, 18.00f)),
                    // 12. Returning North to the North Upper Gallery
                    ("verify_0723_12_returning_to_north_gallery.png",
                        new Vector3(7.20f, eyeY, 26.80f), new Vector3(7.20f, eyeY - 0.05f, 32.50f))
                };

                RenderTexture prevActive = RenderTexture.active;
                for (int i = 0; i < shots.Length; i++)
                {
                    camGo.transform.position = shots[i].pos;
                    camGo.transform.LookAt(shots[i].lookAt, Vector3.up);
                    cam.Render();
                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readTex.Apply();
                    File.WriteAllBytes(Path.Combine(outDir, shots[i].fileName), readTex.EncodeToPNG());
                }
                RenderTexture.active = prevActive;
                Debug.Log($"[UpperElevatorApproachBuilder] Rendered {shots.Length} diagnostic verification views to '{outDir}'.");
            }
            finally
            {
                RenderSettings.skybox = prevSkybox;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.fog = prevFog;
                RenderSettings.fogMode = prevFogMode;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogDensity = prevFogDensity;

                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(readTex);
            }
        }
    }
}
