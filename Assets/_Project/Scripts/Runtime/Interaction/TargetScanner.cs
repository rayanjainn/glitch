using UnityEngine;

namespace BugReport.Interaction
{
    /// <summary>
    /// Finds the gameplay entity under the screen-centre reticle.
    /// Uses a thin sphere cast for forgiving aim, ignores the anomaly's own
    /// hierarchy regardless of layer setup, and takes the nearest hit so walls
    /// occlude what is behind them. Allocation-free per frame.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the camera rig has moved this frame
    [DisallowMultipleComponent]
    public sealed class TargetScanner : MonoBehaviour
    {
        private const int MaxHits = 16;

        [SerializeField] private Camera viewCamera;
        [Tooltip("Hierarchy that can never be targeted (the anomaly).")]
        [SerializeField] private Transform ignoreRoot;
        [SerializeField, Min(0.5f)] private float maxDistance = 30f;
        [SerializeField, Min(0f)] private float probeRadius = 0.12f;
        [Tooltip("Leave empty to use GameLayers.TargetingMask.")]
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private bool autoScan = true;

        private readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        public GameEntity CurrentTarget { get; private set; }
        public bool HasHit { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
        public float MaxDistance => maxDistance;

        public bool AutoScan
        {
            get => autoScan;
            set => autoScan = value;
        }

        public void Configure(Camera cameraToUse, Transform rootToIgnore)
        {
            viewCamera = cameraToUse;
            ignoreRoot = rootToIgnore;
        }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (autoScan) Scan();
        }

        /// <summary>Scans now and returns the targeted entity (or null).</summary>
        public GameEntity Scan()
        {
            CurrentTarget = null;
            HasHit = false;
            if (viewCamera == null) return null;

            int mask = targetMask.value != 0 ? targetMask.value : GameLayers.TargetingMask;
            var ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.SphereCastNonAlloc(ray, probeRadius, hits, maxDistance, mask, QueryTriggerInteraction.Ignore);

            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                // Colliders already overlapping the probe at its origin report distance 0; they are
                // behind or around the camera, not under the reticle.
                if (hit.distance <= 0f) continue;
                if (ignoreRoot != null && hit.transform.IsChildOf(ignoreRoot)) continue;
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = i;
                }
            }

            if (best < 0) return null;
            HasHit = true;
            CurrentHit = hits[best];
            CurrentTarget = CurrentHit.collider.GetComponentInParent<GameEntity>();
            return CurrentTarget;
        }
    }
}
