using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Kinematic platform/machine that oscillates between its start position and start + offset.
    /// Advances with local time, so a <see cref="FreezeCapability"/> on the same object stops it
    /// exactly where it is and it resumes from the same phase.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class PingPongMover : MonoBehaviour, ITimeScaled
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 3f, 0f);
        [SerializeField, Min(0.1f)] private float period = 4f;
        [SerializeField] private bool autoStep = true;

        private Rigidbody body;
        private FreezeCapability freeze;
        private Vector3 start;

        /// <summary>Cycles completed (fractional). Frozen time does not advance it.</summary>
        public float Phase { get; private set; }
        public bool AutoStep
        {
            get => autoStep;
            set => autoStep = value;
        }

        public void Configure(Vector3 newOffset, float newPeriod)
        {
            offset = newOffset;
            period = Mathf.Max(0.1f, newPeriod);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            freeze = GetComponent<FreezeCapability>();
            start = transform.position;
        }

        private void FixedUpdate()
        {
            if (autoStep) Step(Time.fixedDeltaTime);
        }

        /// <summary>Advances by <paramref name="deltaTime"/> of world time, scaled by the local freeze.</summary>
        public void Step(float deltaTime)
        {
            float scale = freeze != null ? freeze.TimeScale : 1f;
            Phase += deltaTime * scale / period;
            body.MovePosition(PositionAt(Phase));
        }

        public Vector3 PositionAt(float phase) => start + offset * (0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f));
    }
}
