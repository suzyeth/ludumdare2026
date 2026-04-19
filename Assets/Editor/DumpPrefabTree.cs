#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DumpPrefabTree
{
    public static void Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GREEN_Watcher.prefab");
        if (prefab == null) { Debug.LogWarning("No GREEN prefab"); return; }
        Walk(prefab.transform, 0);
    }

    static void Walk(Transform t, int depth)
    {
        Debug.Log($"[PrefabTree] {new string(' ', depth*2)}{t.name}");
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth+1);
    }
}
#endif
