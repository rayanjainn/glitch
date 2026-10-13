using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class ContainmentDirectorTests : PlayModeTestBase
    {
        private const EntityCapabilities Transferable = EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable;

        private GameServices services;
        private ContainmentDirector director;
        private ContainmentSettingsAsset settings;
        private GlitchSpec spec;
        private IGlitchEffect effect;
        // Explicit clock: each transfer happens 1 s after the previous one, past the 0.5 s cooldown.
        private float clock;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            settings = Track(ScriptableObject.CreateInstance<ContainmentSettingsAsset>());
            var go = Track(new GameObject("Containment"));
            go.SetActive(false);
            director = go.AddComponent<ContainmentDirector>();
            director.Configure(settings);
            go.SetActive(true);

            var definition = Track(ScriptableObject.CreateInstance<CollisionTransferDefinition>());
            definition.ApplyDefaults(); // containment weight 1
            spec = definition.ToSpec();
            effect = definition.CreateEffect(services);
        }

        private CollisionCapability Capability(string name, Vector3 position, CollisionMode mode)
        {
            var go = Box(name, position, new Vector3(2, 2, 0.5f));
            go.SetActive(false);
            go.layer = GameLayers.Index(mode == CollisionMode.Solid ? GameLayers.World : GameLayers.Intangible);
            go.AddComponent<GameEntity>().Configure(name, EntityCategory.Structure, Transferable);
            go.AddComponent<CollisionCapability>().Configure(go.GetComponents<Collider>(), mode);
            go.SetActive(true);
            Physics.SyncTransforms();
            return go.GetComponent<CollisionCapability>();
        }

        private GlitchOperation Transfer(CollisionCapability a, CollisionCapability b)
        {
            clock += 1f;
            var r = services.Glitches.TryApply(spec, effect, new[] { a.GetComponent<GameEntity>().Id, b.GetComponent<GameEntity>().Id }, clock);
            Assert.That(r.Succeeded, Is.True, r.Describe());
            return r.Operation;
        }

        [Test]
        public void AppliedGlitches_RaisePressure()
        {
            var wall = Capability("WALL", new Vector3(0, 1, 3), CollisionMode.Solid);
            var pad = Capability("PAD", new Vector3(8, 1, 0), CollisionMode.Intangible);
            Transfer(wall, pad);
            Assert.That(services.Containment.Pressure, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void NoGlitches_PressureDecaysToCalm()
        {
            services.Containment.ReportGlitchApplied(3f); // 30 → stage 1
            for (int i = 0; i < 60; i++) director.Step(1f);
            Assert.That(services.Containment.Stage, Is.EqualTo(0));
            Assert.That(services.Containment.Pressure, Is.EqualTo(0f));
        }

        [Test]
        public void HigherStages_MakeGlitchesSlowerToReuse()
        {
            services.Containment.ReportGlitchApplied(5f); // 50 → stage 2 (cooldown x1.5 by default)
            director.Step(0.01f);
            Assert.That(services.Glitches.CooldownScale, Is.EqualTo(1.5f).Within(1e-4f));
        }

        [Test]
        public void PatchDeployment_RevertsOldestGlitch_AfterWarningInterval()
        {
            var wallA = Capability("WALL_A", new Vector3(0, 1, 3), CollisionMode.Solid);
            var padA = Capability("PAD_A", new Vector3(8, 1, 0), CollisionMode.Intangible);
            var wallB = Capability("WALL_B", new Vector3(0, 1, -3), CollisionMode.Solid);
            var padB = Capability("PAD_B", new Vector3(8, 1, 6), CollisionMode.Intangible);
            var oldest = Transfer(wallA, padA);
            var newest = Transfer(wallB, padB);
            GlitchOperation patched = null;
            director.Patched += op => patched = op;

            services.Containment.ReportGlitchApplied(5.5f); // → stage 3 (patch every 6 s by default)
            director.Step(0.01f);
            Assume.That(services.Containment.Stage, Is.EqualTo(3));
            director.Step(5f);
            Assert.That(patched, Is.Null, "players get one full interval of warning");

            director.Step(1.1f);

            Assert.That(patched, Is.SameAs(oldest));
            Assert.That(oldest.EndReason, Is.EqualTo(GlitchEndReason.Patched));
            Assert.That(newest.IsActive, Is.True);
            Assert.That(wallA.EffectiveMode, Is.EqualTo(CollisionMode.Solid));
        }

        [Test]
        public void BlockedPatch_MovesOnToTheNextGlitch()
        {
            var wallA = Capability("WALL_A", new Vector3(0, 1, 3), CollisionMode.Solid);
            var padA = Capability("PAD_A", new Vector3(8, 1, 0), CollisionMode.Intangible);
            var wallB = Capability("WALL_B", new Vector3(0, 1, -3), CollisionMode.Solid);
            var padB = Capability("PAD_B", new Vector3(8, 1, 6), CollisionMode.Intangible);
            var oldest = Transfer(wallA, padA);
            var newest = Transfer(wallB, padB);
            // Someone stands inside WALL_A (now intangible): restoring it would trap them.
            var occupant = Track(new GameObject("Occupant"));
            occupant.layer = GameLayers.Index(GameLayers.Hero);
            occupant.transform.position = new Vector3(0, 1, 3);
            occupant.AddComponent<CharacterController>();
            Physics.SyncTransforms();

            Assert.That(director.TryPatch(), Is.True);

            Assert.That(oldest.IsActive, Is.True, "never patch something around a person");
            Assert.That(newest.EndReason, Is.EqualTo(GlitchEndReason.Patched));
        }

        [Test]
        public void Patch_RelievesPressure()
        {
            var wall = Capability("WALL", new Vector3(0, 1, 3), CollisionMode.Solid);
            var pad = Capability("PAD", new Vector3(8, 1, 0), CollisionMode.Intangible);
            Transfer(wall, pad); // 10
            services.Containment.ReportGlitchApplied(6f); // 70
            float before = services.Containment.Pressure;

            director.TryPatch();

            Assert.That(services.Containment.Pressure, Is.EqualTo(before - 15f).Within(1e-3f));
        }

        [Test]
        public void NothingToPatch_ReturnsFalse()
        {
            Assert.That(director.TryPatch(), Is.False);
        }
    }
}
