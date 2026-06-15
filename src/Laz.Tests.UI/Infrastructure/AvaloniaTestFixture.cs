// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Manages Avalonia application lifecycle for tests.
/// Uses real windows - required for robot input testing.
///
/// IMPORTANT: On macOS, tests must be run via the custom test runner (dotnet run)
/// because macOS requires GUI operations on the main thread.
/// </summary>
public static class AvaloniaTestFixture
{
    private static readonly object Lock = new();
    private static bool _initialized;
    private static CancellationTokenSource? _cts;
    private static Thread? _uiThread;
    private static TaskCompletionSource? _uiReady;

    /// <summary>
    /// Ensures Avalonia is initialized. Safe to call multiple times.
    /// Must be called from the main thread on macOS.
    /// </summary>
    public static void EnsureInitialized()
    {
        lock (Lock)
        {
            if (_initialized) return;

            _cts = new CancellationTokenSource();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DpiConfiguration.Initialize();
                AppBuilder.Configure<AvaloniaTestApp>()
                    .UsePlatformDetect()
                    .SetupWithoutStarting();
                _initialized = true;
                return;
            }

            _uiReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _uiThread = new Thread(() =>
            {
                try
                {
                    DpiConfiguration.Initialize();
                    AppBuilder.Configure<AvaloniaTestApp>()
                        .UsePlatformDetect()
                        .SetupWithoutStarting();
                    _uiReady!.SetResult();
                    Dispatcher.UIThread.MainLoop(_cts!.Token);
                }
                catch (Exception ex)
                {
                    _uiReady!.SetException(ex);
                }
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _uiThread.SetApartmentState(ApartmentState.STA);
            }

            _uiThread.IsBackground = true;
            _uiThread.Start();
        }

        _uiReady.Task.GetAwaiter().GetResult();

        lock (Lock)
        {
            _initialized = true;
        }
    }
    
    /// <summary>
    /// Runs the Avalonia dispatcher loop. Call this from the main thread
    /// after all tests have been queued.
    /// </summary>
    public static void RunMainLoop()
    {
        Dispatcher.UIThread.MainLoop(_cts!.Token);
    }

    /// <summary>
    /// Stops the main loop.
    /// </summary>
    public static void StopMainLoop()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// Shuts down Avalonia.
    /// </summary>
    public static void Shutdown()
    {
        lock (Lock)
        {
            if (!_initialized) return;

            _cts?.Cancel();
            _uiThread?.Join(TimeSpan.FromSeconds(2));
            _cts?.Dispose();
            _cts = null;
            _uiThread = null;
            _uiReady = null;
            _initialized = false;
        }
    }
}
