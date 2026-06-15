// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <stdbool.h>
#include <string.h>
#include <windows.h>

#include "laz_api.h"

LAZ_EXPORT bool LAZ_CALL captureScreen(int x, int y, int width, int height, void* buffer) {
  if (buffer == NULL || width <= 0 || height <= 0) {
    return false;
  }

  bool success = false;

  // Create DC for the entire virtual screen (all monitors).
  HDC hdcScreen = CreateDC(TEXT("DISPLAY"), NULL, NULL, NULL);
  if (hdcScreen == NULL) {
    return false;
  }

  // Create memory DC and bitmap for offscreen rendering.
  HDC hdcMem = CreateCompatibleDC(hdcScreen);
  if (hdcMem == NULL) {
    DeleteDC(hdcScreen);
    return false;
  }

  HBITMAP hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
  if (hBitmap == NULL) {
    DeleteDC(hdcMem);
    DeleteDC(hdcScreen);
    return false;
  }

  HBITMAP hOldBitmap = (HBITMAP)SelectObject(hdcMem, hBitmap);

  // Copy screen content to our bitmap.
  // CAPTUREBLT includes layered/transparent windows (Win2K+).
  if (BitBlt(hdcMem, 0, 0, width, height, hdcScreen, x, y, SRCCOPY | CAPTUREBLT)) {
    // Setup BITMAPINFO for 32-bit top-down DIB.
    // Top-down (negative height) so row 0 is at the top, matching expected buffer layout.
    BITMAPINFO bmi;
    memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = width;
    bmi.bmiHeader.biHeight = -height;  // Negative = top-down DIB
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;  // Uncompressed, BGRX format

    // Get bitmap bits directly into caller's buffer.
    // With BI_RGB and 32-bit, Windows returns BGRX (X = unused, typically 0).
    int scanlines = GetDIBits(hdcMem, hBitmap, 0, height, buffer, &bmi, DIB_RGB_COLORS);

    if (scanlines == height) {
      // Set alpha channel to 255 (opaque) for all pixels.
      // Windows returns BGRX, we need BGRA with A=255.
      unsigned char* pixels = (unsigned char*)buffer;
      int totalPixels = width * height;
      for (int i = 0; i < totalPixels; i++) {
        pixels[i * 4 + 3] = 255;  // Set alpha byte
      }
      success = true;
    }
  }

  // Cleanup GDI objects.
  SelectObject(hdcMem, hOldBitmap);
  DeleteObject(hBitmap);
  DeleteDC(hdcMem);
  DeleteDC(hdcScreen);

  return success;
}

LAZ_EXPORT NativeColor LAZ_CALL getPixelColor(int x, int y) {
  NativeColor color = {0, 0, 0, 255};
  unsigned char pixel[4];

  if (captureScreen(x, y, 1, 1, pixel)) {
    // Buffer is BGRA, convert to NativeColor (RGBA struct).
    color.b = pixel[0];
    color.g = pixel[1];
    color.r = pixel[2];
    color.a = pixel[3];
  }

  return color;
}
