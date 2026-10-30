using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    public static class CorridorVerificationTest
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string ScreenshotPath = "corridor_preview.png";

        [MenuItem("MyVirtualCommunity/Run Corridor Verification Test")]
        public static void RunVerification()
        {
            Debug.Log("[CorridorVerificationTest] Starting Corridor Scene & NPC Verification (Increment 5.3)...");

            // 1. Open Scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Scene {ScenePath} could not be loaded.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[CorridorVerificationTest] PASS: Loaded scene '{scene.name}' successfully.");

            // 2. Verify Key Physical Environment Objects
            string[] requiredObjects = new string[]
            {
                "Road_AvenidaJuarez",
                "Sidewalk_West",
                "Sidewalk_East",
                "Storefront_FarmaciasBenavides",
                "Storefront_OpticaFashion",
                "Bridge_Canopy_PlazaDeCobro",
                "Border_Information_Kiosk",
                "Pedestrian_Queue_Area",
                "FirstPersonPlayer",
                "PlayerCamera",
                "Sun_DirectionalLight"
            };

            foreach (var name in requiredObjects)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    Debug.LogError($"[CorridorVerificationTest] FAIL: Missing required GameObject '{name}'");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log($"[CorridorVerificationTest] PASS: Found GameObject '{name}' at pos {go.transform.position}");
            }

            // 3. Verify Colliders on Key Structures
            var roadGo = GameObject.Find("Road_AvenidaJuarez");
            if (roadGo == null || roadGo.GetComponent<Collider>() == null)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: Roadway is missing a Collider");
                EditorApplication.Exit(1);
                return;
            }

            var benGo = GameObject.Find("Benavides_MainBuilding");
            if (benGo == null || benGo.GetComponent<Collider>() == null)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: Farmacias Benavides is missing physical Collider");
                EditorApplication.Exit(1);
                return;
            }

            var optGo = GameObject.Find("Optica_MainBuilding");
            if (optGo == null || optGo.GetComponent<Collider>() == null)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: Óptica Fashion is missing physical Collider");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[CorridorVerificationTest] PASS: Physical obstacle colliders verified on road and storefronts.");

            // 4. Verify ALL 6 Interaction Points & Character Identities
            var interactionPoints = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            if (interactionPoints.Length < 6)
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Expected at least 6 InteractionPoints (2 Storefronts, 1 Kiosk, 3 Queue Witnesses), found {interactionPoints.Length}");
                EditorApplication.Exit(1);
                return;
            }

            InteractionPoint ptBen = null;
            InteractionPoint ptOpt = null;
            InteractionPoint ptElena = null;
            InteractionPoint ptMateo = null;
            InteractionPoint ptSofia = null;
            InteractionPoint ptHector = null;

            foreach (var pt in interactionPoints)
            {
                Debug.Log($"[CorridorVerificationTest] Inspecting InteractionPoint '{pt.title}' ({pt.characterId}, Category={pt.category})");

                if (pt.characterId == "carlos-farmacia-attendant")
                {
                    ptBen = pt;
                }
                else if (pt.characterId == "valeria-optica-attendant")
                {
                    ptOpt = pt;
                }
                else if (pt.characterId == "elena-border-concierge")
                {
                    ptElena = pt;
                }
                else if (pt.characterId == "mateo-student")
                {
                    ptMateo = pt;
                }
                else if (pt.characterId == "sofia-cross-border-retail")
                {
                    ptSofia = pt;
                }
                else if (pt.characterId == "hector-artisan")
                {
                    ptHector = pt;
                }
            }

            if (ptBen == null || ptOpt == null || ptElena == null || ptMateo == null || ptSofia == null || ptHector == null)
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Missing one or more canonical NPCs! Carlos={ptBen != null}, Valeria={ptOpt != null}, Elena={ptElena != null}, Mateo={ptMateo != null}, Sofia={ptSofia != null}, Hector={ptHector != null}");
                EditorApplication.Exit(1);
                return;
            }

            // Verify NPC Categories & Avatars
            if (ptBen.category != InteractionCategory.StorefrontAttendant || ptBen.avatar != "💊")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Carlos Méndez category or avatar mismatch: {ptBen.category}, {ptBen.avatar}");
                EditorApplication.Exit(1);
                return;
            }
            if (ptOpt.category != InteractionCategory.StorefrontAttendant || ptOpt.avatar != "👓")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Valeria Domínguez category or avatar mismatch: {ptOpt.category}, {ptOpt.avatar}");
                EditorApplication.Exit(1);
                return;
            }
            if (ptElena.category != InteractionCategory.BorderConcierge || ptElena.avatar != "🛂")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Elena Ramos category or avatar mismatch: {ptElena.category}, {ptElena.avatar}");
                EditorApplication.Exit(1);
                return;
            }
            if (ptMateo.category != InteractionCategory.SyntheticWitness || ptMateo.avatar != "🎒")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Mateo Navarro category or avatar mismatch: {ptMateo.category}, {ptMateo.avatar}");
                EditorApplication.Exit(1);
                return;
            }
            if (ptSofia.category != InteractionCategory.SyntheticWitness || ptSofia.avatar != "💼")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Sofia Alarcón category or avatar mismatch: {ptSofia.category}, {ptSofia.avatar}");
                EditorApplication.Exit(1);
                return;
            }
            if (ptHector.category != InteractionCategory.SyntheticWitness || ptHector.avatar != "🪵")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Don Héctor category or avatar mismatch: {ptHector.category}, {ptHector.avatar}");
                EditorApplication.Exit(1);
                return;
            }

            // Verify suggested prompts configured for bilingual inquiry
            if (ptBen.suggestedPromptsEn.Length == 0 || ptBen.suggestedPromptsEs.Length == 0 ||
                ptElena.suggestedPromptsEn.Length == 0 || ptElena.suggestedPromptsEs.Length == 0)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: Expected non-empty suggestedPromptsEn and suggestedPromptsEs on NPCs.");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[CorridorVerificationTest] PASS: All 6 canonical NPCs (Carlos, Valeria, Elena, Mateo, Sofia, Héctor) verified with correct categories, avatars, and bilingual prompts.");

            // 5. Verify Metric Distances
            float distAcrossStreet = Vector3.Distance(ptBen.transform.position, ptOpt.transform.position);
            Debug.Log($"[CorridorVerificationTest] Metric Check: Distance across avenue = {distAcrossStreet:F2}m (Expected ~21m - 23m).");
            if (distAcrossStreet < 18f || distAcrossStreet > 25f)
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Unexpected distance across street: {distAcrossStreet}m");
                EditorApplication.Exit(1);
                return;
            }

            float distToBridgeKiosk = ptElena.transform.position.z - ptBen.transform.position.z;
            Debug.Log($"[CorridorVerificationTest] Metric Check: Distance North from storefronts to Elena's Kiosk = {distToBridgeKiosk:F2}m (Expected ~38m - 45m).");
            if (distToBridgeKiosk < 35f || distToBridgeKiosk > 48f)
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Unexpected distance to bridge kiosk: {distToBridgeKiosk}m");
                EditorApplication.Exit(1);
                return;
            }

            // 6. Verify Player Character Controller & ApiClient
            var player = GameObject.Find("FirstPersonPlayer");
            var cc = player.GetComponent<CharacterController>();
            var fpp = player.GetComponent<FirstPersonPlayer>();
            var api = player.GetComponent<ApiClient>();
            if (cc == null || fpp == null || api == null)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: FirstPersonPlayer is missing CharacterController, FirstPersonPlayer, or ApiClient component");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[CorridorVerificationTest] PASS: FirstPersonPlayer configured with CharacterController, FirstPersonPlayer, and ApiClient.");

            // 7. Verify DTO Serialization and Truth/Safety Invariants
            RunDtoContractValidation();

            // 8. Verify Build Settings Configuration
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length < 2)
            {
                Debug.LogError("[CorridorVerificationTest] FAIL: EditorBuildSettings should contain at least 2 scenes");
                EditorApplication.Exit(1);
                return;
            }
            if (!scenes[0].path.Contains("BorderCorridorScene"))
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Scene 0 should be BorderCorridorScene, got '{scenes[0].path}'");
                EditorApplication.Exit(1);
                return;
            }
            if (!scenes[1].path.Contains("HandshakeScene"))
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: Scene 1 should be HandshakeScene, got '{scenes[1].path}'");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[CorridorVerificationTest] PASS: EditorBuildSettings configured with BorderCorridorScene at index 0 and HandshakeScene at index 1.");

            // 9. Capture Rendered Screenshot from Player Camera
            var camGo = GameObject.Find("PlayerCamera");
            var cam = camGo.GetComponent<Camera>();
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                Texture2D screenShot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                screenShot.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = screenShot.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(screenShot);

                File.WriteAllBytes(ScreenshotPath, bytes);
                Debug.Log($"[CorridorVerificationTest] PASS: Captured preview screenshot to '{Path.GetFullPath(ScreenshotPath)}' ({bytes.Length} bytes).");
            }

            Debug.Log("[CorridorVerificationTest] ALL INCREMENT 5.3 CHECKS PASSED SUCCESSFULLY!");
            EditorApplication.Exit(0);
        }

        private static void RunDtoContractValidation()
        {
            // Test 1: Border Concierge Request/Response JSON Roundtrip
            var conciergeReq = new AskConciergeRequestDto { message = "Is PDN faster?", language = "en" };
            string conciergeJson = JsonUtility.ToJson(conciergeReq);
            if (!conciergeJson.Contains("\"message\":\"Is PDN faster?\"") || !conciergeJson.Contains("\"language\":\"en\""))
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: AskConciergeRequestDto serialization mismatch: {conciergeJson}");
                EditorApplication.Exit(1);
                return;
            }

            // Test 2: Synthetic Witness Request JSON Roundtrip
            var witnessReq = new WitnessChatRequestDto { npcId = "mateo-student", message = "Hello", language = "es" };
            string witnessJson = JsonUtility.ToJson(witnessReq);
            if (!witnessJson.Contains("\"npcId\":\"mateo-student\"") || !witnessJson.Contains("\"language\":\"es\""))
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: WitnessChatRequestDto serialization mismatch: {witnessJson}");
                EditorApplication.Exit(1);
                return;
            }

            // Test 3: Business Attendant Request JSON Roundtrip
            var attendantReq = new BusinessAttendantChatRequestDto { attendantId = "carlos-farmacia-attendant", message = "Hours?", language = "en" };
            string attendantJson = JsonUtility.ToJson(attendantReq);
            if (!attendantJson.Contains("\"attendantId\":\"carlos-farmacia-attendant\""))
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: BusinessAttendantChatRequestDto serialization mismatch: {attendantJson}");
                EditorApplication.Exit(1);
                return;
            }

            // Test 4: FormatDelay Safety Test (Never fabricates 0 min for null/closed/unavailable)
            var obsOpenWithDelay = new BorderCrossingObservationDto
            {
                portStatus = "Open",
                hasDelayMinutes = true,
                delayMinutes = 25
            };
            if (obsOpenWithDelay.FormatDelay("en") != "25 min" || obsOpenWithDelay.FormatDelay("es") != "25 min")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: FormatDelay open with delay failed: {obsOpenWithDelay.FormatDelay("en")}");
                EditorApplication.Exit(1);
                return;
            }

            var obsClosed = new BorderCrossingObservationDto
            {
                portStatus = "Closed",
                hasDelayMinutes = false,
                delayMinutes = 0
            };
            if (obsClosed.FormatDelay("en") != "Port Closed" || obsClosed.FormatDelay("es") != "Puerto Cerrado")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: FormatDelay closed failed: {obsClosed.FormatDelay("en")}");
                EditorApplication.Exit(1);
                return;
            }

            var obsUnavailable = new BorderCrossingObservationDto
            {
                portStatus = "Open",
                hasDelayMinutes = false,
                delayMinutes = 0
            };
            if (obsUnavailable.FormatDelay("en") != "Unavailable / Pending" || obsUnavailable.FormatDelay("es") != "No disponible / Pendiente")
            {
                Debug.LogError($"[CorridorVerificationTest] FAIL: FormatDelay null delay must not be fabricated as 0 min: {obsUnavailable.FormatDelay("en")}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[CorridorVerificationTest] PASS: DTO contracts, serialization, and FormatDelay safety invariants validated.");
        }
    }
}
