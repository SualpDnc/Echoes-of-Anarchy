using System;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>In-world time of Purge night. Drives the bodycam timestamp and story beats.</summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        public int startHour = 18;
        public int startMinute = 52;
        [Tooltip("In-game seconds per real second. Cutscenes can jump time with JumpTo.")]
        public float timeScale = 1f;
        public bool running = true;

        static readonly DateTime NightDate = new DateTime(2034, 3, 21);
        double seconds;

        public DateTime Now => NightDate.AddHours(startHour).AddMinutes(startMinute).AddSeconds(seconds);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (running) seconds += Time.deltaTime * timeScale;
        }

        public void JumpTo(int hour, int minute, int second = 0)
        {
            DateTime start = NightDate.AddHours(startHour).AddMinutes(startMinute);
            DateTime target = NightDate.AddHours(hour).AddMinutes(minute).AddSeconds(second);
            if (target < start) target = target.AddDays(1); // past midnight
            seconds = (target - start).TotalSeconds;
        }
    }
}
