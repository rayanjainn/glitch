using System;
using BugReport.Core.Progress;
using BugReport.Progress;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Settings controls shared by the main menu and the pause menu. Changes apply immediately
    /// (SaveStore.ApplySettings) and are written to disk when the panel closes.
    /// </summary>
    public sealed class SettingsPanel
    {
        private readonly Slider volume;
        private readonly Slider sensitivity;
        private readonly Toggle invertY;
        private readonly Toggle colourSplit;
        private readonly Toggle fullscreen;
        private readonly Button back;

        public VisualElement Root { get; }

        public SettingsPanel(Action onBack, bool showFullscreen)
        {
            Root = Ui.Element("settings", "br-card", "br-settings");
            Root.pickingMode = PickingMode.Position;
            Root.Add(Ui.Text("settings-caption", "OPTIONS", "br-caption"));
            Root.Add(Ui.Text("settings-title", "SETTINGS", "br-title", "br-display"));

            volume = Ui.Add(Root, new Slider("MASTER VOLUME", 0f, 1f) { name = "setting-volume" });
            sensitivity = Ui.Add(Root, new Slider("MOUSE SENSITIVITY", GameSettingsData.MinSensitivity, GameSettingsData.MaxSensitivity) { name = "setting-sensitivity" });
            invertY = Ui.Add(Root, new Toggle("INVERT LOOK UP/DOWN") { name = "setting-invert" });
            colourSplit = Ui.Add(Root, new Toggle("COLOUR-SPLIT EFFECTS") { name = "setting-colour-split" });
            fullscreen = Ui.Add(Root, new Toggle("FULLSCREEN") { name = "setting-fullscreen" });
            Ui.Show(fullscreen, showFullscreen);
            foreach (var field in new VisualElement[] { volume, sensitivity, invertY, colourSplit, fullscreen }) field.AddToClassList("br-setting");

            volume.RegisterValueChangedCallback(e => SetMasterVolume(e.newValue));
            sensitivity.RegisterValueChangedCallback(e => SetSensitivity(e.newValue));
            invertY.RegisterValueChangedCallback(e => SetInvertY(e.newValue));
            colourSplit.RegisterValueChangedCallback(e => SetColourSplit(e.newValue));
            fullscreen.RegisterValueChangedCallback(e => SetFullscreen(e.newValue));

            back = Ui.Add(Root, new Button(() =>
            {
                SaveStore.Flush();
                onBack?.Invoke();
            }) { name = "settings-back", text = "BACK" });
            back.AddToClassList("br-button");
            Refresh();
        }

        /// <summary>Copies the saved values into the controls (without triggering changes).</summary>
        public void Refresh()
        {
            var s = SaveStore.Settings;
            volume.SetValueWithoutNotify(s.masterVolume);
            sensitivity.SetValueWithoutNotify(s.mouseSensitivity);
            invertY.SetValueWithoutNotify(s.invertY);
            colourSplit.SetValueWithoutNotify(s.colourSplitEffects);
            fullscreen.SetValueWithoutNotify(s.fullscreen);
        }

        public void Focus() => Ui.FocusSoon(volume);

        // The handlers are public so tests can drive them without simulating UI events.
        public void SetMasterVolume(float value) => Change(s => s.masterVolume = value);
        public void SetSensitivity(float value) => Change(s => s.mouseSensitivity = value);
        public void SetInvertY(bool value) => Change(s => s.invertY = value);
        public void SetColourSplit(bool value) => Change(s => s.colourSplitEffects = value);
        public void SetFullscreen(bool value) => Change(s => s.fullscreen = value);

        private static void Change(Action<GameSettingsData> apply)
        {
            apply(SaveStore.Settings);
            SaveStore.ApplySettings();
        }
    }
}
