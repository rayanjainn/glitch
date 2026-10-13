using BugReport.Core.GameFlow;
using BugReport.Core.Presentation;
using BugReport.Level;
using BugReport.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Pause screen (resume, comfort toggle, controls reference, restart, quit) and the
    /// level-complete screen. Esc toggles pause through the input layer; the cursor is freed by
    /// GameStatePresenter. The first button takes focus when a screen opens, so keyboard and
    /// gamepad navigation work without the mouse.
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
        [SerializeField] private string levelTitle = "TRAINING LAB";

        private VisualElement pauseScreen;
        private VisualElement completeScreen;
        private Button resumeButton;
        private Button comfortButton;
        private Button completeRestartButton;
        private Label completeSummary;
        private GameState shownState = GameState.Boot;

        protected override string LayerName => "menus";
        protected override int SortKey => 20;

        public bool PauseVisible => pauseScreen != null && Ui.IsShown(pauseScreen);
        public bool CompleteVisible => completeScreen != null && Ui.IsShown(completeScreen);
        public string ComfortButtonText => comfortButton != null ? comfortButton.text : string.Empty;
        public string CompleteSummaryText => completeSummary != null ? completeSummary.text : string.Empty;

        public void Configure(ObjectiveDirector objectiveDirector, string title)
        {
            objectives = objectiveDirector;
            levelTitle = title;
        }

        // ---------------------------------------------------------------- actions (buttons; public for tests)

        public void Resume()
        {
            if (HasServices && Services.State.Current == GameState.Paused) Services.State.TogglePause();
        }

        public void ToggleColourSplit()
        {
            ComfortSettings.ChromaticEffects = !ComfortSettings.ChromaticEffects;
            UpdateComfortLabel();
        }

        // ---------------------------------------------------------------- build

        protected override void Build(VisualElement layer)
        {
            pauseScreen = Ui.Add(layer, MakeScreen("pause"));
            var pauseCard = Ui.Add(pauseScreen, Ui.Element("pause-card", "br-card"));
            pauseCard.Add(Ui.Text("pause-caption", levelTitle, "br-caption"));
            pauseCard.Add(Ui.Text("pause-title", "PAUSED", "br-title"));
            var columns = Ui.Add(pauseCard, Ui.Element("pause-columns", "br-card__columns"));
            var buttons = Ui.Add(columns, Ui.Element("pause-buttons", "br-card__buttons"));
            resumeButton = Ui.Add(buttons, MenuButton("resume", "RESUME", Resume));
            comfortButton = Ui.Add(buttons, MenuButton("comfort", string.Empty, ToggleColourSplit));
            buttons.Add(MenuButton("restart", "RESTART LEVEL", LevelFlow.RestartLevel));
            buttons.Add(MenuButton("quit", "QUIT", LevelFlow.Quit));

            var controls = Ui.Add(columns, Ui.Element("controls", "br-controls"));
            controls.Add(Ui.Text("controls-caption", "CONTROLS", "br-caption"));
            foreach (var (key, action) in Controls)
            {
                var row = Ui.Add(controls, Ui.Element("controls-row", "br-controls__row"));
                row.Add(Ui.Text("controls-key", key, "br-key"));
                row.Add(Ui.Text("controls-action", action, "br-controls__action"));
            }

            completeScreen = Ui.Add(layer, MakeScreen("complete"));
            var completeCard = Ui.Add(completeScreen, Ui.Element("complete-card", "br-card", "br-card--success"));
            completeCard.Add(Ui.Text("complete-caption", "REPORT CLOSED", "br-caption"));
            completeCard.Add(Ui.Text("complete-title", levelTitle + " COMPLETE", "br-title"));
            completeSummary = Ui.Add(completeCard, Ui.Text("complete-summary", string.Empty, "br-card__body"));
            var completeButtons = Ui.Add(completeCard, Ui.Element("complete-buttons", "br-card__buttons"));
            completeRestartButton = Ui.Add(completeButtons, MenuButton("complete-restart", "RESTART LEVEL", LevelFlow.RestartLevel));
            completeButtons.Add(MenuButton("complete-quit", "QUIT", LevelFlow.Quit));

            Ui.Show(pauseScreen, false);
            Ui.Show(completeScreen, false);
            shownState = GameState.Boot;
            UpdateComfortLabel();
        }

        private static VisualElement MakeScreen(string name)
        {
            var screen = Ui.Element(name, "br-screen");
            screen.pickingMode = PickingMode.Position; // blocks clicks to anything underneath
            return screen;
        }

        private static Button MenuButton(string name, string text, System.Action onClick)
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
                UpdateComfortLabel();
                Ui.FocusSoon(resumeButton);
            }
            else if (ended)
            {
                var tracker = objectives != null ? objectives.Tracker : null;
                Ui.SetText(completeSummary, tracker != null
                    ? HudFormat.CompletionSummary(tracker.CompletedCount, objectives.Count)
                    : HudFormat.CompletionSummary(0, 0));
                Ui.FocusSoon(completeRestartButton);
            }
        }

        private void UpdateComfortLabel()
        {
            if (comfortButton != null) comfortButton.text = "COLOUR-SPLIT EFFECTS: " + (ComfortSettings.ChromaticEffects ? "ON" : "OFF");
        }
    }
}
