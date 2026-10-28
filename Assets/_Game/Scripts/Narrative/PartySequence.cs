using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
    /// 8d, the Purge party in the school yard. Daniel comes up the subway stairs inside a plywood hoarding; the yard
    /// beyond is a Smilers' party waiting for "the show" (him: the big screen on the school counts down to it). A
    /// reveller lies passed out behind the hoarding: Daniel takes his mask and jacket and walks the crowd to the school
    /// door. Guards in LED grins watch the crowd: running, a drawn gun, crouching or staring at them fills the eye at the
    /// top of the screen; full, they know him. Through the VIP gap by the stage and along the facade, a step from the
    /// door, the music dies and the screen cuts to Daniel's own camera: Vance on the PA picks him out ("Take off the
    /// mask, Marine"), and the whole party comes for him. On the Smilers' pickup by the door stands a machine gun with no
    /// end to its belt. Wave one is the crowd, wave two comes out of the backstage door, and last the ram truck that hit
    /// the ambulance smashes the yard gate and charges the pickup: stopped, it blows up, and the way to the door is open.
    /// PartyBuilder builds the static set with the scene; this adds the people, the pickup and its gun, the screen,
    /// the music, the hoarding's door and the yard gate.
    /// </summary>
    public class PartySequence : MonoBehaviour
    {
        /// <summary>Up out of the subway into the hoarding (the Party checkpoint).</summary>
        public bool Started { get; private set; }
        /// <summary>On the gun (the Turret checkpoint).</summary>
        public bool OnTurret { get; private set; }
        public bool Done { get; private set; }

        static readonly Vector3 TechPos = new Vector3(-92.05f, DistrictGenerator.Curb, -7.8f); // front to the west, the bed by the door
        static readonly Vector3 CrowdCentre = new Vector3(-88.5f, DistrictGenerator.Curb + 1.6f, -15.5f);
        static readonly Vector3[] TruckRoute =
        {
            new Vector3(-82f, DistrictGenerator.Curb, -40f), new Vector3(-82f, DistrictGenerator.Curb, -26f), new Vector3(-83f, DistrictGenerator.Curb, -20.5f),
            new Vector3(-87f, DistrictGenerator.Curb, -18.5f), new Vector3(-92f, DistrictGenerator.Curb, -16f), new Vector3(-93f, DistrictGenerator.Curb, -12.8f),
            new Vector3(-92.5f, DistrictGenerator.Curb, -10.4f),
        };

        GameObject player;
        Player.CameraRig rig;
        Player.EchoesInput input;
        Player.FPSController body;
        WeaponPistol pistol;
        Health playerHealth;
        MetroSequence metro;

        readonly List<NPCBrain> crowd = new List<NPCBrain>();
        readonly List<NPCBrain> guards = new List<NPCBrain>();
        readonly List<NPCBrain> waveTwo = new List<NPCBrain>();
        readonly List<float> phase = new List<float>();
        NPCBrain dj;
        VehicleRig technical;
        PickupTurret turret;
        StreamScreen screen;
        Transform boxDoor, gateL, gateR, maskProp;
        HumanoidRig reveller;
        Light spot;
        AudioSource music, walla, roar;
        CheckpointStage restoreStage;
        bool built, spawned, talking, maskTaken, boxDoorOpen, revealed, engaged, gateSmashed, saidMaskSeen;
        float suspicion, nextWary, nextNoMask;
        int hypeLines, talkLines, waryLines;

        // ------------------------------------------------------------------ setup (after the NavMesh bake)

        public void Setup()
        {
            if (built) return;
            built = true;
            var root = new GameObject("PartyRuntime").transform;
            root.SetParent(transform, false);
            screen = StreamScreen.Build(root, PartyBuilder.ScreenCentre, 0f);
            Technical(root);
            BoxDoor(root);
            YardGate(root);
            Reveller(root);
            var sg = new GameObject("RevealSpot", typeof(Light));
            sg.transform.SetParent(root, false);
            sg.transform.position = new Vector3(PartyBuilder.StageX0 + 0.2f, DistrictGenerator.Curb + 5.6f, -8.5f);
            spot = sg.GetComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.97f, 0.92f);
            spot.intensity = 30f;
            spot.spotAngle = 11f;
            spot.range = 32f;
            spot.shadows = LightShadows.None;
            spot.enabled = false;
            Health.AnyDamaged += OnAnyDamaged;
            NoiseSystem.Emitted += OnNoise;
        }

        void OnDestroy()
        {
            Health.AnyDamaged -= OnAnyDamaged;
            NoiseSystem.Emitted -= OnNoise;
        }

        /// <summary>The Smilers' pickup parked along the facade by the door, the machine gun in its bed.</summary>
        void Technical(Transform root)
        {
            technical = VehicleFactory.Pickup(root, "Technical", new Color(0.22f, 0.035f, 0.03f), false);
            technical.transform.SetPositionAndRotation(TechPos, Quaternion.Euler(0f, 270f, 0f));
            var o = technical.gameObject.AddComponent<NavMeshObstacle>();
            o.shape = NavMeshObstacleShape.Box;
            o.center = new Vector3(0f, 1.2f, 0.1f);
            o.size = new Vector3(2.1f, 2f, 6f);
            o.carving = true;
            turret = technical.gameObject.AddComponent<PickupTurret>();
            turret.Build();
            var use = new GameObject("GunUse", typeof(BoxCollider));
            use.transform.SetParent(technical.transform, false);
            use.transform.localPosition = new Vector3(0f, 1.6f, -2f);
            var bc = use.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(2.2f, 1.6f, 1.8f);
            StoryInteractable.Add(use, "Man the gun", () => revealed && !turret.Mounted && !Done, p => MountGun(), 3.2f);
        }

        /// <summary>The hoarding's door: shut until Daniel looks like one of them.</summary>
        void BoxDoor(Transform root)
        {
            var ply = RuntimeMaterials.Lit(new Color(0.42f, 0.33f, 0.22f), 0.15f);
            float w = PartyBuilder.BoxDoorZ1 - PartyBuilder.BoxDoorZ0;
            boxDoor = new GameObject("HoardingDoor").transform;
            boxDoor.SetParent(root, false);
            boxDoor.SetPositionAndRotation(new Vector3(PartyBuilder.BoxX1, DistrictGenerator.Curb, PartyBuilder.BoxDoorZ0), Quaternion.Euler(0f, -90f, 0f));
            PrimitiveMeshes.Part(boxDoor, "Leaf", PrimitiveType.Cube, new Vector3(w / 2f, 1.15f, 0f), new Vector3(w - 0.04f, 2.3f, 0.05f), ply);
            PrimitiveMeshes.Part(boxDoor, "Sign", PrimitiveType.Cube, new Vector3(w / 2f, 1.5f, 0.03f), new Vector3(0.7f, 0.25f, 0.01f), RuntimeMaterials.Lit(new Color(0.75f, 0.6f, 0.1f), 0.3f));
            var col = new GameObject("Block", typeof(BoxCollider));
            col.transform.SetParent(boxDoor, false);
            col.transform.localPosition = new Vector3(w / 2f, 1.15f, 0f);
            col.GetComponent<BoxCollider>().size = new Vector3(w, 2.3f, 0.1f);
            StoryInteractable.Add(col, "Open the door", () => maskTaken && !boxDoorOpen, p => OpenBoxDoor(true), 2.4f);
        }

        void OpenBoxDoor(bool animate)
        {
            if (boxDoorOpen) return;
            boxDoorOpen = true;
            StartCoroutine(Swing(boxDoor, -90f, -5f, animate ? 0.6f : 0f));
            if (animate && AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, boxDoor.position + Vector3.up, 0.25f, 1.4f, 2f, 20f);
        }

        static IEnumerator Swing(Transform t, float from, float to, float time)
        {
            for (float k = 0f; k < time; k += Time.deltaTime)
            {
                t.rotation = Quaternion.Euler(0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k / time)), 0f);
                yield return null;
            }
            t.rotation = Quaternion.Euler(0f, to, 0f);
        }

        /// <summary>The yard gate at the end of the back alley: two palisade leaves, chained.</summary>
        void YardGate(Transform root)
        {
            var steel = RuntimeMaterials.Lit(new Color(0.06f, 0.06f, 0.065f), 0.35f, 0.7f);
            float x0 = AlleyBuilder.BackAlleyX0, x1 = AlleyBuilder.BackAlleyX1, z = AlleyBuilder.YardZ0 + 0.05f, y = DistrictGenerator.Curb;
            Transform Leaf(string name, float from, float dir)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root, false);
                t.position = new Vector3(from, y, z);
                float w = (x1 - x0) / 2f;
                for (float u = 0.07f; u < w; u += 0.14f)
                    PrimitiveMeshes.Part(t, "Bar", PrimitiveType.Cube, new Vector3(dir * u, 1.1f, 0f), new Vector3(0.025f, 2.2f, 0.025f), steel, null, false);
                foreach (float h in new[] { 0.25f, 1.9f })
                    PrimitiveMeshes.Part(t, "Rail", PrimitiveType.Cube, new Vector3(dir * w / 2f, h, 0f), new Vector3(w, 0.06f, 0.04f), steel, null, false);
                var bc = t.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(dir * w / 2f, 1.1f, 0f);
                bc.size = new Vector3(w, 2.2f, 0.1f);
                return t;
            }
            gateL = Leaf("YardGateL", x0, 1f);
            gateR = Leaf("YardGateR", x1, -1f);
            // The chain where they meet.
            PrimitiveMeshes.Part(gateL, "Chain", PrimitiveType.Cube, new Vector3((x1 - x0) / 2f, 1.1f, -0.03f), new Vector3(0.12f, 0.3f, 0.05f), RuntimeMaterials.Lit(new Color(0.3f, 0.3f, 0.3f), 0.5f, 0.9f), null, false);
        }

        /// <summary>A reveller sleeping it off against the hoarding, his LED mask beside him.</summary>
        void Reveller(Transform root)
        {
            var rng = new System.Random(2626);
            var look = CharacterLook.Pick(Faction.Looter, rng);
            look.head = HeadKind.Face;
            look.hair = HairKind.Short;
            look.top = TopKind.OpenJacket;
            look.Dress(rng);
            reveller = HumanoidBuilder.Build(root, look, rng, out var revellerHealth);
            reveller.gameObject.name = "Reveller";
            revellerHealth.meleeImmune = true; // owner: no "[F] Strike" on him once the mask is taken
            Vector3 spotPos = new Vector3(PartyBuilder.BoxX1 - 0.8f, DistrictGenerator.Curb, PartyBuilder.BoxZ1 - 0.42f);
            reveller.transform.SetPositionAndRotation(spotPos, Quaternion.Euler(0f, 180f, 0f));
            var anim = reveller.gameObject.AddComponent<ProcAnimator>();
            anim.footIK = false;
            anim.pose = ProcAnimator.Pose.Slumped;
            anim.SnapToPose();
            StoryInteractable.Add(reveller.gameObject, "Take his mask and jacket", () => Started && !maskTaken && !talking, p => StartCoroutine(TakeMask()), 2.4f);

            // The mask on the ground by his hand: a black shell with a dotted LED grin.
            maskProp = new GameObject("PartyMask").transform;
            maskProp.SetParent(root, false);
            maskProp.SetPositionAndRotation(spotPos + new Vector3(-0.55f, 0.06f, -0.35f), Quaternion.Euler(-80f, 30f, 0f));
            PrimitiveMeshes.Part(maskProp, "Shell", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.19f, 0.25f, 0.09f), RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.02f), 0.6f));
            var ledMat = RuntimeMaterials.Lit(Color.black, 0.2f, 0f, new Color(1f, 0.8f, 0.15f) * 4f);
            for (int k = 0; k < 7; k++)
            {
                float u = k / 6f * 2f - 1f;
                PrimitiveMeshes.Part(maskProp, "Led", PrimitiveType.Cube, new Vector3(u * 0.06f, -0.04f - 0.025f * (1f - u * u), -0.045f), Vector3.one * 0.012f, ledMat, null, false);
            }
        }

        // ------------------------------------------------------------------ the crowd

        /// <summary>The party itself: sixteen in the crowd, four guards, the DJ (once Daniel is behind the hoarding).</summary>
        void SpawnParty()
        {
            if (spawned) return;
            spawned = true;
            Perception.PlayerConcealed = true; // in the crowd, the guards' eyes (below) decide, not their brains
            var rng = new System.Random(8080);
            for (int i = 0; i < 16; i++)
            {
                var pos = new Vector3(-93.3f + (i % 5) * 2.5f + Jit(rng, 0.7f), DistrictGenerator.Curb, -19.6f + (i / 5) * 2.55f + Jit(rng, 0.6f));
                var look = CharacterLook.Random(i % 4 == 3 ? Faction.Smiler : Faction.Looter, rng);
                var b = NPCSpawner.Create(look, rng, pos, 45f + Jit(rng, 40f), i % 3 == 2 ? NPCWeapon.Pistol : NPCWeapon.Bat, true, 0f);
                if (b == null) continue;
                Mob(b);
                crowd.Add(b);
                phase.Add((float)rng.NextDouble() * 6.3f);
            }
            var gpos = new[]
            {
                new Vector3(PartyBuilder.GapX0 - 0.7f, 0f, PartyBuilder.BarrierZ - 0.9f), new Vector3(PartyBuilder.GapX1 + 0.4f, 0f, PartyBuilder.BarrierZ - 0.9f),
                new Vector3(PartyBuilder.StageX0 + 1.4f, 0f, -15f), new Vector3(-88f, 0f, -15f),
            };
            float[] gyaw = { 200f, 160f, 270f, 0f };
            for (int i = 0; i < gpos.Length; i++)
            {
                var p = gpos[i];
                p.y = i == 2 ? DistrictGenerator.Curb + PartyBuilder.StageH : DistrictGenerator.Curb;
                var b = NPCSpawner.Create(CharacterLook.Random(Faction.Smiler, rng), rng, p, gyaw[i], NPCWeapon.Pistol, i != 3, 4f);
                if (b == null) continue;
                b.shotDamage = 12f;
                b.skill = 0.55f;
                guards.Add(b);
            }
            dj = NPCSpawner.Create(CharacterLook.Random(Faction.Smiler, rng), rng, PartyBuilder.DjSpot, 270f, NPCWeapon.Pistol, true, 0f);
            if (dj != null) dj.shotDamage = 10f;
            var w = new GameObject("CrowdWalla");
            w.transform.SetParent(transform, false);
            w.transform.position = CrowdCentre;
            ProcAudio(w, SoundId.CrowdWalla, 0.55f, 4f, 30f, out walla);
        }

        /// <summary>A partygoer who becomes the mob: they don't run (owner: everyone attacks), and they hit softer than a fighter.</summary>
        static void Mob(NPCBrain b)
        {
            b.courage = 0.95f;
            b.skill = 0.3f;
            b.meleeDamage = 12f;
            b.shotDamage = 8f;
            b.maxFireRange = 30f;
        }

        static float Jit(System.Random r, float a) => ((float)r.NextDouble() * 2f - 1f) * a;

        void ProcAudio(GameObject go, SoundId id, float vol, float min, float max, out AudioSource src)
        {
            ProceduralAudioSource.Attach(go, id, vol, min, max);
            src = go.GetComponent<AudioSource>();
        }

        void StartMusic()
        {
            if (music != null) return;
            var m = new GameObject("PartyMusic");
            m.transform.SetParent(transform, false);
            m.transform.position = new Vector3(PartyBuilder.StageX0 + 0.5f, DistrictGenerator.Curb + 2.5f, -12f);
            ProcAudio(m, SoundId.PartyMusic, 0.9f, 7f, 75f, out music);
        }

        static int Alive(List<NPCBrain> list)
        {
            int n = 0;
            foreach (var b in list) if (b != null && b.State != NPCState.Dead) n++;
            return n;
        }

        // ------------------------------------------------------------------ the scene

        public IEnumerator Play(MetroSequence metroSeq)
        {
            metro = metroSeq;
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            rig = player.GetComponent<Player.CameraRig>();
            input = player.GetComponent<Player.EchoesInput>();
            body = player.GetComponent<Player.FPSController>();
            pistol = player.GetComponent<WeaponPistol>();
            playerHealth = player.GetComponent<Health>();
            var hud = BodycamHUD.Instance;

            // The music carries down into the station.
            while (!metro.Reached && restoreStage < CheckpointStage.Party) yield return null;
            if (restoreStage >= CheckpointStage.School) yield break;
            StartMusic();
            while (!metro.Done) yield return null;
            SpawnParty();
            Started = true;
            if (restoreStage == CheckpointStage.Turret) { yield return TurretRestart(); yield break; }

            // Behind the hoarding, a step from the party.
            yield return new WaitForSeconds(1.2f);
            yield return Speak(DemoLines.PartyUp);
            if (hud != null)
            {
                hud.SetObjective("Through the party - the school door");
                hud.ShowHint("The man asleep by the hoarding: take his mask and jacket (F).", 6f);
            }
            StartCoroutine(Ambience());
            while (!maskTaken)
            {
                if (!saidMaskSeen && Vector3.Distance(player.transform.position, reveller.transform.position) < 2.6f) { saidMaskSeen = true; StartCoroutine(Speak(DemoLines.MaskSeen)); }
                if (Time.time > nextNoMask && Vector3.Distance(player.transform.position, PartyBuilder.BoxDoor) < 1.8f)
                {
                    nextNoMask = Time.time + 12f;
                    StartCoroutine(Speak(DemoLines.NoMask));
                }
                yield return null;
            }

            // Through the crowd, the VIP gap at the stage end, along the facade to the door.
            while (!revealed)
            {
                if (InVipByTheDoor()) StartCoroutine(Reveal(false));
                yield return null;
            }
        }

        IEnumerator TakeMask()
        {
            talking = true;
            var hud = BodycamHUD.Instance;
            if (input != null) input.InputLocked = true;
            if (pistol != null) pistol.Stowed = true;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { if (hud != null) hud.SetFade(t / 0.35f * 0.9f); yield return null; }
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Bandage, 0.8f, 0.8f);
            if (maskProp != null) Destroy(maskProp.gameObject);
            yield return new WaitForSeconds(1.1f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { if (hud != null) hud.SetFade(0.9f * (1f - t / 0.35f)); yield return null; }
            if (hud != null) hud.SetFade(0f);
            if (input != null) input.InputLocked = false;
            if (pistol != null) pistol.Stowed = false;
            maskTaken = true;
            talking = false;
            if (SuspicionEye.Instance != null) SuspicionEye.Instance.Set(0f, true);
            if (hud != null)
            {
                hud.ShowHint("Masked. Walk, don't run; keep the gun holstered; don't stare at the guards in LED grins. The eye at the top fills as they get suspicious. The way into the VIP strip is by the stage.", 11f);
                hud.SetObjective("The school door - through the VIP gap by the stage");
            }
            yield return Speak(DemoLines.MaskOn);
        }

        /// <summary>The DJ on the mic now and then, and the crowd talking about tonight when Daniel walks past.</summary>
        IEnumerator Ambience()
        {
            float nextHype = Time.time + 6f, nextTalk = Time.time + 4f;
            while (!revealed)
            {
                if (!talking && Time.time > nextHype && dj != null && dj.State != NPCState.Dead)
                {
                    nextHype = Time.time + Random.Range(22f, 32f);
                    StartCoroutine(Speak(DemoLines.PartyHype(hypeLines++).At(dj.transform)));
                }
                if (!talking && maskTaken && Time.time > nextTalk)
                {
                    NPCBrain near = null;
                    float best = 3.8f;
                    foreach (var b in crowd)
                    {
                        if (b == null || b.State == NPCState.Dead) continue;
                        float d = Vector3.Distance(b.transform.position, player.transform.position);
                        if (d < best) { best = d; near = b; }
                    }
                    if (near != null)
                    {
                        nextTalk = Time.time + Random.Range(5f, 8f);
                        StartCoroutine(Speak(DemoLines.PartyTalk(talkLines++).At(near.transform)));
                    }
                }
                yield return null;
            }
        }

        bool InVipByTheDoor()
        {
            Vector3 p = player.transform.position;
            return p.z > PartyBuilder.BarrierZ + 0.2f && p.x < PartyBuilder.GapX0 - 2.5f && p.y < DistrictGenerator.Curb + 1f;
        }

        // ------------------------------------------------------------------ the guards' eyes

        void Update()
        {
            if (!spawned || player == null) return;
            Dance();
            if (revealed) { TrackSpot(); return; }
            if (!maskTaken) return;

            Vector3 chest = player.transform.position + Vector3.up * 1.3f;
            Transform cam = rig != null ? rig.cam.transform : null;
            float total = 0f;
            NPCBrain watcher = null;
            foreach (var g in guards)
            {
                if (g == null || g.State == NPCState.Dead) continue;
                var grig = g.GetComponent<HumanoidRig>();
                Vector3 eye = grig != null && grig.eyes != null ? grig.eyes.position : g.transform.position + Vector3.up * 1.6f;
                float d = Vector3.Distance(eye, chest);
                if (d > 11f) continue;
                Vector3 toPlayer = (chest - eye) / d;
                if (d > 4f && Vector3.Angle(g.transform.forward, toPlayer) > 75f) continue;
                if (!Sees(eye, chest, g.transform)) continue;
                float rate = 0f;
                if (body != null && body.IsSprinting) rate += 0.9f;
                if (body != null && body.IsCrouching) rate += 0.5f;
                if (pistol != null && pistol.IsDrawn) rate += 1.4f;
                if (cam != null && d < 8f && Vector3.Angle(cam.forward, eye - cam.position) < 9f) rate += 0.45f; // staring at him
                if (d < 2.5f) rate += 0.15f;                                                                          // brushing past him
                if (rate <= 0f) continue;
                total += rate * Mathf.Lerp(1.4f, 0.4f, d / 11f);
                watcher = g;
            }
            suspicion = total > 0f ? suspicion + total * Time.deltaTime * 0.5f : Mathf.Max(0f, suspicion - 0.12f * Time.deltaTime);
            if (watcher != null)
            {
                if (watcher.TryGetComponent(out ProcAnimator wa)) wa.lookTarget = chest + Vector3.up * 0.3f;
                if (suspicion > 0.45f && Time.time > nextWary && !talking)
                {
                    nextWary = Time.time + 8f;
                    StartCoroutine(Speak(DemoLines.GuardWary(waryLines++).At(watcher.transform)));
                }
            }
            if (SuspicionEye.Instance != null) SuspicionEye.Instance.Set(suspicion, true);
            if (suspicion >= 1f) Caught(watcher);
        }

        /// <summary>The first thing a ray from the guard's eye meets is Daniel (people in between hide him).</summary>
        bool Sees(Vector3 eye, Vector3 target, Transform guard)
        {
            Vector3 d = target - eye;
            float best = float.MaxValue;
            Transform first = null;
            foreach (var h in Physics.RaycastAll(eye, d.normalized, d.magnitude + 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(guard)) continue;
                if (h.distance < best) { best = h.distance; first = h.collider.transform; }
            }
            return first != null && first.IsChildOf(player.transform);
        }

        void OnAnyDamaged(Health h, DamageInfo info)
        {
            // A shot or a blow from the man in the mask gives him away at once.
            if (info.fromPlayer && maskTaken && !revealed) Caught(null);
        }

        void OnNoise(NoiseEvent ev)
        {
            if (!ev.fromPlayer || !maskTaken || revealed) return;
            if (ev.kind == NoiseKind.Gunshot) Caught(null);
            else if (ev.kind == NoiseKind.Impact) suspicion += 0.3f; // a bottle smashing in the crowd
        }

        void Caught(NPCBrain by)
        {
            if (revealed) return;
            if (by == null)
            {
                float best = float.MaxValue;
                foreach (var g in guards)
                {
                    if (g == null || g.State == NPCState.Dead) continue;
                    float d = Vector3.Distance(g.transform.position, player.transform.position);
                    if (d < best) { best = d; by = g; }
                }
            }
            StartCoroutine(Reveal(true, by));
        }

        void Dance()
        {
            if (revealed) return;
            float t = Time.time * (124f / 60f) * Mathf.PI; // half-time with the music
            Vector3 screenPos = PartyBuilder.ScreenCentre;
            for (int i = 0; i < crowd.Count; i++)
            {
                var b = crowd[i];
                if (b == null || b.State != NPCState.Idle || !b.TryGetComponent(out ProcAnimator a)) continue;
                float s = Mathf.Sin(t * 0.25f + phase[i]);
                a.pose = s > 0.82f ? ProcAnimator.Pose.HandsUp : ProcAnimator.Pose.Normal;
                a.lookTarget = (i % 2 == 0 ? screenPos : PartyBuilder.DjSpot + Vector3.up * 1.6f);
            }
            if (dj != null && dj.State == NPCState.Idle && dj.TryGetComponent(out ProcAnimator da))
                da.pose = Mathf.Sin(t * 0.5f) > 0.6f ? ProcAnimator.Pose.HandsUp : ProcAnimator.Pose.Normal;
        }

        void TrackSpot()
        {
            if (spot == null || !spot.enabled) return;
            Vector3 aim = player.transform.position + Vector3.up * 1.2f;
            spot.transform.rotation = Quaternion.Slerp(spot.transform.rotation, Quaternion.LookRotation(aim - spot.transform.position), 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        // ------------------------------------------------------------------ the reveal

        IEnumerator Reveal(bool caught, NPCBrain by = null)
        {
            if (revealed) yield break;
            revealed = true;
            var hud = BodycamHUD.Instance;
            var am = AudioManager.Instance;
            if (SuspicionEye.Instance != null) SuspicionEye.Instance.Set(0f, false);
            if (music != null) music.Stop();
            if (am != null) am.Play2D(SoundId.TapeSkip, 0.5f, 0.6f);
            if (walla != null) walla.volume = 0.12f;
            if (hud != null) hud.Glitch(0.35f);
            if (rig != null) screen.ShowFeed(rig.cam);
            spot.enabled = true;
            spot.transform.rotation = Quaternion.LookRotation(player.transform.position - spot.transform.position);
            foreach (var b in crowd)
                if (b != null && b.TryGetComponent(out ProcAnimator a)) { a.pose = ProcAnimator.Pose.Normal; a.lookTarget = player.transform.position + Vector3.up * 1.5f; }
            if (hud != null)
            {
                hud.SetObjective("The gun on the pickup");
                hud.ShowHint("The machine gun on the pickup by the door: get on it (F)!", 8f);
            }

            if (caught)
            {
                if (by != null) yield return Speak(DemoLines.GuardCaught.At(by.transform));
                Engage();
                yield return Speak(DemoLines.CaughtVance.At(screen.transform));
                yield break;
            }
            // Vance takes his time: he has an audience. Daniel can go for the gun while he talks.
            if (am != null) am.Play2D(SoundId.TensionHit, 0.6f);
            var lines = DemoLines.Reveal;
            for (int i = 0; i < lines.Length && !turret.Mounted; i++) yield return Speak(lines[i].At(screen.transform));
            if (!turret.Mounted) yield return Speak(DemoLines.RevealThought);
            yield return Speak(DemoLines.RevealGo.At(screen.transform));
            Engage();
        }

        void Engage()
        {
            if (engaged) return;
            engaged = true;
            Perception.PlayerConcealed = false;
            foreach (var b in crowd) if (b != null) b.Engage();
            foreach (var b in guards) if (b != null) b.Engage();
            if (dj != null) dj.Engage();
            if (walla != null) walla.Stop();
            var r = new GameObject("CrowdRoar");
            r.transform.SetParent(transform, false);
            r.transform.position = CrowdCentre;
            ProcAudio(r, SoundId.CrowdRoar, 0.75f, 5f, 45f, out roar);
        }

        // ------------------------------------------------------------------ the gun

        void MountGun()
        {
            if (turret.Mounted) return;
            turret.Mount(player, 180f);
            if (!OnTurret)
            {
                OnTurret = true;
                StartCoroutine(Fight());
            }
        }

        /// <summary>Back from a death on the gun: the party already turned, Daniel already on it.</summary>
        IEnumerator TurretRestart()
        {
            maskTaken = boxDoorOpen = true;
            OpenBoxDoor(false);
            if (maskProp != null) Destroy(maskProp.gameObject);
            yield return null;
            revealed = true;
            if (music != null) music.Stop();
            if (rig != null) screen.ShowFeed(rig.cam);
            spot.enabled = true;
            MountGun(); // Fight gives the restart's fade-in a moment before they come
        }

        IEnumerator Fight()
        {
            var hud = BodycamHUD.Instance;
            var am = AudioManager.Instance;
            if (restoreStage == CheckpointStage.Turret) yield return new WaitForSeconds(2.6f);
            Engage();
            if (hud != null)
            {
                hud.SetObjective("Hold them off");
                hud.ShowHint("Left click: fire. Right click: steady. The belt never runs out.", 6f);
            }
            StartCoroutine(Speak(DemoLines.GunThought));

            // Wave one: the party.
            var mob = new List<NPCBrain>(crowd);
            mob.AddRange(guards);
            if (dj != null) mob.Add(dj);
            float t0 = Time.time;
            bool taunted = false;
            while (Alive(mob) > 3 && Time.time - t0 < 75f)
            {
                if (!taunted && Time.time - t0 > 10f) { taunted = true; StartCoroutine(Speak(DemoLines.TurretTaunt.At(screen.transform))); }
                if (roar != null) roar.volume = Mathf.Lerp(0.15f, 0.75f, Alive(mob) / (float)Mathf.Max(1, mob.Count));
                yield return null;
            }

            // Wave two: out of the backstage door, onto the stage, down into the yard.
            yield return Speak(DemoLines.WaveTwo.At(screen.transform));
            if (am != null) am.PlayAt(SoundId.DoorSlam, PartyBuilder.BackstageDoor + Vector3.up, 0.9f, 0.9f, 3f, 60f);
            var rng = new System.Random(9191);
            for (int i = 0; i < 6; i++)
            {
                var p = PartyBuilder.BackstageDoor + new Vector3(-1.2f - (i % 2) * 0.9f, 0f, (i % 3 - 1) * 1.1f);
                var b = NPCSpawner.Create(CharacterLook.Random(Faction.Smiler, rng), rng, p, 270f, i % 3 == 0 ? NPCWeapon.Bat : NPCWeapon.Pistol, false, 0f);
                if (b != null)
                {
                    b.engageOnSpawn = true;
                    b.shotDamage = 11f;
                    b.meleeDamage = 14f;
                    waveTwo.Add(b);
                    mob.Add(b);
                }
                yield return new WaitForSeconds(0.7f);
            }
            t0 = Time.time;
            while (Alive(mob) > 1 && Time.time - t0 < 50f)
            {
                if (roar != null) roar.volume = Mathf.Lerp(0.1f, 0.5f, Alive(mob) / 8f);
                yield return null;
            }
            if (roar != null) roar.volume = 0.1f;

            // The finale: the ram truck.
            yield return Truck();
        }

        // ------------------------------------------------------------------ the ram truck

        IEnumerator Truck()
        {
            var am = AudioManager.Instance;
            var hud = BodycamHUD.Instance;
            StartCoroutine(Speak(DemoLines.TruckComing.At(screen.transform)));
            yield return new WaitForSeconds(1.5f);
            var truck = VehicleFactory.RamTruck(transform);
            var health = truck.gameObject.AddComponent<Health>();
            health.maxHealth = 650f;
            health.SetHealth(650f);
            var drive = truck.gameObject.AddComponent<ScriptedVehicle>();
            drive.engineSound = SoundId.EngineTruck;
            drive.engineVolume = 1f;
            drive.idlePitch = 0.9f;
            drive.pitchPerSpeed = 0.06f;
            drive.acceleration = 4f;
            drive.SetPath(TruckRoute, 6f);
            drive.targetSpeed = 8f;
            truck.SetHeadlights(true);
            if (truck.headlights != null && truck.headlights.Length > 0 && truck.headlights[0] != null) truck.headlights[0].shadows = LightShadows.Soft;
            if (am != null) am.PlayAt(SoundId.CarHorn, TruckRoute[0] + Vector3.up, 1f, 0.9f, 5f, 120f);
            if (hud != null) hud.ShowHint("The truck! Stop it before it reaches you!", 4f);

            bool smoking = false;
            while (true)
            {
                if (!gateSmashed && truck.transform.position.z > AlleyBuilder.YardZ0 - 3f) SmashGate(truck.transform.forward);
                if (!smoking && health.Fraction < 0.5f && DemoDirector.Instance != null)
                {
                    smoking = true;
                    FireFX.Fire(DemoDirector.Instance.fx, truck.body, truck.transform.TransformPoint(new Vector3(0f, 1.5f, 2.1f)), 0.6f, false, false);
                }
                if (health.IsDead) { yield return Explode(truck, drive); break; }
                if (drive.Remaining < 0.4f)
                {
                    // It got through: it buries the pickup and the man on it.
                    if (am != null) am.PlayAt(SoundId.CrashImpact, truck.transform.position + Vector3.up, 1f, 1f, 5f, 120f);
                    if (rig != null) rig.AddTrauma(1f);
                    yield return Explode(truck, drive);
                    if (playerHealth != null && !playerHealth.IsDead)
                        playerHealth.ApplyDamage(new DamageInfo { amount = 999f, point = player.transform.position + Vector3.up, direction = truck.transform.forward, source = truck.gameObject });
                    yield break;
                }
                yield return null;
            }

            yield return new WaitForSeconds(1.6f);
            turret.Dismount();
            if (roar != null) roar.Stop();
            Perception.PlayerConcealed = true; // whoever is left has had enough
            if (hud != null) hud.SetObjective("The school door");
            Done = true;
            yield return Speak(DemoLines.TruckDown);
        }

        void SmashGate(Vector3 dir)
        {
            gateSmashed = true;
            var am = AudioManager.Instance;
            Vector3 g = new Vector3((AlleyBuilder.BackAlleyX0 + AlleyBuilder.BackAlleyX1) / 2f, DistrictGenerator.Curb + 1f, AlleyBuilder.YardZ0);
            if (am != null) am.PlayAt(SoundId.CrashImpact, g, 1f, 1.1f, 4f, 100f);
            foreach (var leaf in new[] { gateL, gateR })
            {
                if (leaf == null) continue;
                var rb = leaf.gameObject.AddComponent<Rigidbody>();
                rb.mass = 60f;
                rb.AddForce((dir + Vector3.up * 0.4f) * 900f + Random.insideUnitSphere * 150f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 200f, ForceMode.Impulse);
            }
            if (rig != null) rig.AddTrauma(0.15f);
        }

        IEnumerator Explode(VehicleRig truck, ScriptedVehicle drive)
        {
            var am = AudioManager.Instance;
            Vector3 c = truck.transform.TransformPoint(new Vector3(0f, 1.2f, 0.5f));
            drive.targetSpeed = 0f;
            drive.braking = 14f;
            truck.SetHeadlights(false);
            if (am != null) am.PlayAt(SoundId.Explosion, c, 1f, 1f, 10f, 300f);
            if (DemoDirector.Instance != null) FireFX.Fire(DemoDirector.Instance.fx, truck.transform, c + Vector3.up * 0.5f, 1.7f, true, false);
            truck.body.localRotation = Quaternion.Euler(-5f, 0f, 7f);
            if (rig != null) rig.AddTrauma(0.6f);
            // The blast takes whoever is next to it.
            foreach (var col in Physics.OverlapSphere(c, 6f))
                if (col.TryGetComponent(out Hitbox hb) && hb.owner != null && hb.owner.gameObject != player)
                    hb.Hit(new DamageInfo { amount = 300f, point = col.transform.position, direction = (col.transform.position - c).normalized, force = 900f, source = player, fromPlayer = true });
            var lg = new GameObject("Blast", typeof(Light));
            lg.transform.position = c + Vector3.up;
            var l = lg.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.3f);
            l.range = 30f;
            l.shadows = LightShadows.None;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { l.intensity = Mathf.Lerp(40f, 0f, t / 0.8f); yield return null; }
            Destroy(lg);
        }

        // ------------------------------------------------------------------ checkpoints

        /// <summary>
        /// A restart at or past the party: Party = behind the hoarding (as if just up); Turret = on the gun with the
        /// party turned on him; School or later = the party over (nobody there, the music off).
        /// </summary>
        public void RestoreAfterCheckpoint(CheckpointStage stage)
        {
            restoreStage = stage;
            if (stage < CheckpointStage.School) return;
            Started = OnTurret = Done = true;
            revealed = engaged = true;
        }

        /// <summary>One voice at a time in this scene.</summary>
        IEnumerator Speak(params Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Conversation(lines);
            talking = false;
        }
    }
}
