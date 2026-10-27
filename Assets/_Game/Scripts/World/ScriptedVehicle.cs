using System.Collections.Generic;
using UnityEngine;
using Echoes.Audio;
using Echoes.Player;

namespace Echoes.World
{
    /// <summary>
    /// Drives a VehicleRig along a smooth path (Catmull-Rom through control points, walked by arc length). Speed eases
    /// toward targetSpeed and brakes for the end of the path. The body pitches under braking and rolls in turns,
    /// wheels spin, and the engine loop rises with speed. Story scripts set targetSpeed (or speed directly) and can
    /// stop driving to take over the transform (the crash).
    /// </summary>
    [RequireComponent(typeof(VehicleRig))]
    public class ScriptedVehicle : MonoBehaviour
    {
        public bool driving = true;
        public float targetSpeed = 6f;
        public float acceleration = 2.5f;
        public float braking = 5f;
        public bool stopAtEnd = true;
        [Tooltip("Daniel rides in this one: its speed drives the bodycam's engine and road buzz.")]
        public bool carriesPlayer;

        [Header("Suspension")]
        public float pitchPerAccel = 0.9f;   // degrees per m/s²
        public float rollPerLateral = 0.8f;  // degrees per m/s²

        [Header("Engine")]
        public SoundId engineSound = SoundId.EngineDiesel;
        public float engineVolume = 0.45f;
        public float idlePitch = 0.85f;
        public float pitchPerSpeed = 0.045f;

        public float Speed { get; set; }
        public float Distance { get; private set; }
        public float Length => cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;
        public float Remaining => Length - Distance;
        public AudioSource Engine { get; private set; }

        VehicleRig rig;
        readonly List<Vector3> samples = new List<Vector3>();
        readonly List<float> cumulative = new List<float>();
        int seg;
        float pitch, roll, wheelAngle, lastYaw;
        Vector3 bodyRest;
        Quaternion steerRest;
        float steer;

        void Awake()
        {
            rig = GetComponent<VehicleRig>();
            if (rig.body != null) bodyRest = rig.body.localPosition;
            if (rig.steeringWheel != null) steerRest = rig.steeringWheel.localRotation;
        }

        void Start()
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            Engine = gameObject.AddComponent<AudioSource>();
            Engine.clip = am.Clip(engineSound);
            Engine.loop = true;
            Engine.spatialBlend = 1f;
            Engine.minDistance = 3f;
            Engine.maxDistance = 60f;
            Engine.rolloffMode = AudioRolloffMode.Logarithmic;
            Engine.dopplerLevel = 0f;
            Engine.volume = engineVolume;
            Engine.time = Random.Range(0f, Engine.clip != null ? Engine.clip.length : 0f);
            Engine.Play();
        }

        /// <summary>Replaces the path and puts the vehicle at its start, facing along it.</summary>
        public void SetPath(IList<Vector3> points, float startSpeed)
        {
            samples.Clear();
            cumulative.Clear();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[Mathf.Max(0, i - 1)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(points.Count - 1, i + 2)];
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) / 0.5f));
                for (int k = 0; k < steps; k++) samples.Add(CatmullRom(p0, p1, p2, p3, k / (float)steps));
            }
            samples.Add(points[points.Count - 1]);
            float d = 0f;
            cumulative.Add(0f);
            for (int i = 1; i < samples.Count; i++) { d += Vector3.Distance(samples[i - 1], samples[i]); cumulative.Add(d); }
            Distance = 0f;
            seg = 0;
            Speed = startSpeed;
            Place();
            lastYaw = transform.eulerAngles.y;
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float prevSpeed = Speed;
            if (driving && samples.Count > 1)
            {
                float want = targetSpeed;
                // Brake for the end of the path so it rolls to a stop instead of overshooting.
                if (stopAtEnd) want = Mathf.Min(want, Mathf.Sqrt(2f * braking * 0.8f * Mathf.Max(0f, Remaining - 0.5f)));
                Speed = Mathf.MoveTowards(Speed, want, (want > Speed ? acceleration : braking) * dt);
                Distance = Mathf.Min(Length, Distance + Speed * dt);
                if (Distance >= Length) Speed = 0f;
                Place();
            }

            // Suspension: nose dips under braking, the body leans out of turns.
            float yaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(lastYaw, yaw) / dt * Mathf.Deg2Rad;
            lastYaw = yaw;
            float longAccel = (Speed - prevSpeed) / dt;
            float lateral = yawRate * Speed;

            // Steering wheel from the path's curvature: road angle = wheelbase × yaw rate / speed, about 16:1 at the wheel.
            if (rig.steeringWheel != null && driving)
            {
                float want = Speed > 0.5f ? Mathf.Clamp(yawRate * 4.6f / Speed * Mathf.Rad2Deg * 16f, -110f, 110f) : steer;
                steer = Mathf.Lerp(steer, want, 1f - Mathf.Exp(-5f * dt));
                rig.steeringWheel.localRotation = steerRest * Quaternion.Euler(0f, 0f, -steer);
            }
            pitch = Mathf.Lerp(pitch, Mathf.Clamp(-longAccel * pitchPerAccel, -4f, 4f), 1f - Mathf.Exp(-4f * dt));
            roll = Mathf.Lerp(roll, Mathf.Clamp(lateral * rollPerLateral, -5f, 5f), 1f - Mathf.Exp(-3f * dt));
            if (rig.body != null && driving)
            {
                float t = Time.time;
                float road = Speed * 0.002f;
                rig.body.localPosition = bodyRest + Vector3.up * (Mathf.PerlinNoise(t * 3.1f, 0.3f) - 0.5f) * road;
                rig.body.localRotation = Quaternion.Euler(pitch + (Mathf.PerlinNoise(t * 2.3f, 5f) - 0.5f) * road * 20f, 0f, roll);
            }

            wheelAngle += Speed * dt / Mathf.Max(0.1f, rig.wheelRadius) * Mathf.Rad2Deg;
            if (rig.wheels != null)
                foreach (var w in rig.wheels) if (w != null) w.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);

            if (Engine != null)
            {
                Engine.pitch = idlePitch + Speed * pitchPerSpeed + Mathf.Max(0f, longAccel) * 0.03f;
                Engine.volume = engineVolume * (0.8f + Mathf.Clamp01(Speed / 12f) * 0.4f);
            }

            // Riding inside: a fine engine and road buzz on the bodycam.
            if (carriesPlayer && Camera.main != null)
            {
                var camRig = Camera.main.GetComponentInParent<CameraRig>();
                if (camRig != null && camRig.IsMounted) camRig.Vibration = 0.02f + Mathf.Clamp01(Speed / 12f) * 0.08f;
            }
        }

        void Place()
        {
            while (seg < cumulative.Count - 2 && cumulative[seg + 1] < Distance) seg++;
            float segLen = Mathf.Max(1e-4f, cumulative[seg + 1] - cumulative[seg]);
            float u = Mathf.Clamp01((Distance - cumulative[seg]) / segLen);
            Vector3 pos = Vector3.Lerp(samples[seg], samples[seg + 1], u);
            // Heading from a point a little ahead, so turns are smooth rather than segment-by-segment.
            Vector3 ahead = PointAt(Distance + 1.5f), behind = PointAt(Distance - 1.5f);
            Vector3 dir = ahead - behind;
            dir.y = 0f;
            transform.position = pos;
            if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        /// <summary>Arc-length position on the path nearest to a world point.</summary>
        public float DistanceOf(Vector3 p)
        {
            float best = float.MaxValue, d = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                float s = (samples[i] - p).sqrMagnitude;
                if (s < best) { best = s; d = cumulative[i]; }
            }
            return d;
        }

        public Vector3 PointAt(float d)
        {
            d = Mathf.Clamp(d, 0f, Length);
            int i = 0;
            while (i < cumulative.Count - 2 && cumulative[i + 1] < d) i++;
            float segLen = Mathf.Max(1e-4f, cumulative[i + 1] - cumulative[i]);
            return Vector3.Lerp(samples[i], samples[i + 1], Mathf.Clamp01((d - cumulative[i]) / segLen));
        }
    }
}
