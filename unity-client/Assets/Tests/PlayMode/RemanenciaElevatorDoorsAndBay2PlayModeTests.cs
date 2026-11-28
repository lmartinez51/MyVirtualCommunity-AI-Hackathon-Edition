#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Tests.PlayMode
{
    /// <summary>
    /// Real Unity PlayMode integration tests for Implementation 07.29 using BackroomsLevel1Scene
    /// and an actual Unity CharacterController capsule (height=1.80, radius=0.28, skinWidth=0.04, stepOffset=0.30).
    ///
    /// Covers:
    ///   - Section 9.2: Full Bay 1 lower-level open -> board -> close -> reopen from inside -> exit
    ///     traversal across the lower threshold in BOTH directions, upper landing locked, cabin stationary,
    ///     plus real CharacterController anti-pinch reversal in all 4 threshold obstruction positions.
    ///   - Section 9.3: Real CharacterController entry into permanently open Bay 2 from lower floor,
    ///     movement inside shaft, concrete divider containment (zero passage to Bay 1), entry from upper floor,
    ///     uninterrupted free-fall down Bay 2 shaft to genuine Backrooms_Floor termination (-50.000),
    ///     zero Bay 2 door prompts, zero Bay 2 cabin, and zero Bay 2 door controller.
    /// </summary>
    public class RemanenciaElevatorDoorsAndBay2PlayModeTests
    {
        private static GameObject SpawnPlayerCC(Vector3 worldPos, out CharacterController cc)
        {
            GameObject go = new GameObject("__PlayMode0729PlayerCC__");
            cc = go.AddComponent<CharacterController>();
            cc.height = 1.80f;
            cc.radius = 0.28f;
            cc.skinWidth = 0.04f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.30f;
            cc.center = new Vector3(0f, 0.90f, 0f);
            go.transform.position = worldPos;
            Physics.SyncTransforms();
            return go;
        }

        private static IEnumerator WalkTowardsXZ(CharacterController cc, Vector2 targetXZ, int maxFrames, float speed = 3.2f)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                Vector2 cur = new Vector2(cc.transform.position.x, cc.transform.position.z);
                Vector2 diff = targetXZ - cur;
                if (diff.magnitude < 0.08f) yield break;
                Vector2 dir = diff.normalized;
                cc.Move(new Vector3(dir.x * speed, -4.5f, dir.y * speed) * 0.025f);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Bay1LowerLevelSequence_PromptOpenBoardCloseReopenExitAndUpperLocked_Succeeds()
        {
            yield return SceneManager.LoadSceneAsync("BackroomsLevel1Scene", LoadSceneMode.Single);
            yield return null;

            var controller = Object.FindFirstObjectByType<RemanenciaElevatorDoorController>();
            Assert.IsNotNull(controller, "RemanenciaElevatorDoorController missing in BackroomsLevel1Scene.");
            Transform cabinRoot = controller.cabinRoot;
            Assert.IsNotNull(cabinRoot);
            float initialCabinY = cabinRoot.position.y;
            Assert.AreEqual(-50.000f, initialCabinY, 0.002f);

            var interactables = Object.FindObjectsByType<RemanenciaElevatorDoorInteractable>(FindObjectsSortMode.None);
            RemanenciaElevatorDoorInteractable hallInt = null;
            RemanenciaElevatorDoorInteractable cabinInt = null;
            foreach (var it in interactables)
            {
                if (it.zone == ElevatorDoorInteractZone.LowerHallPanel) hallInt = it;
                if (it.zone == ElevatorDoorInteractZone.CabinInterior) cabinInt = it;
            }
            Assert.IsNotNull(hallInt);
            Assert.IsNotNull(cabinInt);

            GameObject playerGo = SpawnPlayerCC(new Vector3(4.15f, -49.96f, 6.05f), out CharacterController cc);
            try
            {
                // 1 & 2. Approach Bay 1 lower hall panel -> receives correct prompt (and NOT when standing at Bay 2!)
                Assert.IsTrue(hallInt.IsAvailableForPlayer(playerGo.transform.position, Vector3.left));
                Assert.AreEqual("[E] Open elevator doors", hallInt.GetPromptText(false));
                Assert.AreEqual("[E] Abrir puertas del elevador", hallInt.GetPromptText(true));
                Assert.IsFalse(hallInt.IsAvailableForPlayer(new Vector3(4.15f, -49.96f, 7.85f), Vector3.left),
                    "Player at Bay 2 must never receive Bay 1 hall prompt.");

                // 3. Open lower landing + cabin doors synchronously (1.60 s)
                Assert.IsTrue(hallInt.Interact());
                for (int f = 0; f < 40; f++)
                {
                    controller.StepController(0.05f);
                    yield return null;
                }
                Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.LowerState);
                Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.CabinState);
                Assert.AreEqual(ElevatorDoorState.Locked, controller.Interlock.UpperState, "Upper Bay 1 must remain Locked in 07.29.");
                Assert.AreEqual(0f, controller.upperLandingDoors.OpenFraction, 0.0001f);
                Assert.AreEqual(initialCabinY, cabinRoot.position.y, 0.0001f, "Cabin must remain stationary in 07.29.");

                // 4 & 10. Walk actual CharacterController West across lower threshold into cabin!
                yield return WalkTowardsXZ(cc, new Vector2(2.15f, 5.85f), 140);
                Assert.Less(playerGo.transform.position.x, 2.35f,
                    $"CharacterController failed to cross lower threshold into cabin (X={playerGo.transform.position.x:F3})!");
                Assert.AreEqual(-49.96f, playerGo.transform.position.y, 0.08f);

                // 5. Inside cabin -> close doors cleanly
                Assert.IsTrue(cabinInt.IsAvailableForPlayer(playerGo.transform.position, Vector3.right));
                Assert.AreEqual("[E] Close doors", cabinInt.GetPromptText(false));
                Assert.IsTrue(cabinInt.Interact());
                for (int f = 0; f < 40; f++)
                {
                    controller.StepController(0.05f);
                    yield return null;
                }
                Assert.AreEqual(ElevatorDoorState.Closed, controller.Interlock.CabinState);
                Assert.AreEqual(ElevatorDoorState.Closed, controller.Interlock.LowerState);
                Assert.IsTrue(controller.AreAllDoorsClosedAndLockedForTravel);

                // 6. Reopen doors from inside the cabin!
                Assert.IsTrue(cabinInt.IsAvailableForPlayer(playerGo.transform.position, Vector3.right));
                Assert.AreEqual("[E] Open doors", cabinInt.GetPromptText(false));
                Assert.IsTrue(cabinInt.Interact());
                for (int f = 0; f < 40; f++)
                {
                    controller.StepController(0.05f);
                    yield return null;
                }
                Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.CabinState);

                // 7 & 10. Walk actual CharacterController East back out across lower threshold into lobby!
                yield return WalkTowardsXZ(cc, new Vector2(4.15f, 5.85f), 140);
                Assert.Greater(playerGo.transform.position.x, 3.95f,
                    $"CharacterController failed to walk back out across lower threshold (X={playerGo.transform.position.x:F3})!");
                Assert.AreEqual(-50.00f, playerGo.transform.position.y, 0.08f);

                // 8 & 9. Upper landing never unlocked and cabin never moved
                Assert.IsFalse(controller.RequestOpenLanding(ElevatorLandingId.Upper));
                Assert.AreEqual(ElevatorDoorState.Locked, controller.Interlock.UpperState);
                Assert.AreEqual(initialCabinY, cabinRoot.position.y, 0.0001f);
            }
            finally
            {
                Object.Destroy(playerGo);
            }
        }

        [UnityTest]
        public IEnumerator Bay1AntiPinch_PlayerInDoorwayPartialOrEnteringWhileClosing_ReopensDoors()
        {
            yield return SceneManager.LoadSceneAsync("BackroomsLevel1Scene", LoadSceneMode.Single);
            yield return null;

            var controller = Object.FindFirstObjectByType<RemanenciaElevatorDoorController>();
            Assert.IsNotNull(controller);

            // Open lower doors fully first
            Assert.IsTrue(controller.RequestOpenLanding(ElevatorLandingId.Lower));
            for (int f = 0; f < 40; f++) { controller.StepController(0.05f); yield return null; }
            Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.LowerState);

            GameObject playerGo = SpawnPlayerCC(new Vector3(4.20f, -49.96f, 5.85f), out CharacterController cc);
            try
            {
                // Test 3 static obstruction positions: centered in doorway, partly inside cabin, partly outside in lobby
                float[] blockedXs = { 3.06f, 2.86f, 3.26f };
                foreach (float bx in blockedXs)
                {
                    cc.enabled = false;
                    playerGo.transform.position = new Vector3(bx, -49.96f, 5.85f);
                    Physics.SyncTransforms();
                    cc.enabled = true;
                    yield return null;

                    Assert.IsTrue(controller.IsThresholdObstructed(ElevatorLandingId.Lower),
                        $"Anti-pinch failed to detect player capsule at X={bx:F2}!");
                    Assert.IsFalse(controller.RequestCloseLanding(ElevatorLandingId.Lower));
                    controller.StepController(0.05f);
                    Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.LowerState);
                }

                // Test 4: Player enters doorway MID-CLOSING -> doors immediately reverse and reopen to 1.0!
                cc.enabled = false;
                playerGo.transform.position = new Vector3(4.15f, -49.96f, 5.85f);
                Physics.SyncTransforms();
                cc.enabled = true;
                yield return null;

                Assert.IsFalse(controller.IsThresholdObstructed(ElevatorLandingId.Lower));
                Assert.IsTrue(controller.RequestCloseLanding(ElevatorLandingId.Lower));
                for (int f = 0; f < 14; f++) { controller.StepController(0.05f); yield return null; } // ~0.70s closed (~0.56 open)
                Assert.AreEqual(ElevatorDoorState.Closing, controller.Interlock.LowerState);
                Assert.Less(controller.Interlock.LowerOpenFraction, 0.75f);

                // Player steps into closing threshold
                cc.enabled = false;
                playerGo.transform.position = new Vector3(3.06f, -49.96f, 5.85f);
                Physics.SyncTransforms();
                cc.enabled = true;
                yield return null;

                controller.StepController(0.05f);
                Assert.AreEqual(ElevatorDoorState.Opening, controller.Interlock.LowerState,
                    "Doors MUST immediately reverse to Opening when player enters closing threshold!");
                Assert.IsTrue(controller.lowerLandingDoors.AreCollidersEnabled(), "Leaf colliders must stay enabled during anti-pinch.");

                for (int f = 0; f < 35; f++) { controller.StepController(0.05f); yield return null; }
                Assert.AreEqual(ElevatorDoorState.Open, controller.Interlock.LowerState);
                Assert.AreEqual(1f, controller.Interlock.LowerOpenFraction, 0.001f);
            }
            finally
            {
                Object.Destroy(playerGo);
            }
        }

        [UnityTest]
        public IEnumerator Bay2PermanentlyOpenUnfinishedShaft_LowerEntryUpperFallAndDividerIsolation_Succeeds()
        {
            yield return SceneManager.LoadSceneAsync("BackroomsLevel1Scene", LoadSceneMode.Single);
            yield return null;

            // Verify zero Bay 2 cabin, zero Bay 2 door assembly/controller, zero Bay 2 interactable!
            foreach (var asm in Object.FindObjectsByType<RemanenciaElevatorDoorAssembly>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(asm.name.Contains("Bay2"), $"Bay 2 must never have a door assembly ({asm.name}).");
            }
            foreach (var inter in Object.FindObjectsByType<RemanenciaElevatorDoorInteractable>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(inter.IsAvailableForPlayer(new Vector3(4.10f, -49.96f, 7.85f), Vector3.left));
                Assert.IsFalse(inter.IsAvailableForPlayer(new Vector3(4.10f, -46.36f, 7.85f), Vector3.left));
                Assert.IsFalse(inter.IsAvailableForPlayer(new Vector3(2.20f, -49.96f, 7.85f), Vector3.left));
            }
            Assert.IsNull(GameObject.Find("Bay2_Cabin"), "Bay 2 must never contain a cabin.");

            // 1. Lower Bay 2 entry, movement inside shaft, and concrete divider containment (zero passage to Bay 1!)
            GameObject playerGo = SpawnPlayerCC(new Vector3(4.15f, -49.96f, 7.85f), out CharacterController cc);
            try
            {
                yield return WalkTowardsXZ(cc, new Vector2(2.15f, 7.85f), 120);
                Assert.Less(playerGo.transform.position.x, 2.35f,
                    $"CharacterController failed to enter permanently open Lower Bay 2 (X={playerGo.transform.position.x:F3})!");
                Assert.AreEqual(-50.00f, playerGo.transform.position.y, 0.08f);

                // Move freely inside Bay 2 shaft North & South, then push hard South against Bay 1/2 concrete divider (Z=+6.920)
                yield return WalkTowardsXZ(cc, new Vector2(2.15f, 8.35f), 60);
                Assert.Greater(playerGo.transform.position.z, 8.20f);
                for (int k = 0; k < 50; k++)
                {
                    cc.Move(new Vector3(0f, -4.5f, -3.5f) * 0.025f);
                    yield return null;
                }
                Assert.Greater(playerGo.transform.position.z, 7.15f,
                    $"Bay 1 / Bay 2 concrete divider breached! Player moved to Z={playerGo.transform.position.z:F3}!");

                // 2. Upper Bay 2 entry -> physical uninterrupted free-fall down the open shaft to Backrooms_Floor (-50.000)!
                cc.enabled = false;
                playerGo.transform.position = new Vector3(4.15f, -46.36f, 7.85f);
                Physics.SyncTransforms();
                cc.enabled = true;
                yield return null;

                // Walk West through permanently open Upper Bay 2 opening into the shaft void (X = +4.15 -> +2.20)
                float vy = 0f;
                bool passedCeilingPlane = false;
                for (int frame = 0; frame < 180; frame++)
                {
                    float dt = 0.025f;
                    float vx = playerGo.transform.position.x > 2.25f ? -3.0f : 0f;
                    vy -= 9.81f * dt;
                    CollisionFlags flags = cc.Move(new Vector3(vx, vy, 0f) * dt);
                    if ((flags & CollisionFlags.Below) != 0 && vy < 0f)
                    {
                        vy = -1.5f;
                    }
                    if (playerGo.transform.position.y < -47.50f)
                    {
                        passedCeilingPlane = true;
                    }
                    if (passedCeilingPlane && playerGo.transform.position.y <= -49.92f)
                    {
                        break;
                    }
                    yield return null;
                }

                Assert.Less(playerGo.transform.position.x, 3.00f,
                    $"Player failed to enter Upper Bay 2 open doorway (X={playerGo.transform.position.x:F3})!");
                Assert.IsTrue(passedCeilingPlane,
                    $"Player fall in Bay 2 was interrupted above Y=-47.50 (stopped at Y={playerGo.transform.position.y:F3})!");
                Assert.AreEqual(-50.00f, playerGo.transform.position.y, 0.08f,
                    $"Player did not reach genuine Backrooms_Floor termination at Y=-50.000 (got Y={playerGo.transform.position.y:F3})!");

                // Walk cleanly back out of Lower Bay 2 into the ground-floor lobby!
                yield return WalkTowardsXZ(cc, new Vector2(4.15f, 7.85f), 120);
                Assert.Greater(playerGo.transform.position.x, 3.95f,
                    $"Player could not exit Lower Bay 2 back to lobby after falling (X={playerGo.transform.position.x:F3})!");
            }
            finally
            {
                Object.Destroy(playerGo);
            }
        }
    }
}
#endif
