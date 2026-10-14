using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Core.Tutorial;
using BugReport.Glitches;
using BugReport.Level;
using UnityEngine;

namespace BugReport.Tutorial
{
    public enum GuideCondition
    {
        /// <summary>The player has flown a short distance (latches).</summary>
        Move,
        /// <summary>The target is under the reticle (optionally while <c>requireSelected</c> is selected).</summary>
        LookAt,
        /// <summary>The target is selected.</summary>
        Select,
        /// <summary>The glitch is equipped.</summary>
        Equip,
        /// <summary>The glitch has been applied (to the target, if set) (latches).</summary>
        Apply,
        /// <summary>The player undid a glitch (latches).</summary>
        Revert,
        /// <summary>The player opened inspection mode (latches).</summary>
        Inspect,
        /// <summary>Never met: shown until the objective completes ("watch the hero").</summary>
        WaitForObjective,
    }

    /// <summary>
    /// Teaches by doing: shows one short instruction at a time, reacting to what the player
    /// actually does, with a marker on the object it talks about. Steps are grouped by objective
    /// (their section); the guide never blocks progress because objectives advance on their own
    /// (see <see cref="GuideTracker"/>). Also introduces glitches one at a time by locking those
    /// whose section has not started yet.
    /// </summary>
    public sealed class GuideDirector : MonoBehaviour
    {
        [Serializable]
        public struct Step
        {
            [Tooltip("Objective index this step belongs to (0 = first objective).")]
            public int section;
            [TextArea(1, 3), Tooltip("Instruction; key tokens such as [E] are highlighted.")]
            public string text;
            public GuideCondition condition;
            [OptionalReference, Tooltip("LookAt / Select / Apply: the object the step is about, marked in the world. For Apply, any target of the glitch (use the one the player aims at).")]
            public GameEntity target;
            [OptionalReference, Tooltip("LookAt only: also require this entity to be selected (e.g. aim at the destination while the source is selected).")]
            public GameEntity requireSelected;
            [OptionalReference, Tooltip("Equip / Apply: the glitch.")]
            public GlitchDefinition glitch;

            public Step(int section, string text, GuideCondition condition, GameEntity target = null,
                GlitchDefinition glitch = null, GameEntity requireSelected = null)
            {
                this.section = section;
                this.text = text;
                this.condition = condition;
                this.target = target;
                this.glitch = glitch;
                this.requireSelected = requireSelected;
            }
        }

        [Serializable]
        public struct Unlock
        {
            [Tooltip("The glitch is locked until this objective index starts.")]
            public int section;
            public GlitchDefinition glitch;

            public Unlock(int section, GlitchDefinition glitch)
            {
                this.section = section;
                this.glitch = glitch;
            }
        }

        [SerializeField] private Step[] steps = new Step[0];
        [SerializeField] private Unlock[] unlocks = new Unlock[0];
        [SerializeField] private GlitchController glitches;
        [SerializeField] private ObjectiveDirector objectives;
        [SerializeField, OptionalReference, Tooltip("The anomaly, for Move steps.")] private Transform player;
        [SerializeField, Min(0.1f)] private float moveDistance = 2f;

        private readonly HashSet<string> appliedGlitches = new HashSet<string>();
        private readonly HashSet<(string glitch, GameEntityId target)> appliedTargets = new HashSet<(string, GameEntityId)>();
        private GuideTracker tracker;
        private GameServices services;
        private Action<GameServices> bindCallback;
        private Vector3 playerStart;
        private bool reverted;
        private bool inspected;
        private int highWater = -1;
        private int highWaterSection = -1;

        /// <summary>
        /// Raised the first time a step is passed and when a new section starts (audio, UI flash).
        /// Not raised again when a state step is lost and regained (looking away and back).
        /// </summary>
        public event Action Advanced;

        public bool IsActive => tracker != null && tracker.Current >= 0 && services != null && !services.IsDisposed;
        public string CurrentText => IsActive ? steps[tracker.Current].text : string.Empty;
        public int CurrentStep => tracker != null ? tracker.Current : -1;

        /// <summary>The object the current step is about, if any (for the world marker).</summary>
        public GameEntity CurrentTarget
        {
            get
            {
                if (!IsActive) return null;
                var step = steps[tracker.Current];
                switch (step.condition)
                {
                    case GuideCondition.LookAt:
                    case GuideCondition.Select:
                    case GuideCondition.Apply:
                        return step.target != null && step.target.isActiveAndEnabled ? step.target : null;
                    default:
                        return null;
                }
            }
        }

        /// <summary>"2/5": position of the current step within its objective.</summary>
        public (int number, int total) Position => tracker != null ? tracker.Position() : (0, 0);

        public void Configure(Step[] newSteps, Unlock[] newUnlocks, GlitchController glitchController, ObjectiveDirector objectiveDirector, Transform playerTransform)
        {
            steps = newSteps ?? new Step[0];
            unlocks = newUnlocks ?? new Unlock[0];
            glitches = glitchController;
            objectives = objectiveDirector;
            player = playerTransform;
            tracker = null;
        }

        private void OnEnable()
        {
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
            if (tracker == null) ApplyUnlocks(0); // before the first frame, so locked glitches never flash in the HUD
            // Unlock the next glitch in the same frame the objective completes, not one Update later.
            if (objectives != null) objectives.ObjectiveCompleted += OnObjectiveCompleted;
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
            if (services != null && !services.IsDisposed)
            {
                services.Glitches.OperationStarted -= OnOperationStarted;
                services.Glitches.OperationEnded -= OnOperationEnded;
                services.State.StateChanged -= OnStateChanged;
            }
            if (tracker != null) tracker.Advanced -= OnAdvanced;
            services = null;
        }

        private void Bind(GameServices s)
        {
            services = s;
            s.Glitches.OperationStarted += OnOperationStarted;
            s.Glitches.OperationEnded += OnOperationEnded;
            s.State.StateChanged += OnStateChanged;
            if (player != null) playerStart = player.position;
            EnsureTracker();
        }

        private void EnsureTracker()
        {
            if (tracker != null) return;
            var sections = new int[steps.Length];
            var latching = new bool[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                sections[i] = steps[i].section;
                latching[i] = IsLatching(steps[i].condition);
            }
            tracker = new GuideTracker(sections, latching);
            tracker.Advanced += OnAdvanced;
        }

        private static bool IsLatching(GuideCondition c)
            => c == GuideCondition.Move || c == GuideCondition.Apply || c == GuideCondition.Revert || c == GuideCondition.Inspect;

        private void Update() => Evaluate();

        /// <summary>Re-evaluates the guide now. Public for tests.</summary>
        public void Evaluate()
        {
            if (services == null || services.IsDisposed) return;
            EnsureTracker();
            int section = CurrentSection();
            ApplyUnlocks(section);
            tracker.Update(section, IsMet);
        }

        private int CurrentSection()
        {
            var t = objectives != null ? objectives.Tracker : null;
            return t != null ? t.CompletedCount : 0;
        }

        private void ApplyUnlocks(int section)
        {
            if (glitches == null) return;
            foreach (var u in unlocks) glitches.SetLocked(u.glitch, u.section > section);
        }

        private bool IsMet(int index)
        {
            var step = steps[index];
            var selection = services.Selection;
            switch (step.condition)
            {
                case GuideCondition.Move:
                    return player != null && (player.position - playerStart).sqrMagnitude >= moveDistance * moveDistance;
                case GuideCondition.LookAt:
                    return Is(selection.Hovered, step.target)
                        && (step.requireSelected == null || Is(selection.Selected, step.requireSelected));
                case GuideCondition.Select:
                    return Is(selection.Selected, step.target);
                case GuideCondition.Equip:
                    return glitches != null && step.glitch != null && glitches.CurrentDefinition == step.glitch;
                case GuideCondition.Apply:
                    if (step.glitch == null) return false;
                    return step.target == null
                        ? appliedGlitches.Contains(step.glitch.Id)
                        : appliedTargets.Contains((step.glitch.Id, step.target.Id));
                case GuideCondition.Revert:
                    return reverted;
                case GuideCondition.Inspect:
                    return inspected || services.State.Current == GameState.Inspection;
                default:
                    return false;
            }
        }

        private static bool Is(GameEntityId id, GameEntity entity) => entity != null && id.IsValid && entity.Id == id;

        private void OnOperationStarted(GlitchOperation operation)
        {
            appliedGlitches.Add(operation.Spec.Id);
            for (int i = 0; i < operation.TargetCount; i++) appliedTargets.Add((operation.Spec.Id, operation.Target(i)));
        }

        private void OnOperationEnded(GlitchOperation operation, GlitchEndReason reason)
        {
            if (reason == GlitchEndReason.Reverted) reverted = true;
        }

        private void OnObjectiveCompleted(int _) => Evaluate();

        private void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Inspection) inspected = true;
        }

        private void OnAdvanced()
        {
            int section = tracker.Section;
            int current = tracker.Current < 0 ? int.MaxValue : tracker.Current;
            if (section == highWaterSection && current <= highWater) return;
            highWaterSection = section;
            highWater = current;
            Advanced?.Invoke();
        }

        /// <summary>Authoring check used by the level builder and tests; null when valid.</summary>
        public string Validate()
        {
            for (int i = 0; i < steps.Length; i++)
            {
                var s = steps[i];
                if (string.IsNullOrWhiteSpace(s.text)) return "step " + i + " has no text";
                if (i > 0 && s.section < steps[i - 1].section) return "step " + i + " is out of section order";
                bool needsTarget = s.condition == GuideCondition.LookAt || s.condition == GuideCondition.Select;
                if (needsTarget && s.target == null) return "step " + i + " (" + s.condition + ") needs a target";
                bool needsGlitch = s.condition == GuideCondition.Equip || s.condition == GuideCondition.Apply;
                if (needsGlitch && s.glitch == null) return "step " + i + " (" + s.condition + ") needs a glitch";
                if (s.condition == GuideCondition.Move && player == null) return "step " + i + " (Move) needs the player transform";
            }
            foreach (var u in unlocks)
                if (u.glitch == null) return "an unlock entry has no glitch";
            return null;
        }
    }
}
