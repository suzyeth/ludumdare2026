#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReorganizeHierarchy
{
    public static void Execute()
    {
        // 1. Move WP_1 / WP_2 under GREEN_Watcher/Waypoints
        var green = GameObject.Find("GREEN_Watcher");
        if (green != null)
        {
            Transform waypointsT = green.transform.Find("Waypoints");
            if (waypointsT == null)
            {
                var wpParent = new GameObject("Waypoints");
                // Keep local at zero so wp world pos is preserved
                wpParent.transform.SetParent(green.transform, worldPositionStays: false);
                waypointsT = wpParent.transform;
            }
            foreach (var name in new[] { "WP_1", "WP_2" })
            {
                var wp = GameObject.Find(name);
                if (wp != null && wp.transform.parent != waypointsT)
                {
                    wp.transform.SetParent(waypointsT, worldPositionStays: true);
                }
            }
            Debug.Log("[Reorg] Waypoints nested under GREEN");
        }

        // 2. Group interactables under _Interactables
        var interRoot = GameObject.Find("_Interactables");
        if (interRoot == null)
            interRoot = new GameObject("_Interactables");
        foreach (var name in new[] { "Door_Passcode", "Cabinet", "Pickup_CluePaper" })
        {
            var go = GameObject.Find(name);
            if (go != null && go.transform.parent != interRoot.transform)
                go.transform.SetParent(interRoot.transform, worldPositionStays: true);
        }
        Debug.Log("[Reorg] Interactables grouped");

        // 3. Group enemies under _Enemies (room to grow later)
        var enemyRoot = GameObject.Find("_Enemies");
        if (enemyRoot == null)
            enemyRoot = new GameObject("_Enemies");
        if (green != null && green.transform.parent != enemyRoot.transform)
            green.transform.SetParent(enemyRoot.transform, worldPositionStays: true);
        Debug.Log("[Reorg] Enemies grouped");

        // 4. Red pickup sprite
        var pk = GameObject.Find("Pickup_CluePaper");
        if (pk != null)
        {
            var sr = pk.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/S_Red.png");
                Debug.Log("[Reorg] Pickup sprite -> S_Red");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
