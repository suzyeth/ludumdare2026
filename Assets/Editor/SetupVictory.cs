#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;
using PrismZone.Interact;

/// <summary>
/// 1. Build VictoryPanel under Canvas (parallel to GameOverPanel).
/// 2. Rebuild HUD_Canvas prefab to include it.
/// 3. Spawn a test ExitTrigger in SampleScene as a stand-in escape point.
/// </summary>
public static class SetupVictory
{
    [MenuItem("Tools/Prism Zone/Setup Victory + ExitTrigger")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas"); return; }

        if (canvas.transform.Find("VictoryPanel") == null)
        {
            BuildVictoryPanel(canvas);
            Debug.Log("[Victory] Added VictoryPanel to Canvas");
        }
        else Debug.Log("[Victory] VictoryPanel already present");

        // Spawn an ExitTrigger the player can walk into (top-right corner of room).
        var interRoot = GameObject.Find("_Interactables");
        if (interRoot != null && GameObject.Find("ExitTrigger_Escape") == null)
        {
            BuildExitTrigger(interRoot.transform);
            Debug.Log("[Victory] Spawned ExitTrigger_Escape at (9, 3, 0)");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        RebuildHUDPrefab.Execute();
    }

    static void BuildVictoryPanel(GameObject canvas)
    {
        var go = new GameObject("VictoryPanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.08f, 0.9f);

        var title = MakeText(go, "Title", new Vector2(0, 60), new Vector2(400, 60), "ESCAPED", 36, Color.white);
        title.alignment = TextAlignmentOptions.Center;

        var reason = MakeText(go, "Reason", new Vector2(0, 10), new Vector2(420, 30), "", 14, new Color(0.85f, 0.9f, 1f));
        reason.alignment = TextAlignmentOptions.Center;

        var restart = MakeButton(go, "Btn_Restart", new Vector2(-75, -50), new Vector2(140, 40), "Play Again");
        var menu    = MakeButton(go, "Btn_MainMenu", new Vector2(75, -50), new Vector2(140, 40), "Main Menu");

        go.AddComponent<CanvasGroup>();

        var panel = go.AddComponent<VictoryPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("reasonLabel").objectReferenceValue = reason;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.ApplyModifiedProperties();
    }

    static void BuildExitTrigger(Transform parent)
    {
        var go = new GameObject("ExitTrigger_Escape");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(9, 3, 0);
        go.tag = "Interactable";

        var spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(go.transform, false);
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Props/prop_stairs_front.png");
        if (sr.sprite == null)
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Props/prop_cabinet_open.png");
        sr.sortingLayerName = "Mid";
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1, 2);
        col.isTrigger = true;

        var et = go.AddComponent<ExitTrigger>();
        // Leave targetScene blank → this is the final victory trigger.
    }

    static TMP_Text MakeText(GameObject parent, string name, Vector2 pos, Vector2 size, string text, float fs, Color color)
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
        t.color = color;
        return t;
    }

    static Button MakeButton(GameObject parent, string name, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
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
