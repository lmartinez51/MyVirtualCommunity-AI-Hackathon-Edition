using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Authoritative status of the World Clock connection on the client.
    /// </summary>
    public enum AuthoritativeTimeState
    {
        /// <summary>No valid authoritative timestamp has been received yet (neutral fallback).</summary>
        Unknown,
        /// <summary>Authoritative world time is fresh (received recently within refresh window).</summary>
        Fresh,
        /// <summary>Prior authoritative time received, but updates have stopped / timed out.</summary>
        Stale
    }

    /// <summary>
    /// Controls deterministic time-of-day environmental presentation for the Avenida Benito Juárez corridor.
    /// Synchronizes directional sunlight/moonlight, ambient lighting, skybox exposure/tint,
    /// distance fog, and streetlights with the authoritative World Clock from the World Core.
    ///
    /// ARCHITECTURAL INVARIANT:
    /// Unity is strictly a presentation consumer. It NEVER uses DateTime.Now, DateTime.UtcNow,
    /// or local system time to determine the simulation time or time-of-day phase.
    /// </summary>
    public class TimeOfDayEnvironmentController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private ApiClient apiClient;

        [Header("Transition Settings")]
        [Tooltip("Speed multiplier for smooth visual transitions between phases.")]
        [SerializeField] private float transitionSpeed = 2.0f;
        [Tooltip("Seconds after which an authoritative timestamp is considered stale if not refreshed.")]
        [SerializeField] private float staleThresholdSeconds = 60.0f;
        [Tooltip("Interval in seconds between authoritative environment polling.")]
        [SerializeField] private float refreshIntervalSeconds = 20.0f;

        // Runtime-managed streetlights
        private struct ManagedLight
        {
            public Light lightComponent;
            public float baseIntensity;
        }
        private readonly List<ManagedLight> managedStreetlights = new List<ManagedLight>();

        // Runtime-managed luminaire emissive diffusers (Luminaire_Bowl)
        private struct ManagedLuminaireRenderer
        {
            public Renderer rendererComponent;
            public Color dayBaseColor;
            public Color nightBaseColor;
            public Color nightEmissionColor;
        }
        private readonly List<ManagedLuminaireRenderer> managedLuminaireRenderers = new List<ManagedLuminaireRenderer>();
        private MaterialPropertyBlock luminairePropertyBlock;

        // Material instance for non-destructive runtime skybox control
        private Material runtimeSkyboxMaterial;

        // Current Authoritative State
        private AuthoritativeTimeState timeState = AuthoritativeTimeState.Unknown;
        private string canonicalUtcInstant = "";
        private string localCivilTimeIso = "";
        private string localCivilTimeFormatted = "--:--";
        private string currentPhase = "midday";
        private string currentPhaseTitleEn = "Midday";
        private string currentPhaseTitleEs = "Mediodía";
        private float phaseProgress = 0.5f;
        private bool isNight = false;
        private float lastSuccessfulReceiveTime = -999f;
        private float lastPollTime = -999f;
        private bool isInitialSnapDone = false;

        // Target environmental parameters (from authoritative backend)
        private float targetSunPitch = 68.0f;
        private float targetSunYaw = 175.0f;
        private float targetSunIntensity = 1.35f;
        private Color targetSunColor = Color.white;
        private float targetAmbientIntensity = 1.1f;
        private Color targetAmbientSky = new Color(0.6f, 0.72f, 0.88f);
        private Color targetAmbientEquator = new Color(0.44f, 0.48f, 0.53f);
        private Color targetAmbientGround = new Color(0.26f, 0.24f, 0.22f);
        private bool targetStreetlightsActive = false;
        private float targetStreetlightsIntensity = 0.0f;
        private float targetSkyboxExposure = 1.1f;
        private Color targetFogColor = new Color(0.6f, 0.72f, 0.88f);

        // Current interpolated environmental parameters (active rendering)
        private float currentSunPitch = 68.0f;
        private float currentSunYaw = 175.0f;
        private float currentSunIntensity = 1.35f;
        private Color currentSunColor = Color.white;
        private float currentAmbientIntensity = 1.1f;
        private Color currentAmbientSky = new Color(0.6f, 0.72f, 0.88f);
        private Color currentAmbientEquator = new Color(0.44f, 0.48f, 0.53f);
        private Color currentAmbientGround = new Color(0.26f, 0.24f, 0.22f);
        private float currentStreetlightsIntensity = 0.0f;
        private float currentSkyboxExposure = 1.1f;
        private Color currentFogColor = new Color(0.6f, 0.72f, 0.88f);

        #region Public Properties

        public Light SunLight
        {
            get => sunLight;
            set => sunLight = value;
        }

        public AuthoritativeTimeState TimeState => timeState;
        public string CanonicalUtcInstant => canonicalUtcInstant;
        public string LocalCivilTimeIso => localCivilTimeIso;
        public string LocalCivilTimeFormatted => localCivilTimeFormatted;
        public string CurrentPhase => currentPhase;
        public string CurrentPhaseTitleEn => currentPhaseTitleEn;
        public string CurrentPhaseTitleEs => currentPhaseTitleEs;
        public float PhaseProgress => phaseProgress;
        public bool IsNight => isNight;

        #endregion

        private void Awake()
        {
            DiscoverSceneReferences();
            SetupRuntimeSkybox();
        }

        private void Start()
        {
            // Initial authoritative sync
            PollAuthoritativeEnvironment();
        }

        private void Update()
        {
            // Update time freshness state
            UpdateFreshnessStatus();

            // Periodic authoritative polling
            if (Time.time - lastPollTime >= refreshIntervalSeconds)
            {
                lastPollTime = Time.time;
                PollAuthoritativeEnvironment();
            }

            // Smoothly interpolate visual presentation towards authoritative targets
            InterpolateParameters(Time.deltaTime);
            ApplyParametersToScene();
        }

        private void OnDestroy()
        {
            // Clean up runtime material copy
            if (runtimeSkyboxMaterial != null)
            {
                Destroy(runtimeSkyboxMaterial);
                runtimeSkyboxMaterial = null;
            }
        }

        #region Scene Setup & Discovery

        public void DiscoverSceneReferences()
        {
            // 1. Locate ApiClient
            if (apiClient == null)
            {
                apiClient = FindAnyObjectByType<ApiClient>();
            }

            // 2. Locate Sun Directional Light
            if (sunLight == null)
            {
                GameObject sunGo = GameObject.Find("Sun_DirectionalLight");
                if (sunGo != null)
                {
                    sunLight = sunGo.GetComponent<Light>();
                }
            }
            if (sunLight == null)
            {
                // Fallback: locate any directional light in active scene
                Light[] allLights = Resources.FindObjectsOfTypeAll<Light>();
                foreach (var l in allLights)
                {
                    if (l != null && l.type == LightType.Directional && l.gameObject.scene.isLoaded)
                    {
                        sunLight = l;
                        break;
                    }
                }
            }

            // 3. Locate and index corridor streetlights
            managedStreetlights.Clear();
            Light[] candidateLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in candidateLights)
            {
                if (l == null || l.type == LightType.Directional) continue;

                // Check if belongs to street lighting or canopy downlights hierarchy
                Transform current = l.transform;
                bool isStreetlight = false;
                while (current != null)
                {
                    string nameLower = current.name.ToLowerInvariant();
                    if (nameLower.Contains("street_lighting") ||
                        nameLower.Contains("streetlight") ||
                        nameLower.Contains("canopy_downlights") ||
                        nameLower.Contains("downlight"))
                    {
                        isStreetlight = true;
                        break;
                    }
                    current = current.parent;
                }

                if (isStreetlight)
                {
                    managedStreetlights.Add(new ManagedLight
                    {
                        lightComponent = l,
                        baseIntensity = Mathf.Max(0.5f, l.intensity)
                    });
                }
            }

            // 4. Locate and index corridor streetlight diffuser bowls (Luminaire_Bowl)
            managedLuminaireRenderers.Clear();
            Renderer[] candidateRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in candidateRenderers)
            {
                if (r == null) continue;
                string rNameLower = r.gameObject.name.ToLowerInvariant();
                if (rNameLower.Contains("luminaire_bowl") || rNameLower.Contains("luminaire_bulb"))
                {
                    managedLuminaireRenderers.Add(new ManagedLuminaireRenderer
                    {
                        rendererComponent = r,
                        dayBaseColor = new Color(0.92f, 0.93f, 0.95f, 1.0f),
                        nightBaseColor = new Color(1.00f, 0.93f, 0.72f, 1.0f),
                        nightEmissionColor = new Color(2.40f, 1.95f, 1.15f, 1.0f)
                    });
                }
            }
        }

        private void SetupRuntimeSkybox()
        {
            if (RenderSettings.skybox != null && runtimeSkyboxMaterial == null)
            {
                runtimeSkyboxMaterial = new Material(RenderSettings.skybox);
                runtimeSkyboxMaterial.name = "Runtime_Skybox_Instance";
                RenderSettings.skybox = runtimeSkyboxMaterial;
            }

            // Configure ambient mode for gradient trilight
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0025f;
        }

        #endregion

        #region Authoritative Backend Polling

        public void PollAuthoritativeEnvironment()
        {
            if (apiClient == null)
            {
                apiClient = FindAnyObjectByType<ApiClient>();
                if (apiClient == null) return;
            }

            apiClient.GetWorldEnvironment(
                res =>
                {
                    if (res != null && res.success && res.environment != null)
                    {
                        ApplyAuthoritativeSnapshot(res);
                    }
                },
                err =>
                {
                    // Fail gracefully: retain last valid visual state without fabricating world time
                }
            );
        }

        public void ApplyAuthoritativeSnapshot(WorldEnvironmentResponseDto dto, bool snapImmediately = false)
        {
            if (dto == null || dto.environment == null) return;

            canonicalUtcInstant = dto.evaluatedAt ?? "";
            localCivilTimeIso = dto.localCivilTime ?? "";
            timeState = AuthoritativeTimeState.Fresh;
            lastSuccessfulReceiveTime = Time.time;

            // Format HH:mm for HUD
            if (dto.civilTime != null)
            {
                localCivilTimeFormatted = $"{dto.civilTime.hour:D2}:{dto.civilTime.minute:D2}";
            }
            else if (!string.IsNullOrEmpty(localCivilTimeIso) && localCivilTimeIso.Length >= 16)
            {
                // Extract from ISO: YYYY-MM-DDTHH:mm
                int tIdx = localCivilTimeIso.IndexOf('T');
                if (tIdx >= 0 && localCivilTimeIso.Length >= tIdx + 6)
                {
                    localCivilTimeFormatted = localCivilTimeIso.Substring(tIdx + 1, 5);
                }
            }

            var env = dto.environment;
            currentPhase = env.phase ?? "midday";
            if (env.phaseTitle != null)
            {
                currentPhaseTitleEn = env.phaseTitle.en ?? currentPhase;
                currentPhaseTitleEs = env.phaseTitle.es ?? currentPhase;
            }
            phaseProgress = env.phaseProgress;
            isNight = env.isNight;

            // Parse target values
            targetSunPitch = env.sunPitchDegrees;
            targetSunYaw = env.sunYawDegrees;
            targetSunIntensity = env.sunIntensity;
            if (!string.IsNullOrEmpty(env.sunColorHex) && ColorUtility.TryParseHtmlString(env.sunColorHex, out Color sc))
            {
                targetSunColor = sc;
            }

            targetAmbientIntensity = env.ambientIntensity;
            if (!string.IsNullOrEmpty(env.ambientSkyHex) && ColorUtility.TryParseHtmlString(env.ambientSkyHex, out Color asky))
            {
                targetAmbientSky = asky;
            }
            if (!string.IsNullOrEmpty(env.ambientEquatorHex) && ColorUtility.TryParseHtmlString(env.ambientEquatorHex, out Color aeq))
            {
                targetAmbientEquator = aeq;
            }
            if (!string.IsNullOrEmpty(env.ambientGroundHex) && ColorUtility.TryParseHtmlString(env.ambientGroundHex, out Color agnd))
            {
                targetAmbientGround = agnd;
            }

            targetStreetlightsActive = env.streetlightsActive;
            targetStreetlightsIntensity = env.streetlightsIntensity;
            targetSkyboxExposure = env.skyboxExposure;

            if (!string.IsNullOrEmpty(env.fogColorHex) && ColorUtility.TryParseHtmlString(env.fogColorHex, out Color fc))
            {
                targetFogColor = fc;
            }

            // If initial snapshot after startup or explicit snap requested, snap immediately
            if (!isInitialSnapDone || snapImmediately || !Application.isPlaying)
            {
                SnapToTargetParameters();
                isInitialSnapDone = true;
            }
        }

        private void UpdateFreshnessStatus()
        {
            if (timeState == AuthoritativeTimeState.Unknown)
            {
                return;
            }

            if (Time.time - lastSuccessfulReceiveTime > staleThresholdSeconds)
            {
                timeState = AuthoritativeTimeState.Stale;
            }
            else
            {
                timeState = AuthoritativeTimeState.Fresh;
            }
        }

        #endregion

        #region Visual Interpolation & Application

        public void SnapToTargetParameters()
        {
            if (sunLight == null)
            {
                DiscoverSceneReferences();
            }

            currentSunPitch = targetSunPitch;
            currentSunYaw = targetSunYaw;
            currentSunIntensity = targetSunIntensity;
            currentSunColor = targetSunColor;

            currentAmbientIntensity = targetAmbientIntensity;
            currentAmbientSky = targetAmbientSky;
            currentAmbientEquator = targetAmbientEquator;
            currentAmbientGround = targetAmbientGround;

            currentStreetlightsIntensity = targetStreetlightsIntensity;
            currentSkyboxExposure = targetSkyboxExposure;
            currentFogColor = targetFogColor;

            ApplyParametersToScene();
        }

        private void InterpolateParameters(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            float step = deltaTime * transitionSpeed;

            // Directional Light angles and intensity
            currentSunPitch = Mathf.MoveTowards(currentSunPitch, targetSunPitch, step * 15f);
            currentSunYaw = Mathf.MoveTowardsAngle(currentSunYaw, targetSunYaw, step * 15f);
            currentSunIntensity = Mathf.MoveTowards(currentSunIntensity, targetSunIntensity, step * 2f);
            currentSunColor = Color.Lerp(currentSunColor, targetSunColor, step * 3f);

            // Ambient Lighting
            currentAmbientIntensity = Mathf.MoveTowards(currentAmbientIntensity, targetAmbientIntensity, step * 2f);
            currentAmbientSky = Color.Lerp(currentAmbientSky, targetAmbientSky, step * 3f);
            currentAmbientEquator = Color.Lerp(currentAmbientEquator, targetAmbientEquator, step * 3f);
            currentAmbientGround = Color.Lerp(currentAmbientGround, targetAmbientGround, step * 3f);

            // Atmosphere & Sky
            currentSkyboxExposure = Mathf.MoveTowards(currentSkyboxExposure, targetSkyboxExposure, step * 2f);
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, step * 3f);

            // Streetlights
            currentStreetlightsIntensity = Mathf.MoveTowards(currentStreetlightsIntensity, targetStreetlightsIntensity, step * 2f);
        }

        private void ApplyParametersToScene()
        {
            // 1. Sun / Moon Directional Light
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(currentSunPitch, currentSunYaw, 0f);
                sunLight.color = currentSunColor;
                sunLight.intensity = currentSunIntensity;
            }

            // 2. Ambient Trilight
            RenderSettings.ambientIntensity = currentAmbientIntensity;
            RenderSettings.ambientSkyColor = currentAmbientSky;
            RenderSettings.ambientEquatorColor = currentAmbientEquator;
            RenderSettings.ambientGroundColor = currentAmbientGround;

            // 3. Skybox Exposure & Tint
            if (runtimeSkyboxMaterial != null)
            {
                if (runtimeSkyboxMaterial.HasProperty("_Exposure"))
                {
                    runtimeSkyboxMaterial.SetFloat("_Exposure", currentSkyboxExposure);
                }
                if (runtimeSkyboxMaterial.HasProperty("_SkyTint"))
                {
                    runtimeSkyboxMaterial.SetColor("_SkyTint", currentAmbientSky);
                }
            }

            // 4. Atmospheric Fog
            RenderSettings.fogColor = currentFogColor;

            // 5. Streetlights & Canopy Downlights
            bool anyStreetlightsActive = currentStreetlightsIntensity > 0.05f;
            for (int i = 0; i < managedStreetlights.Count; i++)
            {
                var ml = managedStreetlights[i];
                if (ml.lightComponent == null) continue;

                ml.lightComponent.enabled = anyStreetlightsActive;
                if (anyStreetlightsActive)
                {
                    ml.lightComponent.intensity = ml.baseIntensity * currentStreetlightsIntensity;
                }
            }

            // 6. Streetlight Luminaire Bowl Diffusers (Visual Day/Night Glow)
            if (managedLuminaireRenderers.Count > 0)
            {
                if (luminairePropertyBlock == null)
                {
                    luminairePropertyBlock = new MaterialPropertyBlock();
                }

                float glowFactor = anyStreetlightsActive ? Mathf.Clamp01(currentStreetlightsIntensity) : 0f;
                for (int i = 0; i < managedLuminaireRenderers.Count; i++)
                {
                    var mr = managedLuminaireRenderers[i];
                    if (mr.rendererComponent == null) continue;

                    Color activeBase = Color.Lerp(mr.dayBaseColor, mr.nightBaseColor, glowFactor);
                    Color activeEmission = mr.nightEmissionColor * glowFactor;

                    mr.rendererComponent.GetPropertyBlock(luminairePropertyBlock);
                    luminairePropertyBlock.SetColor("_BaseColor", activeBase);
                    luminairePropertyBlock.SetColor("_Color", activeBase);
                    luminairePropertyBlock.SetColor("_EmissionColor", activeEmission);
                    mr.rendererComponent.SetPropertyBlock(luminairePropertyBlock);
                }
            }
        }

        #endregion

        #region Public Helpers for HUD Display

        public string GetFormattedHUDString(bool isSpanish)
        {
            string phaseName = isSpanish ? currentPhaseTitleEs : currentPhaseTitleEn;

            switch (timeState)
            {
                case AuthoritativeTimeState.Fresh:
                    return $"[{localCivilTimeFormatted} {phaseName}]";

                case AuthoritativeTimeState.Stale:
                    return isSpanish
                        ? $"[{localCivilTimeFormatted} {phaseName} (Desactualizado)]"
                        : $"[{localCivilTimeFormatted} {phaseName} (Stale)]";

                case AuthoritativeTimeState.Unknown:
                default:
                    return isSpanish
                        ? "[--:-- Sin hora autoritativa]"
                        : "[--:-- No Authoritative Time]";
            }
        }

        public Color GetHUDColor()
        {
            switch (timeState)
            {
                case AuthoritativeTimeState.Fresh:
                    return isNight ? new Color(0.6f, 0.78f, 1.0f) : new Color(1.0f, 0.95f, 0.75f);
                case AuthoritativeTimeState.Stale:
                    return new Color(1.0f, 0.72f, 0.35f); // Amber warning
                case AuthoritativeTimeState.Unknown:
                default:
                    return new Color(0.65f, 0.65f, 0.65f); // Muted gray
            }
        }

        #endregion
    }
}
