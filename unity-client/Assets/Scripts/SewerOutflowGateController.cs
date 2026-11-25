using UnityEngine;
using MyVirtualCommunity.Client.Progression;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Interactive heavy rusted iron storm gate at the Rio Bravo outflow headwall.
    /// Swings smoothly open/closed with [E] and calls Physics.SyncTransforms() so
    /// the player can walk right out onto the south bank of the Rio Bravo under the
    /// Puente Internacional Paso del Norte.
    /// </summary>
    public class SewerOutflowGateController : MonoBehaviour
    {
        public Transform gateHingePivot;
        public float closedYaw = 0f;
        public float openYaw = -108f;
        public float swingSpeedDegPerSec = 115f;
        public float interactionRadius = 3.2f;

        private bool isOpen = false;
        private float currentYaw = 0f;
        private AudioSource audioSource;
        private AudioClip creakClip;
        private PlayerProgressionService progressionServiceOverride;

        public bool IsOpen => isOpen;

        public void SetProgressionServiceForTesting(PlayerProgressionService service)
        {
            progressionServiceOverride = service;
        }

        private PlayerProgressionService GetActiveProgressionService()
        {
            if (progressionServiceOverride != null) return progressionServiceOverride;
            return Application.isPlaying ? PlayerProgressionService.Shared : null;
        }

        public void ApplyProgressionState(PlayerProgressionService progression)
        {
            if (progression == null) return;
            isOpen = progression.IsSewerOutflowGateOpened;
            currentYaw = isOpen ? openYaw : closedYaw;
            if (gateHingePivot != null)
            {
                gateHingePivot.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
                Physics.SyncTransforms();
            }
        }

        public void ResetRuntimeStateToDefault()
        {
            isOpen = false;
            currentYaw = closedYaw;
            if (gateHingePivot != null)
            {
                gateHingePivot.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
                Physics.SyncTransforms();
            }
        }

        private void Awake()
        {
            currentYaw = closedYaw;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.85f;
            audioSource.volume = 0.65f;
            creakClip = CreateGateCreakClip();
        }

        private void Start()
        {
            ApplyProgressionState(GetActiveProgressionService());
        }

        public bool IsPlayerInRange(Vector3 playerPos)
        {
            return Vector3.Distance(playerPos, transform.position) <= interactionRadius;
        }

        public string GetPromptText(bool isSpanish)
        {
            if (isOpen)
                return isSpanish ? "[E] Cerrar reja del Río Bravo" : "[E] Close Rio Grande outflow gate";
            return isSpanish ? "[E] Abrir reja hacia el Río Bravo" : "[E] Open storm gate to Rio Grande bank";
        }

        public void ToggleGate()
        {
            isOpen = !isOpen;
            if (creakClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(creakClip, 0.7f);
            }
            GetActiveProgressionService()?.RecordSewerOutflowGateState(isOpen);
        }

        private void Update()
        {
            if (gateHingePivot == null) return;
            float target = isOpen ? openYaw : closedYaw;
            if (Mathf.Abs(currentYaw - target) > 0.05f)
            {
                currentYaw = Mathf.MoveTowards(currentYaw, target, swingSpeedDegPerSec * Time.deltaTime);
                gateHingePivot.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
                Physics.SyncTransforms();
            }
        }

        private static AudioClip CreateGateCreakClip()
        {
            int sr = 22050;
            float dur = 0.75f;
            int samples = Mathf.RoundToInt(sr * dur);
            float[] data = new float[samples];
            float p1 = 0f, p2 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float u = t / dur;
                float env = Mathf.Sin(u * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(t * 32f));
                float f1 = Mathf.Lerp(210f, 340f, u) + Mathf.Sin(t * 19f) * 18f;
                p1 += 2f * Mathf.PI * f1 / sr;
                p2 += 2f * Mathf.PI * (f1 * 1.49f) / sr;
                data[i] = (Mathf.Sin(p1) * 0.45f + Mathf.Sin(p2) * 0.35f) * env * 0.45f;
            }
            AudioClip clip = AudioClip.Create("OutflowGateCreak", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
