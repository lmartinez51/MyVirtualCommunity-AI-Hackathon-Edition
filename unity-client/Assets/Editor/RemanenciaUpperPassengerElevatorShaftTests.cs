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
    /// Dedicated automated EditMode verification suite for Implementation 07.26:
    /// Bay 1 Passenger Elevator Shaft Isolation (Static Structural Implementation Only —
    /// No Cabin, No Guide Rails, No Moving Doors, No Runtime Controller, No Audio, No New Lights).
    ///
    /// Covers all required 07.26 verification checks:
    ///   - Four-piece Backrooms_Ceiling perimeter reconstruction &amp; exact Bay 1 shaft aperture
    ///   - Continuous reinforced concrete divider separating Bay 1 from Bay 2 with zero gap
    ///   - Upper vertical shaft enclosure, East plenum seal &amp; sealed roof cap
    ///   - Upper Bay 1 backing wall split &amp; preserved closed 07.24 finished facade + solid safety backing
    ///   - Unconditional Bay 2 isolation (overhead cap, solid backing, untouched unfinished facade)
    ///   - Uncut Backrooms_Floor, untouched Waiting FloorSlab/CeilingSlab, untouched Hotel_Elevator_Core (35 children),
    ///     all 5 adjacent lower fixtures, all 10 Lightwell objects, exact 18 lights, BorderCorridorScene SHA-256,
    ///     full CharacterController.Move() waiting-area safety, and full 6-builder cross-idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaUpperPassengerElevatorShaftTests
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
        public void BackroomsCeilingPerimeterSplit_ExactApertureAndBay2OverheadCap_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Assert.IsNotNull(shaftRoot);
            Assert.IsTrue(UpperPassengerElevatorShaftBuilder.VerifyStructurePresent(shell));

            // Strict static structural purity: zero runtime scripts, zero Animators, zero Lights, zero trigger colliders
            Assert.AreEqual(0, shaftRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length);
            Assert.AreEqual(0, shaftRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length);
            Assert.AreEqual(0, shaftRoot.GetComponentsInChildren<Light>(includeInactive: true).Length);
            foreach (Collider c in shaftRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsFalse(c.isTrigger, $"Shaft collider '{c.name}' must not be a trigger.");
            }

            // Original Backrooms_Ceiling preserved in-place directly under Backrooms_Architecture as East slab
            Transform ceilEast = shell.Find("Backrooms_Ceiling");
            Transform ceilSplit = shaftRoot.Find("Bay1Shaft_CeilingSplit");
            Transform ceilWest = ceilSplit.Find("Backrooms_Ceiling_West");
            Transform ceilSouth = ceilSplit.Find("Backrooms_Ceiling_South");
            Transform ceilNorth = ceilSplit.Find("Backrooms_Ceiling_North");
            Assert.IsNotNull(ceilEast);
            Assert.IsNotNull(ceilWest);
            Assert.IsNotNull(ceilSouth);
            Assert.IsNotNull(ceilNorth);

            Collider cEast = ceilEast.GetComponent<BoxCollider>();
            Collider cWest = ceilWest.GetComponent<BoxCollider>();
            Collider cSouth = ceilSouth.GetComponent<BoxCollider>();
            Collider cNorth = ceilNorth.GetComponent<BoxCollider>();
            Assert.IsTrue(cEast.enabled && cWest.enabled && cSouth.enabled && cNorth.enabled);

            // Verify exact Y bounds [-47.150, -46.950] & CeilingSoffit material on all 4 slabs
            Collider[] slabs = { cEast, cWest, cSouth, cNorth };
            for (int i = 0; i < slabs.Length; i++)
            {
                Assert.AreEqual(-47.150f, slabs[i].bounds.min.y, 0.002f, $"{slabs[i].name} min Y mismatch.");
                Assert.AreEqual(-46.950f, slabs[i].bounds.max.y, 0.002f, $"{slabs[i].name} max Y mismatch.");
                Assert.AreEqual(BackroomsLevel1Builder.CeilingSoffitMatPath,
                    AssetDatabase.GetAssetPath(slabs[i].GetComponent<MeshRenderer>().sharedMaterial),
                    $"{slabs[i].name} must use CeilingSoffit material.");
            }

            // Verify exact horizontal perimeter bounds around Bay 1 aperture X in [+1.240, +3.150], Z in [+4.440, +6.680]
            Assert.AreEqual(3.150f, cEast.bounds.min.x, 0.002f);
            Assert.AreEqual(20.300f, cEast.bounds.max.x, 0.002f);
            Assert.AreEqual(-20.300f, cWest.bounds.min.x, 0.002f);
            Assert.AreEqual(1.240f, cWest.bounds.max.x, 0.002f);
            Assert.AreEqual(1.240f, cSouth.bounds.min.x, 0.002f);
            Assert.AreEqual(3.150f, cSouth.bounds.max.x, 0.002f);
            Assert.AreEqual(-12.300f, cSouth.bounds.min.z, 0.002f);
            Assert.AreEqual(4.440f, cSouth.bounds.max.z, 0.002f);
            bool has0729Bay2Open = shaftRoot.Find(UpperPassengerElevatorDoorsBuilder.Bay2OpenShaftGroupName) != null;
            Assert.AreEqual(1.240f, cNorth.bounds.min.x, 0.002f);
            Assert.AreEqual(3.150f, cNorth.bounds.max.x, 0.002f);
            Assert.AreEqual(has0729Bay2Open ? 9.160f : 6.920f, cNorth.bounds.min.z, 0.002f);
            Assert.AreEqual(12.300f, cNorth.bounds.max.z, 0.002f);

            // Vertical raycasts inside Bay 1 shaft aperture (X in [1.32, 3.025], Z in [4.62, 6.56]) must pass freely
            // across the Backrooms_Ceiling plane (Y in [-47.150, -46.950]) all the way up to RoofCap soffit Y = -43.550.
            // When the 07.27 static cabin is parked at lower stop (Y in [-50.000, -47.600]), start above Cabin roof (-47.50).
            float apertureSweepStartY = shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName) != null ? -47.50f : -49.80f;
            float[] sampleXs = { 1.32f, 2.195f, 3.025f };
            float[] sampleZs = { 4.62f, 5.560f, 6.560f };
            for (int ix = 0; ix < sampleXs.Length; ix++)
            {
                for (int iz = 0; iz < sampleZs.Length; iz++)
                {
                    Vector3 start = new Vector3(sampleXs[ix], apertureSweepStartY, sampleZs[iz]);
                    Vector3 end = new Vector3(sampleXs[ix], -43.65f, sampleZs[iz]);
                    Assert.IsFalse(Physics.Linecast(start, end, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore),
                        $"Bay 1 vertical shaft aperture obstructed at ({sampleXs[ix]:F2}, {sampleZs[iz]:F2}) by '{hit.collider?.name}' at Y={hit.point.y:F3}!");
                }
            }

            // Vertical raycasts inside Bay 2 (Z in [7.10, 8.90]): capped pre-07.29; permanently open void in 07.29+!
            float[] bay2Zs = { 7.10f, 7.85f, 8.90f };
            for (int iz = 0; iz < bay2Zs.Length; iz++)
            {
                if (has0729Bay2Open)
                {
                    Assert.IsFalse(
                        Physics.Linecast(new Vector3(2.195f, -49.50f, bay2Zs[iz]), new Vector3(2.195f, -43.65f, bay2Zs[iz]),
                            out RaycastHit obsHit, ~0, QueryTriggerInteraction.Ignore),
                        $"Bay 2 open vertical shaft obstructed at Z={bay2Zs[iz]:F2} by '{obsHit.collider?.name}'!");
                }
                else
                {
                    Assert.IsTrue(
                        Physics.Linecast(new Vector3(2.195f, -49.50f, bay2Zs[iz]), new Vector3(2.195f, -46.00f, bay2Zs[iz]),
                            out RaycastHit capHit, ~0, QueryTriggerInteraction.Ignore),
                        $"Bay 2 overhead ceiling cap missing at Z={bay2Zs[iz]:F2}!");
                    Assert.AreEqual("Backrooms_Ceiling_North", capHit.collider.name);
                    Assert.AreEqual(-47.150f, capHit.point.y, 0.01f);
                }
            }

            // Representative lobby ceiling raycasts outside Bay 1 aperture MUST still hit solid ceiling at Y = -47.150
            Vector2[] lobbySamples =
            {
                new Vector2(5.0f, 5.75f), new Vector2(-5.0f, 5.75f),
                new Vector2(2.195f, 2.0f), new Vector2(2.195f, 10.5f),
                new Vector2(15.0f, -8.0f), new Vector2(-15.0f, 8.0f)
            };
            for (int i = 0; i < lobbySamples.Length; i++)
            {
                Vector3 p0 = new Vector3(lobbySamples[i].x, -49.00f, lobbySamples[i].y);
                Vector3 p1 = new Vector3(lobbySamples[i].x, -46.50f, lobbySamples[i].y);
                Assert.IsTrue(Physics.Linecast(p0, p1, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore),
                    $"Lobby ceiling gap detected at ({lobbySamples[i].x:F2}, {lobbySamples[i].y:F2})!");
                Assert.AreEqual(-47.150f, hit.point.y, 0.01f);
            }
        }

        [Test]
        public void ConcreteDividerUpperEnclosureRoofCapAndUpperPortalBacking_AreSealedAndExact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);

            // 1. Continuous reinforced concrete divider: exact X [1.24, 3.15], Z [6.68, 6.92], Y [-50.00, -43.55]
            Collider divider = shaftRoot.Find("Bay1Shaft_ConcreteDivider/Bay1Shaft_DividerWall_North").GetComponent<BoxCollider>();
            Assert.IsTrue(divider.enabled);
            Assert.AreEqual(1.240f, divider.bounds.min.x, 0.002f);
            Assert.AreEqual(3.150f, divider.bounds.max.x, 0.002f);
            Assert.AreEqual(6.680f, divider.bounds.min.z, 0.002f);
            Assert.AreEqual(6.920f, divider.bounds.max.z, 0.002f);
            Assert.AreEqual(-50.000f, divider.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.550f, divider.bounds.max.y, 0.002f);

            // Multi-height N-S raycasts across the divider proving ZERO gaps between Bay 1 and Bay 2
            // (uses RaycastAll so parked cabin / guide-rail colliders in Bay 1 do not mask divider verification)
            float[] testYs = { -49.90f, -48.50f, -47.05f, -46.77f, -46.50f, -45.30f, -43.65f, -43.45f };
            float[] testXs = { 1.30f, 2.195f, 3.10f };
            for (int iy = 0; iy < testYs.Length; iy++)
            {
                for (int ix = 0; ix < testXs.Length; ix++)
                {
                    RaycastHit[] hits = Physics.RaycastAll(
                        new Vector3(testXs[ix], testYs[iy], 6.40f),
                        Vector3.forward,
                        0.80f,
                        ~0,
                        QueryTriggerInteraction.Ignore);
                    bool hitDivider = false;
                    for (int h = 0; h < hits.Length; h++)
                    {
                        if (hits[h].collider.name == "Bay1Shaft_DividerWall_North" ||
                            hits[h].collider.name == "Bay1Shaft_DividerTopCoping")
                        {
                            Assert.AreEqual(6.680f, hits[h].point.z, 0.01f);
                            hitDivider = true;
                            break;
                        }
                    }
                    Assert.IsTrue(hitDivider, $"Divider gap between Bay 1 and Bay 2 at X={testXs[ix]:F2}, Y={testYs[iy]:F2}!");
                }
            }

            // 2. Upper vertical enclosure walls & sealed roof cap: exact Y [-43.550, -43.350]
            Collider roofCap = shaftRoot.Find("Bay1Shaft_UpperEnclosure/UpperShaft_RoofCap").GetComponent<BoxCollider>();
            Collider backWall = shaftRoot.Find("Bay1Shaft_UpperEnclosure/UpperShaft_BackWall").GetComponent<BoxCollider>();
            Collider southWall = shaftRoot.Find("Bay1Shaft_UpperEnclosure/UpperShaft_SouthWall").GetComponent<BoxCollider>();
            Collider plenumSeal = shaftRoot.Find("Bay1Shaft_UpperEnclosure/UpperShaft_EastPlenumSeal").GetComponent<BoxCollider>();
            Assert.IsTrue(roofCap.enabled && backWall.enabled && southWall.enabled && plenumSeal.enabled);

            Assert.AreEqual(1.240f, roofCap.bounds.min.x, 0.002f);
            Assert.AreEqual(3.150f, roofCap.bounds.max.x, 0.002f);
            Assert.AreEqual(4.440f, roofCap.bounds.min.z, 0.002f);
            Assert.AreEqual(6.680f, roofCap.bounds.max.z, 0.002f);
            Assert.AreEqual(-43.550f, roofCap.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.350f, roofCap.bounds.max.y, 0.002f);

            // Verify zero gaps on West, South, and East shaft faces across all critical horizontal joints
            // (-47.150 ceiling soffit, -47.050 ceiling mid, -46.945 plenum seal, -46.770 plinth, -46.500 floor slab, -43.800 transom)
            float[] jointYs = { -47.50f, -47.05f, -46.945f, -46.77f, -46.50f, -45.30f, -43.80f };
            for (int i = 0; i < jointYs.Length; i++)
            {
                float y = jointYs[i];
                // West wall solid
                Assert.IsTrue(Physics.Linecast(new Vector3(1.80f, y, 5.56f), new Vector3(0.80f, y, 5.56f), out RaycastHit wHit, ~0, QueryTriggerInteraction.Ignore),
                    $"West shaft wall gap at Y={y:F3}!");
                Assert.AreEqual(1.240f, wHit.point.x, 0.01f);

                // South wall solid (sampled at X=+1.800 clear of the centered T-rail flange X in [2.135, 2.205])
                Assert.IsTrue(Physics.Linecast(new Vector3(1.800f, y, 5.00f), new Vector3(1.800f, y, 4.00f), out RaycastHit sHit, ~0, QueryTriggerInteraction.Ignore),
                    $"South shaft wall gap at Y={y:F3}!");
                Assert.AreEqual(4.440f, sHit.point.z, 0.01f);

                // East wall solid everywhere except inside the future Bay 1 portal (sealed at X=+3.346 pre-07.29 or
                // by locked UpperLanding telescopic doors at X=+3.060 in 07.29+)
                bool has0729Doors = shell.Find(UpperPassengerElevatorDoorsBuilder.DoorsRootName) != null;
                Assert.IsTrue(Physics.Linecast(new Vector3(2.80f, y, 5.75f), new Vector3(3.60f, y, 5.75f), out RaycastHit eHit, ~0, QueryTriggerInteraction.Ignore),
                    $"East shaft enclosure gap at Y={y:F3}!");
                if (y > -46.39f && y < -44.21f)
                {
                    float minPortalSealX = has0729Doors ? 3.055f : 3.340f;
                    Assert.GreaterOrEqual(eHit.point.x, minPortalSealX,
                        $"Expected Bay 1 portal sealed at X>={minPortalSealX:F3} at Y={y:F3}, hit '{eHit.collider.name}' at X={eHit.point.x:F3}.");
                }
                else if (y > -47.79f)
                {
                    Assert.AreEqual(3.150f, eHit.point.x, 0.015f, $"East shaft transition gap at Y={y:F3} (hit '{eHit.collider.name}' at X={eHit.point.x:F3}).");
                }
            }

            // Upward raycasts from inside shaft MUST terminate cleanly at RoofCap soffit Y = -43.550
            Assert.IsTrue(Physics.Linecast(new Vector3(2.195f, -45.00f, 5.56f), new Vector3(2.195f, -42.50f, 5.56f),
                out RaycastHit capHit, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual("UpperShaft_RoofCap", capHit.collider.name);
            Assert.AreEqual(-43.550f, capHit.point.y, 0.005f);

            // 3. Upper backing wall split around Bay 1 portal (and Bay 2 portal in 07.29+)
            bool has0729 = shell.Find(UpperPassengerElevatorDoorsBuilder.DoorsRootName) != null;
            Collider backingNorth = approachRoot.Find("UpperElevatorApproach_WaitingArea/UpperElev_Waiting_WestCoreBackingWall").GetComponent<BoxCollider>();
            Collider backingSouth = shaftRoot.Find("Bay1Shaft_UpperPortalBacking/Bay1UpperBacking_SouthPier").GetComponent<BoxCollider>();
            Collider backingTransom = shaftRoot.Find("Bay1Shaft_UpperPortalBacking/Bay1UpperBacking_Transom").GetComponent<BoxCollider>();
            Assert.AreEqual(6.435f, backingNorth.bounds.min.z, 0.002f, "North/Mid backing wall must start cleanly at Z=+6.435.");
            Assert.AreEqual(has0729 ? 7.165f : 9.400f, backingNorth.bounds.max.z, 0.002f);
            Assert.AreEqual(4.200f, backingSouth.bounds.min.z, 0.002f);
            Assert.AreEqual(5.065f, backingSouth.bounds.max.z, 0.002f);
            Assert.AreEqual(5.065f, backingTransom.bounds.min.z, 0.002f);
            Assert.AreEqual(6.435f, backingTransom.bounds.max.z, 0.002f);
            Assert.AreEqual(-44.200f, backingTransom.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.550f, backingTransom.bounds.max.y, 0.002f);

            // 4. Verify Bay 1 remains closed & impassable from Waiting Area; Bay 2 is permanently open in 07.29+
            Transform bay1SealedPanel = approachRoot.Find("UpperElevatorApproach_SealedBayFrames/UpperElev_SealedBay_1_SealedBackingPanel");
            Assert.IsNotNull(bay1SealedPanel);
            Assert.IsTrue(Physics.Linecast(new Vector3(4.50f, -45.30f, 5.75f), new Vector3(2.20f, -45.30f, 5.75f),
                out RaycastHit facadeHit, ~0, QueryTriggerInteraction.Ignore));
            Assert.GreaterOrEqual(facadeHit.point.x, has0729 ? 3.055f : 3.345f, "Locked Bay 1 facade/doors must still block Waiting Area entry!");

            if (!has0729)
            {
                Assert.IsTrue(bay1SealedPanel.GetComponent<BoxCollider>().enabled);
                Assert.IsTrue(Physics.Linecast(new Vector3(2.20f, -45.30f, 7.85f), new Vector3(4.50f, -45.30f, 7.85f),
                    out RaycastHit bay2BackHit, ~0, QueryTriggerInteraction.Ignore));
                Assert.AreEqual("UpperElev_Waiting_WestCoreBackingWall", bay2BackHit.collider.name);
                Assert.AreEqual(3.150f, bay2BackHit.point.x, 0.01f);
            }
            else
            {
                Assert.IsFalse(Physics.Linecast(new Vector3(4.50f, -45.30f, 7.85f), new Vector3(2.20f, -45.30f, 7.85f),
                    out RaycastHit b2Hit, ~0, QueryTriggerInteraction.Ignore),
                    $"Upper Bay 2 doorway must be permanently open in 07.29 (blocked by '{b2Hit.collider?.name}')!");
            }
        }

        [Test]
        public void ProtectedStructuresExact18LightsAndSixBuilderCrossIdempotency_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform approachRoot = shell.Find(UpperElevatorApproachBuilder.RootName);

            // Uncut Backrooms_Floor (monolithic 40.6m x 24.6m, top Y = -50.000)
            Collider floorCol = shell.Find("Backrooms_Floor").GetComponent<BoxCollider>();
            Assert.AreEqual(-50.000f, floorCol.bounds.max.y, 0.002f);
            Assert.AreEqual(40.600f, floorCol.bounds.size.x, 0.01f);
            Assert.AreEqual(24.600f, floorCol.bounds.size.z, 0.01f);

            // Untouched UpperElev_Waiting_FloorSlab & CeilingSlab (X in [+3.150, +6.420])
            Collider waitFloor = approachRoot.Find("UpperElevatorApproach_WaitingArea/UpperElev_Waiting_FloorSlab").GetComponent<BoxCollider>();
            Collider waitCeil = approachRoot.Find("UpperElevatorApproach_WaitingArea/UpperElev_Waiting_CeilingSlab").GetComponent<BoxCollider>();
            Assert.AreEqual(3.150f, waitFloor.bounds.min.x, 0.002f);
            Assert.AreEqual(6.420f, waitFloor.bounds.max.x, 0.002f);
            Assert.AreEqual(-46.400f, waitFloor.bounds.max.y, 0.002f);
            Assert.AreEqual(3.150f, waitCeil.bounds.min.x, 0.002f);
            Assert.AreEqual(6.420f, waitCeil.bounds.max.x, 0.002f);
            Assert.AreEqual(-43.550f, waitCeil.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.350f, waitCeil.bounds.max.y, 0.002f);

            // Untouched Hotel_Elevator_Core (exact 35 children) & 5 adjacent fixtures
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorsHeader"));
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorNotice"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorJunctionBox"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorConduitRun"));
            Assert.IsNotNull(GameObject.Find("Hotel_Lobby_Beam_West"));

            // All 10 Lightwell objects, Room 111-B, Threshold of Trust, schemaVersion == 1, BorderCorridorScene SHA-256
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

            // Exact 18 Unity Light components, zero shadow casting
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene Unity Light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // Full 6-builder upper-stratum cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23 -> 07.24 -> 07.26)
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);
            UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(scene);
            UpperElevatorBayArchitectureBuilder.MigrateUpperElevatorBayArchitectureInOpenScene(scene);
            UpperPassengerElevatorShaftBuilder.MigrateUpperPassengerElevatorShaftInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            // Re-verify backing wall stayed split even after re-running 07.23 -> 07.24 -> 07.26!
            Collider backingAfter = approachRoot.Find("UpperElevatorApproach_WaitingArea/UpperElev_Waiting_WestCoreBackingWall").GetComponent<BoxCollider>();
            Assert.AreEqual(6.435f, backingAfter.bounds.min.z, 0.002f, "07.23 re-run must preserve 07.26 Bay 1 backing wall split.");

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => UpperPassengerElevatorShaftBuilder.MigrateUpperPassengerElevatorShaftInOpenScene(emptyScene));
        }
    }
}
