using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Core.Properties;
using BugReport.Glitches;
using BugReport.Guards;
using BugReport.Hero;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Property Swap, Desync, AI Override (guards) and security drones.</summary>
    public class NewGlitchTests : PlayModeTestBase
    {
        private GameServices services;
        private float clock;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
        }

        private GlitchResult Apply(GlitchDefinition definition, params GameEntity[] targets)
        {
            clock += 1f;
            var ids = new GameEntityId[targets.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = targets[i].Id;
            return services.Glitches.TryApply(definition.ToSpec(), definition.CreateEffect(services), ids, clock);
        }

        // ---------------------------------------------------------------- Property Swap

        private GameEntity Weighty(string name, float x, float mass)
        {
            var go = Box(name, new Vector3(x, 0.5f, 0), Vector3.one);
            go.SetActive(false);
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.isKinematic = true;
            var entity = go.AddComponent<GameEntity>();
            entity.Configure(name, EntityCategory.Prop, EntityCapabilities.Inspectable | EntityCapabilities.PropertyMutable);
            go.AddComponent<PropertyCapability>();
            go.SetActive(true);
            return entity;
        }

        private PropertySwapDefinition SwapDefinition(SwapProperty property)
        {
            var d = Track(ScriptableObject.CreateInstance<PropertySwapDefinition>());
            d.ApplyDefaults(property);
            Assert.That(d.ValidateDefinition(), Is.Null);
            return d;
        }

        [Test]
        public void MassSwap_TradesWeights_AndRevertRestoresThem()
        {
            var light = Weighty("LIGHT", 0, 1f);
            var heavy = Weighty("HEAVY", 3, 20f);
            var result = Apply(SwapDefinition(SwapProperty.Mass), heavy, light);
            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(light.GetComponent<Rigidbody>().mass, Is.EqualTo(20f));
            Assert.That(heavy.GetComponent<Rigidbody>().mass, Is.EqualTo(1f));
            Assert.That(light.GetComponent<PropertyCapability>().IsAltered(SwapProperty.Mass), Is.True);

            Assert.That(services.Glitches.Revert(result.Operation.Id).Succeeded, Is.True);
            Assert.That(light.GetComponent<Rigidbody>().mass, Is.EqualTo(1f));
            Assert.That(heavy.GetComponent<Rigidbody>().mass, Is.EqualTo(20f));
        }

        [Test]
        public void MassSwap_SameWeight_IsRejected_AndSwappedObjectsAreExclusive()
        {
            var a = Weighty("A", 0, 5f);
            var b = Weighty("B", 3, 5f);
            var c = Weighty("C", 6, 30f);
            var definition = SwapDefinition(SwapProperty.Mass);
            var same = Apply(definition, a, b);
            Assert.That(same.Succeeded, Is.False);
            StringAssert.Contains("SAME MASS", same.Describe());

            Assert.That(Apply(definition, a, c).Succeeded, Is.True);
            var again = Apply(definition, b, c);
            Assert.That(again.Succeeded, Is.False, "C is already part of a swap");
            StringAssert.Contains("ALREADY GLITCHED", again.Describe());
        }

        [Test]
        public void SpeedSwap_ExchangesMoverSpeeds()
        {
            GameEntity Machine(string name, float x)
            {
                var go = Box(name, new Vector3(x, 1, 0), Vector3.one);
                go.SetActive(false);
                go.AddComponent<Rigidbody>();
                var mover = go.AddComponent<PingPongMover>();
                var entity = go.AddComponent<GameEntity>();
                entity.Configure(name, EntityCategory.SystemDevice, EntityCapabilities.Inspectable | EntityCapabilities.PropertyMutable);
                go.AddComponent<PropertyCapability>();
                go.SetActive(true);
                return entity;
            }
            var fast = Machine("FAST", 0);
            var slow = Machine("SLOW", 4);
            fast.GetComponent<PingPongMover>().SpeedScale = 2f;
            slow.GetComponent<PropertyCapability>().Channel(SwapProperty.Speed).Base = 0.5f;
            fast.GetComponent<PropertyCapability>().Channel(SwapProperty.Speed).Base = 2f;

            var result = Apply(SwapDefinition(SwapProperty.Speed), fast, slow);
            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(fast.GetComponent<PingPongMover>().SpeedScale, Is.EqualTo(0.5f));
            Assert.That(slow.GetComponent<PingPongMover>().SpeedScale, Is.EqualTo(2f));
        }

        [Test]
        public void Swap_NeedsTheCapability_ForTheChosenProperty()
        {
            var weighty = Weighty("BOX", 0, 5f);
            var other = Weighty("OTHER", 3, 9f);
            var speed = Apply(SwapDefinition(SwapProperty.Speed), weighty, other);
            Assert.That(speed.Succeeded, Is.False, "neither object has a mover");
            StringAssert.Contains("SPEED", speed.Describe());
        }

        // ---------------------------------------------------------------- Desync

        private (GameEntity entity, DesyncCapability capability, GameObject collisionRoot) DesyncWall(Vector3 offset)
        {
            var root = Track(new GameObject("Wall"));
            root.SetActive(false);
            root.transform.position = new Vector3(0, 1, 0);
            var collision = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Track(collision);
            collision.name = "Collision";
            collision.transform.SetParent(root.transform, false);
            collision.transform.localScale = new Vector3(2, 2, 0.5f);
            var entity = root.AddComponent<GameEntity>();
            entity.Configure("WALL", EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.Desyncable);
            var capability = root.AddComponent<DesyncCapability>();
            capability.Configure(collision.transform, offset, null);
            root.SetActive(true);
            Physics.SyncTransforms();
            return (entity, capability, collision);
        }

        private DesyncDefinition DesyncDef()
        {
            var d = Track(ScriptableObject.CreateInstance<DesyncDefinition>());
            d.ApplyDefaults();
            return d;
        }

        [Test]
        public void Desync_MovesTheCollision_NotTheLooks_AndRevertSnapsBack()
        {
            var (entity, capability, collision) = DesyncWall(new Vector3(0, 0, -4));
            var original = collision.transform.position;
            Assert.That(Physics.CheckBox(original, Vector3.one * 0.2f), Is.True);

            var result = Apply(DesyncDef(), entity);
            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(capability.IsDesynced, Is.True);
            Assert.That(collision.transform.position.z, Is.EqualTo(original.z - 4f).Within(1e-3f));
            Assert.That(Physics.CheckBox(original, Vector3.one * 0.2f), Is.False, "nothing solid where the wall looks to be");
            Assert.That(Physics.CheckBox(original + new Vector3(0, 0, -4), Vector3.one * 0.2f), Is.True, "solid where the collision really is");

            Assert.That(services.Glitches.Revert(result.Operation.Id).Succeeded, Is.True);
            Assert.That(collision.transform.position, Is.EqualTo(original));
        }

        [Test]
        public void Desync_IsRefused_WhenSomethingWouldBeInsideTheNewCollision()
        {
            var (entity, _, _) = DesyncWall(new Vector3(0, 0, -4));
            var blocker = Box("Crate", new Vector3(0, 1, -4), Vector3.one);
            blocker.layer = GameLayers.Index(GameLayers.Props);
            Physics.SyncTransforms();
            var result = Apply(DesyncDef(), entity);
            Assert.That(result.Succeeded, Is.False);
            StringAssert.Contains("DESTINATION OCCUPIED", result.Describe());
        }

        [Test]
        public void Desync_CannotBeStacked()
        {
            var (entity, _, _) = DesyncWall(new Vector3(0, 0, -4));
            Assert.That(Apply(DesyncDef(), entity).Succeeded, Is.True);
            var again = Apply(DesyncDef(), entity);
            Assert.That(again.Succeeded, Is.False);
            StringAssert.Contains("ALREADY DESYNCED", again.Describe());
        }

        // ---------------------------------------------------------------- Guards and AI Override

        private Transform Point(string name, Vector3 position)
        {
            var go = Track(new GameObject(name));
            go.transform.position = position;
            return go.transform;
        }

        private GuardController Guard(Vector3 position, Transform[] route, HeroController hero = null, Transform anomaly = null, ContainmentDirector containment = null)
        {
            var go = Box("GUARD", position, new Vector3(0.8f, 1.8f, 0.8f));
            go.SetActive(false);
            var entity = go.AddComponent<GameEntity>();
            entity.Configure("GUARD", EntityCategory.Enemy, EntityCapabilities.Inspectable | EntityCapabilities.AIControllable | EntityCapabilities.Freezable);
            go.AddComponent<FreezeCapability>();
            var guard = go.AddComponent<GuardController>();
            guard.Configure(route, hero, anomaly, containment, null);
            go.SetActive(true);
            go.transform.rotation = Quaternion.LookRotation(Vector3.right);
            return guard;
        }

        private static void Run(GuardController guard, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.05f) guard.Step(0.05f);
        }

        [Test]
        public void Guard_PatrolsBetweenItsPoints()
        {
            var a = Point("A", new Vector3(0, 0.9f, 0));
            var b = Point("B", new Vector3(6, 0.9f, 0));
            var guard = Guard(a.position, new[] { b, a });
            Run(guard, 12f);
            Assert.That(guard.Brain.PatrolIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(guard.transform.position.x, Is.GreaterThan(0.5f), "it has walked");
        }

        [Test]
        public void AiOverride_LuresTheGuardToTheDecoy_ThenItReturnsOnRevert()
        {
            var a = Point("A", new Vector3(0, 0.9f, 0));
            var b = Point("B", new Vector3(-6, 0.9f, 0));
            var guard = Guard(a.position, new[] { a, b });
            var decoy = Box("DECOY", new Vector3(0, 0.5f, 8), Vector3.one);
            decoy.SetActive(false);
            var decoyEntity = decoy.AddComponent<GameEntity>();
            decoyEntity.Configure("DECOY", EntityCategory.Prop, EntityCapabilities.Inspectable);
            decoy.SetActive(true);

            var definition = Track(ScriptableObject.CreateInstance<AiOverrideDefinition>());
            definition.ApplyDefaults();
            Assert.That(definition.ValidateDefinition(), Is.Null);
            var result = Apply(definition, guard.GetComponent<GameEntity>(), decoyEntity);
            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(guard.Brain.State, Is.EqualTo(Core.AI.GuardState.Investigate));
            Assert.That(guard.IsNeutralized, Is.True, "a lured guard does not threaten the hero");

            Run(guard, 8f);
            Assert.That(guard.transform.position.z, Is.GreaterThan(5f), "it walked to the decoy");

            Assert.That(services.Glitches.Revert(result.Operation.Id).Succeeded, Is.True);
            Assert.That(guard.Brain.State, Is.EqualTo(Core.AI.GuardState.Return));
            Assert.That(guard.IsNeutralized, Is.False);
        }

        [Test]
        public void AiOverride_RejectsAGuardAsDecoy_AndFrozenGuards_AndNonGuards()
        {
            var a = Point("A", new Vector3(0, 0.9f, 0));
            var guard = Guard(a.position, new[] { a });
            var other = Guard(new Vector3(5, 0.9f, 0), new[] { Point("B", new Vector3(5, 0.9f, 0)) });
            var crate = Weighty("CRATE", 10, 5f);
            var definition = Track(ScriptableObject.CreateInstance<AiOverrideDefinition>());
            definition.ApplyDefaults();

            var guardDecoy = Apply(definition, guard.GetComponent<GameEntity>(), other.GetComponent<GameEntity>());
            Assert.That(guardDecoy.Succeeded, Is.False);
            var notAGuard = Apply(definition, crate, guard.GetComponent<GameEntity>());
            Assert.That(notAGuard.Succeeded, Is.False);

            var freezeDefinition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDefinition.ApplyDefaults();
            Assert.That(Apply(freezeDefinition, guard.GetComponent<GameEntity>()).Succeeded, Is.True);
            var frozen = Apply(definition, guard.GetComponent<GameEntity>(), crate);
            Assert.That(frozen.Succeeded, Is.False);
            StringAssert.Contains("FROZEN", frozen.Describe());
        }

        [Test]
        public void FrozenGuard_IsHarmless_AndDoesNotMove()
        {
            var a = Point("A", new Vector3(0, 0.9f, 0));
            var b = Point("B", new Vector3(8, 0.9f, 0));
            var guard = Guard(a.position, new[] { b, a });
            var freezeDefinition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDefinition.ApplyDefaults();
            Assert.That(Apply(freezeDefinition, guard.GetComponent<GameEntity>()).Succeeded, Is.True);
            Assert.That(guard.IsNeutralized, Is.True);
            var before = guard.transform.position;
            Run(guard, 5f);
            Assert.That(guard.transform.position, Is.EqualTo(before));
        }

        [Test]
        public void Guard_AddsContainmentPressure_WhileItSeesTheAnomaly()
        {
            var settings = Track(ScriptableObject.CreateInstance<ContainmentSettingsAsset>());
            var directorGo = Track(new GameObject("Containment"));
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<ContainmentDirector>();
            director.Configure(settings);
            directorGo.SetActive(true);

            var a = Point("A", new Vector3(0, 0.9f, 0));
            var anomaly = Point("Anomaly", new Vector3(4, 1.2f, 0));
            var guard = Guard(a.position, new[] { a }, anomaly: anomaly, containment: director);
            guard.Step(0.1f);
            Assert.That(guard.SeesAnomaly, Is.True);
            director.Step(1f);
            Assert.That(services.Containment.Pressure, Is.GreaterThan(0f));

            anomaly.position = new Vector3(-4, 1.2f, 0); // behind the guard
            guard.Step(0.1f);
            Assert.That(guard.SeesAnomaly, Is.False);
        }

        [Test]
        public void Guard_CatchesTheHeroWhoWalksIntoIt_AndTheHeroRestartsTheRoute()
        {
            var graph = HeroTestKit.Chain(go => Track(go), out var nodes, new Vector3(0, 0, 0), new Vector3(8, 0, 0));
            var hero = HeroTestKit.Hero(go => Track(go), new Vector3(0.5f, 1.05f, 0), graph, nodes[1]);
            var a = Point("A", new Vector3(0, 0.9f, 0));
            var guard = Guard(new Vector3(0.7f, 0.9f, 0), new[] { a }, hero: hero);
            int caught = 0;
            guard.HeroCaught += () => caught++;
            guard.Step(0.05f);
            Assert.That(caught, Is.EqualTo(1));
            Assert.That(hero.RespawnCount, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- drones

        [Test]
        public void Drone_SightsTheAnomalyInItsLight_ButNotWhenFrozen()
        {
            var settings = Track(ScriptableObject.CreateInstance<ContainmentSettingsAsset>());
            var directorGo = Track(new GameObject("Containment"));
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<ContainmentDirector>();
            director.Configure(settings);
            directorGo.SetActive(true);

            var go = Box("DRONE", new Vector3(0, 3, 0), Vector3.one * 0.6f);
            go.SetActive(false);
            go.AddComponent<GameEntity>().Configure("DRONE", EntityCategory.Enemy, EntityCapabilities.Inspectable | EntityCapabilities.Freezable);
            go.AddComponent<FreezeCapability>();
            var drone = go.AddComponent<SecurityDrone>();
            var anomaly = Point("Anomaly", new Vector3(1, 1f, 0));
            drone.Configure(new Transform[0], anomaly, director, null, null);
            go.SetActive(true);

            drone.Step(0.1f);
            Assert.That(drone.SeesAnomaly, Is.True);
            director.Step(1f);
            Assert.That(services.Containment.Pressure, Is.GreaterThan(0f));

            anomaly.position = new Vector3(9, 1f, 0);
            drone.Step(0.1f);
            Assert.That(drone.SeesAnomaly, Is.False, "outside the searchlight");

            anomaly.position = new Vector3(1, 1f, 0);
            var freezeDefinition = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freezeDefinition.ApplyDefaults();
            Assert.That(Apply(freezeDefinition, go.GetComponent<GameEntity>()).Succeeded, Is.True);
            drone.Step(0.1f);
            Assert.That(drone.SeesAnomaly, Is.False, "a frozen drone is blind");
        }
    }
}
