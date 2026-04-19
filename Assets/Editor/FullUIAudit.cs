#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;
using PrismZone.UI;
using PrismZone.Player;
using PrismZone.Interact;

public static class FullUIAudit
{
    private static StringBuilder sb;
    private static int pass, fail;

    public static void Execute()
    {
        sb = new StringBuilder();
        pass = 0; fail = 0;
        sb.AppendLine("=========== FULL UI FUNCTIONAL AUDIT ===========");

        Section1_CanvasRoot();
        Section2_FilterHUD();
        Section3_InventoryHUD();
        Section4_InteractPrompt();
        Section5_CluePopup();
        Section6_PasscodePanel();
        Section7_GameOverPanel();
        Section8_ItemDetailPanel();
        Section9_MainMenuScene();

        sb.AppendLine($"\n=========== TOTAL: {pass} pass / {fail} fail ===========");
        Debug.Log(sb.ToString());
    }

    static void Check(string desc, bool ok)
    {
        if (ok) { sb.AppendLine($"  [OK]  {desc}"); pass++; }
        else    { sb.AppendLine($"  [XX]  {desc}"); fail++; }
    }

    // ------------------------------------------------------------
    static void Section1_CanvasRoot()
    {
        sb.AppendLine("\n-- [1] Canvas root");
        var canvas = GameObject.Find("Canvas");
        Check("Canvas in scene", canvas != null);
        if (canvas == null) return;
        var c = canvas.GetComponent<Canvas>();
        Check("renderMode == ScreenSpaceOverlay", c != null && c.renderMode == RenderMode.ScreenSpaceOverlay);
        var cs = canvas.GetComponent<CanvasScaler>();
        Check("scaler ref 640x360", cs != null && cs.referenceResolution == new Vector2(640, 360));
        Check("GraphicRaycaster present", canvas.GetComponent<GraphicRaycaster>() != null);

        var es = GameObject.Find("EventSystem");
        Check("EventSystem exists", es != null);
        if (es != null)
            Check("EventSystem has InputSystemUIInputModule",
                es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null);
    }

    // ------------------------------------------------------------
    static void Section2_FilterHUD()
    {
        sb.AppendLine("\n-- [2] FilterHUD");
        var t = GameObject.Find("Canvas/FilterHUD");
        Check("present", t != null);
        if (t == null) return;
        var hud = t.GetComponent<FilterHUD>();
        Check("FilterHUD script attached", hud != null);
        var label = t.transform.Find("Label")?.GetComponent<TMP_Text>();
        Check("Label child present", label != null);

        // Runtime behaviour only if in play mode
        if (Application.isPlaying && FilterManager.Instance != null)
        {
            FilterManager.Instance.SetFilter(FilterColor.Red);
            Check("Label -> 'Red' after SetFilter(Red)",
                label != null && label.text == I18nManager.Get("ui.filter.red"));
            FilterManager.Instance.SetFilter(FilterColor.None);
            Check("Label -> 'No Filter' after SetFilter(None)",
                label != null && label.text == I18nManager.Get("ui.filter.none"));
        }
        else sb.AppendLine("  (skip runtime checks — not playing)");
    }

    // ------------------------------------------------------------
    static void Section3_InventoryHUD()
    {
        sb.AppendLine("\n-- [3] InventoryHUD");
        var t = GameObject.Find("Canvas/InventoryHUD");
        Check("present", t != null);
        if (t == null) return;
        Check("4 slots as expected", t.transform.childCount == 4);
        Check("InventoryHUD script attached", t.GetComponent<InventoryHUD>() != null);

        if (Application.isPlaying && Inventory.Instance != null)
        {
            int beforeCount = Inventory.Instance.Slots.Count;
            bool added = Inventory.Instance.TryAdd("item.diary");
            Check("TryAdd returns true", added);
            Check("Slots count +1 after add", Inventory.Instance.Slots.Count == beforeCount + 1);
            Inventory.Instance.Remove("item.diary");
            Check("Slots count back to original after remove",
                Inventory.Instance.Slots.Count == beforeCount);
        }
    }

    // ------------------------------------------------------------
    static void Section4_InteractPrompt()
    {
        sb.AppendLine("\n-- [4] InteractPrompt");
        var t = GameObject.Find("Canvas/InteractPrompt");
        Check("present", t != null);
        if (t == null) return;
        Check("has CanvasGroup", t.GetComponent<CanvasGroup>() != null);
        Check("script attached", t.GetComponent<InteractPrompt>() != null);
        var label = t.GetComponentInChildren<TMP_Text>();
        Check("Label present", label != null);

        if (Application.isPlaying)
        {
            var cg = t.GetComponent<CanvasGroup>();
            var player = GameObject.FindGameObjectWithTag("Player");
            var pi = player ? player.GetComponent<PlayerInteraction>() : null;
            bool hasTarget = pi != null && pi.CurrentTarget != null;
            Check($"runtime: alpha ({cg.alpha}) matches hasTarget ({hasTarget})",
                (cg.alpha > 0.5f) == hasTarget);
        }
    }

    // ------------------------------------------------------------
    static void Section5_CluePopup()
    {
        sb.AppendLine("\n-- [5] CluePopup");
        var t = GameObject.Find("Canvas/CluePopup");
        Check("present", t != null);
        if (t == null) return;
        Check("has CanvasGroup", t.GetComponent<CanvasGroup>() != null);
        Check("script attached", t.GetComponent<CluePopup>() != null);

        if (Application.isPlaying && CluePopup.Instance != null)
        {
            CluePopup.Instance.Show("clue.room1.code");
            var cg = t.GetComponent<CanvasGroup>();
            Check("Show sets alpha=1", cg.alpha > 0.5f);
            Check("IsOpen true", CluePopup.Instance.IsOpen);
            CluePopup.Instance.Close();
            Check("Close sets alpha=0", cg.alpha < 0.5f);
        }
    }

    // ------------------------------------------------------------
    static void Section6_PasscodePanel()
    {
        sb.AppendLine("\n-- [6] PasscodePanel");
        var t = GameObject.Find("Canvas/PasscodePanel");
        Check("present", t != null);
        if (t == null) return;
        Check("has CanvasGroup", t.GetComponent<CanvasGroup>() != null);
        var panel = t.GetComponent<PasscodePanel>();
        Check("script attached", panel != null);
        Check("Btn_Close present", t.transform.Find("Btn_Close") != null);
        Check("Btn_Submit present", t.transform.Find("Btn_Submit") != null);
        // Count digit buttons
        int digitBtns = 0;
        for (int i = 0; i < t.transform.childCount; i++)
            if (t.transform.GetChild(i).name.StartsWith("Btn_") &&
                char.IsDigit(t.transform.GetChild(i).name[4])) digitBtns++;
        Check("10 digit buttons (0-9)", digitBtns == 10);
    }

    // ------------------------------------------------------------
    static void Section7_GameOverPanel()
    {
        sb.AppendLine("\n-- [7] GameOverPanel");
        var t = GameObject.Find("Canvas/GameOverPanel");
        Check("present", t != null);
        if (t == null) return;
        Check("has CanvasGroup", t.GetComponent<CanvasGroup>() != null);
        var panel = t.GetComponent<GameOverPanel>();
        Check("script attached", panel != null);
        Check("Btn_Restart child present", t.transform.Find("Btn_Restart") != null);

        if (Application.isPlaying)
        {
            var cg = t.GetComponent<CanvasGroup>();
            GameOverController.TriggerGameOver("caught by GREEN");
            Check("TriggerGameOver sets alpha=1", cg.alpha > 0.5f);
            Check("IsGameOver=true", GameOverController.IsGameOver);
            Check("Time.timeScale==0 (paused)", Time.timeScale == 0f);
            // Simulate OnReset without real scene reload
            var m = typeof(GameOverController).GetField("OnReset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            // Use the Restart path but swallow the LoadScene:
            // Just call OnReset manually via reflection to test panel reset.
            GameOverControllerEmitReset();
            Check("OnReset hides panel (alpha<0.5)", cg.alpha < 0.5f);
            // Reset runtime state
            Time.timeScale = 1f;
            var f = typeof(GameOverController).GetProperty("IsGameOver",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            f.GetSetMethod(true)?.Invoke(null, new object[] { false });
        }
    }

    static void GameOverControllerEmitReset()
    {
        // Use reflection to invoke OnReset without triggering LoadScene
        var t = typeof(GameOverController);
        var eventField = t.GetField("OnReset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var multicast = eventField?.GetValue(null) as System.Delegate;
        multicast?.DynamicInvoke();
    }

    // ------------------------------------------------------------
    static void Section8_ItemDetailPanel()
    {
        sb.AppendLine("\n-- [8] ItemDetailPanel");
        var t = GameObject.Find("Canvas/ItemDetailPanel");
        Check("present", t != null);
        if (t == null) return;
        Check("has CanvasGroup", t.GetComponent<CanvasGroup>() != null);
        Check("script attached", t.GetComponent<ItemDetailPanel>() != null);
        Check("BigImage child present", t.transform.Find("BigImage") != null);
        Check("Body child present", t.transform.Find("Body") != null);

        if (Application.isPlaying && ItemDetailPanel.Instance != null)
        {
            var diary = ItemDatabase.Get("item.diary");
            Check("ItemDatabase loads 'item.diary'", diary != null);
            if (diary != null)
            {
                ItemDetailPanel.Instance.Show(diary);
                var cg = t.GetComponent<CanvasGroup>();
                Check("Show(diary) alpha=1", cg.alpha > 0.5f);
                Check("diary.PageCount >= 1", diary.PageCount >= 1);
                ItemDetailPanel.Instance.Close();
                Check("Close alpha=0", cg.alpha < 0.5f);
            }
        }
    }

    // ------------------------------------------------------------
    static void Section9_MainMenuScene()
    {
        sb.AppendLine("\n-- [9] MainMenu scene");
        bool exists = System.IO.File.Exists("Assets/Scenes/Scene_MainMenu.unity");
        Check("Scene_MainMenu.unity exists", exists);
        int idx = -1;
        var scenes = UnityEditor.EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
            if (scenes[i].path == "Assets/Scenes/Scene_MainMenu.unity") { idx = i; break; }
        Check("in Build Settings", idx >= 0);
        Check("is first in Build Settings", idx == 0);
    }
}
#endif
