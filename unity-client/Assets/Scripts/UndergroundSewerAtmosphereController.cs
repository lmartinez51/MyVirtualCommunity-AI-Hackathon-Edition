using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Manages underground storm-drain atmosphere: dense dark fog, suppressed outdoor sunlight,
    /// near-black damp ambient gloom (making the [F] flashlight essential), erratic flickering
    /// old maintenance lights inside the Central Cistern Vault, subtle low tunnel hum with sparse
    /// organic water droplets, and smooth daylight fade-in exiting to the Rio Bravo (Z >= 182m).
    /// </summary>
    public class UndergroundSewerAtmosphereController : MonoBehaviour
    {
        public static UndergroundSewerAtmosphereController ActiveInstance { get; private set; }

        [Header("Underground Spatial Bounds")]
        public float undergroundThresholdY = -0.85f;
        public float outflowDaylightStartZ = 162.0f;
        public float outflowDaylightFullZ = 190.5f;

        [Header("Dense Dark Underground Visual Tuning")]
        public Color undergroundFogColor = new Color(0.004f, 0.007f, 0.008f, 1.0f);
        public float undergroundFogDensity = 0.098f;
        public Color undergroundAmbientSky = new Color(0.0045f, 0.007f, 0.0075f, 1.0f);
        public Color undergroundAmbientEquator = new Color(0.003f, 0.005f, 0.0055f, 1.0f);
        public Color undergroundAmbientGround = new Color(0.0018f, 0.0025f, 0.0028f, 1.0f);
        public float undergroundAmbientIntensity = 0.28f;
        public float undergroundReflectionIntensity = 0.02f;

        private Transform playerTransform;
        private Light sunLight;
        private SecretBackroomsTransitionController backroomsTransition;
        private readonly List<Light> undergroundLights = new List<Light>();
        private readonly List<Light> flickeringVaultLights = new List<Light>();
        private readonly List<float> flickeringBaseIntensities = new List<float>();

        private AudioSource ambientLoopSource;
        private AudioSource dripSource;
        private AudioClip dripClipSoft;
        private AudioClip dripClipDeep;
        private float nextDripTime = 6.5f;

        private float currentUndergroundBlend = 0f;
        private bool wasUndergroundApplied = false;
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
                // Keep exterior Rio Bravo under-bridge floodlights active at all times (never disable when exiting underground!)
                if (l.gameObject.name.Contains("BridgeFlood")) continue;
                undergroundLights.Add(l);
                if (l.gameObject.name.Contains("VaultFlicker") || l.gameObject.name.Contains("Sputter"))
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
            if (pos.y < -20f) return 0f;
            if (backroomsTransition != null && (backroomsTransition.IsPlayerInBackrooms || backroomsTransition.IsTransitioning))
                return 0f;

            if (pos.y > undergroundThresholdY) return 0f;

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

            if (currentUndergroundBlend <= 0.005f)
            {
                if (wasUndergroundApplied)
                {
                    // Cleanly restore outdoor skybox reflections when returning to the surface
                    RenderSettings.reflectionIntensity = 1.0f;
                    wasUndergroundApplied = false;
                }
                return;
            }

            wasUndergroundApplied = true;

            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.0f, currentUndergroundBlend);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, undergroundFogColor, currentUndergroundBlend);
            RenderSettings.fogDensity = Mathf.Lerp(0.0025f, undergroundFogDensity, currentUndergroundBlend);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = Mathf.Lerp(1.0f, undergroundAmbientIntensity, currentUndergroundBlend);
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, undergroundAmbientSky, currentUndergroundBlend);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, undergroundAmbientEquator, currentUndergroundBlend);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, undergroundAmbientGround, currentUndergroundBlend);

            // Suppress outdoor skybox reflections underground so water & damp concrete stay pitch-dark instead of glowing sky-blue!
            RenderSettings.reflectionIntensity = Mathf.Lerp(1.0f, undergroundReflectionIntensity, currentUndergroundBlend);
        }

        private void UpdateFlickerLights()
        {
            if (currentUndergroundBlend <= 0.05f) return;
            for (int i = 0; i < flickeringVaultLights.Count; i++)
            {
                Light l = flickeringVaultLights[i];
                if (l == null) continue;
                float n = Mathf.PerlinNoise(Time.time * 5.2f + i * 13.7f, 0.35f);
                float sputter = n < 0.22f ? 0.14f : (n < 0.34f ? 0.48f : (0.82f + 0.18f * n));
                l.intensity = flickeringBaseIntensities[i] * sputter;
            }
        }

        private void UpdateAudio()
        {
            if (ambientLoopSource != null)
            {
                ambientLoopSource.volume = 0.18f * currentUndergroundBlend;
                if (currentUndergroundBlend > 0.02f && !ambientLoopSource.isPlaying)
                    ambientLoopSource.Play();
                else if (currentUndergroundBlend <= 0.02f && ambientLoopSource.isPlaying)
                    ambientLoopSource.Stop();
            }

            // Sparse, subtle, organic water droplets (every 9.5–22 seconds instead of annoying constant dripping!)
            if (currentUndergroundBlend > 0.45f && dripSource != null && Time.time >= nextDripTime)
            {
                AudioClip chosen = Random.value < 0.55f ? dripClipSoft : dripClipDeep;
                if (chosen != null)
                {
                    dripSource.pitch = Random.Range(0.84f, 1.16f);
                    dripSource.panStereo = Random.Range(-0.70f, 0.70f);
                    dripSource.PlayOneShot(chosen, Random.Range(0.08f, 0.17f) * currentUndergroundBlend);
                }
                // Occasional subtle second echo drop (20% chance), otherwise long quiet pause!
                nextDripTime = Time.time + (Random.value < 0.20f ? Random.Range(0.55f, 1.15f) : Random.Range(9.5f, 22.0f));
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
            dripClipSoft = CreateNaturalWaterDripClip("SewerDripSoft", 460f, 980f, 0.14f);
            dripClipDeep = CreateNaturalWaterDripClip("SewerDripDeep", 310f, 680f, 0.18f);
        }

        private static AudioClip CreateTunnelWaterHumClip()
        {
            int sr = 22050;
            int samples = sr * 5;
            float[] data = new float[samples];
            System.Random rng = new System.Random(4407);
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float white = (float)rng.NextDouble() * 2f - 1f;
                // Double low-pass for deep, warm subterranean airflow & distant flowing drain water
                lp1 = lp1 * 0.955f + white * 0.045f;
                lp2 = lp2 * 0.92f + lp1 * 0.08f;
                float resonance = Mathf.Sin(2f * Mathf.PI * 52f * t) * 0.09f;
                float fade = 1f;
                if (i < 1200) fade = i / 1200f;
                else if (i > samples - 1200) fade = (samples - i) / 1200f;
                data[i] = (lp2 * 2.4f + resonance) * 0.42f * fade;
            }
            AudioClip clip = AudioClip.Create("SewerAmbientWaterHum", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNaturalWaterDripClip(string name, float fStart, float fEnd, float duration)
        {
            int sr = 22050;
            int samples = Mathf.RoundToInt(sr * duration);
            float[] data = new float[samples];
            float phase = 0f;
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float u = Mathf.Clamp01(t / (duration * 0.45f));
                float freq = Mathf.Lerp(fStart, fEnd, u * u);
                phase += 2f * Mathf.PI * freq / sr;
                float attack = Mathf.Clamp01(t / 0.004f);
                float decay = Mathf.Exp(-t * 34f);
                float raw = Mathf.Sin(phase) * attack * decay;
                lp = lp * 0.55f + raw * 0.45f; // Soft dampened concrete cave absorption
                data[i] = lp * 0.55f;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
