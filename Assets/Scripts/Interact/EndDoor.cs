using System.Collections;
using UnityEngine;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Final exit door. Two states:
    ///
    ///   - Locked (default): sprite = <see cref="lockedSprite"/>. E shows the
    ///     "门暂时被锁住了" prompt via CluePopup — no dialogue fires, door
    ///     stays shut.
    ///   - Unlocked: auto-switches when <see cref="unlockNodeId"/> (default T-20)
    ///     finishes. Sprite swaps to <see cref="unlockedSprite"/>. E now fires
    ///     <see cref="interactNodeId"/> (default T-21), which drives the
    ///     final escape flow (VictoryController reacts to T-21 finishing).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EndDoor : MonoBehaviour, IInteractable
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        [Header("Dialogue Triggers")]
        [Tooltip("When this dialogue id finishes, the door unlocks. Default T-20.")]
        [SerializeField] private string unlockNodeId = "T-20";
        [Tooltip("Dialogue to fire when player interacts with the unlocked door. Default T-21.")]
        [SerializeField] private string interactNodeId = "T-21";

        [Header("Prompts")]
        [Tooltip("Shown above the door while still locked.")]
        [SerializeField] private string promptLockedKey = "ui.door.end_locked";
        [Tooltip("Shown above the door once unlocked.")]
        [SerializeField] private string promptOpenKey = "ui.door.end_prompt";

        private bool _unlocked;
        private bool _interacted;
        private bool _subscribed;

        public string PromptKey => _unlocked ? promptOpenKey : promptLockedKey;

        private void Awake()
        {
            if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
            ApplySprite();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private IEnumerator Start()
        {
            while (DialogueManager.Instance == null) yield return null;
            DialogueManager.Instance.OnDialogueFinished += HandleDialogueFinished;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueFinished -= HandleDialogueFinished;
        }

        private void HandleDialogueFinished(DialogueType type, string tag)
        {
            if (_unlocked) return;
            if (string.IsNullOrEmpty(unlockNodeId) || tag != unlockNodeId) return;
            Unlock();
        }

        public void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            ApplySprite();
        }

        public bool CanInteract(GameObject who) => true;

        public void Interact(GameObject who)
        {
            if (!_unlocked)
            {
                // Still locked — surface the "door is sealed" hint via the
                // lightweight CluePopup so we don't queue a full AVG beat.
                if (CluePopup.Instance != null) CluePopup.Instance.Show(promptLockedKey);
                return;
            }
            if (_interacted) return; // one-shot — prevents re-trigger while T-21 is showing
            _interacted = true;
            if (!string.IsNullOrEmpty(interactNodeId) && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowById(interactNodeId);
            }
        }

        private void ApplySprite()
        {
            if (doorRenderer == null) return;
            var s = _unlocked ? unlockedSprite : lockedSprite;
            if (s != null) doorRenderer.sprite = s;
        }
    }
}
