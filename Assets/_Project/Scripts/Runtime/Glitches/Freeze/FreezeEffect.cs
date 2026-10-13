using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Adds an exclusive "frozen" modifier to the target's <see cref="FreezeCapability"/>.
    /// The velocity at freeze time is stored in the operation (not on the object), so only
    /// the operation that froze it can hand momentum back, and only if the definition says so.
    /// </summary>
    public sealed class FreezeEffect : IGlitchEffect
    {
        private readonly EntityRegistry registry;
        private readonly FreezeDefinition definition;
        private readonly HashSet<GameEntity> warnedMisconfigured = new HashSet<GameEntity>();

        public FreezeEffect(EntityRegistry registry, FreezeDefinition definition)
        {
            this.registry = registry;
            this.definition = definition;
        }

        private sealed class Snapshot
        {
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
        }

        public string Validate(GlitchRequest request)
        {
            if (!TryResolve(request.Targets[0], out var capability, out var targetName))
                return targetName + " HAS NO FREEZE CAPABILITY";
            if (!capability.Channel.CanAdd(request.PendingId)) return targetName + " IS ALREADY FROZEN";
            if (!capability.HasAnythingToFreeze) return targetName + " HAS NOTHING TO FREEZE";
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            failure = null;
            if (!TryResolve(operation.Target(0), out var capability, out var targetName))
            {
                failure = "TARGET LOST";
                return false;
            }

            var body = capability.Body;
            if (body != null && !body.isKinematic)
            {
                operation.EffectState = new Snapshot { LinearVelocity = body.linearVelocity, AngularVelocity = body.angularVelocity };
            }

            if (!capability.Channel.TryAdd(operation.Id, true, out var lease))
            {
                failure = targetName + " IS ALREADY FROZEN";
                return false;
            }
            operation.AddLease(lease);
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason) => null;

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
            if (definition == null || definition.Release != FreezeRelease.RestoreMomentum) return;
            if (reason.IsForced()) return;
            if (!(operation.EffectState is Snapshot snapshot)) return;
            if (!TryResolve(operation.Target(0), out var capability, out _)) return;

            var body = capability.Body;
            if (body == null || body.isKinematic || capability.IsFrozen) return;
            body.linearVelocity = Vector3.ClampMagnitude(snapshot.LinearVelocity, definition.MaxReleaseSpeed);
            body.angularVelocity = snapshot.AngularVelocity;
        }

        private bool TryResolve(GameEntityId id, out FreezeCapability capability, out string displayName)
        {
            capability = null;
            displayName = id.ToString();
            if (!registry.TryGet(id, out var descriptor)) return false;
            displayName = descriptor.DisplayName;
            var entity = descriptor as GameEntity;
            if (entity == null) return false;
            capability = entity.GetComponent<FreezeCapability>();
            if (capability == null && warnedMisconfigured.Add(entity))
                Debug.LogWarning("[BUG REPORT] " + displayName + " declares Freezable but has no FreezeCapability component.", entity);
            return capability != null;
        }
    }
}
