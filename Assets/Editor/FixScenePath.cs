#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixScenePath
{
    public static void Execute()
    {
        const string stray = "Assets/SampleScene.unity";
        const string canonical = "Assets/Scenes/SampleScene.unity";

        // If only the stray exists, move it back into Scenes/.
        bool hasStray = System.IO.File.Exists(stray);
        bool hasCanonical = System.IO.File.Exists(canonical);
        Debug.Log($"[FixScene] stray={hasStray} canonical={hasCanonical}");

        if (!hasStray) { Debug.Log("[FixScene] No stray to move."); return; }

        // Close any open reference to old canonical, then overwrite.
        if (hasCanonical)
        {
            AssetDatabase.DeleteAsset(canonical);
            Debug.Log("[FixScene] Deleted outdated canonical scene");
        }

        string err = AssetDatabase.MoveAsset(stray, canonical);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError($"[FixScene] MoveAsset failed: {err}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Re-open the moved scene so Unity doesn't sit on a broken reference
        EditorSceneManager.OpenScene(canonical, OpenSceneMode.Single);
        Debug.Log($"[FixScene] Moved stray to {canonical} and re-opened.");
    }
}
#endif
