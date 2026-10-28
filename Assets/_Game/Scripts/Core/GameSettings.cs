using UnityEngine;

namespace Echoes.Core
{
    public enum SubtitleLanguage { English, Turkish }

    /// <summary>
    /// Player settings, saved in PlayerPrefs. Subtitles show one language only (owner's choice): the English voice
    /// text, or the Turkish translation. Chosen on the first start (intro card); the pause menu (Esc) changes it and
    /// the other settings; F3 and F2 still work as shortcuts.
    /// </summary>
    public static class GameSettings
    {
        const string LanguageKey = "Echoes.SubtitleLanguage";

        public static bool LanguageChosen => PlayerPrefs.HasKey(LanguageKey);

        public static SubtitleLanguage Subtitles
        {
            get => (SubtitleLanguage)PlayerPrefs.GetInt(LanguageKey, (int)SubtitleLanguage.Turkish);
            set
            {
                PlayerPrefs.SetInt(LanguageKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool Turkish => Subtitles == SubtitleLanguage.Turkish;

        /// <summary>The text for the chosen language.</summary>
        public static string Pick(string en, string tr) => Turkish ? tr : en;

        const string DamageNumbersKey = "Echoes.DamageNumbers";

        /// <summary>Faint damage numbers over hit enemies (on by default; F2 toggles until the settings menu).</summary>
        public static bool DamageNumbers
        {
            get => PlayerPrefs.GetInt(DamageNumbersKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(DamageNumbersKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Mouse look multiplier (0.25..3), set in the pause menu.</summary>
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat("Echoes.Sensitivity", 1f);
            set { PlayerPrefs.SetFloat("Echoes.Sensitivity", Mathf.Clamp(value, 0.25f, 3f)); PlayerPrefs.Save(); }
        }

        /// <summary>Master volume (0..1), applied to the AudioListener.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat("Echoes.Volume", 1f);
            set { PlayerPrefs.SetFloat("Echoes.Volume", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        /// <summary>Brightness as an exposure shift in EV (−1..+1.5), added by BodycamVolume.</summary>
        public static float Brightness
        {
            get => PlayerPrefs.GetFloat("Echoes.Brightness", 0f);
            set { PlayerPrefs.SetFloat("Echoes.Brightness", Mathf.Clamp(value, -1f, 1.5f)); PlayerPrefs.Save(); }
        }
    }
}
