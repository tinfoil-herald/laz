// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Laz;

/// <summary>
/// Utilities for waiting. 
/// </summary>
internal static class Delays
{
    
    /// <summary>
    /// Makes the thread sleep for the given amount of time. 
    /// </summary>
    internal static void WaitFor(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative.");

        if (delay == TimeSpan.Zero)
            return;

        Thread.Sleep(delay);
    }
}
