using System;

namespace BugReport.Core.Glitches
{
    /// <summary>Identifies one applied glitch operation. Never reused within a <see cref="GlitchManager"/>.</summary>
    public readonly struct OperationId : IEquatable<OperationId>
    {
        public static readonly OperationId None = default;

        public readonly int Value;

        public OperationId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public bool IsValid => Value > 0;

        public bool Equals(OperationId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is OperationId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(OperationId a, OperationId b) => a.Value == b.Value;
        public static bool operator !=(OperationId a, OperationId b) => a.Value != b.Value;
        public override string ToString() => IsValid ? "OP-" + Value.ToString("000") : "OP-NONE";
    }
}
