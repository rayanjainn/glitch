using BugReport.Core.Progress;
using BugReport.Level;
using BugReport.Progress;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Title screen: continue, new game, level select (locked until the previous level is done),
    /// settings, credits, quit. Lives in the MainMenu scene, which has no gameplay services.
    /// </summary>
    public sealed class MainMenu : UiLayer
    {
        private const string Credits =
            "BUG//REPORT\n" +
            "Design, code, procedural art and sounds: Rayan Jain\n\n" +
            "Hero and dungeon props: KayKit by Kay Lousberg (CC0)\n" +
            "Sound effects: Kenney (CC0)\n" +
            "Lilita One font: Juan Montoreano (SIL Open Font License)\n\n" +
            "Full list with sources: CREDITS.md";

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private string tagline = "You are not the hero. You are the bug.";
        [SerializeField, OptionalReference, Tooltip("Enables key rebinding in Settings.")]
        private UnityEngine.InputSystem.InputActionAsset actions;

        private VisualElement home;
        private VisualElement levels;
        private VisualElement levelList;
        private VisualElement credits;
        private SettingsPanel settings;
        private Button continueButton;
        private Button newGameButton;

        protected override string LayerName => "main-menu";
        protected override int SortKey => 20;

        public void Configure(LevelCatalog levelCatalog) => catalog = levelCatalog;

        public bool ContinueVisible => continueButton != null && Ui.IsShown(continueButton);
        public string Showing => home != null && Ui.IsShown(home) ? "home"
            : levels != null && Ui.IsShown(levels) ? "levels"
            : settings != null && Ui.IsShown(settings.Root) ? "settings"
            : credits != null && Ui.IsShown(credits) ? "credits" : string.Empty;

        /// <summary>Level select rows (tests): name "level-{index}", disabled when locked.</summary>
        public VisualElement LevelList => levelList;

        protected override void OnEnable()
        {
            base.OnEnable();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        protected override void OnDisable()
        {
            settings?.Dispose();
            settings = null;
            base.OnDisable();
        }

        public SettingsPanel Settings => settings;

        // ---------------------------------------------------------------- actions

        public void ShowHome()
        {
            Show(home);
            UpdateContinue();
            Ui.FocusSoon(ContinueVisible ? continueButton : newGameButton);
        }

        public void ShowLevels()
        {
            BuildLevelList();
            Show(levels);
        }

        public void ShowSettings()
        {
            settings.Refresh();
            Show(settings.Root);
            settings.Focus();
        }

        public void ShowCredits() => Show(credits);

        public void Continue()
        {
            int index = catalog != null ? LevelProgress.ContinueIndex(catalog.Ids, SaveStore.Current) : -1;
            Play(index < 0 ? 0 : index);
        }

        public void Play(int index)
        {
            if (catalog == null || index < 0 || index >= catalog.Count) return;
            if (!LevelProgress.IsUnlocked(catalog.Ids, SaveStore.Current, index)) return;
            LevelFlow.LoadLevel(catalog.Get(index).sceneName);
        }

        // ---------------------------------------------------------------- build

        protected override void Build(VisualElement layer)
        {
            var screen = Ui.Add(layer, Ui.Element("menu-screen", "br-menu"));
            screen.pickingMode = PickingMode.Position;

            home = Ui.Add(screen, Ui.Element("menu-home", "br-menu__home"));
            home.Add(Ui.Text("menu-title", "BUG//REPORT", "br-menu__title", "br-display"));
            home.Add(Ui.Text("menu-tagline", tagline, "br-menu__tagline"));
            var buttons = Ui.Add(home, Ui.Element("menu-buttons", "br-card__buttons"));
            continueButton = Ui.Add(buttons, GameMenus.MenuButton("continue", "CONTINUE", Continue));
            newGameButton = Ui.Add(buttons, GameMenus.MenuButton("new-game", "NEW GAME", () => Play(0)));
            buttons.Add(GameMenus.MenuButton("level-select", "LEVEL SELECT", ShowLevels));
            buttons.Add(GameMenus.MenuButton("settings", "SETTINGS", ShowSettings));
            buttons.Add(GameMenus.MenuButton("credits", "CREDITS", ShowCredits));
            buttons.Add(GameMenus.MenuButton("quit", "QUIT", LevelFlow.Quit));

            levels = Ui.Add(screen, Ui.Element("menu-levels", "br-card", "br-menu__levels"));
            levels.pickingMode = PickingMode.Position;
            levels.Add(Ui.Text("levels-caption", "LEVEL SELECT", "br-caption"));
            levels.Add(Ui.Text("levels-title", "CHOOSE A LEVEL", "br-title", "br-display"));
            levelList = Ui.Add(levels, Ui.Element("level-list", "br-levelgrid"));
            levels.Add(GameMenus.MenuButton("levels-back", "BACK", ShowHome));

            settings?.Dispose();
            settings = new SettingsPanel(ShowHome, showFullscreen: !Application.isEditor, actions);
            screen.Add(settings.Root);

            credits = Ui.Add(screen, Ui.Element("menu-credits", "br-card"));
            credits.pickingMode = PickingMode.Position;
            credits.Add(Ui.Text("credits-caption", "THANK YOU", "br-caption"));
            credits.Add(Ui.Text("credits-title", "CREDITS", "br-title", "br-display"));
            credits.Add(Ui.Text("credits-text", Credits, "br-card__body", "br-credits"));
            credits.Add(GameMenus.MenuButton("credits-back", "BACK", ShowHome));

            // Esc goes back one page (keyboard players have no mouse for BACK).
            layer.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape || Showing == "home" || (settings != null && settings.IsRebinding)) return;
                ShowHome();
                e.StopPropagation();
            });

            ShowHome();
        }

        private void BuildLevelList()
        {
            levelList.Clear();
            if (catalog == null) return;
            var save = SaveStore.Current;
            for (int i = 0; i < catalog.Count; i++)
            {
                int index = i;
                var entry = catalog.Get(i);
                bool unlocked = LevelProgress.IsUnlocked(catalog.Ids, save, i);
                string state = !unlocked ? "  [LOCKED]" : save.IsCompleted(entry.id) ? "  " + RunSummary.Stars(save.StarsFor(entry.id)) : string.Empty;
                var button = GameMenus.MenuButton("level-" + i, (i + 1).ToString("00") + "  " + entry.title + state, () => Play(index));
                button.AddToClassList("br-chapter-" + Mathf.Clamp(entry.chapterIndex, 1, 7));
                button.tooltip = entry.chapter;
                button.SetEnabled(unlocked);
                levelList.Add(button);
            }
            // Focus the level the player most likely wants: the first open level not yet completed.
            int focus = LevelProgress.ContinueIndex(catalog.Ids, save);
            if (levelList.childCount > 0) Ui.FocusSoon((Focusable)levelList[focus >= 0 && focus < levelList.childCount ? focus : 0]);
        }

        private void UpdateContinue()
        {
            bool canContinue = catalog != null && LevelProgress.ContinueIndex(catalog.Ids, SaveStore.Current) >= 0;
            Ui.Show(continueButton, canContinue);
        }

        private void Show(VisualElement page)
        {
            foreach (var p in new[] { home, levels, settings.Root, credits }) Ui.Show(p, p == page);
        }

        protected override void Refresh() { }
    }
}
