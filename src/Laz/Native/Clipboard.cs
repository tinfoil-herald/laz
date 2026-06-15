// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Laz.Native;

/// <summary>
/// Cross-platform clipboard operations.
/// Supported on Windows and macOS only.
/// </summary>
internal static class Clipboard
{
    /// <summary>
    /// Sets the clipboard text content.
    /// </summary>
    /// <param name="text">The text to copy to clipboard.</param>
    public static void SetText(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException();

        NativeLazbot.SetClipboardText(text);
    }
}
