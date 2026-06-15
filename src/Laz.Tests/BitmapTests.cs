// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Tests.Infrastructure;
using Xunit;

namespace Laz.Tests;

public class CaptureExtensionsTests
{
    [Fact]
    public void ReturnsCorrectColor()
    {
        // BGRA bytes for a single pixel:
        // B=1, G=2, R=3, A=4 => (R=3, G=2, B=1, A=4)
        (byte[] Data, int Width, int Height) capture = ([1, 2, 3, 4], 1, 1);
        var color = capture.GetPixel(0, 0);

        Assert.Equal((byte)3, color.R);
        Assert.Equal((byte)2, color.G);
        Assert.Equal((byte)1, color.B);
        Assert.Equal((byte)4, color.A);
    }

    [Fact]
    public void ThrowsOutOfBounds()
    {
        var capture = (new byte[4], 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => capture.GetPixel(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => capture.GetPixel(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => capture.GetPixel(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => capture.GetPixel(0, 1));
    }

    [Fact]
    public void IsBgraOrder()
    {
        const byte blue = 10;
        const byte green = 20;
        const byte red = 30;
        const byte alpha = 40;
        (byte[] Data, int Width, int Height) capture = ([blue, green, red, alpha], 1, 1);

        var color = capture.GetPixel(0, 0);

        Assert.Equal(red, color.R);
        Assert.Equal(green, color.G);
        Assert.Equal(blue, color.B);
        Assert.Equal(alpha, color.A);
    }

    [Fact]
    public void ReturnsCorrectPixel()
    {
        // 2x2 bitmap to verify row stride calculation
        // Layout in BGRA: [row0: pixel(0,0), pixel(1,0)] [row1: pixel(0,1), pixel(1,1)]
        var data = new byte[]
        {
            // Row 0
            0, 0, 0, 255,       // pixel (0,0): black
            255, 255, 255, 255, // pixel (1,0): white
            // Row 1
            0, 0, 255, 255,     // pixel (0,1): red (BGRA: B=0, G=0, R=255)
            0, 255, 0, 255      // pixel (1,1): green (BGRA: B=0, G=255, R=0)
        };
        var capture = (data, 2, 2);

        var bottomRight = capture.GetPixel(1, 1);

        Assert.Equal((byte)0, bottomRight.R);
        Assert.Equal((byte)255, bottomRight.G);
        Assert.Equal((byte)0, bottomRight.B);
        Assert.Equal((byte)255, bottomRight.A);
    }
}
