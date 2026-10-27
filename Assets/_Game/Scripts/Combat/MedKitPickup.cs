using UnityEngine;
using Echoes.Audio;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Combat
{
    /// <summary>A trauma kit (red pouch, white cross) to pick up: one more med kit for PlayerHealth. Glows faintly.</summary>
    public class MedKitPickup : Interactable
    {
        public override string Prompt => "Take med kit";

        public event System.Action Taken;

        Light glow;

        public static MedKitPickup Create(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("MedKit");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var red = RuntimeMaterials.Fabric(new Color(0.45f, 0.04f, 0.03f), 0.25f, Fabric.Cotton);
            var white = RuntimeMaterials.Lit(new Color(0.85f, 0.85f, 0.85f), 0.3f, 0f, new Color(1f, 1f, 1f) * 0.25f);
            PrimitiveMeshes.Part(go.transform, "Pouch", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.32f, 0.16f, 0.22f), red);
            PrimitiveMeshes.Part(go.transform, "CrossA", PrimitiveType.Cube, new Vector3(0f, 0.161f, 0f), new Vector3(0.12f, 0.004f, 0.035f), white, null, false);
            PrimitiveMeshes.Part(go.transform, "CrossB", PrimitiveType.Cube, new Vector3(0f, 0.161f, 0f), new Vector3(0.035f, 0.004f, 0.12f), white, null, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.12f, 0f);
            col.size = new Vector3(0.5f, 0.3f, 0.4f); // a little generous so it is easy to aim at on the ground
            col.isTrigger = true;
            return go.AddComponent<MedKitPickup>();
        }

        void Start()
        {
            var go = new GameObject("PickupGlow", typeof(Light));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.35f;
            glow = go.GetComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.75f, 0.7f);
            glow.range = 1.6f;
            glow.shadows = LightShadows.None;
        }

        void Update()
        {
            if (glow != null) glow.intensity = 0.8f + 0.4f * Mathf.Sin(Time.time * 3f);
        }

        public override void Interact(GameObject player)
        {
            if (!player.TryGetComponent(out PlayerHealth ph)) return;
            ph.medKits++;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Bandage, 0.35f, 1.4f);
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo($"MED KITS {ph.medKits}", 1.5f);
            Taken?.Invoke();
            Destroy(gameObject);
        }
    }
}
