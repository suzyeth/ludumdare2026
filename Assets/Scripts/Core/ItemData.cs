using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Designer-authored item definition. The Inventory stores a string id; use
    /// <see cref="ItemDatabase.Get"/> to resolve id → full ItemData for display.
    ///
    /// Detail pages are i18n keys — put the long-form text in zh/en.json and
    /// reference e.g. `item.diary.page.1`, `item.diary.page.2`.
    /// </summary>
    [CreateAssetMenu(menuName = "PrismZone/Item Data", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string id = "item.example";
        [SerializeField] private string nameKey = "item.example";
        [SerializeField] private Sprite worldIcon;       // 24×24 drop sprite
        [SerializeField] private Sprite bigIcon;         // 96-128 px detail view
        [SerializeField] private string[] pageKeys;      // i18n keys for detail pages
        [SerializeField] private bool hasDetailPopup = true;
        [Tooltip("Keys / notes want to be tracked by Inventory (flags, gates) but stay out of the 4-slot HUD. Uncheck to hide from the right-bottom bar.")]
        [SerializeField] private bool showInInventory = true;

        public string Id => id;
        public string NameKey => nameKey;
        public Sprite WorldIcon => worldIcon;
        public Sprite BigIcon => bigIcon;
        public string[] PageKeys => pageKeys;
        public bool HasDetailPopup => hasDetailPopup;
        public bool ShowInInventory => showInInventory;
        public int PageCount => pageKeys?.Length ?? 0;
    }
}
