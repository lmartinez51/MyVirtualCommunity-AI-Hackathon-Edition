using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Scene-level coordinator for Bay 1 South's three synchronized two-speed telescopic door assemblies
    /// (Cabin, Lower Landing, Upper Landing).
    ///
    /// Strictly Bay 1 only: never discovers or controls Bay 2 objects.
    /// Uses real Physics.OverlapBox threshold obstruction detection (ignoring own door/cabin/static
    /// architectural colliders) so any CharacterController / player capsule obstructing the closing
    /// threshold immediately triggers anti-pinch reversal to Open without disabling leaf colliders.
    /// </summary>
    public sealed class RemanenciaElevatorDoorController : MonoBehaviour
    {
        [Header("Explicit Bay 1 References (Never Broad-Discovered)")]
        public Transform cabinRoot;
        public RemanenciaElevatorDoorAssembly cabinDoors;
        public RemanenciaElevatorDoorAssembly lowerLandingDoors;
        public RemanenciaElevatorDoorAssembly upperLandingDoors;

        [Header("Operation Policy (07.29: Upper Landing Locked, Cabin Stationary)")]
        public bool allowUpperLandingOperation = false;
        public float moveDurationSeconds = RemanenciaElevatorDoorInterlock.DefaultMoveDurationSeconds;

        [Header("Threshold Obstruction CheckBox Geometry")]
        public float thresholdCenterX = 3.055f;
        public float thresholdHalfExtentX = 0.090f; // X in [+2.965, +3.145]
        public float thresholdCenterZ = 5.850f;
        public float thresholdHalfExtentZ = 0.540f; // Z in [+5.310, +6.390] inside clear 1.10m portal
        public float thresholdMinOffsetY = 0.080f;
        public float thresholdMaxOffsetY = 2.020f;

        private RemanenciaElevatorDoorInterlock interlock;
        private readonly Collider[] overlapBuffer = new Collider[24];
        private bool isCabinMoving;

        public RemanenciaElevatorDoorInterlock Interlock
        {
            get
            {
                EnsureInitialized();
                return interlock;
            }
        }

        public bool AreAllDoorsClosedAndLockedForTravel
        {
            get
            {
                EnsureInitialized();
                SyncColliderStatus();
                return interlock.CanCabinDepart();
            }
        }

        public bool IsCabinMoving => isCabinMoving;

        private void Awake()
        {
            EnsureInitialized();
            ApplyAssembliesToScene();
        }

        private void Update()
        {
            StepController(Time.deltaTime);
        }

        /// <summary>
        /// Deterministic tick callable from both Unity Update() and automated PlayMode/EditMode tests.
        /// </summary>
        public void StepController(float deltaSeconds)
        {
            EnsureInitialized();
            float cabinY = cabinRoot != null ? cabinRoot.position.y : RemanenciaElevatorDoorInterlock.LowerStopCabinRootY;
            interlock.UpdateCabinState(cabinY, isCabinMoving);
            SyncColliderStatus();

            bool obstructed = false;
            if (RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(cabinY, out ElevatorLandingId stop) &&
                interlock.IsLandingAuthorizedForCurrentPosition(stop))
            {
                ElevatorDoorState st = interlock.GetLandingState(stop);
                if (st == ElevatorDoorState.Closing || st == ElevatorDoorState.Blocked)
                {
                    obstructed = IsThresholdObstructed(stop);
                }
            }

            interlock.Step(deltaSeconds, obstructed);
            ApplyAssembliesToScene();
        }

        public void SetCabinMovementState(float cabinRootY, bool moving)
        {
            EnsureInitialized();
            isCabinMoving = moving;
            interlock.UpdateCabinState(cabinRootY, moving);
            ApplyAssembliesToScene();
        }

        public bool RequestToggleLanding(ElevatorLandingId landing)
        {
            EnsureInitialized();
            SyncCabinDatum();
            bool obstructed = IsThresholdObstructed(landing);
            bool accepted = interlock.RequestToggle(landing, obstructed);
            ApplyAssembliesToScene();
            return accepted;
        }

        public bool RequestOpenLanding(ElevatorLandingId landing)
        {
            EnsureInitialized();
            SyncCabinDatum();
            bool accepted = interlock.RequestOpen(landing);
            ApplyAssembliesToScene();
            return accepted;
        }

        public bool RequestCloseLanding(ElevatorLandingId landing)
        {
            EnsureInitialized();
            SyncCabinDatum();
            bool obstructed = IsThresholdObstructed(landing);
            bool accepted = interlock.RequestClose(landing, obstructed);
            ApplyAssembliesToScene();
            return accepted;
        }

        public bool RequestToggleFromInsideCabin()
        {
            EnsureInitialized();
            SyncCabinDatum();
            if (isCabinMoving) return false;
            if (!RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(interlock.CabinRootY, out ElevatorLandingId stop))
            {
                return false;
            }
            return RequestToggleLanding(stop);
        }

        public bool RequestCloseForTravel()
        {
            EnsureInitialized();
            SyncCabinDatum();
            if (interlock.CanCabinDepart()) return true;
            if (!RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(interlock.CabinRootY, out ElevatorLandingId stop))
            {
                return false;
            }
            return RequestCloseLanding(stop);
        }

        /// <summary>
        /// Real Physics.OverlapBox check inside the active Bay 1 doorway threshold.
        /// Ignores own door leaves, cabin architecture, and static shaft/landing walls so only real
        /// dynamic obstructions (such as the player CharacterController capsule) trigger anti-pinch.
        /// </summary>
        public bool IsThresholdObstructed(ElevatorLandingId landing)
        {
            GetThresholdBoxWorld(landing, out Vector3 center, out Vector3 halfExtents);
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = overlapBuffer[i];
                if (c == null || !c.enabled) continue;
                if (IsIgnoredStructuralCollider(c)) continue;
                return true;
            }
            return false;
        }

        public void GetThresholdBoxWorld(ElevatorLandingId landing, out Vector3 center, out Vector3 halfExtents)
        {
            float floorY = landing == ElevatorLandingId.Lower ? -50.000f : -46.400f;
            float minY = floorY + thresholdMinOffsetY;
            float maxY = floorY + thresholdMaxOffsetY;
            center = new Vector3(thresholdCenterX, (minY + maxY) * 0.5f, thresholdCenterZ);
            halfExtents = new Vector3(thresholdHalfExtentX, (maxY - minY) * 0.5f, thresholdHalfExtentZ);
        }

        private bool IsIgnoredStructuralCollider(Collider c)
        {
            if (cabinDoors != null && cabinDoors.OwnsCollider(c)) return true;
            if (lowerLandingDoors != null && lowerLandingDoors.OwnsCollider(c)) return true;
            if (upperLandingDoors != null && upperLandingDoors.OwnsCollider(c)) return true;
            if (cabinRoot != null && c.transform.IsChildOf(cabinRoot)) return true;

            // Player CharacterController or any dynamic non-static actor MUST trigger obstruction
            if (c is CharacterController) return false;
            if (c.GetComponentInParent<FirstPersonPlayer>() != null) return false;

            // Static world architecture (walls, jambs, piers, floor slabs, sills) must never false-trigger
            if (c.gameObject.isStatic) return true;

            // Lower Hotel_Elevator_Core children are non-static=0 in legacy scene; ignore them as permanent architecture
            Transform t = c.transform;
            while (t != null)
            {
                if (t.name == "Hotel_Elevator_Core" ||
                    t.name == "Hotel_UpperPassengerElevatorDoors" ||
                    t.name == "Hotel_UpperPassengerElevatorShaft")
                {
                    return true;
                }
                t = t.parent;
            }
            return false;
        }

        private void EnsureInitialized()
        {
            if (interlock != null) return;
            float initialY = cabinRoot != null ? cabinRoot.position.y : RemanenciaElevatorDoorInterlock.LowerStopCabinRootY;
            interlock = new RemanenciaElevatorDoorInterlock(initialY, allowUpperLandingOperation, moveDurationSeconds);
            SyncColliderStatus();
        }

        private void SyncCabinDatum()
        {
            float cabinY = cabinRoot != null ? cabinRoot.position.y : RemanenciaElevatorDoorInterlock.LowerStopCabinRootY;
            interlock.UpdateCabinState(cabinY, isCabinMoving);
            SyncColliderStatus();
        }

        private void SyncColliderStatus()
        {
            bool lCol = lowerLandingDoors != null && lowerLandingDoors.AreCollidersEnabled();
            bool uCol = upperLandingDoors != null && upperLandingDoors.AreCollidersEnabled();
            bool cCol = cabinDoors != null && cabinDoors.AreCollidersEnabled();
            interlock.UpdateColliderStates(lCol, uCol, cCol);
        }

        private void ApplyAssembliesToScene()
        {
            if (lowerLandingDoors != null) lowerLandingDoors.ApplyOpenFraction(interlock.LowerOpenFraction);
            if (upperLandingDoors != null) upperLandingDoors.ApplyOpenFraction(interlock.UpperOpenFraction);
            if (cabinDoors != null) cabinDoors.ApplyOpenFraction(interlock.CabinOpenFraction);
            Physics.SyncTransforms();
        }
    }
}
