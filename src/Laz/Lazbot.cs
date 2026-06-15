// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz;

/// <summary>
/// The entry point for all input simulation and screen capture.
/// Create one instance and access mouse, keyboard, and screen operations through its properties.
/// </summary>
public class Lazbot
{
    /// <summary>
    /// The default delay inserted between simulated input events.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// A delay between events is often necessary for the target application to correctly recognize the input.
    /// </para>
    ///
    /// <para>
    /// Defaults to 50 ms. Override by setting the <c>LAZ_DELAY_MS</c> environment variable before the process starts.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan DefaultDelay = ReadDefaultDelay() ?? TimeSpan.FromMilliseconds(50);
    
    /// <summary>Mouse simulation.</summary>
    public Mouse Mouse { get; }

    /// <summary>Keyboard simulation.</summary>
    public Keyboard Keyboard { get; }

    /// <summary>Screen capture.</summary>
    public Screen Screen { get; }

    /// <summary>
    /// Initializes a new <see cref="Lazbot"/> instance for the current platform.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current OS is not Windows, macOS, or Linux.
    /// </exception>
    public Lazbot()
    {
        Native.NativeLazbot nativeLazbot;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            nativeLazbot = new Native.MacLazbot();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            nativeLazbot = new Native.WinLazbot();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            nativeLazbot = new Native.LinuxLazbot();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        Mouse = new Mouse(nativeLazbot);
        Keyboard = new Keyboard();
        Screen = new Screen();
    }
    
    private static TimeSpan? ReadDefaultDelay()
    {
        var raw = Environment.GetEnvironmentVariable("LAZ_DELAY_MS");
        if (raw != null && int.TryParse(raw, out var ms) && ms >= 0)
            return TimeSpan.FromMilliseconds(ms);
        return null;
    }
}
