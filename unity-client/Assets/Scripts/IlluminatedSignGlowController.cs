using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Controls day/night HDR illumination and localized night glow lights for
    /// 3D illuminated storefront channel-letter signs (e.g., Farmalivio).
    /// Uses a serialized URP/Lit _EMISSION material template so Unity never strips
    /// the _EMISSION shader variant from standalone builds.
    /// </summary>
    public class IlluminatedSignGlowController : MonoBehaviour
    {
        [Header("Emissive Faces & Night Glow Lights")]
        public Renderer[] emissiveRenderers;
        public Material baseEmissiveMaterial;
        public Light[] nightGlowLights;

        [Header("Day / Night Illumination Settings")]
        public Color emissionTint = Color.white;
        public float dayEmissionIntensity = 0.45f;
        public float nightEmissionIntensity = 3.8f;
        public Color nightLightColor = new Color(0.10f, 0.90f, 0.75f, 1.0f);
        public float nightLightIntensity = 1.65f;
        public float nightLightRange = 4.2f;

        private Material runtimeMaterial;
        private TimeOfDayEnvironmentController timeOfDayController;
        private bool? nightOverrideForPreview = null;
        private bool lastAppliedNight = false;
        private bool initialized = false;

        private void Awake()
        {
            EnsureResources();
            ApplyIllumination(IsNightModeActive());
        }

        private void OnEnable()
        {
            EnsureResources();
            ApplyIllumination(IsNightModeActive());
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial);
                else DestroyImmediate(runtimeMaterial);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            bool isNight = IsNightModeActive();
            if (!initialized || isNight != lastAppliedNight)
            {
                ApplyIllumination(isNight);
            }
        }

        public void SetNightPreviewOverride(bool? forceNight)
        {
            nightOverrideForPreview = forceNight;
            EnsureResources();
            ApplyIllumination(IsNightModeActive());
        }

        private bool IsNightModeActive()
        {
            if (nightOverrideForPreview.HasValue)
            {
                return nightOverrideForPreview.Value;
            }
            if (timeOfDayController == null && Application.isPlaying)
            {
                timeOfDayController = FindAnyObjectByType<TimeOfDayEnvironmentController>();
            }
            if (timeOfDayController != null)
            {
                return timeOfDayController.IsNight;
            }
            return RenderSettings.ambientIntensity < 0.55f;
        }

        private void EnsureResources()
        {
            if (runtimeMaterial == null && baseEmissiveMaterial != null)
            {
                runtimeMaterial = new Material(baseEmissiveMaterial)
                {
                    name = $"{gameObject.name}_RuntimeGlowMat"
                };
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                if (emissiveRenderers != null)
                {
                    for (int i = 0; i < emissiveRenderers.Length; i++)
                    {
                        if (emissiveRenderers[i] != null)
                        {
                            emissiveRenderers[i].sharedMaterial = runtimeMaterial;
                        }
                    }
                }
            }
        }

        private void ApplyIllumination(bool isNight)
        {
            initialized = true;
            lastAppliedNight = isNight;

            float intensity = isNight ? nightEmissionIntensity : dayEmissionIntensity;
            if (runtimeMaterial != null)
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor("_EmissionColor", new Color(
                    emissionTint.r * intensity,
                    emissionTint.g * intensity,
                    emissionTint.b * intensity,
                    1.0f));
            }

            if (nightGlowLights != null)
            {
                for (int i = 0; i < nightGlowLights.Length; i++)
                {
                    if (nightGlowLights[i] == null) continue;
                    nightGlowLights[i].enabled = isNight && nightLightIntensity > 0.01f;
                    nightGlowLights[i].color = nightLightColor;
                    nightGlowLights[i].intensity = isNight ? nightLightIntensity : 0.0f;
                    nightGlowLights[i].range = nightLightRange;
                }
            }
        }
    }
}
