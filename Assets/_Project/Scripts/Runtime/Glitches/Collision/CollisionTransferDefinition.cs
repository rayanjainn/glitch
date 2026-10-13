using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// COLLISION TRANSFER: the source's collision moves to the target.
    /// The source becomes intangible; the target becomes solid.
    /// </summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/Collision Transfer", fileName = "GL_CollisionTransfer")]
    public sealed class CollisionTransferDefinition : GlitchDefinition
    {
        public static readonly Color ElectricBlue = new Color(0.15f, 0.5f, 1f);

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 2 ? null : "needs exactly 2 slots (SOURCE, TARGET), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new CollisionTransferEffect(services.Registry);

        /// <summary>Default settings used by the editor tooling when it creates the asset.</summary>
        public void ApplyDefaults()
        {
            var caps = EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable;
            Configure("collision_transfer", "COLLISION TRANSFER",
                "Moves an object's collision to another object. The source becomes intangible; the target becomes solid.",
                ElectricBlue, newDuration: 0f, newCooldown: 0.5f, newMaxActive: 2,
                new[]
                {
                    new TargetSlot("SOURCE", caps, EntityCategory.Anomaly, EntityCategory.Hero),
                    new TargetSlot("TARGET", caps, EntityCategory.Anomaly, EntityCategory.Hero),
                });
        }
    }
}
