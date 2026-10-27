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

        /// <summary>In the cab, before the voicemail.</summary>
        public static Line[] CabBeforeVoicemail => new[]
        {
            L(Speaker.Luis, "Your phone buzzed again. Mia?", "Telefonun yine titredi. Mia mı?"),
            L(Speaker.Daniel, "Voicemail. Hang on.", "Sesli mesaj. Bir saniye.").Then(0.6f),
        };

        public static Line[] CabAfterVoicemail => new[]
        {
            L(Speaker.Daniel, "She's alone in the house.", "Evde tek başına."),
            L(Speaker.Luis, "Then we drop the rig and I drive you over myself. Siren or no siren.",
                            "O zaman aracı bırakırız, seni oraya kendim götürürüm. Siren olsa da olmasa da."),
            L(Speaker.Daniel, "Thanks, Luis.", "Sağ ol, Luis."),
        };

        /// <summary>19:00, the siren starts while they are still on Harlow.</summary>
        public static Line[] CabSiren => new[]
        {
            L(Speaker.Luis, "That's it. That's the siren.", "İşte bu. Siren çaldı."),
            L(Speaker.Daniel, "Lights on. Get us off the main road.", "Işıkları aç. Bizi ana yoldan çıkar."),
            L(Speaker.Luis, "Lights on. Hold on to something.", "Işıklar açık. Bir yere tutun."),
        };

        public static Line CabGridDown => L(Speaker.Luis, "The grid's gone. I can't see a damn—", "Elektrikler gitti. Hiçbir şey göremiyor—").Then(0f);
        public static Line CabWarning => L(Speaker.Daniel, "Luis!", "Luis!").Then(0f);

        /// <summary>Coming round on the asphalt after the crash.</summary>
        public static Line[] WakeUp => new[]
        {
            L(Speaker.Luis, "Danny... Danny!", "Danny... Danny!"),
            L(Speaker.Daniel, "I'm here. I'm okay.", "Buradayım. İyiyim."),
        };

        public static Line LuisCalling(int i) => i % 2 == 0
            ? L(Speaker.Luis, "Over here. I can't get up.", "Buradayım. Kalkamıyorum.")
            : L(Speaker.Luis, "Danny, my leg... I can't feel my leg.", "Danny, bacağım... Bacağımı hissetmiyorum.");

        public static Line[] LuisHurt => new[]
        {
            L(Speaker.Luis, "Truck came out of nowhere. No lights. It was waiting for us.",
                            "Kamyon birden çıktı. Farları kapalıydı. Bizi bekliyordu."),
            L(Speaker.Daniel, "Let me see the leg.", "Bacağına bakayım."),
            L(Speaker.Luis, "It's bad, Danny. The jump bag went out the back.", "Kötü, Danny. Çanta arkadan fırladı."),
        };

        public static Line LuisChoice => L(Speaker.Luis, "Patch me up, or go. Mia needs you more than I do.",
                                           "Ya beni sar ya da git. Mia'nın sana benden çok ihtiyacı var.");

        /// <summary>The first Smilers, standing at the intersection, filming.</summary>
        public static Line[] SmilersSighted => new[]
        {
            L(Speaker.Luis, "Danny. Up the street.", "Danny. Yolun yukarısında."),
            L(Speaker.Daniel, "Who the hell are they?", "Bunlar da kim?"),
            L(Speaker.Luis, "Smilers. They hunt on stream. If they're filming us, we're the show.",
                            "Smiler'lar. Canlı yayında av yapıyorlar. Bizi çekiyorlarsa, gösteri biziz."),
        };

        public static Line[] SmilersSightedAlone => new[]
        {
            L(Speaker.Daniel, "Who the hell are they?", "Bunlar da kim?"),
        };

        public static Line[] LuisTreated => new[]
        {
            L(Speaker.Daniel, "Hold still. This'll hurt.", "Kıpırdama. Bu acıtacak."),
        };

        public static Line[] LuisThanks => new[]
        {
            L(Speaker.Luis, "Thanks, brother. I'll crawl into the rig and lie low.",
                            "Sağ ol kardeşim. Aracın içine girip saklanırım."),
            L(Speaker.Daniel, "I'll come back for you.", "Senin için geri döneceğim."),
        };

        public static Line LuisGo => L(Speaker.Luis, "Go, Danny. Mia's waiting.", "Git Danny. Mia bekliyor.");

        /// <summary>Luis gives the way (and points it): owner found the direction after the crash unclear.</summary>
        public static Line[] LuisDirections => new[]
        {
            L(Speaker.Luis, "Mia's on Kessler, right? South. Down Harlow, past the gas station.",
                            "Mia Kessler Sokağı'nda, değil mi? Güneye. Harlow'dan aşağı, benzin istasyonunu geç."),
            L(Speaker.Luis, "They closed the street behind us. North's no good.",
                            "Arkamızdan yolu kapattılar. Kuzeyden olmaz."),
        };

        public static Line WrongWay => L(Speaker.Daniel, "Not that way. Home's south.", "O taraf olmaz. Ev güneyde.");

        /// <summary>Treated: Luis hands over his radio and becomes Daniel's eyes on the dispatch channel.</summary>
        public static Line[] LuisGivesRadio => new[]
        {
            L(Speaker.Luis, "Take my radio. I'll listen to Dispatch and be your eyes.",
                            "Telsizimi al. Merkezi dinler, senin gözün kulağın olurum."),
            L(Speaker.Daniel, "Channel two. Keep your head down, Luis.", "İkinci kanal. Başını eğik tut, Luis."),
        };

        public static Line[] LuisRadioWarning => new[]
        {
            L(Speaker.LuisRadio, "Danny, you there? Dispatch has looters at the gas station on Harlow. Go around the back.",
                                 "Danny, orada mısın? Merkez, Harlow'daki benzin istasyonunda yağmacılar var diyor. Arkadan dolaş."),
            L(Speaker.Daniel, "Copy. Thanks, Luis.", "Anlaşıldı. Sağ ol, Luis.").Via(LineChannel.Radio),
        };

        /// <summary>Left behind: the Smilers come back for him.</summary>
        public static Line LuisAfterShots => L(Speaker.Daniel, "Luis...?", "Luis...?");

        public static Line LuisAbandoned => L(Speaker.Luis, "Danny? Danny, don't... don't leave me here!",
                                              "Danny? Danny, yapma... beni burada bırakma!");

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
