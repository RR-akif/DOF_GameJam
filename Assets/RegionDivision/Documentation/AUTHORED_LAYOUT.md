# Authored 7 × 6 layout

Coordinates are zero based: column 0 is leftmost, row 0 is topmost.
The solution was authored by partitioning the board into the rectangles below,
then placing exactly one clue at a corner of each rectangle. No solver is used.

| Region / clue index | Clue cell (col, row) | Clue | Opposite drag corner | Columns covered | Rows covered | Area |
| --- | --- | ---: | --- | --- | --- | ---: |
| A / 0 | (0, 0) | 6 | (2, 1) | 0–2 | 0–1 | 3 × 2 = 6 |
| B / 1 | (4, 1) | 4 | (3, 0) | 3–4 | 0–1 | 2 × 2 = 4 |
| C / 2 | (6, 0) | 6 | (5, 2) | 5–6 | 0–2 | 2 × 3 = 6 |
| D / 3 | (2, 3) | 6 | (0, 2) | 0–2 | 2–3 | 3 × 2 = 6 |
| E / 4 | (3, 2) | 4 | (4, 3) | 3–4 | 2–3 | 2 × 2 = 4 |
| F / 5 | (5, 5) | 6 | (6, 3) | 5–6 | 3–5 | 2 × 3 = 6 |
| G / 6 | (0, 5) | 4 | (1, 4) | 0–1 | 4–5 | 2 × 2 = 4 |
| H / 7 | (4, 4) | 6 | (2, 5) | 2–4 | 4–5 | 3 × 2 = 6 |

Manual coverage check:

| Row \ Col | 0 | 1 | 2 | 3 | 4 | 5 | 6 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | A **6** | A | A | B | B | C | C **6** |
| 1 | A | A | A | B | B **4** | C | C |
| 2 | D | D | D | E **4** | E | C | C |
| 3 | D | D | D **6** | E | E | F | F |
| 4 | G | G | H | H | H **6** | F | F |
| 5 | G **4** | G | H | H | H | F **6** | F |

Every row contains seven cells, all six rows are covered, and no region overlaps.
Every letter occupies one contiguous axis-aligned rectangle and contains precisely
its own clue. The clues sum to **6 + 4 + 6 + 6 + 4 + 6 + 4 + 6 = 42 = 7 × 6**.
All clues occupy corners; therefore every rectangle can be entered by one drag
using the exact fixed-corner interaction. The set demonstrates all four drag directions.

This is an existence witness, not a claim that the solution is unique. The game
accepts any full partition satisfying the rules. A sum check alone does not prove
solvability. For another layout, author and manually verify a corner-reachable
partition and update both the layout and its debug solution in the Inspector.

`ValidateAuthoredSolution` checks this explicit witness in linear time in its
rectangle cells. It never searches for a solution. The witness remains for authoring
validation; gameplay auto-solve is disabled.
