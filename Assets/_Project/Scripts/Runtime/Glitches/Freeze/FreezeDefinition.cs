using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    public enum FreezeRelease
    {
        /// <summary>Unfrozen bodies start from rest. Safest; default.</summary>
        AtRest,
        /// <summary>Unfrozen bodies continue with the velocity they had when frozen (clamped).</summary>
        RestoreMomentum,
    }

    /// <summary>FREEZE: stops one entity's local time while the rest of the world keeps running.</summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Glitches/Freeze", fileName = "GL_Freeze")]
    public sealed class FreezeDefinition : GlitchDefinition
    {
        public static readonly Color PaleCyan = new Color(0.6f, 0.95f, 1f);

        [Header("Freeze")]
        [SerializeField] private FreezeRelease release = FreezeRelease.AtRest;
        [Tooltip("Upper bound on restored speed (RestoreMomentum only).")]
        [SerializeField, Min(0f)] private float maxReleaseSpeed = 12f;

        public FreezeRelease Release => release;
        public float MaxReleaseSpeed => maxReleaseSpeed;

        public void ConfigureRelease(FreezeRelease newRelease, float newMaxReleaseSpeed)
        {
            release = newRelease;
            maxReleaseSpeed = Mathf.Max(0f, newMaxReleaseSpeed);
        }

        public override string ValidateDefinition()
        {
            var baseError = base.ValidateDefinition();
            if (baseError != null) return baseError;
            return SlotCount == 1 ? null : "needs exactly 1 slot (TARGET), has " + SlotCount;
        }

        public override IGlitchEffect CreateEffect(GameServices services) => new FreezeEffect(services.Registry, this);

        public void ApplyDefaults()
        {
            Configure("freeze", "FREEZE",
                "Stops one object's time. Everything else keeps moving.",
                PaleCyan, newDuration: 8f, newCooldown: 0.5f, newMaxActive: 3,
                new[] { new TargetSlot("TARGET", EntityCapabilities.Inspectable | EntityCapabilities.Freezable, EntityCategory.Anomaly, EntityCategory.Hero) });
            ConfigureRelease(FreezeRelease.AtRest, 12f);
        }
    }
}
