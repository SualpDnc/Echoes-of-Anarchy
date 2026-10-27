using System.Collections.Generic;
using UnityEngine;

namespace Echoes.World
{
    /// <summary>Collects visual boxes and matching box colliders for one static object (set pieces: gas station, alleys).</summary>
    public class Solid
    {
        public readonly MeshBuilder mb = new MeshBuilder();
        readonly List<(Vector3 c, Vector3 s)> cols = new List<(Vector3, Vector3)>();

        public void Box(Material m, Vector3 c, Vector3 s, bool collide = true)
        {
            mb.Box(m, c, s);
            if (collide) cols.Add((c, s));
        }

        /// <summary>A collider with nothing drawn (invisible walls, or a collider simpler than the mesh).</summary>
        public void Collider(Vector3 c, Vector3 s) => cols.Add((c, s));

        public GameObject Build(string name, Transform parent)
        {
            var go = mb.Build(name, parent);
            go.isStatic = true;
            foreach (var (c, s) in cols)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = c;
                bc.size = s;
            }
            return go;
        }
    }
}
