using System.Collections.Generic;
using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    public static class EulerPathValidator
    {
        public static bool IsValidEulerLayout(EulerPathLayout layout, out string error)
        {
            error = null;
            if (layout == null || layout.nodes == null || layout.edges == null ||
                layout.nodes.Length < 2 || layout.edges.Length == 0)
                return Fail("Provide at least two nodes and one edge.", out error);

            var positions = new Dictionary<int, Vector2>();
            var neighbours = new Dictionary<int, List<int>>();
            foreach (var node in layout.nodes)
            {
                if (positions.ContainsKey(node.id)) return Fail("Duplicate node ID: " + node.id, out error);
                if (!Finite(node.position.x) || !Finite(node.position.y))
                    return Fail("Node " + node.id + " has a non-finite position.", out error);
                foreach (var other in positions.Values)
                    if ((other - node.position).sqrMagnitude < 0.01f)
                        return Fail("Distinct nodes occupy the same position.", out error);
                positions.Add(node.id, node.position);
                neighbours.Add(node.id, new List<int>());
            }
            var edgeKeys = new HashSet<string>();
            foreach (var edge in layout.edges)
            {
                if (!positions.ContainsKey(edge.nodeA) || !positions.ContainsKey(edge.nodeB))
                    return Fail("An edge references a missing node.", out error);
                if (edge.nodeA == edge.nodeB) return Fail("Self-loops are not supported by straight-line tracing.", out error);
                string key = edge.nodeA < edge.nodeB ? edge.nodeA + ":" + edge.nodeB : edge.nodeB + ":" + edge.nodeA;
                if (!edgeKeys.Add(key)) return Fail("Duplicate undirected edge: " + key, out error);
                neighbours[edge.nodeA].Add(edge.nodeB);
                neighbours[edge.nodeB].Add(edge.nodeA);
            }
            int odd = 0;
            foreach (var pair in neighbours)
            {
                if (pair.Value.Count == 0) return Fail("Isolated node: " + pair.Key, out error);
                if (pair.Value.Count % 2 == 1) odd++;
            }
            if (odd != 0 && odd != 2)
                return Fail("Euler rule failed: " + odd + " odd-degree nodes; require exactly 0 or 2.", out error);
            var reached = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(layout.nodes[0].id);
            while (pending.Count > 0)
            {
                int id = pending.Pop();
                if (!reached.Add(id)) continue;
                foreach (int next in neighbours[id]) pending.Push(next);
            }
            if (reached.Count != layout.nodes.Length) return Fail("Graph is disconnected.", out error);

            // Rendering must agree with topology: split crossings and intermediate vertices.
            foreach (var edge in layout.edges)
                foreach (var node in layout.nodes)
                    if (node.id != edge.nodeA && node.id != edge.nodeB &&
                        PointOnSegment(node.position, positions[edge.nodeA], positions[edge.nodeB]))
                        return Fail("An edge passes through node " + node.id + "; split it at this node.", out error);
            for (int i = 0; i < layout.edges.Length; i++)
                for (int j = i + 1; j < layout.edges.Length; j++)
                {
                    var a = layout.edges[i]; var b = layout.edges[j];
                    if (a.nodeA == b.nodeA || a.nodeA == b.nodeB || a.nodeB == b.nodeA || a.nodeB == b.nodeB) continue;
                    if (Crosses(positions[a.nodeA], positions[a.nodeB], positions[b.nodeA], positions[b.nodeB]))
                        return Fail("Edges " + i + " and " + j + " cross without a shared node; split the crossing.", out error);
                }
            return true;
        }

        public static Dictionary<int, int> Degrees(EulerPathLayout layout)
        {
            var result = new Dictionary<int, int>();
            foreach (var node in layout.nodes) result[node.id] = 0;
            foreach (var edge in layout.edges) { result[edge.nodeA]++; result[edge.nodeB]++; }
            return result;
        }

        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
        private static bool Fail(string message, out string error) { error = message; return false; }
        private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
        private static bool PointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float t = Vector2.Dot(p - a, d) / d.sqrMagnitude;
            return t > 0 && t < 1 && (p - (a + t * d)).sqrMagnitude < 0.01f;
        }
        private static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 r = b - a, s = d - c;
            float denominator = Cross(r, s);
            if (Mathf.Abs(denominator) < 0.001f) return false;
            float t = Cross(c - a, s) / denominator, u = Cross(c - a, r) / denominator;
            return t > 0 && t < 1 && u > 0 && u < 1;
        }
    }
}
