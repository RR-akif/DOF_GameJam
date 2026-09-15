using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RollTheBall.Tests
{
    public sealed class RollTheBallBoardTests
    {
        private RollTheBallLayout layout;
        private RollTheBallBoard board;

        [SetUp]
        public void Setup()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RollTheBall/Layouts/DefaultLayout.json");
            Assert.That(json, Is.Not.Null);
            layout = RollTheBallLayout.FromJson(json.text);
            board = new RollTheBallBoard(layout);
        }

        private void Solve()
        {
            foreach (var slide in layout.debugSolution) Assert.That(board.TrySlide(slide.from, slide.to), Is.True);
        }

        [Test]
        public void DefaultStartsUnsolvedAndHasALegalSolution()
        {
            Assert.DoesNotThrow(() => RollTheBallBoard.ValidatePlayable(layout));
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(out path), Is.False);
            Solve();
            Assert.That(board.TryFindPath(out path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),
                new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(3,2), new Vector2Int(3,1), new Vector2Int(4,1) }));
        }

        [Test]
        public void LockedStartGoalAndOtherLockedTilesCannotMove()
        {
            foreach (var cell in layout.cells)
                if (cell.tileType == TileType.Locked)
                    for (int row = 0; row < layout.rows; row++)
                    for (int col = 0; col < layout.columns; col++)
                        Assert.That(board.TrySlide(new Vector2Int(cell.col, cell.row), new Vector2Int(col, row)), Is.False);
        }

        [TestCase(1, 2, 2, 3)] // diagonal
        [TestCase(1, 2, 0, 2)] // wrong axis, locked destination
        [TestCase(3, 3, 5, 3)] // board edge
        [TestCase(2, 2, 4, 2)] // cannot jump over blocker at 3,2
        [TestCase(3, 1, 4, 1)] // locked goal
        [TestCase(1, 2, 1, 2)] // no-op
        [TestCase(1, 1, 2, 1)] // empty source
        public void RejectsIllegalSlides(int x, int y, int toX, int toY)
        {
            Assert.That(board.TrySlide(new Vector2Int(x, y), new Vector2Int(toX, toY)), Is.False);
            Assert.That(board.History.Count, Is.Zero);
        }

        [Test]
        public void MultiCellSlideKeepsIdentityAxisAndFixedGrooves()
        {
            int id = board.OccupantAt(new Vector2Int(1, 2));
            Assert.That(board.TrySlide(new Vector2Int(1, 2), new Vector2Int(1, 4)), Is.True);
            Assert.That(board.OccupantAt(new Vector2Int(1, 4)), Is.EqualTo(id));
            Assert.That(board.AxisAt(new Vector2Int(1, 4)), Is.EqualTo(SlideAxis.Vertical));
            Assert.That(board.FloorAt(new Vector2Int(1, 2)).grooveRight, Is.True);
            Assert.That(board.FloorAt(new Vector2Int(1, 4)).grooveRight, Is.False);
            Assert.That(layout.cells[11].startsEmpty, Is.False); // asset was not mutated
        }

        [Test]
        public void ReCoveringAnOpenGrooveBreaksConnectivity()
        {
            Solve();
            Assert.That(board.TrySlide(new Vector2Int(1, 4), new Vector2Int(1, 2)), Is.True);
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(out path), Is.False);
        }

        [Test]
        public void MismatchedReciprocalPortsDoNotConnect()
        {
            var cell = layout.cells[11];
            cell.grooveLeft = false;
            layout.cells[11] = cell;
            board = new RollTheBallBoard(layout);
            Solve();
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(out path), Is.False);
            Assert.Throws<ArgumentException>(() => RollTheBallBoard.ValidatePlayable(layout));
        }

        [Test]
        public void LockedCarvedSupportIsPassableButCannotBeOccupied()
        {
            var cell = layout.cells[12];
            cell.tileType = TileType.Locked;
            layout.cells[12] = cell;
            board = new RollTheBallBoard(layout);
            for (int i = 0; i < layout.debugSolution.Length - 1; i++)
                Assert.That(board.TrySlide(layout.debugSolution[i].from, layout.debugSolution[i].to), Is.True);
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(out path), Is.True);
            Assert.That(board.OccupantAt(new Vector2Int(2,2)), Is.EqualTo(-2));
        }

        [Test]
        public void UndoReturnsExactlyToInitialOccupancy()
        {
            var initial = new int[25];
            for (int i = 0; i < 25; i++) initial[i] = board.OccupantAt(new Vector2Int(i % 5, i / 5));
            Solve();
            while (board.UndoLastSlide()) { }
            for (int i = 0; i < 25; i++) Assert.That(board.OccupantAt(new Vector2Int(i % 5, i / 5)), Is.EqualTo(initial[i]));
            Assert.That(board.History.Count, Is.Zero);
        }

        [Test]
        public void PathCanBeginAtCurrentBallCellButNotACoveredCell()
        {
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(new Vector2Int(2,2), out path), Is.False);
            Solve();
            Assert.That(board.TryFindPath(new Vector2Int(3,3), out path), Is.True);
            Assert.That(path[0], Is.EqualTo(new Vector2Int(3,3)));
            Assert.That(path[path.Count - 1], Is.EqualTo(board.Goal));
        }

        [Test]
        public void CycleDoesNotLoopAndBranchFindsGoal()
        {
            // Add an uncovered detour to the default path, making a cycle.
            int[] cycle = { 11, 16, 17, 12, 11 };
            for (int i = 0; i < cycle.Length - 1; i++)
            {
                int a = cycle[i], b = cycle[i + 1];
                var ca = layout.cells[a]; var cb = layout.cells[b];
                if (b == a + 5) { ca.grooveUp = true; cb.grooveDown = true; }
                if (b == a - 5) { ca.grooveDown = true; cb.grooveUp = true; }
                if (b == a + 1) { ca.grooveRight = true; cb.grooveLeft = true; }
                if (b == a - 1) { ca.grooveLeft = true; cb.grooveRight = true; }
                layout.cells[a] = ca; layout.cells[b] = cb;
            }
            board = new RollTheBallBoard(layout);
            Solve();
            List<Vector2Int> path;
            Assert.That(board.TryFindPath(out path), Is.True);
            Assert.That(path.Count, Is.LessThanOrEqualTo(25));
        }

        [Test]
        public void MalformedLayoutsAndFalseSolutionProofsAreRejected()
        {
            layout.cells[1] = layout.cells[0];
            Assert.Throws<ArgumentException>(() => new RollTheBallBoard(layout));
            Setup();
            layout.debugSolution[0] = new TileSlide(new Vector2Int(2,2), new Vector2Int(4,2));
            Assert.Throws<ArgumentException>(() => RollTheBallBoard.ValidatePlayable(layout));
        }
    }
}
