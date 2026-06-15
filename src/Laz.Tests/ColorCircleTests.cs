// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Laz.Tests.Infrastructure;
using Xunit;

namespace Laz.Tests;

public class ColorCircleTests
{
    #region RGB to HSV Conversion Tests

    [Fact]
    public void RedReturnsHue0()
    {
        var hsv = ColorCircle.ToHsv((255, 0, 0));

        Assert.Equal(0f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void GreenReturnsHue120()
    {
        var hsv = ColorCircle.ToHsv((0, 255, 0));

        Assert.Equal(120f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void BlueReturnsHue240()
    {
        var hsv = ColorCircle.ToHsv((0, 0, 255));

        Assert.Equal(240f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void YellowReturnsHue60()
    {
        var hsv = ColorCircle.ToHsv((255, 255, 0));

        Assert.Equal(60f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void CyanReturnsHue180()
    {
        var hsv = ColorCircle.ToHsv((0, 255, 255));

        Assert.Equal(180f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void MagentaReturnsHue300()
    {
        var hsv = ColorCircle.ToHsv((255, 0, 255));

        Assert.Equal(300f, hsv.H, precision: 1);
        Assert.Equal(1f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
    }

    [Fact]
    public void WhiteReturnsZeroSaturation()
    {
        var hsv = ColorCircle.ToHsv((255, 255, 255));

        Assert.Equal(0f, hsv.S, precision: 2);
        Assert.Equal(1f, hsv.V, precision: 2);
        Assert.True(hsv.IsWhite);
    }

    [Fact]
    public void BlackReturnsZeroValue()
    {
        var hsv = ColorCircle.ToHsv((0, 0, 0));

        Assert.Equal(0f, hsv.V, precision: 2);
        Assert.True(hsv.IsBlack);
    }

    [Fact]
    public void GrayReturnsLowSaturation()
    {
        var hsv = ColorCircle.ToHsv((128, 128, 128));

        Assert.Equal(0f, hsv.S, precision: 2);
        Assert.True(hsv.IsGrayscale);
    }

    #endregion

    #region Hue Distance Tests

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 90, 90)]
    [InlineData(0, 180, 180)]
    [InlineData(0, 270, 90)]  // Wraps around
    [InlineData(350, 10, 20)] // Crosses 0
    [InlineData(10, 350, 20)] // Crosses 0 (opposite direction)
    public void HueDistanceReturnsShortestPath(float hue1, float hue2, float expected)
    {
        var distance = ColorCircle.HueDistance(hue1, hue2);
        Assert.Equal(expected, distance, precision: 1);
    }

    #endregion

    #region Color Distance Tests

    [Fact]
    public void IdenticalColorsReturnZeroDistance()
    {
        var distance = ColorCircle.Distance((100, 150, 200), (100, 150, 200));

        Assert.Equal(0f, distance, precision: 3);
    }

    [Fact]
    public void SimilarRedsReturnSmallDistance()
    {
        var distance = ColorCircle.Distance((255, 0, 0), (250, 10, 5));

        Assert.True(distance < 0.1f, $"Similar reds should have small distance, got {distance}");
    }

    [Fact]
    public void RedAndGreenReturnLargeDistance()
    {
        var distance = ColorCircle.Distance((255, 0, 0), (0, 255, 0));

        Assert.True(distance > 0.3f, $"Red and green should have large distance, got {distance}");
    }

    [Fact]
    public void GraysWithDifferentBrightnessBasedOnValue()
    {
        var distance = ColorCircle.Distance((200, 200, 200), (50, 50, 50));

        Assert.True(distance > 0.3f, $"Light and dark gray should differ in value, got {distance}");
    }

    [Fact]
    public void SameHueDifferentSaturationModerateDistance()
    {
        var distance = ColorCircle.Distance((255, 0, 0), (255, 128, 128));

        Assert.True(distance is > 0.1f and < 0.5f,
            $"Same hue, different saturation should have moderate distance, got {distance}");
    }

    #endregion

    #region AreSimilar Tests

    [Fact]
    public void IdenticalColorsAreSimilar()
    {
        Assert.True(ColorCircle.AreSimilar((100, 150, 200), (100, 150, 200)));
    }

    [Fact]
    public void SlightlyDifferentColorsAreSimilar()
    {
        Assert.True(ColorCircle.AreSimilar((100, 150, 200), (105, 148, 203)));
    }

    [Fact]
    public void VeryDifferentColorsAreNotSimilar()
    {
        Assert.False(ColorCircle.AreSimilar((255, 0, 0), (0, 0, 255)));
    }

    #endregion

    #region Color Classification Tests

    [Fact]
    public void PureRedIsRed()
    {
        Assert.True(ColorCircle.IsRed((255, 0, 0)));
    }

    [Fact]
    public void DarkRedIsRed()
    {
        Assert.True(ColorCircle.IsRed((139, 0, 0)));
    }

    [Fact]
    public void BlueIsNotRed()
    {
        Assert.False(ColorCircle.IsRed((0, 0, 255)));
    }

    [Fact]
    public void PureGreenIsGreen()
    {
        Assert.True(ColorCircle.IsGreen((0, 255, 0)));
    }

    [Fact]
    public void ForestGreenIsGreen()
    {
        Assert.True(ColorCircle.IsGreen((34, 139, 34)));
    }

    [Fact]
    public void PureBlueIsBlue()
    {
        Assert.True(ColorCircle.IsBlue((0, 0, 255)));
    }

    [Fact]
    public void NavyBlueIsBlue()
    {
        Assert.True(ColorCircle.IsBlue((0, 0, 128)));
    }

    [Fact]
    public void PureYellowIsYellow()
    {
        Assert.True(ColorCircle.IsYellow((255, 255, 0)));
    }

    [Fact]
    public void PureCyanIsCyan()
    {
        Assert.True(ColorCircle.IsCyan((0, 255, 255)));
    }

    [Fact]
    public void PureMagentaIsMagenta()
    {
        Assert.True(ColorCircle.IsMagenta((255, 0, 255)));
    }

    [Fact]
    public void GrayMatchesNoHue()
    {
        Assert.False(ColorCircle.IsRed((128, 128, 128)));
        Assert.False(ColorCircle.IsGreen((128, 128, 128)));
        Assert.False(ColorCircle.IsBlue((128, 128, 128)));
        Assert.False(ColorCircle.IsYellow((128, 128, 128)));
        Assert.False(ColorCircle.IsCyan((128, 128, 128)));
        Assert.False(ColorCircle.IsMagenta((128, 128, 128)));
    }

    #endregion
}
