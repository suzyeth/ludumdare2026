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

            // E = interact with nearest
            if (kb.eKey.wasPressedThisFrame && _current != null)
            {
                _current.Interact(gameObject);
            }

            // F = open detail popup on first inventory item that has one
            if (kb.fKey.wasPressedThisFrame) TryOpenFirstDetail();
        }

        private void TryOpenFirstDetail()
        {
            var panel = PrismZone.UI.ItemDetailPanel.Instance;
            if (panel == null) return;
            if (panel.IsOpen) { panel.Close(); return; }
            var inv = PrismZone.UI.Inventory.Instance;
            if (inv == null) return;
            foreach (var id in inv.Slots)
            {
                var data = PrismZone.Core.ItemDatabase.Get(id);
                if (data != null && data.HasDetailPopup) { panel.Show(data); break; }
            }
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
