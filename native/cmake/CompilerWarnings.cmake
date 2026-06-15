# CompilerWarnings.cmake
# Comprehensive compiler warnings configuration for Laz native code

if(MSVC)
    # Visual Studio warnings
    add_compile_options(/W4)  # Warning level 4
    if(WARNINGS_AS_ERRORS)
        add_compile_options(/WX)  # Warnings as errors
    endif()
    message(STATUS "Compiler warnings: MSVC /W4")
else()
    # GCC/Clang warnings
    add_compile_options(
        -Wall           # Enable all standard warnings
        -Wextra         # Extra warnings
        -Wpedantic      # Pedantic warnings (strict standard compliance)
        -Wshadow        # Warn about variable shadowing
        -Wformat=2      # Format string vulnerabilities
        -Wconversion    # Implicit type conversions
        -Wnull-dereference  # Potential null dereferences
    )

    if(WARNINGS_AS_ERRORS)
        add_compile_options(-Werror)  # Treat warnings as errors
    endif()

    message(STATUS "Compiler warnings: -Wall -Wextra -Wpedantic")
endif()
