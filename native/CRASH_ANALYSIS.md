# Crash Dump Analysis

| Platform | Debug info location | Symbolication tool |
|----------|--------------------|--------------------|
| macOS | Separate `.dSYM` bundle | `atos` |
| Windows | Separate `.pdb` file | WinDbg / Visual Studio |
| Linux | Embedded in `.so` | `addr2line` / `gdb` |

## macOS

**Crash dump format and location**

When a P/Invoke call crashes, Apple's crash reporter catches the signal and writes a JSON
`.ips` report to `~/Library/Logs/DiagnosticReports/`. It contains a `usedImages` array
listing each loaded library with its load address, and a `threads` array with raw frame
addresses.

To extract what you need:
1. Find `laz_native.dylib` in `usedImages`: the `base` field is the load address.
2. Find the faulting frame in `threads`: the `imageOffset` field is the offset from the load address.

**Symbolication**

```bash
atos -o laz_native.dSYM/Contents/Resources/DWARF/laz_native.dylib \
     -l <load_address> \
     <crash_address>
# Example output: sendMouseDown (mouse.mm:42)
```

**Deciphering a report received from a user**

When a user sends you an `.ips` file, here is how to turn it into a readable stack trace.

*Step 1: Get the matching dSYM*

The `.ips` file embeds a UUID for every loaded library. You need the dSYM that was built from
the exact same binary the user was running. Retrieve it from the release archive and verify
its UUID:

```bash
dwarfdump --uuid laz_native.dSYM/Contents/Resources/DWARF/laz_native.dylib
# UUID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX (arm64)
```

Open the `.ips` file in a text editor. The file has two JSON objects: the first line is a
short metadata header; everything from line 2 onward is the main report. Search for
`laz_native.dylib` inside the `usedImages` array. Its entry looks like:

```json
{
  "arch": "arm64",
  "base": 4305240064,
  "name": "laz_native.dylib",
  "uuid": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
  ...
}
```

Compare the `uuid` value to the one printed by `dwarfdump`. They must match. If they
do not, you have a dSYM from a different build and symbolication will be wrong.

*Step 2: Find the load address*

The `base` field in the `usedImages` entry above is the load address. Note it down; you
will pass it to `atos` as the `-l` argument. In the example above it is `4305240064`
(decimal), which is `0x1009CC000` in hex. `atos` accepts both.

*Step 3: Find the crash address*

Scroll to the `threads` array and look for the thread whose `"triggered"` field is `true`.
Walk its `frames` list and find the frame whose `imageIndex` corresponds to `laz_native.dylib`
(match by index against the `usedImages` array). That frame has an `imageOffset` field:

```json
{ "imageIndex": 5, "imageOffset": 14292, ... }
```

The crash address is `base + imageOffset`. You can compute it in Python or the shell:

```bash
python3 -c "print(hex(4305240064 + 14292))"
# 0x1009cf7d4
```

*Step 4: Symbolicate*

```bash
atos -o laz_native.dSYM/Contents/Resources/DWARF/laz_native.dylib \
     -l 0x1009cc000 \
     0x1009cf7d4
# Example output: sendMouseDown (mouse.mm:42)
```

The output shows the function name, source file, and line number. If `atos` prints only
the function name without a file and line, the dSYM UUID does not match the binary; go
back to step 1.


## Windows

**Crash dump format and location**

Windows Error Reporting (WER) catches the access violation and writes a minidump (`.dmp`)
to `%LOCALAPPDATA%\CrashDumps\`. WER dump collection is off by default for non-packaged
apps. Enable it via the registry (run PowerShell as Administrator):

```powershell
$key = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
New-Item -Path $key -Force
Set-ItemProperty -Path $key -Name DumpType -Value 2   # 2 = full dump
```

**Symbolication**

Open the `.dmp` in WinDbg and point the symbol path at the directory containing the `.pdb`:

```
.sympath+ C:\path\to\symbols
.reload
!analyze -v
```

Or open the `.dmp` in Visual Studio via **File > Open > Crash Dump** with the `.pdb` present.

**Runbook for third-party dumps (Win64)**

Use this checklist when another person sends you a crash dump and you need file+line symbols.

1. Retrieve the matching `laz_native.pdb` from the release archive for the version the user was running.
2. Collect the user's `.dmp` (usually from `%LOCALAPPDATA%\CrashDumps\`).
3. Load the dump in WinDbg and point symbols to the folder with `laz_native.pdb`.

WinDbg command sequence:
```
.symfix
.sympath+ C:\path\to\folder\with\laz_native.pdb
.reload /f laz_native.dll
.lines
.ecxr
kb
!analyze -v
```

Command-line automation with `cdb.exe`:
```powershell
$cdb = "C:\Program Files\WindowsApps\Microsoft.WinDbg_...\amd64\cdb.exe"
$dump = "C:\path\to\user.dmp"
$cmds = @"
.symfix
.sympath+ C:\path\to\folder\with\laz_native.pdb
.reload /f laz_native.dll
.lines
.ecxr
kb
!analyze -v
q
"@
$cmdFile = "$env:TEMP\cdb_cmds.txt"
Set-Content -Path $cmdFile -Value $cmds
& $cdb -z $dump -cf $cmdFile
```

## Linux

**Crash dump format and location**

The .NET runtime ships a `createdump` helper that intercepts fatal signals and writes an ELF
core dump. Enable it with environment variables before launching the .NET process:

```bash
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpType=4        # 4 = full dump
export DOTNET_DbgMiniDumpName=/tmp/core.%p
```

Without these, no dump is written unless the OS `core_pattern` and `ulimit -c` are configured.

**Symbolication**

gdb is the primary tool. Debug info is embedded in the `.so`, so no extra files are needed:

```bash
gdb dotnet /tmp/core.<pid> -ex "bt" -ex "quit"
# Example output: lazCrash () at /path/to/mouse.c:80
```

`addr2line` is a lighter alternative when you already have a specific address to look up.
It takes the address in the ELF virtual address space (not the runtime address):

```bash
# Compute the VMA: runtime_addr - load_base (see runbook below for how to obtain load_base)
addr2line -e runtimes/linux-x64/native/laz_native.so -f <vma>
# Example output: sendMouseDown
#                 /path/to/mouse.c:42
```

**Runbook for third-party core dumps (Linux x64)**

Use this checklist when a user sends you a core dump and you need file+line symbols.

1. Retrieve the matching `laz_native.so` from the release archive for the version the user was running.
2. Collect the user's core dump (the file specified in `DOTNET_DbgMiniDumpName`, e.g. `/tmp/core.12345`).
3. Find where gdb loads `laz_native.so` from and place the archived `.so` there if it differs:
```bash
gdb --batch dotnet /tmp/core.12345 -ex "info sharedlibrary" 2>/dev/null | grep laz_native
```
4. Open the core dump and print the full backtrace:
```bash
gdb --batch dotnet /tmp/core.12345 -ex "bt" -ex "quit"
```

*Using `addr2line` for a specific address*

`addr2line` expects a VMA: the address within the ELF file, not the runtime address.
To convert a runtime address to a VMA:

1. Get the runtime load base of `laz_native.so` from the core dump:
```bash
gdb --batch dotnet /tmp/core.12345 -ex "info proc mappings" -ex "quit" 2>/dev/null \
  | grep laz_native.so | head -1
# Example: 0x754dc230e000  0x754dc230f000  ...  laz_native.so
# The first address is the load base.
```
2. Subtract the load base from the runtime crash address to get the VMA:
```bash
python3 -c "print(hex(0x754dc2310854 - 0x754dc230e000))"
# -> 0x2854
```
3. Look up the VMA in the `.so`:
```bash
addr2line -e runtimes/linux-x64/native/laz_native.so -f 0x2854
# sendMouseDown
# /path/to/mouse.c:42
```
