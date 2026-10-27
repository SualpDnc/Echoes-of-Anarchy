using System;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>Sounds of Daniel's house (8b): Mia's music box winding down, a door slammed by a draught.</summary>
    public static partial class SynthLibrary
    {
        /// <summary>
        /// A child's music box: steel-comb tines (a fundamental with the comb's inharmonic overtones, a bright pluck, a
        /// long ring) playing an original lullaby in a minor key. It runs down: each note comes a little later and a
        /// little flatter than the last, and the last ones barely come at all.
        /// </summary>
        static float[] MusicBox(Random r)
        {
            // Semitones from A4, in pairs of (note, beats).
            int[] tune = { 3, 1, 10, 1, 8, 1, 7, 2, 5, 1, 7, 1, 3, 2, 2, 1, 3, 1, 5, 1, 7, 1, 8, 2, 7, 1, 5, 1, 3, 3,
                           3, 1, 10, 1, 12, 1, 10, 2, 8, 1, 7, 1, 5, 2, 7, 1, 3, 1, 2, 1, 3, 4 };
            var buf = new float[Samples(17f)];
            float t = 0.15f, beat = 0.34f;
            for (int i = 0; i < tune.Length; i += 2)
            {
                float k = i / (float)tune.Length;
                float slow = 1f + k * k * 1.8f;                          // the spring running down
                float f = 440f * (float)Math.Pow(2.0, tune[i] / 12.0) * (1f - k * k * 0.03f);
                Tine(buf, r, Samples(t), f, 0.8f - k * 0.25f);
                if (tune[i + 1] >= 2 && k < 0.8f) Tine(buf, r, Samples(t + 0.02f), f / 2f, 0.3f); // a lower tine on the long notes
                t += tune[i + 1] * beat * slow;
                if (t > 16f) break;
            }
            Normalize(buf, 0.7f);
            Fade(buf, 0.002f, 0.6f);
            return buf;
        }

        static void Tine(float[] buf, Random r, int start, float f, float amp)
        {
            float[] ratio = { 1f, 5.4f, 11.2f };
            float[] gain = { 1f, 0.18f, 0.05f };
            float[] decay = { 1.6f, 0.35f, 0.08f };
            float ph = Range(r, 0f, 1f);
            for (int i = 0; start + i < buf.Length && i < Samples(2.5f); i++)
            {
                float tt = (float)i / Rate;
                float v = 0f;
                for (int m = 0; m < 3; m++) v += Sin(TwoPi * (f * ratio[m] * tt + ph)) * gain[m] * Exp(-tt / decay[m]);
                v += White(r) * Exp(-tt / 0.002f) * 0.15f;               // the pin plucking the tine
                buf[start + i] += v * amp;
            }
        }

        /// <summary>An interior door slammed by a draught: the wooden thud, the latch, the frame rattling, the room answering.</summary>
        static float[] DoorSlam(Random r)
        {
            var buf = new float[Samples(1.4f)];
            var thudLp = Biquad.LowPass(Range(r, 260f, 340f));
            var bodyBp = Biquad.BandPass(Range(r, 140f, 180f), 2f);
            float latch = Range(r, 0.008f, 0.014f);
            for (int i = 0; i < Samples(0.6f); i++)
            {
                float t = (float)i / Rate;
                float thud = thudLp.Process(White(r)) * Env(t, 0.0008f, 0.06f) * 6f;
                float body = bodyBp.Process(White(r)) * Env(t, 0.001f, 0.12f) * 5f;
                float click = (Sin(TwoPi * 2400f * (t - latch)) * 0.5f + White(r) * 0.6f) * Env(t - latch, 0.0002f, 0.006f);
                float rattle = White(r) * Env(t - 0.05f, 0.002f, 0.05f) * 0.12f * (0.5f + 0.5f * Sin(TwoPi * 31f * t));
                buf[i] = (float)Math.Tanh((thud + body) * 1.2f) + click + rattle;
            }
            ApplyReverb(buf, 0.45f, 0.6f, 0.35f);
            Normalize(buf, 0.9f);
            Fade(buf, 0f, 0.3f);
            return buf;
        }
    }
}
