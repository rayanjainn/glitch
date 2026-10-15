using BugReport.Core.GameFlow;
using BugReport.Core.Presentation;
using BugReport.Progress;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// The level's story beat: a few lines typed out like a system log as the level starts. It never
    /// blocks play, and is shown only the first time a level is played (replays go straight in).
    /// </summary>
    public sealed class StoryOverlay : UiLayer
    {
        private const float CharactersPerSecond = 38f;
        private const float LingerSeconds = 4.5f;

        [SerializeField, OptionalReference] private LevelCatalog catalog;
        [SerializeField, Tooltip("Show the beat even when the level was completed before (tests).")]
        private bool alwaysShow;

        private VisualElement box;
        private Label body;
        private string text = string.Empty;
        private float startedAt = -1f;
        private float elapsed;
        private int shownCharacters = -1;
        private bool consumed;

        protected override string LayerName => "story";
        protected override int SortKey => 15;

        public bool BoxVisible => box != null && Ui.IsShown(box);
        public string ShownText => body != null ? body.text : string.Empty;

        public void Configure(LevelCatalog levelCatalog, bool showAlways = false)
        {
            catalog = levelCatalog;
            alwaysShow = showAlways;
        }

        protected override void Build(VisualElement layer)
        {
            box = Ui.Add(layer, Ui.Element("story-box", "br-panel", "br-story"));
            box.Add(Ui.Text("story-tag", "SYSTEM LOG", "br-caption"));
            body = Ui.Add(box, Ui.Text("story-text", string.Empty, "br-story__text"));
            Ui.Show(box, false);
        }

        protected override void Refresh()
        {
            var state = HasServices ? Services.State.Current : GameState.Boot;
            if (!consumed && state == GameState.Playing) TryStart();

            bool running = startedAt >= 0f && state != GameState.Ending;
            if (running)
            {
                if (state == GameState.Playing || state == GameState.Inspection) elapsed += Time.unscaledDeltaTime;
                int visible = Typewriter.VisibleCharacters(elapsed, CharactersPerSecond, text.Length);
                if (visible != shownCharacters)
                {
                    shownCharacters = visible;
                    Ui.SetText(body, text.Substring(0, visible));
                }
                if (elapsed > text.Length / CharactersPerSecond + LingerSeconds) startedAt = -1f;
            }
            Ui.Show(box, running && state != GameState.Paused);
        }

        private void TryStart()
        {
            consumed = true;
            if (catalog == null) return;
            int index = catalog.IndexOfScene(gameObject.scene.name);
            if (index < 0) return;
            var entry = catalog.Get(index);
            if (string.IsNullOrWhiteSpace(entry.intro)) return;
            if (!alwaysShow && SaveStore.Current.IsCompleted(entry.id)) return;
            text = entry.intro;
            startedAt = 0f;
            elapsed = 0f;
        }
    }
}
