#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DumpPrefabDetail
{
    public static void Execute()
    {
        Dump("Assets/Prefabs/HUD_Canvas.prefab");
        Dump("Assets/Prefabs/EventSystem.prefab");
    }

    static void Dump(string path)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (p == null) { Debug.Log($"[Detail] MISSING {path}"); return; }
        Debug.Log($"[Detail] --- {path}");
        Walk(p.transform, 0);
    }

    static void Walk(Transform t, int depth)
    {
        var comps = t.GetComponents<Component>();
        string comp = "";
        foreach (var c in comps) if (c != null) comp += " " + c.GetType().Name;
        Debug.Log($"[Detail] {new string(' ', depth*2)}{t.name}  [{comp.Trim()}]");
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth+1);
    }
}
#endif
