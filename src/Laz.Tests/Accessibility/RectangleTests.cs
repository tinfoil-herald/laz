// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace Laz.Tests;

public class RectangleTests
{
    [Fact]
    public void CenterIsInTheMiddle()
    {
        Assert.Equal(new Point(15, 30), new Rectangle(10, 20, 10, 20).Center);
        Assert.Equal(new Point(-5, -5), new Rectangle(-10, -10, 10, 10).Center);
    }

    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(19, 39, true)]
    [InlineData(20, 20, false)]
    [InlineData(10, 40, false)]
    [InlineData(9, 20, false)]
    public void ContainsUsesExclusiveRightAndBottomEdges(int x, int y, bool expected)
    {
        Assert.Equal(expected, new Rectangle(10, 20, 10, 20).Contains(new Point(x, y)));
    }

    [Fact]
    public void IntersectsWithOverlappingRectangles()
    {
        var rect = new Rectangle(0, 0, 10, 10);

        Assert.True(rect.IntersectsWith(new Rectangle(5, 5, 10, 10)));
        Assert.False(rect.IntersectsWith(new Rectangle(10, 0, 10, 10)));
        Assert.False(rect.IntersectsWith(new Rectangle(5, 5, 0, 10)));
    }

    [Fact]
    public void EmptyWhenNoArea()
    {
        Assert.True(new Rectangle(0, 0, 0, 10).IsEmpty);
        Assert.False(new Rectangle(0, 0, 1, 1).IsEmpty);
    }
}
