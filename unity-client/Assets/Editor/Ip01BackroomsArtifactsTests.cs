using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode verification suite for Implementation 04 (IP-01 Critical Replacement):
    /// Verifies that all 12 retired horror-franchise artifacts in BackroomsLevel1Scene.unity and
    /// DiscoveryJournalCatalog have been replaced with original archival/anomalous artifacts,
    /// zero forbidden IP-01 terms or quotes remain in playable scene descriptors/names or journal copy,
    /// preserved gameplay elements remain intact, and legacy saves migrate deterministically & idempotently.
    /// </summary>
    [TestFixture]
    public class Ip01BackroomsArtifactsTests
    {
        private string tempRootDir;

        private static readonly string[] ForbiddenIp01Terms =
        {
            "Annabelle",
            "The Conjuring",
            "El Conjuro",
            "Babadook",
            "Black Phone",
            "Teléfono Negro",
            "Telefono Negro",
            "The Ring",
            "Ringu",
            "El Aro",
            "7 DAYS",
            "7 DÍAS",
            "7 DIAS",
            "Billy",
            "Billy / Saw",
            "Quiero jugar un juego",
            "play a game",
            "Chucky",
            "Good Guy",
            "Child's Play",
            "Dybbuk",
            "The Possession",
            "Origen del Mal",
            "Evil Dead",
            "Hamunaptra",
            "Necronomicon",
            "Grimorio",
            "Grimoire",
            "Book of the Dead",
            "Libro de los Muertos",
            "Talk to Me",
            "Háblame",
            "Hablame",
            "90 segundos",
            "90 seconds",
            "Hellraiser",
            "Lament Configuration",
            "Configuración del Lamento",
            "Configuracion del Lamento",
            "The Exorcist",
            "El Exorcista",
            "Ouija",
            "Planchette",
            "GOODBYE",
            "POSITIVELY DO NOT OPEN",
            "POSITIVAMENTE NO ABRIR"
        };

        private static readonly (string GameObjectName, string ArtifactId)[] ExpectedReplacementArtifacts =
        {
            ("Artifact_ShiftClock0307", ProgressionIds.ArtifactShiftClock0307),
            ("Artifact_SealedNotebook", ProgressionIds.ArtifactSealedNotebook),
            ("Artifact_ServiceIntercom", ProgressionIds.ArtifactServiceIntercom),
            ("Artifact_SignalMonitor_Root", ProgressionIds.ArtifactSignalMonitor),
            ("Artifact_SpecimenVitrine", ProgressionIds.ArtifactSpecimenVitrine),
            ("Artifact_ArticulatedWoodAssembly", ProgressionIds.ArtifactArticulatedWoodAssembly),
            ("Artifact_ArchiveCase06", ProgressionIds.ArtifactArchiveCase06),
            ("Artifact_SealedCustodyCase", ProgressionIds.ArtifactSealedCustodyCase),
            ("Artifact_FracturedInsulator", ProgressionIds.ArtifactFracturedInsulator),
            ("Artifact_CoordinatePlottingPlate", ProgressionIds.ArtifactCoordinatePlottingPlate),
            ("Artifact_AnomalousGeometryAtlas", ProgressionIds.ArtifactAnomalousGeometryAtlas),
            ("Artifact_FoldedTopologyCore", ProgressionIds.ArtifactFoldedTopologyCore)
        };

        [SetUp]
        public void SetUp()
        {
            tempRootDir = Path.Combine(Path.GetTempPath(), "MVC_Ip01Tests_" + Guid.NewGuid().ToString("N"));
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
                // Ignore transient Windows file locks during cleanup
            }
        }

        [Test]
        public void BackroomsScene_ContainsZeroForbiddenIp01FranchiseReferences()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BackroomsLevel1Scene must open cleanly.");

            var allTransforms = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                allTransforms.AddRange(root.GetComponentsInChildren<Transform>(includeInactive: true));
            }

            foreach (Transform t in allTransforms)
            {
                AssertNoForbiddenIp01Text(t.name, $"GameObject name '{t.name}'");
            }

            var descriptors = new List<SecretContainmentArtifactDescriptor>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                descriptors.AddRange(root.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true));
            }

            Assert.GreaterOrEqual(descriptors.Count, 24, "Expected all dispersed artifacts, vault/portal descriptors, and Reality-Bridge props.");

            foreach (SecretContainmentArtifactDescriptor desc in descriptors)
            {
                Assert.IsFalse(
                    ProgressionIds.LegacyEvidenceIdMigrationMap.ContainsKey(desc.artifactId ?? string.Empty),
                    $"Retired IP-01 artifactId '{desc.artifactId}' must not remain in BackroomsLevel1Scene on '{desc.gameObject.name}'.");

                AssertNoForbiddenIp01Text(desc.artifactId, $"Descriptor artifactId on '{desc.gameObject.name}'");
                AssertNoForbiddenIp01Text(desc.titleEs, $"Descriptor titleEs on '{desc.gameObject.name}'");
                AssertNoForbiddenIp01Text(desc.titleEn, $"Descriptor titleEn on '{desc.gameObject.name}'");
                AssertNoForbiddenIp01Text(desc.loreEs, $"Descriptor loreEs on '{desc.gameObject.name}'");
                AssertNoForbiddenIp01Text(desc.loreEn, $"Descriptor loreEn on '{desc.gameObject.name}'");
            }
        }

        [Test]
        public void BackroomsScene_AllTwelveOriginalReplacementArtifactsPresentAndInteractable()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform artifactsRoot = root.transform.Find("Backrooms_DispersedArtifacts");
            Transform propsRoot = root.transform.Find("Backrooms_OrdinaryLiminalProps");
            Assert.IsNotNull(artifactsRoot);
            Assert.IsNotNull(propsRoot);

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (goName, expectedId) in ExpectedReplacementArtifacts)
            {
                Transform artTf = artifactsRoot.Find(goName);
                Assert.IsNotNull(artTf, $"Missing replacement artifact GameObject '{goName}' under Backrooms_DispersedArtifacts.");

                SecretContainmentArtifactDescriptor desc = artTf.GetComponentInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true);
                Assert.IsNotNull(desc, $"Replacement artifact '{goName}' is missing SecretContainmentArtifactDescriptor.");
                Assert.AreEqual(expectedId, desc.artifactId, $"Artifact '{goName}' has unexpected artifactId.");
                Assert.AreEqual(ArtifactInspectionScope.SecretRoomOrBackrooms, desc.inspectionScope, $"Artifact '{goName}' must use SecretRoomOrBackrooms scope.");
                Assert.IsTrue(seenIds.Add(desc.artifactId), $"Duplicate artifactId '{desc.artifactId}'.");

                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.titleEs), $"Artifact '{goName}' missing Spanish title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.titleEn), $"Artifact '{goName}' missing English title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.loreEs), $"Artifact '{goName}' missing Spanish lore.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.loreEn), $"Artifact '{goName}' missing English lore.");

                Collider col = desc.GetComponent<Collider>();
                Assert.IsNotNull(col, $"Descriptor object '{desc.gameObject.name}' under '{goName}' must have a Collider.");
                Assert.IsTrue(col.enabled, $"Collider on '{desc.gameObject.name}' must be enabled.");
                Assert.IsTrue(col.isTrigger, $"Inspection collider on '{desc.gameObject.name}' must be a trigger.");
            }

            // Confirm the 4 renamed pedestals exist and no retired franchise pedestal names remain
            Assert.IsNotNull(propsRoot.Find("SpecimenVitrine_Pedestal"));
            Assert.IsNotNull(propsRoot.Find("Archive_WoodenCrate"));
            Assert.IsNotNull(propsRoot.Find("Insulator_Pedestal"));
            Assert.IsNotNull(propsRoot.Find("Survey_RelicTable"));

            Assert.IsNull(propsRoot.Find("DollVitrine_Pedestal"));
            Assert.IsNull(propsRoot.Find("Billy_WoodenCrate"));
            Assert.IsNull(propsRoot.Find("CeramicHand_Pedestal"));
            Assert.IsNull(propsRoot.Find("Ouija_RelicTable"));
        }

        [Test]
        public void BackroomsScene_PreservedGameplayElementsRemainIntact()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            Assert.IsNotNull(root);
            Transform artifactsRoot = root.transform.Find("Backrooms_DispersedArtifacts");
            Transform realityRoot = root.transform.Find("Backrooms_RealityBridgeObjects");
            Assert.IsNotNull(artifactsRoot);
            Assert.IsNotNull(realityRoot);

            Assert.IsNotNull(root.transform.Find("Backrooms_Architecture/Backrooms_Floor"));
            Assert.IsNotNull(root.transform.Find("Backrooms_SpawnPoint"));
            Assert.IsNotNull(root.transform.Find("Backrooms_ExitDoor_ToStreet"));
            Assert.AreEqual(8, realityRoot.childCount, "All 8 Reality-Bridge props (out of scope for IP-01) must remain intact.");

            // Preserved original artifacts
            AssertDescriptorPresent(artifactsRoot, "Artifact_EvidenceCorkboard", ProgressionIds.ArtifactEvidenceBoardPdn00);
            AssertDescriptorPresent(artifactsRoot, "Artifact_PaquimeObsidianMask", "artifact:mascara-paquime");
            AssertDescriptorPresent(artifactsRoot, "Artifact_AnomalousCompass", "artifact:brujula-astral");

            // Level-Omega Vault Safe ("654321") + El Ojo del Umbral + El Umbral de Lasser
            Transform safeTf = artifactsRoot.Find("SecretRoom_HeavyVaultSafe");
            Assert.IsNotNull(safeTf, "Level-Omega Vault Safe must remain intact.");
            SecretKeypadDoorController safeCtrl = safeTf.GetComponent<SecretKeypadDoorController>();
            Assert.IsNotNull(safeCtrl);
            Assert.AreEqual(ProgressionIds.SafeBackroomsOmegaVault, safeCtrl.doorId);
            Assert.AreEqual("654321", safeCtrl.activeAccessCode);
            AssertDescriptorPresent(safeTf, "VaultSafe_AstrolabeKeystone", ProgressionIds.ArtifactOjoDelUmbral);

            Transform portalTf = artifactsRoot.Find("Portal_DimensionalMirror_Root");
            Assert.IsNotNull(portalTf, "El Umbral de Lasser portal must remain intact.");
            AssertDescriptorPresent(portalTf, "Portal_MirrorSurface", ProgressionIds.PortalUmbralDeLasser);
            AssertDescriptorPresent(portalTf, "Portal_KeystoneSocket_Root/Portal_KeystoneSocket_Trigger", ProgressionIds.PortalCuencaOctagonal);
        }

        [Test]
        public void DiscoveryJournalCatalog_ContainsOriginalBilingualEntriesAndZeroIp01Terms()
        {
            foreach (var (_, canonicalId) in ExpectedReplacementArtifacts)
            {
                Assert.IsTrue(
                    DiscoveryJournalCatalog.TryGetEvidence(canonicalId, out DiscoveryJournalCatalog.LocalizedEntryMetadata meta),
                    $"DiscoveryJournalCatalog must resolve canonical ID '{canonicalId}'.");
                Assert.IsNotNull(meta);
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.TitleEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.TitleEn));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.SummaryEs));
                Assert.IsFalse(string.IsNullOrWhiteSpace(meta.SummaryEn));

                AssertNoForbiddenIp01Text(meta.TitleEs, $"Catalog TitleEs for '{canonicalId}'");
                AssertNoForbiddenIp01Text(meta.TitleEn, $"Catalog TitleEn for '{canonicalId}'");
                AssertNoForbiddenIp01Text(meta.SummaryEs, $"Catalog SummaryEs for '{canonicalId}'");
                AssertNoForbiddenIp01Text(meta.SummaryEn, $"Catalog SummaryEn for '{canonicalId}'");
            }
        }

        [Test]
        public void LegacySaveMigration_PreservesAllTwelveRetiredIdsDeterministicallyAndIdempotently()
        {
            Assert.AreEqual(12, ProgressionIds.LegacyEvidenceIdMigrationMap.Count, "Expected exactly 12 legacy IP-01 ID mappings.");

            var storage = new LocalProgressionStorage(tempRootDir);
            Directory.CreateDirectory(storage.SaveDirectory);

            // Author a realistic v1 pre-IP01 save file directly on disk in an isolated temp folder
            var legacyIds = new List<string>(ProgressionIds.LegacyEvidenceIdMigrationMap.Keys);
            legacyIds.Add(ProgressionIds.ArtifactEvidenceBoardPdn00);
            legacyIds.Add("artifact:mascara-paquime");
            legacyIds.Add("artifact:brujula-astral");

            var sb = new StringBuilder();
            sb.Append("{\"schemaVersion\":1,\"saveId\":\"legacy-ip01-test\",\"createdAtUtc\":\"2026-11-24T12:00:00.000Z\",");
            sb.Append("\"updatedAtUtc\":\"2026-11-24T12:05:00.000Z\",\"saveSequence\":3,");
            sb.Append("\"latestCheckpointId\":\"checkpoint:evidence-board-pdn00-inspected\",");
            sb.Append("\"latestCheckpointAtUtc\":\"2026-11-24T12:05:00.000Z\",");
            sb.Append("\"permanent\":{\"discoveredEvidenceIds\":[");
            for (int i = 0; i < legacyIds.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(legacyIds[i]).Append('"');
            }
            sb.Append("],\"learnedCodeIds\":[\"code:omega-vault-safe\"],\"solvedPuzzleIds\":[],\"acquiredArtifactIds\":[],\"unlockedMilestoneIds\":[\"milestone:backrooms-entered\"]},");
            sb.Append("\"evolving\":{\"ojoDelUmbralPlacement\":\"\",\"lasserPortalCurrentlyActive\":false,\"sewerOutflowGateOpened\":false,\"flags\":[]},");
            sb.Append("\"temporal\":{\"entries\":[]}}");

            File.WriteAllText(storage.PrimaryFilePath, sb.ToString(), new UTF8Encoding(false));

            // 1. Load legacy save and verify deterministic translation without data loss
            var session1 = new PlayerProgressionService(storage);
            ProgressionLoadResult loadResult = session1.EnsureInitialized();
            Assert.AreEqual(ProgressionLoadStatus.Loaded, loadResult.Status);
            Assert.AreEqual(ProgressionSaveData.CurrentSchemaVersion, session1.CurrentData.schemaVersion);
            Assert.AreEqual(15, session1.CurrentData.permanent.discoveredEvidenceIds.Count,
                "All 12 migrated IP-01 discoveries plus 3 preserved discoveries must survive migration.");

            foreach (var kvp in ProgressionIds.LegacyEvidenceIdMigrationMap)
            {
                Assert.IsFalse(session1.CurrentData.permanent.discoveredEvidenceIds.Contains(kvp.Key),
                    $"Legacy ID '{kvp.Key}' should be replaced in-memory by canonical ID '{kvp.Value}'.");
                Assert.IsTrue(session1.CurrentData.permanent.discoveredEvidenceIds.Contains(kvp.Value),
                    $"Canonical ID '{kvp.Value}' must be present after migrating '{kvp.Key}'.");
                Assert.IsTrue(session1.HasDiscoveredEvidence(kvp.Key), "Querying legacy ID must remain backward-compatible.");
                Assert.IsTrue(session1.HasDiscoveredEvidence(kvp.Value), "Querying canonical ID must succeed.");
            }

            // 2. Re-recording either legacy or canonical IDs must NOT duplicate entries or fire duplicate notifications
            int extraNotifications = 0;
            session1.OnNewDiscoveryRecorded += _ => extraNotifications++;
            foreach (var kvp in ProgressionIds.LegacyEvidenceIdMigrationMap)
            {
                Assert.IsFalse(session1.RecordEvidenceInspected(kvp.Key, autoSave: false));
                Assert.IsFalse(session1.RecordEvidenceInspected(kvp.Value, autoSave: false));
            }
            Assert.AreEqual(0, extraNotifications, "Already-migrated discoveries must not fire duplicate notifications.");
            Assert.AreEqual(15, session1.CurrentData.permanent.discoveredEvidenceIds.Count);

            // 3. Discovery Journal renders all 15 entries cleanly in ES & EN with zero forbidden IP-01 terms or raw IDs
            DiscoveryJournalSnapshot en = DiscoveryJournalPresenter.BuildSnapshot(session1, isEs: false);
            DiscoveryJournalSnapshot es = DiscoveryJournalPresenter.BuildSnapshot(session1, isEs: true);
            Assert.AreEqual(15, en.EvidenceEntries.Count);
            Assert.AreEqual(15, es.EvidenceEntries.Count);

            foreach (JournalEntryView entry in en.EvidenceEntries)
            {
                AssertNoForbiddenIp01Text(entry.Title, "Journal EN Title");
                AssertNoForbiddenIp01Text(entry.Description, "Journal EN Description");
                StringAssert.DoesNotContain("artifact:", entry.Title);
                Assert.AreNotEqual("Recorded Evidence", entry.Title, "Migrated artifact must resolve to a real catalog title, not generic fallback.");
            }
            foreach (JournalEntryView entry in es.EvidenceEntries)
            {
                AssertNoForbiddenIp01Text(entry.Title, "Journal ES Title");
                AssertNoForbiddenIp01Text(entry.Description, "Journal ES Description");
                StringAssert.DoesNotContain("artifact:", entry.Title);
                Assert.AreNotEqual("Evidencia registrada", entry.Title, "Migrated artifact must resolve to a real Spanish catalog title.");
            }

            // 4. Save migrated state and verify persisted JSON contains ONLY canonical IDs and reloads idempotently
            Assert.IsTrue(session1.SaveNow().Success);
            string persistedJson = File.ReadAllText(storage.PrimaryFilePath, Encoding.UTF8);
            foreach (var kvp in ProgressionIds.LegacyEvidenceIdMigrationMap)
            {
                StringAssert.DoesNotContain(kvp.Key, persistedJson, $"Persisted JSON should no longer contain retired ID '{kvp.Key}'.");
                StringAssert.Contains(kvp.Value, persistedJson, $"Persisted JSON must contain canonical ID '{kvp.Value}'.");
            }

            var session2 = new PlayerProgressionService(storage);
            Assert.AreEqual(ProgressionLoadStatus.Loaded, session2.EnsureInitialized().Status);
            CollectionAssert.AreEqual(
                session1.CurrentData.permanent.discoveredEvidenceIds,
                session2.CurrentData.permanent.discoveredEvidenceIds,
                "Repeated load/save cycles must be 100% idempotent.");
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == rootName) return go;
            }
            return null;
        }

        private static void AssertDescriptorPresent(Transform parent, string relativePath, string expectedArtifactId)
        {
            Transform tf = parent.Find(relativePath);
            Assert.IsNotNull(tf, $"Missing preserved object '{relativePath}' under '{parent.name}'.");
            SecretContainmentArtifactDescriptor desc = tf.GetComponentInChildren<SecretContainmentArtifactDescriptor>(includeInactive: true);
            Assert.IsNotNull(desc, $"Missing SecretContainmentArtifactDescriptor on preserved object '{relativePath}'.");
            Assert.AreEqual(expectedArtifactId, desc.artifactId, $"Preserved object '{relativePath}' artifactId changed unexpectedly.");
        }

        private static void AssertNoForbiddenIp01Text(string text, string context)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int i = 0; i < ForbiddenIp01Terms.Length; i++)
            {
                string term = ForbiddenIp01Terms[i];
                if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Assert.Fail($"Forbidden IP-01 term '{term}' found in {context}: \"{text}\"");
                }
            }
        }
    }
}
