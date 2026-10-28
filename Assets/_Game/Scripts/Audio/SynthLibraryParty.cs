using System;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>
    /// The Purge party in the school yard (8d): the track off the stage's PA, the crowd talking over it, the crowd turned
    /// into a mob, and the machine gun on the pickup.
    /// </summary>
    public static partial class SynthLibrary
    {
        /// <summary>Folds the tail past `loopLen` back onto the head, so a loop that rings on (reverb, a long note) wraps seamlessly.</summary>
        static float[] Wrap(float[] buf, int loopLen)
        {
            var o = new float[loopLen];
            for (int i = 0; i < buf.Length; i++) o[i % loopLen] += buf[i];
            return o;
        }

        /// <summary>
        /// Four bars of dark club music at 124 BPM through a cheap, overdriven PA: a kick on every beat, open hats on the
        /// off-beats, a clap on two and four, an off-beat bass in A minor, a filtered minor stab that opens over the bars,
        /// a slap back off the school's wall. Loops seamlessly.
        /// </summary>
        static float[] PartyMusic(Random r)
        {
            const float bpm = 124f;
            float beat = 60f / bpm;
            int loop = Samples(beat * 16f);
            var buf = new float[loop + Samples(1.5f)];
            float[] roots = { 55f, 55f, 43.65f, 49f };   // A, A, F, G (per bar)

            for (int b = 0; b < 16; b++)
            {
                int at = Samples(b * beat);
                int bar = b / 4;
                // Kick: a falling thump with a click (this is music: a kick drum is meant to sound like one).
                float ph = 0f;
                for (int i = 0; i < Samples(0.32f); i++)
                {
                    float t = (float)i / Rate;
                    float f = 45f + 85f * Exp(-t / 0.035f);
                    ph += f / Rate;
                    buf[at + i] += Sin(TwoPi * ph) * Env(t, 0.001f, 0.16f) * 0.9f + White(r) * Env(t, 0.0002f, 0.002f) * 0.3f;
                }
                // Open hat on the off-beat.
                var hp = Biquad.HighPass(7000f);
                int off = at + Samples(beat * 0.5f);
                for (int i = 0; i < Samples(0.12f); i++)
                {
                    float t = (float)i / Rate;
                    buf[off + i] += hp.Process(White(r)) * Env(t, 0.001f, 0.045f) * 0.22f;
                }
                // Clap on two and four: three quick taps of band-passed noise.
                if (b % 2 == 1)
                {
                    var bp = Biquad.BandPass(1300f, 1.2f);
                    for (int i = 0; i < Samples(0.25f); i++)
                    {
                        float t = (float)i / Rate;
                        float e = Env(t, 0.0005f, 0.006f) + Env(t - 0.011f, 0.0005f, 0.006f) + Env(t - 0.022f, 0.0005f, 0.07f);
                        buf[at + i] += bp.Process(White(r)) * e * 0.55f;
                    }
                }
                // Off-beat bass: a saw through a low-pass, a fifth up on the last off-beat of the bar.
                float bf = roots[bar] * (b % 4 == 3 ? 1.5f : 1f);
                var bl = Biquad.LowPass(320f, 1.4f);
                float bph = 0f;
                for (int i = 0; i < Samples(beat * 0.45f); i++)
                {
                    float t = (float)i / Rate;
                    bph += bf * 2f / Rate;
                    float saw = (bph - (float)Math.Floor(bph)) * 2f - 1f;
                    buf[off + i] += bl.Process(saw) * Env(t, 0.004f, 0.12f) * 0.45f;
                }
            }

            // The stab: a minor triad of detuned saws on the last eighth of each bar, the filter opening bar by bar.
            for (int bar = 0; bar < 4; bar++)
            {
                int at = Samples((bar * 4 + 3.5f) * beat);
                float root = roots[bar] * 4f;
                float[] chord = { root, root * 1.189f, root * 1.498f };
                var lp = Biquad.LowPass(900f + bar * 500f, 2f);
                var phs = new float[6];
                for (int i = 0; i < Samples(beat * 0.9f); i++)
                {
                    float t = (float)i / Rate, s = 0f;
                    for (int k = 0; k < 6; k++)
                    {
                        phs[k] += chord[k % 3] * (k < 3 ? 1f : 1.006f) / Rate;
                        s += (phs[k] - (float)Math.Floor(phs[k])) * 2f - 1f;
                    }
                    buf[at + i] += lp.Process(s / 6f) * Env(t, 0.003f, 0.18f) * 0.55f;
                }
            }

            // The PA: band-limited and driven hard, then the slap off the school's brick wall.
            var lo = Biquad.HighPass(38f);
            var hi = Biquad.LowPass(9500f);
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(hi.Process(lo.Process(buf[i])) * 1.8f);
            var slap = (float[])buf.Clone();
            Echo(buf, slap, 0.06f, 0.22f, 3000f);
            ApplyReverb(buf, 0.55f, 0.5f, 0.18f);
            var o = Wrap(buf, loop);
            Normalize(o, 0.85f);
            return o;
        }

        /// <summary>
        /// One shouting or chattering voice for the crowd: a glottal pulse train through two formants of a random vowel,
        /// in syllables. `shout` raises the pitch and the effort.
        /// </summary>
        static void CrowdVoice(Random r, float[] buf, float gain, bool shout)
        {
            float[,] vowels = { { 730f, 1090f }, { 530f, 1840f }, { 270f, 2290f }, { 570f, 840f }, { 440f, 1020f }, { 660f, 1720f } };
            float f0 = shout ? Range(r, 170f, 330f) : Range(r, 95f, 230f);
            float t0 = Range(r, 0f, 1.5f);
            int i = Samples(t0);
            var f1 = new Biquad();
            var f2 = new Biquad();
            float ph = 0f;
            while (i < buf.Length)
            {
                int v = r.Next(vowels.GetLength(0));
                f1.SetBandPass(vowels[v, 0] * Range(r, 0.9f, 1.1f), 5f);
                f2.SetBandPass(vowels[v, 1] * Range(r, 0.9f, 1.1f), 7f);
                float syl = shout ? Range(r, 0.25f, 0.7f) : Range(r, 0.12f, 0.24f);
                float pitch = f0 * Range(r, 0.9f, 1.15f);
                int n = Samples(syl);
                for (int k = 0; k < n && i + k < buf.Length; k++)
                {
                    float t = (float)k / Rate, u = t / syl;
                    float f = pitch * (1f + (shout ? 0.12f : 0.06f) * (0.5f - u));
                    ph += f / Rate;
                    float pulse = ph - (float)Math.Floor(ph) < 0.12f ? 1f : -0.14f;
                    float src = pulse + White(r) * (shout ? 0.4f : 0.25f);
                    float env = Clamp01(u / 0.15f) * Clamp01((1f - u) / 0.3f);
                    buf[i + k] += (f1.Process(src) + f2.Process(src) * 0.6f) * env * gain;
                }
                i += n;
                // Pauses: short between syllables, long between phrases.
                i += Samples(r.NextDouble() < (shout ? 0.35 : 0.22) ? Range(r, 0.4f, 1.4f) : Range(r, 0.02f, 0.09f));
            }
        }

        /// <summary>A crowd of a few dozen talking and laughing over the music, an occasional whoop. A loop.</summary>
        static float[] CrowdWalla(Random r)
        {
            int loop = Samples(9f);
            var buf = new float[loop + Samples(1.5f)];
            for (int v = 0; v < 22; v++) CrowdVoice(r, buf, Range(r, 0.3f, 1f), false);
            // Whoops: a voice gliding up an octave.
            for (int w = 0; w < 3; w++)
            {
                int at = Samples(Range(r, 0f, 8f));
                float f0 = Range(r, 380f, 520f), ph = 0f;
                var bp = Biquad.BandPass(f0 * 2.2f, 2f);
                for (int i = 0; i < Samples(0.7f) && at + i < buf.Length; i++)
                {
                    float t = (float)i / Rate;
                    ph += f0 * (1f + 0.9f * Clamp01(t / 0.45f)) * (1f + 0.02f * Sin(TwoPi * 6f * t)) / Rate;
                    float saw = (ph - (float)Math.Floor(ph)) * 2f - 1f;
                    buf[at + i] += bp.Process(saw) * Clamp01(t / 0.05f) * Clamp01((0.7f - t) / 0.2f) * 1.6f;
                }
            }
            var lp = Biquad.LowPass(4200f);
            for (int i = 0; i < buf.Length; i++) buf[i] = lp.Process(buf[i]);
            ApplyReverb(buf, 0.6f, 0.5f, 0.3f);
            var o = Wrap(buf, loop);
            Normalize(o, 0.7f);
            return o;
        }

        /// <summary>The crowd turned into a mob: shouting all at once, harder and higher. A loop.</summary>
        static float[] CrowdRoar(Random r)
        {
            int loop = Samples(6f);
            var buf = new float[loop + Samples(1.5f)];
            for (int v = 0; v < 26; v++) CrowdVoice(r, buf, Range(r, 0.4f, 1f), true);
            var lp = Biquad.LowPass(5000f);
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(lp.Process(buf[i]) * 0.9f);
            ApplyReverb(buf, 0.6f, 0.45f, 0.35f);
            var o = Wrap(buf, loop);
            Normalize(o, 0.85f);
            return o;
        }

        /// <summary>
        /// One round from the pickup's belt-fed machine gun, heard from behind it: a bigger, longer blast wave than the
        /// pistol's (rifle calibre), heavy low-passed noise for weight (no tuned sine: that reads as a drum), the bolt's
        /// clack, the school wall slapping it back. Short, so a burst stays a rattle and not a wash.
        /// </summary>
        static float[] MachineGun(Random r)
        {
            var buf = new float[Samples(0.75f)];
            var shot = new float[Samples(0.2f)];
            float td = Range(r, 0.0009f, 0.0013f);
            var gasBp = Biquad.BandPass(Range(r, 700f, 1000f), 0.6f);
            var gasHp = Biquad.HighPass(2400f);
            var thumpLp = Biquad.LowPass(Range(r, 110f, 150f));
            var boltHp = Biquad.HighPass(2200f);
            float bolt = Range(r, 0.004f, 0.007f);
            for (int i = 0; i < shot.Length; i++)
            {
                float t = (float)i / Rate, x = t / td;
                float blast = (1f - x) * Exp(-1.2f * x) * 4.5f;
                float gas = gasBp.Process(White(r)) * Env(t, 0.0002f, 0.022f) * 3.6f + gasHp.Process(White(r)) * Env(t, 0.0001f, 0.006f) * 2f;
                float thump = thumpLp.Process(White(r)) * Env(t, 0.0008f, 0.04f) * 14f;
                float v = (float)Math.Tanh((blast + gas + thump) * 2.6f) * 0.95f;
                v += boltHp.Process(White(r)) * Env(t - bolt, 0.0001f, 0.002f) * 0.4f + Sin(TwoPi * 3900f * (t - bolt)) * Env(t - bolt, 0.0002f, 0.006f) * 0.15f;
                shot[i] = v;
            }
            Mix(buf, shot, 0, 1f);
            Echo(buf, shot, Range(r, 0.004f, 0.007f), 0.4f, 6000f);
            Echo(buf, shot, Range(r, 0.02f, 0.035f), 0.3f, 3000f);
            Echo(buf, shot, Range(r, 0.06f, 0.11f), 0.18f, 1800f);
            var tail = (float[])buf.Clone();
            var tailLp = Biquad.LowPass(1400f);
            var rv = new Reverb(0.7f, 0.6f);
            for (int i = 0; i < buf.Length; i++) buf[i] += rv.Process(tailLp.Process(tail[i])) * 0.45f;
            var dc = new OnePoleHP(30f);
            for (int i = 0; i < buf.Length; i++) buf[i] = dc.Process(buf[i]);
            Normalize(buf, 0.98f);
            Fade(buf, 0f, 0.25f);
            return buf;
        }
    }
}
