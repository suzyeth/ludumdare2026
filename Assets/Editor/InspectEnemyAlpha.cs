#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;

public static class InspectEnemyAlpha
{
    public static void Execute()
    {
        var current = FilterManager.Instance ? FilterManager.Instance.Current.ToString() : "(null)";
        Debug.Log($"[Alpha] FilterManager.Current={current}");
        foreach (var name in new[] { "RED_Shrike", "GREEN_Watcher", "BLUE_Weaver" })
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            Debug.Log($"[Alpha] {name}: sprite={sr?.sprite?.name} color={sr?.color}");
        }
    }
}
#endif
