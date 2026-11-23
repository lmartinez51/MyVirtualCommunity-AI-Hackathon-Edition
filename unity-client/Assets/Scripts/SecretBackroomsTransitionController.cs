using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Orchestrates the non-Euclidean Backrooms Level 1 transition from the Building A South
    /// mysterious doorway:
    ///   - Shows the opaque Threshold Veil inside the doorway while the vault is available
    ///     (hiding the interior from anyone peeking from the sidewalk).
    ///   - When another player already occupies Backrooms Level 1 ("decoy_empty"), hides the
    ///     veil so newcomers only see the small, empty dusty room.
    ///   - When an authorized player walks completely past the Threshold Veil (X < triggerX),
    ///     slams & relocks the real exterior door behind them, claims occupancy on World Core,
    ///     additively loads "BackroomsLevel1Scene", switches lighting/fog to Level 0 fluorescent
    ///     ambience, and teleports the player seamlessly into the 14m x 14m liminal maze.
    ///   - Exiting via BackroomsExitDoorTrigger unloads "BackroomsLevel1Scene", restores the
    ///     corridor sky/sun/HUD, releases occupancy, and returns the player to Av. Benito Juárez.
    /// </summary>
    public class SecretBackroomsTransitionController : MonoBehaviour
    {
        public const string BackroomsSceneName = "BackroomsLevel1Scene";

        [Header("Scene References")]
        public SecretKeypadDoorController outerStreetDoor;
        public GameObject thresholdVeilObject;

        [Header("Threshold Trigger Bounds (BorderCorridorScene)")]
        public float veilTriggerX = -11.94f;
        public float roomMinZ = -13.50f;
        public float roomMaxZ = -11.15f;
        public Vector3 streetExitReturnPosition = new Vector3(-11.20f, 0.24f, -11.99f);
        public float streetExitReturnYaw = 90f; // Facing East toward Av. Benito Juárez

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool isPlayerInBackrooms = false;
        [SerializeField] private bool isTransitioning = false;
        [SerializeField] private bool occupiedByOtherPlayer = false;
        [SerializeField, Range(0f, 1f)] private float fadeAlpha = 0f;

        private ApiClient cachedApiClient;
        private CharacterController cachedPlayerCC;
        private Transform cachedPlayerTransform;
        private float nextStatusPollTime = 0f;

        // Saved outdoor lighting/fog/environment state restored on exit
        private readonly List<Light> suspendedOutdoorLights = new List<Light>();
        private Material savedSkybox;
        private Color savedAmbientLight;
        private UnityEngine.Rendering.AmbientMode savedAmbientMode;
        private bool savedFogEnabled;
        private Color savedFogColor;
        private float savedFogDensity;
        private FogMode savedFogMode;
        private bool hasSavedRenderState = false;
        private Texture2D fadeBlackTex;

        public bool IsPlayerInBackrooms => isPlayerInBackrooms;
        public bool IsTransitioning => isTransitioning;
        public bool IsOccupiedByOtherPlayer => occupiedByOtherPlayer;
        public float TransitionFadeAlpha => fadeAlpha;

        private void Start()
        {
            if (outerStreetDoor == null)
            {
                GameObject doorGo = GameObject.Find("BuildingA_SouthBay_StorageDoor");
                if (doorGo != null) outerStreetDoor = doorGo.GetComponent<SecretKeypadDoorController>();
            }
            cachedApiClient = FindFirstObjectByType<ApiClient>();
            RefreshThresholdVeilVisibility();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (cachedPlayerTransform == null)
            {
                CharacterController cc = FindFirstObjectByType<CharacterController>();
                if (cc != null)
                {
                    cachedPlayerCC = cc;
                    cachedPlayerTransform = cc.transform;
                }
            }

            // Periodically poll World Core occupancy while outside near the street block
            if (!isPlayerInBackrooms && !isTransitioning && Time.unscaledTime >= nextStatusPollTime)
            {
                nextStatusPollTime = Time.unscaledTime + 3.0f;
                PollVaultOccupancyStatus();
            }

            RefreshThresholdVeilVisibility();

            // Trigger seamless entry into Backrooms Level 1 as soon as player steps into the open Threshold Veil
            if (!isPlayerInBackrooms && !isTransitioning && !occupiedByOtherPlayer && cachedPlayerTransform != null)
            {
                Vector3 p = cachedPlayerTransform.position;
                bool doorIsOpen = outerStreetDoor == null || (outerStreetDoor.IsOpen && outerStreetDoor.OpenProgress > 0.55f);
                if (doorIsOpen && p.x < veilTriggerX && p.z >= roomMinZ && p.z <= roomMaxZ && p.y > -5f)
                {
                    StartCoroutine(EnterBackroomsRoutine());
                }
            }
        }

        private void OnGUI()
        {
            if (fadeAlpha <= 0.01f) return;
            if (fadeBlackTex == null)
            {
                fadeBlackTex = new Texture2D(1, 1);
                fadeBlackTex.SetPixel(0, 0, Color.black);
                fadeBlackTex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.03f, fadeAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeBlackTex);
            GUI.color = prev;
        }

        private void OnDestroy()
        {
            if (isPlayerInBackrooms && cachedApiClient != null)
            {
                cachedApiClient.LeaveSecretVault("player-1");
            }
            if (fadeBlackTex != null)
            {
                Destroy(fadeBlackTex);
                fadeBlackTex = null;
            }
        }

        public void SetOccupiedByOtherForTest(bool occupied)
        {
            occupiedByOtherPlayer = occupied;
            RefreshThresholdVeilVisibility();
        }

        private void PollVaultOccupancyStatus()
        {
            if (cachedApiClient == null)
            {
                cachedApiClient = FindFirstObjectByType<ApiClient>();
            }
            if (cachedApiClient == null) return;

            cachedApiClient.GetSecretVaultStatus(
                "player-1",
                res =>
                {
                    if (res != null && res.success)
                    {
                        occupiedByOtherPlayer = res.occupiedByOther || res.modeForPlayer == "decoy_empty";
                        RefreshThresholdVeilVisibility();
                    }
                },
                _ =>
                {
                    // Offline fallback: solo player can always enter Backrooms
                    occupiedByOtherPlayer = false;
                    RefreshThresholdVeilVisibility();
                });
        }

        private void RefreshThresholdVeilVisibility()
        {
            if (thresholdVeilObject == null) return;
            bool shouldShowVeil = !isPlayerInBackrooms && !occupiedByOtherPlayer;
            if (thresholdVeilObject.activeSelf != shouldShowVeil)
            {
                thresholdVeilObject.SetActive(shouldShowVeil);
            }
        }

        public void RequestExitBackrooms()
        {
            if (!isPlayerInBackrooms || isTransitioning) return;
            StartCoroutine(ExitBackroomsRoutine());
        }

        private IEnumerator EnterBackroomsRoutine()
        {
            isTransitioning = true;

            // 1. Slam & relock the real exterior door behind the player immediately
            if (outerStreetDoor != null)
            {
                outerStreetDoor.CloseAndRelock();
            }

            // 2. Claim authoritative occupancy in World Core (graceful offline fallback if unreachable)
            bool occupancyResolved = false;
            bool granted = true;
            if (cachedApiClient != null)
            {
                cachedApiClient.EnterSecretVault(
                    "player-1",
                    res =>
                    {
                        if (res != null && res.success)
                        {
                            granted = res.granted && res.modeForPlayer != "decoy_empty";
                            occupiedByOtherPlayer = !granted;
                        }
                        occupancyResolved = true;
                    },
                    _ =>
                    {
                        granted = true;
                        occupancyResolved = true;
                    });
                float waitLimit = Time.unscaledTime + 0.65f;
                while (!occupancyResolved && Time.unscaledTime < waitLimit)
                {
                    yield return null;
                }
            }

            if (!granted)
            {
                // Another player claimed the vault at the exact same moment -> stay in empty decoy room
                RefreshThresholdVeilVisibility();
                isTransitioning = false;
                yield break;
            }

            // 3. Quick claustrophobic blackout fade-in (0.28s)
            yield return FadeTo(1f, 0.28f);

            // 4. Additively load BackroomsLevel1Scene
            Scene backroomsScene = SceneManager.GetSceneByName(BackroomsSceneName);
            if (!backroomsScene.isLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(BackroomsSceneName, LoadSceneMode.Additive);
                if (loadOp != null)
                {
                    while (!loadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // 5. Mark Backrooms mode active BEFORE teleporting so void-fall & street reconciliation guards never race
            isPlayerInBackrooms = true;
            RefreshThresholdVeilVisibility();

            // 6. Switch lighting & fog to classic Backrooms Level 0 fluorescent liminal atmosphere
            ApplyBackroomsAtmosphere();

            // 7. Teleport player smoothly to Backrooms Arrival SpawnPoint
            GameObject spawnMarker = GameObject.Find("Backrooms_SpawnPoint");
            Vector3 targetPos = spawnMarker != null ? spawnMarker.transform.position : new Vector3(0f, -49.92f, -5.85f);
            Quaternion targetRot = spawnMarker != null ? spawnMarker.transform.rotation : Quaternion.identity;
            TeleportPlayer(targetPos, targetRot);

            yield return new WaitForSecondsRealtime(0.10f);
            yield return FadeTo(0f, 0.35f);
            isTransitioning = false;
        }

        private IEnumerator ExitBackroomsRoutine()
        {
            isTransitioning = true;

            // 1. Fade to black
            yield return FadeTo(1f, 0.26f);

            // 2. Restore corridor sky/sun/fog & teleport player cleanly back onto Av. Benito Juárez sidewalk
            RestoreOutdoorAtmosphere();
            TeleportPlayer(streetExitReturnPosition, Quaternion.Euler(0f, streetExitReturnYaw, 0f));

            // Keep exterior door locked behind player
            if (outerStreetDoor != null)
            {
                outerStreetDoor.CloseAndRelock();
            }

            // 3. Unload BackroomsLevel1Scene to free memory/GPU immediately
            Scene backroomsScene = SceneManager.GetSceneByName(BackroomsSceneName);
            if (backroomsScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(backroomsScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // 4. Release authoritative occupancy on World Core
            if (cachedApiClient != null)
            {
                cachedApiClient.LeaveSecretVault("player-1");
            }

            isPlayerInBackrooms = false;
            occupiedByOtherPlayer = false;
            RefreshThresholdVeilVisibility();

            yield return new WaitForSecondsRealtime(0.08f);
            yield return FadeTo(0f, 0.32f);
            isTransitioning = false;
        }

        private void TeleportPlayer(Vector3 worldPos, Quaternion worldRot)
        {
            if (cachedPlayerTransform == null) return;
            if (cachedPlayerCC != null) cachedPlayerCC.enabled = false;
            cachedPlayerTransform.position = worldPos;
            cachedPlayerTransform.rotation = worldRot;
            Physics.SyncTransforms();
            if (cachedPlayerCC != null) cachedPlayerCC.enabled = true;
        }

        private void ApplyBackroomsAtmosphere()
        {
            if (!hasSavedRenderState)
            {
                savedSkybox = RenderSettings.skybox;
                savedAmbientLight = RenderSettings.ambientLight;
                savedAmbientMode = RenderSettings.ambientMode;
                savedFogEnabled = RenderSettings.fog;
                savedFogColor = RenderSettings.fogColor;
                savedFogDensity = RenderSettings.fogDensity;
                savedFogMode = RenderSettings.fogMode;
                hasSavedRenderState = true;
            }

            suspendedOutdoorLights.Clear();
            Scene mainScene = gameObject.scene;
            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l != null && l.gameObject.scene == mainScene && l.type == LightType.Directional && l.enabled)
                {
                    l.enabled = false;
                    suspendedOutdoorLights.Add(l);
                }
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.33f, 0.21f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.22f, 0.20f, 0.11f);
            RenderSettings.fogDensity = 0.045f;
        }

        private void RestoreOutdoorAtmosphere()
        {
            foreach (Light l in suspendedOutdoorLights)
            {
                if (l != null) l.enabled = true;
            }
            suspendedOutdoorLights.Clear();

            if (hasSavedRenderState)
            {
                RenderSettings.skybox = savedSkybox;
                RenderSettings.ambientMode = savedAmbientMode;
                RenderSettings.ambientLight = savedAmbientLight;
                RenderSettings.fog = savedFogEnabled;
                RenderSettings.fogColor = savedFogColor;
                RenderSettings.fogDensity = savedFogDensity;
                RenderSettings.fogMode = savedFogMode;
                hasSavedRenderState = false;
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float start = fadeAlpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeAlpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            fadeAlpha = targetAlpha;
        }
    }
}
