// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#import <Cocoa/Cocoa.h>
#include "laz_api.h"

bool setClipboardText(const char* text) {
    if (text == nullptr) {
        return false;
    }

    @autoreleasepool {
        NSString* string = [NSString stringWithUTF8String:text];
        if (string == nil) {
            return false;
        }

        NSPasteboard* pasteboard = [NSPasteboard generalPasteboard];
        [pasteboard clearContents];
        return [pasteboard setString:string forType:NSPasteboardTypeString];
    }
}
