using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Attached to the double metal return door inside BackroomsLevel1Scene.unity.
    /// Pressing [E] or clicking this door exits Backrooms Level 1 and returns the player
    /// seamlessly to the Av. Benito Juárez sidewalk while releasing World Core vault occupancy.
    /// </summary>
    public class BackroomsExitDoorTrigger : MonoBehaviour
    {
        public string promptEs = "[E] Abrir puerta y regresar a Av. Benito Juárez";
        public string promptEn = "[E] Open door and return to Av. Benito Juárez";
        public float interactionRange = 2.4f;

        public string GetPrompt(bool isEs) => isEs ? promptEs : promptEn;

        public bool IsWithinRange(Vector3 playerPos)
        {
            return Vector3.Distance(transform.position, playerPos) <= interactionRange;
        }
    }
}
