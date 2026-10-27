using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// Plays dialogue: shows the English line and Turkish subtitle, and "voices" it with babble syllables timed to
    /// the words (pauses at commas and full stops, falling pitch, a rise on questions). Also runs phone calls,
    /// voicemails and radio traffic. Story sequences yield on these coroutines.
    /// </summary>
    public class DialogueDirector : MonoBehaviour
    {
        public static DialogueDirector Instance { get; private set; }

        [Range(0f, 1.5f)] public float voiceVolume = 0.65f; // owner: babble reads as less artificial when it sits lower in the mix
        [Tooltip("Minimum seconds a subtitle stays up per character of Turkish text (reading speed).")]
        public float readSecondsPerChar = 0.055f;

        public bool IsSpeaking { get; private set; }

        struct Word { public int syllables; public float pauseAfter; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Coroutine Run(IEnumerator sequence) => StartCoroutine(sequence);

        /// <summary>Lines in order. Once anyone speaking in it has died, the conversation is over (no replies to a corpse).</summary>
        public IEnumerator Conversation(params Line[] lines)
        {
            foreach (var l in lines)
            {
                foreach (var other in lines) if (IsDead(other)) yield break;
                yield return Say(l);
            }
        }

        /// <summary>The speaker of a line said in the world (someone standing there) has been killed.</summary>
        static bool IsDead(Line line)
        {
            if (line.source == null || (line.channel ?? Speakers.Get(line.who).channel) != LineChannel.World) return false;
            var h = line.source.GetComponentInParent<Health>();
            return h != null && h.IsDead;
        }

        /// <summary>Radio traffic: a squelch before and after each transmission.</summary>
        public IEnumerator Radio(params Line[] lines)
        {
            var am = AudioManager.Instance;
            foreach (var l in lines)
            {
                bool radio = (l.channel ?? Speakers.Get(l.who).channel) == LineChannel.Radio;
                if (radio && am != null) am.Play2D(SoundId.RadioSquelch, 0.45f);
                if (radio) yield return new WaitForSeconds(0.15f);
                yield return Say(l);
                if (radio && am != null) am.Play2D(SoundId.RadioSquelch, 0.4f);
            }
        }

        /// <summary>A phone call. Incoming calls ring twice before Daniel picks up.</summary>
        public IEnumerator Call(string label, bool incoming, params Line[] lines) => PhoneCall(label, incoming, false, lines);

        /// <summary>A call that dies mid-sentence: a burst of static and "CALL LOST" instead of a hang-up.</summary>
        public IEnumerator DroppedCall(string label, bool incoming, params Line[] lines) => PhoneCall(label, incoming, true, lines);

        IEnumerator PhoneCall(string label, bool incoming, bool dropped, Line[] lines)
        {
            var am = AudioManager.Instance;
            var hud = BodycamHUD.Instance;
            if (incoming)
            {
                if (hud != null) hud.ShowCall(label, true);
                for (int i = 0; i < 2; i++)
                {
                    if (am != null) am.Play2D(SoundId.PhoneRing, 0.7f);
                    yield return new WaitForSeconds(2.2f);
                }
            }
            if (am != null) am.Play2D(SoundId.PhonePickup, 0.6f);
            if (hud != null) hud.ShowCall(label, false);
            yield return new WaitForSeconds(0.5f);
            foreach (var l in lines) yield return Say(l);
            if (dropped)
            {
                if (am != null) am.Play2D(SoundId.RadioSquelch, 0.6f, 0.7f);
                if (hud != null) { hud.Glitch(0.2f); hud.ShowCall(label + "  ·  CALL LOST", false); }
                yield return new WaitForSeconds(1.4f);
                if (hud != null) hud.HideCall();
                yield break;
            }
            yield return new WaitForSeconds(0.3f);
            if (am != null) am.Play2D(SoundId.PhoneHangup, 0.5f);
            yield return new WaitForSeconds(0.8f);
            if (hud != null) hud.HideCall();
        }

        public IEnumerator Voicemail(string label, params Line[] lines)
        {
            var am = AudioManager.Instance;
            var hud = BodycamHUD.Instance;
            if (hud != null) hud.ShowCall("VOICEMAIL  ·  " + label, false);
            if (am != null) am.Play2D(SoundId.VoicemailBeep, 0.45f);
            yield return new WaitForSeconds(0.7f);
            foreach (var l in lines) yield return Say(l);
            yield return new WaitForSeconds(0.4f);
            if (am != null) am.Play2D(SoundId.PhonePickup, 0.4f);
            if (hud != null) hud.HideCall();
        }

        /// <summary>Speaks one line and returns when it has been said and could be read.</summary>
        public IEnumerator Say(Line line)
        {
            // The dead don't talk (owner: the gas station clerk kept talking after he shot her).
            if (IsDead(line)) yield break;
            var p = Speakers.Get(line.who);
            var ch = line.channel ?? p.channel;
            float glitch = line.glitch ?? p.glitch;
            var words = Plan(line.en);
            int total = 0;
            float estimate = 0f;
            foreach (var w in words) { total += w.syllables; estimate += w.syllables * 0.165f + w.pauseAfter; }
            float readTime = Mathf.Max(1.5f, GameSettings.Pick(line.en, line.tr).Length * readSecondsPerChar);
            float shown = Mathf.Max(estimate, readTime) + 0.4f;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowLine(p.name, p.color, line.en, line.tr, shown);

            IsSpeaking = true;
            float start = Time.time;
            var am = AudioManager.Instance;
            int variations = am != null ? am.Variations(p.voice) : 0;
            bool question = line.en.TrimEnd().EndsWith("?");
            float loud = line.en.Contains("!") ? 1.2f : 1f;
            int k = 0;
            foreach (var w in words)
            {
                for (int s = 0; s < w.syllables; s++)
                {
                    if (IsDead(line)) { CutOff(); yield break; } // killed mid-sentence
                    float progress = k / (float)Mathf.Max(1, total - 1);
                    float pitch = Mathf.Lerp(1.05f, 0.95f, progress) * Random.Range(0.96f, 1.04f);
                    if (question && k >= total - 2) pitch *= 1.12f;
                    float dur = 0.16f;
                    if (variations > 0)
                    {
                        int v = Random.Range(0, variations);
                        var clip = am.Clip(p.voice, v);
                        Voice(am, p.voice, v, ch, line.source, loud, pitch);
                        dur = clip.length / pitch * 0.85f;
                        // The clone sometimes catches on a syllable.
                        if (glitch > 0f && Random.value < glitch * 0.15f)
                        {
                            yield return new WaitForSeconds(0.05f);
                            Voice(am, p.voice, v, ch, line.source, loud, pitch);
                        }
                    }
                    yield return new WaitForSeconds(dur);
                    k++;
                }
                yield return new WaitForSeconds(w.pauseAfter);
            }
            IsSpeaking = false;
            float until = start + shown - 0.4f;
            while (Time.time < until)
            {
                if (IsDead(line)) { CutOff(); yield break; }
                yield return null;
            }
            yield return new WaitForSeconds(line.pause);
        }

        void CutOff()
        {
            IsSpeaking = false;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowSubtitle("", 0f);
        }

        void Voice(AudioManager am, SoundId id, int variation, LineChannel ch, Transform source, float loud, float pitch)
        {
            float v = voiceVolume * loud;
            switch (ch)
            {
                case LineChannel.World when source != null:
                    am.PlayAt(id, source.position + Vector3.up * 1.55f, 0.9f * v, pitch, 2f, 35f, false, variation);
                    break;
                case LineChannel.Phone: am.Play2D(id, 0.6f * v, pitch, variation); break;
                case LineChannel.Radio: am.Play2D(id, 0.55f * v, pitch, variation); break;
                default: am.Play2D(id, 0.75f * v, pitch, variation); break;
            }
        }

        /// <summary>Words with English syllable counts and the pause their punctuation asks for.</summary>
        static List<Word> Plan(string text)
        {
            var list = new List<Word>();
            foreach (string token in text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                float pause = 0.04f;
                if (token.EndsWith(",") || token.EndsWith(";") || token.EndsWith(":")) pause = 0.16f;
                else if (token.EndsWith("...") || token.EndsWith("—") || token.EndsWith("-")) pause = 0.24f;
                else if (token.EndsWith(".") || token.EndsWith("!") || token.EndsWith("?")) pause = 0.3f;
                int syl = Syllables(token);
                if (syl == 0 && list.Count > 0)
                {
                    var last = list[list.Count - 1];
                    last.pauseAfter = Mathf.Max(last.pauseAfter, pause);
                    list[list.Count - 1] = last;
                    continue;
                }
                list.Add(new Word { syllables = syl, pauseAfter = pause });
            }
            return list;
        }

        static int Syllables(string word)
        {
            int count = 0, letters = 0;
            bool prevVowel = false;
            string w = word.ToLowerInvariant();
            foreach (char c in w)
            {
                if (!char.IsLetter(c)) { prevVowel = false; continue; }
                letters++;
                bool vowel = "aeiouy".IndexOf(c) >= 0;
                if (vowel && !prevVowel) count++;
                prevVowel = vowel;
            }
            string trimmed = w.TrimEnd('.', ',', '!', '?', ';', ':', '—', '-');
            if (count > 1 && trimmed.EndsWith("e") && !trimmed.EndsWith("le")) count--;
            return letters == 0 ? 0 : Mathf.Max(1, count);
        }
    }
}
