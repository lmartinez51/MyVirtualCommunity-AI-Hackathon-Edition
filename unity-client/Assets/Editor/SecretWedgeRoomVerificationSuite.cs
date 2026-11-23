using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity batchmode verification suite for the Building A South Bay
    /// semi-triangular empty interior & 6-digit electronic keypad door.
    /// </summary>
    public static class SecretWedgeRoomVerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public static void RunVerification()
        {
            int passed = 0;
            int total = 0;

            void Check(string label, bool condition)
            {
                total++;
                if (condition)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            Debug.Log("[SecretWedgeVerify] Starting Semi-Triangular Secret Room & 6-Digit Keypad Door Verification...");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Structural shell, idempotency, and preserved exterior bay
            SecretWedgeRoomBuilder.BuildIntoCurrentScene();
            SecretWedgeRoomBuilder.BuildIntoCurrentScene(); // Verify idempotency (zero duplicates!)
            Physics.SyncTransforms();

            GameObject upperWall = GameObject.Find(SecretWedgeRoomBuilder.UpperWallName);
            GameObject roomRoot = GameObject.Find(SecretWedgeRoomBuilder.RoomRootName);
            GameObject doorRoot = GameObject.Find(SecretWedgeRoomBuilder.DoorRootName);
            GameObject southWall = GameObject.Find("SecretRoom_SouthWall");
            GameObject northWall = GameObject.Find("SecretRoom_NorthWall");
            GameObject apexWall = GameObject.Find("SecretRoom_ApexWall");
            GameObject floor = GameObject.Find("SecretRoom_Floor");
            GameObject ceiling = GameObject.Find("SecretRoom_Ceiling");

            int roomRootCount = 0;
            int doorRootCount = 0;
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == SecretWedgeRoomBuilder.RoomRootName) roomRootCount++;
                if (t.name == SecretWedgeRoomBuilder.DoorRootName) doorRootCount++;
            }

            Check(
                $"Idempotent build: exactly 1 secret room root ({roomRootCount}), 1 door root ({doorRootCount}), upper wall & all 5 shell surfaces present",
                roomRootCount == 1 && doorRootCount == 1 &&
                upperWall != null && roomRoot != null && doorRoot != null &&
                southWall != null && northWall != null && apexWall != null &&
                floor != null && ceiling != null
            );

            // 2. Ceiling height matches tall gray structural frame (~2.76m clear height, Y=0.20..2.96m) and floor flush at Y=0.20m
            float floorTopY = floor != null ? floor.GetComponent<Collider>().bounds.max.y : -99f;
            float ceilingBotY = ceiling != null ? ceiling.GetComponent<Collider>().bounds.min.y : -99f;
            float clearHeight = ceilingBotY - floorTopY;
            GameObject lateralFlank = GameObject.Find("BuildingA_SouthLateralFlankWall");
            GameObject leftWhiteWall = GameObject.Find("Chamfer_LeftWhiteWall");
            Check(
                $"Floor flush with sidewalk (Y={floorTopY:F3}m), ceiling matches tall gray frame (ceilingBot={ceilingBotY:F3}m, clear={clearHeight:F2}m), & 2-story lateral flank wall + left white wall present",
                Mathf.Abs(floorTopY - 0.20f) < 0.01f &&
                clearHeight >= 2.65f && clearHeight <= 2.85f &&
                lateralFlank != null && leftWhiteWall != null
            );

            // 3. 4-sided right-trapezoid secret room footprint verified (flat front width 2.18m, left straight wall 1.60m, parallel back wall 0.80m, right diagonal wall 2.11m)
            float frontWidth = Vector2.Distance(SecretWedgeRoomBuilder.CornerSE, SecretWedgeRoomBuilder.CornerNE);
            float leftWallLen = Vector2.Distance(SecretWedgeRoomBuilder.CornerSE, SecretWedgeRoomBuilder.CornerSW);
            float backParallelLen = Vector2.Distance(SecretWedgeRoomBuilder.CornerSW, SecretWedgeRoomBuilder.CornerNW);
            float rightDiagLen = Vector2.Distance(SecretWedgeRoomBuilder.CornerNE, SecretWedgeRoomBuilder.CornerNW);
            bool backParallelToFront =
                Mathf.Abs(SecretWedgeRoomBuilder.CornerSE.x - SecretWedgeRoomBuilder.CornerNE.x) < 0.005f &&
                Mathf.Abs(SecretWedgeRoomBuilder.CornerSW.x - SecretWedgeRoomBuilder.CornerNW.x) < 0.005f;
            bool insideBay =
                SecretWedgeRoomBuilder.CornerSE.y >= SecretWedgeRoomBuilder.BaySouthZ &&
                SecretWedgeRoomBuilder.CornerNE.y <= SecretWedgeRoomBuilder.BayNorthZ + 0.05f;
            Check(
                $"4-sided secret room with parallel back wall verified (front={frontWidth:F2}m, left={leftWallLen:F2}m, backParallel={backParallelLen:F2}m, rightDiag={rightDiagLen:F2}m, parallel={backParallelToFront})",
                insideBay &&
                backParallelToFront &&
                frontWidth >= 2.10f && frontWidth <= 2.30f &&
                leftWallLen >= 1.50f && leftWallLen <= 1.75f &&
                backParallelLen >= 0.70f && backParallelLen <= 0.95f &&
                rightDiagLen >= 2.00f && rightDiagLen <= 2.25f
            );

            // 4. 3D Keypad lock + SecretKeypadDoorController wired with default 6-digit code "123456"
            SecretKeypadDoorController doorCtrl = doorRoot != null ? doorRoot.GetComponent<SecretKeypadDoorController>() : null;
            GameObject keypadHousing = GameObject.Find("Keypad_Housing");
            GameObject keypadLed = GameObject.Find("Keypad_StatusLED");
            Check(
                "3D Keypad lock (housing + LED) & SecretKeypadDoorController wired with default code '123456'",
                doorCtrl != null &&
                doorCtrl.activeAccessCode == "123456" &&
                doorCtrl.RequiredCodeLength == 6 &&
                doorCtrl.southLeafPivot != null &&
                doorCtrl.northLeafPivot != null &&
                doorCtrl.closedDoorBlockerCollider != null &&
                keypadHousing != null &&
                keypadLed != null
            );

            // 5. Locked state blocks entry & rejects wrong codes; correct code "123456" unlocks & swings leaves outward
            bool wrongRejected = doorCtrl != null && !doorCtrl.TrySubmitCode("000000") && !doorCtrl.IsUnlocked;
            bool blockerWhenLocked = doorCtrl != null && doorCtrl.closedDoorBlockerCollider.enabled;
            bool rightAccepted = doorCtrl != null && doorCtrl.TrySubmitCode("123456") && doorCtrl.IsUnlocked && doorCtrl.IsOpen;
            if (doorCtrl != null)
            {
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
            }
            bool openedCleanly = doorCtrl != null &&
                                 doorCtrl.OpenProgress > 0.98f &&
                                 !doorCtrl.closedDoorBlockerCollider.enabled &&
                                 Mathf.Abs(Mathf.DeltaAngle(doorCtrl.southLeafPivot.localEulerAngles.y, doorCtrl.openAngleDegrees)) < 1.5f &&
                                 Mathf.Abs(Mathf.DeltaAngle(doorCtrl.northLeafPivot.localEulerAngles.y, -doorCtrl.openAngleDegrees)) < 1.5f;
            Check(
                $"Keypad rejects wrong code, accepts '123456', disables doorway blocker, and swings both leaves outward ±{doorCtrl?.openAngleDegrees:F0}°",
                wrongRejected && blockerWhenLocked && rightAccepted && openedCleanly
            );

            // 6. Future daily random code hook works cleanly
            bool dailyCodeSet = doorCtrl != null &&
                                doorCtrl.SetAuthoritativeDailyCode("739204") &&
                                doorCtrl.activeAccessCode == "739204" &&
                                !doorCtrl.SetAuthoritativeDailyCode("12345"); // rejects 5-digit
            Check("Future daily random 6-digit code API (SetAuthoritativeDailyCode) validates & updates code", dailyCodeSet);

            // 7. Physical 0.40m-radius player capsule walkability from sidewalk through the 24.7° chamfer doorway into the wide semi-triangular room!
            Physics.SyncTransforms();
            float playerR = 0.40f;
            float pBotY = 0.20f + playerR + 0.05f;
            float pTopY = 0.20f + 1.80f - playerR - 0.05f;
            Vector3[] walkSamples = new Vector3[]
            {
                new Vector3(-11.20f, 0f, -11.99f), // Sidewalk outside open door
                new Vector3(-12.00f, 0f, -11.99f), // Threshold right through open doorway (X = -12.00m)
                new Vector3(-12.55f, 0f, -12.25f), // Stepping inside front of secret room
                new Vector3(-12.95f, 0f, -12.65f), // Center of secret room
                new Vector3(-13.10f, 0f, -12.90f)  // Deeper inside facing the straight parallel back wall
            };
            bool walkClear = true;
            foreach (Vector3 pt in walkSamples)
            {
                Collider[] hits = Physics.OverlapCapsule(
                    new Vector3(pt.x, pBotY, pt.z),
                    new Vector3(pt.x, pTopY, pt.z),
                    playerR,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider h in hits)
                {
                    if (h != null && h.enabled && !h.isTrigger)
                    {
                        Debug.LogError($"    Walk obstacle at {pt}: {h.gameObject.name}");
                        walkClear = false;
                    }
                }
            }
            Check("Player capsule (r=0.40m, h=1.80m) walks cleanly from sidewalk through open doorway into 4-sided secret room", walkClear);

            // 8. Ground check under player inside the room (zero void!)
            bool hasFloor = Physics.Raycast(new Vector3(-12.95f, 1.0f, -12.65f), Vector3.down, out RaycastHit floorHit, 2.0f) &&
                            floorHit.collider != null && floorHit.collider.gameObject.name == "SecretRoom_Floor";
            Check($"Solid interior floor grounded under player inside room (hit={floorHit.collider?.gameObject.name}, Y={floorHit.point.y:F3}m)", hasFloor);

            Debug.Log($"[SecretWedgeVerify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        public static void CaptureVerificationScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";

            // 0. Full South Block Panorama matching user's screenshot & video comparison!
            RenderView(
                new Vector3(-3.80f, 1.75f, -15.20f),
                new Vector3(-12.20f, 2.15f, -14.10f),
                58f,
                System.IO.Path.Combine(outDir, "south_block_streetview_match.png"));

            // 1. Video Frame 1 Angle: Close oblique looking NW at México Mágico protruding fuchsia fin + aligned mystery door false wall!
            RenderView(
                new Vector3(-9.40f, 1.62f, -15.35f),
                new Vector3(-12.10f, 1.95f, -12.95f),
                56f,
                System.IO.Path.Combine(outDir, "secret_wedge_door_closed_keypad.png"));

            // 2. Open door programmatically (in-memory only, do not save open state!) & capture doorway + 4-sided interior
            GameObject doorRoot = GameObject.Find(SecretWedgeRoomBuilder.DoorRootName);
            SecretKeypadDoorController doorCtrl = doorRoot != null ? doorRoot.GetComponent<SecretKeypadDoorController>() : null;
            if (doorCtrl != null)
            {
                doorCtrl.TrySubmitCode("123456");
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
            }

            RenderView(
                new Vector3(-9.75f, 1.55f, -12.15f),
                new Vector3(-13.66f, 1.35f, -12.75f),
                64f,
                System.IO.Path.Combine(outDir, "secret_wedge_door_open_entry.png"));

            RenderView(
                new Vector3(-12.05f, 1.50f, -12.45f),
                new Vector3(-13.66f, 1.38f, -12.82f),
                76f,
                System.IO.Path.Combine(outDir, "secret_wedge_interior_view.png"));

            Debug.Log("[SecretWedgeVerify] Captured all 4 verification screenshots cleanly.");
            EditorApplication.Exit(0);
        }

        private static void RenderView(Vector3 camPos, Vector3 lookAt, float fov, string outputPath)
        {
            GameObject camGo = new GameObject("TempVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 120f;

            const int w = 1280;
            const int h = 720;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render(); // Warm up URP first-frame shaders/lights cleanly
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(outputPath, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
