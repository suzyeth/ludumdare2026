#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PrismZone.Core;
using PrismZone.Enemy;

/// <summary>
/// Builds RED_Shrike.prefab and BLUE_Weaver.prefab as disk-only prefabs (no scene
/// instances). Designers drag them into any scene when they want those enemy types.
/// </summary>
public static class MakeRedBluePrefabs
{
    private const string Folder = "Assets/Prefabs";

    public static void Execute()
    {
        MakeRed();
        MakeBlue();
        AssetDatabase.SaveAssets();
        Debug.Log("[MakeRedBlue] Done");
    }

    static Sprite SP(string relPath) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + relPath);

    static void MakeRed()
    {
        string path = $"{Folder}/RED_Shrike.prefab";
        var root = new GameObject("RED_Shrike");
        try
        {
            root.tag = "Enemy";

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root.transform, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = SP("NPCs/npc_red_idle.png");
            sr.sortingLayerName = SortingLayerOr("PlayerNPC");
            sr.sortingOrder = 1;

            var ai = root.AddComponent<RedEnemy>();
            SO(ai, "revealFilter", (int)FilterColor.Blue);
            SO(ai, "chaseSpeed", 5f);
            SO(ai, "returnSpeed", 2f);
            SO(ai, "hearingRadius", 8f);
            SO(ai, "stillnessRecallTime", 10f);
            SO(ai, "hiddenAlpha", 0.35f);
            SO(ai, "spriteRenderer", sr);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
        Debug.Log("[MakeRedBlue] Saved " + path);
    }

    static void MakeBlue()
    {
        string path = $"{Folder}/BLUE_Weaver.prefab";
        var root = new GameObject("BLUE_Weaver");
        try
        {
            root.tag = "Trap";

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root.transform, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = SP("NPCs/npc_blue.png");
            sr.sortingLayerName = SortingLayerOr("Traps");
            sr.sortingOrder = 0;

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var ai = root.AddComponent<BlueTrap>();
            SO(ai, "revealFilter", (int)FilterColor.Green);
            SO(ai, "alarmDuration", 10f);
            SO(ai, "retriggerCooldown", 3f);
            SO(ai, "hiddenAlpha", 0.35f);
            SO(ai, "spriteRenderer", sr);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
        Debug.Log("[MakeRedBlue] Saved " + path);
    }

    static string SortingLayerOr(string want)
    {
        foreach (var l in SortingLayer.layers) if (l.name == want) return want;
        return "Default";
    }

    static void SO(Object obj, string field, object value)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(field);
        if (p == null) return;
        switch (value)
        {
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case Object o: p.objectReferenceValue = o; break;
        }
        so.ApplyModifiedProperties();
    }
}
#endif
