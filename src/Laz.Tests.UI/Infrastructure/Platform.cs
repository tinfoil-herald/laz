// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Flags representing target platforms for tests.
/// </summary>
[Flags]
public enum Platform
{
    Windows = 1,
    MacOs = 2,
    LinuxX11 = 4,
    LinuxWayland = 8,
    LinuxMixed = 16,  // XWayland: Wayland compositor with X11 compatibility

    // Convenience combination for any Linux variant
    Linux = LinuxX11 | LinuxWayland | LinuxMixed
}

/// <summary>
/// Extension methods for Platform enum to handle platform detection.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Gets the current platform based on OS and environment variables.
    /// </summary>
    private static Platform GetCurrent()
    {
        if (OperatingSystem.IsWindows()) return Platform.Windows;
        if (OperatingSystem.IsMacOS()) return Platform.MacOs;
        return OperatingSystem.IsLinux() ? GetLinuxPlatform() : 0;
    }

    /// <summary>
    /// Checks if the specified platform flags include the current platform.
    /// </summary>
    public static bool IncludesCurrent(this Platform platforms)
    {
        return (platforms & GetCurrent()) != 0;
    }

    private static Platform GetLinuxPlatform()
    {
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var x11Display = Environment.GetEnvironmentVariable("DISPLAY");

        // Explicit session type takes precedence
        if (string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase))
        {
            // Wayland session, but check if X11 is also available (XWayland)
            return !string.IsNullOrEmpty(x11Display) ? Platform.LinuxMixed : Platform.LinuxWayland;
        }

        if (string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase))
        {
            return Platform.LinuxX11;
        }

        // Fallback: check environment variables
        if (!string.IsNullOrEmpty(waylandDisplay))
        {
            return !string.IsNullOrEmpty(x11Display) ? Platform.LinuxMixed : Platform.LinuxWayland;
        }

        return Platform.LinuxX11;
    }
}
