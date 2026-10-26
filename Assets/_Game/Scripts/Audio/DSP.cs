using System;

namespace Echoes.Audio
{
    /// <summary>
    /// Minimal sample-level DSP toolkit for offline synthesis. Thread-safe as long as each thread uses
    /// its own filter instances and Random. Uses System.Math only (no UnityEngine calls) so recipes can run in parallel.
    /// </summary>
    public static class DSP
    {
        public const int Rate = 44100;
        public const float TwoPi = (float)(Math.PI * 2.0);

        public static int Samples(float seconds) => (int)(seconds * Rate);
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float Exp(float v) => (float)Math.Exp(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Rand(Random r) => (float)r.NextDouble();
        public static float Range(Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        public static float White(Random r) => (float)r.NextDouble() * 2f - 1f;

        /// <summary>Exponential decay envelope with a linear attack.</summary>
        public static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return Exp(-(t - attack) / decay);
        }

        /// <summary>Paul Kellet's economy pink noise.</summary>
        public class Pink
        {
            float b0, b1, b2;
            public float Next(Random r)
            {
                float w = White(r);
                b0 = 0.99765f * b0 + w * 0.0990460f;
                b1 = 0.96300f * b1 + w * 0.2965164f;
                b2 = 0.57000f * b2 + w * 1.0526913f;
                return (b0 + b1 + b2 + w * 0.1848f) * 0.2f;
            }
        }

        /// <summary>Leaky-integrated white noise (−6 dB/oct).</summary>
        public class Brown
        {
            float last;
            public float Next(Random r)
            {
                last = (last + 0.02f * White(r)) / 1.02f;
                return last * 3.5f;
            }
        }

        public class OnePoleLP
        {
            float a, z;
            public OnePoleLP(float cutoff) { Set(cutoff); }
            public void Set(float cutoff) => a = 1f - Exp(-TwoPi * cutoff / Rate);
            public float Process(float x) { z += a * (x - z); return z; }
        }

        public class OnePoleHP
        {
            readonly OnePoleLP lp;
            public OnePoleHP(float cutoff) { lp = new OnePoleLP(cutoff); }
            public float Process(float x) => x - lp.Process(x);
        }

        /// <summary>RBJ biquad. Recalculate with Set* for sweeps (cheap enough per sample for offline use).</summary>
        public class Biquad
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;

            public static Biquad BandPass(float f, float q) { var b = new Biquad(); b.SetBandPass(f, q); return b; }
            public static Biquad LowPass(float f, float q = 0.707f) { var b = new Biquad(); b.SetLowPass(f, q); return b; }
            public static Biquad HighPass(float f, float q = 0.707f) { var b = new Biquad(); b.SetHighPass(f, q); return b; }
            public static Biquad Peak(float f, float q, float gainDb) { var b = new Biquad(); b.SetPeak(f, q, gainDb); return b; }

            void Coefs(float w, float q, out float cos, out float alpha)
            {
                w = Math.Min(w, Rate * 0.49f);
                float om = TwoPi * w / Rate;
                cos = (float)Math.Cos(om);
                alpha = (float)Math.Sin(om) / (2f * q);
            }

            public void SetBandPass(float f, float q)
            {
                Coefs(f, q, out float c, out float al);
                float a0 = 1f + al;
                b0 = al / a0; b1 = 0f; b2 = -al / a0; a1 = -2f * c / a0; a2 = (1f - al) / a0;
            }

            public void SetLowPass(float f, float q)
            {
                Coefs(f, q, out float c, out float al);
                float a0 = 1f + al;
                b0 = (1f - c) / 2f / a0; b1 = (1f - c) / a0; b2 = b0; a1 = -2f * c / a0; a2 = (1f - al) / a0;
            }

            public void SetHighPass(float f, float q)
            {
                Coefs(f, q, out float c, out float al);
                float a0 = 1f + al;
                b0 = (1f + c) / 2f / a0; b1 = -(1f + c) / a0; b2 = b0; a1 = -2f * c / a0; a2 = (1f - al) / a0;
            }

            public void SetPeak(float f, float q, float gainDb)
            {
                Coefs(f, q, out float c, out float al);
                float A = (float)Math.Pow(10.0, gainDb / 40.0);
                float a0 = 1f + al / A;
                b0 = (1f + al * A) / a0; b1 = -2f * c / a0; b2 = (1f - al * A) / a0; a1 = b1; a2 = (1f - al / A) / a0;
            }

            public float Process(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }

        /// <summary>Schroeder/Freeverb-style mono reverb for baking street/alley tails into distant sounds.</summary>
        public class Reverb
        {
            readonly float[][] comb;
            readonly int[] combIdx;
            readonly float[] combLp;
            readonly float[][] ap;
            readonly int[] apIdx;
            readonly float feedback, damp;

            public Reverb(float roomSize, float damping)
            {
                int[] combLens = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
                int[] apLens = { 556, 441, 341, 225 };
                float scale = 0.6f + roomSize * 1.2f;
                comb = new float[combLens.Length][];
                for (int i = 0; i < combLens.Length; i++) comb[i] = new float[(int)(combLens[i] * scale)];
                combIdx = new int[combLens.Length];
                combLp = new float[combLens.Length];
                ap = new float[apLens.Length][];
                for (int i = 0; i < apLens.Length; i++) ap[i] = new float[apLens[i]];
                apIdx = new int[apLens.Length];
                feedback = 0.7f + roomSize * 0.27f;
                damp = damping;
            }

            public float Process(float x)
            {
                float input = x * 0.015f;
                float sum = 0f;
                for (int i = 0; i < comb.Length; i++)
                {
                    float[] buf = comb[i];
                    float o = buf[combIdx[i]];
                    combLp[i] = o * (1f - damp) + combLp[i] * damp;
                    buf[combIdx[i]] = input + combLp[i] * feedback;
                    if (++combIdx[i] >= buf.Length) combIdx[i] = 0;
                    sum += o;
                }
                for (int i = 0; i < ap.Length; i++)
                {
                    float[] buf = ap[i];
                    float b = buf[apIdx[i]];
                    buf[apIdx[i]] = sum + b * 0.5f;
                    sum = b - sum;
                    if (++apIdx[i] >= buf.Length) apIdx[i] = 0;
                }
                return sum;
            }
        }

        // ------------------------------------------------------------------ buffer helpers

        public static void Normalize(float[] buf, float peak = 0.9f)
        {
            float m = 0f;
            for (int i = 0; i < buf.Length; i++) m = Math.Max(m, Math.Abs(buf[i]));
            if (m < 1e-6f) return;
            float g = peak / m;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }

        /// <summary>Fade both ends to avoid clicks.</summary>
        public static void Fade(float[] buf, float inSec, float outSec, int channels = 1)
        {
            int fi = Samples(inSec) * channels, fo = Samples(outSec) * channels;
            for (int i = 0; i < fi && i < buf.Length; i++) buf[i] *= (float)i / fi;
            for (int i = 0; i < fo && i < buf.Length; i++) buf[buf.Length - 1 - i] *= (float)i / fo;
        }

        /// <summary>Makes a loop seamless by cross-fading its tail into its head. Input must be longer than the output by `xfade`.</summary>
        public static float[] MakeLoop(float[] src, float xfadeSec, int channels = 1)
        {
            int xf = Samples(xfadeSec) * channels;
            int outLen = src.Length - xf;
            var o = new float[outLen];
            Array.Copy(src, o, outLen);
            for (int i = 0; i < xf; i++)
            {
                float t = (float)i / xf;
                o[i] = o[i] * t + src[outLen + i] * (1f - t);
            }
            return o;
        }

        /// <summary>Applies reverb in place, mixing wet/dry. Buffer should include room for the tail.</summary>
        public static void ApplyReverb(float[] buf, float room, float damp, float wet, float dry = 1f)
        {
            var rv = new Reverb(room, damp);
            for (int i = 0; i < buf.Length; i++) buf[i] = buf[i] * dry + rv.Process(buf[i]) * wet;
        }

        /// <summary>Mixes src into dst at sample offset with gain.</summary>
        public static void Mix(float[] dst, float[] src, int offset, float gain)
        {
            for (int i = 0; i < src.Length && i + offset < dst.Length; i++)
                if (i + offset >= 0) dst[i + offset] += src[i] * gain;
        }
    }
}
