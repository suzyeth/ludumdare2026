#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;
using PrismZone.UI;

/// <summary>
/// 1. Creates ItemData assets for diary / recorder / love_letter / glasses.
/// 2. Adds ItemDetailPanel child to HUD_Canvas.
/// 3. Re-saves HUD_Canvas prefab.
/// </summary>
public static class SetupV04Content
{
    public static void Execute()
    {
        CreateItemData("item.glasses",     "Item_Glasses",    "Items/item_glasses.png", hasDetail:false);
        CreateItemData("item.diary",       "Item_Diary",      "Items/item_note_red.png",   hasDetail:true,  pageKeys: new[] { "item.diary.page.1", "item.diary.page.2" });
        CreateItemData("item.recorder",    "Item_Recorder",   "Items/item_note_blue.png",  hasDetail:true,  pageKeys: new[] { "item.recorder.page.1" });
        CreateItemData("item.love_letter", "Item_LoveLetter", "Items/item_note_green.png", hasDetail:true,  pageKeys: new[] { "item.love_letter.page.1" });
        CreateItemData("item.key.a",       "Item_KeyA",       "Items/item_glasses.png",    hasDetail:false); // reuse sprite until art
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Add ItemDetailPanel to SampleScene's Canvas
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.transform.Find("ItemDetailPanel") == null)
        {
            BuildItemDetailPanel(canvas);
            Debug.Log("[V04Content] Added ItemDetailPanel");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Rebuild HUD_Canvas prefab with new child
        RebuildHUDPrefab.Execute();
    }

    static void CreateItemData(string id, string fileName, string iconRelPath, bool hasDetail, string[] pageKeys = null)
    {
        string path = $"Assets/Resources/Items/{fileName}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }
        var so = new SerializedObject(item);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("nameKey").stringValue = id;
        so.FindProperty("worldIcon").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + iconRelPath);
        so.FindProperty("bigIcon").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + iconRelPath);
        so.FindProperty("hasDetailPopup").boolValue = hasDetail;
        var arr = so.FindProperty("pageKeys");
        if (pageKeys != null)
        {
            arr.arraySize = pageKeys.Length;
            for (int i = 0; i < pageKeys.Length; i++)
                arr.GetArrayElementAtIndex(i).stringValue = pageKeys[i];
        }
        else arr.arraySize = 0;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);
        Debug.Log($"[V04Content] ItemData: {fileName} ({id})");
    }

    public static void BuildItemDetailPanelPublic(GameObject canvas) => BuildItemDetailPanel(canvas);

    static void BuildItemDetailPanel(GameObject canvas)
    {
        var go = new GameObject("ItemDetailPanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);

        // Big image in upper center
        var imgGo = new GameObject("BigImage", typeof(RectTransform));
        imgGo.transform.SetParent(go.transform, false);
        var imgRt = (RectTransform)imgGo.transform;
        imgRt.anchorMin = imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = new Vector2(0, 60);
        imgRt.sizeDelta = new Vector2(128, 128);
        var img = imgGo.AddComponent<Image>();
        img.preserveAspect = true;

        // Name label
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = (RectTransform)nameGo.transform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0.5f);
        nameRt.anchoredPosition = new Vector2(0, -10);
        nameRt.sizeDelta = new Vector2(400, 30);
        var nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize = 20;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;

        // Body
        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(go.transform, false);
        var bodyRt = (RectTransform)bodyGo.transform;
        bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.anchoredPosition = new Vector2(0, -60);
        bodyRt.sizeDelta = new Vector2(400, 80);
        var body = bodyGo.AddComponent<TextMeshProUGUI>();
        body.fontSize = 14;
        body.alignment = TextAlignmentOptions.Top;
        body.color = new Color(0.9f, 0.9f, 0.9f);

        // Page count
        var pageGo = new GameObject("PageCount", typeof(RectTransform));
        pageGo.transform.SetParent(go.transform, false);
        var pageRt = (RectTransform)pageGo.transform;
        pageRt.anchorMin = pageRt.anchorMax = new Vector2(0.5f, 0.5f);
        pageRt.anchoredPosition = new Vector2(0, -115);
        pageRt.sizeDelta = new Vector2(80, 20);
        var pageLabel = pageGo.AddComponent<TextMeshProUGUI>();
        pageLabel.fontSize = 12;
        pageLabel.alignment = TextAlignmentOptions.Center;
        pageLabel.color = new Color(0.6f, 0.6f, 0.6f);

        // Hint
        var hintGo = new GameObject("Hint", typeof(RectTransform));
        hintGo.transform.SetParent(go.transform, false);
        var hintRt = (RectTransform)hintGo.transform;
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0);
        hintRt.pivot = new Vector2(0.5f, 0);
        hintRt.anchoredPosition = new Vector2(0, 16);
        hintRt.sizeDelta = new Vector2(400, 20);
        var hint = hintGo.AddComponent<TextMeshProUGUI>();
        hint.fontSize = 10;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.7f, 0.7f, 0.7f);
        hint.text = "[F/Esc] close   [←/→] page";

        var panel = go.AddComponent<ItemDetailPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("bigImage").objectReferenceValue = img;
        so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
        so.FindProperty("bodyLabel").objectReferenceValue = body;
        so.FindProperty("pageCountLabel").objectReferenceValue = pageLabel;
        so.ApplyModifiedProperties();
    }
}
#endif
