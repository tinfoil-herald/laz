// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Extensions.Keyboard;
using Xunit;

namespace Laz.Tests;

public class TypingExtensionTests
{
    private readonly Keyboard _keyboard = new Lazbot().Keyboard;

    [Fact]
    public void NullTextIsNoOp()
    {
        var result = _keyboard.Type(null!);
        Assert.Same(_keyboard, result);
    }

    [Fact]
    public void EmptyTextIsNoOp()
    {
        var result = _keyboard.Type("");
        Assert.Same(_keyboard, result);
    }
}
