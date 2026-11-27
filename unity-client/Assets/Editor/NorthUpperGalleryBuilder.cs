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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.17:
    /// North Upper Gallery (first controlled fragment of Approved Alternative 1).
    ///
    /// Opens the pre-framed East aperture of the 07.15 Northwest Service Staircase upper landing
    /// (NWStair_UpperShell_WallEast preserved in-place as South pier; SealedEastFrameS/N/Top
    /// preserved untouched with exact fileIDs) and builds a physically traversable 3.00m-wide
    /// unfinished concrete upper gallery spanning X in [-9.49, +15.87], Z in [+29.80, +32.80]
    /// at Y = -46.400m (ceiling soffit Y = -43.550m, 2.850m clear height), terminating at a
    /// solid, collision-protected Eastern wall aligned with NorthWing_OuterWall_East (X = +16.00).
    ///
    /// Explicitly defers the Courtyard Lightwell Overlook, East Overhang, room numbering/signage,
    /// and Twin Elevator Core. Adds ZERO Unity Light components (exact 18-light budget preserved),
    /// ZERO new materials/textures, and ZERO runtime MonoBehaviours.
    /// </summary>
    public static class NorthUpperGalleryBuilder
    {
        public const string RootName = BackroomsLevel1Builder.NorthUpperGalleryRootName;

        // Verified East opening coordinates aligned with 07.15 SealedEastFrameS/N/Top
        public const float PortalWallCenterX = NorthwestServiceStaircaseBuilder.EnvelopeMaxX; // -9.60f
        public const float PortalWallThickness = 0.22f;
        public const float PortalMinZ = 30.28f;    // Inner North face of SealedEastFrameS (30.20 + 0.08)
        public const float PortalMaxZ = 32.32f;    // Inner South face of SealedEastFrameN (32.40 - 0.08)
        public const float PortalCenterZ = 31.30f;
        public const float PortalClearWidth = PortalMaxZ - PortalMinZ;                        // 2.04f
        public const float PortalClearHeight = 2.24f;                                         // -46.40 to -44.16
        public const float PortalHeaderBottomY = NorthwestServiceStaircaseBuilder.UpperLandingY + PortalClearHeight; // -44.16f

        // Trimmed 07.15 NWStair_UpperShell_WallEast (South pier) & BaseboardEast preserved in-place
        public const float TrimmedEastWallSouthCenterZ = 29.475f;  // Z in [28.67, 30.28]
        public const float TrimmedEastWallSouthLengthZ = 1.610f;
        public const float TrimmedEastBaseboardSouthCenterZ = 29.595f; // Z in [28.91, 30.28]
        public const float TrimmedEastBaseboardSouthLengthZ = 1.370f;

        // Exact North Upper Gallery envelope (strictly within North Wing East boundary X <= +16.00;
        // strictly North of Courtyard Lightwell SkylightCap Zmax = +26.30 with +3.28m clearance)
        public const float GalleryFloorY = NorthwestServiceStaircaseBuilder.UpperLandingY;       // -46.400f
        public const float GalleryCeilingSoffitY = NorthwestServiceStaircaseBuilder.UpperCeilingSoffitY; // -43.550f
        public const float GalleryClearHeight = NorthwestServiceStaircaseBuilder.UpperClearHeight; // 2.850f
        public const float SlabThickness = 0.20f;

        public const float GalleryClearMinX = -9.490f;    // East outer face of PortalWall (-9.60 + 0.11)
        public const float GalleryClearMaxX = 15.870f;    // West inner face of TerminalWallEast (+16.00 - 0.13)
        public const float GalleryClearLengthX = GalleryClearMaxX - GalleryClearMinX;            // 25.360f
        public const float GalleryTerminalWallCenterX = 16.000f;
        public const float GalleryTerminalWallThickness = 0.26f;

        public const float GalleryClearMinZ = 29.800f;
        public const float GalleryClearMaxZ = 32.800f;
        public const float GalleryClearWidthZ = GalleryClearMaxZ - GalleryClearMinZ;             // 3.000f
        public const float GalleryWallThickness = 0.22f;
        public const float GalleryWallSouthCenterZ = GalleryClearMinZ - GalleryWallThickness * 0.5f; // 29.690f (outer 29.580)
        public const float GalleryWallNorthCenterZ = GalleryClearMaxZ + GalleryWallThickness * 0.5f; // 32.910f (outer 33.020)

        private struct GalleryMaterials
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

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.17 North Upper Gallery")]
        public static void MigrateNorthUpperGalleryAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateNorthUpperGalleryInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.17 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[NorthUpperGalleryBuilder] Implementation 07.17 North Upper Gallery migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.17.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateNorthUpperGalleryInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"NorthUpperGalleryBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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

            Transform stairRoot = shell.Find(NorthwestServiceStaircaseBuilder.RootName);
            if (stairRoot == null)
            {
                throw new InvalidOperationException(
                    $"Expected '{NorthwestServiceStaircaseBuilder.RootName}' before running 07.17 North Upper Gallery migration.");
            }

            GalleryMaterials mats = LoadExistingHotelMaterials();

            // 1. Open the authorized East doorway in NWStair_UpperShell_WallEast in-place
            //    (preserves WallEast, BaseboardEast & SealedEastFrameS/N/Top fileIDs verbatim!)
            TrimUpperShellEastWallInPlace(stairRoot);

            // 2. Build/update dedicated Hotel_NorthUpperGallery root cleanly and idempotently
            Transform galleryRoot = EnsureSubgroup(shell, RootName);

            BuildPortalTransition(EnsureSubgroup(galleryRoot, "NUGallery_PortalTransition"), mats);
            BuildGalleryStructure(EnsureSubgroup(galleryRoot, "NUGallery_Structure"), mats);
            BuildArchitecturalDetails(EnsureSubgroup(galleryRoot, "NUGallery_Details"), mats);
            BuildEastTerminal(EnsureSubgroup(galleryRoot, "NUGallery_EastTerminal"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform root = architectureShell.Find(RootName);
            if (root == null) return false;
            return root.Find("NUGallery_PortalTransition") != null &&
                   root.Find("NUGallery_Structure") != null &&
                   root.Find("NUGallery_Details") != null &&
                   root.Find("NUGallery_EastTerminal") != null;
        }

        private static GalleryMaterials LoadExistingHotelMaterials()
        {
            return new GalleryMaterials
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

        private static void TrimUpperShellEastWallInPlace(Transform stairRoot)
        {
            Transform upperShell = stairRoot.Find("NWStair_UpperLandingShell");
            if (upperShell == null)
            {
                throw new InvalidOperationException("NWStair_UpperLandingShell not found under Hotel_NorthwestServiceStaircase.");
            }

            Transform wallEast = upperShell.Find("NWStair_UpperShell_WallEast");
            if (wallEast == null)
            {
                throw new InvalidOperationException("NWStair_UpperShell_WallEast not found.");
            }

            float wallBotY = BackroomsLevel1Builder.CeilingY;      // -47.15f
            float wallH = GalleryCeilingSoffitY - wallBotY;        // 3.60f
            float wallMidY = wallBotY + wallH * 0.5f;              // -45.35f

            wallEast.position = new Vector3(PortalWallCenterX, wallMidY, TrimmedEastWallSouthCenterZ);
            wallEast.rotation = Quaternion.identity;
            wallEast.localScale = new Vector3(PortalWallThickness, wallH, TrimmedEastWallSouthLengthZ);

            Transform baseEast = upperShell.Find("NWStair_UpperShell_BaseboardEast");
            if (baseEast != null)
            {
                baseEast.position = new Vector3(NorthwestServiceStaircaseBuilder.ClearMaxX - 0.01f, GalleryFloorY + 0.06f, TrimmedEastBaseboardSouthCenterZ);
                baseEast.rotation = Quaternion.identity;
                baseEast.localScale = new Vector3(0.03f, 0.12f, TrimmedEastBaseboardSouthLengthZ);
            }
        }

        private static void BuildPortalTransition(Transform group, GalleryMaterials mats)
        {
            float wallBotY = BackroomsLevel1Builder.CeilingY;      // -47.15f
            float wallH = GalleryCeilingSoffitY - wallBotY;        // 3.60f
            float wallMidY = wallBotY + wallH * 0.5f;              // -45.35f

            // North pier completing the split East wall of the 07.15 upper shell (Z in [32.32, 34.13])
            EnsureBox(group, "NUGallery_EastWall_NorthPier",
                new Vector3(PortalWallCenterX, wallMidY, 33.225f), Quaternion.identity,
                new Vector3(PortalWallThickness, wallH, 1.810f), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NUGallery_EastWall_NorthBaseboard",
                new Vector3(NorthwestServiceStaircaseBuilder.ClearMaxX - 0.01f, GalleryFloorY + 0.06f, 33.095f), Quaternion.identity,
                new Vector3(0.03f, 0.12f, 1.550f), mats.BaseboardConcrete, keepCollider: false);

            // Solid concrete transom sealing above SealedEastFrameTop (Y in [-44.16, -43.55])
            float transomH = GalleryCeilingSoffitY - PortalHeaderBottomY; // 0.61f
            EnsureBox(group, "NUGallery_EastWall_Transom",
                new Vector3(PortalWallCenterX, PortalHeaderBottomY + transomH * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(PortalWallThickness, transomH, PortalClearWidth), mats.WallConcrete, keepCollider: true);

            // Solid sub-floor masonry kick closing the plenum gap below the doorway (Y in [-47.15, -46.40])
            float subH = GalleryFloorY - wallBotY; // 0.75f
            EnsureBox(group, "NUGallery_EastWall_SubfloorKick",
                new Vector3(PortalWallCenterX, wallBotY + subH * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(PortalWallThickness, subH, PortalClearWidth), mats.WallConcrete, keepCollider: true);

            // Through-wall architectural concrete reveals & flush threshold plate bridging the 2.04m opening
            EnsureBox(group, "NUGallery_Portal_RevealS",
                new Vector3(PortalWallCenterX, GalleryFloorY + PortalClearHeight * 0.5f, PortalMinZ - 0.015f), Quaternion.identity,
                new Vector3(0.26f, PortalClearHeight, 0.04f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Portal_RevealN",
                new Vector3(PortalWallCenterX, GalleryFloorY + PortalClearHeight * 0.5f, PortalMaxZ + 0.015f), Quaternion.identity,
                new Vector3(0.26f, PortalClearHeight, 0.04f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Portal_RevealTop",
                new Vector3(PortalWallCenterX, PortalHeaderBottomY + 0.015f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.26f, 0.04f, PortalClearWidth + 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Portal_ThresholdPlate",
                new Vector3(PortalWallCenterX, GalleryFloorY + 0.0025f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.32f, 0.005f, PortalClearWidth), mats.IndustrialMetal, keepCollider: false);

            // Symmetrical architectural shoulder trim on gallery side (gradual visual widening 2.04m -> 3.00m)
            EnsureBox(group, "NUGallery_Portal_EastPilasterS",
                new Vector3(GalleryClearMinX + 0.045f, GalleryFloorY + GalleryClearHeight * 0.5f, 30.04f), Quaternion.identity,
                new Vector3(0.09f, GalleryClearHeight, 0.48f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Portal_EastPilasterN",
                new Vector3(GalleryClearMinX + 0.045f, GalleryFloorY + GalleryClearHeight * 0.5f, 32.56f), Quaternion.identity,
                new Vector3(0.09f, GalleryClearHeight, 0.48f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Portal_EastLintelBeam",
                new Vector3(GalleryClearMinX + 0.08f, GalleryCeilingSoffitY - 0.14f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.18f, 0.28f, GalleryClearWidthZ), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildGalleryStructure(Transform group, GalleryMaterials mats)
        {
            float slabMinX = NorthwestServiceStaircaseBuilder.ClearMaxX; // -9.710f (butt-joins 07.15 UpperLanding_FloorSlab)
            float slabMaxX = GalleryTerminalWallCenterX + GalleryTerminalWallThickness * 0.5f; // +16.130f
            float slabLenX = slabMaxX - slabMinX;                        // 25.840f
            float slabCenterX = (slabMinX + slabMaxX) * 0.5f;            // +3.210f
            float slabWidthZ = GalleryClearWidthZ + GalleryWallThickness * 2f; // 3.440f

            // 1. Continuous walkable concrete floor slab (top Y = -46.400m, bottom Y = -46.600m, +0.348m plenum above Nivel 0 ceiling)
            EnsureBox(group, "NUGallery_FloorSlab",
                new Vector3(slabCenterX, GalleryFloorY - SlabThickness * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(slabLenX, SlabThickness, slabWidthZ), mats.FloorConcrete, keepCollider: true);

            // 2. Continuous sealed concrete ceiling slab (soffit Y = -43.550m, top Y = -43.350m, 2.850m clear headroom)
            EnsureBox(group, "NUGallery_CeilingSlab",
                new Vector3(slabCenterX, GalleryCeilingSoffitY + SlabThickness * 0.5f, PortalCenterZ), Quaternion.identity,
                new Vector3(slabLenX, SlabThickness, slabWidthZ), mats.CeilingSoffit, keepCollider: true);

            // 3. Solid continuous North and South concrete gallery walls (Y in [-46.60, -43.55], height 3.05m)
            float wallBotY = GalleryFloorY - SlabThickness;              // -46.600f
            float wallH = GalleryCeilingSoffitY - wallBotY;              // 3.050f
            float wallMidY = wallBotY + wallH * 0.5f;                    // -45.075f

            EnsureBox(group, "NUGallery_WallNorth",
                new Vector3(slabCenterX, wallMidY, GalleryWallNorthCenterZ), Quaternion.identity,
                new Vector3(slabLenX, wallH, GalleryWallThickness), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "NUGallery_WallSouth",
                new Vector3(slabCenterX, wallMidY, GalleryWallSouthCenterZ), Quaternion.identity,
                new Vector3(slabLenX, wallH, GalleryWallThickness), mats.WallConcrete, keepCollider: true);

            // Baseboards along North & South walls (non-colliding)
            float innerLenX = GalleryClearLengthX;
            float innerCenterX = (GalleryClearMinX + GalleryClearMaxX) * 0.5f; // +3.190f
            EnsureBox(group, "NUGallery_BaseboardNorth",
                new Vector3(innerCenterX, GalleryFloorY + 0.06f, GalleryClearMaxZ - 0.012f), Quaternion.identity,
                new Vector3(innerLenX, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_BaseboardSouth",
                new Vector3(innerCenterX, GalleryFloorY + 0.06f, GalleryClearMinZ + 0.012f), Quaternion.identity,
                new Vector3(innerLenX, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildArchitecturalDetails(Transform group, GalleryMaterials mats)
        {
            // 1. Rhythmic structural concrete wall pilasters & overhead cross-beams (non-colliding, shallow 0.08m relief)
            float[] bayXs = { -4.60f, 0.20f, 5.00f, 9.80f, 14.20f };
            for (int i = 0; i < bayXs.Length; i++)
            {
                float bx = bayXs[i];
                EnsureBox(group, $"NUGallery_PilasterNorth_{i}",
                    new Vector3(bx, GalleryFloorY + GalleryClearHeight * 0.5f, GalleryClearMaxZ - 0.04f), Quaternion.identity,
                    new Vector3(0.28f, GalleryClearHeight, 0.08f), mats.ColumnConcrete, keepCollider: false);
                EnsureBox(group, $"NUGallery_PilasterSouth_{i}",
                    new Vector3(bx, GalleryFloorY + GalleryClearHeight * 0.5f, GalleryClearMinZ + 0.04f), Quaternion.identity,
                    new Vector3(0.28f, GalleryClearHeight, 0.08f), mats.ColumnConcrete, keepCollider: false);
                EnsureBox(group, $"NUGallery_CeilingBeam_{i}",
                    new Vector3(bx, GalleryCeilingSoffitY - 0.14f, PortalCenterZ), Quaternion.identity,
                    new Vector3(0.28f, 0.28f, GalleryClearWidthZ), mats.ColumnConcrete, keepCollider: false);
            }

            // 2. Subtle interrupted-construction architectural cues (ZERO room numbers, ZERO signage, backed by solid walls)
            //    Two sealed/bricked-in unnumbered doorway recesses along North wall (one raw concrete, one weathered plaster skim)
            BuildSealedRecessFrameNorth(group, "NUGallery_SealedNorthBay_01", centerX: -2.20f, clearWidth: 1.32f, infillMat: mats.WallConcrete, mats);
            BuildSealedRecessFrameNorth(group, "NUGallery_SealedNorthBay_02", centerX: 7.40f, clearWidth: 1.32f, infillMat: mats.PlasterWall, mats);

            //    Restrained sealed South structural wall relief (aligned above Courtyard zone — 100% solid WallSouth behind it!)
            EnsureBox(group, "NUGallery_SealedSouthRelief_L",
                new Vector3(-1.35f, GalleryFloorY + 1.18f, GalleryClearMinZ + 0.03f), Quaternion.identity,
                new Vector3(0.14f, 2.36f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_SealedSouthRelief_R",
                new Vector3(1.75f, GalleryFloorY + 1.18f, GalleryClearMinZ + 0.03f), Quaternion.identity,
                new Vector3(0.14f, 2.36f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_SealedSouthRelief_Top",
                new Vector3(0.20f, GalleryFloorY + 2.30f, GalleryClearMinZ + 0.03f), Quaternion.identity,
                new Vector3(3.24f, 0.14f, 0.06f), mats.ColumnConcrete, keepCollider: false);

            // 3. Subtle longitudinal off-axis floor expansion seam & exposed overhead service conduit (non-colliding)
            float innerCenterX = (GalleryClearMinX + GalleryClearMaxX) * 0.5f;
            EnsureBox(group, "NUGallery_FloorJointSeam",
                new Vector3(innerCenterX, GalleryFloorY + 0.002f, PortalCenterZ + 0.14f), Quaternion.identity,
                new Vector3(GalleryClearLengthX - 0.20f, 0.004f, 0.028f), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_OverheadConduit",
                new Vector3(innerCenterX, GalleryCeilingSoffitY - 0.12f, GalleryClearMaxZ - 0.14f), Quaternion.identity,
                new Vector3(GalleryClearLengthX - 0.40f, 0.04f, 0.04f), mats.IndustrialMetal, keepCollider: false);

            // 4. Four restrained emissive-only utility strip fixtures (ZERO Unity Light components; preserves exact 18-light budget)
            float[] stripXs = { -6.00f, -1.80f, 3.80f, 11.80f };
            for (int i = 0; i < stripXs.Length; i++)
            {
                float sx = stripXs[i];
                EnsureBox(group, $"NUGallery_StripBackplate_{i}",
                    new Vector3(sx, GalleryCeilingSoffitY - 0.025f, PortalCenterZ), Quaternion.identity,
                    new Vector3(1.42f, 0.04f, 0.14f), mats.IndustrialMetal, keepCollider: false);
                EnsureBox(group, $"NUGallery_StripTube_{i}",
                    new Vector3(sx, GalleryCeilingSoffitY - 0.055f, PortalCenterZ), Quaternion.identity,
                    new Vector3(1.32f, 0.035f, 0.065f), mats.UtilityWarmOn, keepCollider: false);
            }
        }

        private static void BuildSealedRecessFrameNorth(Transform group, string prefix, float centerX, float clearWidth, Material infillMat, GalleryMaterials mats)
        {
            float halfW = clearWidth * 0.5f;
            float zFace = GalleryClearMaxZ - 0.03f;
            EnsureBox(group, $"{prefix}_JambW",
                new Vector3(centerX - halfW - 0.06f, GalleryFloorY + 1.12f, zFace), Quaternion.identity,
                new Vector3(0.12f, 2.24f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, $"{prefix}_JambE",
                new Vector3(centerX + halfW + 0.06f, GalleryFloorY + 1.12f, zFace), Quaternion.identity,
                new Vector3(0.12f, 2.24f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, $"{prefix}_Header",
                new Vector3(centerX, GalleryFloorY + 2.26f, zFace), Quaternion.identity,
                new Vector3(clearWidth + 0.24f, 0.12f, 0.06f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, $"{prefix}_InfillPanel",
                new Vector3(centerX, GalleryFloorY + 1.10f, GalleryClearMaxZ - 0.012f), Quaternion.identity,
                new Vector3(clearWidth, 2.20f, 0.024f), infillMat, keepCollider: false);
        }

        private static void BuildEastTerminal(Transform group, GalleryMaterials mats)
        {
            float wallBotY = GalleryFloorY - SlabThickness;              // -46.600f
            float wallH = GalleryCeilingSoffitY - wallBotY;              // 3.050f
            float wallMidY = wallBotY + wallH * 0.5f;                    // -45.075f
            float wallWidthZ = GalleryClearWidthZ + GalleryWallThickness * 2f; // 3.440f

            // Solid, collision-protected sealed Eastern terminal wall aligned with NorthWing_OuterWall_East (X = +16.00)
            EnsureBox(group, "NUGallery_TerminalWall_East",
                new Vector3(GalleryTerminalWallCenterX, wallMidY, PortalCenterZ), Quaternion.identity,
                new Vector3(GalleryTerminalWallThickness, wallH, wallWidthZ), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "NUGallery_Terminal_Baseboard",
                new Vector3(GalleryClearMaxX - 0.012f, GalleryFloorY + 0.06f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.025f, 0.12f, GalleryClearWidthZ), mats.BaseboardConcrete, keepCollider: false);

            // Sealed interrupted-construction continuation frame (100% solid TerminalWall_East behind it; zero passage)
            float frameX = GalleryClearMaxX - 0.035f;
            EnsureBox(group, "NUGallery_Terminal_SealedFrameS",
                new Vector3(frameX, GalleryFloorY + 1.18f, PortalMinZ - 0.08f), Quaternion.identity,
                new Vector3(0.07f, 2.36f, 0.16f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Terminal_SealedFrameN",
                new Vector3(frameX, GalleryFloorY + 1.18f, PortalMaxZ + 0.08f), Quaternion.identity,
                new Vector3(0.07f, 2.36f, 0.16f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "NUGallery_Terminal_SealedFrameTop",
                new Vector3(frameX, GalleryFloorY + 2.32f, PortalCenterZ), Quaternion.identity,
                new Vector3(0.07f, 0.16f, PortalClearWidth + 0.32f), mats.ColumnConcrete, keepCollider: false);

            // Restrained terminated conduit drop on East terminal corner (interrupted construction detail)
            EnsureBox(group, "NUGallery_Terminal_ConduitDrop",
                new Vector3(GalleryClearMaxX - 0.04f, GalleryFloorY + 1.85f, GalleryClearMaxZ - 0.18f), Quaternion.identity,
                new Vector3(0.035f, 2.00f, 0.035f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(group, "NUGallery_Terminal_JunctionBox",
                new Vector3(GalleryClearMaxX - 0.05f, GalleryFloorY + 1.55f, GalleryClearMaxZ - 0.18f), Quaternion.identity,
                new Vector3(0.07f, 0.18f, 0.14f), mats.IndustrialMetal, keepCollider: false);
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
        /// Read-only Editor verification camera utility for Implementation 07.17.
        /// Captures diagnostic views of the East opening, North Upper Gallery, and sealed East terminal.
        /// </summary>
        public static void RenderNorthUpperGalleryVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_NUGALLERY_0717_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_NUGallery_0717_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempNUGallery0717VerifyCam__");
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
                    ("verify_0717_01_stair_top_into_east_opening.png", new Vector3(-11.40f, GalleryFloorY + 1.56f, 31.30f), new Vector3( 2.00f, GalleryFloorY + 1.45f, 31.30f)),
                    ("verify_0717_02_gallery_west_looking_east.png",   new Vector3( -7.80f, GalleryFloorY + 1.56f, 31.30f), new Vector3(15.80f, GalleryFloorY + 1.45f, 31.30f)),
                    ("verify_0717_03_gallery_mid_sealed_bays.png",     new Vector3(  2.50f, GalleryFloorY + 1.56f, 30.60f), new Vector3( 9.50f, GalleryFloorY + 1.40f, 32.60f)),
                    ("verify_0717_04_sealed_east_terminal.png",        new Vector3( 11.20f, GalleryFloorY + 1.56f, 31.30f), new Vector3(15.85f, GalleryFloorY + 1.35f, 31.30f)),
                    ("verify_0717_05_gallery_looking_west_return.png", new Vector3( -2.00f, GalleryFloorY + 1.56f, 31.30f), new Vector3(-11.40f, GalleryFloorY + 1.45f, 31.30f))
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
                    Debug.Log($"[NorthUpperGalleryBuilder] Rendered 07.17 verification view: {fullFile}");
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
