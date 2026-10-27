using UnityEngine;
using UnityEngine.AI;

namespace Echoes.AI
{
    /// <summary>
    /// Spawns a group of procedural NPCs on the NavMesh at Start (after RuntimeNavMesh has baked).
    /// Characters are built at runtime so the saved scene stays small. Story scenes place single NPCs with `Create`.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        public Faction faction = Faction.Looter;
        public int count = 3;
        public float spread = 3f;
        [Range(0f, 1f)] public float pistolChance = 0.4f;
        public bool stationary = true;
        public float patrolRadius = 10f;
        public int seed = 1;

        void Start()
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = transform.position + new Vector3(((float)rng.NextDouble() - 0.5f) * 2f * spread, 0f, ((float)rng.NextDouble() - 0.5f) * 2f * spread);
                int npcSeed = rng.Next();
                float yaw = (float)rng.NextDouble() * 360f;
                var weapon = faction == Faction.Smiler ? NPCWeapon.Pistol : faction == Faction.Civilian ? NPCWeapon.None
                           : rng.NextDouble() < pistolChance ? NPCWeapon.Pistol : NPCWeapon.Bat;
                var npcRng = new System.Random(npcSeed);
                Create(CharacterLook.Random(faction, npcRng), npcRng, p, yaw, weapon, stationary, patrolRadius);
            }
        }

        /// <summary>
        /// Builds one NPC at `pos` on the NavMesh with the faction's defaults (health, skill, courage, ranges).
        /// Callers can change the brain's fields afterwards (before its Start runs, i.e. in the same frame).
        /// Returns null if there is no NavMesh nearby.
        /// </summary>
        public static NPCBrain Create(CharacterLook look, System.Random rng, Vector3 pos, float yaw, NPCWeapon weapon, bool stationary, float patrolRadius)
        {
            if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas)) return null;
            pos = hit.position;
            var rig = HumanoidBuilder.Build(null, look, rng, out var health);
            var go = rig.gameObject;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.75f;
            agent.angularSpeed = 420f;
            agent.acceleration = 9f;
            agent.stoppingDistance = 0.4f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.Warp(pos);

            go.AddComponent<Perception>();
            go.AddComponent<ProcAnimator>();
            var brain = go.AddComponent<NPCBrain>();
            brain.faction = look.faction;
            brain.home = pos;
            brain.stationary = stationary;
            brain.patrolRadius = patrolRadius;
            brain.weapon = weapon;
            switch (look.faction)
            {
                case Faction.Smiler:
                    health.maxHealth = 120f;
                    brain.skill = 0.55f + (float)rng.NextDouble() * 0.2f;
                    brain.courage = 0.95f;
                    brain.minRange = 8f; brain.maxRange = 18f;
                    break;
                case Faction.Civilian:
                    health.maxHealth = 80f;
                    brain.courage = 0.1f;
                    break;
                default:
                    brain.skill = 0.25f + (float)rng.NextDouble() * 0.2f;
                    brain.courage = 0.35f + (float)rng.NextDouble() * 0.25f;
                    brain.minRange = 5f; brain.maxRange = 13f;
                    break;
            }
            health.Heal(health.maxHealth); // maxHealth changed after Awake
            return brain;
        }
    }
}
