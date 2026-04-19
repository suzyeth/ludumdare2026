#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixCluePopup
{
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        var cp = canvas.transform.Find("CluePopup");
        if (cp != null)
        {
            cp.gameObject.SetActive(false);
            Debug.Log("[FixCluePopup] Deactivated CluePopup.");
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
