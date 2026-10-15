using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// One place to switch each presentation feature on/off without touching code. Used for
    /// quick rollback when a visual doesn't work out, and for comparisons while polishing.
    /// A missing asset means "everything on".
    /// </summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Visual Settings", fileName = "VS_Default")]
    public sealed class VisualSettings : ScriptableObject
    {
        [Tooltip("Floating name tags and capability dots in inspection mode (Tab), plus the selected-object panel.")]
        public bool inspectionLabels = true;
        [Tooltip("Hero body language: bob, lean, confused wobble, arrival hop, state icon.")]
        public bool heroAnimation = true;
        [Tooltip("Anomaly eye follows the aimed target; light trail.")]
        public bool anomalyEye = true;
        [Tooltip("Bloom, tone mapping, vignette, colour grading on the camera.")]
        public bool postProcessing = true;
        [Tooltip("Short colour-split / lens-warp bursts when glitches fire, red vignette pulse on patches.")]
        public bool glitchCameraBursts = true;
        [Tooltip("Collapsing outline flash on copies and frozen objects.")]
        public bool spawnFlashes = true;
        [Tooltip("Dark panels behind HUD text.")]
        public bool hudPanels = true;

        public static bool On(VisualSettings settings, System.Func<VisualSettings, bool> feature) => settings == null || feature(settings);
    }

    /// <summary>Player comfort preferences, stored in the save file's settings (see SaveStore).</summary>
    public static class ComfortSettings
    {
        /// <summary>Turns off flashing UI and the patch vignette pulse.</summary>
        public static bool ReducedFlashes => Progress.SaveStore.Settings.reducedFlashes;

        /// <summary>Colour-split and lens-warp bursts. Some players find them uncomfortable (GDD 9.4).</summary>
        public static bool ChromaticEffects
        {
            get => Progress.SaveStore.Settings.colourSplitEffects;
            set
            {
                if (Progress.SaveStore.Settings.colourSplitEffects == value) return;
                Progress.SaveStore.Settings.colourSplitEffects = value;
                Progress.SaveStore.ApplySettings();
                Progress.SaveStore.Flush();
            }
        }
    }
}
