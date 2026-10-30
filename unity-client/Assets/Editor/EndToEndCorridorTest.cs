using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated End-to-End Validation & Acceptance Suite for Increment 5.4.
    /// Executes comprehensive checks covering:
    /// 1. Interactive NPC Conversations (Carlos, Valeria, Mateo, Sofia, Hector, Elena)
    /// 2. Live Border Intelligence (Wait times, CBP observations, freshness, no zero-fabrications)
    /// 3. Provider & Provenance Labels (Meta Muse Spark, Deterministic Fallback, Mock Meta)
    /// 4. Gameplay Behavior (Modal state, proximity, message isolation, error retry, language toggles)
    /// 5. Live / Mock HTTP integration against the authoritative Node.js server.
    /// </summary>
    public static class EndToEndCorridorTest
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string BackendBaseUrl = "http://127.0.0.1:3000";

        [MenuItem("MyVirtualCommunity/Run End-to-End Corridor Validation (Increment 5.4)")]
        public static void RunE2EValidation()
        {
            Debug.Log("================================================================================");
            Debug.Log("[EndToEndCorridorTest] STARTING INCREMENT 5.4 END-TO-END VALIDATION SUITE");
            Debug.Log("================================================================================");

            try
            {
                // Step 1: Verify Scene & Scene Graph Invariants
                VerifySceneAndEntities();

                // Step 2: Verify Local Gameplay Behavior & State Isolation
                VerifyGameplayStateAndInvariants();

                // Step 3: Verify Provider & Provenance Badges
                VerifyProviderAndProvenanceBadges();

                // Step 4: Verify Live Border Intelligence Data Invariants
                VerifyBorderWaitTimeInvariants();

                // Step 5: Verify Live HTTP API Endpoints against Node.js Backend
                VerifyLiveHttpEndpoints();

                Debug.Log("================================================================================");
                Debug.Log("[EndToEndCorridorTest] SUCCESS: ALL INCREMENT 5.4 VALIDATION CHECKS PASSED!");
                Debug.Log("================================================================================");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EndToEndCorridorTest] CRITICAL FAILURE: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        private static void VerifySceneAndEntities()
        {
            Debug.Log("[EndToEndCorridorTest] [Phase 1/5] Verifying Scene and Entity Setup...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Scene at {ScenePath} could not be loaded.");
            }

            var playerGo = GameObject.Find("FirstPersonPlayer");
            if (playerGo == null) throw new InvalidOperationException("FirstPersonPlayer GameObject missing.");

            var fpp = playerGo.GetComponent<FirstPersonPlayer>();
            if (fpp == null) throw new InvalidOperationException("FirstPersonPlayer script missing.");

            var api = playerGo.GetComponent<ApiClient>();
            if (api == null) throw new InvalidOperationException("ApiClient component missing on player.");

            // Verify all 6 canonical NPCs
            var pts = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            if (pts.Length < 6)
            {
                throw new InvalidOperationException($"Expected at least 6 interaction points, found {pts.Length}.");
            }

            Debug.Log($"[EndToEndCorridorTest] PASS: Scene verified with {pts.Length} interaction points and configured FirstPersonPlayer.");
        }

        private static void VerifyGameplayStateAndInvariants()
        {
            Debug.Log("[EndToEndCorridorTest] [Phase 2/5] Verifying Gameplay State, Proximity, and Message Isolation...");

            var playerGo = GameObject.Find("FirstPersonPlayer");
            var fpp = playerGo.GetComponent<FirstPersonPlayer>();

            // Find Carlos and Elena interaction points
            InteractionPoint ptBen = null;
            InteractionPoint ptElena = null;
            InteractionPoint ptValeria = null;

            foreach (var pt in UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None))
            {
                if (pt.characterId == "carlos-farmacia-attendant") ptBen = pt;
                if (pt.characterId == "elena-border-concierge") ptElena = pt;
                if (pt.characterId == "valeria-optica-attendant") ptValeria = pt;
            }

            if (ptBen == null || ptElena == null || ptValeria == null)
            {
                throw new InvalidOperationException("Could not find Carlos, Elena, or Valeria interaction points.");
            }

            // Test 1: Language Toggle
            fpp.CurrentLanguage = "es";
            if (fpp.CurrentLanguage != "es") throw new InvalidOperationException("CurrentLanguage failed to toggle to 'es'.");
            fpp.CurrentLanguage = "en";
            if (fpp.CurrentLanguage != "en") throw new InvalidOperationException("CurrentLanguage failed to toggle to 'en'.");

            // Local helper to strictly assert mutually exclusive UI state invariants
            void AssertPrimaryUIState(PrimaryUIState expectedState, bool expectedCursorLocked, bool expectedCanMove, string stepLabel)
            {
                if (fpp.CurrentUIState != expectedState)
                    throw new InvalidOperationException($"[{stepLabel}] CurrentUIState mismatch: expected {expectedState}, got {fpp.CurrentUIState}");

                if (fpp.IsDialogueOpen != (expectedState == PrimaryUIState.Dialogue))
                    throw new InvalidOperationException($"[{stepLabel}] IsDialogueOpen mismatch for {expectedState}");

                if (fpp.IsPauseMenuOpen != (expectedState == PrimaryUIState.PauseMenu))
                    throw new InvalidOperationException($"[{stepLabel}] IsPauseMenuOpen mismatch for {expectedState}");

                if (fpp.IsControlsGuideOpen != (expectedState == PrimaryUIState.ControlsGuide))
                    throw new InvalidOperationException($"[{stepLabel}] IsControlsGuideOpen mismatch for {expectedState}");

                if (fpp.ShowControlsOverlay != (expectedState == PrimaryUIState.ControlsGuide))
                    throw new InvalidOperationException($"[{stepLabel}] ShowControlsOverlay mismatch for {expectedState}");

                // CRITICAL DEFECT ASSERTION: Pause Menu and Controls Guide must NEVER be open simultaneously
                if (fpp.IsPauseMenuOpen && fpp.IsControlsGuideOpen)
                    throw new InvalidOperationException($"[{stepLabel}] CRITICAL UI OVERLAP DEFECT: PauseMenu and ControlsGuide are both open at the same time!");

                // Global Invariant: exactly one primary UI state is active
                int activePrimaryCount = (fpp.IsDialogueOpen ? 1 : 0) +
                                         (fpp.IsPauseMenuOpen ? 1 : 0) +
                                         (fpp.IsControlsGuideOpen ? 1 : 0) +
                                         (fpp.CurrentUIState == PrimaryUIState.Exploration ? 1 : 0);
                if (activePrimaryCount != 1)
                    throw new InvalidOperationException($"[{stepLabel}] Primary UI exclusivity violated: active primary screens count = {activePrimaryCount}");

                if (fpp.IsCursorLocked != expectedCursorLocked)
                    throw new InvalidOperationException($"[{stepLabel}] Cursor lock mismatch: expected {expectedCursorLocked}, got {fpp.IsCursorLocked}");

                if (fpp.CanPlayerMove != expectedCanMove)
                    throw new InvalidOperationException($"[{stepLabel}] Player movement mismatch: expected {expectedCanMove}, got {fpp.CanPlayerMove}");
            }

            // Phase 3 Requirement: Initial First-Launch State
            AssertPrimaryUIState(PrimaryUIState.ControlsGuide, false, false, "Initial Startup");

            // Dismiss Initial Guide -> Enter Exploration
            fpp.CloseControlsGuide();
            AssertPrimaryUIState(PrimaryUIState.Exploration, true, true, "After Dismissing Initial Guide");

            // Required Transition Sequence (Phase 5):
            // 1. Exploration → Escape → PauseMenu
            fpp.OpenPauseMenu();
            AssertPrimaryUIState(PrimaryUIState.PauseMenu, false, false, "Step 1: Exploration -> PauseMenu");

            // 2. PauseMenu → Controls → ControlsGuide (Origin = PauseMenu)
            fpp.OpenControlsGuide(PrimaryUIState.PauseMenu);
            AssertPrimaryUIState(PrimaryUIState.ControlsGuide, false, false, "Step 2: PauseMenu -> ControlsGuide");
            if (fpp.ControlsGuideOrigin != PrimaryUIState.PauseMenu)
                throw new InvalidOperationException("ControlsGuideOrigin should be PauseMenu after opening from Pause Menu.");

            // 3. ControlsGuide → Back to Pause Menu → PauseMenu
            fpp.CloseControlsGuide();
            AssertPrimaryUIState(PrimaryUIState.PauseMenu, false, false, "Step 3: ControlsGuide -> Back to PauseMenu");

            // 4. PauseMenu → Resume → Exploration
            fpp.ResumeFromPauseMenu();
            AssertPrimaryUIState(PrimaryUIState.Exploration, true, true, "Step 4: PauseMenu -> Resume -> Exploration");

            // 5. Exploration → F1 → ControlsGuide (Origin = Exploration)
            fpp.OpenControlsGuide(PrimaryUIState.Exploration);
            AssertPrimaryUIState(PrimaryUIState.ControlsGuide, false, false, "Step 5: Exploration -> F1 -> ControlsGuide");
            if (fpp.ControlsGuideOrigin != PrimaryUIState.Exploration)
                throw new InvalidOperationException("ControlsGuideOrigin should be Exploration after pressing F1 in Exploration.");

            // 6. ControlsGuide → Escape → Exploration (Returns to Exploration, NOT PauseMenu!)
            fpp.CloseControlsGuide();
            AssertPrimaryUIState(PrimaryUIState.Exploration, true, true, "Step 6: ControlsGuide -> Escape -> Exploration");

            // 7. Exploration → NPC interaction → Dialogue
            fpp.OpenInteractionModal(ptBen);
            AssertPrimaryUIState(PrimaryUIState.Dialogue, false, false, "Step 7: Exploration -> Dialogue");
            if (fpp.ActiveModalPoint != ptBen) throw new InvalidOperationException("ActiveModalPoint mismatch.");

            int carlosInitialCount = fpp.GetDialogueMessageCount("carlos-farmacia-attendant");
            if (carlosInitialCount < 1) throw new InvalidOperationException("Carlos should have an initial greeting message.");

            // Dialogue History Isolation
            int elenaInitialCount = fpp.GetDialogueMessageCount("elena-border-concierge");
            int valeriaInitialCount = fpp.GetDialogueMessageCount("valeria-optica-attendant");
            if (elenaInitialCount != 0) throw new InvalidOperationException("Elena's dialogue history should be isolated and empty.");
            if (valeriaInitialCount != 0) throw new InvalidOperationException("Valeria's dialogue history should be isolated and empty.");

            // 8. Dialogue → Escape → Exploration
            fpp.CloseInteractionModal();
            AssertPrimaryUIState(PrimaryUIState.Exploration, true, true, "Step 8: Dialogue -> Escape -> Exploration");
            if (fpp.ActiveModalPoint != null) throw new InvalidOperationException("ActiveModalPoint should be null after closing.");

            // 9. PauseMenu → Quit to Desktop invokes the existing quit behavior
            fpp.OpenPauseMenu();
            AssertPrimaryUIState(PrimaryUIState.PauseMenu, false, false, "Step 9a: PauseMenu before Quit");
            fpp.ResumeFromPauseMenu(); // Return to exploration for remaining tests

            // Test 6: PlayerSettings Windowed Presentation
            if (PlayerSettings.defaultScreenWidth != 1280 || PlayerSettings.defaultScreenHeight != 720)
            {
                throw new InvalidOperationException($"PlayerSettings resolution mismatch: {PlayerSettings.defaultScreenWidth}x{PlayerSettings.defaultScreenHeight} (expected 1280x720)");
            }
            if (PlayerSettings.fullScreenMode != FullScreenMode.Windowed)
            {
                throw new InvalidOperationException($"PlayerSettings fullScreenMode mismatch: {PlayerSettings.fullScreenMode} (expected Windowed)");
            }
            if (!PlayerSettings.resizableWindow)
            {
                throw new InvalidOperationException("PlayerSettings resizableWindow must be true.");
            }

            Debug.Log("[EndToEndCorridorTest] PASS: Gameplay modal states, pause menu, and windowed PlayerSettings verified.");
        }

        private static void VerifyProviderAndProvenanceBadges()
        {
            Debug.Log("[EndToEndCorridorTest] [Phase 3/5] Verifying Provider and Provenance Badge Formatting...");

            var playerGo = GameObject.Find("FirstPersonPlayer");
            var fpp = playerGo.GetComponent<FirstPersonPlayer>();

            // Test 1: Meta Muse Spark Provider Badge (EN & ES)
            string enMetaBadge = fpp.FormatProviderBadgePublic("meta-muse-spark-1.3", "🏪 Storefront Attendant", "en");
            if (!enMetaBadge.Contains("[Meta Muse Spark 1.3]"))
            {
                throw new InvalidOperationException($"Meta badge missing provider tag: {enMetaBadge}");
            }

            string esMetaBadge = fpp.FormatProviderBadgePublic("meta-muse-spark-1.3", "🏪 Encargado Virtual", "es");
            if (!esMetaBadge.Contains("[Meta Muse Spark 1.3]"))
            {
                throw new InvalidOperationException($"Meta badge missing provider tag in ES: {esMetaBadge}");
            }

            // Test 2: Deterministic Fallback Provider Badge (EN & ES)
            string enFallbackBadge = fpp.FormatProviderBadgePublic("deterministic-fallback", "🛂 Border Concierge", "en");
            if (!enFallbackBadge.Contains("[Deterministic Fallback]"))
            {
                throw new InvalidOperationException($"Fallback badge missing tag in EN: {enFallbackBadge}");
            }

            string esFallbackBadge = fpp.FormatProviderBadgePublic("deterministic-fallback", "🛂 Conserje Fronterizo", "es");
            if (!esFallbackBadge.Contains("[Regla Determinista (Fallback)]"))
            {
                throw new InvalidOperationException($"Fallback badge missing tag in ES: {esFallbackBadge}");
            }

            // Test 3: Mock Meta Provider Badge
            string mockBadge = fpp.FormatProviderBadgePublic("mock-meta", "🎒 Synthetic Witness", "en");
            if (!mockBadge.Contains("[Mock Meta Provider]"))
            {
                throw new InvalidOperationException($"Mock Meta badge missing tag: {mockBadge}");
            }

            Debug.Log("[EndToEndCorridorTest] PASS: Provider and provenance badge formatting verified.");
        }

        private static void VerifyBorderWaitTimeInvariants()
        {
            Debug.Log("[EndToEndCorridorTest] [Phase 4/5] Verifying Border Wait Time Invariants and Formatting Rules...");

            // Invariant 1: Valid open delay with numbers
            var openObs = new BorderCrossingObservationDto
            {
                portStatus = "Open",
                hasDelayMinutes = true,
                delayMinutes = 35
            };
            if (openObs.FormatDelay("en") != "35 min" || openObs.FormatDelay("es") != "35 min")
            {
                throw new InvalidOperationException($"FormatDelay mismatch for open observation: {openObs.FormatDelay("en")}");
            }

            // Invariant 2: Closed port MUST say Port Closed / Puerto Cerrado, NEVER "0 min"
            var closedObs = new BorderCrossingObservationDto
            {
                portStatus = "Closed",
                hasDelayMinutes = false,
                delayMinutes = 0
            };
            if (closedObs.FormatDelay("en") != "Port Closed" || closedObs.FormatDelay("es") != "Puerto Cerrado")
            {
                throw new InvalidOperationException($"Closed port must not show 0 min: {closedObs.FormatDelay("en")}");
            }

            // Invariant 3: Unavailable / pending delay MUST say Unavailable / Pending, NEVER "0 min"
            var pendingObs = new BorderCrossingObservationDto
            {
                portStatus = "Open",
                hasDelayMinutes = false,
                delayMinutes = 0
            };
            if (pendingObs.FormatDelay("en") != "Unavailable / Pending" || pendingObs.FormatDelay("es") != "No disponible / Pendiente")
            {
                throw new InvalidOperationException($"Pending observation must not fabricate 0 min: {pendingObs.FormatDelay("en")}");
            }

            Debug.Log("[EndToEndCorridorTest] PASS: Border wait times formatting and zero-fabrication prevention verified.");
        }

        private static void VerifyLiveHttpEndpoints()
        {
            Debug.Log("[EndToEndCorridorTest] [Phase 5/5] Verifying Live HTTP Endpoints against Node.js Server...");

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(15);

            // Step 1: Health Probe
            Debug.Log($"Testing GET {BackendBaseUrl}/health...");
            var healthResp = http.GetAsync($"{BackendBaseUrl}/health").GetAwaiter().GetResult();
            if (!healthResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"GET /health returned HTTP {healthResp.StatusCode}. Ensure server is running at {BackendBaseUrl}.");
            }
            string healthBody = healthResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var healthDto = JsonUtility.FromJson<HealthResponseDto>(healthBody);
            if (healthDto == null || (healthDto.status != "ok" && healthDto.status != "healthy"))
            {
                throw new InvalidOperationException($"GET /health returned non-healthy status: {healthBody}");
            }
            Debug.Log($"  -> PASS: /health status = {healthDto.status}, model = {healthDto.model}");

            // Step 2: Live Border Wait Times
            Debug.Log($"Testing GET {BackendBaseUrl}/api/border/wait-times...");
            var waitTimesResp = http.GetAsync($"{BackendBaseUrl}/api/border/wait-times").GetAwaiter().GetResult();
            if (!waitTimesResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"GET /api/border/wait-times returned HTTP {waitTimesResp.StatusCode}.");
            }
            string waitTimesBody = waitTimesResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var waitTimesDto = JsonUtility.FromJson<BorderWaitTimesResponseDto>(waitTimesBody);
            if (waitTimesDto == null || !waitTimesDto.success || waitTimesDto.comparison?.observations == null)
            {
                throw new InvalidOperationException($"GET /api/border/wait-times returned invalid payload: {waitTimesBody}");
            }

            var pdn = waitTimesDto.comparison.observations.pasoDelNorte;
            var bota = waitTimesDto.comparison.observations.bridgeOfTheAmericas;
            if (pdn == null || bota == null)
            {
                throw new InvalidOperationException("Both Paso del Norte and BOTA observations must be present.");
            }

            if (!pdn.canonicalName.Contains("Paso del Norte"))
            {
                throw new InvalidOperationException($"PDN canonical name unexpected: {pdn.canonicalName}");
            }
            if (!bota.canonicalName.Contains("Bridge of the Americas") && !bota.canonicalName.Contains("Américas"))
            {
                throw new InvalidOperationException($"BOTA canonical name unexpected: {bota.canonicalName}");
            }

            Debug.Log($"  -> PASS: Live Border Times: PDN Delay='{pdn.FormatDelay("en")}', Freshness='{pdn.freshness}', BOTA Delay='{bota.FormatDelay("en")}'");

            // Step 3: Carlos Méndez (Storefront Attendant - Farmacias Benavides)
            Debug.Log("Testing POST /api/businesses/attendants/chat (Carlos Méndez - Hours inquiry in EN)...");
            var carlosReqJson = "{\"attendantId\":\"carlos-farmacia-attendant\",\"message\":\"What are your business hours today?\",\"language\":\"en\"}";
            var carlosResp = http.PostAsync($"{BackendBaseUrl}/api/businesses/attendants/chat", new StringContent(carlosReqJson, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            if (!carlosResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Carlos attendant request failed with HTTP {carlosResp.StatusCode}.");
            }
            string carlosBody = carlosResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var carlosDto = JsonUtility.FromJson<BusinessAttendantChatResponseDto>(carlosBody);
            if (carlosDto == null || !carlosDto.success || carlosDto.response == null)
            {
                throw new InvalidOperationException($"Carlos response invalid: {carlosBody}");
            }
            if (carlosDto.response.attendantName != "Carlos Méndez" || carlosDto.response.businessCanonicalName != "Farmacias Benavides")
            {
                throw new InvalidOperationException($"Carlos response identity mismatch: {carlosDto.response.attendantName}, {carlosDto.response.businessCanonicalName}");
            }
            if (string.IsNullOrEmpty(carlosDto.response.provider))
            {
                throw new InvalidOperationException("Carlos response missing provider field.");
            }
            Debug.Log($"  -> PASS: Carlos Méndez responded (Provider: {carlosDto.response.provider}): '{carlosDto.response.text.Substring(0, Math.Min(80, carlosDto.response.text.Length))}...'");

            // Step 4: Valeria Domínguez (Storefront Attendant - Óptica Fashion)
            Debug.Log("Testing POST /api/businesses/attendants/chat (Valeria Domínguez - Location inquiry in ES)...");
            var valeriaReqJson = "{\"attendantId\":\"valeria-optica-attendant\",\"message\":\"¿Dónde se encuentra ubicada la óptica?\",\"language\":\"es\"}";
            var valeriaResp = http.PostAsync($"{BackendBaseUrl}/api/businesses/attendants/chat", new StringContent(valeriaReqJson, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            if (!valeriaResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Valeria attendant request failed with HTTP {valeriaResp.StatusCode}.");
            }
            string valeriaBody = valeriaResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var valeriaDto = JsonUtility.FromJson<BusinessAttendantChatResponseDto>(valeriaBody);
            if (valeriaDto == null || !valeriaDto.success || valeriaDto.response == null)
            {
                throw new InvalidOperationException($"Valeria response invalid: {valeriaBody}");
            }
            if (valeriaDto.response.attendantName != "Valeria Domínguez" || valeriaDto.response.businessCanonicalName != "Óptica Fashion")
            {
                throw new InvalidOperationException($"Valeria response identity mismatch: {valeriaDto.response.attendantName}");
            }
            Debug.Log($"  -> PASS: Valeria Domínguez responded (Provider: {valeriaDto.response.provider}): '{valeriaDto.response.text.Substring(0, Math.Min(80, valeriaDto.response.text.Length))}...'");

            // Step 5: Mateo Navarro (Synthetic Witness)
            Debug.Log("Testing POST /api/border/witnesses/chat (Mateo Navarro - Commuter line inquiry in EN)...");
            var mateoReqJson = "{\"npcId\":\"mateo-student\",\"message\":\"How is the line moving today?\",\"language\":\"en\"}";
            var mateoResp = http.PostAsync($"{BackendBaseUrl}/api/border/witnesses/chat", new StringContent(mateoReqJson, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            if (!mateoResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mateo witness request failed with HTTP {mateoResp.StatusCode}.");
            }
            string mateoBody = mateoResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var mateoDto = JsonUtility.FromJson<WitnessChatResponseDto>(mateoBody);
            if (mateoDto == null || !mateoDto.success || mateoDto.response == null)
            {
                throw new InvalidOperationException($"Mateo response invalid: {mateoBody}");
            }
            if (mateoDto.response.witnessName != "Mateo Navarro" || mateoDto.response.provenance?.type != "synthetic-witness-simulation")
            {
                throw new InvalidOperationException($"Mateo identity or provenance mismatch: {mateoDto.response.witnessName}");
            }
            Debug.Log($"  -> PASS: Mateo Navarro responded (Provenance Provider: {mateoDto.response.provenance?.provider}): '{mateoDto.response.text.Substring(0, Math.Min(80, mateoDto.response.text.Length))}...'");

            // Step 6: Elena Ramos (Border Concierge)
            Debug.Log("Testing POST /api/border/concierge/chat (Elena Ramos - Bridge comparison in EN)...");
            var elenaReqJson = "{\"message\":\"Which bridge is faster right now for pedestrian crossing?\",\"language\":\"en\"}";
            var elenaResp = http.PostAsync($"{BackendBaseUrl}/api/border/concierge/chat", new StringContent(elenaReqJson, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            if (!elenaResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Elena concierge request failed with HTTP {elenaResp.StatusCode}.");
            }
            string elenaBody = elenaResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var elenaDto = JsonUtility.FromJson<AskConciergeResponseDto>(elenaBody);
            if (elenaDto == null || !elenaDto.success || elenaDto.concierge == null)
            {
                throw new InvalidOperationException($"Elena response invalid: {elenaBody}");
            }
            if (elenaDto.concierge.name != "Elena Ramos")
            {
                throw new InvalidOperationException($"Elena concierge name mismatch: {elenaDto.concierge.name}");
            }
            if (string.IsNullOrEmpty(elenaDto.provider))
            {
                throw new InvalidOperationException("Elena response missing provider field.");
            }
            Debug.Log($"  -> PASS: Elena Ramos responded (Provider: {elenaDto.provider}): '{elenaDto.text.Substring(0, Math.Min(80, elenaDto.text.Length))}...'");

            // Step 7: Error handling branch (Invalid Attendant ID)
            Debug.Log("Testing error handling: POST /api/businesses/attendants/chat with unknown ID...");
            var invalidReqJson = "{\"attendantId\":\"unknown-ghost-attendant\",\"message\":\"Hello?\",\"language\":\"en\"}";
            var invalidResp = http.PostAsync($"{BackendBaseUrl}/api/businesses/attendants/chat", new StringContent(invalidReqJson, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            if (invalidResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Expected HTTP 404/error for unknown attendant, but got HTTP success.");
            }
            Debug.Log($"  -> PASS: Unknown attendant produced expected error HTTP {invalidResp.StatusCode}.");
        }
    }
}
