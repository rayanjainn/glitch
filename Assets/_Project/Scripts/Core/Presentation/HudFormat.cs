using System;
using System.Globalization;

namespace BugReport.Core.Presentation
{
    /// <summary>
    /// Player-facing HUD strings, kept in one engine-free place so wording and number
    /// formatting are consistent and testable. Numbers use the invariant culture so the HUD
    /// reads the same on every system locale.
    /// </summary>
    public static class HudFormat
    {
        private static readonly string[] StageNames =
        {
            "SYSTEM NOMINAL", "ANOMALY DETECTED", "LOCAL INVESTIGATION", "PATCH DEPLOYMENT", "EMERGENCY RECOVERY",
        };

        /// <summary>Containment stage the director starts patching at (ContainmentDirector).</summary>
        public const int PatchStage = 3;

        public static int StageCount => StageNames.Length;

        public static string StageName(int stage) => StageNames[Math.Max(0, Math.Min(stage, StageNames.Length - 1))];

        /// <summary>"OBJECTIVE 2/3" for the zero-based <paramref name="index"/>.</summary>
        public static string ObjectiveHeader(int index, int count) => "OBJECTIVE " + (index + 1) + "/" + count;

        /// <summary>"2.4s", or empty when nothing is left. Rounds up so "0.0s" is never shown.</summary>
        public static string Seconds(float seconds)
        {
            if (!(seconds > 0f)) return string.Empty;
            double tenths = Math.Ceiling(seconds * 10.0 - 1e-4);
            return (tenths / 10.0).ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

        /// <summary>Second banner line: patch countdown from stage 3, otherwise empty.</summary>
        public static string ContainmentDetail(int stage, float nextPatchIn)
            => stage >= PatchStage && nextPatchIn > 0f ? "PATCH IN " + Seconds(nextPatchIn) : string.Empty;

        /// <summary>"3/3 OBJECTIVES" style summary for the completion screen.</summary>
        public static string CompletionSummary(int completed, int total)
            => total > 0 ? completed + "/" + total + " OBJECTIVES  //  HERO REACHED THE EXIT" : "HERO REACHED THE EXIT";
    }
}
