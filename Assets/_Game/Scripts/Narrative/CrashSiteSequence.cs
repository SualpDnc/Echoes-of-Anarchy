using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Player;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// Demo sequence 2. Daniel comes round on the asphalt, ears ringing, and gets up hurt. Movement and med-kit hints;
    /// the jump bag spilled out of the back of the ambulance. Luis is pinned against the wreck with a bad leg: treat
    /// him (costs one of the two kits) or walk away. Both are written to the Ledger. Meanwhile two Smilers watch from
    /// the intersection, one filming, then walk off.
    /// </summary>
    [RequireComponent(typeof(ColdOpenSequence))]
    public class CrashSiteSequence : MonoBehaviour
    {
        [Tooltip("Walking this far from Luis without treating him counts as leaving him.")]
        public float abandonDistance = 28f;
        public float startHealth = 55f;

        public bool LuisTreated { get; private set; }
        public bool LuisAbandoned { get; private set; }
        public bool LuisDead => luisHealth != null && luisHealth.IsDead;
        public bool Treating { get; private set; }
        public bool Done { get; private set; }

        ColdOpenSequence site;
        GameObject player;
        CameraRig rig;
        EchoesInput input;
        PlayerHealth playerHealth;
        Health health;
        FPSController body;
        Health luisHealth;
        ProcAnimator luisAnim;
        readonly List<SmilerWatcher> watchers = new List<SmilerWatcher>();
        bool talking, metLuis, luisChoiceOffered, kitTaken;

        void Awake() => site = GetComponent<ColdOpenSequence>();

        public IEnumerator Play()
        {
            var hud = BodycamHUD.Instance;
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            rig = player.GetComponent<CameraRig>();
            input = player.GetComponent<EchoesInput>();
            playerHealth = player.GetComponent<PlayerHealth>();
            health = player.GetComponent<Health>();
            body = player.GetComponent<FPSController>();
            if (site.Luis != null)
            {
                luisHealth = site.Luis.GetComponent<Health>();
                luisAnim = site.Luis.GetComponent<ProcAnimator>();
                site.Luis.gameObject.AddComponent<LuisInteractable>().scene = this;
            }

            // Lying on his side in the road, out cold.
            if (hud != null) hud.SetFade(1f);
            rig.Unmount();
            input.LookOnly = false;
            input.InputLocked = true;
            body.Teleport(site.WakePoint, site.WakeYaw);
            rig.enabled = false;
            Transform pivot = rig.pivot, cam = rig.cam.transform;
            cam.localPosition = Vector3.zero;
            cam.localRotation = Quaternion.identity;
            Vector3 lyingPos = new Vector3(0.12f, 0.24f, 0.2f), standPos = new Vector3(0f, body.standHeight - rig.mountDrop, rig.forwardOffset);
            Quaternion lyingRot = Quaternion.Euler(-6f, 12f, 76f);
            pivot.localPosition = lyingPos;
            pivot.localRotation = lyingRot;

            SpillKits();
            SpawnWatchers();

            yield return new WaitForSeconds(2.2f);
            StartCoroutine(Unmuffle(14f));
            yield return ColdOpenSequence.Fade(1f, 0f, 2.8f);
            yield return Speak(Luis(DemoLines.WakeUp));

            // He gets up: roll, push up to a knee, stand.
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Land, 0.4f, 0.8f);
            for (float t = 0f; t < 2.3f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 2.3f);
                float kneel = Mathf.Sin(u * Mathf.PI) * 0.12f; // a pause on one knee halfway up
                pivot.localPosition = Vector3.Lerp(lyingPos, standPos, Mathf.Clamp01(u * 1.15f - kneel));
                pivot.localRotation = Quaternion.Slerp(lyingRot, Quaternion.identity, Mathf.Clamp01(u * 1.3f));
                yield return null;
            }
            pivot.localPosition = standPos;
            pivot.localRotation = Quaternion.identity;
            rig.SetYawPitch(player.transform.eulerAngles.y, 0f);
            rig.enabled = true;
            input.InputLocked = false;
            Perception.PlayerConcealed = false;
            health.SetHealth(startHealth);
            playerHealth.medKits = 0; // his own kit went with the jump bag
            // A full belt (owner: more rounds for the fight at the gas station): a loaded gun and five spare magazines.
            if (player.TryGetComponent(out WeaponPistol gun)) { gun.ammoInMag = gun.magSize; gun.reserve = gun.magSize * 5; }
            if (hud != null)
            {
                hud.SetVitalsVisible(true);
                hud.ShowHint("WASD move   SHIFT run   C crouch   T flashlight", 7f);
                hud.SetObjective("Get home to Mia - south on Harlow");
            }

            StartCoroutine(Watchers());
            StartCoroutine(LightTheWay());
            StartCoroutine(WrongWay());
            StartCoroutine(StayNearLuis());
            yield return LuisScene();
            Done = true;
        }

        // ------------------------------------------------------------------ Luis

        IEnumerator LuisScene()
        {
            var hud = BodycamHUD.Instance;
            if (site.Luis == null) yield break;
            Transform luis = site.Luis.transform;
            float nextCall = Time.time + 4f, farFor = 0f;
            int calls = 0;
            while (!LuisTreated && !LuisDead)
            {
                float dist = Vector3.Distance(player.transform.position, luis.position);
                if (luisAnim != null) luisAnim.lookTarget = dist < 14f && Camera.main != null ? Camera.main.transform.position : (Vector3?)null;

                // Leaving him: a call after Daniel, and it goes on the record.
                farFor = dist > abandonDistance ? farFor + Time.deltaTime : 0f;
                if (farFor > 1.5f)
                {
                    LuisAbandoned = true;
                    Ledger.Record(LedgerAct.AbandonedWounded, "Luis");
                    yield return Speak(Luis(DemoLines.LuisAbandoned));
                    StartCoroutine(Execution()); // the Smilers come back for him once Daniel is gone
                    yield break;
                }

                if (!metLuis && dist < 3.4f)
                {
                    metLuis = true;
                    yield return Speak(Luis(DemoLines.LuisHurt));
                    StartCoroutine(PointSouth(4.5f));
                    yield return Speak(Luis(DemoLines.LuisDirections));
                    if (!kitTaken && hud != null) hud.ShowHint("Search the back of the ambulance for med kits", 7f);
                    if (!LuisTreated)
                    {
                        yield return Speak(Luis(new[] { DemoLines.LuisChoice }));
                        luisChoiceOffered = true;
                    }
                }
                else if (!metLuis && Time.time > nextCall && calls < 4)
                {
                    nextCall = Time.time + 9f;
                    yield return Speak(Luis(new[] { DemoLines.LuisCalling(calls++) }));
                }
                yield return null;
            }
        }

        /// <summary>
        /// Until Luis has said his piece, Daniel can't walk off (owner: you could leave before he finished): an invisible
        /// tether round Luis, with Daniel saying why. The wake point and the spilled kits lie well inside it.
        /// </summary>
        IEnumerator StayNearLuis()
        {
            const float radius = 11f;
            var hud = BodycamHUD.Instance;
            float nextLine = 0f;
            while (site.Luis != null && !luisChoiceOffered && !LuisTreated && !LuisDead)
            {
                Vector3 c = site.Luis.transform.position, p = player.transform.position;
                Vector3 off = new Vector3(p.x - c.x, 0f, p.z - c.z);
                float dist = off.magnitude;
                if (dist > radius && body.enabled && body.Controller.enabled)
                {
                    body.Controller.Move(-off / dist * (dist - radius));
                    if (Time.time > nextLine)
                    {
                        nextLine = Time.time + 8f;
                        if (hud != null) hud.ShowHint("Luis is hurt. Go to him first.", 4f);
                        if (!talking) StartCoroutine(Speak(new[] { DemoLines.StayWithLuis }));
                    }
                }
                yield return null;
            }
        }

        public void InteractWithLuis(GameObject who)
        {
            if (LuisTreated)
            {
                if (!talking) StartCoroutine(Speak(Luis(new[] { DemoLines.LuisGo })));
                return;
            }
            if (playerHealth.medKits <= 0)
            {
                var hud = BodycamHUD.Instance;
                if (hud != null) hud.ShowHint("You need a med kit. The jump bag spilled out of the back of the ambulance.", 5f);
                return;
            }
            StartCoroutine(Treat());
        }

        IEnumerator Treat()
        {
            Treating = true;
            playerHealth.medKits--;
            input.InputLocked = true;
            var am = AudioManager.Instance;

            // Turn to him and kneel a little: the camera drops and looks down at the leg.
            Vector3 to = site.Luis.transform.position + Vector3.up * 0.3f - rig.cam.transform.position;
            float yaw0 = rig.Yaw, pitch0 = rig.Pitch;
            float yaw1 = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float pitch1 = Mathf.Clamp(-Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg, -20f, 55f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.6f);
                rig.SetYawPitch(Mathf.LerpAngle(yaw0, yaw1, u), Mathf.Lerp(pitch0, pitch1, u));
                yield return null;
            }
            if (am != null) am.Play2D(SoundId.Bandage, 0.7f);
            yield return Speak(DemoLines.LuisTreated);
            if (am != null) am.Play2D(SoundId.Bandage, 0.7f, 0.9f);
            yield return new WaitForSeconds(1.4f);
            if (am != null) am.Play2D(SoundId.Bandage, 0.5f, 1.1f);
            yield return new WaitForSeconds(0.8f);

            LuisTreated = true;
            Ledger.Record(LedgerAct.HelpedWounded, "Luis");
            input.InputLocked = false;
            Treating = false;
            yield return Speak(Luis(DemoLines.LuisThanks));

            // Consequence: Luis's handheld. He stays on the dispatch channel and warns Daniel of what is ahead.
            yield return Speak(Luis(DemoLines.LuisGivesRadio));
            StoryState.HasLuisRadio = true;
            if (am != null) am.Play2D(SoundId.RadioSquelch, 0.5f);
            var hud = BodycamHUD.Instance;
            if (hud != null) hud.ShowAmmo("RADIO  ·  MEDIC 14  CH 2", 3f);
            if (luisAnim != null) luisAnim.lookTarget = null;
            StartCoroutine(RadioWarning());
        }

        /// <summary>Luis on the radio once Daniel is well down Harlow: the looters at the gas station (6c).</summary>
        IEnumerator RadioWarning()
        {
            while (player.transform.position.z > -45f) yield return null;
            while (talking) yield return null;
            talking = true;
            var d = DialogueDirector.Instance;
            if (d != null) yield return d.Radio(DemoLines.LuisRadioWarning);
            talking = false;
        }

        /// <summary>
        /// Left behind: once Daniel is far off (or has not looked back for a while), the Smilers return. A scream,
        /// two shots, and Luis is dead; a grin is daubed on the wreck beside him. Treating him first cancels it.
        /// </summary>
        IEnumerator Execution()
        {
            float unseen = 0f;
            while (!LuisTreated && !LuisDead)
            {
                float dist = Vector3.Distance(player.transform.position, site.Luis.transform.position);
                unseen = InView(site.Luis.transform.position + Vector3.up * 0.6f) ? 0f : unseen + Time.deltaTime;
                if (dist > 45f || (dist > 20f && unseen > 6f)) break;
                yield return null;
            }
            if (LuisTreated || LuisDead) yield break;

            var am = AudioManager.Instance;
            Vector3 p = site.Luis.transform.position + Vector3.up * 0.8f;
            if (am != null) am.PlayAt(SoundId.Scream, p, 0.8f, 1.05f, 8f, 200f);
            yield return new WaitForSeconds(1.1f);
            for (int i = 0; i < 2; i++)
            {
                if (am != null) am.PlayAt(SoundId.GunPistolNear, p, 1f, Random.Range(0.95f, 1.03f), 6f, 300f);
                yield return new WaitForSeconds(0.45f);
            }
            if (luisHealth != null && !luisHealth.IsDead)
                luisHealth.ApplyDamage(new DamageInfo { amount = 999f, point = p, direction = Vector3.down, force = 120f, source = gameObject, part = BodyPart.Head });
            StoryState.LuisAlive = false;
            site.PaintGrinBesideLuis();
            yield return new WaitForSeconds(0.9f);
            yield return Speak(new[] { DemoLines.LuisAfterShots });
        }

        /// <summary>Luis points down Harlow while he gives the way.</summary>
        IEnumerator PointSouth(float seconds)
        {
            if (luisAnim == null || site.Luis == null) yield break;
            luisAnim.aimTarget = site.Luis.transform.position + Vector3.back * 25f + Vector3.up * 1.2f;
            yield return new WaitForSeconds(seconds);
            luisAnim.aimTarget = null;
        }

        /// <summary>The avenue lamps south of the crash come back on emergency power: a lit route toward the gas station.</summary>
        IEnumerator LightTheWay()
        {
            yield return new WaitForSeconds(4f);
            LightTheWayNow(0f, true);
        }

        void LightTheWayNow(float delay, bool onlyDark)
        {
            foreach (var lamp in StreetLight.All.ToArray())
            {
                Vector3 p = lamp.transform.position;
                if (p.z < -10f && Mathf.Abs(p.x) < 10f && (!onlyDark || !lamp.IsOn)) lamp.EmergencyPower(delay + Random.Range(0.2f, 2.5f), 0.65f);
            }
        }

        /// <summary>Luis's part is over: treated, or dead. Leaving him alive only counts once the Smilers have been back.</summary>
        public bool Resolved => LuisTreated || LuisDead;
        public bool Busy => talking || Treating;

        /// <summary>
        /// A checkpoint restart past the crash site: the scene is not played, it is put back as Daniel left it. Luis
        /// treated (he gave his radio), or dead by the wreck with the Smilers' grin beside him if they came back for him.
        /// The lamps down Harlow are lit again.
        /// </summary>
        public void RestoreAfterCheckpoint()
        {
            LightTheWayNow(1.2f, false);
            if (site.Luis == null) return;
            luisHealth = site.Luis.GetComponent<Health>();
            luisAnim = site.Luis.GetComponent<ProcAnimator>();
            if (luisAnim != null) luisAnim.lookTarget = null;
            if (StoryState.HasLuisRadio) LuisTreated = true;
            if (StoryState.LuisAlive || luisHealth == null || luisHealth.IsDead) return;
            bool executed = false;
            foreach (var e in Ledger.Entries) if (e.act == LedgerAct.AbandonedWounded) executed = true;
            foreach (var e in Ledger.Entries) if (e.act == LedgerAct.KilledCivilian && e.detail == "Luis") executed = false;
            luisHealth.ApplyDamage(new DamageInfo { amount = 999f, point = site.Luis.transform.position + Vector3.up, direction = Vector3.down, force = 40f, source = gameObject, part = BodyPart.Head });
            if (executed) site.PaintGrinBesideLuis();
        }

        /// <summary>Walking up to the barricade: Daniel turns himself round.</summary>
        IEnumerator WrongWay()
        {
            var hud = BodycamHUD.Instance;
            float next = 0f;
            for (int said = 0; said < 3;)
            {
                if (player.transform.position.z > ColdOpenSequence.BarricadeZ - 7f && Time.time > next)
                {
                    said++;
                    next = Time.time + 20f;
                    if (hud != null) hud.ShowHint("Home is south: follow Harlow Ave toward the lit gas station sign", 6f);
                    yield return Speak(new[] { DemoLines.WrongWay });
                }
                yield return null;
            }
        }

        bool InView(Vector3 point)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(point);
            return vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
        }

        Line[] Luis(Line[] lines)
        {
            foreach (var l in lines) if (l.who == Speaker.Luis && site.Luis != null) l.At(site.Luis.transform);
            return lines;
        }

        Line[] Luis(Line l) => Luis(new[] { l });

        /// <summary>One conversation at a time: the Luis scene, the Smilers and the treatment share the voice.</summary>
        IEnumerator Speak(Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            var d = DialogueDirector.Instance;
            if (d != null) yield return d.Conversation(lines);
            talking = false;
        }

        // ------------------------------------------------------------------ the jump bag

        void SpillKits()
        {
            foreach (var p in site.SpilledKitPoints())
            {
                var kit = MedKitPickup.Create(transform, p, Random.Range(0f, 360f));
                kit.Taken += OnKitTaken;
            }
        }

        void OnKitTaken()
        {
            var hud = BodycamHUD.Instance;
            if (hud == null) return;
            bool first = !kitTaken;
            kitTaken = true;
            if (!first) return;
            hud.ShowHint(LuisTreated || LuisAbandoned || LuisDead
                ? "H   dress your own wounds (uses a med kit)"
                : "H   dress your own wounds   ·   F on Luis to treat him", 7f);
        }

        // ------------------------------------------------------------------ the Smilers

        void SpawnWatchers()
        {
            Vector3 away = new Vector3(-34f, 0f, 31f); // off down the cross street, west
            watchers.Add(SmilerWatcher.Spawn(new Vector3(-3.5f, 0f, 24.6f), 180f, away, true, 9101));
            watchers.Add(SmilerWatcher.Spawn(new Vector3(-1.4f, 0f, 25.6f), 180f, away + Vector3.forward * 1.5f, false, 9102));
        }

        IEnumerator Watchers()
        {
            float seen = 0f, sinceMet = 0f;
            while (true)
            {
                watchers.RemoveAll(w => w == null);
                if (watchers.Count == 0) yield break;
                seen = CanSeeAny() ? seen + Time.deltaTime : 0f;
                if (metLuis) sinceMet += Time.deltaTime;
                // Spotted by Daniel, or Luis points them out a little after the talk.
                if (seen > 0.6f || (luisChoiceOffered && sinceMet > 6f)) break;
                yield return null;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.5f);
            bool luisNear = site.Luis != null && !LuisDead && !LuisAbandoned
                            && Vector3.Distance(player.transform.position, site.Luis.transform.position) < 16f;
            yield return Speak(luisNear ? Luis(DemoLines.SmilersSighted) : DemoLines.SmilersSightedAlone);
            yield return new WaitForSeconds(1.5f);
            foreach (var w in watchers) if (w != null) w.Leave(false);
        }

        bool CanSeeAny()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            foreach (var w in watchers)
            {
                if (w == null) continue;
                Vector3 head = w.transform.position + Vector3.up * 1.6f;
                Vector3 vp = cam.WorldToViewportPoint(head);
                if (vp.z <= 0f || vp.z > 80f || vp.x < 0.15f || vp.x > 0.85f || vp.y < 0.1f || vp.y > 0.9f) continue;
                Vector3 from = cam.transform.position, d = head - from;
                if (!Physics.Raycast(from, d.normalized, out RaycastHit hit, d.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider.transform.IsChildOf(w.transform)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ hearing comes back

        IEnumerator Unmuffle(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = t / seconds;
                ColdOpenSequence.Muffle(Mathf.Lerp(700f, 12500f, u * u));
                yield return null;
            }
            ColdOpenSequence.Muffle(12500f);
        }
    }
}
