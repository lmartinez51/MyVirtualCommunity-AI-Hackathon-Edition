using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Dynamic controller for the 4 vehicular toll barrier gates ("plumas de peaje")
    /// across Arcos 1, 2, 3, and 4 at the Paso del Norte International Bridge entrance.
    ///
    /// Manages:
    /// - Smooth physical kinematic rotation between Open (85 deg) and Closed (0 deg)
    /// - Collision blocking synchronization (BoxCollider active/blocking when lowered)
    /// - Dynamic green/red lane traffic indicator lights
    /// - Synchronization with the authoritative World Core backend via ApiClient
    /// - Local user/debug override controls (e.g., via Pause Menu) without scene rebuilds
    /// </summary>
    public class TollBarrierController : MonoBehaviour
    {
        public static TollBarrierController Instance { get; private set; }

        [Header("Barrier Targets")]
        [SerializeField] private ApiClient apiClient;
        [SerializeField] private float rotationSpeedDegreesPerSec = 55.0f;
        [SerializeField] private float serverPollIntervalSeconds = 20.0f;

        [System.Serializable]
        public class LaneBarrierUnit
        {
            public int archNumber; // 1, 2, 3, 4
            public string laneId;  // "lane-1", "lane-2", "lane-3", "lane-4"
            public Transform armPivot;
            public Collider barrierCollider;
            public Renderer statusGreenRenderer;
            public Renderer statusRedRenderer;

            [HideInInspector] public float targetAngle = 85.0f; // 85 = open, 0 = closed
            [HideInInspector] public float currentAngle = 85.0f;
            [HideInInspector] public float angularVelocity = 0.0f;
            [HideInInspector] public bool isOpen = true;
            [HideInInspector] public string statusReason = "Flujo vehicular regular";
        }

        [SerializeField] private LaneBarrierUnit[] barrierUnits = new LaneBarrierUnit[4];

        private float nextServerPollTime = 0.0f;
        private bool isPolling = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (apiClient == null)
            {
                apiClient = FindFirstObjectByType<ApiClient>();
            }

            InitializeBarriers();
        }

        private void Start()
        {
            RefreshFromWorldCore();
        }

        private void InitializeBarriers()
        {
            for (int i = 0; i < 4; i++)
            {
                int archNum = i + 1;
                string laneId = $"lane-{archNum}";

                if (barrierUnits[i] == null)
                {
                    barrierUnits[i] = new LaneBarrierUnit();
                }

                barrierUnits[i].archNumber = archNum;
                barrierUnits[i].laneId = laneId;
                barrierUnits[i].targetAngle = 85.0f;
                barrierUnits[i].currentAngle = 85.0f;
                barrierUnits[i].isOpen = true;

                // Find scene components if not explicitly bound
                if (barrierUnits[i].armPivot == null)
                {
                    GameObject pivotGo = GameObject.Find($"Arch{archNum}_TollBarrier_Arm_Pivot");
                    if (pivotGo != null)
                    {
                        barrierUnits[i].armPivot = pivotGo.transform;
                    }
                }

                if (barrierUnits[i].barrierCollider == null && barrierUnits[i].armPivot != null)
                {
                    barrierUnits[i].barrierCollider = barrierUnits[i].armPivot.GetComponentInChildren<Collider>();
                }

                ApplyPivotRotation(barrierUnits[i], 85.0f);
            }
        }

        private void Update()
        {
            // Smooth kinematic animation for all 4 barrier arms
            for (int i = 0; i < barrierUnits.Length; i++)
            {
                var unit = barrierUnits[i];
                if (unit == null || unit.armPivot == null) continue;

                if (Mathf.Abs(unit.currentAngle - unit.targetAngle) > 0.05f)
                {
                    unit.currentAngle = Mathf.MoveTowards(
                        unit.currentAngle,
                        unit.targetAngle,
                        rotationSpeedDegreesPerSec * Time.deltaTime
                    );

                    ApplyPivotRotation(unit, unit.currentAngle);
                }
            }

            // Periodic authoritative World Core polling
            if (Time.unscaledTime >= nextServerPollTime && !isPolling)
            {
                nextServerPollTime = Time.unscaledTime + serverPollIntervalSeconds;
                RefreshFromWorldCore();
            }
        }

        private void ApplyPivotRotation(LaneBarrierUnit unit, float angle)
        {
            if (unit.armPivot == null) return;

            // In our geometry:
            // Closed (0 deg) = arm lies horizontally along +X across the lane
            // Open (85 deg) = arm swings upward in the XY plane (around Z axis)
            unit.armPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Update traffic light indicators if present
            UpdateTrafficSignalLights(unit);
        }

        private void UpdateTrafficSignalLights(LaneBarrierUnit unit)
        {
            bool fullyOrMostlyOpen = (unit.currentAngle >= 45.0f);

            if (unit.statusGreenRenderer != null)
            {
                unit.statusGreenRenderer.enabled = fullyOrMostlyOpen;
            }
            if (unit.statusRedRenderer != null)
            {
                unit.statusRedRenderer.enabled = !fullyOrMostlyOpen;
            }
        }

        public void RefreshFromWorldCore()
        {
            if (apiClient == null) return;

            isPolling = true;
            apiClient.GetBorderLaneBarriers(
                onSuccess: dto =>
                {
                    isPolling = false;
                    if (dto == null || dto.lanesList == null || dto.lanesList.Length == 0) return;

                    foreach (var laneDto in dto.lanesList)
                    {
                        if (laneDto == null || string.IsNullOrEmpty(laneDto.laneId)) continue;
                        int archIdx = laneDto.archNumber - 1;
                        if (archIdx >= 0 && archIdx < barrierUnits.Length)
                        {
                            var unit = barrierUnits[archIdx];
                            if (unit != null)
                            {
                                bool shouldBeOpen = (laneDto.status == "open");
                                unit.targetAngle = shouldBeOpen ? 85.0f : 0.0f;
                                unit.isOpen = shouldBeOpen;
                                unit.statusReason = laneDto.reason ?? (shouldBeOpen ? "Abierto" : "Cerrado");
                            }
                        }
                    }
                },
                onError: err =>
                {
                    isPolling = false;
                    // Maintain resilient offline state (no intrusive errors)
                }
            );
        }

        public void SetBarrierState(int archNumber, bool open, string reason = null, bool notifyServer = true)
        {
            int idx = archNumber - 1;
            if (idx < 0 || idx >= barrierUnits.Length) return;

            var unit = barrierUnits[idx];
            if (unit == null) return;

            unit.isOpen = open;
            unit.targetAngle = open ? 85.0f : 0.0f;
            unit.statusReason = reason ?? (open ? "Habilitado por operador" : "Cierre por operador");

            if (notifyServer && apiClient != null)
            {
                apiClient.SetBorderLaneBarrierStatus(
                    unit.laneId,
                    open ? "open" : "closed",
                    unit.statusReason,
                    onSuccess: res => { },
                    onError: err => { Debug.LogWarning($"[TollBarrierController] Server sync failed: {err}"); }
                );
            }
        }

        public void ToggleBarrier(int archNumber)
        {
            int idx = archNumber - 1;
            if (idx < 0 || idx >= barrierUnits.Length) return;

            var unit = barrierUnits[idx];
            if (unit == null) return;

            SetBarrierState(archNumber, !unit.isOpen, null, true);
        }

        public bool IsBarrierOpen(int archNumber)
        {
            int idx = archNumber - 1;
            if (idx < 0 || idx >= barrierUnits.Length) return true;
            return barrierUnits[idx]?.isOpen ?? true;
        }

        public float GetBarrierCurrentAngle(int archNumber)
        {
            int idx = archNumber - 1;
            if (idx < 0 || idx >= barrierUnits.Length) return 85.0f;
            return barrierUnits[idx]?.currentAngle ?? 85.0f;
        }

        public string GetBarrierReason(int archNumber)
        {
            int idx = archNumber - 1;
            if (idx < 0 || idx >= barrierUnits.Length) return "";
            return barrierUnits[idx]?.statusReason ?? "";
        }
    }
}

