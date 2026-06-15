// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Extensions.Mouse;
using Xunit;

namespace Laz.Tests;

public class EasingFunctionTests
{
    #region Boundary: f(0) == 0

    [Theory]
    [InlineData(MoveEasingFunction.Linear)]
    [InlineData(MoveEasingFunction.EaseInQuad)]
    [InlineData(MoveEasingFunction.EaseInOutQuad)]
    [InlineData(MoveEasingFunction.EaseOutCubic)]
    [InlineData(MoveEasingFunction.EaseInOutCubic)]
    [InlineData(MoveEasingFunction.EaseOutQuartic)]
    [InlineData(MoveEasingFunction.EaseOutExpo)]
    [InlineData(MoveEasingFunction.EaseInOutSine)]
    [InlineData(MoveEasingFunction.EaseOutCircular)]
    [InlineData(MoveEasingFunction.EaseOutElastic)]
    [InlineData(MoveEasingFunction.EaseOutBounce)]
    public void AtZeroReturnsZero(MoveEasingFunction fn)
    {
        Assert.Equal(0.0, EasingFunctions.Evaluate(fn, 0.0), precision: 10);
    }

    #endregion

    #region Boundary: f(1) == 1

    [Theory]
    [InlineData(MoveEasingFunction.Linear)]
    [InlineData(MoveEasingFunction.EaseInQuad)]
    [InlineData(MoveEasingFunction.EaseInOutQuad)]
    [InlineData(MoveEasingFunction.EaseOutCubic)]
    [InlineData(MoveEasingFunction.EaseInOutCubic)]
    [InlineData(MoveEasingFunction.EaseOutQuartic)]
    [InlineData(MoveEasingFunction.EaseOutExpo)]
    [InlineData(MoveEasingFunction.EaseInOutSine)]
    [InlineData(MoveEasingFunction.EaseOutCircular)]
    [InlineData(MoveEasingFunction.EaseOutElastic)]
    [InlineData(MoveEasingFunction.EaseOutBounce)]
    public void AtOneReturnsOne(MoveEasingFunction fn)
    {
        Assert.Equal(1.0, EasingFunctions.Evaluate(fn, 1.0), precision: 10);
    }

    #endregion

    #region Clamping: t outside [0, 1] is clamped

    [Theory]
    [InlineData(MoveEasingFunction.Linear)]
    [InlineData(MoveEasingFunction.EaseInQuad)]
    [InlineData(MoveEasingFunction.EaseInOutQuad)]
    [InlineData(MoveEasingFunction.EaseOutCubic)]
    [InlineData(MoveEasingFunction.EaseInOutCubic)]
    [InlineData(MoveEasingFunction.EaseOutQuartic)]
    [InlineData(MoveEasingFunction.EaseOutExpo)]
    [InlineData(MoveEasingFunction.EaseInOutSine)]
    [InlineData(MoveEasingFunction.EaseOutCircular)]
    [InlineData(MoveEasingFunction.EaseOutElastic)]
    [InlineData(MoveEasingFunction.EaseOutBounce)]
    public void NegativeTClampedToZero(MoveEasingFunction fn)
    {
        Assert.Equal(
            EasingFunctions.Evaluate(fn, 0.0),
            EasingFunctions.Evaluate(fn, -1.0),
            precision: 10);
    }

    [Theory]
    [InlineData(MoveEasingFunction.Linear)]
    [InlineData(MoveEasingFunction.EaseInQuad)]
    [InlineData(MoveEasingFunction.EaseInOutQuad)]
    [InlineData(MoveEasingFunction.EaseOutCubic)]
    [InlineData(MoveEasingFunction.EaseInOutCubic)]
    [InlineData(MoveEasingFunction.EaseOutQuartic)]
    [InlineData(MoveEasingFunction.EaseOutExpo)]
    [InlineData(MoveEasingFunction.EaseInOutSine)]
    [InlineData(MoveEasingFunction.EaseOutCircular)]
    [InlineData(MoveEasingFunction.EaseOutElastic)]
    [InlineData(MoveEasingFunction.EaseOutBounce)]
    public void OverOneClampedToOne(MoveEasingFunction fn)
    {
        Assert.Equal(
            EasingFunctions.Evaluate(fn, 1.0),
            EasingFunctions.Evaluate(fn, 2.0),
            precision: 10);
    }

    #endregion

    #region Mid-point ordering: monotonic variants

    // EaseIn accelerates - less than half the distance covered at the halfway point.
    [Theory]
    [InlineData(MoveEasingFunction.EaseInQuad)]
    public void EaseInIsBelowHalfAtMidpoint(MoveEasingFunction fn)
    {
        Assert.True(EasingFunctions.Evaluate(fn, 0.5) < 0.5);
    }

    // EaseOut decelerates - more than half the distance covered at the halfway point.
    [Theory]
    [InlineData(MoveEasingFunction.EaseOutCubic)]
    [InlineData(MoveEasingFunction.EaseOutQuartic)]
    [InlineData(MoveEasingFunction.EaseOutExpo)]
    [InlineData(MoveEasingFunction.EaseOutCircular)]
    public void EaseOutIsAboveHalfAtMidpoint(MoveEasingFunction fn)
    {
        Assert.True(EasingFunctions.Evaluate(fn, 0.5) > 0.5);
    }

    // EaseInOut and Linear are symmetric - exactly half the distance at the halfway point.
    [Theory]
    [InlineData(MoveEasingFunction.Linear)]
    [InlineData(MoveEasingFunction.EaseInOutQuad)]
    [InlineData(MoveEasingFunction.EaseInOutCubic)]
    [InlineData(MoveEasingFunction.EaseInOutSine)]
    public void SymmetricVariantsAreAtHalfAtMidpoint(MoveEasingFunction fn)
    {
        Assert.Equal(0.5, EasingFunctions.Evaluate(fn, 0.5), precision: 10);
    }

    #endregion

    #region Non-monotonic variant properties

    // EaseOutElastic overshoots - the curve exceeds 1.0 during motion before settling.
    [Fact]
    public void ElasticOvershoots()
    {
        var peak = Enumerable.Range(1, 99)
            .Select(i => EasingFunctions.Evaluate(MoveEasingFunction.EaseOutElastic, i / 100.0))
            .Max();
        Assert.True(peak > 1.0, $"EaseOutElastic should overshoot 1.0, peak was {peak}");
    }

    // EaseOutBounce stays non-negative throughout - the mouse never moves behind the start point.
    [Fact]
    public void BounceIsNeverNegative()
    {
        var floor = Enumerable.Range(0, 101)
            .Select(i => EasingFunctions.Evaluate(MoveEasingFunction.EaseOutBounce, i / 100.0))
            .Min();
        Assert.True(floor >= 0.0, $"EaseOutBounce should never go below 0, floor was {floor}");
    }

    #endregion
}
