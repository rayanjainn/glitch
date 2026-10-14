using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Interaction;
using BugReport.Level;
using BugReport.Tutorial;
using BugReport.UI;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class GuideDirectorTests : PlayModeTestBase
    {
        private GameServices services;
        private GlitchController glitches;
        private FreezeDefinition freeze;
        private DuplicationDefinition duplication;
        private ObjectiveDirector objectives;
        private ManualCondition first;
        private GuideDirector guide;
        private GameEntity wall;
        private GameEntity machine;

        [SetUp]
        public void SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            wall = Entity("WALL", new Vector3(0, 0.5f, 5), EntityCapabilities.Inspectable, kinematic: true);
            machine = Entity("MACHINE", new Vector3(3, 0.5f, 5), EntityCapabilities.Inspectable | EntityCapabilities.Freezable, kinematic: true);
            machine.gameObject.SetActive(false);
            machine.gameObject.AddComponent<FreezeCapability>();
            machine.gameObject.SetActive(true);

            var systems = Track(new GameObject("Systems"));
            systems.SetActive(false);
            var selection = systems.AddComponent<SelectionController>();
            glitches = systems.AddComponent<GlitchController>();
            freeze = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freeze.ApplyDefaults();
            duplication = Track(ScriptableObject.CreateInstance<DuplicationDefinition>());
            duplication.ApplyDefaults();
            glitches.Configure(new GlitchDefinition[] { freeze, duplication }, selection);

            first = systems.AddComponent<ManualCondition>();
            var second = systems.AddComponent<ManualCondition>();
            objectives = systems.AddComponent<ObjectiveDirector>();
            objectives.Configure(new[]
            {
                new ObjectiveDirector.Entry("one", "ONE", "", first),
                new ObjectiveDirector.Entry("two", "TWO", "", second),
            });

            guide = systems.AddComponent<GuideDirector>();
            guide.Configure(new[]
            {
                new GuideDirector.Step(0, "Look at the wall.", GuideCondition.LookAt, wall),
                new GuideDirector.Step(0, "Select it with [E].", GuideCondition.Select, wall),
                new GuideDirector.Step(0, "Look at the machine.", GuideCondition.LookAt, machine, requireSelected: wall),
                new GuideDirector.Step(0, "Freeze it [LMB].", GuideCondition.Apply, machine, freeze),
                new GuideDirector.Step(0, "Watch.", GuideCondition.WaitForObjective),
                new GuideDirector.Step(1, "Switch with [Q].", GuideCondition.Equip, glitch: duplication),
                new GuideDirector.Step(1, "Watch again.", GuideCondition.WaitForObjective),
            }, new[] { new GuideDirector.Unlock(1, duplication) }, glitches, objectives, null);
            systems.SetActive(true);
            guide.Evaluate();
        }

        private GameEntity Entity(string name, Vector3 position, EntityCapabilities caps, bool kinematic)
        {
            var go = Box(name, position, Vector3.one);
            go.SetActive(false);
            go.AddComponent<Rigidbody>().isKinematic = kinematic;
            var entity = go.AddComponent<GameEntity>();
            entity.Configure(name, EntityCategory.Prop, caps);
            go.SetActive(true);
            return entity;
        }

        [Test]
        public void ScriptIsValid() => Assert.That(guide.Validate(), Is.Null);

        [Test]
        public void FollowsWhatThePlayerDoes_AndWalksBackWhenTheyUndoIt()
        {
            Assert.That(guide.CurrentText, Is.EqualTo("Look at the wall."));
            Assert.That(guide.CurrentTarget, Is.EqualTo(wall), "the marker shows what to look at");

            Assert.That(services.Selection.TrySelect(wall.Id).IsValid, Is.True);
            guide.Evaluate();
            Assert.That(guide.CurrentText, Is.EqualTo("Look at the machine."), "selecting implies having looked");
            Assert.That(guide.Position, Is.EqualTo((3, 5)));

            services.Selection.SetHovered(machine.Id);
            guide.Evaluate();
            Assert.That(guide.CurrentText, Is.EqualTo("Freeze it [LMB]."));

            services.Selection.ClearSelection();
            guide.Evaluate();
            Assert.That(guide.CurrentText, Is.EqualTo("Look at the wall."), "lost the selection: back to the start");
        }

        [Test]
        public void ApplyingTheGlitch_Latches_EvenIfDoneEarly()
        {
            services.Selection.SetHovered(machine.Id);
            glitches.ApplyCurrent();
            Assert.That(glitches.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Applied), glitches.LastFeedback.Message);
            guide.Evaluate();
            Assert.That(guide.CurrentText, Is.EqualTo("Watch."));
        }

        [Test]
        public void LaterGlitches_StayLockedUntilTheirObjectiveStarts()
        {
            Assert.That(glitches.UnlockedCount, Is.EqualTo(1));
            glitches.CycleGlitch();
            Assert.That(glitches.CurrentDefinition, Is.EqualTo(freeze), "cycling skips locked glitches");

            first.met = true;
            objectives.Evaluate(); // unlocks in the same call, not on the next frame
            Assert.That(glitches.UnlockedCount, Is.EqualTo(2));
            StringAssert.Contains("NEW GLITCH", glitches.LastFeedback.Message);
            Assert.That(guide.CurrentText, Is.EqualTo("Switch with [Q]."));

            glitches.CycleGlitch();
            guide.Evaluate();
            Assert.That(glitches.CurrentDefinition, Is.EqualTo(duplication));
            Assert.That(guide.CurrentText, Is.EqualTo("Watch again."));
        }

        [Test]
        public void AdvancedFiresOncePerStep_NotWhenLookingAwayAndBack()
        {
            int advanced = 0;
            guide.Advanced += () => advanced++;
            services.Selection.SetHovered(wall.Id);
            guide.Evaluate();
            Assert.That(advanced, Is.EqualTo(1));

            services.Selection.SetHovered(GameEntityId.None);
            guide.Evaluate();
            services.Selection.SetHovered(wall.Id);
            guide.Evaluate();
            Assert.That(advanced, Is.EqualTo(1));
        }

        [Test]
        public void Hud_ShowsTheStep_AndHidesTheObjectiveHintMeanwhile()
        {
            var panel = Track(ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>());
            panel.themeStyleSheet = Track(ScriptableObject.CreateInstance<UnityEngine.UIElements.ThemeStyleSheet>());
            var ui = Track(new GameObject("UI"));
            ui.SetActive(false);
            ui.AddComponent<UnityEngine.UIElements.UIDocument>().panelSettings = panel;
            var hud = ui.AddComponent<GameHud>();
            var containment = ui.AddComponent<Containment.ContainmentDirector>();
            containment.Configure(Track(ScriptableObject.CreateInstance<Containment.ContainmentSettingsAsset>()));
            hud.Configure(glitches, null, containment, objectives, null, guide);
            ui.SetActive(true);
            hud.RefreshNow();
            Assert.That(hud.GuideTextShown, Is.EqualTo("Look at the wall."));
            Assert.That(hud.Find<UnityEngine.UIElements.Label>("guide-step").text, Is.EqualTo("GUIDE  1/5"));
        }
    }
}
