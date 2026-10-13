using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Scanner → SelectionModel → SelectionController without any input device.</summary>
    public class SelectionFlowTests : PlayModeTestBase
    {
        private GameServices services;
        private TargetScanner scanner;
        private SelectionController controller;

        [SetUp]
        public void SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            var cam = Track(new GameObject("Camera", typeof(Camera)));
            scanner = cam.AddComponent<TargetScanner>();
            scanner.AutoScan = false;
            scanner.Configure(cam.GetComponent<Camera>(), null);

            controller = Track(new GameObject("Selection")).AddComponent<SelectionController>();
            controller.Configure(scanner);
        }

        private GameEntity Place(string name, EntityCategory category, EntityCapabilities caps)
        {
            var go = Box(name, new Vector3(0f, 0f, 5f), Vector3.one);
            var entity = go.AddComponent<GameEntity>();
            // Configure must run before registration: re-enable to apply.
            go.SetActive(false);
            entity.Configure(name, category, caps);
            go.SetActive(true);
            Physics.SyncTransforms();
            return entity;
        }

        private void Hover()
        {
            scanner.Scan();
            services.Selection.SetHovered(scanner.CurrentTarget != null ? scanner.CurrentTarget.Id : EntityId.None);
        }

        [Test]
        public void SelectHovered_ValidTarget_SelectsAndEmitsFeedback()
        {
            var crate = Place("CRATE", EntityCategory.Prop, EntityCapabilities.Inspectable);
            SelectionFeedback? received = null;
            controller.Feedback += f => received = f;

            Hover();
            controller.SelectHovered();

            Assert.That(services.Selection.Selected, Is.EqualTo(crate.Id));
            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.Kind, Is.EqualTo(SelectionFeedbackKind.Selected));
        }

        [Test]
        public void SelectHovered_InvalidTarget_RejectsWithReason()
        {
            Place("PILLAR", EntityCategory.Decoration, EntityCapabilities.None);

            Hover();
            controller.SelectHovered();

            Assert.That(services.Selection.HasSelection, Is.False);
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(SelectionFeedbackKind.Rejected));
            StringAssert.Contains("INSPECTABLE", controller.LastFeedback.Message);
        }

        [Test]
        public void SelectHoveredTwice_TogglesOff()
        {
            Place("CRATE", EntityCategory.Prop, EntityCapabilities.Inspectable);
            Hover();
            controller.SelectHovered();
            controller.SelectHovered();

            Assert.That(services.Selection.HasSelection, Is.False);
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(SelectionFeedbackKind.Deselected));
        }

        [Test]
        public void WhilePaused_SelectionRequestsAreIgnored()
        {
            Place("CRATE", EntityCategory.Prop, EntityCapabilities.Inspectable);
            Hover();
            services.State.TogglePause();

            controller.SelectHovered();

            Assert.That(services.Selection.HasSelection, Is.False);
        }

        [Test]
        public void DestroyingSelectedEntity_ClearsSelection()
        {
            var crate = Place("CRATE", EntityCategory.Prop, EntityCapabilities.Inspectable);
            Hover();
            controller.SelectHovered();

            Object.DestroyImmediate(crate.gameObject);

            Assert.That(services.Selection.HasSelection, Is.False);
            Assert.That(services.Selection.TryGetSelected(out _), Is.False);
        }
    }
}
