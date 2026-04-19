#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the physics layers we need (Wall, DoorBlock) in the User Layer slots
/// if they are missing. Idempotent.
/// </summary>
public static class EnsureLayers
{
    private static readonly string[] Wanted = { "Wall", "DoorBlock" };

    public static void Execute()
    {
        var tm = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
        var so = new SerializedObject(tm);
        var layersProp = so.FindProperty("layers");

        foreach (var wanted in Wanted)
        {
            if (IndexOf(layersProp, wanted) >= 0) continue;
            int slot = FirstEmpty(layersProp, start: 8); // user layers start at 8
            if (slot < 0) { Debug.LogWarning($"[EnsureLayers] No empty slot for '{wanted}'"); continue; }
            layersProp.GetArrayElementAtIndex(slot).stringValue = wanted;
            Debug.Log($"[EnsureLayers] Added layer '{wanted}' to slot {slot}");
        }
        so.ApplyModifiedProperties();

        // Apply Wall layer to Tilemap_Walls if present
        var walls = GameObject.Find("Tilemap_Walls");
        if (walls != null)
        {
            int idx = LayerMask.NameToLayer("Wall");
            if (idx >= 0) walls.layer = idx;
        }
    }

    private static int IndexOf(SerializedProperty arr, string name)
    {
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).stringValue == name) return i;
        return -1;
    }

    private static int FirstEmpty(SerializedProperty arr, int start)
    {
        for (int i = start; i < arr.arraySize; i++)
            if (string.IsNullOrEmpty(arr.GetArrayElementAtIndex(i).stringValue)) return i;
        return -1;
    }
}
#endif
