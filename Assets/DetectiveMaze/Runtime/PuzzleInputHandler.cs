using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Owns a private action map. Never uses Input.GetAxis or a main-game action asset.</summary>
[DisallowMultipleComponent]
public sealed class PuzzleInputHandler : MonoBehaviour
{
    public event Action CancelRequested;
    private InputActionMap mazePuzzleActionMap;
    private InputAction move;
    private InputAction cancel;
    public bool IsInputEnabled => mazePuzzleActionMap != null && mazePuzzleActionMap.enabled;

    private void Awake() => EnsureActions();

    private void EnsureActions()
    {
        if (mazePuzzleActionMap != null) return;
        mazePuzzleActionMap = new InputActionMap("MazePuzzle");
        // No expectedControlType / expectedControlLayout named argument:
        // the Vector2 composite and stick bindings supply their own control layout.
        move = mazePuzzleActionMap.AddAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");
        cancel = mazePuzzleActionMap.AddAction("Cancel", InputActionType.Button);
        cancel.AddBinding("<Keyboard>/escape");
        cancel.AddBinding("<Gamepad>/buttonEast");
        cancel.performed += OnCancel;
    }

    public void EnableMazeInput()
    {
        EnsureActions();
        mazePuzzleActionMap.Enable();
    }

    public void DisableMazeInput() => mazePuzzleActionMap?.Disable();

    public Vector2 ReadMove()
    {
        return IsInputEnabled ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
    }

    private void OnCancel(InputAction.CallbackContext context) => CancelRequested?.Invoke();
    private void OnDisable() => DisableMazeInput();

    private void OnDestroy()
    {
        if (mazePuzzleActionMap == null) return;
        cancel.performed -= OnCancel;
        mazePuzzleActionMap.Disable();
        mazePuzzleActionMap.Dispose();
        mazePuzzleActionMap = null;
    }
}
