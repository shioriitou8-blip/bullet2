using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public static class InputBridge
{
    public static bool IsKeyPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return TryGetKeyControl(key, out KeyControl control) && control.isPressed;
#else
        return Input.GetKey(key);
#endif
    }

    public static bool WasKeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return TryGetKeyControl(key, out KeyControl control) && control.wasPressedThisFrame;
#else
        return Input.GetKeyDown(key);
#endif
    }

    public static Vector2 GetMoveVector2D()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            move += stick;
        }

        return Vector2.ClampMagnitude(move, 1f);
#else
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return Vector2.ClampMagnitude(input, 1f);
#endif
    }

    public static bool IsFire1Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool gamepad = Gamepad.current != null &&
            (Gamepad.current.buttonSouth.isPressed || Gamepad.current.rightTrigger.ReadValue() > 0.35f);
        return mouse || gamepad;
#else
        try
        {
            return Input.GetButton("Fire1");
        }
        catch
        {
            return false;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetKeyControl(KeyCode key, out KeyControl control)
    {
        control = null;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.A: control = keyboard.aKey; return true;
            case KeyCode.B: control = keyboard.bKey; return true;
            case KeyCode.C: control = keyboard.cKey; return true;
            case KeyCode.D: control = keyboard.dKey; return true;
            case KeyCode.E: control = keyboard.eKey; return true;
            case KeyCode.F: control = keyboard.fKey; return true;
            case KeyCode.G: control = keyboard.gKey; return true;
            case KeyCode.H: control = keyboard.hKey; return true;
            case KeyCode.I: control = keyboard.iKey; return true;
            case KeyCode.J: control = keyboard.jKey; return true;
            case KeyCode.K: control = keyboard.kKey; return true;
            case KeyCode.L: control = keyboard.lKey; return true;
            case KeyCode.M: control = keyboard.mKey; return true;
            case KeyCode.N: control = keyboard.nKey; return true;
            case KeyCode.O: control = keyboard.oKey; return true;
            case KeyCode.P: control = keyboard.pKey; return true;
            case KeyCode.Q: control = keyboard.qKey; return true;
            case KeyCode.R: control = keyboard.rKey; return true;
            case KeyCode.S: control = keyboard.sKey; return true;
            case KeyCode.T: control = keyboard.tKey; return true;
            case KeyCode.U: control = keyboard.uKey; return true;
            case KeyCode.V: control = keyboard.vKey; return true;
            case KeyCode.W: control = keyboard.wKey; return true;
            case KeyCode.X: control = keyboard.xKey; return true;
            case KeyCode.Y: control = keyboard.yKey; return true;
            case KeyCode.Z: control = keyboard.zKey; return true;
            case KeyCode.UpArrow: control = keyboard.upArrowKey; return true;
            case KeyCode.DownArrow: control = keyboard.downArrowKey; return true;
            case KeyCode.LeftArrow: control = keyboard.leftArrowKey; return true;
            case KeyCode.RightArrow: control = keyboard.rightArrowKey; return true;
            case KeyCode.LeftShift: control = keyboard.leftShiftKey; return true;
            case KeyCode.RightShift: control = keyboard.rightShiftKey; return true;
            case KeyCode.Space: control = keyboard.spaceKey; return true;
            default: return false;
        }
    }
#endif
}
