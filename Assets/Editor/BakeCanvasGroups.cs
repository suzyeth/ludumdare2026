#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Force-add CanvasGroup at EDIT time to every UI panel that needs it, so audits
/// pass outside Play mode and the component is serialized into the prefab.
/// Also re-pins Scene_MainMenu to Build Settings index 0.
/// </summary>
public static class BakeCanvasGroups
{
    [MenuItem("Tools/Prism Zone/Bake CanvasGroups + BuildSettings")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        foreach (var name in new[] {
            "InteractPrompt", "CluePopup", "PasscodePanel", "GameOverPanel",
            "ItemDetailPanel", "VictoryPanel", "PauseMenu"
        })
        {
            var go = GameObject.Find($"Canvas/{name}");
            if (go == null) { Debug.Log($"[Bake] {name} not in scene (skip)"); continue; }
            if (go.GetComponent<CanvasGroup>() == null)
            {
                go.AddComponent<CanvasGroup>();
                Debug.Log($"[Bake] +CanvasGroup on Canvas/{name}");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Rebuild the HUD prefab from this baked scene Canvas
        RebuildHUDPrefab.Execute();

        // Re-pin MainMenu at Build Settings index 0
        FixBuildSettings();
    }

    static void FixBuildSettings()
    {
        const string menu = "Assets/Scenes/Scene_MainMenu.unity";
        const string sample = "Assets/Scenes/SampleScene.unity";

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        if (System.IO.File.Exists(menu))   list.Add(new EditorBuildSettingsScene(menu,   true));
        if (System.IO.File.Exists(sample)) list.Add(new EditorBuildSettingsScene(sample, true));

        // Append any other scenes that were already there but not our two
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == menu || s.path == sample) continue;
            list.Add(s);
        }
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Bake] Build Settings: MainMenu first, SampleScene second");
    }
}
#endif
