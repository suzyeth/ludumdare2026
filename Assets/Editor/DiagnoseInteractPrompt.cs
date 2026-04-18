#if UNITY_EDITOR
using UnityEngine;
using TMPro;
using PrismZone.UI;

public static class DiagnoseInteractPrompt
{
    public static void Execute()
    {
        var go = GameObject.Find("Canvas/InteractPrompt");
        Debug.Log($"[DiagIP] GO found: {(go!=null)} active={go?.activeSelf}");
        if (go == null) return;

        var prompt = go.GetComponent<InteractPrompt>();
        Debug.Log($"[DiagIP] InteractPrompt component: {(prompt!=null)}");

        var label = go.GetComponentInChildren<TMP_Text>(true);
        Debug.Log($"[DiagIP] Label text='{label?.text}' enabled={label?.enabled}");

        var rt = go.GetComponent<RectTransform>();
        Debug.Log($"[DiagIP] rect anchored={rt.anchoredPosition} size={rt.sizeDelta} world={rt.position}");
    }
}
#endif
