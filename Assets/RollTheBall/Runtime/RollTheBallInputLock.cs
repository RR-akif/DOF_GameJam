using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace RollTheBall
{
    // Custom gameplay input owners can implement this without any prefab references.
    // The token must restore the owner's exact previous state when disposed.
    public interface IRollTheBallInputParticipant
    {
        IDisposable AcquirePuzzleInputLock();
    }

    public sealed class RollTheBallInputLock : IDisposable
    {
        public static bool IsLocked { get; private set; }
        public static event Action<bool> Changed;
        private readonly List<Action> restore = new List<Action>();
        private readonly CursorLockMode cursorMode;
        private readonly bool cursorVisible;
        private readonly EventSystem previousEventSystem;
        private readonly GameObject previousSelection;
        private bool disposed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { IsLocked = false; Changed = null; }

        internal RollTheBallInputLock(Transform puzzleRoot)
        {
            if (IsLocked) throw new InvalidOperationException("A puzzle already owns the input lock.");
            cursorMode = Cursor.lockState;
            cursorVisible = Cursor.visible;
            previousEventSystem = EventSystem.current;
            previousSelection = previousEventSystem ? previousEventSystem.currentSelectedGameObject : null;
            IsLocked = true;
            try
            {
                // Snapshot action states before disabling UI modules: a host UI module
                // can share its action asset with PlayerInput.
                var playerInputType = Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
                foreach (var behaviour in Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (!behaviour || !behaviour.isActiveAndEnabled || behaviour.transform.IsChildOf(puzzleRoot)) continue;
                    if (playerInputType != null && playerInputType.IsInstanceOfType(behaviour))
                        SuspendPlayerInput(behaviour);
                    var participant = behaviour as IRollTheBallInputParticipant;
                    if (participant != null)
                    {
                        var token = participant.AcquirePuzzleInputLock();
                        if (token != null) restore.Add(token.Dispose);
                    }
                }
                foreach (var module in Object.FindObjectsOfType<BaseInputModule>())
                {
                    if (!module.enabled || module.transform.IsChildOf(puzzleRoot)) continue;
                    var captured = module;
                    restore.Add(() => { if (captured) captured.enabled = true; });
                    // Drop pending host pointer presses/drags rather than replaying
                    // input gathered while its EventSystem was suspended.
                    module.DeactivateModule();
                    module.enabled = false;
                }
                // The popup owns a private EventSystem, so host UI/physics raycasts are suspended.
                foreach (var system in Object.FindObjectsOfType<EventSystem>())
                {
                    if (!system.enabled || system.transform.IsChildOf(puzzleRoot)) continue;
                    var captured = system;
                    restore.Add(() => { if (captured) captured.enabled = true; });
                    system.SetSelectedGameObject(null);
                    system.enabled = false;
                }
                foreach (var raycaster in Object.FindObjectsOfType<BaseRaycaster>())
                {
                    if (!raycaster.enabled || raycaster.transform.IsChildOf(puzzleRoot)) continue;
                    var captured = raycaster;
                    restore.Add(() => { if (captured) captured.enabled = true; });
                    raycaster.enabled = false;
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Notify(true);
            }
            catch { Dispose(); throw; }
        }

        private void SuspendPlayerInput(MonoBehaviour player)
        {
            Type type = player.GetType();
            bool wasActive = (bool)type.GetProperty("inputIsActive").GetValue(player, null);
            object asset = type.GetProperty("actions").GetValue(player, null);
            var enabledActions = new List<object>();
            if (asset != null)
            {
                foreach (var action in (IEnumerable)asset)
                    if ((bool)action.GetType().GetProperty("enabled").GetValue(action, null)) enabledActions.Add(action);
            }
            // Register restoration before mutation, including partially failed acquisition.
            restore.Add(() =>
            {
                if (!player) return;
                if (wasActive) Invoke(player, "ActivateInput");
                else Invoke(player, "DeactivateInput");
                if (asset == null) return;
                Invoke(asset, "Disable");
                foreach (var action in enabledActions) Invoke(action, "Enable");
            });
            Invoke(player, "DeactivateInput");
            if (asset != null) Invoke(asset, "Disable");
        }

        internal static void Invoke(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, Type.EmptyTypes);
            if (info == null) throw new MissingMethodException(target.GetType().FullName, method);
            info.Invoke(target, null);
        }

        private static void Notify(bool value)
        {
            var handlers = Changed;
            if (handlers == null) return;
            foreach (Action<bool> handler in handlers.GetInvocationList())
                try { handler(value); } catch (Exception error) { Debug.LogException(error); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Host input remains gated until every captured owner has been restored.
            for (int i = restore.Count - 1; i >= 0; i--)
                try { restore[i](); } catch (Exception error) { Debug.LogException(error); }
            restore.Clear();
            if (previousEventSystem && previousEventSystem.isActiveAndEnabled)
            {
                EventSystem.current = previousEventSystem;
                if (previousSelection && previousSelection.activeInHierarchy)
                    previousEventSystem.SetSelectedGameObject(previousSelection);
            }
            Cursor.lockState = cursorMode;
            Cursor.visible = cursorVisible;
            IsLocked = false;
            Notify(false);
        }

        internal static GameObject CreateEventSystem(Transform parent)
        {
            var go = new GameObject("Puzzle EventSystem");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<EventSystem>();
            system.pixelDragThreshold = 8;
            system.sendNavigationEvents = true;
            try
            {
#if ENABLE_INPUT_SYSTEM
                var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (type == null) throw new InvalidOperationException("Input System UI module is unavailable.");
                // OnEnable assigns its own default actions, independent of PlayerInput's assets.
                go.AddComponent(type);
#else
                go.AddComponent<StandaloneInputModule>();
#endif
                return go;
            }
            catch { Object.Destroy(go); throw; }
        }
    }
}
