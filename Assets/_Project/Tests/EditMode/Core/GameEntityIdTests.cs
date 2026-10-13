using System;
using BugReport.Core.Entities;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class GameEntityIdTests
    {
        [Test]
        public void Default_IsNoneAndInvalid()
        {
            Assert.That(default(GameEntityId), Is.EqualTo(GameEntityId.None));
            Assert.That(GameEntityId.None.IsValid, Is.False);
        }

        [Test]
        public void Equality_IsByValue()
        {
            Assert.That(new GameEntityId(3) == new GameEntityId(3), Is.True);
            Assert.That(new GameEntityId(3) != new GameEntityId(4), Is.True);
            Assert.That(new GameEntityId(3).GetHashCode(), Is.EqualTo(new GameEntityId(3).GetHashCode()));
        }

        [Test]
        public void ToString_UsesDiagnosticFormat()
        {
            Assert.That(new GameEntityId(7).ToString(), Is.EqualTo("ENT-0007"));
            Assert.That(GameEntityId.None.ToString(), Is.EqualTo("ENT-NONE"));
        }

        [Test]
        public void NegativeValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEntityId(-1));
        }
    }
}
