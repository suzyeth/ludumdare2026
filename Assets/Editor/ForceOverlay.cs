#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ForceOverlay
{
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas"); return; }
        var c = canvas.GetComponent<Canvas>();
        c.worldCamera = null;              // clear so it can't fall back to Camera mode
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        Debug.Log($"[Overlay] mode={c.renderMode} worldCamera={(c.worldCamera==null?"null":c.worldCamera.name)}");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
