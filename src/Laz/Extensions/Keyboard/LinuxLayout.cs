// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Laz.Native;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// The Linux keyboard layout. 
/// </summary>
internal class LinuxLayout : Layout<int>
{
    private static readonly ConcurrentDictionary<int, Dictionary<char, Chord>> _directMaps = new();
    private static readonly ConcurrentDictionary<int, Dictionary<char, DeadChord>> _deadKeyMaps = new();

    protected override ConcurrentDictionary<int, Dictionary<char, Chord>> DirectMaps => _directMaps;
    protected override ConcurrentDictionary<int, Dictionary<char, DeadChord>> DeadKeyMaps => _deadKeyMaps;

    /// <summary>
    /// XKB shift levels mapped to modifier combinations. Where:
    ///  * Level 0 -> base,
    ///  * Level 1 -> Shift
    ///  * Level 2 -> AltGr
    ///  * Level 3 -> Shift+AltGr
    /// </summary>
    private static readonly (Modifiers Mods, int Level)[] ModifierCombinations =
    {
        (Modifiers.None, 0),
        (Modifiers.Shift, 1),
        (Modifiers.Alt, 2),
        (Modifiers.Shift | Modifiers.Alt, 3)
    };

    protected override int GetKeyboardLayoutId()
    {
        return LinuxLazbot.getKeyboardLayoutId();
    }

    protected override Dictionary<char, DeadChord> BuildDeadKeyMap(int layoutId, Dictionary<char, Chord> directMap)
    {
        var deadKeys = new List<(char DeadChar, Chord DeadChord)>();
        foreach (var key in CharacterKeys)
        {
            var virtualKeyCode = (int)key;

            foreach (var (mods, level) in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (result, chars) = Probe(virtualKeyCode, level);

                if (result != ProbeResult.DeadKey || chars.Length <= 0) continue;
                deadKeys.Add((chars[0], chord));
                break;
            }
        }

        return ProbeDeadKeyCompositions(deadKeys, directMap);
    }

    protected override Dictionary<char, Chord> BuildDirectMap(int layoutId)
    {
        var directMap = new Dictionary<char, Chord>();
        foreach (var key in CharacterKeys)
        {
            var virtualKeyCode = (int)key;

            foreach (var (mods, level) in ModifierCombinations)
            {
                var chord = new Chord(key, mods);
                var (result, chars) = Probe(virtualKeyCode, level);

                if (result != ProbeResult.Character || chars.Length <= 0) continue;
                var c = chars[0];
                if (!directMap.ContainsKey(c) || CountModifiers(mods) < CountModifiers(directMap[c].Modifiers))
                {
                    directMap[c] = chord;
                }
            }
        }

        return directMap;
    }

    private static (ProbeResult Result, string Chars) Probe(int virtualKeyCode, int level)
    {
        uint deadKeyState = 0;
        var outChars = new ushort[4];
        var result = LinuxLazbot.probeKeyOutput(virtualKeyCode, level, ref deadKeyState, outChars, outChars.Length);

        if (result == -1)
        {
            // We use the space as an arbitrary key to probe the dead key to find the standalone dead character.
            var testKey = Key.Space;
            var composeState = deadKeyState;
            var composeChars = new ushort[4];
            var composeResult = LinuxLazbot.probeKeyOutput(
                (int) testKey, 0, ref composeState, composeChars, composeChars.Length);

            if (composeResult > 0)
            {
                return (ProbeResult.DeadKey, ((char)composeChars[0]).ToString());
            }
            else
            {
                return (ProbeResult.DeadKey, "");    
            }
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

        var baseModifiers = new[] { (Modifiers.None, 0), (Modifiers.Shift, 1) };

        foreach (var (_, deadChord) in deadKeys)
        {
            foreach (var baseKey in baseKeys)
            {
                foreach (var (baseMods, baseLevel) in baseModifiers)
                {
                    var baseChord = new Chord(baseKey, baseMods);

                    var deadLevel = GetLevel(deadChord.Modifiers);
                    uint deadKeyState = 0;
                    var deadOutChars = new ushort[4];
                    _ = LinuxLazbot.probeKeyOutput(
                        (int)deadChord.Key, deadLevel, ref deadKeyState, deadOutChars, deadOutChars.Length);

                    if (deadKeyState == 0)
                        continue;

                    var outChars = new ushort[4];
                    var result = LinuxLazbot.probeKeyOutput(
                        (int)baseKey, baseLevel, ref deadKeyState, outChars, outChars.Length);

                    if (result > 0)
                    {
                        var c = (char)outChars[0];
                        if (!directMap.ContainsKey(c) && !deadMap.ContainsKey(c))
                        {
                            deadMap[c] = new DeadChord(deadChord, baseChord);
                        }
                    }
                }
            }
        }

        return deadMap;
    }

    private static int GetLevel(Modifiers mods)
    {
        var shift = mods.HasFlag(Modifiers.Shift);
        var alt = mods.HasFlag(Modifiers.Alt);
        if (shift && alt) return 3;
        if (alt) return 2;
        if (shift) return 1;
        return 0;
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
