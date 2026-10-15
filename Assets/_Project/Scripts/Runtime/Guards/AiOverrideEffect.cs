using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.AI;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Guards
{
    /// <summary>
    /// AI Override: SOURCE (a guard) investigates TARGET (a decoy) for the duration, then returns to
    /// its route. A temporary behaviour-state change only (GDD 4.5); it ends when reverted, expired
    /// or patched.
    /// </summary>
    public sealed class AiOverrideEffect : IGlitchEffect
    {
        private readonly EntityRegistry registry;
        private readonly HashSet<GameEntity> warned = new HashSet<GameEntity>();

        public AiOverrideEffect(EntityRegistry registry) => this.registry = registry;

        public string Validate(GlitchRequest request)
        {
            if (!TryResolveGuard(request.Targets[0], out var guard, out var guardName)) return guardName + " IS NOT AI-CONTROLLABLE";
            if (!registry.TryGet(request.Targets[1], out var decoy)) return "DECOY LOST";
            if (guard.IsFrozen) return guardName + " IS FROZEN";
            if (guard.Brain.State == GuardState.Disabled) return guardName + " IS DISABLED";
            if (!guard.OverrideChannel.CanAdd(request.PendingId)) return guardName + " IS ALREADY OVERRIDDEN";
            if (decoy is GameEntity e && e.GetComponent<GuardController>() != null) return "A GUARD CANNOT BE A DECOY";
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            failure = null;
            if (!TryResolveGuard(operation.Target(0), out var guard, out var guardName) || !registry.TryGet(operation.Target(1), out var decoy))
            {
                failure = "TARGET LOST";
                return false;
            }
            if (!guard.OverrideChannel.TryAdd(operation.Id, true, out var lease))
            {
                failure = guardName + " IS ALREADY OVERRIDDEN";
                return false;
            }
            operation.AddLease(lease);
            var position = (decoy as GameEntity) != null ? ((GameEntity)decoy).transform.position : guard.transform.position;
            guard.Distract(position);
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason) => null;

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
            if (TryResolveGuard(operation.Target(0), out var guard, out _)) guard.ClearOverride();
        }

        private bool TryResolveGuard(GameEntityId id, out GuardController guard, out string displayName)
        {
            guard = null;
            displayName = id.ToString();
            if (!registry.TryGet(id, out var descriptor)) return false;
            displayName = descriptor.DisplayName;
            var entity = descriptor as GameEntity;
            if (entity == null) return false;
            guard = entity.GetComponent<GuardController>();
            if (guard == null && warned.Add(entity))
                Debug.LogWarning("[BUG REPORT] " + displayName + " declares AIControllable but has no GuardController component.", entity);
            return guard != null;
        }
    }
}
