// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Laz.Native;

/// <summary>
/// Native point struct for P/Invoke marshaling.
/// Layout matches native point.h struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    public int X;
    public int Y;

    public static implicit operator Laz.Point(Point native) => new(native.X, native.Y);
    public static implicit operator Point(Laz.Point managed) => new() { X = managed.X, Y = managed.Y };
}
