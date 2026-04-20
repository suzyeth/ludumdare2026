using System;
using System.Collections.Generic;

namespace PrismZone.Core
{
    /// <summary>
    /// Central key/value state bus (FRAMEWORK.md §3.1). All mutable game state that
    /// matters for narrative gating — dialogue triggered, items in inventory, current
    /// filter, broadcast/recorder/scene state — is written here. Triggers and other
    /// reactive systems subscribe to <see cref="OnChanged"/> instead of polling.
    ///
    /// Design constraints:
    ///   - Static so any caller can reach it without holding a singleton reference.
    ///   - bool keyspace and int keyspace are separate dictionaries (so a key reused
    ///     accidentally as both never silently coerces).
    ///   - <see cref="OnChanged"/> fires AFTER the write, with the key only — caller
    ///     re-reads if it needs the new value (avoids stale-snapshot bugs in chains).
    ///
    /// Use <see cref="FlagKeys"/> constants instead of raw strings whenever possible:
    /// the TSV importer auto-generates entries for every dialogue node and item.
    /// </summary>
    public static class GameFlags
    {
        private static readonly Dictionary<string, bool> _bools = new();
        private static readonly Dictionary<string, int>  _ints  = new();

        /// <summary>Fires every time a value is set, with the key that changed.</summary>
        public static event Action<string> OnChanged;

        // --- Bool API ---------------------------------------------------------

        public static bool Get(string key)
            => !string.IsNullOrEmpty(key) && _bools.TryGetValue(key, out var v) && v;

        public static void Set(string key, bool val)
        {
            if (string.IsNullOrEmpty(key)) return;
            // Skip notification when value is unchanged — prevents recursive trigger
            // storms from systems that re-write the same flag every frame.
            if (_bools.TryGetValue(key, out var prev) && prev == val) return;
            _bools[key] = val;
            OnChanged?.Invoke(key);
        }

        // --- Int API ----------------------------------------------------------

        public static int GetInt(string key)
            => !string.IsNullOrEmpty(key) && _ints.TryGetValue(key, out var v) ? v : 0;

        public static void SetInt(string key, int val)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_ints.TryGetValue(key, out var prev) && prev == val) return;
            _ints[key] = val;
            OnChanged?.Invoke(key);
        }

        public static void Increment(string key, int delta = 1) => SetInt(key, GetInt(key) + delta);

        // --- Bulk helpers (used by Condition.Evaluate) ------------------------

        public static bool AllSet(IEnumerable<string> keys)
        {
            if (keys == null) return true;
            foreach (var k in keys) if (!Get(k)) return false;
            return true;
        }

        public static bool NoneSet(IEnumerable<string> keys)
        {
            if (keys == null) return true;
            foreach (var k in keys) if (Get(k)) return false;
            return true;
        }

        public static bool AnySet(IEnumerable<string> keys)
        {
            if (keys == null) return false;
            foreach (var k in keys) if (Get(k)) return true;
            return false;
        }

        // --- Lifecycle --------------------------------------------------------

        /// <summary>
        /// Resets all flags. Called on New Game / death restart so the next run
        /// doesn't see prior progress. Does NOT clear OnChanged subscriptions.
        /// Intentionally SILENT — firing OnChanged for every cleared key during a
        /// bulk reset would re-invoke OnState DialogueTrigger subscribers (e.g.
        /// any re-readable node with no condition) in the middle of the reset,
        /// which could queue a popup on the main menu frame. Subscribers that
        /// need to react to "fresh run" should watch the scene-load instead.
        /// </summary>
        public static void Clear()
        {
            _bools.Clear();
            _ints.Clear();
        }

        /// <summary>Read-only snapshot for save/debug. Returns a copy.</summary>
        public static Dictionary<string, bool> SnapshotBools() => new(_bools);
        public static Dictionary<string, int>  SnapshotInts()  => new(_ints);
    }
}
