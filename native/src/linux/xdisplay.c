// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "xdisplay.h"

#include <X11/Xlib.h>
#include <stdbool.h>
#include <stdlib.h> /* For atexit() */

static Display *g_mainDisplay = NULL;
static int g_registered = 0;
static int g_threadsInitialized = 0;

static void closeMainDisplay(void) {
  if (g_mainDisplay != NULL) {
    XCloseDisplay(g_mainDisplay);
    g_mainDisplay = NULL;
  }
}

Display *getMainDisplay(void) {
  if (!g_threadsInitialized) {
    XInitThreads();
    g_threadsInitialized = 1;
  }

  if (g_mainDisplay == NULL) {
    g_mainDisplay = XOpenDisplay(NULL);

    if (g_mainDisplay != NULL && !g_registered) {
      atexit(&closeMainDisplay);
      g_registered = 1;
    }
  }

  return g_mainDisplay;
}

bool isX11InputAvailable(void) { return getMainDisplay() != NULL; }
