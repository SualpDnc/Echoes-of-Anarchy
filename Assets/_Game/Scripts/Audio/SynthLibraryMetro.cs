using System;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>Sounds of the dead subway station (8c): the rusted gate forced open, the blind Smiler humming in his mask.</summary>
    public static partial class SynthLibrary
    {
        /// <summary>
        /// A rusted steel gate dragged open: stick-slip screeching (inharmonic partials whose pitch wanders as the hinge
        /// catches and lets go), grit, then the gate hitting its stop. Loud on purpose: the hunter hears it.
        /// </summary>
        static float[] GateScreech(Random r)
        {
            float len = Range(r, 2.2f, 2.8f);
            var buf = new float[Samples(len + 1.2f)];
            float[] ratio = { 1f, 2.37f, 3.91f, 5.6f };
            float f0 = Range(r, 520f, 700f);
            var grit = Biquad.BandPass(Range(r, 1800f, 2600f), 1.5f);
            var wander = new OnePoleLP(3f);
            var slipLp = new OnePoleLP(25f);
            float ph0 = 0f, ph1 = 0f, ph2 = 0f, ph3 = 0f;
            for (int i = 0; i < Samples(len); i++)
            {
                float t = (float)i / Rate;
                float bend = 1f + wander.Process(White(r)) * 2.5f + 0.15f * (t / len);
                // Stick-slip: the screech comes in catches, not as one tone.
                float slip = Clamp01(slipLp.Process(Rand(r) < 0.5f ? 1.4f : -0.6f) * 2f);
                float f = f0 * bend;
                ph0 += f * ratio[0] / Rate; ph1 += f * ratio[1] / Rate; ph2 += f * ratio[2] / Rate; ph3 += f * ratio[3] / Rate;
                float tone = Sin(TwoPi * ph0) + 0.6f * Sin(TwoPi * ph1) + 0.35f * Sin(TwoPi * ph2) + 0.2f * Sin(TwoPi * ph3);
                float env = Clamp01(t / 0.08f) * Clamp01((len - t) / 0.2f);
                buf[i] = (float)Math.Tanh(tone * slip * 1.4f) * 0.6f * env + grit.Process(White(r)) * env * 0.5f;
            }
            // The stop: a low clang with a ring.
            int at = Samples(len - 0.05f);
            for (int i = 0; at + i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[at + i] += (Sin(TwoPi * 180f * t) * 0.8f + Sin(TwoPi * 431f * t) * 0.5f + Sin(TwoPi * 977f * t) * 0.3f) * Env(t, 0.001f, 0.35f)
                               + White(r) * Env(t, 0.0005f, 0.01f) * 0.8f;
            }
            ApplyReverb(buf, 0.9f, 0.4f, 0.55f);
            Normalize(buf, 0.92f);
            Fade(buf, 0.005f, 0.4f);
            return buf;
        }

        /// <summary>
        /// The blind Smiler, humming through his mask as he walks: a slow, wandering phrase on "mm" (a buzzy low source
        /// through a closed-mouth resonance), breath between the notes. A seamless loop the player can track him by.
        /// </summary>
        static float[] ArmorHum(Random r)
        {
            int[] tune = { 0, 3, 5, 3, 0, -2, 0, 7, 5, 3 };
            const float note = 0.75f;
            float total = tune.Length * note;
            var buf = new float[Samples(total + 1f)];
            var mouth = Biquad.BandPass(240f, 1.5f);
            var chest = Biquad.LowPass(500f);
            var breath = Biquad.BandPass(1200f, 0.8f);
            float phase = 0f, f = 98f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                int k = Math.Min(tune.Length - 1, (int)(t / note) % tune.Length);
                float target = 98f * (float)Math.Pow(2.0, tune[k] / 12.0);
                f += (target - f) * 0.0015f;
                phase += f * (1f + 0.004f * Sin(TwoPi * 4.5f * t)) / Rate;
                float saw = (phase - (float)Math.Floor(phase)) * 2f - 1f;
                float inNote = (t % note) / note;
                float env = Clamp01(inNote / 0.12f) * Clamp01((1f - inNote) / 0.25f);
                float hum = mouth.Process(saw) * 1.5f + chest.Process(saw) * 0.5f;
                float air = breath.Process(White(r)) * (1f - env) * 0.12f;
                buf[i] = (float)Math.Tanh(hum * env * 1.3f) * 0.8f + air;
            }
            var loop = MakeLoop(buf, 1f);
            Normalize(loop, 0.8f);
            return loop;
        }
    }
}
