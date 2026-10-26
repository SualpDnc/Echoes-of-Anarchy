using UnityEngine;
using Echoes.Player;

namespace Echoes.Audio
{
    /// <summary>Daniel's foley: surface-aware footsteps, landings, exhausted breathing, flashlight clicks.</summary>
    [RequireComponent(typeof(FPSController))]
    public class PlayerAudio : MonoBehaviour
    {
        [Range(0f, 1f)] public float footstepVolume = 0.55f;
        [Range(0f, 1f)] public float breathVolume = 0.5f;

        FPSController body;
        EchoesInput input;
        AudioSource breath;
        int lastVariation = -1;

        void Awake()
        {
            body = GetComponent<FPSController>();
            input = GetComponent<EchoesInput>();
            body.Footstep += OnFootstep;
            body.Landed += OnLanded;
            body.Jumped += OnJumped;
        }

        void OnDestroy()
        {
            body.Footstep -= OnFootstep;
            body.Landed -= OnLanded;
            body.Jumped -= OnJumped;
        }

        void Start()
        {
            if (AudioManager.Instance == null) return;
            breath = gameObject.AddComponent<AudioSource>();
            breath.clip = AudioManager.Instance.Clip(SoundId.Breath);
            breath.loop = true;
            breath.spatialBlend = 0f;
            breath.volume = 0f;
            breath.Play();
        }

        void Update()
        {
            if (breath != null)
            {
                // Breathing becomes audible as stamina drains and fades out as he recovers.
                float fatigue = 1f - body.Stamina01;
                float target = Mathf.Clamp01((fatigue - 0.25f) / 0.75f) * breathVolume;
                breath.volume = Mathf.Lerp(breath.volume, target, 1f - Mathf.Exp(-2.5f * Time.deltaTime));
                breath.pitch = Mathf.Lerp(0.95f, 1.3f, fatigue);
            }
            if (input.FlashlightPressed && AudioManager.Instance != null)
                AudioManager.Instance.Play2D(SoundId.FlashlightClick, 0.35f, Random.Range(0.95f, 1.05f));
        }

        void OnFootstep(float intensity)
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            var id = DetectSurface();
            // Never repeat the same variation twice in a row — the machine-gun effect kills realism.
            int n = am.Variations(id);
            int v = Random.Range(0, n);
            if (n > 1 && v == lastVariation) v = (v + 1) % n;
            lastVariation = v;
            float vol = footstepVolume * Mathf.Lerp(0.25f, 1f, intensity);
            am.Play2D(id, vol, Random.Range(0.92f, 1.08f), v);
            // Sprinting carries ~14 m, walking ~6 m, crouching barely 2 m.
            Echoes.Combat.NoiseSystem.Emit(transform.position, Mathf.Lerp(1.5f, 14f, intensity * intensity), Echoes.Combat.NoiseKind.Footstep, gameObject, true);
        }

        void OnLanded(float impact)
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            am.Play2D(SoundId.Land, Mathf.Clamp01(impact / 9f) * 0.9f, Random.Range(0.9f, 1.05f));
            am.Play2D(DetectSurface(), footstepVolume, 0.9f);
        }

        void OnJumped()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.Jump, 0.3f, Random.Range(0.95f, 1.1f));
        }

        SoundId DetectSurface()
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 1f, ~0, QueryTriggerInteraction.Ignore))
                return SoundId.StepConcrete;
            string matName = MaterialAt(hit);
            if (matName == null) return SoundId.StepConcrete;
            if (matName.Contains("Asphalt") || matName.Contains("Roof") || matName.Contains("Dirt")) return SoundId.StepAsphalt;
            if (matName.Contains("Metal") || matName.Contains("Galvanized") || matName.Contains("Rust") || matName.Contains("Burnt")
                || matName.Contains("Car") || matName.Contains("Dumpster") || matName.Contains("Shutter")) return SoundId.StepMetal;
            if (matName.Contains("Wood") || matName.Contains("Plywood")) return SoundId.StepWood;
            return SoundId.StepConcrete;
        }

        /// <summary>Resolves the material under a hit, including the right submesh for mesh colliders.</summary>
        static string MaterialAt(RaycastHit hit)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) return null;
            var mats = rend.sharedMaterials;
            if (mats.Length == 1 || !(hit.collider is MeshCollider mc) || mc.sharedMesh == null || hit.triangleIndex < 0)
                return mats.Length > 0 && mats[0] != null ? mats[0].name : null;
            var mesh = mc.sharedMesh;
            int idx = hit.triangleIndex * 3;
            for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
            {
                var sm = mesh.GetSubMesh(s);
                if (idx >= sm.indexStart && idx < sm.indexStart + sm.indexCount)
                    return mats[s] != null ? mats[s].name : null;
            }
            return mats[0] != null ? mats[0].name : null;
        }
    }
}
