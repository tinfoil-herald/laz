// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Extensions.Mouse;
using Xunit;

namespace Laz.Tests;

public class MouseMoveExtensionTests
{
    private readonly Mouse _mouse = new Lazbot().Mouse;

    #region Duration Validation

    [Fact]
    public void ThrowsOnZeroDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _mouse.MoveTo(new Point(0, 0), new Point(100, 100), TimeSpan.Zero));
    }

    [Fact]
    public void ThrowsOnNegativeDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _mouse.MoveTo(new Point(0, 0), new Point(100, 100), TimeSpan.FromMilliseconds(-1)));
    }

    #endregion

    #region Non-Finite Start Coordinates

    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MinValue)]
    [InlineData(0, int.MaxValue)]
    public void ThrowsOnNonFiniteStart(int x, int y)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _mouse.MoveTo(new Point(x, y), new Point(100, 100), TimeSpan.FromSeconds(1)));
        Assert.Equal("start", ex.ParamName);
    }

    #endregion

    #region Non-Finite End Coordinates

    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MinValue)]
    [InlineData(0, int.MaxValue)]
    public void ThrowsOnNonFiniteEnd(int x, int y)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _mouse.MoveTo(new Point(0, 0), new Point(x, y), TimeSpan.FromSeconds(1)));
        Assert.Equal("end", ex.ParamName);
    }

    #endregion
}
