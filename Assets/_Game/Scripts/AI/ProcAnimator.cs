using UnityEngine;
using UnityEngine.AI;
using Echoes.Combat;

namespace Echoes.AI
{
    /// <summary>
    /// Procedural animation for HumanoidRig, in layers:
    /// 1. Locomotion straight from the gait phase (no smoothing lag): hip/knee/ankle curves with push-off and heel
    ///    strike, pelvis bob/yaw/roll, counter-rotating torso, arm swing, plus idle breathing and weight shift,
    ///    leaning into acceleration and turns, and shuffling steps when turning on the spot.
    /// 2. Poses (Aim, HandsUp, Kneel, Cower, Melee, Seated, Slumped) blended in per bone, so legs keep walking while the arms aim.
    /// 3. Additive: hit flinch, head look with neck limits and gaze stabilisation.
    /// 4. Foot IK: two-bone leg IK onto the ground under each foot (curbs, debris), the pelvis drops to reach the
    ///    lower foot, and planted soles tilt to the ground normal.
    /// Bones rest at identity with limbs along −Y: +X rotation swings a limb backward, −X forward.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig))]
    public class ProcAnimator : MonoBehaviour
    {
        public enum Pose { Normal, Aim, HandsUp, Kneel, Cower, Melee, Seated, Slumped }

        public Pose pose = Pose.Normal;
        public Vector3? lookTarget;
        public Vector3? aimTarget;
        [Range(0f, 1f)] public float meleeProgress;

        [Header("Options")]
        [Tooltip("Overrides the NavMeshAgent speed (m/s) when ≥ 0; the character lineup uses it to walk in place.")]
        public float speedOverride = -1f;
        [Tooltip("Plants the feet on uneven ground (curbs, debris).")]
        public bool footIK = true;
        [Tooltip("No foot IK beyond this distance from the camera.")]
        public float ikDistance = 40f;

        [Header("Hand IK")]
        [Tooltip("Optional: wrists are pulled to these (hands on a steering wheel). Elbows keep the pose's bend.")]
        public Transform handTargetL, handTargetR;
        [Range(0f, 1f)] public float handIK = 1f;

        const int N = 16;
        static readonly RaycastHit[] hits = new RaycastHit[8];

        HumanoidRig rig;
        NavMeshAgent agent;
        readonly Quaternion[] loco = new Quaternion[N], target = new Quaternion[N], poseRot = new Quaternion[N];
        readonly float[] poseW = new float[N];
        readonly bool[] overridden = new bool[N];
        Vector3 pelvisRest;
        float footRestHeight;
        float phase, speed, crouch, clock, turnRate, prevYaw, ikPelvis;
        float pelvisYaw, spineYaw, chestYaw;
        Vector3 flinch, flinchVel, accel, prevVel;
        Quaternion head = Quaternion.identity;
        readonly Vector3[] ankles = new Vector3[2], normals = new Vector3[2];
        readonly float[] fkHeights = new float[2];

        void Awake()
        {
            rig = GetComponent<HumanoidRig>();
            TryGetComponent(out agent);
            for (int i = 0; i < N; i++) loco[i] = target[i] = poseRot[i] = Quaternion.identity;
            pelvisRest = rig[Bone.Pelvis].localPosition;
            footRestHeight = rig[Bone.FootL].position.y - transform.position.y;
            phase = Random.value * 10f;
            clock = Random.value * 100f;
            prevYaw = transform.eulerAngles.y;
        }

        bool snap;

        /// <summary>Jumps straight to the current pose on the next frame (no blend), e.g. for a character placed mid-scene.</summary>
        public void SnapToPose() => snap = true;

        public void Flinch(Vector3 worldDir)
        {
            Vector3 local = transform.InverseTransformDirection(worldDir);
            flinchVel += new Vector3(local.z * 220f, 0f, -local.x * 160f);
        }

        void LateUpdate()
        {
            if (rig.IsRagdoll) { enabled = false; return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            clock += dt;

            Vector3 vel = speedOverride >= 0f ? transform.forward * speedOverride : agent != null && agent.enabled ? agent.velocity : Vector3.zero;
            vel.y = 0f;
            speed = Mathf.Lerp(speed, vel.magnitude, 1f - Mathf.Exp(-8f * dt));
            float yaw = transform.eulerAngles.y;
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(prevYaw, yaw) / dt, 1f - Mathf.Exp(-6f * dt));
            prevYaw = yaw;
            Vector3 a = transform.InverseTransformDirection(vel - prevVel) / dt;
            prevVel = vel;
            accel = Vector3.Lerp(accel, new Vector3(Mathf.Clamp(a.x, -6f, 6f), 0f, Mathf.Clamp(a.z, -6f, 6f)), 1f - Mathf.Exp(-4f * dt));

            float run = Mathf.Clamp01((speed - 2f) / 2.2f);
            float moving = Mathf.Clamp01(speed / 1f);
            float shuffle = speed < 0.3f ? Mathf.Clamp01((Mathf.Abs(turnRate) - 40f) / 120f) : 0f; // small steps when turning on the spot
            float stride = Mathf.Lerp(1.35f, 2.3f, run); // matches the leg swing, so feet don't skate
            phase += (speed / stride * Mathf.PI * 2f + shuffle * 7f) * dt;
            float gait = Mathf.Max(moving, shuffle * 0.35f);

            float bob = Locomotion(gait, run, moving);
            float targetCrouch = PoseTargets();
            if (snap)
            {
                snap = false;
                crouch = targetCrouch;
                for (int i = 0; i < N; i++)
                    if (overridden[i]) { poseRot[i] = target[i]; poseW[i] = 1f; }
            }
            BlendPoses(dt);
            Additive(dt, gait);

            for (int i = 0; i < N; i++)
                if (rig.bones[i] != null) rig.bones[i].localRotation = loco[i];

            crouch = Mathf.Lerp(crouch, targetCrouch, 1f - Mathf.Exp(-6f * dt));
            float idle = 1f - gait;
            Vector3 shift = Vector3.right * 0.014f * Mathf.Sin(clock * 0.45f) * idle;
            rig[Bone.Pelvis].localPosition = pelvisRest + Vector3.down * (crouch + bob) + shift;

            float legPose = Mathf.Max(poseW[(int)Bone.UpperLegL], poseW[(int)Bone.UpperLegR]);
            float ikW = footIK ? 1f - legPose : 0f;
            var cam = Camera.main;
            if (cam != null && (cam.transform.position - transform.position).sqrMagnitude > ikDistance * ikDistance) ikW = 0f;
            FootIK(dt, ikW);
            HandIK();
        }

        void HandIK()
        {
            if (handIK <= 0.001f) return;
            Arm(handTargetL, Bone.UpperArmL, Bone.LowerArmL, Bone.HandL, -1f);
            Arm(handTargetR, Bone.UpperArmR, Bone.LowerArmR, Bone.HandR, 1f);
        }

        void Arm(Transform target, Bone upper, Bone lower, Bone hand, float side)
        {
            if (target == null) return;
            Transform h = rig[hand];
            // Elbows hang down and a little out.
            TwoBone(rig[upper], rig[lower], h, Vector3.Lerp(h.position, target.position, handIK), -transform.up + transform.right * side * 0.6f);
        }

        // ------------------------------------------------------------------ layer 1: locomotion

        /// <summary>Writes the walk/run/idle rotations into loco[] and returns the pelvis drop.</summary>
        float Locomotion(float g, float run, float moving)
        {
            for (int i = 0; i < N; i++) loco[i] = Quaternion.identity;
            float idle = 1f - g;
            float hipA = Mathf.Lerp(24f, 40f, run) * g;
            float kneeSwing = Mathf.Lerp(55f, 95f, run) * g;
            float kneeStance = Mathf.Lerp(8f, 30f, run) * g;
            for (int leg = 0; leg < 2; leg++)
            {
                float ph = phase + leg * Mathf.PI;
                float hip = -hipA * Mathf.Sin(ph);
                // Big knee flex in early swing, a small give after heel strike.
                float knee = 4f + kneeSwing * Pow(Mathf.Cos(ph + 0.5f), 2f) + kneeStance * Pow(Mathf.Cos(ph - 1.9f), 4f);
                // Sole stays level, toes push off before swing and lift before heel strike.
                float ankle = -(hip + knee) + 22f * g * Pow(Mathf.Cos(ph + 1.35f), 6f) - 10f * g * Pow(Mathf.Cos(ph - 1.35f), 4f);
                float splay = leg == 0 ? -2f : 2f;
                loco[(int)(leg == 0 ? Bone.UpperLegL : Bone.UpperLegR)] = Quaternion.Euler(hip, 0f, splay * idle);
                loco[(int)(leg == 0 ? Bone.LowerLegL : Bone.LowerLegR)] = Quaternion.Euler(knee, 0f, 0f);
                loco[(int)(leg == 0 ? Bone.FootL : Bone.FootR)] = Quaternion.Euler(ankle, 0f, 0f);
            }

            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float breath = Mathf.Sin(clock * Mathf.PI * 2f * 0.27f);
            float sway = Mathf.Sin(clock * 0.45f);

            // Pelvis: rotates with the forward leg, drops on the swing side, sways when standing.
            pelvisYaw = 7f * g * s;
            float pelvisRoll = 3f * g * c - 2.5f * sway * idle;
            loco[(int)Bone.Pelvis] = Quaternion.Euler(0f, pelvisYaw, pelvisRoll);

            // Torso counter-rotates; leans into acceleration, speed and turns.
            float leanFwd = Mathf.Clamp(3f + 10f * run + accel.z * 1.4f, -8f, 18f);
            float leanSide = Mathf.Clamp(-turnRate * speed * 0.025f - accel.x * 1.2f, -9f, 9f);
            spineYaw = -pelvisYaw * 0.55f;
            chestYaw = -pelvisYaw * 0.65f;
            loco[(int)Bone.Spine] = Quaternion.Euler(leanFwd * 0.6f, spineYaw, leanSide * 0.6f + 1.5f * sway * idle - pelvisRoll * 0.5f);
            loco[(int)Bone.Chest] = Quaternion.Euler(leanFwd * 0.4f + 1.2f * breath * (1f - 0.5f * g), chestYaw, leanSide * 0.4f);

            // Arms swing against the legs; elbows bend more when the arm comes forward and when running.
            float armA = Mathf.Lerp(20f, 45f, run) * g;
            float baseElbow = Mathf.Lerp(12f, 85f, run) * moving + 8f * idle;
            for (int arm = 0; arm < 2; arm++)
            {
                float side = arm == 0 ? -1f : 1f;
                float swing = armA * Mathf.Sin(phase - 0.2f) * (arm == 0 ? 1f : -1f);
                float fwd = Mathf.Max(0f, -swing / Mathf.Max(1f, armA));
                float abduct = side * (8f + 0.6f * breath * idle);
                loco[(int)(arm == 0 ? Bone.UpperArmL : Bone.UpperArmR)] = Quaternion.Euler(swing, 0f, abduct);
                loco[(int)(arm == 0 ? Bone.LowerArmL : Bone.LowerArmR)] = Quaternion.Euler(-(baseElbow + 18f * g * fwd), 0f, 0f);
                loco[(int)(arm == 0 ? Bone.HandL : Bone.HandR)] = Quaternion.Euler(-6f, 0f, side * 4f);
            }
            return Mathf.Lerp(0.03f, 0.05f, run) * g * s * s;
        }

        static float Pow(float x, float p) => Mathf.Pow(Mathf.Max(0f, x), p);

        // ------------------------------------------------------------------ layer 2: poses

        void Set(Bone b, Quaternion q)
        {
            target[(int)b] = q;
            overridden[(int)b] = true;
        }

        /// <summary>Fills target[] for the bones the current pose controls; returns the crouch depth.</summary>
        float PoseTargets()
        {
            for (int i = 0; i < N; i++) overridden[i] = false;
            switch (pose)
            {
                case Pose.Aim:
                {
                    // Weaver-ish stance: strong arm extended, support arm folded in; aim pitch from the target.
                    float pitch = 0f, yaw = 0f;
                    if (aimTarget.HasValue)
                    {
                        Vector3 local = transform.InverseTransformDirection(aimTarget.Value - rig[Bone.UpperArmR].position);
                        pitch = Mathf.Clamp(Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -50f, 50f);
                        yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -40f, 40f);
                    }
                    Set(Bone.UpperArmR, Quaternion.Euler(-88f - pitch, yaw - 8f, 0f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-4f, 0f, 0f));
                    Set(Bone.HandR, Quaternion.identity);
                    Set(Bone.UpperArmL, Quaternion.Euler(-78f - pitch, yaw + 32f, 0f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-38f, 0f, 0f));
                    Set(Bone.Spine, Quaternion.Euler(6f, yaw * 0.4f, 0f));
                    return 0.03f; // slight knee bend
                }
                case Pose.HandsUp:
                    Set(Bone.UpperArmL, Quaternion.Euler(-160f, 0f, -22f));
                    Set(Bone.UpperArmR, Quaternion.Euler(-160f, 0f, 22f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-35f, 0f, 0f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-35f, 0f, 0f));
                    Set(Bone.HandL, Quaternion.Euler(-10f, 0f, 0f));
                    Set(Bone.HandR, Quaternion.Euler(-10f, 0f, 0f));
                    return 0f;
                case Pose.Kneel:
                    Set(Bone.UpperLegL, Quaternion.Euler(-80f, 0f, -6f));
                    Set(Bone.UpperLegR, Quaternion.Euler(10f, 0f, 6f));
                    Set(Bone.LowerLegL, Quaternion.Euler(85f, 0f, 0f));
                    Set(Bone.LowerLegR, Quaternion.Euler(95f, 0f, 0f));
                    Set(Bone.FootL, Quaternion.Euler(-5f, 0f, 0f));
                    Set(Bone.FootR, Quaternion.Euler(-50f, 0f, 0f));
                    Set(Bone.UpperArmL, Quaternion.Euler(-165f, 0f, -18f));
                    Set(Bone.UpperArmR, Quaternion.Euler(-165f, 0f, 18f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-70f, 0f, 0f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-70f, 0f, 0f));
                    Set(Bone.Spine, Quaternion.Euler(-4f, 0f, 0f));
                    Set(Bone.Pelvis, Quaternion.identity);
                    return 0.42f;
                case Pose.Cower:
                    Set(Bone.UpperLegL, Quaternion.Euler(-75f, 0f, -10f));
                    Set(Bone.UpperLegR, Quaternion.Euler(-75f, 0f, 10f));
                    Set(Bone.LowerLegL, Quaternion.Euler(120f, 0f, 0f));
                    Set(Bone.LowerLegR, Quaternion.Euler(120f, 0f, 0f));
                    Set(Bone.FootL, Quaternion.Euler(-40f, 0f, 0f));
                    Set(Bone.FootR, Quaternion.Euler(-40f, 0f, 0f));
                    Set(Bone.Spine, Quaternion.Euler(40f, 0f, 0f));
                    Set(Bone.UpperArmL, Quaternion.Euler(-150f, 0f, -45f));
                    Set(Bone.UpperArmR, Quaternion.Euler(-150f, 0f, 45f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-110f, 0f, 0f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-110f, 0f, 0f));
                    Set(Bone.Pelvis, Quaternion.identity);
                    return 0.5f;
                case Pose.Melee:
                {
                    // Wind up over the shoulder, then a fast diagonal strike.
                    float p = meleeProgress;
                    float wind = p < 0.45f ? p / 0.45f : 1f - Mathf.Clamp01((p - 0.45f) / 0.25f);
                    float strike = Mathf.Clamp01((p - 0.45f) / 0.25f);
                    Set(Bone.UpperArmR, Quaternion.Euler(Mathf.Lerp(-20f, -170f, wind) + strike * 60f, 0f, Mathf.Lerp(10f, 40f, wind) - strike * 30f));
                    Set(Bone.LowerArmR, Quaternion.Euler(Mathf.Lerp(-30f, -90f, wind), 0f, 0f));
                    Set(Bone.UpperArmL, Quaternion.Euler(-40f * wind, 0f, -10f));
                    Set(Bone.Spine, Quaternion.Euler(10f * strike, Mathf.Lerp(0f, 25f, wind) - strike * 45f, 0f));
                    return 0.05f * wind;
                }
                case Pose.Seated:
                    // Driving: thighs forward, shins down to the pedals, leaning back a little, hands on the wheel.
                    Set(Bone.Pelvis, Quaternion.identity);
                    Set(Bone.UpperLegL, Quaternion.Euler(-82f, 0f, -4f));
                    Set(Bone.UpperLegR, Quaternion.Euler(-82f, 0f, 4f));
                    Set(Bone.LowerLegL, Quaternion.Euler(72f, 0f, 0f));
                    Set(Bone.LowerLegR, Quaternion.Euler(72f, 0f, 0f));
                    Set(Bone.FootL, Quaternion.Euler(10f, 0f, 0f));
                    Set(Bone.FootR, Quaternion.Euler(10f, 0f, 0f));
                    Set(Bone.Spine, Quaternion.Euler(-8f, 0f, 0f));
                    Set(Bone.UpperArmL, Quaternion.Euler(-45f, 0f, 6f));
                    Set(Bone.UpperArmR, Quaternion.Euler(-45f, 0f, -6f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-55f, 0f, 0f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-55f, 0f, 0f));
                    Set(Bone.HandL, Quaternion.Euler(-10f, 0f, 0f));
                    Set(Bone.HandR, Quaternion.Euler(-10f, 0f, 0f));
                    return 0.45f; // pelvis 0.45 m below standing: on a seat cushion
                case Pose.Slumped:
                    // Sitting on the ground against a wall, hurt: one knee up, the bad leg out straight, a hand pressed on it.
                    Set(Bone.Pelvis, Quaternion.identity);
                    Set(Bone.UpperLegL, Quaternion.Euler(-70f, 0f, -12f));
                    Set(Bone.LowerLegL, Quaternion.Euler(75f, 0f, 0f));
                    Set(Bone.FootL, Quaternion.Euler(-5f, 0f, 0f));
                    Set(Bone.UpperLegR, Quaternion.Euler(-88f, 0f, 10f));
                    Set(Bone.LowerLegR, Quaternion.Euler(8f, 0f, 0f));
                    Set(Bone.FootR, Quaternion.identity);
                    Set(Bone.Spine, Quaternion.Euler(-18f, 0f, 4f));
                    Set(Bone.Chest, Quaternion.Euler(-4f, 0f, 0f));
                    Set(Bone.UpperArmR, Quaternion.Euler(-30f, 0f, -8f));
                    Set(Bone.LowerArmR, Quaternion.Euler(-45f, 0f, 0f));
                    Set(Bone.UpperArmL, Quaternion.Euler(-15f, 0f, -28f));
                    Set(Bone.LowerArmL, Quaternion.Euler(-20f, 0f, 0f));
                    if (aimTarget.HasValue)
                    {
                        // Pointing the way with the right arm (wider yaw than Aim: he can point off to his side).
                        Vector3 local = transform.InverseTransformDirection(aimTarget.Value - rig[Bone.UpperArmR].position);
                        float pitch = Mathf.Clamp(Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -30f, 40f);
                        float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -30f, 110f);
                        Set(Bone.UpperArmR, Quaternion.Euler(-88f - pitch, yaw, 0f));
                        Set(Bone.LowerArmR, Quaternion.Euler(-6f, 0f, 0f));
                    }
                    return pelvisRest.y - 0.14f; // pelvis just above the ground
            }
            return 0f;
        }

        /// <summary>Per-bone weights fade poses in and out; pose-to-pose changes slerp, so nothing pops.</summary>
        void BlendPoses(float dt)
        {
            float k = 1f - Mathf.Exp(-(pose == Pose.Melee ? 30f : 9f) * dt);
            for (int i = 0; i < N; i++)
            {
                if (overridden[i])
                {
                    poseRot[i] = poseW[i] < 0.01f ? target[i] : Quaternion.Slerp(poseRot[i], target[i], k);
                    poseW[i] = Mathf.Lerp(poseW[i], 1f, k);
                }
                else poseW[i] = Mathf.Lerp(poseW[i], 0f, k);
                if (poseW[i] > 0.001f) loco[i] = Quaternion.Slerp(loco[i], poseRot[i], poseW[i]);
            }
        }

        // ------------------------------------------------------------------ layer 3: additive

        void Additive(float dt, float gait)
        {
            // Hit flinch spring on the spine.
            flinchVel += (-flinch * 200f - flinchVel * 16f) * dt;
            flinch += flinchVel * dt;
            loco[(int)Bone.Spine] *= Quaternion.Euler(flinch);

            // Head: track a look target within neck limits, otherwise keep the gaze steady against the torso's twist.
            Quaternion want;
            if (lookTarget.HasValue)
            {
                Transform chest = rig[Bone.Chest];
                Vector3 local = chest.InverseTransformPoint(lookTarget.Value) - rig[Bone.Head].localPosition;
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -35f, 40f);
                want = Quaternion.Euler(pitch, yaw, 0f);
                loco[(int)Bone.Chest] *= Quaternion.Euler(0f, yaw * 0.2f, 0f); // the shoulders turn a little too
            }
            else
            {
                float glance = 5f * Mathf.Sin(clock * 0.23f) * (1f - gait);
                want = Quaternion.Euler(-2f * Mathf.Sin(clock * 0.31f) * (1f - gait), -(pelvisYaw + spineYaw + chestYaw) + glance, 0f);
            }
            head = Quaternion.Slerp(head, want, 1f - Mathf.Exp(-7f * dt));
            loco[(int)Bone.Head] = head;
        }

        // ------------------------------------------------------------------ layer 4: foot IK

        void FootIK(float dt, float w)
        {
            Vector3 up = Vector3.up;
            float rootY = transform.position.y;
            float drop = 0f;
            for (int leg = 0; leg < 2; leg++)
            {
                Transform foot = rig[leg == 0 ? Bone.FootL : Bone.FootR];
                Vector3 ankle = foot.position;
                fkHeights[leg] = ankle.y - rootY;
                float ground = rootY;
                normals[leg] = up;
                if (w > 0.01f && Ground(new Vector3(ankle.x, rootY + 0.55f, ankle.z), out RaycastHit hit))
                {
                    ground = hit.point.y;
                    normals[leg] = hit.normal;
                }
                ankles[leg] = new Vector3(ankle.x, ground + fkHeights[leg], ankle.z);
                drop = Mathf.Min(drop, ground - rootY);
            }
            // Drop the pelvis so the lower foot can reach; never lift it (the agent already stands on the higher ground).
            ikPelvis = Mathf.Lerp(ikPelvis, Mathf.Clamp(drop, -0.3f, 0f) * w, 1f - Mathf.Exp(-10f * dt));
            if (w <= 0.01f && Mathf.Abs(ikPelvis) < 0.001f) return;
            rig[Bone.Pelvis].position += up * ikPelvis;
            if (w <= 0.01f) return;

            for (int leg = 0; leg < 2; leg++)
            {
                Transform ul = rig[leg == 0 ? Bone.UpperLegL : Bone.UpperLegR];
                Transform ll = rig[leg == 0 ? Bone.LowerLegL : Bone.LowerLegR];
                Transform foot = rig[leg == 0 ? Bone.FootL : Bone.FootR];
                Quaternion fkFoot = foot.rotation;
                TwoBone(ul, ll, foot, Vector3.Lerp(foot.position, ankles[leg], w), transform.forward);
                // Planted soles tilt to the ground; lifted ones keep their swing pose.
                float plant = 1f - Mathf.Clamp01((fkHeights[leg] - footRestHeight) / 0.06f);
                Quaternion aligned = Quaternion.FromToRotation(up, Vector3.Slerp(up, normals[leg], 0.8f)) * fkFoot;
                foot.rotation = Quaternion.Slerp(fkFoot, aligned, plant * w);
            }
        }

        /// <summary>Analytic two-bone IK: places the knee by the law of cosines in the current bend plane.</summary>
        static void TwoBone(Transform upper, Transform lower, Transform end, Vector3 goal, Vector3 pole)
        {
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float la = (b - a).magnitude, lb = (c - b).magnitude;
            Vector3 at = goal - a;
            float d = Mathf.Clamp(at.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
            Vector3 dir = at.normalized;
            Vector3 ac = (c - a).sqrMagnitude > 1e-8f ? (c - a).normalized : dir;
            Vector3 bend = (b - a) - Vector3.Project(b - a, ac) + pole * 0.02f;
            bend = Vector3.ProjectOnPlane(bend, dir).normalized;
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f);
            Vector3 knee = a + dir * (la * cosA) + bend * (la * Mathf.Sqrt(1f - cosA * cosA));
            upper.rotation = Quaternion.FromToRotation(b - a, knee - a) * upper.rotation;
            Vector3 c2 = end.position;
            lower.rotation = Quaternion.FromToRotation(c2 - knee, a + dir * d - knee) * lower.rotation;
        }

        /// <summary>Nearest static ground below a point, ignoring this body, other characters and loose props.</summary>
        bool Ground(Vector3 from, out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            float bestD = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform) || col is CharacterController) continue;
                if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) continue;
                if (col.TryGetComponent<Hitbox>(out _)) continue;
                if (hits[i].distance < bestD) { bestD = hits[i].distance; best = hits[i]; found = true; }
            }
            return found;
        }
    }
}
