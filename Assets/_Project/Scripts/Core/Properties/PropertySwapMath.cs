using System;

namespace BugReport.Core.Properties
{
    /// <summary>The properties Property Swap can exchange (GDD 4.4: an explicit, small list).</summary>
    public enum SwapProperty
    {
        Mass,
        Speed,
    }

    /// <summary>Safe range for one property, so a swap can never produce a zero-mass body or a runaway machine.</summary>
    public readonly struct PropertyLimits
    {
        public readonly float Min;
        public readonly float Max;

        public PropertyLimits(float min, float max)
        {
            if (!(min > 0f) || max < min) throw new ArgumentOutOfRangeException(nameof(min));
            Min = min;
            Max = max;
        }

        public float Clamp(float value) => float.IsNaN(value) ? Min : Math.Max(Min, Math.Min(Max, value));

        public static PropertyLimits For(SwapProperty property)
        {
            switch (property)
            {
                case SwapProperty.Mass: return new PropertyLimits(0.5f, 60f);
                case SwapProperty.Speed: return new PropertyLimits(0.25f, 4f);
                default: throw new ArgumentOutOfRangeException(nameof(property));
            }
        }
    }

    public static class PropertySwapMath
    {
        /// <summary>
        /// Exchanges two values within the property's limits. Returns false (and leaves the outputs as
        /// the inputs) when swapping would change nothing, so the player gets "NOTHING TO SWAP" instead
        /// of a wasted use.
        /// </summary>
        public static bool TrySwap(SwapProperty property, float a, float b, out float newA, out float newB)
        {
            var limits = PropertyLimits.For(property);
            newA = a;
            newB = b;
            if (Math.Abs(a - b) < 1e-3f) return false;
            newA = limits.Clamp(b);
            newB = limits.Clamp(a);
            return Math.Abs(newA - a) > 1e-3f || Math.Abs(newB - b) > 1e-3f;
        }
    }
}
