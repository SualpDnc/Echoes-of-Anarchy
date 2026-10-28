using System;
using UnityEngine;
using UnityEngine.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Rendering;

namespace Echoes.AI
{
    /// <summary>
    /// The blind Smiler in the subway (8c): blindfolded, in bullet-proof plates (Armored), he hunts by sound alone. He
    /// walks a patrol humming the music-box tune through his mask (the player can track him by ear); a sound sends him to
    /// look (a thrown bottle to where it broke), close or loud enough ones (running, landing, a gunshot, being shot) make
    /// him hunt: he runs at the last thing he heard, goes quiet, swings his pipe when he is on top of Daniel, and gives up
    /// after a stretch of silence. Light means nothing to him. Crouched, slow footsteps only carry a couple of metres.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig))]
    public class BlindHunter : MonoBehaviour
    {
        public enum Mode { Patrol, Investigate, Listen, Hunt, Hold }

        [Tooltip("Walls cut a sound's reach in half; this widens what reaches him through the air (he lives by his ears).")]
        public float hearing = 1.35f;
        [Tooltip("Hunting he is faster than Daniel sprints (5.4): running away from him doesn't work (owner).")]
        public float walkSpeed = 1.1f, searchSpeed = 1.8f, huntSpeed = 6.2f;
        [Tooltip("One blow nearly finishes Daniel and throws him; the second kills (owner: running must be deadly).")]
        public float meleeDamage = 70f, meleeReach = 1.6f, meleeCooldown = 1.4f;
        public Vector3[] route;

        public Mode State { get; private set; } = Mode.Patrol;
        /// <summary>A sound turned him toward Daniel (the sequence uses it for a line or a hint).</summary>
        public event Action<Mode> ModeChanged;

        NavMeshAgent agent;
        ProcAnimator anim;
        AudioSource hum;
        Transform player;
        Health playerHealth;
        Vector3 heard;
        float suspicion, lastHeard, modeTime, nextSwing, nextStep, listenLook, pauseUntil = -1f;
        int routeIndex;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<ProcAnimator>();
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { player = p.transform; playerHealth = p.GetComponent<Health>(); }
            ProceduralAudioSource.Attach(gameObject, SoundId.ArmorHum, 0.55f, 2f, 20f);
            hum = GetComponent<AudioSource>();
            if (TryGetComponent(out Armored armour)) armour.Struck += OnStruck;
            SetMode(Mode.Patrol);
        }

        void OnEnable() => NoiseSystem.Emitted += OnNoise;
        void OnDisable() => NoiseSystem.Emitted -= OnNoise;

        /// <summary>Stand and wait (a scripted moment); Release() lets him go back to work.</summary>
        public void Hold() => SetMode(Mode.Hold);
        public void Release() => SetMode(Mode.Patrol);

        /// <summary>Hunt toward a point now (the sequence forces it when Daniel forces the gate).</summary>
        public void HuntAt(Vector3 p)
        {
            heard = p;
            lastHeard = Time.time;
            suspicion = 1.2f;
            SetMode(Mode.Hunt);
        }

        void SetMode(Mode m)
        {
            if (State == m && modeTime > 0f) return;
            State = m;
            modeTime = 0f;
            if (hum != null) hum.mute = m == Mode.Hunt; // he goes silent when he comes for you
            if (m == Mode.Patrol && route != null && route.Length > 0 && agent.isOnNavMesh) { pauseUntil = -1f; agent.SetDestination(route[routeIndex]); }
            ModeChanged?.Invoke(m);
        }

        // ------------------------------------------------------------------ hearing

        void OnNoise(NoiseEvent ev)
        {
            if (State == Mode.Hold || ev.source == gameObject) return;
            Vector3 ears = transform.position + Vector3.up * 1.6f;
            float r = ev.radius * hearing;
            float d = Vector3.Distance(ears, ev.position);
            if (d > r) return;
            if (Physics.Linecast(ears, ev.position + Vector3.up * 0.3f, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore)
                && !h.collider.transform.IsChildOf(transform) && (player == null || !h.collider.transform.IsChildOf(player)))
            {
                r *= 0.5f;
                if (d > r) return;
            }
            float strength = 1f - d / r;
            bool thrown = ev.kind == NoiseKind.Impact && ev.source != null && ev.source.GetComponent<Bottle>() != null;
            switch (ev.kind)
            {
                case NoiseKind.Footstep:
                    suspicion += 0.25f + 0.55f * strength;
                    break;
                case NoiseKind.Impact:
                    // A bottle breaking: something is there. He goes to the glass, not to Daniel.
                    suspicion = Mathf.Max(suspicion, thrown ? 0.75f : 1.1f);
                    break;
                default:
                    suspicion = 1.2f;
                    break;
            }
            heard = ev.position;
            lastHeard = Time.time;
            if (suspicion >= 1f) SetMode(Mode.Hunt);
            else if (State != Mode.Hunt) SetMode(Mode.Investigate);
        }

        void OnStruck(DamageInfo info)
        {
            // Shot or hit: he knows exactly where from.
            HuntAt(info.source != null ? info.source.transform.position : info.point);
        }

        // ------------------------------------------------------------------ behaviour

        void Update()
        {
            if (player == null || !agent.isOnNavMesh) return;
            float dt = Time.deltaTime;
            modeTime += dt;
            suspicion = Mathf.Max(0f, suspicion - 0.12f * dt);
            float toPlayer = Vector3.Distance(transform.position, player.position);
            bool dead = playerHealth != null && playerHealth.IsDead;

            // Bumping into someone is not a sound, but he feels it.
            if (!dead && toPlayer < 0.9f && State != Mode.Hold) HuntAt(player.position);

            switch (State)
            {
                case Mode.Hold:
                    agent.isStopped = true;
                    anim.lookTarget = null;
                    break;

                case Mode.Patrol:
                    anim.lookTarget = null;
                    if (route == null || route.Length == 0) { agent.isStopped = true; break; }
                    if (!agent.hasPath && !agent.pathPending) agent.SetDestination(route[routeIndex]);
                    if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.6f)
                    {
                        // A pause at each point, head cocked, listening.
                        if (pauseUntil < 0f) pauseUntil = Time.time + 2.5f;
                        if (Time.time > pauseUntil)
                        {
                            pauseUntil = -1f;
                            routeIndex = (routeIndex + 1) % route.Length;
                            agent.SetDestination(route[routeIndex]);
                        }
                    }
                    if (pauseUntil > 0f) agent.isStopped = true;
                    else Walk(walkSpeed);
                    break;

                case Mode.Investigate:
                    Walk(searchSpeed);
                    agent.SetDestination(heard);
                    anim.lookTarget = heard + Vector3.up * 1.2f;
                    if (!agent.pathPending && agent.remainingDistance < 1f) SetMode(Mode.Listen);
                    break;

                case Mode.Listen:
                    agent.isStopped = true;
                    listenLook += dt;
                    anim.lookTarget = transform.position + Quaternion.Euler(0f, Mathf.Sin(listenLook * 0.9f) * 70f, 0f) * transform.forward * 3f + Vector3.up * 1.5f;
                    if (modeTime > 4.5f) SetMode(Mode.Patrol);
                    break;

                case Mode.Hunt:
                    Walk(huntSpeed);
                    agent.SetDestination(heard);
                    anim.lookTarget = heard + Vector3.up * 1.4f;
                    if (!dead && toPlayer < meleeReach && Time.time > nextSwing) Swing();
                    // Lost him: nothing heard for a while and he stands where the last sound was.
                    if (Time.time - lastHeard > 3.5f && (!agent.pathPending && agent.remainingDistance < 1.2f))
                    {
                        suspicion = 0.6f;
                        SetMode(Mode.Listen);
                    }
                    break;
            }
            Steps(dt);
        }

        void Walk(float speed)
        {
            agent.isStopped = false;
            agent.speed = speed;
        }

        void Swing()
        {
            nextSwing = Time.time + meleeCooldown;
            StartCoroutine(SwingRoutine());
        }

        System.Collections.IEnumerator SwingRoutine()
        {
            anim.pose = ProcAnimator.Pose.Melee;
            var am = AudioManager.Instance;
            if (am != null) am.PlayAt(SoundId.BatSwing, transform.position + Vector3.up * 1.4f, 0.9f, 0.8f, 1f, 20f, false);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                anim.meleeProgress = t / 0.5f;
                yield return null;
            }
            if (player != null && playerHealth != null && Vector3.Distance(transform.position, player.position) < meleeReach + 0.4f)
            {
                playerHealth.ApplyDamage(new DamageInfo { amount = meleeDamage, point = player.position + Vector3.up * 1.4f, direction = transform.forward, source = gameObject });
                if (am != null) am.PlayAt(SoundId.BatHit, player.position + Vector3.up * 1.4f, 1f, 0.75f, 1f, 30f, false);
                StartCoroutine(Throw());
            }
            anim.meleeProgress = 0f;
            anim.pose = ProcAnimator.Pose.Normal;
        }

        /// <summary>The blow lifts Daniel off his feet: three metres back, the camera knocked about.</summary>
        System.Collections.IEnumerator Throw()
        {
            if (player == null || !player.TryGetComponent(out CharacterController cc)) yield break;
            if (player.TryGetComponent(out Player.CameraRig cam)) cam.AddTrauma(0.75f);
            Vector3 push = (player.position - transform.position);
            push.y = 0f;
            push = push.sqrMagnitude > 0.01f ? push.normalized : transform.forward;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                if (cc.enabled) cc.Move((push * 10f + Vector3.up * (1f - t / 0.3f) * 3f) * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>Heavy boots on concrete, and the plates knocking when he runs.</summary>
        void Steps(float dt)
        {
            float v = agent.velocity.magnitude;
            if (v < 0.3f || Time.time < nextStep) return;
            nextStep = Time.time + Mathf.Lerp(0.62f, 0.32f, Mathf.InverseLerp(1f, 4.3f, v));
            var am = AudioManager.Instance;
            if (am == null) return;
            am.PlayAt(SoundId.StepConcrete, transform.position, Mathf.Lerp(0.5f, 0.9f, v / 4.3f), 0.8f, 1.5f, 28f);
            if (v > 3f && UnityEngine.Random.value < 0.5f) am.PlayAt(SoundId.ImpactMetal, transform.position + Vector3.up * 1.2f, 0.12f, 1.6f, 1f, 15f);
        }

        // ------------------------------------------------------------------ spawning

        /// <summary>Builds him at a point on the NavMesh: a big Smiler in a suit, blindfolded, in plates, a steel pipe in hand.</summary>
        public static BlindHunter Spawn(Vector3 pos, float yaw, Vector3[] route, int seed)
        {
            if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas)) return null;
            var rng = new System.Random(seed);
            var look = CharacterLook.Pick(Faction.Smiler, rng);
            look.feminine = false;
            look.height = 1.08f;
            look.bulk = 1.25f;
            look.blindfold = true;
            look.armor = true;
            look.Dress(rng);
            var rig = HumanoidBuilder.Build(null, look, rng, out var health);
            var go = rig.gameObject;
            go.name = "Boone"; // Vance names him on the phone
            go.transform.SetPositionAndRotation(hit.position, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<Armored>();
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.9f;
            agent.angularSpeed = 300f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.3f;
            agent.Warp(hit.position);
            var anim = go.AddComponent<ProcAnimator>();
            anim.footIK = true;
            // The pipe.
            PrimitiveMeshes.Part(rig.weaponSocket, "Pipe", PrimitiveType.Cylinder, new Vector3(0f, -0.25f, 0.02f), new Vector3(0.035f, 0.42f, 0.035f),
                                 RuntimeMaterials.Lit(new Color(0.25f, 0.18f, 0.12f), 0.35f, 0.7f), null, true);
            var hunter = go.AddComponent<BlindHunter>();
            hunter.route = route;
            health.Heal(health.maxHealth);
            return hunter;
        }
    }
}
