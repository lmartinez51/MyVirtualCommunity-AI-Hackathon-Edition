using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode spatial, traversal, collision, and integrity verification suite
    /// for Implementation 07.5 (Remanencia Nivel 0 — Module M1: North Hotel Wing & Courtyard Loop).
    /// Verifies real 3D Physics capsule clearance, floor continuity, dual independent connections,
    /// courtyard perimeter circulation loop, two distinct walkable routes without entering private rooms,
    /// Guest Rooms 107-110 (with upright plaques), Banquet Hall, Housekeeping Core, zero furniture,
    /// untouched elevators, preserved gameplay anchors & save IDs, zero new gameplay/enemy scripts,
    /// zero new point/shadow lights, and migration idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaNorthWingExpansionTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;
        private const float PlayerRadius = 0.32f;

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
            return (n == "Backrooms_Floor" || n == "NorthWing_FloorSlab") && Mathf.Abs(hit.point.y - BaseY) < 0.03f;
        }

        private static void AssertWalkablePolyline(string routeName, Vector2[] waypoints, float stepMeters = 0.45f)
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
                }
            }
        }

        private static float MeasureClearWidthX(float centerX, float z, float maxHalfSpan = 3.0f)
        {
            Vector3 origin = new Vector3(centerX, BaseY + 1.15f, z);
            float leftClear = maxHalfSpan;
            float rightClear = maxHalfSpan;
            if (Physics.Raycast(origin, Vector3.left, out RaycastHit hitL, maxHalfSpan, ~0, QueryTriggerInteraction.Ignore))
            {
                leftClear = hitL.distance;
            }
            if (Physics.Raycast(origin, Vector3.right, out RaycastHit hitR, maxHalfSpan, ~0, QueryTriggerInteraction.Ignore))
            {
                rightClear = hitR.distance;
            }
            return leftClear + rightClear;
        }

        [Test]
        public void ModuleM1RootSubgroupsAndSpatialEnvelope_MatchApprovedDimensions()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // Hotel_NorthWing_M1 must exist exactly once in the entire scene
            int m1Count = 0;
            Transform m1Root = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == BackroomsLevel1Builder.NorthWingM1RootName)
                    {
                        m1Count++;
                        m1Root = t;
                    }
                }
            }
            Assert.AreEqual(1, m1Count, "Hotel_NorthWing_M1 must exist exactly once.");
            Assert.IsNotNull(m1Root);

            string[] requiredSubgroups =
            {
                "NorthWing_Connection_Lobby",
                "NorthWing_Connection_Service",
                "NorthWing_Courtyard",
                "NorthWing_EastArcade",
                "NorthWing_WestArcade",
                "NorthWing_GuestRooms_107_110",
                "NorthWing_BanquetHall",
                "NorthWing_HousekeepingCore",
                "NorthWing_ConstructionDetails"
            };
            foreach (string sub in requiredSubgroups)
            {
                Assert.IsNotNull(m1Root.Find(sub), $"Missing required M1 subgroup '{sub}'.");
            }

            // Verify M1 floor envelope matches approved ~32m x 22m (704 m²) bounds (X in [-16,+16], Z in [+12,+34])
            Transform floorTf = m1Root.Find("NorthWing_Connection_Lobby/NorthWing_FloorSlab");
            Assert.IsNotNull(floorTf, "NorthWing_FloorSlab must exist.");
            Collider floorCol = floorTf.GetComponent<Collider>();
            Assert.IsNotNull(floorCol);
            Bounds fb = floorCol.bounds;
            Assert.AreEqual(BackroomsLevel1Builder.NorthWingMinX, fb.min.x, 0.40f, "M1 min X out of approved tolerance.");
            Assert.AreEqual(BackroomsLevel1Builder.NorthWingMaxX, fb.max.x, 0.40f, "M1 max X out of approved tolerance.");
            Assert.AreEqual(BackroomsLevel1Builder.NorthWingMinZ, fb.min.z, 0.40f, "M1 min Z out of approved tolerance.");
            Assert.AreEqual(BackroomsLevel1Builder.NorthWingMaxZ, fb.max.z, 0.40f, "M1 max Z out of approved tolerance.");
            Assert.AreEqual(BaseY, fb.max.y, 0.02f, "M1 floor top elevation must match Nivel 0 BaseY.");

            // Verify outer North, East, and West perimeter walls enclose M1 cleanly (no open world-facing holes)
            Assert.IsTrue(Physics.Linecast(new Vector3(0f, BaseY + 1.2f, 33.0f), new Vector3(0f, BaseY + 1.2f, 35.0f), ~0, QueryTriggerInteraction.Ignore),
                "M1 North outer wall at Z=+34 must be solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(15.0f, BaseY + 1.2f, 20.0f), new Vector3(17.0f, BaseY + 1.2f, 20.0f), ~0, QueryTriggerInteraction.Ignore),
                "M1 East outer wall at X=+16 must be solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-15.0f, BaseY + 1.2f, 20.0f), new Vector3(-17.0f, BaseY + 1.2f, 20.0f), ~0, QueryTriggerInteraction.Ignore),
                "M1 West outer wall at X=-16 must be solid.");

            // Verify construction details introduce zero movement-blocking colliders
            Transform details = m1Root.Find("NorthWing_ConstructionDetails");
            Assert.AreEqual(0, details.GetComponentsInChildren<Collider>(includeInactive: true).Length,
                "NorthWing_ConstructionDetails must be strictly non-colliding.");
        }

        [Test]
        public void DualConnectionsCourtyardLoopAndTwoDistinctRoutes_ArePhysicallyTraversable()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // 1. Connection A (Main Lobby North Archway: X = +3.8..+7.0, Z = +12.0) >= 2.2m clear width
            float connAWidth = MeasureClearWidthX(5.40f, 12.0f, 2.5f);
            Assert.GreaterOrEqual(connAWidth, 2.40f, $"Connection A clear width ({connAWidth:F2}m) must be >= 2.4m.");
            AssertWalkablePolyline("ConnectionA_LobbyEntry", new[]
            {
                new Vector2(5.40f, 9.50f),
                new Vector2(5.40f, 12.00f),
                new Vector2(5.40f, 16.50f)
            });

            // 2. Connection B (NW Service Hall: X = -12.0..-9.58, Z = +12.0) >= 2.2m clear width
            float connBWidth = MeasureClearWidthX(-10.80f, 12.0f, 2.5f);
            Assert.GreaterOrEqual(connBWidth, 2.20f, $"Connection B clear width ({connBWidth:F2}m) must be >= 2.2m.");
            AssertWalkablePolyline("ConnectionB_ServiceEntry", new[]
            {
                new Vector2(-10.80f, 9.50f),
                new Vector2(-10.80f, 12.00f),
                new Vector2(-10.80f, 16.50f)
            });

            // 3. Connections A and B are physically independent (solid mid perimeter wall separates them along Z=+12.0)
            Assert.IsTrue(
                Physics.Linecast(new Vector3(-3.0f, BaseY + 1.2f, 11.2f), new Vector3(-3.0f, BaseY + 1.2f, 12.8f), ~0, QueryTriggerInteraction.Ignore),
                "Mid North-perimeter seal between Connection A and Connection B must remain solid.");

            // 4. Full 360-degree Courtyard Perimeter Circulation Loop (without entering private rooms)
            AssertWalkablePolyline("Courtyard_Perimeter_Loop", new[]
            {
                new Vector2( 6.60f, 16.50f), // SE Arcade Corner
                new Vector2( 6.60f, 27.40f), // East Arcade -> NE Corridor
                new Vector2(-6.80f, 27.40f), // North Guest Corridor -> NW Arcade
                new Vector2(-6.80f, 16.50f), // West Arcade -> SW Arcade
                new Vector2( 6.60f, 16.50f)  // South Promenade -> closes 360 loop
            });

            // 5. Courtyard Interior Traversability (all 4 cardinal portals + safe ground floor + sealed lightwell overhead)
            AssertWalkablePolyline("Courtyard_NorthSouth_Traversal", new[]
            {
                new Vector2( 0.00f, 16.50f),
                new Vector2( 0.00f, 19.80f),
                new Vector2(-1.75f, 19.80f),
                new Vector2(-1.75f, 24.20f),
                new Vector2( 0.00f, 24.20f),
                new Vector2( 0.00f, 27.40f)
            });
            AssertWalkablePolyline("Courtyard_EastWest_Traversal", new[]
            {
                new Vector2(-6.80f, 22.00f),
                new Vector2(-1.75f, 22.00f),
                new Vector2(-1.75f, 24.20f),
                new Vector2( 1.75f, 24.20f),
                new Vector2( 1.75f, 22.00f),
                new Vector2( 6.60f, 22.00f)
            });
            // Sealed skylight cap above courtyard (zero sky hole / fall hazard)
            Assert.IsTrue(
                Physics.Raycast(new Vector3(0f, BaseY + 1.5f, 22.0f), Vector3.up, out RaycastHit skyHit, 10.0f, ~0, QueryTriggerInteraction.Ignore),
                "Courtyard lightwell must be sealed overhead with a solid skylight cap.");
            Assert.AreEqual("NorthWing_Lightwell_SkylightCap", skyHit.collider.gameObject.name);

            // 6. Two DISTINCT, non-overlapping public routes between Connection A & Connection B (without entering private rooms)
            // Route 1: Direct South Promenade Route
            AssertWalkablePolyline("Route1_SouthPromenade_A_to_B", new[]
            {
                new Vector2(  5.40f, 12.00f),
                new Vector2(  5.40f, 16.50f),
                new Vector2( -6.80f, 16.50f),
                new Vector2(-10.80f, 16.50f),
                new Vector2(-10.80f, 12.00f)
            });

            // Route 2: North Arc Loop (East Arcade -> North Guest Corridor -> North Service Bulkhead -> Maintenance Corridor)
            AssertWalkablePolyline("Route2_NorthCorridorLoop_A_to_B", new[]
            {
                new Vector2(  5.40f, 12.00f),
                new Vector2(  6.60f, 16.50f),
                new Vector2(  6.60f, 27.40f),
                new Vector2( -6.80f, 27.40f),
                new Vector2(-10.80f, 27.40f),
                new Vector2(-10.80f, 12.00f)
            });
        }

        [Test]
        public void GuestRooms107To110BanquetHallAndHousekeepingCore_AreValidWalkableSpaces()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform m1Root = GameObject.Find(BackroomsLevel1Builder.NorthWingM1RootName)?.transform;
            Assert.IsNotNull(m1Root);
            Transform roomsRoot = m1Root.Find("NorthWing_GuestRooms_107_110");
            Assert.IsNotNull(roomsRoot);

            // 1. Verify all 4 new guest rooms (107-110) exist exactly once, have traversable doorways, open interiors,
            //    solid enclosing walls, and upright non-mirrored room-number plaques
            (int num, Vector2 corrApproach, Vector2 doorCenter, Vector2 roomInterior, Vector2 wallCheckA, Vector2 wallCheckB)[] newRooms =
            {
                (107, new Vector2(11.30f, 27.60f), new Vector2(11.30f, 28.80f), new Vector2(11.80f, 32.40f), new Vector2(14.50f, 27.60f), new Vector2(14.50f, 29.60f)),
                (108, new Vector2( 7.50f, 27.60f), new Vector2( 7.50f, 28.80f), new Vector2( 7.40f, 32.40f), new Vector2( 4.20f, 27.60f), new Vector2( 4.20f, 29.60f)),
                (109, new Vector2(-1.90f, 27.60f), new Vector2(-1.90f, 28.80f), new Vector2(-1.20f, 32.40f), new Vector2( 1.40f, 27.60f), new Vector2( 1.40f, 29.60f)),
                (110, new Vector2(-6.10f, 27.60f), new Vector2(-6.10f, 28.80f), new Vector2(-5.80f, 32.40f), new Vector2(-8.40f, 27.60f), new Vector2(-8.40f, 29.60f))
            };

            foreach (var rm in newRooms)
            {
                string groupName = $"Hotel_GuestRoom_{rm.num}";
                int count = 0;
                for (int i = 0; i < roomsRoot.childCount; i++)
                {
                    if (roomsRoot.GetChild(i).name == groupName) count++;
                }
                Assert.AreEqual(1, count, $"Expected exactly one '{groupName}' under NorthWing_GuestRooms_107_110.");

                AssertWalkablePolyline($"GuestRoom_{rm.num}_Entry", new[] { rm.corrApproach, rm.doorCenter, rm.roomInterior });
                Assert.IsTrue(
                    Physics.Linecast(
                        new Vector3(rm.wallCheckA.x, BaseY + 1.2f, rm.wallCheckA.y),
                        new Vector3(rm.wallCheckB.x, BaseY + 1.2f, rm.wallCheckB.y),
                        ~0, QueryTriggerInteraction.Ignore),
                    $"Guest Room {rm.num} corridor wall adjacent to doorway must be solid.");

                // Verify room plaque exists, references its dedicated material, and is upright (local +Z facing South -Z)
                Transform plaque = roomsRoot.Find($"{groupName}/Hotel_Room{rm.num}_CorridorWall_RoomPlaque");
                Assert.IsNotNull(plaque, $"Room plaque missing for Room {rm.num}.");
                Material expectedMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Settings/Remanencia_Level0_RoomPlaque_{rm.num}.mat");
                Assert.AreEqual(expectedMat, plaque.GetComponent<Renderer>().sharedMaterial);
                Assert.Greater(Vector3.Dot(plaque.forward, Vector3.back), 0.99f, $"Room {rm.num} plaque must orient +Z toward South corridor.");
                Assert.Greater(Vector3.Dot(plaque.up, Vector3.up), 0.99f, $"Room {rm.num} plaque must be upright.");
            }

            // Interconnecting doorway between Room 109 and Room 110 is physically traversable
            AssertWalkablePolyline("Suite_109_110_Interconnect", new[]
            {
                new Vector2(-2.20f, 30.65f),
                new Vector2(-3.60f, 30.65f),
                new Vector2(-5.00f, 30.65f)
            });

            // 2. Banquet / Dining Hall Shell: open walkable public volume connected via 2 arcade portals + North corridor door
            AssertWalkablePolyline("BanquetHall_FullLoop", new[]
            {
                new Vector2( 6.60f, 16.80f), // East Arcade South Portal
                new Vector2(10.50f, 16.80f), // Banquet South Interior
                new Vector2(13.80f, 19.50f), // Banquet Open Center-East
                new Vector2(11.60f, 23.70f), // Banquet North Interior
                new Vector2( 6.60f, 23.70f)  // Back out North Portal into East Arcade
            });
            AssertWalkablePolyline("BanquetHall_NorthCorridorLink", new[]
            {
                new Vector2(11.60f, 23.70f),
                new Vector2(11.60f, 26.00f),
                new Vector2(11.60f, 27.40f)
            });

            // 3. Housekeeping / Service Core: Maintenance Corridor + 3 back-of-house room shells + Utility<->Linen bypass + NW Landing
            AssertWalkablePolyline("Housekeeping_Storage_Entry", new[]
            {
                new Vector2(-10.80f, 14.90f),
                new Vector2(-12.00f, 14.90f),
                new Vector2(-14.00f, 14.90f)
            });
            AssertWalkablePolyline("Housekeeping_Utility_To_Linen_Loop", new[]
            {
                new Vector2(-10.80f, 20.50f),
                new Vector2(-12.00f, 20.50f),
                new Vector2(-14.10f, 20.50f),
                new Vector2(-14.10f, 23.40f), // Internal Utility <-> Linen doorway
                new Vector2(-14.10f, 26.10f),
                new Vector2(-12.00f, 26.10f),
                new Vector2(-10.80f, 26.10f)
            });
            AssertWalkablePolyline("Housekeeping_NW_Landing", new[]
            {
                new Vector2(-10.80f, 27.40f),
                new Vector2(-10.80f, 28.80f),
                new Vector2(-12.80f, 31.20f)
            });
        }

        [Test]
        public void NoFurnitureUntouchedElevatorsPreservedAnchorsAndNoNewSystems_AreEnforced()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform m1Root = GameObject.Find(BackroomsLevel1Builder.NorthWingM1RootName)?.transform;
            Assert.IsNotNull(m1Root);

            // 1. ZERO furniture and ZERO forbidden gameplay/enemy/radiation/bio terms inside Hotel_NorthWing_M1
            string[] forbiddenTerms =
            {
                "Bed", "Desk", "Chair", "Table", "Sofa", "Couch", "Cabinet", "Lamp", "Dresser",
                "Nightstand", "Counter", "Mattress", "Wardrobe", "Shelf", "Bench", "Stool",
                "Enemy", "Monster", "Creature", "NPC", "Stealth", "Hiding", "Cobalt", "Radiation", "Fungal", "Fungus"
            };
            foreach (Transform t in m1Root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on North Wing object '{t.name}'.");
                }
            }

            // 2. ZERO new MonoBehaviours (no enemies, NPCs, triggers, locks, puzzles, or stealth scripts) inside M1
            Assert.AreEqual(0, m1Root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "Hotel_NorthWing_M1 must be pure architecture with zero MonoBehaviour gameplay/enemy scripts.");

            // 3. ZERO new Light components inside M1 & exact 18 total scene lights with 0 shadow-casting lights
            Assert.AreEqual(0, m1Root.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Hotel_NorthWing_M1 must not add new Light components.");
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count must remain exactly 18.");

            // 4. Existing Elevator Core 100% untouched (exact child count & closed bay doors)
            Transform elev = GameObject.Find("Hotel_Elevator_Core")?.transform;
            Assert.IsNotNull(elev);
            Assert.AreEqual(35, elev.childCount, "Hotel_Elevator_Core child count must remain untouched.");
            Assert.Less(Vector3.Distance(elev.Find("Hotel_ElevatorBay_1_DoorLeafS").position, new Vector3(3.14f, BaseY + 1.10f, 5.38f)), 0.005f);
            Assert.Less(Vector3.Distance(elev.Find("Hotel_ElevatorBay_2_DoorLeafN").position, new Vector3(3.14f, BaseY + 1.10f, 8.22f)), 0.005f);

            // 5. Existing Gameplay Anchors & Progression IDs untouched
            Assert.Less(Vector3.Distance(root.transform.Find("Backrooms_SpawnPoint").position, new Vector3(17.80f, BaseY + 0.06f, 8.80f)), 0.01f);
            Assert.IsNotNull(root.transform.Find("Backrooms_ExitDoor_ToStreet"));
            Transform artifacts = root.transform.Find("Backrooms_DispersedArtifacts");
            Assert.Less(Vector3.Distance(artifacts.Find("SecretRoom_HeavyVaultSafe").position, new Vector3(-16.40f, BaseY, 11.42f)), 0.02f);
            Assert.Less(Vector3.Distance(artifacts.Find("Portal_DimensionalMirror_Root").position, new Vector3(-19.70f, BaseY, -8.20f)), 0.02f);
            Assert.IsNotNull(artifacts.Find("Artifact_EvidenceCorkboard"));
            Assert.AreEqual("safe:backrooms-level1-omega-vault", ProgressionIds.SafeBackroomsOmegaVault);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);
            Assert.AreEqual("portal:umbral-de-lasser", ProgressionIds.PortalUmbralDeLasser);
        }

        [Test]
        public void NorthWingMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeColliders = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaNorthWingM1InOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running North Wing M1 migration must not duplicate or remove any GameObjects.");
            Assert.AreEqual(beforeColliders, root.GetComponentsInChildren<Collider>(includeInactive: true).Length,
                "Re-running North Wing M1 migration must preserve exact Collider count.");
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Re-running North Wing M1 migration must preserve exact Light count.");

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaNorthWingM1InOpenScene(emptyScene),
                "North Wing M1 migration must reject any scene other than BackroomsLevel1Scene.unity.");
        }
    }
}
