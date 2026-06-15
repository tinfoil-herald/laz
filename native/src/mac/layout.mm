// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#import <Foundation/Foundation.h>
#import <Carbon/Carbon.h>
#import <CoreGraphics/CoreGraphics.h>
#include "laz_api_macos.h"
#include "mac_common.h"

// Cached keyboard layout data. We retain the TISInputSourceRef so the
// UCKeyboardLayout pointer (obtained via TISGetInputSourceProperty) stays valid.
// The C# side caches by layout ID and only calls probeKeyOutput when the layout
// changes, so we fetch once in getKeyboardLayoutId() and reuse in probeKeyOutput().
static TISInputSourceRef g_cachedSource = nullptr;
static const UCKeyboardLayout* g_cachedLayout = nullptr;

// TIS functions must be dispatched via performOnMainThread (same as keyboard/mouse
// events) to avoid deadlocks with the .NET thread pool.

int getKeyboardLayoutId() {
    __block int hash = 0;

    performOnMainThread(^{
        auto *source = TISCopyCurrentKeyboardLayoutInputSource();
        if (source == nullptr) {
            return;
        }

        const auto *sourceId = (CFStringRef)TISGetInputSourceProperty(source, kTISPropertyInputSourceID);
        hash = sourceId != nullptr ? (int)CFHash(sourceId) : 0;

        // Cache the layout data while we have the source, so probeKeyOutput
        // can use it without additional TIS calls.
        const auto *layoutData = (CFDataRef)TISGetInputSourceProperty(source, kTISPropertyUnicodeKeyLayoutData);
        if (layoutData != nullptr) {
            if (g_cachedSource != nullptr) {
                CFRelease(g_cachedSource);
            }
            g_cachedSource = source;
            g_cachedLayout = (const UCKeyboardLayout*)CFDataGetBytePtr(layoutData);
        } else {
            CFRelease(source);
        }
    });

    return hash;
}

int probeKeyOutput(int macKeyCode, int modifierFlags, uint32_t* deadKeyState,
                   uint16_t* outChars, int maxChars) {
    if (g_cachedLayout == nullptr) {
        return 0;
    }

    UniCharCount actualLength = 0;
    OSStatus status = UCKeyTranslate(
        g_cachedLayout,
        (UInt16)macKeyCode,
        kUCKeyActionDown,
        (UInt32)modifierFlags,
        LMGetKbdType(),
        0,
        deadKeyState,
        (UniCharCount)maxChars,
        &actualLength,
        outChars
    );

    if (status != noErr) {
        return 0;
    }

    if (*deadKeyState != 0) {
        return -1;
    }

    return (int)actualLength;
}
