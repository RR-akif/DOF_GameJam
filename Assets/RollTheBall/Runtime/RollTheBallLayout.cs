using System;
using UnityEngine;

namespace RollTheBall
{
    public enum TileType { Movable, Locked }
    public enum SlideAxis { Horizontal, Vertical }

    [Serializable]
    public struct GridCell
    {
        public int col;
        public int row;
        public TileType tileType;
        public bool grooveUp, grooveDown, grooveLeft, grooveRight;

        // These two extra fields describe initial occupancy and the occupant's axis.
        // Empty cells still have fixed floor/groove data. Locked cells cannot be empty.
        public bool startsEmpty;
        public SlideAxis slideAxis;
    }

    [Serializable]
    public struct TileSlide
    {
        public Vector2Int from;
        public Vector2Int to;
        public TileSlide(Vector2Int from, Vector2Int to) { this.from = from; this.to = to; }
    }

    [Serializable]
    public class RollTheBallLayout
    {
        public int columns;
        public int rows;
        public GridCell[] cells;
        public Vector2Int startCell;
        public Vector2Int goalCell;
        // A legal solution from the initial state; also serves as a solvability proof.
        public TileSlide[] debugSolution;

        public static RollTheBallLayout FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Layout JSON is empty.");
            var result = JsonUtility.FromJson<RollTheBallLayout>(json);
            if (result == null) throw new ArgumentException("Could not deserialize layout JSON.");
            return result;
        }
    }
}
