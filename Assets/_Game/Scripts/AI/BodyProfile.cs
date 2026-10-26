using System.Collections.Generic;
using UnityEngine;

namespace Echoes.AI
{
    /// <summary>
    /// A body segment described as horizontal cross-sections keyed by height (or by distance along the tube axis).
    /// Each section is a superellipse with separate front and back depth, so a chest, a calf or a heel can bulge on
    /// one side only. Sections are interpolated with Catmull-Rom, so a handful of keys give a smooth surface.
    /// </summary>
    public class BodyProfile
    {
        public struct Key
        {
            public float y, rx, rzF, rzB, sq, cx, cz;
            public Key(float y, float rx, float rzF, float rzB, float sq = 2f, float cx = 0f, float cz = 0f)
            {
                this.y = y; this.rx = rx; this.rzF = rzF; this.rzB = rzB; this.sq = sq; this.cx = cx; this.cz = cz;
            }
        }

        /// <summary>A garment band: covers [y0, y1] at a thickness that runs from thick0 to thick1, with optional rib edges.</summary>
        public struct Layer
        {
            public float y0, y1, thick0, thick1, rib0, rib1;
            public Material mat;
            public Layer(Material mat, float y0, float y1, float thick0, float thick1, float rib0 = 0f, float rib1 = 0f)
            {
                this.mat = mat; this.y0 = y0; this.y1 = y1; this.thick0 = thick0; this.thick1 = thick1; this.rib0 = rib0; this.rib1 = rib1;
            }
        }

        public const float RibThickness = 0.004f;

        readonly List<Key> keys = new List<Key>();
        public readonly List<Layer> layers = new List<Layer>();
        public Material baseMat;

        public float MinY => keys[0].y;
        public float MaxY => keys[keys.Count - 1].y;

        public BodyProfile Add(Key k) { keys.Add(k); return this; }
        public BodyProfile Add(float y, float rx, float rzF, float rzB, float sq = 2f, float cx = 0f, float cz = 0f) => Add(new Key(y, rx, rzF, rzB, sq, cx, cz));

        public void Sort() => keys.Sort((a, b) => a.y.CompareTo(b.y));

        /// <summary>Scales every radius (body bulk) and optionally a depth range (belly, bust).</summary>
        public void Inflate(float factor)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                k.rx *= factor; k.rzF *= factor; k.rzB *= factor;
                keys[i] = k;
            }
        }

        /// <summary>Adds a smooth bump to the front depth around height y (belly, bust, calf...).</summary>
        public void Bulge(float y, float width, float front, float back = 0f, float side = 0f)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                float w = Mathf.Exp(-(k.y - y) * (k.y - y) / (width * width));
                k.rzF += front * w; k.rzB += back * w; k.rx += side * w;
                keys[i] = k;
            }
        }

        public Key Sample(float y)
        {
            int n = keys.Count;
            if (y <= keys[0].y) return keys[0];
            if (y >= keys[n - 1].y) return keys[n - 1];
            int i = 0;
            while (i < n - 2 && keys[i + 1].y < y) i++;
            Key k0 = keys[Mathf.Max(0, i - 1)], k1 = keys[i], k2 = keys[i + 1], k3 = keys[Mathf.Min(n - 1, i + 2)];
            float t = (y - k1.y) / Mathf.Max(1e-5f, k2.y - k1.y);
            return new Key(y,
                Mathf.Max(0.002f, CR(k0.rx, k1.rx, k2.rx, k3.rx, t)),
                Mathf.Max(0.002f, CR(k0.rzF, k1.rzF, k2.rzF, k3.rzF, t)),
                Mathf.Max(0.002f, CR(k0.rzB, k1.rzB, k2.rzB, k3.rzB, t)),
                Mathf.Lerp(k1.sq, k2.sq, t),
                CR(k0.cx, k1.cx, k2.cx, k3.cx, t),
                CR(k0.cz, k1.cz, k2.cz, k3.cz, t));
        }

        static float CR(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Surface point at height y and angle theta (0 = +X, π/2 = front +Z), pushed out by offset metres.</summary>
        public Vector3 Point(float y, float theta, float offset)
        {
            var k = Sample(y);
            return Point(k, theta, offset);
        }

        public static Vector3 Point(Key k, float theta, float offset)
        {
            float cs = Mathf.Cos(theta), sn = Mathf.Sin(theta);
            float e = 2f / k.sq;
            float ex = Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), e);
            float ez = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), e);
            float rz = sn >= 0f ? k.rzF : k.rzB;
            return new Vector3(k.cx + (k.rx + offset) * ex, k.y, k.cz + (rz + offset) * ez);
        }

        /// <summary>Outermost garment at height y: its material and thickness (base material and 0 when bare).</summary>
        public void Cover(float y, out Material mat, out float thickness)
        {
            mat = baseMat;
            thickness = 0f;
            for (int i = 0; i < layers.Count; i++)
            {
                var l = layers[i];
                if (y < l.y0 || y > l.y1) continue;
                mat = l.mat;
                thickness = Mathf.Lerp(l.thick0, l.thick1, Mathf.InverseLerp(l.y0, l.y1, y));
                if ((l.rib0 > 0f && y <= l.y0 + l.rib0) || (l.rib1 > 0f && y >= l.y1 - l.rib1)) thickness += RibThickness;
            }
        }

        /// <summary>Heights where the cover changes (garment edges and rib edges), for crisp hems.</summary>
        public IEnumerable<float> Edges()
        {
            foreach (var l in layers)
            {
                yield return l.y0;
                yield return l.y1;
                if (l.rib0 > 0f) yield return l.y0 + l.rib0;
                if (l.rib1 > 0f) yield return l.y1 - l.rib1;
            }
        }
    }
}
