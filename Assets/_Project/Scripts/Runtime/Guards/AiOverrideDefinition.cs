using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Guards
{
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/AI Override", fileName = "GL_AiOverride")]
    public sealed class AiOverrideDefinition : GlitchDefinition
    {
        public static readonly Color Violet = new Color(0.72f, 0.45f, 1f);

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 2 ? null : "needs exactly 2 slots (GUARD, DECOY), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new AiOverrideEffect(services.Registry);

        public void ApplyDefaults()
        {
            Configure("ai_override", "AI OVERRIDE",
                "Point a guard at a decoy. It goes to investigate, forgets about its patrol for a while, then goes back.",
                Violet, newDuration: 14f, newCooldown: 0.5f, newMaxActive: 2,
                new[]
                {
                    new TargetSlot("GUARD", EntityCapabilities.Inspectable | EntityCapabilities.AIControllable, EntityCategory.Anomaly, EntityCategory.Hero),
                    new TargetSlot("DECOY", EntityCapabilities.Inspectable, EntityCategory.Anomaly, EntityCategory.Hero, EntityCategory.Enemy),
                });
        }
    }
}
