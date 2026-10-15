using System;
using BugReport.Core.AI;
using BugReport.Core.Glitches;
using BugReport.Containment;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using UnityEngine;

namespace BugReport.Guards
{
    /// <summary>
    /// A skeleton guard: walks a patrol loop, watches a cone in front of it, catches the hero that
    /// ends up next to it, and adds containment pressure while it has the anomaly in view. All
    /// decisions come from the engine-free <see cref="GuardBrain"/> (a finite-state machine); this
    /// component only moves, looks and reports. The hero treats the guard's sight volume as a hazard
    /// and waits while it is unsafe (patrolling, not lured, not frozen).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GuardController : MonoBehaviour, ITimeScaled, IHeroHazardSource
    {
        [SerializeField] private Transform[] patrol = new Transform[0];
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 420f;
        [SerializeField, Min(1f)] private float sightRange = 6f;
        [SerializeField, Range(5f, 90f)] private float sightHalfAngle = 32f;
        [SerializeField, Min(0.2f)] private float catchDistance = 1.1f;
        [SerializeField, Min(0f)] private float patrolPause = 1.2f;
        [SerializeField, Min(0f)] private float lingerSeconds = 3f;
        [SerializeField, OptionalReference] private HeroController hero;
        [SerializeField, OptionalReference] private Transform anomaly;
        [SerializeField, OptionalReference] private ContainmentDirector containment;
        [SerializeField, OptionalReference] private Animator animator;

        private static readonly int StateParameter = Animator.StringToHash("State");

        private GuardBrain brain;
        private FreezeCapability freeze;
        private Rigidbody body;
        private ModifierStack<bool> overrideChannel;
        private Vector3 decoyPoint;
        private float catchCooldown;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        public GuardBrain Brain => brain ??= new GuardBrain(Mathf.Max(1, patrol.Length), patrolPause, lingerSeconds);
        public bool IsFrozen => freeze != null && freeze.IsFrozen;
        public bool IsNeutralized => Brain.IsNeutralized || IsFrozen;
        public bool SeesAnomaly { get; private set; }
        public int CatchCount { get; private set; }
        public float SightRange => sightRange;
        public float SightHalfAngle => sightHalfAngle;
        public Vector3 Forward => transform.forward;
        public Vector3 DecoyPoint => decoyPoint;

        public event Action HeroCaught;

        public ModifierStack<bool> OverrideChannel => overrideChannel ??= new ModifierStack<bool>(false, ConflictPolicy.Exclusive);

        public void Configure(Transform[] route, HeroController heroController, Transform anomalyTransform, ContainmentDirector containmentDirector, Animator modelAnimator)
        {
            patrol = route ?? new Transform[0];
            hero = heroController;
            anomaly = anomalyTransform;
            containment = containmentDirector;
            animator = modelAnimator;
            brain = null;
        }

        private void Awake()
        {
            freeze = GetComponent<FreezeCapability>();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None; // moved directly every frame (smooth, and queries see it at once)
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            brain = null;
            _ = Brain;
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the guard by <paramref name="deltaTime"/>. Public for tests.</summary>
        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f || patrol.Length == 0 || IsFrozen) return;
            catchCooldown = Mathf.Max(0f, catchCooldown - deltaTime);

            var target = CurrentTarget(out bool atTarget);
            Brain.Tick(deltaTime, atTarget);
            bool moving = false;
            if (Brain.ShouldMove && !atTarget)
            {
                var toTarget = target - transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                if (distance > 0.001f)
                {
                    var direction = toTarget / distance;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * deltaTime);
                    float step = Mathf.Min(distance, walkSpeed * deltaTime);
                    var next = transform.position + direction * step;
                    body.position = next;
                    transform.position = next;
                    moving = true;
                }
            }
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetInteger(StateParameter, moving ? 1 : 0);

            Look();
            TryCatchHero();
        }

        private Vector3 CurrentTarget(out bool atTarget)
        {
            Vector3 target;
            switch (Brain.State)
            {
                case GuardState.Investigate: target = decoyPoint; break;
                default: target = patrol[Mathf.Clamp(Brain.PatrolIndex, 0, patrol.Length - 1)].position; break;
            }
            var flat = target - transform.position;
            flat.y = 0f;
            // Lured guards stop short of the decoy instead of walking into it.
            float stop = Brain.State == GuardState.Investigate ? 1.2f : 0.15f;
            atTarget = flat.magnitude <= stop;
            return target;
        }

        // ---------------------------------------------------------------- senses

        private void Look()
        {
            SeesAnomaly = false;
            if (IsNeutralized || anomaly == null) return;
            var from = transform.position + Vector3.up * 1.4f;
            var to = anomaly.position;
            if (Mathf.Abs(to.y - transform.position.y) > 5f) return;
            if (!VisionCone.Contains(from.x, from.z, transform.forward.x, transform.forward.z, to.x, to.z, sightRange, sightHalfAngle)) return;
            if (Physics.Linecast(from, to, GameLayers.HeroBlockingMask, QueryTriggerInteraction.Ignore)) return;
            SeesAnomaly = true;
            if (containment != null) containment.ReportSighted();
        }

        private void TryCatchHero()
        {
            if (hero == null || IsNeutralized || catchCooldown > 0f) return;
            var d = hero.Feet - transform.position;
            d.y = 0f;
            if (d.magnitude > catchDistance) return;
            catchCooldown = 2f;
            CatchCount++;
            hero.Respawn(caught: true);
            HeroCaught?.Invoke();
        }

        // ---------------------------------------------------------------- overrides (used by AI Override)

        /// <summary>Lure the guard to <paramref name="point"/>.</summary>
        public void Distract(Vector3 point)
        {
            decoyPoint = point;
            Brain.Distract();
        }

        /// <summary>The override ended: back to the patrol route.</summary>
        public void ClearOverride() => Brain.ClearOverride();

        /// <summary>Level restart: back to the start of the route and the starting pose.</summary>
        public void ResetGuard()
        {
            Brain.Reset();
            body.position = spawnPosition;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            CatchCount = 0;
        }
    }
}
