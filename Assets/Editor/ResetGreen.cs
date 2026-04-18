#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrismZone.Enemy;

public static class ResetGreen
{
    public static void Execute()
    {
        var green = GameObject.Find("GREEN_Watcher");
        if (green == null) { Debug.LogError("No GREEN"); return; }

        // 1. Teleport back to sane position
        green.transform.position = new Vector3(8f, -3f, 0f);

        // 2. Reconnect target (Player)
        var ai = green.GetComponent<GreenEnemy>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var so = new SerializedObject(ai);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.ApplyModifiedProperties();
        }

        // 3. Rewire waypoints by finding them wherever they are in hierarchy
        Transform wp1 = null, wp2 = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "WP_1") wp1 = go.transform;
            else if (go.name == "WP_2") wp2 = go.transform;
        }
        Debug.Log($"[ResetGreen] Found WP_1={(wp1!=null)} WP_2={(wp2!=null)}");

        var so2 = new SerializedObject(ai);
        var wpProp = so2.FindProperty("waypoints");
        int count = 0;
        if (wp1 != null) count++;
        if (wp2 != null) count++;
        wpProp.arraySize = count;
        int idx = 0;
        if (wp1 != null) wpProp.GetArrayElementAtIndex(idx++).objectReferenceValue = wp1;
        if (wp2 != null) wpProp.GetArrayElementAtIndex(idx++).objectReferenceValue = wp2;
        so2.ApplyModifiedProperties();

        Debug.Log($"[ResetGreen] GREEN@{green.transform.position} target={(player!=null?"Player":"NULL")} waypoints={count}");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
