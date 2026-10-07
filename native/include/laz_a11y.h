// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_A11Y_H
#define LAZ_A11Y_H

#include <stdbool.h>
#include <stdint.h>

#include "laz_export.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Read-only access to the platform accessibility tree.
 *
 * Backends:
 *  - Windows: UI Automation.
 *  - macOS:   AXUIElement. Requires the Accessibility permission.
 *  - Linux:   AT-SPI2 over D-Bus, loaded from laz_a11y_atspi.so.
 *
 * Elements are opaque handles. Every handle returned by this API is owned by the caller and must
 * be released with lazA11yRelease(). A handle must not be used from two threads at the same time,
 * but different handles may be used concurrently, and lazA11yRelease() may be called from any
 * thread.
 *
 * Do not call this API from the UI thread of a window that is being inspected. The platform
 * accessibility service sends requests back to that thread, so the call blocks until it times out.
 */

/** An opaque element handle. */
typedef struct LazA11yElement LazA11yElement;

/** Result codes. Every function that returns `int` returns one of these. */
typedef enum {
  LAZ_A11Y_OK = 0,
  LAZ_A11Y_E_INVALID_ARG = -1,
  /** The accessibility service is not available, e.g. no AT-SPI bus or COM initialization failed. */
  LAZ_A11Y_E_NOT_AVAILABLE = -2,
  /** macOS: the process is not trusted for accessibility. */
  LAZ_A11Y_E_ACCESS_DENIED = -3,
  /** The element no longer exists. */
  LAZ_A11Y_E_ELEMENT_GONE = -4,
  LAZ_A11Y_E_TIMEOUT = -5,
  /** The requested element does not exist, e.g. the parent of the root. */
  LAZ_A11Y_E_NOT_FOUND = -6,
  LAZ_A11Y_E_OUT_OF_MEMORY = -7,
  LAZ_A11Y_E_INTERNAL = -8
} LazA11yResult;

/** What the bounds in LazA11yInfo are relative to. */
typedef enum {
  /** The element has no bounds. */
  LAZ_A11Y_BOUNDS_NONE = 0,
  /** Screen coordinates, in the same space as the mouse API. */
  LAZ_A11Y_BOUNDS_SCREEN = 1,
  /** Relative to the top-left corner of the containing window (Linux on Wayland). */
  LAZ_A11Y_BOUNDS_WINDOW_RELATIVE = 2
} LazA11yBoundsKind;

/* State bit flags. The values are part of the ABI. */
#define LAZ_A11Y_STATE_ENABLED (1u << 0)
#define LAZ_A11Y_STATE_FOCUSED (1u << 1)
#define LAZ_A11Y_STATE_FOCUSABLE (1u << 2)
#define LAZ_A11Y_STATE_SELECTED (1u << 3)
#define LAZ_A11Y_STATE_CHECKED (1u << 4)
#define LAZ_A11Y_STATE_MIXED (1u << 5)
#define LAZ_A11Y_STATE_EXPANDED (1u << 6)
#define LAZ_A11Y_STATE_COLLAPSED (1u << 7)
#define LAZ_A11Y_STATE_OFFSCREEN (1u << 8)
#define LAZ_A11Y_STATE_READONLY (1u << 9)
#define LAZ_A11Y_STATE_PASSWORD (1u << 10)
#define LAZ_A11Y_STATE_MODAL (1u << 11)
#define LAZ_A11Y_STATE_ACTIVE (1u << 12)

/**
 * A snapshot of the properties of an element.
 *
 * String fields are UTF-8 and may be NULL. They are owned by the element handle and stay valid
 * until the next lazA11yGetInfo() or lazA11yRefresh() on that handle, or until it is released.
 */
typedef struct {
  /** The caller sets this to sizeof(LazA11yInfo) before calling lazA11yGetInfo(). */
  int32_t structSize;
  /** UIA ControlTypeId on Windows, AtspiRole on Linux, -1 on macOS. */
  int32_t rawRoleId;
  /** AXRole on macOS, the AT-SPI role name on Linux, the UIA localized control type on Windows. */
  const char* rawRole;
  /** AXSubrole on macOS, NULL elsewhere. */
  const char* rawSubrole;
  const char* name;
  const char* value;
  const char* description;
  /** UIA AutomationId, AXIdentifier, or AT-SPI AccessibleId. */
  const char* automationId;
  /** UIA ClassName, NULL elsewhere. */
  const char* className;
  int32_t x;
  int32_t y;
  int32_t width;
  int32_t height;
  /** One of LazA11yBoundsKind. */
  int32_t boundsKind;
  /** A combination of LAZ_A11Y_STATE_* flags. */
  uint32_t states;
  int32_t processId;
  /** The number of children if known without another query, otherwise -1. */
  int32_t childCountHint;
} LazA11yInfo;

/**
 * Checks whether the accessibility service can be used.
 *
 * @param[in] prompt macOS: when true and the process is not trusted, shows the system prompt
 *                   that asks the user to grant the Accessibility permission. Ignored elsewhere.
 * @return LAZ_A11Y_OK, LAZ_A11Y_E_ACCESS_DENIED, or LAZ_A11Y_E_NOT_AVAILABLE.
 */
LAZ_EXPORT int LAZ_CALL lazA11yIsAvailable(bool prompt);

/**
 * Sets the timeout for a single request to the accessibility service.
 *
 * @param[in] milliseconds A positive timeout.
 */
LAZ_EXPORT int LAZ_CALL lazA11ySetTimeout(int milliseconds);

/** Gets the root of the tree: the desktop. */
LAZ_EXPORT int LAZ_CALL lazA11yGetRoot(LazA11yElement** out);

/** Gets the element that has the keyboard focus. */
LAZ_EXPORT int LAZ_CALL lazA11yGetFocused(LazA11yElement** out);

/** Gets the deepest element at the given screen point, in the mouse coordinate space. */
LAZ_EXPORT int LAZ_CALL lazA11yElementFromPoint(int x, int y, LazA11yElement** out);

/** Gets the parent of an element. Returns LAZ_A11Y_E_NOT_FOUND for the root. */
LAZ_EXPORT int LAZ_CALL lazA11yGetParent(LazA11yElement* element, LazA11yElement** out);

/**
 * Gets the children of an element.
 *
 * On success, `*outArray` points to an array of `*outCount` handles. The caller owns every handle
 * and must free the array itself with lazA11yFreeElementArray(). When there are no children,
 * `*outArray` is NULL and `*outCount` is 0.
 */
LAZ_EXPORT int LAZ_CALL lazA11yGetChildren(LazA11yElement* element, LazA11yElement*** outArray,
                                           int* outCount);

/** Frees an array returned by lazA11yGetChildren(). Does not release the handles in it. */
LAZ_EXPORT void LAZ_CALL lazA11yFreeElementArray(LazA11yElement** array);

/** Fills `info` with the properties read when the handle was created or last refreshed. */
LAZ_EXPORT int LAZ_CALL lazA11yGetInfo(LazA11yElement* element, LazA11yInfo* info);

/** Reads the properties of an element again. Invalidates strings from earlier lazA11yGetInfo(). */
LAZ_EXPORT int LAZ_CALL lazA11yRefresh(LazA11yElement* element);

/** Checks whether two handles refer to the same element. */
LAZ_EXPORT int LAZ_CALL lazA11yIsSameElement(LazA11yElement* a, LazA11yElement* b, bool* out);

/** Creates an independent handle to the same element. */
LAZ_EXPORT int LAZ_CALL lazA11yCloneHandle(LazA11yElement* element, LazA11yElement** out);

/** Releases a handle. Accepts NULL. Can be called from any thread. */
LAZ_EXPORT void LAZ_CALL lazA11yRelease(LazA11yElement* element);

#ifdef __cplusplus
}
#endif

#endif // LAZ_A11Y_H
