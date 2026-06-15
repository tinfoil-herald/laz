// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Laz.Native;

/// <summary>
/// Native color struct for P/Invoke marshaling.
/// Layout matches native color.h struct (RGBA byte order).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct Color
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;
}
