#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

/// <summary>
/// Adds PersistentRoot to each prefab that should survive scene loads. Run once.
/// </summary>
public static class AttachPersistent
{
    public static void Execute()
    {
        foreach (var name in new[] { "Player", "HUD_Canvas", "EventSystem", "_Bootstrap" })
        {
            string path = $"Assets/Prefabs/{name}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[Persistent] Missing {path}"); continue; }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (contents.GetComponent<PersistentRoot>() == null)
                {
                    contents.AddComponent<PersistentRoot>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Debug.Log($"[Persistent] Added PersistentRoot to {path}");
                }
                else
                {
                    Debug.Log($"[Persistent] {path} already has PersistentRoot");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
