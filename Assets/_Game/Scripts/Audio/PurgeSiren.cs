using System.Collections;
using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>
    /// The start-of-Purge sequence: Emergency Broadcast tone on nearby devices, then air-raid sirens rising
    /// across the whole city. Several sources at different distances make the wash huge; arrival is staggered
    /// naturally by the speed of sound.
    /// </summary>
    public class PurgeSiren : MonoBehaviour
    {
        public static PurgeSiren Instance { get; private set; }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Sound(bool withBroadcastTone = true) => StartCoroutine(Sequence(withBroadcastTone));

        IEnumerator Sequence(bool tone)
        {
            var am = AudioManager.Instance;
            if (am == null) yield break;
            if (tone)
            {
                am.Play2D(SoundId.EBSTone, 0.35f);
                yield return new WaitForSeconds(8.5f);
            }
            Vector3 c = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            float[] distances = { 160f, 420f, 700f, 1100f, 1600f };
            for (int i = 0; i < distances.Length; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                Vector3 p = c + new Vector3(dir.x, 0f, dir.y) * distances[i] + Vector3.up * 25f;
                float pitch = Random.Range(0.96f, 1.04f);
                am.PlayAt(SoundId.PurgeSiren, p, i == 0 ? 0.9f : 0.75f, pitch, 300f, 3000f);
                yield return new WaitForSeconds(Random.Range(0.2f, 1.2f));
            }
        }
    }
}
