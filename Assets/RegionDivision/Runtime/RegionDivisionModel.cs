using System;
using System.Collections.Generic;

namespace RegionDivision
{
    [Serializable]
    public struct ClueCell
    {
        public int col;
        public int row;
        public int targetCellCount;
        public ClueCell(int col, int row, int targetCellCount)
        { this.col = col; this.row = row; this.targetCellCount = targetCellCount; }
    }

    [Serializable]
    public sealed class RegionDivisionLayout
    {
        public int columns = 7;
        public int rows = 6;
        public ClueCell[] clues = Array.Empty<ClueCell>();

        public RegionDivisionLayout Copy()
        {
            return new RegionDivisionLayout { columns = columns, rows = rows,
                clues = clues == null ? null : (ClueCell[])clues.Clone() };
        }
    }

    [Serializable]
    public struct AuthoredRegion
    {
        public int clueIndex;
        public int oppositeCol;
        public int oppositeRow;
        public AuthoredRegion(int clueIndex, int col, int row)
        { this.clueIndex = clueIndex; oppositeCol = col; oppositeRow = row; }
    }

    public struct RegionRectangle
    {
        public readonly int MinCol, MinRow, MaxCol, MaxRow;
        public int Area { get { return (MaxCol - MinCol + 1) * (MaxRow - MinRow + 1); } }
        public RegionRectangle(int colA, int rowA, int colB, int rowB)
        {
            MinCol = Math.Min(colA, colB); MinRow = Math.Min(rowA, rowB);
            MaxCol = Math.Max(colA, colB); MaxRow = Math.Max(rowA, rowB);
        }
        public bool Contains(int col, int row)
        { return col >= MinCol && col <= MaxCol && row >= MinRow && row <= MaxRow; }
    }

    public static class RegionDivisionLayouts
    {
        // Coordinates are zero based, from the top left. This is a hand-authored
        // partition witness, not a search/solver. See AUTHORED_LAYOUT.md.
        public static RegionDivisionLayout CreateDefault()
        {
            return new RegionDivisionLayout { columns = 7, rows = 6, clues = new[] {
                new ClueCell(0, 0, 6), new ClueCell(4, 1, 4),
                new ClueCell(6, 0, 6), new ClueCell(2, 3, 6),
                new ClueCell(3, 2, 4), new ClueCell(5, 5, 6),
                new ClueCell(0, 5, 4), new ClueCell(4, 4, 6)
            }};
        }

        public static AuthoredRegion[] CreateDefaultSolution()
        {
            return new[] {
                new AuthoredRegion(0, 2, 1), new AuthoredRegion(1, 3, 0),
                new AuthoredRegion(2, 5, 2), new AuthoredRegion(3, 0, 2),
                new AuthoredRegion(4, 4, 3), new AuthoredRegion(5, 6, 3),
                new AuthoredRegion(6, 1, 4), new AuthoredRegion(7, 2, 5)
            };
        }

        public static bool IsValidRegionLayout(RegionDivisionLayout layout, out string error)
        {
            error = null;
            if (layout == null) { error = "Layout is missing."; return false; }
            if (layout.columns < 1 || layout.rows < 1 || layout.columns > 64 || layout.rows > 64)
            { error = "Grid dimensions must be between 1 and 64."; return false; }
            if (layout.clues == null || layout.clues.Length == 0)
            { error = "At least one clue is required."; return false; }
            var occupied = new HashSet<int>();
            long sum = 0;
            int total = layout.columns * layout.rows;
            for (int i = 0; i < layout.clues.Length; i++)
            {
                ClueCell clue = layout.clues[i];
                if (clue.col < 0 || clue.col >= layout.columns || clue.row < 0 || clue.row >= layout.rows)
                { error = "Clue " + i + " is outside the grid."; return false; }
                if (clue.targetCellCount <= 0 || clue.targetCellCount > total)
                { error = "Clue " + i + " must have a positive value no greater than the grid area."; return false; }
                if (!occupied.Add(clue.row * layout.columns + clue.col))
                { error = "Two clues occupy the same cell."; return false; }
                sum += clue.targetCellCount;
            }
            if (sum != total)
            { error = "Clue sum is " + sum + "; grid area is " + total + ". They must match."; return false; }
            return true; // Necessary checks only; this does not assert solvability or uniqueness.
        }

        public static bool ValidateAuthoredSolution(RegionDivisionLayout layout,
            AuthoredRegion[] solution, out string error)
        {
            if (!IsValidRegionLayout(layout, out error)) return false;
            if (solution == null || solution.Length != layout.clues.Length)
            { error = "The debug solution needs one rectangle per clue."; return false; }
            var board = new RegionDivisionBoard(layout);
            foreach (AuthoredRegion region in solution)
            {
                if (region.clueIndex < 0 || region.clueIndex >= layout.clues.Length)
                { error = "The debug solution references an unknown clue."; return false; }
                ClueCell clue = layout.clues[region.clueIndex];
                if (!board.TryClaim(clue.col, clue.row, region.oppositeCol, region.oppositeRow, out error))
                { error = "Debug solution clue " + region.clueIndex + ": " + error; return false; }
            }
            if (!board.IsSolved()) { error = "Debug solution does not cover the full grid."; return false; }
            error = null;
            return true;
        }
    }

    /// <summary>Pure C# rules/state. No UI, Unity lifecycle, input, or solver.</summary>
    public sealed class RegionDivisionBoard
    {
        private readonly RegionDivisionLayout layout;
        private readonly int[] owners;
        private readonly int[] clueAt;
        private readonly Dictionary<int, RegionRectangle> regions = new Dictionary<int, RegionRectangle>();
        public int Columns { get { return layout.columns; } }
        public int Rows { get { return layout.rows; } }
        public int ClueCount { get { return layout.clues.Length; } }
        public int ClaimedCellCount { get; private set; }
        public int RegionCount { get { return regions.Count; } }

        public RegionDivisionBoard(RegionDivisionLayout source)
        {
            string error;
            if (!RegionDivisionLayouts.IsValidRegionLayout(source, out error))
                throw new ArgumentException(error, nameof(source));
            layout = source.Copy();
            owners = new int[Columns * Rows];
            clueAt = new int[owners.Length];
            for (int i = 0; i < owners.Length; i++) { owners[i] = -1; clueAt[i] = -1; }
            for (int i = 0; i < ClueCount; i++) clueAt[Index(layout.clues[i].col, layout.clues[i].row)] = i;
        }

        private int Index(int col, int row) { return row * Columns + col; }
        public bool InBounds(int col, int row) { return col >= 0 && col < Columns && row >= 0 && row < Rows; }
        public int OwnerAt(int col, int row) { return InBounds(col, row) ? owners[Index(col, row)] : -1; }
        public int ClueAt(int col, int row) { return InBounds(col, row) ? clueAt[Index(col, row)] : -1; }
        public ClueCell GetClue(int index) { return layout.clues[index]; }
        public bool CanStart(int col, int row) { return ClueAt(col, row) >= 0 && OwnerAt(col, row) < 0; }

        public bool CanClaim(int startCol, int startRow, int endCol, int endRow, out string error)
        {
            error = null;
            if (!InBounds(startCol, startRow) || !InBounds(endCol, endRow))
            { error = "Release inside the grid."; return false; }
            int clueIndex = ClueAt(startCol, startRow);
            if (clueIndex < 0) { error = "Start on a number."; return false; }
            if (OwnerAt(startCol, startRow) >= 0)
            { error = "That clue already has a region. Tap it to undo."; return false; }
            var rect = new RegionRectangle(startCol, startRow, endCol, endRow);
            if (rect.Area != layout.clues[clueIndex].targetCellCount)
            { error = "Needs " + layout.clues[clueIndex].targetCellCount + " cells; selected " + rect.Area + "."; return false; }
            int cluesInside = 0;
            for (int row = rect.MinRow; row <= rect.MaxRow; row++)
                for (int col = rect.MinCol; col <= rect.MaxCol; col++)
                {
                    if (OwnerAt(col, row) >= 0) { error = "Regions cannot overlap."; return false; }
                    if (ClueAt(col, row) >= 0) cluesInside++;
                }
            if (cluesInside != 1) { error = "A region must contain exactly one number."; return false; }
            return true;
        }

        public bool TryClaim(int startCol, int startRow, int endCol, int endRow, out string error)
        {
            if (!CanClaim(startCol, startRow, endCol, endRow, out error)) return false;
            int clueIndex = ClueAt(startCol, startRow);
            var rect = new RegionRectangle(startCol, startRow, endCol, endRow);
            regions.Add(clueIndex, rect);
            for (int row = rect.MinRow; row <= rect.MaxRow; row++)
                for (int col = rect.MinCol; col <= rect.MaxCol; col++) owners[Index(col, row)] = clueIndex;
            ClaimedCellCount += rect.Area;
            return true;
        }

        public bool TryUnclaim(int col, int row)
        {
            int owner = OwnerAt(col, row);
            RegionRectangle rect;
            if (owner < 0 || !regions.TryGetValue(owner, out rect)) return false;
            for (int r = rect.MinRow; r <= rect.MaxRow; r++)
                for (int c = rect.MinCol; c <= rect.MaxCol; c++) owners[Index(c, r)] = -1;
            regions.Remove(owner);
            ClaimedCellCount -= rect.Area;
            return true;
        }

        public bool IsSolved()
        {
            if (ClaimedCellCount != owners.Length || regions.Count != ClueCount) return false;
            // Rebuild coverage independently; full occupancy alone is not a win.
            var visited = new bool[owners.Length];
            foreach (KeyValuePair<int, RegionRectangle> entry in regions)
            {
                int owner = entry.Key;
                RegionRectangle rect = entry.Value;
                ClueCell clue = layout.clues[owner];
                if (rect.Area != clue.targetCellCount || !rect.Contains(clue.col, clue.row)
                    || !InBounds(rect.MinCol, rect.MinRow) || !InBounds(rect.MaxCol, rect.MaxRow)) return false;
                if ((clue.col != rect.MinCol && clue.col != rect.MaxCol)
                    || (clue.row != rect.MinRow && clue.row != rect.MaxRow)) return false;
                int clueCount = 0;
                for (int row = rect.MinRow; row <= rect.MaxRow; row++)
                    for (int col = rect.MinCol; col <= rect.MaxCol; col++)
                    {
                        int cell = Index(col, row);
                        if (visited[cell] || owners[cell] != owner) return false;
                        visited[cell] = true;
                        if (clueAt[cell] >= 0) clueCount++;
                    }
                if (clueCount != 1) return false;
            }
            for (int i = 0; i < visited.Length; i++) if (!visited[i]) return false;
            return true;
        }
    }
}
