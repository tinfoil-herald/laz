// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit;

namespace Laz.Tests;

public class DelayTests
{
    [Fact]
    public void WaitsApproximatelyRequestedTime()
    {
        var requested = TimeSpan.FromMilliseconds(25);
        var stopWatch = Stopwatch.StartNew();
        Delays.WaitFor(requested);
        stopWatch.Stop();

        Assert.True(stopWatch.Elapsed >= requested - TimeSpan.FromMilliseconds(5), $"Expected ~{requested}, got {stopWatch.Elapsed}");
    }

    [Fact]
    public void HandlesNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Delays.WaitFor(TimeSpan.FromMilliseconds(-1)));
    }
}
