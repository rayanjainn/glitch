using System.Collections.Generic;
using BugReport.Core.Glitches;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class ModifierStackTests
    {
        private static readonly OperationId A = new OperationId(1);
        private static readonly OperationId B = new OperationId(2);

        [Test]
        public void Initially_EffectiveEqualsBase()
        {
            var stack = new ModifierStack<int>(5, ConflictPolicy.Stack);
            Assert.That(stack.Effective, Is.EqualTo(5));
            Assert.That(stack.IsModified, Is.False);
        }

        [Test]
        public void AddAndRelease_RestoresBase()
        {
            var changes = new List<int>();
            var stack = new ModifierStack<int>(5, ConflictPolicy.Exclusive, changes.Add);

            Assert.That(stack.TryAdd(A, 9, out var lease), Is.True);
            Assert.That(stack.Effective, Is.EqualTo(9));
            lease.Release();

            Assert.That(stack.Effective, Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(new[] { 9, 5 }));
        }

        [Test]
        public void Release_IsIdempotent()
        {
            var changes = new List<int>();
            var stack = new ModifierStack<int>(5, ConflictPolicy.Stack, changes.Add);
            stack.TryAdd(A, 9, out var lease);
            lease.Release();
            lease.Release();
            Assert.That(lease.IsReleased, Is.True);
            Assert.That(changes, Is.EqualTo(new[] { 9, 5 }));
        }

        [Test]
        public void Exclusive_RejectsSecondOwner_AllowsSameOwner()
        {
            var stack = new ModifierStack<int>(0, ConflictPolicy.Exclusive);
            stack.TryAdd(A, 1, out _);

            Assert.That(stack.CanAdd(B), Is.False);
            Assert.That(stack.TryAdd(B, 2, out var rejected), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(stack.TryAdd(A, 3, out _), Is.True, "same owner may layer modifiers");
            Assert.That(stack.Effective, Is.EqualTo(3));
        }

        [Test]
        public void InvalidOwner_IsRejected()
        {
            var stack = new ModifierStack<int>(0, ConflictPolicy.Stack);
            Assert.That(stack.TryAdd(OperationId.None, 1, out _), Is.False);
        }

        [Test]
        public void Stack_HighestPriorityWins_TieGoesToNewest()
        {
            var stack = new ModifierStack<int>(0, ConflictPolicy.Stack);
            stack.TryAdd(A, 10, out var low, priority: 0);
            stack.TryAdd(B, 20, out var high, priority: 5);
            Assert.That(stack.Effective, Is.EqualTo(20));

            stack.TryAdd(new OperationId(3), 30, out var newestLow, priority: 0);
            Assert.That(stack.Effective, Is.EqualTo(20), "priority beats recency");

            high.Release();
            Assert.That(stack.Effective, Is.EqualTo(30), "newest of equal priority");
            newestLow.Release();
            Assert.That(stack.Effective, Is.EqualTo(10));
            low.Release();
            Assert.That(stack.Effective, Is.EqualTo(0));
        }

        [Test]
        public void ReleasingOlderModifier_DoesNotUndoNewerOne()
        {
            // The core reason for stacks instead of snapshots.
            var stack = new ModifierStack<int>(0, ConflictPolicy.Stack);
            stack.TryAdd(A, 1, out var first);
            stack.TryAdd(B, 2, out _);

            first.Release();

            Assert.That(stack.Effective, Is.EqualTo(2));
        }

        [Test]
        public void BaseChangedWhileModified_IsWhatComesBack()
        {
            var stack = new ModifierStack<int>(0, ConflictPolicy.Exclusive);
            stack.TryAdd(A, 9, out var lease);

            stack.Base = 4; // gameplay changed the authoritative value meanwhile
            Assert.That(stack.Effective, Is.EqualTo(9));
            lease.Release();

            Assert.That(stack.Effective, Is.EqualTo(4), "no stale snapshot restored");
        }

        [Test]
        public void NoChangeNotification_WhenEffectiveValueIsUnchanged()
        {
            int calls = 0;
            var stack = new ModifierStack<int>(5, ConflictPolicy.Stack, _ => calls++);
            stack.TryAdd(A, 5, out var lease);
            lease.Release();
            stack.Base = 5;
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void IsModifiedBy_TracksOwners()
        {
            var stack = new ModifierStack<int>(0, ConflictPolicy.Stack);
            stack.TryAdd(A, 1, out var lease);
            Assert.That(stack.IsModifiedBy(A), Is.True);
            Assert.That(stack.IsModifiedBy(B), Is.False);
            lease.Release();
            Assert.That(stack.IsModifiedBy(A), Is.False);
        }
    }
}
