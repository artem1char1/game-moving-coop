using UnityEngine;
using UnityEngine.InputSystem;

namespace MovingCoop.UnityAdapters
{
    public static class LocalInput
    {
        public static float Turn() {var k=Keyboard.current; return UnityEngine.Application.isFocused && k!=null ? (k.rKey.isPressed?1:0)-(k.qKey.isPressed?1:0):0;}
        public static Vector2 Read()
        {
            return Read(Keyboard.current, UnityEngine.Application.isFocused);
        }
        public static Vector2 Read(Keyboard keyboard, bool focused)
        {
            if (!focused || keyboard == null) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0)), 1);
        }
        public static Vector2 ToWorld(Vector2 screen)
        {
            var right = new Vector2(0.70710678f, -0.70710678f);
            var forward = new Vector2(0.70710678f, 0.70710678f);
            return right * screen.x + forward * screen.y;
        }
    }
}
