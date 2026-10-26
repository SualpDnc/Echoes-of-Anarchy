using System;
using System.Collections.Generic;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>Close-range combat sounds: the player's own pistol, mechanics, impacts, bullet flybys, stingers.</summary>
    public static partial class SynthLibrary
    {
        /// <summary>
        /// A 9 mm fired in a street canyon at arm's length: supersonic crack, muzzle blast, slide clack,
        /// early facade reflections at 20–60 ms, then a short street tail. Saturated so it reads as LOUD.
        /// </summary>
        static float[] GunNear(Random r)
        {
            var buf = new float[Samples(1.8f)];
            var crackHp = new OnePoleHP(1200f);
            var bodyLp = Biquad.LowPass(Range(r, 2200f, 2800f));
            var blastLp = Biquad.LowPass(500f);
            float boomF = Range(r, 75f, 95f);
            var shot = new float[Samples(0.35f)];
            for (int i = 0; i < shot.Length; i++)
            {
                float t = (float)i / Rate;
                float crack = crackHp.Process(White(r)) * Env(t, 0.0001f, 0.0025f) * 2.4f;
                float body = bodyLp.Process(White(r)) * Env(t, 0.0003f, 0.045f) * 1.6f;
                float blast = blastLp.Process(White(r)) * Env(t, 0.001f, 0.09f) * 2.2f;
                float boom = Sin(TwoPi * boomF * t * (1f - t * 0.8f)) * Env(t, 0.0015f, 0.07f) * 1.4f;
                float clack = Sin(TwoPi * 3100f * (t - 0.012f)) * Env(t - 0.012f, 0.0003f, 0.008f) * 0.35f;
                shot[i] = (float)Math.Tanh((crack + body + blast + boom + clack) * 1.3f);
            }
            Mix(buf, shot, 0, 1f);
            // Early reflections off nearby walls and cars.
            for (int k = 0; k < 4; k++)
            {
                var e = (float[])shot.Clone();
                var lp = Biquad.LowPass(3000f - k * 500f);
                for (int i = 0; i < e.Length; i++) e[i] = lp.Process(e[i]);
                Mix(buf, e, Samples(Range(r, 0.02f, 0.06f) + k * 0.03f), Range(r, 0.18f, 0.3f));
            }
            // Distant slap from the far side of the street.
            var slap = (float[])shot.Clone();
            var slapLp = Biquad.LowPass(1200f);
            for (int i = 0; i < slap.Length; i++) slap[i] = slapLp.Process(slap[i]);
            Mix(buf, slap, Samples(Range(r, 0.18f, 0.32f)), 0.18f);
            ApplyReverb(buf, 0.75f, 0.5f, 0.3f);
            Normalize(buf, 0.98f);
            Fade(buf, 0f, 0.4f);
            return buf;
        }

        static float[] DryFire(Random r)
        {
            var buf = new float[Samples(0.08f)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = Sin(TwoPi * 2600f * t) * Env(t, 0.0002f, 0.004f) + White(r) * Env(t, 0.0001f, 0.0012f) * 0.6f;
            }
            Normalize(buf, 0.5f);
            return buf;
        }

        /// <summary>Generic mechanical action: a scrape of `len` seconds ending in a latch click.</summary>
        static float[] Mech(Random r, float len, float clickF, float clickAmp)
        {
            var buf = new float[Samples(len + 0.08f)];
            var bp = Biquad.BandPass(Range(r, 1500f, 2500f), 1.2f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float scrape = t < len ? bp.Process(White(r)) * 0.25f * Sin((float)Math.PI * t / len) : 0f;
                float tc = t - len;
                float click = Sin(TwoPi * clickF * tc) * Env(tc, 0.0002f, 0.006f) * clickAmp + White(r) * Env(tc, 0.0001f, 0.0015f) * clickAmp * 0.8f;
                buf[i] = scrape + click;
            }
            Normalize(buf, 0.6f);
            return buf;
        }

        static float[] SlideRack(Random r)
        {
            var a = Mech(r, 0.06f, 2000f, 0.7f);
            var b = Mech(r, 0.05f, 2900f, 1f);
            var buf = new float[a.Length + b.Length];
            Mix(buf, a, 0, 1f);
            Mix(buf, b, Samples(0.1f), 1f);
            Normalize(buf, 0.65f);
            return buf;
        }

        /// <summary>Brass on pavement: a few bright bounces with ringing modes.</summary>
        static float[] ShellCasing(Random r)
        {
            var buf = new float[Samples(0.7f)];
            float[] modes = { Range(r, 4200f, 5200f), Range(r, 7000f, 8800f), Range(r, 11000f, 12500f) };
            float t0 = 0f, amp = 1f;
            for (int b = 0; b < 4; b++)
            {
                int s0 = Samples(t0);
                for (int i = 0; i < Samples(0.12f) && s0 + i < buf.Length; i++)
                {
                    float t = (float)i / Rate, v = 0f;
                    for (int m = 0; m < modes.Length; m++) v += Sin(TwoPi * modes[m] * t) * Env(t, 0.0001f, 0.03f / (m + 1)) / (m + 1);
                    buf[s0 + i] += v * amp;
                }
                t0 += Range(r, 0.06f, 0.16f) * amp;
                amp *= Range(r, 0.4f, 0.6f);
            }
            Normalize(buf, 0.35f);
            return buf;
        }

        static float[] ImpactHard(Random r)
        {
            var buf = new float[Samples(0.5f)];
            var hp = new OnePoleHP(900f);
            var lp = Biquad.LowPass(1500f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = hp.Process(White(r)) * Env(t, 0.0001f, 0.006f) * 1.5f + lp.Process(White(r)) * Env(t, 0.0005f, 0.03f);
                if (t > 0.01f && t < 0.3f && r.NextDouble() < 0.003) v += White(r) * 0.5f; // debris
                buf[i] = v;
            }
            Normalize(buf, 0.8f);
            return buf;
        }

        static float[] ImpactMetal(Random r)
        {
            var buf = new float[Samples(0.8f)];
            float[] modes = { Range(r, 900f, 1300f), Range(r, 2100f, 2900f), Range(r, 3700f, 4600f), Range(r, 6000f, 7500f) };
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate, v = White(r) * Env(t, 0.0001f, 0.004f);
                for (int m = 0; m < modes.Length; m++) v += Sin(TwoPi * modes[m] * t) * Env(t, 0.0002f, 0.18f / (m + 1)) * 0.5f;
                buf[i] = v;
            }
            Normalize(buf, 0.75f);
            return buf;
        }

        static float[] ImpactFlesh(Random r)
        {
            var buf = new float[Samples(0.3f)];
            var lp = Biquad.LowPass(Range(r, 500f, 800f));
            var bp = Biquad.BandPass(1200f, 1.5f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = lp.Process(White(r)) * Env(t, 0.001f, 0.04f) * 2f + bp.Process(White(r)) * Env(t - 0.01f, 0.003f, 0.05f) * 0.7f
                       + Sin(TwoPi * 70f * t) * Env(t, 0.002f, 0.05f);
            }
            Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>A round passing within a metre: sharp N-wave crack, then a short tearing tail.</summary>
        static float[] Whizz(Random r)
        {
            var buf = new float[Samples(0.35f)];
            var hp = new OnePoleHP(2000f);
            var bp = Biquad.BandPass(Range(r, 2500f, 4000f), 1f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float crack = hp.Process(White(r)) * Env(t, 0.0001f, 0.0015f) * 2f;
                float tear = bp.Process(White(r)) * Env(t - 0.002f, 0.004f, 0.05f) * 0.6f;
                buf[i] = crack + tear;
            }
            Normalize(buf, 0.85f);
            return buf;
        }

        /// <summary>"You've been seen" stinger: rising air swell into a sub hit.</summary>
        static float[] TensionHit(Random r)
        {
            var buf = new float[Samples(2.2f)];
            var lp = Biquad.LowPass(300f);
            float hitT = 0.45f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float swell = t < hitT ? lp.Process(White(r)) * (float)Math.Pow(t / hitT, 3) * 2f : 0f;
                float th = t - hitT;
                float hit = Sin(TwoPi * 42f * th) * Env(th, 0.003f, 0.6f) * 1.2f + Sin(TwoPi * 84f * th) * Env(th, 0.002f, 0.2f) * 0.3f;
                buf[i] = swell + hit;
            }
            Normalize(buf, 0.7f);
            Fade(buf, 0.01f, 0.3f);
            return buf;
        }

        /// <summary>One "lub-dub" per second; loop it and raise pitch for a faster pulse.</summary>
        static float[] Heartbeat(Random r)
        {
            var buf = new float[Samples(1f)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = Sin(TwoPi * 48f * t) * Env(t, 0.008f, 0.07f) + Sin(TwoPi * 58f * (t - 0.26f)) * Env(t - 0.26f, 0.008f, 0.06f) * 0.7f;
            }
            Normalize(buf, 0.8f);
            return buf;
        }

        static float[] BodyFall(Random r)
        {
            var buf = new float[Samples(0.9f)];
            var lp = Biquad.LowPass(600f);
            var cloth = Biquad.BandPass(900f, 0.7f);
            float t2 = Range(r, 0.12f, 0.25f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = lp.Process(White(r)) * (Env(t, 0.002f, 0.06f) + Env(t - t2, 0.002f, 0.05f) * 0.7f) * 2f;
                v += Sin(TwoPi * 55f * t) * Env(t, 0.003f, 0.09f) * 1.2f;
                v += cloth.Process(White(r)) * Env(t, 0.01f, 0.15f) * 0.3f;
                buf[i] = v;
            }
            Normalize(buf, 0.8f);
            return buf;
        }

        static float[] BatSwing(Random r)
        {
            var buf = new float[Samples(0.4f)];
            var bp = Biquad.BandPass(600f, 1f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float env = (float)Math.Pow(Sin((float)Math.PI * Clamp01(t / 0.35f)), 3);
                if ((i & 15) == 0) bp.SetBandPass(400f + 1400f * env, 1f);
                buf[i] = bp.Process(White(r)) * env;
            }
            Normalize(buf, 0.6f);
            return buf;
        }

        static float[] BatHit(Random r)
        {
            var buf = new float[Samples(0.4f)];
            var lp = Biquad.LowPass(900f);
            float woodF = Range(r, 280f, 320f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = lp.Process(White(r)) * Env(t, 0.0005f, 0.03f) * 2f + Sin(TwoPi * woodF * t) * Env(t, 0.0005f, 0.05f) * 0.5f
                       + Sin(TwoPi * 65f * t) * Env(t, 0.002f, 0.06f);
            }
            Normalize(buf, 0.85f);
            return buf;
        }

        /// <summary>Velcro/fabric tear of a trauma dressing.</summary>
        static float[] Bandage(Random r)
        {
            var buf = new float[Samples(1.2f)];
            var bp = Biquad.BandPass(2500f, 0.8f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float rip = t > 0.1f && t < 0.6f ? (r.NextDouble() < 0.35 ? White(r) : 0f) * Sin((float)Math.PI * (t - 0.1f) / 0.5f) : 0f;
                float rustle = White(r) * Env(t - 0.7f, 0.05f, 0.15f) * 0.2f;
                buf[i] = bp.Process(rip + rustle);
            }
            Normalize(buf, 0.5f);
            return buf;
        }
    }
}
