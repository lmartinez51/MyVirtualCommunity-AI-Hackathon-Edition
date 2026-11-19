using System;
using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Animates a commercial roll-up metal shutter (cortina metálica replegable) cleanly
    /// driven by authoritative backend business availability (via the store's InteractionPoint).
    ///
    /// - For 24h storefronts (e.g., Cambios Santa Fe), set alwaysOpen = true so the shutter
    ///   remains rolled up inside its overhead drum housing.
    /// - For scheduled businesses (e.g., Óptica Fashion), binds automatically to
    ///   InteractionPoint_OpticaFashion.interactionAvailability synced from World Core:
    ///   lowers smoothly to sidewalk level when off_duty/unavailable and rolls up when open.
    /// </summary>
    public class StorefrontShutterController : MonoBehaviour
    {
        [Header("Authoritative State Binding")]
        [Tooltip("If true, shutter stays permanently rolled up (e.g., 24-hour Cambios Santa Fe).")]
        public bool alwaysOpen = false;

        [Tooltip("Name of the InteractionPoint GameObject synced with World Core open/closed status.")]
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

        private InteractionPoint linkedPoint;
        private float searchCooldown = 0f;

        public bool IsClosed => targetClosed;
        public float ClosedProgress => closedProgress;

        private void Awake()
        {
            if (alwaysOpen)
            {
                targetClosed = false;
                closedProgress = 0f;
            }
            ApplyCurtainVisual(closedProgress);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (alwaysOpen)
            {
                targetClosed = false;
            }
            else
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
