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
    /// Automated EditMode verification suite for Implementation 07.2 (Remanencia Nivel 0 — Hotel de las Palomas Visual Pivot).
    /// Verifies structural & gameplay preservation, re-authored concrete/plaster/soffit textures and materials,
    /// removal of mustard-yellow surface/atmosphere monoculture, industrial lighting constraints (16 fixture lights,
    /// 5 flicker controllers, zero shadow-casting point lights), IP-05 device texture preservation, and idempotency
    /// of BackroomsLevel1Builder.MigrateRemanenciaLevel0VisualsInOpenScene().
    /// </summary>
    [TestFixture]
    public class RemanenciaLevel0VisualTests
    {
        [Test]
        public void TargetScene_PreservesRequiredRootsStructuralCountsAndGameplayAnchors()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BackroomsLevel1Scene must open cleanly.");

            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            Assert.IsNotNull(root, "BackroomsLevel1_Root must remain present.");

            Transform shell = root.transform.Find("Backrooms_Architecture");
            Transform lights = root.transform.Find("Backrooms_FluorescentLights");
            Transform spawn = root.transform.Find("Backrooms_SpawnPoint");
            Transform exitDoor = root.transform.Find("Backrooms_ExitDoor_ToStreet");
            Transform props = root.transform.Find("Backrooms_OrdinaryLiminalProps");
            Transform reality = root.transform.Find("Backrooms_RealityBridgeObjects");
            Transform artifacts = root.transform.Find("Backrooms_DispersedArtifacts");

            Assert.IsNotNull(shell, "Backrooms_Architecture missing.");
            Assert.IsNotNull(lights, "Backrooms_FluorescentLights missing.");
            Assert.IsNotNull(spawn, "Backrooms_SpawnPoint missing.");
            Assert.IsNotNull(exitDoor, "Backrooms_ExitDoor_ToStreet missing.");
            Assert.IsNotNull(props, "Backrooms_OrdinaryLiminalProps missing.");
            Assert.IsNotNull(reality, "Backrooms_RealityBridgeObjects missing.");
            Assert.IsNotNull(artifacts, "Backrooms_DispersedArtifacts missing.");

            Assert.IsNotNull(exitDoor.GetComponent<BackroomsExitDoorTrigger>(), "Exit door trigger must remain attached.");
            Assert.Less(Vector3.Distance(spawn.position, new Vector3(17.80f, BackroomsLevel1Builder.BaseY + 0.06f, 8.80f)), 0.05f,
                "Spawn position must remain unchanged.");

            // Structural counts under Backrooms_Architecture
            int wallCount = 0, wallBaseboardCount = 0, pillarCount = 0, pillarBaseboardCount = 0;
            int missingVoidCount = 0, crookedHatchCount = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                string n = shell.GetChild(i).name;
                if (n.StartsWith("Wall_", StringComparison.Ordinal))
                {
                    if (n.EndsWith("_Baseboard", StringComparison.Ordinal)) wallBaseboardCount++;
                    else wallCount++;
                }
                else if (n.StartsWith("Pillar_", StringComparison.Ordinal))
                {
                    if (n.EndsWith("_Baseboard", StringComparison.Ordinal)) pillarBaseboardCount++;
                    else pillarCount++;
                }
                else if (n == "Backrooms_MissingTileVoid") missingVoidCount++;
                else if (n == "Backrooms_CrookedCeilingTile") crookedHatchCount++;
            }

            Assert.AreEqual(15, wallCount, "Expected exactly 15 retained structural perimeter/vault/portal/utility walls under Backrooms_Architecture.");
            Assert.AreEqual(15, wallBaseboardCount, "Expected exactly 15 retained wall baseboards.");
            Assert.AreEqual(13, pillarCount, "Expected exactly 13 structural columns.");
            Assert.AreEqual(13, pillarBaseboardCount, "Expected exactly 13 column baseboards.");
            Assert.AreEqual(2, missingVoidCount, "Expected 2 repurposed ceiling opening objects.");
            Assert.AreEqual(2, crookedHatchCount, "Expected 2 repurposed ceiling access hatch objects.");
            Assert.IsNotNull(shell.Find(BackroomsLevel1Builder.HotelArchitectureRootName),
                "Expected Hotel_GroundFloor_Architecture under Backrooms_Architecture.");

            // Floor & Ceiling colliders preserved
            Transform floorTf = shell.Find("Backrooms_Floor");
            Transform ceilingTf = shell.Find("Backrooms_Ceiling");
            Assert.IsNotNull(floorTf);
            Assert.IsNotNull(ceilingTf);
            Assert.IsNotNull(floorTf.GetComponent<Collider>(), "Floor collider must remain intact.");
            Assert.IsNotNull(ceilingTf.GetComponent<Collider>(), "Ceiling collider must remain intact.");

            // Portal, safe, evidence board, and Reality-Bridge roots preserved
            Assert.IsNotNull(artifacts.Find("SecretRoom_HeavyVaultSafe"), "Level-Omega Safe must remain intact.");
            Assert.IsNotNull(artifacts.Find("Portal_DimensionalMirror_Root"), "Dimensional Passage root must remain intact.");
            Assert.IsNotNull(artifacts.Find("Artifact_EvidenceCorkboard"), "Evidence Board PDN-00 must remain intact.");
            Assert.AreEqual(8, reality.childCount, "All 8 Reality-Bridge roots must remain intact.");

            // Progression IDs preserved
            Assert.AreEqual("safe:backrooms-level1-omega-vault", ProgressionIds.SafeBackroomsOmegaVault);
            Assert.AreEqual("milestone:backrooms-entered", ProgressionIds.MilestoneBackroomsEntered);
            Assert.AreEqual("checkpoint:backrooms-entered", ProgressionIds.CheckpointBackroomsEntered);
            Assert.AreEqual("portal:umbral-de-lasser", ProgressionIds.PortalUmbralDeLasser);
        }

        [Test]
        public void ArchitecturalMaterialsAndTextures_ReplaceMustardYellowWithConcretePlasterPalette()
        {
            AssertSurfaceMaterialNonMustard(BackroomsLevel1Builder.WallConcreteMatPath, BackroomsLevel1Builder.WallConcreteTexPath, maxYellowExcess: 0.10f);
            AssertSurfaceMaterialNonMustard(BackroomsLevel1Builder.FloorConcreteMatPath, BackroomsLevel1Builder.FloorConcreteTexPath, maxYellowExcess: 0.10f);
            AssertSurfaceMaterialNonMustard(BackroomsLevel1Builder.CeilingSoffitMatPath, BackroomsLevel1Builder.CeilingSoffitTexPath, maxYellowExcess: 0.08f);
            AssertSurfaceMaterialNonMustard(BackroomsLevel1Builder.ColumnConcreteMatPath, BackroomsLevel1Builder.ColumnConcreteTexPath, maxYellowExcess: 0.08f);
            AssertSurfaceMaterialNonMustard(BackroomsLevel1Builder.PlasterWallMatPath, BackroomsLevel1Builder.PlasterWallTexPath, maxYellowExcess: 0.14f);

            // Scene renderers reference the expected re-authored materials
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            Transform shell = root.transform.Find("Backrooms_Architecture");

            Material wallConcrete = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.WallConcreteMatPath);
            Material wallPlaster = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.PlasterWallMatPath);
            Material colConcrete = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.ColumnConcreteMatPath);
            Material floorConcrete = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.FloorConcreteMatPath);
            Material ceilingSoffit = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.CeilingSoffitMatPath);
            Material industrialMetal = AssetDatabase.LoadAssetAtPath<Material>(BackroomsLevel1Builder.IndustrialMetalMatPath);

            Assert.AreEqual(floorConcrete, shell.Find("Backrooms_Floor").GetComponent<Renderer>().sharedMaterial);
            Assert.AreEqual(ceilingSoffit, shell.Find("Backrooms_Ceiling").GetComponent<Renderer>().sharedMaterial);

            int concreteWalls = 0, plasterWalls = 0, concreteColumns = 0, metalHatches = 0;
            for (int i = 0; i < shell.childCount; i++)
            {
                Transform c = shell.GetChild(i);
                Renderer r = c.GetComponent<Renderer>();
                if (r == null) continue;

                if (c.name.StartsWith("Wall_", StringComparison.Ordinal) && !c.name.EndsWith("_Baseboard", StringComparison.Ordinal))
                {
                    if (r.sharedMaterial == wallConcrete) concreteWalls++;
                    else if (r.sharedMaterial == wallPlaster) plasterWalls++;
                    else Assert.Fail($"Unexpected wall material '{r.sharedMaterial?.name}' on '{c.name}'.");
                }
                else if (c.name.StartsWith("Pillar_", StringComparison.Ordinal) && !c.name.EndsWith("_Baseboard", StringComparison.Ordinal))
                {
                    Assert.AreEqual(colConcrete, r.sharedMaterial, $"Column '{c.name}' must use Remanencia reinforced concrete column material.");
                    concreteColumns++;
                }
                else if (c.name == "Backrooms_CrookedCeilingTile")
                {
                    Assert.AreEqual(industrialMetal, r.sharedMaterial, "Repurposed ceiling access hatch must use industrial metal material.");
                    metalHatches++;
                }
            }

            Assert.Greater(concreteWalls, 0, "Expected unfinished formwork concrete walls.");
            Assert.Greater(plasterWalls, 0, "Expected weathered plaster/terracotta partition walls for architectural variation.");
            Assert.AreEqual(15, concreteWalls + plasterWalls);
            Assert.AreEqual(13, concreteColumns);
            Assert.AreEqual(2, metalHatches);

            // Runtime atmosphere palette must be cool-neutral concrete haze (not yellow-brown)
            Color amb = SecretBackroomsTransitionController.RemanenciaLevel0AmbientLight;
            Color fog = SecretBackroomsTransitionController.RemanenciaLevel0FogColor;
            Assert.GreaterOrEqual(amb.b, amb.r, "Ambient concrete shadow light should not be yellow-shifted.");
            Assert.GreaterOrEqual(fog.b, fog.r, "Enclosed concrete fog should not be yellow-brown.");
            Assert.Less(SecretBackroomsTransitionController.RemanenciaLevel0FogDensity, 0.036f,
                "Fog density should remain lighter/more readable than the legacy yellow fog.");
        }

        [Test]
        public void IndustrialLighting_PreservesCountsZeroShadowsAndFlickerAssignments()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            Transform lightsRoot = root.transform.Find("Backrooms_FluorescentLights");

            Assert.AreEqual(24, lightsRoot.childCount, "Expected exactly 24 industrial ceiling fixtures.");

            Light[] fixtureLights = lightsRoot.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(16, fixtureLights.Length, "Fixture point-light count must remain exactly 16.");

            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count (16 fixtures + portal + signal monitor) must remain 18.");

            foreach (Light l in allLights)
            {
                Assert.AreEqual(LightShadows.None, l.shadows, $"Light '{l.gameObject.name}' must not cast shadows (2 GB VRAM budget).");
            }

            BackroomsAmbientFlicker[] flickers = lightsRoot.GetComponentsInChildren<BackroomsAmbientFlicker>(includeInactive: true);
            Assert.AreEqual(5, flickers.Length, "All 5 existing BackroomsAmbientFlicker controllers must remain preserved.");
            foreach (BackroomsAmbientFlicker fl in flickers)
            {
                Assert.IsNotNull(fl.targetLight, $"Flicker on '{fl.gameObject.name}' must retain targetLight.");
                Assert.IsNotNull(fl.panelRenderer, $"Flicker on '{fl.gameObject.name}' must retain panelRenderer.");
            }

            // Confirm fixtures are re-themed away from old 1.22 x 0.58 office troffers
            for (int i = 0; i < 24; i++)
            {
                Transform panel = lightsRoot.Find($"Backrooms_FluorPanel_{i}");
                Assert.IsNotNull(panel, $"Missing fixture panel Backrooms_FluorPanel_{i}.");
                Assert.IsNotNull(panel.Find($"Backrooms_FluorFrame_{i}"), $"Missing fixture housing Backrooms_FluorFrame_{i}.");
                Assert.Less(panel.localScale.z, 0.30f, $"Fixture Backrooms_FluorPanel_{i} should not retain 0.58m wide office troffer proportions.");
            }

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks(), "BackroomsLevel1Builder.RunVerificationChecks() must pass.");
        }

        [Test]
        public void Ip05DeviceScreenMaterialsAndTextures_RemainUnchanged()
        {
            Material diarioMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Backrooms_DiarioScreen.mat");
            Material laptopMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Backrooms_LaptopScreen.mat");
            Material alexaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Backrooms_AlexaCyanRing.mat");

            Assert.IsNotNull(diarioMat);
            Assert.IsNotNull(laptopMat);
            Assert.IsNotNull(alexaMat);

            Assert.AreEqual(
                "Assets/Textures/Environment/T_Backrooms_DiarioJuarez_Screen.png",
                AssetDatabase.GetAssetPath(diarioMat.GetTexture("_BaseMap")),
                "IP-05 Diario screen texture assignment must remain unchanged.");
            Assert.AreEqual(
                "Assets/Textures/Environment/T_Backrooms_Laptop_Screen.png",
                AssetDatabase.GetAssetPath(laptopMat.GetTexture("_BaseMap")),
                "IP-05 Laptop screen texture assignment must remain unchanged.");
        }

        [Test]
        public void VisualMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            GameObject root = FindRoot(scene, "BackroomsLevel1_Root");
            int initialTransformCount = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int initialLightCount = root.GetComponentsInChildren<Light>(includeInactive: true).Length;
            int initialFlickerCount = root.GetComponentsInChildren<BackroomsAmbientFlicker>(includeInactive: true).Length;

            // Run migration again in memory without saving; counts must remain identical
            BackroomsLevel1Builder.MigrateRemanenciaLevel0VisualsInOpenScene(scene);

            int afterTransformCount = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int afterLightCount = root.GetComponentsInChildren<Light>(includeInactive: true).Length;
            int afterFlickerCount = root.GetComponentsInChildren<BackroomsAmbientFlicker>(includeInactive: true).Length;

            Assert.AreEqual(initialTransformCount, afterTransformCount, "Re-running visual migration must not duplicate or remove objects.");
            Assert.AreEqual(initialLightCount, afterLightCount, "Re-running visual migration must preserve exact light count.");
            Assert.AreEqual(initialFlickerCount, afterFlickerCount, "Re-running visual migration must preserve exact flicker count.");

            // Reload clean scene from disk so dirty in-memory state does not leak
            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            // Verify guard rejects a non-target scene
            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaLevel0VisualsInOpenScene(emptyScene),
                "Visual migration must refuse to operate on scenes other than BackroomsLevel1Scene.unity.");
        }

        private static void AssertSurfaceMaterialNonMustard(string matPath, string expectedTexPath, float maxYellowExcess)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Assert.IsNotNull(mat, $"Missing material at {matPath}.");

            Texture tex = mat.GetTexture("_BaseMap");
            Assert.IsNotNull(tex, $"Material '{matPath}' missing _BaseMap.");
            Assert.AreEqual(expectedTexPath, AssetDatabase.GetAssetPath(tex), $"Material '{matPath}' should reference '{expectedTexPath}'.");

            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", expectedTexPath));
            Assert.IsTrue(File.Exists(fullPath), $"Missing PNG texture on disk at {fullPath}.");

            Texture2D raw = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(raw.LoadImage(File.ReadAllBytes(fullPath)), $"Failed to decode PNG at {expectedTexPath}.");
                Assert.GreaterOrEqual(raw.width, 512, $"Re-authored texture '{expectedTexPath}' should be at least 512px wide.");
                Assert.GreaterOrEqual(raw.height, 512, $"Re-authored texture '{expectedTexPath}' should be at least 512px tall.");

                Color32[] pixels = raw.GetPixels32();
                double rSum = 0, gSum = 0, bSum = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    rSum += pixels[i].r / 255.0;
                    gSum += pixels[i].g / 255.0;
                    bSum += pixels[i].b / 255.0;
                }
                double n = pixels.Length;
                Color tint = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                float avgR = (float)(rSum / n) * tint.r;
                float avgG = (float)(gSum / n) * tint.g;
                float avgB = (float)(bSum / n) * tint.b;

                float luminance = 0.2126f * avgR + 0.7152f * avgG + 0.0722f * avgB;
                float yellowBias = Mathf.Min(avgR, avgG) - avgB;

                Assert.GreaterOrEqual(luminance, 0.25f, $"Surface '{matPath}' is too dark (lum={luminance:F3}).");
                Assert.LessOrEqual(luminance, 0.80f, $"Surface '{matPath}' is overly washed out (lum={luminance:F3}).");
                Assert.LessOrEqual(yellowBias, maxYellowExcess,
                    $"Surface '{matPath}' still shows excessive yellow/mustard bias (R={avgR:F3}, G={avgG:F3}, B={avgB:F3}, yellowBias={yellowBias:F3}).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raw);
            }
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == rootName) return go;
            }
            return null;
        }
    }
}
