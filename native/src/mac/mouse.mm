// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#import <AppKit/AppKit.h>
#include <ApplicationServices/ApplicationServices.h>
#import <Foundation/Foundation.h>
#include <array>
#include "mac_common.h"
#include "mouse_button.h"
#include "point.h"

// =============================================================================
// Event Number and Click Count Handling
// =============================================================================
//
// Synthetic mouse events require proper event numbers and click counts to work
// correctly with macOS window management and application event handling.
//
// EVENT NUMBERS:
// Apple docs don't specify requirements for event numbers, but they must be
// higher than real hardware event numbers (which start near zero). Without
// proper event numbers, z-order issues occur (e.g., clicking doesn't bring
// windows to front, menu bar doesn't switch between apps).
//
// References:
// -
// https://stackoverflow.com/questions/2602224/synthetic-click-doesnt-switch-applications-menu-bar-mac-os-x
// -
// https://macosx-port-dev.openjdk.java.narkive.com/EjxSMEOA/review-request-for-macosx-port-651-modal-behavior-difference-with-and-without-robot-interaction
//
// CLICK COUNTS:
// macOS applications expect proper click counts for double-click, triple-click
// detection. getClickCount() automatically detects multi-clicks by measuring
// time between mouse events and comparing to system's double-click interval.
// This allows applications to receive proper double-click events without the
// caller explicitly tracking timing.
// =============================================================================

// Starting number for synthetic event numbers. Value 32000 is empirically
// chosen to be higher than real hardware events (JDK uses this value).
static const int EVENT_NUMBER_START = 32000;

// Click count state - shared across all buttons for simplicity
// (In macOS, left/right share click count, middle is separate, but we simplify)
static int g_clickCount = 0;
static NSTimeInterval g_lastClickTime = 0;

// Event number tracking (left, middle, right)
static int g_nextEventNumber = EVENT_NUMBER_START;
static std::array<int, 3> g_buttonEventNumbers = {EVENT_NUMBER_START, EVENT_NUMBER_START,
                                                  EVENT_NUMBER_START};

// Calculate click count based on timing between mouse events.
// Uses system's double-click interval from System Preferences.
static int getClickCount(bool isDown) {
  NSTimeInterval now = [[NSDate date] timeIntervalSinceReferenceDate];
  NSTimeInterval clickInterval = now - g_lastClickTime;
  bool isWithinThreshold = clickInterval < [NSEvent doubleClickInterval];

  if (isDown) {
    if (isWithinThreshold) {
      g_clickCount++;
    } else {
      g_clickCount = 1;
    }
    g_lastClickTime = now;
  } else {
    // Mouse up has click count of last mouse down if within threshold, else 0
    if (!isWithinThreshold) {
      g_clickCount = 0;
    }
  }

  return g_clickCount;
}

// Get button index for event number tracking (0=left, 1=middle, 2=right)
static size_t getButtonIndex(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return 0;
    case MOUSE_BUTTON_MIDDLE:
      return 1;
    case MOUSE_BUTTON_SECONDARY:
      return 2;
    default:
      return 0;
  }
}

// Helper to get CGMouseButton from MouseButton
static CGMouseButton getCGButton(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return kCGMouseButtonLeft;
    case MOUSE_BUTTON_SECONDARY:
      return kCGMouseButtonRight;
    case MOUSE_BUTTON_MIDDLE:
      return kCGMouseButtonCenter;
    default:
      return kCGMouseButtonLeft;
  }
}

// Helper to get button down event type
static CGEventType getButtonDownEventType(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return kCGEventLeftMouseDown;
    case MOUSE_BUTTON_SECONDARY:
      return kCGEventRightMouseDown;
    case MOUSE_BUTTON_MIDDLE:
      return kCGEventOtherMouseDown;
    default:
      return kCGEventLeftMouseDown;
  }
}

// Helper to get button up event type
static CGEventType getButtonUpEventType(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return kCGEventLeftMouseUp;
    case MOUSE_BUTTON_SECONDARY:
      return kCGEventRightMouseUp;
    case MOUSE_BUTTON_MIDDLE:
      return kCGEventOtherMouseUp;
    default:
      return kCGEventLeftMouseUp;
  }
}

// Helper to get drag event type (when button is held during move)
static CGEventType getDragEventType(MouseButton button) {
  switch (button) {
    case MOUSE_BUTTON_PRIMARY:
      return kCGEventLeftMouseDragged;
    case MOUSE_BUTTON_SECONDARY:
      return kCGEventRightMouseDragged;
    case MOUSE_BUTTON_MIDDLE:
      return kCGEventOtherMouseDragged;
    default:
      return kCGEventLeftMouseDragged;
  }
}

extern "C" {
void sendMouseDown(int x, int y, MouseButton button) {
  autoDelay();

  CGPoint point = CGPointMake(x, y);
  CGMouseButton cgButton = getCGButton(button);
  CGEventType eventType = getButtonDownEventType(button);
  size_t buttonIndex = getButtonIndex(button);

  performOnMainThread(^{
    int eventClickCount = getClickCount(true);
    g_buttonEventNumbers[buttonIndex] = g_nextEventNumber++;
    int eventNumber = g_buttonEventNumbers[buttonIndex];

    CGEventSourceRef source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
    CGEventRef event = CGEventCreateMouseEvent(source, eventType, point, cgButton);
    if (event != nullptr) {
      CGEventSetIntegerValueField(event, kCGMouseEventClickState, eventClickCount);
      CGEventSetIntegerValueField(event, kCGMouseEventNumber, eventNumber);
      CGEventPost(kCGHIDEventTap, event);
      CFRelease(event);
    }
    if (source != nullptr) {
      CFRelease(source);
    }
  });
}

void sendMouseUp(int x, int y, MouseButton button) {
  autoDelay();

  CGPoint point = CGPointMake(x, y);
  CGMouseButton cgButton = getCGButton(button);
  CGEventType eventType = getButtonUpEventType(button);

  size_t buttonIndex = getButtonIndex(button);

  performOnMainThread(^{
    int eventClickCount = getClickCount(false);
    int eventNumber = g_buttonEventNumbers[buttonIndex];

    CGEventSourceRef source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
    CGEventRef event = CGEventCreateMouseEvent(source, eventType, point, cgButton);
    if (event != nullptr) {
      CGEventSetIntegerValueField(event, kCGMouseEventClickState, eventClickCount);
      CGEventSetIntegerValueField(event, kCGMouseEventNumber, eventNumber);
      CGEventPost(kCGHIDEventTap, event);
      CFRelease(event);
    }
    if (source != nullptr) {
      CFRelease(source);
    }
  });
}

void sendMouseMove(int x, int y, MouseButton pressedButton, bool isButtonDown) {
  // Update auto-delay timer for moves (skip sleep, but next click must wait 50ms after last move)
  autoDelay(true);

  CGPoint point = CGPointMake(x, y);
  CGMouseButton cgButton = getCGButton(pressedButton);
  CGEventType eventType = isButtonDown ? getDragEventType(pressedButton) : kCGEventMouseMoved;

  performOnMainThread(^{
    // Mouse movement resets click count (double-clicks happen at same location).
    g_lastClickTime = 0;

    CGEventSourceRef source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
    CGEventRef event = CGEventCreateMouseEvent(source, eventType, point, cgButton);
    if (event != nullptr) {
      // Click count is 0 for move/drag events.
      CGEventSetIntegerValueField(event, kCGMouseEventClickState, 0);
      CGEventSetIntegerValueField(event, kCGMouseEventNumber, g_nextEventNumber);
      CGEventPost(kCGHIDEventTap, event);
      CFRelease(event);
    }
    if (source != nullptr) {
      CFRelease(source);
    }
  });
}

NativePoint getMousePosition() {
  __block NativePoint result = {0, 0};
  performOnMainThread(^{
    CGEventRef event = CGEventCreate(nullptr);
    if (event != nullptr) {
      CGPoint loc = CGEventGetLocation(event);
      result.x = (int)loc.x;
      result.y = (int)loc.y;
      CFRelease(event);
    }
  });
  return result;
}

void sendScrollEvent(int notches) {
  autoDelay();

  // Convert notches to lines. One notch (wheel click) typically scrolls ~3 lines.
  // This matches Windows default behavior and provides cross-platform consistency.
  // macOS doesn't expose a system setting for this, so we use a fixed value.
  const int linesPerNotch = 3;
  int lines = notches * linesPerNotch;

  // Uses NULL source for isolated events that don't blend with physical input state.
  // kCGScrollEventUnitLine scrolls by logical lines (like mouse wheel), not pixels.
  performOnMainThread(^{
    CGEventSourceRef source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
    CGEventRef event = CGEventCreateScrollWheelEvent(source, kCGScrollEventUnitLine, 1, lines);
    if (event != nullptr) {
      CGEventPost(kCGHIDEventTap, event);
      CFRelease(event);
    }
    if (source != nullptr) {
      CFRelease(source);
    }
  });
}
}
