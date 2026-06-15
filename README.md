<div align="center">

<img src=".github/marketing-resources/logo.png" width="200px" alt="Laz Logo">

# Laz

Automate mouse movements, keyboard input, and screen capture across Windows, Linux, and macOS.

[![Build](https://github.com/tinfoil-herald/laz/actions/workflows/release.yml/badge.svg)](https://github.com/tinfoil-herald/laz/actions)
[![Tests](https://github.com/tinfoil-herald/laz/actions/workflows/ci.yml/badge.svg)](https://github.com/tinfoil-herald/laz/actions)
[![codecov](https://codecov.io/gh/tinfoil-herald/laz/badge.svg)](https://codecov.io/gh/tinfoil-herald/laz)
[![NuGet](https://img.shields.io/nuget/v/Laz.svg)](https://www.nuget.org/packages/Laz/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Laz.svg)](https://www.nuget.org/packages/Laz/)
[![.NET](https://img.shields.io/badge/.NET-6%2B-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

</div>

<br>

<p align="center">
  <img src=".github/marketing-resources/demo.gif" width="600px" alt="Laz Demo">
</p>

## Install

```
dotnet add package Laz
```

## Platform support

Laz works on Windows x64/ARM, Linux x64/ARM (glibc), and macOS x64/ARM. On Linux, Laz requires an X server for mouse and
keyboard simulation. Supported RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

## Quick start

```csharp
using Laz;

var laz = new Laz();

laz.Mouse.MoveTo(100, 100);
laz.Mouse.Click();
laz.Keyboard.Type("Hello from Laz!");
```

## Features

### Mouse

All mouse methods return `Mouse`, so calls can be chained.

**Jump to position**

`JumpTo(point)` moves the cursor instantly. `GetPosition()` reads where it currently is.

```csharp
laz.Mouse.JumpTo(new (500, 300));
Point pos = laz.Mouse.GetPosition();
```

**Click**

`Click(button)` presses and releases a button. `DoubleClick(button)` does it twice. Both default to the primary (left)
button.

```csharp
laz.Mouse.Click();
laz.Mouse.Click(MouseButton.Right);
laz.Mouse.DoubleClick();
```

**Explicit button events**

`Press(button)` holds a button down; `Release(button)` lets it go. Use these when `DragAndDrop` isn't flexible enough
and you need full control over the timing.

```csharp
laz.Mouse
   .Press()
   .JumpTo(new (800, 400))
   .Release();
```

**Scroll**

`Scroll(notches)` spins the wheel. Positive values scroll up, negative scroll down.

```csharp
laz.Mouse.Scroll(3);
laz.Mouse.Scroll(-5);
```

**Smooth movement**

`MoveTo(start, target)` moves the cursor over time instead of jumping. You control the duration, path shape (`Linear` or
`Bezier`), and easing function.

```csharp
using Laz.Extensions.Mouse;

laz.Mouse.MoveTo(
    start: new (100, 100),
    target: new (900, 500),
    duration: TimeSpan.FromSeconds(1),
    moveFunction: MouseMoveFunction.Bezier,
    easingFunction: MoveEasingFunction.EaseInOut
);
```

**Drag and drop**

`DragAndDrop(start, target)` combines press, smooth move, and release. Pass `dragPreamble: true` to add a short downward
nudge before the move; some applications need this to register the gesture correctly.

```csharp
laz.Mouse.DragAndDrop(
    start: new (200, 300),
    target: new (700, 300)
);
```

**Delay**

`Delay()` pauses for the default delay between chained actions. The default is 50 ms; set the `LAZ_DELAY_MS` environment
variable to change it.

```csharp
laz.Mouse
   .Click()
   .Delay()
   .Click();
```

---

### Keyboard

All keyboard methods return `Keyboard`, so calls can be chained.

**Key down and up**

Use `KeyDown(key)` to press a key and `KeyUp(key)` to release it. Use them together to send key combinations.

```csharp
laz.Keyboard
   .KeyDown(Key.Control)
   .KeyDown(Key.Shift)
   .KeyDown(Key.Alt)
   .KeyUp(Key.Alt)
   .KeyUp(Key.Shift)
   .KeyUp(Key.Control);
```

```csharp
laz.Keyboard
   .KeyDown(Key.Control)
   .Stroke(Key.C)
   .KeyUp(Key.Control);
```

**Stroke a key**

`Stroke(key)` presses and releases a single key with the default delay in between.

```csharp
laz.Keyboard.Stroke(Key.Enter);
laz.Keyboard.Stroke(Key.Tab);
laz.Keyboard.Stroke(Key.ArrowDown);
laz.Keyboard.Stroke(Key.F5);
```

**Type text**

`Type(text)` turns a string into keystrokes. This method handles dead keys and characters that need modifier 
combinations. If a character can't be produced through key simulation, pass `clipboardFallback: true` to paste it 
using clipboard.

The `clipboardFallback` works only on Windows and macOS. On Windows, pass `useCtrlInsert: true` to use 
Ctrl/Shift+Insert instead of Ctrl+C/V.

```csharp
using Laz.Extensions.Keyboard;

laz.Keyboard.Type("Hello, World!");
laz.Keyboard.Type("café");                        // Dead keys handled automatically.
laz.Keyboard.Type("€", clipboardFallback: true);  // Paste unsupported characters using clipboard. The rest is typed normally.
```

**Delay**

`Delay()` inserts a pause between actions. The default is 50 ms; set the `LAZ_DELAY_MS` environment
variable to change it.

```csharp
laz.Keyboard
   .Stroke(Key.F2)
   .Delay()
   .Type("new name")
   .Stroke(Key.Enter);
```

---

### Screen

**Capture a region**

`Capture(origin, width, height)` takes a screenshot of the given rectangle. The result contains raw pixel data,
width, and height. Data is in BGRA byte order, 4 bytes per pixel, rows from top to bottom.

```csharp
var (data, width, height) = laz.Screen.Capture(new (0, 0), 1920, 1080);
```

**Read a pixel color**

`GetColorAt(point)` returns the color of a single pixel as `(byte R, byte G, byte B, byte A)`. Useful for checking
whether a UI element has appeared or changed state.

```csharp
var (r, g, b, _) = laz.Screen.GetColorAt(new (100, 200));
bool isRed = r > 200 && g < 50 && b < 50;
```

## Building

To learn how build the project and run the tests, check the [`BUILDING.md`](BUILDING.md) file or one of the 
GitHub Actions [workflows](.github/workflows).

## Issues and contributing

Bug reports and pull requests are welcome. If you found a bug, include your OS, .NET version, 
and a minimal reproduction steps.

## License

[Laz][repo-url] is free and open-source software licensed under the [MIT License](LICENSE).

[repo-url]: https://github.com/tinfoil-herald/laz
[issues-url]: https://github.com/tinfoil-herald/laz/issues
