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

            // 5. Locked state blocks entry & rejects wrong codes; correct code "123456" unlocks & swings leaves outward; closing relocks cleanly
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

            // Verify closing relocks the door and re-enables the blocker collider (cannot reopen via ToggleDoorOpen without code)
            bool relockedOnClose = false;
            if (doorCtrl != null)
            {
                doorCtrl.ToggleDoorOpen();
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
                bool stayedLockedAfterToggle = !doorCtrl.IsUnlocked && !doorCtrl.IsOpen && doorCtrl.OpenProgress < 0.02f && doorCtrl.closedDoorBlockerCollider.enabled;
                doorCtrl.ToggleDoorOpen(); // Should NOT open because door is relocked!
                relockedOnClose = stayedLockedAfterToggle && !doorCtrl.IsUnlocked && !doorCtrl.IsOpen;

                // Re-open with valid code so walkability check #7 runs with open doorway
                doorCtrl.TrySubmitCode("123456");
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
            }

            Check(
                $"Keypad rejects wrong code, accepts '123456', swings leaves ±{doorCtrl?.openAngleDegrees:F0}°, and relocks automatically on close",
                wrongRejected && blockerWhenLocked && rightAccepted && openedCleanly && relockedOnClose
            );

            // 6. Future daily random code hook works cleanly
            bool dailyCodeSet = doorCtrl != null &&
                                doorCtrl.SetAuthoritativeDailyCode("739204") &&
                                doorCtrl.activeAccessCode == "739204" &&
                                !doorCtrl.SetAuthoritativeDailyCode("12345"); // rejects 5-digit
            Check("Future daily random 6-digit code API (SetAuthoritativeDailyCode) validates & updates code", dailyCodeSet);

            // 7. Continuous walkability using real effective CharacterController radius (0.28m + 0.04m skinWidth = 0.32m)
            //    PLUS verify that the open double metal doors have solid non-trigger BoxColliders so players collide with them!
            Physics.SyncTransforms();
            GameObject southLeaf = GameObject.Find("StorageDoor_Leaf_South");
            GameObject northLeaf = GameObject.Find("StorageDoor_Leaf_North");
            BoxCollider southLeafCol = southLeaf != null ? southLeaf.GetComponent<BoxCollider>() : null;
            BoxCollider northLeafCol = northLeaf != null ? northLeaf.GetComponent<BoxCollider>() : null;
            bool openDoorsSolid = southLeafCol != null && southLeafCol.enabled && !southLeafCol.isTrigger &&
                                  northLeafCol != null && northLeafCol.enabled && !northLeafCol.isTrigger;

            float playerR = 0.32f; // Matches FirstPersonPlayer CharacterController (0.28m radius + 0.04m skinWidth)
            float pBotY = 0.20f + playerR + 0.05f;
            float pTopY = 0.20f + 1.80f - playerR - 0.05f;
            Vector3[] walkSamples = new Vector3[]
            {
                new Vector3(-11.20f, 0f, -11.99f), // Sidewalk outside open door
                new Vector3(-11.60f, 0f, -11.99f), // Between the two solid open door leaves (Z = -12.62..-11.36m)
                new Vector3(-12.00f, 0f, -11.99f), // Doorway threshold (X = -12.00m)
                new Vector3(-12.25f, 0f, -12.10f), // Stepping past threshold cleanly
                new Vector3(-12.50f, 0f, -12.30f), // Inside front-center of secret room
                new Vector3(-12.75f, 0f, -12.55f), // Center of secret room
                new Vector3(-12.95f, 0f, -12.75f), // Approaching containment rack & safe
                new Vector3(-13.20f, 0f, -12.95f), // Right in front of back-wall Dimensional Portal & Octagonal Socket
                new Vector3(-12.50f, 0f, -12.95f)  // Standing right in front of the Level-Omega Vault Safe
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
            // Verify continuous 3-lane capsule sweep (left Z=-12.24, center Z=-11.99, right/keypad Z=-11.74) reaches past veilTriggerX (-11.90m)!
            SecretBackroomsTransitionController trCtrlCheck = UnityEngine.Object.FindFirstObjectByType<SecretBackroomsTransitionController>();
            float trigX = trCtrlCheck != null ? trCtrlCheck.veilTriggerX : -11.90f;
            float[] sweepLanesZ = new float[] { -12.24f, -11.99f, -11.74f };
            bool allLanesCrossVeil = true;
            foreach (float lz in sweepLanesZ)
            {
                Vector3 sp1 = new Vector3(-11.20f, pBotY, lz);
                Vector3 sp2 = new Vector3(-11.20f, pTopY, lz);
                float reachedX = -13.70f;
                if (Physics.CapsuleCast(sp1, sp2, playerR, Vector3.left, out RaycastHit sHit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    reachedX = -11.20f - sHit.distance;
                }
                if (reachedX >= trigX - 0.05f)
                {
                    Debug.LogError($"    Lane Z={lz:F2} stopped at X={reachedX:F3} before veilTriggerX={trigX:F2}!");
                    allLanesCrossVeil = false;
                }
            }
            Check("Open double doors have solid colliders AND player capsule (eff r=0.32m) cleanly crosses veilTriggerX across left, center & keypad lanes", openDoorsSolid && walkClear && allLanesCrossVeil);

            // 8. Ground check under player inside the room (zero void!)
            bool hasFloor = Physics.Raycast(new Vector3(-12.95f, 1.0f, -12.65f), Vector3.down, out RaycastHit floorHit, 2.0f) &&
                            floorHit.collider != null && floorHit.collider.gameObject.name == "SecretRoom_Floor";
            Check($"Solid interior floor grounded under player inside room (hit={floorHit.collider?.gameObject.name}, Y={floorHit.point.y:F3}m)", hasFloor);

            // 9. Empty Decoy Real Room + Opaque Threshold Veil + SecretBackroomsTransitionController occupancy toggle in BorderCorridorScene
            GameObject thresholdVeil = GameObject.Find("SecretRoom_ThresholdVeil");
            SecretBackroomsTransitionController transitionCtrl = UnityEngine.Object.FindFirstObjectByType<SecretBackroomsTransitionController>();
            bool decoyCleanAndVeilToggles = false;
            if (thresholdVeil != null && transitionCtrl != null)
            {
                bool initiallyFreeVeilOn = thresholdVeil.activeSelf;
                transitionCtrl.SetOccupiedByOtherForTest(true);
                bool occupiedVeilHidden = !thresholdVeil.activeSelf;
                transitionCtrl.SetOccupiedByOtherForTest(false);
                bool restoredVeilOn = thresholdVeil.activeSelf;
                // Confirm real room is clean empty decoy (no safe/portal inside BorderCorridorScene)
                bool noArtifactsInMainRoom = GameObject.Find("SecretRoom_HeavyVaultSafe") == null &&
                                             GameObject.Find("Portal_DimensionalMirror_Root") == null;
                decoyCleanAndVeilToggles = initiallyFreeVeilOn && occupiedVeilHidden && restoredVeilOn && noArtifactsInMainRoom;
            }
            Check(
                "Real small secret room is clean empty decoy with Threshold Veil toggling cleanly on World Core occupancy (free=veil ON, occupied=empty decoy)",
                decoyCleanAndVeilToggles
            );

            // 10. Open BackroomsLevel1Scene additively and verify 14m x 14m Level 0 maze, all 13 artifacts, Exit Door, Safe ('654321'), Astrolabe & Monumental Portal!
            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Additive);
            Physics.SyncTransforms();

            GameObject safeGo = GameObject.Find("SecretRoom_HeavyVaultSafe");
            GameObject portalGo = GameObject.Find("Portal_DimensionalMirror_Root");
            GameObject socketGo = GameObject.Find("Portal_KeystoneSocket_Root");
            GameObject exitDoorGo = GameObject.Find("Backrooms_ExitDoor_ToStreet");
            GameObject spawnGo = GameObject.Find("Backrooms_SpawnPoint");
            GameObject crtGo = GameObject.Find("Artifact_CrtTv_Root");
            GameObject dollCaseGo = GameObject.Find("Artifact_DollGlassCase");
            GameObject grimoireGo = GameObject.Find("Artifact_NecronomiconGrimoire");
            GameObject handGo = GameObject.Find("Artifact_CeramicHand");
            GameObject lamentGo = GameObject.Find("Artifact_LamentPuzzleBox");
            GameObject dybbukGo = GameObject.Find("Artifact_DybbukBox");
            GameObject corkboardGo = GameObject.Find("Artifact_EvidenceCorkboard");
            GameObject blackPhoneGo = GameObject.Find("Artifact_BlackRotaryPhone");
            GameObject paquimeMaskGo = GameObject.Find("Artifact_PaquimeObsidianMask");
            GameObject compassGo = GameObject.Find("Artifact_AnomalousCompass");

            SecretRoomDimensionVaultController vaultCtrl = UnityEngine.Object.FindFirstObjectByType<SecretRoomDimensionVaultController>();
            SecretKeypadDoorController safeDoorCtrl = safeGo != null ? safeGo.GetComponent<SecretKeypadDoorController>() : null;
            bool safeRejectsOuterCode = safeDoorCtrl != null && !safeDoorCtrl.allowInteriorSafetyBypass && !safeDoorCtrl.TrySubmitCode("123456") && !safeDoorCtrl.IsUnlocked;
            bool safeAcceptsOwnCode = safeDoorCtrl != null && safeDoorCtrl.TrySubmitCode("654321") && safeDoorCtrl.IsUnlocked && safeDoorCtrl.IsOpen;
            if (safeDoorCtrl != null)
            {
                for (int i = 0; i < 30; i++) safeDoorCtrl.StepDoorForVerification(0.05f);
            }
            bool tookAstrolabe = vaultCtrl != null && vaultCtrl.TryTakeAstrolabeFromSafe() && vaultCtrl.HasAstrolabeInInventory;
            bool activatedPortal = vaultCtrl != null &&
                                   vaultCtrl.TryToggleAstrolabeInPortalSocket() &&
                                   vaultCtrl.IsPortalActivated &&
                                   vaultCtrl.astrolabeInSocketObject != null &&
                                   vaultCtrl.astrolabeInSocketObject.activeSelf &&
                                   vaultCtrl.portalActiveVortexGroup != null &&
                                   vaultCtrl.portalActiveVortexGroup.activeSelf &&
                                   vaultCtrl.portalChainsGroup != null &&
                                   !vaultCtrl.portalChainsGroup.activeSelf;

            // Verify continuous walkability inside Backrooms Level 1 (arrival -> SW office -> pillared hall -> NW doll room -> SE safe -> NE portal!)
            Vector3[] brWalkPts = new Vector3[]
            {
                new Vector3(0.0f, BackroomsLevel1Builder.BaseY, -5.80f), // Arrival vestibule
                new Vector3(-3.8f, BackroomsLevel1Builder.BaseY, -4.4f), // SW Office
                new Vector3(-3.6f, BackroomsLevel1Builder.BaseY,  0.5f), // West Pillared Hall
                new Vector3(-4.5f, BackroomsLevel1Builder.BaseY,  4.8f), // NW Doll Room
                new Vector3(0.8f, BackroomsLevel1Builder.BaseY,  5.6f),  // North Corridor
                new Vector3(4.5f, BackroomsLevel1Builder.BaseY, -4.5f),  // SE Vault Office
                new Vector3(4.5f, BackroomsLevel1Builder.BaseY,  4.5f)   // NE Portal Chamber
            };
            bool brWalkClear = true;
            foreach (Vector3 pt in brWalkPts)
            {
                if (!Physics.Raycast(new Vector3(pt.x, pt.y + 1.0f, pt.z), Vector3.down, out RaycastHit hit, 2.0f) ||
                    hit.collider == null || hit.collider.gameObject.name != "Backrooms_Floor")
                {
                    Debug.LogError($"    Backrooms missing floor at {pt}");
                    brWalkClear = false;
                }
            }

            Check(
                "BackroomsLevel1Scene (14x14m additive) verified: all 13 artifacts, Exit Door, walkable zones, Safe ('654321'), Astrolabe Keystone & Monumental Portal activation",
                safeGo != null && portalGo != null && socketGo != null && exitDoorGo != null && spawnGo != null &&
                crtGo != null && dollCaseGo != null && grimoireGo != null && handGo != null &&
                lamentGo != null && dybbukGo != null && corkboardGo != null && blackPhoneGo != null &&
                paquimeMaskGo != null && compassGo != null &&
                safeRejectsOuterCode && safeAcceptsOwnCode && tookAstrolabe && activatedPortal && brWalkClear
            );

            // 11. Runtime Physics Guard: verify global Physics.simulationMode == FixedUpdate (never Script!)
            //     AND verify real FirstPersonPlayer CharacterController.Move() walks cleanly past the open door
            //     leaves & veilTriggerX across all 3 lanes WITHOUT any manual test-side Physics.SyncTransforms()!
            bool simModeIsFixedUpdate = Physics.simulationMode == SimulationMode.FixedUpdate;
            bool ccWalksCleanlyAllLanes = false;
            CharacterController playerCc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            if (playerCc != null && doorCtrl != null && transitionCtrl != null)
            {
                // Close & re-open the door through SecretKeypadDoorController alone (no manual Physics.SyncTransforms!)
                doorCtrl.CloseAndRelock();
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
                doorCtrl.TrySubmitCode(doorCtrl.activeAccessCode);
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);

                bool allCrossed = true;
                foreach (float laneZ in new float[] { -12.25f, -11.99f, -11.70f })
                {
                    playerCc.enabled = false;
                    playerCc.transform.position = new Vector3(-10.80f, 0.25f, laneZ);
                    playerCc.enabled = true;
                    for (int step = 0; step < 60; step++)
                    {
                        playerCc.Move(new Vector3(-0.05f, -0.05f, 0f));
                    }
                    Vector3 endPos = playerCc.transform.position;
                    bool crossed = endPos.x < transitionCtrl.veilTriggerX &&
                                   endPos.x < -12.80f &&
                                   endPos.z >= transitionCtrl.roomMinZ &&
                                   endPos.z <= transitionCtrl.roomMaxZ;
                    if (!crossed)
                    {
                        Debug.LogError($"    [FAIL] Real CC.Move failed laneZ={laneZ:F2}: endPos={endPos} (veilTriggerX={transitionCtrl.veilTriggerX}, Z=[{transitionCtrl.roomMinZ},{transitionCtrl.roomMaxZ}])");
                        allCrossed = false;
                    }
                }
                ccWalksCleanlyAllLanes = allCrossed &&
                                         Mathf.Abs(playerCc.radius - 0.28f) < 0.01f &&
                                         Mathf.Abs(playerCc.skinWidth - 0.04f) < 0.01f;
            }
            Check(
                $"Physics.simulationMode == FixedUpdate ({Physics.simulationMode}) AND real CharacterController.Move() crosses open door & Threshold Veil across all 3 lanes without manual sync",
                simModeIsFixedUpdate && ccWalksCleanlyAllLanes
            );

            Debug.Log($"[SecretWedgeVerify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        public static void CaptureVerificationScreenshots()
        {
            string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";

            // 1. Street doorway open showing the dark opaque Threshold Veil hiding the interior from Av. Benito Juárez
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject doorRoot = GameObject.Find(SecretWedgeRoomBuilder.DoorRootName);
            SecretKeypadDoorController doorCtrl = doorRoot != null ? doorRoot.GetComponent<SecretKeypadDoorController>() : null;
            if (doorCtrl != null)
            {
                doorCtrl.TrySubmitCode("123456");
                for (int i = 0; i < 30; i++) doorCtrl.StepDoorForVerification(0.05f);
            }
            RenderView(
                new Vector3(-9.75f, 1.55f, -12.05f),
                new Vector3(-12.50f, 1.32f, -12.02f),
                62f,
                System.IO.Path.Combine(outDir, "secret_threshold_veil_street.png"));

            // 2. Open BackroomsLevel1Scene standalone for crisp Level 0 fluorescent screenshots!
            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.33f, 0.21f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.22f, 0.20f, 0.11f);
            RenderSettings.fogDensity = 0.045f;

            float yEye = BackroomsLevel1Builder.BaseY + 1.55f;

            // Shot A: Backrooms Arrival Corridor + Pillared Hall + Step-Ladder + Fluorescent Ceiling Grid
            RenderView(
                new Vector3(0.0f, yEye, -5.80f),
                new Vector3(-2.60f, yEye - 0.12f, 0.20f),
                70f,
                System.IO.Path.Combine(outDir, "backrooms_level0_corridor_pillars.png"));

            // Shot B: West Pillared Hall with Black Rotary Phone on Pillar + Glowing Blue CRT TV ("7 DÍAS") + NW Doll Room
            RenderView(
                new Vector3(-2.30f, yEye, -1.60f),
                new Vector3(-5.80f, yEye - 0.25f, 0.35f),
                72f,
                System.IO.Path.Combine(outDir, "backrooms_crt_phone_hall.png"));

            // Shot C: SE Level-Omega Vault Office (Open Vault Safe with "El Ojo del Umbral" + Grimoire + Ceramic Hand + Lament Cube + Ouija)
            GameObject safeGo = GameObject.Find("SecretRoom_HeavyVaultSafe");
            SecretKeypadDoorController safeCtrl = safeGo != null ? safeGo.GetComponent<SecretKeypadDoorController>() : null;
            if (safeCtrl != null)
            {
                safeCtrl.TrySubmitCode("654321");
                for (int i = 0; i < 30; i++) safeCtrl.StepDoorForVerification(0.05f);
            }
            RenderView(
                new Vector3(3.60f, yEye, -3.40f),
                new Vector3(5.90f, yEye - 0.45f, -5.80f),
                70f,
                System.IO.Path.Combine(outDir, "backrooms_omega_safe_office.png"));

            // Shot D: NE Portal Chamber with "El Ojo del Umbral" inserted into Octagonal Socket — Monumental Dimensional Portal ACTIVATED!
            SecretRoomDimensionVaultController vaultCtrl = UnityEngine.Object.FindFirstObjectByType<SecretRoomDimensionVaultController>();
            if (vaultCtrl != null)
            {
                vaultCtrl.TryTakeAstrolabeFromSafe();
                vaultCtrl.TryToggleAstrolabeInPortalSocket();
            }
            RenderView(
                new Vector3(3.80f, yEye, 2.85f),
                new Vector3(4.50f, yEye - 0.15f, 6.76f),
                66f,
                System.IO.Path.Combine(outDir, "backrooms_monumental_portal_active.png"));

            Debug.Log("[SecretWedgeVerify] Captured Threshold Veil & 4 Backrooms Level 0 screenshots cleanly.");
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
