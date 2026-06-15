// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef MAC_COMMON_H
#define MAC_COMMON_H

#import <Foundation/Foundation.h>

// Minimum delay between non-move events (50ms matches JDK behavior)
// Reference: https://bugs.openjdk.org/browse/JDK-8242174
static const double EVENT_DELAY_SECONDS = 0.050;

// Execute block on main thread synchronously.
// CG event APIs must be called from the main thread for reliable behavior.
void performOnMainThread(void (^block)(void));

// Ensures minimum delay between synthetic input events.
//
// macOS processes CGEvents asynchronously - events are posted to a system queue
// and processed later by the window server. Without delays between events:
// - Internal state (focused window, modifier keys, click count) may not update
//   before the next event arrives
// - Rapid events may be coalesced or dropped
// - Window focus changes may not complete, causing clicks to go to wrong window
//
// This is NOT needed for mouse moves (losing some is acceptable), but is
// required for clicks, key presses, and scroll events that must be reliably
// processed in order.
//
// Windows does not need this because SendInput is synchronous.
void autoDelay(void);

#endif  // MAC_COMMON_H
