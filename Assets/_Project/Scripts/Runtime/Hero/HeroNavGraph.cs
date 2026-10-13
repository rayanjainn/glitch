using System.Collections.Generic;
using BugReport.Core.Navigation;
using UnityEngine;

namespace BugReport.Hero
{
    /// <summary>
    /// Builds a <see cref="NavGraph"/> from authored <see cref="HeroNavNode"/>s and answers
    /// "is this edge walkable right now?" with a capsule sweep against what is currently solid.
    /// Because passability is a live physics query, glitches that change collision (Collision
    /// Transfer, duplicated crates) change the hero's routes immediately, with no nav rebuild.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroNavGraph : MonoBehaviour
    {
        [SerializeField] private HeroNavNode[] nodes = new HeroNavNode[0];
        [SerializeField, Min(0.05f)] private float agentRadius = 0.4f;
        [SerializeField, Min(0.5f)] private float agentHeight = 2f;
        [Tooltip("Steps lower than this (pads, decals) are ignored by the sweep.")]
        [SerializeField, Min(0f)] private float stepClearance = 0.3f;
        [Tooltip("Leave empty to use GameLayers.HeroBlockingMask.")]
        [SerializeField] private LayerMask blockingMask;

        private readonly Dictionary<HeroNavNode, int> ids = new Dictionary<HeroNavNode, int>();
        private readonly List<int> byDistance = new List<int>();
        private NavGraph graph;

        public NavGraph Graph
        {
            get
            {
                if (graph == null) Build();
                return graph;
            }
        }

        public int Mask => blockingMask.value != 0 ? blockingMask.value : GameLayers.HeroBlockingMask;

        public void Configure(HeroNavNode[] newNodes, float radius = 0.4f, float height = 2f)
        {
            nodes = newNodes ?? new HeroNavNode[0];
            agentRadius = radius;
            agentHeight = height;
            graph = null;
        }

        /// <summary>(Re)builds the graph from the node list. Nodes are static; call again only if they move.</summary>
        public void Build()
        {
            graph = new NavGraph();
            ids.Clear();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null || ids.ContainsKey(nodes[i])) continue;
                ids.Add(nodes[i], i);
                var p = nodes[i].transform.position;
                graph.AddNode(i, p.x, p.y, p.z);
            }
            foreach (var pair in ids)
            {
                foreach (var other in pair.Key.Links)
                {
                    if (other != null && ids.TryGetValue(other, out var otherId)) graph.Connect(pair.Value, otherId);
                    else if (other != null) Debug.LogWarning("[BUG REPORT] Nav node " + pair.Key.name + " links to " + other.name + ", which is not in the graph.", this);
                }
            }
        }

        public bool TryGetId(HeroNavNode node, out int id)
        {
            if (graph == null) Build();
            return ids.TryGetValue(node, out id);
        }

        public Vector3 Position(int id)
        {
            Graph.GetPosition(id, out var x, out var y, out var z);
            return new Vector3(x, y, z);
        }

        public bool EdgePassable(int a, int b) => IsPassable(Position(a), Position(b));

        /// <summary>True if a hero-sized capsule standing at <paramref name="fromFeet"/> can walk straight to <paramref name="toFeet"/>.</summary>
        public bool IsPassable(Vector3 fromFeet, Vector3 toFeet)
        {
            var delta = toFeet - fromFeet;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 1e-3f) return true;
            float r = agentRadius * 0.95f;
            var bottom = fromFeet + Vector3.up * (stepClearance + r);
            var top = fromFeet + Vector3.up * Mathf.Max(stepClearance + r, agentHeight - r);
            return !Physics.CapsuleCast(bottom, top, r, delta / distance, distance, Mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Nearest node the hero can walk straight to from <paramref name="feet"/>; -1 if none.</summary>
        public int NearestReachable(Vector3 feet)
        {
            if (graph == null) Build();
            byDistance.Clear();
            foreach (var id in ids.Values) byDistance.Add(id);
            byDistance.Sort((a, b) => (Position(a) - feet).sqrMagnitude.CompareTo((Position(b) - feet).sqrMagnitude));
            foreach (var id in byDistance)
            {
                if (IsPassable(feet, Position(id))) return id;
            }
            return -1;
        }
    }
}
