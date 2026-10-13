using BugReport.Core.Entities;
using BugReport.Core.Interaction;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class TargetValidatorTests
    {
        [Test]
        public void Null_IsNoTarget()
        {
            var result = TargetValidator.Validate(null, TargetRequirement.Selection);
            Assert.That(result.Failure, Is.EqualTo(TargetValidationFailure.NoTarget));
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void DeadEntity_IsDestroyed()
        {
            var e = new FakeEntity("A") { IsAlive = false };
            Assert.That(TargetValidator.Validate(e, TargetRequirement.Selection).Failure, Is.EqualTo(TargetValidationFailure.Destroyed));
        }

        [Test]
        public void Anomaly_IsNeverSelectable()
        {
            var e = new FakeEntity("Self", EntityCategory.Anomaly, EntityCapabilities.Inspectable);
            Assert.That(TargetValidator.Validate(e, TargetRequirement.Selection).Failure, Is.EqualTo(TargetValidationFailure.CategoryNotAllowed));
        }

        [Test]
        public void MissingCapability_ReportsWhatIsMissing()
        {
            var e = new FakeEntity("Wall", EntityCategory.Structure, EntityCapabilities.Inspectable);
            var requirement = new TargetRequirement(EntityCapabilities.Inspectable | EntityCapabilities.Freezable | EntityCapabilities.Duplicable);

            var result = TargetValidator.Validate(e, requirement);

            Assert.That(result.Failure, Is.EqualTo(TargetValidationFailure.MissingCapability));
            Assert.That(result.MissingCapabilities, Is.EqualTo(EntityCapabilities.Freezable | EntityCapabilities.Duplicable));
            StringAssert.Contains("FREEZABLE", result.Describe());
        }

        [Test]
        public void CompatibleEntity_IsValid()
        {
            var e = new FakeEntity("Crate", EntityCategory.Prop, EntityCapabilities.Inspectable | EntityCapabilities.Freezable);
            var result = TargetValidator.Validate(e, new TargetRequirement(EntityCapabilities.Freezable));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Describe(), Is.EqualTo("OK"));
        }

        [Test]
        public void Requirement_ExcludesOnlyListedCategories()
        {
            var requirement = new TargetRequirement(EntityCapabilities.None, EntityCategory.Hero, EntityCategory.Anomaly);
            Assert.That(requirement.Excludes(EntityCategory.Hero), Is.True);
            Assert.That(requirement.Excludes(EntityCategory.Anomaly), Is.True);
            Assert.That(requirement.Excludes(EntityCategory.Prop), Is.False);
        }
    }
}
