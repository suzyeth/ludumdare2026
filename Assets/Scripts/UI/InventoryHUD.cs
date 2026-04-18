using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Bottom-right 4-slot HUD. Slots are pre-placed in the prefab; this component
    /// just syncs their labels/icons to Inventory.Instance.Slots.
    /// </summary>
    public class InventoryHUD : MonoBehaviour
    {
        [System.Serializable]
        public class SlotView
        {
            public Image icon;
            public TMP_Text label;
        }

        [SerializeField] private SlotView[] slots = new SlotView[Inventory.MaxSlots];
        [SerializeField] private Sprite emptyIcon;

        private void OnEnable()
        {
            if (Inventory.Instance != null) Inventory.Instance.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (Inventory.Instance != null) Inventory.Instance.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            var inv = Inventory.Instance;
            for (int i = 0; i < slots.Length; i++)
            {
                bool filled = inv != null && i < inv.Slots.Count;
                string id = filled ? inv.Slots[i] : null;
                var v = slots[i];
                if (v == null) continue;
                if (v.icon != null) v.icon.enabled = !filled || emptyIcon != null;
                if (v.label != null) v.label.text = filled ? I18nManager.Get(id) : string.Empty;
            }
        }
    }
}
