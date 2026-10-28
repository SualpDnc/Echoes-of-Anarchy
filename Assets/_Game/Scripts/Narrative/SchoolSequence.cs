using System.Collections;
using UnityEngine;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Core;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Demo sequence 5, the cliffhanger at the school door. A phone on a tripod films the chained doors; "Mia" speaks
    /// from it ("Look at the door, Daddy"). Sprayed across the doors: "SMILE, DANNY. YOU'RE LIVE.", with Daniel's
    /// Marine photo from Mia's room taped between the lines. Looking at it, the Smilers' stream breaks into the bodycam
    /// picture (LIVE badge, viewer count, chat that knows what he did tonight), they taunt him, the floodlights die, the
    /// footage cuts. Over black: Detective Hale's note, then TO BE CONTINUED. The end of the demo.
    /// </summary>
    public class SchoolSequence : MonoBehaviour
    {
        public bool Done { get; private set; }
        /// <summary>Daniel is in the school yard (the last checkpoint).</summary>
        public bool Reached => playerReady;
        public bool CanLook => built && !looking && !Done && playerReady;

        SchoolDoorBuilder.Parts door;
        GameObject player;
        bool built, looking, playerReady, fired;
        System.DateTime choiceTime;

        /// <summary>Builds the door scene (after the NavMesh has baked; the door is on the facade, off the walkable yard).</summary>
        public void Setup()
        {
            if (built) return;
            built = true;
            door = SchoolDoorBuilder.Build(transform);
            door.door.AddComponent<DoorPhotoInteractable>().scene = this;
        }

        public IEnumerator Play(PartySequence party)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            // 8d: the yard is the party until the ram truck burns.
            while (!party.Done) yield return null;

            // Into the yard (up the back alley from the house, or started there).
            while (!InYard()) yield return null;
            playerReady = true;
            StartCoroutine(BlinkRec());

            // Close to the door, the phone speaks with her voice.
            while (Vector3.Distance(Flat(player.transform.position), Flat(AlleyBuilder.SchoolDoor)) > 9f && !looking) yield return null;
            if (!looking)
            {
                var dd = DialogueDirector.Instance;
                if (dd != null)
                {
                    yield return dd.Say(DemoLines.SchoolDoorMia.At(door.phone));
                    if (!looking) yield return dd.Say(DemoLines.SchoolDoorDaniel);
                }
                if (BodycamHUD.Instance != null && !looking) BodycamHUD.Instance.SetObjective("The door");
            }
        }

        bool InYard()
        {
            Vector3 p = player.transform.position;
            // At yard level: the subway's exit stairs run under the yard (8c), and they don't count.
            return p.z > AlleyBuilder.YardZ0 + 1f && p.x > AlleyBuilder.YardX0 && p.x < AlleyBuilder.YardX1 && p.y > DistrictGenerator.Curb - 0.5f;
        }

        IEnumerator BlinkRec()
        {
            while (door.recDot != null)
            {
                door.recDot.enabled = Mathf.Repeat(Time.time, 1f) < 0.6f;
                yield return null;
            }
        }

        public void LookAtPhoto()
        {
            if (!CanLook) return;
            looking = true;
            StartCoroutine(TheEnd());
        }

        IEnumerator TheEnd()
        {
            var hud = BodycamHUD.Instance;
            var dd = DialogueDirector.Instance;
            var am = AudioManager.Instance;
            var input = player.GetComponent<Player.EchoesInput>();
            if (input != null) input.LookOnly = true;
            Perception.PlayerConcealed = true;
            if (hud != null) { hud.SetObjective(""); hud.SetPrompt(""); }

            // The photo, held up to the lens.
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (hud != null) hud.ShowInspect(ProcTextures.MarinePhoto(), t / 0.4f); yield return null; }
            if (hud != null) hud.ShowInspect(ProcTextures.MarinePhoto(), 1f);
            if (dd != null) yield return dd.Conversation(DemoLines.PhotoLook);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (hud != null) hud.ShowInspect(null, 1f - t / 0.4f); yield return null; }
            if (hud != null) hud.ShowInspect(null, 0f);

            // The stream takes over the picture.
            if (hud != null) hud.Glitch(0.5f);
            if (am != null) am.Play2D(SoundId.TapeSkip, 0.35f);
            yield return new WaitForSeconds(0.3f);
            var stream = StreamOverlay.Instance;
            if (stream != null) stream.Show(true);
            if (am != null) am.Play2D(SoundId.TensionHit, 0.6f);
            ChatAbout(stream);

            if (dd != null)
            {
                yield return dd.Say(DemoLines.StreamTaunt[0].At(door.phone));
                if (stream != null) stream.Chat("grinmaxxer", "SMILE DANNY");
                yield return dd.Say(DemoLines.StreamTaunt[1].At(door.phone));
                if (!StoryState.LuisAlive)
                {
                    yield return dd.Say(DemoLines.StreamLuisDead.At(door.phone));
                    if (stream != null) stream.Chat("bloodmoon_tv", "he LEFT him lmaooo");
                }
                else if (StoryState.HasLuisRadio) yield return dd.Radio(DemoLines.StreamLuisRadio);
                yield return dd.Say(DemoLines.WhereIsShe);
                yield return dd.Say(DemoLines.GymInvite.At(door.phone));
            }

            // 8e: the chains drop, and inside is the gym (GymSequence): Vance, the truth, the choice.
            if (am != null) am.PlayAt(SoundId.ImpactMetal, AlleyBuilder.SchoolDoor + Vector3.up, 0.9f, 0.8f, 2f, 40f);
            if (stream != null) stream.Chat("chat_decides", "FINALE FINALE FINALE");
            yield return new WaitForSeconds(1f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { if (hud != null) hud.SetFade(t / 0.8f); yield return null; }
            if (stream != null) stream.Show(false);
            var gym = GetComponent<GymSequence>();
            if (gym == null) gym = gameObject.AddComponent<GymSequence>();
            yield return gym.Play();
            fired = gym.Fired;
            choiceTime = gym.ChoiceTime;
            if (hud != null) hud.Glitch(1.2f);
            if (am != null) am.Play2D(SoundId.TapeSkip, 0.7f);
            yield return new WaitForSeconds(0.35f);

            // Footage ends.
            Done = true;
            if (input != null) input.InputLocked = true;
            if (stream != null) stream.Show(false);
            if (hud != null) { hud.SetFade(1f); hud.ShowSubtitle("", 0f); hud.SetVitalsVisible(false); }
            AudioListener.pause = true; // the tape is silent from here: the city stops, only Hale's voice plays over the cards
            yield return new WaitForSeconds(2.5f);
            yield return EndCards();
        }

        /// <summary>The chat knows what the bodycam saw tonight.</summary>
        static void ChatAbout(StreamOverlay stream)
        {
            if (stream == null) return;
            bool executed = false, spared = false, helped = false;
            foreach (var e in Ledger.Entries)
            {
                if (e.act == LedgerAct.KilledSurrendered) executed = true;
                if (e.act == LedgerAct.SparedSurrendered) spared = true;
                if (e.act == LedgerAct.HelpedWounded) helped = true;
            }
            stream.Chat("purgedad", "hes HERE");
            if (executed) stream.Chat("cleanstreets", "he shot the kid on his knees. COLD");
            else if (spared) stream.Chat("xX_nightshift", "let the looter go?? soft");
            if (helped && StoryState.LuisAlive) stream.Chat("4am_dolly", "medic saved his buddy, cute");
            stream.Chat("lilsmile", "look at his FACE");
        }

        IEnumerator EndCards()
        {
            var hud = BodycamHUD.Instance;
            if (hud == null) yield break;
            var clock = GameClock.Instance;
            string stamp = clock != null ? clock.Now.ToString("HH:mm:ss") : "--:--:--";
            // The night runs in real time, so the moment Hale quotes is the bodycam's own (it used to say 23:41 while the tape read ~19:25).
            string at = choiceTime != System.DateTime.MinValue ? choiceTime.ToString("HH:mm") : stamp.Substring(0, 5);
            string note = GameSettings.Pick(
                "<size=24>EXHIBIT 14  ·  BWC-14  ·  D. REYES #4471</size>\n<size=22>RECORDING ENDS  " + stamp + "</size>\n\n\n" +
                // The night is legal, so the case is what came before the siren (the first call, the house) and the
                // price is the morning: the faces on the tape.
                (fired
                    ? "<i>At " + at + " Vance told him everyone was watching, and Reyes pulled the trigger.\n" +
                      "Legal, at " + at + ". Four million people watched it live. Not one of them looked away.\n\n" +
                      "But the first call came at 18:53. Seven minutes before the siren, they were already in his house.\n" +
                      "That part was never legal, and Vance said so on this tape. Vance can't answer for it. The others can.</i>\n\n"
                    : "<i>At " + at + " Vance told him everyone was watching, and Reyes didn't fire.\n" +
                      "He turned his camera on them. The night was legal. The morning isn't: every face in that gym\n" +
                      "is online now. Eleven of them have already lost their jobs. Two have lost their homes.\n\n" +
                      "And the first call came at 18:53. Seven minutes before the siren, they were already in his house.\n" +
                      "That part was never legal, and Vance said so on this tape. We picked him up at 07:02.</i>\n\n") +
                "<size=24>— Det. M. Hale, 22 March 2034, 07:58</size>",
                "<size=24>DELİL 14  ·  BWC-14  ·  D. REYES #4471</size>\n<size=22>KAYIT SONU  " + stamp + "</size>\n\n\n" +
                (fired
                    ? "<i>Saat " + at + ". Vance ona herkesin izlediğini söyledi ve Reyes tetiği çekti.\n" +
                      "O saatte bu yasaldı. Dört milyon kişi canlı izledi. Hiçbiri başını çevirmedi.\n\n" +
                      "Ama ilk arama 18:53'te geldi. Siren çalmadan yedi dakika önce evinin içindeydiler.\n" +
                      "O kısım hiç yasal olmadı ve Vance bunu bu kasette söyledi. Vance hesap veremez. Diğerleri verecek.</i>\n\n"
                    : "<i>Saat " + at + ". Vance ona herkesin izlediğini söyledi ve Reyes ateş etmedi.\n" +
                      "Kamerasını onlara çevirdi. Gece yasaldı. Sabah öyle değil: o salondaki her yüz\n" +
                      "artık internette. On biri şimdiden işini kaybetti. İkisi evini.\n\n" +
                      "Ve ilk arama 18:53'te geldi. Siren çalmadan yedi dakika önce evinin içindeydiler.\n" +
                      "O kısım hiç yasal olmadı ve Vance bunu bu kasette söyledi. Onu 07:02'de aldık.</i>\n\n") +
                "<size=24>— Dedektif M. Hale, 22 Mart 2034, 07:58</size>");

            yield return CardFade(note, 1.5f, true);
            var dd = DialogueDirector.Instance;
            if (dd != null) yield return dd.Say(DemoLines.HaleEnd(fired));
            yield return new WaitForSeconds(3.5f);
            yield return CardFade(note, 1.2f, false);
            yield return new WaitForSeconds(1f);

            string tbc = GameSettings.Pick("THE END", "SON");
            yield return CardFade("<size=46>" + tbc + "</size>", 2f, true);
            yield return new WaitForSeconds(4f);
            yield return CardFade("<size=46>" + tbc + "</size>", 1.5f, false);
            hud.SetCard("<size=30>ECHOES OF ANARCHY</size>\n<size=22>" + GameSettings.Pick("thank you for playing", "oynadığın için teşekkürler") + "</size>\n\n\n<size=18>" +
                        GameSettings.Pick("press any key", "bir tuşa bas") + "</size>", 0.75f);

            // 8f: any key goes back to the title screen with a fresh game.
            yield return new WaitForSeconds(1f);
            while (!AnyKeyToTitle()) yield return null;
            Checkpoint.NewGame();
            PauseMenu.TitleShown = false;
            AudioListener.pause = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>A key or a click, but not Esc (that opens the menu) and not while the menu is open.</summary>
        static bool AnyKeyToTitle()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return false;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame) return true;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        IEnumerator CardFade(string text, float seconds, bool fadeIn)
        {
            var hud = BodycamHUD.Instance;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float a = t / seconds;
                hud.SetCard(text, fadeIn ? a : 1f - a);
                yield return null;
            }
            hud.SetCard(fadeIn ? text : "", fadeIn ? 1f : 0f);
        }

        void OnDestroy()
        {
            if (Done) AudioListener.pause = false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
