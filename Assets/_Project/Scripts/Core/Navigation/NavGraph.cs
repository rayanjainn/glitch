using System;
using System.Collections.Generic;

namespace BugReport.Core.Navigation
{
    /// <summary>
    /// Small authored waypoint graph with A* search. Passability is supplied per query
    /// (physics checks in the game, a set of blocked edges in tests), so the same graph
    /// reacts to glitches that change what is solid without any rebuild.
    ///
    /// When the goal is unreachable, the search returns a partial path to the reachable
    /// node closest to the goal, so the hero walks as far as it can and then waits there
    /// (readable: it visibly stands at the obstacle instead of idling at its spawn).
    /// </summary>
    public sealed class NavGraph
    {
        private readonly Dictionary<int, Node> nodes = new Dictionary<int, Node>();

        public int NodeCount => nodes.Count;

        public void AddNode(int id, float x, float y, float z)
        {
            if (nodes.ContainsKey(id)) throw new ArgumentException("Duplicate node id " + id, nameof(id));
            nodes.Add(id, new Node(id, x, y, z));
        }

        /// <summary>Adds an undirected edge. Duplicate edges are ignored.</summary>
        public void Connect(int a, int b)
        {
            if (a == b) throw new ArgumentException("Self-loops are not allowed.");
            var na = Get(a);
            var nb = Get(b);
            if (!na.Links.Contains(b)) na.Links.Add(b);
            if (!nb.Links.Contains(a)) nb.Links.Add(a);
        }

        public bool Contains(int id) => nodes.ContainsKey(id);

        public void GetPosition(int id, out float x, out float y, out float z)
        {
            var n = Get(id);
            x = n.X;
            y = n.Y;
            z = n.Z;
        }

        public IReadOnlyList<int> Neighbours(int id) => Get(id).Links;

        public float Distance(int a, int b) => Dist(Get(a), Get(b));

        /// <summary>
        /// Finds a path from <paramref name="start"/> to <paramref name="goal"/> using only edges for
        /// which <paramref name="passable"/>(from, to) is true. Fills <paramref name="path"/> (start
        /// first). Returns true for a complete path; false with a partial path to the reachable node
        /// closest to the goal (possibly just <paramref name="start"/>).
        /// </summary>
        public bool TryFindPath(int start, int goal, Func<int, int, bool> passable, List<int> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (passable == null) throw new ArgumentNullException(nameof(passable));
            path.Clear();
            var s = Get(start);
            var g = Get(goal);

            var open = new List<int> { start };
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [start] = 0f };
            var closed = new HashSet<int>();
            int best = start;
            float bestH = Dist(s, g);

            while (open.Count > 0)
            {
                int currentIndex = 0;
                float currentF = float.PositiveInfinity;
                for (int i = 0; i < open.Count; i++)
                {
                    var n = nodes[open[i]];
                    float f = gScore[n.Id] + Dist(n, g);
                    // Ties broken by id for determinism.
                    if (f < currentF || (f == currentF && n.Id < open[currentIndex]))
                    {
                        currentF = f;
                        currentIndex = i;
                    }
                }

                int current = open[currentIndex];
                open.RemoveAt(currentIndex);
                if (current == goal)
                {
                    Reconstruct(cameFrom, goal, path);
                    return true;
                }
                closed.Add(current);

                var cn = nodes[current];
                float h = Dist(cn, g);
                if (h < bestH || (h == bestH && current < best))
                {
                    bestH = h;
                    best = current;
                }

                foreach (int next in cn.Links)
                {
                    if (closed.Contains(next) || !passable(current, next)) continue;
                    float tentative = gScore[current] + Dist(cn, nodes[next]);
                    if (gScore.TryGetValue(next, out var known) && tentative >= known) continue;
                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            Reconstruct(cameFrom, best, path);
            return false;
        }

        /// <summary>Id of the node nearest to a point, or -1 for an empty graph.</summary>
        public int Nearest(float x, float y, float z)
        {
            int best = -1;
            float bestD = float.PositiveInfinity;
            foreach (var n in nodes.Values)
            {
                float dx = n.X - x, dy = n.Y - y, dz = n.Z - z;
                float d = dx * dx + dy * dy + dz * dz;
                if (d < bestD || (d == bestD && n.Id < best))
                {
                    bestD = d;
                    best = n.Id;
                }
            }
            return best;
        }

        private static void Reconstruct(Dictionary<int, int> cameFrom, int end, List<int> path)
        {
            path.Add(end);
            while (cameFrom.TryGetValue(end, out var prev))
            {
                path.Add(prev);
                end = prev;
            }
            path.Reverse();
        }

        private Node Get(int id)
        {
            if (!nodes.TryGetValue(id, out var n)) throw new KeyNotFoundException("Unknown nav node " + id);
            return n;
        }

        private static float Dist(Node a, Node b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private sealed class Node
        {
            public Node(int id, float x, float y, float z)
            {
                Id = id;
                X = x;
                Y = y;
                Z = z;
            }

            public int Id { get; }
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public List<int> Links { get; } = new List<int>();
        }
    }
}
