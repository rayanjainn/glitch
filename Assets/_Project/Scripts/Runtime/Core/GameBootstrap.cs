using BugReport.Core.GameFlow;
using UnityEngine;

namespace BugReport
{
    /// <summary>
    /// Scene-level bootstrap. Creates <see cref="GameServices"/> before any other
    /// project script runs and disposes them when the scene unloads.
    /// A dedicated Bootstrap scene replaces this per-level setup in a later milestone
    /// (see TASKS.md); the service API stays the same.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameState initialState = GameState.Playing;

        private static GameBootstrap owner;
        private GameServices services;

        public GameServices Services => services;

        private void Awake()
        {
            if (GameServices.IsReady)
            {
                if (owner != null && owner != this)
                {
                    Debug.LogError("[BUG REPORT] A second GameBootstrap was found; only one may be active. Disabling " + name + ".", this);
                    enabled = false;
                    return;
                }
                // The previous owner is already destroyed (e.g. teardown order during a scene reload).
                // Its services are stale; dispose them so nothing leaks into this level.
                Debug.LogWarning("[BUG REPORT] Disposing stale GameServices left by a destroyed bootstrap.", this);
                GameServices.Current.Dispose();
            }

            if (!GameLayers.AreConfigured(out var missing))
            {
                Debug.LogWarning("[BUG REPORT] Missing physics layers: " + missing +
                                 ". Run 'BUG REPORT > Setup > 1. Configure Project'. Targeting and collision rules will be wrong until then.", this);
            }

            services = new GameServices();
            GameServices.Install(services);
            owner = this;
            if (!services.State.TryTransition(initialState))
            {
                Debug.LogError("[BUG REPORT] Cannot enter initial state " + initialState + " from " + services.State.Current + ".", this);
            }
        }

        private void OnDestroy()
        {
            if (owner == this) owner = null;
            if (services == null) return;
            services.Dispose();
            services = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => owner = null;
    }
}
