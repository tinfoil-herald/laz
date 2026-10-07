// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Accessibility tree on top of UI Automation.
//
// All UIA calls run on one worker thread that lives in the multi-threaded apartment. Callers post
// a job and wait for it. This makes the API safe to call from STA threads (UI threads of WPF,
// WinForms, Avalonia) and from the .NET finalizer thread, and keeps UIA proxies in one apartment.
//
// UIAutomationClient.h defines property IDs as `const long` variables, so it must be included in
// exactly one translation unit. Keep all UIA code in this file.

#define COBJMACROS
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <oleauto.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#pragma warning(push)
// The COM headers use nameless structs and unions.
#pragma warning(disable : 4201)
#include <UIAutomation.h>
#pragma warning(pop)

#include "laz_api.h"

// Local copies of the GUIDs, so the library does not depend on the import library that defines
// them. Values are from UIAutomationClient.h.
static const CLSID kClsidCUIAutomation = {
    0xff48dba4, 0x60ef, 0x4201, {0xaa, 0x87, 0x54, 0x10, 0x3e, 0xef, 0x59, 0x4e}};
static const CLSID kClsidCUIAutomation8 = {
    0xe22ad333, 0xb25f, 0x460c, {0x83, 0xd0, 0x05, 0x81, 0x10, 0x73, 0x95, 0xc9}};
static const IID kIidIUIAutomation = {
    0x30cbe57d, 0xd9d0, 0x452a, {0xab, 0x13, 0x7a, 0xc5, 0xac, 0x48, 0x25, 0xee}};
static const IID kIidIUIAutomation2 = {
    0x34723aff, 0x0c9d, 0x49d0, {0x98, 0x96, 0x7a, 0xb5, 0x2d, 0xf8, 0xcd, 0x8a}};

#define DEFAULT_TIMEOUT_MS 5000

// ============================================================================
// Element handles
// ============================================================================

enum {
  STR_ROLE,
  STR_NAME,
  STR_VALUE,
  STR_DESCRIPTION,
  STR_AUTOMATION_ID,
  STR_CLASS_NAME,
  STR_COUNT
};

struct LazA11yElement {
  // A live UIA element with cached properties. Owned.
  IUIAutomationElement* element;
  LazA11yInfo info;
  // UTF-8 strings referenced from `info`. Owned.
  char* strings[STR_COUNT];
};

// ============================================================================
// Worker thread
// ============================================================================

typedef int (*JobFn)(void* args);
typedef DPI_AWARENESS_CONTEXT(WINAPI* GetThreadDpiContextFn)(void);
typedef DPI_AWARENESS_CONTEXT(WINAPI* SetThreadDpiContextFn)(DPI_AWARENESS_CONTEXT);

static INIT_ONCE g_initOnce = INIT_ONCE_STATIC_INIT;
static CRITICAL_SECTION g_submitLock;
static HANDLE g_jobReady = NULL;
static HANDLE g_jobDone = NULL;
static int g_initResult = LAZ_A11Y_E_NOT_AVAILABLE;

// The current job. Guarded by g_submitLock on the caller side; the worker reads it only between
// g_jobReady and g_jobDone.
static JobFn g_jobFn = NULL;
static void* g_jobArgs = NULL;
static int g_jobResult = LAZ_A11Y_OK;
static DPI_AWARENESS_CONTEXT g_jobDpiContext = NULL;

// Owned by the worker thread.
static IUIAutomation* g_uia = NULL;
static IUIAutomation2* g_uia2 = NULL;
static IUIAutomationTreeWalker* g_walker = NULL;
static IUIAutomationCacheRequest* g_cache = NULL;

static GetThreadDpiContextFn g_getThreadDpiContext = NULL;
static SetThreadDpiContextFn g_setThreadDpiContext = NULL;

static int mapHresult(HRESULT hr) {
  if (SUCCEEDED(hr)) {
    return LAZ_A11Y_OK;
  }
  switch ((unsigned long)hr) {
    case UIA_E_ELEMENTNOTAVAILABLE:
    case 0x80010108UL:  // RPC_E_DISCONNECTED
    case 0x800706BAUL:  // RPC_S_SERVER_UNAVAILABLE
    case 0x800706BEUL:  // RPC_S_CALL_FAILED
      return LAZ_A11Y_E_ELEMENT_GONE;
    case UIA_E_TIMEOUT:
      return LAZ_A11Y_E_TIMEOUT;
    case E_OUTOFMEMORY:
      return LAZ_A11Y_E_OUT_OF_MEMORY;
    case E_INVALIDARG:
    case E_POINTER:
      return LAZ_A11Y_E_INVALID_ARG;
    default:
      return LAZ_A11Y_E_INTERNAL;
  }
}

// Maps the result of an operation on an existing element. In-process providers, such as Avalonia,
// report E_FAIL instead of UIA_E_ELEMENTNOTAVAILABLE once their control is disconnected.
static int mapElementHresult(HRESULT hr) {
  return hr == E_FAIL ? LAZ_A11Y_E_ELEMENT_GONE : mapHresult(hr);
}

static void applyTimeout(int milliseconds) {
  if (g_uia2 != NULL) {
    IUIAutomation2_put_ConnectionTimeout(g_uia2, (DWORD)milliseconds);
    IUIAutomation2_put_TransactionTimeout(g_uia2, (DWORD)milliseconds);
  }
}

static int createCacheRequest(void) {
  HRESULT hr = IUIAutomation_CreateCacheRequest(g_uia, &g_cache);
  if (FAILED(hr)) {
    return mapHresult(hr);
  }

  // Properties that do not exist on older Windows versions fail to register. That is fine: the
  // corresponding fields stay empty.
  const PROPERTYID properties[] = {
      UIA_ControlTypePropertyId,
      UIA_LocalizedControlTypePropertyId,
      UIA_NamePropertyId,
      UIA_AutomationIdPropertyId,
      UIA_ClassNamePropertyId,
      UIA_HelpTextPropertyId,
      UIA_FullDescriptionPropertyId,
      UIA_BoundingRectanglePropertyId,
      UIA_IsEnabledPropertyId,
      UIA_HasKeyboardFocusPropertyId,
      UIA_IsKeyboardFocusablePropertyId,
      UIA_IsOffscreenPropertyId,
      UIA_IsPasswordPropertyId,
      UIA_ProcessIdPropertyId,
      UIA_NativeWindowHandlePropertyId,
      UIA_ValueValuePropertyId,
      UIA_ValueIsReadOnlyPropertyId,
      UIA_RangeValueValuePropertyId,
      UIA_ToggleToggleStatePropertyId,
      UIA_ExpandCollapseExpandCollapseStatePropertyId,
      UIA_SelectionItemIsSelectedPropertyId,
      UIA_WindowIsModalPropertyId,
      // Pattern properties of unsupported patterns report default values, such as
      // ToggleState_Indeterminate or IsReadOnly=TRUE, so they are read only when the pattern exists.
      UIA_IsValuePatternAvailablePropertyId,
      UIA_IsRangeValuePatternAvailablePropertyId,
      UIA_IsTogglePatternAvailablePropertyId,
      UIA_IsExpandCollapsePatternAvailablePropertyId,
      UIA_IsSelectionItemPatternAvailablePropertyId,
      UIA_IsWindowPatternAvailablePropertyId,
  };
  for (size_t i = 0; i < sizeof(properties) / sizeof(properties[0]); ++i) {
    IUIAutomationCacheRequest_AddProperty(g_cache, properties[i]);
  }
  return LAZ_A11Y_OK;
}

static int initializeUia(void) {
  HRESULT hr = CoCreateInstance(&kClsidCUIAutomation8, NULL, CLSCTX_INPROC_SERVER,
                                &kIidIUIAutomation, (void**)&g_uia);
  if (FAILED(hr)) {
    hr = CoCreateInstance(&kClsidCUIAutomation, NULL, CLSCTX_INPROC_SERVER, &kIidIUIAutomation,
                          (void**)&g_uia);
  }
  if (FAILED(hr) || g_uia == NULL) {
    return LAZ_A11Y_E_NOT_AVAILABLE;
  }

  // IUIAutomation2 (Windows 8+) provides timeouts. Without it, the UIA defaults apply.
  if (FAILED(IUIAutomation_QueryInterface(g_uia, &kIidIUIAutomation2, (void**)&g_uia2))) {
    g_uia2 = NULL;
  }
  applyTimeout(DEFAULT_TIMEOUT_MS);

  hr = IUIAutomation_get_ControlViewWalker(g_uia, &g_walker);
  if (FAILED(hr)) {
    return LAZ_A11Y_E_NOT_AVAILABLE;
  }
  return createCacheRequest();
}

static DWORD WINAPI workerMain(LPVOID param) {
  (void)param;

  HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
  g_initResult = SUCCEEDED(hr) ? initializeUia() : LAZ_A11Y_E_NOT_AVAILABLE;
  SetEvent(g_jobDone);
  if (g_initResult != LAZ_A11Y_OK) {
    return 0;
  }

  // The thread serves jobs for the lifetime of the process.
  for (;;) {
    if (WaitForSingleObject(g_jobReady, INFINITE) != WAIT_OBJECT_0) {
      return 0;
    }

    // Run the job in the DPI context of the caller, so coordinates match those that the caller
    // passes to and receives from the mouse API.
    DPI_AWARENESS_CONTEXT previous = NULL;
    if (g_setThreadDpiContext != NULL && g_jobDpiContext != NULL) {
      previous = g_setThreadDpiContext(g_jobDpiContext);
    }

    g_jobResult = g_jobFn(g_jobArgs);

    if (previous != NULL) {
      g_setThreadDpiContext(previous);
    }
    SetEvent(g_jobDone);
  }
}

static BOOL CALLBACK startWorker(PINIT_ONCE initOnce, PVOID param, PVOID* context) {
  (void)initOnce;
  (void)param;
  (void)context;

  // Available since Windows 10 1607. Without them, the worker uses the process DPI awareness.
  HMODULE user32 = GetModuleHandleW(L"user32.dll");
  if (user32 != NULL) {
    g_getThreadDpiContext =
        (GetThreadDpiContextFn)(void*)GetProcAddress(user32, "GetThreadDpiAwarenessContext");
    g_setThreadDpiContext =
        (SetThreadDpiContextFn)(void*)GetProcAddress(user32, "SetThreadDpiAwarenessContext");
  }

  InitializeCriticalSection(&g_submitLock);
  g_jobReady = CreateEventW(NULL, FALSE, FALSE, NULL);
  g_jobDone = CreateEventW(NULL, FALSE, FALSE, NULL);
  if (g_jobReady == NULL || g_jobDone == NULL) {
    g_initResult = LAZ_A11Y_E_NOT_AVAILABLE;
    return TRUE;
  }

  HANDLE thread = CreateThread(NULL, 0, workerMain, NULL, 0, NULL);
  if (thread == NULL) {
    g_initResult = LAZ_A11Y_E_NOT_AVAILABLE;
    return TRUE;
  }
  CloseHandle(thread);

  // Wait until the worker has initialized UIA and published g_initResult.
  WaitForSingleObject(g_jobDone, INFINITE);
  return TRUE;
}

static int ensureWorker(void) {
  InitOnceExecuteOnce(&g_initOnce, startWorker, NULL, NULL);
  return g_initResult;
}

static int runJob(JobFn fn, void* args) {
  int result = ensureWorker();
  if (result != LAZ_A11Y_OK) {
    return result;
  }

  EnterCriticalSection(&g_submitLock);
  g_jobFn = fn;
  g_jobArgs = args;
  g_jobDpiContext = g_getThreadDpiContext != NULL ? g_getThreadDpiContext() : NULL;
  SetEvent(g_jobReady);
  WaitForSingleObject(g_jobDone, INFINITE);
  result = g_jobResult;
  LeaveCriticalSection(&g_submitLock);
  return result;
}

// ============================================================================
// Reading properties (worker thread)
// ============================================================================

// Converts a BSTR to a newly allocated UTF-8 string. Returns NULL for NULL or empty input.
static char* bstrToUtf8(BSTR value) {
  if (value == NULL) {
    return NULL;
  }
  int length = (int)SysStringLen(value);
  if (length == 0) {
    return NULL;
  }
  int size = WideCharToMultiByte(CP_UTF8, 0, value, length, NULL, 0, NULL, NULL);
  if (size <= 0) {
    return NULL;
  }
  char* result = (char*)malloc((size_t)size + 1);
  if (result == NULL) {
    return NULL;
  }
  WideCharToMultiByte(CP_UTF8, 0, value, length, result, size, NULL, NULL);
  result[size] = '\0';
  return result;
}

// Takes ownership of the BSTR.
static char* takeBstr(HRESULT hr, BSTR value) {
  char* result = SUCCEEDED(hr) ? bstrToUtf8(value) : NULL;
  SysFreeString(value);
  return result;
}

static bool getCachedVariant(IUIAutomationElement* element, PROPERTYID property, VARIANT* out) {
  VariantInit(out);
  return SUCCEEDED(IUIAutomationElement_GetCachedPropertyValue(element, property, out));
}

static bool getCachedBool(IUIAutomationElement* element, PROPERTYID property) {
  VARIANT v;
  bool result = false;
  if (getCachedVariant(element, property, &v)) {
    result = v.vt == VT_BOOL && v.boolVal == VARIANT_TRUE;
    VariantClear(&v);
  }
  return result;
}

static bool getCachedInt(IUIAutomationElement* element, PROPERTYID property, int* out) {
  VARIANT v;
  bool result = false;
  if (getCachedVariant(element, property, &v)) {
    if (v.vt == VT_I4) {
      *out = v.lVal;
      result = true;
    }
    VariantClear(&v);
  }
  return result;
}

static char* getCachedString(IUIAutomationElement* element, PROPERTYID property) {
  VARIANT v;
  char* result = NULL;
  if (getCachedVariant(element, property, &v)) {
    if (v.vt == VT_BSTR) {
      result = bstrToUtf8(v.bstrVal);
    }
    VariantClear(&v);
  }
  return result;
}

static void clearStrings(LazA11yElement* handle) {
  for (int i = 0; i < STR_COUNT; ++i) {
    free(handle->strings[i]);
    handle->strings[i] = NULL;
  }
}

// Reads the cached properties of handle->element into handle->info.
static void fillInfo(LazA11yElement* handle) {
  IUIAutomationElement* el = handle->element;
  LazA11yInfo* info = &handle->info;

  clearStrings(handle);
  memset(info, 0, sizeof(*info));
  info->structSize = (int32_t)sizeof(*info);
  info->childCountHint = -1;
  info->rawRoleId = -1;

  CONTROLTYPEID controlType = 0;
  if (SUCCEEDED(IUIAutomationElement_get_CachedControlType(el, &controlType))) {
    info->rawRoleId = controlType;
  }

  BSTR bstr = NULL;
  HRESULT hr = IUIAutomationElement_get_CachedLocalizedControlType(el, &bstr);
  handle->strings[STR_ROLE] = takeBstr(hr, bstr);

  bstr = NULL;
  hr = IUIAutomationElement_get_CachedName(el, &bstr);
  handle->strings[STR_NAME] = takeBstr(hr, bstr);

  bstr = NULL;
  hr = IUIAutomationElement_get_CachedAutomationId(el, &bstr);
  handle->strings[STR_AUTOMATION_ID] = takeBstr(hr, bstr);

  bstr = NULL;
  hr = IUIAutomationElement_get_CachedClassName(el, &bstr);
  handle->strings[STR_CLASS_NAME] = takeBstr(hr, bstr);

  // Prefer the full description. Fall back to the help text, which older frameworks use instead.
  handle->strings[STR_DESCRIPTION] = getCachedString(el, UIA_FullDescriptionPropertyId);
  if (handle->strings[STR_DESCRIPTION] == NULL) {
    bstr = NULL;
    hr = IUIAutomationElement_get_CachedHelpText(el, &bstr);
    handle->strings[STR_DESCRIPTION] = takeBstr(hr, bstr);
  }

  const bool hasValue = getCachedBool(el, UIA_IsValuePatternAvailablePropertyId);
  const bool hasRangeValue = getCachedBool(el, UIA_IsRangeValuePatternAvailablePropertyId);

  // The Value pattern for text and combo boxes, the RangeValue pattern for sliders and progress.
  if (hasValue) {
    handle->strings[STR_VALUE] = getCachedString(el, UIA_ValueValuePropertyId);
  }
  if (handle->strings[STR_VALUE] == NULL && hasRangeValue) {
    VARIANT v;
    if (getCachedVariant(el, UIA_RangeValueValuePropertyId, &v)) {
      if (v.vt == VT_R8) {
        char buffer[64];
        int written = snprintf(buffer, sizeof(buffer), "%g", v.dblVal);
        if (written > 0) {
          handle->strings[STR_VALUE] = _strdup(buffer);
        }
      }
      VariantClear(&v);
    }
  }

  RECT rect;
  if (SUCCEEDED(IUIAutomationElement_get_CachedBoundingRectangle(el, &rect)) &&
      rect.right > rect.left && rect.bottom > rect.top) {
    info->x = rect.left;
    info->y = rect.top;
    info->width = rect.right - rect.left;
    info->height = rect.bottom - rect.top;
    info->boundsKind = LAZ_A11Y_BOUNDS_SCREEN;
  } else {
    info->boundsKind = LAZ_A11Y_BOUNDS_NONE;
  }

  int processId = 0;
  if (SUCCEEDED(IUIAutomationElement_get_CachedProcessId(el, &processId))) {
    info->processId = processId;
  }

  uint32_t states = 0;
  BOOL flag = FALSE;
  if (SUCCEEDED(IUIAutomationElement_get_CachedIsEnabled(el, &flag)) && flag) {
    states |= LAZ_A11Y_STATE_ENABLED;
  }
  flag = FALSE;
  if (SUCCEEDED(IUIAutomationElement_get_CachedHasKeyboardFocus(el, &flag)) && flag) {
    states |= LAZ_A11Y_STATE_FOCUSED;
  }
  flag = FALSE;
  if (SUCCEEDED(IUIAutomationElement_get_CachedIsKeyboardFocusable(el, &flag)) && flag) {
    states |= LAZ_A11Y_STATE_FOCUSABLE;
  }
  flag = FALSE;
  if (SUCCEEDED(IUIAutomationElement_get_CachedIsOffscreen(el, &flag)) && flag) {
    states |= LAZ_A11Y_STATE_OFFSCREEN;
  }
  flag = FALSE;
  if (SUCCEEDED(IUIAutomationElement_get_CachedIsPassword(el, &flag)) && flag) {
    states |= LAZ_A11Y_STATE_PASSWORD;
  }
  if (hasValue && getCachedBool(el, UIA_ValueIsReadOnlyPropertyId)) {
    states |= LAZ_A11Y_STATE_READONLY;
  }
  if (getCachedBool(el, UIA_IsSelectionItemPatternAvailablePropertyId) &&
      getCachedBool(el, UIA_SelectionItemIsSelectedPropertyId)) {
    states |= LAZ_A11Y_STATE_SELECTED;
  }
  if (getCachedBool(el, UIA_IsWindowPatternAvailablePropertyId) &&
      getCachedBool(el, UIA_WindowIsModalPropertyId)) {
    states |= LAZ_A11Y_STATE_MODAL;
  }

  int toggleState = 0;
  if (getCachedBool(el, UIA_IsTogglePatternAvailablePropertyId) &&
      getCachedInt(el, UIA_ToggleToggleStatePropertyId, &toggleState)) {
    if (toggleState == ToggleState_On) {
      states |= LAZ_A11Y_STATE_CHECKED;
    } else if (toggleState == ToggleState_Indeterminate) {
      states |= LAZ_A11Y_STATE_MIXED;
    }
  }

  int expandState = 0;
  if (getCachedBool(el, UIA_IsExpandCollapsePatternAvailablePropertyId) &&
      getCachedInt(el, UIA_ExpandCollapseExpandCollapseStatePropertyId, &expandState)) {
    if (expandState == ExpandCollapseState_Collapsed) {
      states |= LAZ_A11Y_STATE_COLLAPSED;
    } else if (expandState == ExpandCollapseState_Expanded ||
               expandState == ExpandCollapseState_PartiallyExpanded) {
      states |= LAZ_A11Y_STATE_EXPANDED;
    }
  }

  // UIA has no "active window" property. A top-level window is active when it is the
  // foreground window.
  int hwnd = 0;
  if (getCachedInt(el, UIA_NativeWindowHandlePropertyId, &hwnd) && hwnd != 0 &&
      (HWND)(INT_PTR)hwnd == GetForegroundWindow()) {
    states |= LAZ_A11Y_STATE_ACTIVE;
  }

  info->states = states;
  info->rawRole = handle->strings[STR_ROLE];
  info->rawSubrole = NULL;
  info->name = handle->strings[STR_NAME];
  info->value = handle->strings[STR_VALUE];
  info->description = handle->strings[STR_DESCRIPTION];
  info->automationId = handle->strings[STR_AUTOMATION_ID];
  info->className = handle->strings[STR_CLASS_NAME];
}

// Wraps an element. Takes ownership of `element` and releases it on failure.
static int wrapElement(IUIAutomationElement* element, LazA11yElement** out) {
  if (element == NULL) {
    return LAZ_A11Y_E_NOT_FOUND;
  }
  LazA11yElement* handle = (LazA11yElement*)calloc(1, sizeof(LazA11yElement));
  if (handle == NULL) {
    IUIAutomationElement_Release(element);
    return LAZ_A11Y_E_OUT_OF_MEMORY;
  }
  handle->element = element;
  fillInfo(handle);
  *out = handle;
  return LAZ_A11Y_OK;
}

static void destroyHandle(LazA11yElement* handle) {
  if (handle->element != NULL) {
    IUIAutomationElement_Release(handle->element);
  }
  clearStrings(handle);
  free(handle);
}

// ============================================================================
// Jobs (worker thread)
// ============================================================================

typedef struct {
  LazA11yElement* element;
  LazA11yElement* other;
  LazA11yElement** out;
  LazA11yElement*** outArray;
  int* outCount;
  bool* outBool;
  int x;
  int y;
} JobArgs;

static int jobNoop(void* args) {
  (void)args;
  return LAZ_A11Y_OK;
}

static int jobSetTimeout(void* args) {
  applyTimeout(((JobArgs*)args)->x);
  return LAZ_A11Y_OK;
}

static int jobGetRoot(void* args) {
  IUIAutomationElement* el = NULL;
  HRESULT hr = IUIAutomation_GetRootElementBuildCache(g_uia, g_cache, &el);
  if (FAILED(hr)) {
    return mapHresult(hr);
  }
  return wrapElement(el, ((JobArgs*)args)->out);
}

static int jobGetFocused(void* args) {
  IUIAutomationElement* el = NULL;
  HRESULT hr = IUIAutomation_GetFocusedElementBuildCache(g_uia, g_cache, &el);
  if (FAILED(hr)) {
    return mapHresult(hr);
  }
  return wrapElement(el, ((JobArgs*)args)->out);
}

static int jobElementFromPoint(void* args) {
  JobArgs* a = (JobArgs*)args;
  POINT point = {a->x, a->y};
  IUIAutomationElement* el = NULL;
  HRESULT hr = IUIAutomation_ElementFromPointBuildCache(g_uia, point, g_cache, &el);
  if (FAILED(hr)) {
    return mapHresult(hr);
  }
  return wrapElement(el, a->out);
}

static int jobGetParent(void* args) {
  JobArgs* a = (JobArgs*)args;
  IUIAutomationElement* el = NULL;
  HRESULT hr =
      IUIAutomationTreeWalker_GetParentElementBuildCache(g_walker, a->element->element, g_cache, &el);
  if (FAILED(hr)) {
    return mapElementHresult(hr);
  }
  return wrapElement(el, a->out);
}

static void releaseArray(LazA11yElement** array, int count) {
  for (int i = 0; i < count; ++i) {
    destroyHandle(array[i]);
  }
  free(array);
}

static int jobGetChildren(void* args) {
  JobArgs* a = (JobArgs*)args;
  LazA11yElement** array = NULL;
  int count = 0;
  int capacity = 0;

  IUIAutomationElement* current = NULL;
  HRESULT hr = IUIAutomationTreeWalker_GetFirstChildElementBuildCache(
      g_walker, a->element->element, g_cache, &current);

  while (SUCCEEDED(hr) && current != NULL) {
    if (count == capacity) {
      int newCapacity = capacity == 0 ? 8 : capacity * 2;
      LazA11yElement** grown =
          (LazA11yElement**)realloc(array, (size_t)newCapacity * sizeof(LazA11yElement*));
      if (grown == NULL) {
        IUIAutomationElement_Release(current);
        releaseArray(array, count);
        return LAZ_A11Y_E_OUT_OF_MEMORY;
      }
      array = grown;
      capacity = newCapacity;
    }

    IUIAutomationElement* next = NULL;
    hr = IUIAutomationTreeWalker_GetNextSiblingElementBuildCache(g_walker, current, g_cache, &next);

    int result = wrapElement(current, &array[count]);
    if (result != LAZ_A11Y_OK) {
      if (next != NULL) {
        IUIAutomationElement_Release(next);
      }
      releaseArray(array, count);
      return result;
    }
    ++count;
    current = next;
  }

  if (FAILED(hr)) {
    releaseArray(array, count);
    return mapElementHresult(hr);
  }

  *a->outArray = array;
  *a->outCount = count;
  return LAZ_A11Y_OK;
}

static int jobRefresh(void* args) {
  LazA11yElement* handle = ((JobArgs*)args)->element;
  IUIAutomationElement* updated = NULL;
  HRESULT hr = IUIAutomationElement_BuildUpdatedCache(handle->element, g_cache, &updated);
  if (FAILED(hr)) {
    return mapElementHresult(hr);
  }
  if (updated == NULL) {
    return LAZ_A11Y_E_ELEMENT_GONE;
  }
  IUIAutomationElement_Release(handle->element);
  handle->element = updated;
  fillInfo(handle);
  return LAZ_A11Y_OK;
}

static int jobIsSame(void* args) {
  JobArgs* a = (JobArgs*)args;
  BOOL same = FALSE;
  HRESULT hr = IUIAutomation_CompareElements(g_uia, a->element->element, a->other->element, &same);
  if (FAILED(hr)) {
    return mapElementHresult(hr);
  }
  *a->outBool = same != FALSE;
  return LAZ_A11Y_OK;
}

static int jobClone(void* args) {
  JobArgs* a = (JobArgs*)args;
  // The cached properties travel with the element, so the clone needs no cross-process call.
  IUIAutomationElement_AddRef(a->element->element);
  return wrapElement(a->element->element, a->out);
}

static int jobRelease(void* args) {
  destroyHandle(((JobArgs*)args)->element);
  return LAZ_A11Y_OK;
}

// ============================================================================
// Public API
// ============================================================================

LAZ_EXPORT int LAZ_CALL lazA11yIsAvailable(bool prompt) {
  (void)prompt;
  JobArgs args = {0};
  return runJob(jobNoop, &args);
}

LAZ_EXPORT int LAZ_CALL lazA11ySetTimeout(int milliseconds) {
  if (milliseconds <= 0) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  JobArgs args = {0};
  args.x = milliseconds;
  return runJob(jobSetTimeout, &args);
}

static int runOutJob(JobFn fn, JobArgs* args, LazA11yElement** out) {
  if (out == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  *out = NULL;
  args->out = out;
  return runJob(fn, args);
}

LAZ_EXPORT int LAZ_CALL lazA11yGetRoot(LazA11yElement** out) {
  JobArgs args = {0};
  return runOutJob(jobGetRoot, &args, out);
}

LAZ_EXPORT int LAZ_CALL lazA11yGetFocused(LazA11yElement** out) {
  JobArgs args = {0};
  return runOutJob(jobGetFocused, &args, out);
}

LAZ_EXPORT int LAZ_CALL lazA11yElementFromPoint(int x, int y, LazA11yElement** out) {
  JobArgs args = {0};
  args.x = x;
  args.y = y;
  return runOutJob(jobElementFromPoint, &args, out);
}

LAZ_EXPORT int LAZ_CALL lazA11yGetParent(LazA11yElement* element, LazA11yElement** out) {
  if (element == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  JobArgs args = {0};
  args.element = element;
  return runOutJob(jobGetParent, &args, out);
}

LAZ_EXPORT int LAZ_CALL lazA11yGetChildren(LazA11yElement* element, LazA11yElement*** outArray,
                                           int* outCount) {
  if (element == NULL || outArray == NULL || outCount == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  *outArray = NULL;
  *outCount = 0;
  JobArgs args = {0};
  args.element = element;
  args.outArray = outArray;
  args.outCount = outCount;
  return runJob(jobGetChildren, &args);
}

LAZ_EXPORT void LAZ_CALL lazA11yFreeElementArray(LazA11yElement** array) {
  free(array);
}

LAZ_EXPORT int LAZ_CALL lazA11yGetInfo(LazA11yElement* element, LazA11yInfo* info) {
  if (element == NULL || info == NULL || info->structSize < (int32_t)sizeof(LazA11yInfo)) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  // The info is a snapshot taken on the worker thread; reading it needs no UIA call.
  *info = element->info;
  return LAZ_A11Y_OK;
}

LAZ_EXPORT int LAZ_CALL lazA11yRefresh(LazA11yElement* element) {
  if (element == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  JobArgs args = {0};
  args.element = element;
  return runJob(jobRefresh, &args);
}

LAZ_EXPORT int LAZ_CALL lazA11yIsSameElement(LazA11yElement* a, LazA11yElement* b, bool* out) {
  if (a == NULL || b == NULL || out == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  *out = false;
  JobArgs args = {0};
  args.element = a;
  args.other = b;
  args.outBool = out;
  return runJob(jobIsSame, &args);
}

LAZ_EXPORT int LAZ_CALL lazA11yCloneHandle(LazA11yElement* element, LazA11yElement** out) {
  if (element == NULL) {
    return LAZ_A11Y_E_INVALID_ARG;
  }
  JobArgs args = {0};
  args.element = element;
  return runOutJob(jobClone, &args, out);
}

LAZ_EXPORT void LAZ_CALL lazA11yRelease(LazA11yElement* element) {
  if (element == NULL) {
    return;
  }
  // Handles exist only after the worker has started, so the job always runs.
  JobArgs args = {0};
  args.element = element;
  runJob(jobRelease, &args);
}
