using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DegreesOfFreedom.EulerPath.Editor
{
    // No NUnit/Test Framework dependency. These checks execute the shipped C# implementation.
    public static class EulerPathSelfTests
    {
        private static int checks;

        [MenuItem("Tools/Euler Path/Run graph and tracing checks")]
        public static void RunCore()
        {
            checks = 0;
            var graph = EulerPathDefaultLayout.Create();
            string error;
            Check(EulerPathValidator.IsValidEulerLayout(graph, out error), error);
            Check(graph.edges.Length == 17 && graph.nodes.Length == 13, "Default size");
            foreach (int degree in EulerPathValidator.Degrees(graph).Values) Check(degree % 2 == 0, "Default even degree");
            var asset = AssetDatabase.LoadAssetAtPath<EulerPathLayoutAsset>(EulerPathEditorTools.LayoutPath);
            Check(asset != null && EulerPathValidator.IsValidEulerLayout(asset.layout, out error), "Packaged layout asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EulerPathEditorTools.PrefabPath);
            Check(prefab != null && prefab.GetComponent<EulerPathPuzzleController>() != null, "Packaged prefab and script reference");
            Check(prefab.GetComponent<EulerPathPuzzleController>().LayoutAsset == asset, "Prefab layout reference");

            // Exercise branching in many different valid circuits, including each possible start.
            for (int seed = 0; seed < 240; seed++)
            {
                var session = new EulerTraceSession(graph);
                int solves = 0, edgeEvents = 0;
                session.Solved += () => solves++;
                session.EdgeTraced += edge => edgeEvents++;
                var route = EulerRoute(graph, graph.nodes[seed % graph.nodes.Length].id, seed);
                Check(session.Begin(session.Position(route[0])), "Begin generated route");
                for (int i = 1; i < route.Count; i++) session.Move(session.Position(route[i]));
                Check(session.IsSolved && session.TracedCount == graph.edges.Length && solves == 1 && edgeEvents == graph.edges.Length, "Valid route completes exactly once, seed " + seed);
                session.Move(Vector2.zero); session.Release(); session.Begin(Vector2.zero);
                Check(solves == 1, "No duplicate solved callback");
            }
            var s = new EulerTraceSession(graph);
            s.Begin(s.Position(0)); s.Move((s.Position(0) + s.Position(1)) * 0.5f);
            Check(s.TracedCount == 0 && s.PreviewEdge >= 0, "Half-edge is only a preview");
            s.Release();
            Check(s.TracedCount == 0 && s.PreviewEdge == -1 && s.CurrentNode == 0, "Release discards partial edge");
            Check(!s.Begin(s.Position(1)), "Cannot resume at a different node");
            Check(s.Begin(s.Position(0)), "Resume at current node");
            s.Move(s.Position(1));
            Check(s.TracedCount == 1 && s.CurrentNode == 1, "Full edge commits");
            s.Move(s.Position(0));
            Check(s.TracedCount == 1 && s.CurrentNode == 1, "Reverse retrace rejected");
            s.Move(s.Position(1)); s.Move(s.Position(2));
            Check(s.TracedCount == 2 && s.CurrentNode == 2, "Recover from rejected retrace without releasing");
            s.Reset(); s.Begin(s.Position(4)); s.Move(s.Position(2));
            Check(s.TracedCount == 0 && s.CurrentNode == 4, "No jumping across off-edge space");
            s.Move(s.Position(4)); s.Move(s.Position(0));
            Check(s.TracedCount == 3 && s.CurrentNode == 0, "Fast collinear sweep crosses three edges");
            s.Reset(); s.Begin(s.Position(0));
            foreach (int node in new[] { 1, 2, 9, 6, 11, 0 }) s.Move(s.Position(node));
            Check(s.IsDeadEnd && !s.IsSolved && s.TracedCount == 6, "Premature closed loop is a dead end");
            s.Reset(); Check(!s.HasStarted && s.TracedCount == 0 && !s.IsDragging, "Restart clears all state");

            var open = Chain();
            Check(EulerPathValidator.IsValidEulerLayout(open, out error), "Two-odd layout is valid");
            s = new EulerTraceSession(open);
            Check(!s.Begin(s.Position(20)), "Two-odd layout rejects even start");
            Check(s.Begin(s.Position(10)), "Two-odd layout accepts odd start");
            s.Move(s.Position(20)); s.Release(); s.Begin(s.Position(20)); s.Move(s.Position(30));
            Check(s.IsSolved && s.CurrentNode == 30, "Open path finishes at other odd node after resume");
            s = new EulerTraceSession(open); s.Begin(s.Position(30)); s.Move(s.Position(20)); s.Move(s.Position(10));
            Check(s.IsSolved, "Either odd endpoint is a valid start");
            s = new EulerTraceSession(open); open.nodes[0].position = new Vector2(999, 999);
            Check(s.Position(10) == new Vector2(0, 0), "Session copies authored data");

            Invalid(null, "Null graph");
            var bad = graph.Copy(); bad.nodes[1].id = bad.nodes[0].id; Invalid(bad, "Duplicate IDs");
            bad = graph.Copy(); bad.nodes[1].position = bad.nodes[0].position; Invalid(bad, "Overlapping node positions");
            bad = graph.Copy(); bad.nodes[0].position.x = float.NaN; Invalid(bad, "Non-finite coordinate");
            bad = graph.Copy(); bad.edges[0].nodeA = 999; Invalid(bad, "Missing endpoint");
            bad = graph.Copy(); bad.edges[0] = new PuzzleEdge(0, 0); Invalid(bad, "Self-loop");
            bad = graph.Copy(); bad.edges[0] = new PuzzleEdge(graph.edges[1].nodeB, graph.edges[1].nodeA); Invalid(bad, "Duplicate undirected edge");
            bad = new EulerPathLayout
            {
                nodes = new[] { N(0, 0, 0), N(1, 100, 0), N(2, 0, 100), N(3, 300, 0), N(4, 400, 0), N(5, 300, 100) },
                edges = new[] { E(0, 1), E(1, 2), E(2, 0), E(3, 4), E(4, 5), E(5, 3) }
            };
            Invalid(bad, "Disconnected even-degree cycles");
            bad = new EulerPathLayout { nodes = new[] { N(0, 0, 0), N(1, 100, 0), N(2, 0, 100), N(3, -100, 0) },
                edges = new[] { E(0, 1), E(0, 2), E(0, 3) } };
            Invalid(bad, "Four odd nodes");
            bad = new EulerPathLayout { nodes = new[] { N(0, 0, 0), N(1, 100, 100), N(2, 0, 100), N(3, 100, 0) },
                edges = new[] { E(0, 1), E(1, 2), E(2, 3), E(3, 0) } };
            Invalid(bad, "Unsplit crossing");
            Debug.Log("Euler Path graph/tracing checks PASSED (" + checks + " assertions, 240 full circuits).");
        }

        [MenuItem("Tools/Euler Path/Run lifecycle checks (Play Mode)")]
        public static void RunLifecycle()
        {
            if (!Application.isPlaying) { Debug.LogError("Enter Play Mode in an empty test scene first."); return; }
            if (EulerPathPuzzleController.IsMainGameInputLocked) { Debug.LogError("Close the existing Euler popup first."); return; }
            checks = 0;
            float oldTime = Time.timeScale;
            bool oldVisible = Cursor.visible;
            var oldCursor = Cursor.lockState;
            var go = new GameObject("EulerLifecycleFixture");
            var probeGo = new GameObject("EulerInputProbe");
            var probe = probeGo.AddComponent<EulerInputLockProbe>();
            var disabledProbe = probeGo.AddComponent<EulerInputLockProbe>(); disabledProbe.enabled = false;
            var puzzle = go.AddComponent<EulerPathPuzzleController>();
            int solves = 0;
            puzzle.OnPuzzleSolved += () => { Check(puzzle.IsOpen, "Solved event precedes close"); solves++; };
#if ENABLE_INPUT_SYSTEM
            var enabledAction = new InputAction("EulerTestEnabled", binding: "<Keyboard>/space");
            var disabledAction = new InputAction("EulerTestDisabled", binding: "<Keyboard>/enter");
            enabledAction.Enable();
#endif
            try
            {
                Check(!puzzle.IsOpen && !EulerPathPuzzleController.IsMainGameInputLocked, "Initially closed");
                puzzle.StartPuzzle(); puzzle.StartPuzzle();
                Check(puzzle.IsOpen && Time.timeScale == 0 && !probe.enabled && !disabledProbe.enabled, "Open locks gameplay; repeated Start is idempotent");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Cursor available");
#if ENABLE_INPUT_SYSTEM
                Check(!enabledAction.enabled && !disabledAction.enabled, "Input actions disabled");
#endif
                puzzle.CancelPuzzle(); puzzle.CancelPuzzle();
                Check(!puzzle.IsOpen && solves == 0 && !EulerPathPuzzleController.IsMainGameInputLocked, "Cancel never fires success");
                Check(probe.enabled && !disabledProbe.enabled && Time.timeScale == oldTime, "Exact script/time state restored");
#if ENABLE_INPUT_SYSTEM
                Check(enabledAction.enabled && !disabledAction.enabled, "Exact action state restored");
#endif
                Check(Cursor.visible == oldVisible && Cursor.lockState == oldCursor, "Cursor state restored");
                puzzle.StartPuzzle();
                var state = (EulerTraceSession)typeof(EulerPathPuzzleController).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(puzzle);
                var route = EulerRoute(state.Layout, 0, 42);
                state.Begin(state.Position(route[0]));
                for (int i = 1; i < route.Count; i++) state.Move(state.Position(route[i]));
                Check(solves == 1 && !puzzle.IsOpen && !EulerPathPuzzleController.IsMainGameInputLocked, "Success event and automatic close");
                puzzle.StartPuzzle(); Check(puzzle.TracedEdgeCount == 0, "Reopen is fresh");
                go.SetActive(false);
                Check(!EulerPathPuzzleController.IsMainGameInputLocked && probe.enabled && Time.timeScale == oldTime, "Host disable restores state");
                go.SetActive(true); puzzle.StartPuzzle();
                UnityEngine.Object.DestroyImmediate(go);
                Check(!EulerPathPuzzleController.IsMainGameInputLocked && probe.enabled && Time.timeScale == oldTime, "Host destroy restores state");
                Debug.Log("Euler Path lifecycle checks PASSED (" + checks + " assertions).");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(probeGo);
#if ENABLE_INPUT_SYSTEM
                enabledAction.Dispose(); disabledAction.Dispose();
#endif
                Time.timeScale = oldTime; Cursor.visible = oldVisible; Cursor.lockState = oldCursor;
            }
        }

        private static List<int> EulerRoute(EulerPathLayout graph, int start, int seed)
        {
            var neighbours = new Dictionary<int, List<int>>();
            foreach (var n in graph.nodes) neighbours[n.id] = new List<int>();
            for (int i = 0; i < graph.edges.Length; i++) { neighbours[graph.edges[i].nodeA].Add(i); neighbours[graph.edges[i].nodeB].Add(i); }
            var random = new System.Random(seed);
            foreach (var list in neighbours.Values)
                for (int i = list.Count - 1; i > 0; i--) { int j = random.Next(i + 1), temp = list[i]; list[i] = list[j]; list[j] = temp; }
            var stack = new Stack<int>(); var route = new List<int>(); var used = new HashSet<int>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                int node = stack.Peek(); var edges = neighbours[node];
                while (edges.Count > 0 && used.Contains(edges[edges.Count - 1])) edges.RemoveAt(edges.Count - 1);
                if (edges.Count == 0) { route.Add(stack.Pop()); continue; }
                int index = edges[edges.Count - 1]; edges.RemoveAt(edges.Count - 1); used.Add(index);
                var edge = graph.edges[index]; stack.Push(edge.nodeA == node ? edge.nodeB : edge.nodeA);
            }
            route.Reverse(); return route;
        }
        private static EulerPathLayout Chain()
        { return new EulerPathLayout { nodes = new[] { N(10, 0, 0), N(20, 100, 0), N(30, 200, 0) }, edges = new[] { E(10, 20), E(20, 30) } }; }
        private static PuzzleNode N(int id, float x, float y) { return new PuzzleNode(id, new Vector2(x, y)); }
        private static PuzzleEdge E(int a, int b) { return new PuzzleEdge(a, b); }
        private static void Invalid(EulerPathLayout layout, string name)
        { string error; Check(!EulerPathValidator.IsValidEulerLayout(layout, out error) && !string.IsNullOrEmpty(error), name); }
        private static void Check(bool condition, string description)
        { checks++; if (!condition) throw new Exception("Euler Path check failed: " + description); }
    }
}
