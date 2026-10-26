using UnityEngine;

namespace Echoes.Combat
{
    public enum SurfaceKind { Concrete, Asphalt, Metal, Wood, Glass, Flesh }

    /// <summary>Works out what a ray hit from the material under it (submesh-aware for mesh colliders).</summary>
    public static class SurfaceUtil
    {
        public static SurfaceKind Classify(RaycastHit hit)
        {
            if (hit.collider.GetComponent<Hitbox>() != null || hit.collider.GetComponentInParent<Health>() != null && hit.collider.GetComponent<CharacterController>() == null)
                return SurfaceKind.Flesh;
            string n = MaterialName(hit);
            if (n == null) return SurfaceKind.Concrete;
            if (n.Contains("Asphalt") || n.Contains("Roof") || n.Contains("Dirt")) return SurfaceKind.Asphalt;
            if (n.Contains("Glass")) return SurfaceKind.Glass;
            if (n.Contains("Metal") || n.Contains("Galvanized") || n.Contains("Rust") || n.Contains("Burnt") || n.Contains("Car")
                || n.Contains("Dumpster") || n.Contains("Shutter") || n.Contains("Chrome") || n.Contains("Signal")) return SurfaceKind.Metal;
            if (n.Contains("Wood") || n.Contains("Plywood")) return SurfaceKind.Wood;
            return SurfaceKind.Concrete;
        }

        public static string MaterialName(RaycastHit hit)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) return null;
            var mats = rend.sharedMaterials;
            if (mats.Length == 0) return null;
            if (mats.Length == 1 || !(hit.collider is MeshCollider mc) || mc.sharedMesh == null || hit.triangleIndex < 0)
                return mats[0] != null ? mats[0].name : null;
            var mesh = mc.sharedMesh;
            int idx = hit.triangleIndex * 3;
            for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
            {
                var sm = mesh.GetSubMesh(s);
                if (idx >= sm.indexStart && idx < sm.indexStart + sm.indexCount)
                    return mats[s] != null ? mats[s].name : null;
            }
            return mats[0] != null ? mats[0].name : null;
        }

        /// <summary>First hit along a ray that does not belong to `ignore` (an NPC's own body, the player's capsule...).</summary>
        public static bool RaycastIgnoring(Vector3 origin, Vector3 dir, float dist, Transform ignore, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
            hit = default;
            float best = float.MaxValue;
            bool found = false;
            foreach (var h in hits)
            {
                if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
                if (h.distance < best) { best = h.distance; hit = h; found = true; }
            }
            return found;
        }
    }
}
