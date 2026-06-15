// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Themes.Fluent;

namespace Laz.Tests.UI.Infrastructure;

/// <summary>
/// Minimal Avalonia Application for running tests with real windows.
/// </summary>
public class AvaloniaTestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
