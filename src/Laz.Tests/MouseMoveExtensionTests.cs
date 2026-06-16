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

    #region LinearInterpolate

    [Fact]
    public void LinearInterpolateAtT0ReturnsStart()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(10, 20), new Point(100, 200), 0);

        Assert.Equal(10, result.x);
        Assert.Equal(20, result.y);
    }

    [Fact]
    public void LinearInterpolateAtT1ReturnsEnd()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(10, 20), new Point(100, 200), 1);

        Assert.Equal(100, result.x);
        Assert.Equal(200, result.y);
    }

    [Fact]
    public void LinearInterpolateAtMidpointReturnsAverage()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(0, 0), new Point(100, 200), 0.5);

        Assert.Equal(50, result.x);
        Assert.Equal(100, result.y);
    }

    [Fact]
    public void LinearInterpolateAtQuarterReturnsCorrectValue()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(0, 0), new Point(200, 400), 0.25);

        Assert.Equal(50, result.x);
        Assert.Equal(100, result.y);
    }

    [Fact]
    public void LinearInterpolateSameStartAndEndReturnsPoint()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(42, 17), new Point(42, 17), 0.73);

        Assert.Equal(42, result.x);
        Assert.Equal(17, result.y);
    }

    [Fact]
    public void LinearInterpolateNegativeCoordinates()
    {
        var result = MouseMoveExtension.LinearInterpolate(new Point(-100, -200), new Point(100, 200), 0.5);

        Assert.Equal(0, result.x);
        Assert.Equal(0, result.y);
    }

    #endregion

    #region HasMovedSignificantly

    [Fact]
    public void HasMovedSignificantlyNoMovementReturnsFalse()
    {
        Assert.False(MouseMoveExtension.HasMovedSignificantly((10, 20), (10, 20)));
    }

    [Fact]
    public void HasMovedSignificantlySubPixelReturnsFalse()
    {
        Assert.False(MouseMoveExtension.HasMovedSignificantly((10, 20), (10.5, 20.5)));
    }

    [Fact]
    public void HasMovedSignificantlyExactlyOnePixelReturnsTrue()
    {
        Assert.True(MouseMoveExtension.HasMovedSignificantly((10, 20), (11, 20)));
    }

    [Fact]
    public void HasMovedSignificantlyLargeMovementReturnsTrue()
    {
        Assert.True(MouseMoveExtension.HasMovedSignificantly((0, 0), (100, 100)));
    }

    [Fact]
    public void HasMovedSignificantlyDiagonalSubPixelReturnsFalse()
    {
        // sqrt(0.6^2 + 0.6^2) = ~0.85 < 1
        Assert.False(MouseMoveExtension.HasMovedSignificantly((0, 0), (0.6, 0.6)));
    }

    [Fact]
    public void HasMovedSignificantlyDiagonalJustOverThresholdReturnsTrue()
    {
        // sqrt(0.8^2 + 0.8^2) = ~1.13 >= 1 (squared distance = 1.28 >= 1.0)
        Assert.True(MouseMoveExtension.HasMovedSignificantly((0, 0), (0.8, 0.8)));
    }

    #endregion

    #region RoundToInt

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.4, 0)]
    [InlineData(0.5, 0)]   // banker's rounding: 0.5 rounds to nearest even
    [InlineData(0.6, 1)]
    [InlineData(1.5, 2)]   // banker's rounding: 1.5 rounds to 2
    [InlineData(-0.4, 0)]
    [InlineData(-0.6, -1)]
    [InlineData(99.9, 100)]
    public void RoundToIntReturnsExpectedValue(double input, int expected)
    {
        Assert.Equal(expected, MouseMoveExtension.RoundToInt(input));
    }

    #endregion

    #region MoveTo Invalid MoveFunction

    [Fact]
    public void ThrowsOnInvalidMoveFunction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _mouse.MoveTo(new Point(0, 0), new Point(100, 100), TimeSpan.FromSeconds(1),
                moveFunction: (MouseMoveFunction)999));
    }

    #endregion
}
