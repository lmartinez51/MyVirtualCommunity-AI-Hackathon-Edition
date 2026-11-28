using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode spatial & architectural verification suite for Implementation 07.2.1
    /// (Remanencia Nivel 0 — Hotel de las Palomas ground-floor architecture).
    /// Validates actual 3D geometry, collision boundaries, lobby volume, elevator core safety,
    /// double-loaded corridor traversability, 6 distinct empty guest-room shells, service wing
    /// connectivity, zero added furniture, and migration idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaHotelArchitectureTests
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

        private static bool HasSolidFloorBelow(float x, float z)
        {
            return Physics.Raycast(new Vector3(x, BaseY + 1.0f, z), Vector3.down, out RaycastHit hit, 2.0f, ~0, QueryTriggerInteraction.Ignore) &&
                   hit.collider != null &&
                   hit.collider.gameObject.name == "Backrooms_Floor";
        }

        [Test]
        public void EntranceAndMainLobby_FormOpenPublicVolumeWithReceptionArchitecture()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform hotelRoot = GameObject.Find(BackroomsLevel1Builder.HotelArchitectureRootName)?.transform;
            Assert.IsNotNull(hotelRoot, "Hotel_GroundFloor_Architecture root must exist.");

            Transform entry = hotelRoot.Find("Hotel_Entrance_Vestibule");
            Transform lobby = hotelRoot.Find("Hotel_Lobby_Reception");
            Assert.IsNotNull(entry, "Hotel_Entrance_Vestibule must exist.");
            Assert.IsNotNull(lobby, "Hotel_Lobby_Reception must exist.");

            // Direct arrival sightline from Spawn (17.8, 8.8) straight West into the open Main Lobby & Elevator Lobby
            // must be completely unobstructed (old Wall_NE_SightlineBaffle removed!)
            Vector3 eyeStart = new Vector3(17.8f, BaseY + 1.55f, 8.8f);
            Vector3 lobbyWest = new Vector3(4.5f, BaseY + 1.55f, 8.8f);
            Assert.IsFalse(
                Physics.Linecast(eyeStart, lobbyWest, ~0, QueryTriggerInteraction.Ignore),
                "Arrival sightline from Spawn (17.8, 8.8) straight West into Main Lobby & Elevator Waiting Bay must be open.");

            // Walkable open Lobby & Reception floor points
            Vector2[] lobbySamples =
            {
                new Vector2(17.8f, 8.8f), // Entrance Vestibule
                new Vector2(16.2f, 8.8f), // Framed Entrance Archway
                new Vector2(13.0f, 8.8f), // East Main Lobby
                new Vector2(10.8f, 10.2f), // Under Reception Alcove Canopy
                new Vector2( 7.5f, 7.2f), // Central Lobby
                new Vector2( 5.0f, 6.8f), // Elevator Waiting Lobby
                new Vector2(12.2f, 2.5f)  // South Lobby Foyer
            };
            foreach (Vector2 pt in lobbySamples)
            {
                Assert.IsTrue(HasSolidFloorBelow(pt.x, pt.y), $"Missing floor at lobby point ({pt.x}, {pt.y}).");
                Assert.IsTrue(IsCapsuleClear(pt.x, pt.y), $"Unexpected solid obstruction in open lobby at ({pt.x}, {pt.y}).");
            }

            // Architectural Reception canopy & pilasters exist as geometry (no furniture)
            Assert.IsNotNull(lobby.Find("Hotel_Reception_CanopySoffit"), "Reception architectural canopy soffit missing.");
            Assert.IsNotNull(lobby.Find("Hotel_Reception_PilasterW"), "Reception West architectural pilaster missing.");
            Assert.IsNotNull(lobby.Find("Hotel_Reception_PilasterE"), "Reception East architectural pilaster missing.");
        }

        [Test]
        public void ElevatorCore_HasTwoIdentifiableClosedBaysAndZeroFallHazards()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform elev = GameObject.Find(BackroomsLevel1Builder.HotelArchitectureRootName)?.transform.Find("Hotel_Elevator_Core");
            Assert.IsNotNull(elev, "Hotel_Elevator_Core must exist.");

            string[] expectedParts =
            {
                "Hotel_ElevatorCore_BackWall",
                "Hotel_ElevatorCore_NorthWall",
                "Hotel_ElevatorCore_SouthWall",
                "Hotel_ElevatorBay_1_DoorLeafS",
                "Hotel_ElevatorBay_1_DoorLeafN",
                "Hotel_ElevatorBay_1_FrameHeader",
                "Hotel_ElevatorBay_1_IndicatorHousing",
                "Hotel_ElevatorBay_2_DoorLeafS",
                "Hotel_ElevatorBay_2_DoorLeafN",
                "Hotel_ElevatorBay_2_FrameHeader",
                "Hotel_ElevatorBay_2_IndicatorHousing",
                "Hotel_Elevator_CallPanelPlate"
            };
            foreach (string part in expectedParts)
            {
                Assert.IsNotNull(elev.Find(part), $"Missing elevator architectural part '{part}'.");
            }

            // Bay 1 presents closed solid elevator doors (functional LowerDoor_* in 07.29+, or legacy Hotel_ElevatorBay_* pre-07.29).
            // In 07.29+, Bay 2 (Z=7.85) is intentionally a permanently open abandoned unfinished shaft.
            bool has0729Doors = GameObject.Find(BackroomsLevel1Builder.UpperPassengerElevatorDoorsRootName) != null;
            float[] closedBayZs = has0729Doors ? new[] { 5.75f } : new[] { 5.75f, 7.85f };
            foreach (float bayZ in closedBayZs)
            {
                Vector3 lobbyEye = new Vector3(4.6f, BaseY + 1.2f, bayZ);
                bool hitDoor = Physics.Raycast(lobbyEye, Vector3.left, out RaycastHit hit, 2.2f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(hitDoor, $"Elevator bay at Z={bayZ} must present a solid closed barrier facing the lobby.");
                if (has0729Doors)
                {
                    Assert.IsTrue(
                        hit.collider.gameObject.name.StartsWith("LowerDoor_", System.StringComparison.Ordinal),
                        $"Expected closed Bay 1 telescopic door at Z={bayZ}, hit '{hit.collider.gameObject.name}'.");
                }
                else
                {
                    StringAssert.Contains("Hotel_ElevatorBay_", hit.collider.gameObject.name,
                        $"Expected solid closed elevator door blocking shaft at Z={bayZ}, hit '{hit.collider.gameObject.name}'.");
                }
            }

            // Floor slab under elevator core remains intact (zero unguarded floor void)
            Assert.IsTrue(HasSolidFloorBelow(2.2f, 6.8f), "Floor slab must remain continuous under elevator core.");
        }

        [Test]
        public void MainGuestCorridorAndSixEmptyGuestRoomShells_AreTraversableAndProperlyBounded()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform hotelRoot = GameObject.Find(BackroomsLevel1Builder.HotelArchitectureRootName)?.transform;
            Assert.IsNotNull(hotelRoot);

            // 1. Main Double-Loaded Guest-Room Corridor continuous traversal (X = +10.5 to -11.0 along Z = -0.2)
            for (float x = 10.5f; x >= -11.0f; x -= 1.5f)
            {
                Assert.IsTrue(HasSolidFloorBelow(x, -0.2f), $"Corridor missing floor at X={x:F1}.");
                Assert.IsTrue(IsCapsuleClear(x, -0.2f), $"Corridor obstructed at X={x:F1}, Z=-0.2.");
            }

            // 2. Verify all 6 distinct empty guest-room shells (Rooms 101-106):
            //    Each room must exist, have a framed doorway clear for player traversal, an open interior floor,
            //    and a solid separating wall adjacent to the doorway (proving actual room enclosure).
            (string group, Vector2 corridorSide, Vector2 doorwayCenter, Vector2 roomInterior, Vector2 solidWallCheckFrom, Vector2 solidWallCheckTo)[] rooms =
            {
                ("Hotel_GuestRoom_101", new Vector2(-0.44f,  0.5f), new Vector2(-0.44f,  1.4f), new Vector2(-1.8f,  8.5f), new Vector2(-2.5f,  0.5f), new Vector2(-2.5f,  2.5f)),
                ("Hotel_GuestRoom_102", new Vector2(-7.10f,  0.5f), new Vector2(-7.10f,  1.4f), new Vector2(-5.2f,  8.5f), new Vector2(-4.5f,  0.5f), new Vector2(-4.5f,  2.5f)),
                ("Hotel_GuestRoom_103", new Vector2(15.90f, -3.5f), new Vector2(15.90f, -4.4f), new Vector2(16.0f, -8.4f), new Vector2(18.5f, -3.5f), new Vector2(18.5f, -5.5f)),
                ("Hotel_GuestRoom_104", new Vector2(11.12f, -3.5f), new Vector2(11.12f, -4.4f), new Vector2(10.8f, -8.5f), new Vector2( 8.6f, -3.5f), new Vector2( 8.6f, -5.5f)),
                ("Hotel_GuestRoom_105", new Vector2( 4.00f, -0.8f), new Vector2( 4.00f, -1.8f), new Vector2( 4.2f, -8.2f), new Vector2( 2.2f, -0.8f), new Vector2( 2.2f, -2.8f)),
                ("Hotel_GuestRoom_106", new Vector2(-3.52f, -0.8f), new Vector2(-3.52f, -1.8f), new Vector2(-3.5f, -8.5f), new Vector2( 0.0f, -0.8f), new Vector2( 0.0f, -2.8f))
            };

            Assert.GreaterOrEqual(rooms.Length, 4, "At least 4 distinct guest-room shells are required.");
            foreach (var rm in rooms)
            {
                Transform rmTf = hotelRoot.Find(rm.group);
                Assert.IsNotNull(rmTf, $"Missing guest-room shell '{rm.group}'.");

                // Doorway threshold & room interior must be clear for capsule traversal
                Assert.IsTrue(IsCapsuleClear(rm.corridorSide.x, rm.corridorSide.y), $"{rm.group} corridor approach blocked.");
                Assert.IsTrue(IsCapsuleClear(rm.doorwayCenter.x, rm.doorwayCenter.y), $"{rm.group} doorway threshold blocked.");
                Assert.IsTrue(IsCapsuleClear(rm.roomInterior.x, rm.roomInterior.y), $"{rm.group} interior floor blocked.");
                Assert.IsTrue(HasSolidFloorBelow(rm.roomInterior.x, rm.roomInterior.y), $"{rm.group} missing floor.");

                // Line through doorway at eye height must be open
                Assert.IsFalse(
                    Physics.Linecast(
                        new Vector3(rm.corridorSide.x, BaseY + 1.2f, rm.corridorSide.y),
                        new Vector3(rm.doorwayCenter.x, BaseY + 1.2f, rm.doorwayCenter.y),
                        ~0, QueryTriggerInteraction.Ignore),
                    $"{rm.group} doorway opening must be physically traversable.");

                // Adjacent corridor wall must be solid (proving enclosed room boundary)
                Assert.IsTrue(
                    Physics.Linecast(
                        new Vector3(rm.solidWallCheckFrom.x, BaseY + 1.2f, rm.solidWallCheckFrom.y),
                        new Vector3(rm.solidWallCheckTo.x, BaseY + 1.2f, rm.solidWallCheckTo.y),
                        ~0, QueryTriggerInteraction.Ignore),
                    $"{rm.group} corridor wall adjacent to doorway must be solid.");
            }
        }

        [Test]
        public void ServiceCirculationGameplayAnchorsAndNoFurnitureConstraint_AreVerified()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // Service wing circulation points connecting Main Corridor -> N-S Service Corridor -> NW Safe -> West Utility -> SW Portal
            Vector2[] serviceSamples =
            {
                new Vector2(-11.0f,  0.0f), // N-S Service Corridor center
                new Vector2(-11.0f,  7.0f), // N-S Service Corridor North
                new Vector2(-16.4f,  8.8f), // NW Level-Omega Safe Room
                new Vector2(-14.5f, -1.6f), // West Mechanical / Signal Monitor Niche
                new Vector2( -7.4f, -1.8f), // SW Maintenance Hall Framed Entry
                new Vector2( -8.4f, -6.5f), // SW Maintenance Hall
                new Vector2(-12.5f, -7.8f), // SW Portal Bulkhead Doorway
                new Vector2(-16.5f, -8.2f)  // Far SW Dimensional Passage Chamber
            };
            foreach (Vector2 pt in serviceSamples)
            {
                Assert.IsTrue(HasSolidFloorBelow(pt.x, pt.y), $"Service path missing floor at ({pt.x}, {pt.y}).");
                Assert.IsTrue(IsCapsuleClear(pt.x, pt.y), $"Service circulation blocked at ({pt.x}, {pt.y}).");
            }

            // Confirm ZERO furniture objects were added inside Hotel_GroundFloor_Architecture
            Transform hotelRoot = GameObject.Find(BackroomsLevel1Builder.HotelArchitectureRootName)?.transform;
            Assert.IsNotNull(hotelRoot);
            string[] forbiddenFurnitureTerms = { "Bed", "Desk", "Chair", "Table", "Sofa", "Couch", "Cabinet", "Lamp", "Dresser", "Nightstand", "Counter", "Mattress" };
            foreach (Transform t in hotelRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenFurnitureTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Forbidden furniture-named object '{t.name}' found inside Hotel_GroundFloor_Architecture.");
                }
            }

            // Confirm strictly 1 pre-existing desk + 1 pre-existing chair across the entire scene (no new furniture added)
            int deskCount = 0, chairCount = 0;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == "BackroomsProp_OfficeDesk") deskCount++;
                    if (t.name == "BackroomsProp_OfficeChair") chairCount++;
                }
            }
            Assert.AreEqual(1, deskCount, "No new desks may be added.");
            Assert.AreEqual(1, chairCount, "No new chairs may be added.");
        }

        [Test]
        public void HotelArchitectureMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            int beforeCount = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaHotelArchitectureInOpenScene(scene);

            int afterCount = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int afterLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            Assert.AreEqual(beforeCount, afterCount, "Re-running hotel architecture migration must not duplicate or remove objects.");
            Assert.AreEqual(beforeLights, afterLights, "Re-running hotel architecture migration must preserve light count.");

            // Reload clean scene from disk
            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            // Non-target scene guard
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaHotelArchitectureInOpenScene(emptyScene),
                "Hotel architecture migration must reject any scene other than BackroomsLevel1Scene.unity.");
        }
    }
}
