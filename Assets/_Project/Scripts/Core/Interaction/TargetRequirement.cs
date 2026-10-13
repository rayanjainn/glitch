using System;
using BugReport.Core.Entities;

namespace BugReport.Core.Interaction
{
    /// <summary>
    /// What a target must satisfy for an action (selection now, glitches later).
    /// Glitch definitions will produce one of these from their designer settings.
    /// </summary>
    [Serializable]
    public readonly struct TargetRequirement
    {
        public readonly EntityCapabilities RequiredCapabilities;
        /// <summary>Bit set of <see cref="EntityCategory"/> values that are rejected (bit = 1 &lt;&lt; (int)category).</summary>
        public readonly int ExcludedCategoryMask;

        public TargetRequirement(EntityCapabilities requiredCapabilities, params EntityCategory[] excludedCategories)
        {
            RequiredCapabilities = requiredCapabilities;
            int mask = 0;
            if (excludedCategories != null)
            {
                for (int i = 0; i < excludedCategories.Length; i++) mask |= CategoryBit(excludedCategories[i]);
            }
            ExcludedCategoryMask = mask;
        }

        public bool Excludes(EntityCategory category) => (ExcludedCategoryMask & CategoryBit(category)) != 0;

        public static int CategoryBit(EntityCategory category) => 1 << (int)category;

        /// <summary>Default rule for plain selection: inspectable, and never the anomaly itself.</summary>
        public static readonly TargetRequirement Selection = new TargetRequirement(EntityCapabilities.Inspectable, EntityCategory.Anomaly);
    }
}
