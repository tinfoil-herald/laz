// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz.Native;

internal abstract class NativeLazbot
{
    protected const string LibraryName = "laz_native";

    public static (byte[] Data, int Width, int Height) CreateScreenCapture(int width, int height, int x, int y)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Capture width must be > 0.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Capture height must be > 0.");

        int byteCount;
        try
        {
            byteCount = checked(width * height * 4);
        }
        catch (OverflowException ex)
        {
            throw new ArgumentException("Capture dimensions are too large.", ex);
        }

        var buffer = new byte[byteCount];

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var success = captureScreen(
                x,
                y,
                width,
                height,
                handle.AddrOfPinnedObject());

            if (!success)
            {
                throw new InvalidOperationException("Failed to capture screen.");
            }
        }
        finally
        {
            handle.Free();
        }

        return (buffer, width, height);
    }

    public static Laz.Point GetMousePosition()
    {
        return getMousePosition();
    }

    public static Color GetPixelColor(int x, int y)
    {
        return getPixelColor(x, y);
    }

    public static void KeyDown(Key key)
    {
        sendKeyPress((int)key);
    }

    public static void KeyUp(Key key)
    {
        sendKeyRelease((int)key);
    }

    public static void MouseScroll(int value)
    {
        sendScrollEvent(value);
    }

    public abstract void MouseUp(MouseButton button);
    public abstract void MouseDown(MouseButton button);
    public abstract void JumpTo(int x, int y);

    #region P/Invoke Declarations

    [DllImport(LibraryName, EntryPoint = "captureScreen")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool captureScreen(int x, int y, int width, int height, IntPtr buffer);

    [DllImport(LibraryName, EntryPoint = "getMousePosition")]
    private static extern Point getMousePosition();

    [DllImport(LibraryName, EntryPoint = "getPixelColor")]
    private static extern Color getPixelColor(int x, int y);

    [DllImport(LibraryName, EntryPoint = "sendKeyPress")]
    private static extern void sendKeyPress(int keyCode);

    [DllImport(LibraryName, EntryPoint = "sendKeyRelease")]
    private static extern void sendKeyRelease(int keyCode);

    [DllImport(LibraryName, EntryPoint = "sendScrollEvent")]
    private static extern void sendScrollEvent(int value);

    // CA2101 wants LPWStr (UTF-16), but the native function takes const char* (UTF-8).
    // LPUTF8Str is the correct marshaling; suppress the false positive.
#pragma warning disable CA2101
    [DllImport(LibraryName, EntryPoint = "setClipboardText")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool SetClipboardText(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text);
#pragma warning restore CA2101

    #endregion
}