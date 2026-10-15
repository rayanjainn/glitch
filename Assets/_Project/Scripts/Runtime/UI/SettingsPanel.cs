using System;
using System.Collections.Generic;
using BugReport.Core.Progress;
using BugReport.InputHandling;
using BugReport.Progress;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Settings controls shared by the main menu and the pause menu. Changes apply immediately
    /// (SaveStore.ApplySettings) and are written to disk when the panel closes. With an actions asset,
    /// a second column lists the rebindable keys: click one, press the new key (Esc cancels).
    /// </summary>
    public sealed class SettingsPanel : IDisposable
    {
        private readonly InputActionAsset bindings;
        private readonly List<Button> keyButtons = new List<Button>();
        private InputActionRebindingExtensions.RebindingOperation rebinding;

        private readonly Slider volume;
        private readonly Slider music;
        private readonly Slider sensitivity;
        private readonly Toggle invertY;
        private readonly Toggle colourSplit;
        private readonly Toggle reducedFlashes;
        private readonly Slider uiScale;
        private readonly Toggle fullscreen;
        private readonly Button back;

        public VisualElement Root { get; }

        public SettingsPanel(Action onBack, bool showFullscreen, InputActionAsset actions = null)
        {
            Root = Ui.Element("settings", "br-card", "br-settings");
            Root.pickingMode = PickingMode.Position;
            Root.Add(Ui.Text("settings-caption", "OPTIONS", "br-caption"));
            Root.Add(Ui.Text("settings-title", "SETTINGS", "br-title", "br-display"));
            var columns = Ui.Add(Root, Ui.Element("settings-columns", "br-card__columns"));
            var general = Ui.Add(columns, Ui.Element("settings-general", "br-settings__column"));

            volume = Ui.Add(general, new Slider("MASTER VOLUME", 0f, 1f) { name = "setting-volume" });
            music = Ui.Add(general, new Slider("MUSIC VOLUME", 0f, 1f) { name = "setting-music" });
            sensitivity = Ui.Add(general, new Slider("MOUSE SENSITIVITY", GameSettingsData.MinSensitivity, GameSettingsData.MaxSensitivity) { name = "setting-sensitivity" });
            invertY = Ui.Add(general, new Toggle("INVERT LOOK UP/DOWN") { name = "setting-invert" });
            colourSplit = Ui.Add(general, new Toggle("SCREEN-WARP BURSTS (COLOUR SPLIT, LENS WARP, ZOOM PUNCH)") { name = "setting-colour-split" });
            reducedFlashes = Ui.Add(general, new Toggle("REDUCED FLASHING (ALERT FLASH, PATCH PULSE)") { name = "setting-reduced-flashes" });
            uiScale = Ui.Add(general, new Slider("UI SIZE", GameSettingsData.MinUiScale, GameSettingsData.MaxUiScale) { name = "setting-ui-scale" });
            fullscreen = Ui.Add(general, new Toggle("FULLSCREEN") { name = "setting-fullscreen" });
            Ui.Show(fullscreen, showFullscreen);
            foreach (var field in new VisualElement[] { volume, music, sensitivity, invertY, colourSplit, reducedFlashes, uiScale, fullscreen }) field.AddToClassList("br-setting");

            volume.RegisterValueChangedCallback(e => SetMasterVolume(e.newValue));
            music.RegisterValueChangedCallback(e => SetMusicVolume(e.newValue));
            sensitivity.RegisterValueChangedCallback(e => SetSensitivity(e.newValue));
            invertY.RegisterValueChangedCallback(e => SetInvertY(e.newValue));
            colourSplit.RegisterValueChangedCallback(e => SetColourSplit(e.newValue));
            reducedFlashes.RegisterValueChangedCallback(e => SetReducedFlashes(e.newValue));
            uiScale.RegisterValueChangedCallback(e => SetUiScale(e.newValue));
            fullscreen.RegisterValueChangedCallback(e => SetFullscreen(e.newValue));

            if (actions != null)
            {
                bindings = UnityEngine.Object.Instantiate(actions);
                BindingOverrides.ApplyTo(bindings);
                BuildControls(Ui.Add(columns, Ui.Element("settings-controls", "br-settings__column", "br-controls")));
            }

            back = Ui.Add(Root, new Button(() =>
            {
                CancelRebinding();
                SaveStore.Flush();
                onBack?.Invoke();
            }) { name = "settings-back", text = "BACK" });
            back.AddToClassList("br-button");
            Refresh();
        }

        private void BuildControls(VisualElement column)
        {
            column.Add(Ui.Text("controls-caption", "KEYS  (CLICK, THEN PRESS A KEY; ESC CANCELS)", "br-caption"));
            for (int i = 0; i < BindingOverrides.Rebindables.Length; i++)
            {
                int index = i;
                var row = Ui.Add(column, Ui.Element("rebind-row-" + i, "br-controls__row"));
                row.pickingMode = PickingMode.Position;
                var key = new Button(() => StartRebinding(index)) { name = "rebind-" + i };
                key.AddToClassList("br-key-button");
                row.Add(key);
                row.Add(Ui.Text("rebind-label-" + i, BindingOverrides.Rebindables[i].Label, "br-controls__action"));
                keyButtons.Add(key);
            }
            var reset = new Button(ResetControls) { name = "rebind-reset", text = "RESET KEYS" };
            reset.AddToClassList("br-button");
            column.Add(reset);
            RefreshKeys();
        }

        /// <summary>Current key shown for rebindable <paramref name="index"/> (tests, pause screen).</summary>
        public string KeyText(int index) => bindings != null ? BindingOverrides.KeyLabel(bindings, BindingOverrides.Rebindables[index]) : string.Empty;

        public bool IsRebinding => rebinding != null;

        private void StartRebinding(int index)
        {
            if (bindings == null) return;
            var r = BindingOverrides.Rebindables[index];
            var action = BindingOverrides.FindAction(bindings, r);
            int bindingIndex = BindingOverrides.BindingIndex(action, r);
            if (bindingIndex < 0) return;
            CancelRebinding();
            string oldPath = BindingOverrides.PathOf(bindings, index);
            keyButtons[index].text = "PRESS A KEY...";
            rebinding = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    op.Dispose();
                    rebinding = null;
                    BindingOverrides.ResolveConflict(bindings, index, oldPath);
                    BindingOverrides.SaveFrom(bindings);
                    RefreshKeys();
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    rebinding = null;
                    RefreshKeys();
                })
                .Start();
        }

        public void ResetControls()
        {
            CancelRebinding();
            BindingOverrides.ResetAll(bindings);
            RefreshKeys();
        }

        private void CancelRebinding()
        {
            if (rebinding == null) return;
            var op = rebinding;
            rebinding = null;
            op.Cancel();
        }

        private void RefreshKeys()
        {
            for (int i = 0; i < keyButtons.Count; i++) keyButtons[i].text = KeyText(i);
        }

        public void Dispose()
        {
            CancelRebinding();
            if (bindings != null) UnityEngine.Object.Destroy(bindings);
        }

        /// <summary>Copies the saved values into the controls (without triggering changes).</summary>
        public void Refresh()
        {
            if (bindings != null)
            {
                BindingOverrides.ApplyTo(bindings);
                RefreshKeys();
            }
            var s = SaveStore.Settings;
            volume.SetValueWithoutNotify(s.masterVolume);
            music.SetValueWithoutNotify(s.musicVolume);
            sensitivity.SetValueWithoutNotify(s.mouseSensitivity);
            invertY.SetValueWithoutNotify(s.invertY);
            colourSplit.SetValueWithoutNotify(s.colourSplitEffects);
            reducedFlashes.SetValueWithoutNotify(s.reducedFlashes);
            uiScale.SetValueWithoutNotify(s.uiScale);
            fullscreen.SetValueWithoutNotify(s.fullscreen);
        }

        public void Focus() => Ui.FocusSoon(volume);

        // The handlers are public so tests can drive them without simulating UI events.
        public void SetMasterVolume(float value) => Change(s => s.masterVolume = value);
        public void SetMusicVolume(float value) => Change(s => s.musicVolume = value);
        public void SetSensitivity(float value) => Change(s => s.mouseSensitivity = value);
        public void SetInvertY(bool value) => Change(s => s.invertY = value);
        public void SetReducedFlashes(bool value) => Change(s => s.reducedFlashes = value);
        public void SetUiScale(float value) => Change(s => s.uiScale = value);
        public void SetColourSplit(bool value) => Change(s => s.colourSplitEffects = value);
        public void SetFullscreen(bool value) => Change(s => s.fullscreen = value);

        private static void Change(Action<GameSettingsData> apply)
        {
            apply(SaveStore.Settings);
            SaveStore.ApplySettings();
        }
    }
}
