using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Rendering;

namespace Echoes.AI
{
    public enum NPCState { Idle, Patrol, Suspicious, Search, Attack, Flee, Surrender, Cower, Dead }

    /// <summary>
    /// Behaviour state machine for Purge-night NPCs. Looters break and surrender when hurt and covered by a gun;
    /// Smilers press the attack; civilians run and hide. Ranged NPCs fire in bursts with reaction delays and
    /// reload every 7 rounds — real openings for the player. Every death is written to the Ledger.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Perception), typeof(ProcAnimator))]
    public class NPCBrain : MonoBehaviour
    {
        public static readonly List<NPCBrain> All = new List<NPCBrain>();
        public static event System.Action<NPCBrain> AnyDied;
        /// <summary>Raised when an NPC gives up (story scenes give him a line and the player the choice).</summary>
        public static event System.Action<NPCBrain> AnySurrendered;

        [Header("Identity")]
        public Faction faction = Faction.Looter;
        public NPCWeapon weapon = NPCWeapon.Pistol;
        [Range(0f, 1f)] public float skill = 0.5f;
        [Range(0f, 1f)] public float courage = 0.5f;

        [Header("Territory")]
        public Vector3 home;
        public float patrolRadius = 8f;
        public bool stationary;
        [Tooltip("Starts hidden and cowering (a civilian behind a counter).")]
        public bool startCowering;

        [Header("Combat")]
        public float minRange = 6f, maxRange = 16f;
        [Tooltip("Never opens fire beyond this — fights happen where the player can see them.")]
        public float maxFireRange = 26f;
        public float shotDamage = 22f;
        public float meleeDamage = 24f;

        [Header("Story chase")]
        [Tooltip("Runs Daniel down wherever he is and, until he hurts one of them, only shoots to scare and hits to hurt (the Smilers want him alive, on camera).")]
        public bool hunting;
        [Tooltip("Arrives already fighting: alerted to the player on spawn (reinforcement waves).")]
        public bool engageOnSpawn;
        /// <summary>Daniel has hurt one of the hunters: from now on they shoot to hit.</summary>
        public bool HuntProvoked { get; private set; }

        public NPCState State { get; private set; }

        NavMeshAgent agent;
        Perception perception;
        ProcAnimator anim;
        HumanoidRig rig;
        Health health;
        Transform player;
        Health playerHealth;
        WeaponPistol playerGun;
        GameObject weaponGo;
        Transform muzzle;
        Light muzzleLight;

        float stateTime, waitUntil, nextShot, reactionReadyAt, nextStrafe, lostSight;
        int burstLeft, roundsLeft = 7;
        int shotsThisEngagement;
        Light carriedLight;
        bool meleeSwinging, meleeHit;
        float meleeT;
        Vector3 investigate;
        int searchPoints;
        float morale;
        NPCState stateBeforeDeath;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<Perception>();
            anim = GetComponent<ProcAnimator>();
            rig = GetComponent<HumanoidRig>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            perception.BecameAlerted += OnAlerted;
        }

        void OnEnable()
        {
            All.Add(this);
            AnyDied += OnAllyDied;
        }

        void OnDisable()
        {
            All.Remove(this);
            AnyDied -= OnAllyDied;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerHealth = p.GetComponent<Health>();
                playerGun = p.GetComponent<WeaponPistol>();
            }
            morale = courage; // set here: spawners configure courage after Awake
            if (home == Vector3.zero) home = transform.position;
            Arm();
            CarryLight();
            if ((hunting || engageOnSpawn) && player != null) { perception.ForceAlert(player.position); SetState(NPCState.Attack); }
            else SetState(startCowering ? NPCState.Cower : stationary ? NPCState.Idle : NPCState.Patrol);
        }

        /// <summary>Gives up on the spot (story scenes make sure someone does).</summary>
        public void Surrender()
        {
            if (State != NPCState.Dead && State != NPCState.Surrender) SetState(NPCState.Surrender);
        }

        void Arm()
        {
            if (weapon == NPCWeapon.None) return;
            if (weapon == NPCWeapon.Pistol)
            {
                var gun = WeaponModels.Pistol(rig.weaponSocket, false);
                gun.root.localRotation = Quaternion.Euler(90f, 0f, 0f);
                gun.root.localPosition = new Vector3(0f, 0.01f, 0.01f);
                weaponGo = gun.root.gameObject;
                muzzle = gun.muzzle;
                var lg = new GameObject("MuzzleLight", typeof(Light));
                lg.transform.SetParent(muzzle, false);
                muzzleLight = lg.GetComponent<Light>();
                muzzleLight.type = LightType.Point;
                muzzleLight.color = new Color(1f, 0.72f, 0.42f);
                muzzleLight.range = 9f;
                muzzleLight.intensity = 7f;
                muzzleLight.enabled = false;
            }
            else
            {
                weaponGo = WeaponModels.Bat(rig.weaponSocket);
                weaponGo.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                weaponGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            }
        }

        /// <summary>
        /// Smilers wear a head-mounted stream light (with a red LIVE tally); armed looters carry a phone light on
        /// the chest. Realistic for the night — and it is what lets the player spot them before they spot him.
        /// </summary>
        void CarryLight()
        {
            if (faction == Faction.Civilian || (weapon == NPCWeapon.Bat && Random.value < 0.5f)) return;
            carriedLight = AttachCarriedLight(rig, faction == Faction.Smiler);
        }

        /// <summary>Head stream light with a LIVE tally (Smilers) or chest phone light (looters). Also used by scripted NPCs.</summary>
        public static Light AttachCarriedLight(HumanoidRig rig, bool smiler)
        {
            Transform mount = smiler ? rig[Bone.Head] : rig[Bone.Chest];
            var go = new GameObject("CarriedLight", typeof(Light));
            go.transform.SetParent(mount, false);
            go.transform.localPosition = smiler ? new Vector3(0.05f, 0.23f, 0.08f) : new Vector3(0.09f, 0.22f, 0.13f);
            go.transform.localRotation = Quaternion.Euler(smiler ? 6f : 12f, 0f, 0f);
            var light = go.GetComponent<Light>();
            light.type = LightType.Spot;
            light.color = smiler ? new Color(0.85f, 0.92f, 1f) : new Color(1f, 0.97f, 0.92f);
            light.range = 18f;
            light.spotAngle = 60f;
            light.innerSpotAngle = 22f;
            light.intensity = smiler ? 4.5f : 3.2f;
            light.shadows = LightShadows.None;
            // Visible lens so the source itself reads at a distance (bloom does the rest).
            PrimitiveMeshes.Part(go.transform, "Lens", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.01f), Vector3.one * 0.03f,
                                 RuntimeMaterials.Lit(Color.white, 0.9f, 0f, new Color(1f, 0.97f, 0.92f) * 9f), null, false);
            if (smiler)
                PrimitiveMeshes.Part(go.transform, "LiveTally", PrimitiveType.Sphere, new Vector3(-0.04f, 0.01f, 0f), Vector3.one * 0.014f,
                                     RuntimeMaterials.Lit(Color.red, 0.9f, 0f, new Color(1f, 0.05f, 0.03f) * 10f), null, false);
            return light;
        }

        // ------------------------------------------------------------------ state machine

        void SetState(NPCState s)
        {
            if (State == NPCState.Dead) return;
            State = s;
            stateTime = 0f;
            anim.pose = ProcAnimator.Pose.Normal;
            anim.aimTarget = null;
            if (agent.isOnNavMesh) agent.isStopped = false;
            agent.updateRotation = true;
            switch (s)
            {
                case NPCState.Idle: agent.speed = 1.2f; waitUntil = Time.time + Random.Range(2f, 6f); break;
                case NPCState.Patrol: agent.speed = 1.3f; PickPatrolPoint(); break;
                case NPCState.Suspicious: agent.speed = 1.6f; investigate = perception.StimulusPos; Stop(); break;
                case NPCState.Search: agent.speed = 2.2f; searchPoints = 3; Go(perception.LastSeenPos); break;
                case NPCState.Attack: reactionReadyAt = Time.time + Random.Range(0.75f, 1.3f) * (1.6f - skill); burstLeft = 0; lostSight = 0f; shotsThisEngagement = 0; break;
                case NPCState.Flee: agent.speed = 4.6f; FleeFrom(player != null ? player.position : transform.position); break;
                case NPCState.Surrender: Stop(); DropWeapon(); gameObject.AddComponent<SpareInteractable>().brain = this; AnySurrendered?.Invoke(this); break;
                case NPCState.Cower: Stop(); anim.pose = ProcAnimator.Pose.Cower; break;
            }
        }

        void Update()
        {
            if (State == NPCState.Dead || player == null || !agent.isOnNavMesh) return;
            stateTime += Time.deltaTime;
            anim.lookTarget = null;
            // Hunters never lose interest or go looking: they know where he is.
            if (hunting && (State == NPCState.Idle || State == NPCState.Patrol || State == NPCState.Suspicious || State == NPCState.Search)) SetState(NPCState.Attack);

            switch (State)
            {
                case NPCState.Idle: TickIdle(); break;
                case NPCState.Patrol: TickPatrol(); break;
                case NPCState.Suspicious: TickSuspicious(); break;
                case NPCState.Search: TickSearch(); break;
                case NPCState.Attack: TickAttack(); break;
                case NPCState.Flee: TickFlee(); break;
                case NPCState.Surrender: TickSurrender(); break;
                case NPCState.Cower: TickCower(); break;
            }
        }

        bool ReactToThreat()
        {
            if (perception.Alerted)
            {
                SetState(faction == Faction.Civilian || weapon == NPCWeapon.None ? NPCState.Flee : NPCState.Attack);
                return true;
            }
            if (perception.Suspicious && State != NPCState.Suspicious)
            {
                if (faction == Faction.Civilian) { SetState(NPCState.Cower); return true; }
                SetState(NPCState.Suspicious);
                return true;
            }
            return false;
        }

        void TickIdle()
        {
            if (ReactToThreat()) return;
            // Shift weight, look around, take a step now and then.
            if (Time.time > waitUntil)
            {
                waitUntil = Time.time + Random.Range(3f, 8f);
                if (Random.value < 0.4f) Go(home + Random.insideUnitSphere.Flat() * 1.5f);
            }
            if (Random.value < 0.01f) anim.lookTarget = transform.position + Random.insideUnitSphere.Flat() * 5f + Vector3.up * 1.6f;
        }

        void TickPatrol()
        {
            if (ReactToThreat()) return;
            if (!agent.pathPending && agent.remainingDistance < 0.6f)
            {
                if (waitUntil < 0f) waitUntil = Time.time + Random.Range(2f, 5f);
                else if (Time.time > waitUntil) { waitUntil = -1f; PickPatrolPoint(); }
            }
        }

        void TickSuspicious()
        {
            if (perception.Alerted) { ReactToThreat(); return; }
            if ((perception.StimulusPos - investigate).sqrMagnitude > 4f) { investigate = perception.StimulusPos; stateTime = Mathf.Min(stateTime, 1.2f); }
            anim.lookTarget = investigate + Vector3.up * 1.2f;
            if (stateTime < 1.3f) { FaceTowards(investigate, 4f); return; } // freeze and look first
            if (stateTime < 1.4f) Go(investigate);
            if (!agent.pathPending && agent.remainingDistance < 1.2f)
            {
                // Look around at the spot.
                agent.isStopped = true;
                transform.Rotate(0f, Mathf.Sin(stateTime * 1.3f) * 60f * Time.deltaTime, 0f);
                if (stateTime > 9f && !perception.Suspicious) SetState(stationary ? NPCState.Idle : NPCState.Patrol);
                if (stateTime > 14f) { perception.Calm(); SetState(stationary ? NPCState.Idle : NPCState.Patrol); }
            }
        }

        void TickSearch()
        {
            if (perception.CanSeePlayer && perception.Alerted) { SetState(NPCState.Attack); return; }
            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                if (searchPoints-- <= 0 || stateTime > 25f) { perception.Calm(); SetState(stationary ? NPCState.Idle : NPCState.Patrol); return; }
                Go(perception.LastSeenPos + Random.insideUnitSphere.Flat() * 8f);
            }
        }

        void TickAttack()
        {
            Vector3 target = player.position;
            Vector3 chest = target + Vector3.up * 1.35f;
            float dist = Vector3.Distance(transform.position, target);
            anim.lookTarget = chest;

            if (perception.CanSeePlayer) lostSight = 0f;
            else lostSight += Time.deltaTime;
            if (lostSight > 4f && !hunting) { SetState(NPCState.Search); return; }
            if (playerHealth != null && playerHealth.IsDead) { agent.isStopped = true; anim.pose = ProcAnimator.Pose.Normal; return; }

            if (weapon == NPCWeapon.Bat) { TickMelee(target, dist); return; }

            if (!perception.CanSeePlayer)
            {
                agent.speed = hunting ? 4.8f : 3.8f;
                Go(hunting ? target : perception.LastSeenPos);
                anim.pose = ProcAnimator.Pose.Normal;
                return;
            }

            // Range management + occasional strafe so they don't stand like targets. Hunters keep closing, gun up.
            agent.speed = 2.6f;
            if (hunting) { agent.speed = dist > 10f ? 4.2f : 2.8f; if (dist > 5f) Go(target); else Stop(); }
            else if (dist > maxRange) Go(target);
            else if (dist < minRange * 0.6f) Go(transform.position + (transform.position - target).normalized * 4f);
            else if (Time.time > nextStrafe)
            {
                nextStrafe = Time.time + Random.Range(2f, 4.5f);
                if (Random.value < 0.55f) Go(transform.position + Vector3.Cross(Vector3.up, target - transform.position).normalized * Random.Range(-3.5f, 3.5f));
                else Stop();
            }
            agent.updateRotation = false;
            FaceTowards(target, 7f);
            anim.pose = ProcAnimator.Pose.Aim;
            anim.aimTarget = chest;

            if (Time.time < reactionReadyAt || Time.time < nextShot) return;
            if (dist > maxFireRange) { Go(target); return; }
            if (roundsLeft <= 0)
            {
                // Reload: a 2 s window for the player.
                roundsLeft = 7;
                nextShot = Time.time + 2.1f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.MagOut, transform.position + Vector3.up, 0.6f, 1f, 1f, 18f);
                return;
            }
            if (burstLeft <= 0) burstLeft = Random.Range(1, 4);
            Shoot(chest, dist);
            burstLeft--;
            nextShot = Time.time + (burstLeft > 0 ? Random.Range(0.28f, 0.5f) : Random.Range(0.9f, 2f) * (1.4f - skill));
        }

        void TickMelee(Vector3 target, float dist)
        {
            agent.speed = hunting ? 4.8f : 4.3f;
            if (meleeSwinging)
            {
                meleeT += Time.deltaTime / 0.6f;
                anim.pose = ProcAnimator.Pose.Melee;
                anim.meleeProgress = meleeT;
                FaceTowards(target, 10f);
                if (!meleeHit && meleeT > 0.55f)
                {
                    meleeHit = true;
                    if (dist < 2.1f && Vector3.Angle(transform.forward, target - transform.position) < 50f && playerHealth != null)
                    {
                        playerHealth.ApplyDamage(new DamageInfo { amount = hunting && !HuntProvoked ? meleeDamage * 0.5f : meleeDamage, point = target + Vector3.up * 1.4f, direction = transform.forward, source = gameObject });
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.BatHit, target + Vector3.up * 1.4f, 1f, 1f, 1f, 30f, false);
                    }
                }
                if (meleeT >= 1f) { meleeSwinging = false; anim.pose = ProcAnimator.Pose.Normal; nextShot = Time.time + Random.Range(0.6f, 1.2f); agent.isStopped = false; }
                return;
            }
            Go(target);
            if (dist < 1.9f && Time.time > nextShot && Time.time > reactionReadyAt)
            {
                meleeSwinging = true;
                meleeHit = false;
                meleeT = 0f;
                agent.isStopped = true;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.BatSwing, transform.position + Vector3.up * 1.4f, 0.8f, Random.Range(0.9f, 1.1f), 1f, 20f, false);
            }
        }

        void TickFlee()
        {
            if (!agent.pathPending && agent.remainingDistance < 1.2f)
            {
                if (player != null && Vector3.Distance(transform.position, player.position) < 25f && stateTime < 20f) FleeFrom(player.position);
                else SetState(NPCState.Cower);
            }
        }

        void TickCower()
        {
            anim.pose = ProcAnimator.Pose.Cower;
            if (faction == Faction.Civilian) return; // civilians stay hidden
            if (stateTime > 25f && !perception.Alerted) SetState(NPCState.Patrol);
        }

        void TickSurrender()
        {
            anim.pose = stateTime < 1.3f ? ProcAnimator.Pose.HandsUp : ProcAnimator.Pose.Kneel;
            anim.lookTarget = player.position + Vector3.up * 1.6f;
            FaceTowards(player.position, 2f);
            // Left alone long enough, he takes the chance and runs.
            if (Vector3.Distance(transform.position, player.position) > 30f && stateTime > 12f) Spare(false);
        }

        // ------------------------------------------------------------------ actions

        void Shoot(Vector3 aimPoint, float dist)
        {
            roundsLeft--;
            Vector3 origin = muzzle != null ? muzzle.position : rig.eyes.position;
            var am = AudioManager.Instance;
            if (am != null) am.PlayAt(SoundId.GunPistolNear, origin, 1f, Random.Range(0.93f, 1.05f), 6f, 300f);
            NoiseSystem.Emit(origin, 80f, NoiseKind.Gunshot, gameObject, false);
            if (muzzleLight != null) StartCoroutine(Flash());

            // Hit chance: skill, how visible the player is, his movement, range.
            float moving = player.TryGetComponent(out Player.FPSController body) ? body.Speed01 : 0f;
            float chance = (0.18f + skill * 0.4f) * Mathf.Lerp(0.5f, 1f, perception.PlayerVisibility) * Mathf.Lerp(1f, 0.55f, moving) * Mathf.Clamp01(1.1f - dist / 30f);
            // The opening shots of a fight always miss: the crack past his ear tells the player where it came from.
            if (shotsThisEngagement++ < 2) chance = 0f;
            if (hunting && !HuntProvoked) chance = 0f; // herding: every round cracks past him
            if (body != null && body.IsCrouching) chance *= 0.8f;
            Vector3 dir;
            if (Random.value < chance)
            {
                dir = (aimPoint + Random.insideUnitSphere * 0.15f - origin).normalized;
            }
            else
            {
                // Deliberate miss: a near pass the player hears crack past.
                Vector3 miss = aimPoint + Random.onUnitSphere * Random.Range(0.6f, 1.6f);
                dir = (miss - origin).normalized;
                if (am != null && dist > 4f) am.PlayAt(SoundId.Whizz, Vector3.Lerp(aimPoint, miss, 0.5f), 0.9f, Random.Range(0.9f, 1.1f), 1f, 10f, false);
            }
            if (SurfaceUtil.RaycastIgnoring(origin, dir, 200f, transform, out RaycastHit hit))
            {
                var info = new DamageInfo { amount = shotDamage * Random.Range(0.8f, 1.2f), point = hit.point, direction = dir, force = 200f, source = gameObject };
                if (hit.collider.transform.IsChildOf(player)) playerHealth.ApplyDamage(info);
                else
                {
                    Hitbox.Damage(hit.collider, info);
                    ImpactFX.Spawn(hit, dir);
                }
            }
        }

        System.Collections.IEnumerator Flash()
        {
            muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.05f);
            muzzleLight.enabled = false;
        }

        void DropWeapon()
        {
            if (weaponGo == null) return;
            weaponGo.transform.SetParent(null, true);
            var col = weaponGo.AddComponent<BoxCollider>();
            col.size = weapon == NPCWeapon.Pistol ? new Vector3(0.03f, 0.13f, 0.19f) : new Vector3(0.07f, 0.85f, 0.07f);
            var rb = weaponGo.AddComponent<Rigidbody>();
            rb.mass = weapon == NPCWeapon.Pistol ? 0.8f : 1f;
            rb.linearVelocity = transform.forward * 1.2f + Vector3.up;
            PlayerCollision.Ignore(col);
            if (weapon == NPCWeapon.Pistol) weaponGo.AddComponent<AmmoPickup>().rounds = Mathf.Clamp(roundsLeft, 2, 7);
            if (muzzleLight != null) Destroy(muzzleLight.gameObject);
            weaponGo = null;
            muzzle = null;
            weapon = NPCWeapon.None;
        }

        /// <summary>Called by SpareInteractable (player chose mercy) or when he slips away on his own.</summary>
        public void Spare(bool byPlayer)
        {
            if (State != NPCState.Surrender) return;
            if (byPlayer) Ledger.Record(LedgerAct.SparedSurrendered, faction.ToString());
            if (TryGetComponent(out SpareInteractable s)) Destroy(s);
            perception.Calm();
            SetState(NPCState.Flee);
        }

        // ------------------------------------------------------------------ events

        void OnAlerted()
        {
            if (State == NPCState.Dead || State == NPCState.Surrender) return;
            // Stinger the first time anyone locks onto the player.
            if (faction != Faction.Civilian && weapon != NPCWeapon.None)
            {
                bool anyoneFighting = false;
                foreach (var b in All) if (b != this && b.State == NPCState.Attack) anyoneFighting = true;
                if (!anyoneFighting && AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.55f);
            }
        }

        void OnDamaged(DamageInfo info)
        {
            if (State == NPCState.Dead) return;
            anim.Flinch(info.direction);
            if (info.fromPlayer && player != null && State != NPCState.Surrender) perception.ForceAlert(player.position);
            if (hunting && info.fromPlayer) foreach (var b in All) if (b.hunting) b.HuntProvoked = true;
            morale -= info.amount / health.maxHealth * 0.9f;

            if (State == NPCState.Surrender || State == NPCState.Cower || faction == Faction.Smiler) { if (State != NPCState.Attack && faction == Faction.Smiler) SetState(NPCState.Attack); return; }
            if (faction == Faction.Civilian) { SetState(NPCState.Flee); return; }
            if (health.Fraction < 0.55f && morale < 0.35f) BreakMorale();
            else if (State != NPCState.Attack && weapon != NPCWeapon.None) SetState(NPCState.Attack);
        }

        void BreakMorale()
        {
            bool covered = playerGun != null && playerGun.IsAimingAt(transform.position + Vector3.up * 1.2f, 14f)
                           && Vector3.Distance(player.position, transform.position) < 16f;
            if (covered && Random.value < 0.75f) SetState(NPCState.Surrender);
            else if (Random.value < 0.7f) SetState(NPCState.Flee);
        }

        void OnAllyDied(NPCBrain other)
        {
            if (other == this || other.faction != faction || State == NPCState.Dead) return;
            if (Vector3.Distance(other.transform.position, transform.position) > 25f) return;
            morale -= 0.25f;
            if (faction == Faction.Looter && morale < 0.2f && State == NPCState.Attack && health.Fraction < 0.8f) BreakMorale();
        }

        void OnDied(DamageInfo info)
        {
            stateBeforeDeath = State;
            if (info.fromPlayer)
            {
                LedgerAct act = faction == Faction.Civilian ? LedgerAct.KilledCivilian
                              : stateBeforeDeath == NPCState.Surrender ? LedgerAct.KilledSurrendered
                              : stateBeforeDeath == NPCState.Flee || stateBeforeDeath == NPCState.Cower ? LedgerAct.KilledFleeing
                              : LedgerAct.KilledHostile;
                Ledger.Record(act, faction.ToString());
            }
            Vector3 vel = agent.enabled ? agent.velocity : Vector3.zero;
            State = NPCState.Dead;
            DropWeapon();
            agent.enabled = false;
            perception.enabled = false;
            anim.enabled = false;
            if (TryGetComponent(out SpareInteractable s)) Destroy(s);
            rig.EnableRagdoll(vel, info);
            ImpactFX.BloodPool(rig[Bone.Pelvis], health.transform);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.BodyFall, transform.position, 0.8f, Random.Range(0.9f, 1.05f), 1.5f, 30f);
            AnyDied?.Invoke(this);
            enabled = false;
        }

        // ------------------------------------------------------------------ navigation helpers

        void Go(Vector3 p)
        {
            if (!agent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        void Stop()
        {
            if (agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        }

        void PickPatrolPoint() => Go(home + Random.insideUnitSphere.Flat() * patrolRadius);

        void FleeFrom(Vector3 threat)
        {
            Vector3 away = (transform.position - threat).Flat().normalized;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = transform.position + Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * away * Random.Range(18f, 30f);
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 5f, NavMesh.AllAreas)) { agent.isStopped = false; agent.SetDestination(hit.position); return; }
            }
        }

        void FaceTowards(Vector3 p, float sharpness)
        {
            Vector3 d = (p - transform.position).Flat();
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }
    }

    static class VecExt
    {
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
