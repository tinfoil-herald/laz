// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef LAZ_API_H
#define LAZ_API_H

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
