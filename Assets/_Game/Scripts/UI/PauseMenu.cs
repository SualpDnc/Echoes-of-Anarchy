using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Echoes.Audio;
using Echoes.Core;
using Echoes.Player;
using Echoes.Rendering;

namespace Echoes.UI
{
    /// <summary>
    /// The Esc menu (owner request; replaces "hold Esc to quit"). Opening it freezes the game (time scale 0, the audio
    /// listener paused, no player input) and frees the cursor. Resume, Settings, Restart from the last checkpoint, Quit.
    /// Settings: mouse sensitivity, volume, brightness, subtitle language, damage numbers, all saved in GameSettings.
    /// Built in code on the HUD canvas in the bodycam's style: monospace text on smoked glass, red for the selection.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public bool IsOpen { get; private set; }
        /// <summary>The title screen has been passed once this launch (static: a restart or a death reload doesn't show it again).</summary>
        public static bool TitleShown;

        RectTransform root, mainPage, settingsPage, titlePage;
        bool title;
        Font font;
        float prevTimeScale = 1f;
        bool prevListenerPause;
        readonly List<(Text text, Func<string> value)> labels = new List<(Text, Func<string>)>();

        static readonly Color Ink = new Color(0.92f, 0.94f, 0.96f);
        static readonly Color Dim = new Color(0.6f, 0.64f, 0.7f);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (IsOpen) { Time.timeScale = 1f; AudioListener.pause = false; }
            EchoesInput.Paused = false;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (root != null) return; // already built by OpenTitle on the first frame
            AudioListener.volume = GameSettings.Volume;
            var hud = GetComponent<BodycamHUD>();
            font = hud != null && hud.Mono != null ? hud.Mono : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Build();
            root.gameObject.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (!IsOpen) Open();
            else if (settingsPage.gameObject.activeSelf) ShowPage(title ? titlePage : mainPage);
            else if (!title) Close();
        }

        /// <summary>
        /// The title screen (8f): the same menu with New game / Settings / Quit, shown once per launch before the intro
        /// card. The caller waits while <see cref="IsOpen"/>.
        /// </summary>
        public void OpenTitle()
        {
            if (root == null) { Start(); }
            title = true;
            Open();
            ShowPage(titlePage);
        }

        void StartGame()
        {
            TitleShown = true;
            Close();
            title = false;
        }

        // ------------------------------------------------------------------ open / close

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            EchoesInput.Paused = true;
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            prevListenerPause = AudioListener.pause;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            ShowPage(mainPage);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            EchoesInput.Paused = false;
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            AudioListener.pause = prevListenerPause;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            root.gameObject.SetActive(false);
        }

        void ShowPage(RectTransform page)
        {
            mainPage.gameObject.SetActive(page == mainPage);
            settingsPage.gameObject.SetActive(page == settingsPage);
            titlePage.gameObject.SetActive(page == titlePage);
            Refresh();
        }

        void Refresh()
        {
            foreach (var (text, value) in labels) text.text = value();
        }

        void Restart()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            EchoesInput.Paused = false;
            IsOpen = false;
            if (Checkpoint.Stage == CheckpointStage.None) Checkpoint.NewGame(); // the very start: forget the choices made so far
            else Checkpoint.ReloadAfterDeath();                                   // the last checkpoint
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ building

        void Build()
        {
            root = Rect("PauseMenu", transform, Vector2.zero, Vector2.one, Vector2.zero);
            root.offsetMin = root.offsetMax = Vector2.zero;
            var shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.78f);

            var heading = Label(root, () => title ? "ECHOES OF ANARCHY" : GameSettings.Pick("PAUSED", "DURAKLATILDI"), 54, new Vector2(0f, 300f), Ink);
            heading.fontStyle = FontStyle.Bold;
            Label(root, () => title ? GameSettings.Pick("EXHIBIT 14  ·  BWC-14  ·  D. REYES #4471", "DELİL 14  ·  BWC-14  ·  D. REYES #4471")
                                    : GameSettings.Pick("BWC-14  ·  RECORDING HELD", "BWC-14  ·  KAYIT BEKLEMEDE"), 22, new Vector2(0f, 248f), new Color(1f, 0.3f, 0.25f));

            titlePage = Rect("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Button(titlePage, () => GameSettings.Pick("PLAY THE TAPE", "KASETİ OYNAT"), new Vector2(0f, 90f), 460f, StartGame);
            Button(titlePage, () => GameSettings.Pick("SETTINGS", "AYARLAR"), new Vector2(0f, 15f), 460f, () => ShowPage(settingsPage));
            Button(titlePage, () => GameSettings.Pick("QUIT TO DESKTOP", "MASAÜSTÜNE ÇIK"), new Vector2(0f, -60f), 460f, Quit);
            Label(titlePage, () => GameSettings.Pick("About 30 minutes  ·  Use headphones", "Yaklaşık 30 dakika  ·  Kulaklık önerilir"), 20, new Vector2(0f, -170f), Dim);

            mainPage = Rect("Main", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Button(mainPage, () => GameSettings.Pick("RESUME", "DEVAM ET"), new Vector2(0f, 120f), 460f, Close);
            Button(mainPage, () => GameSettings.Pick("SETTINGS", "AYARLAR"), new Vector2(0f, 45f), 460f, () => ShowPage(settingsPage));
            Button(mainPage, () => Checkpoint.Stage == CheckpointStage.None
                       ? GameSettings.Pick("RESTART", "BAŞTAN BAŞLA")
                       : GameSettings.Pick("RESTART FROM CHECKPOINT", "KONTROL NOKTASINDAN BAŞLA"), new Vector2(0f, -30f), 460f, Restart);
            Button(mainPage, () => GameSettings.Pick("QUIT TO DESKTOP", "MASAÜSTÜNE ÇIK"), new Vector2(0f, -105f), 460f, Quit);
            Label(mainPage, () => GameSettings.Pick("ESC  resume", "ESC  devam"), 20, new Vector2(0f, -190f), Dim);

            settingsPage = Rect("Settings", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            float y = 150f;
            Setting(() => GameSettings.Pick("MOUSE SENSITIVITY", "FARE HASSASİYETİ"), () => GameSettings.Sensitivity.ToString("0.00") + "x",
                    d => GameSettings.Sensitivity = Mathf.Round((GameSettings.Sensitivity + d * 0.1f) * 100f) / 100f, ref y);
            Setting(() => GameSettings.Pick("VOLUME", "SES"), () => Mathf.RoundToInt(GameSettings.Volume * 100f) + "%",
                    d =>
                    {
                        GameSettings.Volume = Mathf.Round((GameSettings.Volume + d * 0.1f) * 10f) / 10f;
                        AudioListener.volume = GameSettings.Volume; // AudioManager keeps it from the next frame
                        // The world is paused behind the menu: a tick at the new level, so the change can be heard.
                        if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.FlashlightClick, 0.8f);
                    }, ref y);
            Setting(() => GameSettings.Pick("BRIGHTNESS", "PARLAKLIK"), () => (GameSettings.Brightness >= 0f ? "+" : "") + GameSettings.Brightness.ToString("0.00"),
                    d => { GameSettings.Brightness += d * 0.25f; if (BodycamVolume.Instance != null) BodycamVolume.Instance.ApplyBrightness(); }, ref y);
            Setting(() => GameSettings.Pick("SUBTITLES", "ALTYAZI"), () => GameSettings.Turkish ? "TÜRKÇE" : "ENGLISH",
                    d => GameSettings.Subtitles = GameSettings.Turkish ? SubtitleLanguage.English : SubtitleLanguage.Turkish, ref y);
            Setting(() => GameSettings.Pick("DAMAGE NUMBERS", "HASAR SAYILARI"), () => GameSettings.DamageNumbers ? GameSettings.Pick("ON", "AÇIK") : GameSettings.Pick("OFF", "KAPALI"),
                    d => GameSettings.DamageNumbers = !GameSettings.DamageNumbers, ref y);
            Button(settingsPage, () => GameSettings.Pick("BACK", "GERİ"), new Vector2(0f, y - 40f), 300f, () => ShowPage(title ? titlePage : mainPage));
        }

        /// <summary>One settings row: the name on the left, then ‹ value ›.</summary>
        void Setting(Func<string> name, Func<string> value, Action<int> change, ref float y)
        {
            var row = Rect("Row", settingsPage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y));
            var n = Label(row, name, 28, new Vector2(-170f, 0f), Ink);
            n.alignment = TextAnchor.MiddleLeft;
            n.rectTransform.sizeDelta = new Vector2(420f, 50f);
            Button(row, () => "<", new Vector2(150f, 0f), 60f, () => { change(-1); Refresh(); });
            var v = Label(row, value, 28, new Vector2(270f, 0f), new Color(1f, 0.85f, 0.6f));
            v.rectTransform.sizeDelta = new Vector2(180f, 50f);
            Button(row, () => ">", new Vector2(390f, 0f), 60f, () => { change(1); Refresh(); });
            y -= 70f;
        }

        RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        Text Label(Transform parent, Func<string> value, int size, Vector2 pos, Color color)
        {
            var rt = Rect("Label", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos);
            rt.sizeDelta = new Vector2(900f, size * 1.6f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.supportRichText = false;
            labels.Add((t, value));
            t.text = value();
            return t;
        }

        void Button(Transform parent, Func<string> label, Vector2 pos, float width, Action onClick)
        {
            var rt = Rect("Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos);
            rt.sizeDelta = new Vector2(width, 56f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            var c = b.colors;
            c.normalColor = new Color(1f, 1f, 1f, 0.06f);
            c.highlightedColor = new Color(0.85f, 0.15f, 0.12f, 0.45f);
            c.pressedColor = new Color(1f, 0.25f, 0.2f, 0.7f);
            c.selectedColor = c.normalColor;
            c.fadeDuration = 0.05f;
            b.colors = c;
            b.onClick.AddListener(() => onClick());
            var t = Label(rt, label, 30, Vector2.zero, Ink);
            t.rectTransform.sizeDelta = new Vector2(width, 56f);
        }
    }
}
