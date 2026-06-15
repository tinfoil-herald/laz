// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_API_LINUX_H
#define LAZ_API_LINUX_H

#include <stdbool.h>

#include "color.h"
#include "key_code.h"
#include "mouse_button.h"
#include "point.h"

#if defined(__linux__)
#else
#error "laz_api_linux.h included on a non-Linux platform"
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Simulates the press of the given mouse button at the current pointer position.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] button The mouse button to press.
 */
void sendMouseDown(MouseButton button);

/**
 * Simulates the release of the given mouse button at the current pointer position.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] button The mouse button to release.
 */
void sendMouseUp(MouseButton button);

/**
 * Simulates a mouse move to the given coordinates, in a single immediate jump.
 *
 * The `x` and `y` parameters are interpreted as coordinates in the root window coordinate
 * space and are expressed in pixels. In the common multi-monitor setup where all monitors are
 * combined under a single X display, this is a single, continuous coordinate space spanning all
 * monitors with the origin at the top-left of the desktop.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] x The horizontal coordinate of the target point.
 * @param[in] y The vertical coordinate of the target point.
 */
void sendMouseMove(int x, int y);

/**
 * Gets the current mouse cursor position.
 *
 * Returns coordinates within the root window. In the common multi-monitor setup where all
 * monitors are combined under a single X display, this is a single, continuous coordinate
 * space spanning all monitors.
 *
 * Requires an active X11 or XWayland session.
 *
 * @return The current mouse position. Returns `{0, 0}` on failure.
 */
NativePoint getMousePosition(void);

/**
 * Simulates the vertical scroll.
 *
 * The `notches` parameter represents discrete wheel steps (detents). Positive values scroll
 * up, negative values scroll down.
 *
 * Each notch is sent as one X11 button press+release event (button 4 for up, button 5 for
 * down). The scroll distance per event is determined by the receiving application.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] notches The amount of scroll wheel notches to perform.
 */
void sendScrollEvent(int notches);

/**
 * Simulates a press of a keyboard key.
 *
 * This API operates on keys, not characters. The actual character produced depends on the active
 * keyboard layout and what modifier keys are pressed.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] keyCode The key to press.
 */
void sendKeyPress(VirtualKeyCode keyCode);

/**
 * Simulates a release of a keyboard key.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * Requires an active X11 or XWayland session.
 *
 * @param[in] keyCode The key to release.
 */
void sendKeyRelease(VirtualKeyCode keyCode);

/**
 * Captures a rectangular region of the screen.
 *
 * All coordinates and dimensions are in **physical screen pixels** (the display panel's native
 * resolution). On pure X11 without scaling this matches the root window coordinate space.
 * Under XWayland with display scaling, callers must convert from their coordinate space to
 * physical pixels before calling this function.
 *
 * In Wayland environment, including XWayland, this method works through PipeWire
 * which requires user's consent to take screenshots. Therefore, it will trigger a permission dialog
 * and ask for permissions once per session.
 *
 * The buffer must be pre-allocated and sized to hold width * height * 4 bytes (BGRA format).
 * Pixels are written in BGRA order with alpha always set to 255 (fully opaque).
 *
 * @param[in] x The left coordinate of the capture region in physical pixels.
 * @param[in] y The top coordinate of the capture region in physical pixels.
 * @param[in] width The width of the capture region in physical pixels.
 * @param[in] height The height of the capture region in physical pixels.
 * @param[out] buffer Pointer to a pre-allocated buffer to receive pixel data (BGRA format).
 * @return true if capture succeeded, false on error or if user denied permission.
 */
bool captureScreen(int x, int y, int width, int height, void* buffer);

/**
 * Gets the color of a single pixel at the specified screen coordinates.
 *
 * Coordinates are in physical screen pixels. Under XWayland with display scaling,
 * callers must convert from their coordinate space to physical pixels before calling
 * this function.
 *
 * In a Wayland environment (including XWayland), capturing requires PipeWire access.
 * On the first call within a session, the user will be prompted to grant permission.
 *
 * @param[in] x The horizontal coordinate of the pixel in physical pixels.
 * @param[in] y The vertical coordinate of the pixel in physical pixels.
 * @return The color of the pixel in RGBA format.
 */
NativeColor getPixelColor(int x, int y);

/**
 * Returns an opaque integer identifying the currently active keyboard layout.
 *
 * The value changes when the active XKB layout, variant, or group changes. Callers
 * can detect layout changes by comparing values across calls.
 *
 * On the first call, lazily initializes the libxkbcommon compose table as a side effect.
 *
 * @return An integer identifying the active layout.
 */
int getKeyboardLayoutId(void);

/**
 * Probes the character output of a physical key at a given XKB level.
 *
 * @param[in]     virtualKeyCode The platform-independent virtual key code.
 * @param[in]     level          XKB shift level (0=base, 1=Shift, 2=AltGr, 3=Shift+AltGr).
 * @param[in,out] deadKeyState   On input, 0 for a fresh probe or the keysym of a preceding
 *                               dead key to compose with. On output, set to the dead keysym
 *                               when the probed key is itself a dead key.
 * @param[out]    outChars       Buffer to receive the resulting UTF-16 code units.
 * @param[in]     maxChars       Size of the outChars buffer.
 * @return  1 if a character was produced (stored in outChars[0]),
 *         -1 if the key is a dead key (deadKeyState is set),
 *          0 if no output could be determined.
 */
int probeKeyOutput(int virtualKeyCode, int level, unsigned int* deadKeyState,
                   unsigned short* outChars, int maxChars);

#ifdef __cplusplus
}
#endif

#endif  // LAZ_API_LINUX_H
