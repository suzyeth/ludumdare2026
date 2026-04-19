using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class _ApplyFlashbackPanel
{
    public static void Execute()
    {
        // Find the FlashbackPanel under HUD_Canvas in active scene.
        var scene = SceneManager.GetActiveScene();
        GameObject flashbackPanel = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "HUD_Canvas")
            {
                var t = root.transform.Find("FlashbackPanel");
                if (t != null) { flashbackPanel = t.gameObject; break; }
            }
        }
        if (flashbackPanel == null)
        {
            Debug.LogError("[_ApplyFlashbackPanel] HUD_Canvas/FlashbackPanel not found in active scene.");
            return;
        }

        const string prefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        if (!PrefabUtility.IsAddedGameObjectOverride(flashbackPanel))
        {
            Debug.LogWarning("[_ApplyFlashbackPanel] FlashbackPanel is not an added-GameObject override. Skipping.");
            return;
        }

        PrefabUtility.ApplyAddedGameObject(flashbackPanel, prefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[_ApplyFlashbackPanel] Applied FlashbackPanel to {prefabPath}");
        EditorSceneManager.SaveOpenScenes();
    }
}
