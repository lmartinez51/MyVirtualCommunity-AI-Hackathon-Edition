using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Attached to inspectable containment artifacts inside the Cuarto Secreto so aiming at them
    /// or clicking/pressing [E] displays their localized containment dossier and mystery lore.
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

        public string GetTitle(bool isEs) => isEs ? titleEs : titleEn;
        public string GetLore(bool isEs) => isEs ? loreEs : loreEn;
    }
}
