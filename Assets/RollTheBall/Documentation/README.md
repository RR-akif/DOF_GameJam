# Roll the Ball — Unity prefab

A self-contained popup puzzle with a fixed groove underneath sliding wooden blockers. All tile, groove, ball, screw, arrow, button, and panel art is generated as UI meshes at runtime. No imported images, sprites, textures, custom shaders, scene camera, or scene references are required. Text uses Unity's built-in font.

## Import and play

1. Import `RollTheBall.unitypackage` using **Assets → Import Package → Custom Package**. Alternatively, copy the supplied `Assets/RollTheBall` folder into your project's `Assets` folder. Choose one import method.
2. Use Unity **2021.3 or newer**, with **Unity UI / uGUI** (`com.unity.ugui`) installed. Both the Built-in pipeline and SRP use the same screen-space overlay UI. These are target versions, not an executed compatibility certification.
3. Drag `Assets/RollTheBall/Prefabs/RollTheBallPuzzle.prefab` into any scene. You can also use **GameObject → Puzzles → Roll the Ball**. Keep its parent active.
4. Enter Play Mode and click **Test: Roll the Ball**. Drag tiles along their arrow direction; release to snap. Click **Debug: auto-solve** to reveal a valid path and watch the ball roll.
5. Turn off **Show Debug Controls** on the prefab for production. The puzzle itself remains closed until `StartPuzzle()` is called. Editor Inspector buttons and context menus are also provided for testing.

The prefab is already supplied; rebuilding it is optional. **Tools → Roll the Ball → Rebuild Prefab** regenerates its serialized controller/layout wiring in Unity if needed.

## Main-game API

The class is `RollTheBall.RollTheBallPuzzleController`.

```csharp
using RollTheBall;
using UnityEngine;

public sealed class LevelPuzzleTrigger : MonoBehaviour
{
    [SerializeField] private RollTheBallPuzzleController puzzle;

    private void Awake() => puzzle.OnPuzzleSolved += HandleSolved;
    private void OnDestroy()
    {
        if (puzzle) puzzle.OnPuzzleSolved -= HandleSolved;
    }

    public void OpenPuzzle() => puzzle.StartPuzzle();
    public void AbortPuzzle() => puzzle.CancelPuzzle();

    private void HandleSolved()
    {
        // Advance your level here. The puzzle closes itself after this event.
    }
}
```

The controller already subscribes a placeholder that calls exactly `Debug.Log("Roll the ball solved")`. You do not need to add another logger. Disable **Log Solved Placeholder** when real logic is connected.

`StartPuzzle()` resets an already-open run too. It clones and validates the layout, captures host input state, creates the popup and its private EventSystem, and accepts tile input. `CancelPuzzle()` is idempotent, stops a drag or roll, clears the board, deactivates/destroys generated popup objects, and restores host input without firing success. Disabling or destroying the prefab also releases input.

After each committed move, reciprocal groove connectivity is checked. When a path is found, tile input locks immediately and the ball follows cell centers at a constant speed using unscaled time. After arrival at the goal, `OnPuzzleSolved` fires once, while the popup is still open; the popup then closes automatically. Subscriber exceptions cannot prevent cleanup or subsequent subscribers from being notified. A subscriber may start a new run without the old completion closing it.

The root controller remains as the reusable API host while closed. Only the optional debug launcher is visible; the popup, grid, drag state, ball, and private modal EventSystem are gone. No generated textures or sprites need disposal. No score, stars, collectibles, or bonus system is present.

## Exclusive input

No scene input references are serialized in the prefab. On opening, it discovers and temporarily suspends active host EventSystems, input modules, raycasters, and `PlayerInput` components; on closing, it restores enabled actions, UI selection, and cursor state. Pending host pointer gestures are cleared. The popup creates independent UI input actions with the newer Input System, or a `StandaloneInputModule` with the legacy Input Manager. It supports **Old**, **New**, and **Both** Active Input Handling settings; these still need verification in the target project. `link.xml` preserves the reflected optional Input System APIs for IL2CPP builds.

**Custom gameplay input needs its existing gate connected.** A UI popup cannot globally intercept arbitrary `Input.GetKey`, `Mouse.current`, `OnMouseDown`, unrelated `InputAction` callbacks, or third-party input code. If your gameplay uses those, check this shared gate at its input entry point:

```csharp
if (RollTheBallInputLock.IsLocked) return;
```

For an existing input manager, subscribe to `RollTheBallInputLock.Changed`, or implement `IRollTheBallInputParticipant.AcquirePuzzleInputLock()` on its component. Return an `IDisposable` token that restores the exact prior state. Active participants are discovered automatically; no prefab-to-scene reference is needed. Gate input owners created/enabled while the popup is open as well. The input manager/source of the earlier maze and Euler puzzles was not supplied, so this adapter is the integration point for their existing convention.

The controller does not change `Time.timeScale`, physics, audio, or scene object activity. Animation continues if the host game is already paused. The close button and Escape/gamepad Cancel remain available during rolling; tile interactions do not.

## Data and layout authoring

Assign a `RollTheBallLayoutAsset` ScriptableObject or a JSON `TextAsset` in the Inspector. A ScriptableObject takes precedence. Runtime replacements can use `SetLayoutAsset(...)` or `SetLayoutJson(...)`; they validate the replacement before accepting it and close the current run. The shipped prefab uses `Layouts/DefaultLayout.json`.

The spec's `TileType`, `GridCell`, and `RollTheBallLayout` fields are retained. Three additions remove ambiguities from the spec:

| Field | Meaning |
| --- | --- |
| `GridCell.startsEmpty` | No initial movable occupant. Empty space is essential for legal sliding. Must be false on locked cells. |
| `GridCell.slideAxis` | Fixed axis of the tile initially on this cell: `0` horizontal, `1` vertical. Travels with the tile's identity. |
| `RollTheBallLayout.debugSolution` | Ordered `{ "from": {"x":…, "y":…}, "to": {…} }` legal slides from the initial configuration. Required solvability proof and debug solution. |

Coordinates are zero-based: column increases right and row increases **up**. Supply exactly one cell per coordinate. Supported dimensions are 2–16 on each axis. `tileType` is `0` for movable/empty floor cells and `1` for locked supports. Start and goal must be distinct locked cells with groove ports.

At runtime, the rendering-independent `RollTheBallBoard` maintains separate fixed floor data and an occupancy array. Occupancy is `-1` for empty, `-2` for a fixed carved support, and a stable nonnegative tile ID for a solid movable blocker. Locked supports expose their own carved groove; all movable tiles block a groove while occupying it. The start and goal therefore remain passable while staying immovable. A moved tile carries its ID and axis; no groove data moves with it. There is no independently mutable "uncovered" flag.

Each drag captures one pointer, clamps displacement to the tile's fixed axis and the contiguous empty cells before the first obstruction, and snaps to the nearest reachable cell on release. Diagonal moves, jumps over tiles, movement into locked cells, out-of-bounds moves, and simultaneous second-pointer drags are rejected. Focus loss, pause, screen rotation, and cancellation abort a drag without committing it.

The breadth-first connectivity search requires matching ports on both sides of every edge and open occupancy on both cells. It supports branches/cycles and can begin at the current ball cell. Ball motion only begins after a complete path exists.

Debug auto-solve works from any state reached by legal play: it reverses the committed move history legally, then replays the validated solution's legal slides. These moves are applied instantly and rendered together, followed by the normal connectivity check and ball roll. It never sets a solved flag or fires success directly. New layouts must include a valid proof; invalid or already-solved initial layouts produce an explanatory error and release any acquired resources/input.

## Default layout and proof

The 5×5 board has six locked supports, seven movable tiles, and twelve empty cells. Six movable tiles initially cover the eight-cell groove. One extra movable tile occupies `(4,0)`.

The fixed path is `(0,2) → (1,2) → (2,2) → (2,3) → (3,3) → (3,2) → (3,1) → (4,1)`.

| Slide | From | To | Axis |
| --- | --- | --- | --- |
| 1 | (1,2) | (1,4) | Vertical |
| 2 | (2,3) | (2,4) | Vertical |
| 3 | (3,3) | (4,3) | Horizontal |
| 4 | (3,1) | (1,1) | Horizontal |
| 5 | (3,2) | (3,4) | Vertical |
| 6 | (2,2) | (4,2) | Horizontal |

An independent exhaustive check of the supplied JSON found **8,314 reachable occupancy states**, **117,038 directed legal transitions**, and **147 connected configurations**. The minimum solution length is six slides. This enumeration includes legal board states beyond those where normal gameplay would already have started rolling.

## Validation and practical limits

This delivery environment had **no Unity Editor or Unity project**. C# syntax parsing and the independent layout/state-space checks were executed successfully; Unity compilation, prefab import, UI rendering, real device input, and the included Edit/Play Mode tests have **not** been executed here. The prefab YAML's local GUID/file-ID references were checked structurally. See `Validation/VALIDATION.md` for the exact checks and test commands.

Install Unity Test Framework (`com.unity.test-framework`) to run the supplied tests through **Window → General → Test Runner**. The tests cover model legality and connectivity, actual drag handler snapping/pointer ownership, lifecycle cleanup, cancellation while rolling, callback ordering, exceptions, replay, paused-time animation, and multiple prefab instances. Run both EditMode and PlayMode suites before merging into your game.

Useful Unity API references: [PlayerInput](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.PlayerInput.html), [InputSystemUIInputModule](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html), and [runtime UI event systems](https://docs.unity3d.com/2023.2/Documentation/Manual/UIE-Runtime-Event-System.html).
