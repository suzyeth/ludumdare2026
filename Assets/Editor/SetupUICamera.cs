#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Splits rendering into two cameras so the user can keep Canvas in
/// ScreenSpaceCamera mode (better UI authoring in Scene view) without the
/// FullScreen filter pass eating the UI.
///
/// Main Camera: game world + filter; culls UI layer.
/// UI Camera  : Overlay stacked on Main; renders only UI layer; no post-FX.
/// Canvas     : ScreenSpaceCamera -> UI Camera.
/// </summary>
public static class SetupUICamera
{
    public static void Execute()
    {
        var main = GameObject.Find("Main Camera");
        if (main == null) { Debug.LogError("No Main Camera"); return; }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) { Debug.LogError("No UI layer"); return; }

        // --- Main Camera tweaks
        var mainCam = main.GetComponent<Camera>();
        mainCam.cullingMask &= ~(1 << uiLayer); // exclude UI

        var mainExtra = main.GetComponent<UniversalAdditionalCameraData>();
        if (mainExtra == null) mainExtra = main.AddComponent<UniversalAdditionalCameraData>();
        mainExtra.renderType = CameraRenderType.Base;

        // --- UI Camera: create or reuse
        var uiGo = GameObject.Find("UI Camera");
        if (uiGo == null)
        {
            uiGo = new GameObject("UI Camera");
            uiGo.transform.SetParent(main.transform, false);
            uiGo.transform.localPosition = Vector3.zero;
        }

        var uiCam = uiGo.GetComponent<Camera>();
        if (uiCam == null) uiCam = uiGo.AddComponent<Camera>();
        uiCam.clearFlags = CameraClearFlags.Depth;
        uiCam.cullingMask = 1 << uiLayer;        // only UI
        uiCam.orthographic = true;
        uiCam.orthographicSize = mainCam.orthographicSize;
        uiCam.nearClipPlane = 0.1f;
        uiCam.farClipPlane = 100f;

        var uiExtra = uiGo.GetComponent<UniversalAdditionalCameraData>();
        if (uiExtra == null) uiExtra = uiGo.AddComponent<UniversalAdditionalCameraData>();
        uiExtra.renderType = CameraRenderType.Overlay;
        uiExtra.renderPostProcessing = false;

        // Stack: main's overlay list must contain uiCam
        if (!mainExtra.cameraStack.Contains(uiCam))
        {
            mainExtra.cameraStack.Add(uiCam);
        }

        // --- Canvas
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = uiCam;
            c.planeDistance = 10f;
            canvas.layer = uiLayer;
            // Make all UI children also on UI layer
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = uiLayer;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        Debug.Log("[SetupUICamera] Done: UI Camera stacked, Canvas -> UI Camera, main culls UI.");
    }
}
#endif
