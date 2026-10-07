// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Laz;

/// <summary>
/// Limits for a search in the accessibility tree.
/// </summary>
/// <remarks>
/// Accessibility trees of large applications, such as browsers and IDEs, can contain tens of thousands of
/// elements, and every visited element costs a request to another process. These limits keep a search bounded.
/// </remarks>
public sealed class FindOptions
{
    /// <summary>
    /// The default options.
    /// </summary>
    public static readonly FindOptions Default = new();

    /// <summary>
    /// The maximum depth below the search root. Defaults to 50.
    /// </summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>
    /// The maximum number of elements to visit. Defaults to 10,000.
    /// </summary>
    public int MaxElements { get; init; } = 10_000;

    /// <summary>
    /// The time budget for the whole search. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
