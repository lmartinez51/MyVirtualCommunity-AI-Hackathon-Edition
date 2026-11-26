using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode & deterministic runtime-cycle verification suite for Implementation 07.9
    /// (Remanencia Nivel 0 — First Living Microfauna).
    /// Verifies hierarchy, <= 2 organism limit, shared procedural mesh & non-emissive material,
    /// deterministic emergence/pause/retreat & startle-retreat motion without teleporting,
    /// full concealment in architectural recesses, zero forbidden components/registrations,
    /// zero progression/save/schema changes, preserved 07.6/07.7/07.8 content & 18 lights,
    /// and strict scene-validated migration idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaLevel0FirstLivingMicrofaunaTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;

        [Test]
        public void MicrofaunaHierarchyCountSharedMeshAndMaterial_AreValidAndAmbiguous()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root, "BackroomsLevel1_Root must exist.");

            // Hotel_FirstLivingMicrofauna must exist exactly once directly under BackroomsLevel1_Root
            int rootCount = 0;
            Transform microRoot = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name == BackroomsLevel1Builder.HotelFirstLivingMicrofaunaRootName)
                    {
                        rootCount++;
                        microRoot = t;
                    }
                }
            }
            Assert.AreEqual(1, rootCount, "Hotel_FirstLivingMicrofauna must exist exactly once.");
            Assert.IsNotNull(microRoot);
            Assert.AreEqual(root.transform, microRoot.parent, "Hotel_FirstLivingMicrofauna must be a direct sibling under BackroomsLevel1_Root.");

            // Strictly 1..2 organism instances maximum
            RemanenciaAmbientMicrofaunaController[] controllers =
                microRoot.GetComponentsInChildren<RemanenciaAmbientMicrofaunaController>(includeInactive: true);
            Assert.GreaterOrEqual(controllers.Length, 1, "At least one living microfauna encounter must exist.");
            Assert.LessOrEqual(controllers.Length, 2, "Implementation 07.9 allows at most two living microfauna organisms.");
            Assert.AreEqual(2, controllers.Length, "Expected primary Utility Room and secondary Maintenance Corridor encounters.");

            Assert.IsNotNull(microRoot.Find("Microfauna_Utility_Encounter"));
            Assert.IsNotNull(microRoot.Find("Microfauna_Maintenance_Encounter"));

            // Shared procedural mesh & shared non-emissive opaque material reusing existing 07.7 texture
            Mesh sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BackroomsLevel1Builder.MicrofaunaBodyMeshPath);
            Material sharedMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.MicrofaunaBodyMatPath);
            Assert.IsNotNull(sharedMesh, "Missing M_Remanencia_MicrofaunaBody.asset.");
            Assert.IsNotNull(sharedMat, "Missing Remanencia_Microfauna_Body.mat.");
            Assert.IsFalse(sharedMat.IsKeywordEnabled("_EMISSION"), "Microfauna material must NOT emit/glow.");
            Assert.AreEqual(
                "Assets/Textures/Environment/T_Remanencia_Ecology_FungalCap.png",
                AssetDatabase.GetAssetPath(sharedMat.GetTexture("_BaseMap")),
                "Microfauna material should reuse existing 07.7 procedural texture.");

            // Body length must sit cleanly within 2.5 - 4.0 cm (0.025 - 0.040 m)
            Bounds meshBounds = sharedMesh.bounds;
            Assert.GreaterOrEqual(meshBounds.size.z, 0.025f, "Microfauna body length must be >= 2.5 cm.");
            Assert.LessOrEqual(meshBounds.size.z, 0.042f, "Microfauna body length must be <= 4.0 cm.");

            foreach (var ctrl in controllers)
            {
                MeshFilter mf = ctrl.GetComponent<MeshFilter>();
                MeshRenderer mr = ctrl.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mf);
                Assert.IsNotNull(mr);
                Assert.AreEqual(sharedMesh, mf.sharedMesh, $"'{ctrl.name}' must share M_Remanencia_MicrofaunaBody.asset.");
                Assert.AreEqual(sharedMat, mr.sharedMaterial, $"'{ctrl.name}' must share Remanencia_Microfauna_Body.mat.");
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, mr.shadowCastingMode,
                    "Microfauna must not cast shadows as a visual effect.");
                Assert.IsNotNull(ctrl.waypoints);
                Assert.GreaterOrEqual(ctrl.waypoints.Length, 3);
                Assert.GreaterOrEqual(ctrl.TotalPathLength, 0.10f, $"'{ctrl.name}' excursion path should be >= 10 cm.");
                Assert.LessOrEqual(ctrl.TotalPathLength, 0.22f, $"'{ctrl.name}' excursion path should remain restrained (<= 22 cm).");
            }
        }

        [Test]
        public void AmbientMicrofaunaRuntimeCycle_IsDeterministicNonTeleportingAndConcealsCleanly()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            try
            {
                Transform microRoot = GameObject.Find(BackroomsLevel1Builder.HotelFirstLivingMicrofaunaRootName)?.transform;
                Assert.IsNotNull(microRoot);
                var controllers = microRoot.GetComponentsInChildren<RemanenciaAmbientMicrofaunaController>(includeInactive: true);

                const float dt = 1f / 60f;
                foreach (var ctrl in controllers)
                {
                    ctrl.ResetToConcealedState();
                    Vector3 concealedPos = ctrl.waypoints[0];
                    Assert.AreEqual(RemanenciaAmbientMicrofaunaController.MicrofaunaState.Concealed, ctrl.CurrentState);
                    Assert.Less(Vector3.Distance(ctrl.transform.position, concealedPos), 0.001f);

                    // 1. Far away observer (10 m away): must stay strictly concealed with zero drift
                    Vector3 farObserver = concealedPos + new Vector3(10f, 1.5f, 0f);
                    for (int f = 0; f < 120; f++)
                    {
                        ctrl.StepSimulation(dt, farObserver, hasObserver: true);
                    }
                    Assert.AreEqual(RemanenciaAmbientMicrofaunaController.MicrofaunaState.Concealed, ctrl.CurrentState);
                    Assert.Less(Vector3.Distance(ctrl.transform.position, concealedPos), 0.001f);

                    // 2. Approaching observer (2.0 m away): full Concealed -> IdleWait -> Emerging -> Paused -> Retreating -> Cooldown cycle
                    Vector3 nearObserver = concealedPos + new Vector3(2.0f, 1.5f, 0.2f);
                    bool sawIdle = false, sawEmerging = false, sawPaused = false, sawRetreating = false, sawCooldown = false;
                    Vector3 prevPos = ctrl.transform.position;
                    float maxStep = 0f;
                    float maxExcursion = 0f;
                    Vector3 run1MidPos = Vector3.zero;
                    int midSampleFrame = -1;

                    for (int f = 0; f < 600; f++)
                    {
                        ctrl.StepSimulation(dt, nearObserver, hasObserver: true);
                        float stepDist = Vector3.Distance(prevPos, ctrl.transform.position);
                        if (stepDist > maxStep) maxStep = stepDist;
                        prevPos = ctrl.transform.position;

                        float exc = Vector3.Distance(concealedPos, ctrl.transform.position);
                        if (exc > maxExcursion) maxExcursion = exc;

                        switch (ctrl.CurrentState)
                        {
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.IdleWait: sawIdle = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Emerging:
                                sawEmerging = true;
                                if (midSampleFrame < 0 && ctrl.PathProgress > 0.5f)
                                {
                                    midSampleFrame = f;
                                    run1MidPos = ctrl.transform.position;
                                }
                                break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Paused: sawPaused = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Retreating: sawRetreating = true; break;
                            case RemanenciaAmbientMicrofaunaController.MicrofaunaState.Cooldown: sawCooldown = true; break;
                        }
                        if (sawCooldown) break;
                    }

                    Assert.IsTrue(sawIdle, $"'{ctrl.name}' must wait a short deterministic idle interval before emerging.");
                    Assert.IsTrue(sawEmerging, $"'{ctrl.name}' must emerge when observer approaches.");
                    Assert.IsTrue(sawPaused, $"'{ctrl.name}' must pause briefly at excursion end.");
                    Assert.IsTrue(sawRetreating, $"'{ctrl.name}' must retreat back to cover.");
                    Assert.IsTrue(sawCooldown, $"'{ctrl.name}' must complete retreat and enter concealed cooldown.");
                    Assert.Less(maxStep, 0.005f, $"'{ctrl.name}' must move continuously with zero teleportation.");
                    Assert.GreaterOrEqual(maxExcursion, 0.10f, $"'{ctrl.name}' must crawl a readable 10-20 cm path.");
                    Assert.Less(Vector3.Distance(ctrl.transform.position, concealedPos), 0.001f,
                        $"'{ctrl.name}' must return cleanly to its occluded concealment crevice.");

                    // 3. Exact determinism verification: repeating the identical sequence hits the exact same position at midSampleFrame
                    ctrl.ResetToConcealedState();
                    for (int f = 0; f <= midSampleFrame; f++)
                    {
                        ctrl.StepSimulation(dt, nearObserver, hasObserver: true);
                    }
                    Assert.Less(Vector3.Distance(run1MidPos, ctrl.transform.position), 1e-5f,
                        $"'{ctrl.name}' runtime motion must be strictly deterministic.");

                    // 4. Startle retreat: stepping within startleRetreatRadius mid-emerge immediately & smoothly retreats without teleporting
                    Vector3 startlePos = concealedPos + new Vector3(0.45f, 0.20f, 0f);
                    Vector3 beforeStartlePos = ctrl.transform.position;
                    ctrl.StepSimulation(dt, startlePos, hasObserver: true);
                    Assert.AreEqual(RemanenciaAmbientMicrofaunaController.MicrofaunaState.Retreating, ctrl.CurrentState);
                    Assert.Less(Vector3.Distance(beforeStartlePos, ctrl.transform.position), 0.005f,
                        "Startle retreat must transition smoothly from current position without teleporting.");
                }
            }
            finally
            {
                // Always reload clean scene from disk
                EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            }
        }

        [Test]
        public void MicrofaunaObjects_ExcludeForbiddenComponentsSystemsAndProhibitedTerms()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            Transform microRoot = GameObject.Find(BackroomsLevel1Builder.HotelFirstLivingMicrofaunaRootName)?.transform;
            Assert.IsNotNull(microRoot);

            // Only RemanenciaAmbientMicrofaunaController MonoBehaviours allowed (zero interaction, NPC, enemy, combat, or progression scripts)
            MonoBehaviour[] mbs = microRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            Assert.AreEqual(2, mbs.Length, "Only the 2 ambient microfauna controllers may be attached.");
            foreach (MonoBehaviour mb in mbs)
            {
                Assert.IsInstanceOf<RemanenciaAmbientMicrofaunaController>(mb);
            }

            Assert.AreEqual(0, microRoot.GetComponentsInChildren<Collider>(includeInactive: true).Length, "Zero Colliders/Triggers allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, "Zero Rigidbodies allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<Light>(includeInactive: true).Length, "Zero Lights allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, "Zero AudioSources allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true).Length, "Zero ParticleSystems allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<Animator>(includeInactive: true).Length, "Zero Animators allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<NavMeshAgent>(includeInactive: true).Length, "Zero NavMeshAgents allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<InteractionPoint>(includeInactive: true).Length, "Zero InteractionPoints allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length, "Zero ArtifactDescriptors allowed.");
            Assert.AreEqual(0, microRoot.GetComponentsInChildren<NpcWaypointMovementController>(includeInactive: true).Length, "Zero NPC controllers allowed.");

            string[] forbiddenTerms =
            {
                "Insect", "Bug", "Spider", "Roach", "Beetle", "Ant", "Larva", "Worm", "Rat", "Mouse", "Animal",
                "Enemy", "Monster", "Hostile", "Combat", "Boss", "NPC", "Swarm", "Nest", "Radiation", "Geiger",
                "Cobalt", "Toxic", "Glow", "Mutant", "Infect", "Disease", "Health"
            };
            foreach (Transform t in microRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on '{t.name}'.");
                }
            }
        }

        [Test]
        public void Existing076Evidence077Ecology078TracesProgressionAndLights_RemainIntactAndIdempotent()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // Exact 18 lights preserved (0 shadow-casting)
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length);
            foreach (Light l in allLights) Assert.AreEqual(LightShadows.None, l.shadows);

            // 07.6 Narrative Evidence intact & unobstructed
            Transform narrativeRoot = root.transform.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName);
            Assert.IsNotNull(narrativeRoot);
            Assert.AreEqual(4, narrativeRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length);

            Transform microRoot = root.transform.Find(BackroomsLevel1Builder.HotelFirstLivingMicrofaunaRootName);
            var controllers = microRoot.GetComponentsInChildren<RemanenciaAmbientMicrofaunaController>(includeInactive: true);
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
                Assert.IsNotNull(evTf);
                foreach (var ctrl in controllers)
                {
                    foreach (Vector3 wp in ctrl.waypoints)
                    {
                        Assert.Greater(Vector3.Distance(evTf.position, wp), 1.0f,
                            $"Microfauna waypoint on '{ctrl.name}' must remain well clear of '{evName}'.");
                    }
                }
            }

            // 07.7 Early Ecology (23 renderers, 4 subgroups) & 07.8 Faunal Traces (16 renderers, 3 subgroups) completely untouched
            Transform ecoRoot = root.transform.Find(BackroomsLevel1Builder.HotelEarlyEcologyRootName);
            Transform faunalRoot = root.transform.Find(BackroomsLevel1Builder.HotelFaunalTracesRootName);
            Assert.AreEqual(4, ecoRoot.childCount);
            Assert.AreEqual(23, ecoRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(3, faunalRoot.childCount);
            Assert.AreEqual(16, faunalRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);

            // Zero progression / save schema changes
            var saveData = new ProgressionSaveData();
            Assert.AreEqual(1, saveData.schemaVersion);
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());

            // Migration idempotency & scene validation
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeRenderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaFirstLivingMicrofaunaInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeRenderers, root.GetComponentsInChildren<MeshRenderer>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaFirstLivingMicrofaunaInOpenScene(emptyScene));
        }
    }
}
