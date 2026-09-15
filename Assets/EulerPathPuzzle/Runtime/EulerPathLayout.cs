using System;
using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    [Serializable]
    public struct PuzzleNode
    {
        public int id;
        public Vector2 position;
        public PuzzleNode(int id, Vector2 position) { this.id = id; this.position = position; }
    }

    [Serializable]
    public struct PuzzleEdge
    {
        public int nodeA;
        public int nodeB;
        public PuzzleEdge(int a, int b) { nodeA = a; nodeB = b; }
    }

    [Serializable]
    public class EulerPathLayout
    {
        public PuzzleNode[] nodes;
        public PuzzleEdge[] edges;

        public EulerPathLayout Copy()
        {
            return new EulerPathLayout
            {
                nodes = nodes == null ? null : (PuzzleNode[])nodes.Clone(),
                edges = edges == null ? null : (PuzzleEdge[])edges.Clone()
            };
        }
    }
}
