using BugReport.Core.GameFlow;
using BugReport.Level;
using UnityEngine;

namespace BugReport.Progress
{
    /// <summary>
    /// Tracks the run (time spent playing, glitches used, worst containment stage) and saves the
    /// result the moment the last objective is done. Time counts only while the game is playing,
    /// so pausing and reading do not cost stars or records.
    /// </summary>
    public sealed class LevelCompletionRecorder : MonoBehaviour
    {
        [SerializeField] private ObjectiveDirector objectives;
        [SerializeField] private LevelCatalog catalog;

        private GameServices services;

        public float Seconds { get; private set; }
        public int GlitchUses { get; private set; }
        public int MaxStage { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>Stars earned by the finished run (0 until finished or when not catalogued).</summary>
        public int Stars { get; private set; }

        public void Configure(ObjectiveDirector objectiveDirector, LevelCatalog levelCatalog)
        {
            objectives = objectiveDirector;
            catalog = levelCatalog;
        }

        private void OnEnable()
        {
            if (objectives != null) objectives.LevelCompleted += Record;
            GameServices.WhenReady(Bind);
        }

        private void OnDisable()
        {
            if (objectives != null) objectives.LevelCompleted -= Record;
            GameServices.CancelWhenReady(Bind);
            if (services != null && !services.IsDisposed) services.Glitches.OperationStarted -= OnGlitch;
            services = null;
        }

        private void Bind(GameServices s)
        {
            services = s;
            s.Glitches.OperationStarted += OnGlitch;
        }

        private void OnGlitch(Core.Glitches.GlitchOperation op)
        {
            if (!Finished) GlitchUses++;
        }

        private void Update()
        {
            if (Finished || services == null || services.IsDisposed) return;
            if (services.State.Current != GameState.Playing) return;
            Seconds += Time.unscaledDeltaTime;
            if (services.Containment.Stage > MaxStage) MaxStage = services.Containment.Stage;
        }

        /// <summary>The catalog id of the active scene, or null if the scene isn't a catalogued level.</summary>
        public string CurrentLevelId()
        {
            int index = CurrentIndex();
            return index >= 0 ? catalog.Get(index).id : null;
        }

        public int CurrentIndex() => catalog == null ? -1 : catalog.IndexOfScene(gameObject.scene.name);

        public int ParUses
        {
            get
            {
                int index = CurrentIndex();
                return index >= 0 ? catalog.Get(index).parUses : 3;
            }
        }

        private void Record()
        {
            if (Finished) return;
            Finished = true;
            if (services != null && !services.IsDisposed && services.Containment.Stage > MaxStage) MaxStage = services.Containment.Stage;
            var id = CurrentLevelId();
            if (id != null) Stars = SaveStore.SubmitRun(id, Seconds, GlitchUses, MaxStage, ParUses);
        }
    }
}
