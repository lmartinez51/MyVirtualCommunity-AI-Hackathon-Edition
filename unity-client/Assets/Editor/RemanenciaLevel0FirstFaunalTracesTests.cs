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
    /// Automated EditMode verification suite for Implementation 07.8
    /// (Remanencia Nivel 0 — First Faunal Traces: Filaments, Shed Cuticle Fragments & Micro-Nesting Residue).
    /// Verifies sibling root Hotel_Faunal_Traces under BackroomsLevel1_Root, all 3 approved zone groups
    /// and 16 localized static traces (with Zone D Room 110 unchanged), strictly 3 reusable procedural meshes
    /// & 3 non-emissive materials reusing existing textures, zero forbidden components/terms, unobstructed
    /// 07.6 narrative evidence and intact 07.7 ecology, zero progression/schema changes, exact 18 scene lights,
    /// and strict scene-validated migration idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaLevel0FirstFaunalTracesTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;

        [Test]
        public void FaunalTracesRootZoneGroupsObjectsMeshesAndMaterials_AreValidAndRestrained()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root, "BackroomsLevel1_Root must exist.");

            // Hotel_Faunal_Traces must exist exactly once in the entire scene and be a direct sibling child of BackroomsLevel1_Root
            int faunalCount = 0;
            Transform faunalRoot = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == BackroomsLevel1Builder.HotelFaunalTracesRootName)
                    {
                        faunalCount++;
                        faunalRoot = t;
                    }
                }
            }
            Assert.AreEqual(1, faunalCount, "Hotel_Faunal_Traces must exist exactly once.");
            Assert.IsNotNull(faunalRoot);
            Assert.AreEqual(root.transform, faunalRoot.parent, "Hotel_Faunal_Traces must be a direct sibling under BackroomsLevel1_Root.");

            // Strictly 3 approved zone subgroups (Zone D Room 110 intentionally omitted)
            Assert.AreEqual(3, faunalRoot.childCount, "Expected exactly 3 zone subgroups under Hotel_Faunal_Traces.");
            Transform courtyard = faunalRoot.Find("Faunal_Courtyard_Traces");
            Transform maint = faunalRoot.Find("Faunal_Maintenance_Traces");
            Transform utility = faunalRoot.Find("Faunal_Utility_Traces");
            Assert.IsNotNull(courtyard, "Missing Faunal_Courtyard_Traces.");
            Assert.IsNotNull(maint, "Missing Faunal_Maintenance_Traces.");
            Assert.IsNotNull(utility, "Missing Faunal_Utility_Traces.");

            // Verify exact expected object counts & zone bounding envelopes
            AssertZoneTraces(courtyard, expectedCount: 4, new Vector2(-3.2f, 19.5f), new Vector2(5.2f, 26.2f));
            AssertZoneTraces(maint,     expectedCount: 5, new Vector2(-9.5f, 21.2f), new Vector2(-8.5f, 23.2f));
            AssertZoneTraces(utility,   expectedCount: 7, new Vector2(-16.0f, 18.0f), new Vector2(-15.0f, 19.6f));

            // Strictly 3 reusable mesh assets and 3 matte non-emissive materials reusing existing textures
            Mesh filMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BackroomsLevel1Builder.FaunalFilamentMeshPath);
            Mesh cutMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BackroomsLevel1Builder.FaunalCuticleMeshPath);
            Mesh resMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BackroomsLevel1Builder.FaunalResidueMeshPath);
            Assert.IsNotNull(filMesh, "Missing M_Remanencia_FaunalFilament.asset.");
            Assert.IsNotNull(cutMesh, "Missing M_Remanencia_FaunalCuticleFragment.asset.");
            Assert.IsNotNull(resMesh, "Missing M_Remanencia_FaunalMicroResidue.asset.");

            var allowedMeshes = new HashSet<Mesh> { filMesh, cutMesh, resMesh };
            Material filMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.FaunalFilamentMatPath);
            Material cutMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.FaunalCuticleMatPath);
            Material resMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.FaunalResidueMatPath);
            Assert.IsNotNull(filMat, "Missing Remanencia_Faunal_Filament.mat.");
            Assert.IsNotNull(cutMat, "Missing Remanencia_Faunal_Cuticle.mat.");
            Assert.IsNotNull(resMat, "Missing Remanencia_Faunal_Residue.mat.");

            Material[] faunalMats = { filMat, cutMat, resMat };
            var allowedMats = new HashSet<Material>(faunalMats);
            foreach (Material mat in faunalMats)
            {
                Assert.IsFalse(mat.IsKeywordEnabled("_EMISSION"), $"Faunal material '{mat.name}' must NOT emit/glow.");
                Texture baseMap = mat.GetTexture("_BaseMap");
                Assert.IsNotNull(baseMap, $"Faunal material '{mat.name}' must bind an existing procedural texture.");
                string texPath = AssetDatabase.GetAssetPath(baseMap);
                Assert.IsTrue(
                    texPath == "Assets/Textures/Environment/T_Remanencia_Ecology_FungalCap.png" ||
                    texPath == "Assets/Textures/Environment/T_Remanencia_Ecology_VegetalRoot.png",
                    $"Faunal material '{mat.name}' should reuse existing 07.7 textures rather than introducing redundant textures (found '{texPath}').");
            }

            MeshRenderer[] allTraces = faunalRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            Assert.AreEqual(16, allTraces.Length, "Expected exactly 16 restrained faunal trace renderers (4 + 5 + 7).");
            var usedMeshes = new HashSet<Mesh>();
            var usedMats = new HashSet<Material>();
            foreach (MeshRenderer mr in allTraces)
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf);
                Assert.IsTrue(allowedMeshes.Contains(mf.sharedMesh), $"Unexpected mesh on '{mr.gameObject.name}'.");
                Assert.IsTrue(allowedMats.Contains(mr.sharedMaterial), $"Unexpected material on '{mr.gameObject.name}'.");
                Assert.Greater(mf.sharedMesh.vertexCount, 24, $"Mesh on '{mr.gameObject.name}' must be genuine sculpted procedural geometry.");
                usedMeshes.Add(mf.sharedMesh);
                usedMats.Add(mr.sharedMaterial);
            }
            Assert.AreEqual(3, usedMeshes.Count, "All 3 reusable faunal trace mesh types should be represented.");
            Assert.AreEqual(3, usedMats.Count, "All 3 faunal trace materials should be represented.");
        }

        [Test]
        public void FaunalTraces_ContainZeroForbiddenComponentsAndZeroForbiddenTerms()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            Transform faunalRoot = GameObject.Find(BackroomsLevel1Builder.HotelFaunalTracesRootName)?.transform;
            Assert.IsNotNull(faunalRoot);

            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<Collider>(includeInactive: true).Length, "Faunal traces must not add Colliders.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, "Faunal traces must not add Rigidbodies.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length, "Faunal traces must not add MonoBehaviours.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<Light>(includeInactive: true).Length, "Faunal traces must not add Lights.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, "Faunal traces must not add AudioSources.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true).Length, "Faunal traces must not add ParticleSystems.");
            Assert.AreEqual(0, faunalRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length, "Faunal traces must remain static (zero Animators).");

            string[] forbiddenTerms =
            {
                "Insect", "Bug", "Spider", "Cobweb", "Web", "Animal", "Rat", "Bird", "Creature", "Monster",
                "Enemy", "NPC", "Human", "Corpse", "Mutant", "Larva", "Egg", "Nest", "Spawn", "Hole",
                "Radiation", "Geiger", "Cobalt", "Toxic", "Glow", "Infect", "Disease",
                "Bed", "Desk", "Chair", "Table", "Cabinet", "Lamp", "Sofa"
            };
            foreach (Transform t in faunalRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on faunal trace object '{t.name}'.");
                }
            }
        }

        [Test]
        public void ExistingNarrativeEvidenceEarlyEcologyProgressionAndLighting_RemainIntactAndUnobstructed()
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

            // 07.6 Narrative Evidence: all 4 discoveries intact, unobstructed, zero overlap with 07.8 faunal traces
            Transform narrativeRoot = root.transform.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName);
            Assert.IsNotNull(narrativeRoot);
            SecretContainmentArtifactDescriptor[] narrativeDescs =
                narrativeRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true);
            Assert.AreEqual(4, narrativeDescs.Length, "No new interactable discoveries may be introduced in 07.8.");

            Transform faunalRoot = root.transform.Find(BackroomsLevel1Builder.HotelFaunalTracesRootName);
            MeshRenderer[] faunalRenderers = faunalRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

            string[] evidenceNames =
            {
                "Hotel_Evidence_UnfinishedProjectPlan",
                "Hotel_Evidence_RoomRegistrationRecord",
                "Hotel_Evidence_MaintenanceRecord",
                "Hotel_Evidence_Cobalt60RumorNote"
            };
            foreach (string evName in evidenceNames)
            {
                Transform evTf = narrativeRoot.Find(evName);
                Assert.IsNotNull(evTf, $"Missing 07.6 evidence '{evName}'.");
                Bounds evBounds = evTf.GetComponent<Renderer>().bounds;
                foreach (MeshRenderer fMr in faunalRenderers)
                {
                    Assert.IsFalse(evBounds.Intersects(fMr.bounds),
                        $"Faunal trace '{fMr.gameObject.name}' overlaps narrative evidence '{evName}'.");
                    Assert.Greater(Vector3.Distance(evTf.position, fMr.transform.position), 0.55f,
                        $"Faunal trace '{fMr.gameObject.name}' is too close to narrative evidence '{evName}'.");
                }
            }

            // 07.7 Early Ecology: independent sibling root completely untouched (exact 4 subgroups & 23 renderers)
            Transform ecoRoot = root.transform.Find(BackroomsLevel1Builder.HotelEarlyEcologyRootName);
            Assert.IsNotNull(ecoRoot, "Hotel_Early_Ecology must remain intact.");
            Assert.AreEqual(4, ecoRoot.childCount, "Hotel_Early_Ecology subgroup count must remain 4.");
            Assert.AreEqual(23, ecoRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length,
                "Hotel_Early_Ecology renderer count must remain untouched at 23.");
            Assert.AreEqual(2, ecoRoot.Find("Ecology_Optional_GuestRoomTrace").childCount,
                "Zone D (Room 110) must remain unchanged.");

            // Zero progression ID or save schema changes
            var saveData = new ProgressionSaveData();
            Assert.AreEqual(1, saveData.schemaVersion, "Save schema version must remain 1.");
            Assert.AreEqual("clue:remanencia-hotel-plan-01", ProgressionIds.ClueRemanenciaHotelPlan01);
            Assert.AreEqual("clue:remanencia-hotel-register-02", ProgressionIds.ClueRemanenciaHotelRegister02);
            Assert.AreEqual("clue:remanencia-hotel-maintenance-03", ProgressionIds.ClueRemanenciaHotelMaintenance03);
            Assert.AreEqual("clue:remanencia-hotel-cobalt-rumor-04", ProgressionIds.ClueRemanenciaHotelCobaltRumor04);

            // Architecture, elevators, and builder verification gate intact
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());
        }

        [Test]
        public void FirstFaunalTracesMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaFirstFaunalTracesInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running 07.8 migration must not duplicate or remove any GameObjects.");
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaFirstFaunalTracesInOpenScene(emptyScene));
        }

        private static void AssertZoneTraces(Transform zone, int expectedCount, Vector2 minXZ, Vector2 maxXZ)
        {
            Assert.IsNotNull(zone);
            MeshRenderer[] renderers = zone.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            Assert.AreEqual(expectedCount, renderers.Length, $"Expected exactly {expectedCount} traces inside '{zone.name}'.");
            foreach (MeshRenderer mr in renderers)
            {
                Vector3 p = mr.transform.position;
                Assert.IsTrue(
                    p.x >= minXZ.x && p.x <= maxXZ.x && p.z >= minXZ.y && p.z <= maxXZ.y,
                    $"Faunal trace '{mr.gameObject.name}' at ({p.x:F2}, {p.z:F2}) is outside zone '{zone.name}' bounds.");
                Assert.IsTrue(
                    p.y >= BaseY - 0.05f && p.y <= BackroomsLevel1Builder.CeilingY,
                    $"Faunal trace '{mr.gameObject.name}' Y={p.y:F2} is outside playable ground-floor height.");
            }
        }
    }
}
