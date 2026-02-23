using UnityEditor;
using UnityEngine.SceneManagement;

namespace _scripts.Editor
{
    public class EditorShortcuts
    {
#if UNITY_EDITOR
        [MenuItem("Game/Play from Start")]
        public static void PlayFromStart()
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneManager.GetSceneByBuildIndex(0).path);
            EditorApplication.EnterPlaymode();
        }
#endif
    }
}