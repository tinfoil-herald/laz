// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license
// information.

/**
 * Wayland/XWayland screen backend using PipeWire + xdg-desktop-portal
 *
 * Allocator convention:
 *   - glib (g_free):  pointers from glib APIs (g_build_filename, g_get_user_config_dir)
 *   - libc (free):    pointers from libc/PipeWire (strdup, calloc) and CapturedFrame.data
 *   - portal session: freed by portalCloseSession (uses glib internally)
 */

#include <errno.h>
#include <glib.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

#include "pipewire_capture.h"
#include "portal.h"
#include "screen_backend.h"

static char *getTokenPath(void) {
  const char *configBase = g_get_user_config_dir();
  return g_build_filename(configBase, "laz", "screencast-token", NULL);
}

static char *getConfigDir(void) {
  const char *configBase = g_get_user_config_dir();
  return g_build_filename(configBase, "laz", NULL);
}

static char *loadRestoreToken(void) {
  char *path = getTokenPath();
  FILE *f = fopen(path, "r");
  g_free(path);

  if (!f) return NULL;

  char buf[512];
  if (!fgets(buf, sizeof(buf), f)) {
    fclose(f);
    return NULL;
  }
  fclose(f);

  buf[strcspn(buf, "\n")] = 0;
  if (buf[0] == '\0') return NULL;

  return strdup(buf);
}

static void saveRestoreToken(const char *token) {
  if (!token || token[0] == '\0') return;

  char *dir = getConfigDir();
  if (mkdir(dir, 0700) != 0 && errno != EEXIST) {
    g_free(dir);
    return;
  }
  g_free(dir);

  char *path = getTokenPath();
  FILE *f = fopen(path, "w");
  g_free(path);

  if (!f) return;

  fprintf(f, "%s\n", token);
  fclose(f);
}

static void cropToBuffer(const CapturedFrame *frame, int x, int y, int width, int height,
                         uint8_t *output) {
  const int outWidth = width;
  int srcX = x;
  int srcY = y;
  int dstX = 0;
  int dstY = 0;

  if (srcX < 0) {
    dstX = -srcX;
    width += srcX;
    srcX = 0;
  }
  if (srcY < 0) {
    dstY = -srcY;
    height += srcY;
    srcY = 0;
  }
  if (srcX + width > (int)frame->width) {
    width = (int)frame->width - srcX;
  }
  if (srcY + height > (int)frame->height) {
    height = (int)frame->height - srcY;
  }

  if (width <= 0 || height <= 0) return;

  for (int row = 0; row < height; row++) {
    const uint8_t *srcRow =
        frame->data + (size_t)(srcY + row) * (size_t)frame->stride + (size_t)srcX * 4;
    uint8_t *dstRow = output + (size_t)(dstY + row) * (size_t)outWidth * 4 + (size_t)dstX * 4;
    memcpy(dstRow, srcRow, (size_t)width * 4);
  }
}

LAZ_EXPORT bool lazScreenCapture(int x, int y, int width, int height, void *buffer) {
  if (width <= 0 || height <= 0 || !buffer) {
    return false;
  }

  char *restoreToken = loadRestoreToken();
  PortalSession session = {.pipewireFd = -1};
  if (!portalCreateSession(restoreToken, &session)) {
    free(restoreToken);
    return false;
  }
  free(restoreToken);
  if (session.restoreToken) {
    saveRestoreToken(session.restoreToken);
  }

  int pipewireFd = session.pipewireFd;
  session.pipewireFd = -1;  // ownership transfers to pipewireCaptureFrame

  CapturedFrame frame;
  if (!pipewireCaptureFrame(pipewireFd, session.nodeId, &frame)) {
    portalCloseSession(&session);
    return false;
  }

  cropToBuffer(&frame, x, y, width, height, (uint8_t *)buffer);

  free(frame.data);
  portalCloseSession(&session);
  return true;
}

LAZ_EXPORT NativeColor lazGetPixelColor(int x, int y) {
  NativeColor color = {0, 0, 0, 255};

  char *restoreToken = loadRestoreToken();
  PortalSession session = {.pipewireFd = -1};
  if (!portalCreateSession(restoreToken, &session)) {
    free(restoreToken);
    return color;
  }
  free(restoreToken);
  if (session.restoreToken) {
    saveRestoreToken(session.restoreToken);
  }

  int pipewireFd = session.pipewireFd;
  session.pipewireFd = -1;  // ownership transfers to pipewireCaptureFrame

  CapturedFrame frame;
  if (!pipewireCaptureFrame(pipewireFd, session.nodeId, &frame)) {
    portalCloseSession(&session);
    return color;
  }

  if (x >= 0 && x < (int)frame.width && y >= 0 && y < (int)frame.height) {
    const uint8_t *px = frame.data + (size_t)y * (size_t)frame.stride + (size_t)x * 4;
    color.r = px[2];
    color.g = px[1];
    color.b = px[0];
    color.a = px[3];
  }

  free(frame.data);
  portalCloseSession(&session);
  return color;
}
