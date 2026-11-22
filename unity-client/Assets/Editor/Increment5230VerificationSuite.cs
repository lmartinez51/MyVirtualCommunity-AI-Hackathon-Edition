using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity batchmode verification suite for Increment 5.23.0:
    /// Óptica Fashion Interior Reconstruction & Physical Store Accessibility.
    ///
    /// Covers:
    ///   1. Interior dimensions & structural envelope reconciliation
    ///   2. Faithful architectural layout & furniture uniqueness
    ///   3. Real physical accessibility (Physics.CheckCapsule & floor Raycasts along sidewalk -> open doorway ->
    ///      main showroom -> rear doorway -> Optica_CalibrationStation anchor)
    ///   4. Fixed glass collision & absence of monolithic obstructing glass panel
    ///   5. Idempotency (running OpticaFashionInteriorBuilder twice produces zero duplicates)
    ///   6. Regressions (Valeria external placement, Optica_MainBuilding collider, neighbors)
    /// </summary>
    public static class Increment5230VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const float NpcCapsuleRadius = 0.35f;
        private const float NpcCapsuleHeight = 1.75f;

        [MenuItem("MyVirtualCommunity/Increment 5.23.0/Run Verification Suite")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5230Verify] Starting Increment 5.23.0 Óptica Fashion Interior & Accessibility Verification...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5230Verify] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            Physics.SyncTransforms();

            int passed = 0;
            int total = 0;
            void Check(string label, bool ok)
            {
                total++;
                if (ok)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            int CountByName(string objName)
            {
                int count = 0;
                foreach (var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (tr.name == objName) count++;
                }
                return count;
            }

            // -------------------------------------------------------------------------
            // 1. INTERIOR DIMENSIONS & STRUCTURAL ENVELOPE
            // -------------------------------------------------------------------------
            GameObject floor = GameObject.Find("Optica_InteriorFloor");
            GameObject ceiling = GameObject.Find("Optica_InteriorCeiling");
            GameObject southWall = GameObject.Find("Optica_Int_SouthWall");
            GameObject northWall = GameObject.Find("Optica_Int_NorthWall");
            GameObject deepBackWall = GameObject.Find("Optica_Int_DeepBackWall");
            GameObject rearPartition = GameObject.Find("Optica_InteriorBackWall");

            bool dimsOk = false;
            if (floor != null && ceiling != null && southWall != null && northWall != null && deepBackWall != null && rearPartition != null)
            {
                Bounds fb = floor.GetComponent<Renderer>().bounds;
                Bounds cb = ceiling.GetComponent<Renderer>().bounds;
                Bounds swb = southWall.GetComponent<Renderer>().bounds;
                Bounds nwb = northWall.GetComponent<Renderer>().bounds;
                Bounds db = deepBackWall.GetComponent<Renderer>().bounds;

                float interiorWidth = nwb.min.z - swb.max.z;     // ~4.75m
                float totalDepth = OpticaFashionInteriorBuilder.StorefrontX - db.max.x; // ~7.11m
                float clearHeight = cb.min.y - fb.max.y;         // ~2.84m
                float floorTopY = fb.max.y;                      // ~0.20m

                Debug.Log($"[Increment5230Verify] Measured interior: width={interiorWidth:F2}m, totalDepth={totalDepth:F2}m, clearHeight={clearHeight:F2}m, floorTopY={floorTopY:F3}m");
                dimsOk = interiorWidth >= 4.60f && interiorWidth <= 4.95f &&
                         totalDepth >= 6.50f && totalDepth <= 7.50f &&
                         clearHeight >= 2.75f && clearHeight <= 3.15f &&
                         Mathf.Abs(floorTopY - 0.20f) < 0.02f;
            }
            Check("Interior dimensions reconcile with envelope (width ~4.85m, total depth ~7.1m, clear height ~2.84m, floor Y=0.20m)", dimsOk);

            GameObject optMain = GameObject.Find("Optica_MainBuilding");
            bool envelopeOk = optMain != null &&
                optMain.GetComponent<Collider>() != null &&
                optMain.TryGetComponent<Renderer>(out Renderer omr) &&
                omr.bounds.max.x <= -19.15f &&
                Mathf.Abs(omr.bounds.max.z - 3.40f) < 0.10f &&
                GameObject.Find("Optica_MainBuilding_SouthWing")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_MainBuilding_NorthPier")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_MainBuilding_UpperAboveStore")?.GetComponent<Collider>() != null;
            Check("Hollow cavity carved cleanly out of Optica_MainBuilding preserving exterior envelope & colliders", envelopeOk);

            // -------------------------------------------------------------------------
            // 2. ARCHITECTURAL LAYOUT & FURNITURE UNIQUENESS
            // -------------------------------------------------------------------------
            Check("Entrance accessories present exactly once (doormat, red fire extinguisher, gray electrical panel, front display rack)",
                CountByName("Optica_InteriorDoormat") == 1 &&
                CountByName("Optica_FireExtinguisher") == 1 &&
                CountByName("Optica_ElectricalPanel") == 1 &&
                CountByName("Optica_FrontGlassDisplayRack") == 1);

            GameObject longCounter = GameObject.Find("Optica_LongCounter_Base");
            bool counterOk = CountByName("Optica_LongCounter_Base") == 1 &&
                CountByName("Optica_LongCounter_GlassTop") == 1 &&
                CountByName("Optica_TableMirror_Disc") == 1 &&
                longCounter != null &&
                longCounter.GetComponent<Collider>() != null &&
                longCounter.TryGetComponent<Renderer>(out Renderer cr) &&
                cr.bounds.size.x >= 2.80f && cr.bounds.center.z < 0f; // Located on photographic left (South, -Z)
            Check("Long left/south glass display counter present once (~3.0m long, -Z side) with collider & fitting mirror", counterOk);

            Check("Right/north waiting area contains 2 blue chairs, glass side table, catalog, corner bin & plant",
                CountByName("Optica_WaitingChair_0") == 1 &&
                CountByName("Optica_WaitingChair_1") == 1 &&
                CountByName("Optica_WaitingSideTable") == 1 &&
                CountByName("Optica_TableCatalog") == 1 &&
                CountByName("Optica_CornerBin") == 1 &&
                CountByName("Optica_CornerPlant") == 1);

            Check("Deep right/north wall eyewear display shelving present once",
                CountByName("Optica_NorthWall_EyewearDisplay") == 1 &&
                GameObject.Find("NorthRack_Backing")?.GetComponent<Collider>() != null);

            Check("Rear wall composition present (diamond mirror, dark burgundy cabinet, framed open rear-right doorway)",
                CountByName("Optica_RearDiamondMirror") == 1 &&
                CountByName("Optica_RearBurgundyCabinet") == 1 &&
                CountByName("Optica_RearDoorFrame_South") == 1 &&
                CountByName("Optica_RearDoorFrame_North") == 1);

            Check("Rear consultation/calibration room furnished (workbench, lensometer, lens trial case, Snellen chart, calibration anchor)",
                CountByName("Optica_CalibrationWorkbench") == 1 &&
                CountByName("Optica_LensometerInstrument") == 1 &&
                CountByName("Optica_LensTrialCase") == 1 &&
                CountByName("Optica_SnellenChart") == 1 &&
                CountByName(OpticaFashionInteriorBuilder.CalibrationAnchorName) == 1);

            Check("Interior ceiling fluorescent fixtures & 2 lightweight retail lights present (plus exterior tube preserved)",
                CountByName("Optica_Fluorescent_MainFront") == 1 &&
                CountByName("Optica_Fluorescent_MainRear") == 1 &&
                CountByName("Optica_Fluorescent_RearRoom") == 1 &&
                CountByName("Optica_MainStore_Light") == 1 &&
                CountByName("Optica_RearRoom_Light") == 1 &&
                CountByName("Optica_EntryFluorescentTube") == 1);

            // -------------------------------------------------------------------------
            // 3. PHYSICAL ACCESSIBILITY, CLEAR DOORWAYS & COLLIDER VALIDATION
            // -------------------------------------------------------------------------
            // Verify monolithic blocking glass is GONE and fixed glass panels REMAIN collidable
            bool glassSegOk = GameObject.Find("Optica_GlassPane_Full") == null &&
                GameObject.Find("Optica_Glass_SouthSidelight")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_Glass_NorthSidelight")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_DoorLeaf_South_Glass")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_Glass_TopTransom")?.GetComponent<Collider>() != null &&
                GameObject.Find("Optica_DoorLeaf_North_Pivot") != null;
            Check("Monolithic obstructing glass removed; fixed glass panels remain collidable & north door leaf is hinged", glassSegOk);

            // Verify open storefront doorway clear width >= 0.90m
            GameObject centerMullion = GameObject.Find("Optica_Frame_Mullion_2");
            GameObject openDoorGlass = GameObject.Find("Optica_DoorLeaf_North_Glass");
            float clearFrontDoorWidth = 0f;
            if (centerMullion != null && openDoorGlass != null)
            {
                float southEdgeZ = centerMullion.GetComponent<Renderer>().bounds.max.z;
                float northEdgeZ = openDoorGlass.GetComponent<Renderer>().bounds.min.z;
                clearFrontDoorWidth = northEdgeZ - southEdgeZ;
                Debug.Log($"[Increment5230Verify] Open storefront doorway clear Z width = {clearFrontDoorWidth:F3}m (southEdge={southEdgeZ:F3}, northEdge={northEdgeZ:F3})");
            }
            Check($"Open storefront doorway clear width >= 0.90m (actual {clearFrontDoorWidth:F2}m)", clearFrontDoorWidth >= 0.90f);

            // Ensure dynamic shutter is open during walkability checks (simulating open business hours)
            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);
            Physics.SyncTransforms();

            // Physics capsule walkability check along the entire route from sidewalk -> open doorway ->
            // main retail aisle -> rear consultation doorway -> Optica_CalibrationStation standing anchor!
            bool IsCapsuleClearAndGrounded(Vector3 footPos, string label)
            {
                Vector3 p0 = footPos + Vector3.up * (NpcCapsuleRadius + 0.08f);
                Vector3 p1 = footPos + Vector3.up * (NpcCapsuleHeight - NpcCapsuleRadius);
                Collider[] hits = Physics.OverlapCapsule(p0, p1, NpcCapsuleRadius - 0.01f);
                bool blocked = false;
                foreach (var c in hits)
                {
                    if (c.gameObject.name == "Optica_InteriorFloor" || c.gameObject.name == "Sidewalk_West") continue;
                    Debug.LogError($"    [BLOCKED at {label} {footPos}] overlapped '{c.gameObject.name}'");
                    blocked = true;
                }
                bool hasFloor = Physics.Raycast(footPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.0f) &&
                                Mathf.Abs(hit.point.y - 0.20f) < 0.04f;
                if (!hasFloor)
                {
                    Debug.LogError($"    [NO FLOOR at {label} {footPos}]");
                }
                return !blocked && hasFloor;
            }

            Vector3[] walkPath = new Vector3[]
            {
                new Vector3(-11.20f, 0.20f, 1.20f), // Sidewalk outside open doorway
                new Vector3(-12.02f, 0.20f, 1.20f), // Directly inside open storefront doorway threshold
                new Vector3(-13.20f, 0.20f, 1.00f), // Main store entrance clear aisle
                new Vector3(-14.70f, 0.20f, 0.90f), // Mid main showroom aisle beside long glass counter & chairs
                new Vector3(-15.70f, 0.20f, 1.65f), // Approaching rear consultation doorway
                new Vector3(-16.88f, 0.20f, 2.00f), // Directly through open rear doorway
                new Vector3(-17.85f, 0.20f, 2.00f), // Inside rear room north entry
                new Vector3(-17.85f, 0.20f, 0.80f), // Inside rear room moving south
                OpticaFashionInteriorBuilder.CalibrationStationWorldPos // Standing anchor facing lensometer!
            };

            bool fullPathWalkable = true;
            for (int i = 0; i < walkPath.Length; i++)
            {
                if (!IsCapsuleClearAndGrounded(walkPath[i], $"Step_{i}"))
                {
                    fullPathWalkable = false;
                }
            }
            Check("Continuous walkable floor & unobstructed 0.35m-radius capsule path from sidewalk -> main store -> rear room -> calibration station", fullPathWalkable);

            GameObject anchorGo = GameObject.Find(OpticaFashionInteriorBuilder.CalibrationAnchorName);
            bool anchorOk = anchorGo != null &&
                Vector3.Distance(anchorGo.transform.position, OpticaFashionInteriorBuilder.CalibrationStationWorldPos) < 0.02f &&
                IsCapsuleClearAndGrounded(anchorGo.transform.position, "CalibrationAnchor");
            Check($"Calibration standing anchor '{OpticaFashionInteriorBuilder.CalibrationAnchorName}' at {OpticaFashionInteriorBuilder.CalibrationStationWorldPos} clear of all furniture", anchorOk);

            // -------------------------------------------------------------------------
            // 4. IDEMPOTENCY TEST (Rebuild twice in memory and assert zero duplicates)
            // -------------------------------------------------------------------------
            Transform blockArchTr = GameObject.Find("WestBlock_HistoricArchitecture")?.transform;
            Transform opticaRootTr = GameObject.Find("Storefront_OpticaFashion")?.transform;
            int childCountBefore = blockArchTr != null ? blockArchTr.childCount : -1;
            if (blockArchTr != null && opticaRootTr != null)
            {
                OpticaFashionInteriorBuilder.BuildOpticaFashionInteriorAndStorefront(blockArchTr, opticaRootTr);
                OpticaFashionInteriorBuilder.BuildOpticaFashionInteriorAndStorefront(blockArchTr, opticaRootTr);
            }
            bool idempotentOk = blockArchTr != null &&
                blockArchTr.childCount == childCountBefore &&
                CountByName(OpticaFashionInteriorBuilder.InteriorRootName) == 1 &&
                CountByName("Optica_LongCounter_Base") == 1 &&
                CountByName("Optica_MainStore_Light") == 1 &&
                CountByName(OpticaFashionInteriorBuilder.CalibrationAnchorName) == 1;
            Check("Idempotency verified: repeated interior builds produce zero duplicate walls, lights, furniture, or anchors", idempotentOk);

            // -------------------------------------------------------------------------
            // 5. REGRESSION CHECKS (Valeria external anchor, neighbors, signs)
            // -------------------------------------------------------------------------
            Transform valeria = GameObject.Find("ValeriaDominguez_Attendant")?.transform;
            bool regressionOk = valeria != null &&
                Vector3.Distance(valeria.position, new Vector3(-10.5f, 0.20f, 1.5f)) < 0.05f &&
                GameObject.Find("Optica_Signboard") != null &&
                GameObject.Find("Optica_Awning") != null &&
                GameObject.Find("Sign_Pharmacy") != null &&
                GameObject.Find("Sign_SantaFe") != null &&
                GameObject.Find("CambiosSantaFe_ExchangeLedSign") != null &&
                GameObject.Find("Sign_DentalMexico") != null;
            Check("Regressions clean: Valeria preserved at external position (-10.5, 0.2, 1.5) & neighboring storefronts intact", regressionOk);

            Debug.Log($"[Increment5230Verify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        [MenuItem("MyVirtualCommunity/Increment 5.23.0/Capture Interior Verification Screenshots")]
        public static void CaptureInteriorVerificationScreenshots()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            const string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);

            GameObject camGo = new GameObject("Optica5230VerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.Skybox;

            try
            {
                // Shot 1: Exterior Sidewalk Approach looking through the open inward-swinging north door into the full showroom!
                cam.fieldOfView = 52f;
                cam.transform.position = new Vector3(-7.4f, 1.65f, 0.90f);
                cam.transform.LookAt(new Vector3(-14.5f, 1.55f, 0.55f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_5230_ext_open_door.png")); // URP warmup
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_5230_ext_open_door.png"));

                // Shot 2: Standing inside the doorway looking down the main retail aisle (left long glass display counter, rear diamond mirror, burgundy cabinet, rear doorway, ceiling fluorescents!)
                cam.fieldOfView = 62f;
                cam.transform.position = new Vector3(-12.35f, 1.65f, 1.25f);
                cam.transform.LookAt(new Vector3(-16.80f, 1.35f, 0.20f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_5230_main_showroom.png"));

                // Shot 3: Looking across main showroom toward the Right/North waiting area (2 cobalt-blue chairs, glass table + catalog, deep right eyewear shelving, & rear doorway!)
                cam.fieldOfView = 64f;
                cam.transform.position = new Vector3(-13.30f, 1.65f, -0.05f);
                cam.transform.LookAt(new Vector3(-15.50f, 1.25f, 2.55f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_5230_waiting_and_right.png"));

                // Shot 4: Standing inside the Rear Consultation / Calibration Room looking at the calibration workbench, lensometer, trial case, task lamp & Snellen chart!
                cam.fieldOfView = 64f;
                cam.transform.position = new Vector3(-17.18f, 1.65f, 1.95f);
                cam.transform.LookAt(new Vector3(-18.75f, 1.25f, -0.25f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_5230_rear_calibration_room.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static void RenderToFile(Camera cam, int width, int height, string savePath)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(savePath, tex.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[Increment5230Verify] Saved screenshot: {savePath}");
        }
    }
}
