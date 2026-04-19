#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves each scene GameObject as a Prefab under Assets/Prefabs/, then replaces the
/// scene instance with a prefab connection (so future edits to the prefab propagate).
/// Skips if the GO is already a prefab instance.
/// </summary>
public static class MakePrefabs
{
    private const string PrefabFolder = "Assets/Prefabs";

    private static readonly string[] Targets = {
        "Player",
        "_Enemies/GREEN_Watcher",
        "_Interactables/Door_Passcode",
        "_Interactables/Cabinet",
        "_Interactables/Pickup_CluePaper",
        "_Bootstrap",
    };

    public static void Execute()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.Refresh();
        }

        int saved = 0;
        foreach (var path in Targets)
        {
            // Scene path uses "/" between transforms; find by traversal.
            var go = FindByScenePath(path);
            if (go == null) { Debug.LogWarning($"[Prefab] Missing: {path}"); continue; }

            // Skip already-prefabbed instances
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                Debug.Log($"[Prefab] {go.name} already instance — skip");
                continue;
            }

            string prefabName = go.name;
            string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            if (prefab != null) { saved++; Debug.Log($"[Prefab] Saved + connected: {prefabPath}"); }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        Debug.Log($"[Prefab] Done. Saved {saved} new prefabs.");
    }

    static GameObject FindByScenePath(string path)
    {
        var parts = path.Split('/');
        var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
        {
            // Match by tail name first, walk up to check full path
            if (go.name != parts[parts.Length - 1]) continue;
            var t = go.transform;
            bool match = true;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (t == null) { match = false; break; }
                if (t.name != parts[i]) { match = false; break; }
                t = t.parent;
            }
            if (match) return go;
        }
        return null;
    }
}
#endif
