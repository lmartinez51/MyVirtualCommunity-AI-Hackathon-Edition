using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    public enum ElevatorDoorInteractZone
    {
        LowerHallPanel,
        CabinInterior
    }

    /// <summary>
    /// Bilingual [E] player interaction point for Bay 1 South's passenger elevator doors.
    /// Reuses FirstPersonPlayer's existing proximity/facing OnGUI HUD prompt pattern (zero modal UI).
    ///
    /// Strict Bay 1 scoping:
    ///   - LowerHallPanel requires the player to be in the lower lobby East of the core (X >= +3.30),
    ///     on the Bay 1 South side (Z <= +6.95 — never triggering when approaching or standing in Bay 2!),
    ///     within 2.20 m, and generally facing Bay 1.
    ///   - CabinInterior requires the player to be physically inside the Bay 1 cabin interior envelope.
    ///   - Upper Bay 1 remains locked in 07.29 (no open prompt); Bay 2 has zero door interactables.
    /// </summary>
    public sealed class RemanenciaElevatorDoorInteractable : MonoBehaviour
    {
        public RemanenciaElevatorDoorController doorController;
        public ElevatorDoorInteractZone zone = ElevatorDoorInteractZone.LowerHallPanel;
        public float interactionRadius = 2.20f;

        public bool IsAvailableForPlayer(Vector3 playerPos, Vector3 cameraForward)
        {
            if (doorController == null) return false;
            var interlock = doorController.Interlock;
            if (interlock == null) return false;

            if (zone == ElevatorDoorInteractZone.LowerHallPanel)
            {
                // Must be on Nivel 0 lobby floor, East of core piers, and strictly on Bay 1 side (NOT Bay 2!)
                if (Mathf.Abs(playerPos.y - (-50.000f)) > 1.25f) return false;
                if (playerPos.x < 3.30f) return false;
                if (playerPos.z > 6.95f || playerPos.z < 4.40f) return false;

                Vector2 d = new Vector2(playerPos.x - transform.position.x, playerPos.z - transform.position.z);
                if (d.magnitude > interactionRadius) return false;

                // One-sided facing check toward Bay 1 doorway / hall panel (prevents Bay 2 or back-turned triggers)
                Vector2 flatFwd = new Vector2(cameraForward.x, cameraForward.z);
                if (flatFwd.sqrMagnitude > 0.001f)
                {
                    flatFwd.Normalize();
                    Vector2 toBay1 = new Vector2(3.15f - playerPos.x, 6.05f - playerPos.z).normalized;
                    Vector2 toPanel = new Vector2(transform.position.x - playerPos.x, transform.position.z - playerPos.z).normalized;
                    if (Vector2.Dot(flatFwd, toBay1) < 0.20f && Vector2.Dot(flatFwd, toPanel) < 0.30f)
                    {
                        return false;
                    }
                }

                return !string.IsNullOrEmpty(GetPromptText(true));
            }
            else
            {
                // CabinInterior: player must physically stand inside the Bay 1 cabin footprint
                float cabinY = doorController.cabinRoot != null
                    ? doorController.cabinRoot.position.y
                    : RemanenciaElevatorDoorInterlock.LowerStopCabinRootY;
                if (Mathf.Abs(playerPos.y - (cabinY + 0.040f)) > 0.70f) return false;
                if (playerPos.x < 1.32f || playerPos.x > 3.04f) return false;
                if (playerPos.z < 4.62f || playerPos.z > 6.56f) return false;

                return !string.IsNullOrEmpty(GetPromptText(true));
            }
        }

        public string GetPromptText(bool isSpanish)
        {
            if (doorController == null) return string.Empty;
            var interlock = doorController.Interlock;
            if (interlock == null || interlock.IsCabinMoving) return string.Empty;

            if (zone == ElevatorDoorInteractZone.LowerHallPanel)
            {
                if (!interlock.IsLandingAuthorizedForCurrentPosition(ElevatorLandingId.Lower))
                {
                    return string.Empty;
                }

                ElevatorDoorState st = interlock.LowerState;
                if (st == ElevatorDoorState.Closed)
                {
                    return isSpanish ? "[E] Abrir puertas del elevador" : "[E] Open elevator doors";
                }
                if (st == ElevatorDoorState.Open)
                {
                    return isSpanish ? "[E] Cerrar puertas" : "[E] Close doors";
                }
                return string.Empty; // Suppressed while Opening, Closing, Blocked, or Locked
            }
            else
            {
                if (!RemanenciaElevatorDoorInterlock.TryDetectAuthorizedStop(interlock.CabinRootY, out ElevatorLandingId stop))
                {
                    return string.Empty;
                }
                if (!interlock.IsLandingAuthorizedForCurrentPosition(stop))
                {
                    return string.Empty;
                }

                ElevatorDoorState st = interlock.CabinState;
                if (st == ElevatorDoorState.Closed)
                {
                    return isSpanish ? "[E] Abrir puertas" : "[E] Open doors";
                }
                if (st == ElevatorDoorState.Open)
                {
                    return isSpanish ? "[E] Cerrar puertas" : "[E] Close doors";
                }
                return string.Empty;
            }
        }

        public bool Interact()
        {
            if (doorController == null) return false;
            if (zone == ElevatorDoorInteractZone.LowerHallPanel)
            {
                return doorController.RequestToggleLanding(ElevatorLandingId.Lower);
            }
            return doorController.RequestToggleFromInsideCabin();
        }
    }
}
