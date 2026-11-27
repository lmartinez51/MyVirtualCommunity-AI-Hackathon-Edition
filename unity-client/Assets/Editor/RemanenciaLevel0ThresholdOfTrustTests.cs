using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode & deterministic transition verification suite for Implementation 07.12
    /// (Room 111-B: The Guest Room, Public Directory 107-110, Threshold of Trust & Isolated One-Way Entrance View).
    /// Covers all 28 verification requirements in Section 17 plus anti-softlock reachability from
    /// Backrooms_SpawnPoint and all 10 Azucenas Manhole drop locations.
    /// </summary>
    [TestFixture]
    public class RemanenciaLevel0ThresholdOfTrustTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;
        private const float PlayerRadius = 0.32f;

        private const string ExpectedBorderCorridorSceneSha256 =
            "0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B";

        private static bool IsCapsuleClear(float x, float z)
        {
            Vector3 pBot = new Vector3(x, BaseY + PlayerRadius + 0.08f, z);
            Vector3 pTop = new Vector3(x, BaseY + 1.80f - PlayerRadius - 0.05f, z);
            Collider[] hits = Physics.OverlapCapsule(pBot, pTop, PlayerRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].enabled && !hits[i].isTrigger)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasValidGroundFloor(float x, float z)
        {
            if (!Physics.Raycast(new Vector3(x, BaseY + 1.0f, z), Vector3.down, out RaycastHit hit, 2.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (hit.collider == null) return false;
            string n = hit.collider.gameObject.name;
            bool isKnownFloor =
                n == "Backrooms_Floor" ||
                n == "NorthWing_FloorSlab" ||
                n == "Room111B_EastCorr_FloorSlab" ||
                n == "Room111B_Shell_FloorSlab";
            return isKnownFloor && Mathf.Abs(hit.point.y - BaseY) < 0.03f;
        }

        private static void AssertWalkablePolyline(string routeName, Vector2[] waypoints, float stepMeters = 0.25f)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / stepMeters));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, (float)s / steps);
                    Assert.IsTrue(HasValidGroundFloor(p.x, p.y), $"{routeName}: missing walkable floor at ({p.x:F2}, {p.y:F2}).");
                    Assert.IsTrue(IsCapsuleClear(p.x, p.y), $"{routeName}: solid collision obstruction at ({p.x:F2}, {p.y:F2}).");
                }
            }
        }

        private static string ComputeFileSha256(string relativeAssetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));
            Assert.IsTrue(File.Exists(fullPath), $"Missing expected file: {fullPath}");
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        [Test]
        public void PublicDirectoryRoom111BInteriorAndFurnitureClearance_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            Transform interiorRoot = root.transform.Find(BackroomsLevel1Builder.HotelRoom111BGuestInteriorRootName);
            Assert.IsNotNull(interiorRoot, "Hotel_Room111B_GuestInterior must exist directly under BackroomsLevel1_Root.");

            // 1 & 2. Public North Corridor directory lists ONLY 107-110 (zero public mention of 111-B)
            Assert.IsTrue(BackroomsLevel1Builder.NorthCorridorDirectoryEs.Contains("107-110"));
            Assert.IsTrue(BackroomsLevel1Builder.NorthCorridorDirectoryRooms.Contains("107"));
            Assert.IsTrue(BackroomsLevel1Builder.NorthCorridorDirectoryRooms.Contains("108"));
            Assert.IsTrue(BackroomsLevel1Builder.NorthCorridorDirectoryRooms.Contains("109"));
            Assert.IsTrue(BackroomsLevel1Builder.NorthCorridorDirectoryRooms.Contains("110"));
            Assert.IsFalse(BackroomsLevel1Builder.NorthCorridorDirectoryEs.Contains("111"), "Public directory must NOT list 111-B.");
            Assert.IsFalse(BackroomsLevel1Builder.NorthCorridorDirectoryRooms.Contains("111"), "Public directory must NOT list 111-B.");
            Assert.IsFalse(BackroomsLevel1Builder.SignGuestRoomsEs.Contains("111"), "South guest room wayfinding must NOT list 111-B.");

            Transform pubDir = interiorRoot.Find("Room111B_NorthCorridorDirectory/Hotel_NorthCorridor_PublicDirectory");
            Assert.IsNotNull(pubDir, "Hotel_NorthCorridor_PublicDirectory must exist.");
            Material dirMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.NorthCorridorDirectoryMatPath);
            Assert.IsNotNull(dirMat);
            Assert.AreEqual(dirMat, pubDir.GetComponent<Renderer>().sharedMaterial);
            Assert.IsNull(pubDir.GetComponent<Collider>(), "Public directory sign must be non-colliding.");

            // 3 & 4. 07.6 Room Registration Record STILL contains 111-B AND physical Room111B_Plaque_111B remains present
            Transform regRecord = root.transform.Find("Hotel_Narrative_Evidence/Hotel_Evidence_RoomRegistrationRecord");
            Assert.IsNotNull(regRecord);
            SecretContainmentArtifactDescriptor regDesc = regRecord.GetComponent<SecretContainmentArtifactDescriptor>();
            Assert.IsNotNull(regDesc);
            Assert.IsTrue(regDesc.loreEs.Contains("111-B") && regDesc.loreEn.Contains("111-B"),
                "07.6 Room Registration Record must continue to reference 111-B.");
            Assert.IsNotNull(root.transform.Find("Backrooms_Architecture/Hotel_Room111B_Anomaly/Room111B_Signage/Room111B_Plaque_111B"),
                "Physical Room111B_Plaque_111B must remain intact.");

            // 5. Room 111-B interior objects exist under Room111B_FurnitureAndFinishes
            Transform furnRoot = interiorRoot.Find("Room111B_FurnitureAndFinishes");
            Assert.IsNotNull(furnRoot);
            string[] expectedFurniture =
            {
                "Room111B_Interior_FloorFinish",
                "Room111B_Furn_DoubleBed",
                "Room111B_Furn_BedsideW",
                "Room111B_Furn_BedsideE",
                "Room111B_Furn_Wardrobe",
                "Room111B_Furn_Desk",
                "Room111B_Furn_Chair",
                "Room111B_Furn_WallMirror"
            };
            foreach (string fn in expectedFurniture)
            {
                Assert.IsNotNull(furnRoot.Find(fn), $"Missing expected Room 111-B interior group '{fn}'.");
            }

            // Major solid furniture pieces have simple colliders; no NavMesh/Rigidbody/Audio/Lights/Documents inside interior
            Assert.IsNotNull(furnRoot.Find("Room111B_Furn_DoubleBed/DoubleBed_Base").GetComponent<BoxCollider>());
            Assert.IsNotNull(furnRoot.Find("Room111B_Furn_Wardrobe/Wardrobe_Carcass").GetComponent<BoxCollider>());
            Assert.IsNotNull(furnRoot.Find("Room111B_Furn_Desk/Desk_Top").GetComponent<BoxCollider>());
            Assert.AreEqual(0, interiorRoot.GetComponentsInChildren<Light>(includeInactive: true).Length, "Zero new Light components allowed.");
            Assert.AreEqual(0, interiorRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, "Zero Rigidbodies allowed.");
            Assert.AreEqual(0, interiorRoot.GetComponentsInChildren<NavMeshAgent>(includeInactive: true).Length, "Zero NavMeshAgents allowed.");
            Assert.AreEqual(0, interiorRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, "Zero AudioSources allowed.");
            Assert.AreEqual(0, interiorRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length,
                "Zero new narrative documents or journal discoveries allowed in 07.12.");

            // 6 & 7. Player capsule traverses entire room cleanly around furniture & right up to the Threshold of Trust
            AssertWalkablePolyline("Room111B_Furnished_FullTraversal", new[]
            {
                new Vector2(19.01f, 22.40f), // Doorway
                new Vector2(21.82f, 22.40f), // Room center
                new Vector2(23.50f, 21.00f), // SE clear space
                new Vector2(23.50f, 23.80f), // NE clear space
                new Vector2(20.20f, 23.80f), // NW walkway past desk & wardrobe
                new Vector2(20.20f, 21.00f), // SW walkway past bedside
                new Vector2(20.20f, 22.40f), // Back to center axis
                new Vector2(24.08f, 22.40f)  // Step deliberately into Threshold of Trust recess!
            });
        }

        [Test]
        public void ThresholdOfTrustGeometryTriggerAndReturnTransition_WorkDeterministicallyAndSafely()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform thresholdGroup = root.transform.Find($"{BackroomsLevel1Builder.HotelRoom111BGuestInteriorRootName}/Room111B_ThresholdOfTrust");
            Assert.IsNotNull(thresholdGroup);

            // 8 & 9. Threshold exists at documented East coordinates & trigger is properly oriented
            Transform trigTf = thresholdGroup.Find("Room111B_Threshold_TriggerVolume");
            Assert.IsNotNull(trigTf);
            Assert.Less(Vector3.Distance(trigTf.position, new Vector3(24.26f, BaseY + 1.15f, 22.40f)), 0.05f);
            Assert.Greater(Vector3.Dot(trigTf.forward, Vector3.left), 0.99f, "Threshold trigger forward must face West (-X) into Room 111-B.");
            BoxCollider trigBox = trigTf.GetComponent<BoxCollider>();
            Assert.IsNotNull(trigBox);
            Assert.IsTrue(trigBox.isTrigger);

            RemanenciaThresholdOfTrustTrigger trustTrig = trigTf.GetComponent<RemanenciaThresholdOfTrustTrigger>();
            Assert.IsNotNull(trustTrig);

            // 10. Proximity outside crossing boundary does NOT trigger
            Assert.IsFalse(trustTrig.IsWithinCrossingZone(new Vector3(21.82f, BaseY, 22.40f)), "Center of room must not trigger.");
            Assert.IsFalse(trustTrig.IsWithinCrossingZone(new Vector3(23.50f, BaseY, 22.40f)), "Walking past along X=23.50 must not trigger.");
            Assert.IsFalse(trustTrig.IsWithinCrossingZone(new Vector3(23.85f, BaseY, 22.40f)), "Inspecting right outside sill (X=23.85) must not trigger.");
            Assert.IsFalse(trustTrig.IsWithinCrossingZone(new Vector3(24.15f, BaseY, 21.20f)), "Outside Z span must not trigger.");
            Assert.IsTrue(trustTrig.IsWithinCrossingZone(new Vector3(24.08f, BaseY, 22.40f)), "Deliberate crossing past X=23.92 inside Z=[21.8,23.0] must register.");

            // 11-17. Execute actual ExitBackroomsRoutine() via SecretBackroomsTransitionController on isolated test harness
            AmbientMode savedAmbMode = RenderSettings.ambientMode;
            Color savedAmbLight = RenderSettings.ambientLight;
            bool savedFog = RenderSettings.fog;
            Color savedFogCol = RenderSettings.fogColor;
            float savedFogDens = RenderSettings.fogDensity;
            Material savedSky = RenderSettings.skybox;

            GameObject harness = new GameObject("__TestTransitionHarness__");
            GameObject playerGo = new GameObject("__TestPlayer__");
            try
            {
                CharacterController cc = playerGo.AddComponent<CharacterController>();
                playerGo.transform.position = new Vector3(24.08f, BaseY, 22.40f);

                SecretBackroomsTransitionController ctrl = harness.AddComponent<SecretBackroomsTransitionController>();
                ctrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
                Assert.AreEqual(PlayerWorldContext.RemanenciaLevel0, ctrl.CurrentWorldContext);

                // Seed outdoor atmosphere baseline, switch into Remanencia atmosphere
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
                RenderSettings.fog = false;
                ctrl.CaptureAndApplyBackroomsAtmosphereForTest();
                Assert.IsTrue(RenderSettings.fog, "Remanencia atmosphere should enable exponential fog.");

                // Proximity check does NOT fire
                trustTrig.ResetTriggerLatchForTest();
                Assert.IsFalse(trustTrig.EvaluatePlayerCrossing(new Vector3(23.50f, BaseY, 22.40f), ctrl));
                Assert.IsFalse(trustTrig.HasTriggeredThisVisit);

                // Step the real ExitBackroomsRoutine() coroutine for Threshold of Trust!
                // First test duplicate-request guard: once transitioning starts, a second call returns false
                IEnumerator routine = ctrl.CreateExitRemanenciaRoutineForTest(RemanenciaThresholdOfTrustTrigger.ExitSourceId);
                Assert.IsTrue(routine.MoveNext(), "Exit routine must start.");
                Assert.IsTrue(ctrl.IsTransitioning, "Controller must mark isTransitioning immediately.");
                Assert.IsFalse(ctrl.RequestExitRemanencia(RemanenciaThresholdOfTrustTrigger.ExitSourceId),
                    "Duplicate transition request while transitioning must be rejected.");

                // Drain the real coroutine cleanly to completion (advancing frames)
                int guard = 0;
                while (routine.MoveNext() && guard++ < 500) { }

                Assert.IsFalse(ctrl.IsTransitioning, "Transition must complete cleanly.");
                Assert.IsFalse(ctrl.IsPlayerInBackrooms, "Player must no longer be marked in Backrooms.");
                Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, ctrl.CurrentWorldContext, "WorldContext must return to SurfaceCorridor.");
                Assert.AreEqual(RemanenciaThresholdOfTrustTrigger.ExitSourceId, ctrl.LastExitSource);
                Assert.IsFalse(RenderSettings.fog, "Outdoor RenderSettings.fog must be restored cleanly.");
                Assert.IsNull(ctrl.ActiveExteriorObservationSnapshot, "Single-shot observation RenderTexture must be released on exit.");

                // Verify conventional exit door path ALSO works through RequestExitRemanencia("exit-door-to-street")
                ctrl.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
                IEnumerator doorRoutine = ctrl.CreateExitRemanenciaRoutineForTest("exit-door-to-street");
                int guard2 = 0;
                while (doorRoutine.MoveNext() && guard2++ < 500) { }
                Assert.IsFalse(ctrl.IsPlayerInBackrooms);
                Assert.AreEqual("exit-door-to-street", ctrl.LastExitSource);
                Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, ctrl.CurrentWorldContext);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(harness);
                UnityEngine.Object.DestroyImmediate(playerGo);
                RenderSettings.ambientMode = savedAmbMode;
                RenderSettings.ambientLight = savedAmbLight;
                RenderSettings.fog = savedFog;
                RenderSettings.fogColor = savedFogCol;
                RenderSettings.fogDensity = savedFogDens;
                RenderSettings.skybox = savedSky;
                trustTrig.ResetTriggerLatchForTest();
            }
        }

        [Test]
        public void AntiSoftlockReachabilityFromSpawnAndAllTenManholeDropZones_IsVerified()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // Primary entry: Backrooms_SpawnPoint -> Main Lobby -> Connection A -> East Arcade -> North Corridor -> Room 111-B -> Threshold of Trust
            AssertWalkablePolyline("SpawnPoint_To_ThresholdOfTrust", new[]
            {
                new Vector2(17.80f,  8.80f), // Backrooms_SpawnPoint
                new Vector2(14.50f,  8.80f), // Lobby Entrance Arch
                new Vector2( 5.40f,  8.80f), // Main Lobby North Approach
                new Vector2( 5.40f, 12.00f), // Connection A
                new Vector2( 6.60f, 16.50f), // East Arcade South
                new Vector2( 6.60f, 27.60f), // East Arcade -> North Guest Corridor
                new Vector2(16.00f, 27.60f), // Concealed East Opening
                new Vector2(16.78f, 27.60f), // Baffle Throat
                new Vector2(16.78f, 24.40f), // East Corridor North
                new Vector2(17.65f, 22.40f), // East Corridor facing 111-B
                new Vector2(19.01f, 22.40f), // Room 111-B Doorway
                new Vector2(24.08f, 22.40f)  // Threshold of Trust Crossing Plane!
            });

            // All 10 Azucenas Manhole safe-drop zones: verify ground floor & capsule clearance (or safe fallback) and path to public circulation
            Vector2[] dropZones =
            {
                new Vector2(-7.5f,  2.7f),
                new Vector2(-3.5f, -0.2f),
                new Vector2( 0.5f,  2.7f),
                new Vector2( 3.8f,  8.2f),
                new Vector2( 9.8f,  7.2f),
                new Vector2(12.6f,  3.0f),
                new Vector2(17.2f, -1.5f),
                new Vector2( 4.5f, -8.2f),
                new Vector2(-3.8f, -8.5f),
                new Vector2(-14.5f, 0.8f)
            };

            for (int i = 0; i < dropZones.Length; i++)
            {
                Vector2 dz = dropZones[i];
                Assert.IsTrue(HasValidGroundFloor(dz.x, dz.y), $"Manhole drop zone #{i} at ({dz.x}, {dz.y}) lacks ground floor.");
            }

            // Verify West Service Hall (Connection B) & Main Guest Corridor both connect unobstructed to Room 111-B
            AssertWalkablePolyline("MainCorridor_West_To_ConnectionA", new[]
            {
                new Vector2(-10.80f, -0.20f),
                new Vector2(  5.40f, -0.20f),
                new Vector2(  5.40f, 12.00f)
            });
            AssertWalkablePolyline("ServiceHall_To_ConnectionB_And_NorthCorridor", new[]
            {
                new Vector2(-10.80f,  9.50f),
                new Vector2(-10.80f, 12.00f),
                new Vector2(-10.80f, 27.60f),
                new Vector2( 16.00f, 27.60f)
            });
        }

        [Test]
        public void OriginalExitDoorSurfacePreservationOneWayIsolationProgressionAndIdempotency_AreVerified()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 18. Original Backrooms_ExitDoor_ToStreet remains enabled & operational during 07.12
            Transform exitDoor = root.transform.Find("Backrooms_ExitDoor_ToStreet");
            Assert.IsNotNull(exitDoor);
            BackroomsExitDoorTrigger exitTrig = exitDoor.GetComponent<BackroomsExitDoorTrigger>();
            Assert.IsNotNull(exitTrig);
            Assert.IsTrue(exitTrig.enabled);
            Assert.IsTrue(exitTrig.GetPrompt(isEs: true).Contains("Av. Benito Juárez"));
            Assert.IsTrue(exitTrig.GetPrompt(isEs: false).Contains("Av. Benito Juárez"));

            // 19, 20, 21. Strict Surface-World Preservation: BorderCorridorScene.unity byte-for-byte identical
            string actualBorderSha = ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity");
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, actualBorderSha,
                "CRITICAL VIOLATION: BorderCorridorScene.unity must remain 100% byte-for-byte identical.");

            // 22. One-way observation view is strictly confined to BackroomsLevel1Scene & dedicated material
            Transform entIllusion = root.transform.Find($"{BackroomsLevel1Builder.HotelRoom111BGuestInteriorRootName}/Room111B_InteriorEntranceIllusion");
            Assert.IsNotNull(entIllusion);
            RemanenciaOneWayEntranceView oneWayView = entIllusion.GetComponent<RemanenciaOneWayEntranceView>();
            Assert.IsNotNull(oneWayView);
            Assert.IsNotNull(oneWayView.unifiedGlassPane);
            Assert.AreEqual(0, entIllusion.GetComponentsInChildren<Collider>(includeInactive: true).Length,
                "Interior entrance visual overlay must not add blocking colliders over Backrooms_ExitDoor_ToStreet.");
            Material oneWayMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.EntranceOneWayGlassMatPath);
            Assert.IsNotNull(oneWayMat);
            Assert.AreEqual(oneWayMat, oneWayView.unifiedGlassPane.sharedMaterial);

            // 24, 25, 26. Zero save schema / progression ID changes; exact 18 lights (0 shadow-casting)
            var saveData = new ProgressionSaveData();
            Assert.AreEqual(1, saveData.schemaVersion);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);

            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length);
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // 28. Migration is scene-specific & strictly idempotent
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeColliders = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaRoom111BInteriorAndThresholdInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeColliders, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaRoom111BInteriorAndThresholdInOpenScene(emptyScene));
        }
    }
}
