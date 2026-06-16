// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Laz.Native;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// The Windows keyboard layout. 
/// </summary>
internal class WinLayout : Layout<IntPtr>
{
    private static readonly Dictionary<IntPtr, Dictionary<char, Chord>> _directMaps = new();
    private static readonly Dictionary<IntPtr, Dictionary<char, DeadChord>> _deadKeyMaps = new();
    protected override Dictionary<IntPtr, Dictionary<char, Chord>> DirectMaps => _directMaps;
    protected override Dictionary<IntPtr, Dictionary<char, DeadChord>> DeadKeyMaps => _deadKeyMaps;

    /// <summary>
    /// Modifier combinations to probe. AltGr is Ctrl+Alt on Windows.
    /// </summary>
    private static readonly Modifiers[] ModifierCombinations =
    {
        Modifiers.None,
        Modifiers.Shift,
        Modifiers.Ctrl | Modifiers.Alt, // AltGr
        Modifiers.Shift | Modifiers.Ctrl | Modifiers.Alt // Shift+AltGr
    };

    protected override IntPtr GetKeyboardLayoutId()
    {
        var hwnd = WinLazbot.GetForegroundWindow();
        var tid = WinLazbot.GetWindowThreadProcessId(hwnd, out _);
        return WinLazbot.GetKeyboardLayout(tid);
    }

    protected override Dictionary<char, DeadChord> BuildDeadKeyMap(IntPtr layoutId,
        Dictionary<char, Chord> directMap)
    {
        var deadKeys = new List<(char DeadChar, Chord DeadChord)>();

        foreach (var key in CharacterKeys)
        {
            foreach (var mods in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (result, chars) = Probe(layoutId, key, mods);

                if (result == ProbeResult.DeadKey && chars.Length > 0)
                {
                    deadKeys.Add((chars[0], chord));
                }
            }
        }

        return ProbeDeadKeyCompositions(layoutId, deadKeys, directMap);
    }

    protected override Dictionary<char, Chord> BuildDirectMap(IntPtr layoutId)
    {
        var directMap = new Dictionary<char, Chord>();

        foreach (var key in CharacterKeys)
        {
            foreach (var mods in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (result, chars) = Probe(layoutId, key, mods);

                if (result != ProbeResult.Character || chars.Length <= 0) continue;
                var c = chars[0];
                // Prefer chords with fewer modifiers.
                if (!directMap.ContainsKey(c) || CountModifiers(mods) < CountModifiers(directMap[c].Modifiers))
                {
                    directMap[c] = chord;
                }
            }
        }

        return directMap;
    }

    // Bytes 128-159 have a fixed mapping that differs from Latin-1/Unicode.
    // Bytes 160-255 are identical to Unicode (U+00A0-U+00FF), so no table is needed.
    private static readonly Dictionary<char, int> Windows1252ExtendedMap = new()
    {
        { '\u20AC', 128 }, // €
        { '\u201A', 130 }, // ‚
        { '\u0192', 131 }, // ƒ
        { '\u201E', 132 }, // „
        { '\u2026', 133 }, // …
        { '\u2020', 134 }, // †
        { '\u2021', 135 }, // ‡
        { '\u02C6', 136 }, // ˆ
        { '\u2030', 137 }, // ‰
        { '\u0160', 138 }, // Š
        { '\u2039', 139 }, // ‹
        { '\u0152', 140 }, // Œ
        { '\u017D', 142 }, // Ž
        { '\u2018', 145 }, // '
        { '\u2019', 146 }, // '
        { '\u201C', 147 }, // "
        { '\u201D', 148 }, // "
        { '\u2022', 149 }, // •
        { '\u2013', 150 }, // –
        { '\u2014', 151 }, // —
        { '\u02DC', 152 }, // ˜
        { '\u2122', 153 }, // ™
        { '\u0161', 154 }, // š
        { '\u203A', 155 }, // ›
        { '\u0153', 156 }, // œ
        { '\u017E', 158 }, // ž
        { '\u0178', 159 }, // Ÿ
    };

    // Using space as an arbitrary test button.
    private const Key TestKey = Key.Space;

    public override bool TryComboMapping(string token, out TypingAction action)
    {
        action = null;

        if (token.Length != 1) return false;
        var character = token[0];

        int code;
        if (character < 128) return false;

        if (character is >= (char)160 and <= (char)255)
            code = character;
        else if (!Windows1252ExtendedMap.TryGetValue(character, out code))
            return false;

        // Alt+0xxx format (with leading zero).
        var digits = $"0{code}";
        var keys = new Key[digits.Length];
        for (var i = 0; i < digits.Length; i++)
        {
            keys[i] = digits[i] switch
            {
                '0' => Key.Numpad0, '1' => Key.Numpad1, '2' => Key.Numpad2,
                '3' => Key.Numpad3, '4' => Key.Numpad4, '5' => Key.Numpad5,
                '6' => Key.Numpad6, '7' => Key.Numpad7, '8' => Key.Numpad8,
                '9' => Key.Numpad9,
                _ => throw new InvalidOperationException($"Unexpected digit: {digits[i]}")
            };
        }

        action = new AltCodeSequence(keys);
        return true;
    }

    private static (ProbeResult Result, string Chars) Probe(IntPtr layoutId, Key key, Modifiers mods)
    {
        var keyState = BuildKeyState(mods);
        var scanCode = WinLazbot.MapVirtualKeyExW((uint)key, WinLazbot.MAPVK_VK_TO_VSC, layoutId);
        var buffer = new char[8];

        var result = WinLazbot.ToUnicodeEx((uint)key, scanCode, keyState, buffer, 8, 0, layoutId);

        switch (result)
        {
            case -1:
            {
                // Clear the dead key state.
                var deadChar = new string(buffer, 0, 1);
                _ = WinLazbot.ToUnicodeEx((uint)key, scanCode, keyState, buffer, 8, 0, layoutId);
                return (ProbeResult.DeadKey, deadChar);
            }
            case > 0:
                return (ProbeResult.Character, new string(buffer, 0, result));
            default:
                return (ProbeResult.None, "");
        }
    }

    private static Dictionary<char, DeadChord> ProbeDeadKeyCompositions(
        IntPtr hkl,
        List<(char DeadChar, Chord DeadChord)> deadKeys,
        Dictionary<char, Chord> directMap)
    {
        var deadMap = new Dictionary<char, DeadChord>();
        var baseKeys = new[]
        {
            Key.A, Key.E, Key.I, Key.O, Key.U, Key.Y, Key.N, Key.C, Key.S, Key.Z, Key.Space
        };

        var baseModifiers = new[] { Modifiers.None, Modifiers.Shift };

        foreach (var (_, deadChord) in deadKeys)
        {
            foreach (var baseKey in baseKeys)
            {
                foreach (var baseMods in baseModifiers)
                {
                    var baseChord = new Chord(baseKey, baseMods);

                    ApplyDeadKey(hkl, deadChord);
                    var (result, chars) = Probe(hkl, baseKey, baseMods);

                    if (result == ProbeResult.Character && chars.Length > 0)
                    {
                        var c = chars[0];
                        if (!directMap.ContainsKey(c) && !deadMap.ContainsKey(c))
                        {
                            deadMap[c] = new DeadChord(deadChord, baseChord);
                        }
                    }
                    ClearDeadKeyState(hkl);
                }
            }
        }

        return deadMap;
    }

    private static void ApplyDeadKey(IntPtr hkl, Chord deadChord)
    {
        var keyState = BuildKeyState(deadChord.Modifiers);
        var scanCode = WinLazbot.MapVirtualKeyExW((uint)deadChord.Key, WinLazbot.MAPVK_VK_TO_VSC, hkl);
        var buffer = new char[8];
        _ = WinLazbot.ToUnicodeEx((uint)deadChord.Key, scanCode, keyState, buffer, 8, 0, hkl);
    }

    private static void ClearDeadKeyState(IntPtr hkl)
    {
        var keyState = new byte[256];
        var scanCode = WinLazbot.MapVirtualKeyExW((uint)TestKey, WinLazbot.MAPVK_VK_TO_VSC, hkl);
        var buffer = new char[8];
        _ = WinLazbot.ToUnicodeEx((uint)TestKey, scanCode, keyState, buffer, 8, 0, hkl);
    }

    private static byte[] BuildKeyState(Modifiers mods)
    {
        var state = new byte[256];

        if (mods.HasFlag(Modifiers.Shift))
            state[WinLazbot.VK_SHIFT] = 0x80;

        if (mods.HasFlag(Modifiers.Ctrl))
            state[WinLazbot.VK_CONTROL] = 0x80;

        if (mods.HasFlag(Modifiers.Alt))
            state[WinLazbot.VK_MENU] = 0x80;

        return state;
    }

    private static int CountModifiers(Modifiers mods)
    {
        var count = 0;
        if (mods.HasFlag(Modifiers.Shift)) count++;
        if (mods.HasFlag(Modifiers.Ctrl)) count++;
        if (mods.HasFlag(Modifiers.Alt)) count++;
        return count;
    }
}
