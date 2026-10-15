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
        /// <summary>Default key text, action text, and the rebindable indices whose current keys replace the default.</summary>
        private static readonly (string key, string action, int[] rebindables)[] Controls =
        {
            ("WASD / MOUSE", "FLY AND LOOK", new[] { 0, 2, 1, 3 }),
            ("SPACE / CTRL", "UP / DOWN", new[] { 4, 5 }),
            ("SHIFT", "BOOST", new[] { 6 }),
            ("E", "SELECT", new[] { 7 }),
            ("LMB", "APPLY GLITCH", new[] { 8 }),
            ("R", "UNDO LAST GLITCH", new[] { 9 }),
            ("Q", "SWITCH GLITCH", new[] { 10 }),
            ("TAB", "INSPECT", new[] { 11 }),
        };

        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField, OptionalReference, Tooltip("For the NEXT LEVEL button. Without it the complete screen offers restart and main menu only.")]
        private LevelCatalog catalog;
        [SerializeField] private string levelTitle = "TRAINING LAB";
        [SerializeField, OptionalReference, Tooltip("Enables key rebinding in Settings and shows the player's keys in the controls list.")]
        private UnityEngine.InputSystem.InputActionAsset actions;
        private readonly System.Collections.Generic.List<Label> controlKeys = new System.Collections.Generic.List<Label>();

        private VisualElement pauseScreen;
        private VisualElement pauseCard;
        private SettingsPanel settings;
        private VisualElement completeScreen;
        private Button resumeButton;
        private Button nextButton;
        private Button completeRestartButton;
        private Label completeSummary;
        private Label completeStars;
        private Label completeStats;
        private Label completeHint;
        private Label completeOutro;
        private Label completeStamp;
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

        protected override void OnDisable()
        {
            settings?.Dispose();
            settings = null;
            base.OnDisable();
        }

        /// <summary>Shows the player's current keys (after rebinding) in the pause screen's list.</summary>
        private void RefreshControlKeys()
        {
            if (actions == null || settings == null) return;
            for (int i = 0; i < Controls.Length && i < controlKeys.Count; i++)
            {
                var parts = new string[Controls[i].rebindables.Length];
                for (int p = 0; p < parts.Length; p++) parts[p] = settings.KeyText(Controls[i].rebindables[p]);
                string joined = string.Join(i == 0 ? "" : " / ", parts);
                Ui.SetText(controlKeys[i], i == 0 ? joined + " / MOUSE" : joined);
            }
        }

        public void CloseSettings()
        {
            SaveStore.Flush();
            RefreshControlKeys();
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
            controlKeys.Clear();
            foreach (var (key, action, _) in Controls)
            {
                var row = Ui.Add(controls, Ui.Element("controls-row", "br-controls__row"));
                controlKeys.Add(Ui.Add(row, Ui.Text("controls-key", key, "br-key")));
                row.Add(Ui.Text("controls-action", action, "br-controls__action"));
            }

            settings?.Dispose();
            settings = new SettingsPanel(CloseSettings, showFullscreen: !Application.isEditor, actions);
            pauseScreen.Add(settings.Root);

            completeScreen = Ui.Add(layer, MakeScreen("complete"));
            var completeCard = Ui.Add(completeScreen, Ui.Element("complete-card", "br-card", "br-card--success"));
            completeCard.Add(Ui.Text("complete-caption", "REPORT CLOSED", "br-caption"));
            completeCard.Add(Ui.Text("complete-title", levelTitle + " COMPLETE", "br-title", "br-display"));
            completeStars = Ui.Add(completeCard, Ui.Text("complete-stars", string.Empty, "br-stars", "br-display"));
            completeStats = Ui.Add(completeCard, Ui.Text("complete-stats", string.Empty, "br-card__body", "br-stats"));
            completeHint = Ui.Add(completeCard, Ui.Text("complete-hint", string.Empty, "br-caption"));
            completeOutro = Ui.Add(completeCard, Ui.Text("complete-outro", string.Empty, "br-card__body", "br-outro"));
            completeStamp = Ui.Add(completeScreen, Ui.Text("complete-stamp", "CLOSED", "br-stamp", "br-display"));
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

        /// <summary>Stars, time, uses and the story outro on the level-complete card.</summary>
        private void FillRunCard()
        {
            var recorder = FindAnyObjectByType<LevelCompletionRecorder>();
            if (recorder == null || recorder.CurrentIndex() < 0)
            {
                Ui.SetText(completeStars, string.Empty);
                Ui.SetText(completeStats, string.Empty);
                Ui.SetText(completeHint, string.Empty);
                Ui.SetText(completeOutro, string.Empty);
                return;
            }
            Ui.SetText(completeStars, RunSummary.Stars(recorder.Stars));
            Ui.SetText(completeStats, "TIME " + RunSummary.Time(recorder.Seconds) + "   GLITCHES " + recorder.GlitchUses
                + " (PAR " + recorder.ParUses + ")   PEAK ALERT " + recorder.MaxStage);
            Ui.SetText(completeHint, RunSummary.NextStarHint(recorder.Stars, recorder.ParUses));
            int index = recorder.CurrentIndex();
            Ui.SetText(completeOutro, index >= 0 && catalog != null ? catalog.Get(index).outro ?? string.Empty : string.Empty);
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
                RefreshControlKeys();
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
                FillRunCard();
                bool hasNext = NextLevelScene() != null;
                Ui.Show(nextButton, hasNext);
                Ui.FocusSoon(hasNext ? nextButton : completeRestartButton);
            }
        }
    }
}
