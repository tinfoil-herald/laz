// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Laz.Extensions.Keyboard;
using Xunit;

namespace Laz.Tests;

public class TypingExtensionTests
{
    private readonly Keyboard _keyboard = new Lazbot().Keyboard;

    [Fact]
    public void NullTextIsNoOp()
    {
        var result = _keyboard.Type(null!);
        Assert.Same(_keyboard, result);
    }

    [Fact]
    public void EmptyTextIsNoOp()
    {
        var result = _keyboard.Type("");
        Assert.Same(_keyboard, result);
    }

    [Fact]
    public void FindTypingActions_DirectMapping_ReturnsChord()
    {
        var expectedChord = new Chord(Key.A);
        var layout = new FakeLayout(direct: expectedChord);

        var actions = TypingExtension.FindTypingActions(layout, "a").ToList();

        Assert.Single(actions);
        Assert.Same(expectedChord, actions[0]);
    }

    [Fact]
    public void FindTypingActions_DeadMapping_ReturnsDeadChord()
    {
        var expectedDeadChord = new DeadChord(new Chord(Key.E), new Chord(Key.Grave));
        var layout = new FakeLayout(dead: expectedDeadChord);

        var actions = TypingExtension.FindTypingActions(layout, "è").ToList();

        Assert.Single(actions);
        Assert.Same(expectedDeadChord, actions[0]);
    }

    [Fact]
    public void FindTypingActions_ComboMapping_ReturnsComboAction()
    {
        var expectedCombo = new Chord(Key.E, Modifiers.Alt);
        var layout = new FakeLayout(combo: expectedCombo);

        var actions = TypingExtension.FindTypingActions(layout, "€").ToList();

        Assert.Single(actions);
        Assert.Same(expectedCombo, actions[0]);
    }

    [Fact]
    public void FindTypingActions_DirectTakesPriorityOverDead()
    {
        var directChord = new Chord(Key.A);
        var deadChord = new DeadChord(new Chord(Key.A), new Chord(Key.Grave));
        var layout = new FakeLayout(direct: directChord, dead: deadChord);

        var actions = TypingExtension.FindTypingActions(layout, "a").ToList();

        Assert.Single(actions);
        Assert.Same(directChord, actions[0]);
    }

    [Fact]
    public void FindTypingActions_DeadTakesPriorityOverCombo()
    {
        var deadChord = new DeadChord(new Chord(Key.E), new Chord(Key.Grave));
        var comboChord = new Chord(Key.E, Modifiers.Alt);
        var layout = new FakeLayout(dead: deadChord, combo: comboChord);

        var actions = TypingExtension.FindTypingActions(layout, "è").ToList();

        Assert.Single(actions);
        Assert.Same(deadChord, actions[0]);
    }

    [Fact]
    public void FindTypingActions_NoMapping_NoFallback_Throws()
    {
        var layout = new FakeLayout();

        var ex = Assert.Throws<ArgumentException>(
            () => TypingExtension.FindTypingActions(layout, "★").ToList());

        Assert.Contains("★", ex.Message);
    }

    [Fact]
    public void FindTypingActions_NoMapping_ClipboardFallbackOnNonLinux_ReturnsClipboardPaste()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return; // clipboard fallback is intentionally disabled on Linux

        var layout = new FakeLayout();

        var actions = TypingExtension.FindTypingActions(layout, "★", clipboardFallback: true).ToList();

        Assert.Single(actions);
        Assert.IsType<ClipboardPaste>(actions[0]);
    }

    [Fact]
    public void FindTypingActions_ClipboardFallbackFalse_Throws()
    {
        var layout = new FakeLayout();

        Assert.Throws<ArgumentException>(
            () => TypingExtension.FindTypingActions(layout, "★", clipboardFallback: false).ToList());
    }

    [Fact]
    public void FindTypingActions_DirectMapping_WithModifiers_ReturnsChordWithModifiers()
    {
        var shiftedChord = new Chord(Key.A, Modifiers.Shift);
        var layout = new FakeLayout(direct: shiftedChord);

        var actions = TypingExtension.FindTypingActions(layout, "A").ToList();

        Assert.Single(actions);
        var chord = Assert.IsType<Chord>(actions[0]);
        Assert.Equal(Key.A, chord.Key);
        Assert.Equal(Modifiers.Shift, chord.Modifiers);
    }

    /// <summary>
    /// A fake ILayout for unit testing FindTypingActions routing logic.
    /// Returns pre-configured actions regardless of the token.
    /// </summary>
    private sealed class FakeLayout : ILayout
    {
        private readonly TypingAction? _direct;
        private readonly TypingAction? _dead;
        private readonly TypingAction? _combo;

        public FakeLayout(TypingAction? direct = null, TypingAction? dead = null, TypingAction? combo = null)
        {
            _direct = direct;
            _dead = dead;
            _combo = combo;
        }

        public bool TryDirectMapping(string token, out TypingAction action)
        {
            action = _direct!;
            return _direct != null;
        }

        public bool TryDeadMapping(string token, out TypingAction action)
        {
            action = _dead!;
            return _dead != null;
        }

        public bool TryComboMapping(string token, out TypingAction action)
        {
            action = _combo!;
            return _combo != null;
        }
    }
}
