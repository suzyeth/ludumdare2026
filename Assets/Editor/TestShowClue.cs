#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismZone.UI;

public static class TestShowClue
{
    public static void Execute()
    {
        if (CluePopup.Instance == null) { Debug.LogWarning("No CluePopup.Instance"); return; }
        CluePopup.Instance.Show("clue.room1.code");
        Debug.Log($"[TestShowClue] Show called. IsOpen={CluePopup.Instance.IsOpen}");

        // Inspect internal state
        var go = GameObject.Find("Canvas");
        var cp = go ? go.transform.Find("CluePopup") : null;
        if (cp == null) { Debug.LogWarning("No CluePopup GO"); return; }
        var cg = cp.GetComponent<CanvasGroup>();
        Debug.Log($"[TestShowClue] CluePopup active={cp.gameObject.activeInHierarchy} CG alpha={cg?.alpha}");

        var body = cp.Find("Body")?.GetComponent<TMP_Text>();
        Debug.Log($"[TestShowClue] Body text='{body?.text}' enabled={body?.enabled}");

        var bgImg = cp.GetComponent<Image>();
        Debug.Log($"[TestShowClue] BG color={bgImg?.color} alpha={bgImg?.color.a}");
    }
}
#endif
