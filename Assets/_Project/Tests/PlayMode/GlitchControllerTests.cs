using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Player workflow: select SOURCE (E), aim at TARGET, apply (LMB), revert (R). No input devices involved.</summary>
    public class GlitchControllerTests : PlayModeTestBase
    {
        private const EntityCapabilities Transferable = EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable;

        private GameServices services;
        private GlitchController controller;
        private CollisionCapability wall;
        private CollisionCapability pad;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            wall = Capability("WALL", CollisionMode.Solid, new Vector3(0, 1, 3));
            pad = Capability("PAD", CollisionMode.Intangible, new Vector3(8, 0.05f, 0));

            var definition = Track(ScriptableObject.CreateInstance<CollisionTransferDefinition>());
            definition.ApplyDefaults();
            var go = Track(new GameObject("Glitches"));
            go.SetActive(false);
            controller = go.AddComponent<GlitchController>();
            controller.Configure(new GlitchDefinition[] { definition }, null);
            go.SetActive(true);
        }

        private CollisionCapability Capability(string name, CollisionMode mode, Vector3 position)
        {
            var go = Box(name, position, new Vector3(2, 2, 0.5f));
            go.SetActive(false);
            go.layer = GameLayers.Index(mode == CollisionMode.Solid ? GameLayers.World : GameLayers.Intangible);
            go.AddComponent<GameEntity>().Configure(name, EntityCategory.Structure, Transferable);
            var c = go.AddComponent<CollisionCapability>();
            c.Configure(go.GetComponents<Collider>(), mode);
            go.SetActive(true);
            Physics.SyncTransforms();
            return c;
        }

        private static GameEntityId Id(Component c) => c.GetComponent<GameEntity>().Id;

        [Test]
        public void LoadsValidDefinitions()
        {
            Assert.That(controller.LoadedCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDefinition, Is.InstanceOf<CollisionTransferDefinition>());
        }

        [Test]
        public void ApplyWithoutSource_PromptsForSelection()
        {
            services.Selection.SetHovered(Id(pad));
            controller.ApplyCurrent();

            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Rejected));
            StringAssert.Contains("SELECT A SOURCE", controller.LastFeedback.Message);
            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void SelectSource_AimTarget_Apply_ThenRevert()
        {
            services.Selection.TrySelect(Id(wall));
            services.Selection.SetHovered(Id(pad));

            controller.ApplyCurrent();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Applied), controller.LastFeedback.Message);
            Assert.That(wall.EffectiveMode, Is.EqualTo(CollisionMode.Intangible));
            Assert.That(services.Selection.HasSelection, Is.False, "source selection is consumed");

            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Reverted));
            Assert.That(wall.EffectiveMode, Is.EqualTo(CollisionMode.Solid));
        }

        [Test]
        public void RevertWithNothingActive_SaysSo()
        {
            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Message, Is.EqualTo("NOTHING TO REVERT"));
        }

        [Test]
        public void WhilePaused_NothingIsApplied()
        {
            services.Selection.TrySelect(Id(wall));
            services.Selection.SetHovered(Id(pad));
            services.State.TogglePause();

            controller.ApplyCurrent();

            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void InvalidDefinition_IsSkippedWithError()
        {
            var broken = Track(ScriptableObject.CreateInstance<CollisionTransferDefinition>()); // no slots
            var go = Track(new GameObject("Broken"));
            go.SetActive(false);
            var c = go.AddComponent<GlitchController>();
            c.Configure(new GlitchDefinition[] { broken }, null);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("has no target slots"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no usable glitches"));

            go.SetActive(true);

            Assert.That(c.LoadedCount, Is.EqualTo(0));
        }
    }
}
