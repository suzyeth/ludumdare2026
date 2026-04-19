#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

/// <summary>
/// Builds the AVG dialogue stack on HUD_Canvas: one DialogueManager + five
/// AvgPopup children (NAR bottom bar, READ page, TIP top strip, FLASH
/// fullscreen, ENV bubble). Safe to re-run — reuses existing children by name.
///
/// Menu: Tools > Prism Zone > Setup Dialogue Stack
/// </summary>
public static class SetupDialogue
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Prism Zone/Setup Dialogue Stack")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Dialogue] SampleScene has no Canvas"); return; }

        var nar   = EnsurePopup(canvas, "AVG_NAR",   DialogueType.NAR,   BuildNar);
        var read  = EnsurePopup(canvas, "AVG_READ",  DialogueType.READ,  BuildRead);
        var tip   = EnsurePopup(canvas, "AVG_TIP",   DialogueType.TIP,   BuildTip);
        var flash = EnsurePopup(canvas, "AVG_FLASH", DialogueType.FLASH, BuildFlash);
        var env   = EnsurePopup(canvas, "AVG_ENV",   DialogueType.ENV,   BuildEnv);

        var mgrGo = canvas.transform.Find("DialogueManager")?.gameObject;
        if (mgrGo == null)
        {
            mgrGo = new GameObject("DialogueManager");
            mgrGo.transform.SetParent(canvas.transform, false);
        }
        var mgr = mgrGo.GetComponent<DialogueManager>();
        if (mgr == null) mgr = mgrGo.AddComponent<DialogueManager>();

        var so = new SerializedObject(mgr);
        var arr = so.FindProperty("popups");
        arr.arraySize = 5;
        WireSlot(arr.GetArrayElementAtIndex(0), DialogueType.NAR,   nar);
        WireSlot(arr.GetArrayElementAtIndex(1), DialogueType.READ,  read);
        WireSlot(arr.GetArrayElementAtIndex(2), DialogueType.TIP,   tip);
        WireSlot(arr.GetArrayElementAtIndex(3), DialogueType.FLASH, flash);
        WireSlot(arr.GetArrayElementAtIndex(4), DialogueType.ENV,   env);
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        RebuildHUDPrefab.Execute();
        Debug.Log("[Dialogue] Dialogue stack installed on HUD_Canvas");
    }

    private static void WireSlot(SerializedProperty slot, DialogueType t, AvgPopup popup)
    {
        slot.FindPropertyRelative("type").enumValueIndex = (int)t;
        slot.FindPropertyRelative("popup").objectReferenceValue = popup;
    }

    private static AvgPopup EnsurePopup(GameObject canvas, string name, DialogueType type,
                                        System.Func<GameObject, (TMP_Text body, TMP_Text counter, bool skippable, float autoDismiss)> buildVisuals)
    {
        var existing = canvas.transform.Find(name)?.gameObject;
        if (existing != null)
        {
            var p = existing.GetComponent<AvgPopup>();
            if (p == null) p = existing.AddComponent<AvgPopup>();
            return p;
        }
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        var (body, counter, skippable, autoDismiss) = buildVisuals(go);

        var popup = go.AddComponent<AvgPopup>();
        var typew = body != null ? body.gameObject.AddComponent<TypewriterText>() : null;

        var so = new SerializedObject(popup);
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("bodyLabel").objectReferenceValue = body;
        so.FindProperty("pageCounterLabel").objectReferenceValue = counter;
        so.FindProperty("typewriter").objectReferenceValue = typew;
        so.FindProperty("skippable").boolValue = skippable;
        so.FindProperty("autoDismissSeconds").floatValue = autoDismiss;
        so.ApplyModifiedProperties();
        return popup;
    }

    // ----- Visual builders per style ------------------------------------------

    private static (TMP_Text, TMP_Text, bool, float) BuildNar(GameObject go)
    {
        Fullscreen(go);
        // Bottom translucent bar — spec: "屏幕下方半透明黑底文字条，无头像"
        var bar = MakeFrame(go, "Bar",
            anchorMin: new Vector2(0.05f, 0.04f), anchorMax: new Vector2(0.95f, 0.24f),
            color: new Color(0f, 0f, 0f, 0.78f));
        var body = MakeText(bar, "Body", fontSize: 20, align: TextAlignmentOptions.TopLeft,
            padding: new Vector4(24, 20, 24, 20));
        return (body, null, skippable: true, autoDismiss: 0f);
    }

    private static (TMP_Text, TMP_Text, bool, float) BuildRead(GameObject go)
    {
        Fullscreen(go);
        MakeBackdrop(go, new Color(0f, 0f, 0f, 0.55f));
        var page = MakeFrame(go, "Page",
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            size: new Vector2(640, 400), color: new Color(0.95f, 0.92f, 0.82f, 1f));
        var body = MakeText(page, "Body", fontSize: 22, align: TextAlignmentOptions.TopLeft,
            padding: new Vector4(36, 32, 36, 48), textColor: new Color(0.15f, 0.1f, 0.08f));
        var counter = MakeText(page, "PageCounter", fontSize: 14, align: TextAlignmentOptions.BottomRight,
            padding: new Vector4(0, 0, 20, 12), textColor: new Color(0.3f, 0.25f, 0.2f));
        return (body, counter, skippable: true, autoDismiss: 0f);
    }

    private static (TMP_Text, TMP_Text, bool, float) BuildTip(GameObject go)
    {
        Fullscreen(go);
        var bar = MakeFrame(go, "Bar",
            anchorMin: new Vector2(0.2f, 0.82f), anchorMax: new Vector2(0.8f, 0.92f),
            color: new Color(0.1f, 0.15f, 0.25f, 0.82f));
        var body = MakeText(bar, "Body", fontSize: 16, align: TextAlignmentOptions.Center,
            padding: new Vector4(12, 8, 12, 8), textColor: new Color(0.9f, 0.95f, 1f));
        return (body, null, skippable: true, autoDismiss: 4.5f);
    }

    private static (TMP_Text, TMP_Text, bool, float) BuildFlash(GameObject go)
    {
        Fullscreen(go);
        MakeBackdrop(go, Color.black);
        var body = MakeText(go, "Body", fontSize: 28, align: TextAlignmentOptions.Center,
            padding: new Vector4(80, 120, 80, 120), textColor: Color.white);
        return (body, null, skippable: false, autoDismiss: 0f);
    }

    private static (TMP_Text, TMP_Text, bool, float) BuildEnv(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 64);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        var body = MakeText(go, "Body", fontSize: 14, align: TextAlignmentOptions.Center,
            padding: new Vector4(12, 8, 12, 8), textColor: Color.white);
        return (body, null, skippable: true, autoDismiss: 3.5f);
    }

    // ----- Primitives ---------------------------------------------------------

    private static void Fullscreen(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void MakeBackdrop(GameObject parent, Color c)
    {
        var bgGo = new GameObject("Backdrop", typeof(RectTransform));
        bgGo.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)bgGo.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = c;
    }

    private static GameObject MakeFrame(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
        Vector2? size = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        if (size.HasValue) rt.sizeDelta = size.Value;
        else { rt.offsetMin = rt.offsetMax = Vector2.zero; }
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static TMP_Text MakeText(GameObject parent, string name, float fontSize,
                                     TextAlignmentOptions align, Vector4 padding,
                                     Color? textColor = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding.x, padding.y);
        rt.offsetMax = new Vector2(-padding.z, -padding.w);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = string.Empty;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = textColor ?? Color.white;
        t.enableWordWrapping = true;
        return t;
    }
}
#endif
