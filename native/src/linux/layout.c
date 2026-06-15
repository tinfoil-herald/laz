// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include <X11/XKBlib.h>
#include <X11/Xatom.h>
#include <X11/Xlib.h>
#include <locale.h>
#include <stdbool.h>
#include <stdint.h>
#include <string.h>
#include <xkbcommon/xkbcommon-compose.h>
#include <xkbcommon/xkbcommon.h>

#include "keycode.h"
#include "laz_api.h"
#include "xdisplay.h"

// Lazily initialized xkbcommon globals for dead-key composition.
static struct xkb_context *g_xkbCtx = NULL;
static struct xkb_compose_table *g_composeTable = NULL;
static struct xkb_compose_state *g_composeState = NULL;

// Cached active XKB group (set by getKeyboardLayoutId).
static int g_activeGroup = 0;

static uint32_t hashString(const char *s) {
  uint32_t h = 5381;
  while (*s) {
    h = h * 33 + (unsigned char)*s++;
  }
  return h;
}

int getKeyboardLayoutId(void) {
  Display *display = getMainDisplay();
  if (!display) {
    return 0;
  }

  // Get the active XKB group.
  XkbStateRec xkbState;
  if (XkbGetState(display, XkbUseCoreKbd, &xkbState) != Success) {
    return 0;
  }
  g_activeGroup = xkbState.group;

  // Read _XKB_RULES_NAMES from root window to get layout+variant strings.
  Atom rulesAtom = XInternAtom(display, "_XKB_RULES_NAMES", True);
  if (rulesAtom == None) {
    return 0;
  }

  Atom actualType;
  int actualFormat;
  unsigned long nItems, bytesAfter;
  unsigned char *propData = NULL;

  if (XGetWindowProperty(display, DefaultRootWindow(display), rulesAtom, 0, 1024, False, XA_STRING,
                         &actualType, &actualFormat, &nItems, &bytesAfter, &propData) != Success ||
      propData == NULL) {
    return 0;
  }

  // _XKB_RULES_NAMES is a sequence of null-terminated strings:
  //   rules \0 model \0 layout \0 variant \0 options \0
  // We want fields [2] (layout) and [3] (variant).
  const char *fields[5] = {NULL};
  const char *p = (const char *)propData;
  const char *end = (const char *)propData + nItems;

  for (int i = 0; i < 5 && p < end; i++) {
    fields[i] = p;
    p += strlen(p) + 1;
  }

  const char *layout = fields[2] ? fields[2] : "";
  const char *variant = fields[3] ? fields[3] : "";

  // Hash layout + variant + group into a single cache key.
  uint32_t h = hashString(layout);
  h = h * 33 + hashString(variant);
  h = h * 33 + (uint32_t)g_activeGroup;

  XFree(propData);

  // Initialize xkbcommon compose state on first call.
  if (g_xkbCtx == NULL) {
    g_xkbCtx = xkb_context_new(XKB_CONTEXT_NO_FLAGS);
    if (g_xkbCtx != NULL) {
      const char *locale = setlocale(LC_CTYPE, NULL);
      if (locale == NULL) {
        locale = "C";
      }
      g_composeTable =
          xkb_compose_table_new_from_locale(g_xkbCtx, locale, XKB_COMPOSE_COMPILE_NO_FLAGS);
      if (g_composeTable != NULL) {
        g_composeState = xkb_compose_state_new(g_composeTable, XKB_COMPOSE_STATE_NO_FLAGS);
      }
    }
  }

  return (int)h;
}

int probeKeyOutput(int virtualKeyCode, int level, unsigned int *deadKeyState,
                   unsigned short *outChars, int maxChars) {
  Display *display = getMainDisplay();
  if (!display || maxChars < 1) {
    return 0;
  }

  // Map our virtual key code to an X11 keysym, then to a hardware keycode.
  KeySym inputKeySym = getLinuxKeySym((VirtualKeyCode)virtualKeyCode);
  if (inputKeySym == NoSymbol) {
    return 0;
  }

  KeyCode keycode = XKeysymToKeycode(display, inputKeySym);
  if (keycode == 0) {
    return 0;
  }

  // Look up what this keycode produces at the given group+level.
  KeySym outputKeySym = XkbKeycodeToKeysym(display, keycode, g_activeGroup, level);
  if (outputKeySym == NoSymbol) {
    return 0;
  }

  // Composition mode: a previous call returned a dead key, and now we're
  // composing it with this key's output.
  if (*deadKeyState != 0) {
    if (g_composeState == NULL) {
      return 0;
    }

    xkb_compose_state_reset(g_composeState);
    xkb_compose_state_feed(g_composeState, (xkb_keysym_t)*deadKeyState);
    xkb_compose_state_feed(g_composeState, (xkb_keysym_t)outputKeySym);

    enum xkb_compose_status status = xkb_compose_state_get_status(g_composeState);
    if (status == XKB_COMPOSE_COMPOSED) {
      xkb_keysym_t composed = xkb_compose_state_get_one_sym(g_composeState);
      uint32_t cp = xkb_keysym_to_utf32(composed);
      if (cp != 0 && cp <= 0xFFFF) {
        outChars[0] = (unsigned short)cp;
        *deadKeyState = 0;
        return 1;
      }
    }

    *deadKeyState = 0;
    return 0;
  }

  // Check if the output keysym is a dead key (range 0xFE50-0xFEFF).
  if (outputKeySym >= 0xFE50 && outputKeySym <= 0xFEFF) {
    *deadKeyState = (unsigned int)outputKeySym;
    return -1;
  }

  // Normal key - convert keysym to Unicode.
  uint32_t cp = xkb_keysym_to_utf32((xkb_keysym_t)outputKeySym);
  if (cp != 0 && cp <= 0xFFFF) {
    outChars[0] = (unsigned short)cp;
    return 1;
  }

  return 0;
}
