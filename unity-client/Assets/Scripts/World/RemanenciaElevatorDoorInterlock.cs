using System;

namespace MyVirtualCommunity.Client.World
{
    public enum ElevatorDoorState
    {
        Closed,
        Opening,
        Open,
        Closing,
        Blocked,
        Locked
    }

    public enum ElevatorLandingId
    {
        Lower = 0,
        Upper = 1
    }

    /// <summary>
    /// Pure C# deterministic door interlock and synchronized state machine for Bay 1 South.
    /// Has zero Unity scene dependencies so every invariant can be unit-tested in EditMode.
    ///
    /// Authoritative cabin root Y datum (deck underside):
    ///   - Lower stop: Y = -50.000 m
    ///   - Upper stop: Y = -46.440 m
    ///   - Endpoint tolerance: +/- 0.002 m (never uses exact float equality)
    ///
    /// Enforces:
    ///   - Cabin doors open synchronously with Lower landing at Lower stop (Upper remains Locked).
    ///   - Cabin doors open synchronously with Upper landing at Upper stop (Lower remains Locked).
    ///   - Lower and Upper landing doors can never be open simultaneously.
    ///   - No door may open while cabin is moving.
    ///   - Departure readiness requires all 3 assemblies Closed/Locked, openFraction == 0 within
    ///     tolerance, and active solid colliders enabled on all 3 pairs.
    ///   - Obstruction during Closing transitions immediately to Blocked -> reverses to Opening.
    /// </summary>
    public sealed class RemanenciaElevatorDoorInterlock
    {
        public const float LowerStopCabinRootY = -50.000f;
        public const float UpperStopCabinRootY = -46.440f;
        public const float StopToleranceMeters = 0.002f;
        public const float ClosedFractionTolerance = 0.0005f;
        public const float DefaultMoveDurationSeconds = 1.6f;

        private readonly float moveDurationSeconds;
        private bool allowUpperLandingOperation;

        public ElevatorDoorState LowerState { get; private set; } = ElevatorDoorState.Closed;
        public ElevatorDoorState UpperState { get; private set; } = ElevatorDoorState.Locked;
        public ElevatorDoorState CabinState { get; private set; } = ElevatorDoorState.Closed;

        public float LowerOpenFraction { get; private set; }
        public float UpperOpenFraction { get; private set; }
        public float CabinOpenFraction { get; private set; }

        public float CabinRootY { get; private set; }
        public bool IsCabinMoving { get; private set; }

        public bool LowerCollidersEnabled { get; private set; } = true;
        public bool UpperCollidersEnabled { get; private set; } = true;
        public bool CabinCollidersEnabled { get; private set; } = true;

        public bool AllowUpperLandingOperation
        {
            get => allowUpperLandingOperation;
            set
            {
                allowUpperLandingOperation = value;
                SyncLockedStates();
            }
        }

        public float MoveDurationSeconds => moveDurationSeconds;

        public RemanenciaElevatorDoorInterlock(
            float initialCabinRootY = LowerStopCabinRootY,
            bool allowUpperLandingOperation = false,
            float moveDurationSeconds = DefaultMoveDurationSeconds)
        {
            this.moveDurationSeconds = moveDurationSeconds > 0.05f ? moveDurationSeconds : DefaultMoveDurationSeconds;
            this.allowUpperLandingOperation = allowUpperLandingOperation;
            CabinRootY = initialCabinRootY;
            IsCabinMoving = false;
            SyncLockedStates();
        }

        public static bool TryDetectAuthorizedStop(float cabinRootY, out ElevatorLandingId stop)
        {
            if (Math.Abs(cabinRootY - LowerStopCabinRootY) <= StopToleranceMeters)
            {
                stop = ElevatorLandingId.Lower;
                return true;
            }
            if (Math.Abs(cabinRootY - UpperStopCabinRootY) <= StopToleranceMeters)
            {
                stop = ElevatorLandingId.Upper;
                return true;
            }
            stop = ElevatorLandingId.Lower;
            return false;
        }

        public bool IsLandingAuthorizedForCurrentPosition(ElevatorLandingId landing)
        {
            if (IsCabinMoving) return false;
            if (landing == ElevatorLandingId.Upper && !allowUpperLandingOperation) return false;
            return TryDetectAuthorizedStop(CabinRootY, out ElevatorLandingId activeStop) && activeStop == landing;
        }

        public void UpdateCabinState(float cabinRootY, bool isMoving)
        {
            CabinRootY = cabinRootY;
            IsCabinMoving = isMoving;
            SyncLockedStates();
        }

        public void UpdateColliderStates(bool lowerEnabled, bool upperEnabled, bool cabinEnabled)
        {
            LowerCollidersEnabled = lowerEnabled;
            UpperCollidersEnabled = upperEnabled;
            CabinCollidersEnabled = cabinEnabled;
        }

        public ElevatorDoorState GetLandingState(ElevatorLandingId landing)
        {
            return landing == ElevatorLandingId.Lower ? LowerState : UpperState;
        }

        public float GetLandingOpenFraction(ElevatorLandingId landing)
        {
            return landing == ElevatorLandingId.Lower ? LowerOpenFraction : UpperOpenFraction;
        }

        /// <summary>
        /// Requests opening the synchronized pair (Cabin + requested landing).
        /// Rejects if moving, out of endpoint tolerance, landing locked, or opposite landing is not closed.
        /// </summary>
        public bool RequestOpen(ElevatorLandingId landing)
        {
            SyncLockedStates();
            if (!IsLandingAuthorizedForCurrentPosition(landing)) return false;

            // Opposite landing must be completely closed/locked
            float oppFrac = landing == ElevatorLandingId.Lower ? UpperOpenFraction : LowerOpenFraction;
            if (oppFrac > ClosedFractionTolerance) return false;

            ElevatorDoorState curState = GetLandingState(landing);
            if (curState == ElevatorDoorState.Open || curState == ElevatorDoorState.Opening)
            {
                return true;
            }

            SetSynchronizedState(landing, ElevatorDoorState.Opening);
            return true;
        }

        /// <summary>
        /// Requests opening the cabin doors together with whichever landing the cabin is currently parked at.
        /// Works at both Lower and Upper authorized endpoints.
        /// </summary>
        public bool RequestOpenAtCurrentStop()
        {
            if (IsCabinMoving) return false;
            if (!TryDetectAuthorizedStop(CabinRootY, out ElevatorLandingId stop)) return false;
            return RequestOpen(stop);
        }

        /// <summary>
        /// Requests closing the synchronized pair at the specified landing.
        /// If threshold is currently obstructed, transitions to Blocked and reverses cleanly to Opening.
        /// </summary>
        public bool RequestClose(ElevatorLandingId landing, bool isObstructed = false)
        {
            SyncLockedStates();
            if (!IsLandingAuthorizedForCurrentPosition(landing)) return false;

            ElevatorDoorState curState = GetLandingState(landing);
            if (curState == ElevatorDoorState.Closed || curState == ElevatorDoorState.Locked)
            {
                return true;
            }

            if (isObstructed)
            {
                SetSynchronizedState(landing, ElevatorDoorState.Blocked);
                return false;
            }

            if (curState == ElevatorDoorState.Closing)
            {
                return true;
            }

            SetSynchronizedState(landing, ElevatorDoorState.Closing);
            return true;
        }

        public bool RequestCloseAtCurrentStop(bool isObstructed = false)
        {
            if (IsCabinMoving) return false;
            if (!TryDetectAuthorizedStop(CabinRootY, out ElevatorLandingId stop)) return false;
            return RequestClose(stop, isObstructed);
        }

        /// <summary>
        /// Deterministic user toggle for [E] interaction: opens when Closed, closes when Open,
        /// reverses smoothly if interrupted mid-motion (Opening -> Closing, Closing -> Opening).
        /// </summary>
        public bool RequestToggle(ElevatorLandingId landing, bool isObstructed = false)
        {
            SyncLockedStates();
            if (!IsLandingAuthorizedForCurrentPosition(landing)) return false;

            ElevatorDoorState curState = GetLandingState(landing);
            switch (curState)
            {
                case ElevatorDoorState.Closed:
                case ElevatorDoorState.Closing:
                case ElevatorDoorState.Blocked:
                    return RequestOpen(landing);
                case ElevatorDoorState.Open:
                case ElevatorDoorState.Opening:
                    return RequestClose(landing, isObstructed);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Advances synchronized door fractions deterministically over deltaSeconds.
        /// Must be driven from gameplay Update; handles mid-close obstruction reversal with zero discontinuity.
        /// </summary>
        public void Step(float deltaSeconds, bool activeThresholdObstructed)
        {
            SyncLockedStates();
            if (deltaSeconds <= 0f) return;

            float stepFrac = deltaSeconds / moveDurationSeconds;

            if (TryDetectAuthorizedStop(CabinRootY, out ElevatorLandingId activeStop) &&
                IsLandingAuthorizedForCurrentPosition(activeStop))
            {
                StepSynchronizedPair(activeStop, stepFrac, activeThresholdObstructed);
            }
        }

        /// <summary>
        /// Returns true ONLY when the cabin is safe to depart:
        /// all 3 assemblies are Closed or Locked, openFraction <= tolerance, and all solid colliders are enabled.
        /// </summary>
        public bool CanCabinDepart()
        {
            if (IsCabinMoving) return false;
            bool lowerSafe = (LowerState == ElevatorDoorState.Closed || LowerState == ElevatorDoorState.Locked) &&
                             LowerOpenFraction <= ClosedFractionTolerance;
            bool upperSafe = (UpperState == ElevatorDoorState.Closed || UpperState == ElevatorDoorState.Locked) &&
                             UpperOpenFraction <= ClosedFractionTolerance;
            bool cabinSafe = (CabinState == ElevatorDoorState.Closed || CabinState == ElevatorDoorState.Locked) &&
                             CabinOpenFraction <= ClosedFractionTolerance;

            return lowerSafe && upperSafe && cabinSafe &&
                   LowerCollidersEnabled && UpperCollidersEnabled && CabinCollidersEnabled;
        }

        private void StepSynchronizedPair(ElevatorLandingId landing, float stepFrac, bool obstructed)
        {
            ElevatorDoorState state = GetLandingState(landing);
            float frac = GetLandingOpenFraction(landing);

            if (state == ElevatorDoorState.Closing && obstructed)
            {
                // Anti-pinch safety: immediately enter Blocked then reverse cleanly toward Open
                SetSynchronizedState(landing, ElevatorDoorState.Blocked);
                state = ElevatorDoorState.Blocked;
            }

            if (state == ElevatorDoorState.Blocked)
            {
                // Immediately begin reopening from current exact fraction (zero positional jump)
                SetSynchronizedState(landing, ElevatorDoorState.Opening);
                state = ElevatorDoorState.Opening;
            }

            if (state == ElevatorDoorState.Opening)
            {
                frac = Math.Min(1f, frac + stepFrac);
                SetSynchronizedFraction(landing, frac);
                if (frac >= 1f - ClosedFractionTolerance)
                {
                    SetSynchronizedFraction(landing, 1f);
                    SetSynchronizedState(landing, ElevatorDoorState.Open);
                }
            }
            else if (state == ElevatorDoorState.Closing)
            {
                frac = Math.Max(0f, frac - stepFrac);
                SetSynchronizedFraction(landing, frac);
                if (frac <= ClosedFractionTolerance)
                {
                    SetSynchronizedFraction(landing, 0f);
                    SetSynchronizedState(landing, ElevatorDoorState.Closed);
                }
            }
        }

        private void SetSynchronizedState(ElevatorLandingId landing, ElevatorDoorState state)
        {
            if (landing == ElevatorLandingId.Lower)
            {
                LowerState = state;
            }
            else
            {
                UpperState = state;
            }
            CabinState = state;
        }

        private void SetSynchronizedFraction(ElevatorLandingId landing, float fraction)
        {
            float clamped = Math.Max(0f, Math.Min(1f, fraction));
            if (landing == ElevatorLandingId.Lower)
            {
                LowerOpenFraction = clamped;
            }
            else
            {
                UpperOpenFraction = clamped;
            }
            CabinOpenFraction = clamped;
        }

        private void SyncLockedStates()
        {
            bool lowerAuth = IsLandingAuthorizedForCurrentPosition(ElevatorLandingId.Lower);
            bool upperAuth = IsLandingAuthorizedForCurrentPosition(ElevatorLandingId.Upper);

            if (!lowerAuth && LowerOpenFraction <= ClosedFractionTolerance)
            {
                LowerState = ElevatorDoorState.Locked;
            }
            else if (lowerAuth && LowerState == ElevatorDoorState.Locked)
            {
                LowerState = ElevatorDoorState.Closed;
            }

            if (!upperAuth && UpperOpenFraction <= ClosedFractionTolerance)
            {
                UpperState = ElevatorDoorState.Locked;
            }
            else if (upperAuth && UpperState == ElevatorDoorState.Locked)
            {
                UpperState = ElevatorDoorState.Closed;
            }

            if (!lowerAuth && !upperAuth && CabinOpenFraction <= ClosedFractionTolerance)
            {
                CabinState = ElevatorDoorState.Locked;
            }
            else if ((lowerAuth || upperAuth) && CabinState == ElevatorDoorState.Locked)
            {
                CabinState = ElevatorDoorState.Closed;
            }
        }
    }
}
