#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;

public static class TestVictory
{
    public static void Execute()
    {
        VictoryController.TriggerVictory("escaped");
        Debug.Log($"[TestVictory] IsVictory={VictoryController.IsVictory} timeScale={Time.timeScale}");
    }

    public static void Reset()
    {
        VictoryController.Restart(); // reloads scene — careful in Editor
    }
}
#endif
