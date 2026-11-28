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
    /// Dedicated automated EditMode verification suite for Implementation 07.24:
    /// Twin Elevator Bay Architecture (Bay 1 Finished Appearance / Bay 2 Unfinished Appearance —
    /// Static Architectural Differentiation with Zero Elevator Functionality).
    /// Covers all 44 required verification checks: Bay 1 finished doors, Bay 2 unfinished infill,
    /// relative geometric equivalence &amp; lower-core alignment, 5-builder upper-stratum cross-idempotency,
    /// full CharacterController.Move() collision &amp; return traversal, and all protected invariants.
    /// </summary>
    [TestFixture]
    public class RemanenciaUpperElevatorBayArchitectureTests
    {
        private const string ExpectedBorderCorridorSceneSha256 =
            "0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B";

        private static string ComputeFileSha256(string relativeAssetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));
            Assert.IsTrue(File.Exists(fullPath), $"Expected file missing: {fullPath}");
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        [Test]
        public void Bay1FinishedAndBay2UnfinishedFacades_AlignmentEquivalenceContrastAndSealing_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);
            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);
            Assert.IsNotNull(approachRoot);

            // 1, 8. Both bay facade roots exist exactly once, zero runtime scripts, zero triggers, zero animators, zero Lights
            Assert.IsTrue(UpperElevatorBayArchitectureBuilder.VerifyStructurePresent(shell));
            Transform bayArch = approachRoot.Find(UpperElevatorBayArchitectureBuilder.GroupName);
            int b1Count = 0, b2Count = 0;
            for (int i = 0; i < bayArch.childCount; i++)
            {
                string n = bayArch.GetChild(i).name;
                if (n == UpperElevatorBayArchitectureBuilder.Bay1RootName) b1Count++;
                if (n == UpperElevatorBayArchitectureBuilder.Bay2RootName) b2Count++;
            }
            Assert.AreEqual(1, b1Count, "UpperElevBay1_FinishedFacade must exist exactly once.");
            Assert.AreEqual(1, b2Count, "UpperElevBay2_UnfinishedFacade must exist exactly once.");
            Assert.AreEqual(0, bayArch.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "Upper bay architecture must contain zero runtime scripts.");
            Assert.AreEqual(0, bayArch.GetComponentsInChildren<Animator>(includeInactive: true).Length,
                "Upper bay architecture must contain zero Animators.");
            Assert.AreEqual(0, bayArch.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Upper bay architecture must add zero Unity Light components.");
            foreach (Collider c in bayArch.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsFalse(c.isTrigger, $"Bay collider '{c.name}' must not be a trigger.");
            }

            Transform bay1 = bayArch.Find(UpperElevatorBayArchitectureBuilder.Bay1RootName);
            Transform bay2 = bayArch.Find(UpperElevatorBayArchitectureBuilder.Bay2RootName);
            Transform elevCore = GameObject.Find("Hotel_Elevator_Core").transform;
            float lowerBay1Z = elevCore.Find("Hotel_ElevatorBay_1_CenterSeam").position.z; // +5.750
            float lowerBay2Z = elevCore.Find("Hotel_ElevatorBay_2_CenterSeam").position.z; // +7.850

            // 2, 3, 4. Bay 1 finished doors: aligned to Z=+5.750, bi-parting metal leaves overlapping with zero gap
            Transform leafS = bay1.Find("UpperElevBay1_StaticDoors/UpperElevBay1_DoorLeafS");
            Transform leafN = bay1.Find("UpperElevBay1_StaticDoors/UpperElevBay1_DoorLeafN");
            Transform seam1 = bay1.Find("UpperElevBay1_StaticDoors/UpperElevBay1_CenterSeam");
            Transform jamb1S = bay1.Find("UpperElevBay1_PerimeterFrame/UpperElevBay1_FrameJambS");
            Transform jamb1N = bay1.Find("UpperElevBay1_PerimeterFrame/UpperElevBay1_FrameJambN");
            Transform header1 = bay1.Find("UpperElevBay1_PerimeterFrame/UpperElevBay1_FrameHeader");
            Assert.IsNotNull(leafS);
            Assert.IsNotNull(leafN);
            Assert.IsNotNull(seam1);
            Assert.AreEqual(lowerBay1Z, seam1.position.z, 0.005f, "Bay 1 finished doors must align with lower Bay 1 Z=+5.750.");

            Collider leafSCol = leafS.GetComponent<Collider>();
            Collider leafNCol = leafN.GetComponent<Collider>();
            Assert.IsTrue(leafSCol.enabled && leafNCol.enabled);
            Assert.GreaterOrEqual(leafSCol.bounds.max.z, leafNCol.bounds.min.z,
                "Bay 1 closed bi-parting leaves must meet/overlap flush with zero center gap.");
            Assert.AreEqual(BackroomsLevel1Builder.ElevatorDoorMetalMatPath,
                AssetDatabase.GetAssetPath(leafS.GetComponent<MeshRenderer>().sharedMaterial),
                "Bay 1 door leaves must use Remanencia_Level0_ElevatorDoorMetal.");
            Assert.AreEqual(leafS.GetComponent<MeshRenderer>().sharedMaterial, leafN.GetComponent<MeshRenderer>().sharedMaterial);

            // 9, 10, 11. Bay 2 unfinished facade: aligned to Z=+7.850, visually distinct concrete frame + solid masonry + steel closure
            Transform infill2 = bay2.Find("UpperElevBay2_SealedInfill/UpperElevBay2_SolidMasonryInfill");
            Transform closure2 = bay2.Find("UpperElevBay2_SealedInfill/UpperElevBay2_ClosurePlate");
            Transform jamb2S = bay2.Find("UpperElevBay2_ConstructionFrame/UpperElevBay2_ConcreteJambS");
            Transform jamb2N = bay2.Find("UpperElevBay2_ConstructionFrame/UpperElevBay2_ConcreteJambN");
            Transform header2 = bay2.Find("UpperElevBay2_ConstructionFrame/UpperElevBay2_ConcreteHeader");
            Assert.IsNotNull(infill2);
            Assert.IsNotNull(closure2);
            Assert.AreEqual(lowerBay2Z, infill2.position.z, 0.005f, "Bay 2 unfinished infill must align with lower Bay 2 Z=+7.850.");
            Assert.AreEqual(lowerBay2Z, closure2.position.z, 0.005f);

            // Verify clear visual material distinction between Bay 1 (finished metal doors & steel frame) and Bay 2 (concrete frame & solid concrete/closure infill)
            Assert.AreNotEqual(leafS.GetComponent<MeshRenderer>().sharedMaterial, infill2.GetComponent<MeshRenderer>().sharedMaterial,
                "Bay 1 finished metal doors and Bay 2 unfinished masonry infill must use distinct materials.");
            Assert.AreNotEqual(jamb1S.GetComponent<MeshRenderer>().sharedMaterial, jamb2S.GetComponent<MeshRenderer>().sharedMaterial,
                "Bay 1 fitted steel frame and Bay 2 exposed concrete jambs must use distinct materials.");
            Assert.AreNotEqual(header1.GetComponent<MeshRenderer>().sharedMaterial, header2.GetComponent<MeshRenderer>().sharedMaterial);

            // Verify NO transparent materials exist anywhere on Bay 1 or Bay 2 (zero open/windowed shaft!)
            foreach (MeshRenderer mr in bayArch.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                Assert.IsNotNull(mr.sharedMaterial);
                Assert.AreNotEqual("Transparent", mr.sharedMaterial.GetTag("RenderType", false),
                    $"'{mr.name}' must be opaque — transparent shaft peering is forbidden in 07.24.");
            }

            // 15-20. Relative equivalence: identical nominal aperture width (1.370m), height (2.200m), floor/header elevation
            float bay1ClearW = jamb1N.GetComponent<Collider>().bounds.min.z - jamb1S.GetComponent<Collider>().bounds.max.z;
            float bay2ClearW = jamb2N.GetComponent<Collider>().bounds.min.z - jamb2S.GetComponent<Collider>().bounds.max.z;
            Assert.AreEqual(1.370f, bay1ClearW, 0.005f, "Bay 1 clear aperture width must equal 1.370m.");
            Assert.AreEqual(1.370f, bay2ClearW, 0.005f, "Bay 2 clear aperture width must equal 1.370m.");
            Assert.AreEqual(bay1ClearW, bay2ClearW, 0.002f, "Bay 1 and Bay 2 nominal aperture widths must match.");
            Assert.AreEqual(-44.200f, header1.GetComponent<Collider>().bounds.min.y, 0.005f);
            Assert.AreEqual(-44.200f, header2.GetComponent<Collider>().bounds.min.y, 0.005f);
            Assert.AreEqual(-46.400f, leafSCol.bounds.min.y, 0.005f);
            Assert.AreEqual(-46.400f, infill2.GetComponent<Collider>().bounds.min.y, 0.005f);

            // 5, 6, 12, 13. Exhaustive 5x5 grid linecast sweep across BOTH Bay 1 and Bay 2 proving ZERO gaps and solid backing
            float[] bayCenters = { lowerBay1Z, lowerBay2Z };
            for (int b = 0; b < bayCenters.Length; b++)
            {
                float cz = bayCenters[b];
                for (int iz = -2; iz <= 2; iz++)
                {
                    float tz = cz + iz * 0.30f; // spans [-0.60, +0.60] across 1.370m opening
                    for (int iy = 0; iy < 5; iy++)
                    {
                        float ty = Mathf.Lerp(-46.25f, -44.30f, iy / 4f);
                        Assert.IsTrue(
                            Physics.Linecast(new Vector3(4.50f, ty, tz), new Vector3(2.50f, ty, tz), out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore),
                            $"Collision gap detected in Bay {b + 1} at Z={tz:F2}, Y={ty:F2}!");
                        Assert.GreaterOrEqual(hit.point.x, 3.34f,
                            $"Bay {b + 1} penetrated past solid backing X=+3.34 at Z={tz:F2}, Y={ty:F2} (hit X={hit.point.x:F3}).");
                    }
                }
            }

            // 22. Preserved 07.23 solid backing panels still exist with enabled BoxColliders
            Transform legacyGrp = approachRoot.Find("UpperElevatorApproach_SealedBayFrames");
            Assert.IsNotNull(legacyGrp);
            Assert.IsTrue(legacyGrp.Find("UpperElev_SealedBay_1_SealedBackingPanel").GetComponent<BoxCollider>().enabled);
            Assert.IsTrue(legacyGrp.Find("UpperElev_SealedBay_2_SealedBackingPanel").GetComponent<BoxCollider>().enabled);
        }

        [Test]
        public void CharacterControllerSafety_Bay2ToBay1ApproachesContainmentAndFullReturn_Succeeds()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject ccGo = new GameObject("__Test0724PlayerCC__");
            try
            {
                CharacterController cc = ccGo.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.28f;
                cc.skinWidth = 0.04f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.30f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                ccGo.transform.position = new Vector3(4.85f, -46.36f, 10.40f);
                Physics.SyncTransforms();

                void WalkTo(Vector2 targetXZ, int maxSteps)
                {
                    const float dt = 0.04f;
                    const float speed = 3.4f;
                    for (int k = 0; k < maxSteps; k++)
                    {
                        Vector2 cur = new Vector2(ccGo.transform.position.x, ccGo.transform.position.z);
                        Vector2 diff = targetXZ - cur;
                        if (diff.magnitude < 0.12f) break;
                        Vector2 dir = diff.normalized;
                        cc.Move(new Vector3(dir.x * speed, -4.0f, dir.y * speed) * dt);
                    }
                }

                // 26. Enter waiting area, approach Bay 2 (unfinished, North) and push West -> solidly stopped
                WalkTo(new Vector2(4.40f, 7.85f), 80);
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(-3.4f, -4.0f, 0f) * 0.04f);
                Assert.Greater(ccGo.transform.position.x, 3.65f, $"Player CC penetrated Bay 2 unfinished facade (X={ccGo.transform.position.x:F3})!");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

                // 27, 28, 29, 30. Slide/walk smoothly from Bay 2 South to Bay 1 (zero snagging on protruding construction details), push West -> solidly stopped
                WalkTo(new Vector2(4.20f, 5.75f), 80);
                Assert.Less(ccGo.transform.position.z, 6.00f, $"Player CC snagged while moving from Bay 2 to Bay 1 (Z={ccGo.transform.position.z:F2})!");
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(-3.4f, -4.0f, 0f) * 0.04f);
                Assert.Greater(ccGo.transform.position.x, 3.65f, $"Player CC penetrated Bay 1 finished doors (X={ccGo.transform.position.x:F3})!");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

                // 31, 32, 33. Turn around and return cleanly through Vestibule -> East Corridor -> Gallery -> NW Staircase -> Nivel 0
                WalkTo(new Vector2(4.85f, 10.60f), 90);
                WalkTo(new Vector2(7.20f, 10.60f), 60);
                WalkTo(new Vector2(7.20f, 31.30f), 220);
                Assert.Greater(ccGo.transform.position.z, 31.00f, "Failed return to North Upper Gallery.");

                WalkTo(new Vector2(-10.70f, 31.30f), 220);
                float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
                float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;
                WalkTo(new Vector2(-15.05f, f2Z), 100);
                Assert.AreEqual(-48.20f, ccGo.transform.position.y, 0.12f);
                WalkTo(new Vector2(-15.05f, f1Z), 60);
                WalkTo(new Vector2(-10.70f, f1Z), 100);
                Assert.AreEqual(-50.00f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to descend back to Nivel 0 (got Y={ccGo.transform.position.y:F2}).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ccGo);
            }
        }

        [Test]
        public void ProtectedInvariantsAndFiveBuilderCrossIdempotency_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 34-44. All lower Elevator Core 35 children, 5 adjacent fixtures, all upper structures, 10 Lightwell objects, Room 111-B, 18 lights, BorderCorridorScene
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorsHeader"));
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorNotice"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorJunctionBox"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorConduitRun"));
            Assert.IsNotNull(GameObject.Find("Hotel_Lobby_Beam_West"));

            Assert.IsTrue(NorthwestServiceStaircaseBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(NorthUpperGalleryBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(CourtyardLightwellOverlookBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(EastOverhangBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(UpperElevatorApproachBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(UpperElevatorBayArchitectureBuilder.VerifyStructurePresent(shell));

            string[] lwNames =
            {
                "NorthWing_Lightwell_WallN", "NorthWing_Lightwell_WallS",
                "NorthWing_Lightwell_WallE", "NorthWing_Lightwell_WallW",
                "NorthWing_Lightwell_SkylightCap",
                "NorthWing_Lightwell_ConcreteBeam_0", "NorthWing_Lightwell_ConcreteBeam_1", "NorthWing_Lightwell_ConcreteBeam_2",
                "NorthWing_Lightwell_SteelPurlinN", "NorthWing_Lightwell_SteelPurlinS"
            };
            foreach (string lw in lwNames) Assert.IsNotNull(GameObject.Find(lw), $"Missing protected {lw}.");

            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_Anomaly"));
            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_GuestInterior"));
            Assert.IsNotNull(GameObject.Find("Room111B_Threshold_TriggerVolume")?.GetComponent<RemanenciaThresholdOfTrustTrigger>());
            Assert.AreEqual(1, new ProgressionSaveData().schemaVersion);
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"));

            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene Unity Light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // 21, 23, 24. Full 5-builder upper-stratum cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23 -> 07.24)
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);
            UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(scene);
            UpperElevatorBayArchitectureBuilder.MigrateUpperElevatorBayArchitectureInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => UpperElevatorBayArchitectureBuilder.MigrateUpperElevatorBayArchitectureInOpenScene(emptyScene));
        }
    }
}
