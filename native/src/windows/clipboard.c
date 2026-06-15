// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <stdbool.h>
#include <windows.h>

#include "laz_api.h"

LAZ_EXPORT bool LAZ_CALL setClipboardText(const char* text) {
  if (text == NULL) {
    return false;
  }

  // Convert UTF-8 to UTF-16 (Windows native encoding)
  int wlen = MultiByteToWideChar(CP_UTF8, 0, text, -1, NULL, 0);
  if (wlen == 0) {
    return false;
  }

  // Allocate moveable global memory for the clipboard (UTF-16 + null terminator)
  HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, (SIZE_T)wlen * sizeof(WCHAR));
  if (hGlobal == NULL) {
    return false;
  }

  LPWSTR ptr = (LPWSTR)GlobalLock(hGlobal);
  if (ptr == NULL) {
    GlobalFree(hGlobal);
    return false;
  }

  MultiByteToWideChar(CP_UTF8, 0, text, -1, ptr, wlen);
  GlobalUnlock(hGlobal);

  if (!OpenClipboard(NULL)) {
    GlobalFree(hGlobal);
    return false;
  }

  EmptyClipboard();

  // Clipboard takes ownership of hGlobal on success
  if (SetClipboardData(CF_UNICODETEXT, hGlobal) == NULL) {
    CloseClipboard();
    GlobalFree(hGlobal);
    return false;
  }

  CloseClipboard();
  return true;
}
