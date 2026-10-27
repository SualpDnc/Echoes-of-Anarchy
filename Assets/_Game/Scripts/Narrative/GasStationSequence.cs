using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Demo sequence 3, the gas station. Three looters: one with a pistol at the store door, one inside working the
    /// shelves, a scared kid with a bat by the pumps. The clerk hides behind the counter. Daniel can sneak along the
    /// dark north corridor to the back gate, pull them off with a bottle, or fight. The kid gives up once the others
    /// are down: spare or execute (the Ledger keeps it). The clerk, if Daniel talks to her, points to the gate and
    /// hands him two bottles. Going through the back gate ends the sequence.
    /// </summary>
    public class GasStationSequence : MonoBehaviour
    {
        public bool Done { get; private set; }

        static readonly Vector3 Centre = new Vector3(-24f, 0f, -77f);

        readonly List<NPCBrain> looters = new List<NPCBrain>();
        readonly List<NPCBrain> reinforcements = new List<NPCBrain>();
        NPCBrain kid, clerk, lastSurrendered;
        GameObject player;
        bool talking, clerkTalked, built;

        public NPCBrain Clerk => clerk;
        public bool ClerkTalked => clerkTalked;
        public bool Talking => talking;

        /// <summary>Places the people (after the NavMesh has baked).</summary>
        public void Setup()
        {
            if (built) return;
            built = true;
            var rng = new System.Random(606);
            float y = DistrictGenerator.Curb;
            looters.Add(Looter(rng, new Vector3(-25.6f, y, -77.4f), 100f, NPCWeapon.Pistol, true, 0f, 0.5f));   // at the door, watching the street
            looters.Add(Looter(rng, new Vector3(-31f, y, -76.5f), 260f, NPCWeapon.Bat, true, 0f, 0.4f));      // inside, at the shelves
            kid = Looter(rng, new Vector3(-18.5f, y, -73f), 140f, NPCWeapon.Bat, false, 3f, 0.05f);            // the kid by the pumps
            looters.Add(kid);
            looters.RemoveAll(l => l == null);

            var look = CharacterLook.Pick(Faction.Civilian, rng);
            look.feminine = true;
            look.height = 0.95f;
            look.top = TopKind.Sweater;
            look.hair = HairKind.Long;
            look.Dress(rng);
            clerk = NPCSpawner.Create(look, rng, new Vector3(-37.2f, y, -78.5f), 90f, NPCWeapon.None, true, 1f);
            if (clerk != null)
            {
                clerk.startCowering = true;
                clerk.gameObject.AddComponent<ClerkInteractable>().scene = this;
                if (clerk.TryGetComponent(out Health h)) h.Died += _ => StoryState.ClerkAlive = false;
            }

            NPCBrain.AnySurrendered += OnSurrendered;
            Ledger.Recorded += OnLedger;
        }

        void OnDestroy()
        {
            NPCBrain.AnySurrendered -= OnSurrendered;
            Ledger.Recorded -= OnLedger;
        }

        static NPCBrain Looter(System.Random rng, Vector3 pos, float yaw, NPCWeapon weapon, bool stationary, float patrol, float courage)
        {
            var look = CharacterLook.Pick(Faction.Looter, rng);
            look.Dress(rng);
            var b = NPCSpawner.Create(look, rng, pos, yaw, weapon, stationary, patrol);
            if (b != null) b.courage = courage;
            return b;
        }

        // ------------------------------------------------------------------ the scene

        public IEnumerator Play()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            var hud = BodycamHUD.Instance;

            // Coming down Harlow toward the lit sign. Only once Daniel is on his own feet and on the street side of the
            // station: at load he still stands on the scene's default spawn (−2, −88), 25 m from here, and the briefing
            // used to fire there, during the intro, the ride and the alleys (owner bug).
            var input = player.GetComponent<Player.EchoesInput>();
            var rig = player.GetComponent<Player.CameraRig>();
            yield return null;
            while (!Approaching(input, rig) && !ThroughTheGate()) yield return null;
            if (hud != null && !ThroughTheGate()) // starting past it (startAt = Alleys) skips the briefing
            {
                hud.SetObjective("Get past the gas station - the back gate");
                hud.ShowHint("Looters at the gas station. Sneak round the back (C crouch, T light off), throw a bottle (G) to pull them away, or fight.", 9f);
            }
            StartCoroutine(Chatter());
            StartCoroutine(Waves());

            while (!ThroughTheGate())
            {
                ForceSurrender();
                yield return null;
            }
            Done = true;
            if (hud != null) hud.SetObjective("Get home to Mia");
            if (StoryState.HasLuisRadio && StoryState.LuisAlive)
            {
                yield return new WaitForSeconds(2f);
                while (talking) yield return null;
                talking = true;
                if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Radio(DemoLines.PastTheStationRadio);
                talking = false;
            }
        }

        /// <summary>
        /// A checkpoint restart behind the station: the looters are dealt with (gone), and the clerk lies where she
        /// died if Daniel killed her. Not a fromPlayer kill, so the Ledger (restored from the checkpoint) is untouched.
        /// </summary>
        public void RestoreAfterCheckpoint()
        {
            foreach (var l in looters) if (l != null) Destroy(l.gameObject);
            looters.Clear();
            foreach (var l in reinforcements) if (l != null) Destroy(l.gameObject);
            reinforcements.Clear();
            kid = null;
            if (!StoryState.ClerkAlive && clerk != null && clerk.TryGetComponent(out Health h) && !h.IsDead)
                h.ApplyDamage(new DamageInfo { amount = 999f, point = clerk.transform.position + Vector3.up, direction = Vector3.down, force = 20f, source = gameObject });
        }

        bool Approaching(Player.EchoesInput input, Player.CameraRig rig)
        {
            if (input != null && (input.LookOnly || input.InputLocked)) return false;
            if (rig != null && rig.IsMounted) return false;
            Vector3 p = player.transform.position;
            if (p.x < GasStationBuilder.StoreX0 || p.z < -100f || p.z > -40f) return false; // on Harlow or the forecourt
            return Vector3.Distance(Flat(p), Centre) < 34f;
        }

        bool ThroughTheGate()
        {
            Vector3 p = player.transform.position;
            return p.x < GasStationBuilder.BackX - 0.8f && p.z > GasStationBuilder.LotZMin - 6f && p.z < GasStationBuilder.LotZMax + 6f;
        }

        /// <summary>The kid breaks once the other two are down and Daniel is close: someone has to beg.</summary>
        void ForceSurrender()
        {
            if (kid == null || kid.State == NPCState.Dead || kid.State == NPCState.Surrender) return;
            int down = 0;
            foreach (var l in looters) if (l != kid && (l == null || l.State == NPCState.Dead)) down++;
            bool fighting = kid.State == NPCState.Attack || kid.State == NPCState.Search || kid.State == NPCState.Suspicious;
            if (down >= 2 && fighting && Vector3.Distance(player.transform.position, kid.transform.position) < 16f) kid.Surrender();
        }

        /// <summary>While none of them knows he is there, they talk: Daniel hears where they are.</summary>
        IEnumerator Chatter()
        {
            int i = 0;
            while (!Done)
            {
                yield return new WaitForSeconds(Random.Range(7f, 11f));
                bool unaware = true;
                var alive = new List<NPCBrain>();
                foreach (var l in looters)
                {
                    if (l == null || l.State == NPCState.Dead) continue;
                    alive.Add(l);
                    if (l.State != NPCState.Idle && l.State != NPCState.Patrol) unaware = false;
                }
                if (!unaware || alive.Count == 0 || talking) continue;
                var who = alive[i % alive.Count];
                if (Vector3.Distance(player.transform.position, who.transform.position) > 32f) continue;
                yield return Speak(DemoLines.LooterChatter(i++).At(who.transform));
            }
        }

        void OnSurrendered(NPCBrain b)
        {
            if (!looters.Contains(b)) return;
            lastSurrendered = b;
            StartCoroutine(Speak(DemoLines.StationSurrender.At(b.transform)));
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowHint("F  let him go   -   or don't", 6f);
        }

        void OnLedger(Ledger.Entry e)
        {
            if (e.act == LedgerAct.SparedSurrendered && lastSurrendered != null && looters.Contains(lastSurrendered))
                StartCoroutine(Speak(DemoLines.StationSpared.At(lastSurrendered.transform)));
        }

        // ------------------------------------------------------------------ reinforcements

        // Owner: the fight at the station should be harder and come in waves. Sneaking past starts none of it; the
        // first shot does. Wave 2 climbs the barricade on Harlow south of the lot; wave 3 flanks from the store's north
        // corridor and the back gate. Each spawn point is used only while Daniel can't see it.
        static readonly Vector3[] SouthWave = { new Vector3(-4f, 0f, -93.2f), new Vector3(2.5f, 0f, -93.2f), new Vector3(-8.5f, 0f, -93.2f) };
        static readonly NPCWeapon[] SouthArms = { NPCWeapon.Pistol, NPCWeapon.Bat, NPCWeapon.Pistol };
        static readonly Vector3[] FlankWave =
        {
            new Vector3(-32f, DistrictGenerator.Curb, -63.6f), new Vector3(-27f, DistrictGenerator.Curb, -63.6f), new Vector3(-40.5f, DistrictGenerator.Curb, -70f),
        };
        static readonly NPCWeapon[] FlankArms = { NPCWeapon.Pistol, NPCWeapon.Pistol, NPCWeapon.Bat };

        bool Fighting()
        {
            foreach (var l in looters)
                if (l == null || l.State == NPCState.Dead || l.State == NPCState.Attack) return true;
            return false;
        }

        IEnumerator Waves()
        {
            while (!Done && !Fighting()) yield return null;
            if (Done) yield break;
            var rng = new System.Random(707);
            yield return new WaitForSeconds(7f);
            if (Done) yield break;
            yield return Wave(SouthWave, SouthArms, rng, DemoLines.WaveSouth);

            // The next ones once this wave is nearly spent.
            float since = 0f;
            while (!Done && (AliveIn(reinforcements) > 1 || since < 6f)) { since += Time.deltaTime; yield return null; }
            if (Done) yield break;
            yield return new WaitForSeconds(3f);
            if (Done) yield break;
            yield return Wave(FlankWave, FlankArms, rng, DemoLines.WaveFlank);
        }

        IEnumerator Wave(Vector3[] spawns, NPCWeapon[] arms, System.Random rng, Line shout)
        {
            var hud = BodycamHUD.Instance;
            NPCBrain first = null;
            for (int i = 0; i < spawns.Length; i++)
            {
                // Out of sight, or far enough that a pop-in can't be noticed; give up waiting after a few seconds.
                for (float wait = 0f; wait < 4f && Visible(spawns[i]); wait += Time.deltaTime) yield return null;
                if (Done) yield break;
                var look = CharacterLook.Pick(Faction.Looter, rng);
                look.Dress(rng);
                var b = NPCSpawner.Create(look, rng, spawns[i], 0f, arms[i], false, 4f);
                if (b == null) continue;
                b.courage = 0.75f;   // reinforcements came for a fight: they don't break as easily
                b.skill = 0.6f;
                b.engageOnSpawn = true;
                reinforcements.Add(b);
                if (first == null) first = b;
                yield return new WaitForSeconds(0.6f);
            }
            if (first == null) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.5f);
            if (hud != null) hud.ShowHint("More of them coming!", 4f);
            StartCoroutine(Speak(shout.At(first.transform)));
        }

        bool Visible(Vector3 p)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 head = p + Vector3.up * 1.6f, from = cam.transform.position;
            if (Vector3.Distance(from, head) > 45f) return false;
            Vector3 vp = cam.WorldToViewportPoint(head);
            if (vp.z <= 0f || vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) return false;
            return !Physics.Linecast(from, head, ~0, QueryTriggerInteraction.Ignore);
        }

        static int AliveIn(List<NPCBrain> list)
        {
            int n = 0;
            foreach (var b in list) if (b != null && b.State != NPCState.Dead && b.State != NPCState.Surrender && b.State != NPCState.Flee) n++;
            return n;
        }

        // ------------------------------------------------------------------ the clerk

        bool ClerkDead => clerk == null || clerk.State == NPCState.Dead;

        public void TalkToClerk(GameObject who)
        {
            if (ClerkDead || talking) return;
            if (clerkTalked)
            {
                StartCoroutine(Speak(DemoLines.ClerkAgain.At(clerk.transform)));
                return;
            }
            clerkTalked = true;
            StartCoroutine(ClerkScene(who));
        }

        IEnumerator ClerkScene(GameObject who)
        {
            var lines = DemoLines.ClerkTalk;
            foreach (var l in lines) if (l.who == Speaker.CivilianWoman) l.At(clerk.transform);
            yield return Speak(lines[0], lines[1], lines[2], lines[3]);
            if (ClerkDead) yield break; // shot mid-conversation: no bottles, no last line (DialogueDirector cuts her off)
            if (who.TryGetComponent(out ThrowController t))
            {
                t.bottles += 2;
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo($"BOTTLES {t.bottles}", 2f);
            }
            yield return Speak(lines[4]);
        }

        /// <summary>One voice at a time in this scene.</summary>
        IEnumerator Speak(params Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Conversation(lines);
            talking = false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
