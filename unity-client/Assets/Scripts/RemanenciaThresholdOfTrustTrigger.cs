using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Deterministic crossing trigger for Implementation 07.12 (Room 111-B — Threshold of Trust).
    /// Placed inside the recessed East architectural threshold of Room 111-B (X in [+24.04, +24.48],
    /// Z in [+21.85, +22.95], BaseY = -50.00).
    ///
    /// Does NOT trigger from ordinary proximity in the room (X <= crossingPlaneX). Only fires when
    /// the player deliberately steps across the recessed threshold sill into the overcast boundary,
    /// invoking SecretBackroomsTransitionController.RequestExitRemanencia("threshold-of-trust")
    /// strictly once per visit.
    /// </summary>
    public class RemanenciaThresholdOfTrustTrigger : MonoBehaviour
    {
        public const string ExitSourceId = "threshold-of-trust";

        [Header("Deliberate Crossing Bounds (World Space)")]
        public float crossingPlaneX = 23.92f;
        public float minZ = 21.80f;
        public float maxZ = 23.00f;
        public float floorY = -50.00f;
        public float maxVerticalOffset = 1.60f;

        private bool hasTriggeredThisVisit = false;
        private SecretBackroomsTransitionController cachedTransition;
        private Transform cachedPlayerTransform;

        public bool HasTriggeredThisVisit => hasTriggeredThisVisit;

        private void OnEnable()
        {
            hasTriggeredThisVisit = false;
        }

        public void ResetTriggerLatchForTest()
        {
            hasTriggeredThisVisit = false;
        }

        private void Update()
        {
            if (!Application.isPlaying || hasTriggeredThisVisit) return;

            if (cachedTransition == null)
            {
                cachedTransition = FindFirstObjectByType<SecretBackroomsTransitionController>();
            }
            if (cachedTransition == null || !cachedTransition.IsPlayerInBackrooms || cachedTransition.IsTransitioning)
            {
                return;
            }

            if (cachedPlayerTransform == null)
            {
                CharacterController cc = FindFirstObjectByType<CharacterController>();
                if (cc != null) cachedPlayerTransform = cc.transform;
            }
            if (cachedPlayerTransform == null) return;

            EvaluatePlayerCrossing(cachedPlayerTransform.position, cachedTransition);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryHandleCollider(other);
        }

        private void TryHandleCollider(Collider other)
        {
            if (!Application.isPlaying || hasTriggeredThisVisit || other == null) return;
            CharacterController cc = other.GetComponent<CharacterController>() ?? other.GetComponentInParent<CharacterController>();
            if (cc == null) return;

            if (cachedTransition == null)
            {
                cachedTransition = FindFirstObjectByType<SecretBackroomsTransitionController>();
            }
            EvaluatePlayerCrossing(cc.transform.position, cachedTransition);
        }

        /// <summary>
        /// Pure, deterministic crossing predicate used both at runtime and by automated tests.
        /// Returns true strictly when a deliberate crossing initiates RequestExitRemanencia().
        /// </summary>
        public bool IsWithinCrossingZone(Vector3 playerWorldPos)
        {
            return playerWorldPos.x >= crossingPlaneX &&
                   playerWorldPos.z >= minZ &&
                   playerWorldPos.z <= maxZ &&
                   Mathf.Abs(playerWorldPos.y - floorY) <= maxVerticalOffset;
        }

        public bool EvaluatePlayerCrossing(Vector3 playerWorldPos, SecretBackroomsTransitionController transition)
        {
            if (hasTriggeredThisVisit || transition == null) return false;
            if (!transition.IsPlayerInBackrooms || transition.IsTransitioning) return false;
            if (!IsWithinCrossingZone(playerWorldPos)) return false;

            bool started = transition.RequestExitRemanencia(ExitSourceId);
            if (started)
            {
                hasTriggeredThisVisit = true;
            }
            return started;
        }
    }
}
