using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Core.Properties;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>One definition per swappable property (MASS SWAP, SPEED SWAP), amber (GDD 7.2).</summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/Property Swap", fileName = "GL_PropertySwap")]
    public sealed class PropertySwapDefinition : GlitchDefinition
    {
        public static readonly Color Amber = new Color(1f, 0.72f, 0.2f);

        [Header("Property Swap")]
        [SerializeField] private SwapProperty property = SwapProperty.Mass;

        public SwapProperty Property => property;

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 2 ? null : "needs exactly 2 slots (SOURCE, TARGET), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new PropertySwapEffect(services.Registry, property);

        public void ApplyDefaults(SwapProperty swapProperty)
        {
            property = swapProperty;
            bool mass = swapProperty == SwapProperty.Mass;
            var caps = EntityCapabilities.Inspectable | EntityCapabilities.PropertyMutable;
            Configure(mass ? "mass_swap" : "speed_swap", mass ? "MASS SWAP" : "SPEED SWAP",
                mass ? "Two objects trade their weight. Light things become heavy; heavy things become light."
                     : "Two machines trade their pace. A slow thing gets quick; a quick thing gets slow.",
                Amber, newDuration: 30f, newCooldown: 0.5f, newMaxActive: 2,
                new[]
                {
                    new TargetSlot("SOURCE", caps, EntityCategory.Anomaly, EntityCategory.Hero),
                    new TargetSlot("TARGET", caps, EntityCategory.Anomaly, EntityCategory.Hero),
                });
        }
    }
}
