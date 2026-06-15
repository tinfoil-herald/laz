// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LINUX_KEYCODE_H
#define LINUX_KEYCODE_H

#include <X11/XF86keysym.h>
#include <X11/Xutil.h>

#include "key_code.h"

/**
 * Maps an OS-agnostic VirtualKeyCode to an X11 KeySym.
 *
 * @param[in] keyCode The cross-platform key code to map.
 * @return The X11 KeySym, or NoSymbol if no mapping exists for this key.
 */
KeySym getLinuxKeySym(VirtualKeyCode keyCode);

#endif  // LINUX_KEYCODE_H
