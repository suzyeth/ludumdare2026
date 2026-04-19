using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.Core;
using PrismZone.Enemy;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// 4F 402 lectern recorder. Drives the v1.2 recorder thread (T-18 ~ T-21):
    ///   1. Player approaches and E-interacts → T-18 NAR ("red light, source of broadcast").
    ///   2. Player presses F → recorder stops → T-19 NAR + every enemy goes to Stopped
    ///      (per spec §4.3 "录音笔关闭后永久退出追击") + BroadcastController is told
    ///      to never broadcast again.
    ///   3. After stop, a switch to green lens enables a second F press →
    ///      T-21 READ ("the second segment of the recording").
    ///
    /// AVG dispatch goes through DialogueManager so all the queueing / freeze /
    /// chase-cancel rules are inherited.
    /// </summary>
    [AddComponentMenu("Prism Zone/Recorder")]
    [RequireComponent(typeof(Collider2D))]
    public class Recorder : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Node IDs (TSV)")]
        [SerializeField] private string nodeInspect = "T-18";
        [SerializeField] private string nodeStop = "T-19";
        [SerializeField] private string nodeSecondTip = "T-20";
        [SerializeField] private string nodeSecondPlay = "T-21";

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource broadcastLoop;
        [SerializeField] private SoundId clickSfx = SoundId.UIClick;

        [Header("Visual (optional — red blinking light)")]
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private Color onColor = new(0.95f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color offColor = new(0.2f, 0.2f, 0.2f, 1f);

        public string PromptKey => _stopped ? "ui.recorder.play" : "ui.interact.prompt";

        private bool _inspected;
        private bool _stopped;
        private bool _secondTipShown;
        private bool _playerInRange;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            ApplyIndicator();
            if (broadcastLoop != null)
            {
                broadcastLoop.loop = true;
                if (!broadcastLoop.isPlaying) broadcastLoop.Play();
            }
        }

        private void OnEnable()
        {
            FilterManager.OnFilterChanged += HandleFilterChanged;
        }

        private void OnDisable()
        {
            FilterManager.OnFilterChanged -= HandleFilterChanged;
        }

        private void Update()
        {
            if (!_playerInRange) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            // F is the action verb on the recorder per spec ("按 F 停止" / "按 F 播放").
            // We only react to F here; E (inspect) is routed via IInteractable below.
            if (!kb.fKey.wasPressedThisFrame) return;

            if (!_stopped)
            {
                StopRecorder();
            }
            else if (FilterManager.Instance != null && FilterManager.Instance.Current == FilterColor.Green)
            {
                PlaySecondSegment();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player")) _playerInRange = false;
        }

        // --- IInteractable (E to inspect — T-18 first time) -------------------

        public bool CanInteract(GameObject who) => !_inspected;

        public void Interact(GameObject who)
        {
            if (_inspected || DialogueManager.Instance == null) return;
            _inspected = true;
            DialogueManager.Instance.ShowById(nodeInspect);
        }

        // --- F: stop recorder -------------------------------------------------

        private void StopRecorder()
        {
            if (_stopped) return;
            _stopped = true;

            if (broadcastLoop != null && broadcastLoop.isPlaying) broadcastLoop.Stop();
            AudioManager.Instance?.Play(clickSfx);
            ApplyIndicator();

            // Spec §4.3: Stopped state is the absorbing terminal — no enemy can ever
            // re-enter Chase. Push every active enemy into it.
            foreach (var enemy in EnemyBase.All)
            {
                if (enemy != null) enemy.RequestState(EnemyBase.State.Stopped);
            }

            // Tell BroadcastController (if present) to disarm permanently.
            if (BroadcastController.Instance != null) BroadcastController.Instance.DisarmPermanent();

            DialogueManager.Instance?.ShowById(nodeStop);
        }

        // --- F: replay second segment (green lens only) -----------------------

        private void PlaySecondSegment()
        {
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.ShowById(nodeSecondPlay);
        }

        // --- Filter changes: surface T-20 once when green is donned post-stop -

        private void HandleFilterChanged(FilterColor prev, FilterColor next)
        {
            if (!_stopped || _secondTipShown) return;
            if (next != FilterColor.Green) return;
            _secondTipShown = true;
            DialogueManager.Instance?.ShowById(nodeSecondTip);
        }

        private void ApplyIndicator()
        {
            if (indicator == null) return;
            indicator.color = _stopped ? offColor : onColor;
        }
    }
}
