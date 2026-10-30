using System;
using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    public enum InteractionCategory
    {
        StorefrontAttendant,
        BorderConcierge,
        SyntheticWitness,
        Informational
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

        [Tooltip("Emoji avatar or visual symbol.")]
        public string avatar = "👤";

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
