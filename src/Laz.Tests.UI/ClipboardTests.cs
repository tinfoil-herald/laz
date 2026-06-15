// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Laz.Native;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

public class ClipboardTests : RobotTestBase
{
    private Task<string?> ReadClipboardText() =>
        OnUIThread(() => TopLevel.GetTopLevel(TestWindow)?.Clipboard?.GetTextAsync() ?? Task.FromResult<string?>(null))
            .Unwrap();

    [Fact, SkipOn(Platform.Linux, "Clipboard is not supported on Linux")]
    public void IsSupportedOnCurrentPlatform()
    {
        Assert.True(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    }

    [Fact, SkipOn(Platform.Linux, "Clipboard is not supported on Linux")]
    public async Task SetsClipboardText()
    {
        const string expected = "hello clipboard";

        await Task.Run(() => Clipboard.SetText(expected), TestContext.Current.CancellationToken);

        var actual = await ReadClipboardText();
        Assert.Equal(expected, actual);
    }

    [Fact, SkipOn(Platform.Linux, "Clipboard is not supported on Linux")]
    public async Task OverwritesPreviousContent()
    {
        await Task.Run(() => Clipboard.SetText("first"), TestContext.Current.CancellationToken);
        await Task.Run(() => Clipboard.SetText("second"), TestContext.Current.CancellationToken);

        var actual = await ReadClipboardText();
        Assert.Equal("second", actual);
    }

    [Fact, SkipOn(Platform.Linux, "Clipboard is not supported on Linux")]
    public async Task SetsEmptyString()
    {
        await Task.Run(() => Clipboard.SetText("non-empty"), TestContext.Current.CancellationToken);
        await Task.Run(() => Clipboard.SetText(""), TestContext.Current.CancellationToken);

        var actual = await ReadClipboardText();
        Assert.Equal("", actual);
    }

    [Fact, SkipOn(Platform.Linux, "Clipboard is not supported on Linux")]
    public async Task SetsUnicodeText()
    {
        const string expected = "Hello, 世界! 🎉";

        await Task.Run(() => Clipboard.SetText(expected), TestContext.Current.CancellationToken);

        var actual = await ReadClipboardText();
        Assert.Equal(expected, actual);
    }
}
