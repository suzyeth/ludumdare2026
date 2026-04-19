using UnityEngine;
using UnityEngine.InputSystem;

namespace PrismZone.Player
{
    /// <summary>
    /// Picks the closest IInteractable inside interactRadius and invokes it on E.
    /// Interactables set their own gizmo prompt via UI; this only wires the input.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 0.75f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly Collider2D[] _buffer = new Collider2D[8];
        private ContactFilter2D _filter;
        private IInteractable _current;

        public IInteractable CurrentTarget => _current;

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true,
                layerMask = interactableMask
            };
        }

        private void Update()
        {
            ScanNearest();
            var kb = Keyboard.current;
            if (kb == null) return;

            // E = interact. Suppressed when any modal is open — those modals consume E
            // themselves (CluePopup dismiss, ItemDetailPanel close, PasscodePanel input).
            // Without this guard, the same E press both dismisses the clue AND retriggers
            // the interactable next to the player in the same frame.
            if (kb.eKey.wasPressedThisFrame && _current != null && !IsAnyModalOpen())
            {
                _current.Interact(gameObject);
            }
            // Item detail is now opened by clicking a HUD slot (InventoryHUD.OnSlotClicked).
            // F was removed because it was ambiguous with multiple detail items in bag.
        }

        private static bool IsAnyModalOpen()
        {
            if (PrismZone.UI.CluePopup.Instance != null && PrismZone.UI.CluePopup.Instance.IsOpen) return true;
            if (PrismZone.UI.ItemDetailPanel.Instance != null && PrismZone.UI.ItemDetailPanel.Instance.IsOpen) return true;
            if (PrismZone.UI.PauseMenu.Instance != null && PrismZone.UI.PauseMenu.Instance.IsOpen) return true;
            if (PrismZone.UI.SettingsPanel.Instance != null && PrismZone.UI.SettingsPanel.Instance.IsOpen) return true;
            // v1.2: AVG popups consume E / Space themselves; without this guard the same
            // E press would dismiss the popup AND retrigger the interactable behind it.
            if (PrismZone.UI.DialogueManager.Instance != null && PrismZone.UI.DialogueManager.Instance.IsShowing) return true;
            if (PrismZone.Core.GameOverController.IsGameOver) return true;
            if (PrismZone.Core.VictoryController.IsVictory) return true;
            return false;
        }

        private void ScanNearest()
        {
            _filter.layerMask = interactableMask;
            int count = Physics2D.OverlapCircle(transform.position, interactRadius, _filter, _buffer);
            IInteractable best = null;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var c = _buffer[i];
                if (c == null) continue;
                var it = c.GetComponentInParent<IInteractable>();
                if (it == null || !it.CanInteract(gameObject)) continue;
                float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = it; }
            }
            _current = best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }

    public interface IInteractable
    {
        bool CanInteract(GameObject who);
        void Interact(GameObject who);
        string PromptKey { get; } // i18n key, e.g. "ui.cabinet.prompt"
    }
}
