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
    /// Dedicated automated EditMode verification suite for Implementation 07.17:
    /// North Upper Gallery (first controlled fragment of Approved Alternative 1).
    /// Covers all 17 verification requirements including full end-to-end CharacterController.Move()
    /// ascent, East-opening traversal, gallery walk to sealed Eastern terminal, and complete return
    /// down the 07.15 Northwest Service Staircase to Nivel 0.
    /// </summary>
    [TestFixture]
    public class RemanenciaNorthUpperGalleryTests
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

        private static bool IsPlayerCapsuleClearAt(Vector3 footPos)
        {
            const float radius = 0.30f;
            Vector3 pBot = footPos + Vector3.up * (radius + 0.14f);
            Vector3 pTop = footPos + Vector3.up * (1.80f - radius - 0.04f);
            Collider[] hits = Physics.OverlapCapsule(pBot, pTop, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].enabled && !hits[i].isTrigger) return false;
            }
            return true;
        }

        [Test]
        public void GalleryStructure_EnvelopeDimensionsOpeningFramesAndSealing_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. Gallery root exists exactly once
            int rootCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                if (shell.GetChild(i).name == NorthUpperGalleryBuilder.RootName) rootCount++;
            }
            Assert.AreEqual(1, rootCount, "Hotel_NorthUpperGallery must exist exactly once.");
            Assert.IsTrue(NorthUpperGalleryBuilder.VerifyStructurePresent(shell));

            Transform galleryRoot = shell.Find(NorthUpperGalleryBuilder.RootName);
            Assert.AreEqual(0, galleryRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "North Upper Gallery must contain zero runtime MonoBehaviours.");
            Assert.AreEqual(0, galleryRoot.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "North Upper Gallery must add zero Unity Light components.");

            // 3 & 4. Preserved 07.15 East continuation frames & exact 2.04m x 2.24m opening
            Transform upperShell = shell.Find($"{NorthwestServiceStaircaseBuilder.RootName}/NWStair_UpperLandingShell");
            Assert.IsNotNull(upperShell);
            Transform fS = upperShell.Find("NWStair_UpperShell_SealedEastFrameS");
            Transform fN = upperShell.Find("NWStair_UpperShell_SealedEastFrameN");
            Transform fTop = upperShell.Find("NWStair_UpperShell_SealedEastFrameTop");
            Assert.IsNotNull(fS);
            Assert.IsNotNull(fN);
            Assert.IsNotNull(fTop);
            float clearOpeningZ = (fN.position.z - fN.localScale.z * 0.5f) - (fS.position.z + fS.localScale.z * 0.5f);
            float clearOpeningH = (fTop.position.y - fTop.localScale.y * 0.5f) - NorthUpperGalleryBuilder.GalleryFloorY;
            Assert.AreEqual(2.04f, clearOpeningZ, 0.01f, "East doorway clear width must match 2.04m.");
            Assert.AreEqual(2.24f, clearOpeningH, 0.01f, "East doorway clear height must match 2.24m.");

            // Opening is traversable at Z = 31.30 while South & North East-wall piers and transom remain solid
            float eyeY = NorthUpperGalleryBuilder.GalleryFloorY + 1.20f;
            Assert.IsFalse(Physics.Linecast(new Vector3(-10.50f, eyeY, 31.30f), new Vector3(-8.50f, eyeY, 31.30f), ~0, QueryTriggerInteraction.Ignore),
                "Authorized East doorway must be open for traversal.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.50f, eyeY, 29.50f), new Vector3(-8.50f, eyeY, 29.50f), ~0, QueryTriggerInteraction.Ignore),
                "South pier of East wall must remain solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.50f, eyeY, 33.10f), new Vector3(-8.50f, eyeY, 33.10f), ~0, QueryTriggerInteraction.Ignore),
                "North pier of East wall must remain solid.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.50f, -43.85f, 31.30f), new Vector3(-8.50f, -43.85f, 31.30f), ~0, QueryTriggerInteraction.Ignore),
                "Transom above East doorway must remain solid.");

            // 5, 6, 7. Continuous flush floor (-46.400m), 3.00m clear width, strictly inside approved envelope (no East Overhang!)
            Collider landingCol = upperShell.Find("NWStair_UpperLanding_FloorSlab").GetComponent<Collider>();
            Collider galleryFloorCol = galleryRoot.Find("NUGallery_Structure/NUGallery_FloorSlab").GetComponent<Collider>();
            Assert.AreEqual(landingCol.bounds.max.y, galleryFloorCol.bounds.max.y, 0.002f,
                "Gallery floor must join 07.15 upper landing with zero vertical lip.");
            Assert.AreEqual(-46.40f, galleryFloorCol.bounds.max.y, 0.003f);

            Collider wallN = galleryRoot.Find("NUGallery_Structure/NUGallery_WallNorth").GetComponent<Collider>();
            Collider wallS = galleryRoot.Find("NUGallery_Structure/NUGallery_WallSouth").GetComponent<Collider>();
            float actualClearWidth = wallN.bounds.min.z - wallS.bounds.max.z;
            Assert.AreEqual(3.00f, actualClearWidth, 0.01f, "Gallery clear width must equal 3.00m.");

            // 6 & 10. All gallery renderers stay strictly inside X in [-9.80, +16.20], Z in [29.40, 34.20] (no Lightwell intersection, no East Overhang)
            Collider lightwellCap = GameObject.Find("NorthWing_Lightwell_SkylightCap")?.GetComponent<Collider>();
            Assert.IsNotNull(lightwellCap, "Protected Courtyard Lightwell SkylightCap must remain intact.");
            foreach (Renderer r in galleryRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Bounds b = r.bounds;
                Assert.GreaterOrEqual(b.min.x, -9.80f, $"Gallery object '{r.name}' extends West outside authorized envelope.");
                Assert.LessOrEqual(b.max.x, 16.20f, $"Gallery object '{r.name}' extends East past X=+16.00 (East Overhang is deferred!).");
                Assert.GreaterOrEqual(b.min.z, 29.40f, $"Gallery object '{r.name}' extends South toward Lightwell (minZ={b.min.z:F2}).");
                Assert.LessOrEqual(b.max.z, 34.20f, $"Gallery object '{r.name}' extends North outside North Wing.");
                Assert.IsFalse(b.Intersects(lightwellCap.bounds), $"Gallery object '{r.name}' must never intersect Courtyard Lightwell.");
            }
            Assert.GreaterOrEqual(wallS.bounds.min.z - lightwellCap.bounds.max.z, 3.00f,
                "Gallery South outer wall must remain well clear North of Courtyard Lightwell SkylightCap.");

            // 8, 9, 11. Continuous floor support, >= 2.20m portal / 2.85m gallery headroom, solid North/South/East terminal enclosure
            for (int i = 0; i <= 25; i++)
            {
                float x = Mathf.Lerp(-9.60f, 15.40f, i / 25f);
                Assert.IsTrue(SampleFloorAndHeadroom(x, 31.30f, -46.40f, 0.03f, out float fy, out float hr),
                    $"Gallery floor/ceiling missing at X={x:F2}.");
                float minExpectedHr = x < -9.20f ? 2.20f : 2.75f;
                Assert.GreaterOrEqual(hr, minExpectedHr, $"Insufficient headroom ({hr:F2}m) at X={x:F2}.");
                Assert.IsTrue(IsPlayerCapsuleClearAt(new Vector3(x, fy, 31.30f)), $"Unexpected blocking collision at X={x:F2}.");

                if (x >= -9.00f)
                {
                    Assert.IsTrue(Physics.Linecast(new Vector3(x, eyeY, 31.30f), new Vector3(x, eyeY, 33.40f), ~0, QueryTriggerInteraction.Ignore),
                        $"Gallery North wall gap detected at X={x:F2}.");
                    Assert.IsTrue(Physics.Linecast(new Vector3(x, eyeY, 31.30f), new Vector3(x, eyeY, 29.20f), ~0, QueryTriggerInteraction.Ignore),
                        $"Gallery South wall gap detected at X={x:F2}.");
                }
            }

            // 9. Sealed Eastern terminal wall blocks passage across full Z width
            foreach (float testZ in new[] { 30.20f, 31.30f, 32.40f })
            {
                Assert.IsTrue(Physics.Linecast(new Vector3(15.20f, eyeY, testZ), new Vector3(16.80f, eyeY, testZ), ~0, QueryTriggerInteraction.Ignore),
                    $"Eastern terminal wall must be solidly sealed at Z={testZ:F2}.");
            }
        }

        [Test]
        public void EndToEndCharacterControllerTraversal_StairAscentGalleryWalkAndReturn_Succeeds()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // 12, 13, 14. Full physical CharacterController.Move() route:
            // Nivel 0 -> Staircase F1 -> MidLanding -> Staircase F2 -> Upper Landing -> East Opening ->
            // Full North Upper Gallery -> Sealed Eastern Terminal -> Turn around -> Back to Nivel 0!
            float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
            float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;

            GameObject ccGo = new GameObject("__Test0717PlayerCC__");
            try
            {
                CharacterController cc = ccGo.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.28f;
                cc.skinWidth = 0.04f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.30f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                ccGo.transform.position = new Vector3(-10.70f, -49.96f, f1Z);
                Physics.SyncTransforms();

                void WalkTo(Vector2 targetXZ, int maxSteps)
                {
                    const float dt = 0.04f;
                    const float speed = 3.4f;
                    for (int k = 0; k < maxSteps; k++)
                    {
                        Vector2 curXZ = new Vector2(ccGo.transform.position.x, ccGo.transform.position.z);
                        Vector2 diff = targetXZ - curXZ;
                        if (diff.magnitude < 0.12f) break;
                        Vector2 dir = diff.normalized;
                        cc.Move(new Vector3(dir.x * speed, -4.0f, dir.y * speed) * dt);
                    }
                }

                // Ascend 07.15 staircase to Upper Landing
                WalkTo(new Vector2(-15.05f, f1Z), 90);
                Assert.AreEqual(-48.20f, ccGo.transform.position.y, 0.12f);
                WalkTo(new Vector2(-15.05f, f2Z), 60);
                WalkTo(new Vector2(-10.70f, 31.30f), 100);
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.12f);

                // Walk straight East through East opening and across the entire North Upper Gallery to the sealed terminal
                WalkTo(new Vector2(15.20f, 31.30f), 260);
                Assert.Greater(ccGo.transform.position.x, 14.90f,
                    $"Player CC failed to reach Eastern end of North Upper Gallery (stopped at X={ccGo.transform.position.x:F2}).");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f,
                    "Player Y elevation drifted unexpectedly along North Upper Gallery.");

                // Attempt to push past the sealed Eastern terminal wall — must be physically stopped inside X < 15.87!
                for (int k = 0; k < 40; k++)
                {
                    cc.Move(new Vector3(3.4f, -4.0f, 0f) * 0.04f);
                }
                Assert.Less(ccGo.transform.position.x, 15.65f,
                    $"Player CC penetrated sealed Eastern terminal wall (X={ccGo.transform.position.x:F2})!");

                // Turn around, return through East opening onto 07.15 Upper Landing, and descend back to Nivel 0
                WalkTo(new Vector2(-10.70f, 31.30f), 260);
                Assert.Less(ccGo.transform.position.x, -10.50f, "Player CC failed to return through East opening onto Upper Landing.");
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.08f);

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
        public void ProtectedInvariantsLightsIdempotencyAndSurfaceWorld_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 15. Exact 18 Unity Light components (0 shadow-casting)
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene Unity Light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            // 16. Elevators, Rooms 107-110, Room 110 Zone D, Room 111-B, Lightwell & Threshold of Trust intact
            Transform elev = GameObject.Find("Hotel_Elevator_Core")?.transform;
            Assert.IsNotNull(elev);
            Assert.AreEqual(35, elev.childCount);
            Assert.IsNotNull(GameObject.Find("Hotel_GuestRoom_107"));
            Assert.IsNotNull(GameObject.Find("Hotel_GuestRoom_108"));
            Assert.IsNotNull(GameObject.Find("Hotel_GuestRoom_109"));
            Assert.IsNotNull(GameObject.Find("Hotel_GuestRoom_110"));
            Assert.IsNotNull(GameObject.Find("Eco_Room110_ChaseRiser_FungalSprout"));
            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_Anomaly"));
            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_GuestInterior"));
            Assert.IsNotNull(GameObject.Find("Room111B_Threshold_TriggerVolume")?.GetComponent<RemanenciaThresholdOfTrustTrigger>());
            Assert.IsNotNull(GameObject.Find("NorthWing_Lightwell_WallN"));
            Assert.IsNotNull(GameObject.Find("NorthWing_Lightwell_WallS"));
            Assert.IsNotNull(GameObject.Find("NorthWing_Lightwell_WallE"));
            Assert.IsNotNull(GameObject.Find("NorthWing_Lightwell_WallW"));
            Assert.IsNotNull(GameObject.Find("NorthWing_Lightwell_SkylightCap"));

            // 17. Entry-only door, Threshold of Trust authorization, progression/save schema & BorderCorridorScene unchanged
            BackroomsExitDoorTrigger exitTrig = root.transform.Find("Backrooms_ExitDoor_ToStreet")?.GetComponent<BackroomsExitDoorTrigger>();
            Assert.IsNotNull(exitTrig);
            Assert.IsTrue(exitTrig.isEntryOnly);
            Assert.IsTrue(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(RemanenciaThresholdOfTrustTrigger.ExitSourceId));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource));
            Assert.AreEqual(1, new ProgressionSaveData().schemaVersion);
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"));

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // 2. Strict builder idempotency & non-target scene rejection
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(emptyScene));
        }
    }
}
