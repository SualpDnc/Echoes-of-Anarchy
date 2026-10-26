using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// Accumulates geometry into per-material submeshes. UVs are world-scaled (1 UV unit = 1 metre) so
    /// textures keep a consistent physical size; materials set their tiling as 1 / tile size in metres.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<List<int>> subTris = new List<List<int>>();
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Material, int> matIndex = new Dictionary<Material, int>();

        /// <summary>Optional transform applied to all added geometry (local space of the target object).</summary>
        public Matrix4x4 matrix = Matrix4x4.identity;

        int Sub(Material m)
        {
            if (!matIndex.TryGetValue(m, out int idx))
            {
                idx = materials.Count;
                materials.Add(m);
                subTris.Add(new List<int>());
                matIndex[m] = idx;
            }
            return idx;
        }

        /// <summary>Quad from four corners in counter-clockwise order seen from the front (a=bottom-left, b=bottom-right, c=top-right, d=top-left).</summary>
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvC)
        {
            a = matrix.MultiplyPoint3x4(a); b = matrix.MultiplyPoint3x4(b);
            c = matrix.MultiplyPoint3x4(c); d = matrix.MultiplyPoint3x4(d);
            Vector3 n = Vector3.Cross(d - a, b - a).normalized; // Unity is left-handed: front faces wind clockwise
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
            uvs.Add(uvA); uvs.Add(new Vector2(uvC.x, uvA.y)); uvs.Add(uvC); uvs.Add(new Vector2(uvA.x, uvC.y));
            var t = subTris[Sub(m)];
            t.Add(s); t.Add(s + 2); t.Add(s + 1);
            t.Add(s); t.Add(s + 3); t.Add(s + 2);
        }

        /// <summary>Quad with UVs derived from its own size in metres (offset by uvOrigin for continuity across pieces).</summary>
        public void QuadWorldUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvOrigin)
        {
            float w = Vector3.Distance(a, b), h = Vector3.Distance(a, d);
            Quad(m, a, b, c, d, uvOrigin, uvOrigin + new Vector2(w, h));
        }

        /// <summary>Axis-aligned box (in builder space). Faces can be skipped with the mask (bit order: +X -X +Y -Y +Z -Z).</summary>
        public void Box(Material m, Vector3 center, Vector3 size, int faceMask = 0b111111, Material topMat = null)
        {
            Vector3 h = size * 0.5f;
            Vector3 p0 = center + new Vector3(-h.x, -h.y, -h.z), p1 = center + new Vector3(h.x, -h.y, -h.z);
            Vector3 p2 = center + new Vector3(h.x, -h.y, h.z), p3 = center + new Vector3(-h.x, -h.y, h.z);
            Vector3 p4 = center + new Vector3(-h.x, h.y, -h.z), p5 = center + new Vector3(h.x, h.y, -h.z);
            Vector3 p6 = center + new Vector3(h.x, h.y, h.z), p7 = center + new Vector3(-h.x, h.y, h.z);
            // UV origins use world position so adjacent boxes line up.
            if ((faceMask & 1) != 0) Quad(m, p1, p2, p6, p5, new Vector2(p1.z, p1.y), new Vector2(p6.z, p6.y));            // +X
            if ((faceMask & 2) != 0) Quad(m, p3, p0, p4, p7, new Vector2(-p3.z, p3.y), new Vector2(-p4.z, p4.y));          // -X
            if ((faceMask & 4) != 0) Quad(topMat ?? m, p4, p5, p6, p7, new Vector2(p4.x, p4.z), new Vector2(p6.x, p6.z));  // +Y
            if ((faceMask & 8) != 0) Quad(m, p3, p2, p1, p0, new Vector2(p3.x, -p3.z), new Vector2(p1.x, -p1.z));          // -Y
            if ((faceMask & 16) != 0) Quad(m, p2, p3, p7, p6, new Vector2(-p2.x, p2.y), new Vector2(-p7.x, p7.y));         // +Z
            if ((faceMask & 32) != 0) Quad(m, p0, p1, p5, p4, new Vector2(p0.x, p0.y), new Vector2(p5.x, p5.y));           // -Z
        }

        /// <summary>Box rotated about its own centre.</summary>
        public void BoxRotated(Material m, Vector3 center, Vector3 size, Quaternion rotation, int faceMask = 0b111111)
        {
            var saved = matrix;
            matrix = saved * Matrix4x4.TRS(center, rotation, Vector3.one);
            Box(m, Vector3.zero, size, faceMask);
            matrix = saved;
        }

        /// <summary>Capped cylinder along local Y.</summary>
        public void Cylinder(Material m, Vector3 baseCenter, float radius, float height, int segments = 16, bool caps = true, Material capMat = null)
        {
            float circ = 2f * Mathf.PI * radius;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f, a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = baseCenter + d0 * radius, b1 = baseCenter + d1 * radius;
                Vector3 t0 = b0 + Vector3.up * height, t1 = b1 + Vector3.up * height;
                float u0 = circ * i / segments, u1 = circ * (i + 1) / segments;
                SmoothQuad(m, b0, b1, t1, t0, d0, d1, new Vector2(u0, 0f), new Vector2(u1, height));
                if (caps)
                {
                    Tri(capMat ?? m, baseCenter + Vector3.up * height, t1, t0);
                    Tri(capMat ?? m, baseCenter, b0, b1);
                }
            }
        }

        void SmoothQuad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector2 uvA, Vector2 uvC)
        {
            int s = verts.Count;
            verts.Add(matrix.MultiplyPoint3x4(a)); verts.Add(matrix.MultiplyPoint3x4(b));
            verts.Add(matrix.MultiplyPoint3x4(c)); verts.Add(matrix.MultiplyPoint3x4(d));
            Vector3 wa = matrix.MultiplyVector(na).normalized, wb = matrix.MultiplyVector(nb).normalized;
            normals.Add(wa); normals.Add(wb); normals.Add(wb); normals.Add(wa);
            uvs.Add(new Vector2(uvA.x, uvA.y)); uvs.Add(new Vector2(uvC.x, uvA.y));
            uvs.Add(new Vector2(uvC.x, uvC.y)); uvs.Add(new Vector2(uvA.x, uvC.y));
            var t = subTris[Sub(m)];
            t.Add(s); t.Add(s + 2); t.Add(s + 1);
            t.Add(s); t.Add(s + 3); t.Add(s + 2);
        }

        public void Tri(Material m, Vector3 a, Vector3 b, Vector3 c)
        {
            a = matrix.MultiplyPoint3x4(a); b = matrix.MultiplyPoint3x4(b); c = matrix.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            // Planar projection on the dominant axis.
            uvs.Add(PlanarUV(a, n)); uvs.Add(PlanarUV(b, n)); uvs.Add(PlanarUV(c, n));
            var t = subTris[Sub(m)];
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }

        /// <summary>Extrudes a closed 2D profile (XY plane, counter-clockwise) along Z by depth, with caps.</summary>
        public void Extrude(Material side, Material cap, IList<Vector2> profile, float zMin, float zMax)
        {
            int n = profile.Count;
            float perim = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = profile[i], p1 = profile[(i + 1) % n];
                float len = Vector2.Distance(p0, p1);
                Vector3 a = new Vector3(p0.x, p0.y, zMin), b = new Vector3(p1.x, p1.y, zMin);
                Vector3 c = new Vector3(p1.x, p1.y, zMax), d = new Vector3(p0.x, p0.y, zMax);
                Quad(side, b, a, d, c, new Vector2(perim + len, zMin), new Vector2(perim, zMax));
                perim += len;
            }
            // Fan-triangulate caps (profiles are kept convex-ish by callers).
            for (int i = 1; i < n - 1; i++)
            {
                Tri(cap, new Vector3(profile[0].x, profile[0].y, zMin), new Vector3(profile[i + 1].x, profile[i + 1].y, zMin), new Vector3(profile[i].x, profile[i].y, zMin));
                Tri(cap, new Vector3(profile[0].x, profile[0].y, zMax), new Vector3(profile[i].x, profile[i].y, zMax), new Vector3(profile[i + 1].x, profile[i + 1].y, zMax));
            }
        }

        static Vector2 PlanarUV(Vector3 p, Vector3 n)
        {
            Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (an.y >= an.x && an.y >= an.z) return new Vector2(p.x, p.z);
            if (an.x >= an.z) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }

        public bool IsEmpty => verts.Count == 0;

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++) mesh.SetTriangles(subTris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Creates a GameObject with MeshFilter/MeshRenderer for the accumulated geometry.</summary>
        public GameObject Build(string name, Transform parent, bool castShadows = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            if (parent != null) go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = ToMesh(name);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = materials.ToArray();
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
    }
}
