#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;
using PrismZone.UI;

/// <summary>
/// Creates an empty level scene template at Assets/Scenes/Level_Template.unity.
/// Contains ONLY the always-needed infrastructure (Bootstrap, Camera, Canvas, Grid)
/// and drops the prefabs so designers have live references to drop into the scene.
///
/// Designer workflow:
///   Assets > Scenes > Level_Template.unity > right-click > Duplicate > rename Level_XX
///   Open the new scene and paint walls + drop prefabs from Assets/Prefabs/.
/// </summary>
public static class MakeLevelTemplate
{
    private const string Path = "Assets/Scenes/Level_Template.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Bootstrap: instance the _Bootstrap prefab (holds Managers).
        InstantiatePrefab("_Bootstrap", Vector3.zero);

        // --- Main Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.075f, 0.075f, 0.1f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();
        var ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.refResolutionX = 640;
        ppc.refResolutionY = 360;
        ppc.assetsPPU = 32;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
        var ud = camGo.AddComponent<UniversalAdditionalCameraData>();
        ud.renderPostProcessing = true;

        // --- Global Light 2D (so 2D sprites render lit; URP 2D default)
        var lightGo = new GameObject("Global Light 2D");
        var l2d = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        l2d.color = Color.white;
        l2d.intensity = 1f;

        // --- Grid + two empty Tilemaps
        var gridGo = new GameObject("Grid");
        gridGo.AddComponent<Grid>();

        CreateTilemapChild(gridGo, "Tilemap_Ground", "BG", -10, withCollider: false);
        var wallsGo = CreateTilemapChild(gridGo, "Tilemap_Walls", "Mid", 0, withCollider: true);
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0) wallsGo.layer = wallLayer;

        // --- Canvas (copy same structure as SampleScene but with nothing added yet; user
        // can drop _Bootstrap which already holds UI prefab references, OR they can
        // manually build Canvas. For template simplicity we just place an empty Canvas.)
        var canvasGo = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.matchWidthOrHeight = 0.5f;

        // --- EventSystem
        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // --- Comment GO so designer sees a note in hierarchy
        new GameObject("----- Drop prefabs from Assets/Prefabs/ below -----");

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Path);
        Debug.Log($"[Template] Created {Path}");
    }

    static GameObject InstantiatePrefab(string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{name}.prefab");
        if (prefab == null) { Debug.LogWarning($"[Template] Missing prefab: {name}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go != null) go.transform.position = pos;
        return go;
    }

    static GameObject CreateTilemapChild(GameObject parent, string name, string sortingLayer, int order, bool withCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        // Sorting layer may not exist in a fresh project — fall back to Default
        bool hasLayer = false;
        foreach (var sl in SortingLayer.layers) if (sl.name == sortingLayer) { hasLayer = true; break; }
        tr.sortingLayerName = hasLayer ? sortingLayer : "Default";
        tr.sortingOrder = order;

        if (withCollider)
        {
            var tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            go.AddComponent<CompositeCollider2D>();
        }
        return go;
    }
}
#endif
