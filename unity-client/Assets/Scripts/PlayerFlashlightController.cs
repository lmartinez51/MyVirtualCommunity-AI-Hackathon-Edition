using UnityEngine;
using UnityEngine.Rendering;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Handheld player flashlight toggled with [F] during normal first-person exploration
    /// (both underground in the storm-drain network and outdoors at night).
    /// Features smooth physical aim lag, procedural mechanical click audio, and URP spot lighting.
    /// </summary>
    public class PlayerFlashlightController : MonoBehaviour
    {
        [Header("Flashlight Tuning")]
        [SerializeField] private float spotRange = 42f;
        [SerializeField] private float spotAngle = 48f;
        [SerializeField] private float innerSpotAngle = 20f;
        [SerializeField] private float spotIntensity = 68.0f;
        [SerializeField] private Color beamColor = new Color(1.00f, 0.97f, 0.90f);
        [SerializeField] private float aimFollowSpeed = 14f;

        private Light spotLight;
        private Light fillBounceLight;
        private Transform lightRig;
        private Transform cameraTransform;
        private AudioSource clickAudioSource;
        private AudioClip clickOnClip;
        private AudioClip clickOffClip;
        private bool isFlashlightOn = false;

        public bool IsFlashlightOn => isFlashlightOn;

        public static PlayerFlashlightController ActiveInstance { get; private set; }

        private void Awake()
        {
            ActiveInstance = this;
        }

        private void Start()
        {
            ResolveCameraAndBuildRig();
            BuildProceduralClickClips();
            ApplyLightState(false, playSound: false);
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        private void ResolveCameraAndBuildRig()
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : transform;

            GameObject rigGo = new GameObject("PlayerFlashlight_Rig");
            rigGo.transform.position = cameraTransform.position;
            rigGo.transform.rotation = cameraTransform.rotation;
            lightRig = rigGo.transform;

            // Primary focused beam
            GameObject spotGo = new GameObject("Flashlight_SpotBeam");
            spotGo.transform.SetParent(lightRig, false);
            spotGo.transform.localPosition = new Vector3(0.18f, -0.14f, 0.12f);
            spotLight = spotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = spotRange;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = innerSpotAngle;
            spotLight.intensity = spotIntensity;
            spotLight.color = beamColor;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.92f;
            spotLight.shadowBias = 0.03f;
            spotLight.shadowNormalBias = 0.25f;
            spotLight.renderMode = LightRenderMode.ForcePixel;
            spotLight.enabled = false;

            // Soft near-field bounce fill so damp tunnel walls near the player read clearly
            GameObject fillGo = new GameObject("Flashlight_NearBounce");
            fillGo.transform.SetParent(lightRig, false);
            fillGo.transform.localPosition = new Vector3(0.16f, -0.10f, 0.35f);
            fillBounceLight = fillGo.AddComponent<Light>();
            fillBounceLight.type = LightType.Point;
            fillBounceLight.range = 8.5f;
            fillBounceLight.intensity = 3.8f;
            fillBounceLight.color = new Color(0.96f, 0.93f, 0.85f);
            fillBounceLight.shadows = LightShadows.None;
            fillBounceLight.enabled = false;

            clickAudioSource = gameObject.AddComponent<AudioSource>();
            clickAudioSource.playOnAwake = false;
            clickAudioSource.spatialBlend = 0f;
            clickAudioSource.volume = 0.35f;
        }

        public void ToggleFlashlight()
        {
            ApplyLightState(!isFlashlightOn, playSound: true);
        }

        public void SetFlashlight(bool on, bool playSound = false)
        {
            ApplyLightState(on, playSound);
        }

        private void ApplyLightState(bool on, bool playSound)
        {
            isFlashlightOn = on;
            if (spotLight != null) spotLight.enabled = on;
            if (fillBounceLight != null) fillBounceLight.enabled = on;

            if (playSound && clickAudioSource != null)
            {
                AudioClip clip = on ? clickOnClip : clickOffClip;
                if (clip != null) clickAudioSource.PlayOneShot(clip, 0.40f);
            }
        }

        private void LateUpdate()
        {
            if (lightRig == null) return;
            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>() ?? Camera.main;
                if (cam != null) cameraTransform = cam.transform;
                else return;
            }

            // Smooth physical follow with slight handheld inertia
            lightRig.position = cameraTransform.position;
            float t = 1f - Mathf.Exp(-aimFollowSpeed * Time.deltaTime);
            lightRig.rotation = Quaternion.Slerp(lightRig.rotation, cameraTransform.rotation, t);
        }

        private void BuildProceduralClickClips()
        {
            clickOnClip = CreateClickClip("FlashlightClickOn", 1850f, 0.045f);
            clickOffClip = CreateClickClip("FlashlightClickOff", 1320f, 0.040f);
        }

        private static AudioClip CreateClickClip(string name, float freq, float duration)
        {
            int sr = 22050;
            int samples = Mathf.Max(64, Mathf.RoundToInt(sr * duration));
            float[] data = new float[samples];
            System.Random rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 85f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.45f;
                data[i] = (tone + noise) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
