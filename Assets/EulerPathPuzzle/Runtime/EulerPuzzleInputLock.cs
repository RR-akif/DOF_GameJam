using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DegreesOfFreedom.EulerPath
{
    // Implement on managers that must continue updating while a puzzle is open.
    // Such components must honour EulerPathPuzzleController.IsMainGameInputLocked themselves.
    public interface IEulerPuzzleKeepRunning { }

    internal sealed class EulerPuzzleInputLock : IDisposable
    {
        private static EulerPuzzleInputLock owner;
        public static bool IsLocked { get { return owner != null; } }
        private static readonly Dictionary<Type, bool> pollingTypes = new Dictionary<Type, bool>();
        private readonly List<MonoBehaviour> suspended = new List<MonoBehaviour>();
        private float oldTimeScale;
        private CursorLockMode oldCursorLock;
        private bool oldCursorVisible, acquired, restoring;
#if ENABLE_INPUT_SYSTEM
        private List<InputAction> enabledActions;
        private InputSettings.UpdateMode oldUpdateMode;
#endif

        public bool Acquire(Transform puzzleRoot, bool pauseTime)
        {
            if (owner != null) return false;
            owner = this; acquired = true;
            oldTimeScale = Time.timeScale;
            oldCursorLock = Cursor.lockState; oldCursorVisible = Cursor.visible;
#if ENABLE_INPUT_SYSTEM
            enabledActions = InputSystem.ListEnabledActions();
            oldUpdateMode = InputSystem.settings.updateMode;
#endif
            try
            {
                if (pauseTime) Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                // Capture enabled polling scripts, not scene references. Pure event managers remain active.
                var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null || !behaviour.isActiveAndEnabled || !behaviour.gameObject.scene.IsValid() ||
                        behaviour.transform.IsChildOf(puzzleRoot) || behaviour is IEulerPuzzleKeepRunning ||
                        IsVisual(behaviour) || !PollsInput(behaviour.GetType())) continue;
                    suspended.Add(behaviour);
                    behaviour.enabled = false;
                    if (!acquired) return false; // A user's OnDisable canceled/destroyed the puzzle.
                }
#if ENABLE_INPUT_SYSTEM
                InputSystem.DisableAllEnabledActions();
                // Fixed-update input would stop when Time.timeScale is zero.
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                InputSystem.onActionChange += BlockNewAction;
#endif
                return true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (!acquired || restoring) return;
            restoring = true; acquired = false;
            try
            {
#if ENABLE_INPUT_SYSTEM
            InputSystem.onActionChange -= BlockNewAction;
#endif
            // Keep the public lock set during OnEnable callbacks.
            for (int i = suspended.Count - 1; i >= 0; i--)
            {
                var behaviour = suspended[i];
                if (behaviour == null) continue;
                try { behaviour.enabled = true; }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            suspended.Clear();
#if ENABLE_INPUT_SYSTEM
            // OnEnable can enable entire maps. Restore the exact pre-popup action set afterwards.
            InputSystem.DisableAllEnabledActions();
            if (enabledActions != null)
                foreach (var action in enabledActions)
                    try { action.Enable(); }
                    catch (Exception ex) { Debug.LogWarning("Euler Path: an input action could not be restored: " + ex.Message); }
            enabledActions = null;
            InputSystem.settings.updateMode = oldUpdateMode;
#endif
            }
            finally
            {
                Time.timeScale = oldTimeScale;
                Cursor.lockState = oldCursorLock; Cursor.visible = oldCursorVisible;
                if (owner == this) owner = null;
                restoring = false;
            }
        }

        private static bool IsVisual(MonoBehaviour b)
        {
            return b is Graphic || b is CanvasScaler || b is LayoutGroup || b is ContentSizeFitter ||
                b is AspectRatioFitter || b is Mask || b is RectMask2D || b is BaseMeshEffect;
        }
        private static bool PollsInput(Type type)
        {
            bool result;
            if (pollingTypes.TryGetValue(type, out result)) return result;
            for (Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                if (t.FullName == "UnityEngine.InputSystem.PlayerInput" ||
                    t.FullName == "UnityEngine.EventSystems.BaseInputModule" ||
                    t.FullName == "UnityEngine.EventSystems.EventSystem") result = true;
                foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (method.Name == "Update" || method.Name == "FixedUpdate" || method.Name == "LateUpdate" ||
                        method.Name == "OnGUI" || method.Name.StartsWith("OnMouse", StringComparison.Ordinal)) result = true;
            }
            pollingTypes[type] = result;
            return result;
        }
#if ENABLE_INPUT_SYSTEM
        private void BlockNewAction(object subject, InputActionChange change)
        {
            if (!acquired) return;
            if (change == InputActionChange.ActionEnabled)
            {
                var action = subject as InputAction;
                if (action != null) action.Disable();
            }
            else if (change == InputActionChange.ActionMapEnabled)
            {
                var map = subject as InputActionMap;
                if (map != null) map.Disable();
            }
        }
#endif
    }
}
