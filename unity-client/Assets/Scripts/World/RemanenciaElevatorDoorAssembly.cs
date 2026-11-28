using UnityEngine;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Controls one two-speed telescopic elevator door pair (SlowLeaf + FastLeaf) for Bay 1 South.
    /// Moves both leaves toward South (-Z) at an exact 2:1 speed ratio:
    ///   - SlowLeaf travel: -0.550 m
    ///   - FastLeaf travel: -1.100 m
    ///   - Nominal duration: 1.60 s
    /// Keeps both solid non-trigger BoxColliders permanently enabled throughout movement.
    /// </summary>
    public sealed class RemanenciaElevatorDoorAssembly : MonoBehaviour
    {
        public const float DefaultSlowTravelZ = -0.550f;
        public const float DefaultFastTravelZ = -1.100f;

        [Header("Assembly Identity")]
        public string assemblyId = "Bay1_DoorAssembly";
        public ElevatorLandingId landingId = ElevatorLandingId.Lower;
        public bool isCabinAssembly = false;

        [Header("Telescopic Leaves")]
        public Transform slowLeaf;
        public Transform fastLeaf;
        public BoxCollider slowLeafCollider;
        public BoxCollider fastLeafCollider;

        [Header("Deterministic Closed Positions & 2:1 Travel Deltas")]
        public Vector3 slowClosedLocalPos;
        public Vector3 fastClosedLocalPos;
        public Vector3 slowTravelLocalDelta = new Vector3(0f, 0f, DefaultSlowTravelZ);
        public Vector3 fastTravelLocalDelta = new Vector3(0f, 0f, DefaultFastTravelZ);
        public float moveDurationSeconds = RemanenciaElevatorDoorInterlock.DefaultMoveDurationSeconds;

        [SerializeField]
        [Range(0f, 1f)]
        private float openFraction = 0f;

        public float OpenFraction => openFraction;
        public bool IsFullyClosed => openFraction <= RemanenciaElevatorDoorInterlock.ClosedFractionTolerance;
        public bool IsFullyOpen => openFraction >= 1f - RemanenciaElevatorDoorInterlock.ClosedFractionTolerance;

        private void Awake()
        {
            ResolveCollidersIfNeeded();
            ApplyOpenFraction(openFraction);
        }

        public void ConfigureGeometry(
            string id,
            ElevatorLandingId landing,
            bool isCabin,
            Transform slow,
            Transform fast,
            Vector3 slowClosedLocal,
            Vector3 fastClosedLocal)
        {
            assemblyId = id;
            landingId = landing;
            isCabinAssembly = isCabin;
            slowLeaf = slow;
            fastLeaf = fast;
            slowClosedLocalPos = slowClosedLocal;
            fastClosedLocalPos = fastClosedLocal;
            slowTravelLocalDelta = new Vector3(0f, 0f, DefaultSlowTravelZ);
            fastTravelLocalDelta = new Vector3(0f, 0f, DefaultFastTravelZ);
            moveDurationSeconds = RemanenciaElevatorDoorInterlock.DefaultMoveDurationSeconds;
            ResolveCollidersIfNeeded();
            ApplyOpenFraction(0f);
        }

        /// <summary>
        /// Applies normalized openFraction in [0, 1] directly to both telescopic leaves at an exact 2:1 ratio.
        /// Handles arbitrary direction reversals with zero positional discontinuity and keeps solid colliders active.
        /// </summary>
        public void ApplyOpenFraction(float fraction)
        {
            openFraction = Mathf.Clamp01(fraction);
            ResolveCollidersIfNeeded();

            if (slowLeaf != null)
            {
                slowLeaf.localPosition = slowClosedLocalPos + slowTravelLocalDelta * openFraction;
            }
            if (fastLeaf != null)
            {
                fastLeaf.localPosition = fastClosedLocalPos + fastTravelLocalDelta * openFraction;
            }

            if (slowLeafCollider != null)
            {
                slowLeafCollider.enabled = true;
                slowLeafCollider.isTrigger = false;
            }
            if (fastLeafCollider != null)
            {
                fastLeafCollider.enabled = true;
                fastLeafCollider.isTrigger = false;
            }
        }

        public bool AreCollidersEnabled()
        {
            ResolveCollidersIfNeeded();
            return slowLeafCollider != null && slowLeafCollider.enabled && !slowLeafCollider.isTrigger &&
                   fastLeafCollider != null && fastLeafCollider.enabled && !fastLeafCollider.isTrigger;
        }

        public bool OwnsCollider(Collider col)
        {
            if (col == null) return false;
            ResolveCollidersIfNeeded();
            return col == slowLeafCollider || col == fastLeafCollider ||
                   (slowLeaf != null && col.transform.IsChildOf(slowLeaf)) ||
                   (fastLeaf != null && col.transform.IsChildOf(fastLeaf));
        }

        private void ResolveCollidersIfNeeded()
        {
            if (slowLeaf != null && slowLeafCollider == null)
            {
                slowLeafCollider = slowLeaf.GetComponent<BoxCollider>();
            }
            if (fastLeaf != null && fastLeafCollider == null)
            {
                fastLeafCollider = fastLeaf.GetComponent<BoxCollider>();
            }
        }
    }
}
