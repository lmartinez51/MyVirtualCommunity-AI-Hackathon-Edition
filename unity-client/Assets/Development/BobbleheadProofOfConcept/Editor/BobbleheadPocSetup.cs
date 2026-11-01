using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Development.Bobblehead
{
    /// <summary>
    /// Editor automation tool for Increment 5.8.3: Bobblehead NPC Isolated Proof of Concept.
    /// Builds an isolated test scene with:
    /// - Station A: Production Kiosk dimensions with Elena on standard 0.08m mat.
    /// - Station B: Production Kiosk dimensions with Elena on 0.25m ergonomic attendant riser.
    /// - Station C: Open Full-Body Pedestal for 360-degree anatomy and proportion evaluation.
    /// Generates the standalone ElenaBobblehead_Poc.prefab and captures 12 evaluation screenshots.
    /// </summary>
    public static class BobbleheadPocSetup
    {
        private const string SceneDirectory = "Assets/Development/BobbleheadProofOfConcept/Scenes";
        private const string ScenePath = "Assets/Development/BobbleheadProofOfConcept/Scenes/BobbleheadElenaPocScene.unity";
        private const string PrefabDirectory = "Assets/Development/BobbleheadProofOfConcept/Prefabs";
        private const string PrefabPath = "Assets/Prefabs/Characters/NPCs/ElenaRamos_Bobblehead.prefab";
        private const string ScreenshotDir = "Assets/Development/BobbleheadProofOfConcept/Screenshots";

        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        [MenuItem("MyVirtualCommunity/POC/1. Build Bobblehead POC Scene")]
        public static void BuildPocScene()
        {
            Debug.Log("[BobbleheadPocSetup] Building Isolated Bobblehead Proof-of-Concept Scene...");

            EnsureDirectories();

            // Create new single empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Environment Lighting & Sun
            SetupEnvironmentLighting();

            // 2. Concrete Sidewalk & Plaza Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Poc_Ground_Plaza";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3.0f, 1.0f, 2.0f); // 30m x 20m
            Material groundMat = LoadOrCreateMat("Assets/Settings/ConcreteMaterial.mat", new Color(0.72f, 0.72f, 0.74f), 0.2f);
            SetMat(ground, groundMat);

            // 3. Station A: Production Kiosk (Elena on standard 0.08m mat)
            GameObject stationA = BuildKioskStation("StationA_StandardKiosk", new Vector3(0f, 0f, 0f), 0.08f, out BobbleheadCharacter elenaA, out BobbleheadAnimator animA);

            // 4. Station B: Production Kiosk with 0.25m Ergonomic Riser
            GameObject stationB = BuildKioskStation("StationB_ElevatedKiosk", new Vector3(4.5f, 0f, 0f), 0.25f, out BobbleheadCharacter elenaB, out BobbleheadAnimator animB);

            // 5. Station C: Open Full-Body Pedestal (No counter obstruction)
            GameObject stationC = BuildOpenPedestalStation("StationC_OpenPedestal", new Vector3(-4.5f, 0f, 0f), out BobbleheadCharacter elenaC, out BobbleheadAnimator animC);

            // 6. Save Prefab from Station C instance
            SaveBobbleheadPrefab(elenaC.gameObject);

            // 7. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BobbleheadPocSetup] Successfully built and saved POC scene to: {ScenePath}");
        }

        [MenuItem("MyVirtualCommunity/POC/2. Capture All POC Screenshots")]
        public static void CapturePocScreenshots()
        {
            Debug.Log("[BobbleheadPocSetup] Starting POC Screenshot Capture Suite...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BobbleheadPocSetup] Failed to open scene at {ScenePath}. Run BuildPocScene first.");
                return;
            }

            EnsureDirectories();
            string absScreenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScreenshotDir));
            if (!Directory.Exists(absScreenshotDir)) Directory.CreateDirectory(absScreenshotDir);

            // Setup dedicated capture camera
            GameObject camGo = new GameObject("Poc_Capture_Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;

            // Trigger animation poses so limbs rest in attendant posture
            var animators = Object.FindObjectsByType<BobbleheadAnimator>(FindObjectsSortMode.None);
            foreach (var a in animators)
            {
                a.SetAttendantMode(true);
                a.CacheInitialPoses();
                a.UpdateLocomotionState(0f, true);
            }

            try
            {
                // Station A Target: Vector3(0f, 0.08f, 0.35f)
                Vector3 targetA = new Vector3(0f, 0.95f, 0.35f); // Elena's chest/head center
                Vector3 targetB = new Vector3(4.5f, 1.10f, 0.35f); // Station B target
                Vector3 targetC = new Vector3(-4.5f, 0.85f, 0f); // Station C target

                // 1. poc_01_front.png: Straight Front View at Station A
                CaptureView(cam, targetA, 0f, 10f, 2.2f, Path.Combine(absScreenshotDir, "poc_01_front.png"));

                // 2. poc_02_front_left.png: 45° Front-Left View at Station A
                CaptureView(cam, targetA, 45f, 12f, 2.3f, Path.Combine(absScreenshotDir, "poc_02_front_left.png"));

                // 3. poc_03_left.png: 90° Profile Left View at Station A
                CaptureView(cam, targetA, 90f, 8f, 2.4f, Path.Combine(absScreenshotDir, "poc_03_left.png"));

                // 4. poc_04_rear.png: 180° Rear View at Station A (Hair cap, ponytail plume, hair tie)
                CaptureView(cam, targetA, 180f, 10f, 2.2f, Path.Combine(absScreenshotDir, "poc_04_rear.png"));

                // 5. poc_05_right.png: 270° Profile Right View at Station A
                CaptureView(cam, targetA, 270f, 8f, 2.4f, Path.Combine(absScreenshotDir, "poc_05_right.png"));

                // 6. poc_06_front_right.png: 315° Front-Right View at Station A
                CaptureView(cam, targetA, 315f, 12f, 2.3f, Path.Combine(absScreenshotDir, "poc_06_front_right.png"));

                // 7. poc_07_elevated.png: 30° Elevated Angle Looking into Kiosk
                CaptureView(cam, targetA, 15f, 32f, 2.8f, Path.Combine(absScreenshotDir, "poc_07_elevated.png"));

                // 8. poc_08_low_angle.png: Low Angle Looking Up at Elena & Canopy
                CaptureView(cam, targetA, 0f, -8f, 2.4f, Path.Combine(absScreenshotDir, "poc_08_low_angle.png"));

                // 9. poc_09_closeup_face_badge.png: Close-up of Elena's Face, Ponytail & Nameplate (Station C Full-Body Pedestal)
                CaptureView(cam, new Vector3(-4.5f, 1.35f, 0f), 15f, 5f, 1.15f, Path.Combine(absScreenshotDir, "poc_09_closeup_face_badge.png"));

                // 9b. poc_09b_kiosk_closeup_stationB.png: Close-up of Elena behind Counter with Ergonomic Riser
                CaptureView(cam, new Vector3(4.5f, 1.25f, 0.35f), 10f, 8f, 1.4f, Path.Combine(absScreenshotDir, "poc_09b_kiosk_closeup_stationB.png"));

                // 10. poc_10_fullbody_anatomy.png: Full Body View at Station C (Head, Torso, Limbs, Feet)
                CaptureView(cam, targetC, 20f, 8f, 2.8f, Path.Combine(absScreenshotDir, "poc_10_fullbody_anatomy.png"));

                // 11. poc_11_counter_comparison.png: Dual Comparison View (Station A vs Station B)
                // Place camera between Station A (X=0) and Station B (X=4.5) to capture both kiosks
                cam.fieldOfView = 55f;
                cam.transform.position = new Vector3(2.25f, 1.35f, -3.8f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                SaveCameraFrame(cam, Path.Combine(absScreenshotDir, "poc_11_counter_comparison.png"));

                // 12. poc_12_gameplay_approach.png: First-Person Gameplay Perspective at 3.5m Approach
                cam.fieldOfView = 60f;
                cam.transform.position = new Vector3(0f, 1.65f, -3.5f); // 1.65m eye level, 3.5m distance
                cam.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
                SaveCameraFrame(cam, Path.Combine(absScreenshotDir, "poc_12_gameplay_approach.png"));

                Debug.Log($"[BobbleheadPocSetup] All 12 POC screenshots generated successfully in: {absScreenshotDir}");
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        private static void CaptureView(Camera cam, Vector3 target, float azimuthDeg, float elevationDeg, float distance, string outputPath)
        {
            cam.fieldOfView = 48f;
            float azRad = azimuthDeg * Mathf.Deg2Rad;
            float elRad = elevationDeg * Mathf.Deg2Rad;

            // Compute camera position relative to target
            // When azimuth = 0, camera is at -Z (facing North toward Elena who faces South)
            float x = target.x + distance * Mathf.Cos(elRad) * Mathf.Sin(azRad);
            float y = target.y + distance * Mathf.Sin(elRad);
            float z = target.z - distance * Mathf.Cos(elRad) * Mathf.Cos(azRad);

            cam.transform.position = new Vector3(x, y, z);
            cam.transform.LookAt(target);

            SaveCameraFrame(cam, outputPath);
        }

        private static void SaveCameraFrame(Camera cam, string outputPath)
        {
            RenderTexture rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] pngData = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(outputPath, pngData);
            Debug.Log($"[BobbleheadPocSetup] Saved: {Path.GetFileName(outputPath)}");
        }

        private static GameObject BuildKioskStation(string rootName, Vector3 worldPos, float attendantFloorHeight, out BobbleheadCharacter character, out BobbleheadAnimator animator)
        {
            GameObject stationGo = new GameObject(rootName);
            stationGo.transform.position = worldPos;

            // Materials
            Material kioskBlueMat = LoadOrCreateMat("Assets/Settings/KioskBlue.mat", new Color(0.12f, 0.35f, 0.75f), 0.3f);
            Material countertopMat = LoadOrCreateMat("Assets/Settings/CountertopMat.mat", new Color(0.94f, 0.95f, 0.96f), 0.5f);
            Material turquoiseMat = LoadOrCreateMat("Assets/Settings/BridgeTurquoise.mat", new Color(0.15f, 0.55f, 0.60f), 0.3f);
            Material attendantMat = LoadOrCreateMat("Assets/Settings/AttendantMat.mat", new Color(0.15f, 0.15f, 0.17f), 0.1f);
            Material steelMat = LoadOrCreateMat("Assets/Settings/BridgeSteel.mat", new Color(0.35f, 0.38f, 0.42f), 0.6f);

            // Kiosk Desk Base
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Kiosk_Desk";
            desk.transform.SetParent(stationGo.transform, false);
            desk.transform.localPosition = new Vector3(0f, 0.50f, -0.25f);
            desk.transform.localScale = new Vector3(2.4f, 1.00f, 0.6f);
            SetMat(desk, kioskBlueMat);

            // Kiosk Countertop Surface (Top at Y = 1.05m)
            GameObject countertop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            countertop.name = "Kiosk_Countertop";
            countertop.transform.SetParent(stationGo.transform, false);
            countertop.transform.localPosition = new Vector3(0f, 1.025f, -0.25f);
            countertop.transform.localScale = new Vector3(2.55f, 0.05f, 0.75f);
            SetMat(countertop, countertopMat);

            // Attendant Platform / Mat
            GameObject matGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matGo.name = "Attendant_Platform";
            matGo.transform.SetParent(stationGo.transform, false);
            matGo.transform.localPosition = new Vector3(0f, attendantFloorHeight / 2f, 0.35f);
            matGo.transform.localScale = new Vector3(1.4f, attendantFloorHeight, 0.9f);
            SetMat(matGo, attendantMat);

            // Kiosk Support Posts
            for (int i = 0; i < 2; i++)
            {
                float sign = (i == 0) ? -1f : 1f;
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Kiosk_Post_{(i == 0 ? "L" : "R")}";
                post.transform.SetParent(stationGo.transform, false);
                post.transform.localPosition = new Vector3(sign * 1.15f, 1.35f, 0.9f);
                post.transform.localScale = new Vector3(0.08f, 1.35f, 0.08f);
                SetMat(post, steelMat);
            }

            // Kiosk Canopy Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Kiosk_Roof";
            roof.transform.SetParent(stationGo.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.7f, 0.2f);
            roof.transform.localScale = new Vector3(3.0f, 0.15f, 2.4f);
            SetMat(roof, turquoiseMat);

            // Fascia Header Sign (Quad facing oncoming pedestrians unmirrored)
            GameObject fascia = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fascia.name = "Kiosk_FasciaSign";
            fascia.transform.SetParent(stationGo.transform, false);
            fascia.transform.localPosition = new Vector3(0f, 2.70f, -1.02f);
            fascia.transform.localRotation = Quaternion.identity;
            fascia.transform.localScale = new Vector3(2.76f, 0.36f, 1.0f);
            Material signMat = LoadOrCreateMat("Assets/Materials/Characters/Kiosk_Sign.mat", new Color(0.10f, 0.18f, 0.32f), 0.2f);
            SetMat(fascia, signMat);
            Object.DestroyImmediate(fascia.GetComponent<Collider>());

            // Canopy Interior Warm Spotlight (Soft warm illumination on Elena and counter)
            GameObject spotGo = new GameObject("Kiosk_Interior_Spotlight");
            spotGo.transform.SetParent(stationGo.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 2.45f, 0.0f);
            spotGo.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            Light spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.94f, 0.85f);
            spot.intensity = 15f;
            spot.range = 5.0f;
            spot.spotAngle = 80f;

            // Brochure Holder on Countertop
            GameObject brochure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brochure.name = "Counter_BrochureHolder";
            brochure.transform.SetParent(stationGo.transform, false);
            brochure.transform.localPosition = new Vector3(-0.75f, 1.10f, -0.22f);
            brochure.transform.localScale = new Vector3(0.25f, 0.15f, 0.18f);
            SetMat(brochure, LoadOrCreateMat("Assets/Settings/BrochureMat.mat", new Color(0.85f, 0.85f, 0.90f), 0.2f));

            // Elena Bobblehead Character
            GameObject elenaGo = new GameObject("ElenaRamos_Bobblehead");
            elenaGo.transform.SetParent(stationGo.transform, false);
            elenaGo.transform.localPosition = new Vector3(0f, attendantFloorHeight, 0.35f);
            elenaGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Faces South towards counter & player

            character = elenaGo.AddComponent<BobbleheadCharacter>();
            var config = BobbleheadConfig.CreateElenaRamosConfig();
            character.Initialize(config);

            animator = elenaGo.AddComponent<BobbleheadAnimator>();
            animator.SetAttendantMode(true);
            animator.CacheInitialPoses();

            return stationGo;
        }

        private static GameObject BuildOpenPedestalStation(string rootName, Vector3 worldPos, out BobbleheadCharacter character, out BobbleheadAnimator animator)
        {
            GameObject stationGo = new GameObject(rootName);
            stationGo.transform.position = worldPos;

            // Circular Inspection Pedestal
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Inspection_Pedestal";
            pedestal.transform.SetParent(stationGo.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            pedestal.transform.localScale = new Vector3(1.8f, 0.04f, 1.8f);
            Material ringMat = LoadOrCreateMat("Assets/Settings/HighlightRing.mat", new Color(0.15f, 0.55f, 0.60f), 0.4f);
            SetMat(pedestal, ringMat);

            // Overhead Label Billboard
            GameObject labelGo = new GameObject("Pedestal_Label");
            labelGo.transform.SetParent(stationGo.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "Elena Ramos\nBorder Concierge";
            tm.fontSize = 24;
            tm.characterSize = 0.018f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.10f, 0.18f, 0.32f);

            // Elena Bobblehead Character
            GameObject elenaGo = new GameObject("ElenaRamos_FullBodyDisplay");
            elenaGo.transform.SetParent(stationGo.transform, false);
            elenaGo.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            elenaGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            character = elenaGo.AddComponent<BobbleheadCharacter>();
            var config = BobbleheadConfig.CreateElenaRamosConfig();
            character.Initialize(config);

            animator = elenaGo.AddComponent<BobbleheadAnimator>();
            animator.SetAttendantMode(false); // Relaxed full-body stance
            animator.CacheInitialPoses();

            return stationGo;
        }

        private static void SaveBobbleheadPrefab(GameObject sourceCharacter)
        {
            // Create a clean standalone prefab root
            GameObject prefabRoot = Object.Instantiate(sourceCharacter);
            prefabRoot.name = "ElenaBobblehead_Poc";
            prefabRoot.transform.position = Vector3.zero;
            prefabRoot.transform.rotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Object.DestroyImmediate(prefabRoot);
            Debug.Log($"[BobbleheadPocSetup] Saved clean Elena Bobblehead prefab to: {PrefabPath}");
        }

        private static void SetupEnvironmentLighting()
        {
            // Create Directional Light (Sun)
            GameObject sunGo = new GameObject("POC_Directional_Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.97f, 0.92f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            // Warm subtle fill light from opposite angle
            GameObject fillGo = new GameObject("POC_Fill_Light");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.80f, 0.88f, 1.0f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(30f, 145f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
        }

        private static Material LoadOrCreateMat(string path, Color fallbackColor, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(s);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallbackColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallbackColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Development"))
                AssetDatabase.CreateFolder("Assets", "Development");

            if (!AssetDatabase.IsValidFolder("Assets/Development/BobbleheadProofOfConcept"))
                AssetDatabase.CreateFolder("Assets/Development", "BobbleheadProofOfConcept");

            if (!AssetDatabase.IsValidFolder("Assets/Development/BobbleheadProofOfConcept/Scenes"))
                AssetDatabase.CreateFolder("Assets/Development/BobbleheadProofOfConcept", "Scenes");

            if (!AssetDatabase.IsValidFolder("Assets/Development/BobbleheadProofOfConcept/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Development/BobbleheadProofOfConcept", "Prefabs");

            if (!AssetDatabase.IsValidFolder("Assets/Development/BobbleheadProofOfConcept/Screenshots"))
                AssetDatabase.CreateFolder("Assets/Development/BobbleheadProofOfConcept", "Screenshots");
        }
    }
}

