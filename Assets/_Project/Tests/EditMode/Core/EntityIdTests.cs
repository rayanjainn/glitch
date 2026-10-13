using System;
using BugReport.Core.Entities;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class EntityIdTests
    {
        [Test]
        public void Default_IsNoneAndInvalid()
        {
            Assert.That(default(EntityId), Is.EqualTo(EntityId.None));
            Assert.That(EntityId.None.IsValid, Is.False);
        }

        [Test]
        public void Equality_IsByValue()
        {
            Assert.That(new EntityId(3) == new EntityId(3), Is.True);
            Assert.That(new EntityId(3) != new EntityId(4), Is.True);
            Assert.That(new EntityId(3).GetHashCode(), Is.EqualTo(new EntityId(3).GetHashCode()));
        }

        [Test]
        public void ToString_UsesDiagnosticFormat()
        {
            Assert.That(new EntityId(7).ToString(), Is.EqualTo("ENT-0007"));
            Assert.That(EntityId.None.ToString(), Is.EqualTo("ENT-NONE"));
        }

        [Test]
        public void NegativeValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EntityId(-1));
        }
    }
}
