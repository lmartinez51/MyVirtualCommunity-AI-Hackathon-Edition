using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Fixes East queue NPC elevations on the sloped Z=34..54 sidewalk and trims Pedestrian_Queue_Canopy
    /// + its north support posts (Canopy_Support_Post_3 & 4) so they end cleanly before EastWalkway_ShortConcreteWall_4.5m (Z=49.50m).
    /// </summary>
    public static class EastQueueAndCanopyFix
    {
        [MenuItem("MyVirtualCommunity/Fix East Queue NPCs & Trim Queue Canopy")]
        public static void ExecuteFix()
        {
            const string scenePath = "Assets/Scenes/BorderCorridorScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Trim Pedestrian_Queue_Canopy to end at Z = 49.40m (just before ShortConcreteWall at Z = 49.50m)
            //    South end stays at Z = 39.00m -> center Z = 44.20m, length = 10.40m.
            GameObject canopy = GameObject.Find("Pedestrian_Queue_Canopy");
            if (canopy != null)
            {
                Vector3 p = canopy.transform.position;
                Vector3 s = canopy.transform.localScale;
                canopy.transform.position = new Vector3(p.x, p.y, 44.20f);
                canopy.transform.localScale = new Vector3(s.x, s.y, 10.40f);
                Debug.Log("[EastQueueAndCanopyFix] Trimmed Pedestrian_Queue_Canopy to Z = 39.00..49.40m.");
            }

            // 2. Move north support posts (Canopy_Support_Post_3 & 4) to Z = 49.25m (cleanly south of Z=49.50m wall)
            //    Floor at Z=49.25m is Y = 0.20 + (49.25 - 34)/20 * 0.20 = 0.3525m; canopy bottom is ~3.14m.
            float postZ = 49.25f;
            float floorY = GetSouthQueueFloorY(postZ);
            float postTopY = 3.20f;
            float halfH = (postTopY - floorY) * 0.5f;
            float centerY = floorY + halfH;

            foreach (string postName in new[] { "Canopy_Support_Post_3", "Canopy_Support_Post_4" })
            {
                GameObject post = GameObject.Find(postName);
                if (post != null)
                {
                    Vector3 p = post.transform.position;
                    Vector3 s = post.transform.localScale;
                    post.transform.position = new Vector3(p.x, centerY, postZ);
                    post.transform.localScale = new Vector3(s.x, halfH, s.z);
                    Debug.Log($"[EastQueueAndCanopyFix] Moved {postName} to Z={postZ}, Y={centerY:F3}.");
                }
            }

            // 3. Seat queue NPCs flush on the sloped Sidewalk_Plaza_South_Main_Z34_54 surface (Y=0.200m at Z=34m -> Y=0.520m at Z=54m)
            SeatNpcOnQueueFloor("Witness_MateoNavarro");
            SeatNpcOnQueueFloor("Witness_SofiaAlarcon");
            SeatNpcOnQueueFloor("Witness_DonHector");

            // 4. Re-seat Pedestrian_Queue_Area railings (Queue_Rail_L_* & Queue_Rail_R_*) to follow the sloped sidewalk
            GameObject queueArea = GameObject.Find("Pedestrian_Queue_Area");
            if (queueArea != null)
            {
                int railCount = 0;
                foreach (Transform child in queueArea.transform)
                {
                    if (child.name.StartsWith("Queue_Rail_L_") || child.name.StartsWith("Queue_Rail_R_"))
                    {
                        Vector3 rp = child.position;
                        float rFloorY = GetSouthQueueFloorY(rp.z);
                        child.position = new Vector3(rp.x, rFloorY + 0.30f, rp.z);
                        railCount++;
                    }
                }
                Debug.Log($"[EastQueueAndCanopyFix] Re-seated {railCount} queue railings to follow Z=34..54 slope (Y=0.200->0.520).");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            CaptureVerificationScreenshots();
            Debug.Log("[EastQueueAndCanopyFix] FINISHED SUCCESSFULLY!");
        }

        private static float GetSouthQueueFloorY(float z)
        {
            float t = Mathf.Clamp01((z - 34.00f) / 20.00f);
            return 0.200f + t * 0.320f;
        }

        private static void SeatNpcOnQueueFloor(string npcName)
        {
            GameObject npc = GameObject.Find(npcName);
            if (npc == null)
            {
                Debug.LogWarning($"[EastQueueAndCanopyFix] {npcName} not found.");
                return;
            }
            Vector3 p = npc.transform.position;
            float y = GetSouthQueueFloorY(p.z);
            npc.transform.position = new Vector3(p.x, y, p.z);
            Debug.Log($"[EastQueueAndCanopyFix] Seated {npcName} at ({p.x:F2}, {y:F3}, {p.z:F2}).");
        }

        private static void CaptureVerificationScreenshots()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempEastQueueCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;

            // Shot 1: Same angle as user screenshot showing NPCs standing cleanly on floor + canopy ending before short wall
            camGo.transform.position = new Vector3(15.20f, 1.75f, 40.00f);
            camGo.transform.rotation = Quaternion.Euler(8.0f, -12.0f, 0.0f);
            string p1 = Path.Combine(outDir, "east_queue_npcs_and_canopy_fixed.png");
            CaptureShot(cam, p1);
            try { File.Copy(p1, Path.Combine(brainDir, "east_queue_npcs_and_canopy_fixed.png"), true); }
            catch (Exception ex) { Debug.LogWarning(ex.Message); }

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void CaptureShot(Camera cam, string savePath)
        {
            int w = 1280; int h = 720;
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

        [MenuItem("MyVirtualCommunity/Fix Accessible Ramp Exit & Remove Guardrail Sign")]
        public static void FixAccessibleRampAndRemoveGuardrailSign()
        {
            const string scenePath = "Assets/Scenes/BorderCorridorScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Remove Sign_NoEstacionarse_Board from the 3-tube guardrail
            GameObject signBoard = GameObject.Find("Sign_NoEstacionarse_Board");
            if (signBoard != null)
            {
                UnityEngine.Object.DestroyImmediate(signBoard);
                Debug.Log("[EastQueueAndCanopyFix] Removed Sign_NoEstacionarse_Board.");
            }

            // 2. Make the final exit mouth (Z = 54.00m..55.20m) level at Y = 0.400m (exact street asphalt level)
            //    and slope smoothly from Z = 55.20m (Y = 0.400m) up to Z = 58.00m (Y = 0.520m).
            const float midY = 0.400f + (56.70f - 55.20f) / 2.80f * (0.520f - 0.400f); // 0.4642857f

            GameObject blueChute = GameObject.Find("Sidewalk_Ramp_BlueChute_Z54_56");
            if (blueChute != null)
            {
                Material blueMat = blueChute.GetComponent<MeshRenderer>().sharedMaterial;
                Transform parent = blueChute.transform.parent;

                GameObject oldLanding = GameObject.Find("Sidewalk_Ramp_BlueExitLanding_Z54_55");
                if (oldLanding != null) UnityEngine.Object.DestroyImmediate(oldLanding);

                CreateSlabMeshObject("Sidewalk_Ramp_BlueExitLanding_Z54_55", parent,
                    14.00f, 15.05f, 54.00f, 55.20f, 0.400f, 0.400f, 0.200f, blueMat);

                UpdateSlabMesh(blueChute, 14.00f, 15.05f, 55.20f, 56.70f, 0.400f, midY, 0.200f);
                Debug.Log("[EastQueueAndCanopyFix] Updated Sidewalk_Ramp_BlueExitLanding_Z54_55 (flat Y=0.400m) and Sidewalk_Ramp_BlueChute_Z54_56.");
            }

            GameObject northConcrete = GameObject.Find("Sidewalk_Ramp_NorthConcrete_Z56_58");
            if (northConcrete != null)
            {
                UpdateSlabMesh(northConcrete, 14.00f, 15.05f, 56.70f, 58.00f, midY, 0.520f, 0.200f);
            }

            GameObject exitLip = GameObject.Find("Sidewalk_Ramp_ExitLip_Z54_55");
            if (exitLip != null)
            {
                UpdateSlabMesh(exitLip, 13.85f, 14.00f, 54.00f, 55.20f, 0.400f, 0.400f, 0.350f);
                Debug.Log("[EastQueueAndCanopyFix] Flattened Sidewalk_Ramp_ExitLip_Z54_55 to Y=0.400m flush with street.");
            }

            GameObject curbSecA = GameObject.Find("Sidewalk_Bridge_YellowCurb_SecA_Ramp");
            if (curbSecA != null)
            {
                UpdateSlabMesh(curbSecA, 13.80f, 14.00f, 55.20f, 58.00f, 0.400f, 0.520f, 0.350f);
                Debug.Log("[EastQueueAndCanopyFix] Updated Sidewalk_Bridge_YellowCurb_SecA_Ramp to start at Y=0.400m at Z=55.20m.");
            }

            // 3. Re-seat interior yellow safety railing posts/spans along X=15.05m to match the updated ramp surface
            float[] yellowPostZ = new float[] { 54.00f, 55.35f, 56.70f };
            for (int i = 0; i < yellowPostZ.Length; i++)
            {
                float pFloorY = GetAccessibleRampY(yellowPostZ[i]);
                GameObject post = GameObject.Find($"YellowRail_Long_Post_{i + 1}");
                if (post != null) post.transform.position = new Vector3(15.05f, pFloorY + 0.525f, yellowPostZ[i]);
                GameObject basePl = GameObject.Find($"YellowRail_Long_Base_{i + 1}");
                if (basePl != null) basePl.transform.position = new Vector3(15.05f, pFloorY + 0.015f, yellowPostZ[i]);
            }

            float[] yellowRelHeights = new float[] { 0.15f, 0.75f, 0.95f };
            string[] yellowTubeNames = new string[] { "ToeRail_0.15m", "LowerHandrail_0.75m", "UpperHandrail_0.95m" };
            for (int s = 0; s < yellowPostZ.Length - 1; s++)
            {
                float zA = yellowPostZ[s], zB = yellowPostZ[s + 1];
                float yA = GetAccessibleRampY(zA), yB = GetAccessibleRampY(zB);
                for (int t = 0; t < 3; t++)
                {
                    GameObject span = GameObject.Find($"YellowRail_Long_Span_{s + 1}_{yellowTubeNames[t]}");
                    if (span != null)
                    {
                        Vector3 a = new Vector3(15.05f, yA + yellowRelHeights[t], zA);
                        Vector3 b = new Vector3(15.05f, yB + yellowRelHeights[t], zB);
                        Vector3 d = b - a;
                        span.transform.position = (a + b) * 0.5f;
                        span.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
                        Vector3 sc = span.transform.localScale;
                        span.transform.localScale = new Vector3(sc.x, d.magnitude * 0.5f, sc.z);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // Capture lateral verification screenshot matching the user's angle
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempRampVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.transform.position = new Vector3(10.50f, 1.35f, 55.30f);
            camGo.transform.LookAt(new Vector3(14.50f, 0.85f, 55.30f));

            string p1 = Path.Combine(outDir, "east_accessible_ramp_fixed.png");
            CaptureShot(cam, p1);
            try { File.Copy(p1, Path.Combine(brainDir, "east_accessible_ramp_fixed.png"), true); }
            catch (Exception ex) { Debug.LogWarning(ex.Message); }

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[EastQueueAndCanopyFix] FixAccessibleRampAndRemoveGuardrailSign FINISHED SUCCESSFULLY!");
        }

        private static float GetAccessibleRampY(float z)
        {
            if (z <= 55.20f) return 0.400f;
            if (z <= 58.00f) return 0.400f + (z - 55.20f) / 2.80f * (0.520f - 0.400f);
            return 0.520f;
        }

        private static void CreateSlabMeshObject(string name, Transform parent,
            float xMin, float xMax, float zMin, float zMax,
            float yStart, float yEnd, float yBottom, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            UpdateSlabMesh(go, xMin, xMax, zMin, zMax, yStart, yEnd, yBottom);
        }

        private static void UpdateSlabMesh(GameObject go,
            float xMin, float xMax, float zMin, float zMax,
            float yStart, float yEnd, float yBottom)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) return;
            Mesh mesh = new Mesh { name = $"{go.name}_Mesh" };
            Vector3 vNW = new Vector3(xMin, yEnd, zMax);
            Vector3 vNE = new Vector3(xMax, yEnd, zMax);
            Vector3 vSE = new Vector3(xMax, yStart, zMin);
            Vector3 vSW = new Vector3(xMin, yStart, zMin);
            Vector3 vbNW = new Vector3(xMin, yBottom, zMax);
            Vector3 vbNE = new Vector3(xMax, yBottom, zMax);
            Vector3 vbSE = new Vector3(xMax, yBottom, zMin);
            Vector3 vbSW = new Vector3(xMin, yBottom, zMin);

            mesh.vertices = new Vector3[]
            {
                vNW, vNE, vSE, vSW,
                vbSW, vbSE, vbNE, vbNW,
                vSW, vSE, vbSE, vbSW,
                vNE, vNW, vbNW, vbNE,
                vNW, vSW, vbSW, vbNW,
                vSE, vNE, vbNE, vbSE
            };
            int[] tris = new int[36];
            for (int f = 0; f < 6; f++)
            {
                int b = f * 4;
                tris[f * 6 + 0] = b; tris[f * 6 + 1] = b + 1; tris[f * 6 + 2] = b + 2;
                tris[f * 6 + 3] = b; tris[f * 6 + 4] = b + 2; tris[f * 6 + 5] = b + 3;
            }
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            var mc = go.GetComponent<MeshCollider>();
            if (mc != null) mc.sharedMesh = mesh;
        }
    }
}

