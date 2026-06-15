// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz.Native;

internal class MacLazbot : NativeLazbot
{
    // macOS doesn't generate dragged events as a result of button press and
    // mouse move events. This means that we have to track button state
    // in order to generate dragged events ourselves.
    private bool _primaryButtonDown;
    private bool _secondaryButtonDown;
    private bool _middleButtonDown;

    public override void MouseUp(MouseButton button)
    {
        var position = GetMousePosition();
        sendMouseUp(position.X, position.Y, button);
        SetButtonState(button, false);
    }

    public override void MouseDown(MouseButton button)
    {
        var position = GetMousePosition();
        sendMouseDown(position.X, position.Y, button);
        SetButtonState(button, true);
    }

    public override void JumpTo(int x, int y)
    {
        // On macOS, we need to pass the current button state to generate
        // proper drag events when buttons are held during movement.
        if (GetPressedButton(out var button))
        {
            sendMouseMove(x, y, button, true);
        }
        else
        {
            sendMouseMove(x, y, MouseButton.Primary, false);
        }
    }

    private void SetButtonState(MouseButton button, bool isDown)
    {
        switch (button)
        {
            case MouseButton.Primary:
                _primaryButtonDown = isDown;
                break;
            case MouseButton.Secondary:
                _secondaryButtonDown = isDown;
                break;
            case MouseButton.Middle:
                _middleButtonDown = isDown;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button), button, null);
        }
    }

    private bool GetPressedButton(out MouseButton button)
    {
        if (_primaryButtonDown)
        {
            button = MouseButton.Primary;
            return true;
        }

        if (_secondaryButtonDown)
        {
            button = MouseButton.Secondary;
            return true;
        }

        if (_middleButtonDown)
        {
            button = MouseButton.Middle;
            return true;
        }

        button = MouseButton.Primary;
        return false;
    }

    #region P/Invoke Declarations

    [DllImport(LibraryName, EntryPoint = "sendMouseDown")]
    private static extern void sendMouseDown(int x, int y, MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseUp")]
    private static extern void sendMouseUp(int x, int y, MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseMove")]
    private static extern void sendMouseMove(
        int x,
        int y,
        MouseButton pressedButton,
        [MarshalAs(UnmanagedType.I1)] bool isButtonDown);

    [DllImport(LibraryName, EntryPoint = "getKeyboardLayoutId")]
    internal static extern int getKeyboardLayoutId();

    [DllImport(LibraryName, EntryPoint = "probeKeyOutput")]
    internal static extern int probeKeyOutput(
        int macKeyCode, int modifierFlags, ref uint deadKeyState,
        [Out] ushort[] outChars, int maxChars);

    #endregion
}
