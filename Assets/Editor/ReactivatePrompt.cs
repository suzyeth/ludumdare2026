#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReactivatePrompt
{
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas"); return; }
        var ipT = canvas.transform.Find("InteractPrompt");
        if (ipT == null) { Debug.LogError("No InteractPrompt under Canvas"); return; }
        ipT.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        Debug.Log("[ReactivatePrompt] Done");
    }
}
#endif
