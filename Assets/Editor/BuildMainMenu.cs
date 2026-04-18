#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

public static class BuildMainMenu
{
    private const string Path = "Assets/Scenes/Scene_MainMenu.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera (UI-only scene, solid color)
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // EventSystem (via prefab)
        var esPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EventSystem.prefab");
        if (esPrefab != null) PrefabUtility.InstantiatePrefab(esPrefab);

        // Canvas (brand new Overlay, NOT the HUD prefab)
        var canvasGo = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.matchWidthOrHeight = 0.5f;

        // Title
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(canvasGo.transform, false);
        var trt = (RectTransform)titleGo.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0, 80);
        trt.sizeDelta = new Vector2(400, 60);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "PRISM ZONE";
        title.fontSize = 36;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        // Start button
        var startBtn = MakeButton(canvasGo.transform, "Btn_Start", new Vector2(0, 0), "Start");
        var quitBtn  = MakeButton(canvasGo.transform, "Btn_Quit",  new Vector2(0, -50), "Quit");

        // Menu controller
        var mm = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(mm);
        so.FindProperty("firstScene").stringValue = "SampleScene";
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("startButton").objectReferenceValue = startBtn;
        so.FindProperty("quitButton").objectReferenceValue = quitBtn;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Path);
        Debug.Log($"[MainMenu] Created {Path}");

        // Ensure build settings include MainMenu + SampleScene + Level_Template
        AddToBuildSettings(Path, true);
        AddToBuildSettings("Assets/Scenes/SampleScene.unity", false);

        // Reopen SampleScene as primary
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
    }

    static Button MakeButton(Transform parent, string name, Vector2 pos, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180, 40);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = labelGo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 18;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return btn;
    }

    static void AddToBuildSettings(string scenePath, bool putFirst)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int existing = scenes.FindIndex(s => s.path == scenePath);
        if (existing >= 0) { scenes[existing].enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); return; }
        var entry = new EditorBuildSettingsScene(scenePath, true);
        if (putFirst) scenes.Insert(0, entry); else scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[MainMenu] Build settings += {scenePath} (first={putFirst})");
    }
}
#endif
