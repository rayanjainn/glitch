namespace BugReport.Core.Audio
{
    /// <summary>
    /// What a stretch of the game sounds like: key, mode, tempo and chord loop. Themes are data; the
    /// <see cref="MusicEngine"/> turns them into endless generative music, so there is nothing to
    /// license and nothing to loop.
    /// </summary>
    public sealed class MusicTheme
    {
        public string Name;
        public int RootMidi = 45;
        /// <summary>Semitone offsets of the scale's degrees from the root.</summary>
        public int[] Scale = { 0, 2, 3, 5, 7, 8, 10 };
        /// <summary>Scale degree of each bar's chord root, one entry per bar, looped.</summary>
        public int[] Progression = { 0, 5, 3, 4 };
        public float Bpm = 92f;
        /// <summary>Added to the chord root's degree for each arpeggio step, looped.</summary>
        public int[] ArpPattern = { 0, 2, 4, 2, 7, 4, 2, 4 };
        /// <summary>Upper harmonics in the pad (0 = soft sine, 1 = bright).</summary>
        public float Brightness = 0.4f;
        public float PadLevel = 1f;
    }

    public static class MusicThemes
    {
        /// <summary>Title screen: slow, wide, a little wistful.</summary>
        public static MusicTheme Menu() => new MusicTheme
        {
            Name = "menu", RootMidi = 45, Bpm = 72f, Brightness = 0.25f, PadLevel = 1.2f,
            Scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, Progression = new[] { 0, 5, 3, 6 }, ArpPattern = new[] { 0, 2, 4, 7, 4, 2 },
        };

        /// <summary>Chapters 1-2: clean lab, bright and curious.</summary>
        public static MusicTheme Lab() => new MusicTheme
        {
            Name = "lab", RootMidi = 48, Bpm = 98f, Brightness = 0.35f,
            Scale = new[] { 0, 2, 4, 7, 9, 12, 14 }, Progression = new[] { 0, 3, 4, 3 }, ArpPattern = new[] { 0, 2, 4, 2, 5, 4, 2, 1 },
        };

        /// <summary>Chapter 3: machinery, steady and driving.</summary>
        public static MusicTheme Industrial() => new MusicTheme
        {
            Name = "industrial", RootMidi = 43, Bpm = 112f, Brightness = 0.55f,
            Scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, Progression = new[] { 0, 0, 5, 6 }, ArpPattern = new[] { 0, 0, 4, 0, 7, 0, 4, 2 },
        };

        /// <summary>Chapters 4-5: a test chamber, a security facility. Cool and watchful.</summary>
        public static MusicTheme Facility() => new MusicTheme
        {
            Name = "facility", RootMidi = 46, Bpm = 104f, Brightness = 0.5f,
            Scale = new[] { 0, 1, 3, 5, 7, 8, 10 }, Progression = new[] { 0, 5, 1, 4 }, ArpPattern = new[] { 0, 3, 5, 3, 7, 5, 3, 1 },
        };

        /// <summary>Chapter 6: patch day. Tense and unstable.</summary>
        public static MusicTheme Corrupted() => new MusicTheme
        {
            Name = "corrupted", RootMidi = 42, Bpm = 120f, Brightness = 0.7f,
            Scale = new[] { 0, 1, 3, 5, 6, 8, 10 }, Progression = new[] { 0, 1, 0, 4 }, ArpPattern = new[] { 0, 4, 1, 5, 0, 6, 2, 5 },
        };

        /// <summary>Chapter 7 and the finale: everything at once.</summary>
        public static MusicTheme Finale() => new MusicTheme
        {
            Name = "finale", RootMidi = 45, Bpm = 126f, Brightness = 0.65f, PadLevel = 1.1f,
            Scale = new[] { 0, 2, 3, 5, 7, 8, 11 }, Progression = new[] { 0, 5, 2, 6, 3, 5, 4, 0 }, ArpPattern = new[] { 0, 2, 4, 7, 9, 7, 4, 2 },
        };

        public static MusicTheme ForChapter(int chapter)
        {
            switch (chapter)
            {
                case 0: return Menu();
                case 1:
                case 2: return Lab();
                case 3: return Industrial();
                case 4:
                case 5: return Facility();
                case 6: return Corrupted();
                default: return Finale();
            }
        }
    }
}
