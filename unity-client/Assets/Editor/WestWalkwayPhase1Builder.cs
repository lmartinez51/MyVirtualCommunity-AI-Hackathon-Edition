using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Symmetrically mirrors the East pedestrian walkway onto the West side of the Paso del Norte bridge:
    /// 1. Disables blind West concrete parapets (Bridge_Barrier_West, Bridge_Parapet_West_Seg01..28, Plaza_RampParapet_West).
    /// 2. Builds continuous 2.5m-wide West walkway slabs from Z=53.0m through Seg01..28 and Plaza_DescentRamp.
    /// 3. Builds the yellow curb and 3-tube galvanized guardrail along the West roadside edge.
    /// 4. Builds the curved terracotta barrel-vault roof and white metallic Warren-truss lateral structure,
    ///    starting further South at Z=53.0m (matching the real-world webcam reference).
    /// </summary>
    public static class WestWalkwayPhase1Builder
    {
        private const string RootName = "WestWalkway_Symmetrical_Root";

        private const float HalfRoofWidth = 1.45f;
        private const float ArchRise = 0.48f;
        private const float RoofThickness = 0.08f;
        private const float EaveHeightAboveSlab = 2.50f;
        private const float MidRailHeightAboveSlab = 1.15f;
        private const float OuterTrussX = -1.05f; // Westmost edge of walkway
        private const float InnerTrussX = 1.05f;  // Roadside edge of walkway

        [MenuItem("MyVirtualCommunity/Build West Walkway Symmetrical (Roof + Truss + Guardrail)")]
        public static void ExecuteRebuild()
        {
            Debug.Log("[WestWalkwayPhase1Builder] Starting symmetrical West walkway rebuild...");

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

            // 2. Disable blind West concrete parapets & barrier
            DisableLegacyWestParapets();

            // 3. Load shared materials matching the East walkway
            Material concreteMat = LoadOrCreateMaterial("Assets/Settings/SidewalkPavement.mat", new Color(0.75f, 0.74f, 0.72f, 1f), 0.15f);
            Material yellowCurbMat = LoadOrCreateMaterial("Assets/Settings/YellowSafetyCurb.mat", new Color(0.95f, 0.76f, 0.05f, 1f), 0.35f);
            Material steelMat = LoadOrCreateMaterial("Assets/Settings/Meter_GalvanizedSteel.mat", new Color(0.76f, 0.78f, 0.80f, 1f), 0.65f);
            Material roofTerracottaMat = LoadOrCreateMaterial("Assets/Settings/Walkway_Roof_Terracotta.mat", new Color(0.70f, 0.37f, 0.31f, 1f), 0.30f);
            Material trussWhiteSteelMat = LoadOrCreateMaterial("Assets/Settings/Walkway_Truss_WhiteSteel.mat", new Color(0.84f, 0.86f, 0.88f, 1f), 0.45f);

            GameObject root = new GameObject(RootName);

            // 4. Create West walkway floor slabs (South approach Z=53..68 + Seg01..28 + North ramp)
            var slabs = BuildWestWalkwaySlabs(root.transform, concreteMat);

            // 5. Build yellow curb + 3-tube galvanized guardrail along the roadside (+right) edge
            BuildWestCurbAndGuardrail(root.transform, slabs, yellowCurbMat, steelMat);

            // 6. Build curved terracotta roof, Warren-truss lateral structure & outer fence starting at Z=53.0m
            for (int i = 0; i < slabs.Count; i++)
            {
                bool capNorthEnd = (i == slabs.Count - 1);
                BuildSegmentRoofAndTruss(root.transform, slabs[i], capNorthEnd, roofTerracottaMat, trussWhiteSteelMat);
            }

            // 7. Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // 8. Capture verification screenshots
            CaptureScreenshots();

            Debug.Log($"[WestWalkwayPhase1Builder] WEST WALKWAY REBUILD FINISHED SUCCESSFULLY ({slabs.Count} segments)!");
        }

        private static void DisableLegacyWestParapets()
        {
            int disabled = 0;

            string[] singles = new string[] { "Bridge_Barrier_West", "Plaza_RampParapet_West", "Bridge_Pedestrian_UpperWalkway" };
            foreach (string s in singles)
            {
                GameObject go = GameObject.Find(s);
                if (go != null)
                {
                    go.SetActive(false);
                    disabled++;
                }
            }

            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject go = GameObject.Find($"Bridge_Parapet_West_Seg{seg:D2}");
                if (go != null)
                {
                    go.SetActive(false);
                    disabled++;
                }
            }

            Debug.Log($"[WestWalkwayPhase1Builder] Disabled {disabled} legacy blind West parapet/barrier objects.");
        }

        private static List<Transform> BuildWestWalkwaySlabs(Transform root, Material concreteMat)
        {
            GameObject slabsRoot = new GameObject("WestWalkway_Slabs");
            slabsRoot.transform.SetParent(root);

            var slabs = new List<Transform>();

            // 0) Transition slab replacing Bridge_Pedestrian_UpperWalkway (Z = 42.20m to 53.00m):
            //    - At Z=42.20m: matches Step_3_YellowRiser (X = -6.725..-4.575, Y = 0.560m)
            //    - At Z=46.00m: widens West to X = -8.500 while clearing Pillar 1 (X = -4.575)
            //    - At Z=53.00m: tapers East edge to X = -6.000, Y = 0.486m (100% flush with WestWalkway_Slab_SouthExt_Z53_68)
            //    - Grounded down to Y = 0.150m (below Sidewalk_West_North_Section Y=0.200m)
            BuildTransitionSlabZ42_53(slabsRoot.transform, concreteMat);

            // A) South extended approach (Z = 53.0m to 68.018m, length = 15.018m)
            //    Starts further South than East fixed roof (Z=68m), matching the real webcam photo!
            GameObject southExt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southExt.name = "WestWalkway_Slab_SouthExt_Z53_68";
            southExt.transform.SetParent(slabsRoot.transform);
            southExt.transform.position = new Vector3(-7.25f, 0.411f, 60.509f);
            southExt.transform.rotation = Quaternion.identity;
            southExt.transform.localScale = new Vector3(2.50f, 0.15f, 15.018f);
            SetMaterial(southExt, concreteMat);
            slabs.Add(southExt.transform);

            // Grounding base under SouthExt down to Y = 0.150m so there is zero shadow/floating gap above Sidewalk_West_North_Section
            GameObject southExtBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southExtBase.name = "WestWalkway_SouthExt_Base_Z53_68";
            southExtBase.transform.SetParent(slabsRoot.transform);
            southExtBase.transform.position = new Vector3(-7.25f, 0.27f, 60.509f);
            southExtBase.transform.rotation = Quaternion.identity;
            southExtBase.transform.localScale = new Vector3(2.50f, 0.24f, 15.018f);
            SetMaterial(southExtBase, concreteMat);

            // B) Main bridge deck segments Seg01..28 (mirror of East Walkway_Slab_Seg01..28)
            for (int seg = 1; seg <= 28; seg++)
            {
                GameObject deckGo = GameObject.Find($"Bridge_Deck_Seg{seg:D2}");
                if (deckGo == null) continue;

                Transform deck = deckGo.transform;
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"WestWalkway_Slab_Seg{seg:D2}";
                slab.transform.SetParent(slabsRoot.transform);
                slab.transform.position = deck.position + deck.right * (-11.25f) + deck.up * 0.375f;
                slab.transform.rotation = deck.rotation;
                slab.transform.localScale = new Vector3(2.50f, 0.15f, deck.localScale.z);
                SetMaterial(slab, concreteMat);
                slabs.Add(slab.transform);
            }

            // C) North descent ramp (mirror of Walkway_Slab_Ramp along Plaza_DescentRamp)
            GameObject rampGo = GameObject.Find("Plaza_DescentRamp");
            if (rampGo != null)
            {
                Transform ramp = rampGo.transform;
                GameObject rampSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rampSlab.name = "WestWalkway_Slab_Ramp";
                rampSlab.transform.SetParent(slabsRoot.transform);
                rampSlab.transform.position = ramp.position + ramp.right * (-11.25f) + ramp.up * 0.225f;
                rampSlab.transform.rotation = ramp.rotation;
                rampSlab.transform.localScale = new Vector3(2.50f, 0.15f, ramp.localScale.z);
                SetMaterial(rampSlab, concreteMat);
                slabs.Add(rampSlab.transform);
            }

            return slabs;
        }

        private static void BuildWestCurbAndGuardrail(Transform root, List<Transform> slabs, Material yellowCurbMat, Material steelMat)
        {
            GameObject railRoot = new GameObject("WestWalkway_GuardrailAndCurb_Root");
            railRoot.transform.SetParent(root);

            var postAnchors = new List<Vector3>();
            float[] tubeRelHeights = new float[] { 0.35f, 0.70f, 1.05f };

            for (int i = 0; i < slabs.Count; i++)
            {
                Transform slab = slabs[i];
                float slabLen = slab.localScale.z;

                if (i == 0 && slab.name == "WestWalkway_Slab_SouthExt_Z53_68")
                {
                    // Split yellow curb around the crosswalk entrance opening (Z = 53.90m..55.30m)
                    // 1a. South curb piece (Z = 53.00m..53.90m)
                    GameObject curbSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    curbSouth.name = $"WestExt_YellowCurb_{slab.name}_South";
                    curbSouth.transform.SetParent(railRoot.transform);
                    curbSouth.transform.position = new Vector3(-5.90f, 0.336f, 53.45f);
                    curbSouth.transform.rotation = Quaternion.identity;
                    curbSouth.transform.localScale = new Vector3(0.20f, 0.30f, 0.90f);
                    SetMaterial(curbSouth, yellowCurbMat);

                    // 1b. Flush curb cut / vado at the crosswalk opening (Z = 53.90m..55.30m, top Y = 0.400m street level)
                    GameObject curbMouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    curbMouth.name = $"WestExt_YellowCurb_{slab.name}_CrosswalkMouth";
                    curbMouth.transform.SetParent(railRoot.transform);
                    curbMouth.transform.position = new Vector3(-5.90f, 0.380f, 54.60f);
                    curbMouth.transform.rotation = Quaternion.identity;
                    curbMouth.transform.localScale = new Vector3(0.20f, 0.04f, 1.40f);
                    SetMaterial(curbMouth, yellowCurbMat);

                    // 1c. North curb piece (Z = 55.30m..68.018m)
                    float northLen = 68.018f - 55.30f;
                    GameObject curbNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    curbNorth.name = $"WestExt_YellowCurb_{slab.name}_North";
                    curbNorth.transform.SetParent(railRoot.transform);
                    curbNorth.transform.position = new Vector3(-5.90f, 0.336f, 55.30f + northLen * 0.5f);
                    curbNorth.transform.rotation = Quaternion.identity;
                    curbNorth.transform.localScale = new Vector3(0.20f, 0.30f, northLen);
                    SetMaterial(curbNorth, yellowCurbMat);

                    // 2. Place posts on either side of the crosswalk opening (Z=53.20, 53.90, 55.30, then every 2m)
                    postAnchors.Add(new Vector3(-5.90f, 0.486f, 53.20f));
                    postAnchors.Add(new Vector3(-5.90f, 0.486f, 53.90f));
                    postAnchors.Add(new Vector3(-5.90f, 0.486f, 55.30f));
                    for (float pz = 57.30f; pz <= 67.85f; pz += 2.0f)
                    {
                        postAnchors.Add(new Vector3(-5.90f, 0.486f, pz));
                    }
                    continue;
                }

                // 1. Yellow curb flush with WestWalkway_Slab top (+0.075m) on the roadside (+1.35m right) edge
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = $"WestExt_YellowCurb_{slab.name}";
                curb.transform.SetParent(railRoot.transform);
                curb.transform.position = slab.position + slab.right * 1.35f + slab.up * (-0.075f);
                curb.transform.rotation = slab.rotation;
                curb.transform.localScale = new Vector3(0.20f, 0.30f, slabLen);
                SetMaterial(curb, yellowCurbMat);

                // 2. Sample post anchors every ~2.0m along the curb top surface
                float startMeters = -(slabLen * 0.5f) + (i == 0 ? 0.2f : 1.0f);
                float endMeters = (slabLen * 0.5f) - (i == slabs.Count - 1 ? 0.2f : 1.0f);
                for (float d = startMeters; d <= endMeters + 0.01f; d += 2.0f)
                {
                    Vector3 anchor = slab.position + slab.right * 1.35f + slab.up * 0.075f + slab.forward * d;
                    if (postAnchors.Count == 0 || Vector3.Distance(postAnchors[postAnchors.Count - 1], anchor) >= 1.2f)
                    {
                        postAnchors.Add(anchor);
                    }
                }
            }

            // 3. Instantiate posts, anchor plates, and 3-tube spans (leaving the Z=53.90..55.30m crosswalk opening clear)
            for (int i = 0; i < postAnchors.Count; i++)
            {
                Vector3 p = postAnchors[i];

                GameObject postGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postGo.name = $"WestExt_Post_{i + 1}";
                postGo.transform.SetParent(railRoot.transform);
                postGo.transform.position = p + Vector3.up * 0.575f;
                postGo.transform.localScale = new Vector3(0.07f, 0.575f, 0.07f);
                SetMaterial(postGo, steelMat);

                GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseGo.name = $"WestExt_AnchorPlate_{i + 1}";
                baseGo.transform.SetParent(railRoot.transform);
                baseGo.transform.position = p + Vector3.up * 0.015f;
                baseGo.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
                SetMaterial(baseGo, steelMat);

                if (i < postAnchors.Count - 1)
                {
                    Vector3 pNext = postAnchors[i + 1];
                    // Skip tube span across the West crosswalk entrance opening (Z = 53.90m..55.30m)
                    bool isCrosswalkOpening = Mathf.Abs(p.z - 53.90f) < 0.05f && Mathf.Abs(pNext.z - 55.30f) < 0.05f;
                    if (!isCrosswalkOpening)
                    {
                        for (int t = 0; t < 3; t++)
                        {
                            CreateSlopedTube($"WestExt_Tube_Span_{i + 1}_Level_{t + 1}", railRoot.transform,
                                p.x, p.y + tubeRelHeights[t], p.z,
                                pNext.x, pNext.y + tubeRelHeights[t], pNext.z,
                                0.05f, steelMat);
                        }
                    }
                }
            }
        }

        private static void BuildSegmentRoofAndTruss(
            Transform root,
            Transform slab,
            bool capNorthEnd,
            Material roofMat,
            Material steelMat)
        {
            float segLen = slab.localScale.z;
            float slabTopOffset = slab.localScale.y * 0.5f;

            GameObject segRoot = new GameObject($"WestStructure_{slab.name}");
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

            // 2. Outer West parapet fence (symmetrical to Walkway_Fence_SegXX on the East side)
            CreateBeam($"{slab.name}_OuterFence", segRoot.transform, slab, -1.15f, slabTopOffset + 0.55f, 0f, 0.10f, 1.10f, segLen, steelMat);

            // 3. Longitudinal beams (top eaves + mid-height rails)
            CreateBeam($"{slab.name}_OuterTopBeam", segRoot.transform, slab, OuterTrussX, slabTopOffset + EaveHeightAboveSlab, 0f, 0.10f, 0.12f, segLen, steelMat);
            CreateBeam($"{slab.name}_InnerTopBeam", segRoot.transform, slab, InnerTrussX, slabTopOffset + EaveHeightAboveSlab, 0f, 0.10f, 0.12f, segLen, steelMat);
            CreateBeam($"{slab.name}_OuterMidRail", segRoot.transform, slab, OuterTrussX, slabTopOffset + MidRailHeightAboveSlab, 0f, 0.07f, 0.07f, segLen, steelMat);
            CreateBeam($"{slab.name}_InnerMidRail", segRoot.transform, slab, InnerTrussX, slabTopOffset + MidRailHeightAboveSlab, 0f, 0.07f, 0.07f, segLen, steelMat);

            // 4. ~3.0m bays for vertical posts, diagonal truss struts & transverse arch ribs
            int bayCount = Mathf.Max(1, Mathf.RoundToInt(segLen / 3.0f));
            float bayLen = segLen / bayCount;
            float zStart = -segLen * 0.5f;

            for (int b = 0; b <= bayCount; b++)
            {
                if (b == bayCount && !capNorthEnd) continue;
                float z = zStart + b * bayLen;

                CreatePost($"{slab.name}_PostOut_{b}", segRoot.transform, slab, OuterTrussX, slabTopOffset, z, EaveHeightAboveSlab, 0.09f, steelMat);
                CreatePost($"{slab.name}_PostIn_{b}", segRoot.transform, slab, InnerTrussX, slabTopOffset, z, EaveHeightAboveSlab, 0.09f, steelMat);
                CreateTransverseArchRib($"{slab.name}_ArchRib_{b}", segRoot.transform, slab, slabTopOffset + EaveHeightAboveSlab, z, OuterTrussX, InnerTrussX, ArchRise, steelMat);
            }

            for (int b = 0; b < bayCount; b++)
            {
                float zA = zStart + b * bayLen;
                float zB = zA + bayLen;
                float zMid = (zA + zB) * 0.5f;

                Vector3 outTopA = GetWorldPoint(slab, OuterTrussX, slabTopOffset + EaveHeightAboveSlab, zA);
                Vector3 outMid = GetWorldPoint(slab, OuterTrussX, slabTopOffset + MidRailHeightAboveSlab, zMid);
                Vector3 outTopB = GetWorldPoint(slab, OuterTrussX, slabTopOffset + EaveHeightAboveSlab, zB);
                CreateStrut($"{slab.name}_TrussOut_{b}_A", segRoot.transform, outTopA, outMid, 0.055f, steelMat);
                CreateStrut($"{slab.name}_TrussOut_{b}_B", segRoot.transform, outMid, outTopB, 0.055f, steelMat);

                Vector3 inTopA = GetWorldPoint(slab, InnerTrussX, slabTopOffset + EaveHeightAboveSlab, zA);
                Vector3 inMid = GetWorldPoint(slab, InnerTrussX, slabTopOffset + MidRailHeightAboveSlab, zMid);
                Vector3 inTopB = GetWorldPoint(slab, InnerTrussX, slabTopOffset + EaveHeightAboveSlab, zB);
                CreateStrut($"{slab.name}_TrussIn_{b}_A", segRoot.transform, inTopA, inMid, 0.055f, steelMat);
                CreateStrut($"{slab.name}_TrussIn_{b}_B", segRoot.transform, inMid, inTopB, 0.055f, steelMat);
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

        private static void CreateSlopedTube(string name, Transform parent,
            float xA, float yA, float zA,
            float xB, float yB, float zB,
            float diameter, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);

            Vector3 start = new Vector3(xA, yA, zA);
            Vector3 end = new Vector3(xB, yB, zB);
            Vector3 dir = end - start;

            go.transform.position = (start + end) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(diameter, dir.magnitude * 0.5f, diameter);
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

                CreateStrut($"{name}_{s}", parent,
                    GetWorldPoint(slab, x0, y0, z),
                    GetWorldPoint(slab, x1, y1, z),
                    0.07f, mat);
            }
        }

        private static void CreateBarrelVaultRoof(
            string name, Transform parent, Vector3 origin, Quaternion rotation,
            float halfWidth, float archRise, float thickness, float length, Material mat)
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

            for (int i = 0; i < arcSegs; i++)
            {
                float u0 = (float)i / arcSegs;
                float u1 = (float)(i + 1) / arcSegs;
                float x0 = Mathf.Lerp(-halfWidth, halfWidth, u0);
                float x1 = Mathf.Lerp(-halfWidth, halfWidth, u1);
                float y0 = archRise * (1f - Mathf.Pow(2f * u0 - 1f, 2f));
                float y1 = archRise * (1f - Mathf.Pow(2f * u1 - 1f, 2f));

                AddQuad(verts, tris, uvs,
                    new Vector3(x0, y0 + thickness, zMax),
                    new Vector3(x1, y1 + thickness, zMax),
                    new Vector3(x1, y1 + thickness, zMin),
                    new Vector3(x0, y0 + thickness, zMin),
                    u0, u1);

                AddQuad(verts, tris, uvs,
                    new Vector3(x0, y0, zMin),
                    new Vector3(x1, y1, zMin),
                    new Vector3(x1, y1, zMax),
                    new Vector3(x0, y0, zMax),
                    u0, u1);
            }

            AddQuad(verts, tris, uvs,
                new Vector3(-halfWidth, thickness, zMax),
                new Vector3(-halfWidth, thickness, zMin),
                new Vector3(-halfWidth, 0f, zMin),
                new Vector3(-halfWidth, 0f, zMax),
                0f, 1f);

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
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float u0, float u1)
        {
            int idx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
            uvs.Add(new Vector2(u0, 1f)); uvs.Add(new Vector2(u1, 1f));
            uvs.Add(new Vector2(u1, 0f)); uvs.Add(new Vector2(u0, 0f));
        }

        private static void BuildTransitionSlabZ42_53(Transform parent, Material mat)
        {
            // Two smooth segments:
            //   1) Z = 42.20m -> 46.00m: West widens -6.725 -> -8.500, East stays -4.575 (clears Pillar 1), Y slopes 0.560 -> 0.534
            //   2) Z = 46.00m -> 53.00m: West stays -8.500, East tapers -4.575 -> -6.000, Y slopes 0.534 -> 0.486
            GameObject go = new GameObject("WestWalkway_TransitionSlab_Z42_53");
            go.transform.SetParent(parent);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            float[] zs = new float[] { 42.20f, 46.00f, 53.00f };
            float[] xW = new float[] { -6.725f, -8.500f, -8.500f };
            float[] xE = new float[] { -4.575f, -4.575f, -6.000f };
            float[] yTop = new float[] { 0.560f, 0.534f, 0.486f };
            const float yBot = 0.150f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i < 2; i++)
            {
                Vector3 sw = new Vector3(xW[i], yTop[i], zs[i]);
                Vector3 se = new Vector3(xE[i], yTop[i], zs[i]);
                Vector3 ne = new Vector3(xE[i + 1], yTop[i + 1], zs[i + 1]);
                Vector3 nw = new Vector3(xW[i + 1], yTop[i + 1], zs[i + 1]);

                Vector3 bsw = new Vector3(xW[i], yBot, zs[i]);
                Vector3 bse = new Vector3(xE[i], yBot, zs[i]);
                Vector3 bne = new Vector3(xE[i + 1], yBot, zs[i + 1]);
                Vector3 bnw = new Vector3(xW[i + 1], yBot, zs[i + 1]);

                // Top
                AddQuad(verts, tris, uvs, nw, ne, se, sw, 0f, 1f);
                // West side
                AddQuad(verts, tris, uvs, nw, sw, bsw, bnw, 0f, 1f);
                // East side
                AddQuad(verts, tris, uvs, se, ne, bne, bse, 0f, 1f);
                // Bottom
                AddQuad(verts, tris, uvs, bsw, bse, bne, bnw, 0f, 1f);

                if (i == 0)
                    AddQuad(verts, tris, uvs, sw, se, bse, bsw, 0f, 1f); // South end
                if (i == 1)
                    AddQuad(verts, tris, uvs, ne, nw, bnw, bne, 0f, 1f); // North end
            }

            Mesh mesh = new Mesh();
            mesh.name = "WestWalkway_TransitionSlab_Z42_53_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        private static void CaptureScreenshots()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "BridgeRoadway_Verification");
            string brainDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("TempWestWalkwayCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;

            // Shot 1: Webcam overview showing symmetrical West + East walkways & West roof starting further South
            camGo.transform.position = new Vector3(4.00f, 9.50f, 48.00f);
            camGo.transform.rotation = Quaternion.Euler(14.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "west_walkway_shot1_webcam_overview.png");

            // Shot 2: Lateral view from roadway looking at West curved roof, truss lattice, yellow curb & 3-tube guardrail
            camGo.transform.position = new Vector3(-1.50f, 2.50f, 48.00f);
            camGo.transform.rotation = Quaternion.Euler(2.0f, -18.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "west_walkway_shot2_lateral_south_start.png");

            // Shot 3: Inside West pedestrian walkway looking North from Z=54m
            camGo.transform.position = new Vector3(-7.25f, 2.10f, 54.00f);
            camGo.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "west_walkway_shot3_inside_walkway.png");

            // Shot 4: User angle 1 - Lower sidewalk next to Admin building looking N/NE at transition & entrance
            camGo.transform.position = new Vector3(-9.50f, 1.80f, 44.00f);
            camGo.transform.rotation = Quaternion.Euler(5.0f, 12.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "west_walkway_shot4_junction_from_west.png");

            // Shot 5: User angle 2 - South approach next to Pillar 1 looking NW at transition & entrance
            camGo.transform.position = new Vector3(-4.20f, 1.75f, 39.50f);
            camGo.transform.rotation = Quaternion.Euler(6.0f, -18.0f, 0.0f);
            CaptureAndCopy(cam, outDir, brainDir, "west_walkway_shot5_junction_from_southeast.png");

            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void CaptureAndCopy(Camera cam, string outDir, string brainDir, string fileName)
        {
            string localPath = Path.Combine(outDir, fileName);
            CaptureShot(cam, localPath);
            try { File.Copy(localPath, Path.Combine(brainDir, fileName), true); }
            catch (Exception ex) { Debug.LogWarning($"[WestWalkwayPhase1Builder] Copy warning: {ex.Message}"); }
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
    }
}

