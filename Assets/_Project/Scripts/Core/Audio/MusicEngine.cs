using System;

namespace BugReport.Core.Audio
{
    /// <summary>
    /// Endless generative music, rendered sample by sample (no audio files, no allocations while
    /// rendering). Layers fade in with <see cref="Intensity"/> (pad always, then arpeggio, bass, kick
    /// and hats as the level progresses); <see cref="Tension"/> (containment) adds a dissonant drone,
    /// a rising noise bed and a bit-crushed "glitch" colour to the whole mix.
    ///
    /// Engine-free and deterministic for a given seed, so it is unit-tested outside Unity. The Unity
    /// adapter calls <see cref="Render"/> from OnAudioFilterRead; parameters may be set from the
    /// main thread (single float writes).
    /// </summary>
    public sealed class MusicEngine
    {
        private const int PlucksMax = 6;
        private const double TwoPi = Math.PI * 2.0;

        private readonly MusicTheme theme;
        private readonly int sampleRate;
        private readonly double samplesPerBeat;
        private uint rng;
        private long samplePos;
        private long lastStep = -1;

        // smoothed controls
        private float smIntensity, smTension;
        private readonly float smoothing;

        // pad
        private readonly double[] padFreq = new double[3];
        private readonly double[] padTarget = new double[3];
        private readonly double[] padPhaseA = new double[3];
        private readonly double[] padPhaseB = new double[3];
        private double tremPhase;

        // plucks
        private readonly double[] pluckPhase = new double[PlucksMax];
        private readonly double[] pluckFreq = new double[PlucksMax];
        private readonly float[] pluckEnv = new float[PlucksMax];
        private int nextPluck;
        private readonly float pluckDecay;

        // bass, kick, hat
        private double bassPhase, bassFreq;
        private float bassEnv;
        private double kickPhase;
        private float kickEnv;
        private float hatEnv, hatPrev;
        private readonly float bassDecay, kickDecay, hatDecay;

        // tension
        private double dronePhaseA, dronePhaseB;
        private float noiseLp;

        // ping-pong delay
        private readonly float[] delayL, delayR;
        private int delayPos;
        private readonly int delayLength;

        // bit crush
        private float heldL, heldR;
        private int holdCounter;

        public float Intensity { get; set; }
        public float Tension { get; set; }
        public float Volume { get; set; } = 0.5f;
        public MusicTheme Theme => theme;
        public int SampleRate => sampleRate;

        public MusicEngine(MusicTheme theme, int sampleRate, uint seed = 12345)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            if (sampleRate < 8000) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            this.theme = theme;
            this.sampleRate = sampleRate;
            rng = seed == 0 ? 1u : seed;
            samplesPerBeat = sampleRate * 60.0 / Math.Max(30f, theme.Bpm);
            smoothing = 1f - (float)Math.Exp(-1.0 / (sampleRate * 1.5));
            pluckDecay = (float)Math.Exp(-1.0 / (sampleRate * 0.32));
            bassDecay = (float)Math.Exp(-1.0 / (sampleRate * 0.45));
            kickDecay = (float)Math.Exp(-1.0 / (sampleRate * 0.12));
            hatDecay = (float)Math.Exp(-1.0 / (sampleRate * 0.05));
            delayLength = Math.Max(16, (int)Math.Min(sampleRate * 1.5, samplesPerBeat * 0.75));
            delayL = new float[delayLength];
            delayR = new float[delayLength];
            for (int i = 0; i < 3; i++)
            {
                padFreq[i] = padTarget[i] = Frequency(DegreeToMidi(theme.Progression[0] + i * 2) + 12);
            }
        }

        /// <summary>Fills <paramref name="buffer"/> with interleaved samples in [-1, 1].</summary>
        public void Render(float[] buffer, int frames, int channels)
        {
            if (buffer == null || channels < 1) return;
            int count = Math.Min(frames, buffer.Length / channels);
            float targetI = Clamp01(Intensity), targetT = Clamp01(Tension);
            float volume = Math.Max(0f, Math.Min(1.5f, Volume));

            for (int n = 0; n < count; n++)
            {
                smIntensity += (targetI - smIntensity) * smoothing;
                smTension += (targetT - smTension) * smoothing;

                long step = (long)Math.Floor(samplePos / samplesPerBeat * 2.0);
                if (step != lastStep)
                {
                    lastStep = step;
                    OnStep(step);
                }

                Mix(out float left, out float right);

                // glitch colour: sample-hold bit crush when tension is high
                if (smTension > 0.55f)
                {
                    int hold = 1 + (int)((smTension - 0.55f) * 14f);
                    if (holdCounter <= 0)
                    {
                        heldL = left;
                        heldR = right;
                        holdCounter = hold;
                    }
                    holdCounter--;
                    float mixAmount = Clamp01((smTension - 0.55f) * 2.2f);
                    left += (heldL - left) * mixAmount;
                    right += (heldR - right) * mixAmount;
                }

                left = SoftClip(left * 1.5f) * volume;
                right = SoftClip(right * 1.5f) * volume;

                int o = n * channels;
                if (channels == 1)
                {
                    buffer[o] = (left + right) * 0.5f;
                }
                else
                {
                    buffer[o] = left;
                    buffer[o + 1] = right;
                    for (int c = 2; c < channels; c++) buffer[o + c] = 0f;
                }
                samplePos++;
            }
        }

        // ---------------------------------------------------------------- sequencing

        private void OnStep(long step)
        {
            int stepInBar = (int)(step % 8);
            int bar = (int)(step / 8);
            int chord = theme.Progression[bar % theme.Progression.Length];

            if (stepInBar == 0)
            {
                for (int i = 0; i < 3; i++) padTarget[i] = Frequency(DegreeToMidi(chord + i * 2) + 12);
            }

            float intensity = smIntensity;
            if (intensity > 0.08f)
            {
                float chance = 0.3f + 0.65f * intensity;
                if ((step % 2 == 0 && NextFloat() < chance + 0.2f) || NextFloat() < chance * 0.6f)
                {
                    int degree = chord + theme.ArpPattern[(int)(step % theme.ArpPattern.Length)];
                    TriggerPluck(Frequency(DegreeToMidi(degree) + 12));
                }
            }

            if (intensity > 0.25f && (stepInBar == 0 || stepInBar == 3 || (stepInBar == 6 && intensity > 0.6f)))
            {
                bassFreq = Frequency(DegreeToMidi(chord) - 12);
                bassEnv = 1f;
            }
            if (intensity > 0.55f && stepInBar % 4 == 0)
            {
                kickEnv = 1f;
                kickPhase = 0;
            }
            if (intensity > 0.45f && step % 2 == 1) hatEnv = 1f;
        }

        private void TriggerPluck(double freq)
        {
            int i = nextPluck;
            nextPluck = (nextPluck + 1) % PlucksMax;
            pluckFreq[i] = freq;
            pluckPhase[i] = 0;
            pluckEnv[i] = 1f;
        }

        // ---------------------------------------------------------------- synthesis

        private void Mix(out float left, out float right)
        {
            double inv = 1.0 / sampleRate;
            float l = 0f, r = 0f;

            // pad: three glided voices, each two slightly detuned oscillators with a soft upper harmonic
            tremPhase += TwoPi * 0.11 * inv;
            if (tremPhase > TwoPi) tremPhase -= TwoPi;
            float trem = 0.85f + 0.15f * (float)Math.Sin(tremPhase);
            float padGain = 0.075f * theme.PadLevel * trem;
            for (int i = 0; i < 3; i++)
            {
                padFreq[i] += (padTarget[i] - padFreq[i]) * 0.0007;
                padPhaseA[i] += TwoPi * padFreq[i] * 0.998 * inv;
                padPhaseB[i] += TwoPi * padFreq[i] * 1.002 * inv;
                if (padPhaseA[i] > TwoPi) padPhaseA[i] -= TwoPi;
                if (padPhaseB[i] > TwoPi) padPhaseB[i] -= TwoPi;
                float v = (float)(Math.Sin(padPhaseA[i]) + Math.Sin(padPhaseB[i]) + theme.Brightness * 0.5 * Math.Sin(padPhaseA[i] * 2.0));
                float pan = (i - 1) * 0.35f;
                l += v * padGain * (1f - pan * 0.5f);
                r += v * padGain * (1f + pan * 0.5f);
            }

            // plucks (arpeggio), fed to the delay
            float pluckSum = 0f;
            float arpGain = 0.1f + 0.14f * smIntensity;
            for (int i = 0; i < PlucksMax; i++)
            {
                if (pluckEnv[i] < 0.0005f) continue;
                pluckPhase[i] += TwoPi * pluckFreq[i] * inv;
                if (pluckPhase[i] > TwoPi) pluckPhase[i] -= TwoPi;
                float tone = (float)(Math.Sin(pluckPhase[i]) + 0.35 * Math.Sin(pluckPhase[i] * 3.0) * theme.Brightness);
                pluckSum += tone * pluckEnv[i] * arpGain;
                pluckEnv[i] *= pluckDecay;
            }
            float wetL = delayL[delayPos];
            float wetR = delayR[delayPos];
            delayL[delayPos] = pluckSum + wetR * 0.42f;
            delayR[delayPos] = wetL * 0.42f;
            delayPos = (delayPos + 1) % delayLength;
            l += pluckSum * 0.8f + wetL * 0.55f;
            r += pluckSum * 0.8f + wetR * 0.55f;

            // bass
            if (bassEnv > 0.0005f)
            {
                bassPhase += TwoPi * bassFreq * inv;
                if (bassPhase > TwoPi) bassPhase -= TwoPi;
                float b = (float)(Math.Sin(bassPhase) + 0.25 * Math.Sin(bassPhase * 2.0)) * bassEnv * 0.26f;
                l += b;
                r += b;
                bassEnv *= bassDecay;
            }

            // kick
            if (kickEnv > 0.0005f)
            {
                kickPhase += TwoPi * (42.0 + 90.0 * kickEnv * kickEnv) * inv;
                if (kickPhase > TwoPi) kickPhase -= TwoPi;
                float k = (float)Math.Sin(kickPhase) * kickEnv * 0.32f;
                l += k;
                r += k;
                kickEnv *= kickDecay;
            }

            // hat: differentiated noise
            float noise = NextFloat() * 2f - 1f;
            if (hatEnv > 0.0005f)
            {
                float h = (noise - hatPrev) * hatEnv * 0.05f;
                l += h * 0.9f;
                r += h * 1.1f;
                hatEnv *= hatDecay;
            }
            hatPrev = noise;

            // tension: dissonant drone (a tritone apart) and a rising noise bed
            if (smTension > 0.01f)
            {
                double root = Frequency(theme.RootMidi - 24);
                dronePhaseA += TwoPi * root * inv;
                dronePhaseB += TwoPi * root * 1.4142 * inv;
                if (dronePhaseA > TwoPi) dronePhaseA -= TwoPi;
                if (dronePhaseB > TwoPi) dronePhaseB -= TwoPi;
                noiseLp += (noise - noiseLp) * (0.02f + 0.2f * smTension);
                float t = (float)(Math.Sin(dronePhaseA) + 0.8 * Math.Sin(dronePhaseB)) * 0.07f + noiseLp * 0.12f;
                t *= smTension;
                l += t;
                r += t;
            }

            left = l;
            right = r;
        }

        // ---------------------------------------------------------------- helpers

        public double DegreeToMidi(int degree)
        {
            int n = theme.Scale.Length;
            int octave = degree >= 0 ? degree / n : -((-degree + n - 1) / n);
            int index = degree - octave * n;
            return theme.RootMidi + 12 * octave + theme.Scale[index];
        }

        public static double Frequency(double midi) => 440.0 * Math.Pow(2.0, (midi - 69.0) / 12.0);

        private float NextFloat()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return (rng & 0xFFFFFF) / 16777216f;
        }

        private static float Clamp01(float v) => float.IsNaN(v) ? 0f : Math.Max(0f, Math.Min(1f, v));
        private static float SoftClip(float x) => x / (1f + Math.Abs(x));
    }
}
