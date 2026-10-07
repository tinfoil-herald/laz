// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz;

/// <summary>
/// Describes what the <see cref="AccessibleElement.Bounds"/> of an element are relative to.
/// </summary>
public enum BoundsKind
{
    /// <summary>The element has no bounds, for example, because it is not rendered.</summary>
    None = 0,

    /// <summary>
    /// Screen coordinates, in the same space as <see cref="Mouse"/> and <see cref="Screen"/>.
    /// </summary>
    Screen = 1,

    /// <summary>
    /// Coordinates relative to the top-left corner of the containing window.
    /// Reported on Linux in Wayland sessions, where applications do not know their position on the screen.
    /// </summary>
    WindowRelative = 2,
}
