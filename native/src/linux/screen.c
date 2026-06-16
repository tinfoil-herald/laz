// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// NOLINTNEXTLINE(bugprone-reserved-identifier) - required for dladdr
#define _GNU_SOURCE
#include <dlfcn.h>
#include <libgen.h>
#include <pthread.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#include "laz_api.h"
#include "screen_backend.h"

typedef enum { DS_UNKNOWN, DS_X11, DS_WAYLAND, DS_XWAYLAND } DisplayServer;

/**
 * Detects the current display server type.
 *
 * @return The detected display server type
 */
static DisplayServer detectDisplayServer(void) {
  const char* sessionType = getenv("XDG_SESSION_TYPE");

  if (sessionType == NULL) {
    if (getenv("WAYLAND_DISPLAY") != NULL) {
      return getenv("DISPLAY") != NULL ? DS_XWAYLAND : DS_WAYLAND;
    }
    if (getenv("DISPLAY") != NULL) {
      return DS_X11;
    }
    return DS_UNKNOWN;
  }

  if (strcmp(sessionType, "x11") == 0) {
    return DS_X11;
  }

  if (strcmp(sessionType, "wayland") == 0) {
    return getenv("DISPLAY") != NULL ? DS_XWAYLAND : DS_WAYLAND;
  }

  return DS_UNKNOWN;
}

/**
 * Either PipeWire or X11 backend that actually implements screen-related methods.
 *
 * Since either PipeWire or X11 can be missing in the environment, we need such a contraption
 * to load them dynamically.
 **/
typedef struct {
  void* handle;
  // The pointer to the captureScreen() implementation.
  LazCaptureFn capture;
  // The pointer to the getPixelColor() implementation.
  LazGetPixelFn getPixel;
} ScreenBackend;

static ScreenBackend g_backend = {0};
static DisplayServer g_loadedDs = DS_UNKNOWN;
static pthread_mutex_t g_backendMutex = PTHREAD_MUTEX_INITIALIZER;

static const char* getBackendLibraryName(DisplayServer ds) {
  switch (ds) {
    case DS_X11:
      return "laz_screen_x11.so";
    case DS_WAYLAND:
    case DS_XWAYLAND:
      return "laz_screen_wayland.so";
    default:
      return NULL;
  }
}

// Get the directory where this library (laz_native.so) is located
static char* getSelfDir(void) {
  Dl_info info;
  // POSIX mandates dladdr takes void*, but only provides function addresses to query.
  // This cast is unavoidable and safe on all POSIX systems.
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
  if (dladdr((void*)getSelfDir, &info) == 0 || info.dli_fname == NULL) {
#pragma GCC diagnostic pop
    return NULL;
  }
  char* path = strdup(info.dli_fname);
  if (path == NULL) {
    return NULL;
  }
  const char* dir = dirname(path);
  char* result = strdup(dir);
  free(path);
  return result;
}

static void unloadBackend(void) {
  if (g_backend.handle != NULL) {
    dlclose(g_backend.handle);
  }
  g_backend.handle = NULL;
  g_backend.capture = NULL;
  g_backend.getPixel = NULL;
  g_loadedDs = DS_UNKNOWN;
}

static bool loadBackend(DisplayServer ds) {
  const char* soName = getBackendLibraryName(ds);
  if (soName == NULL) {
    return false;
  }

  if (g_backend.handle != NULL && g_loadedDs == ds) {
    return true;
  }

  if (g_backend.handle != NULL && g_loadedDs != ds) {
    unloadBackend();
  }

  char* selfDir = getSelfDir();
  char fullPath[4096];
  if (selfDir != NULL) {
    snprintf(fullPath, sizeof(fullPath), "%s/%s", selfDir, soName);
    free(selfDir);
  } else {
    // Fallback to just the name.
    snprintf(fullPath, sizeof(fullPath), "%s", soName);
  }

  void* handle = dlopen(fullPath, RTLD_LAZY | RTLD_LOCAL);
  if (handle == NULL) {
    return false;
  }

  // ISO C forbids casting void* to a function pointer; this aliasing trick avoids
  // the compiler warning while relying on POSIX-guaranteed compatible representation.
  LazCaptureFn capture;
  *(void**)&capture = dlsym(handle, LAZ_SCREEN_BACKEND_CAPTURE_SYMBOL);
  LazGetPixelFn getPixel;
  *(void**)&getPixel = dlsym(handle, LAZ_SCREEN_BACKEND_PIXEL_SYMBOL);

  if (capture == NULL) {
    dlclose(handle);
    return false;
  }

  g_backend.handle = handle;
  g_backend.capture = capture;
  g_backend.getPixel = getPixel;
  g_loadedDs = ds;
  return true;
}

bool captureScreen(int x, int y, int width, int height, void* buffer) {
  if (buffer == NULL) {
    return false;
  }

  if (width <= 0 || height <= 0) {
    return false;
  }
  if (width > LAZ_MAX_CAPTURE_DIMENSION || height > LAZ_MAX_CAPTURE_DIMENSION) {
    return false;
  }

  const DisplayServer ds = detectDisplayServer();

  pthread_mutex_lock(&g_backendMutex);
  if (!loadBackend(ds)) {
    pthread_mutex_unlock(&g_backendMutex);
    return false;
  }
  LazCaptureFn capture = g_backend.capture;
  pthread_mutex_unlock(&g_backendMutex);

  return capture(x, y, width, height, buffer);
}

NativeColor getPixelColor(int x, int y) {
  NativeColor color = {0, 0, 0, 255};
  const DisplayServer ds = detectDisplayServer();

  pthread_mutex_lock(&g_backendMutex);
  if (!loadBackend(ds)) {
    pthread_mutex_unlock(&g_backendMutex);
    return color;
  }
  LazGetPixelFn getPixel = g_backend.getPixel;
  LazCaptureFn capture = g_backend.capture;
  pthread_mutex_unlock(&g_backendMutex);

  if (getPixel != NULL) {
    return getPixel(x, y);
  }

  unsigned char pixel[4];
  if (capture(x, y, 1, 1, pixel)) {
    color.r = pixel[2];
    color.g = pixel[1];
    color.b = pixel[0];
    color.a = pixel[3];
  }

  return color;
}
