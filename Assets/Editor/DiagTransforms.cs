#if UNITY_EDITOR
using UnityEngine;

public static class DiagTransforms
{
    public static void Execute()
    {
        foreach (var name in new[] { "_Enemies", "GREEN_Watcher", "Waypoints", "WP_1", "WP_2", "_Interactables", "Pickup_CluePaper" })
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name != name) continue;
                var t = go.transform;
                Debug.Log($"[T] {name}: worldPos={t.position} localPos={t.localPosition} parent={(t.parent ? t.parent.name : "(root)")}");
            }
        }
    }
}
#endif
