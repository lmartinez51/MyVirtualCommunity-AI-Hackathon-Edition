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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.27:
    /// Static Passenger Cabin &amp; Guide Rail System (Hybrid Older Hotel Elevator — Static Architectural Only).
    ///
    /// Strict architectural root separation:
    ///   1. Static shaft-side guide rails &amp; mounting brackets under:
    ///      Backrooms_Architecture/Hotel_UpperPassengerElevatorShaft/Bay1Shaft_GuideRails (isStatic = true)
    ///      - Two cold-drawn ISO 7465 T70-1/B machined steel T-rails (70mm base flange x 10mm thick,
    ///        40mm blade depth x 16mm blade width, total 50mm profile depth) centered at X = +2.170m,
    ///        running continuously Y in [-49.980, -43.650] (6.330m) atop 20mm steel base feet resting
    ///        flush on uncut Backrooms_Floor (Y = -50.000) and terminating 100mm clear below RoofCap (-43.550).
    ///      - Five evenly spaced bracket tiers (Y = -49.650, -48.150, -46.650, -45.150, -43.750) mounted
    ///        exclusively to Bay 1 inner faces (South wall Z=+4.440, North concrete divider Z=+6.680),
    ///        specifying 50mm surface anchor embedment leaving 190mm solid concrete cover to Bay 2.
    ///   2. Future movable passenger cabin root (ALL objects strictly non-static, isStatic = false) under:
    ///      Backrooms_Architecture/Hotel_UpperPassengerElevatorCabin (parked at lower stop Y = -50.000)
    ///      - Outer cabin bounds: X in [+1.320, +3.025] (1.705m), Y in [-50.000, -47.600] (2.400m),
    ///        Z in [+4.620, +6.560] (1.940m). Deck bottom flush on -50.000, deck top at -49.960.
    ///      - Clear passenger interior: X in [+1.380, +2.940] (1.560m), Z in [+4.680, +6.500] (1.820m),
    ///        clear height 2.240m (Y in [-49.960, -47.720]), area 2.8392 m² with ZERO intrusion.
    ///      - East-facing open entrance portal: Z in [+5.300, +6.400] (1.100m), Y in [-49.960, -47.760] (2.200m),
    ///        clearing all four 07.25A reserved two-speed telescopic door swept volumes by >= 5.0mm.
    ///      - Four cabin-mounted U-channel guide shoe assemblies (2 South, 2 North; vertical separation 2.080m)
    ///        straddling the 16mm rail blades with deliberate non-overlapping gameplay clearances
    ///        (+6.0mm tip, +4.0mm lateral per side, +8.0mm flange/bracket passage clearance).
    ///      - Restrained older-hotel interior finishes (wood wainscoting, warm plaster upper panels,
    ///        dark metal baseboards &amp; flush handrail, brushed metal entrance returns, emissive-only ceiling
    ///        diffuser, blank non-interactive future control plate) reusing existing project materials only.
    ///
    /// Adds ZERO Unity Light components (exact 18 preserved), ZERO runtime scripts, ZERO Animators,
    /// ZERO AudioSources, and ZERO trigger colliders. Leaves landing facades closed and untouched.
    /// </summary>
    public static class UpperPassengerElevatorCabinBuilder
    {
        public const string CabinRootName = BackroomsLevel1Builder.UpperPassengerElevatorCabinRootName;
        public const string ShaftGuideRailsGroupName = "Bay1Shaft_GuideRails";

        // Authoritative lower-stop outer cabin envelope (Section 3A)
        public const float CabinMinX = 1.320f;
        public const float CabinMaxX = 3.025f;
        public const float CabinMinY = -50.000f;
        public const float CabinMaxY = -47.600f;
        public const float CabinMinZ = 4.620f;
        public const float CabinMaxZ = 6.560f;
        public const float CabinCenterX = (CabinMinX + CabinMaxX) * 0.5f; // +2.1725f
        public const float CabinCenterZ = (CabinMinZ + CabinMaxZ) * 0.5f; // +5.5900f

        public const float DeckThicknessY = 0.040f;
        public const float DeckTopY = CabinMinY + DeckThicknessY;         // -49.960f
        public const float RoofThicknessY = 0.120f;
        public const float RoofSoffitY = CabinMaxY - RoofThicknessY;      // -47.720f
        public const float ClearInteriorHeightY = RoofSoffitY - DeckTopY; //   2.240f
        public const float FutureTravelDeltaY = 3.560f;                   // Upper stop deck top = -46.400f

        // Authoritative clear passenger interior envelope (Section 3B)
        public const float InteriorMinX = 1.380f;
        public const float InteriorMaxX = 2.940f;
        public const float InteriorMinZ = 4.680f;
        public const float InteriorMaxZ = 6.500f;

        // Authoritative East-facing cabin entrance portal (Section 3C)
        public const float PortalMinZ = 5.300f;
        public const float PortalMaxZ = 6.400f;
        public const float PortalMinY = DeckTopY;                         // -49.960f
        public const float PortalMaxY = -47.760f;                         // 2.200m clear height

        // Reconciled ISO 7465 T70-1/B Guide Rail System (Section 4)
        public const float RailCenterX = 2.170f;
        public const float RailFlangeWidthX = 0.070f;                     // X in [+2.135, +2.205]
        public const float RailFlangeThickZ = 0.010f;
        public const float RailBladeWidthX = 0.016f;                      // X in [+2.162, +2.178]
        public const float RailBladeDepthZ = 0.040f;
        public const float RailTotalDepthZ = RailFlangeThickZ + RailBladeDepthZ; // 0.050m
        public const float RailFootMinY = -50.000f;
        public const float RailMinY = -49.980f;                           // +20mm base foot
        public const float RailMaxY = -43.650f;                           // +100mm below RoofCap (-43.550)

        // South Rail Z stack (South shaft face Z = +4.440 -> Cabin South outer face Z = +4.620; 180mm pocket)
        public const float SouthWallFaceZ = UpperPassengerElevatorShaftBuilder.ApertureMinZ; // +4.440f
        public const float SouthBracketWallPlateMaxZ = 4.454f;            // 14mm plate
        public const float SouthRailFlangeMinZ = 4.530f;                  // 76mm stand-off
        public const float SouthRailFlangeMaxZ = 4.540f;                  // 10mm flange
        public const float SouthRailBladeTipZ = 4.580f;                   // 40mm blade -> +40mm clear to Cabin (+4.620)
        public const float SouthRailCenterZ = 4.555f;

        // North Rail Z stack (Cabin North outer face Z = +6.560 -> Divider South face Z = +6.680; 120mm pocket)
        public const float NorthDividerFaceZ = UpperPassengerElevatorShaftBuilder.ApertureMaxZ; // +6.680f
        public const float NorthBracketWallPlateMinZ = 6.666f;            // 14mm plate
        public const float NorthRailFlangeMaxZ = 6.650f;                  // 16mm stand-off
        public const float NorthRailFlangeMinZ = 6.640f;                  // 10mm flange
        public const float NorthRailBladeTipZ = 6.600f;                   // 40mm blade -> +40mm clear to Cabin (+6.560)
        public const float NorthRailCenterZ = 6.625f;

        // Structural anchor specification (Bay 1 inner faces; 240mm concrete walls -> 190mm remaining cover)
        public const float AnchorEmbedmentDepthM = 0.050f;
        public const float WallConcreteThicknessM = 0.240f;
        public const float RemainingConcreteCoverM = WallConcreteThicknessM - AnchorEmbedmentDepthM; // 0.190m

        // Guide shoe geometry & deliberate running clearances (4 shoes: lower & upper on South & North)
        public const float ShoeTipClearanceZ = 0.006f;                    // 6mm clear from rail blade tip
        public const float ShoeLateralClearanceX = 0.004f;                // 4mm clear per side of 16mm blade
        public const float ShoeFlangeClearanceZ = 0.008f;                 // 8mm clear from rail flange & clips
        public const float LowerShoeCenterY = -49.840f;                   // Y in [-49.930, -49.750]
        public const float UpperShoeCenterY = -47.760f;                   // Y in [-47.850, -47.670] (2.080m separation)

        public static readonly float[] BracketTierYs = { -49.650f, -48.150f, -46.650f, -45.150f, -43.750f };

        private struct CabinMaterials
        {
            public Material ElevatorDoorMetal;
            public Material IndustrialMetal;
            public Material PlasterWall;
            public Material UnfinishedWoodDoor;
            public Material FloorConcrete;
            public Material UtilityWarmOn;
            public Material BaseboardConcrete;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.27 Static Passenger Cabin & Guide Rail System")]
        public static void MigrateUpperPassengerElevatorCabinAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateUpperPassengerElevatorCabinInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.27 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UpperPassengerElevatorCabinBuilder] Implementation 07.27 Static Passenger Cabin & Guide Rail System migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.27.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateUpperPassengerElevatorCabinInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UpperPassengerElevatorCabinBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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

            if (!UpperPassengerElevatorShaftBuilder.VerifyStructurePresent(shell))
            {
                throw new InvalidOperationException(
                    "Expected 07.26 Bay 1 Passenger Elevator Shaft Isolation before running 07.27 Cabin & Guide Rail migration.");
            }

            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            CabinMaterials mats = LoadExistingMaterials();

            // 1. Static Guide Rail System & Mounting Brackets under static Shaft root (isStatic = true)
            Transform railsRoot = EnsureSubgroup(shaftRoot, ShaftGuideRailsGroupName, isStatic: true, worldPos: Vector3.zero);
            BuildStaticGuideRailSystem(railsRoot, mats);

            // 2. Dedicated Future Movable Cabin Root under Backrooms_Architecture (strictly non-static!)
            Transform cabinRoot = EnsureSubgroup(
                shell,
                CabinRootName,
                isStatic: false,
                worldPos: new Vector3(CabinCenterX, CabinMinY, CabinCenterZ));

            BuildCabinStructuralShell(EnsureSubgroup(cabinRoot, "Cabin_StructuralShell", isStatic: false), mats);
            BuildCabinInteriorFinishes(EnsureSubgroup(cabinRoot, "Cabin_InteriorFinishes", isStatic: false), mats);
            BuildCabinGuideShoes(EnsureSubgroup(cabinRoot, "Cabin_GuideShoes", isStatic: false), mats);

            // Guarantee strictly zero static flags anywhere in the movable cabin hierarchy
            EnforceNonStaticRecursive(cabinRoot);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform shaftRoot = architectureShell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Transform cabinRoot = architectureShell.Find(CabinRootName);
            if (shaftRoot == null || cabinRoot == null) return false;

            Transform railsRoot = shaftRoot.Find(ShaftGuideRailsGroupName);
            if (railsRoot == null) return false;

            return railsRoot.Find("Bay1Shaft_GuideRail_South/GuideRail_South_Blade") != null &&
                   railsRoot.Find("Bay1Shaft_GuideRail_North/GuideRail_North_Blade") != null &&
                   railsRoot.Find("Bay1Shaft_RailBrackets/RailBracket_S_0") != null &&
                   railsRoot.Find("Bay1Shaft_RailBrackets/RailBracket_N_4") != null &&
                   cabinRoot.Find("Cabin_StructuralShell/Cabin_FloorDeck_Main") != null &&
                   cabinRoot.Find("Cabin_StructuralShell/Cabin_RoofSlab") != null &&
                   cabinRoot.Find("Cabin_InteriorFinishes/Cabin_CeilingDiffuser") != null &&
                   cabinRoot.Find("Cabin_GuideShoes/GuideShoe_South_Lower") != null &&
                   cabinRoot.Find("Cabin_GuideShoes/GuideShoe_South_Upper") != null &&
                   cabinRoot.Find("Cabin_GuideShoes/GuideShoe_North_Lower") != null &&
                   cabinRoot.Find("Cabin_GuideShoes/GuideShoe_North_Upper") != null;
        }

        private static CabinMaterials LoadExistingMaterials()
        {
            return new CabinMaterials
            {
                ElevatorDoorMetal = LoadRequiredMat(BackroomsLevel1Builder.ElevatorDoorMetalMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                PlasterWall = LoadRequiredMat(BackroomsLevel1Builder.PlasterWallMatPath),
                UnfinishedWoodDoor = LoadRequiredMat(BackroomsLevel1Builder.UnfinishedWoodDoorMatPath),
                FloorConcrete = LoadRequiredMat(BackroomsLevel1Builder.FloorConcreteMatPath),
                UtilityWarmOn = LoadRequiredMat(BackroomsLevel1Builder.UtilityWarmOnMatPath),
                BaseboardConcrete = LoadRequiredMat(BackroomsLevel1Builder.BaseboardMatPath)
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

        // =====================================================================
        // 1. STATIC SHAFT GUIDE RAILS & BRACKET SYSTEM (STATIC SHAFT GEOMETRY)
        // =====================================================================
        private static void BuildStaticGuideRailSystem(Transform railsRoot, CabinMaterials mats)
        {
            float railLenY = RailMaxY - RailMinY;             // 6.330f
            float railMidY = (RailMinY + RailMaxY) * 0.5f;    // -46.815f
            float footH = RailMinY - RailFootMinY;            // 0.020f
            float footMidY = (RailFootMinY + RailMinY) * 0.5f; // -49.990f

            // South ISO 7465 T70-1/B Guide Rail (pointing +Z toward Cabin South face)
            Transform southRail = EnsureSubgroup(railsRoot, "Bay1Shaft_GuideRail_South", isStatic: true, worldPos: Vector3.zero);
            EnsureBox(southRail, "GuideRail_South_Flange",
                new Vector3(RailCenterX, railMidY, (SouthRailFlangeMinZ + SouthRailFlangeMaxZ) * 0.5f),
                new Vector3(RailFlangeWidthX, railLenY, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);
            EnsureBox(southRail, "GuideRail_South_Blade",
                new Vector3(RailCenterX, railMidY, (SouthRailFlangeMaxZ + SouthRailBladeTipZ) * 0.5f),
                new Vector3(RailBladeWidthX, railLenY, RailBladeDepthZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);
            EnsureBox(southRail, "GuideRail_South_BaseFoot",
                new Vector3(RailCenterX, footMidY, (4.495f + SouthRailBladeTipZ) * 0.5f),
                new Vector3(0.130f, footH, SouthRailBladeTipZ - 4.495f),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // North ISO 7465 T70-1/B Guide Rail (pointing -Z toward Cabin North face)
            Transform northRail = EnsureSubgroup(railsRoot, "Bay1Shaft_GuideRail_North", isStatic: true, worldPos: Vector3.zero);
            EnsureBox(northRail, "GuideRail_North_Flange",
                new Vector3(RailCenterX, railMidY, (NorthRailFlangeMinZ + NorthRailFlangeMaxZ) * 0.5f),
                new Vector3(RailFlangeWidthX, railLenY, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);
            EnsureBox(northRail, "GuideRail_North_Blade",
                new Vector3(RailCenterX, railMidY, (NorthRailBladeTipZ + NorthRailFlangeMinZ) * 0.5f),
                new Vector3(RailBladeWidthX, railLenY, RailBladeDepthZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: true);
            EnsureBox(northRail, "GuideRail_North_BaseFoot",
                new Vector3(RailCenterX, footMidY, (NorthRailBladeTipZ + 6.678f) * 0.5f),
                new Vector3(0.130f, footH, 6.678f - NorthRailBladeTipZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // Five evenly spaced static wall-mounted bracket tiers on South wall (+4.440) and North divider (+6.680)
            Transform bracketsGroup = EnsureSubgroup(railsRoot, "Bay1Shaft_RailBrackets", isStatic: true, worldPos: Vector3.zero);
            for (int i = 0; i < BracketTierYs.Length; i++)
            {
                float y = BracketTierYs[i];
                BuildSouthBracketTier(EnsureSubgroup(bracketsGroup, $"RailBracket_S_{i}", isStatic: true, worldPos: Vector3.zero), y, mats);
                BuildNorthBracketTier(EnsureSubgroup(bracketsGroup, $"RailBracket_N_{i}", isStatic: true, worldPos: Vector3.zero), y, mats);
            }
        }

        private static void BuildSouthBracketTier(Transform group, float y, CabinMaterials mats)
        {
            // Wall Anchor Plate flush against South shaft face (Z in [+4.440, +4.454])
            EnsureBox(group, "WallPlate",
                new Vector3(RailCenterX, y, (SouthWallFaceZ + SouthBracketWallPlateMaxZ) * 0.5f),
                new Vector3(0.220f, 0.100f, SouthBracketWallPlateMaxZ - SouthWallFaceZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // Structural Stand-Off Channel (Z in [+4.454, +4.530], 76mm projection)
            EnsureBox(group, "Standoff",
                new Vector3(RailCenterX, y, (SouthBracketWallPlateMaxZ + SouthRailFlangeMinZ) * 0.5f),
                new Vector3(0.110f, 0.080f, SouthRailFlangeMinZ - SouthBracketWallPlateMaxZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // Forged Rail Clips gripping flange edges (Z in [+4.530, +4.540]; 8mm clear of guide shoes at +4.548)
            EnsureBox(group, "ClipW",
                new Vector3(RailCenterX - 0.045f, y, (SouthRailFlangeMinZ + SouthRailFlangeMaxZ) * 0.5f),
                new Vector3(0.020f, 0.060f, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);
            EnsureBox(group, "ClipE",
                new Vector3(RailCenterX + 0.045f, y, (SouthRailFlangeMinZ + SouthRailFlangeMaxZ) * 0.5f),
                new Vector3(0.020f, 0.060f, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);

            // Visible Bay-1-side hex anchor bolt heads (structural 50mm embedment into 240mm South wall -> 190mm cover)
            EnsureBox(group, "AnchorBoltW",
                new Vector3(RailCenterX - 0.080f, y, SouthBracketWallPlateMaxZ + 0.005f),
                new Vector3(0.022f, 0.022f, 0.010f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
            EnsureBox(group, "AnchorBoltE",
                new Vector3(RailCenterX + 0.080f, y, SouthBracketWallPlateMaxZ + 0.005f),
                new Vector3(0.022f, 0.022f, 0.010f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
        }

        private static void BuildNorthBracketTier(Transform group, float y, CabinMaterials mats)
        {
            // Wall Anchor Plate flush against Bay 1 face of concrete divider (Z in [+6.666, +6.680]; zero divider penetration!)
            EnsureBox(group, "WallPlate",
                new Vector3(RailCenterX, y, (NorthBracketWallPlateMinZ + NorthDividerFaceZ) * 0.5f),
                new Vector3(0.220f, 0.100f, NorthDividerFaceZ - NorthBracketWallPlateMinZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // Structural Stand-Off Spacer (Z in [+6.650, +6.666], 16mm projection)
            EnsureBox(group, "Standoff",
                new Vector3(RailCenterX, y, (NorthRailFlangeMaxZ + NorthBracketWallPlateMinZ) * 0.5f),
                new Vector3(0.110f, 0.080f, NorthBracketWallPlateMinZ - NorthRailFlangeMaxZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: true);

            // Forged Rail Clips gripping flange edges (Z in [+6.640, +6.650]; 8mm clear of guide shoes at +6.632)
            EnsureBox(group, "ClipW",
                new Vector3(RailCenterX - 0.045f, y, (NorthRailFlangeMinZ + NorthRailFlangeMaxZ) * 0.5f),
                new Vector3(0.020f, 0.060f, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);
            EnsureBox(group, "ClipE",
                new Vector3(RailCenterX + 0.045f, y, (NorthRailFlangeMinZ + NorthRailFlangeMaxZ) * 0.5f),
                new Vector3(0.020f, 0.060f, RailFlangeThickZ),
                mats.IndustrialMetal, keepCollider: false, isStatic: true);

            // Visible Bay-1-side hex anchor bolt heads (structural 50mm embedment into 240mm divider -> 190mm solid cover to Bay 2)
            EnsureBox(group, "AnchorBoltW",
                new Vector3(RailCenterX - 0.080f, y, NorthBracketWallPlateMinZ - 0.005f),
                new Vector3(0.022f, 0.022f, 0.010f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
            EnsureBox(group, "AnchorBoltE",
                new Vector3(RailCenterX + 0.080f, y, NorthBracketWallPlateMinZ - 0.005f),
                new Vector3(0.022f, 0.022f, 0.010f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: true);
        }

        // =====================================================================
        // 2. PASSENGER CABIN STRUCTURAL SHELL (NON-STATIC MOVABLE ASSEMBLY)
        // =====================================================================
        private static void BuildCabinStructuralShell(Transform group, CabinMaterials mats)
        {
            float cabinW = CabinMaxX - CabinMinX;          // 1.705f
            float cabinD = CabinMaxZ - CabinMinZ;          // 1.940f
            float wallH = RoofSoffitY - DeckTopY;          // 2.240f
            float wallMidY = (DeckTopY + RoofSoffitY) * 0.5f; // -48.840f

            // Floor Deck resting flush on uncut Backrooms_Floor (Y in [-50.000, -49.960]), split cleanly into
            // main interior floor deck (X in [1.320, 2.940]) + brushed-metal entrance threshold sill (X in [2.940, 3.025])
            float deckMidY = (CabinMinY + DeckTopY) * 0.5f; // -49.980f
            EnsureBox(group, "Cabin_FloorDeck_Main",
                new Vector3((CabinMinX + InteriorMaxX) * 0.5f, deckMidY, CabinCenterZ),
                new Vector3(InteriorMaxX - CabinMinX, DeckThicknessY, cabinD),
                mats.FloorConcrete, keepCollider: true, isStatic: false);
            EnsureBox(group, "Cabin_FloorSill_East",
                new Vector3((InteriorMaxX + CabinMaxX) * 0.5f, deckMidY, CabinCenterZ),
                new Vector3(CabinMaxX - InteriorMaxX, DeckThicknessY, cabinD),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Roof Slab: main structural roof deck (Y in [-47.700, -47.600]) + 20mm perimeter ceiling frame (Y in [-47.720, -47.700])
            // leaving central recessed pocket for the flush emissive diffuser without reducing 2.240m clear headroom!
            EnsureBox(group, "Cabin_RoofSlab",
                new Vector3(CabinCenterX, (-47.700f + CabinMaxY) * 0.5f, CabinCenterZ),
                new Vector3(cabinW, 0.100f, cabinD),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);

            // Outer structural steel shell walls (45mm carcass; flush 15mm interior finishes bring faces exactly to 1.380, 4.680, 6.500)
            const float carcassT = 0.045f;
            EnsureBox(group, "Cabin_WallWest_Carcass",
                new Vector3(CabinMinX + carcassT * 0.5f, wallMidY, CabinCenterZ),
                new Vector3(carcassT, wallH, cabinD),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);
            EnsureBox(group, "Cabin_WallSouth_Carcass",
                new Vector3((CabinMinX + carcassT + CabinMaxX) * 0.5f, wallMidY, CabinMinZ + carcassT * 0.5f),
                new Vector3(CabinMaxX - (CabinMinX + carcassT), wallH, carcassT),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);
            EnsureBox(group, "Cabin_WallNorth_Carcass",
                new Vector3((CabinMinX + carcassT + CabinMaxX) * 0.5f, wallMidY, CabinMaxZ - carcassT * 0.5f),
                new Vector3(CabinMaxX - (CabinMinX + carcassT), wallH, carcassT),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);

            // East-facing Entrance Structural Returns & Header (strictly preserving clear portal Z in [+5.300, +6.400],
            // Y in [-49.960, -47.760] AND >= 5.0mm clearance to all future 2-speed telescopic cabin/landing door sweeps!)
            // South front return: X in [+2.946, +2.960] (14mm structural backing; +5mm clear of Cabin Slow Leaf X_min=+2.965)
            // plus flush 6mm front face panel in Cabin_InteriorFinishes bringing inner face cleanly to X = +2.940.
            EnsureBox(group, "Cabin_EastReturnSouth",
                new Vector3((2.946f + 2.960f) * 0.5f, wallMidY, (InteriorMinZ + PortalMinZ) * 0.5f),
                new Vector3(0.014f, wallH, PortalMinZ - InteriorMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // North strike jamb: full depth X in [+2.940, +3.025] strictly North of Z = +6.405 (+5mm clear of Fast Leaf Z_max=+6.400)
            EnsureBox(group, "Cabin_EastStrikeJambNorth",
                new Vector3((InteriorMaxX + CabinMaxX) * 0.5f, wallMidY, (6.405f + InteriorMaxZ) * 0.5f),
                new Vector3(CabinMaxX - InteriorMaxX, wallH, InteriorMaxZ - 6.405f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // North inner portal reveal lip closing [6.400, 6.405] strictly at X in [+2.940, +2.960] (+5mm clear of Slow Leaf)
            EnsureBox(group, "Cabin_EastStrikeLipNorth",
                new Vector3((InteriorMaxX + 2.960f) * 0.5f, wallMidY, (PortalMaxZ + 6.405f) * 0.5f),
                new Vector3(0.020f, wallH, 0.005f),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            // Entrance header/transom: Y in [-47.760, -47.720] at X in [+2.940, +2.960] (leaves X in [2.965, 3.025] open for door track!)
            EnsureBox(group, "Cabin_EastHeader",
                new Vector3((InteriorMaxX + 2.960f) * 0.5f, (PortalMaxY + RoofSoffitY) * 0.5f, (PortalMinZ + PortalMaxZ) * 0.5f),
                new Vector3(0.020f, RoofSoffitY - PortalMaxY, PortalMaxZ - PortalMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);
        }

        // =====================================================================
        // 3. RESTRAINED OLDER-HOTEL INTERIOR FINISHES (ZERO CLEAR-VOLUME INTRUSION)
        // =====================================================================
        private static void BuildCabinInteriorFinishes(Transform group, CabinMaterials mats)
        {
            // Flush 15mm finish strata landing EXACTLY on InteriorMinX (+1.380), InteriorMinZ (+4.680), InteriorMaxZ (+6.500)
            // Vertical finish bands across Y in [-49.960, -47.720] (2.240m):
            //   - Dark metal baseboard:    [-49.960, -49.840] (0.120m)
            //   - Modest wood wainscot:    [-49.840, -48.940] (0.900m)
            //   - Dark metal chair rail:   [-48.940, -48.905] (0.035m)
            //   - Warm aged plaster upper: [-48.905, -47.780] (1.125m)
            //   - Dark metal ceiling trim: [-47.780, -47.720] (0.060m)
            const float finishT = 0.015f;
            float intW = InteriorMaxX - InteriorMinX; // 1.560f
            float intD = InteriorMaxZ - InteriorMinZ; // 1.820f
            float intMidX = (InteriorMinX + InteriorMaxX) * 0.5f; // +2.160f
            float intMidZ = (InteriorMinZ + InteriorMaxZ) * 0.5f; // +5.590f

            // Helper to build all 5 flush strata along a wall plane with solid BoxColliders on the finished interior plane
            void BuildWallStrata(string prefix, Vector3 baseCenter, bool alongZ, float span)
            {
                (string suffix, float y0, float y1, Material mat)[] bands =
                {
                    ("Baseboard", -49.960f, -49.840f, mats.BaseboardConcrete),
                    ("Wainscot",  -49.840f, -48.940f, mats.UnfinishedWoodDoor),
                    ("ChairRail", -48.940f, -48.905f, mats.IndustrialMetal),
                    ("UpperWall", -48.905f, -47.780f, mats.PlasterWall),
                    ("CrownTrim", -47.780f, -47.720f, mats.IndustrialMetal)
                };

                for (int i = 0; i < bands.Length; i++)
                {
                    var b = bands[i];
                    float midY = (b.y0 + b.y1) * 0.5f;
                    float h = b.y1 - b.y0;
                    Vector3 pos = new Vector3(baseCenter.x, midY, baseCenter.z);
                    Vector3 scl = alongZ ? new Vector3(finishT, h, span) : new Vector3(span, h, finishT);
                    EnsureBox(group, $"{prefix}_{b.suffix}", pos, scl, b.mat, keepCollider: true, isStatic: false);
                }
            }

            // West rear wall finish strata (X in [1.365, 1.380]); pull West ChairRail back 4mm so brushed-metal
            // handrail sits flush at X = +1.380 with crisp architectural reveal and ZERO coplanar Z-fighting
            BuildWallStrata("Cabin_FinishWest", new Vector3(InteriorMinX - finishT * 0.5f, 0f, intMidZ), alongZ: true, span: intD);
            Transform westChairRail = group.Find("Cabin_FinishWest_ChairRail");
            if (westChairRail != null)
            {
                westChairRail.position = new Vector3(InteriorMinX - 0.0095f, -48.9225f, intMidZ);
                westChairRail.localScale = new Vector3(0.011f, 0.035f, intD);
            }

            // Recessed subtle metal handrail bar flush at West wall wainscot top (X in [1.372, 1.380], zero intrusion past +1.380!)
            EnsureBox(group, "Cabin_Handrail_West",
                new Vector3(InteriorMinX - 0.004f, -48.9225f, intMidZ),
                new Vector3(0.008f, 0.026f, intD - 0.240f),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: false);

            // South side wall finish strata (Z in [4.665, 4.680])
            BuildWallStrata("Cabin_FinishSouth", new Vector3(intMidX, 0f, InteriorMinZ - finishT * 0.5f), alongZ: false, span: intW);

            // North side wall finish strata (Z in [6.500, 6.515])
            BuildWallStrata("Cabin_FinishNorth", new Vector3(intMidX, 0f, InteriorMaxZ + finishT * 0.5f), alongZ: false, span: intW);

            // East South interior return finish skin (X in [2.942, 2.946], flush with Cabin_EastReturnSouth at 2.946)
            // + Blank non-interactive future control-panel mounting surface (X in [2.940, 2.942], ZERO buttons/scripts/Z-fighting!)
            float returnMinZ = InteriorMinZ; // +4.680f
            float returnMaxZ = PortalMinZ;   // +5.300f
            float returnMidZ = (returnMinZ + returnMaxZ) * 0.5f;
            float wallH = RoofSoffitY - DeckTopY;
            float wallMidY = (DeckTopY + RoofSoffitY) * 0.5f;
            EnsureBox(group, "Cabin_EastReturnSouth_FinishSkin",
                new Vector3(2.944f, wallMidY, returnMidZ),
                new Vector3(0.004f, wallH, returnMaxZ - returnMinZ),
                mats.ElevatorDoorMetal, keepCollider: false, isStatic: false);

            // Blank future interior button panel reserve (2mm relief proud of return skin, landing flush at X = +2.940, zero intrusion!)
            EnsureBox(group, "Cabin_FutureControlPanel_BlankReserve",
                new Vector3(2.941f, -48.770f, 4.990f),
                new Vector3(0.002f, 0.420f, 0.220f),
                mats.IndustrialMetal, keepCollider: false, isStatic: false);

            // Ceiling perimeter soffit border (Y in [-47.720, -47.700]) framing the recessed emissive-only ceiling diffuser
            // Clear soffit underside sits at Y = -47.720 (exact 2.240m clear headroom!)
            const float soffitMinY = RoofSoffitY; // -47.720f
            const float soffitMaxY = -47.700f;
            float soffitMidY = (soffitMinY + soffitMaxY) * 0.5f;
            float soffitT = soffitMaxY - soffitMinY; // 0.020f
            const float diffMinX = 1.800f, diffMaxX = 2.520f;
            const float diffMinZ = 5.140f, diffMaxZ = 6.040f;

            EnsureBox(group, "Cabin_CeilingFrame_W",
                new Vector3((InteriorMinX + diffMinX) * 0.5f, soffitMidY, intMidZ),
                new Vector3(diffMinX - InteriorMinX, soffitT, intD),
                mats.PlasterWall, keepCollider: false, isStatic: false);
            EnsureBox(group, "Cabin_CeilingFrame_E",
                new Vector3((diffMaxX + InteriorMaxX) * 0.5f, soffitMidY, intMidZ),
                new Vector3(InteriorMaxX - diffMaxX, soffitT, intD),
                mats.PlasterWall, keepCollider: false, isStatic: false);
            EnsureBox(group, "Cabin_CeilingFrame_S",
                new Vector3((diffMinX + diffMaxX) * 0.5f, soffitMidY, (InteriorMinZ + diffMinZ) * 0.5f),
                new Vector3(diffMaxX - diffMinX, soffitT, diffMinZ - InteriorMinZ),
                mats.PlasterWall, keepCollider: false, isStatic: false);
            EnsureBox(group, "Cabin_CeilingFrame_N",
                new Vector3((diffMinX + diffMaxX) * 0.5f, soffitMidY, (diffMaxZ + InteriorMaxZ) * 0.5f),
                new Vector3(diffMaxX - diffMinX, soffitT, InteriorMaxZ - diffMaxZ),
                mats.PlasterWall, keepCollider: false, isStatic: false);

            // Simple recessed emissive-only ceiling diffuser (UtilityWarmOn; ZERO Unity Light component!)
            EnsureBox(group, "Cabin_CeilingDiffuser",
                new Vector3((diffMinX + diffMaxX) * 0.5f, -47.708f, (diffMinZ + diffMaxZ) * 0.5f),
                new Vector3(diffMaxX - diffMinX - 0.020f, 0.012f, diffMaxZ - diffMinZ - 0.020f),
                mats.UtilityWarmOn, keepCollider: false, isStatic: false);
        }

        // =====================================================================
        // 4. CABIN-MOUNTED GUIDE SHOE ASSEMBLIES (4 U-CHANNEL SLIDING SHOES)
        // =====================================================================
        private static void BuildCabinGuideShoes(Transform group, CabinMaterials mats)
        {
            BuildSingleGuideShoe(EnsureSubgroup(group, "GuideShoe_South_Lower", isStatic: false), isSouth: true, LowerShoeCenterY, mats);
            BuildSingleGuideShoe(EnsureSubgroup(group, "GuideShoe_South_Upper", isStatic: false), isSouth: true, UpperShoeCenterY, mats);
            BuildSingleGuideShoe(EnsureSubgroup(group, "GuideShoe_North_Lower", isStatic: false), isSouth: false, LowerShoeCenterY, mats);
            BuildSingleGuideShoe(EnsureSubgroup(group, "GuideShoe_North_Upper", isStatic: false), isSouth: false, UpperShoeCenterY, mats);
        }

        private static void BuildSingleGuideShoe(Transform shoeGroup, bool isSouth, float centerY, CabinMaterials mats)
        {
            // Deliberate non-overlapping clearances:
            //   - Blade X in [+2.162, +2.178] -> West Cheek X in [+2.136, +2.158] (+4mm), East Cheek X in [+2.182, +2.204] (+4mm)
            //   - South rail tip +4.580 -> Saddle Z in [+4.586, +4.608] (+6mm tip), MountPlate [+4.608, +4.620], Cheeks [+4.548, +4.586] (+8mm to flange +4.540)
            //   - North rail tip +6.600 -> Saddle Z in [+6.572, +6.594] (+6mm tip), MountPlate [+6.560, +6.572], Cheeks [+6.594, +6.632] (+8mm to flange +6.640)
            float mountMinZ, mountMaxZ, saddleMinZ, saddleMaxZ, cheekMinZ, cheekMaxZ;
            if (isSouth)
            {
                mountMaxZ = CabinMinZ;                                          // +4.620f
                mountMinZ = mountMaxZ - 0.012f;                                 // +4.608f
                saddleMaxZ = mountMinZ;                                         // +4.608f
                saddleMinZ = SouthRailBladeTipZ + ShoeTipClearanceZ;            // +4.586f (22mm saddle)
                cheekMaxZ = saddleMinZ;                                         // +4.586f
                cheekMinZ = SouthRailFlangeMaxZ + ShoeFlangeClearanceZ;         // +4.548f (38mm engagement along 40mm blade!)
            }
            else
            {
                mountMinZ = CabinMaxZ;                                          // +6.560f
                mountMaxZ = mountMinZ + 0.012f;                                 // +6.572f
                saddleMinZ = mountMaxZ;                                         // +6.572f
                saddleMaxZ = NorthRailBladeTipZ - ShoeTipClearanceZ;            // +6.594f (22mm saddle)
                cheekMinZ = saddleMaxZ;                                         // +6.594f
                cheekMaxZ = NorthRailFlangeMinZ - ShoeFlangeClearanceZ;         // +6.632f (38mm engagement along 40mm blade!)
            }

            float bladeMinX = RailCenterX - RailBladeWidthX * 0.5f;             // +2.162f
            float bladeMaxX = RailCenterX + RailBladeWidthX * 0.5f;             // +2.178f
            float cheekWMaxX = bladeMinX - ShoeLateralClearanceX;               // +2.158f
            float cheekWMinX = cheekWMaxX - 0.022f;                             // +2.136f
            float cheekEMinX = bladeMaxX + ShoeLateralClearanceX;               // +2.182f
            float cheekEMaxX = cheekEMinX + 0.022f;                             // +2.204f

            EnsureBox(shoeGroup, "MountPlate",
                new Vector3(RailCenterX, centerY, (mountMinZ + mountMaxZ) * 0.5f),
                new Vector3(0.120f, 0.180f, mountMaxZ - mountMinZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);

            EnsureBox(shoeGroup, "Saddle",
                new Vector3(RailCenterX, centerY, (saddleMinZ + saddleMaxZ) * 0.5f),
                new Vector3(cheekEMaxX - cheekWMinX, 0.140f, saddleMaxZ - saddleMinZ),
                mats.IndustrialMetal, keepCollider: true, isStatic: false);

            EnsureBox(shoeGroup, "CheekW",
                new Vector3((cheekWMinX + cheekWMaxX) * 0.5f, centerY, (cheekMinZ + cheekMaxZ) * 0.5f),
                new Vector3(cheekWMaxX - cheekWMinX, 0.140f, cheekMaxZ - cheekMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);

            EnsureBox(shoeGroup, "CheekE",
                new Vector3((cheekEMinX + cheekEMaxX) * 0.5f, centerY, (cheekMinZ + cheekMaxZ) * 0.5f),
                new Vector3(cheekEMaxX - cheekEMinX, 0.140f, cheekMaxZ - cheekMinZ),
                mats.ElevatorDoorMetal, keepCollider: true, isStatic: false);
        }

        private static Transform EnsureSubgroup(Transform parent, string name, bool isStatic, Vector3? worldPos = null)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

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

            if (isStatic)
            {
                go.isStatic = true;
            }
            else
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
            }

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

            if (isStatic)
            {
                go.isStatic = true;
            }
            else
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
            }

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

        private static void EnforceNonStaticRecursive(Transform root)
        {
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);
            for (int i = 0; i < root.childCount; i++)
            {
                EnforceNonStaticRecursive(root.GetChild(i));
            }
        }

        // =====================================================================
        // READ-ONLY DIAGNOSTIC VISUAL VERIFICATION (6 VIEWS — NEVER SAVES SCENE)
        // =====================================================================
        public static void RenderCabinAndGuideSystemDiagnosticViews()
        {
            string outDir = Environment.GetEnvironmentVariable("REMANENCIA_0727_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "scratch_renders_0727"));
            }
            Directory.CreateDirectory(outDir);

            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath} for 07.27 diagnostic renders.");
            }

            GameObject camGo = new GameObject("__Diag0727Cam__");
            GameObject keyLightGo = new GameObject("__Diag0727KeyLight__");
            GameObject fillLightGo = new GameObject("__Diag0727FillLight__");
            List<(MeshRenderer renderer, bool wasEnabled)> hiddenRenderers = new List<(MeshRenderer, bool)>();
            List<GameObject> tempOverlays = new List<GameObject>();
            List<Material> tempMaterials = new List<Material>();

            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.055f, 0.065f, 1f);
                cam.nearClipPlane = 0.03f;
                cam.farClipPlane = 60f;
                cam.fieldOfView = 58f;

                Light keyLight = keyLightGo.AddComponent<Light>();
                keyLight.type = LightType.Point;
                keyLight.range = 16f;
                keyLight.intensity = 5.5f;
                keyLight.color = new Color(1.00f, 0.96f, 0.90f);
                keyLight.shadows = LightShadows.None;

                Light fillLight = fillLightGo.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.range = 14f;
                fillLight.intensity = 3.2f;
                fillLight.color = new Color(0.82f, 0.88f, 0.96f);
                fillLight.shadows = LightShadows.None;

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
                        if (hiddenRenderers[i].renderer != null) hiddenRenderers[i].renderer.enabled = hiddenRenderers[i].wasEnabled;
                    }
                    hiddenRenderers.Clear();
                }

                // View 1: Cabin exterior at lower stop, viewed from the landing side (East looking West, lower Bay 1 doors temporarily hidden)
                HideByName("Hotel_ElevatorBay_1_DoorLeafS", "Hotel_ElevatorBay_1_DoorLeafN", "Hotel_ElevatorBay_1_CenterSeam",
                    "Hotel_ElevatorCore_FrontPierS", "Hotel_ElevatorCore_FrontPierMid", "Hotel_ElevatorCore_FrontHeader");
                keyLightGo.transform.position = new Vector3(3.80f, -48.50f, 5.75f);
                fillLightGo.transform.position = new Vector3(2.20f, -48.40f, 5.59f);
                RenderView(cam, new Vector3(4.55f, -48.75f, 5.75f), new Vector3(2.60f, -48.80f, 5.65f),
                    58f, Path.Combine(outDir, "verify_0727_01_cabin_exterior_from_landing.png"));
                RestoreHidden();

                // View 2: Cabin interior cutaway showing dimensions, finishes, wainscoting, handrail, blank panel & emissive diffuser
                HideByName("Cabin_WallEast_Group", "Hotel_ElevatorBay_1_DoorLeafS", "Hotel_ElevatorBay_1_DoorLeafN",
                    "Hotel_ElevatorBay_1_CenterSeam", "Hotel_ElevatorCore_FrontPierS", "Hotel_ElevatorCore_FrontPierMid",
                    "Cabin_EastReturnSouth", "Cabin_EastReturnSouth_FinishSkin", "Cabin_FutureControlPanel_BlankReserve",
                    "Cabin_WallSouth_Carcass", "Cabin_FinishSouth_Baseboard", "Cabin_FinishSouth_Wainscot",
                    "Cabin_FinishSouth_ChairRail", "Cabin_FinishSouth_UpperWall", "Cabin_FinishSouth_CrownTrim",
                    "Hotel_ElevatorCore_SouthWall", "Hotel_ElevatorCore_SouthWall_Baseboard", "Pillar_CentralHall_9");
                keyLightGo.transform.position = new Vector3(2.35f, -48.20f, 5.60f);
                fillLightGo.transform.position = new Vector3(2.80f, -49.10f, 4.90f);
                RenderView(cam, new Vector3(3.30f, -48.65f, 4.25f), new Vector3(1.85f, -48.85f, 5.85f),
                    64f, Path.Combine(outDir, "verify_0727_02_cabin_interior_cutaway.png"));
                RestoreHidden();

                // View 3: Guide-rail cross-section close-up (North tight 120mm pocket: Divider -> Bracket -> T-Rail -> U-Channel Guide Shoe -> Cabin)
                HideByName("UpperShaft_RoofCap", "Cabin_RoofSlab", "Cabin_CeilingFrame_N", "Backrooms_Ceiling_West");
                keyLight.intensity = 1.1f;
                fillLight.intensity = 0.5f;
                keyLightGo.transform.position = new Vector3(1.90f, -47.00f, 6.30f);
                fillLightGo.transform.position = new Vector3(2.45f, -47.40f, 6.55f);
                RenderView(cam, new Vector3(2.17f, -47.22f, 6.55f), new Vector3(2.17f, -47.82f, 6.62f),
                    48f, Path.Combine(outDir, "verify_0727_03_guide_rail_shoe_bracket_cross_section.png"));
                keyLight.intensity = 5.5f;
                fillLight.intensity = 3.2f;
                RestoreHidden();

                // View 4: Vertical section showing full 6.33m rail coverage & 5 bracket tiers across lower-to-upper travel range
                HideByName("UpperShaft_SouthWall", "Hotel_ElevatorCore_SouthWall", "Hotel_ElevatorCore_SouthWall_Baseboard",
                    "UpperElev_Waiting_WallSouth", "UpperElev_Support_WaitingGirderS", "Backrooms_Ceiling_South",
                    "Hotel_Lobby_Beam_West", "Pillar_CentralHall_9", "Pillar_CentralHall_9_Baseboard",
                    "Bay1Shaft_GuideRail_South");
                keyLightGo.transform.position = new Vector3(2.20f, -45.50f, 3.80f);
                fillLightGo.transform.position = new Vector3(2.20f, -48.50f, 3.80f);
                RenderView(cam, new Vector3(2.20f, -46.80f, 0.85f), new Vector3(2.17f, -46.80f, 6.20f),
                    64f, Path.Combine(outDir, "verify_0727_04_vertical_section_rail_coverage.png"));
                RestoreHidden();

                // View 5: Top-down shaft view showing parked cabin, both rails, upper guide shoes, divider, and Bay 2 isolation
                HideByName("UpperShaft_RoofCap", "Bay1Shaft_DividerTopCoping", "UpperElev_Waiting_CeilingSlab",
                    "UpperElev_Waiting_Strip_Backplate", "UpperElev_Waiting_Strip_Tube");
                keyLightGo.transform.position = new Vector3(2.17f, -44.20f, 6.00f);
                fillLightGo.transform.position = new Vector3(2.17f, -47.20f, 5.60f);
                RenderView(cam, new Vector3(2.20f, -42.30f, 6.10f), new Vector3(2.17f, -48.60f, 6.12f),
                    56f, Path.Combine(outDir, "verify_0727_05_topdown_cabin_rails_divider_bay2.png"));
                RestoreHidden();

                // View 6: East-facing cabin entrance & temporary diagnostic overlay of reserved 2-speed telescopic door swept volumes
                HideByName("Hotel_ElevatorBay_1_DoorLeafS", "Hotel_ElevatorBay_1_DoorLeafN", "Hotel_ElevatorBay_1_CenterSeam",
                    "Hotel_ElevatorCore_FrontPierS", "Hotel_ElevatorCore_FrontPierMid", "Hotel_ElevatorCore_FrontHeader",
                    "Hotel_ElevatorBay_1_FrameJambS", "Hotel_ElevatorBay_1_FrameJambN", "Hotel_ElevatorBay_1_FrameHeader",
                    "Hotel_ElevatorCore_SouthWall", "Pillar_CentralHall_9");

                // Temporary read-only translucent cyan/amber prisms illustrating reserved telescopic door volumes (destroyed in finally!)
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material cabSweepMat = new Material(sh);
                cabSweepMat.SetColor("_BaseColor", new Color(0.15f, 0.78f, 0.92f, 1f));
                tempMaterials.Add(cabSweepMat);
                Material landSweepMat = new Material(sh);
                landSweepMat.SetColor("_BaseColor", new Color(0.95f, 0.62f, 0.16f, 1f));
                tempMaterials.Add(landSweepMat);

                GameObject AddOverlay(string n, Vector3 pos, Vector3 scl, Material m)
                {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = n;
                    g.transform.position = pos;
                    g.transform.localScale = scl;
                    UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                    g.GetComponent<MeshRenderer>().sharedMaterial = m;
                    tempOverlays.Add(g);
                    return g;
                }

                // Thin top/bottom marker bars showing reserved Cabin door X=[2.965,3.025] & Landing door X=[3.060,3.120], Z=[4.720,6.400]
                AddOverlay("__DiagCabinDoorSweep__", new Vector3(2.995f, -49.940f, 5.560f), new Vector3(0.060f, 0.030f, 1.680f), cabSweepMat);
                AddOverlay("__DiagLandingDoorSweep__", new Vector3(3.090f, -49.940f, 5.560f), new Vector3(0.060f, 0.030f, 1.680f), landSweepMat);
                AddOverlay("__DiagCabinDoorSweepTop__", new Vector3(2.995f, -47.780f, 5.560f), new Vector3(0.060f, 0.030f, 1.680f), cabSweepMat);
                AddOverlay("__DiagLandingDoorSweepTop__", new Vector3(3.090f, -47.780f, 5.560f), new Vector3(0.060f, 0.030f, 1.680f), landSweepMat);

                keyLightGo.transform.position = new Vector3(3.80f, -48.60f, 5.20f);
                fillLightGo.transform.position = new Vector3(2.50f, -48.60f, 5.60f);
                RenderView(cam, new Vector3(4.15f, -48.55f, 3.85f), new Vector3(2.92f, -48.85f, 5.60f),
                    58f, Path.Combine(outDir, "verify_0727_06_entrance_and_reserved_door_sweep.png"));
                RestoreHidden();
            }
            finally
            {
                for (int i = hiddenRenderers.Count - 1; i >= 0; i--)
                {
                    if (hiddenRenderers[i].renderer != null) hiddenRenderers[i].renderer.enabled = hiddenRenderers[i].wasEnabled;
                }
                for (int i = 0; i < tempOverlays.Count; i++)
                {
                    if (tempOverlays[i] != null) UnityEngine.Object.DestroyImmediate(tempOverlays[i]);
                }
                for (int i = 0; i < tempMaterials.Count; i++)
                {
                    if (tempMaterials[i] != null) UnityEngine.Object.DestroyImmediate(tempMaterials[i]);
                }
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(keyLightGo);
                UnityEngine.Object.DestroyImmediate(fillLightGo);
            }

            Debug.Log($"[UpperPassengerElevatorCabinBuilder] Rendered 6 diagnostic views to {outDir}.");
        }

        private static void RenderView(Camera cam, Vector3 position, Vector3 lookTarget, float fov, string outputPath)
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
