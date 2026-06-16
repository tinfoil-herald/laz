// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Laz.Native;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// The macOS keyboard layout. 
/// </summary>
internal class MacLayout : Layout<int>
{
    private static readonly ConcurrentDictionary<int, Dictionary<char, Chord>> _directMaps = new();
    private static readonly ConcurrentDictionary<int, Dictionary<char, DeadChord>> _deadKeyMaps = new();
    protected override ConcurrentDictionary<int, Dictionary<char, Chord>> DirectMaps => _directMaps;
    protected override ConcurrentDictionary<int, Dictionary<char, DeadChord>> DeadKeyMaps => _deadKeyMaps;

    private static readonly Dictionary<Key, int> KeyCodeMapping = new()
    {
        [Key.A] = 0x00, [Key.S] = 0x01, [Key.D] = 0x02, [Key.F] = 0x03,
        [Key.H] = 0x04, [Key.G] = 0x05, [Key.Z] = 0x06, [Key.X] = 0x07,
        [Key.C] = 0x08, [Key.V] = 0x09, [Key.B] = 0x0B, [Key.Q] = 0x0C,
        [Key.W] = 0x0D, [Key.E] = 0x0E, [Key.R] = 0x0F, [Key.Y] = 0x10,
        [Key.T] = 0x11, [Key.O] = 0x1F, [Key.U] = 0x20, [Key.I] = 0x22,
        [Key.P] = 0x23, [Key.L] = 0x25, [Key.J] = 0x26, [Key.K] = 0x28,
        [Key.N] = 0x2D, [Key.M] = 0x2E,

        [Key.One] = 0x12, [Key.Two] = 0x13, [Key.Three] = 0x14,
        [Key.Four] = 0x15, [Key.Six] = 0x16, [Key.Five] = 0x17,
        [Key.Equal] = 0x18, [Key.Nine] = 0x19, [Key.Seven] = 0x1A,
        [Key.Minus] = 0x1B, [Key.Eight] = 0x1C, [Key.Zero] = 0x1D,

        [Key.CloseBracket] = 0x1E, [Key.OpenBracket] = 0x21,
        [Key.Apostrophe] = 0x27, [Key.Semicolon] = 0x29,
        [Key.Backslash] = 0x2A, [Key.Comma] = 0x2B,
        [Key.Slash] = 0x2C, [Key.Dot] = 0x2F, [Key.Grave] = 0x32,

        [Key.Space] = 0x31
    };

    private static readonly (Modifiers Mods, int NativeFlags)[] ModifierCombinations =
    {
        (Modifiers.None, 0x00),
        (Modifiers.Shift, 0x02),
        (Modifiers.Alt, 0x08), // Option.
        (Modifiers.Shift | Modifiers.Alt, 0x0A) // Shift+Option
    };

    protected override Dictionary<char, DeadChord> BuildDeadKeyMap(int layoutId,
        Dictionary<char, Chord> directMap)
    {
        var deadKeys = new List<(char DeadChar, Chord DeadChord)>();

        foreach (var key in CharacterKeys)
        {
            if (!KeyCodeMapping.TryGetValue(key, out var keyCode))
                continue;

            foreach (var (mods, nativeFlags) in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (result, chars) = Probe(keyCode, nativeFlags);
                if (result == ProbeResult.DeadKey && chars.Length > 0)
                {
                    deadKeys.Add((chars[0], chord));
                }
            }
        }

        return ProbeDeadKeyCompositions(deadKeys, directMap);
    }

    protected override Dictionary<char, Chord> BuildDirectMap(int layoutId)
    {
        var result = new Dictionary<char, Chord>();
        foreach (var key in CharacterKeys)
        {
            if (!KeyCodeMapping.TryGetValue(key, out var keyCode))
                continue;

            foreach (var (mods, nativeFlags) in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (probeResult, chars) = Probe(keyCode, nativeFlags);

                if (probeResult != ProbeResult.Character || chars.Length <= 0) continue;
                var c = chars[0];
                if (!result.ContainsKey(c) || CountModifiers(mods) < CountModifiers(result[c].Modifiers))
                {
                    result[c] = chord;
                }
            }
        }

        return result;
    }

    private static (ProbeResult Result, string Chars) Probe(int macKeyCode, int nativeFlags)
    {
        uint deadKeyState = 0;
        var outChars = new ushort[4];
        var result = MacLazbot.probeKeyOutput(macKeyCode, nativeFlags, ref deadKeyState, outChars, outChars.Length);

        if (result == -1)
        {
            // We use the space as an arbitrary key to probe the dead key to find the standalone dead character.
            var testKey = KeyCodeMapping[Key.Space];
            var clearState = deadKeyState;
            var clearChars = new ushort[4];
            var clearResult =
                MacLazbot.probeKeyOutput(testKey, 0, ref clearState, clearChars, clearChars.Length);

            if (clearResult > 0)
            {
                var chars = new char[clearResult];
                for (var i = 0; i < clearResult; i++)
                    chars[i] = (char)clearChars[i];
                return (ProbeResult.DeadKey, new string(chars));
            }

            if (outChars[0] != 0)
                return (ProbeResult.DeadKey, ((char)outChars[0]).ToString());

            return (ProbeResult.DeadKey, "");
        }

        if (result > 0)
        {
            var chars = new char[result];
            for (var i = 0; i < result; i++)
                chars[i] = (char)outChars[i];
            return (ProbeResult.Character, new string(chars));
        }

        return (ProbeResult.None, "");
    }

    private static Dictionary<char, DeadChord> ProbeDeadKeyCompositions(
        List<(char DeadChar, Chord DeadChord)> deadKeys,
        Dictionary<char, Chord> directMap)
    {
        var deadMap = new Dictionary<char, DeadChord>();

        var baseKeys = new[]
        {
            Key.A, Key.E, Key.I, Key.O, Key.U, Key.Y, Key.N, Key.C, Key.S, Key.Z, Key.Space
        };

        var baseModifiers = new[] { (Modifiers.None, 0x00), (Modifiers.Shift, 0x02) };

        foreach (var (_, deadChord) in deadKeys)
        {
            foreach (var baseKey in baseKeys)
            {
                if (!KeyCodeMapping.TryGetValue(baseKey, out var baseMacKeyCode))
                    continue;

                foreach (var (baseMods, baseNativeFlags) in baseModifiers)
                {
                    var baseChord = new Chord(baseKey, baseMods);

                    if (!KeyCodeMapping.TryGetValue(deadChord.Key, out var deadMacKeyCode))
                        continue;

                    var deadNativeFlags = GetNativeFlags(deadChord.Modifiers);
                    uint deadKeyState = 0;
                    var deadOutChars = new ushort[4];
                    _ = MacLazbot.probeKeyOutput(deadMacKeyCode, deadNativeFlags, ref deadKeyState, deadOutChars,
                        deadOutChars.Length);

                    var outChars = new ushort[4];
                    var result = MacLazbot.probeKeyOutput(baseMacKeyCode, baseNativeFlags, ref deadKeyState, outChars,
                        outChars.Length);

                    if (result > 0)
                    {
                        var c = (char)outChars[0];
                        if (!directMap.ContainsKey(c) && !deadMap.ContainsKey(c))
                        {
                            deadMap[c] = new DeadChord(deadChord, baseChord);
                        }
                    }

                    // Clear any lingering dead key state.
                    if (deadKeyState != 0)
                    {
                        var clearState = deadKeyState;
                        var clearChars = new ushort[4];
                        // We use the space as an arbitrary key.
                        var testKey = KeyCodeMapping[Key.Space];
                        _ = MacLazbot.probeKeyOutput(testKey, 0, ref clearState, clearChars, clearChars.Length);
                    }
                }
            }
        }

        return deadMap;
    }


    private static int CountModifiers(Modifiers mods)
    {
        var count = 0;
        if (mods.HasFlag(Modifiers.Shift)) count++;
        if (mods.HasFlag(Modifiers.Ctrl)) count++;
        if (mods.HasFlag(Modifiers.Alt)) count++;
        return count;
    }

    private static int GetNativeFlags(Modifiers mods)
    {
        var flags = 0;
        if (mods.HasFlag(Modifiers.Shift)) flags |= 0x02;
        if (mods.HasFlag(Modifiers.Alt)) flags |= 0x08;
        return flags;
    }

    protected override int GetKeyboardLayoutId()
    {
        return MacLazbot.getKeyboardLayoutId();
    }
}
