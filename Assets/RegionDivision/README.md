# Region Division — Unity prefab

Complete source and one reusable prefab for the attached Region Division spec.
The supplied project files were a spec only, so this is an importable feature
package rather than a modification of an existing Unity project.

## Install and open

1. Import `RegionDivision.unitypackage` using **Assets → Import Package → Custom Package**.
   Alternatively, copy the included `Assets/RegionDivision` folder into your project.
   Use one method; both contain the same assets and GUIDs.
2. Drag `Assets/RegionDivision/Prefabs/RegionDivisionPuzzle.prefab` into the scene.
3. Enter Play Mode and press **DEBUG: Open Region Division** in the top-left corner.
4. Drag from a number to claim a rectangle. Tap any filled cell to release its
   entire region. Press **Forfeit** or Escape to cancel without firing success.
5. Complete the grid to win. The Console logs `Region division solved`.

Auto-solve is disabled completely. To update an existing installation, stop Play
Mode and re-import the included package, accepting the updated scripts. Asset GUIDs
are preserved so existing prefab instances keep their references.

Target: Unity **2022.3 LTS or newer**, with the **Unity UI / uGUI** package enabled.
The source also retains the pre-2022.2 built-in-font fallback. No TextMeshPro,
external artwork, texture, sprite, font import, custom shader, scene camera,
scene EventSystem, or test framework is needed. The code supports the legacy
Input Manager and the Input System package (1.6+ target), selecting the new
backend if `ENABLE_INPUT_SYSTEM` is defined, including “Both” configurations.
Keep these scripts in their supplied folders without adding a parent assembly
definition, or add the appropriate Unity UI/Input System references to your own
assembly definition if you deliberately move them into an assembly.

## Main-game API

All types are in namespace `RegionDivision`.

```csharp
using RegionDivision;
using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    [SerializeField] private RegionDivisionPuzzleController puzzle;

    private void OnEnable()  => puzzle.OnPuzzleSolved += HandleSolved;
    private void OnDisable() => puzzle.OnPuzzleSolved -= HandleSolved;

    public void OpenPuzzle() => puzzle.StartPuzzle();
    public void AbortPuzzle() => puzzle.CancelPuzzle();

    private void HandleSolved()
    {
        // Connect level progression here. The prefab already logs success.
        // The solved grid is still available during this callback.
        // It will auto-close and restore input after listeners return.
    }
}
```

`StartPuzzle()` creates a fresh board every time, including when called while
already open. `CancelPuzzle()` is idempotent and never raises success. After the
last accepted claim, the controller revalidates the full grid, raises the event
once, then closes in a `finally` block. A listener exception is logged without
blocking subsequent listeners or cleanup. A reentrant Start call from a solved
listener is ignored; schedule a new opening for a later frame if required.

The prefab root stays available as the public API/debug launcher. Its popup
child and private EventSystem are generated only when opened, disabled immediately
on close, and destroyed at end of frame. Board and gesture state are discarded.
Disable or destroy the root/controller to clean up an open session. Only one
Region Division popup can be open at a time. The game is not paused and timeScale
is never changed. Input and UI therefore work when the host has already paused time.

## Gameplay input integration

The prefab automatically suspends active scene EventSystems and enabled scene
raycasters while open, uses
its own EventSystem, blocks UI raycasts behind its fullscreen overlay, unlocks
the cursor, and restores the captured state on close. With the new Input System,
it also snapshots and disables all currently enabled Input Actions (including
`PlayerInput`, independent maps, and project-wide actions) and restores exactly
those actions. Previously disabled actions and
EventSystems/raycasters remain disabled. The puzzle UI uses its own generated
default actions. The legacy pointer module does not require named Input Manager
axes such as Horizontal/Vertical/Submit/Cancel.

**Your project's existing input-lock API was not supplied.** To use the same
locking approach as your other puzzles, implement
`IRegionDivisionInputLockProvider` on the existing active input-manager component.
The prefab finds it at runtime, with no scene reference serialized into the prefab:

```csharp
public System.IDisposable AcquirePuzzleInputLock()
{
    return existingInputService.AcquireExclusiveLock(); // Use your actual API.
}
```

The provider's token must restore its previous state when disposed and support
other lock owners. For a manager using raw `Input.GetKey`, `Input.GetMouseButton`,
`Mouse.current`, or scripts that can enable new action maps while a modal is open,
a simple integration is:

```csharp
if (RegionDivisionInputLock.IsLocked) return;
// Read and process gameplay controls here.
```

Check this flag in every gameplay input entry point, including callbacks, unless
your provider already disables them. Unity UI cannot consume arbitrary physical
device polling in another script. The action snapshot does not cover direct device
polling or scripts that re-enable actions during the modal. Those require the flag
or provider above; silently disabling unrelated scene scripts would be unsafe.
The flag remains true through the closing frame to suppress release-click leakage.
Apply the guard to input processing, not to unrelated game simulation updates.

## Layout and validation

The serialized default is 7 columns × 6 rows, with eight clues summing to 42.
`Documentation/AUTHORED_LAYOUT.md` gives every clue, opposite drag corner, and
the manually checked partition. Each clue is at a rectangle corner, making the
whole partition reachable by the specified single-drag gesture. No solver exists.

`RegionDivisionPuzzleController.IsValidRegionLayout(layout, out error)` and
`RegionDivisionLayouts.IsValidRegionLayout(layout, out error)` check dimensions,
clue bounds, duplicate locations, positive values, and the exact clue sum.
`OnValidate()` logs invalid authored data in the editor and verifies the stored
debug partition when present. Layout sizes are limited to 1–64 in each dimension.
These structural checks do not assert uniqueness or arbitrary-layout solvability.

All claimed ownership is in a private flat grid indexed by `row * columns + col`.
The board defensively copies the layout. Each preview uses the same rule check
as the final claim; previews do not mutate ownership. A rejected release leaves
all prior regions intact. Release outside the grid rejects rather than clamping
and committing a region. One pointer owns a drag; additional pointers cannot
complete it. Focus loss and application pause cancel the gesture.

The full-grid check rebuilds coverage from the region records and validates
area, bounds, single-clue containment, corner placement, owner consistency,
non-overlap, and full coverage. It runs after successful claims, never on a timer.
Any valid complete partition wins. The stored partition is used only to validate
authored data. Inspector changes take effect on the next run; an open run retains
its own layout snapshot.

## Generated visuals and debug controls

Cells and overlays are sprite-free `UI.Image` meshes; clues and labels use
`UI.Text` with Unity's built-in font. Eight stable palette colors are indexed by
clue. The green/red translucent preview reports cell count or the rejection
reason. Text remains above the preview. A responsive aspect-fit grid scales
within a screen-space overlay Canvas, independent of the 3D camera/render pipeline.

`Show Debug Hooks` controls only the standalone Open launcher. Turn it off before
a production release to hide that launcher. There is no auto-solve button or
Inspector context command. `DebugAutoSolve()` is an unconditional no-op retained
for compatibility with existing scripts or UnityEvents; no flag re-enables it.
The popup has one centered **Forfeit** button, wired to `CancelPuzzle()`.
The standalone launcher uses IMGUI so the closed puzzle does not need an active
Canvas or alter the scene's EventSystem. Production gameplay opens only through
`StartPuzzle()`. The Inspector context menu offers Start and Cancel.
Turn off `Log Success Placeholder` when replacing the placeholder with level logic.

## Verification

Executed in the authoring environment:

- Compiled the exact pure C# model and rule-check source using Roslyn, C# 7.3,
  with warnings treated as errors, and ran them on .NET 8: **passed**.
- Confirmed the hand-authored 42-cell witness, clue corner reachability, and
  agreement between the prefab's serialized data and the code defaults.
- Checked C# syntax, prefab script GUID resolution, and archive integrity.

Unity Editor is not installed in the authoring environment. The complete Unity
assemblies, prefab import, rendering, actual mouse/touch routing, and Play Mode
lifecycle checks **have not been run in Unity**. Run the included checks there:

- **Tools → Region Division → Run Rule Checks**: pure C# validation, invalid
  inputs, overlap, extra clues, atomic rejection, undo, solved/unsolved transitions,
  defensive copies, and three orders of the authored partition.
- **Tools → Region Division → Run Lifecycle Checks (Play Mode)**: opening,
  input/cursor restoration, pointer preview and release, second-pointer isolation,
  non-clue starts, outside releases, undo taps versus drags, focus-loss cleanup,
  repeated starts/cancels, completion event ordering, auto-close, and root/component
  deactivation. Close any puzzle first. This temporarily creates its own prefab-equivalent
  controller and logs the result; it does not modify scene assets.

Batch rule checks can use:

```text
Unity -batchmode -nographics -projectPath YOUR_PROJECT -executeMethod RegionDivision.Editor.RegionDivisionVerification.RunModelChecks -quit -logFile rules.log
```

Manual acceptance pass: import in a clean scene; verify with no EventSystem,
then with an existing one; try legacy/new/Both input configurations; drag in all
directions; release outside; cancel mid-drag; undo a valid but inconvenient claim;
complete by hand; forfeit after partial claims; reopen after each closure;
test a throwing success listener; verify your game input manager's lock integration;
check a development/device build and both landscape/portrait resolutions. Mouse
and touch are supported; no gamepad rectangle-drawing interaction is claimed.

Reference for the generated new-input UI module:
[Unity InputSystemUIInputModule API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).
