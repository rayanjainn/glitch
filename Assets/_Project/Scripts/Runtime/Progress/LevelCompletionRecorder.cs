using BugReport.Level;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugReport.Progress
{
    /// <summary>Saves the level as completed the moment its last objective is done.</summary>
    public sealed class LevelCompletionRecorder : MonoBehaviour
    {
        [SerializeField] private ObjectiveDirector objectives;
        [SerializeField] private LevelCatalog catalog;

        public void Configure(ObjectiveDirector objectiveDirector, LevelCatalog levelCatalog)
        {
            objectives = objectiveDirector;
            catalog = levelCatalog;
        }

        private void OnEnable()
        {
            if (objectives != null) objectives.LevelCompleted += Record;
        }

        private void OnDisable()
        {
            if (objectives != null) objectives.LevelCompleted -= Record;
        }

        /// <summary>The catalog id of the active scene, or null if the scene isn't a catalogued level.</summary>
        public string CurrentLevelId()
        {
            if (catalog == null) return null;
            int index = catalog.IndexOfScene(gameObject.scene.name);
            return index >= 0 ? catalog.Get(index).id : null;
        }

        private void Record()
        {
            var id = CurrentLevelId();
            if (id != null) SaveStore.MarkLevelCompleted(id);
        }
    }
}
