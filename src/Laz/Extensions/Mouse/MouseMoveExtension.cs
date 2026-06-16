// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Laz.Extensions.Mouse;

/// <summary>
/// An extension for the <see cref="Mouse"/> that performs realistic mouse move and drag-n-drop operations.
/// </summary>
public static class MouseMoveExtension
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan DragPreambleDelay = TimeSpan.FromMilliseconds(1000);

    private const int MinSampleIntervalMs = 10;

    /// <summary>
    /// Naturally moves the mouse pointer from one point to another.
    /// </summary>
    /// 
    /// <param name="mouse">The mouse instance</param>
    /// <param name="start">The starting point.</param>
    /// <param name="target">The target point.</param>
    /// <param name="duration">The optional duration of the maneuver, 1.5 seconds by default.</param>
    /// <param name="moveFunction">The optional movement function, linear by default.</param>
    /// <param name="easingFunction">The optional timing easing function, linear by default.</param>
    /// <returns></returns>
    public static Laz.Mouse MoveTo(
        this Laz.Mouse mouse,
        Point start,
        Point target,
        TimeSpan? duration,
        MouseMoveFunction moveFunction = MouseMoveFunction.Linear,
        MoveEasingFunction easingFunction = MoveEasingFunction.Linear)
    {
        var actualDuration = duration ?? DefaultDuration;

        ValidatePreconditions(start, target, actualDuration);

        return moveFunction switch
        {
            MouseMoveFunction.Linear => ExecuteLinearMovement(mouse, start, target, actualDuration, easingFunction),
            MouseMoveFunction.Bezier => ExecuteBezierMovement(mouse, start, target, actualDuration, easingFunction),
            _ => throw new ArgumentOutOfRangeException(nameof(moveFunction), moveFunction, "Unknown move function")
        };
    }

    /// <summary>
    /// Presses the primary mouse button, naturally moves the mouse pointer to another point, and then releases
    /// the mouse button.
    /// </summary>
    /// 
    /// <param name="mouse">The mouse instance</param>
    /// <param name="start">The starting point.</param>
    /// <param name="target">The target point.</param>
    /// <param name="duration">The optional duration of the maneuver, 1.5 seconds by default.</param>
    /// <param name="moveFunction">The optional movement function, linear by default.</param>
    /// <param name="easingFunction">The optional timing easing function, linear by default.</param>
    /// <param name="dragPreamble">If true, this method will start drag with a slow short movement downwards, that
    /// may improve the drag gesture recognition on certain systems.</param>
    /// <returns></returns>
    public static Laz.Mouse DragAndDrop(
        this Laz.Mouse mouse,
        Point start,
        Point target,
        TimeSpan? duration = null,
        MouseMoveFunction moveFunction = MouseMoveFunction.Linear,
        MoveEasingFunction easingFunction = MoveEasingFunction.Linear,
        bool dragPreamble = false)
    {
        mouse.JumpTo(start);
        mouse.Press().Delay();

        try
        {
            if (dragPreamble)
            {
                Delays.WaitFor(DragPreambleDelay);
                var newStart = PerformDragPreamble(mouse, start);
                mouse.MoveTo(newStart, target, duration, moveFunction, easingFunction);
            }
            else
            {
                mouse.MoveTo(start, target, duration, moveFunction, easingFunction);
            }
        }
        finally
        {
            mouse.Release();
        }
        return mouse;
    }

    private static Point PerformDragPreamble(Laz.Mouse mouse, Point start)
    {
        var slightlyDown = new Point(start.X, start.Y + 10);
        mouse.MoveTo(start, slightlyDown, DragPreambleDelay);
        return slightlyDown;
    }

    private static void ValidatePreconditions(Point start, Point end, TimeSpan duration)
    {
        if (!IsFinite(start.X) || !IsFinite(start.Y))
            throw new ArgumentException("Start coordinates must be finite numeric values.", nameof(start));

        if (!IsFinite(end.X) || !IsFinite(end.Y))
            throw new ArgumentException("End coordinates must be finite numeric values.", nameof(end));

        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be strictly positive.");
    }

    private static bool IsFinite(int value) => value != int.MinValue && value != int.MaxValue;

    private static Laz.Mouse ExecuteLinearMovement(
        Laz.Mouse mouse,
        Point start,
        Point end,
        TimeSpan duration,
        MoveEasingFunction easing)
    {
        return ExecuteMovement(mouse, start, end, duration, easing, LinearInterpolate);
    }

    private static Laz.Mouse ExecuteBezierMovement(
        Laz.Mouse mouse,
        Point start,
        Point end,
        TimeSpan duration,
        MoveEasingFunction easing)
    {
        var (cp1, cp2) = CubicBezier.ComputeControlPoints((start.X, start.Y), (end.X, end.Y));
        return ExecuteMovement(mouse, start, end, duration, easing,
            (s, e, t) => CubicBezier.Evaluate((s.X, s.Y), cp1, cp2, (e.X, e.Y), t));
    }

    private static Laz.Mouse ExecuteMovement(
        Laz.Mouse mouse,
        Point start,
        Point end,
        TimeSpan duration,
        MoveEasingFunction easing,
        Func<Point, Point, double, (double x, double y)> interpolate)
    {
        mouse.JumpTo(start);

        var stopwatch = Stopwatch.StartNew();
        var durationMs = duration.TotalMilliseconds;
        var lastPosition = (x: (double)start.X, y: (double)start.Y);

        while (stopwatch.ElapsedMilliseconds < durationMs)
        {
            var normalizedTime = stopwatch.ElapsedMilliseconds / durationMs;
            var easedTime = EasingFunctions.Evaluate(easing, normalizedTime);
            var position = interpolate(start, end, easedTime);

            if (HasMovedSignificantly(lastPosition, position))
            {
                var point = new Point(RoundToInt(position.x), RoundToInt(position.y));
                mouse.JumpTo(point);
                lastPosition = position;
            }

            Delays.WaitFor(TimeSpan.FromMilliseconds(MinSampleIntervalMs));
        }

        mouse.JumpTo(end);
        return mouse;
    }

    private static (double x, double y) LinearInterpolate(Point start, Point end, double t)
    {
        return (
            x: start.X + (end.X - start.X) * t,
            y: start.Y + (end.Y - start.Y) * t
        );
    }

    private static bool HasMovedSignificantly((double x, double y) last, (double x, double y) current)
    {
        var dx = current.x - last.x;
        var dy = current.y - last.y;
        return dx * dx + dy * dy >= 1.0;
    }

    private static int RoundToInt(double value)
    {
        return (int)Math.Round(value);
    }
}
