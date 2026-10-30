using System;
using System.Collections.Generic;
using UnityEngine;
using MyVirtualCommunity.Client.Api;

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
                        CloseInteractionModal();
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
        }

        public void CloseInteractionModal()
        {
            activeModalPoint = null;
            currentState = PrimaryUIState.Exploration;
            requestErrorMessage = null;
            currentInputText = "";
            LockCursor(true);
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

                    string markerText = response.response.provenance?.marker?.Get(currentLanguage);
                    string baseBadge = !string.IsNullOrEmpty(markerText)
                        ? $"🎒 {markerText}"
                        : ((currentLanguage == "es") ? "🎒 Testigo Sintético Simulado" : "🎒 Synthetic Simulated Witness");
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
            requestErrorMessage = error ?? "Unknown server communication failure.";
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
            GUI.Box(rect, GUIContent.none);

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
            GUI.Box(modalRect, GUIContent.none);

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
            GUI.Box(cardRect, GUIContent.none);

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

            const float width = 720;
            const float height = 580;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 16, y + 14, width - 32, height - 28));

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

            GUILayout.Space(6);

            // 3. Special Case: Official Border Intelligence Panel for Elena's Kiosk
            if (point.category == InteractionCategory.BorderConcierge)
            {
                DrawOfficialBorderIntelligenceSection();
                GUILayout.Space(6);
            }

            // 4. Conversation History Area (Scrollable)
            float chatBoxHeight = (point.category == InteractionCategory.BorderConcierge) ? 190 : 280;
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
            }
            else
            {
                GUILayout.Label(isEs ? "Presione 'Actualizar' para consultar CBP." : "Click 'Refresh' to fetch official CBP wait times.", GetMutedStyle());
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

            GUILayout.BeginHorizontal();
            GUILayout.Label((currentLanguage == "es") ? "Preguntas sugeridas:" : "Quick prompts:", GetMutedStyle(), GUILayout.Width(110));

            foreach (var prompt in prompts)
            {
                if (GUILayout.Button(prompt, GUILayout.Height(22)))
                {
                    currentInputText = prompt;
                    if (!isRequestPending)
                    {
                        SendUserMessage();
                    }
                }
                GUILayout.Space(4);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region GUI Styles Cache

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
    }
}
