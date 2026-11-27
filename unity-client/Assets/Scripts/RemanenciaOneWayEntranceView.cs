using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Isolated Remanencia-side receiver for the optional one-way exterior observation glass pane
    /// mounted exclusively inside BackroomsLevel1Scene.unity.
    ///
    /// Never references or modifies BorderCorridorScene.unity or any surface-world exterior door
    /// material. Applies a runtime single-shot RenderTexture (captured before outdoor lighting is
    /// suspended) seamlessly behind the interior hotel double-door mullions, and releases the
    /// runtime material instance cleanly on unload/destroy.
    /// </summary>
    public class RemanenciaOneWayEntranceView : MonoBehaviour
    {
        [Header("Remanencia-Only Interior Observation Glass")]
        public Renderer unifiedGlassPane;

        private Material runtimeMat;
        private bool hasAppliedRuntimeCapture = false;

        public bool HasAppliedRuntimeCapture => hasAppliedRuntimeCapture;

        public void ApplyCapturedExteriorTexture(Texture capturedTexture)
        {
            if (capturedTexture == null || unifiedGlassPane == null) return;

            if (runtimeMat == null && unifiedGlassPane.sharedMaterial != null)
            {
                runtimeMat = new Material(unifiedGlassPane.sharedMaterial);
                unifiedGlassPane.sharedMaterial = runtimeMat;
            }
            if (runtimeMat == null) return;

            if (runtimeMat.HasProperty("_BaseMap"))
            {
                runtimeMat.SetTexture("_BaseMap", capturedTexture);
                runtimeMat.SetTextureScale("_BaseMap", Vector2.one);
                runtimeMat.SetTextureOffset("_BaseMap", Vector2.zero);
            }
            if (runtimeMat.HasProperty("_MainTex"))
            {
                runtimeMat.SetTexture("_MainTex", capturedTexture);
                runtimeMat.SetTextureScale("_MainTex", Vector2.one);
                runtimeMat.SetTextureOffset("_MainTex", Vector2.zero);
            }
            if (runtimeMat.HasProperty("_EmissionMap"))
            {
                runtimeMat.SetTexture("_EmissionMap", capturedTexture);
                runtimeMat.SetTextureScale("_EmissionMap", Vector2.one);
                runtimeMat.SetTextureOffset("_EmissionMap", Vector2.zero);
            }
            hasAppliedRuntimeCapture = true;
        }

        public void ClearCapturedExteriorTexture()
        {
            hasAppliedRuntimeCapture = false;
            if (runtimeMat != null)
            {
                if (Application.isPlaying) Destroy(runtimeMat);
                else DestroyImmediate(runtimeMat);
                runtimeMat = null;
            }
        }

        private void OnDestroy()
        {
            ClearCapturedExteriorTexture();
        }
    }
}
