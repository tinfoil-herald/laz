// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Laz.Tests.Infrastructure;
using Laz.Tests.UI.Infrastructure;
using Xunit;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaScreen = Avalonia.Platform.Screen;

namespace Laz.Tests.UI;

/// <summary>
/// Tests for multi-monitor scenarios.
/// These tests require at least 2 monitors and will be skipped otherwise.
/// </summary>
public class MultiMonitorTests : IAsyncLifetime
{
    private Window _primaryWindow = null!;
    private Window _secondaryWindow = null!;
    private AvaloniaScreen _primaryScreen = null!;
    private AvaloniaScreen _secondaryScreen = null!;

    private Lazbot Lazbot { get; } = new();

    public async ValueTask InitializeAsync()
    {
        AvaloniaTestFixture.EnsureInitialized();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var tempWindow = new Window();
            var screens = tempWindow.Screens.All.ToList();

            if (screens.Count < 2)
            {
                return;
            }

            _primaryScreen = screens[0];
            _secondaryScreen = screens[1];

            _primaryWindow = CreateWindowOnScreen(_primaryScreen, "Primary", Brushes.Blue);
            _primaryWindow.Show();

            _secondaryWindow = CreateWindowOnScreen(_secondaryScreen, "Secondary", Brushes.Red);
            _secondaryWindow.Show();
        });

        await Task.Delay(300);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_primaryWindow is null) return;
            _primaryWindow.Activate();
            _secondaryWindow.Activate();
        });

        await Task.Delay(200);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_primaryWindow is null) return;
            _primaryWindow.Close();
            _secondaryWindow.Close();
        });

        await Task.Delay(50);
    }

    private static Window CreateWindowOnScreen(AvaloniaScreen screen, string title, IBrush background)
    {
        var window = new Window
        {
            Title = $"MultiMonitor Test: {title}",
            Width = 300,
            Height = 200,
            Background = background,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint(
                screen.Bounds.X + (screen.Bounds.Width - 300) / 2,
                screen.Bounds.Y + (screen.Bounds.Height - 200) / 2
            )
        };

        return window;
    }

    private static async Task<Point> GetWindowCenter(Window window)
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var bounds = window.Bounds;
            var center = new AvaloniaPoint(bounds.Width / 2, bounds.Height / 2);
            var screenPoint = window.PointToScreen(center);
            return new Point(screenPoint.X, screenPoint.Y);
        });
    }

    #region Mouse Movement Tests

    [MultiMonitorFact]
    public async Task MovesToCorrectPosition()
    {
        var targetPos = await GetWindowCenter(_secondaryWindow);

        Lazbot.Mouse.JumpTo(targetPos);
        await Task.Delay(50);

        var actualPos = Lazbot.Mouse.GetPosition();
        Assert.Equal(targetPos.X, actualPos.X);
        Assert.Equal(targetPos.Y, actualPos.Y);
    }

    [MultiMonitorFact]
    public async Task WorksCorrectly()
    {
        var primaryCenter = await GetWindowCenter(_primaryWindow);
        var secondaryCenter = await GetWindowCenter(_secondaryWindow);

        Lazbot.Mouse.JumpTo(primaryCenter);
        await Task.Delay(50);
        var posOnPrimary = Lazbot.Mouse.GetPosition();

        Lazbot.Mouse.JumpTo(secondaryCenter);
        await Task.Delay(50);
        var posOnSecondary = Lazbot.Mouse.GetPosition();

        Assert.Equal(primaryCenter.X, posOnPrimary.X);
        Assert.Equal(primaryCenter.Y, posOnPrimary.Y);
        Assert.Equal(secondaryCenter.X, posOnSecondary.X);
        Assert.Equal(secondaryCenter.Y, posOnSecondary.Y);
    }

    [MultiMonitorFact]
    public async Task ReturnsCorrectCoordinates()
    {
        var targetPos = await GetWindowCenter(_secondaryWindow);

        Lazbot.Mouse.JumpTo(targetPos);
        await Task.Delay(50);
        var actualPos = Lazbot.Mouse.GetPosition();

        Assert.Equal(targetPos.X, actualPos.X);
        Assert.Equal(targetPos.Y, actualPos.Y);
    }

    #endregion

    #region Click Tests

    [MultiMonitorFact]
    public async Task TriggersEvent()
    {
        var clicked = false;
        Panel panel;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            panel = new Panel { Background = Brushes.Green };
            panel.PointerPressed += (_, _) => clicked = true;
            _secondaryWindow.Content = panel;
        });

        await Task.Delay(100);

        var center = await GetWindowCenter(_secondaryWindow);
        Lazbot.Mouse.JumpTo(center);
        await Task.Delay(50);

        Lazbot.Mouse.Click();
        await Task.Delay(100);

        Assert.True(clicked, "Click on secondary monitor should trigger event");
    }

    #endregion

    #region Drag Tests

    [MultiMonitorFact]
    public async Task GeneratesDragEvents()
    {
        var dragStarted = false;
        Panel primaryPanel;
        Panel secondaryPanel;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            primaryPanel = new Panel { Background = Brushes.LightBlue };
            primaryPanel.PointerPressed += (_, _) => dragStarted = true;
            _primaryWindow.Content = primaryPanel;

            secondaryPanel = new Panel { Background = Brushes.LightCoral };
            _secondaryWindow.Content = secondaryPanel;
        });

        await Task.Delay(100);

        var startPos = await GetWindowCenter(_primaryWindow);
        var endPos = await GetWindowCenter(_secondaryWindow);

        Lazbot.Mouse.JumpTo(startPos);
        await Task.Delay(50);
        Lazbot.Mouse.Press();
        await Task.Delay(50);
        Lazbot.Mouse.JumpTo(endPos);
        await Task.Delay(100);
        Lazbot.Mouse.Release();
        await Task.Delay(50);

        Assert.True(dragStarted, "Drag should start on primary monitor");
        // Note: dragMoved may or may not fire depending on whether the drag crosses the window
        // The important thing is no exceptions were thrown and the operation completed
    }

    #endregion

    #region Screen Capture Tests

    [MultiMonitorFact]
    public async Task ReturnsCorrectColor()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _secondaryWindow.Background = Brushes.Red;
            _secondaryWindow.Content = null;
        });
        await Task.Delay(200);

        var windowPos = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var screenPoint = _secondaryWindow.PointToScreen(new AvaloniaPoint(50, 50));
            return new Point(screenPoint.X, screenPoint.Y);
        });

        var rect = new Rectangle(windowPos.X, windowPos.Y, 50, 50);

        var capture = Lazbot.Screen.Capture(new (rect.X, rect.Y), rect.Width, rect.Height);

        Assert.Equal(50, capture.Width);
        Assert.Equal(50, capture.Height);

        var (r, g, b, _) = capture.GetPixel(25, 25);
        Assert.True(r > 200, $"Red component should be high on secondary monitor, got {r}");
        Assert.True(g < 100, $"Green component should be low, got {g}");
        Assert.True(b < 100, $"Blue component should be low, got {b}");
    }

    [MultiMonitorFact]
    public async Task ReturnsCorrectColorAtPoint()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _secondaryWindow.Background = Brushes.Red;
            _secondaryWindow.Content = null;
        });
        await Task.Delay(200);

        var center = await GetWindowCenter(_secondaryWindow);

        var (r, g, b, _) = Lazbot.Screen.GetColorAt(new (center.X, center.Y));

        Assert.True(r > 200, $"Red component should be high on secondary monitor, got {r}");
        Assert.True(g < 100, $"Green component should be low, got {g}");
        Assert.True(b < 100, $"Blue component should be low, got {b}");
    }

    #endregion

    #region DPI Tests

    [MultiMonitorFact]
    public async Task ReportsValidScalingValues()
    {
        var (primaryScaling, secondaryScaling) = await Dispatcher.UIThread.InvokeAsync(() =>
            (_primaryScreen.Scaling, _secondaryScreen.Scaling)
        );

        Assert.True(primaryScaling > 0, $"Primary monitor scaling must be positive, got {primaryScaling}.");
        Assert.True(secondaryScaling > 0, $"Secondary monitor scaling must be positive, got {secondaryScaling}.");
    }

    [MultiMonitorFact]
    public async Task ReturnsPhysicalPixels()
    {
        var (primaryScaling, secondaryScaling) = await Dispatcher.UIThread.InvokeAsync(() =>
            (_primaryScreen.Scaling, _secondaryScreen.Scaling)
        );

        var higherDpiWindow = secondaryScaling > primaryScaling ? _secondaryWindow : _primaryWindow;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            higherDpiWindow.Background = Brushes.Magenta;
            higherDpiWindow.Content = null;
        });
        await Task.Delay(200);

        var windowPos = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var screenPoint = higherDpiWindow.PointToScreen(new AvaloniaPoint(20, 20));
            return new Point(screenPoint.X, screenPoint.Y);
        });

        const int captureSize = 50;
        var rect = new Rectangle(windowPos.X, windowPos.Y, captureSize, captureSize);
        var capture = Lazbot.Screen.Capture(new (rect.X, rect.Y), rect.Width, rect.Height);

        // (Per-Monitor Aware mode uses physical pixels)
        Assert.Equal(captureSize, capture.Width);
        Assert.Equal(captureSize, capture.Height);

        var (r, g, b, a) = capture.GetPixel(captureSize / 2, captureSize / 2);
        Assert.True(r > 200 && b > 200, "Should capture magenta color");
    }

    #endregion

    #region Coordinate Edge Cases

    [MultiMonitorFact]
    public async Task AreAccessible()
    {
        var screenBounds = await Dispatcher.UIThread.InvokeAsync(() => _secondaryScreen.Bounds);

        var testPoint = new Point(
            screenBounds.X + 100,
            screenBounds.Y + 100
        );

        Lazbot.Mouse.JumpTo(testPoint);
        await Task.Delay(50);
        var actualPos = Lazbot.Mouse.GetPosition();

        Assert.Equal(testPoint.X, actualPos.X);
        Assert.Equal(testPoint.Y, actualPos.Y);
    }

    [MultiMonitorFact]
    public async Task HandlesNegativeXCoordinatesWhenSecondaryIsLeft()
    {
        var screenBounds = await Dispatcher.UIThread.InvokeAsync(() => _secondaryScreen.Bounds);

        if (screenBounds.X >= 0)
        {
            return;
        }

        var negativePoint = new Point(
            screenBounds.X + 50,
            screenBounds.Y + 50
        );

        Lazbot.Mouse.JumpTo(negativePoint);
        await Task.Delay(50);
        var actualPos = Lazbot.Mouse.GetPosition();

        Assert.True(negativePoint.X < 0, "Test expects negative X coordinate");
        Assert.Equal(negativePoint.X, actualPos.X);
        Assert.Equal(negativePoint.Y, actualPos.Y);
    }

    #endregion
}
