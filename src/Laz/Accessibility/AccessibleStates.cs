// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Laz;

/// <summary>
/// The states of an accessible element. The values match the <c>LAZ_A11Y_STATE_*</c> flags of the native API.
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "A set of states.")]
public enum AccessibleStates : uint
{
    /// <summary>No states.</summary>
    None = 0,

    /// <summary>The element accepts input.</summary>
    Enabled = 1u << 0,

    /// <summary>The element has the keyboard focus.</summary>
    Focused = 1u << 1,

    /// <summary>The element can receive the keyboard focus.</summary>
    Focusable = 1u << 2,

    /// <summary>The element is selected.</summary>
    Selected = 1u << 3,

    /// <summary>The element is checked or toggled on.</summary>
    Checked = 1u << 4,

    /// <summary>The element is partially checked.</summary>
    Mixed = 1u << 5,

    /// <summary>The element is expanded.</summary>
    Expanded = 1u << 6,

    /// <summary>The element can be expanded and is collapsed.</summary>
    Collapsed = 1u << 7,

    /// <summary>The element is not visible on the screen, for example, because it is scrolled out of view.</summary>
    Offscreen = 1u << 8,

    /// <summary>The value of the element cannot be edited.</summary>
    ReadOnly = 1u << 9,

    /// <summary>The element is a password field.</summary>
    Password = 1u << 10,

    /// <summary>The element is a modal window.</summary>
    Modal = 1u << 11,

    /// <summary>The element is the active window.</summary>
    Active = 1u << 12,
}
