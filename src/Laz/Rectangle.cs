// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz;

/// <summary>
/// A rectangle with integer coordinates.
/// </summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// The center of the rectangle, rounded down.
    /// </summary>
    public Point Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// Whether the rectangle has no area.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Checks whether the point lies inside the rectangle. The right and bottom edges are exclusive.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns><c>true</c> if the rectangle contains the point.</returns>
    public bool Contains(Point point)
    {
        return point.X >= X && point.X < X + Width && point.Y >= Y && point.Y < Y + Height;
    }

    /// <summary>
    /// Checks whether two rectangles overlap.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><c>true</c> if the rectangles share at least one point.</returns>
    public bool IntersectsWith(Rectangle other)
    {
        return !IsEmpty && !other.IsEmpty &&
               other.X < X + Width && X < other.X + other.Width &&
               other.Y < Y + Height && Y < other.Y + other.Height;
    }
}
