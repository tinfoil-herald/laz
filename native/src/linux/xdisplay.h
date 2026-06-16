// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef XDISPLAY_H
#define XDISPLAY_H

#include <X11/Xlib.h>
#include <stdbool.h>

/**
 * Returns the shared X11 Display connection.
 *
 * The returned pointer is owned by this module. Callers must not close it.
 *
 * @return The Display connection, or NULL if it could not be opened.
 */
Display *getMainDisplay(void);

/**
 * Returns true if an X11 display is available for input injection.
 *
 * Returns false in pure Wayland sessions where DISPLAY is not set.
 */
bool isX11InputAvailable(void);

#endif  // XDISPLAY_H
