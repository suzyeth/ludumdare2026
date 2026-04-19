using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.EditorTools
{
    /// <summary>
    /// Reads <c>Assets/Config/text_table.tsv</c> and writes
    /// <c>Assets/Resources/TextTable.asset</c> + regenerates <c>FlagKeys.cs</c>.
    ///
    /// Triggers (FRAMEWORK.md §6.1):
    ///   - Manual menu: Tools > Prism Zone > Import Text Table
    ///   - Auto: <see cref="TextTableAssetPostprocessor"/> watches TSV save.
    ///
    /// Format expected (header row required, tab-delimited, 20 cols documented in
    /// <c>FRAMEWORK.md §3.5</c>; missing trailing cols are tolerated). Lines whose
    /// id is empty or starts with '#' are skipped (designer comments).
    /// </summary>
    public static class TextTableImporter
    {
        private const string TsvPath  = "Assets/Config/text_table.tsv";
        private const string SoFolder = "Assets/Resources";
        private const string SoPath   = "Assets/Resources/TextTable.asset";

        [MenuItem("Tools/Prism Zone/Import Text Table")]
        public static void RunFromMenu() => ImportNow(verbose: true);

        public static bool ImportNow(bool verbose)
        {
            if (!File.Exists(TsvPath))
            {
                Debug.LogError($"[TextTableImporter] TSV not found at {TsvPath}");
                return false;
            }
            string text = File.ReadAllText(TsvPath);
            var entries = ParseTsv(text, out int errors);
            if (entries == null) return false;

            // Load-or-create the SO.
            if (!Directory.Exists(SoFolder)) Directory.CreateDirectory(SoFolder);
            var so = AssetDatabase.LoadAssetAtPath<TextTable>(SoPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<TextTable>();
                AssetDatabase.CreateAsset(so, SoPath);
            }
            so.entries = entries;
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            TextTable.Invalidate();

            // Regenerate the auto-side of FlagKeys.
            int newFlags = FlagKeysGenerator.Regenerate(entries);

            if (verbose)
                Debug.Log($"[TextTableImporter] Imported {entries.Count} rows ({errors} errors). FlagKeys regenerated with {newFlags} entries.");
            AssetDatabase.Refresh();
            return errors == 0;
        }

        // --- TSV parsing ------------------------------------------------------

        private static List<TextEntry> ParseTsv(string text, out int errorCount)
        {
            errorCount = 0;
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = text.Split('\n');
            if (lines.Length == 0) { Debug.LogError("[TextTableImporter] Empty TSV."); return null; }

            var header = lines[0].Split('\t');
            int idx_id            = IndexOf(header, "id");
            int idx_category      = IndexOf(header, "category");
            int idx_avg_type      = IndexOf(header, "avg_type");
            int idx_zh            = IndexOf(header, "zh");
            int idx_en            = IndexOf(header, "en");
            int idx_zh_red        = IndexOf(header, "zh_red");
            int idx_en_red        = IndexOf(header, "en_red");
            int idx_zh_green      = IndexOf(header, "zh_green");
            int idx_en_green      = IndexOf(header, "en_green");
            int idx_filter_req    = IndexOf(header, "filter_req");
            int idx_repeatable    = IndexOf(header, "repeatable");
            int idx_cant_skip     = IndexOf(header, "cant_skip");
            int idx_auto_hide_sec = IndexOf(header, "auto_hide_sec");
            int idx_follow_up_id  = IndexOf(header, "follow_up_id");
            int idx_trigger_mode  = IndexOf(header, "trigger_mode");
            int idx_sfx           = IndexOf(header, "sfx");
            int idx_notes         = IndexOf(header, "notes");
            int idx_require       = IndexOf(header, "require_flags");
            int idx_forbid        = IndexOf(header, "forbid_flags");
            int idx_setflags      = IndexOf(header, "set_flags_on_fire");

            if (idx_id < 0) { Debug.LogError("[TextTableImporter] Header missing 'id' column."); return null; }

            var list = new List<TextEntry>(lines.Length);
            for (int li = 1; li < lines.Length; li++)
            {
                var raw = lines[li];
                if (string.IsNullOrEmpty(raw)) continue;
                if (raw.Length > 0 && raw[0] == '#') continue;

                var cells = raw.Split('\t');
                string id = Cell(cells, idx_id);
                if (string.IsNullOrEmpty(id) || id.StartsWith("#")) continue;

                var e = new TextEntry { id = id };
                e.category    = ParseEnum(Cell(cells, idx_category),    TextEntry.Category.dialogue);
                e.avgType     = ParseEnum(Cell(cells, idx_avg_type),    TextEntry.AvgType.None);
                e.zh          = Cell(cells, idx_zh);
                e.en          = Cell(cells, idx_en);
                e.zh_red      = Cell(cells, idx_zh_red);
                e.en_red      = Cell(cells, idx_en_red);
                e.zh_green    = Cell(cells, idx_zh_green);
                e.en_green    = Cell(cells, idx_en_green);
                e.filterReq   = ParseEnum(Cell(cells, idx_filter_req),  TextEntry.FilterReq.None);
                e.repeatable  = ParseBool(Cell(cells, idx_repeatable),  false);
                e.cantSkip    = ParseBool(Cell(cells, idx_cant_skip),   false);
                e.autoHideSec = ParseFloat(Cell(cells, idx_auto_hide_sec), 0f);
                e.followUpId  = Cell(cells, idx_follow_up_id);
                e.triggerMode = ParseEnum(Cell(cells, idx_trigger_mode), TextEntry.TriggerMode.Manual);
                e.sfx         = Cell(cells, idx_sfx);
                e.notes       = Cell(cells, idx_notes);
                e.requireFlagsRaw   = Cell(cells, idx_require);
                e.forbidFlagsRaw    = Cell(cells, idx_forbid);
                e.setFlagsOnFireRaw = Cell(cells, idx_setflags);

                e.requireFlags    = SplitCsvCell(e.requireFlagsRaw);
                e.forbidFlags     = SplitCsvCell(e.forbidFlagsRaw);
                e.setFlagsOnFire  = SplitCsvCell(e.setFlagsOnFireRaw);

                list.Add(e);
            }
            return list;
        }

        private static int IndexOf(string[] header, string key)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i]?.Trim(), key, System.StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        private static string Cell(string[] cells, int idx) => (idx < 0 || idx >= cells.Length) ? "" : (cells[idx] ?? "").Trim();
        private static T ParseEnum<T>(string s, T def) where T : struct
            => System.Enum.TryParse<T>(s, true, out var v) ? v : def;
        private static bool ParseBool(string s, bool def)
        {
            if (string.IsNullOrEmpty(s)) return def;
            s = s.Trim().ToLowerInvariant();
            if (s == "true" || s == "1" || s == "yes") return true;
            if (s == "false" || s == "0" || s == "no") return false;
            return def;
        }
        private static float ParseFloat(string s, float def)
            => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
        private static string[] SplitCsvCell(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return System.Array.Empty<string>();
            var parts = s.Split(',');
            var clean = new List<string>(parts.Length);
            foreach (var p in parts) { var t = p.Trim(); if (t.Length > 0) clean.Add(t); }
            return clean.ToArray();
        }
    }

    /// <summary>
    /// AssetPostprocessor that re-runs the import whenever the TSV is saved.
    /// Lets designer edit-and-save in Excel without ever touching Unity.
    /// </summary>
    internal class TextTableAssetPostprocessor : AssetPostprocessor
    {
        private const string TsvPath = "Assets/Config/text_table.tsv";
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
            {
                if (p.Replace('\\', '/').Equals(TsvPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    TextTableImporter.ImportNow(verbose: true);
                    return;
                }
            }
        }
    }
}
