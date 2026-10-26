using System;
using UnityEngine;

namespace Echoes.Combat
{
    public enum NoiseKind { Footstep, Impact, Gunshot, Voice }

    public struct NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public NoiseKind kind;
        public GameObject source;
        public bool fromPlayer;
    }

    /// <summary>World-wide hearing bus. Anything audible to AI goes through Emit; Perception components listen.</summary>
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> Emitted;

        public static void Emit(Vector3 position, float radius, NoiseKind kind, GameObject source, bool fromPlayer)
        {
            Emitted?.Invoke(new NoiseEvent { position = position, radius = radius, kind = kind, source = source, fromPlayer = fromPlayer });
        }
    }
}
