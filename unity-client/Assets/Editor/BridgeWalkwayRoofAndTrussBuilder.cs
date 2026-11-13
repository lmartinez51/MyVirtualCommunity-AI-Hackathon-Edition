using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Replaces the flat Walkway_Roof_* slabs and sparse posts on the East bridge walkway
    /// with a curved terracotta barrel-vault canopy, transverse arch ribs, and white/light-grey
    /// metallic lateral Warren-truss lattice structure matching the real Paso del Norte bridge.
    /// </summary>
    public static class BridgeWalkwayRoofAndTrussBuilder
    {
        private const string RootName = "EastWalkway_CurvedRoof_And_Truss_Root";

        private const float HalfRoofWidth = 1.45f;    // 2.90m total canopy width
        private const float ArchRise = 0.48f;         // Barrel-vault arch height at center
        private const float RoofThickness = 0.08f;
        private const float EaveHeightAboveSlab = 2.50f;
        private const float MidRailHeightAboveSlab = 1.15f;
        private const float WestTrussX = -1.05f;      // X = 14.20m at Seg01
        private const float EastTrussX = 1.05f;       // X = 16.30m at Seg01

        [MenuItem("MyVirtualCommunity/Rebuild East Walkway Curved Roof & Lateral Truss")]
        public static void ExecuteRebuild()
        {
            Debug.Log("[BridgeWalkwayRoofAndTrussBuilder] Starting curved roof & lateral truss rebuild...");

            const string scenePath = "Assets/Scenes/BorderCorridorScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Clean up previous build if re-running
            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            // 2. Disable flat roofs and sparse legacy posts
            DisableLegacyRoofsAndPosts();

            // 3. Materials
            Material roofTerracottaMat = LoadOrCreateMaterial(
                "Assets/Settings/Walkway_Roof_Terracotta.mat",
                new Color(0.70f, 0.37f, 0.31f, 1f),
                0.30f);
            Material trussWhiteSteelMat = LoadOrCreateMaterial(
                "Assets/Settings/Walkway_Truss_WhiteSteel.mat",
                new Color(0.84f, 0.86f, 0.88f, 1f),
                0.45f);

            GameObject root = new GameObject(RootName);

            // 4. Collect all walkway floor slabs in order
            var slabs = new List<Transform>();
            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject go = GameObject.Find($"Walkway_Slab_Seg{seg:D2}");
                if (go != null) slabs.Add(go.transform);
            }
            GameObject rampGo = GameObject.Find("Walkway_Slab_Ramp");
            if (rampGo != null) slabs.Add(rampGo.transform);
            GameObject g01Go = GameObject.Find("Walkway_Slab_G01");
            if (g01Go != null) slabs.Add(g01Go.transform);

            // 5. Build curved barrel-vault roof and lateral truss per segment
            for (int i = 0; i < slabs.Count; i++)
            {
                Transform slab = slabs[i];
                bool isLast = (i == slabs.Count - 1);
                BuildSegmentRoofAndTruss(root.transform, slab, isLast, roofTerracottaMat, trussWhiteSteelMat);
            }

            // 6. Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // 7. Capture verification screenshots
            CaptureScreenshots();

            Debug.Log($"[BridgeWalkwayRoofAndTrussBuilder] REBUILD FINISHED SUCCESSFULLY for {slabs.Count} walkway segments!");
        }

        private static void DisableLegacyRoofsAndPosts()
        {
            int disabled = 0;
            for (int seg = 1; seg <= 28; seg++)
            {
                string[] names = new string[]
                {
                    $"Walkway_Roof_Seg{seg:D2}",
                    $"Walkway_Post_InA_Seg{seg:D2}",
                    $"Walkway_Post_InB_Seg{seg:D2}",
                    $"Walkway_Post_OutA_Seg{seg:D2}",
                    $"Walkway_Post_OutB_Seg{seg:D2}"
                };
                foreach (string n in names)
                {
                    GameObject go = GameObject.Find(n);
                    if (go != null)
                    {
                        go.SetActive(false);
                        disabled++;
                    }
                }
            }

            string[] extras = new string[]
            {
                "Walkway_Roof_Ramp",
                "Walkway_Roof_G01",
                "Walkway_Post_Ramp_Z365",
                "Walkway_Post_Ramp_Z405",
                "SentriSign_Board",
                "SentriSign_Post"
            };
            foreach (string n in extras)
            {
                GameObject go = GameObject.Find(n);
                if (go != null)
                {
                    go.SetActive(false);
                    disabled++;
                }
            }

            Debug.Log($"[BridgeWalkwayRoofAndTrussBuilder] Disabled {disabled} legacy flat roof & post objects.");
        }

        private static void BuildSegmentRoofAndTruss(
            Transform root,
            Transform slab,
            bool capNorthEnd,
            Material roofMat,
            Material steelMat)
        {
            float segLen = slab.localScale.z;
            float slabTopOffset = slab.localScale.y * 0.5f; // 0.075m

            GameObject segRoot = new GameObject($"WalkwayStructure_{slab.name}");
            segRoot.transform.SetParent(root);

            // 1. Curved barrel-vault roof shell
            Vector3 eaveCenter = slab.position + slab.up * (slabTopOffset + EaveHeightAboveSlab);
            CreateBarrelVaultRoof(
                $"{slab.name}_CurvedRoof",
                segRoot.transform,
                eaveCenter,
                slab.rotation,
                HalfRoofWidth,
                ArchRise,
                RoofThickness,
                segLen,
                roofMat);

            // 2. Longitudinal beams (top eaves + mid-height rails) on West and East sides
            CreateBeam($"{slab.name}_WestTopBeam", segRoot.transform, slab, WestTrussX, slabTopOffset + EaveHeightAboveSlab, 0f, 0.10f, 0.12f, segLen, steelMat);
            CreateBeam($"{slab.name}_EastTopBeam", segRoot.transform, slab, EastTrussX, slabTopOffset + EaveHeightAboveSlab, 0f, 0.10f, 0.12f, segLen, steelMat);
            CreateBeam($"{slab.name}_WestMidRail", segRoot.transform, slab, WestTrussX, slabTopOffset + MidRailHeightAboveSlab, 0f, 0.07f, 0.07f, segLen, steelMat);
            CreateBeam($"{slab.name}_EastMidRail", segRoot.transform, slab, EastTrussX, slabTopOffset + MidRailHeightAboveSlab, 0f, 0.07f, 0.07f, segLen, steelMat);

            // 3. Divide segment into ~3.0m bays for vertical posts, diagonal truss struts, and transverse arch ribs
            int bayCount = Mathf.Max(1, Mathf.RoundToInt(segLen / 3.0f));
            float bayLen = segLen / bayCount;
            float zStart = -segLen * 0.5f;

            for (int b = 0; b <= bayCount; b++)
            {
                if (b == bayCount && !capNorthEnd) continue;
                float z = zStart + b * bayLen;

                // Vertical posts on West and East sides
                CreatePost($"{slab.name}_PostW_{b}", segRoot.transform, slab, WestTrussX, slabTopOffset, z, EaveHeightAboveSlab, 0.09f, steelMat);
                CreatePost($"{slab.name}_PostE_{b}", segRoot.transform, slab, EastTrussX, slabTopOffset, z, EaveHeightAboveSlab, 0.09f, steelMat);

                // Transverse curved arch rib under the roof
                CreateTransverseArchRib($"{slab.name}_ArchRib_{b}", segRoot.transform, slab, slabTopOffset + EaveHeightAboveSlab, z, WestTrussX, EastTrussX, ArchRise, steelMat);
            }

            // 4. Diagonal Warren-truss zig-zag struts in each bay between MidRail and TopBeam
            for (int b = 0; b < bayCount; b++)
            {
                float zA = zStart + b * bayLen;
                float zB = zA + bayLen;
                float zMid = (zA + zB) * 0.5f;

                // V-truss: (zA, top) -> (zMid, mid) -> (zB, top)
                Vector3 wTopA = GetWorldPoint(slab, WestTrussX, slabTopOffset + EaveHeightAboveSlab, zA);
                Vector3 wMid = GetWorldPoint(slab, WestTrussX, slabTopOffset + MidRailHeightAboveSlab, zMid);
                Vector3 wTopB = GetWorldPoint(slab, WestTrussX, slabTopOffset + EaveHeightAboveSlab, zB);

                CreateStrut($"{slab.name}_TrussW_{b}_A", segRoot.transform, wTopA, wMid, 0.055f, steelMat);
                CreateStrut($"{slab.name}_TrussW_{b}_B", segRoot.transform, wMid, wTopB, 0.055f, steelMat);

                Vector3 eTopA = GetWorldPoint(slab, EastTrussX, slabTopOffset + EaveHeightAboveSlab, zA);
                Vector3 eMid = GetWorldPoint(slab, EastTrussX, slabTopOffset + MidRailHeightAboveSlab, zMid);
                Vector3 eTopB = GetWorldPoint(slab, EastTrussX, slabTopOffset + EaveHeightAboveSlab, zB);

                CreateStrut($"{slab.name}_TrussE_{b}_A", segRoot.transform, eTopA, eMid, 0.055f, steelMat);
                CreateStrut($"{slab.name}_TrussE_{b}_B", segRoot.transform, eMid, eTopB, 0.055f, steelMat);
            }
        }

        private static Vector3 GetWorldPoint(Transform slab, float rightX, float upY, float forwardZ)
        {
            return slab.position + slab.right * rightX + slab.up * upY + slab.forward * forwardZ;
        }

        private static void CreateBeam(
            string name, Transform parent, Transform slab,
            float rightX, float upY, float forwardZ,
            float width, float height, float length, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = GetWorldPoint(slab, rightX, upY, forwardZ);
            go.transform.rotation = slab.rotation;
            go.transform.localScale = new Vector3(width, height, length);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            SetMaterial(go, mat);
        }

        private static void CreatePost(
            string name, Transform parent, Transform slab,
            float rightX, float slabTopY, float forwardZ,
            float height, float size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = GetWorldPoint(slab, rightX, slabTopY + height * 0.5f, forwardZ);
            go.transform.rotation = slab.rotation;
            go.transform.localScale = new Vector3(size, height, size);
            SetMaterial(go, mat);
        }

        private static void CreateStrut(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            Vector3 diff = b - a;
            float len = diff.magnitude;
            if (len < 0.01f) return;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(diff.normalized);
            go.transform.localScale = new Vector3(thickness, thickness, len);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            SetMaterial(go, mat);
        }

        private static void CreateTransverseArchRib(
            string name, Transform parent, Transform slab,
            float eaveY, float z, float xMin, float xMax, float archRise, Material mat)
        {
            const int segments = 4;
            for (int s = 0; s < segments; s++)
            {
                float t0 = (float)s / segments;
                float t1 = (float)(s + 1) / segments;
                float x0 = Mathf.Lerp(xMin, xMax, t0);
                float x1 = Mathf.Lerp(xMin, xMax, t1);
                float y0 = eaveY + archRise * (1f - Mathf.Pow(2f * t0 - 1f, 2f)) - 0.04f;
                float y1 = eaveY + archRise * (1f - Mathf.Pow(2f * t1 - 1f, 2f)) - 0.04f;

                Vector3 p0 = GetWorldPoint(slab, x0, y0, z);
                Vector3 p1 = GetWorldPoint(slab, x1, y1, z);
                CreateStrut($"{name}_{s}", parent, p0, p1, 0.07f, mat);
            }
        }

        private static void CreateBarrelVaultRoof(
            string name,
            Transform parent,
            Vector3 origin,
            Quaternion rotation,
            float halfWidth,
            float archRise,
            float thickness,
            float length,
            Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = origin;
            go.transform.rotation = rotation;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            const int arcSegs = 10;
            float zMin = -length * 0.5f;
            float zMax = length * 0.5f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            // Build top & bottom curved arc surfaces
            for (int i = 0; i < arcSegs; i++)
            {
                float u0 = (float)i / arcSegs;
                float u1 = (float)(i + 1) / arcSegs;
                float x0 = Mathf.Lerp(-halfWidth, halfWidth, u0);
                float x1 = Mathf.Lerp(-halfWidth, halfWidth, u1);

                float y0 = archRise * (1f - Mathf.Pow(2f * u0 - 1f, 2f));
                float y1 = archRise * (1f - Mathf.Pow(2f * u1 - 1f, 2f));

                // Top face quad
                AddQuad(verts, tris, uvs,
                    new Vector3(x0, y0 + thickness, zMax),
                    new Vector3(x1, y1 + thickness, zMax),
                    new Vector3(x1, y1 + thickness, zMin),
                    new Vector3(x0, y0 + thickness, zMin),
                    u0, u1);

                // Bottom face quad
                AddQuad(verts, tris, uvs,
                    new Vector3(x0, y0, zMin),
                    new Vector3(x1, y1, zMin),
                    new Vector3(x1, y1, zMax),
                    new Vector3(x0, y0, zMax),
                    u0, u1);
            }

            // West eave fascia (-halfWidth)
            AddQuad(verts, tris, uvs,
                new Vector3(-halfWidth, thickness, zMax),
                new Vector3(-halfWidth, thickness, zMin),
                new Vector3(-halfWidth, 0f, zMin),
                new Vector3(-halfWidth, 0f, zMax),
                0f, 1f);

            // East eave fascia (+halfWidth)
            AddQuad(verts, tris, uvs,
                new Vector3(halfWidth, thickness, zMin),
                new Vector3(halfWidth, thickness, zMax),
                new Vector3(halfWidth, 0f, zMax),
                new Vector3(halfWidth, 0f, zMin),
                0f, 1f);

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
        }

        private static void AddQuad(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            float u0, float u1)
        {
            int idx = verts.Count;
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            tris.Add(idx + 0); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx + 0); tris.Add(idx + 2); tris.Add(idx + 3);

            uvs.Add(new Vector2(u0, 1f));
            uvs.Add(new Vector2(u1, 1f));
            uvs.Add(new Vector2(u1, 0f));
            uvs.Add(new Vector2(u0, 0f));
        }

        private static void CaptureScreenshots()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempWalkwayRoofCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;

            // Shot 1: Webcam overview showing gantry sign, 4 lanes, white bollards, and curved terracotta roof + truss
            camGo.transform.position = new Vector3(4.00f, 9.50f, 48.00f);
            camGo.transform.rotation = Quaternion.Euler(14.0f, 1.5f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "walkway_roof_shot1_webcam_overview.png");

            // Shot 2: Lateral view from roadway looking at curved roof, white truss lattice, and 3-tube guardrail
            camGo.transform.position = new Vector3(9.50f, 2.50f, 62.00f);
            camGo.transform.rotation = Quaternion.Euler(2.0f, 18.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "walkway_roof_shot2_lateral_truss.png");

            // Shot 3: Inside pedestrian walkway looking North under the curved canopy and arch ribs
            camGo.transform.position = new Vector3(15.25f, 2.10f, 64.00f);
            camGo.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "walkway_roof_shot3_inside_walkway.png");

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void CaptureAndCopy(Camera cam, string outDir, string brainDir, string fileName)
        {
            string localPath = Path.Combine(outDir, fileName);
            CaptureShot(cam, localPath);
            try
            {
                File.Copy(localPath, Path.Combine(brainDir, fileName), true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BridgeWalkwayRoofAndTrussBuilder] Copy warning: {ex.Message}");
            }
        }

        private static void CaptureShot(Camera cam, string savePath)
        {
            int w = 1280;
            int h = 720;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(savePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static Material LoadOrCreateMaterial(string path, Color col, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = col;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }
    }
}

