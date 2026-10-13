using System;
using BugReport.Core.Entities;
using BugReport.Core.Interaction;
using UnityEngine;

namespace BugReport.Interaction
{
    public enum SelectionFeedbackKind
    {
        Selected,
        Deselected,
        Rejected,
    }

    /// <summary>Emitted for UI/audio. <see cref="Message"/> is short, player-facing text.</summary>
    public readonly struct SelectionFeedback
    {
        public readonly SelectionFeedbackKind Kind;
        public readonly GameEntityId Target;
        public readonly string Message;

        public SelectionFeedback(SelectionFeedbackKind kind, GameEntityId target, string message)
        {
            Kind = kind;
            Target = target;
            Message = message;
        }
    }

    /// <summary>
    /// Bridges the <see cref="TargetScanner"/> (physics) to the engine-independent
    /// <see cref="SelectionModel"/> and turns select/cancel requests into feedback.
    /// Does not read input itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectionController : MonoBehaviour
    {
        [SerializeField] private TargetScanner scanner;

        private GameServices services;
        private Action<GameServices> bindCallback;

        public event Action<SelectionFeedback> Feedback;

        public TargetScanner Scanner => scanner;
        public SelectionFeedback LastFeedback { get; private set; }
        public float LastFeedbackTime { get; private set; } = float.NegativeInfinity;

        public void Configure(TargetScanner targetScanner) => scanner = targetScanner;

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
        }

        private void Update()
        {
            if (services == null || services.IsDisposed || !services.State.IsGameplayActive) return;
            var target = scanner != null ? scanner.CurrentTarget : null;
            services.Selection.SetHovered(target != null && target.IsRegistered ? target.Id : GameEntityId.None);
        }

        /// <summary>Select what the reticle is on; selecting the current selection again deselects it.</summary>
        public void SelectHovered()
        {
            if (!CanAct()) return;
            var selection = services.Selection;

            if (selection.Hovered.IsValid && selection.Hovered == selection.Selected)
            {
                CancelSelection();
                return;
            }

            var hovered = selection.Hovered;
            var result = selection.TrySelectHovered();
            if (result.IsValid)
            {
                selection.TryGetSelected(out var entity);
                Emit(new SelectionFeedback(SelectionFeedbackKind.Selected, hovered, "SELECTED: " + (entity != null ? entity.DisplayName : hovered.ToString())));
            }
            else
            {
                Emit(new SelectionFeedback(SelectionFeedbackKind.Rejected, hovered, result.Describe()));
            }
        }

        public void CancelSelection()
        {
            if (!CanAct()) return;
            var previous = services.Selection.Selected;
            if (services.Selection.ClearSelection())
                Emit(new SelectionFeedback(SelectionFeedbackKind.Deselected, previous, "SELECTION CLEARED"));
        }

        private bool CanAct() => services != null && !services.IsDisposed && services.State.IsGameplayActive;

        private void Emit(SelectionFeedback feedback)
        {
            LastFeedback = feedback;
            LastFeedbackTime = Time.unscaledTime;
            Feedback?.Invoke(feedback);
        }
    }
}
