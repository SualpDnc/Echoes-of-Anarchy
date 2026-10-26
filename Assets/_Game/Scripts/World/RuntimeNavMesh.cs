using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Echoes.World
{
    /// <summary>Bakes the NavMesh from physics colliders when the level loads, so generated worlds never need a saved bake.</summary>
    [RequireComponent(typeof(NavMeshSurface))]
    [DefaultExecutionOrder(-50)]
    public class RuntimeNavMesh : MonoBehaviour
    {
        void Awake()
        {
            Echoes.AI.LightExposure.Collect(transform); // stealth needs every world light; this is the world root
            var surface = GetComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            float t = Time.realtimeSinceStartup;
            surface.BuildNavMesh();
            Debug.Log($"[Echoes] NavMesh built in {(Time.realtimeSinceStartup - t) * 1000f:0} ms");
        }
    }
}
