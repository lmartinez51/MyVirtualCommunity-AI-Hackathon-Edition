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
    /// Dedicated, deterministic, idempotent Editor builder for Implementation 07.24:
    /// Twin Elevator Bay Architecture (Static Architectural Differentiation — No Elevator Functionality).
    ///
    /// Approved Upper Elevator Waiting Area configuration:
    ///   - Bay 1 (South, Z = +5.750): Finished-looking, permanently closed hotel elevator facade
    ///     (two static brushed-metal door leaves in Remanencia_Level0_ElevatorDoorMetal, narrow
    ///     central meeting seam, fitted oxidized-steel perimeter frame, flush metal floor sill,
    ///     solid collision backing — zero buttons, zero indicators, zero signage, zero scripts).
    ///   - Bay 2 (North, Z = +7.850): Visibly unfinished, permanently sealed construction facade
    ///     (exposed concrete jambs &amp; header, incomplete recessed steel subframe &amp; anchor plates,
    ///     solid masonry + weathered plaster backing, bolted dark-steel provisional closure plate —
    ///     zero open shaft, zero transparent cavity view, zero buttons, zero scripts).
    ///
    /// Both bays retain strictly equivalent nominal aperture dimensions (1.370m wide x 2.200m tall,
    /// Y in [-46.400, -44.200]), strict Z alignment with lower-level Bay 1 (+5.750) and Bay 2 (+7.850),
    /// solid collision containment, and zero elevator functionality.
    ///
    /// Leaves the entire lower-level Hotel_Elevator_Core (all 35 children), all 5 adjacent lower
    /// fixtures, Backrooms_Ceiling, Backrooms_Floor, all 10 Lightwell objects, 07.15 Staircase,
    /// 07.17 Gallery, 07.19 Overlook, 07.21 East Overhang, 07.23 Corridor/Vestibule/Waiting shell,
    /// Room 111-B, Threshold of Trust, BorderCorridorScene.unity, and the exact 18-light budget
    /// 100% untouched. Reuses existing project materials strictly read-only.
    /// </summary>
    public static class UpperElevatorBayArchitectureBuilder
    {
        public const string GroupName = "UpperElevatorApproach_BayArchitecture";
        public const string Bay1RootName = "UpperElevBay1_FinishedFacade";
        public const string Bay2RootName = "UpperElevBay2_UnfinishedFacade";

        // Exact lower-level bay Z centers & shared architectural aperture envelope
        public const float Bay1CenterZ = UpperElevatorApproachBuilder.Bay1CenterZ;              // +5.750f
        public const float Bay2CenterZ = UpperElevatorApproachBuilder.Bay2CenterZ;              // +7.850f
        public const float ApertureClearWidthZ = UpperElevatorApproachBuilder.PlaceholderClearWidthZ; // 1.370f
        public const float ApertureClearHeightY = UpperElevatorApproachBuilder.PlaceholderClearHeightY; // 2.200f
        public const float FloorY = UpperElevatorApproachBuilder.GalleryFloorY;                 // -46.400f
        public const float HeaderBottomY = UpperElevatorApproachBuilder.PlaceholderHeaderBottomY; // -44.200f

        private struct BayMaterials
        {
            public Material ElevatorDoorMetal;
            public Material IndustrialMetal;
            public Material ColumnConcrete;
            public Material WallConcrete;
            public Material PlasterWall;
            public Material CeilingVoid;
        }

        [MenuItem("My Virtual Community/Remanencia/Migrate 07.24 Twin Elevator Bay Architecture")]
        public static void MigrateUpperElevatorBayArchitectureAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {BackroomsLevel1Builder.ScenePath}.");
            }

            MigrateUpperElevatorBayArchitectureInOpenScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save {BackroomsLevel1Builder.ScenePath} after 07.24 migration.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UpperElevatorBayArchitectureBuilder] Implementation 07.24 Twin Elevator Bay Architecture migrated and saved cleanly.");
        }

        /// <summary>
        /// Strictly scene-specific, idempotent migration entry point for Implementation 07.24.
        /// Rejects any scene other than BackroomsLevel1Scene.unity.
        /// </summary>
        public static void MigrateUpperElevatorBayArchitectureInOpenScene(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.path, BackroomsLevel1Builder.ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"UpperElevatorBayArchitectureBuilder only targets '{BackroomsLevel1Builder.ScenePath}', got '{scene.path}'.");
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
                    $"Expected '{UpperElevatorApproachBuilder.RootName}' before running 07.24 Twin Elevator Bay Architecture migration.");
            }

            BayMaterials mats = LoadExistingMaterials();

            // 1. Preserve UpperElevatorApproach_SealedBayFrames & both solid backing panel GameObjects in-place,
            //    while cleanly removing superseded uniform 07.23 trim strips so zero Z-fighting occurs.
            AdaptLegacyPlaceholdersInPlace(approachRoot);

            // 2. Build/update differentiated static Bay 1 (Finished) & Bay 2 (Unfinished) facades idempotently
            Transform bayArchRoot = EnsureSubgroup(approachRoot, GroupName);
            BuildBay1FinishedFacade(EnsureSubgroup(bayArchRoot, Bay1RootName), mats);
            BuildBay2UnfinishedFacade(EnsureSubgroup(bayArchRoot, Bay2RootName), mats);

            Physics.SyncTransforms();
        }

        public static bool VerifyStructurePresent(Transform architectureShell)
        {
            if (architectureShell == null) return false;
            Transform approachRoot = architectureShell.Find(UpperElevatorApproachBuilder.RootName);
            if (approachRoot == null) return false;
            Transform bayArch = approachRoot.Find(GroupName);
            if (bayArch == null) return false;

            Transform b1 = bayArch.Find(Bay1RootName);
            Transform b2 = bayArch.Find(Bay2RootName);
            return b1 != null &&
                   b1.Find("UpperElevBay1_StaticDoors") != null &&
                   b1.Find("UpperElevBay1_PerimeterFrame") != null &&
                   b2 != null &&
                   b2.Find("UpperElevBay2_ConstructionFrame") != null &&
                   b2.Find("UpperElevBay2_SealedInfill") != null;
        }

        private static BayMaterials LoadExistingMaterials()
        {
            return new BayMaterials
            {
                ElevatorDoorMetal = LoadRequiredMat(BackroomsLevel1Builder.ElevatorDoorMetalMatPath),
                IndustrialMetal = LoadRequiredMat(BackroomsLevel1Builder.IndustrialMetalMatPath),
                ColumnConcrete = LoadRequiredMat(BackroomsLevel1Builder.ColumnConcreteMatPath),
                WallConcrete = LoadRequiredMat(BackroomsLevel1Builder.WallConcreteMatPath),
                PlasterWall = LoadRequiredMat(BackroomsLevel1Builder.PlasterWallMatPath),
                CeilingVoid = LoadRequiredMat(BackroomsLevel1Builder.CeilingVoidMatPath)
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
        /// Preserves UpperElevatorApproach_SealedBayFrames and both UpperElev_SealedBay_1/2_SealedBackingPanel
        /// GameObjects in-place as recessed solid safety backing (X in [+3.346, +3.356], MeshRenderer off to avoid
        /// coplanar bleed behind the new facades, solid BoxCollider ON), while removing superseded uniform 07.23 trim.
        /// </summary>
        private static void AdaptLegacyPlaceholdersInPlace(Transform approachRoot)
        {
            Transform legacyGroup = approachRoot.Find("UpperElevatorApproach_SealedBayFrames");
            if (legacyGroup == null) return;

            float bayMidY = FloorY + ApertureClearHeightY * 0.5f; // -45.300f
            (string name, float centerZ)[] backings =
            {
                ("UpperElev_SealedBay_1_SealedBackingPanel", Bay1CenterZ),
                ("UpperElev_SealedBay_2_SealedBackingPanel", Bay2CenterZ)
            };

            for (int i = 0; i < backings.Length; i++)
            {
                Transform panel = legacyGroup.Find(backings[i].name);
                if (panel == null) continue;
                panel.position = new Vector3(3.351f, bayMidY, backings[i].centerZ);
                panel.rotation = Quaternion.identity;
                panel.localScale = new Vector3(0.010f, ApertureClearHeightY, ApertureClearWidthZ);
                MeshRenderer mr = panel.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                BoxCollider col = panel.GetComponent<BoxCollider>();
                if (col == null) col = panel.gameObject.AddComponent<BoxCollider>();
                col.enabled = true;
                col.isTrigger = false;
            }

            string[] supersededSuffixes = { "FrameJambS", "FrameJambN", "FrameHeader", "FloorSill", "RecessSeamVert" };
            for (int bay = 1; bay <= 2; bay++)
            {
                for (int s = 0; s < supersededSuffixes.Length; s++)
                {
                    Transform oldPart = legacyGroup.Find($"UpperElev_SealedBay_{bay}_{supersededSuffixes[s]}");
                    if (oldPart != null) UnityEngine.Object.DestroyImmediate(oldPart.gameObject);
                }
            }
        }

        private static void BuildBay1FinishedFacade(Transform bay1Root, BayMaterials mats)
        {
            Transform doorsGroup = EnsureSubgroup(bay1Root, "UpperElevBay1_StaticDoors");
            Transform frameGroup = EnsureSubgroup(bay1Root, "UpperElevBay1_PerimeterFrame");

            float halfW = ApertureClearWidthZ * 0.5f;             // 0.685f
            float bayMidY = FloorY + ApertureClearHeightY * 0.5f; // -45.300f

            // Two static closed bi-parting brushed-metal hotel elevator door leaves overlapping 10mm at Z=+5.750
            // (zero gap, solid BoxCollider on each leaf, recessed cleanly inside the X=+3.410 concrete piers)
            EnsureBox(doorsGroup, "UpperElevBay1_DoorLeafS",
                new Vector3(3.378f, bayMidY, Bay1CenterZ - 0.340f), Quaternion.identity,
                new Vector3(0.040f, ApertureClearHeightY, 0.690f), mats.ElevatorDoorMetal, keepCollider: true);
            EnsureBox(doorsGroup, "UpperElevBay1_DoorLeafN",
                new Vector3(3.378f, bayMidY, Bay1CenterZ + 0.340f), Quaternion.identity,
                new Vector3(0.040f, ApertureClearHeightY, 0.690f), mats.ElevatorDoorMetal, keepCollider: true);

            // Narrow crisp dark meeting seam between closed leaves (visual detail only, non-colliding)
            EnsureBox(doorsGroup, "UpperElevBay1_CenterSeam",
                new Vector3(3.400f, bayMidY, Bay1CenterZ), Quaternion.identity,
                new Vector3(0.006f, ApertureClearHeightY - 0.006f, 0.014f), mats.CeilingVoid, keepCollider: false);

            // Restrained modest older-hotel brushed kick-plates along bottom of each door leaf (non-colliding)
            EnsureBox(doorsGroup, "UpperElevBay1_DoorKickPlateS",
                new Vector3(3.400f, FloorY + 0.080f, Bay1CenterZ - 0.345f), Quaternion.identity,
                new Vector3(0.004f, 0.140f, 0.660f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(doorsGroup, "UpperElevBay1_DoorKickPlateN",
                new Vector3(3.400f, FloorY + 0.080f, Bay1CenterZ + 0.345f), Quaternion.identity,
                new Vector3(0.004f, 0.140f, 0.660f), mats.IndustrialMetal, keepCollider: false);

            // Complete fitted oxidized-steel perimeter surround jambs, top header, inner track reveal & flush sill
            EnsureBox(frameGroup, "UpperElevBay1_FrameJambS",
                new Vector3(3.396f, bayMidY + 0.020f, Bay1CenterZ - halfW - 0.032f), Quaternion.identity,
                new Vector3(0.092f, ApertureClearHeightY + 0.040f, 0.064f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(frameGroup, "UpperElevBay1_FrameJambN",
                new Vector3(3.396f, bayMidY + 0.020f, Bay1CenterZ + halfW + 0.032f), Quaternion.identity,
                new Vector3(0.092f, ApertureClearHeightY + 0.040f, 0.064f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(frameGroup, "UpperElevBay1_FrameHeader",
                new Vector3(3.396f, HeaderBottomY + 0.040f, Bay1CenterZ), Quaternion.identity,
                new Vector3(0.092f, 0.080f, ApertureClearWidthZ + 0.128f), mats.IndustrialMetal, keepCollider: true);
            EnsureBox(frameGroup, "UpperElevBay1_HeaderTrackReveal",
                new Vector3(3.398f, HeaderBottomY - 0.012f, Bay1CenterZ), Quaternion.identity,
                new Vector3(0.014f, 0.024f, ApertureClearWidthZ - 0.006f), mats.ElevatorDoorMetal, keepCollider: false);
            EnsureBox(frameGroup, "UpperElevBay1_FloorSill",
                new Vector3(3.403f, FloorY + 0.008f, Bay1CenterZ), Quaternion.identity,
                new Vector3(0.106f, 0.016f, ApertureClearWidthZ + 0.110f), mats.IndustrialMetal, keepCollider: false);
        }

        private static void BuildBay2UnfinishedFacade(Transform bay2Root, BayMaterials mats)
        {
            Transform frameGroup = EnsureSubgroup(bay2Root, "UpperElevBay2_ConstructionFrame");
            Transform infillGroup = EnsureSubgroup(bay2Root, "UpperElevBay2_SealedInfill");

            float halfW = ApertureClearWidthZ * 0.5f;             // 0.685f
            float bayMidY = FloorY + ApertureClearHeightY * 0.5f; // -45.300f

            // Exposed cast-concrete perimeter jambs & header (distinct from Bay 1's fitted dark-steel surround)
            EnsureBox(frameGroup, "UpperElevBay2_ConcreteJambS",
                new Vector3(3.392f, bayMidY + 0.020f, Bay2CenterZ - halfW - 0.032f), Quaternion.identity,
                new Vector3(0.084f, ApertureClearHeightY + 0.040f, 0.064f), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(frameGroup, "UpperElevBay2_ConcreteJambN",
                new Vector3(3.392f, bayMidY + 0.020f, Bay2CenterZ + halfW + 0.032f), Quaternion.identity,
                new Vector3(0.084f, ApertureClearHeightY + 0.040f, 0.064f), mats.ColumnConcrete, keepCollider: true);
            EnsureBox(frameGroup, "UpperElevBay2_ConcreteHeader",
                new Vector3(3.392f, HeaderBottomY + 0.040f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.084f, 0.080f, ApertureClearWidthZ + 0.128f), mats.ColumnConcrete, keepCollider: true);

            // Incomplete recessed dark-steel subframe angles + partial upper lintel channel (interrupted installation)
            EnsureBox(frameGroup, "UpperElevBay2_SubframeRailS",
                new Vector3(3.377f, bayMidY, Bay2CenterZ - halfW + 0.018f), Quaternion.identity,
                new Vector3(0.054f, ApertureClearHeightY - 0.020f, 0.036f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(frameGroup, "UpperElevBay2_SubframeRailN",
                new Vector3(3.377f, bayMidY - 0.140f, Bay2CenterZ + halfW - 0.018f), Quaternion.identity,
                new Vector3(0.054f, ApertureClearHeightY - 0.300f, 0.036f), mats.IndustrialMetal, keepCollider: false);
            EnsureBox(frameGroup, "UpperElevBay2_SubframePartialTop",
                new Vector3(3.377f, HeaderBottomY - 0.022f, Bay2CenterZ - 0.180f), Quaternion.identity,
                new Vector3(0.054f, 0.044f, 0.960f), mats.IndustrialMetal, keepCollider: false);

            // Four flush steel fixing/anchor plates mounted along the exposed concrete jambs (zero snagging)
            float[] plateYs = { FloorY + 0.550f, FloorY + 1.720f };
            for (int i = 0; i < plateYs.Length; i++)
            {
                EnsureBox(frameGroup, $"UpperElevBay2_AnchorPlateS_{i}",
                    new Vector3(3.436f, plateYs[i], Bay2CenterZ - halfW - 0.032f), Quaternion.identity,
                    new Vector3(0.006f, 0.140f, 0.060f), mats.IndustrialMetal, keepCollider: false);
                EnsureBox(frameGroup, $"UpperElevBay2_AnchorPlateN_{i}",
                    new Vector3(3.436f, plateYs[i], Bay2CenterZ + halfW + 0.032f), Quaternion.identity,
                    new Vector3(0.006f, 0.140f, 0.060f), mats.IndustrialMetal, keepCollider: false);
            }

            // Raw poured-concrete threshold sill (contrasts with Bay 1's metal elevator sill)
            EnsureBox(frameGroup, "UpperElevBay2_RawConcreteSill",
                new Vector3(3.398f, FloorY + 0.007f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.096f, 0.014f, ApertureClearWidthZ + 0.100f), mats.ColumnConcrete, keepCollider: false);

            // 100% solid opaque unfinished masonry infill spanning full 1.370m x 2.200m aperture (ZERO shaft cavity!)
            EnsureBox(infillGroup, "UpperElevBay2_SolidMasonryInfill",
                new Vector3(3.368f, bayMidY, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.032f, ApertureClearHeightY, ApertureClearWidthZ), mats.WallConcrete, keepCollider: true);

            // Upper unfinished weathered-plaster infill course contrasting with raw concrete lower zone
            EnsureBox(infillGroup, "UpperElevBay2_WeatheredPlasterCourse",
                new Vector3(3.386f, FloorY + 1.750f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.006f, 0.880f, ApertureClearWidthZ - 0.072f), mats.PlasterWall, keepCollider: false);

            // Provisional dark-steel construction closure panel sealed over lower/mid bay (solid collider, non-interactive)
            EnsureBox(infillGroup, "UpperElevBay2_ClosurePlate",
                new Vector3(3.391f, FloorY + 0.670f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.018f, 1.180f, ApertureClearWidthZ - 0.090f), mats.IndustrialMetal, keepCollider: true);

            // Two slim horizontal stiffener battens & 4 corner fixing gussets on the closure panel (non-colliding, flush)
            EnsureBox(infillGroup, "UpperElevBay2_ClosureBatten_Bot",
                new Vector3(3.403f, FloorY + 0.280f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.010f, 0.055f, ApertureClearWidthZ - 0.120f), mats.ColumnConcrete, keepCollider: false);
            EnsureBox(infillGroup, "UpperElevBay2_ClosureBatten_Top",
                new Vector3(3.403f, FloorY + 1.060f, Bay2CenterZ), Quaternion.identity,
                new Vector3(0.010f, 0.055f, ApertureClearWidthZ - 0.120f), mats.ColumnConcrete, keepCollider: false);
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

        /// <summary>
        /// Read-only Editor diagnostic camera utility for Implementation 07.24.
        /// Captures all 8 required first-person views of Bay 1 (finished) and Bay 2 (unfinished)
        /// using exact Remanencia Level 0 flat ambient light, exponential-squared fog, SolidColor clear, and 60° FOV.
        /// </summary>
        public static void RenderUpperElevatorBayVerificationViews()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Could not open {BackroomsLevel1Builder.ScenePath}.");
            }

            string outDir = Environment.GetEnvironmentVariable("MVC_ELEV_BAY_0724_RENDER_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetTempPath(), "MVC_ElevBay_0724_Renders");
            }
            Directory.CreateDirectory(outDir);

            AmbientMode prevAmbMode = RenderSettings.ambientMode;
            Color prevAmbLight = RenderSettings.ambientLight;
            bool prevFog = RenderSettings.fog;
            FogMode prevFogMode = RenderSettings.fogMode;
            Color prevFogColor = RenderSettings.fogColor;
            float prevFogDensity = RenderSettings.fogDensity;
            Material prevSkybox = RenderSettings.skybox;

            GameObject camGo = new GameObject("__TempElevBay0724VerifyCam__");
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
                    // 1. Entering the Upper Elevator Waiting Area from the Transition Vestibule
                    ("verify_0724_01_entering_waiting_from_vestibule.png",
                        new Vector3(5.30f, eyeY, 10.40f), new Vector3(4.10f, eyeY - 0.12f, 7.20f)),
                    // 2. First view of Bay 2's unfinished construction facade
                    ("verify_0724_02_first_view_bay2_unfinished.png",
                        new Vector3(5.55f, eyeY, 8.70f), new Vector3(3.38f, eyeY - 0.25f, 7.85f)),
                    // 3. Close view of Bay 2's exposed concrete frame, incomplete subframe, anchor plates & bolted closure infill
                    ("verify_0724_03_close_view_bay2_infill.png",
                        new Vector3(4.85f, eyeY - 0.10f, 7.95f), new Vector3(3.38f, eyeY - 0.45f, 7.85f)),
                    // 4. View from Bay 2 looking South toward Bay 1's finished metal doors
                    ("verify_0724_04_view_from_bay2_toward_bay1.png",
                        new Vector3(5.35f, eyeY, 8.15f), new Vector3(3.55f, eyeY - 0.22f, 5.55f)),
                    // 5. Full frontal view of Bay 1's finished-looking static hotel elevator doors
                    ("verify_0724_05_frontal_view_bay1_finished.png",
                        new Vector3(5.55f, eyeY, 5.75f), new Vector3(3.38f, eyeY - 0.30f, 5.75f)),
                    // 6. Side-by-side comparison showing both Bay 1 (left) and Bay 2 (right) together
                    ("verify_0724_06_comparison_both_bays_together.png",
                        new Vector3(6.00f, eyeY, 6.80f), new Vector3(3.38f, eyeY - 0.25f, 6.80f)),
                    // 7. Close angled view of shared alignment, floor sills & consistent proportions
                    ("verify_0724_07_shared_alignment_and_proportions.png",
                        new Vector3(5.70f, eyeY - 0.15f, 5.10f), new Vector3(3.38f, eyeY - 0.45f, 7.25f)),
                    // 8. Looking back North from the elevator bay area toward the Transition Vestibule
                    ("verify_0724_08_looking_back_toward_vestibule.png",
                        new Vector3(4.95f, eyeY, 5.10f), new Vector3(4.90f, eyeY - 0.08f, 10.60f))
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
                Debug.Log($"[UpperElevatorBayArchitectureBuilder] Rendered {shots.Length} diagnostic verification views to '{outDir}'.");
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
