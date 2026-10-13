using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>DUPLICATION: creates a copy of a designated puzzle object next to it.</summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/Duplication", fileName = "GL_Duplication")]
    public sealed class DuplicationDefinition : GlitchDefinition
    {
        public static readonly Color Magenta = new Color(1f, 0.25f, 0.85f);

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 1 ? null : "needs exactly 1 slot (SOURCE), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new DuplicationEffect(services, Id);

        public void ApplyDefaults()
        {
            // Max active doubles as the global copy cap.
            Configure("duplication", "DUPLICATION",
                "Creates a copy of a designated object. Copies vanish when reverted.",
                Magenta, newDuration: 0f, newCooldown: 0.5f, newMaxActive: 4,
                new[] { new TargetSlot("SOURCE", EntityCapabilities.Inspectable | EntityCapabilities.Duplicable, EntityCategory.Anomaly, EntityCategory.Hero) });
        }
    }
}
