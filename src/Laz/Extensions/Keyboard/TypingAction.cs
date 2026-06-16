// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// An action that types a sequence of keystrokes required to type a printable character.
/// </summary>
internal abstract class TypingAction
{
    public abstract void Execute(TypingContext context);
}

/// <summary>
/// An action that performs a single keystroke of a key that produces a printable character, with optional modifiers.
/// </summary>
internal sealed class Chord : TypingAction
{
    public Chord(Key key, Modifiers modifiers = Modifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public Modifiers Modifiers { get; }
    public Key Key { get; }

    public override void Execute(TypingContext context)
    {
        var modsUp = context.CurrentMods & ~Modifiers;
        var modsDown = Modifiers & ~context.CurrentMods;

        context.ReleaseModifiers(modsUp);
        context.PressModifiers(modsDown);

        if (modsDown != Modifiers.None)
            context.Keyboard.Delay();

        context.Keyboard
            .KeyDown(Key)
            .Delay()
            .KeyUp(Key)
            .Delay();
        context.CurrentMods = Modifiers;
    }
}

/// <summary>
/// A composite action that presses the system dead key and then presses a character key.  
/// </summary>
///
/// <remarks>
/// The sequence required to press the "dead key" is discovered automatically before the typing starts.
/// </remarks>
internal sealed class DeadChord : TypingAction
{
    private Chord Base { get; }
    private Chord DeadKey { get; }

    public DeadChord(Chord baseChord, Chord deadChord)
    {
        Base = baseChord;
        DeadKey = deadChord;
    }

    public override void Execute(TypingContext context)
    {
        TypingAction deadKey = DeadKey;
        TypingAction baseChord = Base;
        deadKey.Execute(context);
        baseChord.Execute(context);
    }
}

/// <summary>
/// A Windows-only action that types a character from the Windows-1252 table by pressing Alt + Number sequence.
/// </summary>
internal sealed class AltCodeSequence : TypingAction
{
    /// <summary>
    /// The numpad keys to press while Alt is held.
    /// </summary>
    private Key[] NumpadKeys { get; }

    public AltCodeSequence(Key[] numpadKeys)
    {
        NumpadKeys = numpadKeys;
    }

    public override void Execute(TypingContext context)
    {
        context.ReleaseAllModifiers();

        context.Keyboard.KeyDown(Key.Alt).Delay();

        foreach (var numpadKey in NumpadKeys)
        {
            context.Keyboard
                .KeyDown(numpadKey)
                .Delay()
                .KeyUp(numpadKey)
                .Delay();
        }

        context.Keyboard.KeyUp(Key.Alt).Delay();
    }
}

/// <summary>
/// An action that uses clipboard to copy and paste any character instead of typing it.
/// </summary>
///
/// <remarks>
/// This action is supported only on Windows and macOS.
/// 
/// It uses Cmd+C/V combination on macOS, and supports both Ctrl+C/V and Ctrl/Shift+Insert combinations
/// on Windows.
/// </remarks>
internal sealed class ClipboardPaste : TypingAction
{
    private string Text { get; }

    public ClipboardPaste(string text)
    {
        Text = text;
    }

    public override void Execute(TypingContext context)
    {
        context.ReleaseAllModifiers();

        Native.Clipboard.SetText(Text);
        context.Keyboard.Delay();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            context.Keyboard
                .KeyDown(Key.Command)
                .Delay()
                .Stroke(Key.V)
                .KeyUp(Key.Command);
        }
        else if (context.UseCtrlInsert)
        {
            context.Keyboard
                .KeyDown(Key.Shift)
                .Delay()
                .Stroke(Key.Insert)
                .Delay()
                .KeyUp(Key.Shift);
        }
        else
        {
            context.Keyboard
                .KeyDown(Key.Control)
                .Delay()
                .Stroke(Key.V)
                .Delay()
                .KeyUp(Key.Control);
        }

        context.Keyboard.Delay();
    }
}
