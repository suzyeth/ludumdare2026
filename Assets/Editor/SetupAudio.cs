#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

/// <summary>
/// One-shot: creates Assets/Resources/Audio/SoundCatalog.asset (prefilled with every
/// SoundId and blank clip slots) and adds AudioManager to the _Bootstrap prefab.
///
/// Menu: Tools > Prism Zone > Setup Audio
/// </summary>
public static class SetupAudio
{
    private const string CatalogPath = "Assets/Resources/Audio/SoundCatalog.asset";
    private const string BootstrapPrefab = "Assets/Prefabs/_Bootstrap.prefab";

    [MenuItem("Tools/Prism Zone/Setup Audio")]
    public static void Execute()
    {
        // 1. Catalog asset
        var catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log("[SetupAudio] Created " + CatalogPath);
        }
        PrefillCatalog(catalog);

        // 2. Attach AudioManager to _Bootstrap prefab
        var contents = PrefabUtility.LoadPrefabContents(BootstrapPrefab);
        if (contents == null) { Debug.LogError("Missing _Bootstrap prefab"); return; }
        try
        {
            var am = contents.GetComponent<AudioManager>();
            if (am == null)
            {
                am = contents.AddComponent<AudioManager>();
                Debug.Log("[SetupAudio] Added AudioManager to _Bootstrap prefab");
            }
            var so = new SerializedObject(am);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(contents, BootstrapPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupAudio] Done");
    }

    static void PrefillCatalog(SoundCatalog catalog)
    {
        var so = new SerializedObject(catalog);
        var arr = so.FindProperty("entries");

        var allIds = System.Enum.GetValues(typeof(SoundId));
        // Keep existing user edits — only add IDs that are missing.
        var existing = new System.Collections.Generic.HashSet<SoundId>();
        for (int i = 0; i < arr.arraySize; i++)
        {
            var idProp = arr.GetArrayElementAtIndex(i).FindPropertyRelative("id");
            existing.Add((SoundId)idProp.intValue);
        }

        int added = 0;
        foreach (SoundId id in allIds)
        {
            if (id == SoundId.None) continue;
            if (existing.Contains(id)) continue;
            int idx = arr.arraySize;
            arr.arraySize = idx + 1;
            var entry = arr.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("id").intValue = (int)id;
            entry.FindPropertyRelative("volume").floatValue = 1f;
            entry.FindPropertyRelative("pitch").floatValue = 1f;
            entry.FindPropertyRelative("loop").boolValue = false;
            entry.FindPropertyRelative("isMusic").boolValue = id >= SoundId.BgmMenu;
            added++;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        if (added > 0) Debug.Log($"[SetupAudio] Prefilled {added} empty SoundId slots in catalog");
    }
}
#endif
