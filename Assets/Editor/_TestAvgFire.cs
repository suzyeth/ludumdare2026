using UnityEngine;
using PrismZone.UI;

public static class _TestAvgFire
{
    public static void Execute()
    {
        var mgr = DialogueManager.Instance;
        if (mgr == null) { Debug.LogError("[AVG TEST] DialogueManager null"); return; }
        mgr.ShowKeys(DialogueType.READ,
            new[] { "ui.popup.next_hint", "ui.popup.page_hint" }, null, null,
            "DEBUG-READ", "ui.popup.close_hint", null);
        Debug.Log("[AVG TEST] READ popup fired");
    }
}
