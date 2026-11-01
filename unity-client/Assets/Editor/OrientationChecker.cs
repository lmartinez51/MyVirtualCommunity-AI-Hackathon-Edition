using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class OrientationChecker
    {
        public static void CheckAllRotations()
        {
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/NPCs/ElenaRamos_Bobblehead.prefab");
            var go = UnityEngine.Object.Instantiate(bodyPrefab);
            go.transform.position = Vector3.zero;

            var anim = go.GetComponent<Animator>();
            if (anim != null) anim.Update(1.0f);

            GameObject camGo = new GameObject("TestCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.gray;
            // Camera at (0, 1.4, -2.2) looking at (0, 1.4, 0) -> looking +Z
            cam.transform.position = new Vector3(0f, 1.4f, -2.2f);
            cam.transform.rotation = Quaternion.identity;

            // Add a directional light so the character is lit
            GameObject lightGo = new GameObject("TestLight");
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(30f, 30f, 0f);

            string screenshotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots"));
            if (!Directory.Exists(screenshotDir)) Directory.CreateDirectory(screenshotDir);

            int[] rots = new int[] { 0, 90, 180, 270 };
            foreach (int r in rots)
            {
                go.transform.rotation = Quaternion.Euler(0f, r, 0f);
                RenderTexture rt = new RenderTexture(640, 480, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                Texture2D tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string outPath = Path.Combine(screenshotDir, $"test_rot_{r}.png");
                File.WriteAllBytes(outPath, bytes);
                Debug.Log($"[OrientationChecker] Rendered rotation {r} to {outPath}");
            }

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(lightGo);
            EditorApplication.Exit(0);
        }
    }
}
