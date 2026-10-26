using UnityEngine;
using UnityEngine.AI;

namespace Echoes.AI
{
    /// <summary>
    /// Procedural locomotion and poses for HumanoidRig. Bones rest at identity with limbs along −Y, so
    /// +X rotation swings a limb backward and −X forward. Everything blends with exponential smoothing.
    /// </summary>
    [RequireComponent(typeof(HumanoidRig))]
    public class ProcAnimator : MonoBehaviour
    {
        public enum Pose { Normal, Aim, HandsUp, Kneel, Cower, Melee }

        public Pose pose = Pose.Normal;
        public Vector3? lookTarget;
        public Vector3? aimTarget;
        [Range(0f, 1f)] public float meleeProgress;

        HumanoidRig rig;
        NavMeshAgent agent;
        readonly Quaternion[] current = new Quaternion[16];
        Vector3 pelvisRest;
        float phase, speed, crouch;
        Vector3 flinch, flinchVel;

        void Awake()
        {
            rig = GetComponent<HumanoidRig>();
            TryGetComponent(out agent);
            for (int i = 0; i < 16; i++) current[i] = Quaternion.identity;
            pelvisRest = rig[Bone.Pelvis].localPosition;
            phase = Random.value * 10f;
        }

        public void Flinch(Vector3 worldDir)
        {
            Vector3 local = transform.InverseTransformDirection(worldDir);
            flinchVel += new Vector3(local.z * 220f, 0f, -local.x * 160f);
        }

        void LateUpdate()
        {
            if (rig.IsRagdoll) { enabled = false; return; }
            float dt = Time.deltaTime;
            float v = agent != null && agent.enabled ? new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude : 0f;
            speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-8f * dt));
            float run = Mathf.Clamp01((speed - 2f) / 2.2f);
            float moving = Mathf.Clamp01(speed / 1.2f);
            float stride = Mathf.Lerp(1.35f, 2.3f, run);
            phase += speed / stride * Mathf.PI * 2f * dt;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);

            float swing = s * Mathf.Lerp(24f, 42f, run) * moving;
            float kneeL = Mathf.Lerp(6f, 70f, run) * moving * Mathf.Clamp01(-c) + 4f;
            float kneeR = Mathf.Lerp(6f, 70f, run) * moving * Mathf.Clamp01(c) + 4f;

            var t = new Quaternion[16];
            for (int i = 0; i < 16; i++) t[i] = Quaternion.identity;
            t[(int)Bone.UpperLegL] = Quaternion.Euler(-swing, 0f, 0f);
            t[(int)Bone.UpperLegR] = Quaternion.Euler(swing, 0f, 0f);
            t[(int)Bone.LowerLegL] = Quaternion.Euler(kneeL, 0f, 0f);
            t[(int)Bone.LowerLegR] = Quaternion.Euler(kneeR, 0f, 0f);
            t[(int)Bone.FootL] = Quaternion.Euler(-kneeL * 0.3f, 0f, 0f);
            t[(int)Bone.FootR] = Quaternion.Euler(-kneeR * 0.3f, 0f, 0f);
            float armSwing = swing * 0.75f;
            t[(int)Bone.UpperArmL] = Quaternion.Euler(armSwing, 0f, -6f);
            t[(int)Bone.UpperArmR] = Quaternion.Euler(-armSwing, 0f, 6f);
            t[(int)Bone.LowerArmL] = Quaternion.Euler(-12f - 50f * run, 0f, 0f);
            t[(int)Bone.LowerArmR] = Quaternion.Euler(-12f - 50f * run, 0f, 0f);
            t[(int)Bone.Spine] = Quaternion.Euler(3f + 12f * run, s * 4f * moving, 0f);
            t[(int)Bone.Pelvis] = Quaternion.Euler(0f, -s * 6f * moving, 0f);
            float targetCrouch = 0f;

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
                    t[(int)Bone.UpperArmR] = Quaternion.Euler(-88f - pitch, yaw - 8f, 0f);
                    t[(int)Bone.LowerArmR] = Quaternion.Euler(-4f, 0f, 0f);
                    t[(int)Bone.UpperArmL] = Quaternion.Euler(-78f - pitch, yaw + 32f, 0f);
                    t[(int)Bone.LowerArmL] = Quaternion.Euler(-38f, 0f, 0f);
                    t[(int)Bone.Spine] = Quaternion.Euler(6f, yaw * 0.4f, 0f);
                    break;
                }
                case Pose.HandsUp:
                    t[(int)Bone.UpperArmL] = Quaternion.Euler(-160f, 0f, -22f);
                    t[(int)Bone.UpperArmR] = Quaternion.Euler(-160f, 0f, 22f);
                    t[(int)Bone.LowerArmL] = Quaternion.Euler(-35f, 0f, 0f);
                    t[(int)Bone.LowerArmR] = Quaternion.Euler(-35f, 0f, 0f);
                    break;
                case Pose.Kneel:
                    t[(int)Bone.UpperLegL] = Quaternion.Euler(-80f, 0f, -6f);
                    t[(int)Bone.UpperLegR] = Quaternion.Euler(10f, 0f, 6f);
                    t[(int)Bone.LowerLegL] = Quaternion.Euler(85f, 0f, 0f);
                    t[(int)Bone.LowerLegR] = Quaternion.Euler(95f, 0f, 0f);
                    t[(int)Bone.FootR] = Quaternion.Euler(-50f, 0f, 0f);
                    t[(int)Bone.UpperArmL] = Quaternion.Euler(-165f, 0f, -18f);
                    t[(int)Bone.UpperArmR] = Quaternion.Euler(-165f, 0f, 18f);
                    t[(int)Bone.LowerArmL] = Quaternion.Euler(-70f, 0f, 0f);
                    t[(int)Bone.LowerArmR] = Quaternion.Euler(-70f, 0f, 0f);
                    t[(int)Bone.Spine] = Quaternion.Euler(-4f, 0f, 0f);
                    targetCrouch = 0.42f;
                    break;
                case Pose.Cower:
                    t[(int)Bone.UpperLegL] = Quaternion.Euler(-75f, 0f, -10f);
                    t[(int)Bone.UpperLegR] = Quaternion.Euler(-75f, 0f, 10f);
                    t[(int)Bone.LowerLegL] = Quaternion.Euler(120f, 0f, 0f);
                    t[(int)Bone.LowerLegR] = Quaternion.Euler(120f, 0f, 0f);
                    t[(int)Bone.FootL] = Quaternion.Euler(-40f, 0f, 0f);
                    t[(int)Bone.FootR] = Quaternion.Euler(-40f, 0f, 0f);
                    t[(int)Bone.Spine] = Quaternion.Euler(40f, 0f, 0f);
                    t[(int)Bone.UpperArmL] = Quaternion.Euler(-150f, 0f, -45f);
                    t[(int)Bone.UpperArmR] = Quaternion.Euler(-150f, 0f, 45f);
                    t[(int)Bone.LowerArmL] = Quaternion.Euler(-110f, 0f, 0f);
                    t[(int)Bone.LowerArmR] = Quaternion.Euler(-110f, 0f, 0f);
                    targetCrouch = 0.5f;
                    break;
                case Pose.Melee:
                {
                    // Wind up over the shoulder, then a fast diagonal strike.
                    float p = meleeProgress;
                    float wind = p < 0.45f ? p / 0.45f : 1f - Mathf.Clamp01((p - 0.45f) / 0.25f);
                    float strike = Mathf.Clamp01((p - 0.45f) / 0.25f);
                    t[(int)Bone.UpperArmR] = Quaternion.Euler(Mathf.Lerp(-20f, -170f, wind) + strike * 60f, 0f, Mathf.Lerp(10f, 40f, wind) - strike * 30f);
                    t[(int)Bone.LowerArmR] = Quaternion.Euler(Mathf.Lerp(-30f, -90f, wind), 0f, 0f);
                    t[(int)Bone.UpperArmL] = Quaternion.Euler(-40f * wind, 0f, -10f);
                    t[(int)Bone.Spine] = Quaternion.Euler(10f * strike, Mathf.Lerp(0f, 25f, wind) - strike * 45f, 0f);
                    break;
                }
            }

            // Hit flinch spring on the spine.
            flinchVel += (-flinch * 200f - flinchVel * 16f) * dt;
            flinch += flinchVel * dt;
            t[(int)Bone.Spine] *= Quaternion.Euler(flinch);

            // Head tracks a look target within neck limits.
            if (lookTarget.HasValue)
            {
                Transform chest = rig[Bone.Chest];
                Vector3 local = chest.InverseTransformPoint(lookTarget.Value) - rig[Bone.Head].localPosition;
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -35f, 40f);
                t[(int)Bone.Head] = Quaternion.Euler(pitch, yaw, 0f);
            }

            float k = 1f - Mathf.Exp(-(pose == Pose.Melee ? 30f : 11f) * dt);
            for (int i = 0; i < 16; i++)
            {
                current[i] = Quaternion.Slerp(current[i], t[i], k);
                if (rig.bones[i] != null) rig.bones[i].localRotation = current[i];
            }
            crouch = Mathf.Lerp(crouch, targetCrouch, 1f - Mathf.Exp(-6f * dt));
            float bob = Mathf.Abs(c) * 0.035f * moving * (0.5f + run);
            rig[Bone.Pelvis].localPosition = pelvisRest + Vector3.down * (crouch + bob * 0.6f);
        }
    }
}
