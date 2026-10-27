using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Core;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// The black screen before the footage: on the very first start the subtitle language is chosen, then the date and
    /// the one rule of the night (8 seconds; owner: shorter, the Voss quote dropped). Space, Enter or a click skips ahead.
    /// </summary>
    public static class IntroCard
    {
        const string BodyEn =
            "<size=40><b>MARCH 21, 2034</b></size>\n\n" +
            "One night a year, for twelve hours, every crime is legal.\n" +
            "It starts at 19:00.";

        const string BodyTr =
            "<size=40><b>21 MART 2034</b></size>\n\n" +
            "Yılda bir gece, on iki saat boyunca her suç yasal.\n" +
            "19:00'da başlıyor.";

        public static IEnumerator Play()
        {
            var hud = BodycamHUD.Instance;
            if (hud == null) yield break;
            hud.SetFade(1f);

            if (!GameSettings.LanguageChosen)
            {
                hud.SetCard("<size=26>SUBTITLES  /  ALTYAZI</size>\n\n[1]  English          [2]  Türkçe", 1f);
                while (true)
                {
                    var kb = Keyboard.current;
                    if (kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)) { GameSettings.Subtitles = SubtitleLanguage.English; break; }
                    if (kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)) { GameSettings.Subtitles = SubtitleLanguage.Turkish; break; }
                    yield return null;
                }
                hud.SetCard("", 0f);
                yield return new WaitForSeconds(0.5f);
            }

            string body = GameSettings.Pick(BodyEn, BodyTr);
            float t = 0f, total = 8f;
            yield return null; // don't take the language key press as a skip
            while (t < total)
            {
                t += Time.deltaTime;
                hud.SetCard(body, Mathf.Clamp01(t / 1f) * Mathf.Clamp01((total - t) / 0.8f));
                if (Skip() && t < total - 0.8f) t = total - 0.8f; // jump to the fade-out
                yield return null;
            }
            hud.SetCard("", 0f);
        }

        static bool Skip()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
