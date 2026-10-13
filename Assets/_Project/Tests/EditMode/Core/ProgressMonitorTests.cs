using System;
using BugReport.Core.AI;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class ProgressMonitorTests
    {
        [Test]
        public void SteadyProgress_NeverStuck()
        {
            var m = new ProgressMonitor(minProgress: 0.1f, timeout: 1f);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(m.Tick(100f - i, 0.5f), Is.False);
            }
        }

        [Test]
        public void NoProgress_BecomesStuckAfterTimeout()
        {
            var m = new ProgressMonitor(0.1f, 1f);
            m.Tick(10f, 0.1f); // first sample establishes baseline
            Assert.That(m.Tick(10f, 0.5f), Is.False);
            Assert.That(m.Tick(9.95f, 0.4f), Is.False, "less than MinProgress does not count");
            Assert.That(m.Tick(10f, 0.1f), Is.True);
            Assert.That(m.IsStuck, Is.True);
        }

        [Test]
        public void MovingAway_CountsAsNoProgress()
        {
            var m = new ProgressMonitor(0.1f, 1f);
            m.Tick(5f, 0f);
            Assert.That(m.Tick(6f, 1f), Is.True);
        }

        [Test]
        public void ProgressAfterStall_ResetsTimer()
        {
            var m = new ProgressMonitor(0.1f, 1f);
            m.Tick(10f, 0f);
            m.Tick(10f, 0.9f);
            m.Tick(9f, 0.1f);
            Assert.That(m.StalledTime, Is.EqualTo(0f));
            Assert.That(m.IsStuck, Is.False);
        }

        [Test]
        public void Reset_ClearsStuckState()
        {
            var m = new ProgressMonitor(0.1f, 0.5f);
            m.Tick(3f, 0f);
            m.Tick(3f, 1f);
            Assert.That(m.IsStuck, Is.True);

            m.Reset();

            Assert.That(m.IsStuck, Is.False);
            Assert.That(m.Tick(3f, 0.1f), Is.False, "first tick after reset is a new baseline");
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressMonitor(0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressMonitor(0.1f, 0f));
            var m = new ProgressMonitor(0.1f, 1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Tick(1f, -0.1f));
            Assert.Throws<ArgumentException>(() => m.Tick(float.NaN, 0.1f));
        }
    }
}
