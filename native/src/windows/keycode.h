// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef WINDOWS_KEYCODE_H
#define WINDOWS_KEYCODE_H

#include <stdbool.h>

#include "key_code.h"

// Maps OS-agnostic key codes to Windows virtual key codes.
// See: https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
int getWindowsKeyCode(VirtualKeyCode keyCode);

// Returns true if the key requires the KEYEVENTF_EXTENDEDKEY flag.
// Extended keys are those in the navigation cluster and right-side modifiers.
// See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
bool isExtendedKey(int windowsKeyCode);

#endif  // WINDOWS_KEYCODE_H
