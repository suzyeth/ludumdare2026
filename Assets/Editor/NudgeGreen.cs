#if UNITY_EDITOR
using UnityEngine;

public static class NudgeGreen
{
    public static void Execute()
    {
        var green = GameObject.Find("GREEN_Watcher");
        if (green != null) green.transform.position = new Vector3(8, -3, 0);
        var wp1 = GameObject.Find("WP_1");
        var wp2 = GameObject.Find("WP_2");
        if (wp1 != null) wp1.transform.position = new Vector3(6, -3, 0);
        if (wp2 != null) wp2.transform.position = new Vector3(9, -3, 0);

        // Player a bit further left too, away from GREEN's vision cone.
        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(-6, 0, 0);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Nudge] Moved GREEN and waypoints to the far-right corner.");
    }
}
#endif
