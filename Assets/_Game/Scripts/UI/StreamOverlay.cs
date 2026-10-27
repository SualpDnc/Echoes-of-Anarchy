using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.UI
{
    /// <summary>
    /// The Smilers' stream breaking into the bodycam picture (sequence 5): a red LIVE badge with the channel and a
    /// climbing viewer count at top left, and their chat scrolling up the right edge. It is in-world text, so it stays
    /// English like the spray paint. `Show(true)` starts it, `Chat(user, text)` posts a line, filler chat runs on its own.
    /// </summary>
    public class StreamOverlay : MonoBehaviour
    {
        public static StreamOverlay Instance { get; private set; }

        const int ChatLines = 9;
        static readonly string[] Users = { "grinmaxxer", "xX_nightshift", "bloodmoon_tv", "purgedad", "lilsmile", "kesslerboi", "chat_decides", "4am_dolly", "renewal_enjoyer", "cleanstreets" };
        static readonly string[] Filler =
        {
            "SMILE DANNY", "he's so slow lmao", "daddy's home", "CLIP IT", "40k lets gooo", "is he crying", "run marine run",
            "nice photo grandpa", "the girl is inside??", "5$ he breaks the door", "W stream", "zoom on his face", "lmaooo",
            "best night of the year", "say hi danny", "round 2 when",
        };

        RectTransform root;
        Text badge, viewersText, chatText;
        Image badgeBg;
        readonly List<string> chat = new List<string>();
        float viewers, nextFiller;
        bool on;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            var hud = GetComponent<BodycamHUD>();
            Font font = hud != null && hud.Mono != null ? hud.Mono : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = new GameObject("Stream", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            var bg = new GameObject("LiveBadge", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(44f, -84f);
            brt.sizeDelta = new Vector2(132f, 40f);
            badgeBg = bg.GetComponent<Image>();
            badgeBg.color = new Color(0.85f, 0.05f, 0.1f, 0.92f);
            badgeBg.raycastTarget = false;
            badge = Label(font, bg.transform, "● LIVE", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(132f, 40f), new Vector2(0.5f, 0.5f));

            viewersText = Label(font, root, "", 24, TextAnchor.MiddleLeft, new Vector2(190f, -104f), new Vector2(700f, 40f), new Vector2(0f, 1f));
            chatText = Label(font, root, "", 22, TextAnchor.LowerLeft, new Vector2(-560f, 250f), new Vector2(520f, 420f), new Vector2(1f, 0f));
            chatText.horizontalOverflow = HorizontalWrapMode.Wrap;
            chatText.supportRichText = true;
            chatText.lineSpacing = 1.1f;
            root.gameObject.SetActive(false);
        }

        static Text Label(Font font, Transform parent, string s, int size, TextAnchor anchor, Vector2 pos, Vector2 sizeDelta, Vector2 pivotAnchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = pivotAnchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = s;
            var sh = go.GetComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public bool IsOn => on;

        public void Show(bool show, int startViewers = 38412)
        {
            if (root == null) Build();
            on = show;
            root.gameObject.SetActive(show);
            if (show) { viewers = startViewers; chat.Clear(); nextFiller = Time.time + 0.6f; Refresh(); }
        }

        public void Chat(string user, string text)
        {
            if (root == null) Build();
            chat.Add("<color=#" + ColorUtility.ToHtmlStringRGB(UserColor(user)) + ">" + user + "</color>  " + text);
            while (chat.Count > ChatLines) chat.RemoveAt(0);
            Refresh();
        }

        /// <summary>A line from a random viewer.</summary>
        public void Chat(string text) => Chat(Users[Random.Range(0, Users.Length)], text);

        static Color UserColor(string user)
        {
            float hue = Mathf.Repeat(user.GetHashCode() * 0.618f, 1f);
            return Color.HSVToRGB(hue, 0.55f, 1f);
        }

        void Refresh()
        {
            viewersText.text = "SMILE.TV/danny   " + Mathf.RoundToInt(viewers).ToString("N0") + " watching";
            chatText.text = string.Join("\n", chat);
        }

        void Update()
        {
            if (!on) return;
            viewers += Time.deltaTime * Random.Range(40f, 260f); // people keep joining
            badgeBg.color = new Color(0.85f, 0.05f, 0.1f, Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.85f ? 0.92f : 0.6f);
            if (Time.time > nextFiller)
            {
                nextFiller = Time.time + Random.Range(0.35f, 1.1f);
                Chat(Filler[Random.Range(0, Filler.Length)]);
            }
            Refresh();
        }
    }
}
