using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrismZone.UI
{
    /// <summary>
    /// 4-slot inventory. Items are identified by string id (e.g. "item.glasses").
    /// UI binds to OnChanged and reads Slots directly.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public const int MaxSlots = 4;

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
            OnChanged?.Invoke();
            return true;
        }

        public bool Remove(string itemId)
        {
            bool removed = Slots.Remove(itemId);
            if (removed) OnChanged?.Invoke();
            return removed;
        }
    }
}
