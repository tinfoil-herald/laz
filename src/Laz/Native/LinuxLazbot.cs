// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz.Native;

internal class LinuxLazbot : NativeLazbot
{
    public override void MouseUp(MouseButton button)
    {
        EnsureX11Input();
        sendMouseUp(button);
    }

    public override void MouseDown(MouseButton button)
    {
        EnsureX11Input();
        sendMouseDown(button);
    }

    public override void JumpTo(int x, int y)
    {
        EnsureX11Input();
        sendMouseMove(x, y);
    }

    private static void EnsureX11Input()
    {
        if (!isX11InputAvailable())
            throw new PlatformNotSupportedException(
                "Mouse and keyboard input require an X11 display (DISPLAY is not set). " +
                "Pure Wayland sessions are not supported for input injection. " +
                "Enable XWayland or set the DISPLAY environment variable.");
    }

    #region P/Invoke Declarations

    [DllImport(LibraryName, EntryPoint = "sendMouseDown")]
    private static extern void sendMouseDown(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseUp")]
    private static extern void sendMouseUp(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseMove")]
    private static extern void sendMouseMove(int x, int y);

    [DllImport(LibraryName, EntryPoint = "isX11InputAvailable")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool isX11InputAvailable();

    [DllImport(LibraryName, EntryPoint = "getKeyboardLayoutId")]
    internal static extern int getKeyboardLayoutId();

    [DllImport(LibraryName, EntryPoint = "probeKeyOutput")]
    internal static extern int probeKeyOutput(
        int virtualKeyCode, int level, ref uint deadKeyState,
        [Out] ushort[] outChars, int maxChars);

    #endregion
}
