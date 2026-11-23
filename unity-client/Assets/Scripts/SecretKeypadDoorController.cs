using System;
using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Controls a double-leaf metal door equipped with a 6-digit electronic keypad lock.
    /// Designed so future increments can supply deterministic daily randomized 6-digit codes
    /// from the World Core backend via <see cref="SetAuthoritativeDailyCode"/>, while defaulting
    /// to "123456" for initial interior construction and player entry.
    /// </summary>
    public class SecretKeypadDoorController : MonoBehaviour
    {
        [Header("Door Identity & Access Code")]
        [Tooltip("Canonical identifier for this keypad-locked door.")]
        public string doorId = "door:building-a-south-secret-wedge";

        [Tooltip("Localized display title shown on the HUD prompt and keypad modal.")]
        public string doorTitleEs = "Puerta misteriosa";
        public string doorTitleEn = "Mysterious Door";

        [Tooltip("Active 6-digit access code (default '123456'; can be overridden daily by backend).")]
        public string activeAccessCode = "123456";

        [Tooltip("Exact number of digits required by the electronic keypad.")]
        public int requiredCodeLength = 6;

        [Header("Door Mechanics")]
        [Tooltip("South leaf hinge pivot (swings outward +Y toward street).")]
        public Transform southLeafPivot;

        [Tooltip("North leaf hinge pivot (swings outward -Y toward street).")]
        public Transform northLeafPivot;

        [Tooltip("Solid doorway blocker collider enabled while closed/locked and disabled once open.")]
        public BoxCollider closedDoorBlockerCollider;

        [Tooltip("Outward swing angle in degrees for each leaf (clears adjacent walls cleanly).")]
        public float openAngleDegrees = 88f;

        [Tooltip("Door open/close interpolation speed (units/sec in 0..1 progress).")]
        public float doorSwingSpeed = 2.8f;

        [Header("Interaction & Keypad Visual Feedback")]
        [Tooltip("World-space point near the keypad used for proximity detection.")]
        public Vector3 keypadInteractionAnchor = new Vector3(-11.94f, 1.18f, -12.86f);

        [Tooltip("Maximum distance in meters at which the player can interact with the door/keypad.")]
        public float interactionRadius = 2.65f;

        [Tooltip("Interior threshold X coordinate: player west of this X is inside the room and can always exit.")]
        public float interiorThresholdX = -12.04f;

        [Tooltip("Renderer for the 3D status LED bar on the electronic keypad.")]
        public Renderer statusLedRenderer;

        [Tooltip("Optional short-range point light reflecting the keypad lock status at night.")]
        public Light statusLedLight;

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool isUnlocked = false;
        [SerializeField] private bool isOpen = false;
        [SerializeField, Range(0f, 1f)] private float openProgress = 0f;

        private float errorFlashUntilTime = -1f;
        private Material runtimeLedMat;

        private static readonly Color LockedLedColor = new Color(0.95f, 0.12f, 0.12f);
        private static readonly Color UnlockedLedColor = new Color(0.14f, 0.95f, 0.36f);
        private static readonly Color ErrorLedColor = new Color(1.0f, 0.58f, 0.08f);

        public bool IsUnlocked => isUnlocked;
        public bool IsOpen => isOpen;
        public float OpenProgress => openProgress;
        public int RequiredCodeLength => Mathf.Clamp(requiredCodeLength, 4, 8);

        private void Start()
        {
            ApplyDoorVisuals();
            UpdateLedVisuals();
        }

        private void Update()
        {
            StepDoor(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (runtimeLedMat != null)
            {
                Destroy(runtimeLedMat);
                runtimeLedMat = null;
            }
        }

        public string GetLocalizedTitle(bool isEs)
        {
            return isEs
                ? (!string.IsNullOrEmpty(doorTitleEs) ? doorTitleEs : "Puerta misteriosa")
                : (!string.IsNullOrEmpty(doorTitleEn) ? doorTitleEn : "Mysterious Door");
        }

        public bool IsPlayerInsideRoom(Vector3 playerWorldPos)
        {
            return playerWorldPos.x < interiorThresholdX &&
                   playerWorldPos.z >= -13.55f &&
                   playerWorldPos.z <= -12.10f;
        }

        public bool IsWithinInteractionRange(Vector3 playerWorldPos)
        {
            Vector2 pXZ = new Vector2(playerWorldPos.x, playerWorldPos.z);
            Vector2 aXZ = new Vector2(keypadInteractionAnchor.x, keypadInteractionAnchor.z);
            return Vector2.Distance(pXZ, aXZ) <= interactionRadius;
        }

        /// <summary>
        /// Hook for future increments to supply a deterministic daily random 6-digit code
        /// discovered elsewhere in the world without modifying the door controller contract.
        /// </summary>
        public bool SetAuthoritativeDailyCode(string sixDigitCode)
        {
            if (string.IsNullOrEmpty(sixDigitCode) || sixDigitCode.Length != RequiredCodeLength)
            {
                return false;
            }
            for (int i = 0; i < sixDigitCode.Length; i++)
            {
                if (!char.IsDigit(sixDigitCode[i])) return false;
            }
            activeAccessCode = sixDigitCode;
            return true;
        }

        /// <summary>
        /// Validates an entered code string against <see cref="activeAccessCode"/>.
        /// Unlocks and opens the double door on exact match; triggers an error LED flash otherwise.
        /// </summary>
        public bool TrySubmitCode(string enteredCode)
        {
            string expected = string.IsNullOrEmpty(activeAccessCode) ? "123456" : activeAccessCode.Trim();
            string candidate = string.IsNullOrEmpty(enteredCode) ? "" : enteredCode.Trim();

            if (string.Equals(candidate, expected, StringComparison.Ordinal))
            {
                isUnlocked = true;
                isOpen = true;
                errorFlashUntilTime = -1f;
                UpdateLedVisuals();
                return true;
            }

            errorFlashUntilTime = Time.unscaledTime + 1.15f;
            UpdateLedVisuals();
            return false;
        }

        /// <summary>
        /// Opens or closes the door once unlocked.
        /// </summary>
        public void ToggleDoorOpen()
        {
            if (!isUnlocked) return;
            isOpen = !isOpen;
        }

        /// <summary>
        /// Safety exit guarantee: a player already inside the secret room can always open the door
        /// from the interior side without getting trapped.
        /// </summary>
        public void OpenFromInside()
        {
            isUnlocked = true;
            isOpen = true;
            UpdateLedVisuals();
        }

        /// <summary>
        /// Deterministic stepping method for EditMode/batchmode automated verification suites.
        /// </summary>
        public void StepDoorForVerification(float deltaTime)
        {
            StepDoor(deltaTime);
        }

        private void StepDoor(float dt)
        {
            float target = isOpen ? 1f : 0f;
            if (!Mathf.Approximately(openProgress, target))
            {
                openProgress = Mathf.MoveTowards(openProgress, target, Mathf.Max(0.01f, doorSwingSpeed) * Mathf.Max(0f, dt));
                ApplyDoorVisuals();
            }
            UpdateLedVisuals();
        }

        private void ApplyDoorVisuals()
        {
            // Smoothstep easing for natural heavy steel door swing
            float eased = openProgress * openProgress * (3f - 2f * openProgress);
            float angle = openAngleDegrees * eased;

            // South leaf extends +Z from south hinge -> positive Y rotation swings outward (+X)
            if (southLeafPivot != null)
            {
                southLeafPivot.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            // North leaf extends -Z from north hinge -> negative Y rotation swings outward (+X)
            if (northLeafPivot != null)
            {
                northLeafPivot.localRotation = Quaternion.Euler(0f, -angle, 0f);
            }

            if (closedDoorBlockerCollider != null)
            {
                closedDoorBlockerCollider.enabled = openProgress < 0.60f;
            }
        }

        private void UpdateLedVisuals()
        {
            bool flashingError = !isUnlocked && Time.unscaledTime < errorFlashUntilTime;
            Color targetColor = isUnlocked
                ? UnlockedLedColor
                : (flashingError ? ErrorLedColor : LockedLedColor);

            if (statusLedRenderer != null)
            {
                Material mat = Application.isPlaying ? EnsureRuntimeLedMaterial() : statusLedRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", targetColor);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", targetColor * 2.4f);
                    }
                }
            }

            if (statusLedLight != null)
            {
                statusLedLight.color = targetColor;
                statusLedLight.intensity = isUnlocked ? 0.045f : (flashingError ? 0.06f : 0.03f);
            }
        }

        private Material EnsureRuntimeLedMaterial()
        {
            if (runtimeLedMat == null && statusLedRenderer != null && statusLedRenderer.sharedMaterial != null)
            {
                runtimeLedMat = new Material(statusLedRenderer.sharedMaterial);
                statusLedRenderer.sharedMaterial = runtimeLedMat;
            }
            return runtimeLedMat;
        }
    }
}
