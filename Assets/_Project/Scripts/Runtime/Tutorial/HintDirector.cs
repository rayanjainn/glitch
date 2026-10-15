using BugReport.Core.GameFlow;
using BugReport.Core.Tutorial;
using BugReport.Level;
using UnityEngine;

namespace BugReport.Tutorial
{
    /// <summary>
    /// Escalating help for a stuck player (see <see cref="StuckHints"/>): nothing at first, then the
    /// objective's hint text, then a marker on the key object of the solution. Time only counts while
    /// playing; any new objective or glitch use restarts the clock. The HUD and the world overlay read
    /// <see cref="Tier"/> and <see cref="FocusTarget"/>; nothing here changes gameplay.
    /// </summary>
    public sealed class HintDirector : MonoBehaviour
    {
        [SerializeField] private ObjectiveDirector objectives;
        [SerializeField, Min(1f)] private float pulseAfter = 25f;
        [SerializeField, Min(1f)] private float hintAfter = 40f;
        [SerializeField, Min(1f)] private float markerAfter = 90f;

        private StuckHints stuck;
        private GameServices services;
        private System.Action<GameServices> bindCallback;
        private int glitchActions;

        public int Tier => stuck != null ? stuck.Tier : 0;
        public bool ShowHintText => Tier >= 2;
        public float IdleSeconds => stuck != null ? stuck.IdleSeconds : 0f;

        /// <summary>The current objective's key object once the player has been stuck long enough, otherwise null.</summary>
        public GameEntity FocusTarget
        {
            get
            {
                if (Tier < 3 || objectives == null || objectives.Tracker == null || objectives.Tracker.AllComplete) return null;
                return objectives.Get(objectives.Tracker.CurrentIndex).focus;
            }
        }

        public void Configure(ObjectiveDirector objectiveDirector) => objectives = objectiveDirector;

        private void Awake() => stuck = new StuckHints(pulseAfter, hintAfter, markerAfter);

        private void OnEnable()
        {
            bindCallback = s =>
            {
                services = s;
                s.Glitches.OperationStarted += OnGlitch;
            };
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed) services.Glitches.OperationStarted -= OnGlitch;
            services = null;
        }

        private void OnGlitch(Core.Glitches.GlitchOperation _) => glitchActions++;

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>Advances the idle clock. Public for tests.</summary>
        public void Step(float deltaTime)
        {
            if (stuck == null) stuck = new StuckHints(pulseAfter, hintAfter, markerAfter);
            if (services == null || services.IsDisposed || objectives == null || objectives.Tracker == null || objectives.Tracker.AllComplete) return;
            var state = services.State.Current;
            if (state != GameState.Playing && state != GameState.Inspection) return;
            stuck.Update(deltaTime, objectives.Tracker.CurrentIndex, glitchActions);
        }
    }
}
