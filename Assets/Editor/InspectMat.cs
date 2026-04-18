#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PrismZone.Core;

public static class InspectMat
{
    public static void Execute()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Shaders/M_PrismFilter.mat");
        Debug.Log($"[Mat] _FilterMode = {mat.GetFloat("_FilterMode")}");
        if (FilterManager.Instance != null)
            Debug.Log($"[Mat] FilterManager.Current = {FilterManager.Instance.Current}");
    }
}
#endif
