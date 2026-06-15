// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Xunit;
using AvaloniaPoint = Avalonia.Point;

namespace Laz.Tests.UI.Infrastructure;

#if WINDOWS
internal static partial class KeyboardStateNative
{
    private const int VK_CAPITAL = 0x14;

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int nVirtKey);

    /// <summary>
    /// Returns true if Caps Lock is currently toggled ON.
    /// </summary>
    public static bool IsCapsLockOn() => (GetKeyState(VK_CAPITAL) & 1) != 0;
}
#endif

/// <summary>
/// Base class for all robot tests. Provides window lifecycle management
/// and helper methods for testing robot interactions with UI.
/// </summary>
public abstract class RobotTestBase : IAsyncLifetime
{
    /// <summary>
    /// The test window. Created fresh for each test.
    /// </summary>
    protected Window TestWindow { get; private set; } = null!;

    /// <summary>
    /// The robot instance for this test.
    /// </summary>
    protected Lazbot Lazbot { get; } = new();

    /// <summary>
    /// Called before each test. Creates and shows a new window.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        AvaloniaTestFixture.EnsureInitialized();

        ResetModifierKeys();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TestWindow = CreateTestWindow();
            TestWindow.Show();
        });

        await Task.Delay(100);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TestWindow.Topmost = true;
            TestWindow.Activate();
            TestWindow.Focus();
        });

        await Task.Delay(200);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TestWindow.Topmost = false;
        });
    }

    /// <summary>
    /// Called after each test. Closes and disposes the window.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        ResetModifierKeys();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TestWindow.Close();
        });

        await Task.Delay(50);
    }

    /// <summary>
    /// Releases all modifier keys and resets toggle keys like CapsLock.
    /// This ensures a clean keyboard state regardless of how the test finished.
    /// </summary>
    private void ResetModifierKeys()
    {
        Lazbot.Keyboard.KeyUp(Key.Shift);
        Lazbot.Keyboard.KeyUp(Key.Control);
        Lazbot.Keyboard.KeyUp(Key.Alt);
        Lazbot.Keyboard.KeyUp(Key.RightShift);
        Lazbot.Keyboard.KeyUp(Key.RightControl);
        Lazbot.Keyboard.KeyUp(Key.RightAlt);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Lazbot.Keyboard.KeyUp(Key.Command);
        }

#if WINDOWS
        if (KeyboardStateNative.IsCapsLockOn())
        {
            Lazbot.Keyboard.Stroke(Key.CapsLock);
        }
#endif
    }

    /// <summary>
    /// Creates the test window. Override to customize window properties.
    /// </summary>
    private Window CreateTestWindow()
    {
        return new Window
        {
            Title = $"Test: {GetType().Name}",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
    }

    /// <summary>
    /// Executes an action on the Avalonia UI thread.
    /// </summary>
    protected static async Task OnUIThread(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <summary>
    /// Executes a function on the Avalonia UI thread and returns the result.
    /// </summary>
    protected static async Task<T> OnUIThread<T>(Func<T> func)
    {
        return await Dispatcher.UIThread.InvokeAsync(func);
    }

    /// <summary>
    /// Gets the screen position of a control's center point.
    /// Must be called from UI thread or use OnUIThread.
    /// </summary>
    protected static Point GetScreenCenter(Visual control)
    {
        var bounds = control.Bounds;
        var center = new AvaloniaPoint(bounds.Width / 2, bounds.Height / 2);
        var screenPoint = control.PointToScreen(center);
        return new Point(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Gets the screen position of a control's top-left corner.
    /// Must be called from UI thread or use OnUIThread.
    /// </summary>
    protected static Point GetScreenTopLeft(Visual control)
    {
        var screenPoint = control.PointToScreen(new AvaloniaPoint(0, 0));
        return new Point(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Returns a point 50px outside the window (bottom-right).
    /// Useful for tests that need the cursor to start outside a window without using (0,0).
    /// </summary>
    protected static Point GetPointOutsideWindow(Window window, int offset = 50)
    {
        // Window position is in physical pixels; bounds are DIPs, so convert to physical pixels.
        var pos = window.Position;
        var scale = window.RenderScaling;
        var width = (int)Math.Round(window.Bounds.Width * scale);
        var height = (int)Math.Round(window.Bounds.Height * scale);

        return new Point(pos.X + width + offset, pos.Y + height + offset);
    }

    /// <summary>
    /// Moves the robot cursor to the center of a control.
    /// </summary>
    protected async Task MoveToControl(Visual control)
    {
        var pos = await OnUIThread(() => GetScreenCenter(control));
        Lazbot.Mouse.JumpTo(pos);
        await Task.Delay(50); // Allow UI to process
    }

    /// <summary>
    /// Waits for a control to be initialized and laid out.
    /// </summary>
    protected static async Task WaitForControlReady(Control control, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(2);
        var tcs = new TaskCompletionSource();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (control.IsLoaded)
            {
                tcs.TrySetResult();
                return;
            }

            control.Loaded += OnLoaded;
            return;

            void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                control.Loaded -= OnLoaded;
                tcs.TrySetResult();
            }
        });

        using var cts = new CancellationTokenSource(timeout.Value);
        await tcs.Task.WaitAsync(cts.Token);

        await Task.Delay(100, cts.Token);
    }
}
