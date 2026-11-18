using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Controls the central LED billboard screen on the Avenida Benito Juárez Gateway Arch.
    ///
    /// Hybrid Authoritative Architecture:
    /// 1. Polls `GET /api/world/arch-playlist` in the background and pre-downloads active
    ///    sponsor/event `.mp4` videos into `Application.persistentDataPath/ArchCache/` with
    ///    atomic `.part` writes, byte-size verification, `.sha256` tracking, and automatic
    ///    cleanup of retired videos — zero Unity rebuild or player update needed.
    /// 2. Falls back seamlessly to local clips in `Application.streamingAssetsPath/ArchMedia/`
    ///    (and built-in promo textures) if offline or when the backend playlist is empty.
    /// 3. Plays clips from 10s (short loops) up to 45s full-length, alternating a concise 7s pause
    ///    between videos (Live Official Clock/Weather/PDN Board <-> Cultural Promo Slide), while
    ///    keeping the top telemetry ribbon permanently live.
    /// </summary>
    public class JuarezArchDisplayController : MonoBehaviour
    {
        public enum DisplayMode
        {
            VideoClip,
            WeatherAndClockBoard,
            PromoSlide
        }

        [Header("Core Dependencies")]
        [SerializeField] private ApiClient apiClient;
        [SerializeField] private TimeOfDayEnvironmentController timeController;

        [Header("World-Space Canvas Elements")]
        [SerializeField] private RawImage mainMediaRawImage;
        [SerializeField] private GameObject infoDashboardPanel;
        [SerializeField] private Text tickerTimeText;
        [SerializeField] private Text tickerWeatherText;
        [SerializeField] private Text tickerBorderText;
        [SerializeField] private Text boardClockText;
        [SerializeField] private Text boardPhaseDateText;
        [SerializeField] private Text boardTempText;
        [SerializeField] private Text boardWeatherDetailText;
        [SerializeField] private Text boardBorderStatusText;
        [SerializeField] private Text boardEventFooterText;

        [Header("Built-in Fallback / Event Promo Textures")]
        [SerializeField] private List<Texture2D> builtInPromoTextures = new List<Texture2D>();

        [Header("Playback & Refresh Settings")]
        [SerializeField] private float minVideoDurationSeconds = 10.0f;
        [SerializeField] private float maxVideoDurationSeconds = 45.0f;
        [SerializeField] private float videoSlideDurationSeconds = 16.0f;
        [SerializeField] private float infoSlideDurationSeconds = 7.0f;
        [SerializeField] private float promoSlideDurationSeconds = 7.0f;
        [SerializeField] private float weatherPollIntervalSeconds = 120.0f;
        [SerializeField] private float borderPollIntervalSeconds = 60.0f;
        [SerializeField] private float playlistPollIntervalSeconds = 120.0f;

        private readonly List<string> localFallbackVideoPaths = new List<string>();
        private readonly List<string> syncedBackendVideoPaths = new List<string>();
        private readonly List<Texture2D> loadedCustomImageSlides = new List<Texture2D>();

        private VideoPlayer videoPlayer;
        private RenderTexture videoRenderTexture;

        private DisplayMode currentMode = DisplayMode.PromoSlide;
        private float currentSlideElapsed = 0f;
        private float currentSlideTargetDuration = 7.0f;
        private int activeVideoCursor = 0;
        private int activePromoCursor = 0;
        private int playlistStepCounter = 0;
        private bool hasStartedVideoPlayback = false;
        private string activeVideoPath = "";
        private string lastSyncedPlaylistSignature = "";
        private bool isSyncingPlaylist = false;

        // Authoritative polled backend state
        private WorldWeatherObservationDto latestWeather = null;
        private BorderCrossingObservationDto latestPdnObservation = null;
        private float lastWeatherPollTime = -999f;
        private float lastBorderPollTime = -999f;
        private float lastPlaylistPollTime = -999f;

        public string MediaDirectoryPath => Path.Combine(Application.streamingAssetsPath, "ArchMedia");
        public string ArchCacheDirectoryPath => Path.Combine(Application.persistentDataPath, "ArchCache");
        public IReadOnlyList<string> DiscoveredVideoPaths => GetActiveVideoList();
        public IReadOnlyList<string> SyncedBackendVideoPaths => syncedBackendVideoPaths;
        public IReadOnlyList<string> LocalFallbackVideoPaths => localFallbackVideoPaths;
        public bool UsingBackendPlaylist => syncedBackendVideoPaths.Count > 0;
        public float MinVideoDurationSeconds => minVideoDurationSeconds;
        public float MaxVideoDurationSeconds => maxVideoDurationSeconds;
        public float InfoSlideDurationSeconds => infoSlideDurationSeconds;
        public float PromoSlideDurationSeconds => promoSlideDurationSeconds;
        public DisplayMode CurrentDisplayMode => currentMode;
        public string ActiveVideoPath => activeVideoPath;
        public WorldWeatherObservationDto LatestWeather => latestWeather;

        public void AssignUiReferences(
            RawImage mediaRawImage,
            GameObject dashboardPanel,
            Text tickerTime,
            Text tickerWeather,
            Text tickerBorder,
            Text boardClock,
            Text boardPhaseDate,
            Text boardTemp,
            Text boardWeatherDetail,
            Text boardBorderStatus,
            Text boardEventFooter,
            List<Texture2D> promoTextures)
        {
            mainMediaRawImage = mediaRawImage;
            infoDashboardPanel = dashboardPanel;
            tickerTimeText = tickerTime;
            tickerWeatherText = tickerWeather;
            tickerBorderText = tickerBorder;
            boardClockText = boardClock;
            boardPhaseDateText = boardPhaseDate;
            boardTempText = boardTemp;
            boardWeatherDetailText = boardWeatherDetail;
            boardBorderStatusText = boardBorderStatus;
            boardEventFooterText = boardEventFooter;
            builtInPromoTextures = promoTextures ?? new List<Texture2D>();
        }

        private void Awake()
        {
            EnsureVideoResources();
        }

        private void Start()
        {
            if (apiClient == null)
            {
                apiClient = FindAnyObjectByType<ApiClient>();
            }
            if (timeController == null)
            {
                timeController = FindAnyObjectByType<TimeOfDayEnvironmentController>();
            }

            DiscoverMediaFiles();
            InitializeFirstSlide();

            if (Application.isPlaying)
            {
                StartCoroutine(PollLiveTelemetryLoop());
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
                videoRenderTexture = null;
            }
            foreach (var tex in loadedCustomImageSlides)
            {
                if (tex != null) Destroy(tex);
            }
            loadedCustomImageSlides.Clear();
        }

        private void EnsureVideoResources()
        {
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
                {
                    name = "JuarezArch_VideoRT",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                videoRenderTexture.Create();
            }

            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = videoRenderTexture;
                videoPlayer.isLooping = true;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
                videoPlayer.errorReceived += OnVideoPlayerError;
                videoPlayer.prepareCompleted += OnVideoPrepared;
            }
        }

        private IReadOnlyList<string> GetActiveVideoList()
        {
            return syncedBackendVideoPaths.Count > 0
                ? syncedBackendVideoPaths
                : localFallbackVideoPaths;
        }

        public void DiscoverMediaFiles()
        {
            localFallbackVideoPaths.Clear();

            string mediaDir = MediaDirectoryPath;
            if (!Directory.Exists(mediaDir))
            {
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(mediaDir);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file);
                    if (IsAllowedVideoExtension(ext))
                    {
                        localFallbackVideoPaths.Add(file);
                    }
                    else if (Application.isPlaying &&
                             (string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase)))
                    {
                        byte[] bytes = File.ReadAllBytes(file);
                        Texture2D customTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (customTex.LoadImage(bytes))
                        {
                            customTex.name = Path.GetFileNameWithoutExtension(file);
                            loadedCustomImageSlides.Add(customTex);
                        }
                        else
                        {
                            Destroy(customTex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JuarezArchDisplay] Failed scanning ArchMedia folder '{mediaDir}': {ex.Message}");
            }
        }

        private static bool IsAllowedVideoExtension(string ext)
        {
            return string.Equals(ext, ".mp4", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".mov", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeFirstSlide()
        {
            playlistStepCounter = 0;
            activeVideoCursor = 0;
            activePromoCursor = 0;

            var videos = GetActiveVideoList();
            if (videos.Count > 0)
            {
                ActivateVideoSlide(videos[0]);
            }
            else if (GetPromoCount() > 0)
            {
                ActivatePromoSlide();
            }
            else
            {
                ActivateWeatherClockBoard();
            }

            RefreshTelemetryLabels();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            currentSlideElapsed += Time.deltaTime;
            if (currentSlideElapsed >= currentSlideTargetDuration)
            {
                AdvancePlaylist();
            }

            RefreshTelemetryLabels();
        }

        public void AdvancePlaylist()
        {
            playlistStepCounter++;

            var videos = GetActiveVideoList();
            bool hasVideos = videos.Count > 0;
            bool hasPromos = GetPromoCount() > 0;

            // Dynamic Shorts/Reels rhythm:
            // Video 1 -> Weather/Clock Board (7s) -> Video 2 -> Promo Slide (7s) -> Video 3 -> ...
            if (hasVideos)
            {
                if (playlistStepCounter % 2 == 0)
                {
                    activeVideoCursor = (activeVideoCursor + 1) % videos.Count;
                    ActivateVideoSlide(videos[activeVideoCursor]);
                }
                else
                {
                    int pauseTurn = (playlistStepCounter / 2) % 2;
                    if (pauseTurn == 0 || !hasPromos)
                    {
                        ActivateWeatherClockBoard();
                    }
                    else
                    {
                        activePromoCursor = (activePromoCursor + 1) % GetPromoCount();
                        ActivatePromoSlide();
                    }
                }
            }
            else
            {
                int slot = playlistStepCounter % 2;
                if (slot == 0 && hasPromos)
                {
                    activePromoCursor = (activePromoCursor + 1) % GetPromoCount();
                    ActivatePromoSlide();
                }
                else
                {
                    ActivateWeatherClockBoard();
                }
            }
        }

        private int GetPromoCount() => builtInPromoTextures.Count + loadedCustomImageSlides.Count;

        private Texture2D GetPromoAt(int index)
        {
            int customCount = loadedCustomImageSlides.Count;
            if (index < customCount)
            {
                return loadedCustomImageSlides[index];
            }
            int builtInIdx = index - customCount;
            if (builtInIdx >= 0 && builtInIdx < builtInPromoTextures.Count)
            {
                return builtInPromoTextures[builtInIdx];
            }
            return null;
        }

        private void ActivateVideoSlide(string filePath)
        {
            EnsureVideoResources();
            currentMode = DisplayMode.VideoClip;
            currentSlideElapsed = 0f;
            currentSlideTargetDuration = videoSlideDurationSeconds;
            activeVideoPath = filePath;
            hasStartedVideoPlayback = false;

            if (infoDashboardPanel != null) infoDashboardPanel.SetActive(false);
            if (mainMediaRawImage != null)
            {
                mainMediaRawImage.gameObject.SetActive(true);
                mainMediaRawImage.texture = videoRenderTexture;
                mainMediaRawImage.color = Color.white;
            }

            if (videoPlayer != null && !string.IsNullOrEmpty(filePath))
            {
                videoPlayer.Stop();
                videoPlayer.url = new Uri(filePath).AbsoluteUri;
                videoPlayer.Prepare();
            }
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            if (currentMode != DisplayMode.VideoClip) return;
            hasStartedVideoPlayback = true;
            if (source.length > 0.5)
            {
                // Play short loops at least 10s; play clips up to 45s in full; cap long clips cleanly at 45s.
                currentSlideTargetDuration = Mathf.Clamp(
                    (float)source.length,
                    minVideoDurationSeconds,
                    maxVideoDurationSeconds);
            }
            source.Play();
        }

        private void OnVideoPlayerError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"[JuarezArchDisplay] Video playback fallback ({activeVideoPath}): {message}");
            if (!hasStartedVideoPlayback && currentMode == DisplayMode.VideoClip)
            {
                AdvancePlaylist();
            }
        }

        public void ActivateWeatherClockBoard()
        {
            currentMode = DisplayMode.WeatherAndClockBoard;
            currentSlideElapsed = 0f;
            currentSlideTargetDuration = infoSlideDurationSeconds;
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            if (mainMediaRawImage != null) mainMediaRawImage.gameObject.SetActive(false);
            if (infoDashboardPanel != null) infoDashboardPanel.SetActive(true);
            RefreshTelemetryLabels();
        }

        public void ActivatePromoSlide()
        {
            Texture2D tex = GetPromoAt(activePromoCursor);
            if (tex == null)
            {
                ActivateWeatherClockBoard();
                return;
            }

            currentMode = DisplayMode.PromoSlide;
            currentSlideElapsed = 0f;
            currentSlideTargetDuration = promoSlideDurationSeconds;
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            if (infoDashboardPanel != null) infoDashboardPanel.SetActive(false);
            if (mainMediaRawImage != null)
            {
                mainMediaRawImage.gameObject.SetActive(true);
                mainMediaRawImage.texture = tex;
                mainMediaRawImage.color = Color.white;
            }
        }

        private IEnumerator PollLiveTelemetryLoop()
        {
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                if (apiClient == null)
                {
                    apiClient = FindAnyObjectByType<ApiClient>();
                }
                if (timeController == null)
                {
                    timeController = FindAnyObjectByType<TimeOfDayEnvironmentController>();
                }

                if (apiClient != null)
                {
                    if (Time.realtimeSinceStartup - lastWeatherPollTime >= weatherPollIntervalSeconds || latestWeather == null)
                    {
                        lastWeatherPollTime = Time.realtimeSinceStartup;
                        apiClient.GetWorldWeather(
                            bypassCache: false,
                            onSuccess: dto =>
                            {
                                if (dto != null && dto.success && dto.weather != null)
                                {
                                    latestWeather = dto.weather;
                                    RefreshTelemetryLabels();
                                }
                            },
                            onError: _ => { }
                        );
                    }

                    if (Time.realtimeSinceStartup - lastBorderPollTime >= borderPollIntervalSeconds || latestPdnObservation == null)
                    {
                        lastBorderPollTime = Time.realtimeSinceStartup;
                        apiClient.GetBorderWaitTimes(
                            bypassCache: false,
                            onSuccess: dto =>
                            {
                                if (dto?.comparison?.observations?.pasoDelNorte != null)
                                {
                                    latestPdnObservation = dto.comparison.observations.pasoDelNorte;
                                    RefreshTelemetryLabels();
                                }
                            },
                            onError: _ => { }
                        );
                    }

                    if (!isSyncingPlaylist &&
                        (Time.realtimeSinceStartup - lastPlaylistPollTime >= playlistPollIntervalSeconds || lastPlaylistPollTime < 0f))
                    {
                        lastPlaylistPollTime = Time.realtimeSinceStartup;
                        StartCoroutine(SyncBackendPlaylistCoroutine());
                    }
                }

                yield return new WaitForSeconds(5.0f);
            }
        }

        private IEnumerator SyncBackendPlaylistCoroutine()
        {
            if (apiClient == null) yield break;
            isSyncingPlaylist = true;

            ArchPlaylistResponseDto received = null;
            bool done = false;

            apiClient.GetArchPlaylist(
                dto => { received = dto; done = true; },
                _ => { done = true; }
            );

            while (!done) yield return null;

            if (received == null || !received.success)
            {
                isSyncingPlaylist = false;
                yield break;
            }

            if (received.pollRecommendedSeconds >= 15)
            {
                playlistPollIntervalSeconds = received.pollRecommendedSeconds;
            }

            string cacheDir = ArchCacheDirectoryPath;
            try
            {
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JuarezArchDisplay] Failed creating ArchCache dir '{cacheDir}': {ex.Message}");
                isSyncingPlaylist = false;
                yield break;
            }

            var items = received.items ?? Array.Empty<ArchPlaylistItemDto>();
            var readyCachePaths = new List<string>();
            var expectedCacheFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.fileName) || string.IsNullOrEmpty(item.url))
                {
                    continue;
                }

                string safeName = Path.GetFileName(item.fileName);
                if (!IsAllowedVideoExtension(Path.GetExtension(safeName)))
                {
                    continue;
                }

                expectedCacheFiles.Add(safeName);
                expectedCacheFiles.Add(safeName + ".sha256");

                string localTarget = Path.Combine(cacheDir, safeName);
                string hashSidecar = localTarget + ".sha256";

                bool isCacheValid = IsCachedVideoUpToDate(localTarget, hashSidecar, item);
                if (!isCacheValid)
                {
                    string partPath = localTarget + ".part";
                    string downloadUrl = apiClient.ResolveUrl(item.url);
                    using (UnityWebRequest uwr = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET))
                    {
                        uwr.downloadHandler = new DownloadHandlerFile(partPath) { removeFileOnAbort = true };
                        uwr.timeout = 60;
                        yield return uwr.SendWebRequest();

                        if (uwr.result == UnityWebRequest.Result.Success && File.Exists(partPath))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(partPath);
                                if (fi.Length > 0 && (item.sizeBytes <= 0 || fi.Length == item.sizeBytes))
                                {
                                    if (File.Exists(localTarget)) File.Delete(localTarget);
                                    File.Move(partPath, localTarget);
                                    if (!string.IsNullOrEmpty(item.sha256))
                                    {
                                        File.WriteAllText(hashSidecar, item.sha256);
                                    }
                                    isCacheValid = true;
                                }
                                else if (File.Exists(partPath))
                                {
                                    File.Delete(partPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[JuarezArchDisplay] Failed finalizing cached clip '{safeName}': {ex.Message}");
                                if (File.Exists(partPath)) File.Delete(partPath);
                            }
                        }
                    }
                }

                if (isCacheValid && File.Exists(localTarget))
                {
                    readyCachePaths.Add(localTarget);
                }
            }

            CleanupRetiredCacheFiles(cacheDir, expectedCacheFiles);
            ApplySyncedBackendPlaylist(readyCachePaths);
            isSyncingPlaylist = false;
        }

        private static bool IsCachedVideoUpToDate(string localPath, string sidecarPath, ArchPlaylistItemDto item)
        {
            try
            {
                if (!File.Exists(localPath)) return false;
                var fi = new FileInfo(localPath);
                if (fi.Length <= 0) return false;
                if (item.sizeBytes > 0 && fi.Length != item.sizeBytes) return false;

                if (!string.IsNullOrEmpty(item.sha256))
                {
                    if (!File.Exists(sidecarPath)) return false;
                    string savedSha = File.ReadAllText(sidecarPath).Trim();
                    return string.Equals(savedSha, item.sha256, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CleanupRetiredCacheFiles(string cacheDir, HashSet<string> expectedNames)
        {
            try
            {
                foreach (string file in Directory.GetFiles(cacheDir))
                {
                    string name = Path.GetFileName(file);
                    if (expectedNames.Contains(name)) continue;
                    // Never delete currently playing active file mid-stream
                    if (!string.IsNullOrEmpty(activeVideoPath) &&
                        string.Equals(Path.GetFullPath(file), Path.GetFullPath(activeVideoPath), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JuarezArchDisplay] Failed pruning ArchCache: {ex.Message}");
            }
        }

        public void ApplySyncedBackendPlaylist(IReadOnlyList<string> newCachedPaths)
        {
            syncedBackendVideoPaths.Clear();
            if (newCachedPaths != null)
            {
                foreach (string p in newCachedPaths)
                {
                    if (!string.IsNullOrEmpty(p)) syncedBackendVideoPaths.Add(p);
                }
            }

            string signature = string.Join("|", syncedBackendVideoPaths);
            if (signature == lastSyncedPlaylistSignature)
            {
                return;
            }

            lastSyncedPlaylistSignature = signature;
            if (syncedBackendVideoPaths.Count > 0)
            {
                activeVideoCursor = Mathf.Clamp(activeVideoCursor, 0, syncedBackendVideoPaths.Count - 1);
                // If screen was idle on Promo/Board fallback because zero local videos existed,
                // bring the newly downloaded sponsor video on air immediately.
                if (currentMode != DisplayMode.VideoClip && localFallbackVideoPaths.Count == 0 && Application.isPlaying)
                {
                    activeVideoCursor = 0;
                    ActivateVideoSlide(syncedBackendVideoPaths[0]);
                }
            }
        }

        public void ApplyAuthoritativeWeather(WorldWeatherObservationDto weather)
        {
            latestWeather = weather;
            RefreshTelemetryLabels();
        }

        public void ApplyAuthoritativeBorderObservation(BorderCrossingObservationDto pdn)
        {
            latestPdnObservation = pdn;
            RefreshTelemetryLabels();
        }

        public void RefreshTelemetryLabels()
        {
            // 1. Authoritative World Time (never DateTime.Now)
            string clockStr = "--:--";
            string phaseStr = "CENTRO HISTÓRICO";
            string dateStr = "";
            if (timeController != null && timeController.TimeState != AuthoritativeTimeState.Unknown)
            {
                clockStr = timeController.LocalCivilTimeFormatted;
                phaseStr = !string.IsNullOrEmpty(timeController.CurrentPhaseTitleEs)
                    ? timeController.CurrentPhaseTitleEs.ToUpperInvariant()
                    : "CENTRO HISTÓRICO";
                if (!string.IsNullOrEmpty(timeController.LocalCivilTimeIso) && timeController.LocalCivilTimeIso.Length >= 10)
                {
                    dateStr = timeController.LocalCivilTimeIso.Substring(0, 10);
                }
            }

            // 2. Authoritative Weather
            string tempShort = "--°C";
            string condEs = "Clima no disponible";
            string weatherFullDetail = "Conectando con observatorio meteorológico...";
            if (latestWeather != null)
            {
                if (latestWeather.status == "available" && latestWeather.hasTemperature)
                {
                    float tempC = latestWeather.temperatureCelsius;
                    int tempF = Mathf.RoundToInt(tempC * 9f / 5f + 32f);
                    condEs = latestWeather.conditionLabel != null && !string.IsNullOrEmpty(latestWeather.conditionLabel.es)
                        ? latestWeather.conditionLabel.es
                        : "Disponible";
                    tempShort = $"{Mathf.RoundToInt(tempC)}°C / {tempF}°F";
                    string hum = latestWeather.hasHumidity ? $"Humedad {Mathf.RoundToInt(latestWeather.relativeHumidityPercent)}%" : "";
                    string wind = latestWeather.hasWindSpeed ? $"Viento {Mathf.RoundToInt(latestWeather.windSpeedKmh)} km/h" : "";
                    string sep = (!string.IsNullOrEmpty(hum) && !string.IsNullOrEmpty(wind)) ? "  •  " : "";
                    weatherFullDetail = $"{condEs}  •  {hum}{sep}{wind}";
                }
                else
                {
                    tempShort = "--°C";
                    condEs = "Sin señal";
                    weatherFullDetail = "Datos climáticos no disponibles sin conexión";
                }
            }

            // 3. Paso del Norte Border Crossing status
            string pdnTicker = "PDN: -- min";
            string pdnBoard = "PUENTE PASO DEL NORTE: Consultando CBP...";
            if (latestPdnObservation != null)
            {
                if (latestPdnObservation.operationalStatus == "unavailable" ||
                    latestPdnObservation.freshness == "unavailable" ||
                    latestPdnObservation.portStatus == "Unknown")
                {
                    pdnTicker = "PDN: N/D";
                    pdnBoard = "PUENTE PASO DEL NORTE: Sin observación CBP vigente";
                }
                else if (latestPdnObservation.operationalStatus == "closed")
                {
                    pdnTicker = "PDN: CERRADO";
                    pdnBoard = "PUENTE PASO DEL NORTE: CERRADO";
                }
                else if (latestPdnObservation.hasDelayMinutes)
                {
                    pdnTicker = $"PDN: {latestPdnObservation.delayMinutes} min";
                    pdnBoard = $"PUENTE PASO DEL NORTE (Estándar): {latestPdnObservation.delayMinutes} min espera";
                }
                else
                {
                    pdnTicker = "PDN: ABIERTO";
                    pdnBoard = "PUENTE PASO DEL NORTE (Estándar): Abierto (sin demora)";
                }
            }

            if (tickerTimeText != null) tickerTimeText.text = $"HORA {clockStr}";
            if (tickerWeatherText != null) tickerWeatherText.text = $"{tempShort} • {condEs}";
            if (tickerBorderText != null) tickerBorderText.text = pdnTicker;

            if (boardClockText != null) boardClockText.text = clockStr;
            if (boardPhaseDateText != null)
            {
                boardPhaseDateText.text = string.IsNullOrEmpty(dateStr)
                    ? $"CD. JUÁREZ  •  {phaseStr}"
                    : $"CD. JUÁREZ  •  {dateStr}  •  {phaseStr}";
            }
            if (boardTempText != null) boardTempText.text = tempShort;
            if (boardWeatherDetailText != null) boardWeatherDetailText.text = weatherFullDetail;
            if (boardBorderStatusText != null) boardBorderStatusText.text = pdnBoard;
            if (boardEventFooterText != null)
            {
                boardEventFooterText.text = GetActiveVideoList().Count > 0
                    ? "CARTELERA CENTRO HISTÓRICO  •  15 km/h MÁX."
                    : "BIENVENIDOS AL CENTRO HISTÓRICO  •  15 km/h MÁX.";
            }
        }
    }
}
