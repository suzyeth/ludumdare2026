#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

public static class ApplyV04Changes
{
    public static void Execute()
    {
        // 1. Transparency Sort Mode Y-axis (3/4 view Y-sort)
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);
        Debug.Log("[V04] GraphicsSettings transparencySortMode = CustomAxis (0,1,0)");

        // 2. Open SampleScene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        // 3. Player collider 1 x 1.5
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var col = player.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(1f, 1.5f);
            Debug.Log("[V04] Player BoxCollider2D size -> 1 x 1.5");
        }

        // 4. GREEN collider 1 x 1.5
        var green = GameObject.Find("GREEN_Watcher");
        if (green != null)
        {
            var col = green.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(1f, 1.5f);
            Debug.Log("[V04] GREEN BoxCollider2D size -> 1 x 1.5");
        }

        // 5. Strip glasses from Inventory startingItems (v0.4 = world pickup)
        var boot = GameObject.Find("_Bootstrap");
        if (boot != null)
        {
            var inv = boot.GetComponent<PrismZone.UI.Inventory>();
            if (inv != null)
            {
                var so = new SerializedObject(inv);
                var prop = so.FindProperty("startingItems");
                prop.arraySize = 0;
                so.ApplyModifiedProperties();
                Debug.Log("[V04] Inventory.startingItems cleared (glasses now world pickup)");
            }
        }

        // 6. Add GameOverPanel to Canvas
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.transform.Find("GameOverPanel") == null)
        {
            BuildGameOverPanel(canvas);
            Debug.Log("[V04] Added GameOverPanel to Canvas");
        }

        // 7. Create a Pickup_Glasses in scene if missing (so player can grab glasses)
        var interRoot = GameObject.Find("_Interactables");
        if (interRoot != null && GameObject.Find("Pickup_Glasses") == null)
        {
            SpawnGlassesPickup(interRoot.transform);
            Debug.Log("[V04] Spawned Pickup_Glasses");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[V04] Done");
    }

    static void BuildGameOverPanel(GameObject canvas)
    {
        var go = new GameObject("GameOverPanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)titleGo.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0, 40);
        trt.sizeDelta = new Vector2(400, 60);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 36;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;

        var reasonGo = new GameObject("Reason", typeof(RectTransform));
        reasonGo.transform.SetParent(go.transform, false);
        var rrt = (RectTransform)reasonGo.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.anchoredPosition = new Vector2(0, 0);
        rrt.sizeDelta = new Vector2(400, 30);
        var reason = reasonGo.AddComponent<TextMeshProUGUI>();
        reason.text = "";
        reason.fontSize = 14;
        reason.color = new Color(0.85f, 0.85f, 0.85f);
        reason.alignment = TextAlignmentOptions.Center;

        var btnGo = new GameObject("Btn_Restart", typeof(RectTransform));
        btnGo.transform.SetParent(go.transform, false);
        var brt = (RectTransform)btnGo.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0, -50);
        brt.sizeDelta = new Vector2(160, 40);
        var bImg = btnGo.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = bImg;

        var bLabelGo = new GameObject("Label", typeof(RectTransform));
        bLabelGo.transform.SetParent(btnGo.transform, false);
        var bLrt = (RectTransform)bLabelGo.transform;
        bLrt.anchorMin = Vector2.zero; bLrt.anchorMax = Vector2.one;
        bLrt.offsetMin = Vector2.zero; bLrt.offsetMax = Vector2.zero;
        var bLabel = bLabelGo.AddComponent<TextMeshProUGUI>();
        bLabel.text = "Restart";
        bLabel.fontSize = 16;
        bLabel.color = Color.white;
        bLabel.alignment = TextAlignmentOptions.Center;

        var panel = go.AddComponent<GameOverPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("reasonLabel").objectReferenceValue = reason;
        so.FindProperty("restartButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();
    }

    static void SpawnGlassesPickup(Transform parent)
    {
        var go = new GameObject("Pickup_Glasses");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(-3f, 2f, 0f);
        go.tag = "Interactable";

        var spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(go.transform, false);
        spriteGo.transform.localPosition = Vector3.zero;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Items/item_glasses.png");
        sr.sortingLayerName = "Mid";
        sr.sortingOrder = 3;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        var pk = go.AddComponent<PrismZone.Interact.Pickup>();
        var so = new SerializedObject(pk);
        so.FindProperty("itemId").stringValue = "item.glasses";
        so.FindProperty("clueTextKey").stringValue = "dialog.intro";
        so.FindProperty("promptKey").stringValue = "ui.pickup.prompt";
        so.FindProperty("destroyOnPickup").boolValue = true;
        so.FindProperty("oneShotClue").boolValue = true;
        so.ApplyModifiedProperties();
    }
}
#endif
