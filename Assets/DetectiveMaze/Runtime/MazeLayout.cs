using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authoring uses rows from top to bottom. # = wall, . = floor, S/E = doors.</summary>
public static class MazeLayout
{
    // PROCEDURAL GENERATION HOOK: replace only this method with a generator
    // returning the same row format. Keep Validate and the controller API unchanged.
    public static string[] CreateRows()
    {
        return new[]
        {
            "#####################",
            "#S....#.......#.....#",
            "#####.#.#####.#.###.#",
            "#.....#.#...#...#...#",
            "#.#####.#.#.#####.#.#",
            "#.......#.#.......#.#",
            "#.#######.#########.#",
            "#.#.....#.....#.....#",
            "#.#.###.#####.#.###.#",
            "#...#...#.....#.#...#",
            "#####.###.#####.#.###",
            "#.....#...#.....#...#",
            "#.#####.###.#######.#",
            "#...........#......E#",
            "#####################"
        };
    }

    public static void Validate(string[] rows)
    {
        if (rows == null || rows.Length < 3 || rows[0] == null || rows[0].Length < 3)
            throw new ArgumentException("Maze needs at least a 3 x 3 grid.");
        int width = rows[0].Length, starts = 0, exits = 0, openCells = 0;
        Vector2Int start = default;
        for (int y = 0; y < rows.Length; y++)
        {
            if (rows[y] == null || rows[y].Length != width)
                throw new ArgumentException("Every maze row must have the same width.");
            for (int x = 0; x < width; x++)
            {
                char cell = rows[y][x];
                if (cell != '#' && cell != '.' && cell != 'S' && cell != 'E')
                    throw new ArgumentException($"Unknown maze symbol '{cell}' at {x}, {y}.");
                if ((x == 0 || y == 0 || x == width - 1 || y == rows.Length - 1) && cell != '#')
                    throw new ArgumentException("The maze must have a solid wall perimeter.");
                if (cell != '#') openCells++;
                if (cell == 'S') { starts++; start = new Vector2Int(x, y); }
                if (cell == 'E') exits++;
            }
        }
        if (starts != 1 || exits != 1)
            throw new ArgumentException("The maze needs exactly one S and one E.");
        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= rows.Length) continue;
                if (rows[next.y][next.x] != '#' && visited.Add(next)) queue.Enqueue(next);
            }
        }
        if (visited.Count != openCells)
            throw new ArgumentException("Every corridor, including the exit, must be reachable from S.");
    }
}
