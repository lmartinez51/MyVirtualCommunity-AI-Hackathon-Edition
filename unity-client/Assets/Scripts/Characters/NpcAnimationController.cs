using System;
using UnityEngine;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Client.World
{
    public enum NpcVisualBehaviorState
    {
        Walking = 0,
        ContextualActivity = 1,
        GreetingCue = 2,
        AttentiveService = 3,
        RestingOrOffDuty = 4,
        NeutralFallback = 5
    }

    public enum NpcAnimationRole
    {
        GenericResident = 0,
        BorderConcierge = 1,
        PharmacyAttendant = 2,
        OpticsAttendant = 3,
        StudentCommuter = 4,
        RetailCommuter = 5,
        LocalArtisan = 6,
        CitizenRepresentative = 7
    }

    public enum NpcVisualAwarenessState
    {
        Unaware = 0,
        PlayerAware = 1,
        PlayerAttentive = 2,
        NpcAware = 3,
        SocialEngaged = 4,
        ReturningToActivity = 5
    }

    public enum NpcVisualSocialInteractionPhase
    {
        None = 0,
        Initiating = 1,
        Engaged = 2,
        Closing = 3,
        Completed = 4
    }

    /// <summary>
    /// Shared presentation-only animation controller for the seven canonical NPCs.
    ///
    /// Architectural Invariants:
    /// 1. Presentation only: never modifies authoritative NPC world coordinates or waypoint state.
    /// 2. Root transform safety: enforces Animator.applyRootMotion = false if an Animator exists,
    ///    and never mutates the authoritative root transform position or yaw.
    /// 3. Articulated Bobblehead bone offsets are strictly applied to child joints relative to cached rest poses;
    ///    models without rigged joints fall back to subtle motion on an isolated visual child transform.
    /// 4. Consumes existing authoritative movement and interaction context without adding API polling.
    /// </summary>
    public class NpcAnimationController : MonoBehaviour
    {
        [Header("Canonical NPC Binding")]
        [SerializeField] private string characterId = "";
        [SerializeField] private NpcAnimationRole animationRole = NpcAnimationRole.GenericResident;
        [SerializeField] private InteractionPoint interactionPoint;
        [SerializeField] private BobbleheadCharacter bobbleheadCharacter;
        [SerializeField] private BobbleheadAnimator legacyBobbleheadAnimator;
        [SerializeField] private Animator optionalUnityAnimator;
        [SerializeField] private Transform fallbackVisualChild;

        [Header("Locomotion Hysteresis & Blending")]
        [SerializeField] private float walkStartSpeedThreshold = 0.05f;
        [SerializeField] private float walkStopSpeedThreshold = 0.03f;
        [SerializeField] private float idleExitGraceSeconds = 0.08f;
        [SerializeField] private float walkReferenceFrequency = 3.2f;
        [SerializeField] private float maxLegSwingDegrees = 24f;
        [SerializeField] private float maxArmSwingDegrees = 20f;
        [SerializeField] private float maxKneeBendDegrees = 26f;

        [Header("Head Orientation & Awareness")]
        [SerializeField] private float defaultLookRangeMeters = 6.5f;
        [SerializeField] private float busyLookRangeMeters = 3.5f;
        [SerializeField] private float maxHeadYawDegrees = 42f;
        [SerializeField] private float maxHeadPitchUpDegrees = 14f;
        [SerializeField] private float maxHeadPitchDownDegrees = 18f;
        [SerializeField] private float rearCutoutYawDegrees = 78f;
        [SerializeField] private float headTrackBlendSpeed = 4.5f;
        [SerializeField] private float maxSocialTorsoYawDegrees = 22f;
        [SerializeField] private float maxSocialBodyYawDegrees = 148f;
        [SerializeField] private float maxNpcAwareBodyYawDegrees = 36f;
        [SerializeField] private float bodySocialTurnSpeed = 4.4f;
        [SerializeField] private float socialTargetHoldDurationSeconds = 0.6f;
        [SerializeField] private float returnToActivityDurationSeconds = 0.85f;

        // Cached authoritative state inputs (updated via SyncPresentationInputs or InteractionPoint)
        private string _movementState = "stationary";
        private string _interactionAvailability = "available_normal";
        private string _interactionMode = "normal_service";
        private string _activityState = "available";
        private string _actorArchetype = "";
        private string _currentWaypointId = "";
        private bool _canInteract = true;
        private float _reportedVisualSpeed = 0f;

        // Dirty-check references to avoid unnecessary work or string allocations per frame
        private string _lastMovementStateRef;
        private string _lastAvailabilityRef;
        private string _lastModeRef;
        private string _lastActivityRef;
        private string _lastActorArchetypeRef;
        private string _lastWaypointRef;
        private bool _lastCanInteractVal = true;

        // Deterministic per-NPC timing offsets (prevents synchronized clone motion)
        private float _phaseOffsetRad = 0f;
        private float _breathRateScale = 1.0f;
        private float _swayRateScale = 1.0f;

        // Runtime animation state
        private NpcVisualBehaviorState _currentBehaviorState = NpcVisualBehaviorState.NeutralFallback;
        private float _walkCyclePhase = 0f;
        private float _breathPhase = 0f;
        private float _ambientScanPhase = 0f;
        private float _locomotionBlend = 0f;
        private float _smoothedVisualSpeed = 0f;
        private float _idleGraceElapsed = 0f;
        private bool _isWalkingLatched = false;
        private float _greetingRemainingSeconds = 0f;
        private const float GreetingDurationSeconds = 1.15f;

        // Constrained head-tracking & social orientation state (applied additively to child joints only)
        private float _currentHeadTrackYaw = 0f;
        private float _currentHeadTrackPitch = 0f;
        private float _currentTorsoSocialYaw = 0f;
        private float _currentBodySocialYaw = 0f;

        // Authoritative social awareness presentation state (Increment 5.20.0)
        private NpcVisualAwarenessState _currentAwarenessState = NpcVisualAwarenessState.Unaware;
        private string _authoritativeAwarenessState = "unaware";
        private string _awarenessTargetEntityType = "none";
        private string _awarenessTargetEntityId = "";
        private Vector2 _awarenessDirectionXZ = Vector2.zero;
        private float _awarenessDistanceMeters = -1f;
        private bool _awarenessCanOrient = false;
        private bool _awarenessRetainsContextual = false;
        private bool _hasAuthoritativeAwareness = false;
        private bool _hasExplicitPlayerLookOverride = false;
        private Transform _socialTargetTransform = null;
        private float _targetHoldRemainingSeconds = 0f;
        private float _returningRemainingSeconds = 0f;

        // Autonomous social interaction lifecycle presentation state (Increment 5.21.0)
        private NpcVisualSocialInteractionPhase _currentSocialInteractionPhase = NpcVisualSocialInteractionPhase.None;
        private bool _hasAuthoritativeSocialInteraction = false;
        private string _socialInteractionSessionId = "";
        private string _socialInteractionPartnerNpcId = "";
        private string _socialInteractionConversationalRole = "none";
        private int _socialInteractionTurnIndex = 0;
        private Transform _socialInteractionPartnerTransform = null;
        private float _socialConversationBlend = 0f;
        private float _socialConversationPhaseTimer = 0f;

        // Autonomous locomotion & bounded business activity presentation state (Increment 5.22.0)
        private string _behaviorProfileType = "";
        private string _autonomousBehaviorState = "";
        private string _movementControlMode = "";
        private string _patrolDirection = "none";
        private bool _isSuspendedByPlayerInteraction = false;
        private string _passingEncounterPartnerNpcId = "";
        private string _passingEncounterType = "";

        // Cached initial rest poses
        private bool _posesCached = false;
        private bool _hasArticulatedRig = false;
        private Quaternion _initVisualRootRot = Quaternion.identity;
        private Quaternion _initTorsoRot = Quaternion.identity;
        private Quaternion _initHeadHingeRot = Quaternion.identity;
        private Quaternion _initLeftArmRot = Quaternion.identity;
        private Quaternion _initRightArmRot = Quaternion.identity;
        private Quaternion _initLeftForearmRot = Quaternion.identity;
        private Quaternion _initRightForearmRot = Quaternion.identity;
        private Quaternion _initLeftLegRot = Quaternion.identity;
        private Quaternion _initRightLegRot = Quaternion.identity;
        private Quaternion _initLeftKneeRot = Quaternion.identity;
        private Quaternion _initRightKneeRot = Quaternion.identity;
        private Vector3 _initPelvisPos = Vector3.zero;
        private Quaternion _initPelvisRot = Quaternion.identity;
        private Vector3 _initFallbackChildPos = Vector3.zero;
        private Quaternion _initFallbackChildRot = Quaternion.identity;

        // Cached player head/camera reference for proximity gaze
        private Transform _playerLookTarget;
        private float _nextTargetLookupTime = -10f;

        public string CharacterId => characterId;
        public NpcAnimationRole AnimationRole => animationRole;
        public NpcVisualBehaviorState CurrentBehaviorState => _currentBehaviorState;
        public NpcVisualAwarenessState CurrentAwarenessState => _currentAwarenessState;
        public string CurrentAwarenessTargetType => _awarenessTargetEntityType;
        public string CurrentAwarenessTargetId => _awarenessTargetEntityId;
        public bool HasAuthoritativeAwareness => _hasAuthoritativeAwareness;
        public bool IsWalkingActive => _isWalkingLatched;
        public bool HasArticulatedRig => _hasArticulatedRig;
        public bool HasHeadBone => bobbleheadCharacter != null && bobbleheadCharacter.HeadHinge != null;
        public float CurrentHeadYawDegrees => _currentHeadTrackYaw;
        public float CurrentHeadPitchDegrees => _currentHeadTrackPitch;
        public float CurrentTorsoSocialYawDegrees => _currentTorsoSocialYaw;
        public float CurrentBodySocialYawDegrees => _currentBodySocialYaw;
        public float LocomotionBlendWeight => _locomotionBlend;
        public NpcVisualSocialInteractionPhase CurrentSocialInteractionPhase => _currentSocialInteractionPhase;
        public string CurrentSocialInteractionSessionId => _socialInteractionSessionId;
        public string CurrentSocialInteractionPartnerId => _socialInteractionPartnerNpcId;
        public string CurrentConversationalRole => _socialInteractionConversationalRole;
        public int CurrentSocialTurnIndex => _socialInteractionTurnIndex;
        public float SocialConversationBlendWeight => _socialConversationBlend;
        public bool HasAuthoritativeSocialInteraction => _hasAuthoritativeSocialInteraction;
        public string CurrentBehaviorProfileType => _behaviorProfileType;
        public string CurrentAutonomousBehaviorState => _autonomousBehaviorState;
        public string CurrentMovementControlMode => _movementControlMode;
        public string CurrentPatrolDirection => _patrolDirection;
        public bool IsSuspendedByPlayerInteraction => _isSuspendedByPlayerInteraction;
        public string CurrentPassingEncounterPartnerId => _passingEncounterPartnerNpcId;
        public string CurrentPassingEncounterType => _passingEncounterType;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
        }

        /// <summary>
        /// Binds this animation controller to an InteractionPoint and optional Bobblehead visual rig.
        /// Safe to call in Editor/batchmode tests without entering Play mode.
        /// </summary>
        public void Bind(
            InteractionPoint point,
            BobbleheadCharacter character,
            Transform visualRootTransform = null)
        {
            if (point != null)
            {
                interactionPoint = point;
                if (!string.IsNullOrEmpty(point.characterId))
                {
                    ConfigureRole(point.characterId);
                }
            }

            if (character != null)
            {
                bobbleheadCharacter = character;
            }

            if (visualRootTransform != null && bobbleheadCharacter != null && bobbleheadCharacter.VisualRoot != null)
            {
                fallbackVisualChild = bobbleheadCharacter.VisualRoot;
            }
            else if (visualRootTransform != null && visualRootTransform != transform)
            {
                fallbackVisualChild = visualRootTransform;
            }

            InitializeIfNeeded(forceRecachePoses: true);
        }

        public void ConfigureRole(string npcId)
        {
            characterId = npcId ?? "";
            animationRole = ResolveRole(characterId);
            ComputeDeterministicPhaseOffsets(characterId);
        }

        /// <summary>
        /// Overrides the player target used for head orientation (useful for verification tests).
        /// </summary>
        public void SetPlayerLookTarget(Transform target)
        {
            _playerLookTarget = target;
            _hasExplicitPlayerLookOverride = (target != null);
        }

        /// <summary>
        /// Synchronizes authoritative social-awareness state from World Core (Increment 5.20.0).
        /// Cosmetic visual hold damping prevents rapid flicker on close boundary transitions.
        /// </summary>
        public void SyncSocialAwareness(
            string awarenessState,
            string targetEntityType,
            string targetEntityId,
            Vector2 directionXZ,
            float horizontalDistanceMeters,
            bool canOrientTowardTarget,
            bool retainsContextualActivity,
            Transform targetTransform = null)
        {
            string normalizedState = string.IsNullOrEmpty(awarenessState) ? "unaware" : awarenessState;
            string normalizedType = string.IsNullOrEmpty(targetEntityType) ? "none" : targetEntityType;
            string normalizedId = targetEntityId ?? "";

            bool isTargetSwapBetweenActivePeers =
                _hasAuthoritativeAwareness &&
                !string.Equals(normalizedState, "unaware", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_authoritativeAwarenessState, "unaware", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_awarenessTargetEntityId, normalizedId, StringComparison.Ordinal) &&
                !string.Equals(normalizedState, "player_attentive", StringComparison.OrdinalIgnoreCase) &&
                _targetHoldRemainingSeconds > 0f;

            if (isTargetSwapBetweenActivePeers)
            {
                return;
            }

            bool wasActive =
                _hasAuthoritativeAwareness &&
                !string.Equals(_authoritativeAwarenessState, "unaware", StringComparison.OrdinalIgnoreCase);
            bool nowUnaware = string.Equals(normalizedState, "unaware", StringComparison.OrdinalIgnoreCase);

            if (!string.Equals(_awarenessTargetEntityId, normalizedId, StringComparison.Ordinal) && !nowUnaware)
            {
                _targetHoldRemainingSeconds = socialTargetHoldDurationSeconds;
            }

            if (wasActive && nowUnaware && !_isWalkingLatched && !IsRestingOrClosedState())
            {
                _returningRemainingSeconds = returnToActivityDurationSeconds;
            }
            else if (!nowUnaware)
            {
                _returningRemainingSeconds = 0f;
            }

            _hasAuthoritativeAwareness = true;
            _authoritativeAwarenessState = normalizedState;
            _awarenessTargetEntityType = normalizedType;
            _awarenessTargetEntityId = normalizedId;
            _awarenessDirectionXZ = directionXZ;
            _awarenessDistanceMeters = horizontalDistanceMeters;
            _awarenessCanOrient = canOrientTowardTarget;
            _awarenessRetainsContextual = retainsContextualActivity;
            _socialTargetTransform = targetTransform;

            ReevaluateAwarenessState();
        }

        /// <summary>
        /// Clears authoritative awareness safely on backend failure or missing spatial state
        /// without fabricating social engagement.
        /// </summary>
        public void ClearSocialAwareness()
        {
            bool wasActive =
                _hasAuthoritativeAwareness &&
                !string.Equals(_authoritativeAwarenessState, "unaware", StringComparison.OrdinalIgnoreCase);

            _hasAuthoritativeAwareness = false;
            _authoritativeAwarenessState = "unaware";
            _awarenessTargetEntityType = "none";
            _awarenessTargetEntityId = "";
            _awarenessDirectionXZ = Vector2.zero;
            _awarenessDistanceMeters = -1f;
            _awarenessCanOrient = false;
            _awarenessRetainsContextual = false;
            _socialTargetTransform = null;
            _targetHoldRemainingSeconds = 0f;

            if (wasActive && !_isWalkingLatched && !IsRestingOrClosedState())
            {
                _returningRemainingSeconds = returnToActivityDurationSeconds;
            }
            else
            {
                _returningRemainingSeconds = 0f;
            }

            ReevaluateAwarenessState();
        }

        /// <summary>
        /// Synchronizes authoritative autonomous social interaction lifecycle state (Increment 5.21.0).
        /// </summary>
        public void SyncSocialInteraction(
            string sessionId,
            string phase,
            string conversationalRole,
            int turnIndex,
            string partnerNpcId,
            bool inActiveInteraction,
            Transform partnerTransform = null)
        {
            NpcVisualSocialInteractionPhase parsedPhase = ParseSocialInteractionPhase(phase, inActiveInteraction);
            string normalizedSession = sessionId ?? "";
            string normalizedPartner = partnerNpcId ?? "";
            string normalizedRole = string.IsNullOrEmpty(conversationalRole) ? "none" : conversationalRole.ToLowerInvariant();

            if (_currentSocialInteractionPhase != parsedPhase ||
                _socialInteractionTurnIndex != turnIndex ||
                !string.Equals(_socialInteractionSessionId, normalizedSession, StringComparison.Ordinal))
            {
                _socialConversationPhaseTimer = 0f;
            }

            _hasAuthoritativeSocialInteraction = true;
            _currentSocialInteractionPhase = parsedPhase;
            _socialInteractionSessionId = normalizedSession;
            _socialInteractionPartnerNpcId = normalizedPartner;
            _socialInteractionConversationalRole = normalizedRole;
            _socialInteractionTurnIndex = Mathf.Max(0, turnIndex);
            _socialInteractionPartnerTransform = inActiveInteraction ? partnerTransform : null;

            if (parsedPhase == NpcVisualSocialInteractionPhase.Completed || parsedPhase == NpcVisualSocialInteractionPhase.None)
            {
                _socialInteractionPartnerTransform = null;
                if (!IsRestingOrClosedState() && !_isWalkingLatched && !_hasAuthoritativeAwareness)
                {
                    _returningRemainingSeconds = returnToActivityDurationSeconds;
                }
            }
        }

        /// <summary>
        /// Clears authoritative social interaction presentation safely without mutating movement.
        /// </summary>
        public void ClearSocialInteraction()
        {
            bool wasActive = IsActiveSocialInteractionPhase();

            _hasAuthoritativeSocialInteraction = false;
            _currentSocialInteractionPhase = NpcVisualSocialInteractionPhase.None;
            _socialInteractionSessionId = "";
            _socialInteractionPartnerNpcId = "";
            _socialInteractionConversationalRole = "none";
            _socialInteractionTurnIndex = 0;
            _socialInteractionPartnerTransform = null;
            _socialConversationPhaseTimer = 0f;

            if (wasActive && !_isWalkingLatched && !IsRestingOrClosedState())
            {
                _returningRemainingSeconds = returnToActivityDurationSeconds;
            }
        }

        private bool IsActiveSocialInteractionPhase()
        {
            return _currentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Initiating ||
                   _currentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Engaged ||
                   _currentSocialInteractionPhase == NpcVisualSocialInteractionPhase.Closing;
        }

        private static NpcVisualSocialInteractionPhase ParseSocialInteractionPhase(string phase, bool inActiveInteraction)
        {
            if (string.IsNullOrEmpty(phase))
            {
                return NpcVisualSocialInteractionPhase.None;
            }
            switch (phase.Trim().ToLowerInvariant())
            {
                case "initiating":
                    return NpcVisualSocialInteractionPhase.Initiating;
                case "engaged":
                    return NpcVisualSocialInteractionPhase.Engaged;
                case "closing":
                    return NpcVisualSocialInteractionPhase.Closing;
                case "completed":
                    return NpcVisualSocialInteractionPhase.Completed;
                default:
                    return inActiveInteraction ? NpcVisualSocialInteractionPhase.Engaged : NpcVisualSocialInteractionPhase.None;
            }
        }

        private void ReevaluateAwarenessState()
        {
            if (!_hasAuthoritativeAwareness || IsRestingOrClosedState())
            {
                _currentAwarenessState = _returningRemainingSeconds > 0f && !IsRestingOrClosedState() && !_isWalkingLatched
                    ? NpcVisualAwarenessState.ReturningToActivity
                    : NpcVisualAwarenessState.Unaware;
                return;
            }

            switch (_authoritativeAwarenessState)
            {
                case "player_aware":
                    _currentAwarenessState = NpcVisualAwarenessState.PlayerAware;
                    break;
                case "player_attentive":
                    _currentAwarenessState = NpcVisualAwarenessState.PlayerAttentive;
                    break;
                case "npc_aware":
                    _currentAwarenessState = NpcVisualAwarenessState.NpcAware;
                    break;
                case "social_engaged":
                    _currentAwarenessState = NpcVisualAwarenessState.SocialEngaged;
                    break;
                case "unaware":
                default:
                    _currentAwarenessState = _returningRemainingSeconds > 0f && !_isWalkingLatched
                        ? NpcVisualAwarenessState.ReturningToActivity
                        : NpcVisualAwarenessState.Unaware;
                    break;
            }
        }

        /// <summary>
        /// Updates authoritative presentation inputs without allocating per frame.
        /// </summary>
        public void SyncPresentationInputs(
            string movementState,
            string interactionAvailability,
            string interactionMode,
            string activityState,
            string currentWaypointId,
            bool canInteract,
            float visualSpeedMps,
            string actorArchetype = null)
        {
            _movementState = string.IsNullOrEmpty(movementState) ? "stationary" : movementState;
            _interactionAvailability = string.IsNullOrEmpty(interactionAvailability) ? "available_normal" : interactionAvailability;
            _interactionMode = string.IsNullOrEmpty(interactionMode) ? "normal_service" : interactionMode;
            _activityState = string.IsNullOrEmpty(activityState) ? "available" : activityState;
            if (!string.IsNullOrEmpty(actorArchetype))
            {
                _actorArchetype = actorArchetype;
            }
            _currentWaypointId = currentWaypointId ?? "";
            _canInteract = canInteract;
            _reportedVisualSpeed = Mathf.Max(0f, visualSpeedMps);
        }

        /// <summary>
        /// Synchronizes authoritative Increment 5.22.0 autonomous locomotion and business activity states.
        /// </summary>
        public void SyncAutonomousBehavior(
            string behaviorProfileType,
            string autonomousBehaviorState,
            string controlMode,
            string patrolDirection,
            bool isSuspendedByPlayerInteraction,
            string passingEncounterPartnerId = null,
            string passingEncounterType = null)
        {
            _behaviorProfileType = behaviorProfileType ?? "";
            _autonomousBehaviorState = autonomousBehaviorState ?? "";
            _movementControlMode = controlMode ?? "";
            _patrolDirection = string.IsNullOrEmpty(patrolDirection) ? "none" : patrolDirection;
            _isSuspendedByPlayerInteraction = isSuspendedByPlayerInteraction;
            _passingEncounterPartnerNpcId = passingEncounterPartnerId ?? "";
            _passingEncounterType = passingEncounterType ?? "";
        }

        /// <summary>
        /// Triggers a brief polite welcoming gesture if stationary and responsive.
        /// Authoritative movement always takes priority and suppresses or cancels greetings.
        /// </summary>
        public void TriggerGreetingCue()
        {
            if (_isWalkingLatched || _currentBehaviorState == NpcVisualBehaviorState.Walking)
            {
                return;
            }

            if (IsRestingOrClosedState())
            {
                return;
            }

            _greetingRemainingSeconds = GreetingDurationSeconds;
            ReevaluateBehaviorState();
            ReevaluateAwarenessState();
        }

        /// <summary>
        /// Steps animation presentation deterministically by dt seconds.
        /// Exposed for automated verification suites alongside runtime LateUpdate.
        /// </summary>
        public void StepPresentation(float dt)
        {
            if (dt <= 0f) return;

            if (!_posesCached)
            {
                InitializeIfNeeded();
            }

            // Enforce root motion invariant if any Animator component exists on this hierarchy.
            if (optionalUnityAnimator != null && optionalUnityAnimator.applyRootMotion)
            {
                optionalUnityAnimator.applyRootMotion = false;
            }

            PullLatestPointFieldsIfDirty();

            UpdateLocomotionHysteresis(dt);

            if (_greetingRemainingSeconds > 0f)
            {
                if (_isWalkingLatched)
                {
                    _greetingRemainingSeconds = 0f;
                }
                else
                {
                    _greetingRemainingSeconds = Mathf.Max(0f, _greetingRemainingSeconds - dt);
                }
            }

            if (_targetHoldRemainingSeconds > 0f)
            {
                _targetHoldRemainingSeconds = Mathf.Max(0f, _targetHoldRemainingSeconds - dt);
            }

            if (_returningRemainingSeconds > 0f)
            {
                if (_isWalkingLatched || IsRestingOrClosedState())
                {
                    _returningRemainingSeconds = 0f;
                }
                else
                {
                    _returningRemainingSeconds = Mathf.Max(0f, _returningRemainingSeconds - dt);
                }
            }

            ReevaluateBehaviorState();
            ReevaluateAwarenessState();

            _breathPhase += dt * (1.75f * _breathRateScale);
            _ambientScanPhase += dt * (0.42f * _swayRateScale);
            _socialConversationPhaseTimer += dt;

            float targetSocialBlend = 0f;
            if (_hasAuthoritativeSocialInteraction &&
                !_isWalkingLatched &&
                !_isSuspendedByPlayerInteraction &&
                !string.Equals(_autonomousBehaviorState, "player_interaction_paused", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_autonomousBehaviorState, "customer_interaction", StringComparison.OrdinalIgnoreCase) &&
                !IsRestingOrClosedState() &&
                !IsContextualOrBusyState() &&
                _currentAwarenessState != NpcVisualAwarenessState.PlayerAttentive &&
                _greetingRemainingSeconds <= 0f)
            {
                switch (_currentSocialInteractionPhase)
                {
                    case NpcVisualSocialInteractionPhase.Initiating:
                        targetSocialBlend = 0.72f;
                        break;
                    case NpcVisualSocialInteractionPhase.Engaged:
                        targetSocialBlend = 1.0f;
                        break;
                    case NpcVisualSocialInteractionPhase.Closing:
                        targetSocialBlend = 0.35f;
                        break;
                    case NpcVisualSocialInteractionPhase.Completed:
                    case NpcVisualSocialInteractionPhase.None:
                    default:
                        targetSocialBlend = 0f;
                        break;
                }
            }
            _socialConversationBlend = Mathf.MoveTowards(_socialConversationBlend, targetSocialBlend, dt * 3.2f);

            float targetLocomotionWeight = _isWalkingLatched ? 1f : 0f;
            _locomotionBlend = Mathf.MoveTowards(_locomotionBlend, targetLocomotionWeight, dt * 14.0f);

            UpdateHeadTrackingAngles(dt);

            if (_hasArticulatedRig)
            {
                ApplyArticulatedPresentation(dt);
            }
            else
            {
                ApplyNonRiggedChildFallback(dt);
            }
        }

        private void LateUpdate()
        {
            StepPresentation(Time.deltaTime);
        }

        public void InitializeIfNeeded(bool forceRecachePoses = false)
        {
            if (interactionPoint == null)
            {
                interactionPoint = GetComponent<InteractionPoint>() ?? GetComponentInParent<InteractionPoint>();
            }

            if (string.IsNullOrEmpty(characterId) && interactionPoint != null && !string.IsNullOrEmpty(interactionPoint.characterId))
            {
                ConfigureRole(interactionPoint.characterId);
            }
            else if (string.IsNullOrEmpty(characterId))
            {
                ConfigureRole(gameObject.name);
            }

            if (bobbleheadCharacter == null)
            {
                bobbleheadCharacter = GetComponent<BobbleheadCharacter>() ?? GetComponentInChildren<BobbleheadCharacter>();
            }

            if (bobbleheadCharacter != null)
            {
                bobbleheadCharacter.EnsureCharacterBuilt();
            }

            if (legacyBobbleheadAnimator == null)
            {
                legacyBobbleheadAnimator = GetComponent<BobbleheadAnimator>() ?? GetComponentInChildren<BobbleheadAnimator>();
            }

            if (legacyBobbleheadAnimator != null)
            {
                legacyBobbleheadAnimator.SetExternalControllerActive(true);
            }

            if (optionalUnityAnimator == null)
            {
                optionalUnityAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            }

            if (optionalUnityAnimator != null)
            {
                // Root transform stays under authoritative World Core presentation control.
                optionalUnityAnimator.applyRootMotion = false;
            }

            CacheInitialRestPoses(forceRecachePoses);
            PullLatestPointFieldsIfDirty(force: true);
            ReevaluateBehaviorState();
            ReevaluateAwarenessState();
        }

        private void CacheInitialRestPoses(bool forceRecache)
        {
            if (_posesCached && !forceRecache) return;

            _hasArticulatedRig = false;

            if (bobbleheadCharacter != null && bobbleheadCharacter.VisualRoot != null)
            {
                if (bobbleheadCharacter.VisualRoot != transform)
                {
                    _initVisualRootRot = bobbleheadCharacter.VisualRoot.localRotation;
                }
                if (bobbleheadCharacter.Torso != null) _initTorsoRot = bobbleheadCharacter.Torso.localRotation;
                if (bobbleheadCharacter.HeadHinge != null) _initHeadHingeRot = bobbleheadCharacter.HeadHinge.localRotation;
                if (bobbleheadCharacter.LeftArmRoot != null) _initLeftArmRot = bobbleheadCharacter.LeftArmRoot.localRotation;
                if (bobbleheadCharacter.RightArmRoot != null) _initRightArmRot = bobbleheadCharacter.RightArmRoot.localRotation;
                if (bobbleheadCharacter.LeftForearm != null) _initLeftForearmRot = bobbleheadCharacter.LeftForearm.localRotation;
                if (bobbleheadCharacter.RightForearm != null) _initRightForearmRot = bobbleheadCharacter.RightForearm.localRotation;
                if (bobbleheadCharacter.LeftLegRoot != null) _initLeftLegRot = bobbleheadCharacter.LeftLegRoot.localRotation;
                if (bobbleheadCharacter.RightLegRoot != null) _initRightLegRot = bobbleheadCharacter.RightLegRoot.localRotation;
                if (bobbleheadCharacter.LeftLowerLeg != null) _initLeftKneeRot = bobbleheadCharacter.LeftLowerLeg.localRotation;
                if (bobbleheadCharacter.RightLowerLeg != null) _initRightKneeRot = bobbleheadCharacter.RightLowerLeg.localRotation;

                if (bobbleheadCharacter.Pelvis != null)
                {
                    _initPelvisPos = bobbleheadCharacter.Pelvis.localPosition;
                    _initPelvisRot = bobbleheadCharacter.Pelvis.localRotation;
                }

                _hasArticulatedRig = bobbleheadCharacter.Pelvis != null && bobbleheadCharacter.Torso != null;
            }

            if (fallbackVisualChild == null)
            {
                if (bobbleheadCharacter != null && bobbleheadCharacter.VisualRoot != null && bobbleheadCharacter.VisualRoot != transform)
                {
                    fallbackVisualChild = bobbleheadCharacter.VisualRoot;
                }
                else if (transform.childCount > 0)
                {
                    fallbackVisualChild = transform.GetChild(0);
                }
            }

            if (fallbackVisualChild != null && fallbackVisualChild != transform)
            {
                _initFallbackChildPos = fallbackVisualChild.localPosition;
                _initFallbackChildRot = fallbackVisualChild.localRotation;
            }

            _posesCached = true;
        }

        private void PullLatestPointFieldsIfDirty(bool force = false)
        {
            if (interactionPoint == null) return;

            bool dirty = force ||
                !ReferenceEquals(_lastMovementStateRef, interactionPoint.movementState) ||
                !ReferenceEquals(_lastAvailabilityRef, interactionPoint.interactionAvailability) ||
                !ReferenceEquals(_lastModeRef, interactionPoint.interactionMode) ||
                !ReferenceEquals(_lastActivityRef, interactionPoint.activityState) ||
                !ReferenceEquals(_lastActorArchetypeRef, interactionPoint.actorArchetype) ||
                !ReferenceEquals(_lastWaypointRef, interactionPoint.currentWaypointId) ||
                _lastCanInteractVal != interactionPoint.canInteract;

            if (!dirty) return;

            _lastMovementStateRef = interactionPoint.movementState;
            _lastAvailabilityRef = interactionPoint.interactionAvailability;
            _lastModeRef = interactionPoint.interactionMode;
            _lastActivityRef = interactionPoint.activityState;
            _lastActorArchetypeRef = interactionPoint.actorArchetype;
            _lastWaypointRef = interactionPoint.currentWaypointId;
            _lastCanInteractVal = interactionPoint.canInteract;

            _movementState = string.IsNullOrEmpty(interactionPoint.movementState) ? "stationary" : interactionPoint.movementState;
            _interactionAvailability = string.IsNullOrEmpty(interactionPoint.interactionAvailability) ? "available_normal" : interactionPoint.interactionAvailability;
            _interactionMode = string.IsNullOrEmpty(interactionPoint.interactionMode) ? "normal_service" : interactionPoint.interactionMode;
            _activityState = string.IsNullOrEmpty(interactionPoint.activityState) ? "available" : interactionPoint.activityState;
            if (!string.IsNullOrEmpty(interactionPoint.actorArchetype))
            {
                _actorArchetype = interactionPoint.actorArchetype;
            }
            _currentWaypointId = interactionPoint.currentWaypointId ?? "";
            _canInteract = interactionPoint.canInteract;
        }

        private void UpdateLocomotionHysteresis(float dt)
        {
            if (_isSuspendedByPlayerInteraction ||
                string.Equals(_autonomousBehaviorState, "player_interaction_paused", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_autonomousBehaviorState, "customer_interaction", StringComparison.OrdinalIgnoreCase))
            {
                _isWalkingLatched = false;
                _idleGraceElapsed = 0f;
                _smoothedVisualSpeed = 0f;
                return;
            }

            // Direct physical speed tracking (eliminates phase lag between body translation and leg swing)
            _smoothedVisualSpeed = Mathf.Lerp(_smoothedVisualSpeed, _reportedVisualSpeed, Mathf.Clamp01(dt * 18f));
            bool authMoving = string.Equals(_movementState, "moving", StringComparison.OrdinalIgnoreCase);

            bool wantsWalk = (authMoving && _reportedVisualSpeed >= walkStartSpeedThreshold) ||
                             _reportedVisualSpeed >= 0.12f ||
                             (authMoving && _locomotionBlend > 0.25f);

            if (wantsWalk)
            {
                _isWalkingLatched = true;
                _idleGraceElapsed = 0f;
            }
            else if (_isWalkingLatched)
            {
                if (!authMoving && _smoothedVisualSpeed <= walkStopSpeedThreshold)
                {
                    _idleGraceElapsed += dt;
                    if (_idleGraceElapsed >= idleExitGraceSeconds)
                    {
                        _isWalkingLatched = false;
                        _idleGraceElapsed = 0f;
                    }
                }
                else if (authMoving)
                {
                    _idleGraceElapsed = 0f;
                }
            }
        }

        private void ReevaluateBehaviorState()
        {
            // Strict state priority:
            // 1. Authoritative Locomotion
            if (_isWalkingLatched)
            {
                _currentBehaviorState = NpcVisualBehaviorState.Walking;
                return;
            }

            // 2. Contextual Activity / Demeanor (break, workshop calibration, workbench, inspection, busy)
            if (IsRestingOrClosedState())
            {
                _currentBehaviorState = NpcVisualBehaviorState.RestingOrOffDuty;
                return;
            }

            if (IsContextualOrBusyState())
            {
                _currentBehaviorState = NpcVisualBehaviorState.ContextualActivity;
                return;
            }

            // 3. Transient Greeting Gesture (when authorized & stationary)
            if (_greetingRemainingSeconds > 0f)
            {
                _currentBehaviorState = NpcVisualBehaviorState.GreetingCue;
                return;
            }

            // 4. Normal Service / Attentive Waiting
            if (_hasArticulatedRig)
            {
                _currentBehaviorState = NpcVisualBehaviorState.AttentiveService;
                return;
            }

            // 5. Fallback
            _currentBehaviorState = NpcVisualBehaviorState.NeutralFallback;
        }

        private bool IsCommunityPlayerProxyActor()
        {
            if (!string.IsNullOrEmpty(_actorArchetype))
            {
                return string.Equals(_actorArchetype, "community_player_proxy", StringComparison.OrdinalIgnoreCase);
            }
            return interactionPoint != null && interactionPoint.IsCommunityPlayerProxy();
        }

        private bool IsRestingOrClosedState()
        {
            // Community Player Proxies NEVER enter off-duty / closed posture states.
            if (IsCommunityPlayerProxyActor())
            {
                return false;
            }

            return string.Equals(_interactionAvailability, "off_duty", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_interactionAvailability, "unavailable", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_activityState, "off_duty", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_activityState, "unavailable", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_autonomousBehaviorState, "off_duty_inside", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_interactionMode, "off_duty_courtesy", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(_interactionMode, "closed", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsContextualOrBusyState()
        {
            if (string.Equals(_interactionAvailability, "available_contextual", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_interactionAvailability, "busy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_interactionMode, "break_or_dwell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_interactionMode, "busy_priority", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_activityState, "busy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_movementState, "arrived", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_movementState, "waiting", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void UpdateHeadTrackingAngles(float dt)
        {
            if (!HasHeadBone)
            {
                _currentHeadTrackYaw = 0f;
                _currentHeadTrackPitch = 0f;
                _currentTorsoSocialYaw = 0f;
                _currentBodySocialYaw = 0f;
                return;
            }

            float targetYaw = 0f;
            float targetPitch = 0f;
            float targetTorsoYaw = 0f;
            float targetBodyYaw = 0f;

            // Evaluate look direction in un-rotated character root + VisualRoot rest space
            // so rotating VisualRoot in child-local space never creates an orientation feedback loop.
            Transform referenceBasis = transform;
            Quaternion visualRestRot = (bobbleheadCharacter.VisualRoot != null && bobbleheadCharacter.VisualRoot != transform)
                ? _initVisualRootRot
                : Quaternion.identity;

            if (_hasAuthoritativeAwareness)
            {
                bool activeAwareness =
                    _awarenessCanOrient &&
                    _currentBehaviorState != NpcVisualBehaviorState.RestingOrOffDuty &&
                    (_currentAwarenessState == NpcVisualAwarenessState.PlayerAware ||
                     _currentAwarenessState == NpcVisualAwarenessState.PlayerAttentive ||
                     _currentAwarenessState == NpcVisualAwarenessState.NpcAware ||
                     _currentAwarenessState == NpcVisualAwarenessState.SocialEngaged);

                if (activeAwareness && referenceBasis != null)
                {
                    Vector3 worldLookDir = Vector3.zero;
                    float headWeight = 1.0f;
                    float torsoWeight = 0f;

                    switch (_currentAwarenessState)
                    {
                        case NpcVisualAwarenessState.PlayerAttentive:
                            headWeight = 1.0f;
                            torsoWeight = _awarenessRetainsContextual || _isWalkingLatched ? 0f : 0.35f;
                            break;
                        case NpcVisualAwarenessState.SocialEngaged:
                            headWeight = 0.88f;
                            torsoWeight = _awarenessRetainsContextual || _isWalkingLatched ? 0f : 0.38f;
                            break;
                        case NpcVisualAwarenessState.NpcAware:
                            headWeight = 0.72f;
                            torsoWeight = _awarenessRetainsContextual || _isWalkingLatched ? 0f : 0.16f;
                            break;
                        case NpcVisualAwarenessState.PlayerAware:
                        default:
                            headWeight = 0.65f;
                            torsoWeight = _awarenessRetainsContextual || _isWalkingLatched ? 0f : 0.12f;
                            break;
                    }

                    if (_isWalkingLatched || _awarenessRetainsContextual)
                    {
                        headWeight *= 0.6f;
                        torsoWeight = 0f;
                    }

                    Vector3 eyePos = bobbleheadCharacter.HeadHinge.position;
                    if (_awarenessTargetEntityType == "player")
                    {
                        Transform pTarget = ResolvePlayerLookTarget();
                        if (pTarget != null)
                        {
                            Vector3 playerHeadPos = pTarget.position;
                            if (pTarget.GetComponent<Camera>() == null)
                            {
                                playerHeadPos += Vector3.up * 1.45f;
                            }
                            Vector3 delta = playerHeadPos - eyePos;
                            if (delta.sqrMagnitude > 0.04f)
                            {
                                worldLookDir = delta.normalized;
                            }
                        }
                    }
                    else if (_awarenessTargetEntityType == "npc" && _socialTargetTransform != null)
                    {
                        Vector3 npcHeadPos = _socialTargetTransform.position + Vector3.up * 1.38f;
                        Vector3 delta = npcHeadPos - eyePos;
                        if (delta.sqrMagnitude > 0.04f)
                        {
                            worldLookDir = delta.normalized;
                        }
                    }

                    if (worldLookDir.sqrMagnitude < 0.001f && _awarenessDirectionXZ.sqrMagnitude > 0.001f)
                    {
                        worldLookDir = new Vector3(_awarenessDirectionXZ.x, 0f, _awarenessDirectionXZ.y).normalized;
                    }

                    if (worldLookDir.sqrMagnitude > 0.001f)
                    {
                        Vector3 localDir = Quaternion.Inverse(visualRestRot) * referenceBasis.InverseTransformDirection(worldLookDir);
                        float rawYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                        float horizMag = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
                        float rawPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.001f, horizMag)) * Mathf.Rad2Deg;
                        bool canTurnChildBody =
                            !_awarenessRetainsContextual &&
                            !_isWalkingLatched &&
                            bobbleheadCharacter.VisualRoot != null &&
                            bobbleheadCharacter.VisualRoot != transform;

                        if (_currentAwarenessState == NpcVisualAwarenessState.SocialEngaged)
                        {
                            // Stabilize +-180 straight-behind queue alignment so north partners turn cleanly inward
                            if (Mathf.Abs(localDir.x) < 0.03f && localDir.z < 0f)
                            {
                                rawYaw = transform.position.x >= 0f ? -179.5f : 179.5f;
                            }

                            if (canTurnChildBody)
                            {
                                if (Mathf.Abs(rawYaw) < 12f)
                                {
                                    // Straight-ahead colleague (e.g., South-to-North queue pair): shift into an open 3/4 conversational stance
                                    float stanceSign = transform.position.x >= 0f ? -1f : 1f;
                                    targetBodyYaw = stanceSign * 16f;
                                    targetTorsoYaw = stanceSign * 6f;
                                    targetYaw = Mathf.Clamp(rawYaw - targetBodyYaw - targetTorsoYaw, -maxHeadYawDegrees, maxHeadYawDegrees);
                                }
                                else
                                {
                                    // Side or behind colleague (including North-to-South 180° turnaround): split across VisualRoot + Torso + Head
                                    targetBodyYaw = Mathf.Clamp(rawYaw * 0.78f, -maxSocialBodyYawDegrees, maxSocialBodyYawDegrees);
                                    float postBodyYaw = rawYaw - targetBodyYaw;
                                    targetTorsoYaw = Mathf.Clamp(postBodyYaw * 0.48f, -maxSocialTorsoYawDegrees, maxSocialTorsoYawDegrees);
                                    float postTorsoYaw = postBodyYaw - targetTorsoYaw;
                                    targetYaw = Mathf.Clamp(postTorsoYaw, -maxHeadYawDegrees, maxHeadYawDegrees);
                                }
                                targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * headWeight;
                            }
                            else if (Mathf.Abs(rawYaw) <= 135f)
                            {
                                targetYaw = Mathf.Clamp(rawYaw, -maxHeadYawDegrees, maxHeadYawDegrees) * headWeight;
                                targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * headWeight;
                            }
                        }
                        else if (_currentAwarenessState == NpcVisualAwarenessState.NpcAware)
                        {
                            if (Mathf.Abs(rawYaw) <= 138f)
                            {
                                if (canTurnChildBody)
                                {
                                    targetBodyYaw = Mathf.Clamp(rawYaw * 0.28f, -maxNpcAwareBodyYawDegrees, maxNpcAwareBodyYawDegrees);
                                    float postBodyYaw = rawYaw - targetBodyYaw;
                                    targetTorsoYaw = Mathf.Clamp(postBodyYaw * torsoWeight, -maxSocialTorsoYawDegrees * 0.65f, maxSocialTorsoYawDegrees * 0.65f);
                                    targetYaw = Mathf.Clamp(postBodyYaw - targetTorsoYaw, -maxHeadYawDegrees, maxHeadYawDegrees) * headWeight;
                                }
                                else
                                {
                                    targetYaw = Mathf.Clamp(rawYaw, -maxHeadYawDegrees, maxHeadYawDegrees) * headWeight;
                                }
                                targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * headWeight;
                            }
                        }
                        else if (Mathf.Abs(rawYaw) <= rearCutoutYawDegrees)
                        {
                            targetYaw = Mathf.Clamp(rawYaw, -maxHeadYawDegrees, maxHeadYawDegrees) * headWeight;
                            targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * headWeight;
                            targetTorsoYaw = Mathf.Clamp(rawYaw * torsoWeight, -maxSocialTorsoYawDegrees, maxSocialTorsoYawDegrees);
                        }
                    }
                }
            }

            if (_hasAuthoritativeSocialInteraction &&
                IsActiveSocialInteractionPhase() &&
                _socialInteractionPartnerTransform != null &&
                !_isWalkingLatched &&
                _currentBehaviorState != NpcVisualBehaviorState.RestingOrOffDuty &&
                _currentBehaviorState != NpcVisualBehaviorState.ContextualActivity &&
                _currentAwarenessState != NpcVisualAwarenessState.PlayerAttentive &&
                referenceBasis != null &&
                Mathf.Abs(targetBodyYaw) < 0.01f &&
                Mathf.Abs(targetYaw) < 0.01f)
            {
                Vector3 eyePos = bobbleheadCharacter.HeadHinge.position;
                Vector3 partnerHeadPos = _socialInteractionPartnerTransform.position + Vector3.up * 1.38f;
                Vector3 delta = partnerHeadPos - eyePos;
                if (delta.sqrMagnitude > 0.04f)
                {
                    Vector3 worldLookDir = delta.normalized;
                    Vector3 localDir = Quaternion.Inverse(visualRestRot) * referenceBasis.InverseTransformDirection(worldLookDir);
                    float rawYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                    float horizMag = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
                    float rawPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.001f, horizMag)) * Mathf.Rad2Deg;

                    if (Mathf.Abs(localDir.x) < 0.03f && localDir.z < 0f)
                    {
                        rawYaw = transform.position.x >= 0f ? -179.5f : 179.5f;
                    }

                    bool canTurnChildBody =
                        bobbleheadCharacter.VisualRoot != null &&
                        bobbleheadCharacter.VisualRoot != transform;

                    if (canTurnChildBody)
                    {
                        if (Mathf.Abs(rawYaw) < 12f)
                        {
                            float stanceSign = transform.position.x >= 0f ? -1f : 1f;
                            targetBodyYaw = stanceSign * 16f;
                            targetTorsoYaw = stanceSign * 6f;
                            targetYaw = Mathf.Clamp(rawYaw - targetBodyYaw - targetTorsoYaw, -maxHeadYawDegrees, maxHeadYawDegrees);
                        }
                        else
                        {
                            targetBodyYaw = Mathf.Clamp(rawYaw * 0.78f, -maxSocialBodyYawDegrees, maxSocialBodyYawDegrees);
                            float postBodyYaw = rawYaw - targetBodyYaw;
                            targetTorsoYaw = Mathf.Clamp(postBodyYaw * 0.48f, -maxSocialTorsoYawDegrees, maxSocialTorsoYawDegrees);
                            targetYaw = Mathf.Clamp(postBodyYaw - targetTorsoYaw, -maxHeadYawDegrees, maxHeadYawDegrees);
                        }
                        targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * 0.9f;
                    }
                    else if (Mathf.Abs(rawYaw) <= 135f)
                    {
                        targetYaw = Mathf.Clamp(rawYaw, -maxHeadYawDegrees, maxHeadYawDegrees) * 0.9f;
                        targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees) * 0.9f;
                    }
                }
            }
            else if (!_hasAuthoritativeAwareness && _hasExplicitPlayerLookOverride)
            {
                float effectiveRange = ResolveHeadLookRangeMeters();
                if (effectiveRange > 0.1f && _playerLookTarget != null && referenceBasis != null)
                {
                    Vector3 eyePos = bobbleheadCharacter.HeadHinge.position;
                    Vector3 playerHeadPos = _playerLookTarget.position;
                    if (_playerLookTarget.GetComponent<Camera>() == null)
                    {
                        playerHeadPos += Vector3.up * 1.45f;
                    }

                    Vector3 toPlayer = playerHeadPos - eyePos;
                    float distSq = toPlayer.sqrMagnitude;
                    if (distSq > 0.09f && distSq <= effectiveRange * effectiveRange)
                    {
                        Vector3 localDir = referenceBasis.InverseTransformDirection(toPlayer.normalized);
                        float rawYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

                        if (Mathf.Abs(rawYaw) <= rearCutoutYawDegrees)
                        {
                            float horizMag = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
                            float rawPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.001f, horizMag)) * Mathf.Rad2Deg;

                            targetYaw = Mathf.Clamp(rawYaw, -maxHeadYawDegrees, maxHeadYawDegrees);
                            targetPitch = Mathf.Clamp(rawPitch, -maxHeadPitchUpDegrees, maxHeadPitchDownDegrees);
                        }
                    }
                }
            }

            float blendStep = Mathf.Clamp01(dt * headTrackBlendSpeed);
            _currentHeadTrackYaw = Mathf.Lerp(_currentHeadTrackYaw, targetYaw, blendStep);
            _currentHeadTrackPitch = Mathf.Lerp(_currentHeadTrackPitch, targetPitch, blendStep);
            _currentTorsoSocialYaw = Mathf.Lerp(_currentTorsoSocialYaw, targetTorsoYaw, blendStep);
            _currentBodySocialYaw = Mathf.Lerp(_currentBodySocialYaw, targetBodyYaw, Mathf.Clamp01(dt * bodySocialTurnSpeed));
        }

        private float ResolveHeadLookRangeMeters()
        {
            if (_currentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty)
            {
                return 0f;
            }

            if (_currentBehaviorState == NpcVisualBehaviorState.ContextualActivity &&
                string.Equals(_interactionAvailability, "busy", StringComparison.OrdinalIgnoreCase))
            {
                return busyLookRangeMeters;
            }

            if (_currentBehaviorState == NpcVisualBehaviorState.Walking)
            {
                return 4.5f;
            }

            return defaultLookRangeMeters;
        }

        private Transform ResolvePlayerLookTarget()
        {
            if (_playerLookTarget != null) return _playerLookTarget;

            if (!Application.isPlaying && Time.realtimeSinceStartup < _nextTargetLookupTime)
            {
                return null;
            }

            if (Application.isPlaying && Time.unscaledTime < _nextTargetLookupTime)
            {
                return null;
            }

            _nextTargetLookupTime = (Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup) + 2.0f;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _playerLookTarget = mainCam.transform;
                return _playerLookTarget;
            }

            var player = FindAnyObjectByType<FirstPersonPlayer>();
            if (player != null)
            {
                _playerLookTarget = player.transform;
            }

            return _playerLookTarget;
        }

        private void ApplyArticulatedPresentation(float dt)
        {
            // Advance gait cycle directly proportional to actual physical displacement per frame
            // (zero artificial speed floor when stationary so legs never churn in place)
            if (_locomotionBlend > 0.01f)
            {
                float gaitSpeed = _reportedVisualSpeed > 0.02f
                    ? Mathf.Min(_reportedVisualSpeed, 4.5f)
                    : (_isWalkingLatched ? Mathf.Clamp(_smoothedVisualSpeed, 0f, 4.5f) : 0f);
                _walkCyclePhase += dt * gaitSpeed * walkReferenceFrequency;
            }

            float breath = Mathf.Sin(_breathPhase + _phaseOffsetRad);
            float slowWeightShift = Mathf.Sin((_breathPhase * 0.34f) + (_phaseOffsetRad * 1.7f));
            float scanSweep = Mathf.Sin(_ambientScanPhase + _phaseOffsetRad) * 0.65f +
                              Mathf.Sin((_ambientScanPhase * 0.47f) + (_phaseOffsetRad * 2.1f)) * 0.35f;

            // Compute stationary/contextual target posture angles first
            ComputeIdlePostureTargets(
                breath,
                slowWeightShift,
                scanSweep,
                out Vector3 idlePelvisOffset,
                out Vector3 idlePelvisEuler,
                out Vector3 idleTorsoEuler,
                out Vector3 idleArmLEuler,
                out Vector3 idleArmREuler,
                out Vector3 idleForearmLEuler,
                out Vector3 idleForearmREuler,
                out Vector3 idleHeadEuler);

            // Compute walk cycle targets
            float sinW = Mathf.Sin(_walkCyclePhase);
            float cosW = Mathf.Cos(_walkCyclePhase);

            float legSwing = sinW * maxLegSwingDegrees * _locomotionBlend;
            float leftKnee = Mathf.Max(0f, cosW) * maxKneeBendDegrees * _locomotionBlend;
            float rightKnee = Mathf.Max(0f, -cosW) * maxKneeBendDegrees * _locomotionBlend;

            Vector3 walkPelvisOffset = new Vector3(0f, Mathf.Abs(sinW) * 0.014f, 0f);
            Vector3 walkTorsoEuler = new Vector3(2.2f, -sinW * 2.4f, 0f);
            Vector3 walkArmLEuler = new Vector3(-sinW * maxArmSwingDegrees, 0f, -6f);
            Vector3 walkArmREuler = new Vector3(sinW * maxArmSwingDegrees, 0f, 6f);
            Vector3 walkForearmLEuler = new Vector3(-8f - Mathf.Max(0f, -sinW) * 14f, 0f, 0f);
            Vector3 walkForearmREuler = new Vector3(-8f - Mathf.Max(0f, sinW) * 14f, 0f, 0f);
            Vector3 walkHeadEuler = new Vector3(1.2f, sinW * 1.8f, 0f);

            // Blend smoothly between stationary posture and locomotion gait using _locomotionBlend
            Vector3 targetPelvisOffset = Vector3.Lerp(idlePelvisOffset, walkPelvisOffset, _locomotionBlend);
            Vector3 targetPelvisEuler = Vector3.Lerp(idlePelvisEuler, Vector3.zero, _locomotionBlend);
            Vector3 targetTorsoEuler = Vector3.Lerp(idleTorsoEuler, walkTorsoEuler, _locomotionBlend);
            Vector3 targetArmLEuler = Vector3.Lerp(idleArmLEuler, walkArmLEuler, _locomotionBlend);
            Vector3 targetArmREuler = Vector3.Lerp(idleArmREuler, walkArmREuler, _locomotionBlend);
            Vector3 targetForearmLEuler = Vector3.Lerp(idleForearmLEuler, walkForearmLEuler, _locomotionBlend);
            Vector3 targetForearmREuler = Vector3.Lerp(idleForearmREuler, walkForearmREuler, _locomotionBlend);
            Vector3 targetHeadBaseEuler = Vector3.Lerp(idleHeadEuler, walkHeadEuler, _locomotionBlend);

            float idleReturnDamping = Mathf.Clamp01(dt * 10.0f);
            // Following BobbleheadAnimator.AnimateWalkCycle from the reference project:
            // apply active walking limb targets directly (no sluggish Slerp lag) once blended in,
            // while using smooth Slerp only when transitioning or returning to Idle.
            float limbDamping = (_isWalkingLatched && _locomotionBlend > 0.85f) ? 1.0f : idleReturnDamping;

            if (bobbleheadCharacter.Pelvis != null)
            {
                bobbleheadCharacter.Pelvis.localPosition = Vector3.Lerp(
                    bobbleheadCharacter.Pelvis.localPosition,
                    _initPelvisPos + targetPelvisOffset,
                    limbDamping);
                bobbleheadCharacter.Pelvis.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.Pelvis.localRotation,
                    _initPelvisRot * Quaternion.Euler(targetPelvisEuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.VisualRoot != null && bobbleheadCharacter.VisualRoot != transform)
            {
                bobbleheadCharacter.VisualRoot.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.VisualRoot.localRotation,
                    _initVisualRootRot * Quaternion.Euler(0f, _currentBodySocialYaw, 0f),
                    Mathf.Clamp01(dt * bodySocialTurnSpeed * 2.0f));
            }

            if (bobbleheadCharacter.Torso != null)
            {
                bobbleheadCharacter.Torso.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.Torso.localRotation,
                    _initTorsoRot * Quaternion.Euler(targetTorsoEuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.LeftArmRoot != null)
            {
                bobbleheadCharacter.LeftArmRoot.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.LeftArmRoot.localRotation,
                    _initLeftArmRot * Quaternion.Euler(targetArmLEuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.RightArmRoot != null)
            {
                bobbleheadCharacter.RightArmRoot.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.RightArmRoot.localRotation,
                    _initRightArmRot * Quaternion.Euler(targetArmREuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.LeftForearm != null)
            {
                bobbleheadCharacter.LeftForearm.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.LeftForearm.localRotation,
                    _initLeftForearmRot * Quaternion.Euler(targetForearmLEuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.RightForearm != null)
            {
                bobbleheadCharacter.RightForearm.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.RightForearm.localRotation,
                    _initRightForearmRot * Quaternion.Euler(targetForearmREuler),
                    limbDamping);
            }

            if (bobbleheadCharacter.LeftLegRoot != null)
            {
                bobbleheadCharacter.LeftLegRoot.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.LeftLegRoot.localRotation,
                    _initLeftLegRot * Quaternion.Euler(legSwing, 0f, 0f),
                    limbDamping);
            }

            if (bobbleheadCharacter.RightLegRoot != null)
            {
                bobbleheadCharacter.RightLegRoot.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.RightLegRoot.localRotation,
                    _initRightLegRot * Quaternion.Euler(-legSwing, 0f, 0f),
                    limbDamping);
            }

            if (bobbleheadCharacter.LeftLowerLeg != null)
            {
                bobbleheadCharacter.LeftLowerLeg.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.LeftLowerLeg.localRotation,
                    _initLeftKneeRot * Quaternion.Euler(leftKnee, 0f, 0f),
                    limbDamping);
            }

            if (bobbleheadCharacter.RightLowerLeg != null)
            {
                bobbleheadCharacter.RightLowerLeg.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.RightLowerLeg.localRotation,
                    _initRightKneeRot * Quaternion.Euler(rightKnee, 0f, 0f),
                    limbDamping);
            }

            if (bobbleheadCharacter.HeadHinge != null)
            {
                // Layer player/peer awareness tracking cleanly over subtle idle scan
                float playerLookStrength = Mathf.Clamp01((Mathf.Abs(_currentHeadTrackYaw) + Mathf.Abs(_currentHeadTrackPitch)) / 6f);
                if (_hasAuthoritativeAwareness && _awarenessCanOrient &&
                    (_currentAwarenessState == NpcVisualAwarenessState.PlayerAttentive ||
                     _currentAwarenessState == NpcVisualAwarenessState.SocialEngaged ||
                     _currentAwarenessState == NpcVisualAwarenessState.NpcAware))
                {
                    playerLookStrength = 1.0f;
                }
                float finalHeadPitch = Mathf.Lerp(targetHeadBaseEuler.x, _currentHeadTrackPitch, playerLookStrength);
                float finalHeadYaw = Mathf.Lerp(targetHeadBaseEuler.y, _currentHeadTrackYaw, playerLookStrength);

                bobbleheadCharacter.HeadHinge.localRotation = Quaternion.Slerp(
                    bobbleheadCharacter.HeadHinge.localRotation,
                    _initHeadHingeRot * Quaternion.Euler(finalHeadPitch, finalHeadYaw, targetHeadBaseEuler.z),
                    idleReturnDamping);
            }
        }

        private void ComputeIdlePostureTargets(
            float breath,
            float weightShift,
            float scanSweep,
            out Vector3 pelvisOffset,
            out Vector3 pelvisEuler,
            out Vector3 torsoEuler,
            out Vector3 armLEuler,
            out Vector3 armREuler,
            out Vector3 forearmLEuler,
            out Vector3 forearmREuler,
            out Vector3 headEuler)
        {
            // Gentle respiratory vertical settle and hip weight shift
            pelvisOffset = new Vector3(weightShift * 0.004f, breath * 0.0025f, 0f);
            pelvisEuler = new Vector3(0f, weightShift * 1.4f, weightShift * 0.8f);

            // Defaults: relaxed natural standing arms
            torsoEuler = new Vector3(breath * 0.7f, -weightShift * 0.9f, 0f);
            armLEuler = new Vector3(-2f + breath * 0.6f, 0f, -6f);
            armREuler = new Vector3(-2f + breath * 0.6f, 0f, 6f);
            forearmLEuler = new Vector3(-7f, 0f, 0f);
            forearmREuler = new Vector3(-7f, 0f, 0f);
            headEuler = new Vector3(breath * 0.45f, scanSweep * 4.5f, 0f);

            if (_currentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty)
            {
                // Off-duty or closed: slower relaxed drop, no rigid counter stance
                torsoEuler = new Vector3(1.2f + breath * 0.55f, weightShift * 1.1f, 0f);
                armLEuler = new Vector3(1.5f + breath * 0.4f, 0f, -4.5f);
                armREuler = new Vector3(1.5f + breath * 0.4f, 0f, 4.5f);
                forearmLEuler = new Vector3(-4f, 0f, 0f);
                forearmREuler = new Vector3(-4f, 0f, 0f);
                headEuler = new Vector3(3.2f + breath * 0.35f, scanSweep * 2.5f, 0f);
                return;
            }

            if (_currentBehaviorState == NpcVisualBehaviorState.ContextualActivity)
            {
                switch (animationRole)
                {
                    case NpcAnimationRole.PharmacyAttendant:
                        if (string.Equals(_interactionMode, "break_or_dwell", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_currentWaypointId, "wp_east_dining_benches", StringComparison.OrdinalIgnoreCase))
                        {
                            // Carlos meal break posture at corridor benches
                            torsoEuler = new Vector3(-1.5f + breath * 0.6f, weightShift * 1.5f, 0f);
                            armLEuler = new Vector3(-8f, 3f, -5f);
                            armREuler = new Vector3(-8f, -3f, 5f);
                            forearmLEuler = new Vector3(-16f, 0f, 0f);
                            forearmREuler = new Vector3(-16f, 0f, 0f);
                            headEuler = new Vector3(2.0f, scanSweep * 5.5f, 0f);
                        }
                        else
                        {
                            // Busy inventory / prescription task at counter
                            torsoEuler = new Vector3(3.5f + breath * 0.5f, weightShift * 1.2f, 0f);
                            armLEuler = new Vector3(-25f, 6f, -6f);
                            armREuler = new Vector3(-25f, -6f, 6f);
                            forearmLEuler = new Vector3(-34f + Mathf.Sin(_breathPhase * 1.4f) * 2.5f, 0f, 0f);
                            forearmREuler = new Vector3(-34f - Mathf.Sin(_breathPhase * 1.4f) * 2.5f, 0f, 0f);
                            headEuler = new Vector3(5.5f, scanSweep * 2.0f, 0f);
                        }
                        break;

                    case NpcAnimationRole.OpticsAttendant:
                        if (string.Equals(_currentWaypointId, "wp_optica_calibration_station", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_currentWaypointId, "wp_optica_workshop", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_interactionMode, "break_or_dwell", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_interactionAvailability, "busy", StringComparison.OrdinalIgnoreCase))
                        {
                            // Valeria calibration & frame alignment presentation
                            torsoEuler = new Vector3(4.5f + breath * 0.45f, 0f, 0f);
                            armLEuler = new Vector3(-26f, 8f, -5f);
                            armREuler = new Vector3(-26f, -8f, 5f);
                            forearmLEuler = new Vector3(-40f, 0f, 0f);
                            forearmREuler = new Vector3(-38f + Mathf.Sin(_breathPhase * 1.6f) * 2.0f, 0f, 0f);
                            headEuler = new Vector3(7.0f, scanSweep * 2.2f, 0f);
                        }
                        break;

                    case NpcAnimationRole.LocalArtisan:
                        if (string.Equals(_currentWaypointId, "wp_hector_bench", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_interactionAvailability, "available_contextual", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(_interactionAvailability, "busy", StringComparison.OrdinalIgnoreCase))
                        {
                            // Don Héctor stationary woodcraft workbench focus (restrained forearm motion, no fabricated tools)
                            float gentleCraftWave = Mathf.Sin(_breathPhase * 1.5f) * 3.0f;
                            torsoEuler = new Vector3(6.0f + breath * 0.45f, 0f, 0f);
                            armLEuler = new Vector3(-20f, 8f, -6f);
                            armREuler = new Vector3(-22f, -8f, 6f);
                            forearmLEuler = new Vector3(-28f, 0f, 0f);
                            forearmREuler = new Vector3(-32f + gentleCraftWave, 0f, 0f);
                            headEuler = new Vector3(8.5f, scanSweep * 2.5f, 0f);
                        }
                        break;

                    case NpcAnimationRole.BorderConcierge:
                        // Shift handover & bridge plaza log check
                        torsoEuler = new Vector3(1.5f + breath * 0.6f, weightShift * 1.2f, 0f);
                        armLEuler = new Vector3(-12f, 4f, -6f);
                        armREuler = new Vector3(-16f, -5f, 6f);
                        forearmLEuler = new Vector3(-22f, 0f, 0f);
                        forearmREuler = new Vector3(-26f, 0f, 0f);
                        headEuler = new Vector3(2.0f, scanSweep * 6.5f, 0f);
                        break;

                    case NpcAnimationRole.CitizenRepresentative:
                        if (string.Equals(_currentWaypointId, "wp_west_inspection", StringComparison.OrdinalIgnoreCase))
                        {
                            // Sidewalk inspection observation posture
                            torsoEuler = new Vector3(2.5f + breath * 0.5f, weightShift * 1.5f, 0f);
                            armLEuler = new Vector3(-6f, 2f, -6f);
                            armREuler = new Vector3(-12f, -3f, 6f);
                            forearmLEuler = new Vector3(-12f, 0f, 0f);
                            forearmREuler = new Vector3(-22f, 0f, 0f);
                            headEuler = new Vector3(5.0f, scanSweep * 7.5f, 0f);
                        }
                        else
                        {
                            // Attentive desk posture (busy reviewing reports)
                            torsoEuler = new Vector3(2.5f + breath * 0.5f, 0f, 0f);
                            armLEuler = new Vector3(-22f, 6f, -6f);
                            armREuler = new Vector3(-22f, -6f, 6f);
                            forearmLEuler = new Vector3(-32f, 0f, 0f);
                            forearmREuler = new Vector3(-32f, 0f, 0f);
                            headEuler = new Vector3(4.0f, scanSweep * 3.0f, 0f);
                        }
                        break;

                    case NpcAnimationRole.StudentCommuter:
                    case NpcAnimationRole.RetailCommuter:
                    default:
                        // Queue wait / canopy coordination stance with natural asymmetrical forearm rest
                        torsoEuler = new Vector3(breath * 0.65f, weightShift * 1.6f, 0f);
                        armLEuler = new Vector3(-4f, 0f, -6f);
                        armREuler = new Vector3(-9f, -4f, 6f);
                        forearmLEuler = new Vector3(-10f, 0f, 0f);
                        forearmREuler = new Vector3(-18f, 0f, 0f);
                        headEuler = new Vector3(0.5f + breath * 0.4f, scanSweep * 6.5f, 0f);
                        break;
                }
            }
            else
            {
                // Attentive normal service / station posture tuned per role
                switch (animationRole)
                {
                    case NpcAnimationRole.BorderConcierge:
                        // Upright welcoming counter posture & broad pedestrian awareness scan
                        torsoEuler = new Vector3(breath * 0.7f, -weightShift * 0.8f, 0f);
                        armLEuler = new Vector3(-20f, 5f, -6f);
                        armREuler = new Vector3(-20f, -5f, 6f);
                        forearmLEuler = new Vector3(-28f, 0f, 0f);
                        forearmREuler = new Vector3(-28f, 0f, 0f);
                        headEuler = new Vector3(-0.5f + breath * 0.45f, scanSweep * 8.0f, 0f);
                        break;

                    case NpcAnimationRole.PharmacyAttendant:
                    case NpcAnimationRole.OpticsAttendant:
                    case NpcAnimationRole.CitizenRepresentative:
                        // Counter service posture
                        torsoEuler = new Vector3(breath * 0.7f, 0f, 0f);
                        armLEuler = new Vector3(-22f, 6f, -6f);
                        armREuler = new Vector3(-22f, -6f, 6f);
                        forearmLEuler = new Vector3(-30f, 0f, 0f);
                        forearmREuler = new Vector3(-30f, 0f, 0f);
                        headEuler = new Vector3(-breath * 0.45f, scanSweep * 5.0f, 0f);
                        break;

                    case NpcAnimationRole.StudentCommuter:
                    case NpcAnimationRole.RetailCommuter:
                    case NpcAnimationRole.LocalArtisan:
                    default:
                        // Relaxed natural standing posture with subtle asymmetry
                        armLEuler = new Vector3(-2f + breath * 0.5f, 0f, -6.5f);
                        armREuler = new Vector3(-4f + breath * 0.5f, 0f, 6.5f);
                        forearmLEuler = new Vector3(-7f, 0f, 0f);
                        forearmREuler = new Vector3(-11f, 0f, 0f);
                        headEuler = new Vector3(breath * 0.45f, scanSweep * 6.0f, 0f);
                        break;
                }
            }

            // Layer gentle social-awareness upper-body orientation & autonomous social conversation lifecycle (never overrides walking or contextual priority tasks)
            torsoEuler.y += _currentTorsoSocialYaw;

            bool canExpressSocialBehavior =
                !_awarenessRetainsContextual &&
                _currentBehaviorState != NpcVisualBehaviorState.ContextualActivity &&
                _currentBehaviorState != NpcVisualBehaviorState.Walking &&
                _currentBehaviorState != NpcVisualBehaviorState.RestingOrOffDuty &&
                _currentBehaviorState != NpcVisualBehaviorState.GreetingCue &&
                _currentAwarenessState != NpcVisualAwarenessState.PlayerAttentive;

            if (canExpressSocialBehavior && _hasAuthoritativeSocialInteraction && _socialConversationBlend > 0.01f)
            {
                float w = _socialConversationBlend;
                switch (_currentSocialInteractionPhase)
                {
                    case NpcVisualSocialInteractionPhase.Initiating:
                    {
                        float greetingDip = Mathf.Clamp01(1.0f - (_socialConversationPhaseTimer * 0.45f));
                        float gentleAck = Mathf.Sin((_socialConversationPhaseTimer * 2.4f) + _phaseOffsetRad);
                        headEuler.x += (3.8f * (0.4f + 0.6f * greetingDip) + gentleAck * 1.4f) * w;
                        torsoEuler.x += 1.6f * w;
                        armLEuler += new Vector3(-4.0f, 3.0f, -2.0f) * w;
                        armREuler += new Vector3(-16.0f, -6.0f, 7.0f) * w;
                        forearmREuler += new Vector3(-20.0f, 0f, 0f) * w;
                        break;
                    }
                    case NpcVisualSocialInteractionPhase.Engaged:
                    {
                        if (string.Equals(_socialInteractionConversationalRole, "speaker", StringComparison.OrdinalIgnoreCase))
                        {
                            float speakWave = Mathf.Sin((_socialConversationPhaseTimer * 3.1f) + _phaseOffsetRad);
                            float emphasisWave = Mathf.Cos((_socialConversationPhaseTimer * 1.9f) + (_phaseOffsetRad * 1.3f));
                            torsoEuler.x += (1.8f + emphasisWave * 1.4f) * w;
                            torsoEuler.y += (speakWave * 2.8f) * w;
                            headEuler.x += (speakWave * 3.6f) * w;
                            headEuler.y += (emphasisWave * 4.2f) * w;
                            armLEuler += new Vector3(-7.0f + speakWave * 5.0f, 3.5f, -2.5f) * w;
                            armREuler += new Vector3(-13.0f - speakWave * 6.5f, -4.5f, 3.5f) * w;
                            forearmLEuler += new Vector3(-14.0f + emphasisWave * 6.0f, 0f, 0f) * w;
                            forearmREuler += new Vector3(-20.0f - speakWave * 7.5f, 0f, 0f) * w;
                        }
                        else if (string.Equals(_socialInteractionConversationalRole, "listener", StringComparison.OrdinalIgnoreCase))
                        {
                            float listenNod = Mathf.Sin((_socialConversationPhaseTimer * 1.85f) + (_phaseOffsetRad * 1.7f));
                            torsoEuler.x += 0.8f * w;
                            torsoEuler.z += 1.6f * Mathf.Sin((_breathPhase * 0.45f) + _phaseOffsetRad) * w;
                            headEuler.x += (2.2f + listenNod * 3.5f) * w;
                            headEuler.z += 2.0f * w;
                            armLEuler += new Vector3(-2.5f, 1.5f, -1.0f) * w;
                            armREuler += new Vector3(-3.5f, -1.5f, 1.0f) * w;
                            forearmLEuler += new Vector3(-5.0f, 0f, 0f) * w;
                            forearmREuler += new Vector3(-7.0f, 0f, 0f) * w;
                        }
                        else
                        {
                            float socialNodWave = Mathf.Sin((_breathPhase * 1.15f) + _phaseOffsetRad);
                            float socialPoiseWave = Mathf.Cos((_breathPhase * 0.68f) + (_phaseOffsetRad * 1.4f));
                            headEuler.x += socialNodWave * 2.8f * w;
                            torsoEuler.y += socialPoiseWave * 2.0f * w;
                            armLEuler += new Vector3(-3.5f + socialPoiseWave * 1.8f, 2.5f, 0f) * w;
                            armREuler += new Vector3(-6.5f + socialNodWave * 2.8f, -2.5f, 0f) * w;
                            forearmLEuler += new Vector3(-7.0f + socialNodWave * 2.2f, 0f, 0f) * w;
                            forearmREuler += new Vector3(-11.0f - socialNodWave * 2.8f, 0f, 0f) * w;
                        }
                        break;
                    }
                    case NpcVisualSocialInteractionPhase.Closing:
                    {
                        float fadeNod = Mathf.Sin((_socialConversationPhaseTimer * 1.6f) + _phaseOffsetRad);
                        headEuler.x += (2.4f + fadeNod * 1.4f) * w;
                        armLEuler += new Vector3(-2.0f, 1.0f, -1.0f) * w;
                        armREuler += new Vector3(-4.0f, -1.5f, 1.0f) * w;
                        forearmREuler += new Vector3(-6.0f, 0f, 0f) * w;
                        break;
                    }
                }
            }
            else if (canExpressSocialBehavior &&
                     !_hasAuthoritativeSocialInteraction &&
                     _currentAwarenessState == NpcVisualAwarenessState.SocialEngaged)
            {
                float socialNodWave = Mathf.Sin((_breathPhase * 1.15f) + _phaseOffsetRad);
                float socialPoiseWave = Mathf.Cos((_breathPhase * 0.68f) + (_phaseOffsetRad * 1.4f));
                headEuler.x += socialNodWave * 2.8f;
                torsoEuler.y += socialPoiseWave * 2.0f;
                armLEuler += new Vector3(-3.5f + socialPoiseWave * 1.8f, 2.5f, 0f);
                armREuler += new Vector3(-6.5f + socialNodWave * 2.8f, -2.5f, 0f);
                forearmLEuler += new Vector3(-7.0f + socialNodWave * 2.2f, 0f, 0f);
                forearmREuler += new Vector3(-11.0f - socialNodWave * 2.8f, 0f, 0f);
            }

            // Apply gentle transient welcoming gesture if authorized dialogue just opened
            if (_currentBehaviorState == NpcVisualBehaviorState.GreetingCue && _greetingRemainingSeconds > 0f)
            {
                float u = 1f - (_greetingRemainingSeconds / GreetingDurationSeconds);
                float bell = Mathf.Sin(u * Mathf.PI); // Smooth 0 -> 1 -> 0 envelope
                torsoEuler += new Vector3(2.2f * bell, 0f, 0f);
                armREuler = Vector3.Lerp(armREuler, new Vector3(-34f, -10f, 12f), bell);
                forearmREuler = Vector3.Lerp(forearmREuler, new Vector3(-42f, 0f, 0f), bell);
                headEuler += new Vector3(4.5f * bell, 0f, 0f);
            }
            else if (!_isWalkingLatched &&
                     string.Equals(_autonomousBehaviorState, "passing_encounter", StringComparison.OrdinalIgnoreCase) &&
                     _socialConversationBlend <= 0.05f)
            {
                // Brief passing encounter nod and friendly hand acknowledgment (Increment 5.22.0 Subsystem A2)
                float nod = Mathf.Sin((_breathPhase * 2.1f) + _phaseOffsetRad);
                torsoEuler += new Vector3(1.8f, 0f, 0f);
                armREuler += new Vector3(-16f + nod * 4.5f, -6f, 6f);
                forearmREuler += new Vector3(-22f - nod * 5.0f, 0f, 0f);
                headEuler += new Vector3(3.2f + nod * 2.5f, 0f, 0f);
            }
            else if (!_isWalkingLatched &&
                     (string.Equals(_autonomousBehaviorState, "observing", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(_autonomousBehaviorState, "idle_observation", StringComparison.OrdinalIgnoreCase)) &&
                     _socialConversationBlend <= 0.05f &&
                     !_isSuspendedByPlayerInteraction)
            {
                // Stationary environmental / display observation scan (Increment 5.22.0)
                headEuler.y += scanSweep * 4.2f;
                torsoEuler.y += scanSweep * 1.4f;
            }
        }

        private void ApplyNonRiggedChildFallback(float dt)
        {
            if (fallbackVisualChild == null || fallbackVisualChild == transform) return;

            float breath = Mathf.Sin(_breathPhase + _phaseOffsetRad);
            float amplitudeY = _isWalkingLatched ? 0.015f : 0.004f;
            float speedScale = _isWalkingLatched ? 5.0f : 1.0f;
            float verticalOffset = Mathf.Sin((_breathPhase * speedScale) + _phaseOffsetRad) * amplitudeY;
            float yawOffset = (_isWalkingLatched ? 0f : breath * 1.5f) + _currentBodySocialYaw;

            Vector3 targetPos = _initFallbackChildPos + new Vector3(0f, verticalOffset, 0f);
            Quaternion targetRot = _initFallbackChildRot * Quaternion.Euler(0f, yawOffset, 0f);

            float damping = Mathf.Clamp01(dt * 6.0f);
            fallbackVisualChild.localPosition = Vector3.Lerp(fallbackVisualChild.localPosition, targetPos, damping);
            fallbackVisualChild.localRotation = Quaternion.Slerp(fallbackVisualChild.localRotation, targetRot, damping);
        }

        private static NpcAnimationRole ResolveRole(string npcId)
        {
            switch (npcId)
            {
                case "elena-border-concierge":
                    return NpcAnimationRole.BorderConcierge;
                case "carlos-farmacia-attendant":
                    return NpcAnimationRole.PharmacyAttendant;
                case "valeria-optica-attendant":
                    return NpcAnimationRole.OpticsAttendant;
                case "mateo-student":
                    return NpcAnimationRole.StudentCommuter;
                case "sofia-cross-border-retail":
                    return NpcAnimationRole.RetailCommuter;
                case "hector-artisan":
                    return NpcAnimationRole.LocalArtisan;
                case "patricia-citizen-rep":
                    return NpcAnimationRole.CitizenRepresentative;
                default:
                    return NpcAnimationRole.GenericResident;
            }
        }

        private void ComputeDeterministicPhaseOffsets(string seedId)
        {
            uint hash = 2166136261u;
            if (!string.IsNullOrEmpty(seedId))
            {
                for (int i = 0; i < seedId.Length; i++)
                {
                    hash ^= seedId[i];
                    hash *= 16777619u;
                }
            }

            float normalized01 = (hash % 1000u) / 1000f;
            float secondary01 = ((hash / 1000u) % 1000u) / 1000f;

            _phaseOffsetRad = normalized01 * Mathf.PI * 2f;
            _breathRateScale = 0.90f + normalized01 * 0.20f;
            _swayRateScale = 0.88f + secondary01 * 0.24f;
        }
    }
}
