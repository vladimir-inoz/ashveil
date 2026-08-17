using UnityEngine;

namespace Ashveil
{
    public static class GameInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (UnityEngine.InputSystem.Mouse.current != null)
                    return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif
                return Input.mousePosition;
            }
        }

        public static bool Key(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            return kb[Map(key)].isPressed;
#else
            return Input.GetKey(key);
#endif
        }

        public static bool KeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            return kb[Map(key)].wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool Mouse(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return false;
            if (button == 0) return m.leftButton.isPressed;
            if (button == 1) return m.rightButton.isPressed;
            return m.middleButton.isPressed;
#else
            return Input.GetMouseButton(button);
#endif
        }

        public static bool MouseDown(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return false;
            if (button == 0) return m.leftButton.wasPressedThisFrame;
            if (button == 1) return m.rightButton.wasPressedThisFrame;
            return m.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        public static Vector2 MouseDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m != null) return m.delta.ReadValue();
#endif
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
            }
        }

        public static float Scroll
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var m = UnityEngine.InputSystem.Mouse.current;
                return m != null ? m.scroll.ReadValue().y * 0.01f : 0f;
#else
                return Input.mouseScrollDelta.y;
#endif
            }
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static UnityEngine.InputSystem.Key Map(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.W: return UnityEngine.InputSystem.Key.W;
                case KeyCode.A: return UnityEngine.InputSystem.Key.A;
                case KeyCode.S: return UnityEngine.InputSystem.Key.S;
                case KeyCode.D: return UnityEngine.InputSystem.Key.D;
                case KeyCode.Q: return UnityEngine.InputSystem.Key.Q;
                case KeyCode.E: return UnityEngine.InputSystem.Key.E;
                case KeyCode.R: return UnityEngine.InputSystem.Key.R;
                case KeyCode.F: return UnityEngine.InputSystem.Key.F;
                case KeyCode.T: return UnityEngine.InputSystem.Key.T;
                case KeyCode.V: return UnityEngine.InputSystem.Key.V;
                case KeyCode.G: return UnityEngine.InputSystem.Key.G;
                case KeyCode.C: return UnityEngine.InputSystem.Key.C;
                case KeyCode.P: return UnityEngine.InputSystem.Key.P;
                case KeyCode.M: return UnityEngine.InputSystem.Key.M;
                case KeyCode.N: return UnityEngine.InputSystem.Key.N;
                case KeyCode.X: return UnityEngine.InputSystem.Key.X;
                case KeyCode.Z: return UnityEngine.InputSystem.Key.Z;
                case KeyCode.Tab: return UnityEngine.InputSystem.Key.Tab;
                case KeyCode.Escape: return UnityEngine.InputSystem.Key.Escape;
                case KeyCode.Space: return UnityEngine.InputSystem.Key.Space;
                case KeyCode.LeftShift: return UnityEngine.InputSystem.Key.LeftShift;
                case KeyCode.LeftControl: return UnityEngine.InputSystem.Key.LeftCtrl;
                case KeyCode.Backspace: return UnityEngine.InputSystem.Key.Backspace;
                case KeyCode.Alpha0: return UnityEngine.InputSystem.Key.Digit0;
                case KeyCode.Alpha1: return UnityEngine.InputSystem.Key.Digit1;
                case KeyCode.Alpha2: return UnityEngine.InputSystem.Key.Digit2;
                case KeyCode.Alpha3: return UnityEngine.InputSystem.Key.Digit3;
                case KeyCode.Minus: return UnityEngine.InputSystem.Key.Minus;
                case KeyCode.Equals: return UnityEngine.InputSystem.Key.Equals;
                case KeyCode.F1: return UnityEngine.InputSystem.Key.F1;
                case KeyCode.F3: return UnityEngine.InputSystem.Key.F3;
                case KeyCode.F11: return UnityEngine.InputSystem.Key.F11;
                case KeyCode.LeftBracket: return UnityEngine.InputSystem.Key.LeftBracket;
                case KeyCode.RightBracket: return UnityEngine.InputSystem.Key.RightBracket;
                case KeyCode.UpArrow: return UnityEngine.InputSystem.Key.UpArrow;
                case KeyCode.DownArrow: return UnityEngine.InputSystem.Key.DownArrow;
                case KeyCode.LeftArrow: return UnityEngine.InputSystem.Key.LeftArrow;
                case KeyCode.RightArrow: return UnityEngine.InputSystem.Key.RightArrow;
                default: return UnityEngine.InputSystem.Key.None;
            }
        }
#endif
    }
}
