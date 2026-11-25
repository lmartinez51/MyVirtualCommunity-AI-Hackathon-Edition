using System.Collections;
using UnityEngine;
using MyVirtualCommunity.Client.Progression;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Interactive cast-iron manhole access linking the surface streets of Ciudad Juarez
    /// to the underground storm-drain collector network via a smooth lid slide + ladder climb.
    /// Also carries the Phase-2-ready flag (triggerBackroomsNoClipTwist) for the Av. Benito Juarez
    /// x Calle Azucenas manhole Easter Egg.
    /// </summary>
    public class SewerManholeInteractable : MonoBehaviour
    {
        [Header("Identity")]
        public string manholeId = "manhole_juarez_azucenas";
        public string labelEs = "Alcantarilla (Av. Juárez × Azucenas)";
        public string labelEn = "Storm Drain Manhole (Av. Juarez x Azucenas)";

        [Header("Surface & Underground Anchors")]
        public Transform lidPivot;
        public Vector3 surfaceExitPosition;
        public float surfaceExitYaw = 0f;
        public Vector3 undergroundLandingPosition;
        public float undergroundLandingYaw = 0f;
        public float interactionRadius = 2.65f;

        [Header("Phase 2 Hook (Backrooms Kane Pixels Easter Egg)")]
        [Tooltip("Left false in Phase 1 so all manholes access the underground storm-drain network. Phase 2 flips this on the Av. Juarez x Azucenas manhole.")]
        public bool triggerBackroomsNoClipTwist = false;

        private bool isTransitioning = false;
        private Vector3 lidClosedLocalPos;
        private bool hasSavedLidPos = false;
        private AudioSource audioSource;
        private AudioClip ironScrapeClip;

        private void Awake()
        {
            if (lidPivot != null)
            {
                lidClosedLocalPos = lidPivot.localPosition;
                hasSavedLidPos = true;
            }
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.7f;
            audioSource.volume = 0.55f;
            ironScrapeClip = CreateIronScrapeClip();
        }

        public bool IsPlayerNearSurface(Vector3 playerPos)
        {
            if (playerPos.y < -1.2f) return false;
            Vector2 d = new Vector2(playerPos.x - transform.position.x, playerPos.z - transform.position.z);
            return d.magnitude <= interactionRadius;
        }

        public bool IsPlayerNearUndergroundLadder(Vector3 playerPos)
        {
            if (playerPos.y >= -1.2f) return false;
            Vector2 d = new Vector2(playerPos.x - undergroundLandingPosition.x, playerPos.z - undergroundLandingPosition.z);
            return d.magnitude <= interactionRadius + 0.35f && Mathf.Abs(playerPos.y - undergroundLandingPosition.y) <= 2.4f;
        }

        public string GetPromptText(Vector3 playerPos, bool isSpanish)
        {
            if (isTransitioning) return "";
            if (IsPlayerNearSurface(playerPos))
            {
                return isSpanish
                    ? $"[E] Abrir y bajar por {labelEs}"
                    : $"[E] Open & descend {labelEn}";
            }
            if (IsPlayerNearUndergroundLadder(playerPos))
            {
                return isSpanish
                    ? $"[E] Subir a la calle ({labelEs})"
                    : $"[E] Climb to street ({labelEn})";
            }
            return "";
        }

        public void TriggerClimb(FirstPersonPlayer player, bool goingDown)
        {
            if (isTransitioning || player == null) return;
            StartCoroutine(ClimbRoutine(player, goingDown));
        }

        private IEnumerator ClimbRoutine(FirstPersonPlayer player, bool goingDown)
        {
            isTransitioning = true;

            if (ironScrapeClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(ironScrapeClip, 0.6f);
            }

            // Slide heavy cast-iron lid open smoothly (identical on every manhole — zero hint!)
            if (lidPivot != null && hasSavedLidPos)
            {
                Vector3 openPos = lidClosedLocalPos + new Vector3(0.55f, 0.03f, 0.15f);
                float t = 0f;
                while (t < 0.32f)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / 0.32f);
                    lidPivot.localPosition = Vector3.Lerp(lidClosedLocalPos, openPos, k);
                    yield return null;
                }
            }

            // Phase 2 Silent Easter Egg: Descending Manhole #1 triggers an unexpected Kane Pixels
            // ceiling no-clip drop into a random disorienting spot inside BackroomsLevel1Scene!
            if (goingDown && triggerBackroomsNoClipTwist)
            {
                var backroomsCtrl = FindFirstObjectByType<SecretBackroomsTransitionController>();
                if (backroomsCtrl != null)
                {
                    if (lidPivot != null && hasSavedLidPos)
                    {
                        lidPivot.localPosition = lidClosedLocalPos;
                    }
                    backroomsCtrl.TriggerManholeNoClipDropToBackrooms(player.transform);
                    isTransitioning = false;
                    yield break;
                }
            }

            // Brief fade via UndergroundSewerAtmosphereController
            var atmos = UndergroundSewerAtmosphereController.ActiveInstance;
            if (atmos != null) yield return atmos.FadeScreen(0f, 1f, 0.24f);

            // Teleport player cleanly to destination landing with CharacterController disabled
            CharacterController cc = player.GetComponent<CharacterController>();
            Vector3 targetPos = goingDown ? undergroundLandingPosition : surfaceExitPosition;
            float targetYaw = goingDown ? undergroundLandingYaw : surfaceExitYaw;

            if (cc != null) cc.enabled = false;
            player.transform.position = targetPos;
            player.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;

            if (goingDown && Application.isPlaying)
            {
                PlayerProgressionService.Shared.RecordUndergroundEntered();
            }

            // Close lid back flush with street so traffic/pedestrians never see an open pit
            if (lidPivot != null && hasSavedLidPos)
            {
                lidPivot.localPosition = lidClosedLocalPos;
            }

            if (atmos != null)
            {
                atmos.SnapAtmosphereForCurrentPosition(targetPos);
                yield return atmos.FadeScreen(1f, 0f, 0.28f);
            }

            isTransitioning = false;
        }

        private static AudioClip CreateIronScrapeClip()
        {
            int sr = 22050;
            float dur = 0.42f;
            int samples = Mathf.RoundToInt(sr * dur);
            float[] data = new float[samples];
            System.Random rng = new System.Random(9011);
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float freq = Mathf.Lerp(145f, 92f, t / dur);
                phase += 2f * Mathf.PI * freq / sr;
                float metal = Mathf.Sin(phase) * 0.40f + Mathf.Sin(phase * 2.7f) * 0.22f;
                float grit = ((float)rng.NextDouble() * 2f - 1f) * 0.38f;
                data[i] = (metal + grit) * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create("ManholeIronScrape", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
