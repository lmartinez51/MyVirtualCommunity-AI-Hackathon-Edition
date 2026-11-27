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
    /// Dedicated automated EditMode verification suite for Implementation 07.19:
    /// Courtyard Lightwell Overlook — Alternative C (Single-Bay Framed Architectural Observation
    /// with a +0.540m Three-Step Concrete Viewing Dais and Transparent Glass Safety Barrier).
    /// Covers all 35 verification checks: construction & idempotency, three-step dais geometry &
    /// end-to-end CharacterController.Move() traversal, real scene raycast sightlines against
    /// NorthWing_Lightwell_SkylightCap, physical containment & safety, and all protected invariants.
    /// </summary>
    [TestFixture]
    public class RemanenciaCourtyardLightwellOverlookTests
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
        public void OverlookStructure_OpeningStepsDaisBarrierAndCompactEnclosure_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. Observation root exists exactly once, zero runtime scripts, zero added Light components
            int rootCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                if (shell.GetChild(i).name == CourtyardLightwellOverlookBuilder.RootName) rootCount++;
            }
            Assert.AreEqual(1, rootCount, "Hotel_CourtyardLightwellOverlook must exist exactly once.");
            Assert.IsTrue(CourtyardLightwellOverlookBuilder.VerifyStructurePresent(shell));

            Transform overlookRoot = shell.Find(CourtyardLightwellOverlookBuilder.RootName);
            Assert.AreEqual(0, overlookRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "Overlook must contain zero runtime MonoBehaviours.");
            Assert.AreEqual(0, overlookRoot.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Overlook must add zero Unity Light components.");

            // 4. Exact authorized single-bay opening X in [-4.460, +0.060] (4.520m wide)
            Transform galleryRoot = shell.Find(NorthUpperGalleryBuilder.RootName);
            Assert.IsNotNull(galleryRoot);
            Collider wallSouthEast = galleryRoot.Find("NUGallery_Structure/NUGallery_WallSouth").GetComponent<Collider>();
            Collider wallSouthWest = overlookRoot.Find("Overlook_GalleryTransition/Overlook_WallSouth_WestSegment").GetComponent<Collider>();
            Assert.AreEqual(-4.460f, wallSouthWest.bounds.max.x, 0.005f, "West edge of South opening must equal X=-4.460m.");
            Assert.AreEqual(0.060f, wallSouthEast.bounds.min.x, 0.005f, "East edge of South opening must equal X=+0.060m.");
            Assert.AreEqual(4.520f, wallSouthEast.bounds.min.x - wallSouthWest.bounds.max.x, 0.01f, "Opening width must equal 4.520m.");

            // 5, 6, 7, 28, 29. All 5 South pilasters, all 5 ceiling beams, and all 4 utility strips preserved untouched
            Transform details = galleryRoot.Find("NUGallery_Details");
            float[] expectedBayXs = { -4.60f, 0.20f, 5.00f, 9.80f, 14.20f };
            for (int i = 0; i < expectedBayXs.Length; i++)
            {
                Transform p = details.Find($"NUGallery_PilasterSouth_{i}");
                Transform b = details.Find($"NUGallery_CeilingBeam_{i}");
                Assert.IsNotNull(p, $"Missing preserved NUGallery_PilasterSouth_{i}");
                Assert.IsNotNull(b, $"Missing preserved NUGallery_CeilingBeam_{i}");
                Assert.AreEqual(expectedBayXs[i], p.position.x, 0.005f);
                Assert.AreEqual(expectedBayXs[i], b.position.x, 0.005f);
            }
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(details.Find($"NUGallery_StripTube_{i}"), $"Missing preserved NUGallery_StripTube_{i}");
            }

            // 8. Compact scope check: all alcove objects stay strictly compact (no 156m² chamber around Lightwell!)
            Collider lightwellCap = GameObject.Find("NorthWing_Lightwell_SkylightCap")?.GetComponent<Collider>();
            Assert.IsNotNull(lightwellCap);
            foreach (Renderer r in overlookRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Bounds b = r.bounds;
                Assert.IsFalse(b.Intersects(lightwellCap.bounds), $"Overlook object '{r.name}' must never intersect SkylightCap.");
                if (r.name == "Overlook_WallSouth_WestSegment" || r.name == "Overlook_BaseboardSouth_WestSegment")
                {
                    Assert.GreaterOrEqual(b.min.x, -9.75f);
                    Assert.LessOrEqual(b.max.x, -4.44f);
                }
                else
                {
                    Assert.GreaterOrEqual(b.min.x, -4.72f, $"Overlook object '{r.name}' extends West outside compact bay (minX={b.min.x:F3}).");
                    Assert.LessOrEqual(b.max.x, 0.32f, $"Overlook object '{r.name}' extends East outside compact bay (maxX={b.max.x:F3}).");
                    Assert.GreaterOrEqual(b.min.z, 27.15f, $"Overlook object '{r.name}' extends too far South (minZ={b.min.z:F3}; compact bay only!).");
                    Assert.LessOrEqual(b.max.z, 29.86f, $"Overlook object '{r.name}' encroaches into gallery walkway (maxZ={b.max.z:F3}).");
                }
            }

            // 9, 10, 11, 12. Exactly three visible concrete steps, exact risers (0.18m) & treads (0.28m), raised dais (-45.860m)
            Transform daisGroup = overlookRoot.Find("Overlook_ViewingDais");
            float prevTopY = CourtyardLightwellOverlookBuilder.GalleryFloorY;
            for (int i = 0; i < 3; i++)
            {
                Transform step = daisGroup.Find($"Overlook_Dais_Step_{i + 1:00}");
                Assert.IsNotNull(step, $"Missing visible concrete step {i + 1}.");
                Collider stepCol = step.GetComponent<Collider>();
                MeshRenderer stepMr = step.GetComponent<MeshRenderer>();
                Assert.IsNotNull(stepCol);
                Assert.IsTrue(stepCol.enabled && !stepCol.isTrigger);
                Assert.IsNotNull(stepMr);
                Assert.IsTrue(stepMr.enabled);

                float riser = stepCol.bounds.max.y - prevTopY;
                float tread = stepCol.bounds.size.z;
                Assert.AreEqual(0.180f, riser, 0.003f, $"Step {i + 1} riser height mismatch.");
                Assert.AreEqual(0.280f, tread, 0.003f, $"Step {i + 1} tread depth mismatch.");
                prevTopY = stepCol.bounds.max.y;
            }
            Assert.AreEqual(-45.860f, prevTopY, 0.003f, "Top of Step 3 must reach exact raised platform elevation -45.860m.");

            Collider daisCol = daisGroup.Find("Overlook_Dais_PlatformSlab").GetComponent<Collider>();
            Assert.AreEqual(-45.860f, daisCol.bounds.max.y, 0.003f, "Raised viewing platform top must equal -45.860m.");
            Assert.AreEqual(27.400f, daisCol.bounds.min.z, 0.003f);
            Assert.AreEqual(28.740f, daisCol.bounds.max.z, 0.003f);
            Assert.AreEqual(4.520f, daisCol.bounds.size.x, 0.005f);

            // Ceiling baseline at Y = -43.550m gives 2.310m clear headroom over dais
            Collider ceilCol = overlookRoot.Find("Overlook_CompactEnclosure/Overlook_CeilingSlab").GetComponent<Collider>();
            Assert.AreEqual(-43.550f, ceilCol.bounds.min.y, 0.003f);
            Assert.AreEqual(2.310f, ceilCol.bounds.min.y - daisCol.bounds.max.y, 0.005f);

            // 21. Parapet (1.05m tall, Z in [27.20, 27.40]) & visibly represented transparent glass panels with valid colliders
            Transform barrier = overlookRoot.Find("Overlook_SafetyBarrier");
            Collider parapetCol = barrier.Find("Overlook_Parapet_Concrete").GetComponent<Collider>();
            Assert.AreEqual(-44.810f, parapetCol.bounds.max.y, 0.003f);
            Assert.AreEqual(27.200f, parapetCol.bounds.min.z, 0.003f);
            Assert.AreEqual(27.400f, parapetCol.bounds.max.z, 0.003f);
            Assert.GreaterOrEqual(parapetCol.bounds.min.z - lightwellCap.bounds.max.z, 0.89f,
                "Parapet must stay >= 0.90m clear North of SkylightCap.");

            for (int p = 0; p < 3; p++)
            {
                Transform panel = barrier.Find($"Overlook_SafetyGlass_Panel_{p}");
                Assert.IsNotNull(panel, $"Missing visible transparent glass panel {p}.");
                MeshRenderer mr = panel.GetComponent<MeshRenderer>();
                Collider col = panel.GetComponent<Collider>();
                Assert.IsNotNull(mr);
                Assert.IsTrue(mr.enabled, $"Glass panel {p} MeshRenderer must be visibly enabled.");
                Assert.IsNotNull(mr.sharedMaterial);
                Assert.AreEqual("Transparent", mr.sharedMaterial.GetTag("RenderType", false),
                    $"Glass panel {p} must use a Transparent URP material.");
                Assert.IsNotNull(col);
                Assert.IsTrue(col.enabled && !col.isTrigger, $"Glass panel {p} must have an enabled solid collider.");
            }
        }

        [Test]
        public void SightlinesAndOpacity_RaisedDaisRevealsGlowingSkylightRoofAndCorners()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // 19. Opaque Lightwell cap remains opaque (_Surface == 0)
            GameObject capGo = GameObject.Find("NorthWing_Lightwell_SkylightCap");
            Assert.IsNotNull(capGo);
            MeshRenderer capMr = capGo.GetComponent<MeshRenderer>();
            Assert.IsNotNull(capMr);
            Assert.IsNotNull(capMr.sharedMaterial);
            Assert.AreEqual(0f, capMr.sharedMaterial.GetFloat("_Surface"), 0.01f,
                "Protected NorthWing_Lightwell_SkylightCap material must remain opaque (_Surface == 0).");

            // 17, 18, 20. Real Physics raycasts from actual grounded standing eye on raised dais (Y_eye = -44.220m)
            // Verify rays passing through the transparent glass panels (ignoring only the glass panels themselves so
            // we confirm zero opaque obstruction!) hit NorthWing_Lightwell_SkylightCap's upper luminous surface (Y+ normal!),
            // its northern fascia edge (Z+ normal), and both NW/NE corners!
            const float eyeY = CourtyardLightwellOverlookBuilder.DaisFloorY + 0.040f + 1.600f; // -44.220f
            Vector3 centerEye = new Vector3(-2.20f, eyeY, 27.78f);
            Vector3 westEye = new Vector3(-3.90f, eyeY, 27.78f);
            Vector3 eastEye = new Vector3(-0.35f, eyeY, 27.78f);

            bool RayHitsTargetThroughTransparentGlass(Vector3 origin, Vector3 targetPoint, string expectedName, out RaycastHit hit)
            {
                Vector3 dir = (targetPoint - origin).normalized;
                float dist = Vector3.Distance(origin, targetPoint) + 1.0f;
                RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    string n = hits[i].collider.gameObject.name;
                    if (n.StartsWith("Overlook_SafetyGlass_Panel_", StringComparison.Ordinal)) continue;
                    hit = hits[i];
                    return string.Equals(n, expectedName, StringComparison.Ordinal);
                }
                hit = default;
                return false;
            }

            // Direct hit on SkylightCap TOP surface (Y+ normal) from center dais!
            Assert.IsTrue(RayHitsTargetThroughTransparentGlass(centerEye, new Vector3(-2.20f, -44.640f, 23.00f), "NorthWing_Lightwell_SkylightCap", out RaycastHit topHit),
                $"Center dais sightline to SkylightCap top surface was blocked by '{topHit.collider?.name}'.");
            Assert.Greater(topHit.normal.y, 0.90f, "Center dais ray must strike the UPPER glowing surface (Y+ normal) of SkylightCap.");

            // Direct hit on SkylightCap North fascia edge (Z+ normal)
            Assert.IsTrue(RayHitsTargetThroughTransparentGlass(centerEye, new Vector3(-2.20f, -44.720f, 26.300f), "NorthWing_Lightwell_SkylightCap", out RaycastHit northHit),
                $"Center dais sightline to SkylightCap North fascia blocked by '{northHit.collider?.name}'.");
            Assert.Greater(northHit.normal.z, 0.90f, "Ray to fascia must strike North face (Z+ normal).");

            // NW corner (X=-5.15, Z=26.15) visible from East side of dais; NE corner (X=+5.15, Z=26.15) visible from West side of dais
            Assert.IsTrue(RayHitsTargetThroughTransparentGlass(eastEye, new Vector3(-5.15f, -44.640f, 26.15f), "NorthWing_Lightwell_SkylightCap", out RaycastHit nwHit),
                $"NW skylight corner sightline from East dais blocked by '{nwHit.collider?.name}'.");
            Assert.IsTrue(RayHitsTargetThroughTransparentGlass(westEye, new Vector3(5.15f, -44.640f, 26.15f), "NorthWing_Lightwell_SkylightCap", out RaycastHit neHit),
                $"NE skylight corner sightline from West dais blocked by '{neHit.collider?.name}'.");

            // Verify central glass panel is transparent (alpha <= 0.30) and non-obstructive
            Transform centerGlass = GameObject.Find("Overlook_SafetyGlass_Panel_1")?.transform;
            Assert.IsNotNull(centerGlass);
            Color c = centerGlass.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
            Assert.LessOrEqual(c.a, 0.30f, "Central glass barrier alpha must remain <= 0.30 so skylight view is clear.");
            Assert.GreaterOrEqual(c.a, 0.10f, "Central glass barrier alpha must remain >= 0.10 so barrier is visibly legible.");
        }

        [Test]
        public void EndToEndCharacterControllerTraversal_AndPhysicalContainment_Succeed()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
            float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;

            GameObject ccGo = new GameObject("__Test0719PlayerCC__");
            try
            {
                CharacterController cc = ccGo.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.28f;
                cc.skinWidth = 0.04f;
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.30f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                ccGo.transform.position = new Vector3(-2.20f, -46.36f, 31.30f);
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

                // 13 & 14. Climb the three concrete steps onto raised viewing dais (Y = -45.860m)
                WalkTo(new Vector2(-2.20f, 27.95f), 100);
                Assert.Less(ccGo.transform.position.z, 28.20f, $"Player CC failed to enter dais (stopped at Z={ccGo.transform.position.z:F2}).");
                Assert.AreEqual(-45.860f, ccGo.transform.position.y - cc.skinWidth, 0.05f,
                    $"Player CC floor height on dais mismatch (pivot Y={ccGo.transform.position.y:F3}).");

                // 22 & 23. Push hard South against parapet/glass barrier — must NOT pass Z = 27.40 or reach SkylightCap!
                for (int k = 0; k < 50; k++) cc.Move(new Vector3(0f, -4.0f, -4.0f) * 0.04f);
                Assert.GreaterOrEqual(ccGo.transform.position.z, 27.68f,
                    $"Player CC penetrated observation barrier (Z={ccGo.transform.position.z:F3})!");
                Assert.AreEqual(-45.860f, ccGo.transform.position.y - cc.skinWidth, 0.05f,
                    "Player CC climbed onto parapet or fell while pushing South barrier!");

                // 24 & 25. Walk laterally West and East across full dais width and push side walls — strictly contained!
                WalkTo(new Vector2(-4.05f, 27.95f), 80);
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(-4.0f, -4.0f, -1.5f) * 0.04f);
                Assert.GreaterOrEqual(ccGo.transform.position.x, -4.18f,
                    $"Player CC escaped West alcove sidewall (X={ccGo.transform.position.x:F3})!");
                Assert.GreaterOrEqual(ccGo.transform.position.z, 27.68f);

                WalkTo(new Vector2(-0.35f, 27.95f), 100);
                for (int k = 0; k < 40; k++) cc.Move(new Vector3(4.0f, -4.0f, -1.5f) * 0.04f);
                Assert.LessOrEqual(ccGo.transform.position.x, -0.22f,
                    $"Player CC escaped East alcove sidewall (X={ccGo.transform.position.x:F3})!");
                Assert.GreaterOrEqual(ccGo.transform.position.z, 27.68f);

                // 15 & 27. Return smoothly down the three concrete steps back into North Upper Gallery (zero snagging)
                WalkTo(new Vector2(-2.20f, 31.30f), 100);
                Assert.Greater(ccGo.transform.position.z, 31.10f, "Player CC failed to return from dais into gallery.");
                Assert.AreEqual(-46.400f, ccGo.transform.position.y - cc.skinWidth, 0.06f,
                    "Player CC failed to step cleanly back down to gallery floor elevation.");

                // 16. Continue West through East opening and descend 07.15 Northwest Service Staircase back to Nivel 0!
                WalkTo(new Vector2(-10.70f, 31.30f), 140);
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

            // 21 & 26. Continuous barrier linecast across full 4.52m width at parapet & glass heights (zero gap!)
            float[] checkXs = { -4.35f, -3.50f, -2.93f, -2.20f, -1.47f, -0.60f, -0.05f };
            float[] checkYs = { -45.30f, -44.85f, -44.40f, -43.90f, -43.65f };
            foreach (float cx in checkXs)
            {
                foreach (float cy in checkYs)
                {
                    Assert.IsTrue(Physics.Linecast(new Vector3(cx, cy, 27.60f), new Vector3(cx, cy, 27.00f), ~0, QueryTriggerInteraction.Ignore),
                        $"Barrier collision gap detected at X={cx:F2}, Y={cy:F2}!");
                }
            }
        }

        [Test]
        public void ProtectedLightwellLightsIdempotencyAndSurfaceWorld_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 30. Protected Lightwell objects preserve exact transforms, scales, and colliders
            void AssertProtectedBox(string name, Vector3 pos, Vector3 scl)
            {
                GameObject go = GameObject.Find(name);
                Assert.IsNotNull(go, $"Protected Lightwell object '{name}' missing!");
                Assert.AreEqual(pos.x, go.transform.position.x, 0.002f, $"{name}.pos.x altered!");
                Assert.AreEqual(pos.y, go.transform.position.y, 0.002f, $"{name}.pos.y altered!");
                Assert.AreEqual(pos.z, go.transform.position.z, 0.002f, $"{name}.pos.z altered!");
                Assert.AreEqual(scl.x, go.transform.localScale.x, 0.002f, $"{name}.scl.x altered!");
                Assert.AreEqual(scl.y, go.transform.localScale.y, 0.002f, $"{name}.scl.y altered!");
                Assert.AreEqual(scl.z, go.transform.localScale.z, 0.002f, $"{name}.scl.z altered!");
                Collider c = go.GetComponent<Collider>();
                Assert.IsNotNull(c, $"{name} missing collider!");
                Assert.IsTrue(c.enabled && !c.isTrigger);
            }

            AssertProtectedBox("NorthWing_Lightwell_WallN", new Vector3(0.0f, -45.975f, 26.08f), new Vector3(10.32f, 2.35f, 0.20f));
            AssertProtectedBox("NorthWing_Lightwell_WallS", new Vector3(0.0f, -45.975f, 17.92f), new Vector3(10.32f, 2.35f, 0.20f));
            AssertProtectedBox("NorthWing_Lightwell_WallW", new Vector3(-5.08f, -45.975f, 22.00f), new Vector3(0.20f, 2.35f, 8.32f));
            AssertProtectedBox("NorthWing_Lightwell_WallE", new Vector3(5.08f, -45.975f, 22.00f), new Vector3(0.20f, 2.35f, 8.32f));
            AssertProtectedBox("NorthWing_Lightwell_SkylightCap", new Vector3(0.0f, -44.72f, 22.00f), new Vector3(10.60f, 0.16f, 8.60f));

            // 31, 32, 33, 34, 35. Elevators, Room 111-B, Threshold of Trust, entry-only rule, exact 18 lights, BorderCorridorScene
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

            // 2. Cross-builder idempotency (running both 07.17 gallery and 07.19 overlook migrations preserves exact counts & opening!)
            int beforeT = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeC = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeR = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeL = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthUpperGalleryBuilder.MigrateNorthUpperGalleryInOpenScene(scene);
            CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(scene);

            Assert.AreEqual(beforeT, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeC, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeR, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeL, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            // Non-target scene rejection
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => CourtyardLightwellOverlookBuilder.MigrateCourtyardLightwellOverlookInOpenScene(emptyScene));
        }
    }
}
