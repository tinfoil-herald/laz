// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <ApplicationServices/ApplicationServices.h>
#import <Foundation/Foundation.h>
#include <dispatch/dispatch.h>
#include "mac_common.h"

// Auto-delay state
static NSTimeInterval g_nextEventTime = 0;

void performOnMainThread(void (^block)(void)) {
  if ([NSThread isMainThread]) {
    block();
  } else {
    dispatch_sync(dispatch_get_main_queue(), block);
  }
}

bool waitUntilWithPollInterval(bool (^condition)(void), double timeoutSeconds,
                               double pollIntervalSeconds) {
  static const NSTimeInterval kMinPollInterval = 0.0005;  // 0.5 ms
  NSTimeInterval pollInterval = pollIntervalSeconds > 0 ? pollIntervalSeconds : kMinPollInterval;

  const NSTimeInterval deadline = [NSDate timeIntervalSinceReferenceDate] + timeoutSeconds;
  while (!condition()) {
    const NSTimeInterval now = [NSDate timeIntervalSinceReferenceDate];
    const NSTimeInterval remaining = deadline - now;
    if (remaining <= 0) {
      return false;
    }
    [NSThread sleepForTimeInterval:MIN(pollInterval, remaining)];
  }
  return true;
}

bool waitUntil(bool (^condition)(void), double timeoutSeconds) {
  // Short poll interval keeps latency low while confirmation typically succeeds
  // within the first few iterations.
  static const NSTimeInterval kDefaultPollInterval = 0.0005;  // 0.5 ms
  return waitUntilWithPollInterval(condition, timeoutSeconds, kDefaultPollInterval);
}

void autoDelay() {
  NSTimeInterval now = [[NSDate date] timeIntervalSinceReferenceDate];
  NSTimeInterval delay = g_nextEventTime - now;
  if (delay > 0) {
    [NSThread sleepForTimeInterval:delay];
  }
  g_nextEventTime = now + EVENT_DELAY_SECONDS;
}
