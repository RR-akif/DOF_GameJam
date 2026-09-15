using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollTheBall
{
    // No GameObjects, animation, input, or mutable layout-asset references live here.
    public sealed class RollTheBallBoard
    {
        private readonly GridCell[] floor;
        private readonly int[] occupants;
        private readonly SlideAxis[] axes;
        private readonly List<TileSlide> history = new List<TileSlide>();
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public Vector2Int Start { get; private set; }
        public Vector2Int Goal { get; private set; }
        public IReadOnlyList<TileSlide> History { get { return history.AsReadOnly(); } }

        // Occupancy: -2 = fixed carved tile, -1 = empty, >= 0 = solid movable ID.
        public RollTheBallBoard(RollTheBallLayout layout)
        {
            ValidateStructure(layout);
            Columns = layout.columns;
            Rows = layout.rows;
            Start = layout.startCell;
            Goal = layout.goalCell;
            floor = new GridCell[Columns * Rows];
            occupants = new int[floor.Length];
            axes = new SlideAxis[floor.Length];
            foreach (var cell in layout.cells)
            {
                int i = cell.row * Columns + cell.col;
                floor[i] = cell;
                occupants[i] = cell.tileType == TileType.Locked ? -2 : cell.startsEmpty ? -1 : i;
                axes[i] = cell.slideAxis;
            }
        }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;
        }

        private int Index(Vector2Int cell)
        {
            if (!Contains(cell)) throw new ArgumentOutOfRangeException("cell");
            return cell.y * Columns + cell.x;
        }

        public GridCell FloorAt(Vector2Int cell) { return floor[Index(cell)]; }
        public int OccupantAt(Vector2Int cell) { return occupants[Index(cell)]; }
        public bool IsMovable(Vector2Int cell) { return Contains(cell) && occupants[Index(cell)] >= 0; }
        public bool IsOpen(Vector2Int cell) { return Contains(cell) && occupants[Index(cell)] < 0; }
        public SlideAxis AxisAt(Vector2Int cell)
        {
            if (!IsMovable(cell)) throw new InvalidOperationException("Cell has no movable tile.");
            return axes[OccupantAt(cell)];
        }

        // Inclusive signed distance limits; scanning stops at the FIRST obstruction.
        public void GetSlideLimits(Vector2Int from, out int minimum, out int maximum)
        {
            minimum = maximum = 0;
            if (!IsMovable(from)) return;
            Vector2Int step = AxisAt(from) == SlideAxis.Horizontal ? Vector2Int.right : Vector2Int.up;
            var next = from - step;
            while (Contains(next) && OccupantAt(next) == -1) { minimum--; next -= step; }
            next = from + step;
            while (Contains(next) && OccupantAt(next) == -1) { maximum++; next += step; }
        }

        public bool CanSlide(Vector2Int from, Vector2Int to)
        {
            if (!IsMovable(from) || !Contains(to) || from == to) return false;
            bool horizontal = AxisAt(from) == SlideAxis.Horizontal;
            if (horizontal ? from.y != to.y : from.x != to.x) return false;
            int distance = horizontal ? to.x - from.x : to.y - from.y;
            int min, max;
            GetSlideLimits(from, out min, out max);
            return distance >= min && distance <= max;
        }

        public bool TrySlide(Vector2Int from, Vector2Int to)
        {
            if (!CanSlide(from, to)) return false;
            occupants[Index(to)] = occupants[Index(from)];
            occupants[Index(from)] = -1;
            history.Add(new TileSlide(from, to));
            return true;
        }

        public bool UndoLastSlide()
        {
            if (history.Count == 0) return false;
            var last = history[history.Count - 1];
            if (!CanSlide(last.to, last.from)) throw new InvalidOperationException("Invalid undo history.");
            occupants[Index(last.from)] = occupants[Index(last.to)];
            occupants[Index(last.to)] = -1;
            history.RemoveAt(history.Count - 1);
            return true;
        }

        private static readonly Vector2Int[] Directions =
            { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private static bool HasSide(GridCell cell, int direction)
        {
            switch (direction)
            {
                case 0: return cell.grooveUp;
                case 1: return cell.grooveRight;
                case 2: return cell.grooveDown;
                default: return cell.grooveLeft;
            }
        }

        public bool TryFindPath(out List<Vector2Int> path) { return TryFindPath(Start, out path); }

        // BFS requires reciprocal ports AND uncovered occupancy on both cells.
        public bool TryFindPath(Vector2Int ballCell, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (!IsOpen(ballCell) || !IsOpen(Goal)) return false;
            int[] previous = new int[floor.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            var pending = new Queue<Vector2Int>();
            int origin = Index(ballCell);
            previous[origin] = origin;
            pending.Enqueue(ballCell);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                if (current == Goal)
                {
                    int cursor = Index(current);
                    while (cursor != origin)
                    {
                        path.Add(new Vector2Int(cursor % Columns, cursor / Columns));
                        cursor = previous[cursor];
                    }
                    path.Add(ballCell);
                    path.Reverse();
                    return true;
                }
                for (int d = 0; d < 4; d++)
                {
                    var next = current + Directions[d];
                    if (!IsOpen(next) || previous[Index(next)] != -1) continue;
                    if (!HasSide(FloorAt(current), d) || !HasSide(FloorAt(next), (d + 2) % 4)) continue;
                    previous[Index(next)] = Index(current);
                    pending.Enqueue(next);
                }
            }
            return false;
        }

        public static void ValidatePlayable(RollTheBallLayout layout)
        {
            var board = new RollTheBallBoard(layout);
            List<Vector2Int> path;
            if (board.TryFindPath(out path)) throw new ArgumentException("Initial layout is already solved.");
            if (layout.debugSolution == null || layout.debugSolution.Length == 0)
                throw new ArgumentException("Author a debugSolution of legal slides to prove solvability.");
            for (int i = 0; i < layout.debugSolution.Length; i++)
            {
                var slide = layout.debugSolution[i];
                if (!board.TrySlide(slide.from, slide.to))
                    throw new ArgumentException("debugSolution contains an illegal slide at index " + i + ".");
            }
            if (!board.TryFindPath(out path)) throw new ArgumentException("debugSolution does not uncover a path.");
        }

        private static void ValidateStructure(RollTheBallLayout layout)
        {
            if (layout == null) throw new ArgumentNullException("layout");
            if (layout.columns < 2 || layout.rows < 2 || layout.columns > 16 || layout.rows > 16)
                throw new ArgumentException("Grid dimensions must each be between 2 and 16.");
            int count = layout.columns * layout.rows;
            if (layout.cells == null || layout.cells.Length != count)
                throw new ArgumentException("Provide exactly one GridCell for every coordinate.");
            var seen = new bool[count];
            var byIndex = new GridCell[count];
            foreach (var cell in layout.cells)
            {
                if (cell.col < 0 || cell.row < 0 || cell.col >= layout.columns || cell.row >= layout.rows)
                    throw new ArgumentException("GridCell is outside the board.");
                int i = cell.row * layout.columns + cell.col;
                if (seen[i]) throw new ArgumentException("Duplicate GridCell coordinate.");
                seen[i] = true;
                byIndex[i] = cell;
                if (!Enum.IsDefined(typeof(TileType), cell.tileType) || !Enum.IsDefined(typeof(SlideAxis), cell.slideAxis))
                    throw new ArgumentException("Invalid tile type or slide axis.");
                if (cell.tileType == TileType.Locked && cell.startsEmpty)
                    throw new ArgumentException("Locked cells must contain their fixed carved tile.");
                if ((cell.col == 0 && cell.grooveLeft) || (cell.row == 0 && cell.grooveDown) ||
                    (cell.col == layout.columns - 1 && cell.grooveRight) ||
                    (cell.row == layout.rows - 1 && cell.grooveUp))
                    throw new ArgumentException("Groove ports may not point outside the board.");
            }
            if (layout.startCell == layout.goalCell) throw new ArgumentException("Start and goal must differ.");
            foreach (var endpoint in new[] { layout.startCell, layout.goalCell })
            {
                if (endpoint.x < 0 || endpoint.y < 0 || endpoint.x >= layout.columns || endpoint.y >= layout.rows)
                    throw new ArgumentException("Start or goal is outside the board.");
                var cell = byIndex[endpoint.y * layout.columns + endpoint.x];
                if (cell.tileType != TileType.Locked)
                    throw new ArgumentException("Start and goal must be locked.");
                if (!(cell.grooveUp || cell.grooveDown || cell.grooveLeft || cell.grooveRight))
                    throw new ArgumentException("Start and goal need a groove port.");
            }
        }
    }
}
