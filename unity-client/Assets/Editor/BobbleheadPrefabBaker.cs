using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    public static class BobbleheadPrefabBaker
    {
        private const string MaterialDir = "Assets/Materials/Characters/Bobblehead";
        private const string MeshDir = "Assets/Meshes/Characters/Bobblehead";
        private const string PrefabDir = "Assets/Prefabs/Characters/NPCs";
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public const string ElenaPrefabPath = "Assets/Prefabs/Characters/NPCs/ElenaRamos_Bobblehead.prefab";
        public const string CarlosPrefabPath = "Assets/Prefabs/Characters/NPCs/CarlosMendez_Bobblehead.prefab";
        public const string ValeriaPrefabPath = "Assets/Prefabs/Characters/NPCs/ValeriaDominguez_Bobblehead.prefab";
        public const string MateoPrefabPath = "Assets/Prefabs/Characters/NPCs/MateoNavarro_Bobblehead.prefab";
        public const string SofiaPrefabPath = "Assets/Prefabs/Characters/NPCs/SofiaAlarcon_Bobblehead.prefab";
        public const string HectorPrefabPath = "Assets/Prefabs/Characters/NPCs/DonHectorMendez_Bobblehead.prefab";
        public const string PatriciaPrefabPath = "Assets/Prefabs/Characters/NPCs/PatriciaMorales_Bobblehead.prefab";

        [MenuItem("MyVirtualCommunity/Bake All Corridor NPC Prefabs (5.9)")]
        public static void BakeAllCorridorNpcPrefabs()
        {
            Debug.Log("[BobbleheadPrefabBaker] Starting Increment 5.9 NPC Prefab Bake Process...");

            EnsureDirectory(MaterialDir);
            EnsureDirectory(MeshDir);
            EnsureDirectory(PrefabDir);

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[BobbleheadPrefabBaker] CRITICAL: 'Universal Render Pipeline/Lit' shader not found!");
                EditorApplication.Exit(1);
                return;
            }

            // 0. Elena Ramos (Border Concierge)
            BakeSingleNpc("Elena", BobbleheadConfig.CreateElenaRamosConfig(), ElenaPrefabPath, isAttendant: true, urpLitShader);

            // 1. Carlos Méndez (Farmacias Benavides Attendant)
            BakeSingleNpc("Carlos", BobbleheadConfig.CreateCarlosMendezConfig(), CarlosPrefabPath, isAttendant: true, urpLitShader);

            // 2. Valeria Domínguez (Óptica Fashion Attendant)
            BakeSingleNpc("Valeria", BobbleheadConfig.CreateValeriaDominguezConfig(), ValeriaPrefabPath, isAttendant: true, urpLitShader);

            // 3. Mateo Navarro (Student Witness)
            BakeSingleNpc("Mateo", BobbleheadConfig.CreateMateoNavarroConfig(), MateoPrefabPath, isAttendant: false, urpLitShader);

            // 4. Sofia Alarcón (Retail Worker Witness)
            BakeSingleNpc("Sofia", BobbleheadConfig.CreateSofiaAlarconConfig(), SofiaPrefabPath, isAttendant: false, urpLitShader);

            // 5. Don Héctor Méndez (Artisan Witness)
            BakeSingleNpc("Hector", BobbleheadConfig.CreateDonHectorMendezConfig(), HectorPrefabPath, isAttendant: false, urpLitShader);

            // 6. Lic. Patricia Morales (Citizen Service Representative - Increment 5.11)
            BakeSingleNpc("Patricia", BobbleheadConfig.CreatePatriciaMoralesConfig(), PatriciaPrefabPath, isAttendant: true, urpLitShader);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BobbleheadPrefabBaker] All 5 NPC Prefabs baked successfully. Now updating corridor scene...");
            UpdateCorridorSceneWithAllNpcs();

            Debug.Log("[BobbleheadPrefabBaker] Increment 5.9 Population Complete!");
            EditorApplication.Exit(0);
        }

        [MenuItem("MyVirtualCommunity/Bake Elena Bobblehead Assets & Prefab")]
        public static void BakeElenaPrefab()
        {
            Debug.Log("[BobbleheadPrefabBaker] Starting Elena Bobblehead Bake Process...");
            EnsureDirectory(MaterialDir);
            EnsureDirectory(MeshDir);
            EnsureDirectory(PrefabDir);

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[BobbleheadPrefabBaker] CRITICAL: 'Universal Render Pipeline/Lit' shader not found!");
                EditorApplication.Exit(1);
                return;
            }

            BakeSingleNpc("Elena", BobbleheadConfig.CreateElenaRamosConfig(), ElenaPrefabPath, isAttendant: true, urpLitShader);
            UpdateProductionSceneElenaOnly();

            Debug.Log("[BobbleheadPrefabBaker] Elena Bake Complete!");
            EditorApplication.Exit(0);
        }

        private static void BakeSingleNpc(string shortName, BobbleheadConfig config, string prefabPath, bool isAttendant, Shader shader)
        {
            Debug.Log($"[BobbleheadPrefabBaker] Baking {config.characterName} ({shortName}) to {prefabPath}...");

            var matDictionary = new Dictionary<string, Material>();
            matDictionary["Outerwear"] = GetOrCreateMaterial($"Mat_{shortName}_Outerwear", config.outerwearColor, 0.12f, 0.0f, shader);
            matDictionary["Innerwear"] = GetOrCreateMaterial($"Mat_{shortName}_Innerwear", config.innerwearColor, 0.12f, 0.0f, shader);
            matDictionary["Pants"] = GetOrCreateMaterial($"Mat_{shortName}_Pants", config.pantsColor, 0.12f, 0.0f, shader);
            matDictionary["Skin"] = GetOrCreateMaterial($"Mat_{shortName}_Skin", config.skinColor, 0.18f, 0.0f, shader);
            matDictionary["Hair"] = GetOrCreateMaterial($"Mat_{shortName}_Hair", config.hairColor, 0.12f, 0.0f, shader);
            matDictionary["GoldBadge"] = GetOrCreateMaterial($"Mat_{shortName}_GoldBadge", new Color(0.95f, 0.82f, 0.15f), 0.90f, 0.85f, shader);
            matDictionary["HeadwearAccent"] = GetOrCreateMaterial($"Mat_{shortName}_HeadwearAccent", config.headwearAccentColor, 0.20f, 0.80f, shader);
            matDictionary["EyeWhite"] = GetOrCreateMaterial($"Mat_{shortName}_EyeWhite", new Color(0.98f, 0.98f, 0.98f), 0.75f, 0.0f, shader);
            matDictionary["Iris"] = GetOrCreateMaterial($"Mat_{shortName}_Iris", config.eyeColor, 0.80f, 0.0f, shader);
            matDictionary["Pupil"] = GetOrCreateMaterial($"Mat_{shortName}_Pupil", config.pupilColor, 0.85f, 0.0f, shader);
            matDictionary["Specular"] = GetOrCreateMaterial($"Mat_{shortName}_Specular", Color.white, 0.98f, 0.0f, shader);
            matDictionary["Brow"] = GetOrCreateMaterial($"Mat_{shortName}_Brow", config.eyebrowColor, 0.10f, 0.0f, shader);
            matDictionary["Nose"] = GetOrCreateMaterial($"Mat_{shortName}_Nose", Color.Lerp(config.skinColor, config.lipColor, 0.12f), 0.18f, 0.0f, shader);
            matDictionary["Lip"] = GetOrCreateMaterial($"Mat_{shortName}_Lip", config.lipColor, 0.25f, 0.0f, shader);
            matDictionary["Blush"] = GetOrCreateMaterial($"Mat_{shortName}_Blush", Color.Lerp(config.skinColor, new Color(0.85f, 0.40f, 0.45f), 0.25f), 0.10f, 0.0f, shader);
            matDictionary["ShoeBase"] = GetOrCreateMaterial($"Mat_{shortName}_ShoeBase", config.shoeBaseColor, 0.25f, 0.0f, shader);
            matDictionary["ShoeAccent"] = GetOrCreateMaterial($"Mat_{shortName}_ShoeAccent", config.shoeAccentColor, 0.30f, 0.0f, shader);
            matDictionary["Stubble"] = GetOrCreateMaterial($"Mat_{shortName}_Stubble", Color.Lerp(config.skinColor, config.hairColor, 0.55f), 0.08f, 0.0f, shader);

            if (config.hasGlasses)
            {
                matDictionary["Glasses"] = GetOrCreateMaterial($"Mat_{shortName}_Glasses", config.glassesColor, 0.45f, 0.60f, shader);
            }
            if (config.hasBackpack)
            {
                matDictionary["Backpack"] = GetOrCreateMaterial($"Mat_{shortName}_Backpack", config.backpackColor, 0.15f, 0.0f, shader);
            }
            if (config.hasSatchel)
            {
                matDictionary["Satchel"] = GetOrCreateMaterial($"Mat_{shortName}_Satchel", config.satchelColor, 0.25f, 0.0f, shader);
            }

            GameObject tempRoot = new GameObject($"{shortName}Bobblehead_BakeTemp");
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;

            var charComp = tempRoot.AddComponent<BobbleheadCharacter>();
            charComp.Initialize(config);

            var animComp = tempRoot.AddComponent<BobbleheadAnimator>();
            animComp.SetAttendantMode(isAttendant);
            animComp.CacheInitialPoses();

            var collider = tempRoot.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.85f, 0f);
            collider.radius = 0.35f;
            collider.height = 1.65f;

            // Bake and persist all procedural meshes to disk
            var meshFilters = tempRoot.GetComponentsInChildren<MeshFilter>(true);
            int meshIndex = 0;
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                string meshName = mf.gameObject.name;
                string meshAssetPath = $"{MeshDir}/{shortName}_Mesh_{meshIndex}_{meshName}.asset";

                Mesh clonedMesh = Object.Instantiate(mf.sharedMesh);
                clonedMesh.name = $"{shortName}_{meshName}";

                AssetDatabase.CreateAsset(clonedMesh, meshAssetPath);
                mf.sharedMesh = clonedMesh;
                meshIndex++;
            }

            // Assign persistent materials to all MeshRenderers
            var meshRenderers = tempRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                string goName = mr.gameObject.name;
                if (goName == "Badge_Text") continue;

                Material targetMat = ResolveMaterialForGameObject(goName, matDictionary);
                if (targetMat != null)
                {
                    mr.sharedMaterial = targetMat;
                }
                else
                {
                    Debug.LogWarning($"[BobbleheadPrefabBaker] Could not resolve material for '{goName}', falling back to Skin.");
                    mr.sharedMaterial = matDictionary["Skin"];
                }
            }

            if (isAttendant)
            {
                animComp.ForcePoseDirect();
            }

            PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);
            Object.DestroyImmediate(tempRoot);
            Debug.Log($"[BobbleheadPrefabBaker] Successfully baked and saved {prefabPath} with {meshIndex} meshes.");
        }

        private static Material GetOrCreateMaterial(string matName, Color color, float smoothness, float metallic, Shader shader)
        {
            string path = $"{MaterialDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = matName;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material ResolveMaterialForGameObject(string name, Dictionary<string, Material> dict)
        {
            if (name == "Badge_Text") return null;
            if (name == "Pelvis" || name.StartsWith("Thigh") || name.StartsWith("Calf")) return dict["Pants"];
            if (name == "Torso" || name.StartsWith("UpperArm") || name.StartsWith("Sleeve") || name.StartsWith("Forearm")) return dict["Outerwear"];
            if (name.Contains("Collar")) return dict["Innerwear"];
            if (name.Contains("Badge")) return dict["GoldBadge"];
            if (name.Contains("Tie") || name.Contains("Clip")) return dict["HeadwearAccent"];
            if (name.Contains("Hair") || name.Contains("Plume") || name.Contains("Ponytail") || name.Contains("Sideburn") || name.Contains("Mustache") || name.Contains("Beard") || name.Contains("Goatee")) return dict["Hair"];
            if (name == "Head" || name == "Neck" || name.StartsWith("Hand") || name.StartsWith("Thumb") || name.StartsWith("Ear")) return dict["Skin"];
            if (name.StartsWith("Eye_")) return dict["EyeWhite"];
            if (name == "Iris") return dict["Iris"];
            if (name == "Pupil") return dict["Pupil"];
            if (name == "Catchlight") return dict["Specular"];
            if (name.StartsWith("Brow")) return dict["Brow"];
            if (name == "Nose") return dict["Nose"];
            if (name == "Lip" || name == "Mouth") return dict["Lip"];
            if (name.StartsWith("Blush")) return dict["Blush"];
            if (name.StartsWith("Shoe_") && !name.Contains("Stripe")) return dict["ShoeBase"];
            if (name.Contains("Stripe") || name.Contains("Wrist")) return dict["ShoeAccent"];
            if (name == "Glasses") return dict.ContainsKey("Glasses") ? dict["Glasses"] : dict["HeadwearAccent"];
            if (name == "Backpack") return dict.ContainsKey("Backpack") ? dict["Backpack"] : dict["Outerwear"];
            if (name == "Satchel") return dict.ContainsKey("Satchel") ? dict["Satchel"] : dict["Outerwear"];
            if (name == "Stubble") return dict["Stubble"];

            return null;
        }

        public static void UpdateCorridorSceneWithAllNpcs()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BobbleheadPrefabBaker] Failed to open scene {ScenePath}");
                return;
            }

            // 0. Elena Ramos @ Concierge Kiosk
            var kioskGo = GameObject.Find("ConciergeKiosk");
            if (kioskGo != null)
            {
                var oldElena = kioskGo.transform.Find("ElenaRamos_Attendant");
                if (oldElena != null) Object.DestroyImmediate(oldElena.gameObject);

                var elenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElenaPrefabPath);
                if (elenaPrefab != null)
                {
                    var elenaInstance = (GameObject)PrefabUtility.InstantiatePrefab(elenaPrefab, kioskGo.transform);
                    elenaInstance.name = "ElenaRamos_Attendant";
                    elenaInstance.transform.localPosition = new Vector3(0f, 0.38f, 0.35f);
                    elenaInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                    var anim = elenaInstance.GetComponent<BobbleheadAnimator>();
                    if (anim != null)
                    {
                        anim.SetAttendantMode(true);
                        anim.CacheInitialPoses();
                    }
                    Debug.Log("[BobbleheadPrefabBaker] Integrated Elena Ramos Bobblehead.");
                }
            }

            // 1. Carlos Méndez @ Farmacias Benavides
            var benavides = GameObject.Find("Storefront_FarmaciasBenavides");
            if (benavides != null)
            {
                var oldCarlos = benavides.transform.Find("CarlosMendez_Attendant");
                if (oldCarlos != null) Object.DestroyImmediate(oldCarlos.gameObject);

                var carlosPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarlosPrefabPath);
                if (carlosPrefab != null)
                {
                    var carlosInstance = (GameObject)PrefabUtility.InstantiatePrefab(carlosPrefab, benavides.transform);
                    carlosInstance.name = "CarlosMendez_Attendant";
                    carlosInstance.transform.localPosition = new Vector3(10.5f, 0.1f, 0f);
                    carlosInstance.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Face West toward street

                    var anim = carlosInstance.GetComponent<BobbleheadAnimator>();
                    if (anim != null)
                    {
                        anim.SetAttendantMode(true);
                        anim.CacheInitialPoses();
                    }
                    Debug.Log("[BobbleheadPrefabBaker] Integrated Carlos Méndez Bobblehead.");
                }
            }

            // 2. Valeria Domínguez @ Óptica Fashion
            var optica = GameObject.Find("Storefront_OpticaFashion");
            if (optica != null)
            {
                var oldValeria = optica.transform.Find("ValeriaDominguez_Attendant");
                if (oldValeria != null) Object.DestroyImmediate(oldValeria.gameObject);

                var valeriaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ValeriaPrefabPath);
                if (valeriaPrefab != null)
                {
                    var valeriaInstance = (GameObject)PrefabUtility.InstantiatePrefab(valeriaPrefab, optica.transform);
                    valeriaInstance.name = "ValeriaDominguez_Attendant";
                    valeriaInstance.transform.localPosition = new Vector3(-10.5f, 0.1f, 1.5f);
                    valeriaInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Face East toward street

                    var anim = valeriaInstance.GetComponent<BobbleheadAnimator>();
                    if (anim != null)
                    {
                        anim.SetAttendantMode(true);
                        anim.CacheInitialPoses();
                    }
                    Debug.Log("[BobbleheadPrefabBaker] Integrated Valeria Domínguez Bobblehead.");
                }
            }

            // 3. Mateo Navarro in Queue
            UpdateQueueWitnessVisual("Witness_MateoNavarro", MateoPrefabPath, "MateoNavarro_Visual");

            // 4. Sofia Alarcón in Queue
            UpdateQueueWitnessVisual("Witness_SofiaAlarcon", SofiaPrefabPath, "SofiaAlarcon_Visual");

            // 5. Don Héctor Méndez in Queue
            UpdateQueueWitnessVisual("Witness_DonHector", HectorPrefabPath, "DonHectorMendez_Visual");

            // 6. Lic. Patricia Morales @ Citizen Service Desk (Increment 5.11)
            UpdateCitizenServiceDesk();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BobbleheadPrefabBaker] Production scene {ScenePath} successfully saved with all 7 Bobblehead NPCs!");
        }

        [MenuItem("MyVirtualCommunity/Update Citizen Service Desk Only")]
        public static void UpdateCitizenServiceDeskOnly()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BobbleheadPrefabBaker] Failed to open scene: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            UpdateCitizenServiceDesk();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BobbleheadPrefabBaker] Successfully updated CitizenServiceDesk in {ScenePath}!");
            EditorApplication.Exit(0);
        }

        private static void UpdateCitizenServiceDesk()
        {
            var deskRoot = GameObject.Find("CitizenServiceDesk");
            if (deskRoot == null)
            {
                deskRoot = new GameObject("CitizenServiceDesk");
            }

            // Target location: close to west wall, near south corner of UrbanBuilding_West_North
            deskRoot.transform.position = new Vector3(-10.8f, 0.0f, 12.8f);
            // Target orientation: facing outward towards the street (East, +X)
            deskRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Clean up any legacy children to avoid duplicate or misaligned geometry
            string[] legacyNames = new string[] {
                "Desk_CounterTop", "Desk_CounterBase", "Desk_Sign",
                "Kiosk_Desk", "Kiosk_Countertop", "Kiosk_Roof",
                "Kiosk_Pillar_1", "Kiosk_Pillar_2", "Kiosk_Pillar_3", "Kiosk_Pillar_4",
                "Kiosk_Front_Fascia_Backing", "Kiosk_Front_Fascia_Sign",
                "Attendant_Platform", "Kiosk_Interior_Spotlight"
            };
            foreach (var legName in legacyNames)
            {
                var child = deskRoot.transform.Find(legName);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }

            // Load Materials
            var kioskBlueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/KioskBlue.mat");
            var countertopMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/CountertopMat.mat");
            var canopyRoofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/CanopyRoof.mat");
            var attendantMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/AttendantMat.mat");
            var signMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/CitizenService_Sign.mat");

            // 1. Counter Base
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Kiosk_Desk";
            baseGo.transform.SetParent(deskRoot.transform, false);
            baseGo.transform.localPosition = new Vector3(0f, 0.45f, 0.25f);
            baseGo.transform.localRotation = Quaternion.identity;
            baseGo.transform.localScale = new Vector3(2.2f, 0.9f, 0.55f);
            if (kioskBlueMat != null) baseGo.GetComponent<Renderer>().sharedMaterial = kioskBlueMat;

            // 2. Counter Top
            var topGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topGo.name = "Kiosk_Countertop";
            topGo.transform.SetParent(deskRoot.transform, false);
            topGo.transform.localPosition = new Vector3(0f, 0.93f, 0.25f);
            topGo.transform.localRotation = Quaternion.identity;
            topGo.transform.localScale = new Vector3(2.4f, 0.06f, 0.7f);
            if (countertopMat != null) topGo.GetComponent<Renderer>().sharedMaterial = countertopMat;

            // 3. Canopy Roof
            var roofGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofGo.name = "Kiosk_Roof";
            roofGo.transform.SetParent(deskRoot.transform, false);
            roofGo.transform.localPosition = new Vector3(0f, 2.70f, 0.0f);
            roofGo.transform.localRotation = Quaternion.identity;
            roofGo.transform.localScale = new Vector3(2.8f, 0.15f, 2.2f);
            if (canopyRoofMat != null) roofGo.GetComponent<Renderer>().sharedMaterial = canopyRoofMat;

            // 4. Pillars (4 corner posts supporting roof)
            Vector3[] pillarPositions = new Vector3[]
            {
                new Vector3(-1.15f, 1.35f, 0.85f),  // Front Left
                new Vector3(1.15f, 1.35f, 0.85f),   // Front Right
                new Vector3(-1.15f, 1.35f, -0.85f), // Back Left
                new Vector3(1.15f, 1.35f, -0.85f)   // Back Right
            };
            for (int i = 0; i < pillarPositions.Length; i++)
            {
                var pillarGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillarGo.name = $"Kiosk_Pillar_{i + 1}";
                pillarGo.transform.SetParent(deskRoot.transform, false);
                pillarGo.transform.localPosition = pillarPositions[i];
                pillarGo.transform.localRotation = Quaternion.identity;
                pillarGo.transform.localScale = new Vector3(0.12f, 2.6f, 0.12f);
                if (kioskBlueMat != null) pillarGo.GetComponent<Renderer>().sharedMaterial = kioskBlueMat;
            }

            // 5. Upper Fascia Sign Backing (mounted on front edge of canopy roof)
            var fasciaBackingGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fasciaBackingGo.name = "Kiosk_Front_Fascia_Backing";
            fasciaBackingGo.transform.SetParent(deskRoot.transform, false);
            fasciaBackingGo.transform.localPosition = new Vector3(0f, 2.70f, 1.12f);
            fasciaBackingGo.transform.localRotation = Quaternion.identity;
            fasciaBackingGo.transform.localScale = new Vector3(2.8f, 0.40f, 0.04f);
            if (kioskBlueMat != null) fasciaBackingGo.GetComponent<Renderer>().sharedMaterial = kioskBlueMat;

            // 6. Upper Fascia Sign (Quad with T_CitizenService_FasciaSign texture, mounted in front of backing)
            var fasciaSignGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fasciaSignGo.name = "Kiosk_Front_Fascia_Sign";
            fasciaSignGo.transform.SetParent(deskRoot.transform, false);
            fasciaSignGo.transform.localPosition = new Vector3(0f, 2.70f, 1.145f);
            fasciaSignGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Face East (+Z local = World East)
            fasciaSignGo.transform.localScale = new Vector3(2.76f, 0.36f, 1.0f);
            var quadCollider = fasciaSignGo.GetComponent<Collider>();
            if (quadCollider != null) Object.DestroyImmediate(quadCollider);
            if (signMat != null) fasciaSignGo.GetComponent<Renderer>().sharedMaterial = signMat;

            // 7. Attendant Platform / Mat
            var platformGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformGo.name = "Attendant_Platform";
            platformGo.transform.SetParent(deskRoot.transform, false);
            platformGo.transform.localPosition = new Vector3(0f, 0.19f, -0.45f);
            platformGo.transform.localRotation = Quaternion.identity;
            platformGo.transform.localScale = new Vector3(1.4f, 0.38f, 0.85f);
            if (attendantMat != null) platformGo.GetComponent<Renderer>().sharedMaterial = attendantMat;

            // 8. Interior Warm Spotlight
            var lightGo = new GameObject("Kiosk_Interior_Spotlight");
            lightGo.transform.SetParent(deskRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point downward
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.94f, 0.85f);
            light.intensity = 15f;
            light.range = 5f;
            light.spotAngle = 80f;

            // 9. Instantiate or update Patricia Bobblehead Attendant
            var oldPatricia = deskRoot.transform.Find("PatriciaMorales_Attendant");
            if (oldPatricia != null) Object.DestroyImmediate(oldPatricia.gameObject);

            var patriciaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatriciaPrefabPath);
            if (patriciaPrefab != null)
            {
                var patriciaInstance = (GameObject)PrefabUtility.InstantiatePrefab(patriciaPrefab, deskRoot.transform);
                patriciaInstance.name = "PatriciaMorales_Attendant";
                patriciaInstance.transform.localPosition = new Vector3(0f, 0.38f, -0.45f);
                patriciaInstance.transform.localRotation = Quaternion.identity; // Face forward (+Z local = World East) across counter

                var anim = patriciaInstance.GetComponent<BobbleheadAnimator>();
                if (anim != null)
                {
                    anim.SetAttendantMode(true);
                    anim.CacheInitialPoses();
                }
            }

            // 10. Ensure InteractionPoint
            var point = deskRoot.GetComponent<InteractionPoint>();
            if (point == null)
            {
                point = deskRoot.AddComponent<InteractionPoint>();
            }

            point.category = InteractionCategory.CitizenService;
            point.characterId = "patricia-citizen-rep";
            point.characterName = "Lic. Patricia Morales";
            point.characterRole = "Enlace de Atención Ciudadana";
            point.avatar = "📋";
            point.suggestedPromptsEn = new string[]
            {
                "I found a pothole on Avenida Juárez near the crossing.",
                "How does simulated citizen issue reporting work?",
                "What areas along the corridor can I report?"
            };
            point.suggestedPromptsEs = new string[]
            {
                "Encontré un bache en Avenida Juárez cerca del cruce.",
                "¿Cómo funciona el reporte ciudadano simulado?",
                "¿Qué zonas del corredor puedo reportar?"
            };
            point.landmarkId = "civic:atencion-ciudadana";
            point.title = "Lic. Patricia Morales";
            point.subtitle = "Enlace de Atención Ciudadana (Servicio Municipal Simulado)";
            point.address = "Avenida Benito Juárez (Acera Poniente)";
            point.description = "Módulo de atención ciudadana para canalizar reportes viales e incidencias urbanas en el corredor virtual con análisis visual SAM 3.1 y estructuración Muse Spark 1.3.";
            point.interactionRadius = 3.5f;

            Debug.Log("[BobbleheadPrefabBaker] Integrated Lic. Patricia Morales at CitizenServiceDesk facing East with upper fascia sign.");
        }

        private static void UpdateQueueWitnessVisual(string witnessRootName, string prefabPath, string visualName)
        {
            var witnessGo = GameObject.Find(witnessRootName);
            if (witnessGo == null)
            {
                Debug.LogWarning($"[BobbleheadPrefabBaker] Witness root '{witnessRootName}' not found in scene!");
                return;
            }

            // Destroy old primitive standee body & head if present
            var oldBody = witnessGo.transform.Find("Standee_Body");
            if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);

            var oldHead = witnessGo.transform.Find("Standee_Head");
            if (oldHead != null) Object.DestroyImmediate(oldHead.gameObject);

            // Destroy old visual instance if present
            var oldVisual = witnessGo.transform.Find(visualName);
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

            // Instantiate Bobblehead Prefab as child visual representation
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[BobbleheadPrefabBaker] Prefab not found at '{prefabPath}' for {witnessRootName}!");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, witnessGo.transform);
            instance.name = visualName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity; // Face North toward toll booths

            var anim = instance.GetComponent<BobbleheadAnimator>();
            if (anim != null)
            {
                anim.SetAttendantMode(false);
                anim.CacheInitialPoses();
            }

            Debug.Log($"[BobbleheadPrefabBaker] Integrated {visualName} into {witnessRootName}.");
        }

        private static void UpdateProductionSceneElenaOnly()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            var kioskGo = GameObject.Find("Border_Information_Kiosk");
            if (kioskGo == null) return;

            var existingElena = kioskGo.transform.Find("ElenaRamos_Attendant");
            if (existingElena != null) Object.DestroyImmediate(existingElena.gameObject);

            GameObject bakedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElenaPrefabPath);
            if (bakedPrefab == null) return;

            GameObject elenaInstance = (GameObject)PrefabUtility.InstantiatePrefab(bakedPrefab, kioskGo.transform);
            elenaInstance.name = "ElenaRamos_Attendant";
            elenaInstance.transform.localPosition = new Vector3(0f, 0.38f, 0.35f);
            elenaInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var anim = elenaInstance.GetComponent<BobbleheadAnimator>();
            if (anim != null)
            {
                anim.SetAttendantMode(true);
                anim.CacheInitialPoses();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
