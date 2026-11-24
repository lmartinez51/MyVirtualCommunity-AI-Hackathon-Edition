using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Manages underground storm-drain atmosphere, fog, directional-sun attenuation,
    /// subtle flickering maintenance lights inside the Central Cistern Vault, procedural
    /// water-drip & tunnel-hum ambient audio, and seamless daylight fade-in as the player
    /// exits through the Rio Bravo storm outflow headwall (Z >= 186m).
    /// </summary>
    public class UndergroundSewerAtmosphereController : MonoBehaviour
    {
        public static UndergroundSewerAtmosphereController ActiveInstance { get; private set; }

        [Header("Underground Spatial Bounds")]
        public float undergroundThresholdY = -0.85f;
        public float outflowDaylightStartZ = 180.0f;
        public float outflowDaylightFullZ = 195.5f;

        [Header("Underground Visual Tuning")]
        public Color undergroundFogColor = new Color(0.035f, 0.055f, 0.060f, 1.0f);
        public float undergroundFogDensity = 0.052f;
        public Color undergroundAmbientSky = new Color(0.06f, 0.09f, 0.10f, 1.0f);
        public Color undergroundAmbientEquator = new Color(0.045f, 0.065f, 0.068f, 1.0f);
        public Color undergroundAmbientGround = new Color(0.03f, 0.035f, 0.035f, 1.0f);

        private Transform playerTransform;
        private Light sunLight;
        private SecretBackroomsTransitionController backroomsTransition;
        private readonly List<Light> undergroundLights = new List<Light>();
        private readonly List<Light> flickeringVaultLights = new List<Light>();
        private readonly List<float> flickeringBaseIntensities = new List<float>();

        private AudioSource ambientLoopSource;
        private AudioSource dripSource;
        private AudioClip dripClip;
        private float nextDripTime = 1.5f;

        private float currentUndergroundBlend = 0f;
        private float screenFadeAlpha = 0f;
        private Texture2D blackTex;

        public float UndergroundBlend => currentUndergroundBlend;
        public bool IsPlayerUnderground => currentUndergroundBlend > 0.12f;

        private void Awake()
        {
            ActiveInstance = this;
        }

        private void Start()
        {
            blackTex = new Texture2D(1, 1);
            blackTex.SetPixel(0, 0, Color.black);
            blackTex.Apply();

            DiscoverReferences();
            SetupProceduralSewerAudio();
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this) ActiveInstance = null;
            if (blackTex != null) Destroy(blackTex);
        }

        private void DiscoverReferences()
        {
            var fp = FindFirstObjectByType<FirstPersonPlayer>();
            if (fp != null) playerTransform = fp.transform;

            GameObject sunGo = GameObject.Find("Sun_DirectionalLight");
            if (sunGo != null) sunLight = sunGo.GetComponent<Light>();
            backroomsTransition = FindFirstObjectByType<SecretBackroomsTransitionController>();

            undergroundLights.Clear();
            flickeringVaultLights.Clear();
            flickeringBaseIntensities.Clear();

            Light[] childLights = GetComponentsInChildren<Light>(true);
            foreach (var l in childLights)
            {
                if (l == null) continue;
                undergroundLights.Add(l);
                if (l.gameObject.name.Contains("VaultFlicker"))
                {
                    flickeringVaultLights.Add(l);
                    flickeringBaseIntensities.Add(l.intensity);
                }
            }
        }

        public void SnapAtmosphereForCurrentPosition(Vector3 pos)
        {
            currentUndergroundBlend = ComputeTargetBlend(pos);
            ApplyAtmosphere();
        }

        public IEnumerator FadeScreen(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                screenFadeAlpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            screenFadeAlpha = toAlpha;
        }

        private float ComputeTargetBlend(Vector3 pos)
        {
            // Inside Backrooms scene (Y < -20m): do NOT interfere with Backrooms lighting
            if (pos.y < -20f) return 0f;
            if (backroomsTransition != null && (backroomsTransition.IsPlayerInBackrooms || backroomsTransition.IsTransitioning))
                return 0f;

            // Above street level -> 0
            if (pos.y > undergroundThresholdY) return 0f;

            // Outside the Rio Bravo storm outflow headwall (Z >= outflowDaylightFullZ) -> real outdoor sky & sun!
            if (pos.z >= outflowDaylightFullZ) return 0f;
            if (pos.z > outflowDaylightStartZ)
            {
                float t = Mathf.InverseLerp(outflowDaylightFullZ, outflowDaylightStartZ, pos.z);
                return Mathf.Clamp01(t);
            }

            return 1f;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var fp = FindFirstObjectByType<FirstPersonPlayer>();
                if (fp != null) playerTransform = fp.transform;
                else return;
            }

            float targetBlend = ComputeTargetBlend(playerTransform.position);
            currentUndergroundBlend = Mathf.MoveTowards(currentUndergroundBlend, targetBlend, Time.deltaTime * 2.4f);

            ApplyAtmosphere();
            UpdateAudio();
            UpdateFlickerLights();
        }

        private void ApplyAtmosphere()
        {
            bool lightsActive = currentUndergroundBlend > 0.02f;
            for (int i = 0; i < undergroundLights.Count; i++)
            {
                if (undergroundLights[i] != null && undergroundLights[i].enabled != lightsActive)
                {
                    undergroundLights[i].enabled = lightsActive;
                }
            }

            if (currentUndergroundBlend <= 0.005f) return;

            // Override scene lighting smoothly while deep inside the storm drains
            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.04f, currentUndergroundBlend);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, undergroundFogColor, currentUndergroundBlend);
            RenderSettings.fogDensity = Mathf.Lerp(0.0025f, undergroundFogDensity, currentUndergroundBlend);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, undergroundAmbientSky, currentUndergroundBlend);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, undergroundAmbientEquator, currentUndergroundBlend);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, undergroundAmbientGround, currentUndergroundBlend);
        }

        private void UpdateFlickerLights()
        {
            if (currentUndergroundBlend <= 0.05f) return;
            for (int i = 0; i < flickeringVaultLights.Count; i++)
            {
                Light l = flickeringVaultLights[i];
                if (l == null) continue;
                float n = Mathf.PerlinNoise(Time.time * 4.5f + i * 11.3f, 0.25f);
                float dip = n < 0.20f ? 0.28f : (0.85f + 0.22f * n);
                l.intensity = flickeringBaseIntensities[i] * dip;
            }
        }

        private void UpdateAudio()
        {
            if (ambientLoopSource != null)
            {
                ambientLoopSource.volume = 0.28f * currentUndergroundBlend;
                if (currentUndergroundBlend > 0.02f && !ambientLoopSource.isPlaying)
                    ambientLoopSource.Play();
                else if (currentUndergroundBlend <= 0.02f && ambientLoopSource.isPlaying)
                    ambientLoopSource.Stop();
            }

            if (currentUndergroundBlend > 0.35f && dripSource != null && dripClip != null && Time.time >= nextDripTime)
            {
                dripSource.pitch = Random.Range(0.78f, 1.32f);
                dripSource.panStereo = Random.Range(-0.75f, 0.75f);
                dripSource.PlayOneShot(dripClip, Random.Range(0.14f, 0.32f) * currentUndergroundBlend);
                nextDripTime = Time.time + Random.Range(1.4f, 4.2f);
            }
        }

        private void OnGUI()
        {
            if (screenFadeAlpha > 0.01f && blackTex != null)
            {
                Color prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, screenFadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
                GUI.color = prev;
            }
        }

        private void SetupProceduralSewerAudio()
        {
            ambientLoopSource = gameObject.AddComponent<AudioSource>();
            ambientLoopSource.loop = true;
            ambientLoopSource.playOnAwake = false;
            ambientLoopSource.spatialBlend = 0f;
            ambientLoopSource.volume = 0f;
            ambientLoopSource.clip = CreateTunnelWaterHumClip();

            dripSource = gameObject.AddComponent<AudioSource>();
            dripSource.playOnAwake = false;
            dripSource.spatialBlend = 0f;
            dripClip = CreateWaterDripClip();
        }

        private static AudioClip CreateTunnelWaterHumClip()
        {
            int sr = 22050;
            int samples = sr * 4;
            float[] data = new float[samples];
            System.Random rng = new System.Random(4407);
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float white = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.94f + white * 0.06f; // Low-pass rushing drain water
                float resonance = Mathf.Sin(2f * Mathf.PI * 58f * t) * 0.14f + Mathf.Sin(2f * Mathf.PI * 116f * t) * 0.06f;
                // Seamless loop fade
                float fade = 1f;
                if (i < 1000) fade = i / 1000f;
                else if (i > samples - 1000) fade = (samples - i) / 1000f;
                data[i] = (lp * 1.6f + resonance) * 0.45f * fade;
            }
            AudioClip clip = AudioClip.Create("SewerAmbientWaterHum", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWaterDripClip()
        {
            int sr = 22050;
            int samples = Mathf.RoundToInt(sr * 0.22f);
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float freq = Mathf.Lerp(620f, 1650f, Mathf.Clamp01(t / 0.06f));
                phase += 2f * Mathf.PI * freq / sr;
                float env = Mathf.Exp(-t * 28f);
                data[i] = Mathf.Sin(phase) * env * 0.7f;
            }
            AudioClip clip = AudioClip.Create("SewerWaterDrip", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
