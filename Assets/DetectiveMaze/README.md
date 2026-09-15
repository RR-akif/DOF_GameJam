# Detective Maze — sprite and popup update

## Apply this update

1. Stop Play mode in Unity.
2. Extract this ZIP and copy **`Assets/DetectiveMaze`** over the same folder in your project. Replace the supplied files. Keep Unity's existing `.meta` files and your `Generated` folder.
3. Wait for compilation. Choose **Tools → Detective Maze → Build Prefab and Test Scene**. This step is required: it creates/repairs the separate sprite materials and opens the updated scene. Save your current scene if prompted.
4. Press **Play**. **Only the popup minigame opens. There is no launch menu.** Move with WASD, arrows, gamepad left stick, or D-pad.
5. Reach the **second/open door**. The popup closes, `success` is logged, and the success/closed events fire. It stays closed. Escape or gamepad East cancels without success.

The generated prefab and scene are in **`Assets/DetectiveMaze/Generated/`**. Existing prefab/scene files are preserved; repeated setup creates numbered copies. Use the newly generated scene/prefab.

## What changed

- `DetectivePlayer` uses `Detective.png` and its own **Detective** material.
- `StartDoor` uses `LockedDoor.png` and its own **LockedDoor** material.
- `EndDoor` uses `OpenDoor.png` and its own **OpenDoor** material.
- All world artwork has explicit sprite and texture bindings. Materials are created under **`Resources/DetectiveMaze/Materials/`** with Unity's standard sprite shaders. The old shared `MazeSprite` material is no longer used.
- The launch menu, headings, instructions screen, and test buttons are no longer generated. A compatibility handler disables the old `TemporaryTestUI` in previously generated scenes.
- **Open On Start** defaults to enabled on `MazePuzzleController`. Startup happens once; completion does not reopen the puzzle.
- The exit's 2D trigger handles entering or already overlapping the open doorway and completes only once.

## Settings and API

Requires Unity 2022.3 or Unity 6, Input System, uGUI, Built-in or URP rendering, **Active Input Handling = Both**, **Time.timeScale > 0**, and **Physics 2D Simulation Mode = Fixed Update**.

The prefab still uses 2D sprites/physics and an orthographic camera rendering exclusively to the popup's Render Texture. Keep its root active with zero rotation and unit scale. Assign your movement/look/interaction input-reader components to **Main Game Controllers** when testing over your main game.

```csharp
MazePuzzleController.Instance.StartPuzzle();
MazePuzzleController.Instance.CompletePuzzle();
MazePuzzleController.Instance.ClosePuzzle();
```

Events remain `OnPuzzleSuccess` and `OnPuzzleClosed`. Disable **Open On Start** only when a later main-game trigger should open the puzzle instead.

## Verification

Source syntax and sprite/material wiring checks pass. Unity rendering and Play mode could not be executed in this workspace.

Optional checks have no game UI: in Play mode, select **MazeDiagnostics**, open the **Maze Puzzle Play Mode Checks** component's three-dot/context menu, and choose **Run Play Mode Checks**. These checks exercise the separate sprite textures, movement, walls, exit-trigger closure, and input restoration. Results appear in the Console.

The full hierarchy and implementation notes are in **`Assets/DetectiveMaze/Documentation/Implementation.md`**. The originally supplied spec is preserved there; this update follows your later instruction to remove its temporary launch menu.
