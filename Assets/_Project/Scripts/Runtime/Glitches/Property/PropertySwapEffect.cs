using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Core.Properties;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>Swaps one property's value between SOURCE and TARGET until the glitch ends.</summary>
    public sealed class PropertySwapEffect : IGlitchEffect
    {
        private readonly EntityRegistry registry;
        private readonly SwapProperty property;
        private readonly HashSet<GameEntity> warned = new HashSet<GameEntity>();

        public PropertySwapEffect(EntityRegistry registry, SwapProperty property)
        {
            this.registry = registry;
            this.property = property;
        }

        public string Validate(GlitchRequest request)
        {
            if (!TryResolve(request.Targets[0], out var source, out var sourceName)) return "SOURCE " + sourceName + " HAS NO " + Label() + " TO SWAP";
            if (!TryResolve(request.Targets[1], out var target, out var targetName)) return "TARGET " + targetName + " HAS NO " + Label() + " TO SWAP";
            if (!source.Channel(property).CanAdd(request.PendingId)) return "SOURCE " + Label() + " IS ALREADY GLITCHED";
            if (!target.Channel(property).CanAdd(request.PendingId)) return "TARGET " + Label() + " IS ALREADY GLITCHED";
            if (!PropertySwapMath.TrySwap(property, source.Value(property), target.Value(property), out _, out _))
                return "NOTHING TO SWAP: SAME " + Label();
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            failure = null;
            if (!TryResolve(operation.Target(0), out var source, out _) || !TryResolve(operation.Target(1), out var target, out _))
            {
                failure = "TARGET LOST";
                return false;
            }
            if (!PropertySwapMath.TrySwap(property, source.Value(property), target.Value(property), out var newSource, out var newTarget))
            {
                failure = "NOTHING TO SWAP";
                return false;
            }
            if (!source.Channel(property).TryAdd(operation.Id, newSource, out var sourceLease))
            {
                failure = "SOURCE " + Label() + " IS ALREADY GLITCHED";
                return false;
            }
            operation.AddLease(sourceLease);
            if (!target.Channel(property).TryAdd(operation.Id, newTarget, out var targetLease))
            {
                failure = "TARGET " + Label() + " IS ALREADY GLITCHED";
                return false; // the manager releases the source lease
            }
            operation.AddLease(targetLease);
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason) => null;

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
        }

        private string Label() => property == SwapProperty.Mass ? "MASS" : "SPEED";

        private bool TryResolve(GameEntityId id, out PropertyCapability capability, out string displayName)
        {
            capability = null;
            displayName = id.ToString();
            if (!registry.TryGet(id, out var descriptor)) return false;
            displayName = descriptor.DisplayName;
            var entity = descriptor as GameEntity;
            if (entity == null) return false;
            capability = entity.GetComponent<PropertyCapability>();
            if (capability == null)
            {
                if (warned.Add(entity)) Debug.LogWarning("[BUG REPORT] " + displayName + " declares PropertyMutable but has no PropertyCapability component.", entity);
                return false;
            }
            return capability.Has(property);
        }
    }
}
