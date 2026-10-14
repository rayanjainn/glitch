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

        private void OnDisable()
        {
            SaveStore.SettingsChanged -= Apply;
            SaveStore.Flush();
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
#if !UNITY_EDITOR
            if (Screen.fullScreen != s.fullscreen) Screen.fullScreen = s.fullscreen;
#endif
        }
    }
}
