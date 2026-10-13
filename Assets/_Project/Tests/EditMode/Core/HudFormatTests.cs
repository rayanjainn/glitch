using System.Collections.Generic;
using BugReport.Core.Presentation;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class HudFormatTests
    {
        [Test]
        public void Seconds_RoundsUp_AndHidesZero()
        {
            Assert.That(HudFormat.Seconds(0f), Is.Empty);
            Assert.That(HudFormat.Seconds(-1f), Is.Empty);
            Assert.That(HudFormat.Seconds(float.NaN), Is.Empty);
            Assert.That(HudFormat.Seconds(0.01f), Is.EqualTo("0.1s"));
            Assert.That(HudFormat.Seconds(2.35f), Is.EqualTo("2.4s"));
            Assert.That(HudFormat.Seconds(3f), Is.EqualTo("3.0s"));
        }

        [Test]
        public void Seconds_IgnoresSystemLocale()
        {
            var previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.That(HudFormat.Seconds(1.5f), Is.EqualTo("1.5s"));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void StageNames_AreClamped()
        {
            Assert.That(HudFormat.StageName(-3), Is.EqualTo("SYSTEM NOMINAL"));
            Assert.That(HudFormat.StageName(3), Is.EqualTo("PATCH DEPLOYMENT"));
            Assert.That(HudFormat.StageName(99), Is.EqualTo("EMERGENCY RECOVERY"));
            Assert.That(HudFormat.StageCount, Is.EqualTo(5));
        }

        [Test]
        public void ContainmentDetail_OnlyShowsCountdownFromPatchStage()
        {
            Assert.That(HudFormat.ContainmentDetail(2, 1.2f), Is.Empty);
            Assert.That(HudFormat.ContainmentDetail(3, 0f), Is.Empty);
            Assert.That(HudFormat.ContainmentDetail(3, 1.2f), Is.EqualTo("PATCH IN 1.2s"));
        }

        [Test]
        public void ObjectiveAndCompletionText()
        {
            Assert.That(HudFormat.ObjectiveHeader(0, 3), Is.EqualTo("OBJECTIVE 1/3"));
            StringAssert.StartsWith("3/3 OBJECTIVES", HudFormat.CompletionSummary(3, 3));
            Assert.That(HudFormat.CompletionSummary(0, 0), Is.EqualTo("HERO REACHED THE EXIT"));
        }
    }

    public class ToastQueueTests
    {
        private readonly List<Toast> visible = new List<Toast>();

        [Test]
        public void NewestFirst_AndCapacityDropsOldest()
        {
            var q = new ToastQueue(capacity: 2, lifetime: 5f, fade: 1f);
            q.Push("A", ToastTone.Neutral, 0u, 0f);
            q.Push("B", ToastTone.Neutral, 0u, 0.1f);
            q.Push("C", ToastTone.Negative, 0u, 0.2f);
            q.Visible(0.3f, visible);
            Assert.That(visible.ConvertAll(t => t.Message), Is.EqualTo(new[] { "C", "B" }));
        }

        [Test]
        public void ImmediateRepeat_CollapsesWithCount_AndRefreshesLifetime()
        {
            var q = new ToastQueue(3, 2f, 0.5f);
            q.Push("OCCUPIED", ToastTone.Negative, 0u, 0f);
            q.Push("OCCUPIED", ToastTone.Negative, 0u, 1.5f);
            q.Visible(3f, visible);
            Assert.That(visible.Count, Is.EqualTo(1));
            Assert.That(visible[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Expires_AndFadesAtTheEnd()
        {
            var q = new ToastQueue(3, 2f, 0.5f);
            q.Push("X", ToastTone.Positive, 0xFF0000FFu, 10f);
            q.Visible(10.5f, visible);
            Assert.That(q.Opacity(visible[0], 10.5f), Is.EqualTo(1f));
            Assert.That(q.Opacity(visible[0], 11.75f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(visible[0].Color, Is.EqualTo(0xFF0000FFu));
            q.Visible(12f, visible);
            Assert.That(visible, Is.Empty);
        }

        [Test]
        public void IgnoresEmptyMessages_AndValidatesArguments()
        {
            var q = new ToastQueue();
            q.Push(null, ToastTone.Neutral, 0u, 0f);
            q.Push(string.Empty, ToastTone.Neutral, 0u, 0f);
            q.Visible(0f, visible);
            Assert.That(visible, Is.Empty);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ToastQueue(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ToastQueue(2, 1f, 2f));
        }
    }
}
