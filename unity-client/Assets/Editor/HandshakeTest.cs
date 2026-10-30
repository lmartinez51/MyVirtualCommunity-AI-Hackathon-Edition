using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Editor
{
    public static class HandshakeTest
    {
        private const string BaseUrl = "http://127.0.0.1:3000";

        [MenuItem("MyVirtualCommunity/Run API Handshake Verification")]
        public static void RunVerification()
        {
            Debug.Log($"[HandshakeTest] Starting API Handshake verification against {BaseUrl}...");

            // 1. Test /health
            string healthUrl = $"{BaseUrl}/health";
            using (var req = UnityWebRequest.Get(healthUrl))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    System.Threading.Thread.Sleep(20);
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[HandshakeTest] FAIL: /health returned error: {req.error}");
                    EditorApplication.Exit(1);
                    return;
                }

                string healthJson = req.downloadHandler.text;
                var healthDto = JsonUtility.FromJson<HealthResponseDto>(healthJson);

                if (healthDto == null || healthDto.status != "ok")
                {
                    Debug.LogError($"[HandshakeTest] FAIL: Invalid health payload: {healthJson}");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"[HandshakeTest] PASS: /health queried successfully. Service='{healthDto.service}', Status='{healthDto.status}', Model='{healthDto.model}', Provider='{healthDto.metaProvider}'.");
            }

            // 2. Test /api/spatial/corridor
            string corridorUrl = $"{BaseUrl}/api/spatial/corridor";
            using (var req = UnityWebRequest.Get(corridorUrl))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    System.Threading.Thread.Sleep(20);
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[HandshakeTest] FAIL: /api/spatial/corridor returned error: {req.error}");
                    EditorApplication.Exit(1);
                    return;
                }

                string corridorJson = req.downloadHandler.text;
                var corridorDto = JsonUtility.FromJson<SpatialCorridorResponseDto>(corridorJson);

                if (corridorDto == null || !corridorDto.success || corridorDto.corridor == null)
                {
                    Debug.LogError($"[HandshakeTest] FAIL: Invalid corridor payload: {corridorJson}");
                    EditorApplication.Exit(1);
                    return;
                }

                int entityCount = corridorDto.corridor.entities != null ? corridorDto.corridor.entities.Length : 0;
                if (entityCount != 3)
                {
                    Debug.LogError($"[HandshakeTest] FAIL: Expected 3 corridor entities, got {entityCount}");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"[HandshakeTest] PASS: /api/spatial/corridor queried successfully. Corridor='{corridorDto.corridor.id}', Entities={entityCount}.");
                foreach (var entity in corridorDto.corridor.entities)
                {
                    Debug.Log($"[HandshakeTest]   - Entity: [{entity.entityType}] {entity.canonicalName} @ ({entity.coordinates.latitude:F6}, {entity.coordinates.longitude:F6})");
                }
            }

            Debug.Log("[HandshakeTest] ALL API HANDSHAKE CHECKS PASSED SUCCESSFULLY!");
            EditorApplication.Exit(0);
        }
    }
}

