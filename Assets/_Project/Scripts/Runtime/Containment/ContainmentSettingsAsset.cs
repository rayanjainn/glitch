using BugReport.Core.Containment;
using UnityEngine;

namespace BugReport.Containment
{
    /// <summary>Designer-tunable containment escalation (ScriptableObject → core <see cref="ContainmentSettings"/>).</summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Containment Settings", fileName = "CS_Default")]
    public sealed class ContainmentSettingsAsset : ScriptableObject
    {
        [Header("Stages (index 0 = stage 1 … index 3 = stage 4)")]
        [SerializeField] private float[] enterThresholds = { 20f, 45f, 70f, 90f };
        [Tooltip("Must be below the matching enter threshold (hysteresis).")]
        [SerializeField] private float[] exitThresholds = { 12f, 35f, 60f, 80f };
        [SerializeField, Min(1f)] private float maxPressure = 100f;

        [Header("Pressure sources")]
        [SerializeField, Min(0f)] private float decayPerSecond = 1.5f;
        [SerializeField, Min(0f)] private float applyImpulsePerWeight = 10f;
        [SerializeField, Min(0f)] private float activePerWeightPerSecond = 1f;
        [SerializeField, Min(0f)] private float perProductPerSecond = 0.5f;
        [SerializeField, Min(0f)] private float sightedPerSecond = 8f;
        [SerializeField, Min(0f)] private float restrictedImpulse = 15f;
        [SerializeField, Min(0f)] private float patchRelief = 15f;

        [Header("Responses per stage (index = stage 0…4)")]
        [Tooltip("Seconds between patches at each stage. 0 = no patching.")]
        [SerializeField] private float[] patchInterval = { 0f, 0f, 0f, 6f, 3f };
        [Tooltip("Cooldown multiplier for glitches applied at each stage (>= 1).")]
        [SerializeField] private float[] cooldownScale = { 1f, 1f, 1.5f, 1.5f, 2f };

        public ContainmentSettings ToSettings() => new ContainmentSettings
        {
            EnterThresholds = (float[])enterThresholds.Clone(),
            ExitThresholds = (float[])exitThresholds.Clone(),
            MaxPressure = maxPressure,
            DecayPerSecond = decayPerSecond,
            ApplyImpulsePerWeight = applyImpulsePerWeight,
            ActivePerWeightPerSecond = activePerWeightPerSecond,
            PerProductPerSecond = perProductPerSecond,
            SightedPerSecond = sightedPerSecond,
            RestrictedImpulse = restrictedImpulse,
            PatchRelief = patchRelief,
        };

        public float PatchInterval(int stage) => At(patchInterval, stage, 0f);
        public float CooldownScale(int stage) => Mathf.Max(1f, At(cooldownScale, stage, 1f));

        /// <summary>Returns null if usable, otherwise what is wrong.</summary>
        public string Validate()
        {
            var problem = ToSettings().Validate();
            if (problem != null) return problem;
            if (patchInterval == null || patchInterval.Length != ContainmentSettings.MaxStage + 1) return "patchInterval needs 5 values";
            if (cooldownScale == null || cooldownScale.Length != ContainmentSettings.MaxStage + 1) return "cooldownScale needs 5 values";
            return null;
        }

        /// <summary>Authoring helper for tests and tooling.</summary>
        public void ConfigureResponses(float[] newPatchInterval, float[] newCooldownScale)
        {
            patchInterval = newPatchInterval;
            cooldownScale = newCooldownScale;
        }

        private static float At(float[] values, int index, float fallback)
            => values != null && index >= 0 && index < values.Length ? values[index] : fallback;
    }
}
