using UnityEngine;

namespace BugReport.Audio
{
    /// <summary>
    /// Synthesises short placeholder sound effects at runtime (sine/square sweeps, noise
    /// bursts). Original by construction, so there is nothing to license; replaced by designed
    /// audio in the art pass. Clips are created once and reused.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = 44100;

        public enum Wave
        {
            Sine,
            Square,
            Noise,
        }

        /// <summary>A tone sweeping from <paramref name="startHz"/> to <paramref name="endHz"/> with a short attack and exponential decay.</summary>
        public static AudioClip Sweep(string name, float startHz, float endHz, float seconds, Wave wave, float volume = 0.5f, float noiseMix = 0f)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startHz, endHz, t);
                phase += freq / SampleRate;
                float s;
                switch (wave)
                {
                    case Wave.Square: s = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f; break;
                    case Wave.Noise: s = (float)(rng.NextDouble() * 2.0 - 1.0); break;
                    default: s = Mathf.Sin(phase * Mathf.PI * 2f); break;
                }
                if (noiseMix > 0f) s = Mathf.Lerp(s, (float)(rng.NextDouble() * 2.0 - 1.0), noiseMix);
                float attack = Mathf.Clamp01(i / (0.005f * SampleRate));
                float decay = Mathf.Exp(-4f * t);
                data[i] = s * attack * decay * volume;
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Several sweeps one after another (e.g. a rising arpeggio).</summary>
        public static AudioClip Sequence(string name, float[] notesHz, float noteSeconds, Wave wave, float volume = 0.4f)
        {
            int perNote = Mathf.Max(1, Mathf.RoundToInt(noteSeconds * SampleRate));
            var data = new float[perNote * notesHz.Length];
            for (int n = 0; n < notesHz.Length; n++)
            {
                var note = Sweep(name + n, notesHz[n], notesHz[n], noteSeconds, wave, volume);
                var buffer = new float[perNote];
                note.GetData(buffer, 0);
                System.Array.Copy(buffer, 0, data, n * perNote, perNote);
                Object.Destroy(note);
            }
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
