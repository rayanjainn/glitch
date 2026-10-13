using System.Collections.Generic;
using BugReport.Hero;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class HeroControllerTests : PlayModeTestBase
    {
        private int world;

        [SetUp]
        public void SetUp()
        {
            Assume.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
            world = GameLayers.Index(GameLayers.World);
            Ground().layer = world;
        }

        private GameObject T(GameObject go) => Track(go);

        private GameObject Wall(Vector3 position, Vector3 size)
        {
            var w = Box("Wall", position, size);
            w.layer = world;
            Physics.SyncTransforms();
            return w;
        }

        [Test]
        public void WalksTheGraphToTheGoal()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, n[2]);

            Assert.That(HeroTestKit.SimulateUntil(hero, HeroState.Arrived, 10f), Is.True, "state " + hero.State);
            Assert.That(Vector3.Distance(new Vector3(hero.Feet.x, 0, hero.Feet.z), n[2].transform.position), Is.LessThan(0.5f));
            Assert.That(hero.PathComplete, Is.True);
        }

        [Test]
        public void Unreachable_WalksAsFarAsPossible_ThenWaitsBlocked()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 4), new Vector3(0, 0, 10));
            Wall(new Vector3(0, 1.5f, 7), new Vector3(20, 3, 0.5f));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, n[2]);

            HeroTestKit.Simulate(hero, 5f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
            Assert.That(hero.PathComplete, Is.False);
            Assert.That(hero.Feet.z, Is.EqualTo(4f).Within(0.5f), "waits at the closest reachable node, at the obstacle");
        }

        [Test]
        public void WallRemoved_BlockedHeroContinues()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 4), new Vector3(0, 0, 10));
            var wall = Wall(new Vector3(0, 1.5f, 7), new Vector3(20, 3, 0.5f));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, n[2]);
            HeroTestKit.Simulate(hero, 5f);
            Assume.That(hero.State, Is.EqualTo(HeroState.Blocked));

            wall.GetComponent<Collider>().enabled = false;
            hero.NotifyWorldChanged();

            Assert.That(HeroTestKit.SimulateUntil(hero, HeroState.Arrived, 10f), Is.True, "state " + hero.State);
        }

        [Test]
        public void BlockedRoute_IsAvoidedByTakingTheOtherBranch()
        {
            // 0 → 1 → (2 direct | 3 detour) → 4
            var root = T(new GameObject("Graph"));
            HeroNavNode Node(string name, Vector3 p)
            {
                var go = T(new GameObject(name));
                go.transform.SetParent(root.transform, false);
                go.transform.position = p;
                return go.AddComponent<HeroNavNode>();
            }
            var a = Node("A", new Vector3(0, 0, 0));
            var b = Node("B", new Vector3(0, 0, 4));
            var direct = Node("Direct", new Vector3(0, 0, 8));
            var detour = Node("Detour", new Vector3(5, 0, 8));
            var goal = Node("Goal", new Vector3(0, 0, 12));
            a.Configure(b);
            b.Configure(direct, detour);
            direct.Configure(goal);
            detour.Configure(goal);
            var graph = root.AddComponent<HeroNavGraph>();
            graph.Configure(new[] { a, b, direct, detour, goal });
            Wall(new Vector3(0, 1.5f, 6), new Vector3(3, 3, 0.5f)); // blocks b→direct only

            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, goal);

            Assert.That(HeroTestKit.SimulateUntil(hero, HeroState.Arrived, 20f), Is.True, "state " + hero.State);
        }

        [Test]
        public void ObstacleAppearingMidRoute_ConfusesThenReplans()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 10));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, n[1]);
            var seen = new List<HeroState>();
            hero.StateChanged += (_, s) => seen.Add(s);
            HeroTestKit.Simulate(hero, 1f);
            Assume.That(hero.State, Is.EqualTo(HeroState.Walking));

            Wall(new Vector3(0, 1.5f, 6), new Vector3(20, 3, 0.5f));
            HeroTestKit.Simulate(hero, 4f);

            Assert.That(seen, Does.Contain(HeroState.Confused));
            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked).Or.EqualTo(HeroState.Confused));
            Assert.That(hero.Feet.z, Is.LessThan(6f));
        }

        [Test]
        public void IntangibleObjects_DoNotBlockPlanning()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 10));
            var ghost = Wall(new Vector3(0, 1.5f, 5), new Vector3(20, 3, 0.5f));
            ghost.layer = GameLayers.Index(GameLayers.Intangible);
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, n[1]);

            Assert.That(HeroTestKit.SimulateUntil(hero, HeroState.Arrived, 10f), Is.True, "state " + hero.State);
        }

        [Test]
        public void MissingGoal_StaysBlockedWithoutErrors()
        {
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 4));
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, null);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no graph or goal"));

            hero.Tick(0.1f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
        }
    }
}
