// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <stdbool.h>
#include <windows.h>

#include "laz_api.h"

static void doMouseMove(int x, int y) {
  INPUT input = {0};
  input.type = INPUT_MOUSE;
  input.mi.time = 0;
  input.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE | MOUSEEVENTF_VIRTUALDESK;

  // Adjust coordinates relative to the virtual screen origin (can be negative with multi-monitor).
  int relX = x - GetSystemMetrics(SM_XVIRTUALSCREEN);
  int relY = y - GetSystemMetrics(SM_YVIRTUALSCREEN);

  int screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
  int screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

  // Normalize pixel coordinates to the 0..65535 absolute range.
  input.mi.dx = screenWidth > 1 ? (LONG)MulDiv(relX, 65535, screenWidth - 1) : 0;
  input.mi.dy = screenHeight > 1 ? (LONG)MulDiv(relY, 65535, screenHeight - 1) : 0;

  POINT before = {0};
  BOOL haveBefore = GetCursorPos(&before);

  UINT sent = SendInput(1, &input, sizeof(input));
  if (sent != 1) {
    SetCursorPos(x, y);
    return;
  }

  // The 0..65535 normalization above is lossy: Windows rescales it back to pixels using its own
  // internal fixed-point math, which can land 1px off from the requested target. Snap to the exact
  // pixel with SetCursorPos (no normalization involved) when that happens.
  POINT actual;
  for (int i = 0; i < 3; i++) {
    if (GetCursorPos(&actual) && (!haveBefore || actual.x != before.x || actual.y != before.y)) {
      break;
    }
    Sleep(0);
  }
  if (GetCursorPos(&actual) && (actual.x != x || actual.y != y)) {
    SetCursorPos(x, y);
  }
}

static void doMouseButton(MouseButton button, bool isDown) {
  INPUT input = {0};
  input.type = INPUT_MOUSE;
  input.mi.time = 0;

  BOOL swapped = GetSystemMetrics(SM_SWAPBUTTON);

  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      if (swapped) {
        input.mi.dwFlags = isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
      } else {
        input.mi.dwFlags = isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
      }
      break;
    case MOUSE_BUTTON_SECONDARY:
      if (swapped) {
        input.mi.dwFlags = isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
      } else {
        input.mi.dwFlags = isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
      }
      break;
    case MOUSE_BUTTON_MIDDLE:
      input.mi.dwFlags = isDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
      break;
  }

  SendInput(1, &input, sizeof(input));
}

LAZ_EXPORT void LAZ_CALL sendMouseDown(MouseButton button) { doMouseButton(button, true); }

LAZ_EXPORT void LAZ_CALL sendMouseUp(MouseButton button) { doMouseButton(button, false); }

LAZ_EXPORT void LAZ_CALL sendMouseMove(int x, int y) { doMouseMove(x, y); }


LAZ_EXPORT NativePoint LAZ_CALL getMousePosition(void) {
  POINT pt;
  if (GetCursorPos(&pt)) {
    return (NativePoint){pt.x, pt.y};
  }
  return (NativePoint){0, 0};
}

// One scroll wheel notch (click/detent) equals WHEEL_DELTA (120) in Windows.
// See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput
LAZ_EXPORT void LAZ_CALL sendScrollEvent(int notches) {
  INPUT input = {0};
  input.type = INPUT_MOUSE;
  input.mi.dwFlags = MOUSEEVENTF_WHEEL;
  input.mi.mouseData = notches * WHEEL_DELTA;

  SendInput(1, &input, sizeof(input));
}
