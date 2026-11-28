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
    /// Dedicated automated EditMode verification suite for Implementation 07.21:
    /// East Overhang — Approved Alternative C (Discontinuous East Fragment).
    /// Covers all 41 verification requirements: scene construction, preserved terminal frame & conduit,
    /// oblique threshold neck centerline shift (-0.300m), displaced fragment geometry & ceiling rise (+0.300m),
    /// solid South wall (zero South window), East laminated safety glass & truncated beam remnants,
    /// visible cantilever support frame (zero columns into void, zero Room 111-B overlap), end-to-end
    /// CharacterController.Move() traversal & containment, and all protected Remanencia & surface-world invariants.
    /// </summary>
    [TestFixture]
    public class RemanenciaEastOverhangTests
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
        public void ConstructionGeometrySupportAndSolidSouthWall_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. EastOverhang root exists exactly once, zero runtime scripts, zero added Light components
            int rootCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                if (shell.GetChild(i).name == EastOverhangBuilder.RootName) rootCount++;
            }
            Assert.AreEqual(1, rootCount, "Hotel_EastOverhang must exist exactly once.");
            Assert.IsTrue(EastOverhangBuilder.VerifyStructurePresent(shell));

            Transform overhangRoot = shell.Find(EastOverhangBuilder.RootName);
            Assert.AreEqual(0, overhangRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "East Overhang must contain zero runtime MonoBehaviours.");
            Assert.AreEqual(0, overhangRoot.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "East Overhang must add zero Unity Light components.");

            // 4, 5, 6, 7. Preserved East continuation frame (2.04m x 2.24m), conduit & junction box, split terminal wall piers
            Transform eastTerm = shell.Find($"{NorthUpperGalleryBuilder.RootName}/NUGallery_EastTerminal");
            Assert.IsNotNull(eastTerm);
            Transform fS = eastTerm.Find("NUGallery_Terminal_SealedFrameS");
            Transform fN = eastTerm.Find("NUGallery_Terminal_SealedFrameN");
            Transform fTop = eastTerm.Find("NUGallery_Terminal_SealedFrameTop");
            Assert.IsNotNull(fS);
            Assert.IsNotNull(fN);
            Assert.IsNotNull(fTop);
            Assert.IsNotNull(eastTerm.Find("NUGallery_Terminal_ConduitDrop"));
            Assert.IsNotNull(eastTerm.Find("NUGallery_Terminal_JunctionBox"));

            float clearW = (fN.position.z - fN.localScale.z * 0.5f) - (fS.position.z + fS.localScale.z * 0.5f);
            float clearH = (fTop.position.y - fTop.localScale.y * 0.5f) - EastOverhangBuilder.FloorY;
            Assert.AreEqual(2.040f, clearW, 0.01f, "East portal clear width must remain 2.040m.");
            Assert.AreEqual(2.240f, clearH, 0.01f, "East portal clear height must remain 2.240m.");

            Collider southPier = eastTerm.Find("NUGallery_TerminalWall_East").GetComponent<Collider>();
            Collider northPier = overhangRoot.Find("EastOverhang_ThresholdNeck/EastOverhang_Terminal_NorthPier").GetComponent<Collider>();
            Assert.AreEqual(30.280f, southPier.bounds.max.z, 0.005f);
            Assert.AreEqual(32.320f, northPier.bounds.min.z, 0.005f);

            // 8, 9, 10, 12, 13, 14. Threshold neck & displaced fragment dimensions, exact -0.300m centerline shift,
            //                       level Y=-46.400m floor, raised Y=-43.250m ceiling, total walkable ~12.69m²
            Collider neckFloor = overhangRoot.Find("EastOverhang_ThresholdNeck/EastOverhang_Neck_FloorSlab").GetComponent<Collider>();
            Collider fragFloor = overhangRoot.Find("EastOverhang_DisplacedFragment/EastOverhang_Fragment_FloorSlab").GetComponent<Collider>();
            Collider fragCeil = overhangRoot.Find("EastOverhang_DisplacedFragment/EastOverhang_Fragment_CeilingSlab").GetComponent<Collider>();
            Collider fragWallS = overhangRoot.Find("EastOverhang_DisplacedFragment/EastOverhang_Fragment_WallSouth").GetComponent<Collider>();
            Collider fragWallN = overhangRoot.Find("EastOverhang_DisplacedFragment/EastOverhang_Fragment_WallNorth").GetComponent<Collider>();
            Collider eastParapet = overhangRoot.Find("EastOverhang_EastFacade/EastOverhang_EastParapet_Concrete").GetComponent<Collider>();

            Assert.AreEqual(16.130f, neckFloor.bounds.min.x, 0.005f);
            Assert.AreEqual(17.330f, neckFloor.bounds.max.x, 0.005f);
            Assert.AreEqual(-46.400f, neckFloor.bounds.max.y, 0.003f);
            Assert.AreEqual(-46.400f, fragFloor.bounds.max.y, 0.003f);
            Assert.AreEqual(-43.250f, fragCeil.bounds.min.y, 0.003f, "Displaced fragment ceiling soffit must equal Y=-43.250m.");

            float fragClearX = eastParapet.bounds.min.x - EastOverhangBuilder.FragmentMinX;
            float fragClearZ = fragWallN.bounds.min.z - fragWallS.bounds.max.z;
            float fragCenterZ = (fragWallS.bounds.max.z + fragWallN.bounds.min.z) * 0.5f;
            Assert.AreEqual(3.200f, fragClearX, 0.01f, "Fragment clear length X must equal 3.200m.");
            Assert.AreEqual(3.200f, fragClearZ, 0.01f, "Fragment clear width Z must equal 3.200m.");
            Assert.AreEqual(31.000f, fragCenterZ, 0.005f, "Fragment centerline Z must shift exactly -0.300m South to Z=+31.000.");
            Assert.AreEqual(-0.300f, fragCenterZ - EastOverhangBuilder.PortalCenterZ, 0.005f);

            float walkableArea = (EastOverhangBuilder.NeckLengthX * EastOverhangBuilder.PortalClearWidthZ) + (fragClearX * fragClearZ);
            Assert.AreEqual(12.688f, walkableArea, 0.05f, "Total new walkable area must match ~12.69 m².");

            // 15. South wall is 100% SOLID (zero South window, zero viewing slot, zero aperture toward Room 111-B!)
            foreach (float sx in new[] { 17.60f, 18.40f, 19.20f, 20.00f, 20.30f })
            {
                foreach (float sy in new[] { -45.90f, -45.10f, -44.40f, -43.60f })
                {
                    Assert.IsTrue(Physics.Linecast(new Vector3(sx, sy, 29.75f), new Vector3(sx, sy, 28.90f), ~0, QueryTriggerInteraction.Ignore),
                        $"Solid South wall gap/window detected at X={sx:F2}, Y={sy:F2}!");
                }
            }

            // 16. East-facing observation facade has 1.050m parapet & 3 visibly transparent colliding safety-glass panels
            Assert.AreEqual(-45.350f, eastParapet.bounds.max.y, 0.003f, "East parapet top must equal Y=-45.350m (1.050m guard).");
            Transform facade = overhangRoot.Find("EastOverhang_EastFacade");
            for (int i = 0; i < 3; i++)
            {
                Transform panel = facade.Find($"EastOverhang_SafetyGlass_Panel_{i}");
                Assert.IsNotNull(panel);
                MeshRenderer mr = panel.GetComponent<MeshRenderer>();
                Collider col = panel.GetComponent<Collider>();
                Assert.IsTrue(mr.enabled);
                Assert.AreEqual("Transparent", mr.sharedMaterial.GetTag("RenderType", false));
                Assert.IsTrue(col.enabled && !col.isTrigger);
            }

            // 17, 18, 19, 20, 21, 22, 23. Support frame, zero columns descending into void, zero Room 111-B overlap,
            //                             flush architectural separation reveal, inaccessible exterior beam stubs
            Transform structure = overhangRoot.Find("EastOverhang_Structure");
            Assert.IsNotNull(structure.Find("EastOverhang_Perimeter_LowerAnchorCollar"));
            Assert.IsNotNull(structure.Find("EastOverhang_Cantilever_LowerGirderS"));
            Assert.IsNotNull(structure.Find("EastOverhang_Cantilever_LowerGirderN"));
            Assert.IsNotNull(structure.Find("EastOverhang_BackSpan_TieBeamS"));
            Assert.IsNotNull(structure.Find("EastOverhang_BackSpan_TieBeamN"));

            Collider eastCorrCeil = GameObject.Find("Room111B_EastCorr_CeilingSlab")?.GetComponent<Collider>();
            Collider room111BCeil = GameObject.Find("Room111B_Shell_CeilingSlab")?.GetComponent<Collider>();
            Assert.IsNotNull(eastCorrCeil);
            Assert.IsNotNull(room111BCeil);

            foreach (Renderer r in overhangRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Bounds b = r.bounds;
                Assert.GreaterOrEqual(b.min.y, -47.10f,
                    $"Overhang object '{r.name}' descends into lower exterior void (minY={b.min.y:F3})!");
                Assert.IsFalse(b.Intersects(eastCorrCeil.bounds),
                    $"Overhang object '{r.name}' intersects Room111B_EastCorr_CeilingSlab!");
                Assert.IsFalse(b.Intersects(room111BCeil.bounds),
                    $"Overhang object '{r.name}' intersects Room111B_Shell_CeilingSlab!");
                Assert.GreaterOrEqual(b.min.z, 29.10f,
                    $"Overhang object '{r.name}' shifts too far South toward Room 111-B East Corridor (minZ={b.min.z:F3})!");
            }

            // Separation reveal has continuous floor underneath (zero walkable gap) and no vertical step
            Assert.IsTrue(Physics.Raycast(new Vector3(17.30f, -45.50f, 31.00f), Vector3.down, out RaycastHit revHit, 1.50f, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual(-46.400f, revHit.point.y, 0.005f, "Floor beneath Architectural Separation Reveal must be flush at Y=-46.400m.");

            // Truncated exterior beam stubs sit strictly East of East glass (X >= 20.73) and are inaccessible
            Transform stubS = structure.Find("EastOverhang_ExteriorStub_ParapetS");
            Transform stubN = structure.Find("EastOverhang_ExteriorStub_ParapetN");
            Assert.IsNotNull(stubS);
            Assert.IsNotNull(stubN);
            Assert.GreaterOrEqual(stubS.GetComponent<Renderer>().bounds.min.x, 20.72f);
            Assert.GreaterOrEqual(stubN.GetComponent<Renderer>().bounds.min.x, 20.72f);
        }

        [Test]
        public void EndToEndCharacterControllerTraversal_AndContainment_Succeed()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
            float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;

            GameObject ccGo = new GameObject("__Test0721PlayerCC__");
            try
            {
                CharacterController cc = ccGo.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.28f;
                cc.skinWidth = 0.04f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.30f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                ccGo.transform.position = new Vector3(14.50f, -46.36f, 31.30f);
                Physics.SyncTransforms();

                void WalkTo(Vector2 targetXZ, int maxSteps)
                {
                    const float dt = 0.04f;
                    const float speed = 3.2f;
                    for (int k = 0; k < maxSteps; k++)
                    {
                        Vector2 curXZ = new Vector2(ccGo.transform.position.x, ccGo.transform.position.z);
                        Vector2 diff = targetXZ - curXZ;
                        if (diff.magnitude < 0.10f) break;
                        Vector2 dir = diff.normalized;
                        cc.Move(new Vector3(dir.x * speed, -4.0f, dir.y * speed) * dt);
                    }
                }

                // 24, 25, 26, 27. Cross East portal -> oblique threshold neck -> separation reveal -> center of displaced fragment
                WalkTo(new Vector2(16.73f, 31.15f), 60);
                Assert.AreEqual(-46.400f, ccGo.transform.position.y - cc.skinWidth, 0.05f, "Floor elevation drifted in threshold neck.");

                WalkTo(new Vector2(19.60f, 31.00f), 80);
                Assert.Greater(ccGo.transform.position.x, 19.30f,
                    $"Player CC failed to enter displaced fragment (stopped at X={ccGo.transform.position.x:F2}).");
                Assert.AreEqual(-46.400f, ccGo.transform.position.y - cc.skinWidth, 0.05f);

                // 28 & 29. Push hard East against glass & parapet — strictly contained inside X < 20.30!
                for (int k = 0; k < 50; k++) cc.Move(new Vector3(4.0f, -4.0f, 0f) * 0.04f);
                Assert.LessOrEqual(ccGo.transform.position.x, 20.25f,
                    $"Player CC penetrated East glass facade (X={ccGo.transform.position.x:F3})!");
                Assert.AreEqual(-46.400f, ccGo.transform.position.y - cc.skinWidth, 0.05f);

                // 30 & 31. Explore South & North corners of fragment and push walls — strictly contained!
                WalkTo(new Vector2(19.80f, 29.80f), 60);
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(1.5f, -4.0f, -4.0f) * 0.04f);
                Assert.GreaterOrEqual(ccGo.transform.position.z, 29.68f,
                    $"Player CC penetrated solid South wall (Z={ccGo.transform.position.z:F3})!");
                Assert.LessOrEqual(ccGo.transform.position.x, 20.25f);

                WalkTo(new Vector2(19.80f, 32.20f), 80);
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(1.5f, -4.0f, 4.0f) * 0.04f);
                Assert.LessOrEqual(ccGo.transform.position.z, 32.32f,
                    $"Player CC penetrated solid North wall (Z={ccGo.transform.position.z:F3})!");
                Assert.LessOrEqual(ccGo.transform.position.x, 20.25f);

                // 32. Complete return route: Fragment -> Neck -> North Upper Gallery -> NW Staircase -> Nivel 0!
                WalkTo(new Vector2(17.60f, 31.00f), 60);
                WalkTo(new Vector2(15.00f, 31.30f), 80);
                Assert.Less(ccGo.transform.position.x, 15.20f, "Player CC failed to return through neck into gallery.");

                WalkTo(new Vector2(-10.70f, 31.30f), 280);
                Assert.Less(ccGo.transform.position.x, -10.50f, "Player CC failed to reach NW Staircase Upper Landing.");
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

            // Gap-free East facade collision linecast sweep across full 3.20m width & parapet+glass height
            float[] checkZs = { 29.48f, 29.95f, 30.48f, 31.00f, 31.52f, 32.05f, 32.52f };
            float[] checkYs = { -45.90f, -45.40f, -44.75f, -44.00f, -43.35f };
            foreach (float cz in checkZs)
            {
                foreach (float cy in checkYs)
                {
                    Assert.IsTrue(Physics.Linecast(new Vector3(20.20f, cy, cz), new Vector3(21.00f, cy, cz), ~0, QueryTriggerInteraction.Ignore),
                        $"East facade collision gap at Z={cz:F2}, Y={cy:F2}!");
                }
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
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 33, 34, 35. Staircase, all 5 gallery pilasters/beams, and 07.19 Overlook intact
            Assert.IsTrue(NorthwestServiceStaircaseBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(NorthUpperGalleryBuilder.VerifyStructurePresent(shell));
            Assert.IsTrue(CourtyardLightwellOverlookBuilder.VerifyStructurePresent(shell));
            Transform galDetails = shell.Find($"{NorthUpperGalleryBuilder.RootName}/NUGallery_Details");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsNotNull(galDetails.Find($"NUGallery_PilasterNorth_{i}"));
                Assert.IsNotNull(galDetails.Find($"NUGallery_PilasterSouth_{i}"));
                Assert.IsNotNull(galDetails.Find($"NUGallery_CeilingBeam_{i}"));
            }

            // 36. All 10 Lightwell objects intact
            string[] lwNames =
            {
                "NorthWing_Lightwell_WallN", "NorthWing_Lightwell_WallS",
                "NorthWing_Lightwell_WallE", "NorthWing_Lightwell_WallW",
                "NorthWing_Lightwell_SkylightCap",
                "NorthWing_Lightwell_ConcreteBeam_0", "NorthWing_Lightwell_ConcreteBeam_1", "NorthWing_Lightwell_ConcreteBeam_2",
                "NorthWing_Lightwell_SteelPurlinN", "NorthWing_Lightwell_SteelPurlinS"
            };
            foreach (string lw in lwNames) Assert.IsNotNull(GameObject.Find(lw), $"Missing protected {lw}.");

            // 37, 38, 39, 40, 41. Room 111-B, Threshold of Trust, Elevators (35 children), entry-only, exact 18 lights, BorderCorridorScene
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
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

            // 2. Cross-builder idempotency across all three upper-stratum builders!
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => EastOverhangBuilder.MigrateEastOverhangInOpenScene(emptyScene));
        }

        [Test]
        public void RemanenciaAtmosphere_MatchesCameraVoidBackgroundToFogColor_AndRestoresOnExit()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGo = new GameObject("TestPlayerMainCamera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            Color unityDefaultBlue = new Color(0.19215687f, 0.3019608f, 0.4745098f, 0f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = unityDefaultBlue;

            GameObject ctrlGo = new GameObject("TestTransitionController");
            SecretBackroomsTransitionController ctrl = ctrlGo.AddComponent<SecretBackroomsTransitionController>();

            try
            {
                ctrl.CaptureAndApplyBackroomsAtmosphereForTest();

                Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags,
                    "Inside Remanencia, player camera must clear void pixels with SolidColor instead of Skybox fallback blue.");
                Assert.AreEqual(SecretBackroomsTransitionController.RemanenciaLevel0FogColor, cam.backgroundColor,
                    "Inside Remanencia, player camera background color must match RemanenciaLevel0FogColor.");
                Assert.IsTrue(RenderSettings.fog, "Remanencia fog must be enabled.");
                Assert.AreEqual(SecretBackroomsTransitionController.RemanenciaLevel0FogColor, RenderSettings.fogColor);

                ctrl.RestoreOutdoorAtmosphereForTest();

                Assert.AreEqual(CameraClearFlags.Skybox, cam.clearFlags,
                    "Exiting Remanencia must restore the outdoor camera clearFlags.");
                Assert.AreEqual(unityDefaultBlue, cam.backgroundColor,
                    "Exiting Remanencia must restore the outdoor camera backgroundColor.");
            }
            finally
            {
                ctrl.RestoreOutdoorAtmosphereForTest();
                UnityEngine.Object.DestroyImmediate(ctrlGo);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }
    }
}
