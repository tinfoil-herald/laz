// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#ifndef PIPEWIRE_CAPTURE_H
#define PIPEWIRE_CAPTURE_H

#include <stdbool.h>
#include <stdint.h>

/**
 * Captured frame data.
 */
typedef struct {
  uint8_t *data;  // BGRA buffer (caller frees with free()).
  uint32_t width;
  uint32_t height;
  int32_t stride;
} CapturedFrame;

/**
 * Captures a single frame from a PipeWire stream.
 *
 * This function:
 * 1. Connects to PipeWire using the provided fd.
 * 2. Uses registry to resolve target_object from nodeId.
 * 3. Creates stream with PW_KEY_TARGET_OBJECT property.
 * 4. Connects with PW_ID_ANY (not nodeId directly).
 * 5. Waits for a frame and converts to BGRA.
 *
 * The returned frame data must be freed by the caller using free().
 *
 * IMPORTANT: This function takes ownership of pipewireFd. The caller
 * must not close it after calling this function. The fd will be closed
 * by PipeWire when the core is disconnected.
 *
 * @param pipewireFd File descriptor from portal's OpenPipeWireRemote (ownership transferred)
 * @param nodeId PipeWire node ID from portal
 * @param outFrame Output frame data
 * @return true on success, false on error
 */
bool pipewireCaptureFrame(int pipewireFd, uint32_t nodeId, CapturedFrame *outFrame);

#endif  // PIPEWIRE_CAPTURE_H
