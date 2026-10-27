using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Core;

namespace Echoes.UI
{
    /// <summary>
    /// Until the phase 7 menu exists: holding Esc for 1.5 s quits the game (a tap still frees the cursor, as before).
    /// Shared demo builds need a way out that isn't Alt+F4.
    /// </summary>
    public class QuitKey : MonoBehaviour
    {
        const float Hold = 1.5f;
        float held;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.isPressed) { held = 0f; return; }
            held += Time.unscaledDeltaTime;
            if (BodycamHUD.Instance != null)
                BodycamHUD.Instance.ShowCenter(GameSettings.Pick("HOLD ESC TO QUIT", "ÇIKMAK İÇİN ESC'YE BASILI TUT") + "  " + new string('|', Mathf.CeilToInt(held / Hold * 10f)), 0.3f);
            if (held < Hold) return;
            held = float.MinValue; // once
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
