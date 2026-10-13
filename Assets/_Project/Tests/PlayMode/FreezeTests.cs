using System.Collections;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Core.Interaction;
using BugReport.Glitches;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BugReport.Tests.PlayMode
{
    public class FreezeTests : PlayModeTestBase
    {
        private const EntityCapabilities Freezable = EntityCapabilities.Inspectable | EntityCapabilities.Freezable;

        private GameServices services;
        private FreezeDefinition definition;
        private GlitchSpec spec;
        private IGlitchEffect effect;

        [SetUp]
        public void SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
            definition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            definition.ApplyDefaults();
            Rebuild();
        }

        private void Rebuild()
        {
            spec = definition.ToSpec();
            effect = definition.CreateEffect(services);
        }

        private FreezeCapability Body(string name, Vector3 position, bool kinematic = false, EntityCategory category = EntityCategory.Prop)
        {
            var go = Box(name, position, Vector3.one);
            go.SetActive(false);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = kinematic;
            go.AddComponent<GameEntity>().Configure(name, category, Freezable);
            var freeze = go.AddComponent<FreezeCapability>();
            go.SetActive(true);
            return freeze;
        }

        private GlitchResult Freeze(Component target, float now = 0f)
            => services.Glitches.TryApply(spec, effect, new[] { target.GetComponent<GameEntity>().Id }, now);

        private static IEnumerator FixedFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator FrozenBody_StaysPut_WhileTheRestOfTheWorldMoves()
        {
            var frozen = Body("Frozen", new Vector3(0, 10, 0));
            var free = Body("Free", new Vector3(3, 10, 0));
            Assert.That(Freeze(frozen).Succeeded, Is.True);

            yield return FixedFrames(20);

            Assert.That(frozen.transform.position.y, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(free.transform.position.y, Is.LessThan(9.5f), "local freeze, not a global pause");
            Assert.That(frozen.IsFrozen, Is.True);
        }

        [UnityTest]
        public IEnumerator ForcesWhileFrozen_DoNotAccumulate()
        {
            var target = Body("Target", new Vector3(0, 10, 0));
            var op = Freeze(target).Operation;
            for (int i = 0; i < 20; i++)
            {
                target.Body.AddForce(Vector3.up * 1000f, ForceMode.Force);
                yield return new WaitForFixedUpdate();
            }

            services.Glitches.Revert(op.Id);

            Assert.That(target.Body.isKinematic, Is.False);
            Assert.That(target.Body.linearVelocity.magnitude, Is.LessThan(1e-3f), "released at rest");
            yield return FixedFrames(2);
            Assert.That(target.Body.linearVelocity.y, Is.LessThan(0.5f), "no stored-up force kicks in");
        }

        [Test]
        public void Unfreeze_RestoresTheKinematicFlagItFound()
        {
            var dynamicBody = Body("Dynamic", new Vector3(0, 10, 0));
            var kinematicBody = Body("Kinematic", new Vector3(3, 10, 0), kinematic: true);
            Freeze(dynamicBody);
            Freeze(kinematicBody);

            services.Glitches.EndAll(GlitchEndReason.Reverted);

            Assert.That(dynamicBody.Body.isKinematic, Is.False);
            Assert.That(kinematicBody.Body.isKinematic, Is.True);
        }

        [Test]
        public void RestoreMomentum_GivesBackClampedVelocity()
        {
            definition.ConfigureRelease(FreezeRelease.RestoreMomentum, 2f);
            Rebuild();
            var target = Body("Target", new Vector3(0, 10, 0));
            target.Body.useGravity = false;
            target.Body.linearVelocity = new Vector3(5f, 0f, 0f);
            var op = Freeze(target).Operation;

            services.Glitches.Revert(op.Id);

            Assert.That(target.Body.linearVelocity.magnitude, Is.EqualTo(2f).Within(1e-3f));
            Assert.That(target.Body.linearVelocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void ScriptedMover_StopsAndResumesFromTheSamePhase()
        {
            var go = Box("Lift", Vector3.zero, Vector3.one);
            go.SetActive(false);
            go.AddComponent<Rigidbody>();
            go.AddComponent<GameEntity>().Configure("LIFT", EntityCategory.SystemDevice, Freezable);
            go.AddComponent<FreezeCapability>();
            var mover = go.AddComponent<PingPongMover>();
            mover.Configure(Vector3.up * 2f, 4f);
            mover.AutoStep = false;
            go.SetActive(true);

            mover.Step(1f);
            Assert.That(mover.Phase, Is.EqualTo(0.25f).Within(1e-4f));
            var op = Freeze(mover).Operation;
            mover.Step(1f);
            Assert.That(mover.Phase, Is.EqualTo(0.25f).Within(1e-4f), "frozen: no progress");
            services.Glitches.Revert(op.Id);
            mover.Step(1f);
            Assert.That(mover.Phase, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void SecondFreeze_IsRejected()
        {
            var target = Body("Target", new Vector3(0, 10, 0));
            Freeze(target);
            var second = Freeze(target, now: 5f);
            StringAssert.Contains("ALREADY FROZEN", second.Describe());
        }

        [Test]
        public void Expiry_Unfreezes()
        {
            var target = Body("Target", new Vector3(0, 10, 0));
            Freeze(target, now: 0f);
            services.Glitches.Tick(7.9f);
            Assert.That(target.IsFrozen, Is.True);
            services.Glitches.Tick(8f);
            Assert.That(target.IsFrozen, Is.False);
        }

        [Test]
        public void DestroyedWhileFrozen_EndsCleanly()
        {
            var target = Body("Target", new Vector3(0, 10, 0));
            var op = Freeze(target).Operation;
            Assert.DoesNotThrow(() => Object.DestroyImmediate(target.gameObject));
            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Hero_CannotBeFrozen()
        {
            var hero = Body("Hero", new Vector3(0, 10, 0), category: EntityCategory.Hero);
            var result = Freeze(hero);
            Assert.That(result.TargetValidation.Failure, Is.EqualTo(TargetValidationFailure.CategoryNotAllowed));
        }

        [Test]
        public void NothingToFreeze_IsRejected()
        {
            var go = Box("Statue", new Vector3(0, 1, 0), Vector3.one);
            go.SetActive(false);
            go.AddComponent<GameEntity>().Configure("STATUE", EntityCategory.Decoration, Freezable);
            go.AddComponent<FreezeCapability>();
            go.SetActive(true);

            StringAssert.Contains("NOTHING TO FREEZE", Freeze(go.transform).Describe());
        }

        [Test]
        public void FrozenDynamicBody_StillCountsAsDynamicForCollisionTransfer()
        {
            // Otherwise: freeze a crate, make it intangible, unfreeze → it falls through the floor.
            var target = Body("Crate", new Vector3(0, 0.5f, 0));
            target.gameObject.SetActive(false);
            target.gameObject.AddComponent<CollisionCapability>().Configure(target.GetComponents<Collider>(), CollisionMode.Solid);
            target.gameObject.SetActive(true);
            Freeze(target);

            Assert.That(target.Body.isKinematic, Is.True);
            Assert.That(target.GetComponent<CollisionCapability>().HasDynamicBody, Is.True);
        }
    }
}
