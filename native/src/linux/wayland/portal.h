// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef PORTAL_H
#define PORTAL_H

#include <stdbool.h>
#include <stdint.h>

/**
 * The portal session information.
 *
 * IMPORTANT: The D-Bus connection must remain open while using PipeWire.
 * Closing it prematurely causes the compositor to tear down the screencast.
 */
typedef struct {
  int pipewireFd;      // File descriptor for PipeWire connection
  uint32_t nodeId;     // PipeWire node ID
  char *restoreToken;  // Token for future permission-less captures (caller frees)
  void *dbus;          // Internal: GDBusConnection* (kept alive for session)
} PortalSession;

/**
 * Creates a portal session and requests screen capture access.
 *
 * This function will:
 * 1. Create a ScreenCast session via D-Bus.
 * 2. Call SelectSources (may show user permission dialog).
 * 3. Call Start to get PipeWire stream info.
 * 4. Call OpenPipeWireRemote to get the fd.
 *
 * If restore_token is provided, it will be used to avoid showing the
 * permission dialog (if the token is still valid).
 *
 * @param restore_token Optional token from previous session (can be NULL)
 * @param session_out Output parameter for session info
 * @return true on success, false on error or user denial
 */
bool portalCreateSession(const char *restoreToken, PortalSession *sessionOut);

/**
 * Closes and frees a portal session.
 */
void portalCloseSession(PortalSession *session);

#endif  // PORTAL_H
