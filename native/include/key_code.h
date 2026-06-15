// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef KEY_CODE_H
#define KEY_CODE_H

/**
 * Virtual key codes.
 * Values are aligned with Microsoft System.Windows.Forms.Keys enum.
 * See: https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys
 */
typedef enum {
    KEY_BACKSPACE = 8,
    KEY_TAB = 9,
    KEY_CLEAR = 12,
    KEY_ENTER = 13,
    KEY_SHIFT = 16,
    KEY_CONTROL = 17,
    KEY_ALT = 18,
    KEY_PAUSE = 19,
    KEY_CAPS_LOCK = 20,
    KEY_ESCAPE = 27,
    KEY_SPACE = 32,

    KEY_PAGE_UP = 33,
    KEY_PAGE_DOWN = 34,
    KEY_END = 35,
    KEY_HOME = 36,
    KEY_LEFT = 37,
    KEY_UP = 38,
    KEY_RIGHT = 39,
    KEY_DOWN = 40,

    KEY_PRINT_SCREEN = 44,
    KEY_INSERT = 45,
    KEY_DELETE = 46,
    KEY_HELP = 47,

    KEY_0 = 48,
    KEY_1 = 49,
    KEY_2 = 50,
    KEY_3 = 51,
    KEY_4 = 52,
    KEY_5 = 53,
    KEY_6 = 54,
    KEY_7 = 55,
    KEY_8 = 56,
    KEY_9 = 57,

    KEY_A = 65,
    KEY_B = 66,
    KEY_C = 67,
    KEY_D = 68,
    KEY_E = 69,
    KEY_F = 70,
    KEY_G = 71,
    KEY_H = 72,
    KEY_I = 73,
    KEY_J = 74,
    KEY_K = 75,
    KEY_L = 76,
    KEY_M = 77,
    KEY_N = 78,
    KEY_O = 79,
    KEY_P = 80,
    KEY_Q = 81,
    KEY_R = 82,
    KEY_S = 83,
    KEY_T = 84,
    KEY_U = 85,
    KEY_V = 86,
    KEY_W = 87,
    KEY_X = 88,
    KEY_Y = 89,
    KEY_Z = 90,

    KEY_LEFT_WIN = 91,
    KEY_RIGHT_WIN = 92,
    KEY_APPS = 93,
    KEY_SLEEP = 95,

    KEY_NUMPAD_0 = 96,
    KEY_NUMPAD_1 = 97,
    KEY_NUMPAD_2 = 98,
    KEY_NUMPAD_3 = 99,
    KEY_NUMPAD_4 = 100,
    KEY_NUMPAD_5 = 101,
    KEY_NUMPAD_6 = 102,
    KEY_NUMPAD_7 = 103,
    KEY_NUMPAD_8 = 104,
    KEY_NUMPAD_9 = 105,
    KEY_NUMPAD_MULTIPLY = 106,
    KEY_NUMPAD_PLUS = 107,
    KEY_NUMPAD_SEPARATOR = 108,
    KEY_NUMPAD_MINUS = 109,
    KEY_NUMPAD_DECIMAL = 110,
    KEY_NUMPAD_DIVIDE = 111,

    KEY_F1 = 112,
    KEY_F2 = 113,
    KEY_F3 = 114,
    KEY_F4 = 115,
    KEY_F5 = 116,
    KEY_F6 = 117,
    KEY_F7 = 118,
    KEY_F8 = 119,
    KEY_F9 = 120,
    KEY_F10 = 121,
    KEY_F11 = 122,
    KEY_F12 = 123,
    KEY_F13 = 124,
    KEY_F14 = 125,
    KEY_F15 = 126,
    KEY_F16 = 127,
    KEY_F17 = 128,
    KEY_F18 = 129,
    KEY_F19 = 130,
    KEY_F20 = 131,
    KEY_F21 = 132,
    KEY_F22 = 133,
    KEY_F23 = 134,
    KEY_F24 = 135,

    KEY_NUM_LOCK = 144,
    KEY_SCROLL_LOCK = 145,
    KEY_NUMPAD_EQUALS = 146,

    KEY_LEFT_SHIFT = 160,
    KEY_RIGHT_SHIFT = 161,
    KEY_LEFT_CONTROL = 162,
    KEY_RIGHT_CONTROL = 163,
    KEY_LEFT_ALT = 164,
    KEY_RIGHT_ALT = 165,

    KEY_BROWSER_BACK = 166,
    KEY_BROWSER_FORWARD = 167,
    KEY_BROWSER_REFRESH = 168,
    KEY_BROWSER_STOP = 169,
    KEY_BROWSER_SEARCH = 170,
    KEY_BROWSER_FAVORITES = 171,
    KEY_BROWSER_HOME = 172,

    KEY_VOLUME_MUTE = 173,
    KEY_VOLUME_DOWN = 174,
    KEY_VOLUME_UP = 175,

    KEY_MEDIA_NEXT = 176,
    KEY_MEDIA_PREV = 177,
    KEY_MEDIA_STOP = 178,
    KEY_MEDIA_PLAY_PAUSE = 179,

    KEY_SEMICOLON = 186,
    KEY_EQUAL = 187,
    KEY_COMMA = 188,
    KEY_MINUS = 189,
    KEY_DOT = 190,
    KEY_SLASH = 191,
    KEY_GRAVE = 192,

    KEY_OPEN_BRACKET = 219,
    KEY_BACKSLASH = 220,
    KEY_CLOSE_BRACKET = 221,
    KEY_APOSTROPHE = 222,

    KEY_FN = 255,

    // Cross-platform aliases
    KEY_OPTION = KEY_ALT,          // macOS Option = Alt
    KEY_COMMAND = KEY_LEFT_WIN,    // macOS Command = Windows key
    KEY_NUMPAD_CLEAR = KEY_CLEAR,  // Numpad Clear = Clear
} VirtualKeyCode;

#endif // KEY_CODE_H
