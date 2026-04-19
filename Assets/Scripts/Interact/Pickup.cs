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
        [SerializeField] private string itemId;
        [SerializeField] private string clueTextKey;
        [SerializeField] private string promptKey = "ui.pickup.prompt";
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private bool oneShotClue = true;

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

            if (!string.IsNullOrEmpty(clueTextKey) && CluePopup.Instance != null)
            {
                CluePopup.Instance.Show(clueTextKey);
            }

            if (oneShotClue || tookSomething)
            {
                _consumed = true;
                if (destroyOnPickup) Destroy(gameObject);
            }
        }
    }
}
