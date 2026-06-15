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

void autoDelay() {
  NSTimeInterval now = [[NSDate date] timeIntervalSinceReferenceDate];
  NSTimeInterval delay = g_nextEventTime - now;
  if (delay > 0) {
    [NSThread sleepForTimeInterval:delay];
  }
  g_nextEventTime = now + EVENT_DELAY_SECONDS;
}
