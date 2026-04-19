#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GenPlaceholderSprites
{
    private const string OutFolder = "Assets/Art/Sprites";
    private const int Size = 32;

    public static void Execute()
    {
        if (!AssetDatabase.IsValidFolder(OutFolder))
        {
            Directory.CreateDirectory(OutFolder);
            AssetDatabase.Refresh();
        }

        // (filename, color)
        var specs = new (string name, Color c)[] {
            ("S_Player",  HexToColor("#EFE3C2")),
            ("S_Red",     HexToColor("#E53935")),
            ("S_Green",   HexToColor("#43A047")),
            ("S_Blue",    HexToColor("#1E88E5")),
            ("S_Wall",    HexToColor("#444444")),
            ("S_Door",    HexToColor("#8B5E3C")),
            ("S_Cabinet", HexToColor("#5A3A1A")),
            ("S_Floor",   HexToColor("#2A2A2A")),
        };

        foreach (var (name, color) in specs)
        {
            string path = $"{OutFolder}/{name}.png";
            WritePng(path, color);
        }

        AssetDatabase.Refresh();

        // Configure as sprites, PPU 32, Point filter
        foreach (var (name, _) in specs)
        {
            string path = $"{OutFolder}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private static void WritePng(string path, Color color)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color32[Size * Size];
        var c32 = (Color32)color;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c32;
        tex.SetPixels32(pixels);
        tex.Apply(false);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return Color.magenta;
    }
}
#endif
