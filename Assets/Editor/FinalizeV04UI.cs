#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalizeV04UI
{
    public static void Execute()
    {
        // Explicit sequential: ensure panel present in SampleScene, save, then rebuild prefab.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Finalize] no Canvas"); return; }

        if (canvas.transform.Find("ItemDetailPanel") == null)
        {
            SetupV04Content.BuildItemDetailPanelPublic(canvas);
            Debug.Log("[Finalize] Added ItemDetailPanel");
        }
        else Debug.Log("[Finalize] ItemDetailPanel already present");

        int childCount = canvas.transform.childCount;
        Debug.Log($"[Finalize] Canvas has {childCount} children");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Finalize] Scene saved");

        RebuildHUDPrefab.Execute();
    }
}
#endif
