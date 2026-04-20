using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Bottom-right 4-slot HUD. Slots are pre-placed in the prefab; this component
    /// just syncs their labels/icons to Inventory.Instance.Slots.
    /// Items with <c>ItemData.ShowInInventory == false</c> (keys, notes) are tracked
    /// by Inventory for gates/flags but filtered out of the HUD view.
    /// </summary>
    public class InventoryHUD : MonoBehaviour
    {
        [System.Serializable]
        public class SlotView
        {
            public Image icon;
            public TMP_Text label;
        }

        [SerializeField] private SlotView[] slots = new SlotView[4];
        [SerializeField] private Sprite emptyIcon;
        [Header("Pagination (optional)")]
        [Tooltip("Only visible when Inventory has more items than one page can hold.")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [Tooltip("Shows e.g. '1/2'. Leave null to hide.")]
        [SerializeField] private TMP_Text pageIndicator;
        [Tooltip("Compact overflow badge, e.g. '+3' — shown when the bag has more items than fit in one page and pagination buttons aren't present.")]
        [SerializeField] private TMP_Text overflowBadge;

        private readonly List<string> _visible = new List<string>(Inventory.MaxSlots);
        private int _currentPage;

        private void OnEnable()
        {
            TrySubscribe();
            WireSlotButtons();
            WirePageButtons();
            Refresh();
        }

        // Start runs after every Awake, so if Inventory.Instance wasn't ready at
        // OnEnable (undefined Awake order across nested prefabs), retry here.
        private void Start()
        {
            TrySubscribe();
            Refresh();
        }

        private void TrySubscribe()
        {
            if (Inventory.Instance == null) return;
            Inventory.Instance.OnChanged -= Refresh; // idempotent
            Inventory.Instance.OnChanged += Refresh;
        }

        private void WirePageButtons()
        {
            if (prevPageButton != null)
            {
                prevPageButton.onClick.RemoveAllListeners();
                prevPageButton.onClick.AddListener(() => { _currentPage--; Refresh(); });
            }
            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveAllListeners();
                nextPageButton.onClick.AddListener(() => { _currentPage++; Refresh(); });
            }
        }

        private void OnDisable()
        {
            if (Inventory.Instance != null) Inventory.Instance.OnChanged -= Refresh;
        }

        // Ensure every slot icon has a Button that routes clicks to this HUD.
        // Adds the component at runtime so designers don't have to wire it by hand,
        // and captures the slot index in a local so the closure doesn't drift.
        private void WireSlotButtons()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                var v = slots[i];
                if (v == null || v.icon == null) continue;
                var btn = v.icon.GetComponent<Button>();
                if (btn == null) btn = v.icon.gameObject.AddComponent<Button>();
                btn.targetGraphic = v.icon; // enables hover/press tint visuals
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            int global = _currentPage * Mathf.Max(1, slots.Length) + slotIndex;
            if (global < 0 || global >= _visible.Count) return;
            var data = ItemDatabase.Get(_visible[global]);
            if (data == null || !data.HasDetailPopup) return; // keys / no-detail items: no-op
            var panel = ItemDetailPanel.Instance;
            if (panel == null) return;
            panel.Show(data); // Show() replaces current item if already open
        }

        private void Refresh()
        {
            var inv = Inventory.Instance;
            _visible.Clear();
            if (inv != null)
            {
                foreach (var id in inv.Slots)
                {
                    var data = ItemDatabase.Get(id);
                    // Missing ItemData → still show (legacy / placeholder behavior).
                    if (data == null || data.ShowInInventory) _visible.Add(id);
                }
            }

            int pageSize = Mathf.Max(1, slots.Length);
            int maxPage = Mathf.Max(0, (_visible.Count - 1) / pageSize);
            _currentPage = Mathf.Clamp(_currentPage, 0, maxPage);
            int offset = _currentPage * pageSize;

            for (int i = 0; i < slots.Length; i++)
            {
                int global = offset + i;
                bool filled = global < _visible.Count;
                string id = filled ? _visible[global] : null;
                var v = slots[i];
                if (v == null) continue;

                if (v.icon != null)
                {
                    // Resolve the per-item world icon from ItemDatabase; fall back to emptyIcon
                    // for empty slots. Image is always enabled — otherwise a filled slot with
                    // no icon sprite but no empty sprite would disappear visually.
                    Sprite target = null;
                    if (filled)
                    {
                        var data = ItemDatabase.Get(id);
                        target = data != null ? data.WorldIcon : null;
                    }
                    if (target == null) target = emptyIcon;
                    v.icon.sprite = target;
                    v.icon.enabled = true;
                    v.icon.preserveAspect = true; // keep icon proportions when slot isn't square
                    // Force full white on filled slots — the prefab shipped the icon Image
                    // at a dark grey tint (0.15,0.15,0.15,0.7) to read as "empty", which
                    // would multiply over real sprites and make them look black.
                    // Empty slots keep the dim tint so the placeholder still reads as empty.
                    v.icon.color = filled
                        ? Color.white
                        : new Color(0.15f, 0.15f, 0.15f, emptyIcon != null ? 0.5f : 0f);
                }
                // Designers don't want the slot name label; keep it empty.
                // (Prefab can also just delete the TMP child — this is a belt-and-braces guard.)
                if (v.label != null) v.label.text = string.Empty;
            }

            // Page chrome — hidden when the bag fits in one page, and each arrow is
            // hidden (not just disabled) when there's nothing in that direction.
            bool multiPage = _visible.Count > pageSize;
            if (prevPageButton != null)
                prevPageButton.gameObject.SetActive(multiPage && _currentPage > 0);
            if (nextPageButton != null)
                nextPageButton.gameObject.SetActive(multiPage && _currentPage < maxPage);
            if (pageIndicator != null)
            {
                pageIndicator.gameObject.SetActive(multiPage);
                pageIndicator.text = multiPage ? $"{_currentPage + 1}/{maxPage + 1}" : string.Empty;
            }

            // Overflow badge: small "+N" counter for items hidden beyond the current page.
            // Hidden only when nothing overflows *past* the current page, so the badge
            // disappears on the last page instead of rendering an empty "+0".
            if (overflowBadge != null)
            {
                int hidden = Mathf.Max(0, _visible.Count - (_currentPage + 1) * pageSize);
                bool show = multiPage && hidden > 0;
                overflowBadge.gameObject.SetActive(show);
                if (show) overflowBadge.text = $"+{hidden}";
            }
        }
    }
}
