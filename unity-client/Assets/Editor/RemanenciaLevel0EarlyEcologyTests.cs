using System;
using System.Collections.Generic;
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
    /// Automated EditMode verification suite for Implementation 07.7
    /// (Remanencia Nivel 0 — First Anomalous Ecology: Fungal & Vegetal Manifestations).
    /// Verifies exact single root & subgroups, genuine procedural mesh geometry & matte non-emissive
    /// materials in all approved zones, playable footprint bounds, zero colliders/lights/scripts/particles,
    /// zero prohibited organism categories or furniture, unobstructed access to all 4 narrative evidence
    /// documents, preserved progression IDs, preserved North Wing M1 & elevators, exact 18 scene lights,
    /// and migration idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaLevel0EarlyEcologyTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;

        [Test]
        public void EcologyRootSubgroupsMeshesAndZonePlacement_AreValidWithinPlayableFootprint()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            // Hotel_Early_Ecology must exist exactly once in the entire scene
            int ecoCount = 0;
            Transform ecoRoot = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == BackroomsLevel1Builder.HotelEarlyEcologyRootName)
                    {
                        ecoCount++;
                        ecoRoot = t;
                    }
                }
            }
            Assert.AreEqual(1, ecoCount, "Hotel_Early_Ecology must exist exactly once.");
            Assert.IsNotNull(ecoRoot);

            string[] requiredGroups =
            {
                "Ecology_Courtyard_Vegetation",
                "Ecology_Maintenance_Mycelium",
                "Ecology_Utility_FungalGrowth",
                "Ecology_Optional_GuestRoomTrace"
            };
            foreach (string grp in requiredGroups)
            {
                int found = 0;
                for (int i = 0; i < ecoRoot.childCount; i++)
                {
                    if (ecoRoot.GetChild(i).name == grp) found++;
                }
                Assert.AreEqual(1, found, $"Expected exactly one ecological subgroup '{grp}'.");
            }

            // Validate zone-specific bounding envelopes & non-empty procedural meshes + matte materials
            AssertZoneRenderersValid(ecoRoot.Find("Ecology_Courtyard_Vegetation"), new Vector2(-5.2f, 17.8f), new Vector2(5.2f, 26.2f), minRenderers: 6);
            AssertZoneRenderersValid(ecoRoot.Find("Ecology_Maintenance_Mycelium"), new Vector2(-12.2f, 20.5f), new Vector2(-7.8f, 24.0f), minRenderers: 5);
            AssertZoneRenderersValid(ecoRoot.Find("Ecology_Utility_FungalGrowth"), new Vector2(-16.1f, 18.0f), new Vector2(-12.0f, 23.5f), minRenderers: 6);
            AssertZoneRenderersValid(ecoRoot.Find("Ecology_Optional_GuestRoomTrace"), new Vector2(-9.8f, 31.5f), new Vector2(-7.6f, 34.1f), minRenderers: 2);

            // All 4 ecology materials must exist, have textures, and MUST NOT emit radioactive/neon glow
            string[] ecoMatPaths =
            {
                BackroomsLevel1Builder.EcologyMyceliumMatPath,
                BackroomsLevel1Builder.EcologyFungalCapMatPath,
                BackroomsLevel1Builder.EcologyVegetalLeafMatPath,
                BackroomsLevel1Builder.EcologyVegetalRootMatPath
            };
            foreach (string mp in ecoMatPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                Assert.IsNotNull(mat, $"Missing ecology material '{mp}'.");
                Assert.IsNotNull(mat.GetTexture("_BaseMap"), $"Ecology material '{mp}' must reference a procedural texture.");
                Assert.IsFalse(mat.IsKeywordEnabled("_EMISSION"), $"Ecology material '{mp}' must not glow/emit.");
            }
        }

        [Test]
        public void EcologyObjects_ContainZeroCollidersScriptsLightsAudioOrAnimationsAndZeroForbiddenTerms()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            Transform ecoRoot = GameObject.Find(BackroomsLevel1Builder.HotelEarlyEcologyRootName)?.transform;
            Assert.IsNotNull(ecoRoot);

            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<Collider>(includeInactive: true).Length, "Ecology must not add colliders.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, "Ecology must not add rigidbodies.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length, "Ecology must not add MonoBehaviours.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<Light>(includeInactive: true).Length, "Ecology must not add Lights.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, "Ecology must not add AudioSources.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true).Length, "Ecology must not add ParticleSystems.");
            Assert.AreEqual(0, ecoRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length, "Ecology must remain static (zero Animators).");

            string[] forbiddenTerms =
            {
                "Enemy", "Monster", "Creature", "NPC", "Insect", "Bug", "Spider", "Animal", "Rat", "Bird",
                "Human", "Corpse", "Mutant", "Spore", "Radiation", "Geiger", "Cobalt", "Toxic", "Glow",
                "Bed", "Desk", "Chair", "Table", "Cabinet", "Lamp", "Sofa"
            };
            foreach (Transform t in ecoRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on ecology object '{t.name}'.");
                }
            }
        }

        [Test]
        public void ExistingEvidenceDiscoveriesArchitectureElevatorsAndLightBudget_RemainCompletelyIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // Exact 18 scene lights preserved (0 shadow-casting)
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count must remain exactly 18.");
            foreach (Light l in allLights)
            {
                Assert.AreEqual(LightShadows.None, l.shadows);
            }

            // All 4 narrative discoveries remain untouched, unobstructed, and unchanged in ID
            Transform narrativeRoot = root.transform.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName);
            Assert.IsNotNull(narrativeRoot);
            SecretContainmentArtifactDescriptor[] descs = narrativeRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true);
            Assert.AreEqual(4, descs.Length, "No new interactable narrative discoveries may be introduced in 07.7.");

            (string goName, string clueId, Vector2 standXZ)[] evidenceChecks =
            {
                ("Hotel_Evidence_UnfinishedProjectPlan",  ProgressionIds.ClueRemanenciaHotelPlan01,        new Vector2(  8.85f, 10.60f)),
                ("Hotel_Evidence_RoomRegistrationRecord", ProgressionIds.ClueRemanenciaHotelRegister02,    new Vector2( -3.85f, 27.40f)),
                ("Hotel_Evidence_MaintenanceRecord",      ProgressionIds.ClueRemanenciaHotelMaintenance03, new Vector2(-10.10f, 22.45f)),
                ("Hotel_Evidence_Cobalt60RumorNote",      ProgressionIds.ClueRemanenciaHotelCobaltRumor04, new Vector2(-12.75f, 22.00f))
            };

            Transform ecoRoot = root.transform.Find(BackroomsLevel1Builder.HotelEarlyEcologyRootName);
            MeshRenderer[] ecoRenderers = ecoRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var ev in evidenceChecks)
            {
                Transform evTf = narrativeRoot.Find(ev.goName);
                Assert.IsNotNull(evTf);
                Assert.AreEqual(ev.clueId, evTf.GetComponent<SecretContainmentArtifactDescriptor>().artifactId);

                // Ensure NO ecological mesh overlaps/obscures the evidence document's visual bounds
                Bounds evBounds = evTf.GetComponent<Renderer>().bounds;
                foreach (MeshRenderer ecoMr in ecoRenderers)
                {
                    Assert.IsFalse(evBounds.Intersects(ecoMr.bounds),
                        $"Ecology object '{ecoMr.gameObject.name}' overlaps narrative evidence '{ev.goName}'.");
                }
            }

            // Existing North Wing M1, Elevators, Safe, Portal & RunVerificationChecks intact
            Assert.IsNotNull(root.transform.Find("Backrooms_Architecture/" + BackroomsLevel1Builder.NorthWingM1RootName));
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsNotNull(root.transform.Find("Backrooms_DispersedArtifacts/SecretRoom_HeavyVaultSafe"));
            Assert.IsNotNull(root.transform.Find("Backrooms_DispersedArtifacts/Portal_DimensionalMirror_Root"));
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());
        }

        [Test]
        public void EarlyEcologyMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaEarlyEcologyInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running early ecology migration must not duplicate or remove any GameObjects.");
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaEarlyEcologyInOpenScene(emptyScene));
        }

        private static void AssertZoneRenderersValid(Transform zone, Vector2 minXZ, Vector2 maxXZ, int minRenderers)
        {
            Assert.IsNotNull(zone);
            MeshRenderer[] renderers = zone.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            Assert.GreaterOrEqual(renderers.Length, minRenderers, $"Expected at least {minRenderers} organic meshes inside '{zone.name}'.");

            foreach (MeshRenderer mr in renderers)
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf, $"Ecology renderer '{mr.gameObject.name}' missing MeshFilter.");
                Assert.IsNotNull(mf.sharedMesh, $"Ecology renderer '{mr.gameObject.name}' missing sharedMesh.");
                Assert.Greater(mf.sharedMesh.vertexCount, 12, $"Ecology mesh on '{mr.gameObject.name}' should be genuine organic geometry.");
                Assert.IsNotNull(mr.sharedMaterial, $"Ecology renderer '{mr.gameObject.name}' missing sharedMaterial.");

                Vector3 p = mr.transform.position;
                Assert.IsTrue(
                    p.x >= minXZ.x && p.x <= maxXZ.x && p.z >= minXZ.y && p.z <= maxXZ.y,
                    $"Ecology object '{mr.gameObject.name}' at ({p.x:F2}, {p.z:F2}) is outside zone '{zone.name}' bounds.");
                Assert.IsTrue(
                    p.y >= BaseY - 0.05f && p.y <= BackroomsLevel1Builder.CeilingY,
                    $"Ecology object '{mr.gameObject.name}' Y={p.y:F2} is outside playable ground-floor height.");
            }
        }
    }
}
