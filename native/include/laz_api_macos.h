// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_API_MACOS_H
#define LAZ_API_MACOS_H

#include <stdbool.h>
#include "color.h"
#include "point.h"
#include "mouse_button.h"
#include "key_code.h"

#if defined(__APPLE__)
#else
    #error "laz_api_macos.h included on a non-macOS platform"
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Simulates the press of the given mouse button.
 *
 * The coordinates are expected to be logical points (not pixels) in the global display
 * coordinate system. The origin is the top-left corner of the main display. Coordinates
 * can be negative if the target display is positioned above or to the left of the main display.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * @param[in] x      The horizontal coordinate of the mouse event.
 * @param[in] y      The vertical coordinate of the mouse event.
 * @param[in] button The mouse button to press.
 */
void sendMouseDown(int x, int y, MouseButton button);

/**
 * Simulates the release of the given mouse button.
 *
 * The coordinates are expected to be logical points (not pixels) in the global display
 * coordinate system. The origin is the top-left corner of the main display. Coordinates
 * can be negative if the target display is positioned above or to the left of the main display.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * @param[in] x      The horizontal coordinate of the mouse event.
 * @param[in] y      The vertical coordinate of the mouse event.
 * @param[in] button The mouse button to release.
 */
void sendMouseUp(int x, int y, MouseButton button);

/**
 * Simulates a mouse move to the given coordinates, in a single immediate jump.
 *
 * The coordinates are expected to be logical points (not pixels) in the global display
 * coordinate system. The origin is the top-left corner of the main display. Coordinates
 * can be negative if the target display is positioned above or to the left of the main display.
 *
 * When `isButtonDown` is true, a drag event is posted instead of a plain mouse move,
 * which is required for drag-and-drop to work correctly.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * @param[in] x             The horizontal coordinate of the target point.
 * @param[in] y             The vertical coordinate of the target point.
 * @param[in] pressedButton The button currently held down (relevant only when isButtonDown is true).
 * @param[in] isButtonDown  If true, posts a drag event; otherwise posts a plain mouse move.
 */
void sendMouseMove(int x, int y, MouseButton pressedButton, bool isButtonDown);

/**
 * Gets the current mouse cursor position.
 *
 * This method returns coordinates in the global display coordinate system with origin
 * at the top-left corner of the main display.
 *
 * @return The current mouse position. Returns `{0, 0}` on failure.
 */
NativePoint getMousePosition(void);

/**
 * Simulates the vertical scroll.
 *
 * The `notches` parameter represents discrete wheel steps (detents). Positive values scroll
 * up/away from the user; negative values scroll down/toward the user.
 *
 * Each notch scrolls a fixed 3 lines.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * @param[in] notches The amount of scroll wheel notches to perform.
 */
void sendScrollEvent(int notches);

/**
 * Simulates a press of a keyboard key.
 *
 * This API operates on keys, not characters. The actual character produced depends on the
 * active keyboard layout and the state of modifier keys.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * The physical Fn key state is always stripped from the simulated event, so the key
 * behavior is independent of whether the physical Fn key is held.
 *
 * @param[in] keyCode The key to press.
 */
void sendKeyPress(VirtualKeyCode keyCode);

/**
 * Simulates a release of a keyboard key.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * Requires Accessibility permission. Without it, the event is silently dropped.
 *
 * The physical Fn key state is always stripped from the simulated event, so the key
 * behavior is independent of whether the physical Fn key is held.
 *
 * @param[in] keyCode The key to release.
 */
void sendKeyRelease(VirtualKeyCode keyCode);

/**
 * Captures a rectangular region of the screen.
 *
 * The buffer must be pre-allocated and sized to hold width * height * 4 bytes (BGRA format).
 * Pixels are written in BGRA order with alpha always set to 255 (fully opaque).
 *
 * @param[in] x The left coordinate of the capture region.
 * @param[in] y The top coordinate of the capture region.
 * @param[in] width The width of the capture region in logical points.
 * @param[in] height The height of the capture region in logical points.
 * @param[out] buffer Pointer to a pre-allocated buffer to receive pixel data (BGRA format).
 * @return true if capture succeeded, false on error.
 */
bool captureScreen(int x, int y, int width, int height, void* buffer);

/**
 * Gets the color of a single pixel at the specified screen coordinates.
 *
 * Coordinates are logical points in the global display coordinate system, with the origin
 * at the top-left corner of the main display. Coordinates can be negative if a display is
 * positioned above or to the left of the main display.
 *
 * Requires Screen Recording permission. macOS shows a permission prompt on first use.
 * If previously denied, grant access in System Settings.
 *
 * @param[in] x The horizontal coordinate of the pixel.
 * @param[in] y The vertical coordinate of the pixel.
 * @return The color of the pixel in RGBA format.
 */
NativeColor getPixelColor(int x, int y);

/**
 * Sets the system clipboard to the given text.
 *
 * @param[in] text Null-terminated UTF-8 encoded string. Returns false if NULL or
 *                 if the string cannot be decoded as valid UTF-8.
 * @return true if the clipboard was updated successfully, false on any failure.
 */
bool setClipboardText(const char* text);

/**
 * Returns an opaque integer identifying the currently active keyboard input source.
 *
 * The value changes when the user switches keyboard layouts. Callers can detect
 * layout changes by comparing values across calls.
 *
 * @return An integer identifying the active layout, or 0 on failure.
 */
int getKeyboardLayoutId(void);

/**
 * Probes the character output for a given key and modifier combination.
 *
 * @param[in]     macKeyCode    macOS virtual key code (kVK_*).
 * @param[in]     modifierFlags Modifier flags in the format: (CGEventFlags >> 8) & 0xFF.
 * @param[in,out] deadKeyState  Initialize to 0 for a fresh probe. If the probed key is a
 *                              dead key, updated to reflect the dead key state for use in
 *                              a subsequent call to complete the composition.
 * @param[out]    outChars      Buffer to receive the resulting UTF-16 code units.
 * @param[in]     maxChars      Size of the outChars buffer.
 * @return Number of characters produced (>0), 0 for no output, -1 for dead key.
 */
int probeKeyOutput(int macKeyCode, int modifierFlags, unsigned int* deadKeyState,
                   unsigned short* outChars, int maxChars);

#ifdef __cplusplus
}
#endif

#endif // LAZ_API_MACOS_H
