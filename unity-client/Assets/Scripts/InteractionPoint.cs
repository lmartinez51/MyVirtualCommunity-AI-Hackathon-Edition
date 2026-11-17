using System;
using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    public enum InteractionCategory
    {
        StorefrontAttendant,
        BorderConcierge,
        SyntheticWitness,
        Informational,
        CitizenService
    }

    /// <summary>
    /// Represents an in-world interactive landmark or NPC along the corridor.
    /// Supports both informational modal cards and live conversational dialogues.
    /// </summary>
    public class InteractionPoint : MonoBehaviour
    {
        [Header("Interaction Classification")]
        [Tooltip("The category of interaction for this character or landmark.")]
        public InteractionCategory category = InteractionCategory.Informational;

        [Header("Character Identity (for NPCs)")]
        [Tooltip("Canonical ID matching server contracts (e.g. carlos-farmacia-attendant, mateo-student, elena-border-concierge).")]
        public string characterId = "";

        [Tooltip("Display name of the character.")]
        public string characterName = "";

        [Tooltip("Display role of the character.")]
        public string characterRole = "";

        [Tooltip("Visual symbol or avatar indicator.")]
        public string avatar = "[NPC]";

        [Header("Authoritative Actor Archetype & Witness Classification (Increment 5.20.0.2)")]
        [Tooltip("Authoritative backend archetype: business_attendant | public_servant | community_player_proxy.")]
        public string actorArchetype = "";

        [Tooltip("Whether this actor is governed by an employment/service schedule.")]
        public bool hasEmploymentSchedule = true;

        [Tooltip("Whether this actor is permitted to enter off_duty state.")]
        public bool canEnterOffDuty = true;

        [Tooltip("Whether this actor holds canonical community witness classification.")]
        public bool isWitness = false;

        [Tooltip("Witness classification category (e.g. none | community_queue_witness).")]
        public string witnessClassification = "none";

        [Tooltip("Canonical witness identifier if isWitness is true.")]
        public string witnessId = "";

        public bool IsCommunityPlayerProxy()
        {
            if (!string.IsNullOrEmpty(actorArchetype))
            {
                return string.Equals(actorArchetype, "community_player_proxy", StringComparison.OrdinalIgnoreCase);
            }
            return category == InteractionCategory.SyntheticWitness;
        }

        public bool CanDisplayOffDutyState()
        {
            return !IsCommunityPlayerProxy() && canEnterOffDuty;
        }

        [Header("Authoritative World Core Activity (Simulated Presence)")]
        [Tooltip("Current activity state: available | busy | off_duty | unavailable.")]
        public string activityState = "available";

        [Tooltip("Localized title of current activity.")]
        public string activityTitleEn = "";
        public string activityTitleEs = "";

        [Tooltip("Localized description of current activity.")]
        public string activityDescriptionEn = "";
        public string activityDescriptionEs = "";

        [Tooltip("Whether the NPC can currently be engaged in dialogue.")]
        public bool canInteract = true;

        [Tooltip("Special notice or demeanor prompt when busy or off duty.")]
        public string interactionNoticeEn = "";
        public string interactionNoticeEs = "";

        [Header("Authoritative Interaction Context (Increment 5.17.0)")]
        [Tooltip("Interaction availability: available_normal | available_in_transit | available_contextual | busy | off_duty | unavailable.")]
        public string interactionAvailability = "available_normal";

        [Tooltip("Interaction mode: normal_service | in_transit | break_or_dwell | busy_priority | off_duty_courtesy | closed.")]
        public string interactionMode = "normal_service";

        [Tooltip("Whether normal station/counter service is active.")]
        public bool isAvailableForNormalService = true;

        [Tooltip("Authoritative decision reason explaining NPC demeanor.")]
        public string decisionReasonEn = "";
        public string decisionReasonEs = "";

        [Tooltip("Authoritative guidance for player interaction demeanor.")]
        public string dialogueGuidanceEn = "";
        public string dialogueGuidanceEs = "";

        [Tooltip("Contextual greeting reflecting current activity/location.")]
        public string contextualGreetingEn = "";
        public string contextualGreetingEs = "";

        public string GetContextualGreeting(string language)
        {
            if (string.Equals(language, "es", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrEmpty(contextualGreetingEs) ? contextualGreetingEs : contextualGreetingEn;
            return !string.IsNullOrEmpty(contextualGreetingEn) ? contextualGreetingEn : contextualGreetingEs;
        }

        public string GetDecisionReason(string language)
        {
            if (string.Equals(language, "es", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrEmpty(decisionReasonEs) ? decisionReasonEs : decisionReasonEn;
            return !string.IsNullOrEmpty(decisionReasonEn) ? decisionReasonEn : decisionReasonEs;
        }

        public string GetInteractionAvailabilityBadge(string language)
        {
            bool isEs = string.Equals(language, "es", StringComparison.OrdinalIgnoreCase);
            bool isProxy = IsCommunityPlayerProxy();
            switch (interactionAvailability)
            {
                case "available_in_transit":
                    return isEs ? "[En tránsito]" : "[In Transit]";
                case "available_contextual":
                    return isEs ? "[Descanso / Actividad]" : "[Break / Activity]";
                case "busy":
                    return isEs ? "[Ocupado]" : "[Busy]";
                case "off_duty":
                    if (isProxy)
                    {
                        return isEs ? "[Disponible]" : "[Available]";
                    }
                    return isEs ? "[Fuera de turno]" : "[Off Duty]";
                case "unavailable":
                    if (isProxy)
                    {
                        return isEs ? "[Disponible]" : "[Available]";
                    }
                    return isEs ? "[Cerrado hoy]" : "[Closed Today]";
                case "available_normal":
                default:
                    if (isProxy)
                    {
                        return isEs ? "[Disponible]" : "[Available]";
                    }
                    return isEs ? "[En servicio]" : "[On Duty]";
            }
        }

        [Header("Authoritative Waypoint Movement (Increment 5.16.0)")]
        [Tooltip("Movement state: stationary | moving | arrived | waiting.")]
        public string movementState = "stationary";

        [Tooltip("Current or most recently occupied waypoint ID.")]
        public string currentWaypointId = "";

        [Tooltip("Target waypoint ID when moving or arriving.")]
        public string destinationWaypointId = "";

        [Tooltip("Active route ID being traversed.")]
        public string activeRouteId = "";

        [Tooltip("Movement progress along active route (0.0 to 1.0).")]
        [Range(0f, 1f)]
        public float progression = 0f;

        [Header("Authoritative Social Awareness (Increment 5.20.0)")]
        [Tooltip("Awareness state: unaware | player_aware | player_attentive | npc_aware | social_engaged.")]
        public string socialAwarenessState = "unaware";

        [Tooltip("Detected target entity type: none | player | npc.")]
        public string socialTargetEntityType = "none";

        [Tooltip("Detected target entity identifier.")]
        public string socialTargetEntityId = "";

        [Tooltip("Mutual 1-to-1 social partner NPC ID when social_engaged.")]
        public string socialPairedPartnerNpcId = "";

        [Tooltip("Horizontal distance in meters to current awareness target (-1 if none).")]
        public float socialTargetHorizontalDistanceMeters = -1f;

        [Tooltip("Authoritative unit planar direction (XZ) toward target.")]
        public Vector2 socialTargetDirectionXZ = Vector2.zero;

        [Tooltip("Whether World Core allows visual orientation toward the target.")]
        public bool socialCanOrientTowardTarget = false;

        [Tooltip("Whether the NPC retains a focused contextual activity alongside awareness.")]
        public bool socialRetainsContextualActivity = false;

        [Tooltip("Whether the NPC is currently eligible for mutual social pairing.")]
        public bool socialEligible = false;

        [Tooltip("True when authoritative social awareness snapshot data is available.")]
        public bool socialAwarenessAvailable = false;

        [Header("Autonomous Social Interaction Lifecycle (Increment 5.21.0)")]
        [Tooltip("Active or recently completed session identifier.")]
        public string socialInteractionSessionId = "";

        [Tooltip("Lifecycle phase: none | initiating | engaged | closing | completed.")]
        public string socialInteractionPhase = "";

        [Tooltip("Contextual interaction type classification.")]
        public string socialInteractionType = "";

        [Tooltip("Canonical NPC identifier of the interaction partner.")]
        public string socialInteractionPartnerNpcId = "";

        [Tooltip("Conversational turn role: speaker | listener | mutual | none.")]
        public string socialInteractionConversationalRole = "none";

        [Tooltip("Zero-based deterministic turn counter during engaged phase.")]
        public int socialInteractionTurnIndex = 0;

        public string socialInteractionStartedAt = "";
        public string socialInteractionExpectedEndAt = "";
        public string socialInteractionCooldownUntil = "";
        public bool socialInteractionInCooldown = false;
        public bool socialInteractionActive = false;
        public bool socialInteractionAvailable = false;

        [Header("Autonomous Locomotion & Bounded Business Activity (Increment 5.22.0)")]
        [Tooltip("Behavioral profile type: bridge_patrol | business_local_autonomy | fixed_anchor_scheduled.")]
        public string behaviorProfileType = "";

        [Tooltip("Authoritative behavioral state: patrolling | observing | paused | turning_around | passing_encounter | player_interaction_paused | resuming | working_stationary | local_roaming | idle_observation | customer_awareness | returning_to_station | customer_interaction | off_duty | fixed_station.")]
        public string autonomousBehaviorState = "";

        [Tooltip("Movement control mode: autonomous_bridge_patrol | autonomous_business_local | scheduled_routine_transition | player_interaction_suspended | fixed_station_anchor.")]
        public string movementControlMode = "";

        [Tooltip("Bridge patrol direction: northbound | southbound | none.")]
        public string patrolDirection = "none";

        [Tooltip("Deterministic patrol or local-autonomy cycle index.")]
        public int patrolCycleIndex = 0;

        [Tooltip("True when an authorized player-initiated conversation has suspended autonomous movement.")]
        public bool isSuspendedByPlayerInteraction = false;

        [Tooltip("Partner NPC ID during a brief passing encounter.")]
        public string passingEncounterPartnerNpcId = "";

        [Tooltip("Passing encounter type: passing_greeting | passing_exchange.")]
        public string passingEncounterType = "";

        [Tooltip("Assigned spatial domain identifier.")]
        public string businessDomainId = "";

        [Tooltip("True when the authoritative position lies within the configured spatial domain.")]
        public bool withinAuthorizedDomain = true;

        public string GetMovementDisplayLabel(string language)
        {
            bool isEs = string.Equals(language, "es", StringComparison.OrdinalIgnoreCase);
            switch (movementState)
            {
                case "moving":
                    return isEs ? "[En movimiento]" : "[Moving]";
                case "arrived":
                    return isEs ? "[Llegó a destino]" : "[Arrived]";
                case "waiting":
                    return isEs ? "[En espera]" : "[Waiting]";
                default:
                    return "";
            }
        }

        public string GetActivityTitle(string language)
        {
            if (string.Equals(language, "es", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrEmpty(activityTitleEs) ? activityTitleEs : activityTitleEn;
            return !string.IsNullOrEmpty(activityTitleEn) ? activityTitleEn : activityTitleEs;
        }

        public string GetInteractionNotice(string language)
        {
            if (string.Equals(language, "es", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrEmpty(interactionNoticeEs) ? interactionNoticeEs : interactionNoticeEn;
            return !string.IsNullOrEmpty(interactionNoticeEn) ? interactionNoticeEn : interactionNoticeEs;
        }

        [Header("Suggested Prompts")]
        [Tooltip("Quick suggested questions in English.")]
        public string[] suggestedPromptsEn = new string[0];

        [Tooltip("Quick suggested questions in Spanish.")]
        public string[] suggestedPromptsEs = new string[0];

        [Header("Landmark Metadata")]
        [Tooltip("Unique canonical identifier matching spatial-corridor.ts.")]
        public string landmarkId = "landmark:unknown";

        [Tooltip("Display title of the landmark or entity.")]
        public string title = "Landmark";

        [Tooltip("Subtitle describing character role or business type.")]
        public string subtitle = "Interactive Landmark";

        [Tooltip("Street address or location details.")]
        public string address = "";

        [TextArea(3, 5)]
        [Tooltip("Contextual overview of this location.")]
        public string description = "";

        [TextArea(3, 6)]
        [Tooltip("Explicit disclosure of the backend feature connected to this location.")]
        public string backendFeatureNotice = "";

        [Header("Interaction Settings")]
        [Tooltip("Radius in meters within which player can trigger interaction.")]
        public float interactionRadius = 3.5f;

        [Tooltip("Prompt shown on HUD when player is within range.")]
        public string promptText = "Press [E] to talk / interact";

        [Header("Visual Cue")]
        [SerializeField]
        private GameObject highlightIndicator;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightIndicator != null)
            {
                highlightIndicator.SetActive(highlighted);
            }
        }
    }
}
