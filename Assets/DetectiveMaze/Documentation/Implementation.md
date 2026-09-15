# Detective Maze implementation — popup-only revision

The user's follow-up replaces the original spec's temporary launch-button requirement: Play should open only the maze popup, and the second/open door should close it. The original spec is kept unchanged for reference.

## Rendering repair

Previously the builder assigned the same custom `MazeSprite` material to all renderers. The revision creates one material for each world-art texture and explicitly binds that texture to its SpriteRenderer. Both the Sprite field and material texture are validated. A missing/mismatched material produces a repair instruction instead of substituting another image.

| Object / role | Sprite source | Material asset under `Resources/DetectiveMaze/Materials` |
|---|---|---|
| DetectivePlayer | `Art/Detective.png` | `Detective.mat` |
| StartDoor | `Art/LockedDoor.png` | `LockedDoor.mat` |
| EndDoor | `Art/OpenDoor.png` | `OpenDoor.mat` |
| Walls | `Art/Wall.png` | `Wall.mat` |
| Floors | `Art/Floor.png` | `Floor.mat` |
| Lamp / moonlight / detective glow | `Art/LightGlow.png` | `LightGlow.mat` |

The setup command uses `Sprites/Default` in Built-in projects and `Universal Render Pipeline/2D/Sprite-Unlit-Default` in URP projects. These are Unity sprite shaders; the old custom shader/material is no longer assigned. The old shader file remains only to avoid breaking leftover assets in an existing installation. The command updates the generated per-art materials in place when rerun. Every renderer also gets an explicit `_MainTex` property block containing its own sprite's texture.

Unity distinguishes the Sprite texture from the shader/material used to render it; a shared material name alone does not prove which image is rendered. This revision makes the actual binding explicit and checks it. See [Unity's Sprite Renderer reference](https://docs.unity3d.com/2022.3/Documentation/Manual/class-SpriteRenderer.html) and [URP 2D renderer reference](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/2DRendererData-overview.html).

All seven final images remain in `Resources/DetectiveMaze/Art`. Doors, detective and glow retain generated alpha transparency. PopupBackdrop is loaded automatically in Awake and tinted to 92% alpha. Artwork was generated using the built-in image generation tool; original prompts and the overhead detective correction are preserved beside this file.

## Exact prefab hierarchy

| Path under `MazePuzzlePopup` | Components / configuration |
|---|---|
| Root | MazePuzzleController; active, unit scale, zero rotation; Open On Start enabled |
| `Canvas` | Screen Space – Overlay, sorting order 30000, CanvasScaler, GraphicRaycaster |
| `Canvas/MazePanel` | Image, full screen; Source Image empty in prefab; generated backdrop loaded in Awake |
| `Canvas/MazePanel/RawImage` | RawImage; anchors (0,0) to (1,1), all offsets zero; PuzzleCamera's Render Texture |
| `PuzzleCamera` | Orthographic, MazePuzzle layer only, target texture assigned, disabled while closed |
| `MazeRoot` | MazeBuilder; MazePuzzle layer; local position (10000,10000,0) |
| `MazeRoot/Grid/Walls` | Wall sprites with BoxCollider2D |
| `MazeRoot/Grid/Floors` | Floor sprites |
| `MazeRoot/Grid/Lighting` | Generated light-pool sprites |
| `MazeRoot/StartDoor` | LockedDoor sprite/material |
| `MazeRoot/EndDoor` | OpenDoor sprite/material, trigger BoxCollider2D, MazeExitDoor |
| `MazeRoot/DetectivePlayer` | Detective sprite/material, dynamic Rigidbody2D, CircleCollider2D, MazeDetectiveMovement |
| `MazeRoot/DetectivePlayer/DetectiveLight` | Generated glow following the detective |
| `PuzzleInputHandler` | Private new Input System MazePuzzle action map: Move and Cancel |

The detective explicitly sets its own layer in code. Camera framing is derived from the maze's width/height and the viewport's aspect ratio each build/resize. Movement is continuous Rigidbody2D velocity, never grid stepping. The radius is 0.25 units, speed 3.5 units/second, gravity is zero, rotation is frozen, and collisions use continuous detection with a zero-friction material.

The fixed maze is 21 by 15 cells. All 141 corridor cells are connected; the shortest entrance-to-exit path has 54 grid edges. `MazeLayout.CreateRows()` is the hand-authoring location and the marked procedural-generation hook. Rows use `#`, `.`, `S`, `E`. Lighting uses generated local pools over dim floor artwork, without fog-of-war or a Light2D dependency.

For manual Inspector reference repair, assign the root controller's fields to the corresponding Canvas, MazePanel Image, RawImage, PuzzleCamera, MazeRoot's MazeBuilder, PuzzleInputHandler, and `Generated/MazeView.renderTexture`. Leave Source Image empty on MazePanel. The Editor setup command performs this wiring and saves the actual prefab automatically.

## Startup and completion

`MazePuzzleController.Start()` opens the puzzle once if Open On Start is checked. The generated scene has no second UI Canvas, launch buttons, title screen, or game-facing diagnostics. Only the popup is visible. Closing leaves the underlying scene visible; in the standalone test scene this is its plain background camera.

`MazeExitDoor` checks both OnTriggerEnter2D and OnTriggerStay2D. Only the detective owned by this puzzle can complete it. A consumed flag and the controller's open/completing guards prevent duplicate success. The controller logs `success`, invokes OnPuzzleSuccess, closes the view, restores control, and invokes OnPuzzleClosed. Manual close/cancel invokes only OnPuzzleClosed. Start is not called again when the popup closes.

The API remains `Instance.StartPuzzle()`, `CompletePuzzle()`, `ClosePuzzle()`, `OnPuzzleSuccess`, and `OnPuzzleClosed`. The prefab contains no main-game rewards, unlocks, dialogue, or other hookup logic. Turn off Open On Start if a later integration should launch through an external trigger.

## Input and environment

The maze action map is enabled only while open. Assign all main-game movement/look/interaction readers to Main Game Controllers; their prior enabled states are restored, including components already disabled. Readers can use either input system. Event-subscribed components must honor their enabled/OnDisable lifecycle; include PlayerInput when it owns gameplay callbacks. Unknown scripts are not rewritten or auto-discovered.

The popup blocks pointer UI raycasts. While open it also saves/restores cursor state and background UI navigation/selection, temporarily excludes the reserved maze layer from other game cameras, and isolates that layer from other 2D physics layers. No global time-scale changes or unknown Rigidbody velocity changes are made.

Target assumptions: Unity 2022.3 or Unity 6, Input System 1.x, uGUI, Built-in or URP, Active Input Handling = Both, positive time scale, Physics 2D Simulation Mode = Fixed Update. Existing Input System and project packages should be retained. Keep the prefab root at unit scale/zero rotation and reserve MazePuzzle for this minigame.

## Upgrade and diagnostics

Copy updated files over the same Assets/DetectiveMaze folder, preserving existing .meta files. Run Tools → Detective Maze → Build Prefab and Test Scene once to create the new materials and open the updated scene. Generated prefab/scene copies are numbered so existing scene edits are preserved. Use the new scene/prefab.

`MazePuzzleTestButton` is retained only as a compatibility shim: in an old generated scene its Awake disables the former `TemporaryTestUI` root. The new assembler does not create that menu or attach the handler.

The new test scene has an invisible MazeDiagnostics component and two invisible controller probes outside the prefab. To run checks, enter Play, select MazeDiagnostics, and choose Run Play Mode Checks from the component's context menu. The checks close/reset the puzzle and inject a temporary gamepad to check actual movement, wall blocking/sliding, door closure, cancellation, enabled-state restoration, event order, and correct texture bindings. They print to the Console and create no game UI.

Keep Runtime, Resources, the generated prefab and Render Texture together. Resources art/materials must be included when exporting even if some runtime string loads are not listed as serialized dependencies. You may remove the test scene plus both Editor and Prototyping after setup; the assembler references diagnostic types. The prefab runtime does not depend on them.

## Verification limits

C# syntax parsing and static resource/material/startup/exit wiring checks were run in the supplied workspace. No Unity project/Editor was supplied, so Unity compilation, shader rendering, prefab serialization, and Play mode remain unverified here. Use the included Editor and Play mode checks to complete that verification in the actual project.
