using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    public static class ConsolidationMover
    {
        private const string OldMeshDir = "Assets/Development/BobbleheadProofOfConcept/Meshes";
        private const string NewMeshDir = "Assets/Meshes/Characters/Bobblehead";

        private const string OldTabletTex = "Assets/Staging/ElenaPilot/Textures/T_Elena_TabletScreen.png";
        private const string NewTabletTex = "Assets/Textures/Environment/T_Kiosk_TerminalScreen.png";

        private const string PrefabPath = "Assets/Prefabs/Characters/NPCs/ElenaRamos_Bobblehead.prefab";
        private const string TabletMatPath = "Assets/Materials/Characters/Elena_TabletScreen.mat";
        private const string CorridorScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string PocScenePath = "Assets/Development/BobbleheadProofOfConcept/Scenes/BobbleheadElenaPocScene.unity";

        [MenuItem("MyVirtualCommunity/Consolidate Production Assets (5.8.5.1)")]
        public static void ExecuteConsolidation()
        {
            Debug.Log("[ConsolidationMover] Starting Increment 5.8.5.1 Asset Consolidation...");

            // 1. Ensure target directories
            EnsureDirectory("Assets/Meshes");
            EnsureDirectory("Assets/Meshes/Characters");
            EnsureDirectory(NewMeshDir);
            EnsureDirectory("Assets/Textures");
            EnsureDirectory("Assets/Textures/Environment");

            // 2. Move active kiosk terminal texture
            if (File.Exists(OldTabletTex))
            {
                string oldGuid = AssetDatabase.AssetPathToGUID(OldTabletTex);
                Debug.Log($"[ConsolidationMover] Moving Tablet Texture from {OldTabletTex} (GUID: {oldGuid}) to {NewTabletTex}...");
                string moveErr = AssetDatabase.MoveAsset(OldTabletTex, NewTabletTex);
                if (!string.IsNullOrEmpty(moveErr))
                {
                    Debug.LogError($"[ConsolidationMover] ERROR moving tablet texture: {moveErr}");
                }
                else
                {
                    string newGuid = AssetDatabase.AssetPathToGUID(NewTabletTex);
                    Debug.Log($"[ConsolidationMover] Successfully moved tablet texture. New path: {NewTabletTex}, GUID preserved: {newGuid == oldGuid} ({newGuid})");
                }
            }
            else
            {
                Debug.LogWarning($"[ConsolidationMover] Tablet texture not found at {OldTabletTex}. Checking {NewTabletTex}...");
                if (File.Exists(NewTabletTex))
                {
                    Debug.Log($"[ConsolidationMover] Tablet texture already present at {NewTabletTex} (GUID: {AssetDatabase.AssetPathToGUID(NewTabletTex)})");
                }
            }

            // 3. Move the 47 Elena Bobblehead meshes
            if (Directory.Exists(OldMeshDir))
            {
                string[] meshFiles = Directory.GetFiles(OldMeshDir, "*.asset");
                Debug.Log($"[ConsolidationMover] Found {meshFiles.Length} mesh assets in {OldMeshDir}.");
                int successCount = 0;
                int failCount = 0;

                foreach (string meshFile in meshFiles)
                {
                    string relOld = meshFile.Replace('\\', '/');
                    string fileName = Path.GetFileName(meshFile);
                    string relNew = $"{NewMeshDir}/{fileName}";

                    string oldGuid = AssetDatabase.AssetPathToGUID(relOld);
                    string err = AssetDatabase.MoveAsset(relOld, relNew);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError($"[ConsolidationMover] Failed to move {relOld} to {relNew}: {err}");
                        failCount++;
                    }
                    else
                    {
                        string newGuid = AssetDatabase.AssetPathToGUID(relNew);
                        if (newGuid != oldGuid)
                        {
                            Debug.LogWarning($"[ConsolidationMover] GUID mismatch for {fileName}! Old: {oldGuid}, New: {newGuid}");
                        }
                        successCount++;
                    }
                }
                Debug.Log($"[ConsolidationMover] Mesh moves complete. Succeeded: {successCount}, Failed: {failCount}");
            }
            else
            {
                Debug.LogWarning($"[ConsolidationMover] Mesh directory {OldMeshDir} not found.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 4. Verify Elena_TabletScreen.mat texture reference
            Material tabletMat = AssetDatabase.LoadAssetAtPath<Material>(TabletMatPath);
            if (tabletMat != null)
            {
                Texture baseMap = tabletMat.GetTexture("_BaseMap");
                Texture mainTex = tabletMat.GetTexture("_MainTex");
                string baseMapPath = baseMap != null ? AssetDatabase.GetAssetPath(baseMap) : "null";
                string mainTexPath = mainTex != null ? AssetDatabase.GetAssetPath(mainTex) : "null";
                Debug.Log($"[ConsolidationMover] Tablet Material Verification: _BaseMap={baseMapPath}, _MainTex={mainTexPath}");
            }
            else
            {
                Debug.LogError($"[ConsolidationMover] Could not load tablet material at {TabletMatPath}");
            }

            // 5. Verify ElenaRamos_Bobblehead.prefab dependencies
            GameObject prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabGo != null)
            {
                MeshFilter[] filters = prefabGo.GetComponentsInChildren<MeshFilter>(true);
                MeshRenderer[] renderers = prefabGo.GetComponentsInChildren<MeshRenderer>(true);
                int missingMeshes = 0;
                int missingMaterials = 0;

                foreach (var mf in filters)
                {
                    if (mf.sharedMesh == null)
                    {
                        Debug.LogError($"[ConsolidationMover] Missing mesh on {mf.gameObject.name} in prefab!");
                        missingMeshes++;
                    }
                    else
                    {
                        string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                        if (meshPath.StartsWith("Assets/Development/"))
                        {
                            Debug.LogWarning($"[ConsolidationMover] Prefab mesh still points to Development path: {meshPath} on {mf.gameObject.name}");
                        }
                    }
                }

                foreach (var mr in renderers)
                {
                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat == null)
                        {
                            Debug.LogError($"[ConsolidationMover] Missing material on {mr.gameObject.name} in prefab!");
                            missingMaterials++;
                        }
                    }
                }

                Debug.Log($"[ConsolidationMover] Prefab Check: {filters.Length} MeshFilters checked, missing meshes={missingMeshes}; {renderers.Length} Renderers checked, missing materials={missingMaterials}");
            }
            else
            {
                Debug.LogError($"[ConsolidationMover] Could not load prefab at {PrefabPath}");
            }

            // 6. Verify Corridor Scene
            var scene = EditorSceneManager.OpenScene(CorridorScenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                Debug.Log($"[ConsolidationMover] Opened {CorridorScenePath} successfully. Scene name: {scene.name}, root count: {scene.rootCount}");
            }
            else
            {
                Debug.LogError($"[ConsolidationMover] Failed to open {CorridorScenePath}");
            }

            // 7. Verify POC Scene
            if (File.Exists(PocScenePath))
            {
                var pocScene = EditorSceneManager.OpenScene(PocScenePath, OpenSceneMode.Single);
                if (pocScene.IsValid())
                {
                    Debug.Log($"[ConsolidationMover] Opened POC scene {PocScenePath} successfully. Scene name: {pocScene.name}, root count: {pocScene.rootCount}");
                }
            }

            // Return to corridor scene
            EditorSceneManager.OpenScene(CorridorScenePath, OpenSceneMode.Single);

            Debug.Log("[ConsolidationMover] Consolidation and Verification Completed Successfully.");
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }
                AssetDatabase.CreateFolder(parent, folder);
                Debug.Log($"[ConsolidationMover] Created folder: {path}");
            }
        }
    }
}

