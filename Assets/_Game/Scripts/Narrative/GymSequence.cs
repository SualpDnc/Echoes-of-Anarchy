using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// 8e, the finale in the school gym, the Smilers' broadcast set. The gym is a closed room built at runtime away from
    /// the district (the school building has no inside): through the doors, a fade, and Daniel stands at the gym's end.
    /// Vance waits under the lights with three Smilers filming, Daniel's own feed on the big screen behind him. Face to
    /// face he takes off his mask; the past (Daniel reported him); "Mia was never here". Daniel plays Rachel's voicemail
    /// at last and calls Mia, who answers half asleep. "Do it, Danny. Everyone's watching": the gun comes up and the
    /// player decides, fire (left click) or holster it (X). The ending is the same; the lines and Hale's note change.
    /// </summary>
    public class GymSequence : MonoBehaviour
    {
        /// <summary>The player pulled the trigger on Vance.</summary>
        public bool Fired { get; private set; }
        /// <summary>The bodycam's time when the gun came up (Hale's note quotes it; the night runs in real time, so it isn't a fixed hour).</summary>
        public System.DateTime ChoiceTime { get; private set; }

        static readonly Vector3 Origin = new Vector3(0f, -60f, -40f);
        const float HalfX = 12f, HalfZ = 9f, Height = 7f;

        HumanoidRig vance;
        ProcAnimator vanceAnim;
        Transform mask;
        readonly List<ProcAnimator> crew = new List<ProcAnimator>();
        bool shot;

        public IEnumerator Play()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            var hud = BodycamHUD.Instance;
            var dd = DialogueDirector.Instance;
            var input = player.GetComponent<Player.EchoesInput>();
            var body = player.GetComponent<Player.FPSController>();
            var rig = player.GetComponent<Player.CameraRig>();
            var pistol = player.GetComponent<WeaponPistol>();

            Build(rig);
            body.Teleport(Origin + new Vector3(0f, 0.05f, -HalfZ + 1.2f), 0f);
            if (input != null) { input.InputLocked = false; input.LookOnly = false; }
            Perception.PlayerConcealed = true;
            if (hud != null)
            {
                hud.SetObjective("Vance");
                for (float t = 0f; t < 1f; t += Time.deltaTime) { hud.SetFade(1f - t); yield return null; }
                hud.SetFade(0f);
            }
            if (dd != null) yield return dd.Say(DemoLines.GymEnter);

            // Up to him.
            while (Vector3.Distance(player.transform.position, vance.transform.position) > 5.5f) { Face(player); yield return null; }
            if (input != null) input.LookOnly = true;
            var talk = DemoLines.GymTalk;
            if (dd != null)
            {
                yield return dd.Say(talk[0].At(vance.transform));
                yield return dd.Say(talk[1].At(vance.transform));
                yield return Unmask();
                for (int i = 2; i < talk.Length; i++) yield return dd.Say(talk[i].At(vance.transform));
                yield return dd.Say(DemoLines.Voicemail);
                yield return dd.Voicemail("RACHEL", DemoLines.RachelMessage);
                yield return dd.Call("MIA", false, DemoLines.RealMia);
                yield return dd.Say(DemoLines.DoIt.At(vance.transform));
            }

            // The choice: the gun comes up, Daniel can look and fire, but not walk away.
            body.enabled = false;
            if (Core.GameClock.Instance != null) ChoiceTime = Core.GameClock.Instance.Now;
            if (input != null) input.LookOnly = false;
            if (pistol != null) { if (pistol.ammoInMag < 1) pistol.ammoInMag = 1; pistol.Draw(); } // one round, always
            if (hud != null) hud.ShowHint("Fire (left click), or holster the gun (X).", 20f);
            bool fired = false;
            System.Action<NoiseEvent> onShot = ev => { if (ev.fromPlayer && ev.kind == NoiseKind.Gunshot) fired = true; };
            NoiseSystem.Emitted += onShot;
            yield return new WaitForSeconds(1f);
            float until = Time.time + 25f;
            while (!fired && Time.time < until && (pistol == null || pistol.IsDrawn || pistol.IsBusy)) yield return null;
            NoiseSystem.Emitted -= onShot;
            Fired = fired;
            if (hud != null) hud.ShowHint("", 0.1f);
            if (input != null) input.LookOnly = true;

            if (fired)
            {
                if (!shot) Kill(player); // a miss still counts: he pulled the trigger, and Vance goes down on camera
                yield return new WaitForSeconds(1.2f);
                if (dd != null)
                {
                    yield return dd.Say(DemoLines.FiredCheer.At(crew.Count > 0 ? crew[0].transform : vance.transform));
                    yield return dd.Say(DemoLines.FiredThought);
                }
            }
            else
            {
                if (pistol != null) pistol.Stowed = true;
                var lowered = DemoLines.Lowered;
                if (dd != null)
                    for (int i = 0; i < lowered.Length; i++)
                        yield return dd.Say(i == 2 ? lowered[i].At(vance.transform) : lowered[i]);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.PoliceSiren, 0.35f);
            yield return new WaitForSeconds(2.5f);
        }

        void Face(GameObject player)
        {
            Vector3 eyes = player.transform.position + Vector3.up * 1.5f;
            if (vanceAnim != null && !vance.IsRagdoll) vanceAnim.lookTarget = eyes;
            foreach (var c in crew) if (c != null) c.lookTarget = eyes;
        }

        IEnumerator Unmask()
        {
            if (mask == null) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.Bandage, mask.position, 0.6f, 1.2f, 1f, 15f);
            Vector3 from = mask.position, to = vance.transform.position + vance.transform.right * 0.35f + Vector3.up * 0.95f;
            mask.SetParent(null, true);
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                mask.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.9f));
                yield return null;
            }
            var rb = mask.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            mask.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.26f, 0.1f);
            yield return new WaitForSeconds(0.4f);
        }

        void Kill(GameObject player)
        {
            if (vance == null || vance.IsRagdoll) return;
            if (vanceAnim != null) vanceAnim.enabled = false;
            Vector3 dir = (vance.transform.position - player.transform.position).normalized;
            vance.EnableRagdoll(Vector3.zero, new DamageInfo { amount = 999f, direction = dir, force = 400f, point = vance.transform.position + Vector3.up * 1.4f, source = player, fromPlayer = true });
        }

        // ------------------------------------------------------------------ the set

        void Build(Player.CameraRig rig)
        {
            var root = new GameObject("Gym").transform;
            root.SetParent(transform, false);
            root.position = Origin;
            var floor = RuntimeMaterials.Lit(new Color(0.42f, 0.28f, 0.15f), 0.55f);
            var wall = RuntimeMaterials.Lit(new Color(0.32f, 0.3f, 0.28f), 0.2f);
            var dark = RuntimeMaterials.Lit(new Color(0.04f, 0.04f, 0.045f), 0.35f, 0.4f);
            var paint = RuntimeMaterials.Lit(new Color(0.75f, 0.72f, 0.65f), 0.4f);
            void Box(Vector3 c, Vector3 s, Material m, bool collide = true)
            {
                var go = PrimitiveMeshes.Part(root, "GymPart", PrimitiveType.Cube, c, s, m);
                if (collide) go.AddComponent<BoxCollider>();
            }
            // Shell.
            Box(new Vector3(0f, -0.15f, 0f), new Vector3(HalfX * 2f, 0.3f, HalfZ * 2f), floor);
            Box(new Vector3(0f, Height + 0.15f, 0f), new Vector3(HalfX * 2f, 0.3f, HalfZ * 2f), wall);
            foreach (float s in new[] { -1f, 1f })
            {
                Box(new Vector3(s * (HalfX + 0.15f), Height / 2f, 0f), new Vector3(0.3f, Height, HalfZ * 2f), wall);
                Box(new Vector3(0f, Height / 2f, s * (HalfZ + 0.15f)), new Vector3(HalfX * 2f, Height, 0.3f), wall);
            }
            // Court lines, the hoops, the bleachers along the west wall.
            Box(new Vector3(0f, 0.004f, 0f), new Vector3(0.08f, 0.008f, HalfZ * 2f - 1f), paint, false);
            Box(new Vector3(0f, 0.004f, 0f), new Vector3(HalfX * 2f - 1f, 0.008f, 0.08f), paint, false);
            foreach (float s in new[] { -1f, 1f })
            {
                Box(new Vector3(0f, 3.05f, s * (HalfZ - 1.2f)), new Vector3(1.8f, 1.05f, 0.05f), paint, false);
                Box(new Vector3(0f, 2.9f, s * (HalfZ - 0.6f)), new Vector3(0.1f, 0.1f, 1.2f), dark, false);
            }
            for (int k = 0; k < 4; k++)
                Box(new Vector3(-HalfX + 0.6f + k * 0.8f, 0.25f + k * 0.45f, 0f), new Vector3(0.8f, 0.5f + k * 0.9f, HalfZ * 1.6f), dark);
            // The set: a chair under the lights, ring lights on stands, tripods.
            Box(new Vector3(0f, 0.45f, 2.6f), new Vector3(0.5f, 0.05f, 0.5f), dark);
            Box(new Vector3(0f, 0.9f, 2.85f), new Vector3(0.5f, 0.9f, 0.05f), dark, false);
            var ring = RuntimeMaterials.Lit(Color.black, 0.2f, 0f, new Color(1f, 0.95f, 0.9f) * 3f);
            foreach (float s in new[] { -1f, 1f })
            {
                Box(new Vector3(s * 2.6f, 1f, 3.6f), new Vector3(0.05f, 2f, 0.05f), dark, false);
                Box(new Vector3(s * 2.6f, 2.1f, 3.6f), new Vector3(0.6f, 0.6f, 0.04f), ring, false);
            }
            var screen = StreamScreen.Build(root, Origin + new Vector3(0f, 4.4f, HalfZ - 0.3f), 0f);
            if (rig != null) screen.ShowFeed(rig.cam);
            foreach (var p in new[] { new Vector3(-4f, 6.8f, -2f), new Vector3(4f, 6.8f, -2f), new Vector3(0f, 6.8f, 4f) })
            {
                var lg = new GameObject("GymSpot", typeof(Light));
                lg.transform.SetParent(root, false);
                lg.transform.position = Origin + p;
                lg.transform.rotation = Quaternion.LookRotation(Origin + new Vector3(0f, 0f, 2f) - lg.transform.position);
                var l = lg.GetComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 55f;
                l.range = 18f;
                l.intensity = 9f;
                l.color = new Color(1f, 0.95f, 0.88f);
                l.shadows = p.z > 0f ? LightShadows.Soft : LightShadows.None;
            }
            var amb = new GameObject("GymFill", typeof(Light));
            amb.transform.SetParent(root, false);
            amb.transform.position = Origin + new Vector3(0f, 5f, -5f);
            var al = amb.GetComponent<Light>();
            al.type = LightType.Point;
            al.range = 20f;
            al.intensity = 1.2f;
            al.shadows = LightShadows.None;
            var rz = new GameObject("GymReverb", typeof(AudioReverbZone)).GetComponent<AudioReverbZone>();
            rz.transform.SetParent(root, false);
            rz.transform.position = Origin + Vector3.up * 2f;
            rz.reverbPreset = AudioReverbPreset.Auditorium;
            rz.minDistance = 10f;
            rz.maxDistance = 16f;

            // Vance, in the light, his LED mask on.
            var rng = new System.Random(4471);
            var look = CharacterLook.Pick(Faction.Smiler, rng);
            look.head = HeadKind.Face;
            look.hair = HairKind.Short;
            look.feminine = false;
            look.height = 1.04f;
            look.bulk = 1.1f;
            look.top = TopKind.OpenJacket;
            look.closeUp = true;
            look.definition = 1f;
            look.Dress(rng);
            vance = HumanoidBuilder.Build(root, look, rng, out var vh);
            vance.gameObject.name = "Vance";
            vance.transform.SetPositionAndRotation(Origin + new Vector3(0f, 0f, 2f), Quaternion.Euler(0f, 180f, 0f));
            vanceAnim = vance.gameObject.AddComponent<ProcAnimator>();
            vanceAnim.footIK = false;
            vh.maxHealth = 20f;
            vh.SetHealth(20f);
            vh.Died += info => { shot = true; if (vanceAnim != null) vanceAnim.enabled = false; vance.EnableRagdoll(Vector3.zero, info); };
            var head = vance[Bone.Head];
            mask = new GameObject("VanceMask").transform;
            mask.SetPositionAndRotation(head.position + vance.transform.forward * 0.12f + Vector3.up * 0.03f, vance.transform.rotation);
            mask.SetParent(head, true);
            PrimitiveMeshes.Part(mask, "Shell", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.19f, 0.25f, 0.09f), RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.02f), 0.6f));
            var led = RuntimeMaterials.Lit(Color.black, 0.2f, 0f, new Color(1f, 0.8f, 0.15f) * 4f);
            for (int k = 0; k < 7; k++)
            {
                float u = k / 6f * 2f - 1f;
                PrimitiveMeshes.Part(mask, "Led", PrimitiveType.Cube, new Vector3(u * 0.06f, -0.04f - 0.025f * (1f - u * u), 0.045f), Vector3.one * 0.012f, led, null, false);
            }

            // Three Smilers filming him and Daniel.
            foreach (var p in new[] { new Vector3(-3.5f, 0f, 1f), new Vector3(3.5f, 0f, 1f), new Vector3(-5f, 0f, -3f) })
            {
                var c = HumanoidBuilder.Build(root, CharacterLook.Random(Faction.Smiler, rng), rng, out _);
                c.transform.SetPositionAndRotation(Origin + p, Quaternion.Euler(0f, p.x < 0f ? 120f : 240f, 0f));
                var a = c.gameObject.AddComponent<ProcAnimator>();
                a.footIK = false;
                a.pose = ProcAnimator.Pose.Aim; // a phone held up
                a.aimTarget = Origin + new Vector3(0f, 1.5f, -2f);
                NPCBrain.AttachCarriedLight(c, true);
                crew.Add(a);
            }
        }
    }
}
