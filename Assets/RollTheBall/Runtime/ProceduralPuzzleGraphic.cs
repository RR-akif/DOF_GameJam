using UnityEngine;
using UnityEngine.UI;

namespace RollTheBall
{
    // Mesh-only UI art: no sprite, texture, material, Resources art, or shader dependency.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProceduralPuzzleGraphic : MaskableGraphic
    {
        public enum Shape { RoundedRectangle, Circle, Ball, Wood }
        public Shape shape;
        public float cornerRadius = 10f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (shape == Shape.Circle || shape == Shape.Ball)
            {
                float radius = Mathf.Min(rect.width, rect.height) * .5f;
                Disc(mesh, rect.center, radius, color, shape == Shape.Ball);
                if (shape == Shape.Ball)
                    Disc(mesh, rect.center + new Vector2(-.28f, .3f) * radius,
                        radius * .18f, new Color(1f, 1f, 1f, .72f), false);
                return;
            }
            Rounded(mesh, rect, Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .5f), color);
            if (shape == Shape.Wood)
            {
                // Deterministic short grain lines stay inside the rounded tile edge.
                for (int row = 0; row < 7; row++)
                {
                    float y = rect.yMin + rect.height * (.16f + row * .105f);
                    float inset = rect.width * (.13f + .04f * Mathf.Sin(row * 2.1f));
                    Rounded(mesh, new Rect(rect.xMin + inset, y, rect.width - 2 * inset, 1.3f), .6f,
                        new Color(.25f, .13f, .065f, .13f));
                }
            }
        }

        private static void Vertex(VertexHelper mesh, Vector2 point, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = point;
            vertex.color = tint;
            mesh.AddVert(vertex);
        }

        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint, bool shaded)
        {
            int first = mesh.currentVertCount;
            Vertex(mesh, center, shaded ? Color.Lerp(tint, Color.white, .42f) : tint);
            const int count = 48;
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                var radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Color shade = shaded ? Color.Lerp(tint * .45f, tint, (radial.y - radial.x + 2f) * .25f) : tint;
                shade.a = tint.a;
                Vertex(mesh, center + radial * radius, shade);
            }
            for (int i = 0; i < count; i++) mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % count);
        }

        private static void Rounded(VertexHelper mesh, Rect rect, float radius, Color tint)
        {
            int first = mesh.currentVertCount;
            Vertex(mesh, rect.center, tint);
            const int subdivisions = 8;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int i = 0; i <= subdivisions; i++)
                {
                    float a = (corner * 90f + i * 90f / subdivisions) * Mathf.Deg2Rad;
                    Vertex(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint);
                }
            }
            int perimeter = 4 * (subdivisions + 1);
            for (int i = 0; i < perimeter; i++) mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % perimeter);
        }
    }
}
