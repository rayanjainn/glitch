using System;
using System.Collections.Generic;
using BugReport.Core.Navigation;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class NavGraphTests
    {
        //  0 ---- 1 ---- 2 ---- 5(goal)
        //          \          /
        //           3 ------ 4
        private NavGraph graph;
        private readonly List<int> path = new List<int>();

        [SetUp]
        public void SetUp()
        {
            graph = new NavGraph();
            graph.AddNode(0, 0, 0, 0);
            graph.AddNode(1, 4, 0, 0);
            graph.AddNode(2, 8, 0, 0);
            graph.AddNode(3, 6, 0, -3);
            graph.AddNode(4, 10, 0, -3);
            graph.AddNode(5, 12, 0, 0);
            graph.Connect(0, 1);
            graph.Connect(1, 2);
            graph.Connect(2, 5);
            graph.Connect(1, 3);
            graph.Connect(3, 4);
            graph.Connect(4, 5);
        }

        private static Func<int, int, bool> Blocking(params (int, int)[] blocked)
        {
            var set = new HashSet<(int, int)>();
            foreach (var (a, b) in blocked)
            {
                set.Add((a, b));
                set.Add((b, a));
            }
            return (a, b) => !set.Contains((a, b));
        }

        [Test]
        public void ShortestRoute_WhenEverythingIsOpen()
        {
            Assert.That(graph.TryFindPath(0, 5, Blocking(), path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { 0, 1, 2, 5 }));
        }

        [Test]
        public void Reroutes_AroundABlockedEdge()
        {
            Assert.That(graph.TryFindPath(0, 5, Blocking((2, 5)), path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { 0, 1, 3, 4, 5 }));
        }

        [Test]
        public void Unreachable_ReturnsPartialPathToClosestReachableNode()
        {
            // The "firewall": cut the graph between 0-1 side and the rest beyond 1.
            Assert.That(graph.TryFindPath(0, 5, Blocking((1, 2), (1, 3)), path), Is.False);
            Assert.That(path, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void FullyBlockedStart_ReturnsJustTheStart()
        {
            Assert.That(graph.TryFindPath(0, 5, Blocking((0, 1)), path), Is.False);
            Assert.That(path, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void StartEqualsGoal_IsTrivialPath()
        {
            Assert.That(graph.TryFindPath(5, 5, Blocking(), path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void Nearest_FindsClosestNode()
        {
            Assert.That(graph.Nearest(7.6f, 0, 0.2f), Is.EqualTo(2));
            Assert.That(new NavGraph().Nearest(0, 0, 0), Is.EqualTo(-1));
        }

        [Test]
        public void InvalidUse_Throws()
        {
            Assert.Throws<ArgumentException>(() => graph.AddNode(0, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => graph.Connect(1, 1));
            Assert.Throws<KeyNotFoundException>(() => graph.Connect(1, 99));
            Assert.Throws<KeyNotFoundException>(() => graph.TryFindPath(0, 99, Blocking(), path));
        }

        [Test]
        public void ConnectTwice_DoesNotDuplicateLinks()
        {
            graph.Connect(0, 1);
            Assert.That(graph.Neighbours(0).Count, Is.EqualTo(1));
        }
    }
}
