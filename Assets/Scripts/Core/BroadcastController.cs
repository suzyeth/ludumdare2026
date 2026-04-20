using System.Collections;
using UnityEngine;
using PrismZone.Enemy;
using PrismZone.UI;

namespace PrismZone.Core
{
    /// <summary>
    /// Drives the school broadcast cycle (v1.2 spec §4.1, §4.3).
    ///
    /// Cycle:  idle (interval) → prelude (SFX) → broadcast (loop SFX + AVG freeze
    /// + every enemy in <see cref="EnemyBase.State.Locating"/>) → idle.
    ///
    /// Special hooks:
    ///   - First prelude also fires T-03 ("listen for this — find a place to hide").
    ///   - <see cref="DisarmPermanent"/> shuts the cycle off forever (called by Recorder
    ///     after T-19 "stops the broadcast").
    ///   - <see cref="IsBroadcasting"/> is a static flag the player controller can read
    ///     to freeze movement during a broadcast (player stunned per spec).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BroadcastController : MonoBehaviour
    {
        public static BroadcastController Instance { get; private set; }
        public static bool IsBroadcasting { get; private set; }

        [Header("Timing (seconds)")]
        [Tooltip("Time before the first broadcast after scene start.")]
        [SerializeField] private float firstDelay = 25f;
        [Tooltip("Idle gap between broadcasts.")]
        [SerializeField] private float interval = 60f;
        [SerializeField] private float preludeDuration = 3f;
        [SerializeField] private float broadcastDuration = 10f;

        [Header("Tutorial Beat")]
        [Tooltip("Optional TSV node to fire on the first broadcast prelude. Leave empty if no tutorial beat is wanted — picking a wrong node (e.g. T-03 which is now the diary pickup) makes the broadcast replay an unrelated story beat.")]
        [SerializeField] private string firstPreludeNodeId = "";

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource broadcastSource;
        [SerializeField] private SoundId preludeSfx = SoundId.BroadcastPrelude;
        [SerializeField] private SoundId loopSfx = SoundId.BroadcastLoop;

        private bool _firstPreludePlayed;
        private bool _disarmed;
        private Coroutine _cycle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsBroadcasting = false;
        }

        private void OnEnable()
        {
            if (_cycle == null) _cycle = StartCoroutine(CycleLoop());
        }

        private void OnDisable()
        {
            if (_cycle != null) { StopCoroutine(_cycle); _cycle = null; }
            IsBroadcasting = false;
        }

        /// <summary>Recorder calls this on T-19 stop. Once disarmed: never broadcasts again.</summary>
        public void DisarmPermanent()
        {
            _disarmed = true;
            if (_cycle != null) { StopCoroutine(_cycle); _cycle = null; }
            EndBroadcastImmediate();
        }

        /// <summary>Designer can wire a button or scripted event to fire one immediately.</summary>
        public void TriggerNow()
        {
            if (_disarmed || IsBroadcasting) return;
            StartCoroutine(RunOne());
        }

        private IEnumerator CycleLoop()
        {
            yield return WaitPausedByAvg(firstDelay);
            while (!_disarmed)
            {
                yield return RunOne();
                if (_disarmed) yield break;
                yield return WaitPausedByAvg(interval);
            }
        }

        private IEnumerator RunOne()
        {
            // --- Prelude ---
            AudioManager.Instance?.Play(preludeSfx);
            if (!_firstPreludePlayed)
            {
                _firstPreludePlayed = true;
                if (!string.IsNullOrEmpty(firstPreludeNodeId))
                    DialogueManager.Instance?.ShowById(firstPreludeNodeId);
            }
            yield return WaitPausedByAvg(preludeDuration);

            // --- Broadcast ---
            IsBroadcasting = true;
            DialogueManager.Instance?.SetFrozen(true);

            // Prefer the dedicated AudioSource so the broadcast loop survives
            // SFX pool rotation. Fall through to AudioManager if the source
            // isn't usable (null, or no clip wired — which plays silence).
            if (broadcastSource != null && broadcastSource.clip != null && !broadcastSource.isPlaying)
                broadcastSource.Play();
            else
                AudioManager.Instance?.Play(loopSfx);

            // Push every active enemy into Locating. Skip ones already Stopped.
            foreach (var e in EnemyBase.All)
            {
                if (e == null || e.Current == EnemyBase.State.Stopped) continue;
                e.RequestState(EnemyBase.State.Locating);
            }

            yield return WaitPausedByAvg(broadcastDuration);

            EndBroadcastImmediate();
        }

        /// <summary>
        /// Real-time wait that freezes while an AVG popup is on screen. Prevents
        /// the broadcast cycle from running down (or firing a new broadcast)
        /// while the player is reading a clue / note. Time.timeScale-agnostic:
        /// uses unscaledDeltaTime so pausing the game also pauses the timer.
        /// </summary>
        private IEnumerator WaitPausedByAvg(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                var dm = DialogueManager.Instance;
                bool avgOpen = dm != null && dm.IsShowing;
                if (!avgOpen) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void EndBroadcastImmediate()
        {
            if (broadcastSource != null && broadcastSource.isPlaying) broadcastSource.Stop();
            DialogueManager.Instance?.SetFrozen(false);
            IsBroadcasting = false;

            foreach (var e in EnemyBase.All)
            {
                if (e == null || e.Current == EnemyBase.State.Stopped) continue;
                if (e.Current == EnemyBase.State.Locating) e.RequestState(EnemyBase.State.Patrol);
            }
        }
    }
}
