// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "portal.h"

#include <gio/gio.h>
#include <gio/gunixfdlist.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>

// Timeout for portal D-Bus responses. Must be generous because the user
// permission dialog (shown on SelectSources/Start) blocks until dismissed.
#define PORTAL_TIMEOUT_MS 120000  // 2 minutes

typedef struct {
  GMainLoop *loop;
  gboolean done;
  guint32 response;
  GVariant *results;
} PortalWait;

static gboolean onPortalTimeout(gpointer userData) {
  PortalWait *wait = userData;
  g_main_loop_quit(wait->loop);
  return G_SOURCE_REMOVE;
}

// D-Bus signal callback that captures the portal response and stops the event loop.
static void onPortalResponse(GDBusConnection *conn, const gchar *senderName,
                             const gchar *objectPath, const gchar *interfaceName,
                             const gchar *signalName, GVariant *parameters, gpointer userData) {
  (void)conn;
  (void)senderName;
  (void)objectPath;
  (void)interfaceName;
  (void)signalName;

  PortalWait *wait = userData;
  guint32 response = 2;
  GVariant *results = NULL;

  g_variant_get(parameters, "(u@a{sv})", &response, &results);
  wait->response = response;
  wait->results = results;
  wait->done = TRUE;

  g_main_loop_quit(wait->loop);
}

/**
 * Makes a portal D-Bus request with proper signal subscription to avoid race conditions.
 */
static gboolean portalRequest(GDBusConnection *dbus, const char *method, GVariant *params,
                              const char *handleToken, guint32 *outResponse,
                              GVariant **outResults) {
  // Build expected request path from handleToken.
  // Path format: /org/freedesktop/portal/desktop/request/{senderId}/{handleToken}.
  const char *uniqueName = g_dbus_connection_get_unique_name(dbus);
  char *senderId = g_strdup(uniqueName + 1);  // Skip leading ':'.
  // D-Bus names like "1.42" need dots replaced with underscores for portal object paths.
  for (char *ch = senderId; *ch; ch++) {
    if (*ch == '.') *ch = '_';
  }

  char *expectedPath =
      g_strdup_printf("/org/freedesktop/portal/desktop/request/%s/%s", senderId, handleToken);
  g_free(senderId);

  PortalWait wait = {0};
  wait.loop = g_main_loop_new(NULL, FALSE);

  const guint subId = g_dbus_connection_signal_subscribe(
      dbus, "org.freedesktop.portal.Desktop", "org.freedesktop.portal.Request", "Response",
      expectedPath, NULL, G_DBUS_SIGNAL_FLAGS_NONE, onPortalResponse, &wait, NULL);

  GError *error = NULL;
  GVariant *reply = g_dbus_connection_call_sync(
      dbus, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop",
      "org.freedesktop.portal.ScreenCast", method, params, G_VARIANT_TYPE("(o)"),
      G_DBUS_CALL_FLAGS_NONE, -1, NULL, &error);

  if (!reply) {
    g_clear_error(&error);
    g_dbus_connection_signal_unsubscribe(dbus, subId);
    g_main_loop_unref(wait.loop);
    g_free(expectedPath);
    return FALSE;
  }

  g_free(expectedPath);
  g_variant_unref(reply);

  // Wait for the Response signal, with a timeout to avoid hanging forever.
  const guint timeoutId = g_timeout_add(PORTAL_TIMEOUT_MS, onPortalTimeout, &wait);
  g_main_loop_run(wait.loop);
  if (wait.done) {
    g_source_remove(timeoutId);
  }

  g_dbus_connection_signal_unsubscribe(dbus, subId);
  g_main_loop_unref(wait.loop);

  if (!wait.done) {
    return FALSE;
  }

  *outResponse = wait.response;
  *outResults = wait.results;
  return TRUE;
}

// Generates a random token suitable for portal handle_token parameters.
static char *makePortalToken(void) {
  char *uuid = g_uuid_string_random();
  for (char *p = uuid; *p; p++) {
    if (*p == '-') *p = '_';
  }
  return uuid;
}

// Extracts the session handle from a CreateSession response.
static int parseSessionHandle(GVariant *results, char **outSessionHandle) {
  GVariant *v = g_variant_lookup_value(results, "session_handle", NULL);
  if (!v) return -1;

  GVariant *ov = NULL;
  if (g_variant_is_of_type(v, G_VARIANT_TYPE_VARIANT))
    ov = g_variant_get_variant(v);
  else
    ov = g_variant_ref(v);

  const char *sessionHandle = g_variant_get_string(ov, NULL);
  if (!sessionHandle) {
    g_variant_unref(ov);
    g_variant_unref(v);
    return -1;
  }
  *outSessionHandle = g_strdup(sessionHandle);
  g_variant_unref(ov);
  g_variant_unref(v);
  return 0;
}

// Parses the Start response to extract the best stream's node ID, size, and restore token.
static int parseStartResults(GVariant *results, uint32_t *outNodeId, uint32_t *outWidth,
                             uint32_t *outHeight, char **outRestoreToken) {
  GVariant *streamsV = g_variant_lookup_value(results, "streams", NULL);
  if (!streamsV) return -1;

  GVariant *streams = NULL;
  if (g_variant_is_of_type(streamsV, G_VARIANT_TYPE_VARIANT))
    streams = g_variant_get_variant(streamsV);
  else
    streams = g_variant_ref(streamsV);

  GVariantIter iter;
  g_variant_iter_init(&iter, streams);

  uint32_t bestNodeId = 0;
  uint32_t bestW = 0;
  uint32_t bestH = 0;
  uint64_t bestArea = 0;

  for (;;) {
    GVariant *child = g_variant_iter_next_value(&iter);
    if (!child) break;

    uint32_t nodeId = 0;
    GVariant *props = NULL;
    g_variant_get(child, "(u@a{sv})", &nodeId, &props);

    uint32_t w = 0;
    uint32_t h = 0;
    if (props) {
      GVariant *sizeV = g_variant_lookup_value(props, "size", NULL);
      if (sizeV) {
        GVariant *sv = NULL;
        if (g_variant_is_of_type(sizeV, G_VARIANT_TYPE_VARIANT))
          sv = g_variant_get_variant(sizeV);
        else
          sv = g_variant_ref(sizeV);

        if (g_variant_is_of_type(sv, G_VARIANT_TYPE("(uu)"))) {
          g_variant_get(sv, "(uu)", &w, &h);
        } else if (g_variant_is_of_type(sv, G_VARIANT_TYPE("(ii)"))) {
          int32_t iw = 0, ih = 0;
          g_variant_get(sv, "(ii)", &iw, &ih);
          if (iw > 0 && ih > 0) {
            w = (uint32_t)iw;
            h = (uint32_t)ih;
          }
        }
        g_variant_unref(sv);
        g_variant_unref(sizeV);
      }
    }

    const uint64_t area = (uint64_t)w * (uint64_t)h;
    if (area > bestArea) {
      bestArea = area;
      bestNodeId = nodeId;
      bestW = w;
      bestH = h;
    }

    if (props) g_variant_unref(props);
    g_variant_unref(child);
  }
  g_variant_unref(streams);
  g_variant_unref(streamsV);

  GVariant *tokenV = g_variant_lookup_value(results, "restore_token", NULL);
  if (tokenV) {
    GVariant *ts = NULL;
    if (g_variant_is_of_type(tokenV, G_VARIANT_TYPE_VARIANT))
      ts = g_variant_get_variant(tokenV);
    else
      ts = g_variant_ref(tokenV);
    const char *restoreToken = g_variant_get_string(ts, NULL);
    if (restoreToken) *outRestoreToken = g_strdup(restoreToken);
    g_variant_unref(ts);
    g_variant_unref(tokenV);
  }

  *outNodeId = bestNodeId;
  *outWidth = bestW;
  *outHeight = bestH;
  return 0;
}

// Opens a full XDG ScreenCast portal session and returns a PipeWire fd ready for capture.
bool portalCreateSession(const char *restoreToken, PortalSession *sessionOut) {
  memset(sessionOut, 0, sizeof(*sessionOut));
  sessionOut->pipewireFd = -1;

  GError *error = NULL;
  GDBusConnection *dbus = g_bus_get_sync(G_BUS_TYPE_SESSION, NULL, &error);
  if (!dbus) {
    g_clear_error(&error);
    return false;
  }

  // CreateSession
  char *handleToken = makePortalToken();
  char *sessionToken = makePortalToken();

  GVariantBuilder options;
  g_variant_builder_init(&options, G_VARIANT_TYPE_VARDICT);
  g_variant_builder_add(&options, "{sv}", "handle_token", g_variant_new_string(handleToken));
  g_variant_builder_add(&options, "{sv}", "session_handle_token",
                        g_variant_new_string(sessionToken));

  guint32 response = 2;
  GVariant *results = NULL;
  if (!portalRequest(dbus, "CreateSession", g_variant_new("(a{sv})", &options), handleToken,
                     &response, &results)) {
    g_free(handleToken);
    g_free(sessionToken);
    g_object_unref(dbus);
    return false;
  }

  if (response != 0) {
    if (results) g_variant_unref(results);
    g_free(handleToken);
    g_free(sessionToken);
    g_object_unref(dbus);
    return false;
  }

  char *sessionHandle = NULL;
  if (parseSessionHandle(results, &sessionHandle) != 0) {
    g_variant_unref(results);
    g_free(handleToken);
    g_free(sessionToken);
    g_object_unref(dbus);
    return false;
  }
  g_variant_unref(results);
  g_free(handleToken);
  g_free(sessionToken);

  // SelectSources
  handleToken = makePortalToken();
  g_variant_builder_init(&options, G_VARIANT_TYPE_VARDICT);
  g_variant_builder_add(&options, "{sv}", "handle_token", g_variant_new_string(handleToken));
  g_variant_builder_add(&options, "{sv}", "types", g_variant_new_uint32(1));  // MONITOR
  g_variant_builder_add(&options, "{sv}", "persist_mode",
                        g_variant_new_uint32(2));  // Persist until revoked.
  if (restoreToken && restoreToken[0] != '\0') {
    g_variant_builder_add(&options, "{sv}", "restore_token", g_variant_new_string(restoreToken));
  }

  if (!portalRequest(dbus, "SelectSources", g_variant_new("(oa{sv})", sessionHandle, &options),
                     handleToken, &response, &results)) {
    g_free(handleToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }

  if (response != 0) {
    if (results) g_variant_unref(results);
    g_free(handleToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }
  if (results) g_variant_unref(results);
  g_free(handleToken);

  // Start
  handleToken = makePortalToken();
  g_variant_builder_init(&options, G_VARIANT_TYPE_VARDICT);
  g_variant_builder_add(&options, "{sv}", "handle_token", g_variant_new_string(handleToken));

  if (!portalRequest(dbus, "Start", g_variant_new("(osa{sv})", sessionHandle, "", &options),
                     handleToken, &response, &results)) {
    g_free(handleToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }

  if (response != 0) {
    if (results) g_variant_unref(results);
    g_free(handleToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }

  uint32_t nodeId = 0;
  uint32_t width = 0;
  uint32_t height = 0;
  char *newRestoreToken = NULL;

  if (parseStartResults(results, &nodeId, &width, &height, &newRestoreToken) != 0) {
    g_variant_unref(results);
    g_free(handleToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }
  g_variant_unref(results);
  g_free(handleToken);

  if (nodeId == 0) {
    g_free(newRestoreToken);
    g_free(sessionHandle);
    g_object_unref(dbus);
    return false;
  }

  // OpenPipeWireRemote
  GUnixFDList *fdList = NULL;
  g_variant_builder_init(&options, G_VARIANT_TYPE_VARDICT);

  GVariant *fdReply = g_dbus_connection_call_with_unix_fd_list_sync(
      dbus, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop",
      "org.freedesktop.portal.ScreenCast", "OpenPipeWireRemote",
      g_variant_new("(oa{sv})", sessionHandle, &options), G_VARIANT_TYPE("(h)"),
      G_DBUS_CALL_FLAGS_NONE, -1, NULL, &fdList, NULL, &error);

  g_free(sessionHandle);

  if (!fdReply) {
    g_clear_error(&error);
    g_free(newRestoreToken);
    g_object_unref(dbus);
    return false;
  }

  int fdIndex = -1;
  g_variant_get(fdReply, "(h)", &fdIndex);
  const int pwFd = g_unix_fd_list_get(fdList, fdIndex, NULL);

  g_variant_unref(fdReply);
  g_object_unref(fdList);

  // NOTE: Do NOT close dbus here! The session must remain active for PipeWire to work.
  // The dbus connection is stored in sessionOut and closed in portalCloseSession.
  if (pwFd < 0) {
    g_free(newRestoreToken);
    g_object_unref(dbus);
    return false;
  }

  sessionOut->pipewireFd = pwFd;
  sessionOut->nodeId = nodeId;
  sessionOut->restoreToken = newRestoreToken;
  sessionOut->dbus = dbus;  // Keep D-Bus connection alive.

  return true;
}

// Closes the portal session, releasing the PipeWire fd and D-Bus connection.
void portalCloseSession(PortalSession *session) {
  if (!session) return;

  if (session->pipewireFd >= 0) {
    close(session->pipewireFd);
    session->pipewireFd = -1;
  }

  g_free(session->restoreToken);
  session->restoreToken = NULL;

  // Close D-Bus connection - this allows compositor to tear down screencast.
  if (session->dbus) {
    g_object_unref((GDBusConnection *)session->dbus);
    session->dbus = NULL;
  }
}
