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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.19:
    /// Courtyard Lightwell Overlook — Alternative C (Single-Bay Framed Architectural Observation
    /// with a +0.540m Three-Step Concrete Viewing Dais).
    ///
    /// Opens exactly one structural bay in NUGallery_WallSouth (X in [-4.460, +0.060], clear width 4.520m)
    /// between preserved pilasters NUGallery_PilasterSouth_0 and NUGallery_PilasterSouth_1, builds a
    /// compact 9.85m² observation alcove (Z in [+27.400, +29.580]) with three poured-concrete steps
    /// (3 x 0.180m risers, 3 x 0.280m treads, total rise +0.540m to Y = -45.860m), encloses the compact
    /// bay with minimal concrete sidewalls and baseline gallery ceiling continuation (Y = -43.550m,
    /// 2.310m clear headroom over dais), and installs a 1.050m solid concrete parapet (Z in [+27.200, +27.400])
    /// topped by visibly represented reinforced transparent glass safety panels in dark-steel perimeter
    /// framing and narrow mullions with aligned solid colliders.
    ///
    /// Leaves all NorthWing_Lightwell_* objects, all 5 gallery pilasters, all 5 ceiling beams, all 4
    /// emissive strips, Northwest Service Staircase, Elevators, Rooms 107-110, Room 111-B, Threshold of
    /// Trust, entry-only door, BorderCorridorScene.unity, and the exact 18-light budget 100% untouched.
    /// </summary>
    public static class CourtyardLightwellOverlookBuilder
    {
        public const string RootName = BackroomsLevel1Builder.CourtyardLightwellOverlookRootName;

        // Authorized single-bay South opening in North Upper Gallery (between PilasterSouth_0 & PilasterSouth_1)
        public const float OpeningMinX = -4.460f;
        public const float OpeningMaxX = 0.060f;
        public const float OpeningCenterX = (OpeningMinX + OpeningMaxX) * 0.5f;   // -2.200f
        public const float OpeningClearWidthX = OpeningMaxX - OpeningMinX;        //  4.520f

        // Trimmed 07.17 NUGallery_WallSouth & BaseboardSouth East segments (preserved in-place)
        public const float GallerySlabMinX = NorthwestServiceStaircaseBuilder.ClearMaxX; // -9.710f
        public const float GallerySlabMaxX = NorthUpperGalleryBuilder.GalleryTerminalWallCenterX + NorthUpperGalleryBuilder.GalleryTerminalWallThickness * 0.5f; // +16.130f
        public const float TrimmedSouthWallEastLengthX = GallerySlabMaxX - OpeningMaxX; // 16.070f
        public const float TrimmedSouthWallEastCenterX = (OpeningMaxX + GallerySlabMaxX) * 0.5f; // +8.095f
        public const float TrimmedSouthBaseboardEastLengthX = NorthUpperGalleryBuilder.GalleryClearMaxX - OpeningMaxX; // 15.810f
        public const float TrimmedSouthBaseboardEastCenterX = (OpeningMaxX + NorthUpperGalleryBuilder.GalleryClearMaxX) * 0.5f; // +7.965f

        // West segment of split Gallery South wall (built inside Overlook_GalleryTransition)
        public const float SplitSouthWallWestLengthX = OpeningMinX - GallerySlabMinX; // 5.250f
        public const float SplitSouthWallWestCenterX = (GallerySlabMinX + OpeningMinX) * 0.5f; // -7.085f
        public const float SplitSouthBaseboardWestLengthX = OpeningMinX - NorthUpperGalleryBuilder.GalleryClearMinX; // 5.030f
        public const float SplitSouthBaseboardWestCenterX = (NorthUpperGalleryBuilder.GalleryClearMinX + OpeningMinX) * 0.5f; // -6.975f

        // Compact observation alcove Z envelope (strictly North of SkylightCap Zmax = +26.300 by +0.900m..+1.100m!)
        public const float GallerySouthOuterZ = NorthUpperGalleryBuilder.GalleryClearMinZ - NorthUpperGalleryBuilder.GalleryWallThickness; // +29.580f
        public const float AlcoveWalkableMinZ = 27.400f;
        public const float AlcoveWalkableDepthZ = GallerySouthOuterZ - AlcoveWalkableMinZ; // 2.180f
        public const float ParapetThicknessZ = 0.200f;
        public const float ParapetOuterMinZ = AlcoveWalkableMinZ - ParapetThicknessZ;      // +27.200f
        public const float ParapetCenterZ = (ParapetOuterMinZ + AlcoveWalkableMinZ) * 0.5f; // +27.300f
        public const float SideWallThicknessX = 0.220f;
        public const float EnclosureOuterMinX = OpeningMinX - SideWallThicknessX;          // -4.680f
        public const float EnclosureOuterMaxX = OpeningMaxX + SideWallThicknessX;          // +0.280f

        // Elevations & three-step viewing dais geometry (matches 07.15 riser/tread module)
        public const float GalleryFloorY = NorthUpperGalleryBuilder.GalleryFloorY;         // -46.400f
        public const float SlabBottomY = GalleryFloorY - NorthUpperGalleryBuilder.SlabThickness; // -46.600f
        public const float CeilingSoffitY = NorthUpperGalleryBuilder.GalleryCeilingSoffitY; // -43.550f
        public const float CeilingSlabThickness = NorthUpperGalleryBuilder.SlabThickness;  //  0.200f
        public const int StepCount = 3;
        public const float RiserHeight = 0.180f;
        public const float TreadDepth = 0.280f;
        public const float StepsTotalRise = StepCount * RiserHeight;                       //  0.540f
        public const float StepsHorizontalRun = StepCount * TreadDepth;                    //  0.840f
        public const float DaisFloorY = GalleryFloorY + StepsTotalRise;                    // -45.860f
        public const float DaisNorthEdgeZ = GallerySouthOuterZ - StepsHorizontalRun;       // +28.740f
        public const float DaisLevelDepthZ = DaisNorthEdgeZ - AlcoveWalkableMinZ;          //  1.340f
        public const float DaisClearHeadroom = CeilingSoffitY - DaisFloorY;                //  2.310f

        // Guard parapet & transparent reinforced glass barrier
        public const float ParapetGuardHeight = 1.050f;
        public const float ParapetTopY = DaisFloorY + ParapetGuardHeight;                  // -44.810f
        public const float SteelSillHeight = 0.040f;
        public const float GlassBottomY = ParapetTopY + SteelSillHeight;                   // -44.770f
        public const float TopHeaderHeight = 0.060f;
        public const float GlassTopY = CeilingSoffitY - TopHeaderHeight;                   // -43.610f
        public const float GlassHeight = GlassTopY - GlassBottomY;                         //  1.160f
        public const float GlassThicknessZ = 0.024f;
        public const float FrameJambWidthX = 0.055f;
        public const float MullionWidthX = 0.048f;

        private struct OverlookMaterials
        {
            public Material WallConcrete;
            public Material FloorConcrete;
            public Material CeilingSoffit;
            public Material ColumnConcrete;
            public Material BaseboardConcrete;
            public Material IndustrialMetal;
            public Material SafetyGlass;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.19 Courtyard Lightwell Overlook")]
        public static void MigrateCourtyardLightwellOverlookAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateCourtyardLightwellOverlookInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.19 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[CourtyardLightwellOverlookBuilder] Implementation 07.19 Courtyard Lightwell Overlook migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.19.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateCourtyardLightwellOverlookInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"CourtyardLightwellOverlookBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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
                    $"Expected '{NorthUpperGalleryBuilder.RootName}' before running 07.19 Courtyard Lightwell Overlook migration.");
            }

            OverlookMaterials mats = LoadMaterials();

            // 1. Open exactly one structural bay (X in [-4.460, +0.060]) in NUGallery_WallSouth in-place
            //    while preserving all 5 South pilasters, all 5 ceiling beams, and all 4 utility strips!
            TrimGallerySouthWallInPlace(galleryRoot);

            // 2. Build/update dedicated compact Hotel_CourtyardLightwellOverlook root cleanly and idempotently
            Transform overlookRoot = EnsureSubgroup(shell, RootName);

            BuildGalleryTransition(EnsureSubgroup(overlookRoot, "Overlook_GalleryTransition"), mats);
            BuildThreeStepDais(EnsureSubgroup(overlookRoot, "Overlook_ViewingDais"), mats);
            BuildCompactEnclosure(EnsureSubgroup(overlookRoot, "Overlook_CompactEnclosure"), mats);
            BuildSafetyBarrier(EnsureSubgroup(overlookRoot, "Overlook_SafetyBarrier"), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform root = architectureShell.Find(RootName);
            if (root == null) return false;
            return root.Find("Overlook_GalleryTransition") != null &&
                   root.Find("Overlook_ViewingDais") != null &&
                   root.Find("Overlook_CompactEnclosure") != null &&
                   root.Find("Overlook_SafetyBarrier") != null;
        }

        private static OverlookMaterials LoadMaterials()
        {
            return new OverlookMaterials
            {
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath),
                FloorConcrete = LoadRequiredMat(BackroomsLevel1Builder.FloorConcreteMatPath),
                CeilingSoffit = LoadRequiredMat(BackroomsLevel1Builder.CeilingSoffitMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                BaseboardConcrete = LoadRequiredMat(BackroomsLevel1Builder.BaseboardMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                SafetyGlass = EnsureDedicatedSafetyGlassMaterial(BackroomsLevel1Builder.OverlookSafetyGlassMatPath)
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
        /// Creates or updates the dedicated, localized URP Transparent reinforced safety glass material
        /// exclusively for the 07.19 Courtyard Lightwell Overlook barrier (zero modification to any shared
        /// or surface-world glass material).
        /// </summary>
        private static Material EnsureDedicatedSafetyGlassMaterial(string assetPath)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            bool created = false;
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, assetPath);
                created = true;
            }
            else if (urpLit != null && mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            // Subtle architectural laminated-glass cool-slate tint: visibly legible, ~80% optical clarity
            Color glassTint = new Color(0.56f, 0.64f, 0.69f, 0.20f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glassTint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", glassTint);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.06f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // Alpha
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Back);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
            if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 1f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.DisableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;

            EditorUtility.SetDirty(mat);
            if (created) AssetDatabase.SaveAssets();
            return mat;
        }

        private static void TrimGallerySouthWallInPlace(Transform galleryRoot)
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

            bool hasUpperElevatorApproach = galleryRoot.parent != null &&
                                            galleryRoot.parent.Find(BackroomsLevel1Builder.UpperElevatorApproachRootName) != null;
            float southWallCenterX = hasUpperElevatorApproach
                ? UpperElevatorApproachBuilder.TrimmedSouthWallMidCenterX
                : TrimmedSouthWallEastCenterX;
            float southWallLenX = hasUpperElevatorApproach
                ? UpperElevatorApproachBuilder.TrimmedSouthWallMidLengthX
                : TrimmedSouthWallEastLengthX;
            float southBaseCenterX = hasUpperElevatorApproach
                ? UpperElevatorApproachBuilder.TrimmedSouthBaseboardMidCenterX
                : TrimmedSouthBaseboardEastCenterX;
            float southBaseLenX = hasUpperElevatorApproach
                ? UpperElevatorApproachBuilder.TrimmedSouthBaseboardMidLengthX
                : TrimmedSouthBaseboardEastLengthX;

            // Retain NUGallery_WallSouth GameObject/fileID in-place as East (07.19) or Mid (07.23) segment
            wallSouth.position = new Vector3(southWallCenterX, wallMidY, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ);
            wallSouth.rotation = Quaternion.identity;
            wallSouth.localScale = new Vector3(southWallLenX, wallH, NorthUpperGalleryBuilder.GalleryWallThickness);

            Transform baseSouth = structure.Find("NUGallery_BaseboardSouth");
            if (baseSouth != null)
            {
                baseSouth.position = new Vector3(southBaseCenterX, GalleryFloorY + 0.06f, NorthUpperGalleryBuilder.GalleryClearMinZ + 0.012f);
                baseSouth.rotation = Quaternion.identity;
                baseSouth.localScale = new Vector3(southBaseLenX, 0.12f, 0.025f);
            }

            // Replace the 07.17 sealed South decorative relief spanning X in [-1.42, +1.82]
            Transform details = galleryRoot.Find("NUGallery_Details");
            if (details != null)
            {
                string[] replacedReliefs = { "NUGallery_SealedSouthRelief_L", "NUGallery_SealedSouthRelief_R", "NUGallery_SealedSouthRelief_Top" };
                for (int i = 0; i < replacedReliefs.Length; i++)
                {
                    Transform oldRelief = details.Find(replacedReliefs[i]);
                    if (oldRelief != null) UnityEngine.Object.DestroyImmediate(oldRelief.gameObject);
                }
            }
        }

        private static void BuildGalleryTransition(Transform group, OverlookMaterials mats)
        {
            float wallH = CeilingSoffitY - SlabBottomY; // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f; // -45.075f

            // West segment completing the split NUGallery_WallSouth (X in [-9.710, -4.460])
            EnsureBox(group, "Overlook_WallSouth_WestSegment",
                new Vector3(SplitSouthWallWestCenterX, wallMidY, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ), Quaternion.identity,
                new Vector3(SplitSouthWallWestLengthX, wallH, NorthUpperGalleryBuilder.GalleryWallThickness), mats.WallConcrete, keepCollider: true);

            EnsureBox(group, "Overlook_BaseboardSouth_WestSegment",
                new Vector3(SplitSouthBaseboardWestCenterX, GalleryFloorY + 0.06f, NorthUpperGalleryBuilder.GalleryClearMinZ + 0.012f), Quaternion.identity,
                new Vector3(SplitSouthBaseboardWestLengthX, 0.12f, 0.025f), mats.BaseboardConcrete, keepCollider: false);

            // Coherent architectural concrete portal reveals framing the 4.520m x 2.850m single-bay opening (non-colliding;
            // proud by 5mm from the X=-4.460/+0.060 wall faces and 2mm below the Y=-43.550 soffit to eliminate coplanar Z-fighting)
            float portalH = CeilingSoffitY - GalleryFloorY - 0.004f; // 2.846f
            float portalMidY = GalleryFloorY + 0.002f + portalH * 0.5f;
            EnsureBox(group, "Overlook_Portal_RevealW",
                new Vector3(OpeningMinX - 0.015f, portalMidY, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ), Quaternion.identity,
                new Vector3(0.040f, portalH, NorthUpperGalleryBuilder.GalleryWallThickness + 0.04f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "Overlook_Portal_RevealE",
                new Vector3(OpeningMaxX + 0.015f, portalMidY, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ), Quaternion.identity,
                new Vector3(0.040f, portalH, NorthUpperGalleryBuilder.GalleryWallThickness + 0.04f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "Overlook_Portal_RevealTop",
                new Vector3(OpeningCenterX, CeilingSoffitY - 0.042f, NorthUpperGalleryBuilder.GalleryWallSouthCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX - 0.010f, 0.080f, NorthUpperGalleryBuilder.GalleryWallThickness + 0.04f), mats.ColumnConcrete, keepCollider: false);

            // Flush industrial-metal threshold seam plate at base of Step 1 (non-colliding, zero collision lip)
            EnsureBox(group, "Overlook_Portal_ThresholdPlate",
                new Vector3(OpeningCenterX, GalleryFloorY + 0.0025f, GallerySouthOuterZ + 0.060f), Quaternion.identity,
                new Vector3(OpeningClearWidthX, 0.005f, 0.140f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void BuildThreeStepDais(Transform group, OverlookMaterials mats)
        {
            // Three recognizable individual poured-concrete steps rising Southward (moving -Z from Z=+29.580 toward Lightwell)
            //   Step 1: Z in [29.300, 29.580], top Y = -46.220 (+0.180m)
            //   Step 2: Z in [29.020, 29.300], top Y = -46.040 (+0.360m)
            //   Step 3: Z in [28.740, 29.020], top Y = -45.860 (+0.540m, flush into raised viewing dais)
            // Each step extends monolithically down to SlabBottomY (-46.600m) with its own 1:1 BoxCollider
            // (riser 0.180m is well below CharacterController.stepOffset = 0.300m -> zero floating, zero penetration!).
            for (int i = 0; i < StepCount; i++)
            {
                float stepTopY = GalleryFloorY + (i + 1) * RiserHeight;
                float stepHeight = stepTopY - SlabBottomY;
                float stepCenterY = SlabBottomY + stepHeight * 0.5f;
                float stepMaxZ = GallerySouthOuterZ - i * TreadDepth;
                float stepMinZ = stepMaxZ - TreadDepth;
                float stepCenterZ = (stepMinZ + stepMaxZ) * 0.5f;

                EnsureBox(group, $"Overlook_Dais_Step_{i + 1:00}",
                    new Vector3(OpeningCenterX, stepCenterY, stepCenterZ), Quaternion.identity,
                    new Vector3(OpeningClearWidthX, stepHeight, TreadDepth), mats.FloorConcrete, keepCollider: true);

                // Subtle concrete nosing edge strip along North front lip of each step (non-colliding)
                EnsureBox(group, $"Overlook_Dais_StepNosing_{i + 1:00}",
                    new Vector3(OpeningCenterX, stepTopY - 0.013f, stepMaxZ - 0.016f), Quaternion.identity,
                    new Vector3(OpeningClearWidthX - 0.004f, 0.026f, 0.034f), mats.ColumnConcrete, keepCollider: false);
            }

            // Solid monolithic poured-concrete raised viewing dais platform (X in [-4.460, +0.060], Z in [+27.400, +28.740], top Y = -45.860m)
            float daisHeight = DaisFloorY - SlabBottomY; // 0.740f
            float daisCenterY = SlabBottomY + daisHeight * 0.5f;
            float daisCenterZ = (AlcoveWalkableMinZ + DaisNorthEdgeZ) * 0.5f; // +28.070f

            EnsureBox(group, "Overlook_Dais_PlatformSlab",
                new Vector3(OpeningCenterX, daisCenterY, daisCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX, daisHeight, DaisLevelDepthZ), mats.FloorConcrete, keepCollider: true);

            // Subtle expansion joint & dark-concrete floor edge reveal along dais perimeter (non-colliding)
            EnsureBox(group, "Overlook_Dais_FloorJointSeam",
                new Vector3(OpeningCenterX, DaisFloorY + 0.002f, daisCenterZ), Quaternion.identity,
                new Vector3(0.026f, 0.004f, DaisLevelDepthZ - 0.12f), mats.BaseboardConcrete, keepCollider: false);
        }

        private static void BuildCompactEnclosure(Transform group, OverlookMaterials mats)
        {
            float enclDepthZ = GallerySouthOuterZ - ParapetOuterMinZ; // 2.380f (Z in [+27.200, +29.580])
            float enclCenterZ = (ParapetOuterMinZ + GallerySouthOuterZ) * 0.5f; // +28.390f
            float wallH = CeilingSoffitY - SlabBottomY; // 3.050f
            float wallMidY = SlabBottomY + wallH * 0.5f; // -45.075f

            // Compact West & East alcove containment sidewalls (strictly enclosing Z in [+27.200, +29.580] only!)
            EnsureBox(group, "Overlook_SideWall_West",
                new Vector3(OpeningMinX - SideWallThicknessX * 0.5f, wallMidY, enclCenterZ), Quaternion.identity,
                new Vector3(SideWallThicknessX, wallH, enclDepthZ), mats.WallConcrete, keepCollider: true);
            EnsureBox(group, "Overlook_SideWall_East",
                new Vector3(OpeningMaxX + SideWallThicknessX * 0.5f, wallMidY, enclCenterZ), Quaternion.identity,
                new Vector3(SideWallThicknessX, wallH, enclDepthZ), mats.WallConcrete, keepCollider: true);

            // Subtle darker-concrete vertical framing reveals inside sidewalls near viewing edge (non-colliding)
            float revealH = CeilingSoffitY - DaisFloorY; // 2.310f
            float revealMidY = DaisFloorY + revealH * 0.5f;
            EnsureBox(group, "Overlook_SideFrame_West",
                new Vector3(OpeningMinX + 0.030f, revealMidY, AlcoveWalkableMinZ + 0.070f), Quaternion.identity,
                new Vector3(0.060f, revealH, 0.140f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(group, "Overlook_SideFrame_East",
                new Vector3(OpeningMaxX - 0.030f, revealMidY, AlcoveWalkableMinZ + 0.070f), Quaternion.identity,
                new Vector3(0.060f, revealH, 0.140f), mats.ColumnConcrete, keepCollider: false);

            // Dais-level baseboards along West & East alcove sidewalls (Z in [+27.400, +28.740], non-colliding)
            float daisCenterZ = (AlcoveWalkableMinZ + DaisNorthEdgeZ) * 0.5f;
            EnsureBox(group, "Overlook_SideBaseboard_West",
                new Vector3(OpeningMinX + 0.012f, DaisFloorY + 0.060f, daisCenterZ), Quaternion.identity,
                new Vector3(0.025f, 0.120f, DaisLevelDepthZ), mats.BaseboardConcrete, keepCollider: false);
            EnsureBox(group, "Overlook_SideBaseboard_East",
                new Vector3(OpeningMaxX - 0.012f, DaisFloorY + 0.060f, daisCenterZ), Quaternion.identity,
                new Vector3(0.025f, 0.120f, DaisLevelDepthZ), mats.BaseboardConcrete, keepCollider: false);

            // Compact local ceiling slab continuing NUGallery_CeilingSlab at baseline Y = -43.550m (2.310m clear headroom over dais)
            float ceilWidthX = EnclosureOuterMaxX - EnclosureOuterMinX; // 4.960f
            EnsureBox(group, "Overlook_CeilingSlab",
                new Vector3(OpeningCenterX, CeilingSoffitY + CeilingSlabThickness * 0.5f, enclCenterZ), Quaternion.identity,
                new Vector3(ceilWidthX, CeilingSlabThickness, enclDepthZ), mats.CeilingSoffit, keepCollider: true);

            // Restrained dark-concrete soffit fascia border framing the viewing window overhead (non-colliding)
            EnsureBox(group, "Overlook_Ceiling_ViewingSoffitTrim",
                new Vector3(OpeningCenterX, CeilingSoffitY - 0.030f, ParapetCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX, 0.060f, ParapetThicknessZ), mats.ColumnConcrete, keepCollider: false);
        }

        private static void BuildSafetyBarrier(Transform group, OverlookMaterials mats)
        {
            // 1. Solid poured-concrete viewing parapet (X in [-4.460, +0.060], Z in [+27.200, +27.400], Y in [-46.600, -44.810])
            //    1.050m guard height above raised dais (-45.860m); +0.900m clear North of SkylightCap (Zmax = +26.300m).
            float parapetH = ParapetTopY - SlabBottomY; // 1.790f
            float parapetMidY = SlabBottomY + parapetH * 0.5f;
            EnsureBox(group, "Overlook_Parapet_Concrete",
                new Vector3(OpeningCenterX, parapetMidY, ParapetCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX, parapetH, ParapetThicknessZ), mats.ColumnConcrete, keepCollider: true);

            // Dark-steel coping sill crowning the concrete parapet (Y in [-44.810, -44.770], solid collider)
            EnsureBox(group, "Overlook_Parapet_SteelSill",
                new Vector3(OpeningCenterX, ParapetTopY + SteelSillHeight * 0.5f, ParapetCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX, SteelSillHeight, ParapetThicknessZ + 0.030f), mats.IndustrialMetal, keepCollider: true);

            // 2. Dark-steel perimeter framing & two slim vertical mullions dividing the 4.520m aperture into 3 balanced glass bays
            //    (keeps center bay X in [-2.929, -1.471] completely unobstructed across the primary sightline!)
            float frameSpanH = CeilingSoffitY - GlassBottomY; // 1.220f
            float frameMidY = GlassBottomY + frameSpanH * 0.5f;
            float glassMidY = GlassBottomY + GlassHeight * 0.5f;

            EnsureBox(group, "Overlook_GlassFrame_JambW",
                new Vector3(OpeningMinX + FrameJambWidthX * 0.5f, frameMidY, ParapetCenterZ), Quaternion.identity,
                new Vector3(FrameJambWidthX, frameSpanH, 0.080f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "Overlook_GlassFrame_JambE",
                new Vector3(OpeningMaxX - FrameJambWidthX * 0.5f, frameMidY, ParapetCenterZ), Quaternion.identity,
                new Vector3(FrameJambWidthX, frameSpanH, 0.080f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "Overlook_GlassFrame_Header",
                new Vector3(OpeningCenterX, CeilingSoffitY - TopHeaderHeight * 0.5f, ParapetCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX, TopHeaderHeight, 0.080f), mats.IndustrialMetal, keepCollider: true);

            // Narrow lower glazing channel seated on steel sill (non-colliding visual detail)
            EnsureBox(group, "Overlook_GlassFrame_BottomChannel",
                new Vector3(OpeningCenterX, GlassBottomY + 0.015f, ParapetCenterZ), Quaternion.identity,
                new Vector3(OpeningClearWidthX - 2f * FrameJambWidthX, 0.030f, 0.052f), mats.IndustrialMetal, keepCollider: false);

            // Compute exact 3-bay partition between inner jamb faces
            float innerMinX = OpeningMinX + FrameJambWidthX; // -4.405f
            float innerMaxX = OpeningMaxX - FrameJambWidthX; // +0.005f
            float mullionWCenterX = Mathf.Lerp(innerMinX, innerMaxX, 1f / 3f); // -2.935f
            float mullionECenterX = Mathf.Lerp(innerMinX, innerMaxX, 2f / 3f); // -1.465f

            EnsureBox(group, "Overlook_GlassFrame_Mullion_0",
                new Vector3(mullionWCenterX, glassMidY, ParapetCenterZ), Quaternion.identity,
                new Vector3(MullionWidthX, GlassHeight, 0.068f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(group, "Overlook_GlassFrame_Mullion_1",
                new Vector3(mullionECenterX, glassMidY, ParapetCenterZ), Quaternion.identity,
                new Vector3(MullionWidthX, GlassHeight, 0.068f), mats.IndustrialMetal, keepCollider: true);

            // 3. Three visibly represented, transparent reinforced architectural glass panels — each with its own
            //    directly aligned solid BoxCollider (100% gap-free containment, ZERO unexplained invisible wall!).
            float halfM = MullionWidthX * 0.5f;
            (string name, float minX, float maxX)[] glassBays =
            {
                ("Overlook_SafetyGlass_Panel_0", innerMinX, mullionWCenterX - halfM),
                ("Overlook_SafetyGlass_Panel_1", mullionWCenterX + halfM, mullionECenterX - halfM),
                ("Overlook_SafetyGlass_Panel_2", mullionECenterX + halfM, innerMaxX)
            };

            for (int i = 0; i < glassBays.Length; i++)
            {
                float w = glassBays[i].maxX - glassBays[i].minX;
                float cx = (glassBays[i].minX + glassBays[i].maxX) * 0.5f;
                EnsureBox(group, glassBays[i].name,
                    new Vector3(cx, glassMidY, ParapetCenterZ), Quaternion.identity,
                    new Vector3(w, GlassHeight, GlassThicknessZ), mats.SafetyGlass, keepCollider: true);
            }
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
        /// Read-only Editor verification camera utility for Implementation 07.19.
        /// Captures all 7 required diagnostic first-person views using exact Remanencia Level 0
        /// ambient light, exponential-squared fog, and PlayerCamera FOV (60°).
        /// </summary>
        public static void RenderCourtyardLightwellOverlookVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_OVERLOOK_0719_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_Overlook_0719_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempOverlook0719VerifyCam__");
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

                const float galleryEyeY = GalleryFloorY + 0.040f + 1.600f; // -44.760f
                const float daisEyeY = DaisFloorY + 0.040f + 1.600f;       // -44.220f

                (string fileName, Vector3 pos, Vector3 lookAt)[] shots =
                {
                    // 1. Entering the new opening from the North Upper Gallery
                    ("verify_0719_01_gallery_entering_opening.png",
                        new Vector3(-6.20f, galleryEyeY, 31.30f), new Vector3(-2.00f, -45.00f, 28.40f)),
                    // 2. Approaching the three concrete steps
                    ("verify_0719_02_approaching_three_steps.png",
                        new Vector3(-2.20f, galleryEyeY, 31.10f), new Vector3(-2.20f, -45.55f, 27.80f)),
                    // 3. Standing on the raised viewing dais
                    ("verify_0719_03_standing_on_raised_dais.png",
                        new Vector3(-4.00f, daisEyeY, 28.35f), new Vector3(-0.80f, -44.65f, 26.80f)),
                    // 4. Looking through the transparent barrier toward the Lightwell
                    ("verify_0719_04_through_glass_toward_lightwell.png",
                        new Vector3(-2.20f, daisEyeY, 27.85f), new Vector3(-1.20f, -44.75f, 22.00f)),
                    // 5. Looking toward the Northern and Eastern skylight edges
                    ("verify_0719_05_north_and_east_skylight_edges.png",
                        new Vector3(-3.90f, daisEyeY, 27.78f), new Vector3( 2.80f, -44.72f, 23.00f)),
                    // 6. Looking back through the opening into the gallery
                    ("verify_0719_06_looking_back_into_gallery.png",
                        new Vector3(-2.20f, daisEyeY, 27.85f), new Vector3(-2.20f, -44.90f, 32.80f)),
                    // 7. Returning from the dais to the gallery
                    ("verify_0719_07_returning_from_dais_to_gallery.png",
                        new Vector3(-2.20f, galleryEyeY + 0.36f, 29.05f), new Vector3( 4.00f, galleryEyeY, 31.30f))
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
                    Debug.Log($"[CourtyardLightwellOverlookBuilder] Rendered 07.19 verification view: {fullFile}");
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
