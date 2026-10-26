namespace Echoes.Narrative
{
    /// <summary>
    /// The demo's (Act 1) spoken lines: English voice text and Turkish subtitles. Sequences in phase 6b–6e use
    /// these. Hint for the twist: the real Mia says "Dad"; the clone says "Daddy" and knows things she shouldn't.
    /// </summary>
    public static class DemoLines
    {
        static Line L(Speaker s, string en, string tr) => new Line(s, en, tr);

        /// <summary>Cold open, in the ambulance at 18:52.</summary>
        public static Line[] ColdOpenRadio => new[]
        {
            L(Speaker.Dispatch, "Medic Fourteen, Dispatch. Lockdown in eight minutes. All units return to station.",
                                "Sağlık Ekibi On Dört, burası Merkez. Kapanmaya sekiz dakika. Tüm ekipler istasyona dönsün."),
            L(Speaker.Daniel, "Dispatch, Fourteen. We're coming in on Harlow. Ten minutes.",
                              "Merkez, burası On Dört. Harlow'dan geliyoruz. On dakika."),
            L(Speaker.Dispatch, "Ten minutes is past the siren, Fourteen. Your call.",
                                "On dakika sirenden sonrası demek, On Dört. Karar sizin."),
            L(Speaker.Luis, "We'll make it, Danny. We always make it.",
                            "Yetişiriz Danny. Hep yetişiriz."),
        };

        /// <summary>Mia's voicemail, played in the ambulance. This one is really her.</summary>
        public static Line[] MiaVoicemail => new[]
        {
            L(Speaker.Mia, "Dad, it's me. Mom's phone is off again.",
                           "Baba, benim. Annemin telefonu yine kapalı."),
            L(Speaker.Mia, "You said you'd be home before the siren. You promised.",
                           "Sirenden önce evde olacağını söylemiştin. Söz vermiştin."),
            L(Speaker.Mia, "I locked the doors like you showed me. Please hurry.",
                           "Bana gösterdiğin gibi kapıları kilitledim. Lütfen acele et."),
        };

        /// <summary>The alley call (sequence 4). Not her: "Daddy", the glitches, and the stream she couldn't know about.</summary>
        public static Line[] SchoolCall => new[]
        {
            L(Speaker.MiaClone, "Daddy? Daddy, it's me.", "Babacığım? Babacığım, benim."),
            L(Speaker.Daniel, "Mia? Where are you? Are you hurt?", "Mia? Neredesin? Yaralandın mı?"),
            L(Speaker.MiaClone, "Come to the school. I'm at the school.", "Okula gel. Okuldayım."),
            L(Speaker.Daniel, "Stay where you are. I'm coming.", "Olduğun yerde kal. Geliyorum."),
            L(Speaker.MiaClone, "Hurry, Daddy. Everyone's watching.", "Acele et babacığım. Herkes izliyor.").Glitch(0.7f),
        };
    }
}
