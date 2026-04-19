using System;
using System.Collections.Generic;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// 4-slot inventory. Items are identified by string id (e.g. "item.glasses").
    /// UI binds to OnChanged and reads Slots directly.
    ///
    /// v1.2: every Add/Remove also writes <c>inventory.has.{baseId}</c> to
    /// <see cref="GameFlags"/> so OnInventoryHas triggers can react and
    /// the F10 debug overlay shows pickup state. <c>baseId</c> is the item id
    /// stripped of the leading "item." prefix.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        // Logical capacity of the bag. Not to be confused with the *visible* slot
        // count on the HUD, which is a separate page-size decided by InventoryHUD.
        public const int MaxSlots = 16;

        public static Inventory Instance { get; private set; }

        [SerializeField] private string[] startingItems;

        public readonly List<string> Slots = new List<string>(MaxSlots);

        public event Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (startingItems != null)
            {
                for (int i = 0; i < startingItems.Length && Slots.Count < MaxSlots; i++)
                {
                    if (!string.IsNullOrEmpty(startingItems[i])) Slots.Add(startingItems[i]);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Has(string itemId) => Slots.Contains(itemId);

        public bool TryAdd(string itemId)
        {
            if (Slots.Count >= MaxSlots) return false;
            if (string.IsNullOrEmpty(itemId)) return false;
            Slots.Add(itemId);
            WriteFlag(itemId, true);
            OnChanged?.Invoke();
            return true;
        }

        public bool Remove(string itemId)
        {
            bool removed = Slots.Remove(itemId);
            if (removed)
            {
                WriteFlag(itemId, false);
                OnChanged?.Invoke();
            }
            return removed;
        }

        // Strip "item." prefix so flag matches FlagKeys.Inventory.Has_X (which the
        // FlagKeysGenerator builds from baseId only). Items not following the
        // "item.X" convention are written as-is so legacy callers still flag.
        private static void WriteFlag(string itemId, bool value)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            string baseId = itemId.StartsWith("item.") ? itemId.Substring("item.".Length) : itemId;
            GameFlags.Set("inventory.has." + baseId, value);
        }
    }
}
