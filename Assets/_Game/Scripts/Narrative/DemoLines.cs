namespace Echoes.Narrative
{
    /// <summary>
    /// The demo's (Act 1) spoken lines: English voice text and Turkish subtitles. Sequences in phase 6b–6e use
    /// these. The twist (30-minute game): every "Mia" call is the clone, even the first; the clone catches on syllables
    /// (Glitch) and later says "Daddy"; the real Mia, heard only at the end, sounds half asleep and says "Dad".
    /// </summary>
    public static class DemoLines
    {
        static Line L(Speaker s, string en, string tr) => new Line(s, en, tr);

        /// <summary>Cold open, in the ambulance at 18:52 (8a: cut to about 45 seconds).</summary>
        public static Line[] ColdOpenRadio => new[]
        {
            L(Speaker.Dispatch, "Medic Fourteen, Dispatch. Lockdown in eight minutes. All units return to station.",
                                "Sağlık Ekibi On Dört, burası Merkez. Kapanmaya sekiz dakika. Tüm ekipler istasyona dönsün."),
            L(Speaker.Daniel, "Dispatch, Fourteen. Coming in on Harlow.",
                              "Merkez, burası On Dört. Harlow'dan geliyoruz."),
        };

        /// <summary>
        /// Rachel's voicemail, dismissed unheard. It says Mia is with her tonight: Daniel only plays it in the gym, at
        /// the end (the twist's proof).
        /// </summary>
        public static Line[] RachelIgnored => new[]
        {
            L(Speaker.Luis, "Your phone. Rachel.", "Telefonun. Rachel."),
            L(Speaker.Daniel, "Not now, Rach.", "Sonra, Rach."),
        };

        /// <summary>
        /// "Mia" calls. It is already the clone, though nothing says so yet: the voice is nearly perfect and only catches
        /// on a syllable once. Then the line dies.
        /// </summary>
        public static Line[] MiaCall => new[]
        {
            L(Speaker.Mia, "Dad? Dad, there's someone at the door.", "Baba? Baba, kapıda biri var."),
            L(Speaker.Mia, "They keep knocking... They're smiling, Dad. They're all smiling.",
                           "Durmadan çalıyorlar... Gülümsüyorlar baba. Hepsi gülümsüyor.").Glitch(0.25f),
            L(Speaker.Daniel, "Mia, listen to me. Lock your door, get in the closet, don't make a sound. I'm coming.",
                              "Mia, beni dinle. Kapını kilitle, dolaba gir, ses çıkarma. Geliyorum."),
            L(Speaker.Mia, "Dad, they're—", "Baba, onlar—").Glitch(1f).Then(0f),
        };

        public static Line[] CallDropped => new[]
        {
            L(Speaker.Daniel, "Mia? Mia!", "Mia? Mia!").Then(0.2f),
            L(Speaker.Luis, "Hold on. I'm getting you there.", "Tutun. Seni oraya götürüyorum."),
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
            L(Speaker.DanielThought, "Masks. Standing there. Filming us like it's a show.", "Maskeler. Orada dikilmiş... bizi bir gösteriymiş gibi çekiyorlar."),
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

        public static Line WrongWay => L(Speaker.DanielThought, "No. Not north. Home is south, past the gas station.", "Hayır. Kuzey değil. Ev güneyde, benzinliğin ötesinde.");
        public static Line StayWithLuis => L(Speaker.DanielThought, "Not like this. He's still talking to me. I don't walk away from my partner.", "Böyle olmaz. Hâlâ benimle konuşuyor. Ortağımı arkamda bırakıp gitmem.");

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
        public static Line LuisAfterShots => L(Speaker.DanielThought, "Two shots. Back at the wreck... Luis. God, no.", "İki el ateş. Kaza yerinden... Luis. Tanrım, hayır.");

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

        /// <summary>Reinforcements once the shooting starts (owner: the station fight in waves).</summary>
        public static Line WaveSouth => L(Speaker.Looter, "Over the wall! They're hitting our guys at the pumps!", "Duvarın üstünden! Pompalarda bizimkilere giriyorlar!");
        public static Line WaveFlank => L(Speaker.Looter, "Round the back! Box him in!", "Arkadan dolaşın! Sıkıştırın şunu!");

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

        // ------------------------------------------------------------------ 8b: Daniel's house

        /// <summary>Past the gas station's back gate: "Mia" again (the clone). The house is right across the alley.</summary>
        public static Line[] HouseCall => new[]
        {
            L(Speaker.Mia, "Dad? I'm in the closet. They came inside... I can hear them on the stairs.",
                           "Baba? Dolaptayım. İçeri girdiler... Merdivende seslerini duyuyorum.").Glitch(0.2f),
            L(Speaker.Daniel, "Stay in there. Don't make a sound. I'm right outside.", "Orada kal. Ses çıkarma. Hemen dışarıdayım."),
            L(Speaker.Mia, "Please hurry, Dad.", "Lütfen acele et baba.").Glitch(0.5f),
        };

        public static Line HouseEnter => L(Speaker.Daniel, "Mia? Mia, it's Dad!", "Mia? Mia, benim, baban!");
        public static Line HouseSteps => L(Speaker.DanielThought, "Footsteps. Upstairs. Someone's in my house.", "Ayak sesleri. Yukarıda. Evimde biri var.");

        /// <summary>The phone in Mia's closet, looping her voice. Daniel says what it means (owner: make the story clear).</summary>
        public static Line[] ClosetPhone => new[]
        {
            L(Speaker.DanielThought, "Her voice... from a phone. On a loop.", "Onun sesi... bir telefondan. Durmadan tekrar ediyor."),
            L(Speaker.DanielThought, "It's a recording. They're using my daughter's voice to bait me.", "Bu bir kayıt. Kızımın sesini beni tuzağa çekmek için kullanıyorlar."),
            L(Speaker.DanielThought, "Then where is she?", "O zaman o nerede?"),
        };

        /// <summary>The unit photo on Mia's bed, Vance's face scratched out, and the note.</summary>
        public static Line[] UnitPhotoLook => new[]
        {
            L(Speaker.DanielThought, "Charlie Company. Twenty-nineteen.", "Charlie Bölüğü. İki bin on dokuz."),
            L(Speaker.DanielThought, "One face scratched out. I don't need to see it. Vance.", "Bir yüz kazınmış. Görmeme gerek yok. Vance."),
            L(Speaker.DanielThought, "I put you in Leavenworth. You should still be rotting there.", "Seni askerî cezaevine ben soktum. Hâlâ orada çürüyor olmalıydın."),
        };

        public static Line[] NoteLook => new[]
        {
            L(Speaker.DanielThought, "\"School. Come alone.\" In red marker. Her marker.", "\"Okul. Yalnız gel.\" Kırmızı kalemle. Onun kalemiyle."),
            L(Speaker.DanielThought, "Her school. He wants me there, and he wants me alone.", "Onun okulu. Beni orada istiyor. Yalnız."),
        };

        public static Line RaidSeen => L(Speaker.DanielThought, "High beams. That truck. They're back. Out the back, now.", "Uzun farlar. O kamyon. Geri geldiler. Arkadan çık, hemen.");

        public static Line[] RaidTaunts => new[]
        {
            L(Speaker.Smiler, "Danny! Did you find her? No?", "Danny! Buldun mu onu? Bulamadın mı?"),
            L(Speaker.Smiler, "Knock knock, Danny!", "Tak tak, Danny!"),
        };

        public static Line RaidWithdraw => L(Speaker.Smiler, "Not here, Danny! The show's at the school!", "Burada değil Danny! Gösteri okulda!");

        // ------------------------------------------------------------------ 8c: the subway

        /// <summary>
        /// Vance calls before Daniel goes down (owner: someone should say on the phone who is down there). The first
        /// time his voice is heard: he names the hunter, says he is blind, brainwashed, huge, and that bullets won't do.
        /// </summary>
        public static Line[] VanceCall => new[]
        {
            L(Speaker.Vance, "Alley's closed, Danny. The shortcut's under your feet.", "Sokak kapalı Danny. Kestirme yol ayaklarının altında."),
            L(Speaker.Daniel, "Vance. Where is she?", "Vance. Nerede o?"),
            L(Speaker.Vance, "Down there's Boone. We scrubbed his head clean. Big boy, does what he's told.",
                             "Aşağıda Boone var. Beynini tertemiz yıkadık. Koca adamdır, ne denirse onu yapar."),
            L(Speaker.Vance, "Can't see a thing. But he hears a heartbeat at ten paces, and bullets only make him angry.",
                             "Hiçbir şey göremez. Ama on adımdan kalp atışını duyar, kurşunlar da onu sadece kızdırır."),
            L(Speaker.Vance, "Walk softly, Marine. Don't run. He's faster than you.", "Sessiz yürü asker. Sakın koşma. O senden hızlı."),
        };

        public static Line MetroBlocked => L(Speaker.DanielThought, "The alley's on fire. The subway, then: Kessler Street station comes up in her schoolyard.",
                                                                    "Sokak yanıyor. O zaman metro: Kessler Caddesi istasyonu onun okul bahçesine çıkıyor.");
        public static Line MetroDark => L(Speaker.DanielThought, "No power down here. Not a sound.", "Aşağıda elektrik yok. Çıt yok.");

        /// <summary>The blind Smiler on the platform: he hears Daniel come in.</summary>
        public static Line BlindReveal => L(Speaker.Smiler, "Danny... I can't see you. But I can hear you breathing.",
                                                            "Danny... Seni göremiyorum. Ama nefesini duyabiliyorum.");
        public static Line BlindThought => L(Speaker.DanielThought, "Boone. Blindfolded, plated like a tank. He hunts by sound... so I make none.",
                                                                    "Boone. Gözleri bağlı, tank gibi zırhlı. Sese göre avlanıyor... o zaman hiç ses çıkarmayacağım.");

        /// <summary>When he catches a sound and comes for it.</summary>
        public static Line BlindHeard(int i)
        {
            switch (i % 3)
            {
                case 0: return L(Speaker.Smiler, "There you are...", "İşte oradasın...");
                case 1: return L(Speaker.Smiler, "I hear you, Danny.", "Seni duyuyorum, Danny.");
                default: return L(Speaker.Smiler, "Shhh... listen.", "Şşş... dinle.");
            }
        }

        public static Line GateRusted => L(Speaker.DanielThought, "Rusted shut. This is going to be loud.", "Paslanmış. Bu çok gürültülü olacak.");
        public static Line BlindChase => L(Speaker.Smiler, "THERE you are!", "İŞTE oradasın!");
        public static Line BlindGrille => L(Speaker.Smiler, "Run, Danny! The show's almost on!", "Koş Danny! Gösteri neredeyse başlıyor!");

        // ------------------------------------------------------------------ 8d: the party in the school yard

        public static Line PartyUp => L(Speaker.DanielThought, "Music. A party... in her schoolyard. They're all waiting for something.",
                                                               "Müzik. Bir parti... onun okul bahçesinde. Hepsi bir şey bekliyor.");
        public static Line MaskSeen => L(Speaker.DanielThought, "Out cold. His mask, his jacket... in a crowd of masks nobody looks twice.",
                                                                "Kendinden geçmiş. Maskesi, ceketi... maskeli bir kalabalıkta kimse ikinci kez bakmaz.");
        public static Line NoMask => L(Speaker.DanielThought, "Not like this. They'd know my face in a second.", "Böyle olmaz. Yüzümü bir saniyede tanırlar.");
        public static Line MaskOn => L(Speaker.DanielThought, "Walk slow. Don't stare at the grins. Get to the school door.",
                                                              "Yavaş yürü. Sırıtanlara bakma. Okulun kapısına ulaş.");

        /// <summary>The DJ on the PA between tracks.</summary>
        public static Line PartyHype(int i)
        {
            switch (i % 3)
            {
                case 0: return L(Speaker.Smiler, "Twenty minutes to showtime, Harlow!", "Gösteriye yirmi dakika, Harlow!");
                case 1: return L(Speaker.Smiler, "Who's ready to watch a hero break?", "Bir kahramanın kırılışını izlemeye kim hazır?");
                default: return L(Speaker.Smiler, "Make some noise for the Marine!", "Denizci için ses verin!");
            }
        }

        /// <summary>What the crowd says as Daniel walks past (it tells the player what tonight is).</summary>
        public static Line PartyTalk(int i)
        {
            switch (i % 6)
            {
                case 0: return L(Speaker.Looter, "Vance says the Marine walks in tonight. Live.", "Vance, denizci bu gece içeri girecek diyor. Canlı.");
                case 1: return L(Speaker.CivilianWoman, "They've been streaming him all night. Since the ambulance.", "Bütün gece onu yayınlıyorlar. Ambulanstan beri.");
                case 2: return L(Speaker.Looter, "He ratted Vance out, back in the war. Tonight he pays for it.", "Savaşta Vance'i ihbar etmiş. Bu gece bedelini ödeyecek.");
                case 3: return L(Speaker.CivilianWoman, "Ten bucks says he cracks before midnight.", "On dolarına iddiaya girerim, gece yarısından önce kırılır.");
                case 4: return L(Speaker.Looter, "Best Purge ever. Best. Purge. Ever.", "Gelmiş geçmiş en iyi Arınma. En. İyi. Arınma.");
                default: return L(Speaker.CivilianWoman, "Did you see what he did at the gas station?", "Benzinlikte ne yaptığını gördün mü?");
            }
        }

        /// <summary>A guard who has noticed something off about him.</summary>
        public static Line GuardWary(int i)
        {
            switch (i % 3)
            {
                case 0: return L(Speaker.Smiler, "Hey. You. ...Huh.", "Hey. Sen. ...Hıh.");
                case 1: return L(Speaker.Smiler, "Something wrong with you, pal?", "Bir sorunun mu var, dostum?");
                default: return L(Speaker.Smiler, "Easy there. Easy.", "Sakin. Sakin ol.");
            }
        }

        public static Line GuardCaught => L(Speaker.Smiler, "That's him! That's Reyes!", "Bu o! Bu Reyes!");

        /// <summary>The reveal: Vance on the PA, Daniel's own camera on the big screen.</summary>
        public static Line[] Reveal => new[]
        {
            L(Speaker.Vance, "Kill the music.", "Müziği kesin."),
            L(Speaker.Vance, "Harlow... look who came to the party.", "Harlow... bakın partiye kim gelmiş."),
            L(Speaker.Vance, "That's his camera you're watching. Wave, Danny.", "İzlediğiniz onun kamerası. El salla Danny."),
            L(Speaker.Vance, "Take off the mask, Marine. Show them who you are.", "Maskeni çıkar asker. Onlara kim olduğunu göster."),
        };
        public static Line RevealThought => L(Speaker.DanielThought, "The gun on the truck. Now.", "Kamyondaki silah. Hemen.");
        public static Line RevealGo => L(Speaker.Vance, "Get him.", "Yakalayın onu.");
        public static Line CaughtVance => L(Speaker.Vance, "Busted already, Danny? Get him!", "Hemen mi yakalandın Danny? Yakalayın onu!");

        public static Line GunThought => L(Speaker.DanielThought, "They brought this. Not me.", "Bunu onlar istedi. Ben değil.");
        public static Line TurretTaunt => L(Speaker.Vance, "Look at him go! Tell me again you're not a killer, Danny.",
                                                           "Şuna bakın! Bana bir daha katil olmadığını söyle Danny.");
        public static Line WaveTwo => L(Speaker.Vance, "Backstage, boys. Bring him down.", "Kulise çocuklar. İndirin onu.");
        public static Line TruckComing => L(Speaker.Vance, "You want a show? Here's the finale.", "Gösteri mi istiyorsun? İşte final.");
        public static Line TruckDown => L(Speaker.DanielThought, "The door. Go.", "Kapı. Git.");

        // ------------------------------------------------------------------ 8e: the gym

        public static Line GymInvite => L(Speaker.Vance, "Doors are open, Danny. Come say hello.", "Kapılar açık Danny. Gel de bir merhaba de.");
        public static Line GymEnter => L(Speaker.DanielThought, "Her gym. They turned it into a studio.", "Onun spor salonu. Stüdyoya çevirmişler.");

        /// <summary>Vance, face to face. Between the second and third line he takes off his mask.</summary>
        public static Line[] GymTalk => new[]
        {
            L(Speaker.Vance, "Welcome to the show, Marine.", "Gösteriye hoş geldin asker."),
            L(Speaker.Vance, "You remember this face?", "Bu yüzü hatırlıyor musun?"),
            L(Speaker.Daniel, "Where's my daughter, Eli?", "Kızım nerede Eli?"),
            L(Speaker.Vance, "You reported me. Four years in Leavenworth for doing what they sent us there to do.",
                             "Beni ihbar ettin. Bizi oraya ne için gönderdilerse onu yaptım diye Leavenworth'te dört yıl."),
            L(Speaker.Vance, "Tonight everybody sees what you really are. Same as me. Just with a camera on your chest.",
                             "Bu gece herkes senin aslında ne olduğunu görüyor. Benim gibi. Sadece göğsünde bir kamerayla."),
            L(Speaker.Daniel, "Where is she?!", "Nerede o?!"),
            // "Before the siren" is the confession Hale's case rests on: the night is legal, the hour before it isn't.
            L(Speaker.Vance, "Mia was never here, Danny. Not at your house, not on the phone. That was us. From the first call, before the siren even sounded.",
                             "Mia hiç burada olmadı Danny. Ne evinde, ne telefonda. O bizdik. İlk aramadan beri, daha siren bile çalmadan."),
        };
        public static Line Voicemail => L(Speaker.DanielThought, "The voicemail. Rachel's. I never listened.", "Sesli mesaj. Rachel'ınki. Hiç dinlemedim.");
        public static Line RachelMessage => L(Speaker.Rachel, "Dan, it's me. I picked Mia up early, she's with me tonight. Call me when you can.",
                                                              "Dan, benim. Mia'yı erken aldım, bu gece benimle. Fırsatın olunca ara beni.");
        public static Line[] RealMia => new[]
        {
            L(Speaker.Mia, "Dad? ...What time is it?", "Baba? ...Saat kaç?"),
            L(Speaker.Daniel, "Late, baby. Go back to sleep. I love you.", "Geç oldu bebeğim. Uyumaya devam et. Seni seviyorum."),
            L(Speaker.Mia, "Love you too, Dad.", "Ben de seni seviyorum baba."),
        };
        public static Line DoIt => L(Speaker.Vance, "Do it, Danny. Everyone's watching.", "Yap şunu Danny. Herkes izliyor.");
        public static Line FiredCheer => L(Speaker.Smiler, "He did it! He actually did it!", "Yaptı! Gerçekten yaptı!");
        public static Line FiredThought => L(Speaker.DanielThought, "That's what they wanted. And I gave it to them.", "İstedikleri buydu. Ben de verdim.");
        public static Line[] Lowered => new[]
        {
            L(Speaker.Daniel, "No. You wanted them to see who I am.", "Hayır. Kim olduğumu görmelerini istedin."),
            L(Speaker.Daniel, "This camera's been running all night, Eli. Every mask. Every face. Every name.", "Bu kamera bütün gece çalıştı Eli. Her maske. Her yüz. Her isim."),
            L(Speaker.Vance, "...Turn it off.", "...Kapat onu."),
            L(Speaker.Daniel, "Not a chance.", "Asla."),
        };

        public static Line HaleEnd(bool fired) => fired
            ? L(Speaker.Hale, "He pulled the trigger. Legal. He'll carry it anyway.", "Tetiği çekti. Yasal. Yine de ömür boyu taşıyacak.")
            : L(Speaker.Hale, "He lowered the gun. The tape did the rest.", "Silahı indirdi. Gerisini kaset yaptı.");

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

        // ------------------------------------------------------------------ sequence 5: the school door

        /// <summary>From the phone on the tripod in front of the door: still "her" voice, still "Daddy".</summary>
        public static Line SchoolDoorMia => L(Speaker.MiaClone, "Daddy? I'm inside. Look at the door, Daddy.", "Babacığım? İçerideyim. Kapıya bak babacığım.").Glitch(0.6f);
        public static Line SchoolDoorDaniel => L(Speaker.Daniel, "Mia?!", "Mia?!");

        public static Line[] PhotoLook => new[]
        {
            L(Speaker.DanielThought, "That's me. Parris Island, twenty-sixteen.", "Bu benim. Parris Island, iki bin on altı."),
            L(Speaker.DanielThought, "The empty spot on the living-room wall. They took it off my wall to put it here.", "Salon duvarındaki o boşluk. Bunu buraya asmak için duvarımdan sökmüşler."),
        };

        public static Line[] StreamTaunt => new[]
        {
            L(Speaker.Smiler, "Smile, Danny. You're live.", "Gülümse, Danny. Canlı yayındasın."),
            L(Speaker.Smiler, "Forty thousand people want to see what a Marine does to get his little girl back.",
                              "Kırk bin kişi bir denizcinin kızını geri almak için ne yapacağını görmek istiyor."),
        };

        public static Line StreamLuisDead => L(Speaker.Smiler, "They already loved the part where you left Luis.", "Luis'i bırakıp gittiğin kısma zaten bayıldılar.");
        public static Line StreamLuisRadio => L(Speaker.LuisRadio, "Danny... you're on my phone. You're on every phone. Get out of the light!",
                                                "Danny... telefonumdasın. Her telefonda sen varsın. Işıktan çık!");
        public static Line WhereIsShe => L(Speaker.Daniel, "Where is she?!", "Nerede o?!");
        public static Line RoundTwo => L(Speaker.Smiler, "Round two starts now.", "İkinci tur şimdi başlıyor.");
        public static Line LastDaddy => L(Speaker.MiaClone, "Daddy—", "Babacığım—").Glitch(1f).Then(0f);

        /// <summary>Over black, after the footage cuts: Detective Hale, the morning after.</summary>
        public static Line HaleNote => L(Speaker.Hale, "Three hours. Gone. Where were you, Mister Reyes?", "Üç saat. Kayıp. Neredeydiniz, Bay Reyes?");
    }
}
