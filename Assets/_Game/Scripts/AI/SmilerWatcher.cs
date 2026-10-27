using UnityEngine;
using UnityEngine.AI;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Rendering;

namespace Echoes.AI
{
    /// <summary>
    /// A scripted Smiler who only watches: stands at a distance facing Daniel, head light on, one of them holding a
    /// phone up to film. When the scene says so (or when shot at) he turns and walks off, then is removed once out
    /// of sight. No perception or combat: the first sighting is a promise, not a fight.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig), typeof(ProcAnimator), typeof(NavMeshAgent))]
    public class SmilerWatcher : MonoBehaviour
    {
        public Vector3 leaveTo;
        public bool filming;

        public bool Leaving { get; private set; }

        HumanoidRig rig;
        ProcAnimator anim;
        NavMeshAgent agent;
        Health health;
        Transform cam;
        float unseenFor;

        public static SmilerWatcher Spawn(Vector3 pos, float yaw, Vector3 leaveTo, bool filming, int seed)
        {
            var rig = HumanoidBuilder.Build(null, Faction.Smiler, seed, out var health);
            var go = rig.gameObject;
            go.name = "SmilerWatcher";
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas)) pos = hit.position;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.75f;
            agent.speed = 1.4f;
            agent.angularSpeed = 300f;
            agent.acceleration = 6f;
            agent.stoppingDistance = 0.5f;
            agent.Warp(pos);
            go.AddComponent<ProcAnimator>();
            var w = go.AddComponent<SmilerWatcher>();
            w.leaveTo = leaveTo;
            w.filming = filming;
            health.maxHealth = 120f;
            health.Heal(120f);
            return w;
        }

        void Awake()
        {
            rig = GetComponent<HumanoidRig>();
            anim = GetComponent<ProcAnimator>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            NPCBrain.AttachCarriedLight(rig, true);
            if (filming) Phone();
            agent.updateRotation = false;
        }

        /// <summary>A phone held up to film: dark slab, the screen toward him, its torch LED toward the subject.</summary>
        void Phone()
        {
            var socket = rig.weaponSocket;
            PrimitiveMeshes.Part(socket, "Phone", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0.04f), new Vector3(0.075f, 0.009f, 0.15f),
                                 RuntimeMaterials.Lit(new Color(0.02f, 0.02f, 0.022f), 0.85f), null, false);
            PrimitiveMeshes.Part(socket, "Screen", PrimitiveType.Cube, new Vector3(0f, -0.045f, 0.04f), new Vector3(0.065f, 0.002f, 0.135f),
                                 RuntimeMaterials.Lit(Color.black, 0.9f, 0f, new Color(0.6f, 0.75f, 1f) * 2f), null, false);
            PrimitiveMeshes.Part(socket, "Torch", PrimitiveType.Sphere, new Vector3(0.02f, -0.056f, 0.1f), Vector3.one * 0.012f,
                                 RuntimeMaterials.Lit(Color.white, 0.9f, 0f, Color.white * 12f), null, false);
        }

        public void Leave(bool run)
        {
            if (Leaving || health.IsDead) return;
            Leaving = true;
            filming = false;
            agent.updateRotation = true;
            agent.speed = run ? 4.4f : 1.4f;
            if (agent.isOnNavMesh) agent.SetDestination(leaveTo);
        }

        void Update()
        {
            if (health.IsDead || cam == null) return;
            if (!Leaving)
            {
                // Square up to him and watch; the one filming keeps the phone on him.
                Vector3 to = cam.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-2f * Time.deltaTime));
                anim.lookTarget = cam.position;
                anim.pose = filming ? ProcAnimator.Pose.Aim : ProcAnimator.Pose.Normal;
                anim.aimTarget = cam.position;
                return;
            }

            anim.pose = ProcAnimator.Pose.Normal;
            anim.lookTarget = null;
            // Gone once he is out of view (or has arrived).
            Vector3 vp = Camera.main != null ? Camera.main.WorldToViewportPoint(transform.position + Vector3.up) : Vector3.zero;
            bool inView = vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
            unseenFor = inView ? 0f : unseenFor + Time.deltaTime;
            bool arrived = !agent.pathPending && agent.remainingDistance < 1f;
            if (unseenFor > 3f || (arrived && !inView)) Destroy(gameObject);
        }

        void OnDamaged(DamageInfo info)
        {
            if (!health.IsDead) Leave(true);
        }

        void OnDied(DamageInfo info)
        {
            if (info.fromPlayer) Ledger.Record(LedgerAct.KilledHostile, "Smiler (watching)");
            anim.enabled = false;
            agent.enabled = false;
            rig.EnableRagdoll(Vector3.zero, info);
            enabled = false;
        }
    }
}
