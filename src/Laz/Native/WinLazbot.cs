// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz.Native;

internal class WinLazbot : NativeLazbot
{
    public override void JumpTo(int x, int y)
    {
        sendMouseMove(x, y);
    }

    public override void MouseUp(MouseButton button)
    {
        sendMouseUp(button);
    }

    public override void MouseDown(MouseButton button)
    {
        sendMouseDown(button);
    }

    #region Constants

    // We keep the names consistent with their native counterparts.
    // ReSharper disable InconsistentNaming
    internal const int VK_SHIFT = 0x10;
    internal const int VK_CONTROL = 0x11;
    internal const int VK_MENU = 0x12; // Alt
    internal const int VK_CAPITAL = 0x14; // Caps Lock
    // ReSharper restore InconsistentNaming

    internal const uint MAPVK_VK_TO_VSC = 0;

    #endregion

    #region P/Invoke - laz_native

    [DllImport(LibraryName, EntryPoint = "sendMouseDown")]
    private static extern void sendMouseDown(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseUp")]
    private static extern void sendMouseUp(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseMove")]
    private static extern void sendMouseMove(int x, int y);

    #endregion

    #region P/Invoke - Win32 (user32.dll)

    private const string User32 = "user32.dll";

    [DllImport(User32)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport(User32)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport(User32)]
    internal static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport(User32, CharSet = CharSet.Unicode)]
    internal static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        char[] pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    [DllImport(User32)]
    internal static extern uint MapVirtualKeyExW(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport(User32)]
    internal static extern short GetKeyState(int nVirtKey);

    #endregion
}
