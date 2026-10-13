using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class EntityRegistryTests
    {
        private EntityRegistry registry;

        [SetUp]
        public void SetUp() => registry = new EntityRegistry();

        [Test]
        public void Register_AssignsUniqueValidIds()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            var b = new FakeEntity("B").RegisterIn(registry);

            Assert.That(a.Id.IsValid, Is.True);
            Assert.That(b.Id.IsValid, Is.True);
            Assert.That(a.Id, Is.Not.EqualTo(b.Id));
            Assert.That(registry.Count, Is.EqualTo(2));
        }

        [Test]
        public void Register_Twice_ReturnsSameIdWithoutDuplicating()
        {
            var a = new FakeEntity("A");
            var first = registry.Register(a);
            var second = registry.Register(a);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void Ids_AreNeverReused_AfterUnregister()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            registry.Unregister(a.Id);
            var b = new FakeEntity("B").RegisterIn(registry);

            Assert.That(b.Id, Is.Not.EqualTo(a.Id));
            Assert.That(registry.TryGet(a.Id, out _), Is.False, "stale id must not resolve to a new entity");
        }

        [Test]
        public void Ids_AreNeverReused_AfterClear()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            registry.Clear();
            var b = new FakeEntity("B").RegisterIn(registry);

            Assert.That(b.Id, Is.Not.EqualTo(a.Id));
        }

        [Test]
        public void Unregister_UnknownId_ReturnsFalse()
        {
            Assert.That(registry.Unregister(new GameEntityId(99)), Is.False);
            Assert.That(registry.Unregister(GameEntityId.None), Is.False);
        }

        [Test]
        public void Unregister_RaisesEventOnce()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            int calls = 0;
            registry.Unregistered += (id, e) => { calls++; Assert.That(id, Is.EqualTo(a.Id)); Assert.That(e, Is.SameAs(a)); };

            registry.Unregister(a.Id);
            registry.Unregister(a.Id);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void TryGet_DeadEntity_IsPrunedAndReported()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            var removed = new List<GameEntityId>();
            registry.Unregistered += (id, _) => removed.Add(id);

            a.IsAlive = false;

            Assert.That(registry.TryGet(a.Id, out var found), Is.False);
            Assert.That(found, Is.Null);
            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(removed, Is.EqualTo(new[] { a.Id }));
        }

        [Test]
        public void TryGetId_FindsRegisteredEntity()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            Assert.That(registry.TryGetId(a, out var id), Is.True);
            Assert.That(id, Is.EqualTo(a.Id));
            Assert.That(registry.TryGetId(new FakeEntity("B"), out _), Is.False);
            Assert.That(registry.TryGetId(null, out _), Is.False);
        }

        [Test]
        public void GetAll_SkipsAndPrunesDeadEntities()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            var b = new FakeEntity("B").RegisterIn(registry);
            b.IsAlive = false;

            var results = new List<IEntityDescriptor> { new FakeEntity("stale") };
            registry.GetAll(results);

            Assert.That(results, Is.EqualTo(new IEntityDescriptor[] { a }));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void PruneDead_ReturnsRemovedCount()
        {
            new FakeEntity("A").RegisterIn(registry).IsAlive = false;
            new FakeEntity("B").RegisterIn(registry).IsAlive = false;
            new FakeEntity("C").RegisterIn(registry);

            Assert.That(registry.PruneDead(), Is.EqualTo(2));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear_UnregistersEverythingAndRaisesEvents()
        {
            new FakeEntity("A").RegisterIn(registry);
            new FakeEntity("B").RegisterIn(registry);
            int calls = 0;
            registry.Unregistered += (_, __) => calls++;

            registry.Clear();

            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void Unregister_FromInsideUnregisteredHandler_IsSafe()
        {
            var a = new FakeEntity("A").RegisterIn(registry);
            var b = new FakeEntity("B").RegisterIn(registry);
            // A handler that cascades (e.g. a duplicate dying with its source) must not corrupt Clear().
            registry.Unregistered += (id, _) => { if (id == a.Id) registry.Unregister(b.Id); };

            Assert.DoesNotThrow(() => registry.Clear());
            Assert.That(registry.Count, Is.EqualTo(0));
        }
    }
}
