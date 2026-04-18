#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class RevertToOverlay
{
    public static void Execute()
    {
        // 1. Delete UI Camera if present
        var uiCamGo = GameObject.Find("UI Camera");
        if (uiCamGo != null)
        {
            // First remove from Main Camera stack
            var main = GameObject.Find("Main Camera");
            if (main != null)
            {
                var extra = main.GetComponent<UniversalAdditionalCameraData>();
                if (extra != null)
                {
                    var uc = uiCamGo.GetComponent<Camera>();
                    if (uc != null) extra.cameraStack.Remove(uc);
                }
                var mc = main.GetComponent<Camera>();
                if (mc != null) mc.cullingMask = ~0; // everything
            }
            Object.DestroyImmediate(uiCamGo);
            Debug.Log("[Revert] Removed UI Camera + cleared main culling mask");
        }

        // 2. Canvas back to Overlay
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var c = canvas.GetComponent<Canvas>();
            c.worldCamera = null;
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[Revert] Canvas -> ScreenSpaceOverlay");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
