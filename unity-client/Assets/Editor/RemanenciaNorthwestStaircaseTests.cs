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
    /// Comprehensive automated EditMode verification suite for Implementation 07.15:
    /// Option B — Northwest Service Staircase in Remanencia — Nivel 0 (Hotel de las Palomas).
    /// Covers all 13 verification requirements including real CharacterController.Move()
    /// physical climb and descent, exact step/riser/tread geometry, headroom, localized
    /// ceiling opening, sealed upper landing shell, idempotency, and surface-world immutability.
    /// </summary>
    [TestFixture]
    public class RemanenciaNorthwestStaircaseTests
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

        private static bool SampleFloorAndHeadroom(float x, float z, float expectedFloorY, float floorTolerance, out float actualFloorY, out float headroom)
        {
            actualFloorY = float.NaN;
            headroom = 0f;

            Vector3 downOrigin = new Vector3(x, expectedFloorY + 1.15f, z);
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit floorHit, 2.20f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            actualFloorY = floorHit.point.y;
            if (Mathf.Abs(actualFloorY - expectedFloorY) > floorTolerance)
            {
                return false;
            }

            Vector3 upOrigin = new Vector3(x, actualFloorY + 0.10f, z);
            if (!Physics.Raycast(upOrigin, Vector3.up, out RaycastHit ceilHit, 10.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            headroom = ceilHit.point.y - actualFloorY;
            return true;
        }

        private static bool IsPlayerCapsuleClearAt(Vector3 footPos)
        {
            const float radius = 0.30f; // Player CC radius 0.28 + 0.02 margin
            Vector3 pBot = footPos + Vector3.up * (radius + 0.14f);
            Vector3 pTop = footPos + Vector3.up * (1.80f - radius - 0.04f);
            Collider[] hits = Physics.OverlapCapsule(pBot, pTop, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].enabled && !hits[i].isTrigger)
                {
                    return false;
                }
            }
            return true;
        }

        [Test]
        public void StaircaseGeometry_ElevationsRisersTreadsWidthsAndEnvelope_AreExact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform shell = root.transform.Find("Backrooms_Architecture");
            Assert.IsNotNull(shell);

            // 1. Staircase root exists exactly once
            int stairRootCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                if (shell.GetChild(i).name == NorthwestServiceStaircaseBuilder.RootName)
                {
                    stairRootCount++;
                }
            }
            Assert.AreEqual(1, stairRootCount, "Hotel_NorthwestServiceStaircase root must exist exactly once.");

            Transform stairRoot = shell.Find(NorthwestServiceStaircaseBuilder.RootName);
            Assert.IsTrue(NorthwestServiceStaircaseBuilder.VerifyStructurePresent(shell),
                "All 6 required staircase subgroups must be present.");

            // Zero new runtime MonoBehaviours and zero new Light components inside staircase assembly
            Assert.AreEqual(0, stairRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length,
                "Northwest staircase must contain zero runtime MonoBehaviours.");
            Assert.AreEqual(0, stairRoot.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Northwest staircase must add zero Unity Light components (preserves 18-light budget).");

            // 3, 4, 5, 6. Exact elevations, riser count/height, tread count/depth, widths & landing depths
            Assert.AreEqual(-50.00f, NorthwestServiceStaircaseBuilder.LowerFloorY, 0.001f);
            Assert.AreEqual(-48.20f, NorthwestServiceStaircaseBuilder.IntermediateLandingY, 0.001f);
            Assert.AreEqual(-46.40f, NorthwestServiceStaircaseBuilder.UpperLandingY, 0.001f);
            Assert.AreEqual(3.60f, NorthwestServiceStaircaseBuilder.TotalVerticalRise, 0.001f);
            Assert.AreEqual(20, NorthwestServiceStaircaseBuilder.TotalRisers);
            Assert.AreEqual(0.18f, NorthwestServiceStaircaseBuilder.RiserHeight, 0.001f);
            Assert.AreEqual(18, NorthwestServiceStaircaseBuilder.TotalTreads);
            Assert.AreEqual(0.28f, NorthwestServiceStaircaseBuilder.TreadDepth, 0.001f);
            Assert.AreEqual(2.52f, NorthwestServiceStaircaseBuilder.FlightHorizontalRun, 0.001f);
            Assert.GreaterOrEqual(NorthwestServiceStaircaseBuilder.FlightClearWidth, 1.45f);
            Assert.GreaterOrEqual(NorthwestServiceStaircaseBuilder.MidLandingDepth, 1.45f);
            Assert.GreaterOrEqual(NorthwestServiceStaircaseBuilder.UpperLandingDepth, 1.45f);

            // Verify actual scene step objects match riser/tread counts & measurements
            Transform f1 = stairRoot.Find("NWStair_Flight1_Lower");
            Transform f2 = stairRoot.Find("NWStair_Flight2_Upper");
            for (int i = 1; i <= 9; i++)
            {
                Transform s1 = f1.Find($"NWStair_F1_Step_{i:00}");
                Transform s2 = f2.Find($"NWStair_F2_Step_{i:00}");
                Assert.IsNotNull(s1, $"Missing F1 step {i:00}");
                Assert.IsNotNull(s2, $"Missing F2 step {i:00}");
                Assert.AreEqual(0.28f, s1.localScale.x, 0.01f, $"F1 step {i:00} tread depth mismatch.");
                Assert.AreEqual(0.28f, s2.localScale.x, 0.01f, $"F2 step {i:00} tread depth mismatch.");
                Assert.GreaterOrEqual(s1.localScale.z, 1.45f);
                Assert.GreaterOrEqual(s2.localScale.z, 1.45f);
                float f1Top = s1.position.y + s1.localScale.y * 0.5f;
                float f2Top = s2.position.y + s2.localScale.y * 0.5f;
                Assert.AreEqual(NorthwestServiceStaircaseBuilder.LowerFloorY + i * 0.18f, f1Top, 0.005f, $"F1 step {i:00} elevation mismatch.");
                Assert.AreEqual(NorthwestServiceStaircaseBuilder.IntermediateLandingY + i * 0.18f, f2Top, 0.005f, $"F2 step {i:00} elevation mismatch.");
            }
            Assert.IsNotNull(f1.Find("NWStair_F1_Riser_10"));
            Assert.IsNotNull(f2.Find("NWStair_F2_Riser_20"));

            // Verify physical landing slab top elevations
            Collider midCol = stairRoot.Find("NWStair_IntermediateLanding/NWStair_MidLanding_Slab")?.GetComponent<Collider>();
            Collider upCol = stairRoot.Find("NWStair_UpperLandingShell/NWStair_UpperLanding_FloorSlab")?.GetComponent<Collider>();
            Assert.IsNotNull(midCol);
            Assert.IsNotNull(upCol);
            Assert.AreEqual(-48.20f, midCol.bounds.max.y, 0.005f, "Intermediate landing top elevation must equal -48.20m.");
            Assert.AreEqual(-46.40f, upCol.bounds.max.y, 0.005f, "Upper landing top elevation must equal -46.40m.");

            // 7. All staircase geometry (excluding NWStair_CeilingFill_SouthCorridor which patches Maintenance/Linen ceiling)
            //    stays strictly inside authorized Northwest reserve envelope X in [-16.30, -9.45], Z in [28.60, 34.25]
            foreach (Renderer r in stairRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (r.gameObject.name == "NWStair_CeilingFill_SouthCorridor") continue;
                Bounds b = r.bounds;
                Assert.GreaterOrEqual(b.min.x, -16.30f, $"Object '{r.gameObject.name}' extends West outside NW envelope (minX={b.min.x:F2}).");
                Assert.LessOrEqual(b.max.x, -9.45f, $"Object '{r.gameObject.name}' extends East into Room 110 (maxX={b.max.x:F2}).");
                Assert.GreaterOrEqual(b.min.z, 28.60f, $"Object '{r.gameObject.name}' extends South outside NW envelope (minZ={b.min.z:F2}).");
                Assert.LessOrEqual(b.max.z, 34.25f, $"Object '{r.gameObject.name}' extends North outside NW envelope (maxZ={b.max.z:F2}).");
            }
        }

        [Test]
        public void CeilingOpening_UpperShellSealing_HeadroomAndPhysicalTraversal_AreValid()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            // 9. Ceiling opening is strictly localized to the NW reserve while preserving unaffected ceilings
            //    Rooms 110, 109, 108, 107, North Guest Corridor, Maintenance Corridor & Linen Room remain sealed at Y ~ -47.15
            Vector2[] preservedCeilingSamples =
            {
                new Vector2(-6.50f, 31.20f),  // Room 110
                new Vector2( 0.00f, 31.20f),  // Room 109
                new Vector2( 6.40f, 31.20f),  // Room 108
                new Vector2(12.80f, 31.20f),  // Room 107
                new Vector2( 0.00f, 27.40f),  // North Guest Corridor
                new Vector2(-10.80f, 27.40f), // Maintenance Corridor North end (patched by NWStair_CeilingFill_SouthCorridor)
                new Vector2(-14.00f, 26.80f)  // Linen Room (patched by NWStair_CeilingFill_SouthCorridor)
            };
            foreach (Vector2 pt in preservedCeilingSamples)
            {
                bool hitCeil = Physics.Raycast(new Vector3(pt.x, -49.0f, pt.y), Vector3.up, out RaycastHit hit, 3.0f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(hitCeil, $"Preserved ceiling missing above ({pt.x:F2}, {pt.y:F2}).");
                Assert.AreEqual(-47.15f, hit.point.y, 0.05f, $"Preserved ceiling height altered above ({pt.x:F2}, {pt.y:F2}).");
            }

            // Verify NW Stairwell shaft IS open past Y = -47.15 up to Upper Ceiling (-43.55)
            bool shaftUp = Physics.Raycast(new Vector3(-13.00f, -47.90f, 32.90f), Vector3.up, out RaycastHit shaftHit, 6.0f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(shaftUp, "Upper stairwell ceiling must seal shaft overhead.");
            Assert.AreEqual(-43.55f, shaftHit.point.y, 0.05f, "Stairwell must open cleanly up to sealed Upper Ceiling at Y = -43.55m.");

            // 8 & 11. Headroom (>= 2.10m) and capsule clearance sampled along entire climb route (Lower -> F1 -> Mid -> F2 -> Upper)
            float f1Z = (NorthwestServiceStaircaseBuilder.Flight1MinZ + NorthwestServiceStaircaseBuilder.Flight1MaxZ) * 0.5f;
            float f2Z = (NorthwestServiceStaircaseBuilder.Flight2MinZ + NorthwestServiceStaircaseBuilder.Flight2MaxZ) * 0.5f;

            // Lower arrival & F1 ascent
            for (int s = 0; s <= 10; s++)
            {
                float t = s / 10f;
                float x = Mathf.Lerp(-11.42f, -14.22f, t);
                float expectedY = Mathf.Lerp(-50.00f, -48.20f, t);
                Assert.IsTrue(SampleFloorAndHeadroom(x, f1Z, expectedY, 0.14f, out float fy, out float hr),
                    $"Flight 1 floor/ceiling sample failed at X={x:F2}, Z={f1Z:F2} (actualY={fy:F2}).");
                Assert.GreaterOrEqual(hr, 2.10f, $"Insufficient Flight 1 headroom ({hr:F2}m) at X={x:F2}.");
                Assert.IsTrue(IsPlayerCapsuleClearAt(new Vector3(x, fy, f1Z)), $"Blocking collision on Flight 1 at X={x:F2}, Y={fy:F2}.");
            }

            // Intermediate landing turn
            Vector2[] midPts =
            {
                new Vector2(-15.05f, f1Z),
                new Vector2(-15.05f, 31.92f),
                new Vector2(-15.05f, f2Z)
            };
            foreach (Vector2 p in midPts)
            {
                Assert.IsTrue(SampleFloorAndHeadroom(p.x, p.y, -48.20f, 0.06f, out float fy, out float hr),
                    $"Intermediate landing sample failed at ({p.x:F2}, {p.y:F2}).");
                Assert.GreaterOrEqual(hr, 2.10f, $"Insufficient intermediate landing headroom ({hr:F2}m).");
                Assert.IsTrue(IsPlayerCapsuleClearAt(new Vector3(p.x, fy, p.y)), $"Blocking collision on intermediate landing at ({p.x:F2}, {p.y:F2}).");
            }

            // Flight 2 ascent
            for (int s = 0; s <= 10; s++)
            {
                float t = s / 10f;
                float x = Mathf.Lerp(-14.50f, -11.70f, t);
                float expectedY = Mathf.Lerp(-48.20f, -46.40f, t);
                Assert.IsTrue(SampleFloorAndHeadroom(x, f2Z, expectedY, 0.14f, out float fy, out float hr),
                    $"Flight 2 floor/ceiling sample failed at X={x:F2}, Z={f2Z:F2} (actualY={fy:F2}).");
                Assert.GreaterOrEqual(hr, 2.10f, $"Insufficient Flight 2 headroom ({hr:F2}m) at X={x:F2}.");
                Assert.IsTrue(IsPlayerCapsuleClearAt(new Vector3(x, fy, f2Z)), $"Blocking collision on Flight 2 at X={x:F2}, Y={fy:F2}.");
            }

            // 10. Upper landing walkable support, headroom & sealed 4-wall + ceiling enclosure + fall-protection parapets
            Vector2[] upperPts =
            {
                new Vector2(-10.70f, 30.94f),
                new Vector2(-10.70f, 29.70f),
                new Vector2(-10.70f, 32.90f)
            };
            foreach (Vector2 p in upperPts)
            {
                Assert.IsTrue(SampleFloorAndHeadroom(p.x, p.y, -46.40f, 0.05f, out float fy, out float hr),
                    $"Upper landing support missing at ({p.x:F2}, {p.y:F2}).");
                Assert.GreaterOrEqual(hr, 2.75f, $"Upper landing headroom ({hr:F2}m) below expected ~2.85m.");
                Assert.IsTrue(IsPlayerCapsuleClearAt(new Vector3(p.x, fy, p.y)), $"Blocking collision on upper landing at ({p.x:F2}, {p.y:F2}).");
            }

            // Sealed perimeter walls on Upper Landing (East, North, South, West shaft enclosure, plus West well parapets)
            float uEyeY = -46.40f + 1.20f;
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.70f, uEyeY, 31.30f), new Vector3(-9.00f, uEyeY, 31.30f), ~0, QueryTriggerInteraction.Ignore),
                "Upper landing East wall must be solid (sealed).");
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.70f, uEyeY, 32.80f), new Vector3(-10.70f, uEyeY, 34.60f), ~0, QueryTriggerInteraction.Ignore),
                "Upper landing North wall must be solid (sealed).");
            Assert.IsTrue(Physics.Linecast(new Vector3(-10.70f, uEyeY, 29.80f), new Vector3(-10.70f, uEyeY, 28.20f), ~0, QueryTriggerInteraction.Ignore),
                "Upper landing South wall must be solid (sealed).");
            Assert.IsTrue(Physics.Linecast(new Vector3(-15.00f, uEyeY, 31.30f), new Vector3(-16.60f, uEyeY, 31.30f), ~0, QueryTriggerInteraction.Ignore),
                "Upper stairwell West wall must be solid (sealed).");
            // Fall protection parapets along exposed stairwell drop-offs (waist height Y = -46.40 + 0.65)
            float uParapetY = -46.40f + 0.65f;
            Assert.IsTrue(Physics.Linecast(new Vector3(-11.00f, uParapetY, 32.90f), new Vector3(-12.50f, uParapetY, 32.90f), ~0, QueryTriggerInteraction.Ignore),
                "Upper landing North stairwell edge must have solid fall-protection parapet.");
            Assert.IsTrue(Physics.Linecast(new Vector3(-11.00f, uParapetY, 29.50f), new Vector3(-12.50f, uParapetY, 29.50f), ~0, QueryTriggerInteraction.Ignore),
                "Upper landing South stairwell edge must have solid fall-protection parapet.");

            // Real CharacterController.Move() full physical climb AND descent test matching FirstPersonPlayer CC specs!
            GameObject ccGo = new GameObject("__Test0715PlayerCC__");
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
                    const float speed = 3.2f;
                    for (int k = 0; k < maxSteps; k++)
                    {
                        Vector2 curXZ = new Vector2(ccGo.transform.position.x, ccGo.transform.position.z);
                        Vector2 diff = targetXZ - curXZ;
                        if (diff.magnitude < 0.12f) break;
                        Vector2 dir = diff.normalized;
                        Vector3 vel = new Vector3(dir.x * speed, -4.0f, dir.y * speed);
                        cc.Move(vel * dt);
                    }
                }

                // Ascend: Lower -> Flight 1 -> Intermediate Landing -> Flight 2 -> Upper Landing
                WalkTo(new Vector2(-15.05f, f1Z), 90);
                Assert.AreEqual(-48.20f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to climb Flight 1 to Intermediate Landing (got Y={ccGo.transform.position.y:F2}).");

                WalkTo(new Vector2(-15.05f, f2Z), 60);
                WalkTo(new Vector2(-10.70f, f2Z), 100);
                Assert.AreEqual(-46.40f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to climb Flight 2 to Upper Landing (got Y={ccGo.transform.position.y:F2}).");

                // Descend: Upper Landing -> Flight 2 -> Intermediate Landing -> Flight 1 -> Lower Floor
                WalkTo(new Vector2(-15.05f, f2Z), 100);
                Assert.AreEqual(-48.20f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to descend Flight 2 to Intermediate Landing (got Y={ccGo.transform.position.y:F2}).");

                WalkTo(new Vector2(-15.05f, f1Z), 60);
                WalkTo(new Vector2(-10.70f, f1Z), 100);
                Assert.AreEqual(-50.00f, ccGo.transform.position.y, 0.12f,
                    $"Player CC failed to descend Flight 1 back to Lower Floor (got Y={ccGo.transform.position.y:F2}).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ccGo);
            }
        }

        [Test]
        public void InvariantsIdempotencyAndSurfacePreservation_RemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // 12. Elevators (35 children), Room 111-B, Threshold of Trust, 18 lights (0 shadow), Room 110 Zone D ecology intact
            Transform elev = GameObject.Find("Hotel_Elevator_Core")?.transform;
            Assert.IsNotNull(elev);
            Assert.AreEqual(35, elev.childCount, "Elevator core must remain completely untouched.");

            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_Anomaly"));
            Assert.IsNotNull(GameObject.Find("Hotel_Room111B_GuestInterior"));
            Transform trustTf = GameObject.Find("Room111B_Threshold_TriggerVolume")?.transform;
            Assert.IsNotNull(trustTf);
            Assert.IsNotNull(trustTf.GetComponent<RemanenciaThresholdOfTrustTrigger>());
            Assert.IsNotNull(GameObject.Find("Eco_Room110_ChaseRiser_FungalSprout"));

            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count must remain exactly 18.");
            for (int i = 0; i < allLights.Length; i++)
            {
                Assert.AreEqual(LightShadows.None, allLights[i].shadows);
            }

            // 13. Entry-only door & Threshold of Trust authorization unchanged
            BackroomsExitDoorTrigger exitTrig = root.transform.Find("Backrooms_ExitDoor_ToStreet")?.GetComponent<BackroomsExitDoorTrigger>();
            Assert.IsNotNull(exitTrig);
            Assert.IsTrue(exitTrig.isEntryOnly);
            Assert.IsTrue(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(RemanenciaThresholdOfTrustTrigger.ExitSourceId));
            Assert.IsFalse(SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(SecretBackroomsTransitionController.LegacyInteriorDoorExitSource));

            // Save schema & progression IDs unchanged
            var save = new ProgressionSaveData();
            Assert.AreEqual(1, save.schemaVersion);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);

            // Surface-world BorderCorridorScene.unity remains 100% byte-for-byte identical
            Assert.AreEqual(ExpectedBorderCorridorSceneSha256, ComputeFileSha256("Assets/Scenes/BorderCorridorScene.unity"),
                "CRITICAL VIOLATION: BorderCorridorScene.unity must remain 100% byte-for-byte unchanged.");

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // 2. Repeated builder execution is strictly idempotent & rejects non-target scenes
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeColliders = root.GetComponentsInChildren<Collider>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            NorthwestServiceStaircaseBuilder.MigrateNorthwestServiceStaircaseInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running NorthwestServiceStaircaseBuilder must not duplicate or remove GameObjects.");
            Assert.AreEqual(beforeColliders, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => NorthwestServiceStaircaseBuilder.MigrateNorthwestServiceStaircaseInOpenScene(emptyScene));
        }
    }
}
