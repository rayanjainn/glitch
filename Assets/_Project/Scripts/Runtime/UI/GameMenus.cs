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
        private VisualElement completeCard;
        private VisualElement starsRow;
        private VisualElement statRows;
        private Label completeCaption, completeTitle, completeSubtitle;
        private Label rowObjectives, rowTime, rowGlitches, rowAlert, rowFiled;
        private readonly VisualElement[] starElements = new VisualElement[3];
        private GameState shownState = GameState.Boot;

        protected override string LayerName => "menus";
        protected override int SortKey => 20;

        public bool PauseVisible => pauseScreen != null && Ui.IsShown(pauseScreen);
        public bool SettingsVisible => settings != null && Ui.IsShown(settings.Root);
        public bool CompleteVisible => completeScreen != null && Ui.IsShown(completeScreen);
        public bool NextLevelAvailable => nextButton != null && Ui.IsShown(nextButton);
        public string CompleteSummaryText => completeSummary != null ? completeSummary.text : string.Empty;
        public SettingsPanel Settings => settings;

        private static GameMenus instance;

        /// <summary>Esc while the settings screen is open (and no key is being rebound) goes back; returns true if it did.</summary>
        public static bool TryCloseSettingsWithEscape()
        {
            if (instance == null || !instance.SettingsVisible) return false;
            if (instance.settings.IsRebinding) return true; // Esc cancels the rebind only
            instance.CloseSettings();
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            instance = this;
        }

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
            if (instance == this) instance = null;
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
            buttons.Add(ConfirmButton("restart", "RESTART LEVEL", LevelFlow.RestartLevel));
            buttons.Add(ConfirmButton("main-menu", "MAIN MENU", LevelFlow.LoadMainMenu));
            buttons.Add(ConfirmButton("quit", "QUIT GAME", LevelFlow.Quit));

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
            completeCard = Ui.Add(completeScreen, Ui.Element("complete-card", "br-complete"));
            completeCaption = Ui.Add(completeCard, Ui.Text("complete-caption", "Bug report closed", "br-complete__eyebrow"));
            completeTitle = Ui.Add(completeCard, Ui.Text("complete-title", HudFormat.TitleCase(levelTitle), "br-complete__title"));
            completeSubtitle = Ui.Add(completeCard, Ui.Text("complete-subtitle", "Level complete", "br-complete__subtitle"));
            starsRow = Ui.Add(completeCard, Ui.Element("complete-stars-row", "br-complete__stars"));
            for (int i = 0; i < 3; i++)
            {
                var star = Ui.Add(starsRow, Ui.Element("star-" + i, "br-star"));
                star.Add(Ui.Element("star-a", "br-star__a"));
                star.Add(Ui.Element("star-b", "br-star__b"));
                starElements[i] = star;
            }
            completeStars = Ui.Add(completeCard, Ui.Text("complete-stars", string.Empty, "br-complete__starcount"));
            statRows = Ui.Add(completeCard, Ui.Element("complete-rows", "br-complete__rows"));
            completeSummary = AddStatRow("Objectives", out rowObjectives);
            AddStatRow("Time", out rowTime);
            AddStatRow("Glitches used", out rowGlitches);
            AddStatRow("Highest alert", out rowAlert);
            AddStatRow("Filed under", out rowFiled);
            completeHint = Ui.Add(completeCard, Ui.Text("complete-hint", string.Empty, "br-complete__hint"));
            completeOutro = Ui.Add(completeCard, Ui.Text("complete-outro", string.Empty, "br-complete__outro"));
            completeStamp = Ui.Add(completeCard, Ui.Text("complete-stamp", "Closed", "br-complete__stamp"));
            var completeButtons = Ui.Add(completeCard, Ui.Element("complete-buttons", "br-complete__buttons"));
            nextButton = Ui.Add(completeButtons, MenuButton("next-level", "Next level", LoadNextLevel));
            nextButton.AddToClassList("br-button--primary");
            completeRestartButton = Ui.Add(completeButtons, MenuButton("complete-restart", "Restart", LevelFlow.RestartLevel));
            completeRestartButton.AddToClassList("br-button--secondary");
            var menuButton = Ui.Add(completeButtons, MenuButton("complete-main-menu", "Main menu", LevelFlow.LoadMainMenu));
            menuButton.AddToClassList("br-button--tertiary");

            Ui.Show(pauseScreen, false);
            Ui.Show(settings.Root, false);
            Ui.Show(completeScreen, false);
            shownState = GameState.Boot;
        }

        private Label AddStatRow(string label, out Label value)
        {
            var row = Ui.Add(statRows, Ui.Element("row-" + label.ToLowerInvariant().Replace(' ', '-'), "br-statrow"));
            row.Add(Ui.Text("row-label", label, "br-statrow__label"));
            value = Ui.Add(row, Ui.Text("row-value", string.Empty, "br-statrow__value"));
            return value;
        }

        private void Reveal(VisualElement element, int delayMs)
        {
            if (element == null) return;
            element.AddToClassList("br-fade");
            completeScreen.schedule.Execute(() => element.RemoveFromClassList("br-fade")).ExecuteLater(delayMs);
        }

        /// <summary>Fills the level-complete card and plays its reveal: title, stars one by one, stat rows, verdict stamp, buttons.</summary>
        private void FillRunCard()
        {
            var recorder = FindAnyObjectByType<LevelCompletionRecorder>();
            bool known = recorder != null && recorder.CurrentIndex() >= 0;
            int stars = known ? recorder.Stars : 0;

            Ui.Show(starsRow, known);
            Ui.Show(completeStars, known);
            Ui.Show(statRows, true);
            Ui.Show(rowTime.parent, known);
            Ui.Show(rowGlitches.parent, known);
            Ui.Show(rowAlert.parent, known);
            Ui.Show(rowFiled.parent, known);
            Ui.Show(completeHint, known);
            Ui.Show(completeOutro, known);
            Ui.Show(completeStamp, known);

            if (known)
            {
                Ui.SetText(completeStars, stars + " of 3 stars");
                Ui.SetText(rowTime, RunSummary.Time(recorder.Seconds));
                Ui.SetText(rowGlitches, recorder.GlitchUses + "  (par " + recorder.ParUses + ")");
                Ui.SetText(rowAlert, HudFormat.StageWord(recorder.MaxStage).Replace("!", string.Empty));
                Ui.SetText(rowFiled, recorder.UsedSummary());
                Ui.SetText(completeHint, RunSummary.NextStarHint(stars, recorder.ParUses));
                Ui.Show(completeHint, !string.IsNullOrEmpty(completeHint.text));
                int index = recorder.CurrentIndex();
                Ui.SetText(completeOutro, catalog != null ? catalog.Get(index).outro ?? string.Empty : string.Empty);
                Ui.Show(completeOutro, !string.IsNullOrEmpty(completeOutro.text));
                string[] verdicts = { "Works as intended", "Won't fix", "Cannot reproduce", "Closed", "Duplicate of itself" };
                Ui.SetText(completeStamp, verdicts[System.Math.Abs(index) % verdicts.Length]);
            }

            // Reveal sequence (the card is readable even if the timers are skipped: the end state is the default).
            Reveal(completeCaption, 0);
            Reveal(completeTitle, 120);
            Reveal(completeSubtitle, 240);
            for (int i = 0; i < starElements.Length; i++)
            {
                var star = starElements[i];
                star.RemoveFromClassList("br-star--on");
                star.AddToClassList("br-fade");
                int delay = 500 + i * 300;
                bool earned = i < stars;
                completeScreen.schedule.Execute(() =>
                {
                    star.RemoveFromClassList("br-fade");
                    if (earned) star.AddToClassList("br-star--on");
                }).ExecuteLater(delay);
            }
            Reveal(completeStars, 1250);
            var rows = new[] { rowObjectives, rowTime, rowGlitches, rowAlert, rowFiled };
            for (int i = 0; i < rows.Length; i++) Reveal(rows[i].parent, 1450 + i * 120);
            Reveal(completeHint, 2100);
            Reveal(completeOutro, 2250);
            completeStamp.RemoveFromClassList("br-stamp--on");
            completeStamp.AddToClassList("br-fade");
            completeScreen.schedule.Execute(() =>
            {
                completeStamp.RemoveFromClassList("br-fade");
                completeStamp.AddToClassList("br-stamp--on");
            }).ExecuteLater(2400);
        }

        private static VisualElement MakeScreen(string name)
        {
            var screen = Ui.Element(name, "br-screen");
            screen.pickingMode = PickingMode.Position; // blocks clicks to anything underneath
            return screen;
        }

        /// <summary>A button that needs a second click within three seconds (destructive actions).</summary>
        internal static Button ConfirmButton(string name, string text, System.Action onConfirmed)
        {
            Button button = null;
            bool armed = false;
            button = MenuButton(name, text, () =>
            {
                if (armed)
                {
                    onConfirmed();
                    return;
                }
                armed = true;
                button.text = "CLICK AGAIN TO CONFIRM";
                button.schedule.Execute(() =>
                {
                    armed = false;
                    button.text = text;
                }).ExecuteLater(3000);
            });
            return button;
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
                Ui.SetText(completeTitle, HudFormat.TitleCase(levelTitle));
                FillRunCard();
                bool hasNext = NextLevelScene() != null;
                Ui.Show(nextButton, hasNext);
                // A moment of read-only time so a player still clicking does not skip the card.
                var buttons = new[] { nextButton, completeRestartButton };
                foreach (var b in buttons) b.SetEnabled(false);
                completeScreen.schedule.Execute(() =>
                {
                    foreach (var b in buttons) b.SetEnabled(true);
                    Ui.FocusSoon(hasNext ? nextButton : completeRestartButton);
                }).ExecuteLater(2600);
            }
        }
    }
}
