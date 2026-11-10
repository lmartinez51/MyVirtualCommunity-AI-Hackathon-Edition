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
            switch (interactionAvailability)
            {
                case "available_in_transit":
                    return isEs ? "[En tránsito]" : "[In Transit]";
                case "available_contextual":
                    return isEs ? "[Descanso / Actividad]" : "[Break / Activity]";
                case "busy":
                    return isEs ? "[Ocupado]" : "[Busy]";
                case "off_duty":
                    return isEs ? "[Fuera de turno]" : "[Off Duty]";
                case "unavailable":
                    return isEs ? "[Cerrado hoy]" : "[Closed Today]";
                case "available_normal":
                default:
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
