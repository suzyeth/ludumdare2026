using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PrismZone.DebugTools;

public static class _McpRemoveTester
{
    public static string Execute()
    {
        if (EditorApplication.isPlaying) return "exit play first";
        var sb = new StringBuilder();

        foreach (var scenePath in new[]{
            "Assets/_Art/Scene/Scn_1&2Floor.unity",
            "Assets/_Art/Scene/Scn_3Floor.unity",
            "Assets/_Art/Scene/Scn_4Floor.unity",
        })
        {
            if (!System.IO.File.Exists(scenePath)) continue;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;
            foreach (var t in Object.FindObjectsByType<UIPanelTester>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var host = t.gameObject;
                sb.AppendLine($"{scene.name}: destroying {host.name} (hosting UIPanelTester)");
                Object.DestroyImmediate(host);
                dirty = true;
            }
            if (dirty) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }
}
