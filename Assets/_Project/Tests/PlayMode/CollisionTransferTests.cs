using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using BugReport.Hero;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// Collision Transfer against real physics. Needs the project layers
    /// (BUG REPORT > Setup > 1. Configure Project); tests are skipped otherwise.
    /// </summary>
    public class CollisionTransferTests : PlayModeTestBase
    {
        private const EntityCapabilities Transferable = EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable;

        private GameServices services;
        private CollisionTransferDefinition definition;
        private GlitchSpec spec;
        private CollisionTransferEffect effect;
        private int world;
        private int intangible;
        private int heroLayer;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            world = GameLayers.Index(GameLayers.World);
            intangible = GameLayers.Index(GameLayers.Intangible);
            heroLayer = GameLayers.Index(GameLayers.Hero);

            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            definition = Track(ScriptableObject.CreateInstance<CollisionTransferDefinition>());
            definition.ApplyDefaults();
            spec = definition.ToSpec();
            effect = new CollisionTransferEffect(services.Registry);
        }

        // ---------------------------------------------------------------- helpers

        private CollisionCapability Transferrable(string name, Vector3 position, Vector3 size, CollisionMode mode)
        {
            var go = Box(name, position, size);
            go.SetActive(false); // configure before Awake/OnEnable
            go.layer = mode == CollisionMode.Solid ? world : intangible;
            go.AddComponent<GameEntity>().Configure(name, EntityCategory.Structure, Transferable);
            var capability = go.AddComponent<CollisionCapability>();
            capability.Configure(go.GetComponents<Collider>(), mode, GameLayers.World);
            go.SetActive(true);
            Physics.SyncTransforms();
            return capability;
        }

        private static GameEntityId Id(Component c) => c.GetComponent<GameEntity>().Id;

        private GlitchResult Transfer(CollisionCapability source, CollisionCapability target, float now = 0f)
            => services.Glitches.TryApply(spec, effect, new[] { Id(source), Id(target) }, now);

        private PlaceholderHero Hero(Vector3 position, params Vector3[] route)
        {
            var go = Track(new GameObject("Hero"));
            go.layer = heroLayer;
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            var hero = go.AddComponent<PlaceholderHero>();
            hero.AutoTick = false;
            var points = new Transform[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                points[i] = Track(new GameObject("WP" + i)).transform;
                points[i].position = route[i];
            }
            hero.Configure(points, newStartDelay: 0f);
            Physics.SyncTransforms();
            return hero;
        }

        private static void Simulate(PlaceholderHero hero, float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++)
            {
                hero.Tick(1f / 60f);
                Physics.SyncTransforms();
            }
        }

        // ---------------------------------------------------------------- tests

        [Test]
        public void Apply_MakesSourceIntangibleAndTargetSolid()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);

            var result = Transfer(wall, pad);

            Assert.That(result.Succeeded, Is.True, result.Describe());
            Assert.That(wall.gameObject.layer, Is.EqualTo(intangible));
            Assert.That(pad.gameObject.layer, Is.EqualTo(world));
            Assert.That(wall.EffectiveMode, Is.EqualTo(CollisionMode.Intangible));
            Assert.That(pad.EffectiveMode, Is.EqualTo(CollisionMode.Solid));
        }

        [Test]
        public void Revert_RestoresBothExactly()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            Transfer(wall, pad);

            Assert.That(services.Glitches.RevertLatest().Succeeded, Is.True);

            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
            Assert.That(pad.gameObject.layer, Is.EqualTo(intangible));
            Assert.That(wall.Channel.IsModified || pad.Channel.IsModified, Is.False);
        }

        [Test]
        public void UnrelatedObjects_AreUnaffected()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            var bystander = Transferrable("Bystander", new Vector3(-8, 1, 0), Vector3.one, CollisionMode.Solid);
            var plain = Box("Plain", new Vector3(-8, 1, 5), Vector3.one);
            plain.layer = world;

            Transfer(wall, pad);

            Assert.That(bystander.gameObject.layer, Is.EqualTo(world));
            Assert.That(bystander.Channel.IsModified, Is.False);
            Assert.That(plain.layer, Is.EqualTo(world));
            Assert.That(Physics.GetIgnoreLayerCollision(world, heroLayer), Is.False, "no global physics side effects");
        }

        [Test]
        public void BlockedHero_WalksThroughAfterTransfer()
        {
            Ground().layer = world;
            var wall = Transferrable("Wall", new Vector3(0, 1.5f, 3), new Vector3(20, 3, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, -6), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            var hero = Hero(new Vector3(0, 1.05f, 0), new Vector3(0, 0, 8));
            Simulate(hero, 4f);
            Assume.That(hero.State, Is.EqualTo(HeroState.Blocked));

            Assert.That(Transfer(wall, pad).Succeeded, Is.True);
            Simulate(hero, 8f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Arrived));
        }

        [Test]
        public void SolidifiedTarget_BlocksTheHero()
        {
            Ground().layer = world;
            var source = Transferrable("Source", new Vector3(-8, 1, -6), Vector3.one, CollisionMode.Solid);
            var holo = Transferrable("Holo", new Vector3(0, 1.5f, 3), new Vector3(20, 3, 0.5f), CollisionMode.Intangible);
            var hero = Hero(new Vector3(0, 1.05f, 0), new Vector3(0, 0, 8));

            Assert.That(Transfer(source, holo).Succeeded, Is.True);
            Simulate(hero, 4f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
            Assert.That(hero.transform.position.z, Is.LessThan(3f));
        }

        [Test]
        public void DestroyingTarget_EndsOperation_AndRestoresSource()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            var op = Transfer(wall, pad).Operation;

            Object.DestroyImmediate(pad.gameObject);

            Assert.That(op.EndReason, Is.EqualTo(GlitchEndReason.TargetLost));
            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
            Assert.That(services.Glitches.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void OccupiedTarget_IsRejected()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var zone = Transferrable("Zone", new Vector3(8, 1, 0), new Vector3(3, 2, 3), CollisionMode.Intangible);
            Hero(new Vector3(8, 1.05f, 0));

            var result = Transfer(wall, zone);

            Assert.That(result.Failure, Is.EqualTo(GlitchFailure.EffectRejected));
            StringAssert.Contains("OCCUPIED", result.Describe());
            Assert.That(zone.gameObject.layer, Is.EqualTo(intangible));
        }

        [Test]
        public void Revert_WaitsWhileSomethingIsInsideTheSource()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 2f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            var op = Transfer(wall, pad).Operation;
            var hero = Hero(new Vector3(0, 1.05f, 3)); // standing inside the now-intangible wall

            var blocked = services.Glitches.Revert(op.Id);
            Assert.That(blocked.Failure, Is.EqualTo(GlitchFailure.EndBlocked));
            Assert.That(wall.gameObject.layer, Is.EqualTo(intangible));

            hero.GetComponent<CharacterController>().enabled = false;
            hero.transform.position = new Vector3(0, 1.05f, -5);
            hero.GetComponent<CharacterController>().enabled = true;
            Physics.SyncTransforms();

            Assert.That(services.Glitches.Revert(op.Id).Succeeded, Is.True);
            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
        }

        [Test]
        public void DynamicSource_IsRejected()
        {
            var crate = Transferrable("Crate", new Vector3(0, 0.5f, 3), Vector3.one, CollisionMode.Solid);
            crate.gameObject.AddComponent<Rigidbody>();
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);

            var result = Transfer(crate, pad);

            StringAssert.Contains("MOVING", result.Describe());
            Assert.That(crate.gameObject.layer, Is.EqualTo(world));
        }

        [Test]
        public void FlagWithoutComponent_IsRejectedWithReason()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var fake = Box("Fake", new Vector3(8, 1, 0), Vector3.one);
            fake.SetActive(false);
            fake.AddComponent<GameEntity>().Configure("FAKE", EntityCategory.Prop, Transferable);
            fake.SetActive(true);

            var result = services.Glitches.TryApply(spec, effect, new[] { Id(wall), fake.GetComponent<GameEntity>().Id }, 0f);

            StringAssert.Contains("NO COLLISION CAPABILITY", result.Describe());
            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
        }

        [Test]
        public void SecondTransferOntoSolidTarget_IsRejected()
        {
            var wallA = Transferrable("WallA", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var wallB = Transferrable("WallB", new Vector3(0, 1, -3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            Transfer(wallA, pad);

            var second = Transfer(wallB, pad, now: 5f);

            StringAssert.Contains("ALREADY SOLID", second.Describe());
            Assert.That(wallB.gameObject.layer, Is.EqualTo(world));
        }

        [Test]
        public void Expiry_RestoresAutomatically()
        {
            definition.Configure("timed", "TIMED", "", Color.blue, newDuration: 1f, newCooldown: 0f, newMaxActive: 0,
                new[]
                {
                    new GlitchDefinition.TargetSlot("SOURCE", Transferable),
                    new GlitchDefinition.TargetSlot("TARGET", Transferable),
                });
            spec = definition.ToSpec();
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            Transfer(wall, pad, now: 10f);

            services.Glitches.Tick(10.5f);
            Assert.That(wall.gameObject.layer, Is.EqualTo(intangible));
            services.Glitches.Tick(11f);
            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
        }

        [Test]
        public void LevelTeardown_RestoresSurvivingObjects()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            Transfer(wall, pad);

            services.Dispose();

            Assert.That(wall.gameObject.layer, Is.EqualTo(world));
            Assert.That(pad.gameObject.layer, Is.EqualTo(intangible));
        }

        [Test]
        public void IntangibleObject_IsStillTargetable()
        {
            var wall = Transferrable("Wall", new Vector3(0, 0, 5), new Vector3(4, 4, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            Transfer(wall, pad);

            var cam = Track(new GameObject("Cam", typeof(Camera)));
            var scanner = cam.AddComponent<Interaction.TargetScanner>();
            scanner.AutoScan = false;
            scanner.Configure(cam.GetComponent<Camera>(), null);

            Assert.That(scanner.Scan(), Is.SameAs(wall.GetComponent<GameEntity>()), "player must be able to aim at it to restore it");
        }

        [Test]
        public void ModeVisual_SwapsAndRestoresMaterials()
        {
            var wall = Transferrable("Wall", new Vector3(0, 1, 3), new Vector3(4, 2, 0.5f), CollisionMode.Solid);
            var pad = Transferrable("Pad", new Vector3(8, 0.05f, 0), new Vector3(2, 0.1f, 2), CollisionMode.Intangible);
            var renderer = wall.GetComponent<Renderer>();
            var original = renderer.sharedMaterial;
            var ghost = Track(new Material(Shader.Find("Sprites/Default")));
            var solid = Track(new Material(Shader.Find("Sprites/Default")));
            wall.gameObject.SetActive(false);
            wall.gameObject.AddComponent<Presentation.RendererOverrides>();
            wall.gameObject.AddComponent<CollisionModeVisual>().Configure(ghost, solid);
            wall.gameObject.SetActive(true);

            Transfer(wall, pad);
            Assert.That(renderer.sharedMaterial, Is.SameAs(ghost));

            services.Glitches.RevertLatest();
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
        }
    }
}
