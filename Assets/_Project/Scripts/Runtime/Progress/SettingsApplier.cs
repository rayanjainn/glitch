using BugReport.Anomaly;
using UnityEngine;

namespace BugReport.Progress
{
    /// <summary>
    /// Applies the saved settings to this scene: master volume, mouse sensitivity and invert Y on
    /// the camera rig, fullscreen (builds only). Re-applies whenever settings change and writes the
    /// save on quit.
    /// </summary>
    public sealed class SettingsApplier : MonoBehaviour
    {
        [SerializeField, OptionalReference] private AnomalyCameraRig cameraRig;

        public void Configure(AnomalyCameraRig rig) => cameraRig = rig;

        private void OnEnable()
        {
            SaveStore.SettingsChanged += Apply;
            Apply();
        }

        private void Start() => Apply(); // UI documents may not exist yet in OnEnable

        private void OnDisable()
        {
            SaveStore.SettingsChanged -= Apply;
            SaveStore.Flush();
        }

        private readonly System.Collections.Generic.List<UnityEngine.UIElements.UIDocument> documents = new System.Collections.Generic.List<UnityEngine.UIElements.UIDocument>();
        private readonly System.Collections.Generic.Dictionary<UnityEngine.UIElements.PanelSettings, UnityEngine.UIElements.PanelSettings> cloneOf = new System.Collections.Generic.Dictionary<UnityEngine.UIElements.PanelSettings, UnityEngine.UIElements.PanelSettings>();
        private readonly System.Collections.Generic.HashSet<UnityEngine.UIElements.PanelSettings> clones = new System.Collections.Generic.HashSet<UnityEngine.UIElements.PanelSettings>();

        /// <summary>UI size: a smaller reference resolution makes every panel larger. Works on a runtime copy of the panel settings.</summary>
        private void ApplyUiScale(float scale)
        {
            documents.Clear();
            documents.AddRange(FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None));
            foreach (var document in documents)
            {
                var panel = document.panelSettings;
                if (panel == null) continue;
                if (!clones.Contains(panel))
                {
                    // One copy per original, so documents that shared a panel still share one (never edit the project asset).
                    if (!cloneOf.TryGetValue(panel, out var copy))
                    {
                        copy = Instantiate(panel);
                        cloneOf[panel] = copy;
                        clones.Add(copy);
                    }
                    panel = copy;
                    document.panelSettings = panel;
                }
                panel.referenceResolution = new Vector2Int(Mathf.RoundToInt(1920f / scale), Mathf.RoundToInt(1080f / scale));
            }
        }

        private void OnDestroy()
        {
            foreach (var clone in clones) if (clone != null) Destroy(clone);
            clones.Clear();
            cloneOf.Clear();
        }

        private void OnApplicationQuit() => SaveStore.Flush();

        public void Apply()
        {
            var s = SaveStore.Settings;
            AudioListener.volume = s.masterVolume;
            if (cameraRig != null)
            {
                cameraRig.SensitivityScale = s.mouseSensitivity;
                cameraRig.InvertY = s.invertY;
            }
            ApplyUiScale(s.uiScale);
#if !UNITY_EDITOR
            if (Screen.fullScreen != s.fullscreen) Screen.fullScreen = s.fullscreen;
#endif
        }
    }
}
