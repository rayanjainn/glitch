using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Slot 0 (source) must currently be solid, slot 1 (target) intangible. Applying
    /// adds two exclusive modifiers: source → Intangible, target → Solid. Reverting
    /// releases them. Safety checks:
    /// - the target must not be occupied when it turns solid;
    /// - a normal revert waits while something is inside the source (it would turn solid around it);
    /// - dynamic rigidbodies cannot be sources (they would fall through the floor).
    /// </summary>
    public sealed class CollisionTransferEffect : IGlitchEffect
    {
        private readonly EntityRegistry registry;
        // Validate runs every frame for the reticle preview: warn about misconfiguration once per entity.
        private readonly HashSet<GameEntity> warnedMisconfigured = new HashSet<GameEntity>();

        public CollisionTransferEffect(EntityRegistry registry)
        {
            this.registry = registry;
        }

        public string Validate(GlitchRequest request)
        {
            if (!TryResolve(request.Targets[0], out var source, out var sourceName))
                return "SOURCE " + sourceName + " HAS NO COLLISION CAPABILITY";
            if (!TryResolve(request.Targets[1], out var target, out var targetName))
                return "TARGET " + targetName + " HAS NO COLLISION CAPABILITY";

            if (source.EffectiveMode != CollisionMode.Solid) return "SOURCE IS ALREADY INTANGIBLE";
            if (target.EffectiveMode != CollisionMode.Intangible) return "TARGET IS ALREADY SOLID";
            if (!source.Channel.CanAdd(request.PendingId)) return "SOURCE COLLISION IS ALREADY GLITCHED";
            if (!target.Channel.CanAdd(request.PendingId)) return "TARGET COLLISION IS ALREADY GLITCHED";
            if (source.HasDynamicBody) return "SOURCE UNSTABLE: MOVING OBJECTS CANNOT LOSE COLLISION";
            if (source.Colliders.Count == 0) return "SOURCE HAS NO COLLIDERS";
            if (target.Colliders.Count == 0) return "TARGET HAS NO COLLIDERS";

            if (PhysicsOccupancy.IsOccupied(target.Colliders, GameLayers.OccupantMask, out var occupant))
                return "TARGET OCCUPIED BY " + Describe(occupant);
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

            if (!source.Channel.TryAdd(operation.Id, CollisionMode.Intangible, out var sourceLease))
            {
                failure = "SOURCE COLLISION IS ALREADY GLITCHED";
                return false;
            }
            operation.AddLease(sourceLease);

            if (!target.Channel.TryAdd(operation.Id, CollisionMode.Solid, out var targetLease))
            {
                failure = "TARGET COLLISION IS ALREADY GLITCHED";
                return false; // the manager releases the source lease
            }
            operation.AddLease(targetLease);
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason)
        {
            // Ending turns the source solid again. Wait if something is inside it.
            if (!TryResolve(operation.Target(0), out var source, out var sourceName)) return null;
            if (PhysicsOccupancy.IsOccupied(source.Colliders, GameLayers.OccupantMask, out var occupant))
                return "CANNOT RESTORE " + sourceName + ": " + Describe(occupant) + " IS INSIDE";
            return null;
        }

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
        }

        private bool TryResolve(GameEntityId id, out CollisionCapability capability, out string displayName)
        {
            capability = null;
            displayName = id.ToString();
            if (!registry.TryGet(id, out var descriptor)) return false;
            displayName = descriptor.DisplayName;
            var entity = descriptor as GameEntity;
            if (entity == null) return false;
            capability = entity.GetComponent<CollisionCapability>();
            if (capability == null)
            {
                if (warnedMisconfigured.Add(entity))
                    Debug.LogWarning("[BUG REPORT] " + displayName + " declares CollisionTransferable but has no CollisionCapability component.", entity);
                return false;
            }
            return true;
        }

        private static string Describe(Collider occupant)
        {
            if (occupant == null) return "SOMETHING";
            var entity = occupant.GetComponentInParent<GameEntity>();
            return entity != null ? entity.DisplayName : occupant.name.ToUpperInvariant();
        }
    }
}
