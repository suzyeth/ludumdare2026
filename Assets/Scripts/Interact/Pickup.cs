using UnityEngine;
using PrismZone.Core;
using PrismZone.Player;
using PrismZone.UI;

namespace PrismZone.Interact
{
    /// <summary>
    /// Floor item / world interactable. On E:
    ///  - Adds <see cref="itemId"/> to Inventory (if set)
    ///  - Fires <see cref="dialogueNodeId"/> via DialogueManager (if set) — type / pages /
    ///    SFX / follow-up / filter-conditional text all come from <c>text_table.tsv</c>
    ///  - Otherwise falls back to legacy <see cref="clueTextKey"/> → CluePopup
    ///  - Destroys itself if <see cref="destroyOnPickup"/>
    ///
    /// Read-only interactables (wall notes, graffiti) leave itemId empty and set
    /// <see cref="oneShotClue"/> = false so the node stays re-readable.
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

        [Header("v1.2 AVG Dispatch")]
        [Tooltip("TSV node id (e.g. T-02). When set, DialogueManager.ShowById is called — everything else (type, pages, sfx, follow-up) comes from text_table.tsv.")]
        [NodeIdDropdown]
        [SerializeField] private string dialogueNodeId;

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
                PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.Pickup);
            }

            bool firedDialogue = false;
            if (!string.IsNullOrEmpty(dialogueNodeId) && DialogueManager.Instance != null)
            {
                // Source the READ popup's left-slot sprite from ItemData.BigIcon when
                // we picked up an item — keeps the "diary page / letter / note" art
                // wired without re-introducing a per-prefab Sprite field.
                Sprite header = null;
                if (!string.IsNullOrEmpty(itemId))
                {
                    var data = ItemDatabase.Get(itemId);
                    if (data != null) header = data.BigIcon;
                }
                DialogueManager.Instance.ShowById(dialogueNodeId, null, null, null, header);
                firedDialogue = true;
            }
            else if (!string.IsNullOrEmpty(clueTextKey) && CluePopup.Instance != null)
            {
                CluePopup.Instance.Show(clueTextKey);
            }

            // Consume only when we added an item, or the designer marked the clue/dialogue
            // as one-shot. Read-only re-readable nodes (notes, graffiti) stay interactable.
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
