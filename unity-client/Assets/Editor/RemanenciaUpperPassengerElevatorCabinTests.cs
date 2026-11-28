using System;
using System.Collections.Generic;
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
    /// Dedicated automated EditMode verification suite for Implementation 07.27:
    /// Static Passenger Cabin &amp; Guide Rail System (Hybrid Older Hotel Elevator — Static Only).
    ///
    /// Covers all Section 10 verification requirements:
    ///   - Cabin outer bounds, clear interior volume (zero intrusion), East portal, non-static flags, materials
    ///   - Complete 3D ISO 7465 T70-1/B guide rail, bracket, anchor cover, and 4-shoe geometry &amp; clearances
    ///   - Full future vertical travel swept envelope (DeltaY = +3.560m) vs. shaft walls, divider, roof cap,
    ///     4-piece ceiling aperture, upper landing structure, and all four 07.25A reserved door swept volumes
    ///   - Protected invariants, exact 18 Unity lights, zero scripts/animators/audio/triggers, and
    ///     full 7-builder cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23 -> 07.24 -> 07.26 -> 07.27).
    /// </summary>
    [TestFixture]
    public class RemanenciaUpperPassengerElevatorCabinTests
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

        private static bool StrictlyOverlaps(Bounds a, Bounds b, float tol = 0.0005f)
        {
            return (a.min.x < b.max.x - tol && a.max.x > b.min.x + tol) &&
                   (a.min.y < b.max.y - tol && a.max.y > b.min.y + tol) &&
                   (a.min.z < b.max.z - tol && a.max.z > b.min.z + tol);
        }

        private static Bounds ShiftY(Bounds b, float dy)
        {
            return new Bounds(new Vector3(b.center.x, b.center.y + dy, b.center.z), b.size);
        }

        private static Bounds SweepY(Bounds b, float maxDy)
        {
            Vector3 min = b.min;
            Vector3 max = new Vector3(b.max.x, b.max.y + maxDy, b.max.z);
            Bounds swept = new Bounds();
            swept.SetMinMax(min, max);
            return swept;
        }

        [Test]
        public void CabinBoundsClearInteriorEastPortalNonStaticAndMaterials_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform cabinRoot = shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName);
            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Assert.IsNotNull(cabinRoot);
            Assert.NotNull(shaftRoot);
            Assert.IsTrue(UpperPassengerElevatorCabinBuilder.VerifyStructurePresent(shell));

            // Mandatory separation: cabin root is direct sibling of shaft root under Backrooms_Architecture
            Assert.AreEqual(shell, cabinRoot.parent);
            Assert.AreNotEqual(shaftRoot, cabinRoot.parent);

            // Strictly non-static flags across entire movable cabin hierarchy; zero unauthorized scripts/animators/audio/lights/triggers
            bool has0729Doors = shell.Find(UpperPassengerElevatorDoorsBuilder.DoorsRootName) != null;
            foreach (Transform t in cabinRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                Assert.AreEqual((StaticEditorFlags)0, GameObjectUtility.GetStaticEditorFlags(t.gameObject),
                    $"Movable cabin object '{t.name}' must have ZERO static flags!");
            }
            MonoBehaviour[] cabinScripts = cabinRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            if (!has0729Doors)
            {
                Assert.AreEqual(0, cabinScripts.Length);
            }
            else
            {
                foreach (MonoBehaviour mb in cabinScripts)
                {
                    Assert.IsTrue(mb is RemanenciaElevatorDoorAssembly || mb is RemanenciaElevatorDoorInteractable,
                        $"Unexpected script '{mb.GetType().Name}' on cabin object '{mb.name}'.");
                }
            }
            Assert.AreEqual(0, cabinRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length);
            Assert.AreEqual(0, cabinRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length);
            Assert.AreEqual(0, cabinRoot.GetComponentsInChildren<Light>(includeInactive: true).Length);
            foreach (Collider c in cabinRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsFalse(c.isTrigger, $"Cabin collider '{c.name}' must not be a trigger.");
            }

            // Outer structural shell bounds (excluding guide shoes): exact X [1.320, 3.025], Y [-50.000, -47.600], Z [4.620, 6.560]
            Transform shellGrp = cabinRoot.Find("Cabin_StructuralShell");
            Collider[] shellCols = shellGrp.GetComponentsInChildren<Collider>(includeInactive: true);
            Bounds shellUnion = shellCols[0].bounds;
            for (int i = 1; i < shellCols.Length; i++) shellUnion.Encapsulate(shellCols[i].bounds);

            Assert.AreEqual(1.320f, shellUnion.min.x, 0.002f, "Cabin outer min X mismatch.");
            Assert.AreEqual(3.025f, shellUnion.max.x, 0.002f, "Cabin outer max X mismatch.");
            Assert.AreEqual(-50.000f, shellUnion.min.y, 0.002f, "Cabin deck bottom must rest flush on Backrooms_Floor Y=-50.000.");
            Assert.AreEqual(-47.600f, shellUnion.max.y, 0.002f, "Cabin roof top must equal Y=-47.600 (2.400m height).");
            Assert.AreEqual(4.620f, shellUnion.min.z, 0.002f, "Cabin outer min Z mismatch.");
            Assert.AreEqual(6.560f, shellUnion.max.z, 0.002f, "Cabin outer max Z mismatch.");

            // Deck top (-49.960) & Roof soffit (-47.720) -> 2.240m clear interior height
            Collider deckMain = shellGrp.Find("Cabin_FloorDeck_Main").GetComponent<BoxCollider>();
            Collider deckSill = shellGrp.Find("Cabin_FloorSill_East").GetComponent<BoxCollider>();
            Collider roofSlab = shellGrp.Find("Cabin_RoofSlab").GetComponent<BoxCollider>();
            Assert.AreEqual(-50.000f, deckMain.bounds.min.y, 0.001f);
            Assert.AreEqual(-49.960f, deckMain.bounds.max.y, 0.001f);
            Assert.AreEqual(-50.000f, deckSill.bounds.min.y, 0.001f);
            Assert.AreEqual(-49.960f, deckSill.bounds.max.y, 0.001f);
            Assert.AreEqual(-47.600f, roofSlab.bounds.max.y, 0.001f);

            // Deck flush on uncut Backrooms_Floor without penetration
            Collider backroomsFloor = shell.Find("Backrooms_Floor").GetComponent<BoxCollider>();
            Assert.IsFalse(StrictlyOverlaps(deckMain.bounds, backroomsFloor.bounds));
            Assert.AreEqual(backroomsFloor.bounds.max.y, deckMain.bounds.min.y, 0.001f);

            // ZERO collider or renderer intrusion into Clear Passenger Interior X [1.380, 2.940], Y [-49.960, -47.720], Z [4.680, 6.500]
            Bounds clearInterior = new Bounds();
            clearInterior.SetMinMax(new Vector3(1.380f, -49.960f, 4.680f), new Vector3(2.940f, -47.720f, 6.500f));
            foreach (Collider c in cabinRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsFalse(StrictlyOverlaps(c.bounds, clearInterior),
                    $"Cabin collider '{c.name}' ({c.bounds}) intrudes into clear passenger interior!");
            }
            foreach (MeshRenderer r in cabinRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                Assert.IsFalse(StrictlyOverlaps(r.bounds, clearInterior),
                    $"Cabin mesh '{r.name}' ({r.bounds}) intrudes into clear passenger interior!");
            }
            Assert.AreEqual(2.8392f, clearInterior.size.x * clearInterior.size.z, 0.001f);
            Assert.AreEqual(2.240f, clearInterior.size.y, 0.001f);

            // East-facing Entrance Portal: unobstructed pre-07.29, or closed solely by Cabin_Doors leaves in 07.29+
            float[] portalZs = { 5.34f, 5.60f, 5.85f, 6.10f, 6.36f };
            float[] portalYs = { -49.90f, -49.20f, -48.50f, -47.80f };
            for (int iz = 0; iz < portalZs.Length; iz++)
            {
                for (int iy = 0; iy < portalYs.Length; iy++)
                {
                    bool hitPortal = Physics.Linecast(new Vector3(2.80f, portalYs[iy], portalZs[iz]),
                        new Vector3(3.04f, portalYs[iy], portalZs[iz]), out RaycastHit pHit, ~0, QueryTriggerInteraction.Ignore);
                    if (!has0729Doors)
                    {
                        Assert.IsFalse(hitPortal,
                            $"Cabin East entrance portal obstructed at Z={portalZs[iz]:F2}, Y={portalYs[iy]:F2} by '{pHit.collider?.name}'!");
                    }
                    else
                    {
                        Assert.IsTrue(hitPortal && (pHit.collider.name == "CabinDoor_SlowLeaf" || pHit.collider.name == "CabinDoor_FastLeaf"),
                            $"Expected closed Cabin_Doors leaf across portal at Z={portalZs[iz]:F2}, Y={portalYs[iy]:F2}, got '{pHit.collider?.name}'.");
                    }
                }
            }

            // Verify emissive ceiling diffuser uses UtilityWarmOn (zero Light) and materials are existing project assets
            MeshRenderer diffMr = cabinRoot.Find("Cabin_InteriorFinishes/Cabin_CeilingDiffuser").GetComponent<MeshRenderer>();
            Assert.AreEqual(BackroomsLevel1Builder.UtilityWarmOnMatPath, AssetDatabase.GetAssetPath(diffMr.sharedMaterial));
            foreach (MeshRenderer mr in cabinRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                Assert.IsNotNull(mr.sharedMaterial, $"Missing material on '{mr.name}'.");
                Assert.IsTrue(AssetDatabase.GetAssetPath(mr.sharedMaterial).StartsWith("Assets/Settings/"),
                    $"'{mr.name}' must reuse an existing project material.");
            }
        }

        [Test]
        public void GuideRailSystemBracketsShoesAndFullFutureTravelEnvelope_AreCollisionFree()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform shaftRoot = shell.Find(UpperPassengerElevatorShaftBuilder.RootName);
            Transform cabinRoot = shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName);
            Transform railsRoot = shaftRoot.Find(UpperPassengerElevatorCabinBuilder.ShaftGuideRailsGroupName);
            Transform shoesRoot = cabinRoot.Find("Cabin_GuideShoes");

            // 1. Static rail profiles, vertical coverage, and concrete cover spec
            Collider sFlange = railsRoot.Find("Bay1Shaft_GuideRail_South/GuideRail_South_Flange").GetComponent<BoxCollider>();
            Collider sBlade = railsRoot.Find("Bay1Shaft_GuideRail_South/GuideRail_South_Blade").GetComponent<BoxCollider>();
            Collider nFlange = railsRoot.Find("Bay1Shaft_GuideRail_North/GuideRail_North_Flange").GetComponent<BoxCollider>();
            Collider nBlade = railsRoot.Find("Bay1Shaft_GuideRail_North/GuideRail_North_Blade").GetComponent<BoxCollider>();

            Assert.AreEqual(0.070f, sFlange.bounds.size.x, 0.001f);
            Assert.AreEqual(0.010f, sFlange.bounds.size.z, 0.001f);
            Assert.AreEqual(0.016f, sBlade.bounds.size.x, 0.001f);
            Assert.AreEqual(0.040f, sBlade.bounds.size.z, 0.001f);
            Assert.AreEqual(0.070f, nFlange.bounds.size.x, 0.001f);
            Assert.AreEqual(0.016f, nBlade.bounds.size.x, 0.001f);
            Assert.AreEqual(0.040f, nBlade.bounds.size.z, 0.001f);
            Assert.AreEqual(-49.980f, sBlade.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.650f, sBlade.bounds.max.y, 0.002f);
            Assert.AreEqual(-49.980f, nBlade.bounds.min.y, 0.002f);
            Assert.AreEqual(-43.650f, nBlade.bounds.max.y, 0.002f);
            Assert.GreaterOrEqual(UpperPassengerElevatorCabinBuilder.RemainingConcreteCoverM, 0.190f - 0.001f);

            // 5 South + 5 North bracket tiers present & static, zero penetration into Bay 1/2 concrete divider or RoofCap
            Transform bracketsGrp = railsRoot.Find("Bay1Shaft_RailBrackets");
            Assert.AreEqual(10, bracketsGrp.childCount, "Expected exactly 10 rail bracket tiers (5 South + 5 North).");

            Collider dividerCol = shaftRoot.Find("Bay1Shaft_ConcreteDivider/Bay1Shaft_DividerWall_North").GetComponent<BoxCollider>();
            Collider roofCapCol = shaftRoot.Find("Bay1Shaft_UpperEnclosure/UpperShaft_RoofCap").GetComponent<BoxCollider>();
            Collider backroomsFloor = shell.Find("Backrooms_Floor").GetComponent<BoxCollider>();

            foreach (Collider rc in railsRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.IsTrue(rc.gameObject.isStatic, $"Static shaft rail component '{rc.name}' should be static.");
                Assert.IsFalse(StrictlyOverlaps(rc.bounds, dividerCol.bounds), $"Rail component '{rc.name}' penetrates Bay 1/2 divider!");
                Assert.IsFalse(StrictlyOverlaps(rc.bounds, roofCapCol.bounds), $"Rail component '{rc.name}' penetrates RoofCap!");
                Assert.IsFalse(StrictlyOverlaps(rc.bounds, backroomsFloor.bounds), $"Rail component '{rc.name}' penetrates Backrooms_Floor!");
                Assert.GreaterOrEqual(rc.bounds.min.z, 4.439f, $"Rail component '{rc.name}' exits Bay 1 South boundary.");
                Assert.LessOrEqual(rc.bounds.max.z, 6.681f, $"Rail component '{rc.name}' breaches concrete divider into Bay 2!");
            }

            // 2. Guide shoes: 4 assemblies, vertical separation 2.080m, geometric U-channel engagement with deliberate clearance
            Assert.AreEqual(4, shoesRoot.childCount);
            Transform sLower = shoesRoot.Find("GuideShoe_South_Lower");
            Transform sUpper = shoesRoot.Find("GuideShoe_South_Upper");
            Transform nLower = shoesRoot.Find("GuideShoe_North_Lower");
            Transform nUpper = shoesRoot.Find("GuideShoe_North_Upper");
            Assert.AreEqual(2.080f, sUpper.Find("Saddle").position.y - sLower.Find("Saddle").position.y, 0.002f);
            Assert.AreEqual(2.080f, nUpper.Find("Saddle").position.y - nLower.Find("Saddle").position.y, 0.002f);

            // Verify deliberate non-overlapping clearances on both South and North upper/lower shoes
            void AssertShoeEngagesBlade(Transform shoe, Collider blade, bool isSouth)
            {
                Bounds saddle = shoe.Find("Saddle").GetComponent<BoxCollider>().bounds;
                Bounds cw = shoe.Find("CheekW").GetComponent<BoxCollider>().bounds;
                Bounds ce = shoe.Find("CheekE").GetComponent<BoxCollider>().bounds;

                // Lateral straddle: West cheek < blade.min.x < blade.max.x < East cheek (+4mm gap each side)
                Assert.AreEqual(0.004f, blade.bounds.min.x - cw.max.x, 0.001f);
                Assert.AreEqual(0.004f, ce.min.x - blade.bounds.max.x, 0.001f);

                // Depth overlap along blade Z & tip clearance (+6mm)
                if (isSouth)
                {
                    Assert.AreEqual(0.006f, saddle.min.z - blade.bounds.max.z, 0.001f);
                    Assert.Less(cw.min.z, blade.bounds.max.z - 0.030f, "South shoe cheeks must straddle rail blade depth.");
                }
                else
                {
                    Assert.AreEqual(0.006f, blade.bounds.min.z - saddle.max.z, 0.001f);
                    Assert.Greater(cw.max.z, blade.bounds.min.z + 0.030f, "North shoe cheeks must straddle rail blade depth.");
                }
            }
            AssertShoeEngagesBlade(sLower, sBlade, isSouth: true);
            AssertShoeEngagesBlade(sUpper, sBlade, isSouth: true);
            AssertShoeEngagesBlade(nLower, nBlade, isSouth: false);
            AssertShoeEngagesBlade(nUpper, nBlade, isSouth: false);

            // 3. Complete Future Travel Swept Envelope (DeltaY = +3.560m -> Upper stop deck top = -46.400, roof top = -44.040)
            // Validate EVERY cabin collider (shell + finishes + all 4 guide shoes) across full vertical sweep against:
            //   - All static shaft walls, divider, roof cap, 4-piece Backrooms_Ceiling, upper waiting structure
            //   - All static guide rails & brackets (deliberate >= 4mm/6mm/8mm clearance — ZERO solid overlap!)
            //   - All four 07.25A reserved 2-speed telescopic cabin & landing door swept volumes!
            List<Collider> staticObstacles = new List<Collider>();
            staticObstacles.Add(shell.Find("Backrooms_Ceiling").GetComponent<Collider>());
            staticObstacles.Add(backroomsFloor);
            staticObstacles.AddRange(shaftRoot.GetComponentsInChildren<Collider>(includeInactive: true));
            Transform waitGrp = shell.Find($"{UpperElevatorApproachBuilder.RootName}/UpperElevatorApproach_WaitingArea");
            staticObstacles.Add(waitGrp.Find("UpperElev_Waiting_FloorSlab").GetComponent<Collider>());
            staticObstacles.Add(waitGrp.Find("UpperElev_Waiting_CeilingSlab").GetComponent<Collider>());
            staticObstacles.Add(waitGrp.Find("UpperElev_Waiting_WestCoreBackingWall").GetComponent<Collider>());
            Transform supportGrp = shell.Find($"{UpperElevatorApproachBuilder.RootName}/UpperElevatorApproach_SupportStructure");
            staticObstacles.Add(supportGrp.Find("UpperElev_Support_WaitingCorePlinthW").GetComponent<Collider>());

            // Four 07.25A reserved telescopic door swept volumes across cabin lower-stop door height Y in [-49.960, -47.760]
            // (checked at lower stop for cabin doors, and across full vertical sweep for landing doors!)
            Bounds MakeMinMax(Vector3 min, Vector3 max) { Bounds b = new Bounds(); b.SetMinMax(min, max); return b; }
            Bounds cabinSlowLeafVol = MakeMinMax(new Vector3(2.965f, -49.960f, 4.720f), new Vector3(2.990f, -47.760f, 5.850f));
            Bounds cabinFastLeafVol = MakeMinMax(new Vector3(3.000f, -49.960f, 4.720f), new Vector3(3.025f, -47.760f, 6.400f));
            Bounds landingFastLeafLower = MakeMinMax(new Vector3(3.060f, -49.960f, 4.720f), new Vector3(3.085f, -47.760f, 6.400f));
            Bounds landingSlowLeafLower = MakeMinMax(new Vector3(3.095f, -49.960f, 4.720f), new Vector3(3.120f, -47.760f, 5.850f));
            Bounds landingFastLeafUpper = ShiftY(landingFastLeafLower, UpperPassengerElevatorCabinBuilder.FutureTravelDeltaY);
            Bounds landingSlowLeafUpper = ShiftY(landingSlowLeafLower, UpperPassengerElevatorCabinBuilder.FutureTravelDeltaY);

            Collider[] cabinCols = cabinRoot.GetComponentsInChildren<Collider>(includeInactive: true);
            for (int i = 0; i < cabinCols.Length; i++)
            {
                Bounds curB = cabinCols[i].bounds;
                Bounds sweptB = SweepY(curB, UpperPassengerElevatorCabinBuilder.FutureTravelDeltaY);

                // Check against every static shaft/rail/bracket/ceiling/upper-landing collider across ENTIRE 3.560m sweep!
                for (int j = 0; j < staticObstacles.Count; j++)
                {
                    Assert.IsFalse(StrictlyOverlaps(sweptB, staticObstacles[j].bounds),
                        $"Future travel collision: cabin '{cabinCols[i].name}' swept {sweptB} hits static '{staticObstacles[j].name}' {staticObstacles[j].bounds}!");
                }

                // Check against cabin door volumes (co-moving at lower stop; skip the actual CabinDoor leaves in 07.29+)
                // and both lower/upper landing door volumes
                bool isCabinDoorLeaf = cabinCols[i].name == "CabinDoor_SlowLeaf" || cabinCols[i].name == "CabinDoor_FastLeaf";
                if (!isCabinDoorLeaf)
                {
                    Assert.IsFalse(StrictlyOverlaps(curB, cabinSlowLeafVol), $"Cabin '{cabinCols[i].name}' obstructs reserved Cabin Slow Leaf volume!");
                    Assert.IsFalse(StrictlyOverlaps(curB, cabinFastLeafVol), $"Cabin '{cabinCols[i].name}' obstructs reserved Cabin Fast Leaf volume!");
                }
                Assert.IsFalse(StrictlyOverlaps(sweptB, landingFastLeafLower), $"Cabin '{cabinCols[i].name}' swept volume hits Lower Landing Fast Leaf!");
                Assert.IsFalse(StrictlyOverlaps(sweptB, landingSlowLeafLower), $"Cabin '{cabinCols[i].name}' swept volume hits Lower Landing Slow Leaf!");
                Assert.IsFalse(StrictlyOverlaps(sweptB, landingFastLeafUpper), $"Cabin '{cabinCols[i].name}' swept volume hits Upper Landing Fast Leaf!");
                Assert.IsFalse(StrictlyOverlaps(sweptB, landingSlowLeafUpper), $"Cabin '{cabinCols[i].name}' swept volume hits Upper Landing Slow Leaf!");
            }

            // Confirm rails remain continuously engaged with all 4 shoes at BOTH Lower Stop and Upper Stop!
            foreach (Transform shoe in new[] { sLower, sUpper, nLower, nUpper })
            {
                Bounds sb = shoe.Find("Saddle").GetComponent<BoxCollider>().bounds;
                Bounds sbUpper = ShiftY(sb, UpperPassengerElevatorCabinBuilder.FutureTravelDeltaY);
                Assert.GreaterOrEqual(sb.min.y, sBlade.bounds.min.y + 0.050f, $"Lower stop shoe '{shoe.name}' too close to rail bottom.");
                Assert.LessOrEqual(sbUpper.max.y, sBlade.bounds.max.y - 0.150f, $"Upper stop shoe '{shoe.name}' exits rail top!");
            }

            // Verify static rails/brackets also stay completely clear of all four reserved door volumes!
            foreach (Collider rc in railsRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Assert.Less(rc.bounds.max.x, 2.300f, $"Rail/bracket '{rc.name}' must stay well West of door sweep (X >= +2.965).");
            }
        }

        [Test]
        public void ProtectedInvariantsExact18LightsAndSevenBuilderCrossIdempotency_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");

            // Protected lower core (exact 35 children), adjacent fixtures, uncut floor, 07.24 facades, Lightwell, Room 111-B, schemaVersion=1, BorderCorridorScene
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorsHeader"));
            Assert.IsNotNull(GameObject.Find("Hotel_Sign_ElevatorNotice"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorJunctionBox"));
            Assert.IsNotNull(GameObject.Find("Hotel_Elec_ElevatorConduitRun"));
            Assert.IsNotNull(GameObject.Find("Hotel_Lobby_Beam_West"));
            Assert.AreEqual(1, new ProgressionSaveData().schemaVersion);
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"));

            // Exact 18 Unity Light components, zero shadow casting
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene Unity Light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++) Assert.AreEqual(LightShadows.None, allLights[i].shadows);

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // Full 7-builder cross-idempotency (07.17 -> 07.19 -> 07.21 -> 07.23 -> 07.24 -> 07.26 -> 07.27)
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;
            int cabinIdBefore = shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName).gameObject.GetInstanceID();

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);
            EastOverhangBuilder.MigrateEastOverhangInOpenScene(scene);
            UpperElevatorApproachBuilder.MigrateUpperElevatorApproachInOpenScene(scene);
            UpperElevatorBayArchitectureBuilder.MigrateUpperElevatorBayArchitectureInOpenScene(scene);
            UpperPassengerElevatorShaftBuilder.MigrateUpperPassengerElevatorShaftInOpenScene(scene);
            UpperPassengerElevatorCabinBuilder.MigrateUpperPassengerElevatorCabinInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);
            Assert.AreEqual(cabinIdBefore, shell.Find(UpperPassengerElevatorCabinBuilder.CabinRootName).gameObject.GetInstanceID());

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => UpperPassengerElevatorCabinBuilder.MigrateUpperPassengerElevatorCabinInOpenScene(emptyScene));
        }
    }
}
