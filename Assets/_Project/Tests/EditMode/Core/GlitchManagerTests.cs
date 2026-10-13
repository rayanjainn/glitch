using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Core.Interaction;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class GlitchManagerTests
    {
        private EntityRegistry registry;
        private GlitchManager manager;
        private FakeGlitchEffect effect;
        private FakeEntity a;
        private FakeEntity b;
        private FakeEntity plain;

        private static readonly TargetRequirement Transferable =
            new TargetRequirement(EntityCapabilities.CollisionTransferable, EntityCategory.Anomaly);

        [SetUp]
        public void SetUp()
        {
            registry = new EntityRegistry();
            manager = new GlitchManager(registry);
            effect = new FakeGlitchEffect();
            var caps = EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable;
            a = new FakeEntity("A", EntityCategory.Structure, caps).RegisterIn(registry);
            b = new FakeEntity("B", EntityCategory.Decoration, caps).RegisterIn(registry);
            plain = new FakeEntity("Crate", EntityCategory.Prop, EntityCapabilities.Inspectable).RegisterIn(registry);
        }

        [TearDown]
        public void TearDown() => manager.Dispose();

        private static GlitchSpec Spec(int arity = 2, float duration = 0f, float cooldown = 0f, int maxActive = 0, string id = "collision")
        {
            var slots = new TargetRequirement[arity];
            for (int i = 0; i < arity; i++) slots[i] = Transferable;
            return new GlitchSpec(id, id.ToUpperInvariant(), slots, duration, cooldown, maxActive);
        }

        private GlitchResult ApplyAB(GlitchSpec spec = null, float now = 0f)
            => manager.TryApply(spec ?? Spec(), effect, new[] { a.Id, b.Id }, now);

        // ---------------------------------------------------------------- apply

        [Test]
        public void Apply_Valid_ChangesTargetsAndRaisesStarted()
        {
            GlitchOperation started = null;
            manager.OperationStarted += op => started = op;

            var result = ApplyAB();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(started, Is.SameAs(result.Operation));
            Assert.That(result.Operation.IsActive, Is.True);
            Assert.That(effect.Channel(a.Id).Effective, Is.EqualTo(1));
            Assert.That(effect.Channel(b.Id).Effective, Is.EqualTo(1));
            Assert.That(manager.ActiveCount, Is.EqualTo(1));
            Assert.That(manager.IsTargeted(a.Id), Is.True);
            Assert.That(manager.IsTargeted(plain.Id), Is.False);
        }

        [Test]
        public void Apply_WrongArity_Rejected()
        {
            var r = manager.TryApply(Spec(), effect, new[] { a.Id }, 0f);
            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.WrongTargetCount));
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Apply_SameTargetTwice_Rejected()
        {
            var r = manager.TryApply(Spec(), effect, new[] { a.Id, a.Id }, 0f);
            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.DuplicateTarget));
            StringAssert.Contains("DIFFER", r.Describe());
        }

        [Test]
        public void Apply_TargetMissingCapability_ReportsSlotAndReason()
        {
            var r = manager.TryApply(Spec(), effect, new[] { a.Id, plain.Id }, 0f);
            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.InvalidTarget));
            Assert.That(r.TargetIndex, Is.EqualTo(1));
            Assert.That(r.TargetValidation.MissingCapabilities, Is.EqualTo(EntityCapabilities.CollisionTransferable));
            StringAssert.StartsWith("TARGET ", r.Describe());
        }

        [Test]
        public void Apply_DestroyedOrNoTarget_Rejected()
        {
            a.IsAlive = false;
            Assert.That(ApplyAB().TargetValidation.Failure, Is.EqualTo(TargetValidationFailure.Destroyed));
            var none = manager.TryApply(Spec(), effect, new[] { GameEntityId.None, b.Id }, 0f);
            Assert.That(none.TargetValidation.Failure, Is.EqualTo(TargetValidationFailure.NoTarget));
        }

        [Test]
        public void Apply_EffectRejects_NothingChanges()
        {
            effect.RejectReason = "OCCUPIED";
            var r = ApplyAB();
            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.EffectRejected));
            Assert.That(r.Describe(), Is.EqualTo("OCCUPIED"));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
        }

        [Test]
        public void Apply_PartialFailure_ReleasesWhatWasApplied()
        {
            effect.FailAfterFirstLease = true;
            var r = ApplyAB();
            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.EffectFailed));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False, "partial apply must not leak");
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Apply_EffectThrows_IsContainedAndReported()
        {
            effect.ThrowOnApply = true;
            int errors = 0;
            manager.EffectError += (_, __) => errors++;

            var r = ApplyAB();

            Assert.That(r.Failure, Is.EqualTo(GlitchFailure.EffectFailed));
            Assert.That(errors, Is.EqualTo(1));
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Apply_ConflictOnSameChannel_Rejected()
        {
            ApplyAB();
            var second = manager.TryApply(Spec(arity: 1), effect, new[] { a.Id }, 0f);
            Assert.That(second.Failure, Is.EqualTo(GlitchFailure.EffectRejected));
            Assert.That(second.Detail, Is.EqualTo("ALREADY GLITCHED"));
        }

        [Test]
        public void Validate_HasNoSideEffects()
        {
            var r = manager.Validate(Spec(), effect, new[] { a.Id, b.Id }, 0f);
            Assert.That(r.Succeeded, Is.True);
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
        }

        [Test]
        public void OperationIds_AreUniqueAndNeverReused()
        {
            var first = ApplyAB().Operation.Id;
            manager.RevertLatest();
            var second = ApplyAB().Operation.Id;
            Assert.That(second, Is.Not.EqualTo(first));
        }

        // ---------------------------------------------------------------- limits

        [Test]
        public void Cooldown_BlocksUntilElapsed()
        {
            var spec = Spec(cooldown: 2f);
            ApplyAB(spec, now: 10f);
            manager.RevertLatest();

            var early = ApplyAB(spec, now: 11f);
            Assert.That(early.Failure, Is.EqualTo(GlitchFailure.OnCooldown));
            Assert.That(manager.CooldownRemaining("collision", 11f), Is.EqualTo(1f).Within(1e-4f));

            Assert.That(ApplyAB(spec, now: 12f).Succeeded, Is.True);
        }

        [Test]
        public void FailedApply_DoesNotStartCooldown()
        {
            var spec = Spec(cooldown: 5f);
            effect.RejectReason = "NO";
            ApplyAB(spec, 0f);
            Assert.That(manager.CooldownRemaining("collision", 0f), Is.EqualTo(0f));
        }

        [Test]
        public void MaxActive_IsEnforcedPerGlitch()
        {
            var c = new FakeEntity("C", EntityCategory.Prop, EntityCapabilities.CollisionTransferable).RegisterIn(registry);
            var spec = Spec(arity: 1, maxActive: 2);
            Assert.That(manager.TryApply(spec, effect, new[] { a.Id }, 0f).Succeeded, Is.True);
            Assert.That(manager.TryApply(spec, effect, new[] { b.Id }, 0f).Succeeded, Is.True);
            Assert.That(manager.TryApply(spec, effect, new[] { c.Id }, 0f).Failure, Is.EqualTo(GlitchFailure.LimitReached));
            Assert.That(manager.TryApply(Spec(arity: 1, id: "other"), effect, new[] { c.Id }, 0f).Succeeded, Is.True, "limit is per glitch");
        }

        // ---------------------------------------------------------------- ending

        [Test]
        public void Revert_RestoresAndRaisesEndedOnce()
        {
            var op = ApplyAB().Operation;
            var ended = new List<GlitchEndReason>();
            manager.OperationEnded += (_, r) => ended.Add(r);

            Assert.That(manager.Revert(op.Id).Succeeded, Is.True);
            Assert.That(manager.Revert(op.Id).Failure, Is.EqualTo(GlitchFailure.NotFound));

            Assert.That(effect.Channel(a.Id).Effective, Is.EqualTo(0));
            Assert.That(effect.Channel(b.Id).Effective, Is.EqualTo(0));
            Assert.That(op.State, Is.EqualTo(OperationState.Ended));
            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.Reverted));
            Assert.That(ended, Is.EqualTo(new[] { GlitchEndReason.Reverted }));
            Assert.That(effect.Ended.Count, Is.EqualTo(1));
        }

        [Test]
        public void RevertLatest_IsLifoOverActiveOperations()
        {
            var c = new FakeEntity("C", EntityCategory.Prop, EntityCapabilities.CollisionTransferable).RegisterIn(registry);
            var spec = Spec(arity: 1);
            var first = manager.TryApply(spec, effect, new[] { a.Id }, 0f).Operation;
            var second = manager.TryApply(spec, effect, new[] { b.Id }, 0f).Operation;
            var third = manager.TryApply(spec, effect, new[] { c.Id }, 0f).Operation;
            manager.Revert(third.Id);

            Assert.That(manager.RevertLatest().Operation, Is.SameAs(second));
            Assert.That(manager.RevertLatest().Operation, Is.SameAs(first));
            Assert.That(manager.RevertLatest().Failure, Is.EqualTo(GlitchFailure.NotFound));
        }

        [Test]
        public void Tick_ExpiresOnlyElapsedOperations()
        {
            var timed = ApplyAB(Spec(duration: 5f), now: 10f).Operation;
            Assert.That(timed.Remaining(12f), Is.EqualTo(3f).Within(1e-4f));

            manager.Tick(14.9f);
            Assert.That(timed.IsActive, Is.True);
            manager.Tick(15f);
            Assert.That(timed.EndReason, Is.EqualTo(GlitchEndReason.Expired));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
        }

        [Test]
        public void UnlimitedDuration_NeverExpires()
        {
            var op = ApplyAB().Operation;
            manager.Tick(1e9f);
            Assert.That(op.IsActive, Is.True);
            Assert.That(float.IsPositiveInfinity(op.Remaining(5f)), Is.True);
        }

        [Test]
        public void EndBlocked_KeepsOperationActive_ExpiryRetriesLater()
        {
            var op = ApplyAB(Spec(duration: 1f)).Operation;
            effect.BlockEnd = "OCCUPIED";

            var revert = manager.Revert(op.Id);
            Assert.That(revert.Failure, Is.EqualTo(GlitchFailure.EndBlocked));
            Assert.That(revert.Describe(), Is.EqualTo("OCCUPIED"));
            manager.Tick(2f);
            Assert.That(op.IsActive, Is.True, "blocked expiry waits");

            effect.BlockEnd = null;
            manager.Tick(3f);
            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.Expired));
        }

        [Test]
        public void ForcedReasons_IgnoreVeto()
        {
            var op = ApplyAB().Operation;
            effect.BlockEnd = "OCCUPIED";
            manager.EndAll(GlitchEndReason.SceneUnload);
            Assert.That(op.IsActive, Is.False);
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
        }

        [Test]
        public void TargetUnregistered_EndsOperationAndRestoresSurvivor()
        {
            var op = ApplyAB().Operation;
            registry.Unregister(b.Id);

            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False, "surviving target restored");
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void TargetDestroyedSilently_EndsOnNextRegistryLookup()
        {
            var op = ApplyAB().Operation;
            b.IsAlive = false;
            registry.PruneDead();
            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
        }

        [Test]
        public void UnrelatedEntityRemoval_DoesNotAffectOperations()
        {
            var op = ApplyAB().Operation;
            registry.Unregister(plain.Id);
            Assert.That(op.IsActive, Is.True);
        }

        [Test]
        public void OnEndedThrowing_StillCompletesBookkeeping()
        {
            var op = ApplyAB().Operation;
            effect.ThrowOnEnded = true;
            int errors = 0;
            manager.EffectError += (_, __) => errors++;

            Assert.That(manager.Revert(op.Id).Succeeded, Is.True);
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
            Assert.That(errors, Is.EqualTo(1));
        }

        [Test]
        public void CascadingEnd_FromOnEnded_IsSafe()
        {
            var c = new FakeEntity("C", EntityCategory.Prop, EntityCapabilities.CollisionTransferable).RegisterIn(registry);
            var spec = Spec(arity: 1);
            var first = manager.TryApply(spec, effect, new[] { a.Id }, 0f).Operation;
            var second = manager.TryApply(spec, effect, new[] { c.Id }, 0f).Operation;
            // Ending the first unregisters the second's target (like a duplicate despawning, Milestone 3).
            effect.OnEndedHook = (op, _) => { if (op == first) registry.Unregister(c.Id); };

            Assert.DoesNotThrow(() => manager.EndAll(GlitchEndReason.Reverted));
            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.False);
            Assert.That(manager.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_EndsEverythingAndRejectsFurtherWork()
        {
            var op = ApplyAB().Operation;
            manager.Dispose();

            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.SceneUnload));
            Assert.That(effect.Channel(a.Id).IsModified, Is.False);
            Assert.That(ApplyAB().Failure, Is.EqualTo(GlitchFailure.ManagerDisposed));
            Assert.DoesNotThrow(() => registry.Unregister(a.Id));
        }

        [Test]
        public void AddLease_FromAnotherOperation_Throws()
        {
            var op = ApplyAB().Operation;
            var stack = new ModifierStack<int>(0, ConflictPolicy.Stack);
            stack.TryAdd(new OperationId(999), 1, out var foreign);
            Assert.Throws<System.ArgumentException>(() => op.AddLease(foreign));
        }

        [Test]
        public void Spec_RejectsInvalidConfiguration()
        {
            Assert.Throws<System.ArgumentException>(() => new GlitchSpec("", "x", new[] { Transferable }));
            Assert.Throws<System.ArgumentException>(() => new GlitchSpec("x", "x", new TargetRequirement[0]));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GlitchSpec("x", "x", new[] { Transferable }, duration: -1f));
        }
    }
}
