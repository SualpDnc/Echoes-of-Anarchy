using UnityEngine;
using UnityEngine.UI;

namespace Echoes.UI
{
    /// <summary>
    /// 8d: a faint eye at the top of the screen while Daniel walks the party in a mask. It fills from the bottom as the
    /// guards grow suspicious (white, then amber, then red, pulsing near the top) and fades out when nobody is
    /// watching. Added by BodycamHUD; PartySequence drives it with Set.
    /// </summary>
    public class SuspicionEye : MonoBehaviour
    {
        public static SuspicionEye Instance { get; private set; }

        RawImage outline, fill;
        RectTransform fillRt;
        float shown, level, target;
        bool visible;

        void Awake()
        {
            Instance = this;
            var tex = EyeTexture();
            outline = Image("SuspicionEye", tex, new Vector2(0f, -70f));
            fill = Image("SuspicionFill", tex, Vector2.zero);
            fillRt = fill.rectTransform;
            fillRt.SetParent(outline.rectTransform, false);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.anchoredPosition = Vector2.zero;
            outline.color = new Color(1f, 1f, 1f, 0f);
            fill.color = new Color(1f, 1f, 1f, 0f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        RawImage Image(string name, Texture tex, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(72f, 36f);
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>`k` 0..1 = how close they are to seeing through him; `show` = the eye is up at all (the mask is on).</summary>
        public void Set(float k, bool show)
        {
            target = Mathf.Clamp01(k);
            visible = show;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            level = Mathf.MoveTowards(level, target, dt * 2f);
            // Up while anyone is suspicious; a ghost of it otherwise, so the player knows it is there.
            float want = !visible ? 0f : level > 0.02f ? 0.85f : 0.18f;
            shown = Mathf.MoveTowards(shown, want, dt * 2.5f);
            outline.color = new Color(1f, 1f, 1f, shown * 0.45f);
            Color c = level < 0.5f ? Color.Lerp(Color.white, new Color(1f, 0.75f, 0.2f), level * 2f)
                                   : Color.Lerp(new Color(1f, 0.75f, 0.2f), new Color(1f, 0.15f, 0.1f), (level - 0.5f) * 2f);
            if (level > 0.75f) c.a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
            fill.color = new Color(c.r, c.g, c.b, c.a * shown);
            fillRt.sizeDelta = new Vector2(0f, 36f * level);
            fill.uvRect = new Rect(0f, 0f, 1f, Mathf.Max(0.001f, level));
        }

        /// <summary>An almond eye outline with an iris, white on clear.</summary>
        static Texture2D EyeTexture()
        {
            const int w = 96, h = 48;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                // Lids: two arcs meeting at the corners.
                float lid = (1f - u * u) * 0.92f;
                float edge = Mathf.Abs(Mathf.Abs(v) - lid);
                float a = Mathf.Clamp01(1f - edge * h * 0.35f) * (Mathf.Abs(u) < 0.98f ? 1f : 0f);
                // Iris ring and pupil.
                float r = Mathf.Sqrt(u * u * 4f + v * v);
                if (Mathf.Abs(v) < lid) a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(r - 0.55f) * 12f), r < 0.28f ? 1f : 0f);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = "SuspicionEye", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
