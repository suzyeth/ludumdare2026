#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 1. Saves the scene's Canvas (with all UI children) as HUD_Canvas.prefab.
/// 2. Saves EventSystem as EventSystem.prefab (if not already a prefab instance).
/// 3. Reconnects the scene Canvas as a prefab instance.
///
/// After this, any scene just needs to drop HUD_Canvas and EventSystem from
/// Assets/Prefabs/ to get full working UI.
/// </summary>
public static class MakeCanvasPrefab
{
    private const string Folder = "Assets/Prefabs";

    public static void Execute()
    {
        SaveAsPrefab("Canvas", "HUD_Canvas");
        SaveAsPrefab("EventSystem", "EventSystem");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[MakeCanvas] Done");
    }

    static void SaveAsPrefab(string goName, string prefabName)
    {
        var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject go = null;
        foreach (var g in all)
        {
            if (g.name != goName) continue;
            if (g.transform.parent == null) { go = g; break; }
        }
        if (go == null) { Debug.LogWarning($"[MakeCanvas] Missing {goName}"); return; }
        if (PrefabUtility.IsPartOfPrefabInstance(go))
        {
            Debug.Log($"[MakeCanvas] {goName} already prefab instance — skip");
            return;
        }

        string path = $"{Folder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        Debug.Log($"[MakeCanvas] Saved {path}");
    }
}
#endif
