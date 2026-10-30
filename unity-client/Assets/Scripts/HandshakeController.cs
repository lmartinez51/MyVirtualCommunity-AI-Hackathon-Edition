using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyVirtualCommunity.Client.Api
{
    public enum ConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Controls the initial Increment 5.1 foundation scene, demonstrating
    /// end-to-end API communication with the Node.js assistant server.
    /// </summary>
    [RequireComponent(typeof(ApiClient))]
    public class HandshakeController : MonoBehaviour
    {
        private ApiClient apiClient;

        [Header("State")]
        [SerializeField]
        private ConnectionState state = ConnectionState.Idle;

        [Header("Scene Visualization")]
        [Tooltip("Parent transform to hold spawned entity landmark markers.")]
        [SerializeField]
        private Transform markersRoot;

        private HealthResponseDto lastHealth;
        private SpatialCorridorResponseDto lastCorridor;
        private string lastErrorMessage = string.Empty;
        private float lastHandshakeLatencyMs = 0f;
        private string lastStatusTimestamp = string.Empty;

        private readonly List<GameObject> spawnedMarkers = new List<GameObject>();

        private Vector2 scrollPosition = Vector2.zero;

        private void Awake()
        {
            apiClient = GetComponent<ApiClient>();
        }

        private void Start()
        {
            // Execute automated initial handshake on startup
            RunFullHandshake();
        }

        public void RunFullHandshake()
        {
            state = ConnectionState.Connecting;
            lastErrorMessage = string.Empty;
            float startTime = Time.realtimeSinceStartup;

            apiClient.GetHealth(
                health =>
                {
                    lastHealth = health;
                    apiClient.GetSpatialCorridor(
                        corridor =>
                        {
                            lastCorridor = corridor;
                            lastHandshakeLatencyMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                            lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            state = ConnectionState.Connected;
                            SpawnSpatialMarkers(corridor);
                        },
                        corridorError =>
                        {
                            state = ConnectionState.Error;
                            lastErrorMessage = corridorError;
                            lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    );
                },
                healthError =>
                {
                    state = ConnectionState.Error;
                    lastErrorMessage = healthError;
                    lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            );
        }

        public void CheckHealthOnly()
        {
            state = ConnectionState.Connecting;
            lastErrorMessage = string.Empty;
            float startTime = Time.realtimeSinceStartup;

            apiClient.GetHealth(
                health =>
                {
                    lastHealth = health;
                    lastHandshakeLatencyMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                    lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    state = ConnectionState.Connected;
                },
                error =>
                {
                    state = ConnectionState.Error;
                    lastErrorMessage = error;
                    lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            );
        }

        public void FetchCorridorOnly()
        {
            state = ConnectionState.Connecting;
            lastErrorMessage = string.Empty;
            float startTime = Time.realtimeSinceStartup;

            apiClient.GetSpatialCorridor(
                corridor =>
                {
                    lastCorridor = corridor;
                    lastHandshakeLatencyMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                    lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    state = ConnectionState.Connected;
                    SpawnSpatialMarkers(corridor);
                },
                error =>
                {
                    state = ConnectionState.Error;
                    lastErrorMessage = error;
                    lastStatusTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            );
        }

        public void Clear()
        {
            state = ConnectionState.Idle;
            lastHealth = null;
            lastCorridor = null;
            lastErrorMessage = string.Empty;
            lastHandshakeLatencyMs = 0f;
            ClearSpawnedMarkers();
        }

        private void SpawnSpatialMarkers(SpatialCorridorResponseDto response)
        {
            ClearSpawnedMarkers();

            if (response?.corridor?.entities == null) return;

            Transform root = markersRoot != null ? markersRoot : transform;

            // In Avenida Juárez coordinates:
            // Center is around lat 31.7462, lon -106.4865
            // 1 degree latitude ~= 111,000 meters
            // 1 degree longitude ~= 94,000 meters (at 31.7N)
            double refLat = response.corridor.center != null ? response.corridor.center.latitude : 31.7462;
            double refLon = response.corridor.center != null ? response.corridor.center.longitude : -106.4865;

            foreach (var entity in response.corridor.entities)
            {
                if (entity?.coordinates == null) continue;

                // Relative meters from corridor center
                float relZ = (float)((entity.coordinates.latitude - refLat) * 111000.0) * 0.1f; // scale down for compact view
                float relX = (float)((entity.coordinates.longitude - refLon) * 94000.0) * 0.1f;

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Marker_{entity.id}";
                marker.transform.SetParent(root, false);
                marker.transform.localPosition = new Vector3(relX, 1.0f, relZ);
                marker.transform.localScale = new Vector3(1.2f, 2.0f, 1.2f);

                // Give distinct colors
                Renderer rend = marker.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (entity.entityType == "infrastructure")
                        rend.material.color = new Color(0.2f, 0.6f, 1.0f); // Blue
                    else if (entity.entityType == "pharmacy")
                        rend.material.color = new Color(0.2f, 0.9f, 0.4f); // Green
                    else if (entity.entityType == "optical")
                        rend.material.color = new Color(1.0f, 0.8f, 0.2f); // Gold
                    else
                        rend.material.color = Color.white;
                }

                spawnedMarkers.Add(marker);
            }
        }

        private void ClearSpawnedMarkers()
        {
            foreach (var m in spawnedMarkers)
            {
                if (m != null) Destroy(m);
            }
            spawnedMarkers.Clear();
        }

        private void OnGUI()
        {
            const int panelWidth = 460;
            const int panelHeight = 560;
            const int margin = 20;

            Rect panelRect = new Rect(margin, margin, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(panelRect.x + 12, panelRect.y + 10, panelRect.width - 24, panelRect.height - 20));

            // Header
            GUILayout.Label("MyVirtualCommunity AI Hackathon", GetTitleStyle());
            GUILayout.Label("Unity 3D Client Foundation (Increment 5.1)", GetSubtitleStyle());
            GUILayout.Space(6);

            // Target URL info
            GUILayout.BeginHorizontal();
            GUILayout.Label("Server URL:", GUILayout.Width(75));
            string currentUrl = GUILayout.TextField(apiClient.BaseUrl);
            if (currentUrl != apiClient.BaseUrl)
            {
                apiClient.BaseUrl = currentUrl;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Status bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", GUILayout.Width(50));
            GUILayout.Label(GetStatusPillText(), GetStatusPillStyle());
            if (lastHandshakeLatencyMs > 0)
            {
                GUILayout.Label($"({lastHandshakeLatencyMs:F0} ms)", GUILayout.Width(70));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Action Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Handshake", GUILayout.Height(28)))
            {
                RunFullHandshake();
            }
            if (GUILayout.Button("Check Health", GUILayout.Height(28)))
            {
                CheckHealthOnly();
            }
            if (GUILayout.Button("Corridor", GUILayout.Height(28)))
            {
                FetchCorridorOnly();
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(28)))
            {
                Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Output Display Area (Scrollable)
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(370));

            if (state == ConnectionState.Connecting)
            {
                GUILayout.Label("Connecting to Node.js assistant server...", GetMutedStyle());
            }
            else if (state == ConnectionState.Error)
            {
                GUILayout.Label("CONNECTION ERROR", GetErrorHeaderStyle());
                GUILayout.TextArea(lastErrorMessage, GetErrorBodyStyle());
                GUILayout.Space(8);
                GUILayout.Label("Troubleshooting Guide:", GetBoldStyle());
                GUILayout.Label("1. Ensure the Node.js server is running in another terminal:\n   npm run dev\n2. Verify the server is listening on http://127.0.0.1:3000\n3. Click 'Test Handshake' above to retry.", GetMutedStyle());
            }
            else if (state == ConnectionState.Connected)
            {
                if (lastHealth != null)
                {
                    GUILayout.Label("Server Diagnostics (/health)", GetSectionHeaderStyle());
                    GUILayout.Label($"Service: {lastHealth.service}");
                    GUILayout.Label($"Status: {lastHealth.status.ToUpper()} | Model: {lastHealth.model}");
                    GUILayout.Label($"Meta Provider: {lastHealth.metaProvider}");
                    if (lastHealth.worldCore != null)
                    {
                        GUILayout.Label($"World Core: {lastHealth.worldCore.mode} (reachability: {lastHealth.worldCore.reachability})");
                    }
                    GUILayout.Label($"Timestamp: {lastHealth.timestamp}");
                    GUILayout.Space(8);
                }

                if (lastCorridor?.corridor != null)
                {
                    var c = lastCorridor.corridor;
                    GUILayout.Label("Authoritative Corridor (/api/spatial/corridor)", GetSectionHeaderStyle());
                    GUILayout.Label($"Corridor ID: {c.id}");
                    GUILayout.Label($"Name: {c.name?.en}");
                    GUILayout.Label($"Location: {c.city}, {c.state}, {c.country}");
                    GUILayout.Label($"Timezone: {c.timeZone}");
                    if (c.center != null)
                    {
                        GUILayout.Label($"Center: ({c.center.latitude:F5}, {c.center.longitude:F5})");
                    }

                    int entityCount = c.entities != null ? c.entities.Length : 0;
                    GUILayout.Label($"Corridor Entities ({entityCount}):", GetBoldStyle());

                    if (c.entities != null)
                    {
                        foreach (var e in c.entities)
                        {
                            GUILayout.Label($" - [{e.entityType.ToUpper()}] {e.canonicalName}");
                            GUILayout.Label($"   Address: {e.address}", GetMutedStyle());
                            if (e.coordinates != null)
                            {
                                GUILayout.Label($"   Coordinates: ({e.coordinates.latitude:F6}, {e.coordinates.longitude:F6})", GetMutedStyle());
                            }
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("Click 'Test Handshake' to query the local Node.js server.", GetMutedStyle());
            }

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private GUIStyle titleStyle;
        private GUIStyle GetTitleStyle()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
                };
            }
            return titleStyle;
        }

        private GUIStyle subtitleStyle;
        private GUIStyle GetSubtitleStyle()
        {
            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
            return subtitleStyle;
        }

        private GUIStyle statusPillStyle;
        private GUIStyle GetStatusPillStyle()
        {
            if (statusPillStyle == null)
            {
                statusPillStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            switch (state)
            {
                case ConnectionState.Connected:
                    statusPillStyle.normal.textColor = new Color(0.2f, 0.9f, 0.3f);
                    break;
                case ConnectionState.Connecting:
                    statusPillStyle.normal.textColor = new Color(1.0f, 0.85f, 0.2f);
                    break;
                case ConnectionState.Error:
                    statusPillStyle.normal.textColor = new Color(1.0f, 0.3f, 0.3f);
                    break;
                default:
                    statusPillStyle.normal.textColor = Color.gray;
                    break;
            }
            return statusPillStyle;
        }

        private string GetStatusPillText()
        {
            switch (state)
            {
                case ConnectionState.Connected: return "[CONNECTED]";
                case ConnectionState.Connecting: return "[CONNECTING...]";
                case ConnectionState.Error: return "[DISCONNECTED]";
                default: return "[IDLE]";
            }
        }

        private GUIStyle sectionHeaderStyle;
        private GUIStyle GetSectionHeaderStyle()
        {
            if (sectionHeaderStyle == null)
            {
                sectionHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.4f, 0.8f, 1.0f) }
                };
            }
            return sectionHeaderStyle;
        }

        private GUIStyle boldStyle;
        private GUIStyle GetBoldStyle()
        {
            if (boldStyle == null)
            {
                boldStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
            return boldStyle;
        }

        private GUIStyle mutedStyle;
        private GUIStyle GetMutedStyle()
        {
            if (mutedStyle == null)
            {
                mutedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    wordWrap = true
                };
            }
            return mutedStyle;
        }

        private GUIStyle errorHeaderStyle;
        private GUIStyle GetErrorHeaderStyle()
        {
            if (errorHeaderStyle == null)
            {
                errorHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1.0f, 0.35f, 0.35f) }
                };
            }
            return errorHeaderStyle;
        }

        private GUIStyle errorBodyStyle;
        private GUIStyle GetErrorBodyStyle()
        {
            if (errorBodyStyle == null)
            {
                errorBodyStyle = new GUIStyle(GUI.skin.textArea)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(1.0f, 0.85f, 0.85f) },
                    wordWrap = true
                };
            }
            return errorBodyStyle;
        }
    }
}

