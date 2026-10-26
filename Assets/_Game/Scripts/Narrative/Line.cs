using UnityEngine;
using Echoes.Audio;

namespace Echoes.Narrative
{
    public enum Speaker { Daniel, Mia, MiaClone, Luis, Dispatch, Smiler, Looter, Civilian, CivilianWoman, Hale }

    /// <summary>Where a voice comes from: Daniel's own (chest mic), a person in the world, his phone, or the radio.</summary>
    public enum LineChannel { Self, World, Phone, Radio }

    /// <summary>How each speaker is shown and heard. The cloned Mia is labelled exactly like Mia: the game never tells.</summary>
    public static class Speakers
    {
        public struct Profile
        {
            public string name;
            public Color color;
            public SoundId voice;
            public LineChannel channel;
            public float glitch;
        }

        public static Profile Get(Speaker s)
        {
            switch (s)
            {
                case Speaker.Daniel: return P("DANIEL", new Color(0.8f, 0.9f, 1f), SoundId.VoiceDaniel, LineChannel.Self);
                case Speaker.Mia: return P("MIA", new Color(1f, 0.68f, 0.82f), SoundId.VoiceMiaPhone, LineChannel.Phone);
                case Speaker.MiaClone: return P("MIA", new Color(1f, 0.68f, 0.82f), SoundId.VoiceMiaClone, LineChannel.Phone, 0.35f);
                case Speaker.Luis: return P("LUIS", new Color(0.7f, 1f, 0.75f), SoundId.VoiceLuis, LineChannel.World);
                case Speaker.Dispatch: return P("DISPATCH", new Color(1f, 0.82f, 0.45f), SoundId.VoiceDispatch, LineChannel.Radio);
                case Speaker.Smiler: return P("???", new Color(0.4f, 1f, 1f), SoundId.VoiceSmiler, LineChannel.World);
                case Speaker.Looter: return P("LOOTER", new Color(1f, 0.55f, 0.45f), SoundId.VoiceMale, LineChannel.World);
                case Speaker.CivilianWoman: return P("WOMAN", new Color(0.9f, 0.9f, 0.75f), SoundId.VoiceFemale, LineChannel.World);
                case Speaker.Hale: return P("DET. HALE", new Color(0.75f, 0.75f, 0.75f), SoundId.VoiceHale, LineChannel.Self);
                default: return P("MAN", new Color(0.9f, 0.9f, 0.75f), SoundId.VoiceMale, LineChannel.World);
            }
        }

        static Profile P(string name, Color c, SoundId voice, LineChannel ch, float glitch = 0f) =>
            new Profile { name = name, color = c, voice = voice, channel = ch, glitch = glitch };
    }

    /// <summary>One spoken line: the English voice text and its Turkish subtitle, plus delivery options.</summary>
    public class Line
    {
        public Speaker who;
        public string en, tr;
        public LineChannel? channel;
        public float? glitch;
        public float pause = 0.3f;
        public Transform source;

        public Line(Speaker who, string en, string tr)
        {
            this.who = who;
            this.en = en;
            this.tr = tr;
        }

        /// <summary>Spoken by someone standing in the world (positional voice).</summary>
        public Line At(Transform speaker) { source = speaker; channel = LineChannel.World; return this; }
        public Line Via(LineChannel ch) { channel = ch; return this; }
        public Line Then(float seconds) { pause = seconds; return this; }
        public Line Glitch(float amount) { glitch = amount; return this; }
    }
}
