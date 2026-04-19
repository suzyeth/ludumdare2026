#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SaveAll
{
    public static void Execute()
    {
        // Save active scene + mark dirty first (in case Inspector edits weren't flushed)
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Save every dirty loaded scene
        EditorSceneManager.SaveOpenScenes();

        // Flush project assets (ScriptableObjects, material tweaks, prefab refs)
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log($"[SaveAll] Saved scene '{scene.name}' + all dirty assets.");
    }
}
#endif
