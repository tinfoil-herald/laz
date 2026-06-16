// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Laz;

/// <summary>
/// Contains methods for reading the pixels from the screen.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API reserved for potential future stateful extensions.")]
public class Screen
{
    internal Screen()
    {
    }

    /// <summary>
    /// Captures a rectangular region of the screen and returns the raw pixel data.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The returned <c>Data</c> array contains pixels in BGRA order (blue, green, red, alpha),
    /// 4 bytes per pixel, in row-major order from top to bottom. The alpha channel is always 255.
    /// </para>
    ///
    /// <para>
    /// On Windows, the coordinate space depends on the DPI awareness of the current process.
    /// The capture spans the full virtual desktop and includes layered and transparent windows.
    /// </para>
    ///
    /// <para>
    /// On macOS, the operating system handles scaling transparently, so the coordinate space is
    /// consistent with other operations regardless of display resolution. This call requires
    /// special permissions granted in System Settings. The operating system will prompt the user when this API is used
    /// for the first time. Without the permissions, this method will fail.
    /// </para>
    ///
    /// <para>
    /// On Linux, the backend is selected automatically based on the current display server. In Wayland sessions,
    /// including XWayland, Laz always uses a PipeWire-based backend, which requires a screen recording permission.
    /// The operating system will prompt the user when this API is used for the first time. Without the permissions,
    /// this method will fail.
    ///
    /// The X11 backend is used only in pure X11 sessions, like Xvfb.
    /// </para>
    /// </remarks>
    ///
    /// <param name="origin">The top-left corner of the capture region.</param>
    /// <param name="width">The width of the capture region in pixels.</param>
    /// <param name="height">The height of the capture region in pixels.</param>
    /// <returns>A tuple of the raw pixel data, width, and height of the captured region.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the capture fails, including when Screen Recording permission is not granted on macOS.</exception>
    public (byte[] Data, int Width, int Height) Capture(Point origin, int width, int height)
    {
        return Native.NativeLazbot.CreateScreenCapture(width, height, origin.X, origin.Y);
    }

    /// <summary>
    /// Returns the color of the pixel at the specified screen position.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On Windows, the coordinate space depends on the DPI awareness of the current process.
    /// </para>
    ///
    /// <para>
    /// On macOS, the operating system handles scaling transparently, so the returned
    /// coordinates are consistent with other operations regardless of display resolution. This call requires
    /// special permissions granted in System Settings. The operating system will prompt the user when this API is used
    /// for the first time. Without the permissions, this method will fail.
    /// </para>
    ///
    /// <para>
    /// On Linux, the backend is selected automatically based on the current display server. In Wayland sessions,
    /// including XWayland, Laz always uses a PipeWire-based backend, which requires a screen recording permission.
    /// The operating system will prompt the user when this API is used for the first time. Without the permissions,
    /// this method will fail.
    ///
    /// The X11 backend is used only in pure X11 sessions, like Xvfb.
    /// </para>
    /// </remarks>
    ///
    /// <param name="point">The screen position of the pixel.</param>
    /// <returns>The RGBA color of the pixel at the specified position. The alpha channel is always 255.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the capture fails, including when Screen Recording permission is not granted on macOS.</exception>
    public (byte R, byte G, byte B, byte A) GetColorAt(Point point)
    {
        var (data, _, _) = Capture(point, 1, 1);
        return (R: data[2], G: data[1], B: data[0], A: data[3]);
    }
}
