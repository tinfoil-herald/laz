# Building Laz

## System requirements

On all platforms:

- CMake 3.16+
- .NET SDK 6.0+

### Windows

- Visual Studio 2022 with C++ workload (MSVC 19.x)
- Windows SDK 10.0.22621 or later

### macOS

- Xcode Command Line Tools (Clang)
- macOS 14.0 SDK or later

### Linux
- GCC 10+ or Clang 12+
- pkg-config
- libx11-dev, libxtst-dev
- libxkbcommon-dev
- libpipewire-0.3-dev (0.3.40+)
- libglib2.0-dev

## 1. Build native library

From repository root:

### Windows

```bash
cmake -B native/build -S native -G "Visual Studio 17 2022" -A x64
cmake --build native/build --config Release
```

Output: `runtimes/win-x64/native/laz_native.dll`

For ARM64:
```bash
cmake -B native/build -S native -G "Visual Studio 17 2022" -A ARM64
cmake --build native/build --config Release
```

Output: `runtimes/win-arm64/native/laz_native.dll`

### macOS

```bash
cmake -B native/build -S native -DCMAKE_BUILD_TYPE=Release
cmake --build native/build
```

Output: `runtimes/osx-arm64/native/laz_native.dylib` (on Apple Silicon)
Output: `runtimes/osx-x64/native/laz_native.dylib` (on Intel)

Cross-compile for other architecture:
```bash
# Build x64 on ARM64 Mac.
cmake -B native/build -S native -DCMAKE_OSX_ARCHITECTURES=x86_64 -DCMAKE_BUILD_TYPE=Release
cmake --build native/build

# Build ARM64 on Intel Mac.
cmake -B native/build -S native -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_BUILD_TYPE=Release
cmake --build native/build
```

### Linux

```bash
cmake -B native/build -S native -DCMAKE_BUILD_TYPE=Release
cmake --build native/build
```

Output:
- `runtimes/linux-x64/native/laz_native.so`
- `runtimes/linux-x64/native/laz_screen_wayland.so`
- `runtimes/linux-x64/native/laz_screen_x11.so`

## 2. Build debug symbols

For crash dump symbolication, build the native library with debug info.

### Windows

```bash
cmake -B native/build -S native -G "Visual Studio 17 2022" -A x64
cmake --build native/build --config RelWithDebInfo
```

Output:
- `runtimes/win-x64/native/RelWithDebInfo/laz_native.dll`
- `runtimes/win-x64/native/RelWithDebInfo/laz_native.pdb`

### macOS

```bash
cmake -B native/build -S native -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_FLAGS="-g" -DCMAKE_OBJCXX_FLAGS="-g"
cmake --build native/build
dsymutil runtimes/osx-arm64/native/laz_native.dylib -o laz_native.dSYM
```

Output: `laz_native.dSYM` bundle.

### Linux

```bash
cmake -B native/build -S native -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_FLAGS="-g" -DCMAKE_CXX_FLAGS="-g"
cmake --build native/build
```

DWARF info is embedded directly in the `.so` files. The size increase (~112% for `laz_native.so`) is acceptable.

## 3. Build .NET library

```bash
dotnet build src/Laz/Laz.csproj -c Release
```

Output: `src/Laz/bin/Release/net6.0/Laz.dll`

## 4. Verify build

Run tests to confirm everything works:

```bash
dotnet test src/Laz.Tests/Laz.Tests.csproj
dotnet test src/Laz.Tests.UI/Laz.Tests.UI.csproj
```

All tests should pass. Note: `Laz.Tests.UI` requires a working desktop environment and can't work headless.