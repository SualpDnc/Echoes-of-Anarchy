using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Player;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// Demo sequence 1. Daniel rides in the passenger seat of Medic 14 (he can look around the cab): radio traffic,
    /// Rachel's voicemail dismissed unheard, "Mia" calling (the clone) and the line dying, then the tape skips to
    /// 18:59:56 (about 45 seconds in all). The ambulance turns down Harlow Ave timed so the siren,
    /// the blackout wave and the ram truck land in order: the truck's high beams flare, it hits the front left
    /// (Luis's side), and the ambulance rolls onto its side. The footage cuts to black on the landing.
    /// Also owns the crash-site layout (final poses) that sequence 2 plays out in.
    /// </summary>
    public class ColdOpenSequence : MonoBehaviour
    {
        [Tooltip("Seconds after the grid drops that the truck hits (the nearest lamps have just died).")]
        public float impactAfterGridDown = 3.2f;

        /// <summary>The ambulance's origin at the moment of impact. (Declared before the routes: static initialisers run in order.)</summary>
        public static readonly Vector3 CrashPoint = new Vector3(-1.75f, 0f, -14f);

        // Cross street, eastbound, before the tape skip (around the intersection bonfire).
        static readonly Vector3[] CrossStreetRoute =
        {
            new Vector3(-95f, 0f, 28.25f), new Vector3(-20f, 0f, 28.25f), new Vector3(-8f, 0f, 27.2f), new Vector3(3f, 0f, 26.9f),
            new Vector3(14f, 0f, 27.4f), new Vector3(30f, 0f, 28.25f), new Vector3(92f, 0f, 28.25f)
        };
        // Harlow Ave, southbound lane, after the skip. Passes the burnt-out wreck at z 63.
        static readonly Vector3[] AvenueRoute =
        {
            new Vector3(-1.75f, 0f, 74f), new Vector3(-2.2f, 0f, 63f), new Vector3(-1.75f, 0f, 50f), new Vector3(-1.75f, 0f, 0f),
            CrashPoint, new Vector3(-1.75f, 0f, -30f)
        };
        // Where everything comes to rest. The ambulance lies on its right side, roof to the west, wheels to the east.
        static readonly Vector3 AmbulanceRest = new Vector3(-0.6f, 0f, -18f);
        const float AmbulanceRestYaw = 190f, AmbulanceRestRoll = -88f, LyingLift = 1.2f;
        static readonly Vector3 TruckRest = new Vector3(2.6f, 0f, -25.5f);
        const float TruckRestYaw = -20f;

        public VehicleRig Ambulance { get; private set; }
        public VehicleRig Truck { get; private set; }
        public HumanoidRig Luis { get; private set; }
        public bool Playing { get; private set; }
        public bool Crashed { get; private set; }

        ScriptedVehicle ambDrive, truckDrive;
        ProcAnimator luisAnim;
        FireFX.Materials fx;
        bool built;
        bool gridDown;
        float nextGlance, glanceUntil;
        Coroutine gridLine;

        GameObject player;
        CameraRig rig;
        EchoesInput input;

        // ------------------------------------------------------------------ setup

        public void Build(FireFX.Materials effects)
        {
            if (built) return;
            built = true;
            fx = effects;
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                rig = player.GetComponent<CameraRig>();
                input = player.GetComponent<EchoesInput>();
            }

            Ambulance = VehicleFactory.Ambulance(transform);
            ambDrive = Ambulance.gameObject.AddComponent<ScriptedVehicle>();
            ambDrive.carriesPlayer = true;
            ambDrive.SetPath(CrossStreetRoute, 0f);

            Truck = VehicleFactory.RamTruck(transform);
            truckDrive = Truck.gameObject.AddComponent<ScriptedVehicle>();
            truckDrive.engineSound = SoundId.EngineTruck;
            truckDrive.engineVolume = 0.85f;
            truckDrive.idlePitch = 0.8f;
            truckDrive.pitchPerSpeed = 0.05f;
            truckDrive.stopAtEnd = false;
            Truck.gameObject.SetActive(false);

            BuildLuis();
            PurgeClock.GridDown += OnGridDown;
        }

        void OnDestroy() => PurgeClock.GridDown -= OnGridDown;

        void OnGridDown() => gridDown = true;

        /// <summary>Luis Ortega, Daniel's partner: navy EMS uniform, blue nitrile gloves. Seated at the wheel.</summary>
        void BuildLuis()
        {
            var rng = new System.Random(1414);
            var look = CharacterLook.Pick(Faction.Civilian, rng);
            look.feminine = false;
            look.height = 1.0f;
            look.bulk = 1.07f;
            look.belly = 0.35f;
            look.top = TopKind.WorkJacket;
            look.pants = PantsKind.Cargo;
            look.shoes = ShoeKind.Boot;
            look.head = HeadKind.Face;
            look.hair = HairKind.Short;
            look.gloves = true;
            look.shortSleeves = false;
            look.closeUp = true; // he is half a metre from the camera for a minute
            look.definition = 1f;  // owner: sharper, more realistic lines on his face
            look.Dress(rng);
            // A mid skin tone, so his features read in the dim cab.
            look.skinTone = new Color(0.5f, 0.34f, 0.25f);
            look.skin = RuntimeMaterials.Fabric(look.skinTone, 0.38f, Fabric.Skin);
            look.lips = RuntimeMaterials.Fabric(new Color(0.4f, 0.19f, 0.15f), 0.45f, Fabric.Skin);
            look.topMat = RuntimeMaterials.Fabric(new Color(0.035f, 0.05f, 0.1f), 0.22f, Fabric.Cotton);
            look.pantsMat = RuntimeMaterials.Fabric(new Color(0.03f, 0.04f, 0.08f), 0.2f, Fabric.Cotton);
            look.gloveMat = RuntimeMaterials.Lit(new Color(0.12f, 0.17f, 0.5f), 0.55f);

            Luis = HumanoidBuilder.Build(Ambulance.body, look, rng, out var health);
            Luis.gameObject.name = "Luis";
            luisAnim = Luis.gameObject.AddComponent<ProcAnimator>();
            luisAnim.footIK = false;
            luisAnim.handTargetL = Ambulance.gripL; // hands on the wheel, turning with it
            luisAnim.handTargetR = Ambulance.gripR;
            luisAnim.pose = ProcAnimator.Pose.Seated;
            luisAnim.SnapToPose();
            // Pelvis on the driver's seat: the Seated pose lowers it 0.45 m from standing.
            Luis.transform.localPosition = Ambulance.driverSeat.localPosition - Vector3.up * (0.98f * look.height - 0.45f);
            Luis.transform.localRotation = Quaternion.identity;
            health.Died += info =>
            {
                if (info.fromPlayer) Ledger.Record(LedgerAct.KilledCivilian, "Luis");
                StoryState.LuisAlive = false; // whoever killed him: later scenes (and checkpoints) see a dead man
                luisAnim.enabled = false;
                Luis.EnableRagdoll(Vector3.zero, info);
            };
        }

        void Update()
        {
            if (!built || Crashed || luisAnim == null || Camera.main == null) return;
            // Eyes on the road, with a glance across at Daniel now and then.
            if (Time.time > nextGlance)
            {
                glanceUntil = Time.time + Random.Range(1.2f, 2f);
                nextGlance = Time.time + Random.Range(6f, 11f);
            }
            luisAnim.lookTarget = Time.time < glanceUntil ? Camera.main.transform.position : Ambulance.transform.TransformPoint(new Vector3(-0.5f, 1.8f, 25f));
        }

        // ------------------------------------------------------------------ the sequence

        public IEnumerator Play()
        {
            Playing = true;
            var hud = BodycamHUD.Instance;
            var d = DialogueDirector.Instance;
            SeatPlayer();
            Ambulance.SetHeadlights(true);
            ambDrive.SetPath(CrossStreetRoute, 0f);
            ambDrive.targetSpeed = 5f;

            // Intro card over black: the engine idles, the clock and the ride wait for it.
            var clock = GameClock.Instance;
            if (clock != null) clock.running = false;
            ambDrive.driving = false;
            yield return IntroCard.Play();
            if (clock != null) clock.running = true;
            ambDrive.driving = true;

            if (hud != null) { hud.SetFade(1f); hud.ShowCenter("EXHIBIT 14", 1.6f); }
            yield return new WaitForSeconds(1.7f);
            yield return Fade(1f, 0f, 1.2f);
            yield return new WaitForSeconds(0.4f);

            // About 45 seconds to the crash (30-minute game, 8a): the radio, Rachel's voicemail dismissed unheard (the
            // proof of the twist, played at the end), then "Mia" whispers that they are at the door, and the line dies.
            if (d != null)
            {
                yield return d.Radio(WithLuis(DemoLines.ColdOpenRadio));
                yield return new WaitForSeconds(0.5f);
                if (hud != null) hud.ShowCall("VOICEMAIL  ·  RACHEL", false);
                if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.VoicemailBeep, 0.35f);
                yield return d.Conversation(WithLuis(DemoLines.RachelIgnored));
                if (hud != null) hud.HideCall();
                yield return new WaitForSeconds(0.6f);
                yield return d.DroppedCall("MIA", true, DemoLines.MiaCall);
                yield return d.Conversation(WithLuis(DemoLines.CallDropped));
            }

            // The footage skips forward to the last seconds before the siren.
            yield return TapeSkip(() =>
            {
                ambDrive.SetPath(AvenueRoute, 5.5f);
                ambDrive.targetSpeed = 5.5f;
                if (GameClock.Instance != null && !PurgeStarted) GameClock.Instance.JumpTo(18, 59, 56);
            });

            yield return DriveToImpact();
            yield return Crash();
            Playing = false;
        }

        static bool PurgeStarted => PurgeClock.Instance != null && PurgeClock.Instance.Started;

        IEnumerator DriveToImpact()
        {
            var d = DialogueDirector.Instance;
            float crashDist = ambDrive.DistanceOf(CrashPoint);
            float toGrid = PurgeClock.Instance != null && !PurgeStarted ? PurgeClock.Instance.SecondsToGridDown : 12f;
            float impactAt = Time.time + toGrid + impactAfterGridDown;
            bool sirenLines = false;
            gridDown = false;

            // Pace the drive so the ambulance reaches the crash point just after the lights die.
            while (impactAt - Time.time > 3f)
            {
                float left = impactAt - Time.time;
                ambDrive.targetSpeed = Mathf.Clamp((crashDist - ambDrive.Distance) / left, 3f, 13f);
                if (!sirenLines && PurgeStarted)
                {
                    sirenLines = true;
                    StartCoroutine(SirenLines());
                }
                if (gridDown && gridLine == null && d != null)
                {
                    gridLine = StartCoroutine(d.Say(WithLuis(DemoLines.CabGridDown)));
                    impactAt = Mathf.Min(impactAt, Time.time + impactAfterGridDown); // the event is the truth
                }
                yield return null;
            }

            // Final run at a fixed speed: the truck is launched to meet the ambulance at the crash point.
            float runTime = Mathf.Max(0.5f, impactAt - Time.time);
            float speed = Mathf.Max(3f, (crashDist - ambDrive.Distance) / runTime);
            ambDrive.targetSpeed = speed;
            ambDrive.Speed = speed;
            ambDrive.stopAtEnd = false;

            Truck.gameObject.SetActive(true);
            Truck.SetHeadlights(false);
            Vector3 hit = new Vector3(CrashPoint.x + 1.1f, 0f, CrashPoint.z - 6.45f);
            truckDrive.SetPath(new[]
            {
                new Vector3(2.3f, 0f, CrashPoint.z - 46f), new Vector3(2.3f, 0f, CrashPoint.z - 18f),
                new Vector3(1.6f, 0f, CrashPoint.z - 12f), hit // clear of the wreck at z -42
            }, 0f);
            truckDrive.Speed = truckDrive.targetSpeed = truckDrive.Length / runTime;

            bool flared = false;
            while (ambDrive.Distance < crashDist)
            {
                if (!flared && impactAt - Time.time < 1.3f)
                {
                    // High beams and the roof bar, straight into the windscreen.
                    flared = true;
                    Truck.SetHeadlights(true);
                    if (gridLine != null) StopCoroutine(gridLine);
                    if (d != null) StartCoroutine(d.Say(DemoLines.CabWarning));
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayAt(SoundId.TireScreech, Ambulance.transform.position, 0.8f, 1f, 4f, 80f, false);
                }
                yield return null;
            }
        }

        IEnumerator SirenLines()
        {
            var d = DialogueDirector.Instance;
            if (d == null) yield break;
            yield return new WaitForSeconds(1.2f);
            var lines = WithLuis(DemoLines.CabSiren);
            yield return d.Say(lines[0]);
            yield return d.Say(lines[1]);
            Ambulance.lightBar.SetOn(true);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.FlashlightClick, 0.4f, 0.7f);
            if (!Crashed) yield return d.Say(lines[2]);
        }

        IEnumerator Crash()
        {
            var am = AudioManager.Instance;
            var hud = BodycamHUD.Instance;
            ambDrive.driving = false;
            truckDrive.driving = false;
            ambDrive.Speed = truckDrive.Speed = 0f;
            if (gridLine != null) StopCoroutine(gridLine);
            if (hud != null) hud.ShowSubtitle("", 0f);

            Vector3 contact = Ambulance.transform.TransformPoint(new Vector3(0.4f, 1f, 3.4f));
            if (am != null) am.PlayAt(SoundId.CrashImpact, contact, 1f, 1f, 8f, 200f, false);
            if (rig != null) { rig.AddTrauma(1f); rig.AddRecoil(new Vector2(-7f, 5f)); }

            Vector3 a0 = Ambulance.transform.position, t0 = Truck.transform.position;
            float yaw0 = Ambulance.transform.eulerAngles.y;
            Quaternion tr0 = Truck.transform.rotation;
            bool scraped = false;
            const float dur = 1.15f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float u = t / dur;
                float yaw = Mathf.LerpAngle(yaw0, AmbulanceRestYaw, 1f - (1f - u) * (1f - u));
                float roll = AmbulanceRestRoll * Mathf.Pow(Mathf.Clamp01((t - 0.2f) / (dur - 0.2f)), 1.6f);
                Vector3 p = Vector3.Lerp(a0, AmbulanceRest, 1f - (1f - u) * (1f - u));
                p.y = LyingLift * Mathf.Sin(-roll * Mathf.Deg2Rad);
                Ambulance.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, roll));

                float k = Mathf.Clamp01(t / 0.7f);
                k = 1f - (1f - k) * (1f - k);
                Truck.transform.SetPositionAndRotation(Vector3.Lerp(t0, TruckRest, k), Quaternion.Slerp(tr0, Quaternion.Euler(0f, TruckRestYaw, 0f), k));
                if (!scraped && t > 0.35f)
                {
                    scraped = true;
                    if (am != null) am.PlayAt(SoundId.MetalScrape, Ambulance.transform.position, 0.9f, 1f, 6f, 120f, false);
                }
                yield return null;
            }

            // Landing on its side. The footage cuts out on the hit.
            if (am != null)
            {
                am.PlayAt(SoundId.CrashImpact, Ambulance.transform.position, 0.9f, 0.75f, 8f, 200f, false);
                PlayInsideHead(SoundId.Tinnitus, 0.45f);
            }
            if (hud != null) hud.SetFade(1f);
            Muffle(700f);
            ApplyCrashed();
        }

        // ------------------------------------------------------------------ the crash site

        /// <summary>Puts everything in its final place (end of the crash, or straight away when skipping to it).</summary>
        public void ApplyCrashed()
        {
            if (Crashed) return;
            Crashed = true;
            ambDrive.driving = truckDrive.driving = false;
            ambDrive.Speed = truckDrive.Speed = 0f;
            if (ambDrive.Engine != null) ambDrive.Engine.Stop();
            if (truckDrive.Engine != null) { truckDrive.Engine.volume = 0.35f; truckDrive.Engine.pitch = 0.75f; }

            Ambulance.transform.SetPositionAndRotation(AmbulanceRest + Vector3.up * LyingLift * Mathf.Sin(-AmbulanceRestRoll * Mathf.Deg2Rad),
                Quaternion.Euler(0f, AmbulanceRestYaw, 0f) * Quaternion.Euler(0f, 0f, AmbulanceRestRoll));
            Ambulance.body.localPosition = Vector3.zero;
            Ambulance.body.localRotation = Quaternion.identity;
            Ambulance.SetHeadlights(false);
            Ambulance.lightBar.SetOn(true);
            Ambulance.interiorLight.enabled = false;
            if (Ambulance.windshield != null) Ambulance.windshield.SetActive(false);
            Ambulance.rearDoorR.localRotation = Quaternion.Euler(0f, -92f, 0f); // lies open on the asphalt
            Ambulance.compartmentLight.enabled = true;
            var flicker = Ambulance.compartmentLight.gameObject.AddComponent<LightFlicker>();
            flicker.mode = LightFlicker.Mode.FailingLamp;
            flicker.baseIntensity = 0.9f;

            Truck.gameObject.SetActive(true);
            Truck.transform.SetPositionAndRotation(TruckRest, Quaternion.Euler(0f, TruckRestYaw, 0f));
            Truck.body.localPosition = Vector3.zero;
            Truck.body.localRotation = Quaternion.identity;
            Truck.SetHeadlights(true);
            if (fx.steam != null) FireFX.Steam(fx, Truck.transform, Truck.transform.TransformPoint(new Vector3(0f, 1.5f, 2.2f)));
            StartCoroutine(StuckHorn());

            Obstacle(Ambulance.gameObject, new Vector3(0f, 1.7f, -0.05f), new Vector3(2.4f, 2.6f, 6.9f));
            Obstacle(Truck.gameObject, new Vector3(0f, 1.2f, 0.1f), new Vector3(2.05f, 1.9f, 5.9f));
            Debris();
            Barricade();

            // Luis, thrown clear, sits against the underside of the wreck.
            if (Luis != null && !Luis.IsRagdoll)
            {
                Luis.transform.SetParent(null, true);
                Vector3 west = Ambulance.transform.up; west.y = 0f; west.Normalize();
                Vector3 spot = Ambulance.transform.TransformPoint(new Vector3(0f, 0.55f, 0.3f)) - west * 0.32f;
                spot.y = 0f;
                if (NavMesh.SamplePosition(spot, out NavMeshHit nh, 1f, NavMesh.AllAreas)) spot.y = nh.position.y;
                Luis.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(-west));
                luisAnim.handTargetL = luisAnim.handTargetR = null;
                luisAnim.pose = ProcAnimator.Pose.Slumped;
                luisAnim.lookTarget = null;
                luisAnim.SnapToPose();
            }
        }

        /// <summary>Where the Smilers closed Harlow behind the ambulance: the way north is shut.</summary>
        public const float BarricadeZ = 19.5f;

        /// <summary>
        /// Jersey barriers kerb to kerb, burning tyre stacks and a fire in the middle, dragged across Harlow just south of
        /// the intersection after the ambulance went by. The Smiler watchers stand behind it in the firelight. A tall
        /// invisible wall keeps Daniel from vaulting it: the way is south.
        /// </summary>
        void Barricade()
        {
            var root = new GameObject("SmilerBarricade").transform;
            root.SetParent(transform, false);
            var concrete = RuntimeMaterials.Lit(new Color(0.42f, 0.41f, 0.39f), 0.25f);
            var rubber = RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.02f), 0.3f);
            var paint = RuntimeMaterials.Lit(new Color(0.75f, 0.75f, 0.72f), 0.2f);
            var mb = new MeshBuilder();
            Vector2[] profile = { new Vector2(-0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(0.3f, 0.08f), new Vector2(0.18f, 0.3f), new Vector2(0.1f, 0.81f), new Vector2(-0.1f, 0.81f), new Vector2(-0.18f, 0.3f), new Vector2(-0.3f, 0.08f) };
            var rng = new System.Random(19);
            for (int i = 0; i < 7; i++)
            {
                float x = -9.6f + i * 3.2f;
                float y = Mathf.Abs(x) > 7f ? 0.15f : 0f; // the ends stand on the sidewalks
                float jitter = i == 3 ? 0f : 1f; // the middle one carries the painted grin
                mb.matrix = Matrix4x4.TRS(new Vector3(x, y, BarricadeZ + (float)(rng.NextDouble() - 0.5) * 0.5f * jitter), Quaternion.Euler(0f, 90f + (float)(rng.NextDouble() - 0.5) * 14f * jitter, 0f), Vector3.one);
                mb.Extrude(concrete, concrete, profile, -1.55f, 1.55f);
            }
            mb.matrix = Matrix4x4.identity;
            // A grin daubed across the middle barriers, like the one on the truck.
            Grin(mb, paint, Matrix4x4.Translate(new Vector3(0f, 0.55f, BarricadeZ - 0.16f)), 1.6f, false);
            foreach (float tx in new[] { -3.4f, 4.6f })
                for (int k = 0; k < 4; k++)
                {
                    var save = mb.matrix;
                    mb.matrix = Matrix4x4.TRS(new Vector3(tx + (float)(rng.NextDouble() - 0.5) * 0.3f, k * 0.22f, BarricadeZ - 0.9f), Quaternion.identity, Vector3.one);
                    mb.Cylinder(rubber, Vector3.zero, 0.34f, 0.22f, 16);
                    mb.matrix = save;
                }
            var go = mb.Build("Barriers", root);
            var wall = go.AddComponent<BoxCollider>();
            wall.center = new Vector3(0f, 1.5f, BarricadeZ - 0.3f);
            wall.size = new Vector3(22.5f, 3f, 1.6f);
            Obstacle(go, wall.center, wall.size);
            if (fx.flame != null)
            {
                FireFX.Fire(fx, root, new Vector3(-3.4f, 0.9f, BarricadeZ - 0.9f), 1.1f, true, false);
                FireFX.Fire(fx, root, new Vector3(4.6f, 0.9f, BarricadeZ - 0.9f), 1.1f, false, false);
                FireFX.Fire(fx, root, new Vector3(0.6f, 0.2f, BarricadeZ + 0.8f), 1.5f, true, false);
            }
        }

        /// <summary>
        /// The Smilers' mark, daubed in white: a sagging row of strokes (and two eyes) in the XY plane of `frame`,
        /// centred on its origin. The same grin as on the ram truck.
        /// </summary>
        static void Grin(MeshBuilder mb, Material paint, Matrix4x4 frame, float halfWidth, bool eyes)
        {
            var saved = mb.matrix;
            mb.matrix = frame;
            float stroke = halfWidth * 0.25f, thick = 0.012f + halfWidth * 0.024f;
            for (int k = 0; k < 9; k++)
            {
                float u = (k - 4) / 4f;
                mb.BoxRotated(paint, new Vector3(u * halfWidth, -halfWidth * 0.125f * (1f - u * u), 0f), new Vector3(stroke, thick, 0.02f), Quaternion.Euler(0f, 0f, -u * 20f));
            }
            if (eyes)
                foreach (float s in new[] { -0.4f, 0.4f })
                    mb.Box(paint, new Vector3(s * halfWidth, halfWidth * 0.45f, 0f), new Vector3(thick, stroke * 0.8f, 0.02f));
            mb.matrix = saved;
        }

        /// <summary>After the Smilers kill Luis: their grin on the underside of the wreck, right beside him.</summary>
        public void PaintGrinBesideLuis()
        {
            var t = Ambulance.transform;
            // On the frame rail's face (local y 0.5; local x is height now that the ambulance lies on its side), 1.35 m up.
            Vector3 c = t.TransformPoint(new Vector3(-0.15f, 0.48f, 0.9f));
            var frame = Matrix4x4.TRS(c, Quaternion.LookRotation(-t.up, Vector3.up), Vector3.one);
            var mb = new MeshBuilder();
            Grin(mb, RuntimeMaterials.Lit(new Color(0.75f, 0.75f, 0.72f), 0.2f), frame, 0.45f, true);
            mb.Build("SmilerMark", transform, false);
        }

        /// <summary>Points on the asphalt around the wreck, for the crash-site scene.</summary>
        public Vector3 WakePoint => new Vector3(3.4f, 0.05f, -15.2f);
        public float WakeYaw => 235f;

        public Vector3[] SpilledKitPoints()
        {
            var t = Ambulance.transform;
            Vector3 a = t.TransformPoint(new Vector3(0f, 1.3f, -4.4f)), b = t.TransformPoint(new Vector3(0f, 2.5f, -5.2f));
            a.y = b.y = 0.02f;
            return new[] { a, b };
        }

        IEnumerator StuckHorn()
        {
            var horn = new GameObject("StuckHorn");
            horn.transform.SetParent(Truck.transform, false);
            horn.transform.localPosition = new Vector3(0f, 1.1f, 2.4f);
            var src = ProceduralAudioSource.Attach(horn, SoundId.CarHorn, 0.45f, 3f, 70f);
            src.pitchJitter = 0f;
            yield return new WaitForSeconds(11f);
            var a = horn.GetComponent<AudioSource>();
            // The battery gives up: the horn sags and dies.
            for (float t = 0f; t < 1.4f && a != null; t += Time.deltaTime)
            {
                a.pitch = Mathf.Lerp(1f, 0.82f, t / 1.4f);
                a.volume = Mathf.Lerp(0.45f, 0f, t / 1.4f);
                yield return null;
            }
            Destroy(horn);
        }

        static void Obstacle(GameObject go, Vector3 center, Vector3 size)
        {
            var o = go.AddComponent<NavMeshObstacle>();
            o.shape = NavMeshObstacleShape.Box;
            o.center = center;
            o.size = size;
            o.carving = true;
        }

        /// <summary>Windscreen glass glittering in the truck's lights, bits of trim and a broken lens.</summary>
        void Debris()
        {
            var rng = new System.Random(77);
            var mb = new MeshBuilder();
            var glassBits = RuntimeMaterials.Lit(new Color(0.5f, 0.58f, 0.6f), 0.95f);
            var trim = RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.03f), 0.3f);
            var lens = RuntimeMaterials.Lit(new Color(0.5f, 0.05f, 0.04f), 0.9f);
            Vector3 front = Ambulance.transform.TransformPoint(new Vector3(0f, 1.2f, 3.8f));
            front.y = 0f;
            for (int i = 0; i < 70; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble()) * 3.2f;
                Vector3 p = front + new Vector3(Mathf.Cos(a) * r, 0.006f, Mathf.Sin(a) * r);
                float s = 0.012f + (float)rng.NextDouble() * 0.025f;
                mb.BoxRotated(glassBits, p, new Vector3(s, 0.006f, s * 0.8f), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            }
            mb.BoxRotated(trim, front + new Vector3(1.2f, 0.06f, -0.8f), new Vector3(1.4f, 0.12f, 0.16f), Quaternion.Euler(0f, 35f, 0f));
            mb.BoxRotated(lens, front + new Vector3(-0.6f, 0.02f, 1.1f), new Vector3(0.2f, 0.03f, 0.08f), Quaternion.Euler(0f, 70f, 0f));
            mb.BoxRotated(trim, front + new Vector3(2.4f, 0.05f, 0.9f), new Vector3(0.18f, 0.3f, 0.2f), Quaternion.Euler(80f, 20f, 0f)); // a mirror
            mb.Build("CrashDebris", transform, false);
        }

        // ------------------------------------------------------------------ helpers

        void SeatPlayer()
        {
            if (rig == null) return;
            rig.Mount(Ambulance.passengerMount);
            input.LookOnly = true;
            var light = player.GetComponentInChildren<Flashlight>();
            if (light != null) light.SetOn(false);
            Perception.PlayerConcealed = true;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.SetVitalsVisible(false);
        }

        /// <summary>Luis's lines come from where he sits (or lies).</summary>
        Line[] WithLuis(Line[] lines)
        {
            foreach (var l in lines) WithLuis(l);
            return lines;
        }

        Line WithLuis(Line l)
        {
            if (l.who == Speaker.Luis && Luis != null) l.At(Luis.transform);
            return l;
        }

        IEnumerator TapeSkip(System.Action during)
        {
            var hud = BodycamHUD.Instance;
            if (hud != null) hud.Glitch(0.45f);
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TapeSkip, 0.55f);
            yield return new WaitForSeconds(0.2f);
            if (hud != null) hud.SetFade(1f);
            during();
            yield return new WaitForSeconds(0.35f);
            if (hud != null) { hud.SetFade(0f); hud.Glitch(0.3f); }
        }

        public static IEnumerator Fade(float from, float to, float seconds)
        {
            var hud = BodycamHUD.Instance;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (hud != null) hud.SetFade(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            if (hud != null) hud.SetFade(to);
        }

        /// <summary>
        /// A sound in his own head (the ringing): bypasses the listener's low-pass, so it stays sharp while the world
        /// is muffled.
        /// </summary>
        void PlayInsideHead(SoundId id, float volume)
        {
            var am = AudioManager.Instance;
            var clip = am != null ? am.Clip(id) : null;
            if (clip == null) return;
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.bypassListenerEffects = true;
            src.Play();
            Destroy(src, clip.length + 0.1f);
        }

        /// <summary>Dulls the whole mix (ringing ears): the bodycam mic's low-pass on the listener.</summary>
        public static void Muffle(float cutoff)
        {
            if (Camera.main == null || !Camera.main.TryGetComponent(out AudioLowPassFilter lp)) return;
            lp.cutoffFrequency = cutoff;
        }

        /// <summary>Called when skipping straight to the crash site: no ride, the night already broken.</summary>
        public void Skip()
        {
            StopAllCoroutines();
            Playing = false;
            var hud = BodycamHUD.Instance;
            if (hud != null) { hud.HideCall(); hud.ShowSubtitle("", 0f); hud.SetCard("", 0f); hud.SetFade(1f); }
            if (GameClock.Instance != null)
            {
                GameClock.Instance.running = true; // the intro card pauses it
                if (!PurgeStarted) GameClock.Instance.JumpTo(18, 59, 59);
            }
            if (rig != null && !rig.IsMounted) SeatPlayer();
            ApplyCrashed();
        }
    }
}
