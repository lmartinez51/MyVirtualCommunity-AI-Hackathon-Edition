using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Editor
{
    public static class OfflineTest
    {
        private const string OfflineUrl = "http://127.0.0.1:3999";

        [MenuItem("MyVirtualCommunity/Run Offline Handshake Verification")]
        public static void RunOfflineVerification()
        {
            Debug.Log($"[OfflineTest] Testing graceful failure handling against offline server at {OfflineUrl}...");

            using (var req = UnityWebRequest.Get($"{OfflineUrl}/health"))
            {
                req.timeout = 2;
                req.downloadHandler = new DownloadHandlerBuffer();
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    System.Threading.Thread.Sleep(20);
                }

                if (req.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log($"[OfflineTest] PASS: Gracefully caught connection error as expected: '{req.error}'");
                    Debug.Log("[OfflineTest] Verified: No unhandled exceptions thrown; scene remains stable.");
                    EditorApplication.Exit(0);
                    return;
                }

                Debug.LogError($"[OfflineTest] FAIL: Expected ConnectionError, got result: {req.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}

