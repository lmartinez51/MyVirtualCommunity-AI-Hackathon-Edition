using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    public static class Increment5180VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Run Increment 5.18.0 Verification")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5180Verification] Starting Increment 5.18.0 Server-Authoritative Interaction Verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5180Verification] FAIL: Could not load {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonPlayer>();
            if (player == null)
            {
                Debug.LogError("[Increment5180Verification] FAIL: FirstPersonPlayer not found in scene.");
                EditorApplication.Exit(1);
                return;
            }

            var apiClient = UnityEngine.Object.FindAnyObjectByType<ApiClient>();
            if (apiClient == null)
            {
                Debug.LogError("[Increment5180Verification] FAIL: ApiClient not found in scene.");
                EditorApplication.Exit(1);
                return;
            }

            // Verify movement intent DTO serialization & deserialization (5.18.0.1)
            var moveReqDto = new PlayerMovementIntentRequestDto
            {
                direction = new PlanarDirectionDto { x = 0.0f, z = 1.0f },
                movementMode = "walk",
                sequenceNumber = 1
            };
            string moveReqJson = JsonUtility.ToJson(moveReqDto);
            if (string.IsNullOrEmpty(moveReqJson) || !moveReqJson.Contains("\"walk\""))
            {
                Debug.LogError("[Increment5180Verification] FAIL: PlayerMovementIntentRequestDto serialization failed.");
                EditorApplication.Exit(1);
                return;
            }

            string sampleMoveResJson =
                "{\"success\":true,\"accepted\":true,\"outOfOrder\":false,\"reasonCode\":\"movement_applied\"," +
                "\"player\":{\"playerId\":\"player-1\",\"position\":{\"x\":0.0,\"y\":1.0,\"z\":-14.55},\"lastSequenceNumber\":1}," +
                "\"appliedSpeedMps\":4.5,\"elapsedSeconds\":0.1,\"displacementMeters\":0.45}";
            var moveResDto = JsonUtility.FromJson<PlayerMovementIntentResponseDto>(sampleMoveResJson);
            if (moveResDto == null || !moveResDto.accepted || moveResDto.player?.position == null ||
                Mathf.Abs(moveResDto.appliedSpeedMps - 4.5f) > 0.01f)
            {
                Debug.LogError("[Increment5180Verification] FAIL: PlayerMovementIntentResponseDto deserialization failed.");
                EditorApplication.Exit(1);
                return;
            }

            // Verify DTO serialization & deserialization for authoritative validation
            var reqDto = new NpcInteractionValidationRequestDto
            {
                playerId = "player-1",
                npcId = "elena-border-concierge",
                intent = "talk",
                requestId = "unity-batch-test-1"
            };
            string reqJson = JsonUtility.ToJson(reqDto);
            if (string.IsNullOrEmpty(reqJson) || !reqJson.Contains("elena-border-concierge"))
            {
                Debug.LogError("[Increment5180Verification] FAIL: NpcInteractionValidationRequestDto serialization failed.");
                EditorApplication.Exit(1);
                return;
            }

            string sampleAllowedJson =
                "{\"success\":true,\"allowed\":true,\"reasonCode\":\"authorized_normal_service\"," +
                "\"message\":{\"en\":\"Elena Ramos is on active duty.\",\"es\":\"Elena Ramos está en turno activo.\"}," +
                "\"playerId\":\"player-1\",\"npcId\":\"elena-border-concierge\",\"intent\":\"talk\",\"distanceMeters\":1.5," +
                "\"maxDistanceMeters\":5.0,\"interactionAvailability\":\"available_normal\"}";

            var allowedDto = JsonUtility.FromJson<NpcInteractionValidationResponseDto>(sampleAllowedJson);
            if (allowedDto == null || !allowedDto.allowed || allowedDto.reasonCode != "authorized_normal_service")
            {
                Debug.LogError("[Increment5180Verification] FAIL: Allowed NpcInteractionValidationResponseDto deserialization failed.");
                EditorApplication.Exit(1);
                return;
            }

            string sampleRejectedJson =
                "{\"success\":true,\"allowed\":false,\"reasonCode\":\"out_of_range\"," +
                "\"message\":{\"en\":\"Too far from Elena Ramos.\",\"es\":\"Estás demasiado lejos de Elena Ramos.\"}," +
                "\"playerId\":\"player-1\",\"npcId\":\"elena-border-concierge\",\"intent\":\"talk\",\"distanceMeters\":12.4," +
                "\"maxDistanceMeters\":5.0}";

            var rejectedDto = JsonUtility.FromJson<NpcInteractionValidationResponseDto>(sampleRejectedJson);
            if (rejectedDto == null || rejectedDto.allowed || rejectedDto.reasonCode != "out_of_range" ||
                string.IsNullOrEmpty(rejectedDto.message?.es))
            {
                Debug.LogError("[Increment5180Verification] FAIL: Rejected NpcInteractionValidationResponseDto deserialization failed.");
                EditorApplication.Exit(1);
                return;
            }

            // Verify all 7 canonical NPCs have InteractionPoint components
            var points = UnityEngine.Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None);
            int npcCount = 0;
            foreach (var pt in points)
            {
                if (pt != null && !string.IsNullOrEmpty(pt.characterId) && pt.category != InteractionCategory.Informational)
                {
                    npcCount++;
                }
            }
            if (npcCount < 7)
            {
                Debug.LogError($"[Increment5180Verification] FAIL: Expected >= 7 canonical NPC InteractionPoints, found {npcCount}.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Increment5180Verification] PASS: Verified FirstPersonPlayer, ApiClient, DTOs, and {npcCount} NPC InteractionPoints.");
            EditorApplication.Exit(0);
        }
    }
}
