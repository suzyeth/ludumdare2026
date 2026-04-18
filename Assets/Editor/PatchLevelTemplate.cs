#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds Assets/Scenes/Level_Template.unity so it uses the HUD_Canvas +
/// EventSystem prefabs (instead of the ad-hoc empty ones created earlier).
/// </summary>
public static class PatchLevelTemplate
{
    private const string ScenePath = "Assets/Scenes/Level_Template.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 1. Replace empty Canvas with prefab instance
        var oldCanvas = Find("Canvas");
        if (oldCanvas != null && !PrefabUtility.IsPartOfPrefabInstance(oldCanvas))
        {
            Object.DestroyImmediate(oldCanvas);
            Debug.Log("[Template] Deleted empty Canvas");
        }
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD_Canvas.prefab");
        if (hudPrefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            inst.name = "Canvas"; // keep standard name
            Debug.Log("[Template] Instanced HUD_Canvas prefab");
        }

        // 2. EventSystem
        var oldEs = Find("EventSystem");
        if (oldEs != null && !PrefabUtility.IsPartOfPrefabInstance(oldEs))
        {
            Object.DestroyImmediate(oldEs);
            Debug.Log("[Template] Deleted raw EventSystem");
        }
        var esPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EventSystem.prefab");
        if (esPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(esPrefab);
            Debug.Log("[Template] Instanced EventSystem prefab");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Template] Saved " + ScenePath);

        // Reopen SampleScene as primary
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
    }

    static GameObject Find(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == name && go.transform.parent == null) return go;
        }
        return null;
    }
}
#endif
