// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "xdisplay.h"

#include <X11/Xlib.h>
#include <pthread.h>
#include <stdbool.h>
#include <stdlib.h> /* For atexit() */

static Display *g_mainDisplay = NULL;
static pthread_once_t g_initOnce = PTHREAD_ONCE_INIT;

static void closeMainDisplay(void) {
  if (g_mainDisplay != NULL) {
    XCloseDisplay(g_mainDisplay);
    g_mainDisplay = NULL;
  }
}

static void initDisplay(void) {
  XInitThreads();
  g_mainDisplay = XOpenDisplay(NULL);
  if (g_mainDisplay != NULL) {
    atexit(&closeMainDisplay);
  }
}

Display *getMainDisplay(void) {
  pthread_once(&g_initOnce, initDisplay);
  return g_mainDisplay;
}

bool isX11InputAvailable(void) { return getMainDisplay() != NULL; }
