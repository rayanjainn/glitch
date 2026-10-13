using System;

namespace BugReport.Core.GameFlow
{
    /// <summary>
    /// High-level game state with an explicit transition table.
    /// Presentation (cursor lock, time scale, menus) reacts to <see cref="StateChanged"/>;
    /// this class never touches the engine.
    /// </summary>
    public sealed class GameStateMachine
    {
        private GameState resumeState = GameState.Playing;

        public GameStateMachine(GameState initial = GameState.Boot)
        {
            Current = initial;
        }

        public GameState Current { get; private set; }

        /// <summary>(previous, current).</summary>
        public event Action<GameState, GameState> StateChanged;

        /// <summary>True while the anomaly can act in the world.</summary>
        public bool IsGameplayActive => Current == GameState.Playing || Current == GameState.Inspection;

        public static bool IsTransitionAllowed(GameState from, GameState to)
        {
            if (from == to) return false;
            switch (from)
            {
                case GameState.Boot:
                    return to == GameState.MainMenu || to == GameState.Playing || to == GameState.LevelTransition;
                case GameState.MainMenu:
                    return to == GameState.LevelTransition || to == GameState.Playing;
                case GameState.Playing:
                    return to == GameState.Inspection || to == GameState.Paused || to == GameState.LevelTransition || to == GameState.Ending;
                case GameState.Inspection:
                    return to == GameState.Playing || to == GameState.Paused || to == GameState.LevelTransition || to == GameState.Ending;
                case GameState.Paused:
                    return to == GameState.Playing || to == GameState.Inspection || to == GameState.MainMenu || to == GameState.LevelTransition;
                case GameState.LevelTransition:
                    return to == GameState.Playing || to == GameState.MainMenu;
                case GameState.Ending:
                    return to == GameState.MainMenu || to == GameState.LevelTransition;
                default:
                    return false;
            }
        }

        public bool TryTransition(GameState to)
        {
            var from = Current;
            if (!IsTransitionAllowed(from, to)) return false;
            if (to == GameState.Paused) resumeState = from;
            Current = to;
            StateChanged?.Invoke(from, to);
            return true;
        }

        /// <summary>Pauses gameplay, or resumes into the state that was active before pausing.</summary>
        public bool TogglePause()
        {
            if (Current == GameState.Paused) return TryTransition(resumeState);
            return TryTransition(GameState.Paused);
        }

        /// <summary>Switches between normal play and system inspection. No effect in other states.</summary>
        public bool ToggleInspection()
        {
            if (Current == GameState.Playing) return TryTransition(GameState.Inspection);
            if (Current == GameState.Inspection) return TryTransition(GameState.Playing);
            return false;
        }
    }
}
