cmake_minimum_required(VERSION 3.16)
project(LazNative
    VERSION 0.695.0
    LANGUAGES C CXX
    DESCRIPTION "Native libraries for Laz"
)

if(APPLE)
    enable_language(OBJCXX)
endif()

# ============================================================================
# Options
# ============================================================================

option(ENABLE_FORMATTING "Enable code formatting targets" ON)
option(ENABLE_CLANG_TIDY "Enable clang-tidy analysis" OFF)
option(ENABLE_CPPCHECK "Enable cppcheck analysis" OFF)
option(WARNINGS_AS_ERRORS "Treat compiler warnings as errors" ON)

# ============================================================================
# Build Configuration
# ============================================================================

# Default to Release if not specified
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

message(STATUS "==============================================")
message(STATUS "Laz Native Build Configuration")
message(STATUS "==============================================")
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "System: ${CMAKE_SYSTEM_NAME}")
message(STATUS "Processor: ${CMAKE_SYSTEM_PROCESSOR}")
message(STATUS "==============================================")

# Output directories
set(RUNTIME_OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../runtimes")

# ========================================================================
# Shared Headers (IDE visibility)
# ========================================================================

add_library(LazHeaders INTERFACE)

target_include_directories(LazHeaders INTERFACE
    "${CMAKE_SOURCE_DIR}/include"
)

target_sources(LazHeaders INTERFACE
    "${CMAKE_SOURCE_DIR}/include/laz_api.h"
    "${CMAKE_SOURCE_DIR}/include/color.h"
    "${CMAKE_SOURCE_DIR}/include/key_code.h"
    "${CMAKE_SOURCE_DIR}/include/mouse_button.h"
    "${CMAKE_SOURCE_DIR}/include/point.h"
)

# ============================================================================
# Compiler Warnings
# ============================================================================

include(cmake/CompilerWarnings.cmake)

# ============================================================================
# Sanitizers (Debug builds)
# ============================================================================

option(ENABLE_THREAD_SANITIZER "Use ThreadSanitizer instead of ASan+UBSan in Debug builds" OFF)

if(CMAKE_BUILD_TYPE MATCHES Debug AND NOT MSVC)
    if(ENABLE_THREAD_SANITIZER)
        message(STATUS "Enabling ThreadSanitizer for Debug build")
        add_compile_options(
            -fsanitize=thread
            -fno-omit-frame-pointer
            -g
        )
        add_link_options(
            -fsanitize=thread
        )
    else()
        message(STATUS "Enabling ASan+UBSan for Debug build")
        add_compile_options(
            -fsanitize=address
            -fsanitize=undefined
            -fno-omit-frame-pointer
            -g
        )
        add_link_options(
            -fsanitize=address
            -fsanitize=undefined
        )
    endif()
endif()

# ============================================================================
# Code Formatting
# ============================================================================

if(ENABLE_FORMATTING)
    find_program(CLANG_FORMAT_EXECUTABLE
        NAMES
            clang-format
            clang-format-19
            clang-format-18
        PATHS
            /usr/bin
            /usr/local/bin
            /opt/homebrew/bin
            "C:/Program Files/LLVM/bin"
        NO_DEFAULT_PATH
    )

    if(CLANG_FORMAT_EXECUTABLE)
        file(GLOB_RECURSE ALL_SOURCE_FILES
            ${CMAKE_CURRENT_SOURCE_DIR}/src/*.c
            ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp
            ${CMAKE_CURRENT_SOURCE_DIR}/src/*.h
            ${CMAKE_CURRENT_SOURCE_DIR}/src/*.mm
        )

        add_custom_target(format
            COMMAND ${CLANG_FORMAT_EXECUTABLE}
                -i
                -style=file
                ${ALL_SOURCE_FILES}
            COMMENT "Formatting source files with clang-format"
            VERBATIM
        )

        add_custom_target(format-check
            COMMAND ${CLANG_FORMAT_EXECUTABLE}
                --dry-run
                --Werror
                -style=file
                ${ALL_SOURCE_FILES}
            COMMENT "Checking code formatting"
            VERBATIM
        )

        message(STATUS "Code formatting targets enabled")
    else()
        message(WARNING "clang-format not found, formatting targets disabled")
    endif()
endif()

# ============================================================================
# Static Analysis - clang-tidy
# ============================================================================

if(ENABLE_CLANG_TIDY)
    find_program(CLANG_TIDY_EXECUTABLE NAMES clang-tidy)

    if(CLANG_TIDY_EXECUTABLE)
        set(CMAKE_C_CLANG_TIDY ${CLANG_TIDY_EXECUTABLE})
        set(CMAKE_CXX_CLANG_TIDY ${CLANG_TIDY_EXECUTABLE})
        set(CMAKE_OBJC_CLANG_TIDY ${CLANG_TIDY_EXECUTABLE})
        set(CMAKE_OBJCXX_CLANG_TIDY ${CLANG_TIDY_EXECUTABLE})
        message(STATUS "clang-tidy enabled: ${CLANG_TIDY_EXECUTABLE}")
    else()
        message(WARNING "clang-tidy requested but not found")
    endif()
endif()

# ============================================================================
# Static Analysis - cppcheck
# ============================================================================

if(ENABLE_CPPCHECK)
    find_program(CPPCHECK_EXECUTABLE NAMES cppcheck)

    if(CPPCHECK_EXECUTABLE)
        set(CMAKE_C_CPPCHECK ${CPPCHECK_EXECUTABLE}
            --enable=warning,style,performance,portability
            --suppress=missingIncludeSystem
            --inline-suppr
        )
        set(CMAKE_CXX_CPPCHECK ${CMAKE_C_CPPCHECK})
        message(STATUS "cppcheck enabled: ${CPPCHECK_EXECUTABLE}")
    else()
        message(WARNING "cppcheck requested but not found")
    endif()
endif()

# ============================================================================
# Platform-Specific Builds
# ============================================================================

if(APPLE)
    if(CMAKE_OSX_ARCHITECTURES MATCHES "x86_64")
        message(STATUS "Building for macOS x86_64")
        set(MACOS_ARCH "x86_64")
        set(RUNTIME_ID "osx-x64")
    elseif(CMAKE_OSX_ARCHITECTURES MATCHES "arm64" OR CMAKE_SYSTEM_PROCESSOR MATCHES "arm64|aarch64")
        message(STATUS "Building for macOS ARM64")
        set(MACOS_ARCH "arm64")
        set(RUNTIME_ID "osx-arm64")
    else()
        message(STATUS "Building for macOS x86_64")
        set(MACOS_ARCH "x86_64")
        set(RUNTIME_ID "osx-x64")
    endif()
    add_subdirectory(src/mac)
elseif(UNIX AND NOT APPLE)
    if(CMAKE_SYSTEM_PROCESSOR MATCHES "aarch64|arm64")
        message(STATUS "Building for Linux ARM64")
        set(RUNTIME_ID "linux-arm64")
    else()
        message(STATUS "Building for Linux x64")
        set(RUNTIME_ID "linux-x64")
    endif()
    add_subdirectory(src/linux)
elseif(WIN32)
    if(CMAKE_GENERATOR_PLATFORM MATCHES "ARM64")
        message(STATUS "Building for Windows ARM64")
        set(RUNTIME_ID "win-arm64")
    else()
        message(STATUS "Building for Windows x64")
        set(RUNTIME_ID "win-x64")
    endif()
    add_subdirectory(src/windows)
else()
    message(FATAL_ERROR "Unsupported platform: ${CMAKE_SYSTEM_NAME}")
endif()

message(STATUS "==============================================")
message(STATUS "Runtime ID: ${RUNTIME_ID}")
message(STATUS "Output directory: ${RUNTIME_OUTPUT_DIR}/${RUNTIME_ID}/native")
message(STATUS "==============================================")
