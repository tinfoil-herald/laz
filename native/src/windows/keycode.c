// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "keycode.h"

#include <windows.h>

#include "key_code.h"

// Mapping from OS-agnostic key codes to Windows virtual key codes.
// Windows VK_* codes are defined in winuser.h.
// See: https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes

int getWindowsKeyCode(VirtualKeyCode keyCode) {
  switch (keyCode) {
    // Letters (Windows VK codes for A-Z are 0x41-0x5A, same as ASCII uppercase)
    case KEY_A:
      return 'A';
    case KEY_B:
      return 'B';
    case KEY_C:
      return 'C';
    case KEY_D:
      return 'D';
    case KEY_E:
      return 'E';
    case KEY_F:
      return 'F';
    case KEY_G:
      return 'G';
    case KEY_H:
      return 'H';
    case KEY_I:
      return 'I';
    case KEY_J:
      return 'J';
    case KEY_K:
      return 'K';
    case KEY_L:
      return 'L';
    case KEY_M:
      return 'M';
    case KEY_N:
      return 'N';
    case KEY_O:
      return 'O';
    case KEY_P:
      return 'P';
    case KEY_Q:
      return 'Q';
    case KEY_R:
      return 'R';
    case KEY_S:
      return 'S';
    case KEY_T:
      return 'T';
    case KEY_U:
      return 'U';
    case KEY_V:
      return 'V';
    case KEY_W:
      return 'W';
    case KEY_X:
      return 'X';
    case KEY_Y:
      return 'Y';
    case KEY_Z:
      return 'Z';

    // Numbers (Windows VK codes for 0-9 are 0x30-0x39, same as ASCII)
    case KEY_0:
      return '0';
    case KEY_1:
      return '1';
    case KEY_2:
      return '2';
    case KEY_3:
      return '3';
    case KEY_4:
      return '4';
    case KEY_5:
      return '5';
    case KEY_6:
      return '6';
    case KEY_7:
      return '7';
    case KEY_8:
      return '8';
    case KEY_9:
      return '9';

    // Control keys
    case KEY_BACKSPACE:
      return VK_BACK;
    case KEY_TAB:
      return VK_TAB;
    case KEY_CLEAR:  // Also serves KEY_NUMPAD_CLEAR (same value)
      return VK_CLEAR;
    case KEY_ENTER:
      return VK_RETURN;
    case KEY_ESCAPE:
      return VK_ESCAPE;
    case KEY_SPACE:
      return VK_SPACE;
    case KEY_DELETE:
      return VK_DELETE;
    case KEY_HELP:
      return VK_HELP;

    // Modifiers (KEY_OPTION = KEY_ALT, KEY_COMMAND = KEY_LEFT_WIN are aliases)
    case KEY_SHIFT:
      return VK_SHIFT;
    case KEY_CONTROL:
      return VK_CONTROL;
    case KEY_ALT:  // Also serves KEY_OPTION (same value)
      return VK_MENU;
    case KEY_COMMAND:  // Also serves KEY_LEFT_WIN (same value)
      return VK_LWIN;
    case KEY_CAPS_LOCK:
      return VK_CAPITAL;

    // Left-side modifiers
    case KEY_LEFT_SHIFT:
      return VK_LSHIFT;
    case KEY_LEFT_CONTROL:
      return VK_LCONTROL;
    case KEY_LEFT_ALT:
      return VK_LMENU;

    // Right-side modifiers
    case KEY_RIGHT_SHIFT:
      return VK_RSHIFT;
    case KEY_RIGHT_CONTROL:
      return VK_RCONTROL;
    case KEY_RIGHT_ALT:
      return VK_RMENU;

    // Windows/system keys
    case KEY_RIGHT_WIN:
      return VK_RWIN;
    case KEY_APPS:
      return VK_APPS;
    case KEY_SLEEP:
      return VK_SLEEP;

    // Arrow keys
    case KEY_LEFT:
      return VK_LEFT;
    case KEY_UP:
      return VK_UP;
    case KEY_RIGHT:
      return VK_RIGHT;
    case KEY_DOWN:
      return VK_DOWN;

    // Navigation
    case KEY_INSERT:
      return VK_INSERT;
    case KEY_HOME:
      return VK_HOME;
    case KEY_END:
      return VK_END;
    case KEY_PAGE_UP:
      return VK_PRIOR;
    case KEY_PAGE_DOWN:
      return VK_NEXT;

    // Function keys
    case KEY_F1:
      return VK_F1;
    case KEY_F2:
      return VK_F2;
    case KEY_F3:
      return VK_F3;
    case KEY_F4:
      return VK_F4;
    case KEY_F5:
      return VK_F5;
    case KEY_F6:
      return VK_F6;
    case KEY_F7:
      return VK_F7;
    case KEY_F8:
      return VK_F8;
    case KEY_F9:
      return VK_F9;
    case KEY_F10:
      return VK_F10;
    case KEY_F11:
      return VK_F11;
    case KEY_F12:
      return VK_F12;
    case KEY_F13:
      return VK_F13;
    case KEY_F14:
      return VK_F14;
    case KEY_F15:
      return VK_F15;
    case KEY_F16:
      return VK_F16;
    case KEY_F17:
      return VK_F17;
    case KEY_F18:
      return VK_F18;
    case KEY_F19:
      return VK_F19;
    case KEY_F20:
      return VK_F20;
    case KEY_F21:
      return VK_F21;
    case KEY_F22:
      return VK_F22;
    case KEY_F23:
      return VK_F23;
    case KEY_F24:
      return VK_F24;

    // Symbols (OEM keys - US keyboard layout)
    case KEY_GRAVE:
      return VK_OEM_3;  // ` ~
    case KEY_MINUS:
      return VK_OEM_MINUS;  // - _
    case KEY_EQUAL:
      return VK_OEM_PLUS;  // = +
    case KEY_OPEN_BRACKET:
      return VK_OEM_4;  // [ {
    case KEY_CLOSE_BRACKET:
      return VK_OEM_6;  // ] }
    case KEY_BACKSLASH:
      return VK_OEM_5;  // \ |
    case KEY_SEMICOLON:
      return VK_OEM_1;  // ; :
    case KEY_APOSTROPHE:
      return VK_OEM_7;  // ' "
    case KEY_COMMA:
      return VK_OEM_COMMA;  // , <
    case KEY_DOT:
      return VK_OEM_PERIOD;  // . >
    case KEY_SLASH:
      return VK_OEM_2;  // / ?

    // Other
    case KEY_PRINT_SCREEN:
      return VK_SNAPSHOT;
    case KEY_SCROLL_LOCK:
      return VK_SCROLL;
    case KEY_NUM_LOCK:
      return VK_NUMLOCK;
    case KEY_PAUSE:
      return VK_PAUSE;

    // Numpad
    case KEY_NUMPAD_0:
      return VK_NUMPAD0;
    case KEY_NUMPAD_1:
      return VK_NUMPAD1;
    case KEY_NUMPAD_2:
      return VK_NUMPAD2;
    case KEY_NUMPAD_3:
      return VK_NUMPAD3;
    case KEY_NUMPAD_4:
      return VK_NUMPAD4;
    case KEY_NUMPAD_5:
      return VK_NUMPAD5;
    case KEY_NUMPAD_6:
      return VK_NUMPAD6;
    case KEY_NUMPAD_7:
      return VK_NUMPAD7;
    case KEY_NUMPAD_8:
      return VK_NUMPAD8;
    case KEY_NUMPAD_9:
      return VK_NUMPAD9;
    case KEY_NUMPAD_DECIMAL:
      return VK_DECIMAL;
    case KEY_NUMPAD_MULTIPLY:
      return VK_MULTIPLY;
    case KEY_NUMPAD_PLUS:
      return VK_ADD;
    case KEY_NUMPAD_SEPARATOR:
      return VK_SEPARATOR;
    case KEY_NUMPAD_MINUS:
      return VK_SUBTRACT;
    case KEY_NUMPAD_DIVIDE:
      return VK_DIVIDE;

    // Browser keys
    case KEY_BROWSER_BACK:
      return VK_BROWSER_BACK;
    case KEY_BROWSER_FORWARD:
      return VK_BROWSER_FORWARD;
    case KEY_BROWSER_REFRESH:
      return VK_BROWSER_REFRESH;
    case KEY_BROWSER_STOP:
      return VK_BROWSER_STOP;
    case KEY_BROWSER_SEARCH:
      return VK_BROWSER_SEARCH;
    case KEY_BROWSER_FAVORITES:
      return VK_BROWSER_FAVORITES;
    case KEY_BROWSER_HOME:
      return VK_BROWSER_HOME;

    // Volume keys
    case KEY_VOLUME_MUTE:
      return VK_VOLUME_MUTE;
    case KEY_VOLUME_DOWN:
      return VK_VOLUME_DOWN;
    case KEY_VOLUME_UP:
      return VK_VOLUME_UP;

    // Media keys
    case KEY_MEDIA_NEXT:
      return VK_MEDIA_NEXT_TRACK;
    case KEY_MEDIA_PREV:
      return VK_MEDIA_PREV_TRACK;
    case KEY_MEDIA_STOP:
      return VK_MEDIA_STOP;
    case KEY_MEDIA_PLAY_PAUSE:
      return VK_MEDIA_PLAY_PAUSE;

    default:
      return -1;
  }
}

// Extended keys require the KEYEVENTF_EXTENDEDKEY flag for correct behavior.
// These are keys in the navigation cluster and right-side modifiers that
// share scancodes with numpad keys.
// See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
bool isExtendedKey(int windowsKeyCode) {
  switch (windowsKeyCode) {
    case VK_RMENU:     // Right Alt
    case VK_RCONTROL:  // Right Control
    case VK_INSERT:
    case VK_DELETE:
    case VK_HOME:
    case VK_END:
    case VK_PRIOR:  // Page Up
    case VK_NEXT:   // Page Down
    case VK_LEFT:
    case VK_RIGHT:
    case VK_UP:
    case VK_DOWN:
    case VK_NUMLOCK:
    case VK_SNAPSHOT:  // Print Screen
    case VK_DIVIDE:    // Numpad divide (only numpad key that's extended)
    case VK_LWIN:
    case VK_RWIN:
    case VK_APPS:
      return true;
    default:
      return false;
  }
}
