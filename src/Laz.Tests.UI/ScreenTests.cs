// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media;
using Laz.Tests.Infrastructure;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

public class ScreenTests : RobotTestBase
{
    #region Validation Tests (Non-UI)

    [Fact]
    public void WidthMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var screenRect = new Rectangle(0, 0, 0, 1);
            return Lazbot.Screen.Capture(new(screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var screenRect = new Rectangle(0, 0, -1, 1);
            return Lazbot.Screen.Capture(new(screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
        });
    }

    [Fact]
    public void HeightMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var screenRect = new Rectangle(0, 0, 1, 0);
            return Lazbot.Screen.Capture(new(screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var screenRect = new Rectangle(0, 0, 1, -1);
            return Lazbot.Screen.Capture(new(screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
        });
    }

    [Fact]
    public void ThrowArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            var screenRect = new Rectangle(0, 0, int.MaxValue, int.MaxValue);
            Lazbot.Screen.Capture(new(screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
        });
        Assert.Contains("Capture dimensions are too large.", ex.Message);
        Assert.IsType<OverflowException>(ex.InnerException);
    }

    #endregion

    #region Capture Tests

    [Fact]
    public void ReturnsNonNullImage()
    {
        var rect = new Rectangle(0, 0, 100, 100);

        Lazbot.Screen.Capture(new(rect.X, rect.Y), rect.Width, rect.Height);
    }

    [Fact]
    public void ReturnsCorrectDimensions()
    {
        var rect = new Rectangle(0, 0, 150, 75);

        var image = Lazbot.Screen.Capture(new(rect.X, rect.Y), rect.Width, rect.Height);

        Assert.Equal(150, image.Width);
        Assert.Equal(75, image.Height);
    }

    [Fact]
    public async Task ReturnsImageWithCorrectColor()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.Blue;
            TestWindow.Content = null;
        });

        var windowPos = await OnUIThread(() => GetScreenTopLeft(TestWindow));
        var rect = new Rectangle(windowPos.X + 50, windowPos.Y + 50, 50, 50);

        await DelayedAssertion.Eventually("Window should render with blue background and captured image should contain blue pixels", () =>
        {
            var image = Lazbot.Screen.Capture(new(rect.X, rect.Y), rect.Width, rect.Height);

            Assert.Equal(50, image.Width);
            Assert.Equal(50, image.Height);

            var centerPixel = image.GetPixel(25, 25);
            Assert.True(ColorCircle.IsBlue((centerPixel.R, centerPixel.G, centerPixel.B)),
                $"Expected blue, got RGB({centerPixel.R}, {centerPixel.G}, {centerPixel.B})");
        });
    }

    [Fact]
    public void ReturnsExactly4Bytes()
    {
        var rect = new Rectangle(100, 100, 1, 1);

        var image = Lazbot.Screen.Capture(new(rect.X, rect.Y), rect.Width, rect.Height);

        Assert.Equal(1, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(4, image.Data.Length); // 1 pixel * 4 bytes (BGRA)
    }

    #endregion

    #region GetColorAt Tests

    [Fact]
    public async Task ReturnsRedColor()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.Red;
            TestWindow.Content = null;
        });

        var center = await OnUIThread(() => GetScreenCenter(TestWindow));

        await DelayedAssertion.Eventually("Window center pixel should be red after setting red background", () =>
        {
            var color = Lazbot.Screen.GetColorAt(new(center.X, center.Y));
            Assert.True(ColorCircle.IsRed((color.R, color.G, color.B)),
                $"Expected red, got RGB({color.R}, {color.G}, {color.B})");
        });
    }

    [Fact]
    public async Task ReturnsGreenColor()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.Green;
            TestWindow.Content = null;
        });

        var center = await OnUIThread(() => GetScreenCenter(TestWindow));

        await DelayedAssertion.Eventually("Window center pixel should be green after setting green background", () =>
        {
            var color = Lazbot.Screen.GetColorAt(new(center.X, center.Y));
            Assert.True(ColorCircle.IsGreen((color.R, color.G, color.B)),
                $"Expected green, got RGB({color.R}, {color.G}, {color.B})");
        });
    }

    [Fact]
    public async Task ReturnsBlueColor()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.Blue;
            TestWindow.Content = null;
        });

        var center = await OnUIThread(() => GetScreenCenter(TestWindow));

        await DelayedAssertion.Eventually("Window center pixel should be blue after setting blue background", () =>
        {
            var color = Lazbot.Screen.GetColorAt(new(center.X, center.Y));
            Assert.True(ColorCircle.IsBlue((color.R, color.G, color.B)),
                $"Expected blue, got RGB({color.R}, {color.G}, {color.B})");
        });
    }

    [Fact]
    public async Task ReturnsWhiteColor()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.White;
            TestWindow.Content = null;
        });

        var center = await OnUIThread(() => GetScreenCenter(TestWindow));

        await DelayedAssertion.Eventually("Window center pixel should be white after setting white background", () =>
        {
            var color = Lazbot.Screen.GetColorAt(new(center.X, center.Y));
            var hsv = ColorCircle.ToHsv((color.R, color.G, color.B));
            Assert.True(hsv.IsWhite,
                $"Expected white, got RGB({color.R}, {color.G}, {color.B}) / HSV({hsv.H:F0}, {hsv.S:F2}, {hsv.V:F2})");
        });
    }

    [Fact]
    public void ReturnsBlack()
    {
        var offScreenPoint = new Point(-10000, -10000);

        var color = Lazbot.Screen.GetColorAt(new(offScreenPoint.X, offScreenPoint.Y));

        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public async Task MatchesCapturePixel()
    {
        await OnUIThread(() =>
        {
            TestWindow.Background = Brushes.Magenta;
            TestWindow.Content = null;
        });

        // Use a point well inside the window to avoid edge effects
        var topLeft = await OnUIThread(() => GetScreenTopLeft(TestWindow));
        var point = new Point(topLeft.X + 100, topLeft.Y + 100);

        await DelayedAssertion.Eventually("GetColorAt and Capture should return matching magenta colors", () =>
        {
            var colorFromGetColorAt = Lazbot.Screen.GetColorAt(new (point.X, point.Y));
            var screenRect = new Rectangle(point.X, point.Y, 1, 1);
            var capture = Lazbot.Screen.Capture(new (screenRect.X, screenRect.Y), screenRect.Width, screenRect.Height);
            var colorFromCapture = capture.GetPixel(0, 0);

            // Using HSV-based comparison which is more robust to color space differences
            var distance = ColorCircle.Distance(
                (colorFromGetColorAt.R, colorFromGetColorAt.G, colorFromGetColorAt.B),
                (colorFromCapture.R, colorFromCapture.G, colorFromCapture.B));
            Assert.True(ColorCircle.AreSimilar(
                (colorFromGetColorAt.R, colorFromGetColorAt.G, colorFromGetColorAt.B),
                (colorFromCapture.R, colorFromCapture.G, colorFromCapture.B),
                tolerance: 0.15f),
                $"Colors should match. GetColorAt=RGB({colorFromGetColorAt.R}, {colorFromGetColorAt.G}, {colorFromGetColorAt.B}), " +
                $"Capture=RGB({colorFromCapture.R}, {colorFromCapture.G}, {colorFromCapture.B}), Distance={distance:F3}");

            Assert.True(ColorCircle.IsMagenta((colorFromGetColorAt.R, colorFromGetColorAt.G, colorFromGetColorAt.B)),
                $"GetColorAt should return magenta, got RGB({colorFromGetColorAt.R}, {colorFromGetColorAt.G}, {colorFromGetColorAt.B})");
            Assert.True(ColorCircle.IsMagenta((colorFromCapture.R, colorFromCapture.G, colorFromCapture.B)),
                $"Capture should return magenta, got RGB({colorFromCapture.R}, {colorFromCapture.G}, {colorFromCapture.B})");
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CaptureAtOriginPointReturnsImage()
    {
        var rect = new Rectangle(0, 0, 10, 10);

        Lazbot.Screen.Capture(new(rect.X, rect.Y), rect.Width, rect.Height);
    }

    [Fact]
    public void GetColorAtOriginPointReturnsColor()
    {
        var point = new Point(0, 0);

        var exception = Record.Exception(() => Lazbot.Screen.GetColorAt(new (point.X, point.Y)));
        Assert.Null(exception);
    }

    #endregion
}
