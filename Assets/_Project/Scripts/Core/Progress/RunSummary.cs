using System;

namespace BugReport.Core.Progress
{
    /// <summary>Text for the level-complete "bug report" card. Pure formatting.</summary>
    public static class RunSummary
    {
        public static string Time(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f || seconds >= 5999f) seconds = seconds >= 5999f ? 5999f : 0f;
            int total = (int)seconds;
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        /// <summary>"★★☆" style string; clamps to 0..3.</summary>
        public static string Stars(int stars)
        {
            stars = Math.Max(0, Math.Min(3, stars));
            return new string('★', stars) + new string('☆', 3 - stars);
        }

        /// <summary>One line explaining what the next star needs, or empty at 3 stars.</summary>
        public static string NextStarHint(int stars, int parUses)
        {
            if (stars <= 1) return "2 STARS: FINISH USING " + parUses + " GLITCHES OR FEWER";
            if (stars == 2) return "3 STARS: ALSO STAY BELOW CONTAINMENT STAGE 2";
            return string.Empty;
        }
    }
}
