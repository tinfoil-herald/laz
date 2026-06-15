// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_SCREEN_BACKEND_H
#define LAZ_SCREEN_BACKEND_H

#include <stdbool.h>

#include "color.h"

#define LAZ_SCREEN_BACKEND_CAPTURE_SYMBOL "lazScreenCapture"
#define LAZ_SCREEN_BACKEND_PIXEL_SYMBOL "lazGetPixelColor"

#define LAZ_EXPORT __attribute__((visibility("default")))

typedef bool (*LazCaptureFn)(int x, int y, int width, int height, void* buffer);
typedef NativeColor (*LazGetPixelFn)(int x, int y);

#endif  // LAZ_SCREEN_BACKEND_H
