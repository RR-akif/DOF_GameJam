# Validation record

## Executed in the delivery workspace

- Parsed all C# source files using the tree-sitter C# grammar: no syntax errors. This is syntax validation, not C# compilation against Unity assemblies.
- Loaded the actual `DefaultLayout.json` and checked its coordinates, occupancy, locked endpoints, legal authored slides, and reciprocal groove ports.
- Exhaustively enumerated 8,314 reachable occupancy configurations and 117,038 directed legal transitions; the shortest solution is six slides and 147 configurations have a connected path.
- Checked a covered starting cell, current-ball-cell search, mismatched entry/exit ports, re-covering a previously open groove, wrong-axis moves, diagonal moves, collisions, jumping over a blocker, locked destinations, empty sources, and board edges in the independent verifier.
- Checked `.meta` uniqueness, all prefab external GUIDs resolving inside the package, local object references, JSON/assembly definitions, XML, and archive round trips.

Re-run the independent layout check with Python 3 (no external packages):

```sh
python3 Validation/verify_layout.py
```

`layout-verification.json` is the machine-readable result. This Python verifier independently exercises the data/rules; it does not execute the C# model.

## Supplied Unity tests — not run here

EditMode tests are in `Assets/RollTheBall/Tests/EditMode`. PlayMode tests are in `Assets/RollTheBall/Tests/PlayMode`. Install Unity Test Framework and run both from the Test Runner. The PlayMode suite uses the imported prefab and JSON, creates a host input participant, and exercises the runtime controller and pointer handlers.

For CI, use the installed Unity Editor executable and your real project path:

```sh
Unity -batchmode -nographics -projectPath /absolute/path/to/your/project -runTests -testPlatform EditMode -testResults /absolute/path/edit-results.xml -logFile /absolute/path/edit.log
Unity -batchmode -nographics -projectPath /absolute/path/to/your/project -runTests -testPlatform PlayMode -testResults /absolute/path/play-results.xml -logFile /absolute/path/play.log
```

Do not add `-quit` to these test invocations; let the test runner finish and exit. The executable may be named differently on your OS.

## Verify in the target Unity project

1. Import the package and confirm no compile errors. Run both automated suites.
2. In an empty scene, drop the prefab in, enter Play Mode, use the test-open button, drag tiles, and auto-solve. Confirm the exact placeholder log fires once after ball arrival, followed by automatic close.
3. In the real game scene, confirm player movement, camera input, UI, and custom input callbacks all respect the modal input gate. Confirm previous action states, selection, and cursor mode restore after both cancel and success.
4. Exercise Old/New/Both input settings used by your project, mouse and touch, release outside the board, interruption by focus loss, and a second touch while dragging.
5. Check portrait, landscape, screen resizing, and device safe areas. Inspect channel joins, the locked screws, ball travel, and start/goal colors.
6. Build with your target render pipeline/backend (including IL2CPP if used), and confirm the debug launcher, optional Input System module, cancellation, and asset stripping behavior.

No Unity import/compile, Unity Test Runner results, screenshot QA, or device/build compatibility is claimed by the workspace checks.
