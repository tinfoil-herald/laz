// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Configures Windows DPI awareness for test runs.
/// Must be called before any UI initialization.
///
/// Usage: Set LAZ_DPI_MODE environment variable or use runsettings:
///   dotnet test --settings dpi-unaware.runsettings
///   dotnet test --settings dpi-permonitor.runsettings
/// </summary>
public static partial class DpiConfiguration
{
    private const IntPtr DpiAwarenessContextUnaware = -1;
    private const IntPtr DpiAwarenessContextSystemAware = -2;
    private const IntPtr DpiAwarenessContextPerMonitorAwareV2 = -4;

    [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void SetProcessDpiAwarenessContext(IntPtr value);

    private static bool _initialized;

    /// <summary>
    /// Initializes DPI awareness based on LAZ_DPI_MODE environment variable.
    /// Must be called before any windows are created.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var mode = CurrentMode;
        var context = mode switch
        {
            "Unaware" => DpiAwarenessContextUnaware,
            "System" => DpiAwarenessContextSystemAware,
            _ => DpiAwarenessContextPerMonitorAwareV2
        };

        SetProcessDpiAwarenessContext(context);
    }

    /// <summary>
    /// Gets the current DPI mode from environment variable.
    /// Defaults to PerMonitorV2.
    /// </summary>
    private static string CurrentMode =>
        Environment.GetEnvironmentVariable("LAZ_DPI_MODE") ?? "PerMonitorV2";
}
