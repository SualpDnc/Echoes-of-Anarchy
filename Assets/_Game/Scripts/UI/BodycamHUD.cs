using UnityEngine;
using UnityEngine.UI;
using Echoes.Core;
using Echoes.Rendering;

namespace Echoes.UI
{
    /// <summary>
    /// Burned-in bodycam overlay built entirely in code: REC indicator, device ID, timestamp,
    /// sensor grain + interlace lines, plus subtitle and interaction prompt lines.
    /// </summary>
    public class BodycamHUD : MonoBehaviour
    {
        public static BodycamHUD Instance { get; private set; }

        public string deviceLine = "MERIDIAN EMS  BWC-14  D.REYES #4471";
        [Range(0f, 0.2f)] public float grainAlpha = 0.006f;
        [Range(0f, 0.3f)] public float scanlineAlpha = 0.01f;

        Canvas canvas;
        Text recText, stampText, deviceText, subtitleText, promptText;
        RawImage grainImg, scanImg;
        Font mono;
        float subtitleUntil;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            mono = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Lucida Console", "Courier New" }, 32);
            if (mono == null) mono = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            grainImg = FullScreenRaw("Grain", ProcTextures.Grain(), grainAlpha);
            scanImg = FullScreenRaw("Scanlines", ProcTextures.Scanlines(), scanlineAlpha);

            recText = MakeText("REC", 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(48, -36), new Color(1f, 0.15f, 0.1f));
            recText.text = "● REC";
            deviceText = MakeText("Device", 24, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-48, -36), new Color(1f, 1f, 1f, 0.9f));
            deviceText.text = deviceLine;
            stampText = MakeText("Stamp", 26, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-48, -68), new Color(1f, 1f, 1f, 0.9f));

            subtitleText = MakeText("Subtitle", 34, TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0, 110), Color.white);
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.rectTransform.sizeDelta = new Vector2(1500, 200);
            promptText = MakeText("Prompt", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Color(1f, 1f, 1f, 0.85f));
            promptText.rectTransform.sizeDelta = new Vector2(900, 60);
        }

        RawImage FullScreenRaw(string name, Texture tex, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            return img;
        }

        Text MakeText(string name, int size, TextAnchor anchor, Vector2 pivotAnchor, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = pivotAnchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900, 40);
            var t = go.GetComponent<Text>();
            t.font = mono;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = go.GetComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        void Update()
        {
            // REC blinks at 1 Hz like real devices.
            recText.enabled = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;

            var clock = GameClock.Instance;
            if (clock != null)
                stampText.text = clock.Now.ToString("yyyy-MM-dd  HH:mm:ss") + " -0500";

            // Shift noise every frame; offset scanlines by a pixel to fake interlace crawl.
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            grainImg.uvRect = new Rect(Random.value, Random.value, 4f * aspect, 4f);
            float lines = Screen.height / 4f;
            scanImg.uvRect = new Rect(0f, (Time.frameCount & 1) * 0.5f, 1f, lines);
            grainImg.color = new Color(1f, 1f, 1f, grainAlpha);
            scanImg.color = new Color(1f, 1f, 1f, scanlineAlpha);

            if (subtitleText.text.Length > 0 && Time.time > subtitleUntil)
            {
                subtitleText.text = "";
                if (englishText != null) englishText.text = "";
            }
        }

        public void ShowSubtitle(string text, float duration)
        {
            subtitleText.text = text;
            if (englishText != null) englishText.text = "";
            subtitleUntil = Time.time + duration;
        }

        // ------------------------------------------------------------------ dialogue

        Text englishText, callText;
        string callLabel;
        float callStart;
        bool callRinging;

        void EnsureDialogue()
        {
            if (englishText != null) return;
            var sans = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            englishText = MakeText("SubtitleEN", 25, TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0, 160), new Color(0.82f, 0.82f, 0.8f, 0.92f));
            englishText.font = sans;
            englishText.horizontalOverflow = HorizontalWrapMode.Wrap;
            englishText.rectTransform.sizeDelta = new Vector2(1400, 80);
            callText = MakeText("Call", 26, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -36), new Color(1f, 1f, 1f, 0.92f));
        }

        /// <summary>
        /// A spoken line: the English voice text (small, speaker name in their colour) above the Turkish subtitle.
        /// </summary>
        public void ShowLine(string speaker, Color color, string en, string tr, float duration)
        {
            EnsureDialogue();
            subtitleText.text = tr;
            englishText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{speaker}:</color> {en}";
            // Sit just above the Turkish block, however many lines it wraps to.
            englishText.rectTransform.anchoredPosition = new Vector2(0f, 110f + subtitleText.preferredHeight + 6f);
            subtitleUntil = Time.time + duration;
        }

        /// <summary>Phone call indicator at the top: ringing (blinking) or connected with a call timer.</summary>
        public void ShowCall(string label, bool ringing)
        {
            EnsureDialogue();
            callLabel = label;
            callRinging = ringing;
            callStart = Time.time;
        }

        public void HideCall()
        {
            callLabel = null;
            if (callText != null) callText.text = "";
        }

        void UpdateCall()
        {
            if (callText == null || callLabel == null) return;
            if (callRinging) callText.text = Mathf.Repeat(Time.time, 0.8f) < 0.5f ? "INCOMING CALL  ·  " + callLabel : "";
            else
            {
                int s = Mathf.FloorToInt(Time.time - callStart);
                callText.text = $"{callLabel}  ·  {s / 60:00}:{s % 60:00}";
            }
        }

        public void SetPrompt(string text) => promptText.text = text ?? "";

        // ------------------------------------------------------------------ combat overlays

        Text ammoText, centerText;
        RawImage fadeImg;
        float ammoUntil, centerUntil;

        void EnsureCombatOverlays()
        {
            if (ammoText != null) return;
            ammoText = MakeText("Ammo", 26, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-48, 40), new Color(1f, 1f, 1f, 0.85f));
            fadeImg = FullScreenRaw("Fade", Texture2D.whiteTexture, 0f);
            fadeImg.color = new Color(0f, 0f, 0f, 0f);
            centerText = MakeText("Center", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
            centerText.rectTransform.sizeDelta = new Vector2(1400, 120);
        }

        /// <summary>Ammo is only shown when checked (reload / R on a full mag) — Daniel counts, the HUD doesn't.</summary>
        public void ShowAmmo(string text, float duration = 2.2f)
        {
            EnsureCombatOverlays();
            ammoText.text = text;
            ammoUntil = Time.time + duration;
        }

        public void SetFade(float alpha)
        {
            EnsureCombatOverlays();
            fadeImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        }

        public void ShowCenter(string text, float duration)
        {
            EnsureCombatOverlays();
            centerText.text = text;
            centerUntil = Time.time + duration;
            centerText.transform.SetAsLastSibling();
        }

        // ------------------------------------------------------------------ vitals

        RectTransform vitalsFill;
        RawImage vitalsFillImg;
        Text vitalsLabel;

        void EnsureVitals()
        {
            if (vitalsFill != null) return;
            vitalsLabel = MakeText("VitalsLabel", 22, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(48, 62), new Color(1f, 1f, 1f, 0.85f));
            var bg = FullScreenRaw("VitalsBack", Texture2D.whiteTexture, 0.35f);
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var brt = bg.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 0f);
            brt.anchoredPosition = new Vector2(48f, 40f);
            brt.sizeDelta = new Vector2(280f, 14f);
            vitalsFillImg = FullScreenRaw("VitalsFill", Texture2D.whiteTexture, 1f);
            vitalsFill = vitalsFillImg.rectTransform;
            vitalsFill.SetParent(brt, false);
            vitalsFill.anchorMin = new Vector2(0f, 0f);
            vitalsFill.anchorMax = new Vector2(1f, 1f);
            vitalsFill.pivot = new Vector2(0f, 0.5f);
            vitalsFill.offsetMin = new Vector2(2f, 2f);
            vitalsFill.offsetMax = new Vector2(-2f, -2f);

        }

        /// <summary>Health 0..1 and med-kit count, bottom-left like a vitals monitor clipped to the vest.</summary>
        public void SetVitals(float fraction, int medKits)
        {
            EnsureVitals();
            fraction = Mathf.Clamp01(fraction);
            vitalsFill.anchorMax = new Vector2(fraction, 1f);
            Color c = fraction > 0.5f ? new Color(0.9f, 0.95f, 0.9f) : Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.8f, 0.3f), (fraction - 0.15f) / 0.35f);
            if (fraction < 0.3f) c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 5f)); // pulse when critical
            vitalsFillImg.color = c;
            vitalsLabel.text = $"VITALS {Mathf.CeilToInt(fraction * 100f),3}   MED x{medKits}";
        }

        void LateUpdate()
        {
            UpdateCall();
            if (ammoText == null) return;
            if (ammoText.text.Length > 0 && Time.time > ammoUntil) ammoText.text = "";
            if (centerText.text.Length > 0 && Time.time > centerUntil) centerText.text = "";
        }
    }
}
