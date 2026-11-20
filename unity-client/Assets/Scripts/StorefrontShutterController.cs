using System;
using System.Collections;
using UnityEngine;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Animates a commercial roll-up metal shutter (cortina metálica replegable) cleanly
    /// driven by authoritative backend business availability:
    ///
    /// - For 24h storefronts (e.g., Cambios Santa Fe, Casa de Cambio Sur), set alwaysOpen = true
    ///   so the shutter remains rolled up inside its overhead drum housing.
    /// - For scheduled businesses without an NPC InteractionPoint (e.g., Farmacia Juárez, Dental México),
    ///   set linkedBusinessId (e.g., "pharmacy:farmacia-juarez") to poll GET /api/businesses/:id/status
    ///   directly from the authoritative backend and automatically lower/raise the shutter.
    /// - For NPC-backed scheduled businesses (e.g., Óptica Fashion), binds automatically to
    ///   InteractionPoint_OpticaFashion.interactionAvailability synced from World Core.
    /// </summary>
    public class StorefrontShutterController : MonoBehaviour
    {
        [Header("Authoritative State Binding")]
        [Tooltip("If true, shutter stays permanently rolled up (e.g., 24-hour establishments).")]
        public bool alwaysOpen = false;

        [Tooltip("Canonical backend BusinessId (e.g., 'pharmacy:farmacia-juarez' or 'dental:dental-mexico') polled via GET /api/businesses/:id/status.")]
        public string linkedBusinessId = "";

        [Tooltip("Seconds between authoritative backend schedule status polls when linkedBusinessId is set.")]
        public float businessStatusPollIntervalSeconds = 15.0f;

        [Tooltip("Optional InteractionPoint GameObject name synced with World Core open/closed status (used when linkedBusinessId is empty).")]
        public string linkedInteractionPointName = "InteractionPoint_OpticaFashion";

        [Header("Shutter Geometry")]
        public Transform shutterCurtain;
        public GameObject openSignVisual;
        public float fullDropHeight = 2.48f;
        public float rolledMinHeight = 0.08f;
        public float topAnchorLocalY = 2.66f;
        public float transitionSpeed = 1.4f;

        [Header("Runtime Inspection")]
        [SerializeField, Range(0f, 1f)] private float closedProgress = 0f;
        [SerializeField] private bool targetClosed = false;
        [SerializeField] private string latestBackendStatus = "unknown";

        private InteractionPoint linkedPoint;
        private ApiClient apiClient;
        private Coroutine pollCoroutine;
        private float searchCooldown = 0f;

        public bool IsClosed => targetClosed;
        public float ClosedProgress => closedProgress;
        public string LatestBackendStatus => latestBackendStatus;

        private void Awake()
        {
            if (alwaysOpen)
            {
                targetClosed = false;
                closedProgress = 0f;
            }
            ApplyCurtainVisual(closedProgress);
        }

        private void OnEnable()
        {
            if (Application.isPlaying && !alwaysOpen && !string.IsNullOrEmpty(linkedBusinessId))
            {
                if (pollCoroutine != null) StopCoroutine(pollCoroutine);
                pollCoroutine = StartCoroutine(PollBusinessStatusLoop());
            }
        }

        private void OnDisable()
        {
            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }
        }

        private IEnumerator PollBusinessStatusLoop()
        {
            // Small initial delay so ApiClient finishes configuring BaseUrl from launch args
            yield return new WaitForSeconds(0.35f);

            while (true)
            {
                if (apiClient == null)
                {
                    apiClient = FindAnyObjectByType<ApiClient>();
                }

                if (apiClient != null && !string.IsNullOrEmpty(linkedBusinessId))
                {
                    bool done = false;
                    apiClient.GetBusinessStatus(linkedBusinessId,
                        onSuccess: resp =>
                        {
                            if (resp != null && resp.success && resp.status != null)
                            {
                                ApplyAuthoritativeBusinessStatus(resp.status);
                            }
                            done = true;
                        },
                        onError: err =>
                        {
                            Debug.LogWarning($"[StorefrontShutter] Failed to poll status for '{linkedBusinessId}': {err}");
                            done = true;
                        });

                    float waited = 0f;
                    while (!done && waited < 8f)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                }

                yield return new WaitForSeconds(Mathf.Max(5f, businessStatusPollIntervalSeconds));
            }
        }

        public void ApplyAuthoritativeBusinessStatus(BusinessOperationalStatusDto status)
        {
            if (alwaysOpen)
            {
                targetClosed = false;
                latestBackendStatus = "OPEN_24H";
                return;
            }

            if (status == null) return;
            latestBackendStatus = !string.IsNullOrEmpty(status.status) ? status.status : (status.isOpen ? "OPEN" : "CLOSED");
            targetClosed = !status.isOpen;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (alwaysOpen)
            {
                targetClosed = false;
            }
            else if (string.IsNullOrEmpty(linkedBusinessId))
            {
                if (linkedPoint == null && !string.IsNullOrEmpty(linkedInteractionPointName))
                {
                    searchCooldown -= Time.deltaTime;
                    if (searchCooldown <= 0f)
                    {
                        searchCooldown = 2.0f;
                        var go = GameObject.Find(linkedInteractionPointName);
                        if (go != null) linkedPoint = go.GetComponent<InteractionPoint>();
                    }
                }

                if (linkedPoint != null)
                {
                    targetClosed = EvaluateClosedFromAvailability(linkedPoint.interactionAvailability);
                }
            }

            float goal = targetClosed ? 1f : 0f;
            if (!Mathf.Approximately(closedProgress, goal))
            {
                closedProgress = Mathf.MoveTowards(closedProgress, goal, transitionSpeed * Time.deltaTime);
                ApplyCurtainVisual(closedProgress);
            }
        }

        public static bool EvaluateClosedFromAvailability(string availability)
        {
            if (string.IsNullOrWhiteSpace(availability)) return false;
            return string.Equals(availability, "off_duty", StringComparison.OrdinalIgnoreCase)
                || string.Equals(availability, "unavailable", StringComparison.OrdinalIgnoreCase)
                || string.Equals(availability, "closed", StringComparison.OrdinalIgnoreCase);
        }

        public void SetClosedImmediate(bool closed)
        {
            targetClosed = alwaysOpen ? false : closed;
            closedProgress = targetClosed ? 1f : 0f;
            ApplyCurtainVisual(closedProgress);
        }

        private void ApplyCurtainVisual(float t)
        {
            if (shutterCurtain != null)
            {
                float h = Mathf.Lerp(rolledMinHeight, fullDropHeight, Mathf.Clamp01(t));
                Vector3 scale = shutterCurtain.localScale;
                scale.y = h;
                shutterCurtain.localScale = scale;

                Vector3 pos = shutterCurtain.localPosition;
                pos.y = topAnchorLocalY - h * 0.5f;
                shutterCurtain.localPosition = pos;

                // Keep curtain crisp and hidden inside housing when completely rolled up
                shutterCurtain.gameObject.SetActive(t > 0.02f);
            }

            if (openSignVisual != null)
            {
                openSignVisual.SetActive(t < 0.35f);
            }
        }
    }
}
