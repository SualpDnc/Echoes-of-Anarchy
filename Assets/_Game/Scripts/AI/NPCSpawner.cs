using UnityEngine;
using UnityEngine.AI;

namespace Echoes.AI
{
    /// <summary>
    /// Spawns a group of procedural NPCs on the NavMesh at Start (after RuntimeNavMesh has baked).
    /// Characters are built at runtime so the saved scene stays small.
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
                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 6f, NavMesh.AllAreas)) continue;
                Spawn(hit.position, rng.Next(), rng);
            }
        }

        NPCBrain Spawn(Vector3 pos, int npcSeed, System.Random rng)
        {
            var rig = HumanoidBuilder.Build(null, faction, npcSeed, out var health);
            var go = rig.gameObject;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));

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
            brain.faction = faction;
            brain.home = pos;
            brain.stationary = stationary;
            brain.patrolRadius = patrolRadius;
            switch (faction)
            {
                case Faction.Smiler:
                    health.maxHealth = 120f;
                    brain.weapon = NPCWeapon.Pistol;
                    brain.skill = 0.55f + (float)rng.NextDouble() * 0.2f;
                    brain.courage = 0.95f;
                    brain.minRange = 8f; brain.maxRange = 18f;
                    break;
                case Faction.Civilian:
                    health.maxHealth = 80f;
                    brain.weapon = NPCWeapon.None;
                    brain.courage = 0.1f;
                    break;
                default:
                    brain.weapon = rng.NextDouble() < pistolChance ? NPCWeapon.Pistol : NPCWeapon.Bat;
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
