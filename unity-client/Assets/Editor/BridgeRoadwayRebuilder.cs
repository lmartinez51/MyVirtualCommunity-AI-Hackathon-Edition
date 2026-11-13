using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Rebuilds the Paso del Norte bridge roadway:
    /// 1. Disables the central yellow Sentri jersey barriers and all old bridge lane lines.
    /// 2. Distributes 4 lanes equitably across the clear bridge width (-9.50m to +9.80m relative to deck center).
    /// 3. Places dashed dividers (lanes 1|2 and 2|3), solid divider (lanes 3|4), edge lines, and forward lane arrows.
    /// 4. Places white delineator bollards along the 3|4 lane divider from the start of the fixed roof (Seg01, Z~68m)
    ///    to the end of the old yellow barrier (Seg28, Z~351m).
    /// </summary>
    public static class BridgeRoadwayRebuilder
    {
        private const string RootName = "Bridge_Roadway_4Lanes_Root";

        // Clear drivable width on Bridge_Deck_SegXX (scale.x = 20):
        // West parapet inner face = -9.50m; East yellow curb inner face = +9.80m -> 19.30m total width.
        private const float RoadWestEdge = -9.50f;
        private const float RoadEastEdge = 9.80f;
        private const float LaneWidth = (RoadEastEdge - RoadWestEdge) / 4f; // 4.825m per lane

        private const float Divider12X = RoadWestEdge + LaneWidth * 1f; // -4.675m
        private const float Divider23X = RoadWestEdge + LaneWidth * 2f; // +0.150m
        private const float Divider34X = RoadWestEdge + LaneWidth * 3f; // +4.975m (Solid line + White Bollards)

        private static readonly float[] LaneCentersX = new float[]
        {
            RoadWestEdge + LaneWidth * 0.5f, // -7.0875m (Lane 1 - Westmost)
            RoadWestEdge + LaneWidth * 1.5f, // -2.2625m (Lane 2)
            RoadWestEdge + LaneWidth * 2.5f, // +2.5625m (Lane 3)
            RoadWestEdge + LaneWidth * 3.5f  // +7.3875m (Lane 4 - Eastmost / SENTRI)
        };

        [MenuItem("MyVirtualCommunity/Rebuild Bridge Roadway 4 Lanes & Bollards")]
        public static void ExecuteRebuild()
        {
            Debug.Log("[BridgeRoadwayRebuilder] Starting 4-lane roadway & white bollards rebuild...");

            const string scenePath = "Assets/Scenes/BorderCorridorScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Clean up previous rebuild root if present
            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            // 2. Disable all existing yellow Sentri barriers and old bridge lane lines
            DisableLegacyBridgeBarriersAndLines();

            // 3. Load materials
            Material whiteStripeMat = LoadOrCreateMaterial(
                "Assets/Settings/WhiteStripeMaterial.mat",
                new Color(0.95f, 0.95f, 0.95f, 1f),
                0.2f);
            Material whiteBollardMat = LoadOrCreateMaterial(
                "Assets/Settings/WhiteMetalBarricade.mat",
                new Color(0.96f, 0.96f, 0.96f, 1f),
                0.4f);
            Material bollardBaseMat = LoadOrCreateMaterial(
                "Assets/Settings/AsphaltDark.mat",
                new Color(0.15f, 0.15f, 0.16f, 1f),
                0.2f);
            Material bollardReflectiveMat = LoadOrCreateMaterial(
                "Assets/Settings/Meter_GalvanizedSteel.mat",
                new Color(0.78f, 0.80f, 0.82f, 1f),
                0.65f);

            GameObject root = new GameObject(RootName);

            // 4. Build 4-lane markings on Bridge_Span_Deck (Z = 51m to 69m, top Y = 0.40m)
            GameObject spanDeck = GameObject.Find("Bridge_Span_Deck");
            if (spanDeck != null)
            {
                BuildSpanDeckMarkings(root.transform, spanDeck.transform, whiteStripeMat);
            }

            // 5. Build 4-lane markings + arrows + white bollards on Bridge_Deck_Seg01..28 (Z ~ 68m to 351m)
            BuildBridgeSegmentsMarkingsAndBollards(root.transform, whiteStripeMat, whiteBollardMat, bollardBaseMat, bollardReflectiveMat);

            // 6. Build 4-lane markings + arrows on Plaza_DescentRamp (Z = 350m to 420m)
            GameObject descentRamp = GameObject.Find("Plaza_DescentRamp");
            if (descentRamp != null)
            {
                BuildDescentRampMarkings(root.transform, descentRamp.transform, whiteStripeMat);
            }

            // 7. Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // 8. Capture verification screenshots
            CaptureVerificationScreenshots();

            Debug.Log("[BridgeRoadwayRebuilder] 4-LANE ROADWAY & WHITE BOLLARDS REBUILD FINISHED SUCCESSFULLY!");
        }

        private static void DisableLegacyBridgeBarriersAndLines()
        {
            int disabledBarriers = 0;
            int disabledLines = 0;

            string[] lineOffsets = new string[] { "-8", "-4", "+0", "+4", "+8" };
            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject barrier = GameObject.Find($"Bridge_SentriBarrier_Seg{seg:D2}");
                if (barrier != null)
                {
                    barrier.SetActive(false);
                    disabledBarriers++;
                }

                foreach (string offset in lineOffsets)
                {
                    GameObject line = GameObject.Find($"Bridge_LaneLine_{offset}_Seg{seg:D2}");
                    if (line != null)
                    {
                        line.SetActive(false);
                        disabledLines++;
                    }
                }
            }

            Debug.Log($"[BridgeRoadwayRebuilder] Disabled {disabledBarriers} yellow barrier segments and {disabledLines} old lane line segments.");
        }

        private static void BuildSpanDeckMarkings(Transform root, Transform spanDeck, Material whiteMat)
        {
            GameObject spanRoot = new GameObject("SpanDeck_Markings");
            spanRoot.transform.SetParent(root);

            float topOffset = spanDeck.localScale.y * 0.5f + 0.012f; // top Y = 0.412m
            // Bridge_Span_Deck spans Z = 51..69 (center Z = 60, len = 18).
            // Pedestrian zebra crosswalk sits at Z = 53.70..55.50 (local Z = -6.30..-4.50, center -5.40m),
            // aligned directly between the East accessible ramp exit and the West guardrail opening.
            // Split longitudinal lane lines around the crosswalk gap (Z = 53.40..55.80, local Z = -6.60..-4.20).
            float southLen = 2.40f;   // Z = 51.00..53.40 (local -9.00..-6.60)
            float southZ = -7.80f;
            float northLen = 12.20f;  // Z = 55.80..68.00 (local -4.20..+8.00)
            float northZ = 1.90f;

            // Edge lines + Solid divider 3|4 (South & North of crosswalk)
            CreateStripe(spanRoot.transform, "Span_EdgeLine_West_South", spanDeck, RoadWestEdge + 0.12f, topOffset, southZ, 0.12f, southLen, whiteMat);
            CreateStripe(spanRoot.transform, "Span_EdgeLine_West_North", spanDeck, RoadWestEdge + 0.12f, topOffset, northZ, 0.12f, northLen, whiteMat);
            CreateStripe(spanRoot.transform, "Span_EdgeLine_East_South", spanDeck, RoadEastEdge - 0.12f, topOffset, southZ, 0.12f, southLen, whiteMat);
            CreateStripe(spanRoot.transform, "Span_EdgeLine_East_North", spanDeck, RoadEastEdge - 0.12f, topOffset, northZ, 0.12f, northLen, whiteMat);
            CreateStripe(spanRoot.transform, "Span_Divider34_Solid_South", spanDeck, Divider34X, topOffset, southZ, 0.18f, southLen, whiteMat);
            CreateStripe(spanRoot.transform, "Span_Divider34_Solid_North", spanDeck, Divider34X, topOffset, northZ, 0.18f, northLen, whiteMat);

            // Dashed dividers 1|2 and 2|3 (clear of the crosswalk)
            CreateStripe(spanRoot.transform, "Span_Divider12_Dash_1", spanDeck, Divider12X, topOffset, -7.80f, 0.15f, 2.0f, whiteMat);
            CreateStripe(spanRoot.transform, "Span_Divider23_Dash_1", spanDeck, Divider23X, topOffset, -7.80f, 0.15f, 2.0f, whiteMat);
            float[] northDashOffsetsZ = new float[] { -0.5f, 5.5f }; // Z = 59.5, 65.5
            for (int i = 0; i < northDashOffsetsZ.Length; i++)
            {
                CreateStripe(spanRoot.transform, $"Span_Divider12_Dash_{i + 2}", spanDeck, Divider12X, topOffset, northDashOffsetsZ[i], 0.15f, 3.0f, whiteMat);
                CreateStripe(spanRoot.transform, $"Span_Divider23_Dash_{i + 2}", spanDeck, Divider23X, topOffset, northDashOffsetsZ[i], 0.15f, 3.0f, whiteMat);
            }

            // Pedestrian zebra crosswalk (alternating yellow & white stripes at Z = 54.60m, local Z = -5.40m)
            Material yellowCrosswalkMat = LoadOrCreateMaterial("Assets/Settings/CrosswalkYellowStripe.mat", new Color(0.86f, 0.68f, 0.16f), 0.2f);
            Material whiteCrosswalkMat = LoadOrCreateMaterial("Assets/Settings/CrosswalkWhiteStripe.mat", new Color(0.86f, 0.86f, 0.84f), 0.2f);
            float crosswalkZ = -5.40f;
            float stripeLenZ = 1.80f;
            float stripeWidthX = 0.42f;
            float stripeStepX = 0.86f;
            int barIdx = 0;
            for (float lx = RoadWestEdge + 0.15f; lx <= RoadEastEdge - 0.15f; lx += stripeStepX)
            {
                Material barMat = (barIdx % 2 == 0) ? yellowCrosswalkMat : whiteCrosswalkMat;
                CreateStripe(spanRoot.transform, $"Span_Crosswalk_Bar_{barIdx + 1}", spanDeck, lx, topOffset + 0.002f, crosswalkZ, stripeWidthX, stripeLenZ, barMat);
                barIdx++;
            }

            // First row of 4 forward arrows at Z = 61.0m (local Z = +1.0m)
            for (int lane = 0; lane < 4; lane++)
            {
                Vector3 arrowCenter = spanDeck.position + spanDeck.right * LaneCentersX[lane] + spanDeck.up * topOffset + spanDeck.forward * 1.0f;
                CreateLaneArrow(spanRoot.transform, $"Span_Arrow_Lane{lane + 1}", arrowCenter, spanDeck.rotation, whiteMat);
            }
        }

        private static void BuildBridgeSegmentsMarkingsAndBollards(
            Transform root,
            Material whiteStripeMat,
            Material whiteBollardMat,
            Material bollardBaseMat,
            Material bollardReflectiveMat)
        {
            GameObject segRoot = new GameObject("BridgeSegments_Markings_And_Bollards");
            segRoot.transform.SetParent(root);

            int[] arrowSegments = new int[] { 2, 6, 10, 14, 18, 22, 26 };

            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject deckGo = GameObject.Find($"Bridge_Deck_Seg{seg:D2}");
                if (deckGo == null) continue;

                Transform deck = deckGo.transform;
                float segLen = deck.localScale.z; // 12m
                float topOffset = deck.localScale.y * 0.5f + 0.012f; // 0.412m above deck center

                // Edge lines + Solid divider 3|4
                CreateStripe(segRoot.transform, $"Seg{seg:D2}_EdgeLine_West", deck, RoadWestEdge + 0.12f, topOffset, 0f, 0.12f, segLen, whiteStripeMat);
                CreateStripe(segRoot.transform, $"Seg{seg:D2}_EdgeLine_East", deck, RoadEastEdge - 0.12f, topOffset, 0f, 0.12f, segLen, whiteStripeMat);
                CreateStripe(segRoot.transform, $"Seg{seg:D2}_Divider34_Solid", deck, Divider34X, topOffset, 0f, 0.18f, segLen, whiteStripeMat);

                // Dashed dividers 1|2 and 2|3 (2 dashes per 12m segment)
                float[] dashZ = new float[] { -3.0f, 3.0f };
                for (int d = 0; d < dashZ.Length; d++)
                {
                    CreateStripe(segRoot.transform, $"Seg{seg:D2}_Divider12_Dash_{d + 1}", deck, Divider12X, topOffset, dashZ[d], 0.15f, 3.2f, whiteStripeMat);
                    CreateStripe(segRoot.transform, $"Seg{seg:D2}_Divider23_Dash_{d + 1}", deck, Divider23X, topOffset, dashZ[d], 0.15f, 3.2f, whiteStripeMat);
                }

                // Forward arrows on selected segments
                if (Array.IndexOf(arrowSegments, seg) >= 0)
                {
                    for (int lane = 0; lane < 4; lane++)
                    {
                        Vector3 arrowCenter = deck.position + deck.right * LaneCentersX[lane] + deck.up * topOffset;
                        CreateLaneArrow(segRoot.transform, $"Seg{seg:D2}_Arrow_Lane{lane + 1}", arrowCenter, deck.rotation, whiteStripeMat);
                    }
                }

                // White Delineator Bollards along Divider 3|4 from start of fixed roof (Seg01) to end of old yellow barrier (Seg28)
                // Place at -4.0m, 0.0m, +4.0m (every 4m) plus end caps at Seg01 (-5.8m) and Seg28 (+5.8m)
                if (seg == 1)
                {
                    Vector3 startPos = deck.position + deck.right * Divider34X + deck.up * (deck.localScale.y * 0.5f) + deck.forward * (-5.8f);
                    CreateWhiteBollard(segRoot.transform, "Bridge_Bollard_Start", startPos, deck.rotation, whiteBollardMat, bollardBaseMat, bollardReflectiveMat);
                }

                float[] bollardOffsets = new float[] { -4.0f, 0.0f, 4.0f };
                for (int b = 0; b < bollardOffsets.Length; b++)
                {
                    Vector3 bPos = deck.position + deck.right * Divider34X + deck.up * (deck.localScale.y * 0.5f) + deck.forward * bollardOffsets[b];
                    CreateWhiteBollard(segRoot.transform, $"Bridge_Bollard_Seg{seg:D2}_{b + 1}", bPos, deck.rotation, whiteBollardMat, bollardBaseMat, bollardReflectiveMat);
                }

                if (seg == 28)
                {
                    Vector3 endPos = deck.position + deck.right * Divider34X + deck.up * (deck.localScale.y * 0.5f) + deck.forward * (5.8f);
                    CreateWhiteBollard(segRoot.transform, "Bridge_Bollard_End", endPos, deck.rotation, whiteBollardMat, bollardBaseMat, bollardReflectiveMat);
                }
            }
        }

        private static void BuildDescentRampMarkings(Transform root, Transform ramp, Material whiteMat)
        {
            GameObject rampRoot = new GameObject("DescentRamp_Markings");
            rampRoot.transform.SetParent(root);

            float rampLen = ramp.localScale.z; // 70.1m
            float topOffset = ramp.localScale.y * 0.5f + 0.012f;

            CreateStripe(rampRoot.transform, "Ramp_EdgeLine_West", ramp, RoadWestEdge + 0.12f, topOffset, 0f, 0.12f, rampLen, whiteMat);
            CreateStripe(rampRoot.transform, "Ramp_EdgeLine_East", ramp, RoadEastEdge - 0.12f, topOffset, 0f, 0.12f, rampLen, whiteMat);
            CreateStripe(rampRoot.transform, "Ramp_Divider34_Solid", ramp, Divider34X, topOffset, 0f, 0.18f, rampLen, whiteMat);

            for (float zOff = -32.0f; zOff <= 32.0f; zOff += 6.0f)
            {
                CreateStripe(rampRoot.transform, $"Ramp_Divider12_Dash_{(int)(zOff + 40)}", ramp, Divider12X, topOffset, zOff, 0.15f, 3.2f, whiteMat);
                CreateStripe(rampRoot.transform, $"Ramp_Divider23_Dash_{(int)(zOff + 40)}", ramp, Divider23X, topOffset, zOff, 0.15f, 3.2f, whiteMat);
            }

            float[] rampArrowZ = new float[] { -18.0f, 12.0f };
            for (int i = 0; i < rampArrowZ.Length; i++)
            {
                for (int lane = 0; lane < 4; lane++)
                {
                    Vector3 arrowCenter = ramp.position + ramp.right * LaneCentersX[lane] + ramp.up * topOffset + ramp.forward * rampArrowZ[i];
                    CreateLaneArrow(rampRoot.transform, $"Ramp_Arrow_{i + 1}_Lane{lane + 1}", arrowCenter, ramp.rotation, whiteMat);
                }
            }
        }

        private static void CreateStripe(Transform parent, string name, Transform deck, float localRight, float localUp, float localForward, float width, float length, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = deck.position + deck.right * localRight + deck.up * localUp + deck.forward * localForward;
            go.transform.rotation = deck.rotation;
            go.transform.localScale = new Vector3(width, 0.02f, length);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            SetMaterial(go, mat);
        }

        private static void CreateLaneArrow(Transform parent, string name, Vector3 centerPos, Quaternion rot, Material mat)
        {
            GameObject arrowRoot = new GameObject(name);
            arrowRoot.transform.SetParent(parent);
            arrowRoot.transform.position = centerPos;
            arrowRoot.transform.rotation = rot;

            // Shaft pointing +forward (North)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(arrowRoot.transform);
            shaft.transform.localPosition = new Vector3(0f, 0f, -0.45f);
            shaft.transform.localRotation = Quaternion.identity;
            shaft.transform.localScale = new Vector3(0.24f, 0.02f, 2.50f);
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());
            SetMaterial(shaft, mat);

            // Left barb
            GameObject barbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barbL.name = "Barb_L";
            barbL.transform.SetParent(arrowRoot.transform);
            barbL.transform.localPosition = new Vector3(-0.34f, 0f, 0.72f);
            barbL.transform.localRotation = Quaternion.Euler(0f, 38f, 0f);
            barbL.transform.localScale = new Vector3(0.22f, 0.02f, 1.25f);
            UnityEngine.Object.DestroyImmediate(barbL.GetComponent<Collider>());
            SetMaterial(barbL, mat);

            // Right barb
            GameObject barbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barbR.name = "Barb_R";
            barbR.transform.SetParent(arrowRoot.transform);
            barbR.transform.localPosition = new Vector3(0.34f, 0f, 0.72f);
            barbR.transform.localRotation = Quaternion.Euler(0f, -38f, 0f);
            barbR.transform.localScale = new Vector3(0.22f, 0.02f, 1.25f);
            UnityEngine.Object.DestroyImmediate(barbR.GetComponent<Collider>());
            SetMaterial(barbR, mat);
        }

        private static void CreateWhiteBollard(
            Transform parent,
            string name,
            Vector3 deckSurfacePos,
            Quaternion deckRot,
            Material whiteMat,
            Material baseMat,
            Material reflectiveMat)
        {
            GameObject bollard = new GameObject(name);
            bollard.transform.SetParent(parent);
            bollard.transform.position = deckSurfacePos;
            bollard.transform.rotation = deckRot;

            // Black base plate
            GameObject basePad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePad.name = "Base";
            basePad.transform.SetParent(bollard.transform);
            basePad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            basePad.transform.localRotation = Quaternion.identity;
            basePad.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
            SetMaterial(basePad, baseMat);

            // White cylindrical post (0.86m tall)
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(bollard.transform);
            post.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            post.transform.localRotation = Quaternion.identity;
            post.transform.localScale = new Vector3(0.11f, 0.43f, 0.11f);
            SetMaterial(post, whiteMat);

            // Two reflective silver bands near top
            float[] bandHeights = new float[] { 0.68f, 0.80f };
            for (int i = 0; i < bandHeights.Length; i++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = $"ReflectiveBand_{i + 1}";
                band.transform.SetParent(bollard.transform);
                band.transform.localPosition = new Vector3(0f, bandHeights[i], 0f);
                band.transform.localRotation = Quaternion.identity;
                band.transform.localScale = new Vector3(0.115f, 0.03f, 0.115f);
                UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
                SetMaterial(band, reflectiveMat);
            }
        }

        private static void CaptureVerificationScreenshots()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempBridgeRoadwayCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;

            // Shot 1: Webcam-like elevated overview looking North along the 4 lanes & white bollards
            camGo.transform.position = new Vector3(4.00f, 9.50f, 48.00f);
            camGo.transform.rotation = Quaternion.Euler(14.0f, 1.5f, 0.0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_roadway_shot1_webcam_overview.png"));
            CopyFile(Path.Combine(outDir, "bridge_roadway_shot1_webcam_overview.png"),
                     Path.Combine(brainDir, "bridge_roadway_shot1_webcam_overview.png"));

            // Shot 2: Eye-level view from bridge entrance looking North at 4 lanes, arrows & bollards starting at fixed roof
            camGo.transform.position = new Vector3(6.50f, 2.20f, 54.00f);
            camGo.transform.rotation = Quaternion.Euler(4.0f, 0.0f, 0.0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_roadway_shot2_eyelevel_north.png"));
            CopyFile(Path.Combine(outDir, "bridge_roadway_shot2_eyelevel_north.png"),
                     Path.Combine(brainDir, "bridge_roadway_shot2_eyelevel_north.png"));

            // Shot 3: Mid-bridge view (near Seg14, Z~200m) verifying curve alignment of 4 lanes, arrows & bollards
            camGo.transform.position = new Vector3(6.00f, 10.50f, 185.00f);
            camGo.transform.rotation = Quaternion.Euler(12.0f, 3.0f, 0.0f);
            CaptureShot(cam, Path.Combine(outDir, "bridge_roadway_shot3_midbridge_curve.png"));
            CopyFile(Path.Combine(outDir, "bridge_roadway_shot3_midbridge_curve.png"),
                     Path.Combine(brainDir, "bridge_roadway_shot3_midbridge_curve.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
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
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        private static void CopyFile(string src, string dst)
        {
            try
            {
                if (File.Exists(src)) File.Copy(src, dst, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BridgeRoadwayRebuilder] Could not copy {src} to {dst}: {ex.Message}");
            }
        }
    }
}

