using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>Development shortcuts for auditioning the soundscape. Stripped from release builds.</summary>
    public class AudioDevHotkeys : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.f9Key.wasPressedThisFrame && PurgeSiren.Instance != null) PurgeSiren.Instance.Sound();
            if (kb.f10Key.wasPressedThisFrame && TryGetComponent(out DistantEvents ev)) ev.TriggerRandom();
            if (kb.f11Key.wasPressedThisFrame && TryGetComponent(out DistantEvents ev2))
            {
                ev2.purgeActive = !ev2.purgeActive;
                Debug.Log("[Echoes] Distant events " + (ev2.purgeActive ? "ON" : "OFF"));
            }
        }
#endif
    }
}
