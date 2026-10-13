using BugReport.Hero;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class PlaceholderHeroTests : PlayModeTestBase
    {
        private const float Dt = 1f / 60f;

        private PlaceholderHero CreateHero(Vector3 start, params Vector3[] waypoints)
        {
            var go = Track(new GameObject("Hero"));
            go.transform.position = start;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            var hero = go.AddComponent<PlaceholderHero>();
            hero.AutoTick = false;

            var route = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                var wp = Track(new GameObject("WP_" + i)).transform;
                wp.position = waypoints[i];
                route[i] = wp;
            }
            hero.Configure(route, newStartDelay: 0f);
            Physics.SyncTransforms();
            return hero;
        }

        private static void Simulate(PlaceholderHero hero, float seconds)
        {
            int frames = Mathf.CeilToInt(seconds / Dt);
            for (int i = 0; i < frames; i++)
            {
                hero.Tick(Dt);
                Physics.SyncTransforms();
            }
        }

        [Test]
        public void WalksRouteAndArrives()
        {
            Ground();
            var hero = CreateHero(new Vector3(0f, 1.05f, 0f), new Vector3(0f, 0f, 3f), new Vector3(3f, 0f, 3f));

            Simulate(hero, 8f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Arrived));
            var p = hero.transform.position;
            Assert.That(new Vector2(p.x, p.z), Is.EqualTo(new Vector2(3f, 3f)).Using(new Vector2Tolerance(0.5f)));
        }

        [Test]
        public void WallInRoute_BecomesBlocked()
        {
            Ground();
            Box("Wall", new Vector3(0f, 1.5f, 3f), new Vector3(20f, 3f, 0.5f));
            var hero = CreateHero(new Vector3(0f, 1.05f, 0f), new Vector3(0f, 0f, 8f));

            Simulate(hero, 4f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
            Assert.That(hero.transform.position.z, Is.LessThan(3f));
        }

        [Test]
        public void RemovingTheWall_LetsHeroRetryAndArrive()
        {
            // The behaviour Collision Transfer depends on: once the obstacle stops colliding, the hero proceeds.
            Ground();
            var wall = Box("Wall", new Vector3(0f, 1.5f, 3f), new Vector3(20f, 3f, 0.5f));
            var hero = CreateHero(new Vector3(0f, 1.05f, 0f), new Vector3(0f, 0f, 8f));
            Simulate(hero, 4f);
            Assume.That(hero.State, Is.EqualTo(HeroState.Blocked));

            wall.GetComponent<Collider>().enabled = false;
            Simulate(hero, 8f);

            Assert.That(hero.State, Is.EqualTo(HeroState.Arrived));
        }

        [Test]
        public void EmptyRoute_ArrivesImmediately()
        {
            Ground();
            var hero = CreateHero(new Vector3(0f, 1.05f, 0f));
            Simulate(hero, 0.1f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Arrived));
        }

        [Test]
        public void StateChanged_ReportsTransitions()
        {
            Ground();
            var hero = CreateHero(new Vector3(0f, 1.05f, 0f), new Vector3(0f, 0f, 1f));
            var seen = new System.Collections.Generic.List<HeroState>();
            hero.StateChanged += (_, s) => seen.Add(s);

            Simulate(hero, 4f);

            Assert.That(seen, Is.EqualTo(new[] { HeroState.Walking, HeroState.Waiting, HeroState.Arrived }));
        }

        /// <summary>Component-wise tolerance comparer (avoids depending on UnityEngine.TestTools.Utils).</summary>
        private sealed class Vector2Tolerance : System.Collections.Generic.IEqualityComparer<Vector2>
        {
            private readonly float tolerance;
            public Vector2Tolerance(float tolerance) => this.tolerance = tolerance;
            public bool Equals(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) <= tolerance && Mathf.Abs(a.y - b.y) <= tolerance;
            public int GetHashCode(Vector2 v) => 0;
        }
    }
}
