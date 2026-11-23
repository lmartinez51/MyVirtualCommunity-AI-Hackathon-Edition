using System;
using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Manages the interactive mystery mechanics inside the Building A South "Cuarto Secreto":
    ///   1. The Level-Omega Containment Safe contains the "Ojo del Umbral" (Octagonal Obsidian & Brass
    ///      Dimensional Astrolabe Keystone).
    ///   2. Once the safe is unlocked with its own 6-digit code ("654321"), the player can pick up
    ///      the Astrolabe Keystone.
    ///   3. Inserting the Astrolabe Keystone into the recessed Octagonal Socket on the back-wall
    ///      Dimensional Mirror-Portal ("El Umbral de Lasser") rotates the astral rings, retracts the
    ///      containment chains, and ignites the Dimensional Portal Vortex.
    ///   4. Also provides localized lore inspection captions for all pop-culture horror & esoteric
    ///      containment artifacts in the room.
    /// </summary>
    public class SecretRoomDimensionVaultController : MonoBehaviour
    {
        [Header("Vault Safe & Astrolabe Keystone")]
        public SecretKeypadDoorController vaultSafeDoorController;
        public GameObject astrolabeInSafeObject;
        public Transform astrolabeSafeVisualTransform;

        [Header("Dimensional Portal & Octagonal Socket (Back Wall)")]
        public GameObject astrolabeInSocketObject;
        public Transform astrolabeSocketRingTransform;
        public GameObject portalChainsGroup;
        public Renderer portalMirrorRenderer;
        public GameObject portalActiveVortexGroup;
        public Light portalRiftLight;

        [Header("Portal Materials")]
        public Material dormantMirrorMaterial;
        public Material activeVortexMaterial;

        [Header("Interaction Anchors")]
        public Vector3 safeAstrolabeAnchor = new Vector3(-12.38f, 0.56f, -13.24f);
        public Vector3 portalSocketAnchor = new Vector3(-13.56f, 1.24f, -12.72f);
        public Vector3 portalCenterAnchor = new Vector3(-13.58f, 1.32f, -13.00f);
        public float interactionRange = 2.35f;

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool hasAstrolabeInInventory = false;
        [SerializeField] private bool isPortalActivated = false;
        [SerializeField] private float socketLockProgress = 0f;

        public bool HasAstrolabeInInventory => hasAstrolabeInInventory;
        public bool IsPortalActivated => isPortalActivated;
        public bool IsSafeOpen => vaultSafeDoorController != null && vaultSafeDoorController.IsOpen && vaultSafeDoorController.OpenProgress > 0.45f;

        private void Start()
        {
            RefreshVisualState();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (astrolabeSafeVisualTransform != null && astrolabeInSafeObject != null && astrolabeInSafeObject.activeSelf)
            {
                astrolabeSafeVisualTransform.Rotate(0f, 32f * dt, 0f, Space.Self);
            }

            if (isPortalActivated)
            {
                socketLockProgress = Mathf.MoveTowards(socketLockProgress, 1f, 1.8f * dt);
                if (astrolabeSocketRingTransform != null)
                {
                    astrolabeSocketRingTransform.Rotate(0f, 0f, 48f * dt, Space.Self);
                }
                if (portalActiveVortexGroup != null)
                {
                    portalActiveVortexGroup.transform.Rotate(0f, 0f, -36f * dt, Space.Self);
                }
                if (portalRiftLight != null)
                {
                    portalRiftLight.intensity = 1.35f + 0.35f * Mathf.Sin(Time.time * 3.2f);
                }
            }
            else
            {
                socketLockProgress = Mathf.MoveTowards(socketLockProgress, 0f, 2.2f * dt);
                if (portalRiftLight != null)
                {
                    portalRiftLight.intensity = 0.28f + 0.06f * Mathf.Sin(Time.time * 1.8f);
                }
            }
        }

        /// <summary>
        /// Picks up the Dimensional Astrolabe Keystone from inside the open Vault Safe.
        /// </summary>
        public bool TryTakeAstrolabeFromSafe()
        {
            if (hasAstrolabeInInventory || isPortalActivated) return false;
            if (vaultSafeDoorController != null && !vaultSafeDoorController.IsOpen) return false;

            hasAstrolabeInInventory = true;
            RefreshVisualState();
            return true;
        }

        /// <summary>
        /// Places the Dimensional Astrolabe Keystone into the recessed Octagonal Socket on the
        /// back-wall Mirror-Portal frame, unsealing the chains and igniting the dimensional vortex.
        /// If already activated, removes the Astrolabe and re-seals the portal.
        /// </summary>
        public bool TryToggleAstrolabeInPortalSocket()
        {
            if (isPortalActivated)
            {
                isPortalActivated = false;
                hasAstrolabeInInventory = true;
                RefreshVisualState();
                return true;
            }

            if (!hasAstrolabeInInventory)
            {
                return false;
            }

            hasAstrolabeInInventory = false;
            isPortalActivated = true;
            RefreshVisualState();
            return true;
        }

        public void RefreshVisualState()
        {
            bool showInSafe = !hasAstrolabeInInventory && !isPortalActivated;
            if (astrolabeInSafeObject != null)
            {
                astrolabeInSafeObject.SetActive(showInSafe);
            }

            if (astrolabeInSocketObject != null)
            {
                astrolabeInSocketObject.SetActive(isPortalActivated);
            }

            if (portalChainsGroup != null)
            {
                portalChainsGroup.SetActive(!isPortalActivated);
            }

            if (portalActiveVortexGroup != null)
            {
                portalActiveVortexGroup.SetActive(isPortalActivated);
            }

            if (portalMirrorRenderer != null)
            {
                if (isPortalActivated && activeVortexMaterial != null)
                {
                    portalMirrorRenderer.sharedMaterial = activeVortexMaterial;
                }
                else if (!isPortalActivated && dormantMirrorMaterial != null)
                {
                    portalMirrorRenderer.sharedMaterial = dormantMirrorMaterial;
                }
            }

            if (portalRiftLight != null)
            {
                portalRiftLight.color = isPortalActivated
                    ? new Color(0.32f, 0.88f, 1.0f)
                    : new Color(0.58f, 0.24f, 0.92f);
                portalRiftLight.range = isPortalActivated ? 4.2f : 2.2f;
            }
        }
    }
}
