// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Threading;
using Xunit;

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Fact attribute that skips the test if less than 2 monitors are available.
/// </summary>
public sealed class MultiMonitorFactAttribute : FactAttribute
{
    public MultiMonitorFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!HasMultipleMonitors())
        {
            Skip = "Test requires multiple monitors";
        }
    }

    private static bool HasMultipleMonitors()
    {
        try
        {
            AvaloniaTestFixture.EnsureInitialized();

            var screenCount = Dispatcher.UIThread.Invoke(() =>
            {
                var tempWindow = new Window();
                var count = tempWindow.Screens.All.Count;
                tempWindow.Close();
                return count;
            });

            return screenCount >= 2;
        }
        catch
        {
            return false;
        }
    }
}
