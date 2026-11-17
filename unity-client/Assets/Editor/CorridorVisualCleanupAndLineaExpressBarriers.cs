using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Focused scene visual cleanup & Línea Express Jersey barrier installation:
    /// 1. Removes the stray box in the middle of Avenida Benito Juárez (`Corner_Bollard_Base` at (0, 0.1, 0)).
    /// 2. Removes all mid-bridge flagpoles & flags (`FlagPole_*_Z220`, `Flag_*_Z220`) from bridge lanes.
    /// 3. Installs interlocking orange plastic New Jersey road barriers delimiting/closing the Línea Express lane (Lane 4)
    ///    both north and south of the white-and-yellow pedestrian zebra crosswalk (Z = 53.70..55.50m),
    ///    keeping the pedestrian crosswalk completely clear and adding NO traffic drum ("tambo").
    /// </summary>
    public static class CorridorVisualCleanupAndLineaExpressBarriers
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string BarriersRootName = "Bridge_LineaExpress_OrangeJerseyBarriers_Root";
        private const string BarrierMeshAssetPath = "Assets/Settings/OrangePlasticNewJerseyBarrier.asset";
        private const string BarrierMaterialAssetPath = "Assets/Settings/OrangeJerseyBarrierPlastic.mat";

        // Crosswalk & NPC clear corridor guard band: Z = 53.50m .. 55.80m
        private const float CrosswalkClearMinZ = 53.50f;
        private const float CrosswalkClearMaxZ = 55.80f;

        [MenuItem("MyVirtualCommunity/Apply Visual Cleanup & Linea Express Jersey Barriers")]
        public static void Execute()
        {
            Debug.Log("================================================================================");
            Debug.Log("[CorridorVisualCleanup] Starting scene visual cleanup & Línea Express barrier placement...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CorridorVisualCleanup] Failed to open scene '{ScenePath}'");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // 1. Remove the stray box in the middle of Avenida Benito Juárez
            int removedJuarezBoxes = RemoveStrayAvJuarezBoxes();

            // 2. Remove all flagpoles & flags in the middle of the International Bridge
            int removedBridgeFlags = RemoveBridgeLaneFlags();

            // 3. Install plastic orange New Jersey barriers closing Línea Express (leaving crosswalk clear, NO drum)
            int placedBarriers = BuildLineaExpressJerseyBarriers();

            // 4. Install rear Fideicomiso Easter Egg camera ("Paso del Norte - Vista Norte")
            InstallVistaNorteEasterEggCamera();

            // 5. Save clean scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CorridorVisualCleanup] Saved scene cleanly. Removed Juarez boxes={removedJuarezBoxes}, bridge flag objects={removedBridgeFlags}, placed orange Jersey barriers={placedBarriers}, installed VistaNorte camera.");

            // 5. Capture verification screenshots & audit bounds
            CaptureVerificationScreenshots();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static int RemoveStrayAvJuarezBoxes()
        {
            var toDestroy = new List<GameObject>();
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (string.Equals(go.name, "Corner_Bollard_Base", StringComparison.OrdinalIgnoreCase))
                {
                    toDestroy.Add(go);
                }
            }

            foreach (var go in toDestroy)
            {
                Debug.Log($"[CorridorVisualCleanup] Removing stray Av. Juárez box '{go.name}' at {go.transform.position} (scale={go.transform.localScale}).");
                UnityEngine.Object.DestroyImmediate(go);
            }

            // Audit: ensure no small stray cube/prop sits in the roadway center of Av. Juárez (X in [-2.4, 6.5], Z in [-58, 38], Y in [0.05, 2.0])
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mr == null || !mr.gameObject.activeInHierarchy) continue;
                Bounds b = mr.bounds;
                bool inJuarezRoadway = b.center.x >= -2.4f && b.center.x <= 6.5f &&
                                       b.center.z >= -58f && b.center.z <= 38f &&
                                       b.center.y >= 0.05f && b.center.y <= 2.5f &&
                                       b.size.x < 3.0f && b.size.z < 3.0f;
                if (inJuarezRoadway)
                {
                    Debug.LogWarning($"[CorridorVisualCleanup][Audit] Remaining object in Av. Juárez roadway: '{mr.gameObject.name}' boundsCenter={b.center} boundsSize={b.size}");
                }
            }

            return toDestroy.Count;
        }

        private static int RemoveBridgeLaneFlags()
        {
            var toDestroy = new List<GameObject>();
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                string n = go.name;
                bool isBridgeFlag = n.StartsWith("FlagPole_MX_", StringComparison.OrdinalIgnoreCase) ||
                                    n.StartsWith("FlagPole_US_", StringComparison.OrdinalIgnoreCase) ||
                                    n.StartsWith("Flag_MX_", StringComparison.OrdinalIgnoreCase) ||
                                    n.StartsWith("Flag_US_", StringComparison.OrdinalIgnoreCase);
                if (isBridgeFlag)
                {
                    toDestroy.Add(go);
                }
            }

            foreach (var go in toDestroy)
            {
                Debug.Log($"[CorridorVisualCleanup] Removing bridge lane flag object '{go.name}' at {go.transform.position}.");
                UnityEngine.Object.DestroyImmediate(go);
            }

            return toDestroy.Count;
        }

        private static int BuildLineaExpressJerseyBarriers()
        {
            while (GameObject.Find(BarriersRootName) != null)
            {
                UnityEngine.Object.DestroyImmediate(GameObject.Find(BarriersRootName));
            }

            GameObject plaza = GameObject.Find("Border_Crossing_Plaza");
            GameObject root = new GameObject(BarriersRootName);
            if (plaza != null)
            {
                root.transform.SetParent(plaza.transform);
            }
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            Mesh barrierMesh = LoadOrCreateJerseyBarrierMesh();
            Material orangeMat = LoadOrCreateMaterial(
                BarrierMaterialAssetPath,
                new Color(0.95f, 0.36f, 0.04f, 1f),
                0.38f);

            int count = 0;

            // Straight-line alignment directly over the solid white Divider 3|4 line (X = 8.975m),
            // matching the continuation of Bridge_Bollard_Start (8.975, 68.215) southward.
            const float divider34X = 8.975f;

            // North Run (North of pedestrian crosswalk Z = 53.70..55.50m):
            // 6 interlocking orange New Jersey barriers in a straight line directly over Span_Divider34_Solid_North,
            // from just south of Bridge_Bollard_Start (Z = 67.85m) down to Z = 55.90m (leaving Z=53.50..55.80m clear).
            count += PlaceJerseyRun(
                root.transform,
                "LineaExpress_Jersey_North",
                new Vector2(divider34X, 67.85f),
                new Vector2(divider34X, 55.90f),
                6,
                barrierMesh,
                orangeMat);

            // South Run (South of pedestrian crosswalk Z = 53.70..55.50m):
            // Single (1) orange New Jersey barrier in a straight line directly over Span_Divider34_Solid_South
            // on the flat Bridge_Span_Deck (Z = 53.35m..51.40m), keeping the crosswalk 100% unobstructed.
            count += PlaceJerseyRun(
                root.transform,
                "LineaExpress_Jersey_South",
                new Vector2(divider34X, 53.35f),
                new Vector2(divider34X, 51.40f),
                1,
                barrierMesh,
                orangeMat);

            // Verify zero encroachment into crosswalk clearance band
            foreach (var col in root.GetComponentsInChildren<BoxCollider>())
            {
                Bounds b = col.bounds;
                if (b.max.z > CrosswalkClearMinZ && b.min.z < CrosswalkClearMaxZ)
                {
                    throw new InvalidOperationException(
                        $"[CorridorVisualCleanup] Barrier '{col.gameObject.name}' overlaps pedestrian crosswalk band Z=[{CrosswalkClearMinZ}, {CrosswalkClearMaxZ}]: boundsZ=[{b.min.z:F2}, {b.max.z:F2}]");
                }
            }

            return count;
        }

        private static int PlaceJerseyRun(
            Transform parent,
            string prefix,
            Vector2 startXZ,
            Vector2 endXZ,
            int segmentCount,
            Mesh barrierMesh,
            Material mat)
        {
            Vector2 delta = endXZ - startXZ;
            float totalLen = delta.magnitude;
            float segPitch = totalLen / segmentCount;
            Vector2 dir2D = delta.normalized;
            float yawDeg = Mathf.Atan2(dir2D.x, dir2D.y) * Mathf.Rad2Deg;

            // Body mesh length is 1.88m (z in [-0.94, +0.94]); scale Z slightly so segments butt tightly (~1.96m total)
            float zScale = (segPitch - 0.02f) / 1.88f;

            for (int i = 0; i < segmentCount; i++)
            {
                float t0 = (float)i / segmentCount;
                float t1 = (float)(i + 1) / segmentCount;
                float tMid = (t0 + t1) * 0.5f;

                Vector2 pBack = Vector2.Lerp(startXZ, endXZ, t0);
                Vector2 pMid = Vector2.Lerp(startXZ, endXZ, tMid);
                Vector2 pFront = Vector2.Lerp(startXZ, endXZ, t1);

                float yBack = SampleBridgeDeckSurfaceY(pBack.x, pBack.y);
                float yFront = SampleBridgeDeckSurfaceY(pFront.x, pFront.y);
                float yMid = (yBack + yFront) * 0.5f;

                // Pitch along travel direction so base sits flush on both flat deck and transition ramp
                float pitchDeg = -Mathf.Atan2(yFront - yBack, segPitch) * Mathf.Rad2Deg;

                GameObject go = new GameObject($"{prefix}_{i + 1:D2}");
                go.transform.SetParent(parent);
                go.transform.position = new Vector3(pMid.x, yMid, pMid.y);
                go.transform.rotation = Quaternion.Euler(pitchDeg, yawDeg, 0f);
                go.transform.localScale = new Vector3(1f, 1f, zScale);
                go.isStatic = true;

                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = barrierMesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                BoxCollider col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.425f, 0f);
                col.size = new Vector3(0.48f, 0.85f, 1.88f);
            }

            return segmentCount;
        }

        private static float SampleBridgeDeckSurfaceY(float x, float z)
        {
            // Bridge_Span_Deck top is 0.400m (Z = 51..69m), Bridge_TransitionRamp_Z51 rises linearly 0.00 -> 0.400m across Z = 47..51m
            if (Physics.Raycast(new Vector3(x, 3.0f, z), Vector3.down, out RaycastHit hit, 5.0f))
            {
                string hitName = hit.collider != null ? hit.collider.gameObject.name : string.Empty;
                if (hitName.Contains("Bridge_Span_Deck") ||
                    hitName.Contains("Bridge_TransitionRamp") ||
                    hitName.Contains("Bridge_Deck_Seg") ||
                    hitName.Contains("Road_AvenidaJuarez"))
                {
                    return hit.point.y;
                }
            }

            if (z >= 51.0f) return 0.400f;
            if (z <= 47.0f) return 0.000f;
            return 0.400f * ((z - 47.0f) / 4.0f);
        }

        private static Mesh LoadOrCreateJerseyBarrierMesh()
        {
            // Always regenerate cleanly into the asset path so profile updates persist
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(BarrierMeshAssetPath);

            // Realistic PE New Jersey barrier cross-section centered at X=0, Y in [0, 0.85m], Z in [-0.94, +0.94]
            Vector2[] profile = new Vector2[]
            {
                new Vector2(-0.25f, 0.00f), // 0: bottom-left
                new Vector2( 0.25f, 0.00f), // 1: bottom-right
                new Vector2( 0.25f, 0.10f), // 2: right foot top
                new Vector2( 0.11f, 0.34f), // 3: right Jersey shoulder
                new Vector2( 0.08f, 0.80f), // 4: right upper wall top
                new Vector2( 0.05f, 0.85f), // 5: right crown
                new Vector2(-0.05f, 0.85f), // 6: left crown
                new Vector2(-0.08f, 0.80f), // 7: left upper wall top
                new Vector2(-0.11f, 0.34f), // 8: left Jersey shoulder
                new Vector2(-0.25f, 0.10f), // 9: left foot top
            };

            float z0 = -0.94f;
            float z1 = 0.94f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outwardHint)
            {
                // Ensure Unity clockwise front-face winding matches outwardHint via left-handed cross product
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(normal, outwardHint) < 0f)
                {
                    Vector3 tmp = b;
                    b = d;
                    d = tmp;
                }

                int baseIdx = verts.Count;
                verts.Add(a);
                verts.Add(b);
                verts.Add(c);
                verts.Add(d);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                tris.Add(baseIdx);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 3);
            }

            // Extruded perimeter quads (flat shaded per face, mathematically verified outward normals)
            for (int i = 0; i < profile.Length; i++)
            {
                Vector2 p0 = profile[i];
                Vector2 p1 = profile[(i + 1) % profile.Length];
                Vector2 edge = p1 - p0;
                // Profile goes CCW in XY, so outward 2D normal is (+edge.y, -edge.x)
                Vector3 outN = new Vector3(edge.y, -edge.x, 0f).normalized;
                AddQuad(
                    new Vector3(p0.x, p0.y, z0),
                    new Vector3(p0.x, p0.y, z1),
                    new Vector3(p1.x, p1.y, z1),
                    new Vector3(p1.x, p1.y, z0),
                    outN);
            }

            // End caps (split into convex strips between symmetric left/right pairs)
            int[][] capPairs = new int[][]
            {
                new int[] { 0, 1, 2, 9 },
                new int[] { 9, 2, 3, 8 },
                new int[] { 8, 3, 4, 7 },
                new int[] { 7, 4, 5, 6 },
            };

            foreach (var q in capPairs)
            {
                // Back cap (-Z) facing -Z
                AddQuad(
                    new Vector3(profile[q[0]].x, profile[q[0]].y, z0),
                    new Vector3(profile[q[3]].x, profile[q[3]].y, z0),
                    new Vector3(profile[q[2]].x, profile[q[2]].y, z0),
                    new Vector3(profile[q[1]].x, profile[q[1]].y, z0),
                    Vector3.back);

                // Front cap (+Z) facing +Z
                AddQuad(
                    new Vector3(profile[q[0]].x, profile[q[0]].y, z1),
                    new Vector3(profile[q[1]].x, profile[q[1]].y, z1),
                    new Vector3(profile[q[2]].x, profile[q[2]].y, z1),
                    new Vector3(profile[q[3]].x, profile[q[3]].y, z1),
                    Vector3.forward);
            }

            // Small interlocking center lug on +Z end (makes barriers visibly lock end-to-end)
            float lugW = 0.045f;
            float lugZ0 = z1;
            float lugZ1 = z1 + 0.07f;
            float lugY0 = 0.08f;
            float lugY1 = 0.78f;
            AddQuad(new Vector3(-lugW, lugY0, lugZ1), new Vector3(lugW, lugY0, lugZ1), new Vector3(lugW, lugY1, lugZ1), new Vector3(-lugW, lugY1, lugZ1), Vector3.forward);
            AddQuad(new Vector3(lugW, lugY0, lugZ0), new Vector3(lugW, lugY0, lugZ1), new Vector3(lugW, lugY1, lugZ1), new Vector3(lugW, lugY1, lugZ0), Vector3.right);
            AddQuad(new Vector3(-lugW, lugY0, lugZ1), new Vector3(-lugW, lugY0, lugZ0), new Vector3(-lugW, lugY1, lugZ0), new Vector3(-lugW, lugY1, lugZ1), Vector3.left);
            AddQuad(new Vector3(-lugW, lugY1, lugZ0), new Vector3(-lugW, lugY1, lugZ1), new Vector3(lugW, lugY1, lugZ1), new Vector3(lugW, lugY1, lugZ0), Vector3.up);

            Mesh mesh = existing ?? new Mesh();
            mesh.Clear();
            mesh.name = "OrangePlasticNewJerseyBarrier";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, BarrierMeshAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Material LoadOrCreateMaterial(string path, Color col, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void CaptureVerificationScreenshots()
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string brainDir = "C:/Users/leolm/.gemini/antigravity/brain/4ce9141f-e967-41a8-b6f3-7471781ad5c8/scratch";
            if (!Directory.Exists(brainDir)) Directory.CreateDirectory(brainDir);

            GameObject camGo = new GameObject("CleanupVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;

            void Snap(string fileName, Vector3 pos, Quaternion rot, float fov = 60f)
            {
                cam.fieldOfView = fov;
                cam.transform.position = pos;
                cam.transform.rotation = rot;
                string localPath = Path.Combine(outDir, fileName);
                RenderToPng(cam, 1280, 720, localPath);
                File.Copy(localPath, Path.Combine(brainDir, fileName), true);
            }

            // 1. Player spawn view along Avenida Benito Juárez looking North (verify stray box at (0,0.1,0) is gone)
            Snap("cleanup_01_av_juarez_no_box.png", new Vector3(0.0f, 1.70f, -15.0f), Quaternion.Euler(4f, 6f, 0f));

            // 2. Mid-bridge view looking North across Z = 220m (verify flagpoles & flags in bridge lanes are gone)
            Snap("cleanup_02_bridge_mid_no_flags.png", new Vector3(6.0f, 10.50f, 185.0f), Quaternion.Euler(11f, 3f, 0f));

            // 3. Elevated webcam perspective matching real traffic photo (verify orange Jersey barriers close Línea Express cleanly with NO drum)
            Snap("cleanup_03_linea_express_webcam_angle.png", new Vector3(17.50f, 6.80f, 46.50f), Quaternion.Euler(27f, 336f, 0f));

            // 4. Eye-level pedestrian crosswalk view looking NE (verify zebra crosswalk is 100% open and clear)
            Snap("cleanup_04_linea_express_crosswalk_eyelevel.png", new Vector3(5.50f, 1.65f, 51.00f), Quaternion.Euler(8f, 48f, 0f));

            // 5. Top-down plan view of Lane 4, Crosswalk Z=53.7..55.5, and all 7 orange Jersey barriers
            Snap("cleanup_05_linea_express_topdown.png", new Vector3(11.00f, 22.00f, 57.50f), Quaternion.Euler(90f, 0f, 0f));

            // 6. Simulated POV from Bridge_Fideicomiso_Camera_VistaNorte (just ahead of lens barrel so sunshield/body never clip) matching YouTube Live screenshot
            Snap("cleanup_06_vista_norte_pov.png", new Vector3(5.96f, 8.14f, 51.32f), Quaternion.Euler(27f, 356f, 0f), 74f);

            // 7. Close-up looking South at the rear of Bridge_Canopy_PlazaDeCobro showing Bridge_Fideicomiso_Camera_VistaNorte mounted cleanly
            Snap("cleanup_07_vista_norte_exterior.png", new Vector3(6.50f, 5.20f, 58.00f), Quaternion.Euler(340f, 184f, 0f), 55f);

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void InstallVistaNorteEasterEggCamera()
        {
            GameObject canopy = GameObject.Find("Bridge_Canopy_PlazaDeCobro");
            if (canopy == null)
            {
                Debug.LogError("[CorridorVisualCleanup] Bridge_Canopy_PlazaDeCobro not found; cannot mount VistaNorte camera.");
                return;
            }

            Material whiteMat = LoadOrCreateMaterial("Assets/Settings/BridgeWhiteConcrete.mat", new Color(0.92f, 0.92f, 0.92f, 1f), 0.1f);
            Material steelMat = LoadOrCreateMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.55f, 0.58f, 0.62f, 1f), 0.4f);
            Material darkMat = LoadOrCreateMaterial("Assets/Settings/West_DarkZocalo.mat", new Color(0.25f, 0.26f, 0.28f, 1f), 0.1f);

            BridgeEntranceBuilder.CreateFideicomisoRearCamera(canopy.transform, whiteMat, steelMat, darkMat);
        }

        private static void RenderToPng(Camera cam, int w, int h, string path)
        {
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
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
