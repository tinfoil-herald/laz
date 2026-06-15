# macOS native library (both Intel x64 and ARM64)

# Set deployment target to macOS 14.0 (Sonoma)
# Required for SCScreenshotManager in ScreenCaptureKit
set(CMAKE_OSX_DEPLOYMENT_TARGET "14.0" CACHE STRING "Minimum macOS version")

add_library(Laz MODULE
        mac_common.h
        common.mm
        mouse.mm
        keycode.h
        keycode.mm
        keyboard.mm
        screen.mm
        clipboard.mm
        layout.mm
)

target_include_directories(Laz PRIVATE
        "${CMAKE_SOURCE_DIR}/include"
        "${CMAKE_CURRENT_SOURCE_DIR}"
)

target_link_libraries(Laz PRIVATE
        LazHeaders
        "-framework Foundation"
        "-framework Cocoa"
        "-framework ScreenCaptureKit"
        "-framework Carbon"
)

set_target_properties(Laz PROPERTIES
        OUTPUT_NAME "laz_native"
        PREFIX ""
        SUFFIX ".dylib"
        LIBRARY_OUTPUT_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native"
        MACOSX_RPATH OFF
        C_STANDARD 17
        CXX_STANDARD 14
)

# Create output directory
file(MAKE_DIRECTORY "${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native")

message(STATUS "macOS library will be output to: ${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native/laz_native.dylib")
