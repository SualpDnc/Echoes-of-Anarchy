using System;
using System.Collections.Generic;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>Close-range combat sounds: the player's own pistol, mechanics, impacts, bullet flybys, stingers.</summary>
    public static partial class SynthLibrary
    {
        /// <summary>
        /// A 9 mm fired at arm's length, heard by the chest camera in a street canyon. The owner heard the old recipe
        /// as a drum: it had a tuned, falling low sine (that is how a kick drum is made). A real shot has no pitch:
        /// a muzzle blast that is a single pressure spike (a Friedlander N-wave) plus turbulent gas noise, clipped
        /// flat by the small mic; the slide's metallic clack a few ms later and its return; the ground bounce, the
        /// facades answering at 15–90 ms, a slap off the far side of the street, and a long, dull rolling tail.
        /// </summary>
        static float[] GunNear(Random r)
        {
            var buf = new float[Samples(2.6f)];
            var shot = new float[Samples(0.25f)];
            float td = Range(r, 0.00045f, 0.0007f);                 // positive phase of the blast wave
            var gasBp = Biquad.BandPass(Range(r, 900f, 1400f), 0.55f);
            var gasHp = Biquad.HighPass(Range(r, 2500f, 3200f));
            var thumpLp = Biquad.LowPass(Range(r, 150f, 210f));     // chest-felt weight, as noise: no pitch
            var clackHp = Biquad.HighPass(1800f);
            float[] modes = { Range(r, 2900f, 3300f), Range(r, 4700f, 5200f), Range(r, 6900f, 7600f), Range(r, 9800f, 10600f) };
            float back = Range(r, 0.005f, 0.008f), home = back + Range(r, 0.026f, 0.034f);
            for (int i = 0; i < shot.Length; i++)
            {
                float t = (float)i / Rate;
                float x = t / td;
                float blast = (1f - x) * Exp(-1.4f * x) * 4f;
                float gas = gasBp.Process(White(r)) * Env(t, 0.0002f, 0.016f) * 3.2f + gasHp.Process(White(r)) * Env(t, 0.0001f, 0.005f) * 2.2f;
                float thump = thumpLp.Process(White(r)) * Env(t, 0.0006f, 0.028f) * 9f;
                // The mic and preamp of a body camera clip hard on a gunshot: the crack comes out flat and crunchy.
                float v = (float)Math.Tanh((blast + gas + thump) * 2.4f) * 0.92f;
                float clack = clackHp.Process(White(r)) * (Env(t - back, 0.0001f, 0.0018f) * 0.45f + Env(t - home, 0.0001f, 0.0014f) * 0.3f);
                for (int m = 0; m < modes.Length; m++)
                {
                    float decay = 0.012f / (1f + m * 0.6f);
                    clack += Sin(TwoPi * modes[m] * (t - back)) * Env(t - back, 0.0002f, decay) * 0.2f
                           + Sin(TwoPi * modes[m] * 1.06f * (t - home)) * Env(t - home, 0.0002f, decay * 0.8f) * 0.13f;
                }
                shot[i] = v + clack;
            }
            Mix(buf, shot, 0, 1f);

            // The street answers: the ground bounce, then the facades (each later one duller), then the far side.
            Echo(buf, shot, Range(r, 0.005f, 0.008f), 0.45f, 7000f);
            int walls = r.Next(4, 7);
            for (int k = 0; k < walls; k++)
                Echo(buf, shot, Range(r, 0.015f, 0.09f), Range(r, 0.14f, 0.28f), Range(r, 2200f, 4200f) - k * 250f);
            Echo(buf, shot, Range(r, 0.16f, 0.3f), Range(r, 0.12f, 0.18f), 1500f);
            Echo(buf, shot, Range(r, 0.38f, 0.6f), Range(r, 0.05f, 0.09f), 900f);

            // The rolling tail: the shot fed through a long, damped reverb, low-passed so it rumbles instead of hissing.
            var tail = new float[buf.Length];
            Array.Copy(buf, tail, buf.Length);
            var tailLp = Biquad.LowPass(1800f);
            var rv = new Reverb(0.92f, 0.6f);
            for (int i = 0; i < tail.Length; i++) buf[i] += rv.Process(tailLp.Process(tail[i])) * 0.55f;

            var dc = new OnePoleHP(35f);
            for (int i = 0; i < buf.Length; i++) buf[i] = dc.Process(buf[i]);
            Normalize(buf, 0.98f);
            Fade(buf, 0f, 0.5f);
            return buf;
        }

        /// <summary>A delayed, low-passed copy of `src` mixed into `dst` (a reflection off a surface).</summary>
        static void Echo(float[] dst, float[] src, float delay, float gain, float cutoff)
        {
            var e = (float[])src.Clone();
            var lp = Biquad.LowPass(cutoff);
            for (int i = 0; i < e.Length; i++) e[i] = lp.Process(e[i]);
            Mix(dst, e, Samples(delay), gain);
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

        /// <summary>An empty magazine hitting asphalt: a hollow polymer-and-steel knock, a bounce, a short rattle.</summary>
        static float[] MagDrop(Random r)
        {
            var buf = new float[Samples(0.6f)];
            var body = Biquad.BandPass(Range(r, 650f, 900f), 2f);
            var tick = Biquad.HighPass(2500f);
            float bounce = Range(r, 0.09f, 0.14f);
            float[] modes = { Range(r, 1400f, 1700f), Range(r, 2600f, 3100f), Range(r, 4200f, 4800f) };
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float hit = Env(t, 0.0003f, 0.02f) + Env(t - bounce, 0.0003f, 0.012f) * 0.45f;
                float v = body.Process(White(r)) * hit * 2.5f + tick.Process(White(r)) * hit * 0.6f;
                for (int m = 0; m < modes.Length; m++) v += Sin(TwoPi * modes[m] * t) * Env(t, 0.0003f, 0.05f / (m + 1)) * 0.25f;
                if (t > bounce && t < 0.45f && r.NextDouble() < 0.002) v += White(r) * 0.3f; // rattle settling
                buf[i] = v;
            }
            Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>A fresh magazine pulled from the belt pouch: fabric drag and a polymer scrape.</summary>
        static float[] MagPouch(Random r)
        {
            var buf = new float[Samples(0.4f)];
            var cloth = Biquad.BandPass(1100f, 0.7f);
            var scrape = Biquad.BandPass(3200f, 1.5f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Sin((float)Math.PI * Clamp01(t / 0.32f));
                buf[i] = cloth.Process(White(r)) * env * 0.8f + scrape.Process(White(r)) * Env(t - 0.2f, 0.01f, 0.06f) * 0.6f;
            }
            Normalize(buf, 0.55f);
            Fade(buf, 0.005f, 0.05f);
            return buf;
        }

        /// <summary>The magazine slapped home: a solid low thunk under a bright latch click.</summary>
        static float[] MagSeat(Random r)
        {
            var buf = new float[Samples(0.3f)];
            var lp = Biquad.LowPass(900f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float thunk = Sin(TwoPi * 170f * t) * Env(t, 0.001f, 0.04f) + lp.Process(White(r)) * Env(t, 0.0005f, 0.02f) * 1.5f;
                float click = Sin(TwoPi * 3300f * (t - 0.004f)) * Env(t - 0.004f, 0.0002f, 0.008f) * 0.7f + White(r) * Env(t - 0.004f, 0.0001f, 0.002f) * 0.6f;
                buf[i] = thunk + click;
            }
            Normalize(buf, 0.85f);
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

        /// <summary>Headshot: a dry bone crack inside a heavy wet slap, then a short tail of spattering droplets.</summary>
        static float[] ImpactHead(Random r)
        {
            var buf = new float[Samples(0.45f)];
            var crackBp = Biquad.BandPass(Range(r, 2200f, 3000f), 2f);
            var lp = Biquad.LowPass(Range(r, 700f, 1000f));
            var spl = Biquad.BandPass(Range(r, 1400f, 2000f), 1.2f);
            float grain = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float crack = crackBp.Process(White(r)) * Env(t, 0.0001f, 0.006f) * 3f;
                float slap = lp.Process(White(r)) * Env(t - 0.002f, 0.001f, 0.06f) * 2.2f + Sin(TwoPi * 85f * t) * Env(t, 0.002f, 0.08f) * 1.2f;
                if (r.NextDouble() < 0.0025) grain = 1f; // sparse wet ticks
                grain *= 0.993f;
                float tail = spl.Process(White(r)) * grain * Env(t - 0.03f, 0.01f, 0.18f) * 1.4f;
                buf[i] = crack + slap + tail;
            }
            Normalize(buf, 0.85f);
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

        /// <summary>A mortar tube firing a shell close by: fuse fizz, a hollow thump, then the shell's whistle climbing away.</summary>
        static float[] FireworkLaunch(Random r)
        {
            var buf = new float[Samples(2.2f)];
            var thumpLp = Biquad.LowPass(260f);
            var fizzBp = Biquad.BandPass(5200f, 0.8f);
            float phase = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float fizz = fizzBp.Process(White(r)) * (t < 0.12f ? 0.5f : 0f);
                float thump = thumpLp.Process(White(r)) * Env(t - 0.1f, 0.002f, 0.06f) * 3f + Sin(TwoPi * 85f * (t - 0.1f)) * Env(t - 0.1f, 0.001f, 0.07f) * 0.9f;
                float tw = t - 0.15f;
                float whistle = 0f;
                if (tw > 0f)
                {
                    phase += Lerp(900f, 2600f, Clamp01(tw / 1.4f)) / Rate;
                    whistle = (Sin(TwoPi * phase) * 0.22f + White(r) * 0.04f) * Clamp01(tw / 0.15f) * Exp(-tw / 0.9f); // fades as it climbs
                }
                buf[i] = fizz + thump + whistle;
            }
            ApplyReverb(buf, 0.6f, 0.4f, 0.25f);
            Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>The shell bursting overhead: a hard crack, a boom that rolls off the buildings, a long crackle as the stars fall.</summary>
        static float[] FireworkBurst(Random r)
        {
            var buf = new float[Samples(4.5f)];
            var crackHp = new OnePoleHP(1200f);
            var boomLp = Biquad.LowPass(380f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = crackHp.Process(White(r)) * Env(t, 0.0002f, 0.012f) * 2f
                       + boomLp.Process(White(r)) * Env(t, 0.003f, 0.35f) * 2.2f
                       + Sin(TwoPi * 48f * t) * Env(t, 0.004f, 0.25f) * 0.8f;
            }
            // Crackle: hundreds of tiny pops spread over the fall of the stars.
            for (int k = 0; k < 260; k++)
            {
                int s0 = Samples(0.35f + Range(r, 0f, 2.6f)), len = Samples(Range(r, 0.003f, 0.012f));
                float a = Range(r, 0.08f, 0.35f);
                for (int j = 0; j < len && s0 + j < buf.Length; j++) buf[s0 + j] += White(r) * a * (1f - (float)j / len);
            }
            ApplyReverb(buf, 0.9f, 0.35f, 0.45f);
            Normalize(buf, 0.9f);
            Fade(buf, 0f, 0.6f);
            return buf;
        }
    }
}
