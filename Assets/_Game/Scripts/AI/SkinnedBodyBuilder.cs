using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.AI
{
    /// <summary>Bone influence of a generated vertex: (1 − t) on bone a, t on bone b.</summary>
    public struct Skin
    {
        public Bone a, b;
        public float t;

        public static Skin On(Bone bone) => new Skin { a = bone, b = bone, t = 0f };
        public static Skin Blend(Bone a, Bone b, float t) => new Skin { a = a, b = b, t = Mathf.Clamp01(t) };

        /// <summary>Blend from bone a (above the joint band) to bone b (below it) around height y.</summary>
        public static Skin Joint(Bone upper, Bone lower, float y, float jointY, float band) =>
            Blend(upper, lower, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jointY + band, jointY - band, y)));

        public BoneWeight ToWeight()
        {
            var w = new BoneWeight();
            if (a == b || t <= 0.001f) { w.boneIndex0 = (int)a; w.weight0 = 1f; return w; }
            if (t >= 0.999f) { w.boneIndex0 = (int)b; w.weight0 = 1f; return w; }
            bool aFirst = t <= 0.5f;
            w.boneIndex0 = (int)(aFirst ? a : b); w.weight0 = aFirst ? 1f - t : t;
            w.boneIndex1 = (int)(aFirst ? b : a); w.weight1 = aFirst ? t : 1f - t;
            return w;
        }
    }

    /// <summary>
    /// Accumulates one skinned character mesh (per-material submeshes) in the rig's rest space: lofted body tubes with
    /// garment layers, conforming patches (pockets, lapels), sculpted ellipsoids (heads, masks), tori and boxes.
    /// Winding is fixed per triangle against an outward hint, so callers never have to reason about it.
    /// UVs are in metres, like MeshBuilder.
    /// </summary>
    public class SkinnedBodyBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<BoneWeight> weights = new List<BoneWeight>();
        readonly List<List<int>> subTris = new List<List<int>>();
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Material, int> matIndex = new Dictionary<Material, int>();

        /// <summary>Applied to everything added (lets a tube built along Y run along Z, e.g. a shoe).</summary>
        public Matrix4x4 matrix = Matrix4x4.identity;

        public int VertexCount => verts.Count;

        List<int> Sub(Material m)
        {
            if (!matIndex.TryGetValue(m, out int idx))
            {
                idx = materials.Count;
                materials.Add(m);
                subTris.Add(new List<int>());
                matIndex[m] = idx;
            }
            return subTris[idx];
        }

        int V(Vector3 p, Vector3 n, Vector2 uv, Skin s)
        {
            verts.Add(p);
            normals.Add(n);
            uvs.Add(uv);
            weights.Add(s.ToWeight());
            return verts.Count - 1;
        }

        void Tri(Material m, int a, int b, int c, Vector3 outward)
        {
            Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (n.sqrMagnitude < 1e-14f) return;
            var t = Sub(m);
            t.Add(a);
            if (Vector3.Dot(n, outward) >= 0f) { t.Add(b); t.Add(c); }
            else { t.Add(c); t.Add(b); }
        }

        void Quad(Material m, int a, int b, int c, int d, Vector3 outward)
        {
            Tri(m, a, b, c, outward);
            Tri(m, a, c, d, outward);
        }

        // ------------------------------------------------------------------ tubes

        struct Row { public float y, offset; public Material mat; public BodyProfile.Key key; }

        /// <summary>
        /// Lofts a profile along its axis (local Y) with garment layers: each band takes the outermost layer's
        /// material and thickness, and every garment edge gets a double ring so hems and cuffs read as real edges.
        /// </summary>
        public void Tube(BodyProfile prof, int segments, float step, Func<Vector3, Skin> skin, bool capLow = true, bool capHigh = true)
        {
            prof.Sort();
            float y0 = prof.MinY, y1 = prof.MaxY;
            var ys = new List<float>();
            for (float y = y0; y < y1 - step * 0.3f; y += step) ys.Add(y);
            ys.Add(y1);
            foreach (float e in prof.Edges()) if (e > y0 + 1e-3f && e < y1 - 1e-3f) ys.Add(e);
            ys.Sort();

            var rows = new List<Row>();
            for (int i = 0; i < ys.Count; i++)
            {
                float y = ys[i];
                if (i > 0 && y - ys[i - 1] < 1e-4f) continue;
                prof.Cover(y - 1e-4f, out Material mb, out float tb);
                prof.Cover(y + 1e-4f, out Material ma, out float ta);
                var key = prof.Sample(y);
                bool edge = i > 0 && i < ys.Count - 1 && (Mathf.Abs(ta - tb) > 1e-4f || ma != mb);
                if (edge)
                {
                    rows.Add(new Row { y = y, offset = tb, mat = ta > tb ? ma : mb, key = key });
                    rows.Add(new Row { y = y, offset = ta, mat = ma, key = key });
                }
                else rows.Add(new Row { y = y, offset = i == ys.Count - 1 ? tb : ta, mat = ma, key = key });
            }

            int cols = segments + 1;
            var idx = new int[rows.Count, cols];
            var pos = new Vector3[rows.Count, segments];
            for (int i = 0; i < rows.Count; i++)
                for (int j = 0; j < segments; j++)
                    pos[i, j] = matrix.MultiplyPoint3x4(BodyProfile.Point(rows[i].key, j / (float)segments * Mathf.PI * 2f, rows[i].offset));

            for (int i = 0; i < rows.Count; i++)
            {
                int prev = i - 1, next = i + 1;
                while (prev >= 0 && Mathf.Abs(rows[prev].y - rows[i].y) < 1e-4f) prev--;
                while (next < rows.Count && Mathf.Abs(rows[next].y - rows[i].y) < 1e-4f) next++;
                prev = Mathf.Max(prev, 0);
                next = Mathf.Min(next, rows.Count - 1);
                var k = rows[i].key;
                Vector3 center = matrix.MultiplyPoint3x4(new Vector3(k.cx, k.y, k.cz));
                float rz = (k.rzF + k.rzB) * 0.5f + rows[i].offset, rx = k.rx + rows[i].offset;
                float perimeter = Mathf.PI * 2f * Mathf.Sqrt((rx * rx + rz * rz) * 0.5f);
                for (int j = 0; j < cols; j++)
                {
                    int jj = j % segments;
                    Vector3 p = pos[i, jj];
                    Vector3 along = pos[next, jj] - pos[prev, jj];
                    Vector3 around = pos[i, (jj + 1) % segments] - pos[i, (jj + segments - 1) % segments];
                    Vector3 radial = p - center;
                    Vector3 n = Vector3.Cross(along, around);
                    if (n.sqrMagnitude < 1e-12f) n = radial;
                    n.Normalize();
                    if (Vector3.Dot(n, radial) < 0f) n = -n;
                    idx[i, j] = V(p, n, new Vector2(j / (float)segments * perimeter, rows[i].y), skin(p));
                }
            }

            for (int i = 0; i < rows.Count - 1; i++)
            {
                if (Mathf.Abs(rows[i].y - rows[i + 1].y) < 1e-4f && Mathf.Abs(rows[i].offset - rows[i + 1].offset) < 1e-5f) continue;
                for (int j = 0; j < segments; j++)
                {
                    Vector3 outward = normals[idx[i, j]] + normals[idx[i + 1, j + 1]];
                    Quad(rows[i].mat, idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j], outward);
                }
            }

            Vector3 axis = matrix.MultiplyVector(Vector3.up).normalized;
            if (capLow) Cap(rows[0], rows[0].mat, pos, 0, segments, -axis, skin);
            if (capHigh) Cap(rows[rows.Count - 1], rows[rows.Count - 2].mat, pos, rows.Count - 1, segments, axis, skin);
        }

        void Cap(Row row, Material mat, Vector3[,] pos, int i, int segments, Vector3 n, Func<Vector3, Skin> skin)
        {
            Vector3 c = matrix.MultiplyPoint3x4(new Vector3(row.key.cx, row.key.y, row.key.cz));
            int ci = V(c, n, Vector2.zero, skin(c));
            int first = verts.Count;
            for (int j = 0; j < segments; j++)
            {
                Vector3 p = pos[i, j];
                V(p, n, new Vector2(p.x, p.z), skin(p));
            }
            for (int j = 0; j < segments; j++) Tri(mat, ci, first + j, first + (j + 1) % segments, n);
        }

        // ------------------------------------------------------------------ patches

        /// <summary>
        /// A panel that hugs a profile: pockets, lapels, a shirt front, a tie. span(y) gives the angle range
        /// (radians, π/2 = front) at each height, so V-shapes and tapers are easy. Thin side walls close the edges.
        /// </summary>
        public void Patch(BodyProfile prof, float y0, float y1, Func<float, Vector2> span, float extra, Material mat, Func<Vector3, Skin> skin,
                          int nu = 6, int nv = 6)
        {
            var outer = new int[nv + 1, nu + 1];
            var basePos = new Vector3[nv + 1, nu + 1];
            var p = new Vector3[nv + 1, nu + 1];
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i <= nv; i++)
            {
                float y = Mathf.Lerp(y0, y1, i / (float)nv);
                var key = prof.Sample(y);
                prof.Cover(y, out _, out float t);
                Vector2 s = span(y);
                for (int j = 0; j <= nu; j++)
                {
                    float th = Mathf.Lerp(s.x, s.y, j / (float)nu);
                    p[i, j] = matrix.MultiplyPoint3x4(BodyProfile.Point(key, th, t + extra));
                    basePos[i, j] = matrix.MultiplyPoint3x4(BodyProfile.Point(key, th, t + 0.0005f));
                    centroid += p[i, j];
                }
            }
            centroid /= (nv + 1) * (nu + 1);
            for (int i = 0; i <= nv; i++)
            {
                var key = prof.Sample(Mathf.Lerp(y0, y1, i / (float)nv));
                Vector3 center = matrix.MultiplyPoint3x4(new Vector3(key.cx, key.y, key.cz));
                for (int j = 0; j <= nu; j++)
                {
                    Vector3 along = p[Mathf.Min(i + 1, nv), j] - p[Mathf.Max(i - 1, 0), j];
                    Vector3 around = p[i, Mathf.Min(j + 1, nu)] - p[i, Mathf.Max(j - 1, 0)];
                    Vector3 radial = p[i, j] - center;
                    Vector3 n = Vector3.Cross(along, around);
                    if (n.sqrMagnitude < 1e-12f) n = radial;
                    n.Normalize();
                    if (Vector3.Dot(n, radial) < 0f) n = -n;
                    outer[i, j] = V(p[i, j], n, new Vector2(p[i, j].x + p[i, j].z, p[i, j].y), skin(p[i, j]));
                }
            }
            for (int i = 0; i < nv; i++)
                for (int j = 0; j < nu; j++)
                    Quad(mat, outer[i, j], outer[i, j + 1], outer[i + 1, j + 1], outer[i + 1, j], normals[outer[i, j]] + normals[outer[i + 1, j + 1]]);

            // Side walls around the boundary.
            var loop = new List<Vector2Int>();
            for (int j = 0; j <= nu; j++) loop.Add(new Vector2Int(0, j));
            for (int i = 1; i <= nv; i++) loop.Add(new Vector2Int(i, nu));
            for (int j = nu - 1; j >= 0; j--) loop.Add(new Vector2Int(nv, j));
            for (int i = nv - 1; i >= 1; i--) loop.Add(new Vector2Int(i, 0));
            for (int k = 0; k < loop.Count; k++)
            {
                var a = loop[k];
                var b = loop[(k + 1) % loop.Count];
                Vector3 oa = p[a.x, a.y], ob = p[b.x, b.y], ia = basePos[a.x, a.y], ib = basePos[b.x, b.y];
                Vector3 hint = (oa + ob) * 0.5f - centroid;
                Vector3 n = Vector3.Cross(ob - oa, ia - oa).normalized;
                if (Vector3.Dot(n, hint) < 0f) n = -n;
                int v0 = V(ia, n, new Vector2(0f, ia.y), skin(ia)), v1 = V(ib, n, new Vector2(0.01f, ib.y), skin(ib));
                int v2 = V(ob, n, new Vector2(0.01f, ob.y), skin(ob)), v3 = V(oa, n, new Vector2(0f, oa.y), skin(oa));
                Quad(mat, v0, v1, v2, v3, hint);
            }
        }

        // ------------------------------------------------------------------ ellipsoids

        /// <summary>
        /// Lat-long ellipsoid. dir is the unit direction (+Z front, +Y up) before scaling by radii; deform adds a
        /// local-space offset per direction (sculpting) and keep drops cells (openings in hoods and masks).
        /// Normals are averaged across the UV seam and at the poles so no crease shows.
        /// </summary>
        public void Ellipsoid(Material mat, Vector3 center, Vector3 radii, Quaternion rot, Func<Vector3, Skin> skin, int lat = 12, int lon = 18,
                              Func<Vector3, Vector3> deform = null, Func<Vector3, bool> keep = null)
        {
            int cols = lon + 1;
            var dirs = new Vector3[lat + 1, cols];
            var pts = new Vector3[lat + 1, cols];
            var idx = new int[lat + 1, cols];
            float rAvg = (radii.x + radii.y + radii.z) / 3f;
            Vector3 wc = matrix.MultiplyPoint3x4(center);
            for (int i = 0; i <= lat; i++)
            {
                float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / (float)lat);
                for (int j = 0; j < cols; j++)
                {
                    float lam = j / (float)lon * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(phi) * Mathf.Sin(lam), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Cos(lam));
                    dirs[i, j] = d;
                    Vector3 local = Vector3.Scale(d, radii) + (deform != null ? deform(d) : Vector3.zero);
                    pts[i, j] = matrix.MultiplyPoint3x4(center + rot * local);
                    idx[i, j] = V(pts[i, j], Vector3.zero, new Vector2(lam * rAvg, phi * rAvg), skin(pts[i, j]));
                }
            }
            // Shared normal slots: seam column folds onto column 0, each pole row is one slot.
            var acc = new Vector3[(lat + 1) * lon];
            int Slot(int i, int j) => i == 0 ? 0 : i == lat ? lat * lon : i * lon + (j % lon);
            int firstTri = Sub(mat).Count;
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    Vector3 dc = (dirs[i, j] + dirs[i + 1, j + 1]).normalized;
                    if (keep != null && !keep(dc)) continue;
                    Vector3 hint = (pts[i, j] + pts[i + 1, j + 1]) * 0.5f - wc;
                    Quad(mat, idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j], hint);
                }
            var tris = Sub(mat);
            int baseIdx = idx[0, 0];
            for (int t = firstTri; t < tris.Count; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                Vector3 fn = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                foreach (int v in new[] { a, b, c })
                {
                    int local = v - baseIdx;
                    acc[Slot(local / cols, local % cols)] += fn;
                }
            }
            for (int i = 0; i <= lat; i++)
                for (int j = 0; j < cols; j++)
                {
                    Vector3 n = acc[Slot(i, j)];
                    normals[idx[i, j]] = n.sqrMagnitude > 1e-14f ? n.normalized : (pts[i, j] - wc).normalized;
                }
        }

        // ------------------------------------------------------------------ small parts

        /// <summary>Torus around its local Y axis (halo, rings).</summary>
        public void Torus(Material mat, Vector3 center, Quaternion rot, float major, float minor, Skin skin, int segMajor = 28, int segMinor = 8)
        {
            var idx = new int[segMajor + 1, segMinor + 1];
            for (int i = 0; i <= segMajor; i++)
            {
                float a = i / (float)segMajor * Mathf.PI * 2f;
                var ring = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int j = 0; j <= segMinor; j++)
                {
                    float b = j / (float)segMinor * Mathf.PI * 2f;
                    Vector3 n = ring * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    Vector3 p = matrix.MultiplyPoint3x4(center + rot * (ring * major + n * minor));
                    idx[i, j] = V(p, matrix.MultiplyVector(rot * n).normalized, new Vector2(a * major, b * minor), skin);
                }
            }
            for (int i = 0; i < segMajor; i++)
                for (int j = 0; j < segMinor; j++)
                    Quad(mat, idx[i, j], idx[i + 1, j], idx[i + 1, j + 1], idx[i, j + 1], normals[idx[i, j]] + normals[idx[i + 1, j + 1]]);
        }

        /// <summary>Box with flat faces; m maps the unit-centred box into rest space.</summary>
        public void Box(Material mat, Vector3 center, Vector3 size, Quaternion rot, Skin skin)
        {
            var m = matrix * Matrix4x4.TRS(center, rot, size);
            Vector3 c = m.MultiplyPoint3x4(Vector3.zero);
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var ax in axes)
            {
                Vector3 u = Mathf.Abs(ax.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 w = Vector3.Cross(ax, u);
                Vector3 f = ax * 0.5f;
                Vector3[] q = { f - u * 0.5f - w * 0.5f, f + u * 0.5f - w * 0.5f, f + u * 0.5f + w * 0.5f, f - u * 0.5f + w * 0.5f };
                Vector3 n = m.MultiplyVector(ax).normalized;
                int[] vi = new int[4];
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p = m.MultiplyPoint3x4(q[k]);
                    vi[k] = V(p, n, new Vector2(Vector3.Dot(p, m.MultiplyVector(w).normalized), Vector3.Dot(p, m.MultiplyVector(u).normalized)), skin);
                }
                Quad(mat, vi[0], vi[1], vi[2], vi[3], n);
            }
        }

        // ------------------------------------------------------------------ output

        /// <summary>Uniform scale of everything added so far (bodies are authored at 1.0 and scaled to height).</summary>
        public void ScaleVertices(float s)
        {
            for (int i = 0; i < verts.Count; i++) verts[i] *= s;
        }

        /// <summary>Builds the skinned mesh. Bind poses come from the bones' current (rest) placement relative to space.</summary>
        public Mesh ToMesh(string name, Transform[] bones, Transform space, out Material[] mats)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.boneWeights = weights.ToArray();
            var bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bind[i] = bones[i].worldToLocalMatrix * space.localToWorldMatrix;
            mesh.bindposes = bind;
            mesh.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++) mesh.SetTriangles(subTris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mats = materials.ToArray();
            return mesh;
        }
    }
}
