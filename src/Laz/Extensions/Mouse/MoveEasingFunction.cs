// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Extensions.Mouse;

/// <summary>
/// Available easing functions to emulate mouse movements. 
/// </summary>
public enum MoveEasingFunction
{
#pragma warning disable CS1591
    Linear,
    EaseInOutCubic,
    EaseOutCubic,
    EaseOutQuartic,
    EaseOutExpo,
    EaseInQuad,
    EaseInOutQuad,
    EaseInOutSine,
    EaseOutCircular,
    EaseOutElastic,
    EaseOutBounce
#pragma warning restore CS1591
}
