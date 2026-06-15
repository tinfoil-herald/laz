// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <X11/Xlib.h>
#include <X11/extensions/XTest.h>
#include <stdbool.h>

#include "keycode.h"
#include "laz_api.h"
#include "xdisplay.h"

static void sendKeyEventInternal(VirtualKeyCode keyCode, bool isDown) {
  Display *display = getMainDisplay();
  if (!display) {
    return;
  }

  const KeySym keysym = getLinuxKeySym(keyCode);
  if (keysym == NoSymbol) {
    return;
  }

  const KeyCode x11KeyCode = XKeysymToKeycode(display, keysym);
  if (x11KeyCode == 0) {
    return;
  }

  XTestFakeKeyEvent(display, x11KeyCode, isDown ? True : False, CurrentTime);
  XSync(display, False);
}

void sendKeyPress(VirtualKeyCode keyCode) { sendKeyEventInternal(keyCode, true); }

void sendKeyRelease(VirtualKeyCode keyCode) { sendKeyEventInternal(keyCode, false); }
