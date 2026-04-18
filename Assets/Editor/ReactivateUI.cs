#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReactivateUI
{
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas"); return; }

        foreach (var name in new[] { "PasscodePanel", "CluePopup", "InteractPrompt" })
        {
            var t = canvas.transform.Find(name);
            if (t != null)
            {
                t.gameObject.SetActive(true);
                Debug.Log($"[ReactivateUI] Activated {name}");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
