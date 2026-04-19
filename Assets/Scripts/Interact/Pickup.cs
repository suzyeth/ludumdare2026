using UnityEngine;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Floor item. On E:
    ///  - Adds itemId to Inventory (if set)
    ///  - Shows CluePopup with clueTextKey (if set)
    ///  - Destroys itself if destroyOnPickup
    ///
    /// Items with only clueTextKey and no itemId are "inspect-only" clues — think
    /// a graffiti wall or a scrap of paper the player reads but doesn't take.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour, IInteractable
    {
        [Header("Item / Legacy Clue")]
        [SerializeField] private string itemId;
        [SerializeField] private string clueTextKey;
        [SerializeField] private string promptKey = "ui.pickup.prompt";
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private bool oneShotClue = true;

        [Header("v1.2 AVG Dispatch (optional)")]
        [Tooltip("If true, fire a DialogueManager popup on pickup instead of (or in addition to) CluePopup.")]
        [SerializeField] private bool useDialogueManager = false;
        [SerializeField] private DialogueType dialogueType = DialogueType.NAR;
        [Tooltip("i18n keys passed to DialogueManager.ShowKeys (multi-page READ supported).")]
        [SerializeField] private string[] dialogueKeys;
        [SerializeField] private string nodeTag;
        [Tooltip("READ only — i18n key for popup title bar.")]
        [SerializeField] private string dialogueTitleKey;
        [Tooltip("READ only — header sprite (128×128).")]
        [SerializeField] private Sprite dialogueHeaderSprite;

        private bool _consumed;

        public string PromptKey => promptKey;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public bool CanInteract(GameObject who) => !_consumed;

        public void Interact(GameObject who)
        {
            if (_consumed) return;

            bool tookSomething = false;
            if (!string.IsNullOrEmpty(itemId) && Inventory.Instance != null)
            {
                tookSomething = Inventory.Instance.TryAdd(itemId);
                if (!tookSomething) return; // inventory full — bail without consuming
            }

            // v1.2 AVG dispatch path takes precedence over the legacy CluePopup when wired.
            bool firedDialogue = false;
            if (useDialogueManager && DialogueManager.Instance != null && dialogueKeys != null && dialogueKeys.Length > 0)
            {
                var type = dialogueType == DialogueType.TIP ? DialogueType.NAR : dialogueType;
                Vector3? pos = type == DialogueType.ENV ? (Vector3?)transform.position : null;
                DialogueManager.Instance.ShowKeys(type, dialogueKeys, null, pos, nodeTag, dialogueTitleKey, dialogueHeaderSprite);
                firedDialogue = true;
            }
            else if (!string.IsNullOrEmpty(clueTextKey) && CluePopup.Instance != null)
            {
                CluePopup.Instance.Show(clueTextKey);
            }

            // Consume only when:
            //   - we actually put an item into Inventory, OR
            //   - the designer marked the clue as one-shot (read once only).
            // Otherwise (inspect-only reusable), keep interactable for re-read.
            bool shouldConsume = tookSomething
                || (oneShotClue && !string.IsNullOrEmpty(clueTextKey))
                || (oneShotClue && firedDialogue);
            if (shouldConsume)
            {
                _consumed = true;
                if (destroyOnPickup) Destroy(gameObject);
            }
        }
    }
}
