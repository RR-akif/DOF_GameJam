using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace DegreesOfFreedom.EulerPath
{
    internal struct EulerPointerFrame
    {
        public Vector2 position;
        public bool down, held, up;
    }

    // Polls devices directly, so no EventSystem or scene InputActionAsset is needed.
    internal sealed class EulerPuzzleInput
    {
        private int finger = -1;
        private bool mouseCaptured;
        private Vector2 lastPosition;

        public void Reset() { finger = -1; mouseCaptured = false; }

        public EulerPointerFrame Poll()
        {
            var frame = new EulerPointerFrame { position = lastPosition };
#if ENABLE_INPUT_SYSTEM
            var screen = Touchscreen.current;
            TouchControl touch = null;
            if (finger >= 0)
            {
                if (screen != null)
                    foreach (var candidate in screen.touches)
                        if (candidate.touchId.ReadValue() == finger) { touch = candidate; break; }
                if (touch == null) { finger = -1; frame.up = true; return frame; }
                frame.position = touch.position.ReadValue();
                frame.held = touch.press.isPressed;
                frame.up = !frame.held;
                if (frame.up) finger = -1;
            }
            else if (screen != null && !mouseCaptured)
            {
                foreach (var candidate in screen.touches)
                    if (candidate.press.wasPressedThisFrame)
                    {
                        finger = candidate.touchId.ReadValue();
                        frame.position = candidate.position.ReadValue();
                        frame.down = true; frame.held = candidate.press.isPressed;
                        frame.up = !frame.held;
                        if (frame.up) finger = -1;
                        lastPosition = frame.position;
                        return frame;
                    }
            }
            if (touch == null && finger < 0 && Mouse.current != null)
            {
                var mouse = Mouse.current;
                frame.position = mouse.position.ReadValue();
                frame.down = mouse.leftButton.wasPressedThisFrame;
                frame.held = mouse.leftButton.isPressed;
                frame.up = mouse.leftButton.wasReleasedThisFrame || (mouseCaptured && !frame.held);
                if (frame.down) mouseCaptured = true;
                if (frame.up) mouseCaptured = false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER || !UNITY_2019_3_OR_NEWER
            if (finger >= 0)
            {
                bool found = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId != finger) continue;
                    found = true; frame.position = touch.position;
                    frame.up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                    frame.held = !frame.up;
                    break;
                }
                if (!found) frame.up = true;
                if (frame.up) finger = -1;
            }
            else if (Input.touchCount > 0 && !mouseCaptured)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began) continue;
                    finger = touch.fingerId; frame.position = touch.position;
                    frame.down = true; frame.held = true; break;
                }
            }
            else
            {
                frame.position = Input.mousePosition;
                frame.down = Input.GetMouseButtonDown(0);
                frame.held = Input.GetMouseButton(0);
                frame.up = Input.GetMouseButtonUp(0) || (mouseCaptured && !frame.held);
                if (frame.down) mouseCaptured = true;
                if (frame.up) mouseCaptured = false;
            }
#endif
            lastPosition = frame.position;
            return frame;
        }

        public static bool CancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER || !UNITY_2019_3_OR_NEWER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
        public static bool RestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER || !UNITY_2019_3_OR_NEWER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
        public static bool DebugPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER || !UNITY_2019_3_OR_NEWER
            return Input.GetKeyDown(KeyCode.F8);
#else
            return false;
#endif
        }
    }
}
