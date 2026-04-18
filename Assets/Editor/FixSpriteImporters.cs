#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FixSpriteImporters
{
    [UnityEditor.MenuItem("Tools/Prism Zone/Fix Sprite Importers")]
    public static void RunFromMenu() => Execute();

    public static void Execute()
    {
        string root = "Assets/Art/Sprites";
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
        int touched = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spritePixelsPerUnit != 32f)                { imp.spritePixelsPerUnit = 32f;                changed = true; }
            if (imp.filterMode != FilterMode.Point)            { imp.filterMode = FilterMode.Point;            changed = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                                                               { imp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (imp.mipmapEnabled)                             { imp.mipmapEnabled = false;                    changed = true; }
            if (imp.spriteImportMode == SpriteImportMode.None) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            // wrap
            if (imp.wrapMode != TextureWrapMode.Clamp)         { imp.wrapMode = TextureWrapMode.Clamp;          changed = true; }

            if (changed)
            {
                imp.SaveAndReimport();
                touched++;
            }
        }
        Debug.Log($"[FixSprite] Updated importer on {touched} sprite files under {root}");
    }
}
#endif
