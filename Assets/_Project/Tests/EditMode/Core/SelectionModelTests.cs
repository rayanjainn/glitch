using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Interaction;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class SelectionModelTests
    {
        private EntityRegistry registry;
        private SelectionModel selection;

        [SetUp]
        public void SetUp()
        {
            registry = new EntityRegistry();
            selection = new SelectionModel(registry, TargetRequirement.Selection);
        }

        [TearDown]
        public void TearDown() => selection.Dispose();

        [Test]
        public void Initially_NothingHoveredOrSelected()
        {
            Assert.That(selection.Hovered, Is.EqualTo(GameEntityId.None));
            Assert.That(selection.HasSelection, Is.False);
            Assert.That(selection.HoveredValidation.Failure, Is.EqualTo(TargetValidationFailure.NoTarget));
        }

        [Test]
        public void TrySelect_ValidTarget_SelectsAndRaisesOnce()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            var events = new List<(GameEntityId, GameEntityId)>();
            selection.SelectionChanged += (p, c) => events.Add((p, c));

            Assert.That(selection.TrySelect(crate.Id).IsValid, Is.True);
            Assert.That(selection.TrySelect(crate.Id).IsValid, Is.True); // re-select: no extra event

            Assert.That(selection.Selected, Is.EqualTo(crate.Id));
            Assert.That(events, Is.EqualTo(new[] { (GameEntityId.None, crate.Id) }));
        }

        [Test]
        public void TrySelect_InvalidTarget_KeepsPreviousSelection()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            var self = new FakeEntity("Anomaly", EntityCategory.Anomaly).RegisterIn(registry);
            selection.TrySelect(crate.Id);

            var result = selection.TrySelect(self.Id);

            Assert.That(result.Failure, Is.EqualTo(TargetValidationFailure.CategoryNotAllowed));
            Assert.That(selection.Selected, Is.EqualTo(crate.Id));
        }

        [Test]
        public void TrySelect_UnknownId_ReportsDestroyed()
        {
            Assert.That(selection.TrySelect(new GameEntityId(42)).Failure, Is.EqualTo(TargetValidationFailure.Destroyed));
            Assert.That(selection.TrySelect(GameEntityId.None).Failure, Is.EqualTo(TargetValidationFailure.NoTarget));
        }

        [Test]
        public void SetHovered_ValidatesAndRaisesOnChangeOnly()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            var deco = new FakeEntity("Plant", EntityCategory.Decoration, EntityCapabilities.None).RegisterIn(registry);
            int changes = 0;
            selection.HoverChanged += (_, __) => changes++;

            selection.SetHovered(crate.Id);
            selection.SetHovered(crate.Id);
            Assert.That(selection.HoveredValidation.IsValid, Is.True);

            selection.SetHovered(deco.Id);
            Assert.That(selection.HoveredValidation.Failure, Is.EqualTo(TargetValidationFailure.MissingCapability));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void TrySelectHovered_UsesHoveredEntity()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            selection.SetHovered(crate.Id);
            Assert.That(selection.TrySelectHovered().IsValid, Is.True);
            Assert.That(selection.Selected, Is.EqualTo(crate.Id));
        }

        [Test]
        public void UnregisteringSelectedEntity_ClearsSelectionAndHover()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            selection.SetHovered(crate.Id);
            selection.TrySelect(crate.Id);
            GameEntityId cleared = crate.Id;
            selection.SelectionChanged += (p, c) => cleared = c;

            registry.Unregister(crate.Id);

            Assert.That(selection.HasSelection, Is.False);
            Assert.That(selection.Hovered, Is.EqualTo(GameEntityId.None));
            Assert.That(cleared, Is.EqualTo(GameEntityId.None));
        }

        [Test]
        public void DestroyedSelectedEntity_IsDroppedOnNextLookup()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            selection.TrySelect(crate.Id);
            crate.IsAlive = false; // destroyed without unregistering (e.g. Unity Destroy race)

            Assert.That(selection.TryGetSelected(out var found), Is.False);
            Assert.That(found, Is.Null);
            Assert.That(selection.HasSelection, Is.False);
        }

        [Test]
        public void ClearSelection_ReturnsWhetherSomethingWasSelected()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            Assert.That(selection.ClearSelection(), Is.False);
            selection.TrySelect(crate.Id);
            Assert.That(selection.ClearSelection(), Is.True);
            Assert.That(selection.HasSelection, Is.False);
        }

        [Test]
        public void SetRequirement_DropsSelectionThatNoLongerQualifies()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            selection.TrySelect(crate.Id);

            selection.SetRequirement(new TargetRequirement(EntityCapabilities.Freezable));

            Assert.That(selection.HasSelection, Is.False);
        }

        [Test]
        public void SetRequirement_KeepsSelectionThatStillQualifies()
        {
            var crate = new FakeEntity("Crate", EntityCategory.Prop, EntityCapabilities.Inspectable | EntityCapabilities.Freezable).RegisterIn(registry);
            selection.TrySelect(crate.Id);

            selection.SetRequirement(new TargetRequirement(EntityCapabilities.Freezable));

            Assert.That(selection.Selected, Is.EqualTo(crate.Id));
        }

        [Test]
        public void Dispose_StopsListeningToRegistry()
        {
            var crate = new FakeEntity("Crate").RegisterIn(registry);
            selection.TrySelect(crate.Id);
            selection.Dispose();

            Assert.DoesNotThrow(() => registry.Unregister(crate.Id));
            Assert.That(selection.Selected, Is.EqualTo(crate.Id), "disposed model no longer reacts");
        }
    }
}
