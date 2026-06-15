// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz.Extensions.Mouse;

/// <summary>
/// Bezier functions used for realistic mouse movements.
/// </summary>
internal static class CubicBezier
{
    private static readonly Random Rng = new();

    internal static (double x, double y) Evaluate(
        (double x, double y) p0,
        (double x, double y) p1,
        (double x, double y) p2,
        (double x, double y) p3,
        double t)
    {
        var oneMinusT = 1 - t;
        var oneMinusT2 = oneMinusT * oneMinusT;
        var oneMinusT3 = oneMinusT2 * oneMinusT;
        var t2 = t * t;
        var t3 = t2 * t;

        return (
            x: oneMinusT3 * p0.x + 3 * oneMinusT2 * t * p1.x + 3 * oneMinusT * t2 * p2.x + t3 * p3.x,
            y: oneMinusT3 * p0.y + 3 * oneMinusT2 * t * p1.y + 3 * oneMinusT * t2 * p2.y + t3 * p3.y
        );
    }

    internal static ((double x, double y) cp1, (double x, double y) cp2) ComputeControlPoints(
        (double x, double y) start,
        (double x, double y) end)
    {
        var dx = end.x - start.x;
        var dy = end.y - start.y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 1)
            return (start, end);

        var forwardX = dx / distance;
        var forwardY = dy / distance;

        var lateralX = -forwardY;
        var lateralY = forwardX;

        var forwardOffset1 = RandomInRange(0.20, 0.35) * distance;
        var forwardOffset2 = RandomInRange(0.20, 0.35) * distance;

        var lateralSign = Rng.NextDouble() < 0.5 ? -1.0 : 1.0;
        var lateralMagnitude1 = RandomInRange(0.05, 0.20) * distance * lateralSign;
        var lateralMagnitude2 = RandomInRange(0.05, 0.20) * distance * lateralSign;

        var cp1 = (
            x: start.x + forwardX * forwardOffset1 + lateralX * lateralMagnitude1,
            y: start.y + forwardY * forwardOffset1 + lateralY * lateralMagnitude1
        );

        var cp2 = (
            x: end.x - forwardX * forwardOffset2 + lateralX * lateralMagnitude2,
            y: end.y - forwardY * forwardOffset2 + lateralY * lateralMagnitude2
        );

        return (cp1, cp2);
    }

    private static double RandomInRange(double min, double max)
    {
        return min + Rng.NextDouble() * (max - min);
    }
}
