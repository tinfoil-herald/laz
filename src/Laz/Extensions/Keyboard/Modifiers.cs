// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// Modifier keys that can be combined with a physical key.
/// </summary>
[Flags]
internal enum Modifiers
{
    None = 0,
    Shift = 1,
    Ctrl = 2,

    // Also used as Option.
    Alt = 4
}