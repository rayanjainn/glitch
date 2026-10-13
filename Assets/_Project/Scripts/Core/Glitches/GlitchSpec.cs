using System;
using System.Collections.Generic;
using BugReport.Core.Interaction;

namespace BugReport.Core.Glitches
{
    /// <summary>
    /// Engine-independent description of a glitch, produced from the designer's
    /// <c>GlitchDefinition</c> ScriptableObject. One requirement per target slot.
    /// </summary>
    public sealed class GlitchSpec
    {
        private readonly TargetRequirement[] slots;

        public GlitchSpec(string id, string displayName, IReadOnlyList<TargetRequirement> slots,
            float duration = 0f, float cooldown = 0f, int maxActive = 0, float containmentWeight = 1f)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Glitch id is required.", nameof(id));
            if (slots == null || slots.Count == 0) throw new ArgumentException("A glitch needs at least one target slot.", nameof(slots));
            if (duration < 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            if (cooldown < 0f) throw new ArgumentOutOfRangeException(nameof(cooldown));
            if (maxActive < 0) throw new ArgumentOutOfRangeException(nameof(maxActive));
            if (containmentWeight < 0f) throw new ArgumentOutOfRangeException(nameof(containmentWeight));

            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            this.slots = new TargetRequirement[slots.Count];
            for (int i = 0; i < slots.Count; i++) this.slots[i] = slots[i];
            Duration = duration;
            Cooldown = cooldown;
            MaxActive = maxActive;
            ContainmentWeight = containmentWeight;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Arity => slots.Length;
        /// <summary>Seconds until the operation expires. 0 = lasts until reverted.</summary>
        public float Duration { get; }
        /// <summary>Seconds after a successful apply before this glitch can be applied again.</summary>
        public float Cooldown { get; }
        /// <summary>Maximum simultaneously active operations of this glitch. 0 = unlimited.</summary>
        public int MaxActive { get; }
        /// <summary>How suspicious containment finds this glitch (impulse on apply, pressure while active).</summary>
        public float ContainmentWeight { get; }

        public TargetRequirement Slot(int index) => slots[index];
    }
}
