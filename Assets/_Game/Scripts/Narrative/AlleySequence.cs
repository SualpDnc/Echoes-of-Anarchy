using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Demo sequence 4, the alleys. Past the gas station's back gate the phone rings: "Mia" (the clone) calls Daniel to
    /// the school; he stands still for the call. The moment he hangs up, six Smilers come through the gate behind him and
    /// run him down the north alley, shooting past his head: they are herding, not killing, until he hurts one. The way
    /// out is the half-open shutter of a garage that only a crouching man fits under; they can't follow, and they don't
    /// seem to mind ("See you at school"). In the back alley, a Smiler in the lit school yard lights a firework that
    /// bursts into a grin, and runs into the school. The yard gate ends the sequence.
    /// </summary>
    public class AlleySequence : MonoBehaviour
    {
        public bool Done { get; private set; }

        // Six of them, all behind him: in the lot behind the store and along its dark north corridor.
        static readonly Vector3[] HunterSpawns =
        {
            new Vector3(-40.5f, DistrictGenerator.Curb, -70f),
            new Vector3(-40.5f, DistrictGenerator.Curb, -67f),
            new Vector3(-40.6f, DistrictGenerator.Curb, -77.5f),
            new Vector3(-40.6f, DistrictGenerator.Curb, -83f),
            new Vector3(-32f, DistrictGenerator.Curb, -63.6f),
            new Vector3(-27f, DistrictGenerator.Curb, -63.6f),
        };
        static readonly bool[] HunterBats = { false, true, false, false, true, false };

        readonly List<NPCBrain> hunters = new List<NPCBrain>();
        GameObject player;
        bool talking;

        /// <summary>Places the pickups (after the NavMesh has baked).</summary>
        public void Setup()
        {
            // A loose magazine on a crate by the fire in the dead-end pocket: exploring pays.
            var crate = new GameObject("AmmoCrate", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            crate.transform.position = new Vector3(AlleyBuilder.NorthAlleyX0 + 2.6f, DistrictGenerator.Curb + 0.3f, AlleyBuilder.NorthAlleySouthZ + 1.2f);
            crate.GetComponent<MeshFilter>().sharedMesh = PrimitiveMeshes.Get(PrimitiveType.Cube);
            crate.GetComponent<MeshRenderer>().sharedMaterial = RuntimeMaterials.Lit(new Color(0.32f, 0.24f, 0.16f), 0.2f);
            crate.transform.localScale = new Vector3(0.7f, 0.6f, 0.5f);
            var mag = new GameObject("Magazine", typeof(BoxCollider));
            mag.transform.position = crate.transform.position + new Vector3(0f, 0.32f, 0f);
            mag.transform.rotation = Quaternion.Euler(0f, 35f, 80f);
            mag.GetComponent<BoxCollider>().size = new Vector3(0.12f, 0.06f, 0.2f);
            PrimitiveMeshes.Part(mag.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.025f, 0.12f, 0.035f),
                                 RuntimeMaterials.Lit(new Color(0.05f, 0.05f, 0.055f), 0.5f, 0.6f), null, false);
            mag.AddComponent<AmmoPickup>().rounds = 7;
        }

        public IEnumerator Play(GasStationSequence station)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            var hud = BodycamHUD.Instance;
            var dd = DialogueDirector.Instance;
            bool radio = StoryState.HasLuisRadio && StoryState.LuisAlive;

            // Out of the back gate, a few steps into the alley, once Luis (if he's on the radio) has had his say.
            while (!InGateAlley(-47f)) yield return null;
            while (station.Talking) yield return null;

            // "Daddy, come to the school." Daniel stops for it (owner: walking on during the call left the Smilers far
            // behind); he can still look around. The Smilers come the moment he hangs up.
            var input = player.GetComponent<Player.EchoesInput>();
            if (input != null) input.LookOnly = true;
            talking = true;
            if (dd != null)
            {
                yield return dd.Call("MIA", true, DemoLines.SchoolCall);
                yield return dd.Say(DemoLines.AfterSchoolCall);
            }
            talking = false;
            if (input != null) input.LookOnly = false;
            if (hud != null) hud.SetObjective("Get to the school - through the alleys");
            yield return Chase(radio);

            // Through: a Smiler is already standing in the lit yard, in line with the back alley, with a firework.
            StartCoroutine(FireworkScene());
            if (radio)
            {
                yield return new WaitForSeconds(6f);
                yield return SpeakRadio(DemoLines.SchoolPowerRadio);
            }

            // Through the yard gate.
            while (player.transform.position.z < AlleyBuilder.YardZ0 + 1.5f || player.transform.position.x > AlleyBuilder.BackAlleyX1 + 4f) yield return null;
            Done = true;
            if (hud != null) hud.SetObjective("Find Mia - the school");
        }

        bool InGateAlley(float pastX)
        {
            Vector3 p = player.transform.position;
            return p.x < pastX && p.x > AlleyBuilder.NorthAlleyX0 && p.z > AlleyBuilder.GateAlleyZ0 - 1f && p.z < AlleyBuilder.GateAlleyZ1 + 1f;
        }

        // ------------------------------------------------------------------ the chase

        IEnumerator Chase(bool radio)
        {
            var hud = BodycamHUD.Instance;
            var rng = new System.Random(808);
            for (int i = 0; i < HunterSpawns.Length; i++)
            {
                var b = NPCSpawner.Create(CharacterLook.Random(Faction.Smiler, rng), rng, HunterSpawns[i], 270f, HunterBats[i] ? NPCWeapon.Bat : NPCWeapon.Pistol, false, 0f);
                if (b == null) continue;
                b.hunting = true;
                hunters.Add(b);
            }
            if (hunters.Count == 0) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.6f);
            if (hud != null)
            {
                hud.SetObjective("Run - north, under the lit garage shutter on the left");
                hud.ShowHint("They're behind you! Sprint (Shift) NORTH up the alley. The lit shutter on the LEFT: crouch (C) and crawl under it.", 8f);
            }
            yield return new WaitForSeconds(0.8f);
            // They say it out loud: this is a show, not an execution (owner asked for it to be clear).
            var start = DemoLines.ChaseStart;
            StartCoroutine(Speak(start[0].At(hunters[0].transform), start[1].At(hunters[0].transform), start[2].At(hunters[hunters.Count - 1].transform)));
            float warnAt = Time.time + 9f;
            if (!StoryState.LuisAlive) StartCoroutine(Speak(DemoLines.ChaseLuisTaunt.At(hunters[0].transform)));
            else if (radio) StartCoroutine(Speak(DemoLines.ChaseRadio));

            // Until he is through the garage (or they are all down). Near the shutter, at the fence or in the dead end,
            // Daniel and the hint say where to go.
            float nextShutterHint = 0f;
            bool saidFence = false, saidDeadEnd = false, warned = false;
            while (!Escaped() && AliveHunters() > 0)
            {
                Vector3 p = player.transform.position;
                if (!warned && Time.time > warnAt && !hunters.Exists(h => h != null && h.HuntProvoked))
                {
                    warned = true;
                    var n = Nearest(p);
                    if (n != null) StartCoroutine(Speak(DemoLines.ChaseWarningShots.At(n.transform)));
                }
                if (Time.time > nextShutterHint && Vector3.Distance(Flat(p), Flat(AlleyBuilder.ShutterPoint)) < 14f)
                {
                    nextShutterHint = Time.time + 10f;
                    if (hud != null) hud.ShowHint("The shutter! Crouch (C) and go under it.", 5f);
                }
                bool inNorthAlley = p.x < AlleyBuilder.NorthAlleyX1 && p.x > AlleyBuilder.NorthAlleyX0;
                if (!saidFence && inNorthAlley && p.z > AlleyBuilder.NorthFenceZ - 4f)
                {
                    saidFence = true;
                    StartCoroutine(Speak(DemoLines.ChaseFence));
                    if (hud != null) hud.ShowHint("Fenced off. Back to the lit shutter (now on your RIGHT), crouch (C) under it.", 6f);
                }
                if (!saidDeadEnd && inNorthAlley && p.z < AlleyBuilder.GateAlleyZ0 - 3f)
                {
                    saidDeadEnd = true;
                    StartCoroutine(Speak(DemoLines.ChaseDeadEnd));
                    if (hud != null) hud.ShowHint("Dead end. Go NORTH up the alley, to the lit shutter.", 6f);
                }
                yield return null;
            }
            if (hud != null) hud.SetObjective("Get to the school - through the alleys");
            if (AliveHunters() > 0) StartCoroutine(AtTheShutter());
        }

        /// <summary>They pile up at the shutter, bang on it, laugh, and go: they know where he's headed.</summary>
        IEnumerator AtTheShutter()
        {
            yield return new WaitForSeconds(1.2f);
            var am = AudioManager.Instance;
            Vector3 shutter = AlleyBuilder.ShutterPoint + new Vector3(0.1f, 1.8f, 0f);
            for (int i = 0; i < 3 && am != null; i++)
            {
                am.PlayAt(SoundId.ImpactMetal, shutter, 1f, Random.Range(0.7f, 0.85f), 2f, 60f);
                yield return new WaitForSeconds(Random.Range(0.35f, 0.6f));
            }
            var near = Nearest(AlleyBuilder.ShutterPoint);
            if (near != null) yield return Speak(DemoLines.ChaseLost[1].At(near.transform));
            foreach (var h in hunters)
                if (h != null && h.State != NPCState.Dead) StartCoroutine(Withdraw(h));
        }

        bool Escaped()
        {
            Vector3 p = player.transform.position;
            return p.x < AlleyBuilder.NorthAlleyX0 - 1f && p.z > AlleyBuilder.GarageZ0 - 2f && p.z < AlleyBuilder.YardZ0;
        }

        int AliveHunters()
        {
            int n = 0;
            foreach (var h in hunters) if (h != null && h.State != NPCState.Dead) n++;
            return n;
        }

        NPCBrain Nearest(Vector3 p)
        {
            NPCBrain best = null;
            float bd = float.MaxValue;
            foreach (var h in hunters)
            {
                if (h == null || h.State == NPCState.Dead) continue;
                float d = Vector3.Distance(h.transform.position, p);
                if (d < bd) { bd = d; best = h; }
            }
            return best;
        }

        /// <summary>A hunter walks back the way he came and is gone once nobody is looking.</summary>
        IEnumerator Withdraw(NPCBrain h)
        {
            h.hunting = false;
            h.enabled = false; // the brain lets go; the agent and animator keep working
            var agent = h.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var anim = h.GetComponent<ProcAnimator>();
            var hp = h.GetComponent<Health>();
            anim.pose = ProcAnimator.Pose.Normal;
            anim.aimTarget = null;
            anim.lookTarget = null;
            agent.updateRotation = true;
            agent.speed = 1.6f;
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(GasStationBuilder.GateCenter + new Vector3(3f, 0f, 0f)); }
            float unseen = 0f;
            while (h != null && !hp.IsDead)
            {
                var cam = Camera.main;
                Vector3 vp = cam != null ? cam.WorldToViewportPoint(h.transform.position + Vector3.up) : Vector3.zero;
                bool inView = vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
                unseen = inView ? 0f : unseen + Time.deltaTime;
                if (unseen > 3f) { Destroy(h.gameObject); yield break; }
                yield return null;
            }
        }

        // ------------------------------------------------------------------ the firework

        /// <summary>
        /// A Smiler in the floodlit yard, in line with the back alley, a firework at his feet. When Daniel sees him he
        /// kneels and lights it, and runs for the school as it goes up; it bursts into a grin over the yard. Owner: a
        /// firework instead of the wave, then he runs.
        /// </summary>
        IEnumerator FireworkScene()
        {
            var pos = new Vector3((AlleyBuilder.BackAlleyX0 + AlleyBuilder.BackAlleyX1) / 2f - 0.5f, DistrictGenerator.Curb, -13f);
            var w = SmilerWatcher.Spawn(pos, 180f, AlleyBuilder.SchoolDoor + new Vector3(0f, 0f, -0.8f), false, 909);
            var fw = Firework.Place(pos + new Vector3(0.3f, 0f, -0.9f), 0f);
            fw.height = 22f; // low enough to burst inside the view from the back alley

            // Until Daniel sees him, or walks right up without looking.
            while (w != null && !Sees(w.transform.position + Vector3.up * 1.5f) && Vector3.Distance(player.transform.position, w.transform.position) > 9f)
                yield return null;
            if (w == null) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.45f);
            yield return new WaitForSeconds(0.8f);
            if (w == null) yield break;

            // Down on one knee at the tube, the fuse catches, he gets up and runs.
            w.kneeling = true;
            yield return new WaitForSeconds(0.9f);
            fw.Ignite();
            yield return new WaitForSeconds(1.1f);
            if (w != null) { w.kneeling = false; w.Leave(true); StartCoroutine(IntoTheSchool(w)); }
            yield return new WaitForSeconds(2.4f); // fuse end + climb: the burst
            if (DialogueDirector.Instance != null) StartCoroutine(DialogueDirector.Instance.Say(DemoLines.FireworkSeen));
        }

        /// <summary>The runner goes in through the school door; it bangs shut behind him.</summary>
        IEnumerator IntoTheSchool(SmilerWatcher w)
        {
            while (w != null && Vector3.Distance(Flat(w.transform.position), Flat(AlleyBuilder.SchoolDoor)) > 1.6f) yield return null;
            if (w == null) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.ImpactHard, AlleyBuilder.SchoolDoor + Vector3.up, 1f, 0.6f, 3f, 60f);
            Destroy(w.gameObject);
        }

        bool Sees(Vector3 p)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(p);
            if (vp.z <= 0f || vp.z > 40f || vp.x < 0.2f || vp.x > 0.8f || vp.y < 0.15f || vp.y > 0.85f) return false;
            // Nothing in between but the watcher himself (his own colliders) and Daniel's.
            var hits = Physics.RaycastAll(cam.transform.position, (p - cam.transform.position).normalized, Vector3.Distance(cam.transform.position, p) - 0.2f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
                if (h.collider.GetComponentInParent<SmilerWatcher>() == null && !h.collider.transform.IsChildOf(player.transform)) return false;
            return true;
        }

        IEnumerator SpeakRadio(params Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Radio(lines);
            talking = false;
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
