using UnityEngine;

namespace Echoes.Core
{
    public enum SubtitleLanguage { English, Turkish }

    /// <summary>
    /// Player settings, saved in PlayerPrefs. Subtitles show one language only (owner's choice): the English voice
    /// text, or the Turkish translation. Chosen on the first start (intro card); F3 switches it until the phase 7
    /// settings menu exists.
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
    }
}
