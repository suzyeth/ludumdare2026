#if UNITY_EDITOR
using UnityEditor.SceneManagement;
public static class OpenTemplate
{
    public static void Execute()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level_Template.unity", OpenSceneMode.Single);
    }
}
#endif
