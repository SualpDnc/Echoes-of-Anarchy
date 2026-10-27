using System.Collections;
using UnityEngine;
using Echoes.Rendering;
using Echoes.UI;

namespace Echoes.Narrative
{
    /// <summary>
    /// Runs the demo (Act 1) in order: sequence 1 (cold open in the ambulance) then sequence 2 (the crash site).
    /// Later steps (6c–6e) chain on after these. `startAt` can begin at the crash site, or skip the story entirely
    /// for sandbox testing; F4 skips the ride while it plays.
    /// </summary>
    [RequireComponent(typeof(ColdOpenSequence), typeof(CrashSiteSequence))]
    public class DemoDirector : MonoBehaviour
    {
        public enum StartAt { ColdOpen, CrashSite, Sandbox }

        public static DemoDirector Instance { get; private set; }

        public StartAt startAt = StartAt.ColdOpen;
        [Tooltip("Smoke and steam materials from the scene builder (the truck's steaming radiator).")]
        public FireFX.Materials fx;

        ColdOpenSequence coldOpen;
        CrashSiteSequence crashSite;
        Coroutine running;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            coldOpen = GetComponent<ColdOpenSequence>();
            crashSite = GetComponent<CrashSiteSequence>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            if (startAt == StartAt.Sandbox) yield break;
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.SetFade(1f); // nothing of the spawn point shows
            yield return null; // the NavMesh bakes in Awake; characters are placed on it from here on
            coldOpen.Build(fx);
            if (startAt == StartAt.CrashSite) coldOpen.Skip();
            running = StartCoroutine(Flow(startAt == StartAt.ColdOpen));
        }

        IEnumerator Flow(bool ride)
        {
            if (ride) yield return coldOpen.Play();
            yield return crashSite.Play();
            running = null;
        }

        /// <summary>Dev shortcut: cut the ambulance ride and go straight to the crash site.</summary>
        public void SkipToCrash()
        {
            if (startAt == StartAt.Sandbox || coldOpen.Crashed || !coldOpen.Playing) return;
            if (running != null) StopCoroutine(running);
            coldOpen.Skip();
            running = StartCoroutine(Flow(false));
        }
    }
}
