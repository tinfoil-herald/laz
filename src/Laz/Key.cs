// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz;

/// <summary>
/// The OS-agnostic virtual keys.
/// </summary>
/// <remarks>
/// Most values are mostly aligned with
/// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys">System.Windows.Forms.Keys</see>
/// for convenience. This enum introduces <see cref="NumpadEquals"/> and <see cref="Fn"/> that have no equivalent in
/// that enum.
/// </remarks>
public enum Key
{
#pragma warning disable CS1591
    Backspace = 8,
    Tab = 9,
    Clear = 12,
    Enter = 13,
    Shift = 16,
    Control = 17,
    Alt = 18,
    Pause = 19,
    CapsLock = 20,
    Esc = 27,
    Space = 32,

    PageUp = 33,
    PageDown = 34,
    End = 35,
    Home = 36,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,

    PrintScreen = 44,
    Insert = 45,
    Delete = 46,
    Help = 47,

    Zero = 48,
    One = 49,
    Two = 50,
    Three = 51,
    Four = 52,
    Five = 53,
    Six = 54,
    Seven = 55,
    Eight = 56,
    Nine = 57,

    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,

    LeftWin = 91,
    RightWin = 92,
    Apps = 93,
    Sleep = 95,

    Numpad0 = 96,
    Numpad1 = 97,
    Numpad2 = 98,
    Numpad3 = 99,
    Numpad4 = 100,
    Numpad5 = 101,
    Numpad6 = 102,
    Numpad7 = 103,
    Numpad8 = 104,
    Numpad9 = 105,
    NumpadMultiply = 106,
    NumpadPlus = 107,
    NumpadSeparator = 108,
    NumpadMinus = 109,
    NumpadDecimal = 110,
    NumpadDivide = 111,

    F1 = 112,
    F2 = 113,
    F3 = 114,
    F4 = 115,
    F5 = 116,
    F6 = 117,
    F7 = 118,
    F8 = 119,
    F9 = 120,
    F10 = 121,
    F11 = 122,
    F12 = 123,
    F13 = 124,
    F14 = 125,
    F15 = 126,
    F16 = 127,
    F17 = 128,
    F18 = 129,
    F19 = 130,
    F20 = 131,
    F21 = 132,
    F22 = 133,
    F23 = 134,
    F24 = 135,

    NumLock = 144,
    ScrollLock = 145,
    NumpadEquals = 146,

    LeftShift = 160,
    RightShift = 161,
    LeftControl = 162,
    RightControl = 163,
    LeftAlt = 164,
    RightAlt = 165,

    BrowserBack = 166,
    BrowserForward = 167,
    BrowserRefresh = 168,
    BrowserStop = 169,
    BrowserSearch = 170,
    BrowserFavorites = 171,
    BrowserHome = 172,

    VolumeMute = 173,
    VolumeDown = 174,
    VolumeUp = 175,

    MediaNext = 176,
    MediaPrev = 177,
    MediaStop = 178,
    MediaPlayPause = 179,

    Semicolon = 186,
    Equal = 187,
    Comma = 188,
    Minus = 189,
    Dot = 190,
    Slash = 191,
    Grave = 192,

    OpenBracket = 219,
    Backslash = 220,
    CloseBracket = 221,
    Apostrophe = 222,

    Fn = 255,

    // Cross-platform aliases
    Option = Alt,
    Command = LeftWin,
    NumpadClear = Clear
#pragma warning restore CS1591
}
