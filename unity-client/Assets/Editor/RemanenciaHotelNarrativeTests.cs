using System;
using System.Collections.Generic;
using System.IO;
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
    /// Automated EditMode verification suite for Implementation 07.6
    /// (Remanencia Nivel 0 — Hotel de las Palomas: First Environmental Narrative Layer).
    /// Verifies the 4 new inspectable bilingual discoveries, raycast accessibility & upright UV orientation,
    /// non-sequential Discovery Journal & offline JSON save/load persistence, Remanencia world-context
    /// notification scoping, subtle non-colliding matte anomalies, preservation of legacy progression IDs,
    /// puzzles, elevators, North Wing M1, zero new furniture/lights/enemies/survival systems, and idempotency.
    /// </summary>
    [TestFixture]
    public class RemanenciaHotelNarrativeTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;
        private string tempRootDir;

        private static readonly (string GoName, string ClueId, Vector2 PlayerStandXZ, Vector3 ExpectedFacing)[] ExpectedDiscoveries =
        {
            ("Hotel_Evidence_UnfinishedProjectPlan",  ProgressionIds.ClueRemanenciaHotelPlan01,        new Vector2(  8.85f, 10.60f), Vector3.back),
            ("Hotel_Evidence_RoomRegistrationRecord", ProgressionIds.ClueRemanenciaHotelRegister02,    new Vector2( -3.85f, 27.40f), Vector3.back),
            ("Hotel_Evidence_MaintenanceRecord",      ProgressionIds.ClueRemanenciaHotelMaintenance03, new Vector2(-10.10f, 22.45f), Vector3.left),
            ("Hotel_Evidence_Cobalt60RumorNote",      ProgressionIds.ClueRemanenciaHotelCobaltRumor04, new Vector2(-12.75f, 22.00f), Vector3.back)
        };

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_HotelNarrative_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRootDir);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerProgressionService.ResetSharedForTesting();
            try
            {
                if (!string.IsNullOrEmpty(tempRootDir) && Directory.Exists(tempRootDir))
                {
                    Directory.Delete(tempRootDir, recursive: true);
                }
            }
            catch
            {
                // Ignore transient Windows file lock during cleanup
            }
        }

        [Test]
        public void FourNarrativeDiscoveries_ExistOnceWithValidDescriptorsBilingualCopyAndInspectionReachability()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform narrativeRoot = GameObject.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName)?.transform;
            Assert.IsNotNull(narrativeRoot, "Hotel_Narrative_Evidence root must exist.");

            SecretContainmentArtifactDescriptor[] narrativeDescs =
                narrativeRoot.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true);
            Assert.AreEqual(4, narrativeDescs.Length, "Expected exactly 4 new narrative evidence descriptors under Hotel_Narrative_Evidence.");

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in ExpectedDiscoveries)
            {
                Transform tf = narrativeRoot.Find(item.GoName);
                Assert.IsNotNull(tf, $"Missing evidence GameObject '{item.GoName}'.");

                SecretContainmentArtifactDescriptor desc = tf.GetComponent<SecretContainmentArtifactDescriptor>();
                Assert.IsNotNull(desc, $"Evidence '{item.GoName}' must attach SecretContainmentArtifactDescriptor.");
                Assert.AreEqual(item.ClueId, desc.artifactId);
                Assert.AreEqual(ArtifactInspectionScope.SecretRoomOrBackrooms, desc.inspectionScope);
                Assert.IsTrue(seenIds.Add(desc.artifactId), $"Duplicate discovery ID '{desc.artifactId}'.");
                Assert.IsTrue(ProgressionIds.IsRemanenciaHotelNarrativeClue(desc.artifactId));

                // Complete bilingual copy on descriptor & DiscoveryJournalCatalog
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.titleEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.titleEn));
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.loreEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.loreEn));
                Assert.IsTrue(DiscoveryJournalCatalog.TryGetEvidence(item.ClueId, out var meta),
                    $"DiscoveryJournalCatalog must resolve '{item.ClueId}'.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.TitleEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.TitleEn));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.SummaryEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.SummaryEn));

                // Inside approved playable hotel footprint (X in [-20,+20], Z in [-12,+34])
                Vector3 p = tf.position;
                Assert.IsTrue(p.x >= -20f && p.x <= 20f && p.z >= -12f && p.z <= 34f,
                    $"Evidence '{item.GoName}' at ({p.x:F2}, {p.z:F2}) is outside playable hotel footprint.");

                // Trigger collider properly configured & reachable by FirstPersonPlayer's 2.75m center raycast
                BoxCollider col = tf.GetComponent<BoxCollider>();
                Assert.IsNotNull(col);
                Assert.IsTrue(col.enabled);
                Assert.IsTrue(col.isTrigger, $"Evidence '{item.GoName}' collider must be an inspection trigger.");

                Vector3 eye = new Vector3(item.PlayerStandXZ.x, BaseY + 1.50f, item.PlayerStandXZ.y);
                Vector3 dir = (p - eye).normalized;
                float dist = Vector3.Distance(eye, p);
                Assert.Less(dist, 2.50f, $"Evidence '{item.GoName}' should be comfortably within 2.75m inspection distance.");

                float maxSolidDist = 2.75f;
                if (Physics.Raycast(eye, dir, out RaycastHit solidHit, 2.75f, ~0, QueryTriggerInteraction.Ignore))
                {
                    maxSolidDist = solidHit.distance + 0.05f;
                }
                RaycastHit[] hits = Physics.RaycastAll(eye, dir, 2.75f, ~0, QueryTriggerInteraction.Collide);
                bool rayReachedTarget = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].distance <= maxSolidDist && hits[i].collider == col)
                    {
                        rayReachedTarget = true;
                        break;
                    }
                }
                Assert.IsTrue(rayReachedTarget, $"Player eye raycast from ({item.PlayerStandXZ.x:F2}, {item.PlayerStandXZ.y:F2}) failed to reach '{item.GoName}'.");

                // Upright, non-mirrored rendered front face (local +Z oriented outward toward player)
                Assert.Greater(Vector3.Dot(tf.forward, item.ExpectedFacing), 0.95f, $"Evidence '{item.GoName}' +Z front face must face player.");
                Assert.Greater(Vector3.Dot(tf.up, Vector3.up), 0.95f, $"Evidence '{item.GoName}' must be upright.");
            }
        }

        [Test]
        public void AllFourDiscoveries_RecordNonSequentiallyPersistAcrossRestartAndDoNotGatePuzzles()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var session1 = new PlayerProgressionService(storage);
            session1.StartNewGame();

            var notifications = new List<ProgressionDiscoveryNotification>();
            session1.OnNewDiscoveryRecorded += n => notifications.Add(n);

            // Record in reverse/non-sequential order (4 -> 2 -> 1 -> 3)
            Assert.IsTrue(session1.RecordEvidenceInspected(ProgressionIds.ClueRemanenciaHotelCobaltRumor04));
            Assert.IsTrue(session1.RecordEvidenceInspected(ProgressionIds.ClueRemanenciaHotelRegister02));
            Assert.IsTrue(session1.RecordEvidenceInspected(ProgressionIds.ClueRemanenciaHotelPlan01));
            Assert.IsTrue(session1.RecordEvidenceInspected(ProgressionIds.ClueRemanenciaHotelMaintenance03));

            // Re-recording any clue must not duplicate or re-notify
            Assert.IsFalse(session1.RecordEvidenceInspected(ProgressionIds.ClueRemanenciaHotelCobaltRumor04));
            Assert.AreEqual(4, notifications.Count, "Expected exactly 4 notifications (1 per unique discovery).");

            // Must NOT unlock codes, solve puzzles, acquire artifacts, or advance checkpoints
            Assert.AreEqual(0, session1.CurrentData.permanent.learnedCodeIds.Count);
            Assert.AreEqual(0, session1.CurrentData.permanent.solvedPuzzleIds.Count);
            Assert.AreEqual(0, session1.CurrentData.permanent.acquiredArtifactIds.Count);
            Assert.AreEqual(ProgressionIds.CheckpointArrival, session1.CurrentData.latestCheckpointId);
            Assert.AreEqual(1, session1.CurrentData.schemaVersion, "Save schema version must remain 1.");

            // Simulate scene exit & full application restart
            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);

            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(session2, isEs: true);
            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(session2, isEs: false);
            Assert.AreEqual(4, es.EvidenceEntries.Count);
            Assert.AreEqual(4, en.EvidenceEntries.Count);

            foreach (var entry in es.EvidenceEntries)
            {
                StringAssert.DoesNotContain("clue:", entry.Title);
                Assert.AreNotEqual("Evidencia registrada", entry.Title);
            }
            foreach (var entry in en.EvidenceEntries)
            {
                StringAssert.DoesNotContain("clue:", entry.Title);
                Assert.AreNotEqual("Recorded Evidence", entry.Title);
            }
        }

        [Test]
        public void DiscoveryNotifications_AreWorldContextScopedToRemanenciaAndPreserveJournalOnSurface()
        {
            GameObject go = new GameObject("TempTransitionForNarrativeWorldContextTest");
            try
            {
                var transition = go.AddComponent<SecretBackroomsTransitionController>();

                foreach (var item in ExpectedDiscoveries)
                {
                    var notif = new ProgressionDiscoveryNotification(DiscoveryNotificationKind.Evidence, item.ClueId);

                    // Surface world (Avenida Juárez) -> Remanencia hotel narrative toast suppressed
                    transition.SetWorldTransitionStateForTest(inBackrooms: false, transitioning: false);
                    Assert.IsFalse(FirstPersonPlayer.ShouldShowDiscoveryNotificationToast(notif, transition),
                        $"Notification for '{item.ClueId}' must not appear on Avenida Benito Juárez.");

                    // Mid-transition -> suppressed
                    transition.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: true);
                    Assert.IsFalse(FirstPersonPlayer.ShouldShowDiscoveryNotificationToast(notif, transition));

                    // Active inside Remanencia Nivel 0 -> allowed
                    transition.SetWorldTransitionStateForTest(inBackrooms: true, transitioning: false);
                    Assert.IsTrue(FirstPersonPlayer.ShouldShowDiscoveryNotificationToast(notif, transition));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SubtleAnomaliesExistingSystemsLegacyIdsAndProhibitions_RemainCleanlyEnforced()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform narrativeRoot = root.transform.Find(BackroomsLevel1Builder.HotelNarrativeEvidenceRootName);
            Transform anomalies = narrativeRoot?.Find("Hotel_Subtle_Anomalies");
            Assert.IsNotNull(anomalies, "Hotel_Subtle_Anomalies subgroup must exist.");
            Assert.GreaterOrEqual(anomalies.childCount, 5, "Expected subtle architectural anomaly traces.");

            // Subtle anomalies must be strictly non-colliding, non-glowing, zero Lights, zero MonoBehaviours
            Assert.AreEqual(0, anomalies.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            Assert.AreEqual(0, anomalies.GetComponentsInChildren<Light>(includeInactive: true).Length);
            Assert.AreEqual(0, anomalies.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length);

            Material mineralMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.AnomalyMineralJointMatPath);
            Material stainMat = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.AnomalySurfaceStainMatPath);
            Assert.IsNotNull(mineralMat);
            Assert.IsNotNull(stainMat);
            Assert.IsFalse(mineralMat.IsKeywordEnabled("_EMISSION"), "Anomaly mineral material must NOT glow.");
            Assert.IsFalse(stainMat.IsKeywordEnabled("_EMISSION"), "Anomaly stain material must NOT glow.");

            // Zero added Lights across entire scene (18 exact, 0 shadows)
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length);

            // Zero furniture, enemies, NPCs, fungal, or survival/radiation-meter objects inside Hotel_Narrative_Evidence
            string[] forbiddenTerms = { "Bed", "Desk", "Chair", "Table", "Cabinet", "Lamp", "Enemy", "Monster", "Creature", "NPC", "Geiger", "RadiationMeter", "Mutant", "Fungus", "Fungal" };
            foreach (Transform t in narrativeRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on '{t.name}'.");
                }
            }

            // All 7 legacy Backrooms-containing persistent progression IDs preserved verbatim
            Assert.AreEqual("safe:backrooms-level1-omega-vault", ProgressionIds.SafeBackroomsOmegaVault);
            Assert.AreEqual("puzzle:safe:backrooms-level1-omega-vault", ProgressionIds.PuzzleOmegaVaultSafe);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("milestone:backrooms-entered-via-veil", ProgressionIds.MilestoneBackroomsEnteredViaVeil);
            Assert.AreEqual("milestone:backrooms-entered-via-azucenas-manhole", ProgressionIds.MilestoneBackroomsEnteredViaAzucenas);
            Assert.AreEqual("milestone:returned-to-border-corridor-from-backrooms", ProgressionIds.MilestoneReturnedFromBackrooms);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);

            // North Wing M1 & Elevators & VerificationChecks intact
            Assert.IsNotNull(root.transform.Find("Backrooms_Architecture/" + BackroomsLevel1Builder.NorthWingM1RootName));
            Assert.AreEqual(35, GameObject.Find("Hotel_Elevator_Core").transform.childCount);
            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks());
        }

        [Test]
        public void NarrativeMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeTransforms = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeDescs = root.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaHotelNarrativeInOpenScene(scene);

            Assert.AreEqual(beforeTransforms, root.GetComponentsInChildren<Transform>(includeInactive: true).Length);
            Assert.AreEqual(beforeDescs, root.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true).Length);
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length);

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaHotelNarrativeInOpenScene(emptyScene));
        }
    }
}
