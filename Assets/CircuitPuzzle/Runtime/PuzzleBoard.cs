
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CircuitPuzzle
{
    // Vector UI art: crisp at any resolution,
    // no textures or downloaded assets needed.
    public class PuzzleBoard : MaskableGraphic,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler
    {
        public PuzzleState State { get; private set; }

        public System.Action changed;

        private int pointer = int.MinValue;

        // ============================================================
        // BIND PUZZLE STATE
        // ============================================================

        public void Bind(PuzzleState state)
        {
            State = state;
            pointer = int.MinValue;

            Debug.Log("PUZZLE: Board bound to PuzzleState.");

            SetVerticesDirty();
        }

        // ============================================================
        // BOARD POSITION / SPACING
        // ============================================================

        float Spacing
        {
            get
            {
                return Mathf.Min(
                    rectTransform.rect.width / (State.level.width + 1),
                    rectTransform.rect.height / (State.level.height + 1)
                );
            }
        }

        Vector2 Point(int x, int y)
        {
            return rectTransform.rect.center +
                   new Vector2(
                       x - (State.level.width - 1) * 0.5f,
                       y - (State.level.height - 1) * 0.5f
                   ) * Spacing;
        }

        // ============================================================
        // DRAWING HELPERS
        // ============================================================

        void Quad(
            VertexHelper vh,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color col)
        {
            int n = vh.currentVertCount;

            vh.AddVert(a, col, Vector2.zero);
            vh.AddVert(b, col, Vector2.zero);
            vh.AddVert(c, col, Vector2.zero);
            vh.AddVert(d, col, Vector2.zero);

            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }

        void Line(
            VertexHelper vh,
            Vector2 a,
            Vector2 b,
            float w,
            Color col)
        {
            Vector2 n = new Vector2(
                -(b - a).y,
                (b - a).x
            ).normalized * w * 0.5f;

            Quad(
                vh,
                a - n,
                a + n,
                b + n,
                b - n,
                col
            );
        }

        void Square(
            VertexHelper vh,
            Vector2 p,
            float r,
            Color c)
        {
            Quad(
                vh,
                p + new Vector2(-r, -r),
                p + new Vector2(-r, r),
                p + new Vector2(r, r),
                p + new Vector2(r, -r),
                c
            );
        }

        void Ring(
            VertexHelper vh,
            Vector2 p,
            float r,
            float w,
            Color c)
        {
            Line(
                vh,
                p + new Vector2(-r, -r),
                p + new Vector2(-r, r),
                w,
                c
            );

            Line(
                vh,
                p + new Vector2(-r, r),
                p + new Vector2(r, r),
                w,
                c
            );

            Line(
                vh,
                p + new Vector2(r, r),
                p + new Vector2(r, -r),
                w,
                c
            );

            Line(
                vh,
                p + new Vector2(r, -r),
                p + new Vector2(-r, -r),
                w,
                c
            );
        }

        // ============================================================
        // DRAW THE PUZZLE
        // ============================================================

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (State == null)
                return;

            var l = State.level;

            float s = Spacing;

            Color grid = new Color(
                0.12f,
                0.25f,
                0.28f
            );

            // Horizontal grid lines
            for (int y = 0; y < l.height; y++)
            {
                Line(
                    vh,
                    Point(0, y),
                    Point(l.width - 1, y),
                    2,
                    grid
                );
            }

            // Vertical grid lines
            for (int x = 0; x < l.width; x++)
            {
                Line(
                    vh,
                    Point(x, 0),
                    Point(x, l.height - 1),
                    2,
                    grid
                );
            }

            // Draw current paths
            for (int i = 0; i < State.paths.Count; i++)
            {
                var path = State.paths[i];

                Color col =
                    PuzzleLevel.ColorFor(State.inks[i]);

                for (int j = 1; j < path.Count; j++)
                {
                    Line(
                        vh,
                        Point(
                            path[j - 1].x,
                            path[j - 1].y
                        ),
                        Point(
                            path[j].x,
                            path[j].y
                        ),
                        s * 0.16f,
                        col
                    );
                }

                foreach (var p in path)
                {
                    Square(
                        vh,
                        Point(p.x, p.y),
                        s * 0.08f,
                        col
                    );
                }
            }

            // Draw puzzle elements
            for (int y = 0; y < l.height; y++)
            {
                for (int x = 0; x < l.width; x++)
                {
                    var cell = l.At(x, y);

                    var p = Point(x, y);

                    var col =
                        PuzzleLevel.ColorFor(cell.ink);

                    float r = s * 0.18f;

                    switch (cell.element)
                    {
                        case Element.Start:

                            Square(
                                vh,
                                p,
                                r,
                                col
                            );

                            break;

                        case Element.End:

                            Ring(
                                vh,
                                p,
                                r,
                                s * 0.055f,
                                col
                            );

                            break;

                        case Element.Fail:

                            Line(
                                vh,
                                p + new Vector2(-r, -r),
                                p + new Vector2(r, r),
                                s * 0.06f,
                                col
                            );

                            Line(
                                vh,
                                p + new Vector2(-r, r),
                                p + new Vector2(r, -r),
                                s * 0.06f,
                                col
                            );

                            break;

                        case Element.Dot:

                            for (int j = 0; j < 16; j++)
                            {
                                float a =
                                    j * Mathf.PI / 8;

                                float b =
                                    (j + 1) * Mathf.PI / 8;

                                Quad(
                                    vh,
                                    p,
                                    p + new Vector2(
                                        Mathf.Cos(a),
                                        Mathf.Sin(a)
                                    ) * s * 0.10f,
                                    p + new Vector2(
                                        Mathf.Cos(b),
                                        Mathf.Sin(b)
                                    ) * s * 0.10f,
                                    p,
                                    col
                                );
                            }

                            break;
                    }
                }
            }
        }

        // ============================================================
        // DETECT WHICH GRID CELL THE MOUSE IS OVER
        // ============================================================

        bool Hit(
            PointerEventData e,
            out Vector2Int cell)
        {
            cell = default(Vector2Int);

            if (State == null)
            {
                Debug.LogError(
                    "PUZZLE: Hit() failed because State is NULL."
                );

                return false;
            }

            Vector2 p;

            bool inside =
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    e.position,
                    e.pressEventCamera,
                    out p
                );

            if (!inside)
            {
                Debug.Log(
                    "PUZZLE: ScreenPointToLocalPoint failed."
                );

                return false;
            }

            Vector2 origin = Point(0, 0);

            Vector2 q =
                (p - origin) / Spacing;

            cell = new Vector2Int(
                Mathf.RoundToInt(q.x),
                Mathf.RoundToInt(q.y)
            );

            bool valid =
                cell.x >= 0 &&
                cell.y >= 0 &&
                cell.x < State.level.width &&
                cell.y < State.level.height &&
                (q - (Vector2)cell).sqrMagnitude < 0.25f;

            if (!valid)
            {
                Debug.Log(
                    "PUZZLE: Mouse is not over a valid cell. " +
                    "Calculated cell = " + cell
                );
            }

            return valid;
        }

        // ============================================================
        // REFRESH BOARD
        // ============================================================

        void Notify()
        {
            SetVerticesDirty();

            changed?.Invoke();
        }

        // ============================================================
        // DRAG INITIALIZATION
        // ============================================================

        public void OnInitializePotentialDrag(
            PointerEventData e)
        {
            Debug.Log(
                "PUZZLE: Initialize Potential Drag detected."
            );

            // Start dragging immediately.
            e.useDragThreshold = false;
        }

        // ============================================================
        // LEFT MOUSE BUTTON PRESSED
        // ============================================================

        public void OnPointerDown(
            PointerEventData e)
        {
            Debug.Log(
                "PUZZLE: Pointer Down detected."
            );

            Debug.Log(
                "PUZZLE: Pointer ID = " +
                e.pointerId
            );

            Debug.Log(
                "PUZZLE: Mouse position = " +
                e.position
            );

            if (e.button !=
                PointerEventData.InputButton.Left)
            {
                Debug.Log(
                    "PUZZLE: Not left mouse button."
                );

                return;
            }

            if (pointer != int.MinValue)
            {
                Debug.Log(
                    "PUZZLE: Another pointer is already active."
                );

                return;
            }

            if (State == null)
            {
                Debug.LogError(
                    "PUZZLE: State is NULL."
                );

                return;
            }

            if (State.Solved)
            {
                Debug.Log(
                    "PUZZLE: Puzzle is already solved."
                );

                return;
            }

            Vector2Int p;

            if (!Hit(e, out p))
            {
                Debug.Log(
                    "PUZZLE: Pointer Down did not hit a valid grid cell."
                );

                return;
            }

            Debug.Log(
                "PUZZLE: Pointer Down hit cell = " +
                p
            );

            bool selected =
                State.Select(p);

            Debug.Log(
                "PUZZLE: State.Select returned = " +
                selected
            );

            if (selected)
            {
                pointer = e.pointerId;

                Debug.Log(
                    "PUZZLE: START SELECTED successfully."
                );

                Debug.Log(
                    "PUZZLE: Stored pointer ID = " +
                    pointer
                );

                Notify();
            }
            else
            {
                Debug.Log(
                    "PUZZLE: This cell cannot start a path."
                );
            }
        }

        // ============================================================
        // MOUSE DRAG
        // ============================================================

        public void OnDrag(
            PointerEventData e)
        {
            Debug.Log(
                "PUZZLE: Drag detected."
            );

            if (e.pointerId != pointer)
            {
                Debug.Log(
                    "PUZZLE: Pointer mismatch. " +
                    "Drag pointer = " +
                    e.pointerId +
                    ", stored pointer = " +
                    pointer
                );

                return;
            }

            if (State == null)
            {
                Debug.LogError(
                    "PUZZLE: State became NULL during drag."
                );

                return;
            }

            if (State.Solved)
            {
                Debug.Log(
                    "PUZZLE: Puzzle already solved."
                );

                return;
            }

            Vector2Int p;

            if (!Hit(e, out p))
            {
                return;
            }

            Debug.Log(
                "PUZZLE: Drag currently over cell = " +
                p
            );

            var path =
                State.paths[State.active];

            var last =
                path[path.Count - 1];

            Debug.Log(
                "PUZZLE: Last path cell = " +
                last
            );

            // Only allow movement in a straight row/column.
            if (last.x != p.x &&
                last.y != p.y)
            {
                Debug.Log(
                    "PUZZLE: Diagonal movement ignored."
                );

                return;
            }

            var delta =
                new Vector2Int(
                    System.Math.Sign(
                        p.x - last.x
                    ),
                    System.Math.Sign(
                        p.y - last.y
                    )
                );

            int distance =
                Mathf.Abs(
                    p.x - last.x
                ) +
                Mathf.Abs(
                    p.y - last.y
                );

            for (int i = 0;
                 i < distance;
                 i++)
            {
                last += delta;

                bool success =
                    State.Step(last);

                Debug.Log(
                    "PUZZLE: Step to " +
                    last +
                    " returned " +
                    success
                );

                if (!success)
                {
                    break;
                }
            }

            Notify();
        }

        // ============================================================
        // LEFT MOUSE RELEASED
        // ============================================================

        public void OnPointerUp(
            PointerEventData e)
        {
            Debug.Log(
                "PUZZLE: Pointer Up detected."
            );

            Debug.Log(
                "PUZZLE: Released pointer = " +
                e.pointerId +
                ", stored pointer = " +
                pointer
            );

            if (e.pointerId == pointer)
            {
                pointer = int.MinValue;

                Debug.Log(
                    "PUZZLE: Pointer released successfully."
                );
            }
        }
    }
}