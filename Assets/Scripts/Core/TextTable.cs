using System.Collections.Generic;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// ScriptableObject holding the imported text rows (FRAMEWORK.md §3.4).
    ///
    /// Lifecycle:
    ///   - Editor: <c>TextTableImporter</c> reads <c>Assets/Config/text_table.tsv</c>
    ///     → repopulates <see cref="entries"/> on this asset.
    ///   - Runtime: first call to any static accessor lazy-loads
    ///     <c>Resources/TextTable.asset</c> and builds an id→entry index.
    ///
    /// Static API mirrors what the framework spec lists; instance methods on the
    /// asset itself are also fine if a caller already holds the SO reference.
    /// </summary>
    [CreateAssetMenu(menuName = "PrismZone/Text Table", fileName = "TextTable")]
    public class TextTable : ScriptableObject
    {
        public List<TextEntry> entries = new();

        // --- Runtime singleton ------------------------------------------------

        private static TextTable _instance;
        private static Dictionary<string, TextEntry> _index;

        public static TextTable Instance
        {
            get
            {
                if (_instance == null) Load();
                return _instance;
            }
        }

        private static void Load()
        {
            _instance = Resources.Load<TextTable>("TextTable");
            if (_instance == null)
            {
                Debug.LogWarning("[TextTable] Resources/TextTable.asset not found. Run Tools > Prism Zone > Import Text Table.");
                _instance = ScriptableObject.CreateInstance<TextTable>();
            }
            BuildIndex();
        }

        private static void BuildIndex()
        {
            _index = new Dictionary<string, TextEntry>(_instance.entries.Count);
            foreach (var e in _instance.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id)) continue;
                _index[e.id] = e;
            }
        }

        /// <summary>Editor: invalidate the cached singleton so re-import takes effect this play.</summary>
        public static void Invalidate()
        {
            _instance = null;
            _index = null;
        }

        // --- Public API -------------------------------------------------------

        public static TextEntry Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_instance == null) Load();
            return _index.TryGetValue(id, out var e) ? e : null;
        }

        /// <summary>
        /// Resolve final text for a node + page + lang + active filter.
        /// Returns the id back if missing, so designers see "T-99" on screen
        /// instead of empty bubbles when they typo.
        /// </summary>
        public static string T(string id, int page = 0, string lang = null, FilterColor filter = FilterColor.None)
        {
            var entry = Get(id);
            if (entry == null) return id;
            lang ??= I18nManager.CurrentLang;

            string raw = PickColumn(entry, lang, filter);
            return Page(raw, page);
        }

        public static int PageCount(string id, string lang = null, FilterColor filter = FilterColor.None)
        {
            var entry = Get(id);
            if (entry == null) return 0;
            lang ??= I18nManager.CurrentLang;
            string raw = PickColumn(entry, lang, filter);
            if (string.IsNullOrEmpty(raw)) return 0;
            // Pipe-separated pages.
            int count = 1;
            for (int i = 0; i < raw.Length; i++) if (raw[i] == '|') count++;
            return count;
        }

        public static IEnumerable<string> GetAllIds(TextEntry.Category? category = null)
        {
            if (_instance == null) Load();
            foreach (var e in _instance.entries)
            {
                if (e == null) continue;
                if (category.HasValue && e.category != category.Value) continue;
                yield return e.id;
            }
        }

        // --- Internals --------------------------------------------------------

        // Filter override rules (FRAMEWORK §3.5):
        //   - filter=Red and the *_red column for this lang is non-empty → use it
        //   - filter=Green and the *_green column for this lang is non-empty → use it
        //   - otherwise use the base column for the lang
        private static string PickColumn(TextEntry e, string lang, FilterColor filter)
        {
            bool zh = lang == "zh";
            string baseCol = zh ? e.zh : e.en;
            switch (filter)
            {
                case FilterColor.Red:
                    var redCol = zh ? e.zh_red : e.en_red;
                    return string.IsNullOrEmpty(redCol) ? baseCol : redCol;
                case FilterColor.Green:
                    var greenCol = zh ? e.zh_green : e.en_green;
                    return string.IsNullOrEmpty(greenCol) ? baseCol : greenCol;
                default:
                    return baseCol;
            }
        }

        private static string Page(string raw, int page)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            if (page <= 0)
            {
                int firstPipe = raw.IndexOf('|');
                if (firstPipe < 0) return raw.Replace("\\n", "\n");
                return raw.Substring(0, firstPipe).Replace("\\n", "\n");
            }
            // Walk to the requested page, scanning forward.
            int idx = 0;
            int pagesSeen = 0;
            while (pagesSeen < page && idx < raw.Length)
            {
                int next = raw.IndexOf('|', idx);
                if (next < 0) return string.Empty; // requested page beyond end
                idx = next + 1;
                pagesSeen++;
            }
            int end = raw.IndexOf('|', idx);
            string slice = end < 0 ? raw.Substring(idx) : raw.Substring(idx, end - idx);
            return slice.Replace("\\n", "\n");
        }
    }
}
