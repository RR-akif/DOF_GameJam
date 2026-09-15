using System;
using System.Collections.Generic;
using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    // Pure graph/geometry state machine: no GameObject, UI, device, time, or scene dependency.
    public sealed class EulerTraceSession
    {
        public EulerPathLayout Layout { get; private set; }
        public int TracedCount { get; private set; }
        public int CurrentNode { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsSolved { get { return TracedCount == Layout.edges.Length; } }
        public int PreviewEdge { get; private set; }
        public Vector2 PreviewPoint { get; private set; }
        public string Hint { get; private set; }
        public event Action<int> EdgeTraced;
        public event Action Solved;

        private readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, List<int>> incident = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, int> degrees;
        private readonly bool[] traced;
        private readonly float nodeRadius, corridor;
        private readonly bool twoOdd;
        private bool mustReturn;
        private Vector2 previous;

        public EulerTraceSession(EulerPathLayout layout, float nodeRadius = 18f, float corridor = 20f)
        {
            string error;
            if (!EulerPathValidator.IsValidEulerLayout(layout, out error)) throw new ArgumentException(error, "layout");
            Layout = layout.Copy();
            this.nodeRadius = Mathf.Max(1, nodeRadius);
            this.corridor = Mathf.Max(1, corridor);
            traced = new bool[Layout.edges.Length];
            degrees = EulerPathValidator.Degrees(Layout);
            foreach (var node in Layout.nodes)
            {
                positions[node.id] = node.position;
                incident[node.id] = new List<int>();
                if (degrees[node.id] % 2 == 1) twoOdd = true;
            }
            for (int i = 0; i < Layout.edges.Length; i++)
            {
                incident[Layout.edges[i].nodeA].Add(i);
                incident[Layout.edges[i].nodeB].Add(i);
            }
            Reset();
        }

        public bool IsTraced(int edge) { return traced[edge]; }
        public Vector2 Position(int id) { return positions[id]; }
        public bool IsAllowedStart(int id) { return degrees.ContainsKey(id) && (!twoOdd || degrees[id] % 2 == 1); }
        public bool IsOdd(int id) { return degrees[id] % 2 == 1; }
        public bool IsDeadEnd
        {
            get
            {
                if (!HasStarted || IsSolved) return false;
                foreach (int edge in incident[CurrentNode]) if (!traced[edge]) return false;
                return true;
            }
        }

        public void Reset()
        {
            Array.Clear(traced, 0, traced.Length);
            TracedCount = 0; HasStarted = false; IsDragging = false;
            CurrentNode = 0; PreviewEdge = -1; mustReturn = false;
            Hint = twoOdd ? "Start at either glowing endpoint." : "Press any node and follow the lines.";
        }

        public bool Begin(Vector2 point)
        {
            if (IsSolved || IsDragging) return false;
            if (HasStarted)
            {
                if (Vector2.Distance(point, positions[CurrentNode]) > nodeRadius)
                { Hint = "Resume at the highlighted current node."; return false; }
            }
            else
            {
                int best = 0; float distance = float.MaxValue;
                foreach (var node in Layout.nodes)
                {
                    float d = Vector2.Distance(point, node.position);
                    if (d < distance) { distance = d; best = node.id; }
                }
                if (distance > nodeRadius) { Hint = "Press a node to start."; return false; }
                if (!IsAllowedStart(best)) { Hint = "This path must start at a glowing endpoint."; return false; }
                CurrentNode = best; HasStarted = true;
            }
            IsDragging = true; previous = point; mustReturn = false; PreviewEdge = -1;
            Hint = "Trace each edge once. Nodes can be revisited.";
            return true;
        }

        public void Release()
        {
            IsDragging = false; PreviewEdge = -1; mustReturn = false;
            if (!IsSolved && HasStarted)
                Hint = IsDeadEnd ? "No unused edge here. Select Restart to try again." : "Press the highlighted node to resume.";
        }

        public void Move(Vector2 point)
        {
            if (!IsDragging || IsSolved) return;
            Vector2 from = previous;
            previous = point;
            // Sweep the actual pointer segment so a fast drag cannot jump through off-edge space.
            int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, point) / (Mathf.Min(nodeRadius, corridor) * 0.3f)));
            if (count > 4096) { Reject("Return to the highlighted node."); return; }
            for (int i = 1; i <= count && IsDragging && !IsSolved; i++)
                Sample(Vector2.Lerp(from, point, (float)i / count));
        }

        private void Sample(Vector2 p)
        {
            Vector2 start = positions[CurrentNode];
            float fromStart = Vector2.Distance(p, start);
            if (mustReturn)
            {
                if (fromStart <= nodeRadius) { mustReturn = false; Hint = "Follow an unused lavender edge."; }
                return;
            }
            if (fromStart <= nodeRadius)
            {
                PreviewEdge = -1;
                return;
            }
            if (PreviewEdge < 0)
            {
                int best = -1; float bestDistance = float.MaxValue;
                foreach (int index in incident[CurrentNode])
                {
                    Vector2 end = positions[Other(index)];
                    Vector2 d = end - start;
                    float t = Vector2.Dot(p - start, d) / d.sqrMagnitude;
                    if (t <= 0 || t > 1 + nodeRadius / d.magnitude) continue;
                    float distance = Vector2.Distance(p, start + Mathf.Clamp01(t) * d);
                    if (distance < bestDistance) { bestDistance = distance; best = index; }
                }
                if (best < 0 || bestDistance > corridor) { Reject("Stay on a line; return to the highlighted node."); return; }
                if (traced[best]) { Reject("That edge is already traced. Choose an unused edge."); return; }
                PreviewEdge = best;
            }
            int edge = PreviewEdge, destination = Other(edge);
            Vector2 target = positions[destination], direction = target - start;
            float projection = Vector2.Dot(p - start, direction) / direction.sqrMagnitude;
            Vector2 closest = start + Mathf.Clamp01(projection) * direction;
            if (Vector2.Distance(p, closest) > corridor) { Reject("Stay on the line; return to the highlighted node."); return; }
            PreviewPoint = closest;
            if (Vector2.Distance(p, target) > nodeRadius) return;

            traced[edge] = true; TracedCount++; CurrentNode = destination; PreviewEdge = -1;
            Hint = IsDeadEnd ? "No unused edge here. Select Restart to try again." : "Keep tracing unused lavender edges.";
            if (EdgeTraced != null) EdgeTraced(edge);
            // The win check belongs to this edge-commit event, never to an update timer.
            if (IsSolved)
            {
                IsDragging = false;
                Hint = "All edges traced.";
                if (Solved != null) Solved();
            }
        }

        private int Other(int edge)
        {
            var e = Layout.edges[edge];
            return e.nodeA == CurrentNode ? e.nodeB : e.nodeA;
        }
        private void Reject(string hint) { PreviewEdge = -1; mustReturn = true; Hint = hint; }
    }
}
