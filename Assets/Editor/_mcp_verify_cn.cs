using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;

public static class _McpVerifyCn
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("== TMP Global Fallback ==");
        var gft = TMP_Settings.fallbackFontAssets;
        if (gft != null)
            foreach (var f in gft) sb.AppendLine($"  {(f != null ? f.name : "null")}");

        sb.AppendLine();
        sb.AppendLine("== LiberationSans SDF fallback ==");
        var libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (libSans != null && libSans.fallbackFontAssetTable != null)
            foreach (var f in libSans.fallbackFontAssetTable) sb.AppendLine($"  {(f != null ? f.name : "null")}");
        else sb.AppendLine("  (no per-font fallback)");

        sb.AppendLine();
        sb.AppendLine("== NotoSansSC SDF sanity ==");
        var noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansSC SDF.asset");
        if (noto != null)
        {
            sb.AppendLine($"  chars={noto.characterTable.Count} glyphs={noto.glyphTable.Count}  mode={noto.atlasPopulationMode}  sourceTtf={(noto.sourceFontFile!=null?noto.sourceFontFile.name:"null")}");
            foreach (var c in new[]{'中','文','测','试','你','好','棱','镜','禁','区'})
            {
                sb.AppendLine($"    '{c}' (0x{((int)c):X4}) -> {(noto.HasCharacter(c, true)?"ok":"MISSING")}");
            }
        }
        else sb.AppendLine("  NotoSansSC SDF missing!");

        sb.AppendLine();
        sb.AppendLine("== Zpix SDF sanity ==");
        var zpix = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Zpix SDF.asset");
        if (zpix != null)
            sb.AppendLine($"  chars={zpix.characterTable.Count} glyphs={zpix.glyphTable.Count}");
        else sb.AppendLine("  Zpix SDF missing!");

        return sb.ToString();
    }
}
