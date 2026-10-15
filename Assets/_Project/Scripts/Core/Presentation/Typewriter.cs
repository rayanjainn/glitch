using System;

namespace BugReport.Core.Presentation
{
    /// <summary>Reveals text character by character for system-log style overlays.</summary>
    public static class Typewriter
    {
        public static int VisibleCharacters(float elapsedSeconds, float charactersPerSecond, int length)
        {
            if (length <= 0) return 0;
            if (!(charactersPerSecond > 0f)) return length;
            int count = (int)Math.Floor(Math.Max(0f, elapsedSeconds) * charactersPerSecond);
            return Math.Min(length, count);
        }

        /// <summary>Seconds to type all lines plus a pause after each.</summary>
        public static float TotalSeconds(string[] lines, float charactersPerSecond, float pausePerLine)
        {
            float total = 0f;
            if (lines == null) return total;
            foreach (var line in lines)
                total += (string.IsNullOrEmpty(line) ? 0f : line.Length / Math.Max(0.01f, charactersPerSecond)) + pausePerLine;
            return total;
        }
    }
}
