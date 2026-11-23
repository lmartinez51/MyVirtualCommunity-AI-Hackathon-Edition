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

        [Tooltip("Increment 5.23.2: Prevent shutter closure from trapping FirstPersonPlayer inside a walkable interior (e.g., Óptica Fashion).")]
        public bool protectPlayerInteriorExit = true;

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
        [SerializeField] private string latestBusinessLifecycleState = "";
        [SerializeField] private bool heldOpenForPlayerExit = false;

        private InteractionPoint linkedPoint;
        private ApiClient apiClient;
        private Coroutine pollCoroutine;
        private float searchCooldown = 0f;
        private FirstPersonPlayer cachedPlayer;
        private Collider cachedCurtainCollider;
        private bool hasAppliedInitialAuthoritativeState = false;

        public bool IsClosed => targetClosed;
        public float ClosedProgress => closedProgress;
        public string LatestBackendStatus => latestBackendStatus;
        public string LatestBusinessLifecycleState => latestBusinessLifecycleState;
        public bool HeldOpenForPlayerExit => heldOpenForPlayerExit;

        private void Awake()
        {
            if (shutterCurtain != null)
            {
                cachedCurtainCollider = shutterCurtain.GetComponent<Collider>();
            }
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
                latestBusinessLifecycleState = "business_open";
                return;
            }

            if (status == null) return;
            latestBackendStatus = !string.IsNullOrEmpty(status.status) ? status.status : (status.isOpen ? "OPEN" : "CLOSED");
            if (!string.IsNullOrEmpty(status.businessLifecycleState))
            {
                latestBusinessLifecycleState = status.businessLifecycleState;
                targetClosed = status.shutterClosedPermitted;
            }
            else
            {
                targetClosed = !status.isOpen;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            StepShutter(Time.deltaTime, playerTransformOverride: null);
        }

        /// <summary>
        /// Deterministically evaluates shutter target and advances curtain animation by dt seconds.
        /// Exposed for Increment 5.23.2 batchmode verification suites alongside runtime Update().
        /// </summary>
        public void StepShutterForVerification(float dt, Transform playerTransformOverride = null)
        {
            StepShutter(dt, playerTransformOverride);
        }

        private void StepShutter(float dt, Transform playerTransformOverride)
        {
            if (dt < 0f) dt = 0f;
            heldOpenForPlayerExit = false;

            if (alwaysOpen)
            {
                targetClosed = false;
                latestBusinessLifecycleState = "business_open";
            }
            else if (string.IsNullOrEmpty(linkedBusinessId))
            {
                if (linkedPoint == null && !string.IsNullOrEmpty(linkedInteractionPointName))
                {
                    searchCooldown -= dt;
                    if (searchCooldown <= 0f)
                    {
                        searchCooldown = 2.0f;
                        var go = GameObject.Find(linkedInteractionPointName);
                        if (go != null) linkedPoint = go.GetComponent<InteractionPoint>();
                    }
                }

                if (linkedPoint != null)
                {
                    if (linkedPoint.hasAuthoritativeLifecycleShutter)
                    {
                        // Single authoritative World Core decision: shutter never closes while Valeria is outside/closing_pending
                        latestBusinessLifecycleState = linkedPoint.businessLifecycleState ?? "";
                        latestBackendStatus = latestBusinessLifecycleState;
                        targetClosed = linkedPoint.shutterClosedPermitted;

                        // Snap closed immediately on first authoritative Play-mode sync when store is closed
                        if (Application.isPlaying && !hasAppliedInitialAuthoritativeState)
                        {
                            hasAppliedInitialAuthoritativeState = true;
                            if (targetClosed && !IsPlayerInsideWalkableStoreInterior(playerTransformOverride))
                            {
                                SetClosedImmediate(true);
                                return;
                            }
                        }
                    }
                    else
                    {
                        targetClosed = EvaluateClosedFromAvailability(linkedPoint.interactionAvailability);
                    }
                }
            }

            // Player non-trap safety guard: only hold/raise shutter if the player is genuinely DEEP inside
            // Óptica Fashion's interior behind the storefront glass plane (X < -12.33m). A player walking
            // up to or pushing against the closed exterior shutter (X >= -11.88m) NEVER opens it!
            bool playerInside = IsPlayerInsideWalkableStoreInterior(playerTransformOverride);
            if (playerInside)
            {
                heldOpenForPlayerExit = true;
                targetClosed = false;
            }

            if (cachedCurtainCollider == null && shutterCurtain != null)
            {
                cachedCurtainCollider = shutterCurtain.GetComponent<Collider>();
            }
            if (cachedCurtainCollider != null)
            {
                // Disable solid shutter collider ONLY while player is genuinely inside so exit is guaranteed;
                // keep solid collider active whenever shutter is lowered (even 2%+) so outside players cannot enter.
                cachedCurtainCollider.enabled = !playerInside && closedProgress > 0.02f;
            }

            float goal = targetClosed ? 1f : 0f;
            if (!Mathf.Approximately(closedProgress, goal))
            {
                closedProgress = Mathf.MoveTowards(closedProgress, goal, transitionSpeed * dt);
                ApplyCurtainVisual(closedProgress);
            }
        }

        private bool IsPlayerInsideWalkableStoreInterior(Transform playerTransformOverride)
        {
            if (!protectPlayerInteriorExit) return false;
            // Applies specifically to walkable storefront interiors on the West block (Óptica Fashion, wallX = -11.88, glass plane X = -12.02, interior X = -19.2..-12.02)
            bool isOpticaShutter =
                string.Equals(linkedInteractionPointName, "InteractionPoint_OpticaFashion", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameObject.name, "Optica_RollUpShutter_Dynamic", StringComparison.OrdinalIgnoreCase);
            if (!isOpticaShutter) return false;

            Transform playerT = playerTransformOverride;
            if (playerT == null)
            {
                if (cachedPlayer == null)
                {
                    cachedPlayer = FindAnyObjectByType<FirstPersonPlayer>();
                }
                if (cachedPlayer != null)
                {
                    playerT = cachedPlayer.transform;
                }
            }
            if (playerT == null) return false;

            Vector3 p = playerT.position;
            float shutterX = transform.position.x; // -11.88m
            float shutterZ = transform.position.z; // +0.625m
            // Player must be strictly behind the storefront glass plane (-12.02m) with margin: X < -12.33m
            return p.x < shutterX - 0.45f &&
                   p.x > shutterX - 8.20f &&
                   Mathf.Abs(p.z - shutterZ) <= 2.40f;
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
            if (cachedCurtainCollider == null && shutterCurtain != null)
            {
                cachedCurtainCollider = shutterCurtain.GetComponent<Collider>();
            }
            if (cachedCurtainCollider != null)
            {
                cachedCurtainCollider.enabled = targetClosed && !heldOpenForPlayerExit;
            }
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

            Physics.SyncTransforms();
        }
    }
}
