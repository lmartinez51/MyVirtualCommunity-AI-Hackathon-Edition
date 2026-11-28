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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.21:
    /// East Overhang — Approved Alternative C (Discontinuous East Fragment: Perimeter Threshold Neck +
    /// Displaced Concrete/Plaster Fragment).
    ///
    /// Opens ONLY the pre-framed 2.040m x 2.240m aperture in NUGallery_TerminalWall_East (Z in [+30.280, +32.320],
    /// preserving NUGallery_Terminal_SealedFrameS/N/Top, ConduitDrop, and JunctionBox in-place), builds a
    /// 1.200m compressed concrete Perimeter Threshold Neck (X in [+16.130, +17.330], 2.040m clear width,
    /// gradually shifting centerline -0.300m South from Z=+31.300 to Z=+31.000 with zero sharp corners),
    /// frames a flush dark-steel Architectural Separation Reveal at X ≈ +17.280..+17.330 over a continuous
    /// solid Y=-46.400m walking slab, and opens into a 3.200m x 3.200m Displaced East Fragment
    /// (X in [+17.330, +20.530], Z in [+29.400, +32.600], ceiling soffit Y=-43.250m -> 3.150m headroom,
    /// weathered plaster infill, SOLID South & North walls, and East-facing framed laminated safety-glass
    /// observation bays reusing Remanencia_Level0_OverlookSafetyGlass.mat read-only).
    ///
    /// Articulates a coherent visible concrete cantilever/transfer frame grafted onto the existing
    /// X=+16.00 perimeter bearing zone and X=+14.20 back-span frame (zero columns descending into void,
    /// zero intersection with Room 111-B or lower slabs), with inaccessible truncated beam remnants
    /// terminating past the East glass into exterior fog. Adds ZERO Unity Light components (exact 18-light
    /// budget preserved) and ZERO runtime scripts.
    /// </summary>
    public static class EastOverhangBuilder
    {
        public const string RootName = BackroomsLevel1Builder.EastOverhangRootName;

        // Verified existing East terminal aperture (aligned with NUGallery_Terminal_SealedFrameS/N/Top)
        public const float TerminalWallCenterX = NorthUpperGalleryBuilder.GalleryTerminalWallCenterX; // +16.000f
        public const float TerminalWallThickness = NorthUpperGalleryBuilder.GalleryTerminalWallThickness; // 0.260f
        public const float TerminalWallInnerX = NorthUpperGalleryBuilder.GalleryClearMaxX;            // +15.870f
        public const float TerminalWallOuterX = TerminalWallCenterX + TerminalWallThickness * 0.5f;   // +16.130f
        public const float PortalMinZ = NorthUpperGalleryBuilder.PortalMinZ;                          // +30.280f
        public const float PortalMaxZ = NorthUpperGalleryBuilder.PortalMaxZ;                          // +32.320f
        public const float PortalCenterZ = NorthUpperGalleryBuilder.PortalCenterZ;                    // +31.300f
        public const float PortalClearWidthZ = PortalMaxZ - PortalMinZ;                               //  2.040f
        public const float PortalClearHeightY = NorthUpperGalleryBuilder.PortalClearHeight;           //  2.240f
        public const float FloorY = NorthUpperGalleryBuilder.GalleryFloorY;                           // -46.400f
        public const float SlabThickness = NorthUpperGalleryBuilder.SlabThickness;                    //  0.200f
        public const float SlabBottomY = FloorY - SlabThickness;                                      // -46.600f
        public const float GalleryCeilingSoffitY = NorthUpperGalleryBuilder.GalleryCeilingSoffitY;    // -43.550f
        public const float NeckCeilingSoffitY = FloorY + PortalClearHeightY;                          // -44.160f

        // Trimmed 07.17 NUGallery_TerminalWall_East (preserved in-place as South pier) & Baseboard
        public const float GalleryOuterMinZ = NorthUpperGalleryBuilder.GalleryClearMinZ - NorthUpperGalleryBuilder.GalleryWallThickness; // +29.580f
        public const float GalleryOuterMaxZ = NorthUpperGalleryBuilder.GalleryClearMaxZ + NorthUpperGalleryBuilder.GalleryWallThickness; // +33.020f
        public const float TrimmedTerminalSouthPierLengthZ = PortalMinZ - GalleryOuterMinZ;           //  0.700f
        public const float TrimmedTerminalSouthPierCenterZ = (GalleryOuterMinZ + PortalMinZ) * 0.5f;  // +29.930f
        public const float TrimmedTerminalSouthBaseboardLengthZ = PortalMinZ - NorthUpperGalleryBuilder.GalleryClearMinZ; // 0.480f
        public const float TrimmedTerminalSouthBaseboardCenterZ = (NorthUpperGalleryBuilder.GalleryClearMinZ + PortalMinZ) * 0.5f; // +30.040f

        // North pier completing the split East terminal wall (built inside EastOverhang_ThresholdNeck)
        public const float TerminalNorthPierLengthZ = GalleryOuterMaxZ - PortalMaxZ;                  //  0.700f
        public const float TerminalNorthPierCenterZ = (PortalMaxZ + GalleryOuterMaxZ) * 0.5f;         // +32.670f
        public const float TerminalNorthBaseboardLengthZ = NorthUpperGalleryBuilder.GalleryClearMaxZ - PortalMaxZ; // 0.480f
        public const float TerminalNorthBaseboardCenterZ = (PortalMaxZ + NorthUpperGalleryBuilder.GalleryClearMaxZ) * 0.5f; // +32.560f

        // Perimeter Threshold Neck (X in [+16.130, +17.330], 1.200m long, shifts centerline -0.300m South smoothly!)
        public const float NeckMinX = TerminalWallOuterX;                                             // +16.130f
        public const float NeckLengthX = 1.200f;
        public const float NeckMaxX = NeckMinX + NeckLengthX;                                         // +17.330f
        public const float CenterlineShiftZ = -0.300f;
        public const float NeckExitCenterZ = PortalCenterZ + CenterlineShiftZ;                        // +31.000f
        public const float NeckExitMinZ = PortalMinZ + CenterlineShiftZ;                              // +29.980f
        public const float NeckExitMaxZ = PortalMaxZ + CenterlineShiftZ;                              // +32.020f
        public const float WallThickness = 0.220f;

        // Displaced East Fragment (X in [+17.330, +20.530], Z in [+29.400, +32.600], 3.200m x 3.200m, ceil Y = -43.250m)
        public const float FragmentMinX = NeckMaxX;                                                   // +17.330f
        public const float FragmentLengthX = 3.200f;
        public const float FragmentMaxX = FragmentMinX + FragmentLengthX;                             // +20.530f
        public const float FragmentClearWidthZ = 3.200f;
        public const float FragmentCenterZ = NeckExitCenterZ;                                         // +31.000f
        public const float FragmentMinZ = FragmentCenterZ - FragmentClearWidthZ * 0.5f;               // +29.400f
        public const float FragmentMaxZ = FragmentCenterZ + FragmentClearWidthZ * 0.5f;               // +32.600f
        public const float FragmentOuterMinZ = FragmentMinZ - WallThickness;                          // +29.180f (+0.18m clear N of EastCorr Zmax=29.00!)
        public const float FragmentOuterMaxZ = FragmentMaxZ + WallThickness;                          // +32.820f
        public const float FragmentCeilingSoffitY = GalleryCeilingSoffitY + 0.300f;                   // -43.250f (3.150m clear headroom!)
        public const float FragmentCeilingTopY = FragmentCeilingSoffitY + SlabThickness;              // -43.050f

        // East-facing observation facade (X in [+20.530, +20.730], 1.050m parapet + framed laminated safety glass)
        public const float EastParapetThicknessX = 0.200f;
        public const float EastParapetOuterMaxX = FragmentMaxX + EastParapetThicknessX;               // +20.730f
        public const float EastParapetCenterX = (FragmentMaxX + EastParapetOuterMaxX) * 0.5f;         // +20.630f
        public const float EastParapetGuardHeight = 1.050f;
        public const float EastParapetTopY = FloorY + EastParapetGuardHeight;                         // -45.350f
        public const float SteelSillHeight = 0.040f;
        public const float GlassBottomY = EastParapetTopY + SteelSillHeight;                          // -45.310f
        public const float TopHeaderHeight = 0.060f;
        public const float GlassTopY = FragmentCeilingSoffitY - TopHeaderHeight;                      // -43.310f
        public const float GlassHeight = GlassTopY - GlassBottomY;                                    //  2.000f
        public const float GlassThicknessX = 0.024f;
        public const float FrameJambWidthZ = 0.055f;
        public const float MullionWidthZ = 0.048f;

        // Visible exterior truncated cantilever beam stub reach past East facade
        public const float ExteriorBeamStubReachX = 0.620f;
        public const float ExteriorBeamStubMaxX = EastParapetOuterMaxX + ExteriorBeamStubReachX;      // +21.350f

        private struct OverhangMaterials
        {
            public Material WallConcrete;
            public Material PlasterWall;
            public Material FloorConcrete;
            public Material CeilingSoffit;
            public Material ColumnConcrete;
            public Material BaseboardConcrete;
            public Material IndustrialMetal;
            public Material UtilityWarmOn;
            public Material SafetyGlass;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.21 East Overhang")]
        public static void MigrateEastOverhangAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateEastOverhangInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.21 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EastOverhangBuilder] Implementation 07.21 East Overhang (Alternative C — Discontinuous East Fragment) migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.21.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateEastOverhangInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"EastOverhangBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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
                    $"Expected '{NorthUpperGalleryBuilder.RootName}' before running 07.21 East Overhang migration.");
            }

            OverhangMaterials mats = LoadMaterialsReadOnly();

            // 1. Open ONLY the authorized pre-framed East aperture in NUGallery_TerminalWall_East in-place
            //    (preserves TerminalWall_East, Terminal_Baseboard, SealedFrameS/N/Top, ConduitDrop & JunctionBox!)
            TrimEastTerminalWallInPlace(galleryRoot);

            // 2. Build/update dedicated Hotel_EastOverhang root with the 6 structured logical groups
            Transform overhangRoot = EnsureSubgroup(shell, RootName);

            BuildThresholdNeck(EnsureSubgroup(overhangRoot, "EastOverhang_ThresholdNeck"), mats);
            BuildStructuralSupportFrame(EnsureSubgroup(overhangRoot, "EastOverhang_Structure"), mats);
            BuildArchitecturalReveal(EnsureSubgroup(overhangRoot, "EastOverhang_ArchitecturalReveal"), mats);
            BuildDisplacedFragment(EnsureSubgroup(overhangRoot, "EastOverhang_DisplacedFragment"), mats);
            BuildEastObservationFacade(EnsureSubgroup(overhangRoot, "EastOverhang_EastFacade"), mats);
            BuildInteriorDetails(EnsureSubgroup(overhangRoot, "EastOverhang_InteriorDetails"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform root = architectureShell.Find(RootName);
            if (root == null) return false;
            return root.Find("EastOverhang_ThresholdNeck") != null &&
                   root.Find("EastOverhang_Structure") != null &&
                   root.Find("EastOverhang_ArchitecturalReveal") != null &&
                   root.Find("EastOverhang_DisplacedFragment") != null &&
                   root.Find("EastOverhang_EastFacade") != null &&
                   root.Find("EastOverhang_InteriorDetails") != null;
        }

        private static OverhangMaterials LoadMaterialsReadOnly()
        {
            return new OverhangMaterials
            {
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath),
                PlasterWall = LoadRequiredMat(BackroomsLevel1Builder.PlasterWallMatPath),
                FloorConcrete = LoadRequiredMat(BackroomsLevel1Builder.FloorConcreteMatPath),
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                BaseboardConcrete = LoadRequiredMat(BackroomsLevel1Builder.BaseboardMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                UtilityWarmOn = LoadRequiredMat(BackroomsLevel1Builder.UtilityWarmOnMatPath),
                SafetyGlass = LoadRequiredMat(BackroomsLevel1Builder.OverlookSafetyGlassMatPath)
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

        private static void TrimEastTerminalWallInPlace(Transform galleryRoot)
        {
            Transform eastTerm = galleryRoot.Find("NUGallery_EastTerminal");
            if (eastTerm == null)
            {
                throw new InvalidOperationException("NUGallery_EastTerminal not found under Hotel_NorthUpperGallery.");
            }

            Transform wallEast = eastTerm.Find("NUGallery_TerminalWall_East");
            if (wallEast == null)
            {
                throw new InvalidOperationException("NUGallery_TerminalWall_East not found.");
            }

            float wallH = GalleryCeilingSoffitY - SlabBottomY; // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f;       // -45.075f

            // Retain NUGallery_TerminalWall_East GameObject/fileID in-place as South pier (Z in [+29.580, +30.280])
            wallEast.position = new Vector3(TerminalWallCenterX, wallMidY, TrimmedTerminalSouthPierCenterZ);
            wallEast.rotation = Quaternion.identity;
            wallEast.localScale = new Vector3(TerminalWallThickness, wallH, TrimmedTerminalSouthPierLengthZ);

            Transform baseEast = eastTerm.Find("NUGallery_Terminal_Baseboard");
            if (baseEast != null)
            {
                baseEast.position = new Vector3(TerminalWallInnerX - 0.012f, FloorY + 0.06f, TrimmedTerminalSouthBaseboardCenterZ);
                baseEast.rotation = Quaternion.identity;
                baseEast.localScale = new Vector3(0.025f, 0.12f, TrimmedTerminalSouthBaseboardLengthZ);
            }
        }

        private static void BuildThresholdNeck(Transform group, OverhangMaterials mats)
        {
            float galWallH = GalleryCeilingSoffitY - SlabBottomY; // 3.050f
            float galWallMidY = SlabBottomY + galWallH * 0.5f;

            // 1. Complete the split East terminal wall around Z in [+30.280, +32.320]
            EnsureBox(group, "EastOverhang_Terminal_NorthPier",
                new Vector3(TerminalWallCenterX, galWallMidY, TerminalNorthPierCenterZ), Quaternion.identity,
                new Vector3(TerminalWallThickness, galWallH, TerminalNorthPierLengthZ), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "EastOverhang_Terminal_NorthBaseboard",
                new Vector3(TerminalWallInnerX - 0.012f, FloorY + 0.06f, TerminalNorthBaseboardCenterZ), Quaternion.identity,
                new Vector3(0.025f, 0.12f, TerminalNorthBaseboardLengthZ), mats.BaseboardConcrete, keepCollider: false);

            float transomH = GalleryCeilingSoffitY - NeckCeilingSoffitY; // 0.610f
            EnsureBox(group, "EastOverhang_Terminal_Transom",
                new Vector3(TerminalWallCenterX, NeckCeilingSoffitY + transomH * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(TerminalWallThickness, transomH, PortalClearWidthZ), mats.WallConcrete, keepCollider: true);

            // Solid subfloor slab bridge under the opening (Y in [-46.600, -46.400], flush at FloorY)
            EnsureBox(group, "EastOverhang_Terminal_SubfloorKick",
                new Vector3(TerminalWallCenterX, FloorY - SlabThickness * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(TerminalWallThickness, SlabThickness, PortalClearWidthZ), mats.FloorConcrete, keepCollider: true);

            // Architectural concrete through-wall reveals (5mm proud to eliminate any coplanar Z-fighting) & flush threshold plate
            EnsureBox(group, "EastOverhang_Portal_RevealS",
                new Vector3(TerminalWallCenterX, FloorY + PortalClearHeightY * 0.5f, PortalMinZ - 0.015f), Quaternion.identity,
                new Vector3(TerminalWallThickness + 0.03f, PortalClearHeightY - 0.004f, 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Portal_RevealN",
                new Vector3(TerminalWallCenterX, FloorY + PortalClearHeightY * 0.5f, PortalMaxZ + 0.015f), Quaternion.identity,
                new Vector3(TerminalWallThickness + 0.03f, PortalClearHeightY - 0.004f, 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Portal_RevealTop",
                new Vector3(TerminalWallCenterX, NeckCeilingSoffitY + 0.015f, PortalCenterZ), Quaternion.identity,
                new Vector3(TerminalWallThickness + 0.03f, 0.040f, PortalClearWidthZ + 0.040f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Portal_ThresholdPlate",
                new Vector3(TerminalWallCenterX, FloorY + 0.002f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.320f, 0.004f, PortalClearWidthZ), mats.IndustrialMetal, keepCollider: false);

            // 2. Perimeter Threshold Neck (X in [+16.130, +17.330], 1.200m long, continuous level Y=-46.400m floor)
            //    Gradually shifts centerline -0.300m South (Z=+31.300 -> Z=+31.000) via parallel 14.036° splayed walls
            //    maintaining full 2.040m clear width everywhere -> exact neck walkable area = 1.200 * 2.040 = 2.448 m²!
            float neckCenterX = (NeckMinX + NeckMaxX) * 0.5f; // +16.730f
            float neckSlabCenterZ = 31.150f;
            float neckSlabWidthZ = 2.860f; // Z in [29.720, 32.580] -> fully supports both oblique sidewalls!

            EnsureBox(group, "EastOverhang_Neck_FloorSlab",
                new Vector3(neckCenterX, FloorY - SlabThickness * 0.5f, neckSlabCenterZ), Quaternion.identity,
                new Vector3(NeckLengthX, SlabThickness, neckSlabWidthZ), mats.FloorConcrete, keepCollider: true);

            // Compressed neck ceiling slab (soffit Y = -44.160m -> 2.240m clear height matching portal header;
            // terminates 4cm inside the West fragment wall at X=+17.290 to eliminate coplanar bleed into the fragment!)
            const float neckCeilLenX = NeckLengthX - 0.040f; // 1.160f (X in [+16.130, +17.290])
            EnsureBox(group, "EastOverhang_Neck_CeilingSlab",
                new Vector3(NeckMinX + neckCeilLenX * 0.5f, NeckCeilingSoffitY + SlabThickness * 0.5f, neckSlabCenterZ), Quaternion.identity,
                new Vector3(neckCeilLenX, SlabThickness, neckSlabWidthZ), mats.CeilingSoffit, keepCollider: true);

            // Oblique concrete sidewalls (smooth 14.036° alignment shift, zero sharp corners, zero intrusion into portal!)
            float neckWallH = NeckCeilingSoffitY - SlabBottomY; // 2.440f
            float neckWallMidY = SlabBottomY + neckWallH * 0.5f;
            float dx = NeckLengthX;       // +1.200f
            float dz = CenterlineShiftZ;  // -0.300f
            float obliqueLen = Mathf.Sqrt(dx * dx + dz * dz); // 1.23693f
            float yawDeg = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg; // +14.03624° -> local +X aligns with (+1.20, 0, -0.30)
            Quaternion obliqueRot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 outwardNorth = obliqueRot * Vector3.forward; // (+0.24254, 0, +0.97014)
            Vector3 outwardSouth = -outwardNorth;                // (-0.24254, 0, -0.97014)

            Vector3 southInnerMid = new Vector3(neckCenterX, neckWallMidY, (PortalMinZ + NeckExitMinZ) * 0.5f); // (16.730, midY, 30.130)
            Vector3 northInnerMid = new Vector3(neckCenterX, neckWallMidY, (PortalMaxZ + NeckExitMaxZ) * 0.5f); // (16.730, midY, 32.170)

            EnsureBox(group, "EastOverhang_Neck_WallSouth",
                southInnerMid + outwardSouth * (WallThickness * 0.5f), obliqueRot,
                new Vector3(obliqueLen, neckWallH, WallThickness), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "EastOverhang_Neck_WallNorth",
                northInnerMid + outwardNorth * (WallThickness * 0.5f), obliqueRot,
                new Vector3(obliqueLen, neckWallH, WallThickness), mats.WallConcrete, keepCollider: true);

            // Exterior corner closure plugs sealing the obtuse outer joints behind the oblique walls (strictly outside walkable path)
            EnsureBox(group, "EastOverhang_Neck_OuterCornerN_West",
                new Vector3(NeckMinX + 0.055f, neckWallMidY, PortalMaxZ + 0.140f), Quaternion.identity,
                new Vector3(0.110f, neckWallH, 0.280f), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "EastOverhang_Neck_OuterCornerS_East",
                new Vector3(NeckMaxX - 0.055f, neckWallMidY, NeckExitMinZ - 0.140f), Quaternion.identity,
                new Vector3(0.110f, neckWallH, 0.280f), mats.WallConcrete, keepCollider: true);

            // Matching oblique baseboards along inner neck faces (non-colliding)
            Vector3 southBaseMid = new Vector3(neckCenterX, FloorY + 0.06f, (PortalMinZ + NeckExitMinZ) * 0.5f);
            Vector3 northBaseMid = new Vector3(neckCenterX, FloorY + 0.06f, (PortalMaxZ + NeckExitMaxZ) * 0.5f);
            EnsureBox(group, "EastOverhang_Neck_BaseboardSouth",
                southBaseMid - outwardSouth * 0.012f, obliqueRot,
                new Vector3(obliqueLen - 0.04f, 0.12f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Neck_BaseboardNorth",
                northBaseMid - outwardNorth * 0.012f, obliqueRot,
                new Vector3(obliqueLen - 0.04f, 0.12f, 0.024f), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildStructuralSupportFrame(Transform group, OverhangMaterials mats)
        {
            // Visually articulated concrete cantilever / transfer frame grafted onto the existing
            // X=+16.00 bearing wall (Room111B_OuterWallEast_NorthStub) and X=+14.20 back-span portal
            // (Pilaster_4 / CeilingBeam_4) — ZERO columns into exterior void, ZERO edits to protected objects!

            // 1. Back-span overhead transfer tie beams inside the East end of the gallery (X in [+14.340, +15.870])
            //    Visibly tie CeilingBeam_4 (X=+14.20) to the East bearing wall (X=+15.87), staying clear of ConduitDrop (Z=32.62)!
            float backSpanMinX = 14.340f;
            float backSpanMaxX = TerminalWallInnerX; // 15.870f
            float backSpanLenX = backSpanMaxX - backSpanMinX; // 1.530f
            float backSpanCenterX = (backSpanMinX + backSpanMaxX) * 0.5f;
            EnsureBox(group, "EastOverhang_BackSpan_TieBeamS",
                new Vector3(backSpanCenterX, GalleryCeilingSoffitY - 0.140f, NorthUpperGalleryBuilder.GalleryClearMinZ + 0.140f), Quaternion.identity,
                new Vector3(backSpanLenX, 0.280f, 0.260f), mats.ColumnConcrete, keepCollider: false);
            // Stop North tie beam cleanly at X=+15.72 to stay 0.09m clear West of protected ConduitDrop (X=15.83, Z=32.62)
            float backSpanNMaxX = 15.720f;
            float backSpanNLenX = backSpanNMaxX - backSpanMinX;
            EnsureBox(group, "EastOverhang_BackSpan_TieBeamN",
                new Vector3((backSpanMinX + backSpanNMaxX) * 0.5f, GalleryCeilingSoffitY - 0.140f, NorthUpperGalleryBuilder.GalleryClearMaxZ - 0.140f), Quaternion.identity,
                new Vector3(backSpanNLenX, 0.280f, 0.260f), mats.ColumnConcrete, keepCollider: false);

            // 2. Perimeter transfer anchor collar grafted flush onto outer East face X = +16.130
            EnsureBox(group, "EastOverhang_Perimeter_LowerAnchorCollar",
                new Vector3(NeckMinX + 0.140f, SlabBottomY - 0.225f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.280f, 0.450f, FragmentOuterMaxZ - FragmentOuterMinZ), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Perimeter_UpperAnchorBeam",
                new Vector3(NeckMinX + 0.140f, GalleryCeilingSoffitY + 0.120f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.280f, 0.360f, GalleryOuterMaxZ - GalleryOuterMinZ), mats.ColumnConcrete, keepCollider: false);

            // 3. Twin continuous lower concrete cantilever edge girders (Y in [-47.050, -46.600], 0.450m deep)
            //    Spanning from perimeter anchor X=+16.130 across the entire fragment AND extending +0.620m past the East
            //    safety glass (to X=+21.350) as truncated exterior structural beam remnants!
            float girderLenX = ExteriorBeamStubMaxX - NeckMinX; // 5.220f
            float girderCenterX = (NeckMinX + ExteriorBeamStubMaxX) * 0.5f; // +18.740f
            float girderBotY = SlabBottomY - 0.450f; // -47.050f (clear above lower -47.15 ceiling datum & +0.18m N of EastCorr!)
            float girderMidY = girderBotY + 0.225f;  // -46.825f
            EnsureBox(group, "EastOverhang_Cantilever_LowerGirderS",
                new Vector3(girderCenterX, girderMidY, FragmentOuterMinZ + 0.170f), Quaternion.identity,
                new Vector3(girderLenX, 0.450f, 0.340f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Cantilever_LowerGirderN",
                new Vector3(girderCenterX, girderMidY, FragmentOuterMaxZ - 0.170f), Quaternion.identity,
                new Vector3(girderLenX, 0.450f, 0.340f), mats.ColumnConcrete, keepCollider: false);

            // 4. Twin upper roof cantilever edge beams (Y in [-43.390, -43.050]) running across the fragment roof cornice
            //    and projecting +0.620m East past the glass facade as clearly visible truncated overhead beam stubs!
            float upperBeamMinX = FragmentMinX - 0.220f; // +17.110f (ties into fragment West structural frame)
            float upperBeamLenX = ExteriorBeamStubMaxX - upperBeamMinX; // 4.240f
            float upperBeamCenterX = (upperBeamMinX + ExteriorBeamStubMaxX) * 0.5f;
            EnsureBox(group, "EastOverhang_Cantilever_UpperBeamS",
                new Vector3(upperBeamCenterX, FragmentCeilingSoffitY + 0.030f, FragmentOuterMinZ + 0.150f), Quaternion.identity,
                new Vector3(upperBeamLenX, 0.340f, 0.300f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Cantilever_UpperBeamN",
                new Vector3(upperBeamCenterX, FragmentCeilingSoffitY + 0.030f, FragmentOuterMaxZ - 0.150f), Quaternion.identity,
                new Vector3(upperBeamLenX, 0.340f, 0.300f), mats.ColumnConcrete, keepCollider: false);

            // 5. Two inaccessible exterior parapet-level cantilever beam remnants aligned directly with the East facade mullions
            //    (Z=+30.467 & Z=+31.533, Y in [-45.650, -45.350]), projecting +0.550m East past the solid parapet/glass into fog!
            float innerMinZ = FragmentMinZ + FrameJambWidthZ;
            float innerMaxZ = FragmentMaxZ - FrameJambWidthZ;
            float stubZ0 = Mathf.Lerp(innerMinZ, innerMaxZ, 1f / 3f); // +30.485f
            float stubZ1 = Mathf.Lerp(innerMinZ, innerMaxZ, 2f / 3f); // +31.515f
            const float midStubReach = 0.550f;
            EnsureBox(group, "EastOverhang_ExteriorStub_ParapetS",
                new Vector3(EastParapetOuterMaxX + midStubReach * 0.5f, EastParapetTopY - 0.160f, stubZ0), Quaternion.identity,
                new Vector3(midStubReach, 0.280f, 0.220f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_ExteriorStub_ParapetN",
                new Vector3(EastParapetOuterMaxX + midStubReach * 0.5f, EastParapetTopY - 0.160f, stubZ1), Quaternion.identity,
                new Vector3(midStubReach, 0.280f, 0.220f), mats.ColumnConcrete, keepCollider: false);

            // Upper exterior tie stubs aligned with mullions at cornice level (visible directly through top glass bays!)
            EnsureBox(group, "EastOverhang_ExteriorStub_UpperS",
                new Vector3(EastParapetOuterMaxX + midStubReach * 0.5f, FragmentCeilingSoffitY - 0.080f, stubZ0), Quaternion.identity,
                new Vector3(midStubReach, 0.260f, 0.200f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_ExteriorStub_UpperN",
                new Vector3(EastParapetOuterMaxX + midStubReach * 0.5f, FragmentCeilingSoffitY - 0.080f, stubZ1), Quaternion.identity,
                new Vector3(midStubReach, 0.260f, 0.200f), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildArchitecturalReveal(Transform group, OverhangMaterials mats)
        {
            // Visual & material Architectural Separation Reveal around X = +17.280..+17.340
            // Continuous solid concrete walking slab runs unbroken underneath (ZERO gap, ZERO height lip!).
            const float revealX = 17.300f;

            // Flush dark-steel floor seam plate across the 2.040m threshold (h = 0.004m, non-colliding)
            EnsureBox(group, "EastOverhang_Reveal_FloorSeamPlate",
                new Vector3(revealX, FloorY + 0.002f, NeckExitCenterZ), Quaternion.identity,
                new Vector3(0.090f, 0.004f, PortalClearWidthZ), mats.IndustrialMetal, keepCollider: false);

            // Recessed dark-steel & concrete shadow-reveal portal frames marking the transition from raw neck to plaster fragment
            EnsureBox(group, "EastOverhang_Reveal_SteelJambS",
                new Vector3(revealX, FloorY + PortalClearHeightY * 0.5f, NeckExitMinZ + 0.022f), Quaternion.identity,
                new Vector3(0.070f, PortalClearHeightY - 0.004f, 0.044f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "EastOverhang_Reveal_SteelJambN",
                new Vector3(revealX, FloorY + PortalClearHeightY * 0.5f, NeckExitMaxZ - 0.022f), Quaternion.identity,
                new Vector3(0.070f, PortalClearHeightY - 0.004f, 0.044f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "EastOverhang_Reveal_SteelHeader",
                new Vector3(revealX, NeckCeilingSoffitY - 0.022f, NeckExitCenterZ), Quaternion.identity,
                new Vector3(0.070f, 0.044f, PortalClearWidthZ), mats.IndustrialMetal, keepCollider: false);

            // Fragment-side dark-concrete framing pilasters bordering the reveal seam (non-colliding, proud by 5mm)
            EnsureBox(group, "EastOverhang_Reveal_ConcreteTrimS",
                new Vector3(FragmentMinX + 0.032f, FloorY + PortalClearHeightY * 0.5f, NeckExitMinZ - 0.045f), Quaternion.identity,
                new Vector3(0.064f, PortalClearHeightY, 0.110f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Reveal_ConcreteTrimN",
                new Vector3(FragmentMinX + 0.032f, FloorY + PortalClearHeightY * 0.5f, NeckExitMaxZ + 0.045f), Quaternion.identity,
                new Vector3(0.064f, PortalClearHeightY, 0.110f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Reveal_ConcreteHeader",
                new Vector3(FragmentMinX + 0.032f, NeckCeilingSoffitY + 0.055f, NeckExitCenterZ), Quaternion.identity,
                new Vector3(0.064f, 0.110f, PortalClearWidthZ + 0.200f), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildDisplacedFragment(Transform group, OverhangMaterials mats)
        {
            float fragCenterX = (FragmentMinX + FragmentMaxX) * 0.5f; // +18.930f
            float outerLenX = EastParapetOuterMaxX - FragmentMinX;    //  3.400f (X in [+17.330, +20.730])
            float outerCenterX = (FragmentMinX + EastParapetOuterMaxX) * 0.5f; // +19.030f
            float outerWidthZ = FragmentOuterMaxZ - FragmentOuterMinZ; // 3.640f (Z in [+29.180, +32.820])

            // 1. Continuous level concrete floor slab (top Y = -46.400m, flush with neck & gallery!)
            EnsureBox(group, "EastOverhang_Fragment_FloorSlab",
                new Vector3(outerCenterX, FloorY - SlabThickness * 0.5f, FragmentCenterZ), Quaternion.identity,
                new Vector3(outerLenX, SlabThickness, outerWidthZ), mats.FloorConcrete, keepCollider: true);

            // 2. Elevated solid ceiling slab (soffit Y = -43.250m -> +0.300m higher than gallery -43.550m -> 3.150m headroom!)
            EnsureBox(group, "EastOverhang_Fragment_CeilingSlab",
                new Vector3(outerCenterX, FragmentCeilingSoffitY + SlabThickness * 0.5f, FragmentCenterZ), Quaternion.identity,
                new Vector3(outerLenX, SlabThickness, outerWidthZ), mats.CeilingSoffit, keepCollider: true);

            // 3. SOLID South wall (Z in [+29.180, +29.400] — ZERO window, ZERO viewing slot toward Room 111-B!)
            //    Uses weathered PlasterWall to distinguish the older displaced fragment from the raw-concrete gallery.
            float wallH = FragmentCeilingSoffitY - SlabBottomY; // 3.350f
            float wallMidY = SlabBottomY + wallH * 0.5f;
            float sideWallMinX = FragmentMinX - WallThickness; // +17.110f
            float sideWallLenX = EastParapetOuterMaxX - sideWallMinX; // 3.620f
            float sideWallCenterX = (sideWallMinX + EastParapetOuterMaxX) * 0.5f;

            EnsureBox(group, "EastOverhang_Fragment_WallSouth",
                new Vector3(sideWallCenterX, wallMidY, FragmentMinZ - WallThickness * 0.5f), Quaternion.identity,
                new Vector3(sideWallLenX, wallH, WallThickness), mats.PlasterWall, keepCollider: true);

            // 4. SOLID North wall (Z in [+32.600, +32.820])
            EnsureBox(group, "EastOverhang_Fragment_WallNorth",
                new Vector3(sideWallCenterX, wallMidY, FragmentMaxZ + WallThickness * 0.5f), Quaternion.identity,
                new Vector3(sideWallLenX, wallH, WallThickness), mats.PlasterWall, keepCollider: true);

            // 5. Symmetrical West return walls & elevated ceiling-transition transom framing the 2.040m neck arrival
            float westWallX = FragmentMinX - WallThickness * 0.5f; // +17.220f
            float returnLenZ = NeckExitMinZ - FragmentMinZ;        //  0.580f (South: 29.40..29.98; North: 32.02..32.60)
            EnsureBox(group, "EastOverhang_Fragment_WestReturnS",
                new Vector3(westWallX, wallMidY, FragmentMinZ + returnLenZ * 0.5f), Quaternion.identity,
                new Vector3(WallThickness, wallH, returnLenZ), mats.PlasterWall, keepCollider: true);
            EnsureBox(group, "EastOverhang_Fragment_WestReturnN",
                new Vector3(westWallX, wallMidY, FragmentMaxZ - returnLenZ * 0.5f), Quaternion.identity,
                new Vector3(WallThickness, wallH, returnLenZ), mats.PlasterWall, keepCollider: true);

            float westTransomH = FragmentCeilingSoffitY - NeckCeilingSoffitY; // 0.910f
            EnsureBox(group, "EastOverhang_Fragment_WestTransom",
                new Vector3(westWallX, NeckCeilingSoffitY + westTransomH * 0.5f, NeckExitCenterZ), Quaternion.identity,
                new Vector3(WallThickness, westTransomH, PortalClearWidthZ), mats.PlasterWall, keepCollider: true);

            // Structural interior corner concrete columns & central overhead transfer beam (framing the plaster infill!)
            float colH = FragmentCeilingSoffitY - FloorY; // 3.150f
            float colMidY = FloorY + colH * 0.5f;
            EnsureBox(group, "EastOverhang_Fragment_CornerCol_SW",
                new Vector3(FragmentMinX + 0.085f, colMidY, FragmentMinZ + 0.085f), Quaternion.identity,
                new Vector3(0.180f, colH, 0.180f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Fragment_CornerCol_NW",
                new Vector3(FragmentMinX + 0.085f, colMidY, FragmentMaxZ - 0.085f), Quaternion.identity,
                new Vector3(0.180f, colH, 0.180f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Fragment_CornerCol_SE",
                new Vector3(FragmentMaxX - 0.085f, colMidY, FragmentMinZ + 0.085f), Quaternion.identity,
                new Vector3(0.180f, colH, 0.180f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Fragment_CornerCol_NE",
                new Vector3(FragmentMaxX - 0.085f, colMidY, FragmentMaxZ - 0.085f), Quaternion.identity,
                new Vector3(0.180f, colH, 0.180f), mats.ColumnConcrete, keepCollider: false);

            EnsureBox(group, "EastOverhang_Fragment_CeilingBeam",
                new Vector3(fragCenterX, FragmentCeilingSoffitY - 0.130f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.240f, 0.260f, FragmentClearWidthZ), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildEastObservationFacade(Transform group, OverhangMaterials mats)
        {
            // 1. Solid poured-concrete East parapet (X in [+20.530, +20.730], Z in [+29.400, +32.600], top Y = -45.350m = 1.050m guard)
            float parapetH = EastParapetTopY - SlabBottomY; // 1.250f
            float parapetMidY = SlabBottomY + parapetH * 0.5f;
            EnsureBox(group, "EastOverhang_EastParapet_Concrete",
                new Vector3(EastParapetCenterX, parapetMidY, FragmentCenterZ), Quaternion.identity,
                new Vector3(EastParapetThicknessX, parapetH, FragmentClearWidthZ), mats.ColumnConcrete, keepCollider: true);

            // Dark-steel coping sill crowning the East parapet (Y in [-45.350, -45.310], solid collider)
            EnsureBox(group, "EastOverhang_EastParapet_SteelSill",
                new Vector3(EastParapetCenterX, EastParapetTopY + SteelSillHeight * 0.5f, FragmentCenterZ), Quaternion.identity,
                new Vector3(EastParapetThicknessX + 0.030f, SteelSillHeight, FragmentClearWidthZ), mats.IndustrialMetal, keepCollider: true);

            // 2. Dark-steel perimeter framing & two slim vertical mullions dividing the 3.200m East facade into 3 balanced bays
            float frameSpanH = FragmentCeilingSoffitY - GlassBottomY; // 2.060f
            float frameMidY = GlassBottomY + frameSpanH * 0.5f;
            float glassMidY = GlassBottomY + GlassHeight * 0.5f;

            EnsureBox(group, "EastOverhang_GlassFrame_JambS",
                new Vector3(EastParapetCenterX, frameMidY, FragmentMinZ + FrameJambWidthZ * 0.5f), Quaternion.identity,
                new Vector3(0.080f, frameSpanH, FrameJambWidthZ), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "EastOverhang_GlassFrame_JambN",
                new Vector3(EastParapetCenterX, frameMidY, FragmentMaxZ - FrameJambWidthZ * 0.5f), Quaternion.identity,
                new Vector3(0.080f, frameSpanH, FrameJambWidthZ), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "EastOverhang_GlassFrame_Header",
                new Vector3(EastParapetCenterX, FragmentCeilingSoffitY - TopHeaderHeight * 0.5f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.080f, TopHeaderHeight, FragmentClearWidthZ), mats.IndustrialMetal, keepCollider: true);

            // Narrow bottom glazing channel (non-colliding visual detail)
            EnsureBox(group, "EastOverhang_GlassFrame_BottomChannel",
                new Vector3(EastParapetCenterX, GlassBottomY + 0.015f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.052f, 0.030f, FragmentClearWidthZ - 2f * FrameJambWidthZ), mats.IndustrialMetal, keepCollider: false);

            float innerMinZ = FragmentMinZ + FrameJambWidthZ; // +29.455f
            float innerMaxZ = FragmentMaxZ - FrameJambWidthZ; // +32.545f
            float mullionS = Mathf.Lerp(innerMinZ, innerMaxZ, 1f / 3f); // +30.485f
            float mullionN = Mathf.Lerp(innerMinZ, innerMaxZ, 2f / 3f); // +31.515f

            EnsureBox(group, "EastOverhang_GlassFrame_Mullion_0",
                new Vector3(EastParapetCenterX, glassMidY, mullionS), Quaternion.identity,
                new Vector3(0.068f, GlassHeight, MullionWidthZ), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "EastOverhang_GlassFrame_Mullion_1",
                new Vector3(EastParapetCenterX, glassMidY, mullionN), Quaternion.identity,
                new Vector3(0.068f, GlassHeight, MullionWidthZ), mats.IndustrialMetal, keepCollider: true);

            // 3. Three visibly represented transparent laminated safety-glass panels (reusing 07.19 SafetyGlass material read-only!)
            //    Each panel carries its own directly aligned solid BoxCollider (100% gap-free containment!).
            float halfM = MullionWidthZ * 0.5f;
            (string name, float minZ, float maxZ)[] glassBays =
            {
                ("EastOverhang_SafetyGlass_Panel_0", innerMinZ, mullionS - halfM),
                ("EastOverhang_SafetyGlass_Panel_1", mullionS + halfM, mullionN - halfM),
                ("EastOverhang_SafetyGlass_Panel_2", mullionN + halfM, innerMaxZ)
            };

            for (int i = 0; i < glassBays.Length; i++)
            {
                float wZ = glassBays[i].maxZ - glassBays[i].minZ;
                float cz = (glassBays[i].minZ + glassBays[i].maxZ) * 0.5f;
                EnsureBox(group, glassBays[i].name,
                    new Vector3(EastParapetCenterX, glassMidY, cz), Quaternion.identity,
                    new Vector3(GlassThicknessX, GlassHeight, wZ), mats.SafetyGlass, keepCollider: true);
            }
        }

        private static void BuildInteriorDetails(Transform group, OverhangMaterials mats)
        {
            float fragCenterX = (FragmentMinX + FragmentMaxX) * 0.5f; // +18.930f

            // Fragment baseboards along solid South & North walls (non-colliding)
            EnsureBox(group, "EastOverhang_Fragment_BaseboardSouth",
                new Vector3(fragCenterX, FloorY + 0.06f, FragmentMinZ + 0.012f), Quaternion.identity,
                new Vector3(FragmentLengthX - 0.16f, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "EastOverhang_Fragment_BaseboardNorth",
                new Vector3(fragCenterX, FloorY + 0.06f, FragmentMaxZ - 0.012f), Quaternion.identity,
                new Vector3(FragmentLengthX - 0.16f, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);

            // Subtle longitudinal floor joint along the displaced centerline Z = +31.000 (contrasts with gallery joint Z = +31.440)
            EnsureBox(group, "EastOverhang_Fragment_FloorJointSeam",
                new Vector3(fragCenterX, FloorY + 0.002f, FragmentCenterZ), Quaternion.identity,
                new Vector3(FragmentLengthX - 0.24f, 0.004f, 0.028f), mats.BaseboardConcrete, keepCollider: false);

            // One restrained emissive-only ceiling utility strip (ZERO Unity Light component -> preserves exact 18-light budget!)
            EnsureBox(group, "EastOverhang_StripBackplate",
                new Vector3(fragCenterX - 0.58f, FragmentCeilingSoffitY - 0.025f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.140f, 0.040f, 1.380f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "EastOverhang_StripTube",
                new Vector3(fragCenterX - 0.58f, FragmentCeilingSoffitY - 0.055f, FragmentCenterZ), Quaternion.identity,
                new Vector3(0.065f, 0.035f, 1.280f), mats.UtilityWarmOn, keepCollider: false);
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
        /// Read-only Editor verification camera utility for Implementation 07.21.
        /// Captures all 10 required diagnostic first-person views using exact Remanencia Level 0
        /// ambient light, exponential-squared fog, and PlayerCamera FOV (60°).
        /// </summary>
        public static void RenderEastOverhangVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_OVERHANG_0721_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_Overhang_0721_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempOverhang0721VerifyCam__");
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

                const float eyeY = FloorY + 0.040f + 1.600f; // -44.760f

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    // 1. Approaching the original East terminal from the gallery
                    ("verify_0721_01_approaching_east_terminal.png",
                        new Vector3(11.40f, eyeY, 31.30f), new Vector3(18.50f, eyeY, 31.15f)),
                    // 2. Looking through the preserved framed doorway
                    ("verify_0721_02_preserved_framed_doorway.png",
                        new Vector3(14.50f, eyeY, 31.30f), new Vector3(20.20f, eyeY - 0.10f, 31.05f)),
                    // 3. Entering the compressed threshold neck
                    ("verify_0721_03_entering_threshold_neck.png",
                        new Vector3(16.10f, eyeY, 31.30f), new Vector3(20.50f, eyeY - 0.05f, 31.00f)),
                    // 4. Crossing the architectural separation reveal
                    ("verify_0721_04_architectural_separation_reveal.png",
                        new Vector3(16.85f, eyeY, 31.12f), new Vector3(18.40f, -45.25f, 30.25f)),
                    // 5. Entering the displaced fragment
                    ("verify_0721_05_entering_displaced_fragment.png",
                        new Vector3(17.55f, eyeY, 31.00f), new Vector3(20.60f, eyeY + 0.10f, 31.00f)),
                    // 6. Interior material & ceiling contrast between neck and fragment
                    ("verify_0721_06_material_and_ceiling_contrast.png",
                        new Vector3(19.90f, eyeY, 32.10f), new Vector3(17.20f, -44.60f, 30.10f)),
                    // 7. East-facing glass facade and exterior fog
                    ("verify_0721_07_east_glass_facade_and_fog.png",
                        new Vector3(18.40f, eyeY, 31.00f), new Vector3(21.40f, -44.85f, 31.00f)),
                    // 8. Visible structural beam remnants beyond the glass
                    ("verify_0721_08_structural_beam_remnants.png",
                        new Vector3(19.85f, eyeY, 31.95f), new Vector3(21.30f, -44.65f, 30.15f)),
                    // 9. Looking back toward the North Upper Gallery
                    ("verify_0721_09_looking_back_toward_gallery.png",
                        new Vector3(19.60f, eyeY, 31.00f), new Vector3(12.00f, eyeY, 31.30f)),
                    // 10. Complete return route through neck into gallery
                    ("verify_0721_10_return_route_into_gallery.png",
                        new Vector3(16.65f, eyeY, 31.16f), new Vector3( 8.00f, eyeY, 31.30f))
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
                    Debug.Log($"[EastOverhangBuilder] Rendered 07.21 verification view: {fullFile}");
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
