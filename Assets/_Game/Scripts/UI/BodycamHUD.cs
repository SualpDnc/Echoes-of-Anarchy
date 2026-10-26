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

            if (subtitleText.text.Length > 0 && Time.time > subtitleUntil) subtitleText.text = "";
        }

        public void ShowSubtitle(string text, float duration)
        {
            subtitleText.text = text;
            subtitleUntil = Time.time + duration;
        }

        public void SetPrompt(string text) => promptText.text = text ?? "";
    }
}
