using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
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

        // Dynamic Modal Auto-Height & Scroll State
        private float dynamicControlsHeight = 520f;
        private Vector2 controlsGuideScrollPos = Vector2.zero;
        private float dynamicPauseMenuHeight = 420f;
        private Vector2 pauseMenuScrollPos = Vector2.zero;
        private float dynamicSettingsHeight = 650f;
        private Vector2 settingsDrawerScrollPos = Vector2.zero;

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

        public enum BorderConciergeViewMode
        {
            Conversation,
            OfficialWaitTimes
        }
        private BorderConciergeViewMode borderConciergeViewMode = BorderConciergeViewMode.Conversation;

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

        public enum CitizenReportFormSubStep
        {
            Details,
            LocationAndEvidence
        }

        private CitizenReportWorkflowStep citizenReportStep = CitizenReportWorkflowStep.Consultation;
        private CitizenReportFormSubStep citizenReportFormSubStep = CitizenReportFormSubStep.Details;
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

        // Authoritative NPC Activity & Routine Tracking (Increment 5.14.0)
        private float lastNpcActivityRefreshTime = -999f;
        private const float NPC_ACTIVITY_REFRESH_INTERVAL = 30f;

        // Authoritative Time-of-Day Environmental Controller (Increment 5.15.0)
        private TimeOfDayEnvironmentController environmentController;

        // Authoritative NPC Waypoint Movement Controller (Increment 5.16.0)
        private NpcWaypointMovementController movementController;

        // Secret Fideicomiso Surveillance Camera Easter Egg (Paso del Norte - Vista Sur -> YouTube Live)
        private const string FideicomisoCameraObjectName = "Bridge_Fideicomiso_Camera_VistaSur";
        private const string FideicomisoYoutubeLiveUrl = "https://www.youtube.com/watch?v=IcvugJWPXz8";
        private const string FideicomisoYoutubeThumbUrl = "https://i.ytimg.com/vi/IcvugJWPXz8/hqdefault.jpg";
        private bool isFideicomisoCameraPipOpen = false;
        private bool fideicomisoPipShowRealYoutubeThumb = false; // false = Live 3D CCTV Mirror + Overlay, true = Real YouTube Stream Thumbnail
        private Camera fideicomisoCctvCamera = null;
        private RenderTexture fideicomisoCctvRenderTexture = null;
        private Texture2D fideicomisoYoutubeThumbTex = null;
        private bool isFetchingFideicomisoYoutubeThumb = false;
        private GameObject playerSelfCctvProxy = null;
        private Rect fideicomisoPipWindowRect = new Rect(0, 0, 540, 368);

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

        // Ambient & Navigation Settings (Increment 5.13.4)
        private float masterVolume = 1.0f;
        private bool isAudioMuted = false;
        private bool isSettingsDrawerOpen = false;
        private bool showCompass = true;
        private bool showMinimap = true;
        private bool showLandmarkHUD = true;
        private InteractionPoint[] cachedInteractionPoints = null;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                ApplyAudioSettings();
                SaveAudioSettings();
            }
        }

        public bool IsAudioMuted
        {
            get => isAudioMuted;
            set
            {
                isAudioMuted = value;
                ApplyAudioSettings();
                SaveAudioSettings();
            }
        }

        public bool IsSettingsDrawerOpen => isSettingsDrawerOpen;

        public bool ShowCompass
        {
            get => showCompass;
            set
            {
                showCompass = value;
                PlayerPrefs.SetInt("mvc_show_compass", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool ShowMinimap
        {
            get => showMinimap;
            set
            {
                showMinimap = value;
                PlayerPrefs.SetInt("mvc_show_minimap", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool ShowLandmarkHUD
        {
            get => showLandmarkHUD;
            set
            {
                showLandmarkHUD = value;
                PlayerPrefs.SetInt("mvc_show_landmarks", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void OpenSettingsDrawer() => isSettingsDrawerOpen = true;
        public void CloseSettingsDrawer() => isSettingsDrawerOpen = false;

        // Performance & Hardware Safety Settings (Thermal Emergency Protection)
        private int targetFramerate = 60;
        private float fpsMeasurementTimer = 0f;
        private int fpsFrameCount = 0;
        private int currentFpsDisplay = 60;

        public int TargetFramerate
        {
            get => targetFramerate;
            set
            {
                targetFramerate = (value == 30) ? 30 : 60;
                Application.targetFrameRate = targetFramerate;
                QualitySettings.vSyncCount = 1;
                PlayerPrefs.SetInt("mvc_target_fps", targetFramerate);
                PlayerPrefs.Save();
            }
        }
        public int CurrentFpsDisplay => currentFpsDisplay;

        public string GetCurrentLocationContext(bool isEs) => GetDynamicLocationContext(isEs);

        private string GetDynamicLocationContext(bool isEs)
        {
            Vector3 pos = transform.position;
            string sidewalk;
            if (pos.x < -4.5f)
            {
                sidewalk = isEs ? "Acera Poniente (Oeste)" : "West Sidewalk";
            }
            else if (pos.x > 4.5f)
            {
                sidewalk = isEs ? "Acera Oriente (Este)" : "East Sidewalk";
            }
            else
            {
                sidewalk = isEs ? "Arroyo Vehicular" : "Roadway";
            }

            string zone;
            if (pos.z > 30f)
            {
                zone = isEs ? "Puente Internacional Paso del Norte (Kiosco CBP)" : "Paso del Norte Bridge Entrance (CBP)";
            }
            else if (pos.z >= 10f)
            {
                zone = isEs ? "Sector Atención Ciudadana" : "Citizen Service Sector";
            }
            else if (pos.z >= -10f)
            {
                zone = isEs ? "Sector Comercial Central" : "Central Commercial Sector";
            }
            else
            {
                zone = isEs ? "Entrada Sur (Zona de Inicio)" : "South Entrance (Spawn Zone)";
            }

            return $"Av. Benito Juárez • {sidewalk} • {zone}";
        }
        public float GetPlayerHeading()
        {
            float y = playerCamera != null ? playerCamera.eulerAngles.y : transform.eulerAngles.y;
            return (y % 360f + 360f) % 360f;
        }

        private void ApplyAudioSettings()
        {
            AudioListener.volume = isAudioMuted ? 0.0f : masterVolume;
            AudioListener.pause = isAudioMuted;
        }

        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("mvc_master_volume", masterVolume);
            PlayerPrefs.SetInt("mvc_audio_muted", isAudioMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public InteractionPoint ActiveModalPoint => activeModalPoint;
        public int GetDialogueMessageCount(string characterId) => dialogueHistories.ContainsKey(characterId) ? dialogueHistories[characterId].Count : 0;
        public DialogueMessage GetLastMessage(string characterId) => (dialogueHistories.ContainsKey(characterId) && dialogueHistories[characterId].Count > 0) ? dialogueHistories[characterId][dialogueHistories[characterId].Count - 1] : null;
        public string RequestErrorMessage => requestErrorMessage;
        public bool IsRequestPending => isRequestPending;
        public string FormatProviderBadgePublic(string provider, string categoryBadge, string language) => FormatProviderBadge(provider, categoryBadge, language);

        private void Awake()
        {
            // Hardware thermal safeguard: enforce VSync and cap framerate to 60 FPS
            // Prevents uncapped render loops (500-1500+ FPS) from driving GPU/CPU TDP into thermal emergency shutdown (Event 41)
            targetFramerate = PlayerPrefs.GetInt("mvc_target_fps", 60);
            if (targetFramerate != 30 && targetFramerate != 45 && targetFramerate != 60) targetFramerate = 60;
            PlayerPrefs.SetInt("mvc_target_fps", targetFramerate);
            PlayerPrefs.Save();
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = targetFramerate;

            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
            }

            // Always enforce adult standing eye-level height (1.60m) on player camera
            if (playerCamera != null)
            {
                playerCamera.localPosition = new Vector3(0f, 1.60f, 0f);
                playerCamera.localRotation = Quaternion.identity;
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
            // Load audio & navigation preferences
            masterVolume = PlayerPrefs.GetFloat("mvc_master_volume", 1.0f);
            isAudioMuted = PlayerPrefs.GetInt("mvc_audio_muted", 0) == 1;
            showCompass = PlayerPrefs.GetInt("mvc_show_compass", 1) == 1;
            showMinimap = PlayerPrefs.GetInt("mvc_show_minimap", 1) == 1;
            showLandmarkHUD = PlayerPrefs.GetInt("mvc_show_landmarks", 1) == 1;
            ApplyAudioSettings();

            cachedInteractionPoints = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);

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
            RefreshNpcRosterActivity();
            EnsureEnvironmentController();
            EnsureMovementController();
        }

        private void Update()
        {
            fpsFrameCount++;
            fpsMeasurementTimer += Time.unscaledDeltaTime;
            if (fpsMeasurementTimer >= 0.5f)
            {
                currentFpsDisplay = Mathf.RoundToInt(fpsFrameCount / fpsMeasurementTimer);
                fpsFrameCount = 0;
                fpsMeasurementTimer = 0f;
            }

            if (Time.time - lastNpcActivityRefreshTime >= NPC_ACTIVITY_REFRESH_INTERVAL)
            {
                lastNpcActivityRefreshTime = Time.time;
                RefreshNpcRosterActivity();
            }

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
                        if (isSettingsDrawerOpen)
                        {
                            isSettingsDrawerOpen = false;
                        }
                        else
                        {
                            ResumeFromPauseMenu();
                        }
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
                        if (isFideicomisoCameraPipOpen)
                        {
                            CloseFideicomisoCameraEasterEgg();
                            return;
                        }
                        OpenPauseMenu();
                        return;
                    }
                    if (WasF1Pressed())
                    {
                        OpenControlsGuide(PrimaryUIState.Exploration);
                        return;
                    }
                    if (WasToggleMapPressed())
                    {
                        ShowMinimap = !showMinimap;
                    }

                    // Secret Easter Egg: clicking on Bridge_Fideicomiso_Camera_VistaSur opens the live CCTV / YouTube PiP monitor
                    if (WasMouseClickPressed())
                    {
                        if (TryTriggerFideicomisoCameraEasterEggClick())
                        {
                            break;
                        }

                        if (!isCursorLocked)
                        {
                            // Keep cursor unlocked if the player clicked inside the open CCTV PiP window
                            if (!isFideicomisoCameraPipOpen || !IsMouseInsideFideicomisoPipWindow())
                            {
                                LockCursor(true);
                            }
                        }
                    }

                    if (isFideicomisoCameraPipOpen && WasTogglePipCursorKeyPressed())
                    {
                        LockCursor(!isCursorLocked);
                    }

                    HandleLook();
                    HandleMovement();
                    UpdateFideicomisoPlayerSelfAvatarProxy();
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
            if (cachedInteractionPoints == null || cachedInteractionPoints.Length == 0)
            {
                cachedInteractionPoints = FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            }
            InteractionPoint[] points = cachedInteractionPoints;
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

            RefreshIndividualNpcActivity(point);
            RefreshIndividualNpcInteractionContext(point);
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
                string ctxGreeting = point.GetContextualGreeting(currentLanguage);
                string greeting = !string.IsNullOrEmpty(ctxGreeting) ? ctxGreeting : GetDefaultGreeting(point);
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
                        ? "[CBP] Conserje Fronterizo Virtual · Basado en datos oficiales CBP"
                        : "[CBP] AI Border Concierge · Grounded in official CBP observations";

                case InteractionCategory.SyntheticWitness:
                    return isEs
                        ? "[Testigo] Testigo Comunitario Sintético · Simulación de escenario local"
                        : "[Witness] Synthetic Community Witness · Simulated local scenario";

                case InteractionCategory.StorefrontAttendant:
                    return isEs
                        ? "[Comercio] Encargado Virtual · Basado en horarios verificados de World Core"
                        : "[Storefront] AI Storefront Attendant · Grounded in verified World Core snapshot";

                case InteractionCategory.CitizenService:
                    return isEs
                        ? "[Atención] Enlace de Atención Ciudadana · Servicio Municipal Simulado (Demostración)"
                        : "[Service] Citizen Service Representative · Simulated Municipal Demonstration";

                default:
                    return isEs ? "[Lugar] Información del Lugar" : "[Landmark] Landmark Information";
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
                avatar = (currentLanguage == "es") ? "[Tú]" : "[You]",
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

        private string FormatSafeAvatar(string rawAvatar, string characterName = null)
        {
            if (string.IsNullOrEmpty(rawAvatar)) return "[NPC]";
            string trimmed = rawAvatar.Trim();

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                return trimmed;
            }

            switch (trimmed)
            {
                case "👤":
                case "User":
                case "Player":
                    return (currentLanguage == "es") ? "[Tú]" : "[You]";
                case "💊":
                    return "[Farmacia]";
                case "👓":
                    return "[Óptica]";
                case "🛂":
                    return "[CBP]";
                case "🎒":
                case "🎓":
                    return "[Testigo]";
                case "💼":
                    return "[Comercio]";
                case "🪵":
                case "👴":
                    return "[Testigo]";
                case "📋":
                    return "[Atención]";
                case "🏪":
                    return "[Comercio]";
                default:
                    foreach (char c in trimmed)
                    {
                        if (c > 127) return "[NPC]";
                    }
                    return trimmed;
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
                        ? "[CBP] Conserje Fronterizo Virtual · Basado en observaciones oficiales CBP"
                        : "[CBP] AI Border Concierge · Grounded in official CBP observations";
                    string badge = FormatProviderBadge(response.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.concierge?.name ?? "Elena Ramos",
                        avatar = FormatSafeAvatar(response.concierge?.avatar, "Elena Ramos"),
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
                    ? $"He tomado nota de su consulta sobre: \"{message}\". Para canalizar formalmente esta incidencia simulada a la Dirección de Obras Públicas y procesar su fotografía con segmentación SAM 3.1, seleccione la pestaña '[Reporte] Registrar Reporte Ciudadano' arriba."
                    : $"I've noted your inquiry regarding: \"{message}\". To formally route this simulated issue to Public Works and process your photograph with SAM 3.1 visual segmentation, please select the '[Report] Register Citizen Report' tab above.";
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
                ? "[Atención] Enlace de Atención Ciudadana · Servicio Municipal Simulado"
                : "[Service] Citizen Service Representative · Simulated Municipal Service";

            dialogueHistories[expectedCharKey].Add(new DialogueMessage
            {
                isUser = false,
                senderName = "Lic. Patricia Morales",
                avatar = "[Atención]",
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
                        ? "[Testigo] Perspectiva de testigo simulado ficticio — no es una medición oficial"
                        : "[Witness] Fictional simulated witness perspective — not an official measurement";
                    string badge = FormatProviderBadge(response.response.provenance?.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.response.witnessName ?? expectedCharKey,
                        avatar = FormatSafeAvatar(response.response.avatar, response.response.witnessName ?? expectedCharKey),
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
                        ? "[Comercio] Encargado Virtual · Basado en horarios verificados de World Core (Rechaza recetas/inventario)"
                        : "[Storefront] AI Storefront Attendant · Grounded in verified World Core snapshot (Refuses medical/inventory)";
                    string badge = FormatProviderBadge(response.response.provider, baseBadge, currentLanguage);

                    dialogueHistories[expectedCharKey].Add(new DialogueMessage
                    {
                        isUser = false,
                        senderName = response.response.attendantName ?? expectedCharKey,
                        avatar = FormatSafeAvatar(activeModalPoint != null ? activeModalPoint.avatar : "[Comercio]", response.response.attendantName ?? expectedCharKey),
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

        public void RefreshNpcRosterActivity()
        {
            EnsureApiClient();
            if (apiClient == null) return;

            InteractionPoint[] points = (cachedInteractionPoints != null && cachedInteractionPoints.Length > 0)
                ? cachedInteractionPoints
                : FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);

            if (points == null || points.Length == 0) return;

            foreach (var pt in points)
            {
                if (pt == null || string.IsNullOrEmpty(pt.characterId)) continue;
                RefreshIndividualNpcActivity(pt);
                RefreshIndividualNpcInteractionContext(pt);
            }
        }

        public void RefreshIndividualNpcInteractionContext(InteractionPoint pt)
        {
            if (pt == null || string.IsNullOrEmpty(pt.characterId)) return;
            EnsureApiClient();
            if (apiClient == null) return;

            string charId = pt.characterId;
            apiClient.GetNpcInteractionContext(
                charId,
                res =>
                {
                    var ctx = res != null ? (res.context ?? res.interactionContext) : null;
                    if (ctx != null && pt != null)
                    {
                        pt.interactionAvailability = ctx.interactionAvailability ?? "available_normal";
                        pt.interactionMode = ctx.interactionMode ?? "normal_service";
                        pt.isAvailableForNormalService = ctx.isAvailableForNormalService;
                        pt.canInteract = ctx.canInitiateConversation;
                        if (!string.IsNullOrEmpty(ctx.activityState))
                        {
                            pt.activityState = ctx.activityState;
                        }
                        if (ctx.activityTitle != null)
                        {
                            pt.activityTitleEn = ctx.activityTitle.en ?? pt.activityTitleEn;
                            pt.activityTitleEs = ctx.activityTitle.es ?? pt.activityTitleEs;
                        }
                        if (ctx.activityDescription != null)
                        {
                            pt.activityDescriptionEn = ctx.activityDescription.en ?? pt.activityDescriptionEn;
                            pt.activityDescriptionEs = ctx.activityDescription.es ?? pt.activityDescriptionEs;
                        }
                        if (ctx.decisionReason != null)
                        {
                            pt.decisionReasonEn = ctx.decisionReason.en ?? "";
                            pt.decisionReasonEs = ctx.decisionReason.es ?? "";
                        }
                        if (ctx.dialogueGuidance != null)
                        {
                            pt.dialogueGuidanceEn = ctx.dialogueGuidance.en ?? "";
                            pt.dialogueGuidanceEs = ctx.dialogueGuidance.es ?? "";
                        }
                        if (ctx.contextualGreeting != null)
                        {
                            pt.contextualGreetingEn = ctx.contextualGreeting.en ?? "";
                            pt.contextualGreetingEs = ctx.contextualGreeting.es ?? "";
                        }
                        if (!string.IsNullOrEmpty(ctx.movementState))
                        {
                            pt.movementState = ctx.movementState;
                        }
                        if (!string.IsNullOrEmpty(ctx.currentWaypointId))
                        {
                            pt.currentWaypointId = ctx.currentWaypointId;
                        }
                        if (!string.IsNullOrEmpty(ctx.destinationWaypointId))
                        {
                            pt.destinationWaypointId = ctx.destinationWaypointId;
                        }
                        pt.progression = ctx.progression;
                    }
                },
                err =>
                {
                    // Fail gracefully: retain existing local or default state
                }
            );
        }

        public void RefreshIndividualNpcActivity(InteractionPoint pt)
        {
            if (pt == null || string.IsNullOrEmpty(pt.characterId)) return;
            EnsureApiClient();
            if (apiClient == null) return;

            string charId = pt.characterId;
            apiClient.GetNpcActivity(
                charId,
                res =>
                {
                    if (res != null && res.success && res.activity != null && pt != null)
                    {
                        pt.activityState = res.activity.state ?? "available";
                        pt.canInteract = res.activity.canInteract;
                        if (res.activity.activityTitle != null)
                        {
                            pt.activityTitleEn = res.activity.activityTitle.en ?? "";
                            pt.activityTitleEs = res.activity.activityTitle.es ?? "";
                        }
                        if (res.activity.activityDescription != null)
                        {
                            pt.activityDescriptionEn = res.activity.activityDescription.en ?? "";
                            pt.activityDescriptionEs = res.activity.activityDescription.es ?? "";
                        }
                        if (res.activity.interactionNotice != null)
                        {
                            pt.interactionNoticeEn = res.activity.interactionNotice.en ?? "";
                            pt.interactionNoticeEs = res.activity.interactionNotice.es ?? "";
                        }
                    }
                },
                err =>
                {
                    // Fail gracefully: retain existing local or default state
                }
            );
        }

        public void EnsureEnvironmentController()
        {
            if (environmentController == null)
            {
                environmentController = FindAnyObjectByType<TimeOfDayEnvironmentController>();
                if (environmentController == null)
                {
                    GameObject envGo = new GameObject("TimeOfDayEnvironmentController");
                    environmentController = envGo.AddComponent<TimeOfDayEnvironmentController>();
                }
            }
        }

        public void EnsureMovementController()
        {
            if (movementController == null)
            {
                movementController = FindAnyObjectByType<NpcWaypointMovementController>();
                if (movementController == null)
                {
                    GameObject movGo = new GameObject("NpcWaypointMovementController");
                    movementController = movGo.AddComponent<NpcWaypointMovementController>();
                }
            }
        }

        #endregion

        #region Input Dual-Compatibility Helpers

        public void OpenPauseMenu()
        {
            if (currentState == PrimaryUIState.Dialogue) return;
            isSettingsDrawerOpen = false;
            currentState = PrimaryUIState.PauseMenu;
            LockCursor(false);
        }

        public void ResumeFromPauseMenu()
        {
            isSettingsDrawerOpen = false;
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

        private bool WasToggleMapPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.M)) return true;
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
                    DrawModalScrim();
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
                    DrawModalScrim();
                    if (isSettingsDrawerOpen)
                    {
                        DrawSettingsDrawer();
                    }
                    else
                    {
                        DrawPauseMenu();
                    }
                    break;

                case PrimaryUIState.ControlsGuide:
                    DrawHUD();
                    DrawModalScrim();
                    DrawControlsOverlay();
                    break;

                case PrimaryUIState.Exploration:
                default:
                    DrawReticle();
                    DrawHUD();
                    if (showCompass) DrawCompassStrip();
                    if (showLandmarkHUD) DrawNearbyLandmarksHUD();
                    if (showMinimap) DrawCorridorMinimap();
                    DrawWorldSpaceNPCNameplates();
                    if (currentNearbyPoint != null && isCursorLocked)
                    {
                        DrawInteractionPrompt(currentNearbyPoint);
                    }
                    if (isFideicomisoCameraPipOpen)
                    {
                        DrawFideicomisoCameraEasterEggPiP();
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

            // Dynamic Location Header with directional guide
            string locationText = GetDynamicLocationContext(isEs);
            GUILayout.Label(locationText, GetLocationHeaderStyle());

            // Time-of-Day Environmental HUD Indicator (Increment 5.15.0)
            if (environmentController != null)
            {
                GUILayout.Space(8);
                GUIStyle timeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = environmentController.GetHUDColor() }
                };
                GUILayout.Label(environmentController.GetFormattedHUDString(isEs), timeStyle);
            }

            GUILayout.FlexibleSpace();

            // Help [F1] Toggle in HUD (hidden during dialogue to prevent interruption)
            if (currentState != PrimaryUIState.Dialogue)
            {
                string helpBtnLabel = (currentState == PrimaryUIState.ControlsGuide)
                    ? (isEs ? "Ayuda [F1] [OK]" : "Help [F1] [OK]")
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

            // FPS Counter badge (Thermal Health indicator)
            GUIStyle fpsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.85f, 1f) }
            };
            GUILayout.Label($"[{currentFpsDisplay} FPS]", fpsStyle);

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
                Rect hintsRect = new Rect(15, Screen.height - 40, 680, 30);
                string hints = isEs
                    ? "WASD: Moverse | Shift: Correr | E: Hablar | M: Minimapa | Esc: Pausa y Ajustes | F1: Guía"
                    : "WASD: Move | Shift: Sprint | E: Interact | M: Minimap | Esc: Pause & Settings | F1: Guide";
                GUI.Label(hintsRect, hints, GetMutedStyle());
            }
        }

        private void DrawWorldSpaceNPCNameplates()
        {
            if (currentState != PrimaryUIState.Exploration || playerCamera == null) return;

            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            InteractionPoint[] points = (cachedInteractionPoints != null && cachedInteractionPoints.Length > 0)
                ? cachedInteractionPoints
                : FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
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
                string safeAv = FormatSafeAvatar(pt.avatar, charName);
                bool isEs = (currentLanguage == "es");

                string actBadge = "";
                Color nameplateTextColor;

                if (pt.interactionAvailability == "available_in_transit" || pt.movementState == "moving")
                {
                    actBadge = isEs ? " [En tránsito]" : " [In Transit]";
                    nameplateTextColor = isNearby ? new Color(0.4f, 0.95f, 1f) : new Color(0.5f, 0.85f, 0.95f);
                }
                else if (pt.interactionAvailability == "available_contextual" || pt.movementState == "arrived" || pt.movementState == "waiting")
                {
                    actBadge = isEs ? " [Descanso / Actividad]" : " [Break / Activity]";
                    nameplateTextColor = isNearby ? new Color(1f, 0.95f, 0.35f) : new Color(1f, 0.85f, 0.45f);
                }
                else if (pt.interactionAvailability == "busy" || pt.activityState == "busy")
                {
                    actBadge = isEs ? " [Ocupado]" : " [Busy]";
                    nameplateTextColor = isNearby ? new Color(1f, 0.95f, 0.2f) : new Color(1f, 0.85f, 0.35f);
                }
                else if (pt.interactionAvailability == "off_duty" || pt.activityState == "off_duty")
                {
                    actBadge = isEs ? " [Fuera de turno]" : " [Off Duty]";
                    nameplateTextColor = isNearby ? new Color(1f, 0.85f, 0.4f) : new Color(0.72f, 0.76f, 0.82f);
                }
                else if (pt.interactionAvailability == "unavailable" || pt.activityState == "unavailable")
                {
                    actBadge = isEs ? " [Cerrado]" : " [Closed]";
                    nameplateTextColor = isNearby ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.6f, 0.6f, 0.65f);
                }
                else
                {
                    actBadge = isEs ? " [Disponible]" : " [Available]";
                    nameplateTextColor = isNearby ? new Color(0.35f, 1f, 0.45f) : new Color(0.9f, 0.95f, 1f, 0.92f);
                }

                string labelText = isNearby
                    ? $"[E] {safeAv} {charName}{actBadge}"
                    : $"{safeAv} {charName}{actBadge}";

                float badgeWidth = Mathf.Clamp(labelText.Length * 8f + 22f, 120f, 320f);
                float badgeHeight = 22f;
                Rect badgeRect = new Rect(screenX - (badgeWidth * 0.5f), screenY - (badgeHeight * 0.5f), badgeWidth, badgeHeight);

                GUIStyle nameplateStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = isNearby ? FontStyle.Bold : FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = nameplateTextColor }
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
            string safePromptAv = FormatSafeAvatar(point.avatar, charName);
            string avatarPrefix = !string.IsNullOrEmpty(safePromptAv) ? $"{safePromptAv} " : "";
            GUILayout.Label($"[E] {actionVerb} {avatarPrefix}{charName}", promptStyle);

            GUILayout.FlexibleSpace();

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            string statusTag = "";
            string promptBadge = point.GetInteractionAvailabilityBadge(currentLanguage);
            string act = point.GetActivityTitle(currentLanguage);

            if (point.interactionAvailability == "available_in_transit" || point.movementState == "moving")
            {
                statusTag = $" • {promptBadge} ({act})";
            }
            else if (point.interactionAvailability == "available_contextual" || point.movementState == "arrived" || point.movementState == "waiting")
            {
                statusTag = $" • {promptBadge} ({act})";
            }
            else if (point.interactionAvailability == "busy" || point.activityState == "busy")
            {
                statusTag = $" • {promptBadge} ({act})";
            }
            else if (point.interactionAvailability == "off_duty" || point.activityState == "off_duty")
            {
                statusTag = $" • {promptBadge}";
            }
            else if (point.interactionAvailability == "unavailable" || point.activityState == "unavailable")
            {
                statusTag = $" • {promptBadge}";
            }
            else
            {
                statusTag = $" • {promptBadge}";
            }

            string subText = isEs ? $"{point.subtitle}{statusTag} • Presiona E" : $"{point.subtitle}{statusTag} • Press E";
            GUILayout.Label(subText, subStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPauseMenu()
        {
            bool isEs = currentLanguage == "es";
            const float width = 460;
            float maxHeight = Mathf.Max(300f, Screen.height - 40f);
            float desiredHeight = (dynamicPauseMenuHeight > 0f) ? dynamicPauseMenuHeight : 420f;
            float height = Mathf.Min(desiredHeight, maxHeight);

            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(x + 24, y + 20, width - 48, height - 40));
            GUILayout.BeginVertical();

            bool needsScroll = desiredHeight > maxHeight;
            if (needsScroll)
            {
                pauseMenuScrollPos = GUILayout.BeginScrollView(pauseMenuScrollPos, false, false);
            }

            GUILayout.Label(isEs ? "[Pausa] Menú de Pausa" : "[Pause] Pause Menu", GetModalTitleStyle());
            GUILayout.Label(isEs ? "Avenida Benito Juárez • Paso del Norte" : "Avenida Benito Juárez • Paso del Norte Corridor", GetModalSubtitleStyle());
            GUILayout.Space(14);

            if (GUILayout.Button(isEs ? "> Reanudar Exploración [Esc]" : "> Resume Exploration [Esc]", GUILayout.Height(36)))
            {
                ResumeFromPauseMenu();
            }

            GUILayout.Space(6);

            if (GUILayout.Button(isEs ? "[?] Guía de Controles [F1]" : "[?] Controls Guide [F1]", GUILayout.Height(32)))
            {
                OpenControlsGuide(PrimaryUIState.PauseMenu);
            }

            GUILayout.Space(6);

            if (GUILayout.Button(isEs ? "[*] Ajustes de Audio, Navegación y Rendimiento" : "[*] Audio, Navigation & Performance Settings", GUILayout.Height(34)))
            {
                isSettingsDrawerOpen = true;
            }

            GUILayout.Space(6);

            string langToggleLabel = isEs ? "[ES] Idioma: Español [Cambiar a EN]" : "[EN] Language: English [Switch to ES]";
            if (GUILayout.Button(langToggleLabel, GUILayout.Height(32)))
            {
                CurrentLanguage = isEs ? "en" : "es";
            }

            GUILayout.Space(12);

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button(isEs ? "[X] Salir al Escritorio [Alt+F4]" : "[X] Quit to Desktop [Alt+F4]", GUILayout.Height(34)))
            {
                QuitGame();
            }
            GUI.color = prevColor;

            GUILayout.Space(10);
            GUILayout.Label(isEs ? "Presiona Esc para reanudar el juego" : "Press Escape to resume gameplay", GetMutedStyle());

            if (needsScroll)
            {
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float measured = GUILayoutUtility.GetLastRect().height;
                if (measured > 50f)
                {
                    float totalNeeded = measured + 48f;
                    if (Mathf.Abs(totalNeeded - dynamicPauseMenuHeight) > 2f)
                    {
                        dynamicPauseMenuHeight = totalNeeded;
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawSettingsDrawer()
        {
            bool isEs = currentLanguage == "es";
            const float width = 560;
            float maxHeight = Mathf.Max(300f, Screen.height - 40f);
            float desiredHeight = (dynamicSettingsHeight > 0f) ? dynamicSettingsHeight : 650f;
            float height = Mathf.Min(desiredHeight, maxHeight);

            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(x + 24, y + 18, width - 48, height - 36));
            GUILayout.BeginVertical();

            bool needsScroll = desiredHeight > maxHeight;
            if (needsScroll)
            {
                settingsDrawerScrollPos = GUILayout.BeginScrollView(settingsDrawerScrollPos, false, false);
            }

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? "[Ajustes] Sistema, Sonido y Rendimiento" : "[Settings] System, Audio & Performance", GetModalTitleStyle());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isEs ? "[X] Volver [Esc]" : "[X] Back [Esc]", GUILayout.Width(110), GUILayout.Height(24)))
            {
                isSettingsDrawerOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(isEs ? "Avenida Benito Juárez • Preferencias de Usuario" : "Avenida Benito Juárez • User Preferences", GetModalSubtitleStyle());
            GUILayout.Space(10);

            // Section 1: Audio System
            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };

            GUILayout.Label(isEs ? "Audio y Salida Maestra:" : "Audio & Master Output:", sectionStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(isEs ? $"Volumen Master: {(int)(masterVolume * 100f)}%" : $"Master Volume: {(int)(masterVolume * 100f)}%", GUILayout.Width(160));
            float newVolume = GUILayout.HorizontalSlider(masterVolume, 0.0f, 1.0f);
            if (Mathf.Abs(newVolume - masterVolume) > 0.005f)
            {
                MasterVolume = newVolume;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            string muteText = isAudioMuted
                ? (isEs ? "[Silenciado] Audio Desactivado [Reactivar]" : "[Muted] Audio Off [Unmute]")
                : (isEs ? "[Activo] Audio Habilitado [Silenciar]" : "[Active] Audio On [Mute]");

            Color prevBtnColor = GUI.color;
            if (isAudioMuted) GUI.color = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button(muteText, GUILayout.Height(30)))
            {
                IsAudioMuted = !isAudioMuted;
            }
            GUI.color = prevBtnColor;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Truthful technical disclosure
            string audioDisclosure = isEs
                ? "<b>Aviso de Sistema:</b> La escena modula la ganancia global mediante AudioListener de Unity. La versión actual no distribuye archivos de audio estáticos en disco; este regulador controla la escucha del ejecutable."
                : "<b>System Disclosure:</b> The scene modulates global gain via Unity's AudioListener. The current release bundles no static audio files on disk; this control manages executable listener gain.";
            GUILayout.Label(audioDisclosure, GetMutedStyle());

            GUILayout.Space(12);

            // Section 2: Navigation & HUD Visual Aids
            GUILayout.Label(isEs ? "Ayudas Visuales de Navegación:" : "Navigation & HUD Visual Aids:", sectionStyle);

            bool newCompass = GUILayout.Toggle(showCompass, isEs ? " Mostrar Brújula de Orientación (Ribbon Superior)" : " Show Orientation Compass (Top Ribbon)");
            if (newCompass != showCompass)
            {
                ShowCompass = newCompass;
            }

            bool newLandmarks = GUILayout.Toggle(showLandmarkHUD, isEs ? " Mostrar Indicador de Puntos de Interés Cercanos (Radar)" : " Show Nearby Landmarks Radar HUD");
            if (newLandmarks != showLandmarkHUD)
            {
                ShowLandmarkHUD = newLandmarks;
            }

            bool newMinimap = GUILayout.Toggle(showMinimap, isEs ? " Mostrar Minimapa 2D del Corredor (Tecla M)" : " Show 2D Corridor Minimap (Key M)");
            if (newMinimap != showMinimap)
            {
                ShowMinimap = newMinimap;
            }

            GUILayout.Space(12);

            // Section 3: Performance & Thermal Safeguard (Protección contra apagones por sobrecalentamiento)
            GUILayout.Label(isEs ? "Rendimiento y Protección Térmica (Límite FPS):" : "Performance & Thermal Safeguard (FPS Cap):", sectionStyle);

            GUILayout.BeginHorizontal();
            Color prevBg = GUI.backgroundColor;
            if (targetFramerate == 60) GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button(isEs ? "60 FPS (Estándar Recomendado)" : "60 FPS (Standard Recommended)", GUILayout.Height(28)))
            {
                TargetFramerate = 60;
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(10);

            if (targetFramerate == 30) GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button(isEs ? "30 FPS (Modo Laptop Fría / Batería)" : "30 FPS (Cool Laptop / Eco Mode)", GUILayout.Height(28)))
            {
                TargetFramerate = 30;
            }
            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            string fpsExplanation = isEs
                ? $"<b>Protección Activa:</b> VSync [1: Sincronizado] • Límite [{targetFramerate} FPS] • Medición: [{currentFpsDisplay} FPS]. Limita el uso de GPU/CPU para evitar sobrecalentamiento y apagones térmicos."
                : $"<b>Active Protection:</b> VSync [1: Synced] • Cap [{targetFramerate} FPS] • Current: [{currentFpsDisplay} FPS]. Restricts GPU/CPU usage to prevent thermal saturation and emergency shutdowns.";
            GUILayout.Label(fpsExplanation, GetMutedStyle());

            // Section 4: Control de Plumas Vehiculares de Peaje (Arcos 1 - 4)
            GUILayout.Space(10);
            GUILayout.Label(isEs ? "Plumas Vehiculares de Peaje (Arco 1 - 4):" : "Bridge Toll Barrier Gates (Arch 1 - 4):", sectionStyle);

            TollBarrierController barriers = TollBarrierController.Instance ?? FindFirstObjectByType<TollBarrierController>();
            if (barriers != null)
            {
                GUILayout.BeginHorizontal();
                for (int i = 1; i <= 4; i++)
                {
                    bool isOpen = barriers.IsBarrierOpen(i);
                    Color oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = isOpen ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.95f, 0.4f, 0.4f);
                    string btnText = isEs
                        ? $"Arco {i}: {(isOpen ? "Abierta [✓]" : "Cerrada [✕]")}"
                        : $"Arch {i}: {(isOpen ? "Open [✓]" : "Closed [✕]")}";
                    if (GUILayout.Button(btnText, GUILayout.Height(28)))
                    {
                        barriers.ToggleBarrier(i);
                    }
                    GUI.backgroundColor = oldBg;
                    if (i < 4) GUILayout.Space(6);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(isEs ? "Controlador de plumas disponible en escena." : "Barrier controller active in scene.", GetMutedStyle());
            }

            GUILayout.Space(10);

            // Bottom Return Button
            if (GUILayout.Button(isEs ? "<- Volver al Menú de Pausa [Esc]" : "<- Back to Pause Menu [Esc]", GUILayout.Height(34)))
            {
                isSettingsDrawerOpen = false;
            }

            if (needsScroll)
            {
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float measured = GUILayoutUtility.GetLastRect().height;
                if (measured > 50f)
                {
                    float totalNeeded = measured + 44f;
                    if (Mathf.Abs(totalNeeded - dynamicSettingsHeight) > 2f)
                    {
                        dynamicSettingsHeight = totalNeeded;
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawCompassStrip()
        {
            if (currentState != PrimaryUIState.Exploration || !isCursorLocked) return;

            bool isEs = currentLanguage == "es";
            float heading = GetPlayerHeading(); // 0 to 359 degrees
            int headingInt = Mathf.RoundToInt(heading) % 360;

            string cardinal;
            string destHint;

            if (heading >= 337.5f || heading < 22.5f)
            {
                cardinal = "N";
                destHint = isEs ? "Hacia Puente Internacional Paso del Norte / Kiosco CBP" : "Towards Paso del Norte Bridge / CBP Kiosk";
            }
            else if (heading >= 22.5f && heading < 67.5f)
            {
                cardinal = "NE";
                destHint = isEs ? "Hacia Fila Peatonal y Kiosco CBP" : "Towards Pedestrian Queue & CBP Kiosk";
            }
            else if (heading >= 67.5f && heading < 112.5f)
            {
                cardinal = "E";
                destHint = isEs ? "Hacia Comercios Acera Este (Farmacia Benavides)" : "Towards East Sidewalk (Farmacias Benavides)";
            }
            else if (heading >= 112.5f && heading < 157.5f)
            {
                cardinal = "SE";
                destHint = isEs ? "Hacia Sector Comercial Este" : "Towards South-East Commercial Zone";
            }
            else if (heading >= 157.5f && heading < 202.5f)
            {
                cardinal = "S";
                destHint = isEs ? "Hacia Entrada Sur / Centro de Ciudad Juárez" : "Towards South Entrance / Downtown Ciudad Juárez";
            }
            else if (heading >= 202.5f && heading < 247.5f)
            {
                cardinal = "SW";
                destHint = isEs ? "Hacia Entrada Sur / Acera Poniente" : "Towards South Entrance / West Sidewalk";
            }
            else if (heading >= 247.5f && heading < 292.5f)
            {
                cardinal = "W";
                destHint = isEs ? "Hacia Acera Poniente (Óptica Fashion, Atención Ciudadana)" : "Towards West Sidewalk (Óptica Fashion, Citizen Service)";
            }
            else
            {
                cardinal = "NW";
                destHint = isEs ? "Hacia Módulo Atención Ciudadana / Puente" : "Towards Citizen Service Desk / Bridge";
            }

            const float ribbonW = 380f;
            const float ribbonH = 26f;
            float ribbonX = (Screen.width - ribbonW) * 0.5f;
            float ribbonY = 42f;

            GUI.Box(new Rect(ribbonX, ribbonY, ribbonW, ribbonH), GUIContent.none, GetNavCardStyle());

            GUIStyle compassTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            string stripText = $"<color=#ffcc33>[{cardinal} {headingInt:000}°]</color> <color=#99bbdd>{destHint}</color>";
            GUI.Label(new Rect(ribbonX + 4, ribbonY + 2, ribbonW - 8, ribbonH - 4), stripText, compassTextStyle);
        }

        private void DrawNearbyLandmarksHUD()
        {
            if (currentState != PrimaryUIState.Exploration || !isCursorLocked) return;

            bool isEs = currentLanguage == "es";
            InteractionPoint[] points = (cachedInteractionPoints != null && cachedInteractionPoints.Length > 0)
                ? cachedInteractionPoints
                : FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);

            if (points == null || points.Length == 0) return;

            Vector3 playerPos = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            // Find up to 3 closest points
            var sortedPoints = new List<Tuple<float, InteractionPoint, string, string>>();

            foreach (var pt in points)
            {
                if (pt == null) continue;
                float dist = Vector3.Distance(playerPos, pt.transform.position);

                Vector3 toTarget = pt.transform.position - playerPos;
                toTarget.y = 0;
                float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);

                string dirArrow;
                if (Mathf.Abs(angle) <= 35f)
                {
                    dirArrow = "[^] " + (isEs ? "Frente" : "Ahead");
                }
                else if (angle > 35f && angle <= 145f)
                {
                    dirArrow = "[>] " + (isEs ? "Der" : "Right");
                }
                else if (angle < -35f && angle >= -145f)
                {
                    dirArrow = "[<] " + (isEs ? "Izq" : "Left");
                }
                else
                {
                    dirArrow = "[v] " + (isEs ? "Atrás" : "Back");
                }

                string catBadge = "";
                switch (pt.category)
                {
                    case InteractionCategory.BorderConcierge:
                        catBadge = "[CBP]";
                        break;
                    case InteractionCategory.CitizenService:
                        catBadge = "[Muni]";
                        break;
                    case InteractionCategory.StorefrontAttendant:
                        catBadge = isEs ? "[Comercio]" : "[Store]";
                        break;
                    case InteractionCategory.SyntheticWitness:
                        catBadge = isEs ? "[Fila]" : "[Queue]";
                        break;
                }

                sortedPoints.Add(new Tuple<float, InteractionPoint, string, string>(dist, pt, dirArrow, catBadge));
            }

            sortedPoints.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            const float cardW = 280f;
            int count = Mathf.Min(3, sortedPoints.Count);
            float cardH = 28f + count * 22f;
            float cardX = 14f;
            float cardY = 44f;

            GUI.Box(new Rect(cardX, cardY, cardW, cardH), GUIContent.none, GetNavCardStyle());

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 0.85f, 1f) }
            };

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(cardX + 8, cardY + 4, cardW - 16, 18), isEs ? "[Radar] Puntos Cercanos:" : "[Radar] Nearby Points:", titleStyle);

            for (int i = 0; i < count; i++)
            {
                var entry = sortedPoints[i];
                float dist = entry.Item1;
                InteractionPoint pt = entry.Item2;
                string dir = entry.Item3;
                string badge = entry.Item4;
                string name = !string.IsNullOrEmpty(pt.characterName) ? pt.characterName : pt.title;

                bool inRange = dist <= Mathf.Max(pt.interactionRadius, maxInteractionDistance);
                string rangeHint = inRange ? " <color=#44ffbb>[E: Hablar]</color>" : $" <color=#bbccdd>({(int)dist}m)</color>";

                string actBadge = "";
                if (pt.activityState == "busy")
                {
                    actBadge = " <color=#ffd700>[Ocupado]</color>";
                }
                else if (pt.activityState == "off_duty")
                {
                    actBadge = " <color=#8899aa>[Fuera]</color>";
                }
                else if (pt.activityState == "available")
                {
                    actBadge = " <color=#55ff77>[Disp]</color>";
                }

                string line = $"<color=#ffcc33>{dir}</color> {badge} <b>{name}</b>{actBadge}{rangeHint}";
                GUI.Label(new Rect(cardX + 8, cardY + 24 + (i * 22), cardW - 16, 20), line, itemStyle);
            }
        }

        private void DrawCorridorMinimap()
        {
            if (currentState != PrimaryUIState.Exploration || !isCursorLocked) return;

            bool isEs = currentLanguage == "es";

            const float cardW = 160f;
            const float cardH = 230f;
            float cardX = Screen.width - cardW - 14f;
            float cardY = 44f;

            GUI.Box(new Rect(cardX, cardY, cardW, cardH), GUIContent.none, GetNavCardStyle());

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.85f, 1f) }
            };
            GUI.Label(new Rect(cardX, cardY + 4, cardW, 16), isEs ? "Minimapa Corredor [M]" : "Corridor Minimap [M]", titleStyle);

            // Inner map area
            float mapX = cardX + 10f;
            float mapY = cardY + 22f;
            float mapW = cardW - 20f; // 140
            float mapH = 175f;

            // Geometry bounds: X in [-16, +22] (width 38m), Z in [-25, +65] (length 90m)
            const float minX = -16f;
            const float maxX = 22f;
            const float minZ = -25f;
            const float maxZ = 65f;

            Color prevColor = GUI.color;

            // 1. Sidewalk & plaza background (entire corridor width)
            GUI.color = new Color(0.18f, 0.20f, 0.24f, 0.95f);
            GUI.DrawTexture(new Rect(mapX, mapY, mapW, mapH), Texture2D.whiteTexture);

            // 2. Central roadway: X from -7 to +12 (north approach) / -7 to +7 (avenue)
            float roadX = mapX + mapW * ((-7f - minX) / (maxX - minX));
            float roadW = mapW * (14f / (maxX - minX));
            GUI.color = new Color(0.11f, 0.13f, 0.16f, 0.95f);
            GUI.DrawTexture(new Rect(roadX, mapY, roadW, mapH), Texture2D.whiteTexture);

            // 3. Bridge canopy zone at North end: Z from 50 to 65 -> top portion of map
            float bridgeH = mapH * (15f / 90f);
            GUI.color = new Color(0.15f, 0.25f, 0.35f, 0.70f);
            GUI.DrawTexture(new Rect(mapX, mapY, mapW, bridgeH), Texture2D.whiteTexture);

            // North and South labels
            GUIStyle mapNoteStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.8f, 0.9f, 1f, 0.8f) }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(mapX + 2, mapY, mapW - 4, 14), isEs ? "[N] Puente CBP" : "[N] CBP Bridge", mapNoteStyle);
            GUI.Label(new Rect(mapX + 2, mapY + mapH - 14, mapW - 4, 14), isEs ? "[S] Entrada Sur" : "[S] South Spawn", mapNoteStyle);

            // 4. Draw Landmark blips
            InteractionPoint[] points = (cachedInteractionPoints != null && cachedInteractionPoints.Length > 0)
                ? cachedInteractionPoints
                : FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);

            if (points != null)
            {
                foreach (var pt in points)
                {
                    if (pt == null) continue;
                    Vector3 ptPos = pt.transform.position;
                    float nx = Mathf.Clamp01((ptPos.x - minX) / (maxX - minX));
                    float nz = Mathf.Clamp01((ptPos.z - minZ) / (maxZ - minZ));

                    float px = mapX + nx * mapW;
                    float py = mapY + (1.0f - nz) * mapH;

                    Color blipColor;
                    switch (pt.category)
                    {
                        case InteractionCategory.BorderConcierge:
                            blipColor = new Color(0.2f, 0.85f, 1f, 1f); // Cyan
                            break;
                        case InteractionCategory.CitizenService:
                            blipColor = new Color(1f, 0.8f, 0.2f, 1f); // Gold
                            break;
                        case InteractionCategory.StorefrontAttendant:
                            blipColor = new Color(0.4f, 0.95f, 0.4f, 1f); // Green
                            break;
                        default:
                            blipColor = new Color(0.7f, 0.7f, 1f, 1f); // Lavender
                            break;
                    }

                    GUI.color = blipColor;
                    GUI.DrawTexture(new Rect(px - 2.5f, py - 2.5f, 5f, 5f), Texture2D.whiteTexture);
                }
            }

            // 5. Draw Player position & heading cone
            Vector3 playerPos = transform.position;
            float pnx = Mathf.Clamp01((playerPos.x - minX) / (maxX - minX));
            float pnz = Mathf.Clamp01((playerPos.z - minZ) / (maxZ - minZ));
            float ppx = mapX + pnx * mapW;
            float ppy = mapY + (1.0f - pnz) * mapH;

            // Heading pointer
            Vector3 fwd = transform.forward;
            float dirX = fwd.x;
            float dirZ = fwd.z;
            float ptrX = ppx + dirX * 6f;
            float ptrY = ppy - dirZ * 6f;

            // Player dot
            GUI.color = new Color(1f, 0.95f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(ppx - 3f, ppy - 3f, 6f, 6f), Texture2D.whiteTexture);

            // Heading dot
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(ptrX - 1.5f, ptrY - 1.5f, 3f, 3f), Texture2D.whiteTexture);

            // Reset GUI color
            GUI.color = prevColor;

            // Map legend footer
            GUIStyle legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(cardX, mapY + mapH + 4, cardW, 16), isEs ? "● Tú  ● CBP  ● Muni  ● Tienda" : "● You ● CBP ● Muni ● Store", legendStyle);
        }

        private void DrawControlsOverlay()
        {
            bool isEs = currentLanguage == "es";
            const float width = 540;
            float maxHeight = Mathf.Max(300f, Screen.height - 40f);
            float desiredHeight = (dynamicControlsHeight > 0f) ? dynamicControlsHeight : 520f;
            float height = Mathf.Min(desiredHeight, maxHeight);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect cardRect = new Rect(x, y, width, height);
            GUI.Box(cardRect, GUIContent.none, GetOpaqueCardStyle());

            GUILayout.BeginArea(new Rect(x + 18, y + 14, width - 36, height - 28));
            GUILayout.BeginVertical();

            bool needsScroll = desiredHeight > maxHeight;
            if (needsScroll)
            {
                controlsGuideScrollPos = GUILayout.BeginScrollView(controlsGuideScrollPos, false, false);
            }

            GUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.9f, 1f) }
            };
            GUILayout.Label(isEs ? "[Guía] Controles y Exploración" : "[Guide] Controls & Exploration Guide", titleStyle);

            GUILayout.FlexibleSpace();

            string topCloseText = (controlsGuideOrigin == PrimaryUIState.PauseMenu)
                ? (isEs ? "[X] Volver [Esc]" : "[X] Back [Esc]")
                : (isEs ? "[X] Cerrar [Esc/F1]" : "[X] Close [Esc/F1]");

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
            GUILayout.Label(isEs ? "• M: Mostrar / ocultar minimapa 2D del corredor" : "• M: Toggle 2D corridor minimap", GetMutedStyle());
            GUILayout.Label(isEs ? "• Clic en la ventana: Capturar ratón para explorar" : "• Click in window: Capture mouse for first-person exploration", GetMutedStyle());

            GUILayout.Space(6);
            GUILayout.Label(isEs ? "Interacción con Personajes:" : "NPC & Landmark Interaction:", sectionStyle);
            GUILayout.Label(isEs ? "• E: Hablar con personajes al ver el aviso [E]" : "• E: Talk to characters when [E] prompt appears", GetMutedStyle());
            GUILayout.Label(isEs ? "• Esc: Menú de Pausa y Ajustes (o cerrar diálogo activo)" : "• Esc: Open Pause Menu & Settings (or close active dialogue)", GetMutedStyle());
            GUILayout.Label(isEs ? "• F1: Mostrar / ocultar esta guía en cualquier momento" : "• F1: Toggle this controls guide anytime", GetMutedStyle());

            GUILayout.Space(6);
            GUILayout.Label(isEs ? "Puntos de Interés en Avenida Juárez:" : "Inhabitants & Points along Avenida Juárez:", sectionStyle);
            GUILayout.Label(isEs ? "• [Farmacia] Carlos Méndez (Farmacia Benavides, Acera Este)" : "• [Pharmacy] Carlos Méndez (Farmacias Benavides, East sidewalk)", GetMutedStyle());
            GUILayout.Label(isEs ? "• [Óptica] Valeria Domínguez (Óptica Fashion, Acera Poniente)" : "• [Optical] Valeria Domínguez (Óptica Fashion, West sidewalk)", GetMutedStyle());
            GUILayout.Label(isEs ? "• [Atención Ciudadana] Lic. Patricia Morales (Acera Poniente)" : "• [Citizen Service] Lic. Patricia Morales (West sidewalk)", GetMutedStyle());
            GUILayout.Label(isEs ? "• [Testigos] Mateo, Sofía, Héctor (Fila peatonal al puente)" : "• [Witnesses] Mateo, Sofia, Hector (Pedestrian queue to bridge)", GetMutedStyle());
            GUILayout.Label(isEs ? "• [CBP] Elena Ramos (Kiosco oficial de información CBP)" : "• [CBP] Elena Ramos (Official CBP Border Concierge Kiosk)", GetMutedStyle());

            GUILayout.Space(10);

            // Clear bottom navigation button
            if (controlsGuideOrigin == PrimaryUIState.PauseMenu)
            {
                if (GUILayout.Button(isEs ? "<- Volver al Menú de Pausa [Esc]" : "<- Back to Pause Menu [Esc]", GUILayout.Height(34)))
                {
                    CloseControlsGuide();
                }
            }
            else
            {
                if (GUILayout.Button(isEs ? "> Reanudar Exploración [Esc/F1]" : "> Resume Exploration [Esc/F1]", GUILayout.Height(34)))
                {
                    CloseControlsGuide();
                }
            }

            if (needsScroll)
            {
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float measured = GUILayoutUtility.GetLastRect().height;
                if (measured > 50f)
                {
                    float totalNeeded = measured + 36f;
                    if (Mathf.Abs(totalNeeded - dynamicControlsHeight) > 2f)
                    {
                        dynamicControlsHeight = totalNeeded;
                    }
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
            string safeAv = FormatSafeAvatar(point.avatar, point.characterName);
            string avatarPrefix = !string.IsNullOrEmpty(safeAv) ? $"{safeAv} " : "";
            string displayName = !string.IsNullOrEmpty(point.characterName) ? point.characterName : point.title;
            GUILayout.Label($"{avatarPrefix}{displayName}", GetModalTitleStyle());

            GUILayout.FlexibleSpace();

            // Language Selector in Dialogue Header (Task B - compact ES | EN selector)
            bool isEs = (currentLanguage == "es");
            Color origColor = GUI.color;

            GUI.color = isEs ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.65f, 0.72f, 0.82f);
            if (GUILayout.Button("ES", GUILayout.Width(32), GUILayout.Height(24)))
            {
                CurrentLanguage = "es";
            }

            GUI.color = !isEs ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.65f, 0.72f, 0.82f);
            if (GUILayout.Button("EN", GUILayout.Width(32), GUILayout.Height(24)))
            {
                CurrentLanguage = "en";
            }
            GUI.color = origColor;

            GUILayout.Space(16); // Guaranteed >= 16px clearance before Close

            // Close button
            if (GUILayout.Button(isEs ? "[X] Cerrar [Esc]" : "[X] Close [Esc]", GUILayout.Width(110), GUILayout.Height(24)))
            {
                CloseInteractionModal();
            }
            GUILayout.EndHorizontal();

            // Subtitle / Role & Address
            GUILayout.Label(point.subtitle, GetModalSubtitleStyle());
            if (!string.IsNullOrEmpty(point.address))
            {
                GUILayout.Label($"[*] {point.address}", GetMutedStyle());
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

            // 2.1 Authoritative World Core Activity & Interaction Demeanor Banner (Increment 5.17.0)
            if (!string.IsNullOrEmpty(point.interactionAvailability) || !string.IsNullOrEmpty(point.activityState))
            {
                string actTitle = point.GetActivityTitle(currentLanguage);
                string decisionReason = point.GetDecisionReason(currentLanguage);
                string actStatusText;
                Color statusColor;

                switch (point.interactionAvailability)
                {
                    case "available_in_transit":
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [EN TRÁNSITO] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [IN TRANSIT] {actTitle}";
                        statusColor = new Color(0.4f, 0.95f, 1f);
                        break;
                    case "available_contextual":
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [DESCANSO / ACTIVIDAD] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [BREAK / DWELL] {actTitle}";
                        statusColor = new Color(1f, 0.85f, 0.35f);
                        break;
                    case "busy":
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [OCUPADO EN PRIORIDAD] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [BUSY WITH PRIORITY] {actTitle}";
                        statusColor = new Color(1f, 0.85f, 0.25f);
                        break;
                    case "off_duty":
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [FUERA DE TURNO] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [OFF DUTY] {actTitle}";
                        statusColor = new Color(0.75f, 0.8f, 0.85f);
                        break;
                    case "unavailable":
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [CERRADO HOY] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [CLOSED TODAY] {actTitle}";
                        statusColor = new Color(0.65f, 0.65f, 0.65f);
                        break;
                    case "available_normal":
                    default:
                        actStatusText = isEs
                            ? $"[*] DEMEANOR AUTORITATIVO: [EN SERVICIO] {actTitle}"
                            : $"[*] AUTHORITATIVE DEMEANOR: [ON DUTY] {actTitle}";
                        statusColor = new Color(0.35f, 0.95f, 0.45f);
                        break;
                }

                if (!string.IsNullOrEmpty(decisionReason))
                {
                    actStatusText += $" — {decisionReason}";
                }

                GUIStyle actStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = statusColor }
                };
                GUILayout.Box(actStatusText, actStyle, GUILayout.MinHeight(22));
            }

            // 2.5 Citizen Service Mode Selector (Increment 5.11)
            if (point.category == InteractionCategory.CitizenService)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                bool isConsultation = (citizenReportStep == CitizenReportWorkflowStep.Consultation);
                GUI.color = isConsultation ? new Color(0.4f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "[Q&A] Orientación y Consultas" : "[Q&A] Inquiries & Guidance", GUILayout.Height(26)))
                {
                    citizenReportStep = CitizenReportWorkflowStep.Consultation;
                }
                GUI.color = !isConsultation ? new Color(0.4f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "[Reporte] Registrar Reporte Ciudadano (foto opcional)" : "[Report] Register Citizen Report (photo optional)", GUILayout.Height(26)))
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
                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                bool isChat = (borderConciergeViewMode == BorderConciergeViewMode.Conversation);
                GUI.color = isChat ? new Color(0.25f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "[Chat] Diálogo y Orientación" : "[Chat] Dialogue & Guidance", GUILayout.Height(26)))
                {
                    borderConciergeViewMode = BorderConciergeViewMode.Conversation;
                }
                GUI.color = !isChat ? new Color(0.25f, 0.95f, 0.85f) : Color.white;
                if (GUILayout.Button(isEs ? "[CBP] Tiempos Oficiales y Decisión de Cruce" : "[CBP] Official Wait Times & Crossing Intent", GUILayout.Height(26)))
                {
                    borderConciergeViewMode = BorderConciergeViewMode.OfficialWaitTimes;
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                if (borderConciergeViewMode == BorderConciergeViewMode.OfficialWaitTimes)
                {
                    DrawOfficialBorderIntelligenceSection();
                    GUILayout.EndArea();
                    return;
                }
                else
                {
                    DrawCompactBorderSummaryBar(isEs);
                    GUILayout.Space(4);
                }
            }

            // 4. Conversation History Area (Scrollable with Styled Frame)
            float chatBoxHeight = (point.category == InteractionCategory.BorderConcierge) ? 220f : 240f;
            GUILayout.BeginVertical(GetChatFrameStyle());
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
            GUILayout.EndVertical();

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
                    ? "[...] Consultando servicio de IA / World Core..."
                    : "[...] Processing inquiry with Meta Muse Spark / World Core...";
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
                GUILayout.Box($"[!] {requestErrorMessage}", errorStyle, GUILayout.Height(24));
                if (GUILayout.Button((currentLanguage == "es") ? "Reintentar" : "Retry", GUILayout.Width(90), GUILayout.Height(24)))
                {
                    RetryLastMessage();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // 7. Input Bar (Text Field + Send Button)
            string statelessNotice = (currentLanguage == "es")
                ? "[i] Cada consulta se procesa de forma independiente."
                : "[i] Each inquiry is processed independently.";
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

        private void DrawCompactBorderSummaryBar(bool isEs)
        {
            GUILayout.BeginHorizontal(GetTableCardStyle(), GUILayout.Height(28));

            if (isBorderWaitTimesLoading)
            {
                GUILayout.Label(isEs ? "[CBP] Actualizando observaciones oficiales en vivo..." : "[CBP] Updating live official observations...", GetMutedStyle());
            }
            else if (liveBorderWaitTimes?.comparison?.observations != null)
            {
                var obs = liveBorderWaitTimes.comparison.observations;
                var pdn = obs.pasoDelNorte;
                var bota = obs.bridgeOfTheAmericas;

                string pdnDelay = pdn != null ? pdn.FormatDelay(currentLanguage) : "N/A";
                string pdnStatus = pdn != null ? pdn.portStatus : "N/A";
                string botaDelay = bota != null ? bota.FormatDelay(currentLanguage) : "N/A";
                string botaStatus = bota != null ? bota.portStatus : "N/A";

                string pdnFresh = (pdn != null && pdn.freshness == "fresh")
                    ? (isEs ? "[Vigente]" : "[Fresh]")
                    : (isEs ? "[Desactualizado]" : "[Stale]");

                string summaryText = isEs
                    ? $"[CBP] Paso del Norte: <color=#55FF66>{pdnDelay}</color> ({pdnStatus}) • BOTA: <color=#FFDD55>{botaDelay}</color> ({botaStatus}) • {pdnFresh}"
                    : $"[CBP] Paso del Norte: <color=#55FF66>{pdnDelay}</color> ({pdnStatus}) • BOTA: <color=#FFDD55>{botaDelay}</color> ({botaStatus}) • {pdnFresh}";

                GUIStyle sumStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    richText = true,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f) }
                };
                GUILayout.Label(summaryText, sumStyle);
            }
            else
            {
                GUILayout.Label(isEs ? "[CBP] Tiempos oficiales de espera peatonal disponibles." : "[CBP] Official pedestrian wait times available.", GetMutedStyle());
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(isEs ? "[Ver Tabla Completa ->]" : "[View Full Table ->]", GUILayout.Width(165), GUILayout.Height(20)))
            {
                borderConciergeViewMode = BorderConciergeViewMode.OfficialWaitTimes;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawOfficialBorderIntelligenceSection()
        {
            bool isEs = currentLanguage == "es";

            GUIStyle boxStyle = GetTableCardStyle();

            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.85f, 1f) }
            };
            GUILayout.Label(isEs ? "[CBP] Tiempos Oficiales de Espera Peatonal (CBP / Fila General)" : "[CBP] Official CBP Pedestrian Wait Times (General Lane)", headerStyle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(isEs ? "[->] Actualizar" : "[->] Refresh", GUILayout.Width(105), GUILayout.Height(22)))
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
                GUILayout.Label($"[!] {borderWaitTimesError}", GetMutedStyle());
            }
            else if (liveBorderWaitTimes?.comparison?.observations != null)
            {
                var obs = liveBorderWaitTimes.comparison.observations;
                var pdn = obs.pasoDelNorte;
                var bota = obs.bridgeOfTheAmericas;

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();

                // PDN Column
                if (pdn != null)
                {
                    string delay = pdn.FormatDelay(currentLanguage);
                    string fresh = (pdn.freshness == "fresh")
                        ? (isEs ? "[Vigente] (<60m)" : "[Fresh] (<60m)")
                        : (isEs ? "[Desactualizado]" : "[Stale]");

                    string obsTime = !string.IsNullOrEmpty(pdn.sourceObservationTime) ? pdn.sourceObservationTime : "N/A";
                    string laneCat = isEs ? "Carril Peatonal Estándar / General" : "Standard / General Pedestrian Lane";

                    GUILayout.BeginVertical(GetTableCardStyle(), GUILayout.Width(330));
                    GUILayout.Label("<b>Paso del Norte (Santa Fe)</b>", GetSubheadingStyle());
                    GUILayout.Label(isEs ? $"Demora: <color=#55FF66>{delay}</color> ({pdn.portStatus}) • {fresh}" : $"Wait: <color=#55FF66>{delay}</color> ({pdn.portStatus}) • {fresh}", GetObservationStyle());
                    GUILayout.Label(isEs ? $"Obs. CBP: {obsTime} • Carriles: {pdn.lanesOpen}/{pdn.maximumLanes}" : $"CBP Obs: {obsTime} • Lanes: {pdn.lanesOpen}/{pdn.maximumLanes}", GetMutedStyle());
                    GUILayout.Label($"[*] {laneCat}", GetMutedStyle());
                    GUILayout.EndVertical();
                }

                GUILayout.Space(8);

                // BOTA Column
                if (bota != null)
                {
                    string delay = bota.FormatDelay(currentLanguage);
                    string fresh = (bota.freshness == "fresh")
                        ? (isEs ? "[Vigente] (<60m)" : "[Fresh] (<60m)")
                        : (isEs ? "[Desactualizado]" : "[Stale]");

                    string obsTime = !string.IsNullOrEmpty(bota.sourceObservationTime) ? bota.sourceObservationTime : "N/A";
                    string laneCat = isEs ? "Carril Peatonal Estándar / General" : "Standard / General Pedestrian Lane";

                    GUILayout.BeginVertical(GetTableCardStyle(), GUILayout.Width(330));
                    GUILayout.Label(isEs ? "<b>Puente de las Américas (BOTA)</b>" : "<b>Bridge of the Americas (BOTA)</b>", GetSubheadingStyle());
                    GUILayout.Label(isEs ? $"Demora: <color=#FFDD55>{delay}</color> ({bota.portStatus}) • {fresh}" : $"Wait: <color=#FFDD55>{delay}</color> ({bota.portStatus}) • {fresh}", GetObservationStyle());
                    GUILayout.Label(isEs ? $"Obs. CBP: {obsTime} • Carriles: {bota.lanesOpen}/{bota.maximumLanes}" : $"CBP Obs: {obsTime} • Lanes: {bota.lanesOpen}/{bota.maximumLanes}", GetMutedStyle());
                    GUILayout.Label($"[*] {laneCat}", GetMutedStyle());
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();

                // Authoritative Comparison Statement if eligible
                if (liveBorderWaitTimes.comparison.comparison != null && liveBorderWaitTimes.comparison.comparison.canCompare)
                {
                    string compStatement = liveBorderWaitTimes.comparison.comparison.statement?.Get(currentLanguage);
                    if (!string.IsNullOrEmpty(compStatement))
                    {
                        GUILayout.Space(4);
                        GUILayout.Label($"[Comparativa] {compStatement}", GetComparisonStatementStyle());
                    }
                }

                // Official Reference Video Cameras Link & Disclaimer
                GUILayout.Space(2);
                string camNotice = isEs
                    ? "[Cámaras] Referencia visual en vivo: puentesfronterizos.gob.mx (Referencia visual únicamente; no calcula estimaciones de espera)"
                    : "[Cameras] Official live video reference: puentesfronterizos.gob.mx (Visual reference only; does not calculate wait estimates)";
                GUILayout.Label(camNotice, GetMutedStyle());

                // Player Crossing Intent Selection (Session-Local Decision)
                DrawPlayerCrossingIntentSection(isEs);
            }
            else
            {
                GUILayout.Label(isEs ? "Presione 'Actualizar' para consultar CBP." : "Click 'Refresh' to fetch official CBP wait times.", GetMutedStyle());
            }

            GUILayout.EndVertical();

            // Return to Chat button
            GUILayout.Space(8);
            if (GUILayout.Button(isEs ? "[<-] Volver al Diálogo con Elena" : "[<-] Return to Dialogue with Elena", GUILayout.Height(30)))
            {
                borderConciergeViewMode = BorderConciergeViewMode.Conversation;
            }
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
            GUILayout.Label(isEs ? "[Decisión] Intención de Cruce del Jugador (Decisión Local de Sesión):" : "[Intent] Player Crossing Intent (Local Session Decision):", intentHeaderStyle);
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(selectedCrossingIntent))
            {
                if (GUILayout.Button(isEs ? "[X] Cambiar" : "[X] Change", GUILayout.Width(85), GUILayout.Height(18)))
                {
                    selectedCrossingIntent = null;
                }
            }
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(selectedCrossingIntent))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isEs ? "[->] Optar por Paso del Norte (Centro)" : "[->] Choose Paso del Norte (Downtown)", GUILayout.Height(24)))
                {
                    selectedCrossingIntent = "paso-del-norte";
                }
                GUILayout.Space(6);
                if (GUILayout.Button(isEs ? "[->] Optar por Puente de las Américas (BOTA)" : "[->] Choose Bridge of the Americas (BOTA)", GUILayout.Height(24)))
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
                GUILayout.Label(isEs ? $"[OK] Decisión seleccionada: {chosenTitle}" : $"[OK] Selected Intent: {chosenTitle}", chosenStyle);

                string disclaimer = isEs
                    ? "[i] Decisión local de sesión dentro de la experiencia virtual. No inicia un viaje real, no crea reservaciones ni altera el estado persistente del mundo."
                    : "[i] Session-local intent within this virtual experience. Does not initiate a real-world trip, create reservations, or modify persistent World Core state.";
                GUILayout.Label(disclaimer, GetMutedStyle());
            }

            GUILayout.EndVertical();
        }

        private void DrawMessageBubble(DialogueMessage msg)
        {
            GUILayout.BeginVertical(GetMessageContainerStyle(msg.isUser));

            // Header line (Sender + Badge + Time)
            GUILayout.BeginHorizontal();
            string safeAv = FormatSafeAvatar(msg.avatar, msg.senderName);
            string senderText = msg.isUser
                ? $"<b>{safeAv} {msg.senderName}</b>"
                : $"<b>{safeAv} {msg.senderName}</b> <color=#88CCEE>({msg.provenanceBadge})</color>";

            GUILayout.Label(senderText, GetMessageHeaderStyle(msg.isUser));
            GUILayout.FlexibleSpace();
            GUILayout.Label(msg.timestamp, GetMutedStyle());
            GUILayout.EndHorizontal();

            // Body text
            GUILayout.Label(msg.text, GetMessageBodyStyle(msg.isUser));

            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawSuggestedPromptChips(InteractionPoint point)
        {
            string[] prompts = (currentLanguage == "es") ? point.suggestedPromptsEs : point.suggestedPromptsEn;
            if (prompts == null || prompts.Length == 0) return;

            bool isEs = currentLanguage == "es";
            GUILayout.BeginVertical();
            GUILayout.Label(isEs ? "[*] Preguntas sugeridas (haz clic para consultar):" : "[*] Quick prompts (click to ask):", GetMutedStyle());

            GUIStyle chipStyle = GetPromptChipStyle();
            float availableWidth = (point.category == InteractionCategory.CitizenService) ? 816f : 676f;
            float halfWidth = (availableWidth - 8f) * 0.5f;

            // Multi-row adaptive flow layout with word wrapping
            for (int i = 0; i < prompts.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                string p1 = prompts[i];
                bool hasP2 = (i + 1 < prompts.Length);
                string p2 = hasP2 ? prompts[i + 1] : null;

                // Calculate heights dynamically for chips in this row
                float h1 = chipStyle.CalcHeight(new GUIContent(p1), hasP2 ? halfWidth : availableWidth);
                float h2 = hasP2 ? chipStyle.CalcHeight(new GUIContent(p2), halfWidth) : 0f;
                float rowHeight = Mathf.Max(26f, Mathf.Max(h1, h2));

                if (hasP2)
                {
                    if (GUILayout.Button(p1, chipStyle, GUILayout.Width(halfWidth), GUILayout.Height(rowHeight)))
                    {
                        currentInputText = p1;
                        if (!isRequestPending)
                        {
                            SendUserMessage();
                        }
                    }
                    GUILayout.Space(8);
                    if (GUILayout.Button(p2, chipStyle, GUILayout.Width(halfWidth), GUILayout.Height(rowHeight)))
                    {
                        currentInputText = p2;
                        if (!isRequestPending)
                        {
                            SendUserMessage();
                        }
                    }
                }
                else
                {
                    float singleW = Mathf.Min(halfWidth, chipStyle.CalcSize(new GUIContent(p1)).x + 24f);
                    if (singleW < 240f) singleW = 240f;
                    if (GUILayout.Button(p1, chipStyle, GUILayout.Width(singleW), GUILayout.Height(rowHeight)))
                    {
                        currentInputText = p1;
                        if (!isRequestPending)
                        {
                            SendUserMessage();
                        }
                    }
                    GUILayout.FlexibleSpace();
                }

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

        private static Texture2D navCardTex;
        private GUIStyle navCardStyle;

        private GUIStyle GetNavCardStyle()
        {
            if (navCardStyle == null)
            {
                navCardStyle = new GUIStyle(GUI.skin.box);
                if (navCardTex == null)
                {
                    navCardTex = CreateBorderedTexture(16, 16,
                        new Color(0.08f, 0.10f, 0.14f, 0.88f),
                        new Color(0.25f, 0.35f, 0.48f, 0.90f)
                    );
                }
                navCardStyle.normal.background = navCardTex;
                navCardStyle.border = new RectOffset(2, 2, 2, 2);
            }
            return navCardStyle;
        }

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

        private Texture2D userBubbleTex;
        private Texture2D npcBubbleTex;
        private Texture2D chatFrameTex;
        private Texture2D tableCardTex;
        private GUIStyle userMessageContainerStyle;
        private GUIStyle npcMessageContainerStyle;
        private GUIStyle userMessageHeaderStyle;
        private GUIStyle npcMessageHeaderStyle;
        private GUIStyle userMessageBodyStyle;
        private GUIStyle npcMessageBodyStyle;
        private GUIStyle chatFrameStyle;
        private GUIStyle promptChipStyle;
        private GUIStyle tableCardStyle;

        private GUIStyle GetChatFrameStyle()
        {
            if (chatFrameStyle == null)
            {
                chatFrameStyle = new GUIStyle(GUI.skin.box);
                if (chatFrameTex == null)
                {
                    chatFrameTex = CreateBorderedTexture(16, 16,
                        new Color(11f / 255f, 16f / 255f, 24f / 255f, 0.90f),
                        new Color(35f / 255f, 50f / 255f, 70f / 255f, 0.85f)
                    );
                }
                chatFrameStyle.normal.background = chatFrameTex;
                chatFrameStyle.border = new RectOffset(2, 2, 2, 2);
                chatFrameStyle.padding = new RectOffset(6, 6, 6, 6);
            }
            return chatFrameStyle;
        }

        private GUIStyle GetTableCardStyle()
        {
            if (tableCardStyle == null)
            {
                tableCardStyle = new GUIStyle(GUI.skin.box);
                if (tableCardTex == null)
                {
                    tableCardTex = CreateBorderedTexture(16, 16,
                        new Color(18f / 255f, 25f / 255f, 35f / 255f, 0.92f),
                        new Color(45f / 255f, 65f / 255f, 90f / 255f, 0.90f)
                    );
                }
                tableCardStyle.normal.background = tableCardTex;
                tableCardStyle.border = new RectOffset(2, 2, 2, 2);
                tableCardStyle.padding = new RectOffset(8, 8, 6, 6);
            }
            return tableCardStyle;
        }

        private GUIStyle GetPromptChipStyle()
        {
            if (promptChipStyle == null)
            {
                promptChipStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 4, 4)
                };
                promptChipStyle.normal.textColor = new Color(0.85f, 0.93f, 1f);
                promptChipStyle.hover.textColor = Color.white;
            }
            return promptChipStyle;
        }

        private GUIStyle GetMessageContainerStyle(bool isUser)
        {
            if (isUser)
            {
                if (userMessageContainerStyle == null)
                {
                    userMessageContainerStyle = new GUIStyle(GUI.skin.box);
                    if (userBubbleTex == null)
                    {
                        userBubbleTex = CreateBorderedTexture(16, 16,
                            new Color(18f / 255f, 32f / 255f, 48f / 255f, 0.90f),
                            new Color(40f / 255f, 75f / 255f, 105f / 255f, 0.90f)
                        );
                    }
                    userMessageContainerStyle.normal.background = userBubbleTex;
                    userMessageContainerStyle.border = new RectOffset(2, 2, 2, 2);
                    userMessageContainerStyle.padding = new RectOffset(10, 10, 6, 6);
                }
                return userMessageContainerStyle;
            }
            else
            {
                if (npcMessageContainerStyle == null)
                {
                    npcMessageContainerStyle = new GUIStyle(GUI.skin.box);
                    if (npcBubbleTex == null)
                    {
                        npcBubbleTex = CreateBorderedTexture(16, 16,
                            new Color(22f / 255f, 28f / 255f, 38f / 255f, 0.90f),
                            new Color(50f / 255f, 65f / 255f, 85f / 255f, 0.90f)
                        );
                    }
                    npcMessageContainerStyle.normal.background = npcBubbleTex;
                    npcMessageContainerStyle.border = new RectOffset(2, 2, 2, 2);
                    npcMessageContainerStyle.padding = new RectOffset(10, 10, 6, 6);
                }
                return npcMessageContainerStyle;
            }
        }

        private GUIStyle GetMessageHeaderStyle(bool isUser)
        {
            if (isUser)
            {
                if (userMessageHeaderStyle == null)
                {
                    userMessageHeaderStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        richText = true,
                        normal = { textColor = new Color(0.4f, 0.85f, 1f) }
                    };
                }
                return userMessageHeaderStyle;
            }
            else
            {
                if (npcMessageHeaderStyle == null)
                {
                    npcMessageHeaderStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        richText = true,
                        normal = { textColor = new Color(1f, 0.85f, 0.3f) }
                    };
                }
                return npcMessageHeaderStyle;
            }
        }

        private GUIStyle GetMessageBodyStyle(bool isUser)
        {
            if (isUser)
            {
                if (userMessageBodyStyle == null)
                {
                    userMessageBodyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        wordWrap = true,
                        normal = { textColor = new Color(0.92f, 0.96f, 1f) }
                    };
                }
                return userMessageBodyStyle;
            }
            else
            {
                if (npcMessageBodyStyle == null)
                {
                    npcMessageBodyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        wordWrap = true,
                        normal = { textColor = new Color(0.96f, 0.96f, 0.98f) }
                    };
                }
                return npcMessageBodyStyle;
            }
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
                GUILayout.Box($"[!] {citizenReportErrorMessage}", errStyle, GUILayout.Height(24));
                if (GUILayout.Button("[X]", GUILayout.Width(24), GUILayout.Height(24)))
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
            GUILayout.Label(isEs ? "[Atención] Atención Ciudadana Municipal · Registro Simulado de Incidencias" : "[Service] Municipal Citizen Service · Simulated Issue Intake", GetModalSubtitleStyle());
            GUILayout.Space(2);

            // Step Indicator Header: Guided 2-Phase SubStep Bar for Form Intake
            GUILayout.BeginHorizontal();
            bool isSubStep1 = (citizenReportFormSubStep == CitizenReportFormSubStep.Details);
            GUI.color = isSubStep1 ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.7f, 0.75f, 0.85f);
            if (GUILayout.Button(isEs ? "Paso 1: Detalles de la Incidencia" : "Step 1: Issue Details", GUILayout.Height(26)))
            {
                citizenReportFormSubStep = CitizenReportFormSubStep.Details;
            }
            GUI.color = !isSubStep1 ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.7f, 0.75f, 0.85f);
            if (GUILayout.Button(isEs ? "Paso 2: Ubicación y Evidencia" : "Step 2: Location & Evidence", GUILayout.Height(26)))
            {
                if (string.IsNullOrEmpty(reportDescriptionInput.Trim()))
                {
                    citizenReportErrorMessage = isEs ? "Por favor ingrese una descripción antes de continuar a ubicación y evidencia." : "Please enter an issue description before proceeding to location & evidence.";
                }
                else
                {
                    citizenReportFormSubStep = CitizenReportFormSubStep.LocationAndEvidence;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // Bounded vertical scrollview:
            // Ensures responsive rendering across 1280x720 and 1920x1080 without overflowing dialog boundaries
            citizenReportFormScrollPos = GUILayout.BeginScrollView(
                citizenReportFormScrollPos,
                false,                         // alwaysShowHorizontal: NEVER show horizontal scrollbar
                false,                        // alwaysShowVertical: automatic vertical scrollbar on the right
                GUIStyle.none,                // horizontalScrollbar: completely suppressed
                GUI.skin.verticalScrollbar,   // verticalScrollbar: standard right scrollbar
                GUIStyle.none,                // background: transparent
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            if (citizenReportFormSubStep == CitizenReportFormSubStep.Details)
            {
                // Emergency Notice Banner (Dynamic calculated height)
                string emText = isEs
                    ? "[!] AVISO: Ante emergencias médicas, incendios o delitos en progreso, llame de inmediato al 911. Este canal simula únicamente reportes de servicios municipales no urgentes."
                    : "[!] NOTICE: For medical emergencies, active fires, or crimes in progress, call 911 immediately. This channel simulates non-urgent municipal services only.";

                GUIStyle emBannerStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = new Color(1f, 0.45f, 0.45f) }
                };
                float bannerAvailableWidth = Mathf.Min(Screen.width - 24f, 880f) - 64f;
                float emBannerHeight = emBannerStyle.CalcHeight(new GUIContent(emText), bannerAvailableWidth);
                if (emBannerHeight < 36f) emBannerHeight = 36f;
                GUILayout.Box(emText, emBannerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(emBannerHeight));
                GUILayout.Space(4);

                // Step 1: Category Selection (9 Municipal Areas in 3x3 Grid)
                GUILayout.Label(isEs ? "1. Seleccione la categoría municipal de la incidencia:" : "1. Select the municipal category:", GetSubheadingStyle());

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

                // Step 2: Description Input
                GUILayout.Label(isEs ? "2. Describa la afectación observada o solicitud de servicio:" : "2. Describe the observed issue or service request:", GetSubheadingStyle());
                reportDescriptionInput = GUILayout.TextArea(reportDescriptionInput, GUILayout.Height(50));

                // Quick example chips
                GUILayout.BeginHorizontal();
                GUILayout.Label(isEs ? "Ejemplos rápidos:" : "Quick examples:", GetMutedStyle(), GUILayout.Width(95));
                if (GUILayout.Button(isEs ? "Bache en calzada" : "Pothole in road", GUILayout.Height(20)))
                {
                    reportDescriptionInput = isEs ? "Bache profundo en el carril vehicular que daña neumáticos y suspensión." : "Deep pothole in the vehicular lane damaging tires and suspension.";
                    selectedCategoryIndex = 0; // road-surface-damage
                }
                if (GUILayout.Button(isEs ? "Basura acumulada" : "Accumulated waste", GUILayout.Height(20)))
                {
                    reportDescriptionInput = isEs ? "Bolsas de basura acumuladas y tiradero clandestino obstruyendo la vía pública." : "Accumulated trash bags and clandestine dumping obstructing the public way.";
                    selectedCategoryIndex = 2; // cleaning-and-waste
                }
                if (GUILayout.Button(isEs ? "Fuga / Drenaje" : "Water / Drain", GUILayout.Height(20)))
                {
                    reportDescriptionInput = isEs ? "Encharcamiento importante de aguas limpias por fuga en la red hidráulica." : "Significant standing clean water due to hydraulic distribution leak.";
                    selectedCategoryIndex = 3; // water-and-drainage
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                GUILayout.BeginHorizontal();
                GUILayout.Space(99);
                if (GUILayout.Button(isEs ? "Lámpara apagada" : "Broken light", GUILayout.Height(20)))
                {
                    reportDescriptionInput = isEs ? "Luminaria pública apagada dejando el sector peatonal a oscuras." : "Streetlight out leaving the pedestrian sector in complete darkness.";
                    selectedCategoryIndex = 4; // public-lighting
                }
                if (GUILayout.Button(isEs ? "Banqueta rota" : "Broken sidewalk", GUILayout.Height(20)))
                {
                    reportDescriptionInput = isEs ? "Banqueta fracturada con escombro que impide el paso a personas con discapacidad." : "Fractured sidewalk with debris preventing access for people with disabilities.";
                    selectedCategoryIndex = 1; // sidewalk-obstruction
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Step 1 Primary Action: Advance to Step 2
                GUI.color = new Color(0.25f, 0.95f, 0.85f);
                if (GUILayout.Button(isEs ? "[->] Continuar a Ubicación y Evidencia" : "[->] Continue to Location & Evidence", GUILayout.Height(34)))
                {
                    if (string.IsNullOrEmpty(reportDescriptionInput.Trim()))
                    {
                        citizenReportErrorMessage = isEs ? "Por favor ingrese una descripción de la incidencia." : "Please enter an issue description.";
                    }
                    else
                    {
                        citizenReportErrorMessage = null;
                        citizenReportFormSubStep = CitizenReportFormSubStep.LocationAndEvidence;
                    }
                }
                GUI.color = Color.white;
            }
            else // CitizenReportFormSubStep.LocationAndEvidence
            {
                // Step 3: Incident Location
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
                    ? "[*] Incidencias en el corredor de Av. Juárez cuentan con coordenadas verificadas. En otros sectores se registran a nivel descriptivo sin inventar coordenadas."
                    : "[*] Incidents on Av. Juárez have verified coordinates. In other sectors, locations are recorded descriptively without invented coordinates.",
                    GetMutedStyle());

                GUILayout.Space(6);

                // Step 4: Photographic Evidence (Optional)
                GUILayout.Label(isEs ? "4. Evidencia fotográfica (Opcional — PNG o JPEG, máx 5 MB):" : "4. Photographic evidence (Optional — PNG or JPEG, max 5 MB):", GetSubheadingStyle());

                GUILayout.BeginHorizontal();
                if (selectedImagePreview == null)
                {
                    GUILayout.BeginVertical();
                    if (GUILayout.Button(isEs ? "[Foto] Adjuntar Fotografía (Opcional)..." : "[Photo] Attach Photograph (Optional)...", GUILayout.Width(260), GUILayout.Height(28)))
                    {
                        string filePath = WindowsFileDialog.OpenImageFileDialog(isEs ? "Seleccionar Fotografía de Incidencia" : "Select Issue Photograph");
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            LoadSelectedImage(filePath, isEs);
                        }
                    }
                    GUILayout.Label(isEs ? "[i] Puede continuar sin fotografía si no cuenta con una (trámite 100% válido)." : "[i] You may proceed without photo if unavailable (100% valid report).", GetMutedStyle());
                    GUILayout.EndVertical();
                }
                else
                {
                    // Fixed thumbnail box with aspect ratio preservation
                    Rect thumbRect = GUILayoutUtility.GetRect(110, 75, GUILayout.Width(110), GUILayout.Height(75));
                    GUI.Box(thumbRect, GUIContent.none, GetDarkPlateStyle());
                    GUI.DrawTexture(thumbRect, selectedImagePreview, ScaleMode.ScaleToFit);

                    GUILayout.BeginVertical();
                    GUILayout.Label($"[Foto] {selectedImageFileName}", GetSubheadingStyle());
                    int kb = (selectedImageBytes != null) ? (selectedImageBytes.Length / 1024) : 0;
                    GUILayout.Label(isEs ? $"Tamaño: {kb} KB · Formato: {selectedImageMime}" : $"Size: {kb} KB · Format: {selectedImageMime}", GetMutedStyle());

                    // External AI Consent Toggle
                    externalAiConsent = GUILayout.Toggle(externalAiConsent, isEs
                        ? " Autorizo análisis visual externo con IA (Meta SAM 3.1)"
                        : " Authorize external AI visual analysis (Meta SAM 3.1)");

                    if (externalAiConsent)
                    {
                        GUILayout.Label(isEs
                            ? "[SAM 3.1] La imagen se procesará para segmentación visual asistida."
                            : "[SAM 3.1] Image will be processed for assisted visual segmentation.",
                            GetMutedStyle());
                    }
                    else
                    {
                        GUILayout.Label(isEs
                            ? "[Privacidad] La imagen se guardará localmente pero NO se enviará a Meta."
                            : "[Privacy] Image will be stored locally but NOT sent to Meta.",
                            GetMutedStyle());
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(isEs ? "Cambiar Fotografía..." : "Replace Photo...", GUILayout.Width(140), GUILayout.Height(20)))
                    {
                        string filePath = WindowsFileDialog.OpenImageFileDialog(isEs ? "Seleccionar Nueva Fotografía" : "Select Replacement Photograph");
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            LoadSelectedImage(filePath, isEs);
                        }
                    }
                    if (GUILayout.Button(isEs ? "[X] Quitar Fotografía" : "[X] Remove Photo", GUILayout.Width(140), GUILayout.Height(20)))
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
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Step 2 Bottom Navigation: Back to Details or Generate Draft
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isEs ? "<- Volver a Detalles" : "<- Back to Details", GUILayout.Width(160), GUILayout.Height(34)))
                {
                    citizenReportFormSubStep = CitizenReportFormSubStep.Details;
                }

                GUILayout.FlexibleSpace();

                GUI.color = new Color(0.25f, 0.95f, 0.85f);
                if (GUILayout.Button(isEs ? "[->] Generar Borrador del Reporte (Muse Spark 1.3)" : "[->] Generate Report Draft (Muse Spark 1.3)", GUILayout.Width(350), GUILayout.Height(34)))
                {
                    if (string.IsNullOrEmpty(reportDescriptionInput.Trim()))
                    {
                        citizenReportErrorMessage = isEs ? "Por favor ingrese una descripción de la incidencia." : "Please enter an issue description.";
                        citizenReportFormSubStep = CitizenReportFormSubStep.Details;
                    }
                    else
                    {
                        TriggerDraftAnalysis(isEs);
                    }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
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

            string title = isEs ? "[...] Formulando Borrador de Reporte Ciudadano" : "[...] Formulating Citizen Report Draft";
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

            GUILayout.Label(isEs ? "[Borrador] Revisión del Borrador del Reporte Ciudadano" : "[Draft] Citizen Report Draft Review", GetModalSubtitleStyle());
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
                GUILayout.Box($"[ALERTA] {emMsg}", alertStyle, GUILayout.Height(40));
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
                    isEs ? "[Zoom] Clic en la imagen para ampliar inspección" : "[Zoom] Click image to enlarge & inspect", thumbHintStyle);

                GUILayout.Space(4);

                // Detailed inspection button below thumbnail
                GUI.color = new Color(0.3f, 0.95f, 0.85f);
                if (GUILayout.Button(isEs ? "[Inspección] Inspección Visual de Segmentación" : "[Inspect] Visual Segmentation Inspection", GUILayout.Width(260), GUILayout.Height(24)))
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
                GUILayout.Box(isEs ? "[Info] Reporte sin fotografía adjunta\n(Válido para registro y canalización simulada)" : "[Info] Report without attached photo\n(Valid for simulated registration & routing)", noPhotoBox, GUILayout.Height(100));
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
            if (GUILayout.Button(isEs ? "<- Modificar Datos" : "<- Edit Details", GUILayout.Width(160), GUILayout.Height(32)))
            {
                CloseEnlargedImageViewer();
                citizenReportStep = CitizenReportWorkflowStep.FormInput;
            }

            GUILayout.FlexibleSpace();

            GUI.color = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button(isEs ? "[OK] Confirmar y Registrar en World Core" : "[OK] Confirm and Register in World Core", GUILayout.Width(330), GUILayout.Height(32)))
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
            GUILayout.Box(isEs ? "[COMPROBANTE] Reporte Ciudadano (Demostración Simulada)" : "[RECEIPT] Citizen Report Receipt (Simulated Demonstration)", receiptHeaderStyle, GUILayout.Height(30));

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
            if (GUILayout.Button(isEs ? "[+] Registrar Otra Incidencia" : "[+] Register Another Issue", GUILayout.Width(220), GUILayout.Height(32)))
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

            if (GUILayout.Button(isEs ? "[X] Cerrar [Esc]" : "[X] Close [Esc]", GUILayout.Width(130), GUILayout.Height(32)))
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
                modalBackdropTex.SetPixel(0, 0, new Color(10f / 255f, 14f / 255f, 22f / 255f, 0.72f)); // rgba(10, 14, 22, 0.72)
                modalBackdropTex.Apply();
                modalBackdropStyle.normal.background = modalBackdropTex;
            }
            return modalBackdropStyle;
        }

        private void DrawModalScrim()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, GetModalBackdropStyle());
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
            GUILayout.Label(isEs ? "[SAM 3.1] Inspección Visual de Evidencia · Segmentación SAM 3.1" : "[SAM 3.1] Visual Evidence Inspection · SAM 3.1 Segmentation", GetModalTitleStyle());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isEs ? "[X] Cerrar [Esc]" : "[X] Close [Esc]", GUILayout.Width(110), GUILayout.Height(24)))
            {
                CloseEnlargedImageViewer();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Row 1: Clean Segmented Display Mode Selector (Task A3)
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
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Row 2: Dedicated Secondary Control Row for Bounding Boxes (Task A3)
            GUILayout.BeginHorizontal();
            showBoundingBoxesOverlay = GUILayout.Toggle(showBoundingBoxesOverlay, isEs ? " Mostrar Cajas Delimitadoras (Bounding Boxes)" : " Show Bounding Boxes");
            GUILayout.Space(12);
            GUILayout.Label(isEs ? "(Visualiza límites rectangulares y niveles de confianza por región)" : "(Displays bounding rectangular limits & confidence levels per region)", GetMutedStyle());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Status Banner Truth Table (Prompt Section 4.4)
            DrawSegmentationStatusBanner(isEs);

            GUILayout.Space(4);

            // Main Display Area (Aspect Ratio Preserved)
            float availableImageHeight = vh - 200f;
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
                    ? $"[OK] Segmentación verificable: {count} región(es) detectada(s) con máscaras de píxeles generadas por SAM 3.1."
                    : $"[OK] Verifiable segmentation: {count} region(s) detected with authentic pixel masks generated by SAM 3.1.";
                bannerColor = new Color(0.25f, 0.95f, 0.45f);
            }
            else if (va != null && va.boxes != null && va.boxes.Length > 0 && !va.hasAuthenticMasks)
            {
                bannerText = isEs
                    ? $"[!] Delimitación por cuadros únicamente: {va.boxes.Length} región(es) detectada(s) sin máscaras de píxeles individuales."
                    : $"[!] Bounding boxes only: {va.boxes.Length} region(s) detected without individual pixel masks.";
                bannerColor = new Color(1.0f, 0.85f, 0.25f);
            }
            else if (va != null && (va.provider == "not-authorized" || (currentReportDraft != null && !currentReportDraft.externalAiProcessingConsent)))
            {
                bannerText = isEs
                    ? "[Privacidad] Análisis visual externo no autorizado por el usuario (evidencia fotográfica preservada localmente sin inferencia externa)."
                    : "[Privacy] External visual analysis not authorized by user (photo preserved locally without external AI inference).";
                bannerColor = new Color(0.4f, 0.85f, 1f);
            }
            else if (va != null && (va.provider.Contains("unavailable") || va.status == "unavailable"))
            {
                bannerText = isEs
                    ? "[!] Servicio no disponible: No se pudo conectar con el modelo SAM 3.1 en este momento."
                    : "[!] Service unavailable: Could not connect to SAM 3.1 model at this time.";
                bannerColor = new Color(1f, 0.55f, 0.35f);
            }
            else if (va != null && va.status == "inconclusive" && (va.boxes == null || va.boxes.Length == 0))
            {
                bannerText = isEs
                    ? "[i] Sin detecciones coincidentes: SAM 3.1 no detectó regiones que correspondan al concepto en esta fotografía."
                    : "[i] Zero matching detections: SAM 3.1 found no candidate regions matching the target concept in this photograph.";
                bannerColor = new Color(0.7f, 0.85f, 0.95f);
            }
            else
            {
                bannerText = isEs
                    ? "[i] Geometría de segmentación no disponible o datos no válidos."
                    : "[i] Segmentation geometry not available or invalid data.";
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
                    string chipText = $"[#] {obj.label} [{obj.objectId}{confStr}]";
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
                    string chipText = $"[] {b.label ?? "región"} [{i + 1}{confStr}]";
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
            if (userBubbleTex != null)
            {
                Destroy(userBubbleTex);
                userBubbleTex = null;
            }
            if (npcBubbleTex != null)
            {
                Destroy(npcBubbleTex);
                npcBubbleTex = null;
            }
            if (chatFrameTex != null)
            {
                Destroy(chatFrameTex);
                chatFrameTex = null;
            }
            if (tableCardTex != null)
            {
                Destroy(tableCardTex);
                tableCardTex = null;
            }
            if (navCardTex != null)
            {
                Destroy(navCardTex);
                navCardTex = null;
            }
            CleanupFideicomisoCameraEasterEggResources();
        }

        #region Secret Fideicomiso Surveillance Camera Easter Egg (Paso del Norte - Vista Sur -> YouTube Live)

        private bool WasTogglePipCursorKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Tab)) return true;
            }
            catch { }
#endif
            return false;
        }

        private bool IsMouseInsideFideicomisoPipWindow()
        {
            Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (mousePos == Vector2.zero)
            {
                try { mousePos = Input.mousePosition; } catch { }
            }
#endif
            Vector2 guiPos = new Vector2(mousePos.x, Screen.height - mousePos.y);
            return fideicomisoPipWindowRect.Contains(guiPos);
        }

        private bool TryTriggerFideicomisoCameraEasterEggClick()
        {
            if (playerCamera == null) return false;
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;

            Ray ray;
            if (isCursorLocked)
            {
                ray = new Ray(cam.transform.position, cam.transform.forward);
            }
            else
            {
                // If cursor is unlocked and clicking inside the open PiP window, let OnGUI handle the button click
                if (isFideicomisoCameraPipOpen && IsMouseInsideFideicomisoPipWindow())
                {
                    return false;
                }
                Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) mousePos = Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (mousePos == Vector2.zero)
                {
                    try { mousePos = Input.mousePosition; } catch { }
                }
#endif
                ray = cam.ScreenPointToRay(mousePos);
            }

            // Use RaycastAll (75m range, including trigger colliders) so nothing blocks clicking the facade camera
            RaycastHit[] hits = Physics.RaycastAll(ray, 75.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    Transform t = hits[i].transform;
                    while (t != null)
                    {
                        if (t.name == FideicomisoCameraObjectName)
                        {
                            OpenFideicomisoCameraEasterEgg();
                            return true;
                        }
                        t = t.parent;
                    }
                }
            }

            // Generous angular fallback: if the player is within 60m of the Fideicomiso camera and aiming within 2.5 degrees of it
            Vector3 cameraWorldPos = new Vector3(2.40f, 8.20f, 40.65f);
            Vector3 toCam = cameraWorldPos - ray.origin;
            float dist = toCam.magnitude;
            if (dist <= 65.0f)
            {
                float angle = Vector3.Angle(ray.direction, toCam.normalized);
                if (angle <= 2.4f)
                {
                    OpenFideicomisoCameraEasterEgg();
                    return true;
                }
            }

            return false;
        }

        public void OpenFideicomisoCameraEasterEgg()
        {
            isFideicomisoCameraPipOpen = true;
            EnsureFideicomisoCctvCameraAndProxy();
            if (fideicomisoYoutubeThumbTex == null && !isFetchingFideicomisoYoutubeThumb)
            {
                StartCoroutine(FetchFideicomisoYoutubeThumbnailCoroutine());
            }
            // Unlock cursor immediately so the player can click [Expandir en YouTube] or toggle modes
            LockCursor(false);
        }

        public void CloseFideicomisoCameraEasterEgg()
        {
            isFideicomisoCameraPipOpen = false;
            if (fideicomisoCctvCamera != null)
            {
                fideicomisoCctvCamera.enabled = false;
            }
            if (playerSelfCctvProxy != null)
            {
                playerSelfCctvProxy.SetActive(false);
            }
            LockCursor(true);
        }

        private void EnsureFideicomisoCctvCameraAndProxy()
        {
            if (fideicomisoCctvRenderTexture == null)
            {
                fideicomisoCctvRenderTexture = new RenderTexture(512, 288, 16, RenderTextureFormat.ARGB32);
                fideicomisoCctvRenderTexture.name = "Fideicomiso_CCTV_VistaSur_RT";
                fideicomisoCctvRenderTexture.Create();
            }

            if (fideicomisoCctvCamera == null)
            {
                GameObject cctvGo = new GameObject("Fideicomiso_CCTV_LiveCamera_Runtime");
                // Optical center just in front of Bridge_Fideicomiso_Camera_VistaSur lens looking South (28 deg pitch)
                cctvGo.transform.position = new Vector3(2.40f, 8.15f, 40.20f);
                cctvGo.transform.rotation = Quaternion.Euler(28f, 180f, 0f);

                fideicomisoCctvCamera = cctvGo.AddComponent<Camera>();
                fideicomisoCctvCamera.fieldOfView = 55f;
                fideicomisoCctvCamera.nearClipPlane = 0.25f;
                fideicomisoCctvCamera.farClipPlane = 260f;
                fideicomisoCctvCamera.targetTexture = fideicomisoCctvRenderTexture;

                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    fideicomisoCctvCamera.clearFlags = mainCam.clearFlags;
                    fideicomisoCctvCamera.backgroundColor = mainCam.backgroundColor;
                }
            }
            fideicomisoCctvCamera.enabled = true;

            // Create a visible player avatar proxy so the player can see themselves walking on the CCTV monitor!
            if (playerSelfCctvProxy == null)
            {
                playerSelfCctvProxy = new GameObject("Player_CCTV_SelfAvatar_Proxy");
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

                // Torso / Jacket (Cyan/Blue traveler jacket)
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Avatar_Torso";
                body.transform.SetParent(playerSelfCctvProxy.transform);
                body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                body.transform.localScale = new Vector3(0.52f, 0.58f, 0.42f);
                Destroy(body.GetComponent<Collider>());
                if (litShader != null)
                {
                    Material torsoMat = new Material(litShader) { color = new Color(0.12f, 0.52f, 0.88f) };
                    body.GetComponent<Renderer>().sharedMaterial = torsoMat;
                }

                // Head
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Avatar_Head";
                head.transform.SetParent(playerSelfCctvProxy.transform);
                head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                head.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
                Destroy(head.GetComponent<Collider>());
                if (litShader != null)
                {
                    Material headMat = new Material(litShader) { color = new Color(0.92f, 0.76f, 0.62f) };
                    head.GetComponent<Renderer>().sharedMaterial = headMat;
                }

                // Cap / Hair
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Avatar_Cap";
                cap.transform.SetParent(playerSelfCctvProxy.transform);
                cap.transform.localPosition = new Vector3(0f, 1.80f, 0.03f);
                cap.transform.localScale = new Vector3(0.38f, 0.05f, 0.42f);
                Destroy(cap.GetComponent<Collider>());
                if (litShader != null)
                {
                    Material capMat = new Material(litShader) { color = new Color(0.15f, 0.18f, 0.24f) };
                    cap.GetComponent<Renderer>().sharedMaterial = capMat;
                }
            }
            UpdateFideicomisoPlayerSelfAvatarProxy();
        }

        private void UpdateFideicomisoPlayerSelfAvatarProxy()
        {
            if (playerSelfCctvProxy == null) return;
            if (!isFideicomisoCameraPipOpen)
            {
                if (playerSelfCctvProxy.activeSelf) playerSelfCctvProxy.SetActive(false);
                return;
            }

            // Place the avatar slightly behind the first-person camera along horizontal forward so it never clips the player's own view
            Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            playerSelfCctvProxy.transform.position = transform.position - (flatForward * 0.42f);
            playerSelfCctvProxy.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (!playerSelfCctvProxy.activeSelf) playerSelfCctvProxy.SetActive(true);
        }

        private IEnumerator FetchFideicomisoYoutubeThumbnailCoroutine()
        {
            isFetchingFideicomisoYoutubeThumb = true;
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(FideicomisoYoutubeThumbUrl))
            {
                uwr.timeout = 8;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    fideicomisoYoutubeThumbTex = DownloadHandlerTexture.GetContent(uwr);
                }
            }
            isFetchingFideicomisoYoutubeThumb = false;
        }

        private void DrawFideicomisoCameraEasterEggPiP()
        {
            bool isEs = (currentLanguage == "es");
            float winW = 548f;
            float winH = 382f;
            float winX = Screen.width - winW - 18f;
            float winY = Screen.height - winH - 48f;
            fideicomisoPipWindowRect = new Rect(winX, winY, winW, winH);

            Color prevColor = GUI.color;

            // Outer YouTube Dark Mode Card Frame
            GUI.color = new Color(0.06f, 0.07f, 0.09f, 0.97f);
            GUI.DrawTexture(fideicomisoPipWindowRect, Texture2D.whiteTexture);

            // Top Accent Border (YouTube Red)
            GUI.color = new Color(0.92f, 0.12f, 0.16f, 1f);
            GUI.DrawTexture(new Rect(winX, winY, winW, 3f), Texture2D.whiteTexture);
            GUI.color = prevColor;

            // 1. Header Bar (YouTube Live Title + Mode Toggle + Close)
            Rect headerRect = new Rect(winX + 10f, winY + 7f, winW - 20f, 26f);
            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();

            // Blinking LIVE badge
            bool blinkOn = (Mathf.FloorToInt(Time.unscaledTime * 2.0f) % 2) == 0;
            GUIStyle liveBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.color = blinkOn ? new Color(0.92f, 0.12f, 0.15f, 1f) : new Color(0.68f, 0.10f, 0.12f, 1f);
            GUILayout.Box(isEs ? "● EN VIVO" : "● LIVE", liveBadgeStyle, GUILayout.Width(68f), GUILayout.Height(22f));
            GUI.color = prevColor;

            GUILayout.Space(6f);

            GUIStyle headerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.95f, 0.97f, 1f) }
            };
            GUILayout.Label("Fideicomiso de Puentes Fronterizos — Paso del Norte", headerTitleStyle);

            GUILayout.FlexibleSpace();

            // Close Button [X]
            GUIStyle closeBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.85f) }
            };
            if (GUILayout.Button("✕", closeBtnStyle, GUILayout.Width(28f), GUILayout.Height(22f)))
            {
                CloseFideicomisoCameraEasterEgg();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. Video Viewport Area (16:9 ratio: 528 x 297)
            Rect videoRect = new Rect(winX + 10f, winY + 36f, 528f, 297f);
            GUI.color = Color.black;
            GUI.DrawTexture(videoRect, Texture2D.whiteTexture);
            GUI.color = prevColor;

            if (!fideicomisoPipShowRealYoutubeThumb && fideicomisoCctvRenderTexture != null)
            {
                // Draw Live 3D CCTV RenderTexture
                GUI.DrawTexture(videoRect, fideicomisoCctvRenderTexture, ScaleMode.StretchToFill, false);
            }
            else if (fideicomisoYoutubeThumbTex != null)
            {
                // Draw Real YouTube Live Stream Thumbnail fetched from i.ytimg.com/vi/IcvugJWPXz8/hqdefault.jpg
                GUI.DrawTexture(videoRect, fideicomisoYoutubeThumbTex, ScaleMode.ScaleAndCrop, false);
            }
            else
            {
                // Fallback to CCTV RenderTexture while fetching
                if (fideicomisoCctvRenderTexture != null)
                {
                    GUI.DrawTexture(videoRect, fideicomisoCctvRenderTexture, ScaleMode.StretchToFill, false);
                }
            }

            // 3. Authentic Fideicomiso de Puentes Fronterizos OSD Overlays on top of the video feed!
            if (!fideicomisoPipShowRealYoutubeThumb)
            {
                // Top-Left Black OSD Timestamp & Camera Name (matching real photo)
                string nowTimestamp = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
                Rect osdTopLeft1 = new Rect(videoRect.x + 4f, videoRect.y + 4f, 188f, 18f);
                Rect osdTopLeft2 = new Rect(videoRect.x + 4f, videoRect.y + 22f, 206f, 18f);
                GUI.color = new Color(0f, 0f, 0f, 0.86f);
                GUI.DrawTexture(osdTopLeft1, Texture2D.whiteTexture);
                GUI.DrawTexture(osdTopLeft2, Texture2D.whiteTexture);
                GUI.color = prevColor;

                GUIStyle osdMonoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.96f, 0.96f, 0.96f) }
                };
                GUI.Label(new Rect(osdTopLeft1.x + 4f, osdTopLeft1.y, osdTopLeft1.width - 6f, osdTopLeft1.height), nowTimestamp, osdMonoStyle);
                GUI.Label(new Rect(osdTopLeft2.x + 4f, osdTopLeft2.y, osdTopLeft2.width - 6f, osdTopLeft2.height), "Paso del Norte (Vista Sur)", osdMonoStyle);

                // Top-Right Black Banner: "DESCARGA LA APP PUENTES FRONTERIZOS"
                Rect osdTopRight = new Rect(videoRect.xMax - 246f, videoRect.y + 4f, 242f, 18f);
                GUI.color = new Color(0f, 0f, 0f, 0.86f);
                GUI.DrawTexture(osdTopRight, Texture2D.whiteTexture);
                GUI.color = prevColor;

                GUIStyle osdRightStyle = new GUIStyle(osdMonoStyle)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(osdTopRight, "DESCARGA LA APP PUENTES FRONTERIZOS", osdRightStyle);

                // Left Blue Weather Card (AccuWeather 13 °C Replica from photograph)
                Rect weatherRect = new Rect(videoRect.x + 4f, videoRect.y + 44f, 108f, 92f);
                GUI.color = new Color(0.14f, 0.24f, 0.56f, 0.90f);
                GUI.DrawTexture(weatherRect, Texture2D.whiteTexture);
                GUI.color = prevColor;

                GUIStyle weatherSmall = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 9,
                    normal = { textColor = new Color(0.88f, 0.93f, 1f) }
                };
                GUIStyle weatherBig = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(weatherRect.x + 6f, weatherRect.y + 3f, 98f, 14f), "Clima    Cd. Juárez", weatherSmall);
                GUI.Label(new Rect(weatherRect.x + 6f, weatherRect.y + 18f, 98f, 22f), "☁ 13 °C", weatherBig);
                GUI.Label(new Rect(weatherRect.x + 6f, weatherRect.y + 40f, 98f, 14f), "Sensación: 13.2 °C", weatherSmall);
                GUI.Label(new Rect(weatherRect.x + 6f, weatherRect.y + 54f, 98f, 14f), "Parcialmente nublado", weatherSmall);
                GUI.Label(new Rect(weatherRect.x + 6f, weatherRect.y + 72f, 98f, 14f), "☀ AccuWeather", weatherSmall);

                // Subtle Bottom-Left Self-Mirror Hint inside video
                Rect mirrorHintRect = new Rect(videoRect.x + 6f, videoRect.yMax - 24f, 340f, 18f);
                GUI.color = new Color(0f, 0f, 0f, 0.68f);
                GUI.DrawTexture(mirrorHintRect, Texture2D.whiteTexture);
                GUI.color = prevColor;
                GUIStyle mirrorHintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.55f, 0.95f, 1f) }
                };
                GUI.Label(
                    new Rect(mirrorHintRect.x + 5f, mirrorHintRect.y, mirrorHintRect.width - 8f, mirrorHintRect.height),
                    isEs ? "🎥 Espejo CCTV Activo: Camina con WASD frente al puente para verte en cámara" : "🎥 Live CCTV Mirror: Walk with WASD in front of the bridge to see yourself",
                    mirrorHintStyle);
            }
            else
            {
                // Real YouTube Thumbnail Overlay badge
                Rect realBadgeRect = new Rect(videoRect.x + 8f, videoRect.yMax - 28f, 360f, 22f);
                GUI.color = new Color(0f, 0f, 0f, 0.80f);
                GUI.DrawTexture(realBadgeRect, Texture2D.whiteTexture);
                GUI.color = prevColor;
                GUIStyle realBadgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.92f, 0.45f) }
                };
                GUI.Label(
                    new Rect(realBadgeRect.x + 6f, realBadgeRect.y + 2f, realBadgeRect.width - 10f, 18f),
                    isEs ? "🌐 Captura Real YouTube Live (youtu.be/IcvugJWPXz8)" : "🌐 Real YouTube Live Snapshot (youtu.be/IcvugJWPXz8)",
                    realBadgeStyle);
            }

            // YouTube Red Scrubbing Line at bottom of video frame
            GUI.color = new Color(0.95f, 0.12f, 0.16f, 1f);
            GUI.DrawTexture(new Rect(videoRect.x, videoRect.yMax - 3f, videoRect.width, 3f), Texture2D.whiteTexture);
            GUI.color = prevColor;

            // 4. Bottom Action Controls Bar (Mode Switch + Direct YouTube Redirect Button)
            Rect footerRect = new Rect(winX + 10f, winY + 340f, winW - 20f, 34f);
            GUILayout.BeginArea(footerRect);
            GUILayout.BeginHorizontal();

            // Toggle between Live 3D CCTV Mirror and Real YouTube Stream Thumbnail
            string toggleModeLabel = fideicomisoPipShowRealYoutubeThumb
                ? (isEs ? "🎥 Ver Espejo Virtual 3D" : "🎥 Switch to 3D CCTV")
                : (isEs ? "🌐 Ver Miniatura Real YT" : "🌐 Real YouTube Preview");

            if (GUILayout.Button(toggleModeLabel, GUILayout.Width(175f), GUILayout.Height(28f)))
            {
                fideicomisoPipShowRealYoutubeThumb = !fideicomisoPipShowRealYoutubeThumb;
                if (fideicomisoPipShowRealYoutubeThumb && fideicomisoYoutubeThumbTex == null && !isFetchingFideicomisoYoutubeThumb)
                {
                    StartCoroutine(FetchFideicomisoYoutubeThumbnailCoroutine());
                }
            }

            GUILayout.Space(6f);

            // Hint for cursor toggle [C]
            GUIStyle cursorHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.78f, 0.86f) }
            };
            GUILayout.Label(isEs ? "[C/Tab]: Ratón" : "[C/Tab]: Cursor", cursorHintStyle, GUILayout.Width(92f), GUILayout.Height(28f));

            GUILayout.FlexibleSpace();

            // Primary CTA: Expand / Watch Live Stream on YouTube (https://www.youtube.com/watch?v=IcvugJWPXz8)
            GUIStyle ytExpandBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.96f, 0.45f) }
            };
            string expandLabel = isEs ? "⛶ Expandir en YouTube ↗" : "⛶ Expand on YouTube ↗";
            if (GUILayout.Button(expandLabel, ytExpandBtnStyle, GUILayout.Width(242f), GUILayout.Height(28f)))
            {
                Application.OpenURL(FideicomisoYoutubeLiveUrl);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CleanupFideicomisoCameraEasterEggResources()
        {
            if (fideicomisoCctvCamera != null)
            {
                Destroy(fideicomisoCctvCamera.gameObject);
                fideicomisoCctvCamera = null;
            }
            if (fideicomisoCctvRenderTexture != null)
            {
                fideicomisoCctvRenderTexture.Release();
                Destroy(fideicomisoCctvRenderTexture);
                fideicomisoCctvRenderTexture = null;
            }
            if (fideicomisoYoutubeThumbTex != null)
            {
                Destroy(fideicomisoYoutubeThumbTex);
                fideicomisoYoutubeThumbTex = null;
            }
            if (playerSelfCctvProxy != null)
            {
                Destroy(playerSelfCctvProxy);
                playerSelfCctvProxy = null;
            }
        }

        #endregion
        #endregion
    }
}
