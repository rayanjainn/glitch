using System;
using BugReport.Core.AI;
using UnityEngine;

namespace BugReport.Hero
{
    public enum HeroState
    {
        Idle,
        Walking,
        Waiting,
        Blocked,
        Arrived,
    }

    /// <summary>
    /// Milestone 1 stand-in for the PROJECT HERO protagonist: walks a waypoint
    /// route with a CharacterController (so it is physically blocked by colliders),
    /// detects when it stops making progress, and retries periodically. Retrying is
    /// what lets later glitches (e.g. removing a wall's collision) unblock it.
    /// Replaced by the real hero AI in Milestone 4.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class PlaceholderHero : MonoBehaviour
    {
        [SerializeField] private Transform[] route = new Transform[0];
        [SerializeField] private bool loop;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.2f;
        [SerializeField, Min(1f)] private float turnSpeed = 540f;
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.35f;
        [SerializeField, Min(0f)] private float waitAtWaypoint = 0.75f;
        [SerializeField] private float gravity = -20f;

        [Header("Blocked detection")]
        [SerializeField, Min(0.001f)] private float minProgress = 0.05f;
        [SerializeField, Min(0.1f)] private float blockedTimeout = 1.25f;
        [SerializeField, Min(0.1f)] private float retryDelay = 2f;

        [SerializeField] private bool autoTick = true;

        private CharacterController controller;
        private ProgressMonitor progress;
        private float timer;
        private float verticalVelocity;

        public HeroState State { get; private set; } = HeroState.Idle;
        public int WaypointIndex { get; private set; }
        public int RouteLength => route != null ? route.Length : 0;

        public bool AutoTick
        {
            get => autoTick;
            set => autoTick = value;
        }

        /// <summary>(previous, current).</summary>
        public event Action<HeroState, HeroState> StateChanged;

        public Transform CurrentWaypoint =>
            route != null && WaypointIndex >= 0 && WaypointIndex < route.Length ? route[WaypointIndex] : null;

        public void Configure(Transform[] newRoute, float newStartDelay = 1f, bool newLoop = false)
        {
            route = newRoute ?? new Transform[0];
            startDelay = Mathf.Max(0f, newStartDelay);
            loop = newLoop;
            ResetRoute();
        }

        /// <summary>
        /// Created lazily, not only in Awake: non-serialized fields come back null after a script
        /// recompile during Play mode ("Recompile And Continue Playing"), and Awake does not run again.
        /// </summary>
        private ProgressMonitor Progress =>
            progress ??= new ProgressMonitor(Mathf.Max(0.001f, minProgress), Mathf.Max(0.1f, blockedTimeout));

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            ResetRoute();
        }

        private void Update()
        {
            if (autoTick) Tick(Time.deltaTime);
        }

        public void ResetRoute()
        {
            WaypointIndex = 0;
            timer = startDelay;
            Progress.Reset();
            SetState(HeroState.Idle);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (controller == null) controller = GetComponent<CharacterController>();

            var horizontal = Vector3.zero;
            switch (State)
            {
                case HeroState.Idle:
                    timer -= deltaTime;
                    if (timer <= 0f) BeginWalk();
                    break;

                case HeroState.Waiting:
                    timer -= deltaTime;
                    if (timer <= 0f) AdvanceWaypoint();
                    break;

                case HeroState.Blocked:
                    timer -= deltaTime;
                    if (timer <= 0f) BeginWalk(); // retry: the world may have changed
                    break;

                case HeroState.Walking:
                    horizontal = WalkStep(deltaTime);
                    break;
            }

            if (!controller.enabled) return;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            else verticalVelocity += gravity * deltaTime;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * deltaTime);
        }

        private Vector3 WalkStep(float deltaTime)
        {
            var waypoint = CurrentWaypoint;
            if (waypoint == null)
            {
                Debug.LogWarning("[BUG REPORT] Hero route has a missing waypoint at index " + WaypointIndex + "; stopping.", this);
                SetState(HeroState.Arrived);
                return Vector3.zero;
            }

            var toTarget = waypoint.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= arriveDistance)
            {
                timer = waitAtWaypoint;
                SetState(HeroState.Waiting);
                return Vector3.zero;
            }

            if (Progress.Tick(distance, deltaTime))
            {
                timer = retryDelay;
                SetState(HeroState.Blocked);
                return Vector3.zero;
            }

            var direction = toTarget / distance;
            var facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * deltaTime);
            return direction * walkSpeed;
        }

        private void BeginWalk()
        {
            if (RouteLength == 0)
            {
                SetState(HeroState.Arrived);
                return;
            }
            Progress.Reset();
            SetState(HeroState.Walking);
        }

        private void AdvanceWaypoint()
        {
            WaypointIndex++;
            if (WaypointIndex >= RouteLength)
            {
                if (!loop)
                {
                    WaypointIndex = RouteLength - 1;
                    SetState(HeroState.Arrived);
                    return;
                }
                WaypointIndex = 0;
            }
            BeginWalk();
        }

        private void SetState(HeroState next)
        {
            if (State == next) return;
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
