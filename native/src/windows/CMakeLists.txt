# Windows native library

add_library(Laz MODULE
    mouse.c
    keycode.h
    keycode.c
    keyboard.c
    screen.c
    clipboard.c
)

target_include_directories(Laz PRIVATE
    "${CMAKE_SOURCE_DIR}/include"
    "${CMAKE_CURRENT_SOURCE_DIR}"
)

set_target_properties(Laz PROPERTIES
    OUTPUT_NAME "laz_native"
    PREFIX ""
    SUFFIX ".dll"
    # Output directly to runtimes folder without Debug/Release subdirectory
    LIBRARY_OUTPUT_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    LIBRARY_OUTPUT_DIRECTORY_DEBUG "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    LIBRARY_OUTPUT_DIRECTORY_RELEASE "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
    C_STANDARD 17
    CXX_STANDARD 14
)

# No explicit system library linking needed: MSVC implicitly links kernel32, user32,
# gdi32, etc. which provide SendInput, GetDC, BitBlt, and other Win32 APIs used here.
target_link_libraries(Laz PRIVATE
    LazHeaders
)

# Create output directory
file(MAKE_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native")

message(STATUS "Windows library will be output to: ${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native/laz_native.dll")
