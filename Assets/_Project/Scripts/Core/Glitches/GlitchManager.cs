using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Interaction;

namespace BugReport.Core.Glitches
{
    /// <summary>
    /// Owns every active glitch operation for one level: validation, application,
    /// expiry, reverting, and cleanup when targets disappear or the level unloads.
    ///
    /// Safety guarantees (all unit-tested):
    /// - An operation's changes are modifier leases; ending it releases only those.
    /// - A failed apply releases whatever the effect had already added.
    /// - Destroying/unregistering any target ends every operation on it (TargetLost).
    /// - An exception thrown by an effect never leaves an operation half-ended; it is
    ///   reported through <see cref="EffectError"/> and the bookkeeping completes.
    /// - Time is injected (<c>now</c> parameters), so pausing the game pauses expiry.
    /// </summary>
    public sealed class GlitchManager : IDisposable
    {
        private readonly EntityRegistry registry;
        private readonly List<Active> active = new List<Active>();
        private readonly Dictionary<string, float> cooldownEnds = new Dictionary<string, float>();
        private readonly List<Active> scratch = new List<Active>();
        private int nextId = 1;
        private bool disposed;

        public GlitchManager(EntityRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            registry.Unregistered += OnEntityUnregistered;
        }

        public event Action<GlitchOperation> OperationStarted;
        public event Action<GlitchOperation, GlitchEndReason> OperationEnded;
        /// <summary>An effect threw. The manager has already recovered; log it.</summary>
        public event Action<GlitchOperation, Exception> EffectError;

        public int ActiveCount => active.Count;

        private float cooldownScale = 1f;

        /// <summary>
        /// Multiplies cooldowns started from now on (containment makes glitches "more expensive"
        /// at higher stages). Clamped to at least 1: containment never makes glitches cheaper.
        /// </summary>
        public float CooldownScale
        {
            get => cooldownScale;
            set => cooldownScale = Math.Max(1f, value);
        }

        /// <summary>Sum of <see cref="GlitchSpec.ContainmentWeight"/> over active operations.</summary>
        public float ActiveContainmentWeight
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < active.Count; i++) sum += active[i].Operation.Spec.ContainmentWeight;
                return sum;
            }
        }

        /// <summary>Total entities currently created by active operations (duplicates).</summary>
        public int ActiveProductCount
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < active.Count; i++) sum += active[i].Operation.ProductCount;
                return sum;
            }
        }

        public GlitchOperation GetActive(int index) => active[index].Operation;

        public bool TryGet(OperationId id, out GlitchOperation operation)
        {
            int i = IndexOf(id);
            operation = i >= 0 ? active[i].Operation : null;
            return operation != null;
        }

        /// <summary>True if any active operation targets or created <paramref name="entity"/>.</summary>
        public bool IsInvolved(GameEntityId entity)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.Involves(entity)) return true;
            }
            return false;
        }

        /// <summary>Number of active operations of <paramref name="specId"/> whose slot-0 target is <paramref name="source"/>.</summary>
        public int CountActiveOn(string specId, GameEntityId source)
        {
            int n = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var op = active[i].Operation;
                if (op.Spec.Id == specId && op.TargetCount > 0 && op.Target(0) == source) n++;
            }
            return n;
        }

        public bool IsTargeted(GameEntityId entity)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.Targets(entity)) return true;
            }
            return false;
        }

        public int CountActive(string specId)
        {
            int n = 0;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.Spec.Id == specId) n++;
            }
            return n;
        }

        public float CooldownRemaining(string specId, float now)
            => cooldownEnds.TryGetValue(specId, out var end) ? Math.Max(0f, end - now) : 0f;

        /// <summary>Checks everything <see cref="TryApply"/> would, without changing anything.</summary>
        public GlitchResult Validate(GlitchSpec spec, IGlitchEffect effect, IReadOnlyList<GameEntityId> targets, float now)
            => Check(spec, effect, targets, now, new OperationId(nextId));

        public GlitchResult TryApply(GlitchSpec spec, IGlitchEffect effect, IReadOnlyList<GameEntityId> targets, float now)
        {
            var id = new OperationId(nextId);
            var check = Check(spec, effect, targets, now, id);
            if (!check.Succeeded) return check;

            nextId++;
            var copy = new GameEntityId[targets.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = targets[i];
            var operation = new GlitchOperation(id, spec, copy, now);

            bool applied;
            string failure;
            try
            {
                applied = effect.Apply(operation, out failure);
            }
            catch (Exception e)
            {
                applied = false;
                failure = "EFFECT ERROR";
                EffectError?.Invoke(operation, e);
            }

            if (!applied)
            {
                operation.ReleaseLeases(e => EffectError?.Invoke(operation, e)); // never leak a partial apply
                operation.MarkEnded(GlitchEndReason.None);
                return new GlitchResult(GlitchFailure.EffectFailed, operation, failure);
            }

            active.Add(new Active(operation, effect));
            if (spec.Cooldown > 0f) cooldownEnds[spec.Id] = now + spec.Cooldown * cooldownScale;
            OperationStarted?.Invoke(operation);
            return GlitchResult.Success(operation);
        }

        /// <summary>Ends operations whose duration has elapsed. Expiry an effect blocks is retried next tick.</summary>
        public void Tick(float now)
        {
            if (disposed) return;
            scratch.Clear();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.EndTime <= now) scratch.Add(active[i]);
            }
            for (int i = 0; i < scratch.Count; i++) End(scratch[i].Operation.Id, GlitchEndReason.Expired);
            scratch.Clear();
        }

        public GlitchResult Revert(OperationId id, GlitchEndReason reason = GlitchEndReason.Reverted) => End(id, reason);

        /// <summary>Reverts the most recently applied operation that is still active ("R").</summary>
        public GlitchResult RevertLatest(GlitchEndReason reason = GlitchEndReason.Reverted)
        {
            if (active.Count == 0) return new GlitchResult(GlitchFailure.NotFound);
            return End(active[active.Count - 1].Operation.Id, reason);
        }

        /// <summary>Ends every operation that targets or created <paramref name="entity"/>. Returns how many ended.</summary>
        public int EndAllTargeting(GameEntityId entity, GlitchEndReason reason)
        {
            scratch.Clear();
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.Involves(entity)) scratch.Add(active[i]);
            }
            int ended = 0;
            // Snapshot: ending one operation may end others (cascades), so re-check by id.
            var ids = new OperationId[scratch.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = scratch[i].Operation.Id;
            scratch.Clear();
            for (int i = ids.Length - 1; i >= 0; i--)
            {
                if (End(ids[i], reason).Succeeded) ended++;
            }
            return ended;
        }

        /// <summary>Ends everything, newest first. Forced reasons cannot be vetoed.</summary>
        public int EndAll(GlitchEndReason reason)
        {
            int ended = 0;
            var ids = new OperationId[active.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = active[i].Operation.Id;
            for (int i = ids.Length - 1; i >= 0; i--)
            {
                if (End(ids[i], reason).Succeeded) ended++;
            }
            return ended;
        }

        public void Dispose()
        {
            if (disposed) return;
            EndAll(GlitchEndReason.SceneUnload);
            disposed = true;
            registry.Unregistered -= OnEntityUnregistered;
            OperationStarted = null;
            OperationEnded = null;
            EffectError = null;
        }

        private GlitchResult Check(GlitchSpec spec, IGlitchEffect effect, IReadOnlyList<GameEntityId> targets, float now, OperationId pendingId)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (disposed) return new GlitchResult(GlitchFailure.ManagerDisposed);
            if (targets == null || targets.Count != spec.Arity) return new GlitchResult(GlitchFailure.WrongTargetCount);

            for (int i = 0; i < targets.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (targets[i] == targets[j]) return new GlitchResult(GlitchFailure.DuplicateTarget, targetIndex: i);
                }

                TargetValidationResult validation;
                if (!targets[i].IsValid) validation = new TargetValidationResult(TargetValidationFailure.NoTarget);
                else if (!registry.TryGet(targets[i], out var entity)) validation = new TargetValidationResult(TargetValidationFailure.Destroyed);
                else validation = TargetValidator.Validate(entity, spec.Slot(i));
                if (!validation.IsValid)
                    return new GlitchResult(GlitchFailure.InvalidTarget, targetIndex: i, targetValidation: validation, slotCount: spec.Arity);
            }

            float cooldown = CooldownRemaining(spec.Id, now);
            if (cooldown > 0f) return new GlitchResult(GlitchFailure.OnCooldown, detail: cooldown.ToString("0.0") + "s");
            if (spec.MaxActive > 0 && CountActive(spec.Id) >= spec.MaxActive) return new GlitchResult(GlitchFailure.LimitReached);

            string reason;
            try
            {
                reason = effect.Validate(new GlitchRequest(spec, targets, pendingId));
            }
            catch (Exception e)
            {
                EffectError?.Invoke(null, e);
                reason = "EFFECT ERROR";
            }
            return reason == null ? GlitchResult.Success(null) : new GlitchResult(GlitchFailure.EffectRejected, detail: reason);
        }

        private GlitchResult End(OperationId id, GlitchEndReason reason)
        {
            int index = IndexOf(id);
            if (index < 0) return new GlitchResult(GlitchFailure.NotFound);
            var entry = active[index];
            var operation = entry.Operation;

            if (!reason.IsForced())
            {
                string blocked;
                try
                {
                    blocked = entry.Effect.CanEnd(operation, reason);
                }
                catch (Exception e)
                {
                    EffectError?.Invoke(operation, e);
                    blocked = null; // a broken veto must not keep a glitch alive forever
                }
                if (blocked != null) return new GlitchResult(GlitchFailure.EndBlocked, operation, blocked);
            }

            // Remove first so re-entrant calls (cascades from released leases or OnEnded) see it as gone.
            active.RemoveAt(index);
            operation.MarkEnded(reason);
            operation.ReleaseLeases(e => EffectError?.Invoke(operation, e));

            try
            {
                entry.Effect.OnEnded(operation, reason);
            }
            catch (Exception e)
            {
                EffectError?.Invoke(operation, e);
            }

            OperationEnded?.Invoke(operation, reason);
            return GlitchResult.Success(operation);
        }

        private int IndexOf(OperationId id)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Operation.Id == id) return i;
            }
            return -1;
        }

        private void OnEntityUnregistered(GameEntityId id, IEntityDescriptor _)
        {
            if (!disposed) EndAllTargeting(id, GlitchEndReason.TargetLost);
        }

        private readonly struct Active
        {
            public readonly GlitchOperation Operation;
            public readonly IGlitchEffect Effect;

            public Active(GlitchOperation operation, IGlitchEffect effect)
            {
                Operation = operation;
                Effect = effect;
            }
        }
    }
}
