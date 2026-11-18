using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace FactoryVerse
{
    /// <summary>
    /// Drop-in replacement for the legacy UnityEngine.Input calls used by the desktop/debug controls.
    /// Android (Quest) only supports the Input System package, so these read Keyboard/Mouse from it.
    /// On a headset with no keyboard or mouse every call simply returns false / zero.
    /// </summary>
    public static class FVInput
    {
#if ENABLE_INPUT_SYSTEM
        static KeyControl Key(KeyCode code)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            switch (code)
            {
                case KeyCode.Alpha1: return kb.digit1Key;
                case KeyCode.Alpha2: return kb.digit2Key;
                case KeyCode.Alpha3: return kb.digit3Key;
                case KeyCode.Space: return kb.spaceKey;
                case KeyCode.Escape: return kb.escapeKey;
                case KeyCode.Tab: return kb.tabKey;
                case KeyCode.LeftShift: return kb.leftShiftKey;
                case KeyCode.LeftControl: return kb.leftCtrlKey;
                case KeyCode.F1: return kb.f1Key;
                case KeyCode.F2: return kb.f2Key;
                case KeyCode.UpArrow: return kb.upArrowKey;
                case KeyCode.DownArrow: return kb.downArrowKey;
                case KeyCode.LeftArrow: return kb.leftArrowKey;
                case KeyCode.RightArrow: return kb.rightArrowKey;
            }
            if (code >= KeyCode.A && code <= KeyCode.Z)
                return kb[(UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.A + (code - KeyCode.A))];
            return null;
        }

        static ButtonControl MouseButton(int b)
        {
            var m = Mouse.current;
            if (m == null) return null;
            return b == 0 ? m.leftButton : b == 1 ? m.rightButton : m.middleButton;
        }

        public static bool GetKey(KeyCode k) { var c = Key(k); return c != null && c.isPressed; }
        public static bool GetKeyDown(KeyCode k) { var c = Key(k); return c != null && c.wasPressedThisFrame; }
        public static bool GetMouseButton(int b) { var c = MouseButton(b); return c != null && c.isPressed; }
        public static bool GetMouseButtonDown(int b) { var c = MouseButton(b); return c != null && c.wasPressedThisFrame; }

        public static Vector2 mouseScrollDelta
        {
            get { var m = Mouse.current; return m == null ? Vector2.zero : m.scroll.ReadValue() / 120f; }
        }

        public static float GetAxisRaw(string axis)
        {
            switch (axis)
            {
                case "Horizontal":
                    return (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow) ? 1f : 0f) - (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                case "Vertical":
                    return (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow) ? 1f : 0f) - (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow) ? 1f : 0f);
                case "Mouse X": { var m = Mouse.current; return m == null ? 0f : m.delta.ReadValue().x * 0.05f; }
                case "Mouse Y": { var m = Mouse.current; return m == null ? 0f : m.delta.ReadValue().y * 0.05f; }
            }
            return 0f;
        }

        public static float GetAxis(string axis) => GetAxisRaw(axis);
#else
        public static bool GetKey(KeyCode k) => Input.GetKey(k);
        public static bool GetKeyDown(KeyCode k) => Input.GetKeyDown(k);
        public static bool GetMouseButton(int b) => Input.GetMouseButton(b);
        public static bool GetMouseButtonDown(int b) => Input.GetMouseButtonDown(b);
        public static Vector2 mouseScrollDelta => Input.mouseScrollDelta;
        public static float GetAxisRaw(string axis) => Input.GetAxisRaw(axis);
        public static float GetAxis(string axis) => Input.GetAxis(axis);
#endif
    }
}
