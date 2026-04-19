#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;
using PrismZone.Core;

public static class AuditUI
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== UI AUDIT ===");

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[AuditUI] No Canvas"); return; }

        var c = canvas.GetComponent<Canvas>();
        var cs = canvas.GetComponent<CanvasScaler>();
        sb.AppendLine($"Canvas: renderMode={c.renderMode} scaleFactor={c.scaleFactor} scaler.ref={cs.referenceResolution} match={cs.matchWidthOrHeight}");

        foreach (var name in new[] { "FilterHUD", "InventoryHUD", "InteractPrompt", "CluePopup", "PasscodePanel" })
        {
            var t = canvas.transform.Find(name);
            if (t == null) { sb.AppendLine($"-- {name}: MISSING"); continue; }
            var go = t.gameObject;
            var cg = go.GetComponent<CanvasGroup>();
            var img = go.GetComponent<Image>();
            var rt = (RectTransform)t;
            sb.AppendLine($"-- {name}: active={go.activeInHierarchy} anchored={rt.anchoredPosition} size={rt.sizeDelta}");
            sb.AppendLine($"   CanvasGroup={(cg!=null?$"alpha={cg.alpha}":"none")} BGimg={(img!=null?$"color={img.color}":"none")}");

            // Components + their field wiring
            var monos = go.GetComponents<MonoBehaviour>();
            foreach (var m in monos)
            {
                if (m == null || m is CanvasRenderer) continue;
                sb.AppendLine($"   script={m.GetType().Name} enabled={m.enabled}");
            }

            // Labels in this subtree
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                sb.AppendLine($"   TMP[{tmp.name}]: text='{Truncate(tmp.text, 40)}' fontSize={tmp.fontSize} color={tmp.color} active={tmp.gameObject.activeInHierarchy}");
            }
        }

        sb.AppendLine("=== i18n ===");
        sb.AppendLine($"lang={I18nManager.CurrentLang}");
        foreach (var k in new[] { "ui.filter.none", "ui.pickup.prompt", "ui.cabinet.prompt", "clue.room1.code", "ui.passcode.title", "item.glasses" })
            sb.AppendLine($"  '{k}' -> '{I18nManager.Get(k)}'");

        Debug.Log(sb.ToString());
    }

    static string Truncate(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
#endif
