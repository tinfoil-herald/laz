// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_API_WINDOWS_H
#define LAZ_API_WINDOWS_H

#include <stdbool.h>
#include "color.h"
#include "point.h"
#include "mouse_button.h"
#include "key_code.h"

#ifdef _WIN32
    #define LAZ_EXPORT __declspec(dllexport)
    #define LAZ_CALL __stdcall
#else
    #error "laz_api_windows.h included on a non-Windows platform"
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Simulates the press of the given mouse button at the current pointer position.
 *
 * MOUSE_BUTTON_PRIMARY and MOUSE_BUTTON_SECONDARY respect the system button swap setting
 * (SM_SWAPBUTTON), so MOUSE_BUTTON_PRIMARY always maps to whichever physical button the
 * user has configured as primary.
 *
 * @param[in] button The mouse button to press.
 */
LAZ_EXPORT void LAZ_CALL sendMouseDown(MouseButton button);

/**
 * Simulates the release of the given mouse button at the current pointer position.
 *
 * MOUSE_BUTTON_PRIMARY and MOUSE_BUTTON_SECONDARY respect the system button swap setting
 * (SM_SWAPBUTTON), so MOUSE_BUTTON_PRIMARY always maps to whichever physical button the
 * user has configured as primary.
 *
 * @param[in] button The mouse button to release.
 */
LAZ_EXPORT void LAZ_CALL sendMouseUp(MouseButton button);

/**
 * Simulates a mouse move to the given coordinates, in a single immediate jump.
 *
 * Coordinates are in the virtual screen coordinate space. The origin is the top-left
 * corner of the main display. Coordinates can be negative if the target display is
 * positioned above or to the left of the main display.
 *
 * The coordinate space depends on the DPI awareness of the current process:
 *  - In per-monitor-aware processes, the coordinates are treated as physical pixels.
 *  - In system-aware and unaware processes, the coordinates are treated as virtual pixels
 *    calculated using the DPI of the main display.
 *
 * @param[in] x The horizontal coordinate of the target point.
 * @param[in] y The vertical coordinate of the target point.
 */
LAZ_EXPORT void LAZ_CALL sendMouseMove(int x, int y);

/**
 * Gets the current mouse cursor position.
 *
 * This method returns the coordinates within the DPI context of the current process:
 *  - In per-monitor-aware processes, the coordinates are treated as physical pixels.
 *  - In system-aware and unaware processes, the coordinates are treated as virtual pixels
 *    calculated using the DPI of the main display.
 *
 * Coordinates are in the virtual screen coordinate space and can be negative on multi-monitor
 * setups.
 *
 * @return The current mouse position. Returns `{0, 0}` on failure.
 */
LAZ_EXPORT NativePoint LAZ_CALL getMousePosition(void);

/**
 * Simulates the vertical scroll.
 *
 * The `notches` parameter represents discrete wheel steps. Positive values scroll
 * up, negative values scroll down.
 *
 * One notch maps to one wheel step. The actual scroll distance produced by a
 * step is user-configurable, but usually is 3 lines.
 *
 * @param[in] notches The amount of scroll wheel notches to perform.
 */
LAZ_EXPORT void LAZ_CALL sendScrollEvent(int notches);

/**
 * Simulates a press of a keyboard key.
 *
 * This API operates on keys, not characters. The actual character produced depends on the
 * active keyboard layout and the state of modifier keys.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * @param[in] keyCode The key to press.
 */
LAZ_EXPORT void LAZ_CALL sendKeyPress(VirtualKeyCode keyCode);

/**
 * Simulates a release of a keyboard key.
 *
 * If the key has no mapping on the current platform, the call is silently ignored.
 *
 * @param[in] keyCode The key to release.
 */
LAZ_EXPORT void LAZ_CALL sendKeyRelease(VirtualKeyCode keyCode);

/**
 * Captures a rectangular region of the screen.
 *
 * The buffer must be pre-allocated and sized to hold width * height * 4 bytes (BGRA format).
 * Pixels are written in BGRA order with alpha always set to 255 (fully opaque).
 *
 * @param[in] x The left coordinate of the capture region.
 * @param[in] y The top coordinate of the capture region.
 * @param[in] width The width of the capture region in pixels.
 * @param[in] height The height of the capture region in pixels.
 * @param[out] buffer Pointer to a pre-allocated buffer to receive pixel data (BGRA format).
 * @return true if capture succeeded, false on error.
 */
LAZ_EXPORT bool LAZ_CALL captureScreen(int x, int y, int width, int height, void* buffer);

/**
 * Gets the color of a single pixel at the specified screen coordinates.
 *
 * @param[in] x The horizontal coordinate of the pixel.
 * @param[in] y The vertical coordinate of the pixel.
 * @return The color of the pixel in RGBA format.
 */
LAZ_EXPORT NativeColor LAZ_CALL getPixelColor(int x, int y);

/**
 * Sets the system clipboard to the given text.
 *
 * @param[in] text Null-terminated UTF-8 encoded string. Returns false if NULL.
 * @return true if the clipboard was updated successfully, false on any failure
 *         (NULL input, memory allocation failure, or clipboard access error).
 */
LAZ_EXPORT bool LAZ_CALL setClipboardText(const char* text);

#ifdef __cplusplus
}
#endif

#endif // LAZ_API_WINDOWS_H
