using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MyVirtualCommunity.Client.Api
{
    /// <summary>
    /// Lightweight, non-blocking HTTP client for communicating with the
    /// authoritative MyVirtualCommunity Node.js server.
    /// Handles server diagnostics, spatial corridor data, official border wait times,
    /// synthetic witnesses, border concierge, and business attendants.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        [Header("Server Configuration")]
        [Tooltip("Base URL of the authoritative MyVirtualCommunity Node.js assistant server.")]
        [SerializeField]
        private string baseUrl = "http://127.0.0.1:3000";

        [Tooltip("Request timeout in seconds.")]
        [SerializeField]
        private int timeoutSeconds = 12;

        public string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = value?.TrimEnd('/') ?? "http://127.0.0.1:3000";
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "http://127.0.0.1:3000";
            }
            else
            {
                baseUrl = baseUrl.TrimEnd('/');
            }
        }

        #region Core & Diagnostics Endpoints

        /// <summary>
        /// Queries GET /health for authoritative server and model diagnostics.
        /// </summary>
        public void GetHealth(Action<HealthResponseDto> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendGetRequest<HealthResponseDto>("/health", onSuccess, onError));
        }

        /// <summary>
        /// Queries GET /api/spatial/corridor for authoritative corridor coordinates and entities.
        /// </summary>
        public void GetSpatialCorridor(Action<SpatialCorridorResponseDto> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendGetRequest<SpatialCorridorResponseDto>("/api/spatial/corridor", onSuccess, onError));
        }

        #endregion

        #region Border Intelligence Endpoints (Increment 1 & 5.3)

        /// <summary>
        /// Queries GET /api/border/wait-times for official CBP pedestrian wait times (PDN vs BOTA).
        /// When bypassCache is true, requests a live refresh from official upstream observations.
        /// </summary>
        public void GetBorderWaitTimes(bool bypassCache, Action<BorderWaitTimesResponseDto> onSuccess, Action<string> onError)
        {
            string endpoint = bypassCache ? "/api/border/wait-times?refresh=true" : "/api/border/wait-times";
            StartCoroutine(SendGetRequest<BorderWaitTimesResponseDto>(endpoint, dto =>
            {
                // Verify observations presence
                onSuccess?.Invoke(dto);
            }, onError, postProcessAction: (dto, rawJson) =>
            {
                PostProcessBorderWaitTimes(dto, rawJson);
            }));
        }

        private void PostProcessBorderWaitTimes(BorderWaitTimesResponseDto dto, string rawJson)
        {
            if (dto?.comparison?.observations == null || string.IsNullOrEmpty(rawJson)) return;

            // Check if pasoDelNorte delayMinutes was null in raw JSON
            int pdnIdx = rawJson.IndexOf("\"pasoDelNorte\"", StringComparison.OrdinalIgnoreCase);
            if (pdnIdx >= 0)
            {
                int endIdx = rawJson.IndexOf("\"bridgeOfTheAmericas\"", pdnIdx, StringComparison.OrdinalIgnoreCase);
                string pdnSlice = endIdx > pdnIdx ? rawJson.Substring(pdnIdx, endIdx - pdnIdx) : rawJson.Substring(pdnIdx);
                if (pdnSlice.Contains("\"delayMinutes\":null") || pdnSlice.Contains("\"delayMinutes\": null"))
                {
                    if (dto.comparison.observations.pasoDelNorte != null)
                    {
                        dto.comparison.observations.pasoDelNorte.hasDelayMinutes = false;
                    }
                }
            }

            // Check if bridgeOfTheAmericas delayMinutes was null in raw JSON
            int botaIdx = rawJson.IndexOf("\"bridgeOfTheAmericas\"", StringComparison.OrdinalIgnoreCase);
            if (botaIdx >= 0)
            {
                string botaSlice = rawJson.Substring(botaIdx);
                if (botaSlice.Contains("\"delayMinutes\":null") || botaSlice.Contains("\"delayMinutes\": null"))
                {
                    if (dto.comparison.observations.bridgeOfTheAmericas != null)
                    {
                        dto.comparison.observations.bridgeOfTheAmericas.hasDelayMinutes = false;
                    }
                }
            }
        }

        #endregion

        #region Conversational Endpoints (Increment 2, 3, 4, & 5.3)

        /// <summary>
        /// Calls POST /api/border/concierge/chat for grounded Elena Ramos traveler guidance.
        /// </summary>
        public void AskConcierge(AskConciergeRequestDto request, Action<AskConciergeResponseDto> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendPostRequest<AskConciergeRequestDto, AskConciergeResponseDto>(
                "/api/border/concierge/chat",
                request,
                onSuccess,
                onError
            ));
        }

        /// <summary>
        /// Calls POST /api/border/witnesses/chat for conversations with synthetic witnesses (Mateo, Sofia, Héctor).
        /// </summary>
        public void ChatWithWitness(WitnessChatRequestDto request, Action<WitnessChatResponseDto> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendPostRequest<WitnessChatRequestDto, WitnessChatResponseDto>(
                "/api/border/witnesses/chat",
                request,
                onSuccess,
                onError
            ));
        }

        /// <summary>
        /// Calls POST /api/businesses/attendants/chat for storefront attendants (Carlos Méndez, Valeria Domínguez).
        /// </summary>
        public void ChatWithAttendant(BusinessAttendantChatRequestDto request, Action<BusinessAttendantChatResponseDto> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendPostRequest<BusinessAttendantChatRequestDto, BusinessAttendantChatResponseDto>(
                "/api/businesses/attendants/chat",
                request,
                onSuccess,
                onError
            ));
        }

        #endregion

        #region HTTP Request Pipeline

        private IEnumerator SendGetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<string> onError,
            Action<T, string> postProcessAction = null)
        {
            string url = $"{BaseUrl}{endpoint}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                HandleResponse(request, endpoint, url, onSuccess, onError, postProcessAction);
            }
        }

        private IEnumerator SendPostRequest<TReq, TRes>(
            string endpoint,
            TReq requestPayload,
            Action<TRes> onSuccess,
            Action<string> onError,
            Action<TRes, string> postProcessAction = null)
        {
            string url = $"{BaseUrl}{endpoint}";
            string jsonBody = JsonUtility.ToJson(requestPayload);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                HandleResponse(request, endpoint, url, onSuccess, onError, postProcessAction);
            }
        }

        private void HandleResponse<T>(
            UnityWebRequest request,
            string endpoint,
            string url,
            Action<T> onSuccess,
            Action<string> onError,
            Action<T, string> postProcessAction)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                string msg = $"Connection failed to {url}: {request.error}. Ensure the Node.js server is running on {BaseUrl}.";
                Debug.LogWarning($"[ApiClient] {msg}");
                onError?.Invoke(msg);
                return;
            }

            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                string rawResponse = request.downloadHandler?.text ?? string.Empty;
                string errorDetail = ParseApiErrorMessage(rawResponse) ?? request.error;
                string msg = $"HTTP {(int)request.responseCode} ({request.responseCode}) from {endpoint}: {errorDetail}";
                Debug.LogWarning($"[ApiClient] {msg}");
                onError?.Invoke(msg);
                return;
            }

            if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                string msg = $"Data processing error for {url}: {request.error}";
                Debug.LogError($"[ApiClient] {msg}");
                onError?.Invoke(msg);
                return;
            }

            string json = request.downloadHandler?.text;
            if (string.IsNullOrEmpty(json))
            {
                string msg = $"Received empty response payload from {endpoint}.";
                Debug.LogWarning($"[ApiClient] {msg}");
                onError?.Invoke(msg);
                return;
            }

            T resultDto;
            try
            {
                resultDto = JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                string msg = $"Failed to parse JSON response from {endpoint}: {ex.Message}";
                Debug.LogError($"[ApiClient] {msg}\nRaw payload:\n{json}");
                onError?.Invoke(msg);
                return;
            }

            if (resultDto == null)
            {
                string msg = $"Deserialized payload was null for {endpoint}.";
                Debug.LogError($"[ApiClient] {msg}");
                onError?.Invoke(msg);
                return;
            }

            postProcessAction?.Invoke(resultDto, json);
            onSuccess?.Invoke(resultDto);
        }

        private static string ParseApiErrorMessage(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return null;
            try
            {
                var errObj = JsonUtility.FromJson<ApiErrorResponseDto>(rawJson);
                if (errObj != null && errObj.error != null && !string.IsNullOrEmpty(errObj.error.message))
                {
                    return $"[{errObj.error.code}] {errObj.error.message}";
                }
            }
            catch
            {
                // Fallback to standard HTTP status
            }
            return null;
        }

        #endregion
    }
}
