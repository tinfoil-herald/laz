// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Tests.Infrastructure;

/// <summary>
/// Provides color comparison using HSV (Hue, Saturation, Value) color space.
/// HSV represents colors on a circular wheel, making it more intuitive for
/// comparing perceptually similar colors.
/// </summary>
public static class ColorCircle
{
    /// <summary>
    /// Standard hue angle ranges on the color wheel (in degrees).
    /// </summary>
    private static class HueRanges
    {
        public const float RedEnd = 30f;
        public const float YellowStart = 60f;
        public const float YellowGreenStart = 90f;
        public const float YellowGreenEnd = 120f;
        public const float CyanGreenStart = 150f;
        public const float CyanGreenEnd = 180f;
        public const float BlueCyanStart = 210f;
        public const float BlueCyanEnd = 240f;
        public const float BlueMagentaStart = 270f;
        public const float BlueMagentaEnd = 300f;
        public const float RedMagentaStart = 330f;
        public const float RedMagentaEnd = 360f;
    }

    /// <summary>
    /// Represents a color in HSV (Hue, Saturation, Value) color space.
    /// </summary>
    /// <param name="H">Hue angle in degrees (0-360)</param>
    /// <param name="S">Saturation (0-1)</param>
    /// <param name="V">Value/Brightness (0-1)</param>
    public readonly record struct Hsv(float H, float S, float V)
    {
        ///<summary>
        /// Returns true if this color is essentially grayscale (very low saturation).
        /// </summary>
        public bool IsGrayscale => S < 0.1f;

        /// <summary>
        /// Returns true if this color is essentially black (very low value).
        /// </summary>
        public bool IsBlack => V < 0.1f;

        /// <summary>
        /// Returns true if this color is essentially white (high value, low saturation).
        /// </summary>
        public bool IsWhite => V > 0.9f && S < 0.1f;
    }

    /// <summary>
    /// Converts an RGB color to HSV color space.
    /// </summary>
    public static Hsv ToHsv((byte r, byte g, byte b) color)
    {
        var red = color.r / 255f;
        var green = color.g / 255f;
        var blue = color.b / 255f;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        var saturation = max == 0 ? 0 : delta / max;

        float hue;
        if (delta == 0)
        {
            hue = 0; // Undefined, achromatic (gray)
        }
        else if (max == red)
        {
            hue = 60f * ((green - blue) / delta % 6);
        }
        else if (max == green)
        {
            hue = 60f * ((blue - red) / delta + 2);
        }
        else
        {
            hue = 60f * ((red - green) / delta + 4);
        }

        if (hue < 0)
            hue += 360f;

        return new Hsv(hue, saturation, max);
    }

    /// <summary>
    /// Calculates the angular distance between two hues on the color wheel.
    /// Returns a value between 0 and 180 degrees.
    /// </summary>
    public static float HueDistance(float hue1, float hue2)
    {
        var diff = Math.Abs(hue1 - hue2);
        return diff > 180 ? 360 - diff : diff;
    }

    /// <summary>
    /// Calculates the perceptual distance between two colors in HSV space.
    /// Returns a value between 0 (identical) and 1 (maximum difference).
    /// </summary>
    /// <remarks>
    /// The distance calculation weighs hue, saturation, and value differently:
    /// - When colors are desaturated (grayscale), hue is ignored
    /// - When colors are very dark, both hue and saturation are less important
    /// </remarks>
    public static float Distance((byte r, byte g, byte b) fromColor, (byte r, byte g, byte b) toColor)
    {
        var hsv1 = ToHsv(fromColor);
        var hsv2 = ToHsv(toColor);

        return Distance(hsv1, hsv2);
    }

    /// <summary>
    /// Calculates the perceptual distance between two HSV colors.
    /// Returns a value between 0 (identical) and 1 (maximum difference).
    /// </summary>
    public static float Distance(Hsv fromHsv, Hsv toHsv)
    {
        // Normalize hue distance to 0-1 range (180 degrees max difference)
        var hueDist = HueDistance(fromHsv.H, toHsv.H) / 180f;

        // Saturation and value differences are already in 0-1 range
        var satDist = Math.Abs(fromHsv.S - toHsv.S);
        var valDist = Math.Abs(fromHsv.V - toHsv.V);

        // Weight hue by the minimum saturation (hue is meaningless for grayscale)
        var minSat = Math.Min(fromHsv.S, toHsv.S);

        // Weight saturation by the minimum value (saturation is meaningless for black)
        var minVal = Math.Min(fromHsv.V, toHsv.V);

        // Combine weighted components
        // Hue contributes based on saturation, saturation based on value, value always matters
        var weightedHue = hueDist * minSat;
        var weightedSat = satDist * minVal;

        // Use Euclidean distance in the weighted space
        return (float)Math.Sqrt(
            weightedHue * weightedHue +
            weightedSat * weightedSat +
            valDist * valDist) / (float)Math.Sqrt(3); // Normalize to 0-1
    }

    /// <summary>
    /// Determines if two colors are similar within a given tolerance.
    /// </summary>
    /// <param name="color1">First color</param>
    /// <param name="color2">Second color</param>
    /// <param name="tolerance">Maximum distance (0-1) to consider colors similar. Default is 0.1</param>
    public static bool AreSimilar((byte r, byte g, byte b) color1, (byte r, byte g, byte b) color2, float tolerance = 0.1f)
    {
        return Distance(color1, color2) <= tolerance;
    }

    /// <summary>
    /// Determines if a color falls within a specific hue range.
    /// </summary>
    /// <param name="color">The color to check</param>
    /// <param name="hueStart">Start of hue range in degrees</param>
    /// <param name="hueEnd">End of hue range in degrees</param>
    /// <param name="minSaturation">Minimum saturation required (default 0.2)</param>
    /// <param name="minValue">Minimum value required (default 0.2)</param>
    public static bool IsInHueRange((byte r, byte g, byte b) color, float hueStart, float hueEnd,
        float minSaturation = 0.2f, float minValue = 0.2f)
    {
        var hsv = ToHsv(color);

        if (hsv.S < minSaturation || hsv.V < minValue)
            return false;

        // Handle wrap-around (e.g., red spans 330-360 and 0-30)
        if (hueStart <= hueEnd)
        {
            return hsv.H >= hueStart && hsv.H < hueEnd;
        }

        // Wrap-around case
        return hsv.H >= hueStart || hsv.H < hueEnd;
    }

    /// <summary>
    /// Checks if a color is predominantly red.
    /// </summary>
    public static bool IsRed((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.RedMagentaStart, HueRanges.RedEnd);

    /// <summary>
    /// Checks if a color is predominantly green.
    /// </summary>
    public static bool IsGreen((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.YellowGreenStart, HueRanges.CyanGreenEnd);

    /// <summary>
    /// Checks if a color is predominantly blue.
    /// </summary>
    public static bool IsBlue((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.BlueCyanStart, HueRanges.BlueMagentaEnd);

    /// <summary>
    /// Checks if a color is predominantly yellow.
    /// </summary>
    public static bool IsYellow((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.YellowStart, HueRanges.YellowGreenEnd);

    /// <summary>
    /// Checks if a color is predominantly cyan.
    /// </summary>
    public static bool IsCyan((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.CyanGreenStart, HueRanges.BlueCyanEnd);

    /// <summary>
    /// Checks if a color is predominantly magenta.
    /// </summary>
    public static bool IsMagenta((byte r, byte g, byte b) color) =>
        IsInHueRange(color, HueRanges.BlueMagentaStart, HueRanges.RedMagentaEnd);
}
