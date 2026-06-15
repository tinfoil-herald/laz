// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz.Extensions.Mouse;

/// <summary>
/// Easing functions used for realistic mouse movements.
/// </summary>
public static class EasingFunctions
{
    /// <summary>
    /// Evaluates an easing function at a given progress value.
    /// </summary>
    /// <param name="easing">The easing function to apply.</param>
    /// <param name="t">
    /// The normalized progress in the range [0, 1], where 0 is the start and 1 is the end.
    /// Values outside this range are clamped.
    /// </param>
    /// <returns>The eased output value in the range [0, 1].</returns>
    public static double Evaluate(MoveEasingFunction easing, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return easing switch
        {
            MoveEasingFunction.Linear => t,
            MoveEasingFunction.EaseInQuad => EaseInQuad(t),
            MoveEasingFunction.EaseInOutQuad => EaseInOutQuad(t),
            MoveEasingFunction.EaseOutCubic => EaseOutCubic(t),
            MoveEasingFunction.EaseInOutCubic => EaseInOutCubic(t),
            MoveEasingFunction.EaseOutQuartic => EaseOutQuartic(t),
            MoveEasingFunction.EaseOutExpo => EaseOutExpo(t),
            MoveEasingFunction.EaseInOutSine => EaseInOutSine(t),
            MoveEasingFunction.EaseOutCircular => EaseOutCircular(t),
            MoveEasingFunction.EaseOutElastic => EaseOutElastic(t),
            MoveEasingFunction.EaseOutBounce => EaseOutBounce(t),
            _ => t
        };
    }

    private static double EaseInQuad(double t)
    {
        return Math.Pow(t, 2);
    }

    private static double EaseInOutQuad(double t)
    {
        if (t < 0.5)
            return 2 * Math.Pow(t, 2);

        var tMirrored = 2 - 2 * t;
        return 1 - Math.Pow(tMirrored, 2) / 2;
    }

    private static double EaseOutCubic(double t)
    {
        var remaining = 1 - t;
        return 1 - Math.Pow(remaining, 3);
    }

    private static double EaseInOutCubic(double t)
    {
        if (t < 0.5)
            return 4 * Math.Pow(t, 3);

        var tMirrored = 2 - 2 * t;
        return 1 - Math.Pow(tMirrored, 3) / 2;
    }

    private static double EaseOutQuartic(double t)
    {
        var remaining = 1 - t;
        return 1 - Math.Pow(remaining, 4);
    }

    private static double EaseOutExpo(double t)
    {
        if (t >= 1.0)
            return 1.0;

        const double decayRate = 10; // 2^(-10) ~= 0.001; curve reaches 99.9% at t=1
        return 1 - Math.Pow(2, -decayRate * t);
    }

    private static double EaseInOutSine(double t)
    {
        var cosine = Math.Cos(Math.PI * t);
        return -(cosine - 1) / 2;
    }

    private static double EaseOutCircular(double t)
    {
        var remaining = 1 - t;
        return Math.Sqrt(1 - Math.Pow(remaining, 2));
    }

    private static double EaseOutElastic(double t)
    {
        if (t <= 0.0) return 0.0;
        if (t >= 1.0) return 1.0;

        const double decayRate = 10; // 2^(-10) ~= 0.001; envelope reaches near-zero by t=1
        const double period = 2 * Math.PI / 3; // one full oscillation every 3/decayRate = 0.3 units of t
        const double phaseShift = Math.PI / 2 / period; // shifts sine so f(0) = 0

        var envelope = Math.Pow(2, -decayRate * t);
        var oscillation = Math.Sin((t * decayRate - phaseShift) * period);
        return envelope * oscillation + 1;
    }

    private static double EaseOutBounce(double t)
    {
        const double divisor = 2.75; // Arbitrary segment width ratio (1:1:0.5:0.25) that worked fine in tests.
        var coefficient = Math.Pow(divisor, 2); // Makes the first arc reach y=1 at t=1/divisor.

        const double bounce2Floor = 1 - 1.0 / 4;   // 1 - (1/2)^2
        const double bounce3Floor = 1 - 1.0 / 16;  // 1 - (1/4)^2
        const double bounce4Floor = 1 - 1.0 / 64;  // 1 - (1/8)^2

        if (t < 1.0 / divisor)
        {
            return coefficient * Math.Pow(t, 2);
        }

        if (t < 2.0 / divisor)
        {
            t -= 1.5 / divisor;
            return coefficient * Math.Pow(t, 2) + bounce2Floor;
        }

        if (t < 2.5 / divisor)
        {
            t -= 2.25 / divisor;
            return coefficient * Math.Pow(t, 2) + bounce3Floor;
        }

        t -= 2.625 / divisor;
        return coefficient * Math.Pow(t, 2) + bounce4Floor;
    }
}
