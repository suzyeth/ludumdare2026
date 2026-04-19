#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;
using PrismZone.UI;
using PrismZone.Player;

public static class TestV04
{
    public static void FilterGateTest()
    {
        var fm = FilterManager.Instance;
        var inv = Inventory.Instance;
        Debug.Log($"[TestV04] Start: filter={fm?.Current} inv.hasGlasses={(inv != null && inv.Has("item.glasses"))}");

        // Try filter without glasses (should fail quietly — manager still accepts, but the
        // hotkey handler in PlayerController blocks; we simulate that check here).
        bool before = inv != null && inv.Has("item.glasses");
        fm.SetFilter(FilterColor.Red);
        Debug.Log($"[TestV04] SetFilter(Red) with glasses={before}: actual filter={fm.Current}");

        // Force-add glasses
        inv.TryAdd("item.glasses");
        fm.SetFilter(FilterColor.None);
        fm.SetFilter(FilterColor.Green);
        Debug.Log($"[TestV04] After glasses add + SetFilter(Green): filter={fm.Current}");
    }

    public static void TriggerGameOver()
    {
        GameOverController.TriggerGameOver("caught by GREEN");
        Debug.Log($"[TestV04] IsGameOver={GameOverController.IsGameOver} Time.timeScale={Time.timeScale}");
    }
}
#endif
