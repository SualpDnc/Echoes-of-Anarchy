using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Rendering
{
    /// <summary>Unity's built-in primitive meshes, fetched once (via CreatePrimitive) and shared.</summary>
    public static class PrimitiveMeshes
    {
        static readonly Dictionary<PrimitiveType, Mesh> cache = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Get(PrimitiveType type)
        {
            if (cache.TryGetValue(type, out var m) && m != null) return m;
            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return cache[type] = m;
        }

        /// <summary>Adds a renderer-only primitive (no collider) as a child.</summary>
        public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat,
                                      Quaternion? localRot = null, bool shadows = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<MeshFilter>().sharedMesh = Get(type);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
