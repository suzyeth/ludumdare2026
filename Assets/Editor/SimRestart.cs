#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;

public static class SimRestart
{
    public static void Execute()
    {
        Debug.Log($"[SimRestart] before IsGameOver={GameOverController.IsGameOver} timeScale={Time.timeScale}");
        GameOverController.Restart();
        // Wait one frame then log
    }
}
#endif
