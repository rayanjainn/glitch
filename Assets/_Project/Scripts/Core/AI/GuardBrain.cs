using System;

namespace BugReport.Core.AI
{
    public enum GuardState
    {
        /// <summary>Walking its patrol loop, pausing at each point.</summary>
        Patrol,
        /// <summary>Lured by a decoy (AI Override): goes there and lingers.</summary>
        Investigate,
        /// <summary>Walking back to its patrol route.</summary>
        Return,
        /// <summary>Stunned/overridden: does nothing, sees nothing.</summary>
        Disabled,
    }

    /// <summary>
    /// The guard's decision logic, free of any engine types: a small finite-state machine
    /// (GDD 4.5: manipulate behaviour states, never generate code). The adapter tells it whether the
    /// guard has arrived at the current target and it answers with the state, the patrol index and
    /// what to walk to. Overrides (<see cref="Distract"/>, <see cref="SetDisabled"/>) are temporary and
    /// reversible: <see cref="ClearOverride"/> sends the guard back to its route.
    /// </summary>
    public sealed class GuardBrain
    {
        private readonly int patrolPoints;
        private readonly float patrolPause;
        private readonly float lingerSeconds;
        private float wait;

        public GuardBrain(int patrolPoints, float patrolPauseSeconds = 1f, float lingerSeconds = 2.5f)
        {
            if (patrolPoints < 1) throw new ArgumentOutOfRangeException(nameof(patrolPoints));
            this.patrolPoints = patrolPoints;
            patrolPause = Math.Max(0f, patrolPauseSeconds);
            this.lingerSeconds = Math.Max(0f, lingerSeconds);
            wait = this.patrolPause;
        }

        public GuardState State { get; private set; } = GuardState.Patrol;
        /// <summary>The patrol point being walked to (Patrol, Return).</summary>
        public int PatrolIndex { get; private set; }
        /// <summary>True when the adapter should be moving the guard this frame.</summary>
        public bool ShouldMove => State != GuardState.Disabled && (State == GuardState.Investigate || wait <= 0f);
        public bool IsNeutralized => State == GuardState.Disabled || State == GuardState.Investigate;

        public event Action<GuardState, GuardState> StateChanged;

        /// <summary>
        /// Advances the machine. <paramref name="arrived"/>: the guard has reached its current target
        /// (a patrol point while Patrol/Return, the decoy while Investigate).
        /// </summary>
        public void Tick(float deltaTime, bool arrived)
        {
            if (deltaTime <= 0f) return;
            switch (State)
            {
                case GuardState.Disabled:
                    return;
                case GuardState.Patrol:
                    if (wait > 0f)
                    {
                        wait -= deltaTime;
                        return;
                    }
                    if (arrived)
                    {
                        PatrolIndex = (PatrolIndex + 1) % patrolPoints;
                        wait = patrolPause;
                    }
                    return;
                case GuardState.Investigate:
                    if (!arrived) return;
                    wait -= deltaTime;
                    if (wait <= 0f) Enter(GuardState.Return, 0f);
                    return;
                case GuardState.Return:
                    if (arrived) Enter(GuardState.Patrol, patrolPause);
                    return;
            }
        }

        /// <summary>AI Override: go investigate a decoy. Linger time starts when the guard gets there.</summary>
        public void Distract()
        {
            if (State == GuardState.Disabled) return;
            Enter(GuardState.Investigate, lingerSeconds);
        }

        public void SetDisabled()
        {
            Enter(GuardState.Disabled, 0f);
        }

        /// <summary>The override ended (reverted, expired or patched): back to the route.</summary>
        public void ClearOverride()
        {
            if (State == GuardState.Patrol) return;
            Enter(GuardState.Return, 0f);
        }

        /// <summary>Resets to the start of the patrol loop (level restart).</summary>
        public void Reset()
        {
            State = GuardState.Patrol;
            PatrolIndex = 0;
            wait = patrolPause;
        }

        private void Enter(GuardState next, float newWait)
        {
            wait = newWait;
            if (State == next) return;
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
