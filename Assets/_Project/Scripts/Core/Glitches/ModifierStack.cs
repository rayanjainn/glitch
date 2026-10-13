using System;
using System.Collections.Generic;

namespace BugReport.Core.Glitches
{
    public enum ConflictPolicy
    {
        /// <summary>Only one owner may modify the channel at a time; others are rejected.</summary>
        Exclusive,
        /// <summary>Any number of owners; the highest priority wins, ties go to the most recent.</summary>
        Stack,
    }

    /// <summary>A claim on a modifier channel. Releasing it removes exactly that modifier. Idempotent.</summary>
    public interface IModifierLease
    {
        OperationId Owner { get; }
        bool IsReleased { get; }
        void Release();
    }

    /// <summary>
    /// One modifiable aspect of an object (collision mode, kinematic flag, mass, ...).
    ///
    /// Reversibility rule: glitches never snapshot and restore values. They add a
    /// modifier and later release it; the effective value is always recomputed from
    /// <see cref="Base"/> plus the remaining modifiers. So reverting one glitch
    /// cannot undo another glitch's change or a gameplay change made to the base
    /// in the meantime.
    /// </summary>
    public sealed class ModifierStack<T>
    {
        private readonly List<Entry> entries = new List<Entry>();
        private readonly IEqualityComparer<T> comparer;
        private readonly Action<T> onEffectiveChanged;
        private T baseValue;
        private int sequence;

        public ModifierStack(T baseValue, ConflictPolicy policy, Action<T> onEffectiveChanged = null, IEqualityComparer<T> comparer = null)
        {
            this.baseValue = baseValue;
            Policy = policy;
            this.onEffectiveChanged = onEffectiveChanged;
            this.comparer = comparer ?? EqualityComparer<T>.Default;
            Effective = baseValue;
        }

        public ConflictPolicy Policy { get; }
        public T Effective { get; private set; }
        public int Count => entries.Count;
        public bool IsModified => entries.Count > 0;

        /// <summary>Authoritative unmodified value. Changing it re-evaluates the effective value.</summary>
        public T Base
        {
            get => baseValue;
            set
            {
                baseValue = value;
                Recompute();
            }
        }

        /// <summary>Raised after <see cref="Effective"/> changes (in addition to the constructor callback).</summary>
        public event Action<T> EffectiveChanged;

        public bool IsModifiedBy(OperationId owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner == owner) return true;
            }
            return false;
        }

        /// <summary>Whether <paramref name="owner"/> could add a modifier without a conflict.</summary>
        public bool CanAdd(OperationId owner)
        {
            if (!owner.IsValid) return false;
            if (Policy == ConflictPolicy.Stack) return true;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner != owner) return false;
            }
            return true;
        }

        public bool TryAdd(OperationId owner, T value, out IModifierLease lease, int priority = 0)
        {
            lease = null;
            if (!CanAdd(owner)) return false;
            var entry = new Entry(this, owner, value, priority, ++sequence);
            entries.Add(entry);
            Recompute();
            lease = entry;
            return true;
        }

        private void Remove(Entry entry)
        {
            if (entries.Remove(entry)) Recompute();
        }

        private void Recompute()
        {
            var next = baseValue;
            Entry winner = null;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (winner == null || e.Priority > winner.Priority || (e.Priority == winner.Priority && e.Sequence > winner.Sequence))
                    winner = e;
            }
            if (winner != null) next = winner.Value;

            if (comparer.Equals(next, Effective)) return;
            Effective = next;
            onEffectiveChanged?.Invoke(next);
            EffectiveChanged?.Invoke(next);
        }

        private sealed class Entry : IModifierLease
        {
            private readonly ModifierStack<T> stack;

            public Entry(ModifierStack<T> stack, OperationId owner, T value, int priority, int sequence)
            {
                this.stack = stack;
                Owner = owner;
                Value = value;
                Priority = priority;
                Sequence = sequence;
            }

            public OperationId Owner { get; }
            public T Value { get; }
            public int Priority { get; }
            public int Sequence { get; }
            public bool IsReleased { get; private set; }

            public void Release()
            {
                if (IsReleased) return;
                IsReleased = true;
                stack.Remove(this);
            }
        }
    }
}
