// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef MAC_COMMON_H
#define MAC_COMMON_H

#import <Foundation/Foundation.h>

// Minimum delay between non-move events (50ms matches JDK behavior)
// Reference: https://bugs.openjdk.org/browse/JDK-8242174
static const double EVENT_DELAY_SECONDS = 0.050;

// Upper bound to wait for a posted key/button event to be reflected in the
// system input state before giving up and proceeding anyway. In practice
// confirmation succeeds within the first few poll iterations; this is only a
// safety cap so a call can never hang.
static const double EVENT_CONFIRM_TIMEOUT_SECONDS = 0.100;

// Upper bound to wait for the cursor to reach a requested position. Kept short
// because a reachable, on-screen target is confirmed almost immediately and we
// don't want to stall dense movement paths on an unreachable (clamped) target.
static const double MOVE_CONFIRM_TIMEOUT_SECONDS = 0.050;

// Poll interval for move confirmation checks. This is intentionally looser than
// key/button confirmation polling because move sequences can be dense and the
// window server updates cursor position asynchronously.
static const double MOVE_CONFIRM_POLL_INTERVAL_SECONDS = 0.002;  // 2 ms

// Execute block on main thread synchronously.
// CG event APIs must be called from the main thread for reliable behavior.
void performOnMainThread(void (^block)(void));

// Polls `condition` on the calling thread until it returns true or
// `timeoutSeconds` elapses. Uses `pollIntervalSeconds` between checks.
bool waitUntilWithPollInterval(bool (^condition)(void), double timeoutSeconds,
                               double pollIntervalSeconds);

// Polls `condition` on the calling thread until it returns true or
// `timeoutSeconds` elapses. Returns true if the condition became true, false on
// timeout.
//
// CGEventPost is asynchronous: the window server processes injected events on
// its own schedule, so a call can return before the event has taken effect.
// After posting, we wait until the resulting system state (key/button/cursor)
// reflects the change, which makes consecutive commands execute in order (e.g.
// the modifiers of a key combination, or a click that must land only after a
// preceding move has completed).
bool waitUntil(bool (^condition)(void), double timeoutSeconds);

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
