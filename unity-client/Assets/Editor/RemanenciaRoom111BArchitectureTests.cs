using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode spatial, concealment, traversal, enclosure, preservation, and idempotency
    /// verification suite for Implementation 07.11 (Room 111-B: The Impossible East Wing).
    /// </summary>
    [TestFixture]
    public class RemanenciaRoom111BArchitectureTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;
        private const float CeilingY = BackroomsLevel1Builder.CeilingY;
        private const float PlayerRadius = 0.32f;

        // Verified pre-07.11 baseline SHA-256 of unity-client/Assets/Scenes/BorderCorridorScene.unity
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

        private static bool HasSolidCeiling(float x, float z)
        {
            if (!Physics.Raycast(new Vector3(x, BaseY + 1.5f, z), Vector3.up, out RaycastHit hit, 3.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            // Overhead enclosure may be a framed doorway transom (Y = BaseY + 2.20m) or full ceiling slab (CeilingY = BaseY + 2.85m)
            return hit.collider != null && hit.point.y >= BaseY + 2.15f && hit.point.y <= CeilingY + 0.15f;
        }

        private static void AssertWalkablePolyline(string routeName, Vector2[] waypoints, float stepMeters = 0.25f)
        {
            Assert.GreaterOrEqual(waypoints.Length, 2, $"{routeName} requires at least 2 waypoints.");
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                float dist = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / stepMeters));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, (float)s / steps);
                    Assert.IsTrue(HasValidGroundFloor(p.x, p.y),
                        $"{routeName}: missing or misaligned walkable floor at ({p.x:F2}, {p.y:F2}).");
                    Assert.IsTrue(IsCapsuleClear(p.x, p.y),
                        $"{routeName}: solid collision obstruction at ({p.x:F2}, {p.y:F2}) along segment {i}.");
                    Assert.IsTrue(HasSolidCeiling(p.x, p.y),
                        $"{routeName}: missing solid ceiling slab above ({p.x:F2}, {p.y:F2}).");
                }
            }
        }

        private static string ComputeFileSha256(string relativeAssetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));
            Assert.IsTrue(File.Exists(fullPath), $"Expected file missing: {fullPath}");
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        [Test]
        public void Room111BHierarchyOpeningConcealmentAndSignage_AreArchitecturallyValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. Hotel_Room111B_Anomaly exists exactly once under Backrooms_Architecture
            int count = 0;
            Transform anomalyRoot = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == BackroomsLevel1Builder.HotelRoom111BRootName)
                    {
                        count++;
                        anomalyRoot = t;
                    }
                }
            }
            Assert.AreEqual(1, count, "Hotel_Room111B_Anomaly must exist exactly once.");
            Assert.IsNotNull(anomalyRoot);
            Assert.AreEqual(shell, anomalyRoot.parent, "Hotel_Room111B_Anomaly must be parented under Backrooms_Architecture.");

            // Required 4 subgroups
            string[] subgroups =
            {
                "Room111B_ConcealedOpening",
                "Room111B_EastCorridor",
                "Room111B_RoomShell",
                "Room111B_Signage"
            };
            foreach (string sub in subgroups)
            {
                Assert.IsNotNull(anomalyRoot.Find(sub), $"Missing required subgroup '{sub}'.");
            }

            // 2. Concealed East passage opening exists at X=+16.0, Z in [+26.80, +28.40] (~1.60m clear width x 2.20m height)
            Vector3 openingCenter = new Vector3(16.00f, BaseY + 1.15f, 27.60f);
            Assert.IsTrue(Physics.Raycast(openingCenter, Vector3.back, out RaycastHit southJambHit, 2.0f, ~0, QueryTriggerInteraction.Ignore),
                "Expected solid south jamb at Z=+26.80.");
            Assert.IsTrue(Physics.Raycast(openingCenter, Vector3.forward, out RaycastHit northJambHit, 2.0f, ~0, QueryTriggerInteraction.Ignore),
                "Expected solid north jamb at Z=+28.40.");
            float clearOpeningWidth = southJambHit.distance + northJambHit.distance;
            Assert.GreaterOrEqual(clearOpeningWidth, 1.50f, $"Concealed opening clear width ({clearOpeningWidth:F2}m) must be >= 1.50m.");
            Assert.LessOrEqual(clearOpeningWidth, 1.70f, $"Concealed opening clear width ({clearOpeningWidth:F2}m) should stay near 1.60m.");

            Assert.IsTrue(Physics.Raycast(new Vector3(16.00f, BaseY + 1.10f, 27.60f), Vector3.up, out RaycastHit transomHit, 2.0f, ~0, QueryTriggerInteraction.Ignore),
                "Expected solid overhead transom above concealed opening.");
            float clearOpeningHeight = transomHit.point.y - BaseY;
            Assert.AreEqual(2.20f, clearOpeningHeight, 0.05f, "Concealed opening clear height should be ~2.20m.");

            // 3. Concealment verification from normal North Guest Corridor approach (X = 8..13):
            //    Straight-ahead East sightlines hit solid concrete baffle wall; East Corridor interior & 111-B doorway are occluded
            Vector3[] corridorEyes =
            {
                new Vector3( 8.50f, BaseY + 1.58f, 27.45f),
                new Vector3(10.50f, BaseY + 1.58f, 27.50f),
                new Vector3(12.40f, BaseY + 1.58f, 27.60f),
                new Vector3(13.00f, BaseY + 1.58f, 28.20f)
            };
            Vector3 eastCorridorPoint = new Vector3(17.50f, BaseY + 1.40f, 23.80f);
            Vector3 room111BDoorPoint = new Vector3(19.01f, BaseY + 1.40f, 22.40f);
            foreach (Vector3 eye in corridorEyes)
            {
                Assert.IsTrue(Physics.Raycast(eye, Vector3.right, out RaycastHit deadEndHit, 12.0f, ~0, QueryTriggerInteraction.Ignore),
                    $"Forward East ray from {eye} must hit solid concrete.");
                Assert.AreEqual("Room111B_ConcealedBaffle_Wall", deadEndHit.collider.gameObject.name,
                    $"From {eye}, the apparent dead end should read as solid concrete baffle wall.");

                Assert.IsTrue(Physics.Linecast(eye, eastCorridorPoint, ~0, QueryTriggerInteraction.Ignore),
                    $"East Corridor interior must be occluded from normal corridor position {eye}.");
                Assert.IsTrue(Physics.Linecast(eye, room111BDoorPoint, ~0, QueryTriggerInteraction.Ignore),
                    $"Room 111-B doorway must be occluded from normal corridor position {eye}.");
            }

            // 8. Upright, non-mirrored 111-B plaque exists, uses matte dedicated material, faces West (-X), and is unobstructed
            Transform plaque = anomalyRoot.Find("Room111B_Signage/Room111B_Plaque_111B");
            Assert.IsNotNull(plaque, "Room111B_Plaque_111B must exist under Room111B_Signage.");
            Material expectedPlaqueMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.RoomPlaque111BMatPath);
            Assert.IsNotNull(expectedPlaqueMat, "Remanencia_Level0_RoomPlaque_111B.mat must exist.");
            Assert.IsFalse(expectedPlaqueMat.IsKeywordEnabled("_EMISSION"), "111-B plaque must not emit/glow.");
            Assert.AreEqual(expectedPlaqueMat, plaque.GetComponent<Renderer>().sharedMaterial);
            Assert.Greater(Vector3.Dot(plaque.forward, Vector3.left), 0.99f, "111-B plaque must orient +Z West (-X) toward East Corridor approach.");
            Assert.Greater(Vector3.Dot(plaque.up, Vector3.up), 0.99f, "111-B plaque must be upright.");
            Assert.IsFalse(
                Physics.Linecast(new Vector3(17.50f, BaseY + 1.58f, 23.45f), plaque.position + Vector3.left * 0.05f, ~0, QueryTriggerInteraction.Ignore),
                "111-B plaque must be clearly visible from East Corridor approach.");
        }

        [Test]
        public void ContinuousTraversalThroughConcealedOpeningEastCorridorAndRoom111B_IsUnobstructedAndEnclosed()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // 4, 5, 6, 7. Full continuous walkable route from North Guest Corridor -> Concealed Opening ->
            //             Baffle Throat -> Phantom East Corridor -> Framed Room 111-B Doorway -> Room 111-B Interior
            AssertWalkablePolyline("NorthCorridor_To_Room111B_FullTraversal", new[]
            {
                new Vector2(10.00f, 27.60f), // North Guest Corridor past Room 107
                new Vector2(14.60f, 27.60f), // Past EastOverlookNib (X=13.6)
                new Vector2(16.00f, 27.60f), // Through concealed East perimeter opening (X=+16.0)
                new Vector2(16.78f, 27.60f), // Center of 1.30m baffle throat (X in [16.13, 17.43])
                new Vector2(16.78f, 24.40f), // Turning South past shadow-baffle wall
                new Vector2(17.65f, 22.40f), // Center of Phantom East Corridor facing 111-B doorway
                new Vector2(19.01f, 22.40f), // Through framed Room 111-B doorway (X=+19.01, Z in [21.70, 23.10])
                new Vector2(21.82f, 22.40f), // Center of Room 111-B (beyond original X=+20.0 hotel boundary!)
                new Vector2(23.50f, 21.00f), // Room 111-B SE quadrant
                new Vector2(23.50f, 23.80f), // Room 111-B NE quadrant
                new Vector2(20.20f, 23.80f), // Room 111-B NW quadrant
                new Vector2(20.20f, 21.00f)  // Room 111-B SW quadrant
            });

            // 7. Verify Room 111-B doorway clear dimensions & architectural framing
            Vector3 doorCenter = new Vector3(19.01f, BaseY + 1.15f, 22.40f);
            Assert.IsTrue(Physics.Raycast(doorCenter, Vector3.back, out RaycastHit doorHitS, 1.5f, ~0, QueryTriggerInteraction.Ignore));
            Assert.IsTrue(Physics.Raycast(doorCenter, Vector3.forward, out RaycastHit doorHitN, 1.5f, ~0, QueryTriggerInteraction.Ignore));
            float doorClearW = doorHitS.distance + doorHitN.distance;
            Assert.GreaterOrEqual(doorClearW, 1.25f, $"Room 111-B doorway clear width ({doorClearW:F2}m) must comfortably fit player capsule.");

            Transform corridorGroup = GameObject.Find("Room111B_EastCorridor")?.transform;
            Assert.IsNotNull(corridorGroup);
            Assert.IsNotNull(corridorGroup.Find("Room111B_DoorWall_Transom"));
            Assert.IsNotNull(corridorGroup.Find("Room111B_DoorWall_JambS"));
            Assert.IsNotNull(corridorGroup.Find("Room111B_DoorWall_JambN"));
            Assert.IsNotNull(corridorGroup.Find("Room111B_DoorWall_HeaderTrim"));
            Assert.IsNotNull(corridorGroup.Find("Room111B_Doorway_ThresholdPlate"));

            // 9. Solid enclosure verification (ZERO exterior voids or perimeter gaps around new East Wing & Room 111-B)
            (string label, Vector3 a, Vector3 b)[] enclosureChecks =
            {
                ("Opening North Seal (Z=+28.80)",          new Vector3(16.78f, BaseY + 1.2f, 28.10f), new Vector3(16.78f, BaseY + 1.2f, 29.40f)),
                ("Baffle Wall East Face (X=+17.55, Z=27)", new Vector3(16.80f, BaseY + 1.2f, 27.00f), new Vector3(18.20f, BaseY + 1.2f, 27.00f)),
                ("Baffle East Shoulder (Z=+25.40)",        new Vector3(18.30f, BaseY + 1.2f, 24.80f), new Vector3(18.30f, BaseY + 1.2f, 26.00f)),
                ("East Corridor South Wall (Z=+20.09)",    new Vector3(17.50f, BaseY + 1.2f, 20.80f), new Vector3(17.50f, BaseY + 1.2f, 19.30f)),
                ("East Corridor West Wall (X=+16, Z=23)",  new Vector3(16.80f, BaseY + 1.2f, 23.00f), new Vector3(15.20f, BaseY + 1.2f, 23.00f)),
                ("Room 111-B Wall North (Z=+24.93)",       new Vector3(22.00f, BaseY + 1.2f, 24.20f), new Vector3(22.00f, BaseY + 1.2f, 25.60f)),
                ("Room 111-B Wall South (Z=+19.87)",       new Vector3(22.00f, BaseY + 1.2f, 20.60f), new Vector3(22.00f, BaseY + 1.2f, 19.10f)),
                ("Room 111-B Wall East (X=+24.65)",        new Vector3(23.80f, BaseY + 1.2f, 22.40f), new Vector3(25.40f, BaseY + 1.2f, 22.40f)),
                ("Room 111-B West Wall South of Door",     new Vector3(20.00f, BaseY + 1.2f, 20.60f), new Vector3(18.20f, BaseY + 1.2f, 20.60f)),
                ("Room 111-B West Wall North of Door",     new Vector3(20.00f, BaseY + 1.2f, 24.20f), new Vector3(18.20f, BaseY + 1.2f, 24.20f)),
                ("Room 107 East Perimeter North Stub",     new Vector3(15.20f, BaseY + 1.2f, 31.20f), new Vector3(16.80f, BaseY + 1.2f, 31.20f))
            };
            foreach (var chk in enclosureChecks)
            {
                Assert.IsTrue(Physics.Linecast(chk.a, chk.b, ~0, QueryTriggerInteraction.Ignore),
                    $"Enclosure gap detected on '{chk.label}' between {chk.a} and {chk.b}.");
            }

            // Room 111-B intentionally extends East beyond original hotel boundary X = +20.0
            Transform eastWall = GameObject.Find("Room111B_Shell_WallEast")?.transform;
            Assert.IsNotNull(eastWall);
            Assert.Greater(eastWall.position.x, 24.0f, "Room 111-B East wall must extend past original X=+20.0 footprint.");
        }

        [Test]
        public void ExistingPerimeterRoomsEvidenceEcologyElevatorsExitAndSurfaceWorld_RemainPreserved()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 10 & 11. Mandatory existing M1 perimeter assertions (North X=0, East Z=20, West Z=20)
            Assert.IsTrue(Physics.Linecast(new Vector3(0f, BaseY + 1.2f, 33.0f), new Vector3(0f, BaseY + 1.2f, 35.0f), ~0, QueryTriggerInteraction.Ignore),
                "North perimeter at (X=0, Z=33..35) must remain solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(15.0f, BaseY + 1.2f, 20.0f), new Vector3(17.0f, BaseY + 1.2f, 20.0f), ~0, QueryTriggerInteraction.Ignore),
                "East perimeter at (X=15..17, Z=20.0) must remain solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-15.0f, BaseY + 1.2f, 20.0f), new Vector3(-17.0f, BaseY + 1.2f, 20.0f), ~0, QueryTriggerInteraction.Ignore),
                "West perimeter at (X=-15..-17, Z=20.0) must remain solid.");

            // 12. Existing Rooms 101-110 remain intact
            for (int r = 101; r <= 110; r++)
            {
                Assert.IsNotNull(GameObject.Find($"Hotel_GuestRoom_{r}"), $"Existing Guest Room {r} must remain intact.");
            }

            // 13. All four 07.6 narrative discoveries remain intact
            Transform narrativeRoot = root.transform.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName);
            Assert.IsNotNull(narrativeRoot);
            Assert.IsNotNull(narrativeRoot.Find("Hotel_Evidence_UnfinishedProjectPlan"));
            Assert.IsNotNull(narrativeRoot.Find("Hotel_Evidence_RoomRegistrationRecord"));
            Assert.IsNotNull(narrativeRoot.Find("Hotel_Evidence_MaintenanceRecord"));
            Assert.IsNotNull(narrativeRoot.Find("Hotel_Evidence_Cobalt60RumorNote"));
            Assert.AreEqual(4, narrativeRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length);

            // 14. 07.7 Ecology, 07.8 Faunal Traces, and 07.9 First Living Microfauna remain intact
            Transform ecoRoot = root.transform.Find(BackroomsLevel1Builder.HotelEarlyEcologyRootName);
            Transform faunalRoot = root.transform.Find(BackroomsLevel1Builder.HotelFaunalTracesRootName);
            Transform microRoot = root.transform.Find(BackroomsLevel1Builder.HotelFirstLivingMicrofaunaRootName);
            Assert.IsNotNull(ecoRoot);
            Assert.AreEqual(4, ecoRoot.childCount);
            Assert.AreEqual(23, ecoRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.IsNotNull(faunalRoot);
            Assert.AreEqual(3, faunalRoot.childCount);
            Assert.AreEqual(16, faunalRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.IsNotNull(microRoot);
            Assert.AreEqual(2, microRoot.GetComponentsInChildren<RemanenciaAmbientMicrofaunaController>(includeInactive: true).Length);

            // 15. Both elevator bays unchanged
            Transform elev = GameObject.Find("Hotel_Elevator_Core")?.transform;
            Assert.IsNotNull(elev);
            Assert.AreEqual(35, elev.childCount);

            // 16. Save schema, progression IDs, spawn point, and exit door operational & unchanged
            var saveData = new ProgressionSaveData();
            Assert.AreEqual(1, saveData.schemaVersion);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);
            Transform spawn = root.transform.Find("Backrooms_SpawnPoint");
            Transform exitDoor = root.transform.Find("Backrooms_ExitDoor_ToStreet");
            Assert.IsNotNull(spawn);
            Assert.IsNotNull(exitDoor);
            Assert.Less(Vector3.Distance(spawn.position, new Vector3(17.80f, BaseY + 0.06f, 8.80f)), 0.01f);
            Assert.Less(Vector3.Distance(exitDoor.position, new Vector3(19.86f, BaseY, 8.80f)), 0.01f);
            BackroomsExitDoorTrigger exitTrigger = exitDoor.GetComponent<BackroomsExitDoorTrigger>();
            Assert.IsNotNull(exitTrigger, "BackroomsExitDoorTrigger must remain attached and operational.");
            Assert.IsTrue(exitTrigger.enabled, "BackroomsExitDoorTrigger must remain enabled in 07.11.");

            // 19. No unintended components, lights, furniture, clues, or organisms inside Hotel_Room111B_Anomaly
            Transform anomalyRoot = root.transform.Find($"Backrooms_Architecture/{BackroomsLevel1Builder.HotelRoom111BRootName}");
            Assert.IsNotNull(anomalyRoot);
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length, "Zero MonoBehaviours allowed in Room 111-B architecture.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<Light>(includeInactive: true).Length, "Zero Light components allowed in Room 111-B.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, "Zero AudioSources allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true).Length, "Zero ParticleSystems allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length, "Zero Animators allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<NavMeshAgent>(includeInactive: true).Length, "Zero NavMeshAgents allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, "Zero Rigidbodies allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<InteractionPoint>(includeInactive: true).Length, "Zero InteractionPoints allowed.");
            Assert.AreEqual(0, anomalyRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length, "Zero new documents/clues allowed.");

            string[] forbiddenTerms =
            {
                "Bed", "Desk", "Chair", "Table", "Sofa", "Couch", "Cabinet", "Lamp", "Dresser",
                "Nightstand", "Counter", "Mattress", "Wardrobe", "Shelf", "Bench", "Stool",
                "Enemy", "Monster", "Creature", "NPC", "Microfauna", "Organism", "Clue", "Document", "ExitTrigger", "Portal"
            };
            foreach (Transform t in anomalyRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on '{t.name}'.");
                }
            }

            // Total scene lights remain exact 18 with 0 shadow-casting
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++)
            {
                Assert.AreEqual(LightShadows.None, allLights[i].shadows, "Zero shadow-casting lights allowed.");
            }

            // 17 & 18. Strict Surface-World Preservation: BorderCorridorScene.unity is byte-for-byte unchanged
            string actualBorderSha = ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity");
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, actualBorderSha,
                "CRITICAL VIOLATION: BorderCorridorScene.unity must remain 100% byte-for-byte identical to pre-07.11 baseline.");

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks(), "BackroomsLevel1Builder.RunVerificationChecks() must pass.");
        }

        [Test]
        public void Room111BMigration_IsSceneSpecificAndStrictlyIdempotent()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeColliders = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaRoom111BInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running 07.11 migration must not add or remove any GameObjects.");
            Assert.AreEqual(beforeColliders, root.GetComponentsInChildren<Collider>(includeInactive: true).Length,
                "Re-running 07.11 migration must preserve exact Collider count.");
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length,
                "Re-running 07.11 migration must preserve exact MeshRenderer count.");
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Re-running 07.11 migration must preserve exact Light count.");

            // Reload clean scene and verify scene-path guard rejects non-target scenes
            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaRoom111BInOpenScene(emptyScene),
                "07.11 migration must reject any scene other than BackroomsLevel1Scene.unity.");
        }
    }
}
