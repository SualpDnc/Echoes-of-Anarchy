using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Core;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// The black screen before the footage: on the very first start the subtitle language is chosen, then the date,
    /// what the Purge is, and below it a quote from the man who wrote the law. Space, Enter or a click skips ahead.
    /// </summary>
    public static class IntroCard
    {
        const string BodyEn =
            "<size=40><b>MARCH 21, 2034</b></size>\n\n" +
            "Since the Renewal Act of 2029, for twelve hours once a year, every crime is legal.\n" +
            "From 19:00 to 07:00 there are no police, no fire crews, no ambulances.\n" +
            "The government calls it the Night of Renewal. Everyone else calls it the Purge.";

        const string BodyTr =
            "<size=40><b>21 MART 2034</b></size>\n\n" +
            "2029 Yenilenme Yasası'ndan beri, yılda bir kez, on iki saat boyunca her suç yasal.\n" +
            "19:00'dan 07:00'ye kadar polis yok, itfaiye yok, ambulans yok.\n" +
            "Hükümet buna Yenilenme Gecesi diyor. Herkes ise Purge.";

        // A fictional author of the law: the regime's own justification, as cold as it sounds.
        const string QuoteEn =
            "<i>\"Give a people one night to bleed, and they will repay you with three hundred and sixty-four days of order.\"</i>\n" +
            "<size=24>— Senator Harlan Voss, author of the Renewal Act</size>";

        const string QuoteTr =
            "<i>\"Bir halka kanaması için tek bir gece verin; size üç yüz altmış dört günlük itaatle geri ödesin.\"</i>\n" +
            "<size=24>— Senatör Harlan Voss, Yenilenme Yasası'nın mimarı</size>";

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

            string body = GameSettings.Pick(BodyEn, BodyTr), quote = GameSettings.Pick(QuoteEn, QuoteTr);
            // The quote is always laid out (transparent until it fades in), so the text above it never jumps.
            float t = 0f, total = 15f;
            yield return null; // don't take the language key press as a skip
            while (t < total)
            {
                t += Time.deltaTime;
                float bodyA = Mathf.Clamp01(t / 1.2f) * Mathf.Clamp01((total - t) / 0.8f);
                float quoteA = Mathf.Clamp01((t - 5f) / 1.5f) * Mathf.Clamp01((total - t) / 0.8f);
                hud.SetCard(body + "\n\n\n<color=#FFFFFF" + Hex(quoteA * 0.85f) + ">" + quote + "</color>", bodyA);
                if (Skip() && t < total - 0.8f) t = total - 0.8f; // jump to the fade-out
                yield return null;
            }
            hud.SetCard("", 0f);
        }

        static string Hex(float a) => Mathf.RoundToInt(Mathf.Clamp01(a) * 255f).ToString("X2");

        static bool Skip()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
