using System.Collections.Generic;
using UnityEngine;
using Echoes.UI;

namespace Echoes.AI
{
    /// <summary>
    /// Development tool for checking the characters: F8 lines up one of every look a few metres in front of the
    /// player (and removes it again), F7 cycles what they do. They can be shot, which ragdolls them.
    /// Stripped from release builds.
    /// </summary>
    public class CharacterLineup : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly string[] Modes = { "IDLE", "WALK", "RUN", "AIM", "HANDS UP", "KNEEL", "COWER", "MELEE" };

        Transform root, player;
        int mode;
        readonly List<ProcAnimator> anims = new List<ProcAnimator>();

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.f8Key.wasPressedThisFrame)
            {
                if (root != null) Clear();
                else Spawn();
            }
            if (root == null) return;
            if (kb.f7Key.wasPressedThisFrame)
            {
                mode = (mode + 1) % Modes.Length;
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowCenter("LINEUP: " + Modes[mode], 1.5f);
            }
            Animate();
        }

        void Spawn()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            player = p.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            root = new GameObject("CharacterLineup").transform;

            var looks = Looks();
            for (int i = 0; i < looks.Count; i++)
            {
                var rng = new System.Random(500 + i);
                looks[i].Dress(rng);
                var rig = HumanoidBuilder.Build(root, looks[i], rng, out var health);
                Vector3 pos = player.position + fwd * 5f + right * (i - (looks.Count - 1) * 0.5f) * 0.95f;
                if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore)) pos.y = hit.point.y;
                rig.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-fwd));
                var anim = rig.gameObject.AddComponent<ProcAnimator>();
                anims.Add(anim);
                health.Died += info =>
                {
                    anim.enabled = false;
                    anims.Remove(anim);
                    rig.EnableRagdoll(Vector3.zero, info);
                };
            }
            mode = 0;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowCenter("LINEUP: F7 CYCLES", 2f);
        }

        void Clear()
        {
            Destroy(root.gameObject);
            root = null;
            anims.Clear();
        }

        void Animate()
        {
            Vector3 eye = player.position + Vector3.up * 1.5f;
            foreach (var a in anims)
            {
                a.speedOverride = mode == 1 ? 1.3f : mode == 2 ? 4.2f : 0f;
                a.pose = mode switch
                {
                    3 => ProcAnimator.Pose.Aim,
                    4 => ProcAnimator.Pose.HandsUp,
                    5 => ProcAnimator.Pose.Kneel,
                    6 => ProcAnimator.Pose.Cower,
                    7 => ProcAnimator.Pose.Melee,
                    _ => ProcAnimator.Pose.Normal
                };
                a.aimTarget = eye;
                a.lookTarget = mode == 0 || mode >= 3 ? eye : (Vector3?)null;
                a.meleeProgress = Mathf.Repeat(Time.time * 0.8f, 1f);
            }
        }

        /// <summary>One of every faction look, mask and garment.</summary>
        static List<CharacterLook> Looks()
        {
            CharacterLook L(Faction f, HeadKind head, TopKind top, PantsKind pants, ShoeKind shoes, HairKind hair, bool fem = false, float bulk = 1f, float belly = 0f, bool gloves = false)
            {
                bool shortSleeves = top == TopKind.TShirt;
                return new CharacterLook
                {
                    faction = f, head = head, top = top, pants = pants, shoes = shoes, hair = hair, feminine = fem,
                    height = fem ? 0.95f : 1f, bulk = bulk, belly = belly, gloves = gloves || f == Faction.Smiler, shortSleeves = shortSleeves
                };
            }
            return new List<CharacterLook>
            {
                L(Faction.Smiler, HeadKind.LedSmile, TopKind.Suit, PantsKind.Suit, ShoeKind.Dress, HairKind.None),
                L(Faction.Smiler, HeadKind.LedSmile, TopKind.Suit, PantsKind.Suit, ShoeKind.Dress, HairKind.None, true),
                L(Faction.Looter, HeadKind.Hood, TopKind.Hoodie, PantsKind.Jeans, ShoeKind.Sneaker, HairKind.None),
                L(Faction.Looter, HeadKind.Pig, TopKind.WorkJacket, PantsKind.Cargo, ShoeKind.Boot, HairKind.None, false, 1.12f, 0.8f),
                L(Faction.Looter, HeadKind.Angel, TopKind.Hoodie, PantsKind.Sweatpants, ShoeKind.Sneaker, HairKind.None, true),
                L(Faction.Looter, HeadKind.Face, TopKind.WorkJacket, PantsKind.Jeans, ShoeKind.Boot, HairKind.Beanie, false, 1.05f, 0f, true),
                L(Faction.Civilian, HeadKind.Face, TopKind.TShirt, PantsKind.Jeans, ShoeKind.Sneaker, HairKind.Short),
                L(Faction.Civilian, HeadKind.Face, TopKind.Sweater, PantsKind.Chinos, ShoeKind.Dress, HairKind.Long, true),
                L(Faction.Civilian, HeadKind.Face, TopKind.OpenJacket, PantsKind.Jeans, ShoeKind.Sneaker, HairKind.Cap, false, 1.16f, 1f),
            };
        }
#endif
    }
}
