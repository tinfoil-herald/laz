// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#import <Foundation/Foundation.h>
#import <ScreenCaptureKit/ScreenCaptureKit.h>
#include <dispatch/dispatch.h>
#include <atomic>
#include <cstring>
#include <memory>
#include "laz_api.h"

// Shared between the caller and the async block to coordinate timeout cancellation.
struct CaptureState {
  std::atomic_bool cancelled{false};
  bool success{false};
};

extern "C" {
// Captures a screen region into a pre-allocated BGRA buffer using ScreenCaptureKit.
// Requires macOS 14.0+ and Screen Recording permission.
bool captureScreen(int x, int y, int width, int height, void* buffer) {
  if (buffer == nullptr || width <= 0 || height <= 0) {
    return false;
  }
  if (width > LAZ_MAX_CAPTURE_DIMENSION || height > LAZ_MAX_CAPTURE_DIMENSION) {
    return false;
  }

  auto state = std::make_shared<CaptureState>();
  const auto sharedState = state;
  dispatch_semaphore_t semaphore = dispatch_semaphore_create(0);

  [SCShareableContent
      getShareableContentWithCompletionHandler:^(SCShareableContent* content, NSError* error) {
        if (error != nil || content == nil || content.displays.count == 0) {
          dispatch_semaphore_signal(semaphore);
          return;
        }

        SCDisplay* targetDisplay = nil;
        CGRect captureRect = CGRectMake(x, y, width, height);

        for (SCDisplay* display in content.displays) {
          if (CGRectIntersectsRect(display.frame, captureRect)) {
            targetDisplay = display;
            break;
          }
        }

        if (targetDisplay == nil) {
          targetDisplay = content.displays.firstObject;
        }

        SCContentFilter* filter = [[SCContentFilter alloc] initWithDisplay:targetDisplay
                                                          excludingWindows:@[]];

        SCStreamConfiguration* config = [[SCStreamConfiguration alloc] init];
        config.sourceRect = captureRect;
        config.width = (size_t)width;
        config.height = (size_t)height;
        config.pixelFormat = kCVPixelFormatType_32BGRA;
        config.colorSpaceName = kCGColorSpaceSRGB;
        config.showsCursor = YES;

        [SCScreenshotManager
            captureImageWithFilter:filter
                     configuration:config
                 completionHandler:^(CGImageRef image, NSError* captureError) {
                   if (captureError != nil || image == nullptr) {
                     dispatch_semaphore_signal(semaphore);
                     return;
                   }

                   // Render into a private buffer, then copy to the caller's buffer only
                   // if the wait hasn't timed out. This avoids writing into memory the
                   // caller may have already freed or reused.
                   const size_t stride = (size_t)width * 4;
                   const size_t bytes = stride * (size_t)height;
                   auto* temp = static_cast<uint8_t*>(malloc(bytes));
                   if (temp == nullptr) {
                     dispatch_semaphore_signal(semaphore);
                     return;
                   }

                   CGColorSpaceRef colorSpace = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
                   if (colorSpace == nullptr) {
                     free(temp);
                     dispatch_semaphore_signal(semaphore);
                     return;
                   }

                   // NoneSkipFirst: ignore alpha, always write 0xFF. Avoids premultiplied
                   // RGB values that would corrupt colors for semi-transparent pixels.
                   CGContextRef context = CGBitmapContextCreate(
                       temp, (size_t)width, (size_t)height, 8, stride, colorSpace,
                       static_cast<CGBitmapInfo>(kCGBitmapByteOrder32Little) |
                           kCGImageAlphaNoneSkipFirst);

                   if (context != nullptr) {
                     CGContextDrawImage(context, CGRectMake(0, 0, width, height), image);
                     CGContextRelease(context);

                     if (!sharedState->cancelled.load(std::memory_order_acquire)) {
                       memcpy(buffer, temp, bytes);
                       sharedState->success = true;
                     }
                   }

                   free(temp);
                   CGColorSpaceRelease(colorSpace);
                   dispatch_semaphore_signal(semaphore);
                 }];
      }];

  // Synchronous wait is intentional: ScreenCaptureKit has no synchronous API.
  long waitResult =
      dispatch_semaphore_wait(  // NOLINT(clang-analyzer-optin.performance.GCDAntipattern)
          semaphore, dispatch_time(DISPATCH_TIME_NOW, 5 * NSEC_PER_SEC));
  if (waitResult != 0) {
    state->cancelled.store(true, std::memory_order_release);
    return false;
  }

  return state->success;
}

NativeColor getPixelColor(int x, int y) {
  NativeColor color = {0, 0, 0, 255};
  unsigned char pixel[4];

  if (captureScreen(x, y, 1, 1, pixel)) {
    color.b = pixel[0];
    color.g = pixel[1];
    color.r = pixel[2];
    color.a = pixel[3];
  }

  return color;
}
}
