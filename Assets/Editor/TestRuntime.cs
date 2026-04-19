#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;
using PrismZone.Player;
using PrismZone.Enemy;

public static class TestRuntime
{
    public static void SetFilterRed()     { Set(FilterColor.Red); }
    public static void SetFilterGreen()   { Set(FilterColor.Green); }
    public static void SetFilterBlue()    { Set(FilterColor.Blue); }
    public static void SetFilterNone()    { Set(FilterColor.None); }

    private static void Set(FilterColor c)
    {
        if (FilterManager.Instance == null) { Debug.LogWarning("No FilterManager (not playing?)"); return; }
        FilterManager.Instance.SetFilter(c);
        Debug.Log($"[TestRuntime] Filter -> {c}");
    }

    public static void Report()
    {
        var fm = FilterManager.Instance;
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player ? player.GetComponent<PlayerController>() : null;
        Debug.Log($"[Report] FilterMgr={(fm!=null)} current={fm?.Current} | Player={(player!=null)} pos={player?.transform.position} hidden={pc?.IsHidden} running={pc?.IsRunning}");
        foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            Debug.Log($"[Report] {e.name} state={e.Current} revealFilter={e.RevealFilter}");
    }
}
#endif
