#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;
using PrismZone.Player;

public static class DiagIP2
{
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        var ipT = canvas ? canvas.transform.Find("InteractPrompt") : null;
        var go = ipT ? ipT.gameObject : null;
        Debug.Log($"[IP2] InteractPrompt GO: found={go!=null} active={go?.activeSelf} activeInHierarchy={go?.activeInHierarchy}");
        if (go == null) return;

        var cg = go.GetComponent<CanvasGroup>();
        Debug.Log($"[IP2] CanvasGroup: {(cg!=null)} alpha={cg?.alpha}");

        var prompt = go.GetComponent<InteractPrompt>();
        Debug.Log($"[IP2] InteractPrompt enabled={prompt?.enabled}");

        var label = go.GetComponentInChildren<TMP_Text>(true);
        Debug.Log($"[IP2] Label GO active={label?.gameObject.activeInHierarchy} text='{label?.text}' fontAsset={label?.font?.name}");

        var rt = (RectTransform)go.transform;
        Debug.Log($"[IP2] RectTransform anchored={rt.anchoredPosition} size={rt.sizeDelta} world={rt.position}");

        var player = GameObject.FindGameObjectWithTag("Player");
        var pi = player ? player.GetComponent<PlayerInteraction>() : null;
        Debug.Log($"[IP2] PI.CurrentTarget={(pi?.CurrentTarget==null?"null":pi.CurrentTarget.GetType().Name)}");
    }
}
#endif
