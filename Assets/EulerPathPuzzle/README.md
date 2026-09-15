# Euler Path puzzle prefab

A reusable popup for the Degrees of Freedom Unity game. The package includes one prefab,
the fixed layout asset, all C# source, a runtime test-open button, and editor checks.

## Install and test

1. Open your Unity project. Use **Assets > Import Package > Custom Package...**, select
   `EulerPathPuzzle.unitypackage`, and import every item.
2. In the Project window, open **Assets > EulerPathPuzzle > Prefabs**.
3. Drag **EulerPathPuzzle.prefab** into the Hierarchy. Keep this host GameObject active;
   the controller manages its popup child. Place it at scene level, outside the player
   or any object that you deactivate during a conversation video.
4. Enter Play Mode. Click **TEST EULER PATH [F8]**, or press **F8**. The prefab Inspector
   also has a **TEST: StartPuzzle()** button in Play Mode.
5. Press and hold a node. Drag along lavender edges. A partial yellow preview follows
   your drag; the edge counts only when you reach its destination node.
6. Complete all 17 edges. The Console prints exactly **Euler path solved**, the event
   fires, and the popup closes automatically.

**R / Restart** clears the current attempt while keeping the popup open.
**Escape / Close** cancels and restores gameplay without a success event.
After release, press the highlighted current node to resume. Touch uses the same rules.
Other fingers cannot take over an active trace.

The puzzle does not open automatically on Play. Only the requested debug launcher appears.
Disable **Show Debug Button** on the prefab instance when using your real game trigger.
This also disables F8. **Log Solved To Console** enables the placeholder subscription at
Awake; configure it before entering Play Mode.

Alternative source installation: unzip the source archive and copy its
`Assets/EulerPathPuzzle` folder **and `EulerPathPuzzle.meta`** into your project's Assets
folder. Use either this method or the unitypackage import, not both.

## Updating the previous package

Exit Play Mode and import this updated package over the previous version, selecting all
changed items. Existing prefab, script, and layout GUIDs are preserved. The historical
`Layouts/OverlappingTriangles.asset` and `.json` filenames are intentionally retained
for replacement compatibility; their contents are now the asymmetric polygon graph.
There is no separate old star layout in this package. If your scene uses the supplied
layout asset, reopening the puzzle loads the new map without reconnecting events.

## Requirements and intended compatibility

- Targeted at Unity **2022.3 LTS and Unity 6**, including Universal 3D/URP projects.
- Uses **Unity UI / uGUI** (`com.unity.ugui`), normally present in Unity projects.
- Supports Active Input Handling set to **Input Manager (Old)**, **Input System Package
  (New)**, or **Both**. With Both, the new backend is used once, avoiding duplicate input.
- The Input System package is optional; all references to it are behind
  `ENABLE_INPUT_SYSTEM`. No input action asset, EventSystem, camera reference, TMP setup,
  imported sprites, or third-party runtime dependency is required.
- The supplied `.unitypackage` was assembled with Unity asset metadata. Unity itself
  was unavailable in the authoring environment, so compilation/import and Play Mode
  remain to be checked in your Editor. See the validation section below.

## Public integration API

Namespace: `DegreesOfFreedom.EulerPath`.

```csharp
public void StartPuzzle();
public void CancelPuzzle();
public event System.Action OnPuzzleSolved;
```

Additional useful members:

```csharp
public void RestartPuzzle();
public bool IsOpen { get; }
public int TracedEdgeCount { get; }
public static bool IsMainGameInputLocked { get; }
public bool ShowDebugButton { get; set; }
public EulerPathLayoutAsset LayoutAsset { get; set; } // changes accepted only while closed
public static bool IsValidEulerLayout(EulerPathLayout layout, out string error);
```

Call `StartPuzzle()` at the end of your conversation video, from an interactable, or
from another game script. No trigger or level reference is serialized into the prefab.
An example of a main-game bridge is below. It is documentation, not an extra component
attached to the shipped prefab. The existing built-in listener already prints the
required success placeholder.

```csharp
using UnityEngine;
using DegreesOfFreedom.EulerPath;

public class GatePuzzleBridge : MonoBehaviour
{
    [SerializeField] private EulerPathPuzzleController puzzle;

    private void Awake()
    {
        puzzle.OnPuzzleSolved += HandleSolved;
    }

    public void OnConversationFinished()
    {
        puzzle.StartPuzzle();
    }

    private void HandleSolved()
    {
        // Connect your door/level logic here later.
        // The puzzle closes itself after this callback returns.
    }

    private void OnDestroy()
    {
        if (puzzle != null) puzzle.OnPuzzleSolved -= HandleSolved;
    }
}
```

Assign the scene's EulerPathPuzzle instance to that bridge's `puzzle` field. All
scene-specific wiring belongs to your game, leaving the prefab reusable.

Success is checked immediately after every committed edge, not by polling a timer.
All registered listeners are notified before the popup closes; an exception from one
listener is logged and cannot prevent cleanup or the remaining listeners. Repeated
Start calls while already open do nothing. Repeated Cancel calls are safe. Calls to
Start made from a success callback are ignored until closing finishes. To open another
puzzle, schedule it after the current callback/close sequence.

## Input isolation and cleanup

There was no existing maze implementation in the supplied files to reuse, so this
prefab contains its own scene-independent input lock. On open it snapshots the prior
time scale, cursor settings, enabled polling scripts, and enabled Input System actions.
It then:

- Pauses scaled gameplay time and physics by default.
- Temporarily disables other active MonoBehaviours that implement Update, FixedUpdate,
  LateUpdate, OnGUI, or OnMouse callbacks, plus PlayerInput and UI input modules.
  UI graphics/layout components remain visible. Pure event-driven managers stay active.
- Disables Input System actions and rejects new action/map enables while locked.
- Reads mouse/touch/keyboard devices directly for the puzzle. It temporarily uses
  dynamic Input System updates so touch/mouse input continues when gameplay is paused.
- Unlocks and shows the cursor.

Cancel, success, disabling the host, or destroying it restores the previous component,
action, cursor, input-update mode, and time-scale states. Disabled scripts are not
accidentally enabled. References to objects destroyed during play are skipped. The
trace state is discarded; the next open starts fresh. Generated UI children are reused
while inactive, avoiding repeated asset allocations. Losing window focus releases only
the current drag; the next press resumes at the last reached node.

**Integration detail:** automatically suspending polling scripts calls their
OnDisable/OnEnable methods. A manager that has an Update method and unsubscribes from
OnPuzzleSolved in OnDisable should implement `IEulerPuzzleKeepRunning`, or subscribe in
Awake and unsubscribe in OnDestroy as above. Keep-running managers must check
`IsMainGameInputLocked` before handling gameplay input. The same gate must be used by
custom input handling in unscaled coroutines, native input callbacks, or other systems
that bypass ordinary MonoBehaviour updates/InputActions. These cannot be identified
generically from a prefab. If your project has a central input service, use this gate
there. The default covers normal Unity movement/look/interact scripts and PlayerInput.

Do not put gameplay input scripts beneath the EulerPathPuzzle host: its own descendants
are deliberately excluded from the suspension scan. **Pause Game Time** may be disabled
if the world should continue simulating, but ordinary polling scripts are still suspended.

## Authored graph

The layout is fixed; no runtime procedural generation occurs. It combines an irregular
pentagon with a skewed quadrilateral. All four crossings are split into shared junctions.
The silhouette, edge lengths, and interior regions are asymmetric; the earlier star map
has been replaced in both the authored asset and the code fallback.

| Nodes | Degree | Meaning |
| --- | --- | --- |
| 0–8 | 2 each | Nine polygon corners |
| 9–12 | 4 each | Four shared intersection nodes |

There are **13 nodes, 17 edges, and zero odd-degree nodes**. The graph is connected,
so an Euler circuit exists and any node can be used as the start. Four junctions create
branching choices at a medium puzzle scale. A premature return to a tip may leave
untraced edges; the UI then asks the player to restart.

The supplied attachment did not contain a section named "Generation Guidelines".
The implementation follows the extra message's 14–18-edge, overlapping-polygon, medium
difficulty constraints. The optional timer was omitted because no time limit was specified.

For testing, one solution by node ID is:

`0 → 1 → 2 → 9 → 3 → 10 → 8 → 7 → 9 → 6 → 11 → 5 → 12 → 4 → 10 → 12 → 11 → 0`

`Graph_Reference.png` in the source archive labels these nodes for debugging. It is
documentation only and is not included in Assets or used for game rendering.

## Changing the graph

1. Right-click in the Project window and choose **Create > Euler Path > Layout**.
2. Edit its node IDs, UI-space positions, and endpoint ID pairs in the Inspector.
3. Use **Validate Euler rule, connectivity, and crossings** on the layout Inspector.
4. Assign it to **Layout Asset** on your prefab instance, then test.

The graph classes are plain serializable data. Node IDs need not be consecutive or
equal to array indices. The session copies the data and stores trace flags separately;
playing never modifies your ScriptableObject asset. The JSON file alongside the
ScriptableObject is the same graph in portable form; the prefab uses the ScriptableObject.

Validation rejects invalid references, duplicate IDs/undirected edges, zero-length or
self-loop edges, non-finite/overlapping positions, isolated nodes, disconnected graphs,
unsplit crossings, and odd-degree counts other than 0 or 2. For a two-odd-node layout,
only those two endpoints may be used as the initial start. They receive generated rings.
OnValidate logs authoring errors; invalid layout assets also block builds.

Keep edge lengths comfortably greater than twice Node Hit Radius and avoid placing
unrelated edges closer than the tracing tolerance. The shortest supplied edge is approximately
85 units, with an 18-unit node radius and 20-unit edge tolerance. Those settings
are measured in graph coordinates and scale with the rendered graph.

## Interaction decisions

The overview says "without lifting/releasing", but the detailed interaction section
explicitly specifies resume after release. The implementation follows the detailed rule:
completed edges remain traced, partial progress is discarded, and pressing the current
node resumes. There is no strict-release failure mode in this version.

An edge is committed on reaching the far node's hit area, as permitted by the spec.
Movement between samples is swept in small steps, so fast collinear drags can traverse
several edges and off-line jumps cannot silently skip to a node. Attempting a used edge
changes no graph progress. Return to the highlighted node to choose another edge, or
release and resume there. Completed nodes can be visited repeatedly.

## Visual implementation

The prefab creates its own overlay canvas, safe-area layout, labels, and clickable
buttons. `EulerPathGraphic` creates a rounded capsule-and-circle UI mesh for the graph.
Lavender edges turn yellow when committed; partial progress uses a lighter preview.
Current-node and odd-endpoint rings are generated geometry. The interface uses Unity's
built-in runtime font; there are no imported art/font files. No scene camera is referenced.
The test button and popup buttons use direct hit testing, so they work without an
EventSystem while the game's normal UI input is suspended.

## Validation and testing

Completed outside Unity:

- Independent connectivity/degree and geometric-crossing checks.
- 1,300 valid Euler circuits, covering all 13 starts, verified to use all 17 edges once.
- No nontrivial mirror or rotational symmetry in the authored vertex positions.
- Two-odd-node variant checked independently.
- JSON, ScriptableObject, and C# fallback layout consistency.
- Prefab/asset GUID resolution, metadata presence, and absence of imported art.
- C# delimiter and conditional-directive structure checks (not a compilation).

**Not executed here:** Unity compilation/import, C# self-tests, runtime rendering,
device input, or Play Mode lifecycle checks. Unity Editor and a C# compiler are not
installed in the authoring environment. Do not treat the graph checks as a Unity test pass.

The package includes two menu-driven suites, with no NUnit dependency:

1. **Tools > Euler Path > Run graph and tracing checks** executes the actual C# graph
   validator and tracing session, including 240 complete circuits, two-odd layouts,
   release/resume, retrace rejection, off-line jumps, fast drags, dead ends, restart,
   invalid layouts, and prefab references.
2. Enter Play Mode in an empty test scene and select
   **Tools > Euler Path > Run lifecycle checks (Play Mode)**. It tests open/cancel,
   repeated calls, success-before-close, fresh reopen, cursor/time/input restoration,
   initially disabled scripts/actions, and host disable/destroy cleanup. Temporary
   fixtures are removed afterwards. The success placeholder prints once during this suite.

After those pass, manually test the prefab in your real level: try movement and camera
input while the popup is open, cancel and reopen, finish the puzzle, and repeat on a
touch device if targeting mobile. Also test focus loss, your target display resolution,
and your real success subscriber. This is where custom gameplay input patterns are verified.

## Source files

| File | Responsibility |
| --- | --- |
| EulerPathPuzzleController.cs | Public API, lifecycle, event, cleanup |
| EulerPathLayout.cs / EulerPathLayoutAsset.cs | Serializable graph and ScriptableObject |
| EulerPathDefaultLayout.cs | Fixed fallback matching the authored asset |
| EulerPathValidator.cs | Euler rule, topology, geometry validation |
| EulerTraceSession.cs | Device-independent tracing and win checks |
| EulerPuzzleInput.cs | Mouse/touch and keyboard backend selection |
| EulerPuzzleInputLock.cs | Gameplay suspension and restoration |
| EulerPathGraphic.cs / EulerPuzzleView.cs | Generated graph mesh and popup UI |
| Editor/EulerPathEditor.cs | Inspectors, validation/build checks, add-prefab menu |
| Editor/EulerPathSelfTests.cs | Core and lifecycle checks |

For another project, import the complete unitypackage or export the entire
`Assets/EulerPathPuzzle` folder. Retain the `.meta` files so prefab/asset links stay intact.

Unity API reference used for action snapshot/disable behavior:
[InputSystem API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputSystem.html).
