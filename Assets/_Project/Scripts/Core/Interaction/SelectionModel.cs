using System;
using BugReport.Core.Entities;

namespace BugReport.Core.Interaction
{
    /// <summary>
    /// Engine-independent hover/selection state for the anomaly's targeting.
    ///
    /// Holds entity ids, never object references, and listens to the registry so
    /// a destroyed or unregistered entity can never stay selected.
    /// </summary>
    public sealed class SelectionModel : IDisposable
    {
        private readonly EntityRegistry registry;
        private TargetRequirement requirement;
        private bool disposed;

        public SelectionModel(EntityRegistry registry, TargetRequirement requirement)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.requirement = requirement;
            registry.Unregistered += OnEntityUnregistered;
            HoveredValidation = new TargetValidationResult(TargetValidationFailure.NoTarget);
        }

        public GameEntityId Hovered { get; private set; }
        public GameEntityId Selected { get; private set; }
        public TargetRequirement Requirement => requirement;

        /// <summary>Validation of the hovered entity against the current requirement.</summary>
        public TargetValidationResult HoveredValidation { get; private set; }

        /// <summary>(previous, current) selected ids.</summary>
        public event Action<GameEntityId, GameEntityId> SelectionChanged;
        /// <summary>(previous, current) hovered ids.</summary>
        public event Action<GameEntityId, GameEntityId> HoverChanged;

        public bool HasSelection => Selected.IsValid;

        /// <summary>
        /// Changes the active requirement (e.g. when a glitch is chosen later).
        /// Revalidates the current selection and drops it if it no longer qualifies.
        /// </summary>
        public void SetRequirement(TargetRequirement newRequirement)
        {
            requirement = newRequirement;
            RefreshHoverValidation();
            if (Selected.IsValid && !Validate(Selected).IsValid) ClearSelection();
        }

        /// <summary>Called every frame by the scanner. Passing <see cref="GameEntityId.None"/> clears the hover.</summary>
        public void SetHovered(GameEntityId id)
        {
            var previous = Hovered;
            Hovered = id;
            RefreshHoverValidation();
            if (previous != id) HoverChanged?.Invoke(previous, id);
        }

        public TargetValidationResult Validate(GameEntityId id)
        {
            if (!id.IsValid) return new TargetValidationResult(TargetValidationFailure.NoTarget);
            if (!registry.TryGet(id, out var entity)) return new TargetValidationResult(TargetValidationFailure.Destroyed);
            return TargetValidator.Validate(entity, requirement);
        }

        /// <summary>Selects <paramref name="id"/> if it passes validation. Selection is unchanged on failure.</summary>
        public TargetValidationResult TrySelect(GameEntityId id)
        {
            var result = Validate(id);
            if (!result.IsValid) return result;
            if (Selected != id)
            {
                var previous = Selected;
                Selected = id;
                SelectionChanged?.Invoke(previous, id);
            }
            return result;
        }

        public TargetValidationResult TrySelectHovered() => TrySelect(Hovered);

        /// <summary>Returns true if something was selected.</summary>
        public bool ClearSelection()
        {
            if (!Selected.IsValid) return false;
            var previous = Selected;
            Selected = GameEntityId.None;
            SelectionChanged?.Invoke(previous, GameEntityId.None);
            return true;
        }

        public bool TryGetSelected(out IEntityDescriptor entity)
        {
            if (Selected.IsValid && registry.TryGet(Selected, out entity)) return true;
            entity = null;
            return false;
        }

        public bool TryGetHovered(out IEntityDescriptor entity)
        {
            if (Hovered.IsValid && registry.TryGet(Hovered, out entity)) return true;
            entity = null;
            return false;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            registry.Unregistered -= OnEntityUnregistered;
            SelectionChanged = null;
            HoverChanged = null;
        }

        private void RefreshHoverValidation() => HoveredValidation = Validate(Hovered);

        private void OnEntityUnregistered(GameEntityId id, IEntityDescriptor _)
        {
            if (Hovered == id) SetHovered(GameEntityId.None);
            if (Selected == id) ClearSelection();
        }
    }
}
