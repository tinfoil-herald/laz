// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "keycode.h"
#import <Foundation/Foundation.h>
#include "key_code.h"

static NSDictionary<NSNumber*, NSNumber*>* g_keyCodeMap = nil;

static void initializeKeyCodeMap() {
  if (g_keyCodeMap != nil) {
    return;
  }

  g_keyCodeMap = [@{
    // Letters
    @(KEY_A) : @(kVK_ANSI_A),
    @(KEY_B) : @(kVK_ANSI_B),
    @(KEY_C) : @(kVK_ANSI_C),
    @(KEY_D) : @(kVK_ANSI_D),
    @(KEY_E) : @(kVK_ANSI_E),
    @(KEY_F) : @(kVK_ANSI_F),
    @(KEY_G) : @(kVK_ANSI_G),
    @(KEY_H) : @(kVK_ANSI_H),
    @(KEY_I) : @(kVK_ANSI_I),
    @(KEY_J) : @(kVK_ANSI_J),
    @(KEY_K) : @(kVK_ANSI_K),
    @(KEY_L) : @(kVK_ANSI_L),
    @(KEY_M) : @(kVK_ANSI_M),
    @(KEY_N) : @(kVK_ANSI_N),
    @(KEY_O) : @(kVK_ANSI_O),
    @(KEY_P) : @(kVK_ANSI_P),
    @(KEY_Q) : @(kVK_ANSI_Q),
    @(KEY_R) : @(kVK_ANSI_R),
    @(KEY_S) : @(kVK_ANSI_S),
    @(KEY_T) : @(kVK_ANSI_T),
    @(KEY_U) : @(kVK_ANSI_U),
    @(KEY_V) : @(kVK_ANSI_V),
    @(KEY_W) : @(kVK_ANSI_W),
    @(KEY_X) : @(kVK_ANSI_X),
    @(KEY_Y) : @(kVK_ANSI_Y),
    @(KEY_Z) : @(kVK_ANSI_Z),

    // Numbers
    @(KEY_0) : @(kVK_ANSI_0),
    @(KEY_1) : @(kVK_ANSI_1),
    @(KEY_2) : @(kVK_ANSI_2),
    @(KEY_3) : @(kVK_ANSI_3),
    @(KEY_4) : @(kVK_ANSI_4),
    @(KEY_5) : @(kVK_ANSI_5),
    @(KEY_6) : @(kVK_ANSI_6),
    @(KEY_7) : @(kVK_ANSI_7),
    @(KEY_8) : @(kVK_ANSI_8),
    @(KEY_9) : @(kVK_ANSI_9),

    // Symbols
    @(KEY_COMMA) : @(kVK_ANSI_Comma),
    @(KEY_MINUS) : @(kVK_ANSI_Minus),
    @(KEY_DOT) : @(kVK_ANSI_Period),
    @(KEY_SLASH) : @(kVK_ANSI_Slash),
    @(KEY_SEMICOLON) : @(kVK_ANSI_Semicolon),
    @(KEY_EQUAL) : @(kVK_ANSI_Equal),
    @(KEY_OPEN_BRACKET) : @(kVK_ANSI_LeftBracket),
    @(KEY_CLOSE_BRACKET) : @(kVK_ANSI_RightBracket),
    @(KEY_BACKSLASH) : @(kVK_ANSI_Backslash),
    @(KEY_APOSTROPHE) : @(kVK_ANSI_Quote),
    @(KEY_GRAVE) : @(kVK_ANSI_Grave),

    // Control keys
    @(KEY_BACKSPACE) : @(kVK_Delete),
    @(KEY_TAB) : @(kVK_Tab),
    @(KEY_ENTER) : @(kVK_Return),
    @(KEY_ESCAPE) : @(kVK_Escape),
    @(KEY_SPACE) : @(kVK_Space),
    @(KEY_DELETE) : @(kVK_ForwardDelete),

    // Modifiers (KEY_OPTION = KEY_ALT, KEY_COMMAND = KEY_LEFT_WIN are aliases)
    @(KEY_SHIFT) : @(kVK_Shift),
    @(KEY_CONTROL) : @(kVK_Control),
    @(KEY_ALT) : @(kVK_Option),       // Also serves KEY_OPTION (same value)
    @(KEY_COMMAND) : @(kVK_Command),  // Also serves KEY_LEFT_WIN (same value)
    @(KEY_CAPS_LOCK) : @(kVK_CapsLock),

    // Left-side modifiers
    @(KEY_LEFT_SHIFT) : @(kVK_Shift),
    @(KEY_LEFT_CONTROL) : @(kVK_Control),
    @(KEY_LEFT_ALT) : @(kVK_Option),

    // Right-side modifiers
    @(KEY_RIGHT_SHIFT) : @(kVK_RightShift),
    @(KEY_RIGHT_CONTROL) : @(kVK_RightControl),
    @(KEY_RIGHT_ALT) : @(kVK_RightOption),
    @(KEY_FN) : @(kVK_Function),

    // Arrow keys
    @(KEY_LEFT) : @(kVK_LeftArrow),
    @(KEY_UP) : @(kVK_UpArrow),
    @(KEY_RIGHT) : @(kVK_RightArrow),
    @(KEY_DOWN) : @(kVK_DownArrow),

    // Navigation
    @(KEY_HOME) : @(kVK_Home),
    @(KEY_END) : @(kVK_End),
    @(KEY_PAGE_UP) : @(kVK_PageUp),
    @(KEY_PAGE_DOWN) : @(kVK_PageDown),
    @(KEY_INSERT) : @(kVK_Help),  // Mac doesn't have Insert, Help is closest
    @(KEY_HELP) : @(kVK_Help),
    @(KEY_CLEAR) : @(kVK_ANSI_KeypadClear),  // Also serves KEY_NUMPAD_CLEAR (same value)

    // Function keys
    @(KEY_F1) : @(kVK_F1),
    @(KEY_F2) : @(kVK_F2),
    @(KEY_F3) : @(kVK_F3),
    @(KEY_F4) : @(kVK_F4),
    @(KEY_F5) : @(kVK_F5),
    @(KEY_F6) : @(kVK_F6),
    @(KEY_F7) : @(kVK_F7),
    @(KEY_F8) : @(kVK_F8),
    @(KEY_F9) : @(kVK_F9),
    @(KEY_F10) : @(kVK_F10),
    @(KEY_F11) : @(kVK_F11),
    @(KEY_F12) : @(kVK_F12),
    @(KEY_F13) : @(kVK_F13),
    @(KEY_F14) : @(kVK_F14),
    @(KEY_F15) : @(kVK_F15),
    @(KEY_F16) : @(kVK_F16),
    @(KEY_F17) : @(kVK_F17),
    @(KEY_F18) : @(kVK_F18),
    @(KEY_F19) : @(kVK_F19),
    @(KEY_F20) : @(kVK_F20),
    // F21-F24: no macOS key codes

    // Numpad
    @(KEY_NUMPAD_0) : @(kVK_ANSI_Keypad0),
    @(KEY_NUMPAD_1) : @(kVK_ANSI_Keypad1),
    @(KEY_NUMPAD_2) : @(kVK_ANSI_Keypad2),
    @(KEY_NUMPAD_3) : @(kVK_ANSI_Keypad3),
    @(KEY_NUMPAD_4) : @(kVK_ANSI_Keypad4),
    @(KEY_NUMPAD_5) : @(kVK_ANSI_Keypad5),
    @(KEY_NUMPAD_6) : @(kVK_ANSI_Keypad6),
    @(KEY_NUMPAD_7) : @(kVK_ANSI_Keypad7),
    @(KEY_NUMPAD_8) : @(kVK_ANSI_Keypad8),
    @(KEY_NUMPAD_9) : @(kVK_ANSI_Keypad9),
    @(KEY_NUMPAD_DECIMAL) : @(kVK_ANSI_KeypadDecimal),
    @(KEY_NUMPAD_MULTIPLY) : @(kVK_ANSI_KeypadMultiply),
    @(KEY_NUMPAD_PLUS) : @(kVK_ANSI_KeypadPlus),
    @(KEY_NUMPAD_DIVIDE) : @(kVK_ANSI_KeypadDivide),
    @(KEY_NUMPAD_MINUS) : @(kVK_ANSI_KeypadMinus),
    @(KEY_NUMPAD_EQUALS) : @(kVK_ANSI_KeypadEquals),

    // Other
    @(KEY_SCROLL_LOCK) : @(kVK_F14),            // F14 is ScrollLock on Mac external keyboards
    @(KEY_NUM_LOCK) : @(kVK_ANSI_KeypadClear),  // Clear key on Mac numpad
    @(KEY_PRINT_SCREEN) : @(kVK_F13),           // F13 is PrintScreen on Mac external keyboards
  } copy];
}

// Internal: Query the macOS keycode for a given OS-agnostic key code
// Returns kVK_Undefined (-1) if not found
int getMacKeyCode(VirtualKeyCode keyCode) {
  initializeKeyCodeMap();

  NSNumber* result = [g_keyCodeMap objectForKey:@(keyCode)];
  if (result != nil) {
    return [result intValue];
  }
  return kVK_Undefined;
}
