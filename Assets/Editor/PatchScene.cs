#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

public static class PatchScene
{
    public static void Execute()
    {
        // 1. Swap Pickup sprite to bright cream.
        var pk = GameObject.Find("Pickup_CluePaper");
        if (pk != null)
        {
            var sr = pk.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/S_Player.png");
                sr.sortingOrder = 3;
                sr.color = Color.white;
                Debug.Log("[Patch] Pickup sprite -> S_Player (cream)");
            }
        }

        // 2. Add close button to PasscodePanel if missing.
        var canvas = GameObject.Find("Canvas");
        var panelT = canvas ? canvas.transform.Find("PasscodePanel") : null;
        if (panelT != null)
        {
            var existingClose = panelT.Find("Btn_Close");
            if (existingClose == null)
            {
                var btnGo = new GameObject("Btn_Close", typeof(RectTransform));
                btnGo.transform.SetParent(panelT, false);
                var rt = (RectTransform)btnGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(100, 115);
                rt.sizeDelta = new Vector2(28, 28);
                var img = btnGo.AddComponent<Image>();
                img.color = new Color(0.6f, 0.15f, 0.15f, 0.95f);
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(btnGo.transform, false);
                var lrt = (RectTransform)labelGo.transform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 14;
                tmp.text = "X";
                tmp.color = Color.white;

                var panel = panelT.GetComponent<PasscodePanel>();
                if (panel != null)
                {
                    var so = new SerializedObject(panel);
                    so.FindProperty("closeButton").objectReferenceValue = btn;
                    so.ApplyModifiedProperties();
                }
                Debug.Log("[Patch] Added PasscodePanel Close button");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
