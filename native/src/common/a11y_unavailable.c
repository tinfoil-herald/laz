// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Accessibility API for platforms that do not have a backend yet.
// Every call reports LAZ_A11Y_E_NOT_AVAILABLE, so the managed side can surface a clear error.

#include <stddef.h>

#include "laz_a11y.h"

int lazA11yIsAvailable(bool prompt) {
  (void)prompt;
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11ySetTimeout(int milliseconds) {
  (void)milliseconds;
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11yGetRoot(LazA11yElement** out) {
  if (out != NULL) {
    *out = NULL;
  }
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11yGetFocused(LazA11yElement** out) {
  return lazA11yGetRoot(out);
}

int lazA11yElementFromPoint(int x, int y, LazA11yElement** out) {
  (void)x;
  (void)y;
  return lazA11yGetRoot(out);
}

int lazA11yGetParent(LazA11yElement* element, LazA11yElement** out) {
  (void)element;
  return lazA11yGetRoot(out);
}

int lazA11yGetChildren(LazA11yElement* element, LazA11yElement*** outArray, int* outCount) {
  (void)element;
  if (outArray != NULL) {
    *outArray = NULL;
  }
  if (outCount != NULL) {
    *outCount = 0;
  }
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

void lazA11yFreeElementArray(LazA11yElement** array) {
  (void)array;
}

int lazA11yGetInfo(LazA11yElement* element, LazA11yInfo* info) {
  (void)element;
  (void)info;
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11yRefresh(LazA11yElement* element) {
  (void)element;
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11yIsSameElement(LazA11yElement* a, LazA11yElement* b, bool* out) {
  (void)a;
  (void)b;
  if (out != NULL) {
    *out = false;
  }
  return LAZ_A11Y_E_NOT_AVAILABLE;
}

int lazA11yCloneHandle(LazA11yElement* element, LazA11yElement** out) {
  (void)element;
  return lazA11yGetRoot(out);
}

void lazA11yRelease(LazA11yElement* element) {
  (void)element;
}
