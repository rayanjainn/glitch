using System;
using BugReport.Core.Containment;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Core.Interaction;
using UnityEngine;

namespace BugReport
{
    /// <summary>
    /// Per-level composition root for engine-independent services. Created and
    /// owned by <see cref="GameBootstrap"/>; disposed when the level unloads, which
    /// clears every registration and selection so nothing leaks between attempts.
    ///
    /// Components that may initialise before the bootstrap (scene load order is not
    /// guaranteed) use <see cref="WhenReady"/> instead of reading <see cref="Current"/>.
    /// </summary>
    public sealed class GameServices : IDisposable
    {
        private static GameServices current;
        private static Action<GameServices> pending;

        public GameServices(GameState initialState = GameState.Boot)
        {
            Registry = new EntityRegistry();
            Selection = new SelectionModel(Registry, TargetRequirement.Selection);
            State = new GameStateMachine(initialState);
            Glitches = new GlitchManager(Registry);
            Containment = new ContainmentModel();
        }

        public EntityRegistry Registry { get; }
        public SelectionModel Selection { get; }
        public GameStateMachine State { get; }
        public GlitchManager Glitches { get; }
        /// <summary>Level-scoped: a restart creates new services, so containment always starts calm.</summary>
        public ContainmentModel Containment { get; }
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Raised when level machinery (doors, lifts) changes what is passable without a glitch.
        /// Glitch changes are already covered by GlitchManager events.
        /// </summary>
        public event Action WorldChanged;

        public void RaiseWorldChanged() => WorldChanged?.Invoke();

        public static GameServices Current => current;
        public static bool IsReady => current != null;

        /// <summary>Invokes <paramref name="callback"/> now if services exist, otherwise once they are installed.</summary>
        public static void WhenReady(Action<GameServices> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (current != null) callback(current);
            else pending += callback;
        }

        /// <summary>Removes a callback that has not fired yet. Safe to call at any time.</summary>
        public static void CancelWhenReady(Action<GameServices> callback)
        {
            if (callback != null) pending -= callback;
        }

        public static void Install(GameServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (services.IsDisposed) throw new ObjectDisposedException(nameof(GameServices));
            if (current != null && current != services)
                throw new InvalidOperationException("Another GameServices instance is already installed.");

            current = services;
            var callbacks = pending;
            pending = null;
            callbacks?.Invoke(services);
        }

        public static void Uninstall(GameServices services)
        {
            if (current == services) current = null;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            Uninstall(this);
            // Glitches first: ending operations restores objects while they are still registered.
            Glitches.Dispose();
            WorldChanged = null;
            Selection.Dispose();
            Registry.Clear();
        }

        /// <summary>
        /// Static state survives play-mode entry when Domain Reload is disabled
        /// (Enter Play Mode Options). Reset it explicitly.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            pending = null;
        }
    }
}
