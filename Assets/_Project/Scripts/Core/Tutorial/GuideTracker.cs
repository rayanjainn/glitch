using System;

namespace BugReport.Core.Tutorial
{
    /// <summary>
    /// Step-by-step guidance inside the current objective ("section"). Never blocks progress:
    /// objectives advance on their own, and every step of an earlier section counts as done.
    ///
    /// Within the current section the guide shows the step after the furthest one satisfied:
    /// latching steps stay done once met (an action happened); state steps count only while met
    /// (something is selected or aimed at). So deselecting walks the guide back, and doing a later
    /// step first skips ahead. A section's last step is usually a never-met "watch the hero" step.
    /// </summary>
    public sealed class GuideTracker
    {
        private readonly int[] sections;
        private readonly bool[] latching;
        private readonly bool[] latched;

        public GuideTracker(int[] stepSections, bool[] stepLatching)
        {
            if (stepSections == null || stepLatching == null) throw new ArgumentNullException(nameof(stepSections));
            if (stepSections.Length != stepLatching.Length) throw new ArgumentException("One latching flag per step.");
            for (int i = 1; i < stepSections.Length; i++)
                if (stepSections[i] < stepSections[i - 1]) throw new ArgumentException("Steps must be ordered by section.");
            sections = (int[])stepSections.Clone();
            latching = (bool[])stepLatching.Clone();
            latched = new bool[sections.Length];
            Current = -1;
        }

        public int Count => sections.Length;
        /// <summary>Index of the step to show, or -1 when the current section has none left.</summary>
        public int Current { get; private set; }
        public int Section { get; private set; } = -1;

        /// <summary>Raised when the shown step moves forward (step done) or the section changes.</summary>
        public event Action Advanced;

        public int SectionOf(int step) => sections[step];

        /// <summary>1-based position of <see cref="Current"/> within its section, and the section's step count.</summary>
        public (int number, int total) Position()
        {
            if (Current < 0) return (0, 0);
            int first = Current, last = Current;
            while (first > 0 && sections[first - 1] == sections[Current]) first--;
            while (last < sections.Length - 1 && sections[last + 1] == sections[Current]) last++;
            return (Current - first + 1, last - first + 1);
        }

        /// <param name="section">The current objective index (objectives done so far).</param>
        /// <param name="isMet">Whether step i's condition holds right now.</param>
        public void Update(int section, Func<int, bool> isMet)
        {
            if (isMet == null) throw new ArgumentNullException(nameof(isMet));
            int previous = Current;
            int previousSection = Section;
            Section = section;

            int first = -1, furthest = -1, lastInSection = -1;
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] < section)
                {
                    latched[i] = true;
                    continue;
                }
                if (sections[i] > section) break;
                if (first < 0) first = i;
                lastInSection = i;

                bool met = isMet(i);
                if (latching[i] && met) latched[i] = true;
                if (latched[i] || (!latching[i] && met)) furthest = i;
            }

            if (first < 0) Current = -1;
            else if (furthest < 0) Current = first;
            else Current = furthest < lastInSection ? furthest + 1 : -1;

            bool sectionChanged = previousSection >= 0 && section != previousSection;
            if (sectionChanged || IsForward(previous, Current, section == previousSection)) Advanced?.Invoke();
        }

        private static bool IsForward(int previous, int current, bool sameSection)
            => sameSection && previous >= 0 && (current < 0 || current > previous);

        public bool IsLatched(int step) => latched[step];
    }
}
