// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz;

/// <summary>
/// A class for simulating keyboard actions.
/// </summary>
/// <remarks>
/// This class is designed to simulate separate primitive keyboard events. For typing an arbitrary text in one call,
/// use <see cref="Laz.Extensions.Keyboard.TypingExtension"/>, which is a turnkey algorithm for typing.
/// </remarks>
public class Keyboard
{
    internal Keyboard()
    {
    }

    /// <summary>
    /// Pauses execution for the specified duration, then returns the keyboard instance for chaining.
    /// Defaults to <see cref="Lazbot.DefaultDelay"/> when no value is provided.
    /// </summary>
    /// <param name="delay">The duration to wait. Omit to use <see cref="Lazbot.DefaultDelay"/>.</param>
    /// <returns>The keyboard instance for method chaining.</returns>
    public Keyboard Delay(TimeSpan? delay = null)
    {
        Delays.WaitFor(delay ?? Lazbot.DefaultDelay);
        return this;
    }

    /// <summary>
    /// Presses and holds a key until <see cref="KeyUp"/> is called for the same key.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The event is sent system-wide and will be received by whichever application currently has keyboard focus.
    /// Modifier keys held via <see cref="KeyDown"/> affect all subsequent input globally until released with
    /// <see cref="KeyUp"/>. Pressing a toggle key such as <see cref="Key.CapsLock"/> or <see cref="Key.NumLock"/>
    /// changes its global state until toggled again.
    /// </para>
    ///
    /// <para>
    /// If the key has no mapping on the current platform, the call is silently ignored.
    /// </para>
    ///
    /// <para>
    /// On macOS, the event is posted via Core Graphics and requires Accessibility permissions granted in
    /// System Settings. The operating system will prompt the user when this API is used for the first time.
    /// Without the permissions, the event is silently dropped.
    /// 
    /// Additionally, the physical Fn key state is always stripped from simulated events, so the key behavior
    /// is independent of whether the physical Fn key is pressed
    /// </para>
    ///
    /// <para>
    /// On Linux, keyboard events are simulated via the X server, therefore an active X11 or XWayland session is
    /// required.
    /// </para>
    /// </remarks>
    ///
    /// <param name="key">The key to press.</param>
    /// <returns>The keyboard instance for method chaining.</returns>
    public Keyboard KeyDown(Key key)
    {
        Native.NativeLazbot.KeyDown(key);
        return this;
    }

    /// <summary>
    /// Releases a previously pressed key.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Releasing a held modifier key such as <see cref="Key.Shift"/> or <see cref="Key.Control"/>
    /// restores normal input behavior for all subsequent keystrokes.
    /// </para>
    ///
    /// <para>
    /// If the key has no mapping on the current platform, the call is silently ignored.
    /// </para>
    ///
    /// <para>
    /// On macOS, the event is posted via Core Graphics and requires Accessibility permissions granted in
    /// System Settings. The operating system will prompt the user when this API is used for the first time.
    /// Without the permissions, the event is silently dropped.
    /// 
    /// Additionally, the physical Fn key state is always stripped from simulated events, so the key behavior
    /// is independent of whether the physical Fn key is pressed
    /// </para>
    ///
    /// <para>
    /// On Linux, keyboard events are simulated via the X server, therefore an active X11 or XWayland
    /// session is required.
    /// </para>
    /// </remarks>
    ///
    /// <param name="key">The key to release.</param>
    /// <returns>The keyboard instance for method chaining.</returns>
    public Keyboard KeyUp(Key key)
    {
        Native.NativeLazbot.KeyUp(key);
        return this;
    }

    /// <summary>
    /// Presses and releases a key, with a <see cref="Lazbot.DefaultDelay"/> pause in between.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Equivalent to:
    /// <code>
    /// keyboard.KeyDown(key).Delay().KeyUp(key);
    /// </code>
    /// </para>
    /// </remarks>
    ///
    /// <param name="key">The key to stroke.</param>
    /// <returns>The keyboard instance for method chaining.</returns>
    public Keyboard Stroke(Key key)
    {
        KeyDown(key).Delay().KeyUp(key);
        return this;
    }
}
