// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <ApplicationServices/ApplicationServices.h>
#import <Foundation/Foundation.h>
#include "key_code.h"
#include "keycode.h"
#include "mac_common.h"

static void postKeyEvent(CGKeyCode keyCode, bool isDown) {
  performOnMainThread(^{
    CGEventSourceRef source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
    CGEventRef event = CGEventCreateKeyboardEvent(source, keyCode, isDown);
    if (event != nullptr) {
      CGEventFlags flags = CGEventSourceFlagsState(kCGEventSourceStateHIDSystemState);
      if ((flags & kCGEventFlagMaskSecondaryFn) != 0) {
        flags ^= kCGEventFlagMaskSecondaryFn;
        CGEventSetFlags(event, flags);
      }
      CGEventPost(kCGHIDEventTap, event);
      CFRelease(event);
    }
    if (source != nullptr) {
      CFRelease(source);
    }
  });
}

extern "C" {
// Send key event using OS-agnostic key code.
// Maps to Darwin keycode and posts the event.
void sendKeyPress(VirtualKeyCode keyCode) {
  autoDelay();

  int macKeyCode = getMacKeyCode(keyCode);
  if (macKeyCode == kVK_Undefined) {
    return;
  }

  postKeyEvent((CGKeyCode)macKeyCode, true);

  // CGEventPost is asynchronous. Wait until the system reports the key as down
  // before returning so that key combinations (e.g. Ctrl+Alt+H) are applied in
  // the order they were requested.
  waitUntil(^bool {
    return CGEventSourceKeyState(kCGEventSourceStateHIDSystemState, (CGKeyCode)macKeyCode);
  }, EVENT_CONFIRM_TIMEOUT_SECONDS);
}

void sendKeyRelease(VirtualKeyCode keyCode) {
  autoDelay();

  int macKeyCode = getMacKeyCode(keyCode);
  if (macKeyCode == kVK_Undefined) {
    return;
  }

  postKeyEvent((CGKeyCode)macKeyCode, false);

  // Wait until the system reports the key as up so a subsequent command isn't
  // issued while this key is still considered pressed.
  waitUntil(^bool {
    return !CGEventSourceKeyState(kCGEventSourceStateHIDSystemState, (CGKeyCode)macKeyCode);
  }, EVENT_CONFIRM_TIMEOUT_SECONDS);
}
}
