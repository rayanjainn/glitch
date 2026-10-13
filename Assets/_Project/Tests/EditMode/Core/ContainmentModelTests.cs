using System;
using System.Collections.Generic;
using BugReport.Core.Containment;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class ContainmentModelTests
    {
        private static readonly ContainmentInputs Nothing = new ContainmentInputs(0f, 0, false);

        private static ContainmentModel Model(out List<(int, int)> changes)
        {
            var model = new ContainmentModel();
            var list = new List<(int, int)>();
            model.StageChanged += (a, b) => list.Add((a, b));
            changes = list;
            return model;
        }

        [Test]
        public void StartsCalm()
        {
            var m = new ContainmentModel();
            Assert.That(m.Stage, Is.EqualTo(0));
            Assert.That(m.Pressure, Is.EqualTo(0f));
        }

        [Test]
        public void NormalPlay_NeverEscalates()
        {
            var m = Model(out var changes);
            for (int i = 0; i < 6000; i++) m.Tick(0.016f, Nothing);
            Assert.That(m.Stage, Is.EqualTo(0));
            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void GlitchImpulses_EscalateThroughStagesInOrder()
        {
            var m = Model(out var changes);
            m.ReportGlitchApplied(2.5f); // 25 → stage 1
            Assert.That(m.Stage, Is.EqualTo(1));
            m.ReportGlitchApplied(2.5f); // 50 → stage 2
            m.ReportGlitchApplied(2.5f); // 75 → stage 3
            m.ReportGlitchApplied(2.5f); // 100 (clamped) → stage 4
            Assert.That(m.Stage, Is.EqualTo(4));
            Assert.That(m.Pressure, Is.EqualTo(100f));
            Assert.That(changes, Is.EqualTo(new[] { (0, 1), (1, 2), (2, 3), (3, 4) }));
        }

        [Test]
        public void BigJump_SkipsStagesInOneEvent()
        {
            var m = Model(out var changes);
            m.ReportGlitchApplied(5f); // 50
            Assert.That(changes, Is.EqualTo(new[] { (0, 2) }));
        }

        [Test]
        public void Decay_ReturnsToCalm()
        {
            var m = Model(out var changes);
            m.ReportGlitchApplied(5f); // 50, stage 2
            for (int i = 0; i < 100; i++) m.Tick(1f, Nothing); // -1.5/s
            Assert.That(m.Pressure, Is.EqualTo(0f));
            Assert.That(m.Stage, Is.EqualTo(0));
            Assert.That(changes[changes.Count - 1], Is.EqualTo((1, 0)));
        }

        [Test]
        public void Hysteresis_PreventsFlickerAroundThreshold()
        {
            var m = Model(out var changes);
            m.ReportGlitchApplied(2f); // 20 → stage 1
            // Hover just under the enter threshold: stays in stage 1 until below 12.
            m.Tick(1f, Nothing); // 18.5
            m.Tick(1f, Nothing); // 17
            Assert.That(m.Stage, Is.EqualTo(1));
            m.ReportGlitchApplied(0.2f); // 19 → no change either way
            Assert.That(changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void PersistentGlitches_RaisePressureOverTime()
        {
            var m = new ContainmentModel();
            var busy = new ContainmentInputs(activeGlitchWeight: 3f, productCount: 2, anomalySighted: false);
            for (int i = 0; i < 20; i++) m.Tick(1f, busy); // (3 + 1 - 1.5) = +2.5/s
            Assert.That(m.Pressure, Is.EqualTo(50f).Within(1e-3f));
            Assert.That(m.Stage, Is.EqualTo(2));
        }

        [Test]
        public void BeingSeen_AndRestrictedTargets_AddPressure()
        {
            var m = new ContainmentModel();
            m.ReportGlitchApplied(0f, restrictedTarget: true);
            Assert.That(m.Pressure, Is.EqualTo(15f));
            m.Tick(1f, new ContainmentInputs(0f, 0, anomalySighted: true));
            Assert.That(m.Pressure, Is.EqualTo(15f + 8f - 1.5f).Within(1e-4f));
        }

        [Test]
        public void Patch_RelievesPressure()
        {
            var m = new ContainmentModel();
            m.ReportGlitchApplied(7.5f); // 75, stage 3
            m.ReportPatched(); // 60 → exit threshold for stage 3 is 60, so still 3
            Assert.That(m.Stage, Is.EqualTo(3));
            m.ReportPatched(); // 45 → stage 2
            Assert.That(m.Stage, Is.EqualTo(2));
        }

        [Test]
        public void Reset_ReturnsToCalmAndRaises()
        {
            var m = Model(out var changes);
            m.ReportGlitchApplied(9f);
            m.Reset();
            Assert.That(m.Stage, Is.EqualTo(0));
            Assert.That(m.Pressure, Is.EqualTo(0f));
            Assert.That(changes[changes.Count - 1], Is.EqualTo((4, 0)));
        }

        [Test]
        public void Pressure_NeverNegative()
        {
            var m = new ContainmentModel();
            m.ReportPatched();
            m.Tick(10f, Nothing);
            Assert.That(m.Pressure, Is.EqualTo(0f));
        }

        [Test]
        public void InvalidSettings_AreRejected()
        {
            var bad = new ContainmentSettings { ExitThresholds = new[] { 30f, 35f, 60f, 80f } }; // exit >= enter at stage 1
            Assert.Throws<ArgumentException>(() => new ContainmentModel(bad));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ContainmentModel().ReportGlitchApplied(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ContainmentModel().Tick(-1f, Nothing));
        }
    }
}
