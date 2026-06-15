// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Tests.Infrastructure;

public static class CaptureExtensions
{
    /// <summary>
    /// Gets the color of the pixel at the specified coordinates from a screen capture.
    /// Pixel data is expected in BGRA format (4 bytes per pixel).
    /// </summary>
    public static (byte R, byte G, byte B, byte A) GetPixel(
        this (byte[] Data, int Width, int Height) capture, int x, int y)
    {
        if (x < 0 || x >= capture.Width || y < 0 || y >= capture.Height)
            throw new ArgumentOutOfRangeException(
                $"Coordinates ({x}, {y}) out of bounds for {capture.Width}x{capture.Height} image");

        var offset = (y * capture.Width + x) * 4;
        var b = capture.Data[offset];
        var g = capture.Data[offset + 1];
        var r = capture.Data[offset + 2];
        var a = capture.Data[offset + 3];
        return (r, g, b, a);
    }
}
