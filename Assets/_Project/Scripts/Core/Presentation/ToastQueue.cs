using System;
using System.Collections.Generic;

namespace BugReport.Core.Presentation
{
    public enum ToastTone
    {
        Neutral,
        Positive,
        Negative,
    }

    /// <summary>One short HUD message. <see cref="Color"/> is packed 0xRRGGBBAA (engine-free).</summary>
    public readonly struct Toast
    {
        public readonly string Message;
        public readonly ToastTone Tone;
        public readonly uint Color;
        public readonly float Time;
        public readonly int Count;

        public Toast(string message, ToastTone tone, uint color, float time, int count)
        {
            Message = message;
            Tone = tone;
            Color = color;
            Time = time;
            Count = count;
        }
    }

    /// <summary>
    /// Short-lived feedback messages ("OCCUPIED", "RESTORED: WALL"). Keeps the newest few,
    /// collapses an immediate repeat into one entry with a count, and fades each out at the end
    /// of its lifetime. Pure bookkeeping: the caller supplies the clock.
    /// </summary>
    public sealed class ToastQueue
    {
        private readonly List<Toast> entries = new List<Toast>();
        private readonly int capacity;

        public ToastQueue(int capacity = 3, float lifetime = 2.6f, float fade = 0.4f)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (!(lifetime > 0f) || fade < 0f || fade > lifetime) throw new ArgumentOutOfRangeException(nameof(lifetime));
            this.capacity = capacity;
            Lifetime = lifetime;
            Fade = fade;
        }

        public float Lifetime { get; }
        public float Fade { get; }

        public void Push(string message, ToastTone tone, uint color, float now)
        {
            if (string.IsNullOrEmpty(message)) return;
            int last = entries.Count - 1;
            if (last >= 0 && entries[last].Message == message && entries[last].Tone == tone && now - entries[last].Time < Lifetime)
            {
                entries[last] = new Toast(message, tone, color, now, entries[last].Count + 1);
                return;
            }
            entries.Add(new Toast(message, tone, color, now, 1));
            if (entries.Count > capacity) entries.RemoveAt(0);
        }

        /// <summary>Drops expired entries and copies the live ones into <paramref name="into"/>, newest first.</summary>
        public void Visible(float now, List<Toast> into)
        {
            into.Clear();
            entries.RemoveAll(t => now - t.Time >= Lifetime);
            for (int i = entries.Count - 1; i >= 0; i--) into.Add(entries[i]);
        }

        /// <summary>1 while fresh, falling to 0 over the last <see cref="Fade"/> seconds.</summary>
        public float Opacity(in Toast toast, float now)
        {
            float age = now - toast.Time;
            if (age < 0f) return 1f;
            if (age >= Lifetime) return 0f;
            float fadeStart = Lifetime - Fade;
            return age <= fadeStart || Fade <= 0f ? 1f : 1f - (age - fadeStart) / Fade;
        }

        public void Clear() => entries.Clear();
    }
}
