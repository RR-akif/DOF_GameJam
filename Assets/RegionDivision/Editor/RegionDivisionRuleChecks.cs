using System;

namespace RegionDivision
{
    // Pure C# checks: callable in the Unity editor or a standalone C# test host.
    public static class RegionDivisionRuleChecks
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Region Division check failed: " + message); }

        public static void RunAll()
        {
            string error;
            var layout = RegionDivisionLayouts.CreateDefault();
            var solution = RegionDivisionLayouts.CreateDefaultSolution();
            Check(RegionDivisionLayouts.IsValidRegionLayout(layout, out error), "default layout validity");
            Check(RegionDivisionLayouts.ValidateAuthoredSolution(layout, solution, out error), "authored solution witness");
            Check(!RegionDivisionLayouts.IsValidRegionLayout(null, out error), "null layout");
            var invalid = layout.Copy(); invalid.columns = 0;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "zero dimension");
            invalid = layout.Copy(); invalid.rows = int.MaxValue;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "overflow dimension");
            invalid = layout.Copy(); invalid.clues = null;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "missing clues");
            invalid = layout.Copy(); invalid.clues[0].targetCellCount = 5;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "wrong sum");
            invalid = layout.Copy(); invalid.clues[0].targetCellCount = 0;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "nonpositive clue");
            invalid = layout.Copy(); invalid.clues[0].col = -1;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "off-grid clue");
            invalid = layout.Copy(); invalid.clues[1].col = 0; invalid.clues[1].row = 0;
            Check(!RegionDivisionLayouts.IsValidRegionLayout(invalid, out error), "duplicate clue cell");
            var staleSolution = (AuthoredRegion[])solution.Clone(); staleSolution[1] = staleSolution[0];
            Check(!RegionDivisionLayouts.ValidateAuthoredSolution(layout, staleSolution, out error), "stale witness rejected");

            var board = new RegionDivisionBoard(layout);
            Check(!board.IsSolved() && board.ClaimedCellCount == 0, "empty is not solved");
            Check(!board.TryClaim(1, 0, 2, 2, out error), "start must be a clue");
            Check(!board.TryClaim(-1, 0, 2, 1, out error), "off-grid start");
            Check(!board.TryClaim(0, 0, 7, 1, out error), "off-grid end");
            Check(!board.TryClaim(0, 0, 0, 1, out error), "wrong area");
            Check(board.RegionCount == 0 && board.ClaimedCellCount == 0, "invalid drags are atomic");
            // This exact-area rectangle contains clues A and G.
            Check(!board.TryClaim(0, 0, 0, 5, out error), "second clue rejected");
            Check(board.TryClaim(0, 0, 2, 1, out error), "valid rectangle");
            Check(!board.CanStart(0, 0), "claimed clue cannot start");
            Check(!board.TryClaim(0, 0, 2, 1, out error), "repeat claim rejected");
            // D's vertical six cells intersect A but contain no second clue.
            Check(!board.TryClaim(2, 3, 1, 1, out error), "overlap rejected");
            Check(board.ClaimedCellCount == 6, "rejections preserve previous claims");
            Check(!board.TryUnclaim(6, 5), "empty unclaim harmless");
            Check(board.TryUnclaim(1, 1), "non-clue filled cell can undo");
            Check(board.CanStart(0, 0) && board.RegionCount == 0 && board.ClaimedCellCount == 0, "undo clears full region");
            for (int row = 0; row < board.Rows; row++)
                for (int col = 0; col < board.Columns; col++) Check(board.OwnerAt(col, row) == -1, "no orphan owners");

            // Two additional fixed orders ensure the authored regions are independent.
            CheckOrder(layout, solution, new[] { 7, 6, 5, 4, 3, 2, 1, 0 });
            CheckOrder(layout, solution, new[] { 0, 7, 1, 6, 2, 5, 3, 4 });

            foreach (AuthoredRegion region in solution)
            {
                ClueCell clue = layout.clues[region.clueIndex];
                Check(board.TryClaim(clue.col, clue.row, region.oppositeCol, region.oppositeRow, out error), "all drag directions");
            }
            Check(board.IsSolved() && board.ClaimedCellCount == 42, "full-grid win");
            Check(board.TryUnclaim(4, 5) && !board.IsSolved(), "undo breaks completion");
            Check(board.TryClaim(4, 4, 2, 5, out error) && board.IsSolved(), "reclaim restores completion");

            // A local valid mistake can be made and then repaired.
            var mistake = new RegionDivisionBoard(layout);
            Check(mistake.TryClaim(0, 0, 1, 2, out error), "alternative locally valid region");
            Check(mistake.TryUnclaim(1, 2), "mistake removable");
            Check(mistake.TryClaim(0, 0, 2, 1, out error), "corrected region accepted");

            var single = new RegionDivisionLayout { columns = 1, rows = 1, clues = new[] { new ClueCell(0, 0, 1) } };
            var oneCell = new RegionDivisionBoard(single);
            Check(oneCell.TryClaim(0, 0, 0, 0, out error) && oneCell.IsSolved(), "one-cell tap/claim");
            layout.clues[0].targetCellCount = 999;
            Check(board.GetClue(0).targetCellCount == 6, "board owns a defensive layout copy");
        }

        private static void CheckOrder(RegionDivisionLayout layout, AuthoredRegion[] solution, int[] order)
        {
            var board = new RegionDivisionBoard(layout);
            for (int i = 0; i < order.Length; i++)
            {
                AuthoredRegion region = solution[order[i]];
                ClueCell clue = layout.clues[region.clueIndex];
                string error;
                Check(board.TryClaim(clue.col, clue.row, region.oppositeCol, region.oppositeRow, out error), "ordering claim");
                Check(board.IsSolved() == (i == order.Length - 1), "win only on final rectangle");
            }
        }
    }
}
