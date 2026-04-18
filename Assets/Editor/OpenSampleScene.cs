#if UNITY_EDITOR
using UnityEditor.SceneManagement;
public static class OpenSampleScene
{
    public static void Execute()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
    }
}
#endif
