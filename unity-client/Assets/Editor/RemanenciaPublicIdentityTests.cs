using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode verification suite for Implementation 07.1 (Remanencia Public Identity Transition).
    /// Verifies player-facing bilingual Remanencia milestone and checkpoint labels, updated BackroomsLevel1Scene
    /// descriptors (Ipad_Chassis and Portal_MirrorSurface), removal of Kane Pixels / Found Footage attributions
    /// from cleaned C# files, and strict preservation of all 7 persistent progression IDs.
    /// </summary>
    [TestFixture]
    public class RemanenciaPublicIdentityTests
    {
        private string tempRootDir;

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_RemanenciaIdentity_" + Guid.NewGuid().ToString("N"));
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
        public void DiscoveryJournalAndCheckpoint_DisplayApprovedRemanenciaBilingualLabels()
        {
            var storage = new LocalProgressionStorage(tempRootDir);
            var service = new PlayerProgressionService(storage);
            service.StartNewGame();
            service.RecordBackroomsEntered(viaAzucenasManhole: false);

            // Checkpoint labels
            Assert.AreEqual("Remanencia — Nivel 0", service.GetLocalizedCheckpointLabel(isEs: true));
            Assert.AreEqual("Remanencia — Level 0", service.GetLocalizedCheckpointLabel(isEs: false));

            // Discovery Journal milestone titles
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: true);
            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(service, isEs: false);

            Assert.AreEqual("Remanencia — Nivel 0", es.CurrentCheckpointLabel);
            Assert.AreEqual("Remanencia — Level 0", en.CurrentCheckpointLabel);

            bool foundEsMilestone = false;
            for (int i = 0; i < es.MilestoneEntries.Count; i++)
            {
                if (es.MilestoneEntries[i].Title == "Ingreso a Remanencia — Nivel 0") foundEsMilestone = true;
                StringAssert.DoesNotContain("Backrooms", es.MilestoneEntries[i].Title);
            }
            Assert.IsTrue(foundEsMilestone, "Expected Spanish Remanencia entry milestone in Discovery Journal.");

            bool foundEnMilestone = false;
            for (int i = 0; i < en.MilestoneEntries.Count; i++)
            {
                if (en.MilestoneEntries[i].Title == "Entered Remanencia — Level 0") foundEnMilestone = true;
                StringAssert.DoesNotContain("Backrooms", en.MilestoneEntries[i].Title);
            }
            Assert.IsTrue(foundEnMilestone, "Expected English Remanencia entry milestone in Discovery Journal.");
        }

        [Test]
        public void SceneDescriptors_UseApprovedRemanenciaAndNeutralPortalTerminology()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BackroomsLevel1Scene must open cleanly.");

            SecretContainmentArtifactDescriptor ipadDesc = null;
            SecretContainmentArtifactDescriptor portalDesc = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var desc in root.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true))
                {
                    if (desc.artifactId == "reality-bridge:diario-de-juarez-ipad") ipadDesc = desc;
                    if (desc.artifactId == ProgressionIds.PortalUmbralDeLasser) portalDesc = desc;
                }
            }

            // 1. Ipad_Chassis descriptor uses Remanencia (no Backrooms)
            Assert.IsNotNull(ipadDesc, "Expected Ipad_Chassis descriptor in BackroomsLevel1Scene.");
            StringAssert.Contains("Remanencia", ipadDesc.loreEs);
            StringAssert.Contains("Remanencia", ipadDesc.loreEn);
            StringAssert.DoesNotContain("Backrooms", ipadDesc.loreEs);
            StringAssert.DoesNotContain("Backrooms", ipadDesc.loreEn);

            // 2. Portal_MirrorSurface descriptor uses neutral Dimensional Passage terminology (no El Umbral de Lasser / Level 1 -> Level 2)
            Assert.IsNotNull(portalDesc, "Expected Portal_MirrorSurface descriptor in BackroomsLevel1Scene.");
            Assert.AreEqual("Pasaje dimensional", portalDesc.titleEs);
            Assert.AreEqual("Dimensional Passage", portalDesc.titleEn);
            StringAssert.Contains("Remanencia", portalDesc.loreEs);
            StringAssert.Contains("Remanencia", portalDesc.loreEn);

            string[] rejectedPortalTerms =
            {
                "Umbral de Lasser",
                "Lasser Threshold",
                "Nivel 1",
                "Nivel 2",
                "Level 1",
                "Level 2",
                "Backrooms"
            };
            foreach (string term in rejectedPortalTerms)
            {
                Assert.IsTrue(portalDesc.titleEs.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0, $"Rejected term '{term}' in portal titleEs.");
                Assert.IsTrue(portalDesc.titleEn.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0, $"Rejected term '{term}' in portal titleEn.");
                Assert.IsTrue(portalDesc.loreEs.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0, $"Rejected term '{term}' in portal loreEs.");
                Assert.IsTrue(portalDesc.loreEn.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0, $"Rejected term '{term}' in portal loreEn.");
            }
        }

        [Test]
        public void PersistentProgressionIds_RemainUnchangedForSaveCompatibility()
        {
            Assert.AreEqual("safe:backrooms-level1-omega-vault", ProgressionIds.SafeBackroomsOmegaVault);
            Assert.AreEqual("puzzle:safe:backrooms-level1-omega-vault", ProgressionIds.PuzzleOmegaVaultSafe);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("milestone:backrooms-entered-via-veil", ProgressionIds.MilestoneBackroomsEnteredViaVeil);
            Assert.AreEqual("milestone:backrooms-entered-via-azucenas-manhole", ProgressionIds.MilestoneBackroomsEnteredViaAzucenas);
            Assert.AreEqual("milestone:returned-to-border-corridor-from-backrooms", ProgressionIds.MilestoneReturnedFromBackrooms);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);
            Assert.AreEqual("portal:umbral-de-lasser", ProgressionIds.PortalUmbralDeLasser);
        }

        [Test]
        public void CleanedScripts_ContainZeroKanePixelsOrFoundFootageAttributions()
        {
            string assetsRoot = Application.dataPath;
            string[] cleanedFiles =
            {
                Path.Combine(assetsRoot, "Editor", "BackroomsLevel1Builder.cs"),
                Path.Combine(assetsRoot, "Editor", "UndergroundSewerBuilder.cs"),
                Path.Combine(assetsRoot, "Scripts", "SewerManholeInteractable.cs"),
                Path.Combine(assetsRoot, "Scripts", "SecretBackroomsTransitionController.cs")
            };
            string[] forbiddenAttributions = { "Kane Pixels", "Kane Parsons", "Found Footage" };

            foreach (string file in cleanedFiles)
            {
                Assert.IsTrue(File.Exists(file), $"Expected script file at {file}");
                string content = File.ReadAllText(file);
                foreach (string term in forbiddenAttributions)
                {
                    Assert.IsTrue(
                        content.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Legacy attribution '{term}' must not remain in {Path.GetFileName(file)}.");
                }
            }
        }
    }
}
