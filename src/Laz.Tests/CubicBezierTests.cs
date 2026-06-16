// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Extensions.Mouse;
using Xunit;

namespace Laz.Tests;

public class CubicBezierTests
{
    private const double Tolerance = 1e-10;

    [Fact]
    public void Evaluate_AtT0_ReturnsStartPoint()
    {
        var result = CubicBezier.Evaluate((0, 0), (10, 20), (30, 40), (100, 100), 0);

        Assert.Equal(0, result.x, Tolerance);
        Assert.Equal(0, result.y, Tolerance);
    }

    [Fact]
    public void Evaluate_AtT1_ReturnsEndPoint()
    {
        var result = CubicBezier.Evaluate((0, 0), (10, 20), (30, 40), (100, 100), 1);

        Assert.Equal(100, result.x, Tolerance);
        Assert.Equal(100, result.y, Tolerance);
    }

    [Fact]
    public void Evaluate_CollinearPoints_ProducesLinearInterpolation()
    {
        // All four points on the line y = x
        var result = CubicBezier.Evaluate((0, 0), (33.33, 33.33), (66.67, 66.67), (100, 100), 0.5);

        Assert.Equal(50, result.x, 0.1);
        Assert.Equal(50, result.y, 0.1);
    }

    [Fact]
    public void Evaluate_AtMidpoint_ReturnsExpectedValue()
    {
        // P0=(0,0), P1=(0,100), P2=(100,100), P3=(100,0)
        // At t=0.5: x = 0.125*0 + 0.375*0 + 0.375*100 + 0.125*100 = 50
        //           y = 0.125*0 + 0.375*100 + 0.375*100 + 0.125*0 = 75
        var result = CubicBezier.Evaluate((0, 0), (0, 100), (100, 100), (100, 0), 0.5);

        Assert.Equal(50, result.x, Tolerance);
        Assert.Equal(75, result.y, Tolerance);
    }

    [Fact]
    public void Evaluate_SymmetricCurve_XIsSymmetric()
    {
        // Symmetric control points: evaluating at t and 1-t should mirror in x
        var p0 = (0.0, 0.0);
        var p1 = (20.0, 80.0);
        var p2 = (80.0, 80.0);
        var p3 = (100.0, 0.0);

        var atT = CubicBezier.Evaluate(p0, p1, p2, p3, 0.25);
        var atOneMinusT = CubicBezier.Evaluate(p0, p1, p2, p3, 0.75);

        Assert.Equal(100, atT.x + atOneMinusT.x, Tolerance);
    }

    [Fact]
    public void Evaluate_NegativeCoordinates_WorksCorrectly()
    {
        var result = CubicBezier.Evaluate((-50, -50), (-20, -10), (20, 10), (50, 50), 0);

        Assert.Equal(-50, result.x, Tolerance);
        Assert.Equal(-50, result.y, Tolerance);

        var end = CubicBezier.Evaluate((-50, -50), (-20, -10), (20, 10), (50, 50), 1);

        Assert.Equal(50, end.x, Tolerance);
        Assert.Equal(50, end.y, Tolerance);
    }

    [Fact]
    public void Evaluate_AllSamePoint_ReturnsPoint()
    {
        var result = CubicBezier.Evaluate((42, 17), (42, 17), (42, 17), (42, 17), 0.73);

        Assert.Equal(42, result.x, Tolerance);
        Assert.Equal(17, result.y, Tolerance);
    }
}
