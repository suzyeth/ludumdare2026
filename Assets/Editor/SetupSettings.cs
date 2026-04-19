#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

/// <summary>
/// One-shot: builds a SettingsPanel child on both the gameplay HUD Canvas and
/// the Main Menu canvas, inserts a "Settings" button in MainMenu + PauseMenu,
/// and refreshes HUD_Canvas.prefab so future scenes pick it up.
///
/// Menu: Tools > Prism Zone > Setup Settings Panel
///
/// Safe to re-run: existing SettingsPanel children are reused, buttons are
/// detected by name and skipped if already present.
/// </summary>
public static class SetupSettings
{
    private const string SampleScenePath   = "Assets/Scenes/SampleScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/Scene_MainMenu.unity";

    [MenuItem("Tools/Prism Zone/Setup Settings Panel")]
    public static void Execute()
    {
        // 1. Gameplay scene — SettingsPanel lives on Canvas next to PauseMenu
        var game = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        var gameCanvas = GameObject.Find("Canvas");
        if (gameCanvas != null)
        {
            EnsureSettingsPanel(gameCanvas);
            InjectPauseMenuSettingsButton(gameCanvas);
        }
        else Debug.LogWarning("[Settings] SampleScene has no Canvas");
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        // Refresh HUD_Canvas prefab so Level_Template + future scenes inherit both
        // the new SettingsPanel child and the updated PauseMenu button wiring.
        RebuildHUDPrefab.Execute();

        // 2. Main Menu scene — own Canvas, own Settings button
        if (System.IO.File.Exists(MainMenuScenePath))
        {
            var menu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var menuCanvas = GameObject.Find("Canvas");
            if (menuCanvas != null)
            {
                EnsureSettingsPanel(menuCanvas);
                InjectMainMenuSettingsButton(menuCanvas);
            }
            else Debug.LogWarning("[Settings] Scene_MainMenu has no Canvas");
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Settings] Setup complete — settings reachable from MainMenu + Pause");
    }

    // ---------------------------------------------------------------- Builders

    private static GameObject EnsureSettingsPanel(GameObject canvas)
    {
        var existing = canvas.transform.Find("SettingsPanel");
        if (existing != null) { Debug.Log("[Settings] Panel already on " + canvas.name); return existing.gameObject; }

        var go = new GameObject("SettingsPanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Panel frame
        var frame = new GameObject("Frame", typeof(RectTransform));
        frame.transform.SetParent(go.transform, false);
        var frt = (RectTransform)frame.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = new Vector2(420, 360);
        frt.anchoredPosition = Vector2.zero;
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        var title = MakeText(frame, "Title", new Vector2(0, 140), new Vector2(380, 36), "Settings", 26, FontStyles.Bold);

        // Volume rows
        var masterLabel = MakeText(frame, "MasterLabel", new Vector2(-120, 80), new Vector2(180, 24), "Master", 16);
        var masterSlider = MakeSlider(frame, "MasterSlider", new Vector2(60, 80), new Vector2(200, 18));
        var masterVal = MakeText(frame, "MasterValue", new Vector2(180, 80), new Vector2(50, 22), "100%", 14);

        var sfxLabel = MakeText(frame, "SfxLabel", new Vector2(-120, 40), new Vector2(180, 24), "SFX", 16);
        var sfxSlider = MakeSlider(frame, "SfxSlider", new Vector2(60, 40), new Vector2(200, 18));
        var sfxVal = MakeText(frame, "SfxValue", new Vector2(180, 40), new Vector2(50, 22), "80%", 14);

        var musicLabel = MakeText(frame, "MusicLabel", new Vector2(-120, 0), new Vector2(180, 24), "Music", 16);
        var musicSlider = MakeSlider(frame, "MusicSlider", new Vector2(60, 0), new Vector2(200, 18));
        var musicVal = MakeText(frame, "MusicValue", new Vector2(180, 0), new Vector2(50, 22), "55%", 14);

        // Language row
        var langLabel = MakeText(frame, "LangLabel", new Vector2(-120, -46), new Vector2(180, 24), "Language", 16);
        var langBtn = MakeButton(frame, "LangButton", new Vector2(80, -46), new Vector2(140, 30), "English");
        var langBtnLabel = langBtn.GetComponentInChildren<TMP_Text>();

        // Action row
        var resetBtn = MakeButton(frame, "ResetButton", new Vector2(-90, -120), new Vector2(160, 36), "Reset");
        var closeBtn = MakeButton(frame, "CloseButton", new Vector2(90, -120), new Vector2(160, 36), "Close");

        var panel = go.AddComponent<SettingsPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("masterLabel").objectReferenceValue = masterLabel;
        so.FindProperty("sfxLabel").objectReferenceValue = sfxLabel;
        so.FindProperty("musicLabel").objectReferenceValue = musicLabel;
        so.FindProperty("languageLabel").objectReferenceValue = langLabel;
        so.FindProperty("masterSlider").objectReferenceValue = masterSlider;
        so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
        so.FindProperty("masterValueLabel").objectReferenceValue = masterVal;
        so.FindProperty("sfxValueLabel").objectReferenceValue = sfxVal;
        so.FindProperty("musicValueLabel").objectReferenceValue = musicVal;
        so.FindProperty("languageButton").objectReferenceValue = langBtn;
        so.FindProperty("languageButtonLabel").objectReferenceValue = langBtnLabel;
        so.FindProperty("resetButton").objectReferenceValue = resetBtn;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.ApplyModifiedProperties();

        Debug.Log("[Settings] Built SettingsPanel on " + canvas.name);
        return go;
    }

    // ------------------------------------------------------ Button injection

    private static void InjectPauseMenuSettingsButton(GameObject canvas)
    {
        var pauseGo = canvas.transform.Find("PauseMenu")?.gameObject;
        if (pauseGo == null) { Debug.LogWarning("[Settings] No PauseMenu child"); return; }
        var pause = pauseGo.GetComponent<PauseMenu>();
        if (pause == null) { Debug.LogWarning("[Settings] PauseMenu missing component"); return; }

        // Push existing buttons up by 20 so we can fit one more row, then add Settings.
        NudgeButton(pauseGo, "Btn_Resume",   new Vector2(0,  30));
        NudgeButton(pauseGo, "Btn_MainMenu", new Vector2(0, -60));
        NudgeButton(pauseGo, "Btn_Quit",     new Vector2(0,-110));

        Button settingsBtn;
        var existing = pauseGo.transform.Find("Btn_Settings");
        if (existing != null) settingsBtn = existing.GetComponent<Button>();
        else settingsBtn = MakeButton(pauseGo, "Btn_Settings", new Vector2(0, -20), new Vector2(160, 40), "Settings");

        var so = new SerializedObject(pause);
        so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
        so.ApplyModifiedProperties();
        Debug.Log("[Settings] PauseMenu: Settings button wired");
    }

    private static void InjectMainMenuSettingsButton(GameObject canvas)
    {
        // MainMenu hierarchy is built by BuildMainMenu.cs — use flexible lookup.
        var menuGo = canvas.transform.Find("MainMenu")?.gameObject ?? canvas;
        var menu = menuGo.GetComponent<MainMenu>() ?? canvas.GetComponentInChildren<MainMenu>();
        if (menu == null) { Debug.LogWarning("[Settings] No MainMenu component found"); return; }
        var host = menu.gameObject;

        // Insert Settings between Start and Quit.
        NudgeButton(host, "Btn_Start",    new Vector2(0,  30));
        NudgeButton(host, "Btn_Quit",     new Vector2(0, -50));
        NudgeButton(host, "StartButton",  new Vector2(0,  30));
        NudgeButton(host, "QuitButton",   new Vector2(0, -50));

        Button settingsBtn;
        var existing = host.transform.Find("Btn_Settings");
        if (existing != null) settingsBtn = existing.GetComponent<Button>();
        else settingsBtn = MakeButton(host, "Btn_Settings", new Vector2(0, -10), new Vector2(160, 40), "Settings");

        var so = new SerializedObject(menu);
        so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
        so.ApplyModifiedProperties();
        Debug.Log("[Settings] MainMenu: Settings button wired");
    }

    private static void NudgeButton(GameObject parent, string name, Vector2 pos)
    {
        var t = parent.transform.Find(name);
        if (t == null) return;
        var rt = (RectTransform)t;
        rt.anchoredPosition = pos;
    }

    // ---------------------------------------------------------- UI primitives

    private static TMP_Text MakeText(GameObject parent, string name, Vector2 pos, Vector2 size,
                                      string text, float fs, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fs;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.fontStyle = style;
        return t;
    }

    private static Slider MakeSlider(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fart = (RectTransform)fillArea.transform;
        fart.anchorMin = new Vector2(0, 0); fart.anchorMax = new Vector2(1, 1);
        fart.offsetMin = new Vector2(5, 4); fart.offsetMax = new Vector2(-15, -4);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.75f, 1f, 1f);
        var fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var hart = (RectTransform)handleArea.transform;
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one;
        hart.offsetMin = new Vector2(10, 0); hart.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRt = (RectTransform)handle.transform;
        handleRt.anchorMin = new Vector2(0, 0.5f); handleRt.anchorMax = new Vector2(0, 0.5f);
        handleRt.sizeDelta = new Vector2(16, 22);
        handleRt.anchoredPosition = Vector2.zero;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Button MakeButton(GameObject parent, string name, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var lgo = new GameObject("Label", typeof(RectTransform));
        lgo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)lgo.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = lgo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 16;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return btn;
    }
}
#endif
