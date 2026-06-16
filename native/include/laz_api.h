// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_API_H
#define LAZ_API_H

// Maximum allowed screen capture dimension (width or height) in pixels.
// Caps are enforced in native code to prevent integer overflow in buffer size
// calculations (width * height * 4). 32768 is generous enough for tiled 8K
// multi-monitor setups while keeping all arithmetic safe in size_t.
#define LAZ_MAX_CAPTURE_DIMENSION 32768

#if defined(_WIN32)
    #include "laz_api_windows.h"
#elif defined(__APPLE__)
    #include "laz_api_macos.h"
#elif defined(__linux__)
    #include "laz_api_linux.h"
#else
    #error "Unsupported platform."
#endif

#endif // LAZ_API_H
