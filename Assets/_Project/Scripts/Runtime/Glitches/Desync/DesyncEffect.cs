using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Moves the target's collision away from its visible mesh. Refused if someone is standing where
    /// the collision would land, and refused to revert while someone stands where it would snap back.
    /// </summary>
    public sealed class DesyncEffect : IGlitchEffect
    {
        private static readonly Collider[] Hits = new Collider[16];
        private readonly EntityRegistry registry;
        private readonly HashSet<GameEntity> warned = new HashSet<GameEntity>();

        public DesyncEffect(EntityRegistry registry) => this.registry = registry;

        public string Validate(GlitchRequest request)
        {
            if (!TryResolve(request.Targets[0], out var capability, out var name)) return name + " HAS NO DESYNC CAPABILITY";
            if (capability.Colliders.Length == 0) return name + " HAS NO COLLISION TO MOVE";
            if (!capability.Channel.CanAdd(request.PendingId)) return name + " IS ALREADY DESYNCED";
            var blocker = BlockerAt(capability, capability.transform.TransformVector(capability.Offset));
            if (blocker != null) return "DESTINATION OCCUPIED BY " + Describe(blocker);
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            failure = null;
            if (!TryResolve(operation.Target(0), out var capability, out var name))
            {
                failure = "TARGET LOST";
                return false;
            }
            if (!capability.Channel.TryAdd(operation.Id, capability.Offset, out var lease))
            {
                failure = name + " IS ALREADY DESYNCED";
                return false;
            }
            operation.AddLease(lease);
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason)
        {
            if (!TryResolve(operation.Target(0), out var capability, out var name)) return null;
            // Back to the rest position = shifted by minus the current displacement.
            var displacement = capability.transform.TransformVector(capability.Offset);
            var blocker = BlockerAt(capability, -displacement);
            return blocker != null ? "CANNOT RESYNC " + name + ": " + Describe(blocker) + " IS IN THE WAY" : null;
        }

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
        }

        /// <summary>The first hero/anomaly/prop that would overlap the collision after moving it by <paramref name="worldMove"/>.</summary>
        private static Collider BlockerAt(DesyncCapability capability, Vector3 worldMove)
        {
            var bounds = capability.CollisionBounds();
            int count = Physics.OverlapBoxNonAlloc(bounds.center + worldMove, bounds.extents * 0.96f, Hits, Quaternion.identity,
                GameLayers.OccupantMask, QueryTriggerInteraction.Ignore);
            var own = capability.Colliders;
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit == null || System.Array.IndexOf(own, hit) >= 0) continue;
                if (hit.transform.IsChildOf(capability.transform)) continue;
                return hit;
            }
            return null;
        }

        private static string Describe(Collider c)
        {
            var entity = c != null ? c.GetComponentInParent<GameEntity>() : null;
            return entity != null ? entity.DisplayName : "SOMETHING";
        }

        private bool TryResolve(GameEntityId id, out DesyncCapability capability, out string displayName)
        {
            capability = null;
            displayName = id.ToString();
            if (!registry.TryGet(id, out var descriptor)) return false;
            displayName = descriptor.DisplayName;
            var entity = descriptor as GameEntity;
            if (entity == null) return false;
            capability = entity.GetComponent<DesyncCapability>();
            if (capability == null && warned.Add(entity))
                Debug.LogWarning("[BUG REPORT] " + displayName + " declares Desyncable but has no DesyncCapability component.", entity);
            return capability != null;
        }
    }
}
