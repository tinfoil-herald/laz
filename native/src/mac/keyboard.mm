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
  if (macKeyCode != kVK_Undefined) {
    postKeyEvent((CGKeyCode)macKeyCode, true);
  }
}

void sendKeyRelease(VirtualKeyCode keyCode) {
  autoDelay();

  int macKeyCode = getMacKeyCode(keyCode);
  if (macKeyCode != kVK_Undefined) {
    postKeyEvent((CGKeyCode)macKeyCode, false);
  }
}
}
