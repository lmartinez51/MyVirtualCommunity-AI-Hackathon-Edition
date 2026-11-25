using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Restricts where an inspectable descriptor may be observed so underground clues
    /// cannot be inspected from street level or Backrooms, and vice versa.
    /// </summary>
    public enum ArtifactInspectionScope
    {
        SecretRoomOrBackrooms = 0,
        UndergroundSewer = 1
    }

    /// <summary>
    /// Attached to inspectable containment artifacts and environmental maintenance clues so aiming
    /// at them displays their localized dossier and records discoveries in local progression.
    /// Must reside in its own script file matching its class name for Unity Standalone serialization.
    /// </summary>
    public class SecretContainmentArtifactDescriptor : MonoBehaviour
    {
        public string artifactId = "";
        public string titleEs = "";
        public string titleEn = "";
        [TextArea(2, 4)] public string loreEs = "";
        [TextArea(2, 4)] public string loreEn = "";
        public Color accentColor = new Color(0.75f, 0.52f, 1.0f);
        public ArtifactInspectionScope inspectionScope = ArtifactInspectionScope.SecretRoomOrBackrooms;

        public string GetTitle(bool isEs) => isEs ? titleEs : titleEn;
        public string GetLore(bool isEs) => isEs ? loreEs : loreEn;
    }
}
