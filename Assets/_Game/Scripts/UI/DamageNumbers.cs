using UnityEngine;
using UnityEngine.UI;
using Echoes.Combat;
using Echoes.Core;

namespace Echoes.UI
{
    /// <summary>
    /// Faint damage numbers over whoever Daniel hurts, in the bodycam's own monospace: a small number that sits on the
    /// hit point, drifts up and fades. Headshots read warm red with a tiny "HEAD" tag; quick follow-up hits on the same
    /// target add into one number instead of stacking. Owner first asked for them faint, then found them (and the blood)
    /// invisible at range: they are now bold, outlined, and never shrink with distance.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        const int Count = 16;
        const float MergeWindow = 0.45f;
        static readonly Color BodyColor = new Color(1f, 0.96f, 0.88f, 0.92f);
        static readonly Color HeadColor = new Color(1f, 0.3f, 0.2f, 1f);

        class Label
        {
            public RectTransform rt;
            public Text text;
            public Shadow shadow;
            public Outline outline;
            public Health target;
            public Vector3 world;
            public float born, popAt, amount, jitter;
            public bool head, kill, active;
        }

        readonly Label[] labels = new Label[Count];
        RectTransform root;

        void Start()
        {
            var hud = GetComponent<BodycamHUD>();
            root = new GameObject("DamageNumbers", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling(); // under subtitles, prompts and the vitals bar

            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Dmg", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
                go.transform.SetParent(root, false);
                var l = new Label { rt = (RectTransform)go.transform, text = go.GetComponent<Text>(), shadow = DropShadow(go), outline = go.GetComponent<Outline>() };
                l.outline.effectDistance = new Vector2(1.2f, -1.2f);
                l.rt.anchorMin = l.rt.anchorMax = new Vector2(0.5f, 0.5f);
                l.rt.pivot = new Vector2(0.5f, 0f);
                l.rt.sizeDelta = new Vector2(200f, 60f);
                l.text.font = hud != null && hud.Mono != null ? hud.Mono : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                l.text.alignment = TextAnchor.LowerCenter;
                l.text.horizontalOverflow = HorizontalWrapMode.Overflow;
                l.text.verticalOverflow = VerticalWrapMode.Overflow;
                l.text.supportRichText = true;
                l.text.raycastTarget = false;
                l.shadow.effectDistance = new Vector2(2.5f, -2.5f);
                go.SetActive(false);
                labels[i] = l;
            }
            Health.AnyDamaged += OnDamaged;
        }

        void OnDestroy() => Health.AnyDamaged -= OnDamaged;

        /// <summary>Outline derives from Shadow, so GetComponent&lt;Shadow&gt; could return it.</summary>
        static Shadow DropShadow(GameObject go)
        {
            foreach (var s in go.GetComponents<Shadow>()) if (!(s is Outline)) return s;
            return null;
        }

        void OnDamaged(Health h, DamageInfo info)
        {
            if (!info.fromPlayer || h == null || h.CompareTag("Player") || info.amount < 0.5f || !GameSettings.DamageNumbers) return;
            bool head = info.part == BodyPart.Head, kill = h.Current <= 0f;
            float now = Time.time;

            // Rapid hits on the same person fold into the number already up.
            foreach (var l in labels)
            {
                if (!l.active || l.target != h || now - l.born > MergeWindow) continue;
                l.amount += info.amount;
                l.head |= head;
                l.kill |= kill;
                l.popAt = now;
                Refresh(l);
                return;
            }

            Label pick = null;
            foreach (var l in labels) if (!l.active && pick == null) pick = l;
            if (pick == null)
            {
                pick = labels[0];
                foreach (var l in labels) if (l.born < pick.born) pick = l;
            }
            pick.active = true;
            pick.target = h;
            pick.world = info.point;
            pick.born = pick.popAt = now;
            pick.amount = info.amount;
            pick.head = head;
            pick.kill = kill;
            pick.jitter = Random.Range(-14f, 14f);
            pick.rt.gameObject.SetActive(true);
            Refresh(pick);
        }

        void Refresh(Label l)
        {
            string n = Mathf.RoundToInt(l.amount).ToString();
            l.text.fontSize = l.head ? 40 : 32;
            l.text.text = l.head ? "<size=17>H E A D</size>\n" + n : n;
        }

        void LateUpdate()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
            {
                GameSettings.DamageNumbers = !GameSettings.DamageNumbers;
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowCenter(GameSettings.Pick("DAMAGE NUMBERS: ", "HASAR SAYILARI: ") + (GameSettings.DamageNumbers ? GameSettings.Pick("ON", "AÇIK") : GameSettings.Pick("OFF", "KAPALI")), 1.5f);
            }

            var cam = Camera.main;
            float now = Time.time;
            foreach (var l in labels)
            {
                if (!l.active) continue;
                float life = l.kill ? 1.9f : 1.4f, age = now - l.born;
                if (age > life || cam == null || !GameSettings.DamageNumbers) { l.active = false; l.rt.gameObject.SetActive(false); continue; }

                Vector3 sp = cam.WorldToScreenPoint(l.world);
                if (sp.z < 0.2f) { l.text.enabled = l.shadow.enabled = l.outline.enabled = false; continue; }
                l.text.enabled = l.shadow.enabled = l.outline.enabled = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out Vector2 local);

                // Rises quickly then hangs; far targets get a slightly smaller number.
                float k = age / life;
                float rise = 22f + 60f * (1f - (1f - k) * (1f - k));
                l.rt.anchoredPosition = local + new Vector2(l.jitter, rise);
                float pop = Mathf.Lerp(1.22f, 1f, Mathf.Clamp01((now - l.popAt) / 0.12f));
                float dist = Mathf.Clamp(sp.z / 25f, 1f, 1.25f); // never smaller with range: far hits are the ones that need it
                l.rt.localScale = Vector3.one * pop * dist;

                float fade = Mathf.Clamp01(age / 0.06f) * (1f - Mathf.SmoothStep(0.55f, 1f, k));
                Color c = l.head ? HeadColor : BodyColor;
                if (l.kill) c = Color.Lerp(c, new Color(0.75f, 0.2f, 0.16f, c.a), Mathf.Clamp01((age - 0.25f) / 0.5f)); // cools to dried red
                l.text.color = new Color(c.r, c.g, c.b, c.a * fade);
                l.shadow.effectColor = new Color(0f, 0f, 0f, 0.85f * fade);
                l.outline.effectColor = new Color(0f, 0f, 0f, 0.55f * fade);
            }
        }
    }
}
