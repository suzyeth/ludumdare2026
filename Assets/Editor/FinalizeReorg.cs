#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalizeReorg
{
    public static void Execute()
    {
        // 1. Ensure _Interactables exists and all 3 interactables are nested under it.
        var interRoot = GameObject.Find("_Interactables");
        if (interRoot == null) interRoot = new GameObject("_Interactables");

        foreach (var name in new[] { "Door_Passcode", "Cabinet", "Pickup_CluePaper" })
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject found = null;
            foreach (var g in all)
                if (g.name == name && g.scene.IsValid()) { found = g; break; }
            if (found != null && found.transform.parent != interRoot.transform)
            {
                found.transform.SetParent(interRoot.transform, worldPositionStays: true);
                Debug.Log($"[Finalize] {name} nested under _Interactables");
            }
        }

        // 2. Set pickup sprite to S_Red.
        var pk = GameObject.Find("Pickup_CluePaper");
        if (pk != null)
        {
            var sr = pk.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/S_Red.png");
                Debug.Log("[Finalize] Pickup sprite -> S_Red");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
    }
}
#endif
