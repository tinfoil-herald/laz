// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Laz.Native;

internal class LinuxLazbot : NativeLazbot
{
    public override void MouseUp(MouseButton button)
    {
        sendMouseUp(button);
    }

    public override void MouseDown(MouseButton button)
    {
        sendMouseDown(button);
    }

    public override void JumpTo(int x, int y)
    {
        sendMouseMove(x, y);
    }

    #region P/Invoke Declarations

    [DllImport(LibraryName, EntryPoint = "sendMouseDown")]
    private static extern void sendMouseDown(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseUp")]
    private static extern void sendMouseUp(MouseButton button);

    [DllImport(LibraryName, EntryPoint = "sendMouseMove")]
    private static extern void sendMouseMove(int x, int y);

    [DllImport(LibraryName, EntryPoint = "getKeyboardLayoutId")]
    internal static extern int getKeyboardLayoutId();

    [DllImport(LibraryName, EntryPoint = "probeKeyOutput")]
    internal static extern int probeKeyOutput(
        int virtualKeyCode, int level, ref uint deadKeyState,
        [Out] ushort[] outChars, int maxChars);

    #endregion
}
