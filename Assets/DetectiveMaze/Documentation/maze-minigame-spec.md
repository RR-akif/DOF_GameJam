# Unity Minigame Prefab: "Detective Maze" Puzzle

## 1. Concept

A self-contained maze puzzle that can be triggered from anywhere in the main game as a full-screen popup. The player navigates a top-down maze as a detective character, starting at a locked door (the door they just came through) and trying to reach an open door at the exit. When the exit is reached, the puzzle reports success and closes, handing control back to the main game.

**This is a 2D minigame.** All maze geometry, the detective, and door props are 2D sprites with 2D physics/colliders — not 3D meshes. This applies even though the popup is rendered top-down and can look similar to a 3D scene from that angle.

## 2. Visual Theme

- **Setting:** Interior of a building at night — hallway/office/corridor vibe.
- **Lighting:** Dark ambient lighting with a handful of small light sources (wall sconces, a desk lamp, moonlight through a window) rather than even illumination. A soft vignette or fog-of-war around the player is encouraged so only nearby maze walls are visible — this also disguises the fact that it's a simple grid maze.
- **Walls:** Rendered as 2D sprites depicting building interior geometry — corridor walls, cubicle partitions, bookshelves — reused as modular pieces so the same maze layout can be re-skinned later.
- **Floor:** 2D sprite/tile, wood/tile/carpet material look, dark tone.
- **Start:** A closed/locked door sprite at the maze entrance (visually reinforces "you just came through this and it's now locked behind you").
- **End:** A door sprite in an open/ajar state, with light spilling through it, visually distinct so the player can identify the goal from a distance (or once revealed by the fog-of-war).
- **Player character:** A detective — trench coat + fedora silhouette, top-down sprite.
- **Popup backdrop:** A dark, semi-transparent backdrop image (generated art — e.g. a case-file/window-frame look with subtle grain) behind the maze viewport, so the popup reads as a dimmed window rather than a flat box. This asset is generated once and applied automatically by `MazePuzzleController` at `Awake()` — it should require **no manual art assignment** in the Inspector.
- **Art generation:** All visual assets — wall/floor sprites, door sprites, the detective sprite, the popup backdrop, light/vignette effects — should be actual generated art (images/textures/sprites produced by the agent) that gets used in the project, not just primitive shapes standing in for art. The agent should generate the needed art assets itself and apply them as sprites/materials rather than sourcing external/imported art files.

## 3. Maze Structure

- Grid-based maze layout (walls positioned on a logical grid), but the **detective moves continuously** (WASD/analog) within the corridors rather than snapping cell-to-cell — walls block movement via normal **2D collision** (`BoxCollider2D` on walls, `Rigidbody2D` + `CircleCollider2D` on the detective), not grid-step logic and not 3D physics.
- Maze generation: **hand-authored fixed layout** for the prototype (simplest to test and matches "press a button to test it now"). Leave a clearly marked hook to swap in **procedural generation** (e.g. recursive backtracker) later without changing the public API.
- Camera: a dedicated **orthographic 2D** top-down camera pointed at `MazeRoot`, rendering to a Render Texture (see Section 4) rather than directly to the screen. The camera auto-frames itself (position + orthographic size) based on the maze grid's dimensions each time the maze is built, rather than being manually positioned as a fixed rig.

## 4. Prefab Structure

Everything lives under a single root prefab, e.g. `MazePuzzlePopup`:

```
MazePuzzlePopup (root) — has MazePuzzleController script
├── Canvas (Screen Space - Overlay, high sort order)
│   └── MazePanel (Image component; backdrop sprite applied automatically at runtime — leave Source Image empty in Inspector)
│       └── RawImage (anchors stretched to fill MazePanel; displays the maze's Render Texture — this IS the popup viewport)
├── PuzzleCamera (orthographic, renders only the `MazePuzzle` layer, targetTexture = the Render Texture, auto-framed over MazeRoot each build)
├── MazeRoot
│   ├── Grid/Walls (SpriteRenderer + BoxCollider2D per wall piece, night-building theme, all on the `MazePuzzle` layer)
│   ├── StartDoor (locked door sprite, spawn point)
│   ├── EndDoor (open door sprite, 2D trigger collider)
│   └── DetectivePlayer (Rigidbody2D + CircleCollider2D, continuous-movement controller; SpriteRenderer for the detective sprite; **must explicitly set its GameObject layer to `MazePuzzle` in code**, since it's instantiated at runtime and won't inherit the layer otherwise — if this is missed, the camera's culling mask silently won't render it and the player will appear to have no visual feedback)
└── PuzzleInputHandler (see Section 6)
```

`PuzzleCamera` renders exclusively to its Render Texture (not to the main screen), and only `MazePanel`'s `RawImage` displays that texture — so the maze never competes with the main game's own camera output. `MazeRoot`, `PuzzleCamera`, and everything under them stay on their own dedicated Layer (e.g. `MazePuzzle`) so the main game's cameras can safely ignore that layer (culling mask) and vice versa.

**Implementation note:** the `RawImage` must have its `RectTransform` anchors stretched to fill `MazePanel` (full-stretch anchor preset), not left at a default small centered size — otherwise the render texture appears as a tiny box instead of filling the popup.

## 5. Public API

A single MonoBehaviour, `MazePuzzleController`, exposes the integration surface. This is the important part for wiring into the main game later:

```csharp
public class MazePuzzleController : MonoBehaviour
{
    public static MazePuzzleController Instance; // simple singleton for easy calling

    public event Action OnPuzzleSuccess;   // main game can subscribe instead of editing this script
    public event Action OnPuzzleClosed;    // fires on success OR manual close

    /// Call this to open the popup and start the maze.
    public void StartPuzzle() { /* activates popup, resets player to StartDoor, locks main-game input */ }

    /// Called internally when the player reaches EndDoor's trigger,
    /// but also safe to call manually (e.g. a debug "skip" button).
    public void CompletePuzzle()
    {
        Debug.Log("success");
        OnPuzzleSuccess?.Invoke();
        ClosePuzzle();
    }

    /// Closes the popup and restores main-game input, without necessarily succeeding
    /// (e.g. a Cancel/Escape button, if you want one later).
    public void ClosePuzzle() { /* deactivates popup, unlocks main-game input, invokes OnPuzzleClosed */ }
}
```

**To start the puzzle from anywhere in the main game:**
```csharp
MazePuzzleController.Instance.StartPuzzle();
```

**To react to success from the main game (instead of editing the prefab):**
```csharp
MazePuzzleController.Instance.OnPuzzleSuccess += () => {
    // e.g. unlock a door in the main scene, grant an item, etc.
};
```

For now, `CompletePuzzle()` just does `Debug.Log("success")` — the `OnPuzzleSuccess` event is there so later hookups don't require touching this script again.

Full main-game wiring steps (dropping the prefab into a real scene, removing the test button, subscribing to `OnPuzzleSuccess`, triggering from a real door object) are covered in the companion **Maze Minigame Integration Spec** — keep that as a separate document so the "build the minigame" and "wire it into the main game" concerns stay independent.

## 6. Input Isolation

Requirement: while the maze popup is open, keyboard input should drive **only** the detective/maze, and the main game underneath should receive none of it. When closed, control returns fully to the main game.

The project has **both** the new Input System and the legacy Input Manager active, so the isolation approach shouldn't assume which one drives any particular main-game script. Two-part approach:

1. **The maze's own controls are self-contained**, built entirely on the new Input System with a dedicated `MazePuzzle` Action Map (`Move`, maybe `Cancel`). This map lives inside the prefab and doesn't touch or care about the main game's input setup at all.
   - `StartPuzzle()`: `mazePuzzleActionMap.Enable();`
   - `ClosePuzzle()` / inside `CompletePuzzle()`: `mazePuzzleActionMap.Disable();`
   - **Implementation note:** when constraining an input binding's control type on an `InputAction`, be aware the parameter name varies by Input System package version — some versions use `expectedControlType`, others `expectedControlLayout`. Check the installed package version if a compile error mentions this parameter.

2. **The main game's controls are disabled directly**, regardless of which input system they're built on, by disabling the component(s) that read input and move the player — e.g. `mainPlayerController.enabled = false;` — rather than trying to globally disable "legacy input" or "an action map" the main game might or might not be using. `MazePuzzleController` holds a reference (assigned in the Inspector, or found via a tag like `Player` at `StartPuzzle()` time) to whatever script(s) currently drive main-game movement/interaction, and toggles `enabled` on them:
   - `StartPuzzle()`: `mainGameControllers.ForEach(c => c.enabled = false);`
   - `ClosePuzzle()`: `mainGameControllers.ForEach(c => c.enabled = true);`

This way the maze doesn't need to know or assume anything about how the main game's input is wired — it only needs a reference to whichever script(s) should stop responding while it's open.

## 7. Trigger / Test Button (prototyping only)

- Add a temporary UI `Button` in a test scene (not part of the shippable prefab) wired in the Inspector to call `MazePuzzleController.Instance.StartPuzzle()` via `OnClick()`.
- This lets you press Play and immediately pop the maze open to iterate on it, before any main-game hookup exists.
- Once the prefab is wired into the real game (see the companion Integration Spec), this button and its handler can be deleted — nothing else in the prefab depends on it.

## 8. End-of-Puzzle Behavior (current stage)

- `EndDoor` has a 2D trigger collider.
- On `DetectivePlayer` entering that trigger (`OnTriggerEnter2D`), call `MazePuzzleController.Instance.CompletePuzzle()`.
- `CompletePuzzle()` logs `"success"` to the console, fires `OnPuzzleSuccess`, and closes the popup.

## 9. Later Integration Notes

- Because everything is a prefab with one entry point (`StartPuzzle()`) and one exit event (`OnPuzzleSuccess`), dropping this prefab into any scene only requires: (1) instantiate/enable the prefab, (2) call `StartPuzzle()` when the main game wants to trigger it (e.g. player interacts with a locked door object), (3) subscribe to `OnPuzzleSuccess` to decide what happens in the main game (unlock something, continue dialogue, etc.).
- Swapping the fixed layout for procedural generation later only touches the maze-building code inside `MazeRoot` — the public API above doesn't need to change.
- See the companion **Maze Minigame Integration Spec** for the full step-by-step on removing the test button and hooking this prefab into the real game.
