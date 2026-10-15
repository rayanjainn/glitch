using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/Desync", fileName = "GL_Desync")]
    public sealed class DesyncDefinition : GlitchDefinition
    {
        public static readonly Color Lime = new Color(0.6f, 1f, 0.35f);

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 1 ? null : "needs exactly 1 slot (TARGET), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new DesyncEffect(services.Registry);

        public void ApplyDefaults()
        {
            Configure("desync", "DESYNC",
                "What you see and what is real come apart. The outline shows where the collision actually is.",
                Lime, newDuration: 20f, newCooldown: 0.5f, newMaxActive: 2,
                new[] { new TargetSlot("TARGET", EntityCapabilities.Inspectable | EntityCapabilities.Desyncable, EntityCategory.Anomaly, EntityCategory.Hero) });
        }
    }
}
