using BugReport.Core.GameFlow;
using BugReport.Core.Presentation;
using BugReport.Core.Progress;
using BugReport.Level;
using BugReport.Presentation;
using BugReport.Progress;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Pause screen (resume, settings, controls reference, restart, main menu, quit) and the
    /// level-complete screen (next level, restart, main menu). Esc toggles pause through the input
    /// layer; the cursor is freed by GameStatePresenter. The first button takes focus when a
    /// screen opens, so keyboard and gamepad navigation work without the mouse.
    /// </summary>
    public sealed class GameMenus : UiLayer
    {
        private static readonly (string key, string action)[] Controls =
        {
            ("WASD / MOUSE", "FLY AND LOOK"),
            ("SPACE / CTRL", "UP / DOWN"),
            ("SHIFT", "BOOST"),
            ("E", "SELECT"),
            ("LMB", "APPLY GLITCH"),
            ("R", "UNDO LAST GLITCH"),
            ("Q", "SWITCH GLITCH"),
            ("TAB", "INSPECT"),
        };

        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField, OptionalReference, Tooltip("For the NEXT LEVEL button. Without it the complete screen offers restart and main menu only.")]
        private LevelCatalog catalog;
        [SerializeField] private string levelTitle = "TRAINING LAB";

        private VisualElement pauseScreen;
        private VisualElement pauseCard;
        private SettingsPanel settings;
        private VisualElement completeScreen;
        private Button resumeButton;
        private Button nextButton;
        private Button completeRestartButton;
        private Label completeSummary;
        private GameState shownState = GameState.Boot;

        protected override string LayerName => "menus";
        protected override int SortKey => 20;

        public bool PauseVisible => pauseScreen != null && Ui.IsShown(pauseScreen);
        public bool SettingsVisible => settings != null && Ui.IsShown(settings.Root);
        public bool CompleteVisible => completeScreen != null && Ui.IsShown(completeScreen);
        public bool NextLevelAvailable => nextButton != null && Ui.IsShown(nextButton);
        public string CompleteSummaryText => completeSummary != null ? completeSummary.text : string.Empty;
        public SettingsPanel Settings => settings;

        public void Configure(ObjectiveDirector objectiveDirector, string title, LevelCatalog levelCatalog = null)
        {
            objectives = objectiveDirector;
            levelTitle = title;
            catalog = levelCatalog;
        }

        // ---------------------------------------------------------------- actions (buttons; public for tests)

        public void Resume()
        {
            if (HasServices && Services.State.Current == GameState.Paused) Services.State.TogglePause();
        }

        public void OpenSettings()
        {
            settings.Refresh();
            Ui.Show(pauseCard, false);
            Ui.Show(settings.Root, true);
            settings.Focus();
        }

        public void CloseSettings()
        {
            Ui.Show(settings.Root, false);
            Ui.Show(pauseCard, true);
            Ui.FocusSoon(resumeButton);
        }

        /// <summary>Scene name of the next catalogued level, or null.</summary>
        public string NextLevelScene()
        {
            if (catalog == null) return null;
            int index = catalog.IndexOfScene(gameObject.scene.name);
            if (index < 0) return null;
            int next = LevelProgress.NextIndex(catalog.Ids, catalog.Get(index).id);
            return next >= 0 ? catalog.Get(next).sceneName : null;
        }

        private void LoadNextLevel() => LevelFlow.LoadLevel(NextLevelScene());

        // ---------------------------------------------------------------- build

        protected override void Build(VisualElement layer)
        {
            pauseScreen = Ui.Add(layer, MakeScreen("pause"));
            pauseCard = Ui.Add(pauseScreen, Ui.Element("pause-card", "br-card"));
            pauseCard.Add(Ui.Text("pause-caption", levelTitle, "br-caption"));
            pauseCard.Add(Ui.Text("pause-title", "PAUSED", "br-title", "br-display"));
            var columns = Ui.Add(pauseCard, Ui.Element("pause-columns", "br-card__columns"));
            var buttons = Ui.Add(columns, Ui.Element("pause-buttons", "br-card__buttons"));
            resumeButton = Ui.Add(buttons, MenuButton("resume", "RESUME", Resume));
            buttons.Add(MenuButton("open-settings", "SETTINGS", OpenSettings));
            buttons.Add(MenuButton("restart", "RESTART LEVEL", LevelFlow.RestartLevel));
            buttons.Add(MenuButton("main-menu", "MAIN MENU", LevelFlow.LoadMainMenu));
            buttons.Add(MenuButton("quit", "QUIT GAME", LevelFlow.Quit));

            var controls = Ui.Add(columns, Ui.Element("controls", "br-controls"));
            controls.Add(Ui.Text("controls-caption", "CONTROLS", "br-caption"));
            foreach (var (key, action) in Controls)
            {
                var row = Ui.Add(controls, Ui.Element("controls-row", "br-controls__row"));
                row.Add(Ui.Text("controls-key", key, "br-key"));
                row.Add(Ui.Text("controls-action", action, "br-controls__action"));
            }

            settings = new SettingsPanel(CloseSettings, showFullscreen: !Application.isEditor);
            pauseScreen.Add(settings.Root);

            completeScreen = Ui.Add(layer, MakeScreen("complete"));
            var completeCard = Ui.Add(completeScreen, Ui.Element("complete-card", "br-card", "br-card--success"));
            completeCard.Add(Ui.Text("complete-caption", "REPORT CLOSED", "br-caption"));
            completeCard.Add(Ui.Text("complete-title", levelTitle + " COMPLETE", "br-title", "br-display"));
            completeSummary = Ui.Add(completeCard, Ui.Text("complete-summary", string.Empty, "br-card__body"));
            var completeButtons = Ui.Add(completeCard, Ui.Element("complete-buttons", "br-card__buttons"));
            nextButton = Ui.Add(completeButtons, MenuButton("next-level", "NEXT LEVEL", LoadNextLevel));
            completeRestartButton = Ui.Add(completeButtons, MenuButton("complete-restart", "RESTART LEVEL", LevelFlow.RestartLevel));
            completeButtons.Add(MenuButton("complete-main-menu", "MAIN MENU", LevelFlow.LoadMainMenu));

            Ui.Show(pauseScreen, false);
            Ui.Show(settings.Root, false);
            Ui.Show(completeScreen, false);
            shownState = GameState.Boot;
        }

        private static VisualElement MakeScreen(string name)
        {
            var screen = Ui.Element(name, "br-screen");
            screen.pickingMode = PickingMode.Position; // blocks clicks to anything underneath
            return screen;
        }

        internal static Button MenuButton(string name, string text, System.Action onClick)
        {
            var button = new Button(onClick) { name = name, text = text };
            button.AddToClassList("br-button");
            return button;
        }

        // ---------------------------------------------------------------- refresh

        protected override void Refresh()
        {
            var state = HasServices ? Services.State.Current : GameState.Boot;
            if (state == shownState) return;
            shownState = state;

            bool paused = state == GameState.Paused;
            bool ended = state == GameState.Ending;
            Ui.Show(pauseScreen, paused);
            Ui.Show(completeScreen, ended);

            if (paused)
            {
                Ui.Show(settings.Root, false);
                Ui.Show(pauseCard, true);
                Ui.FocusSoon(resumeButton);
            }
            else
            {
                SaveStore.Flush();
            }

            if (ended)
            {
                var tracker = objectives != null ? objectives.Tracker : null;
                Ui.SetText(completeSummary, tracker != null
                    ? HudFormat.CompletionSummary(tracker.CompletedCount, objectives.Count)
                    : HudFormat.CompletionSummary(0, 0));
                bool hasNext = NextLevelScene() != null;
                Ui.Show(nextButton, hasNext);
                Ui.FocusSoon(hasNext ? nextButton : completeRestartButton);
            }
        }
    }
}
