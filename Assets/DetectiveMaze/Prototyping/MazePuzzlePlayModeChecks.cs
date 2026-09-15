using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Optional real-Unity smoke checks. Lives only in the generated test scene.</summary>
public sealed class MazePuzzlePlayModeChecks : MonoBehaviour
{
    private bool running;

    [ContextMenu("Run Play Mode Checks")]
    public void RunChecks()
    {
        if (running) return;
        if (!Application.isPlaying || !gameObject.scene.name.StartsWith("DetectiveMazeTest", StringComparison.Ordinal))
        {
            Debug.LogError("Run these checks only in Play mode in the generated DetectiveMazeTest scene.");
            return;
        }
        MazePuzzleController controller = MazePuzzleController.Instance;
        if (controller == null)
        {
            Debug.LogWarning("The scene has no active MazePuzzleController.");
            return;
        }
        if (controller.IsOpen) controller.ClosePuzzle();
        StartCoroutine(CheckPuzzle(controller));
    }

    private IEnumerator CheckPuzzle(MazePuzzleController controller)
    {
        running = true;
        var events = new List<string>();
        Action success = () => events.Add("success");
        Action closed = () => events.Add("closed");
        controller.OnPuzzleSuccess += success;
        controller.OnPuzzleClosed += closed;
        Gamepad pad = null;
        try
        {
            pad = InputSystem.AddDevice<Gamepad>();
            MazeControllerProbe[] probes = FindObjectsOfType<MazeControllerProbe>();
            Require(probes.Length == 2, "The test scene needs its two controller probes.");
            MazeControllerProbe activeProbe = probes[0].enabled ? probes[0] : probes[1];
            MazeControllerProbe disabledProbe = probes[0].enabled ? probes[1] : probes[0];
            Require(activeProbe.enabled && !disabledProbe.enabled, "Probe starting states are incorrect.");
            controller.StartPuzzle();
            Require(controller.IsOpen, "StartPuzzle failed; inspect earlier Console errors.");
            MazeBuilder builder = controller.GetComponentInChildren<MazeBuilder>(true);
            PuzzleInputHandler input = controller.GetComponentInChildren<PuzzleInputHandler>(true);
            Camera camera = controller.transform.Find("PuzzleCamera").GetComponent<Camera>();
            RawImage viewport = controller.GetComponentInChildren<RawImage>(true);
            Require(!activeProbe.enabled && !disabledProbe.enabled, "Both supplied controller components must be disabled.");
            int ticks = activeProbe.TickCount;
            Require(input.IsInputEnabled, "Maze action map must be enabled.");
            Require(camera.enabled && camera.orthographic && camera.targetTexture != null &&
                camera.targetTexture == viewport.texture, "RenderTexture camera and RawImage are not connected.");
            Require(builder.Player.gameObject.layer == LayerMask.NameToLayer("MazePuzzle"), "Runtime detective layer is incorrect.");
            Require(viewport.rectTransform.anchorMin == Vector2.zero && viewport.rectTransform.anchorMax == Vector2.one,
                "RawImage is not stretched.");
            RequireArtwork(builder.Player.transform, "Detective");
            RequireArtwork(builder.StartDoor, "LockedDoor");
            RequireArtwork(builder.EndDoor, "OpenDoor");
            Require(controller.GetComponentsInChildren<Button>(true).Length == 0,
                "The popup must not contain test buttons or a launch menu.");

            // Drive the real new Input System and let actual Unity 2D physics run.
            Vector2 start = builder.SpawnPosition;
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right });
            yield return new WaitForSeconds(0.28f);
            Vector2 moved = builder.Player.Body.position;
            Require(moved.x > start.x + 0.25f && Mathf.Abs(moved.y - start.y) < 0.10f,
                "Analog input did not move continuously along the starting corridor.");
            Require(activeProbe.TickCount == ticks, "Main-game controller Update ran while disabled.");

            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
            builder.Player.ResetTo(start);
            // A wall is half a cell above S. Radius 0.25 means the centre must stop at about +0.25.
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up });
            yield return new WaitForSeconds(0.5f);
            Require(builder.Player.Body.position.y <= start.y + 0.31f &&
                builder.Player.Body.position.y >= start.y + 0.10f, "The detective failed to stop against a 2D wall.");
            float xBeforeSlide = builder.Player.Body.position.x;
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(1f, 1f).normalized });
            yield return new WaitForSeconds(0.3f);
            Require(builder.Player.Body.position.x > xBeforeSlide + 0.2f &&
                builder.Player.Body.position.y <= start.y + 0.31f, "Sliding along a wall failed or the player tunneled through.");

            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
            // Real trigger test (not a direct CompletePuzzle call).
            builder.Player.ResetTo(builder.EndDoor.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            Require(!controller.IsOpen && !input.IsInputEnabled && !camera.enabled, "Exit trigger did not close and disable the maze.");
            Require(!viewport.gameObject.activeInHierarchy && !builder.gameObject.activeInHierarchy,
                "Reaching the second door must hide the entire popup and maze.");
            Require(events.Count == 2 && events[0] == "success" && events[1] == "closed", "Success/closed event order is incorrect.");
            Require(activeProbe.enabled && !disabledProbe.enabled, "Original controller enabled states were not restored.");

            controller.StartPuzzle();
            Require(Vector2.Distance(builder.Player.Body.position, builder.SpawnPosition) < 0.01f, "Reopening did not reset to S.");
            MazeDetectiveMovement samePlayer = builder.Player;
            controller.StartPuzzle();
            Require(builder.Player == samePlayer, "Calling StartPuzzle twice while open must be harmless.");
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
            yield return null;
            yield return null;
            Require(!controller.IsOpen && events.Count == 3 && events[2] == "closed", "Cancel must close without success.");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;

            controller.StartPuzzle();
            controller.CompletePuzzle();
            controller.CompletePuzzle();
            controller.ClosePuzzle();
            Require(events.Count == 5 && events[3] == "success" && events[4] == "closed", "Repeated completion/close emitted duplicate events.");
            controller.StartPuzzle();
            controller.enabled = false;
            Require(!controller.IsOpen && activeProbe.enabled && !disabledProbe.enabled && !input.IsInputEnabled,
                "Disabling MazePuzzleController failed to restore controls.");
            controller.enabled = true;
            Require(events.Count == 6 && events[5] == "closed", "Disable cleanup must close exactly once.");
            Debug.Log("DETECTIVE MAZE CHECKS PASSED: separate detective/door sprite textures, popup-only UI, analog movement, 2D wall blocking/sliding, real exit trigger closes popup, cancel, event order, reopen, duplicate calls, controller restoration, disable cleanup, layers and RenderTexture wiring.");
        }
        finally
        {
            if (controller != null)
            {
                controller.ClosePuzzle();
                controller.enabled = true;
                controller.OnPuzzleSuccess -= success;
                controller.OnPuzzleClosed -= closed;
            }
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            running = false;
        }
    }

    private static void RequireArtwork(Transform target, string artworkName)
    {
        Sprite expected = MazeBuilder.LoadSprite(artworkName);
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        Require(renderer != null && renderer.sprite == expected && renderer.sharedMaterial != null &&
            renderer.sharedMaterial.name == artworkName && renderer.sharedMaterial.mainTexture == expected.texture,
            artworkName + " must use its own sprite and material texture.");
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        Require(properties.GetTexture("_MainTex") == expected.texture,
            artworkName + " renderer texture binding is incorrect.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("DETECTIVE MAZE CHECK FAILED: " + message);
    }
}
