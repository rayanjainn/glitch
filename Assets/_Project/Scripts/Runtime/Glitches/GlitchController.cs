using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Interaction;
using UnityEngine;

namespace BugReport.Glitches
{
    public enum GlitchFeedbackKind
    {
        Applied,
        Reverted,
        Rejected,
        Info,
    }

    public readonly struct GlitchFeedback
    {
        public readonly GlitchFeedbackKind Kind;
        public readonly string Message;
        public readonly Color Color;

        public GlitchFeedback(GlitchFeedbackKind kind, string message, Color color)
        {
            Kind = kind;
            Message = message;
            Color = color;
        }
    }

    /// <summary>
    /// Player-facing glitch workflow only: which glitch is equipped, which entities it
    /// would target, and turning requests into <see cref="GlitchManager"/> calls plus
    /// feedback. Contains no per-glitch logic.
    ///
    /// Targeting: one-slot glitches use the hovered entity (or the selection if nothing
    /// is hovered). Two-slot glitches use the selection as SOURCE and the hovered entity
    /// as TARGET: select with E, aim, apply with LMB.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GlitchController : MonoBehaviour
    {
        private static readonly Color RejectColor = new Color(1f, 0.3f, 0.35f);
        private static readonly Color InfoColor = new Color(0.85f, 0.95f, 1f);

        [SerializeField] private GlitchDefinition[] glitches = new GlitchDefinition[0];
        [SerializeField] private SelectionController selection;

        private readonly List<Loaded> loaded = new List<Loaded>();
        private readonly HashSet<GlitchDefinition> lockedDefinitions = new HashSet<GlitchDefinition>();
        private readonly GameEntityId[] oneTarget = new GameEntityId[1];
        private readonly GameEntityId[] twoTargets = new GameEntityId[2];
        private GameServices services;
        private Action<GameServices> bindCallback;
        private int current;

        public event Action<GlitchFeedback> Feedback;

        public int LoadedCount => loaded.Count;
        /// <summary>The equipped glitch, or null when none is loaded and unlocked.</summary>
        public GlitchDefinition CurrentDefinition => HasCurrent ? loaded[current].Definition : null;
        public int CurrentIndex => current;
        public GlitchDefinition DefinitionAt(int index) => loaded[index].Definition;
        public bool IsUnlocked(int index) => !lockedDefinitions.Contains(loaded[index].Definition);

        public int UnlockedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < loaded.Count; i++) if (IsUnlocked(i)) n++;
                return n;
            }
        }

        private bool HasCurrent => loaded.Count > 0 && IsUnlocked(current);

        /// <summary>
        /// Locks or unlocks a glitch (tutorials introduce them one at a time). Locked glitches are
        /// skipped when cycling and hidden in the HUD. Unlocking announces the new glitch.
        /// </summary>
        public void SetLocked(GlitchDefinition definition, bool isLocked)
        {
            if (definition == null) return;
            bool changed = isLocked ? lockedDefinitions.Add(definition) : lockedDefinitions.Remove(definition);
            if (!changed) return;
            if (!isLocked) Emit(GlitchFeedbackKind.Info, "NEW GLITCH: " + definition.DisplayName, definition.Color);
            EnsureCurrentUnlocked();
        }

        private void EnsureCurrentUnlocked()
        {
            if (loaded.Count == 0 || IsUnlocked(current)) return;
            for (int i = 0; i < loaded.Count; i++)
            {
                if (!IsUnlocked(i)) continue;
                current = i;
                return;
            }
        }
        public GlitchFeedback LastFeedback { get; private set; }
        public float LastFeedbackTime { get; private set; } = float.NegativeInfinity;

        /// <summary>What applying right now would do. Valid only when <see cref="HasPreview"/>.</summary>
        public GlitchResult Preview { get; private set; }
        /// <summary>True when the current aim forms a complete target set worth previewing.</summary>
        public bool HasPreview { get; private set; }
        /// <summary>Prompt for the next step when the target set is incomplete (e.g. "SELECT A SOURCE").</summary>
        public string Prompt { get; private set; }

        public void Configure(GlitchDefinition[] definitions, SelectionController selectionController)
        {
            glitches = definitions ?? new GlitchDefinition[0];
            selection = selectionController;
        }

        /// <summary>Colour of the glitch that produced <paramref name="operation"/> (for presenters).</summary>
        public Color ColorFor(GlitchOperation operation)
        {
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i].Spec == operation.Spec) return loaded[i].Definition.Color;
            }
            return InfoColor;
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
                services.Glitches.OperationEnded -= OnOperationEnded;
                services.Glitches.EffectError -= OnEffectError;
            }
            services = null;
            loaded.Clear();
        }

        private void Bind(GameServices s)
        {
            services = s;
            loaded.Clear();
            current = 0;
            foreach (var definition in glitches)
            {
                if (definition == null) continue;
                var problem = definition.ValidateDefinition();
                if (problem != null)
                {
                    Debug.LogError("[BUG REPORT] Glitch '" + definition.name + "' " + problem + "; it is disabled.", definition);
                    continue;
                }
                loaded.Add(new Loaded(definition, definition.ToSpec(), definition.CreateEffect(s)));
            }
            EnsureCurrentUnlocked();
            if (loaded.Count == 0) Debug.LogWarning("[BUG REPORT] GlitchController has no usable glitches.", this);

            s.Glitches.OperationEnded += OnOperationEnded;
            s.Glitches.EffectError += OnEffectError;
        }

        private void Update()
        {
            HasPreview = false;
            Prompt = null;
            if (!CanAct()) return;

            // Scaled time: pausing (timeScale 0) also pauses glitch durations.
            services.Glitches.Tick(Time.time);

            if (!HasCurrent) return;
            var g = loaded[current];
            if (!TryBuildTargets(g, out var targets, out var prompt))
            {
                Prompt = prompt;
                return;
            }
            Preview = services.Glitches.Validate(g.Spec, g.Effect, targets, Time.time);
            HasPreview = true;
        }

        public void ApplyCurrent()
        {
            if (!CanAct()) return;
            if (!HasCurrent)
            {
                Emit(GlitchFeedbackKind.Rejected, "NO GLITCHES AVAILABLE", RejectColor);
                return;
            }

            var g = loaded[current];
            if (!TryBuildTargets(g, out var targets, out var prompt))
            {
                Emit(GlitchFeedbackKind.Rejected, prompt, RejectColor);
                return;
            }

            var result = services.Glitches.TryApply(g.Spec, g.Effect, targets, Time.time);
            if (!result.Succeeded)
            {
                Emit(GlitchFeedbackKind.Rejected, result.Describe(), RejectColor);
                return;
            }

            Emit(GlitchFeedbackKind.Applied, g.Spec.DisplayName + ": " + DescribeTargets(result.Operation), g.Definition.Color);
            if (g.Spec.Arity > 1) services.Selection.ClearSelection(); // the source has been used
        }

        public void RevertLatest()
        {
            if (!CanAct()) return;
            var result = services.Glitches.RevertLatest();
            if (result.Succeeded)
                Emit(GlitchFeedbackKind.Reverted, "RESTORED: " + DescribeTargets(result.Operation), InfoColor);
            else
                Emit(GlitchFeedbackKind.Rejected, result.Describe(), RejectColor);
        }

        public void CycleGlitch()
        {
            if (!CanAct() || !HasCurrent) return;
            for (int step = 0; step < loaded.Count; step++)
            {
                current = (current + 1) % loaded.Count;
                if (IsUnlocked(current)) break;
            }
            Emit(GlitchFeedbackKind.Info, "GLITCH: " + loaded[current].Spec.DisplayName, loaded[current].Definition.Color);
        }

        private bool TryBuildTargets(Loaded g, out GameEntityId[] targets, out string prompt)
        {
            var model = services.Selection;
            prompt = null;
            if (g.Spec.Arity == 1)
            {
                var id = model.Hovered.IsValid ? model.Hovered : model.Selected;
                targets = oneTarget;
                targets[0] = id;
                if (!id.IsValid) prompt = "AIM AT A " + g.Definition.SlotLabel(0);
                return id.IsValid;
            }

            targets = twoTargets;
            targets[0] = model.Selected;
            targets[1] = model.Hovered;
            if (!model.Selected.IsValid) prompt = "SELECT A " + g.Definition.SlotLabel(0) + " [E]";
            else if (!model.Hovered.IsValid) prompt = "AIM AT A " + g.Definition.SlotLabel(1);
            return prompt == null;
        }

        private string DescribeTargets(GlitchOperation operation)
        {
            var text = string.Empty;
            for (int i = 0; i < operation.TargetCount; i++)
            {
                if (i > 0) text += " -> ";
                text += services != null && services.Registry.TryGet(operation.Target(i), out var e) ? e.DisplayName : operation.Target(i).ToString();
            }
            return text;
        }

        private void OnOperationEnded(GlitchOperation operation, GlitchEndReason reason)
        {
            switch (reason)
            {
                case GlitchEndReason.Expired:
                    Emit(GlitchFeedbackKind.Info, operation.Spec.DisplayName + " EXPIRED", InfoColor);
                    break;
                case GlitchEndReason.TargetLost:
                    Emit(GlitchFeedbackKind.Info, operation.Spec.DisplayName + " ENDED: TARGET LOST", InfoColor);
                    break;
                case GlitchEndReason.Patched:
                    Emit(GlitchFeedbackKind.Info, operation.Spec.DisplayName + " PATCHED BY SYSTEM", RejectColor);
                    break;
            }
        }

        private void OnEffectError(GlitchOperation operation, Exception e)
        {
            Debug.LogError("[BUG REPORT] Glitch effect error in " + (operation != null ? operation.ToString() : "validation") + ": " + e.Message, this);
            Debug.LogException(e, this);
        }

        private bool CanAct() => services != null && !services.IsDisposed && services.State.IsGameplayActive;

        private void Emit(GlitchFeedbackKind kind, string message, Color color)
        {
            var feedback = new GlitchFeedback(kind, message, color);
            LastFeedback = feedback;
            LastFeedbackTime = Time.unscaledTime;
            Feedback?.Invoke(feedback);
        }

        private sealed class Loaded
        {
            public Loaded(GlitchDefinition definition, GlitchSpec spec, IGlitchEffect effect)
            {
                Definition = definition;
                Spec = spec;
                Effect = effect;
            }

            public GlitchDefinition Definition { get; }
            public GlitchSpec Spec { get; }
            public IGlitchEffect Effect { get; }
        }
    }
}
