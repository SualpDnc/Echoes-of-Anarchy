using System;
using System.Collections.Generic;
using static Echoes.Audio.DSP;

namespace Echoes.Audio
{
    /// <summary>
    /// Voices without words: each speaker has a bank of babble syllables (formant-filtered glottal pulses with
    /// consonant onsets) that the dialogue director strings together to the rhythm of the line. Meaning comes from
    /// the subtitles. Phone and radio voices have the channel baked in. The cloned "Mia" is the same voice with
    /// digital artefacts in some syllables — the first hint that the caller is not her.
    /// </summary>
    public static partial class SynthLibrary
    {
        public enum VoiceChannel { Direct, Phone, Radio, Muffled }

        struct VoiceSpec
        {
            public float f0, formantScale, breath, glitch;
            public VoiceChannel channel;
        }

        // Male formant frequencies (Hz) for a, e, i, o, u, schwa, æ.
        static readonly float[,] Vowels =
        {
            { 730f, 1090f, 2440f }, { 530f, 1840f, 2480f }, { 270f, 2290f, 3010f }, { 570f, 840f, 2410f },
            { 300f, 870f, 2240f }, { 500f, 1500f, 2500f }, { 660f, 1720f, 2410f }
        };

        static IEnumerable<Recipe> VoiceRecipes()
        {
            Recipe V(SoundId id, float f0, float scale, float breath, VoiceChannel ch, float glitch = 0f) => new Recipe
            {
                id = id, variations = 24,
                render = r => Syllable(r, new VoiceSpec { f0 = f0, formantScale = scale, breath = breath, channel = ch, glitch = glitch })
            };
            yield return V(SoundId.VoiceDaniel, 102f, 1.0f, 0.08f, VoiceChannel.Direct);
            yield return V(SoundId.VoiceLuis, 124f, 1.03f, 0.1f, VoiceChannel.Direct);
            yield return V(SoundId.VoiceMiaPhone, 255f, 1.28f, 0.12f, VoiceChannel.Phone);
            yield return V(SoundId.VoiceMiaClone, 255f, 1.28f, 0.05f, VoiceChannel.Phone, 0.4f); // too clean, then it breaks
            yield return V(SoundId.VoiceDispatch, 195f, 1.15f, 0.05f, VoiceChannel.Radio);
            yield return V(SoundId.VoiceMale, 116f, 1.0f, 0.1f, VoiceChannel.Direct);
            yield return V(SoundId.VoiceFemale, 205f, 1.16f, 0.12f, VoiceChannel.Direct);
            yield return V(SoundId.VoiceSmiler, 108f, 0.97f, 0.04f, VoiceChannel.Muffled);
            yield return V(SoundId.VoiceHale, 94f, 0.98f, 0.09f, VoiceChannel.Direct);
            yield return new Recipe { id = SoundId.PhoneRing, render = PhoneRing };
            yield return new Recipe { id = SoundId.PhonePickup, render = PhonePickup };
            yield return new Recipe { id = SoundId.PhoneHangup, render = PhoneHangup };
            yield return new Recipe { id = SoundId.VoicemailBeep, render = VoicemailBeep };
            yield return new Recipe { id = SoundId.RadioSquelch, variations = 4, render = RadioSquelch };
        }

        /// <summary>Rosenberg glottal pulse (open 40 %, closing 16 %), sampled at a phase in 0..1.</summary>
        static float Glottal(float ph)
        {
            const float open = 0.4f, close = 0.16f;
            if (ph < open) return 0.5f * (1f - (float)Math.Cos(Math.PI * ph / open));
            if (ph < open + close) return (float)Math.Cos(Math.PI * 0.5 * (ph - open) / close);
            return 0f;
        }

        /// <summary>One babble syllable: optional consonant onset, then a vowel that glides toward a second vowel.</summary>
        static float[] Syllable(Random r, VoiceSpec v)
        {
            float vowelLen = Range(r, 0.08f, 0.17f);
            int consonant = r.Next(6); // 0 none, 1 plosive, 2 "s", 3 "sh", 4 nasal, 5 breathy "h"
            float conLen = consonant == 0 ? 0f : consonant == 1 ? 0.028f : consonant == 4 ? 0.05f : 0.055f;
            float total = conLen + vowelLen + 0.03f;
            var buf = new float[Samples(total)];

            int va = r.Next(Vowels.GetLength(0)), vb = r.Next(Vowels.GetLength(0));
            float fs = v.formantScale * Range(r, 0.96f, 1.04f);
            var bp = new[] { new Biquad(), new Biquad(), new Biquad() };
            float[] gains = { 1f, 0.55f, 0.28f };
            float[] bws = { 90f, 120f, 170f };
            var nasalLp = Biquad.LowPass(320f);
            var fricS = Biquad.HighPass(4800f);
            var fricSh = Biquad.BandPass(2900f, 2f);
            var burst = Biquad.BandPass(Range(r, 1600f, 3600f), 1.2f);
            var tilt = new OnePoleLP(v.f0 > 200f ? 4200f : 3000f);

            float pitch0 = v.f0 * Range(r, 0.92f, 1.1f);
            float slope = Range(r, -0.14f, 0.05f);
            float phase = 0f, prevG = 0f, jitter = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float tv = t - conLen;
                if ((i & 63) == 0)
                {
                    float glide = Clamp01(tv / vowelLen) * 0.35f;
                    for (int k = 0; k < 3; k++)
                    {
                        float f = Lerp(Vowels[va, k], Vowels[vb, k], glide) * fs;
                        bp[k].SetBandPass(f, f / (bws[k] * fs));
                    }
                    jitter = Range(r, -0.01f, 0.01f);
                }

                // Glottal source; the vowel envelope gates it, a little of it leaks into nasals.
                float f0 = pitch0 * (1f + slope * Clamp01(tv / vowelLen)) * (1f + 0.01f * Sin(TwoPi * 5.3f * t) + jitter);
                phase += f0 / Rate;
                if (phase >= 1f) phase -= 1f;
                float g = Glottal(phase);
                float src = tilt.Process((g - prevG) * 8f); // radiated (differentiated) pulse with spectral tilt
                prevG = g;
                float vEnv = tv < 0f ? 0f : Math.Min(1f, tv / 0.014f) * Math.Min(1f, (vowelLen - tv) / 0.04f + 0.0001f);
                vEnv = Math.Max(0f, vEnv);
                float excite = src * vEnv + White(r) * v.breath * vEnv;
                if (consonant == 5 && tv < 0f) excite += White(r) * 0.25f * Math.Min(1f, t / 0.02f);

                float y = 0f;
                for (int k = 0; k < 3; k++) y += bp[k].Process(excite) * gains[k];

                // Consonant onsets.
                if (tv < 0.01f)
                {
                    float ce = Math.Min(1f, t / 0.01f) * Math.Min(1f, (conLen + 0.01f - t) / 0.015f);
                    ce = Math.Max(0f, ce);
                    switch (consonant)
                    {
                        case 1: y += burst.Process(White(r)) * Env(t - 0.016f, 0.001f, 0.007f) * 0.9f; break;
                        case 2: y += fricS.Process(White(r)) * ce * 0.3f; break;
                        case 3: y += fricSh.Process(White(r)) * ce * 0.35f; break;
                        case 4: y += nasalLp.Process(src) * ce * 2.2f; break;
                    }
                }
                buf[i] = y;
            }

            if (v.glitch > 0f && r.NextDouble() < v.glitch) Glitch(r, buf, conLen);
            Normalize(buf, 0.8f);
            Channel(r, buf, v.channel);
            Level(buf, 0.15f, 0.85f);
            Fade(buf, 0.003f, 0.006f);
            return buf;
        }

        /// <summary>Even loudness across syllables: scale to a target RMS without exceeding a peak.</summary>
        static void Level(float[] buf, float rms, float peak)
        {
            double sq = 0;
            float p = 0f;
            foreach (float x in buf) { sq += x * x; p = Math.Max(p, Math.Abs(x)); }
            float cur = (float)Math.Sqrt(sq / Math.Max(1, buf.Length));
            if (cur < 1e-6f || p < 1e-6f) return;
            float g = Math.Min(rms / cur, peak / p);
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }

        /// <summary>Digital artefacts for the cloned voice: a stutter, a dropout, a sample-and-hold crunch or a robotic ring.</summary>
        static void Glitch(Random r, float[] buf, float conLen)
        {
            int start = Samples(conLen + Range(r, 0.01f, 0.05f));
            int len = Math.Min(Samples(Range(r, 0.02f, 0.045f)), buf.Length - start - 1);
            if (len <= 0) return;
            switch (r.Next(4))
            {
                case 0: // stutter: the first slice repeats
                    for (int i = start + len; i < Math.Min(buf.Length, start + len * 2); i++) buf[i] = buf[i - len];
                    break;
                case 1: // dropout to digital silence
                    for (int i = start; i < start + len; i++) buf[i] = 0f;
                    break;
                case 2: // sample-and-hold crunch
                {
                    int hold = r.Next(6, 12);
                    for (int i = start; i < start + len * 2 && i < buf.Length; i++) buf[i] = buf[start + ((i - start) / hold) * hold];
                    break;
                }
                default: // ring modulation: a vocoder edge
                    for (int i = start; i < start + len * 2 && i < buf.Length; i++) buf[i] *= Sin(TwoPi * 1150f * i / Rate) * 1.3f;
                    break;
            }
        }

        static void Channel(Random r, float[] buf, VoiceChannel ch)
        {
            if (ch == VoiceChannel.Direct) return;
            float lo = ch == VoiceChannel.Phone ? 320f : ch == VoiceChannel.Radio ? 450f : 90f;
            float hi = ch == VoiceChannel.Phone ? 3300f : ch == VoiceChannel.Radio ? 2800f : 2100f;
            float drive = ch == VoiceChannel.Phone ? 1.6f : ch == VoiceChannel.Radio ? 2.6f : 1.1f;
            float floor = ch == VoiceChannel.Phone ? 0.008f : ch == VoiceChannel.Radio ? 0.03f : 0f;
            var hp1 = Biquad.HighPass(lo); var hp2 = Biquad.HighPass(lo);
            var lp1 = Biquad.LowPass(hi); var lp2 = Biquad.LowPass(hi);
            var body = Biquad.Peak(ch == VoiceChannel.Muffled ? 700f : 1800f, 1.2f, ch == VoiceChannel.Muffled ? 5f : 3f);
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i] + White(r) * floor;
                x = lp2.Process(lp1.Process(hp2.Process(hp1.Process(x))));
                buf[i] = (float)Math.Tanh(body.Process(x) * drive) * 0.8f;
            }
        }

        static float[] PhoneRing(Random r)
        {
            // Phone in a vest pocket: a bright two-tone trill, twice, muffled by fabric. 2.2 s per ring cycle.
            var buf = new float[Samples(2.2f)];
            var lp = Biquad.LowPass(2600f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                bool on = t < 0.45f || (t > 0.6f && t < 1.05f);
                if (!on) { buf[i] = lp.Process(0f); continue; }
                float f = (int)(t * 22f) % 2 == 0 ? 1040f : 1310f;
                float tone = Sin(TwoPi * f * t) * 0.6f + Sin(TwoPi * f * 2f * t) * 0.15f;
                buf[i] = lp.Process(tone) * 0.5f;
            }
            Fade(buf, 0.002f, 0.05f);
            return buf;
        }

        static float[] PhonePickup(Random r)
        {
            var buf = new float[Samples(0.25f)];
            var bp = Biquad.BandPass(2200f, 1.5f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = bp.Process(White(r)) * Env(t, 0.0005f, 0.012f) * 0.9f + White(r) * 0.02f * Env(t - 0.03f, 0.01f, 0.08f);
            }
            return buf;
        }

        static float[] PhoneHangup(Random r)
        {
            // Call ended: three short descending beeps through the earpiece.
            var buf = new float[Samples(0.75f)];
            float[] freqs = { 620f, 480f, 380f };
            for (int b = 0; b < 3; b++)
            {
                int s0 = Samples(b * 0.22f), n = Samples(0.13f);
                for (int i = 0; i < n && s0 + i < buf.Length; i++)
                {
                    float t = (float)i / Rate;
                    float e = Math.Min(1f, t / 0.005f) * Math.Min(1f, (0.13f - t) / 0.01f);
                    buf[s0 + i] = Sin(TwoPi * freqs[b] * t) * e * 0.45f;
                }
            }
            Channel(r, buf, VoiceChannel.Phone);
            return buf;
        }

        static float[] VoicemailBeep(Random r)
        {
            var buf = new float[Samples(0.4f)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                buf[i] = Sin(TwoPi * 1000f * t) * Math.Min(1f, t / 0.005f) * Math.Min(1f, (0.36f - t) / 0.01f) * 0.5f;
                if (t > 0.36f) buf[i] = 0f;
            }
            Channel(r, buf, VoiceChannel.Phone);
            return buf;
        }

        static float[] RadioSquelch(Random r)
        {
            // Key-up click, a burst of carrier hiss and the squelch tail closing.
            var buf = new float[Samples(0.22f)];
            var bp = Biquad.BandPass(Range(r, 1500f, 2200f), 0.6f);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / Rate;
                float click = Sin(TwoPi * 2400f * t) * Env(t, 0.0003f, 0.004f) * 0.6f;
                float hiss = bp.Process(White(r)) * Env(t - 0.01f, 0.005f, 0.07f) * 0.7f;
                buf[i] = click + hiss;
            }
            Channel(r, buf, VoiceChannel.Radio);
            return buf;
        }
    }
}
