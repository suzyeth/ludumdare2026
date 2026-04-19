#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Force every dismissable UI panel to alpha=0 at edit time AND bake that into the
/// HUD_Canvas prefab. Fixes the "two panels visible at once" bug caused by previous
/// test runs leaving CanvasGroup.alpha=1 in the saved scene.
/// </summary>
public static class ResetPanelAlphas
{
    [MenuItem("Tools/Prism Zone/Reset Panel Alphas")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        int reset = 0;
        foreach (var name in new[] {
            "InteractPrompt", "CluePopup", "PasscodePanel", "GameOverPanel",
            "ItemDetailPanel", "VictoryPanel", "PauseMenu"
        })
        {
            var go = GameObject.Find($"Canvas/{name}");
            if (go == null) continue;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) continue;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            EditorUtility.SetDirty(go);
            reset++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ResetAlpha] Reset {reset} panel CanvasGroups to alpha=0");

        RebuildHUDPrefab.Execute();
    }
}
#endif
