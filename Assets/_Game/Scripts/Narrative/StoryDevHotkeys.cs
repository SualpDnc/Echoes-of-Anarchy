using System.Collections;
using UnityEngine;
using Echoes.Core;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// Development shortcuts for the narrative systems: F5 plays sample dialogue (radio, voicemail, the cloned
    /// call), F6 jumps the clock to 18:59:50 so the 19:00 Purge beat can be watched. Stripped from release builds.
    /// </summary>
    public class StoryDevHotkeys : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Coroutine running;

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.f5Key.wasPressedThisFrame && DialogueDirector.Instance != null)
            {
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(Sample());
            }
            if (kb.f6Key.wasPressedThisFrame && GameClock.Instance != null)
            {
                GameClock.Instance.JumpTo(18, 59, 50);
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowCenter("18:59:50", 1.5f);
            }
        }

        IEnumerator Sample()
        {
            var d = DialogueDirector.Instance;
            yield return d.Radio(DemoLines.ColdOpenRadio);
            yield return new WaitForSeconds(1f);
            yield return d.Voicemail("MIA", DemoLines.MiaVoicemail);
            yield return new WaitForSeconds(2f);
            yield return d.Call("MIA", true, DemoLines.SchoolCall);
            running = null;
        }
#endif
    }
}
