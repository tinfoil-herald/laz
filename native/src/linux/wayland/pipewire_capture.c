// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#include "pipewire_capture.h"

#include <errno.h>
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wnull-dereference"
#include <pipewire/pipewire.h>
#pragma GCC diagnostic pop
#include <spa/param/format-utils.h>
#include <spa/param/video/format-utils.h>
#include <spa/utils/dict.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Timeout in seconds for each PipeWire wait step (registry sync, format
// negotiation, frame capture).  No user interaction happens here -- the portal
// permission dialog is already done.
#define PIPEWIRE_WAIT_TIMEOUT_SEC 5

/**
 * PipeWire frame capture for screen capture on Wayland.
 *
 * This implementation uses pw_thread_loop instead of pw_main_loop.
 * This follows the pattern from Java's Robot implementation which avoids
 * blocking forever if PipeWire doesn't deliver frames.
 *
 * Key aspects:
 * - pw_thread_loop runs PipeWire event processing in a separate thread
 * - We use pw_thread_loop_wait() which can be interrupted by signals
 * - Callbacks signal the waiting thread via pw_thread_loop_signal()
 * - Error conditions set a flag and signal, allowing graceful exit
 */

// Waits on the thread loop for up to timeoutSec seconds. Returns -1 on timeout.
static int timedWait(struct pw_thread_loop *loop, int timeoutSec) {
  int ret = pw_thread_loop_timed_wait(loop, timeoutSec);
  if (ret == -ETIMEDOUT) return -1;
  return 0;
}

// State for a single PipeWire capture session, passed as userdata to callbacks.
typedef struct {
  struct pw_thread_loop *loop;        // Event loop running on a dedicated thread.
  struct pw_context *context;         // PipeWire context bound to the loop.
  struct pw_core *core;               // Connection to the PipeWire daemon.
  struct pw_registry *registry;       // Used to look up the screen-cast node.
  struct pw_stream *stream;           // Video stream delivering screen frames.
  struct spa_hook core_listener;      // Listener for core done/error events.
  struct spa_hook registry_listener;  // Listener for registry global events.
  struct spa_hook stream_listener;    // Listener for stream state/format/process events.

  struct spa_video_info_raw video_info;  // Negotiated video format and resolution.
  int32_t fallback_stride;               // Stride estimate before the first frame arrives.
  int has_format;                        // Set once a supported pixel format is negotiated.

  uint8_t *bgrx_buf;    // Output buffer holding the converted BGRx frame.
  int32_t bgrx_stride;  // Row stride in bytes of bgrx_buf.

  int frame_captured;  // Set when a frame has been successfully captured.
  int has_failed;      // Set on any unrecoverable error.

  uint32_t nodeId;      // PipeWire node ID of the screen-cast source.
  char *target_object;  // Resolved serial or node name for stream targeting.
  int got_target;       // Set once target_object has been resolved.
  int got_sync;         // Set when the core sync round-trip completes.
  int sync_seq;         // Sequence number of the pending sync request.
} CaptureApp;

// Allocates the BGRx output buffer if not already allocated.
static int ensureBgrxBuffer(CaptureApp *app, uint32_t width, uint32_t height) {
  if (width == 0 || height == 0) return -1;

  app->bgrx_stride = (int32_t)width * 4;
  const size_t needed = (size_t)app->bgrx_stride * height;
  if (!app->bgrx_buf) {
    app->bgrx_buf = calloc(1, needed);
    if (!app->bgrx_buf) return -1;
  }

  return 0;
}

// Converts a frame from the negotiated pixel format to BGRx.
static void convertToBgrx(CaptureApp *app, const uint8_t *src, int32_t srcStride) {
  const uint32_t width = app->video_info.size.width;
  const uint32_t height = app->video_info.size.height;

  for (uint32_t row = 0; row < height; row++) {
    // Cast to size_t to avoid sign conversion warnings with mixed int32_t/uint32_t.
    const uint8_t *srcRow = src + (size_t)row * (size_t)srcStride;
    uint8_t *dstRow = app->bgrx_buf + (size_t)row * (size_t)app->bgrx_stride;
    for (uint32_t col = 0; col < width; col++) {
      const uint8_t *srcPixel = srcRow + (size_t)col * 4;
      uint8_t *dstPixel = dstRow + (size_t)col * 4;

      switch (app->video_info.format) {
        case SPA_VIDEO_FORMAT_BGRx:
        case SPA_VIDEO_FORMAT_BGRA:
          dstPixel[0] = srcPixel[0];
          dstPixel[1] = srcPixel[1];
          dstPixel[2] = srcPixel[2];
          dstPixel[3] = 0xFF;
          break;
        case SPA_VIDEO_FORMAT_RGBx:
        case SPA_VIDEO_FORMAT_RGBA:
          dstPixel[0] = srcPixel[2];
          dstPixel[1] = srcPixel[1];
          dstPixel[2] = srcPixel[0];
          dstPixel[3] = 0xFF;
          break;
        default:
          dstPixel[0] = 0;
          dstPixel[1] = 0;
          dstPixel[2] = 0;
          dstPixel[3] = 0xFF;
          break;
      }
    }
  }
}

// Called when the stream negotiates a video format; validates and allocates the buffer.
static void onParamChanged(void *data, uint32_t id, const struct spa_pod *param) {
  CaptureApp *app = data;

  if (id != SPA_PARAM_Format || !param) return;

  if (spa_format_video_raw_parse(param, &app->video_info) < 0) {
    app->has_format = 0;
    return;
  }

  switch (app->video_info.format) {
    case SPA_VIDEO_FORMAT_BGRx:
    case SPA_VIDEO_FORMAT_BGRA:
    case SPA_VIDEO_FORMAT_RGBx:
    case SPA_VIDEO_FORMAT_RGBA:
      app->has_format = 1;
      break;
    default:
      app->has_format = 0;
      break;
  }
  app->fallback_stride = (int32_t)app->video_info.size.width * 4;
  if (ensureBgrxBuffer(app, app->video_info.size.width, app->video_info.size.height) != 0) {
    app->has_format = 0;
  }
  pw_thread_loop_signal(app->loop, false);
}

// Signals failure on fatal core errors.
static void onCoreError(void *data, uint32_t id, int seq, int res, const char *message) {
  CaptureApp *app = data;
  (void)seq;
  (void)res;
  (void)message;

  if (id == PW_ID_CORE) {
    app->has_failed = 1;
    pw_thread_loop_signal(app->loop, false);
  }
}

// Signals when the core sync round-trip completes.
static void onCoreDone(void *data, uint32_t id, int seq) {
  CaptureApp *app = data;
  if (id == PW_ID_CORE && seq == app->sync_seq) {
    app->got_sync = 1;
    pw_thread_loop_signal(app->loop, false);
  }
}

// Resolves the target node's serial or name when it appears in the registry.
static void onRegistryGlobal(void *data, uint32_t id, uint32_t permissions, const char *type,
                             uint32_t version, const struct spa_dict *props) {
  CaptureApp *app = data;
  (void)permissions;
  (void)version;
  (void)type;

  if (id != app->nodeId || !props) return;

  const char *serial = spa_dict_lookup(props, PW_KEY_OBJECT_SERIAL);
  const char *nodeName = spa_dict_lookup(props, PW_KEY_NODE_NAME);
  if (serial)
    app->target_object = strdup(serial);
  else if (nodeName)
    app->target_object = strdup(nodeName);

  if (app->target_object) {
    app->got_target = 1;
    pw_thread_loop_signal(app->loop, false);
  }
}

// Signals failure if the stream enters an error or disconnected state.
static void onStreamStateChanged(void *data, enum pw_stream_state old, enum pw_stream_state state,
                                 const char *error) {
  CaptureApp *app = data;
  (void)old;
  (void)error;

  if (state == PW_STREAM_STATE_ERROR || state == PW_STREAM_STATE_UNCONNECTED) {
    app->has_failed = 1;
    pw_thread_loop_signal(app->loop, false);
  }
}

// Dequeues a PipeWire buffer, converts the first frame to BGRx, and signals completion.
static void onProcess(void *data) {
  CaptureApp *app = data;

  if (app->frame_captured) {
    struct pw_buffer *pwBuf = pw_stream_dequeue_buffer(app->stream);
    if (pwBuf) pw_stream_queue_buffer(app->stream, pwBuf);
    return;
  }

  struct pw_buffer *pwBuf = pw_stream_dequeue_buffer(app->stream);
  if (!pwBuf) return;

  const struct spa_buffer *spaBuf = pwBuf->buffer;
  const struct spa_data *bufData = &spaBuf->datas[0];
  if (!bufData->data) {
    pw_stream_queue_buffer(app->stream, pwBuf);
    return;
  }

  int32_t stride = app->fallback_stride;
  uint32_t offset = 0;
  if (bufData->chunk) {
    if (bufData->chunk->stride > 0) stride = bufData->chunk->stride;
    offset = bufData->chunk->offset;
  }

  const uint8_t *frame = (const uint8_t *)bufData->data + offset;

  if (!app->bgrx_buf) {
    pw_stream_queue_buffer(app->stream, pwBuf);
    return;
  }

  convertToBgrx(app, frame, stride);
  app->frame_captured = 1;

  pw_stream_queue_buffer(app->stream, pwBuf);
  pw_thread_loop_signal(app->loop, false);
}

static const struct pw_core_events g_coreEvents = {
    PW_VERSION_CORE_EVENTS,
    .done = onCoreDone,
    .error = onCoreError,
};

static const struct pw_registry_events g_registryEvents = {
    PW_VERSION_REGISTRY_EVENTS,
    .global = onRegistryGlobal,
};

static const struct pw_stream_events g_streamEvents = {
    PW_VERSION_STREAM_EVENTS,
    .state_changed = onStreamStateChanged,
    .param_changed = onParamChanged,
    .process = onProcess,
};

// Releases all resources owned by a CaptureApp. Must be called with the loop unlocked.
// Nulls out freed/destroyed fields so the function is safe to call on partially-initialized state.
static void cleanupCaptureApp(CaptureApp *app) {
  if (app->stream) {
    pw_thread_loop_lock(app->loop);
    pw_stream_disconnect(app->stream);
    pw_stream_destroy(app->stream);
    pw_thread_loop_unlock(app->loop);
    app->stream = NULL;
  }

  free(app->target_object);
  app->target_object = NULL;

  free(app->bgrx_buf);
  app->bgrx_buf = NULL;

  if (app->core) {
    pw_core_disconnect(app->core);
    app->core = NULL;
  }

  if (app->loop) {
    pw_thread_loop_stop(app->loop);
  }

  if (app->context) {
    pw_context_destroy(app->context);
    app->context = NULL;
  }

  if (app->loop) {
    pw_thread_loop_destroy(app->loop);
    app->loop = NULL;
  }
}

// Captures a single screen frame via PipeWire and returns it in outFrame.
bool pipewireCaptureFrame(int pipewireFd, uint32_t nodeId, CapturedFrame *outFrame) {
  memset(outFrame, 0, sizeof(*outFrame));

  pw_init(NULL, NULL);

  CaptureApp app;
  memset(&app, 0, sizeof(app));
  app.nodeId = nodeId;

  app.loop = pw_thread_loop_new("laz-pipewire", NULL);
  if (!app.loop) return false;

  struct pw_loop *innerLoop = pw_thread_loop_get_loop(app.loop);

  app.context = pw_context_new(innerLoop, NULL, 0);
  if (!app.context) {
    pw_thread_loop_destroy(app.loop);
    return false;
  }

  if (pw_thread_loop_start(app.loop) < 0) {
    pw_context_destroy(app.context);
    pw_thread_loop_destroy(app.loop);
    return false;
  }

  pw_thread_loop_lock(app.loop);

  app.core = pw_context_connect_fd(app.context, pipewireFd, NULL, 0);
  if (!app.core) {
    pw_thread_loop_unlock(app.loop);
    pw_thread_loop_stop(app.loop);
    pw_context_destroy(app.context);
    pw_thread_loop_destroy(app.loop);
    return false;
  }

  pw_core_add_listener(app.core, &app.core_listener, &g_coreEvents, &app);
  app.registry = pw_core_get_registry(app.core, PW_VERSION_REGISTRY, 0);
  pw_registry_add_listener(app.registry, &app.registry_listener, &g_registryEvents, &app);
  app.sync_seq = pw_core_sync(app.core, PW_ID_CORE, 0);

  while (!app.got_target && !app.got_sync && !app.has_failed) {
    if (timedWait(app.loop, PIPEWIRE_WAIT_TIMEOUT_SEC) < 0) {
      app.has_failed = 1;
      break;
    }
  }

  if (!app.target_object || app.has_failed) {
    pw_thread_loop_unlock(app.loop);
    cleanupCaptureApp(&app);
    return false;
  }

  app.stream = pw_stream_new(app.core, "laz-screen-capture",
                             pw_properties_new(PW_KEY_MEDIA_TYPE, "Video", PW_KEY_MEDIA_CATEGORY,
                                               "Capture", PW_KEY_MEDIA_ROLE, "Screen",
                                               PW_KEY_TARGET_OBJECT, app.target_object, NULL));

  if (!app.stream) {
    pw_thread_loop_unlock(app.loop);
    cleanupCaptureApp(&app);
    return false;
  }

  pw_stream_add_listener(app.stream, &app.stream_listener, &g_streamEvents, &app);

  uint8_t paramsBuffer[4096];
  struct spa_pod_builder podBuilder = SPA_POD_BUILDER_INIT(paramsBuffer, sizeof(paramsBuffer));
  const struct spa_pod *params[1];

  params[0] = spa_pod_builder_add_object(
      &podBuilder, SPA_TYPE_OBJECT_Format, SPA_PARAM_EnumFormat, SPA_FORMAT_mediaType,
      SPA_POD_Id(SPA_MEDIA_TYPE_video), SPA_FORMAT_mediaSubtype, SPA_POD_Id(SPA_MEDIA_SUBTYPE_raw),
      SPA_FORMAT_VIDEO_size,
      SPA_POD_CHOICE_RANGE_Rectangle(&SPA_RECTANGLE(1280, 720), &SPA_RECTANGLE(1, 1),
                                     &SPA_RECTANGLE(8192, 8192)),
      SPA_FORMAT_VIDEO_framerate,
      SPA_POD_CHOICE_RANGE_Fraction(&SPA_FRACTION(30, 1), &SPA_FRACTION(0, 1),
                                    &SPA_FRACTION(120, 1)));

  // PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS - flags are designed to be OR'd.
  // NOLINTNEXTLINE(clang-analyzer-optin.core.EnumCastOutOfRange)
  const enum pw_stream_flags streamFlags = PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS;
  int res = pw_stream_connect(app.stream, PW_DIRECTION_INPUT, PW_ID_ANY, streamFlags, params, 1);
  if (res < 0) {
    pw_thread_loop_unlock(app.loop);
    cleanupCaptureApp(&app);
    return false;
  }

  while (!app.has_format && !app.has_failed) {
    if (timedWait(app.loop, PIPEWIRE_WAIT_TIMEOUT_SEC) < 0) {
      app.has_failed = 1;
      break;
    }
  }

  if (app.has_failed || !app.has_format) {
    pw_thread_loop_unlock(app.loop);
    cleanupCaptureApp(&app);
    return false;
  }

  while (!app.frame_captured && !app.has_failed) {
    if (timedWait(app.loop, PIPEWIRE_WAIT_TIMEOUT_SEC) < 0) {
      app.has_failed = 1;
      break;
    }
  }

  pw_thread_loop_unlock(app.loop);

  bool success = false;
  if (app.frame_captured && !app.has_failed) {
    outFrame->data = app.bgrx_buf;
    outFrame->width = app.video_info.size.width;
    outFrame->height = app.video_info.size.height;
    outFrame->stride = app.bgrx_stride;
    app.bgrx_buf = NULL;
    success = true;
  }

  cleanupCaptureApp(&app);

  return success;
}

// Frees the frame data allocated by pipewireCaptureFrame.
void pipewireFreeFrame(CapturedFrame *frame) {
  if (frame) {
    free(frame->data);
    memset(frame, 0, sizeof(*frame));
  }
}
