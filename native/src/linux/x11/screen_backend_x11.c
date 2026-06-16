// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <pthread.h>
#include <stdint.h>
#include <string.h>

#include "screen_backend.h"

static Display *g_display = NULL;
static pthread_once_t g_displayOnce = PTHREAD_ONCE_INIT;

static void initDisplay(void) {
  XInitThreads();
  g_display = XOpenDisplay(NULL);
}

static Display *getDisplay(void) {
  pthread_once(&g_displayOnce, initDisplay);
  return g_display;
}

// Returns the bit offset of a channel mask (e.g. 0x00FF00 -> 8).
static int maskShift(unsigned long mask) {
  int shift = 0;
  while (mask && !(mask & 1)) {
    mask >>= 1;
    shift++;
  }
  return shift;
}

// Pre-fills buffer with opaque black so pixels out of the screen are not noise.
// BGRA layout: [0x00, 0x00, 0x00, 0xFF] per pixel.
static void fillBlackOpaque(uint8_t *buffer, int width, int height) {
  const size_t totalBytes = (size_t)width * (size_t)height * 4;
  memset(buffer, 0, totalBytes);
  for (size_t i = 3; i < totalBytes; i += 4) {
    buffer[i] = 255;
  }
}

LAZ_EXPORT bool lazScreenCapture(int x, int y, int width, int height, void *buffer) {
  if (!buffer || width <= 0 || height <= 0) return false;

  Display *display = getDisplay();
  if (!display) {
    return false;
  }

  const int screen = DefaultScreen(display);
  const Window root = RootWindow(display, screen);
  const int screenW = DisplayWidth(display, screen);
  const int screenH = DisplayHeight(display, screen);

  fillBlackOpaque((uint8_t *)buffer, width, height);

  // Clamp capture rect to screen bounds.
  int srcX = x;
  int srcY = y;
  int dstX = 0;
  int dstY = 0;

  if (srcX < 0) {
    dstX = -srcX;
    srcX = 0;
  }
  if (srcY < 0) {
    dstY = -srcY;
    srcY = 0;
  }

  int srcW = width - dstX;
  int srcH = height - dstY;

  if (srcX + srcW > screenW) srcW = screenW - srcX;
  if (srcY + srcH > screenH) srcH = screenH - srcY;

  if (srcW <= 0 || srcH <= 0) {
    return true;
  }

  XImage *image = XGetImage(display, root, srcX, srcY, (unsigned int)srcW, (unsigned int)srcH,
                            AllPlanes, ZPixmap);
  if (!image) {
    return false;
  }

  const int rshift = maskShift(image->red_mask);
  const int gshift = maskShift(image->green_mask);
  const int bshift = maskShift(image->blue_mask);

  for (int row = 0; row < srcH; row++) {
    for (int col = 0; col < srcW; col++) {
      const unsigned long pixel = XGetPixel(image, col, row);
      const unsigned char r = (pixel >> rshift) & 0xFF;
      const unsigned char g = (pixel >> gshift) & 0xFF;
      const unsigned char b = (pixel >> bshift) & 0xFF;

      const int outX = dstX + col;
      const int outY = dstY + row;
      const size_t idx = ((size_t)outY * (size_t)width + (size_t)outX) * 4;
      uint8_t *out = (uint8_t *)buffer;
      out[idx + 0] = b;
      out[idx + 1] = g;
      out[idx + 2] = r;
      out[idx + 3] = 255;
    }
  }

  XDestroyImage(image);
  return true;
}

LAZ_EXPORT NativeColor lazGetPixelColor(int x, int y) {
  NativeColor color = {0, 0, 0, 255};

  Display *display = getDisplay();
  if (!display) {
    return color;
  }

  const int screen = DefaultScreen(display);
  const int screenW = DisplayWidth(display, screen);
  const int screenH = DisplayHeight(display, screen);

  if (x < 0 || y < 0 || x >= screenW || y >= screenH) {
    return color;
  }

  const Window root = RootWindow(display, screen);
  XImage *image = XGetImage(display, root, x, y, 1, 1, AllPlanes, ZPixmap);
  if (!image) {
    return color;
  }

  const unsigned long pixel = XGetPixel(image, 0, 0);
  const int rshift = maskShift(image->red_mask);
  const int gshift = maskShift(image->green_mask);
  const int bshift = maskShift(image->blue_mask);
  color.r = (pixel >> rshift) & 0xFF;
  color.g = (pixel >> gshift) & 0xFF;
  color.b = (pixel >> bshift) & 0xFF;
  color.a = 255;

  XDestroyImage(image);
  return color;
}
