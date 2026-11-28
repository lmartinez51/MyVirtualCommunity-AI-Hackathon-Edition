using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.29:
    ///   1. Bay 1 (South, Z = +5.750): Complete three-assembly two-speed telescopic door system
    ///      (Cabin, Lower Landing, Upper Landing), thresholds/sills, pure C# interlock controller,
    ///      threshold anti-pinch CheckBox, and bilingual [E] player interaction for controlled
    ///      lower-floor boarding/inspection (cabin stationary at Nivel 0; upper landing locked).
    ///   2. Bay 2 (North, Z = +7.850): Permanently open, abandoned, never-completed elevator shaft
    ///      (supersedes earlier sealed-Bay-2 notes). No cabin, no operational doors, no closing
    ///      mechanism, no invisible barriers. Lower &amp; upper landings open cleanly into a real
    ///      continuous vertical void (Y in [-50.000, -43.550]) with exposed incomplete guide-rail
    ///      stubs, unfinished brackets, and hanging suspension cables (visual-only, zero colliders
    ///      in the primary falling/traversal volume), plus a reusable future-floor contract helper.
    ///
    /// Preserves the 0.240m full-height Bay 1 / Bay 2 concrete divider (Z in [+6.680, +6.920]),
    /// Hotel_Elevator_Core's exact 35 children and fileIDs, uncut Backrooms_Floor, exact 18 Unity
    /// Lights, BorderCorridorScene.unity, and Threshold of Trust exit behavior.
    /// </summary>
    public static class UpperPassengerElevatorDoorsBuilder
    {
        public const string DoorsRootName = BackroomsLevel1Builder.UpperPassengerElevatorDoorsRootName;
        public const string Bay2OpenShaftGroupName = "Bay2Shaft_OpenUnfinished";
        public const string CabinDoorsGroupName = "Cabin_Doors";

        // Bay 1 Clear Walk-Through Portal (Z in [+5.300, +6.400], 1.100m wide)
        public const float Bay1PortalMinZ = 5.300f;
        public const float Bay1PortalMaxZ = 6.400f;
        public const float Bay1OpeningMinZ = 5.065f;
        public const float Bay1OpeningMaxZ = 6.435f;

        // Bay 2 Permanent Open Shaft & Unfinished Landing Bounds (Z in [+6.920, +9.160])
        public const float Bay2ShaftMinX = UpperPassengerElevatorShaftBuilder.ApertureMinX; // +1.240f
        public const float Bay2ShaftMaxX = UpperPassengerElevatorShaftBuilder.ApertureMaxX; // +3.150f
        public const float Bay2ShaftMinZ = UpperPassengerElevatorShaftBuilder.DividerMaxZ;  // +6.920f
        public const float Bay2ShaftMaxZ = 9.160f;                                          // Hotel_ElevatorCore_NorthWall inner face
        public const float Bay2OuterNorthZ = 9.400f;
        public const float Bay2PortalMinZ = 7.165f;                                         // Clear 1.370m open portal
        public const float Bay2PortalMaxZ = 8.535f;
        public const float Bay2ShaftBottomY = BackroomsLevel1Builder.BaseY;                 // -50.000f (uncut Backrooms_Floor)
        public const float Bay2ShaftRoofSoffitY = UpperElevatorApproachBuilder.CeilingSoffitY; // -43.550f
        public const float Bay2ShaftRoofTopY = UpperElevatorApproachBuilder.CeilingTopY;       // -43.350f

        private struct DoorMaterials
        {
            public Material ElevatorDoorMetal;
            public Material IndustrialMetal;
            public Material ColumnConcrete;
            public Material CeilingSoffit;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.29 Bay 1 Doors & Bay 2 Open Shaft")]
        public static void MigrateElevatorDoorsAndBay2OpenShaftAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateElevatorDoorsAndBay2OpenShaftInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.29 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UpperPassengerElevatorDoorsBuilder] Implementation 07.29 Bay 1 Functional Doors & Bay 2 Permanently Open Shaft migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.29.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateElevatorDoorsAndBay2OpenShaftInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UpperPassengerElevatorDoorsBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
            }

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            if (root == null) throw new InvalidOperationException("BackroomsLevel1_Root not found.");

            Transform shell = root.transform.Find("Backrooms_Architecture");
            if (shell == null) throw new InvalidOperationException("Backrooms_Architecture not found.");

            if (!UpperPassengerElevatorShaftBuilder.VerifyStructurePresent(shell) ||
                !UpperPassengerElevatorCabinBuilder.VerifyStructurePresent(shell))
            {
                throw new InvalidOperationException("Expected 07.26 Shaft and 07.27 Cabin before running 07.29 migration.");
            }

            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Transform cabinRoot = shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName);
            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);
            GameObject elevCoreGo = GameObject.Find("Hotel_Elevator_Core");
            if (elevCoreGo == null) throw new InvalidOperationException("Hotel_Elevator_Core not found.");

            DoorMaterials mats = LoadExistingMaterials();

            // =================================================================
            // PART 1: BAY 1 FUNCTIONAL THREE-ASSEMBLY TELESCOPIC DOOR SYSTEM
            // =================================================================
            Transform doorsRoot = EnsureSubgroup(shell, DoorsRootName, isStatic: false);

            // 1A. Build Cabin Doors (non-static child of movable Cabin root)
            RemanenciaElevatorDoorAssembly cabinAsm = BuildCabinDoors(cabinRoot, mats);

            // 1B. Build Lower Landing Doors + Threshold Plate + Returns
            RemanenciaElevatorDoorAssembly lowerAsm = BuildLowerLandingDoors(doorsRoot, mats);

            // 1C. Build Upper Landing Doors (Locked in 07.29) + Sill Extension + Returns
            RemanenciaElevatorDoorAssembly upperAsm = BuildUpperLandingDoors(doorsRoot, mats);

            // 1D. Wire Bay 1 Door Controller (explicit references — NEVER touches Bay 2!)
            RemanenciaElevatorDoorController controller = doorsRoot.GetComponent<RemanenciaElevatorDoorController>();
            if (controller == null) controller = doorsRoot.gameObject.AddComponent<RemanenciaElevatorDoorController>();
            controller.cabinRoot = cabinRoot;
            controller.cabinDoors = cabinAsm;
            controller.lowerLandingDoors = lowerAsm;
            controller.upperLandingDoors = upperAsm;
            controller.allowUpperLandingOperation = false; // Upper landing locked throughout 07.29
            controller.moveDurationSeconds = RemanenciaElevatorDoorInterlock.DefaultMoveDurationSeconds;

            // 1E. Wire Bay 1 Bilingual [E] Interactables (Lower Hall Panel & Inside Cabin ONLY; zero Bay 2 interactable!)
            BuildBay1DoorInteractables(doorsRoot, cabinRoot, controller);

            // 1F. ONLY AFTER replacement Bay 1 doors exist, disable legacy static Bay 1 leaf renderers/colliders
            //     and Upper Bay 1 sealed backing collider (preserving all original GOs and 35 core children!)
            DisableLegacyBay1StaticDoorComponents(elevCoreGo.transform, approachRoot);

            // =================================================================
            // PART 2: BAY 2 PERMANENTLY OPEN UNFINISHED SHAFT (NO CABIN, NO DOORS)
            // =================================================================
            BuildBay2PermanentlyOpenShaft(shaftRoot, elevCoreGo.transform, approachRoot, mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform doorsRoot = architectureShell.Find(DoorsRootName);
            Transform cabinRoot = architectureShell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName);
            Transform shaftRoot = architectureShell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            if (doorsRoot == null || cabinRoot == null || shaftRoot == null) return false;

            Transform bay2Open = shaftRoot.Find(Bay2OpenShaftGroupName);
            return doorsRoot.GetComponent<RemanenciaElevatorDoorController>() != null &&
                   doorsRoot.Find("Bay1Doors_LowerLanding/LowerDoor_FastLeaf") != null &&
                   doorsRoot.Find("Bay1Doors_LowerLanding/LowerDoor_SlowLeaf") != null &&
                   doorsRoot.Find("Bay1Doors_UpperLanding/UpperDoor_FastLeaf") != null &&
                   doorsRoot.Find("Bay1Doors_UpperLanding/UpperDoor_SlowLeaf") != null &&
                   doorsRoot.Find("Bay1Doors_UpperLanding/UpperDoor_SillExtension") != null &&
                   cabinRoot.Find($"{CabinDoorsGroupName}/CabinDoor_FastLeaf") != null &&
                   cabinRoot.Find($"{CabinDoorsGroupName}/CabinDoor_SlowLeaf") != null &&
                   bay2Open != null &&
                   bay2Open.Find("Bay2Shaft_BackWall") != null &&
                   bay2Open.Find("Bay2Shaft_NorthWall") != null &&
                   bay2Open.Find("Bay2Shaft_RoofCap") != null &&
                   bay2Open.Find("Bay2UpperLanding_OpenSurround") != null;
        }

        // =====================================================================
        // REUSABLE FUTURE-FLOOR CONTRACT HELPER FOR BAY 2 (SECTIONS 2.2.4 & 9.4)
        // =====================================================================
        /// <summary>
        /// Narrowly scoped, idempotent helper enforcing the canonical Bay 2 architectural contract
        /// for any existing or future floor connecting to Bay 2 (North, Z in [+6.920, +9.160]):
        ///
        ///   CONTRACT: Every Bay 2 landing MUST be permanently open by construction, with NO cabin,
        ///   NO door leaves, NO backing panel, NO closing controller, and NO floor slab intruding
        ///   into the Bay 2 vertical shaft (X &lt; +3.150).
        ///
        /// Accepts arbitrary floor elevation and story height (never hardcoding current floor heights).
        /// Builds only the structural opening surround strictly East of the shaft (X in [+3.150, +3.350]):
        ///   - North backing pier (Z in [+8.535, +9.400], full story height)
        ///   - Upper transom above the open doorway (Z in [+7.165, +8.535], Y in [floorY + clearPortalHeightY, ceilingSoffitY])
        ///   - Unfinished concrete sill edge strip strictly East of X = +3.150 (non-colliding trim)
        /// Leaves the full 1.370 m x clearPortalHeightY portal (Z in [+7.165, +8.535]) completely open.
        /// </summary>
        public static Transform EnsureBay2OpenLandingOpening(
            Transform parent,
            string groupName,
            float floorElevationY,
            float clearPortalHeightY,
            float ceilingSoffitY,
            Material concreteMat,
            Material trimMetalMat = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (clearPortalHeightY <= 0.5f) throw new ArgumentOutOfRangeException(nameof(clearPortalHeightY));
            if (ceilingSoffitY < floorElevationY + clearPortalHeightY)
            {
                throw new ArgumentException("ceilingSoffitY must be >= floorElevationY + clearPortalHeightY.");
            }

            Transform landingGroup = EnsureSubgroup(parent, groupName, isStatic: true);

            const float wallMinX = UpperPassengerElevatorShaftBuilder.BackingWallMinX; // +3.150f
            const float wallMaxX = UpperPassengerElevatorShaftBuilder.BackingWallMaxX; // +3.350f
            float wallCenterX = (wallMinX + wallMaxX) * 0.5f;                          // +3.250f
            float wallThickX = wallMaxX - wallMinX;                                    //  0.200f

            float storyH = ceilingSoffitY - floorElevationY;
            float storyMidY = (floorElevationY + ceilingSoffitY) * 0.5f;

            // North structural pier framing the open Bay 2 landing (Z in [+8.535, +9.400])
            float northLenZ = Bay2OuterNorthZ - Bay2PortalMaxZ; // 0.865f
            EnsureBox(landingGroup, $"{groupName}_NorthPier",
                new Vector3(wallCenterX, storyMidY, (Bay2PortalMaxZ + Bay2OuterNorthZ) * 0.5f),
                new Vector3(wallThickX, storyH, northLenZ),
                concreteMat, keepCollider: true, isStatic: true);

            // Transom directly above the open portal (Z in [+7.165, +8.535], Y in [portalTopY, ceilingSoffitY])
            float portalTopY = floorElevationY + clearPortalHeightY;
            float transomH = ceilingSoffitY - portalTopY;
            if (transomH > 0.005f)
            {
                EnsureBox(landingGroup, $"{groupName}_Transom",
                    new Vector3(wallCenterX, (portalTopY + ceilingSoffitY) * 0.5f, (Bay2PortalMinZ + Bay2PortalMaxZ) * 0.5f),
                    new Vector3(wallThickX, transomH, Bay2PortalMaxZ - Bay2PortalMinZ),
                    concreteMat, keepCollider: true, isStatic: true);
            }

            // Unfinished threshold lip flush at floor edge strictly East of X = +3.150 (never protrudes into shaft!)
            if (trimMetalMat != null)
            {
                EnsureBox(landingGroup, $"{groupName}_UnfinishedEdgeAngle",
                    new Vector3(wallMinX + 0.025f, floorElevationY - 0.015f, (Bay2PortalMinZ + Bay2PortalMaxZ) * 0.5f),
                    new Vector3(0.050f, 0.030f, Bay2PortalMaxZ - Bay2PortalMinZ),
                    trimMetalMat, keepCollider: false, isStatic: true);
            }

            return landingGroup;
        }

        // =====================================================================
        // BAY 1 DOOR ASSEMBLIES
        // =====================================================================
        private static RemanenciaElevatorDoorAssembly BuildCabinDoors(Transform cabinRoot, DoorMaterials mats)
        {
            Transform grp = EnsureSubgroup(cabinRoot, CabinDoorsGroupName, isStatic: false);
            float doorMinY = UpperPassengerElevatorCabinBuilder.DeckTopY;    // -49.960f
            float doorMaxY = UpperPassengerElevatorCabinBuilder.PortalMaxY;  // -47.760f
            float doorH = doorMaxY - doorMinY;                               //   2.200f
            float doorMidY = (doorMinY + doorMaxY) * 0.5f;                   // -48.860f

            // Cabin Slow Leaf: X [2.965, 2.990], Y [-49.960, -47.760], Closed Z [5.270, 5.850]
            GameObject slowGo = EnsureBox(grp, "CabinDoor_SlowLeaf",
                new Vector3(2.9775f, doorMidY, 5.560f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Cabin Fast Leaf: X [3.000, 3.025], Y [-49.960, -47.760], Closed Z [5.820, 6.400]
            GameObject fastGo = EnsureBox(grp, "CabinDoor_FastLeaf",
                new Vector3(3.0125f, doorMidY, 6.110f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Overhead Cabin Door Track & Hanger Housing: X [2.965, 3.025], Y [-47.760, -47.720], Z [4.680, 6.405]
            EnsureBox(grp, "CabinDoor_TrackHeader",
                new Vector3(2.995f, -47.740f, (4.680f + 6.405f) * 0.5f),
                new Vector3(0.060f, 0.040f, 6.405f - 4.680f),
                mats.IndustrialMetal, keepCollider: false, isStatic: false);

            RemanenciaElevatorDoorAssembly asm = grp.GetComponent<RemanenciaElevatorDoorAssembly>();
            if (asm == null) asm = grp.gameObject.AddComponent<RemanenciaElevatorDoorAssembly>();
            asm.ConfigureGeometry("Bay1_CabinDoors", ElevatorLandingId.Lower, true,
                slowGo.transform, fastGo.transform, slowGo.transform.localPosition, fastGo.transform.localPosition);
            EnforceNonStaticRecursive(grp);
            return asm;
        }

        private static RemanenciaElevatorDoorAssembly BuildLowerLandingDoors(Transform doorsRoot, DoorMaterials mats)
        {
            Transform grp = EnsureSubgroup(doorsRoot, "Bay1Doors_LowerLanding", isStatic: false);
            const float doorMinY = -49.976f;
            const float doorMaxY = -47.800f;
            float doorH = doorMaxY - doorMinY;            // 2.176f
            float doorMidY = (doorMinY + doorMaxY) * 0.5f; // -48.888f

            // Lower Landing Fast Leaf: X [3.060, 3.085], Y [-49.976, -47.800], Closed Z [5.820, 6.400]
            GameObject fastGo = EnsureBox(grp, "LowerDoor_FastLeaf",
                new Vector3(3.0725f, doorMidY, 6.110f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Lower Landing Slow Leaf: X [3.095, 3.120], Y [-49.976, -47.800], Closed Z [5.270, 5.850]
            GameObject slowGo = EnsureBox(grp, "LowerDoor_SlowLeaf",
                new Vector3(3.1075f, doorMidY, 5.560f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Static Track Header, South Sidelite Return, North Strike Return & Stationary Lower Threshold Plate
            EnsureBox(grp, "LowerDoor_TrackHeader",
                new Vector3(3.090f, -47.760f, (4.680f + 6.435f) * 0.5f),
                new Vector3(0.060f, 0.080f, 6.435f - 4.680f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);

            EnsureBox(grp, "LowerDoor_SideliteReturnS",
                new Vector3(3.250f, -48.900f, (Bay1OpeningMinZ + Bay1PortalMinZ) * 0.5f),
                new Vector3(0.200f, 2.200f, Bay1PortalMinZ - Bay1OpeningMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);

            EnsureBox(grp, "LowerDoor_StrikeReturnN",
                new Vector3(3.250f, -48.900f, (Bay1PortalMaxZ + Bay1OpeningMaxZ) * 0.5f),
                new Vector3(0.200f, 2.200f, Bay1OpeningMaxZ - Bay1PortalMaxZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);

            // Stationary Lower Threshold Plate: X [3.055, 3.150], Y [-50.000, -49.976], Z [5.065, 6.435]
            // Preserves 30mm horizontal running gap [3.025, 3.055] over solid uncut Backrooms_Floor (-50.000)
            EnsureBox(grp, "LowerDoor_ThresholdPlate",
                new Vector3((3.055f + 3.150f) * 0.5f, (-50.000f + -49.976f) * 0.5f, (Bay1OpeningMinZ + Bay1OpeningMaxZ) * 0.5f),
                new Vector3(0.095f, 0.024f, Bay1OpeningMaxZ - Bay1OpeningMinZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            RemanenciaElevatorDoorAssembly asm = grp.GetComponent<RemanenciaElevatorDoorAssembly>();
            if (asm == null) asm = grp.gameObject.AddComponent<RemanenciaElevatorDoorAssembly>();
            asm.ConfigureGeometry("Bay1_LowerLandingDoors", ElevatorLandingId.Lower, false,
                slowGo.transform, fastGo.transform, slowGo.transform.localPosition, fastGo.transform.localPosition);
            return asm;
        }

        private static RemanenciaElevatorDoorAssembly BuildUpperLandingDoors(Transform doorsRoot, DoorMaterials mats)
        {
            Transform grp = EnsureSubgroup(doorsRoot, "Bay1Doors_UpperLanding", isStatic: false);
            const float doorMinY = -46.400f;
            const float doorMaxY = -44.200f;
            float doorH = doorMaxY - doorMinY;            // 2.200f
            float doorMidY = (doorMinY + doorMaxY) * 0.5f; // -45.300f

            // Upper Landing Fast Leaf: X [3.060, 3.085], Y [-46.400, -44.200], Closed Z [5.820, 6.400]
            GameObject fastGo = EnsureBox(grp, "UpperDoor_FastLeaf",
                new Vector3(3.0725f, doorMidY, 6.110f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Upper Landing Slow Leaf: X [3.095, 3.120], Y [-46.400, -44.200], Closed Z [5.270, 5.850]
            GameObject slowGo = EnsureBox(grp, "UpperDoor_SlowLeaf",
                new Vector3(3.1075f, doorMidY, 5.560f),
                new Vector3(0.025f, doorH, 0.580f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            EnsureBox(grp, "UpperDoor_TrackHeader",
                new Vector3(3.090f, -44.160f, (4.680f + 6.435f) * 0.5f),
                new Vector3(0.060f, 0.080f, 6.435f - 4.680f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);

            EnsureBox(grp, "UpperDoor_SideliteReturnS",
                new Vector3(3.250f, doorMidY, (Bay1OpeningMinZ + Bay1PortalMinZ) * 0.5f),
                new Vector3(0.200f, doorH, Bay1PortalMinZ - Bay1OpeningMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);

            EnsureBox(grp, "UpperDoor_StrikeReturnN",
                new Vector3(3.250f, doorMidY, (Bay1PortalMaxZ + Bay1OpeningMaxZ) * 0.5f),
                new Vector3(0.200f, doorH, Bay1OpeningMaxZ - Bay1PortalMaxZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);

            // Stationary Upper Sill Extension: X [3.055, 3.150], Y [-46.480, -46.400], Z [5.065, 6.435]
            // Flush with UpperElev_Waiting_FloorSlab (-46.400), preserves 30mm running clearance to Cabin (+3.025)
            EnsureBox(grp, "UpperDoor_SillExtension",
                new Vector3((3.055f + 3.150f) * 0.5f, (-46.480f + -46.400f) * 0.5f, (Bay1OpeningMinZ + Bay1OpeningMaxZ) * 0.5f),
                new Vector3(0.095f, 0.080f, Bay1OpeningMaxZ - Bay1OpeningMinZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            RemanenciaElevatorDoorAssembly asm = grp.GetComponent<RemanenciaElevatorDoorAssembly>();
            if (asm == null) asm = grp.gameObject.AddComponent<RemanenciaElevatorDoorAssembly>();
            asm.ConfigureGeometry("Bay1_UpperLandingDoors", ElevatorLandingId.Upper, false,
                slowGo.transform, fastGo.transform, slowGo.transform.localPosition, fastGo.transform.localPosition);
            return asm;
        }

        private static void BuildBay1DoorInteractables(
            Transform doorsRoot,
            Transform cabinRoot,
            RemanenciaElevatorDoorController controller)
        {
            // Lower Hall Panel anchor (X=3.44, Y=-48.82, Z=6.80; strictly Bay-1 gated inside script!)
            Transform hallGo = EnsureSubgroup(doorsRoot, "Bay1Door_Interact_LowerHallPanel", isStatic: false,
                worldPos: new Vector3(3.440f, -48.820f, 6.800f));
            var hallInt = hallGo.GetComponent<RemanenciaElevatorDoorInteractable>();
            if (hallInt == null) hallInt = hallGo.gameObject.AddComponent<RemanenciaElevatorDoorInteractable>();
            hallInt.doorController = controller;
            hallInt.zone = ElevatorDoorInteractZone.LowerHallPanel;
            hallInt.interactionRadius = 2.20f;

            // Inside-Cabin interaction anchor (child of movable cabinRoot so it travels with cabin in 07.30!)
            Transform cabGo = EnsureSubgroup(cabinRoot, "Bay1Door_Interact_CabinInterior", isStatic: false,
                worldPos: new Vector3(2.600f, -48.800f, 5.400f));
            var cabInt = cabGo.GetComponent<RemanenciaElevatorDoorInteractable>();
            if (cabInt == null) cabInt = cabGo.gameObject.AddComponent<RemanenciaElevatorDoorInteractable>();
            cabInt.doorController = controller;
            cabInt.zone = ElevatorDoorInteractZone.CabinInterior;
            cabInt.interactionRadius = 2.00f;
            GameObjectUtility.SetStaticEditorFlags(cabGo.gameObject, 0);
        }

        private static void DisableLegacyBay1StaticDoorComponents(Transform elevCore, Transform approachRoot)
        {
            // Lower Bay 1 legacy static leaves & center seam (preserve all 35 children & fileIDs!)
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_1_DoorLeafS"), disableMesh: true, disableCollider: true);
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_1_DoorLeafN"), disableMesh: true, disableCollider: true);
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_1_CenterSeam"), disableMesh: true, disableCollider: false);

            // Upper Bay 1 legacy static leaves, seam, kickplates & sealed backing collider (preserve GOs & fileIDs!)
            Transform upperB1 = approachRoot.Find("UpperElevatorApproach_BayArchitecture/UpperElevBay1_FinishedFacade/UpperElevBay1_StaticDoors");
            if (upperB1 != null)
            {
                DisableMeshAndCollider(upperB1.Find("UpperElevBay1_DoorLeafS"), disableMesh: true, disableCollider: true);
                DisableMeshAndCollider(upperB1.Find("UpperElevBay1_DoorLeafN"), disableMesh: true, disableCollider: true);
                DisableMeshAndCollider(upperB1.Find("UpperElevBay1_CenterSeam"), disableMesh: true, disableCollider: false);
                DisableMeshAndCollider(upperB1.Find("UpperElevBay1_DoorKickPlateS"), disableMesh: true, disableCollider: false);
                DisableMeshAndCollider(upperB1.Find("UpperElevBay1_DoorKickPlateN"), disableMesh: true, disableCollider: false);
            }
            DisableMeshAndCollider(
                approachRoot.Find("UpperElevatorApproach_SealedBayFrames/UpperElev_SealedBay_1_SealedBackingPanel"),
                disableMesh: true,
                disableCollider: true);
        }

        // =====================================================================
        // BAY 2 PERMANENTLY OPEN UNFINISHED SHAFT (NO CABIN, NO DOORS, REAL VOID)
        // =====================================================================
        private static void BuildBay2PermanentlyOpenShaft(
            Transform shaftRoot,
            Transform elevCore,
            Transform approachRoot,
            DoorMaterials mats)
        {
            // 1. Remove all Lower Bay 2 doorway obstructions (disable MR+COL while preserving 35-child core!)
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_2_DoorLeafS"), disableMesh: true, disableCollider: true);
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_2_DoorLeafN"), disableMesh: true, disableCollider: true);
            DisableMeshAndCollider(elevCore.Find("Hotel_ElevatorBay_2_CenterSeam"), disableMesh: true, disableCollider: false);

            // 2. Remove all Upper Bay 2 infill/closure/backing obstructions while retaining unfinished ConstructionFrame!
            Transform upperB2Infill = approachRoot.Find("UpperElevatorApproach_BayArchitecture/UpperElevBay2_UnfinishedFacade/UpperElevBay2_SealedInfill");
            if (upperB2Infill != null)
            {
                DisableMeshAndCollider(upperB2Infill.Find("UpperElevBay2_SolidMasonryInfill"), disableMesh: true, disableCollider: true);
                DisableMeshAndCollider(upperB2Infill.Find("UpperElevBay2_WeatheredPlasterCourse"), disableMesh: true, disableCollider: false);
                DisableMeshAndCollider(upperB2Infill.Find("UpperElevBay2_ClosurePlate"), disableMesh: true, disableCollider: true);
                DisableMeshAndCollider(upperB2Infill.Find("UpperElevBay2_ClosureBatten_Bot"), disableMesh: true, disableCollider: false);
                DisableMeshAndCollider(upperB2Infill.Find("UpperElevBay2_ClosureBatten_Top"), disableMesh: true, disableCollider: false);
            }
            DisableMeshAndCollider(
                approachRoot.Find("UpperElevatorApproach_SealedBayFrames/UpperElev_SealedBay_2_SealedBackingPanel"),
                disableMesh: true,
                disableCollider: true);

            // 3. Open Backrooms_Ceiling_North cleanly over Bay 2 (trim to Z in [+9.160, +12.300]) so Bay 2 is a continuous
            //    unobstructed vertical void from Y = -43.550 down to uncut Backrooms_Floor Y = -50.000!
            Transform ceilNorth = shaftRoot.Find("Bay1Shaft_CeilingSplit/Backrooms_Ceiling_North");
            if (ceilNorth != null)
            {
                float northMinZ = Bay2ShaftMaxZ; // +9.160f
                float northMaxZ = UpperPassengerElevatorShaftBuilder.CeilingMaxZ; // +12.300f
                float ceilMidY = (UpperPassengerElevatorShaftBuilder.CeilingMinY + UpperPassengerElevatorShaftBuilder.CeilingMaxY) * 0.5f;
                ceilNorth.position = new Vector3((Bay2ShaftMinX + Bay2ShaftMaxX) * 0.5f, ceilMidY, (northMinZ + northMaxZ) * 0.5f);
                ceilNorth.rotation = Quaternion.identity;
                ceilNorth.localScale = new Vector3(Bay2ShaftMaxX - Bay2ShaftMinX, UpperPassengerElevatorShaftBuilder.CeilingThicknessY, northMaxZ - northMinZ);
            }

            // 4. Trim UpperElev_Waiting_WestCoreBackingWall (GO=268893405 preserved!) to Mid Pier Z in [+6.435, +7.165]
            //    so Bay 2's upper portal Z in [+7.165, +8.535], Y in [-46.400, -44.200] is permanently open!
            Transform backingWall = approachRoot.Find("UpperElevatorApproach_WaitingArea/UpperElev_Waiting_WestCoreBackingWall");
            if (backingWall != null)
            {
                float fullWallMinY = UpperElevatorApproachBuilder.SlabBottomY;    // -46.600f
                float fullWallMaxY = UpperElevatorApproachBuilder.CeilingSoffitY; // -43.550f
                float midMinZ = Bay1OpeningMaxZ;                                  // +6.435f
                float midMaxZ = Bay2PortalMinZ;                                   // +7.165f
                backingWall.position = new Vector3(3.250f, (fullWallMinY + fullWallMaxY) * 0.5f, (midMinZ + midMaxZ) * 0.5f);
                backingWall.rotation = Quaternion.identity;
                backingWall.localScale = new Vector3(0.200f, fullWallMaxY - fullWallMinY, midMaxZ - midMinZ);
            }

            // 5. Build Bay 2 Upper Shaft Structural Enclosure + Reusable Open Upper Landing Surround + Unfinished Details
            Transform bay2Group = EnsureSubgroup(shaftRoot, Bay2OpenShaftGroupName, isStatic: true);

            // Use the reusable future-floor contract helper for the Upper Bay 2 open landing!
            EnsureBay2OpenLandingOpening(
                bay2Group,
                "Bay2UpperLanding_OpenSurround",
                floorElevationY: UpperElevatorApproachBuilder.GalleryFloorY,           // -46.400f
                clearPortalHeightY: UpperElevatorApproachBuilder.PlaceholderClearHeightY, // 2.200f
                ceilingSoffitY: UpperElevatorApproachBuilder.CeilingSoffitY,           // -43.550f
                concreteMat: mats.ColumnConcrete,
                trimMetalMat: mats.IndustrialMetal);

            // Upper Shaft Walls & Sealed Roof Cap (Y in [-46.950, -43.350], enclosing Bay 2 completely!)
            float upperWallH = Bay2ShaftRoofTopY - UpperPassengerElevatorShaftBuilder.CeilingMaxY; // 3.600f
            float upperWallMidY = (UpperPassengerElevatorShaftBuilder.CeilingMaxY + Bay2ShaftRoofTopY) * 0.5f; // -45.150f

            // West Back Wall: X [1.000, 1.240], Y [-46.950, -43.350], Z [6.920, 9.400]
            EnsureBox(bay2Group, "Bay2Shaft_BackWall",
                new Vector3(1.120f, upperWallMidY, (Bay2ShaftMinZ + Bay2OuterNorthZ) * 0.5f),
                new Vector3(0.240f, upperWallH, Bay2OuterNorthZ - Bay2ShaftMinZ),
                mats.ColumnConcrete, keepCollider: true, isStatic: true);

            // North Wall: X [1.240, 3.150], Y [-46.950, -43.350], Z [9.160, 9.400]
            EnsureBox(bay2Group, "Bay2Shaft_NorthWall",
                new Vector3((Bay2ShaftMinX + Bay2ShaftMaxX) * 0.5f, upperWallMidY, (Bay2ShaftMaxZ + Bay2OuterNorthZ) * 0.5f),
                new Vector3(Bay2ShaftMaxX - Bay2ShaftMinX, upperWallH, Bay2OuterNorthZ - Bay2ShaftMaxZ),
                mats.ColumnConcrete, keepCollider: true, isStatic: true);

            // Roof Cap: X [1.240, 3.150], Y [-43.550, -43.350], Z [6.920, 9.160]
            EnsureBox(bay2Group, "Bay2Shaft_RoofCap",
                new Vector3((Bay2ShaftMinX + Bay2ShaftMaxX) * 0.5f, (Bay2ShaftRoofSoffitY + Bay2ShaftRoofTopY) * 0.5f, (Bay2ShaftMinZ + Bay2ShaftMaxZ) * 0.5f),
                new Vector3(Bay2ShaftMaxX - Bay2ShaftMinX, Bay2ShaftRoofTopY - Bay2ShaftRoofSoffitY, Bay2ShaftMaxZ - Bay2ShaftMinZ),
                mats.ColumnConcrete, keepCollider: true, isStatic: true);

            // East 10mm Plenum Seal strictly outside X < +3.150 (Y in [-46.950, -46.940], Z in [+6.920, +9.160])
            EnsureBox(bay2Group, "Bay2Shaft_EastPlenumSeal",
                new Vector3(3.250f, -46.945f, (Bay2ShaftMinZ + Bay2ShaftMaxZ) * 0.5f),
                new Vector3(0.200f, 0.010f, Bay2ShaftMaxZ - Bay2ShaftMinZ),
                mats.ColumnConcrete, keepCollider: true, isStatic: true);

            // 6. Unfinished Construction Details (STRICTLY VISUAL ONLY, keepCollider: false, keeping central void 100% clear!)
            Transform details = EnsureSubgroup(bay2Group, "Bay2Shaft_UnfinishedConstructionDetails", isStatic: true);

            // Exposed incomplete divider-side rail stub (stops mid-upper-shaft at Y = -45.400, clearly abandoned!)
            EnsureBox(details, "Bay2_IncompleteRailStub_SouthFlange",
                new Vector3(2.170f, (-49.980f + -45.400f) * 0.5f, 6.955f),
                new Vector3(0.070f, 4.580f, 0.010f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);
            EnsureBox(details, "Bay2_IncompleteRailStub_SouthBlade",
                new Vector3(2.170f, (-49.980f + -45.400f) * 0.5f, 6.980f),
                new Vector3(0.016f, 4.580f, 0.040f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
            float[] sBrkYs = { -49.650f, -48.150f, -46.650f };
            for (int i = 0; i < sBrkYs.Length; i++)
            {
                EnsureBox(details, $"Bay2_UnfinishedBracket_S_{i}",
                    new Vector3(2.170f, sBrkYs[i], 6.935f),
                    new Vector3(0.220f, 0.090f, 0.030f),
                    mats.IndustrialMetal, keepCollider: false, isStatic: true);
            }

            // Staggered incomplete North-wall rail stub (upper half only: Y in [-47.800, -43.700])
            EnsureBox(details, "Bay2_IncompleteRailStub_NorthFlange",
                new Vector3(2.170f, (-47.800f + -43.700f) * 0.5f, 9.125f),
                new Vector3(0.070f, 4.100f, 0.010f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);
            EnsureBox(details, "Bay2_IncompleteRailStub_NorthBlade",
                new Vector3(2.170f, (-47.800f + -43.700f) * 0.5f, 9.100f),
                new Vector3(0.016f, 4.100f, 0.040f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
            float[] nBrkYs = { -47.400f, -45.600f, -43.900f };
            for (int i = 0; i < nBrkYs.Length; i++)
            {
                EnsureBox(details, $"Bay2_UnfinishedBracket_N_{i}",
                    new Vector3(2.170f, nBrkYs[i], 9.145f),
                    new Vector3(0.220f, 0.090f, 0.030f),
                    mats.IndustrialMetal, keepCollider: false, isStatic: true);
            }

            // Abandoned overhead hoist beam near shaft head (Y in [-43.730, -43.570], visual only)
            EnsureBox(details, "Bay2_AbandonedHoistBeam",
                new Vector3(2.195f, -43.650f, 8.040f),
                new Vector3(1.780f, 0.160f, 0.140f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);

            // Four hanging incomplete suspension cables tucked into far NW corner (clear of primary traversal/fall zone!)
            (string name, float x, float z, float len)[] cables =
            {
                ("Bay2_HangingCable_0", 1.420f, 8.860f, 4.850f),
                ("Bay2_HangingCable_1", 1.460f, 8.820f, 3.600f),
                ("Bay2_HangingCable_2", 1.500f, 8.760f, 5.100f),
                ("Bay2_HangingCable_3", 1.440f, 8.710f, 2.550f)
            };
            for (int i = 0; i < cables.Length; i++)
            {
                var c = cables[i];
                float topY = -43.680f;
                EnsureBox(details, c.name,
                    new Vector3(c.x, topY - c.len * 0.5f, c.z),
                    new Vector3(0.014f, c.len, 0.014f),
                    mats.IndustrialMetal, keepCollider: false, isStatic: true);
            }

            // Small unused coiled cable spool tucked in far NW floor corner (visual only, zero cabin implication)
            EnsureBox(details, "Bay2_FloorCableSpool_NW",
                new Vector3(1.440f, -49.940f, 8.920f),
                new Vector3(0.260f, 0.120f, 0.260f),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);
        }

        private static DoorMaterials LoadExistingMaterials()
        {
            return new DoorMaterials
            {
                ElevatorDoorMetal = LoadRequiredMat(BackroomsLevel1Builder.ElevatorDoorMetalMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath)
            };
        }

        private static Material LoadRequiredMat(string assetPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null) throw new InvalidOperationException($"Missing required material '{assetPath}'.");
            return mat;
        }

        private static void DisableMeshAndCollider(Transform target, bool disableMesh, bool disableCollider)
        {
            if (target == null) return;
            if (disableMesh)
            {
                MeshRenderer mr = target.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
            if (disableCollider)
            {
                Collider col = target.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }

        private static Transform EnsureSubgroup(Transform parent, string name, bool isStatic, Vector3? worldPos = null)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null) go.transform.SetParent(parent, false);

            if (worldPos.HasValue)
            {
                go.transform.position = worldPos.Value;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            if (isStatic) go.isStatic = true;
            else GameObjectUtility.SetStaticEditorFlags(go, 0);
            return go.transform;
        }

        private static GameObject EnsureBox(
            Transform parent,
            string name,
            Vector3 worldPos,
            Vector3 worldScale,
            Material mat,
            bool keepCollider,
            bool isStatic)
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
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = worldScale;

            if (isStatic) go.isStatic = true;
            else GameObjectUtility.SetStaticEditorFlags(go, 0);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = true;
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

        private static void EnforceNonStaticRecursive(Transform root)
        {
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);
            for (int i = 0; i < root.childCount; i++) EnforceNonStaticRecursive(root.GetChild(i));
        }
    }
}
