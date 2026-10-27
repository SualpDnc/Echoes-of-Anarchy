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

        // ------------------------------------------------------------------ sequence 3: the gas station

        /// <summary>The looters talking while they work (positional): how the player finds them before they find him.</summary>
        public static Line LooterChatter(int i)
        {
            switch (i % 5)
            {
                case 0: return L(Speaker.Looter, "Grab the smokes. All of them.", "Sigaraları al. Hepsini.");
                case 1: return L(Speaker.Looter, "Hurry up, man. The Smilers are out tonight.", "Acele et be. Bu gece Smiler'lar dışarıda.");
                case 2: return L(Speaker.Looter, "Register's empty. Who keeps cash anymore?", "Kasa boş. Artık kim nakit tutar ki?");
                case 3: return L(Speaker.Looter, "Shut up and fill the bag.", "Kes sesini de çantayı doldur.");
                default: return L(Speaker.Looter, "You hear that? ...Nah. Nothing.", "Duydun mu? ...Yok. Bir şey yok.");
            }
        }

        public static Line StationSurrender => L(Speaker.Looter, "Don't shoot! I'm done, man, I'm done!", "Ateş etme! Bıraktım abi, bıraktım!");
        public static Line StationSpared => L(Speaker.Looter, "Thank you... thank you, man.", "Sağ ol... sağ ol abi.");

        /// <summary>The clerk hiding behind the counter.</summary>
        public static Line[] ClerkTalk => new[]
        {
            L(Speaker.CivilianWoman, "Don't— please. I don't have anything.", "Yapma— lütfen. Bende hiçbir şey yok."),
            L(Speaker.Daniel, "I'm a paramedic. Are you hurt?", "Ben sağlık görevlisiyim. Yaralı mısın?"),
            L(Speaker.CivilianWoman, "No... They came in through the back. The gate's open. Go, before more come.",
                                     "Hayır... Arkadan girdiler. Bahçe kapısı açık. Başkaları gelmeden git."),
            L(Speaker.CivilianWoman, "Here. Bottles from the back. Throw them, they'll chase the noise.",
                                     "Al. Arkadan şişeler. Fırlat, sesin peşinden giderler."),
            L(Speaker.Daniel, "Lock the door behind me.", "Arkamdan kapıyı kilitle."),
        };

        public static Line ClerkAgain => L(Speaker.CivilianWoman, "Just go. Please.", "Git artık. Lütfen.");

        /// <summary>Through the back gate. Luis checks in if he has the radio.</summary>
        public static Line[] PastTheStationRadio => new[]
        {
            L(Speaker.LuisRadio, "Danny? You made it past them? Good. Keep moving.", "Danny? Onları geçtin mi? Güzel. Devam et."),
            L(Speaker.Daniel, "I'm through.", "Geçtim.").Via(LineChannel.Radio),
        };

        // ------------------------------------------------------------------ sequence 4: the alleys

        public static Line AfterSchoolCall => L(Speaker.Daniel, "Hold on, sweetheart. I'm coming.", "Dayan tatlım. Geliyorum.");

        /// <summary>Luis (radio) after the call: the school has power in a blacked-out city.</summary>
        public static Line[] SchoolPowerRadio => new[]
        {
            L(Speaker.LuisRadio, "Danny, Dispatch says there's power at the school on Kessler. The whole grid's down and it's lit up like a stage.",
                                 "Danny, Merkez Kessler'daki okulda elektrik olduğunu söylüyor. Bütün şebeke çökmüş, orası sahne gibi ışıl ışıl."),
            L(Speaker.Daniel, "That's where she is.", "Mia orada.").Via(LineChannel.Radio),
        };

        /// <summary>The hunters come through the station's back gate.</summary>
        public static Line[] ChaseStart => new[]
        {
            L(Speaker.Smiler, "There he is! Smile, Danny!", "İşte orada! Gülümse, Danny!"),
            L(Speaker.Smiler, "Relax, Danny, nobody's killing you! Can't let the fun end this early!",
                              "Rahat ol Danny, seni öldürmeyeceğiz! Keyif hemen bitmesin!"),
            L(Speaker.Smiler, "Run, Danny! Chat wants a chase!", "Koş, Danny! Sohbet kovalamaca istiyor!"),
        };

        /// <summary>After the first rounds crack past him: they really are missing on purpose.</summary>
        public static Line ChaseWarningShots => L(Speaker.Smiler, "Those were warning shots, Danny! Keep running!",
                                                  "Onlar uyarı atışıydı, Danny! Koşmaya devam!");

        /// <summary>If Luis was left behind, they make sure Daniel knows what that cost.</summary>
        public static Line ChaseLuisTaunt => L(Speaker.Smiler, "Your buddy screamed your name, Danny! Over and over!",
                                               "Arkadaşın adını bağırdı, Danny! Defalarca!");

        public static Line ChaseRadio => L(Speaker.LuisRadio, "Danny, I can hear them laughing on the band. They're right behind you. Don't stop!",
                                           "Danny, telsizde gülüşlerini duyuyorum. Hemen arkandalar. Durma!");

        /// <summary>Daniel crawled under the shutter; they can't follow. They don't sound like they mind.</summary>
        public static Line[] ChaseLost => new[]
        {
            L(Speaker.Smiler, "Aw, he's shy!", "Ay, utangaçmış!"),
            L(Speaker.Smiler, "See you at school, Danny!", "Okulda görüşürüz, Danny!"),
        };

        public static Line ChaseFence => L(Speaker.Daniel, "Fenced off— the garage. Under the shutter!", "Çit var— garaj. Panjurun altından!");
        public static Line ChaseDeadEnd => L(Speaker.Daniel, "Dead end. Back, north!", "Çıkmaz. Geri, kuzeye!");

        public static Line FireworkSeen => L(Speaker.Daniel, "A grin... They're putting on a show.", "Bir sırıtış... Gösteri yapıyorlar.");

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
