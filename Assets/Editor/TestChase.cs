#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Enemy;

public static class TestChase
{
    public static void Execute()
    {
        var green = GameObject.Find("GREEN_Watcher");
        var player = GameObject.Find("Player");
        if (green == null || player == null) return;

        // Teleport player near green to force detection
        player.transform.position = new Vector3(5f, -2f, 0);
        Debug.Log("[TestChase] Player teleported to (5, -2, 0). GREEN at " + green.transform.position);

        // Wait a frame then log state (via coroutine-ish approach: just log next)
        var ai = green.GetComponent<GreenEnemy>();
        Debug.Log($"[TestChase] GREEN state before: {ai.Current}");
    }

    public static void Poll()
    {
        var green = GameObject.Find("GREEN_Watcher");
        if (green == null) return;
        var ai = green.GetComponent<GreenEnemy>();
        var path = (System.Collections.Generic.List<Vector3Int>)typeof(GreenEnemy).GetField("_path", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ai);
        Debug.Log($"[TestChase] GREEN state={ai.Current} pos={green.transform.position} pathLen={path?.Count}");
    }
}
#endif
