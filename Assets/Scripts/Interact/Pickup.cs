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

            // Cross-scene persistence guard: if this pickup's dialogue already fired
            // in a previous visit, or the item it grants is already in Inventory,
            // the scene prefab instance must NOT re-offer it. Instance fields like
            // `_consumed` reset on reload — only GameFlags / Inventory survive.
            // Without this, re-entering Scn_3Floor respawns Pickup_Diary and lets
            // T-03 → T-04 replay every time.
            if (destroyOnPickup)
            {
                bool dialogueAlready = !string.IsNullOrEmpty(dialogueNodeId)
                    && GameFlags.Get($"dialogue.{dialogueNodeId}.triggered");
                bool itemAlready = !string.IsNullOrEmpty(itemId)
                    && Inventory.Instance != null && Inventory.Instance.Has(itemId);
                if (dialogueAlready || itemAlready)
                {
                    _consumed = true;
                    // Disable the trigger collider before Destroy so any same-frame
                    // OnTrigger callbacks (player already overlapping at spawn) can't
                    // fire against the dying pickup. Destroy is end-of-frame; the
                    // component disable is immediate.
                    col.enabled = false;
                    Destroy(gameObject);
                }
            }
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
                ItemData data = !string.IsNullOrEmpty(itemId) ? ItemDatabase.Get(itemId) : null;
                Sprite header = data != null ? data.BigIcon : null;
                // titleKey = itemId so the NAR→READ chain's READ popup shows the
                // item's display name (e.g. "item.glasses" → "颜色矫正眼镜") in the
                // title bar. DialogueManager propagates titleKey through follow-ups.
                string title = !string.IsNullOrEmpty(itemId) ? itemId : null;

                // Multi-page items (currently just the diary) replace the per-page
                // TSV chain with one unified READ popup so prev/next buttons can
                // flip across all sub-pages — same UX as clicking the inventory
                // slot. Single-page items keep the existing chain behavior.
                System.Action onNarFinished = BuildUnifiedItemPopupCallback(data);
                DialogueManager.Instance.ShowById(dialogueNodeId, onNarFinished, null, title, header);
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

        /// <summary>
        /// For multi-page items, returns a callback that — after the NAR pickup
        /// beat closes — fires one unified READ popup containing every PageKey,
        /// matching the inventory-slot click UX. Returns null for single-page
        /// items so they keep the existing TSV auto-chain.
        /// </summary>
        private static System.Action BuildUnifiedItemPopupCallback(ItemData data)
        {
            if (data == null || !data.HasDetailPopup) return null;
            var keys = data.PageKeys;
            if (keys == null || keys.Length <= 1) return null;
            var nameKey = data.NameKey;
            var bigIcon = data.BigIcon;
            var itemTag = data.Id;
            return () =>
            {
                // Enqueue the unified popup BEFORE flipping the page.1 flag —
                // any OnState trigger watching for "diary read" (T-04) gets
                // queued AFTER this popup, so it surfaces in narrative order.
                DialogueManager.Instance?.ShowKeys(DialogueType.READ, keys, () =>
                {
                    // Mark every page triggered after the unified popup closes
                    // so future OnState gates see the same flag set the chain
                    // would have produced. page.1 was already set below to
                    // suppress the auto-chain; setting again is a no-op.
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(keys[i]))
                            GameFlags.Set($"dialogue.{keys[i]}.triggered", true);
                    }
                }, null, itemTag, nameKey, bigIcon);

                // Pre-flag page.1 so the NAR's auto-chain check (uses
                // !GameFlags.Get(followUp)) skips firing the per-page chain.
                // Same flag also fires T-04's OnState trigger — which queues
                // after the unified popup we just enqueued above.
                if (!string.IsNullOrEmpty(keys[0]))
                    GameFlags.Set($"dialogue.{keys[0]}.triggered", true);
            };
        }
    }
}
