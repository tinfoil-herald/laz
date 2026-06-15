# Linux X11 native library

find_package(X11 REQUIRED COMPONENTS Xtst)

# libxkbcommon for keyboard layout introspection (dead key composition, keysym->Unicode)
find_package(PkgConfig REQUIRED)
pkg_check_modules(XKBCOMMON REQUIRED xkbcommon)

# PipeWire and GLib dependencies for Wayland screen capture backend
pkg_check_modules(PIPEWIRE REQUIRED libpipewire-0.3>=0.3.40)
pkg_check_modules(GIO REQUIRED gio-2.0)
pkg_check_modules(GLIB REQUIRED glib-2.0)

# pthread for synchronization (Wayland backend)
find_package(Threads REQUIRED)

add_library(Laz MODULE
    mouse.c
    keyboard.c
    keycode.c
    layout.c
    xdisplay.c
    screen.c
)

target_include_directories(Laz PRIVATE
    ${X11_INCLUDE_DIR}
    ${XKBCOMMON_INCLUDE_DIRS}
)

target_link_libraries(Laz PRIVATE
    LazHeaders
    ${X11_LIBRARIES}
    ${X11_Xtst_LIB}
    ${XKBCOMMON_LIBRARIES}
    ${CMAKE_DL_LIBS}
)

set_target_properties(Laz PROPERTIES
    OUTPUT_NAME "laz_native"
    PREFIX ""
    SUFFIX ".so"
    LIBRARY_OUTPUT_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    C_STANDARD 17
    CXX_STANDARD 14
    POSITION_INDEPENDENT_CODE ON
)

# Wayland/XWayland screen backend (PipeWire + portal)
add_library(LazScreenWayland MODULE
    wayland/screen_backend_wayland.c
    wayland/portal.c
    wayland/pipewire_capture.c
)

target_include_directories(LazScreenWayland PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/wayland
)
target_include_directories(LazScreenWayland SYSTEM PRIVATE
    ${PIPEWIRE_INCLUDE_DIRS}
    ${GIO_INCLUDE_DIRS}
    ${GLIB_INCLUDE_DIRS}
)

target_link_libraries(LazScreenWayland PRIVATE
    LazHeaders
    ${PIPEWIRE_LIBRARIES}
    ${GIO_LIBRARIES}
    ${GLIB_LIBRARIES}
    Threads::Threads
)

set_target_properties(LazScreenWayland PROPERTIES
    OUTPUT_NAME "laz_screen_wayland"
    PREFIX ""
    SUFFIX ".so"
    LIBRARY_OUTPUT_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    C_STANDARD 17
    CXX_STANDARD 14
    POSITION_INDEPENDENT_CODE ON
)

# X11 screen backend (X11/GDK in the future)
add_library(LazScreenX11 MODULE
    x11/screen_backend_x11.c
)

target_include_directories(LazScreenX11 PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/x11
    ${X11_INCLUDE_DIR}
)

target_link_libraries(LazScreenX11 PRIVATE
    LazHeaders
    ${X11_LIBRARIES}
)

set_target_properties(LazScreenX11 PROPERTIES
    OUTPUT_NAME "laz_screen_x11"
    PREFIX ""
    SUFFIX ".so"
    LIBRARY_OUTPUT_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    C_STANDARD 17
    CXX_STANDARD 14
    POSITION_INDEPENDENT_CODE ON
)

# Create output directory
file(MAKE_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native")

message(STATUS "Linux library will be output to: ${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native/laz_native.so")
