using UnityEngine;
using UnityEngine.UI;

namespace DegreesOfFreedom.EulerPath
{
    // Single generated UI mesh for all rounded edges, node dots, and current-node ring.
    // No sprites, textures, shaders, materials, LineRenderers, or art imports.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EulerPathGraphic : MaskableGraphic
    {
        public Color ghostColor = new Color(0.56f, 0.53f, 0.70f, 1);
        public Color tracedColor = new Color(1, 0.81f, 0.25f, 1);
        public Color nodeColor = new Color(0.81f, 0.78f, 0.94f, 1);
        private EulerTraceSession session;
        private Vector2 center;
        private float scale = 1;

        public void SetSession(EulerTraceSession value) { session = value; SetVerticesDirty(); }
        public void Refresh() { SetVerticesDirty(); }
        public Vector2 ScreenToGraph(Vector2 screenPoint)
        {
            UpdateTransform();
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out local);
            return (local - rectTransform.rect.center) / scale + center;
        }
        public Vector2 GraphToLocal(Vector2 p) { return (p - center) * scale + rectTransform.rect.center; }

        private void UpdateTransform()
        {
            if (session == null) return;
            Vector2 min = session.Layout.nodes[0].position, max = min;
            foreach (var n in session.Layout.nodes) { min = Vector2.Min(min, n.position); max = Vector2.Max(max, n.position); }
            center = (min + max) * 0.5f;
            Vector2 span = max - min;
            scale = Mathf.Max(0.01f, Mathf.Min((rectTransform.rect.width - 60) / Mathf.Max(1, span.x),
                (rectTransform.rect.height - 60) / Mathf.Max(1, span.y)));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (session == null) return;
            UpdateTransform();
            float radius = Mathf.Clamp(6f * scale, 3f, 8f);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < session.Layout.edges.Length; i++)
                {
                    bool visited = session.IsTraced(i);
                    if (visited != (pass == 1)) continue;
                    var e = session.Layout.edges[i];
                    Capsule(vh, GraphToLocal(session.Position(e.nodeA)), GraphToLocal(session.Position(e.nodeB)), radius,
                        visited ? tracedColor : ghostColor);
                }
            if (session.PreviewEdge >= 0)
                Capsule(vh, GraphToLocal(session.Position(session.CurrentNode)), GraphToLocal(session.PreviewPoint), radius,
                    new Color(tracedColor.r, tracedColor.g, tracedColor.b, 0.65f));
            foreach (var n in session.Layout.nodes)
            {
                Vector2 p = GraphToLocal(n.position);
                if (session.IsOdd(n.id)) Ring(vh, p, radius + 9, 2, tracedColor);
                Disc(vh, p, radius + 2.5f, new Color(0.12f, 0.13f, 0.22f, 1));
                Disc(vh, p, radius - 0.5f, nodeColor);
            }
            if (session.HasStarted)
            {
                Vector2 p = GraphToLocal(session.Position(session.CurrentNode));
                Ring(vh, p, radius + 12, 2.5f, tracedColor);
                Disc(vh, p, radius, tracedColor);
            }
        }

        private static void Capsule(VertexHelper vh, Vector2 a, Vector2 b, float radius, Color color)
        {
            Vector2 d = (b - a).normalized, p = new Vector2(-d.y, d.x) * radius;
            int i = vh.currentVertCount;
            vh.AddVert(a + p, color, Vector2.zero); vh.AddVert(b + p, color, Vector2.zero);
            vh.AddVert(b - p, color, Vector2.zero); vh.AddVert(a - p, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
            Disc(vh, a, radius, color); Disc(vh, b, radius, color);
        }
        private static void Disc(VertexHelper vh, Vector2 p, float radius, Color color)
        {
            const int segments = 24;
            int index = vh.currentVertCount;
            vh.AddVert(p, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vh.AddVert(p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0) vh.AddTriangle(index, index + i, index + i + 1);
            }
        }
        private static void Ring(VertexHelper vh, Vector2 p, float radius, float width, Color color)
        {
            const int segments = 40;
            for (int i = 0; i < segments; i++)
            {
                float a = 2 * Mathf.PI * i / segments, b = 2 * Mathf.PI * (i + 1) / segments;
                Vector2 va = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), vb = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = vh.currentVertCount;
                vh.AddVert(p + va * radius, color, Vector2.zero); vh.AddVert(p + vb * radius, color, Vector2.zero);
                vh.AddVert(p + vb * (radius - width), color, Vector2.zero); vh.AddVert(p + va * (radius - width), color, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
