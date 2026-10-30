using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Editor
{
    public static class SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/HandshakeScene.unity";

        [MenuItem("MyVirtualCommunity/Setup Handshake Scene")]
        public static void SetupFoundationScene()
        {
            Debug.Log("[SceneSetup] Starting foundation scene creation...");

            // Ensure directory exists
            string dir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Ground Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.rotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(3f, 1f, 3f); // 30m x 30m

            // Give ground a clean asphalt/slate color
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                groundMat.color = new Color(0.22f, 0.24f, 0.26f); // dark asphalt
                groundRenderer.sharedMaterial = groundMat;
                AssetDatabase.CreateAsset(groundMat, "Assets/Settings/GroundMaterial.mat");
            }

            // 2. Lighting
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGo.transform.position = new Vector3(0f, 6f, 0f);

            // 3. Camera
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 4f, -14f);
            cameraGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            // 4. Handshake Controller System
            GameObject systemGo = new GameObject("HandshakeSystem");
            ApiClient client = systemGo.AddComponent<ApiClient>();
            client.BaseUrl = "http://127.0.0.1:3000";

            HandshakeController controller = systemGo.AddComponent<HandshakeController>();

            GameObject markersRoot = new GameObject("SpatialMarkers");
            markersRoot.transform.SetParent(systemGo.transform, false);

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty markersProp = serializedController.FindProperty("markersRoot");
            if (markersProp != null)
            {
                markersProp.objectReferenceValue = markersRoot.transform;
                serializedController.ApplyModifiedProperties();
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneSetup] HandshakeScene successfully saved to {ScenePath}");

            // Configure Build Settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[SceneSetup] EditorBuildSettings updated with HandshakeScene at index 0.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

