using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SerpentRogue
{
    public enum GameKey { Left, Right, Boost, Pause, One, Two, Three, Reroll }

    // Works with BOTH Unity input systems (old "Input Manager" and the new
    // "Input System" package), so the project runs whatever your settings are.
    public static class GameInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool MouseHeld
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#elif ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
                return false;
#endif
            }
        }

        public static bool Held(GameKey k)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (k)
            {
                case GameKey.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case GameKey.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case GameKey.Boost: return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case GameKey.Pause: return Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.P);
                case GameKey.One: return Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1);
                case GameKey.Two: return Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2);
                case GameKey.Three: return Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3);
                case GameKey.Reroll: return Input.GetKey(KeyCode.R);
            }
            return false;
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case GameKey.Left: return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                case GameKey.Right: return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                case GameKey.Boost: return kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
                case GameKey.Pause: return kb.escapeKey.isPressed || kb.pKey.isPressed;
                case GameKey.One: return kb.digit1Key.isPressed || kb.numpad1Key.isPressed;
                case GameKey.Two: return kb.digit2Key.isPressed || kb.numpad2Key.isPressed;
                case GameKey.Three: return kb.digit3Key.isPressed || kb.numpad3Key.isPressed;
                case GameKey.Reroll: return kb.rKey.isPressed;
            }
            return false;
#else
            return false;
#endif
        }

        // "Pressed this frame" = held now but not last frame. Call Tick() once per frame.
        static readonly bool[] prev = new bool[8];
        static readonly bool[] now = new bool[8];
        static Vector2 lastMouse;
        public static bool MouseMoved { get; private set; }

        public static void Tick()
        {
            for (int i = 0; i < now.Length; i++)
            {
                prev[i] = now[i];
                now[i] = Held((GameKey)i);
            }
            Vector2 m = MousePosition;
            MouseMoved = (m - lastMouse).sqrMagnitude > 0.5f;
            lastMouse = m;
        }

        public static bool Pressed(GameKey k)
        {
            return now[(int)k] && !prev[(int)k];
        }
    }
}
