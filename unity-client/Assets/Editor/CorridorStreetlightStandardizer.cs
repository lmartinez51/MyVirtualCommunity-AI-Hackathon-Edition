using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Standardizes all corridor streetlights along Avenida Benito Juárez (3 on East sidewalk,
    /// 3 mirrored on West sidewalk) using the slender metallic luminaire design from in front of
    /// Farmacias Benavides / Carlos Méndez, and verifies day/night schedule responsiveness.
    /// </summary>
    public static class CorridorStreetlightStandardizer
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public struct StreetlightPlacement
        {
            public string Name;
            public Vector3 WorldPosition;

            public StreetlightPlacement(string name, Vector3 pos)
            {
                Name = name;
                WorldPosition = pos;
            }
        }

        // Canonical 6 symmetric streetlights (3 East at X = +7.35f, 3 mirrored West at X = -7.35f)
        public static readonly StreetlightPlacement[] CanonicalPlacements = new StreetlightPlacement[]
        {
            // East Sidewalk (Sidewalk_East, Y = 0.20f)
            new StreetlightPlacement("Streetlight_East_SouthCorner",     new Vector3( 7.35f, 0.20f, -20.50f)),
            new StreetlightPlacement("Streetlight_East_BenavidesCenter", new Vector3( 7.35f, 0.20f,  -2.50f)),
            new StreetlightPlacement("Streetlight_East_NorthCorner",     new Vector3( 7.35f, 0.20f,  20.50f)),

            // West Sidewalk (Sidewalk_West widened, Y = 0.20f)
            new StreetlightPlacement("Streetlight_West_SouthCorner",     new Vector3(-3.00f, 0.20f, -20.50f)),
            new StreetlightPlacement("Streetlight_West_Center",          new Vector3(-3.00f, 0.20f,  -2.50f)),
            new StreetlightPlacement("Streetlight_West_NorthCorner",     new Vector3(-3.00f, 0.20f,  19.50f))
        };

        [MenuItem("MyVirtualCommunity/Standardize Corridor Streetlights")]
        public static void StandardizeAndVerify()
        {
            BuildStandardizedStreetlights();
            CaptureDayNightVerificationScreenshots();
        }

        public static void BuildStandardizedStreetlights()
        {
            Debug.Log("[CorridorStreetlightStandardizer] Standardizing 6 symmetric corridor streetlights...");

            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CorridorStreetlightStandardizer] Failed to open scene {ScenePath}");
                return;
            }

            // 1. Setup Standardized Luminaire Materials
            Material poleMat = CreateOrGetMaterial(
                "Assets/Settings/Streetlight_PoleAluminum.mat",
                new Color(0.60f, 0.63f, 0.66f, 1.0f),
                smoothness: 0.62f,
                metallic: 0.45f,
                enableEmission: false);

            Material capMat = CreateOrGetMaterial(
                "Assets/Settings/Streetlight_CapGrey.mat",
                new Color(0.32f, 0.35f, 0.38f, 1.0f),
                smoothness: 0.35f,
                metallic: 0.30f,
                enableEmission: false);

            Material bowlMat = CreateOrGetMaterial(
                "Assets/Settings/Streetlight_LuminaireBowl.mat",
                new Color(0.92f, 0.93f, 0.95f, 1.0f),
                smoothness: 0.55f,
                metallic: 0.05f,
                enableEmission: true);

            // 2. Locate or create Street_Lighting root container
            GameObject lightsRoot = GameObject.Find("Street_Lighting");
            if (lightsRoot == null)
            {
                lightsRoot = new GameObject("Street_Lighting");
                lightsRoot.transform.position = Vector3.zero;
            }

            // 3. Remove legacy streetlights & any prior instances anywhere in the scene
            string[] legacyAndCanonicalNames = new string[]
            {
                "Streetlight_E1",
                "Streetlight_E2",
                "Streetlight_W1",
                "Streetlight_W2",
                "Streetlight_East_SouthCorner",
                "Streetlight_East_BenavidesCenter",
                "Streetlight_East_NorthCorner",
                "Streetlight_West_SouthCorner",
                "Streetlight_West_Center",
                "Streetlight_West_NorthCorner"
            };

            foreach (string lampName in legacyAndCanonicalNames)
            {
                GameObject existing = GameObject.Find(lampName);
                while (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                    existing = GameObject.Find(lampName);
                }
            }

            // Also clear any leftover children directly under Street_Lighting
            for (int i = lightsRoot.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(lightsRoot.transform.GetChild(i).gameObject);
            }

            // 4. Construct all 6 standardized streetlights
            foreach (var placement in CanonicalPlacements)
            {
                CreateStandardStreetlamp(
                    placement.Name,
                    lightsRoot.transform,
                    placement.WorldPosition,
                    poleMat,
                    capMat,
                    bowlMat);
            }

            // 5. Ensure TimeOfDayEnvironmentController is present & synced with scene references
            TimeOfDayEnvironmentController todController = UnityEngine.Object.FindAnyObjectByType<TimeOfDayEnvironmentController>();
            if (todController == null)
            {
                GameObject envGo = GameObject.Find("TimeOfDayEnvironmentController") ?? new GameObject("TimeOfDayEnvironmentController");
                todController = envGo.GetComponent<TimeOfDayEnvironmentController>() ?? envGo.AddComponent<TimeOfDayEnvironmentController>();
            }
            todController.DiscoverSceneReferences();
            EditorUtility.SetDirty(todController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CorridorStreetlightStandardizer] 6 symmetric corridor streetlights saved to '{ScenePath}'!");
        }

        public static GameObject CreateStandardStreetlamp(
            string name,
            Transform parent,
            Vector3 worldPos,
            Material poleMat,
            Material capMat,
            Material bowlMat)
        {
            GameObject lampRoot = new GameObject(name);
            lampRoot.transform.SetParent(parent);
            lampRoot.transform.position = worldPos;
            lampRoot.transform.rotation = Quaternion.identity;

            // 1. Cylindrical Pedestal Base (matching Streetlight_East_BenavidesCenter)
            GameObject basePed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePed.name = "Pole_Base";
            basePed.transform.SetParent(lampRoot.transform);
            basePed.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basePed.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
            SetMaterial(basePed, poleMat);
            UnityEngine.Object.DestroyImmediate(basePed.GetComponent<Collider>());

            // 2. Slender Metallic Shaft (matching Streetlight_East_BenavidesCenter)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Pole_Shaft";
            shaft.transform.SetParent(lampRoot.transform);
            shaft.transform.localPosition = new Vector3(0f, 3.30f, 0f);
            shaft.transform.localScale = new Vector3(0.13f, 3.30f, 0.13f);
            SetMaterial(shaft, poleMat);
            // Keep a slender capsule collider on the pole shaft for physical grounding

            // 3. Conical/Bowl Luminaire Diffuser Head (emissive at night, matte white by day)
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Luminaire_Bowl";
            bowl.transform.SetParent(lampRoot.transform);
            bowl.transform.localPosition = new Vector3(0f, 6.65f, 0f);
            bowl.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
            SetMaterial(bowl, bowlMat);
            UnityEngine.Object.DestroyImmediate(bowl.GetComponent<Collider>());

            // 4. Spherical Top Cap
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Luminaire_Cap";
            cap.transform.SetParent(lampRoot.transform);
            cap.transform.localPosition = new Vector3(0f, 6.76f, 0f);
            cap.transform.localScale = new Vector3(0.68f, 0.22f, 0.68f);
            SetMaterial(cap, capMat);
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());

            // 5. Real Omnidirectional Point Light (illuminates luminaire head, upper facades, and surroundings)
            // Default disabled in daytime scene state; enabled & scaled by TimeOfDayEnvironmentController after 6 PM (18:00)
            Color warmSodiumWhite = new Color(1.00f, 0.88f, 0.66f, 1.0f); // #FFE0A8 warm municipal streetlamp tone

            GameObject pointLightGo = new GameObject("Luminaire_PointLight");
            pointLightGo.transform.SetParent(lampRoot.transform);
            pointLightGo.transform.localPosition = new Vector3(0f, 6.38f, 0f);
            pointLightGo.transform.localRotation = Quaternion.identity;
            Light pointLight = pointLightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = warmSodiumWhite;
            pointLight.range = 22.0f;
            pointLight.intensity = 2.8f; // Base intensity stored by TimeOfDayEnvironmentController
            pointLight.shadows = LightShadows.None;
            pointLight.renderMode = LightRenderMode.ForcePixel;
            pointLight.enabled = false; // Default OFF for daytime default scene state

            // 6. Real Downward Wide Spot Light (casts bright warm pool onto sidewalk, curb, roadway & storefronts)
            GameObject downSpotGo = new GameObject("Luminaire_Downlight");
            downSpotGo.transform.SetParent(lampRoot.transform);
            downSpotGo.transform.localPosition = new Vector3(0f, 6.45f, 0f);
            downSpotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Pointing straight down (-Y)
            Light spotLight = downSpotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = 138f;
            spotLight.innerSpotAngle = 85f;
            spotLight.color = warmSodiumWhite;
            spotLight.range = 18.5f;
            spotLight.intensity = 4.2f; // Base intensity stored by TimeOfDayEnvironmentController
            spotLight.shadows = LightShadows.None;
            spotLight.renderMode = LightRenderMode.ForcePixel;
            spotLight.enabled = false; // Default OFF for daytime default scene state

            return lampRoot;
        }

        public static void CaptureDayNightVerificationScreenshots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("BorderCorridorScene.unity"))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            TimeOfDayEnvironmentController todController = UnityEngine.Object.FindAnyObjectByType<TimeOfDayEnvironmentController>();
            if (todController == null)
            {
                GameObject envGo = GameObject.Find("TimeOfDayEnvironmentController") ?? new GameObject("TimeOfDayEnvironmentController");
                todController = envGo.GetComponent<TimeOfDayEnvironmentController>() ?? envGo.AddComponent<TimeOfDayEnvironmentController>();
            }
            todController.DiscoverSceneReferences();

            GameObject camGo = new GameObject("StreetlightVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // -------------------------------------------------------------------------
            // STATE A: DAYTIME (After 7:00 AM -> 10:30 AM Morning, streetlights OFF)
            // -------------------------------------------------------------------------
            WorldEnvironmentResponseDto dayDto = BuildSyntheticSnapshot(
                hour: 10, minute: 30,
                phase: "morning", titleEn: "Morning", titleEs: "Mañana",
                isNight: false,
                sunPitch: 42.0f, sunYaw: 115.0f, sunIntensity: 1.15f, sunColorHex: "#FFF4E0",
                ambientIntensity: 1.0f, ambientSkyHex: "#87CEEB", ambientEquatorHex: "#6E7F8D", ambientGroundHex: "#3D3730",
                streetlightsActive: false, streetlightsIntensity: 0.0f,
                skyboxExposure: 1.0f, fogColorHex: "#87CEEB");

            todController.ApplyAuthoritativeSnapshot(dayDto, snapImmediately: true);

            // Shot 1: Daytime Full Corridor View from South looking North (all 6 symmetric streetlights OFF)
            cam.fieldOfView = 62f;
            cam.transform.position = new Vector3(0.0f, 3.2f, -27.5f);
            cam.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "streetlights_day_corridor_off.png"));

            // Shot 2: Daytime Perspective View showing East & West center & corner lamps OFF
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-3.2f, 2.3f, -12.5f);
            cam.transform.rotation = Quaternion.Euler(-2.0f, 32f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "streetlights_day_east_closeup.png"));

            // -------------------------------------------------------------------------
            // STATE B: EVENING / NIGHT (After 6:00 PM -> 20:15 PM, streetlights ON)
            // -------------------------------------------------------------------------
            WorldEnvironmentResponseDto nightDto = BuildSyntheticSnapshot(
                hour: 20, minute: 15,
                phase: "evening", titleEn: "Evening", titleEs: "Atardecer / Noche",
                isNight: true,
                sunPitch: 8.0f, sunYaw: 275.0f, sunIntensity: 0.18f, sunColorHex: "#5C6BC0",
                ambientIntensity: 0.42f, ambientSkyHex: "#1E2746", ambientEquatorHex: "#282D3C", ambientGroundHex: "#15151C",
                streetlightsActive: true, streetlightsIntensity: 1.0f,
                skyboxExposure: 0.35f, fogColorHex: "#1E2746");

            todController.ApplyAuthoritativeSnapshot(nightDto, snapImmediately: true);

            // Shot 3: Nighttime Full Corridor View from South looking North (all 6 symmetric streetlights ON & illuminating scene)
            cam.fieldOfView = 62f;
            cam.transform.position = new Vector3(0.0f, 3.2f, -27.5f);
            cam.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "streetlights_night_corridor_on.png"));

            // Shot 4: Nighttime East Sidewalk View (South Corner, Benavides Center & North Corner lamps glowing)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-3.2f, 2.3f, -12.5f);
            cam.transform.rotation = Quaternion.Euler(-2.0f, 32f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "streetlights_night_east_on.png"));

            // Shot 5: Nighttime West Sidewalk Mirror View (South Corner, West Center & North Corner lamps glowing)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(3.2f, 2.3f, -12.5f);
            cam.transform.rotation = Quaternion.Euler(-2.0f, -32f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "streetlights_night_west_on.png"));

            // Restore clean daytime default state in the saved scene so Editor opens in bright morning/midday state
            todController.ApplyAuthoritativeSnapshot(dayDto, snapImmediately: true);
            UnityEngine.Object.DestroyImmediate(camGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[CorridorStreetlightStandardizer] Day/Night verification screenshots captured and default scene state restored!");
        }

        private static WorldEnvironmentResponseDto BuildSyntheticSnapshot(
            int hour, int minute,
            string phase, string titleEn, string titleEs,
            bool isNight,
            float sunPitch, float sunYaw, float sunIntensity, string sunColorHex,
            float ambientIntensity, string ambientSkyHex, string ambientEquatorHex, string ambientGroundHex,
            bool streetlightsActive, float streetlightsIntensity,
            float skyboxExposure, string fogColorHex)
        {
            return new WorldEnvironmentResponseDto
            {
                success = true,
                evaluatedAt = $"2026-11-05T{hour:D2}:{minute:D2}:00Z",
                timeZone = "America/Ciudad_Juarez",
                localCivilTime = $"2026-11-05T{hour:D2}:{minute:D2}:00",
                civilTime = new CivilTimeDto
                {
                    year = 2026,
                    month = 11,
                    day = 5,
                    hour = hour,
                    minute = minute,
                    second = 0,
                    dayOfWeek = 4
                },
                environment = new EnvironmentalLightingDto
                {
                    phase = phase,
                    phaseTitle = new BilingualTextDto { en = titleEn, es = titleEs },
                    phaseProgress = 0.5f,
                    isNight = isNight,
                    sunPitchDegrees = sunPitch,
                    sunYawDegrees = sunYaw,
                    sunIntensity = sunIntensity,
                    sunColorHex = sunColorHex,
                    ambientIntensity = ambientIntensity,
                    ambientSkyHex = ambientSkyHex,
                    ambientEquatorHex = ambientEquatorHex,
                    ambientGroundHex = ambientGroundHex,
                    streetlightsActive = streetlightsActive,
                    streetlightsIntensity = streetlightsIntensity,
                    skyboxExposure = skyboxExposure,
                    fogColorHex = fogColorHex
                }
            };
        }

        private static Material CreateOrGetMaterial(string path, Color color, float smoothness, float metallic, bool enableEmission)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (enableEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", Color.black); // Controlled dynamically via MaterialPropertyBlock
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void RenderToFile(Camera cam, int width, int height, string filePath)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(filePath, png);
            Debug.Log($"[CorridorStreetlightStandardizer] Saved screenshot: {filePath}");
        }
    }
}

