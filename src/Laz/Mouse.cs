// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Laz;

/// <summary>
/// A class for simulating mouse actions.
/// </summary>
/// <remarks>
/// This class is designed to simulate separate primitive mouse events. For realistic mouse movements and
/// drag-and-drop operations, use <see cref="Laz.Extensions.Mouse.MouseMoveExtension"/>, which support
/// configurable easing and natural Bezier-curve paths.
/// </remarks>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API reserved for potential future stateful extensions.")]
public class Mouse
{
    private readonly Native.NativeLazbot _nativeLazbot;

    internal Mouse(Native.NativeLazbot nativeLazbot)
    {
        _nativeLazbot = nativeLazbot;
    }

    /// <summary>
    /// Pauses execution for the specified duration, then returns the mouse instance for chaining.
    /// Defaults to <see cref="Lazbot.DefaultDelay"/> when no value is provided.
    /// </summary>
    /// <param name="delay">The duration to wait. Omit to use <see cref="Lazbot.DefaultDelay"/>.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse Delay(TimeSpan? delay = null)
    {
        Delays.WaitFor(delay ?? Lazbot.DefaultDelay);
        return this;
    }

    /// <summary>
    /// Returns the current position of the mouse pointer.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On Windows, the coordinate space of the returned position depends on the DPI awareness
    /// of the current process.
    /// </para>
    ///
    /// <para>
    /// On macOS, the operating system handles scaling transparently, so the returned
    /// coordinates are consistent with other operations regardless of display resolution.
    /// </para>
    ///
    /// <para>
    /// On Linux, coordinates are returned via the X server in X11 logical pixels. In HiDPI
    /// configurations, the values can vary depending on the environment configuration, fractional scaling settings,
    /// and specific Wayland compositor in XWayland environment.
    /// </para>
    /// </remarks>
    ///
    /// <returns>The current cursor position.</returns>
    public Point GetPosition()
    {
        return Native.NativeLazbot.GetMousePosition();
    }

    /// <summary>
    /// Presses and holds a mouse button until <see cref="Release"/> is called.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On Windows, <see cref="MouseButton.Primary"/> and <see cref="MouseButton.Secondary"/> respect the
    /// system button swap setting, so <see cref="MouseButton.Primary"/> always maps to the button the user
    /// has configured as primary, regardless of which physical button that is.
    /// </para>
    ///
    /// <para>
    /// On macOS, the event requires Accessibility permissions granted in System Settings. The operating
    /// system will prompt the user when this API is used for the first time. Without the permissions,
    /// the event is silently dropped.
    /// </para>
    ///
    /// <para>
    /// On Linux, mouse events are simulated via the X server, therefore an active X11 or XWayland
    /// session is required.
    /// </para>
    /// </remarks>
    ///
    /// <param name="button">The button to press. Defaults to <see cref="MouseButton.Primary"/>.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse Press(MouseButton button = MouseButton.Primary)
    {
        _nativeLazbot.MouseDown(button);
        return this;
    }

    /// <summary>
    /// Releases a previously pressed mouse button.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On Windows, <see cref="MouseButton.Primary"/> and <see cref="MouseButton.Secondary"/> respect the
    /// system button swap setting, so <see cref="MouseButton.Primary"/> always maps to the button the user
    /// has configured as primary, regardless of which physical button that is.
    /// </para>
    ///
    /// <para>
    /// On macOS, the event requires Accessibility permissions granted in System Settings. The operating
    /// system will prompt the user when this API is used for the first time. Without the permissions,
    /// the event is silently dropped.
    /// </para>
    ///
    /// <para>
    /// On Linux, mouse events are simulated via the X server, therefore an active X11 or XWayland
    /// session is required.
    /// </para>
    /// </remarks>
    ///
    /// <param name="button">The button to release. Defaults to <see cref="MouseButton.Primary"/>.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse Release(MouseButton button = MouseButton.Primary)
    {
        _nativeLazbot.MouseUp(button);
        return this;
    }

    /// <summary>
    /// Instantly moves the mouse pointer to the specified position.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On Windows, the coordinate space depends on the DPI awareness of the current process.
    /// Coordinates span the full virtual desktop across all connected displays and can be negative
    /// when a monitor is positioned to the left or above the primary display.
    /// </para>
    ///
    /// <para>
    /// On macOS, the operating system handles scaling transparently, so the coordinate space matches what is used
    /// in other operations.
    /// </para>
    ///
    /// <para>
    /// On Linux, coordinates are interpreted by the X server as X11 logical pixels. In HiDPI
    /// configurations, the behavior can vary depending on the environment, Wayland compositor in case of XWayland
    /// sessions, and fractional scaling settings.
    /// </para>
    /// </remarks>
    ///
    /// <param name="point">The target position.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse JumpTo(Point point)
    {
        _nativeLazbot.JumpTo(point.X, point.Y);
        return this;
    }

    /// <summary>
    /// Scrolls the mouse wheel by the given number of notches.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// A notch is one detent, or click, of the scroll wheel. Positive values scroll up,
    /// negative values scroll down.
    /// </para>
    ///
    /// <para>
    /// On Windows, each notch is sent as 120 scroll units. The number of lines scrolled per notch is determined by the
    /// system scroll speed setting in Windows mouse settings and the receiving application.
    /// </para>
    ///
    /// <para>
    /// On macOS, each notch scrolls 3 logical lines. The event requires Accessibility permissions granted in
    /// System Settings. The operating system will prompt the user when this API is used for the first
    /// time. Without the permissions, the event is silently dropped.
    /// </para>
    ///
    /// <para>
    /// On Linux, each notch is simulated as one X11 button event via the X server (button 4 for up,
    /// button 5 for down). The scroll amount per event is determined by the receiving application.
    /// </para>
    /// </remarks>
    ///
    /// <param name="notches">The number of notches to scroll. Positive values scroll up, negative scroll down.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse Scroll(int notches)
    {
        Native.NativeLazbot.MouseScroll(notches);
        return this;
    }

    /// <summary>
    /// Presses and immediately releases a mouse button, with no delay in between.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Equivalent to:
    /// <code>
    /// mouse.Press(button).Release(button);
    /// </code>
    /// </para>
    /// </remarks>
    ///
    /// <param name="button">The button to click. Defaults to <see cref="MouseButton.Primary"/>.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse Click(MouseButton button = MouseButton.Primary)
    {
        return Press(button).Release(button);
    }

    /// <summary>
    /// Double-clicks a mouse button by doing to consecutive clicks.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// An equivalent to:
    /// <code>
    /// mouse.Click(button).Click(button);
    /// </code>
    /// </para>
    /// </remarks>
    /// 
    /// <param name="button">The button to double-click. Defaults to <see cref="MouseButton.Primary"/>.</param>
    /// <returns>The mouse instance for method chaining.</returns>
    public Mouse DoubleClick(MouseButton button = MouseButton.Primary)
    {
        return Click(button).Click(button);
    }
}
