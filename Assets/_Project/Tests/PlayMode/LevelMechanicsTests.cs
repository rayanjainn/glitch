using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class LevelMechanicsTests : PlayModeTestBase
    {
        private GameServices services;
        private int world;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            world = GameLayers.Index(GameLayers.World);
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
            Ground().layer = world;
        }

        private GameObject T(GameObject go) => Track(go);

        private PressurePlate Plate(Vector3 position)
        {
            var go = Track(new GameObject("Plate", typeof(BoxCollider)));
            go.transform.position = position + Vector3.up * 0.5f;
            go.GetComponent<BoxCollider>().size = new Vector3(1f, 0.8f, 1f);
            var plate = go.AddComponent<PressurePlate>();
            Physics.SyncTransforms();
            return plate;
        }

        private Rigidbody Weight(Vector3 position, float mass, bool kinematic = false)
        {
            var go = Box("Weight", position, Vector3.one * 0.8f);
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.isKinematic = kinematic;
            Physics.SyncTransforms();
            return body;
        }

        // ---------------------------------------------------------------- plates & gate

        [Test]
        public void Plate_PressedByHeavyBody_NotByLightOrCharacters()
        {
            var plate = Plate(Vector3.zero);
            Assert.That(plate.Evaluate(), Is.False);

            var light = Weight(new Vector3(0, 0.5f, 0), 1f);
            Assert.That(plate.Evaluate(), Is.False, "below min mass");
            light.mass = 10f;
            Assert.That(plate.Evaluate(), Is.True);
        }

        [Test]
        public void Plate_CountsFrozenKinematicBodies()
        {
            var plate = Plate(Vector3.zero);
            Weight(new Vector3(0, 0.5f, 0), 10f, kinematic: true);
            Assert.That(plate.Evaluate(), Is.True);
        }

        [Test]
        public void Gate_OpensOnlyWhenAllPlatesPressed_AndStaysOpen()
        {
            var p1 = Plate(new Vector3(-2, 0, 0));
            var p2 = Plate(new Vector3(2, 0, 0));
            var gateGo = Box("Gate", new Vector3(0, 1.5f, 5), new Vector3(3, 3, 0.5f));
            gateGo.SetActive(false);
            gateGo.AddComponent<Rigidbody>();
            var gate = gateGo.AddComponent<GateController>();
            gate.Configure(new[] { p1, p2 }, new Vector3(0, -3.2f, 0), 1f);
            gate.AutoStep = false;
            gateGo.SetActive(true);
            int worldChanged = 0;
            services.WorldChanged += () => worldChanged++;

            var w1 = Weight(new Vector3(-2, 0.5f, 0), 10f);
            p1.Evaluate();
            p2.Evaluate();
            gate.Step(2f);
            Assert.That(gate.State, Is.EqualTo(GateController.GateState.Closed), "one plate is not enough");

            Weight(new Vector3(2, 0.5f, 0), 10f);
            p2.Evaluate();
            for (int i = 0; i < 20; i++) gate.Step(0.1f);
            Assert.That(gate.IsOpen, Is.True);
            Assert.That(gateGo.transform.position.y, Is.EqualTo(-1.7f).Within(1e-3f));
            Assert.That(worldChanged, Is.EqualTo(1), "a waiting hero is told to re-plan");

            Object.DestroyImmediate(w1.gameObject);
            p1.Evaluate();
            gate.Step(1f);
            Assert.That(gate.IsOpen, Is.True, "latched: never closes on the hero");
        }

        // ---------------------------------------------------------------- hazards

        private HeroHazard Piston(Vector3 position, out FreezeCapability freeze, out BoxCollider volume)
        {
            var go = Box("Piston", position, new Vector3(1, 3, 1));
            go.SetActive(false);
            go.layer = world;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<GameEntity>().Configure("PISTON", EntityCategory.SystemDevice, EntityCapabilities.Inspectable | EntityCapabilities.Freezable);
            freeze = go.AddComponent<FreezeCapability>();
            var zone = Track(new GameObject("Danger", typeof(BoxCollider)));
            zone.transform.position = new Vector3(0, 1.5f, 5);
            volume = zone.GetComponent<BoxCollider>();
            volume.size = new Vector3(4, 3, 2);
            var hazard = go.AddComponent<HeroHazard>();
            hazard.Configure(volume);
            go.SetActive(true);
            Physics.SyncTransforms();
            return hazard;
        }

        private HeroNavGraph Graph(out HeroNavNode[] nodes) =>
            HeroTestKit.Chain(T, out nodes, new Vector3(0, 0, 0), new Vector3(0, 0, 10));

        [Test]
        public void MovingHazard_BlocksRoutes_FrozenHazardDoesNot()
        {
            var hazard = Piston(new Vector3(8, 1.5f, 5), out var freeze, out _); // body far away; only the danger zone matters
            var graph = Graph(out _);
            Assert.That(hazard.IsSafe, Is.False);
            Assert.That(graph.EdgePassable(0, 1), Is.False);

            var definition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            definition.ApplyDefaults();
            services.Glitches.TryApply(definition.ToSpec(), definition.CreateEffect(services), new[] { hazard.GetComponent<GameEntity>().Id }, 0f);

            Assert.That(hazard.IsSafe, Is.True);
            Assert.That(graph.EdgePassable(0, 1), Is.True);
        }

        [Test]
        public void Unfreeze_IsVetoed_WhileSomeoneIsInTheMachinesPath()
        {
            var hazard = Piston(new Vector3(8, 1.5f, 5), out _, out _);
            var definition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            definition.ApplyDefaults();
            var op = services.Glitches.TryApply(definition.ToSpec(), definition.CreateEffect(services), new[] { hazard.GetComponent<GameEntity>().Id }, 0f).Operation;
            var occupant = Track(new GameObject("Hero"));
            occupant.layer = GameLayers.Index(GameLayers.Hero);
            occupant.transform.position = new Vector3(0, 1.05f, 5);
            occupant.AddComponent<CharacterController>();
            Physics.SyncTransforms();

            var blocked = services.Glitches.Revert(op.Id);

            Assert.That(blocked.Failure, Is.EqualTo(GlitchFailure.EndBlocked));
            StringAssert.Contains("IN ITS PATH", blocked.Describe());
        }

        [Test]
        public void Hero_WaitsForAMovingHazard_AndCrossesOnceFrozen()
        {
            var hazard = Piston(new Vector3(8, 1.5f, 5), out _, out _);
            var graph = Graph(out var nodes);
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, nodes[1]);
            HeroTestKit.Simulate(hero, 3f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
            Assert.That(hero.Feet.z, Is.LessThan(4f), "never steps into the machine's path");

            var definition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            definition.ApplyDefaults();
            services.Glitches.TryApply(definition.ToSpec(), definition.CreateEffect(services), new[] { hazard.GetComponent<GameEntity>().Id }, 0f);

            Assert.That(HeroTestKit.SimulateUntil(hero, HeroState.Arrived, 10f), Is.True, "state " + hero.State);
        }

        // ---------------------------------------------------------------- objectives

        [Test]
        public void Objectives_CompleteInOrder_ThenLevelEnds()
        {
            var graph = HeroTestKit.Chain(T, out var nodes, new Vector3(0, 0, 0), new Vector3(0, 0, 6), new Vector3(0, 0, 12));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, nodes[2]);
            HeroReachedZone ZoneAt(float z)
            {
                var go = Track(new GameObject("Zone", typeof(BoxCollider)));
                go.transform.position = new Vector3(0, 1.5f, z);
                go.GetComponent<BoxCollider>().size = new Vector3(4, 3, 2);
                var c = go.AddComponent<HeroReachedZone>();
                c.Configure(hero);
                return c;
            }
            var first = ZoneAt(6f);
            var second = ZoneAt(12f);
            var directorGo = Track(new GameObject("Objectives"));
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<ObjectiveDirector>();
            director.Configure(new[]
            {
                new ObjectiveDirector.Entry("a", "A", "", first),
                new ObjectiveDirector.Entry("b", "B", "", second),
            });
            directorGo.SetActive(true);
            bool ended = false;
            director.LevelCompleted += () => ended = true;

            for (int i = 0; i < 600 && !ended; i++)
            {
                hero.Tick(1f / 60f);
                Physics.SyncTransforms();
                director.Evaluate();
            }

            Assert.That(ended, Is.True);
            Assert.That(director.Tracker.CompletedCount, Is.EqualTo(2));
            Assert.That(services.State.Current, Is.EqualTo(GameState.Ending));
        }

        // ---------------------------------------------------------------- kill plane

        [Test]
        public void FallenOriginal_IsPutBack_FallenCopy_IsRemoved()
        {
            var original = Box("Crate", new Vector3(3, 0.5f, 0), Vector3.one);
            original.SetActive(false);
            original.AddComponent<Rigidbody>();
            original.AddComponent<GameEntity>().Configure("CRATE", EntityCategory.Prop, EntityCapabilities.Inspectable);
            original.SetActive(true);
            var copy = Box("Copy", new Vector3(6, 0.5f, 0), Vector3.one);
            copy.SetActive(false);
            copy.AddComponent<Rigidbody>();
            copy.AddComponent<GameEntity>().Configure("COPY", EntityCategory.Prop, EntityCapabilities.Inspectable);
            copy.AddComponent<DuplicateMarker>();
            copy.SetActive(true);
            var guard = Track(new GameObject("Guard")).AddComponent<WorldBoundsGuard>();
            guard.Check(); // records spawn poses

            original.transform.position = new Vector3(3, -50, 0);
            copy.transform.position = new Vector3(6, -50, 0);
            Assert.That(guard.Check(), Is.EqualTo(2));

            Assert.That(original.transform.position, Is.EqualTo(new Vector3(3, 0.5f, 0)));
            Assert.That(copy.activeSelf, Is.False);
        }
    }
}
