using System;
using System.Collections.Generic;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>
    /// Vehicles and the crash: engine loops (pitched by speed at runtime), a stuck horn, the impact and rollover,
    /// tyre squeal, metal scraping on asphalt, the ringing ears afterwards, and the tape-skip of a cut in the footage.
    /// </summary>
    public static partial class SynthLibrary
    {
        static IEnumerable<Recipe> VehicleRecipes()
        {
            yield return new Recipe { id = SoundId.EngineDiesel, render = r => Engine(r, 36f, 0.04f, 0.1f, 0.35f, true) };
            yield return new Recipe { id = SoundId.EngineTruck, render = r => Engine(r, 44f, 0.22f, 0.25f, 0.15f, false) };
            yield return new Recipe { id = SoundId.CarHorn, render = CarHorn };
            yield return new Recipe { id = SoundId.CrashImpact, variations = 2, render = CrashImpact };
            yield return new Recipe { id = SoundId.MetalScrape, render = MetalScrape };
            yield return new Recipe { id = SoundId.TireScreech, render = TireScreech };
            yield return new Recipe { id = SoundId.Tinnitus, render = Tinnitus };
            yield return new Recipe { id = SoundId.TapeSkip, render = TapeSkip };
        }

        /// <summary>
        /// Engine loop at idle. Each firing is a decaying burst of filtered noise plus a body tone; `lope` makes the
        /// firing intervals uneven (a cross-plane V8), `jitter` varies each combustion. Diesels add injector clatter
        /// and the cab's road rumble. Runtime pitch raises the revs.
        /// </summary>
        static float[] Engine(Random r, float firingHz, float lope, float jitter, float clatter, bool cabRumble)
        {
            const float len = 4f;
            int n = Samples(len + 1f);
            var buf = new float[n];
            var noiseLp = Biquad.LowPass(firingHz * 9f, 0.8f);
            var body = Biquad.Peak(firingHz * 2.6f, 1.4f, 7f);
            var outLp = Biquad.LowPass(1100f);
            var clatterHp = Biquad.HighPass(3200f);
            var rumble = new Brown();
            var rumbleLp = Biquad.LowPass(130f);
            float phase = 0f, env = 0f, amp = 1f, tick = 0f;
            int cylinder = 0;
            float decay = Exp(-firingHz * 2.2f / Rate);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                // Uneven firing order: alternating short and long gaps, plus a slow idle hunt.
                float interval = 1f + ((cylinder & 1) == 0 ? lope : -lope) * ((cylinder & 2) == 0 ? 1f : 0.6f);
                phase += firingHz * (1f + 0.015f * Sin(TwoPi * 0.7f * t)) / interval / Rate;
                if (phase >= 1f)
                {
                    phase -= 1f;
                    cylinder = (cylinder + 1) & 7;
                    amp = 1f + (Rand(r) - 0.5f) * 2f * jitter;
                    env = amp;
                    tick = clatter * Range(r, 0.6f, 1f);
                }
                env *= decay;
                tick *= 0.995f;
                float v = noiseLp.Process(White(r)) * env * 1.6f + Sin(TwoPi * firingHz * 0.5f * t) * 0.25f + Sin(TwoPi * firingHz * t) * env * 0.4f;
                v = body.Process(v);
                v += clatterHp.Process(White(r)) * tick * env;
                if (cabRumble) v += rumbleLp.Process(rumble.Next(r)) * 0.9f;
                buf[i] = outLp.Process((float)Math.Tanh(v * 1.4f));
            }
            Normalize(buf, 0.7f);
            return MakeLoop(buf, 1f);
        }

        /// <summary>Dual-tone horn (two integer frequencies, so a 1 s clip loops without a seam).</summary>
        static float[] CarHorn(Random r)
        {
            var buf = new float[Samples(1f)];
            var bp = Biquad.BandPass(950f, 0.6f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float a = Math.Sign(Sin(TwoPi * 420f * t)) * 0.5f, b = Math.Sign(Sin(TwoPi * 500f * t)) * 0.45f;
                buf[i] = (float)Math.Tanh(bp.Process(a + b) * 2.2f);
            }
            Normalize(buf, 0.6f);
            return buf;
        }

        /// <summary>
        /// The ram: a sub boom, crumpling sheet metal (inharmonic modes with ragged onsets), the windscreen
        /// shattering, and debris settling. The rollover landing plays it again, pitched down.
        /// </summary>
        static float[] CrashImpact(Random r)
        {
            float dur = 3.2f;
            var buf = new float[Samples(dur)];
            var boomLp = Biquad.LowPass(220f);
            var crunchBp = Biquad.BandPass(Range(r, 600f, 900f), 0.7f);
            var glassHp = Biquad.HighPass(3500f);
            var modes = new List<(float t0, float f, float decay, float amp)>();
            for (int k = 0; k < 26; k++)
                modes.Add((Range(r, 0f, 0.55f) * Range(r, 0.2f, 1f), Range(r, 180f, 3200f), Range(r, 0.05f, 0.35f), Range(r, 0.15f, 0.6f)));
            var tinkles = new List<(float t0, float f, float amp)>();
            for (int k = 0; k < 60; k++)
                tinkles.Add((0.03f + (float)Math.Pow(Rand(r), 1.6) * 1.6f, Range(r, 3000f, 9000f), Range(r, 0.05f, 0.25f)));
            float boomF = Range(r, 38f, 48f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = Sin(TwoPi * boomF * t * (1f - 0.25f * Clamp01(t * 2f))) * Env(t, 0.002f, 0.45f) * 1.4f;
                v += boomLp.Process(White(r)) * Env(t, 0.001f, 0.12f) * 2.4f;
                v += crunchBp.Process(White(r)) * Env(t, 0.003f, 0.35f) * (0.6f + 0.4f * Rand(r)) * 1.8f;
                foreach (var m in modes)
                {
                    float tm = t - m.t0;
                    if (tm > 0f && tm < m.decay * 5f) v += Sin(TwoPi * m.f * tm) * Env(tm, 0.0005f, m.decay) * m.amp;
                }
                v += glassHp.Process(White(r)) * Env(t - 0.02f, 0.001f, 0.18f) * 0.9f;
                foreach (var g in tinkles)
                {
                    float tg = t - g.t0;
                    if (tg > 0f && tg < 0.06f) v += Sin(TwoPi * g.f * tg) * Env(tg, 0.0002f, 0.012f) * g.amp;
                }
                if (t > 0.4f && t < 2.6f && r.NextDouble() < 0.0015 * Env(t - 0.4f, 0.01f, 0.8f)) v += White(r) * 0.4f; // debris
                buf[i] = (float)Math.Tanh(v * 1.2f);
            }
            Normalize(buf, 0.95f);
            Fade(buf, 0f, 0.4f);
            return buf;
        }

        /// <summary>Steel sliding on asphalt: a ragged band of noise with a wandering squeal.</summary>
        static float[] MetalScrape(Random r)
        {
            float dur = 2.6f;
            var buf = new float[Samples(dur)];
            var bp = Biquad.BandPass(2000f, 1.2f);
            var grit = Biquad.HighPass(1500f);
            var wander = new OnePoleLP(3f);
            float sq = Range(r, 1900f, 2600f), ph = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float w = wander.Process(White(r)) * 40f;
                if ((i & 31) == 0) bp.SetBandPass(Math.Max(600f, 1800f + w * 900f), 1.2f);
                float env = Clamp01(t / 0.05f) * Clamp01((dur - t) / 1.4f);
                ph += (sq + w * 300f) / Rate;
                float squeal = Sin(TwoPi * ph) * 0.25f * (0.5f + 0.5f * Sin(TwoPi * 7f * t));
                buf[i] = (bp.Process(White(r)) * 1.4f + grit.Process(White(r)) * (r.NextDouble() < 0.3 ? 0.5f : 0.1f) + squeal) * env;
            }
            Normalize(buf, 0.7f);
            return buf;
        }

        static float[] TireScreech(Random r)
        {
            float dur = 1.7f;
            var buf = new float[Samples(dur)];
            var bp = Biquad.BandPass(1300f, 3f);
            var flutter = new OnePoleLP(14f);
            float f0 = Range(r, 1050f, 1250f), ph = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Clamp01(t / 0.12f) * Clamp01((dur - t) / 0.5f);
                float fl = 1f + flutter.Process(White(r)) * 3f;
                ph += f0 * (1f - 0.08f * t) * fl / Rate;
                float tone = Sin(TwoPi * ph) * 0.5f + Sin(TwoPi * ph * 2f) * 0.2f + Sin(TwoPi * ph * 3f) * 0.08f;
                buf[i] = (tone + bp.Process(White(r)) * 0.8f) * env;
            }
            Normalize(buf, 0.65f);
            return buf;
        }

        /// <summary>Ringing ears after the crash: a high tone with slow beating that fades over nine seconds.</summary>
        static float[] Tinnitus(Random r)
        {
            float dur = 9f;
            var buf = new float[Samples(dur)];
            var hum = Biquad.LowPass(160f);
            var brown = new Brown();
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float fade = (float)Math.Pow(1f - t / dur, 1.6);
                float ring = (Sin(TwoPi * 3900f * t) + Sin(TwoPi * 3906f * t) * 0.6f) * 0.5f * Clamp01(t / 0.05f) * fade;
                buf[i] = ring * 0.6f + hum.Process(brown.Next(r)) * 0.8f * fade;
            }
            Normalize(buf, 0.55f);
            Fade(buf, 0.02f, 0.5f);
            return buf;
        }

        /// <summary>A cut in the tape: head-switch clicks, chirps sweeping as the transport skips, choppy hiss.</summary>
        static float[] TapeSkip(Random r)
        {
            float dur = 0.75f;
            var buf = new float[Samples(dur)];
            var hp = Biquad.HighPass(700f);
            float ph = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float sweep = 300f + 2400f * (float)Math.Pow((t * 3f) % 1f, 2);
                ph += sweep / Rate;
                float gate = (int)(t * 34f) % 3 == 0 ? 0.25f : 1f;
                float v = hp.Process(White(r)) * 0.5f * gate + Sin(TwoPi * ph) * 0.25f * gate;
                if (i % Samples(0.0167f) < 30) v += White(r) * 0.8f; // head switching
                buf[i] = v * Clamp01((dur - t) / 0.15f);
            }
            Normalize(buf, 0.6f);
            Fade(buf, 0.005f, 0.05f);
            return buf;
        }
    }
}
