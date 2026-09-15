using UnityEngine;

namespace DegreesOfFreedom.EulerPath
{
    // Fixed asymmetric geometry: an irregular pentagon and a skewed quadrilateral.
    // All four crossings are split into shared nodes. No runtime randomization.
    public static class EulerPathDefaultLayout
    {
        public static EulerPathLayout Create()
        {
            return new EulerPathLayout
            {
                nodes = new[]
                {
                    new PuzzleNode(0, new Vector2(-300, 70)),
                    new PuzzleNode(1, new Vector2(-180, 270)),
                    new PuzzleNode(2, new Vector2(100, 210)),
                    new PuzzleNode(3, new Vector2(200, -110)),
                    new PuzzleNode(4, new Vector2(-140, -230)),
                    new PuzzleNode(5, new Vector2(-340, -110)),
                    new PuzzleNode(6, new Vector2(-120, 130)),
                    new PuzzleNode(7, new Vector2(300, 80)),
                    new PuzzleNode(8, new Vector2(230, -270)),
                    new PuzzleNode(9, new Vector2(134.46676971f, 99.70633694f)),
                    new PuzzleNode(10, new Vector2(-39.21824104f, -194.42996743f)),
                    new PuzzleNode(11, new Vector2(-254.02298851f, -16.20689655f)),
                    new PuzzleNode(12, new Vector2(-180.05502063f, -154.89683631f))
                },
                edges = new[]
                {
                    new PuzzleEdge(0, 1), new PuzzleEdge(1, 2), new PuzzleEdge(2, 9),
                    new PuzzleEdge(9, 3), new PuzzleEdge(3, 10), new PuzzleEdge(10, 4),
                    new PuzzleEdge(4, 12), new PuzzleEdge(12, 11), new PuzzleEdge(11, 0),
                    new PuzzleEdge(5, 11), new PuzzleEdge(11, 6), new PuzzleEdge(6, 9),
                    new PuzzleEdge(9, 7), new PuzzleEdge(7, 8), new PuzzleEdge(8, 10),
                    new PuzzleEdge(10, 12), new PuzzleEdge(12, 5)
                }
            };
        }
    }
}
