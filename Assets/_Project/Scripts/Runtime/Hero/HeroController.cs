using System;
using System.Collections.Generic;
using BugReport.Core.AI;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Hero
{
    public enum HeroState
    {
        /// <summary>Waiting for the level to start.</summary>
        Idle,
        /// <summary>Following a planned route.</summary>
        Walking,
        /// <summary>No route to the goal right now; standing as close as it got, re-planning periodically.</summary>
        Blocked,
        /// <summary>Something unexpected interrupted the route; pausing before re-planning.</summary>
        Confused,
        /// <summary>Reached the goal.</summary>
        Arrived,
    }

    /// <summary>
    /// PROJECT HERO's protagonist. Ordinary behaviour first: plan a route over the authored
    /// waypoint graph to the goal, walk it with a CharacterController (so it is physically
    /// blocked by what is solid), and react sensibly when the world changes:
    /// - no full route → walk as far as possible and wait there (Blocked), re-planning;
    /// - the next edge becomes blocked, or it gets physically stuck → Confused, then re-plan;
    /// - any glitch starts or ends → a Blocked hero re-plans almost immediately.
    /// Deterministic and allocation-light; no randomness.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class HeroController : MonoBehaviour
    {
        [SerializeField] private HeroNavGraph graph;
        [SerializeField] private HeroNavNode goal;
        [SerializeField, Min(0f)] private float startDelay = 1.5f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.4f;
        [SerializeField, Min(1f)] private float turnSpeed = 540f;
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.3f;
        [SerializeField] private float gravity = -20f;

        [Header("Reactions")]
        [SerializeField, Min(0.1f)] private float retryDelay = 2f;
        [Tooltip("When a glitch changes the world, a blocked hero re-plans after at most this long.")]
        [SerializeField, Min(0f)] private float worldChangedReplanDelay = 0.3f;
        [SerializeField, Min(0.1f)] private float confusedDuration = 1.2f;
        [SerializeField, Min(0.1f)] private float revalidateInterval = 0.4f;
        [SerializeField, Min(0.001f)] private float minProgress = 0.05f;
        [SerializeField, Min(0.1f)] private float stuckTimeout = 1.25f;

        [SerializeField] private bool autoTick = true;

        private readonly List<int> path = new List<int>();
        private CharacterController controller;
        private ProgressMonitor progress;
        private GameServices services;
        private Action<GameServices> bindCallback;
        private int pathIndex;
        private float timer;
        private float revalidateTimer;
        private float verticalVelocity;
        private int goalId = -1;

        public HeroState State { get; private set; } = HeroState.Idle;
        /// <summary>True if the current route reaches the goal (false: it ends at the closest reachable node).</summary>
        public bool PathComplete { get; private set; }
        public int RouteLength => path.Count;
        public int RouteIndex => pathIndex;
        public HeroNavNode Goal => goal;
        public int ReplanCount { get; private set; }

        public bool AutoTick
        {
            get => autoTick;
            set => autoTick = value;
        }

        /// <summary>(previous, current).</summary>
        public event Action<HeroState, HeroState> StateChanged;

        private ProgressMonitor Progress => progress ??= new ProgressMonitor(minProgress, stuckTimeout);

        public void Configure(HeroNavGraph newGraph, HeroNavNode newGoal, float newStartDelay = 1.5f)
        {
            graph = newGraph;
            goal = newGoal;
            startDelay = Mathf.Max(0f, newStartDelay);
            goalId = -1;
            ResetHero();
        }

        private Pose spawnPose;

        /// <summary>Times the hero fell out of the world and was put back at its start.</summary>
        public int RespawnCount { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPose = new Pose(transform.position, transform.rotation);
            ResetHero();
        }

        /// <summary>
        /// Puts the hero back where the level started it (it fell into a pit, e.g. a bridge stopped
        /// being solid under it). The CharacterController must be disabled to teleport.
        /// </summary>
        public void Respawn()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPose.position, spawnPose.rotation);
            controller.enabled = wasEnabled;
            verticalVelocity = 0f;
            RespawnCount++;
            ResetHero();
            timer = confusedDuration;
            SetState(HeroState.Confused);
        }

        private void OnEnable()
        {
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed)
            {
                services.Glitches.OperationStarted -= OnWorldChanged;
                services.Glitches.OperationEnded -= OnWorldChangedEnded;
                services.WorldChanged -= NotifyWorldChanged;
            }
            services = null;
        }

        private void Bind(GameServices s)
        {
            services = s;
            s.Glitches.OperationStarted += OnWorldChanged;
            s.Glitches.OperationEnded += OnWorldChangedEnded;
            s.WorldChanged += NotifyWorldChanged;
        }

        private void OnWorldChanged(GlitchOperation _) => NotifyWorldChanged();
        private void OnWorldChangedEnded(GlitchOperation _, GlitchEndReason __) => NotifyWorldChanged();

        /// <summary>Something solid may have appeared or vanished: a blocked hero should look again soon.</summary>
        public void NotifyWorldChanged()
        {
            if (State == HeroState.Blocked) timer = Mathf.Min(timer, worldChangedReplanDelay);
        }

        public void ResetHero()
        {
            path.Clear();
            pathIndex = 0;
            PathComplete = false;
            timer = startDelay;
            Progress.Reset();
            SetState(HeroState.Idle);
        }

        private void Update()
        {
            if (autoTick) Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (controller == null) controller = GetComponent<CharacterController>();

            var horizontal = Vector3.zero;
            switch (State)
            {
                case HeroState.Idle:
                case HeroState.Blocked:
                case HeroState.Confused:
                    timer -= deltaTime;
                    if (timer <= 0f) Plan();
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

        /// <summary>Feet position (bottom of the capsule), used for planning.</summary>
        public Vector3 Feet
        {
            get
            {
                if (controller == null) controller = GetComponent<CharacterController>();
                return transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
            }
        }

        private void Plan()
        {
            ReplanCount++;
            if (graph == null || goal == null || !ResolveGoal())
            {
                Debug.LogWarning("[BUG REPORT] Hero has no graph or goal; standing still.", this);
                EnterBlocked();
                return;
            }

            int start = graph.NearestReachable(Feet);
            if (start < 0)
            {
                EnterBlocked();
                return;
            }

            PathComplete = graph.Graph.TryFindPath(start, goalId, graph.EdgePassable, path);
            pathIndex = 0;
            // Don't walk back to the first node if the second is directly reachable from here.
            if (path.Count > 1 && graph.IsPassable(Feet, graph.Position(path[1]))) pathIndex = 1;
            Progress.Reset();
            revalidateTimer = revalidateInterval;

            // No progress possible: either the best reachable node is where planning started (don't walk
            // back to it, wait at the obstacle), or we are already standing at the end of a partial route.
            if (!PathComplete && (path.Count <= 1 || HorizontalDistance(graph.Position(path[path.Count - 1])) <= arriveDistance))
            {
                EnterBlocked();
                return;
            }
            SetState(HeroState.Walking);
        }

        private Vector3 WalkStep(float deltaTime)
        {
            if (pathIndex >= path.Count)
            {
                if (PathComplete) SetState(HeroState.Arrived);
                else EnterBlocked();
                return Vector3.zero;
            }

            var target = graph.Position(path[pathIndex]);
            var toTarget = target - Feet;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= arriveDistance)
            {
                pathIndex++;
                Progress.Reset();
                return Vector3.zero;
            }

            // Moving machinery is re-checked every frame (cheap): a freeze can end at any moment,
            // and the hero must stop before stepping into the machine's path, not 0.4 s later.
            if (EntersUnsafeHazard(target))
            {
                EnterConfused();
                return Vector3.zero;
            }

            revalidateTimer -= deltaTime;
            if (revalidateTimer <= 0f)
            {
                revalidateTimer = revalidateInterval;
                if (!graph.IsPassable(Feet, target))
                {
                    EnterConfused();
                    return Vector3.zero;
                }
            }

            if (Progress.Tick(distance, deltaTime))
            {
                EnterConfused();
                return Vector3.zero;
            }

            var direction = toTarget / distance;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * deltaTime);
            return direction * walkSpeed;
        }

        private bool EntersUnsafeHazard(Vector3 target)
        {
            var hazards = Level.HeroHazard.Active;
            if (hazards.Count == 0) return false;
            float radius = controller != null ? controller.radius : 0.4f;
            var feet = Feet;
            for (int i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                if (h.IsSafe || h.Volume == null) continue;
                // Already inside (cannot happen while unfreezing is vetoed): keep walking out rather than freezing in place.
                if (h.Volume.bounds.Contains(feet + Vector3.up * 0.5f)) continue;
                if (h.Intersects(feet, target, radius)) return true;
            }
            return false;
        }

        private bool ResolveGoal()
        {
            if (goalId >= 0) return true;
            if (!graph.TryGetId(goal, out goalId))
            {
                goalId = -1;
                return false;
            }
            return true;
        }

        private float HorizontalDistance(Vector3 point)
        {
            var d = point - Feet;
            d.y = 0f;
            return d.magnitude;
        }

        private void EnterBlocked()
        {
            timer = retryDelay;
            SetState(HeroState.Blocked);
        }

        private void EnterConfused()
        {
            timer = confusedDuration;
            SetState(HeroState.Confused);
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
