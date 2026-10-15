using System;
using BugReport.Core.AI;
using BugReport.Core.Audio;
using BugReport.Core.Presentation;
using BugReport.Core.Progress;
using BugReport.Core.Properties;
using BugReport.Core.Tutorial;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class StarAndRecordTests
    {
        [Test]
        public void Stars_FinishedParAndQuiet()
        {
            Assert.That(StarRating.Compute(9, 4, 0), Is.EqualTo(1));
            Assert.That(StarRating.Compute(4, 4, 3), Is.EqualTo(2));
            Assert.That(StarRating.Compute(3, 4, 1), Is.EqualTo(3));
            Assert.That(StarRating.Compute(1, 0, 0), Is.EqualTo(1), "no par set: only the finish counts");
        }

        [Test]
        public void SubmitRun_KeepsTheBest_AndMarksCompleted()
        {
            var save = new SaveData();
            Assert.That(save.SubmitRun("a", 90f, 6, 2, 4), Is.EqualTo(1));
            Assert.That(save.IsCompleted("a"), Is.True);
            Assert.That(save.SubmitRun("a", 70f, 3, 0, 4), Is.EqualTo(3));
            Assert.That(save.SubmitRun("a", 200f, 9, 4, 4), Is.EqualTo(1), "a worse run returns its own stars...");
            var r = save.RecordFor("a");
            Assert.That(r.stars, Is.EqualTo(3), "...but never lowers the best");
            Assert.That(r.bestSeconds, Is.EqualTo(70f));
            Assert.That(r.fewestUses, Is.EqualTo(3));
            Assert.That(r.plays, Is.EqualTo(3));
            Assert.That(save.StarsFor("a"), Is.EqualTo(3));
            Assert.That(save.StarsFor("unknown"), Is.EqualTo(0));
        }

        [Test]
        public void Sanitize_RepairsRecords()
        {
            var save = new SaveData();
            save.records.Add(null);
            save.records.Add(new LevelRecord { id = "", stars = 2 });
            save.records.Add(new LevelRecord { id = "x", stars = 9, bestSeconds = float.NaN, fewestUses = -5 });
            save.Sanitize();
            Assert.That(save.records.Count, Is.EqualTo(1));
            Assert.That(save.records[0].stars, Is.EqualTo(3));
            Assert.That(save.records[0].bestSeconds, Is.EqualTo(float.MaxValue));
            Assert.That(save.records[0].fewestUses, Is.EqualTo(int.MaxValue));
        }
    }

    public class StuckHintsTests
    {
        [Test]
        public void TiersRiseWithIdleTime()
        {
            var h = new StuckHints(10f, 20f, 40f);
            Assert.That(h.Update(5f, 0, 0), Is.EqualTo(0));
            Assert.That(h.Update(6f, 0, 0), Is.EqualTo(1));
            Assert.That(h.Update(10f, 0, 0), Is.EqualTo(2));
            Assert.That(h.Update(25f, 0, 0), Is.EqualTo(3));
        }

        [Test]
        public void ANewObjectiveRestartsTheClock()
        {
            var h = new StuckHints(10f, 20f, 40f);
            h.Update(30f, 0, 0);
            Assert.That(h.Tier, Is.EqualTo(2));
            Assert.That(h.Update(0.1f, 1, 0), Is.EqualTo(0));
        }

        [Test]
        public void TryingThingsStepsHelpBack_ButDoesNotResetFully()
        {
            var h = new StuckHints(10f, 20f, 40f);
            h.Update(25f, 0, 0);
            Assert.That(h.Tier, Is.EqualTo(2));
            h.Update(0f, 0, 1); // used a glitch
            Assert.That(h.IdleSeconds, Is.EqualTo(15f).Within(1e-3f));
            Assert.That(h.Tier, Is.EqualTo(1));
        }

        [Test]
        public void RejectsBadThresholds() => Assert.Throws<ArgumentException>(() => new StuckHints(10f, 5f, 40f));
    }

    public class GuardBrainTests
    {
        [Test]
        public void Patrol_PausesThenAdvancesAroundTheLoop()
        {
            var g = new GuardBrain(3, patrolPauseSeconds: 1f);
            Assert.That(g.ShouldMove, Is.False, "starts with a pause");
            g.Tick(1.1f, false);
            Assert.That(g.ShouldMove, Is.True);
            g.Tick(0.1f, true);
            Assert.That(g.PatrolIndex, Is.EqualTo(1));
            Assert.That(g.ShouldMove, Is.False, "pauses at the point");
            g.Tick(1.1f, false);
            g.Tick(0.1f, true);
            g.Tick(1.1f, false);
            g.Tick(0.1f, true);
            Assert.That(g.PatrolIndex, Is.EqualTo(0), "loops");
        }

        [Test]
        public void Distract_GoesToTheDecoy_LingersThenReturns()
        {
            var g = new GuardBrain(2, 0f, lingerSeconds: 2f);
            g.Distract();
            Assert.That(g.State, Is.EqualTo(GuardState.Investigate));
            Assert.That(g.IsNeutralized, Is.True);
            g.Tick(1f, false);
            Assert.That(g.State, Is.EqualTo(GuardState.Investigate), "not there yet: no lingering");
            g.Tick(1f, true);
            Assert.That(g.State, Is.EqualTo(GuardState.Investigate));
            g.Tick(1.1f, true);
            Assert.That(g.State, Is.EqualTo(GuardState.Return));
            Assert.That(g.IsNeutralized, Is.False);
            g.Tick(0.1f, true);
            Assert.That(g.State, Is.EqualTo(GuardState.Patrol));
        }

        [Test]
        public void Disabled_DoesNothing_UntilTheOverrideIsCleared()
        {
            var g = new GuardBrain(2, 0f);
            g.SetDisabled();
            g.Tick(10f, true);
            Assert.That(g.State, Is.EqualTo(GuardState.Disabled));
            Assert.That(g.ShouldMove, Is.False);
            g.Distract();
            Assert.That(g.State, Is.EqualTo(GuardState.Disabled), "a stunned guard ignores lures");
            g.ClearOverride();
            Assert.That(g.State, Is.EqualTo(GuardState.Return));
        }

        [Test]
        public void ClearOverride_OnAPatrollingGuard_ChangesNothing()
        {
            var g = new GuardBrain(2, 0f);
            int changes = 0;
            g.StateChanged += (a, b) => changes++;
            g.ClearOverride();
            Assert.That(changes, Is.EqualTo(0));
            g.Distract();
            g.ClearOverride();
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void Reset_RestartsThePatrol()
        {
            var g = new GuardBrain(3, 0f);
            g.Tick(0.1f, true);
            g.Distract();
            g.Reset();
            Assert.That(g.State, Is.EqualTo(GuardState.Patrol));
            Assert.That(g.PatrolIndex, Is.EqualTo(0));
        }

        [Test]
        public void RejectsAnEmptyRoute() => Assert.Throws<ArgumentOutOfRangeException>(() => new GuardBrain(0));
    }

    public class VisionConeTests
    {
        [Test]
        public void SeesWhatIsInFront_WithinRangeAndAngle()
        {
            Assert.That(VisionCone.Contains(0, 0, 1, 0, 5, 0, 8, 30), Is.True);
            Assert.That(VisionCone.Contains(0, 0, 1, 0, 5, 2, 8, 30), Is.True, "about 22 degrees off axis");
            Assert.That(VisionCone.Contains(0, 0, 1, 0, 5, 4, 8, 30), Is.False, "about 39 degrees off axis");
            Assert.That(VisionCone.Contains(0, 0, 1, 0, -3, 0, 8, 30), Is.False, "behind");
            Assert.That(VisionCone.Contains(0, 0, 1, 0, 9, 0, 8, 30), Is.False, "too far");
        }

        [Test]
        public void EdgeCases()
        {
            Assert.That(VisionCone.Contains(0, 0, 1, 0, 0, 0, 5, 10), Is.True, "on top of the observer");
            Assert.That(VisionCone.Contains(0, 0, 0, 0, 3, 0, 5, 90), Is.False, "no facing direction");
            Assert.That(VisionCone.Contains(0, 0, 0, 1, 1, 0, 5, 180), Is.True, "a full circle sees everywhere in range");
        }
    }

    public class PropertySwapMathTests
    {
        [Test]
        public void SwapsValues()
        {
            Assert.That(PropertySwapMath.TrySwap(SwapProperty.Mass, 2f, 20f, out var a, out var b), Is.True);
            Assert.That(a, Is.EqualTo(20f));
            Assert.That(b, Is.EqualTo(2f));
        }

        [Test]
        public void EqualValues_AreANoOp()
        {
            Assert.That(PropertySwapMath.TrySwap(SwapProperty.Speed, 1f, 1f, out var a, out var b), Is.False);
            Assert.That(a, Is.EqualTo(1f));
            Assert.That(b, Is.EqualTo(1f));
        }

        [Test]
        public void Results_StayWithinTheSafeLimits()
        {
            Assert.That(PropertySwapMath.TrySwap(SwapProperty.Mass, 0.01f, 500f, out var a, out var b), Is.True);
            Assert.That(a, Is.EqualTo(60f), "capped");
            Assert.That(b, Is.EqualTo(0.5f), "never zero");
            Assert.That(PropertyLimits.For(SwapProperty.Speed).Clamp(float.NaN), Is.EqualTo(0.25f));
        }

        [Test]
        public void BadLimits_Throw() => Assert.Throws<ArgumentOutOfRangeException>(() => new PropertyLimits(0f, 5f));
    }

    public class TypewriterTests
    {
        [Test]
        public void RevealsOverTime_AndNeverPastTheEnd()
        {
            Assert.That(Typewriter.VisibleCharacters(0f, 20f, 10), Is.EqualTo(0));
            Assert.That(Typewriter.VisibleCharacters(0.25f, 20f, 10), Is.EqualTo(5));
            Assert.That(Typewriter.VisibleCharacters(5f, 20f, 10), Is.EqualTo(10));
            Assert.That(Typewriter.VisibleCharacters(1f, 0f, 10), Is.EqualTo(10), "speed 0 = instant");
            Assert.That(Typewriter.VisibleCharacters(1f, 20f, 0), Is.EqualTo(0));
            Assert.That(Typewriter.TotalSeconds(new[] { "abcd", "" }, 4f, 0.5f), Is.EqualTo(2f).Within(1e-4f));
        }
    }

    public class MusicEngineTests
    {
        private static float[] Render(MusicEngine e, float seconds, int channels = 2)
        {
            int frames = (int)(e.SampleRate * seconds);
            var buffer = new float[frames * channels];
            e.Render(buffer, frames, channels);
            return buffer;
        }

        private static double Rms(float[] b)
        {
            double sum = 0;
            foreach (var s in b) sum += s * s;
            return Math.Sqrt(sum / b.Length);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(7)]
        public void EveryTheme_IsAudible_Finite_AndBounded(int chapter)
        {
            var e = new MusicEngine(MusicThemes.ForChapter(chapter), 44100) { Intensity = 1f, Tension = 1f };
            var b = Render(e, 6f);
            foreach (var s in b)
            {
                Assert.That(float.IsNaN(s) || float.IsInfinity(s), Is.False);
                Assert.That(Math.Abs(s), Is.LessThanOrEqualTo(1f));
            }
            Assert.That(Rms(b), Is.GreaterThan(0.005));
        }

        [Test]
        public void SameSeed_SameMusic()
        {
            var a = Render(new MusicEngine(MusicThemes.Lab(), 22050, 7) { Intensity = 0.7f }, 2f);
            var b = Render(new MusicEngine(MusicThemes.Lab(), 22050, 7) { Intensity = 0.7f }, 2f);
            Assert.That(a, Is.EqualTo(b));
            var c = Render(new MusicEngine(MusicThemes.Lab(), 22050, 8) { Intensity = 0.7f }, 2f);
            Assert.That(a, Is.Not.EqualTo(c));
        }

        [Test]
        public void MoreIntensity_MoreEnergy()
        {
            var calm = Rms(Render(new MusicEngine(MusicThemes.Facility(), 22050) { Intensity = 0f }, 8f));
            var busy = Rms(Render(new MusicEngine(MusicThemes.Facility(), 22050) { Intensity = 1f }, 8f));
            Assert.That(busy, Is.GreaterThan(calm * 1.15));
        }

        [Test]
        public void Tension_ChangesTheSound()
        {
            var a = Render(new MusicEngine(MusicThemes.Corrupted(), 22050) { Intensity = 0.5f }, 4f);
            var b = Render(new MusicEngine(MusicThemes.Corrupted(), 22050) { Intensity = 0.5f, Tension = 1f }, 4f);
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void MonoAndSurroundBuffers_AreFilledSafely()
        {
            var mono = Render(new MusicEngine(MusicThemes.Menu(), 22050) { Intensity = 1f }, 1f, 1);
            Assert.That(Rms(mono), Is.GreaterThan(0.0));
            var six = Render(new MusicEngine(MusicThemes.Menu(), 22050) { Intensity = 1f }, 0.5f, 6);
            for (int i = 0; i < six.Length; i += 6) Assert.That(six[i + 5], Is.EqualTo(0f));
        }

        [Test]
        public void NotesFollowTheScale()
        {
            var e = new MusicEngine(MusicThemes.Lab(), 22050);
            Assert.That(e.DegreeToMidi(0), Is.EqualTo(48));
            Assert.That(e.DegreeToMidi(7), Is.EqualTo(60), "one octave up after 7 degrees");
            Assert.That(e.DegreeToMidi(-1), Is.EqualTo(48 + 12 * -1 + 14), "wraps below the root");
            Assert.That(MusicEngine.Frequency(69), Is.EqualTo(440.0).Within(1e-6));
        }

        [Test]
        public void RejectsBadArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new MusicEngine(null, 44100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MusicEngine(MusicThemes.Lab(), 100));
        }
    }

    public class PresentationCoreTests
    {
        [Test]
        public void RunSummary_FormatsTimeAndStars()
        {
            Assert.That(RunSummary.Time(0f), Is.EqualTo("00:00"));
            Assert.That(RunSummary.Time(125.9f), Is.EqualTo("02:05"));
            Assert.That(RunSummary.Time(float.NaN), Is.EqualTo("00:00"));
            Assert.That(RunSummary.Time(1e9f), Is.EqualTo("99:59"));
            Assert.That(RunSummary.Stars(2), Is.EqualTo("\u2605\u2605\u2606"));
            Assert.That(RunSummary.Stars(9), Is.EqualTo("\u2605\u2605\u2605"));
            Assert.That(RunSummary.NextStarHint(3, 4), Is.Empty);
            Assert.That(RunSummary.NextStarHint(1, 4), Does.Contain("4"));
        }

        [Test]
        public void HeroBarks_AreRateLimited_ButVictoryAlwaysSpeaks()
        {
            var barks = new HeroBarks(7, 4f);
            Assert.That(barks.TryBark(BarkEvent.Blocked, 10f), Is.Not.Null);
            Assert.That(barks.TryBark(BarkEvent.Confused, 11f), Is.Null, "too soon");
            Assert.That(barks.TryBark(BarkEvent.Arrived, 11.5f), Is.Not.Null);
            Assert.That(barks.TryBark(BarkEvent.Blocked, 20f), Is.Not.Null);
        }

        [Test]
        public void HeroBarks_NeverRepeatTheSameLineTwice()
        {
            var barks = new HeroBarks(3);
            string previous = null;
            for (int i = 0; i < 50; i++)
            {
                var line = barks.Pick(BarkEvent.Blocked);
                Assert.That(line, Is.Not.EqualTo(previous));
                previous = line;
            }
        }
    
    public class GuardLureMovementTests
    {
        [Test]
        public void LuredGuard_MovesImmediately_EvenMidPatrolPause()
        {
            var brain = new GuardBrain(2, patrolPauseSeconds: 5f, lingerSeconds: 3f);
            Assert.That(brain.ShouldMove, Is.False, "paused at the start of the route");
            brain.Distract();
            Assert.That(brain.State, Is.EqualTo(GuardState.Investigate));
            Assert.That(brain.ShouldMove, Is.True, "a lured guard walks to the decoy at once");
        }
    }
}
}
