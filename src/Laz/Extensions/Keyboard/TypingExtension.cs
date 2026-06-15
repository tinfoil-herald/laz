// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// An extension for the <see cref="Keyboard"/> that allows typing arbitrary text.
/// </summary>
public static class TypingExtension
{
    /// <summary>
    /// Types arbitrary text by simulating keystrokes of physical keys.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This method will attempt to type the characters by simulating keystrokes of keys that are actually
    /// present in the active system layout. This method supports typing symbols that require a dead key.
    /// </para>
    ///
    /// <para>
    /// On Windows and macOS, if the character can't be typed with existing layout, this method will attempt to find
    /// an appropriate Alt+Number combination on Windows and Option+Key combination on macOS. If no such combination
    /// exists, this method may optionally use the clipboard to copy and paste an arbitrary character. See
    /// <paramref name="clipboardFallback"/> and <paramref name="useCtrlInsert"/> parameters.
    /// </para>
    ///
    /// <para>
    /// This method waits a fixed delay (<see cref="Laz.Keyboard.Delay(System.TimeSpan?)"/>) between typing each individual
    /// character and simulating individual key events.
    /// </para>
    /// 
    /// <para>
    /// Note that on Windows, this method will change the global dead key to find the dead key combination. This will
    /// happen even if the text doesn't contain characters that require the dead key.
    /// </para>
    /// </remarks>
    /// 
    /// <param name="keyboard">The keyboard instance.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="clipboardFallback">
    /// If true, characters that can't be typed physically will be pasted via clipboard.
    /// Only supported on Windows and macOS; ignored on Linux.
    /// </param>
    /// <param name="useCtrlInsert">
    /// If true, uses Ctrl/Shit+Insert combination instead of Ctrl+C/V on Windows. Only meaningful when
    /// <paramref name="clipboardFallback"/> is true.
    /// </param>
    /// <returns>The keyboard instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if a method can't type a character.</exception>
    public static Laz.Keyboard Type(this Laz.Keyboard keyboard, string text,
        bool clipboardFallback = false, bool useCtrlInsert = false)
    {
        if (string.IsNullOrEmpty(text))
            return keyboard;

        var layout = ILayout.Create();
        foreach (var character in text)
        {
            var actions = FindTypingActions(layout, character, clipboardFallback);
            var context = new TypingContext(keyboard, useCtrlInsert);
            foreach (var action in actions)
            {
                action.Execute(context);
            }

            context.ReleaseAllModifiers();
            keyboard.Delay();
        }

        return keyboard;
    }

    private static IEnumerable<TypingAction> FindTypingActions(ILayout layout, char character,
        bool clipboardFallback = false)
    {
        if (layout.TryDirectMapping(character, out var directChord))
        {
            yield return directChord;
            yield break;
        }

        if (layout.TryDeadMapping(character, out var deadChord))
        {
            yield return deadChord;
            yield break;
        }

        if (layout.TryComboMapping(character, out var comboChord))
        {
            yield return comboChord;
            yield break;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && clipboardFallback)
        {
            yield return new ClipboardPaste(character);
            yield break;
        }

        throw new ArgumentException($"The `{character}` character can't be typed.");
    }
}
