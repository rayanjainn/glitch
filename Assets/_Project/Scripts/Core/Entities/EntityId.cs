using System;

namespace BugReport.Core.Entities
{
    /// <summary>
    /// Runtime identifier for a registered gameplay entity. Unique within one
    /// <see cref="EntityRegistry"/> lifetime (normally one loaded level).
    /// Not stable across sessions; the save system must use authored ids instead.
    /// </summary>
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public static readonly EntityId None = default;

        public readonly int Value;

        public EntityId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Entity ids are non-negative.");
            Value = value;
        }

        public bool IsValid => Value > 0;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;

        /// <summary>Diagnostic label shown in debug UI, e.g. "ENT-0007".</summary>
        public override string ToString() => IsValid ? "ENT-" + Value.ToString("0000") : "ENT-NONE";
    }
}
