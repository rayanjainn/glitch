using System.Collections.Generic;
using BugReport.Hero;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Builds heroes and nav graphs for PlayMode tests. Objects go through the caller's Track.</summary>
    internal static class HeroTestKit
    {
        public delegate T Tracker<T>(T obj) where T : Object;

        /// <summary>Creates nodes at the given floor positions, linked in a chain (0-1-2-…). Returns graph and nodes.</summary>
        public static HeroNavGraph Chain(System.Func<GameObject, GameObject> track, out HeroNavNode[] nodes, params Vector3[] positions)
        {
            var root = track(new GameObject("Graph"));
            nodes = new HeroNavNode[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var go = track(new GameObject("N" + i));
                go.transform.SetParent(root.transform, false);
                go.transform.position = positions[i];
                nodes[i] = go.AddComponent<HeroNavNode>();
            }
            for (int i = 0; i + 1 < nodes.Length; i++) nodes[i].Configure(nodes[i + 1]);
            var graph = root.AddComponent<HeroNavGraph>();
            graph.Configure(nodes);
            return graph;
        }

        public static HeroController Hero(System.Func<GameObject, GameObject> track, Vector3 position, HeroNavGraph graph, HeroNavNode goal)
        {
            var go = track(new GameObject("Hero"));
            go.layer = Mathf.Max(0, GameLayers.Index(GameLayers.Hero));
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            var hero = go.AddComponent<HeroController>();
            hero.AutoTick = false;
            hero.Configure(graph, goal, newStartDelay: 0f);
            Physics.SyncTransforms();
            return hero;
        }

        public static void Simulate(HeroController hero, float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++)
            {
                hero.Tick(1f / 60f);
                Physics.SyncTransforms();
            }
        }

        /// <summary>Runs until the hero reaches <paramref name="state"/> or the time runs out. Returns whether it did.</summary>
        public static bool SimulateUntil(HeroController hero, HeroState state, float maxSeconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(maxSeconds * 60f); i++)
            {
                hero.Tick(1f / 60f);
                Physics.SyncTransforms();
                if (hero.State == state) return true;
            }
            return hero.State == state;
        }
    }
}
