using System;
using System.Collections.Generic;
using BugReport.Core.Objectives;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class ObjectiveTrackerTests
    {
        private static ObjectiveTracker Three() => new ObjectiveTracker(new[] { "firewall", "gate", "piston" });

        [Test]
        public void StartsWithFirstActive()
        {
            var t = Three();
            Assert.That(t.CurrentIndex, Is.EqualTo(0));
            Assert.That(t.Status(0), Is.EqualTo(ObjectiveStatus.Active));
            Assert.That(t.Status(1), Is.EqualTo(ObjectiveStatus.Pending));
            Assert.That(t.AllComplete, Is.False);
        }

        [Test]
        public void CompletingActive_ActivatesNext_InOrder()
        {
            var t = Three();
            var log = new List<string>();
            t.Completed += i => log.Add("done " + i);
            t.Activated += i => log.Add("active " + i);

            t.Report(0, true);

            Assert.That(log, Is.EqualTo(new[] { "done 0", "active 1" }));
            Assert.That(t.CurrentIndex, Is.EqualTo(1));
        }

        [Test]
        public void ReportsForInactiveObjectives_AreIgnored()
        {
            var t = Three();
            t.Report(2, true);
            t.Report(0, false);
            Assert.That(t.CurrentIndex, Is.EqualTo(0));
            Assert.That(t.CompletedCount, Is.EqualTo(0));
        }

        [Test]
        public void Completion_Latches()
        {
            var t = Three();
            t.Report(0, true);
            t.Report(0, false);
            Assert.That(t.Status(0), Is.EqualTo(ObjectiveStatus.Complete));
        }

        [Test]
        public void LastObjective_RaisesAllCompletedOnce()
        {
            var t = Three();
            int all = 0;
            t.AllCompleted += () => all++;
            t.Report(0, true);
            t.Report(1, true);
            t.Report(2, true);
            t.Evaluate(_ => true);
            Assert.That(t.AllComplete, Is.True);
            Assert.That(t.CurrentIndex, Is.EqualTo(-1));
            Assert.That(all, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_CascadesThroughAlreadyMetConditions()
        {
            // An alternative solution can satisfy a later condition early (e.g. the hero is already
            // past the gate): evaluating completes everything that holds, in order.
            var t = Three();
            var met = new[] { true, true, false };
            t.Evaluate(i => met[i]);
            Assert.That(t.CurrentIndex, Is.EqualTo(2));
            Assert.That(t.CompletedCount, Is.EqualTo(2));
        }

        [Test]
        public void Reset_StartsOver()
        {
            var t = Three();
            t.Evaluate(_ => true);
            t.Reset();
            Assert.That(t.CurrentIndex, Is.EqualTo(0));
            Assert.That(t.CompletedCount, Is.EqualTo(0));
            Assert.That(t.Status(2), Is.EqualTo(ObjectiveStatus.Pending));
        }

        [Test]
        public void InvalidDefinitions_Throw()
        {
            Assert.Throws<ArgumentException>(() => new ObjectiveTracker(new string[0]));
            Assert.Throws<ArgumentException>(() => new ObjectiveTracker(new[] { "a", "a" }));
            Assert.Throws<ArgumentException>(() => new ObjectiveTracker(new[] { "a", "" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Three().Report(5, true));
        }
    }
}
