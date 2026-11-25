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
    /// Automated EditMode verification suite for Implementation 05 (IP-02: Original Borderland Drain Intake Prop).
    /// Verifies all 6 curbside drain intakes and their light shafts remain preserved, FoldedCollectorMaintenanceNotice
    /// exists once as a purely decorative folded paper prop with expected mesh/material and no collider/interaction,
    /// zero retired Georgie/PaperBoat references remain in BorderCorridorScene or active assets, existing sewer
    /// gameplay and 3 clue plates remain intact, and targeted migration is idempotent.
    /// </summary>
    [TestFixture]
    public class Ip02DrainIntakePropTests
    {
        private const string BorderCorridorScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        private static readonly (string SlotBaseName, Vector3 CurbPos, float CurbYaw, Vector3 ShaftPos, float ShaftYaw)[] ExpectedIntakes =
        {
            ("DrainIntakeSlot_Juarez_Azucenas_NW",
                new Vector3(-9.80f, 0.09f, -29.10f), 0f,
                new Vector3(-9.80f, -1.40f, -31.36f), 180f),
            ("DrainIntakeSlot_Juarez_MidCorridor_W",
                new Vector3(-2.60f, 0.09f, -4.00f), 90f,
                new Vector3(0.60f, -1.40f, -4.00f), 90f),
            ("DrainIntakeSlot_Mariscal_Azucenas_SW",
                new Vector3(-54.50f, 0.09f, -23.00f), -90f,
                new Vector3(-57.70f, -1.40f, -23.00f), -90f),
            ("DrainIntakeSlot_Mariscal_Gardenias_NW",
                new Vector3(-54.50f, 0.09f, 16.00f), -90f,
                new Vector3(-57.70f, -1.40f, 16.00f), -90f),
            ("DrainIntakeSlot_Gardenias_West_N",
                new Vector3(-26.00f, 0.09f, 24.30f), 180f,
                new Vector3(-26.00f, -1.40f, 26.45f), 0f),
            ("DrainIntakeSlot_NorthTrunk_Mid",
                new Vector3(2.15f, 0.005f, 118.00f), 0f,
                new Vector3(0.60f, -1.40f, 118.00f), 90f)
        };

        [Test]
        public void BorderCorridorScene_ContainsSixCurbsideDrainIntakesAndPreservedLightShafts()
        {
            Scene scene = EditorSceneManager.OpenScene(BorderCorridorScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BorderCorridorScene must open cleanly.");

            GameObject sewerRoot = FindRoot(scene, "Underground_Sewer_Network_Root");
            Assert.IsNotNull(sewerRoot, "Underground_Sewer_Network_Root must exist.");

            Transform intakesRoot = sewerRoot.transform.Find(UndergroundSewerBuilder.DrainIntakesRootName);
            Assert.IsNotNull(intakesRoot, $"Expected '{UndergroundSewerBuilder.DrainIntakesRootName}' under Underground_Sewer_Network_Root.");

            int curbCount = 0;
            int shaftCount = 0;
            for (int i = 0; i < intakesRoot.childCount; i++)
            {
                string n = intakesRoot.GetChild(i).name;
                if (n.EndsWith("_SurfaceCurb", StringComparison.Ordinal)) curbCount++;
                if (n.EndsWith("_UndergroundLightShaft", StringComparison.Ordinal)) shaftCount++;
            }
            Assert.AreEqual(6, curbCount, "Expected exactly 6 curbside drain intake surface curb groups.");
            Assert.AreEqual(6, shaftCount, "Expected exactly 6 curbside drain intake light shaft groups.");

            foreach (var expected in ExpectedIntakes)
            {
                Transform curb = intakesRoot.Find(expected.SlotBaseName + "_SurfaceCurb");
                Transform shaft = intakesRoot.Find(expected.SlotBaseName + "_UndergroundLightShaft");
                Assert.IsNotNull(curb, $"Missing surface curb group '{expected.SlotBaseName}_SurfaceCurb'.");
                Assert.IsNotNull(shaft, $"Missing underground light shaft group '{expected.SlotBaseName}_UndergroundLightShaft'.");

                Assert.LessOrEqual(Vector3.Distance(curb.position, expected.CurbPos), 0.02f, $"Curb '{curb.name}' moved unexpectedly.");
                Assert.LessOrEqual(Quaternion.Angle(curb.rotation, Quaternion.Euler(0f, expected.CurbYaw, 0f)), 0.5f, $"Curb '{curb.name}' rotation changed.");

                Assert.LessOrEqual(Vector3.Distance(shaft.position, expected.ShaftPos), 0.02f, $"Light shaft '{shaft.name}' moved unexpectedly.");
                Assert.LessOrEqual(Quaternion.Angle(shaft.rotation, Quaternion.Euler(0f, expected.ShaftYaw, 0f)), 0.5f, $"Light shaft '{shaft.name}' rotation changed.");

                Assert.IsNotNull(shaft.Find("VolumetricLightCone"), $"Light shaft '{shaft.name}' missing VolumetricLightCone.");
                Transform spotTf = shaft.Find("DrainSlot_SpotLight");
                Assert.IsNotNull(spotTf, $"Light shaft '{shaft.name}' missing DrainSlot_SpotLight.");
                Light sl = spotTf.GetComponent<Light>();
                Assert.IsNotNull(sl);
                Assert.AreEqual(LightType.Spot, sl.type);
                Assert.AreEqual(2.7f, sl.intensity, 0.01f);
                Assert.AreEqual(5.8f, sl.range, 0.01f);
                Assert.AreEqual(44f, sl.spotAngle, 0.1f);
                Assert.AreEqual(18f, sl.innerSpotAngle, 0.1f);
                Assert.AreEqual(0.76f, sl.color.r, 0.01f);
                Assert.AreEqual(0.86f, sl.color.g, 0.01f);
                Assert.AreEqual(0.96f, sl.color.b, 0.01f);
            }
        }

        [Test]
        public void BorderCorridorScene_FoldedCollectorNoticeExistsOnceWithValidMeshMaterialAndNoInteraction()
        {
            Scene scene = EditorSceneManager.OpenScene(BorderCorridorScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            var foundNotices = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform tf in root.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (tf.name == UndergroundSewerBuilder.CollectorNoticeObjectName)
                    {
                        foundNotices.Add(tf);
                    }
                }
            }

            Assert.AreEqual(1, foundNotices.Count, "FoldedCollectorMaintenanceNotice must exist exactly once in BorderCorridorScene.");
            Transform noticeTf = foundNotices[0];
            Assert.IsNotNull(noticeTf.parent);
            Assert.AreEqual(UndergroundSewerBuilder.DrainIntakesRootName, noticeTf.parent.name);
            Assert.LessOrEqual(
                Vector3.Distance(noticeTf.position, UndergroundSewerBuilder.CollectorNoticeWorldPosition),
                0.01f,
                "FoldedCollectorMaintenanceNotice must sit at the expected Azucenas storm-drain intake position.");

            // Purely decorative: no colliders, no inspection descriptor, no interaction components
            Assert.AreEqual(0, noticeTf.GetComponentsInChildren<Collider>(true).Length, "Decorative maintenance notice must have zero colliders.");
            Assert.AreEqual(0, noticeTf.GetComponentsInChildren<SecretContainmentArtifactDescriptor>(true).Length, "Decorative maintenance notice must not be inspectable or added to journal.");

            Transform meshTf = noticeTf.Find(UndergroundSewerBuilder.CollectorNoticeMeshObjectName);
            Assert.IsNotNull(meshTf, $"Expected child mesh object '{UndergroundSewerBuilder.CollectorNoticeMeshObjectName}'.");
            MeshFilter mf = meshTf.GetComponent<MeshFilter>();
            MeshRenderer mr = meshTf.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mf);
            Assert.IsNotNull(mr);
            Assert.IsNotNull(mf.sharedMesh, "Replacement notice must have a valid serialized mesh.");
            Assert.GreaterOrEqual(mf.sharedMesh.vertexCount, 18, "Folded notice mesh should contain multi-panel folded geometry.");
            Assert.Less(mf.sharedMesh.bounds.max.y, 0.06f, "Folded notice must stay a low flat folded sheet (not a tall origami boat sail).");

            Material expectedMat = AssetDatabase.LoadAssetAtPath<Material>(UndergroundSewerBuilder.CollectorNoticeMaterialPath);
            Texture2D expectedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(UndergroundSewerBuilder.CollectorNoticeTexturePath);
            Assert.IsNotNull(expectedMat, "Sewer_CollectorNotice.mat must exist.");
            Assert.IsNotNull(expectedTex, "T_Sewer_CollectorNotice.png must exist.");
            Assert.AreEqual(expectedMat, mr.sharedMaterial, "Notice mesh must reference Sewer_CollectorNotice.mat.");
            Assert.AreEqual(expectedTex, mr.sharedMaterial.GetTexture("_BaseMap"), "Sewer_CollectorNotice.mat must bind T_Sewer_CollectorNotice.png.");
            Assert.AreEqual(0f, mr.sharedMaterial.GetFloat("_Cull"), 0.01f, "Double-sided rendering (_Cull = 0) must remain enabled for folded paper.");
        }

        [Test]
        public void ActiveSceneAndAssets_ContainZeroRetiredGeorgieOrPaperBoatReferences()
        {
            Scene scene = EditorSceneManager.OpenScene(BorderCorridorScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            string[] forbiddenSceneNames = { "Georgie", "PaperBoat", "Origami_PaperBoat" };
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform tf in root.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    foreach (string term in forbiddenSceneNames)
                    {
                        Assert.IsTrue(
                            tf.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                            $"Retired IP-02 term '{term}' found on GameObject '{tf.name}' in BorderCorridorScene.");
                    }
                }
            }

            // Confirm retired assets no longer exist on disk
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(File.Exists(Path.Combine(projectRoot, "Assets", "Textures", "Sewer", "T_Sewer_PaperBoat_Newspaper.png")),
                "Retired T_Sewer_PaperBoat_Newspaper.png must not remain on disk.");
            Assert.IsFalse(File.Exists(Path.Combine(projectRoot, "Assets", "Settings", "Sewer", "Sewer_PaperBoat.mat")),
                "Retired Sewer_PaperBoat.mat must not remain on disk.");
        }

        [Test]
        public void SewerGameplayAndCluePlates_RemainIntactAndVerificationChecksPass()
        {
            Scene scene = EditorSceneManager.OpenScene(BorderCorridorScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            // Confirm the 3 underground clue plates remain intact with UndergroundSewer scope
            AssertCluePlate("CluePlate_01_SectorC0", ProgressionIds.ClueSewerSectorC0);
            AssertCluePlate("CluePlate_02_CisternElNudo", ProgressionIds.ClueCisternElNudo);
            AssertCluePlate("CluePlate_03_AbasoloSiphon", ProgressionIds.ClueAbasoloSiphon);

            // Confirm core underground landmarks & interactivity remain intact
            Assert.AreEqual(5, UnityEngine.Object.FindObjectsByType<SewerManholeInteractable>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("Vault_BalconyRing_SeamlessMesh"));
            Assert.IsNotNull(GameObject.Find("LoreBulkhead_SectorC0_UnderChihuas"));
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<SewerOutflowGateController>());

            // Run the full UndergroundSewerBuilder verification suite
            Assert.IsTrue(UndergroundSewerBuilder.RunVerificationChecks(), "UndergroundSewerBuilder.RunVerificationChecks() must pass.");
        }

        [Test]
        public void TargetedIp02Migration_IsIdempotentInOpenScene()
        {
            Scene scene = EditorSceneManager.OpenScene(BorderCorridorScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            UndergroundSewerBuilder.MigrateIp02DrainNoticeInOpenScene(scene);
            UndergroundSewerBuilder.MigrateIp02DrainNoticeInOpenScene(scene);

            GameObject sewerRoot = FindRoot(scene, "Underground_Sewer_Network_Root");
            Assert.IsNotNull(sewerRoot);
            Transform intakesRoot = sewerRoot.transform.Find(UndergroundSewerBuilder.DrainIntakesRootName);
            Assert.IsNotNull(intakesRoot);

            int noticeCount = 0;
            int curbCount = 0;
            int shaftCount = 0;
            for (int i = 0; i < intakesRoot.childCount; i++)
            {
                string childName = intakesRoot.GetChild(i).name;
                if (childName == UndergroundSewerBuilder.CollectorNoticeObjectName) noticeCount++;
                if (childName.EndsWith("_SurfaceCurb", StringComparison.Ordinal)) curbCount++;
                if (childName.EndsWith("_UndergroundLightShaft", StringComparison.Ordinal)) shaftCount++;
            }

            Assert.AreEqual(1, noticeCount, "Repeated migration must keep exactly 1 FoldedCollectorMaintenanceNotice.");
            Assert.AreEqual(6, curbCount, "Repeated migration must preserve exactly 6 surface curb intakes.");
            Assert.AreEqual(6, shaftCount, "Repeated migration must preserve exactly 6 underground light shafts.");
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == rootName) return go;
            }
            return null;
        }

        private static void AssertCluePlate(string objectName, string expectedId)
        {
            GameObject go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"Missing underground clue plate '{objectName}'.");
            SecretContainmentArtifactDescriptor desc = go.GetComponent<SecretContainmentArtifactDescriptor>();
            Assert.IsNotNull(desc, $"Missing descriptor on '{objectName}'.");
            Assert.AreEqual(expectedId, desc.artifactId);
            Assert.AreEqual(ArtifactInspectionScope.UndergroundSewer, desc.inspectionScope);
        }
    }
}
