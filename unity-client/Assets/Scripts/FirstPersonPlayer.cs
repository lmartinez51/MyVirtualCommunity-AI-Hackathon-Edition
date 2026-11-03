using System;
using System.Collections.Generic;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.Util;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Authoritative mutually exclusive primary UI states.
    /// Invariant: Exactly one primary UI state is active at any time.
    /// </summary>
    public enum PrimaryUIState
    {
        Exploration,
        ControlsGuide,
        PauseMenu,
        Dialogue
    }

    /// <summary>
    /// Represents an in-dialogue message with provenance metadata.
    /// </summary>
    public class DialogueMessage
    {
        public bool isUser;
        public string senderName;
        public string avatar;
        public string text;
        public string provenanceBadge;
        public string timestamp;
    }

    /// <summary>
    /// Smooth, reliable First-Person exploration controller and conversational NPC dialogue manager.
    /// Connects the 3D Avenida Benito Juárez corridor to the authoritative Node.js backend:
    /// - Live Border Intelligence (GET /api/border/wait-times)
    /// - Elena Ramos Border Concierge (POST /api/border/concierge/chat)
    /// - Synthetic Community Witnesses (POST /api/border/witnesses/chat)
    /// - Grounded Storefront Attendants (POST /api/businesses/attendants/chat)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float gravity = 18.0f;

        [Header("Mouse Look")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float upDownLookLimit = 80.0f;

        [Header("Interactions")]
        [SerializeField] private float maxInteractionDistance = 5.0f;

        [Header("API Client")]
        [SerializeField] private ApiClient apiClient;

        [Header("Localization")]
        [SerializeField] private string currentLanguage = "en"; // "en" or "es"

        private CharacterController characterController;
        private float verticalVelocity = 0f;
        private float verticalRotation = 0f;
        private bool isCursorLocked = false;

        private InteractionPoint currentNearbyPoint = null;
        private InteractionPoint activeModalPoint = null;

        // UI & State Machine (Authoritative mutually exclusive primary states)
        private PrimaryUIState currentState = PrimaryUIState.ControlsGuide; // Active on first launch
        private PrimaryUIState controlsGuideOrigin = PrimaryUIState.Exploration;

        // Backend Diagnostics
        private string serverStatusText = "Checking server...";
        private Color serverStatusColor = Color.yellow;

        // Dialogue State per Character (Isolated histories)
        private readonly Dictionary<string, List<DialogueMessage>> dialogueHistories = new Dictionary<string, List<DialogueMessage>>();
        private string currentInputText = "";
        private bool isRequestPending = false;
        private string requestErrorMessage = null;
        private string lastFailedMessage = null;
        private Vector2 dialogueScrollPosition = Vector2.zero;

        // Live Border Intelligence Cache (Elena's Kiosk)
        private BorderWaitTimesResponseDto liveBorderWaitTimes = null;
        private bool isBorderWaitTimesLoading = false;
        private string borderWaitTimesError = null;
        private string selectedCrossingIntent = null; // Session-local: "paso-del-norte" | "bridge-of-the-americas" | null

        public string SelectedCrossingIntent => selectedCrossingIntent;
        public void SetSelectedCrossingIntent(string intent) => selectedCrossingIntent = intent;

        // Citizen Issue Reporting State (Increment 5.11 & 5.11.3 Generalized Municipal Scope)
        public enum CitizenReportWorkflowStep
        {
            Consultation,
            FormInput,
            Analyzing,
            DraftReview,
            ReceiptSuccess
        }

        private CitizenReportWorkflowStep citizenReportStep = CitizenReportWorkflowStep.Consultation;
        private string reportDescriptionInput = "";
        private string reportLocationInput = "Avenida Benito Juárez (sector virtual)";
        private int selectedCategoryIndex = 0;
        private bool externalAiConsent = true;
        private string selectedImagePath = null;
        private byte[] selectedImageBytes = null;
        private Texture2D selectedImagePreview = null;
        private string selectedImageBase64 = null;
        private string selectedImageMime = null;
        private string selectedImageFileName = null;
        private CitizenReportDraftDataDto currentReportDraft = null;
        private CitizenReportReceiptDto currentReportReceipt = null;
        private string citizenReportErrorMessage = null;

        // Increment 5.11.4: Form Layout & Bounded Scroll Region (Objective B)
        private Vector2 citizenReportFormScrollPos = Vector2.zero;
        private float currentModalInnerHeight = 600f;

        // Increment 5.11.4: Visual Evidence Inspection & SAM Overlay (Objective A)
        private bool isEnlargedPhotoViewerOpen = false;
        private int photoViewerDisplayMode = 2; // 0: Original Photo, 1: Segmentation Mask Only, 2: Combined (Photo + Mask)
        private bool showBoundingBoxesOverlay = true;
        private Texture2D segmentationOverlayTexture = null;
        private string loadedOverlayBase64 = null;

        private static readonly string[] CategoryIds = new string[]
        {
            "road-surface-damage",
            "sidewalk-obstruction",
            "cleaning-and-waste",
            "water-and-drainage",
            "public-lighting",
            "traffic-signage",
            "parks-and-public-spaces",
            "trees-and-green-areas",
            "other"
        };

        private static readonly string[] CategoryNamesEs = new string[]
        {
            "Bacheo y Asfalto",
            "Banquetas / Paso",
            "Limpia y Basura",
            "Agua / Drenaje",
            "Alumbrado Público",
            "Señalización Vial",
            "Parques y Plazas",
            "Arbolado Urbano",
            "Otro Asunto Cívico"
        };

        private static readonly string[] CategoryNamesEn = new string[]
        {
            "Roads & Potholes",
            "Sidewalks & Access",
            "Waste & Cleaning",
            "Water & Drainage",
            "Public Lighting",
            "Traffic Signs",
            "Parks & Plazas",
            "Urban Trees",
            "Other Civic Issue"
        };

        public string CurrentLanguage
        {
            get => currentLanguage;
            set => currentLanguage = (value == "es") ? "es" : "en";
        }

        public PrimaryUIState CurrentUIState => currentState;
        public PrimaryUIState ControlsGuideOrigin => controlsGuideOrigin;
        public bool IsDialogueOpen => currentState == PrimaryUIState.Dialogue;
        public bool IsPauseMenuOpen => currentState == PrimaryUIState.PauseMenu;
        public bool IsControlsGuideOpen => currentState == PrimaryUIState.ControlsGuide;
        public bool IsCursorLocked => isCursorLocked;
        public bool CanPlayerMove => currentState == PrimaryUIState.Exploration;

        public bool ShowControlsOverlay
        {
            get => currentState == PrimaryUIState.ControlsGuide;
            set
            {
                if (value)
                {
                    if (currentState != PrimaryUIState.ControlsGuide)
                    {
                        OpenControlsGuide(currentState == PrimaryUIState.PauseMenu ? PrimaryUIState.PauseMenu : PrimaryUIState.Exploration);
                    }
                }
                else
                {
                    if (currentState == PrimaryUIState.ControlsGuide)
                    {
                        CloseControlsGuide();
                    }
                }
            }
        }

        public InteractionPoint ActiveModalPoint => activeModalPoint;
        public int GetDialogueMessageCount(string characterId) => dialogueHistories.ContainsKey(characterId) ? dialogueHistories[characterId].Count : 0;
        public DialogueMessage GetLastMessage(string characterId) => (dialogueHistories.ContainsKey(characterId) && dialogueHistories[characterId].Count > 0) ? dialogueHistories[characterId][dialogueHistories[characterId].Count - 1] : null;
        public string RequestErrorMessage => requestErrorMessage;
        public bool IsRequestPending => isRequestPending;
        public string FormatProviderBadgePublic(string provider, string categoryBadge, string language) => FormatProviderBadge(provider, categoryBadge, language);

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
            if (Screen.fullScreenMode != FullScreenMode.Windowed && !PlayerPrefs.HasKey("mvc_window_initialized"))
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                PlayerPrefs.SetInt("mvc_window_initialized", 1);
                PlayerPrefs.Save();
            }
#endif
            // If starting with ControlsGuide open, keep cursor unlocked; otherwise lock for exploration
            if (currentState == PrimaryUIState.ControlsGuide)
            {
                LockCursor(false);
            }
            else
            {
                LockCursor(true);
            }
            CheckBackendConnection();
        }

        private void Update()
        {
            switch (currentState)
            {
                case PrimaryUIState.Dialogue:
                    if (WasEscapePressed())
                    {
                        if (isEnlargedPhotoViewerOpen)
                        {
                            CloseEnlargedImageViewer();
                        }
                        else
                        {
                            CloseInteractionModal();
                        }
                    }
                    return;

                case PrimaryUIState.PauseMenu:
                    if (WasEscapePressed())
                    {
                        ResumeFromPauseMenu();
                        return;
                    }
                    if (WasF1Pressed())
                    {
                        OpenControlsGuide(PrimaryUIState.PauseMenu);
                        return;
                    }
                    return;

                case PrimaryUIState.ControlsGuide:
                    if (WasEscapePressed() || WasF1Pressed())
                    {
                        CloseControlsGuide();
                        return;
                    }
                    return;

                case PrimaryUIState.Exploration:
                default:
                    if (WasEscapePressed())
                    {
                        OpenPauseMenu();
                        return;
                    }
                    if (WasF1Pressed())
                    {
                        OpenControlsGuide(PrimaryUIState.Exploration);
                        return;
                    }

                    // Re-capture cursor if user clicked into game view while unlocked
                    if (!isCursorLocked && WasMouseClickPressed())
                    {
                        LockCursor(true);
                    }

                    HandleLook();
                    HandleMovement();
                    HandleInteractionProximity();

                    if (currentNearbyPoint != null && WasInteractPressed())
                    {
                        OpenInteractionModal(currentNearbyPoint);
                    }
                    break;
            }
        }

        #region Movement & Exploration

        private void HandleMovement()
        {
            Vector2 input = GetMovementInput();
            bool isSprinting = IsSprintPressed();
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            if (moveDirection.sqrMagnitude > 1.0f)
            {
                moveDirection.Normalize();
            }

            if (characterController.isGrounded)
            {
                verticalVelocity = -1.5f; // Snap to ground and curbs
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 finalVelocity = (moveDirection * currentSpeed) + (Vector3.up * verticalVelocity);
            characterController.Move(finalVelocity * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (!isCursorLocked) return;

            Vector2 look = GetLookInput();

            // Horizontal rotation (player body)
            transform.Rotate(0f, look.x * mouseSensitivity, 0f);

            // Vertical rotation (camera pitch)
            if (playerCamera != null)
            {
                verticalRotation -= look.y * mouseSensitivity;
                verticalRotation = Mathf.Clamp(verticalRotation, -upDownLookLimit, upDownLookLimit);
                playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        private void HandleInteractionProximity()
        {
            InteractionPoint[] points = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            InteractionPoint nearest = null;
            float nearestDist = float.MaxValue;

            Vector3 playerPos = transform.position;

            foreach (var point in points)
            {
                if (point == null) continue;
                float dist = Vector3.Distance(point.transform.position, playerPos);
                float radius = Mathf.Max(point.interactionRadius, maxInteractionDistance);

                if (dist <= radius && dist < nearestDist)
                {
                    nearest = point;
                    nearestDist = dist;
                }
            }

            if (currentNearbyPoint != nearest)
            {
                if (currentNearbyPoint != null) currentNearbyPoint.SetHighlighted(false);
                currentNearbyPoint = nearest;
                if (currentNearbyPoint != null) currentNearbyPoint.SetHighlighted(true);
            }
        }

        #endregion

        #region Modal & Dialogue Flow

        public void OpenInteractionModal(InteractionPoint point)
        {
            if (currentState == PrimaryUIState.PauseMenu || currentState == PrimaryUIState.ControlsGuide) return;

            activeModalPoint = point;
            currentState = PrimaryUIState.Dialogue;
            requestErrorMessage = null;
            currentInputText = "";
            LockCursor(false);

            EnsureInitialGreeting(point);

            // If opening Elena's Kiosk, fetch fresh official wait times
            if (point.category == InteractionCategory.BorderConcierge)
            {
                FetchLiveBorderWaitTimes(false);
            }
            else if (point.category == InteractionCategory.CitizenService)
            {
                if (citizenReportStep == CitizenReportWorkflowStep.ReceiptSuccess)
                {
                    citizenReportStep = CitizenReportWorkflowStep.Consultation;
                }
            }
        }

        public void CloseInteractionModal()
        {
            activeModalPoint = null;
            currentState = PrimaryUIState.Exploration;
            requestErrorMessage = null;
            currentInputText = "";
            CloseEnlargedImageViewer();
            DestroyOverlayTexture();
            LockCursor(true);
        }

        private void CloseEnlargedImageViewer()
        {
            isEnlargedPhotoViewerOpen = false;
        }

        private void DestroyOverlayTexture()
        {
            if (segmentationOverlayTexture != null)
            {
                Destroy(segmentationOverlayTexture);
                segmentationOverlayTexture = null;
                loadedOverlayBase64 = null;
            }
        }

        private void EnsureInitialGreeting(InteractionPoint point)
        {
            if (point == null || string.IsNullOrEmpty(point.characterId)) return;

            string key = point.characterId;
            if (!dialogueHistories.ContainsKey(key))
            {
                dialogueHistories[key] = new List<DialogueMessage>();
            }

            var history = dialogueHistories[key];
            if (history.Count == 0)
            {
                string greeting = GetDefaultGreeting(point);
                string badge = GetProvenanceBadge(point);

                history.Add(new DialogueMessage
                {
                    isUser = false,
                    senderName = !string.IsNullOrEmpty(point.characterName) ? point.characterName : point.title,
                    avatar = point.avatar,
                    text = greeting,
                    provenanceBadge = badge,
                    timestamp = DateTime.Now.ToString("HH:mm")
                });
            }
        }

        private string GetDefaultGreeting(InteractionPoint point)
        {
            bool isEs = currentLanguage == "es";

            switch (point.category)
            {
                case InteractionCategory.BorderConcierge:
                    return isEs
                        ? "¡Hola! Soy Elena Ramos, conserje virtual del cruce Paso del Norte. Le asisto con tiempos oficiales de espera peatonal del CBP, comparaciones con el puente Córdova-Américas y horarios de cruce. ¿Hacia dónde se dirige hoy?"
                        : "Hello! I'm Elena Ramos, virtual concierge at the Paso del Norte crossing. I provide grounded guidance on official CBP pedestrian wait times, comparisons with Bridge of the Americas (BOTA), and operational hours. Where are you heading today?";

                case InteractionCategory.SyntheticWitness:
                    return isEs
                        ? $"Hola, soy {point.characterName}. Estoy aquí en la fila peatonal esperando cruzar el puente hacia El Paso. Puedo contarte sobre mi experiencia en la fila y mis razones de cruce."
                        : $"Hi, I'm {point.characterName}. I'm waiting in the pedestrian queue to cross into El Paso. I can share my experience in line and my regular travel routine.";

                case InteractionCategory.StorefrontAttendant:
                    return isEs
                        ? $"¡Hola! Soy {point.characterName}, encargado virtual de {point.title}. Le puedo orientar con nuestros horarios verificados de atención y la ubicación exacta del establecimiento en Avenida Juárez."
                        : $"Hello! I'm {point.characterName}, virtual attendant at {point.title}. I'm here to assist you with our confirmed business hours and store location on Avenida Juárez.";

                case InteractionCategory.CitizenService:
                    return isEs
                        ? "¡Buen día! Soy la Lic. Patricia Morales, enlace de atención ciudadana municipal. Le asisto como canal de recepción y canalización de reportes sobre cualquier sector de Ciudad Juárez (bacheo, banquetas, limpia y residuos, fugas de agua, alumbrado, semáforos, parques, arbolado y más). Puede formular sus inquietudes o hacer clic en 'Registrar Reporte Ciudadano' para iniciar su trámite (la fotografía es opcional)."
                        : "Good day! I'm Lic. Patricia Morales, municipal citizen service representative. I serve as your intake channel for issues across Ciudad Juárez (potholes, sidewalks, sanitation & waste, water leaks, lighting, signals, parks, urban trees, and more). You can ask questions or click 'Register Citizen Report' to submit your report (photo is optional).";

                default:
                    return point.description;
            }
        }

        private string GetProvenanceBadge(InteractionPoint point)
        {
            bool isEs = currentLanguage == "es";

            switch (point.category)
            {
                case InteractionCategory.BorderConcierge:
                    return isEs
                        ? "🛂 Conserje Fronterizo Virtual · Basado en datos oficiales CBP"
                        : "🛂 AI Border Concierge · Grounded in official CBP observations";

                case InteractionCategory.SyntheticWitness:
                    return isEs
                        ? "🎒 Testigo Comunitario Sintético · Simulación de escenario local"
                        : "🎒 Synthetic Community Witness · Simulated local scenario";

                case InteractionCategory.StorefrontAttendant:
                    return isEs
                        ? "🏪 Encargado Virtual · Basado en horarios verificados de World Core"
                        : "🏪 AI Storefront Attendant · Grounded in verified World Core snapshot";

                case InteractionCategory.CitizenService:
                    return isEs
                        ? "📋 Enlace de Atención Ciudadana · Servicio Municipal Simulado (Demostración)"
                        : "📋 Citizen Service Representative · Simulated Municipal Demonstration";

                default:
                    return isEs ? "📍 Información del Lugar" : "📍 Landmark Information";
            }
        }

        #endregion

        #region API Integrations & Actions

        public void SendUserMessage()
        {
            if (activeModalPoint == null || isRequestPending) return;

            string msg = currentInputText?.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            string charKey = activeModalPoint.characterId;
            if (!dialogueHistories.ContainsKey(charKey))
            {
                dialogueHistories[charKey] = new List<DialogueMessage>();
            }

            // Record user message
            dialogueHistories[charKey].Add(new DialogueMessage
            {
                isUser = true,
                senderName = (currentLanguage == "es") ? "Tú" : "You",
                avatar = "👤",
                text = msg,
                provenanceBadge = (currentLanguage == "es") ? "Consulta del Usuario" : "Player Inquiry",
                timestamp = DateTime.Now.ToString("HH:mm")
            });

            currentInputText = "";
            requestErrorMessage = null;
            lastFailedMessage = msg;
            isRequestPending = true;

            // Auto-scroll to bottom
            dialogueScrollPosition.y = float.MaxValue;

            EnsureApiClient();
            if (apiClient == null)
            {
                HandleApiError("ApiClient component is missing from scene.");
                return;
            }

            switch (activeModalPoint.category)
            {
                case InteractionCategory.BorderConcierge:
                    ExecuteConciergeChat(msg, charKey);
                    break;

                case InteractionCategory.SyntheticWitness:
                    ExecuteWitnessChat(msg, charKey);
                    break;

                case InteractionCategory.StorefrontAttendant:
                    ExecuteAttendantChat(msg, charKey);
                    break;

                case InteractionCategory.CitizenService:
                    ExecuteCitizenServiceChat(msg, charKey);
                    break;

                default:
                    isRequestPending = false;
                    break;
            }
        }

        private string FormatProviderBadge(string provider, string categoryBadge, string language)
        {
            bool isEs = string.Equals(language, "es", StringComparison.OrdinalIgnoreCase);
            string providerLabel;
            if (string.Equals(provider, "meta-muse-spark-1.3", StringComparison.OrdinalIgnoreCase))
            {
                providerLabel = "Meta Muse Spark 1.3";
            }
            else if (string.Equals(provider, "deterministic-fallback", StringComparison.OrdinalIgnoreCase))
            {
                providerLabel = isEs ? "Regla Determinista (Fallback)" : "Deterministic Fallback";
            }
            else if (string.Equals(provider, "mock-meta", StringComparison.OrdinalIgnoreCase))
            {
                providerLabel = isEs ? "Mock Meta" : "Mock Meta Provider";
            }
            else if (!string.IsNullOrEmpty(provider))
            {
                providerLabel = provider;
            }
            else
            {
                providerLabel = isEs ? "Servicio Local" : "Local Service";
            }

            return $"{categoryBadge} [{providerLabel}]";
        }

        private void ExecuteConciergeChat(string message, string expectedCharKey)
        {
            var req = new AskConciergeRequestDto
            {
                message = message,
                language = currentLanguage
            };

            apiClient.AskConcierge(req,
                response =>
                {
                    isRequestPending = false;
                    if (response == null || !response.success)
                    {
                        HandleApiError(response?.error?.message ?? "Concierge returned empty or unsuccessful response.");
                        return;
                    }

                    // Append character response to the correct isolated thread
                    if (!dialogueHistories.ContainsKey(expectedCharKey))
                    {
                        dialogueHistories[expectedCharKey] = new List<DialogueMessage>();
                    }

                    string baseBadge = (currentLanguage == "es")
                        ? "🛂 Conserje Fronterizo Virtual · Basado en observaciones oficiales CBP"
                        : "🛂 AI Border Concierge · Grounded in official CBP observations";
                    string badge = FormatProviderBadge(response.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.concierge?.name ?? "Elena Ramos",
                        avatar = response.concierge?.avatar ?? "🛂",
                        text = response.text,
                        provenanceBadge = badge,
                        timestamp = DateTime.Now.ToString("HH:mm")
                    });

                    dialogueScrollPosition.y = float.MaxValue;
                },
                error => HandleApiError(error)
            );
        }

        private void ExecuteCitizenServiceChat(string message, string expectedCharKey)
        {
            isRequestPending = false;
            bool isEs = currentLanguage == "es";

            string replyText;
            string lower = message.ToLower();
            if (lower.Contains("bache") || lower.Contains("pothole") || lower.Contains("report") || lower.Contains("incidencia") || lower.Contains("daño"))
            {
                replyText = isEs
                    ? $"He tomado nota de su consulta sobre: \"{message}\". Para canalizar formalmente esta incidencia simulada a la Dirección de Obras Públicas y procesar su fotografía con segmentación SAM 3.1, seleccione la pestaña '📝 Reportar Incidencia con Foto' arriba."
                    : $"I've noted your inquiry regarding: \"{message}\". To formally route this simulated issue to Public Works and process your photograph with SAM 3.1 visual segmentation, please select the '📝 Report Issue with Photo' tab above.";
            }
            else if (lower.Contains("cruce") || lower.Contains("border") || lower.Contains("puente") || lower.Contains("bridge") || lower.Contains("espera") || lower.Contains("wait"))
            {
                replyText = isEs
                    ? "Para tiempos oficiales de espera del cruce peatonal y orientación de viaje a El Paso, le sugiero consultar con Elena Ramos en el Kiosko de Información Fronteriza unos metros adelante hacia el norte."
                    : "For official pedestrian border wait times and travel guidance to El Paso, please consult with Elena Ramos at the Border Information Kiosk just a few meters ahead to the north.";
            }
            else
            {
                replyText = isEs
                    ? "Como Enlace de Atención Ciudadana para el corredor Avenida Benito Juárez, recibo reportes simulados sobre baches, banquetas y alumbrado para su canalización. Puede describir la afectación y adjuntar una fotografía del escritorio para su análisis con SAM 3.1 y Muse Spark."
                    : "As Citizen Service Representative for the Avenida Benito Juárez corridor, I receive simulated reports on potholes, sidewalks, and lighting. You can describe the issue and attach a desktop photo for analysis with SAM 3.1 and Muse Spark.";
            }

            if (!dialogueHistories.ContainsKey(expectedCharKey))
            {
                dialogueHistories[expectedCharKey] = new List<DialogueMessage>();
            }

            string baseBadge = isEs
                ? "📋 Enlace de Atención Ciudadana · Servicio Municipal Simulado"
                : "📋 Citizen Service Representative · Simulated Municipal Service";

            dialogueHistories[expectedCharKey].Add(new DialogueMessage
            {
                isUser = false,
                senderName = "Lic. Patricia Morales",
                avatar = "📋",
                text = replyText,
                provenanceBadge = $"{baseBadge} [Meta Muse Spark 1.3]",
                timestamp = DateTime.Now.ToString("HH:mm")
            });

            dialogueScrollPosition.y = float.MaxValue;
        }

        private void ExecuteWitnessChat(string message, string expectedCharKey)
        {
            var req = new WitnessChatRequestDto
            {
                npcId = expectedCharKey,
                message = message,
                language = currentLanguage
            };

            apiClient.ChatWithWitness(req,
                response =>
                {
                    isRequestPending = false;
                    if (response == null || !response.success || response.response == null)
                    {
                        HandleApiError(response?.error?.message ?? "Witness returned empty response.");
                        return;
                    }

                    if (!dialogueHistories.ContainsKey(expectedCharKey))
                    {
                        dialogueHistories[expectedCharKey] = new List<DialogueMessage>();
                    }

                    string baseBadge = (currentLanguage == "es")
                        ? "🎒 Perspectiva de testigo simulado ficticio — no es una medición oficial"
                        : "🎒 Fictional simulated witness perspective — not an official measurement";
                    string badge = FormatProviderBadge(response.response.provenance?.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.response.witnessName ?? expectedCharKey,
                        avatar = response.response.avatar ?? "🎒",
                        text = response.response.text,
                        provenanceBadge = badge,
                        timestamp = DateTime.Now.ToString("HH:mm")
                    });

                    dialogueScrollPosition.y = float.MaxValue;
                },
                error => HandleApiError(error)
            );
        }

        private void ExecuteAttendantChat(string message, string expectedCharKey)
        {
            var req = new BusinessAttendantChatRequestDto
            {
                attendantId = expectedCharKey,
                message = message,
                language = currentLanguage
            };

            apiClient.ChatWithAttendant(req,
                response =>
                {
                    isRequestPending = false;
                    if (response == null || !response.success || response.response == null)
                    {
                        HandleApiError(response?.error?.message ?? "Storefront attendant returned empty response.");
                        return;
                    }

                    if (!dialogueHistories.ContainsKey(expectedCharKey))
                    {
                        dialogueHistories[expectedCharKey] = new List<DialogueMessage>();
                    }

                    string baseBadge = (currentLanguage == "es")
                        ? "🏪 Encargado Virtual · Basado en horarios verificados de World Core (Rechaza recetas/inventario)"
                        : "🏪 AI Storefront Attendant · Grounded in verified World Core snapshot (Refuses medical/inventory)";
                    string badge = FormatProviderBadge(response.response.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.response.attendantName ?? expectedCharKey,
                        avatar = activeModalPoint != null ? activeModalPoint.avatar : "🏪",
                        text = response.response.text,
                        provenanceBadge = badge,
                        timestamp = DateTime.Now.ToString("HH:mm")
                    });

                    dialogueScrollPosition.y = float.MaxValue;
                },
                error => HandleApiError(error)
            );
        }

        private void HandleApiError(string error)
        {
            isRequestPending = false;
            bool isEs = currentLanguage == "es";

            if (string.IsNullOrEmpty(error))
            {
                requestErrorMessage = isEs
                    ? "Error de comunicación con el servicio de atención."
                    : "Attendant service communication error.";
                return;
            }

            string lower = error.ToLowerInvariant();
            if (lower.Contains("502") || lower.Contains("badgateway") || lower.Contains("bad gateway") ||
                lower.Contains("unavailable") || lower.Contains("business-source-unavailable") || lower.Contains("world-core-unavailable"))
            {
                requestErrorMessage = isEs
                    ? "El servicio de atención está temporalmente no disponible. Por favor, reintenta en un momento."
                    : "The attendant service is temporarily unavailable. Please try again in a moment.";
            }
            else if (lower.Contains("connection failed") || lower.Contains("refused") || lower.Contains("timeout"))
            {
                requestErrorMessage = isEs
                    ? "No se pudo conectar con el servidor local. Verifica que el servidor de IA esté en ejecución."
                    : "Could not connect to the local server. Ensure the AI backend is running.";
            }
            else if (lower.Contains("404") || lower.Contains("not found") || lower.Contains("attendant-not-found"))
            {
                requestErrorMessage = isEs
                    ? "El encargado comercial solicitado no se encuentra registrado."
                    : "The requested attendant is not registered.";
            }
            else if (lower.Contains("400") || lower.Contains("invalid"))
            {
                requestErrorMessage = isEs
                    ? "No se pudo procesar la consulta. Por favor, verifica el mensaje e intenta nuevamente."
                    : "Unable to process inquiry. Please check your message and try again.";
            }
            else
            {
                requestErrorMessage = isEs
                    ? "Error al procesar la consulta con el servidor de atención."
                    : "Error processing inquiry with the attendant service.";
            }
        }

        public void RetryLastMessage()
        {
            if (!string.IsNullOrEmpty(lastFailedMessage))
            {
                currentInputText = lastFailedMessage;
                requestErrorMessage = null;
                SendUserMessage();
            }
        }

        public void FetchLiveBorderWaitTimes(bool bypassCache)
        {
            EnsureApiClient();
            if (apiClient == null) return;

            isBorderWaitTimesLoading = true;
            borderWaitTimesError = null;

            apiClient.GetBorderWaitTimes(bypassCache,
                result =>
                {
                    isBorderWaitTimesLoading = false;
                    if (result != null && result.success && result.comparison != null)
                    {
                        liveBorderWaitTimes = result;
                    }
                    else
                    {
                        borderWaitTimesError = result?.error?.message ?? "Unable to parse border wait times response.";
                    }
                },
                error =>
                {
                    isBorderWaitTimesLoading = false;
                    borderWaitTimesError = error;
                }
            );
        }

        private void CheckBackendConnection()
        {
            EnsureApiClient();
            if (apiClient == null)
            {
                serverStatusText = "ApiClient not present";
                serverStatusColor = Color.gray;
                return;
            }

            apiClient.GetHealth(
                health =>
                {
                    serverStatusText = $"Server: [CONNECTED] ({apiClient.BaseUrl}) • Model: {health.model} ({health.metaProvider})";
                    serverStatusColor = new Color(0.2f, 0.9f, 0.3f);
                },
                error =>
                {
                    serverStatusText = $"Server: [OFFLINE] ({apiClient.BaseUrl})";
                    serverStatusColor = new Color(1.0f, 0.4f, 0.4f);
                }
            );
        }

        private void EnsureApiClient()
        {
            if (apiClient == null)
            {
                apiClient = GetComponent<ApiClient>() ?? FindAnyObjectByType<ApiClient>();
            }
        }

        #endregion

        #region Input Dual-Compatibility Helpers

        public void OpenPauseMenu()
        {
            if (currentState == PrimaryUIState.Dialogue) return;
            currentState = PrimaryUIState.PauseMenu;
            LockCursor(false);
        }

        public void ResumeFromPauseMenu()
        {
            currentState = PrimaryUIState.Exploration;
            LockCursor(true);
        }

        public void OpenControlsGuide(PrimaryUIState origin)
        {
            if (currentState == PrimaryUIState.Dialogue) return;
            controlsGuideOrigin = origin;
            currentState = PrimaryUIState.ControlsGuide;
            LockCursor(false);
        }

        public void CloseControlsGuide()
        {
            if (controlsGuideOrigin == PrimaryUIState.PauseMenu)
            {
                currentState = PrimaryUIState.PauseMenu;
                LockCursor(false);
            }
            else
            {
                currentState = PrimaryUIState.Exploration;
                LockCursor(true);
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void LockCursor(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (input == Vector2.zero)
            {
                try
                {
                    input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                }
                catch { }
            }
#endif
            return input;
        }

        private Vector2 GetLookInput()
        {
            Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                look = Mouse.current.delta.ReadValue() * 0.1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (look == Vector2.zero)
            {
                try
                {
                    look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                }
                catch { }
            }
#endif
            return look;
        }

        private bool IsSprintPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
            }
            catch { }
#endif
            return false;
        }

        private bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.E)) return true;
            }
            catch { }
#endif
            return false;
        }

        private bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch { }
#endif
            return false;
        }

        private bool WasF1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.F1)) return true;
            }
            catch { }
#endif
            return false;
        }

        private bool WasMouseClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetMouseButtonDown(0)) return true;
            }
            catch { }
#endif
            return false;
        }

        #endregion

        #region In-World UI & Modal Rendering

        private void OnGUI()
        {
            switch (currentState)
            {
                case PrimaryUIState.Dialogue:
                    DrawHUD();
                    if (activeModalPoint != null)
                    {
                        DrawActiveDialogue(activeModalPoint);
                    }
                    if (isEnlargedPhotoViewerOpen)
                    {
                        DrawEnlargedPhotoViewerModal(currentLanguage == "es");
                    }
                    break;

                case PrimaryUIState.PauseMenu:
                    DrawHUD();
                    DrawPauseMenu();
                    break;

                case PrimaryUIState.ControlsGuide:
                    DrawHUD();
                    DrawControlsOverlay();
                    break;

                case PrimaryUIState.Exploration:
                default:
                    DrawReticle();
                    DrawHUD();
                    DrawWorldSpaceNPCNameplates();
                    if (currentNearbyPoint != null && isCursorLocked)
                    {
                        DrawInteractionPrompt(currentNearbyPoint);
                    }
                    break;
            }
        }

        private void DrawReticle()
        {
            if (currentState != PrimaryUIState.Exploration || !isCursorLocked) return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            const float size = 6f;

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            GUI.Box(new Rect(centerX - (size * 0.5f), centerY - (size * 0.5f), size, size), GUIContent.none);
            GUI.color = oldColor;
        }

        private void DrawHUD()
        {
            bool isEs = currentLanguage == "es";

            // Top Bar: Location & Directional Context & Language Switcher & Diagnostics
            Rect topBarRect = new Rect(14, 10, Screen.width - 28, 30);
            GUILayout.BeginArea(topBarRect);
            GUILayout.BeginHorizontal();

            // Location Header with directional guide
            string locationText = isEs
                ? "Avenida Benito Juárez • Paso del Norte (⬆ Norte: Puente y Kiosco CBP | ⬇ Sur: Centro)"
                : "Avenida Benito Juárez • Paso del Norte Corridor (⬆ North: Bridge & CBP Kiosk | ⬇ South: Downtown)";
            GUILayout.Label(locationText, GetLocationHeaderStyle());

            GUILayout.FlexibleSpace();

            // Help [F1] Toggle in HUD (hidden during dialogue to prevent interruption)
            if (currentState != PrimaryUIState.Dialogue)
            {
                string helpBtnLabel = (currentState == PrimaryUIState.ControlsGuide)
                    ? (isEs ? "Ayuda [F1] ✓" : "Help [F1] ✓")
                    : (isEs ? "Ayuda [F1]" : "Help [F1]");

                if (GUILayout.Button(helpBtnLabel, GUILayout.Width(82), GUILayout.Height(24)))
                {
                    if (currentState == PrimaryUIState.ControlsGuide)
                    {
                        CloseControlsGuide();
                    }
                    else
                    {
                        OpenControlsGuide(currentState);
                    }
                }

                GUILayout.Space(6);
            }

            // Language Switcher in HUD
            if (GUILayout.Button(currentLanguage.ToUpper(), GUILayout.Width(45), GUILayout.Height(24)))
            {
                CurrentLanguage = isEs ? "en" : "es";
            }

            GUILayout.Space(10);

            // Server Diagnostics Pill
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = serverStatusColor }
            };
            GUILayout.Label(serverStatusText, statusStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Bottom-Left Navigation Hints (Exploration only)
            if (currentState == PrimaryUIState.Exploration && isCursorLocked)
            {
                Rect hintsRect = new Rect(15, Screen.height - 40, 560, 30);
                string hints = isEs
                    ? "WASD: Moverse | Shift: Correr | E: Hablar | Esc: Pausa | F1: Guía de Controles"
                    : "WASD: Move | Shift: Sprint | E: Interact | Esc: Pause Menu | F1: Controls Guide";
                GUI.Label(hintsRect, hints, GetMutedStyle());
            }
        }

        private void DrawWorldSpaceNPCNameplates()
        {
            if (currentState != PrimaryUIState.Exploration || playerCamera == null) return;

            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            InteractionPoint[] points = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            Vector3 camPos = cam.transform.position;

            foreach (var pt in points)
            {
                if (pt == null) continue;
                Vector3 targetPos = pt.transform.position + Vector3.up * 2.15f;
                float dist = Vector3.Distance(camPos, targetPos);
                if (dist > 25f) continue; // Visible within 25 meters

                Vector3 screenPos = cam.WorldToScreenPoint(targetPos);
                if (screenPos.z < 0.5f) continue; // Behind camera

                float screenY = Screen.height - screenPos.y;
                float screenX = screenPos.x;

                // Screen bounds safety
                if (screenX < 40 || screenX > Screen.width - 40 || screenY < 35 || screenY > Screen.height - 35)
                    continue;

                bool isNearby = (pt == currentNearbyPoint);
                string charName = !string.IsNullOrEmpty(pt.characterName) ? pt.characterName : pt.title;
                string labelText = isNearby
                    ? $"[E] {pt.avatar} {charName}"
                    : $"{pt.avatar} {charName}";

                float badgeWidth = Mathf.Clamp(labelText.Length * 8.5f + 20f, 110f, 260f);
                float badgeHeight = 22f;
                Rect badgeRect = new Rect(screenX - (badgeWidth * 0.5f), screenY - (badgeHeight * 0.5f), badgeWidth, badgeHeight);

                GUIStyle nameplateStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = isNearby ? FontStyle.Bold : FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isNearby ? new Color(1f, 0.9f, 0.2f) : new Color(0.9f, 0.95f, 1f, 0.92f) }
                };

                GUI.Box(badgeRect, labelText, nameplateStyle);
            }
        }

        private void DrawInteractionPrompt(InteractionPoint point)
        {
            float width = 560;
            float height = 54;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 105;

            Rect rect = new Rect(x, y, width, height);
            GUI.Box(rect, GUIContent.none, GetOpaquePromptStyle());

            GUILayout.BeginArea(new Rect(x + 12, y + 8, width - 24, height - 16));
            GUILayout.BeginHorizontal();

            GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.9f, 0.2f) }
            };

            bool isEs = currentLanguage == "es";
            string actionVerb = isEs ? "Hablar con" : "Talk to";
            string charName = !string.IsNullOrEmpty(point.characterName) ? point.characterName : point.title;
            string avatarPrefix = !string.IsNullOrEmpty(point.avatar) ? $"{point.avatar} " : "";
            GUILayout.Label($"[E] {actionVerb} {avatarPrefix}{charName}", promptStyle);

            GUILayout.FlexibleSpace();

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            string subText = isEs ? $"{point.subtitle} • Presiona E" : $"{point.subtitle} • Press E";
            GUILayout.Label(subText, subStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPauseMenu()
        {
            bool isEs = currentLanguage == "es";
            const float width = 450;
            const float height = 370;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(x + 24, y + 20, width - 48, height - 40));

            GUILayout.Label(isEs ? "⏸️ Menú de Pausa" : "⏸️ Pause Menu", GetModalTitleStyle());
            GUILayout.Label(isEs ? "Avenida Benito Juárez • Paso del Norte" : "Avenida Benito Juárez • Paso del Norte Corridor", GetModalSubtitleStyle());
            GUILayout.Space(14);

            if (GUILayout.Button(isEs ? "▶ Reanudar Exploración [Esc]" : "▶ Resume Exploration [Esc]", GUILayout.Height(38)))
            {
                ResumeFromPauseMenu();
            }

            GUILayout.Space(8);

            if (GUILayout.Button(isEs ? "📖 Guía de Controles [F1]" : "📖 Controls Guide [F1]", GUILayout.Height(34)))
            {
                OpenControlsGuide(PrimaryUIState.PauseMenu);
            }

            GUILayout.Space(8);

            string langToggleLabel = isEs ? "🌐 Idioma: Español [Cambiar a EN]" : "🌐 Language: English [Switch to ES]";
            if (GUILayout.Button(langToggleLabel, GUILayout.Height(34)))
            {
                CurrentLanguage = isEs ? "en" : "es";
            }

            GUILayout.Space(14);

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button(isEs ? "🚪 Salir al Escritorio [Alt+F4]" : "🚪 Quit to Desktop [Alt+F4]", GUILayout.Height(36)))
            {
                QuitGame();
            }
            GUI.color = prevColor;

            GUILayout.FlexibleSpace();
            GUILayout.Label(isEs ? "Presiona Esc para reanudar el juego" : "Press Escape to resume gameplay", GetMutedStyle());

            GUILayout.EndArea();
        }

        private void DrawControlsOverlay()
        {
            bool isEs = currentLanguage == "es";
            const float width = 520;
            const float height = 390;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect cardRect = new Rect(x, y, width, height);
            GUI.Box(cardRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(x + 18, y + 14, width - 36, height - 28));

            GUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.9f, 1f) }
            };
            GUILayout.Label(isEs ? "🎮 Guía de Controles y Exploración" : "🎮 Controls & Exploration Guide", titleStyle);

            GUILayout.FlexibleSpace();

            string topCloseText = (controlsGuideOrigin == PrimaryUIState.PauseMenu)
                ? (isEs ? "✕ Volver [Esc]" : "✕ Back [Esc]")
                : (isEs ? "✕ Cerrar [Esc/F1]" : "✕ Close [Esc/F1]");

            if (GUILayout.Button(topCloseText, GUILayout.Width(110), GUILayout.Height(24)))
            {
                CloseControlsGuide();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };

            GUILayout.Label(isEs ? "Movimiento y Navegación:" : "Movement & Navigation:", sectionStyle);
            GUILayout.Label(isEs ? "• WASD / Flechas: Caminar por la avenida" : "• WASD / Arrow Keys: Walk along the avenue", GetMutedStyle());
            GUILayout.Label(isEs ? "• Shift: Correr (Sprint) | Ratón: Mirar alrededor" : "• Shift: Sprint | Mouse: Look around", GetMutedStyle());
            GUILayout.Label(isEs ? "• Clic en la ventana: Capturar ratón para explorar" : "• Click in window: Capture mouse for first-person exploration", GetMutedStyle());

            GUILayout.Space(6);
            GUILayout.Label(isEs ? "Interacción con Personajes:" : "NPC & Landmark Interaction:", sectionStyle);
            GUILayout.Label(isEs ? "• E: Hablar con personajes al ver el aviso [E]" : "• E: Talk to characters when [E] prompt appears", GetMutedStyle());
            GUILayout.Label(isEs ? "• Esc: Menú de Pausa (o cerrar conversación activa)" : "• Esc: Open Pause Menu (or close active dialogue)", GetMutedStyle());
            GUILayout.Label(isEs ? "• F1: Mostrar / ocultar esta guía en cualquier momento" : "• F1: Toggle this controls guide anytime", GetMutedStyle());

            GUILayout.Space(6);
            GUILayout.Label(isEs ? "Personajes en Avenida Juárez:" : "Inhabitants along Avenida Juárez:", sectionStyle);
            GUILayout.Label(isEs ? "• 💊 Carlos Méndez (Farmacia Benavides, Acera Oeste)" : "• 💊 Carlos Méndez (Farmacias Benavides, West sidewalk)", GetMutedStyle());
            GUILayout.Label(isEs ? "• 👓 Valeria Domínguez (Óptica Fashion, Acera Este)" : "• 👓 Valeria Domínguez (Óptica Fashion, East sidewalk)", GetMutedStyle());
            GUILayout.Label(isEs ? "• 🎒 Mateo, 💼 Sofía, 🪵 Héctor (Fila peatonal al puente)" : "• 🎒 Mateo, 💼 Sofia, 🪵 Hector (Pedestrian queue to bridge)", GetMutedStyle());
            GUILayout.Label(isEs ? "• 🛂 Elena Ramos (Kiosco oficial de información CBP)" : "• 🛂 Elena Ramos (Official CBP Border Concierge Kiosk)", GetMutedStyle());

            GUILayout.FlexibleSpace();

            // Clear bottom navigation button
            if (controlsGuideOrigin == PrimaryUIState.PauseMenu)
            {
                if (GUILayout.Button(isEs ? "← Volver al Menú de Pausa [Esc]" : "← Back to Pause Menu [Esc]", GUILayout.Height(34)))
                {
                    CloseControlsGuide();
                }
            }
            else
            {
                if (GUILayout.Button(isEs ? "▶ Reanudar Exploración [Esc/F1]" : "▶ Resume Exploration [Esc/F1]", GUILayout.Height(34)))
                {
                    CloseControlsGuide();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawActiveDialogue(InteractionPoint point)
        {
            // Enter key submission detection
            Event e = Event.current;
            if (e.isKey && e.keyCode == KeyCode.Return && e.type == EventType.KeyDown)
            {
                if (!string.IsNullOrEmpty(currentInputText.Trim()) && !isRequestPending)
                {
                    SendUserMessage();
                    e.Use();
                }
            }

            float width = (point.category == InteractionCategory.CitizenService) ? Mathf.Min(Screen.width - 24f, 880f) : 740f;
            float height = (point.category == InteractionCategory.CitizenService)
                ? Mathf.Min(Screen.height - 24f, 760f)
                : ((point.category == InteractionCategory.BorderConcierge) ? Mathf.Min(Screen.height - 24f, 710f) : Mathf.Min(Screen.height - 24f, 600f));
            if (height > Screen.height - 20f)
            {
                height = Screen.height - 20f;
            }
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none, GetOpaqueCardStyle());

            currentModalInnerHeight = height - 24f;
            GUILayout.BeginArea(new Rect(x + 16, y + 12, width - 32, currentModalInnerHeight));

            // 1. Dialogue Header
            GUILayout.BeginHorizontal();
            string avatarPrefix = !string.IsNullOrEmpty(point.avatar) ? $"{point.avatar} " : "";
            string displayName = !string.IsNullOrEmpty(point.characterName) ? point.characterName : point.title;
            GUILayout.Label($"{avatarPrefix}{displayName}", GetModalTitleStyle());

            GUILayout.FlexibleSpace();

            // Language Toggle in Dialogue Header
            string langLabel = (currentLanguage == "en") ? "Language: English [Switch to ES]" : "Idioma: Español [Cambiar a EN]";
            if (GUILayout.Button(langLabel, GUILayout.Width(220), GUILayout.Height(24)))
            {
                CurrentLanguage = (currentLanguage == "en") ? "es" : "en";
            }

            GUILayout.Space(6);

            // Close button
            if (GUILayout.Button("✕ Close [Esc]", GUILayout.Width(100), GUILayout.Height(24)))
            {
                CloseInteractionModal();
            }
            GUILayout.EndHorizontal();

            // Subtitle / Role & Address
            GUILayout.Label(point.subtitle, GetModalSubtitleStyle());
            if (!string.IsNullOrEmpty(point.address))
            {
                GUILayout.Label($"📍 {point.address}", GetMutedStyle());
            }

            GUILayout.Space(4);

            // 2. Provenance Badge Banner
            string provenanceBadge = GetProvenanceBadge(point);
            GUIStyle badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.3f, 0.95f, 0.9f) }
            };
            GUILayout.Box(provenanceBadge, badgeStyle, GUILayout.Height(24));

            // 2.5 Citizen Service Mode Selector (Increment 5.11)
            if (point.category == InteractionCategory.CitizenService)
            {
                bool isEs = currentLanguage == "es";
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                bool isConsultation = (citizenReportStep == CitizenReportWorkflowStep.Consultation);
                GUI.color = isConsultation ? new Color(0.4f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "💬 Orientación y Consultas" : "💬 Inquiries & Guidance", GUILayout.Height(26)))
                {
                    citizenReportStep = CitizenReportWorkflowStep.Consultation;
                }
                GUI.color = !isConsultation ? new Color(0.4f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "📝 Registrar Reporte Ciudadano (foto opcional)" : "📝 Register Citizen Report (photo optional)", GUILayout.Height(26)))
                {
                    if (citizenReportStep == CitizenReportWorkflowStep.Consultation)
                    {
                        citizenReportStep = CitizenReportWorkflowStep.FormInput;
                    }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.Space(6);

                if (citizenReportStep != CitizenReportWorkflowStep.Consultation)
                {
                    DrawCitizenReportWorkflowSection(isEs);
                    GUILayout.EndArea();
                    return;
                }
            }

            GUILayout.Space(6);

            // 3. Special Case: Official Border Intelligence Panel for Elena's Kiosk
            if (point.category == InteractionCategory.BorderConcierge)
            {
                DrawOfficialBorderIntelligenceSection();
                GUILayout.Space(6);
            }

            // 4. Conversation History Area (Scrollable)
            float chatBoxHeight = (point.category == InteractionCategory.BorderConcierge) ? 115 : 240;
            dialogueScrollPosition = GUILayout.BeginScrollView(dialogueScrollPosition, GUILayout.Height(chatBoxHeight));

            string charKey = point.characterId;
            if (dialogueHistories.ContainsKey(charKey))
            {
                foreach (var msg in dialogueHistories[charKey])
                {
                    DrawMessageBubble(msg);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);

            // 5. Suggested Prompts Chips
            DrawSuggestedPromptChips(point);

            GUILayout.Space(6);

            // 6. Error & Loading State Banner
            if (isRequestPending)
            {
                GUIStyle loadingStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.85f, 0.3f) }
                };
                string loadingText = (currentLanguage == "es")
                    ? "⏳ Consultando servicio de IA / World Core..."
                    : "⏳ Processing inquiry with Meta Muse Spark / World Core...";
                GUILayout.Box(loadingText, loadingStyle, GUILayout.Height(24));
                GUILayout.Space(4);
            }
            else if (!string.IsNullOrEmpty(requestErrorMessage))
            {
                GUILayout.BeginHorizontal();
                GUIStyle errorStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(1f, 0.4f, 0.4f) }
                };
                GUILayout.Box($"⚠️ {requestErrorMessage}", errorStyle, GUILayout.Height(24));
                if (GUILayout.Button((currentLanguage == "es") ? "Reintentar" : "Retry", GUILayout.Width(90), GUILayout.Height(24)))
                {
                    RetryLastMessage();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // 7. Input Bar (Text Field + Send Button)
            string statelessNotice = (currentLanguage == "es")
                ? "ℹ️ Cada consulta se procesa de forma independiente."
                : "ℹ️ Each inquiry is processed independently.";
            GUILayout.Label(statelessNotice, GetMutedStyle());
            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ChatInputField");
            GUI.enabled = !isRequestPending;
            currentInputText = GUILayout.TextField(currentInputText, GUILayout.Height(30));

            string sendBtnLabel = isRequestPending
                ? ((currentLanguage == "es") ? "Enviando..." : "Sending...")
                : ((currentLanguage == "es") ? "Enviar [Enter]" : "Send [Enter]");

            if (GUILayout.Button(sendBtnLabel, GUILayout.Width(130), GUILayout.Height(30)))
            {
                SendUserMessage();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawOfficialBorderIntelligenceSection()
        {
            bool isEs = currentLanguage == "es";

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6)
            };

            GUILayout.BeginVertical(boxStyle, GUILayout.Height(85));

            GUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.85f, 1f) }
            };
            GUILayout.Label(isEs ? "🏛️ Tiempos Oficiales de Espera Peatonal (CBP / Fila General)" : "🏛️ Official CBP Pedestrian Wait Times (General Lane)", headerStyle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(isEs ? "🔄 Actualizar" : "🔄 Refresh", GUILayout.Width(85), GUILayout.Height(20)))
            {
                FetchLiveBorderWaitTimes(true);
            }
            GUILayout.EndHorizontal();

            if (isBorderWaitTimesLoading)
            {
                GUILayout.Label(isEs ? "Cargando observaciones oficiales de CBP..." : "Fetching official CBP observations...", GetMutedStyle());
            }
            else if (!string.IsNullOrEmpty(borderWaitTimesError))
            {
                GUILayout.Label($"⚠️ {borderWaitTimesError}", GetMutedStyle());
            }
            else if (liveBorderWaitTimes?.comparison?.observations != null)
            {
                var obs = liveBorderWaitTimes.comparison.observations;
                var pdn = obs.pasoDelNorte;
                var bota = obs.bridgeOfTheAmericas;

                GUILayout.BeginHorizontal();

                // PDN Column
                if (pdn != null)
                {
                    string delay = pdn.FormatDelay(currentLanguage);
                    string fresh = (pdn.freshness == "fresh")
                        ? (isEs ? "🟢 Vigente (<60m)" : "🟢 Fresh (<60m)")
                        : (isEs ? "🟠 Desactualizado" : "🟠 Stale");

                    string obsTime = !string.IsNullOrEmpty(pdn.sourceObservationTime) ? pdn.sourceObservationTime : "N/A";

                    GUILayout.BeginVertical(GUILayout.Width(330));
                    GUILayout.Label($"<b>Paso del Norte:</b> <color=#55FF66>{delay}</color> ({pdn.portStatus}) • {fresh}", GetObservationStyle());
                    GUILayout.Label(isEs ? $"Obs. CBP: {obsTime} • Carriles: {pdn.lanesOpen}/{pdn.maximumLanes}" : $"CBP Obs: {obsTime} • Lanes: {pdn.lanesOpen}/{pdn.maximumLanes}", GetMutedStyle());
                    GUILayout.EndVertical();
                }

                GUILayout.Space(10);

                // BOTA Column
                if (bota != null)
                {
                    string delay = bota.FormatDelay(currentLanguage);
                    string fresh = (bota.freshness == "fresh")
                        ? (isEs ? "🟢 Vigente (<60m)" : "🟢 Fresh (<60m)")
                        : (isEs ? "🟠 Desactualizado" : "🟠 Stale");

                    string obsTime = !string.IsNullOrEmpty(bota.sourceObservationTime) ? bota.sourceObservationTime : "N/A";

                    GUILayout.BeginVertical(GUILayout.Width(330));
                    GUILayout.Label($"<b>BOTA:</b> <color=#FFDD55>{delay}</color> ({bota.portStatus}) • {fresh}", GetObservationStyle());
                    GUILayout.Label(isEs ? $"Obs. CBP: {obsTime} • Carriles: {bota.lanesOpen}/{bota.maximumLanes}" : $"CBP Obs: {obsTime} • Lanes: {bota.lanesOpen}/{bota.maximumLanes}", GetMutedStyle());
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();

                // Authoritative Comparison Statement if eligible
                if (liveBorderWaitTimes.comparison.comparison != null && liveBorderWaitTimes.comparison.comparison.canCompare)
                {
                    string compStatement = liveBorderWaitTimes.comparison.comparison.statement?.Get(currentLanguage);
                    if (!string.IsNullOrEmpty(compStatement))
                    {
                        GUILayout.Label($"⚖️ {compStatement}", GetComparisonStatementStyle());
                    }
                }

                // Player Crossing Intent Selection (Session-Local Decision)
                DrawPlayerCrossingIntentSection(isEs);
            }
            else
            {
                GUILayout.Label(isEs ? "Presione 'Actualizar' para consultar CBP." : "Click 'Refresh' to fetch official CBP wait times.", GetMutedStyle());
            }

            GUILayout.EndVertical();
        }

        private void DrawPlayerCrossingIntentSection(bool isEs)
        {
            GUILayout.Space(4);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };
            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();
            GUIStyle intentHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUILayout.Label(isEs ? "🎯 Intención de Cruce del Jugador (Decisión Local de Sesión):" : "🎯 Player Crossing Intent (Local Session Decision):", intentHeaderStyle);
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(selectedCrossingIntent))
            {
                if (GUILayout.Button(isEs ? "✕ Cambiar" : "✕ Change", GUILayout.Width(75), GUILayout.Height(18)))
                {
                    selectedCrossingIntent = null;
                }
            }
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(selectedCrossingIntent))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isEs ? "🚶 Optar por Paso del Norte (Centro)" : "🚶 Choose Paso del Norte (Downtown)", GUILayout.Height(24)))
                {
                    selectedCrossingIntent = "paso-del-norte";
                }
                GUILayout.Space(6);
                if (GUILayout.Button(isEs ? "🚌 Optar por Puente de las Américas (BOTA)" : "🚌 Choose Bridge of the Americas (BOTA)", GUILayout.Height(24)))
                {
                    selectedCrossingIntent = "bridge-of-the-americas";
                }
                GUILayout.EndHorizontal();

                string promptNotice = isEs
                    ? "Elija un cruce tras comparar los datos oficiales de CBP. Esta elección es local a la sesión de juego."
                    : "Select a crossing after reviewing official CBP wait data. This selection is session-local.";
                GUILayout.Label(promptNotice, GetMutedStyle());
            }
            else
            {
                string chosenTitle = (selectedCrossingIntent == "paso-del-norte")
                    ? (isEs ? "Paso del Norte / Santa Fe (Centro)" : "Paso del Norte / Santa Fe (Downtown)")
                    : (isEs ? "Puente de las Américas / BOTA (Puente Libre)" : "Bridge of the Americas / BOTA");

                GUIStyle chosenStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.4f, 1f, 0.5f) }
                };
                GUILayout.Label(isEs ? $"✓ Decisión seleccionada: {chosenTitle}" : $"✓ Selected Intent: {chosenTitle}", chosenStyle);

                string disclaimer = isEs
                    ? "ℹ️ Decisión local de sesión dentro de la experiencia virtual. No inicia un viaje real, no crea reservaciones ni altera el estado persistente del mundo."
                    : "ℹ️ Session-local intent within this virtual experience. Does not initiate a real-world trip, create reservations, or modify persistent World Core state.";
                GUILayout.Label(disclaimer, GetMutedStyle());
            }

            GUILayout.EndVertical();
        }

        private void DrawMessageBubble(DialogueMessage msg)
        {
            GUILayout.BeginVertical(GetMessageContainerStyle(msg.isUser));

            // Header line (Sender + Badge + Time)
            GUILayout.BeginHorizontal();
            string senderText = msg.isUser
                ? $"<b>{msg.avatar} {msg.senderName}</b>"
                : $"<b>{msg.avatar} {msg.senderName}</b> <color=#88CCEE>({msg.provenanceBadge})</color>";

            GUILayout.Label(senderText, GetMessageHeaderStyle(msg.isUser));
            GUILayout.FlexibleSpace();
            GUILayout.Label(msg.timestamp, GetMutedStyle());
            GUILayout.EndHorizontal();

            // Body text
            GUILayout.Label(msg.text, GetMessageBodyStyle(msg.isUser));

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawSuggestedPromptChips(InteractionPoint point)
        {
            string[] prompts = (currentLanguage == "es") ? point.suggestedPromptsEs : point.suggestedPromptsEn;
            if (prompts == null || prompts.Length == 0) return;

            bool isEs = currentLanguage == "es";
            GUILayout.BeginVertical();
            GUILayout.Label(isEs ? "💡 Preguntas sugeridas (haz clic para consultar):" : "💡 Quick prompts (click to ask):", GetMutedStyle());

            // Multi-row adaptive flow layout (2 columns per row)
            // Dialogue inner width is 688px, so 338px per button fits cleanly across resolutions without clipping
            for (int i = 0; i < prompts.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + 2, prompts.Length); j++)
                {
                    string promptText = prompts[j];
                    if (GUILayout.Button(promptText, GUILayout.Height(24), GUILayout.MaxWidth(345)))
                    {
                        currentInputText = promptText;
                        if (!isRequestPending)
                        {
                            SendUserMessage();
                        }
                    }
                    if (j == i && j + 1 < prompts.Length)
                    {
                        GUILayout.Space(8);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (i + 2 < prompts.Length)
                {
                    GUILayout.Space(4);
                }
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region GUI Styles Cache

        private static Texture2D opaqueDarkBackgroundTex;
        private static Texture2D opaquePromptBackgroundTex;
        private GUIStyle opaqueCardStyle;
        private GUIStyle opaquePromptStyle;

        private GUIStyle GetOpaqueCardStyle()
        {
            if (opaqueCardStyle == null)
            {
                opaqueCardStyle = new GUIStyle(GUI.skin.box);
                if (opaqueDarkBackgroundTex == null)
                {
                    opaqueDarkBackgroundTex = CreateBorderedTexture(16, 16,
                        new Color(0.07f, 0.09f, 0.13f, 0.94f),
                        new Color(0.20f, 0.28f, 0.38f, 0.95f)
                    );
                }
                opaqueCardStyle.normal.background = opaqueDarkBackgroundTex;
                opaqueCardStyle.border = new RectOffset(2, 2, 2, 2);
            }
            return opaqueCardStyle;
        }

        private GUIStyle GetOpaquePromptStyle()
        {
            if (opaquePromptStyle == null)
            {
                opaquePromptStyle = new GUIStyle(GUI.skin.box);
                if (opaquePromptBackgroundTex == null)
                {
                    opaquePromptBackgroundTex = CreateBorderedTexture(16, 16,
                        new Color(0.06f, 0.08f, 0.12f, 0.94f),
                        new Color(0.95f, 0.80f, 0.20f, 0.85f)
                    );
                }
                opaquePromptStyle.normal.background = opaquePromptBackgroundTex;
                opaquePromptStyle.border = new RectOffset(2, 2, 2, 2);
            }
            return opaquePromptStyle;
        }

        private static Texture2D CreateBorderedTexture(int width, int height, Color fillColor, Color borderColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
                    tex.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }
            tex.Apply();
            return tex;
        }

        private GUIStyle locationHeaderStyle;
        private GUIStyle GetLocationHeaderStyle()
        {
            if (locationHeaderStyle == null)
            {
                locationHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
                };
            }
            return locationHeaderStyle;
        }

        private GUIStyle modalTitleStyle;
        private GUIStyle GetModalTitleStyle()
        {
            if (modalTitleStyle == null)
            {
                modalTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
            return modalTitleStyle;
        }

        private GUIStyle modalSubtitleStyle;
        private GUIStyle GetModalSubtitleStyle()
        {
            if (modalSubtitleStyle == null)
            {
                modalSubtitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.3f, 0.8f, 1f) }
                };
            }
            return modalSubtitleStyle;
        }

        private GUIStyle mutedStyle;
        private GUIStyle GetMutedStyle()
        {
            if (mutedStyle == null)
            {
                mutedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
            return mutedStyle;
        }

        private GUIStyle observationStyle;
        private GUIStyle GetObservationStyle()
        {
            if (observationStyle == null)
            {
                observationStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }
            return observationStyle;
        }

        private GUIStyle comparisonStatementStyle;
        private GUIStyle GetComparisonStatementStyle()
        {
            if (comparisonStatementStyle == null)
            {
                comparisonStatementStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.95f, 0.9f, 0.5f) }
                };
            }
            return comparisonStatementStyle;
        }

        private GUIStyle subheadingStyle;
        private GUIStyle GetSubheadingStyle()
        {
            if (subheadingStyle == null)
            {
                subheadingStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }
            return subheadingStyle;
        }

        private GUIStyle GetMessageContainerStyle(bool isUser)
        {
            GUIStyle s = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 4, 4)
            };
            return s;
        }

        private GUIStyle GetMessageHeaderStyle(bool isUser)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = isUser ? new Color(0.4f, 0.85f, 1f) : new Color(1f, 0.85f, 0.3f) }
            };
        }

        private GUIStyle GetMessageBodyStyle(bool isUser)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = isUser ? new Color(0.9f, 0.95f, 1f) : Color.white }
            };
        }

        #endregion

        #region Citizen Issue Reporting Workflow (Increment 5.11: SAM 3.1 + Muse Spark 1.3)

        private void DrawCitizenReportWorkflowSection(bool isEs)
        {
            if (!string.IsNullOrEmpty(citizenReportErrorMessage))
            {
                GUIStyle errStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(1f, 0.4f, 0.4f) }
                };
                GUILayout.BeginHorizontal();
                GUILayout.Box($"⚠️ {citizenReportErrorMessage}", errStyle, GUILayout.Height(24));
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(24)))
                {
                    citizenReportErrorMessage = null;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            switch (citizenReportStep)
            {
                case CitizenReportWorkflowStep.FormInput:
                    DrawCitizenReportFormInput(isEs);
                    break;

                case CitizenReportWorkflowStep.Analyzing:
                    DrawCitizenReportAnalyzing(isEs);
                    break;

                case CitizenReportWorkflowStep.DraftReview:
                    DrawCitizenReportDraftReview(isEs);
                    break;

                case CitizenReportWorkflowStep.ReceiptSuccess:
                    DrawCitizenReportReceiptSuccess(isEs);
                    break;

                default:
                    citizenReportStep = CitizenReportWorkflowStep.FormInput;
                    break;
            }
        }

        private void DrawCitizenReportFormInput(bool isEs)
        {
            GUILayout.Label(isEs ? "🏛️ Atención Ciudadana Municipal · Registro Simulado de Incidencias" : "🏛️ Municipal Citizen Service · Simulated Issue Intake", GetModalSubtitleStyle());
            GUILayout.Space(2);

            // Bounded vertical scrollview (Objective B Repair):
            // - GUIStyle.none for horizontal scrollbar guarantees NO horizontal scrollbar is ever rendered
            // - GUI.skin.verticalScrollbar provides smooth vertical scrolling on the right
            // - ExpandHeight(true) fills all remaining space in the dialog window
            citizenReportFormScrollPos = GUILayout.BeginScrollView(
                citizenReportFormScrollPos,
                false,                         // alwaysShowHorizontal: NEVER show horizontal scrollbar
                false,                        // alwaysShowVertical: automatic vertical scrollbar on the right
                GUIStyle.none,                // horizontalScrollbar: GUIStyle.none completely suppresses horizontal scrollbar
                GUI.skin.verticalScrollbar,   // verticalScrollbar: standard vertical scrollbar on the right
                GUIStyle.none,                // background: transparent
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            // Emergency Notice Banner
            GUIStyle emBannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.45f, 0.45f) }
            };
            GUILayout.Box(isEs
                ? "⚠️ AVISO: Ante emergencias médicas, incendios o delitos en progreso, llame de inmediato al 911. Este canal simula únicamente reportes de servicios municipales no urgentes."
                : "⚠️ NOTICE: For medical emergencies, active fires, or crimes in progress, call 911 immediately. This channel simulates non-urgent municipal services only.",
                emBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            // Step 1: Category Selection (9 Municipal Areas)
            GUILayout.Label(isEs ? "1. Seleccione la categoría municipal de la incidencia:" : "1. Select the municipal category:", GetSubheadingStyle());

            // 3x3 category grid
            int totalCats = CategoryIds.Length;
            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int catIdx = row * 3 + col;
                    if (catIdx < totalCats)
                    {
                        bool isSelected = (selectedCategoryIndex == catIdx);
                        GUI.color = isSelected ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.85f, 0.85f, 0.9f);
                        string catLabel = isEs ? CategoryNamesEs[catIdx] : CategoryNamesEn[catIdx];
                        if (GUILayout.Button(catLabel, GUILayout.Height(24)))
                        {
                            selectedCategoryIndex = catIdx;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Step 2: Description
            GUILayout.Label(isEs ? "2. Describa la afectación observada o solicitud de servicio:" : "2. Describe the observed issue or service request:", GetSubheadingStyle());
            reportDescriptionInput = GUILayout.TextArea(reportDescriptionInput, GUILayout.Height(48));

            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? "Ejemplos rápidos:" : "Quick examples:", GetMutedStyle(), GUILayout.Width(95));
            if (GUILayout.Button(isEs ? "Bache en calzada" : "Pothole in road", GUILayout.Height(18)))
            {
                reportDescriptionInput = isEs ? "Bache profundo en el carril vehicular que daña neumáticos y suspensión." : "Deep pothole in the vehicular lane damaging tires and suspension.";
                selectedCategoryIndex = 0; // road-surface-damage
            }
            if (GUILayout.Button(isEs ? "Basura acumulada" : "Accumulated waste", GUILayout.Height(18)))
            {
                reportDescriptionInput = isEs ? "Bolsas de basura acumuladas y tiradero clandestino obstruyendo la vía pública." : "Accumulated trash bags and clandestine dumping obstructing the public way.";
                selectedCategoryIndex = 2; // cleaning-and-waste
            }
            if (GUILayout.Button(isEs ? "Fuga / Drenaje" : "Water / Drain", GUILayout.Height(18)))
            {
                reportDescriptionInput = isEs ? "Encharcamiento importante de aguas limpias por fuga en la red hidráulica." : "Significant standing clean water due to hydraulic distribution leak.";
                selectedCategoryIndex = 3; // water-and-drainage
            }
            if (GUILayout.Button(isEs ? "Lámpara apagada" : "Broken light", GUILayout.Height(18)))
            {
                reportDescriptionInput = isEs ? "Luminaria pública apagada dejando el sector peatonal a oscuras." : "Streetlight out leaving the pedestrian sector in complete darkness.";
                selectedCategoryIndex = 4; // public-lighting
            }
            if (GUILayout.Button(isEs ? "Banqueta rota" : "Broken sidewalk", GUILayout.Height(18)))
            {
                reportDescriptionInput = isEs ? "Banqueta fracturada con escombro que impide el paso a personas con discapacidad." : "Fractured sidewalk with debris preventing access for people with disabilities.";
                selectedCategoryIndex = 1; // sidewalk-obstruction
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Step 3: Incident Location (Citywide decoupled)
            GUILayout.Label(isEs ? "3. Ubicación de la incidencia en Ciudad Juárez:" : "3. Incident location in Ciudad Juárez:", GetSubheadingStyle());
            reportLocationInput = GUILayout.TextField(reportLocationInput);

            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? "Sugerencias:" : "Suggestions:", GetMutedStyle(), GUILayout.Width(75));
            if (GUILayout.Button("Av. Benito Juárez (en corredor demo)", GUILayout.Height(18)))
            {
                reportLocationInput = "Avenida Benito Juárez (sector virtual)";
            }
            if (GUILayout.Button("Colonia Melchor Ocampo", GUILayout.Height(18)))
            {
                reportLocationInput = "Colonia Melchor Ocampo, Calle Juan Escutia";
            }
            if (GUILayout.Button("Blvd. Gómez Morín", GUILayout.Height(18)))
            {
                reportLocationInput = "Blvd. Manuel Gómez Morín #1234";
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(isEs
                ? "📍 Incidencias en el corredor de Av. Juárez cuentan con coordenadas verificadas. En otros sectores se registran a nivel descriptivo sin inventar coordenadas."
                : "📍 Incidents on Av. Juárez have verified coordinates. In other sectors, locations are recorded descriptively without invented coordinates.",
                GetMutedStyle());

            GUILayout.Space(4);

            // Step 4: Photographic Evidence (Strictly Optional)
            GUILayout.Label(isEs ? "4. Evidencia fotográfica (Opcional — PNG o JPEG, máx 5 MB):" : "4. Photographic evidence (Optional — PNG or JPEG, max 5 MB):", GetSubheadingStyle());

            GUILayout.BeginHorizontal();
            if (selectedImagePreview == null)
            {
                if (GUILayout.Button(isEs ? "📷 Adjuntar Fotografía (Opcional)..." : "📷 Attach Photograph (Optional)...", GUILayout.Width(260), GUILayout.Height(28)))
                {
                    string filePath = WindowsFileDialog.OpenImageFileDialog(isEs ? "Seleccionar Fotografía de Incidencia" : "Select Issue Photograph");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        LoadSelectedImage(filePath, isEs);
                    }
                }
                GUILayout.Space(8);
                GUILayout.Label(isEs ? "ℹ️ Puede continuar sin fotografía si no cuenta con una." : "ℹ️ You may proceed without photo if unavailable.", GetMutedStyle());
            }
            else
            {
                Rect thumbRect = GUILayoutUtility.GetRect(100, 65, GUILayout.Width(100), GUILayout.Height(65));
                GUI.DrawTexture(thumbRect, selectedImagePreview, ScaleMode.ScaleToFit);

                GUILayout.BeginVertical();
                GUILayout.Label($"📷 {selectedImageFileName}", GetSubheadingStyle());
                int kb = (selectedImageBytes != null) ? (selectedImageBytes.Length / 1024) : 0;
                GUILayout.Label(isEs ? $"Tamaño: {kb} KB · Formato: {selectedImageMime}" : $"Size: {kb} KB · Format: {selectedImageMime}", GetMutedStyle());

                // External AI Consent Toggle
                externalAiConsent = GUILayout.Toggle(externalAiConsent, isEs
                    ? " Autorizo análisis visual externo con IA (Meta SAM 3.1)"
                    : " Authorize external AI visual analysis (Meta SAM 3.1)");

                if (!externalAiConsent)
                {
                    GUILayout.Label(isEs
                        ? "🔒 Privacidad: La imagen se guardará localmente pero NO se enviará a Meta."
                        : "🔒 Privacy: Image will be stored locally but NOT sent to Meta.",
                        GetMutedStyle());
                }

                if (GUILayout.Button(isEs ? "❌ Quitar Fotografía" : "❌ Remove Photo", GUILayout.Width(160), GUILayout.Height(20)))
                {
                    selectedImagePath = null;
                    selectedImageFileName = null;
                    selectedImageBytes = null;
                    selectedImageBase64 = null;
                    selectedImageMime = null;
                    if (selectedImagePreview != null)
                    {
                        Destroy(selectedImagePreview);
                        selectedImagePreview = null;
                    }
                    DestroyOverlayTexture();
                    CloseEnlargedImageViewer();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Primary Action Button: Located directly inside the scrollable form body,
            // guaranteed to never be pushed outside the window clipping rect,
            // and the vertical scrollbar on the right allows smooth scrolling directly to it if the screen is small!
            GUI.color = new Color(0.25f, 0.95f, 0.85f);
            if (GUILayout.Button(isEs ? "🔍 Generar Borrador del Reporte (Muse Spark 1.3)" : "🔍 Generate Report Draft (Muse Spark 1.3)", GUILayout.Height(36)))
            {
                if (string.IsNullOrEmpty(reportDescriptionInput.Trim()))
                {
                    citizenReportErrorMessage = isEs ? "Por favor ingrese una descripción de la incidencia." : "Please enter an issue description.";
                }
                else
                {
                    TriggerDraftAnalysis(isEs);
                }
            }
            GUI.color = Color.white;
            GUILayout.Space(8);

            // End of scrollable form body
            GUILayout.EndScrollView();
        }

        private void LoadSelectedImage(string filePath, bool isEs)
        {
            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    citizenReportErrorMessage = isEs ? "El archivo seleccionado no existe." : "Selected file does not exist.";
                    return;
                }

                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    citizenReportErrorMessage = isEs ? "El archivo excede el tamaño máximo permitido de 5 MB." : "File exceeds maximum allowed size of 5 MB.";
                    return;
                }

                string ext = fileInfo.Extension.ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                {
                    citizenReportErrorMessage = isEs ? "Formato de imagen no soportado. Únicamente se admiten archivos PNG y JPEG." : "Unsupported image format. Only PNG and JPEG files are accepted.";
                    return;
                }

                selectedImagePath = filePath;
                selectedImageFileName = fileInfo.Name;
                selectedImageBytes = System.IO.File.ReadAllBytes(filePath);
                selectedImageBase64 = Convert.ToBase64String(selectedImageBytes);
                selectedImageMime = (ext == ".png") ? "image/png" : "image/jpeg";

                if (selectedImagePreview != null) Destroy(selectedImagePreview);
                selectedImagePreview = new Texture2D(2, 2);
                selectedImagePreview.LoadImage(selectedImageBytes);
                citizenReportErrorMessage = null;
            }
            catch (Exception ex)
            {
                citizenReportErrorMessage = ex.Message;
            }
        }

        private void TriggerDraftAnalysis(bool isEs)
        {
            citizenReportErrorMessage = null;
            citizenReportStep = CitizenReportWorkflowStep.Analyzing;

            var draftReq = new CitizenReportDraftRequestDto
            {
                citizenDescription = reportDescriptionInput.Trim(),
                locationDescription = reportLocationInput.Trim(),
                category = CategoryIds[selectedCategoryIndex],
                representativeId = "patricia-morales",
                externalAiProcessingConsent = (selectedImageBase64 != null) && externalAiConsent,
                language = currentLanguage,
                image = (selectedImageBase64 != null) ? new CitizenReportImageUploadDto
                {
                    fileName = selectedImageFileName ?? "evidence.jpg",
                    mimeType = selectedImageMime ?? "image/jpeg",
                    base64 = selectedImageBase64
                } : null
            };

            EnsureApiClient();
            apiClient.DraftCitizenReport(draftReq,
                res =>
                {
                    if (res != null && res.success && res.draft != null)
                    {
                        currentReportDraft = res.draft;
                        EnsureOverlayTextureLoaded();
                        citizenReportStep = CitizenReportWorkflowStep.DraftReview;
                    }
                    else
                    {
                        citizenReportErrorMessage = res?.error?.message ?? (isEs ? "Error al generar borrador con modelos Meta." : "Error drafting report with Meta models.");
                        citizenReportStep = CitizenReportWorkflowStep.FormInput;
                    }
                },
                err =>
                {
                    citizenReportErrorMessage = err;
                    citizenReportStep = CitizenReportWorkflowStep.FormInput;
                }
            );
        }

        private void DrawCitizenReportAnalyzing(bool isEs)
        {
            GUILayout.Space(30);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.95f, 0.9f) }
            };

            string title = isEs ? "⏳ Formulando Borrador de Reporte Ciudadano" : "⏳ Formulating Citizen Report Draft";
            GUILayout.Box(title, boxStyle, GUILayout.Height(36));

            GUILayout.Space(10);
            GUILayout.Label(isEs ? "• Modelo de Lenguaje: Meta Muse Spark 1.3 (Estructuración y categorización)..." : "• Language Model: Meta Muse Spark 1.3 (Structuring & categorization)...", GetSubheadingStyle());
            if (selectedImageBase64 != null && externalAiConsent)
            {
                GUILayout.Label(isEs ? "• Modelo Visual: Meta SAM 3.1 (Segmentación visual dinámica de la afectación)..." : "• Visual Model: Meta SAM 3.1 (Dynamic visual segmentation of issue)...", GetSubheadingStyle());
            }
            else if (selectedImageBase64 != null)
            {
                GUILayout.Label(isEs ? "• Evidencia: Resguardo fotográfico local (sin inferencia externa por configuración de privacidad)..." : "• Evidence: Local photo archive (no external inference per privacy consent)...", GetSubheadingStyle());
            }
            GUILayout.Label(isEs ? "• Autoridad de Dominio: MyVirtualCommunity World Core..." : "• Domain Authority: MyVirtualCommunity World Core...", GetSubheadingStyle());

            GUILayout.Space(15);
            GUILayout.Label(isEs ? "Por favor espere unos momentos mientras se formula el borrador..." : "Please wait a moment while the draft is formulated...", GetMutedStyle());
        }

        private void DrawCitizenReportDraftReview(bool isEs)
        {
            if (currentReportDraft == null)
            {
                citizenReportStep = CitizenReportWorkflowStep.FormInput;
                return;
            }

            GUILayout.Label(isEs ? "📋 Revisión del Borrador del Reporte Ciudadano" : "📋 Citizen Report Draft Review", GetModalSubtitleStyle());
            GUILayout.Space(4);

            // Non-emergency alert if detected
            if (currentReportDraft.nonEmergencyAssessment != null && currentReportDraft.nonEmergencyAssessment.isEmergency)
            {
                GUIStyle alertStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
                string emMsg = isEs ? currentReportDraft.nonEmergencyAssessment.emergencyNoticeEs : currentReportDraft.nonEmergencyAssessment.emergencyNoticeEn;
                GUILayout.Box($"🚨 {emMsg}", alertStyle, GUILayout.Height(40));
                GUILayout.Space(4);
            }

            GUILayout.BeginHorizontal();

            // Left Column: Visual Evidence or Text-only status
            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.Label(isEs ? "Evidencia y Análisis Visual:" : "Evidence & Visual Analysis:", GetSubheadingStyle());

            if (selectedImagePreview != null)
            {
                Rect thumbRect = GUILayoutUtility.GetRect(260, 140, GUILayout.Width(260), GUILayout.Height(140));

                // Clickable thumbnail button (Objective A)
                if (GUI.Button(thumbRect, GUIContent.none, GUIStyle.none))
                {
                    OpenEnlargedImageViewer();
                }

                // Base photograph
                GUI.DrawTexture(thumbRect, selectedImagePreview, ScaleMode.ScaleToFit);

                // SAM 3.1 mask overlay or bounding boxes on thumbnail
                if (segmentationOverlayTexture != null && currentReportDraft.visualAnalysis != null && currentReportDraft.visualAnalysis.hasAuthenticMasks)
                {
                    GUI.DrawTexture(thumbRect, segmentationOverlayTexture, ScaleMode.ScaleToFit, true);
                }
                else if (currentReportDraft.visualAnalysis != null && currentReportDraft.visualAnalysis.boxes != null && currentReportDraft.visualAnalysis.boxes.Length > 0)
                {
                    var va = currentReportDraft.visualAnalysis;
                    for (int i = 0; i < va.boxes.Length; i++)
                    {
                        var b = va.boxes[i];
                        float bx = thumbRect.x + b.xmin * thumbRect.width;
                        float by = thumbRect.y + b.ymin * thumbRect.height;
                        float bw = (b.xmax - b.xmin) * thumbRect.width;
                        float bh = (b.ymax - b.ymin) * thumbRect.height;

                        Color bColor = Color.red;
                        if (va.objects != null && i < va.objects.Length && !string.IsNullOrEmpty(va.objects[i].colorHex))
                        {
                            ColorUtility.TryParseHtmlString(va.objects[i].colorHex, out bColor);
                        }

                        GUIStyle boxOutline = new GUIStyle(GUI.skin.box);
                        boxOutline.normal.textColor = bColor;
                        GUI.color = bColor;
                        string boxLabel = b.label ?? (isEs ? "región" : "region");
                        if (b.confidence > 0.01f)
                        {
                            boxLabel += $" ({(b.confidence * 100f):F0}%)";
                        }
                        GUI.Box(new Rect(bx, by, bw, bh), boxLabel, boxOutline);
                        GUI.color = Color.white;
                    }
                }

                // Click hint pill overlay across bottom of thumbnail
                GUIStyle thumbHintStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f) }
                };
                GUI.Box(new Rect(thumbRect.x + 4, thumbRect.y + thumbRect.height - 22, thumbRect.width - 8, 20),
                    isEs ? "🔍 Clic en la imagen para ampliar inspección" : "🔍 Click image to enlarge & inspect", thumbHintStyle);

                GUILayout.Space(4);

                // Detailed inspection button below thumbnail
                GUI.color = new Color(0.3f, 0.95f, 0.85f);
                if (GUILayout.Button(isEs ? "🔬 Inspección Visual de Segmentación" : "🔬 Visual Segmentation Inspection", GUILayout.Width(260), GUILayout.Height(24)))
                {
                    OpenEnlargedImageViewer();
                }
                GUI.color = Color.white;
            }
            else
            {
                GUIStyle noPhotoBox = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
                };
                GUILayout.Box(isEs ? "📄 Reporte sin fotografía adjunta\n(Válido para registro y canalización simulada)" : "📄 Report without attached photo\n(Valid for simulated registration & routing)", noPhotoBox, GUILayout.Height(100));
            }

            string provider = currentReportDraft.visualAnalysis != null ? currentReportDraft.visualAnalysis.provider : "none";
            string badgeText;
            Color badgeColor;

            if (provider == "sam-3.1-live-api")
            {
                badgeText = isEs ? "SAM 3.1 — INFERENCIA EN VIVO COMPLETADA" : "SAM 3.1 — LIVE INFERENCE COMPLETED";
                badgeColor = new Color(0.25f, 0.95f, 0.45f);
            }
            else if (provider == "sam-3.1-local-mock")
            {
                badgeText = isEs ? "SAM 3.1 — VISTA PREVIA SIMULADA (MOCK LOCAL)" : "SAM 3.1 — SIMULATED MOCK PREVIEW";
                badgeColor = new Color(1.0f, 0.85f, 0.2f);
            }
            else if (provider == "not-authorized")
            {
                badgeText = isEs ? "PRIVACIDAD — ANÁLISIS IA NO AUTORIZADO" : "PRIVACY — AI ANALYSIS NOT AUTHORIZED";
                badgeColor = new Color(0.4f, 0.85f, 1f);
            }
            else
            {
                badgeText = isEs ? "ANÁLISIS VISUAL OMITIDO / NO APLICA" : "VISUAL ANALYSIS OMITTED / N/A";
                badgeColor = new Color(0.75f, 0.75f, 0.75f);
            }

            GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = badgeColor }
            };
            GUILayout.Box(badgeText, statusStyle, GUILayout.Height(22));

            string samNotes = currentReportDraft.visualAnalysis?.maskSummary ?? "";
            if (!string.IsNullOrEmpty(samNotes))
            {
                GUILayout.Label(samNotes, GetMutedStyle());
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Right Column: Muse Spark 1.3 & World Core Formatted Report
            GUILayout.BeginVertical();
            GUILayout.Label(isEs ? "Estructuración y Canalización (Muse Spark 1.3):" : "Structured Routing (Muse Spark 1.3):", GetSubheadingStyle());

            var spark = currentReportDraft.structuredInterpretation;
            if (spark != null)
            {
                GUILayout.Label($"<b>{ (isEs ? "Resumen:" : "Summary:") }</b> {spark.summary}", GetSubheadingStyle());

                // Category selector chip to allow override before final confirmation
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{ (isEs ? "Categoría Confirmada:" : "Confirmed Category:") }</b>", GetMutedStyle(), GUILayout.Width(140));
                string catName = isEs ? CategoryNamesEs[selectedCategoryIndex] : CategoryNamesEn[selectedCategoryIndex];
                GUILayout.Label($"<color=#55eeff>{catName}</color>", GetSubheadingStyle());
                GUILayout.EndHorizontal();

                GUILayout.Label($"<b>{ (isEs ? "Área de Responsabilidad Simulada (Recomendación):" : "Simulated Area of Responsibility (Recommendation):") }</b>\n<color=#ffdd66>{currentReportDraft.simulatedDestination}</color>", GetSubheadingStyle());

                // Location & Geographic Coverage Badge
                string coverageStatus = currentReportDraft.location != null ? currentReportDraft.location.coverageStatus : "UNRESOLVED";
                string coverageBadge = (coverageStatus == "IN_DEMO_CORRIDOR")
                    ? (isEs ? "[DENTRO DEL CORREDOR DEMO — COORDENADAS VERIFICADAS]" : "[IN DEMO CORRIDOR — VERIFIED COORDS]")
                    : (isEs ? "[FUERA DE COBERTURA DEMO — REGISTRO DESCRIPTIVO]" : "[OUTSIDE DEMO COVERAGE — DESCRIPTIVE ENTRY]");
                Color covColor = (coverageStatus == "IN_DEMO_CORRIDOR") ? new Color(0.3f, 0.95f, 0.5f) : new Color(1f, 0.8f, 0.3f);

                GUILayout.Label($"<b>{ (isEs ? "Ubicación:" : "Location:") }</b> {currentReportDraft.location?.description}", GetMutedStyle());
                GUIStyle covStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = covColor } };
                GUILayout.Label(coverageBadge, covStyle);

                // Intake Representative
                string repName = currentReportDraft.reportingRepresentative?.name ?? "Lic. Patricia Morales";
                string repRole = currentReportDraft.reportingRepresentative?.role ?? "Enlace de Atención Ciudadana";
                GUILayout.Label($"<b>{ (isEs ? "Canal de Recepción:" : "Intake Channel:") }</b> {repName} ({repRole})", GetMutedStyle());

                if (spark.missingInformation != null && spark.missingInformation.Length > 0)
                {
                    GUILayout.Label($"<b>{ (isEs ? "Observación técnica:" : "Technical note:") }</b> {string.Join(", ", spark.missingInformation)}", GetMutedStyle());
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Mandatory Simulation Notice Box
            GUIStyle noticeBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUILayout.Box(currentReportDraft.simulationNotice, noticeBoxStyle, GUILayout.Height(30));

            GUILayout.Space(6);

            // Action Buttons: Edit or Confirm
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isEs ? "← Modificar Datos" : "← Edit Details", GUILayout.Width(160), GUILayout.Height(32)))
            {
                CloseEnlargedImageViewer();
                citizenReportStep = CitizenReportWorkflowStep.FormInput;
            }

            GUILayout.FlexibleSpace();

            GUI.color = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button(isEs ? "✅ Confirmar y Registrar en World Core" : "✅ Confirm and Register in World Core", GUILayout.Width(330), GUILayout.Height(32)))
            {
                CloseEnlargedImageViewer();
                TriggerReportSubmission(isEs);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        private void TriggerReportSubmission(bool isEs)
        {
            if (currentReportDraft == null) return;

            citizenReportErrorMessage = null;
            isRequestPending = true;

            var subReq = new CitizenReportSubmitRequestDto
            {
                draftId = currentReportDraft.draftId,
                confirmedByPlayer = true, // Authoritative player confirmation required by World Core!
                confirmedCategory = CategoryIds[selectedCategoryIndex]
            };

            EnsureApiClient();
            apiClient.SubmitCitizenReport(subReq,
                res =>
                {
                    isRequestPending = false;
                    if (res != null && res.success && res.receipt != null)
                    {
                        currentReportReceipt = res.receipt;
                        citizenReportStep = CitizenReportWorkflowStep.ReceiptSuccess;
                    }
                    else
                    {
                        citizenReportErrorMessage = res?.error?.message ?? (isEs ? "Error al registrar reporte en World Core." : "Error submitting report to World Core.");
                    }
                },
                err =>
                {
                    isRequestPending = false;
                    citizenReportErrorMessage = err;
                }
            );
        }

        private void DrawCitizenReportReceiptSuccess(bool isEs)
        {
            if (currentReportReceipt == null)
            {
                citizenReportStep = CitizenReportWorkflowStep.FormInput;
                return;
            }

            GUIStyle receiptHeaderStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.95f, 0.5f) }
            };
            GUILayout.Box(isEs ? "✅ Comprobante de Reporte Ciudadano (Demostración Simulada)" : "✅ Citizen Report Receipt (Simulated Demonstration)", receiptHeaderStyle, GUILayout.Height(30));

            GUILayout.Space(8);

            // Receipt Box
            GUIStyle cardBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 10, 10),
                alignment = TextAnchor.UpperLeft
            };

            GUILayout.BeginVertical(cardBox);
            GUILayout.Label($"<b>{ (isEs ? "Folio de Registro:" : "Report Folio ID:") }</b> <size=15><color=#44ffbb>{currentReportReceipt.reportId}</color></size>", GetSubheadingStyle());
            GUILayout.Space(3);
            GUILayout.Label($"<b>{ (isEs ? "Categoría Confirmada:" : "Confirmed Category:") }</b> {currentReportReceipt.category}", GetSubheadingStyle());

            string locText = currentReportReceipt.location;
            if (!string.IsNullOrEmpty(currentReportReceipt.locationCoverage))
            {
                locText += $" <color=#ffbb44>[{currentReportReceipt.locationCoverage}]</color>";
            }
            GUILayout.Label($"<b>{ (isEs ? "Ubicación del Incidente:" : "Incident Location:") }</b> {locText}", GetSubheadingStyle());

            if (!string.IsNullOrEmpty(currentReportReceipt.representative))
            {
                GUILayout.Label($"<b>{ (isEs ? "Canal de Atención:" : "Intake Channel:") }</b> {currentReportReceipt.representative}", GetSubheadingStyle());
            }

            GUILayout.Label($"<b>{ (isEs ? "Área de Responsabilidad Simulada (Recomendación):" : "Simulated Area of Responsibility (Recommendation):") }</b> <color=#ffea66>{currentReportReceipt.simulatedDestination}</color>", GetSubheadingStyle());
            GUILayout.Label($"<b>{ (isEs ? "Evidencia Fotográfica:" : "Photographic Evidence:") }</b> {currentReportReceipt.evidenceStatus}", GetMutedStyle());
            GUILayout.Label($"<b>{ (isEs ? "Análisis SAM 3.1:" : "SAM 3.1 Visual Analysis:") }</b> {currentReportReceipt.visualAnalysisStatus}", GetMutedStyle());
            GUILayout.Label($"<b>{ (isEs ? "Estado en World Core:" : "World Core State:") }</b> <color=#55eeff>{currentReportReceipt.lifecycleState}</color> — { (isEs ? "Demostración local (World Core)" : "Local demonstration (World Core)") }", GetSubheadingStyle());
            GUILayout.Label($"<b>{ (isEs ? "Fecha y Hora:" : "Timestamp:") }</b> {currentReportReceipt.createdAt}", GetMutedStyle());
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Simulation Notice
            GUIStyle noticeBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUILayout.Box(isEs
                ? "Servicio municipal simulado — este reporte queda registrado únicamente dentro de la demostración local MyVirtualCommunity. No se envía a ninguna autoridad gubernamental real."
                : "Simulated municipal service — this report is recorded only inside the local MyVirtualCommunity demonstration. It is not submitted to a real government authority.",
                noticeBoxStyle, GUILayout.Height(32));

            GUILayout.Space(10);

            // Bottom Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isEs ? "📄 Registrar Otra Incidencia" : "📄 Register Another Issue", GUILayout.Width(220), GUILayout.Height(32)))
            {
                reportDescriptionInput = "";
                selectedImagePath = null;
                selectedImageFileName = null;
                selectedImageBytes = null;
                selectedImageBase64 = null;
                selectedImageMime = null;
                if (selectedImagePreview != null)
                {
                    Destroy(selectedImagePreview);
                    selectedImagePreview = null;
                }
                currentReportDraft = null;
                currentReportReceipt = null;
                citizenReportStep = CitizenReportWorkflowStep.FormInput;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(isEs ? "Cerrar [Esc]" : "Close [Esc]", GUILayout.Width(130), GUILayout.Height(32)))
            {
                CloseInteractionModal();
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Objective A: Visual Evidence Inspection & SAM 3.1 Overlay Viewer

        private GUIStyle modalBackdropStyle = null;
        private Texture2D modalBackdropTex = null;
        private GUIStyle darkPlateStyle = null;
        private Texture2D darkPlateTex = null;

        private GUIStyle GetModalBackdropStyle()
        {
            if (modalBackdropStyle == null)
            {
                modalBackdropStyle = new GUIStyle();
                modalBackdropTex = new Texture2D(1, 1);
                modalBackdropTex.SetPixel(0, 0, new Color(0.02f, 0.04f, 0.07f, 0.78f));
                modalBackdropTex.Apply();
                modalBackdropStyle.normal.background = modalBackdropTex;
            }
            return modalBackdropStyle;
        }

        private GUIStyle GetDarkPlateStyle()
        {
            if (darkPlateStyle == null)
            {
                darkPlateStyle = new GUIStyle(GUI.skin.box);
                if (darkPlateTex == null)
                {
                    darkPlateTex = new Texture2D(1, 1);
                    darkPlateTex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.09f, 0.95f));
                    darkPlateTex.Apply();
                }
                darkPlateStyle.normal.background = darkPlateTex;
            }
            return darkPlateStyle;
        }

        private void OpenEnlargedImageViewer()
        {
            if (selectedImagePreview == null) return;
            EnsureOverlayTextureLoaded();
            isEnlargedPhotoViewerOpen = true;
        }

        private void EnsureOverlayTextureLoaded()
        {
            string overlayBase64 = currentReportDraft?.visualAnalysis?.overlayPngBase64;
            if (!string.IsNullOrEmpty(overlayBase64))
            {
                if (segmentationOverlayTexture == null || loadedOverlayBase64 != overlayBase64)
                {
                    DestroyOverlayTexture();
                    try
                    {
                        byte[] overlayBytes = Convert.FromBase64String(overlayBase64);
                        segmentationOverlayTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        segmentationOverlayTexture.LoadImage(overlayBytes);
                        loadedOverlayBase64 = overlayBase64;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[SAM 3.1] Error decoding mask overlay texture: " + ex.Message);
                        segmentationOverlayTexture = null;
                        loadedOverlayBase64 = null;
                    }
                }
            }
            else
            {
                DestroyOverlayTexture();
            }
        }

        private void DrawEnlargedPhotoViewerModal(bool isEs)
        {
            if (!isEnlargedPhotoViewerOpen || selectedImagePreview == null) return;

            // Semi-transparent backdrop overlay to obscure background
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, GetModalBackdropStyle());

            // Window Dimensions (responsive, max 960x680)
            float vw = Mathf.Min(Screen.width - 40f, 960f);
            float vh = Mathf.Min(Screen.height - 40f, 680f);
            float vx = (Screen.width - vw) * 0.5f;
            float vy = (Screen.height - vh) * 0.5f;

            Rect windowRect = new Rect(vx, vy, vw, vh);
            GUI.Box(windowRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(vx + 16, vy + 12, vw - 32, vh - 24));

            // Header: Title & Close Button
            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? "🔬 Inspección Visual de Evidencia · Segmentación SAM 3.1" : "🔬 Visual Evidence Inspection · SAM 3.1 Segmentation", GetModalTitleStyle());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕ Cerrar [Esc]", GUILayout.Width(110), GUILayout.Height(24)))
            {
                CloseEnlargedImageViewer();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Controls Row: 3 Display Modes + Bounding Box Toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? "Modo de Vista:" : "Display Mode:", GetSubheadingStyle(), GUILayout.Width(115));

            bool modeOrig = (photoViewerDisplayMode == 0);
            bool modeMask = (photoViewerDisplayMode == 1);
            bool modeComb = (photoViewerDisplayMode == 2);

            GUI.color = modeOrig ? new Color(0.25f, 0.95f, 0.85f) : Color.white;
            if (GUILayout.Button(isEs ? "1. Foto Original" : "1. Original Photo", GUILayout.Height(24)))
            {
                photoViewerDisplayMode = 0;
            }

            GUI.color = modeMask ? new Color(0.25f, 0.95f, 0.85f) : Color.white;
            if (GUILayout.Button(isEs ? "2. Máscara SAM 3.1 (Solo IA)" : "2. SAM 3.1 Mask (AI Only)", GUILayout.Height(24)))
            {
                photoViewerDisplayMode = 1;
            }

            GUI.color = modeComb ? new Color(0.25f, 0.95f, 0.85f) : Color.white;
            if (GUILayout.Button(isEs ? "3. Vista Combinada (Foto + Máscara)" : "3. Combined (Photo + Mask)", GUILayout.Height(24)))
            {
                photoViewerDisplayMode = 2;
            }
            GUI.color = Color.white;

            GUILayout.Space(12);

            showBoundingBoxesOverlay = GUILayout.Toggle(showBoundingBoxesOverlay, isEs ? " Cajas Delimitadoras" : " Bounding Boxes");

            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Status Banner Truth Table (Prompt Section 4.4)
            DrawSegmentationStatusBanner(isEs);

            GUILayout.Space(4);

            // Main Display Area (Aspect Ratio Preserved)
            float availableImageHeight = vh - 180f;
            if (availableImageHeight < 180f) availableImageHeight = 180f;

            Rect displayArea = GUILayoutUtility.GetRect(vw - 32, availableImageHeight);

            float imgAspect = (float)selectedImagePreview.width / (float)selectedImagePreview.height;
            float areaAspect = displayArea.width / displayArea.height;
            float drawW, drawH;
            if (imgAspect > areaAspect)
            {
                drawW = displayArea.width;
                drawH = displayArea.width / imgAspect;
            }
            else
            {
                drawH = displayArea.height;
                drawW = displayArea.height * imgAspect;
            }

            float drawX = displayArea.x + (displayArea.width - drawW) * 0.5f;
            float drawY = displayArea.y + (displayArea.height - drawH) * 0.5f;
            Rect targetRect = new Rect(drawX, drawY, drawW, drawH);

            // Dark neutral letterbox backing
            GUI.Box(targetRect, GUIContent.none, GetDarkPlateStyle());

            bool hasMask = (currentReportDraft?.visualAnalysis != null && currentReportDraft.visualAnalysis.hasAuthenticMasks && segmentationOverlayTexture != null);

            if (photoViewerDisplayMode == 0) // Original photo only
            {
                GUI.DrawTexture(targetRect, selectedImagePreview, ScaleMode.StretchToFill);
            }
            else if (photoViewerDisplayMode == 1) // Segmentation mask only
            {
                if (hasMask)
                {
                    GUI.DrawTexture(targetRect, segmentationOverlayTexture, ScaleMode.StretchToFill, true);
                }
                else
                {
                    GUIStyle noMaskMsgStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }
                    };
                    GUI.Label(targetRect, isEs ? "Máscara de segmentación por píxeles no disponible para este estado." : "Pixel segmentation mask not available for this state.", noMaskMsgStyle);
                }
            }
            else // Combined (Photo + Mask)
            {
                GUI.DrawTexture(targetRect, selectedImagePreview, ScaleMode.StretchToFill);
                if (hasMask)
                {
                    GUI.DrawTexture(targetRect, segmentationOverlayTexture, ScaleMode.StretchToFill, true);
                }
            }

            // Optional Bounding Boxes with Labels & Confidences
            if (showBoundingBoxesOverlay && currentReportDraft?.visualAnalysis?.boxes != null && currentReportDraft.visualAnalysis.boxes.Length > 0)
            {
                var va = currentReportDraft.visualAnalysis;
                for (int i = 0; i < va.boxes.Length; i++)
                {
                    var b = va.boxes[i];
                    float bx = targetRect.x + b.xmin * targetRect.width;
                    float by = targetRect.y + b.ymin * targetRect.height;
                    float bw = (b.xmax - b.xmin) * targetRect.width;
                    float bh = (b.ymax - b.ymin) * targetRect.height;

                    Color boxColor = Color.cyan;
                    if (va.objects != null && i < va.objects.Length && !string.IsNullOrEmpty(va.objects[i].colorHex))
                    {
                        ColorUtility.TryParseHtmlString(va.objects[i].colorHex, out boxColor);
                    }

                    GUIStyle outlineStyle = new GUIStyle(GUI.skin.box);
                    outlineStyle.normal.textColor = boxColor;
                    GUI.color = boxColor;
                    string label = b.label ?? $"obj_{i}";
                    if (b.confidence > 0.01f)
                    {
                        label += $" ({(b.confidence * 100f):F0}%)";
                    }
                    GUI.Box(new Rect(bx, by, bw, bh), label, outlineStyle);
                    GUI.color = Color.white;
                }
            }

            GUILayout.Space(6);

            // Bottom Legend & Object Inspector
            DrawDetectedObjectsLegend(isEs);

            GUILayout.EndArea();
        }

        private void DrawSegmentationStatusBanner(bool isEs)
        {
            var va = currentReportDraft?.visualAnalysis;
            string bannerText;
            Color bannerColor;

            if (va != null && va.hasAuthenticMasks && segmentationOverlayTexture != null)
            {
                int count = va.objects != null ? va.objects.Length : (va.boxes != null ? va.boxes.Length : 1);
                bannerText = isEs
                    ? $"✅ Segmentación verificable: {count} región(es) detectada(s) con máscaras de píxeles generadas por SAM 3.1."
                    : $"✅ Verifiable segmentation: {count} region(s) detected with authentic pixel masks generated by SAM 3.1.";
                bannerColor = new Color(0.25f, 0.95f, 0.45f);
            }
            else if (va != null && va.boxes != null && va.boxes.Length > 0 && !va.hasAuthenticMasks)
            {
                bannerText = isEs
                    ? $"⚠️ Delimitación por cuadros únicamente: {va.boxes.Length} región(es) detectada(s) sin máscaras de píxeles individuales."
                    : $"⚠️ Bounding boxes only: {va.boxes.Length} region(s) detected without individual pixel masks.";
                bannerColor = new Color(1.0f, 0.85f, 0.25f);
            }
            else if (va != null && (va.provider == "not-authorized" || (currentReportDraft != null && !currentReportDraft.externalAiProcessingConsent)))
            {
                bannerText = isEs
                    ? "🔒 Privacidad: Análisis visual externo no autorizado por el usuario (evidencia fotográfica preservada localmente sin inferencia externa)."
                    : "🔒 Privacy: External visual analysis not authorized by user (photo preserved locally without external AI inference).";
                bannerColor = new Color(0.4f, 0.85f, 1f);
            }
            else if (va != null && (va.provider.Contains("unavailable") || va.status == "unavailable"))
            {
                bannerText = isEs
                    ? "⚠️ Servicio no disponible: No se pudo conectar con el modelo SAM 3.1 en este momento."
                    : "⚠️ Service unavailable: Could not connect to SAM 3.1 model at this time.";
                bannerColor = new Color(1f, 0.55f, 0.35f);
            }
            else if (va != null && va.status == "inconclusive" && (va.boxes == null || va.boxes.Length == 0))
            {
                bannerText = isEs
                    ? "ℹ️ Sin detecciones coincidentes: SAM 3.1 no detectó regiones que correspondan al concepto en esta fotografía."
                    : "ℹ️ Zero matching detections: SAM 3.1 found no candidate regions matching the target concept in this photograph.";
                bannerColor = new Color(0.7f, 0.85f, 0.95f);
            }
            else
            {
                bannerText = isEs
                    ? "ℹ️ Geometría de segmentación no disponible o datos no válidos."
                    : "ℹ️ Segmentation geometry not available or invalid data.";
                bannerColor = new Color(0.75f, 0.75f, 0.75f);
            }

            GUIStyle bStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = bannerColor }
            };
            GUILayout.Box(bannerText, bStyle, GUILayout.Height(26));
        }

        private void DrawDetectedObjectsLegend(bool isEs)
        {
            var va = currentReportDraft?.visualAnalysis;
            if (va == null) return;

            if (va.objects != null && va.objects.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(isEs ? "Regiones Detectadas:" : "Detected Regions:", GetSubheadingStyle(), GUILayout.Width(130));

                for (int i = 0; i < va.objects.Length; i++)
                {
                    var obj = va.objects[i];
                    Color chipCol = Color.cyan;
                    if (!string.IsNullOrEmpty(obj.colorHex))
                    {
                        ColorUtility.TryParseHtmlString(obj.colorHex, out chipCol);
                    }
                    GUI.color = chipCol;
                    string confStr = obj.confidence > 0.01f ? $" ({(obj.confidence * 100f):F0}%)" : "";
                    string chipText = $"■ {obj.label} [{obj.objectId}{confStr}]";
                    GUILayout.Box(chipText, GUILayout.Height(22));
                    GUI.color = Color.white;
                    GUILayout.Space(6);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else if (va.boxes != null && va.boxes.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(isEs ? "Cajas Delimitadoras:" : "Bounding Boxes:", GetSubheadingStyle(), GUILayout.Width(130));
                for (int i = 0; i < va.boxes.Length; i++)
                {
                    var b = va.boxes[i];
                    GUI.color = new Color(1.0f, 0.85f, 0.25f);
                    string confStr = b.confidence > 0.01f ? $" ({(b.confidence * 100f):F0}%)" : "";
                    string chipText = $"▢ {b.label ?? "región"} [{i + 1}{confStr}]";
                    GUILayout.Box(chipText, GUILayout.Height(22));
                    GUI.color = Color.white;
                    GUILayout.Space(6);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void OnDestroy()
        {
            DestroyOverlayTexture();
            if (modalBackdropTex != null)
            {
                Destroy(modalBackdropTex);
                modalBackdropTex = null;
            }
            if (darkPlateTex != null)
            {
                Destroy(darkPlateTex);
                darkPlateTex = null;
            }
            if (selectedImagePreview != null)
            {
                Destroy(selectedImagePreview);
                selectedImagePreview = null;
            }
        }

        #endregion
    }
}
