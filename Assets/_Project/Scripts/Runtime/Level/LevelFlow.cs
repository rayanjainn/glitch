using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugReport.Level
{
    /// <summary>
    /// Level-wide actions behind menu buttons. Restart reloads the scene, which recreates every
    /// service: no stale glitches, copies or containment survive.
    /// </summary>
    public static class LevelFlow
    {
        public static void RestartLevel()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else Debug.LogWarning("[BUG REPORT] Cannot restart: active scene is not in Build Settings.");
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
