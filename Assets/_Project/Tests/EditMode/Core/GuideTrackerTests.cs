using System.Collections.Generic;
using BugReport.Core.Tutorial;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class GuideTrackerTests
    {
        // Section 0: 0 move (latch), 1 look (state), 2 select (state), 3 apply (latch), 4 watch (never met)
        // Section 1: 5 equip (latch), 6 watch
        private static GuideTracker Make() => new GuideTracker(
            new[] { 0, 0, 0, 0, 0, 1, 1 },
            new[] { true, false, false, true, false, true, false });

        private readonly HashSet<int> met = new HashSet<int>();
        private bool IsMet(int i) => met.Contains(i);

        [Test]
        public void StartsAtTheFirstStepOfTheSection()
        {
            var g = Make();
            g.Update(0, IsMet);
            Assert.That(g.Current, Is.EqualTo(0));
            Assert.That(g.Position(), Is.EqualTo((1, 5)));
        }

        [Test]
        public void LatchingStepsStayDone_StateStepsOnlyWhileHeld()
        {
            var g = Make();
            met.Add(0);
            g.Update(0, IsMet);
            met.Clear();
            g.Update(0, IsMet);
            Assert.That(g.Current, Is.EqualTo(1), "moving once is enough");

            met.Add(2); // selected the wall (which implies having looked at it)
            g.Update(0, IsMet);
            Assert.That(g.Current, Is.EqualTo(3));

            met.Remove(2); // deselected: back to "look at it"
            g.Update(0, IsMet);
            Assert.That(g.Current, Is.EqualTo(1));
        }

        [Test]
        public void DoingALaterStepFirst_SkipsAhead_AndWaitsOnTheWatchStep()
        {
            var g = Make();
            met.Add(3);
            g.Update(0, IsMet);
            met.Clear();
            g.Update(0, IsMet);
            Assert.That(g.Current, Is.EqualTo(4), "never-met last step holds until the objective completes");
        }

        [Test]
        public void ObjectiveProgress_CompletesEarlierSections_EvenIfSkipped()
        {
            var g = Make();
            g.Update(0, IsMet);
            g.Update(1, IsMet);
            Assert.That(g.Current, Is.EqualTo(5));
            Assert.That(g.IsLatched(0) && g.IsLatched(3), Is.True);
            Assert.That(g.Position(), Is.EqualTo((1, 2)));
        }

        [Test]
        public void NoStepsLeft_WhenPastTheLastSection()
        {
            var g = Make();
            g.Update(5, IsMet);
            Assert.That(g.Current, Is.EqualTo(-1));
            Assert.That(g.Position(), Is.EqualTo((0, 0)));
        }

        [Test]
        public void AdvancedFires_OnForwardProgressAndSectionChange_NotOnRegression()
        {
            var g = Make();
            int advanced = 0;
            g.Advanced += () => advanced++;
            g.Update(0, IsMet);
            Assert.That(advanced, Is.EqualTo(0), "first update shows step 0, nothing completed yet");

            met.Add(0);
            g.Update(0, IsMet);
            Assert.That(advanced, Is.EqualTo(1));

            met.Add(2);
            g.Update(0, IsMet);
            met.Remove(2);
            g.Update(0, IsMet);
            Assert.That(advanced, Is.EqualTo(2), "walking back is not progress");

            g.Update(1, IsMet);
            Assert.That(advanced, Is.EqualTo(3));
        }

        [Test]
        public void RejectsMalformedScripts()
        {
            Assert.Throws<System.ArgumentException>(() => new GuideTracker(new[] { 1, 0 }, new[] { false, false }));
            Assert.Throws<System.ArgumentException>(() => new GuideTracker(new[] { 0 }, new[] { false, true }));
        }
    }

    public class GuideTextTests
    {
        [Test]
        public void HighlightsKeyTokens()
        {
            Assert.That(GuideText.HighlightKeys("Press [E] now", "#0FF"),
                Is.EqualTo("Press <b><color=#0FF>[E]</color></b> now"));
            Assert.That(GuideText.HighlightKeys("[Q] then [LMB]", "#0FF"),
                Is.EqualTo("<b><color=#0FF>[Q]</color></b> then <b><color=#0FF>[LMB]</color></b>"));
        }

        [Test]
        public void LeavesBrokenOrEmptyBracketsAlone()
        {
            Assert.That(GuideText.HighlightKeys("a [ b", "#0FF"), Is.EqualTo("a [ b"));
            Assert.That(GuideText.HighlightKeys("a [] b", "#0FF"), Is.EqualTo("a [] b"));
            Assert.That(GuideText.HighlightKeys("x [[E] y", "#0FF"), Is.EqualTo("x [<b><color=#0FF>[E]</color></b> y"));
            Assert.That(GuideText.HighlightKeys(null, "#0FF"), Is.Empty);
        }
    }
}
