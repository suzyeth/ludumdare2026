#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

public static class SetupPauseAndRings
{
    [MenuItem("Tools/Prism Zone/Setup Pause + Ring HUD")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas"); return; }

        // --- 1. FilterHUD: swap from 4 dots to 3 rings
        UpgradeFilterHUD(canvas);

        // --- 2. PauseMenu: new child
        if (canvas.transform.Find("PauseMenu") == null)
        {
            BuildPauseMenu(canvas);
            Debug.Log("[Pause] Added PauseMenu");
        }
        else Debug.Log("[Pause] PauseMenu already present");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Rebuild HUD_Canvas prefab with new children
        RebuildHUDPrefab.Execute();
    }

    static Sprite SP(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static void UpgradeFilterHUD(GameObject canvas)
    {
        var t = canvas.transform.Find("FilterHUD");
        if (t == null) { Debug.LogWarning("No FilterHUD"); return; }
        var hud = t.GetComponent<FilterHUD>();
        if (hud == null) return;

        Sprite redOn   = SP("Assets/Art/UI/ui_filter_ring_red_on.png");
        Sprite redOff  = SP("Assets/Art/UI/ui_filter_ring_red_off.png");
        Sprite grnOn   = SP("Assets/Art/UI/ui_filter_ring_green_on.png");
        Sprite grnOff  = SP("Assets/Art/UI/ui_filter_ring_green_off.png");
        Sprite bluOn   = SP("Assets/Art/UI/ui_filter_ring_blue_on.png");
        Sprite bluOff  = SP("Assets/Art/UI/ui_filter_ring_blue_off.png");

        // Prefer existing RedDot / GreenDot / BlueDot Images — treat them as ring holders
        var redImg   = t.Find("RedDot")?.GetComponent<Image>();
        var greenImg = t.Find("GreenDot")?.GetComponent<Image>();
        var blueImg  = t.Find("BlueDot")?.GetComponent<Image>();

        // Resize ring slots to 24×24 (original dot size) — tweak if rings look too small
        foreach (var img in new[] { redImg, greenImg, blueImg })
            if (img != null)
            {
                img.color = Color.white;
                var rt = (RectTransform)img.transform;
                rt.sizeDelta = new Vector2(24, 24);
            }

        // Hide legacy NoneDot (rings fully express 'none' = all three _off)
        var noneDot = t.Find("NoneDot")?.gameObject;
        if (noneDot != null) noneDot.SetActive(false);

        var so = new SerializedObject(hud);
        so.FindProperty("redRing").objectReferenceValue = redImg;
        so.FindProperty("greenRing").objectReferenceValue = greenImg;
        so.FindProperty("blueRing").objectReferenceValue = blueImg;
        so.FindProperty("redOn").objectReferenceValue = redOn;
        so.FindProperty("redOff").objectReferenceValue = redOff;
        so.FindProperty("greenOn").objectReferenceValue = grnOn;
        so.FindProperty("greenOff").objectReferenceValue = grnOff;
        so.FindProperty("blueOn").objectReferenceValue = bluOn;
        so.FindProperty("blueOff").objectReferenceValue = bluOff;
        so.ApplyModifiedProperties();
        Debug.Log("[Pause] FilterHUD upgraded to ring mode");
    }

    static void BuildPauseMenu(GameObject canvas)
    {
        var go = new GameObject("PauseMenu", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var title = MakeText(go, "Title", new Vector2(0, 60), new Vector2(400, 40), "PAUSED", 30);
        var resume = MakeButton(go, "Btn_Resume", new Vector2(0, 10), "Resume");
        var menu   = MakeButton(go, "Btn_MainMenu", new Vector2(0, -40), "Main Menu");
        var quit   = MakeButton(go, "Btn_Quit", new Vector2(0, -90), "Quit");

        var pm = go.AddComponent<PauseMenu>();
        var so = new SerializedObject(pm);
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("mainMenuScene").stringValue = "Scene_MainMenu";
        so.ApplyModifiedProperties();
    }

    static TMP_Text MakeText(GameObject parent, string name, Vector2 pos, Vector2 size, string text, float fs)
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
        return t;
    }

    static Button MakeButton(GameObject parent, string name, Vector2 pos, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 40);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
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
