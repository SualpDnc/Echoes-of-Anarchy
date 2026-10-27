using System;
using System.Collections;
using UnityEngine;
using Echoes.Audio;
using Echoes.Core;
using Echoes.Rendering;

namespace Echoes.Narrative
{
    /// <summary>
    /// The 19:00 beat, driven by GameClock: the Emergency Broadcast tone and sirens, then the grid drops and the
    /// street lights die in a wave rolling out from the player. Some lamps come back on emergency power (dim and
    /// failing), so the streets stay readable. The distant sounds of the Purge start once the sirens have risen.
    /// Before 19:00 the city is quiet.
    /// </summary>
    public class PurgeClock : MonoBehaviour
    {
        public static event Action PurgeStarted;
        /// <summary>The moment the grid drops and the blackout wave starts (blackoutDelay after 19:00).</summary>
        public static event Action GridDown;

        public int hour = 19, minute = 0;

        [Header("Blackout")]
        [Tooltip("Seconds after 19:00 that the grid drops (the EBS tone plays first).")]
        public float blackoutDelay = 9.5f;
        [Tooltip("Speed of the blackout wave in m/s.")]
        public float waveSpeed = 45f;
        [Range(0f, 1f)] public float emergencyFraction = 0.55f;
        [Range(0f, 1f)] public float emergencyLevel = 0.6f;

        public bool Started { get; private set; }
        public static PurgeClock Instance { get; private set; }

        /// <summary>Real seconds from now until the grid drops (scripted scenes time themselves to it).</summary>
        public float SecondsToGridDown
        {
            get
            {
                var clock = GameClock.Instance;
                if (clock == null) return blackoutDelay;
                float toStart = (float)(start - clock.Now).TotalSeconds / Mathf.Max(0.01f, clock.timeScale);
                return Mathf.Max(0f, toStart) + blackoutDelay;
            }
        }

        DistantEvents distant;
        DateTime start;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            // Static events outlive the scene; drop listeners from the last play session.
            PurgeStarted = null;
            GridDown = null;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            distant = FindAnyObjectByType<DistantEvents>();
            var clock = GameClock.Instance;
            if (clock == null) { enabled = false; return; }
            start = new DateTime(clock.Now.Year, clock.Now.Month, clock.Now.Day, hour, minute, 0);
            if (clock.Now < start && distant != null) distant.purgeActive = false;
        }

        void Update()
        {
            if (Started || GameClock.Instance == null || GameClock.Instance.Now < start) return;
            Started = true;
            StartCoroutine(Begin());
        }

        IEnumerator Begin()
        {
            PurgeStarted?.Invoke();
            if (PurgeSiren.Instance != null) PurgeSiren.Instance.Sound();
            yield return new WaitForSeconds(blackoutDelay);

            GridDown?.Invoke();
            Vector3 origin = Camera.main != null ? Camera.main.transform.position : transform.position;
            float last = 0f;
            foreach (var lamp in StreetLight.All.ToArray())
            {
                float delay = Vector3.Distance(origin, lamp.transform.position) / waveSpeed + UnityEngine.Random.Range(0f, 0.5f);
                last = Mathf.Max(last, delay);
                lamp.PowerCut(delay);
                if (UnityEngine.Random.value < emergencyFraction)
                    lamp.EmergencyPower(delay + UnityEngine.Random.Range(4f, 9f), emergencyLevel * UnityEngine.Random.Range(0.8f, 1.1f));
            }

            yield return new WaitForSeconds(Mathf.Max(6f, last));
            if (distant != null) distant.purgeActive = true;
        }
    }
}
