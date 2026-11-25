using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Adds subtle, authentic Backrooms Level 0 fluorescent ceiling-panel hum/flicker
    /// to selected fixtures without modifying shared materials globally.
    /// </summary>
    public class BackroomsAmbientFlicker : MonoBehaviour
    {
        public Light targetLight;
        public Renderer panelRenderer;
        public float baseIntensity = 1.05f;
        public float flickerStrength = 0.22f;
        public float phaseOffset = 0f;

        private Material runtimePanelMat;
        private Color baseEmissionColor = new Color(0.84f, 0.92f, 0.98f) * 2.2f;

        private void Start()
        {
            if (targetLight == null) targetLight = GetComponentInChildren<Light>();
            if (panelRenderer != null && Application.isPlaying && panelRenderer.sharedMaterial != null)
            {
                runtimePanelMat = new Material(panelRenderer.sharedMaterial);
                panelRenderer.sharedMaterial = runtimePanelMat;
                if (runtimePanelMat.HasProperty("_EmissionColor"))
                {
                    baseEmissionColor = runtimePanelMat.GetColor("_EmissionColor");
                }
            }
        }

        private void Update()
        {
            float t = Time.time + phaseOffset;
            // Periodic subtle fluorescent ballast dip
            float wave = Mathf.Sin(t * 2.3f) * Mathf.Sin(t * 7.9f);
            float dip = wave > 0.86f ? (1f - flickerStrength * Mathf.Abs(Mathf.Sin(t * 47f))) : 1f;

            if (targetLight != null)
            {
                targetLight.intensity = baseIntensity * dip;
            }
            if (runtimePanelMat != null && runtimePanelMat.HasProperty("_EmissionColor"))
            {
                runtimePanelMat.SetColor("_EmissionColor", baseEmissionColor * dip);
            }
        }

        private void OnDestroy()
        {
            if (runtimePanelMat != null)
            {
                Destroy(runtimePanelMat);
                runtimePanelMat = null;
            }
        }
    }
}
