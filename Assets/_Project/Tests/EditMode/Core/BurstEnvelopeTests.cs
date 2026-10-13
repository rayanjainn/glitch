using System;
using BugReport.Core.Presentation;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class BurstEnvelopeTests
    {
        private static readonly BurstEnvelope E = new BurstEnvelope(0.1f, 0.2f, 0.4f);

        [Test]
        public void ZeroOutsideTheBurst()
        {
            Assert.That(E.Evaluate(-0.01f), Is.EqualTo(0f));
            Assert.That(E.Evaluate(E.Duration), Is.EqualTo(0f));
            Assert.That(E.Evaluate(10f), Is.EqualTo(0f));
        }

        [Test]
        public void RisesHoldsAndFalls()
        {
            Assert.That(E.Evaluate(0.05f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(E.Evaluate(0.2f), Is.EqualTo(1f));
            Assert.That(E.Evaluate(0.5f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(E.Evaluate(0.69f), Is.LessThan(0.01f));
        }

        [Test]
        public void ZeroLengthPhases_AreHandled()
        {
            var instant = new BurstEnvelope(0f, 0f, 1f);
            Assert.That(instant.Evaluate(0f), Is.EqualTo(1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BurstEnvelope(-1f, 0f, 0f));
        }

        [Test]
        public void Mixer_StrongestBurstWins_AndNeverExceedsOne()
        {
            var m = new BurstMixer(E, 4);
            m.Trigger(0f, 0.5f);
            m.Trigger(0f, 1f);
            m.Trigger(0f, 3f);
            Assert.That(m.Evaluate(0.2f), Is.EqualTo(1f));
            Assert.That(m.Evaluate(5f), Is.EqualTo(0f));
        }

        [Test]
        public void Mixer_Clear_StopsEverything()
        {
            var m = new BurstMixer(E);
            m.Trigger(0f);
            m.Clear();
            Assert.That(m.Evaluate(0.2f), Is.EqualTo(0f));
        }
    }
}
