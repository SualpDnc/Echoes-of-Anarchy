using UnityEngine;
using Echoes.Combat;
using Echoes.Player;

namespace Echoes.AI
{
    /// <summary>
    /// Sight + hearing for one NPC. Builds an awareness value over time instead of binary detection:
    /// 0.3+ = suspicious (investigate), 1 = alerted (combat). Visibility depends on the light at the player,
    /// the flashlight, movement and crouching, so darkness is real cover.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig))]
    public class Perception : MonoBehaviour
    {
        /// <summary>Story beats (riding in the ambulance, lying unconscious) hide Daniel from every NPC.</summary>
        public static bool PlayerConcealed { get; set; }

        public float sightRange = 34f;
        public float fieldOfView = 120f;
        public float closeSense = 2.2f;
        public float awarenessRate = 1.5f;
        public float forgetTime = 20f;

        public float Awareness { get; private set; }
        public bool Alerted { get; private set; }
        public bool CanSeePlayer { get; private set; }
        public bool Suspicious => Awareness >= 0.3f;
        public Vector3 LastSeenPos { get; private set; }
        public Vector3 StimulusPos { get; private set; }
        public float PlayerVisibility { get; private set; }
        public float TimeSinceSeen { get; private set; } = 999f;
        public Transform Player => player;
        /// <summary>Raised when awareness first crosses into alert.</summary>
        public event System.Action BecameAlerted;

        HumanoidRig rig;
        Transform player;
        FPSController playerBody;
        Flashlight playerLight;
        Camera playerCam;
        float nextTick, accum;

        void Awake()
        {
            rig = GetComponent<HumanoidRig>();
            nextTick = Time.time + Random.value * 0.1f;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) { enabled = false; return; }
            player = p.transform;
            playerBody = p.GetComponent<FPSController>();
            playerLight = p.GetComponentInChildren<Flashlight>();
            playerCam = p.GetComponentInChildren<Camera>();
        }

        void OnEnable() => NoiseSystem.Emitted += OnNoise;
        void OnDisable() => NoiseSystem.Emitted -= OnNoise;

        void Update()
        {
            accum += Time.deltaTime;
            TimeSinceSeen += Time.deltaTime;
            if (Time.time < nextTick || player == null) return;
            nextTick = Time.time + 0.1f;
            float dt = accum;
            accum = 0f;
            if (PlayerConcealed) { CanSeePlayer = false; return; }
            Sight(dt);
            if (Alerted && TimeSinceSeen > forgetTime)
            {
                Alerted = false;
                Awareness = 0.5f;
            }
        }

        Vector3 PlayerChest => playerCam != null ? playerCam.transform.position : player.position + Vector3.up * 1.4f;

        void Sight(float dt)
        {
            Vector3 eyes = rig.eyes.position;
            Vector3 target = PlayerChest;
            Vector3 to = target - eyes;
            float dist = to.magnitude;
            Vector3 dir = to / Mathf.Max(0.001f, dist);

            // How visible is he right now?
            float vis = LightExposure.Visibility(target);
            bool flashOn = playerLight != null && playerLight.IsOn;
            bool beamOnMe = false;
            if (flashOn)
            {
                vis += 0.45f;
                Vector3 fromLight = eyes - playerLight.transform.position;
                beamOnMe = fromLight.magnitude < 26f && Vector3.Angle(playerLight.transform.forward, fromLight) < 22f;
            }
            if (playerBody != null)
            {
                vis += playerBody.Speed01 * 0.25f;
                if (playerBody.IsCrouching) vis *= 0.55f;
            }
            PlayerVisibility = Mathf.Clamp(vis, 0.04f, 1f);

            Vector3 look = rig.eyes.forward;
            look.y = 0f;
            Vector3 flat = dir; flat.y = 0f;
            float angle = Vector3.Angle(look, flat);
            bool inCone = angle < fieldOfView * 0.5f || dist < closeSense || beamOnMe;
            float effectiveRange = sightRange * (0.35f + 0.65f * PlayerVisibility);
            if (beamOnMe) effectiveRange = Mathf.Max(effectiveRange, 26f);

            CanSeePlayer = false;
            if (inCone && dist < effectiveRange && HasLineOfSight(eyes, dir, dist))
            {
                CanSeePlayer = true;
                LastSeenPos = player.position;
                StimulusPos = player.position;
                TimeSinceSeen = 0f;
                float central = angle < 30f ? 1f : 0.55f;
                float near = dist < 6f ? 3f : 1f;
                float gain = PlayerVisibility * (1.3f - dist / effectiveRange) * central * near * awarenessRate;
                if (beamOnMe) gain = Mathf.Max(gain, 2.5f);
                Raise(Awareness + gain * dt);
            }
            else if (!Alerted)
            {
                Awareness = Mathf.Max(0f, Awareness - 0.1f * dt);
            }
        }

        bool HasLineOfSight(Vector3 eyes, Vector3 dir, float dist)
        {
            if (!SurfaceUtil.RaycastIgnoring(eyes, dir, dist + 0.5f, transform, out RaycastHit hit)) return true;
            return hit.collider.transform.IsChildOf(player);
        }

        void OnNoise(NoiseEvent ev)
        {
            if (!enabled || player == null || ev.source == gameObject || (PlayerConcealed && ev.fromPlayer)) return;
            if (ev.source != null && ev.source.transform.IsChildOf(transform)) return;
            Vector3 ears = rig.eyes.position;
            float d = Vector3.Distance(ears, ev.position);
            float r = ev.radius;
            if (d > r) return;
            // Walls halve how far a sound carries.
            if (Physics.Linecast(ears, ev.position + Vector3.up * 0.3f, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore)
                && !h.collider.transform.IsChildOf(player) && !(ev.source != null && h.collider.transform.IsChildOf(ev.source.transform)))
            {
                r *= 0.5f;
                if (d > r) return;
            }
            float strength = 1f - d / r;
            switch (ev.kind)
            {
                case NoiseKind.Gunshot:
                    StimulusPos = ev.position;
                    if (ev.fromPlayer)
                    {
                        if (d < r * 0.4f) { LastSeenPos = ev.position; Raise(1f); }
                        else Raise(Mathf.Max(Awareness, 0.7f));
                    }
                    else Raise(Mathf.Max(Awareness, 0.6f)); // someone's shooting: go look
                    break;
                case NoiseKind.Impact:
                    StimulusPos = ev.position;
                    Raise(Mathf.Max(Awareness, 0.45f + strength * 0.35f));
                    break;
                case NoiseKind.Footstep:
                    StimulusPos = ev.position;
                    Raise(Awareness + 0.22f * strength);
                    break;
                case NoiseKind.Voice:
                    StimulusPos = ev.position;
                    Raise(Mathf.Max(Awareness, 0.5f));
                    break;
            }
        }

        void Raise(float value)
        {
            Awareness = Mathf.Clamp(value, 0f, 1.2f);
            if (Awareness >= 1f && !Alerted)
            {
                Alerted = true;
                if (TimeSinceSeen > 1f) LastSeenPos = StimulusPos;
                BecameAlerted?.Invoke();
            }
        }

        /// <summary>Instant alert, e.g. when shot.</summary>
        public void ForceAlert(Vector3 threatPos)
        {
            LastSeenPos = threatPos;
            StimulusPos = threatPos;
            TimeSinceSeen = Mathf.Min(TimeSinceSeen, 0.5f);
            Raise(1.2f);
        }

        public void Calm()
        {
            Alerted = false;
            Awareness = 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => PlayerConcealed = false;
    }
}
