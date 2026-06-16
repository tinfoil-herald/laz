// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Laz.Extensions.Keyboard;

/// <summary>
/// A keyboard layout.
/// </summary>
internal interface ILayout
{
    public static ILayout Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WinLayout();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacLayout();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxLayout();
        }

        throw new PlatformNotSupportedException();
    }

    bool TryDirectMapping(string token, out TypingAction action);

    bool TryDeadMapping(string token, out TypingAction action);

    bool TryComboMapping(string token, out TypingAction action);
}

internal abstract class Layout<TLayoutId> : ILayout
{
    protected abstract ConcurrentDictionary<TLayoutId, Dictionary<char, Chord>> DirectMaps { get; }
    protected abstract ConcurrentDictionary<TLayoutId, Dictionary<char, DeadChord>> DeadKeyMaps { get; }

    private static readonly Dictionary<String, Chord> SpecialKeys = new()
    {
        { "\n", new Chord(Key.Enter) },
        { "\r\n", new Chord(Key.Enter) },
        { "\r", new Chord(Key.Enter) },
        { "\t", new Chord(Key.Tab) },
        { "\b", new Chord(Key.Backspace) }
    };

    protected readonly Key[] CharacterKeys =
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J,
        Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T,
        Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        Key.Zero, Key.One, Key.Two, Key.Three, Key.Four,
        Key.Five, Key.Six, Key.Seven, Key.Eight, Key.Nine,
        Key.Space, Key.Grave, Key.Minus, Key.Equal,
        Key.OpenBracket, Key.CloseBracket, Key.Backslash,
        Key.Semicolon, Key.Apostrophe, Key.Comma, Key.Dot, Key.Slash
    };


    private Dictionary<char, Chord> GetDirectMap()
    {
        var layoutId = GetKeyboardLayoutId();
        return DirectMaps.GetOrAdd(layoutId, BuildDirectMap);
    }

    private Dictionary<char, DeadChord> GetDeadMap()
    {
        var layoutId = GetKeyboardLayoutId();
        return DeadKeyMaps.GetOrAdd(layoutId, id => BuildDeadKeyMap(id, GetDirectMap()));
    }

    protected abstract TLayoutId GetKeyboardLayoutId();

    protected abstract Dictionary<char, DeadChord> BuildDeadKeyMap(TLayoutId layoutId,
        Dictionary<char, Chord> directMap);

    protected abstract Dictionary<char, Chord> BuildDirectMap(TLayoutId layoutId);

    public bool TryDirectMapping(string token, out TypingAction action)
    {
        if (SpecialKeys.TryGetValue(token, out var specialChord))
        {
            action = specialChord;
            return true;
        }

        if (token.Length == 1 && GetDirectMap().TryGetValue(token[0], out var directMap))
        {
            action = directMap;
            return true;
        }

        action = null;
        return false;
    }

    public bool TryDeadMapping(string token, out TypingAction action)
    {
        if (token.Length == 1 && GetDeadMap().TryGetValue(token[0], out var deadMap))
        {
            action = deadMap;
            return true;
        }

        action = null;
        return false;
    }

    public virtual bool TryComboMapping(string token, out TypingAction action)
    {
        action = null;
        return false;
    }

    /// <summary>
    /// Result of probing a key with ToUnicodeEx.
    /// </summary>
    internal enum ProbeResult
    {
        None,
        Character,
        DeadKey
    }
}