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

        private static readonly string[] StageWords = { "System calm", "System noticed", "System worried", "Patching!", "Emergency!" };
        private static readonly string[] StageHints =
        {
            "Every glitch you use raises the alert.",
            "Keep glitches few. R undoes one.",
            "Undo a glitch (R) to calm it down.",
            "The system is undoing your glitches.",
            "Patching constantly. Undo glitches now.",
        };

        /// <summary>Short player-facing name of a containment stage ("System noticed").</summary>
        public static string StageWord(int stage) => StageWords[Math.Max(0, Math.Min(stage, StageWords.Length - 1))];

        /// <summary>One plain sentence about what the stage means and what to do.</summary>
        public static string StageHint(int stage) => StageHints[Math.Max(0, Math.Min(stage, StageHints.Length - 1))];

        /// <summary>"Patch in 4.2s: your last glitch gets undone", or the stage hint when no patch is counting down.</summary>
        public static string AlertLine(int stage, float nextPatchIn)
            => stage >= PatchStage && nextPatchIn > 0f ? "Patch in " + Seconds(nextPatchIn) + ": your last glitch gets undone" : StageHint(stage);

        /// <summary>"GET THE HERO ACROSS" becomes "Get the hero across": only the first letter of each sentence stays upper-case.</summary>
        public static string SentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var chars = text.ToLowerInvariant().ToCharArray();
            bool start = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (start && char.IsLetter(chars[i]))
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                    start = false;
                }
                else if (chars[i] == '.' || chars[i] == '!' || chars[i] == '?') start = true;
            }
            return new string(chars);
        }

        /// <summary>"TRAINING LAB" becomes "Training Lab".</summary>
        public static string TitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var chars = text.ToLowerInvariant().ToCharArray();
            bool start = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetter(chars[i]))
                {
                    if (start) chars[i] = char.ToUpperInvariant(chars[i]);
                    start = false;
                }
                else start = true;
            }
            return new string(chars);
        }

        /// <summary>Containment stage the director starts patching at (ContainmentDirector).</summary>
        public const int PatchStage = 3;

        public static int StageCount => StageNames.Length;

        public static string StageName(int stage) => StageNames[Math.Max(0, Math.Min(stage, StageNames.Length - 1))];

        /// <summary>"OBJECTIVE 2/3" for the zero-based <paramref name="index"/>.</summary>
        public static string ObjectiveHeader(int index, int count) => "Objective " + (index + 1) + " of " + count;

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

        /// <summary>"3 of 3" for the completion card's objectives row.</summary>
        public static string CompletionSummary(int completed, int total)
            => total > 0 ? completed + " of " + total : "Done";
    }
}
