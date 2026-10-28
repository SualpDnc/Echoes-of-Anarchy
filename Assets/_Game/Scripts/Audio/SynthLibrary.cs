using System;
using System.Collections.Generic;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    public enum SoundId
    {
        StepAsphalt, StepConcrete, StepMetal, StepWood, Land, Jump, Breath, FlashlightClick,
        AmbienceCity, Wind, Fire, Steam, LampBuzz, SignalTick,
        GunPistolFar, GunRifleFar, Scream, DogBark, CarAlarm, PoliceSiren, Explosion, Firework, GlassBreak,
        PurgeSiren, EBSTone,
        GunPistolNear, DryFire, MagOut, MagIn, SlideRack, ShellCasing, ImpactHard, ImpactMetal, ImpactFlesh, Whizz,
        TensionHit, Heartbeat, BodyFall, BatSwing, BatHit, Bandage,
        VoiceDaniel, VoiceLuis, VoiceMiaPhone, VoiceMiaClone, VoiceDispatch, VoiceMale, VoiceFemale, VoiceSmiler, VoiceHale,
        PhoneRing, PhonePickup, PhoneHangup, VoicemailBeep, RadioSquelch,
        EngineDiesel, EngineTruck, CarHorn, CrashImpact, MetalScrape, TireScreech, Tinnitus, TapeSkip,
        VoiceLuisRadio, MagDrop, MagPouch, MagSeat, ImpactHead, FireworkLaunch, FireworkBurst,
        MusicBox, DoorSlam, VoiceDanielThought, GateScreech, ArmorHum, VoiceVance,
        PartyMusic, CrowdWalla, CrowdRoar, MachineGun
    }

    /// <summary>
    /// Every sound in the game as a synthesis recipe. Recipes are pure functions of a seeded Random so they
    /// can be rendered in parallel at load time. Distant sounds have their street reverb and air absorption baked in.
    /// </summary>
    public static partial class SynthLibrary
    {
        public class Recipe
        {
            public SoundId id;
            public int variations = 1;
            public bool stereo;
            public Func<Random, float[]> render;
        }

        public static List<Recipe> All()
        {
            var all = new List<Recipe>
            {
                new Recipe { id = SoundId.StepAsphalt, variations = 8, render = r => Step(r, 950f, 0.9f, 0.18f, Surface.Grit) },
                new Recipe { id = SoundId.StepConcrete, variations = 8, render = r => Step(r, 1500f, 1.3f, 0.1f, Surface.Hard) },
                new Recipe { id = SoundId.StepMetal, variations = 8, render = r => Step(r, 700f, 1.0f, 0.05f, Surface.Metal) },
                new Recipe { id = SoundId.StepWood, variations = 8, render = r => Step(r, 380f, 1.6f, 0.05f, Surface.Wood) },
                new Recipe { id = SoundId.Land, variations = 3, render = Land },
                new Recipe { id = SoundId.Jump, variations = 2, render = Jump },
                new Recipe { id = SoundId.Breath, render = Breath },
                new Recipe { id = SoundId.FlashlightClick, render = Click },
                new Recipe { id = SoundId.AmbienceCity, stereo = true, render = AmbienceCity },
                new Recipe { id = SoundId.Wind, stereo = true, render = Wind },
                new Recipe { id = SoundId.Fire, render = Fire },
                new Recipe { id = SoundId.Steam, render = Steam },
                new Recipe { id = SoundId.LampBuzz, render = LampBuzz },
                new Recipe { id = SoundId.SignalTick, render = SignalTick },
                new Recipe { id = SoundId.GunPistolFar, variations = 4, render = r => GunFar(r, false) },
                new Recipe { id = SoundId.GunRifleFar, variations = 3, render = r => GunFar(r, true) },
                new Recipe { id = SoundId.Scream, variations = 5, render = Scream },
                new Recipe { id = SoundId.DogBark, variations = 3, render = DogBark },
                new Recipe { id = SoundId.CarAlarm, variations = 2, render = CarAlarm },
                new Recipe { id = SoundId.PoliceSiren, render = PoliceSiren },
                new Recipe { id = SoundId.Explosion, variations = 2, render = Explosion },
                new Recipe { id = SoundId.Firework, variations = 3, render = Firework },
                new Recipe { id = SoundId.GlassBreak, variations = 3, render = GlassBreak },
                new Recipe { id = SoundId.PurgeSiren, render = PurgeSiren },
                new Recipe { id = SoundId.EBSTone, render = EBSTone },
                new Recipe { id = SoundId.GunPistolNear, variations = 3, render = GunNear },
                new Recipe { id = SoundId.DryFire, render = DryFire },
                new Recipe { id = SoundId.MagOut, render = r => Mech(r, 0.18f, 1800f, 0.6f) },
                new Recipe { id = SoundId.MagIn, render = r => Mech(r, 0.12f, 2400f, 1f) },
                new Recipe { id = SoundId.SlideRack, render = SlideRack },
                new Recipe { id = SoundId.ShellCasing, variations = 4, render = ShellCasing },
                new Recipe { id = SoundId.ImpactHard, variations = 4, render = ImpactHard },
                new Recipe { id = SoundId.ImpactMetal, variations = 3, render = ImpactMetal },
                new Recipe { id = SoundId.ImpactFlesh, variations = 3, render = ImpactFlesh },
                new Recipe { id = SoundId.Whizz, variations = 3, render = Whizz },
                new Recipe { id = SoundId.TensionHit, render = TensionHit },
                new Recipe { id = SoundId.Heartbeat, render = Heartbeat },
                new Recipe { id = SoundId.BodyFall, variations = 2, render = BodyFall },
                new Recipe { id = SoundId.BatSwing, render = BatSwing },
                new Recipe { id = SoundId.BatHit, variations = 2, render = BatHit },
                new Recipe { id = SoundId.Bandage, render = Bandage },
                new Recipe { id = SoundId.MagDrop, variations = 2, render = MagDrop },
                new Recipe { id = SoundId.MagPouch, render = MagPouch },
                new Recipe { id = SoundId.MagSeat, render = MagSeat },
                new Recipe { id = SoundId.ImpactHead, variations = 2, render = ImpactHead },
                new Recipe { id = SoundId.FireworkLaunch, render = FireworkLaunch },
                new Recipe { id = SoundId.FireworkBurst, render = FireworkBurst },
                new Recipe { id = SoundId.MusicBox, render = MusicBox },
                new Recipe { id = SoundId.DoorSlam, variations = 2, render = DoorSlam },
                new Recipe { id = SoundId.GateScreech, variations = 2, render = GateScreech },
                new Recipe { id = SoundId.ArmorHum, render = ArmorHum },
                new Recipe { id = SoundId.PartyMusic, render = PartyMusic },
                new Recipe { id = SoundId.CrowdWalla, render = CrowdWalla },
                new Recipe { id = SoundId.CrowdRoar, render = CrowdRoar },
                new Recipe { id = SoundId.MachineGun, variations = 4, render = MachineGun },
            };
            all.AddRange(VoiceRecipes());
            all.AddRange(VehicleRecipes());
            return all;
        }

        // ================================================================== player foley

        enum Surface { Grit, Hard, Metal, Wood }

        /// <summary>Heel strike + toe roll + grit scuff, plus surface-specific resonance.</summary>
        static float[] Step(Random r, float heelF, float q, float grit, Surface s)
        {
            var buf = new float[Samples(0.42f)];
            float f = heelF * Range(r, 0.85f, 1.15f);
            var heelBp = Biquad.BandPass(f, q);
            var toeBp = Biquad.BandPass(f * Range(r, 1.1f, 1.4f), q);
            var gritHp = new OnePoleHP(3500f);
            float toeT = Range(r, 0.06f, 0.11f);
            float heelAmp = Range(r, 0.8f, 1f), toeAmp = Range(r, 0.3f, 0.5f);
            float thudF = Range(r, 70f, 100f);

            // Metal grating / wood plank resonant modes.
            float[] modes = s == Surface.Metal ? new[] { 310f, 760f, 1430f, 2610f, 3900f }
                          : s == Surface.Wood ? new[] { 170f, 410f, 690f } : new float[0];
            float[] modeF = new float[modes.Length];
            for (int i = 0; i < modes.Length; i++) modeF[i] = modes[i] * Range(r, 0.92f, 1.08f);
            float modeDecay = s == Surface.Metal ? 0.22f : 0.05f;

            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float heel = heelBp.Process(White(r)) * Env(t, 0.0012f, 0.018f) * heelAmp * 2.2f;
                float thud = Sin(TwoPi * thudF * t) * Env(t, 0.002f, 0.03f) * 0.55f;
                float toe = toeBp.Process(White(r)) * Env(t - toeT, 0.002f, 0.022f) * toeAmp * 2.2f;
                float scuff = gritHp.Process(White(r)) * Env(t - toeT * 0.5f, 0.012f, 0.045f) * grit;
                // Sparse grit crackle on asphalt.
                if (s == Surface.Grit && r.NextDouble() < 0.004 * Env(t, 0.005f, 0.08f)) scuff += White(r) * 0.6f;
                float ring = 0f;
                for (int m = 0; m < modeF.Length; m++)
                    ring += Sin(TwoPi * modeF[m] * t) * (Env(t, 0.001f, modeDecay / (1 + m * 0.4f)) + Env(t - toeT, 0.001f, modeDecay * 0.5f) * 0.4f) / (1 + m);
                buf[i] = heel + thud + toe + scuff + ring * (s == Surface.Metal ? 0.35f : 0.5f);
            }
            Normalize(buf, Range(r, 0.75f, 0.9f));
            Fade(buf, 0f, 0.03f);
            return buf;
        }

        static float[] Land(Random r)
        {
            var buf = new float[Samples(0.6f)];
            var bp = Biquad.BandPass(800f, 0.8f);
            var bp2 = Biquad.BandPass(950f, 0.8f);
            float gap = Range(r, 0.015f, 0.04f);
            // Gear rattle: keys, buckles and the med bag shifting.
            var pings = new List<(float t, float f, float a)>();
            for (int k = 0; k < 14; k++) pings.Add((Range(r, 0.02f, 0.2f), Range(r, 2500f, 6500f), Range(r, 0.05f, 0.15f)));
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = bp.Process(White(r)) * Env(t, 0.001f, 0.03f) * 2f + bp2.Process(White(r)) * Env(t - gap, 0.001f, 0.03f) * 1.8f;
                v += Sin(TwoPi * 55f * t) * Env(t, 0.003f, 0.09f) * 1.1f;
                foreach (var p in pings) v += Sin(TwoPi * p.f * (t - p.t)) * Env(t - p.t, 0.0005f, 0.018f) * p.a;
                buf[i] = v;
            }
            Normalize(buf, 0.95f);
            Fade(buf, 0f, 0.05f);
            return buf;
        }

        static float[] Jump(Random r)
        {
            var buf = new float[Samples(0.35f)];
            var bp = Biquad.BandPass(Range(r, 700f, 1000f), 0.6f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Sin((float)Math.PI * Clamp01(t / 0.3f));
                buf[i] = bp.Process(White(r)) * env * env;
            }
            Normalize(buf, 0.5f);
            return buf;
        }

        /// <summary>Exhausted breathing cycle (inhale → exhale), loops cleanly because both ends are silent.</summary>
        static float[] Breath(Random r)
        {
            var buf = new float[Samples(2.6f)];
            var inA = Biquad.BandPass(1600f, 2f); var inB = Biquad.BandPass(2800f, 3f);
            var exA = Biquad.BandPass(850f, 1.5f); var exB = Biquad.BandPass(1700f, 2f);
            var voice = new OnePoleLP(400f);
            float phase = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float inEnv = t < 1.0f ? (float)Math.Pow(Sin((float)Math.PI * t / 1.0f), 1.5) : 0f;
                float exT = t - 1.15f;
                float exEnv = exT > 0f && exT < 0.95f ? (float)Math.Pow(Sin((float)Math.PI * exT / 0.95f), 1.2) : 0f;
                float n = White(r);
                float inhale = (inA.Process(n) * 0.7f + inB.Process(n) * 0.5f) * inEnv * 0.8f;
                phase += 105f / Rate;
                float saw = (phase - (float)Math.Floor(phase)) * 2f - 1f;
                float exhale = (exA.Process(n) + exB.Process(n) * 0.6f) * exEnv * 1.3f + voice.Process(saw) * exEnv * 0.06f;
                buf[i] = inhale + exhale;
            }
            Normalize(buf, 0.7f);
            return buf;
        }

        static float[] Click(Random r)
        {
            var buf = new float[Samples(0.12f)];
            var hp = new OnePoleHP(1800f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float c1 = (hp.Process(White(r)) * Env(t, 0.0002f, 0.002f) + Sin(TwoPi * 3200f * t) * Env(t, 0.0002f, 0.007f) * 0.5f);
                float t2 = t - 0.045f;
                float c2 = Sin(TwoPi * 2700f * t2) * Env(t2, 0.0002f, 0.006f) * 0.35f + White(r) * Env(t2, 0.0002f, 0.0015f) * 0.3f;
                buf[i] = c1 + c2;
            }
            Normalize(buf, 0.6f);
            return buf;
        }

        // ================================================================== ambience beds (stereo, interleaved)

        /// <summary>Empty-city bed: sub rumble, distant urban roar, transformer hum. Decorrelated L/R.</summary>
        static float[] AmbienceCity(Random r)
        {
            int n = Samples(43f);
            var o = new float[n * 2];
            for (int ch = 0; ch < 2; ch++)
            {
                var brown = new Brown(); var pink = new Pink();
                var lp = Biquad.LowPass(140f); var roar = Biquad.BandPass(380f, 0.5f);
                float lfoPhase = Range(r, 0f, 6.28f);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float swell = 0.75f + 0.25f * Sin(TwoPi * 0.045f * t + lfoPhase);
                    float v = lp.Process(brown.Next(r)) * 0.9f;
                    v += roar.Process(pink.Next(r)) * 0.35f * swell;
                    v += (Sin(TwoPi * 60f * t) * 0.6f + Sin(TwoPi * 120f * t) * 0.3f + Sin(TwoPi * 180f * t) * 0.1f) * 0.012f;
                    o[i * 2 + ch] = v;
                }
            }
            Normalize(o, 0.6f);
            return MakeLoop(o, 3f, 2);
        }

        /// <summary>Gusting wind between buildings with occasional whistle.</summary>
        static float[] Wind(Random r)
        {
            int n = Samples(33f);
            var o = new float[n * 2];
            for (int ch = 0; ch < 2; ch++)
            {
                var bp = Biquad.BandPass(500f, 0.7f);
                var whistle = Biquad.BandPass(1500f, 18f);
                var gustLp = new OnePoleLP(0.25f);
                float a = Range(r, 0f, 6.28f), b = Range(r, 0f, 6.28f);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float g = 0.5f + 0.3f * Sin(TwoPi * 0.07f * t + a) + 0.2f * Sin(TwoPi * 0.19f * t + b);
                    g = Clamp01(g + gustLp.Process(White(r)) * 60f);
                    if ((i & 31) == 0) bp.SetBandPass(300f + 700f * g, 0.7f);
                    float noise = White(r);
                    float v = bp.Process(noise) * g * g;
                    if ((i & 63) == 0) whistle.SetBandPass(1200f + 900f * g, 18f);
                    v += whistle.Process(noise) * Math.Max(0f, g - 0.65f) * 1.5f;
                    o[i * 2 + ch] = v;
                }
            }
            Normalize(o, 0.6f);
            return MakeLoop(o, 3f, 2);
        }

        // ================================================================== world loops (mono, 3D)

        static float[] Fire(Random r)
        {
            int n = Samples(9f);
            var buf = new float[n];
            var brown = new Brown();
            var roarLp = Biquad.LowPass(450f);
            var modLp = new OnePoleLP(1.5f);
            var crackle = new List<(int start, float f, float decay, float amp)>();
            for (float t = 0f; t < 9f; t += -(float)Math.Log(1.0 - r.NextDouble()) / 28f)
            {
                bool pop = r.NextDouble() < 0.06;
                crackle.Add((Samples(t), pop ? Range(r, 500f, 1400f) : Range(r, 1500f, 5500f), pop ? Range(r, 0.015f, 0.03f) : Range(r, 0.002f, 0.01f),
                             pop ? Range(r, 0.6f, 1f) : (float)Math.Pow(r.NextDouble(), 2) * 0.6f));
            }
            for (int i = 0; i < n; i++)
            {
                float mod = 0.6f + Clamp01(modLp.Process(White(r)) * 20f + 0.5f) * 0.6f;
                buf[i] = roarLp.Process(brown.Next(r)) * 0.9f * mod;
            }
            foreach (var c in crackle)
            {
                var bp = Biquad.BandPass(c.f, 1.5f);
                int len = Samples(c.decay * 6f);
                for (int k = 0; k < len && c.start + k < n; k++)
                {
                    float t = (float)k / Rate;
                    buf[c.start + k] += bp.Process(White(r)) * Env(t, 0.0002f, c.decay) * c.amp * 3f;
                }
            }
            Normalize(buf, 0.8f);
            return MakeLoop(buf, 1f);
        }

        static float[] Steam(Random r)
        {
            int n = Samples(7f);
            var buf = new float[n];
            var hp = Biquad.HighPass(1200f);
            var bp = Biquad.BandPass(3200f, 0.6f);
            var flutter = new OnePoleLP(6f);
            for (int i = 0; i < n; i++)
            {
                float x = bp.Process(hp.Process(White(r)));
                buf[i] = x * (0.85f + Clamp01(flutter.Process(White(r)) * 10f + 0.5f) * 0.3f);
            }
            Normalize(buf, 0.6f);
            return MakeLoop(buf, 1f);
        }

        /// <summary>Failing sodium ballast: 120 Hz buzz with harmonics and random arcing crackle.</summary>
        static float[] LampBuzz(Random r)
        {
            int n = Samples(4f);
            var buf = new float[n];
            var bp = Biquad.BandPass(240f, 1.5f);
            var bp2 = Biquad.BandPass(720f, 2f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float sq = Math.Sign(Sin(TwoPi * 120f * t));
                float v = bp.Process(sq) * 0.6f + bp2.Process(sq) * 0.3f;
                if (r.NextDouble() < 0.0004) v += White(r) * 2f;
                buf[i] = v;
            }
            Normalize(buf, 0.5f);
            Fade(buf, 0.002f, 0.002f);
            return buf;
        }

        static float[] SignalTick(Random r)
        {
            var buf = new float[Samples(1f)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float a = Sin(TwoPi * 2200f * t) * Env(t, 0.0003f, 0.006f) + White(r) * Env(t, 0.0002f, 0.002f) * 0.5f;
                float t2 = t - 0.5f;
                float b = Sin(TwoPi * 1900f * t2) * Env(t2, 0.0003f, 0.005f) * 0.7f + White(r) * Env(t2, 0.0002f, 0.0015f) * 0.3f;
                buf[i] = a + b;
            }
            Normalize(buf, 0.4f);
            return buf;
        }

        // ================================================================== distant events (street reverb baked in)

        /// <summary>Distant gunshot with slapback echoes off facades and a long street tail.</summary>
        static float[] GunFar(Random r, bool rifle)
        {
            var buf = new float[Samples(3f)];
            var crackHp = new OnePoleHP(1500f);
            var bodyLp = Biquad.LowPass(rifle ? 1800f : 1100f);
            var boomLp = Biquad.LowPass(Range(r, 90f, 130f)); // rumble as filtered noise: a tuned sine read as a drum
            float bodyDecay = rifle ? 0.07f : 0.05f;
            var shot = new float[Samples(0.4f)];
            for (int i = 0; i < shot.Length; i++)
            {
                float t = (float)i / Rate;
                float crack = crackHp.Process(White(r)) * Env(t, 0.0002f, rifle ? 0.005f : 0.003f) * (rifle ? 1.6f : 1.1f);
                float body = bodyLp.Process(White(r)) * Env(t, 0.0008f, bodyDecay) * 1.2f;
                float boom = boomLp.Process(White(r)) * Env(t, 0.002f, 0.12f) * 9f;
                shot[i] = crack + body + boom;
            }
            Mix(buf, shot, Samples(0.02f), 1f);
            // Slapback echoes, each duller than the last.
            int taps = r.Next(3, 6);
            for (int k = 0; k < taps; k++)
            {
                var echo = (float[])shot.Clone();
                var lp = Biquad.LowPass(1600f / (k + 1));
                for (int i = 0; i < echo.Length; i++) echo[i] = lp.Process(echo[i]);
                Mix(buf, echo, Samples(0.02f + Range(r, 0.1f, 0.9f)), Range(r, 0.15f, 0.4f) / (k + 1));
            }
            ApplyReverb(buf, 0.85f, 0.45f, 0.7f);
            var air = Biquad.LowPass(2800f);
            for (int i = 0; i < buf.Length; i++) buf[i] = air.Process(buf[i]);
            Normalize(buf, 0.9f);
            Fade(buf, 0f, 0.4f);
            return buf;
        }

        /// <summary>Distant human scream: glottal sawtooth with jitter through "ah" formants, strained by soft clipping.</summary>
        static float[] Scream(Random r)
        {
            float dur = Range(r, 1.1f, 2.1f);
            var buf = new float[Samples(dur + 1.5f)];
            float f0 = Range(r, 520f, 760f), peak = Range(r, 880f, 1150f);
            var f1 = Biquad.BandPass(Range(r, 850f, 1000f), 5f);
            var f2 = Biquad.BandPass(Range(r, 1300f, 1600f), 6f);
            var f3 = Biquad.BandPass(Range(r, 2700f, 3100f), 8f);
            var jitterLp = new OnePoleLP(30f);
            float phase = 0f;
            for (int i = 0; i < Samples(dur); i++)
            {
                float t = (float)i / Rate;
                float p = t / dur;
                float contour = p < 0.25f ? Lerp(f0, peak, p / 0.25f) : Lerp(peak, f0 * 0.85f, (p - 0.25f) / 0.75f);
                float vib = 1f + 0.025f * Sin(TwoPi * 6.5f * t) + jitterLp.Process(White(r)) * 0.4f;
                phase += contour * vib / Rate;
                float saw = (phase - (float)Math.Floor(phase)) * 2f - 1f;
                float src = saw + White(r) * 0.35f;
                float v = f1.Process(src) + f2.Process(src) * 0.6f + f3.Process(src) * 0.35f;
                float env = Clamp01(t / 0.08f) * Clamp01((dur - t) / 0.35f);
                buf[i] = (float)Math.Tanh(v * 3f) * env;
            }
            ApplyReverb(buf, 0.8f, 0.5f, 0.55f);
            var air = Biquad.LowPass(3200f);
            for (int i = 0; i < buf.Length; i++) buf[i] = air.Process(buf[i]);
            Normalize(buf, 0.8f);
            Fade(buf, 0.005f, 0.3f);
            return buf;
        }

        static float[] DogBark(Random r)
        {
            int barks = r.Next(2, 5);
            var buf = new float[Samples(barks * 0.55f + 1.5f)];
            float t0 = 0.05f;
            for (int b = 0; b < barks; b++)
            {
                var fa = Biquad.BandPass(Range(r, 650f, 800f), 4f);
                var fb = Biquad.BandPass(Range(r, 1400f, 1700f), 5f);
                float startF = Range(r, 380f, 480f), phase = 0f;
                int s0 = Samples(t0), len = Samples(0.2f);
                for (int i = 0; i < len && s0 + i < buf.Length; i++)
                {
                    float t = (float)i / Rate;
                    phase += Lerp(startF, startF * 0.6f, t / 0.2f) / Rate;
                    float saw = (phase - (float)Math.Floor(phase)) * 2f - 1f;
                    float src = saw * 0.7f + White(r) * 0.5f;
                    buf[s0 + i] += (fa.Process(src) + fb.Process(src) * 0.5f) * Env(t, 0.005f, 0.07f) * 3f;
                }
                t0 += Range(r, 0.35f, 0.6f);
            }
            ApplyReverb(buf, 0.75f, 0.5f, 0.5f);
            Normalize(buf, 0.75f);
            Fade(buf, 0f, 0.3f);
            return buf;
        }

        /// <summary>Classic six-tone car alarm cycle through a small horn speaker.</summary>
        static float[] CarAlarm(Random r)
        {
            float dur = 12f;
            var buf = new float[Samples(dur + 1.2f)];
            var speaker = Biquad.BandPass(1700f, 0.7f);
            float phase = 0f;
            float detune = Range(r, 0.93f, 1.07f);
            for (int i = 0; i < Samples(dur); i++)
            {
                float t = (float)i / Rate;
                float seg = t % 12f, f; bool on = true;
                if (seg < 2.5f) f = 1000f + 450f * Sin(TwoPi * 3.2f * seg);                       // whoop
                else if (seg < 5f) { f = 1450f; on = (seg * 4f) % 1f < 0.5f; }                    // beep
                else if (seg < 7.5f) f = ((int)(seg * 10f) & 1) == 0 ? 900f : 1300f;             // warble
                else if (seg < 10f) f = 600f + 1500f * ((seg * 4f) % 1f);                         // rising sweeps
                else { f = 1200f; on = (seg * 6f) % 1f < 0.5f; }                                 // fast chirp
                phase += f * detune / Rate;
                float sq = (phase - (float)Math.Floor(phase)) < 0.5f ? 1f : -1f;
                buf[i] = on ? speaker.Process(sq) : speaker.Process(0f);
            }
            ApplyReverb(buf, 0.8f, 0.5f, 0.45f);
            var air = Biquad.LowPass(3500f);
            for (int i = 0; i < buf.Length; i++) buf[i] = air.Process(buf[i]);
            Normalize(buf, 0.7f);
            Fade(buf, 0.01f, 0.5f);
            return buf;
        }

        /// <summary>Police "wail" loop — two sweeps, 8 s, seamless.</summary>
        static float[] PoliceSiren(Random r)
        {
            var buf = new float[Samples(8.5f)];
            var horn = Biquad.BandPass(1300f, 0.8f);
            var lp = Biquad.LowPass(4200f);
            float phase = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 700f + 650f * (0.5f - 0.5f * (float)Math.Cos(TwoPi * t / 4f));
                phase += f / Rate;
                float p = phase * TwoPi;
                float sq = Sin(p) + Sin(3 * p) / 3f + Sin(5 * p) / 5f + Sin(7 * p) / 7f;
                buf[i] = lp.Process(horn.Process(sq));
            }
            Normalize(buf, 0.75f);
            return MakeLoop(buf, 0.5f);
        }

        static float[] Explosion(Random r)
        {
            var buf = new float[Samples(5.5f)];
            var brown = new Brown();
            var boomLp = Biquad.LowPass(160f);
            var midLp = Biquad.LowPass(700f);
            var crackHp = new OnePoleHP(1200f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = crackHp.Process(White(r)) * Env(t, 0.0005f, 0.012f) * 1.2f;
                v += boomLp.Process(brown.Next(r)) * Env(t, 0.006f, 0.9f) * 6f;
                v += midLp.Process(White(r)) * Env(t, 0.004f, 0.25f) * 1.1f;
                if (t > 0.3f && t < 2.5f && r.NextDouble() < 0.0015) v += White(r) * 0.4f; // falling debris
                buf[i] = v;
            }
            ApplyReverb(buf, 0.95f, 0.55f, 0.6f);
            var air = Biquad.LowPass(1300f);
            for (int i = 0; i < buf.Length; i++) buf[i] = air.Process(buf[i]);
            Normalize(buf, 0.95f);
            Fade(buf, 0f, 0.8f);
            return buf;
        }

        static float[] Firework(Random r)
        {
            var buf = new float[Samples(4f)];
            float popT = Range(r, 1.0f, 1.5f);
            float phase = 0f;
            var crackHp = new OnePoleHP(1500f);
            var boomLp = Biquad.LowPass(500f);
            var sparkles = new List<(float t, float a)>();
            for (int k = 0; k < 60; k++) sparkles.Add((popT + 0.1f + Range(r, 0f, 1.4f), Range(r, 0.1f, 0.5f)));
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = 0f;
                if (t < popT)
                {
                    float f = Lerp(1100f, 3300f, t / popT);
                    phase += f / Rate;
                    v += (Sin(TwoPi * phase) * 0.25f + White(r) * 0.05f) * Clamp01(t / 0.2f);
                }
                float tp = t - popT;
                v += crackHp.Process(White(r)) * Env(tp, 0.0003f, 0.008f) * 1.5f + boomLp.Process(White(r)) * Env(tp, 0.002f, 0.15f) * 1.5f;
                buf[i] = v;
            }
            foreach (var s in sparkles)
            {
                int s0 = Samples(s.t), len = Samples(0.01f);
                for (int k = 0; k < len && s0 + k < buf.Length; k++) buf[s0 + k] += White(r) * s.a * (1f - (float)k / len);
            }
            ApplyReverb(buf, 0.85f, 0.4f, 0.5f);
            Normalize(buf, 0.75f);
            Fade(buf, 0f, 0.5f);
            return buf;
        }

        static float[] GlassBreak(Random r)
        {
            var buf = new float[Samples(2f)];
            var hp = new OnePoleHP(1000f);
            var shards = new List<(float t, float f, float d, float a)>();
            for (int k = 0; k < 70; k++)
            {
                float t = (float)Math.Pow(r.NextDouble(), 2.2) * 0.9f;
                shards.Add((t, Range(r, 2200f, 9000f), Range(r, 0.02f, 0.12f), Range(r, 0.05f, 0.3f) * (1f - t)));
            }
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = hp.Process(White(r)) * Env(t, 0.0005f, 0.035f) * 1.4f;
                foreach (var s in shards) if (t >= s.t && t < s.t + s.d * 6f) v += Sin(TwoPi * s.f * (t - s.t)) * Env(t - s.t, 0.0003f, s.d) * s.a;
                buf[i] = v;
            }
            ApplyReverb(buf, 0.7f, 0.3f, 0.35f);
            Normalize(buf, 0.75f);
            Fade(buf, 0f, 0.3f);
            return buf;
        }

        // ================================================================== story sounds

        /// <summary>The Purge siren: a dual-rotor air-raid siren — slow rise, long moaning hold, falling wail. City-wide reverb.</summary>
        static float[] PurgeSiren(Random r)
        {
            float rise = 7f, hold = 13f, fall = 9f, dur = rise + hold + fall;
            var buf = new float[Samples(dur + 3f)];
            float pa = 0f, pb = 0f;
            var body = Biquad.LowPass(2200f);
            var air = new OnePoleHP(200f);
            var wobble = new OnePoleLP(0.6f);
            for (int i = 0; i < Samples(dur); i++)
            {
                float t = (float)i / Rate;
                float k = t < rise ? (float)Math.Pow(t / rise, 0.7) : t < rise + hold ? 1f : 1f - (float)Math.Pow((t - rise - hold) / fall, 0.6);
                float w = 1f + wobble.Process(White(r)) * 0.4f + 0.008f * Sin(TwoPi * 0.3f * t);
                float f = (60f + 380f * k) * w;
                pa += f / Rate;
                pb += f * 1.19f / Rate; // second rotor: roughly a minor third above — the eerie interval
                float A = pa * TwoPi, B = pb * TwoPi;
                float tone = Sin(A) + 0.5f * Sin(2 * A) + 0.33f * Sin(3 * A) + 0.2f * Sin(4 * A)
                           + 0.8f * (Sin(B) + 0.45f * Sin(2 * B) + 0.25f * Sin(3 * B));
                float chop = 0.85f + 0.15f * Sin(TwoPi * f * 0.25f * t); // rotor port chopping
                buf[i] = (body.Process(tone * chop) + air.Process(White(r)) * 0.08f) * (0.15f + 0.85f * k);
            }
            ApplyReverb(buf, 0.97f, 0.35f, 0.9f, 0.75f);
            Normalize(buf, 0.9f);
            Fade(buf, 0.3f, 2.5f);
            return buf;
        }

        /// <summary>Emergency Broadcast attention signal (853 + 960 Hz), through a phone/radio band.</summary>
        static float[] EBSTone(Random r)
        {
            var buf = new float[Samples(8f)];
            var hp = Biquad.HighPass(300f);
            var lp = Biquad.LowPass(3400f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float v = Sin(TwoPi * 853f * t) + Sin(TwoPi * 960f * t);
                v = (float)Math.Tanh(v * 1.4f) + White(r) * 0.02f;
                buf[i] = lp.Process(hp.Process(v));
            }
            Normalize(buf, 0.6f);
            Fade(buf, 0.02f, 0.05f);
            return buf;
        }
    }
}
