using UnityEngine;

namespace BugReport.Anomaly
{
    /// <summary>
    /// Free-flight movement for the anomaly. Input-agnostic: callers pass a
    /// normalised intent each frame (player controller, tests, cutscenes).
    /// Uses a CharacterController so the anomaly collides with geometry but never
    /// pushes rigidbodies around by accident.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class AnomalyMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxSpeed = 6f;
        [SerializeField, Min(0f)] private float verticalSpeed = 4.5f;
        [SerializeField, Min(1f)] private float boostMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float acceleration = 28f;
        [SerializeField, Min(0.1f)] private float deceleration = 18f;

        private CharacterController controller;
        private Vector3 velocity;

        public Vector3 Velocity => velocity;
        public float Speed => velocity.magnitude;
        public float MaxSpeed => maxSpeed;

        private void Awake() => controller = GetComponent<CharacterController>();

        /// <summary>
        /// Advances movement by <paramref name="deltaTime"/>.
        /// </summary>
        /// <param name="planar">x = strafe, y = forward, each in [-1, 1].</param>
        /// <param name="vertical">+1 ascend, -1 descend.</param>
        /// <param name="boost">Multiplies target speed.</param>
        /// <param name="basis">Orientation defining "forward" (normally the camera yaw). Pitch is ignored.</param>
        public void Tick(Vector2 planar, float vertical, bool boost, Quaternion basis, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (controller == null) controller = GetComponent<CharacterController>();

            planar = Vector2.ClampMagnitude(planar, 1f);
            vertical = Mathf.Clamp(vertical, -1f, 1f);

            var forward = basis * Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);

            float multiplier = boost ? boostMultiplier : 1f;
            var wish = (right * planar.x + forward * planar.y) * (maxSpeed * multiplier)
                       + Vector3.up * (vertical * verticalSpeed * multiplier);

            float rate = wish.sqrMagnitude > 1e-6f ? acceleration * multiplier : deceleration;
            velocity = Vector3.MoveTowards(velocity, wish, rate * deltaTime);

            if (!controller.enabled) return;
            var before = transform.position;
            var flags = controller.Move(velocity * deltaTime);
            // After a collision keep only the motion that actually happened (e.g. sliding along a wall),
            // so velocity into geometry cannot build up. Computed from our own deltaTime rather than
            // CharacterController.velocity, which is tied to the engine frame time.
            if (flags != CollisionFlags.None) velocity = (transform.position - before) / deltaTime;
        }

        /// <summary>Stops immediately (respawn, cutscene, containment capture).</summary>
        public void Halt() => velocity = Vector3.zero;

        /// <summary>Teleports without sweeping through geometry. Use for respawn only.</summary>
        public void Warp(Vector3 position)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
            Halt();
        }
    }
}
