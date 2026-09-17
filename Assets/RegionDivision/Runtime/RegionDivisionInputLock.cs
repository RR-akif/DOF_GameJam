using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RegionDivision
{
    /// <summary>Implement on your existing input manager to reuse its lock mechanism.
    /// Active scene providers are discovered automatically; no prefab scene references.
    /// The returned token must release only this acquisition and restore prior state.</summary>
    public interface IRegionDivisionInputLockProvider
    {
        IDisposable AcquirePuzzleInputLock();
    }

    /// <summary>Raw Input.GetKey/GetMouseButton readers must consult IsLocked before
    /// processing gameplay input, or be suspended by a provider. Unity has no global
    /// consume switch for arbitrary scripts polling physical devices.</summary>
    public static class RegionDivisionInputLock
    {
        private static int count;
        private static int releasedFrame = -1;
        public static bool IsLocked { get { return count > 0 || Time.frameCount == releasedFrame; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { count = 0; releasedFrame = -1; }

        internal static IDisposable Acquire()
        {
            var scope = new Scope();
            count++;
            try
            {
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (!behaviour.isActiveAndEnabled) continue;
                    var provider = behaviour as IRegionDivisionInputLockProvider;
                    if (provider != null)
                    {
                        IDisposable token = provider.AcquirePuzzleInputLock();
                        if (token == null) throw new InvalidOperationException("Input-lock provider returned no token.");
                        scope.tokens.Add(token);
                    }
                }
#if ENABLE_INPUT_SYSTEM
                // Preserve individual action states, including partially enabled maps.
                // Do not ActivateInput(), which can change the host's current action map.
                // This includes PlayerInput, independently owned maps, and project-wide
                // actions. The puzzle's own module is still inactive at this point.
                foreach (InputAction action in InputSystem.ListEnabledActions())
                    scope.actions.Add(action);
                foreach (InputAction action in scope.actions) action.Disable();
#endif
                return scope;
            }
            catch { scope.Dispose(); throw; }
        }

        private sealed class Scope : IDisposable
        {
            internal readonly List<IDisposable> tokens = new List<IDisposable>();
#if ENABLE_INPUT_SYSTEM
            internal readonly List<InputAction> actions = new List<InputAction>();
#endif
            private bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
#if ENABLE_INPUT_SYSTEM
                foreach (InputAction action in actions)
                {
                    try { action.Enable(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
                actions.Clear();
#endif
                for (int i = tokens.Count - 1; i >= 0; i--)
                {
                    try { tokens[i].Dispose(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
                tokens.Clear();
                count = Math.Max(0, count - 1);
                releasedFrame = Time.frameCount; // Avoid feeding the closing click to raw-input gameplay.
            }
        }
    }
}
