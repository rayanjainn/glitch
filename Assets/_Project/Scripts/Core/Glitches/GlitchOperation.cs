using System;
using System.Collections.Generic;
using BugReport.Core.Entities;

namespace BugReport.Core.Glitches
{
    public enum OperationState
    {
        Active,
        Ended,
    }

    public enum GlitchEndReason
    {
        None = 0,
        /// <summary>Player reverted it (R) or code requested a normal revert.</summary>
        Reverted,
        /// <summary>Its duration ran out.</summary>
        Expired,
        /// <summary>A containment patch restored it (Milestone 4).</summary>
        Patched,
        /// <summary>A target was destroyed or unregistered.</summary>
        TargetLost,
        /// <summary>The level is unloading or restarting.</summary>
        SceneUnload,
    }

    public static class GlitchEndReasonExtensions
    {
        /// <summary>Forced endings cannot be vetoed by an effect (see <see cref="IGlitchEffect.CanEnd"/>).</summary>
        public static bool IsForced(this GlitchEndReason reason)
            => reason == GlitchEndReason.TargetLost || reason == GlitchEndReason.SceneUnload;
    }

    /// <summary>
    /// One applied glitch. Owns the modifier leases its effect created; ending the
    /// operation releases exactly those leases (newest first) and nothing else.
    /// </summary>
    public sealed class GlitchOperation
    {
        private readonly GameEntityId[] targets;
        private readonly List<IModifierLease> leases = new List<IModifierLease>();
        private readonly List<GameEntityId> products = new List<GameEntityId>();

        internal GlitchOperation(OperationId id, GlitchSpec spec, GameEntityId[] targets, float startTime)
        {
            Id = id;
            Spec = spec;
            this.targets = targets;
            StartTime = startTime;
            EndTime = spec.Duration > 0f ? startTime + spec.Duration : float.PositiveInfinity;
            State = OperationState.Active;
        }

        public OperationId Id { get; }
        public GlitchSpec Spec { get; }
        public float StartTime { get; }
        /// <summary>Expiry time, or +infinity if it never expires.</summary>
        public float EndTime { get; }
        public OperationState State { get; private set; }
        public GlitchEndReason EndReason { get; private set; }
        public bool IsActive => State == OperationState.Active;
        public int TargetCount => targets.Length;
        public int LeaseCount => leases.Count;
        /// <summary>Entities this operation created (e.g. duplicates). Losing one ends the operation.</summary>
        public int ProductCount => products.Count;

        /// <summary>Effect-specific data (e.g. which side of a transfer is which). Optional.</summary>
        public object EffectState { get; set; }

        public GameEntityId Target(int index) => targets[index];

        public bool Targets(GameEntityId id)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == id) return true;
            }
            return false;
        }

        public GameEntityId Product(int index) => products[index];

        public bool HasProduct(GameEntityId id) => products.Contains(id);

        /// <summary>True if <paramref name="id"/> is a target or a product of this operation.</summary>
        public bool Involves(GameEntityId id) => Targets(id) || HasProduct(id);

        /// <summary>Records an entity created by this operation. Its unregistration ends the operation (TargetLost).</summary>
        public void AddProduct(GameEntityId id)
        {
            if (!id.IsValid) throw new ArgumentException("Product id must be valid.", nameof(id));
            if (!IsActive) throw new InvalidOperationException(Id + " has already ended.");
            if (!products.Contains(id)) products.Add(id);
        }

        public float Remaining(float now) => float.IsPositiveInfinity(EndTime) ? float.PositiveInfinity : Math.Max(0f, EndTime - now);

        /// <summary>Registers a lease to be released when this operation ends.</summary>
        public void AddLease(IModifierLease lease)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (!IsActive) throw new InvalidOperationException(Id + " has already ended.");
            if (lease.Owner != Id) throw new ArgumentException("Lease belongs to " + lease.Owner + ", not " + Id + ".", nameof(lease));
            leases.Add(lease);
        }

        /// <summary>Releases every lease, newest first. One failing release does not stop the others.</summary>
        internal void ReleaseLeases(Action<Exception> onError)
        {
            for (int i = leases.Count - 1; i >= 0; i--)
            {
                try
                {
                    leases[i].Release();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e);
                }
            }
            leases.Clear();
        }

        internal void MarkEnded(GlitchEndReason reason)
        {
            State = OperationState.Ended;
            EndReason = reason;
        }

        public override string ToString() => Id + " " + Spec.Id;
    }
}
