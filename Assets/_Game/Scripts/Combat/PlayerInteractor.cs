using UnityEngine;
using Echoes.Player;
using Echoes.UI;

namespace Echoes.Combat
{
    /// <summary>Looks for an Interactable under the camera centre, shows "[F] …" and uses it on the interact key.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        CameraRig rig;
        EchoesInput input;
        Interactable current;
        PlayerMelee melee;

        void Awake()
        {
            rig = GetComponent<CameraRig>();
            input = GetComponent<EchoesInput>();
            TryGetComponent(out melee);
        }

        void Update()
        {
            current = Find();
            // With nothing to use, F becomes a melee strike on whoever is in reach.
            bool canStrike = current == null && melee != null && melee.FindTarget() != null;
            var hud = BodycamHUD.Instance;
            if (hud != null) hud.SetPrompt(current != null ? "[F]  " + current.Prompt : canStrike ? "[F]  Strike" : "");
            if (!input.InteractPressed || input.InputLocked) return;
            if (current != null) current.Interact(gameObject);
            else if (melee != null) melee.Strike();
        }

        Interactable Find()
        {
            Transform cam = rig.cam.transform;
            var hits = Physics.SphereCastAll(cam.position, 0.25f, cam.forward, 3f, ~0, QueryTriggerInteraction.Collide);
            Interactable best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                var it = h.collider.GetComponentInParent<Interactable>();
                if (it == null || !it.CanInteract || h.distance > it.MaxDistance) continue;
                if (h.distance < bestDist) { bestDist = h.distance; best = it; }
            }
            return best;
        }
    }
}
