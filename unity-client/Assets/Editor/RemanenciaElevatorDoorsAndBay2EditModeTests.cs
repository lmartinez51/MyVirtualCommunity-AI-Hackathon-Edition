using System;
using System.IO;
using System.Security.Cryptography;
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
    /// Comprehensive EditMode verification suite for Implementation 07.29:
    ///   - Section 9.1: Pure C# RemanenciaElevatorDoorInterlock invariants, 3-assembly telescopic
    ///     door geometry, exact 2:1 travel ratio, permanently enabled colliders, anti-pinch
    ///     structural-collider filtering, and Bay 1 interactable Bay-2 isolation.
    ///   - Section 9.4: Isolated Future-Floor Contract tests for EnsureBay2OpenLandingOpening
    ///     with two distinct hypothetical floor elevations (zero production scene pollution).
    ///   - Section 10: Full 8-builder cross-idempotency (07.17 -> 07.29), 35-child lower core,
    ///     Bay 1/Bay 2 concrete divider preservation, exact 18 Unity lights, and BorderCorridorScene SHA-256.
    /// </summary>
    [TestFixture]
    public class RemanenciaElevatorDoorsAndBay2EditModeTests
    {
        private const string ExpectedBorderCorridorSceneSha256 =
            "0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B";

        private static string ComputeFileSha256(string relativeAssetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        [Test]
        public void PureCSharpDoorInterlock_AllEndpointsLocksReversalsAndDepartureReadiness_AreDeterministic()
        {
            // 1. Endpoint tolerance (+/- 0.002 m, never exact float equality)
            Assert.IsTrue(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-50.0018f, out var s1) && s1 == ElevatorLandingId.Lower);
            Assert.IsTrue(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-49.9982f, out var s2) && s2 == ElevatorLandingId.Lower);
            Assert.IsTrue(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-46.4383f, out var s3) && s3 == ElevatorLandingId.Upper);
            Assert.IsTrue(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-46.4418f, out var s4) && s4 == ElevatorLandingId.Upper);
            Assert.IsFalse(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-48.200f, out _));
            Assert.IsFalse(RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(-49.995f, out _));

            // 2. Lower stop: Cabin + Lower open together in 1.60 s; Upper stays Locked; wrong landing rejected
            var il = new RemanenciaElevatorDoorInterlock(-50.000f, allowUpperLandingOperation: false);
            Assert.AreEqual(ElevatorDoorState.Closed, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Locked, il.UpperState);
            Assert.AreEqual(ElevatorDoorState.Closed, il.CabinState);
            Assert.IsTrue(il.CanCabinDepart());

            Assert.IsFalse(il.RequestOpen(ElevatorLandingId.Upper), "Upper landing must not open while cabin is at lower stop.");
            Assert.IsTrue(il.RequestOpen(ElevatorLandingId.Lower));
            Assert.AreEqual(ElevatorDoorState.Opening, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Opening, il.CabinState);
            Assert.AreEqual(ElevatorDoorState.Locked, il.UpperState);
            Assert.IsFalse(il.CanCabinDepart());

            // Advance 0.80 s (halfway = 0.500)
            il.Step(0.80f, activeThresholdObstructed: false);
            Assert.AreEqual(0.50f, il.LowerOpenFraction, 0.002f);
            Assert.AreEqual(0.50f, il.CabinOpenFraction, 0.002f);
            Assert.AreEqual(0.00f, il.UpperOpenFraction, 0.0001f);

            // Interrupt mid-opening & reverse to Closing with zero positional discontinuity
            Assert.IsTrue(il.RequestClose(ElevatorLandingId.Lower, isObstructed: false));
            Assert.AreEqual(ElevatorDoorState.Closing, il.LowerState);
            il.Step(0.16f, activeThresholdObstructed: false); // -0.10 -> 0.40
            Assert.AreEqual(0.40f, il.LowerOpenFraction, 0.002f);

            // Obstruction during Closing immediately triggers Blocked -> Opening reversal from 0.40
            il.Step(0.16f, activeThresholdObstructed: true); // +0.10 -> 0.50
            Assert.AreEqual(ElevatorDoorState.Opening, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Opening, il.CabinState);
            Assert.AreEqual(0.50f, il.LowerOpenFraction, 0.002f);

            // Finish opening cleanly
            il.Step(0.80f, activeThresholdObstructed: false);
            Assert.AreEqual(ElevatorDoorState.Open, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Open, il.CabinState);
            Assert.AreEqual(1.0f, il.LowerOpenFraction, 0.0001f);

            // Close cleanly to 0.0
            Assert.IsTrue(il.RequestClose(ElevatorLandingId.Lower, isObstructed: false));
            il.Step(1.60f, activeThresholdObstructed: false);
            Assert.AreEqual(ElevatorDoorState.Closed, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Closed, il.CabinState);
            Assert.AreEqual(0.0f, il.LowerOpenFraction, 0.0001f);
            Assert.IsTrue(il.CanCabinDepart());

            // 3. Departure readiness fails if ANY collider is disabled or cabin is moving
            il.UpdateColliderStates(lowerEnabled: true, upperEnabled: true, cabinEnabled: false);
            Assert.IsFalse(il.CanCabinDepart(), "Departure must require active solid colliders on all 3 assemblies.");
            il.UpdateColliderStates(true, true, true);
            Assert.IsTrue(il.CanCabinDepart());

            il.UpdateCabinState(-48.000f, isMoving: true);
            Assert.IsFalse(il.RequestOpen(ElevatorLandingId.Lower), "Doors must never open while cabin is moving.");
            Assert.IsFalse(il.RequestOpen(ElevatorLandingId.Upper), "Doors must never open while cabin is moving.");

            // 4. Upper stop (when enabled in 07.30): Cabin doors CAN open synchronously with Upper landing while Lower stays Locked!
            il.AllowUpperLandingOperation = true;
            il.UpdateCabinState(-46.441f, isMoving: false); // within +/- 2mm of -46.440!
            Assert.AreEqual(ElevatorDoorState.Locked, il.LowerState);
            Assert.AreEqual(ElevatorDoorState.Closed, il.UpperState);
            Assert.IsFalse(il.RequestOpen(ElevatorLandingId.Lower), "Lower landing must remain locked at Upper stop.");
            Assert.IsTrue(il.RequestOpenAtCurrentStop(), "Cabin + Upper landing must open synchronously at Upper stop.");
            il.Step(1.60f, activeThresholdObstructed: false);
            Assert.AreEqual(ElevatorDoorState.Open, il.UpperState);
            Assert.AreEqual(ElevatorDoorState.Open, il.CabinState);
            Assert.AreEqual(ElevatorDoorState.Locked, il.LowerState);
            Assert.AreEqual(0f, il.LowerOpenFraction, 0.0001f);
        }

        [Test]
        public void Bay1ThreeDoorAssembliesGeometryTwoToOneRatioCollidersAndAntiPinch_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsTrue(UpperPassengerElevatorDoorsBuilder.VerifyStructurePresent(shell));

            // Exactly 1 door controller and 3 door assemblies in the entire scene (ZERO on Bay 2!)
            RemanenciaElevatorDoorController[] controllers = root.GetComponentsInChildren<RemanenciaElevatorDoorController>(includeInactive: true);
            Assert.AreEqual(1, controllers.Length, "Exactly one Bay 1 door controller must exist.");
            RemanenciaElevatorDoorAssembly[] assemblies = root.GetComponentsInChildren<RemanenciaElevatorDoorAssembly>(includeInactive: true);
            Assert.AreEqual(3, assemblies.Length, "Exactly 3 Bay 1 door assemblies (Cabin, Lower, Upper) must exist — zero on Bay 2.");

            RemanenciaElevatorDoorController ctrl = controllers[0];
            Assert.IsFalse(ctrl.allowUpperLandingOperation, "Upper Bay 1 landing must stay locked throughout 07.29.");
            Assert.IsNotNull(ctrl.cabinDoors);
            Assert.IsNotNull(ctrl.lowerLandingDoors);
            Assert.IsNotNull(ctrl.upperLandingDoors);

            // Verify exact closed & open bounds + exact 2:1 travel ratio + permanently enabled colliders on all 3 assemblies
            void VerifyAssembly(RemanenciaElevatorDoorAssembly asm, float expSlowX0, float expSlowX1, float expFastX0, float expFastX1, float expY0, float expY1)
            {
                asm.ApplyOpenFraction(0f);
                Physics.SyncTransforms();
                Assert.IsTrue(asm.AreCollidersEnabled());

                Bounds sb0 = asm.slowLeafCollider.bounds;
                Bounds fb0 = asm.fastLeafCollider.bounds;
                Assert.AreEqual(expSlowX0, sb0.min.x, 0.002f, $"{asm.assemblyId} Slow min X");
                Assert.AreEqual(expSlowX1, sb0.max.x, 0.002f, $"{asm.assemblyId} Slow max X");
                Assert.AreEqual(expFastX0, fb0.min.x, 0.002f, $"{asm.assemblyId} Fast min X");
                Assert.AreEqual(expFastX1, fb0.max.x, 0.002f, $"{asm.assemblyId} Fast max X");
                Assert.AreEqual(expY0, sb0.min.y, 0.002f, $"{asm.assemblyId} min Y");
                Assert.AreEqual(expY1, sb0.max.y, 0.002f, $"{asm.assemblyId} max Y");
                Assert.AreEqual(5.270f, sb0.min.z, 0.002f);
                Assert.AreEqual(5.850f, sb0.max.z, 0.002f);
                Assert.AreEqual(5.820f, fb0.min.z, 0.002f);
                Assert.AreEqual(6.400f, fb0.max.z, 0.002f);

                // Midpoint t = 0.50 -> exact 2:1 travel (-0.275 vs -0.550) & colliders still active!
                asm.ApplyOpenFraction(0.5f);
                Physics.SyncTransforms();
                Assert.IsTrue(asm.AreCollidersEnabled());
                Assert.AreEqual(-0.275f, asm.slowLeafCollider.bounds.min.z - sb0.min.z, 0.002f);
                Assert.AreEqual(-0.550f, asm.fastLeafCollider.bounds.min.z - fb0.min.z, 0.002f);

                // Fully open t = 1.0 -> both stacked cleanly in Z [4.720, 5.300], leaving Z [5.300, 6.400] clear!
                asm.ApplyOpenFraction(1.0f);
                Physics.SyncTransforms();
                Assert.IsTrue(asm.AreCollidersEnabled());
                Assert.AreEqual(4.720f, asm.slowLeafCollider.bounds.min.z, 0.002f);
                Assert.AreEqual(5.300f, asm.slowLeafCollider.bounds.max.z, 0.002f);
                Assert.AreEqual(4.720f, asm.fastLeafCollider.bounds.min.z, 0.002f);
                Assert.AreEqual(5.300f, asm.fastLeafCollider.bounds.max.z, 0.002f);

                // Restore closed state
                asm.ApplyOpenFraction(0f);
                Physics.SyncTransforms();
            }

            VerifyAssembly(ctrl.cabinDoors, 2.965f, 2.990f, 3.000f, 3.025f, -49.960f, -47.760f);
            VerifyAssembly(ctrl.lowerLandingDoors, 3.095f, 3.120f, 3.060f, 3.085f, -49.976f, -47.800f);
            VerifyAssembly(ctrl.upperLandingDoors, 3.095f, 3.120f, 3.060f, 3.085f, -46.400f, -44.200f);

            // Verify threshold plates & sills exact bounds & 30mm running clearance to Cabin (+3.025)
            Transform doorsRoot = shell.Find(UpperPassengerElevatorDoorsBuilder.DoorsRootName);
            Bounds lowerPlate = doorsRoot.Find("Bay1Doors_LowerLanding/LowerDoor_ThresholdPlate").GetComponent<BoxCollider>().bounds;
            Bounds upperSill = doorsRoot.Find("Bay1Doors_UpperLanding/UpperDoor_SillExtension").GetComponent<BoxCollider>().bounds;
            Assert.AreEqual(3.055f, lowerPlate.min.x, 0.001f);
            Assert.AreEqual(3.150f, lowerPlate.max.x, 0.001f);
            Assert.AreEqual(-50.000f, lowerPlate.min.y, 0.001f);
            Assert.AreEqual(-49.976f, lowerPlate.max.y, 0.001f);
            Assert.AreEqual(3.055f, upperSill.min.x, 0.001f);
            Assert.AreEqual(3.150f, upperSill.max.x, 0.001f);
            Assert.AreEqual(-46.480f, upperSill.min.y, 0.001f);
            Assert.AreEqual(-46.400f, upperSill.max.y, 0.001f);

            // Anti-pinch MUST ignore own door leaves & permanent architecture when no player is in threshold!
            Assert.IsFalse(ctrl.IsThresholdObstructed(ElevatorLandingId.Lower),
                "Lower threshold anti-pinch must NOT false-trigger on own door leaves or static architecture.");
            Assert.IsFalse(ctrl.IsThresholdObstructed(ElevatorLandingId.Upper),
                "Upper threshold anti-pinch must NOT false-trigger on own door leaves or static architecture.");

            // Bay 1 Lower Hall Interactable MUST NOT trigger when player approaches or stands in Bay 2!
            RemanenciaElevatorDoorInteractable hallInt = doorsRoot.Find("Bay1Door_Interact_LowerHallPanel").GetComponent<RemanenciaElevatorDoorInteractable>();
            Assert.IsNotNull(hallInt);
            Assert.IsTrue(hallInt.IsAvailableForPlayer(new Vector3(4.10f, -49.96f, 6.20f), Vector3.left),
                "Bay 1 hall panel must activate when standing at Bay 1 facing West.");
            Assert.IsFalse(hallInt.IsAvailableForPlayer(new Vector3(4.10f, -49.96f, 7.85f), Vector3.left),
                "Bay 1 hall panel MUST NOT trigger when standing in front of Bay 2 (Z=+7.85)!");
            Assert.IsFalse(hallInt.IsAvailableForPlayer(new Vector3(2.20f, -49.96f, 7.85f), Vector3.left),
                "Bay 1 hall panel MUST NOT trigger while inside Bay 2 shaft!");
        }

        [Test]
        public void Bay2FutureFloorContractHelper_TwoHypotheticalElevationsOpenByConstructionAndIdempotent()
        {
            // Isolated test in temporary empty root — zero production scene modification!
            GameObject tempRoot = new GameObject("__Test0729FutureFloors__");
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.ColumnConcreteMatPath);
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.IndustrialMetalMatPath);
            try
            {
                float[] testFloors = { -42.800f, -38.900f };
                float[] testHeights = { 2.200f, 2.400f };
                float[] testCeilings = { -39.950f, -35.800f };

                for (int i = 0; i < testFloors.Length; i++)
                {
                    string tag = $"Bay2_HypotheticalFloor_{i}";
                    Transform g1 = UpperPassengerElevatorDoorsBuilder.EnsureBay2OpenLandingOpening(
                        tempRoot.transform, tag, testFloors[i], testHeights[i], testCeilings[i], concreteMat, metalMat);
                    int count1 = g1.childCount;

                    // Re-run identically -> strictly idempotent (zero duplicate children!)
                    Transform g2 = UpperPassengerElevatorDoorsBuilder.EnsureBay2OpenLandingOpening(
                        tempRoot.transform, tag, testFloors[i], testHeights[i], testCeilings[i], concreteMat, metalMat);
                    Assert.AreEqual(g1, g2);
                    Assert.AreEqual(count1, g2.childCount);

                    // Verify ZERO door leaves, ZERO door controllers/interlocks, ZERO shaft intrusion (X < +3.150)
                    Assert.AreEqual(0, g2.GetComponentsInChildren<RemanenciaElevatorDoorAssembly>(true).Length);
                    Assert.AreEqual(0, g2.GetComponentsInChildren<RemanenciaElevatorDoorController>(true).Length);
                    Assert.AreEqual(0, g2.GetComponentsInChildren<RemanenciaElevatorDoorInteractable>(true).Length);

                    Physics.SyncTransforms();
                    foreach (Collider c in g2.GetComponentsInChildren<Collider>(true))
                    {
                        Assert.GreaterOrEqual(c.bounds.min.x, 3.149f, $"Future Bay 2 landing collider '{c.name}' must not intrude into shaft X < +3.150!");
                    }

                    // Verify full 1.370 m x clearHeight portal (Z in [7.165, 8.535]) is physically open!
                    float midY = testFloors[i] + testHeights[i] * 0.5f;
                    Assert.IsFalse(
                        Physics.Linecast(new Vector3(4.00f, midY, 7.85f), new Vector3(2.20f, midY, 7.85f), ~0, QueryTriggerInteraction.Ignore),
                        $"Hypothetical Bay 2 floor {i} (Y={testFloors[i]:F2}) portal must be permanently open by construction!");

                    // Verify Transom sits cleanly above portal top
                    Collider transom = g2.Find($"{tag}_Transom").GetComponent<BoxCollider>();
                    Assert.AreEqual(testFloors[i] + testHeights[i], transom.bounds.min.y, 0.002f);
                    Assert.AreEqual(testCeilings[i], transom.bounds.max.y, 0.002f);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
            }
        }

        [Test]
        public void EightBuilderCrossIdempotencyProtectedInvariantsAndExact18Lights_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");

            // Protected invariants: 35-child lower core, uncut floor, 18 lights, schemaVersion=1, BorderCorridorScene SHA-256
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.AreEqual(1, new ProgressionSaveData().schemaVersion);
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"));
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length);
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // Full 8-builder cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23 -> 07.24 -> 07.26 -> 07.27 -> 07.29)
            int beforeT = root.GetComponentsInChildren<Transform>(true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);
            UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(scene);
            UpperElevatorBayArchitectureBuilder.MigrateUpperElevatorBayArchitectureInOpenScene(scene);
            UpperPassengerElevatorShaftBuilder.MigrateUpperPassengerElevatorShaftInOpenScene(scene);
            UpperPassengerElevatorCabinBuilder.MigrateUpperPassengerElevatorCabinInOpenScene(scene);
            UpperPassengerElevatorDoorsBuilder.MigrateElevatorDoorsAndBay2OpenShaftInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(true).Length);

            // Confirm Bay 2 stayed permanently open after full 8-builder chain!
            Physics.SyncTransforms();
            Assert.IsFalse(Physics.Linecast(new Vector3(4.20f, -48.90f, 7.85f), new Vector3(2.20f, -48.90f, 7.85f), ~0, QueryTriggerInteraction.Ignore),
                "Lower Bay 2 must remain open after 8-builder cross-idempotency.");
            Assert.IsFalse(Physics.Linecast(new Vector3(4.20f, -45.30f, 7.85f), new Vector3(2.20f, -45.30f, 7.85f), ~0, QueryTriggerInteraction.Ignore),
                "Upper Bay 2 must remain open after 8-builder cross-idempotency.");
        }
    }
}
