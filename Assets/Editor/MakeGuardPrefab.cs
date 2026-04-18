#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using PrismZone.Core;
using PrismZone.Enemy;

/// <summary>
/// Creates Guard_NPC.prefab — a variant of GREEN_Watcher tuned to be the
/// v0.4 main security guard:
///   - revealFilter = None (always visible like a human, not revealed by filter)
///   - vision cone 120° dist 5
///   - chase 3 / patrol 1.5 (same as GREEN tuning)
///   - sprite = npc_guard_idle_front.png
///
/// Designers drop Guard_NPC anywhere — same waypoint pattern.
/// </summary>
public static class MakeGuardPrefab
{
    [MenuItem("Tools/Prism Zone/Make Guard_NPC Prefab")]
    public static void Execute()
    {
        const string dst = "Assets/Prefabs/Guard_NPC.prefab";
        var green = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GREEN_Watcher.prefab");
        if (green == null) { Debug.LogError("Missing GREEN_Watcher.prefab"); return; }

        // Copy GREEN prefab asset file to new path (keeps all components + waypoints child)
        if (System.IO.File.Exists(dst)) AssetDatabase.DeleteAsset(dst);
        AssetDatabase.CopyAsset("Assets/Prefabs/GREEN_Watcher.prefab", dst);
        AssetDatabase.Refresh();

        // Mutate the copy in-memory
        var contents = PrefabUtility.LoadPrefabContents(dst);
        try
        {
            contents.name = "Guard_NPC";

            // Swap sprite
            var sprT = contents.transform.Find("Sprite");
            if (sprT != null)
            {
                var sr = sprT.GetComponent<SpriteRenderer>();
                var guardIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/NPCs/npc_guard_idle_front.png");
                if (sr != null && guardIdle != null) sr.sprite = guardIdle;
            }

            // Tune GreenEnemy fields
            var ai = contents.GetComponent<GreenEnemy>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                so.FindProperty("revealFilter").intValue = (int)FilterColor.None; // guard always visible
                so.FindProperty("visionRange").floatValue = 5f;
                so.FindProperty("visionAngleDeg").floatValue = 120f;
                so.FindProperty("patrolSpeed").floatValue = 1.5f;
                so.FindProperty("chaseSpeed").floatValue = 3f;
                so.FindProperty("aggroLossTime").floatValue = 20f;
                so.FindProperty("hiddenAlpha").floatValue = 1f;  // always visible
                so.FindProperty("chaseAlpha").floatValue = 1f;
                so.FindProperty("revealedAlpha").floatValue = 1f;
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, dst);
            Debug.Log("[Guard] Saved " + dst);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets();
    }
}
#endif
