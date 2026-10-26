using UnityEngine;
using Echoes.Combat;

namespace Echoes.AI
{
    public enum Bone
    {
        Pelvis, Spine, Chest, Head,
        UpperArmL, LowerArmL, HandL, UpperArmR, LowerArmR, HandR,
        UpperLegL, LowerLegL, FootL, UpperLegR, LowerLegR, FootR
    }

    /// <summary>Bone references for a procedural humanoid, plus conversion to a jointed ragdoll on death.</summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform[] bones = new Transform[16];
        public Transform weaponSocket;
        public Transform eyes;
        public bool IsRagdoll { get; private set; }

        public Transform this[Bone b] => bones[(int)b];

        static readonly float[] Mass = { 12f, 8f, 12f, 5f, 2.5f, 1.5f, 0.5f, 2.5f, 1.5f, 0.5f, 7f, 4f, 1f, 7f, 4f, 1f };
        static readonly int[] Parent = { -1, 0, 1, 2, 2, 4, 5, 2, 7, 8, 0, 10, 11, 0, 13, 14 };

        public void EnableRagdoll(Vector3 inheritedVelocity, DamageInfo killingBlow)
        {
            if (IsRagdoll) return;
            IsRagdoll = true;
            var rbs = new Rigidbody[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null || bones[i].GetComponent<Collider>() == null) continue;
                var rb = bones[i].gameObject.AddComponent<Rigidbody>();
                rb.mass = Mass[i];
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.6f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = i == 0 || i == 3 ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Discrete;
                rb.linearVelocity = inheritedVelocity;
                rbs[i] = rb;
            }
            // Overlapping limb colliders of the same body must not push each other apart (ragdoll "explosion").
            var cols = GetComponentsInChildren<Collider>();
            for (int a = 0; a < cols.Length; a++)
                for (int b = a + 1; b < cols.Length; b++)
                    Physics.IgnoreCollision(cols[a], cols[b], true);
            foreach (var c in cols) PlayerCollision.Ignore(c); // walking over a body must not jitter the camera

            for (int i = 1; i < bones.Length; i++)
            {
                int p = Parent[i];
                while (p >= 0 && rbs[p] == null) p = Parent[p];
                if (rbs[i] == null || p < 0) continue;
                var j = bones[i].gameObject.AddComponent<CharacterJoint>();
                j.connectedBody = rbs[p];
                j.enableProjection = true;
                var b = (Bone)i;
                bool hinge = b == Bone.LowerArmL || b == Bone.LowerArmR || b == Bone.LowerLegL || b == Bone.LowerLegR;
                j.axis = Vector3.right;
                j.swingAxis = Vector3.forward;
                // Knees bend back (+X), elbows bend forward (−X).
                bool knee = b == Bone.LowerLegL || b == Bone.LowerLegR;
                j.lowTwistLimit = new SoftJointLimit { limit = hinge ? (knee ? -5f : -120f) : -25f };
                j.highTwistLimit = new SoftJointLimit { limit = hinge ? (knee ? 120f : 5f) : 25f };
                j.swing1Limit = new SoftJointLimit { limit = hinge ? 5f : b == Bone.Head ? 35f : 45f };
                j.swing2Limit = new SoftJointLimit { limit = hinge ? 5f : 30f };
            }
            // Hitbox colliders had no rigidbody while alive, so find the bone nearest to where the round landed.
            var hitRb = killingBlow.hitBody;
            if (hitRb == null)
            {
                float best = float.MaxValue;
                foreach (var rb in rbs)
                {
                    if (rb == null) continue;
                    float d = (rb.GetComponent<Collider>().ClosestPoint(killingBlow.point) - killingBlow.point).sqrMagnitude;
                    if (d < best) { best = d; hitRb = rb; }
                }
            }
            if (hitRb != null && killingBlow.direction.sqrMagnitude > 0.01f)
                hitRb.AddForceAtPosition(killingBlow.direction.normalized * Mathf.Max(60f, killingBlow.force * 0.4f), killingBlow.point, ForceMode.Impulse);
        }
    }
}
