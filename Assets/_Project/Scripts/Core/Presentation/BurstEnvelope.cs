using System;

namespace BugReport.Core.Presentation
{
    /// <summary>
    /// Attack–hold–release curve for short, intentional effect bursts (GDD 7.3: no continuous
    /// maximum-intensity effects). Evaluate with the time since the burst started; returns 0..1.
    /// </summary>
    public readonly struct BurstEnvelope
    {
        public readonly float Attack;
        public readonly float Hold;
        public readonly float Release;

        public BurstEnvelope(float attack, float hold, float release)
        {
            if (attack < 0f || hold < 0f || release < 0f) throw new ArgumentOutOfRangeException(nameof(attack), "Durations must be >= 0.");
            Attack = attack;
            Hold = hold;
            Release = release;
        }

        public float Duration => Attack + Hold + Release;

        public float Evaluate(float elapsed)
        {
            if (elapsed < 0f || elapsed >= Duration) return 0f;
            if (elapsed < Attack) return Attack > 0f ? Smooth(elapsed / Attack) : 1f;
            elapsed -= Attack;
            if (elapsed < Hold) return 1f;
            elapsed -= Hold;
            return Release > 0f ? 1f - Smooth(elapsed / Release) : 0f;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }

    /// <summary>Several overlapping bursts; the strongest one wins (no stacking past 1).</summary>
    public sealed class BurstMixer
    {
        private readonly float[] starts;
        private readonly float[] strengths;
        private readonly BurstEnvelope envelope;
        private int next;

        public BurstMixer(BurstEnvelope envelope, int capacity = 8)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.envelope = envelope;
            starts = new float[capacity];
            strengths = new float[capacity];
            for (int i = 0; i < capacity; i++) starts[i] = float.NegativeInfinity;
        }

        public void Trigger(float now, float strength = 1f)
        {
            starts[next] = now;
            strengths[next] = Math.Max(0f, Math.Min(1f, strength));
            next = (next + 1) % starts.Length;
        }

        public float Evaluate(float now)
        {
            float best = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                if (float.IsNegativeInfinity(starts[i])) continue;
                float v = envelope.Evaluate(now - starts[i]) * strengths[i];
                if (v > best) best = v;
            }
            return best;
        }

        public void Clear()
        {
            for (int i = 0; i < starts.Length; i++) starts[i] = float.NegativeInfinity;
        }
    }
}
