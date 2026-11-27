using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Attached to the interior hotel entrance (Backrooms_ExitDoor_ToStreet) inside
    /// BackroomsLevel1Scene.unity.
    ///
    /// Implementation 07.13 ("Check-in Without Conventional Check-out"):
    /// The interior door remains visible with its hotel-style framing and one-way
    /// Av. Benito Juárez observation glass, but operates in entry-only mode (isEntryOnly = true).
    /// Attempting to open it via [E] or click never initiates an exit transition and delivers
    /// only restrained bilingual environmental feedback ("La puerta no cede." / "The door won't budge.").
    /// The authorized in-world return route is the Room 111-B Threshold of Trust.
    /// </summary>
    public class BackroomsExitDoorTrigger : MonoBehaviour
    {
        public const string DefaultEntryOnlyPromptEs = "[E] Intentar abrir la puerta";
        public const string DefaultEntryOnlyPromptEn = "[E] Try the door";
        public const string DefaultEntryOnlyFeedbackEs = "La puerta no cede.";
        public const string DefaultEntryOnlyFeedbackEn = "The door won't budge.";
        public const float FeedbackDisplayDurationSeconds = 2.6f;

        [Header("Legacy Serialized Fields (Superseded When isEntryOnly Is Active)")]
        public string promptEs = "[E] Abrir puerta y regresar a Av. Benito Juárez";
        public string promptEn = "[E] Open door and return to Av. Benito Juárez";
        public float interactionRange = 2.4f;

        [Header("Implementation 07.13 — Entry-Only Rule")]
        public bool isEntryOnly = true;
        public string entryOnlyPromptEs = DefaultEntryOnlyPromptEs;
        public string entryOnlyPromptEn = DefaultEntryOnlyPromptEn;
        public string entryOnlyFeedbackEs = DefaultEntryOnlyFeedbackEs;
        public string entryOnlyFeedbackEn = DefaultEntryOnlyFeedbackEn;

        private float feedbackUntilUnscaledTime = -1f;
        private int entryOnlyAttemptCount = 0;

        public int EntryOnlyAttemptCount => entryOnlyAttemptCount;

        public bool IsShowingEntryOnlyFeedback(float? overrideNowUnscaledTime = null)
        {
            if (!isEntryOnly) return false;
            float now = overrideNowUnscaledTime ?? Time.unscaledTime;
            return feedbackUntilUnscaledTime >= 0f && now < feedbackUntilUnscaledTime;
        }

        public string GetPrompt(bool isEs, float? overrideNowUnscaledTime = null)
        {
            if (isEntryOnly)
            {
                if (IsShowingEntryOnlyFeedback(overrideNowUnscaledTime))
                {
                    return isEs ? entryOnlyFeedbackEs : entryOnlyFeedbackEn;
                }
                return isEs ? entryOnlyPromptEs : entryOnlyPromptEn;
            }
            return isEs ? promptEs : promptEn;
        }

        /// <summary>
        /// Records a player interaction attempt on the entry-only interior door and activates
        /// restrained bilingual feedback without invoking any scene or surface-return transition.
        /// </summary>
        public string TriggerEntryOnlyFeedback(bool isEs = true, float? overrideNowUnscaledTime = null)
        {
            float now = overrideNowUnscaledTime ?? Time.unscaledTime;
            entryOnlyAttemptCount++;
            feedbackUntilUnscaledTime = now + FeedbackDisplayDurationSeconds;
            return isEs ? entryOnlyFeedbackEs : entryOnlyFeedbackEn;
        }

        public void ResetEntryOnlyFeedbackForTest()
        {
            feedbackUntilUnscaledTime = -1f;
            entryOnlyAttemptCount = 0;
        }

        public bool IsWithinRange(Vector3 playerPos)
        {
            return Vector3.Distance(transform.position, playerPos) <= interactionRange;
        }
    }
}
