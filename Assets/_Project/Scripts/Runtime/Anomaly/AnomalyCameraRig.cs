using UnityEngine;

namespace BugReport.Anomaly
{
    /// <summary>
    /// Third-person orbit camera that follows the anomaly, keeps a stable frame and
    /// pulls in when geometry would block the view. No shake, no motion blur.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class AnomalyCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float initialYaw;
        [SerializeField, Range(-80f, 80f)] private float initialPitch = 15f;

        [Header("Framing")]
        [SerializeField, Min(0.5f)] private float distance = 4.5f;
        [SerializeField, Min(0.1f)] private float minDistance = 0.6f;
        [SerializeField] private float pivotHeight = 0.55f;
        [SerializeField, Min(0f), Tooltip("Over-the-shoulder offset to the right, so the anomaly doesn't cover the reticle. Shrinks near walls.")]
        private float shoulderOffset = 0.75f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.06f;
        [SerializeField, Min(0.1f)] private float zoomOutSpeed = 6f;

        [Header("Look")]
        [Tooltip("Degrees per unit of pointer delta.")]
        [SerializeField, Min(0f)] private float pointerSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField, Min(0f)] private float stickSensitivity = 160f;
        [SerializeField] private bool invertY;
        [SerializeField, Range(-89f, 0f)] private float minPitch = -70f;
        [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

        [Header("Collision")]
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
        [Tooltip("Leave empty to use GameLayers.CameraObstructionMask.")]
        [SerializeField] private LayerMask obstructionMask;

        private Camera viewCamera;
        private float yaw;
        private float pitch;
        private float currentDistance;
        private Vector3 pivot;
        private Vector3 pivotVelocity;
        private int mask;

        public Camera Camera => viewCamera;
        public Transform Target => target;
        public float Yaw => yaw;
        public float Pitch => pitch;
        /// <summary>Movement basis for the anomaly: camera yaw only.</summary>
        public Quaternion YawRotation => Quaternion.Euler(0f, yaw, 0f);
        public Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0f);

        public float PointerSensitivity
        {
            get => pointerSensitivity;
            set => pointerSensitivity = Mathf.Max(0f, value);
        }

        public float StickSensitivity
        {
            get => stickSensitivity;
            set => stickSensitivity = Mathf.Max(0f, value);
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            yaw = initialYaw;
            pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
            currentDistance = distance;
            mask = obstructionMask.value != 0 ? obstructionMask.value : GameLayers.CameraObstructionMask;
            SnapToTarget();
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap) SnapToTarget();
        }

        /// <param name="delta">Pointer delta (pixels) or stick value ([-1, 1]).</param>
        /// <param name="fromPointer">True for mouse/pen/touch deltas.</param>
        public void AddLook(Vector2 delta, bool fromPointer, float deltaTime)
        {
            float scale = fromPointer ? pointerSensitivity : stickSensitivity * Mathf.Max(0f, deltaTime);
            yaw = Mathf.Repeat(yaw + delta.x * scale, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * scale * (invertY ? -1f : 1f), minPitch, maxPitch);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            pivot = DesiredPivot();
            pivotVelocity = Vector3.zero;
            currentDistance = ResolveDistance(pivot, ViewRotation);
            ApplyPose();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var desiredPivot = DesiredPivot();
            pivot = followSmoothTime > 0f
                ? Vector3.SmoothDamp(pivot, desiredPivot, ref pivotVelocity, followSmoothTime)
                : desiredPivot;

            float wanted = ResolveDistance(pivot, ViewRotation);
            // Snap in immediately (never show the inside of a wall), ease back out.
            currentDistance = wanted < currentDistance
                ? wanted
                : Mathf.MoveTowards(currentDistance, wanted, zoomOutSpeed * Time.deltaTime);
            ApplyPose();
        }

        /// <summary>Above the anomaly and to its right, pulled in if a wall is that close on the right.</summary>
        private Vector3 DesiredPivot()
        {
            var basePivot = target.position + Vector3.up * pivotHeight;
            if (shoulderOffset <= 0f) return basePivot;
            var right = YawRotation * Vector3.right;
            float side = shoulderOffset;
            if (Physics.SphereCast(basePivot, collisionRadius, right, out var hit, shoulderOffset, mask, QueryTriggerInteraction.Ignore))
                side = Mathf.Max(0f, hit.distance - 0.05f);
            return basePivot + right * side;
        }

        private float ResolveDistance(Vector3 from, Quaternion rotation)
        {
            var back = rotation * Vector3.back;
            if (Physics.SphereCast(from, collisionRadius, back, out var hit, distance, mask, QueryTriggerInteraction.Ignore))
                return Mathf.Max(minDistance, hit.distance);
            return distance;
        }

        private void ApplyPose()
        {
            var rotation = ViewRotation;
            transform.SetPositionAndRotation(pivot + rotation * Vector3.back * currentDistance, rotation);
        }
    }
}
