using System;

namespace BugReport.Core.Presentation
{
    public enum BarkEvent { Start, Blocked, Confused, Fell, Caught, Arrived }

    /// <summary>
    /// Short lines the hero says in character (a confident knight who has no idea the game is broken).
    /// Deterministic per seed, never repeats a line twice in a row for one event, and rate-limited so the
    /// speech bubble never turns into spam.
    /// </summary>
    public sealed class HeroBarks
    {
        private static readonly string[][] Lines =
        {
            /* Start    */ new[] { "Onward!", "The exit awaits.", "A hero's work is never done.", "Let us be brisk." },
            /* Blocked  */ new[] { "Hm. I will wait until that is safe.", "Something is in my way. I shall wait.", "I won't walk into that.", "This... was not on the map." },
            /* Confused */ new[] { "The floor... is moving?", "I distinctly remember this being solid.", "Is that crate... twice?", "I shall not question it." },
            /* Fell     */ new[] { "Curious. I seem to have fallen.", "Fear not, I have done this before.", "Gravity! Of course." },
            /* Caught   */ new[] { "A guard! Back to the start.", "Halt? I was merely walking.", "Not the dungeon again." },
            /* Arrived  */ new[] { "Victory!", "As foretold.", "Another quest complete.", "Easy. Mostly." },
        };

        private readonly Random random;
        private readonly int[] last = new int[Lines.Length];
        private readonly float minGap;
        private float lastBarkTime = float.NegativeInfinity;

        public HeroBarks(int seed, float minGapSeconds = 4f)
        {
            random = new Random(seed);
            minGap = Math.Max(0f, minGapSeconds);
            for (int i = 0; i < last.Length; i++) last[i] = -1;
        }

        /// <summary>A line for the event, or null if one was spoken less than the minimum gap ago (Arrived and Caught always speak).</summary>
        public string TryBark(BarkEvent evt, float now)
        {
            bool always = evt == BarkEvent.Arrived || evt == BarkEvent.Caught;
            if (!always && now - lastBarkTime < minGap) return null;
            lastBarkTime = now;
            return Pick(evt);
        }

        public string Pick(BarkEvent evt)
        {
            int e = (int)evt;
            var set = Lines[e];
            int index = random.Next(set.Length);
            if (set.Length > 1 && index == last[e]) index = (index + 1) % set.Length;
            last[e] = index;
            return set[index];
        }
    }
}
