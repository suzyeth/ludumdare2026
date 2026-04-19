#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicitly opens SampleScene, grabs its populated Canvas, saves as HUD_Canvas.prefab
/// (overwrite). Then patches Level_Template to re-instance the (now non-empty) prefab.
/// </summary>
public static class RebuildHUDPrefab
{
    public static void Execute()
    {
        // 1. Force SampleScene to be active
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        // 2. Find its Canvas
        GameObject canvas = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "Canvas" && go.transform.parent == null)
            { canvas = go; break; }
        }
        if (canvas == null) { Debug.LogError("[Rebuild] SampleScene has no root Canvas"); return; }
        Debug.Log($"[Rebuild] Found Canvas with {canvas.transform.childCount} children in SampleScene");

        // 3. Delete existing empty prefab asset
        const string prefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        if (System.IO.File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
            Debug.Log("[Rebuild] Deleted old empty prefab");
        }

        // 4. If scene's Canvas is already a (broken) prefab instance, unpack it so we can re-save it cleanly
        if (PrefabUtility.IsPartOfPrefabInstance(canvas))
        {
            PrefabUtility.UnpackPrefabInstance(canvas, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Debug.Log("[Rebuild] Unpacked instance → now plain scene hierarchy");
        }

        // 5. Save as new prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, prefabPath, InteractionMode.AutomatedAction);
        Debug.Log("[Rebuild] Saved " + prefabPath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 6. Re-patch Level_Template
        PatchTemplate();

        // Reopen SampleScene as primary
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
    }

    static void PatchTemplate()
    {
        const string tmplPath = "Assets/Scenes/Level_Template.unity";
        var scene = EditorSceneManager.OpenScene(tmplPath, OpenSceneMode.Single);

        // Remove old (empty) Canvas
        GameObject oldCanvas = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "Canvas" && go.transform.parent == null) { oldCanvas = go; break; }
        }
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
            Debug.Log("[Rebuild] Template: removed empty Canvas");
        }

        // Instance the populated prefab
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUD_Canvas.prefab");
        if (hudPrefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            inst.name = "Canvas";
            Debug.Log($"[Rebuild] Template: instanced HUD_Canvas ({hudPrefab.transform.childCount} children)");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, tmplPath);
    }
}
#endif
