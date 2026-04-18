#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verifies every PNG in Assets/Art/Sprites/ matches GAME-SPEC pixel sizes at 32 PPU.
/// Reports mismatches as warnings and confirms importer settings (Sprite, PPU 32, Point filter).
/// </summary>
public static class VerifySpriteSizes
{
    // Expected pixel sizes per spec. Filename prefix (case-insensitive) -> (w, h, purpose).
    static readonly (string match, int w, int h, string purpose)[] Rules = {
        ("char_player",   48, 24, "Player (1.5×0.75)"),
        ("npc_green",     48, 24, "GREEN NPC (1.5×0.75)"),
        ("npc_red",       32, 32, "RED NPC (1×1)"),
        ("npc_blue",      24, 24, "BLUE trap (0.75×0.75)"),
        ("item_glasses",  24, 24, "Item icon (0.75)"),
        ("item_note",     24, 24, "Item icon (0.75)"),
        ("prop_cabinet",  32, 64, "Cabinet (1×2)"),
        ("prop_door",     32, 64, "Door (1×2)"),
        ("tile_floor",    32, 32, "Tile (1×1)"),
        ("tile_wall",     32, 32, "Tile (1×1)"),
        ("tile_grass",    32, 32, "Tile (1×1)"),
    };

    public static void Execute()
    {
        string root = "Assets/Art/Sprites";
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
        int total = 0, okCount = 0, sizeFail = 0, importerFail = 0, unknown = 0;
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("=== Sprite Size / Importer Audit ===");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            total++;

            string filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            int w = tex.width, h = tex.height;

            var rule = FindRule(filename);
            string status = "";
            bool sizeOk = false;
            if (rule.match == null)
            {
                status = $"[?] {path}  {w}x{h}  (no rule — unknown purpose)";
                unknown++;
            }
            else if (rule.w != w || rule.h != h)
            {
                status = $"[X] {path}  got={w}x{h}  expected={rule.w}x{rule.h}  ({rule.purpose})";
                sizeFail++;
            }
            else
            {
                sizeOk = true;
                okCount++;
            }

            // Importer check for sprites we care about (pixel art settings)
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            string impIssues = "";
            if (imp != null)
            {
                if (imp.textureType != TextureImporterType.Sprite) impIssues += " type!=Sprite";
                if (imp.spritePixelsPerUnit != 32f) impIssues += $" PPU={imp.spritePixelsPerUnit}";
                if (imp.filterMode != FilterMode.Point) impIssues += $" filter={imp.filterMode}";
                if (imp.textureCompression != TextureImporterCompression.Uncompressed) impIssues += $" comp={imp.textureCompression}";
                if (imp.mipmapEnabled) impIssues += " mipmapEnabled";
            }

            if (!string.IsNullOrEmpty(impIssues))
            {
                if (sizeOk) status = $"[⚠] {path}  {w}x{h}  importer:{impIssues}";
                else status += "  importer:" + impIssues;
                importerFail++;
            }
            else if (sizeOk)
            {
                status = $"[✓] {path}  {w}x{h}  ({rule.purpose})";
            }

            lines.AppendLine(status);
        }

        lines.AppendLine($"--- Summary: total={total} ok={okCount} sizeFail={sizeFail} importerFail={importerFail} unknown={unknown}");
        Debug.Log(lines.ToString());
    }

    static (string match, int w, int h, string purpose) FindRule(string lowerFilename)
    {
        foreach (var r in Rules)
            if (lowerFilename.StartsWith(r.match)) return r;
        return (null, 0, 0, "");
    }
}
#endif
