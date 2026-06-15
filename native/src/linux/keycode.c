// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "keycode.h"

#include <X11/keysym.h>

KeySym getLinuxKeySym(VirtualKeyCode keyCode) {
  switch (keyCode) {
    // Letters
    case KEY_A:
      return XK_a;
    case KEY_B:
      return XK_b;
    case KEY_C:
      return XK_c;
    case KEY_D:
      return XK_d;
    case KEY_E:
      return XK_e;
    case KEY_F:
      return XK_f;
    case KEY_G:
      return XK_g;
    case KEY_H:
      return XK_h;
    case KEY_I:
      return XK_i;
    case KEY_J:
      return XK_j;
    case KEY_K:
      return XK_k;
    case KEY_L:
      return XK_l;
    case KEY_M:
      return XK_m;
    case KEY_N:
      return XK_n;
    case KEY_O:
      return XK_o;
    case KEY_P:
      return XK_p;
    case KEY_Q:
      return XK_q;
    case KEY_R:
      return XK_r;
    case KEY_S:
      return XK_s;
    case KEY_T:
      return XK_t;
    case KEY_U:
      return XK_u;
    case KEY_V:
      return XK_v;
    case KEY_W:
      return XK_w;
    case KEY_X:
      return XK_x;
    case KEY_Y:
      return XK_y;
    case KEY_Z:
      return XK_z;

    // Numbers
    case KEY_0:
      return XK_0;
    case KEY_1:
      return XK_1;
    case KEY_2:
      return XK_2;
    case KEY_3:
      return XK_3;
    case KEY_4:
      return XK_4;
    case KEY_5:
      return XK_5;
    case KEY_6:
      return XK_6;
    case KEY_7:
      return XK_7;
    case KEY_8:
      return XK_8;
    case KEY_9:
      return XK_9;

    // Symbols
    case KEY_COMMA:
      return XK_comma;
    case KEY_MINUS:
      return XK_minus;
    case KEY_DOT:
      return XK_period;
    case KEY_SLASH:
      return XK_slash;
    case KEY_SEMICOLON:
      return XK_semicolon;
    case KEY_EQUAL:
      return XK_equal;
    case KEY_OPEN_BRACKET:
      return XK_bracketleft;
    case KEY_BACKSLASH:
      return XK_backslash;
    case KEY_CLOSE_BRACKET:
      return XK_bracketright;
    case KEY_APOSTROPHE:
      return XK_apostrophe;
    case KEY_GRAVE:
      return XK_grave;

    // Control keys
    case KEY_BACKSPACE:
      return XK_BackSpace;
    case KEY_TAB:
      return XK_Tab;
    case KEY_CLEAR:  // Also serves KEY_NUMPAD_CLEAR (same value)
      return XK_Clear;
    case KEY_ENTER:
      return XK_Return;
    case KEY_ESCAPE:
      return XK_Escape;
    case KEY_SPACE:
      return XK_space;
    case KEY_DELETE:
      return XK_Delete;
    case KEY_HELP:
      return XK_Help;

    // Modifiers (KEY_OPTION = KEY_ALT, KEY_COMMAND = KEY_LEFT_WIN are aliases)
    case KEY_SHIFT:
      return XK_Shift_L;
    case KEY_CONTROL:
      return XK_Control_L;
    case KEY_ALT:  // Also serves KEY_OPTION (same value)
      return XK_Alt_L;
    case KEY_COMMAND:  // Also serves KEY_LEFT_WIN (same value)
      return XK_Super_L;
    case KEY_CAPS_LOCK:
      return XK_Caps_Lock;

    // Left-side modifiers
    case KEY_LEFT_SHIFT:
      return XK_Shift_L;
    case KEY_LEFT_CONTROL:
      return XK_Control_L;
    case KEY_LEFT_ALT:
      return XK_Alt_L;

    // Right-side modifiers
    case KEY_RIGHT_SHIFT:
      return XK_Shift_R;
    case KEY_RIGHT_CONTROL:
      return XK_Control_R;
    case KEY_RIGHT_ALT:
      return XK_Alt_R;

    // Windows/system keys
    case KEY_RIGHT_WIN:
      return XK_Super_R;
    case KEY_APPS:
      return XK_Menu;

    // Arrow keys
    case KEY_LEFT:
      return XK_Left;
    case KEY_UP:
      return XK_Up;
    case KEY_RIGHT:
      return XK_Right;
    case KEY_DOWN:
      return XK_Down;

    // Navigation
    case KEY_INSERT:
      return XK_Insert;
    case KEY_HOME:
      return XK_Home;
    case KEY_END:
      return XK_End;
    case KEY_PAGE_UP:
      return XK_Page_Up;
    case KEY_PAGE_DOWN:
      return XK_Page_Down;

    // Function keys
    case KEY_F1:
      return XK_F1;
    case KEY_F2:
      return XK_F2;
    case KEY_F3:
      return XK_F3;
    case KEY_F4:
      return XK_F4;
    case KEY_F5:
      return XK_F5;
    case KEY_F6:
      return XK_F6;
    case KEY_F7:
      return XK_F7;
    case KEY_F8:
      return XK_F8;
    case KEY_F9:
      return XK_F9;
    case KEY_F10:
      return XK_F10;
    case KEY_F11:
      return XK_F11;
    case KEY_F12:
      return XK_F12;
    case KEY_F13:
      return XK_F13;
    case KEY_F14:
      return XK_F14;
    case KEY_F15:
      return XK_F15;
    case KEY_F16:
      return XK_F16;
    case KEY_F17:
      return XK_F17;
    case KEY_F18:
      return XK_F18;
    case KEY_F19:
      return XK_F19;
    case KEY_F20:
      return XK_F20;
    case KEY_F21:
      return XK_F21;
    case KEY_F22:
      return XK_F22;
    case KEY_F23:
      return XK_F23;
    case KEY_F24:
      return XK_F24;

    // Other
    case KEY_PRINT_SCREEN:
      return XK_Print;
    case KEY_SCROLL_LOCK:
      return XK_Scroll_Lock;
    case KEY_NUM_LOCK:
      return XK_Num_Lock;
    case KEY_PAUSE:
      return XK_Pause;

    // Numpad
    case KEY_NUMPAD_0:
      return XK_KP_0;
    case KEY_NUMPAD_1:
      return XK_KP_1;
    case KEY_NUMPAD_2:
      return XK_KP_2;
    case KEY_NUMPAD_3:
      return XK_KP_3;
    case KEY_NUMPAD_4:
      return XK_KP_4;
    case KEY_NUMPAD_5:
      return XK_KP_5;
    case KEY_NUMPAD_6:
      return XK_KP_6;
    case KEY_NUMPAD_7:
      return XK_KP_7;
    case KEY_NUMPAD_8:
      return XK_KP_8;
    case KEY_NUMPAD_9:
      return XK_KP_9;
    case KEY_NUMPAD_DECIMAL:
      return XK_KP_Decimal;
    case KEY_NUMPAD_MULTIPLY:
      return XK_KP_Multiply;
    case KEY_NUMPAD_PLUS:
      return XK_KP_Add;
    case KEY_NUMPAD_SEPARATOR:
      return XK_KP_Separator;
    case KEY_NUMPAD_MINUS:
      return XK_KP_Subtract;
    case KEY_NUMPAD_DIVIDE:
      return XK_KP_Divide;
    case KEY_NUMPAD_EQUALS:
      return XK_KP_Equal;

    default:
      return NoSymbol;
  }
}
