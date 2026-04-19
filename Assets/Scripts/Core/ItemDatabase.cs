using System.Collections.Generic;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Resolves inventory string ids to <see cref="ItemData"/> assets. All ItemData
    /// placed under Assets/Resources/Items/ are auto-loaded the first time a lookup
    /// is performed.
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> _cache;

        public static ItemData Get(string id)
        {
            if (_cache == null) Load();
            if (string.IsNullOrEmpty(id)) return null;
            _cache.TryGetValue(id, out var d);
            return d;
        }

        public static void Clear() { _cache = null; }

        private static void Load()
        {
            _cache = new Dictionary<string, ItemData>();
            var all = Resources.LoadAll<ItemData>("Items");
            foreach (var d in all)
            {
                if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                if (_cache.ContainsKey(d.Id))
                    Debug.LogWarning($"[ItemDB] Duplicate id '{d.Id}' in {d.name}");
                else
                    _cache[d.Id] = d;
            }
            Debug.Log($"[ItemDB] Loaded {_cache.Count} items");
        }
    }
}
