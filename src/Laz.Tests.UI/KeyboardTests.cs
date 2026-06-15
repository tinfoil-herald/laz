// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Laz.Extensions.Keyboard;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

public class KeyboardTests : RobotTestBase
{
    #region Helper Methods

    private async Task<TextBox> CreateFocusedTextBox(string initialText = "")
    {
        TextBox textBox = null!;

        await OnUIThread(() =>
        {
            textBox = new TextBox
            {
                Text = initialText,
                Width = 300,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            TestWindow.Content = textBox;
        });

        await WaitForControlReady(textBox);

        await OnUIThread(() =>
        {
            TestWindow.Topmost = true;
            TestWindow.Activate();
        });

        await OnUIThread(() =>
        {
            textBox.Focus();
            TestWindow.Topmost = false;
        });

        var center = await OnUIThread(() => GetScreenCenter(textBox));
        Lazbot.Mouse.JumpTo(center);
        Lazbot.Mouse.Click().Delay(TimeSpan.FromMilliseconds(100));

        await WaitForFocus(textBox);

        return textBox;
    }

    /// <summary>
    /// Waits for a control to receive focus, using the GotFocus event.
    /// </summary>
    private static async Task WaitForFocus(Control control, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var tcs = new TaskCompletionSource();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (control.IsFocused)
            {
                tcs.TrySetResult();
                return;
            }

            control.GotFocus += OnGotFocus;
            return;

            void OnGotFocus(object? sender, GotFocusEventArgs e)
            {
                control.GotFocus -= OnGotFocus;
                tcs.TrySetResult();
            }
        });

        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Control did not receive focus within {timeout.Value.TotalSeconds} seconds");
        }
    }

    /// <summary>
    /// Represents a captured keyboard event.
    /// </summary>
    private sealed record KeyEvent(Avalonia.Input.Key Key, bool IsKeyDown);

    /// <summary>
    /// Creates a focused TextBox with text input event capturing enabled.
    /// Returns the TextBox and a list that will be populated with captured text input.
    /// </summary>
    private async Task<(TextBox TextBox, List<string> TextInputs)> CreateTextBoxWithTextInputCapture(
        string initialText = "")
    {
        var textBox = await CreateFocusedTextBox(initialText);
        var textInputs = new List<string>();

        await OnUIThread(() =>
        {
            // Use tunneling to capture TextInput events before TextBox handles them
            textBox.AddHandler(
                InputElement.TextInputEvent,
                (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Text))
                        textInputs.Add(e.Text);
                },
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
        });

        return (textBox, textInputs);
    }

    /// <summary>
    /// Creates a focused TextBox with key event capturing enabled.
    /// Returns the TextBox and a list that will be populated with captured key events.
    /// </summary>
    private async Task<(TextBox TextBox, List<KeyEvent> Events)> CreateTextBoxWithKeyCapture(string initialText = "")
    {
        var textBox = await CreateFocusedTextBox(initialText);
        var events = new List<KeyEvent>();

        await OnUIThread(() =>
        {
            // Use both tunneling and bubbling to ensure we capture all events
            textBox.AddHandler(
                InputElement.KeyDownEvent,
                (_, e) => { lock (events) events.Add(new KeyEvent(e.Key, IsKeyDown: true)); },
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                handledEventsToo: true);

            textBox.AddHandler(
                InputElement.KeyUpEvent,
                (_, e) => { lock (events) events.Add(new KeyEvent(e.Key, IsKeyDown: false)); },
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                handledEventsToo: true);
        });

        return (textBox, events);
    }

    #endregion

    #region Key Event Ordering Tests

    // For non-character keys (navigation, modifiers, function keys), we verify KeyDown/KeyUp events.
    // For character keys, TextBox converts them to TextInput events - see Text Input Tests region.

    [Fact]
    public async Task NavigationSendsKeyDownThenKeyUp()
    {
        var (_, events) = await CreateTextBoxWithKeyCapture();

        Lazbot.Keyboard.Stroke(Key.End);

        await DelayedAssertion.Eventually("Type(End) should send both KeyDown and KeyUp events", () =>
        {
            List<KeyEvent> snap;
            lock (events) snap = [..events];
            Assert.True(snap.Count >= 2, $"Expected at least 2 events, got {snap.Count}");
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.End, IsKeyDown: true });
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.End, IsKeyDown: false });
        });
    }

    [Fact]
    public async Task SendsKeyDownEvent()
    {
        var (_, events) = await CreateTextBoxWithKeyCapture();

        Lazbot.Keyboard.KeyDown(Key.Shift);

        await DelayedAssertion.Eventually("KeyDown(Shift) should send KeyDown event", () =>
        {
            List<KeyEvent> snap;
            lock (events) snap = [..events];
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.LeftShift, IsKeyDown: true });
        });

        Lazbot.Keyboard.KeyUp(Key.Shift);
    }

    [Fact]
    public async Task SendsKeyUpEvent()
    {
        var (_, events) = await CreateTextBoxWithKeyCapture();

        Lazbot.Keyboard.KeyDown(Key.Shift);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(50));
        lock (events) events.Clear();

        Lazbot.Keyboard.KeyUp(Key.Shift);

        await DelayedAssertion.Eventually("KeyUp(Shift) should send KeyUp event", () =>
        {
            List<KeyEvent> snap;
            lock (events) snap = [..events];
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.LeftShift, IsKeyDown: false });
        });
    }

    [Fact]
    public async Task FunctionSendsKeyDownThenKeyUp()
    {
        var (_, events) = await CreateTextBoxWithKeyCapture();

        Lazbot.Keyboard.Stroke(Key.F5);

        await DelayedAssertion.Eventually("Type(F5) should send both KeyDown and KeyUp events", () =>
        {
            List<KeyEvent> snap;
            lock (events) snap = [..events];
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.F5, IsKeyDown: true });
            Assert.Contains(snap, e => e is { Key: Avalonia.Input.Key.F5, IsKeyDown: false });
        });
    }

    [Fact]
    public async Task SendsKeyEvents()
    {
        var (_, events) = await CreateTextBoxWithKeyCapture();

        Lazbot.Keyboard
            .Stroke(Key.Left).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.Right).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.Up).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.Down);

        await DelayedAssertion.Eventually("Arrow keys should all send key events", () =>
        {
            List<KeyEvent> snap;
            lock (events) snap = [..events];
            Assert.Contains(snap, e => e.Key == Avalonia.Input.Key.Left);
            Assert.Contains(snap, e => e.Key == Avalonia.Input.Key.Right);
            Assert.Contains(snap, e => e.Key == Avalonia.Input.Key.Up);
            Assert.Contains(snap, e => e.Key == Avalonia.Input.Key.Down);
        });
    }

    #endregion

    #region Text Input Event Tests

    // Character keys generate TextInput events rather than KeyDown events in TextBox.
    // These tests verify that Laz correctly sends character input that Avalonia receives.

    [Fact]
    public async Task GeneratesTextInputEventForLetter()
    {
        var (_, textInputs) = await CreateTextBoxWithTextInputCapture();

        Lazbot.Keyboard.Stroke(Key.A);

        await DelayedAssertion.Eventually("Type(A) should generate TextInput event with 'a'", () =>
        {
            Assert.Single(textInputs);
            Assert.Equal("a", textInputs[0]);
        });
    }

    [Fact]
    public async Task GeneratesTextInputEventForNumber()
    {
        var (_, textInputs) = await CreateTextBoxWithTextInputCapture();

        Lazbot.Keyboard.Stroke(Key.Five);

        await DelayedAssertion.Eventually("Type(Five) should generate TextInput event with '5'", () =>
        {
            Assert.Single(textInputs);
            Assert.Equal("5", textInputs[0]);
        });
    }

    [Fact]
    public async Task GeneratesTextInputEventsInOrder()
    {
        var (_, textInputs) = await CreateTextBoxWithTextInputCapture();

        Lazbot.Keyboard
            .Stroke(Key.H).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.I);

        await DelayedAssertion.Eventually("Type sequence should generate TextInput events in order", () =>
        {
            Assert.Equal(2, textInputs.Count);
            Assert.Equal("h", textInputs[0]);
            Assert.Equal("i", textInputs[1]);
        });
    }

    [Fact]
    public async Task GeneratesUppercaseTextInput()
    {
        var (_, textInputs) = await CreateTextBoxWithTextInputCapture();

        Lazbot.Keyboard
            .KeyDown(Key.Shift).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(30))
            .KeyUp(Key.Shift);

        await DelayedAssertion.Eventually("Shift+A should generate TextInput event with uppercase 'A'", () =>
        {
            Assert.Single(textInputs);
            Assert.Equal("A", textInputs[0]);
        });
    }

    #endregion

    #region Single Key Tests

    [Fact]
    public async Task TypesLowercaseA()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Stroke(Key.A);

        await DelayedAssertion.Eventually("Type(A) should type lowercase 'a'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("a", text);
        });
    }

    [Fact]
    public async Task TypesCharacter()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.KeyDown(Key.B);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Keyboard.KeyUp(Key.B);

        await DelayedAssertion.Eventually("KeyDown/KeyUp should type 'b'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Contains("b", text?.ToLowerInvariant() ?? "");
        });
    }

    #endregion

    #region Letter Keys Tests

    [Theory]
    [InlineData(Key.A, "a")]
    [InlineData(Key.B, "b")]
    [InlineData(Key.C, "c")]
    [InlineData(Key.Z, "z")]
    public async Task TypesCorrectLetter(Key key, string expected)
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Stroke(key);

        await DelayedAssertion.Eventually($"Type({key}) should type '{expected}'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal(expected, text);
        });
    }

    [Fact]
    public async Task TypesAlphabet()
    {
        var textBox = await CreateFocusedTextBox();
        var keys = new[]
        {
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J,
            Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T,
            Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z
        };

        foreach (var key in keys)
        {
            Lazbot.Keyboard.Stroke(key);
            Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(60));
        }

        await DelayedAssertion.Eventually("TextBox should contain all 26 lowercase letters after typing", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("abcdefghijklmnopqrstuvwxyz", text);
        });
    }

    #endregion

    #region Number Keys Tests

    [Theory]
    [InlineData(Key.Zero, "0")]
    [InlineData(Key.One, "1")]
    [InlineData(Key.Two, "2")]
    [InlineData(Key.Nine, "9")]
    public async Task TypesCorrectDigit(Key key, string expected)
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Stroke(key);

        await DelayedAssertion.Eventually($"Type({key}) should type '{expected}'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal(expected, text);
        });
    }

    [Fact]
    public async Task TypesAllNumbers()
    {
        var textBox = await CreateFocusedTextBox();
        var keys = new[]
        {
            Key.Zero, Key.One, Key.Two, Key.Three, Key.Four,
            Key.Five, Key.Six, Key.Seven, Key.Eight, Key.Nine
        };

        foreach (var key in keys)
        {
            Lazbot.Keyboard.Stroke(key);
            Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(30));
        }

        await DelayedAssertion.Eventually("TextBox should contain all digits after typing", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("0123456789", text);
        });
    }

    #endregion

    #region Modifier Keys Tests

    [Fact]
    public async Task TypesUppercase()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard
            .KeyDown(Key.Shift).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(30))
            .KeyUp(Key.Shift);

        await DelayedAssertion.Eventually("Shift+A should type 'A'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("A", text);
        });
    }

    [Fact]
    public async Task SelectsAllText()
    {
        var textBox = await CreateFocusedTextBox("hello world");
        await OnUIThread(() => textBox.CaretIndex = 0);

        var modifier = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Key.Command : Key.Control;
        Lazbot.Keyboard
            .KeyDown(modifier).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(30))
            .KeyUp(modifier);

        await DelayedAssertion.Eventually("SelectAll should select the full text", () =>
        {
            var selectionStart = Dispatcher.UIThread.Invoke(() => textBox.SelectionStart);
            var selectionEnd = Dispatcher.UIThread.Invoke(() => textBox.SelectionEnd);
            Assert.Equal(0, selectionStart);
            Assert.Equal(11, selectionEnd); // "hello world".Length
        });
    }

    #endregion

    #region Special Keys Tests

    [Fact]
    public async Task InsertsNewline()
    {
        TextBox textBox = null!;

        await OnUIThread(() =>
        {
            textBox = new TextBox
            {
                Text = "line1",
                AcceptsReturn = true,
                Width = 300,
                Height = 100
            };
            TestWindow.Content = textBox;
        });

        await WaitForControlReady(textBox);
        await OnUIThread(() =>
        {
            textBox.Focus();
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        });
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(100));

        Lazbot.Keyboard.Stroke(Key.Enter);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Keyboard.Stroke(Key.A);

        await DelayedAssertion.Eventually("Enter should insert a newline", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Contains("\n", text ?? "");
        });
    }

    [Fact]
    public async Task MovesFocusToNextControl()
    {
        TextBox textBox1 = null!;
        TextBox textBox2 = null!;

        await OnUIThread(() =>
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
            textBox1 = new TextBox { Name = "First", Width = 200 };
            textBox2 = new TextBox { Name = "Second", Width = 200 };
            stack.Children.Add(textBox1);
            stack.Children.Add(textBox2);
            TestWindow.Content = stack;
        });

        await WaitForControlReady(textBox1);
        await OnUIThread(() => textBox1.Focus());
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(100));

        Lazbot.Keyboard.Stroke(Key.Tab);

        await DelayedAssertion.Eventually("Tab should move focus to second textbox", () =>
        {
            var secondHasFocus = Dispatcher.UIThread.Invoke(() => textBox2.IsFocused);
            Assert.True(secondHasFocus, "Tab should move focus to second textbox");
        });
    }

    [Fact]
    public async Task DeletesCharacterBeforeCaret()
    {
        var textBox = await CreateFocusedTextBox("abc");
        await OnUIThread(() => textBox.CaretIndex = 3); // End of text

        Lazbot.Keyboard.Stroke(Key.Backspace);

        await DelayedAssertion.Eventually("Backspace should delete character before caret", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("ab", text);
        });
    }

    [Fact]
    public async Task DeletesCharacterAfterCaret()
    {
        var textBox = await CreateFocusedTextBox("abc");
        await OnUIThread(() => textBox.CaretIndex = 1); // After 'a'

        Lazbot.Keyboard.Stroke(Key.Delete);

        await DelayedAssertion.Eventually("Character is deleted", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("ac", text);
        });
    }

    [Fact]
    public async Task MovesCaretToBeginning()
    {
        var textBox = await CreateFocusedTextBox("hello");
        await OnUIThread(() => textBox.CaretIndex = 3); // Middle

        Lazbot.Keyboard.Stroke(Key.Home);

        await DelayedAssertion.Eventually("Home should move caret to beginning", () =>
        {
            var caretIndex = Dispatcher.UIThread.Invoke(() => textBox.CaretIndex);
            Assert.Equal(0, caretIndex);
        });
    }

    [Fact]
    public async Task MovesCaretToEnd()
    {
        var textBox = await CreateFocusedTextBox("hello");
        await OnUIThread(() => textBox.CaretIndex = 0); // Beginning

        Lazbot.Keyboard.Stroke(Key.End);

        await DelayedAssertion.Eventually("End should move caret to end", () =>
        {
            var caretIndex = Dispatcher.UIThread.Invoke(() => textBox.CaretIndex);
            Assert.Equal(5, caretIndex); // "hello".Length
        });
    }

    #endregion

    #region Arrow Keys Tests

    [Fact]
    public async Task MovesCaretLeft()
    {
        var textBox = await CreateFocusedTextBox("hello");
        await OnUIThread(() => textBox.CaretIndex = 3);

        Lazbot.Keyboard.Stroke(Key.Left);

        await DelayedAssertion.Eventually("Left arrow should move caret left", () =>
        {
            var caretIndex = Dispatcher.UIThread.Invoke(() => textBox.CaretIndex);
            Assert.Equal(2, caretIndex);
        });
    }

    [Fact]
    public async Task MovesCaretRight()
    {
        var textBox = await CreateFocusedTextBox("hello");
        await OnUIThread(() => textBox.CaretIndex = 2);

        Lazbot.Keyboard.Stroke(Key.Right);

        await DelayedAssertion.Eventually("Right arrow should move caret right", () =>
        {
            var caretIndex = Dispatcher.UIThread.Invoke(() => textBox.CaretIndex);
            Assert.Equal(3, caretIndex);
        });
    }

    #endregion

    #region Numpad Keys Tests

    [Theory, SkipOn(Platform.Linux, "Numpad digit output depends on NumLock state, unreliable on Linux")]
    [InlineData(Key.Numpad0, "0")]
    [InlineData(Key.Numpad1, "1")]
    [InlineData(Key.Numpad2, "2")]
    [InlineData(Key.Numpad3, "3")]
    [InlineData(Key.Numpad4, "4")]
    [InlineData(Key.Numpad5, "5")]
    [InlineData(Key.Numpad6, "6")]
    [InlineData(Key.Numpad7, "7")]
    [InlineData(Key.Numpad8, "8")]
    [InlineData(Key.Numpad9, "9")]
    public async Task NumpadTypesCorrectDigit(Key key, string expected)
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Stroke(key);

        await DelayedAssertion.Eventually($"Type({key}) should type '{expected}'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal(expected, text);
        });
    }

    [Fact, SkipOn(Platform.Linux, "Numpad digit output depends on NumLock state, unreliable on Linux")]
    public async Task TypesCorrectSymbols()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard
            .Stroke(Key.Numpad1).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.NumpadPlus).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.Numpad2).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.NumpadMultiply).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.Numpad3);

        await DelayedAssertion.Eventually("Numpad operators should type expression", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("1+2*3", text);
        });
    }

    #endregion

    #region Navigation Keys Tests

    [Fact]
    public async Task PageDownSendsKeyEventToFocusedControl()
    {
        var textBox = await CreateFocusedTextBox();
        Avalonia.Input.Key? receivedKey = null;

        await OnUIThread(() =>
        {
            // Use tunneling strategy to capture the event before it's handled
            textBox.AddHandler(
                InputElement.KeyDownEvent,
                (_, e) => receivedKey = e.Key,
                RoutingStrategies.Tunnel);
        });

        Lazbot.Keyboard.Stroke(Key.PageDown);

        await DelayedAssertion.Eventually("TextBox should receive PageDown key event",
            () => Assert.Equal(Avalonia.Input.Key.PageDown, receivedKey));
    }

    [Fact]
    public async Task PageUpSendsKeyEventToFocusedControl()
    {
        var textBox = await CreateFocusedTextBox();
        Avalonia.Input.Key? receivedKey = null;

        await OnUIThread(() =>
        {
            // Use tunneling strategy to capture the event before it's handled
            textBox.AddHandler(
                InputElement.KeyDownEvent,
                (_, e) => receivedKey = e.Key,
                RoutingStrategies.Tunnel);
        });

        Lazbot.Keyboard.Stroke(Key.PageUp);

        await DelayedAssertion.Eventually("TextBox should receive PageUp key event",
            () => Assert.Equal(Avalonia.Input.Key.PageUp, receivedKey));
    }

    #endregion

    #region Right-Side Modifier Keys Tests

    [Fact]
    public async Task RightShiftTypesUppercase()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard
            .KeyDown(Key.RightShift).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(30))
            .KeyUp(Key.RightShift);

        await DelayedAssertion.Eventually("RightShift+A should type 'A'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("A", text);
        });
    }

    [Fact, SkipOn(Platform.MacOs, "macOS uses Command instead of Control")]
    public async Task SelectsAll()
    {
        var textBox = await CreateFocusedTextBox("hello world");
        await OnUIThread(() => textBox.CaretIndex = 0);

        Lazbot.Keyboard
            .KeyDown(Key.RightControl).Delay(TimeSpan.FromMilliseconds(30))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(30))
            .KeyUp(Key.RightControl);

        await DelayedAssertion.Eventually("RightControl+A should select all text", () =>
        {
            var selectionStart = Dispatcher.UIThread.Invoke(() => textBox.SelectionStart);
            var selectionEnd = Dispatcher.UIThread.Invoke(() => textBox.SelectionEnd);
            Assert.Equal(0, selectionStart);
            Assert.Equal(11, selectionEnd);
        });
    }

    #endregion

    #region Toggle Keys Tests

    [Fact,
     SkipOn(Platform.MacOs | Platform.Linux,
         "CapsLock toggle behavior is OS-dependent and unreliable on non-Windows platforms")]
    public async Task TogglesCase()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard
            .Stroke(Key.CapsLock).Delay(TimeSpan.FromMilliseconds(50))
            .Stroke(Key.A).Delay(TimeSpan.FromMilliseconds(50))
            .Stroke(Key.CapsLock).Delay(TimeSpan.FromMilliseconds(50))
            .Stroke(Key.A);

        await DelayedAssertion.Eventually("CapsLock should toggle case", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.NotNull(text);
            Assert.Equal(2, text.Length);
            Assert.True(
                (text[0] == 'A' && text[1] == 'a') || (text[0] == 'a' && text[1] == 'A'),
                $"Expected 'Aa' or 'aA', got '{text}'");
        });
    }

    #endregion

    #region Fluent Interface Tests

    [Fact]
    public void AllMethodsReturnSameInstance()
    {
        var keyboard = Lazbot.Keyboard;

        Assert.Same(keyboard, keyboard.KeyDown(Key.Shift));
        Assert.Same(keyboard, keyboard.KeyUp(Key.Shift));
        keyboard.Stroke(Key.A);
        Assert.Same(keyboard, keyboard.Stroke(Key.B));
        Assert.Same(keyboard, keyboard.Delay(TimeSpan.Zero));
    }

    [Fact]
    public async Task ChainingWorks()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(100));

        Lazbot.Keyboard.KeyDown(Key.Shift);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Keyboard.Stroke(Key.A);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Keyboard.KeyUp(Key.Shift);
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(100));
        Lazbot.Keyboard.Stroke(Key.B);

        await DelayedAssertion.Eventually("Chained keys should type 'Ab'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("Ab", text);
        });
    }

    #endregion

    #region Unmapped Keys Tests

    [Fact]
    public async Task DoesNotThrowAndHasNoEffect()
    {
        var textBox = await CreateFocusedTextBox("initial");

        const Key unmappedKey = (Key)999;

        var exception = Record.Exception(() => { Lazbot.Keyboard.Stroke(unmappedKey); });

        await DelayedAssertion.Eventually("Unmapped key should not affect text", () =>
        {
            Assert.Null(exception);
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("initial", text);
        });
    }

    #endregion

    #region Type() Method Tests

    [Fact]
    public async Task TypeMethodTypesSimpleText()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("Hello");

        await DelayedAssertion.Eventually("Type() should type 'Hello'", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("Hello", text);
        });
    }

    [Fact]
    public async Task TypeMethodHandlesMixedCase()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("HeLLo WoRLD");

        await DelayedAssertion.Eventually("Type() should handle mixed case", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("HeLLo WoRLD", text);
        });
    }

    [Fact]
    public async Task TypeMethodHandlesNumbers()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("abc123");

        await DelayedAssertion.Eventually("Type() should handle numbers", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("abc123", text);
        });
    }

    [Fact]
    public async Task TypeMethodHandlesPunctuation()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("Hello, World!");

        await DelayedAssertion.Eventually("Type() should handle punctuation", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("Hello, World!", text);
        });
    }

    [Fact]
    public async Task TypeMethodHandlesNewline()
    {
        TextBox textBox = null!;

        await OnUIThread(() =>
        {
            textBox = new TextBox
            {
                AcceptsReturn = true,
                Width = 300,
                Height = 100
            };
            TestWindow.Content = textBox;
        });

        await WaitForControlReady(textBox);
        await OnUIThread(() => textBox.Focus());
        Lazbot.Keyboard.Delay(TimeSpan.FromMilliseconds(100));

        Lazbot.Keyboard.Type("Line1\nLine2");

        await DelayedAssertion.Eventually("Type() should handle newline", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Contains("\n", text ?? "");
            Assert.Contains("Line1", text ?? "");
            Assert.Contains("Line2", text ?? "");
        });
    }

    [Fact, SkipOn(Platform.MacOs | Platform.Linux, "Alt codes only work on Windows")]
    public async Task TypeMethodHandlesWindows1252Characters()
    {
        var textBox = await CreateFocusedTextBox();

        // copyright symbol = Alt+0169, registered trademark = Alt+0174, trademark = Alt+0153
        Lazbot.Keyboard.Type("©®™");

        await DelayedAssertion.Eventually("Type() should handle Windows-1252 characters via Alt codes", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("©®™", text);
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact, SkipOn(Platform.MacOs | Platform.Linux, "Alt codes only work on Windows")]
    public async Task TypeMethodHandlesEmDash()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("Hello—World");

        await DelayedAssertion.Eventually("Type() should handle em dash", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("Hello—World", text);
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact, SkipOn(Platform.MacOs | Platform.Linux, "Alt codes only work on Windows")]
    public async Task TypeMethodHandlesCopyright()
    {
        var textBox = await CreateFocusedTextBox();

        Lazbot.Keyboard.Type("© 2024");

        await DelayedAssertion.Eventually("Type() should handle copyright symbol", () =>
        {
            var text = Dispatcher.UIThread.Invoke(() => textBox.Text);
            Assert.Equal("© 2024", text);
        }, timeout: TimeSpan.FromSeconds(10));
    }

    #endregion
}
