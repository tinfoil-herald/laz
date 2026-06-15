// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz.Extensions.Keyboard;

/// <summary>
/// A typing context shared between the actions that type individual characters.
/// </summary>
internal sealed class TypingContext
{
    public Laz.Keyboard Keyboard { get; }
    public bool UseCtrlInsert { get; }
    public Modifiers CurrentMods { get; set; } = Modifiers.None;

    public TypingContext(Laz.Keyboard keyboard, bool useCtrlInsert)
    {
        Keyboard = keyboard;
        UseCtrlInsert = useCtrlInsert;
    }

    public void PressModifiers(Modifiers mods)
    {
        if (mods.HasFlag(Modifiers.Ctrl))
            Keyboard.KeyDown(Key.Control);
        if (mods.HasFlag(Modifiers.Alt))
            Keyboard.KeyDown(Key.RightAlt);
        if (mods.HasFlag(Modifiers.Shift))
            Keyboard.KeyDown(Key.Shift);
    }

    public void ReleaseModifiers(Modifiers mods)
    {
        if (mods.HasFlag(Modifiers.Shift))
            Keyboard.KeyUp(Key.Shift);
        if (mods.HasFlag(Modifiers.Alt))
            Keyboard.KeyUp(Key.RightAlt);
        if (mods.HasFlag(Modifiers.Ctrl))
            Keyboard.KeyUp(Key.Control);
    }

    public void ReleaseAllModifiers()
    {
        ReleaseModifiers(CurrentMods);
        CurrentMods = Modifiers.None;
    }
}