using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.26:
    /// Bay 1 Passenger Elevator Shaft Isolation (Static Structural Implementation Only —
    /// No Cabin, No Guide Rails, No Moving Doors, No Runtime Controller, No Audio, No New Lights).
    ///
    /// Implements strictly the 07.25A-reconciled static structural isolation of Bay 1 South (Z = +5.750):
    ///   1. Continuous reinforced concrete shaft divider separating Bay 1 from Bay 2:
    ///      X in [+1.240, +3.150], Z in [+6.680, +6.920] (0.240m thick), Y in [-50.000, -43.550]
    ///      plus flush 0.200m roof-level coping (Y in [-43.550, -43.350]). Zero gap, zero door pocket.
    ///   2. Four-piece perimeter reconstruction of monolithic Backrooms_Ceiling (Y in [-47.150, -46.950]),
    ///      retaining the original Backrooms_Ceiling GameObject in-place as the East slab
    ///      (X in [+3.150, +20.300], Z in [-12.300, +12.300]) and adding flush West, South, and North
    ///      slabs so ONLY the exact Bay 1 shaft aperture X in [+1.240, +3.150], Z in [+4.440, +6.680]
    ///      (1.910m x 2.240m = 4.2784 m²) is open while Bay 2 remains 100% capped overhead.
    ///   3. Continuous upper vertical shaft enclosure (BackWall X in [+1.000, +1.240], SouthWall
    ///      Z in [+4.200, +4.440], East plenum seal Y in [-46.950, -46.940]) and sealed concrete
    ///      roof cap (X in [+1.240, +3.150], Z in [+4.440, +6.680], Y in [-43.550, -43.350]).
    ///   4. Clean structural split of UpperElev_Waiting_WestCoreBackingWall (retained in-place as
    ///      the solid North section Z in [+6.435, +9.400] covering Bay 2) with South pier and upper
    ///      transom framing the future Bay 1 portal (Z in [+5.065, +6.435], Y in [-46.400, -44.200])
    ///      while preserving the 07.24 closed Bay 1 finished doors and solid safety backing intact.
    ///
    /// Leaves Hotel_Elevator_Core (all 35 children), all 5 adjacent lower fixtures, Backrooms_Floor,
    /// UpperElev_Waiting_FloorSlab, UpperElev_Waiting_CeilingSlab, all 07.24 Bay 1/2 facade objects,
    /// all 10 Lightwell objects, BorderCorridorScene.unity, FirstPersonPlayer.cs, and the exact
    /// 18-light budget 100% untouched. Reuses existing project materials strictly read-only.
    /// </summary>
    public static class UpperPassengerElevatorShaftBuilder
    {
        public const string RootName = BackroomsLevel1Builder.UpperPassengerElevatorShaftRootName;

        // Exact 07.25A-reconciled Bay 1 shaft aperture & envelope bounds
        public const float ApertureMinX = 1.240f;
        public const float ApertureMaxX = 3.150f;
        public const float ApertureMinZ = 4.440f;
        public const float ApertureMaxZ = 6.680f;
        public const float ApertureWidthX = ApertureMaxX - ApertureMinX; // 1.910f
        public const float ApertureDepthZ = ApertureMaxZ - ApertureMinZ; // 2.240f

        // Continuous concrete shaft divider separating Bay 1 from Bay 2
        public const float DividerMinX = ApertureMinX;                   // +1.240f
        public const float DividerMaxX = ApertureMaxX;                   // +3.150f
        public const float DividerMinZ = ApertureMaxZ;                   // +6.680f
        public const float DividerMaxZ = 6.920f;                         // +6.920f (0.240m thick)
        public const float DividerMinY = BackroomsLevel1Builder.BaseY;   // -50.000f
        public const float DividerMaxY = UpperElevatorApproachBuilder.CeilingSoffitY; // -43.550f

        // Monolithic Backrooms_Ceiling original bounds (Y in [-47.150, -46.950])
        public const float CeilingMinX = -20.300f;
        public const float CeilingMaxX = 20.300f;
        public const float CeilingMinZ = -12.300f;
        public const float CeilingMaxZ = 12.300f;
        public const float CeilingMinY = BackroomsLevel1Builder.CeilingY; // -47.150f
        public const float CeilingMaxY = -46.950f;
        public const float CeilingThicknessY = CeilingMaxY - CeilingMinY; // 0.200f

        // Upper shaft enclosure & sealed roof cap elevations
        public const float ShaftOuterMinX = 1.000f;
        public const float ShaftOuterMinZ = 4.200f;
        public const float RoofCapMinY = UpperElevatorApproachBuilder.CeilingSoffitY; // -43.550f
        public const float RoofCapMaxY = UpperElevatorApproachBuilder.CeilingTopY;    // -43.350f
        public const float RoofCapThicknessY = RoofCapMaxY - RoofCapMinY;             //  0.200f

        // Future upper Bay 1 landing opening (plane X in [+3.150, +3.350])
        public const float BackingWallMinX = 3.150f;
        public const float BackingWallMaxX = 3.350f;
        public const float UpperPortalMinZ = 5.065f;
        public const float UpperPortalMaxZ = 6.435f;
        public const float UpperPortalMinY = UpperElevatorApproachBuilder.GalleryFloorY;           // -46.400f
        public const float UpperPortalMaxY = UpperElevatorApproachBuilder.PlaceholderHeaderBottomY; // -44.200f

        private struct ShaftMaterials
        {
            public Material CeilingSoffit;
            public Material ColumnConcrete;
            public Material WallConcrete;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.26 Bay 1 Passenger Elevator Shaft Isolation")]
        public static void MigrateUpperPassengerElevatorShaftAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateUpperPassengerElevatorShaftInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.26 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UpperPassengerElevatorShaftBuilder] Implementation 07.26 Bay 1 Passenger Elevator Shaft Isolation migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.26.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateUpperPassengerElevatorShaftInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UpperPassengerElevatorShaftBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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

            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);
            if (approachRoot == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{UpperElevatorApproachBuilder.RootName}' before running 07.26 Shaft Isolation migration.");
            }

            if (!UpperElevatorBayArchitectureBuilder.VerifyStructurePresent(shell))
            {
                throw new InvalidOperationException(
                    "Expected 07.24 Twin Elevator Bay Architecture to be present before running 07.26 Shaft Isolation migration.");
            }

            Transform existingCeiling = shell.Find("Backrooms_Ceiling");
            if (existingCeiling == null)
            {
                throw new InvalidOperationException("Expected Backrooms_Ceiling under Backrooms_Architecture.");
            }

            Transform waitingGroup = approachRoot.Find("UpperElevatorApproach_WaitingArea");
            if (waitingGroup == null)
            {
                throw new InvalidOperationException("Expected UpperElevatorApproach_WaitingArea under Hotel_UpperElevatorApproach.");
            }

            Transform backingWall = waitingGroup.Find("UpperElev_Waiting_WestCoreBackingWall");
            if (backingWall == null)
            {
                throw new InvalidOperationException("Expected UpperElev_Waiting_WestCoreBackingWall under UpperElevatorApproach_WaitingArea.");
            }

            ShaftMaterials mats = LoadExistingMaterials();

            // Create dedicated sibling root under Backrooms_Architecture (leaving Hotel_Elevator_Core untouched)
            Transform shaftRoot = EnsureSubgroup(shell, RootName);

            // 1. Four-piece perimeter reconstruction of Backrooms_Ceiling (retaining original GO in-place as East slab)
            ReconstructBackroomsCeilingPerimeter(existingCeiling, EnsureSubgroup(shaftRoot, "Bay1Shaft_CeilingSplit"), mats);

            // 2. Continuous reinforced concrete shaft divider separating Bay 1 from Bay 2 (Y in [-50.000, -43.550] + roof coping)
            BuildContinuousConcreteDivider(EnsureSubgroup(shaftRoot, "Bay1Shaft_ConcreteDivider"), mats);

            // 3. Upper vertical shaft enclosure (BackWall, SouthWall, East plenum seal) and sealed roof cap
            BuildUpperShaftEnclosureAndRoofCap(EnsureSubgroup(shaftRoot, "Bay1Shaft_UpperEnclosure"), mats);

            // 4. Split UpperElev_Waiting_WestCoreBackingWall cleanly around future Bay 1 portal (preserving 07.24 facade & Bay 2 backing)
            SplitUpperBay1BackingWall(backingWall, EnsureSubgroup(shaftRoot, "Bay1Shaft_UpperPortalBacking"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform shaftRoot = architectureShell.Find(RootName);
            if (shaftRoot == null) return false;

            Transform ceilingSplit = shaftRoot.Find("Bay1Shaft_CeilingSplit");
            Transform divider = shaftRoot.Find("Bay1Shaft_ConcreteDivider");
            Transform enclosure = shaftRoot.Find("Bay1Shaft_UpperEnclosure");
            Transform portalBacking = shaftRoot.Find("Bay1Shaft_UpperPortalBacking");

            return architectureShell.Find("Backrooms_Ceiling") != null &&
                   ceilingSplit != null &&
                   ceilingSplit.Find("Backrooms_Ceiling_West") != null &&
                   ceilingSplit.Find("Backrooms_Ceiling_South") != null &&
                   ceilingSplit.Find("Backrooms_Ceiling_North") != null &&
                   divider != null &&
                   divider.Find("Bay1Shaft_DividerWall_North") != null &&
                   enclosure != null &&
                   enclosure.Find("UpperShaft_BackWall") != null &&
                   enclosure.Find("UpperShaft_SouthWall") != null &&
                   enclosure.Find("UpperShaft_RoofCap") != null &&
                   enclosure.Find("UpperShaft_EastPlenumSeal") != null &&
                   portalBacking != null &&
                   portalBacking.Find("Bay1UpperBacking_SouthPier") != null &&
                   portalBacking.Find("Bay1UpperBacking_Transom") != null;
        }

        private static ShaftMaterials LoadExistingMaterials()
        {
            return new ShaftMaterials
            {
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath)
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

        /// <summary>
        /// Retains original Backrooms_Ceiling in-place as the East slab (X in [+3.150, +20.300]) and
        /// constructs flush West, South, and North sibling slabs at exact Y in [-47.150, -46.950].
        /// Note: North slab starts at Z = +6.920 (flush against the North face of the continuous concrete
        /// divider Z in [+6.680, +6.920]) so Bay 2 is 100% capped with zero overlapping colliders or Z-fighting,
        /// leaving strictly the exact Bay 1 shaft aperture X in [+1.240, +3.150], Z in [+4.440, +6.680] open.
        /// </summary>
        private static void ReconstructBackroomsCeilingPerimeter(Transform originalCeiling, Transform group, ShaftMaterials mats)
        {
            float ceilMidY = (CeilingMinY + CeilingMaxY) * 0.5f; // -47.050f
            float fullZ = CeilingMaxZ - CeilingMinZ;             //  24.600f
            float centerZ = (CeilingMinZ + CeilingMaxZ) * 0.5f;  //   0.000f

            // 1. East Slab — original Backrooms_Ceiling retained in-place (preserving fileID & hierarchy)
            float eastSizeX = CeilingMaxX - ApertureMaxX;        // 17.150f
            float eastCenterX = (ApertureMaxX + CeilingMaxX) * 0.5f; // +11.725f
            originalCeiling.position = new Vector3(eastCenterX, ceilMidY, centerZ);
            originalCeiling.rotation = Quaternion.identity;
            originalCeiling.localScale = new Vector3(eastSizeX, CeilingThicknessY, fullZ);
            MeshRenderer origMr = originalCeiling.GetComponent<MeshRenderer>();
            if (origMr != null) origMr.sharedMaterial = mats.CeilingSoffit;
            BoxCollider origCol = originalCeiling.GetComponent<BoxCollider>();
            if (origCol == null) origCol = originalCeiling.gameObject.AddComponent<BoxCollider>();
            origCol.enabled = true;
            origCol.isTrigger = false;
            originalCeiling.gameObject.isStatic = true;

            // 2. West Slab: X in [-20.300, +1.240], Z in [-12.300, +12.300]
            float westSizeX = ApertureMinX - CeilingMinX;        // 21.540f
            float westCenterX = (CeilingMinX + ApertureMinX) * 0.5f; // -9.530f
            EnsureBox(group, "Backrooms_Ceiling_West",
                new Vector3(westCenterX, ceilMidY, centerZ), Quaternion.identity,
                new Vector3(westSizeX, CeilingThicknessY, fullZ), mats.CeilingSoffit, keepCollider: true);

            // 3. South Slab: X in [+1.240, +3.150], Z in [-12.300, +4.440]
            float bandCenterX = (ApertureMinX + ApertureMaxX) * 0.5f; // +2.195f
            float southSizeZ = ApertureMinZ - CeilingMinZ;            // 16.740f
            float southCenterZ = (CeilingMinZ + ApertureMinZ) * 0.5f; // -3.930f
            EnsureBox(group, "Backrooms_Ceiling_South",
                new Vector3(bandCenterX, ceilMidY, southCenterZ), Quaternion.identity,
                new Vector3(ApertureWidthX, CeilingThicknessY, southSizeZ), mats.CeilingSoffit, keepCollider: true);

            // 4. North Slab: starts at Z = +9.160 when 07.29 Bay 2 Open Shaft is present (leaving Bay 2 void open!),
            //    or Z = +6.920 in standalone 07.26 mode.
            bool hasBay2Open = group.parent != null && group.parent.Find(UpperPassengerElevatorDoorsBuilder.Bay2OpenShaftGroupName) != null;
            float northStartZ = hasBay2Open ? UpperPassengerElevatorDoorsBuilder.Bay2ShaftMaxZ : DividerMaxZ;
            float northSizeZ = CeilingMaxZ - northStartZ;
            float northCenterZ = (northStartZ + CeilingMaxZ) * 0.5f;
            EnsureBox(group, "Backrooms_Ceiling_North",
                new Vector3(bandCenterX, ceilMidY, northCenterZ), Quaternion.identity,
                new Vector3(ApertureWidthX, CeilingThicknessY, northSizeZ), mats.CeilingSoffit, keepCollider: true);
        }

        private static void BuildContinuousConcreteDivider(Transform group, ShaftMaterials mats)
        {
            float centerX = (DividerMinX + DividerMaxX) * 0.5f; // +2.195f
            float sizeX = DividerMaxX - DividerMinX;            //  1.910f
            float centerZ = (DividerMinZ + DividerMaxZ) * 0.5f; // +6.800f
            float sizeZ = DividerMaxZ - DividerMinZ;            //  0.240f
            float heightY = DividerMaxY - DividerMinY;          //  6.450f (-50.000 -> -43.550)
            float centerY = (DividerMinY + DividerMaxY) * 0.5f; // -46.775f

            // Continuous monolithic reinforced concrete divider separating Bay 1 from Bay 2
            // Connects solidly to BackWall (X=+1.240) and FrontPierMid (X=+3.150) with zero gap or pocket
            EnsureBox(group, "Bay1Shaft_DividerWall_North",
                new Vector3(centerX, centerY, centerZ), Quaternion.identity,
                new Vector3(sizeX, heightY, sizeZ), mats.ColumnConcrete, keepCollider: true);

            // Flush 0.200m concrete coping closing the roof-slab band (Y in [-43.550, -43.350]) directly above the divider
            EnsureBox(group, "Bay1Shaft_DividerTopCoping",
                new Vector3(centerX, (RoofCapMinY + RoofCapMaxY) * 0.5f, centerZ), Quaternion.identity,
                new Vector3(sizeX, RoofCapThicknessY, sizeZ), mats.ColumnConcrete, keepCollider: true);
        }

        private static void BuildUpperShaftEnclosureAndRoofCap(Transform group, ShaftMaterials mats)
        {
            // Upper shaft walls rise seamlessly from top of Backrooms_Ceiling (Y = -46.950) to top of roof cap (Y = -43.350),
            // sitting flush over Backrooms_Ceiling_West/South (which span Y in [-47.150, -46.950] over lower core walls)
            // with zero gap and zero overlapping geometry at every horizontal joint.
            float upperWallH = RoofCapMaxY - CeilingMaxY;           // 3.600f (-46.950 -> -43.350)
            float upperWallMidY = (CeilingMaxY + RoofCapMaxY) * 0.5f; // -45.150f

            // West / Back Shaft Wall: X in [+1.000, +1.240], Z in [+4.200, +6.920], Y in [-46.950, -43.350]
            float backWallSizeX = ApertureMinX - ShaftOuterMinX;    // 0.240f
            float backWallCenterX = (ShaftOuterMinX + ApertureMinX) * 0.5f; // +1.120f
            float backWallSizeZ = DividerMaxZ - ShaftOuterMinZ;     // 2.720f
            float backWallCenterZ = (ShaftOuterMinZ + DividerMaxZ) * 0.5f; // +5.560f
            EnsureBox(group, "UpperShaft_BackWall",
                new Vector3(backWallCenterX, upperWallMidY, backWallCenterZ), Quaternion.identity,
                new Vector3(backWallSizeX, upperWallH, backWallSizeZ), mats.ColumnConcrete, keepCollider: true);

            // South Shaft Wall: X in [+1.240, +3.150], Z in [+4.200, +4.440], Y in [-46.950, -43.350]
            float southWallSizeZ = ApertureMinZ - ShaftOuterMinZ;   // 0.240f
            float southWallCenterZ = (ShaftOuterMinZ + ApertureMinZ) * 0.5f; // +4.320f
            float bandCenterX = (ApertureMinX + ApertureMaxX) * 0.5f; // +2.195f
            EnsureBox(group, "UpperShaft_SouthWall",
                new Vector3(bandCenterX, upperWallMidY, southWallCenterZ), Quaternion.identity,
                new Vector3(ApertureWidthX, upperWallH, southWallSizeZ), mats.ColumnConcrete, keepCollider: true);

            // Sealed Upper Roof Cap: exact X in [+1.240, +3.150], Z in [+4.440, +6.680], Y in [-43.550, -43.350]
            // Flush with UpperElev_Waiting_CeilingSlab (X_min = +3.150, Y in [-43.550, -43.350])
            float capCenterZ = (ApertureMinZ + ApertureMaxZ) * 0.5f; // +5.560f
            EnsureBox(group, "UpperShaft_RoofCap",
                new Vector3(bandCenterX, (RoofCapMinY + RoofCapMaxY) * 0.5f, capCenterZ), Quaternion.identity,
                new Vector3(ApertureWidthX, RoofCapThicknessY, ApertureDepthZ), mats.ColumnConcrete, keepCollider: true);

            // East 10mm Plenum Seal closing the exact gap between Backrooms_Ceiling top (-46.950) and
            // UpperElev_Support_WaitingCorePlinthW bottom (-46.940) strictly outside the aperture (X in [+3.150, +3.350])
            float plenumSealMinY = CeilingMaxY; // -46.950f
            float plenumSealMaxY = UpperElevatorApproachBuilder.SupportPlenumBottomY; // -46.940f
            EnsureBox(group, "UpperShaft_EastPlenumSeal",
                new Vector3((BackingWallMinX + BackingWallMaxX) * 0.5f, (plenumSealMinY + plenumSealMaxY) * 0.5f, capCenterZ),
                Quaternion.identity,
                new Vector3(BackingWallMaxX - BackingWallMinX, plenumSealMaxY - plenumSealMinY, ApertureDepthZ),
                mats.ColumnConcrete, keepCollider: true);
        }

        /// <summary>
        /// Trims existing UpperElev_Waiting_WestCoreBackingWall in-place to the solid North section
        /// (Z in [+6.435, +9.400], keeping Bay 2 100% solid and preserving fileID) and adds the
        /// South pier (Z in [+4.200, +5.065]) and upper transom (Z in [+5.065, +6.435], Y in [-44.200, -43.550])
        /// around the future Bay 1 landing opening (Z in [+5.065, +6.435], Y in [-46.400, -44.200]).
        /// Existing UpperElev_Waiting_FloorSlab cleanly seals Y in [-46.600, -46.400], and the 07.24
        /// closed Bay 1 finished doors &amp; UpperElev_SealedBay_1_SealedBackingPanel remain intact.
        /// </summary>
        private static void SplitUpperBay1BackingWall(Transform backingWall, Transform group, ShaftMaterials mats)
        {
            float wallCenterX = (BackingWallMinX + BackingWallMaxX) * 0.5f; // +3.250f
            float wallThickX = BackingWallMaxX - BackingWallMinX;           //  0.200f
            float fullWallMinY = UpperElevatorApproachBuilder.SlabBottomY;  // -46.600f
            float fullWallMaxY = UpperElevatorApproachBuilder.CeilingSoffitY; // -43.550f
            float fullWallH = fullWallMaxY - fullWallMinY;                  //  3.050f
            float fullWallMidY = (fullWallMinY + fullWallMaxY) * 0.5f;      // -45.075f

            // 1. Retain UpperElev_Waiting_WestCoreBackingWall in-place: Mid Pier (Z in [+6.435, +7.165]) when 07.29
            //    Bay 2 Open Shaft is installed, or full North section (Z in [+6.435, +9.400]) in standalone 07.26 mode.
            bool hasBay2Open = group.parent != null && group.parent.Find(UpperPassengerElevatorDoorsBuilder.Bay2OpenShaftGroupName) != null;
            float northMinZ = UpperPortalMaxZ;                               // +6.435f
            float northMaxZ = hasBay2Open ? UpperPassengerElevatorDoorsBuilder.Bay2PortalMinZ : UpperElevatorApproachBuilder.WaitingMaxZ;
            float northLenZ = northMaxZ - northMinZ;
            float northCenterZ = (northMinZ + northMaxZ) * 0.5f;
            backingWall.position = new Vector3(wallCenterX, fullWallMidY, northCenterZ);
            backingWall.rotation = Quaternion.identity;
            backingWall.localScale = new Vector3(wallThickX, fullWallH, northLenZ);
            BoxCollider northCol = backingWall.GetComponent<BoxCollider>();
            if (northCol == null) northCol = backingWall.gameObject.AddComponent<BoxCollider>();
            northCol.enabled = true;
            northCol.isTrigger = false;

            // 2. South Backing Pier: X in [+3.150, +3.350], Z in [+4.200, +5.065], Y in [-46.400, -43.550]
            float southMinZ = UpperElevatorApproachBuilder.WaitingMinZ;      // +4.200f
            float southMaxZ = UpperPortalMinZ;                               // +5.065f
            float southLenZ = southMaxZ - southMinZ;                         //  0.865f
            float southCenterZ = (southMinZ + southMaxZ) * 0.5f;             // +4.6325f
            float aboveSlabH = fullWallMaxY - UpperPortalMinY;               //  2.850f
            float aboveSlabMidY = (UpperPortalMinY + fullWallMaxY) * 0.5f;   // -44.975f
            EnsureBox(group, "Bay1UpperBacking_SouthPier",
                new Vector3(wallCenterX, aboveSlabMidY, southCenterZ), Quaternion.identity,
                new Vector3(wallThickX, aboveSlabH, southLenZ), mats.ColumnConcrete, keepCollider: true);

            // 3. Upper Transom above Bay 1 portal: X in [+3.150, +3.350], Z in [+5.065, +6.435], Y in [-44.200, -43.550]
            float portalLenZ = UpperPortalMaxZ - UpperPortalMinZ;            //  1.370f
            float portalCenterZ = (UpperPortalMinZ + UpperPortalMaxZ) * 0.5f; // +5.750f
            float transomH = fullWallMaxY - UpperPortalMaxY;                 //  0.650f
            float transomMidY = (UpperPortalMaxY + fullWallMaxY) * 0.5f;     // -43.875f
            EnsureBox(group, "Bay1UpperBacking_Transom",
                new Vector3(wallCenterX, transomMidY, portalCenterZ), Quaternion.identity,
                new Vector3(wallThickX, transomH, portalLenZ), mats.ColumnConcrete, keepCollider: true);
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
                go.transform.SetParent(parent, false);
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

        // =====================================================================
        // READ-ONLY DIAGNOSTIC CUTAWAY VISUAL VERIFICATION (07.26 — NEVER SAVES SCENE)
        // =====================================================================
        public static void RenderBay1ShaftDiagnosticViews()
        {
            string outDir = Environment.GetEnvironmentVariable("REMANENCIA_0726_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "scratch_renders_0726"));
            }
            Directory.CreateDirectory(outDir);

            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath} for 07.26 diagnostic renders.");
            }

            GameObject camGo = new GameObject("__Diag0726Cam__");
            GameObject keyLightGo = new GameObject("__Diag0726KeyLight__");
            GameObject fillLightGo = new GameObject("__Diag0726FillLight__");
            List<(MeshRenderer renderer, bool wasEnabled)> hiddenRenderers = new List<(MeshRenderer, bool)>();
            List<(MeshRenderer renderer, Material originalMat)> tintedRenderers = new List<(MeshRenderer, Material)>();
            List<Material> tempMaterials = new List<Material>();

            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.055f, 0.065f, 1f);
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 80f;
                cam.fieldOfView = 58f;

                // Temporary read-only studio lights strictly for cutaway inspection (never saved, destroyed in finally)
                Light keyLight = keyLightGo.AddComponent<Light>();
                keyLight.type = LightType.Point;
                keyLight.range = 18f;
                keyLight.intensity = 5.5f;
                keyLight.color = new Color(1.00f, 0.96f, 0.90f);
                keyLight.shadows = LightShadows.None;

                Light fillLight = fillLightGo.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.range = 16f;
                fillLight.intensity = 3.2f;
                fillLight.color = new Color(0.75f, 0.86f, 0.98f);
                fillLight.shadows = LightShadows.None;

                // Subtle temporary diagnostic color-coding tints for cutaway clarity (read-only, never saved)
                Material MakeTint(Color c)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    Material m = new Material(sh);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    else m.color = c;
                    tempMaterials.Add(m);
                    return m;
                }

                Material tintDivider = MakeTint(new Color(0.85f, 0.38f, 0.24f, 1f));    // Terracotta-orange concrete divider
                Material tintRoofCap = MakeTint(new Color(0.28f, 0.62f, 0.82f, 1f));    // Steel-blue roof cap
                Material tintShaftWall = MakeTint(new Color(0.56f, 0.60f, 0.64f, 1f));  // Cool concrete upper shaft walls
                Material tintCeiling = MakeTint(new Color(0.78f, 0.76f, 0.68f, 1f));    // Warm acoustic ceiling slabs
                Material tintBacking = MakeTint(new Color(0.66f, 0.56f, 0.34f, 1f));    // Ochre split backing wall pieces

                void HideByName(params string[] names)
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        GameObject go = GameObject.Find(names[i]);
                        if (go == null) continue;
                        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                        {
                            hiddenRenderers.Add((mr, mr.enabled));
                            mr.enabled = false;
                        }
                    }
                }

                void RestoreHidden()
                {
                    for (int i = hiddenRenderers.Count - 1; i >= 0; i--)
                    {
                        if (hiddenRenderers[i].renderer != null)
                        {
                            hiddenRenderers[i].renderer.enabled = hiddenRenderers[i].wasEnabled;
                        }
                    }
                    hiddenRenderers.Clear();
                }

                void ApplyTint(string objectName, Material tintMat)
                {
                    GameObject go = GameObject.Find(objectName);
                    if (go == null) return;
                    MeshRenderer mr = go.GetComponent<MeshRenderer>();
                    if (mr == null) return;
                    tintedRenderers.Add((mr, mr.sharedMaterial));
                    mr.sharedMaterial = tintMat;
                }

                void RestoreTints()
                {
                    for (int i = tintedRenderers.Count - 1; i >= 0; i--)
                    {
                        if (tintedRenderers[i].renderer != null)
                        {
                            tintedRenderers[i].renderer.sharedMaterial = tintedRenderers[i].originalMat;
                        }
                    }
                    tintedRenderers.Clear();
                }

                // Apply clear architectural colorway across 07.26 new structural elements for cutaway readability
                ApplyTint("Bay1Shaft_DividerWall_North", tintDivider);
                ApplyTint("Bay1Shaft_DividerTopCoping", tintDivider);
                ApplyTint("UpperShaft_RoofCap", tintRoofCap);
                ApplyTint("UpperShaft_BackWall", tintShaftWall);
                ApplyTint("UpperShaft_SouthWall", tintShaftWall);
                ApplyTint("Backrooms_Ceiling", tintCeiling);
                ApplyTint("Backrooms_Ceiling_West", tintCeiling);
                ApplyTint("Backrooms_Ceiling_South", tintCeiling);
                ApplyTint("Backrooms_Ceiling_North", tintCeiling);
                ApplyTint("Bay1UpperBacking_SouthPier", tintBacking);
                ApplyTint("Bay1UpperBacking_Transom", tintBacking);
                ApplyTint("UpperElev_Waiting_WestCoreBackingWall", tintBacking);

                // View 1: Top-down cutaway showing Bay 1 / Bay 2 continuous concrete divider & 4-piece Backrooms_Ceiling aperture
                HideByName("UpperShaft_RoofCap", "Bay1Shaft_DividerTopCoping", "UpperElev_Waiting_CeilingSlab",
                    "UpperElev_Vestibule_CeilingSlab", "UpperElev_Waiting_Strip_Backplate", "UpperElev_Waiting_Strip_Tube");
                keyLightGo.transform.position = new Vector3(2.20f, -44.20f, 6.20f);
                fillLightGo.transform.position = new Vector3(2.20f, -48.20f, 5.56f);
                RenderCutaway(cam, new Vector3(2.40f, -42.10f, 6.45f), new Vector3(2.20f, -48.60f, 6.50f),
                    56f, Path.Combine(outDir, "verify_0726_01_topdown_divider_and_ceiling_aperture.png"));
                RestoreHidden();

                // View 2: Vertical cutaway section of Bay 1 shaft showing Backrooms_Ceiling aperture, continuous divider & sealed roof cap
                HideByName("UpperShaft_SouthWall", "Hotel_ElevatorCore_SouthWall", "UpperElev_Waiting_WallSouth",
                    "UpperElev_Support_WaitingGirderS", "Backrooms_Ceiling_South", "Hotel_Lobby_Beam_West");
                keyLightGo.transform.position = new Vector3(2.20f, -45.20f, 3.60f);
                fillLightGo.transform.position = new Vector3(2.20f, -48.40f, 3.80f);
                RenderCutaway(cam, new Vector3(2.25f, -46.70f, 1.45f), new Vector3(2.20f, -46.65f, 6.00f),
                    66f, Path.Combine(outDir, "verify_0726_02_vertical_section_aperture_and_roof_cap.png"));
                RestoreHidden();

                // View 3: Upper Bay 1 landing cutaway from inside shaft looking East — shows split backing wall portal
                //         AND preserved closed 07.24 finished doors behind it (with realistic production materials restored!)
                RestoreTints();
                HideByName("UpperShaft_BackWall", "Hotel_ElevatorCore_BackWall");
                keyLightGo.transform.position = new Vector3(1.80f, -44.80f, 5.75f);
                fillLightGo.transform.position = new Vector3(4.50f, -44.80f, 5.75f);
                RenderCutaway(cam, new Vector3(0.45f, -45.10f, 5.75f), new Vector3(3.35f, -45.15f, 5.75f),
                    56f, Path.Combine(outDir, "verify_0726_03_upper_landing_split_backing_and_closed_facade.png"));
                RestoreHidden();

                // View 4: Bay 2 isolation cutaway from North-West showing Backrooms_Ceiling_North capping Bay 2,
                //         continuous concrete divider wall, and untouched Bay 2 solid backing & unfinished facade
                ApplyTint("Bay1Shaft_DividerWall_North", tintDivider);
                ApplyTint("Bay1Shaft_DividerTopCoping", tintDivider);
                ApplyTint("Backrooms_Ceiling_North", tintCeiling);
                ApplyTint("UpperElev_Waiting_WestCoreBackingWall", tintBacking);
                HideByName("Hotel_ElevatorCore_NorthWall", "Backrooms_Ceiling_West", "UpperElev_Vestibule_WallWest",
                    "UpperElev_Vestibule_FloorSlab", "UpperElev_Vestibule_CeilingSlab", "UpperElev_Support_VestibuleSouthGirder");
                keyLightGo.transform.position = new Vector3(1.60f, -45.50f, 10.20f);
                fillLightGo.transform.position = new Vector3(2.20f, -48.40f, 8.80f);
                RenderCutaway(cam, new Vector3(-0.30f, -45.60f, 11.40f), new Vector3(2.40f, -46.80f, 7.60f),
                    58f, Path.Combine(outDir, "verify_0726_04_bay2_isolation_divider_and_north_cap.png"));
                RestoreHidden();
                RestoreTints();
            }
            finally
            {
                for (int i = hiddenRenderers.Count - 1; i >= 0; i--)
                {
                    if (hiddenRenderers[i].renderer != null) hiddenRenderers[i].renderer.enabled = hiddenRenderers[i].wasEnabled;
                }
                for (int i = tintedRenderers.Count - 1; i >= 0; i--)
                {
                    if (tintedRenderers[i].renderer != null) tintedRenderers[i].renderer.sharedMaterial = tintedRenderers[i].originalMat;
                }
                for (int i = 0; i < tempMaterials.Count; i++)
                {
                    if (tempMaterials[i] != null) UnityEngine.Object.DestroyImmediate(tempMaterials[i]);
                }
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(keyLightGo);
                UnityEngine.Object.DestroyImmediate(fillLightGo);
            }

            Debug.Log($"[UpperPassengerElevatorShaftBuilder] Rendered 4 cutaway diagnostic views to {outDir}.");
        }

        private static void RenderCutaway(Camera cam, Vector3 position, Vector3 lookTarget, float fov, string outputPath)
        {
            const int width = 1280;
            const int height = 720;

            cam.fieldOfView = fov;
            cam.transform.position = position;
            cam.transform.LookAt(lookTarget, Vector3.up);

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = prev;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
