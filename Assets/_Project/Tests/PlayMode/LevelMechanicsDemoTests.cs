using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Mechanics added for the demo levels: floor checks, respawn after a fall, piston phase.</summary>
    public class LevelMechanicsDemoTests : PlayModeTestBase
    {
        private GameObject T(GameObject go) => Track(go);

        /// <summary>Floor on both sides of a 4 m pit, on the World layer (what the hero's checks see).</summary>
        private GameObject[] PitFloors()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            var west = Box("West", new Vector3(-6f, -0.1f, 0f), new Vector3(8f, 0.2f, 6f));
            var east = Box("East", new Vector3(6f, -0.1f, 0f), new Vector3(8f, 0.2f, 6f));
            west.layer = east.layer = GameLayers.Index(GameLayers.World);
            return new[] { west, east };
        }

        private HeroNavGraph GroundGraph(out HeroNavNode[] nodes)
        {
            var graph = HeroTestKit.Chain(T, out nodes, new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f));
            graph.Configure(nodes, 0.4f, 2f, groundRequired: true);
            return graph;
        }

        [Test]
        public void RequireGround_EdgeOverAPitIsNotWalkable()
        {
            PitFloors();
            Physics.SyncTransforms();
            var graph = GroundGraph(out _);
            Assert.That(graph.IsPassable(new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f)), Is.False, "4 m pit in the middle");
            Assert.That(graph.IsPassable(new Vector3(-4f, 0f, 0f), new Vector3(-2.5f, 0f, 0f)), Is.True, "solid all the way");
        }

        [Test]
        public void RequireGround_ASolidBridgeMakesItWalkable_AnIntangibleOneDoesNot()
        {
            PitFloors();
            var bridge = Box("Bridge", new Vector3(0f, -0.12f, 0f), new Vector3(4.4f, 0.2f, 2.4f));
            bridge.layer = GameLayers.Index(GameLayers.Intangible);
            Physics.SyncTransforms();
            var graph = GroundGraph(out _);
            Assert.That(graph.IsPassable(new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f)), Is.False, "a hologram is no floor");

            bridge.layer = GameLayers.Index(GameLayers.World);
            Physics.SyncTransforms();
            Assert.That(graph.IsPassable(new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f)), Is.True);
        }

        [Test]
        public void WithoutRequireGround_PitsAreIgnored_AsBefore()
        {
            var graph = HeroTestKit.Chain(T, out _, new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f));
            Assert.That(graph.RequireGround, Is.False);
            Assert.That(graph.IsPassable(new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f)), Is.True);
        }

        [Test]
        public void HeroFallingOutOfTheWorld_IsPutBackAtItsStart()
        {
            var services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
            Ground();
            var graph = HeroTestKit.Chain(T, out var nodes, new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f));
            var hero = HeroTestKit.Hero(T, new Vector3(0f, 1.05f, 0f), graph, nodes[1]);
            hero.gameObject.SetActive(false);
            hero.gameObject.AddComponent<GameEntity>().Configure("HERO", EntityCategory.Hero, EntityCapabilities.Inspectable);
            hero.gameObject.SetActive(true);
            var guard = Track(new GameObject("Guard")).AddComponent<WorldBoundsGuard>();

            var cc = hero.GetComponent<CharacterController>();
            cc.enabled = false;
            hero.transform.position = new Vector3(3f, -30f, 0f);
            cc.enabled = true;

            Assert.That(guard.Check(), Is.EqualTo(1));
            Assert.That(hero.transform.position.y, Is.EqualTo(1.05f).Within(0.01f));
            Assert.That(hero.RespawnCount, Is.EqualTo(1));
            Assert.That(hero.State, Is.EqualTo(HeroState.Confused));
        }

        [Test]
        public void PingPongMover_CanStartPartWayThroughItsCycle()
        {
            var go = Box("Piston", new Vector3(0f, 1f, 0f), Vector3.one);
            go.SetActive(false);
            go.AddComponent<Rigidbody>();
            var mover = go.AddComponent<PingPongMover>();
            mover.Configure(new Vector3(0f, 0f, 3f), 2f, 0.5f);
            mover.AutoStep = false;
            go.SetActive(true);
            Assert.That(mover.Phase, Is.EqualTo(0.5f));
            Assert.That(go.transform.position.z, Is.EqualTo(3f).Within(1e-3f), "half a cycle = fully extended");
            Assert.That(mover.PositionAt(1f).z, Is.EqualTo(0f).Within(1e-3f), "returns to where it was placed");
        }
    }
}
