using System;
using BugReport.Core.GameFlow;
using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// Applies engine-side consequences of the game state: cursor lock and global
    /// time scale for the pause menu. Restores both when destroyed so a scene
    /// reload or exiting play mode never leaves the game paused.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStatePresenter : MonoBehaviour
    {
        [SerializeField] private bool lockCursorDuringGameplay = true;

        private GameServices services;
        private Action<GameServices> bindCallback;

        private void OnEnable()
        {
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (services != null) services.State.StateChanged -= OnStateChanged;
            services = null;
            RestoreDefaults();
        }

        private void Bind(GameServices s)
        {
            services = s;
            services.State.StateChanged += OnStateChanged;
            Apply(services.State.Current);
        }

        private void OnStateChanged(GameState previous, GameState current) => Apply(current);

        private void Apply(GameState state)
        {
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
            bool locked = lockCursorDuringGameplay && (state == GameState.Playing || state == GameState.Inspection);
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private static void RestoreDefaults()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
