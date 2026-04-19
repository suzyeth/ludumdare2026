#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NestWaypoints
{
    public static void Execute()
    {
        var greens = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject green = null;
        foreach (var g in greens)
            if (g.name == "GREEN_Watcher") { green = g; break; }
        if (green == null) { Debug.LogError("[Nest] No GREEN_Watcher"); return; }

        Transform waypointsT = green.transform.Find("Waypoints");
        if (waypointsT == null)
        {
            var wpParent = new GameObject("Waypoints");
            wpParent.transform.SetParent(green.transform, false);
            wpParent.transform.localPosition = Vector3.zero;
            waypointsT = wpParent.transform;
            Debug.Log("[Nest] Created Waypoints child");
        }

        foreach (var name in new[] { "WP_1", "WP_2" })
        {
            GameObject wp = null;
            foreach (var g in greens)
                if (g.name == name) { wp = g; break; }
            if (wp == null) { Debug.Log($"[Nest] {name} not found"); continue; }
            wp.transform.SetParent(waypointsT, worldPositionStays: true);
            Debug.Log($"[Nest] {name} reparented to {waypointsT.name}. worldPos={wp.transform.position}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
