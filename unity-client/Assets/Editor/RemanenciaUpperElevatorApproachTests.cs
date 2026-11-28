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
    /// Dedicated automated EditMode verification suite for Implementation 07.23:
    /// Upper Elevator Approach — Alternative B (East-Side Corridor, Compact Transition Vestibule
    /// &amp; Sealed Upper Elevator Waiting Area with Two Visually Equivalent Sealed Bay Placeholders).
    /// Covers all 57 verification checks including full end-to-end CharacterController.Move()
    /// traversal from the North Upper Gallery through the East corridor, Transition Vestibule,
    /// Upper Waiting Area (testing containment at both identical sealed bays), and complete safe
    /// return down the 07.15 Northwest Service Staircase to Nivel 0, plus 4-builder cross-idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaUpperElevatorApproachTests
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

        private static bool SampleFloorAndHeadroom(float x, float z, float expectedY, float tol, out float actualY, out float headroom)
        {
            actualY = float.NaN;
            headroom = 0f;
            if (!Physics.Raycast(new Vector3(x, expectedY + 1.10f, z), Vector3.down, out RaycastHit fHit, 2.00f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            actualY = fHit.point.y;
            if (Mathf.Abs(actualY - expectedY) > tol) return false;

            if (!Physics.Raycast(new Vector3(x, actualY + 0.10f, z), Vector3.up, out RaycastHit cHit, 6.00f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            headroom = cHit.point.y - actualY;
            return true;
        }

        [Test]
        public void UpperApproach_GalleryPortalCorridorVestibuleWaitingAndIdenticalSealedBays_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. Root exists exactly once, zero MonoBehaviours, zero Unity Lights
            int rootCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                if (shell.GetChild(i).name == UpperElevatorApproachBuilder.RootName) rootCount++;
            }
            Assert.AreEqual(1, rootCount, "Hotel_UpperElevatorApproach must exist exactly once.");
            Assert.IsTrue(UpperElevatorApproachBuilder.VerifyStructurePresent(shell));

            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);
            Assert.AreEqual(0, approachRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "Upper Elevator Approach must contain zero runtime MonoBehaviours.");
            Assert.AreEqual(0, approachRoot.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Upper Elevator Approach must add zero Unity Light components.");
            Assert.AreEqual(0, approachRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length,
                "Upper Elevator Approach must contain zero Animators.");
            foreach (Collider c in approachRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsFalse(c.isTrigger, $"Upper Elevator Approach collider '{c.name}' must not be a trigger.");
            }

            // 3, 4, 5, 6, 7, 8. Authorized Bay 2 South opening (X in [+6.000, +8.400], 2.400m x 2.240m) & preserved gallery elements
            Transform galleryRoot = shell.Find(NorthUpperGalleryBuilder.RootName);
            Assert.IsNotNull(galleryRoot);
            Collider wallMid = galleryRoot.Find("NUGallery_Structure/NUGallery_WallSouth").GetComponent<Collider>();
            Collider wallEast = approachRoot.Find("UpperElevatorApproach_GalleryPortal/UpperElev_WallSouth_EastSegment").GetComponent<Collider>();
            Collider transom = approachRoot.Find("UpperElevatorApproach_GalleryPortal/UpperElev_GalleryPortal_Transom").GetComponent<Collider>();

            Assert.AreEqual(0.060f, wallMid.bounds.min.x, 0.005f, "Mid WallSouth must preserve X=+0.060 Overlook edge.");
            Assert.AreEqual(6.000f, wallMid.bounds.max.x, 0.005f, "West edge of Bay 2 South opening must equal X=+6.000m.");
            Assert.AreEqual(8.400f, wallEast.bounds.min.x, 0.005f, "East edge of Bay 2 South opening must equal X=+8.400m.");
            Assert.AreEqual(2.400f, wallEast.bounds.min.x - wallMid.bounds.max.x, 0.005f, "Portal clear width must equal 2.400m.");
            Assert.AreEqual(-44.160f, transom.bounds.min.y, 0.005f, "Portal clear header height must equal -44.160m (2.240m clear).");

            Transform galDetails = galleryRoot.Find("NUGallery_Details");
            float[] expectedBayXs = { -4.60f, 0.20f, 5.00f, 9.80f, 14.20f };
            for (int i = 0; i < expectedBayXs.Length; i++)
            {
                Assert.AreEqual(expectedBayXs[i], galDetails.Find($"NUGallery_PilasterSouth_{i}").position.x, 0.005f);
                Assert.AreEqual(expectedBayXs[i], galDetails.Find($"NUGallery_CeilingBeam_{i}").position.x, 0.005f);
            }
            for (int i = 0; i < 4; i++) Assert.IsNotNull(galDetails.Find($"NUGallery_StripTube_{i}"));

            // 9-18. East-Side Approach Corridor bounds, Lightwell clearance (+0.480m), full enclosure, zero lower overlap
            Transform corrGrp = approachRoot.Find("UpperElevatorApproach_EastCorridor");
            Collider corrFloor = corrGrp.Find("UpperElev_EastCorr_FloorSlab").GetComponent<Collider>();
            Collider corrCeil = corrGrp.Find("UpperElev_EastCorr_CeilingSlab").GetComponent<Collider>();
            Collider corrW = corrGrp.Find("UpperElev_EastCorr_WallWest").GetComponent<Collider>();
            Collider corrE = corrGrp.Find("UpperElev_EastCorr_WallEast").GetComponent<Collider>();

            Assert.AreEqual(2.400f, corrE.bounds.min.x - corrW.bounds.max.x, 0.005f, "Corridor clear width must equal 2.400m.");
            Assert.AreEqual(17.780f, corrFloor.bounds.size.z, 0.005f, "Corridor length must equal 17.780m.");
            Assert.AreEqual(-46.400f, corrFloor.bounds.max.y, 0.003f, "Corridor floor top must equal -46.400m.");
            Assert.AreEqual(0.200f, corrFloor.bounds.size.y, 0.003f, "Corridor floor thickness must equal 0.200m.");
            Assert.AreEqual(-43.550f, corrCeil.bounds.min.y, 0.003f, "Corridor ceiling soffit must equal -43.550m (2.850m headroom).");

            Collider skylightCap = GameObject.Find("NorthWing_Lightwell_SkylightCap").GetComponent<Collider>();
            Collider lwWallE = GameObject.Find("NorthWing_Lightwell_WallE").GetComponent<Collider>();
            Assert.GreaterOrEqual(corrW.bounds.min.x - skylightCap.bounds.max.x, 0.475f,
                "East Corridor West outer wall must maintain >= 0.480m solid clearance East of SkylightCap.");
            Assert.GreaterOrEqual(corrW.bounds.min.x - lwWallE.bounds.max.x, 0.595f,
                "East Corridor West outer wall must maintain >= 0.600m clearance East of Lightwell_WallE.");

            // Check no object in Hotel_UpperElevatorApproach intersects Lightwell, Lower Ceilings, or Hotel_Elevator_Core
            Collider backroomsCeil = GameObject.Find("Backrooms_Ceiling").GetComponent<Collider>();
            Collider nwCeilEast = GameObject.Find("NorthWing_CeilingSlab_East").GetComponent<Collider>();
            Collider nwCeilSouth = GameObject.Find("NorthWing_CeilingSlab_South").GetComponent<Collider>();
            Transform elevCore = GameObject.Find("Hotel_Elevator_Core").transform;
            Bounds elevCoreUnion = new Bounds(new Vector3(2.205f, -48.575f, 6.800f), new Vector3(2.52f, 2.85f, 5.33f));

            foreach (Renderer r in approachRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Bounds b = r.bounds;
                Assert.IsFalse(b.Intersects(skylightCap.bounds), $"'{r.name}' must never intersect SkylightCap.");
                Assert.IsFalse(b.Intersects(lwWallE.bounds), $"'{r.name}' must never intersect Lightwell_WallE.");
                Assert.IsFalse(b.Intersects(backroomsCeil.bounds), $"'{r.name}' must never intersect Backrooms_Ceiling.");
                Assert.IsFalse(b.Intersects(nwCeilEast.bounds), $"'{r.name}' must never intersect NorthWing_CeilingSlab_East.");
                Assert.IsFalse(b.Intersects(nwCeilSouth.bounds), $"'{r.name}' must never intersect NorthWing_CeilingSlab_South.");
                Assert.IsFalse(b.Intersects(elevCoreUnion), $"'{r.name}' must never penetrate lower Hotel_Elevator_Core.");
            }

            // Corridor enclosure linecasts along full 17.780m length
            float eyeY = -45.00f;
            for (int i = 0; i <= 12; i++)
            {
                float z = Mathf.Lerp(12.20f, 29.20f, i / 12f);
                Assert.IsTrue(SampleFloorAndHeadroom(7.20f, z, -46.400f, 0.02f, out _, out float hr), $"Missing corridor floor/ceil at Z={z:F2}.");
                Assert.GreaterOrEqual(hr, 2.55f, $"Low corridor headroom ({hr:F2}m) at Z={z:F2}.");
                Assert.IsTrue(Physics.Linecast(new Vector3(7.20f, eyeY, z), new Vector3(5.50f, eyeY, z), ~0, QueryTriggerInteraction.Ignore),
                    $"Corridor West wall gap at Z={z:F2}.");
                Assert.IsTrue(Physics.Linecast(new Vector3(7.20f, eyeY, z), new Vector3(8.90f, eyeY, z), ~0, QueryTriggerInteraction.Ignore),
                    $"Corridor East wall gap at Z={z:F2}.");
            }

            // 19-25. Transition Vestibule bounds & 2.400m North and South portals
            Transform vestGrp = approachRoot.Find("UpperElevatorApproach_TransitionVestibule");
            Collider vestFloor = vestGrp.Find("UpperElev_Vestibule_FloorSlab").GetComponent<Collider>();
            Collider vestWallW = vestGrp.Find("UpperElev_Vestibule_WallWest").GetComponent<Collider>();
            Collider vestWallE = vestGrp.Find("UpperElev_Vestibule_WallEast").GetComponent<Collider>();
            Collider vestWallN = vestGrp.Find("UpperElev_Vestibule_WallNorth").GetComponent<Collider>();
            Collider vestPierW = vestGrp.Find("UpperElev_Vestibule_SouthPortal_PierW").GetComponent<Collider>();
            Collider vestWallSE = vestGrp.Find("UpperElev_Vestibule_WallSouthEast").GetComponent<Collider>();

            Assert.AreEqual(4.990f, vestWallE.bounds.min.x - vestWallW.bounds.max.x, 0.005f, "Vestibule clear X width must equal 4.990m.");
            Assert.AreEqual(2.400f, vestFloor.bounds.size.z, 0.005f, "Vestibule clear Z depth must equal 2.400m.");
            Assert.AreEqual(-46.400f, vestFloor.bounds.max.y, 0.003f, "Vestibule floor top must match -46.400m.");
            Assert.AreEqual(2.400f, vestWallE.bounds.min.x - vestWallN.bounds.max.x, 0.005f, "North corridor entrance must equal 2.400m.");
            Assert.AreEqual(2.400f, vestWallSE.bounds.min.x - vestPierW.bounds.max.x, 0.005f, "South waiting portal must equal 2.400m.");
            Assert.IsFalse(Physics.Linecast(new Vector3(7.20f, eyeY, 12.60f), new Vector3(7.20f, eyeY, 10.60f), ~0, QueryTriggerInteraction.Ignore),
                "North entrance into Vestibule must be unobstructed.");
            Assert.IsFalse(Physics.Linecast(new Vector3(4.85f, eyeY, 10.60f), new Vector3(4.85f, eyeY, 8.40f), ~0, QueryTriggerInteraction.Ignore),
                "South portal from Vestibule into Waiting Area must be unobstructed.");

            // 26-36. Upper Waiting Area bounds, support structure & TWO 100% IDENTICAL SEALED ELEVATOR PLACEHOLDERS
            Transform waitGrp = approachRoot.Find("UpperElevatorApproach_WaitingArea");
            Collider waitFloor = waitGrp.Find("UpperElev_Waiting_FloorSlab").GetComponent<Collider>();
            Collider waitCeil = waitGrp.Find("UpperElev_Waiting_CeilingSlab").GetComponent<Collider>();
            Collider waitWallE = waitGrp.Find("UpperElev_Waiting_WallEast").GetComponent<Collider>();
            Collider waitWallS = waitGrp.Find("UpperElev_Waiting_WallSouth").GetComponent<Collider>();
            Collider waitCorePierMid = waitGrp.Find("UpperElev_Waiting_CorePierMid").GetComponent<Collider>();

            Assert.AreEqual(2.790f, waitWallE.bounds.min.x - waitCorePierMid.bounds.max.x, 0.005f, "Waiting Area clear depth X must equal 2.790m.");
            Assert.AreEqual(5.200f, UpperElevatorApproachBuilder.WaitingMaxZ - waitWallS.bounds.max.z, 0.005f, "Waiting Area clear Z width must equal 5.200m.");
            Assert.AreEqual(-46.400f, waitFloor.bounds.max.y, 0.003f);
            Assert.AreEqual(-43.550f, waitCeil.bounds.min.y, 0.003f, "Waiting Area clear headroom must equal 2.850m.");
            Assert.GreaterOrEqual(approachRoot.Find("UpperElevatorApproach_SupportStructure").childCount, 7,
                "Articulated architectural support girders must be present.");

            // Compare Bay 1 and Bay 2 placeholders strictly component-by-component for equivalence & alignment
            Transform baysGrp = approachRoot.Find("UpperElevatorApproach_SealedBayFrames");
            Transform lowerBay1Seam = elevCore.Find("Hotel_ElevatorBay_1_CenterSeam");
            Transform lowerBay2Seam = elevCore.Find("Hotel_ElevatorBay_2_CenterSeam");
            Assert.IsNotNull(lowerBay1Seam);
            Assert.IsNotNull(lowerBay2Seam);

            string[] suffixes = { "SealedBackingPanel", "FrameJambS", "FrameJambN", "FrameHeader", "FloorSill", "RecessSeamVert" };
            foreach (string sfx in suffixes)
            {
                Transform b1 = baysGrp.Find($"UpperElev_SealedBay_1_{sfx}");
                Transform b2 = baysGrp.Find($"UpperElev_SealedBay_2_{sfx}");
                Assert.IsNotNull(b1, $"Missing Bay 1 placeholder part {sfx}.");
                Assert.IsNotNull(b2, $"Missing Bay 2 placeholder part {sfx}.");
                Assert.AreEqual(b1.position.x, b2.position.x, 0.001f, $"Bay 1 & Bay 2 X mismatch on {sfx}.");
                Assert.AreEqual(b1.position.y, b2.position.y, 0.001f, $"Bay 1 & Bay 2 Y mismatch on {sfx}.");
                Assert.AreEqual(b1.localScale, b2.localScale, $"Bay 1 & Bay 2 scale mismatch on {sfx}.");
                Assert.AreEqual(b1.GetComponent<MeshRenderer>().sharedMaterial, b2.GetComponent<MeshRenderer>().sharedMaterial,
                    $"Bay 1 & Bay 2 material mismatch on {sfx} — both bays must be visually equivalent!");
                Collider c1 = b1.GetComponent<Collider>();
                Collider c2 = b2.GetComponent<Collider>();
                Assert.AreEqual(c1 != null && c1.enabled, c2 != null && c2.enabled, $"Bay 1 & Bay 2 collider mismatch on {sfx}.");
            }

            Transform b1Panel = baysGrp.Find("UpperElev_SealedBay_1_SealedBackingPanel");
            Transform b2Panel = baysGrp.Find("UpperElev_SealedBay_2_SealedBackingPanel");
            Assert.AreEqual(lowerBay1Seam.position.z, b1Panel.position.z, 0.005f, "Upper Bay 1 placeholder must align strictly with lower Bay 1 Z=+5.750.");
            Assert.AreEqual(lowerBay2Seam.position.z, b2Panel.position.z, 0.005f, "Upper Bay 2 placeholder must align strictly with lower Bay 2 Z=+7.850.");

            // Both bays are completely sealed (linecast from waiting area Westward into X = +2.50 must hit solid backing)
            foreach (float bayZ in new[] { 5.75f, 7.85f })
            {
                foreach (float testY in new[] { -45.90f, -45.30f, -44.60f })
                {
                    Assert.IsTrue(Physics.Linecast(new Vector3(4.50f, testY, bayZ), new Vector3(2.50f, testY, bayZ), out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore),
                        $"Sealed elevator placeholder gap detected at Z={bayZ:F2}, Y={testY:F2}!");
                    Assert.GreaterOrEqual(hit.point.x, 3.34f, $"Sealed bay backing penetrated past X=+3.34 at Z={bayZ:F2}.");
                }
            }
        }

        [Test]
        public void EndToEndCharacterControllerTraversal_GalleryCorridorVestibuleWaitingAndReturnToNivel0_Succeeds()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject ccGo = new GameObject("__Test0723PlayerCC__");
            try
            {
                CharacterController cc = ccGo.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.28f;
                cc.skinWidth = 0.04f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.30f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                ccGo.transform.position = new Vector3(5.00f, -46.36f, 31.30f);
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

                // 37-41. Leave North Upper Gallery via Bay 2 South portal -> full 17.78m corridor -> Vestibule -> West turn -> Waiting Area
                WalkTo(new Vector2(7.20f, 31.30f), 60);
                WalkTo(new Vector2(7.20f, 10.60f), 220);
                Assert.Less(ccGo.transform.position.z, 10.90f, $"Failed to traverse full East corridor into Vestibule (Z={ccGo.transform.position.z:F2}).");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

                WalkTo(new Vector2(4.85f, 10.60f), 60);
                Assert.Less(ccGo.transform.position.x, 5.10f, $"Failed Westward turn inside Vestibule (X={ccGo.transform.position.x:F2}).");
                WalkTo(new Vector2(4.85f, 7.85f), 60);
                Assert.Less(ccGo.transform.position.z, 8.10f, $"Failed to enter Upper Waiting Area (Z={ccGo.transform.position.z:F2}).");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

                // 42-43. Approach Bay 2 (Z=+7.85) & Bay 1 (Z=+5.75) and push West — must remain solidly contained (X > +3.65)
                for (int k = 0; k < 35; k++) cc.Move(new Vector3(-3.4f, -4.0f, 0f) * 0.04f);
                Assert.Greater(ccGo.transform.position.x, 3.65f, $"Player CC penetrated sealed Upper Bay 2 (X={ccGo.transform.position.x:F2})!");

                WalkTo(new Vector2(4.85f, 5.75f), 50);
                Assert.Less(ccGo.transform.position.z, 6.00f, "Failed to reach Bay 1 position in Waiting Area.");
                for (int k = 0; k < 35; k++) cc.Move(new Vector3(-3.4f, -4.0f, 0f) * 0.04f);
                Assert.Greater(ccGo.transform.position.x, 3.65f, $"Player CC penetrated sealed Upper Bay 1 (X={ccGo.transform.position.x:F2})!");

                // 44-48. Turn around, return through Vestibule -> East corridor -> North Upper Gallery -> descend NW Staircase to Nivel 0
                WalkTo(new Vector2(4.85f, 10.60f), 80);
                WalkTo(new Vector2(7.20f, 10.60f), 60);
                WalkTo(new Vector2(7.20f, 31.30f), 220);
                Assert.Greater(ccGo.transform.position.z, 31.00f, $"Failed to return into North Upper Gallery (Z={ccGo.transform.position.z:F2}).");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

                WalkTo(new Vector2(-10.70f, 31.30f), 220);
                Assert.Less(ccGo.transform.position.x, -10.40f, "Failed to return onto 07.15 Upper Landing.");

                float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
                float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;
                WalkTo(new Vector2(-15.05f, f2Z), 100);
                Assert.AreEqual(-48.20f, ccGo.transform.position.y, 0.12f);
                WalkTo(new Vector2(-15.05f, f1Z), 60);
                WalkTo(new Vector2(-10.70f, f1Z), 100);
                Assert.AreEqual(-50.00f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to descend NW Staircase back to Nivel 0 (got Y={ccGo.transform.position.y:F2}).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ccGo);
            }
        }

        [Test]
        public void ProtectedInvariantsCrossBuilderIdempotencyAndRejection_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 49-57. All prior upper modules, all 35 Elevator Core children, adjacent signs/elec, 10 Lightwell objects, Room 111-B, 18 lights, BorderCorridorScene
            Assert.IsTrue(NorthwestServiceStaircaseBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(NorthUpperGalleryBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(CourtyardLightwellOverlookBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(EastOverhangBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(UpperElevatorApproachBuilder.VerifyStructurePresent(shell));

            Transform elevCore = GameObject.Find("Hotel_Elevator_Core").transform;
            Assert.AreEqual(35, elevCore.childCount, "Hotel_Elevator_Core must retain all 35 untouched children.");
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorsHeader"));
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorNotice"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorJunctionBox"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorConduitRun"));
            Assert.IsNotNull(GameObject.Find("Hotel_Lobby_Beam_West"));

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
            Assert.IsTrue(root.transform.Find("Backrooms_ExitDoor_ToStreet").GetComponent<BackroomsExitDoorTrigger>().isEntryOnly);
            Assert.IsTrue(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(RemanenciaThresholdOfTrustTrigger.ExitSourceId));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource));
            Assert.AreEqual(1, new ProgressionSaveData().schemaVersion);
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"));

            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene Unity Light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // Full 4-builder upper-stratum cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23)
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);
            UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(emptyScene));
        }
    }
}
