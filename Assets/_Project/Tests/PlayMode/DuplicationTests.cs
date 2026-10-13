using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Core.Interaction;
using BugReport.Glitches;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class DuplicationTests : PlayModeTestBase
    {
        private const EntityCapabilities CrateCaps = EntityCapabilities.Inspectable | EntityCapabilities.Duplicable | EntityCapabilities.Freezable;

        private GameServices services;
        private DuplicationDefinition definition;
        private DuplicationProfile profile;
        private GlitchSpec spec;
        private IGlitchEffect effect;
        private GameObject template;

        [SetUp]
        public void SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
            Ground();

            profile = Track(ScriptableObject.CreateInstance<DuplicationProfile>());
            // Stand-in for a prefab asset: an inactive object that never runs.
            template = Crate("Template", new Vector3(0, -50, 0), CrateCaps, active: false);
            profile.Configure(template, 2);

            definition = Track(ScriptableObject.CreateInstance<DuplicationDefinition>());
            definition.ApplyDefaults();
            Rebuild();
        }

        private void Rebuild()
        {
            spec = definition.ToSpec();
            effect = definition.CreateEffect(services);
        }

        private GameObject Crate(string name, Vector3 position, EntityCapabilities caps, bool active = true, bool withProfile = true)
        {
            var go = Box(name, position, Vector3.one);
            go.SetActive(false);
            go.AddComponent<Rigidbody>();
            go.AddComponent<GameEntity>().Configure(name, EntityCategory.Prop, caps);
            if (withProfile) go.AddComponent<DuplicationCapability>().Configure(profile);
            go.AddComponent<FreezeCapability>();
            if (active) go.SetActive(true);
            Physics.SyncTransforms();
            return go;
        }

        private static GameEntityId Id(GameObject go) => go.GetComponent<GameEntity>().Id;

        private GlitchResult Duplicate(GameObject source, float now = 0f)
            => services.Glitches.TryApply(spec, effect, new[] { Id(source) }, now);

        private GameEntity CopyOf(GlitchResult result)
        {
            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(services.Registry.TryGet(result.Operation.Product(0), out var copy), Is.True);
            return (GameEntity)copy;
        }

        [Test]
        public void CreatesRegisteredCopy_WithNewIdentityAndLineage()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);

            var copy = CopyOf(Duplicate(source));

            Assert.That(copy.Id, Is.Not.EqualTo(Id(source)));
            Assert.That(copy.gameObject, Is.Not.SameAs(source));
            StringAssert.Contains("CRATE_A COPY", copy.DisplayName);
            var marker = copy.GetComponent<DuplicateMarker>();
            Assert.That(marker.Origin, Is.EqualTo(Id(source)));
            Assert.That(marker.Generation, Is.EqualTo(1));
            Assert.That(Vector3.Distance(copy.transform.position, source.transform.position), Is.GreaterThan(0.9f), "placed beside, not inside");
            Assert.That(copy.transform.parent, Is.Null, "temporary spawn holder is not left as parent");
        }

        [Test]
        public void CopyOfFrozenSource_IsNotFrozen()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var freezeDef = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDef.ApplyDefaults();
            services.Glitches.TryApply(freezeDef.ToSpec(), freezeDef.CreateEffect(services), new[] { Id(source) }, 0f);
            Assume.That(source.GetComponent<Rigidbody>().isKinematic, Is.True);

            var copy = CopyOf(Duplicate(source));

            Assert.That(copy.GetComponent<Rigidbody>().isKinematic, Is.False, "glitch state is never cloned");
            Assert.That(copy.GetComponent<FreezeCapability>().IsFrozen, Is.False);
        }

        [Test]
        public void Copy_HasIndependentState()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var copy = CopyOf(Duplicate(source));
            var freezeDef = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDef.ApplyDefaults();

            services.Glitches.TryApply(freezeDef.ToSpec(), freezeDef.CreateEffect(services), new[] { copy.Id }, 0f);

            Assert.That(copy.GetComponent<FreezeCapability>().IsFrozen, Is.True);
            Assert.That(source.GetComponent<FreezeCapability>().IsFrozen, Is.False);
        }

        [Test]
        public void PerSourceCopyLimit_IsEnforced()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            Assert.That(Duplicate(source, 0f).Succeeded, Is.True);
            Assert.That(Duplicate(source, 1f).Succeeded, Is.True);

            var third = Duplicate(source, 2f);

            StringAssert.Contains("COPY LIMIT", third.Describe());
        }

        [Test]
        public void GlobalLimit_IsEnforced()
        {
            definition.Configure("duplication", "DUPLICATION", "", Color.magenta, 0f, 0f, newMaxActive: 1,
                new[] { new GlitchDefinition.TargetSlot("SOURCE", EntityCapabilities.Duplicable) });
            Rebuild();
            var a = Crate("A", new Vector3(0, 0.5f, 0), CrateCaps);
            var b = Crate("B", new Vector3(0, 0.5f, 6), CrateCaps);
            Duplicate(a);

            Assert.That(Duplicate(b).Failure, Is.EqualTo(GlitchFailure.LimitReached));
        }

        [Test]
        public void Revert_RemovesCopy_AndEndsGlitchesOnIt()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var dup = Duplicate(source);
            var copy = CopyOf(dup);
            var freezeDef = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDef.ApplyDefaults();
            var freezeOp = services.Glitches.TryApply(freezeDef.ToSpec(), freezeDef.CreateEffect(services), new[] { copy.Id }, 0f).Operation;

            services.Glitches.Revert(dup.Operation.Id);

            Assert.That(copy == null || !copy.gameObject.activeSelf, Is.True, "copy despawned");
            Assert.That(services.Registry.Contains(dup.Operation.Product(0)), Is.False);
            Assert.That(freezeOp.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void CopyDestroyedElsewhere_EndsDuplicationOperation()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var dup = Duplicate(source);

            Object.DestroyImmediate(CopyOf(dup).gameObject);

            Assert.That(dup.Operation.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(services.Glitches.CountActiveOn("duplication", Id(source)), Is.EqualTo(0), "copy budget is freed");
        }

        [Test]
        public void NoSpace_IsRejected()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            Box("R", new Vector3(1.1f, 0.5f, 0), Vector3.one);
            Box("L", new Vector3(-1.1f, 0.5f, 0), Vector3.one);
            Box("F", new Vector3(0, 0.5f, 1.1f), Vector3.one);
            Box("B", new Vector3(0, 0.5f, -1.1f), Vector3.one);
            Box("U", new Vector3(0, 1.6f, 0), Vector3.one);
            Physics.SyncTransforms();

            StringAssert.Contains("NO SPACE", Duplicate(source).Describe());
            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void NonDuplicable_IsRejected()
        {
            var plain = Crate("WALL", new Vector3(0, 0.5f, 0), EntityCapabilities.Inspectable, withProfile: false);
            Assert.That(Duplicate(plain).TargetValidation.Failure, Is.EqualTo(TargetValidationFailure.MissingCapability));

            var misconfigured = Crate("BROKEN", new Vector3(0, 0.5f, 6), CrateCaps, withProfile: false);
            StringAssert.Contains("NO DUPLICATION PROFILE", Duplicate(misconfigured).Describe());
        }

        [Test]
        public void ObjectiveFlag_IsNotCopiedByDefault()
        {
            var key = Crate("KEYCARD", new Vector3(0, 0.5f, 0), CrateCaps | EntityCapabilities.ObjectiveRelevant);
            var copy = CopyOf(Duplicate(key));
            Assert.That(copy.Capabilities.HasAll(EntityCapabilities.ObjectiveRelevant), Is.False);
            Assert.That(copy.Capabilities.HasAll(EntityCapabilities.Duplicable), Is.True);
        }

        [Test]
        public void CopyOfCopy_TracksGeneration_AndCollapsesWithItsParent()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var first = Duplicate(source, 0f);
            var copy1 = CopyOf(first);
            var second = Duplicate(copy1.gameObject, 1f);
            var copy2 = CopyOf(second);
            Assert.That(copy2.GetComponent<DuplicateMarker>().Generation, Is.EqualTo(2));

            services.Glitches.Revert(first.Operation.Id);

            Assert.That(second.Operation.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(copy2 == null || !copy2.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void LevelTeardown_RemovesAllCopies()
        {
            var source = Crate("CRATE_A", new Vector3(0, 0.5f, 0), CrateCaps);
            var copy = CopyOf(Duplicate(source));

            services.Dispose();

            Assert.That(copy == null || !copy.gameObject.activeSelf, Is.True);
        }
    }
}
