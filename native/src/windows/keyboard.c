// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <stdbool.h>
#include <windows.h>

#include "keycode.h"
#include "laz_api.h"

// Send a keyboard event using Windows SendInput API.
// See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
static void postKeyEvent(int vkey, bool isDown) {
  INPUT input = {0};
  input.type = INPUT_KEYBOARD;
  input.ki.wVk = (WORD)vkey;
  input.ki.wScan = (WORD)MapVirtualKey(vkey, MAPVK_VK_TO_VSC);
  input.ki.time = 0;
  input.ki.dwExtraInfo = 0;

  input.ki.dwFlags = 0;
  if (!isDown) {
    input.ki.dwFlags |= KEYEVENTF_KEYUP;
  }
  if (isExtendedKey(vkey)) {
    input.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;
  }

  SendInput(1, &input, sizeof(input));
}

LAZ_EXPORT void LAZ_CALL sendKeyPress(VirtualKeyCode keyCode) {
  int vkey = getWindowsKeyCode(keyCode);
  if (vkey == -1) {
    return;
  }

  postKeyEvent(vkey, true);
}

LAZ_EXPORT void LAZ_CALL sendKeyRelease(VirtualKeyCode keyCode) {
  int vkey = getWindowsKeyCode(keyCode);
  if (vkey == -1) {
    return;
  }

  postKeyEvent(vkey, false);
}
