using System;

namespace BugReport.Core.Containment
{
    /// <summary>Tuning for <see cref="ContainmentModel"/>. Produced from a ScriptableObject in the game.</summary>
    public sealed class ContainmentSettings
    {
        public const int MaxStage = 4;

        /// <summary>Pressure needed to ENTER stage i+1 (length 4, ascending).</summary>
        public float[] EnterThresholds = { 20f, 45f, 70f, 90f };
        /// <summary>Pressure below which stage i+1 is LEFT (length 4, each below its enter threshold: hysteresis).</summary>
        public float[] ExitThresholds = { 12f, 35f, 60f, 80f };
        public float MaxPressure = 100f;
        /// <summary>Pressure lost per second, always. Normal play with no glitches returns to 0.</summary>
        public float DecayPerSecond = 1.5f;
        /// <summary>Instant pressure per point of glitch weight when a glitch is applied.</summary>
        public float ApplyImpulsePerWeight = 10f;
        /// <summary>Pressure per second per point of weight of glitches currently active (altered states persist).</summary>
        public float ActivePerWeightPerSecond = 1f;
        /// <summary>Pressure per second per existing duplicate.</summary>
        public float PerProductPerSecond = 0.5f;
        /// <summary>Pressure per second while a containment unit can see the anomaly.</summary>
        public float SightedPerSecond = 8f;
        /// <summary>Extra instant pressure when a restricted object is modified.</summary>
        public float RestrictedImpulse = 15f;
        /// <summary>Pressure removed when a patch reverts a glitch (the system calms after acting).</summary>
        public float PatchRelief = 15f;

        public static ContainmentSettings Default => new ContainmentSettings();

        /// <summary>Returns null if valid, otherwise what is wrong.</summary>
        public string Validate()
        {
            if (EnterThresholds == null || EnterThresholds.Length != MaxStage) return "EnterThresholds must have " + MaxStage + " values";
            if (ExitThresholds == null || ExitThresholds.Length != MaxStage) return "ExitThresholds must have " + MaxStage + " values";
            for (int i = 0; i < MaxStage; i++)
            {
                if (ExitThresholds[i] >= EnterThresholds[i]) return "ExitThresholds[" + i + "] must be below EnterThresholds[" + i + "]";
                if (i > 0 && EnterThresholds[i] <= EnterThresholds[i - 1]) return "EnterThresholds must ascend";
                if (EnterThresholds[i] > MaxPressure) return "EnterThresholds[" + i + "] exceeds MaxPressure";
            }
            if (DecayPerSecond < 0f || MaxPressure <= 0f) return "DecayPerSecond >= 0 and MaxPressure > 0 required";
            return null;
        }
    }

    /// <summary>Per-frame continuous inputs, sampled by the game from explicit state (never guessed).</summary>
    public readonly struct ContainmentInputs
    {
        public readonly float ActiveGlitchWeight;
        public readonly int ProductCount;
        public readonly bool AnomalySighted;

        public ContainmentInputs(float activeGlitchWeight, int productCount, bool anomalySighted)
        {
            ActiveGlitchWeight = activeGlitchWeight;
            ProductCount = productCount;
            AnomalySighted = anomalySighted;
        }
    }

    /// <summary>
    /// Deterministic containment escalation (GDD Part 5). Pressure rises only from explicit
    /// events and state (applied glitches, glitches still active, duplicates, being seen,
    /// restricted targets) and always decays, so ordinary play returns to stage 0. Stages use
    /// hysteresis so they do not flicker around a threshold. Never touches the world itself:
    /// responders subscribe to <see cref="StageChanged"/>.
    /// </summary>
    public sealed class ContainmentModel
    {
        private ContainmentSettings settings;

        public ContainmentModel(ContainmentSettings settings = null)
        {
            Configure(settings ?? ContainmentSettings.Default);
        }

        public float Pressure { get; private set; }
        public int Stage { get; private set; }
        public ContainmentSettings Settings => settings;

        /// <summary>Normalised pressure 0..1 for UI.</summary>
        public float Pressure01 => settings.MaxPressure > 0f ? Pressure / settings.MaxPressure : 0f;

        /// <summary>(previous, current).</summary>
        public event Action<int, int> StageChanged;

        public void Configure(ContainmentSettings newSettings)
        {
            if (newSettings == null) throw new ArgumentNullException(nameof(newSettings));
            var problem = newSettings.Validate();
            if (problem != null) throw new ArgumentException("Invalid containment settings: " + problem, nameof(newSettings));
            settings = newSettings;
            Evaluate();
        }

        public void ReportGlitchApplied(float weight, bool restrictedTarget = false)
        {
            if (weight < 0f) throw new ArgumentOutOfRangeException(nameof(weight));
            Add(weight * settings.ApplyImpulsePerWeight + (restrictedTarget ? settings.RestrictedImpulse : 0f));
        }

        public void ReportPatched() => Add(-settings.PatchRelief);

        public void Tick(float deltaTime, in ContainmentInputs inputs)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (deltaTime == 0f) return;
            float rate = inputs.ActiveGlitchWeight * settings.ActivePerWeightPerSecond
                         + inputs.ProductCount * settings.PerProductPerSecond
                         + (inputs.AnomalySighted ? settings.SightedPerSecond : 0f)
                         - settings.DecayPerSecond;
            Add(rate * deltaTime);
        }

        /// <summary>Back to stage 0 with no pressure (level restart).</summary>
        public void Reset()
        {
            Pressure = 0f;
            Evaluate();
        }

        private void Add(float amount)
        {
            Pressure = Math.Max(0f, Math.Min(settings.MaxPressure, Pressure + amount));
            Evaluate();
        }

        private void Evaluate()
        {
            int previous = Stage;
            int stage = Stage;
            while (stage < ContainmentSettings.MaxStage && Pressure >= settings.EnterThresholds[stage]) stage++;
            while (stage > 0 && Pressure < settings.ExitThresholds[stage - 1]) stage--;
            if (stage == previous) return;
            Stage = stage;
            StageChanged?.Invoke(previous, stage);
        }
    }
}
