using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Level;
using BugReport.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class HintDirectorTests : PlayModeTestBase
    {
        private GameServices services;
        private HintDirector hints;
        private GameEntity key;

        [SetUp]
        public void SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            var keyGo = Box("KEY", Vector3.zero, Vector3.one);
            keyGo.SetActive(false);
            key = keyGo.AddComponent<GameEntity>();
            key.Configure("KEY", EntityCategory.Prop, EntityCapabilities.Inspectable);
            keyGo.SetActive(true);

            var condition = Track(new GameObject("Condition")).AddComponent<ManualCondition>();
            var directorGo = Track(new GameObject("Objectives"));
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<ObjectiveDirector>();
            director.Configure(new[] { new ObjectiveDirector.Entry("a", "A", "hint", condition, key) });
            directorGo.SetActive(true);

            var hintGo = Track(new GameObject("Hints"));
            hintGo.SetActive(false);
            hints = hintGo.AddComponent<HintDirector>();
            hints.Configure(director);
            hintGo.SetActive(true);
        }

        [Test]
        public void Help_EscalatesWithIdleTime_AndShowsTheKeyObjectLast()
        {
            hints.Step(10f);
            Assert.That(hints.ShowHintText, Is.False);
            Assert.That(hints.FocusTarget, Is.Null);

            hints.Step(45f);
            Assert.That(hints.ShowHintText, Is.True, "55 s without progress: the hint text");
            Assert.That(hints.FocusTarget, Is.Null);

            hints.Step(50f);
            Assert.That(hints.Tier, Is.EqualTo(3));
            Assert.That(hints.FocusTarget, Is.SameAs(key));
        }

        [Test]
        public void TimePaused_DoesNotCount()
        {
            services.State.TryTransition(GameState.Paused);
            hints.Step(500f);
            Assert.That(hints.Tier, Is.EqualTo(0));
        }
    }
}
