#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 1. Duplicates Renderer2D.asset -> Renderer2D_UI.asset (no FullScreen feature).
/// 2. Registers Renderer2D_UI with the URP pipeline asset (gets rendererIndex 1).
/// 3. Assigns UI Camera to use that renderer.
/// </summary>
public static class MakeUIRenderer
{
    public static void Execute()
    {
        const string src = "Assets/Settings/Renderer2D.asset";
        const string dst = "Assets/Settings/Renderer2D_UI.asset";

        if (!File.Exists(dst))
        {
            AssetDatabase.CopyAsset(src, dst);
            AssetDatabase.Refresh();
            Debug.Log("[UIRenderer] Copied Renderer2D.asset -> Renderer2D_UI.asset");
        }

        // Load duplicate and strip FullScreen feature
        var uiRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(dst);
        if (uiRenderer == null) { Debug.LogError("[UIRenderer] Failed to load " + dst); return; }

        // The rendererFeatures list includes every feature attached. Drop FullScreenPass.
        for (int i = uiRenderer.rendererFeatures.Count - 1; i >= 0; i--)
        {
            var f = uiRenderer.rendererFeatures[i];
            if (f == null) { uiRenderer.rendererFeatures.RemoveAt(i); continue; }
            if (f.GetType().Name.Contains("FullScreenPass"))
            {
                uiRenderer.rendererFeatures.RemoveAt(i);
                Debug.Log("[UIRenderer] Removed FullScreenPass feature from UI renderer");
            }
        }
        EditorUtility.SetDirty(uiRenderer);

        // Register with URP pipeline asset
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (urp == null) { Debug.LogError("[UIRenderer] No UniversalRP.asset"); return; }

        var so = new SerializedObject(urp);
        var listProp = so.FindProperty("m_RendererDataList");
        int uiIndex = -1;
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == uiRenderer)
            {
                uiIndex = i; break;
            }
        }
        if (uiIndex < 0)
        {
            int idx = listProp.arraySize;
            listProp.arraySize = idx + 1;
            listProp.GetArrayElementAtIndex(idx).objectReferenceValue = uiRenderer;
            so.ApplyModifiedProperties();
            uiIndex = idx;
            Debug.Log($"[UIRenderer] Registered UI renderer at index {uiIndex}");
        }
        else
        {
            Debug.Log($"[UIRenderer] UI renderer already at index {uiIndex}");
        }

        // Assign UI Camera to use that renderer
        var uiCamGo = GameObject.Find("UI Camera");
        if (uiCamGo == null) { Debug.LogError("No UI Camera"); return; }
        var extra = uiCamGo.GetComponent<UniversalAdditionalCameraData>();
        extra.SetRenderer(uiIndex);
        EditorUtility.SetDirty(uiCamGo);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        Debug.Log("[UIRenderer] Done. UI Camera renderer index = " + uiIndex);
    }
}
#endif
