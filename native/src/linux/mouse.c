// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <X11/Xlib.h>
#include <X11/extensions/XTest.h>
#include <stdbool.h>

#include "laz_api.h"
#include "xdisplay.h"

static unsigned int getX11ButtonNumber(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return 1;
    case MOUSE_BUTTON_MIDDLE:
      return 2;
    case MOUSE_BUTTON_SECONDARY:
      return 3;
    default:
      return 1;
  }
}

void sendMouseDown(MouseButton button) {
  Display *display = getMainDisplay();
  if (!display) {
    return;
  }

  const unsigned int buttonNumber = getX11ButtonNumber(button);
  XTestFakeButtonEvent(display, buttonNumber, True, CurrentTime);
  XSync(display, False);
}

void sendMouseUp(MouseButton button) {
  Display *display = getMainDisplay();
  if (!display) {
    return;
  }

  const unsigned int buttonNumber = getX11ButtonNumber(button);
  XTestFakeButtonEvent(display, buttonNumber, False, CurrentTime);
  XSync(display, False);
}

void sendMouseMove(int x, int y) {
  Display *display = getMainDisplay();
  if (!display) {
    return;
  }

  XTestFakeMotionEvent(display, -1, x, y, CurrentTime);
  XSync(display, False);
}

NativePoint getMousePosition(void) {
  Display *display = getMainDisplay();
  if (!display) {
    return (NativePoint){0, 0};
  }

  int rootX = 0;
  int rootY = 0;
  Window root = None;
  Window child = None;
  int winX = 0;
  int winY = 0;
  unsigned int mask = 0;

  if (XQueryPointer(display, XDefaultRootWindow(display), &root, &child, &rootX, &rootY, &winX,
                    &winY, &mask) == False) {
    return (NativePoint){0, 0};
  }

  return (NativePoint){rootX, rootY};
}

void sendScrollEvent(int notches) {
  Display *display = getMainDisplay();
  if (!display) {
    return;
  }

  while (notches != 0) {
    const unsigned int buttonNumber = notches > 0 ? 4U : 5U;
    XTestFakeButtonEvent(display, buttonNumber, True, CurrentTime);
    XTestFakeButtonEvent(display, buttonNumber, False, CurrentTime);
    notches += notches > 0 ? -1 : 1;
  }

  XSync(display, False);
}
