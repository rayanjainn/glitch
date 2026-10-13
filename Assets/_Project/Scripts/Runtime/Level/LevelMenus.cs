using System;
using BugReport.Core.GameFlow;
using BugReport.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugReport.Level
{
    /// <summary>
    /// Placeholder pause menu and level-complete screen (IMGUI). Esc pauses (InputReader);
    /// the cursor is free while paused or finished (GameStatePresenter). Restart reloads the
    /// level, which recreates every service: no stale glitches, copies or containment.
    /// </summary>
    public sealed class LevelMenus : MonoBehaviour
    {
        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField] private string levelTitle = "TRAINING LAB";

        private GameServices services;
        private Action<GameServices> bindCallback;
        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle button;

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
        }

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

        private void OnGUI()
        {
            if (services == null || services.IsDisposed) return;
            var state = services.State.Current;
            if (state != GameState.Paused && state != GameState.Ending) return;
            EnsureStyles();

            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float w = 360f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.28f;

            if (state == GameState.Paused)
            {
                GUI.Label(new Rect(x, y, w, 40), "PAUSED", title);
                y += 60;
                if (GUI.Button(new Rect(x, y, w, 40), "RESUME", button)) services.State.TogglePause();
                y += 50;
                string comfort = "COLOUR-SPLIT EFFECTS: " + (ComfortSettings.ChromaticEffects ? "ON" : "OFF");
                if (GUI.Button(new Rect(x, y, w, 40), comfort, button)) ComfortSettings.ChromaticEffects = !ComfortSettings.ChromaticEffects;
                y += 50;
            }
            else
            {
                GUI.Label(new Rect(x - 100, y, w + 200, 40), "REPORT CLOSED: " + levelTitle + " COMPLETE", title);
                y += 46;
                string detail = "The hero reached the exit.";
                if (objectives != null && objectives.Tracker != null)
                    detail = objectives.Tracker.CompletedCount + "/" + objectives.Count + " objectives. " + detail;
                GUI.Label(new Rect(x - 100, y, w + 200, 30), detail, body);
                y += 50;
            }

            if (GUI.Button(new Rect(x, y, w, 40), "RESTART LEVEL", button)) RestartLevel();
            y += 50;
            if (GUI.Button(new Rect(x, y, w, 40), "QUIT", button)) Quit();
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            body.normal.textColor = new Color(0.8f, 0.95f, 1f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
    }
}
